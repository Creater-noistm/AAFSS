using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a compiled acoustic fatigue spectrum — the final product of the compilation pipeline.
/// Contains the frequency-level data, damage assessment, and validation status.
/// </summary>
public class CompiledSpectrum
{
    /// <summary>Unique compiled spectrum identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent project identifier.</summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable spectrum name.</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Spectrum category in the compilation hierarchy.</summary>
    public SpectrumCategory Category { get; set; } = SpectrumCategory.Base;

    /// <summary>Spectrum type (1/3 OCT, PSD, etc.).</summary>
    public SpectrumType SpectrumType { get; set; } = SpectrumType.Octave1_3;

    /// <summary>JSON serialized frequency array (Hz).</summary>
    [MaxLength]
    public string FrequenciesJson { get; set; } = "[]";

    /// <summary>JSON serialized level array (dB SPL or PSD).</summary>
    [MaxLength]
    public string LevelsJson { get; set; } = "[]";

    /// <summary>Calculated cumulative damage value (Miner's D).</summary>
    public double DamageValue { get; set; }

    /// <summary>Validation status after damage check.</summary>
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;

    /// <summary>Validation level (Green/Yellow/Red) from the latest validation.</summary>
    public ValidationLevel ValidationLevel { get; set; } = ValidationLevel.Green;

    /// <summary>JSON serialized list of source spectrum IDs used in compilation.</summary>
    [MaxLength(4000)]
    public string SourceSpectrumIdsJson { get; set; } = "[]";

    /// <summary>Compilation method used.</summary>
    public CompilationMethod Method { get; set; } = CompilationMethod.StateRegionEnvelope;

    /// <summary>Envelope offset applied in dB (0 = no offset).</summary>
    public double EnvelopeOffset { get; set; }

    /// <summary>Overall SPL in dB.</summary>
    public double Oaspl { get; set; }

    /// <summary>Timestamp when the spectrum was compiled.</summary>
    public DateTime CompiledAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to parent project.</summary>
    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    /// <summary>Associated validation report.</summary>
    public ValidationReport? ValidationReport { get; set; }

    /// <summary>
    /// Gets or sets the frequency array.
    /// </summary>
    [NotMapped]
    public double[] Frequencies
    {
        get => System.Text.Json.JsonSerializer.Deserialize<double[]>(FrequenciesJson) ?? Array.Empty<double>();
        set => FrequenciesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets or sets the level array.
    /// </summary>
    [NotMapped]
    public double[] Levels
    {
        get => System.Text.Json.JsonSerializer.Deserialize<double[]>(LevelsJson) ?? Array.Empty<double>();
        set => LevelsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets or sets the list of source spectrum IDs.
    /// </summary>
    [NotMapped]
    public List<Guid> SourceSpectrumIds
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(SourceSpectrumIdsJson) ?? new List<Guid>();
        set => SourceSpectrumIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
