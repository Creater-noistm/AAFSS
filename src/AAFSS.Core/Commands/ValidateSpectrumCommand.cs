using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to validate a compiled spectrum against damage tolerance criteria.
/// Computes the deviation between target and actual damage, and assigns
/// a Green/Yellow/Red validation level.
/// Delegates to IValidationService and publishes ValidationCompletedEvent.
/// </summary>
public record ValidateSpectrumCommand(
    Guid ProjectId,
    Guid SpectrumId,
    double TargetDamage = 1.0,
    double ToleranceGreen = 0.05,
    double ToleranceYellow = 0.10
) : IRequest<ValidationReport>;
