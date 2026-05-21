using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Bridge to Python fatigue analysis module (fatigue/damage.py, fatigue/goodman.py).
/// Provides S-N curve life calculation, Miner damage accumulation, Steinberg
/// three-band method, Dirlik frequency-domain fatigue, and Goodman mean stress
/// correction via Python scientific computing.
/// </summary>
public class FatigueBridge : IDisposable
{
    private readonly PythonScriptExecutor _executor;

    public FatigueBridge(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Computes S-N curve fatigue life for given stress amplitudes.
    /// N_f = C * sigma_a^(-m)
    /// </summary>
    public async Task<double[]> ComputeSNLifeAsync(double[] stressAmplitudes, double C, double m)
    {
        return await Task.Run(() =>
        {
            dynamic np = _executor.ImportModule("numpy");
            var amps = _executor.ToNumPyArray(stressAmplitudes);
            dynamic result = np.power(amps, -m) * C;
            return ConvertFromNumpy(result);
        });
    }

    /// <summary>
    /// Computes Miner's cumulative damage: D = sum(n_i / N_i).
    /// </summary>
    public Task<double> ComputeMinerDamageAsync(double[] cycles, double[] lives)
    {
        return Task.Run(() =>
        {
            if (cycles.Length != lives.Length)
                throw new ArgumentException("cycles and lives must have the same length.");

            double damage = 0.0;
            for (int i = 0; i < cycles.Length; i++)
            {
                if (lives[i] > 0)
                    damage += cycles[i] / lives[i];
            }
            return damage;
        });
    }

    /// <summary>
    /// Steinberg three-band method for random vibration fatigue.
    /// Returns (damage, fatigueLifeSeconds).
    /// </summary>
    public async Task<(double Damage, double FatigueLife)> SteinbergDamageAsync(
        double grms, double fn, double m, double C, double TSeconds)
    {
        return await Task.Run(() =>
        {
            // Positive zero-crossing rate approximates natural frequency
            var n0 = fn;

            // Three stress levels
            var sigma1 = 1.0 * grms;
            var sigma2 = 2.0 * grms;
            var sigma3 = 3.0 * grms;

            // Cycle counts per band
            var n1 = 0.683 * n0 * TSeconds;
            var n2 = 0.271 * n0 * TSeconds;
            var n3 = 0.0433 * n0 * TSeconds;

            // Fatigue life per level
            var N1 = C * Math.Pow(sigma1, -m);
            var N2 = C * Math.Pow(sigma2, -m);
            var N3 = C * Math.Pow(sigma3, -m);

            // Miner's cumulative damage
            var D = n1 / N1 + n2 / N2 + n3 / N3;
            var fatigueLife = D > 0 ? TSeconds / D : double.PositiveInfinity;

            return (D, fatigueLife);
        });
    }

    /// <summary>
    /// Dirlik frequency-domain fatigue damage from PSD.
    /// Returns (damage, fatigueLifeSeconds).
    /// </summary>
    public async Task<(double Damage, double FatigueLife)> DirlikDamageAsync(
        double[] psdFreqs, double[] psdValues, double m, double C, double TSeconds)
    {
        return await Task.Run(() =>
        {
            dynamic np = _executor.ImportModule("numpy");

            var freqs = _executor.ToNumPyArray(psdFreqs);
            var psd = _executor.ToNumPyArray(psdValues);

            // Spectral moments
            double m0 = (double)np.trapz(psd, freqs);
            double m1 = (double)np.trapz(np.multiply(psd, freqs), freqs);
            double m2 = (double)np.trapz(np.multiply(psd, np.power(freqs, 2)), freqs);
            double m4 = (double)np.trapz(np.multiply(psd, np.power(freqs, 4)), freqs);

            if (m0 <= 0 || m2 <= 0)
                return (0.0, double.PositiveInfinity);

            // Irregularity factor
            double gamma = m4 > 0 ? m2 / Math.Sqrt(m0 * m4) : 0.5;

            // Zero up-crossing and peak rates
            double eZero = Math.Sqrt(m2 / m0);
            double eP = m2 > 0 ? Math.Sqrt(m4 / m2) : eZero;

            // Dirlik parameters
            double xm = m4 > 0 ? (m1 / m0) * Math.Sqrt(m2 / m4) : 0.5;
            double denom = 1.0 + gamma * gamma;
            double D1 = 2.0 * (xm - gamma * gamma) / denom;
            double D2 = (1.0 - gamma - D1 + D1 * D1) / Math.Max(1.0 - gamma, 1e-10);
            double D3 = 1.0 - D1 - D2;
            double R = (gamma - xm - D1 * D1) / Math.Max(1.0 - gamma - D1 + D1 * D1, 1e-10);
            double Q = 1.25 * (gamma - D3 - D2 * R) / Math.Max(D1, 1e-10);

            // Stress range discretization
            double sMin = 0.01 * Math.Sqrt(m0);
            double sMax = 5.0 * Math.Sqrt(m0);
            int nBins = 200;
            double dS = (sMax - sMin) / nBins;
            double totalCycles = eP * TSeconds;

            double D = 0.0;
            for (int i = 0; i < nBins; i++)
            {
                double sMid = sMin + (i + 0.5) * dS;
                double Z = sMid / (2.0 * Math.Sqrt(m0));

                double pdf = (D1 / Math.Max(Q, 1e-10)) * Math.Exp(-Z / Math.Max(Q, 1e-10))
                    + (D2 * Z / Math.Max(R * R, 1e-10)) * Math.Exp(-Z * Z / (2.0 * Math.Max(R * R, 1e-10)))
                    + D3 * Z * Math.Exp(-Z * Z / 2.0);
                pdf /= (2.0 * Math.Sqrt(m0));
                pdf = Math.Max(pdf, 0);

                double ni = pdf * dS * totalCycles;
                if (ni <= 0) continue;

                double Ni = C * Math.Pow(sMid, -m);
                D += ni / Ni;
            }

            double fatigueLife = D > 0 ? TSeconds / D : double.PositiveInfinity;
            return (D, fatigueLife);
        });
    }

    /// <summary>
    /// Computes damage for a single frequency band from amplitude distribution.
    /// </summary>
    public async Task<double> BandDamageAsync(
        double[] amplitudes, double[] counts, double m, double C)
    {
        return await Task.Run(() =>
        {
            double damage = 0.0;
            for (int i = 0; i < amplitudes.Length; i++)
            {
                if (amplitudes[i] <= 0 || counts[i] <= 0) continue;
                double N = C * Math.Pow(amplitudes[i], -m);
                damage += counts[i] / N;
            }
            return damage;
        });
    }

    /// <summary>
    /// Goodman mean stress correction: sigma_ar = sigma_a / (1 - sigma_m / uts).
    /// </summary>
    public async Task<double[]> GoodmanCorrectionAsync(double[] amplitudes, double[] means, double uts)
    {
        return await Task.Run(() =>
        {
            if (amplitudes.Length != means.Length)
                throw new ArgumentException("amplitudes and means must have the same length.");

            var result = new double[amplitudes.Length];
            for (int i = 0; i < amplitudes.Length; i++)
            {
                var ratio = means[i] / uts;
                if (ratio >= 1.0)
                    throw new InvalidOperationException($"Mean stress exceeds UTS at index {i}: ratio={ratio:F4}");
                result[i] = amplitudes[i] / (1.0 - ratio);
            }
            return result;
        });
    }

    /// <summary>
    /// Morrow mean stress correction: sigma_ar = sigma_a / (1 - sigma_m / sigma_f').
    /// </summary>
    public async Task<double[]> MorrowCorrectionAsync(double[] amplitudes, double[] means, double fatigueStrengthCoefficient)
    {
        return await Task.Run(() =>
        {
            if (amplitudes.Length != means.Length)
                throw new ArgumentException("amplitudes and means must have the same length.");

            var result = new double[amplitudes.Length];
            for (int i = 0; i < amplitudes.Length; i++)
            {
                var ratio = means[i] / fatigueStrengthCoefficient;
                if (ratio >= 1.0)
                    throw new InvalidOperationException(
                        $"Mean stress exceeds fatigue strength coefficient at index {i}: ratio={ratio:F4}");
                result[i] = amplitudes[i] / (1.0 - ratio);
            }
            return result;
        });
    }

    private static double[] ConvertFromNumpy(dynamic npArray)
    {
        var result = new List<double>();
        foreach (var val in npArray)
            result.Add((double)val);
        return result.ToArray();
    }

    public void Dispose() { }
}
