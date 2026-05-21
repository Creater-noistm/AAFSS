using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to import measurement data from a file into the system.
/// Triggers validation, ingestion into HDF5, and creation of DataSource/TimeSeriesData entities.
/// </summary>
public record ImportDataCommand : IRequest<DataSource>
{
    /// <summary>Target project ID.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Target mission profile ID within the project.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Full path to the data file.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Optional measurement point to associate with the data.</summary>
    public Guid? MeasurementPointId { get; init; }

    /// <summary>Original file name for metadata tracking.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Optional progress reporter.</summary>
    public IProgress<double>? Progress { get; init; }
}
