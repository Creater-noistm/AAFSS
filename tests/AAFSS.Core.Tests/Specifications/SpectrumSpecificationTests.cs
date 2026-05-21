using AAFSS.Core.Models;
using AAFSS.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Specifications;

public class SpectrumSpecificationTests
{
    [Fact]
    public void ByProject_ShouldFilterCorrectly()
    {
        var projectId = Guid.NewGuid();
        var matching = new CompiledSpectrum { ProjectId = projectId };
        var nonMatching = new CompiledSpectrum { ProjectId = Guid.NewGuid() };

        var spec = SpectrumSpecification.ByProject(projectId);

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void ByCategory_ShouldFilterCorrectly()
    {
        var matching = new CompiledSpectrum { Category = SpectrumCategory.Severe };
        var nonMatching = new CompiledSpectrum { Category = SpectrumCategory.Base };

        var spec = SpectrumSpecification.ByCategory(SpectrumCategory.Severe);

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void ByMethod_ShouldFilterCorrectly()
    {
        var matching = new CompiledSpectrum { Method = CompilationMethod.MinerEquivalent };
        var nonMatching = new CompiledSpectrum { Method = CompilationMethod.MaxEnvelope };

        var spec = SpectrumSpecification.ByMethod(CompilationMethod.MinerEquivalent);

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void AboveOaspl_ShouldFilterCorrectly()
    {
        var high = new CompiledSpectrum { Oaspl = 150 };
        var low = new CompiledSpectrum { Oaspl = 100 };

        var spec = SpectrumSpecification.AboveOaspl(140);

        spec.IsSatisfiedBy(high).Should().BeTrue();
        spec.IsSatisfiedBy(low).Should().BeFalse();
    }

    [Fact]
    public void CreatedBetween_ShouldFilterCorrectly()
    {
        var now = DateTime.UtcNow;
        var inside = new CompiledSpectrum { CompiledAt = now.AddDays(-5) };
        var outside = new CompiledSpectrum { CompiledAt = now.AddDays(-20) };

        var spec = SpectrumSpecification.CreatedBetween(now.AddDays(-10), now);

        spec.IsSatisfiedBy(inside).Should().BeTrue();
        spec.IsSatisfiedBy(outside).Should().BeFalse();
    }

    [Fact]
    public void And_ShouldCombineSpecifications()
    {
        var projectId = Guid.NewGuid();
        var matching = new CompiledSpectrum
        {
            ProjectId = projectId,
            Category = SpectrumCategory.Envelope
        };

        var spec = SpectrumSpecification.ByProject(projectId)
            .And(SpectrumSpecification.ByCategory(SpectrumCategory.Envelope));

        spec.IsSatisfiedBy(matching).Should().BeTrue();
    }

    [Fact]
    public void Or_ShouldCombineWithOr()
    {
        var projectId = Guid.NewGuid();
        var ds = new CompiledSpectrum
        {
            ProjectId = Guid.NewGuid(),
            Category = SpectrumCategory.Envelope
        };

        var spec = SpectrumSpecification.ByProject(projectId)
            .Or(SpectrumSpecification.ByCategory(SpectrumCategory.Envelope));

        spec.IsSatisfiedBy(ds).Should().BeTrue();
    }

    [Fact]
    public void Not_ShouldNegateSpecification()
    {
        var spectrum = new CompiledSpectrum { Category = SpectrumCategory.Base };

        var spec = SpectrumSpecification.ByCategory(SpectrumCategory.Base).Not();

        spec.IsSatisfiedBy(spectrum).Should().BeFalse();
    }
}
