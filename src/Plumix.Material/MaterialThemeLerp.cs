using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

internal static class MaterialThemeLerp
{
    public static double? Double(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return from + ((to - from) * t);
    }

    public static Color? Color(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Avalonia.Media.Color from = a
                                    ?? Avalonia.Media.Color.FromArgb(
                                        0,
                                        b!.Value.R,
                                        b.Value.G,
                                        b.Value.B);
        Avalonia.Media.Color to = b
                                  ?? Avalonia.Media.Color.FromArgb(
                                      0,
                                      a!.Value.R,
                                      a.Value.G,
                                      a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }

    public static TextStyle? TextStyle(TextStyle? a, TextStyle? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return Plumix.Widgets.TextStyle.Lerp(
            a ?? new Plumix.Widgets.TextStyle(),
            b ?? new Plumix.Widgets.TextStyle(),
            t);
    }

    public static IconThemeData? IconTheme(IconThemeData? a, IconThemeData? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return IconThemeData.Lerp(a, b, t);
    }

    public static ShapeBorder? Shape(ShapeBorder? a, ShapeBorder? b, double t)
    {
        return ShapeBorder.Lerp(a, b, t);
    }

    public static BorderRadius? BorderRadius(BorderRadius? a, BorderRadius? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        return Plumix.Rendering.BorderRadius.Lerp(a, b, t);
    }

    public static BorderRadiusGeometry? BorderRadiusGeometry(
        BorderRadiusGeometry? a,
        BorderRadiusGeometry? b,
        double t)
    {
        return Plumix.Rendering.BorderRadiusGeometry.Lerp(a, b, t);
    }

    public static BorderSide? BorderSide(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Plumix.Rendering.BorderSide from = a ?? new Plumix.Rendering.BorderSide(
            Avalonia.Media.Color.FromArgb(
                0,
                b!.Value.Color.R,
                b.Value.Color.G,
                b.Value.Color.B),
            0.0,
            b.Value.Style);
        Plumix.Rendering.BorderSide to = b ?? new Plumix.Rendering.BorderSide(
            Avalonia.Media.Color.FromArgb(
                0,
                a!.Value.Color.R,
                a.Value.Color.G,
                a.Value.Color.B),
            0.0,
            a.Value.Style);
        return new Plumix.Rendering.BorderSide(
            Color(from.Color, to.Color, t)!.Value,
            from.Width + ((to.Width - from.Width) * t),
            t < 0.5 ? from.Style : to.Style);
    }

    public static BoxDecoration? Decoration(BoxDecoration? a, BoxDecoration? b, double t)
    {
        return BoxDecoration.Lerp(a, b, t);
    }

    public static Plumix.Rendering.Decoration? AnyDecoration(
        Plumix.Rendering.Decoration? a,
        Plumix.Rendering.Decoration? b,
        double t)
    {
        return Plumix.Rendering.Decoration.Lerp(a, b, t);
    }

    public static EdgeInsetsGeometry? EdgeInsets(EdgeInsetsGeometry? a, EdgeInsetsGeometry? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        return EdgeInsetsGeometry.Lerp(a, b, t);
    }

    public static Size? Size(Size? a, Size? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Avalonia.Size from = a ?? default;
        Avalonia.Size to = b ?? default;
        return new Avalonia.Size(
            from.Width + ((to.Width - from.Width) * t),
            from.Height + ((to.Height - from.Height) * t));
    }

    public static Alignment? Alignment(Alignment? a, Alignment? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Plumix.Rendering.Alignment from = a ?? default;
        Plumix.Rendering.Alignment to = b ?? default;
        return new Plumix.Rendering.Alignment(
            from.X + ((to.X - from.X) * t),
            from.Y + ((to.Y - from.Y) * t));
    }

    public static Thickness? Thickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Avalonia.Thickness from = a ?? default;
        Avalonia.Thickness to = b ?? default;
        return new Avalonia.Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
    }

    public static BoxConstraints? BoxConstraints(BoxConstraints? a, BoxConstraints? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        if (!a.HasValue)
        {
            return ScaleConstraints(b!.Value, t);
        }

        if (!b.HasValue)
        {
            return ScaleConstraints(a.Value, 1.0 - t);
        }

        ValidateConstraintFiniteness(a.Value, b.Value);
        return new Plumix.Rendering.BoxConstraints(
            MinWidth: LerpConstraint(a.Value.MinWidth, b.Value.MinWidth, t),
            MaxWidth: LerpConstraint(a.Value.MaxWidth, b.Value.MaxWidth, t),
            MinHeight: LerpConstraint(a.Value.MinHeight, b.Value.MinHeight, t),
            MaxHeight: LerpConstraint(a.Value.MaxHeight, b.Value.MaxHeight, t));
    }

