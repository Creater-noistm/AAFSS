using MediatR;

namespace AAFSS.Core.Commands;

public record ComputeRainflowCommand : IRequest<Guid>
{
    public Guid TimeSeriesDataId { get; init; }
    public int Bins { get; init; } = 64;
    public bool ApplyMeanStressCorrection { get; init; }
}
