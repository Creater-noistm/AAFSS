using System.Windows;

namespace AAFSS.App;

/// <summary>
/// Application entry point for AAFSS (AeroAcoustic Fatigue Spectrum Studio).
/// Handles startup, exception handling, and single-instance enforcement.
/// </summary>
public partial class App : Application
{
    private static readonly Mutex _mutex = new(true, "AAFSS_SingleInstance_Mutex");
    private Bootstrapper? _bootstrapper;

    /// <summary>
    /// Application startup handler. Performs single-instance check and initializes the DI container.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance check
        if (!_mutex.WaitOne(TimeSpan.Zero, true))
        {
            MessageBox.Show(
                "AAFSS 已在运行中。请切换到已有窗口。",
                "AAFSS - 单实例",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Global exception handlers
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            _bootstrapper = new Bootstrapper();
            _bootstrapper.ConfigureServices();
            _bootstrapper.InitializeLogging();
            _bootstrapper.InitializePythonEngine();
            _bootstrapper.InitializePlugins();

            var mainWindow = _bootstrapper.GetMainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"AAFSS 启动失败:\n{ex.Message}\n\n请检查日志文件获取详细信息。",
                "AAFSS - 启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// Application exit cleanup. Releases the single-instance mutex and shuts down services.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper?.Shutdown();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Handles unhandled exceptions from the WPF dispatcher thread.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("UI线程未处理异常", e.Exception);
        MessageBox.Show(
            $"发生意外错误:\n{e.Exception.Message}\n\n详细信息已记录到日志文件。",
            "AAFSS - 错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>
    /// Handles unhandled exceptions from background AppDomain threads.
    /// </summary>
    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException("AppDomain未处理异常", ex);
        }
    }

    /// <summary>
    /// Handles unobserved task exceptions (fire-and-forget tasks that faulted).
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("未观察任务异常", e.Exception);
        e.SetObserved();
    }

    /// <summary>
    /// Attempts to log an exception via the bootstrapper's logger, falling back to file output.
    /// </summary>
    private static void LogException(string context, Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AAFSS", "Logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(logPath, $"[{context}]\n{ex}\n");
        }
        catch
        {
            // Last-resort: cannot log, swallow silently
        }
    }
}
