using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for project-level operations: creation, loading, saving, and lifecycle management.
/// </summary>
public interface IProjectManagementService
{
    /// <summary>Creates a new project with default configuration.</summary>
    Task<Project> CreateProjectAsync(string name, string? description = null, CancellationToken ct = default);

    /// <summary>Opens an existing project from a .aafss file.</summary>
    Task<Project> OpenProjectAsync(string filePath, CancellationToken ct = default);

    /// <summary>Saves the current project to its file path.</summary>
    Task SaveProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Closes a project without saving.</summary>
    Task CloseProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Deletes a project and all associated data.</summary>
    Task DeleteProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Gets the list of all projects.</summary>
    Task<List<Project>> GetAllProjectsAsync(CancellationToken ct = default);

    /// <summary>Gets a project by ID with all related entities loaded.</summary>
    Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Gets recent projects for the welcome screen.</summary>
    Task<List<Project>> GetRecentProjectsAsync(int count = 10, CancellationToken ct = default);
}
