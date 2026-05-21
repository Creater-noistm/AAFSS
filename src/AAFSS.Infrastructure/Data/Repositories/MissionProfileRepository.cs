using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for MissionProfile entity.
/// </summary>
public interface IMissionProfileRepository
{
    Task<MissionProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<MissionProfile>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<MissionProfile> AddAsync(MissionProfile profile, CancellationToken ct = default);
    Task<MissionProfile> UpdateAsync(MissionProfile profile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of IMissionProfileRepository.
/// </summary>
public class MissionProfileRepository : IMissionProfileRepository
{
    private readonly AafssDbContext _context;

    public MissionProfileRepository(AafssDbContext context)
    {
        _context = context;
    }

    public async Task<MissionProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MissionProfiles
            .Include(p => p.Conditions)
            .Include(p => p.Points)
            .Include(p => p.DataSources)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<MissionProfile>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.MissionProfiles
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.Conditions)
            .Include(p => p.Points)
            .Include(p => p.DataSources)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<MissionProfile> AddAsync(MissionProfile profile, CancellationToken ct = default)
    {
        _context.MissionProfiles.Add(profile);
        await _context.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<MissionProfile> UpdateAsync(MissionProfile profile, CancellationToken ct = default)
    {
        _context.MissionProfiles.Update(profile);
        await _context.SaveChangesAsync(ct);
        return profile;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var profile = await _context.MissionProfiles.FindAsync(new object[] { id }, ct);
        if (profile != null)
        {
            _context.MissionProfiles.Remove(profile);
            await _context.SaveChangesAsync(ct);
        }
    }
}
