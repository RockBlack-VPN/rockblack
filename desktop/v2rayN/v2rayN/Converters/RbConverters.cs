using System.Windows.Media;

namespace v2rayN.Converters;

/// <summary>bool → Visibility; ConverterParameter "invert" flips the value.</summary>
public class RbBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is true;
        if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
        {
            b = !b;
        }
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>Visible when value.ToString() equals the ConverterParameter (enum states such as the sort mode).</summary>
public class RbEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>Latency state → RockBlack colour: Good primary, Warning warning, Bad/Timeout danger, otherwise muted.</summary>
public class RbDelayStateBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value?.ToString() ?? string.Empty) switch
        {
            "Good" => RockBlackPalette.GetBrush("RB.Primary", Brushes.MediumAquamarine),
            "Warning" => RockBlackPalette.GetBrush("RB.Warning", Brushes.Orange),
            "Bad" or "Timeout" => RockBlackPalette.GetBrush("RB.Danger", Brushes.IndianRed),
            _ => RockBlackPalette.GetBrush("RB.TextMuted", Brushes.Gray),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>Percent (0..100) → star GridLength; ConverterParameter "rest" returns the remaining part.</summary>
public class RbPercentToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var p = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => 0d
        };
        if (double.IsNaN(p))
        {
            p = 0;
        }
        p = Math.Clamp(p, 0, 100);
        if (parameter is string s && s == "rest")
        {
            p = 100 - p;
        }
        return new GridLength(p, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
