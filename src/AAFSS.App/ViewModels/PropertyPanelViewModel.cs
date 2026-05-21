using CommunityToolkit.Mvvm.ComponentModel;
using AAFSS.Core.Models;
using AAFSS.Core.Queries;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the property inspector panel.
/// Displays and allows editing of properties for the currently selected
/// entity in the project explorer or active document.
/// </summary>
public partial class PropertyPanelViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _entityName = string.Empty;

    [ObservableProperty]
    private string _entityType = string.Empty;

    [ObservableProperty]
    private ObservablePropertyItem[] _properties = Array.Empty<ObservablePropertyItem>();

    [ObservableProperty]
    private bool _hasSelection;

    public PropertyPanelViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Loads properties for the specified data source.
    /// </summary>
    public async Task LoadDataSourcePropertiesAsync(Guid dataSourceId)
    {
        try
        {
            var query = new GetTimeSeriesDataQuery { DataSourceId = dataSourceId };
            var result = await _mediator.Send(query);
            EntityName = result.FileName;
            EntityType = "数据源";
            HasSelection = true;
            Properties = new[]
            {
                new ObservablePropertyItem("文件路径", result.FilePath),
                new ObservablePropertyItem("采样率", $"{result.SampleRate:F2} Hz"),
                new ObservablePropertyItem("数据点数", $"{result.TotalDataPoints:N0}"),
                new ObservablePropertyItem("通道数", $"{result.ChannelCount}"),
                new ObservablePropertyItem("时间长度", $"{result.DurationSeconds:F2} s"),
                new ObservablePropertyItem("导入时间", result.ImportedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                new ObservablePropertyItem("数据源类型", result.DataSourceType.ToString()),
                new ObservablePropertyItem("传感器类型", result.SensorType.ToString()),
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load properties: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the property panel when nothing is selected.
    /// </summary>
    public void ClearSelection()
    {
        EntityName = string.Empty;
        EntityType = string.Empty;
        Properties = Array.Empty<ObservablePropertyItem>();
        HasSelection = false;
    }
}

/// <summary>
/// Represents a single read-only property row in the property panel.
/// </summary>
public class ObservablePropertyItem
{
    public string Name { get; }
    public string Value { get; }

    public ObservablePropertyItem(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
