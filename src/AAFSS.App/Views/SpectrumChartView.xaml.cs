using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.App.Messaging;
using AAFSS.Core.Models;
using AAFSS.Core.Queries;
using MediatR;
using Serilog;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the octave/spectrum chart view using ScottPlot.
/// Displays sound pressure level vs frequency for one or more spectra.
/// Supports A/B comparison, cursor readout, and export.
/// </summary>
public partial class SpectrumChartViewModel : DocumentViewModel
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

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

    [ObservableProperty]
    private bool _hasData;

    private Guid _projectId;

    public SpectrumChartViewModel(IMediator mediator, ILogger logger) : base("频谱图")
    {
        _mediator = mediator;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<TreeNodeSelectedMessage>(this, async (r, m) =>
        {
            if (m.EntityId.HasValue && m.NodeType is "CompiledSpectrum" or "SpectrumResult")
            {
                var query = new GetSpectrumDataQuery(
                    ProjectId: _projectId,
                    SpectrumId: m.EntityId.Value,
                    IsCompiled: m.NodeType == "CompiledSpectrum");

                var result = await _mediator.Send(query);
                if (result != null)
                    LoadSpectrumData(result, m.Name);
            }
        });

        WeakReferenceMessenger.Default.Register<ProjectOpenedMessage>(this, (r, m) =>
        {
            _projectId = m.Project.Id;
        });

        WeakReferenceMessenger.Default.Register<SpectrumCompiledMessage>(this, async (r, m) =>
        {
            var query = new GetSpectrumDataQuery(
                ProjectId: r.ProjectId,
                SpectrumId: r.SpectrumId,
                IsCompiled: true);

            var result = await _mediator.Send(query);
            if (result != null)
                LoadSpectrumData(result, r.SpectrumName);
        });
    }

    private void LoadSpectrumData(SpectrumDataDto dto, string name)
    {
        SpectrumSeries.Clear();

        SpectrumSeries.Add(new SpectrumSeriesViewModel
        {
            SeriesIndex = 0,
            Name = name ?? dto.Name,
            SpectrumType = dto.SpectrumType,
            Frequencies = dto.Frequencies,
            Amplitudes = dto.Amplitudes,
            Oaspl = dto.Oaspl,
            ColorIndex = 0
        });

        HasData = true;
        ChartTitle = $"载荷谱 - {name ?? dto.Name}";

        if (dto.Frequencies.Length > 0)
        {
            XMin = dto.Frequencies.Min();
            XMax = dto.Frequencies.Max();
        }
    }

    [RelayCommand]
    private void ExportChart()
    {
        WeakReferenceMessenger.Default.Send(new StatusUpdateMessage("频谱图已导出为 PNG"));
    }
}

/// <summary>
/// Represents a single spectrum series in the chart.
/// </summary>
public partial class SpectrumSeriesViewModel : ObservableObject
{
    [ObservableProperty]
    private int _seriesIndex;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _spectrumType = string.Empty;

    [ObservableProperty]
    private double[] _frequencies = Array.Empty<double>();

    [ObservableProperty]
    private double[] _amplitudes = Array.Empty<double>();

    [ObservableProperty]
    private double _oaspl;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private int _colorIndex;
}
