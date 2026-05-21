using AAFSS.Core.Models;

namespace AAFSS.Core.Extensions;

/// <summary>
/// Extension methods for spectrum data manipulation, including OASPL calculation,
/// spectrum arithmetic, envelope computation, and format conversion.
/// </summary>
public static class SpectrumExtensions
{
    /// <summary>
    /// Computes the Overall Sound Pressure Level (OASPL) from frequency bands in dB.
    /// OASPL = 10 * log10(Σ 10^(Li/10))
    /// </summary>
    public static double ComputeOaspl(this IReadOnlyList<double> bandLevelsDb)
    {
        if (bandLevelsDb.Count == 0) return double.NegativeInfinity;

        double sumPower = 0;
        for (int i = 0; i < bandLevelsDb.Count; i++)
        {
            sumPower += Math.Pow(10.0, bandLevelsDb[i] / 10.0);
        }

        return 10.0 * Math.Log10(sumPower);
    }

    /// <summary>
    /// Computes the weighted sound pressure level using A-weighting.
    /// Reference: IEC 61672-1 A-weighting curve.
    /// </summary>
    public static double[] ApplyAWeighting(this IReadOnlyList<double> frequencies, IReadOnlyList<double> levelsDb)
    {
        if (frequencies.Count != levelsDb.Count)
            throw new ArgumentException("Frequency and level arrays must have the same length.");

        var result = new double[levelsDb.Count];
        for (int i = 0; i < levelsDb.Count; i++)
        {
            result[i] = levelsDb[i] + AWeightingCorrection(frequencies[i]);
        }
        return result;
    }

    /// <summary>
    /// Creates an envelope spectrum by taking the maximum level at each frequency
    /// across multiple input spectra. Frequencies must be aligned (same bin count).
    /// </summary>
    /// <param name="spectra">Collection of level arrays (each same length).</param>
    /// <returns>The envelope level array.</returns>
    public static double[] CreateEnvelope(this IEnumerable<double[]> spectra)
    {
        var list = spectra.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one spectrum is required.", nameof(spectra));

        var length = list[0].Length;
        // Validate all arrays have the same length
        if (list.Any(s => s.Length != length))
            throw new ArgumentException("All spectra must have the same number of frequency bins.");

        var envelope = new double[length];
        for (int i = 0; i < length; i++)
        {
            double max = double.NegativeInfinity;
            for (int j = 0; j < list.Count; j++)
            {
                if (!double.IsNaN(list[j][i]) && !double.IsInfinity(list[j][i]))
                    max = Math.Max(max, list[j][i]);
            }
            envelope[i] = max;
        }
        return envelope;
    }

    /// <summary>
    /// Adds a constant offset (in dB) to all levels in a spectrum.
    /// </summary>
    public static double[] AddOffset(this IReadOnlyList<double> levelsDb, double offsetDb)
    {
        var result = new double[levelsDb.Count];
        for (int i = 0; i < levelsDb.Count; i++)
        {
            result[i] = levelsDb[i] + offsetDb;
        }
        return result;
    }

    /// <summary>
    /// Converts a PSD spectrum (Pa²/Hz) to SPL in dB per band.
    /// SPL = 10*log10(PSD * Δf / Pref²), where Pref = 20 μPa.
    /// </summary>
    public static double[] PsdToSpl(this IReadOnlyList<double> psdValues, IReadOnlyList<double> frequencies)
    {
        if (psdValues.Count != frequencies.Count)
            throw new ArgumentException("PSD and frequency arrays must have the same length.");

        const double Pref = 20e-6; // 20 μPa
        const double PrefSq = Pref * Pref;

        var result = new double[psdValues.Count];
        for (int i = 0; i < psdValues.Count; i++)
        {
            // Δf = width of this frequency bin
            double deltaF = i == 0
                ? frequencies[1] - frequencies[0]
                : frequencies[i] - frequencies[i - 1];

            double value = psdValues[i] * deltaF / PrefSq;
            result[i] = value > 0 ? 10.0 * Math.Log10(value) : double.NegativeInfinity;
        }
        return result;
    }

