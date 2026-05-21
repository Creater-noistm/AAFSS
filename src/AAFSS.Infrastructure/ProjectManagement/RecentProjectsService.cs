using System.Text.Json;
using AAFSS.Infrastructure.Configuration;

namespace AAFSS.Infrastructure.ProjectManagement;

/// <summary>
/// Tracks and manages the list of recently opened projects.
/// Persists the list to a JSON file in the application data directory.
/// </summary>
public class RecentProjectsService
{
    private readonly AppConfiguration _configuration;
    private readonly string _recentFilePath;
    private List<RecentProjectEntry> _entries = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes the recent projects service and loads the persisted list.
    /// </summary>
    public RecentProjectsService(AppConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _recentFilePath = Path.Combine(_configuration.LogDirectory, "recent_projects.json");
        Load();
    }

    /// <summary>
    /// Gets the maximum number of recent projects to keep.
    /// </summary>
    public int MaxEntries =>
        _configuration.MaxRecentProjects > 0 ? _configuration.MaxRecentProjects : 10;

    /// <summary>
    /// Gets the recent projects list (thread-safe copy).
    /// </summary>
    public async Task<IReadOnlyList<RecentProjectEntry>> GetRecentProjectsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _entries.OrderByDescending(e => e.LastOpenedAt).ToList().AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Adds or updates a project in the recent list.
    /// If the project already exists, updates its timestamp and file path.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="projectName">Project display name.</param>
    /// <param name="filePath">File path of the project.</param>
    public async Task AddOrUpdateAsync(Guid projectId, string projectName, string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = _entries.FirstOrDefault(e => e.ProjectId == projectId);
            if (existing != null)
            {
                existing.ProjectName = projectName;
                existing.FilePath = filePath;
                existing.LastOpenedAt = DateTime.UtcNow;
                existing.OpenCount++;
            }
            else
            {
                _entries.Add(new RecentProjectEntry
                {
                    ProjectId = projectId,
                    ProjectName = projectName,
                    FilePath = filePath,
                    LastOpenedAt = DateTime.UtcNow,
                    OpenCount = 1
                });
            }

            // Trim excess entries
            if (_entries.Count > MaxEntries)
            {
                _entries = _entries
                    .OrderByDescending(e => e.LastOpenedAt)
                    .Take(MaxEntries)
                    .ToList();
            }

            Save();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Removes a project from the recent list.
    /// </summary>
    /// <param name="projectId">Project identifier to remove.</param>
    public async Task RemoveAsync(Guid projectId)
    {
        await _lock.WaitAsync();
        try
        {
            _entries.RemoveAll(e => e.ProjectId == projectId);
            Save();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Checks if a project file path still exists.
    /// </summary>
    /// <param name="entry">The recent project entry to verify.</param>
    /// <returns>True if the file exists.</returns>
    public static bool FileExists(RecentProjectEntry entry)
    {
        return !string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath);
    }

    /// <summary>
    /// Cleans up entries whose files no longer exist.
    /// </summary>
    /// <returns>Number of entries removed.</returns>
    public async Task<int> CleanupStaleEntriesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var removed = _entries.RemoveAll(e => !FileExists(e));
            if (removed > 0) Save();
            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Clears all recent projects.
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _entries.Clear();
            Save();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads the recent projects from disk.
    /// </summary>
    private void Load()
    {
        try
        {
            if (File.Exists(_recentFilePath))
            {
                var json = File.ReadAllText(_recentFilePath);
                _entries = JsonSerializer.Deserialize<List<RecentProjectEntry>>(json, JsonOptions) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load recent projects: {ex.Message}");
            _entries = new();
        }
    }

    /// <summary>
    /// Persists the recent projects list to disk.
    /// </summary>
    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_recentFilePath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_entries, JsonOptions);
            File.WriteAllText(_recentFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save recent projects: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a single entry in the recent projects list.
/// </summary>
public class RecentProjectEntry
{
    /// <summary>Unique project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Display name of the project.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>File path to the .aafss project file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Timestamp of when the project was last opened.</summary>
    public DateTime LastOpenedAt { get; set; }

    /// <summary>Number of times this project has been opened.</summary>
    public int OpenCount { get; set; }

    /// <summary>Returns a human-readable representation.</summary>
    public override string ToString() =>
        $"{ProjectName} ({LastOpenedAt:yyyy-MM-dd HH:mm})";
}
