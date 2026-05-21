using AAFSS.App.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the status bar — displays current application state indicators.
/// </summary>
public partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _isPythonReady;

    [ObservableProperty]
    private string _pythonVersion = string.Empty;

    [ObservableProperty]
    private double _memoryUsageMB;

    [ObservableProperty]
    private int _activeTaskCount;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _processingLabel = string.Empty;

    [ObservableProperty]
    private string _lastSavedTime = string.Empty;

    [ObservableProperty]
    private bool _isAutoSaveEnabled = true;

    private System.Timers.Timer? _memoryTimer;

    public StatusBarViewModel()
    {
        WeakReferenceMessenger.Default.Register<ProjectOpenedMessage>(this, (r, m) =>
        {
            ProjectName = m.Project.Name;
            IsProjectOpen = true;
            StatusText = "项目已加载";
        });

        WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) =>
        {
            ProjectName = string.Empty;
            IsProjectOpen = false;
            StatusText = "就绪";
            LastSavedTime = string.Empty;
        });

        WeakReferenceMessenger.Default.Register<StatusUpdateMessage>(this, (r, m) =>
        {
            StatusText = m.Message;
        });

        WeakReferenceMessenger.Default.Register<BusyStateMessage>(this, (r, m) =>
        {
            IsProcessing = m.IsBusy;
            ProcessingLabel = m.IsBusy ? m.Message : string.Empty;
        });

        WeakReferenceMessenger.Default.Register<PythonReadyMessage>(this, (r, m) =>
        {
            IsPythonReady = true;
            PythonVersion = m.Version;
        });

        StartMemoryMonitor();
    }

    private void StartMemoryMonitor()
    {
        _memoryTimer = new System.Timers.Timer(5000);
        _memoryTimer.Elapsed += (_, _) =>
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            MemoryUsageMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
        };
        _memoryTimer.Start();
    }
}
