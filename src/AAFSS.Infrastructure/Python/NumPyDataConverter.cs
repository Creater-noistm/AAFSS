namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Converts between .NET data structures and NumPy arrays.
/// Handles efficient data marshaling between managed and unmanaged memory.
/// </summary>
public class NumPyDataConverter : IDisposable
{
    private readonly PythonScriptExecutor _executor;

    public NumPyDataConverter(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Converts a 1D double array to a NumPy array.
    /// </summary>
    public dynamic ToNumPyArray(double[] data)
    {
        return _executor.ToNumPyArray(data);
    }

    /// <summary>
    /// Converts a 2D double array to a NumPy 2D array.
    /// </summary>
    public dynamic ToNumPyArray2D(double[,] data)
    {
        return _executor.ToNumPyArray2D(data);
    }

    /// <summary>
    /// Converts a NumPy 1D array to a .NET double[].
    /// </summary>
    public double[] FromNumPyArray1D(dynamic npArray)
    {
        var result = new List<double>();
        foreach (var val in npArray)
            result.Add((double)val);
        return result.ToArray();
    }

    /// <summary>
    /// Converts a NumPy 2D array to a .NET double[,].
    /// </summary>
    public double[,] FromNumPyArray2D(dynamic npArray)
    {
        dynamic np = _executor.ImportModule("numpy");
        dynamic shape = npArray.shape;
        int rows = (int)shape[0];
        int cols = (int)shape[1];
        var result = new double[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            dynamic row = npArray[r];
            for (int c = 0; c < cols; c++)
            {
                result[r, c] = (double)row[c];
            }
        }

        return result;
    }

    public void Dispose() { }
}
