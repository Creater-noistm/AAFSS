using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for signal processing operations including filtering, detrending,
/// windowing, and decimation of time series data.
/// </summary>
public interface ISignalProcessingService
{
    /// <summary>Applies a digital filter to time series data.</summary>
    Task<ProcessingResult> ApplyFilterAsync(Guid dataSourceId, string filterType, Dictionary<string, double> parameters, CancellationToken ct = default);

    /// <summary>Removes linear trend from the data.</summary>
    Task<ProcessingResult> DetrendAsync(Guid dataSourceId, CancellationToken ct = default);

    /// <summary>Decimates (downsamples) the time series data.</summary>
    Task<ProcessingResult> DecimateAsync(Guid dataSourceId, int factor, CancellationToken ct = default);

    /// <summary>Applies a calibration factor to the data.</summary>
    Task<ProcessingResult> ApplyCalibrationAsync(Guid dataSourceId, double sensitivity, double offset = 0, CancellationToken ct = default);

    /// <summary>Computes basic statistics (mean, RMS, peak, crest factor) for the data.</summary>
    Task<Dictionary<string, double>> ComputeBasicStatsAsync(Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default);
}
