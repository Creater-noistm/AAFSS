using AAFSS.App.ViewModels;
using System.Windows;

namespace AAFSS.App.Views;

/// <summary>
/// Main application window with AvalonDock layout, Fluent Ribbon, and status bar.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        ProjectExplorerView projectExplorer,
        PropertyPanelView propertyPanel,
        BottomPanelView bottomPanel,
        CommandPaletteView commandPalette,
        StatusBarView statusBar)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Wire up AvalonDock content
        var projectExplorerPane = DockManager.Layout.Descendents()
            .OfType<AvalonDock.Layout.LayoutAnchorable>()
            .First(a => a.ContentId == "project-explorer");
        projectExplorerPane.Content = projectExplorer;

        var propertyPane = DockManager.Layout.Descendents()
            .OfType<AvalonDock.Layout.LayoutAnchorable>()
            .First(a => a.ContentId == "property-panel");
        propertyPane.Content = propertyPanel;

        var outputPane = DockManager.Layout.Descendents()
            .OfType<AvalonDock.Layout.LayoutAnchorable>()
            .First(a => a.ContentId == "output-panel");
        outputPane.Content = bottomPanel;

        // Document template selector for different document types
        DockManager.LayoutItemTemplateSelector = new DocumentTemplateSelector();

        // Keyboard shortcuts
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            viewModel.NewProjectCommand,
            System.Windows.Input.Key.N,
            System.Windows.Input.ModifierKeys.Control));

        InputBindings.Add(new System.Windows.Input.KeyBinding(
            viewModel.OpenProjectCommand,
            System.Windows.Input.Key.O,
            System.Windows.Input.ModifierKeys.Control));

        InputBindings.Add(new System.Windows.Input.KeyBinding(
            viewModel.SaveProjectCommand,
            System.Windows.Input.Key.S,
            System.Windows.Input.ModifierKeys.Control));

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Show command palette on Ctrl+Shift+P
        var showPaletteBinding = new System.Windows.Input.KeyBinding(
            new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                var paletteVm = ((MainWindowViewModel)DataContext)
                    .GetType().GetProperty("ServiceProvider")?.GetValue(DataContext);
            }),
            System.Windows.Input.Key.P,
            System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.IsProjectOpen)
        {
            var result = MessageBox.Show(
                "当前项目尚未保存。是否在退出前保存?",
                "AAFSS - 确认退出",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    await vm.SaveProjectCommand.ExecuteAsync(null);
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }
    }
}

/// <summary>
/// Template selector for AvalonDock document panes.
/// Maps ViewModel types to their corresponding View types.
/// </summary>
public class DocumentTemplateSelector : System.Windows.Controls.DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (container is not FrameworkElement element) return null;

        return item switch
        {
            DataTableViewModel => element.FindResource("DataTableTemplate") as DataTemplate,
            SpectrumChartViewModel => element.FindResource("SpectrumChartTemplate") as DataTemplate,
            PsdChartViewModel => element.FindResource("PsdChartTemplate") as DataTemplate,
            WaveformChartViewModel => element.FindResource("WaveformChartTemplate") as DataTemplate,
            RainflowHeatmapViewModel => element.FindResource("RainflowHeatmapTemplate") as DataTemplate,
            ReportPreviewViewModel => element.FindResource("ReportPreviewTemplate") as DataTemplate,
            ProjectExplorerViewModel => element.FindResource("ProjectExplorerTemplate") as DataTemplate,
            _ => null
        };
    }
}
