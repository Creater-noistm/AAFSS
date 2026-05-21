namespace AAFSS.Infrastructure.Plugins;

/// <summary>
/// Discovers available plugins by scanning the plugins directory
/// and analyzing assembly metadata without loading them into the app domain.
/// </summary>
public class PluginDiscoveryService : IDisposable
{
    private readonly string _pluginsDirectory;
    private bool _disposed;

    public PluginDiscoveryService()
    {
        _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    /// <summary>
    /// Gets the plugins directory path.
    /// </summary>
    public string PluginsDirectory => _pluginsDirectory;

    /// <summary>
    /// Discovers all plugin assemblies in the plugins directory.
    /// </summary>
    /// <returns>List of discovered plugin metadata entries.</returns>
    public async Task<List<DiscoveredPlugin>> DiscoverAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var plugins = new List<DiscoveredPlugin>();

            if (!Directory.Exists(_pluginsDirectory))
                return plugins;

            var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (var file in dllFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var name = Path.GetFileNameWithoutExtension(file);

                    // Attempt to read assembly metadata without loading
                    var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(file);

                    plugins.Add(new DiscoveredPlugin
                    {
                        Name = name,
                        FilePath = file,
                        FileSize = fileInfo.Length,
                        Version = version.FileVersion ?? "Unknown",
                        LastModified = fileInfo.LastWriteTimeUtc
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to discover plugin {file}: {ex.Message}");
                }
            }

            return plugins;
        }, ct);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

/// <summary>
/// Represents a discovered plugin before loading.
/// </summary>
public class DiscoveredPlugin
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }

    public override string ToString() => $"{Name} v{Version}";
}
