using AAFSS.App.ViewModels;
using System.Windows.Controls;
using ScottPlot;

namespace AAFSS.App.Views;

public partial class PsdChartView : UserControl
{
    private readonly PsdChartViewModel _viewModel;

    public PsdChartView(PsdChartViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PsdChartViewModel.HasData))
                UpdatePlot();
        };
    }

    private void UpdatePlot()
    {
        var plt = PsdPlot.Plot;
        plt.Clear();

        if (!_viewModel.HasData || _viewModel.Frequencies.Length == 0) return;

        var scatter = plt.Add.Scatter(_viewModel.Frequencies, _viewModel.PsdValues);
        scatter.LineWidth = 1.5;
        scatter.Color = ScottPlot.Color.FromHex("#E65100");

        plt.Title(_viewModel.ChartTitle);
        plt.XLabel(_viewModel.XLabel);
        plt.YLabel(_viewModel.YLabel);
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

        PsdPlot.Refresh();
    }
}
