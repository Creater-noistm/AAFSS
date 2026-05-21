using AAFSS.Core.Models;
using MediatR;

namespace AAFSS.Core.Commands;

/// <summary>
/// Command to apply a signal processing operation (filtering, detrending,
/// decimation, or calibration) to time series data.
/// Delegates to ISignalProcessingService and publishes ProcessingCompletedEvent.
/// </summary>
public record ProcessSignalCommand(
    Guid DataSourceId,
    string OperationType,
    Dictionary<string, double> Parameters,
    int ChannelIndex = 0
) : IRequest<ProcessingResult>;

/// <summary>
/// Well-known signal processing operation types.
/// </summary>
public static class SignalOperationType
{
    public const string ApplyFilter = "Filter";
    public const string Detrend = "Detrend";
    public const string Decimate = "Decimate";
    public const string ApplyCalibration = "Calibration";
}
