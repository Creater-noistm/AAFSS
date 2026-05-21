using AAFSS.Core.Events;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands.Handlers;

public class ComputeRainflowCommandHandler : IRequestHandler<ComputeRainflowCommand, Guid>
{
    private readonly ITimeDomainAnalysisService _timeService;
    private readonly IMediator _mediator;
    private readonly ILogger<ComputeRainflowCommandHandler> _logger;

    public ComputeRainflowCommandHandler(ITimeDomainAnalysisService timeService, IMediator mediator, ILogger<ComputeRainflowCommandHandler> logger)
    {
        _timeService = timeService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid> Handle(ComputeRainflowCommand request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Computing rainflow for {Id}, bins={Bins}, meanStressCorrection={Mc}",
            request.TimeSeriesDataId, request.Bins, request.ApplyMeanStressCorrection);

        var result = await _timeService.RainflowCountAsync(request.TimeSeriesDataId, channelIndex: 0, ct);

        // TODO: ApplyMeanStressCorrection — 后续可通过 FatigueBridge.GoodmanCorrectionAsync
        //       对 result 的振幅做零均值修正后再持久化
        // TODO: request.Bins — 当前实现内部固定 64 bins，后续可扩展为参数化

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = request.TimeSeriesDataId,
            ResultEntityId = result.Id,
            OperationType = "RainflowCounting",
            Status = Models.ProcessingStatus.Completed,
            Success = true
        }, ct);
        return result.Id;
    }
}
