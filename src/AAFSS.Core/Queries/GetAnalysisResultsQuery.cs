using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Queries;

/// <summary>
/// Query to retrieve analysis results (rainflow, statistical models, validation) for a data source.
/// </summary>
public record GetAnalysisResultsQuery : IRequest<AnalysisResultsDto>
{
    /// <summary>Data source ID.</summary>
    public Guid DataSourceId { get; init; }
}

/// <summary>
/// DTO combining all analysis results for a data source.
/// </summary>
public record AnalysisResultsDto
{
    /// <summary>Data source ID.</summary>
    public Guid DataSourceId { get; init; }

    /// <summary>Rainflow counting results.</summary>
    public List<RainflowResult> RainflowResults { get; init; } = new();

    /// <summary>Statistical model fits.</summary>
    public List<StatisticalModel> StatisticalModels { get; init; } = new();

    /// <summary>Spectrum results.</summary>
    public List<SpectrumResult> SpectrumResults { get; init; } = new();

    /// <summary>Compiled spectra that reference this data source.</summary>
    public List<CompiledSpectrum> CompiledSpectra { get; init; } = new();

    /// <summary>Summary of processing status.</summary>
    public ProcessingStatus OverallStatus { get; init; }

    /// <summary>Whether rainflow analysis has been performed.</summary>
    public bool HasRainflowResults { get; init; }

    /// <summary>Whether statistical modeling has been performed.</summary>
    public bool HasStatisticalModels { get; init; }

    /// <summary>Whether spectrum analysis has been performed.</summary>
    public bool HasSpectrumResults { get; init; }
}
