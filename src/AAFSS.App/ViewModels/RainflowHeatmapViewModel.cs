using AAFSS.App.Messaging;
using AAFSS.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the rainflow heatmap — displays rainflow cycle counting results as a matrix.
/// </summary>
public partial class RainflowHeatmapViewModel : DocumentViewModel
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    [ObservableProperty]
    private double[,] _rainflowMatrix = new double[0, 0];

    [ObservableProperty]
    private double[] _fromLevels = Array.Empty<double>();

    [ObservableProperty]
    private double[] _toLevels = Array.Empty<double>();

    [ObservableProperty]
    private int _binCount = 64;

    [ObservableProperty]
    private string _chartTitle = "雨流计数矩阵";

    [ObservableProperty]
    private int _totalCycles;

    [ObservableProperty]
    private double _maxRange;

    [ObservableProperty]
    private double _meanRange;

    [ObservableProperty]
    private double[] _rangeDistribution = Array.Empty<double>();

    [ObservableProperty]
    private double[] _meanDistribution = Array.Empty<double>();

    [ObservableProperty]
    private bool _hasData;

    public RainflowHeatmapViewModel(IMediator mediator, ILogger logger) : base("雨流计数")
    {
        _mediator = mediator;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<TreeNodeSelectedMessage>(this, async (r, m) =>
        {
            if (m.EntityId.HasValue && m.NodeType == "RainflowResult")
                await LoadRainflowAsync(m.EntityId.Value, m.Name);
        });

        WeakReferenceMessenger.Default.Register<RainflowCountRequestMessage>(this, async (r, m) =>
        {
            if (m.DataSourceId.HasValue)
                await LoadRainflowAsync(m.DataSourceId.Value, "雨流计数结果");
        });
    }

    [RelayCommand]
    private async Task LoadRainflowAsync(Guid resultId, string name = "")
    {
        try
        {
            var command = new AAFSS.Core.Commands.RainflowCountCommand
            {
                DataSourceId = resultId,
                ChannelIndex = 0,
                BinCount = BinCount
            };
            var result = await _mediator.Send(command);

            if (result == null || result.FromLevels.Length == 0) return;

            FromLevels = result.FromLevels;
            ToLevels = result.ToLevels;
            RainflowMatrix = result.Matrix;
            TotalCycles = result.FromLevels.Sum();
            ChartTitle = $"雨流计数 - {name ?? "未命名"}";
            HasData = true;

            if (result.FromLevels.Length > 0 && result.ToLevels.Length > 0)
            {
                var ranges = new List<double>();
                for (int i = 0; i < result.FromLevels.Length; i++)
                    ranges.Add(Math.Abs(result.ToLevels[i] - result.FromLevels[i]));
                MaxRange = ranges.Count > 0 ? ranges.Max() : 0;
                MeanRange = ranges.Count > 0 ? ranges.Average() : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load rainflow data");
            HasData = false;
        }
    }
}
