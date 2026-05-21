namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Bridge to Python scipy.signal and numpy for frequency analysis.
/// Provides FFT, PSD (Welch), octave band analysis, and cross-spectrum.
/// </summary>
public class FrequencyAnalysisBridge : IDisposable
{
    private readonly PythonScriptExecutor _executor;

    public FrequencyAnalysisBridge(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Computes PSD using Welch's method.
    /// </summary>
    public async Task<(double[] Frequencies, double[] Psd)> ComputeWelchPsdAsync(double[] data, double sampleRate, int nperseg = 4096)
    {
        return await Task.Run(() =>
        {
            dynamic scipy_signal = _executor.ImportModule("scipy.signal");
            dynamic f = scipy_signal.welch(_executor.ToNumPyArray(data), sampleRate, nperseg: nperseg);
            return ((double[])ConvertFromNumpy(f[0]), (double[])ConvertFromNumpy(f[1]));
        });
    }

    /// <summary>
    /// Computes octave band levels from a PSD.
    /// </summary>
    public async Task<(double[] CenterFrequencies, double[] BandLevels)> ComputeOctaveBandsAsync(
        double[] frequencies, double[] psd, int bandsPerOctave = 3)
    {
        return await Task.Run(() =>
        {
            // Simplified octave band computation
            // For each standard center frequency, integrate PSD in the band
            var fcList = new List<double>();
            var levels = new List<double>();

            var bands = bandsPerOctave;

            // Generate center frequencies: fc = 1000 * 2^(i/bands)
            var minFreq = frequencies[0];
            var maxFreq = frequencies[frequencies.Length - 1];
            var minIndex = (int)Math.Floor(bands * Math.Log2(Math.Max(minFreq, 12.5) / 1000.0));
            var maxIndex = (int)Math.Ceiling(bands * Math.Log2(Math.Min(maxFreq, 20000.0) / 1000.0));

            for (int i = minIndex; i <= maxIndex; i++)
            {
                var fc = 1000.0 * Math.Pow(2.0, (double)i / bands);
                var fl = fc / Math.Pow(2.0, 1.0 / (2.0 * bands));
                var fu = fc * Math.Pow(2.0, 1.0 / (2.0 * bands));

                // Integrate PSD in [fl, fu]
                var bandPsd = 0.0;
                var count = 0;
                for (int j = 0; j < frequencies.Length; j++)
                {
                    if (frequencies[j] >= fl && frequencies[j] <= fu)
                    {
                        bandPsd += Math.Pow(10.0, psd[j] / 10.0);
                        count++;
                    }
                }

                if (count > 0)
                {
                    var spl = 10.0 * Math.Log10(bandPsd);
                    fcList.Add(fc);
                    levels.Add(spl);
                }
            }

            return (fcList.ToArray(), levels.ToArray());
        });
    }

    public void Dispose() { }

    private static double[] ConvertFromNumpy(dynamic npArray)
    {
        var result = new List<double>();
        foreach (var val in npArray)
            result.Add((double)val);
        return result.ToArray();
    }
}
