using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for statistical analysis view — distribution fitting and parameter estimation.
/// </summary>
public partial class StatisticalViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "统计分析";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "就绪";

    [RelayCommand]
    private void FitDistribution()
    {
    }

    [RelayCommand]
    private void ExportResults()
    {
    }
}
