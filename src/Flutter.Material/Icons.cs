using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/icons.dart (subset used by current framework samples/tests)

public static class Icons
{
    public const string MaterialIconsFontFamily = "avares://Flutter.Material/Assets/Fonts/MaterialIcons-Regular.otf#Material Icons";

    public static IconData ArrowBack { get; } = new(
        0xe092,
        FontFamily: MaterialIconsFontFamily,
        MatchTextDirection: true);

    public static IconData Add { get; } = new(0xe047, FontFamily: MaterialIconsFontFamily);

    public static IconData Close { get; } = new(0xe16a, FontFamily: MaterialIconsFontFamily);

    public static IconData InfoOutline { get; } = new(0xe33d, FontFamily: MaterialIconsFontFamily);

    public static IconData Menu { get; } = new(0xe3dc, FontFamily: MaterialIconsFontFamily);

    public static IconData Star { get; } = new(0xe5f9, FontFamily: MaterialIconsFontFamily);

    public static IconData StarOutline { get; } = new(0xe5fd, FontFamily: MaterialIconsFontFamily);
}
