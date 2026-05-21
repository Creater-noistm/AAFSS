using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Hdf5;
using AAFSS.Infrastructure.Import;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Implementation of IDataImportService.
/// Coordinates data import from files into the HDF5 store and database.
/// </summary>
public class DataImportService : IDataImportService
{
    private readonly IUnitOfWork _uow;
    private readonly DataImportFactory _importFactory;
    private readonly DataValidator _validator;
    private readonly Hdf5TimeSeriesWriter _hdf5Writer;

    public DataImportService(
        IUnitOfWork uow,
        DataImportFactory importFactory,
        DataValidator validator,
        Hdf5TimeSeriesWriter hdf5Writer)
    {
        _uow = uow;
        _importFactory = importFactory;
        _validator = validator;
        _hdf5Writer = hdf5Writer;
    }

    public async Task<DataPreview> GetPreviewAsync(string filePath, int maxPreviewRows = 100, CancellationToken ct = default)
    {
        return await _importFactory.GetPreviewAsync(filePath, maxPreviewRows);
    }

    public async Task<DataValidationResult> ValidateAsync(string filePath, CancellationToken ct = default)
    {
        var preview = await _importFactory.GetPreviewAsync(filePath);
        return _validator.ValidatePreview(preview);
    }

    public async Task<DataSource> ImportAsync(
        Guid projectId,
        Guid profileId,
        string filePath,
        Guid? measurementPointId = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var profile = await _uow.MissionProfiles.GetByIdAsync(profileId, ct)
            ?? throw new InvalidOperationException($"Profile {profileId} not found.");

        // Read data from file
        var (data, channelNames, channelUnits, sampleRate) = await _importFactory.ReadFullAsync(
            filePath,
            (read, total) => progress?.Report((double)read / total * 100),
            ct);

        // Validate full data
        var validationResult = _validator.ValidateFullData(data, sampleRate, channelNames);

        // Write to HDF5
        var datasetPath = $"/data/{Guid.NewGuid():N}";
        var timeSeriesData = await _hdf5Writer.WriteFullArrayAsync(
            projectId,
            datasetPath,
            data,
            sampleRate,
            channelNames,
            channelUnits,
            "SoundPressure");

        // Create DataSource entity
        var dataSource = new DataSource
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PointId = measurementPointId,
            Format = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
            FilePath = filePath,
            Type = DataSourceType.Measurement,
            ImportedAt = DateTime.UtcNow,
            TimeSeriesData = timeSeriesData,
            ValidationResult = validationResult
        };

        timeSeriesData.DataSourceId = dataSource.Id;

        await _uow.DataSources.AddAsync(dataSource, ct);
        await _uow.SaveChangesAsync(ct);

        return dataSource;
    }

    public string[] GetSupportedFormats()
    {
        return DataImportFactory.AllSupportedExtensions;
    }
}
