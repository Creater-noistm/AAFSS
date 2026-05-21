using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class ProcessingStepTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var step = new ProcessingStep();

        step.Id.Should().NotBe(Guid.Empty);
        step.DataSourceId.Should().Be(Guid.Empty);
        step.StepOrder.Should().Be(0);
        step.OperationType.Should().BeEmpty();
        step.OperationParams.Should().Be("{}");
        step.InputRef.Should().BeEmpty();
        step.OutputRef.Should().BeEmpty();
        step.Status.Should().Be(ProcessingStatus.Pending);
        step.CompletedAt.Should().BeNull();
        step.ErrorMessage.Should().BeNull();
        step.DurationMs.Should().Be(0);
    }

    [Fact]
    public void MarkCompleted_ShouldSetStatusAndTimestamps()
    {
        var step = new ProcessingStep();

        step.MarkCompleted();

        step.Status.Should().Be(ProcessingStatus.Completed);
        step.CompletedAt.Should().NotBeNull();
        step.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void MarkFailed_ShouldSetErrorAndStatus()
    {
        var step = new ProcessingStep();

        step.MarkFailed("Out of memory");

        step.Status.Should().Be(ProcessingStatus.Failed);
        step.ErrorMessage.Should().Be("Out of memory");
        step.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkRunning_ShouldSetStatusAndStartTime()
    {
        var step = new ProcessingStep();
        var before = DateTime.UtcNow;

        step.MarkRunning();

        step.Status.Should().Be(ProcessingStatus.Running);
        step.StartedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void DurationMs_ShouldAccumulateBetweenStartAndComplete()
    {
        var step = new ProcessingStep();
        step.MarkRunning();
        System.Threading.Thread.Sleep(50);
        step.MarkCompleted();

        step.DurationMs.Should().BeGreaterThan(0);
    }
}
