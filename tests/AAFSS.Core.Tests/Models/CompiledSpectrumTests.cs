using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class CompiledSpectrumTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var spectrum = new CompiledSpectrum();

        spectrum.Id.Should().NotBe(Guid.Empty);
        spectrum.ProjectId.Should().Be(Guid.Empty);
        spectrum.Name.Should().BeEmpty();
        spectrum.Category.Should().Be(SpectrumCategory.Base);
        spectrum.SpectrumType.Should().Be(SpectrumType.Octave1_3);
        spectrum.DamageValue.Should().Be(0);
        spectrum.ValidationStatus.Should().Be(ValidationStatus.Pending);
        spectrum.Method.Should().Be(CompilationMethod.StateRegionEnvelope);
        spectrum.EnvelopeOffset.Should().Be(0);
        spectrum.Oaspl.Should().Be(0);
    }

    [Fact]
    public void Frequencies_Setter_ShouldSerializeAndDeserialize()
    {
        var spectrum = new CompiledSpectrum();
        var freqs = new double[] { 20, 25, 31.5, 40, 50 };

        spectrum.Frequencies = freqs;

        spectrum.Frequencies.Should().Equal(20, 25, 31.5, 40, 50);
    }

    [Fact]
    public void Levels_Setter_ShouldSerializeAndDeserialize()
    {
        var spectrum = new CompiledSpectrum();
        var levels = new double[] { 80, 85, 90, 88, 82 };

        spectrum.Levels = levels;

        spectrum.Levels.Should().Equal(80, 85, 90, 88, 82);
    }

    [Fact]
    public void SourceSpectrumIds_Setter_ShouldSerializeAndDeserialize()
    {
        var spectrum = new CompiledSpectrum();
        var ids = new System.Collections.Generic.List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        spectrum.SourceSpectrumIds = ids;

        spectrum.SourceSpectrumIds.Should().HaveCount(2);
        spectrum.SourceSpectrumIds.Should().BeEquivalentTo(ids);
    }

    [Fact]
    public void SourceSpectrumIds_Getter_WhenEmpty_ShouldReturnEmptyList()
    {
        var spectrum = new CompiledSpectrum();
        spectrum.SourceSpectrumIds.Should().BeEmpty();
    }
}
