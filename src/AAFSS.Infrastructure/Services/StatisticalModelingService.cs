using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Python;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of IStatisticalModelingService using the StatisticalBridge
/// to scipy.stats. Fits probability distributions (Weibull, Log-Normal, Gumbel, etc.)
/// to rainflow cycle amplitude data, performs goodness-of-fit testing via
/// Kolmogorov-Smirnov and AIC, generates synthetic samples, and computes
/// upper tolerance limits (95/95).
/// </summary>
public class StatisticalModelingService : IStatisticalModelingService
{
    private readonly IUnitOfWork _uow;
    private readonly ISpectrumRepository _spectrumRepo;
    private readonly StatisticalBridge _bridge;
    private readonly ILogger<StatisticalModelingService> _logger;

    public StatisticalModelingService(
        IUnitOfWork uow,
        ISpectrumRepository spectrumRepo,
        StatisticalBridge bridge,
        ILogger<StatisticalModelingService> logger)
    {
        _uow = uow;
        _spectrumRepo = spectrumRepo;
        _bridge = bridge;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StatisticalModel> FitDistributionAsync(
        Guid rainflowResultId, DistributionType distributionType, CancellationToken ct = default)
    {
        _logger.LogInformation("Fitting {Distribution} to rainflow result {Id}",
            distributionType, rainflowResultId);

        // Load rainflow result and extract amplitude data
        var amplitudes = await GetAmplitudeDataAsync(rainflowResultId, ct);

        // Fit distribution via Python bridge
        var (parameters, ksStat, ksPVal, aic) = await _bridge.FitDistributionAsync(amplitudes, distributionType);

        // Compute goodness-of-fit from KS p-value (0-1, higher is better)
        var gof = Math.Min(ksPVal, 1.0);

        var fitStatus = ksPVal >= 0.05
            ? "Passed (p >= 0.05)"
            : $"Marginal (p = {ksPVal:F4})";

        var model = new StatisticalModel
        {
            Id = Guid.NewGuid(),
            RainflowResultId = rainflowResultId,
            DistributionType = distributionType,
            Parameters = parameters,
            KsStatistic = ksStat,
            KsPValue = ksPVal,
            AicValue = aic,
            GoodnessOfFit = gof,
            FitStatus = fitStatus,
            FittedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddStatisticalModelAsync(model, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Distribution fit complete: {Dist}, KS={Ks:F4}, AIC={Aic:F2}, GoF={Gof:F3}",
            distributionType, ksStat, aic, gof);

        return model;
    }

    /// <inheritdoc />
    public async Task<StatisticalModel> FitBestDistributionAsync(
        Guid rainflowResultId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fitting best distribution to rainflow result {Id}", rainflowResultId);

        var amplitudes = await GetAmplitudeDataAsync(rainflowResultId, ct);

        var (bestDist, parameters, ksStat, ksPVal, aic) = await _bridge.FitBestDistributionAsync(amplitudes);

        var gof = Math.Min(ksPVal, 1.0);

        var fitStatus = ksPVal >= 0.05
            ? $"Best fit: {bestDist} (p >= 0.05)"
            : $"Best fit: {bestDist} (p = {ksPVal:F4})";

        var model = new StatisticalModel
        {
            Id = Guid.NewGuid(),
            RainflowResultId = rainflowResultId,
            DistributionType = bestDist,
            Parameters = parameters,
            KsStatistic = ksStat,
            KsPValue = ksPVal,
            AicValue = aic,
            GoodnessOfFit = gof,
            FitStatus = fitStatus,
            FittedAt = DateTime.UtcNow
        };

        await _spectrumRepo.AddStatisticalModelAsync(model, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Best distribution: {Dist}, KS={Ks:F4}, AIC={Aic:F2}",
            bestDist, ksStat, aic);

        return model;
    }

    /// <inheritdoc />
    public async Task<double[]> GenerateSamplesAsync(
        Guid statisticalModelId, int sampleCount, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} samples from statistical model {Id}",
            sampleCount, statisticalModelId);

        var model = await _spectrumRepo.GetStatisticalModelByIdAsync(statisticalModelId, ct)
            ?? throw new InvalidOperationException($"Statistical model {statisticalModelId} not found.");

        if (model.Parameters.Length == 0)
            throw new InvalidOperationException("Statistical model has no fitted parameters.");

        if (sampleCount <= 0)
            throw new ArgumentException("Sample count must be positive.", nameof(sampleCount));

        var samples = await _bridge.GenerateSamplesAsync(
            model.DistributionType, model.Parameters, sampleCount);

        _logger.LogInformation("Generated {Count} samples from {Dist} distribution",
            samples.Length, model.DistributionType);

        return samples;
    }

    /// <inheritdoc />
    public async Task<double> ComputeToleranceLimitAsync(
        Guid statisticalModelId,
        double confidence = 0.95,
        double coverage = 0.95,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Computing {Conf:P0}/{Cov:P0} tolerance limit for model {Id}",
            confidence, coverage, statisticalModelId);

        var model = await _spectrumRepo.GetStatisticalModelByIdAsync(statisticalModelId, ct)
            ?? throw new InvalidOperationException($"Statistical model {statisticalModelId} not found.");

        if (model.Parameters.Length == 0)
            throw new InvalidOperationException("Statistical model has no fitted parameters.");

        // Use tolerance limit from the bridge (PPF at coverage level)
        var limit = await _bridge.ComputeToleranceLimitAsync(
            model.DistributionType, model.Parameters, confidence, coverage);

        _logger.LogInformation(
            "Tolerance limit for {Dist}: {Limit:F4} at {Conf:P0}/{Cov:P0}",
            model.DistributionType, limit, confidence, coverage);

        return limit;
    }

    // ─── Helper methods ────────────────────────────────────────────────

    /// <summary>
    /// Extracts cycle amplitude data from a rainflow result for distribution fitting.
    /// Uses cycle counts to reconstruct the raw amplitude sample.
    /// </summary>
    private async Task<double[]> GetAmplitudeDataAsync(Guid rainflowResultId, CancellationToken ct)
    {
        var rfResult = await _spectrumRepo.GetRainflowByIdAsync(rainflowResultId, ct)
            ?? throw new InvalidOperationException($"Rainflow result {rainflowResultId} not found.");

        var cycleCounts = rfResult.CycleCounts;
        if (cycleCounts.Length == 0)
            throw new InvalidOperationException("Rainflow result has no cycle counts.");

        // Reconstruct amplitudes from cycle count histogram
        var totalCycles = (int)cycleCounts.Sum();
        if (totalCycles == 0)
            throw new InvalidOperationException("Rainflow result has zero total cycles.");

        var binCount = cycleCounts.Length;
        var amplitudes = new List<double>();

        // Use normalized bin positions as amplitude proxies
        for (int i = 0; i < binCount; i++)
        {
            var count = (int)Math.Round(cycleCounts[i]);
            if (count <= 0) continue;

            // Map bin index to amplitude using the stored max amplitude range
            var ampProxy = rfResult.MaxAmplitude * (i + 0.5) / binCount;
            for (int j = 0; j < Math.Min(count, totalCycles); j++)
            {
                amplitudes.Add(ampProxy);
            }
        }

        if (amplitudes.Count == 0)
        {
            // Fallback: use bin centers
            for (int i = 0; i < binCount; i++)
                amplitudes.Add(rfResult.MaxAmplitude * (i + 0.5) / binCount);
        }

        _logger.LogInformation("Reconstructed {Count} amplitude samples from rainflow histogram",
            amplitudes.Count);

        return amplitudes.ToArray();
    }
}
