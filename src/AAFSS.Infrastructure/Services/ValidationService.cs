using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of IValidationService — validates compiled spectra
/// against damage tolerance criteria. Compares original damage distribution
/// with the compiled spectrum damage and assigns Green/Yellow/Red status
/// based on relative error thresholds.
///
/// Validation criteria:
///   - error = |D_original - D_compiled| / D_original * 100%
///   - Green:  error <= toleranceGreen (default 5%)
///   - Yellow: toleranceGreen < error <= toleranceYellow (default 10%)
///   - Red:    error > toleranceYellow
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IUnitOfWork _uow;
    private readonly ISpectrumRepository _spectrumRepo;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        IUnitOfWork uow,
        ISpectrumRepository spectrumRepo,
        ILogger<ValidationService> logger)
    {
        _uow = uow;
        _spectrumRepo = spectrumRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationReport> ValidateSpectrumAsync(
        Guid spectrumId,
        double targetDamage,
        double toleranceGreen = 0.05,
        double toleranceYellow = 0.10,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Validating spectrum {Id}: targetD={TargetD}, green={Green}%, yellow={Yellow}%",
            spectrumId, targetDamage, toleranceGreen * 100, toleranceYellow * 100);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        var actualDamage = spectrum.DamageValue;
        var warnings = new List<string>();

        // Compute error and deviation
        double error;
        double deviation;

        if (Math.Abs(targetDamage) < 1e-12)
        {
            deviation = actualDamage;
            error = actualDamage > 1e-12 ? 100.0 : 0.0;
            warnings.Add("Target damage is zero — using absolute deviation.");
        }
        else
        {
            deviation = Math.Abs(actualDamage - targetDamage);
            error = deviation / Math.Abs(targetDamage) * 100.0;
        }

        // Assign validation level
        ValidationLevel level;
        ValidationStatus status;

        if (error <= toleranceGreen * 100.0)
        {
            level = ValidationLevel.Green;
            status = ValidationStatus.Passed;
        }
        else if (error <= toleranceYellow * 100.0)
        {
            level = ValidationLevel.Yellow;
            status = ValidationStatus.Warning;
            warnings.Add($"Damage deviation {error:F2}% exceeds green threshold ({toleranceGreen * 100:F1}%).");
        }
        else
        {
            level = ValidationLevel.Red;
            status = ValidationStatus.Failed;
            warnings.Add($"Damage deviation {error:F2}% exceeds yellow threshold ({toleranceYellow * 100:F1}%).");
        }

        // Additional checks
        if (spectrum.Levels.Length == 0)
        {
            level = ValidationLevel.NotValidated;
            status = ValidationStatus.Pending;
            warnings.Add("Spectrum has no frequency levels — cannot validate.");
        }

        if (actualDamage <= 0 && targetDamage > 0)
        {
            warnings.Add("Computed damage is zero while target is non-zero — S-N parameters may be invalid.");
        }

        var report = new ValidationReport
        {
            Id = Guid.NewGuid(),
            SpectrumId = spectrumId,
            TargetD = targetDamage,
            ActualD = actualDamage,
            Deviation = deviation,
            Level = level,
            Warnings = warnings.ToArray(),
            ValidatedAt = DateTime.UtcNow
        };

        // Update spectrum validation status
        spectrum.ValidationStatus = status;

        // Persist report
        await _spectrumRepo.AddValidationAsync(report, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Validation complete for spectrum {Id}: level={Level}, error={Error:F2}%, " +
            "D_actual={Actual:F6}, D_target={Target:F6}",
            spectrumId, level, error, actualDamage, targetDamage);

        return report;
    }

    /// <inheritdoc />
    public async Task<List<ValidationReport>> ValidateProjectAsync(
        Guid projectId, double targetDamage, CancellationToken ct = default)
    {
        _logger.LogInformation("Bulk validating all spectra in project {ProjectId}", projectId);

        var spectra = await _spectrumRepo.GetCompiledByProjectIdAsync(projectId, ct);
        var reports = new List<ValidationReport>();

        foreach (var spectrum in spectra)
        {
            try
            {
                var report = await ValidateSpectrumAsync(spectrum.Id, targetDamage, ct: ct);
                reports.Add(report);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate spectrum {SpectrumId} in project {ProjectId}",
                    spectrum.Id, projectId);

                reports.Add(new ValidationReport
                {
                    Id = Guid.NewGuid(),
                    SpectrumId = spectrum.Id,
                    TargetD = targetDamage,
                    ActualD = spectrum.DamageValue,
                    Deviation = double.NaN,
                    Level = ValidationLevel.NotValidated,
                    Warnings = new[] { $"Validation failed: {ex.Message}" },
                    ValidatedAt = DateTime.UtcNow
                });
            }
        }

        _logger.LogInformation("Validated {Count} spectra in project {ProjectId}",
            reports.Count, projectId);

        return reports;
    }

    /// <inheritdoc />
    public async Task<ValidationReport?> GetValidationReportAsync(
        Guid spectrumId, CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving validation report for spectrum {Id}", spectrumId);

        return await _spectrumRepo.GetValidationBySpectrumIdAsync(spectrumId, ct);
    }

    /// <inheritdoc />
    public async Task<Guid> ValidateAsync(
        Guid compiledSpectrumId, double targetDamage = 1.0, double tolerance = 0.1, CancellationToken ct = default)
    {
        _logger.LogInformation("Validating compiled spectrum {Id}", compiledSpectrumId);

        var report = await ValidateSpectrumAsync(compiledSpectrumId, targetDamage, tolerance, tolerance, ct);
        return report.Id;
    }
}
