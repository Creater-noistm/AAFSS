using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for importing measurement data from various file formats.
/// Handles preview, validation, and ingestion into the HDF5 data store.
/// </summary>
public interface IDataImportService
{
    /// <summary>Gets a preview of data in a file without fully importing it.</summary>
    Task<DataPreview> GetPreviewAsync(string filePath, int maxPreviewRows = 100, CancellationToken ct = default);

    /// <summary>Validates a potential data file before import.</summary>
    Task<DataValidationResult> ValidateAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Imports a data file into the project's HDF5 store.
    /// Returns the populated DataSource entity with TimeSeriesData reference.
    /// </summary>
    Task<DataSource> ImportAsync(
        Guid projectId,
        Guid profileId,
        string filePath,
        Guid? measurementPointId = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>Gets the list of supported file format extensions.</summary>
    string[] GetSupportedFormats();
}
