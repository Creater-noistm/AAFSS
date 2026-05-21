using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for validating compiled spectra against reference values.
/// Performs damage comparison and generates validation reports with
/// green/yellow/red status indicators.
/// </summary>
public interface IValidationService
{
    /// <summary>Validates a compiled spectrum against damage tolerance criteria.</summary>
    Task<ValidationReport> ValidateSpectrumAsync(
        Guid spectrumId,
        double targetDamage,
        double toleranceGreen = 0.05,
        double toleranceYellow = 0.10,
        CancellationToken ct = default);

    /// <summary>Bulk validates all compiled spectra in a project.</summary>
    Task<List<ValidationReport>> ValidateProjectAsync(Guid projectId, double targetDamage, CancellationToken ct = default);

    /// <summary>Gets the validation summary for a spectrum.</summary>
    Task<ValidationReport?> GetValidationReportAsync(Guid spectrumId, CancellationToken ct = default);

    /// <summary>Validates a compiled spectrum and returns the validation report ID.</summary>
    Task<Guid> ValidateAsync(Guid compiledSpectrumId, double targetDamage = 1.0, double tolerance = 0.1, CancellationToken ct = default);
}
