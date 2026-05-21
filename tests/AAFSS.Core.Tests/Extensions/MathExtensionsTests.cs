using AAFSS.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Extensions;

public class MathExtensionsTests
{
    [Fact]
    public void DbToLinear_Amplitude_ShouldConvertCorrectly()
    {
        20.0.DbToLinear().Should().BeApproximately(10.0, 0.01);
        0.0.DbToLinear().Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void DbToLinear_Power_ShouldConvertCorrectly()
    {
        20.0.DbToLinear(true).Should().BeApproximately(100.0, 0.01);
        10.0.DbToLinear(true).Should().BeApproximately(10.0, 0.01);
    }

    [Fact]
    public void LinearToDb_Amplitude_ShouldConvertCorrectly()
    {
        10.0.LinearToDb().Should().BeApproximately(20.0, 0.01);
        1.0.LinearToDb().Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void LinearToDb_Power_ShouldConvertCorrectly()
    {
        100.0.LinearToDb(true).Should().BeApproximately(20.0, 0.01);
        10.0.LinearToDb(true).Should().BeApproximately(10.0, 0.01);
    }

    [Fact]
    public void LinearToDb_Zero_ShouldReturnNegativeInfinity()
    {
        0.0.LinearToDb().Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Rms_Array_ShouldComputeCorrectly()
    {
        double[] values = { 3.0, 4.0 };
        values.Rms().Should().Be(5.0);
    }

    [Fact]
    public void Rms_EmptyArray_ShouldReturnZero()
    {
        double[] values = Array.Empty<double>();
        values.Rms().Should().Be(0);
    }

    [Fact]
    public void Rms_WithDcRemoval_ShouldRemoveMean()
    {
        double[] values = { 5.0, 5.0, 5.0 };
        values.Rms(true).Should().Be(0);
    }

    [Fact]
    public void Peak_ShouldReturnMaxAbsolute()
    {
        double[] values = { 1.0, -5.0, 3.0 };
        values.Peak().Should().Be(5.0);
    }

    [Fact]
    public void Peak_EmptyArray_ShouldReturnZero()
    {
        double[] values = Array.Empty<double>();
        values.Peak().Should().Be(0);
    }

    [Fact]
    public void CrestFactor_ShouldComputeCorrectly()
    {
        double[] values = { 3.0, 4.0 };
        values.CrestFactor().Should().Be(1.0);
    }

    [Fact]
    public void CrestFactor_ZeroRms_ShouldReturnZero()
    {
        double[] values = { 0, 0, 0 };
        values.CrestFactor().Should().Be(0);
    }

    [Fact]
    public void Moments_NormalDistribution_ShouldComputeCorrectly()
    {
        double[] values = { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var (mean, stdDev, skewness, kurtosis) = values.Moments();

        mean.Should().BeApproximately(3.0, 0.01);
        stdDev.Should().BeApproximately(1.581, 0.01);
    }

    [Fact]
    public void Moments_EmptyArray_ShouldReturnZeros()
    {
        double[] values = Array.Empty<double>();
        var (mean, stdDev, skewness, kurtosis) = values.Moments();

        mean.Should().Be(0);
        stdDev.Should().Be(0);
        skewness.Should().Be(0);
        kurtosis.Should().Be(0);
    }

    [Fact]
    public void Decimate_ShouldReduceSampleCount()
    {
        double[] values = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0 };

        var result = values.Decimate(2);
        result.Should().Equal(1.0, 3.0, 5.0, 7.0);
    }

    [Fact]
    public void Decimate_FactorOne_ShouldReturnCopy()
    {
        double[] values = { 1.0, 2.0, 3.0 };
        var result = values.Decimate(1);
        result.Should().Equal(1.0, 2.0, 3.0);
    }

    [Fact]
    public void DownsampleForDisplay_ShouldReduceToMaxPoints()
    {
        double[] values = Enumerable.Range(0, 1000).Select(i => (double)i).ToArray();

        var result = values.DownsampleForDisplay(100);
        result.Length.Should().Be(100);
    }

    [Fact]
    public void DownsampleForDisplay_AlreadySmallEnough_ShouldReturnCopy()
    {
        double[] values = { 1.0, 2.0, 3.0 };
        var result = values.DownsampleForDisplay(10);
        result.Should().Equal(1.0, 2.0, 3.0);
    }

    [Fact]
    public void Lerp_ShouldInterpolateCorrectly()
    {
        var result = 0.5.Lerp(0, 1.0, 10.0, 20.0);
        result.Should().BeApproximately(15.0, 0.01);
    }

    [Fact]
    public void MinersDamage_ShouldComputeCorrectly()
    {
        double[] amplitudes = { 200.0, 150.0 };
        int[] counts = { 1000, 5000 };
        // Using typical S-N parameters
        var d = amplitudes.MinersDamage(counts, 900.0, -0.1);

        d.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MinersDamage_EmptyArrays_ShouldReturnZero()
    {
        double[] amplitudes = Array.Empty<double>();
        int[] counts = Array.Empty<int>();
        amplitudes.MinersDamage(counts, 900.0, -0.1).Should().Be(0);
    }
}
