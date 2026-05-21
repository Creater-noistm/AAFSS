using AAFSS.Core.Models;
using AAFSS.Core.Services;
using AAFSS.Infrastructure.Hdf5;

namespace AAFSS.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of ITimeSeriesDataAccess using HDF5 storage.
/// </summary>
public class Hdf5TimeSeriesDataAccess : ITimeSeriesDataAccess
{
    private readonly Hdf5TimeSeriesReader _reader;

    public Hdf5TimeSeriesDataAccess(Hdf5DataStore store)
    {
        _reader = new Hdf5TimeSeriesReader(store);
    }

    /// <inheritdoc/>
    public async Task<double[]> ReadChannelAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int channelIndex,
        long startSample = 0,
        long sampleCount = -1)
    {
        return await _reader.ReadChannelAsync(projectId, timeSeriesData, channelIndex, startSample, sampleCount);
    }

    /// <inheritdoc/>
    public async Task<double[,]> ReadPreviewAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int maxSamples = 1000)
    {
        return await _reader.ReadPreviewAsync(projectId, timeSeriesData, maxSamples);
    }
}