    public static MaterialStateProperty<T?>? StateProperty<T>(
        MaterialStateProperty<T?>? a,
        MaterialStateProperty<T?>? b,
        double t,
        Func<T?, T?, double, T?> lerp)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<T?>.ResolveWith(states =>
        {
            T? aValue = a is null ? default : a.Resolve(states);
            T? bValue = b is null ? default : b.Resolve(states);
            return lerp(aValue, bValue, t);
        });
    }

    public static MaterialStateProperty<double?>? DoubleStateProperty(
        MaterialStateProperty<double?>? a,
        MaterialStateProperty<double?>? b,
        double t)
    {
        return StateProperty(a, b, t, Double);
    }

    public static MaterialStateProperty<BorderSide?>? BorderSideStateProperty(
        MaterialStateProperty<BorderSide?>? a,
        MaterialStateProperty<BorderSide?>? b,
        double t)
    {
        return StateProperty(a, b, t, BorderSide);
    }

    public static MaterialStateProperty<Size?>? SizeStateProperty(
        MaterialStateProperty<Size?>? a,
        MaterialStateProperty<Size?>? b,
        double t)
    {
        return StateProperty(a, b, t, Size);
    }

    public static MaterialStateProperty<Thickness?>? ThicknessStateProperty(
        MaterialStateProperty<Thickness?>? a,
        MaterialStateProperty<Thickness?>? b,
        double t)
    {
        return StateProperty(a, b, t, Thickness);
    }

    public static MaterialStateProperty<EdgeInsetsGeometry?>? EdgeInsetsStateProperty(
        MaterialStateProperty<EdgeInsetsGeometry?>? a,
        MaterialStateProperty<EdgeInsetsGeometry?>? b,
        double t)
    {
        return StateProperty(a, b, t, EdgeInsetsGeometry.Lerp);
    }

    public static MaterialStateProperty<TextStyle?>? TextStyleStateProperty(
        MaterialStateProperty<TextStyle?>? a,
        MaterialStateProperty<TextStyle?>? b,
        double t)
    {
        return StateProperty(a, b, t, TextStyle);
    }

    public static MaterialStateProperty<ShapeBorder?>? ShapeStateProperty(
        MaterialStateProperty<ShapeBorder?>? a,
        MaterialStateProperty<ShapeBorder?>? b,
        double t)
    {
        return StateProperty(a, b, t, Shape);
    }

    public static MaterialStateProperty<OutlinedBorder?>? OutlinedBorderStateProperty(
        MaterialStateProperty<OutlinedBorder?>? a,
        MaterialStateProperty<OutlinedBorder?>? b,
        double t)
    {
        return StateProperty(a, b, t, OutlinedBorder.Lerp);
    }

    public static MaterialStateProperty<Color?>? ColorStateProperty(
        MaterialStateProperty<Color?>? a,
        MaterialStateProperty<Color?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(
            states => Color(a?.Resolve(states), b?.Resolve(states), t));
    }

    private static double LerpConstraint(double a, double b, double t)
    {
        if (double.IsPositiveInfinity(a) && double.IsPositiveInfinity(b))
        {
            return double.PositiveInfinity;
        }

        return a + ((b - a) * t);
    }

    private static BoxConstraints ScaleConstraints(BoxConstraints constraints, double factor)
    {
        return new BoxConstraints(
            MinWidth: constraints.MinWidth * factor,
            MaxWidth: double.IsPositiveInfinity(constraints.MaxWidth)
                ? double.PositiveInfinity
                : constraints.MaxWidth * factor,
            MinHeight: constraints.MinHeight * factor,
            MaxHeight: double.IsPositiveInfinity(constraints.MaxHeight)
                ? double.PositiveInfinity
                : constraints.MaxHeight * factor);
    }

    private static void ValidateConstraintFiniteness(BoxConstraints a, BoxConstraints b)
    {
        bool valid = double.IsFinite(a.MinWidth) == double.IsFinite(b.MinWidth)
                     && double.IsFinite(a.MaxWidth) == double.IsFinite(b.MaxWidth)
                     && double.IsFinite(a.MinHeight) == double.IsFinite(b.MinHeight)
                     && double.IsFinite(a.MaxHeight) == double.IsFinite(b.MaxHeight);
        if (!valid)
        {
            throw new ArgumentException(
                "Cannot interpolate between finite and unbounded BoxConstraints fields.");
        }
    }
}
