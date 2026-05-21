using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries;

/// <summary>
/// Handles <see cref="GetValidationResultQuery"/> by retrieving the validation
/// report for a compiled spectrum from the validation service.
/// </summary>
public class GetValidationResultQueryHandler : IRequestHandler<GetValidationResultQuery, ValidationReport?>
{
    private readonly IValidationService _validationService;
    private readonly ILogger<GetValidationResultQueryHandler> _logger;

    public GetValidationResultQueryHandler(
        IValidationService validationService,
        ILogger<GetValidationResultQueryHandler> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ValidationReport?> Handle(GetValidationResultQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving validation result for SpectrumId={SpectrumId}", request.SpectrumId);

        var report = await _validationService.GetValidationReportAsync(request.SpectrumId, cancellationToken);

        if (report == null)
        {
            _logger.LogDebug("No validation report found for SpectrumId={SpectrumId}", request.SpectrumId);
        }
        else
        {
            _logger.LogDebug("Validation report retrieved: ReportId={ReportId}, Level={Level}, Deviation={Deviation:F4}",
                report.Id, report.Level, report.Deviation);
        }

        return report;
    }
}
