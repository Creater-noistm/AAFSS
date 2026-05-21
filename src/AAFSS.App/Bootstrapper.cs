using AAFSS.App.Services;
using AAFSS.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Composition.Hosting;
using System.Windows;

namespace AAFSS.App;

/// <summary>
/// Bootstrapper responsible for initializing the DI container, logging, Python engine, and plugin system.
/// Follows the composition root pattern — all service registrations flow through here.
/// </summary>
public class Bootstrapper
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the configured service provider. Must be called after ConfigureServices().
    /// </summary>
    public IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized. Call ConfigureServices() first.");

    /// <summary>
    /// Configures the dependency injection container with all application services.
    /// </summary>
    public void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Application configuration
        services.AddSingleton<Infrastructure.Configuration.AppConfiguration>();

        // Logging
        services.AddSingleton(Log.Logger);
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AAFSS.Core.Commands.ImportDataCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(AAFSS.Infrastructure.DependencyInjection).Assembly);
        });

        // Infrastructure layer services
        services.AddInfrastructure();

        // Theme service
        services.AddSingleton<IThemeService, ThemeService>();

        // Layout manager
        services.AddSingleton<ILayoutManager, LayoutManager>();

        // MEF2 plugin container
        services.AddSingleton(provider =>
        {
            var configuration = new ContainerConfiguration();
            // Plugin contracts assembly
            configuration.WithAssembly(typeof(AAFSS.PluginContracts.IAlgorithmPlugin).Assembly);
            // Infrastructure assembly (contains built-in plugin implementations)
            configuration.WithAssembly(typeof(AAFSS.Infrastructure.DependencyInjection).Assembly);
            return configuration.CreateContainer();
        });

        // ViewModels (transient, created via DI factory)
        RegisterViewModels(services);

        // Views
        RegisterViews(services);

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Initializes Serilog structured logging with file and debug sinks.
    /// </summary>
    public void InitializeLogging()
    {
        var appConfig = ServiceProvider.GetRequiredService<Infrastructure.Configuration.AppConfiguration>();
        var logConfig = new Infrastructure.Logging.SerilogConfig();
        logConfig.Configure(appConfig);
        Log.Information("AAFSS application starting...");
    }

    /// <summary>
    /// Initializes the Python.NET engine for scientific computing.
    /// </summary>
    public void InitializePythonEngine()
    {
        try
        {
            var engine = Infrastructure.Python.PythonEngine.Instance;
            // Python initialization is deferred to first use via lazy pattern in PythonEngine
            Log.Information("Python engine ready for initialization");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Python engine pre-initialization check failed. Will initialize on first use.");
        }
    }

    /// <summary>
    /// Loads and activates plugins via MEF2 discovery.
    /// </summary>
    public void InitializePlugins()
    {
        try
        {
            var pluginHost = ServiceProvider.GetRequiredService<Infrastructure.Plugins.PluginHost>();
            var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            pluginHost.LoadPlugins(pluginsDir);
            Log.Information("Plugin system initialized. Plugins directory: {PluginsDir}", pluginsDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Plugin initialization failed. Application will continue without plugins.");
        }
    }

    /// <summary>
    /// Resolves and returns the main window from the DI container.
    /// </summary>
    public Window GetMainWindow()
    {
        var mainWindow = ServiceProvider.GetRequiredService<Views.MainWindow>();
        return mainWindow;
    }

    /// <summary>
    /// Performs graceful shutdown of all services.
    /// </summary>
    public void Shutdown()
    {
        Log.Information("AAFSS application shutting down...");
        try
        {
            Infrastructure.Python.PythonEngine.Instance.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error during Python engine shutdown");
        }
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Registers all ViewModels in the DI container.
    /// </summary>
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<ViewModels.MainWindowViewModel>();
        services.AddTransient<ViewModels.ProjectExplorerViewModel>();
        services.AddTransient<ViewModels.PropertyPanelViewModel>();
        services.AddTransient<ViewModels.BottomPanelViewModel>();
        services.AddTransient<ViewModels.CommandPaletteViewModel>();
        services.AddTransient<ViewModels.DataTableViewModel>();
        services.AddTransient<ViewModels.SpectrumChartViewModel>();
        services.AddTransient<ViewModels.PsdChartViewModel>();
        services.AddTransient<ViewModels.WaveformChartViewModel>();
        services.AddTransient<ViewModels.RainflowHeatmapViewModel>();
        services.AddTransient<ViewModels.StatusBarViewModel>();
        services.AddTransient<ViewModels.ReportPreviewViewModel>();
        services.AddTransient<ViewModels.StatisticalViewModel>();
    }

    /// <summary>
    /// Registers all Views in the DI container.
    /// </summary>
    private static void RegisterViews(IServiceCollection services)
    {
        services.AddTransient<Views.MainWindow>();
        services.AddTransient<Views.ProjectExplorerView>();
        services.AddTransient<Views.PropertyPanelView>();
        services.AddTransient<Views.BottomPanelView>();
        services.AddTransient<Views.CommandPaletteView>();
        services.AddTransient<Views.DataTableView>();
        services.AddTransient<Views.SpectrumChartView>();
        services.AddTransient<Views.PsdChartView>();
        services.AddTransient<Views.WaveformChartView>();
        services.AddTransient<Views.RainflowHeatmapView>();
        services.AddTransient<Views.StatisticalView>();
        services.AddTransient<Views.ReportPreviewView>();
        services.AddTransient<Views.StatusBarView>();
        services.AddTransient<Views.ImportDataDialog>();
        services.AddTransient<Views.BatchProcessingDialog>();
        services.AddTransient<Views.OpenProjectDialog>();
    }
}
