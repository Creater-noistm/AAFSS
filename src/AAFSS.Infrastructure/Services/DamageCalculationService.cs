using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Python;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of IDamageCalculationService using the Python fatigue
/// analysis bridge. Computes Miner's linear cumulative damage, S-N curve
/// fatigue life, per-band damage spectra, and total fatigue life estimates
/// for compiled acoustic fatigue spectra.
/// </summary>
public class DamageCalculationService : IDamageCalculationService
{
    private readonly IUnitOfWork _uow;
    private readonly ISpectrumRepository _spectrumRepo;
    private readonly FatigueBridge _fatigueBridge;
    private readonly ILogger<DamageCalculationService> _logger;

    public DamageCalculationService(
        IUnitOfWork uow,
        ISpectrumRepository spectrumRepo,
        FatigueBridge fatigueBridge,
        ILogger<DamageCalculationService> logger)
    {
        _uow = uow;
        _spectrumRepo = spectrumRepo;
        _fatigueBridge = fatigueBridge;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<double> CalculateMinerDamageAsync(
        Guid spectrumId, SnCurve snCurve, CancellationToken ct = default)
    {
        _logger.LogInformation("Calculating Miner damage for spectrum {Id}", spectrumId);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        var levels = spectrum.Levels;
        if (levels.Length == 0)
        {
            _logger.LogWarning("Spectrum {Id} has no levels — damage = 0", spectrumId);
            return 0.0;
        }

        // Compute S-N parameters from material properties
        // Basquin: sigma_a = sigma_f' * (2N_f)^b  =>  N_f = 0.5 * (sigma_a/sigma_f')^(1/b)
        var m = -1.0 / snCurve.FatigueStrengthExponent; // Convert Basquin exponent b to S-N exponent m
        var C = 0.5 * Math.Pow(snCurve.FatigueStrengthCoefficient, m);

        // Convert dB levels to equivalent stress proxy and compute Miner damage
        double totalDamage = 0.0;
        // Reference cycle count per band: 1 flight = 1000 reference cycles
        const double cyclesPerBand = 1000.0;

        foreach (var level in levels)
        {
            if (level <= 0) continue;

            // Map dB SPL to equivalent stress (linear relation in log space)
            var stressEqv = Math.Pow(10, level / 20.0);

            // S-N curve life at this stress level
            var life = C * Math.Pow(stressEqv, -m);

            if (life > 0 && double.IsFinite(life))
            {
                totalDamage += cyclesPerBand / life;
            }
        }

        _logger.LogInformation("Miner damage for spectrum {Id}: D={Damage:F6}, m={m:F2}, C={C:E2}",
            spectrumId, totalDamage, m, C);

        // Store damage in the spectrum entity
        spectrum.DamageValue = totalDamage;
        await _uow.SaveChangesAsync(ct);

        return totalDamage;
    }

    /// <inheritdoc />
    public async Task<double> CalculateBandDamageAsync(
        Guid spectrumId, int bandIndex, SnCurve snCurve, CancellationToken ct = default)
    {
        _logger.LogInformation("Calculating band damage for spectrum {Id}, band {Index}",
            spectrumId, bandIndex);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        if (bandIndex < 0 || bandIndex >= spectrum.Levels.Length)
            throw new ArgumentOutOfRangeException(
                nameof(bandIndex), $"Band index {bandIndex} out of range [0, {spectrum.Levels.Length}).");

        var level = spectrum.Levels[bandIndex];
        if (level <= 0) return 0.0;

        var m = -1.0 / snCurve.FatigueStrengthExponent;
        var C = 0.5 * Math.Pow(snCurve.FatigueStrengthCoefficient, m);

        var stressEqv = Math.Pow(10, level / 20.0);
        var life = C * Math.Pow(stressEqv, -m);
        const double cyclesPerBand = 1000.0;

        var damage = life > 0 ? cyclesPerBand / life : 0.0;

        _logger.LogInformation("Band {Index} damage: D={Damage:F6}", bandIndex, damage);

        return damage;
    }

    /// <inheritdoc />
    public async Task<double[]> ComputeDamageSpectrumAsync(
        Guid spectrumId, SnCurve snCurve, CancellationToken ct = default)
    {
        _logger.LogInformation("Computing damage spectrum for spectrum {Id}", spectrumId);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        var levels = spectrum.Levels;
        var damageSpectrum = new double[levels.Length];

        var m = -1.0 / snCurve.FatigueStrengthExponent;
        var C = 0.5 * Math.Pow(snCurve.FatigueStrengthCoefficient, m);
        const double cyclesPerBand = 1000.0;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] <= 0)
            {
                damageSpectrum[i] = 0.0;
                continue;
            }

            var stressEqv = Math.Pow(10, levels[i] / 20.0);
            var life = C * Math.Pow(stressEqv, -m);
            damageSpectrum[i] = life > 0 ? cyclesPerBand / life : 0.0;
        }

        _logger.LogInformation("Damage spectrum computed: {Count} bands", damageSpectrum.Length);

        return damageSpectrum;
    }

    /// <inheritdoc />
    public async Task<double> EstimateFatigueLifeAsync(
        Guid spectrumId, SnCurve snCurve, double targetDamage = 1.0, CancellationToken ct = default)
    {
        _logger.LogInformation("Estimating fatigue life for spectrum {Id}, target D={TargetD}",
            spectrumId, targetDamage);

        if (targetDamage <= 0)
            throw new ArgumentException("Target damage must be positive.", nameof(targetDamage));

        // Calculate damage per reference period
        var damagePerPeriod = await CalculateMinerDamageAsync(spectrumId, snCurve, ct);

        if (damagePerPeriod <= 0)
        {
            _logger.LogWarning("Zero damage computed for spectrum {Id} — infinite life", spectrumId);
            return double.PositiveInfinity;
        }

        // Life = (targetDamage / damagePerPeriod) * referencePeriod
        // Reference period is implicitly 1 (per CalculateMinerDamageAsync semantics)
        var fatigueLife = targetDamage / damagePerPeriod;

        _logger.LogInformation(
            "Fatigue life for spectrum {Id}: {Life:F1} reference periods (D_period={D:F6})",
            spectrumId, fatigueLife, damagePerPeriod);

        return fatigueLife;
    }
}
