namespace AAFSS.Core.Extensions;

/// <summary>
/// Extension methods for IEnumerable<double> providing common statistical
/// and signal-processing calculations used throughout the pipeline.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>Computes the arithmetic mean of the sequence.</summary>
    public static double Mean(this IEnumerable<double> source)
    {
        var list = source as IList<double> ?? source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Cannot compute mean of an empty sequence.");
        return list.Sum() / list.Count;
    }

    /// <summary>Computes the sample standard deviation (N-1 denominator).</summary>
    public static double StandardDeviation(this IEnumerable<double> source)
    {
        var list = source as IList<double> ?? source.ToList();
        if (list.Count < 2)
            throw new InvalidOperationException("Cannot compute standard deviation with fewer than 2 elements.");
        var avg = list.Mean();
        var sumSq = list.Sum(x => (x - avg) * (x - avg));
        return Math.Sqrt(sumSq / (list.Count - 1));
    }

    /// <summary>Computes the root mean square (RMS) value.</summary>
    public static double Rms(this IEnumerable<double> source)
    {
        var list = source as IList<double> ?? source.ToList();
        if (list.Count == 0) return 0;
        return Math.Sqrt(list.Sum(x => x * x) / list.Count);
    }

    /// <summary>Returns the peak value (maximum absolute value).</summary>
    public static double Peak(this IEnumerable<double> source)
    {
        var list = source as IList<double> ?? source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Cannot find peak of an empty sequence.");
        return list.Max(Math.Abs);
    }

    /// <summary>Computes the crest factor (Peak / RMS).</summary>
    public static double CrestFactor(this IEnumerable<double> source)
    {
        var list = source as IList<double> ?? source.ToList();
        var rms = list.Rms();
        if (rms == 0) return 0;
        return list.Peak() / rms;
    }

    /// <summary>Converts a linear amplitude sequence to decibels: 20*log10(x/ref).</summary>
    public static IEnumerable<double> ToDecibels(this IEnumerable<double> source, double reference = 1.0)
    {
        foreach (var value in source)
        {
            yield return value > 0 ? 20.0 * Math.Log10(value / reference) : double.NegativeInfinity;
        }
    }

    /// <summary>Converts a decibel sequence to linear amplitude: ref * 10^(dB/20).</summary>
    public static IEnumerable<double> FromDecibels(this IEnumerable<double> source, double reference = 1.0)
    {
        foreach (var db in source)
        {
            yield return reference * Math.Pow(10.0, db / 20.0);
        }
    }

    /// <summary>Applies a simple moving average filter with the given window size.</summary>
    public static double[] MovingAverage(this IReadOnlyList<double> source, int windowSize)
    {
        if (windowSize < 1)
            throw new ArgumentException("Window size must be at least 1.", nameof(windowSize));
        if (source.Count == 0)
            return Array.Empty<double>();

        var result = new double[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            int start = Math.Max(0, i - windowSize / 2);
            int end = Math.Min(source.Count - 1, i + windowSize / 2);
            double sum = 0;
            for (int j = start; j <= end; j++)
                sum += source[j];
            result[i] = sum / (end - start + 1);
        }
        return result;
    }

    /// <summary>Finds all local maxima in the sequence.</summary>
    public static List<int> FindPeakIndices(this IReadOnlyList<double> source)
    {
        var peaks = new List<int>();
        if (source.Count < 3) return peaks;

        for (int i = 1; i < source.Count - 1; i++)
        {
            if (source[i] > source[i - 1] && source[i] > source[i + 1])
                peaks.Add(i);
        }
        return peaks;
    }

    /// <summary>Finds all local minima in the sequence.</summary>
    public static List<int> FindValleyIndices(this IReadOnlyList<double> source)
    {
        var valleys = new List<int>();
        if (source.Count < 3) return valleys;

        for (int i = 1; i < source.Count - 1; i++)
        {
            if (source[i] < source[i - 1] && source[i] < source[i + 1])
                valleys.Add(i);
        }
        return valleys;
    }

    /// <summary>
    /// Computes the cumulative sum (running total) of the sequence.
    /// </summary>
    public static double[] CumulativeSum(this IEnumerable<double> source)
    {
        var list = source.ToList();
        var result = new double[list.Count];
        double sum = 0;
        for (int i = 0; i < list.Count; i++)
        {
            sum += list[i];
            result[i] = sum;
        }
        return result;
    }

    /// <summary>
    /// Checks whether all values in the sequence are finite (not NaN or Infinity).
    /// </summary>
    public static bool AllFinite(this IEnumerable<double> source)
    {
        return source.All(v => !double.IsNaN(v) && !double.IsInfinity(v));
    }

    /// <summary>
    /// Filters out NaN and Infinity values from the sequence.
    /// </summary>
    public static IEnumerable<double> WhereFinite(this IEnumerable<double> source)
    {
        return source.Where(v => !double.IsNaN(v) && !double.IsInfinity(v));
    }
}
