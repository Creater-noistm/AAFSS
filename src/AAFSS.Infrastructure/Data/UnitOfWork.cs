using AAFSS.Infrastructure.Data.Repositories;

namespace AAFSS.Infrastructure.Data;

/// <summary>
/// Unit of Work interface for coordinating transactional persistence across repositories.
/// Ensures all changes in a single business operation are committed atomically.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Gets the project repository.</summary>
    IProjectRepository Projects { get; }

    /// <summary>Gets the mission profile repository.</summary>
    IMissionProfileRepository MissionProfiles { get; }

    /// <summary>Gets the data source repository.</summary>
    IDataSourceRepository DataSources { get; }

    /// <summary>Gets the spectrum repository (compiled spectra + results + rainflow).</summary>
    ISpectrumRepository Spectra { get; }

    /// <summary>Gets the processing step repository.</summary>
    IProcessingStepRepository ProcessingSteps { get; }

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of entities written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

/// <summary>
/// EF Core implementation of the Unit of Work pattern.
/// Coordinates all repositories and provides transactional support.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AafssDbContext _context;
    private IProjectRepository? _projects;
    private IMissionProfileRepository? _missionProfiles;
    private IDataSourceRepository? _dataSources;
    private ISpectrumRepository? _spectra;
    private IProcessingStepRepository? _processingSteps;

    /// <summary>
    /// Initializes a new instance with the EF Core database context.
    /// </summary>
    public UnitOfWork(AafssDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    /// <inheritdoc/>
    public IMissionProfileRepository MissionProfiles =>
        _missionProfiles ??= new MissionProfileRepository(_context);

    /// <inheritdoc/>
    public IDataSourceRepository DataSources =>
        _dataSources ??= new DataSourceRepository(_context);

    /// <inheritdoc/>
    public ISpectrumRepository Spectra =>
        _spectra ??= new SpectrumRepository(_context);

    /// <inheritdoc/>
    public IProcessingStepRepository ProcessingSteps =>
        _processingSteps ??= new ProcessingStepRepository(_context);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.BeginTransactionAsync(ct);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.CommitTransactionAsync(ct);
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        await _context.Database.RollbackTransactionAsync(ct);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context.Dispose();
    }
}
