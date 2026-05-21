using AAFSS.App.Messaging;
using AAFSS.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;
using System.Collections.ObjectModel;
using System.Data;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for tabular data display — shows time series or spectrum data in a data grid.
/// </summary>
public partial class DataTableViewModel : DocumentViewModel
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    [ObservableProperty]
    private DataTable? _dataTable;

    [ObservableProperty]
    private DataView? _dataView;

    [ObservableProperty]
    private string _sourceLabel = string.Empty;

    [ObservableProperty]
    private long _totalRows;

    [ObservableProperty]
    private int _totalColumns;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _columnHeaders = new();

    private readonly Dictionary<Guid, string> _sourceNames = new();

    public DataTableViewModel(IMediator mediator, ILogger logger) : base("数据表格")
    {
        _mediator = mediator;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<TreeNodeSelectedMessage>(this, async (r, m) =>
        {
            if (m.NodeType == "DataSource" && m.EntityId.HasValue)
                await LoadTimeSeriesDataAsync(m.EntityId.Value);
        });
    }

    [RelayCommand]
    private async Task LoadTimeSeriesDataAsync(Guid dataSourceId)
    {
        try
        {
            var query = new AAFSS.Core.Queries.GetTimeSeriesDataQuery { DataSourceId = dataSourceId };
            var result = await _mediator.Send(query);

            if (result == null || result.Values.Length == 0)
            {
                _logger.Warning("No time series data found for DataSource {Id}", dataSourceId);
                return;
            }

            var table = new DataTable("TimeSeries");

            // Time column + value columns
            table.Columns.Add("Time (s)", typeof(double));
            ColumnHeaders.Clear();
            ColumnHeaders.Add("Time (s)");

            for (int c = 0; c < result.ChannelCount; c++)
            {
                var colName = result.ChannelNames?.Length > c ? result.ChannelNames[c] : $"Ch{c + 1}";
                table.Columns.Add(colName, typeof(double));
                ColumnHeaders.Add(colName);
            }

            var timeValues = result.TimeValues ?? Enumerable.Range(0, result.Values.Length / Math.Max(1, result.ChannelCount))
                .Select(i => (double)i / result.SampleRate).ToArray();

            for (int r = 0; r < timeValues.Length; r++)
            {
                var row = table.NewRow();
                row[0] = timeValues[r];
                for (int c = 0; c < result.ChannelCount; c++)
                {
                    var idx = r * result.ChannelCount + c;
                    if (idx < result.Values.Length)
                        row[c + 1] = result.Values[idx];
                }
                table.Rows.Add(row);
            }

            DataTable = table;
            DataView = table.DefaultView;
            TotalRows = table.Rows.Count;
            TotalColumns = table.Columns.Count;
            SourceLabel = $"数据源: {result.SourceName} | 采样率: {result.SampleRate:F2} Hz | 通道: {result.ChannelCount}";
            Title = $"数据 - {result.SourceName}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load time series data");
            WeakReferenceMessenger.Default.Send(new OutputMessageAdded(
                new OutputMessage { Level = OutputLevel.Error, Text = $"加载数据失败: {ex.Message}", Source = "DataTable" }));
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        if (DataView == null) return;
        try
        {
            DataView.RowFilter = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            TotalRows = DataView.Count;
        }
        catch
        {
            // Invalid filter expression — ignore
        }
    }
}
