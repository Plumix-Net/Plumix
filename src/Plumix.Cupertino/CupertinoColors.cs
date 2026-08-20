using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/colors.dart

/// <summary>A palette of iOS system colors, matching Apple's Human Interface Guidelines.</summary>
public static class CupertinoColors
{
    public static CupertinoDynamicColor ActiveBlue => SystemBlue;

    public static CupertinoDynamicColor ActiveGreen => SystemGreen;

    public static CupertinoDynamicColor ActiveOrange => SystemOrange;

    public static Color White { get; } = Color.FromUInt32(0xFFFFFFFF);

    public static Color Black { get; } = Color.FromUInt32(0xFF000000);

    public static Color Transparent { get; } = Color.FromUInt32(0x00000000);

    public static Color LightBackgroundGray { get; } = Color.FromUInt32(0xFFE5E5EA);

    public static Color ExtraLightBackgroundGray { get; } = Color.FromUInt32(0xFFEFEFF4);

    public static Color DarkBackgroundGray { get; } = Color.FromUInt32(0xFF171717);

    public static CupertinoDynamicColor InactiveGray { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0xFF999999),
        Color.FromUInt32(0xFF757575),
        debugLabel: "inactiveGray");

    public static CupertinoDynamicColor DestructiveRed => SystemRed;

    public static CupertinoDynamicColor SystemBlue { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 10, 132, 255),
        Color.FromArgb(255, 0, 64, 221),
        Color.FromArgb(255, 64, 156, 255),
        debugLabel: "systemBlue");

    public static CupertinoDynamicColor SystemGreen { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 52, 199, 89),
        Color.FromArgb(255, 48, 209, 88),
        Color.FromArgb(255, 36, 138, 61),
        Color.FromArgb(255, 48, 219, 91),
        debugLabel: "systemGreen");

    public static CupertinoDynamicColor SystemMint { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 0, 199, 190),
        Color.FromArgb(255, 99, 230, 226),
        Color.FromArgb(255, 12, 129, 123),
        Color.FromArgb(255, 102, 212, 207),
        debugLabel: "systemMint");

    public static CupertinoDynamicColor SystemIndigo { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 88, 86, 214),
        Color.FromArgb(255, 94, 92, 230),
        Color.FromArgb(255, 54, 52, 163),
        Color.FromArgb(255, 125, 122, 255),
        debugLabel: "systemIndigo");

    public static CupertinoDynamicColor SystemOrange { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 255, 149, 0),
        Color.FromArgb(255, 255, 159, 10),
        Color.FromArgb(255, 201, 52, 0),
        Color.FromArgb(255, 255, 179, 64),
        debugLabel: "systemOrange");

    public static CupertinoDynamicColor SystemPink { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 255, 45, 85),
        Color.FromArgb(255, 255, 55, 95),
        Color.FromArgb(255, 211, 15, 69),
        Color.FromArgb(255, 255, 100, 130),
        debugLabel: "systemPink");

    public static CupertinoDynamicColor SystemBrown { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 162, 132, 94),
        Color.FromArgb(255, 172, 142, 104),
        Color.FromArgb(255, 127, 101, 69),
        Color.FromArgb(255, 181, 148, 105),
        debugLabel: "systemBrown");

    public static CupertinoDynamicColor SystemPurple { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 175, 82, 222),
        Color.FromArgb(255, 191, 90, 242),
        Color.FromArgb(255, 137, 68, 171),
        Color.FromArgb(255, 218, 143, 255),
        debugLabel: "systemPurple");

    public static CupertinoDynamicColor SystemRed { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 255, 59, 48),
        Color.FromArgb(255, 255, 69, 58),
        Color.FromArgb(255, 215, 0, 21),
        Color.FromArgb(255, 255, 105, 97),
        debugLabel: "systemRed");

    public static CupertinoDynamicColor SystemTeal { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 90, 200, 250),
        Color.FromArgb(255, 100, 210, 255),
        Color.FromArgb(255, 0, 113, 164),
        Color.FromArgb(255, 112, 215, 255),
        debugLabel: "systemTeal");

    public static CupertinoDynamicColor SystemCyan { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 50, 173, 230),
        Color.FromArgb(255, 100, 210, 255),
        Color.FromArgb(255, 0, 113, 164),
        Color.FromArgb(255, 112, 215, 255),
        debugLabel: "systemCyan");

    public static CupertinoDynamicColor SystemYellow { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 255, 204, 0),
        Color.FromArgb(255, 255, 214, 10),
        Color.FromArgb(255, 160, 90, 0),
        Color.FromArgb(255, 255, 212, 38),
        debugLabel: "systemYellow");

    public static CupertinoDynamicColor SystemGrey { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 142, 142, 147),
        Color.FromArgb(255, 142, 142, 147),
        Color.FromArgb(255, 108, 108, 112),
        Color.FromArgb(255, 174, 174, 178),
        debugLabel: "systemGrey");

    public static CupertinoDynamicColor SystemGrey2 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 174, 174, 178),
        Color.FromArgb(255, 99, 99, 102),
        Color.FromArgb(255, 142, 142, 147),
        Color.FromArgb(255, 124, 124, 128),
        debugLabel: "systemGrey2");

    public static CupertinoDynamicColor SystemGrey3 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 199, 199, 204),
        Color.FromArgb(255, 72, 72, 74),
        Color.FromArgb(255, 174, 174, 178),
        Color.FromArgb(255, 84, 84, 86),
        debugLabel: "systemGrey3");

    public static CupertinoDynamicColor SystemGrey4 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 209, 209, 214),
        Color.FromArgb(255, 58, 58, 60),
        Color.FromArgb(255, 188, 188, 192),
        Color.FromArgb(255, 68, 68, 70),
        debugLabel: "systemGrey4");

    public static CupertinoDynamicColor SystemGrey5 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 229, 229, 234),
        Color.FromArgb(255, 44, 44, 46),
        Color.FromArgb(255, 216, 216, 220),
        Color.FromArgb(255, 54, 54, 56),
        debugLabel: "systemGrey5");

    public static CupertinoDynamicColor SystemGrey6 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 28, 28, 30),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 36, 36, 38),
        debugLabel: "systemGrey6");

    public static CupertinoDynamicColor Label { get; } = new(
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        debugLabel: "label");

    public static CupertinoDynamicColor SecondaryLabel { get; } = new(
        Color.FromArgb(153, 60, 60, 67),
        Color.FromArgb(153, 235, 235, 245),
        Color.FromArgb(173, 60, 60, 67),
        Color.FromArgb(173, 235, 235, 245),
        Color.FromArgb(153, 60, 60, 67),
        Color.FromArgb(153, 235, 235, 245),
        Color.FromArgb(173, 60, 60, 67),
        Color.FromArgb(173, 235, 235, 245),
        debugLabel: "secondaryLabel");

    public static CupertinoDynamicColor TertiaryLabel { get; } = new(
        Color.FromArgb(76, 60, 60, 67),
        Color.FromArgb(76, 235, 235, 245),
        Color.FromArgb(96, 60, 60, 67),
        Color.FromArgb(96, 235, 235, 245),
        Color.FromArgb(76, 60, 60, 67),
        Color.FromArgb(76, 235, 235, 245),
        Color.FromArgb(96, 60, 60, 67),
        Color.FromArgb(96, 235, 235, 245),
        debugLabel: "tertiaryLabel");

    public static CupertinoDynamicColor QuaternaryLabel { get; } = new(
        Color.FromArgb(45, 60, 60, 67),
        Color.FromArgb(40, 235, 235, 245),
        Color.FromArgb(66, 60, 60, 67),
        Color.FromArgb(61, 235, 235, 245),
        Color.FromArgb(45, 60, 60, 67),
        Color.FromArgb(40, 235, 235, 245),
        Color.FromArgb(66, 60, 60, 67),
        Color.FromArgb(61, 235, 235, 245),
        debugLabel: "quaternaryLabel");

    public static CupertinoDynamicColor SystemFill { get; } = new(
        Color.FromArgb(51, 120, 120, 128),
        Color.FromArgb(91, 120, 120, 128),
        Color.FromArgb(71, 120, 120, 128),
        Color.FromArgb(112, 120, 120, 128),
        Color.FromArgb(51, 120, 120, 128),
        Color.FromArgb(91, 120, 120, 128),
        Color.FromArgb(71, 120, 120, 128),
        Color.FromArgb(112, 120, 120, 128),
        debugLabel: "systemFill");

    public static CupertinoDynamicColor SecondarySystemFill { get; } = new(
        Color.FromArgb(40, 120, 120, 128),
        Color.FromArgb(81, 120, 120, 128),
        Color.FromArgb(61, 120, 120, 128),
        Color.FromArgb(102, 120, 120, 128),
        Color.FromArgb(40, 120, 120, 128),
        Color.FromArgb(81, 120, 120, 128),
        Color.FromArgb(61, 120, 120, 128),
        Color.FromArgb(102, 120, 120, 128),
        debugLabel: "secondarySystemFill");

    public static CupertinoDynamicColor TertiarySystemFill { get; } = new(
        Color.FromArgb(30, 118, 118, 128),
        Color.FromArgb(61, 118, 118, 128),
        Color.FromArgb(51, 118, 118, 128),
        Color.FromArgb(81, 118, 118, 128),
        Color.FromArgb(30, 118, 118, 128),
        Color.FromArgb(61, 118, 118, 128),
        Color.FromArgb(51, 118, 118, 128),
        Color.FromArgb(81, 118, 118, 128),
        debugLabel: "tertiarySystemFill");

    public static CupertinoDynamicColor QuaternarySystemFill { get; } = new(
        Color.FromArgb(20, 116, 116, 128),
        Color.FromArgb(45, 118, 118, 128),
        Color.FromArgb(40, 116, 116, 128),
        Color.FromArgb(66, 118, 118, 128),
        Color.FromArgb(20, 116, 116, 128),
        Color.FromArgb(45, 118, 118, 128),
        Color.FromArgb(40, 116, 116, 128),
        Color.FromArgb(66, 118, 118, 128),
        debugLabel: "quaternarySystemFill");

    public static CupertinoDynamicColor PlaceholderText { get; } = new(
        Color.FromArgb(76, 60, 60, 67),
        Color.FromArgb(76, 235, 235, 245),
        Color.FromArgb(96, 60, 60, 67),
        Color.FromArgb(96, 235, 235, 245),
        Color.FromArgb(76, 60, 60, 67),
        Color.FromArgb(76, 235, 235, 245),
        Color.FromArgb(96, 60, 60, 67),
        Color.FromArgb(96, 235, 235, 245),
        debugLabel: "placeholderText");

    public static CupertinoDynamicColor SystemBackground { get; } = new(
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 28, 28, 30),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 36, 36, 38),
        debugLabel: "systemBackground");

    public static CupertinoDynamicColor SecondarySystemBackground { get; } = new(
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 28, 28, 30),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 36, 36, 38),
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 44, 44, 46),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 54, 54, 56),
        debugLabel: "secondarySystemBackground");

    public static CupertinoDynamicColor TertiarySystemBackground { get; } = new(
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 44, 44, 46),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 54, 54, 56),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 58, 58, 60),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 68, 68, 70),
        debugLabel: "tertiarySystemBackground");

    public static CupertinoDynamicColor SystemGroupedBackground { get; } = new(
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 28, 28, 30),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 36, 36, 38),
        debugLabel: "systemGroupedBackground");

    public static CupertinoDynamicColor SecondarySystemGroupedBackground { get; } = new(
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 28, 28, 30),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 36, 36, 38),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 44, 44, 46),
        Color.FromArgb(255, 255, 255, 255),
        Color.FromArgb(255, 54, 54, 56),
        debugLabel: "secondarySystemGroupedBackground");

    public static CupertinoDynamicColor TertiarySystemGroupedBackground { get; } = new(
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 44, 44, 46),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 54, 54, 56),
        Color.FromArgb(255, 242, 242, 247),
        Color.FromArgb(255, 58, 58, 60),
        Color.FromArgb(255, 235, 235, 240),
        Color.FromArgb(255, 68, 68, 70),
        debugLabel: "tertiarySystemGroupedBackground");

    public static CupertinoDynamicColor Separator { get; } = new(
        Color.FromArgb(73, 60, 60, 67),
        Color.FromArgb(153, 84, 84, 88),
        Color.FromArgb(94, 60, 60, 67),
        Color.FromArgb(173, 84, 84, 88),
        Color.FromArgb(73, 60, 60, 67),
        Color.FromArgb(153, 210, 210, 210),
        Color.FromArgb(94, 60, 60, 67),
        Color.FromArgb(173, 84, 84, 88),
        debugLabel: "separator");

    public static CupertinoDynamicColor OpaqueSeparator { get; } = new(
        Color.FromArgb(255, 198, 198, 200),
        Color.FromArgb(255, 56, 56, 58),
        Color.FromArgb(255, 198, 198, 200),
        Color.FromArgb(255, 56, 56, 58),
        Color.FromArgb(255, 198, 198, 200),
        Color.FromArgb(255, 56, 56, 58),
        Color.FromArgb(255, 198, 198, 200),
        Color.FromArgb(255, 56, 56, 58),
        debugLabel: "opaqueSeparator");

    public static CupertinoDynamicColor Link { get; } = new(
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 9, 132, 255),
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 9, 132, 255),
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 9, 132, 255),
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 9, 132, 255),
        debugLabel: "link");
}

