using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve the processing history (audit trail) for a data source.
/// Returns all processing steps in chronological order (by StepOrder).
/// </summary>
public record GetProcessingHistoryQuery(Guid DataSourceId) : IRequest<List<ProcessingStep>>;
