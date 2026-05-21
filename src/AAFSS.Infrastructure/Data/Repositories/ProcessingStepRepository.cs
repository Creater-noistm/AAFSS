using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for ProcessingStep entity.
/// </summary>
public interface IProcessingStepRepository
{
    Task<ProcessingStep?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ProcessingStep>> GetByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default);
    Task<ProcessingStep> AddAsync(ProcessingStep step, CancellationToken ct = default);
    Task<ProcessingStep> UpdateAsync(ProcessingStep step, CancellationToken ct = default);
    Task<List<ProcessingStep>> GetFailedStepsAsync(CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of IProcessingStepRepository.
/// </summary>
public class ProcessingStepRepository : IProcessingStepRepository
{
    private readonly AafssDbContext _context;

    public ProcessingStepRepository(AafssDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessingStep?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ProcessingSteps.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<ProcessingStep>> GetByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default)
    {
        return await _context.ProcessingSteps
            .Where(s => s.DataSourceId == dataSourceId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(ct);
    }

    public async Task<ProcessingStep> AddAsync(ProcessingStep step, CancellationToken ct = default)
    {
        _context.ProcessingSteps.Add(step);
        await _context.SaveChangesAsync(ct);
        return step;
    }

    public async Task<ProcessingStep> UpdateAsync(ProcessingStep step, CancellationToken ct = default)
    {
        _context.ProcessingSteps.Update(step);
        await _context.SaveChangesAsync(ct);
        return step;
    }

    public async Task<List<ProcessingStep>> GetFailedStepsAsync(CancellationToken ct = default)
    {
        return await _context.ProcessingSteps
            .Where(s => s.Status == ProcessingStatus.Failed)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);
    }
}
