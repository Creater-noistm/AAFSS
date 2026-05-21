using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a physical measurement point (sensor location) on the aircraft structure.
/// </summary>
public class MeasurementPoint
{
    /// <summary>Unique measurement point identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent mission profile identifier.</summary>
    [Required]
    public Guid ProfileId { get; set; }

    /// <summary>Human-readable point name (e.g., "机身框12-左侧").</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Textual description of the physical location.</summary>
    [MaxLength(500)]
    public string Location { get; set; } = string.Empty;

    /// <summary>X coordinate in aircraft coordinate system (mm).</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in aircraft coordinate system (mm).</summary>
    public double Y { get; set; }

    /// <summary>Z coordinate in aircraft coordinate system (mm).</summary>
    public double Z { get; set; }

    /// <summary>Target fatigue life in flight hours.</summary>
    public double TargetLife { get; set; }

    /// <summary>Type of sensor installed at this point.</summary>
    public SensorType SensorType { get; set; } = SensorType.Microphone;

    /// <summary>Sensor sensitivity in mV/Pa or mV/g.</summary>
    public double Sensitivity { get; set; } = 1.0;

    /// <summary>Sensor serial number.</summary>
    [MaxLength(128)]
    public string? SensorSerialNumber { get; set; }

    /// <summary>Calibration date.</summary>
    public DateTime? CalibrationDate { get; set; }

    /// <summary>Navigation property to parent profile.</summary>
    [ForeignKey(nameof(ProfileId))]
    public MissionProfile? Profile { get; set; }
}
