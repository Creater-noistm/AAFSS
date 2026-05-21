using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Provides read access to raw time series data stored in HDF5,
/// bridging the Core layer's need for data with the Infrastructure HDF5 implementation.
/// </summary>
public interface ITimeSeriesDataAccess
{
    /// <summary>
    /// Reads a single channel of time series data from HDF5 storage.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="timeSeriesData">Metadata describing the HDF5 dataset.</param>
    /// <param name="channelIndex">Channel index to read.</param>
    /// <param name="startSample">Starting sample index.</param>
    /// <param name="sampleCount">Number of samples (-1 for all).</param>
    /// <returns>1D array of sample values.</returns>
    Task<double[]> ReadChannelAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int channelIndex,
        long startSample = 0,
        long sampleCount = -1);

    /// <summary>
    /// Reads a preview of the time series data (first N samples).
    /// </summary>
    Task<double[,]> ReadPreviewAsync(
        Guid projectId,
        TimeSeriesData timeSeriesData,
        int maxSamples = 1000);
}
