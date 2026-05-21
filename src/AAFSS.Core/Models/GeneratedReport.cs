using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a generated Word/PDF report for the project.
/// Tracks the template used, file path, status, and included spectra.
/// </summary>
public class GeneratedReport
{
    /// <summary>Unique report identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent project identifier.</summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>Template name used for generation (e.g., "GJB_67_13_90").</summary>
    [Required]
    [MaxLength(256)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>Output file path of the generated report.</summary>
    [MaxLength(1024)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Timestamp when the report was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Current report status.</summary>
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    /// <summary>JSON serialized list of included spectrum IDs.</summary>
    [MaxLength(4000)]
    public string IncludedSpectrumIdsJson { get; set; } = "[]";

    /// <summary>Error message if generation failed.</summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>Navigation property to parent project.</summary>
    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    /// <summary>
    /// Gets or sets the included spectrum IDs.
    /// </summary>
    [NotMapped]
    public List<Guid> IncludedSpectrumIds
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(IncludedSpectrumIdsJson) ?? new List<Guid>();
        set => IncludedSpectrumIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
