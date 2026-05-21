using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Validation report for a compiled spectrum — assesses damage deviation from the target.
/// </summary>
public class ValidationReport
{
    /// <summary>Unique validation report identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent compiled spectrum identifier.</summary>
    [Required]
    public Guid SpectrumId { get; set; }

    /// <summary>Target damage value (typically 1.0).</summary>
    public double TargetD { get; set; } = 1.0;

    /// <summary>Actual calculated damage value.</summary>
    public double ActualD { get; set; }

    /// <summary>Absolute deviation |ActualD - TargetD|.</summary>
    public double Deviation { get; set; }

    /// <summary>Validation level (Green/Yellow/Red) based on deviation thresholds.</summary>
    public ValidationLevel Level { get; set; } = ValidationLevel.NotValidated;

    /// <summary>JSON serialized warning messages.</summary>
    [MaxLength(4000)]
    public string WarningsJson { get; set; } = "[]";

    /// <summary>Timestamp when validation was performed.</summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to parent spectrum.</summary>
    [ForeignKey(nameof(SpectrumId))]
    public CompiledSpectrum? Spectrum { get; set; }

    /// <summary>
    /// Gets or sets the warning messages.
    /// </summary>
    [NotMapped]
    public string[] Warnings
    {
        get => System.Text.Json.JsonSerializer.Deserialize<string[]>(WarningsJson) ?? Array.Empty<string>();
        set => WarningsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets a human-readable status indicator.
    /// </summary>
    public string GetStatusIndicator() => Level switch
    {
        ValidationLevel.Green => "✓",
        ValidationLevel.Yellow => "⚠",
        ValidationLevel.Red => "✗",
        _ => "?"
    };
}