/// <summary>
/// A color that changes with the ambient <see cref="CupertinoTheme"/> brightness, the accessibility
/// high-contrast setting and the <see cref="CupertinoUserInterfaceLevel"/> of its surroundings.
/// </summary>
/// <remarks>
/// Dart's <c>CupertinoDynamicColor</c> implements <c>Color</c>; Avalonia's <c>Color</c> is a struct
/// and cannot be subclassed, so the polymorphism is replaced by two implicit conversions: a plain
/// <see cref="Avalonia.Media.Color"/> converts to a dynamic color whose eight variants are all that
/// color (so resolving it is a no-op, exactly like Dart's non-dynamic branch), and a dynamic color
/// converts to its currently effective <see cref="Value"/>.
/// </remarks>
public sealed class CupertinoDynamicColor
{
    private readonly Color _effectiveColor;
    private readonly string? _debugLabel;
    private readonly bool _isResolved;

    public CupertinoDynamicColor(
        Color color,
        Color darkColor,
        Color highContrastColor,
        Color darkHighContrastColor,
        Color elevatedColor,
        Color darkElevatedColor,
        Color highContrastElevatedColor,
        Color darkHighContrastElevatedColor,
        string? debugLabel = null)
        : this(
            color,
            color,
            darkColor,
            highContrastColor,
            darkHighContrastColor,
            elevatedColor,
            darkElevatedColor,
            highContrastElevatedColor,
            darkHighContrastElevatedColor,
            isResolved: false,
            debugLabel)
    {
    }

