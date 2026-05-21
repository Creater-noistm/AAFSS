using AAFSS.App.ViewModels;
using System.Windows.Controls;

namespace AAFSS.App.Views;

public partial class RainflowHeatmapView : UserControl
{
    private readonly RainflowHeatmapViewModel _viewModel;

    public RainflowHeatmapView(RainflowHeatmapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(RainflowHeatmapViewModel.HasData))
                UpdatePlot();
        };
    }

    private void UpdatePlot()
    {
        var plt = RainflowPlot.Plot;
        plt.Clear();

        if (!_viewModel.HasData || _viewModel.RainflowMatrix.GetLength(0) == 0) return;

        var matrix = _viewModel.RainflowMatrix;
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        var values = new double[rows * cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                values[r * cols + c] = matrix[r, c];

        var heatmap = plt.Add.Heatmap(values, rows, cols);
        heatmap.Colormap = new ScottPlot.Colormaps.Viridis();

        plt.Title(_viewModel.ChartTitle);
        plt.XLabel("To Level");
        plt.YLabel("From Level");

        RainflowPlot.Refresh();
    }
}
