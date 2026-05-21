using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Hdf5;

/// <summary>
/// Reads time series data from the data store.
/// Provides streaming and chunked access to large time series datasets,
/// with support for channel selection, downsampling, and metadata extraction.
/// </summary>
public class Hdf5TimeSeriesReader
{
    private readonly Hdf5DataStore _store;

    /// <summary>
    /// Initializes the reader with the data store.
    /// </summary>
    public Hdf5TimeSeriesReader(Hdf5DataStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Reads the full time series data as a 2D array [samples, channels].
    /// Use only for datasets that fit in memory. For large datasets, use ReadChunkedAsync.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata referencing the store path.</param>
    /// <returns>2D array with all samples and channels.</returns>
    public async Task<double[,]> ReadFullAsync(Guid projectId, TimeSeriesData timeSeriesData)
    {
        if (timeSeriesData.SampleCount > 10_000_000)
        {
            // Warn but allow — caller should use chunked reading for large datasets
            System.Diagnostics.Debug.WriteLine(
                $"Warning: Reading {timeSeriesData.SampleCount:N0} samples into memory. " +
                $"Consider using ReadChunkedAsync for large datasets.");
        }

        using var handle = _store.OpenProjectFile(projectId);
        return await Task.Run(() => _store.ReadDataset(handle, timeSeriesData.Hdf5Path));
    }

    /// <summary>
    /// Reads a single channel from the time series data.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata.</param>
    /// <param name="channelIndex">Zero-based channel index.</param>
    /// <param name="startSample">Starting sample index.</param>
    /// <param name="sampleCount">Number of samples (-1 = all from start).</param>
    /// <returns>1D array of channel data.</returns>
    public async Task<double[]> ReadChannelAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int channelIndex = 0,
        long startSample = 0,
        long sampleCount = -1)
    {
        using var handle = _store.OpenProjectFile(projectId);
        return await Task.Run(() =>
            _store.ReadChannel(handle, timeSeriesData.Hdf5Path, channelIndex, startSample, sampleCount));
    }

    /// <summary>
    /// Reads time series data in chunks, invoking a callback for each chunk.
    /// Suitable for processing large datasets that exceed available memory.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata.</param>
    /// <param name="chunkSize">Number of samples per chunk.</param>
    /// <param name="onChunk">Callback receiving (chunkIndex, data[,], startSample).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ReadChunkedAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int chunkSize,
        Func<int, double[,], long, Task> onChunk,
        CancellationToken ct = default)
    {
        using var handle = _store.OpenProjectFile(projectId);
        var totalSamples = timeSeriesData.SampleCount;
        var chunkIndex = 0;

        for (long offset = 0; offset < totalSamples && !ct.IsCancellationRequested; offset += chunkSize)
        {
            var count = Math.Min(chunkSize, totalSamples - offset);
            var block = await Task.Run(() => _store.ReadDataset(handle, timeSeriesData.Hdf5Path, offset, count));
            await onChunk(chunkIndex++, block, offset);
        }
    }

    /// <summary>
    /// Reads a downsampled version of the time series data.
    /// Reduces data by taking every Nth sample.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata.</param>
    /// <param name="factor">Decimation factor (e.g., 10 = keep every 10th sample).</param>
    /// <param name="channelIndex">Channel to read (-1 = all channels).</param>
    /// <returns>Downsampled data as 2D array.</returns>
    public async Task<double[,]> ReadDownsampledAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int factor,
        int channelIndex = -1)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Decimation factor must be positive.");

        using var handle = _store.OpenProjectFile(projectId);
        return await Task.Run(() =>
        {
            var totalSamples = timeSeriesData.SampleCount;
            var channels = channelIndex >= 0 ? 1 : timeSeriesData.ChannelCount;
            var downsampledCount = (totalSamples + factor - 1) / factor;
            var result = new double[downsampledCount, channels];

            for (long i = 0; i < downsampledCount; i++)
            {
                var srcRow = i * factor;
                if (channelIndex >= 0)
                {
                    var channelData = _store.ReadChannel(handle, timeSeriesData.Hdf5Path, channelIndex, srcRow, 1);
                    result[i, 0] = channelData.Length > 0 ? channelData[0] : 0.0;
                }
                else
                {
                    var row = _store.ReadDataset(handle, timeSeriesData.Hdf5Path, srcRow, 1);
                    for (int c = 0; c < channels; c++)
                        result[i, c] = row[0, c];
                }
            }

            return result;
        });
    }

    /// <summary>
    /// Reads the first N samples as a preview (lightweight preview for display).
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata.</param>
    /// <param name="maxSamples">Maximum samples to read for preview (default 1000).</param>
    /// <returns>Preview data as 2D array.</returns>
    public async Task<double[,]> ReadPreviewAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int maxSamples = 1000)
    {
        using var handle = _store.OpenProjectFile(projectId);
        var count = Math.Min(maxSamples, timeSeriesData.SampleCount);
        return await Task.Run(() => _store.ReadDataset(handle, timeSeriesData.Hdf5Path, 0, count));
    }

    /// <summary>
    /// Validates that the dataset dimensions match the metadata.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Time series metadata to validate against.</param>
    /// <returns>True if the dataset matches metadata.</returns>
    public async Task<bool> ValidateMetadataAsync(Guid projectId, TimeSeriesData timeSeriesData)
    {
        using var handle = _store.OpenProjectFile(projectId);

        if (!_store.DatasetExists(handle, timeSeriesData.Hdf5Path))
            return false;

        var (rows, cols) = await Task.Run(() => _store.GetDatasetDimensions(handle, timeSeriesData.Hdf5Path));

        return rows == timeSeriesData.SampleCount && cols == timeSeriesData.ChannelCount;
    }
}
