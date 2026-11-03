using System.Windows.Media;

namespace v2rayN.Converters;

/// <summary>RockBlack latency colours: &lt; 300 ms primary, 300–800 ms warning, &gt; 800 ms / timeout danger.</summary>
public class DelayColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var delay = value.ToString().ToInt();

        return delay switch
        {
            <= 0 => RockBlackPalette.GetBrush("RB.Danger", Brushes.IndianRed),
            < 300 => RockBlackPalette.GetBrush("RB.Primary", Brushes.MediumAquamarine),
            <= 800 => RockBlackPalette.GetBrush("RB.Warning", Brushes.Orange),
            _ => RockBlackPalette.GetBrush("RB.Danger", Brushes.IndianRed)
        };
    }

    public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}
