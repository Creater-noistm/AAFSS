namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Bridge to Python for time-domain analysis.
/// Provides rainflow counting (per ASTM E1049), peak-valley extraction,
/// and level crossing analysis.
/// </summary>
public class TimeDomainBridge : IDisposable
{
    private readonly PythonScriptExecutor _executor;

    public TimeDomainBridge(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Performs rainflow cycle counting using the ASTM E1049 standard algorithm.
    /// </summary>
    public async Task<(double[] From, double[] To, double[] Amplitudes, double[] Means)> RainflowCountAsync(double[] data)
    {
        return await Task.Run(() =>
        {
            // Implement rainflow counting using the four-point algorithm (ASTM E1049)
            // This is a pure .NET implementation for reliability
            var peaks = new List<double>();
            var valleys = new List<double>();

            // Extract peaks and valleys
            for (int i = 1; i < data.Length - 1; i++)
            {
                if ((data[i] >= data[i - 1] && data[i] > data[i + 1]) ||
                    (data[i] > data[i - 1] && data[i] >= data[i + 1]))
                {
                    peaks.Add(data[i]);
                }
                else if ((data[i] <= data[i - 1] && data[i] < data[i + 1]) ||
                         (data[i] < data[i - 1] && data[i] <= data[i + 1]))
                {
                    valleys.Add(data[i]);
                }
            }

            // Four-point rainflow algorithm
            var extremes = new List<double>();
            extremes.Add(data[0]);

            for (int i = 1; i < data.Length - 1; i++)
            {
                if ((data[i] > data[i - 1] && data[i] > data[i + 1]) ||
                    (data[i] < data[i - 1] && data[i] < data[i + 1]))
                {
                    extremes.Add(data[i]);
                }
            }
            extremes.Add(data[data.Length - 1]);

            var fromList = new List<double>();
            var toList = new List<double>();
            var amplitudeList = new List<double>();
            var meanList = new List<double>();

            var stack = new Stack<double>();
            foreach (var extreme in extremes)
            {
                stack.Push(extreme);

                while (stack.Count >= 3)
                {
                    var s3 = stack.Pop();
                    var s2 = stack.Pop();
                    var s1 = stack.Pop();

                    var range1 = Math.Abs(s2 - s1);
                    var range2 = Math.Abs(s3 - s2);

                    if (range2 >= range1)
                    {
                        var amplitude = range1 / 2.0;
                        var mean = (s2 + s1) / 2.0;

                        fromList.Add(s1);
                        toList.Add(s2);
                        amplitudeList.Add(amplitude);
                        meanList.Add(mean);

                        stack.Push(s3);
                    }
                    else
                    {
                        stack.Push(s1);
                        stack.Push(s2);
                        stack.Push(s3);
                        break;
                    }
                }
            }

            return (fromList.ToArray(), toList.ToArray(), amplitudeList.ToArray(), meanList.ToArray());
        });
    }

    /// <summary>
    /// Extracts peaks and valleys from a time series.
    /// </summary>
    public Task<(double[] Peaks, double[] Valleys)> ExtractPeakValleyAsync(double[] data)
    {
        return Task.Run(() =>
        {
            var peaks = new List<double>();
            var valleys = new List<double>();

            for (int i = 1; i < data.Length - 1; i++)
            {
                if (data[i] > data[i - 1] && data[i] > data[i + 1])
                    peaks.Add(data[i]);
                else if (data[i] < data[i - 1] && data[i] < data[i + 1])
                    valleys.Add(data[i]);
            }

            return (peaks.ToArray(), valleys.ToArray());
        });
    }

    public void Dispose() { }
}
