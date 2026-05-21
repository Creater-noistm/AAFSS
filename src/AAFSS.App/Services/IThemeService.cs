namespace AAFSS.App.Services;

/// <summary>
/// Service interface for theme management (light/dark switching).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Switches to the specified theme.
    /// </summary>
    /// <param name="themeName">Theme name ("Light" or "Dark").</param>
    void SetTheme(string themeName);

    /// <summary>
    /// Toggles between Light and Dark themes.
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<string>? ThemeChanged;
}
