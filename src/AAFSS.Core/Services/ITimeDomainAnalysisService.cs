using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for time-domain analysis including rainflow counting,
/// peak-valley detection, and statistical distribution fitting.
/// </summary>
public interface ITimeDomainAnalysisService
{
    /// <summary>Performs rainflow cycle counting on time series data.</summary>
    Task<RainflowResult> RainflowCountAsync(Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default);

    /// <summary>Extracts peak and valley sequences from time series.</summary>
    Task<(double[] Peaks, double[] Valleys)> ExtractPeakValleyAsync(Guid dataSourceId, int channelIndex = 0, CancellationToken ct = default);

    /// <summary>Computes the level crossing histogram.</summary>
    Task<(double[] Levels, int[] Counts)> ComputeLevelCrossingsAsync(Guid dataSourceId, int channelIndex = 0, int numBins = 100, CancellationToken ct = default);
}
