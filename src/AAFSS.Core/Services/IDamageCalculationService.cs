using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for fatigue damage calculation using Miner's rule.
/// Computes cumulative damage from a compiled spectrum against an S-N curve.
/// </summary>
public interface IDamageCalculationService
{
    /// <summary>Calculates cumulative damage (Miner's D) against an S-N curve.</summary>
    Task<double> CalculateMinerDamageAsync(Guid spectrumId, SnCurve snCurve, CancellationToken ct = default);

    /// <summary>Calculates damage for a specific octave band against the S-N curve.</summary>
    Task<double> CalculateBandDamageAsync(Guid spectrumId, int bandIndex, SnCurve snCurve, CancellationToken ct = default);

    /// <summary>Computes the damage spectrum (damage per frequency band).</summary>
    Task<double[]> ComputeDamageSpectrumAsync(Guid spectrumId, SnCurve snCurve, CancellationToken ct = default);

    /// <summary>Estimates fatigue life in flight hours.</summary>
    Task<double> EstimateFatigueLifeAsync(Guid spectrumId, SnCurve snCurve, double targetDamage = 1.0, CancellationToken ct = default);
}
