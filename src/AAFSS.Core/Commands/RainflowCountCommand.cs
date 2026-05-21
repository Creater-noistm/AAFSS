using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to perform ASTM E1049 rainflow cycle counting on time series data.
/// Delegates to ITimeDomainAnalysisService.
/// </summary>
public record RainflowCountCommand(
    Guid DataSourceId,
    int ChannelIndex = 0
) : IRequest<RainflowResult>;
