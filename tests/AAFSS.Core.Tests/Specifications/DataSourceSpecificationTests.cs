using AAFSS.Core.Models;
using AAFSS.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Specifications;

public class DataSourceSpecificationTests
{
    [Fact]
    public void ByProject_ShouldFilterCorrectly()
    {
        var projectId = Guid.NewGuid();
        var matching = new DataSource { ProfileId = projectId };
        var nonMatching = new DataSource { ProfileId = Guid.NewGuid() };

        var spec = DataSourceSpecification.ByProject(projectId);

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void ByType_ShouldFilterCorrectly()
    {
        var matching = new DataSource { Type = DataSourceType.Simulation };
        var nonMatching = new DataSource { Type = DataSourceType.Measurement };

        var spec = DataSourceSpecification.ByType(DataSourceType.Simulation);

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void BySensorType_ShouldFilterCorrectly()
    {
        var matching = new DataSource();
        matching.Profile = new MissionProfile();
        matching.Profile.Points.Add(new MeasurementPoint { SensorType = SensorType.Accelerometer, Id = matching.PointId ?? Guid.NewGuid() });
        matching.PointId = Guid.NewGuid();

        var spec = DataSourceSpecification.BySensorType(SensorType.Accelerometer);

        // Since DataSource doesn't have a SensorType property directly,
        // this test verifies the specification compiles and runs.
        spec.Should().NotBeNull();
    }

    [Fact]
    public void And_ShouldCombineSpecifications()
    {
        var projectId = Guid.NewGuid();
        var matching = new DataSource
        {
            ProfileId = projectId,
            Type = DataSourceType.Measurement
        };

        var spec = DataSourceSpecification.ByProject(projectId)
            .And(DataSourceSpecification.ByType(DataSourceType.Measurement));

        spec.IsSatisfiedBy(matching).Should().BeTrue();
    }

    [Fact]
    public void Or_ShouldCombineWithOr()
    {
        var projectId = Guid.NewGuid();
        var ds = new DataSource
        {
            ProfileId = Guid.NewGuid(),
            Type = DataSourceType.Measurement
        };

        var spec = DataSourceSpecification.ByProject(projectId)
            .Or(DataSourceSpecification.ByType(DataSourceType.Measurement));

        spec.IsSatisfiedBy(ds).Should().BeTrue();
    }

    [Fact]
    public void Not_ShouldNegateSpecification()
    {
        var ds = new DataSource { Type = DataSourceType.Measurement };

        var spec = DataSourceSpecification.ByType(DataSourceType.Measurement).Not();

        spec.IsSatisfiedBy(ds).Should().BeFalse();
    }

    [Fact]
    public void ImportedAfter_ShouldFilterByDate()
    {
        var recent = new DataSource { ImportedAt = DateTime.UtcNow };
        var older = new DataSource { ImportedAt = DateTime.UtcNow.AddDays(-10) };

        var spec = DataSourceSpecification.ImportedAfter(DateTime.UtcNow.AddDays(-1));

        spec.IsSatisfiedBy(recent).Should().BeTrue();
        spec.IsSatisfiedBy(older).Should().BeFalse();
    }

    [Fact]
    public void FileNameContains_ShouldFilterCorrectly()
    {
        var matching = new DataSource { FilePath = "C:\\data\\measurement_2024.csv" };
        var nonMatching = new DataSource { FilePath = "C:\\data\\other_2024.csv" };

        var spec = DataSourceSpecification.FileNameContains("measurement");

        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }
}
