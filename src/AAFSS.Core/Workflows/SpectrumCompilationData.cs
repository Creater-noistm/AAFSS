namespace AAFSS.Core.Workflows;

/// <summary>
/// Data context carried through the spectrum compilation workflow.
/// Tracks the state of each pipeline stage and accumulates results.
/// </summary>
public class SpectrumCompilationData
{
    /// <summary>Parent project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Mission profile identifier for data import.</summary>
    public Guid ProfileId { get; set; }

    /// <summary>Path to the input data file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Optional measurement point identifier.</summary>
    public Guid? MeasurementPointId { get; set; }

    /// <summary>Compilation method to use.</summary>
    public Models.CompilationMethod CompilationMethod { get; set; } = Models.CompilationMethod.StateRegionEnvelope;

    /// <summary>Envelope offset in dB.</summary>
    public double EnvelopeOffset { get; set; }

    /// <summary>Name for the compiled spectrum output.</summary>
    public string SpectrumName { get; set; } = string.Empty;

    /// <summary>Report template name.</summary>
    public string ReportTemplateName { get; set; } = "GJB_67_13_90";

    /// <summary>Output directory for the generated report.</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    // --- Pipeline stage results ---

    /// <summary>ID of the imported data source.</summary>
    public Guid DataSourceId { get; set; }

    /// <summary>ID of the computed spectrum result.</summary>
    public Guid SpectrumResultId { get; set; }

    /// <summary>ID of the rainflow result.</summary>
    public Guid RainflowResultId { get; set; }

    /// <summary>ID of the fitted statistical model.</summary>
    public Guid StatisticalModelId { get; set; }

    /// <summary>ID of the compiled spectrum.</summary>
    public Guid CompiledSpectrumId { get; set; }

    /// <summary>ID of the validation report.</summary>
    public Guid ValidationReportId { get; set; }

    /// <summary>ID of the generated report.</summary>
    public Guid GeneratedReportId { get; set; }

    /// <summary>Error message if any stage fails.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Overall workflow status.</summary>
    public Models.ProcessingStatus Status { get; set; } = Models.ProcessingStatus.Pending;
}
