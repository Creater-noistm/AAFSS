using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the command palette — provides quick search and command execution.
/// </summary>
public partial class CommandPaletteViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandItem> _commands = new();

    [ObservableProperty]
    private CommandItem? _selectedCommand;

    [ObservableProperty]
    private bool _isVisible;

    private readonly List<CommandItem> _allCommands = new();

    public CommandPaletteViewModel()
    {
        RegisterBuiltInCommands();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterCommands(value);
    }

    private void RegisterBuiltInCommands()
    {
        _allCommands.AddRange(new[]
        {
            new CommandItem("导入数据...", "导入CSV/Excel/TDMS数据文件", "File",
                () => WeakReferenceMessenger.Default.Send(new ShowImportDialogMessage())),
            new CommandItem("新建项目", "创建新的AAFSS项目", "Project",
                () => WeakReferenceMessenger.Default.Send(new NewProjectRequestMessage())),
            new CommandItem("打开项目", "打开现有的AAFSS项目文件", "Project",
                () => WeakReferenceMessenger.Default.Send(new OpenProjectRequestMessage())),
            new CommandItem("保存项目", "保存当前项目", "Project",
                () => WeakReferenceMessenger.Default.Send(new SaveProjectRequestMessage())),
            new CommandItem("计算频谱", "对选中数据执行频谱分析", "Analysis",
                () => WeakReferenceMessenger.Default.Send(new ComputeSpectrumRequestMessage())),
            new CommandItem("编制载荷谱", "启动载荷谱编制流程", "Analysis",
                () => WeakReferenceMessenger.Default.Send(new CompileSpectrumRequestMessage())),
            new CommandItem("雨流计数", "执行雨流循环计数分析", "Analysis",
                () => WeakReferenceMessenger.Default.Send(new RainflowCountRequestMessage())),
            new CommandItem("损伤计算", "计算疲劳损伤值", "Analysis",
                () => WeakReferenceMessenger.Default.Send(new DamageCalculationRequestMessage())),
            new CommandItem("生成报告", "生成载荷谱报告", "Report",
                () => WeakReferenceMessenger.Default.Send(new GenerateReportRequestMessage())),
            new CommandItem("统计分析", "拟合统计分布模型", "Analysis",
                () => WeakReferenceMessenger.Default.Send(new FitDistributionRequestMessage())),
            new CommandItem("信号预处理", "滤波、去趋势、降采样", "Processing",
                () => WeakReferenceMessenger.Default.Send(new PreprocessSignalRequestMessage())),
            new CommandItem("批量处理", "批量执行分析任务", "Processing",
                () => WeakReferenceMessenger.Default.Send(new BatchProcessingRequestMessage())),
        });

        FilterCommands(string.Empty);
    }

    private void FilterCommands(string filter)
    {
        Commands.Clear();
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allCommands
            : _allCommands.FindAll(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(filter, StringComparison.OrdinalIgnoreCase));

        foreach (var cmd in filtered)
            Commands.Add(cmd);

        if (Commands.Count > 0)
            SelectedCommand = Commands[0];
    }

    [RelayCommand]
    private void ExecuteCommand()
    {
        SelectedCommand?.ExecuteCommand?.Execute(null);
        IsVisible = false;
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void NavigateUp()
    {
        if (Commands.Count == 0) return;
        var idx = SelectedCommand != null ? Commands.IndexOf(SelectedCommand) : 0;
        idx = idx <= 0 ? Commands.Count - 1 : idx - 1;
        SelectedCommand = Commands[idx];
    }

    [RelayCommand]
    private void NavigateDown()
    {
        if (Commands.Count == 0) return;
        var idx = SelectedCommand != null ? Commands.IndexOf(SelectedCommand) : -1;
        idx = idx >= Commands.Count - 1 ? 0 : idx + 1;
        SelectedCommand = Commands[idx];
    }
}

/// <summary>
/// Represents a single command in the command palette.
/// </summary>
public partial class CommandItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private string _category;

    [ObservableProperty]
    private ICommand? _executeCommand;

    public CommandItem(string name, string description, string category, Action execute)
    {
        Name = name;
        Description = description;
        Category = category;
        ExecuteCommand = new RelayCommand(execute);
    }
}
