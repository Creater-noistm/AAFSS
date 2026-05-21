using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AAFSS.Core.Queries;
using MediatR;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for the project explorer tree view.
/// Displays the hierarchical structure: Projects > Mission Profiles > Flight Conditions > Data Sources.
/// Supports drag-drop import and context menu operations.
/// </summary>
public partial class ProjectExplorerViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private ObservableCollection<ProjectTreeNodeViewModel> _rootNodes = new();

    [ObservableProperty]
    private ProjectTreeNodeViewModel? _selectedNode;

    [ObservableProperty]
    private bool _isLoading;

    public ProjectExplorerViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task RefreshTreeAsync()
    {
        IsLoading = true;
        try
        {
            var query = new GetProjectTreeQuery();
            var nodes = await _mediator.Send(query);
            RootNodes = new ObservableCollection<ProjectTreeNodeViewModel>(
                nodes.Select(n => new ProjectTreeNodeViewModel
                {
                    Name = n.Name,
                    NodeType = n.NodeType,
                    EntityId = n.EntityId,
                    Icon = GetIconForType(n.NodeType),
                    Children = new ObservableCollection<ProjectTreeNodeViewModel>(
                        n.Children?.Select(c => MapChildNode(c)) ?? Array.Empty<ProjectTreeNodeViewModel>())
                }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load project tree: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectNode(ProjectTreeNodeViewModel? node)
    {
        SelectedNode = node;
        if (node != null)
        {
            WeakReferenceMessenger.Default.Send(
                new Messaging.NavigationMessage(Messaging.NavigationTarget.PropertyPanel));
        }
    }

    private static ProjectTreeNodeViewModel MapChildNode(GetProjectTreeQuery.TreeNodeDto dto)
    {
        return new ProjectTreeNodeViewModel
        {
            Name = dto.Name,
            NodeType = dto.NodeType,
            EntityId = dto.EntityId,
            Icon = GetIconForType(dto.NodeType),
            Children = new ObservableCollection<ProjectTreeNodeViewModel>(
                dto.Children?.Select(MapChildNode) ?? Array.Empty<ProjectTreeNodeViewModel>())
        };
    }

    private static string GetIconForType(string nodeType) => nodeType switch
    {
        "Project" => "📁",
        "MissionProfile" => "📋",
        "FlightCondition" => "✈️",
        "MeasurementPoint" => "📡",
        "DataSource" => "📊",
        "SpectrumResult" => "📈",
        "RainflowResult" => "🔄",
        "CompiledSpectrum" => "🏗️",
        "Report" => "📄",
        _ => "📎"
    };
}

/// <summary>
/// ViewModel for a single node in the project explorer tree.
/// </summary>
public partial class ProjectTreeNodeViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _nodeType = string.Empty;
    [ObservableProperty] private Guid _entityId;
    [ObservableProperty] private string _icon = "📎";
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private ObservableCollection<ProjectTreeNodeViewModel> _children = new();
}
