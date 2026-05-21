using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class MissionProfileTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var profile = new MissionProfile();

        profile.Id.Should().NotBe(Guid.Empty);
        profile.ProjectId.Should().Be(Guid.Empty);
        profile.Name.Should().BeEmpty();
        profile.Type.Should().Be(MissionProfileType.Standard);
        profile.ParametersJson.Should().Be("{}");
        profile.TotalWeight.Should().Be(0);
        profile.Conditions.Should().BeEmpty();
        profile.Points.Should().BeEmpty();
        profile.DataSources.Should().BeEmpty();
    }

    [Fact]
    public void Parameters_Getter_ShouldReturnDefault()
    {
        var profile = new MissionProfile();
        var p = profile.Parameters;

        p.Should().NotBeNull();
        p.Altitude.Should().Be(0);
        p.MachNumber.Should().Be(0);
        p.Duration.Should().Be(0);
        p.Weight.Should().Be(0);
    }

    [Fact]
    public void Parameters_Setter_ShouldSerializeToJson()
    {
        var profile = new MissionProfile();
        profile.Parameters = new ProfileParameters
        {
            Altitude = 11000,
            MachNumber = 0.85,
            Duration = 120,
            Weight = 25.0
        };

        var deserialized = profile.Parameters;
        deserialized.Altitude.Should().Be(11000);
        deserialized.MachNumber.Should().Be(0.85);
        deserialized.Duration.Should().Be(120);
        deserialized.Weight.Should().Be(25.0);
    }

    [Fact]
    public void ValidateWeights_WithValidWeights_ShouldReturnTrue()
    {
        var profile = new MissionProfile();
        profile.Conditions.Add(new FlightCondition { Weight = 40 });
        profile.Conditions.Add(new FlightCondition { Weight = 35 });
        profile.Conditions.Add(new FlightCondition { Weight = 25 });

        profile.ValidateWeights().Should().BeTrue();
    }

    [Fact]
    public void ValidateWeights_WithInvalidWeights_ShouldReturnFalse()
    {
        var profile = new MissionProfile();
        profile.Conditions.Add(new FlightCondition { Weight = 50 });
        profile.Conditions.Add(new FlightCondition { Weight = 30 });

        profile.ValidateWeights().Should().BeFalse();
    }

    [Fact]
    public void ValidateWeights_WithNoConditions_ShouldReturnFalse()
    {
        var profile = new MissionProfile();
        profile.ValidateWeights().Should().BeFalse();
    }
}
