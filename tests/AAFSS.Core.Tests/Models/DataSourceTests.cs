using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class DataSourceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var ds = new DataSource();

        ds.Id.Should().NotBe(Guid.Empty);
        ds.ProfileId.Should().Be(Guid.Empty);
        ds.PointId.Should().BeNull();
        ds.Type.Should().Be(DataSourceType.Measurement);
        ds.Format.Should().BeEmpty();
        ds.FilePath.Should().BeEmpty();
        ds.Metadata.Should().Be("{}");
        ds.ValidationResultJson.Should().Be("{}");
        ds.ProcessingSteps.Should().BeEmpty();
        ds.TimeSeriesData.Should().BeNull();
        ds.SpectrumResults.Should().BeEmpty();
        ds.RainflowResults.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Getter_ShouldDeserializeDefault()
    {
        var ds = new DataSource();
        var result = ds.ValidationResult;

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidationResult_Setter_ShouldSerializeToJson()
    {
        var ds = new DataSource();
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 1000.0,
            DetectedChannelCount = 4,
            TotalDataPoints = 10000,
            Duration = 10.0,
            SampleRateConsistent = true,
            ChannelsComplete = true,
            OutlierCount = 5,
            Messages = new System.Collections.Generic.List<string> { "OK" }
        };

        ds.ValidationResult = vr;
        var deserialized = ds.ValidationResult;

        deserialized.IsValid.Should().BeTrue();
        deserialized.DetectedSampleRate.Should().Be(1000.0);
        deserialized.DetectedChannelCount.Should().Be(4);
        deserialized.TotalDataPoints.Should().Be(10000);
        deserialized.Duration.Should().Be(10.0);
        deserialized.SampleRateConsistent.Should().BeTrue();
        deserialized.ChannelsComplete.Should().BeTrue();
        deserialized.OutlierCount.Should().Be(5);
        deserialized.Messages.Should().Contain("OK");
    }

    [Fact]
    public void AddProcessingStep_ShouldSetMetadataAndAddToList()
    {
        var ds = new DataSource();
        var step = new ProcessingStep { OperationType = "Import" };

        ds.AddProcessingStep(step);

        step.DataSourceId.Should().Be(ds.Id);
        step.StepOrder.Should().Be(1);
        ds.ProcessingSteps.Should().ContainSingle();
    }

    [Fact]
    public void AddMultipleProcessingSteps_ShouldHaveCorrectOrder()
    {
        var ds = new DataSource();

        ds.AddProcessingStep(new ProcessingStep { OperationType = "Import" });
        ds.AddProcessingStep(new ProcessingStep { OperationType = "Filter" });
        ds.AddProcessingStep(new ProcessingStep { OperationType = "Rainflow" });

        ds.ProcessingSteps.Should().HaveCount(3);
        ds.ProcessingSteps[0].StepOrder.Should().Be(1);
        ds.ProcessingSteps[1].StepOrder.Should().Be(2);
        ds.ProcessingSteps[2].StepOrder.Should().Be(3);
    }

    [Fact]
    public void AddProcessingStep_WithNull_ShouldThrow()
    {
        var ds = new DataSource();
        var act = () => ds.AddProcessingStep(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
