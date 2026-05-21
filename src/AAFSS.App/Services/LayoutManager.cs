using System.IO;
using System.Text.Json;

namespace AAFSS.App.Services;

/// <summary>
/// Manages AvalonDock layout persistence using local app data.
/// </summary>
public class LayoutManager : ILayoutManager
{
    private readonly string _layoutFilePath;

    /// <summary>
    /// Initializes the layout manager with a path in local app data.
    /// </summary>
    public LayoutManager()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AAFSS");
        Directory.CreateDirectory(appData);
        _layoutFilePath = Path.Combine(appData, "layout.json");
    }

    /// <inheritdoc/>
    public void SaveLayout(string layoutXml)
    {
        try
        {
            var data = new LayoutData { LayoutXml = layoutXml, SavedAt = DateTime.UtcNow };
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(_layoutFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save layout: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public string? LoadLayout()
    {
        try
        {
            if (!File.Exists(_layoutFilePath))
                return null;

            var json = File.ReadAllText(_layoutFilePath);
            var data = JsonSerializer.Deserialize<LayoutData>(json);
            return data?.LayoutXml;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load layout: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public void ShowPanel(string panelId)
    {
        // Panel visibility is managed through AvalonDock's DockingManager
        // This is a placeholder for programmatic panel control
    }

    /// <inheritdoc/>
    public void HidePanel(string panelId)
    {
        // Panel visibility is managed through AvalonDock's DockingManager
    }

    /// <inheritdoc/>
    public void ResetToDefault()
    {
        try
        {
            if (File.Exists(_layoutFilePath))
            {
                File.Delete(_layoutFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to reset layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal data structure for layout persistence.
    /// </summary>
    private sealed class LayoutData
    {
        public string LayoutXml { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}
