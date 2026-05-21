using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Python;
using Microsoft.Extensions.Logging;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Full implementation of ISpectrumCompilationService — the core spectrum
/// compilation pipeline that synthesizes acoustic fatigue load spectra from
/// multiple source spectra using equal-damage, envelope, and statistical methods.
///
/// Key compilation methods:
///   - MinerEquivalent: Equal-damage per-band synthesis
///       L_compiled[k] = L_ref[k] + 10 * log10(Sigma(D_i[k]) / D_ref[k]) / m
///   - MaxEnvelope: Per-band maximum across all source spectra
///   - StateRegionEnvelope: Weighted envelope with configurable offset
///   - StatisticalExtreme: P95 upper tolerance limit per band
/// </summary>
public class SpectrumCompilationService : ISpectrumCompilationService
{
    private readonly IUnitOfWork _uow;
    private readonly ISpectrumRepository _spectrumRepo;
    private readonly FatigueBridge _fatigueBridge;
    private readonly ILogger<SpectrumCompilationService> _logger;

    public SpectrumCompilationService(
        IUnitOfWork uow,
        ISpectrumRepository spectrumRepo,
        FatigueBridge fatigueBridge,
        ILogger<SpectrumCompilationService> logger)
    {
        _uow = uow;
        _spectrumRepo = spectrumRepo;
        _fatigueBridge = fatigueBridge;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CompiledSpectrum> CompileAsync(
        Guid projectId,
        string spectrumName,
        CompilationMethod method,
        List<Guid> sourceSpectrumIds,
        double envelopeOffset = 0,
        CancellationToken ct = default)
    {
        if (sourceSpectrumIds == null || sourceSpectrumIds.Count == 0)
            throw new ArgumentException("At least one source spectrum ID is required.", nameof(sourceSpectrumIds));

        _logger.LogInformation(
            "Compiling spectrum: ProjectId={ProjectId}, Name={Name}, Method={Method}, SourceCount={Count}",
            projectId, spectrumName, method, sourceSpectrumIds.Count);

        // Load all source spectra
        var sourceSpectra = new List<SpectrumResult>();
        foreach (var id in sourceSpectrumIds)
        {
            var spectrum = await _spectrumRepo.GetResultByIdAsync(id, ct)
                ?? throw new InvalidOperationException($"Spectrum {id} not found.");
            sourceSpectra.Add(spectrum);
        }

        // Align frequency bands: find the reference spectrum (max bin count)
        var refSpectrum = sourceSpectra
            .OrderByDescending(s => s.BinCount)
            .First();

        var refFreqs = refSpectrum.Frequencies;
        var bandCount = refSpectrum.Frequencies.Length;

        double[] compiledFrequencies;
        double[] compiledLevels;
        SpectrumCategory category;

        switch (method)
        {
            case CompilationMethod.MinerEquivalent:
                category = SpectrumCategory.Base;
                (compiledFrequencies, compiledLevels) = await CompileMinerEquivalentAsync(
                    sourceSpectra, refFreqs, bandCount, ct);
                break;

            case CompilationMethod.MaxEnvelope:
                category = SpectrumCategory.Envelope;
                (compiledFrequencies, compiledLevels) = await CompileMaxEnvelopeAsync(
                    sourceSpectra, refFreqs, bandCount, envelopeOffset, ct);
                break;

            case CompilationMethod.StateRegionEnvelope:
                category = SpectrumCategory.Envelope;
                (compiledFrequencies, compiledLevels) = await CompileStateRegionEnvelopeAsync(
                    sourceSpectra, refFreqs, bandCount, envelopeOffset, ct);
                break;

            case CompilationMethod.StatisticalExtreme:
                category = SpectrumCategory.Severe;
                (compiledFrequencies, compiledLevels) = await CompileStatisticalExtremeAsync(
                    sourceSpectra, refFreqs, bandCount, ct);
                break;

            case CompilationMethod.FlightByFlight:
                category = SpectrumCategory.FlightByFlight;
                (compiledFrequencies, compiledLevels) = await CompileFlightByFlightAsync(
                    sourceSpectra, refFreqs, bandCount, ct);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(method), $"Unknown compilation method: {method}");
        }

        // Compute OASPL from compiled spectrum
        var oaspl = ComputeOaspl(compiledLevels);

        // Estimate cumulative damage using a reference S-N curve
        var damageValue = await EstimateCumulativeDamageAsync(compiledLevels, ct);

        var compiled = new CompiledSpectrum
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = spectrumName,
            Category = category,
            SpectrumType = SpectrumType.Octave1_3,
            Method = method,
            Frequencies = compiledFrequencies,
            Levels = compiledLevels,
            Oaspl = oaspl,
            DamageValue = damageValue,
            EnvelopeOffset = envelopeOffset,
            SourceSpectrumIds = sourceSpectrumIds,
            CompiledAt = DateTime.UtcNow,
            ValidationStatus = ValidationStatus.Pending
        };

