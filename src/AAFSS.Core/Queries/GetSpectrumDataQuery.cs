using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve spectrum data (frequencies, amplitudes, metadata)
/// for chart rendering and analysis. Supports both spectrum results
/// (from frequency analysis) and compiled spectra (from compilation).
/// </summary>
public record GetSpectrumDataQuery : IRequest<SpectrumDataDto?>
{
    /// <summary>The project ID containing the spectrum.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>The spectrum ID to retrieve.</summary>
    public Guid SpectrumId { get; init; }

    /// <summary>Whether the spectrum is compiled (true) or a raw result (false).</summary>
    public bool IsCompiled { get; init; }
}
