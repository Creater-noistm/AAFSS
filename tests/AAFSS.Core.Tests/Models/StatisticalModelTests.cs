using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class StatisticalModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var model = new StatisticalModel();

        model.Id.Should().NotBe(Guid.Empty);
        model.RainflowResultId.Should().Be(Guid.Empty);
        model.DistributionType.Should().Be(DistributionType.Normal);
        model.ParametersJson.Should().Be("[]");
        model.KsStatistic.Should().Be(0);
        model.KsPValue.Should().Be(0);
        model.AicValue.Should().Be(0);
        model.GoodnessOfFit.Should().Be(0);
        model.FitStatus.Should().Be("Pending");
    }

    [Fact]
    public void Parameters_Setter_ShouldSerializeAndDeserialize()
    {
        var model = new StatisticalModel();
        var parameters = new double[] { 1.5, 0.3 };

        model.Parameters = parameters;

        model.Parameters.Should().Equal(1.5, 0.3);
    }

    [Fact]
    public void GetSummary_ShouldReturnFormattedString()
    {
        var model = new StatisticalModel
        {
            DistributionType = DistributionType.Weibull2P,
            KsStatistic = 0.0421,
            AicValue = -120.5,
            GoodnessOfFit = 0.987
        };

        var summary = model.GetSummary();

        summary.Should().Be("Weibull2P: K-S=0.0421, AIC=-120.50, GoF=0.987");
    }

    [Fact]
    public void GetSummary_WithDefaultValues_ShouldStillReturnString()
    {
        var model = new StatisticalModel();
        var summary = model.GetSummary();

        summary.Should().Contain("Normal").And.Contain("K-S=");
    }
}
