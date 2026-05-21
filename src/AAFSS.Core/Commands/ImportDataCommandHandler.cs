using AAFSS.Core.Events;
using AAFSS.Core.Models;
using AAFSS.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Commands;

/// <summary>
/// Handles <see cref="ImportDataCommand"/> by invoking the data import service
/// and publishing a domain event upon successful import.
/// </summary>
public class ImportDataCommandHandler : IRequestHandler<ImportDataCommand, DataSource>
{
    private readonly IDataImportService _importService;
    private readonly IMediator _mediator;
    private readonly ILogger<ImportDataCommandHandler> _logger;

    public ImportDataCommandHandler(
        IDataImportService importService,
        IMediator mediator,
        ILogger<ImportDataCommandHandler> logger)
    {
        _importService = importService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<DataSource> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Importing data: ProjectId={ProjectId}, ProfileId={ProfileId}, File={FilePath}",
            request.ProjectId, request.ProfileId, request.FilePath);

        var dataSource = await _importService.ImportAsync(
            request.ProjectId,
            request.ProfileId,
            request.FilePath,
            request.MeasurementPointId,
            request.Progress,
            cancellationToken);

        var validationResult = dataSource.ValidationResult;

        await _mediator.Publish(new DataImportedEvent
        {
            ProjectId = request.ProjectId,
            ProfileId = request.ProfileId,
            DataSourceId = dataSource.Id,
            FilePath = request.FilePath,
            Format = dataSource.Format,
            DataPointCount = validationResult.TotalDataPoints,
            SampleRate = validationResult.DetectedSampleRate,
            ChannelCount = validationResult.DetectedChannelCount,
            ImportedAt = dataSource.ImportedAt
        }, cancellationToken);

        _logger.LogInformation("Import completed: DataSourceId={DataSourceId}, Samples={Samples}, Channels={Channels}",
            dataSource.Id, validationResult.TotalDataPoints, validationResult.DetectedChannelCount);

        return dataSource;
    }
}
