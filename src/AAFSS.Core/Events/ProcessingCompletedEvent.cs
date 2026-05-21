using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Events;

/// <summary>
/// Domain event raised when a processing operation (filter, rainflow, PSD, etc.) completes.
/// </summary>
public record ProcessingCompletedEvent : INotification
{
    /// <summary>ID of the data source that was processed.</summary>
    public Guid DataSourceId { get; init; }

    /// <summary>ID of the processing step record.</summary>
    public Guid ProcessingStepId { get; init; }

    /// <summary>Type of processing operation performed.</summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>Duration of the processing in milliseconds.</summary>
    public double DurationMs { get; init; }

    /// <summary>Whether the processing completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Processing status (Completed / Failed / Cancelled).</summary>
    public Models.ProcessingStatus Status { get; init; }

    /// <summary>Error message if processing failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Result entity ID (e.g., SpectrumResult.Id, RainflowResult.Id) if created.</summary>
    public Guid? ResultEntityId { get; init; }

    /// <summary>Timestamp when the event was raised.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
