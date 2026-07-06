using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/icons.dart (subset used by current framework samples/tests)

public static class Icons
{
    public const string MaterialIconsFontFamily = "avares://Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf#Material Icons";

    public static IconData ArrowBack { get; } = new(
        0xe092,
        FontFamily: MaterialIconsFontFamily,
        MatchTextDirection: true);

    public static IconData ArrowBackIosNewRounded { get; } = new(
        0xf570,
        FontFamily: MaterialIconsFontFamily,
        MatchTextDirection: true);

    public static IconData ArrowDropDown { get; } = new(0xe098, FontFamily: MaterialIconsFontFamily);

    public static IconData Add { get; } = new(0xe047, FontFamily: MaterialIconsFontFamily);

    public static IconData Check { get; } = new(0xe156, FontFamily: MaterialIconsFontFamily);

    public static IconData Cancel { get; } = new(0xe139, FontFamily: MaterialIconsFontFamily);

    public static IconData Clear { get; } = new(0xe168, FontFamily: MaterialIconsFontFamily);

    public static IconData Close { get; } = new(0xe16a, FontFamily: MaterialIconsFontFamily);

    public static IconData Done { get; } = new(0xe1f6, FontFamily: MaterialIconsFontFamily);

    public static IconData Edit { get; } = new(0xe3c9, FontFamily: MaterialIconsFontFamily);

    public static IconData ExpandMore { get; } = new(0xe246, FontFamily: MaterialIconsFontFamily);

    public static IconData InfoOutline { get; } = new(0xe33d, FontFamily: MaterialIconsFontFamily);

    public static IconData Menu { get; } = new(0xe3dc, FontFamily: MaterialIconsFontFamily);

    public static IconData MoreHoriz { get; } = new(0xe402, FontFamily: MaterialIconsFontFamily);

    public static IconData MoreVert { get; } = new(0xe404, FontFamily: MaterialIconsFontFamily);

    public static IconData Star { get; } = new(0xe5f9, FontFamily: MaterialIconsFontFamily);

    public static IconData StarOutline { get; } = new(0xe5fd, FontFamily: MaterialIconsFontFamily);
}
