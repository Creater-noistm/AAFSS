using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification that matches compiled spectra by their category.
/// Example: new SpectrumByCategorySpec(SpectrumCategory.Envelope)
/// </summary>
public class SpectrumByCategorySpec : Specification<CompiledSpectrum>
{
    private readonly SpectrumCategory _category;

    public SpectrumByCategorySpec(SpectrumCategory category)
    {
        _category = category;
    }

    public override bool IsSatisfiedBy(CompiledSpectrum candidate)
    {
        return candidate.Category == _category;
    }
}
