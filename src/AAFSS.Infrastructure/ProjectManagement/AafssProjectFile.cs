using System.Text.Json;
using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.ProjectManagement;

/// <summary>
/// Manages AAFSS project file format (.aafss).
/// A project file packages the database snapshot and references to HDF5 data files
/// into a single distributable archive (ZIP-based format).
/// </summary>
public class AafssProjectFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Current file format version.
    /// </summary>
    public const string CurrentVersion = "1.0";

    /// <summary>
    /// File extension for AAFSS project files.
    /// </summary>
    public const string FileExtension = ".aafss";

    /// <summary>
    /// Saves a project and its associated data to a .aafss file (ZIP archive).
    /// </summary>
    /// <param name="project">The project to save.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="hdf5DataDir">Directory containing the project's HDF5 data file.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SaveAsync(Core.Models.Project project, string filePath, string? hdf5DataDir = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(filePath);

        if (!filePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            filePath += FileExtension;

        var tempDir = Path.Combine(Path.GetTempPath(), "aafss_save_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);

            // 1. Write project manifest (JSON)
            var manifest = CreateManifest(project);
            var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);
            var manifestPath = Path.Combine(tempDir, "project.json");
            await File.WriteAllTextAsync(manifestPath, manifestJson, ct);

            // 2. Copy HDF5 data file if exists
            var hdf5File = hdf5DataDir != null
                ? Path.Combine(hdf5DataDir, $"{project.Id}.h5")
                : null;

            if (hdf5File != null && File.Exists(hdf5File))
            {
                var destH5 = Path.Combine(tempDir, "data.h5");
                File.Copy(hdf5File, destH5, overwrite: true);
            }

            // 3. Create ZIP archive
            if (File.Exists(filePath))
                File.Delete(filePath);

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, filePath,
                System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            project.FilePath = filePath;
            project.ModifiedAt = DateTime.UtcNow;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Loads a project from a .aafss file.
    /// </summary>
    /// <param name="filePath">Source .aafss file path.</param>
    /// <param name="hdf5OutputDir">Directory to extract HDF5 data to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of (Project, extractedHdf5Path).</returns>
    public async Task<(Core.Models.Project Project, string? Hdf5Path)> LoadAsync(
        string filePath,
        string hdf5OutputDir,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(hdf5OutputDir);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Project file not found: {filePath}");

        var tempDir = Path.Combine(Path.GetTempPath(), "aafss_load_" + Guid.NewGuid().ToString("N"));
        string? hdf5Path = null;

        try
        {
            Directory.CreateDirectory(tempDir);

            // 1. Extract ZIP
            System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempDir);

            // 2. Read manifest
            var manifestPath = Path.Combine(tempDir, "project.json");
            if (!File.Exists(manifestPath))
                throw new InvalidDataException("Invalid project file: missing project.json manifest.");

            var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = JsonSerializer.Deserialize<ProjectManifest>(manifestJson, JsonOptions)
                ?? throw new InvalidDataException("Failed to deserialize project manifest.");

            var project = manifest.ToProject();
            project.FilePath = filePath;

            // 3. Extract HDF5 data if present
            var h5Source = Path.Combine(tempDir, "data.h5");
            if (File.Exists(h5Source))
            {
                Directory.CreateDirectory(hdf5OutputDir);
                hdf5Path = Path.Combine(hdf5OutputDir, $"{project.Id}.h5");
                File.Copy(h5Source, hdf5Path, overwrite: true);
            }

            return (project, hdf5Path);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Validates a .aafss file structure and reports any issues.
    /// </summary>
    /// <param name="filePath">Path to .aafss file.</param>
    /// <returns>Validation result with messages.</returns>
    public static DataValidationResult Validate(string filePath)
    {
        var messages = new List<string>();
        var isValid = true;

        if (!File.Exists(filePath))
        {
            messages.Add($"File not found: {filePath}");
            return new DataValidationResult { IsValid = false, Messages = messages };
        }

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var hasManifest = archive.Entries.Any(e => e.Name == "project.json");
            var hasHdf5 = archive.Entries.Any(e => e.Name == "data.h5");

            if (!hasManifest)
            {
                messages.Add("Missing project.json manifest.");
                isValid = false;
            }

            return new DataValidationResult
            {
                IsValid = isValid,
                Messages = messages,
                DetectedChannelCount = hasHdf5 ? 1 : 0,
                TotalDataPoints = hasHdf5 ? 1 : 0
            };
        }
        catch (Exception ex)
        {
            return new DataValidationResult
            {
                IsValid = false,
                Messages = new List<string> { $"Failed to open project file: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Creates a serializable manifest from a project entity.
    /// </summary>
    private static ProjectManifest CreateManifest(Core.Models.Project project)
    {
        return new ProjectManifest
        {
            Version = CurrentVersion,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            ModifiedAt = project.ModifiedAt,
            Profiles = project.Profiles.Select(p => new ProfileManifestEntry
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type.ToString()
            }).ToList(),
            Spectra = project.Spectra.Select(s => new SpectrumManifestEntry
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category.ToString()
            }).ToList(),
            Reports = project.Reports.Select(r => new ReportManifestEntry
            {
                Id = r.Id,
                TemplateName = r.TemplateName,
                Status = r.Status.ToString()
            }).ToList()
        };
    }
}

/// <summary>
/// Serializable project manifest stored in .aafss files.
/// </summary>
internal class ProjectManifest
{
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public List<ProfileManifestEntry> Profiles { get; set; } = new();
    public List<SpectrumManifestEntry> Spectra { get; set; } = new();
    public List<ReportManifestEntry> Reports { get; set; } = new();

    /// <summary>
    /// Converts the manifest to a Project entity (without child navigation entities).
    /// </summary>
    public Core.Models.Project ToProject()
    {
        return new Core.Models.Project
        {
            Id = Guid.NewGuid(), // New project ID on import
            Name = Name,
            Description = Description,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTime.UtcNow
        };
    }
}

internal class ProfileManifestEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

internal class SpectrumManifestEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

internal class ReportManifestEntry
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
