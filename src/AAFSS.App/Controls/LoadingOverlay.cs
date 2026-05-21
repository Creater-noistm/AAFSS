using System.Windows;
using System.Windows.Controls;

namespace AAFSS.App.Controls;

/// <summary>
/// A loading overlay control that displays a semi-transparent overlay with
/// a progress indicator and message.
/// </summary>
public class LoadingOverlay : ContentControl
{
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false, OnIsLoadingChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
            new PropertyMetadata("Loading..."));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(LoadingOverlay),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(true));

    static LoadingOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingOverlay),
            new FrameworkPropertyMetadata(typeof(LoadingOverlay)));
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
            overlay.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public LoadingOverlay()
    {
        Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
    }
}
