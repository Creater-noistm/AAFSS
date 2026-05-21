namespace AAFSS.Core.Models;

/// <summary>
/// Parameters defining a mission profile's operational envelope.
/// </summary>
public record ProfileParameters
{
    /// <summary>Design cruise altitude in meters.</summary>
    public double Altitude { get; init; }

    /// <summary>Design cruise Mach number.</summary>
    public double MachNumber { get; init; }

    /// <summary>Total mission duration in minutes.</summary>
    public double Duration { get; init; }

    /// <summary>Profile weight percentage in the overall mission (0-100).</summary>
    public double Weight { get; init; }

    /// <summary>Dynamic pressure at cruise, Pa.</summary>
    public double DynamicPressure { get; init; }

    /// <summary>Ambient temperature at cruise altitude, K.</summary>
    public double AmbientTemperature { get; init; }

    /// <summary>Aircraft gross weight fraction.</summary>
    public double GrossWeightFraction { get; init; } = 1.0;

    /// <summary>Additional custom parameters as key-value pairs.</summary>
    public Dictionary<string, double> CustomParameters { get; init; } = new();
}

/// <summary>
/// Result of data validation checks during import.
/// </summary>
public record DataValidationResult
{
    /// <summary>Whether the data passed all validation checks.</summary>
    public bool IsValid { get; init; }

    /// <summary>List of validation messages (warnings and errors).</summary>
    public List<string> Messages { get; init; } = new();

    /// <summary>Whether the sample rate is consistent across channels.</summary>
    public bool SampleRateConsistent { get; init; }

    /// <summary>Whether all expected channels are present.</summary>
    public bool ChannelsComplete { get; init; }

    /// <summary>Number of outlier points detected in preview.</summary>
    public int OutlierCount { get; init; }

    /// <summary>Detected sample rate in Hz.</summary>
    public double DetectedSampleRate { get; init; }

    /// <summary>Number of channels detected.</summary>
    public int DetectedChannelCount { get; init; }

    /// <summary>Total number of data points.</summary>
    public long TotalDataPoints { get; init; }

    /// <summary>Duration of the data in seconds.</summary>
    public double Duration { get; init; }
}

/// <summary>
/// Result from a signal processing or analysis operation.
/// </summary>
public record ProcessingResult
{
    /// <summary>Whether the processing completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>ID of the created ProcessingStep record.</summary>
    public Guid? ProcessingStepId { get; init; }

    /// <summary>Output data reference (HDF5 path or storage key).</summary>
    public string? OutputRef { get; init; }

    /// <summary>Error message if processing failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Processing duration in milliseconds.</summary>
    public double DurationMs { get; init; }

    /// <summary>Additional result metadata.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Preview of data to be imported (first N rows).
/// </summary>
public record DataPreview
{
    /// <summary>Column headers / channel names.</summary>
    public string[] Headers { get; init; } = Array.Empty<string>();

    /// <summary>Preview rows as string values (max 100 rows).</summary>
    public string[][] Rows { get; init; } = Array.Empty<string[]>();

    /// <summary>Total number of rows detected in the file.</summary>
    public long TotalRowCount { get; init; }

    /// <summary>Total number of columns detected.</summary>
    public int ColumnCount { get; init; }

    /// <summary>Detected file format.</summary>
    public string DetectedFormat { get; init; } = string.Empty;
}

/// <summary>
/// Frequency range specification for analysis operations.
/// </summary>
public record FrequencyRange
{
    /// <summary>Minimum frequency in Hz.</summary>
    public double MinHz { get; init; }

    /// <summary>Maximum frequency in Hz.</summary>
    public double MaxHz { get; init; }

    /// <summary>Frequency resolution in Hz.</summary>
    public double ResolutionHz { get; init; }

    /// <summary>Number of frequency bins.</summary>
    public int BinCount => (int)((MaxHz - MinHz) / ResolutionHz) + 1;
}

/// <summary>
/// S-N curve definition for fatigue analysis.
/// </summary>
public record SnCurve
{
    /// <summary>Material name / identifier.</summary>
    public string MaterialName { get; init; } = string.Empty;

    /// <summary>Fatigue strength coefficient (Sf').</summary>
    public double FatigueStrengthCoefficient { get; init; }

    /// <summary>Fatigue strength exponent (b).</summary>
    public double FatigueStrengthExponent { get; init; }

    /// <summary>Fatigue ductility coefficient (Ef').</summary>
    public double FatigueDuctilityCoefficient { get; init; }

    /// <summary>Fatigue ductility exponent (c).</summary>
    public double FatigueDuctilityExponent { get; init; }

    /// <summary>Endurance limit stress in MPa.</summary>
    public double EnduranceLimit { get; init; }

    /// <summary>Elastic modulus in GPa.</summary>
    public double ElasticModulus { get; init; }

    /// <summary>Reference stress concentration factor (Kt).</summary>
    public double Kt { get; init; } = 1.0;
}

/// <summary>
/// Configuration for Goodman correction.
/// </summary>
public record GoodmanCorrectionConfig
{
    /// <summary>Ultimate tensile strength in MPa.</summary>
    public double UltimateTensileStrength { get; init; }

    /// <summary>Mean stress in MPa.</summary>
    public double MeanStress { get; init; }

    /// <summary>Temperature correction factor.</summary>
    public double TemperatureFactor { get; init; } = 1.0;

    /// <summary>Humidity correction factor.</summary>
    public double HumidityFactor { get; init; } = 1.0;
}

/// <summary>
/// Configuration for spectrum smoothing.
/// </summary>
public record SmoothingConfig
{
    /// <summary>Smoothing method ("MovingAverage" or "SavitzkyGolay").</summary>
    public string Method { get; init; } = "MovingAverage";

    /// <summary>Window size for MovingAverage, polynomial order for Savitzky-Golay.</summary>
    public int WindowSize { get; init; } = 5;

    /// <summary>Polynomial order for Savitzky-Golay (ignored for MovingAverage).</summary>
    public int PolynomialOrder { get; init; } = 2;
}

/// <summary>
/// Project tree node for hierarchical display.
/// </summary>
public record ProjectTreeNode
{
    /// <summary>Display name of the node.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Node type category for icon selection.</summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>Associated entity ID, if applicable.</summary>
    public Guid? EntityId { get; init; }

    /// <summary>Processing status for status icon display.</summary>
    public ProcessingStatus Status { get; init; }

    /// <summary>Child nodes.</summary>
    public List<ProjectTreeNode> Children { get; init; } = new();
}
