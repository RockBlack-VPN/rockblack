using System.Windows.Media;

namespace v2rayN.Converters;

public class MaterialDesignFonts
{
    public static FontFamily MyFont { get; }

    static MaterialDesignFonts()
    {
        try
        {
            var fontFamily = AppManager.Instance.Config.UiItem.CurrentFontFamily;
            if (fontFamily.IsNotEmpty())
            {
                var fontPath = Utils.GetFontsPath();
                MyFont = new FontFamily(new Uri(@$"file:///{fontPath}\"), $"./#{fontFamily}");
            }
        }
        catch
        {
        }
        // RockBlack UI: system font first (Segoe UI Variable on Windows 11), CJK fallback kept
        MyFont ??= new FontFamily("Segoe UI Variable Text, Segoe UI, Microsoft YaHei");
    }
}