        await _spectrumRepo.AddCompiledAsync(compiled, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Spectrum compiled: Id={SpectrumId}, OASPL={Oaspl:F2}dB, Damage={Damage:F6}",
            compiled.Id, compiled.Oaspl, compiled.DamageValue);

        return compiled;
    }

    /// <inheritdoc />
    public async Task<CompiledSpectrum> SmoothAsync(Guid spectrumId, SmoothingConfig config, CancellationToken ct = default)
    {
        _logger.LogInformation("Smoothing spectrum {Id} with method {Method}, window={Window}",
            spectrumId, config.Method, config.WindowSize);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        var levels = spectrum.Levels;
        var smoothed = config.Method.ToLowerInvariant() switch
        {
            "movingaverage" => ApplyMovingAverage(levels, config.WindowSize),
            "savitzkygolay" => ApplySavitzkyGolay(levels, config.WindowSize, config.PolynomialOrder),
            _ => levels
        };

        var result = new CompiledSpectrum
        {
            Id = Guid.NewGuid(),
            ProjectId = spectrum.ProjectId,
            Name = $"{spectrum.Name} (Smoothed)",
            Category = SpectrumCategory.Smoothed,
            SpectrumType = spectrum.SpectrumType,
            Method = spectrum.Method,
            Frequencies = spectrum.Frequencies,
            Levels = smoothed,
            Oaspl = ComputeOaspl(smoothed),
            DamageValue = spectrum.DamageValue,
            SourceSpectrumIds = new List<Guid> { spectrum.Id },
            CompiledAt = DateTime.UtcNow,
            ValidationStatus = ValidationStatus.Pending
        };

        await _spectrumRepo.AddCompiledAsync(result, ct);
        await _uow.SaveChangesAsync(ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<CompiledSpectrum> ApplyGoodmanCorrectionAsync(
        Guid spectrumId, GoodmanCorrectionConfig config, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying Goodman correction to spectrum {Id}, UTS={Uts}MPa",
            spectrumId, config.UltimateTensileStrength);

        var spectrum = await _spectrumRepo.GetCompiledByIdAsync(spectrumId, ct)
            ?? throw new InvalidOperationException($"Compiled spectrum {spectrumId} not found.");

        // Goodman correction on levels (treating dB levels as proxy for stress)
        var levels = spectrum.Levels;
        var freqCount = levels.Length;
        var means = Enumerable.Repeat(config.MeanStress, freqCount).ToArray();
        var corrected = await _fatigueBridge.GoodmanCorrectionAsync(
            levels, means, config.UltimateTensileStrength);

        var result = new CompiledSpectrum
        {
            Id = Guid.NewGuid(),
            ProjectId = spectrum.ProjectId,
            Name = $"{spectrum.Name} (Goodman)",
            Category = SpectrumCategory.Corrected,
            SpectrumType = spectrum.SpectrumType,
            Method = spectrum.Method,
            Frequencies = spectrum.Frequencies,
            Levels = corrected,
            Oaspl = ComputeOaspl(corrected),
            DamageValue = spectrum.DamageValue,
            SourceSpectrumIds = new List<Guid> { spectrum.Id },
            CompiledAt = DateTime.UtcNow,
            ValidationStatus = ValidationStatus.Pending
        };

        await _spectrumRepo.AddCompiledAsync(result, ct);
        await _uow.SaveChangesAsync(ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<CompiledSpectrum> CreateEnvelopeAsync(
        Guid projectId, string name, List<Guid> spectrumIds, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating envelope from {Count} spectra", spectrumIds?.Count ?? 0);

        if (spectrumIds == null || spectrumIds.Count == 0)
            throw new ArgumentException("At least one spectrum ID is required.", nameof(spectrumIds));

        var spectra = new List<CompiledSpectrum>();
        foreach (var id in spectrumIds)
        {
            var spectrum = await _spectrumRepo.GetCompiledByIdAsync(id, ct);
            if (spectrum != null)
                spectra.Add(spectrum);
        }

        if (spectra.Count == 0)
            throw new InvalidOperationException("No valid spectra found for envelope creation.");

        // Find the reference (max band count)
        var refSpectrum = spectra.OrderByDescending(s => s.Frequencies.Length).First();
        var bandCount = refSpectrum.Frequencies.Length;
        var freqs = refSpectrum.Frequencies;

        // Per-band maximum
        var levels = new double[bandCount];
        for (int k = 0; k < bandCount; k++)
        {
            double maxLevel = double.MinValue;
            foreach (var spec in spectra)
            {
                if (k < spec.Frequencies.Length)
                {
                    maxLevel = Math.Max(maxLevel, spec.Levels[k]);
                }
            }
            levels[k] = maxLevel > double.MinValue ? maxLevel : 0;
        }

        var oaspl = ComputeOaspl(levels);

        var result = new CompiledSpectrum
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Category = SpectrumCategory.Envelope,
            SpectrumType = refSpectrum.SpectrumType,
            Method = CompilationMethod.MaxEnvelope,
            Frequencies = freqs,
            Levels = levels,
            Oaspl = oaspl,
            SourceSpectrumIds = spectrumIds,
            CompiledAt = DateTime.UtcNow,
            ValidationStatus = ValidationStatus.Pending
        };

        await _spectrumRepo.AddCompiledAsync(result, ct);
        await _uow.SaveChangesAsync(ct);

        return result;
    }

    // ─── Private compilation methods ───────────────────────────────────

    /// <summary>
    /// Equal-damage per-band compilation (Miner equivalent).
    ///
    /// For each frequency band k:
    ///   L_compiled[k] = L_ref[k] + 10 * log10(Sum(D_i[k]) / D_ref[k]) / m
    ///
    /// Uses S-N exponent m = 4.0 as default for aluminum aerospace structures.
    /// </summary>
    private async Task<(double[] freqs, double[] levels)> CompileMinerEquivalentAsync(
        List<SpectrumResult> sourceSpectra, double[] refFreqs, int bandCount, CancellationToken ct)
    {
        const double m = 4.0; // Default S-N exponent for aluminum
        const double C = 1e12; // Reference S-N coefficient (cancels out in ratio)

        var compiledLevels = new double[bandCount];

        for (int k = 0; k < bandCount; k++)
        {
            double sumDamage = 0.0;
            double refLevel = double.MinValue;
            int validSpectra = 0;

            foreach (var spectrum in sourceSpectra)
            {
                if (k >= spectrum.Frequencies.Length) continue;

                var levelDb = spectrum.Amplitudes[k];
                refLevel = Math.Max(refLevel, levelDb);

                // Convert dB SPL to equivalent stress proxy for damage
                var stressProxy = Math.Pow(10, levelDb / 20.0);
                var life = C * Math.Pow(stressProxy, -m);
                if (life > 0)
                {
                    sumDamage += 1.0 / life;
                    validSpectra++;
                }
            }

            if (validSpectra > 0 && sumDamage > 0)
            {
                // Reference damage from reference spectrum
                var refStress = Math.Pow(10, refLevel / 20.0);
                var refLife = C * Math.Pow(refStress, -m);
                var refDamage = refLife > 0 ? 1.0 / refLife : 1e-12;

                // Equal-damage synthesis: L_comp = L_ref + 10*log10(sumD/D_ref)/m
                var damageRatio = sumDamage / refDamage;
                var deltaDb = 10.0 * Math.Log10(Math.Max(damageRatio, 1.0)) / m;
                compiledLevels[k] = refLevel + deltaDb;
            }
            else
            {
                compiledLevels[k] = refLevel > double.MinValue ? refLevel : 0;
            }
        }

        // Copy reference frequencies
        var freqs = new double[refFreqs.Length];
        Array.Copy(refFreqs, freqs, refFreqs.Length);

        return (freqs, compiledLevels);
    }

    /// <summary>
    /// Per-band maximum envelope across all source spectra.
    /// </summary>
    private Task<(double[] freqs, double[] levels)> CompileMaxEnvelopeAsync(
        List<SpectrumResult> sourceSpectra, double[] refFreqs, int bandCount,
        double offset, CancellationToken ct)
    {
        var levels = new double[bandCount];

        for (int k = 0; k < bandCount; k++)
        {
            double maxLevel = double.MinValue;
            foreach (var spectrum in sourceSpectra)
            {
                if (k < spectrum.Frequencies.Length)
                    maxLevel = Math.Max(maxLevel, spectrum.Amplitudes[k]);
            }
            levels[k] = (maxLevel > double.MinValue ? maxLevel : 0) + offset;
        }

        var freqs = new double[refFreqs.Length];
        Array.Copy(refFreqs, freqs, refFreqs.Length);

        return Task.FromResult((freqs, levels));
    }

    /// <summary>
    /// State-region weighted envelope: weighted average of per-band maxima
    /// across different flight state regions.
    /// </summary>
    private Task<(double[] freqs, double[] levels)> CompileStateRegionEnvelopeAsync(
        List<SpectrumResult> sourceSpectra, double[] refFreqs, int bandCount,
        double offset, CancellationToken ct)
    {
        var levels = new double[bandCount];

        for (int k = 0; k < bandCount; k++)
        {
            double sumLevel = 0.0;
            int count = 0;

            foreach (var spectrum in sourceSpectra)
            {
                if (k < spectrum.Frequencies.Length)
                {
                    sumLevel += Math.Pow(10, spectrum.Amplitudes[k] / 10.0);
                    count++;
                }
            }

            if (count > 0)
            {
                // Energy-averaged level
                levels[k] = 10.0 * Math.Log10(sumLevel / count) + offset;
            }
        }

        var freqs = new double[refFreqs.Length];
        Array.Copy(refFreqs, freqs, refFreqs.Length);

        return Task.FromResult((freqs, levels));
    }

    /// <summary>
    /// Statistical extreme: mean + 2*sigma per band across spectra.
    /// </summary>
    private Task<(double[] freqs, double[] levels)> CompileStatisticalExtremeAsync(
        List<SpectrumResult> sourceSpectra, double[] refFreqs, int bandCount,
        CancellationToken ct)
    {
        var levels = new double[bandCount];

        for (int k = 0; k < bandCount; k++)
        {
            var bandLevels = new List<double>();
            foreach (var spectrum in sourceSpectra)
            {
                if (k < spectrum.Frequencies.Length)
                    bandLevels.Add(spectrum.Amplitudes[k]);
            }

            if (bandLevels.Count > 0)
            {
                var mean = bandLevels.Average();
                var n = bandLevels.Count;

                if (n > 1)
                {
                    var stdev = Math.Sqrt(bandLevels.Sum(x => (x - mean) * (x - mean)) / (n - 1));
                    levels[k] = mean + 2.0 * stdev; // P95 upper bound
                }
                else
                {
                    levels[k] = mean;
                }
            }
        }

        var freqs = new double[refFreqs.Length];
        Array.Copy(refFreqs, freqs, refFreqs.Length);

        return Task.FromResult((freqs, levels));
    }

    /// <summary>
    /// Flight-by-flight compilation: sequential concatenation of per-flight spectra.
    /// Simplified as weighted averaging with per-spectrum weight = 1.
    /// </summary>
    private Task<(double[] freqs, double[] levels)> CompileFlightByFlightAsync(
        List<SpectrumResult> sourceSpectra, double[] refFreqs, int bandCount,
        CancellationToken ct)
    {
        // For flight-by-flight, each spectrum represents one flight
        // Average energy across all flights
        var levels = new double[bandCount];

        for (int k = 0; k < bandCount; k++)
        {
            double sumEnergy = 0.0;
            int count = 0;

            foreach (var spectrum in sourceSpectra)
            {
                if (k < spectrum.Frequencies.Length)
                {
                    sumEnergy += Math.Pow(10, spectrum.Amplitudes[k] / 10.0);
                    count++;
                }
            }

            levels[k] = count > 0 ? 10.0 * Math.Log10(sumEnergy / count) : 0;
        }

        var freqs = new double[refFreqs.Length];
        Array.Copy(refFreqs, freqs, refFreqs.Length);

        return Task.FromResult((freqs, levels));
    }

    // ─── Helper methods ────────────────────────────────────────────────

    private static double ComputeOaspl(double[] levels)
    {
        if (levels.Length == 0) return 0;
        var sum = levels.Sum(l => Math.Pow(10, l / 10.0));
        return sum > 0 ? 10.0 * Math.Log10(sum) : 0;
    }

    private async Task<double> EstimateCumulativeDamageAsync(double[] levels, CancellationToken ct)
    {
        // Reference S-N curve: C=1e12, m=4.0 (aluminum structures)
        const double m = 4.0;
        const double C = 1e12;
        const double refHours = 1000.0; // Reference exposure: 1000 flight hours

        double totalDamage = 0.0;
        foreach (var level in levels)
        {
            if (level <= 0) continue;
            var stressProxy = Math.Pow(10, level / 20.0);
            var life = C * Math.Pow(stressProxy, -m);
            if (life > 0)
            {
                // Assume 1e6 cycles per band at the dominant frequency
                totalDamage += 1e6 / life;
            }
        }

        return totalDamage * refHours;
    }

    private static double[] ApplyMovingAverage(double[] data, int windowSize)
    {
        if (windowSize <= 1 || data.Length < windowSize)
            return (double[])data.Clone();

        var result = new double[data.Length];
        var halfWindow = windowSize / 2;

        for (int i = 0; i < data.Length; i++)
        {
            var start = Math.Max(0, i - halfWindow);
            var end = Math.Min(data.Length - 1, i + halfWindow);
            var sum = 0.0;
            var count = 0;
            for (int j = start; j <= end; j++)
            {
                sum += data[j];
                count++;
            }
            result[i] = count > 0 ? sum / count : data[i];
        }

        return result;
    }

    /// <summary>
    /// Applies Savitzky-Golay polynomial smoothing filter.
    /// Fits a least-squares polynomial of given order over a sliding window
    /// and replaces each data point with the polynomial's value at the window center.
    /// </summary>
    private static double[] ApplySavitzkyGolay(double[] data, int windowSize, int polyOrder)
    {
        // Fallback to moving average for invalid parameters
        if (windowSize <= polyOrder || windowSize % 2 == 0)
            return ApplyMovingAverage(data, windowSize);

        if (data.Length < windowSize)
            return ApplyMovingAverage(data, windowSize);

        var result = new double[data.Length];
        var halfWindow = windowSize / 2;

        // Precompute SG convolution coefficients for the full symmetric window.
        double[] fullCoeffs = ComputeSGCoefficients(windowSize, polyOrder);

        for (int i = 0; i < data.Length; i++)
        {
            int left = i - halfWindow;
            int right = i + halfWindow;

            if (left < 0)
            {
                // Left edge — asymmetric window [0, i + halfWindow]
                int actualWindow = right + 1;
                double[] coeffs = ComputeSGCoefficients(actualWindow, polyOrder);
                result[i] = ConvolveSG(data, 0, actualWindow, coeffs, i);
            }
            else if (right >= data.Length)
            {
                // Right edge — asymmetric window [i - halfWindow, data.Length - 1]
                int actualWindow = data.Length - left;
                double[] coeffs = ComputeSGCoefficients(actualWindow, polyOrder);
                result[i] = ConvolveSG(data, left, actualWindow, coeffs, i);
            }
            else
            {
                // Interior point — full symmetric window
                result[i] = ConvolveSG(data, left, windowSize, fullCoeffs, i);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes Savitzky-Golay filter coefficients via least-squares polynomial fitting.
    /// Returns the first row of (A^T·A)^-1·A^T, which gives the smoothed value at t=0.
    /// </summary>
    private static double[] ComputeSGCoefficients(int windowSize, int polyOrder)
    {
        int n = windowSize;
        int p = polyOrder;
        int m = (n - 1) / 2; // center index

        // Build design matrix A: n rows, (p+1) columns
        // A[j, k] = (j - m)^k
        int cols = p + 1;
        double[,] A = new double[n, cols];
        for (int j = 0; j < n; j++)
        {
            double t = j - m;
            double pow = 1.0;
            for (int k = 0; k < cols; k++)
            {
                A[j, k] = pow;
                pow *= t;
            }
        }

        // Compute A^T·A (cols × cols)
        double[,] ATA = new double[cols, cols];
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                double sum = 0;
                for (int r = 0; r < n; r++)
                    sum += A[r, i] * A[r, j];
                ATA[i, j] = sum;
            }
        }

        // Invert A^T·A via Gaussian elimination (small matrix, use double arithmetic)
        double[,] ATAinv = InvertMatrix(ATA, cols);

        // First row of pseudo-inverse: pinv[0, j] = sum_k ATAinv[0, k] * A[j, k]
        // Actually: pinv = (A^T·A)^-1 · A^T
        // pinv[0, j] = sum_k ATAinv[0, k] * A^T[k, j] = sum_k ATAinv[0, k] * A[j, k]
        double[] coeffs = new double[n];
        for (int j = 0; j < n; j++)
        {
            double sum = 0;
            for (int k = 0; k < cols; k++)
                sum += ATAinv[0, k] * A[j, k];
            coeffs[j] = sum;
        }

        return coeffs;
    }

    /// <summary>
    /// Applies precomputed SG coefficients to a window of data.
    /// </summary>
    private static double ConvolveSG(double[] data, int start, int windowSize, double[] coeffs, int targetIndex)
    {
        double sum = 0;
        for (int j = 0; j < windowSize; j++)
            sum += coeffs[j] * data[start + j];
        return sum;
    }

    /// <summary>
    /// Inverts a square matrix via Gauss-Jordan elimination with partial pivoting.
    /// </summary>
    private static double[,] InvertMatrix(double[,] matrix, int n)
    {
        // Augment with identity
        int n2 = 2 * n;
        double[,] aug = new double[n, n2];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = matrix[i, j];
            aug[i, i + n] = 1.0;
        }

        // Gauss-Jordan with partial pivoting
        for (int col = 0; col < n; col++)
        {
            // Find pivot
            int pivotRow = col;
            double maxVal = Math.Abs(aug[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                double val = Math.Abs(aug[row, col]);
                if (val > maxVal)
                {
                    maxVal = val;
                    pivotRow = row;
                }
            }

            // Swap rows
            if (pivotRow != col)
            {
                for (int j = 0; j < n2; j++)
                {
                    double tmp = aug[col, j];
                    aug[col, j] = aug[pivotRow, j];
                    aug[pivotRow, j] = tmp;
                }
            }

            // Normalize pivot row
            double pivot = aug[col, col];
            if (Math.Abs(pivot) < 1e-15)
                throw new InvalidOperationException($"Singular matrix: cannot invert. Column {col} pivot is near-zero.");

            for (int j = 0; j < n2; j++)
                aug[col, j] /= pivot;

            // Eliminate other rows
            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                double factor = aug[row, col];
                for (int j = 0; j < n2; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        // Extract inverse
        double[,] inv = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                inv[i, j] = aug[i, j + n];

        return inv;
    }
}
