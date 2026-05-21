namespace AAFSS.App.Services;

/// <summary>
/// Service for managing the AvalonDock layout (panel visibility, positions, persistence).
/// </summary>
public interface ILayoutManager
{
    /// <summary>
    /// Saves the current docking layout to persistent storage.
    /// </summary>
    void SaveLayout(string layoutXml);

    /// <summary>
    /// Loads the persisted docking layout.
    /// </summary>
    /// <returns>The layout XML string, or null if no saved layout exists.</returns>
    string? LoadLayout();

    /// <summary>
    /// Shows a specific panel by its identifier.
    /// </summary>
    void ShowPanel(string panelId);

    /// <summary>
    /// Hides a specific panel by its identifier.
    /// </summary>
    void HidePanel(string panelId);

    /// <summary>
    /// Resets the layout to default configuration.
    /// </summary>
    void ResetToDefault();
}
