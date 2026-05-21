using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a data source — a file or device input containing acoustic/fatigue measurement data.
/// </summary>
public class DataSource
{
    /// <summary>Unique data source identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent mission profile identifier.</summary>
    [Required]
    public Guid ProfileId { get; set; }

    /// <summary>Parent project identifier (denormalized for querying).</summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>Associated measurement point identifier (optional).</summary>
    public Guid? PointId { get; set; }

    /// <summary>Data source origin type.</summary>
    public DataSourceType Type { get; set; } = DataSourceType.Measurement;

    /// <summary>File format extension (e.g., "csv", "xlsx", "tdms").</summary>
    [Required]
    [MaxLength(32)]
    public string Format { get; set; } = string.Empty;

    /// <summary>Original file path.</summary>
    [MaxLength(1024)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>JSON serialized metadata (channel info, units, etc.).</summary>
    [MaxLength(4000)]
    public string Metadata { get; set; } = "{}";

    /// <summary>Validation result from import-time checks.</summary>
    [MaxLength(4000)]
    public string ValidationResultJson { get; set; } = "{}";

    /// <summary>Timestamp when the data was imported.</summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Total number of data points in the source file.</summary>
    public long TotalDataPoints { get; set; }

    /// <summary>Sensor type classification.</summary>
    public SensorType SensorType { get; set; } = SensorType.Accelerometer;

    /// <summary>Associated processing steps (audit trail).</summary>
    public List<ProcessingStep> ProcessingSteps { get; set; } = new();

    /// <summary>Associated time series data reference.</summary>
    public TimeSeriesData? TimeSeriesData { get; set; }

    /// <summary>Associated spectrum results.</summary>
    public List<SpectrumResult> SpectrumResults { get; set; } = new();

    /// <summary>Associated rainflow results.</summary>
    public List<RainflowResult> RainflowResults { get; set; } = new();

    /// <summary>Navigation property to parent profile.</summary>
    [ForeignKey(nameof(ProfileId))]
    public MissionProfile? Profile { get; set; }

    /// <summary>Navigation property to measurement point.</summary>
    [ForeignKey(nameof(PointId))]
    public MeasurementPoint? MeasurementPoint { get; set; }

    /// <summary>
    /// Gets or sets the deserialized validation result.
    /// </summary>
    [NotMapped]
    public DataValidationResult ValidationResult
    {
        get => System.Text.Json.JsonSerializer.Deserialize<DataValidationResult>(ValidationResultJson) ?? new DataValidationResult();
        set => ValidationResultJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Adds a processing step to the audit trail.
    /// </summary>
    public void AddProcessingStep(ProcessingStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        step.DataSourceId = Id;
        step.StepOrder = ProcessingSteps.Count + 1;
        ProcessingSteps.Add(step);
    }
}
