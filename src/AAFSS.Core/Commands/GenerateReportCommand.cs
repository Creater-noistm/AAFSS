using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to generate a DOCX report for a compiled spectrum or a batch of spectra.
/// Supports GJB 67.13-90 and custom report templates via IReportGenerationService.
/// </summary>
public record GenerateReportCommand(
    Guid ProjectId,
    List<Guid> SpectrumIds,
    string TemplateName,
    string OutputDirectory
) : IRequest<GeneratedReport>;
