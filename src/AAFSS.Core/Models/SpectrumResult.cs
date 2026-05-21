using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a computed spectrum result from frequency domain analysis.
/// Stores frequency-amplitude pairs and analysis configuration metadata.
/// </summary>
public class SpectrumResult
{
    /// <summary>Unique spectrum result identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent data source identifier.</summary>
    [Required]
    public Guid DataSourceId { get; set; }

    /// <summary>Type of spectrum analysis performed.</summary>
    public SpectrumType SpectrumType { get; set; } = SpectrumType.Octave1_3;

    /// <summary>JSON serialized frequency array (Hz).</summary>
    [MaxLength]
    public string FrequenciesJson { get; set; } = "[]";

    /// <summary>JSON serialized amplitude array (dB SPL or PSD).</summary>
    [MaxLength]
    public string AmplitudesJson { get; set; } = "[]";

    /// <summary>Overall Sound Pressure Level in dB.</summary>
    public double Oaspl { get; set; }

    /// <summary>Window function used for PSD analysis.</summary>
    [MaxLength(64)]
    public string WindowType { get; set; } = "Hanning";

    /// <summary>FFT size (number of points).</summary>
    public int FftSize { get; set; } = 4096;

    /// <summary>Overlap ratio for Welch method (0.0-1.0).</summary>
    public double OverlapRatio { get; set; } = 0.5;

    /// <summary>Timestamp when the spectrum was computed.</summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to parent data source.</summary>
    [ForeignKey(nameof(DataSourceId))]
    public DataSource? DataSource { get; set; }

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
    /// Gets or sets the amplitude/level array.
    /// </summary>
    [NotMapped]
    public double[] Amplitudes
    {
        get => System.Text.Json.JsonSerializer.Deserialize<double[]>(AmplitudesJson) ?? Array.Empty<double>();
        set => AmplitudesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets the number of frequency bins in this spectrum.
    /// </summary>
    [NotMapped]
    public int BinCount => Frequencies.Length;
}
