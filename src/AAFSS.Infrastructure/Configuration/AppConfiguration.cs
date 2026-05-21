using Microsoft.Extensions.Configuration;

namespace AAFSS.Infrastructure.Configuration;

/// <summary>
/// Application configuration manager that loads settings from appsettings.json
/// and provides typed access to configuration sections.
/// </summary>
public class AppConfiguration
{
    private readonly IConfigurationRoot _configuration;

    /// <summary>
    /// Initializes the application configuration from appsettings.json.
    /// </summary>
    public AppConfiguration()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AAFSS");

        _configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("AAFSS_ENV") ?? "Development"}.json", optional: true)
            .Build();
    }

    /// <summary>
    /// Gets the raw configuration root for advanced access.
    /// </summary>
    public IConfigurationRoot Configuration => _configuration;

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName =>
        _configuration.GetValue<string>("Application:Name") ?? "AAFSS";

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string ApplicationVersion =>
        _configuration.GetValue<string>("Application:Version") ?? "1.0.0";

    /// <summary>
    /// Gets the Python home directory path, with environment variable resolution.
    /// </summary>
    public string? PythonHome =>
        _configuration.GetValue<string>("Python:Home");

    /// <summary>
    /// Gets the Python scripts path relative to the application base.
    /// </summary>
    public string PythonPath
    {
        get
        {
            var path = _configuration.GetValue<string>("Python:PythonPath") ?? "python";
            if (!Path.IsPathRooted(path))
            {
                // Resolve relative to application base directory
                path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", path));
            }
            return path;
        }
    }

    /// <summary>
    /// Gets whether to use embedded Python.
    /// </summary>
    public bool UseEmbeddedPython =>
        _configuration.GetValue<bool>("Python:UseEmbeddedPython");

    /// <summary>
    /// Gets the database connection string with path resolution.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            var cs = _configuration.GetValue<string>("Database:ConnectionString") ?? "Data Source=aafss.db";
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AAFSS");
            Directory.CreateDirectory(appDataPath);
            return cs.Replace("{AppData}", appDataPath);
        }
    }

    /// <summary>
    /// Gets the auto-save interval in minutes.
    /// </summary>
    public int AutoSaveIntervalMinutes =>
        _configuration.GetValue<int>("Database:AutoSaveIntervalMinutes");

    /// <summary>
    /// Gets the maximum number of recent projects to track.
    /// </summary>
    public int MaxRecentProjects =>
        _configuration.GetValue<int>("Database:MaxRecentProjects");

    /// <summary>
    /// Gets the HDF5 default chunk size in bytes.
    /// </summary>
    public long Hdf5ChunkSize =>
        _configuration.GetValue<long>("Hdf5:DefaultChunkSize");

    /// <summary>
    /// Gets the HDF5 compression level (0-9).
    /// </summary>
    public int Hdf5CompressionLevel =>
        _configuration.GetValue<int>("Hdf5:CompressionLevel");

    /// <summary>
    /// Gets the logging directory path.
    /// </summary>
    public string LogDirectory
    {
        get
        {
            var dir = _configuration.GetValue<string>("Logging:LogDirectory") ?? "{AppData}/AAFSS/Logs";
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AAFSS");
            return dir.Replace("{AppData}", appDataPath);
        }
    }

    /// <summary>
    /// Gets the current UI theme.
    /// </summary>
    public string Theme =>
        _configuration.GetValue<string>("UI:Theme") ?? "Light";

    /// <summary>
    /// Gets the current UI language.
    /// </summary>
    public string Language =>
        _configuration.GetValue<string>("UI:Language") ?? "zh-CN";

    /// <summary>
    /// Gets the data table page size for virtual scrolling.
    /// </summary>
    public int DataTablePageSize =>
        _configuration.GetValue<int>("UI:DataTablePageSize");

    /// <summary>
    /// Gets the virtual scroll threshold (rows above which virtualization activates).
    /// </summary>
    public int VirtualScrollThreshold =>
        _configuration.GetValue<int>("UI:VirtualScrollThreshold");

    /// <summary>
    /// Gets the plugins directory path.
    /// </summary>
    public string PluginsDirectory =>
        _configuration.GetValue<string>("Plugins:PluginsDirectory") ?? "Plugins";

    /// <summary>
    /// Gets whether unsigned plugins are allowed.
    /// </summary>
    public bool AllowUnsignedPlugins =>
        _configuration.GetValue<bool>("Plugins:AllowUnsignedPlugins");

    /// <summary>
    /// Gets the default report template name.
    /// </summary>
    public string DefaultReportTemplate =>
        _configuration.GetValue<string>("Report:DefaultTemplate") ?? "GJB_67_13_90";

    /// <summary>
    /// Gets the damage target value for validation.
    /// </summary>
    public double DamageTargetValue =>
        _configuration.GetValue<double>("Validation:DamageTargetValue");

    /// <summary>
    /// Gets the green tolerance for damage validation.
    /// </summary>
    public double DamageToleranceGreen =>
        _configuration.GetValue<double>("Validation:DamageToleranceGreen");

    /// <summary>
    /// Gets the yellow tolerance for damage validation.
    /// </summary>
    public double DamageToleranceYellow =>
        _configuration.GetValue<double>("Validation:DamageToleranceYellow");

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    public T? GetValue<T>(string key) =>
        _configuration.GetValue<T>(key);

    /// <summary>
    /// Gets a configuration section bound to a typed object.
    /// </summary>
    public T GetSection<T>(string key) where T : new()
    {
        var section = new T();
        _configuration.GetSection(key).Bind(section);
        return section;
    }
}
