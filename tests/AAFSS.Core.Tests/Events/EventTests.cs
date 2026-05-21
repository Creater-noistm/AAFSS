using AAFSS.Core.Events;
using AAFSS.Core.Models;
using MediatR;
using Xunit;
using FluentAssertions;

namespace AAFSS.Core.Tests.Events;

public class EventTests
{
    [Fact]
    public void DataImportedEvent_ShouldImplementINotification()
    {
        var evt = new DataImportedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProfileId = Guid.NewGuid(),
            FilePath = "test.csv",
            DataPointCount = 1000,
            SampleRate = 2000,
            ImportedAt = DateTime.UtcNow
        };

        evt.Should().BeAssignableTo<INotification>();
        evt.FilePath.Should().Be("test.csv");
        evt.DataPointCount.Should().Be(1000);
        evt.SampleRate.Should().Be(2000);
    }

    [Fact]
    public void ProcessingCompletedEvent_ShouldImplementINotification()
    {
        var evt = new ProcessingCompletedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProcessingStepId = Guid.NewGuid(),
            OperationType = "Rainflow",
            Success = true,
            DurationMs = 150.5,
            ErrorMessage = null,
            ResultEntityId = Guid.NewGuid()
        };

        evt.Should().BeAssignableTo<INotification>();
        evt.OperationType.Should().Be("Rainflow");
        evt.Success.Should().BeTrue();
        evt.DurationMs.Should().Be(150.5);
    }

    [Fact]
    public void ProcessingCompletedEvent_Failed_ShouldStoreError()
    {
        var evt = new ProcessingCompletedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProcessingStepId = Guid.NewGuid(),
            OperationType = "Filter",
            Success = false,
            ErrorMessage = "Stack overflow",
            DurationMs = 10
        };

        evt.Success.Should().BeFalse();
        evt.ErrorMessage.Should().Be("Stack overflow");
    }

    [Fact]
    public void SpectrumCompiledEvent_ShouldImplementINotification()
    {
        var evt = new SpectrumCompiledEvent
        {
            SpectrumId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            SpectrumName = "着陆-1/3OCT",
            Category = SpectrumCategory.Envelope,
            Method = CompilationMethod.MaxEnvelope,
            SourceCount = 12,
            DamageValue = 0.98,
            Oaspl = 145.5
        };

        evt.Should().BeAssignableTo<INotification>();
        evt.SpectrumName.Should().Be("着陆-1/3OCT");
        evt.Category.Should().Be(SpectrumCategory.Envelope);
        evt.SourceCount.Should().Be(12);
        evt.DamageValue.Should().Be(0.98);
        evt.Oaspl.Should().Be(145.5);
    }

    [Fact]
    public void ValidationCompletedEvent_ShouldImplementINotification()
    {
        var evt = new ValidationCompletedEvent
        {
            SpectrumId = Guid.NewGuid(),
            ValidationReportId = Guid.NewGuid(),
            Level = ValidationLevel.Yellow,
            Status = ValidationStatus.Warning,
            ActualD = 0.92,
            TargetD = 1.0,
            Deviation = 0.08
        };

        evt.Should().BeAssignableTo<INotification>();
        evt.Level.Should().Be(ValidationLevel.Yellow);
        evt.Status.Should().Be(ValidationStatus.Warning);
        evt.ActualD.Should().Be(0.92);
        evt.TargetD.Should().Be(1.0);
        evt.Deviation.Should().Be(0.08);
    }
}
