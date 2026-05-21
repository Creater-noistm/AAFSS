using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="GenerateReportCommand"/> by generating a single-spectrum
/// or batch report through the report generation service.
/// </summary>
public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, GeneratedReport>
{
    private readonly IReportGenerationService _reportService;
    private readonly IMediator _mediator;
    private readonly ILogger<GenerateReportCommandHandler> _logger;

    public GenerateReportCommandHandler(
        IReportGenerationService reportService,
        IMediator mediator,
        ILogger<GenerateReportCommandHandler> logger)
    {
        _reportService = reportService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<GeneratedReport> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        if (request.SpectrumIds == null || request.SpectrumIds.Count == 0)
            throw new ArgumentException("At least one spectrum ID is required.", nameof(request.SpectrumIds));

        _logger.LogInformation("Generating report: ProjectId={ProjectId}, SpectrumCount={SpectrumCount}, " +
            "Template={TemplateName}, OutputDir={OutputDirectory}",
            request.ProjectId, request.SpectrumIds.Count, request.TemplateName, request.OutputDirectory);

        GeneratedReport report;

        if (request.SpectrumIds.Count == 1)
        {
            report = await _reportService.GenerateReportAsync(
                request.ProjectId,
                request.SpectrumIds[0],
                request.TemplateName,
                request.OutputDirectory,
                cancellationToken);
        }
        else
        {
            report = await _reportService.GenerateBatchReportAsync(
                request.ProjectId,
                request.SpectrumIds,
                request.TemplateName,
                request.OutputDirectory,
                cancellationToken);
        }

        _logger.LogInformation("Report generated: ReportId={ReportId}, Status={Status}, File={FilePath}",
            report.Id, report.Status, report.FilePath);

        // Publish processing event for each spectrum included in the report
        foreach (var spectrumId in request.SpectrumIds)
        {
            var reportStatus = report.Status == ReportStatus.Generated ? ProcessingStatus.Completed : ProcessingStatus.Failed;
            await _mediator.Publish(new ProcessingCompletedEvent
            {
                DataSourceId = spectrumId,
                ProcessingStepId = report.Id,
                OperationType = $"GenerateReport.{request.TemplateName}",
                Status = reportStatus,
                Success = report.Status == ReportStatus.Generated,
                ErrorMessage = report.ErrorMessage,
                Timestamp = report.GeneratedAt
            }, cancellationToken);
        }

        return report;
    }
}
