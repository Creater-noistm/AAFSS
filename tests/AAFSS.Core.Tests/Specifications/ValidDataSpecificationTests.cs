using AAFSS.Core.Models;
using AAFSS.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Specifications;

public class ValidDataSpecificationTests
{
    private readonly ValidDataSpecification _spec;

    public ValidDataSpecificationTests()
    {
        _spec = new ValidDataSpecification();
    }

    [Fact]
    public void IsSatisfiedBy_ValidResult_ShouldReturnTrue()
    {
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 1000,
            TotalDataPoints = 10000,
            SampleRateConsistent = true,
            OutlierCount = 0
        };

        _spec.IsSatisfiedBy(vr).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_NotValid_ShouldReturnFalse()
    {
        var vr = new DataValidationResult { IsValid = false };
        _spec.IsSatisfiedBy(vr).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_LowSampleRate_ShouldReturnFalse()
    {
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 10,
            TotalDataPoints = 10000,
            SampleRateConsistent = true
        };

        _spec.IsSatisfiedBy(vr).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_InsufficientDataPoints_ShouldReturnFalse()
    {
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 1000,
            TotalDataPoints = 50,
            SampleRateConsistent = true
        };

        _spec.IsSatisfiedBy(vr).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_InconsistentSampleRate_ShouldReturnFalse()
    {
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 1000,
            TotalDataPoints = 10000,
            SampleRateConsistent = false
        };

        _spec.IsSatisfiedBy(vr).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_TooManyOutliers_ShouldReturnFalse()
    {
        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 1000,
            TotalDataPoints = 100,
            SampleRateConsistent = true,
            OutlierCount = 10
        };

        _spec.IsSatisfiedBy(vr).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_DataSource_NoTimeSeries_ShouldReturnFalse()
    {
        var ds = new DataSource();
        _spec.IsSatisfiedBy(ds).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_DataSource_Valid_ShouldReturnTrue()
    {
        var ds = new DataSource
        {
            TimeSeriesData = new TimeSeriesData
            {
                SampleRate = 2000,
                SampleCount = 10000
            }
        };

        _spec.IsSatisfiedBy(ds).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_DataSource_LowSampleRate_ShouldReturnFalse()
    {
        var ds = new DataSource
        {
            TimeSeriesData = new TimeSeriesData
            {
                SampleRate = 10,
                SampleCount = 10000
            }
        };

        _spec.IsSatisfiedBy(ds).Should().BeFalse();
    }

    [Fact]
    public void CustomThresholds_ShouldAffectValidation()
    {
        var spec = new ValidDataSpecification
        {
            MinSampleRate = 500,
            MaxSampleRate = 10000,
            MinDataPoints = 1000,
            MaxOutlierRatio = 0.01
        };

        var vr = new DataValidationResult
        {
            IsValid = true,
            DetectedSampleRate = 600,
            TotalDataPoints = 5000,
            SampleRateConsistent = true,
            OutlierCount = 5
        };

        spec.IsSatisfiedBy(vr).Should().BeTrue();
    }
}
