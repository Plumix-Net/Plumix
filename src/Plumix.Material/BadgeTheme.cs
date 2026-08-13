using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/badge_theme.dart

public sealed record BadgeThemeData(
    Color? BackgroundColor = null,
    Color? TextColor = null,
    double? SmallSize = null,
    double? LargeSize = null,
    TextStyle? TextStyle = null,
    Thickness? Padding = null,
    AlignmentGeometry? Alignment = null,
    Vector? Offset = null)
{
    public BadgeThemeData CopyWith(
        Color? backgroundColor = null,
        Color? textColor = null,
        double? smallSize = null,
        double? largeSize = null,
        TextStyle? textStyle = null,
        Thickness? padding = null,
        AlignmentGeometry? alignment = null,
        Vector? offset = null)
    {
        return new BadgeThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            TextColor: textColor ?? TextColor,
            SmallSize: smallSize ?? SmallSize,
            LargeSize: largeSize ?? LargeSize,
            TextStyle: textStyle ?? TextStyle,
            Padding: padding ?? Padding,
            Alignment: alignment ?? Alignment,
            Offset: offset ?? Offset);
    }

    public static BadgeThemeData Lerp(BadgeThemeData? a, BadgeThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new BadgeThemeData(
            BackgroundColor: LerpColor(a?.BackgroundColor, b?.BackgroundColor, t),
            TextColor: LerpColor(a?.TextColor, b?.TextColor, t),
            SmallSize: LerpDouble(a?.SmallSize, b?.SmallSize, t),
            LargeSize: LerpDouble(a?.LargeSize, b?.LargeSize, t),
            TextStyle: LerpTextStyle(a?.TextStyle, b?.TextStyle, t),
            Padding: LerpThickness(a?.Padding, b?.Padding, t),
            Alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, t),
            Offset: LerpVector(a?.Offset, b?.Offset, t));
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Color from = a ?? Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        Color to = b ?? Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return Color.FromArgb(
            LerpChannel(from.A, to.A, t),
            LerpChannel(from.R, to.R, t),
            LerpChannel(from.G, to.G, t),
            LerpChannel(from.B, to.B, t));
    }

    private static byte LerpChannel(byte a, byte b, double t)
    {
        double value = a + ((b - a) * t);
        return (byte)Math.Clamp(
            Math.Round(value, MidpointRounding.AwayFromZero),
            byte.MinValue,
            byte.MaxValue);
    }

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return from + ((to - from) * t);
    }

    private static TextStyle? LerpTextStyle(TextStyle? a, TextStyle? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return TextStyle.Lerp(a ?? new TextStyle(), b ?? new TextStyle(), t);
    }

    private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Thickness from = a ?? default;
        Thickness to = b ?? default;
        return new Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
    }

    private static Vector? LerpVector(Vector? a, Vector? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Vector from = a ?? default;
        Vector to = b ?? default;
        return new Vector(
            from.X + ((to.X - from.X) * t),
            from.Y + ((to.Y - from.Y) * t));
    }
}

public sealed class BadgeTheme : InheritedTheme
{
    public BadgeTheme(BadgeThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BadgeThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new BadgeTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((BadgeTheme)oldWidget).Data, Data);
    }

    public static BadgeThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<BadgeTheme>()?.Data ?? Theme.Of(context).BadgeTheme;
    }
}
