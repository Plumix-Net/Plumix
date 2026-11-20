namespace Plumix.Tests;

// C#-only infrastructure: no Dart parity source.
//
// The CSS named colors (Avalonia's `Avalonia.Media.Colors`) as framework `Plumix.UI.Color`
// values. The test suite uses them as arbitrary probe colors;
// `GlobalUsings.cs` binds `Colors` to this class, so the Material palette (`Plumix.Material.Colors`)
// stays reachable as `MaterialColors`. Only the names in use are listed.

internal static class CssColors
{
    public static Color AliceBlue { get; } = Avalonia.Media.Colors.AliceBlue.ToPlumix();

    public static Color Aqua { get; } = Avalonia.Media.Colors.Aqua.ToPlumix();

    public static Color Aquamarine { get; } = Avalonia.Media.Colors.Aquamarine.ToPlumix();

    public static Color Beige { get; } = Avalonia.Media.Colors.Beige.ToPlumix();

    public static Color Bisque { get; } = Avalonia.Media.Colors.Bisque.ToPlumix();

    public static Color Black { get; } = Avalonia.Media.Colors.Black.ToPlumix();

    public static Color Blue { get; } = Avalonia.Media.Colors.Blue.ToPlumix();

    public static Color Brown { get; } = Avalonia.Media.Colors.Brown.ToPlumix();

    public static Color CadetBlue { get; } = Avalonia.Media.Colors.CadetBlue.ToPlumix();

    public static Color Coral { get; } = Avalonia.Media.Colors.Coral.ToPlumix();

    public static Color CornflowerBlue { get; } = Avalonia.Media.Colors.CornflowerBlue.ToPlumix();

    public static Color Crimson { get; } = Avalonia.Media.Colors.Crimson.ToPlumix();

    public static Color Cyan { get; } = Avalonia.Media.Colors.Cyan.ToPlumix();

    public static Color DarkBlue { get; } = Avalonia.Media.Colors.DarkBlue.ToPlumix();

    public static Color DarkCyan { get; } = Avalonia.Media.Colors.DarkCyan.ToPlumix();

    public static Color DarkGoldenrod { get; } = Avalonia.Media.Colors.DarkGoldenrod.ToPlumix();

    public static Color DarkGray { get; } = Avalonia.Media.Colors.DarkGray.ToPlumix();

    public static Color DarkGreen { get; } = Avalonia.Media.Colors.DarkGreen.ToPlumix();

    public static Color DarkMagenta { get; } = Avalonia.Media.Colors.DarkMagenta.ToPlumix();

    public static Color DarkOliveGreen { get; } = Avalonia.Media.Colors.DarkOliveGreen.ToPlumix();

    public static Color DarkOrange { get; } = Avalonia.Media.Colors.DarkOrange.ToPlumix();

    public static Color DarkRed { get; } = Avalonia.Media.Colors.DarkRed.ToPlumix();

    public static Color DarkSlateBlue { get; } = Avalonia.Media.Colors.DarkSlateBlue.ToPlumix();

    public static Color DarkSlateGray { get; } = Avalonia.Media.Colors.DarkSlateGray.ToPlumix();

    public static Color DeepPink { get; } = Avalonia.Media.Colors.DeepPink.ToPlumix();

    public static Color DeepSkyBlue { get; } = Avalonia.Media.Colors.DeepSkyBlue.ToPlumix();

    public static Color DimGray { get; } = Avalonia.Media.Colors.DimGray.ToPlumix();

    public static Color DodgerBlue { get; } = Avalonia.Media.Colors.DodgerBlue.ToPlumix();

    public static Color ForestGreen { get; } = Avalonia.Media.Colors.ForestGreen.ToPlumix();

    public static Color Fuchsia { get; } = Avalonia.Media.Colors.Fuchsia.ToPlumix();

    public static Color Gainsboro { get; } = Avalonia.Media.Colors.Gainsboro.ToPlumix();

    public static Color Gold { get; } = Avalonia.Media.Colors.Gold.ToPlumix();

    public static Color Goldenrod { get; } = Avalonia.Media.Colors.Goldenrod.ToPlumix();

    public static Color Gray { get; } = Avalonia.Media.Colors.Gray.ToPlumix();

    public static Color Green { get; } = Avalonia.Media.Colors.Green.ToPlumix();

    public static Color Honeydew { get; } = Avalonia.Media.Colors.Honeydew.ToPlumix();

    public static Color HotPink { get; } = Avalonia.Media.Colors.HotPink.ToPlumix();

