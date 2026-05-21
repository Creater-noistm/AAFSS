using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a single processing step in the data processing pipeline.
/// Forms an ordered audit trail from raw data import to final spectrum compilation.
/// </summary>
public class ProcessingStep
{
    /// <summary>Unique processing step identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent data source identifier.</summary>
    [Required]
    public Guid DataSourceId { get; set; }

    /// <summary>Ordinal position in the processing chain (1-based).</summary>
    public int StepOrder { get; set; }

    /// <summary>Operation type identifier (e.g., "Import", "Filter", "Rainflow", "Compile").</summary>
    [Required]
    [MaxLength(128)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>JSON serialized operation parameters.</summary>
    [MaxLength(4000)]
    public string OperationParams { get; set; } = "{}";

    /// <summary>Reference to input data (HDF5 path or entity ID).</summary>
    [MaxLength(1024)]
    public string InputRef { get; set; } = string.Empty;

    /// <summary>Reference to output data (HDF5 path or entity ID).</summary>
    [MaxLength(1024)]
    public string OutputRef { get; set; } = string.Empty;

    /// <summary>Current processing status.</summary>
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    /// <summary>Timestamp when processing started.</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when processing completed (null if still running).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Error message if processing failed.</summary>
    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    /// <summary>Processing duration in milliseconds.</summary>
    public double DurationMs { get; set; }

    /// <summary>Navigation property to parent data source.</summary>
    [ForeignKey(nameof(DataSourceId))]
    public DataSource? DataSource { get; set; }

    /// <summary>
    /// Marks the step as completed successfully.
    /// </summary>
    public void MarkCompleted()
    {
        Status = ProcessingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        DurationMs = (CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the step as failed with an error message.
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        DurationMs = (CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the step as running.
    /// </summary>
    public void MarkRunning()
    {
        Status = ProcessingStatus.Running;
        StartedAt = DateTime.UtcNow;
    }
}
