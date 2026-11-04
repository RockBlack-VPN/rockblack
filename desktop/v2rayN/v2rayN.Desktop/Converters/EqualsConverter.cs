using Avalonia.Data.Converters;

namespace v2rayN.Desktop.Converters;

/// <summary>
/// True when <c>value.ToString()</c> equals the converter parameter (one of several values separated
/// by '|'). Used to drive style classes from enums, e.g. <c>Classes.good="{Binding DelayState, Converter=...,ConverterParameter=Good}"</c>.
/// </summary>
public class EqualsConverter : IValueConverter
{
    public static readonly EqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? string.Empty;
        var options = (parameter?.ToString() ?? string.Empty).Split('|');
        return options.Any(o => string.Equals(o, text, StringComparison.OrdinalIgnoreCase));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
