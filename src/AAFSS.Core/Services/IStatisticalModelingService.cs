using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for statistical modeling of rainflow results.
/// Fits distributions (Weibull, Log-Normal, Gumbel, etc.) to cycle data
/// and selects the best fit using KS test and AIC.
/// </summary>
public interface IStatisticalModelingService
{
    /// <summary>Fits the specified distribution to rainflow data.</summary>
    Task<StatisticalModel> FitDistributionAsync(Guid rainflowResultId, DistributionType distributionType, CancellationToken ct = default);

    /// <summary>Fits all supported distributions and selects the best one.</summary>
    Task<StatisticalModel> FitBestDistributionAsync(Guid rainflowResultId, CancellationToken ct = default);

    /// <summary>Generates synthetic samples from a fitted distribution.</summary>
    Task<double[]> GenerateSamplesAsync(Guid statisticalModelId, int sampleCount, CancellationToken ct = default);

    /// <summary>Computes the 95/95 upper tolerance limit.</summary>
    Task<double> ComputeToleranceLimitAsync(Guid statisticalModelId, double confidence = 0.95, double coverage = 0.95, CancellationToken ct = default);
}
