using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class ValueObjectsTests
{
    [Fact]
    public void ProfileParameters_ShouldHaveCorrectDefaults()
    {
        var pp = new ProfileParameters();

        pp.Altitude.Should().Be(0);
        pp.MachNumber.Should().Be(0);
        pp.Duration.Should().Be(0);
        pp.Weight.Should().Be(0);
        pp.DynamicPressure.Should().Be(0);
        pp.AmbientTemperature.Should().Be(0);
        pp.GrossWeightFraction.Should().Be(1.0);
        pp.CustomParameters.Should().BeEmpty();
    }

    [Fact]
    public void DataValidationResult_ShouldHaveCorrectDefaults()
    {
        var dvr = new DataValidationResult();

        dvr.IsValid.Should().BeFalse();
        dvr.Messages.Should().BeEmpty();
        dvr.SampleRateConsistent.Should().BeFalse();
        dvr.ChannelsComplete.Should().BeFalse();
        dvr.OutlierCount.Should().Be(0);
        dvr.DetectedSampleRate.Should().Be(0);
        dvr.DetectedChannelCount.Should().Be(0);
        dvr.TotalDataPoints.Should().Be(0);
        dvr.Duration.Should().Be(0);
    }

    [Fact]
    public void ProcessingResult_ShouldHaveCorrectDefaults()
    {
        var pr = new ProcessingResult();

        pr.Success.Should().BeFalse();
        pr.ProcessingStepId.Should().BeNull();
        pr.OutputRef.Should().BeNull();
        pr.ErrorMessage.Should().BeNull();
        pr.DurationMs.Should().Be(0);
        pr.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void DataPreview_ShouldHaveCorrectDefaults()
    {
        var dp = new DataPreview();

        dp.Headers.Should().BeEmpty();
        dp.Rows.Should().BeEmpty();
        dp.TotalRowCount.Should().Be(0);
        dp.ColumnCount.Should().Be(0);
        dp.DetectedFormat.Should().BeEmpty();
    }

    [Fact]
    public void FrequencyRange_BinCount_ShouldCalculateCorrectly()
    {
        var range = new FrequencyRange { MinHz = 0, MaxHz = 1000, ResolutionHz = 10 };

        range.BinCount.Should().Be(101); // (1000-0)/10 + 1 = 101
    }

    [Fact]
    public void SnCurve_ShouldHaveCorrectDefaults()
    {
        var sn = new SnCurve();

        sn.MaterialName.Should().BeEmpty();
        sn.FatigueStrengthCoefficient.Should().Be(0);
        sn.FatigueStrengthExponent.Should().Be(0);
        sn.FatigueDuctilityCoefficient.Should().Be(0);
        sn.FatigueDuctilityExponent.Should().Be(0);
        sn.EnduranceLimit.Should().Be(0);
        sn.ElasticModulus.Should().Be(0);
        sn.Kt.Should().Be(1.0);
    }

    [Fact]
    public void ProjectTreeNode_ShouldHaveCorrectDefaults()
    {
        var node = new ProjectTreeNode();

        node.Name.Should().BeEmpty();
        node.NodeType.Should().BeEmpty();
        node.EntityId.Should().BeNull();
        node.Status.Should().Be(ProcessingStatus.Pending);
        node.Children.Should().BeEmpty();
    }
}
