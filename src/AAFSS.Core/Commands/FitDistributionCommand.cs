using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to fit a statistical distribution to rainflow cycle counting results.
/// Supports single-distribution fitting or automatic best-fit selection.
/// Delegates to IStatisticalModelingService.
/// </summary>
public record FitDistributionCommand(
    Guid RainflowResultId,
    DistributionType? DistributionType = null
) : IRequest<StatisticalModel>;
