using System.Globalization;
using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Import;

/// <summary>
/// Validates imported time series data for quality and consistency.
/// Checks for sample rate consistency, channel completeness, outliers,
/// and basic data integrity before ingestion into the processing pipeline.
/// </summary>
public class DataValidator
{
    /// <summary>
    /// Validates a data preview against expected parameters.
    /// Performs lightweight checks suitable for large files without loading all data.
    /// </summary>
    /// <param name="preview">Data preview from the importer.</param>
    /// <param name="expectedChannels">Expected channel names (null = no cross-check).</param>
    /// <param name="expectedSampleRate">Expected sample rate in Hz (0 = no check).</param>
    /// <returns>Validation result with messages and detected parameters.</returns>
    public DataValidationResult ValidatePreview(
        DataPreview preview,
        string[]? expectedChannels = null,
        double expectedSampleRate = 0)
    {
        var messages = new List<string>();
        var isValid = true;

        // 1. Basic checks
        if (preview.Headers.Length == 0)
        {
            messages.Add("No headers detected in the data file.");
            isValid = false;
        }

        if (preview.TotalRowCount == 0)
        {
            messages.Add("File contains no data rows.");
            isValid = false;
        }

        if (preview.ColumnCount == 0)
        {
            messages.Add("No columns detected.");
            isValid = false;
            return new DataValidationResult
            {
                IsValid = false,
                Messages = messages,
                DetectedChannelCount = 0,
                TotalDataPoints = 0
            };
        }

        // 2. Check for duplicate headers
        var duplicateHeaders = preview.Headers
            .GroupBy(h => h)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateHeaders.Count > 0)
        {
            messages.Add($"Duplicate column headers detected: {string.Join(", ", duplicateHeaders)}");
        }

        // 3. Detect numeric columns
        var numericColumnCount = 0;
        if (preview.Rows.Length > 0)
        {
            for (int c = 0; c < preview.ColumnCount; c++)
            {
                var isNumeric = preview.Rows.Take(Math.Min(10, preview.Rows.Length)).All(row =>
                {
                    if (c >= row.Length) return false;
                    return string.IsNullOrWhiteSpace(row[c]) ||
                           double.TryParse(row[c], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                });
                if (isNumeric) numericColumnCount++;
            }
        }

        if (numericColumnCount == 0)
        {
            messages.Add("No numeric data columns detected in the preview.");
            isValid = false;
        }

        // 4. Check expected channels
        if (expectedChannels != null && expectedChannels.Length > 0)
        {
            var headerSet = new HashSet<string>(preview.Headers, StringComparer.OrdinalIgnoreCase);
            var missing = expectedChannels.Where(c => !headerSet.Contains(c)).ToList();
            if (missing.Count > 0)
            {
                messages.Add($"Missing expected channels: {string.Join(", ", missing)}");
            }
        }

        // 5. Outlier detection in preview
        var outlierCount = 0;
        if (numericColumnCount > 0 && preview.Rows.Length > 0)
        {
            outlierCount = CountOutliersInPreview(preview, numericColumnCount);
            if (outlierCount > 0)
            {
                messages.Add($"Detected {outlierCount} potential outlier(s) in preview data.");
            }
        }

        return new DataValidationResult
        {
            IsValid = isValid,
            Messages = messages,
            SampleRateConsistent = true, // Cannot determine from preview alone
            ChannelsComplete = expectedChannels == null || expectedChannels.All(c =>
                preview.Headers.Contains(c, StringComparer.OrdinalIgnoreCase)),
            OutlierCount = outlierCount,
            DetectedSampleRate = 0,
            DetectedChannelCount = numericColumnCount,
            TotalDataPoints = preview.TotalRowCount * numericColumnCount,
            Duration = 0
        };
    }

