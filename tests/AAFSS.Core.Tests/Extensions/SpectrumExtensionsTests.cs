using AAFSS.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Extensions;

public class SpectrumExtensionsTests
{
    [Fact]
    public void ComputeOaspl_ShouldComputeCorrectly()
    {
        double[] levels = { 80, 80 };
        var oaspl = levels.ComputeOaspl();
        oaspl.Should().BeApproximately(83.01, 0.01);
    }

    [Fact]
    public void ComputeOaspl_EmptyArray_ShouldReturnNegativeInfinity()
    {
        double[] levels = Array.Empty<double>();
        levels.ComputeOaspl().Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void ComputeOaspl_SingleBand_ShouldReturnSame()
    {
        double[] levels = { 90.0 };
        levels.ComputeOaspl().Should().BeApproximately(90.0, 0.01);
    }

    [Fact]
    public void ApplyAWeighting_ShouldAdjustLevels()
    {
        double[] freqs = { 1000.0 };
        double[] levels = { 90.0 };

        var result = freqs.ApplyAWeighting(levels);

        result.Length.Should().Be(1);
        // A-weighting at 1000Hz is approximately 0dB
        result[0].Should().BeApproximately(90.0, 0.1);
    }

    [Fact]
    public void ApplyAWeighting_MismatchedLengths_ShouldThrow()
    {
        double[] freqs = { 100, 200 };
        double[] levels = { 80 };
        var act = () => freqs.ApplyAWeighting(levels);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateEnvelope_ShouldTakeMaxAtEachFrequency()
    {
        double[] s1 = { 80, 85, 82 };
        double[] s2 = { 82, 80, 85 };
        double[] s3 = { 78, 82, 88 };

        var envelope = new[] { s1, s2, s3 }.CreateEnvelope();

        envelope.Should().Equal(82, 85, 88);
    }

    [Fact]
    public void CreateEnvelope_SingleSpectrum_ShouldReturnSame()
    {
        double[] s = { 80, 85, 90 };
        var envelope = new[] { s }.CreateEnvelope();
        envelope.Should().Equal(80, 85, 90);
    }

    [Fact]
    public void CreateEnvelope_EmptyCollection_ShouldThrow()
    {
        var act = () => Enumerable.Empty<double[]>().CreateEnvelope();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateEnvelope_MismatchedLengths_ShouldThrow()
    {
        double[] s1 = { 80, 85 };
        double[] s2 = { 80, 85, 90 };
        var act = () => new[] { s1, s2 }.CreateEnvelope();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddOffset_ShouldAddConstantToAllLevels()
    {
        double[] levels = { 80, 85, 90 };

        var result = levels.AddOffset(5.0);

        result.Should().Equal(85, 90, 95);
    }

    [Fact]
    public void PsdToSpl_ShouldConvertCorrectly()
    {
        double[] psd = { 0.04 };
        double[] freqs = { 1000, 1010 };

        var result = psd.PsdToSpl(freqs);

        result[0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void PsdToSpl_MismatchedLengths_ShouldThrow()
    {
        double[] psd = { 1.0, 2.0 };
        double[] freqs = { 100.0 };
        var act = () => psd.PsdToSpl(freqs);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplyGoodmanCorrection_ShouldComputeCorrectly()
    {
        double[] amplitudes = { 100 };
        double[] meanStresses = { 200 };
        double uts = 800;

        var result = amplitudes.ApplyGoodmanCorrection(meanStresses, uts);

        result[0].Should().BeApproximately(133.33, 0.1);
    }

    [Fact]
    public void ApplyGoodmanCorrection_MeanExceedsUts_ShouldReturnZero()
    {
        double[] amplitudes = { 100 };
        double[] meanStresses = { 900 };
        double uts = 800;

        var result = amplitudes.ApplyGoodmanCorrection(meanStresses, uts);

        result[0].Should().Be(0);
    }

    [Fact]
    public void ApplyGoodmanCorrection_MismatchedLengths_ShouldThrow()
    {
        double[] amps = { 100, 200 };
        double[] means = { 50 };
        var act = () => amps.ApplyGoodmanCorrection(means, 800);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FindFirstExceedingBin_ShouldFindCorrectIndex()
    {
        double[] levels = { 80, 85, 90, 88, 82 };

        levels.FindFirstExceedingBin(87).Should().Be(2);
    }

    [Fact]
    public void FindFirstExceedingBin_NoneFound_ShouldReturnMinusOne()
    {
        double[] levels = { 80, 85, 82 };
        levels.FindFirstExceedingBin(100).Should().Be(-1);
    }

    [Fact]
    public void Validate_ValidSpectrum_ShouldReturnValid()
    {
        double[] freqs = { 100, 200, 400, 800 };
        double[] levels = { 80, 85, 90, 88 };

        var (isValid, errors) = freqs.Validate(levels);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NonMonotonicFreqs_ShouldReportError()
    {
        double[] freqs = { 100, 200, 150 };
        double[] levels = { 80, 85, 90 };

        var (isValid, errors) = freqs.Validate(levels);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("not monotonic"));
    }

    [Fact]
    public void Validate_NaNInLevels_ShouldReportError()
    {
        double[] freqs = { 100, 200 };
        double[] levels = { 80, double.NaN };

        var (isValid, errors) = freqs.Validate(levels);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("NaN"));
    }

    [Fact]
    public void Validate_MismatchedLengths_ShouldReportError()
    {
        double[] freqs = { 100, 200, 400 };
        double[] levels = { 80, 85 };

        var (isValid, errors) = freqs.Validate(levels);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("mismatch"));
    }
}
