using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Result returned by the ValidateDamageCommand handler.
/// </summary>
public record ValidationResultDto
{
    public Guid Id { get; init; }
    public ValidationLevel Level { get; init; }
    public double ActualDamage { get; init; }
    public double Deviation { get; init; }
    public string[] Warnings { get; init; } = Array.Empty<string>();
}

public record ValidateDamageCommand : IRequest<ValidationResultDto>
{
    public Guid CompiledSpectrumId { get; init; }
    public double TargetDamage { get; init; } = 1.0;
    public double Tolerance { get; init; } = 0.1;
}
