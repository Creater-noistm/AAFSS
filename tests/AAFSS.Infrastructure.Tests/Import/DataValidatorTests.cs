using AAFSS.Core.Models;
using AAFSS.Infrastructure.Import;
using FluentAssertions;
using Xunit;

namespace AAFSS.Infrastructure.Tests.Import;

public class DataValidatorTests
{
    private readonly DataValidator _validator;

    public DataValidatorTests()
    {
        _validator = new DataValidator();
    }

    [Fact]
    public void ValidatePreview_EmptyHeaders_ShouldReturnInvalid()
    {
        var preview = new DataPreview
        {
            Headers = Array.Empty<string>(),
            Rows = Array.Empty<string[]>(),
            TotalRowCount = 0,
            ColumnCount = 0
        };

        var result = _validator.ValidatePreview(preview);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("No headers"));
    }

    [Fact]
    public void ValidatePreview_NoDataRows_ShouldReturnInvalid()
    {
        var preview = new DataPreview
        {
            Headers = new[] { "Time", "Ch1" },
            Rows = Array.Empty<string[]>(),
            TotalRowCount = 0,
            ColumnCount = 2
        };

        var result = _validator.ValidatePreview(preview);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("no data rows"));
    }

    [Fact]
    public void ValidatePreview_ValidData_ShouldReturnValid()
    {
        var preview = new DataPreview
        {
            Headers = new[] { "Time", "Ch1", "Ch2" },
            Rows = new[]
            {
                new[] { "0.0", "1.5", "2.5" },
                new[] { "0.1", "1.6", "2.6" },
                new[] { "0.2", "1.7", "2.7" }
            },
            TotalRowCount = 3,
            ColumnCount = 3
        };

        var result = _validator.ValidatePreview(preview);

        result.IsValid.Should().BeTrue();
        result.DetectedChannelCount.Should().Be(3);
        result.TotalDataPoints.Should().Be(9);
    }

    [Fact]
    public void ValidatePreview_DuplicateHeaders_ShouldWarn()
    {
        var preview = new DataPreview
        {
            Headers = new[] { "Time", "Ch1", "Ch1" },
            Rows = new[]
            {
                new[] { "0.0", "1.0", "2.0" }
            },
            TotalRowCount = 1,
            ColumnCount = 3
        };

        var result = _validator.ValidatePreview(preview);

        result.Messages.Should().Contain(m => m.Contains("Duplicate"));
    }

    [Fact]
    public void ValidatePreview_NoNumericColumns_ShouldReturnInvalid()
    {
        var preview = new DataPreview
        {
            Headers = new[] { "Name", "Status" },
            Rows = new[]
            {
                new[] { "Test", "OK" },
                new[] { "Test2", "FAIL" }
            },
            TotalRowCount = 2,
            ColumnCount = 2
        };

        var result = _validator.ValidatePreview(preview);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("No numeric"));
    }

    [Fact]
    public void ValidatePreview_MissingExpectedChannels_ShouldWarn()
    {
        var preview = new DataPreview
        {
            Headers = new[] { "Time", "Ch1" },
            Rows = new[]
            {
                new[] { "0.0", "1.0" },
                new[] { "0.1", "1.1" }
            },
            TotalRowCount = 2,
            ColumnCount = 2
        };

        var result = _validator.ValidatePreview(preview, new[] { "Ch1", "Ch2", "Ch3" });

        result.Messages.Should().Contain(m => m.Contains("Missing"));
        result.ChannelsComplete.Should().BeFalse();
    }

    [Fact]
    public void ValidateFullData_ValidData_ShouldReturnValid()
    {
        double[,] data = { { 1.0, 2.0 }, { 3.0, 4.0 }, { 5.0, 6.0 } };

        var result = _validator.ValidateFullData(data, 1000.0, new[] { "Ch1", "Ch2" });

        result.IsValid.Should().BeTrue();
        result.DetectedSampleRate.Should().Be(1000.0);
        result.DetectedChannelCount.Should().Be(2);
        result.TotalDataPoints.Should().Be(6);
        result.Duration.Should().Be(3.0 / 1000.0);
    }

    [Fact]
    public void ValidateFullData_EmptyData_ShouldReturnInvalid()
    {
        double[,] data = new double[0, 0];
        var result = _validator.ValidateFullData(data, 1000.0);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFullData_WithNanValues_ShouldReturnInvalid()
    {
        double[,] data = { { 1.0, double.NaN }, { 3.0, 4.0 } };

        var result = _validator.ValidateFullData(data, 1000.0);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("NaN"));
    }

    [Fact]
    public void ValidateFullData_SampleRateMismatch_ShouldWarn()
    {
        double[,] data = { { 1.0 } };

        var result = _validator.ValidateFullData(data, 1000.0, null, 2000.0);

        result.Messages.Should().Contain(m => m.Contains("mismatch"));
    }

    [Fact]
    public void ValidateFullData_ConstantChannel_ShouldWarn()
    {
        double[,] data = { { 5.0, 5.0 }, { 5.0, 6.0 }, { 5.0, 7.0 } };

        var result = _validator.ValidateFullData(data, 1000.0);

        result.Messages.Should().Contain(m => m.Contains("constant values"));
    }
}
