using AAFSS.Core.Commands;
using AAFSS.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace AAFSS.Core.Workflows;

/// <summary>
/// Full-spectrum compilation workflow that orchestrates the entire pipeline:
/// Import → Compute Spectrum → Rainflow Count → Fit Distribution →
/// Compile Spectrum → Validate → Generate Report.
/// 
/// Each stage is a WorkflowCore step that delegates to a MediatR command.
/// On failure at any stage, the workflow halts and records the error.
/// </summary>
public class SpectrumCompilationWorkflow : IWorkflow<SpectrumCompilationData>
{
    public string Id => nameof(SpectrumCompilationWorkflow);
    public int Version => 1;

    public void Build(IWorkflowBuilder<SpectrumCompilationData> builder)
    {
        builder
            .StartWith<ImportDataStep>()
                .Input(step => step.ProjectId, data => data.ProjectId)
                .Input(step => step.ProfileId, data => data.ProfileId)
                .Input(step => step.FilePath, data => data.FilePath)
                .Input(step => step.MeasurementPointId, data => data.MeasurementPointId)
                .Output(data => data.DataSourceId, step => step.OutputDataSourceId)
            .Then<ComputeSpectrumStep>()
                .Input(step => step.DataSourceId, data => data.DataSourceId)
                .Output(data => data.SpectrumResultId, step => step.OutputSpectrumResultId)
            .Then<RainflowCountStep>()
                .Input(step => step.DataSourceId, data => data.DataSourceId)
                .Output(data => data.RainflowResultId, step => step.OutputRainflowResultId)
            .Then<FitDistributionStep>()
                .Input(step => step.RainflowResultId, data => data.RainflowResultId)
                .Output(data => data.StatisticalModelId, step => step.OutputStatisticalModelId)
            .Then<CompileSpectrumStep>()
                .Input(step => step.ProjectId, data => data.ProjectId)
                .Input(step => step.SpectrumName, data => data.SpectrumName)
                .Input(step => step.Method, data => data.CompilationMethod)
                .Input(step => step.EnvelopeOffset, data => data.EnvelopeOffset)
                .Input(step => step.SpectrumResultId, data => data.SpectrumResultId)
                .Output(data => data.CompiledSpectrumId, step => step.OutputCompiledSpectrumId)
            .Then<ValidateSpectrumStep>()
                .Input(step => step.ProjectId, data => data.ProjectId)
                .Input(step => step.CompiledSpectrumId, data => data.CompiledSpectrumId)
                .Output(data => data.ValidationReportId, step => step.OutputValidationReportId)
            .Then<GenerateReportStep>()
                .Input(step => step.ProjectId, data => data.ProjectId)
                .Input(step => step.CompiledSpectrumId, data => data.CompiledSpectrumId)
                .Input(step => step.TemplateName, data => data.ReportTemplateName)
                .Input(step => step.OutputDirectory, data => data.OutputDirectory)
                .Output(data => data.GeneratedReportId, step => step.OutputGeneratedReportId);
    }
}

// ─── Step Implementations ───────────────────────────────────────────

/// <summary>
/// Workflow step: imports measurement data into the project.
/// </summary>
public class ImportDataStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<ImportDataStep> _logger;

    public Guid ProjectId { get; set; }
    public Guid ProfileId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public Guid? MeasurementPointId { get; set; }

    public Guid OutputDataSourceId { get; set; }

    public ImportDataStep(IMediator mediator, ILogger<ImportDataStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] ImportDataStep: ProjectId={ProjectId}, File={FilePath}",
                ProjectId, FilePath);

            var command = new ImportDataCommand
            {
                ProjectId = ProjectId,
                ProfileId = ProfileId,
                FilePath = FilePath,
                MeasurementPointId = MeasurementPointId
            };
            var result = await _mediator.Send(command);

            OutputDataSourceId = result.Id;
            _logger.LogInformation("[Workflow] ImportDataStep completed: DataSourceId={DataSourceId}", OutputDataSourceId);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] ImportDataStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: computes 1/3 octave spectrum from imported data.
/// </summary>
public class ComputeSpectrumStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<ComputeSpectrumStep> _logger;

    public Guid DataSourceId { get; set; }
    public Guid OutputSpectrumResultId { get; set; }

    public ComputeSpectrumStep(IMediator mediator, ILogger<ComputeSpectrumStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] ComputeSpectrumStep: DataSourceId={DataSourceId}", DataSourceId);

            var command = new ComputeSpectrumCommand(DataSourceId, SpectrumType.Octave1_3);
            var result = await _mediator.Send(command);

            OutputSpectrumResultId = result.Id;
            _logger.LogInformation("[Workflow] ComputeSpectrumStep completed: SpectrumResultId={SpectrumResultId}",
                OutputSpectrumResultId);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] ComputeSpectrumStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: performs ASTM E1049 rainflow cycle counting.