    private CupertinoDynamicColor(
        Color effectiveColor,
        Color color,
        Color darkColor,
        Color highContrastColor,
        Color darkHighContrastColor,
        Color elevatedColor,
        Color darkElevatedColor,
        Color highContrastElevatedColor,
        Color darkHighContrastElevatedColor,
        bool isResolved,
        string? debugLabel)
    {
        _effectiveColor = effectiveColor;
        _isResolved = isResolved;
        _debugLabel = debugLabel;
        Color = color;
        DarkColor = darkColor;
        HighContrastColor = highContrastColor;
        DarkHighContrastColor = darkHighContrastColor;
        ElevatedColor = elevatedColor;
        DarkElevatedColor = darkElevatedColor;
        HighContrastElevatedColor = highContrastElevatedColor;
        DarkHighContrastElevatedColor = darkHighContrastElevatedColor;
    }

    /// <summary>Creates a color that only varies with brightness and accessibility contrast.</summary>
    public static CupertinoDynamicColor WithBrightnessAndContrast(
        Color color,
        Color darkColor,
        Color highContrastColor,
        Color darkHighContrastColor,
        string? debugLabel = null)
    {
        return new CupertinoDynamicColor(
            color,
            darkColor,
            highContrastColor,
            darkHighContrastColor,
            color,
            darkColor,
            highContrastColor,
            darkHighContrastColor,
            debugLabel);
    }

