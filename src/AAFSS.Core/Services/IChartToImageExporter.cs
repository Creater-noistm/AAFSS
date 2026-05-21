namespace AAFSS.Core.Services;

/// <summary>
/// Interface for exporting charts to image files.
/// </summary>
public interface IChartToImageExporter
{
    /// <summary>
    /// Exports a spectrum chart as a PNG image file.
    /// </summary>
    /// <param name="spectrumId">The spectrum identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to the exported PNG file, or null if export failed.</returns>
    Task<string?> ExportChartAsPngAsync(Guid spectrumId, CancellationToken ct = default);
}
