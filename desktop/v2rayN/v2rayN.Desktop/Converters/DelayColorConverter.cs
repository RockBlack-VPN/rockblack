using Avalonia.Data.Converters;

namespace v2rayN.Desktop.Converters;

/// <summary>RockBlack latency colours: &lt; 300 ms primary, 300–800 ms warning, &gt; 800 ms / timeout danger.</summary>
public class DelayColorConverter : IValueConverter
{
    private static readonly IBrush Primary = new SolidColorBrush(Color.Parse("#26D6B0"));
    private static readonly IBrush Warning = new SolidColorBrush(Color.Parse("#FFB547"));
    private static readonly IBrush Danger = new SolidColorBrush(Color.Parse("#FF5C6C"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var delay = value.ToString().ToInt();

        return delay switch
        {
            <= 0 => Danger,
            < 300 => Primary,
            <= 800 => Warning,
            _ => Danger
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
