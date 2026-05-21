using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class TimeSeriesDataTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var tsd = new TimeSeriesData();

        tsd.Id.Should().NotBe(Guid.Empty);
        tsd.DataSourceId.Should().Be(Guid.Empty);
        tsd.SampleRate.Should().Be(0);
        tsd.ChannelCount.Should().Be(0);
        tsd.Duration.Should().Be(0);
        tsd.SampleCount.Should().Be(0);
        tsd.Hdf5Path.Should().BeEmpty();
        tsd.Quantity.Should().Be("SoundPressure");
    }

    [Fact]
    public void ChannelNames_Setter_ShouldSerializeAndDeserialize()
    {
        var tsd = new TimeSeriesData();
        var names = new string[] { "Ch1_Mic_Front", "Ch2_Mic_Rear", "Ch3_Accel" };

        tsd.ChannelNames = names;

        tsd.ChannelNames.Should().Equal("Ch1_Mic_Front", "Ch2_Mic_Rear", "Ch3_Accel");
    }

    [Fact]
    public void ChannelUnits_Setter_ShouldSerializeAndDeserialize()
    {
        var tsd = new TimeSeriesData();
        var units = new string[] { "Pa", "Pa", "g" };

        tsd.ChannelUnits = units;

        tsd.ChannelUnits.Should().Equal("Pa", "Pa", "g");
    }

    [Fact]
    public void ChannelNames_Getter_WhenEmpty_ShouldReturnEmptyArray()
    {
        var tsd = new TimeSeriesData();
        tsd.ChannelNames.Should().BeEmpty();
    }

    [Fact]
    public void ChannelUnits_Getter_WhenEmpty_ShouldReturnEmptyArray()
    {
        var tsd = new TimeSeriesData();
        tsd.ChannelUnits.Should().BeEmpty();
    }
}
