using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity sources:
// flutter/packages/flutter/lib/src/cupertino/theme.dart
// flutter/packages/flutter/lib/src/cupertino/colors.dart

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

public readonly record struct CupertinoDynamicColor(Color Color, Color DarkColor)
{
    public Color ResolveFrom(BuildContext context)
    {
        return CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark ? DarkColor : Color;
    }
}

public static class CupertinoColors
{
    public static Color Black => Colors.Black;

    public static Color White => Colors.White;

    public static Color Transparent => Colors.Transparent;

    public static Color InactiveGray => Color.Parse("#FF8E8E93");

    public static Color SystemBlue => Color.Parse("#FF007AFF");
}
