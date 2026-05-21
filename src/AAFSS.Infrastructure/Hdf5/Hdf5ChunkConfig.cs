namespace AAFSS.Infrastructure.Hdf5;

/// <summary>
/// Configuration for HDF5 chunked storage of time series data.
/// Chunking enables efficient partial I/O and compression of large datasets.
/// </summary>
public class Hdf5ChunkConfig
{
    /// <summary>
    /// Default chunk size in bytes (1 MB).
    /// </summary>
    public const long DefaultChunkSizeBytes = 1_048_576L;

    /// <summary>
    /// Default compression level for GZip (4 = medium).
    /// </summary>
    public const int DefaultCompressionLevel = 4;

    /// <summary>
    /// Gets or sets the maximum chunk size in bytes.
    /// Larger chunks improve compression ratio but increase memory usage during I/O.
    /// </summary>
    public long ChunkSizeBytes { get; set; } = DefaultChunkSizeBytes;

    /// <summary>
    /// Gets or sets the GZip compression level (0 = no compression, 9 = maximum).
    /// </summary>
    public int CompressionLevel { get; set; } = DefaultCompressionLevel;

    /// <summary>
    /// Gets or sets whether to use the shuffle filter for improved compression.
    /// </summary>
    public bool EnableShuffle { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to maintain a separate error checksum for each chunk.
    /// </summary>
    public bool EnableChecksum { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of rows per chunk for time series data.
    /// When set to 0, auto-calculates based on ChunkSizeBytes.
    /// </summary>
    public int MaxRowsPerChunk { get; set; } = 0;

    /// <summary>
    /// Creates a configuration optimized for the given sample count and channel count.
    /// </summary>
    /// <param name="totalSamples">Total number of samples per channel.</param>
    /// <param name="channelCount">Number of channels.</param>
    /// <returns>Optimized chunk configuration.</returns>
    public static Hdf5ChunkConfig CreateOptimized(long totalSamples, int channelCount)
    {
        var config = new Hdf5ChunkConfig();

        // Auto-calculate rows per chunk: aim for ~100 chunks per channel
        if (totalSamples > 0 && channelCount > 0)
        {
            var bytesPerSample = sizeof(double); // 8 bytes
            var bytesPerRow = bytesPerSample * channelCount;
            var chunkSizeBytes = DefaultChunkSizeBytes / bytesPerRow;
            var targetChunkRows = Math.Max(1000, (int)chunkSizeBytes);
            config.MaxRowsPerChunk = Math.Min(targetChunkRows, (int)Math.Min(totalSamples, 100_000));
        }

        return config;
    }
}
