using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="ComputeSpectrumCommand"/> by routing to the appropriate
/// frequency analysis method based on the requested spectrum type.
/// </summary>
public class ComputeSpectrumCommandHandler : IRequestHandler<ComputeSpectrumCommand, SpectrumResult>
{
    private readonly IFrequencyAnalysisService _freqService;
    private readonly IMediator _mediator;
    private readonly ILogger<ComputeSpectrumCommandHandler> _logger;

    public ComputeSpectrumCommandHandler(
        IFrequencyAnalysisService freqService,
        IMediator mediator,
        ILogger<ComputeSpectrumCommandHandler> logger)
    {
        _freqService = freqService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<SpectrumResult> Handle(ComputeSpectrumCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Computing spectrum: DataSourceId={DataSourceId}, Type={SpectrumType}",
            request.DataSourceId, request.SpectrumType);

        SpectrumResult result = request.SpectrumType switch
        {
            SpectrumType.PsdWelch or SpectrumType.PsdPeriodogram =>
                await _freqService.ComputePsdAsync(
                    request.DataSourceId,
                    request.SpectrumType,
                    request.FrequencyRange,
                    cancellationToken),

            SpectrumType.Octave1_1 or SpectrumType.Octave1_3
                or SpectrumType.Octave1_6 or SpectrumType.Octave1_12 =>
                await _freqService.ComputeOctaveBandsAsync(
                    request.DataSourceId,
                    request.SpectrumType,
                    cancellationToken),

            SpectrumType.CrossSpectrum when request.CrossDataSourceId.HasValue =>
                await _freqService.ComputeCrossSpectrumAsync(
                    request.DataSourceId,
                    request.CrossDataSourceId.Value,
                    cancellationToken),

            SpectrumType.Coherence when request.CrossDataSourceId.HasValue =>
                await _freqService.ComputeCoherenceAsync(
                    request.DataSourceId,
                    request.CrossDataSourceId.Value,
                    cancellationToken),

            SpectrumType.ZoomFft when request.FrequencyRange is not null =>
                await _freqService.ComputeZoomFftAsync(
                    request.DataSourceId,
                    request.FrequencyRange,
                    cancellationToken),

            SpectrumType.ZoomFft =>
                throw new ArgumentException("ZoomFFT requires a FrequencyRange to be specified."),

            SpectrumType.CrossSpectrum or SpectrumType.Coherence =>
                throw new ArgumentException(
                    $"Spectrum type {request.SpectrumType} requires CrossDataSourceId to be specified."),

            _ => throw new ArgumentOutOfRangeException(
                nameof(request.SpectrumType), request.SpectrumType, "Unsupported spectrum type.")
        };

        _logger.LogInformation("Spectrum computed: DataSourceId={DataSourceId}, ResultId={ResultId}, " +
            "Type={SpectrumType}, Bins={BinCount}, OASPL={Oaspl:F2}dB",
            request.DataSourceId, result.Id, result.SpectrumType, result.BinCount, result.Oaspl);

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = request.DataSourceId,
            ResultEntityId = result.Id,
            OperationType = $"ComputeSpectrum.{request.SpectrumType}",
            Status = ProcessingStatus.Completed,
            Success = true
        }, cancellationToken);

        return result;
    }
}
