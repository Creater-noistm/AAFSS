using AAFSS.Infrastructure.Configuration;
using AAFSS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AAFSS.Infrastructure.ProjectManagement;

/// <summary>
/// Background service that periodically auto-saves the current project.
/// Prevents data loss by saving at a configurable interval (default: 5 minutes).
/// Only saves if there are unsaved changes since the last save.
/// </summary>
public class AafssAutoSaveService : IDisposable
{
    private readonly AppConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _lock = new();
    private Timer? _timer;
    private Guid? _currentProjectId;
    private string? _currentFilePath;
    private DateTime _lastSaveTime = DateTime.MinValue;
    private bool _hasUnsavedChanges;
    private bool _disposed;

    /// <summary>
    /// Event raised when auto-save occurs.
    /// </summary>
    public event EventHandler<AutoSaveEventArgs>? AutoSaved;

    /// <summary>
    /// Event raised when auto-save fails.
    /// </summary>
    public event EventHandler<AutoSaveErrorEventArgs>? AutoSaveFailed;

    /// <summary>
    /// Initializes the auto-save service.
    /// </summary>
    public AafssAutoSaveService(AppConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        StartTimer();
    }

    /// <summary>
    /// Gets the auto-save interval from configuration.
    /// </summary>
    public TimeSpan Interval =>
        TimeSpan.FromMinutes(Math.Max(1, _configuration.AutoSaveIntervalMinutes > 0
            ? _configuration.AutoSaveIntervalMinutes
            : 5));

    /// <summary>
    /// Gets whether auto-save is currently active.
    /// </summary>
    public bool IsActive => _currentProjectId.HasValue && !string.IsNullOrEmpty(_currentFilePath);

    /// <summary>
    /// Sets the current project for auto-save tracking.
    /// </summary>
    public void SetCurrentProject(Guid projectId, string filePath)
    {
        lock (_lock)
        {
            _currentProjectId = projectId;
            _currentFilePath = filePath;
            _hasUnsavedChanges = false;
            _lastSaveTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks that the current project has unsaved changes.
    /// </summary>
    public void MarkDirty()
    {
        lock (_lock)
        {
            _hasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Clears the current project tracking (called on project close).
    /// </summary>
    public void ClearProject()
    {
        lock (_lock)
        {
            _currentProjectId = null;
            _currentFilePath = null;
            _hasUnsavedChanges = false;
        }
    }

    /// <summary>
    /// Performs an immediate manual save and resets the timer.
    /// </summary>
    public async Task SaveNowAsync(CancellationToken ct = default)
    {
        await PerformSave(ct);
        // Reset the timer after a manual save
        StopTimer();
        StartTimer();
    }

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    public void StopTimer()
    {
        lock (_lock)
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Starts (or restarts) the auto-save timer.
    /// </summary>
    public void StartTimer()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = new Timer(async _ => await OnTimerElapsed(), null, Interval, Interval);
        }
    }

    /// <summary>
    /// Timer callback that triggers auto-save if conditions are met.
    /// </summary>
    private async Task OnTimerElapsed()
    {
        try
        {
            lock (_lock)
            {
                if (!IsActive || !_hasUnsavedChanges) return;
            }

            await PerformSave(CancellationToken.None);
        }
        catch (Exception ex)
        {
            AutoSaveFailed?.Invoke(this, new AutoSaveErrorEventArgs(ex));
            System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs the actual save operation against the database and project file.
    /// </summary>
    private async Task PerformSave(CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_currentProjectId.HasValue || string.IsNullOrEmpty(_currentFilePath)) return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AafssDbContext>();
            var projectFile = scope.ServiceProvider.GetRequiredService<AafssProjectFile>();

            // Save all pending DB changes
            await dbContext.SaveChangesAsync(ct);

            lock (_lock)
            {
                _hasUnsavedChanges = false;
                _lastSaveTime = DateTime.UtcNow;
            }

            AutoSaved?.Invoke(this, new AutoSaveEventArgs(
                _currentProjectId!.Value,
                _currentFilePath!,
                _lastSaveTime));
        }
        catch (Exception ex)
        {
            AutoSaveFailed?.Invoke(this, new AutoSaveErrorEventArgs(ex));
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
    }
}

/// <summary>
/// Event arguments for successful auto-save.
/// </summary>
public class AutoSaveEventArgs : EventArgs
{
    public Guid ProjectId { get; }
    public string FilePath { get; }
    public DateTime SaveTime { get; }

    public AutoSaveEventArgs(Guid projectId, string filePath, DateTime saveTime)
    {
        ProjectId = projectId;
        FilePath = filePath;
        SaveTime = saveTime;
    }
}

/// <summary>
/// Event arguments for auto-save failure.
/// </summary>
public class AutoSaveErrorEventArgs : EventArgs
{
    public Exception Exception { get; }

    public AutoSaveErrorEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
