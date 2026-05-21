using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class RainflowResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var result = new RainflowResult();

        result.Id.Should().NotBe(Guid.Empty);
        result.DataSourceId.Should().Be(Guid.Empty);
        result.TotalCycles.Should().Be(0);
        result.MaxAmplitude.Should().Be(0);
        result.MinMean.Should().Be(0);
        result.MaxMean.Should().Be(0);
        result.BinCount.Should().Be(64);
        result.StatisticalModels.Should().BeEmpty();
    }

    [Fact]
    public void CycleCounts_Getter_ShouldReturnEmptyArray()
    {
        var result = new RainflowResult();
        result.CycleCounts.Should().BeEmpty();
    }

    [Fact]
    public void CycleCounts_Setter_ShouldSerializeAndDeserialize()
    {
        var result = new RainflowResult();
        var counts = new double[] { 10, 20, 30 };

        result.CycleCounts = counts;

        result.CycleCounts.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void FromToMatrix_Setter_ShouldSerializeRowsThenDeserialize()
    {
        var result = new RainflowResult { BinCount = 3 };
        var matrix = new double[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

        result.FromToMatrix = matrix;

        var deserialized = result.FromToMatrix;
        deserialized[0, 0].Should().Be(1);
        deserialized[0, 1].Should().Be(2);
        deserialized[1, 2].Should().Be(6);
        deserialized[2, 2].Should().Be(9);
    }

    [Fact]
    public void MeanAmplitudeMatrix_Setter_ShouldSerializeThenDeserialize()
    {
        var result = new RainflowResult { BinCount = 2 };
        var matrix = new double[2, 2] { { 0.1, 0.2 }, { 0.3, 0.4 } };

        result.MeanAmplitudeMatrix = matrix;

        var deserialized = result.MeanAmplitudeMatrix;
        deserialized[0, 0].Should().Be(0.1);
        deserialized[1, 1].Should().Be(0.4);
    }
}
