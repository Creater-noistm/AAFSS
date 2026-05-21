using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Events;

/// <summary>
/// Domain event raised when a validation check completes for a compiled spectrum.
/// </summary>
public record ValidationCompletedEvent : INotification
{
    /// <summary>ID of the compiled spectrum that was validated.</summary>
    public Guid SpectrumId { get; init; }

    /// <summary>ID of the validation report.</summary>
    public Guid ValidationReportId { get; init; }

    /// <summary>Validation level (Green/Yellow/Red).</summary>
    public ValidationLevel Level { get; init; }

    /// <summary>Validation status (Passed/Warning/Failed).</summary>
    public ValidationStatus Status { get; init; }

    /// <summary>Actual calculated damage value.</summary>
    public double ActualD { get; init; }

    /// <summary>Target damage value.</summary>
    public double TargetD { get; init; }

    /// <summary>Deviation from target (|Actual - Target|).</summary>
    public double Deviation { get; init; }

    /// <summary>Parent project ID.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>List of validation warning messages.</summary>
    public string[] Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Timestamp when validation was performed.</summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}
