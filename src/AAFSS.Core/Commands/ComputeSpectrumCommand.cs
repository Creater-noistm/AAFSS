using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to compute a frequency-domain spectrum from time series data.
/// Supports PSD (Welch/Periodogram), octave band analysis, cross-spectrum,
/// coherence, and zoom FFT through the IFrequencyAnalysisService.
/// </summary>
public record ComputeSpectrumCommand(
    Guid DataSourceId,
    SpectrumType SpectrumType,
    FrequencyRange? FrequencyRange = null,
    Guid? CrossDataSourceId = null
) : IRequest<SpectrumResult>;
