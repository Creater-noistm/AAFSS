using Serilog;
using Serilog.Events;

namespace AAFSS.Infrastructure.Logging;

/// <summary>
/// Configures Serilog structured logging for the AAFSS application.
/// Provides file-based and debug output sinks with rolling file retention.
/// </summary>
public class SerilogConfig
{
    /// <summary>
    /// Configures Serilog with the application configuration settings.
    /// </summary>
    /// <param name="appConfig">Application configuration instance.</param>
    public void Configure(Configuration.AppConfiguration appConfig)
    {
        var logDir = appConfig.LogDirectory;
        Directory.CreateDirectory(logDir);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production")
            
            
            .WriteTo.File(
                path: Path.Combine(logDir, "aafss-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10_485_760, // 10 MB
                rollOnFileSizeLimit: true)
            .WriteTo.File(
                path: Path.Combine(logDir, "aafss-errors-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}{Exception}");

        Log.Logger = loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Creates a simple logger for testing or lightweight scenarios.
    /// </summary>
    public static ILogger CreateSimpleLogger(string logFilePath)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFilePath)
            .CreateLogger();
    }
}
