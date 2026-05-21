using System.Windows;

namespace AAFSS.App.Services;

/// <summary>
/// Manages application theme switching between Light and Dark modes.
/// </summary>
public class ThemeService : IThemeService
{
    private const string LightThemePath = "Themes/LightTheme.xaml";
    private const string DarkThemePath = "Themes/DarkTheme.xaml";
    private const string SharedStylesPath = "Themes/SharedStyles.xaml";

    private string _currentTheme = "Light";

    /// <inheritdoc/>
    public string CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public event EventHandler<string>? ThemeChanged;

    /// <inheritdoc/>
    public void SetTheme(string themeName)
    {
        if (themeName == _currentTheme)
            return;

        var appResources = Application.Current.Resources;
        var dictionaries = appResources.MergedDictionaries;

        // Remove the current theme dictionary
        var oldThemePath = _currentTheme == "Light" ? LightThemePath : DarkThemePath;
        var oldDict = dictionaries.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.EndsWith(oldThemePath, StringComparison.OrdinalIgnoreCase));
        if (oldDict != null)
        {
            dictionaries.Remove(oldDict);
        }

        // Add the new theme dictionary
        var newThemePath = themeName == "Dark" ? DarkThemePath : LightThemePath;
        var newDict = new ResourceDictionary
        {
            Source = new Uri(newThemePath, UriKind.Relative)
        };
        dictionaries.Add(newDict);

        _currentTheme = themeName;
        ThemeChanged?.Invoke(this, _currentTheme);
    }

    /// <inheritdoc/>
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == "Light" ? "Dark" : "Light";
        SetTheme(newTheme);
    }
}
