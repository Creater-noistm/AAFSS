using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class SpectrumResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var result = new SpectrumResult();

        result.Id.Should().NotBe(Guid.Empty);
        result.DataSourceId.Should().Be(Guid.Empty);
        result.SpectrumType.Should().Be(SpectrumType.Octave1_3);
        result.Oaspl.Should().Be(0);
        result.WindowType.Should().Be("Hanning");
        result.FftSize.Should().Be(4096);
        result.OverlapRatio.Should().Be(0.5);
        result.BinCount.Should().Be(0);
    }

    [Fact]
    public void Frequencies_Setter_ShouldSerializeAndDeserialize()
    {
        var result = new SpectrumResult();
        var freqs = new double[] { 100, 200, 400, 800 };

        result.Frequencies = freqs;

        result.Frequencies.Should().Equal(100, 200, 400, 800);
        result.BinCount.Should().Be(4);
    }

    [Fact]
    public void Amplitudes_Setter_ShouldSerializeAndDeserialize()
    {
        var result = new SpectrumResult();
        var amps = new double[] { 85.0, 92.0, 88.0, 82.0 };

        result.Amplitudes = amps;

        result.Amplitudes.Should().Equal(85.0, 92.0, 88.0, 82.0);
    }

    [Fact]
    public void Frequencies_Getter_WhenEmpty_ShouldReturnEmptyArray()
    {
        var result = new SpectrumResult();
        result.Frequencies.Should().BeEmpty();
    }

    [Fact]
    public void Amplitudes_Getter_WhenEmpty_ShouldReturnEmptyArray()
    {
        var result = new SpectrumResult();
        result.Amplitudes.Should().BeEmpty();
    }
}
