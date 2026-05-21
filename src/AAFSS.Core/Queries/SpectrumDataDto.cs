namespace AAFSS.Core.Queries;

/// <summary>
/// Spectrum data payload returned by <see cref="GetSpectrumDataQuery"/>.
/// Contains the frequency-amplitude pairs and associated metadata
/// for chart rendering and data export.
/// </summary>
public record SpectrumDataDto
{
    /// <summary>Unique spectrum/result identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Human-readable name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Spectrum category (Base, Severe, Envelope, etc.).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Spectrum type (1/3 Octave, PSD, etc.).</summary>
    public string SpectrumType { get; init; } = string.Empty;

    /// <summary>Frequency array in Hz.</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>Amplitude/level array (dB SPL or PSD units).</summary>
    public double[] Amplitudes { get; init; } = Array.Empty<double>();

    /// <summary>Overall Sound Pressure Level in dB.</summary>
    public double Oaspl { get; init; }

    /// <summary>Cumulative damage value (for compiled spectra only).</summary>
    public double? DamageValue { get; init; }

    /// <summary>Validation status (for compiled spectra only).</summary>
    public string? ValidationStatus { get; init; }

    /// <summary>Timestamp when the spectrum was computed/compiled.</summary>
    public DateTime ComputedAt { get; init; }

    /// <summary>Number of frequency bins.</summary>
    public int BinCount => Frequencies.Length;
}
