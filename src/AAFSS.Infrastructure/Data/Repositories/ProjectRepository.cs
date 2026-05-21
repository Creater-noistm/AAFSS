using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for Project aggregate root.
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Project>> GetAllAsync(CancellationToken ct = default);
    Task<Project> AddAsync(Project project, CancellationToken ct = default);
    Task<Project> UpdateAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<Project>> GetRecentProjectsAsync(int count, CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of IProjectRepository.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly AafssDbContext _context;

    public ProjectRepository(AafssDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Projects
            .Include(p => p.Profiles)
            .Include(p => p.Spectra)
            .Include(p => p.Reports)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<Project>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Projects
            .OrderByDescending(p => p.ModifiedAt)
            .ToListAsync(ct);
    }

    public async Task<Project> AddAsync(Project project, CancellationToken ct = default)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(Project project, CancellationToken ct = default)
    {
        project.ModifiedAt = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(ct);
        return project;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<List<Project>> GetRecentProjectsAsync(int count, CancellationToken ct = default)
    {
        return await _context.Projects
            .OrderByDescending(p => p.ModifiedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}
