using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AAFSS.App.Messaging;

/// <summary>
/// Message sent to update the status bar with processing progress, warnings,
/// or informational messages.
/// Published by command handlers and workflow steps.
/// Consumed by StatusBarViewModel.
/// </summary>
public class StatusMessage : ValueChangedMessage<StatusMessagePayload>
{
    public StatusMessage(StatusMessagePayload payload) : base(payload) { }
}

/// <summary>
/// Payload for status bar messages.
/// </summary>
public class StatusMessagePayload
{
    /// <summary>The display text for the status bar.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Severity level dictating the status bar color/icon.</summary>
    public StatusSeverity Severity { get; init; } = StatusSeverity.Info;

    /// <summary>Optional progress value (0.0 to 1.0) for progress bar display.</summary>
    public double? Progress { get; init; }

    /// <summary>Whether the status is transient (auto-clears after a timeout).</summary>
    public bool IsTransient { get; init; }

    /// <summary>Optional tooltip for detailed status information.</summary>
    public string? Tooltip { get; init; }
}

/// <summary>
/// Severity level for status messages.
/// </summary>
public enum StatusSeverity
{
    Info,
    Success,
    Warning,
    Error,
    Busy
}
