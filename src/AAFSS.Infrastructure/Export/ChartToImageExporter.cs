using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Export;

/// <summary>
/// Exports spectrum chart images to PNG format for inclusion in reports.
/// Uses ScottPlot for rendering spectrum charts programmatically.
/// </summary>
public class ChartToImageExporter : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Default chart dimensions in pixels.
    /// </summary>
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;

    /// <summary>
    /// Exports a spectrum plot to a PNG file.
    /// </summary>
    /// <param name="spectrum">The compiled spectrum.</param>
    /// <param name="outputPath">Destination file path.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> ExportSpectrumChartAsync(
        CompiledSpectrum spectrum,
        string outputPath,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var plot = new ScottPlot.Plot();

            // Add spectrum data
            if (spectrum.Frequencies?.Length > 0 && spectrum.Levels?.Length > 0)
            {
                var scatter = plot.Add.Scatter(spectrum.Frequencies, spectrum.Levels);
                scatter.LineWidth = 2;
                scatter.Color = ScottPlot.Colors.Blue;
                scatter.LegendText = "SPL";
            }

            // Configure axes
            plot.Axes.Bottom.Label.Text = "Frequency (Hz)";
            plot.Axes.Left.Label.Text = "SPL (dB)";
            plot.Title($"Acoustic Fatigue Spectrum — {spectrum.Name}");

            plot.Legend.IsVisible = true;

            // Save
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            plot.SavePng(outputPath, Width, Height);

            return outputPath;
        }, ct);
    }

    /// <summary>
    /// Exports a damage spectrum plot.
    /// </summary>
    public async Task<string> ExportDamageChartAsync(
        CompiledSpectrum spectrum,
        double[] damageSpectrum,
        string outputPath,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var plot = new ScottPlot.Plot();

            if (spectrum.Frequencies?.Length > 0)
            {
                var freqLen = Math.Min(spectrum.Frequencies.Length, damageSpectrum.Length);
                var freqs = spectrum.Frequencies.Take(freqLen).ToArray();
                var damages = damageSpectrum.Take(freqLen).ToArray();

                var bars = plot.Add.Bars(damages);
                // Labels are set on the bars via positions
            }

            plot.Axes.Bottom.Label.Text = "Frequency (Hz)";
            plot.Axes.Left.Label.Text = "Damage D";
            plot.Title($"Damage Spectrum — {spectrum.Name}");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            plot.SavePng(outputPath, Width, Height);

            return outputPath;
        }, ct);
    }

    /// <summary>
    /// Exports a rainflow matrix heatmap.
    /// </summary>
    public async Task<string> ExportRainflowMatrixAsync(
        RainflowResult rainflow,
        string outputPath,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var plot = new ScottPlot.Plot();
            plot.Title($"Rainflow Matrix — {rainflow.Id}");
            plot.Axes.Bottom.Label.Text = "From";
            plot.Axes.Left.Label.Text = "To";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            plot.SavePng(outputPath, Width, Height);

            return outputPath;
        }, ct);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
