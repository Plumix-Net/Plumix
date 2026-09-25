using System.Diagnostics.CodeAnalysis;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/colors.dart

/// <summary>A palette of iOS system colors, matching Apple's Human Interface Guidelines.</summary>
public static class CupertinoColors
{
    /// <summary>
    /// Dart's top-level <c>createCupertinoColorProperty</c>: a diagnostics property for a
    /// <see cref="Color"/> that has a special case for <see cref="CupertinoDynamicColor"/>.
    /// </summary>
    public static DiagnosticsNode CreateCupertinoColorProperty(
        string name,
        Color? value,
        bool showName = true,
        object? defaultValue = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
    {
        if (value is CupertinoDynamicColor dynamicColor)
        {
            return new DiagnosticsProperty<CupertinoDynamicColor>(
                name,
                dynamicColor,
                description: dynamicColor.DebugLabel,
                showName: showName,
                defaultValue: defaultValue,
                style: style,
                level: level);
        }

        return new ColorProperty(
            name,
            value,
            showName: showName,
            defaultValue: defaultValue,
            style: style,
            level: level);
    }

    public static CupertinoDynamicColor ActiveBlue => SystemBlue;

    public static CupertinoDynamicColor ActiveGreen => SystemGreen;

    public static CupertinoDynamicColor ActiveOrange => SystemOrange;

    public static Color White { get; } = new Color(0xFFFFFFFF);

    public static Color Black { get; } = new Color(0xFF000000);

    public static Color Transparent { get; } = new Color(0x00000000);

    public static Color LightBackgroundGray { get; } = new Color(0xFFE5E5EA);

    public static Color ExtraLightBackgroundGray { get; } = new Color(0xFFEFEFF4);

    public static Color DarkBackgroundGray { get; } = new Color(0xFF171717);

    public static CupertinoDynamicColor InactiveGray { get; } = CupertinoDynamicColor.WithBrightness(
        new Color(0xFF999999),
        new Color(0xFF757575),
        debugLabel: "inactiveGray");

    public static CupertinoDynamicColor DestructiveRed => SystemRed;

    public static CupertinoDynamicColor SystemBlue { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 0, 122, 255),
        Color.FromARGB(255, 10, 132, 255),
        Color.FromARGB(255, 0, 64, 221),
        Color.FromARGB(255, 64, 156, 255),
        debugLabel: "systemBlue");

