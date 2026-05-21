using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a specific flight condition within a mission profile.
/// Examples: takeoff roll, climb at max power, cruise at altitude, landing approach.
/// </summary>
public class FlightCondition
{
    /// <summary>Unique condition identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent mission profile identifier.</summary>
    [Required]
    public Guid ProfileId { get; set; }

    /// <summary>Human-readable condition name.</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Flight phase classification.</summary>
    public FlightPhase Phase { get; set; } = FlightPhase.Cruise;

    /// <summary>Altitude in meters.</summary>
    public double Altitude { get; set; }

    /// <summary>Mach number.</summary>
    public double MachNumber { get; set; }

    /// <summary>Duration of this condition in minutes.</summary>
    public double Duration { get; set; }

    /// <summary>Weight percentage of this condition within the profile (0-100).</summary>
    public double Weight { get; set; }

    /// <summary>Primary noise source designation for this condition.</summary>
    [MaxLength(256)]
    public string PrimaryNoiseSource { get; set; } = "Engine";

    /// <summary>Dynamic pressure in Pa (calculated).</summary>
    public double DynamicPressure { get; set; }

    /// <summary>Serialized additional parameters (JSON).</summary>
    [MaxLength(2000)]
    public string AdditionalParametersJson { get; set; } = "{}";

    /// <summary>Navigation property to parent profile.</summary>
    [ForeignKey(nameof(ProfileId))]
    public MissionProfile? Profile { get; set; }
}
