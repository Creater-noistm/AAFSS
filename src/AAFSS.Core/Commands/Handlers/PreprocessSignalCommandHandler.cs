using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands.Handlers;

public class PreprocessSignalCommandHandler : IRequestHandler<PreprocessSignalCommand, ProcessingResult>
{
    private readonly ISignalProcessingService _signalService;
    private readonly IMediator _mediator;
    private readonly ILogger<PreprocessSignalCommandHandler> _logger;

    public PreprocessSignalCommandHandler(ISignalProcessingService signalService, IMediator mediator, ILogger<PreprocessSignalCommandHandler> logger)
    {
        _signalService = signalService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ProcessingResult> Handle(PreprocessSignalCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Preprocessing signal for data source {Id}, operation {Op}", request.DataSourceId, request.OperationType);

        ProcessingResult result = request.OperationType switch
        {
            "Filter" => await _signalService.ApplyFilterAsync(request.DataSourceId, request.FilterType ?? "lowpass", request.Parameters, cancellationToken),
            "Detrend" => await _signalService.DetrendAsync(request.DataSourceId, cancellationToken),
            "Decimate" => request.Parameters.TryGetValue("factor", out var f)
                ? await _signalService.DecimateAsync(request.DataSourceId, (int)f, cancellationToken)
                : new ProcessingResult { Success = false, ErrorMessage = "Decimate requires 'factor' parameter" },
            "Calibrate" => await _signalService.ApplyCalibrationAsync(
                request.DataSourceId,
                request.Parameters.GetValueOrDefault("sensitivity", 1.0),
                request.Parameters.GetValueOrDefault("offset", 0),
                cancellationToken),
            _ => new ProcessingResult { Success = false, ErrorMessage = $"Unknown operation type: {request.OperationType}" }
        };

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = request.DataSourceId,
            ProcessingStepId = result.ProcessingStepId ?? Guid.Empty,
            OperationType = request.OperationType,
            DurationMs = result.DurationMs,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        }, cancellationToken);

        return result;
    }
}
