using AAFSS.Core.Models;

namespace AAFSS.Core.Services;

/// <summary>
/// Interface for report generation services that produce documented reports
/// from compiled spectrum data.
/// </summary>
public interface IReportBuilder
{
    /// <summary>
    /// Builds a report for the given compiled spectrum and saves to the output directory.
    /// </summary>
    /// <param name="spectrum">The compiled spectrum to report on.</param>
    /// <param name="outputDirectory">Directory to save the report file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full path to the generated report file.</returns>
    Task<string> BuildReportAsync(CompiledSpectrum spectrum, string outputDirectory, CancellationToken ct = default);
}
