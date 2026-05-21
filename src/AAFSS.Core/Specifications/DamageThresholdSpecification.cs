using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification for evaluating whether a compiled spectrum's damage value is within acceptable thresholds.
/// Defines the green/yellow/red tolerance bands used in validation.
/// </summary>
public class DamageThresholdSpecification
{
    /// <summary>Target damage value (D).</summary>
    public double TargetDamage { get; set; } = 1.0;

    /// <summary>Green threshold: |Actual - Target| / Target must be below this value.</summary>
    public double GreenThreshold { get; set; } = 0.05;

    /// <summary>Yellow threshold: |Actual - Target| / Target must be below this value.</summary>
    public double YellowThreshold { get; set; } = 0.10;

    /// <summary>
    /// Evaluates the validation level for a given actual damage value.
    /// </summary>
    /// <param name="actualDamage">The calculated damage value.</param>
    /// <returns>ValidationLevel indicating Green, Yellow, or Red.</returns>
    public ValidationLevel Evaluate(double actualDamage)
    {
        if (TargetDamage <= 0)
            return ValidationLevel.NotValidated;

        var relativeDeviation = Math.Abs(actualDamage - TargetDamage) / TargetDamage;

        if (relativeDeviation <= GreenThreshold)
            return ValidationLevel.Green;

        if (relativeDeviation <= YellowThreshold)
            return ValidationLevel.Yellow;

        return ValidationLevel.Red;
    }

    /// <summary>
    /// Evaluates whether a compiled spectrum passes validation (Green or Yellow).
    /// </summary>
    /// <param name="spectrum">The compiled spectrum to evaluate.</param>
    /// <returns>True if the spectrum passes; false if it fails or hasn't been validated.</returns>
    public bool IsSatisfiedBy(CompiledSpectrum spectrum)
    {
        if (spectrum.ValidationStatus == ValidationStatus.Pending)
            return false;

        return spectrum.ValidationStatus == ValidationStatus.Passed ||
               spectrum.ValidationStatus == ValidationStatus.Warning;
    }

    /// <summary>
    /// Gets the validation status for a given actual damage value.
    /// </summary>
    public ValidationStatus GetValidationStatus(double actualDamage)
    {
        return Evaluate(actualDamage) switch
        {
            ValidationLevel.Green => ValidationStatus.Passed,
            ValidationLevel.Yellow => ValidationStatus.Warning,
            ValidationLevel.Red => ValidationStatus.Failed,
            _ => ValidationStatus.Pending
        };
    }

    /// <summary>
    /// Computes normalized damage (actual / target) for comparison.
    /// </summary>
    public double Normalize(double actualDamage)
    {
        return TargetDamage > 0 ? actualDamage / TargetDamage : double.NaN;
    }
}