    public static CupertinoDynamicColor SystemGreen { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 52, 199, 89),
        Color.FromARGB(255, 48, 209, 88),
        Color.FromARGB(255, 36, 138, 61),
        Color.FromARGB(255, 48, 219, 91),
        debugLabel: "systemGreen");

    public static CupertinoDynamicColor SystemMint { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 0, 199, 190),
        Color.FromARGB(255, 99, 230, 226),
        Color.FromARGB(255, 12, 129, 123),
        Color.FromARGB(255, 102, 212, 207),
        debugLabel: "systemMint");

    public static CupertinoDynamicColor SystemIndigo { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 88, 86, 214),
        Color.FromARGB(255, 94, 92, 230),
        Color.FromARGB(255, 54, 52, 163),
        Color.FromARGB(255, 125, 122, 255),
        debugLabel: "systemIndigo");

    public static CupertinoDynamicColor SystemOrange { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 255, 149, 0),
        Color.FromARGB(255, 255, 159, 10),
        Color.FromARGB(255, 201, 52, 0),
        Color.FromARGB(255, 255, 179, 64),
        debugLabel: "systemOrange");

    public static CupertinoDynamicColor SystemPink { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 255, 45, 85),
        Color.FromARGB(255, 255, 55, 95),
        Color.FromARGB(255, 211, 15, 69),
        Color.FromARGB(255, 255, 100, 130),
        debugLabel: "systemPink");

    public static CupertinoDynamicColor SystemBrown { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 162, 132, 94),
        Color.FromARGB(255, 172, 142, 104),
        Color.FromARGB(255, 127, 101, 69),
        Color.FromARGB(255, 181, 148, 105),
        debugLabel: "systemBrown");

    public static CupertinoDynamicColor SystemPurple { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 175, 82, 222),
        Color.FromARGB(255, 191, 90, 242),
        Color.FromARGB(255, 137, 68, 171),
        Color.FromARGB(255, 218, 143, 255),
        debugLabel: "systemPurple");

    public static CupertinoDynamicColor SystemRed { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 255, 59, 48),
        Color.FromARGB(255, 255, 69, 58),
        Color.FromARGB(255, 215, 0, 21),
        Color.FromARGB(255, 255, 105, 97),
        debugLabel: "systemRed");

    public static CupertinoDynamicColor SystemTeal { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 90, 200, 250),
        Color.FromARGB(255, 100, 210, 255),
        Color.FromARGB(255, 0, 113, 164),
        Color.FromARGB(255, 112, 215, 255),
        debugLabel: "systemTeal");

    public static CupertinoDynamicColor SystemCyan { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 50, 173, 230),
        Color.FromARGB(255, 100, 210, 255),
        Color.FromARGB(255, 0, 113, 164),
        Color.FromARGB(255, 112, 215, 255),
        debugLabel: "systemCyan");

    public static CupertinoDynamicColor SystemYellow { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 255, 204, 0),
        Color.FromARGB(255, 255, 214, 10),
        Color.FromARGB(255, 160, 90, 0),
        Color.FromARGB(255, 255, 212, 38),
        debugLabel: "systemYellow");

    public static CupertinoDynamicColor SystemGrey { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 142, 142, 147),
        Color.FromARGB(255, 142, 142, 147),
        Color.FromARGB(255, 108, 108, 112),
        Color.FromARGB(255, 174, 174, 178),
        debugLabel: "systemGrey");

    public static CupertinoDynamicColor SystemGrey2 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 174, 174, 178),
        Color.FromARGB(255, 99, 99, 102),
        Color.FromARGB(255, 142, 142, 147),
        Color.FromARGB(255, 124, 124, 128),
        debugLabel: "systemGrey2");

    public static CupertinoDynamicColor SystemGrey3 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 199, 199, 204),
        Color.FromARGB(255, 72, 72, 74),
        Color.FromARGB(255, 174, 174, 178),
        Color.FromARGB(255, 84, 84, 86),
        debugLabel: "systemGrey3");

    public static CupertinoDynamicColor SystemGrey4 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 209, 209, 214),
        Color.FromARGB(255, 58, 58, 60),
        Color.FromARGB(255, 188, 188, 192),
        Color.FromARGB(255, 68, 68, 70),
        debugLabel: "systemGrey4");

    public static CupertinoDynamicColor SystemGrey5 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 229, 229, 234),
        Color.FromARGB(255, 44, 44, 46),
        Color.FromARGB(255, 216, 216, 220),
        Color.FromARGB(255, 54, 54, 56),
        debugLabel: "systemGrey5");

    public static CupertinoDynamicColor SystemGrey6 { get; } = CupertinoDynamicColor.WithBrightnessAndContrast(
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 28, 28, 30),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 36, 36, 38),
        debugLabel: "systemGrey6");

    public static CupertinoDynamicColor Label { get; } = new(
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        debugLabel: "label");

    public static CupertinoDynamicColor SecondaryLabel { get; } = new(
        Color.FromARGB(153, 60, 60, 67),
        Color.FromARGB(153, 235, 235, 245),
        Color.FromARGB(173, 60, 60, 67),
        Color.FromARGB(173, 235, 235, 245),
        Color.FromARGB(153, 60, 60, 67),
        Color.FromARGB(153, 235, 235, 245),
        Color.FromARGB(173, 60, 60, 67),
        Color.FromARGB(173, 235, 235, 245),
        debugLabel: "secondaryLabel");

    public static CupertinoDynamicColor TertiaryLabel { get; } = new(
        Color.FromARGB(76, 60, 60, 67),
        Color.FromARGB(76, 235, 235, 245),
        Color.FromARGB(96, 60, 60, 67),
        Color.FromARGB(96, 235, 235, 245),
        Color.FromARGB(76, 60, 60, 67),
        Color.FromARGB(76, 235, 235, 245),
        Color.FromARGB(96, 60, 60, 67),
        Color.FromARGB(96, 235, 235, 245),
        debugLabel: "tertiaryLabel");

    public static CupertinoDynamicColor QuaternaryLabel { get; } = new(
        Color.FromARGB(45, 60, 60, 67),
        Color.FromARGB(40, 235, 235, 245),
        Color.FromARGB(66, 60, 60, 67),
        Color.FromARGB(61, 235, 235, 245),
        Color.FromARGB(45, 60, 60, 67),
        Color.FromARGB(40, 235, 235, 245),
        Color.FromARGB(66, 60, 60, 67),
        Color.FromARGB(61, 235, 235, 245),
        debugLabel: "quaternaryLabel");

    public static CupertinoDynamicColor SystemFill { get; } = new(
        Color.FromARGB(51, 120, 120, 128),
        Color.FromARGB(91, 120, 120, 128),
        Color.FromARGB(71, 120, 120, 128),
        Color.FromARGB(112, 120, 120, 128),
        Color.FromARGB(51, 120, 120, 128),
        Color.FromARGB(91, 120, 120, 128),
        Color.FromARGB(71, 120, 120, 128),
        Color.FromARGB(112, 120, 120, 128),
        debugLabel: "systemFill");

    public static CupertinoDynamicColor SecondarySystemFill { get; } = new(
        Color.FromARGB(40, 120, 120, 128),
        Color.FromARGB(81, 120, 120, 128),
        Color.FromARGB(61, 120, 120, 128),
        Color.FromARGB(102, 120, 120, 128),
        Color.FromARGB(40, 120, 120, 128),
        Color.FromARGB(81, 120, 120, 128),
        Color.FromARGB(61, 120, 120, 128),
        Color.FromARGB(102, 120, 120, 128),
        debugLabel: "secondarySystemFill");

    public static CupertinoDynamicColor TertiarySystemFill { get; } = new(
        Color.FromARGB(30, 118, 118, 128),
        Color.FromARGB(61, 118, 118, 128),
        Color.FromARGB(51, 118, 118, 128),
        Color.FromARGB(81, 118, 118, 128),
        Color.FromARGB(30, 118, 118, 128),
        Color.FromARGB(61, 118, 118, 128),
        Color.FromARGB(51, 118, 118, 128),
        Color.FromARGB(81, 118, 118, 128),
        debugLabel: "tertiarySystemFill");

    public static CupertinoDynamicColor QuaternarySystemFill { get; } = new(
        Color.FromARGB(20, 116, 116, 128),
        Color.FromARGB(45, 118, 118, 128),
        Color.FromARGB(40, 116, 116, 128),
        Color.FromARGB(66, 118, 118, 128),
        Color.FromARGB(20, 116, 116, 128),
        Color.FromARGB(45, 118, 118, 128),
        Color.FromARGB(40, 116, 116, 128),
        Color.FromARGB(66, 118, 118, 128),
        debugLabel: "quaternarySystemFill");

    public static CupertinoDynamicColor PlaceholderText { get; } = new(
        Color.FromARGB(76, 60, 60, 67),
        Color.FromARGB(76, 235, 235, 245),
        Color.FromARGB(96, 60, 60, 67),
        Color.FromARGB(96, 235, 235, 245),
        Color.FromARGB(76, 60, 60, 67),
        Color.FromARGB(76, 235, 235, 245),
        Color.FromARGB(96, 60, 60, 67),
        Color.FromARGB(96, 235, 235, 245),
        debugLabel: "placeholderText");

    public static CupertinoDynamicColor SystemBackground { get; } = new(
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 28, 28, 30),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 36, 36, 38),
        debugLabel: "systemBackground");

    public static CupertinoDynamicColor SecondarySystemBackground { get; } = new(
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 28, 28, 30),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 36, 36, 38),
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 44, 44, 46),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 54, 54, 56),
        debugLabel: "secondarySystemBackground");

    public static CupertinoDynamicColor TertiarySystemBackground { get; } = new(
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 44, 44, 46),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 54, 54, 56),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 58, 58, 60),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 68, 68, 70),
        debugLabel: "tertiarySystemBackground");

    public static CupertinoDynamicColor SystemGroupedBackground { get; } = new(
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 0, 0, 0),
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 28, 28, 30),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 36, 36, 38),
        debugLabel: "systemGroupedBackground");

    public static CupertinoDynamicColor SecondarySystemGroupedBackground { get; } = new(
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 28, 28, 30),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 36, 36, 38),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 44, 44, 46),
        Color.FromARGB(255, 255, 255, 255),
        Color.FromARGB(255, 54, 54, 56),
        debugLabel: "secondarySystemGroupedBackground");

    public static CupertinoDynamicColor TertiarySystemGroupedBackground { get; } = new(
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 44, 44, 46),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 54, 54, 56),
        Color.FromARGB(255, 242, 242, 247),
        Color.FromARGB(255, 58, 58, 60),
        Color.FromARGB(255, 235, 235, 240),
        Color.FromARGB(255, 68, 68, 70),
        debugLabel: "tertiarySystemGroupedBackground");

    public static CupertinoDynamicColor Separator { get; } = new(
        Color.FromARGB(73, 60, 60, 67),
        Color.FromARGB(153, 84, 84, 88),
        Color.FromARGB(94, 60, 60, 67),
        Color.FromARGB(173, 84, 84, 88),
        Color.FromARGB(73, 60, 60, 67),
        Color.FromARGB(153, 210, 210, 210),
        Color.FromARGB(94, 60, 60, 67),
        Color.FromARGB(173, 84, 84, 88),
        debugLabel: "separator");

    public static CupertinoDynamicColor OpaqueSeparator { get; } = new(
        Color.FromARGB(255, 198, 198, 200),
        Color.FromARGB(255, 56, 56, 58),
        Color.FromARGB(255, 198, 198, 200),
        Color.FromARGB(255, 56, 56, 58),
        Color.FromARGB(255, 198, 198, 200),
        Color.FromARGB(255, 56, 56, 58),
        Color.FromARGB(255, 198, 198, 200),
        Color.FromARGB(255, 56, 56, 58),
        debugLabel: "opaqueSeparator");

    public static CupertinoDynamicColor Link { get; } = new(
        Color.FromARGB(255, 0, 122, 255),
        Color.FromARGB(255, 9, 132, 255),
        Color.FromARGB(255, 0, 122, 255),
        Color.FromARGB(255, 9, 132, 255),
        Color.FromARGB(255, 0, 122, 255),
        Color.FromARGB(255, 9, 132, 255),
        Color.FromARGB(255, 0, 122, 255),
        Color.FromARGB(255, 9, 132, 255),
        debugLabel: "link");
}

