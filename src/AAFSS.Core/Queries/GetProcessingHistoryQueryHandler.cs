using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries;

/// <summary>
/// Handles <see cref="GetProcessingHistoryQuery"/> by retrieving all processing
/// steps for a data source through the query data service.
/// </summary>
public class GetProcessingHistoryQueryHandler : IRequestHandler<GetProcessingHistoryQuery, List<ProcessingStep>>
{
    private readonly IQueryDataService _queryDataService;
    private readonly ILogger<GetProcessingHistoryQueryHandler> _logger;

    public GetProcessingHistoryQueryHandler(
        IQueryDataService queryDataService,
        ILogger<GetProcessingHistoryQueryHandler> logger)
    {
        _queryDataService = queryDataService;
        _logger = logger;
    }

    public async Task<List<ProcessingStep>> Handle(GetProcessingHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving processing history for DataSourceId={DataSourceId}", request.DataSourceId);

        var steps = await _queryDataService.GetProcessingStepsAsync(request.DataSourceId, cancellationToken);

        _logger.LogDebug("Retrieved {StepCount} processing steps for DataSourceId={DataSourceId}",
            steps.Count, request.DataSourceId);

        return steps;
    }
}
