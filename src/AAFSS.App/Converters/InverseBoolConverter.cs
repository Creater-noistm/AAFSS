using System.Globalization;
using System.Windows.Data;

namespace AAFSS.App.Converters;

/// <summary>
/// Inverts a boolean value. True becomes false, false becomes true.
/// Useful for binding to IsEnabled vs IsReadOnly, or showing/hiding opposite states.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}
