using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="RainflowCountCommand"/> by invoking the time-domain
/// rainflow counting service and publishing completion events.
/// </summary>
public class RainflowCountCommandHandler : IRequestHandler<RainflowCountCommand, RainflowResult>
{
    private readonly ITimeDomainAnalysisService _timeDomainService;
    private readonly IMediator _mediator;
    private readonly ILogger<RainflowCountCommandHandler> _logger;

    public RainflowCountCommandHandler(
        ITimeDomainAnalysisService timeDomainService,
        IMediator mediator,
        ILogger<RainflowCountCommandHandler> logger)
    {
        _timeDomainService = timeDomainService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<RainflowResult> Handle(RainflowCountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting rainflow counting: DataSourceId={DataSourceId}, Channel={ChannelIndex}",
            request.DataSourceId, request.ChannelIndex);

        var result = await _timeDomainService.RainflowCountAsync(
            request.DataSourceId,
            request.ChannelIndex,
            cancellationToken);

        _logger.LogInformation("Rainflow counting completed: ResultId={ResultId}, TotalCycles={TotalCycles}, " +
            "MaxAmplitude={MaxAmplitude:F2}, Bins={BinCount}",
            result.Id, result.TotalCycles, result.MaxAmplitude, result.BinCount);

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = request.DataSourceId,
            ResultEntityId = result.Id,
            OperationType = "RainflowCount",
            Status = ProcessingStatus.Completed,
            Success = true
        }, cancellationToken);

        return result;
    }
}
