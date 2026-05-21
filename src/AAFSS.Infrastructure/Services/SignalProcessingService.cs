using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Hdf5;
using AAFSS.Infrastructure.Python;
using System.Text.Json;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of ISignalProcessingService using Python.NET bridges.
/// Provides Butterworth filtering, linear detrending, decimation, calibration, and basic statistics.
/// Each operation reads from HDF5, processes via Python/scipy, writes results back to HDF5,
/// and creates a ProcessingStep audit record.
/// </summary>
public class SignalProcessingService : ISignalProcessingService
{
    private readonly IUnitOfWork _uow;
    private readonly Hdf5TimeSeriesReader _reader;
    private readonly Hdf5TimeSeriesWriter _writer;
    private readonly SignalProcessingBridge _bridge;

    public SignalProcessingService(
        IUnitOfWork uow,
        Hdf5TimeSeriesReader reader,
        Hdf5TimeSeriesWriter writer,
        SignalProcessingBridge bridge)
    {
        _uow = uow;
        _reader = reader;
        _writer = writer;
        _bridge = bridge;
    }

    public async Task<ProcessingResult> ApplyFilterAsync(
        Guid dataSourceId, string filterType, Dictionary<string, double> parameters, CancellationToken ct = default)
    {
        try
        {
            var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);
            var data = await _reader.ReadFullAsync(projectId, timeSeries);

            var sampleRate = timeSeries.SampleRate;
            var cutoffHz = parameters.GetValueOrDefault("cutoff", sampleRate / 4.0);
            var order = (int)parameters.GetValueOrDefault("order", 4);

            var resultData = new double[timeSeries.SampleCount, timeSeries.ChannelCount];
            for (int ch = 0; ch < timeSeries.ChannelCount; ch++)
            {
                var channel = new double[timeSeries.SampleCount];
                for (long i = 0; i < timeSeries.SampleCount; i++) channel[i] = data[i, ch];
                var filtered = await _bridge.ButterworthFilterAsync(channel, sampleRate, cutoffHz, filterType, order);
                for (long i = 0; i < timeSeries.SampleCount; i++) resultData[i, ch] = filtered[i];
            }

            var outputPath = $"/data/{dataSourceId:N}/filtered_{Guid.NewGuid():N}";
            await _writer.WriteFullArrayAsync(projectId, outputPath, resultData, sampleRate,
                timeSeries.ChannelNames, timeSeries.ChannelUnits, timeSeries.Quantity);

            var step = CreateStep(dataSourceId, timeSeries.ChannelCount, "Filter",
                timeSeries.Hdf5Path, outputPath, parameters);
            await _uow.ProcessingSteps.AddAsync(step, ct);
            await _uow.SaveChangesAsync(ct);

            return new ProcessingResult
            {
                Success = true, ProcessingStepId = step.Id, OutputRef = outputPath,
                DurationMs = step.DurationMs,
                Metadata = new Dictionary<string, object> { ["filterType"] = filterType, ["cutoffHz"] = cutoffHz, ["order"] = order }
            };
        }
        catch (Exception ex)
        {
            return new ProcessingResult { Success = false, ErrorMessage = $"Filter failed: {ex.Message}" };
        }
    }

    public async Task<ProcessingResult> DetrendAsync(Guid dataSourceId, CancellationToken ct = default)
    {
        try
        {
            var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);
            var data = await _reader.ReadFullAsync(projectId, timeSeries);

            var resultData = new double[timeSeries.SampleCount, timeSeries.ChannelCount];
            for (int ch = 0; ch < timeSeries.ChannelCount; ch++)
            {
                var channel = new double[timeSeries.SampleCount];
                for (long i = 0; i < timeSeries.SampleCount; i++) channel[i] = data[i, ch];
                var detrended = await _bridge.DetrendAsync(channel);
                for (long i = 0; i < timeSeries.SampleCount; i++) resultData[i, ch] = detrended[i];
            }

            var outputPath = $"/data/{dataSourceId:N}/detrended_{Guid.NewGuid():N}";
            await _writer.WriteFullArrayAsync(projectId, outputPath, resultData, timeSeries.SampleRate,
                timeSeries.ChannelNames, timeSeries.ChannelUnits, timeSeries.Quantity);

            var step = CreateStep(dataSourceId, timeSeries.ChannelCount, "Detrend",
                timeSeries.Hdf5Path, outputPath, new Dictionary<string, double>());
            await _uow.ProcessingSteps.AddAsync(step, ct);
            await _uow.SaveChangesAsync(ct);

            return new ProcessingResult
            {
                Success = true, ProcessingStepId = step.Id, OutputRef = outputPath,
                DurationMs = step.DurationMs
            };
        }
        catch (Exception ex)
        {
            return new ProcessingResult { Success = false, ErrorMessage = $"Detrend failed: {ex.Message}" };
        }
    }

    public async Task<ProcessingResult> DecimateAsync(Guid dataSourceId, int factor, CancellationToken ct = default)
    {
        try
        {
            if (factor <= 1)
                return new ProcessingResult { Success = false, ErrorMessage = "Decimation factor must be > 1." };

            var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);
            var data = await _reader.ReadFullAsync(projectId, timeSeries);

            // Pre-filter to avoid aliasing, then decimate
            var nyquist = timeSeries.SampleRate / 2.0;
            var antiAliasCutoff = nyquist / factor;

            var filteredData = new double[timeSeries.SampleCount, timeSeries.ChannelCount];
            for (int ch = 0; ch < timeSeries.ChannelCount; ch++)
            {
                var channel = new double[timeSeries.SampleCount];
                for (long i = 0; i < timeSeries.SampleCount; i++) channel[i] = data[i, ch];
                var filtered = await _bridge.ButterworthFilterAsync(channel, timeSeries.SampleRate, antiAliasCutoff, "low", 8);
                for (long i = 0; i < timeSeries.SampleCount; i++) filteredData[i, ch] = filtered[i];
            }

            var newSampleCount = timeSeries.SampleCount / factor;
            var resultData = new double[newSampleCount, timeSeries.ChannelCount];
            for (int ch = 0; ch < timeSeries.ChannelCount; ch++)
            {
                var channel = new double[timeSeries.SampleCount];
                for (long i = 0; i < timeSeries.SampleCount; i++) channel[i] = filteredData[i, ch];
                var decimated = await _bridge.DecimateAsync(channel, factor);
                for (long i = 0; i < newSampleCount && i < decimated.Length; i++) resultData[i, ch] = decimated[i];
            }

            var outputPath = $"/data/{dataSourceId:N}/decimated_{Guid.NewGuid():N}";
            var newRate = timeSeries.SampleRate / factor;
            await _writer.WriteFullArrayAsync(projectId, outputPath, resultData, newRate,
                timeSeries.ChannelNames, timeSeries.ChannelUnits, timeSeries.Quantity);

            var @params = new Dictionary<string, double> { ["factor"] = factor, ["newSampleRate"] = newRate };
            var step = CreateStep(dataSourceId, timeSeries.ChannelCount, "Decimate",
                timeSeries.Hdf5Path, outputPath, @params);
            await _uow.ProcessingSteps.AddAsync(step, ct);
            await _uow.SaveChangesAsync(ct);

            return new ProcessingResult
            {
                Success = true, ProcessingStepId = step.Id, OutputRef = outputPath,
                DurationMs = step.DurationMs,
                Metadata = new Dictionary<string, object> { ["factor"] = factor, ["newSampleRate"] = newRate, ["newSampleCount"] = newSampleCount }
            };
        }
        catch (Exception ex)
        {
            return new ProcessingResult { Success = false, ErrorMessage = $"Decimate failed: {ex.Message}" };
        }
    }

    public async Task<ProcessingResult> ApplyCalibrationAsync(
        Guid dataSourceId, double sensitivity, double offset = 0, CancellationToken ct = default)
    {
        try
        {
            var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);
            var data = await _reader.ReadFullAsync(projectId, timeSeries);

            var resultData = new double[timeSeries.SampleCount, timeSeries.ChannelCount];
            for (long i = 0; i < timeSeries.SampleCount; i++)
                for (int ch = 0; ch < timeSeries.ChannelCount; ch++)
                    resultData[i, ch] = data[i, ch] * sensitivity + offset;

            var outputPath = $"/data/{dataSourceId:N}/calibrated_{Guid.NewGuid():N}";
            await _writer.WriteFullArrayAsync(projectId, outputPath, resultData, timeSeries.SampleRate,
                timeSeries.ChannelNames, timeSeries.ChannelUnits, timeSeries.Quantity);

            var @params = new Dictionary<string, double> { ["sensitivity"] = sensitivity, ["offset"] = offset };
            var step = CreateStep(dataSourceId, timeSeries.ChannelCount, "Calibrate",
                timeSeries.Hdf5Path, outputPath, @params);
            await _uow.ProcessingSteps.AddAsync(step, ct);
            await _uow.SaveChangesAsync(ct);

            return new ProcessingResult
            {
                Success = true, ProcessingStepId = step.Id, OutputRef = outputPath,
                DurationMs = step.DurationMs,
                Metadata = new Dictionary<string, object> { ["sensitivity"] = sensitivity, ["offset"] = offset }
            };
        }
        catch (Exception ex)
        {
            return new ProcessingResult { Success = false, ErrorMessage = $"Calibration failed: {ex.Message}" };
        }
    }

    public async Task<Dictionary<string, double>> ComputeBasicStatsAsync(
        Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default)
    {
        try
        {
            var (projectId, timeSeries) = await LoadTimeSeriesAsync(dataSourceId, ct);
            var channelData = await _reader.ReadChannelAsync(projectId, timeSeries, channelIndex, 0, -1);

            var n = channelData.Length;
            var mean = channelData.Average();
            var rms = Math.Sqrt(channelData.Sum(x => x * x) / n);
            var peak = channelData.Max(Math.Abs);
            var stdev = Math.Sqrt(channelData.Sum(x => (x - mean) * (x - mean)) / (n - 1));
            var crestFactor = Math.Abs(peak) > 1e-12 ? Math.Abs(peak) / rms : 0;
            var skewness = ComputeSkewness(channelData, mean, stdev) / n;
            var kurtosis = ComputeKurtosis(channelData, mean, stdev) / n - 3; // excess kurtosis
            var min = channelData.Min();
            var max = channelData.Max();

            return new Dictionary<string, double>
            {
                ["n"] = n,
                ["mean"] = mean,
                ["rms"] = rms,
                ["peak"] = peak,
                ["stdev"] = stdev,
                ["crestFactor"] = crestFactor,
                ["skewness"] = skewness,
                ["kurtosis"] = kurtosis,
                ["min"] = min,
                ["max"] = max,
                ["sampleRate"] = timeSeries.SampleRate
            };
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    private async Task<(Guid projectId, TimeSeriesData timeSeries)> LoadTimeSeriesAsync(
        Guid dataSourceId, CancellationToken ct)
    {
        var ds = await _uow.DataSources.GetByIdAsync(dataSourceId, ct)
            ?? throw new InvalidOperationException($"DataSource {dataSourceId} not found.");
        if (ds.TimeSeriesData == null)
            throw new InvalidOperationException($"DataSource {dataSourceId} has no TimeSeriesData.");

        // Find project ID through the profile chain
        var profile = await _uow.MissionProfiles.GetByIdAsync(ds.ProfileId, ct);
        var projectId = profile?.ProjectId ?? Guid.Empty;
        if (projectId == Guid.Empty)
            throw new InvalidOperationException($"Cannot determine ProjectId for DataSource {dataSourceId}.");

        return (projectId, ds.TimeSeriesData);
    }

    private static ProcessingStep CreateStep(Guid dataSourceId, int channels, string opType,
        string inputRef, string outputRef, Dictionary<string, double> parameters)
    {
        var step = new ProcessingStep
        {
            DataSourceId = dataSourceId,
            OperationType = opType,
            InputRef = inputRef,
            OutputRef = outputRef,
            OperationParams = JsonSerializer.Serialize(parameters),
            StepOrder = 1
        };
        step.MarkRunning();
        System.Threading.Thread.Sleep(1); // ensure startedAt < completedAt
        step.MarkCompleted();
        return step;
    }

    private static double ComputeSkewness(double[] data, double mean, double stdev)
    {
        if (stdev < 1e-12) return 0;
        return data.Sum(x => Math.Pow((x - mean) / stdev, 3));
    }

    private static double ComputeKurtosis(double[] data, double mean, double stdev)
    {
        if (stdev < 1e-12) return 0;
        return data.Sum(x => Math.Pow((x - mean) / stdev, 4));
    }
}
