using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class ValidationReportTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var report = new ValidationReport();

        report.Id.Should().NotBe(Guid.Empty);
        report.SpectrumId.Should().Be(Guid.Empty);
        report.TargetD.Should().Be(1.0);
        report.ActualD.Should().Be(0);
        report.Deviation.Should().Be(0);
        report.Level.Should().Be(ValidationLevel.NotValidated);
    }

    [Fact]
    public void Warnings_Setter_ShouldSerializeAndDeserialize()
    {
        var report = new ValidationReport();
        var warnings = new string[] { "High deviation detected", "Check S-N curve data" };

        report.Warnings = warnings;

        report.Warnings.Should().Equal("High deviation detected", "Check S-N curve data");
    }

    [Fact]
    public void GetStatusIndicator_Green_ShouldReturnCheckMark()
    {
        var report = new ValidationReport { Level = ValidationLevel.Green };
        report.GetStatusIndicator().Should().Be("\u2713");
    }

    [Fact]
    public void GetStatusIndicator_Yellow_ShouldReturnWarning()
    {
        var report = new ValidationReport { Level = ValidationLevel.Yellow };
        report.GetStatusIndicator().Should().Be("\u26a0");
    }

    [Fact]
    public void GetStatusIndicator_Red_ShouldReturnX()
    {
        var report = new ValidationReport { Level = ValidationLevel.Red };
        report.GetStatusIndicator().Should().Be("\u2717");
    }

    [Fact]
    public void GetStatusIndicator_NotValidated_ShouldReturnQuestionMark()
    {
        var report = new ValidationReport { Level = ValidationLevel.NotValidated };
        report.GetStatusIndicator().Should().Be("?");
    }
}
