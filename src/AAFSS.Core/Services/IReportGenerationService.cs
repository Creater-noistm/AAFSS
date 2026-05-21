using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for generating spectrum reports in GJB and custom formats.
/// Supports DOCX output with embedded charts, tables, and metadata.
/// </summary>
public interface IReportGenerationService
{
    /// <summary>Generates a report from a compiled spectrum using the specified template.</summary>
    Task<GeneratedReport> GenerateReportAsync(
        Guid projectId,
        Guid spectrumId,
        string templateName,
        string outputDirectory,
        CancellationToken ct = default);

    /// <summary>Generates a batch report covering multiple spectra.</summary>
    Task<GeneratedReport> GenerateBatchReportAsync(
        Guid projectId,
        List<Guid> spectrumIds,
        string templateName,
        string outputDirectory,
        CancellationToken ct = default);

    /// <summary>Gets the list of available report templates.</summary>
    Task<string[]> GetAvailableTemplatesAsync(CancellationToken ct = default);

    /// <summary>Gets the status of a report generation task.</summary>
    Task<ReportStatus> GetReportStatusAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>Exports chart images from a compiled spectrum.</summary>
    Task<string[]> ExportChartsAsync(Guid spectrumId, string outputDirectory, CancellationToken ct = default);
}
