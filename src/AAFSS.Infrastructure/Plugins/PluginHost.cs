using AAFSS.PluginContracts;
using System.Composition.Hosting;
using System.Reflection;

namespace AAFSS.Infrastructure.Plugins;

/// <summary>
/// MEF2-based plugin host for discovering and loading plugin assemblies.
/// Manages the lifecycle of algorithm, data source, view, and report template plugins.
/// </summary>
public class PluginHost : IDisposable
{
    private CompositionHost? _container;
    private readonly string _pluginsDirectory;
    private readonly List<PluginMetadata> _loadedPlugins = new();
    private bool _isLoaded;
    private bool _disposed;

    /// <summary>
    /// Event raised when a plugin is loaded or unloaded.
    /// </summary>
    public event EventHandler<PluginEventArgs>? PluginChanged;

    /// <summary>
    /// Initializes the plugin host.
    /// </summary>
    public PluginHost()
    {
        _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    /// <summary>
    /// Gets all currently loaded plugin metadata.
    /// </summary>
    public IReadOnlyList<PluginMetadata> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Loads all plugins from the plugins directory.
    /// </summary>
    public async Task LoadAllAsync(CancellationToken ct = default)
    {
        if (_isLoaded) return;

        await Task.Run(() =>
        {
            var assemblies = new List<Assembly>();
            var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (var file in dllFiles)
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    assemblies.Add(asm);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load plugin assembly {file}: {ex.Message}");
                }
            }

            if (assemblies.Count > 0)
            {
                var config = new ContainerConfiguration().WithAssemblies(assemblies);
                _container = config.CreateContainer();
            }

            _isLoaded = true;
        }, ct);
    }

    /// <summary>
    /// Gets all loaded algorithm plugins.
    /// </summary>
    public IEnumerable<IAlgorithmPlugin> GetAlgorithmPlugins()
    {
        return _container?.GetExports<IAlgorithmPlugin>() ?? Enumerable.Empty<IAlgorithmPlugin>();
    }

    /// <summary>
    /// Gets all loaded data source plugins.
    /// </summary>
    public IEnumerable<IDataSourcePlugin> GetDataSourcePlugins()
    {
        return _container?.GetExports<IDataSourcePlugin>() ?? Enumerable.Empty<IDataSourcePlugin>();
    }

    /// <summary>
    /// Gets all loaded view plugins.
    /// </summary>
    public IEnumerable<IViewPlugin> GetViewPlugins()
    {
        return _container?.GetExports<IViewPlugin>() ?? Enumerable.Empty<IViewPlugin>();
    }

    /// <summary>
    /// Gets all loaded report template plugins.
    /// </summary>
    public IEnumerable<IReportTemplatePlugin> GetReportTemplatePlugins()
    {
        return _container?.GetExports<IReportTemplatePlugin>() ?? Enumerable.Empty<IReportTemplatePlugin>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _container?.Dispose();
    }
}

/// <summary>
/// Event arguments for plugin load/unload events.
/// </summary>
public class PluginEventArgs : EventArgs
{
    public PluginMetadata Metadata { get; }
    public string Action { get; } // "Loaded" or "Unloaded"

    public PluginEventArgs(PluginMetadata metadata, string action)
    {
        Metadata = metadata;
        Action = action;
    }
}
