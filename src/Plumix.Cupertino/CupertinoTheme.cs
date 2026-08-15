using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity sources:
// cupertino_ui/lib/src/theme.dart
// cupertino_ui/lib/src/colors.dart

public sealed record CupertinoThemeData(
    Color? PrimaryColor = null,
    Color? PrimaryContrastingColor = null,
    PlatformBrightness? Brightness = null)
{
    public Color EffectivePrimaryColor => PrimaryColor ?? CupertinoColors.SystemBlue;

    public Color EffectivePrimaryContrastingColor => PrimaryContrastingColor ?? CupertinoColors.White;
}

public sealed class CupertinoTheme : InheritedWidget
{
    public CupertinoTheme(CupertinoThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CupertinoThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((CupertinoTheme)oldWidget).Data != Data;
    }

    public static CupertinoThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<CupertinoTheme>()?.Data ?? new CupertinoThemeData();
    }

    public static PlatformBrightness BrightnessOf(BuildContext context)
    {
        return Of(context).Brightness
               ?? MediaQuery.MaybeOf(context)?.PlatformBrightness
               ?? PlatformBrightness.Light;
    }
}

/// <summary>The interface elevations a <see cref="CupertinoDynamicColor"/> can resolve against.</summary>
public enum CupertinoUserInterfaceLevelData
{
    Base,
    Elevated,
}

/// <summary>Establishes the visual elevation used by descendant dynamic-color resolution.</summary>
public sealed class CupertinoUserInterfaceLevel : InheritedWidget
{
    public CupertinoUserInterfaceLevel(
        CupertinoUserInterfaceLevelData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CupertinoUserInterfaceLevelData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((CupertinoUserInterfaceLevel)oldWidget).Data != Data;
    }

    public static CupertinoUserInterfaceLevelData Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("CupertinoUserInterfaceLevel not found in context.");
    }

    public static CupertinoUserInterfaceLevelData? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<CupertinoUserInterfaceLevel>()?.Data;
    }
}

/// <summary>
/// A color with brightness-, contrast-, and elevation-dependent variants, resolved against a
/// <see cref="BuildContext"/>. High-contrast variants exist for API parity but the host platform
/// accessibility flag is not surfaced yet, so the normal-contrast variants are always chosen.
/// </summary>
public readonly record struct CupertinoDynamicColor(
    Color Color,
    Color DarkColor,
    Color? HighContrastColor = null,
    Color? DarkHighContrastColor = null,
    Color? ElevatedColor = null,
    Color? DarkElevatedColor = null,
    Color? HighContrastElevatedColor = null,
    Color? DarkHighContrastElevatedColor = null)
{
    public static CupertinoDynamicColor WithBrightness(Color color, Color darkColor) => new(color, darkColor);

    public Color ResolveFrom(BuildContext context)
    {
        bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
        bool elevated = CupertinoUserInterfaceLevel.MaybeOf(context) == CupertinoUserInterfaceLevelData.Elevated;
        if (elevated)
        {
            return dark ? DarkElevatedColor ?? DarkColor : ElevatedColor ?? Color;
        }

        return dark ? DarkColor : Color;
    }

    /// <summary>Dart's static `CupertinoDynamicColor.resolve` for an already-plain color.</summary>
    public static Color Resolve(Color color, BuildContext context) => color;

    public static Color Resolve(CupertinoDynamicColor color, BuildContext context) => color.ResolveFrom(context);

    /// <summary>The light normal-contrast base variant, for call sites that need one plain color.</summary>
    public static implicit operator Color(CupertinoDynamicColor color) => color.Color;
}

public static class CupertinoColors
{
    public static Color Black => Colors.Black;

    public static Color White => Colors.White;

    /// <summary>A fully-transparent color, completely invisible.</summary>
    /// <remarks>
    /// Dart's <c>CupertinoColors.transparent</c> is <c>Color(0x00000000)</c>, not Avalonia's
    /// <c>Colors.Transparent</c> (<c>0x00FFFFFF</c>).
    /// </remarks>
    public static Color Transparent => Color.FromUInt32(0x00000000);

    public static Color InactiveGray => Color.Parse("#FF8E8E93");

    public static CupertinoDynamicColor SystemBlue { get; } = new(
        Color.FromArgb(255, 0, 122, 255),
        Color.FromArgb(255, 10, 132, 255),
        HighContrastColor: Color.FromArgb(255, 0, 64, 221),
        DarkHighContrastColor: Color.FromArgb(255, 64, 156, 255));

    public static CupertinoDynamicColor SystemRed { get; } = new(
        Color.FromArgb(255, 255, 59, 48),
        Color.FromArgb(255, 255, 69, 58),
        HighContrastColor: Color.FromArgb(255, 215, 0, 21),
        DarkHighContrastColor: Color.FromArgb(255, 255, 105, 97));

    public static CupertinoDynamicColor Label { get; } = new(
        Color.FromArgb(255, 0, 0, 0),
        Color.FromArgb(255, 255, 255, 255));

    public static CupertinoDynamicColor Separator { get; } = new(
        Color.FromArgb(73, 60, 60, 67),
        Color.FromArgb(153, 84, 84, 88),
        HighContrastColor: Color.FromArgb(94, 60, 60, 67),
        DarkHighContrastColor: Color.FromArgb(173, 84, 84, 88),
        ElevatedColor: Color.FromArgb(73, 60, 60, 67),
        DarkElevatedColor: Color.FromArgb(153, 210, 210, 210),
        HighContrastElevatedColor: Color.FromArgb(94, 60, 60, 67),
        DarkHighContrastElevatedColor: Color.FromArgb(173, 84, 84, 88));
}

/// <summary>Dart's `kCupertinoModalBarrierColor` from `cupertino/route.dart`.</summary>
public static class CupertinoRouteConstants
{
    public static CupertinoDynamicColor ModalBarrierColor { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0x33000000),
        Color.FromUInt32(0x7A000000));
}
