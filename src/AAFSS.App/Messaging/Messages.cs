using AAFSS.App.ViewModels;
using AAFSS.Core.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AAFSS.App.Messaging;

// ─── Project Lifecycle Messages ────────────────────────────────────────

public class ProjectOpenedMessage(Project project) : ValueChangedMessage<Project>(project)
{
    public Project Project { get; } = project;
}

public class ProjectClosedMessage : ValueChangedMessage<bool>
{
    public ProjectClosedMessage() : base(true) { }
}

public class NewProjectRequestMessage : RequestMessage<bool> { }

public class OpenProjectRequestMessage : RequestMessage<bool> { }

public class SaveProjectRequestMessage : RequestMessage<bool> { }

// ─── Status / Busy Messages ───────────────────────────────────────────

public class StatusUpdateMessage(string message) : ValueChangedMessage<string>(message)
{
    public string Message { get; } = message;
}

public class BusyStateMessage(bool isBusy, string message) : ValueChangedMessage<bool>(isBusy)
{
    public bool IsBusy { get; } = isBusy;
    public string Message { get; } = message;
}

// ─── Document Management Messages ─────────────────────────────────────

public class DocumentOpenMessage(DocumentViewModel document) : ValueChangedMessage<DocumentViewModel>(document)
{
    public DocumentViewModel Document { get; } = document;
    public string Title => Document.Title;
}

public class DocumentCloseMessage(DocumentViewModel document) : ValueChangedMessage<DocumentViewModel>(document)
{
    public DocumentViewModel Document { get; } = document;
}

// ─── Data Import Messages ─────────────────────────────────────────────

public class DataImportedMessage(Guid projectId, string fileName) : ValueChangedMessage<string>(fileName)
{
    public Guid ProjectId { get; } = projectId;
    public string FileName { get; } = fileName;
}

public class ShowImportDialogMessage : ValueChangedMessage<bool>
{
    public ShowImportDialogMessage() : base(true) { }
}

// ─── Spectrum Compilation Messages ────────────────────────────────────

public class SpectrumCompiledMessage(Guid spectrumId, Guid projectId, string spectrumName)
    : ValueChangedMessage<Guid>(spectrumId)
{
    public Guid SpectrumId { get; } = spectrumId;
    public Guid ProjectId { get; } = projectId;
    public string SpectrumName { get; } = spectrumName;
}

// ─── Tree / Selection Messages ────────────────────────────────────────

public class TreeNodeSelectedMessage(Guid entityId, string nodeType, string name)
    : ValueChangedMessage<Guid>(entityId)
{
    public Guid EntityId { get; } = entityId;
    public string NodeType { get; } = nodeType;
    public string Name { get; } = name;
}

public class ClearSelectionMessage : ValueChangedMessage<bool>
{
    public ClearSelectionMessage() : base(true) { }
}

// ─── Output Panel Messages ────────────────────────────────────────────

/// <summary>
/// Severity level for output log messages.
/// </summary>
public enum OutputLevel
{
    Debug,
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// A single output log message entry.
/// </summary>
public class OutputMessage
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Text { get; init; } = string.Empty;
    public OutputLevel Level { get; init; } = OutputLevel.Info;
    public string Source { get; init; } = string.Empty;
}

public class OutputMessageAdded(OutputMessage message) : ValueChangedMessage<OutputMessage>(message)
{
    public OutputMessage Message { get; } = message;
}

// ─── Analysis Request Messages ────────────────────────────────────────

public class ComputeSpectrumRequestMessage : RequestMessage<bool> { }

public class CompileSpectrumRequestMessage : RequestMessage<bool> { }

public class RainflowCountRequestMessage(Guid? dataSourceId = null) : RequestMessage<bool>
{
    public Guid? DataSourceId { get; } = dataSourceId;
}

public class DamageCalculationRequestMessage : RequestMessage<bool> { }

public class GenerateReportRequestMessage : RequestMessage<bool> { }

public class FitDistributionRequestMessage : RequestMessage<bool> { }

public class PreprocessSignalRequestMessage(Guid? dataSourceId = null) : RequestMessage<bool>
{
    public Guid? DataSourceId { get; } = dataSourceId;
}

public class BatchProcessingRequestMessage : RequestMessage<bool> { }

// ─── Infrastructure Messages ──────────────────────────────────────────

public class PythonReadyMessage(string version) : ValueChangedMessage<string>(version)
{
    public string Version { get; } = version;
}
