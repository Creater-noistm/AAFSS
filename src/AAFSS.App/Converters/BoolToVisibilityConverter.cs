using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AAFSS.App.Converters;

/// <summary>
/// Converts a boolean value to Visibility.Visible (true) or Visibility.Collapsed (false).
/// Supports Invert parameter to reverse the logic.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is true;
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            var result = visibility == Visibility.Visible;
            return invert ? !result : result;
        }
        return false;
    }
}
