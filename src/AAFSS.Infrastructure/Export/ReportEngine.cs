using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Export;

/// <summary>
/// Report generation engine using DocX.
/// Generates Word documents with charts, tables, and formatted content.
/// </summary>
public class ReportEngine : IDisposable
{
    private readonly ChartToImageExporter _chartExporter;
    private bool _disposed;

    public ReportEngine(ChartToImageExporter chartExporter)
    {
        _chartExporter = chartExporter;
    }

    /// <summary>
    /// Generates a DOCX report for a compiled spectrum with embedded chart and data table.
    /// </summary>
    public async Task<string> GenerateSpectrumReportAsync(
        CompiledSpectrum spectrum,
        string templateName,
        string outputDirectory,
        CancellationToken ct = default)
    {
        var fileName = $"spectrum_{spectrum.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
        var filePath = Path.Combine(outputDirectory, fileName);
        Directory.CreateDirectory(outputDirectory);

        // Export chart as PNG first (async)
        string? chartFilePath = null;
        try
        {
            var chartsDir = Path.Combine(outputDirectory, "charts");
            var chartPngPath = Path.Combine(chartsDir, $"spectrum_{spectrum.Id:N}.png");
            chartFilePath = await _chartExporter.ExportSpectrumChartAsync(
                spectrum, chartPngPath, ct);
        }
        catch (Exception)
        {
            // Chart export is best-effort; continue without it
        }

        // DOCX generation (synchronous, inside Task.Run)
        var capturedChartPath = chartFilePath;
        return await Task.Run(() =>
        {
            using var doc = Xceed.Words.NET.DocX.Create(filePath);

            // Title
            var title = doc.InsertParagraph($"Acoustic Fatigue Spectrum Report - {spectrum.Name}")
               .FontSize(18).Bold();
            title.Alignment = Xceed.Document.NET.Alignment.center;

            var subtitle = doc.InsertParagraph($"Template: {templateName}")
               .FontSize(10).Color(System.Drawing.Color.Gray);
            subtitle.Alignment = Xceed.Document.NET.Alignment.center;

            doc.InsertParagraph();

            // Metadata section
            doc.InsertParagraph("1. Spectrum Summary").FontSize(14).Bold().SpacingAfter(8);

            var metaTable = doc.InsertTable(6, 2);
            metaTable.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent1;
            metaTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

            metaTable.Rows[0].Cells[0].Paragraphs[0].Append("Spectrum Name").Bold();
            metaTable.Rows[0].Cells[1].Paragraphs[0].Append(spectrum.Name);
            metaTable.Rows[1].Cells[0].Paragraphs[0].Append("Spectrum Type").Bold();
            metaTable.Rows[1].Cells[1].Paragraphs[0].Append(spectrum.SpectrumType.ToString());
            metaTable.Rows[2].Cells[0].Paragraphs[0].Append("Method").Bold();
            metaTable.Rows[2].Cells[1].Paragraphs[0].Append(spectrum.Method.ToString());
            metaTable.Rows[3].Cells[0].Paragraphs[0].Append("OASPL").Bold();
            metaTable.Rows[3].Cells[1].Paragraphs[0].Append($"{spectrum.Oaspl:F2} dB");
            metaTable.Rows[4].Cells[0].Paragraphs[0].Append("Fatigue Damage D").Bold();
            metaTable.Rows[4].Cells[1].Paragraphs[0].Append($"{spectrum.DamageValue:E4}");
            metaTable.Rows[5].Cells[0].Paragraphs[0].Append("Compiled At").Bold();
            metaTable.Rows[5].Cells[1].Paragraphs[0].Append($"{spectrum.CompiledAt:yyyy-MM-dd HH:mm:ss}");

            // Spectrum chart
            if (capturedChartPath != null && File.Exists(capturedChartPath))
            {
                doc.InsertParagraph();
                doc.InsertParagraph("2. Spectrum Chart").FontSize(14).Bold().SpacingAfter(8);

                var image = doc.AddImage(capturedChartPath);
                var picture = image.CreatePicture();
                picture.Height = 400;
                picture.Width = 600;
                doc.InsertParagraph().AppendPicture(picture);
            }

            // Spectrum data table
            if (spectrum.Frequencies?.Length > 0 && spectrum.Levels?.Length > 0)
            {
                doc.InsertParagraph();
                doc.InsertParagraph("3. Spectrum Data Table").FontSize(14).Bold().SpacingAfter(8);

                int rowCount = Math.Min(spectrum.Frequencies.Length, spectrum.Levels.Length) + 1;
                var dataTable = doc.InsertTable(rowCount, 2);
                dataTable.Design = Xceed.Document.NET.TableDesign.LightListAccent1;

                // Header
                dataTable.Rows[0].Cells[0].Paragraphs[0].Append("Frequency (Hz)").Bold();
                dataTable.Rows[0].Cells[1].Paragraphs[0].Append("SPL (dB)").Bold();

                // Data rows - limit to 200 rows
                int maxRows = Math.Min(rowCount - 1, 200);
                for (int i = 0; i < maxRows; i++)
                {
                    dataTable.Rows[i + 1].Cells[0].Paragraphs[0].Append($"{spectrum.Frequencies[i]:F2}");
                    dataTable.Rows[i + 1].Cells[1].Paragraphs[0].Append($"{spectrum.Levels[i]:F2}");
                }

                if (rowCount > 201)
                {
                    var note = doc.InsertParagraph(
                        $"(Table shows first 200 rows, total {rowCount - 1} data rows)")
                        .FontSize(9).Color(System.Drawing.Color.Gray).Italic();
                }
            }

            // Footer
            doc.AddFooters();
            var footerPara = doc.Footers.Odd.InsertParagraph(
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  AAFSS");
            footerPara.Alignment = Xceed.Document.NET.Alignment.center;
            footerPara.FontSize(9).Color(System.Drawing.Color.Gray);

            doc.Save();
            return filePath;
        }, ct);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _chartExporter?.Dispose();
            _disposed = true;
        }
    }
}
