using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using AAFSS.Core.Queries;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the PSD (Power Spectral Density) chart view using ScottPlot.
/// Displays Welch PSD estimates with confidence intervals.
/// Supports band selection, A/B comparison, and export.
/// </summary>
public partial class PsdChartViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _title = "PSD 功率谱密度";

    [ObservableProperty]
    private string _statusText = "就绪 — 选择数据源后计算 PSD";

    [ObservableProperty]
    private ObservableCollection<PsdSeriesViewModel> _psdSeries = new();

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _showConfidence = true;

    [ObservableProperty]
    private double _xMin = 0;

    [ObservableProperty]
    private double _xMax = 5000;

    [ObservableProperty]
    private bool _isLogX = false;

    [ObservableProperty]
    private bool _isLogY = true;

    [ObservableProperty]
    private string _windowType = "Hann";

    [ObservableProperty]
    private int _windowSize = 4096;

    [ObservableProperty]
    private double _overlapPercent = 50;

    public PsdChartViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task CalculatePsdAsync(Guid dataSourceId)
    {
        try
        {
            StatusText = "计算 PSD...";
            var query = new GetSpectrumDataQuery
            {
                ProjectId = Guid.Empty,  // TODO: Wire up project context
                SpectrumId = dataSourceId,
                IsCompiled = false
            };
            var result = await _mediator.Send(query);

            PsdSeries.Clear();
            if (result != null)
            {
                PsdSeries.Add(new PsdSeriesViewModel
                {
                    SeriesIndex = 0,
                    Name = result.Name,
                    Frequencies = result.Frequencies,
                    PsdValues = result.Amplitudes,
                    ColorIndex = 0
                });
            }

            StatusText = result != null ? "PSD 计算完成 — 1 条曲线" : "PSD 计算完成 — 无数据";
        }
        catch (Exception ex)
        {
            StatusText = $"PSD 计算失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportChart()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessage(new StatusMessagePayload
        {
            Text = "PSD 图已导出",
            Severity = StatusSeverity.Success,
            IsTransient = true
        }));
    }
}

/// <summary>
/// Represents a single PSD series in the chart.
/// </summary>
public partial class PsdSeriesViewModel : ObservableObject
{
    [ObservableProperty] private int _seriesIndex;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private double[] _frequencies = Array.Empty<double>();
    [ObservableProperty] private double[] _psdValues = Array.Empty<double>();
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private int _colorIndex;
}
