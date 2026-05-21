using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Hdf5;
using AAFSS.Infrastructure.Python;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of IFrequencyAnalysisService using Python.NET bridges.
/// Provides PSD (Welch), octave band analysis, cross-spectrum, coherence, and zoom FFT.
/// Each operation reads from HDF5, delegates computation to Python/scipy, and persists
/// results as SpectrumResult entities.
/// </summary>
public class FrequencyAnalysisService : IFrequencyAnalysisService
{
    private readonly IUnitOfWork _uow;
    private readonly Hdf5TimeSeriesReader _reader;
    private readonly FrequencyAnalysisBridge _bridge;
    private readonly ISpectrumRepository _spectrumRepo;

    public FrequencyAnalysisService(
        IUnitOfWork uow,
        Hdf5TimeSeriesReader reader,
        FrequencyAnalysisBridge bridge,
        ISpectrumRepository spectrumRepo)
    {
        _uow = uow;
        _reader = reader;
        _bridge = bridge;
        _spectrumRepo = spectrumRepo;
    }

    public async Task<SpectrumResult> ComputePsdAsync(
        Guid dataSourceId, SpectrumType spectrumType, FrequencyRange? range = null, CancellationToken ct = default)
    {
        var ds = await _uow.DataSources.GetByIdAsync(dataSourceId, ct)
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} not found.");
        var ts = ds.TimeSeriesData
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} has no TimeSeriesData.");
        var projectId = await GetProjectIdAsync(ds, ct);

        var channelData = await _reader.ReadChannelAsync(projectId, ts, 0, 0, -1);

        var nperseg = spectrumType switch
        {
            SpectrumType.PsdWelch => 4096,
            SpectrumType.PsdPeriodogram => channelData.Length,
            _ => 4096
        };

        var (freqs, psd) = await _bridge.ComputeWelchPsdAsync(channelData, ts.SampleRate, nperseg);

        var oaspl = ComputeOasplFromPsd(freqs, psd);

        var result = new SpectrumResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            SpectrumType = spectrumType,
            Frequencies = freqs,
            Amplitudes = psd,
            Oaspl = oaspl,
            WindowType = "Hanning",
            FftSize = nperseg,
            OverlapRatio = 0.5,
            ComputedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddResultAsync(result, ct);
        return result;
           }

    public async Task<SpectrumResult> ComputeOctaveBandsAsync(
        Guid dataSourceId, SpectrumType octaveType, CancellationToken ct = default)
    {
        var bandsPerOctave = octaveType switch
        {
            SpectrumType.Octave1_1 => 1,
            SpectrumType.Octave1_3 => 3,
            SpectrumType.Octave1_6 => 6,
            SpectrumType.Octave1_12 => 12,
            _ => 3
        };

        // First compute PSD as intermediate
        var psdResult = await ComputePsdAsync(dataSourceId, SpectrumType.PsdWelch, null, ct);

        var (centerFreqs, bandLevels) = await _bridge.ComputeOctaveBandsAsync(
            psdResult.Frequencies, psdResult.Amplitudes, bandsPerOctave);

        var oaspl = bandLevels.Length > 0 ? ComputeOasplFromBands(bandLevels) : 0;

        var result = new SpectrumResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            SpectrumType = octaveType,
            Frequencies = centerFreqs,
            Amplitudes = bandLevels,
            Oaspl = oaspl,
            WindowType = "Octave",
            FftSize = centerFreqs.Length,
            OverlapRatio = 0,
            ComputedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddResultAsync(result, ct);
        return result;
           }

    public async Task<SpectrumResult> ComputeCrossSpectrumAsync(
        Guid dataSourceId1, Guid dataSourceId2, CancellationToken ct = default)
    {
        var (projId1, ts1) = await LoadTimeSeriesAsync(dataSourceId1, ct);
        var (projId2, ts2) = await LoadTimeSeriesAsync(dataSourceId2, ct);

        var ch1 = await _reader.ReadChannelAsync(projId1, ts1, 0, 0, -1);
        var ch2 = await _reader.ReadChannelAsync(projId2, ts2, 0, 0, -1);

        var minLen = Math.Min(ch1.Length, ch2.Length);
        var x = ch1.Take(minLen).ToArray();
        var y = ch2.Take(minLen).ToArray();

        // Cross-spectrum via Python
        dynamic scipy = Python.PythonEngine.Instance.ImportModule("scipy.signal");
        dynamic np = Python.PythonEngine.Instance.ImportModule("numpy");

        using (global::Python.Runtime.Py.GIL()) {
            dynamic f = scipy.csd(x, y,
            ts1.SampleRate, nperseg: 4096);

        var freqs = ConvertFromNumpy(f[0]);
        var csdValues = ConvertFromNumpy(f[1]);

        var result = new SpectrumResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId1,
            SpectrumType = SpectrumType.CrossSpectrum,
            Frequencies = freqs,
            Amplitudes = csdValues,
            Oaspl = 0,
            WindowType = "Hanning",
            FftSize = 4096,
            OverlapRatio = 0.5,
            ComputedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddResultAsync(result, ct);
        return result;
        }
    }

    public async Task<SpectrumResult> ComputeCoherenceAsync(
        Guid dataSourceId1, Guid dataSourceId2, CancellationToken ct = default)
    {
        var (projId1, ts1) = await LoadTimeSeriesAsync(dataSourceId1, ct);
        var (projId2, ts2) = await LoadTimeSeriesAsync(dataSourceId2, ct);

        var ch1 = await _reader.ReadChannelAsync(projId1, ts1, 0, 0, -1);
        var ch2 = await _reader.ReadChannelAsync(projId2, ts2, 0, 0, -1);

        var minLen = Math.Min(ch1.Length, ch2.Length);
        var x = ch1.Take(minLen).ToArray();
        var y = ch2.Take(minLen).ToArray();

        dynamic scipy = Python.PythonEngine.Instance.ImportModule("scipy.signal");
        dynamic np = Python.PythonEngine.Instance.ImportModule("numpy");

        using (global::Python.Runtime.Py.GIL()) {
            dynamic f = scipy.coherence(x, y,
            ts1.SampleRate, nperseg: 4096);

        var freqs = ConvertFromNumpy(f[0]);
        var coherence = ConvertFromNumpy(f[1]);

        var result = new SpectrumResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId1,
            SpectrumType = SpectrumType.Coherence,
            Frequencies = freqs,
            Amplitudes = coherence,
            Oaspl = 0,
            WindowType = "Hanning",
            FftSize = 4096,
            OverlapRatio = 0.5,
            ComputedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddResultAsync(result, ct);
        return result;
        }
    }

    public async Task<SpectrumResult> ComputeZoomFftAsync(
        Guid dataSourceId, FrequencyRange range, CancellationToken ct = default)
    {
        var ds = await _uow.DataSources.GetByIdAsync(dataSourceId, ct)
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} not found.");
        var ts = ds.TimeSeriesData
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} has no TimeSeriesData.");
        var projectId = await GetProjectIdAsync(ds, ct);

        // Zoom FFT: compute PSD over full range, then extract the requested band
        var psdResult = await ComputePsdAsync(dataSourceId, SpectrumType.PsdWelch, null, ct);
        var fullFreqs = psdResult.Frequencies;
        var fullPsd = psdResult.Amplitudes;

        var zFreqs = new List<double>();
        var zPsd = new List<double>();
        for (int i = 0; i < fullFreqs.Length; i++)
        {
            if (fullFreqs[i] >= range.MinHz && fullFreqs[i] <= range.MaxHz)
            {
                zFreqs.Add(fullFreqs[i]);
                zPsd.Add(fullPsd[i]);
            }
        }

        var result = new SpectrumResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            SpectrumType = SpectrumType.ZoomFft,
            Frequencies = zFreqs.ToArray(),
            Amplitudes = zPsd.ToArray(),
            Oaspl = ComputeOasplFromPsd(zFreqs.ToArray(), zPsd.ToArray()),
            WindowType = "Hanning",
            FftSize = 4096,
            OverlapRatio = 0.5,
            ComputedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddResultAsync(result, ct);
        return result;
    }

    private async Task<Guid> GetProjectIdAsync(DataSource ds, CancellationToken ct)
    {
        var profile = await _uow.MissionProfiles.GetByIdAsync(ds.ProfileId, ct);
        return profile?.ProjectId ?? Guid.Empty;
    }

    private async Task<(Guid projectId, TimeSeriesData ts)> LoadTimeSeriesAsync(
        Guid dataSourceId, CancellationToken ct)
    {
        var ds = await _uow.DataSources.GetByIdAsync(dataSourceId, ct)
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} not found.");
        if (ds.TimeSeriesData == null)
            throw new InvalidOperationException($"DataSource {dataSourceId} has no TimeSeriesData.");
        var projectId = await GetProjectIdAsync(ds, ct);
        return (projectId, ds.TimeSeriesData);
    }

    private static double ComputeOasplFromPsd(double[] freqs, double[] psd)
    {
        if (freqs.Length < 2) return 0;
        var totalPower = 0.0;
        for (int i = 1; i < freqs.Length; i++)
        {
            var df = freqs[i] - freqs[i - 1];
            totalPower += Math.Pow(10, psd[i] / 10) * df;
        }
        return totalPower > 0 ? 10 * Math.Log10(totalPower) : 0;
    }

    private static double ComputeOasplFromBands(double[] bandLevels)
    {
        var sum = bandLevels.Sum(l => Math.Pow(10, l / 10));
        return 10 * Math.Log10(sum);
    }

    private static double[] ConvertFromNumpy(dynamic npArray)
    {
        var result = new List<double>();
        foreach (var val in npArray) result.Add((double)val);
        return result.ToArray();
    }
}
