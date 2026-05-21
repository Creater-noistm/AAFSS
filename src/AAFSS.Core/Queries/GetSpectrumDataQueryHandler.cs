using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Queries;

/// <summary>
/// Handles <see cref="GetSpectrumDataQuery"/> by loading the project aggregate
/// and locating the spectrum by ID, then mapping to a <see cref="SpectrumDataDto"/>.
/// </summary>
public class GetSpectrumDataQueryHandler : IRequestHandler<GetSpectrumDataQuery, SpectrumDataDto?>
{
    private readonly IProjectManagementService _projectService;
    private readonly ILogger<GetSpectrumDataQueryHandler> _logger;

    public GetSpectrumDataQueryHandler(
        IProjectManagementService projectService,
        ILogger<GetSpectrumDataQueryHandler> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public async Task<SpectrumDataDto?> Handle(GetSpectrumDataQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving spectrum data: ProjectId={ProjectId}, SpectrumId={SpectrumId}, IsCompiled={IsCompiled}",
            request.ProjectId, request.SpectrumId, request.IsCompiled);

        var project = await _projectService.GetProjectByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            _logger.LogWarning("Project not found: ProjectId={ProjectId}", request.ProjectId);
            return null;
        }

        if (request.IsCompiled)
        {
            return MapCompiledSpectrum(project, request.SpectrumId);
        }

        return MapSpectrumResult(project, request.SpectrumId);
    }

    private SpectrumDataDto? MapCompiledSpectrum(Project project, Guid spectrumId)
    {
        var spectrum = project.Spectra.FirstOrDefault(s => s.Id == spectrumId);
        if (spectrum == null)
        {
            _logger.LogWarning("Compiled spectrum not found: SpectrumId={SpectrumId} in ProjectId={ProjectId}",
                spectrumId, project.Id);
            return null;
        }

        return new SpectrumDataDto
        {
            Id = spectrum.Id,
            Name = spectrum.Name,
            Category = spectrum.Category.ToString(),
            SpectrumType = spectrum.SpectrumType.ToString(),
            Frequencies = spectrum.Frequencies,
            Amplitudes = spectrum.Levels,
            Oaspl = spectrum.Oaspl,
            DamageValue = spectrum.DamageValue,
            ValidationStatus = spectrum.ValidationStatus.ToString(),
            ComputedAt = spectrum.CompiledAt
        };
    }

    private SpectrumDataDto? MapSpectrumResult(Project project, Guid spectrumResultId)
    {
        foreach (var profile in project.Profiles)
        {
            foreach (var ds in profile.DataSources)
            {
                var result = ds.SpectrumResults.FirstOrDefault(sr => sr.Id == spectrumResultId);
                if (result != null)
                {
                    return new SpectrumDataDto
                    {
                        Id = result.Id,
                        Name = $"{Path.GetFileName(ds.FilePath)} - {result.SpectrumType}",
                        Category = "Source",
                        SpectrumType = result.SpectrumType.ToString(),
                        Frequencies = result.Frequencies,
                        Amplitudes = result.Amplitudes,
                        Oaspl = result.Oaspl,
                        DamageValue = null,
                        ValidationStatus = null,
                        ComputedAt = result.ComputedAt
                    };
                }
            }
        }

        _logger.LogWarning("Spectrum result not found: SpectrumResultId={SpectrumResultId} in ProjectId={ProjectId}",
            spectrumResultId, project.Id);
        return null;
    }
}
