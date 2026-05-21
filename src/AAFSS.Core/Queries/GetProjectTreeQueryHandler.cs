using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries;

/// <summary>
/// Handles <see cref="GetProjectTreeQuery"/> by loading the project aggregate
/// and building a hierarchical tree structure from its entities.
/// </summary>
public class GetProjectTreeQueryHandler : IRequestHandler<GetProjectTreeQuery, List<ProjectTreeNode>>
{
    private readonly IProjectManagementService _projectService;
    private readonly ILogger<GetProjectTreeQueryHandler> _logger;

    public GetProjectTreeQueryHandler(
        IProjectManagementService projectService,
        ILogger<GetProjectTreeQueryHandler> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public async Task<List<ProjectTreeNode>> Handle(GetProjectTreeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Building project tree for ProjectId={ProjectId}", request.ProjectId);

        var project = await _projectService.GetProjectByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            _logger.LogWarning("Project not found: ProjectId={ProjectId}", request.ProjectId);
            return new List<ProjectTreeNode>();
        }

        var tree = new List<ProjectTreeNode>();

        // Profiles node
        var profilesNode = new ProjectTreeNode
        {
            Name = $"Profiles ({project.Profiles.Count})",
            NodeType = "ProfilesGroup",
            EntityId = null,
            Status = ProcessingStatus.Completed,
            Children = project.Profiles.Select(p => new ProjectTreeNode
            {
                Name = p.Name,
                NodeType = "MissionProfile",
                EntityId = p.Id,
                Status = ProcessingStatus.Completed,
                Children = BuildDataSourceNodes(p)
            }).ToList()
        };
        tree.Add(profilesNode);

        // Spectra node
        var spectraNode = new ProjectTreeNode
        {
            Name = $"Spectra ({project.Spectra.Count})",
            NodeType = "SpectraGroup",
            EntityId = null,
            Status = ProcessingStatus.Completed,
            Children = project.Spectra.Select(s => new ProjectTreeNode
            {
                Name = s.Name,
                NodeType = "CompiledSpectrum",
                EntityId = s.Id,
                Status = s.ValidationStatus switch
                {
                    ValidationStatus.Passed => ProcessingStatus.Completed,
                    ValidationStatus.Failed => ProcessingStatus.Failed,
                    ValidationStatus.Warning => ProcessingStatus.Completed,
                    _ => ProcessingStatus.Pending
                },
                Children = new List<ProjectTreeNode>()
            }).ToList()
        };
        tree.Add(spectraNode);

        // Reports node
        var reportsNode = new ProjectTreeNode
        {
            Name = $"Reports ({project.Reports.Count})",
            NodeType = "ReportsGroup",
            EntityId = null,
            Status = ProcessingStatus.Completed,
            Children = project.Reports.Select(r => new ProjectTreeNode
            {
                Name = $"{r.TemplateName} - {r.GeneratedAt:yyyy-MM-dd HH:mm}",
                NodeType = "GeneratedReport",
                EntityId = r.Id,
                Status = r.Status switch
                {
                    ReportStatus.Generated or ReportStatus.Approved or ReportStatus.Archived
                        => ProcessingStatus.Completed,
                    ReportStatus.Error => ProcessingStatus.Failed,
                    _ => ProcessingStatus.Pending
                }
            }).ToList()
        };
        tree.Add(reportsNode);

        return tree;
    }

    private static List<ProjectTreeNode> BuildDataSourceNodes(MissionProfile profile)
    {
        var dataSources = profile.DataSources ?? new List<DataSource>();
        return dataSources.Select(ds => new ProjectTreeNode
        {
            Name = Path.GetFileName(ds.FilePath),
            NodeType = "DataSource",
            EntityId = ds.Id,
            Status = DetermineDataSourceStatus(ds),
            Children = BuildOutputNodes(ds)
        }).ToList();
    }

    private static List<ProjectTreeNode> BuildOutputNodes(DataSource ds)
    {
        var children = new List<ProjectTreeNode>();

        // Spectrum results
        if (ds.SpectrumResults?.Count > 0)
        {
            children.Add(new ProjectTreeNode
            {
                Name = $"Spectra ({ds.SpectrumResults.Count})",
                NodeType = "SpectraResultsGroup",
                EntityId = null,
                Status = ProcessingStatus.Completed,
                Children = ds.SpectrumResults.Select(sr => new ProjectTreeNode
                {
                    Name = sr.SpectrumType.ToString(),
                    NodeType = "SpectrumResult",
                    EntityId = sr.Id,
                    Status = ProcessingStatus.Completed
                }).ToList()
            });
        }

        // Rainflow results
        if (ds.RainflowResults?.Count > 0)
        {
            children.Add(new ProjectTreeNode
            {
                Name = $"Rainflow ({ds.RainflowResults.Count})",
                NodeType = "RainflowResultsGroup",
                EntityId = null,
                Status = ProcessingStatus.Completed,
                Children = ds.RainflowResults.Select(rr => new ProjectTreeNode
                {
                    Name = $"Cycles: {rr.TotalCycles:N0}",
                    NodeType = "RainflowResult",
                    EntityId = rr.Id,
                    Status = ProcessingStatus.Completed,
                    Children = rr.StatisticalModels.Select(sm => new ProjectTreeNode
                    {
                        Name = sm.DistributionType.ToString(),
                        NodeType = "StatisticalModel",
                        EntityId = sm.Id,
                        Status = ProcessingStatus.Completed
                    }).ToList()
                }).ToList()
            });
        }

        return children;
    }

    private static ProcessingStatus DetermineDataSourceStatus(DataSource ds)
    {
        if (ds.ProcessingSteps == null || ds.ProcessingSteps.Count == 0)
            return ProcessingStatus.Pending;

        var lastStep = ds.ProcessingSteps.OrderBy(s => s.StepOrder).Last();
        return lastStep.Status;
    }
}
