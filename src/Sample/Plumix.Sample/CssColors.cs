namespace Plumix.Sample;

// C#-only infrastructure: no Dart parity source.
//
// The CSS named colors as framework `Plumix.UI.Color`
// values. Demo pages use them for illustrative swatches;
// `GlobalUsings.cs` binds `Colors` to this class, so the Material palette (`Plumix.Material.Colors`)
// stays reachable as `MaterialColors`. Only the names in use are listed.

internal static class CssColors
{
    public static Color AliceBlue { get; } = new Color(0xFFF0F8FF);

    public static Color Aqua { get; } = new Color(0xFF00FFFF);

    public static Color Aquamarine { get; } = new Color(0xFF7FFFD4);

    public static Color Beige { get; } = new Color(0xFFF5F5DC);

    public static Color Bisque { get; } = new Color(0xFFFFE4C4);

    public static Color Black { get; } = new Color(0xFF000000);

    public static Color Blue { get; } = new Color(0xFF0000FF);

    public static Color Brown { get; } = new Color(0xFFA52A2A);

    public static Color CadetBlue { get; } = new Color(0xFF5F9EA0);

    public static Color Coral { get; } = new Color(0xFFFF7F50);

    public static Color CornflowerBlue { get; } = new Color(0xFF6495ED);

    public static Color Crimson { get; } = new Color(0xFFDC143C);

    public static Color Cyan { get; } = new Color(0xFF00FFFF);

    public static Color DarkBlue { get; } = new Color(0xFF00008B);

    public static Color DarkCyan { get; } = new Color(0xFF008B8B);

    public static Color DarkGoldenrod { get; } = new Color(0xFFB8860B);

    public static Color DarkGray { get; } = new Color(0xFFA9A9A9);

    public static Color DarkGreen { get; } = new Color(0xFF006400);

    public static Color DarkMagenta { get; } = new Color(0xFF8B008B);

    public static Color DarkOliveGreen { get; } = new Color(0xFF556B2F);

    public static Color DarkOrange { get; } = new Color(0xFFFF8C00);

    public static Color DarkRed { get; } = new Color(0xFF8B0000);

    public static Color DarkSlateBlue { get; } = new Color(0xFF483D8B);

    public static Color DarkSlateGray { get; } = new Color(0xFF2F4F4F);

    public static Color DeepPink { get; } = new Color(0xFFFF1493);

    public static Color DeepSkyBlue { get; } = new Color(0xFF00BFFF);

    public static Color DimGray { get; } = new Color(0xFF696969);

    public static Color DodgerBlue { get; } = new Color(0xFF1E90FF);

    public static Color ForestGreen { get; } = new Color(0xFF228B22);

    public static Color Fuchsia { get; } = new Color(0xFFFF00FF);

    public static Color Gainsboro { get; } = new Color(0xFFDCDCDC);

    public static Color Gold { get; } = new Color(0xFFFFD700);

    public static Color Goldenrod { get; } = new Color(0xFFDAA520);

    public static Color Gray { get; } = new Color(0xFF808080);

    public static Color Green { get; } = new Color(0xFF008000);

    public static Color Honeydew { get; } = new Color(0xFFF0FFF0);

    public static Color HotPink { get; } = new Color(0xFFFF69B4);

    public static Color IndianRed { get; } = new Color(0xFFCD5C5C);

    public static Color Indigo { get; } = new Color(0xFF4B0082);

    public static Color Ivory { get; } = new Color(0xFFFFFFF0);

    public static Color LightBlue { get; } = new Color(0xFFADD8E6);

    public static Color LightCyan { get; } = new Color(0xFFE0FFFF);

    public static Color LightGoldenrodYellow { get; } = new Color(0xFFFAFAD2);

    public static Color LightGray { get; } = new Color(0xFFD3D3D3);

    public static Color LightGreen { get; } = new Color(0xFF90EE90);

    public static Color Lime { get; } = new Color(0xFF00FF00);

    public static Color LimeGreen { get; } = new Color(0xFF32CD32);

    public static Color Magenta { get; } = new Color(0xFFFF00FF);

    public static Color Maroon { get; } = new Color(0xFF800000);

    public static Color MediumAquamarine { get; } = new Color(0xFF66CDAA);

    public static Color MediumBlue { get; } = new Color(0xFF0000CD);

    public static Color MediumPurple { get; } = new Color(0xFF9370DB);

    public static Color MediumSeaGreen { get; } = new Color(0xFF3CB371);

    public static Color MediumSlateBlue { get; } = new Color(0xFF7B68EE);

    public static Color MediumVioletRed { get; } = new Color(0xFFC71585);

    public static Color MidnightBlue { get; } = new Color(0xFF191970);

    public static Color MistyRose { get; } = new Color(0xFFFFE4E1);

    public static Color Moccasin { get; } = new Color(0xFFFFE4B5);

    public static Color Navy { get; } = new Color(0xFF000080);

    public static Color Olive { get; } = new Color(0xFF808000);

    public static Color Orange { get; } = new Color(0xFFFFA500);

    public static Color OrangeRed { get; } = new Color(0xFFFF4500);

    public static Color Pink { get; } = new Color(0xFFFFC0CB);

    public static Color PowderBlue { get; } = new Color(0xFFB0E0E6);

    public static Color Purple { get; } = new Color(0xFF800080);

    public static Color Red { get; } = new Color(0xFFFF0000);

    public static Color SaddleBrown { get; } = new Color(0xFF8B4513);

    public static Color SeaGreen { get; } = new Color(0xFF2E8B57);

    public static Color Silver { get; } = new Color(0xFFC0C0C0);

    public static Color SkyBlue { get; } = new Color(0xFF87CEEB);

    public static Color SlateBlue { get; } = new Color(0xFF6A5ACD);

    public static Color SlateGray { get; } = new Color(0xFF708090);

    public static Color SteelBlue { get; } = new Color(0xFF4682B4);

    public static Color Teal { get; } = new Color(0xFF008080);


    public static Color Wheat { get; } = new Color(0xFFF5DEB3);

    public static Color White { get; } = new Color(0xFFFFFFFF);

    public static Color WhiteSmoke { get; } = new Color(0xFFF5F5F5);

    public static Color Yellow { get; } = new Color(0xFFFFFF00);

    public static Color YellowGreen { get; } = new Color(0xFF9ACD32);

    /// <summary>A color from <c>#RRGGBB</c> or <c>#AARRGGBB</c> hex notation.</summary>
    public static Color ParseHex(string hex)
    {
        string digits = hex.TrimStart('#');
        uint value = uint.Parse(digits, System.Globalization.NumberStyles.HexNumber);
        return new Color(digits.Length <= 6 ? 0xFF000000 | value : value);
    }
}