    /// <summary>
    /// Validates full imported data against quality metrics.
    /// </summary>
    /// <param name="data">Full 2D data array [samples, channels].</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="channelNames">Channel names.</param>
    /// <param name="expectedSampleRate">Expected sample rate (0 = no check).</param>
    /// <returns>Detailed validation result.</returns>
    public DataValidationResult ValidateFullData(
        double[,] data,
        double sampleRate,
        string[]? channelNames = null,
        double expectedSampleRate = 0)
    {
        var messages = new List<string>();
        var totalSamples = data.GetLength(0);
        var channelCount = data.GetLength(1);
        var isValid = true;

        // 1. Basic integrity
        if (totalSamples == 0)
        {
            messages.Add("No data samples found.");
            return new DataValidationResult { IsValid = false, Messages = messages };
        }

        if (channelCount == 0)
        {
            messages.Add("No data channels found.");
            return new DataValidationResult { IsValid = false, Messages = messages };
        }

        // 2. Sample rate check
        if (expectedSampleRate > 0 && Math.Abs(sampleRate - expectedSampleRate) / expectedSampleRate > 0.01)
        {
            messages.Add($"Sample rate mismatch: expected {expectedSampleRate:F1} Hz, detected {sampleRate:F1} Hz.");
        }

        // 3. Detect NaN/Inf values
        var nanCount = 0;
        var infCount = 0;
        for (int r = 0; r < totalSamples; r++)
        {
            for (int c = 0; c < channelCount; c++)
            {
                if (double.IsNaN(data[r, c])) nanCount++;
                if (double.IsInfinity(data[r, c])) infCount++;
            }
        }

        if (nanCount > 0)
        {
            messages.Add($"Found {nanCount} NaN value(s) in the data.");
            isValid = false;
        }
        if (infCount > 0)
        {
            messages.Add($"Found {infCount} Infinity value(s) in the data.");
            isValid = false;
        }

        // 4. Statistical outlier detection (per channel, using IQR method)
        var outlierCount = 0;
        for (int c = 0; c < channelCount; c++)
        {
            outlierCount += CountOutliersInChannel(data, c, totalSamples);
        }

        if (outlierCount > 0)
        {
            var pct = 100.0 * outlierCount / (totalSamples * channelCount);
            messages.Add($"Detected {outlierCount} outlier(s) ({pct:F2}%) using IQR method.");
        }

        // 5. Check for constant-value channels
        for (int c = 0; c < channelCount; c++)
        {
            var firstVal = data[0, c];
            var isConstant = true;
            for (int r = 1; r < Math.Min(totalSamples, 10000); r++)
            {
                if (Math.Abs(data[r, c] - firstVal) > 1e-12)
                {
                    isConstant = false;
                    break;
                }
            }
            if (isConstant)
            {
                var channelIdentifier = channelNames != null && c < channelNames.Length
                    ? channelNames[c]
                    : $"Channel {c}";
                messages.Add($"Warning: {channelIdentifier} appears to have constant values.");
            }
        }

        // 6. Duration
        var duration = sampleRate > 0 ? totalSamples / sampleRate : 0;

        return new DataValidationResult
        {
            IsValid = isValid,
            Messages = messages,
            SampleRateConsistent = expectedSampleRate <= 0 || Math.Abs(sampleRate - expectedSampleRate) / expectedSampleRate <= 0.01,
            ChannelsComplete = true, // We have all channels in the data
            OutlierCount = outlierCount,
            DetectedSampleRate = sampleRate,
            DetectedChannelCount = channelCount,
            TotalDataPoints = totalSamples * (long)channelCount,
            Duration = duration
        };
    }

    /// <summary>
    /// Counts outliers in a preview dataset using simple z-score threshold.
    /// </summary>
    private static int CountOutliersInPreview(DataPreview preview, int numericColumnCount)
    {
        var outlierCount = 0;
        var threshold = 5.0; // 5-sigma for preview (conservative to avoid false positives)

        for (int c = 0; c < Math.Min(numericColumnCount, preview.ColumnCount); c++)
        {
            var values = new List<double>();
            for (int r = 0; r < preview.Rows.Length; r++)
            {
                if (c < preview.Rows[r].Length &&
                    double.TryParse(preview.Rows[r][c], NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                {
                    values.Add(val);
                }
            }

            if (values.Count < 4) continue;

            var mean = values.Average();
            var std = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));

            if (std > 0)
            {
                outlierCount += values.Count(v => Math.Abs(v - mean) / std > threshold);
            }
        }

        return outlierCount;
    }

    /// <summary>
    /// Counts outliers in a single channel using the IQR method.
    /// </summary>
    private static int CountOutliersInChannel(double[,] data, int channel, long totalSamples)
    {
        // Sample-based IQR on first 100k samples for performance
        var sampleSize = (int)Math.Min(totalSamples, 100_000);
        var step = Math.Max(1, (int)(totalSamples / sampleSize));
        var sampled = new List<double>(sampleSize);

        for (long i = 0; i < totalSamples; i += step)
        {
            sampled.Add(data[i, channel]);
        }

        sampled.Sort();
        var q1 = sampled[sampled.Count / 4];
        var q3 = sampled[3 * sampled.Count / 4];
        var iqr = q3 - q1;

        if (iqr <= 0) return 0;

        var lowerFence = q1 - 3.0 * iqr; // Use 3*IQR for extreme outliers
        var upperFence = q3 + 3.0 * iqr;

        var count = 0;
        for (long i = 0; i < totalSamples; i++)
        {
            if (data[i, channel] < lowerFence || data[i, channel] > upperFence)
                count++;
        }

        return count;
    }
}
