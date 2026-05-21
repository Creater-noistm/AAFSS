using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Lightweight query service for read-only data access in CQRS query handlers.
/// Provides direct access to entity data without full aggregate loading,
/// complementing the command-side service interfaces.
/// </summary>
public interface IQueryDataService
{
    /// <summary>Gets processing steps for a data source, ordered by StepOrder.</summary>
    Task<List<ProcessingStep>> GetProcessingStepsAsync(Guid dataSourceId, CancellationToken ct = default);

    /// <summary>Gets a compiled spectrum by ID with its navigation properties.</summary>
    Task<CompiledSpectrum?> GetCompiledSpectrumAsync(Guid spectrumId, CancellationToken ct = default);

    /// <summary>Gets a data source by ID with its processing steps loaded.</summary>
    Task<DataSource?> GetDataSourceAsync(Guid dataSourceId, CancellationToken ct = default);
}
