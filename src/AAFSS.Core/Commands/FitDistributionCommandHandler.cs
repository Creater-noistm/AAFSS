using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="FitDistributionCommand"/> by fitting either a specific
/// distribution or automatically selecting the best-fit distribution.
/// </summary>
public class FitDistributionCommandHandler : IRequestHandler<FitDistributionCommand, StatisticalModel>
{
    private readonly IStatisticalModelingService _statsService;
    private readonly IMediator _mediator;
    private readonly ILogger<FitDistributionCommandHandler> _logger;

    public FitDistributionCommandHandler(
        IStatisticalModelingService statsService,
        IMediator mediator,
        ILogger<FitDistributionCommandHandler> logger)
    {
        _statsService = statsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<StatisticalModel> Handle(FitDistributionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fitting distribution: RainflowResultId={RainflowResultId}, Distribution={DistributionType}",
            request.RainflowResultId, request.DistributionType?.ToString() ?? "BestFit");

        StatisticalModel model;

        if (request.DistributionType.HasValue)
        {
            model = await _statsService.FitDistributionAsync(
                request.RainflowResultId,
                request.DistributionType.Value,
                cancellationToken);
        }
        else
        {
            model = await _statsService.FitBestDistributionAsync(
                request.RainflowResultId,
                cancellationToken);
        }

        _logger.LogInformation("Distribution fit completed: ModelId={ModelId}, " +
            "Distribution={DistributionType}, K-S={KsStatistic:F4}, AIC={AicValue:F2}, GoF={GoodnessOfFit:F3}",
            model.Id, model.DistributionType, model.KsStatistic, model.AicValue, model.GoodnessOfFit);

        await _mediator.Publish(new ProcessingCompletedEvent
        {
            DataSourceId = model.RainflowResultId,
            ResultEntityId = model.Id,
            OperationType = $"FitDistribution.{model.DistributionType}",
            Status = ProcessingStatus.Completed,
            Success = true
        }, cancellationToken);

        return model;
    }
}
