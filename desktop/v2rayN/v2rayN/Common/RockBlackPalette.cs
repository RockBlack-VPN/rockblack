using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace v2rayN.Common;

/// <summary>
/// RockBlack UI 2.0 colour tokens (branding/DESIGN.md) applied on top of the MaterialDesign theme.
/// Presentation only: the stored theme setting keeps its upstream meaning; an empty value means dark.
/// </summary>
internal static class RockBlackPalette
{
    private sealed record Tokens(
        Color Bg, Color Surface, Color Surface2, Color Outline,
        Color Primary, Color OnPrimary, Color PrimarySoft,
        Color Text, Color TextMuted, Color Danger, Color Warning);

    private static readonly Tokens Dark = new(
        C("#0F1011"), C("#18191B"), C("#212326"), C("#2D3034"),
        C("#26D6B0"), C("#04231C"), C("#2426D6B0"),
        C("#F1F3F4"), C("#8E959C"), C("#FF5C6C"), C("#FFB547"));

    private static readonly Tokens Light = new(
        C("#F3F5F6"), C("#FFFFFF"), C("#EEF1F2"), C("#DCE1E4"),
        C("#0FA383"), C("#FFFFFF"), C("#1F0FA383"),
        C("#14171A"), C("#5E666D"), C("#D93848"), C("#B87400"));

    public static bool IsDark(string? theme)
    {
        return theme switch
        {
            nameof(ETheme.Light) => false,
            nameof(ETheme.FollowSystem) => WindowsUtils.IsSystemDarkTheme(),
            _ => true, // Dark and "not set yet": RockBlack is dark by default
        };
    }

    /// <summary>Overrides the MaterialDesign surfaces/outlines; keepPrimary=false also applies the teal accent.</summary>
    public static void Apply(Theme theme, bool dark, bool applyPrimary)
    {
        var t = dark ? Dark : Light;

        if (applyPrimary)
        {
            theme.PrimaryLight = new ColorPair(t.Primary, t.OnPrimary);
            theme.PrimaryMid = new ColorPair(t.Primary, t.OnPrimary);
            theme.PrimaryDark = new ColorPair(t.Primary, t.OnPrimary);
        }
        theme.SecondaryLight = new ColorPair(t.Primary, t.OnPrimary);
        theme.SecondaryMid = new ColorPair(t.Primary, t.OnPrimary);
        theme.SecondaryDark = new ColorPair(t.Primary, t.OnPrimary);

        theme.Background = t.Surface;
        theme.Foreground = t.Text;
        theme.ForegroundLight = t.TextMuted;
        theme.ValidationError = t.Danger;

        theme.Cards.Background = t.Surface;
        theme.Cards.Border = t.Outline;
        theme.Chips.Background = t.Surface2;
        theme.Chips.OutlineBorder = t.Outline;
        theme.ColorZones.DarkBackground = t.Surface;
        theme.ColorZones.DarkForeground = t.Text;
        theme.ColorZones.LightBackground = t.Surface;
        theme.ColorZones.LightForeground = t.Text;

        theme.ComboBoxes.Border = t.Outline;
        theme.ComboBoxes.OutlineBorder = t.Outline;
        theme.ComboBoxes.OutlineInactiveBorder = t.Outline;
        theme.ComboBoxes.HoverBorder = t.Primary;
        theme.ComboBoxes.FilledBackground = t.Surface2;
        theme.ComboBoxes.HoverBackground = t.Surface2;
        theme.ComboBoxes.DropDowns.Background = t.Surface;
        theme.ComboBoxes.Popups.DarkBackground = t.Surface;
        theme.ComboBoxes.Popups.LightBackground = t.Surface;

        theme.TextBoxes.Border = t.Outline;
        theme.TextBoxes.OutlineBorder = t.Outline;
        theme.TextBoxes.OutlineInactiveBorder = t.Outline;
        theme.TextBoxes.HoverBorder = t.Primary;
        theme.TextBoxes.FilledBackground = t.Surface2;
        theme.TextBoxes.HoverBackground = t.Surface2;
        theme.PasswordBoxes.Border = t.Outline;
        theme.PasswordBoxes.OutlineBorder = t.Outline;
        theme.PasswordBoxes.OutlineInactiveBorder = t.Outline;
        theme.PasswordBoxes.HoverBorder = t.Primary;
        theme.PasswordBoxes.FilledBackground = t.Surface2;

        theme.DataGrids.Border = Colors.Transparent;
        theme.DataGrids.RowHoverBackground = t.Surface2;
        theme.DataGrids.Selected = t.PrimarySoft;
        theme.DataGrids.ColumnHeaderForeground = t.TextMuted;
        theme.ListViews.Hover = t.Surface2;
        theme.ListViews.Selected = t.PrimarySoft;
        theme.ListViews.Separator = Colors.Transparent;
        theme.ListBoxItems.Selected = t.PrimarySoft;
        theme.ListBoxItems.Border = t.Outline;

        theme.Separators.Background = t.Outline;
        theme.TabControls.Divider = t.Outline;
        theme.GridSplitters.Background = Colors.Transparent;
        theme.GridSplitters.PreviewBackground = t.Outline;
        theme.ToolBars.Background = t.Surface;
        theme.ToolBars.Separator = t.Outline;
        theme.ToolBars.Items.Background = t.Surface;
        theme.ToolBars.Items.Foreground = t.Text;
        theme.StatusBars.Background = t.Surface;
        theme.StatusBars.Foreground = t.TextMuted;
        theme.SnackBars.Background = t.Surface2;
        theme.ToolTips.Background = t.Surface2;
        theme.ScrollBars.Foreground = t.Outline;
        theme.ToggleButtons.Switches.TrackOffBackground = t.Outline;
        theme.CheckBoxes.UncheckedBorder = t.TextMuted;
        theme.RadioButtons.Border = t.TextMuted;
        theme.Headers.Foreground = t.Text;
    }

    /// <summary>Updates the RB.* brushes used by the RockBlack templates (DynamicResource).</summary>
    public static void ApplyBrushes(bool dark)
    {
        var t = dark ? Dark : Light;
        var res = Application.Current.Resources;
        res["RB.Bg"] = B(t.Bg);
        res["RB.Surface"] = B(t.Surface);
        res["RB.Surface2"] = B(t.Surface2);
        res["RB.Outline"] = B(t.Outline);
        res["RB.Primary"] = B(t.Primary);
        res["RB.OnPrimary"] = B(t.OnPrimary);
        res["RB.PrimarySoft"] = B(t.PrimarySoft);
        res["RB.Text"] = B(t.Text);
        res["RB.TextMuted"] = B(t.TextMuted);
        res["RB.Danger"] = B(t.Danger);
        res["RB.Warning"] = B(t.Warning);
    }

    public static Brush GetBrush(string key, Brush fallback)
    {
        return Application.Current?.TryFindResource(key) as Brush ?? fallback;
    }

    private static Color C(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }

    private static SolidColorBrush B(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