/// </summary>
public class RainflowCountStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<RainflowCountStep> _logger;

    public Guid DataSourceId { get; set; }
    public Guid OutputRainflowResultId { get; set; }

    public RainflowCountStep(IMediator mediator, ILogger<RainflowCountStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] RainflowCountStep: DataSourceId={DataSourceId}", DataSourceId);

            var command = new RainflowCountCommand(DataSourceId);
            var result = await _mediator.Send(command);

            OutputRainflowResultId = result.Id;
            _logger.LogInformation("[Workflow] RainflowCountStep completed: RainflowResultId={RainflowResultId}",
                OutputRainflowResultId);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] RainflowCountStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: fits statistical distribution to rainflow data.
/// Uses automatic best-fit selection across all distribution types.
/// </summary>
public class FitDistributionStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<FitDistributionStep> _logger;

    public Guid RainflowResultId { get; set; }
    public Guid OutputStatisticalModelId { get; set; }

    public FitDistributionStep(IMediator mediator, ILogger<FitDistributionStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] FitDistributionStep: RainflowResultId={RainflowResultId}",
                RainflowResultId);

            // null DistributionType = auto best-fit
            var command = new FitDistributionCommand(RainflowResultId);
            var result = await _mediator.Send(command);

            OutputStatisticalModelId = result.Id;
            _logger.LogInformation("[Workflow] FitDistributionStep completed: Distribution={DistributionType}, ModelId={ModelId}",
                result.DistributionType, OutputStatisticalModelId);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] FitDistributionStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: compiles source spectra into a final compiled spectrum.
/// </summary>
public class CompileSpectrumStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompileSpectrumStep> _logger;

    public Guid ProjectId { get; set; }
    public string SpectrumName { get; set; } = string.Empty;
    public CompilationMethod Method { get; set; }
    public double EnvelopeOffset { get; set; }
    public Guid SpectrumResultId { get; set; }

    public Guid OutputCompiledSpectrumId { get; set; }

    public CompileSpectrumStep(IMediator mediator, ILogger<CompileSpectrumStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] CompileSpectrumStep: ProjectId={ProjectId}, Name={SpectrumName}, Method={Method}",
                ProjectId, SpectrumName, Method);

            var command = new CompileSpectrumCommand(
                ProjectId, SpectrumName, Method,
                new List<Guid> { SpectrumResultId },
                EnvelopeOffset);

            var result = await _mediator.Send(command);

            OutputCompiledSpectrumId = result.Id;
            _logger.LogInformation("[Workflow] CompileSpectrumStep completed: SpectrumId={SpectrumId}, D={DamageValue:F6}",
                OutputCompiledSpectrumId, result.DamageValue);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] CompileSpectrumStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: validates the compiled spectrum against damage criteria.
/// </summary>
public class ValidateSpectrumStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<ValidateSpectrumStep> _logger;

    public Guid ProjectId { get; set; }
    public Guid CompiledSpectrumId { get; set; }
    public Guid OutputValidationReportId { get; set; }

    public ValidateSpectrumStep(IMediator mediator, ILogger<ValidateSpectrumStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] ValidateSpectrumStep: SpectrumId={CompiledSpectrumId}",
                CompiledSpectrumId);

            var command = new ValidateSpectrumCommand(ProjectId, CompiledSpectrumId);
            var result = await _mediator.Send(command);

            OutputValidationReportId = result.Id;
            _logger.LogInformation("[Workflow] ValidateSpectrumStep completed: Level={Level}, Deviation={Deviation:F4}",
                result.Level, result.Deviation);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] ValidateSpectrumStep failed: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Workflow step: generates the final report document.
/// </summary>
public class GenerateReportStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<GenerateReportStep> _logger;

    public Guid ProjectId { get; set; }
    public Guid CompiledSpectrumId { get; set; }
    public string TemplateName { get; set; } = "GJB_67_13_90";
    public string OutputDirectory { get; set; } = string.Empty;
    public Guid OutputGeneratedReportId { get; set; }

    public GenerateReportStep(IMediator mediator, ILogger<GenerateReportStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            _logger.LogInformation("[Workflow] GenerateReportStep: SpectrumId={CompiledSpectrumId}, Template={TemplateName}",
                CompiledSpectrumId, TemplateName);

            var command = new GenerateReportCommand(
                ProjectId,
                new List<Guid> { CompiledSpectrumId },
                TemplateName,
                OutputDirectory);

            var result = await _mediator.Send(command);

            OutputGeneratedReportId = result.Id;
            _logger.LogInformation("[Workflow] GenerateReportStep completed: ReportId={ReportId}, File={FilePath}",
                OutputGeneratedReportId, result.FilePath);

            return ExecutionResult.Next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Workflow] GenerateReportStep failed: {Message}", ex.Message);
            throw;
        }
    }
}
