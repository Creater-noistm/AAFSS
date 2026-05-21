namespace AAFSS.Core.Models;

/// <summary>
/// Flight phases for mission profile classification.
/// </summary>
public enum FlightPhase
{
    Takeoff = 0,
    Climb = 1,
    Cruise = 2,
    Descent = 3,
    Landing = 4,
    Maneuver = 5,
    Taxi = 6,
    Afterburner = 7,
    Custom = 99
}

/// <summary>
/// Types of spectrum results produced by frequency analysis.
/// </summary>
public enum SpectrumType
{
    Octave1_1 = 0,
    Octave1_3 = 1,
    Octave1_6 = 2,
    Octave1_12 = 3,
    PsdWelch = 10,
    PsdPeriodogram = 11,
    CrossSpectrum = 20,
    Coherence = 21,
    ZoomFft = 30
}

/// <summary>
/// Categories of compiled spectra in the compilation hierarchy.
/// </summary>
public enum SpectrumCategory
{
    Base = 0,
    Severe = 1,
    FlightByFlight = 2,
    Envelope = 3,
    Corrected = 4,
    Smoothed = 5
}

/// <summary>
/// Sensor types used for acoustic/vibration measurements.
/// </summary>
public enum SensorType
{
    Microphone = 0,
    Accelerometer = 1,
    StrainGauge = 2,
    PressureTransducer = 3,
    Custom = 99
}

/// <summary>
/// Statistical distribution types for load spectrum fitting.
/// </summary>
public enum DistributionType
{
    Normal = 0,
    LogNormal = 1,
    Weibull2P = 2,
    Weibull3P = 3,
    Gumbel = 4,
    GeneralizedExtremeValue = 5,
    Exponential = 6,
    Rayleigh = 7,
    Gamma = 8
}

/// <summary>
/// Processing status for tracking computation pipeline progress.
/// </summary>
public enum ProcessingStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Skipped = 5
}

/// <summary>
/// Validation level indicators for damage verification.
/// </summary>
public enum ValidationLevel
{
    Green = 0,
    Yellow = 1,
    Red = 2,
    NotValidated = 3
}

/// <summary>
/// Report generation status.
/// </summary>
public enum ReportStatus
{
    Draft = 0,
    Generated = 1,
    Approved = 2,
    Archived = 3,
    Error = 4
}

/// <summary>
/// Data source origin types.
/// </summary>
public enum DataSourceType
{
    Measurement = 0,
    Simulation = 1,
    Analogy = 2,
    Specification = 3,
    Custom = 99
}

/// <summary>
/// Mission profile type classification.
/// </summary>
public enum MissionProfileType
{
    Standard = 0,
    Custom = 1,
    TestFlight = 2
}

/// <summary>
/// Spectrum compilation methods.
/// </summary>
public enum CompilationMethod
{
    StateRegionEnvelope = 0,
    MinerEquivalent = 1,
    FlightByFlight = 2,
    MaxEnvelope = 3,
    StatisticalExtreme = 4
}

/// <summary>
/// Overall validation status for spectrum results.
/// </summary>
public enum ValidationStatus
{
    Passed = 0,
    Warning = 1,
    Failed = 2,
    Pending = 3
}
