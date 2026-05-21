using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.ProjectManagement;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Implementation of IProjectManagementService.
/// Manages project lifecycle: create, open, save, close, delete.
/// </summary>
public class ProjectManagementService : IProjectManagementService
{
    private readonly IUnitOfWork _uow;
    private readonly AafssProjectFile _projectFile;
    private readonly RecentProjectsService _recentProjects;
    private readonly AafssAutoSaveService _autoSave;

    public ProjectManagementService(
        IUnitOfWork uow,
        AafssProjectFile projectFile,
        RecentProjectsService recentProjects,
        AafssAutoSaveService autoSave)
    {
        _uow = uow;
        _projectFile = projectFile;
        _recentProjects = recentProjects;
        _autoSave = autoSave;
    }

    public async Task<Core.Models.Project> CreateProjectAsync(string name, string? description = null, CancellationToken ct = default)
    {
        var project = new Core.Models.Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await _uow.Projects.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        return project;
    }

    public async Task<Core.Models.Project> OpenProjectAsync(string filePath, CancellationToken ct = default)
    {
        var hdf5Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AAFSS", "Hdf5Data");

        var (project, _) = await _projectFile.LoadAsync(filePath, hdf5Dir, ct);

        // Save to database
        await _uow.Projects.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        await _recentProjects.AddOrUpdateAsync(project.Id, project.Name, filePath);
        _autoSave.SetCurrentProject(project.Id, filePath);

        return project;
    }

    public async Task SaveProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _uow.Projects.GetByIdAsync(projectId, ct)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var filePath = project.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AAFSS",
                $"{project.Name}.aafss");
        }

        var hdf5Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AAFSS", "Hdf5Data");

        await _projectFile.SaveAsync(project, filePath, hdf5Dir, ct);
        await _uow.SaveChangesAsync(ct);

        await _recentProjects.AddOrUpdateAsync(project.Id, project.Name, filePath);
    }

    public async Task CloseProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        _autoSave.ClearProject();
        await Task.CompletedTask;
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await _uow.Projects.DeleteAsync(projectId, ct);
        await _uow.SaveChangesAsync(ct);
        await _recentProjects.RemoveAsync(projectId);
    }

    public async Task<List<Core.Models.Project>> GetAllProjectsAsync(CancellationToken ct = default)
    {
        return await _uow.Projects.GetAllAsync(ct);
    }

    public async Task<Core.Models.Project?> GetProjectByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _uow.Projects.GetByIdAsync(projectId, ct);
    }

    public async Task<List<Core.Models.Project>> GetRecentProjectsAsync(int count = 10, CancellationToken ct = default)
    {
        return await _uow.Projects.GetRecentProjectsAsync(count, ct);
    }
}
