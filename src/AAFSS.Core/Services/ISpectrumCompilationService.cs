using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Service for spectrum compilation — the core pipeline that combines
/// multiple spectral results into a compiled fatigue spectrum.
/// Supports state-region envelope, Miner's equivalent, flight-by-flight,
/// max envelope, and statistical extreme methods.
/// </summary>
public interface ISpectrumCompilationService
{
    /// <summary>Compiles spectra using the specified method.</summary>
    Task<CompiledSpectrum> CompileAsync(
        Guid projectId,
        string spectrumName,
        CompilationMethod method,
        List<Guid> sourceSpectrumIds,
        double envelopeOffset = 0,
        CancellationToken ct = default);

    /// <summary>Smoothes a compiled spectrum profile.</summary>
    Task<CompiledSpectrum> SmoothAsync(Guid spectrumId, SmoothingConfig config, CancellationToken ct = default);

    /// <summary>Applies Goodman correction to a compiled spectrum.</summary>
    Task<CompiledSpectrum> ApplyGoodmanCorrectionAsync(Guid spectrumId, GoodmanCorrectionConfig config, CancellationToken ct = default);

    /// <summary>Merges multiple compiled spectra into a single envelope spectrum.</summary>
    Task<CompiledSpectrum> CreateEnvelopeAsync(Guid projectId, string name, List<Guid> spectrumIds, CancellationToken ct = default);
}
