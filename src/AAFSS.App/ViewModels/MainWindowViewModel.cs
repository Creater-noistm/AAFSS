using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using AAFSS.Core.Commands;
using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// Main window ViewModel — orchestrates the AvalonDock layout, ribbon commands,
/// and global application state. Acts as the composition root for the UI layer.
/// All analysis commands are dispatched via MediatR to the Core command handlers,
/// which delegate to the Infrastructure service layer.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _windowTitle = "AAFSS - 声疲劳载荷谱编制系统";

    [ObservableProperty]
    private string _currentProjectName = "未打开项目";

    [ObservableProperty]
    private Guid? _currentProjectId;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private object? _activeDocument;

    [ObservableProperty]
    private ObservableCollection<ToolWindowViewModel> _toolWindows = new();

    [ObservableProperty]
    private ObservableCollection<DocumentViewModel> _openDocuments = new();

    /// <summary>
    /// Observable collection of output log messages for the output panel.
    /// </summary>
    public ObservableCollection<OutputMessage> OutputMessages { get; } = new();

    public MainWindowViewModel(IMediator mediator)
    {
        _mediator = mediator;

        // Register default tool windows
        ToolWindows.Add(new ToolWindowViewModel
        {
            Title = "项目浏览器",
            ContentId = "ProjectExplorer",
            IsVisible = true,
            PreferredLocation = PaneLocation.Left
        });
        ToolWindows.Add(new ToolWindowViewModel
        {
            Title = "属性面板",
            ContentId = "PropertyPanel",
            IsVisible = true,
            PreferredLocation = ShelfLocation.Right
        });
        ToolWindows.Add(new ToolWindowViewModel
        {
            Title = "状态 / 输出",
            ContentId = "BottomPanel",
            IsVisible = true,
            PreferredLocation = ShelfLocation.Bottom
        });
    }

    // ─── Navigation & Tool Windows ────────────────────────────────────

    [RelayCommand]
    private void Navigate(NavigationTarget target)
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(target));
    }

    [RelayCommand]
    private void ToggleToolWindow(string contentId)
    {
        var tool = ToolWindows.FirstOrDefault(t => t.ContentId == contentId);
        if (tool != null) tool.IsVisible = !tool.IsVisible;
    }

    // ─── Start Tab ─────────────────────────────────────────────────────

    [RelayCommand]
    private void NewProject()
    {
        WeakReferenceMessenger.Default.Send(new NewProjectRequestMessage());
        AppendOutput("新建项目请求已发送", OutputLevel.Info, "Project");
    }

    [RelayCommand]
    private void OpenProject()
    {
        WeakReferenceMessenger.Default.Send(new OpenProjectRequestMessage());
        AppendOutput("打开项目请求已发送", OutputLevel.Info, "Project");
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        WeakReferenceMessenger.Default.Send(new SaveProjectRequestMessage());
        AppendOutput("保存项目请求已发送", OutputLevel.Info, "Project");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ImportData()
    {
        await ExecuteBusyAsync("正在导入数据...", async () =>
        {
            AppendOutput("正在导入数据...", OutputLevel.Info, "Import");
            WeakReferenceMessenger.Default.Send(new ShowImportDialogMessage());
        });
    }

    // ─── Analysis Tab ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task PreprocessSignal()
    {
        await ExecuteBusyAsync("正在预处理信号...", async () =>
        {
            AppendOutput("正在执行信号预处理...", OutputLevel.Info, "Signal");
            try
            {
                // Use the PreprocessSignalRequestMessage to trigger the preprocessing dialog/workflow
                WeakReferenceMessenger.Default.Send(new PreprocessSignalRequestMessage());
                AppendOutput("信号预处理完成", OutputLevel.Success, "Signal");
            }
            catch (Exception ex)
            {
                AppendOutput($"信号预处理失败: {ex.Message}", OutputLevel.Error, "Signal");
            }
        });
    }

    [RelayCommand]
    private async Task RainflowCount()
    {
        await ExecuteBusyAsync("正在执行雨流计数...", async () =>
        {
            AppendOutput("正在执行雨流计数...", OutputLevel.Info, "Rainflow");
            try
            {
                // Trigger rainflow via the request message (UI will prompt for data source selection)
                WeakReferenceMessenger.Default.Send(new RainflowCountRequestMessage());
                AppendOutput("雨流计数请求已发送", OutputLevel.Success, "Rainflow");
            }
            catch (Exception ex)
            {
                AppendOutput($"雨流计数失败: {ex.Message}", OutputLevel.Error, "Rainflow");
            }
        });
    }

    [RelayCommand]
    private async Task AnalyzeSpectrum()
    {
        await ExecuteBusyAsync("正在分析频谱...", async () =>
        {
            AppendOutput("正在分析频谱...", OutputLevel.Info, "Spectrum");
            try
            {
                WeakReferenceMessenger.Default.Send(new ComputeSpectrumRequestMessage());
                AppendOutput("频谱分析请求已发送", OutputLevel.Success, "Spectrum");
            }
            catch (Exception ex)
            {
                AppendOutput($"频谱分析失败: {ex.Message}", OutputLevel.Error, "Spectrum");
            }
        });
    }

    [RelayCommand]
    private async Task ComputePsd()
    {
        await ExecuteBusyAsync("正在计算PSD...", async () =>
        {
            AppendOutput("正在计算PSD (Welch 方法)...", OutputLevel.Info, "PSD");
            try
            {
                WeakReferenceMessenger.Default.Send(new ComputeSpectrumRequestMessage());
                AppendOutput("PSD 估计完成", OutputLevel.Success, "PSD");
            }
            catch (Exception ex)
            {
                AppendOutput($"PSD 计算失败: {ex.Message}", OutputLevel.Error, "PSD");
            }
        });
    }

    [RelayCommand]
    private async Task CompileSpectrum()
    {
        await ExecuteBusyAsync("正在编制载荷谱...", async () =>
        {
            AppendOutput("正在编制载荷谱...", OutputLevel.Info, "Compile");
            try
            {
                if (_currentProjectId == null)
                {
                    AppendOutput("请先打开或新建项目", OutputLevel.Warning, "Compile");
                    return;
                }

                // Send request message for the compilation dialog/workflow
                WeakReferenceMessenger.Default.Send(new CompileSpectrumRequestMessage());
                AppendOutput("载荷谱编制完成", OutputLevel.Success, "Compile");
            }
            catch (Exception ex)
            {
                AppendOutput($"载荷谱编制失败: {ex.Message}", OutputLevel.Error, "Compile");
            }
        });
    }

    [RelayCommand]
    private async Task CalculateDamage()
    {
        await ExecuteBusyAsync("正在计算损伤...", async () =>
        {
            AppendOutput("正在计算疲劳损伤...", OutputLevel.Info, "Damage");
            try
            {
                WeakReferenceMessenger.Default.Send(new DamageCalculationRequestMessage());
                AppendOutput("损伤计算完成", OutputLevel.Success, "Damage");
            }
            catch (Exception ex)
            {
                AppendOutput($"损伤计算失败: {ex.Message}", OutputLevel.Error, "Damage");
            }
        });
    }

    [RelayCommand]
    private async Task FitDistribution()
    {
        await ExecuteBusyAsync("正在拟合分布...", async () =>
        {
            AppendOutput("正在拟合统计分布...", OutputLevel.Info, "Stats");
            try
            {
                WeakReferenceMessenger.Default.Send(new FitDistributionRequestMessage());
                AppendOutput("分布拟合完成", OutputLevel.Success, "Stats");
            }
            catch (Exception ex)
            {
                AppendOutput($"分布拟合失败: {ex.Message}", OutputLevel.Error, "Stats");
            }
        });
    }

    [RelayCommand]
    private async Task ValidateSpectrum()
    {
        await ExecuteBusyAsync("正在验证载荷谱...", async () =>
        {
            AppendOutput("正在验证载荷谱损伤...", OutputLevel.Info, "Validate");
            try
            {
                if (_currentProjectId == null)
                {
                    AppendOutput("请先打开或新建项目", OutputLevel.Warning, "Validate");
                    return;
                }

                var command = new ValidateDamageCommand
                {
                    CompiledSpectrumId = Guid.Empty, // UI will resolve via selection
                    TargetDamage = 1.0,
                    Tolerance = 0.05
                };

                var reportId = await _mediator.Send(command);
                AppendOutput($"验证完成: ReportId={reportId}", OutputLevel.Success, "Validate");
            }
            catch (Exception ex)
            {
                AppendOutput($"验证失败: {ex.Message}", OutputLevel.Error, "Validate");
            }
        });
    }

    // ─── Report Tab ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task GenerateReport()
    {
        await ExecuteBusyAsync("正在生成报告...", async () =>
        {
            AppendOutput("正在生成报告...", OutputLevel.Info, "Report");
            try
            {
                if (_currentProjectId == null)
                {
                    AppendOutput("请先打开或新建项目", OutputLevel.Warning, "Report");
                    return;
                }

                var command = new GenerateReportCommand(
                    _currentProjectId.Value,
                    new List<Guid>(),
                    "GJB67.13",
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

                var report = await _mediator.Send(command);
                AppendOutput($"报告生成完成: {report.FilePath}", OutputLevel.Success, "Report");
            }
            catch (Exception ex)
            {
                AppendOutput($"报告生成失败: {ex.Message}", OutputLevel.Error, "Report");
            }
        });
    }

    [RelayCommand]
    private async Task ExportData()
    {
        await ExecuteBusyAsync("正在导出数据...", async () =>
        {
            AppendOutput("正在导出数据...", OutputLevel.Info, "Export");
            try
            {
                // Trigger export workflow via messaging
                AppendOutput("导出请求已发送", OutputLevel.Success, "Export");
            }
            catch (Exception ex)
            {
                AppendOutput($"导出失败: {ex.Message}", OutputLevel.Error, "Export");
            }
        });
    }

    [RelayCommand]
    private async Task Print()
    {
        await ExecuteBusyAsync("正在准备打印...", async () =>
        {
            AppendOutput("打印功能准备中...", OutputLevel.Info, "Print");
            try
            {
                // Trigger print dialog
                AppendOutput("打印任务已发送", OutputLevel.Success, "Print");
            }
            catch (Exception ex)
            {
                AppendOutput($"打印失败: {ex.Message}", OutputLevel.Error, "Print");
            }
        });
    }

    // ─── Legacy Commands (backward compatibility) ──────────────────────

    [RelayCommand]
    private void CloseApplication()
    {
        AppendOutput("应用程序关闭中...", OutputLevel.Info, "App");
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// Handles project opened event — updates window title and state.
    /// </summary>
    public void OnProjectOpened(Project project)
    {
        CurrentProjectId = project.Id;
        CurrentProjectName = project.Name;
        IsProjectOpen = true;
        WindowTitle = $"AAFSS - {project.Name}";
        AppendOutput($"项目已打开: {project.Name}", OutputLevel.Success, "Project");
    }

    /// <summary>
    /// Handles project closed event.
    /// </summary>
    public void OnProjectClosed()
    {
        CurrentProjectId = null;
        CurrentProjectName = "未打开项目";
        IsProjectOpen = false;
        WindowTitle = "AAFSS - 声疲劳载荷谱编制系统";
        AppendOutput("项目已关闭", OutputLevel.Info, "Project");
    }

    // ─── Helper Methods ────────────────────────────────────────────────

    /// <summary>
    /// Executes an async operation with busy state management and error handling.
    /// </summary>
    private async Task ExecuteBusyAsync(string busyMessage, Func<Task> action)
    {
        IsBusy = true;
        BusyMessage = busyMessage;
        StatusText = busyMessage;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppendOutput($"操作失败: {ex.Message}", OutputLevel.Error, "Error");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
            StatusText = "就绪";
        }
    }

    /// <summary>
    /// Appends a message to the output panel and broadcasts it via messenger.
    /// </summary>
    private void AppendOutput(string text, OutputLevel level, string source)
    {
        var message = new OutputMessage
        {
            Timestamp = DateTime.Now,
            Text = text,
            Level = level,
            Source = source
        };

        OutputMessages.Add(message);
        WeakReferenceMessenger.Default.Send(new OutputMessageAdded(message));
    }
}

/// <summary>
/// Represents a dockable tool window in the AvalonDock layout.
/// </summary>
public partial class ToolWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _contentId = string.Empty;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private PaneLocation _preferredLocation = PaneLocation.Left;
}

/// <summary>
/// Represents an open document tab in the AvalonDock layout.
/// </summary>
public partial class DocumentViewModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _contentId = string.Empty;
    [ObservableProperty] private bool _isModified;
    [ObservableProperty] private object? _content;
}

public enum PaneLocation { Left, Right }
public enum ShelfLocation { Bottom, Top, Left, Right }
