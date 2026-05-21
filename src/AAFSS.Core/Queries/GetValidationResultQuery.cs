using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve the validation report for a compiled spectrum.
/// Returns null if the spectrum has not been validated yet.
/// </summary>
public record GetValidationResultQuery(Guid SpectrumId) : IRequest<ValidationReport?>;
