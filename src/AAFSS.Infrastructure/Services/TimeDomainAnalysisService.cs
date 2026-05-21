using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Hdf5;
using AAFSS.Infrastructure.Python;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of ITimeDomainAnalysisService using the TimeDomainBridge.
/// Provides ASTM E1049 rainflow cycle counting, peak-valley extraction, and
/// level crossing analysis on time series data stored in HDF5.
/// </summary>
public class TimeDomainAnalysisService : ITimeDomainAnalysisService
{
    private readonly IUnitOfWork _uow;
    private readonly Hdf5TimeSeriesReader _reader;
    private readonly TimeDomainBridge _bridge;
    private readonly ILogger<TimeDomainAnalysisService> _logger;

    public TimeDomainAnalysisService(
        IUnitOfWork uow,
        Hdf5TimeSeriesReader reader,
        TimeDomainBridge bridge,
        ILogger<TimeDomainAnalysisService> logger)
    {
        _uow = uow;
        _reader = reader;
        _bridge = bridge;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RainflowResult> RainflowCountAsync(
        Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default)
    {
        _logger.LogInformation("Performing rainflow counting on DataSource {Id}, channel {Ch}",
            dataSourceId, channelIndex);

        var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);

        // Read the specified channel from HDF5
        var channelData = await _reader.ReadChannelAsync(projectId, timeSeries, channelIndex, 0, -1);

        // Perform rainflow counting via the bridge
        var (from, to, amplitudes, means) = await _bridge.RainflowCountAsync(channelData);

        // Build cycle count distribution (histogram of amplitudes into bins)
        var binCount = 64;
        var minAmp = amplitudes.Length > 0 ? amplitudes.Min() : 0;
        var maxAmp = amplitudes.Length > 0 ? amplitudes.Max() : 1;
        var ampRange = maxAmp - minAmp;
        if (ampRange < 1e-12) ampRange = 1.0;

        var cycleCounts = new double[binCount];
        foreach (var amp in amplitudes)
        {
            var bin = (int)((amp - minAmp) / ampRange * (binCount - 1));
            bin = Math.Clamp(bin, 0, binCount - 1);
            cycleCounts[bin]++;
        }

        // Build from-to matrix (simplified: 2D histogram of from-to pairs)
        var fromToMatrix = new double[binCount, binCount];
        for (int i = 0; i < from.Length; i++)
        {
            var fromBin = (int)((from[i] - minAmp) / ampRange * (binCount - 1));
            var toBin = (int)((to[i] - minAmp) / ampRange * (binCount - 1));
            fromBin = Math.Clamp(fromBin, 0, binCount - 1);
            toBin = Math.Clamp(toBin, 0, binCount - 1);
            fromToMatrix[fromBin, toBin]++;
        }

        var result = new RainflowResult
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            FromToMatrix = fromToMatrix,
            CycleCounts = cycleCounts,
            TotalCycles = amplitudes.Length,
            MaxAmplitude = maxAmp,
            MinMean = means.Length > 0 ? means.Min() : 0,
            MaxMean = means.Length > 0 ? means.Max() : 0,
            BinCount = binCount,
            ComputedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Rainflow counting complete: {Cycles} cycles, maxAmp={MaxAmp:F4}, " +
            "minMean={MinMean:F4}, maxMean={MaxMean:F4}",
            result.TotalCycles, result.MaxAmplitude, result.MinMean, result.MaxMean);

        return result;
    }

    /// <inheritdoc />
    public async Task<(double[] Peaks, double[] Valleys)> ExtractPeakValleyAsync(
        Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default)
    {
        _logger.LogInformation("Extracting peaks/valleys from DataSource {Id}, channel {Ch}",
            dataSourceId, channelIndex);

        var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);

        var channelData = await _reader.ReadChannelAsync(projectId, timeSeries, channelIndex, 0, -1);

        var (peaks, valleys) = await _bridge.ExtractPeakValleyAsync(channelData);

        _logger.LogInformation("Extracted {PeakCount} peaks and {ValleyCount} valleys",
            peaks.Length, valleys.Length);

        return (peaks, valleys);
    }

    /// <inheritdoc />
    public async Task<(double[] Levels, int[] Counts)> ComputeLevelCrossingsAsync(
        Guid dataSourceId, int channelIndex = 0, int numBins = 100, CancellationToken ct = default)
    {
        _logger.LogInformation("Computing level crossings for DataSource {Id}, channel {Ch}, bins={Bins}",
            dataSourceId, channelIndex, numBins);

        var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);

        var channelData = await _reader.ReadChannelAsync(projectId, timeSeries, channelIndex, 0, -1);

        if (channelData.Length < 2)
        {
            _logger.LogWarning("Data too short for level crossing analysis: {Len} samples", channelData.Length);
            return (Array.Empty<double>(), Array.Empty<int>());
        }

        // Compute level crossing counts
        var dataMin = channelData.Min();
        var dataMax = channelData.Max();
        var range = dataMax - dataMin;
        if (range < 1e-12) range = 1.0;

        var levels = new double[numBins + 1];
        var counts = new int[numBins + 1];

        for (int i = 0; i <= numBins; i++)
        {
            levels[i] = dataMin + range * i / numBins;
        }

        // Count positive-going level crossings
        for (int i = 1; i < channelData.Length; i++)
        {
            for (int j = 0; j < levels.Length; j++)
            {
                if (channelData[i - 1] <= levels[j] && channelData[i] > levels[j])
                {
                    counts[j]++;
                }
            }
        }

        _logger.LogInformation(
            "Level crossing analysis complete: {TotalCrossings} total crossings",
            counts.Sum());

        return (levels, counts);
    }

    // ─── Helper methods ────────────────────────────────────────────────

    private async Task<(Guid projectId, TimeSeriesData timeSeries)> LoadTimeSeriesAsync(
        Guid dataSourceId, CancellationToken ct)
    {
        var ds = await _uow.DataSources.GetByIdAsync(dataSourceId, ct)
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} not found.");
        if (ds.TimeSeriesData == null)
            throw new InvalidOperationException($"DataSource {dataSourceId} has no TimeSeriesData.");

        var profile = await _uow.MissionProfiles.GetByIdAsync(ds.ProfileId, ct);
        var projectId = profile?.ProjectId ?? Guid.Empty;
        if (projectId == Guid.Empty)
            throw new InvalidOperationException($"Cannot determine ProjectId for DataSource {dataSourceId}.");

        return (projectId, ds.TimeSeriesData);
    }
}
