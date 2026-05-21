using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to apply signal preprocessing (filtering, detrending, outlier removal) to a data source.
/// </summary>
public record PreprocessSignalCommand : IRequest<ProcessingResult>
{
    /// <summary>Target data source ID.</summary>
    public Guid DataSourceId { get; init; }

    /// <summary>Operation type: "Filter", "Detrend", "Decimate", "Calibrate", "RemoveOutliers".</summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>Filter type when OperationType is "Filter" (e.g., "lowpass", "bandpass").</summary>
    public string? FilterType { get; init; }

    /// <summary>Operation-specific parameters as key-value pairs.</summary>
    public Dictionary<string, double> Parameters { get; init; } = new();

    /// <summary>Channel index to process (default 0).</summary>
    public int ChannelIndex { get; init; } = 0;
}
