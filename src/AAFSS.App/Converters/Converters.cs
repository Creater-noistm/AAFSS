using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AAFSS.Core.Models;

namespace AAFSS.App.Converters;

/// <summary>
/// Converts boolean to Visibility (true → Visible, false → Collapsed).
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                return b ? Visibility.Collapsed : Visibility.Visible;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                return v != Visibility.Visible;
            return v == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts boolean to inverse boolean.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>
/// Converts null to boolean (null → false, non-null → true).
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts null to Visibility (null → Collapsed, non-null → Visible).
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts ProcessingStatus enum to a Brush color.
/// </summary>
[ValueConversion(typeof(ProcessingStatus), typeof(Brush))]
public class StatusToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush PendingBrush = new(Color.FromRgb(0x90, 0x90, 0x90));
    private static readonly SolidColorBrush RunningBrush = new(Color.FromRgb(0x03, 0xA9, 0xF4));
    private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(0xF4, 0x43, 0x36));
    private static readonly SolidColorBrush CancelledBrush = new(Color.FromRgb(0xFF, 0x98, 0x00));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0x75, 0x75, 0x75));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ProcessingStatus status => status switch
            {
                ProcessingStatus.Pending => PendingBrush,
                ProcessingStatus.Running => RunningBrush,
                ProcessingStatus.Completed => CompletedBrush,
                ProcessingStatus.Failed => FailedBrush,
                ProcessingStatus.Cancelled => CancelledBrush,
                _ => DefaultBrush
            },
            ValidationLevel level => level switch
            {
                ValidationLevel.Green => CompletedBrush,
                ValidationLevel.Yellow => new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B)),
                ValidationLevel.Red => FailedBrush,
                ValidationLevel.NotValidated => DefaultBrush,
                _ => DefaultBrush
            },
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts ProcessingStatus to a text label string.
/// </summary>
[ValueConversion(typeof(ProcessingStatus), typeof(string))]
public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ProcessingStatus.Pending => "待处理",
            ProcessingStatus.Running => "运行中",
            ProcessingStatus.Completed => "已完成",
            ProcessingStatus.Failed => "失败",
            ProcessingStatus.Cancelled => "已取消",
            ProcessingStatus.Skipped => "已跳过",
            _ => "未知"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an Enum to a display-friendly string.
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public class EnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        var type = value.GetType();
        if (!type.IsEnum) return value.ToString()!;

        return type.Name switch
        {
            nameof(FlightPhase) => value switch
            {
                FlightPhase.Takeoff => "起飞",
                FlightPhase.Climb => "爬升",
                FlightPhase.Cruise => "巡航",
                FlightPhase.Descent => "下降",
                FlightPhase.Landing => "着陆",
                FlightPhase.Maneuver => "机动",
                FlightPhase.Taxi => "滑行",
                FlightPhase.Afterburner => "加力",
                _ => value.ToString()!
            },
            nameof(SpectrumType) => value switch
            {
                SpectrumType.Octave1_1 => "1/1 倍频程",
                SpectrumType.Octave1_3 => "1/3 倍频程",
                SpectrumType.Octave1_6 => "1/6 倍频程",
                SpectrumType.Octave1_12 => "1/12 倍频程",
                SpectrumType.PsdWelch => "PSD (Welch)",
                SpectrumType.PsdPeriodogram => "PSD (周期图)",
                SpectrumType.CrossSpectrum => "互谱",
                SpectrumType.Coherence => "相干",
                SpectrumType.ZoomFft => "Zoom FFT",
                _ => value.ToString()!
            },
            nameof(CompilationMethod) => value switch
            {
                CompilationMethod.StateRegionEnvelope => "状态区域包络",
                CompilationMethod.MinerEquivalent => "Miner等效",
                CompilationMethod.FlightByFlight => "逐架次",
                CompilationMethod.MaxEnvelope => "最大包络",
                CompilationMethod.StatisticalExtreme => "统计极值",
                _ => value.ToString()!
            },
            _ => value.ToString()!
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Multiplies a numeric value by a converter parameter.
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), out double factor))
            return d * factor;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), out double factor) && factor != 0)
            return d / factor;
        return value;
    }
}

/// <summary>
/// Formats a file size in bytes to a human-readable string.
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "0 B";
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
