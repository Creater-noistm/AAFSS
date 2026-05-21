using AAFSS.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Fact]
    public void Mean_ShouldComputeCorrectly()
    {
        double[] values = { 1.0, 2.0, 3.0, 4.0, 5.0 };
        values.Mean().Should().Be(3.0);
    }

    [Fact]
    public void Mean_EmptySequence_ShouldThrow()
    {
        double[] values = Array.Empty<double>();
        var act = () => values.Mean();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StandardDeviation_ShouldComputeCorrectly()
    {
        double[] values = { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 };
        var sd = values.StandardDeviation();
        sd.Should().BeApproximately(2.138, 0.01);
    }

    [Fact]
    public void StandardDeviation_LessThanTwoElements_ShouldThrow()
    {
        double[] values = { 1.0 };
        var act = () => values.StandardDeviation();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rms_ShouldComputeCorrectly()
    {
        double[] values = { 3.0, 4.0 };
        values.Rms().Should().Be(5.0);
    }

    [Fact]
    public void Rms_EmptySequence_ShouldReturnZero()
    {
        double[] values = Array.Empty<double>();
        values.Rms().Should().Be(0);
    }

    [Fact]
    public void Rms_SingleValue_ShouldReturnAbsolute()
    {
        double[] values = { -5.0 };
        values.Rms().Should().Be(5.0);
    }

    [Fact]
    public void Peak_ShouldReturnMaxAbsoluteValue()
    {
        double[] values = { 1.0, -5.0, 3.0, -2.0 };
        values.Peak().Should().Be(5.0);
    }

    [Fact]
    public void Peak_EmptySequence_ShouldThrow()
    {
        double[] values = Array.Empty<double>();
        var act = () => values.Peak();
        act.Should().Throw<InvalidOperationException>();
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
    public void ToDecibels_ShouldConvertCorrectly()
    {
        double[] values = { 1.0, 10.0, 100.0 };
        var db = values.ToDecibels().ToArray();
        db[0].Should().BeApproximately(0, 0.01);
        db[1].Should().BeApproximately(20.0, 0.01);
        db[2].Should().BeApproximately(40.0, 0.01);
    }

    [Fact]
    public void ToDecibels_ZeroValue_ShouldReturnNegativeInfinity()
    {
        double[] values = { 0 };
        values.ToDecibels().Single().Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void FromDecibels_ShouldConvertCorrectly()
    {
        double[] values = { 0, 20, 40 };
        var linear = values.FromDecibels().ToArray();
        linear[0].Should().BeApproximately(1.0, 0.01);
        linear[1].Should().BeApproximately(10.0, 0.01);
        linear[2].Should().BeApproximately(100.0, 0.01);
    }

    [Fact]
    public void MovingAverage_ShouldComputeCorrectly()
    {
        double[] values = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0 };
        var result = values.MovingAverage(3);
        result.Length.Should().Be(9);
        result[4].Should().BeApproximately(5.0, 0.01);
    }

    [Fact]
    public void MovingAverage_WindowSizeZero_ShouldThrow()
    {
        double[] values = { 1.0, 2.0 };
        var act = () => values.MovingAverage(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MovingAverage_EmptyArray_ShouldReturnEmpty()
    {
        double[] values = Array.Empty<double>();
        values.MovingAverage(3).Should().BeEmpty();
    }

    [Fact]
    public void FindPeakIndices_ShouldFindAllPeaks()
    {
        double[] values = { 1.0, 3.0, 1.0, 4.0, 2.0, 5.0, 1.0 };
        var peaks = values.FindPeakIndices();
        peaks.Should().Equal(1, 3, 5);
    }

    [Fact]
    public void FindPeakIndices_TooFewElements_ShouldReturnEmpty()
    {
        double[] values = { 1.0, 2.0 };
        values.FindPeakIndices().Should().BeEmpty();
    }

    [Fact]
    public void FindValleyIndices_ShouldFindAllValleys()
    {
        double[] values = { 5.0, 1.0, 4.0, 0.0, 3.0 };
        var valleys = values.FindValleyIndices();
        valleys.Should().Equal(1, 3);
    }

    [Fact]
    public void CumulativeSum_ShouldComputeCorrectly()
    {
        double[] values = { 1.0, 2.0, 3.0, 4.0 };
        var result = values.CumulativeSum();
        result.Should().Equal(1.0, 3.0, 6.0, 10.0);
    }

    [Fact]
    public void CumulativeSum_Empty_ShouldReturnEmpty()
    {
        double[] values = Array.Empty<double>();
        values.CumulativeSum().Should().BeEmpty();
    }

    [Fact]
    public void AllFinite_AllValid_ShouldReturnTrue()
    {
        double[] values = { 1.0, 2.0, 3.0 };
        values.AllFinite().Should().BeTrue();
    }

    [Fact]
    public void AllFinite_WithNaN_ShouldReturnFalse()
    {
        double[] values = { 1.0, double.NaN, 3.0 };
        values.AllFinite().Should().BeFalse();
    }

    [Fact]
    public void WhereFinite_ShouldFilterOutNonFinite()
    {
        double[] values = { 1.0, double.NaN, 3.0, double.PositiveInfinity, 5.0 };
        var result = values.WhereFinite().ToArray();
        result.Should().Equal(1.0, 3.0, 5.0);
    }
}
