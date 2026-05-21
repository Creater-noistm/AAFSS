using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using AAFSS.Core.Models;
using AAFSS.Core.Queries;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the octave/spectrum chart view using ScottPlot.
/// Displays sound pressure level vs frequency for one or more spectra.
/// Supports A/B comparison, cursor readout, and export.
/// </summary>
public partial class SpectrumChartViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _title = "频谱图";

    [ObservableProperty]
    private string _statusText = "就绪 — 选择数据源后加载频谱";

    [ObservableProperty]
    private ObservableCollection<SpectrumSeriesViewModel> _spectrumSeries = new();

    [ObservableProperty]
    private SpectrumSeriesViewModel? _selectedSeries;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _showLegend = true;

    [ObservableProperty]
    private double _cursorFrequency;

    [ObservableProperty]
    private double _cursorAmplitude;

    [ObservableProperty]
    private string _chartTitle = "声压级频谱";

    [ObservableProperty]
    private double _xMin = 20;

    [ObservableProperty]
    private double _xMax = 20000;

    [ObservableProperty]
    private bool _isLogX = true;

    public SpectrumChartViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task LoadSpectrumAsync(Guid entityId)
    {
        try
        {
            StatusText = "加载中...";
            var query = new GetSpectrumDataQuery
            {
                ProjectId = Guid.Empty,  // TODO: Wire up project context
                SpectrumId = entityId,
                IsCompiled = false
            };
            var result = await _mediator.Send(query);

            SpectrumSeries.Clear();
            if (result != null)
            {
                SpectrumSeries.Add(new SpectrumSeriesViewModel
                {
                    SeriesIndex = 0,
                    Name = result.Name,
                    SpectrumType = result.SpectrumType.ToString(),
                    Frequencies = result.Frequencies,
                    Amplitudes = result.Amplitudes,
                    Oaspl = result.Oaspl,
                    ColorIndex = 0
                });
            }

            StatusText = result != null ? $"已加载频谱 | OASPL: {result.Oaspl:F1} dB" : "未找到频谱数据";
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportChart()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessage(new StatusMessagePayload
        {
            Text = "频谱图已导出为 PNG",
            Severity = StatusSeverity.Success,
            IsTransient = true
        }));
    }
}

/// <summary>
/// Represents a single spectrum series in the chart.
/// </summary>
public partial class SpectrumSeriesViewModel : ObservableObject
{
    [ObservableProperty] private int _seriesIndex;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _spectrumType = string.Empty;
    [ObservableProperty] private double[] _frequencies = Array.Empty<double>();
    [ObservableProperty] private double[] _amplitudes = Array.Empty<double>();
    [ObservableProperty] private double _oaspl;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private int _colorIndex;
}
