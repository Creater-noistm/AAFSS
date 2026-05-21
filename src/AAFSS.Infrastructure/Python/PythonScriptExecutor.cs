namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Executes Python scripts via Python.NET (pythonnet).
/// Manages the Python engine lifecycle and provides a bridge between
/// .NET and Python scientific computing libraries.
/// </summary>
public class PythonScriptExecutor : IDisposable
{
    private bool _isInitialized;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets whether the Python engine is initialized and ready.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Initializes the Python engine with the configured environment.
    /// </summary>
    public void Initialize(string? pythonHome = null, string? pythonPath = null)
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;

            try
            {
                if (!string.IsNullOrEmpty(pythonHome))
                {
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", Path.Combine(pythonHome, "python38.dll"));
                }

                if (!string.IsNullOrEmpty(pythonPath))
                {
                    Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);
                }

                // pythonnet 3.x uses PythonEngine
                // Runtime.PythonDLL is configured via PYTHONNET_PYDLL environment variable
                global::Python.Runtime.Runtime.PythonDLL = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL")
                    ?? (Environment.OSVersion.Platform == PlatformID.Win32NT ? "python38.dll" : "libpython3.8.so");

                global::Python.Runtime.PythonEngine.Initialize();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize Python engine: {ex.Message}");
                _isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Executes a Python script file and returns the result.
    /// </summary>
    /// <param name="scriptPath">Path to the Python script.</param>
    /// <param name="args">Script arguments.</param>
    /// <returns>Script output as a string.</returns>
    public string ExecuteScript(string scriptPath, params string[] args)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
        {
            dynamic sys = global::Python.Runtime.Py.Import("sys");
            sys.argv = new global::Python.Runtime.PyList();
            sys.argv.Add(scriptPath);
            foreach (var arg in args)
                sys.argv.Add(arg);

            dynamic builtins = global::Python.Runtime.Py.Import("builtins");
            // Execute the script file
            var scriptCode = File.ReadAllText(scriptPath);
            var result = builtins.exec(scriptCode);

            return result?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Executes an inline Python expression and returns the result.
    /// </summary>
    /// <param name="code">Python code to evaluate.</param>
    /// <returns>Result of the expression.</returns>
    public string ExecuteCode(string code)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
        {
            using var scope = global::Python.Runtime.Py.CreateScope();
            scope.Exec(code);
            return string.Empty;
        }
    }

    /// <summary>
    /// Evaluates a Python expression and returns the result as a .NET object.
    /// </summary>
    public T? Eval<T>(string expression)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
        {
            using var scope = global::Python.Runtime.Py.CreateScope();
            var result = scope.Eval(expression);
            return result.As<T>();
        }
    }

    /// <summary>
    /// Gets a reference to a Python module by name.
    /// </summary>
    public dynamic ImportModule(string moduleName)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
{
    return global::Python.Runtime.Py.Import(moduleName);
        }
    }

    /// <summary>
    /// Converts a .NET double array to a NumPy array.
    /// </summary>
    public dynamic ToNumPyArray(double[] data)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
        {
            dynamic np = global::Python.Runtime.Py.Import("numpy");
            return np.array(data);
        }
    }

    /// <summary>
    /// Converts a 2D .NET double array to a NumPy 2D array.
    /// </summary>
    public dynamic ToNumPyArray2D(double[,] data)
    {
        EnsureInitialized();

        using (global::Python.Runtime.Py.GIL())
        {
            dynamic np = global::Python.Runtime.Py.Import("numpy");
            // Convert to list of lists
            var rows = data.GetLength(0);
            var cols = data.GetLength(1);
            dynamic list = new global::Python.Runtime.PyList();
            for (int r = 0; r < rows; r++)
            {
                dynamic row = new global::Python.Runtime.PyList();
                for (int c = 0; c < cols; c++)
                    row.append(new global::Python.Runtime.PyFloat(data[r, c]));
                list.append(row);
            }
            return np.array(list);
        }
    }

    /// <summary>
    /// Shuts down the Python engine.
    /// </summary>
    public void Shutdown()
    {
        if (!_isInitialized) return;

        lock (_lock)
        {
            if (!_isInitialized) return;

            try
            {
                global::Python.Runtime.PythonEngine.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error shutting down Python: {ex.Message}");
            }
            _isInitialized = false;
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python engine is not initialized. Call Initialize() first.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Shutdown();
    }
}
