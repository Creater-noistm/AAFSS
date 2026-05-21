using System.Globalization;
using System.Windows.Data;

namespace AAFSS.App.Converters;

/// <summary>
/// Converts an enum value to its display-friendly string representation.
/// Uses the enum's ToString() by default, or can look up resource keys.
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public class EnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            return enumValue.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && targetType.IsEnum)
        {
            return Enum.Parse(targetType, str);
        }
        return Binding.DoNothing;
    }
}
