using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve the hierarchical project tree for display in the project explorer.
/// Returns a structured list of ProjectTreeNode records representing
/// Projects → Profiles → DataSources → Spectra → Reports.
/// </summary>
public record GetProjectTreeQuery(Guid ProjectId) : IRequest<List<ProjectTreeNode>>;
