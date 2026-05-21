using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the bottom dock panel containing output log, task progress,
/// and validation messages. Consumes StatusMessage broadcasts.
/// </summary>
public partial class BottomPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LogEntryViewModel> _logEntries = new();

    [ObservableProperty]
    private ObservableCollection<TaskProgressViewModel> _activeTasks = new();

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _filterText = string.Empty;

    private const int MaxLogEntries = 1000;

    public BottomPanelViewModel()
    {
        WeakReferenceMessenger.Default.Register<StatusMessage>(this, OnStatusMessageReceived);
    }

    private void OnStatusMessageReceived(object recipient, StatusMessage message)
    {
        var payload = message.Value;
        AddLogEntry(payload.Text, payload.Severity);
    }

    /// <summary>
    /// Adds a log entry, trimming the list if it exceeds the maximum.
    /// </summary>
    public void AddLogEntry(string text, StatusSeverity severity = StatusSeverity.Info)
    {
        var entry = new LogEntryViewModel
        {
            Timestamp = DateTime.Now,
            Text = text,
            Severity = severity
        };

        LogEntries.Add(entry);

        while (LogEntries.Count > MaxLogEntries)
            LogEntries.RemoveAt(0);

        // Auto-scroll to bottom
        SelectedTabIndex = 0;
    }

    /// <summary>
    /// Adds a task progress item for long-running operations.
    /// </summary>
    public void AddTaskProgress(string taskId, string description)
    {
        ActiveTasks.Add(new TaskProgressViewModel
        {
            TaskId = taskId,
            Description = description,
            Progress = 0,
            Status = "运行中..."
        });
    }

    /// <summary>
    /// Updates the progress of a tracked task.
    /// </summary>
    public void UpdateTaskProgress(string taskId, double progress, string status)
    {
        var task = ActiveTasks.FirstOrDefault(t => t.TaskId == taskId);
        if (task != null)
        {
            task.Progress = progress;
            task.Status = status;
        }
    }

    /// <summary>
    /// Removes a completed task from the progress list.
    /// </summary>
    public void RemoveTask(string taskId)
    {
        var task = ActiveTasks.FirstOrDefault(t => t.TaskId == taskId);
        if (task != null) ActiveTasks.Remove(task);
    }
}

/// <summary>
/// Represents a single log entry in the output panel.
/// </summary>
public partial class LogEntryViewModel : ObservableObject
{
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private StatusSeverity _severity = StatusSeverity.Info;

    public string TimestampDisplay => Timestamp.ToString("HH:mm:ss.fff");
    public string SeverityIcon => Severity switch
    {
        StatusSeverity.Error => "❌",
        StatusSeverity.Warning => "⚠️",
        StatusSeverity.Success => "✅",
        StatusSeverity.Busy => "🔄",
        _ => "ℹ️"
    };
}

/// <summary>
/// Represents a task progress item in the bottom panel.
/// </summary>
public partial class TaskProgressViewModel : ObservableObject
{
    [ObservableProperty] private string _taskId = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status = string.Empty;
}
