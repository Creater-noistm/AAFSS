using AAFSS.Core.Models;

namespace AAFSS.Core.Configuration;

/// <summary>
/// Defines 1/3 octave band center frequencies per ISO 266.
/// fc = 1000 × 2^(n/3), where n is the band index (typically -30 to +20 for audio range).
/// </summary>
public static class OctaveBandDefinition
{
    /// <summary>
    /// Standard 1/3 octave band center frequencies from 12.5 Hz to 20 kHz (ISO 266).
    /// </summary>
    public static readonly double[] StandardThirdOctaveFrequencies =
    {
        12.5, 16.0, 20.0, 25.0, 31.5, 40.0, 50.0, 63.0, 80.0, 100.0,
        125.0, 160.0, 200.0, 250.0, 315.0, 400.0, 500.0, 630.0, 800.0,
        1000.0, 1250.0, 1600.0, 2000.0, 2500.0, 3150.0, 4000.0, 5000.0,
        6300.0, 8000.0, 10000.0, 12500.0, 16000.0, 20000.0
    };

    /// <summary>
    /// 1/1 octave band center frequencies (ISO 266).
    /// </summary>
    public static readonly double[] StandardOctaveFrequencies =
    {
        16.0, 31.5, 63.0, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0
    };

    /// <summary>
    /// Computes the exact center frequency for a given band index n.
    /// fc = 1000 × 2^(n/3)
    /// </summary>
    /// <param name="n">Band index (0 corresponds to 1000 Hz for 1/3 octave).</param>
    /// <returns>Center frequency in Hz.</returns>
    public static double CenterFrequency(int n, int bandsPerOctave = 3)
    {
        return 1000.0 * Math.Pow(2.0, (double)n / bandsPerOctave);
    }

    /// <summary>
    /// Generates an array of center frequencies for the given range of band indices.
    /// </summary>
    /// <param name="startN">Starting band index.</param>
    /// <param name="endN">Ending band index (inclusive).</param>
    /// <param name="bandsPerOctave">Number of bands per octave (1, 3, 6, 12).</param>
    /// <returns>Array of center frequencies.</returns>
    public static double[] GenerateFrequencies(int startN, int endN, int bandsPerOctave = 3)
    {
        var count = endN - startN + 1;
        var frequencies = new double[count];
        for (int i = 0; i < count; i++)
        {
            frequencies[i] = CenterFrequency(startN + i, bandsPerOctave);
        }
        return frequencies;
    }

    /// <summary>
    /// Gets the band edges (lower and upper cutoff frequencies) for a given center frequency.
    /// </summary>
    /// <param name="centerFrequency">Center frequency in Hz.</param>
    /// <param name="bandsPerOctave">Bands per octave.</param>
    /// <returns>Tuple (lowerFc, upperFc).</returns>
    public static (double Lower, double Upper) BandEdges(double centerFrequency, int bandsPerOctave = 3)
    {
        var ratio = Math.Pow(2.0, 1.0 / (2.0 * bandsPerOctave));
        return (centerFrequency / ratio, centerFrequency * ratio);
    }

    /// <summary>
    /// Finds the 1/3 octave band index for a given frequency.
    /// </summary>
    /// <param name="frequency">Frequency in Hz.</param>
    /// <returns>Band index n such that fc ≈ frequency.</returns>
    public static int FindBandIndex(double frequency)
    {
        if (frequency <= 0)
            return -100;
        // Solve: 1000 * 2^(n/3) = frequency → n = 3 * log2(frequency/1000)
        return (int)Math.Round(3.0 * Math.Log2(frequency / 1000.0));
    }

    /// <summary>
    /// Creates a list of OctaveBandInfo for the standard audio range.
    /// </summary>
    public static List<OctaveBandInfo> CreateStandardBands()
    {
        return StandardThirdOctaveFrequencies
            .Select(fc =>
            {
                var (lower, upper) = BandEdges(fc);
                return new OctaveBandInfo
                {
                    CenterFrequency = fc,
                    LowerFrequency = lower,
                    UpperFrequency = upper,
                    BandIndex = FindBandIndex(fc)
                };
            })
            .ToList();
    }

    /// <summary>
    /// Calculates the OASPL from band SPL values using energy summation.
    /// OASPL = 10 * log10(Σ 10^(SPL_i/10))
    /// </summary>
    /// <param name="bandLevels">Array of SPL values per band in dB.</param>
    /// <returns>Overall SPL in dB.</returns>
    public static double CalculateOaspl(double[] bandLevels)
    {
        if (bandLevels.Length == 0)
            return double.NegativeInfinity;

        var sum = bandLevels.Sum(spl => Math.Pow(10.0, spl / 10.0));
        return 10.0 * Math.Log10(sum);
    }
}

/// <summary>
/// Information about a single octave band.
/// </summary>
public class OctaveBandInfo
{
    /// <summary>Center frequency in Hz.</summary>
    public double CenterFrequency { get; set; }

    /// <summary>Lower cutoff frequency in Hz.</summary>
    public double LowerFrequency { get; set; }

    /// <summary>Upper cutoff frequency in Hz.</summary>
    public double UpperFrequency { get; set; }

    /// <summary>Band index n (fc = 1000 × 2^(n/3)).</summary>
    public int BandIndex { get; set; }

    public override string ToString() => $"{CenterFrequency:F1} Hz [{LowerFrequency:F1} - {UpperFrequency:F1}]";
}
