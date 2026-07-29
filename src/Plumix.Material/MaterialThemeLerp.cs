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
        if (a is null || b is null)
        {
            return t < 0.5 ? a : b;
        }

        BorderSide? side = BorderSide(a.Side, b.Side, t);
        double radius = a.BorderRadius.Radius
                        + ((b.BorderRadius.Radius - a.BorderRadius.Radius) * t);
        return new ShapeBorder(BorderRadius.Circular(radius), side)
        {
            Shape = t < 0.5 ? a.Shape : b.Shape,
        };
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

    public static MaterialStateProperty<T?>? StateProperty<T>(
        MaterialStateProperty<T?>? a,
        MaterialStateProperty<T?>? b,
        double t,
        Func<T?, T?, double, T?> lerp) where T : class
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<T?>.ResolveWith(
            states => lerp(a?.Resolve(states), b?.Resolve(states), t));
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

    private static BorderSide? BorderSide(BorderSide? a, BorderSide? b, double t)
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
}
