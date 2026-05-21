using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Project aggregate root — represents a complete acoustic fatigue spectrum compilation project.
/// A project contains mission profiles, compiled spectra, and generated reports.
/// </summary>
public class Project
{
    /// <summary>Unique project identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable project name.</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Project description or notes.</summary>
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Timestamp when the project was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of the last modification.</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON serialized metadata for extensibility.</summary>
    [MaxLength(4000)]
    public string Metadata { get; set; } = "{}";

    /// <summary>File path of the .aafss project file.</summary>
    [MaxLength(1024)]
    public string? FilePath { get; set; }

    /// <summary>Associated mission profiles.</summary>
    public List<MissionProfile> Profiles { get; set; } = new();

    /// <summary>Associated compiled spectra.</summary>
    public List<CompiledSpectrum> Spectra { get; set; } = new();

    /// <summary>Associated generated reports.</summary>
    public List<GeneratedReport> Reports { get; set; } = new();

    /// <summary>
    /// Adds a mission profile to the project and updates the modification timestamp.
    /// </summary>
    public void AddProfile(MissionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.ProjectId = Id;
        Profiles.Add(profile);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a mission profile by its identifier.
    /// </summary>
    public void RemoveProfile(Guid profileId)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null)
        {
            Profiles.Remove(profile);
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Adds a compiled spectrum to the project.
    /// </summary>
    public void AddSpectrum(CompiledSpectrum spectrum)
    {
        ArgumentNullException.ThrowIfNull(spectrum);
        spectrum.ProjectId = Id;
        Spectra.Add(spectrum);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a generated report to the project.
    /// </summary>
    public void AddReport(GeneratedReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        report.ProjectId = Id;
        Reports.Add(report);
        ModifiedAt = DateTime.UtcNow;
    }
}
