using AAFSS.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AAFSS.Infrastructure.Import;

/// <summary>
/// Factory that selects the appropriate data importer based on file extension.
/// Supports CSV (.csv, .tsv, .txt, .dat) and Excel (.xlsx, .xls, .xlsm) formats.
/// </summary>
public class DataImportFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes the factory with a service provider for DI resolution.
    /// </summary>
    public DataImportFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets all supported file extension filters for use in file dialogs.
    /// Format: "CSV Files|*.csv;*.tsv|Excel Files|*.xlsx;*.xls|All Files|*.*"
    /// </summary>
    public static string FileFilter => string.Join("|",
        "CSV Files (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt",
        "Excel Files (*.xlsx;*.xls;*.xlsm)|*.xlsx;*.xls;*.xlsm",
        "All Files (*.*)|*.*");

    /// <summary>
    /// Gets all supported extensions as a flat array.
    /// </summary>
    public static string[] AllSupportedExtensions =>
        CsvDataImporter.SupportedExtensions
            .Concat(ExcelDataImporter.SupportedExtensions)
            .Distinct()
            .ToArray();

    /// <summary>
    /// Determines the appropriate importer type for a given file path.
    /// </summary>
    /// <param name="filePath">File path with extension.</param>
    /// <returns>The type of importer to use.</returns>
    /// <exception cref="NotSupportedException">If the file format is not supported.</exception>
    public static Type GetImporterType(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        if (CsvDataImporter.SupportedExtensions.Contains(ext))
            return typeof(CsvDataImporter);

        if (ExcelDataImporter.SupportedExtensions.Contains(ext))
            return typeof(ExcelDataImporter);

        throw new NotSupportedException(
            $"Unsupported file format: '{ext}'. Supported formats: {string.Join(", ", AllSupportedExtensions)}");
    }

    /// <summary>
    /// Gets a data preview for a file, automatically selecting the correct importer.
    /// </summary>
    /// <param name="filePath">Path to the data file.</param>
    /// <param name="maxPreviewRows">Maximum preview rows.</param>
    /// <returns>DataPreview with headers and sample data.</returns>
    public async Task<DataPreview> GetPreviewAsync(string filePath, int maxPreviewRows = 100)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        if (CsvDataImporter.SupportedExtensions.Contains(ext))
        {
            var importer = _serviceProvider.GetRequiredService<CsvDataImporter>();
            return await importer.GetPreviewAsync(filePath, maxPreviewRows);
        }

        if (ExcelDataImporter.SupportedExtensions.Contains(ext))
        {
            var importer = _serviceProvider.GetRequiredService<ExcelDataImporter>();
            return await importer.GetPreviewAsync(filePath, maxPreviewRows: maxPreviewRows);
        }

        throw new NotSupportedException($"Unsupported file format: '{ext}'.");
    }

    /// <summary>
    /// Reads the full data file, automatically selecting the correct importer.
    /// </summary>
    /// <param name="filePath">Path to the data file.</param>
    /// <param name="onProgress">Optional progress callback.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Data array, channel names, units, and sample rate.</returns>
    public async Task<(double[,] Data, string[] ChannelNames, string[] ChannelUnits, double SampleRate)> ReadFullAsync(
        string filePath,
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        if (CsvDataImporter.SupportedExtensions.Contains(ext))
        {
            var importer = _serviceProvider.GetRequiredService<CsvDataImporter>();
            return await importer.ReadFullAsync(filePath, skipHeader: true, onProgress: onProgress, ct: ct);
        }

        if (ExcelDataImporter.SupportedExtensions.Contains(ext))
        {
            var importer = _serviceProvider.GetRequiredService<ExcelDataImporter>();
            return await importer.ReadFullAsync(filePath, hasHeader: true, onProgress: onProgress, ct: ct);
        }

        throw new NotSupportedException($"Unsupported file format: '{ext}'.");
    }

    /// <summary>
    /// Checks whether a file format is supported.
    /// </summary>
    public static bool IsFormatSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;
        return CsvDataImporter.SupportedExtensions.Contains(ext) ||
               ExcelDataImporter.SupportedExtensions.Contains(ext);
    }
}
