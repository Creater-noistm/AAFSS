using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Export;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of IReportGenerationService.
/// Generates DOCX reports with embedded spectrum charts, metadata tables, and validation data.
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private readonly ISpectrumRepository _spectrumRepo;
    private readonly ReportEngine _reportEngine;
    private readonly GjbReportBuilder _gjbBuilder;
    private readonly ChartToImageExporter _chartExporter;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        ISpectrumRepository spectrumRepo,
        ReportEngine reportEngine,
        GjbReportBuilder gjbBuilder,
        ChartToImageExporter chartExporter,
        ILogger<ReportGenerationService> logger)
    {
        _spectrumRepo = spectrumRepo;
        _reportEngine = reportEngine;
        _gjbBuilder = gjbBuilder;
        _chartExporter = chartExporter;
        _logger = logger;
    }

    public async Task<GeneratedReport> GenerateReportAsync(
        Guid projectId, Guid spectrumId, string templateName,
        string outputDirectory, CancellationToken ct = default)
    {
        var report = new GeneratedReport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            TemplateName = templateName,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            // Fetch spectrum and validation data
            var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct);
            if (spectrum == null)
            {
                report.Status = ReportStatus.Error;
                report.ErrorMessage = $"Spectrum not found: {spectrumId}";
                _logger.LogWarning("GenerateReport: spectrum {SpectrumId} not found", spectrumId);
                return report;
            }

            _logger.LogInformation("Generating report: Spectrum={Name}, Template={Template}",
                spectrum.Name, templateName);

            // Select builder based on template
            string filePath;
            if (templateName.Contains("GJB", StringComparison.OrdinalIgnoreCase))
            {
                filePath = await _gjbBuilder.BuildReportAsync(spectrum, outputDirectory, ct);
            }
            else
            {
                filePath = await _reportEngine.GenerateSpectrumReportAsync(
                    spectrum, templateName, outputDirectory, ct);
            }

            report.FilePath = filePath;
            report.Status = ReportStatus.Generated;
            report.IncludedSpectrumIds = new List<Guid> { spectrumId };

            _logger.LogInformation("Report generated: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            report.Status = ReportStatus.Error;
            report.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Report generation failed for spectrum {SpectrumId}", spectrumId);
        }

        return report;
    }

    public async Task<GeneratedReport> GenerateBatchReportAsync(
        Guid projectId, List<Guid> spectrumIds, string templateName,
        string outputDirectory, CancellationToken ct = default)
    {
        var report = new GeneratedReport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            TemplateName = templateName,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            var filePaths = new List<string>();
            var errors = new List<string>();

            foreach (var spectrumId in spectrumIds)
            {
                ct.ThrowIfCancellationRequested();

                var singleReport = await GenerateReportAsync(
                    projectId, spectrumId, templateName, outputDirectory, ct);

                if (singleReport.Status == ReportStatus.Generated && !string.IsNullOrEmpty(singleReport.FilePath))
                    filePaths.Add(singleReport.FilePath);
                else if (!string.IsNullOrEmpty(singleReport.ErrorMessage))
                    errors.Add($"{spectrumId}: {singleReport.ErrorMessage}");
            }

            report.FilePath = filePaths.Count > 0 ? filePaths[0] : string.Empty;
            report.Status = errors.Count == 0 ? ReportStatus.Generated : ReportStatus.Error;
            report.ErrorMessage = errors.Count > 0
                ? string.Join("; ", errors)
                : null;
            report.IncludedSpectrumIds = spectrumIds;

            _logger.LogInformation("Batch report generated: {Count} spectra, {Success} success, {Errors} errors",
                spectrumIds.Count, filePaths.Count, errors.Count);
        }
        catch (Exception ex)
        {
            report.Status = ReportStatus.Error;
            report.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Batch report generation failed");
        }

        return report;
    }

    public Task<string[]> GetAvailableTemplatesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new[] { "GJB_67_13_90", "GJB_150_16A", "Custom_Report" });
    }

    public Task<ReportStatus> GetReportStatusAsync(Guid reportId, CancellationToken ct = default)
    {
        // Status tracking would require a report repository; for now return Draft
        return Task.FromResult(ReportStatus.Draft);
    }

    public async Task<string[]> ExportChartsAsync(Guid spectrumId, string outputDirectory, CancellationToken ct = default)
    {
        try
        {
            var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct);
            if (spectrum == null)
            {
                _logger.LogWarning("ExportCharts: spectrum {SpectrumId} not found", spectrumId);
                return Array.Empty<string>();
            }

            var chartsDir = Path.Combine(outputDirectory, "charts");
            var spectrumChartPath = Path.Combine(chartsDir, $"spectrum_{spectrumId:N}.png");

            var exportedPaths = new List<string>();

            // Export spectrum chart
            var spectrumPng = await _chartExporter.ExportSpectrumChartAsync(spectrum, spectrumChartPath, ct);
            exportedPaths.Add(spectrumPng);

            _logger.LogInformation("Charts exported: {Count} files", exportedPaths.Count);
            return exportedPaths.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chart export failed for spectrum {SpectrumId}", spectrumId);
            return Array.Empty<string>();
        }
    }
}
