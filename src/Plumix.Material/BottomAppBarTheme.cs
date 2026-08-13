using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/bottom_app_bar_theme.dart

public sealed record BottomAppBarThemeData(
    Color? Color = null,
    double? Elevation = null,
    NotchedShape? Shape = null,
    double? Height = null,
    Color? SurfaceTintColor = null,
    Color? ShadowColor = null,
    Thickness? Padding = null)
{
    public BottomAppBarThemeData CopyWith(
        Color? color = null,
        double? elevation = null,
        NotchedShape? shape = null,
        double? height = null,
        Color? surfaceTintColor = null,
        Color? shadowColor = null,
        Thickness? padding = null)
    {
        return new BottomAppBarThemeData(
            Color: color ?? Color,
            Elevation: elevation ?? Elevation,
            Shape: shape ?? Shape,
            Height: height ?? Height,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            ShadowColor: shadowColor ?? ShadowColor,
            Padding: padding ?? Padding);
    }

    public static BottomAppBarThemeData? Lerp(BottomAppBarThemeData? a, BottomAppBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new BottomAppBarThemeData(
            Color: MaterialThemeLerp.Color(a?.Color, b?.Color, clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            Shape: clampedT < 0.5 ? a?.Shape : b?.Shape,
            Height: MaterialThemeLerp.Double(a?.Height, b?.Height, clampedT),
            SurfaceTintColor: MaterialThemeLerp.Color(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                clampedT),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, clampedT),
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, clampedT));
    }
}

public sealed class BottomAppBarTheme : InheritedTheme
{
    public BottomAppBarTheme(BottomAppBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BottomAppBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new BottomAppBarTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((BottomAppBarTheme)oldWidget).Data, Data);

    public static BottomAppBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<BottomAppBarTheme>()?.Data ?? Theme.Of(context).BottomAppBarTheme;
}
