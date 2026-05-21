using System.Globalization;
using ExcelDataReader;
using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Import;

/// <summary>
/// Imports time series data from Excel files (.xlsx, .xls).
/// Handles sheet selection, header detection, and progressive data reading.
/// </summary>
public class ExcelDataImporter
{
    /// <summary>
    /// Gets the supported file extensions for Excel import.
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".xlsx", ".xls", ".xlsm" };

    // Required for ExcelDataReader on .NET 8: register the encoding provider
    static ExcelDataImporter()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Gets a preview of the Excel data including headers and sample rows.
    /// </summary>
    /// <param name="filePath">Path to the Excel file.</param>
    /// <param name="sheetName">Sheet name (null = first sheet).</param>
    /// <param name="maxPreviewRows">Maximum preview rows.</param>
    /// <returns>DataPreview with headers, sample rows, and metadata.</returns>
    public async Task<DataPreview> GetPreviewAsync(string filePath, string? sheetName = null, int maxPreviewRows = 100)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}");

        return await Task.Run(() =>
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                UseColumnDataType = false,
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            var table = SelectSheet(dataSet, sheetName);
            var headers = new string[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
                headers[i] = table.Columns[i].ColumnName;

            var previewRows = new List<string[]>();
            var previewCount = Math.Min(maxPreviewRows, table.Rows.Count);

            for (int r = 0; r < previewCount; r++)
            {
                var row = table.Rows[r];
                var values = new string[table.Columns.Count];
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    values[c] = row[c]?.ToString() ?? string.Empty;
                }
                previewRows.Add(values);
            }

            return new DataPreview
            {
                Headers = headers,
                Rows = previewRows.ToArray(),
                TotalRowCount = table.Rows.Count,
                ColumnCount = table.Columns.Count,
                DetectedFormat = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant()
            };
        });
    }

    /// <summary>
    /// Reads the full Excel data as a 2D double array.
    /// </summary>
    /// <param name="filePath">Path to the Excel file.</param>
    /// <param name="sheetName">Sheet name (null = first sheet).</param>
    /// <param name="hasHeader">Whether the first row is a header.</param>
    /// <param name="onProgress">Optional progress callback (rowsRead, totalRows).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Data array, channel names, units, and estimated sample rate.</returns>
    public async Task<(double[,] Data, string[] ChannelNames, string[] ChannelUnits, double SampleRate)> ReadFullAsync(
        string filePath,
        string? sheetName = null,
        bool hasHeader = true,
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}");

        return await Task.Run(() =>
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                UseColumnDataType = false,
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = hasHeader
                }
            });

            var table = SelectSheet(dataSet, sheetName);

            // Detect numeric columns by sampling first 10 rows
            var numericCols = new List<int>();
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var isNumeric = true;
                var sampleCount = Math.Min(10, table.Rows.Count);
                for (int r = 0; r < sampleCount; r++)
                {
                    var val = table.Rows[r][c];
                    if (val != null && val != DBNull.Value)
                    {
                        var str = val.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(str) &&
                            !double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        {
                            isNumeric = false;
                            break;
                        }
                    }
                }
                if (isNumeric) numericCols.Add(c);
            }

            if (numericCols.Count == 0)
                throw new InvalidDataException("No numeric data columns found in the Excel file.");

            // Extract headers
            var channelNames = new string[numericCols.Count];
            for (int i = 0; i < numericCols.Count; i++)
            {
                channelNames[i] = numericCols[i] < table.Columns.Count
                    ? table.Columns[numericCols[i]].ColumnName
                    : $"Channel_{i}";
            }
            var channelUnits = new string[numericCols.Count];
            Array.Fill(channelUnits, "Pa");

            // Read data
            var totalRows = table.Rows.Count;
            var data = new double[totalRows, numericCols.Count];

            for (int r = 0; r < totalRows && !ct.IsCancellationRequested; r++)
            {
                var row = table.Rows[r];
                for (int c = 0; c < numericCols.Count; c++)
                {
                    var val = row[numericCols[c]];
                    if (val == null || val == DBNull.Value)
                    {
                        data[r, c] = 0.0;
                    }
                    else
                    {
                        var str = val.ToString() ?? string.Empty;
                        data[r, c] = string.IsNullOrWhiteSpace(str)
                            ? 0.0
                            : double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                }
                onProgress?.Invoke(r + 1, totalRows);
            }

            var sampleRate = EstimateSampleRate(data, totalRows);

            return (data, channelNames, channelUnits, sampleRate);
        });
    }

    /// <summary>
    /// Gets the list of sheet names in the Excel file.
    /// </summary>
    public async Task<string[]> GetSheetNamesAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = CreateReader(stream);
            var dataSet = reader.AsDataSet();
            var names = new string[dataSet.Tables.Count];
            for (int i = 0; i < dataSet.Tables.Count; i++)
                names[i] = dataSet.Tables[i].TableName;
            return names;
        });
    }

    /// <summary>
    /// Creates the appropriate Excel reader based on file extension.
    /// </summary>
    private static IExcelDataReader CreateReader(Stream stream)
    {
        // ExcelDataReader.CreateReader auto-detects format
        return ExcelReaderFactory.CreateReader(stream);
    }

    /// <summary>
    /// Selects a sheet by name or returns the first sheet.
    /// </summary>
    private static System.Data.DataTable SelectSheet(System.Data.DataSet dataSet, string? sheetName)
    {
        if (dataSet.Tables.Count == 0)
            throw new InvalidDataException("The Excel file contains no sheets.");

        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            var table = dataSet.Tables[sheetName];
            if (table != null) return table;

            // Try case-insensitive match
            for (int i = 0; i < dataSet.Tables.Count; i++)
            {
                if (string.Equals(dataSet.Tables[i].TableName, sheetName, StringComparison.OrdinalIgnoreCase))
                    return dataSet.Tables[i];
            }

            throw new ArgumentException($"Sheet '{sheetName}' not found. Available sheets: " +
                string.Join(", ", dataSet.Tables.Cast<System.Data.DataTable>().Select(t => t.TableName)));
        }

        return dataSet.Tables[0];
    }

    /// <summary>
    /// Estimates sample rate from time-like first column.
    /// </summary>
    private static double EstimateSampleRate(double[,] data, long totalRows)
    {
        if (totalRows < 2) return 1.0;

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
            if (avgDelta > 0 && stdDelta / avgDelta < 0.01)
                return 1.0 / avgDelta;
        }

        return 1.0;
    }
}
