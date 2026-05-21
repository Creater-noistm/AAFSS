using System.Windows.Input;

namespace AAFSS.App.Controls;

/// <summary>
/// Static routed commands for SpectrumChartControl toolbar buttons.
/// </summary>
public static class SpectrumChartCommands
{
    public static readonly RoutedUICommand ToggleGrid =
        new("切换网格", nameof(ToggleGrid), typeof(SpectrumChartCommands));

    public static readonly RoutedUICommand ToggleLegend =
        new("切换图例", nameof(ToggleLegend), typeof(SpectrumChartCommands));

    public static readonly RoutedUICommand ToggleLogScale =
        new("切换对数坐标", nameof(ToggleLogScale), typeof(SpectrumChartCommands));

    public static readonly RoutedUICommand ExportPng =
        new("导出PNG", nameof(ExportPng), typeof(SpectrumChartCommands));
}

/// <summary>
/// Static routed commands for DataGridControl toolbar buttons.
/// </summary>
public static class DataGridCommands
{
    public static readonly RoutedUICommand ExportCsv =
        new("导出CSV", nameof(ExportCsv), typeof(DataGridCommands));
}
