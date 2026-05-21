using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Events;

/// <summary>
/// Logging handler for <see cref="DataImportedEvent"/>.
/// Records structured log entries for import auditing and diagnostics.
/// </summary>
public class DataImportedEventHandler : INotificationHandler<DataImportedEvent>
{
    private readonly ILogger<DataImportedEventHandler> _logger;

    public DataImportedEventHandler(ILogger<DataImportedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DataImportedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Data imported: ProjectId={ProjectId}, ProfileId={ProfileId}, DataSourceId={DataSourceId}, " +
            "File={FilePath}, Format={Format}, Samples={DataPointCount}, SampleRate={SampleRate}Hz, " +
            "Channels={ChannelCount}, ImportedAt={ImportedAt}",
            notification.ProjectId, notification.ProfileId, notification.DataSourceId,
            notification.FilePath, notification.Format, notification.DataPointCount,
            notification.SampleRate, notification.ChannelCount, notification.ImportedAt);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Logging handler for <see cref="ProcessingCompletedEvent"/>.
/// Emits informational or warning logs depending on processing outcome.
/// </summary>
public class ProcessingCompletedEventHandler : INotificationHandler<ProcessingCompletedEvent>
{
    private readonly ILogger<ProcessingCompletedEventHandler> _logger;

    public ProcessingCompletedEventHandler(ILogger<ProcessingCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProcessingCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Status == Models.ProcessingStatus.Completed)
        {
            _logger.LogInformation(
                "Processing completed: DataSourceId={DataSourceId}, StepId={ProcessingStepId}, " +
                "Operation={OperationType}, Duration={DurationMs:F1}ms",
                notification.DataSourceId, notification.ProcessingStepId,
                notification.OperationType, notification.DurationMs);
        }
        else if (notification.Status == Models.ProcessingStatus.Failed)
        {
            _logger.LogWarning(
                "Processing failed: DataSourceId={DataSourceId}, StepId={ProcessingStepId}, " +
                "Operation={OperationType}, Error={ErrorMessage}, Duration={DurationMs:F1}ms",
                notification.DataSourceId, notification.ProcessingStepId,
                notification.OperationType, notification.ErrorMessage, notification.DurationMs);
        }
        else if (notification.Status == Models.ProcessingStatus.Cancelled)
        {
            _logger.LogInformation(
                "Processing cancelled: DataSourceId={DataSourceId}, StepId={ProcessingStepId}, " +
                "Operation={OperationType}",
                notification.DataSourceId, notification.ProcessingStepId, notification.OperationType);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Logging handler for <see cref="SpectrumCompiledEvent"/>.
/// Records spectrum compilation outcomes including damage and OASPL metrics.
/// </summary>
public class SpectrumCompiledEventHandler : INotificationHandler<SpectrumCompiledEvent>
{
    private readonly ILogger<SpectrumCompiledEventHandler> _logger;

    public SpectrumCompiledEventHandler(ILogger<SpectrumCompiledEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SpectrumCompiledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Spectrum compiled: ProjectId={ProjectId}, SpectrumId={SpectrumId}, Name={SpectrumName}, " +
            "Category={Category}, Type={SpectrumType}, Method={Method}, " +
            "D={DamageValue:F6}, OASPL={Oaspl:F2}dB, SourceSpectraCount={SourceCount}",
            notification.ProjectId, notification.SpectrumId, notification.SpectrumName,
            notification.Category, notification.SpectrumType, notification.Method,
            notification.DamageValue, notification.Oaspl, notification.SourceCount);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Logging handler for <see cref="ValidationCompletedEvent"/>.
/// Logs validation outcomes with severity-appropriate log levels
/// (Warning for Yellow, Error for Red, Information for Green).
/// </summary>
public class ValidationCompletedEventHandler : INotificationHandler<ValidationCompletedEvent>
{
    private readonly ILogger<ValidationCompletedEventHandler> _logger;

    public ValidationCompletedEventHandler(ILogger<ValidationCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ValidationCompletedEvent notification, CancellationToken cancellationToken)
    {
        var messageTemplate =
            "Validation completed: SpectrumId={SpectrumId}, Level={Level}, " +
            "TargetD={TargetD}, ActualD={ActualD:F6}, Deviation={Deviation:F4}, " +
            "Warnings={WarningCount}";

        switch (notification.Level)
        {
            case Models.ValidationLevel.Green:
                _logger.LogInformation(messageTemplate,
                    notification.SpectrumId, notification.Level, notification.TargetD,
                    notification.ActualD, notification.Deviation, notification.Warnings.Length);
                break;
            case Models.ValidationLevel.Yellow:
                _logger.LogWarning(messageTemplate,
                    notification.SpectrumId, notification.Level, notification.TargetD,
                    notification.ActualD, notification.Deviation, notification.Warnings.Length);
                break;
            case Models.ValidationLevel.Red:
                _logger.LogError(messageTemplate,
                    notification.SpectrumId, notification.Level, notification.TargetD,
                    notification.ActualD, notification.Deviation, notification.Warnings.Length);
                break;
            default:
                _logger.LogInformation("Validation status: SpectrumId={SpectrumId}, Level={Level}",
                    notification.SpectrumId, notification.Level);
                break;
        }

        // Log individual warnings at Debug level for detailed diagnostics
        foreach (var warning in notification.Warnings)
        {
            _logger.LogDebug("Validation warning [SpectrumId={SpectrumId}]: {Warning}",
                notification.SpectrumId, warning);
        }

        return Task.CompletedTask;
    }
}
