namespace AAFSS.PluginContracts;

/// <summary>
/// Input/output data structure for algorithm plugins.
/// </summary>
public class SpectrumData
{
    /// <summary>Frequency array in Hz.</summary>
    public double[] Frequencies { get; set; } = Array.Empty<double>();

    /// <summary>Amplitude/Level array (dB SPL or PSD).</summary>
    public double[] Amplitudes { get; set; } = Array.Empty<double>();

    /// <summary>Overall Sound Pressure Level in dB.</summary>
    public double Oaspl { get; set; }

    /// <summary>Metadata dictionary for algorithm-specific parameters.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result structure for data import plugins.
/// </summary>
public class DataImportResult
{
    /// <summary>Whether the import was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Number of channels imported.</summary>
    public int ChannelCount { get; set; }

    /// <summary>Total number of sample points imported.</summary>
    public long SampleCount { get; set; }

    /// <summary>Sample rate in Hz.</summary>
    public double SampleRate { get; set; }

    /// <summary>Duration of the data in seconds.</summary>
    public double Duration { get; set; }

    /// <summary>Channel names.</summary>
    public string[] ChannelNames { get; set; } = Array.Empty<string>();

    /// <summary>Channel units.</summary>
    public string[] ChannelUnits { get; set; } = Array.Empty<string>();

    /// <summary>The imported data arrays (one per channel).</summary>
    public double[][] Data { get; set; } = Array.Empty<double[]>();

    /// <summary>Error message if import failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Validation warnings encountered during import.</summary>
    public string[] Warnings { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Interface for algorithm plugins that transform spectrum data.
/// </summary>
public interface IAlgorithmPlugin
{
    /// <summary>
    /// Gets the metadata for this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Executes the algorithm on the given input data.
    /// </summary>
    /// <param name="input">Input spectrum data.</param>
    /// <returns>Transformed spectrum data.</returns>
    SpectrumData Execute(SpectrumData input);
}

/// <summary>
/// Interface for data source plugins that import data from various formats/devices.
/// </summary>
public interface IDataSourcePlugin
{
    /// <summary>
    /// Gets the metadata for this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Checks whether this plugin supports the given file extension.
    /// </summary>
    /// <param name="extension">File extension (e.g., ".csv", ".tdms").</param>
    /// <returns>True if the format is supported.</returns>
    bool SupportsFormat(string extension);

    /// <summary>
    /// Imports data from the specified file path.
    /// </summary>
    /// <param name="filePath">Absolute path to the data file.</param>
    /// <returns>Import result containing parsed data.</returns>
    DataImportResult Import(string filePath);
}

/// <summary>
/// Interface for view plugins that add custom UI panels or controls.
/// </summary>
public interface IViewPlugin
{
    /// <summary>
    /// Gets the metadata for this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Gets the name of the view this plugin provides.
    /// </summary>
    string ViewName { get; }

    /// <summary>
    /// Creates an instance of the view.
    /// </summary>
    /// <returns>A WPF FrameworkElement for the view.</returns>
    object CreateView();

    /// <summary>
    /// Creates the ViewModel for this view.
    /// </summary>
    /// <returns>ViewModel instance.</returns>
    object CreateViewModel();
}

/// <summary>
/// Interface for report template plugins that provide custom report formats.
/// </summary>
public interface IReportTemplatePlugin
{
    /// <summary>
    /// Gets the metadata for this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Gets the name of this report template.
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// Gets the standard this template complies with (e.g., "GJB 67.13-90").
    /// </summary>
    string Standard { get; }

    /// <summary>
    /// Gets the file path to the template .docx file.
    /// </summary>
    string TemplateFilePath { get; }

    /// <summary>
    /// Gets the list of placeholder keys used in this template.
    /// </summary>
    string[] Placeholders { get; }

    /// <summary>
    /// Gets the list of chart placeholders used in this template.
    /// </summary>
    string[] ChartPlaceholders { get; }
}
