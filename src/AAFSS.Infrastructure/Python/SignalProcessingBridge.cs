namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Bridge to Python scipy.signal for signal processing operations.
/// Provides filtering, detrending, decimation, and calibration.
/// </summary>
public class SignalProcessingBridge : IDisposable
{
    private readonly PythonScriptExecutor _executor;
    private bool _disposed;

    public SignalProcessingBridge(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Applies a Butterworth filter to the data.
    /// </summary>
    public async Task<double[]> ButterworthFilterAsync(double[] data, double sampleRate, double cutoffHz, string filterType = "low", int order = 4)
    {
        return await Task.Run(() =>
        {
            dynamic np = _executor.ImportModule("numpy");
            dynamic signal = _executor.ImportModule("scipy.signal");

            var nyquist = sampleRate / 2.0;
            var normalizedCutoff = cutoffHz / nyquist;
            dynamic b = signal.butter(order, normalizedCutoff, filterType);
            dynamic a = b[1]; // b, a = signal.butter(...)
            dynamic filtered = signal.lfilter(b[0], a, _executor.ToNumPyArray(data));
            return ConvertFromNumpy(filtered);
        });
    }

    /// <summary>
    /// Removes linear trend from data.
    /// </summary>
    public async Task<double[]> DetrendAsync(double[] data)
    {
        return await Task.Run(() =>
        {
            dynamic scipy_signal = _executor.ImportModule("scipy.signal");
            dynamic result = scipy_signal.detrend(_executor.ToNumPyArray(data));
            return ConvertFromNumpy(result);
        });
    }

    /// <summary>
    /// Decimates (downsamples) the data.
    /// </summary>
    public async Task<double[]> DecimateAsync(double[] data, int factor)
    {
        return await Task.Run(() =>
        {
            dynamic scipy_signal = _executor.ImportModule("scipy.signal");
            dynamic result = scipy_signal.decimate(_executor.ToNumPyArray(data), factor);
            return ConvertFromNumpy(result);
        });
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private static double[] ConvertFromNumpy(dynamic npArray)
    {
        var result = new List<double>();
        foreach (var val in npArray)
            result.Add((double)val);
        return result.ToArray();
    }
}
