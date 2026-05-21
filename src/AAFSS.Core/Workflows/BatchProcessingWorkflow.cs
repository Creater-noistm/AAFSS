using MediatR;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace AAFSS.Core.Workflows;

/// <summary>
/// Batch processing workflow that iterates over multiple input files and
/// runs the full spectrum compilation pipeline for each one.
/// 
/// Uses a while-loop pattern in WorkflowCore to process items sequentially.
/// Each item goes through: Import → ComputeSpectrum → Rainflow → FitDistribution →
/// CompileSpectrum → ValidateSpectrum → GenerateReport.
/// </summary>
public class BatchProcessingWorkflow : IWorkflow<BatchProcessingData>
{
    public string Id => nameof(BatchProcessingWorkflow);
    public int Version => 1;

    public void Build(IWorkflowBuilder<BatchProcessingData> builder)
    {
        builder
            .StartWith<BatchInitializeStep>()
            .While(data => data.HasMoreItems)
                .Do(whileBuilder => whileBuilder
                    .StartWith<BatchProcessSingleItemStep>()
                        .Input(step => step.ProjectId, data => data.ProjectId)
                        .Input(step => step.CurrentIndex, data => data.CurrentIndex)
                        .Input(step => step.OutputDirectory, data => data.OutputDirectory)
                        .Input(step => step.ReportTemplateName, data => data.ReportTemplateName)
                    .Then<BatchAdvanceStep>())
            .Then<BatchCompleteStep>();
    }
}

/// <summary>
/// Initializes the batch processing context.
/// </summary>
public class BatchInitializeStep : IStepBody
{
    private readonly ILogger<BatchInitializeStep> _logger;

    public BatchInitializeStep(ILogger<BatchInitializeStep> logger)
    {
        _logger = logger;
    }

    public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (BatchProcessingData)context.Workflow.Data;
        data.CurrentIndex = 0;
        data.SuccessCount = 0;
        data.FailureCount = 0;
        data.Status = Models.ProcessingStatus.Running;

        _logger.LogInformation("[BatchWorkflow] Initialized: {ItemCount} items to process", data.Items.Count);

        return Task.FromResult(ExecutionResult.Next());
    }
}

/// <summary>
/// Processes a single batch item through the full spectrum compilation pipeline.
/// Each item triggers a sub-workflow (SpectrumCompilationWorkflow) via MediatR commands.
/// </summary>
public class BatchProcessSingleItemStep : IStepBody
{
    private readonly IMediator _mediator;
    private readonly ILogger<BatchProcessSingleItemStep> _logger;

    public Guid ProjectId { get; set; }
    public int CurrentIndex { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public string ReportTemplateName { get; set; } = "GJB_67_13_90";

    public BatchProcessSingleItemStep(IMediator mediator, ILogger<BatchProcessSingleItemStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (BatchProcessingData)context.Workflow.Data;
        var item = data.Items[CurrentIndex];

        _logger.LogInformation("[BatchWorkflow] Processing item {Index}/{Total}: {FilePath}",
            CurrentIndex + 1, data.Items.Count, item.FilePath);

        try
        {
            // Step 1: Import data
            var importCmd = new Commands.ImportDataCommand
            {
                ProjectId = ProjectId,
                ProfileId = item.ProfileId,
                FilePath = item.FilePath,
                MeasurementPointId = item.MeasurementPointId
            };
            var dataSource = await _mediator.Send(importCmd);

            // Step 2: Compute spectrum
            var spectrumCmd = new Commands.ComputeSpectrumCommand(
                dataSource.Id, Models.SpectrumType.Octave1_3);
            var spectrumResult = await _mediator.Send(spectrumCmd);

            // Step 3: Rainflow count
            var rainflowCmd = new Commands.RainflowCountCommand(dataSource.Id);
            var rainflowResult = await _mediator.Send(rainflowCmd);

            // Step 4: Fit distribution (auto best-fit)
            var fitCmd = new Commands.FitDistributionCommand(rainflowResult.Id);
            await _mediator.Send(fitCmd);

            // Step 5: Compile spectrum
            var compileCmd = new Commands.CompileSpectrumCommand(
                ProjectId, item.SpectrumName,
                Models.CompilationMethod.StateRegionEnvelope,
                new List<Guid> { spectrumResult.Id });
            var compiledSpectrum = await _mediator.Send(compileCmd);

            // Step 6: Validate
            var validateCmd = new Commands.ValidateSpectrumCommand(ProjectId, compiledSpectrum.Id);
            await _mediator.Send(validateCmd);

            // Step 7: Generate report
            var reportCmd = new Commands.GenerateReportCommand(
                ProjectId,
                new List<Guid> { compiledSpectrum.Id },
                ReportTemplateName,
                OutputDirectory);
            await _mediator.Send(reportCmd);

            item.Status = Models.ProcessingStatus.Completed;
            data.SuccessCount++;

            _logger.LogInformation("[BatchWorkflow] Item {Index} completed successfully: {FilePath}",
                CurrentIndex + 1, item.FilePath);
        }
        catch (Exception ex)
        {
            item.Status = Models.ProcessingStatus.Failed;
            item.ErrorMessage = ex.Message;
            data.FailureCount++;
            data.ErrorMessages.Add($"[Item {CurrentIndex + 1}] {item.FilePath}: {ex.Message}");

            _logger.LogError(ex, "[BatchWorkflow] Item {Index} failed: {FilePath} - {Message}",
                CurrentIndex + 1, item.FilePath, ex.Message);

            // Continue with next item — don't halt the entire batch
        }

        return ExecutionResult.Next();
    }
}

/// <summary>
/// Advances the batch index to the next item.
/// </summary>
public class BatchAdvanceStep : IStepBody
{
    public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (BatchProcessingData)context.Workflow.Data;
        data.CurrentIndex++;
        return Task.FromResult(ExecutionResult.Next());
    }
}

/// <summary>
/// Finalizes the batch processing run and logs the summary.
/// </summary>
public class BatchCompleteStep : IStepBody
{
    private readonly ILogger<BatchCompleteStep> _logger;

    public BatchCompleteStep(ILogger<BatchCompleteStep> logger)
    {
        _logger = logger;
    }

    public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (BatchProcessingData)context.Workflow.Data;
        data.Status = data.FailureCount > 0
            ? (data.SuccessCount > 0 ? Models.ProcessingStatus.Completed : Models.ProcessingStatus.Failed)
            : Models.ProcessingStatus.Completed;

        _logger.LogInformation(
            "[BatchWorkflow] Completed: {TotalItems} items, {SuccessCount} succeeded, {FailureCount} failed",
            data.Items.Count, data.SuccessCount, data.FailureCount);

        return Task.FromResult(ExecutionResult.Next());
    }
}
