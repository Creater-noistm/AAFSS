using AAFSS.App.ViewModels;
using System.Windows.Controls;
using ScottPlot;

namespace AAFSS.App.Views;

public partial class WaveformChartView : UserControl
{
    private readonly WaveformChartViewModel _viewModel;

    public WaveformChartView(WaveformChartViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(WaveformChartViewModel.HasData) ||
                e.PropertyName == nameof(WaveformChartViewModel.TimeValues))
                UpdatePlot();
        };
    }

    private void UpdatePlot()
    {
        var plt = WaveformPlot.Plot;
        plt.Clear();

        if (!_viewModel.HasData || _viewModel.TimeValues.Length == 0) return;

        var signal = plt.Add.Signal(_viewModel.SignalValues, _viewModel.SampleRate > 0 ? 1.0 / _viewModel.SampleRate : 1);
        signal.LineWidth = 1;
        signal.Color = ScottPlot.Color.FromHex("#2E7D32");

        plt.Title(_viewModel.ChartTitle);
        plt.XLabel(_viewModel.XLabel);
        plt.YLabel(_viewModel.YLabel);
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

        WaveformPlot.Refresh();
    }
}