    /// <summary>
    /// Performs Goodman mean stress correction on stress amplitudes.
    /// σa / σar = 1 - (σm / σuts)
    /// where σa = equivalent fully-reversed stress amplitude,
    /// σar = actual stress amplitude, σm = mean stress, σuts = ultimate tensile strength.
    /// </summary>
    public static double[] ApplyGoodmanCorrection(
        this IReadOnlyList<double> amplitudes,
        IReadOnlyList<double> meanStresses,
        double ultimateTensileStrength)
    {
        if (amplitudes.Count != meanStresses.Count)
            throw new ArgumentException("Amplitude and mean stress arrays must have the same length.");

        if (ultimateTensileStrength <= 0)
            throw new ArgumentException("Ultimate tensile strength must be positive.", nameof(ultimateTensileStrength));

        var result = new double[amplitudes.Count];
        for (int i = 0; i < amplitudes.Count; i++)
        {
            double ratio = 1.0 - (meanStresses[i] / ultimateTensileStrength);
            if (ratio <= 0)
            {
                result[i] = 0; // Mean stress exceeds UTS — component fails
            }
            else
            {
                result[i] = amplitudes[i] / ratio;
            }
        }
        return result;
    }

    /// <summary>
    /// Finds the first frequency bin index where the level exceeds a threshold.
    /// Returns -1 if no bin exceeds the threshold.
    /// </summary>
    public static int FindFirstExceedingBin(this IReadOnlyList<double> levelsDb, double thresholdDb)
    {
        for (int i = 0; i < levelsDb.Count; i++)
        {
            if (levelsDb[i] > thresholdDb)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Validates that the spectrum data is complete and consistent.
    /// Checks for: matching array lengths, monotonic frequencies, no NaN in levels.
    /// </summary>
    public static (bool IsValid, List<string> Errors) Validate(
        this IReadOnlyList<double> frequencies,
        IReadOnlyList<double> levels)
    {
        var errors = new List<string>();

        if (frequencies.Count == 0)
            errors.Add("Frequency array is empty.");
        if (levels.Count == 0)
            errors.Add("Level array is empty.");
        if (frequencies.Count != levels.Count)
            errors.Add($"Array length mismatch: {frequencies.Count} frequencies vs {levels.Count} levels.");

        // Check monotonic increasing frequencies
        for (int i = 1; i < frequencies.Count; i++)
        {
            if (frequencies[i] <= frequencies[i - 1])
            {
                errors.Add($"Frequencies not monotonic at index {i}: {frequencies[i - 1]} → {frequencies[i]}");
                break;
            }
        }

        // Check for NaN in levels
        for (int i = 0; i < levels.Count; i++)
        {
            if (double.IsNaN(levels[i]))
            {
                errors.Add($"NaN value in levels at index {i}.");
            }
        }

        return (errors.Count == 0, errors);
    }

    // ─── A-Weighting Helper ──────────────────────────────────────────

    /// <summary>
    /// Returns the A-weighting correction in dB for a given center frequency.
    /// Based on IEC 61672-1:2013.
    /// </summary>
    private static double AWeightingCorrection(double frequencyHz)
    {
        double f2 = frequencyHz * frequencyHz;
        double f4 = f2 * f2;

        double ra = (12194.0 * 12194.0 * f4) /
            ((f2 + 20.6 * 20.6) *
             Math.Sqrt((f2 + 107.7 * 107.7) * (f2 + 737.9 * 737.9)) *
             (f2 + 12194.0 * 12194.0));

        // To avoid log10(0), add a small epsilon
        double correction = 20.0 * Math.Log10(Math.Max(ra, 1e-30)) + 2.0;
        return correction;
    }
}
