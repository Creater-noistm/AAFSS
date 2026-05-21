using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for compiled spectra and spectrum results.
/// </summary>
public interface ISpectrumRepository
{
    // Compiled Spectrum
    Task<CompiledSpectrum?> GetCompiledByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CompiledSpectrum>> GetCompiledByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<CompiledSpectrum> AddCompiledAsync(CompiledSpectrum spectrum, CancellationToken ct = default);
    Task<CompiledSpectrum> UpdateCompiledAsync(CompiledSpectrum spectrum, CancellationToken ct = default);

    // Spectrum Result
    Task<SpectrumResult?> GetResultByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SpectrumResult>> GetResultsByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default);
    Task<SpectrumResult> AddResultAsync(SpectrumResult result, CancellationToken ct = default);

    // Rainflow Result
    Task<RainflowResult?> GetRainflowByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<RainflowResult>> GetRainflowsByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default);
    Task<RainflowResult> AddRainflowAsync(RainflowResult result, CancellationToken ct = default);

    // Statistical Model
    Task<StatisticalModel?> GetStatisticalModelByIdAsync(Guid id, CancellationToken ct = default);
    Task<StatisticalModel> AddStatisticalModelAsync(StatisticalModel model, CancellationToken ct = default);

    // Validation Report
    Task<ValidationReport?> GetValidationBySpectrumIdAsync(Guid spectrumId, CancellationToken ct = default);
    Task<ValidationReport> AddValidationAsync(ValidationReport report, CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of ISpectrumRepository.
/// </summary>
public class SpectrumRepository : ISpectrumRepository
{
    private readonly AafssDbContext _context;

    public SpectrumRepository(AafssDbContext context)
    {
        _context = context;
    }

    public async Task<CompiledSpectrum?> GetCompiledByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CompiledSpectra
            .Include(s => s.ValidationReport)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<CompiledSpectrum>> GetCompiledByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.CompiledSpectra
            .Where(s => s.ProjectId == projectId)
            .Include(s => s.ValidationReport)
            .OrderBy(s => s.CompiledAt)
            .ToListAsync(ct);
    }

    public async Task<CompiledSpectrum> AddCompiledAsync(CompiledSpectrum spectrum, CancellationToken ct = default)
    {
        _context.CompiledSpectra.Add(spectrum);
        await _context.SaveChangesAsync(ct);
        return spectrum;
    }

    public async Task<CompiledSpectrum> UpdateCompiledAsync(CompiledSpectrum spectrum, CancellationToken ct = default)
    {
        _context.CompiledSpectra.Update(spectrum);
        await _context.SaveChangesAsync(ct);
        return spectrum;
    }

    public async Task<SpectrumResult?> GetResultByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SpectrumResults.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<SpectrumResult>> GetResultsByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default)
    {
        return await _context.SpectrumResults
            .Where(s => s.DataSourceId == dataSourceId)
            .OrderBy(s => s.ComputedAt)
            .ToListAsync(ct);
    }

    public async Task<SpectrumResult> AddResultAsync(SpectrumResult result, CancellationToken ct = default)
    {
        _context.SpectrumResults.Add(result);
        await _context.SaveChangesAsync(ct);
        return result;
    }

    public async Task<RainflowResult?> GetRainflowByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.RainflowResults
            .Include(r => r.StatisticalModels)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<List<RainflowResult>> GetRainflowsByDataSourceIdAsync(Guid dataSourceId, CancellationToken ct = default)
    {
        return await _context.RainflowResults
            .Where(r => r.DataSourceId == dataSourceId)
            .OrderBy(r => r.ComputedAt)
            .ToListAsync(ct);
    }

    public async Task<RainflowResult> AddRainflowAsync(RainflowResult result, CancellationToken ct = default)
    {
        _context.RainflowResults.Add(result);
        await _context.SaveChangesAsync(ct);
        return result;
    }

    public async Task<StatisticalModel?> GetStatisticalModelByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.StatisticalModels.FindAsync(new object[] { id }, ct);
    }

    public async Task<StatisticalModel> AddStatisticalModelAsync(StatisticalModel model, CancellationToken ct = default)
    {
        _context.StatisticalModels.Add(model);
        await _context.SaveChangesAsync(ct);
        return model;
    }

    public async Task<ValidationReport?> GetValidationBySpectrumIdAsync(Guid spectrumId, CancellationToken ct = default)
    {
        return await _context.ValidationReports
            .FirstOrDefaultAsync(v => v.SpectrumId == spectrumId, ct);
    }

    public async Task<ValidationReport> AddValidationAsync(ValidationReport report, CancellationToken ct = default)
    {
        _context.ValidationReports.Add(report);
        await _context.SaveChangesAsync(ct);
        return report;
    }
}
