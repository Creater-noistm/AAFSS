using System.Windows;
using System.Windows.Controls;

namespace AAFSS.App.Views;

/// <summary>
/// DataTemplate selector for mapping tool window content IDs to their corresponding views.
/// </summary>
public class ViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ProjectExplorerTemplate { get; set; }
    public DataTemplate? PropertyPanelTemplate { get; set; }
    public DataTemplate? BottomPanelTemplate { get; set; }
    public DataTemplate? DataTableTemplate { get; set; }
    public DataTemplate? SpectrumChartTemplate { get; set; }
    public DataTemplate? PsdChartTemplate { get; set; }
    public DataTemplate? WaveformChartTemplate { get; set; }
    public DataTemplate? RainflowHeatmapTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is ViewModels.ToolWindowViewModel tool)
        {
            return tool.ContentId switch
            {
                "ProjectExplorer" => ProjectExplorerTemplate,
                "PropertyPanel" => PropertyPanelTemplate,
                "BottomPanel" => BottomPanelTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }

        return base.SelectTemplate(item, container);
    }
}
