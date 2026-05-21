using MediatR;

namespace AAFSS.Core.Events;

/// <summary>
/// Domain event raised when data has been successfully imported into the system.
/// </summary>
public record DataImportedEvent : INotification
{
    /// <summary>ID of the created DataSource entity.</summary>
    public Guid DataSourceId { get; init; }

    /// <summary>Parent project ID.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Parent mission profile ID.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Original file path.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Timestamp of import.</summary>
    public DateTime ImportedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Number of data points imported.</summary>
    public long DataPointCount { get; init; }

    /// <summary>Sample rate of the imported data.</summary>
    public double SampleRate { get; init; }

    /// <summary>Data file format (e.g., "csv", "tdms").</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Number of channels detected in the data.</summary>
    public int ChannelCount { get; init; }
}