    public static Color IndianRed { get; } = Avalonia.Media.Colors.IndianRed.ToPlumix();

    public static Color Indigo { get; } = Avalonia.Media.Colors.Indigo.ToPlumix();

    public static Color Ivory { get; } = Avalonia.Media.Colors.Ivory.ToPlumix();

    public static Color LightBlue { get; } = Avalonia.Media.Colors.LightBlue.ToPlumix();

    public static Color LightCyan { get; } = Avalonia.Media.Colors.LightCyan.ToPlumix();

    public static Color LightGoldenrodYellow { get; } = Avalonia.Media.Colors.LightGoldenrodYellow.ToPlumix();

    public static Color LightGray { get; } = Avalonia.Media.Colors.LightGray.ToPlumix();

    public static Color LightGreen { get; } = Avalonia.Media.Colors.LightGreen.ToPlumix();

    public static Color Lime { get; } = Avalonia.Media.Colors.Lime.ToPlumix();

    public static Color LimeGreen { get; } = Avalonia.Media.Colors.LimeGreen.ToPlumix();

    public static Color Magenta { get; } = Avalonia.Media.Colors.Magenta.ToPlumix();

    public static Color Maroon { get; } = Avalonia.Media.Colors.Maroon.ToPlumix();

    public static Color MediumAquamarine { get; } = Avalonia.Media.Colors.MediumAquamarine.ToPlumix();

    public static Color MediumBlue { get; } = Avalonia.Media.Colors.MediumBlue.ToPlumix();

    public static Color MediumPurple { get; } = Avalonia.Media.Colors.MediumPurple.ToPlumix();

    public static Color MediumSeaGreen { get; } = Avalonia.Media.Colors.MediumSeaGreen.ToPlumix();

    public static Color MediumSlateBlue { get; } = Avalonia.Media.Colors.MediumSlateBlue.ToPlumix();

    public static Color MediumVioletRed { get; } = Avalonia.Media.Colors.MediumVioletRed.ToPlumix();

    public static Color MidnightBlue { get; } = Avalonia.Media.Colors.MidnightBlue.ToPlumix();

    public static Color MistyRose { get; } = Avalonia.Media.Colors.MistyRose.ToPlumix();

    public static Color Moccasin { get; } = Avalonia.Media.Colors.Moccasin.ToPlumix();

    public static Color Navy { get; } = Avalonia.Media.Colors.Navy.ToPlumix();

    public static Color Olive { get; } = Avalonia.Media.Colors.Olive.ToPlumix();

    public static Color Orange { get; } = Avalonia.Media.Colors.Orange.ToPlumix();

    public static Color OrangeRed { get; } = Avalonia.Media.Colors.OrangeRed.ToPlumix();

    public static Color Pink { get; } = Avalonia.Media.Colors.Pink.ToPlumix();

    public static Color PowderBlue { get; } = Avalonia.Media.Colors.PowderBlue.ToPlumix();

    public static Color Purple { get; } = Avalonia.Media.Colors.Purple.ToPlumix();

    public static Color Red { get; } = Avalonia.Media.Colors.Red.ToPlumix();

    public static Color SaddleBrown { get; } = Avalonia.Media.Colors.SaddleBrown.ToPlumix();

    public static Color SeaGreen { get; } = Avalonia.Media.Colors.SeaGreen.ToPlumix();

    public static Color Silver { get; } = Avalonia.Media.Colors.Silver.ToPlumix();

    public static Color SkyBlue { get; } = Avalonia.Media.Colors.SkyBlue.ToPlumix();

    public static Color SlateBlue { get; } = Avalonia.Media.Colors.SlateBlue.ToPlumix();

    public static Color SlateGray { get; } = Avalonia.Media.Colors.SlateGray.ToPlumix();

    public static Color SteelBlue { get; } = Avalonia.Media.Colors.SteelBlue.ToPlumix();

    public static Color Teal { get; } = Avalonia.Media.Colors.Teal.ToPlumix();

    public static Color Transparent { get; } = Avalonia.Media.Colors.Transparent.ToPlumix();

    public static Color Wheat { get; } = Avalonia.Media.Colors.Wheat.ToPlumix();

    public static Color White { get; } = Avalonia.Media.Colors.White.ToPlumix();

    public static Color WhiteSmoke { get; } = Avalonia.Media.Colors.WhiteSmoke.ToPlumix();

    public static Color Yellow { get; } = Avalonia.Media.Colors.Yellow.ToPlumix();

    public static Color YellowGreen { get; } = Avalonia.Media.Colors.YellowGreen.ToPlumix();
}
