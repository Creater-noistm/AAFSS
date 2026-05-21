using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for frequency domain analysis including FFT, PSD,
/// octave band analysis, and cross-spectrum computation.
/// </summary>
public interface IFrequencyAnalysisService
{
    /// <summary>Computes a power spectrum (PSD using Welch's method).</summary>
    Task<SpectrumResult> ComputePsdAsync(Guid dataSourceId, SpectrumType spectrumType, FrequencyRange? range = null, CancellationToken ct = default);

    /// <summary>Computes octave band levels (1/1, 1/3, 1/6, 1/12 octave).</summary>
    Task<SpectrumResult> ComputeOctaveBandsAsync(Guid dataSourceId, SpectrumType octaveType, CancellationToken ct = default);

    /// <summary>Computes cross-spectrum between two data sources.</summary>
    Task<SpectrumResult> ComputeCrossSpectrumAsync(Guid dataSourceId1, Guid dataSourceId2, CancellationToken ct = default);

    /// <summary>Computes coherence between two data sources.</summary>
    Task<SpectrumResult> ComputeCoherenceAsync(Guid dataSourceId1, Guid dataSourceId2, CancellationToken ct = default);

    /// <summary>Performs zoom FFT on specified frequency range.</summary>
    Task<SpectrumResult> ComputeZoomFftAsync(Guid dataSourceId, FrequencyRange range, CancellationToken ct = default);
}
