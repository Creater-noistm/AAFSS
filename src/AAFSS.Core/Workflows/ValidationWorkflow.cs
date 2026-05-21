using AAFSS.Core.Commands;
using AAFSS.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace AAFSS.Core.Workflows;

/// <summary>
/// WorkflowCore workflow for batch validation of multiple compiled spectra
/// against their respective damage targets. Runs validations in parallel
/// where possible and aggregates results into a validation report.
///
/// Used for GJB-conformance verification and before report generation.
/// </summary>
public class ValidationWorkflow : IWorkflow<ValidationWorkflowData>
{
    public string Id => nameof(ValidationWorkflow);
    public int Version => 1;

    public void Build(IWorkflowBuilder<ValidationWorkflowData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .ForEach(data => data.ValidationTasks)
                .Do(x => x
                    .StartWith<RunValidationStep>()
                        .Input(step => step.SpectrumId, (data, ctx) => data.Item.SpectrumId)
                        .Input(step => step.TargetDamage, (data, ctx) => data.Item.TargetDamage)
                        .Input(step => step.MaterialSnCurve, (data, ctx) => data.Item.MaterialSnCurve))
            .Then<GenerateValidationReportStep>()
                .Input(step => step.ProjectId, data => data.ProjectId)
                .Input(step => step.Results, data => data.AggregatedResults)
            .Then(context =>
            {
                var data = (ValidationWorkflowData)context.Workflow.Data;
                data.Status = "Completed";
                return ExecutionResult.Next();
            })
            .OnError(WorkflowErrorHandling.Terminate);
    }
}

/// <summary>
/// Payload data for the validation workflow.
/// </summary>
public class ValidationWorkflowData
{
    public Guid ProjectId { get; set; }
    public List<ValidationTask> ValidationTasks { get; set; } = new();
    public List<ValidationTaskResult> AggregatedResults { get; set; } = new();
    public string Status { get; set; } = "Pending";

    /// <summary>Current item in ForEach iteration, set by WorkflowCore runtime.</summary>
    public ValidationTask? Item { get; set; }
}

/// <summary>
/// Individual validation task for a single spectrum.
/// </summary>
public class ValidationTask
{
    public Guid SpectrumId { get; set; }
    public string SpectrumName { get; set; } = string.Empty;
    public double TargetDamage { get; set; } = 1.0;
    public string MaterialSnCurve { get; set; } = "AL7075-T6";
}

/// <summary>
/// Result of a single validation task.
/// </summary>
public class ValidationTaskResult
{
    public Guid SpectrumId { get; set; }
    public string SpectrumName { get; set; } = string.Empty;
    public ValidationLevel Level { get; set; }
    public double TargetDamage { get; set; }
    public double ActualDamage { get; set; }
    public double Deviation { get; set; }
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public bool Passed { get; set; }
}

/// <summary>
/// Workflow step that executes damage validation for a single spectrum.
/// </summary>
public class RunValidationStep : StepBodyAsync
{
    public Guid SpectrumId { get; set; }
    public double TargetDamage { get; set; }
    public string MaterialSnCurve { get; set; } = "AL7075-T6";

    private readonly IMediator _mediator;
    private readonly ILogger<RunValidationStep> _logger;

    public RunValidationStep(IMediator mediator, ILogger<RunValidationStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        _logger.LogInformation("Validation workflow: validating SpectrumId={SpectrumId}, TargetD={TargetDamage}",
            SpectrumId, TargetDamage);
        var cmd = new ValidateDamageCommand
        {
            CompiledSpectrumId = SpectrumId,
            TargetDamage = TargetDamage,
            Tolerance = MaterialSnCurve == "AL7075-T6" ? 0.1 : 0.15
        };
        var result = await _mediator.Send(cmd);

        var parentData = (ValidationWorkflowData)context.Workflow.Data;
        lock (parentData.AggregatedResults)
        {
            parentData.AggregatedResults.Add(new ValidationTaskResult
            {
                SpectrumId = SpectrumId,
                Level = result.Level,
                TargetDamage = TargetDamage,
                ActualDamage = result.ActualDamage,
                Deviation = result.Deviation,
                Warnings = result.Warnings,
                Passed = result.Level == ValidationLevel.Green
            });
        }

        _logger.LogInformation("Validation completed for SpectrumId={SpectrumId}: Level={Level}, Deviation={Deviation:P2}",
            SpectrumId, result.Level, result.Deviation);
        return ExecutionResult.Next();
    }
}

/// <summary>
/// Workflow step that generates an aggregate validation report.
/// </summary>
public class GenerateValidationReportStep : StepBodyAsync
{
    public Guid ProjectId { get; set; }
    public List<ValidationTaskResult> Results { get; set; } = new();

    private readonly ILogger<GenerateValidationReportStep> _logger;

    public GenerateValidationReportStep(ILogger<GenerateValidationReportStep> logger)
    {
        _logger = logger;
    }

    public override Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var total = Results.Count;
        var passed = Results.Count(r => r.Passed);
        var yellow = Results.Count(r => r.Level == ValidationLevel.Yellow);
        var red = Results.Count(r => r.Level == ValidationLevel.Red);

        _logger.LogInformation(
            "Validation Report for ProjectId={ProjectId}: Total={Total}, Passed={Passed}, Yellow={Yellow}, Red={Red}",
            ProjectId, total, passed, yellow, red);

        return Task.FromResult(ExecutionResult.Next());
    }
}
