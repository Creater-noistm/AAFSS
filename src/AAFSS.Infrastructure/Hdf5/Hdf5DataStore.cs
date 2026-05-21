using System.Text.Json;
using AAFSS.Infrastructure.Configuration;

namespace AAFSS.Infrastructure.Hdf5;

/// <summary>
/// Manages the data store for time series data persistence.
/// Uses a directory-based binary storage format with JSON metadata.
/// Each project gets a subdirectory containing .bin data files and a metadata.json index.
/// </summary>
public class Hdf5DataStore : IDisposable
{
    private readonly AppConfiguration _configuration;
    private readonly Hdf5ChunkConfig _chunkConfig;
    private readonly string _dataDirectory;
    private readonly Dictionary<Guid, Hdf5FileHandle> _openFiles = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes the data store with application configuration.
    /// </summary>
    public Hdf5DataStore(AppConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _chunkConfig = new Hdf5ChunkConfig
        {
            ChunkSizeBytes = configuration.Hdf5ChunkSize > 0 ? configuration.Hdf5ChunkSize : Hdf5ChunkConfig.DefaultChunkSizeBytes,
            CompressionLevel = configuration.Hdf5CompressionLevel >= 0 ? configuration.Hdf5CompressionLevel : Hdf5ChunkConfig.DefaultCompressionLevel
        };

        _dataDirectory = Path.Combine(_configuration.LogDirectory, "Hdf5Data");
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// Gets the root directory where data files are stored.
    /// </summary>
    public string DataDirectory => _dataDirectory;

    /// <summary>
    /// Creates a new project data directory and returns the directory path.
    /// </summary>
    public string CreateProjectFile(Guid projectId)
    {
        var projectDir = GetProjectDirPath(projectId);
        if (Directory.Exists(projectDir))
        {
            throw new IOException($"Data directory already exists for project {projectId}: {projectDir}");
        }

        Directory.CreateDirectory(projectDir);
        // Initialize empty metadata
        var metadata = new Hdf5Metadata();
        SaveMetadata(projectDir, metadata);

        return projectDir;
    }

    /// <summary>
    /// Opens the project data directory, creating it if it does not exist.
    /// </summary>
    public Hdf5FileHandle OpenProjectFile(Guid projectId)
    {
        var projectDir = GetProjectDirPath(projectId);

        if (!Directory.Exists(projectDir))
        {
            CreateProjectFile(projectId);
        }

        return new Hdf5FileHandle(projectDir);
    }

    /// <summary>
    /// Gets the directory path for a project's data (does NOT create or open it).
    /// </summary>
    public string GetProjectFilePath(Guid projectId)
    {
        return GetProjectDirPath(projectId);
    }

    /// <summary>
    /// Creates metadata for a time series dataset.
    /// </summary>
    public long CreateTimeSeriesDataset(Hdf5FileHandle handle, string datasetPath, long totalSamples, int channelCount = 1)
    {
        var metadata = LoadMetadata(handle.DirectoryPath);
        var normalizedPath = NormalizeDatasetPath(datasetPath);

        metadata.Datasets[normalizedPath] = new Hdf5DatasetInfo
        {
            TotalSamples = totalSamples,
            ChannelCount = channelCount
        };

        SaveMetadata(handle.DirectoryPath, metadata);

        // Pre-allocate the binary data file with zeros
        var binPath = GetBinPath(handle.DirectoryPath, normalizedPath);
        var totalElements = totalSamples * channelCount;
        using var stream = new FileStream(binPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        for (long i = 0; i < totalElements; i++)
        {
            writer.Write(0.0);
        }

        return 0L;
    }

    /// <summary>
    /// Writes double-precision data to a dataset.
    /// </summary>
    public void WriteDataset(Hdf5FileHandle handle, string datasetPath, double[,] data, long startRow = 0)
    {
        var normalizedPath = NormalizeDatasetPath(datasetPath);
        var metadata = LoadMetadata(handle.DirectoryPath);

        if (!metadata.Datasets.TryGetValue(normalizedPath, out var info))
            throw new InvalidOperationException($"Dataset not found: {normalizedPath}");

        var rows = data.GetLength(0);
        var cols = data.GetLength(1);
        var binPath = GetBinPath(handle.DirectoryPath, normalizedPath);

        using var stream = new FileStream(binPath, FileMode.Open, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Seek to the correct row position
        var elementSize = sizeof(double);
        var startByte = startRow * info.ChannelCount * elementSize;
        stream.Seek(startByte, SeekOrigin.Begin);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols && c < info.ChannelCount; c++)
            {
                writer.Write(data[r, c]);
            }
        }
    }

    /// <summary>
    /// Reads a contiguous block of double-precision data from a dataset.
    /// </summary>
    public double[,] ReadDataset(Hdf5FileHandle handle, string datasetPath, long startRow = 0, long rowCount = -1)
    {
        var normalizedPath = NormalizeDatasetPath(datasetPath);
        var metadata = LoadMetadata(handle.DirectoryPath);

        if (!metadata.Datasets.TryGetValue(normalizedPath, out var info))
            throw new InvalidOperationException($"Dataset not found: {normalizedPath}");

        var totalRows = info.TotalSamples;
        var cols = info.ChannelCount;

        if (rowCount < 0 || startRow + rowCount > totalRows)
            rowCount = totalRows - startRow;

        var result = new double[rowCount, cols];
        var binPath = GetBinPath(handle.DirectoryPath, normalizedPath);

        using var stream = new FileStream(binPath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        var elementSize = sizeof(double);
        var startByte = startRow * cols * elementSize;
        stream.Seek(startByte, SeekOrigin.Begin);

        var buffer = new byte[cols * elementSize];
        for (long r = 0; r < rowCount; r++)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead < buffer.Length) break;

            for (int c = 0; c < cols; c++)
            {
                result[r, c] = BitConverter.ToDouble(buffer, c * elementSize);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a single channel from a dataset as a 1D array.
    /// </summary>
    public double[] ReadChannel(Hdf5FileHandle handle, string datasetPath, int channelIndex, long startRow = 0, long rowCount = -1)
    {
        var normalizedPath = NormalizeDatasetPath(datasetPath);
        var metadata = LoadMetadata(handle.DirectoryPath);

        if (!metadata.Datasets.TryGetValue(normalizedPath, out var info))
            throw new InvalidOperationException($"Dataset not found: {normalizedPath}");

        var totalRows = info.TotalSamples;
        var cols = info.ChannelCount;

        if (channelIndex < 0 || channelIndex >= cols)
            throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Channel index {channelIndex} out of range [0, {cols - 1}].");

        if (rowCount < 0 || startRow + rowCount > totalRows)
            rowCount = totalRows - startRow;

        var result = new double[rowCount];
        var binPath = GetBinPath(handle.DirectoryPath, normalizedPath);

        using var stream = new FileStream(binPath, FileMode.Open, FileAccess.Read);
        var elementSize = sizeof(double);
        var rowSize = cols * elementSize;

        for (long r = 0; r < rowCount; r++)
        {
            var byteOffset = (startRow + r) * rowSize + channelIndex * elementSize;
            stream.Seek(byteOffset, SeekOrigin.Begin);

            var valueBytes = new byte[elementSize];
            if (stream.Read(valueBytes, 0, elementSize) < elementSize) break;
            result[r] = BitConverter.ToDouble(valueBytes, 0);
        }

        return result;
    }

    /// <summary>
    /// Gets the dimensions of a dataset.
    /// </summary>
    public (long Rows, int Columns) GetDatasetDimensions(Hdf5FileHandle handle, string datasetPath)
    {
        var normalizedPath = NormalizeDatasetPath(datasetPath);
        var metadata = LoadMetadata(handle.DirectoryPath);

        if (!metadata.Datasets.TryGetValue(normalizedPath, out var info))
            throw new InvalidOperationException($"Dataset not found: {normalizedPath}");

        return (info.TotalSamples, info.ChannelCount);
    }

    /// <summary>
    /// Checks if a dataset exists.
    /// </summary>
    public bool DatasetExists(Hdf5FileHandle handle, string datasetPath)
    {
        var normalizedPath = NormalizeDatasetPath(datasetPath);
        var metadata = LoadMetadata(handle.DirectoryPath);
        return metadata.Datasets.ContainsKey(normalizedPath);
    }

    /// <summary>
    /// Gets the total size of the data directory in bytes.
    /// </summary>
    public long GetFileSize(Guid projectId)
    {
        var projectDir = GetProjectDirPath(projectId);
        if (!Directory.Exists(projectDir)) return 0;

        return Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// Deletes a project's data directory.
    /// </summary>
    public bool DeleteProjectFile(Guid projectId)
    {
        var projectDir = GetProjectDirPath(projectId);
        if (Directory.Exists(projectDir))
        {
            Directory.Delete(projectDir, recursive: true);
            return true;
        }
        return false;
    }

    // --- Private helpers ---

    private string GetProjectDirPath(Guid projectId)
    {
        return Path.Combine(_dataDirectory, $"{projectId}");
    }

    private static string NormalizeDatasetPath(string datasetPath)
    {
        var path = datasetPath.Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(path) ? "/" : "/" + path;
    }

    private static string GetBinPath(string projectDir, string normalizedPath)
    {
        // Replace path separators for a flat file name
        var safeName = normalizedPath.Trim('/').Replace('/', '_');
        if (string.IsNullOrEmpty(safeName)) safeName = "root";
        return Path.Combine(projectDir, $"{safeName}.bin");
    }

    private static string GetMetadataPath(string projectDir)
    {
        return Path.Combine(projectDir, "metadata.json");
    }

    private static Hdf5Metadata LoadMetadata(string projectDir)
    {
        var path = GetMetadataPath(projectDir);
        if (!File.Exists(path))
            return new Hdf5Metadata();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Hdf5Metadata>(json) ?? new Hdf5Metadata();
    }

    private static void SaveMetadata(string projectDir, Hdf5Metadata metadata)
    {
        var path = GetMetadataPath(projectDir);
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _openFiles)
        {
            try { kvp.Value.Dispose(); } catch { /* best effort */ }
        }
        _openFiles.Clear();
        _lock.Dispose();
    }
}

/// <summary>
/// Represents an open handle to a project's data directory.
/// Wraps the directory path and provides IDisposable semantics.
/// </summary>
public class Hdf5FileHandle : IDisposable
{
    /// <summary>
    /// Gets the directory path for this handle.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Creates a new file handle for the given directory path.
    /// </summary>
    public Hdf5FileHandle(string directoryPath)
    {
        DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
    }

    /// <summary>
    /// Disposes the handle. This is a no-op for directory-based storage,
    /// but provided for API compatibility.
    /// </summary>
    public void Dispose()
    {
        // No resources to release for directory-based storage
    }
}

/// <summary>
/// Metadata index tracking all datasets in a project's data directory.
/// </summary>
internal class Hdf5Metadata
{
    public Dictionary<string, Hdf5DatasetInfo> Datasets { get; set; } = new();
}

/// <summary>
/// Information about a single dataset.
/// </summary>
internal class Hdf5DatasetInfo
{
    public long TotalSamples { get; set; }
    public int ChannelCount { get; set; }
}
