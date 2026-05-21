using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification that validates whether a data source meets quality requirements
/// for further processing. Checks sample rate consistency, channel completeness, and data density.
/// </summary>
public class ValidDataSpecification
{
    /// <summary>Minimum acceptable sample rate in Hz.</summary>
    public double MinSampleRate { get; set; } = 100.0;

    /// <summary>Maximum acceptable sample rate in Hz.</summary>
    public double MaxSampleRate { get; set; } = 500000.0;

    /// <summary>Minimum number of data points required.</summary>
    public long MinDataPoints { get; set; } = 100;

    /// <summary>Maximum outlier ratio allowed (0-1).</summary>
    public double MaxOutlierRatio { get; set; } = 0.05;

    /// <summary>
    /// Evaluates whether the data validation result meets the specification.
    /// </summary>
    /// <param name="validationResult">The data validation result to check.</param>
    /// <returns>True if the data is valid for processing; otherwise false.</returns>
    public bool IsSatisfiedBy(DataValidationResult validationResult)
    {
        if (!validationResult.IsValid)
            return false;

        if (validationResult.DetectedSampleRate < MinSampleRate ||
            validationResult.DetectedSampleRate > MaxSampleRate)
            return false;

        if (validationResult.TotalDataPoints < MinDataPoints)
            return false;

        if (!validationResult.SampleRateConsistent)
            return false;

        if (validationResult.TotalDataPoints > 0)
        {
            var outlierRatio = (double)validationResult.OutlierCount / validationResult.TotalDataPoints;
            if (outlierRatio > MaxOutlierRatio)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Evaluates whether a data source entity meets the specification based on its properties.
    /// </summary>
    /// <param name="dataSource">The data source to validate.</param>
    /// <returns>True if the data source is valid for processing; otherwise false.</returns>
    public bool IsSatisfiedBy(DataSource dataSource)
    {
        if (dataSource.TimeSeriesData == null)
            return false;

        var tsData = dataSource.TimeSeriesData;

        if (tsData.SampleRate < MinSampleRate || tsData.SampleRate > MaxSampleRate)
            return false;

        if (tsData.SampleCount < MinDataPoints)
            return false;

        return true;
    }
}
