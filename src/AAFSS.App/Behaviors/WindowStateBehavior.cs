using System.Windows;

namespace AAFSS.App.Behaviors;

/// <summary>
/// Attached behavior for managing window state persistence and restore.
/// Attach to a Window to automatically persist/restore position and size.
/// 
/// Usage:
///   behaviors:WindowStateBehavior.PersistState="True"
///   behaviors:WindowStateBehavior.StateKey="MainWindow"
/// </summary>
public static class WindowStateBehavior
{
    private const string RegistryBasePath = @"SOFTWARE\AAFSS\WindowStates";

    public static readonly DependencyProperty PersistStateProperty =
        DependencyProperty.RegisterAttached(
            "PersistState",
            typeof(bool),
            typeof(WindowStateBehavior),
            new PropertyMetadata(false, OnPersistStateChanged));

    public static readonly DependencyProperty StateKeyProperty =
        DependencyProperty.RegisterAttached(
            "StateKey",
            typeof(string),
            typeof(WindowStateBehavior),
            new PropertyMetadata("Default"));

    public static bool GetPersistState(DependencyObject obj)
        => (bool)obj.GetValue(PersistStateProperty);

    public static void SetPersistState(DependencyObject obj, bool value)
        => obj.SetValue(PersistStateProperty, value);

    public static string GetStateKey(DependencyObject obj)
        => (string)obj.GetValue(StateKeyProperty);

    public static void SetStateKey(DependencyObject obj, string value)
        => obj.SetValue(StateKeyProperty, value);

    private static void OnPersistStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if ((bool)e.NewValue)
            {
                RestoreWindowState(window);
                window.Closing += OnWindowClosing;
                window.SourceInitialized += OnWindowSourceInitialized;
            }
            else
            {
                window.Closing -= OnWindowClosing;
                window.SourceInitialized -= OnWindowSourceInitialized;
            }
        }
    }

    private static void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            RestoreWindowState(window);
        }
    }

    private static void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is Window window)
        {
            SaveWindowState(window);
        }
    }

    private static void RestoreWindowState(Window window)
    {
        var key = GetStateKey(window);
        try
        {
            using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"{RegistryBasePath}\\{key}");
            if (regKey != null)
            {
                var left = (int)regKey.GetValue("Left", -1);
                var top = (int)regKey.GetValue("Top", -1);
                var width = (int)regKey.GetValue("Width", -1);
                var height = (int)regKey.GetValue("Height", -1);
                var maximized = (int)regKey.GetValue("Maximized", 0) == 1;

                if (left >= 0 && top >= 0 && width > 0 && height > 0)
                {
                    // Ensure window is on a visible screen
                    var workingArea = SystemParameters.WorkArea;
                    if (left < workingArea.Right - 100 && top < workingArea.Bottom - 100)
                    {
                        window.Left = left;
                        window.Top = top;
                        window.Width = width;
                        window.Height = height;
                    }
                }

                if (maximized)
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
        }
        catch
        {
            // Silently ignore registry read failures
        }
    }

    private static void SaveWindowState(Window window)
    {
        // Only save normal state dimensions
        if (window.WindowState != WindowState.Normal) return;

        var key = GetStateKey(window);
        try
        {
            using var regKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"{RegistryBasePath}\\{key}");
            if (regKey != null)
            {
                regKey.SetValue("Left", (int)window.Left);
                regKey.SetValue("Top", (int)window.Top);
                regKey.SetValue("Width", (int)window.Width);
                regKey.SetValue("Height", (int)window.Height);
                regKey.SetValue("Maximized", 0);
            }
        }
        catch
        {
            // Silently ignore registry write failures
        }
    }
}
