namespace AAFSS.Core.Extensions;

/// <summary>
/// Extension methods for mathematical operations commonly used in acoustic fatigue analysis.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// Converts a decibel value to a linear ratio.
    /// </summary>
    /// <param name="db">Value in dB.</param>
    /// <param name="isPower">True for power quantities (factor 10), false for amplitude (factor 20).</param>
    /// <returns>Linear ratio.</returns>
    public static double DbToLinear(this double db, bool isPower = false)
    {
        var factor = isPower ? 10.0 : 20.0;
        return Math.Pow(10, db / factor);
    }

    /// <summary>
    /// Converts a linear ratio to decibels.
    /// </summary>
    /// <param name="linear">Linear ratio.</param>
    /// <param name="isPower">True for power quantities (factor 10), false for amplitude (factor 20).</param>
    /// <returns>Value in dB.</returns>
    public static double LinearToDb(this double linear, bool isPower = false)
    {
        if (linear <= 0) return double.NegativeInfinity;
        var factor = isPower ? 10.0 : 20.0;
        return factor * Math.Log10(linear);
    }

    /// <summary>
    /// Computes the root mean square (RMS) of an array.
    /// </summary>
    public static double Rms(this double[] values)
    {
        if (values.Length == 0) return 0;
        return Math.Sqrt(values.Sum(v => v * v) / values.Length);
    }

    /// <summary>
    /// Computes the RMS of an array with optional offset removal.
    /// </summary>
    public static double Rms(this double[] values, bool removeDc)
    {
        if (!removeDc || values.Length == 0) return values.Rms();
        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Length);
    }

    /// <summary>
    /// Computes the peak value (maximum absolute) of an array.
    /// </summary>
    public static double Peak(this double[] values)
    {
        if (values.Length == 0) return 0;
        return values.Max(Math.Abs);
    }

    /// <summary>
    /// Computes the crest factor (Peak / RMS).
    /// </summary>
    public static double CrestFactor(this double[] values)
    {
        var rms = values.Rms(true);
        if (rms <= 0) return 0;
        return values.Peak() / rms;
    }

    /// <summary>
    /// Computes basic statistical moments of an array.
    /// </summary>
    public static (double mean, double stdDev, double skewness, double kurtosis) Moments(this double[] values)
    {
        if (values.Length == 0) return (0, 0, 0, 0);

        var n = (double)values.Length;
        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / (n - 1);
        var stdDev = Math.Sqrt(variance);

        var m3 = values.Sum(v => Math.Pow(v - mean, 3)) / n;
        var m4 = values.Sum(v => Math.Pow(v - mean, 4)) / n;

        var skewness = stdDev > 0 ? m3 / Math.Pow(stdDev, 3) : 0;
        var kurtosis = stdDev > 0 ? (m4 / Math.Pow(stdDev, 4)) - 3 : 0; // Excess kurtosis

        return (mean, stdDev, skewness, kurtosis);
    }

    /// <summary>
    /// Decimates an array by taking every Nth sample after low-pass filtering.
    /// </summary>
    /// <param name="values">Input array.</param>
    /// <param name="factor">Decimation factor.</param>
    /// <returns>Decimated array.</returns>
    public static double[] Decimate(this double[] values, int factor)
    {
        if (factor <= 1 || values.Length < factor)
            return values.ToArray();

        var result = new double[(values.Length + factor - 1) / factor];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = values[i * factor];
        }
        return result;
    }

    /// <summary>
    /// Downsamples an array to a maximum number of points for display purposes.
    /// Uses min-max decimation to preserve peaks.
    /// </summary>
    /// <param name="values">Input array.</param>
    /// <param name="maxPoints">Maximum number of output points.</param>
    /// <returns>Downsampled array.</returns>
    public static double[] DownsampleForDisplay(this double[] values, int maxPoints)
    {
        if (values.Length <= maxPoints || maxPoints <= 0)
            return values.ToArray();

        var bucketSize = values.Length / maxPoints;
        if (bucketSize <= 1) return values.ToArray();

        var result = new double[maxPoints];
        for (int i = 0; i < maxPoints; i++)
        {
            var start = i * bucketSize;
            var end = Math.Min(start + bucketSize, values.Length);
            var slice = values[start..end];
            // Preserve peak by taking max absolute value
            var maxAbs = slice.Max(Math.Abs);
            var maxVal = slice.First(v => Math.Abs(v) == maxAbs);
            result[i] = maxVal;
        }
        return result;
    }

    /// <summary>
    /// Linearly interpolates between two points.
    /// </summary>
    public static double Lerp(this double x, double x0, double x1, double y0, double y1)
    {
        if (Math.Abs(x1 - x0) < 1e-10) return (y0 + y1) / 2;
        return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
    }

    /// <summary>
    /// Computes the Miner's cumulative damage from cycle counts and S-N curve.
    /// </summary>
    /// <param name="amplitudes">Cycle amplitudes (stress in MPa).</param>
    /// <param name="counts">Cycle counts per amplitude.</param>
    /// <param name="fatigueStrengthCoefficient">Sf' in MPa.</param>
    /// <param name="fatigueStrengthExponent">b (negative).</param>
    /// <returns>Cumulative damage D.</returns>
    public static double MinersDamage(this double[] amplitudes, int[] counts,
        double fatigueStrengthCoefficient, double fatigueStrengthExponent)
    {
        if (amplitudes.Length == 0 || counts.Length != amplitudes.Length)
            return 0;

        double d = 0;
        for (int i = 0; i < amplitudes.Length; i++)
        {
            if (amplitudes[i] <= 0 || counts[i] <= 0) continue;
            var nf = Math.Pow(amplitudes[i] / fatigueStrengthCoefficient, 1.0 / fatigueStrengthExponent);
            if (nf > 0)
                d += counts[i] / nf;
        }
        return d;
    }
}
