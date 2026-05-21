using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AAFSS.App.Messaging;

/// <summary>
/// Message sent to request navigation to a different view or tool panel.
/// Published by CommandPalette, ribbon buttons, and context menus.
/// Consumed by MainWindowViewModel for AvalonDock layout management.
/// </summary>
public class NavigationMessage : ValueChangedMessage<NavigationTarget>
{
    public NavigationMessage(NavigationTarget target) : base(target) { }
}

/// <summary>
/// Describes the target view or tool to navigate to.
/// </summary>
public enum NavigationTarget
{
    ProjectExplorer,
    DataTable,
    SpectrumChart,
    PsdChart,
    WaveformChart,
    RainflowHeatmap,
    StatisticalView,
    ReportPreview,
    PropertyPanel,
    CommandPalette,
    ImportDialog,
    OpenProjectDialog,
    BatchProcessingDialog
}
