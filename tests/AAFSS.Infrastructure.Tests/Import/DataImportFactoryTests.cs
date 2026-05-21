using AAFSS.Infrastructure.Import;
using FluentAssertions;
using Moq;
using Xunit;

namespace AAFSS.Infrastructure.Tests.Import;

public class DataImportFactoryTests
{
    [Fact]
    public void Constructor_WithNullProvider_ShouldThrow()
    {
        var act = () => new DataImportFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidProvider_ShouldSucceed()
    {
        var provider = new Mock<IServiceProvider>().Object;
        var factory = new DataImportFactory(provider);
        factory.Should().NotBeNull();
    }

    [Theory]
    [InlineData("data.csv", typeof(CsvDataImporter))]
    [InlineData("data.tsv", typeof(CsvDataImporter))]
    [InlineData("data.txt", typeof(CsvDataImporter))]
    [InlineData("data.dat", typeof(CsvDataImporter))]
    [InlineData("data.xlsx", typeof(ExcelDataImporter))]
    [InlineData("data.xls", typeof(ExcelDataImporter))]
    [InlineData("data.xlsm", typeof(ExcelDataImporter))]
    public void GetImporterType_ShouldReturnCorrectType(string filePath, Type expectedType)
    {
        var importerType = DataImportFactory.GetImporterType(filePath);
        importerType.Should().Be(expectedType);
    }

    [Fact]
    public void GetImporterType_UnsupportedFormat_ShouldThrow()
    {
        var act = () => DataImportFactory.GetImporterType("data.unknown");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetImporterType_EmptyExtension_ShouldThrow()
    {
        var act = () => DataImportFactory.GetImporterType("noextension");
        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("data.csv", true)]
    [InlineData("data.xlsx", true)]
    [InlineData("data.xls", true)]
    [InlineData("data.dat", true)]
    [InlineData("data.unknown", false)]
    [InlineData("data.pdf", false)]
    [InlineData("data.zip", false)]
    public void IsFormatSupported_ShouldReturnCorrectly(string filePath, bool expected)
    {
        DataImportFactory.IsFormatSupported(filePath).Should().Be(expected);
    }

    [Fact]
    public void FileFilter_ShouldContainExpectedFormats()
    {
        var filter = DataImportFactory.FileFilter;
        filter.Should().Contain(".csv");
        filter.Should().Contain(".xlsx");
        filter.Should().Contain("*.*");
    }

    [Fact]
    public void AllSupportedExtensions_ShouldContainCsvAndExcel()
    {
        var extensions = DataImportFactory.AllSupportedExtensions;
        extensions.Should().Contain(".csv");
        extensions.Should().Contain(".xlsx");
        extensions.Should().Contain(".xlsm");
    }
}
