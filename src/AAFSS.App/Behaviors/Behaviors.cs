using System.Windows;
using System.Windows.Input;

namespace AAFSS.App.Behaviors;

/// <summary>
/// Attached behavior for handling drag-and-drop file operations.
/// </summary>
public static class DragDropBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(DragDropBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DragOverCommandProperty =
        DependencyProperty.RegisterAttached("DragOverCommand", typeof(ICommand), typeof(DragDropBehavior),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ICommand GetDropCommand(DependencyObject obj) => (ICommand)obj.GetValue(DropCommandProperty);
    public static void SetDropCommand(DependencyObject obj, ICommand value) => obj.SetValue(DropCommandProperty, value);

    public static ICommand GetDragOverCommand(DependencyObject obj) => (ICommand)obj.GetValue(DragOverCommandProperty);
    public static void SetDragOverCommand(DependencyObject obj, ICommand value) => obj.SetValue(DragOverCommandProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        if ((bool)e.NewValue)
        {
            element.AllowDrop = true;
            element.Drop += OnDrop;
            element.DragOver += OnDragOver;
        }
        else
        {
            element.AllowDrop = false;
            element.Drop -= OnDrop;
            element.DragOver -= OnDragOver;
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject d) return;
        var command = GetDropCommand(d);
        if (command?.CanExecute(e.Data) == true)
            command.Execute(e.Data);
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject d) return;
        var command = GetDragOverCommand(d);
        if (command?.CanExecute(e.Data) == true)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }
}

/// <summary>
/// Attached behavior for handling double-click on UI elements.
/// </summary>
public static class DoubleClickBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(DoubleClickBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(DoubleClickBehavior),
            new PropertyMetadata(null));

    public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);
    public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

    public static object GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);
    public static void SetCommandParameter(DependencyObject obj, object value) => obj.SetValue(CommandParameterProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        if (e.NewValue != null)
            element.MouseDown += OnMouseDown;
        else
            element.MouseDown -= OnMouseDown;
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is DependencyObject d)
        {
            var command = GetCommand(d);
            var parameter = GetCommandParameter(d);
            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
                e.Handled = true;
            }
        }
    }
}

/// <summary>
/// Attached behavior for selecting all text when a TextBox receives focus.
/// </summary>
public static class SelectAllOnFocusBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SelectAllOnFocusBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not System.Windows.Controls.TextBox textBox) return;
        if ((bool)e.NewValue)
            textBox.GotKeyboardFocus += OnGotKeyboardFocus;
        else
            textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
            textBox.SelectAll();
    }
}
