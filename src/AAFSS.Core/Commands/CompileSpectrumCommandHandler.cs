using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="CompileSpectrumCommand"/> by invoking the spectrum compilation
/// service and publishing domain events for UI refresh and downstream processing.
/// </summary>
public class CompileSpectrumCommandHandler : IRequestHandler<CompileSpectrumCommand, CompiledSpectrum>
{
    private readonly ISpectrumCompilationService _compilationService;
    private readonly IMediator _mediator;
    private readonly ILogger<CompileSpectrumCommandHandler> _logger;

    public CompileSpectrumCommandHandler(
        ISpectrumCompilationService compilationService,
        IMediator mediator,
        ILogger<CompileSpectrumCommandHandler> logger)
    {
        _compilationService = compilationService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<CompiledSpectrum> Handle(CompileSpectrumCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceSpectrumIds == null || request.SourceSpectrumIds.Count == 0)
            throw new ArgumentException("At least one source spectrum ID is required.", nameof(request.SourceSpectrumIds));

        _logger.LogInformation("Compiling spectrum: ProjectId={ProjectId}, Name={SpectrumName}, " +
            "Method={Method}, SourceCount={SourceCount}, EnvelopeOffset={EnvelopeOffset}",
            request.ProjectId, request.SpectrumName, request.Method,
            request.SourceSpectrumIds.Count, request.EnvelopeOffset);

        var spectrum = await _compilationService.CompileAsync(
            request.ProjectId,
            request.SpectrumName,
            request.Method,
            request.SourceSpectrumIds,
            request.EnvelopeOffset,
            cancellationToken);

        _logger.LogInformation("Spectrum compiled: SpectrumId={SpectrumId}, Category={Category}, " +
            "D={DamageValue:F6}, OASPL={Oaspl:F2}dB",
            spectrum.Id, spectrum.Category, spectrum.DamageValue, spectrum.Oaspl);

        await _mediator.Publish(new SpectrumCompiledEvent
        {
            ProjectId = request.ProjectId,
            SpectrumId = spectrum.Id,
            SpectrumName = spectrum.Name,
            Category = spectrum.Category,
            SpectrumType = spectrum.SpectrumType,
            Method = spectrum.Method,
            DamageValue = spectrum.DamageValue,
            Oaspl = spectrum.Oaspl,
            SourceCount = request.SourceSpectrumIds.Count,
            CompiledAt = spectrum.CompiledAt
        }, cancellationToken);

        return spectrum;
    }
}
