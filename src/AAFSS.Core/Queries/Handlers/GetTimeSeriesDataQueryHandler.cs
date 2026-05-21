using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries.Handlers;

/// <summary>
/// Handles time series data queries, with optional downsampling for display performance.
/// </summary>
public class GetTimeSeriesDataQueryHandler : IRequestHandler<GetTimeSeriesDataQuery, TimeSeriesDataResult>
{
    private readonly IQueryDataService _queryService;
    private readonly ILogger<GetTimeSeriesDataQueryHandler> _logger;

    public GetTimeSeriesDataQueryHandler(IQueryDataService queryService, ILogger<GetTimeSeriesDataQueryHandler> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    public async Task<TimeSeriesDataResult> Handle(GetTimeSeriesDataQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving time series data for data source {DataSourceId}, channel {ChannelIndex}",
            query.DataSourceId, query.ChannelIndex);

        var dataSource = await _queryService.GetDataSourceAsync(query.DataSourceId, cancellationToken);

        // For time series data, we'd normally read from HDF5.
        // For now, return a placeholder with metadata from the time series reference.
        var tsData = dataSource.TimeSeriesData;
        if (tsData == null)
        {
            _logger.LogWarning("No time series data found for data source {DataSourceId}", query.DataSourceId);
            return new TimeSeriesDataResult
            {
                ChannelName = "Unknown",
                Unit = "",
                SampleRate = 0,
                IsDownsampled = false,
                OriginalPointCount = 0
            };
        }

        var channelNames = tsData.ChannelNames;
        var channelUnits = tsData.ChannelUnits;
        var channelName = query.ChannelIndex < channelNames.Length
            ? channelNames[query.ChannelIndex]
            : $"Channel {query.ChannelIndex + 1}";
        var unit = query.ChannelIndex < channelUnits.Length
            ? channelUnits[query.ChannelIndex]
            : string.Empty;

        return new TimeSeriesDataResult
        {
            Timestamps = Array.Empty<double>(),
            Values = Array.Empty<double>(),
            SampleRate = tsData.SampleRate,
            ChannelName = channelName,
            Unit = unit,
            IsDownsampled = false,
            OriginalPointCount = tsData.SampleCount
        };
    }
}
