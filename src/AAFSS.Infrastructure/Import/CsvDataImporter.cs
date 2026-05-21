using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Import;

/// <summary>
/// Imports time series data from CSV (comma-separated values) files.
/// Handles header detection, column type inference, and progressive chunked reading
/// for large files.
/// </summary>
public class CsvDataImporter
{
    private static readonly CsvConfiguration DefaultConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        DetectDelimiter = true,
        MissingFieldFound = null,
        BadDataFound = null,
        TrimOptions = TrimOptions.Trim,
        DetectColumnCountChanges = true
    };

    /// <summary>
    /// Gets the supported file extensions for CSV import.
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".csv", ".tsv", ".txt", ".dat" };

    /// <summary>
    /// Reads headers and a preview from a CSV file without loading the full dataset.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="maxPreviewRows">Maximum number of preview rows to return.</param>
    /// <returns>DataPreview with headers, sample rows, and metadata.</returns>
    public async Task<DataPreview> GetPreviewAsync(string filePath, int maxPreviewRows = 100)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, DefaultConfig);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        // Read preview rows
        var previewRows = new List<string[]>();
        var totalRows = 0L;

        while (await csv.ReadAsync())
        {
            totalRows++;
            if (previewRows.Count < maxPreviewRows)
            {
                var row = new string[headers.Length];
                for (int i = 0; i < headers.Length; i++)
                {
                    row[i] = csv.GetField(i) ?? string.Empty;
                }
                previewRows.Add(row);
            }
        }

        return new DataPreview
        {
            Headers = headers,
            Rows = previewRows.ToArray(),
            TotalRowCount = totalRows,
            ColumnCount = headers.Length,
            DetectedFormat = "csv"
        };
    }

    /// <summary>
    /// Reads the full CSV file and returns data as a 2D double array.
    /// The first column is assumed to be time/index if it is numeric.
    /// Non-numeric columns are skipped.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="skipHeader">Whether to skip the first row as header.</param>
    /// <param name="onProgress">Optional progress callback (rowsRead, totalRows).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>2D array [rows, numeric columns] of parsed data.</returns>
    public async Task<(double[,] Data, string[] ChannelNames, string[] ChannelUnits, double SampleRate)> ReadFullAsync(
        string filePath,
        bool skipHeader = true,
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        // First pass: count rows and detect numeric columns
        var (totalRows, numericColumns, headers) = await AnalyzeCsvAsync(filePath, skipHeader, ct);

        if (numericColumns.Count == 0)
            throw new InvalidDataException("No numeric data columns found in the CSV file.");

        // Allocate result array
        var data = new double[totalRows, numericColumns.Count];

        // Second pass: parse numeric data
        await ParseNumericDataAsync(filePath, data, numericColumns, skipHeader, totalRows, onProgress, ct);

        // Build channel names from headers
        var channelNames = numericColumns.Select(c => headers.Length > c ? headers[c] : $"Channel_{c}").ToArray();
        var channelUnits = new string[numericColumns.Count];
        Array.Fill(channelUnits, "Pa"); // Default to Pa for acoustic data

        // Estimate sample rate from first column if it looks like time
        var sampleRate = EstimateSampleRate(data, totalRows);

        return (data, channelNames, channelUnits, sampleRate);
    }

    /// <summary>
    /// Reads CSV data in chunks and invokes a callback for each chunk.
    /// Suitable for very large files that exceed memory constraints.
    /// </summary>
    public async Task ReadChunkedAsync(
        string filePath,
        int chunkSize,
        Func<double[,], long, Task> onChunk,
        bool skipHeader = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var (totalRows, numericColumns, _) = await AnalyzeCsvAsync(filePath, skipHeader, ct);

        if (numericColumns.Count == 0)
            throw new InvalidDataException("No numeric data columns found.");

        var chunkIndex = 0L;
        for (long offset = 0; offset < totalRows && !ct.IsCancellationRequested; offset += chunkSize)
        {
            var actualChunkSize = (int)Math.Min(chunkSize, totalRows - offset);
            var chunk = new double[actualChunkSize, numericColumns.Count];

            await ParseNumericDataChunkAsync(filePath, chunk, numericColumns, skipHeader, offset, ct);

            await onChunk(chunk, chunkIndex++);
        }
    }

    /// <summary>
    /// Analyzes a CSV file to determine row count and numeric column indices.
    /// </summary>
    private async Task<(long TotalRows, List<int> NumericColumns, string[] Headers)> AnalyzeCsvAsync(
        string filePath, bool skipHeader, CancellationToken ct)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, DefaultConfig);

        var headers = Array.Empty<string>();
        var numericColumns = new List<int>();
        var totalRows = 0L;

        if (skipHeader)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            headers = csv.HeaderRecord ?? Array.Empty<string>();
        }
        else
        {
            // Read first data row to determine column count
            await csv.ReadAsync();
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    _ = csv.GetField(i);
                }
                catch
                {
                    break;
                }
            }
            headers = Enumerable.Range(0, csv.ColumnCount).Select(i => $"Column_{i}").ToArray();
        }

        // Sample first few rows to detect numeric columns
        var sampleRows = new List<string[]>();
        for (int i = 0; i < Math.Min(10, 1_000_000); i++)
        {
            if (!await csv.ReadAsync()) break;
            var record = csv.Parser.Record;
            sampleRows.Add(record ?? Array.Empty<string>());
            totalRows++;
        }

        // Detect which columns are numeric by sampling
        var columnCount = sampleRows.Count > 0 ? sampleRows[0].Length : headers.Length;
        for (int col = 0; col < columnCount; col++)
        {
            var isNumeric = sampleRows.Count > 0 && sampleRows.All(row =>
            {
                if (col >= row.Length) return false;
                var val = row[col];
                return string.IsNullOrWhiteSpace(val) || double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            });

            if (isNumeric)
                numericColumns.Add(col);
        }

        // Count remaining rows
        while (await csv.ReadAsync())
        {
            totalRows++;
            ct.ThrowIfCancellationRequested();
        }

        return (totalRows, numericColumns, headers);
    }

    /// <summary>
    /// Parses all numeric data from a CSV file into a pre-allocated 2D array.
    /// </summary>
    private static async Task ParseNumericDataAsync(
        string filePath, double[,] data, List<int> numericColumns,
        bool skipHeader, long expectedRows,
        Action<long, long>? onProgress, CancellationToken ct)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, DefaultConfig);

        if (skipHeader)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        var rowIndex = 0L;
        while (await csv.ReadAsync() && rowIndex < expectedRows)
        {
            for (int c = 0; c < numericColumns.Count; c++)
            {
                var field = csv.GetField(numericColumns[c]);
                data[rowIndex, c] = string.IsNullOrWhiteSpace(field)
                    ? 0.0
                    : double.Parse(field, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            rowIndex++;
            onProgress?.Invoke(rowIndex, expectedRows);
        }
    }

    /// <summary>
    /// Parses a chunk of numeric data starting at a given offset.
    /// </summary>
    private static async Task ParseNumericDataChunkAsync(
        string filePath, double[,] chunk, List<int> numericColumns,
        bool skipHeader, long startOffset, CancellationToken ct)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, DefaultConfig);

        if (skipHeader)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        // Skip to start offset
        for (long i = 0; i < startOffset && await csv.ReadAsync(); i++) { }

        var chunkRows = chunk.GetLength(0);
        for (int r = 0; r < chunkRows && await csv.ReadAsync(); r++)
        {
            for (int c = 0; c < numericColumns.Count; c++)
            {
                var field = csv.GetField(numericColumns[c]);
                chunk[r, c] = string.IsNullOrWhiteSpace(field)
                    ? 0.0
                    : double.Parse(field, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    /// Estimates sample rate from the first column of time-stamped data.
    /// Returns 1.0 if the first column doesn't appear to be time data.
    /// </summary>
    private static double EstimateSampleRate(double[,] data, long totalRows)
    {
        if (totalRows < 2) return 1.0;

        // If the first column values are monotonically increasing and have consistent deltas, it's likely a time column
        var deltas = new List<double>();
        for (long i = 1; i < Math.Min(totalRows, 100); i++)
        {
            var delta = data[i, 0] - data[i - 1, 0];
            if (delta > 0) deltas.Add(delta);
        }

        if (deltas.Count > 10)
        {
            var avgDelta = deltas.Average();
            var stdDelta = Math.Sqrt(deltas.Average(d => Math.Pow(d - avgDelta, 2)));
            // If the delta is very consistent (CV < 1%), it's likely a time column
            if (avgDelta > 0 && stdDelta / avgDelta < 0.01)
            {
                return 1.0 / avgDelta;
            }
        }

        return 1.0; // Unknown sample rate
    }
}
