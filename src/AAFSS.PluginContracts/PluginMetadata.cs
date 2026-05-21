namespace AAFSS.PluginContracts;

/// <summary>
/// Metadata describing a plugin identity, version, and authorship.
/// Used by MEF2 plugin host for discovery and management.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Unique identifier for the plugin (e.g., "aafss.plugin.csv-importer").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Author or organization name.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of what the plugin does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum AAFSS version required (semver range).
    /// </summary>
    public string MinAafssVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Website or documentation URL for the plugin.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Tags for categorization and search.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    public override string ToString() => $"{Name} v{Version} by {Author}";
}
