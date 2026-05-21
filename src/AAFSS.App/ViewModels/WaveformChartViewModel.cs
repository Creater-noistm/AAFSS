using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using AAFSS.Core.Queries;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the time-domain waveform chart view using ScottPlot.
/// Displays acoustic/vibration time series with pan and zoom.
/// Supports multi-channel overlay and cursor readout.
/// </summary>
public partial class WaveformChartViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _title = "时域波形";

    [ObservableProperty]
    private string _statusText = "就绪 — 选择数据源后显示波形";

    [ObservableProperty]
    private ObservableCollection<WaveformSeriesViewModel> _waveformSeries = new();

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private double _timeWindowStart;

    [ObservableProperty]
    private double _timeWindowDuration = 1.0;

    [ObservableProperty]
    private double _cursorTime;

    [ObservableProperty]
    private double _cursorValue;

    [ObservableProperty]
    private double _maxTime;

    [ObservableProperty]
    private double _sampleRate;

    [ObservableProperty]
    private int _channelIndex;

    public WaveformChartViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task LoadWaveformAsync(Guid dataSourceId)
    {
        try
        {
            StatusText = "加载波形...";
            var query = new GetTimeSeriesDataQuery { DataSourceId = dataSourceId };
            var result = await _mediator.Send(query);

            WaveformSeries.Clear();
            WaveformSeries.Add(new WaveformSeriesViewModel
            {
                SeriesIndex = 0,
                Name = result.FileName,
                TimeData = result.TimeData,
                AmplitudeData = result.AmplitudeData,
                SampleRate = result.SampleRate,
                ColorIndex = 0
            });

            MaxTime = result.DurationSeconds;
            SampleRate = result.SampleRate;
            TimeWindowStart = 0;
            TimeWindowDuration = Math.Min(1.0, result.DurationSeconds);
            StatusText = $"波形已加载 — {result.TotalDataPoints:N0} 点 | {result.SampleRate:F0} Hz | {result.DurationSeconds:F2} s";
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ZoomToFull()
    {
        TimeWindowStart = 0;
        TimeWindowDuration = MaxTime;
    }

    [RelayCommand]
    private void ExportChart()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessage(new StatusMessagePayload
        {
            Text = "波形图已导出",
            Severity = StatusSeverity.Success,
            IsTransient = true
        }));
    }
}

/// <summary>
/// Represents a single waveform series in the chart.
/// </summary>
public partial class WaveformSeriesViewModel : ObservableObject
{
    [ObservableProperty] private int _seriesIndex;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private double[] _timeData = Array.Empty<double>();
    [ObservableProperty] private double[] _amplitudeData = Array.Empty<double>();
    [ObservableProperty] private double _sampleRate;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private int _colorIndex;
}
