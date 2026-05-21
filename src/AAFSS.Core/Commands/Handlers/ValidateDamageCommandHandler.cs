using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;

namespace AAFSS.Core.Commands.Handlers;

public class ValidateDamageCommandHandler : IRequestHandler<ValidateDamageCommand, ValidationResultDto>
{
    private readonly IValidationService _validationService;
    private readonly IMediator _mediator;

    public ValidateDamageCommandHandler(IValidationService validationService, IMediator mediator)
    {
        _validationService = validationService;
        _mediator = mediator;
    }

    public async Task<ValidationResultDto> Handle(ValidateDamageCommand r, CancellationToken ct)
    {
        var reportId = await _validationService.ValidateAsync(
            r.CompiledSpectrumId, r.TargetDamage, r.Tolerance, ct);

        var report = await _validationService.GetValidationReportAsync(r.CompiledSpectrumId, ct);
        if (report == null)
        {
            return new ValidationResultDto
            {
                Id = r.CompiledSpectrumId,
                Level = ValidationLevel.NotValidated,
                ActualDamage = 0,
                Deviation = 0,
                Warnings = Array.Empty<string>()
            };
        }

        var result = new ValidationResultDto
        {
            Id = reportId,
            Level = report.Level,
            ActualDamage = report.ActualD,
            Deviation = report.Deviation,
            Warnings = report.Warnings
        };

        await _mediator.Publish(new ValidationCompletedEvent
        {
            SpectrumId = r.CompiledSpectrumId,
            ValidationReportId = reportId,
            Level = report.Level,
            Status = report.Level == ValidationLevel.Green ? ValidationStatus.Passed
                   : report.Level == ValidationLevel.Red ? ValidationStatus.Failed
                   : ValidationStatus.Warning,
            ActualD = report.ActualD,
            TargetD = r.TargetDamage,
            Deviation = report.Deviation,
            Warnings = report.Warnings
        }, ct);

        return result;
    }
}
