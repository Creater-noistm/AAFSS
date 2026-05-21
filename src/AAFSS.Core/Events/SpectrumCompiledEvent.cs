using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Events;

/// <summary>
/// Domain event raised when a compiled spectrum has been created or updated.
/// </summary>
public record SpectrumCompiledEvent : INotification
{
    /// <summary>ID of the compiled spectrum.</summary>
    public Guid SpectrumId { get; init; }

    /// <summary>Parent project ID.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Spectrum name.</summary>
    public string SpectrumName { get; init; } = string.Empty;

    /// <summary>Spectrum category in the compilation hierarchy.</summary>
    public SpectrumCategory Category { get; init; }

    /// <summary>Compilation method used.</summary>
    public CompilationMethod Method { get; init; }

    /// <summary>Number of source spectra used.</summary>
    public int SourceCount { get; init; }

    /// <summary>Overall SPL in dB.</summary>
    public double Oaspl { get; init; }

    /// <summary>Calculated damage value.</summary>
    public double DamageValue { get; init; }

    /// <summary>Spectrum type (1/3 OCT, PSD, etc.).</summary>
    public Models.SpectrumType SpectrumType { get; init; }

    /// <summary>Timestamp when the spectrum was compiled.</summary>
    public DateTime CompiledAt { get; init; } = DateTime.UtcNow;
}
