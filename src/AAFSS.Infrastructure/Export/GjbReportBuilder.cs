using AAFSS.Core.Models;
using AAFSS.Core.Services;

namespace AAFSS.Infrastructure.Export;

/// <summary>
/// Generates GJB 67.13-90 compliant acoustic fatigue spectrum reports in DOCX format.
/// </summary>
public class GjbReportBuilder : IReportBuilder
{
    private readonly IChartToImageExporter? _chartExporter;

    private static readonly string[] GjbSections =
    {
        "1. Scope",
        "2. Reference Documents",
        "3. Terms and Definitions",
        "4. General Requirements",
        "5. Spectrum Compilation Process",
        "6. Process Parameters",
        "7. Spectrum Data Table",
        "8. Validation and Confirmation",
        "9. Conclusions"
    };

    public GjbReportBuilder(IChartToImageExporter? chartExporter = null)
    {
        _chartExporter = chartExporter;
    }

    /// <inheritdoc />
    public async Task<string> BuildReportAsync(CompiledSpectrum spectrum, string outputDirectory, CancellationToken ct = default)
    {
        var fileName = $"GJB_Report_{spectrum.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
        var filePath = Path.Combine(outputDirectory, fileName);

        string? capturedChartPath = null;
        if (_chartExporter != null)
        {
            capturedChartPath = await _chartExporter.ExportChartAsPngAsync(spectrum.Id, ct);
        }

        await Task.Run(() =>
        {
            using var doc = Xceed.Words.NET.DocX.Create(filePath);

            // Cover page
            var coverTitle = doc.InsertParagraph("Acoustic Fatigue Spectrum Compilation Report")
                .FontSize(24).Bold();
            coverTitle.Alignment = Xceed.Document.NET.Alignment.center;
            coverTitle.SpacingAfter(20);

            var p1 = doc.InsertParagraph("Basis: GJB 67.13-90").FontSize(14);
            p1.Alignment = Xceed.Document.NET.Alignment.center;

            var p2 = doc.InsertParagraph($"Project: {spectrum.Name}").FontSize(12);
            p2.Alignment = Xceed.Document.NET.Alignment.center;

            var p3 = doc.InsertParagraph($"Method: {spectrum.Method}").FontSize(12);
            p3.Alignment = Xceed.Document.NET.Alignment.center;

            var p4 = doc.InsertParagraph($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(12);
            p4.Alignment = Xceed.Document.NET.Alignment.center;

            doc.InsertSection();

            // Section 1: Scope
            AddSection(doc, GjbSections[0],
                $"This report describes the compilation process of {spectrum.Name}, " +
                "including data collection, spectrum analysis, statistical modeling, spectrum synthesis and validation steps.");

            // Section 2: Reference Documents
            AddSection(doc, GjbSections[1],
                "GJB 67.13-90 Military Aircraft Strength and Stiffness Specification - Acoustic Fatigue\n" +
                "GJB 150.16A-2009 Laboratory Environmental Test Methods - Part 16: Vibration Test\n" +
                "HB 20244-2014 Aircraft Structure Acoustic Fatigue Design Guide");

            // Section 3: Terms
            AddSection(doc, GjbSections[2],
                "PSD: Power Spectral Density\n" +
                "SPL: Sound Pressure Level\n" +
                "Miner D: Linear Cumulative Damage Value (Miner's Damage)");

            // Section 4: Requirements
            AddSection(doc, GjbSections[3],
                "Fatigue damage validation deviation: Green <= 5%, Yellow <= 10%, Red > 10%");

            // Section 5: Process
            AddSection(doc, GjbSections[4],
                $"Method: {spectrum.Method}\n" +
                $"Analysis Time: {spectrum.CompiledAt:yyyy-MM-dd HH:mm:ss}");

            // Section 6: Parameters
            AddSection(doc, GjbSections[5],
                $"Method: {spectrum.Method}\n" +
                $"Envelope Offset: {spectrum.EnvelopeOffset:F1} dB\n" +
                $"OASPL: {spectrum.Oaspl:F2} dB\n" +
                $"Spectrum Type: {spectrum.SpectrumType}\n" +
                $"Compiled At: {spectrum.CompiledAt:yyyy-MM-dd HH:mm:ss}");

            // Embed spectrum chart
            if (capturedChartPath != null && File.Exists(capturedChartPath))
            {
                doc.InsertParagraph();
                var chartLabel = doc.InsertParagraph("Spectrum Chart").FontSize(13).Bold();
                chartLabel.SpacingAfter(6);

                var image = doc.AddImage(capturedChartPath);
                var picture = image.CreatePicture();
                picture.Width = 500;
                picture.Height = 350;
                doc.InsertParagraph().AppendPicture(picture);

                var chartCaption = doc.InsertParagraph($"Figure: {spectrum.Name} Spectrum Chart")
                    .FontSize(10).Italic();
                chartCaption.Alignment = Xceed.Document.NET.Alignment.center;
            }

            // Section 7: Spectrum Data Table (first 200 rows)
            var rowCount = spectrum.Frequencies?.Length ?? 0;
            AddSection(doc, GjbSections[6],
                $"Frequency points: {rowCount}  |  OASPL: {spectrum.Oaspl:F2} dB\n" +
                (rowCount > 200 ? $"(Table shows first 200 rows, total {rowCount - 1} data rows)" : ""));

            if (rowCount > 0 && spectrum.Frequencies != null && spectrum.Levels != null)
            {
                var displayRows = Math.Min(rowCount, 200);
                var dataTable = doc.AddTable(displayRows + 1, 2);
                dataTable.Design = Xceed.Document.NET.TableDesign.LightListAccent1;
                dataTable.Rows[0].Cells[0].Paragraphs[0].Append("Frequency (Hz)").Bold();
                dataTable.Rows[0].Cells[1].Paragraphs[0].Append("SPL (dB)").Bold();

                for (int i = 0; i < displayRows; i++)
                {
                    dataTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(
                        spectrum.Frequencies[i].ToString("F2"));
                    if (i < spectrum.Levels.Length)
                        dataTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(
                            spectrum.Levels[i].ToString("F2"));
                }
                doc.InsertTable(dataTable);
            }

            // Section 8: Validation
            AddSection(doc, GjbSections[7], "Validation Results");

            var validationReport = spectrum.ValidationReport;
            if (validationReport != null)
            {
                var valTable = doc.AddTable(2, 4);
                valTable.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent1;
                valTable.Rows[0].Cells[0].Paragraphs[0].Append("Target Damage (D_target)").Bold();
                valTable.Rows[0].Cells[1].Paragraphs[0].Append("Actual Damage (D_actual)").Bold();
                valTable.Rows[0].Cells[2].Paragraphs[0].Append("Deviation (%)").Bold();
                valTable.Rows[0].Cells[3].Paragraphs[0].Append("Status").Bold();

                var deviationPct = validationReport.TargetD > 0
                    ? Math.Abs(validationReport.ActualD - validationReport.TargetD) / validationReport.TargetD * 100.0
                    : 0;

                valTable.Rows[1].Cells[0].Paragraphs[0].Append($"{validationReport.TargetD:F4}");
                valTable.Rows[1].Cells[1].Paragraphs[0].Append($"{validationReport.ActualD:F4}");
                valTable.Rows[1].Cells[2].Paragraphs[0].Append($"{deviationPct:F2}%");
                valTable.Rows[1].Cells[3].Paragraphs[0].Append(
                    deviationPct <= 5 ? "Green - Pass" :
                    deviationPct <= 10 ? "Yellow - Review" : "Red - Recompile");

                doc.InsertTable(valTable);
            }
            else
            {
                doc.InsertParagraph("(No validation data available)");
            }

            // Section 9: Conclusions
            string conclusion;
            if (validationReport == null)
            {
                conclusion = "Spectrum compilation completed. Ready for next phase.";
            }
            else
            {
                var target = validationReport.TargetD;
                var actualD = validationReport.ActualD;
                var deviation = target > 0 ? Math.Abs(actualD - target) / target * 100 : 0;

                if (deviation <= 5)
                {
                    conclusion =
                        "This spectrum compilation meets GJB 67.13-90 requirements. " +
                        $"Damage validation deviation is within {deviation:F1}% green zone. " +
                        "Applicable for subsequent aircraft structure acoustic fatigue analysis.";
                }
                else if (deviation <= 10)
                {
                    conclusion =
                        "This spectrum data basically meets GJB 67.13-90 requirements. " +
                        $"Damage validation deviation is within {deviation:F1}% yellow zone. " +
                        "It is recommended to review key conditions before use.";
                }
                else
                {
                    conclusion =
                        $"This spectrum damage validation deviation exceeds 10% ({deviation:F1}%). " +
                        "Does NOT meet GJB 67.13-90 requirements. " +
                        "Recommend re-collecting data or adjusting compilation parameters and regenerating.";
                }
            }

            AddSection(doc, GjbSections[8], conclusion);

            // Footer
            doc.AddFooters();
            var footerPara = doc.Footers.Odd.InsertParagraph(
                $"GJB 67.13-90  |  {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  AAFSS");
            footerPara.Alignment = Xceed.Document.NET.Alignment.center;
            footerPara.FontSize(9).Color(System.Drawing.Color.Gray);

            doc.Save();
        }, ct);

        return filePath;
    }

    private static void AddSection(Xceed.Words.NET.DocX doc, string title, string content)
    {
        doc.InsertParagraph();
        var heading = doc.InsertParagraph(title).FontSize(14).Bold();
        heading.SpacingAfter(8);
        doc.InsertParagraph(content).FontSize(11).SpacingAfter(4);
    }
}