    /// <summary>Creates a color that only varies with brightness.</summary>
    public static CupertinoDynamicColor WithBrightness(
        Color color,
        Color darkColor,
        string? debugLabel = null)
    {
        return new CupertinoDynamicColor(
            color,
            darkColor,
            color,
            darkColor,
            color,
            darkColor,
            color,
            darkColor,
            debugLabel);
    }

    /// <summary>The color to use in light mode, normal contrast, base interface elevation.</summary>
    public Color Color { get; }

    /// <summary>The color to use in dark mode, normal contrast, base interface elevation.</summary>
    public Color DarkColor { get; }

    /// <summary>The color to use in light mode, high contrast, base interface elevation.</summary>
    public Color HighContrastColor { get; }

    /// <summary>The color to use in dark mode, high contrast, base interface elevation.</summary>
    public Color DarkHighContrastColor { get; }

    /// <summary>The color to use in light mode, normal contrast, elevated interface elevation.</summary>
    public Color ElevatedColor { get; }

    /// <summary>The color to use in dark mode, normal contrast, elevated interface elevation.</summary>
    public Color DarkElevatedColor { get; }

    /// <summary>The color to use in light mode, high contrast, elevated interface elevation.</summary>
    public Color HighContrastElevatedColor { get; }

    /// <summary>The color to use in dark mode, high contrast, elevated interface elevation.</summary>
    public Color DarkHighContrastElevatedColor { get; }

