using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="ValidateSpectrumCommand"/> by performing damage validation
/// and publishing the result as a domain event for UI notification.
/// </summary>
public class ValidateSpectrumCommandHandler : IRequestHandler<ValidateSpectrumCommand, ValidationReport>
{
    private readonly IValidationService _validationService;
    private readonly IMediator _mediator;
    private readonly ILogger<ValidateSpectrumCommandHandler> _logger;

    public ValidateSpectrumCommandHandler(
        IValidationService validationService,
        IMediator mediator,
        ILogger<ValidateSpectrumCommandHandler> logger)
    {
        _validationService = validationService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ValidationReport> Handle(ValidateSpectrumCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating spectrum: ProjectId={ProjectId}, SpectrumId={SpectrumId}, " +
            "TargetD={TargetDamage}, Green={ToleranceGreen}, Yellow={ToleranceYellow}",
            request.ProjectId, request.SpectrumId, request.TargetDamage,
            request.ToleranceGreen, request.ToleranceYellow);

        var report = await _validationService.ValidateSpectrumAsync(
            request.SpectrumId,
            request.TargetDamage,
            request.ToleranceGreen,
            request.ToleranceYellow,
            cancellationToken);

        _logger.LogInformation("Validation completed: SpectrumId={SpectrumId}, Level={Level}, " +
            "TargetD={TargetDamage}, ActualD={ActualD:F6}, Deviation={Deviation:F4}",
            request.SpectrumId, report.Level, report.TargetD, report.ActualD, report.Deviation);

        await _mediator.Publish(new ValidationCompletedEvent
        {
            ProjectId = request.ProjectId,
            SpectrumId = request.SpectrumId,
            ValidationReportId = report.Id,
            Level = report.Level,
            TargetD = report.TargetD,
            ActualD = report.ActualD,
            Deviation = report.Deviation,
            Warnings = report.Warnings,
            ValidatedAt = report.ValidatedAt
        }, cancellationToken);

        return report;
    }
}
