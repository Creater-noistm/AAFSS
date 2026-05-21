using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a mission profile — a specific flight phase with defined operational parameters.
/// Each profile can contain multiple flight conditions, measurement points, and data sources.
/// </summary>
public class MissionProfile
{
    /// <summary>Unique profile identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent project identifier.</summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable profile name (e.g., "起飞-全加力").</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile type classification.</summary>
    public MissionProfileType Type { get; set; } = MissionProfileType.Standard;

    /// <summary>Serialized profile parameters (JSON).</summary>
    [MaxLength(4000)]
    public string ParametersJson { get; set; } = "{}";

    /// <summary>Total weight percentage of all conditions (should sum to 100).</summary>
    public double TotalWeight { get; set; }

    /// <summary>Associated flight conditions.</summary>
    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    /// <summary>Flight conditions within this profile.</summary>
    public List<FlightCondition> Conditions { get; set; } = new();

    /// <summary>Measurement points defined for this profile.</summary>
    public List<MeasurementPoint> Points { get; set; } = new();

    /// <summary>Data sources associated with this profile.</summary>
    public List<DataSource> DataSources { get; set; } = new();

    /// <summary>
    /// Gets or sets the deserialized profile parameters.
    /// </summary>
    [NotMapped]
    public ProfileParameters Parameters
    {
        get => System.Text.Json.JsonSerializer.Deserialize<ProfileParameters>(ParametersJson) ?? new ProfileParameters();
        set => ParametersJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Validates that all condition weights sum to approximately 100%.
    /// </summary>
    /// <returns>True if weights are valid within a 0.1% tolerance.</returns>
    public bool ValidateWeights()
    {
        var sum = Conditions.Sum(c => c.Weight);
        return Math.Abs(sum - 100.0) < 0.1;
    }
}