    /// <summary>
    /// The variant this color currently stands for. Dart's <c>CupertinoDynamicColor</c> exposes this
    /// through the <c>Color</c> interface it implements (<c>toARGB32</c> and friends).
    /// </summary>
    public Color Value => _effectiveColor;

    private bool IsPlatformBrightnessDependent =>
        Color != DarkColor
        || ElevatedColor != DarkElevatedColor
        || HighContrastColor != DarkHighContrastColor
        || HighContrastElevatedColor != DarkHighContrastElevatedColor;

    private bool IsHighContrastDependent =>
        Color != HighContrastColor
        || DarkColor != DarkHighContrastColor
        || ElevatedColor != HighContrastElevatedColor
        || DarkElevatedColor != DarkHighContrastElevatedColor;

    private bool IsInterfaceElevationDependent =>
        Color != ElevatedColor
        || DarkColor != DarkElevatedColor
        || HighContrastColor != HighContrastElevatedColor
        || DarkHighContrastColor != DarkHighContrastElevatedColor;

    /// <summary>Resolves <paramref name="resolvable"/> against <paramref name="context"/>.</summary>
    public static Color Resolve(CupertinoDynamicColor resolvable, BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(resolvable);
        return resolvable.ResolveFrom(context).Value;
    }

    /// <summary>The null-tolerant form of <see cref="Resolve"/>.</summary>
    public static Color? MaybeResolve(CupertinoDynamicColor? resolvable, BuildContext context)
    {
        return resolvable?.ResolveFrom(context).Value;
    }

