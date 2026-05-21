using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve time series data with optional downsampling for chart display.
/// </summary>
public record GetTimeSeriesDataQuery : IRequest<TimeSeriesDataResult>
{
    /// <summary>Data source ID.</summary>
    public Guid DataSourceId { get; init; }

    /// <summary>Channel index to retrieve (default 0).</summary>
    public int ChannelIndex { get; init; } = 0;

    /// <summary>Start time in seconds (null = from beginning).</summary>
    public double? StartTime { get; init; }

    /// <summary>End time in seconds (null = to end).</summary>
    public double? EndTime { get; init; }

    /// <summary>Maximum number of points to return (null = all). Used for display optimization.</summary>
    public int? MaxPoints { get; init; }
}

/// <summary>
/// Result of a time series data query, including the data points and metadata.
/// </summary>
public record TimeSeriesDataResult
{
    /// <summary>Time values in seconds.</summary>
    public double[] Timestamps { get; init; } = Array.Empty<double>();

    /// <summary>Time values (alias for Timestamps, used by views).</summary>
    public double[] TimeValues => Timestamps;

    /// <summary>Amplitude values for all channels (interleaved: [ch0_t0, ch1_t0, ch0_t1, ch1_t1, ...]).</summary>
    public double[] Values { get; init; } = Array.Empty<double>();

    /// <summary>Sample rate in Hz.</summary>
    public double SampleRate { get; init; }

    /// <summary>Name of the primary channel.</summary>
    public string ChannelName { get; init; } = string.Empty;

    /// <summary>Number of channels in the data.</summary>
    public int ChannelCount { get; init; }

    /// <summary>Channel names.</summary>
    public string[] ChannelNames { get; init; } = Array.Empty<string>();

    /// <summary>Physical unit.</summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>Data source name for display.</summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>Whether the data was downsampled for display.</summary>
    public bool IsDownsampled { get; init; }

    /// <summary>Original number of points before downsampling.</summary>
    public long OriginalPointCount { get; init; }
}
