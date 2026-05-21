using AAFSS.Core.Events;
using AAFSS.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AAFSS.Core.Tests.Events;

public class EventHandlerTests
{
    [Fact]
    public async Task DataImportedEventHandler_ShouldLogInformation()
    {
        var logger = new Mock<ILogger<DataImportedEventHandler>>();
        var handler = new DataImportedEventHandler(logger.Object);
        var evt = new DataImportedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProfileId = Guid.NewGuid(),
            FilePath = "data.csv",
            DataPointCount = 5000,
            SampleRate = 1000
        };

        await handler.Handle(evt, CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessingCompletedEventHandler_Completed_ShouldLogInformation()
    {
        var logger = new Mock<ILogger<ProcessingCompletedEventHandler>>();
        var handler = new ProcessingCompletedEventHandler(logger.Object);
        var evt = new ProcessingCompletedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProcessingStepId = Guid.NewGuid(),
            OperationType = "PSD",
            Success = true,
            DurationMs = 100
        };

        await handler.Handle(evt, CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessingCompletedEventHandler_Failed_ShouldLogWarning()
    {
        var logger = new Mock<ILogger<ProcessingCompletedEventHandler>>();
        var handler = new ProcessingCompletedEventHandler(logger.Object);
        var evt = new ProcessingCompletedEvent
        {
            DataSourceId = Guid.NewGuid(),
            ProcessingStepId = Guid.NewGuid(),
            OperationType = "Import",
            Success = false,
            ErrorMessage = "File not found"
        };

        await handler.Handle(evt, CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