    /// <summary>
    /// Resolves every variant-selecting dependency and returns a color whose <see cref="Value"/> is
    /// the selected variant. Only the dependencies this color actually varies with are read, so a
    /// color that does not depend on, say, elevation never subscribes to
    /// <see cref="CupertinoUserInterfaceLevel"/>.
    /// </summary>
    public CupertinoDynamicColor ResolveFrom(BuildContext context)
    {
        PlatformBrightness brightness = IsPlatformBrightnessDependent
            ? CupertinoTheme.MaybeBrightnessOf(context) ?? PlatformBrightness.Light
            : PlatformBrightness.Light;
        CupertinoUserInterfaceLevelData level = IsInterfaceElevationDependent
            ? CupertinoUserInterfaceLevel.MaybeOf(context) ?? CupertinoUserInterfaceLevelData.Base
            : CupertinoUserInterfaceLevelData.Base;
        bool highContrast = IsHighContrastDependent && (MediaQuery.MaybeHighContrastOf(context) ?? false);
        Color resolved = (brightness, level, highContrast) switch
        {
            (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Base, false) => Color,
            (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Base, true) => HighContrastColor,
            (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Elevated, false) => ElevatedColor,
            (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Elevated, true) =>
                HighContrastElevatedColor,
            (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Base, false) => DarkColor,
            (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Base, true) => DarkHighContrastColor,
            (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Elevated, false) => DarkElevatedColor,
            _ => DarkHighContrastElevatedColor,
        };

        return new CupertinoDynamicColor(
            resolved,
            Color,
            DarkColor,
            HighContrastColor,
            DarkHighContrastColor,
            ElevatedColor,
            DarkElevatedColor,
            HighContrastElevatedColor,
            DarkHighContrastElevatedColor,
            isResolved: true,
            _debugLabel);
    }

    public static implicit operator Color(CupertinoDynamicColor color)
    {
        ArgumentNullException.ThrowIfNull(color);
        return color.Value;
    }

    public static implicit operator WidgetStateColor(CupertinoDynamicColor color)
    {
        ArgumentNullException.ThrowIfNull(color);
        return new CupertinoDynamicWidgetStateColor(color);
    }

    public static implicit operator CupertinoDynamicColor(Color color)
    {
        return new CupertinoDynamicColor(color, color, color, color, color, color, color, color);
    }

    public static bool operator ==(CupertinoDynamicColor? left, CupertinoDynamicColor? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(CupertinoDynamicColor? left, CupertinoDynamicColor? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is CupertinoDynamicColor other
               && other._effectiveColor == _effectiveColor
               && other.Color == Color
               && other.DarkColor == DarkColor
               && other.HighContrastColor == HighContrastColor
               && other.DarkHighContrastColor == DarkHighContrastColor
               && other.ElevatedColor == ElevatedColor
               && other.DarkElevatedColor == DarkElevatedColor
               && other.HighContrastElevatedColor == HighContrastElevatedColor
               && other.DarkHighContrastElevatedColor == DarkHighContrastElevatedColor;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_effectiveColor);
        hash.Add(Color);
        hash.Add(DarkColor);
        hash.Add(HighContrastColor);
        hash.Add(ElevatedColor);
        hash.Add(DarkElevatedColor);
        hash.Add(DarkHighContrastColor);
        hash.Add(DarkHighContrastElevatedColor);
        hash.Add(HighContrastElevatedColor);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        string Describe(string name, Color color)
        {
            string marker = color == _effectiveColor ? "*" : string.Empty;
            return $"{marker}{name} = {color}{marker}";
        }

        var parts = new List<string> { Describe("color", Color) };
        if (IsPlatformBrightnessDependent)
        {
            parts.Add(Describe("darkColor", DarkColor));
        }

        if (IsHighContrastDependent)
        {
            parts.Add(Describe("highContrastColor", HighContrastColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent)
        {
            parts.Add(Describe("darkHighContrastColor", DarkHighContrastColor));
        }

        if (IsInterfaceElevationDependent)
        {
            parts.Add(Describe("elevatedColor", ElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsInterfaceElevationDependent)
        {
            parts.Add(Describe("darkElevatedColor", DarkElevatedColor));
        }

        if (IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            parts.Add(Describe("highContrastElevatedColor", HighContrastElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            parts.Add(Describe("darkHighContrastElevatedColor", DarkHighContrastElevatedColor));
        }

        string label = _debugLabel ?? nameof(CupertinoDynamicColor);
        string resolvedBy = _isResolved ? "resolved" : "UNRESOLVED";
        return $"{label}({string.Join(", ", parts)}, resolved by: {resolvedBy})";
    }
}

internal sealed class CupertinoDynamicWidgetStateColor : WidgetStateColor
{
    public CupertinoDynamicWidgetStateColor(CupertinoDynamicColor dynamicColor)
        : base(dynamicColor.Value)
    {
        DynamicColor = dynamicColor;
    }

    public CupertinoDynamicColor DynamicColor { get; }
}
