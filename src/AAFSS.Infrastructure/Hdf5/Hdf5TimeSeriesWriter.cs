using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Hdf5;

/// <summary>
/// Writes time series data to the data store.
/// Supports chunked writing for memory-efficient import of large datasets,
/// with automatic dataset creation.
/// </summary>
public class Hdf5TimeSeriesWriter
{
    private readonly Hdf5DataStore _store;
    private readonly Hdf5ChunkConfig _chunkConfig;

    /// <summary>
    /// Initializes the writer with the data store.
    /// </summary>
    public Hdf5TimeSeriesWriter(Hdf5DataStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _chunkConfig = new Hdf5ChunkConfig();
    }

    /// <summary>
    /// Writes a complete time series dataset in chunks.
    /// Creates the dataset and writes data progressively to manage memory.
    /// </summary>
    public async Task<TimeSeriesData> WriteTimeSeriesAsync(
        Guid projectId,
        string datasetPath,
        Func<long, int, double[,]> reader,
        long totalSamples,
        int channelCount,
        string[] channelNames,
        string[] channelUnits,
        string quantity = "SoundPressure",
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        if (totalSamples <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalSamples), "Total samples must be positive.");
        if (channelCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(channelCount), "Channel count must be positive.");
        if (string.IsNullOrWhiteSpace(datasetPath))
            throw new ArgumentException("Dataset path is required.", nameof(datasetPath));

        // Normalize path
        datasetPath = datasetPath.StartsWith('/') ? datasetPath : "/" + datasetPath;
        if (datasetPath.EndsWith('/'))
            datasetPath = datasetPath.TrimEnd('/');

        using var handle = _store.OpenProjectFile(projectId);

        // Determine chunk size
        var chunkRows = _chunkConfig.MaxRowsPerChunk > 0
            ? _chunkConfig.MaxRowsPerChunk
            : Math.Max(1000, (int)(_chunkConfig.ChunkSizeBytes / (sizeof(double) * channelCount)));
        chunkRows = (int)Math.Min(chunkRows, totalSamples);

        // Create the dataset
        _store.CreateTimeSeriesDataset(handle, datasetPath, totalSamples, channelCount);

        // Write data in chunks
        var samplesWritten = 0L;
        for (long offset = 0; offset < totalSamples && !ct.IsCancellationRequested; offset += chunkRows)
        {
            var count = (int)Math.Min(chunkRows, totalSamples - offset);
            var chunk = reader(offset, count);

            if (chunk.GetLength(0) != count || chunk.GetLength(1) != channelCount)
                throw new InvalidOperationException(
                    $"Reader returned chunk of size [{chunk.GetLength(0)}, {chunk.GetLength(1)}], " +
                    $"expected [{count}, {channelCount}].");

            _store.WriteDataset(handle, datasetPath, chunk, offset);
            samplesWritten += count;
            onProgress?.Invoke(samplesWritten, totalSamples);
        }

        ct.ThrowIfCancellationRequested();

        // Calculate duration (need sample rate — use 1.0 as placeholder, caller should override)
        var sampleRate = 1.0;
        var duration = totalSamples / sampleRate;

        return new TimeSeriesData
        {
            Id = Guid.NewGuid(),
            Hdf5Path = datasetPath,
            SampleRate = sampleRate,
            ChannelCount = channelCount,
            SampleCount = totalSamples,
            Duration = duration,
            Quantity = quantity,
            ChannelNames = channelNames,
            ChannelUnits = channelUnits
        };
    }

    /// <summary>
    /// Writes a full 2D array in one operation.
    /// Suitable for datasets that fit comfortably in memory.
    /// </summary>
    public async Task<TimeSeriesData> WriteFullArrayAsync(
        Guid projectId,
        string datasetPath,
        double[,] data,
        double sampleRate,
        string[] channelNames,
        string[] channelUnits,
        string quantity = "SoundPressure")
    {
        var totalSamples = data.GetLength(0);
        var channelCount = data.GetLength(1);

        using var handle = _store.OpenProjectFile(projectId);

        // Normalize path
        datasetPath = datasetPath.StartsWith('/') ? datasetPath : "/" + datasetPath;
        if (datasetPath.EndsWith('/'))
            datasetPath = datasetPath.TrimEnd('/');

        // Create and write
        _store.CreateTimeSeriesDataset(handle, datasetPath, totalSamples, channelCount);
        _store.WriteDataset(handle, datasetPath, data, 0);

        return new TimeSeriesData
        {
            Id = Guid.NewGuid(),
            Hdf5Path = datasetPath,
            SampleRate = sampleRate,
            ChannelCount = channelCount,
            SampleCount = totalSamples,
            Duration = totalSamples / sampleRate,
            Quantity = quantity,
            ChannelNames = channelNames,
            ChannelUnits = channelUnits
        };
    }

    /// <summary>
    /// Appends data to an existing dataset by extending it.
    /// </summary>
    public async Task<long> AppendDataAsync(
        Guid projectId,
        string datasetPath,
        double[,] newData)
    {
        using var handle = _store.OpenProjectFile(projectId);

        var (currentRows, currentCols) = _store.GetDatasetDimensions(handle, datasetPath);
        var newRows = newData.GetLength(0);
        var newCols = newData.GetLength(1);

        if (newCols != currentCols)
            throw new InvalidOperationException(
                $"Column count mismatch: existing has {currentCols}, new data has {newCols}.");

        // Write appended data
        _store.WriteDataset(handle, datasetPath, newData, currentRows);

        return currentRows + newRows;
    }
}
