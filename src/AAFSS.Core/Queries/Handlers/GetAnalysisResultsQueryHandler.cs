using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries.Handlers;

/// <summary>
/// Handles queries for analysis results, aggregating rainflow, statistical, and spectrum data.
/// </summary>
public class GetAnalysisResultsQueryHandler : IRequestHandler<GetAnalysisResultsQuery, AnalysisResultsDto>
{
    private readonly IQueryDataService _queryService;
    private readonly ILogger<GetAnalysisResultsQueryHandler> _logger;

    public GetAnalysisResultsQueryHandler(IQueryDataService queryService, ILogger<GetAnalysisResultsQueryHandler> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    public async Task<AnalysisResultsDto> Handle(GetAnalysisResultsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving analysis results for data source {DataSourceId}", query.DataSourceId);

        var dataSource = await _queryService.GetDataSourceAsync(query.DataSourceId, cancellationToken);

        return new AnalysisResultsDto
        {
            DataSourceId = dataSource.Id,
            RainflowResults = dataSource.RainflowResults,
            SpectrumResults = dataSource.SpectrumResults,
            StatisticalModels = dataSource.RainflowResults
                .SelectMany(r => r.StatisticalModels)
                .ToList(),
            HasRainflowResults = dataSource.RainflowResults.Count > 0,
            HasStatisticalModels = dataSource.RainflowResults.Any(r => r.StatisticalModels.Count > 0),
            HasSpectrumResults = dataSource.SpectrumResults.Count > 0,
            OverallStatus = DetermineOverallStatus(dataSource)
        };
    }

    private static ProcessingStatus DetermineOverallStatus(DataSource dataSource)
    {
        if (dataSource.ProcessingSteps.Count == 0)
            return ProcessingStatus.Pending;

        if (dataSource.ProcessingSteps.Any(s => s.Status == ProcessingStatus.Failed))
            return ProcessingStatus.Failed;

        if (dataSource.ProcessingSteps.Any(s => s.Status == ProcessingStatus.Running))
            return ProcessingStatus.Running;

        return ProcessingStatus.Completed;
    }
}
