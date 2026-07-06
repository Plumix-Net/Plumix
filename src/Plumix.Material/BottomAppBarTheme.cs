using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/bottom_app_bar_theme.dart

public sealed record BottomAppBarThemeData(
    Color? Color = null,
    double? Elevation = null,
    NotchedShape? Shape = null,
    double? Height = null,
    Color? SurfaceTintColor = null,
    Color? ShadowColor = null,
    Thickness? Padding = null)
{
    public static BottomAppBarThemeData? Lerp(BottomAppBarThemeData? a, BottomAppBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b)) return a;
        t = Math.Clamp(t, 0.0, 1.0);
        return new BottomAppBarThemeData(
            Color: LerpColor(a?.Color, b?.Color, t),
            Elevation: LerpDouble(a?.Elevation, b?.Elevation, t),
            Shape: t < 0.5 ? a?.Shape : b?.Shape,
            Height: LerpDouble(a?.Height, b?.Height, t),
            SurfaceTintColor: LerpColor(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            ShadowColor: LerpColor(a?.ShadowColor, b?.ShadowColor, t),
            Padding: LerpThickness(a?.Padding, b?.Padding, t));
    }

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        return (a ?? 0) + (((b ?? 0) - (a ?? 0)) * t);
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? Avalonia.Media.Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        var to = b ?? Avalonia.Media.Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }

    private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? default;
        var to = b ?? default;
        return new Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
    }
}

public sealed class BottomAppBarTheme : InheritedWidget
{
    public BottomAppBarTheme(BottomAppBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BottomAppBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((BottomAppBarTheme)oldWidget).Data, Data);

    public static BottomAppBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<BottomAppBarTheme>()?.Data ?? Theme.Of(context).BottomAppBarTheme;
}
