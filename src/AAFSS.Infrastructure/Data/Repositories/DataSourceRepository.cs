using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for DataSource entity.
/// </summary>
public interface IDataSourceRepository
{
    Task<DataSource?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DataSource>> GetByProfileIdAsync(Guid profileId, CancellationToken ct = default);
    Task<DataSource> AddAsync(DataSource dataSource, CancellationToken ct = default);
    Task<DataSource> UpdateAsync(DataSource dataSource, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of IDataSourceRepository.
/// </summary>
public class DataSourceRepository : IDataSourceRepository
{
    private readonly AafssDbContext _context;

    public DataSourceRepository(AafssDbContext context)
    {
        _context = context;
    }

    public async Task<DataSource?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DataSources
            .Include(d => d.ProcessingSteps)
            .Include(d => d.TimeSeriesData)
            .Include(d => d.SpectrumResults)
            .Include(d => d.RainflowResults)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<List<DataSource>> GetByProfileIdAsync(Guid profileId, CancellationToken ct = default)
    {
        return await _context.DataSources
            .Where(d => d.ProfileId == profileId)
            .Include(d => d.ProcessingSteps)
            .OrderBy(d => d.ImportedAt)
            .ToListAsync(ct);
    }

    public async Task<DataSource> AddAsync(DataSource dataSource, CancellationToken ct = default)
    {
        _context.DataSources.Add(dataSource);
        await _context.SaveChangesAsync(ct);
        return dataSource;
    }

    public async Task<DataSource> UpdateAsync(DataSource dataSource, CancellationToken ct = default)
    {
        _context.DataSources.Update(dataSource);
        await _context.SaveChangesAsync(ct);
        return dataSource;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dataSource = await _context.DataSources.FindAsync(new object[] { id }, ct);
        if (dataSource != null)
        {
            _context.DataSources.Remove(dataSource);
            await _context.SaveChangesAsync(ct);
        }
    }
}
