using global::Python.Runtime;
using Serilog;

namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Singleton wrapper around Python.NET (pythonnet) for thread-safe Python execution.
/// Manages the Python runtime lifecycle, GIL acquisition, and exposes Execute/CallFunction/Shutdown.
/// </summary>
public sealed class PythonEngine : IDisposable
{
    private static readonly Lazy<PythonEngine> _instance = new(() => new PythonEngine());
    private readonly object _lock = new();
    private bool _initialized;
    private IntPtr _threadState;

    /// <summary>Singleton instance.</summary>
    public static PythonEngine Instance => _instance.Value;

    private PythonEngine() { }

    /// <summary>
    /// Initializes the Python engine if not already running.
    /// Sets the Python home path, adds the project's python directory to sys.path, and acquires the GIL.
    /// Thread-safe 鈥?only one initialization ever occurs.
    /// </summary>
    /// <param name="pythonHome">Optional Python installation root. If null, auto-detected.</param>
    public void Initialize(string? pythonHome = null)
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                if (!string.IsNullOrWhiteSpace(pythonHome))
                {
                    Runtime.PythonDLL = Path.Combine(pythonHome,
                        OperatingSystem.IsWindows() ? "python312.dll" :
                        OperatingSystem.IsMacOS() ? "libpython3.12.dylib" : "libpython3.12.so");
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
                }

                global::Python.Runtime.PythonEngine.Initialize();
                _threadState = global::Python.Runtime.PythonEngine.BeginAllowThreads();

                // Add the project's python directory and subdirectories to sys.path
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var pythonDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "python"));
                    if (Directory.Exists(pythonDir))
                    {
                        sys.path.insert(0, pythonDir);
                        sys.path.insert(0, Path.Combine(pythonDir, "signal_processing"));
                        sys.path.insert(0, Path.Combine(pythonDir, "frequency"));
                        sys.path.insert(0, Path.Combine(pythonDir, "time_domain"));
                        sys.path.insert(0, Path.Combine(pythonDir, "statistical"));
                    }
                }

                _initialized = true;
                Log.Information("PythonEngine initialized successfully. Python DLL: {Dll}", Runtime.PythonDLL);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize PythonEngine");
                throw;
            }
        }
    }

    /// <summary>
    /// Executes an arbitrary Python code string.
    /// Acquires the GIL before execution and releases it afterward.
    /// </summary>
    /// <param name="code">Python source code to execute.</param>
    /// <returns>Dictionary of local variables after execution.</returns>
    public Dictionary<string, object> Execute(string code)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            using var scope = Py.CreateScope();
            scope.Exec(code);

            var result = new Dictionary<string, object>();
            foreach (var item in ((IEnumerable<KeyValuePair<string, object>>)scope))
            {
                var key = item.Key?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(key)) continue;
                result[key] = item.Value;
            }

            return result;
        }
    }

    /// <summary>
    /// Calls a named Python function with positional and keyword arguments.
    /// The function must be defined in a module that has been imported (e.g., via Execute).
    /// </summary>
    /// <param name="moduleName">Python module name (e.g., "butterworth_filter").</param>
    /// <param name="functionName">Function name within the module.</param>
    /// <param name="args">Positional arguments.</param>
    /// <param name="kwargs">Keyword arguments as dictionary.</param>
    /// <returns>The Python function's return value converted to a .NET object.</returns>
    public object? CallFunction(string moduleName, string functionName, object[]? args = null, Dictionary<string, object>? kwargs = null)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            try
            {
                dynamic module = Py.Import(moduleName);
                dynamic func = module.GetAttr(functionName);

                if (args == null && kwargs == null)
                {
                    return func();
                }

                if (kwargs != null && kwargs.Count > 0)
                {
                    // Build kwargs using Python dict
                    dynamic pyKwargs = new PyDict();
                    foreach (var kvp in kwargs)
                    {
                        pyKwargs[kvp.Key] = kvp.Value?.ToPython();
                    }

                    if (args != null && args.Length > 0)
                    {
                        dynamic pyArgs = new PyList();
                        foreach (var arg in args)
                        {
                            pyArgs.Add(arg?.ToPython() ?? PyObject.None);
                        }
                        return func.call(pyArgs, pyKwargs);
                    }

                    return func.call(new PyDict(), pyKwargs);
                }

                if (args != null && args.Length > 0)
                {
                    var pyArgs = args.Select(a => a?.ToPython() ?? PyObject.None).ToArray();
                    return func.call(pyArgs);
                }

                return func();
            }
            catch (PythonException ex)
            {
                Log.Error(ex, "Python function call failed: {Module}.{Function}", moduleName, functionName);
                throw new InvalidOperationException(
                    $"Python call {moduleName}.{functionName} failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Imports a Python module and returns it as a dynamic object.
    /// </summary>
    public dynamic ImportModule(string moduleName)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            return Py.Import(moduleName);
        }
    }

    /// <summary>
    /// Executes code within an active GIL scope.
    /// Caller is responsible for ensuring GIL is held (use within Execute/CallFunction, or call with GIL block).
    /// </summary>
    public void ExecuteWithGil(Action action)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            action();
        }
    }

    /// <summary>
    /// Executes a function within an active GIL scope and returns a result.
    /// </summary>
    public T ExecuteWithGil<T>(Func<T> func)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            return func();
        }
    }

    /// <summary>
    /// Checks whether the Python engine has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Shuts down the Python engine, releasing all resources.
    /// Safe to call multiple times.
    /// </summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            if (!_initialized) return;

            try
            {
                global::Python.Runtime.PythonEngine.EndAllowThreads(_threadState);
                global::Python.Runtime.PythonEngine.Shutdown();
                _initialized = false;
                Log.Information("PythonEngine shut down successfully");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error during PythonEngine shutdown");
            }
        }
    }

    /// <summary>
    /// Gets the current Python GIL state for external management.
    /// </summary>
    public void EnsureInitialized()
    {
        if (!_initialized) Initialize();
    }

    public void Dispose()
    {
        Shutdown();
    }
}
