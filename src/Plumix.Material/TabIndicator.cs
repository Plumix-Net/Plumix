using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/tab_indicator.dart

/// <summary>
/// Used with <see cref="TabBar.Indicator"/> to draw a horizontal line below the selected tab.
/// </summary>
/// <remarks>
/// The selected tab underline is inset from the tab's boundary by <see cref="Insets"/>. The
/// <see cref="BorderSide"/> defines the line's color and weight. <see cref="TabBar.IndicatorSize"/>
/// can be used to define the indicator's bounds in terms of its (centered) widget with
/// <see cref="TabBarIndicatorSize.Label"/>, or the entire tab with <see cref="TabBarIndicatorSize.Tab"/>.
/// </remarks>
public sealed record UnderlineTabIndicator : Decoration
{
    public UnderlineTabIndicator(
        BorderRadius? borderRadius = null,
        BorderSide? borderSide = null,
        EdgeInsetsGeometry? insets = null)
    {
        BorderRadius = borderRadius;
        BorderSide = borderSide ?? new BorderSide(Colors.White, 2.0);
        Insets = insets ?? EdgeInsetsGeometry.Zero;
    }

    /// <summary>The radius of the indicator's corners; a rectangular indicator is drawn when null.</summary>
    public BorderRadius? BorderRadius { get; init; }

    /// <summary>The color and weight of the horizontal line drawn below the selected tab.</summary>
    public BorderSide BorderSide { get; init; }

    /// <summary>Locates the selected tab's underline relative to the tab's boundary.</summary>
    public EdgeInsetsGeometry Insets { get; init; }

    public override Decoration? LerpFrom(Decoration? a, double t)
    {
        if (a is UnderlineTabIndicator from)
        {
            return new UnderlineTabIndicator(
                borderSide: BorderSide.Lerp(from.BorderSide, BorderSide, t),
                insets: EdgeInsetsGeometry.Lerp(from.Insets, Insets, t)!.Value);
        }

        return base.LerpFrom(a, t);
    }

    public override Decoration? LerpTo(Decoration? b, double t)
    {
        if (b is UnderlineTabIndicator to)
        {
            return new UnderlineTabIndicator(
                borderSide: BorderSide.Lerp(BorderSide, to.BorderSide, t),
                insets: EdgeInsetsGeometry.Lerp(Insets, to.Insets, t)!.Value);
        }

        return base.LerpTo(b, t);
    }

    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        return new UnderlinePainter(this, BorderRadius, onChanged);
    }

    public override Plumix.UI.Path GetClipPath(Rect rect, TextDirection textDirection)
    {
        Rect indicator = IndicatorRectFor(rect, textDirection);
        var path = new Plumix.UI.Path();
        if (BorderRadius is { } borderRadius)
        {
            path.AddRRect(borderRadius.ToRRect(indicator));
            return path;
        }

        path.AddRect(indicator);
        return path;
    }

    internal Rect IndicatorRectFor(Rect rect, TextDirection textDirection)
    {
        Thickness resolved = Insets.Resolve(textDirection);
        var indicator = new Rect(
            rect.Left + resolved.Left,
            rect.Top + resolved.Top,
            Math.Max(0.0, rect.Width - resolved.Left - resolved.Right),
            Math.Max(0.0, rect.Height - resolved.Top - resolved.Bottom));
        return new Rect(
            indicator.Left,
            indicator.Bottom - BorderSide.Width,
            indicator.Width,
            BorderSide.Width);
    }
}

// Dart parity source: material_ui/lib/src/tab_indicator.dart (_UnderlinePainter)
internal sealed class UnderlinePainter : BoxPainter
{
    private readonly UnderlineTabIndicator _decoration;
    private readonly BorderRadius? _borderRadius;

    public UnderlinePainter(
        UnderlineTabIndicator decoration,
        BorderRadius? borderRadius,
        Action? onChanged) : base(onChanged)
    {
        _decoration = decoration;
        _borderRadius = borderRadius;
    }

    public override void Paint(PaintingContext context, Point offset, ImageConfiguration configuration)
    {
        Size size = configuration.Size
            ?? throw new InvalidOperationException("UnderlineTabIndicator requires a configured size.");
        TextDirection textDirection = configuration.TextDirection
            ?? throw new InvalidOperationException("UnderlineTabIndicator requires a text direction.");
        var rect = new Rect(offset, size);
        if (_borderRadius is { } borderRadius)
        {
            Rect rounded = _decoration.IndicatorRectFor(rect, textDirection);
            context.DrawRRect(
                Plumix.UI.RRect.FromRectAndCorners(rounded, borderRadius),
                new SolidColorBrush(_decoration.BorderSide.Color),
                pen: null);
            return;
        }

        if (_decoration.BorderSide.Style == BorderStyle.None)
        {
            return;
        }

        double width = _decoration.BorderSide.Width;
        Rect strip = _decoration.IndicatorRectFor(rect, textDirection);
        // Dart deflates the strip by half the stroke width and draws its bottom edge; the strip is
        // exactly `width` tall, so the deflated bottom edge sits on the strip's vertical centre.
        double y = strip.Bottom - (width / 2.0);
        context.DrawLine(
            new Pen(
                new SolidColorBrush(_decoration.BorderSide.Color),
                width,
                lineCap: PenLineCap.Square),
            new Point(strip.Left + (width / 2.0), y),
            new Point(strip.Right - (width / 2.0), y));
    }
}
