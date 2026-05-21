using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to compile multiple source spectra into a single compiled fatigue spectrum
/// using the specified compilation method (StateRegionEnvelope, MinerEquivalent,
/// FlightByFlight, MaxEnvelope, StatisticalExtreme).
/// Delegates to ISpectrumCompilationService and publishes SpectrumCompiledEvent.
/// </summary>
public record CompileSpectrumCommand(
    Guid ProjectId,
    string SpectrumName,
    CompilationMethod Method,
    List<Guid> SourceSpectrumIds,
    double EnvelopeOffset = 0
) : IRequest<CompiledSpectrum>;