/// <summary>
/// A <see cref="Color"/> that has different values for different visual contexts: the ambient
/// <see cref="CupertinoTheme"/> brightness, the accessibility high-contrast setting and the
/// <see cref="CupertinoUserInterfaceLevel"/> of its surroundings.
/// </summary>
/// <remarks>
/// Like Dart's <c>CupertinoDynamicColor implements Color</c>, it derives from <see cref="Color"/>, so
/// any colour slot can hold one and <see cref="Resolve"/> recovers it with a runtime type test. Every
/// <see cref="Color"/> member forwards to the currently effective variant. C# has no mixins, so
/// Dart's <c>with Diagnosticable</c> is the <see cref="IDiagnosticable"/> interface.
/// </remarks>
public class CupertinoDynamicColor : Color, IDiagnosticable
{
    private readonly Color _effectiveColor;
    private readonly string? _debugLabel;
    private readonly Element? _debugResolveContext;

    /// <summary>Creates an adaptive <see cref="Color"/> that changes with every visual context.</summary>
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
            null,
            debugLabel)
    {
    }

    // Dart's `CupertinoDynamicColor._`.
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
        Element? debugResolveContext,
        string? debugLabel)
        : base(effectiveColor.A, effectiveColor.R, effectiveColor.G, effectiveColor.B, effectiveColor.ColorSpace)
    {
        _effectiveColor = effectiveColor;
        Color = color;
        DarkColor = darkColor;
        HighContrastColor = highContrastColor;
        DarkHighContrastColor = darkHighContrastColor;
        ElevatedColor = elevatedColor;
        DarkElevatedColor = darkElevatedColor;
        HighContrastElevatedColor = highContrastElevatedColor;
        DarkHighContrastElevatedColor = darkHighContrastElevatedColor;
        _debugResolveContext = debugResolveContext;
        _debugLabel = debugLabel;
    }

    /// <summary>
    /// Creates an adaptive <see cref="Color"/> that changes its effective color based on the
    /// platform brightness and the accessibility contrast setting.
    /// </summary>
    public static CupertinoDynamicColor WithBrightnessAndContrast(
        Color color,
        Color darkColor,
        Color highContrastColor,
        Color darkHighContrastColor,
        string? debugLabel = null) =>
        new(
            color,
            darkColor,
            highContrastColor,
            darkHighContrastColor,
            color,
            darkColor,
            highContrastColor,
            darkHighContrastColor,
            debugLabel);

    /// <summary>
    /// Creates an adaptive <see cref="Color"/> that changes its effective color based on the given
    /// <see cref="BuildContext"/>'s brightness.
    /// </summary>
    public static CupertinoDynamicColor WithBrightness(
        Color color,
        Color darkColor,
        string? debugLabel = null) =>
        new(
            color,
            darkColor,
            color,
            darkColor,
            color,
            darkColor,
            color,
            darkColor,
            debugLabel);

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
    /// Resolves the given <see cref="Color"/> by calling <see cref="ResolveFrom"/>.
    /// </summary>
    /// <remarks>
    /// If the given color is already a concrete <see cref="Color"/>, it will be returned as is.
    /// </remarks>
    public static Color Resolve(Color resolvable, BuildContext context) =>
        resolvable is CupertinoDynamicColor dynamicColor ? dynamicColor.ResolveFrom(context) : resolvable;

    /// <summary>
    /// Resolves the given <see cref="Color"/> by calling <see cref="ResolveFrom"/>, or returns null
    /// when <paramref name="resolvable"/> is null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(resolvable))]
    public static Color? MaybeResolve(Color? resolvable, BuildContext context) =>
        resolvable is CupertinoDynamicColor dynamicColor ? dynamicColor.ResolveFrom(context) : resolvable;

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

    /// <summary>
    /// Resolves this <see cref="CupertinoDynamicColor"/> using the provided
    /// <see cref="BuildContext"/>.
    /// </summary>
    /// <remarks>
    /// Only the dependencies this color actually varies with are read, so a color that does not
    /// depend on, say, elevation never subscribes to <see cref="CupertinoUserInterfaceLevel"/>.
    /// </remarks>
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

        Element? debugContext = null;
        if (Constants.KDebugMode)
        {
            debugContext = context as Element;
        }

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
            debugContext,
            _debugLabel);
    }

    // Dart's private `_debugLabel`, read by `createCupertinoColorProperty`.
    internal string? DebugLabel => _debugLabel;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is CupertinoDynamicColor other
               && other.Value == Value
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
        hash.Add(Value);
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

    public override string ToString() => ToString(DiagnosticLevel.Info);

    /// <summary>Dart's <c>toString({DiagnosticLevel minLevel})</c>.</summary>
    public string ToString(DiagnosticLevel minLevel)
    {
        _ = minLevel;
        string Describe(string name, Color color)
        {
            string marker = color == _effectiveColor ? "*" : string.Empty;
            return $"{marker}{name} = {color}{marker}";
        }

        var xs = new List<string> { Describe("color", Color) };
        if (IsPlatformBrightnessDependent)
        {
            xs.Add(Describe("darkColor", DarkColor));
        }

        if (IsHighContrastDependent)
        {
            xs.Add(Describe("highContrastColor", HighContrastColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent)
        {
            xs.Add(Describe("darkHighContrastColor", DarkHighContrastColor));
        }

        if (IsInterfaceElevationDependent)
        {
            xs.Add(Describe("elevatedColor", ElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsInterfaceElevationDependent)
        {
            xs.Add(Describe("darkElevatedColor", DarkElevatedColor));
        }

        if (IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            xs.Add(Describe("highContrastElevatedColor", HighContrastElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            xs.Add(Describe("darkHighContrastElevatedColor", DarkHighContrastElevatedColor));
        }

        string label = _debugLabel ?? Diagnostics.ObjectRuntimeType(this, "CupertinoDynamicColor");
        object resolvedBy = (object?)_debugResolveContext?.Widget ?? "UNRESOLVED";
        return $"{label}({string.Join(", ", xs)}, resolved by: {resolvedBy})";
    }

    /// <inheritdoc />
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (_debugLabel != null)
        {
            properties.Add(new MessageProperty("debugLabel", _debugLabel));
        }

        properties.Add(CupertinoColors.CreateCupertinoColorProperty("color", Color));
        if (IsPlatformBrightnessDependent)
        {
            properties.Add(CupertinoColors.CreateCupertinoColorProperty("darkColor", DarkColor));
        }

        if (IsHighContrastDependent)
        {
            properties.Add(CupertinoColors.CreateCupertinoColorProperty("highContrastColor", HighContrastColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent)
        {
            properties.Add(
                CupertinoColors.CreateCupertinoColorProperty("darkHighContrastColor", DarkHighContrastColor));
        }

        if (IsInterfaceElevationDependent)
        {
            properties.Add(CupertinoColors.CreateCupertinoColorProperty("elevatedColor", ElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsInterfaceElevationDependent)
        {
            properties.Add(CupertinoColors.CreateCupertinoColorProperty("darkElevatedColor", DarkElevatedColor));
        }

        if (IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            properties.Add(
                CupertinoColors.CreateCupertinoColorProperty(
                    "highContrastElevatedColor",
                    HighContrastElevatedColor));
        }

        if (IsPlatformBrightnessDependent && IsHighContrastDependent && IsInterfaceElevationDependent)
        {
            properties.Add(
                CupertinoColors.CreateCupertinoColorProperty(
                    "darkHighContrastElevatedColor",
                    DarkHighContrastElevatedColor));
        }

        if (_debugResolveContext != null)
        {
            properties.Add(new DiagnosticsProperty<Element>("last resolved", _debugResolveContext));
        }
    }

    // Every `Color` member forwards to the effective color, as Dart's `implements Color` does.

    public override uint Value => _effectiveColor.Value;

    public override uint ToARGB32() => _effectiveColor.ToARGB32();

    public override int Alpha => _effectiveColor.Alpha;

    public override int Blue => _effectiveColor.Blue;

    public override double ComputeLuminance() => _effectiveColor.ComputeLuminance();

    public override int Green => _effectiveColor.Green;

    public override double Opacity => _effectiveColor.Opacity;

    public override int Red => _effectiveColor.Red;

    public override Color WithAlpha(int a) => _effectiveColor.WithAlpha(a);

    public override Color WithBlue(int b) => _effectiveColor.WithBlue(b);

    public override Color WithGreen(int g) => _effectiveColor.WithGreen(g);

    public override Color WithOpacity(double opacity) => _effectiveColor.WithOpacity(opacity);

    public override Color WithRed(int r) => _effectiveColor.WithRed(r);

    public override double A => _effectiveColor.A;

    public override double R => _effectiveColor.R;

    public override double G => _effectiveColor.G;

    public override double B => _effectiveColor.B;

    public override ColorSpace ColorSpace => _effectiveColor.ColorSpace;

    public override Color WithValues(
        double? alpha = null,
        double? red = null,
        double? green = null,
        double? blue = null,
        ColorSpace? colorSpace = null) =>
        _effectiveColor.WithValues(alpha: alpha, red: red, green: green, blue: blue, colorSpace: colorSpace);
}
