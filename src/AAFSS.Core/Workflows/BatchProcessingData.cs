namespace AAFSS.Core.Workflows;

/// <summary>
/// Data context for the batch processing workflow.
/// Carries a list of input file specifications and tracks
/// the processing status of each item in the batch.
/// </summary>
public class BatchProcessingData
{
    /// <summary>Parent project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Output directory for generated reports.</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>Report template name.</summary>
    public string ReportTemplateName { get; set; } = "GJB_67_13_90";

    /// <summary>List of batch items to process.</summary>
    public List<BatchItem> Items { get; set; } = new();

    /// <summary>Index of the currently processing item.</summary>
    public int CurrentIndex { get; set; }

    /// <summary>Number of items successfully processed.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of items that failed.</summary>
    public int FailureCount { get; set; }

    /// <summary>Overall batch processing status.</summary>
    public Models.ProcessingStatus Status { get; set; } = Models.ProcessingStatus.Pending;

    /// <summary>Error messages collected across all items.</summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>Whether there are more items to process.</summary>
    public bool HasMoreItems => CurrentIndex < Items.Count;
}

/// <summary>
/// A single item in a batch processing job.
/// </summary>
public class BatchItem
{
    /// <summary>Profile to associate the imported data with.</summary>
    public Guid ProfileId { get; set; }

    /// <summary>Path to the input data file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Name for the resulting compiled spectrum.</summary>
    public string SpectrumName { get; set; } = string.Empty;

    /// <summary>Optional measurement point identifier.</summary>
    public Guid? MeasurementPointId { get; set; }

    /// <summary>Status of this item's processing.</summary>
    public Models.ProcessingStatus Status { get; set; } = Models.ProcessingStatus.Pending;

    /// <summary>Error message if this item failed.</summary>
    public string? ErrorMessage { get; set; }
}
