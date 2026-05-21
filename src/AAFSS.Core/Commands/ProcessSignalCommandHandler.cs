using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="ProcessSignalCommand"/> by routing to the appropriate
/// signal processing method and publishing completion events.
/// </summary>
public class ProcessSignalCommandHandler : IRequestHandler<ProcessSignalCommand, ProcessingResult>
{
    private readonly ISignalProcessingService _signalService;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessSignalCommandHandler> _logger;

    public ProcessSignalCommandHandler(
        ISignalProcessingService signalService,
        IMediator mediator,
        ILogger<ProcessSignalCommandHandler> logger)
    {
        _signalService = signalService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ProcessingResult> Handle(ProcessSignalCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing signal: DataSourceId={DataSourceId}, Operation={OperationType}",
            request.DataSourceId, request.OperationType);

        var parameters = request.Parameters;

        ProcessingResult result = request.OperationType switch
        {
            SignalOperationType.ApplyFilter => await _signalService.ApplyFilterAsync(
                request.DataSourceId,
                "lowpass",
                parameters,
                cancellationToken),

            SignalOperationType.Detrend => await _signalService.DetrendAsync(
                request.DataSourceId,
                cancellationToken),

            SignalOperationType.Decimate => await _signalService.DecimateAsync(
                request.DataSourceId,
                (int)GetRequiredParam(parameters, "factor"),
                cancellationToken),

            SignalOperationType.ApplyCalibration => await _signalService.ApplyCalibrationAsync(
                request.DataSourceId,
                GetRequiredParam(parameters, "sensitivity"),
                parameters.GetValueOrDefault("offset", 0),
                cancellationToken),

            _ => throw new ArgumentException($"Unknown signal operation type: {request.OperationType}")
        };

        var status = result.Success ? ProcessingStatus.Completed : ProcessingStatus.Failed;

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = request.DataSourceId,
            ProcessingStepId = result.ProcessingStepId ?? Guid.Empty,
            OperationType = request.OperationType.ToString(),
            Status = status,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            DurationMs = result.DurationMs,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Signal processing failed: DataSourceId={DataSourceId}, Operation={OperationType}, Error={Error}",
                request.DataSourceId, request.OperationType, result.ErrorMessage);
        }

        return result;
    }

    private static double GetRequiredParam(Dictionary<string, double> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
            return value;
        throw new ArgumentException($"Required parameter '{key}' not provided for signal processing operation.");
    }
}
