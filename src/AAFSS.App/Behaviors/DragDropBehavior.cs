using System.Windows;
using System.Windows.Input;

namespace AAFSS.App.Behaviors;

/// <summary>
/// Attached behavior that enables drag-and-drop for files onto any UI element.
/// Use: behaviors:DragDropBehavior.AllowDrop="True"
///      behaviors:DragDropBehavior.FileDroppedCommand="{Binding ImportFileCommand}"
/// </summary>
public static class DragDropBehavior
{
    public static readonly DependencyProperty AllowDropProperty =
        DependencyProperty.RegisterAttached(
            "AllowDrop",
            typeof(bool),
            typeof(DragDropBehavior),
            new PropertyMetadata(false, OnAllowDropChanged));

    public static readonly DependencyProperty FileDroppedCommandProperty =
        DependencyProperty.RegisterAttached(
            "FileDroppedCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(null));

    public static bool GetAllowDrop(DependencyObject obj)
        => (bool)obj.GetValue(AllowDropProperty);

    public static void SetAllowDrop(DependencyObject obj, bool value)
        => obj.SetValue(AllowDropProperty, value);

    public static ICommand GetFileDroppedCommand(DependencyObject obj)
        => (ICommand)obj.GetValue(FileDroppedCommandProperty);

    public static void SetFileDroppedCommand(DependencyObject obj, ICommand value)
        => obj.SetValue(FileDroppedCommandProperty, value);

    private static void OnAllowDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.AllowDrop = true;
                element.Drop += OnDrop;
            }
            else
            {
                element.AllowDrop = false;
                element.Drop -= OnDrop;
                element.DragOver -= OnDragOver;
            }
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            sender is DependencyObject depObj)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var command = GetFileDroppedCommand(depObj);
            if (command != null && command.CanExecute(files))
            {
                command.Execute(files);
            }
        }
        e.Handled = true;
    }
}
