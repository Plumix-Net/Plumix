using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: the render object behind the C#-only InkSplash widget; no Dart counterpart.

namespace Plumix.Rendering;

public sealed class RenderInkSplash : RenderProxyBox
{
    private Color? _splashColor;
    private Point _splashOrigin;
    private double _splashProgress;
    private double? _splashRadius;
    private bool _clipToBounds = true;

    public RenderInkSplash(
        Color? splashColor = null,
        Point splashOrigin = default,
        double splashProgress = 0,
        double? splashRadius = null,
        bool clipToBounds = true,
        RenderBox? child = null)
    {
        _splashColor = splashColor;
        _splashOrigin = splashOrigin;
        _splashProgress = NormalizeProgress(splashProgress);
        _splashRadius = NormalizeRadius(splashRadius);
        _clipToBounds = clipToBounds;
        Child = child;
    }

    public Color? SplashColor
    {
        get => _splashColor;
        set
        {
            if (_splashColor == value)
            {
                return;
            }

            _splashColor = value;
            MarkNeedsPaint();
        }
    }

    public Point SplashOrigin
    {
        get => _splashOrigin;
        set
        {
            if (_splashOrigin == value)
            {
                return;
            }

            _splashOrigin = value;
            MarkNeedsPaint();
        }
    }

    public double SplashProgress
    {
        get => _splashProgress;
        set
        {
            double normalized = NormalizeProgress(value);
            if (Math.Abs(_splashProgress - normalized) < 0.0001)
            {
                return;
            }

            _splashProgress = normalized;
            MarkNeedsPaint();
        }
    }

    public double? SplashRadius
    {
        get => _splashRadius;
        set
        {
            double? normalized = NormalizeRadius(value);
            if (_splashRadius == normalized)
            {
                return;
            }

            _splashRadius = normalized;
            MarkNeedsPaint();
        }
    }

    public bool ClipToBounds
    {
        get => _clipToBounds;
        set
        {
            if (_clipToBounds == value)
            {
                return;
            }

            _clipToBounds = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_clipToBounds)
        {
            ctx.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                (clippedContext, clippedOffset) =>
                {
                    PaintSplash(clippedContext, clippedOffset);
                    base.Paint(clippedContext, clippedOffset);
                });
            return;
        }

        PaintSplash(ctx, offset);
        base.Paint(ctx, offset);
    }

    private void PaintSplash(PaintingContext ctx, Point offset)
    {
        if (_splashColor == null || _splashProgress <= 0)
        {
            return;
        }

        var resolvedOrigin = ResolveOrigin(Size, _splashOrigin);
        double localMaxRadius = Math.Sqrt((Size.Width * Size.Width) + (Size.Height * Size.Height));
        double constrainedMaxRadius = _splashRadius.HasValue
            ? Math.Min(localMaxRadius, _splashRadius.Value)
            : localMaxRadius;
        double radius = constrainedMaxRadius * _splashProgress;

        var brush = new SolidColorBrush(_splashColor!);
        ctx.Canvas.DrawCircle(brush, pen: null, center: offset + resolvedOrigin, radius: radius);
    }

    private static double NormalizeProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static double? NormalizeRadius(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        double resolved = value.Value;
        if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
        {
            return null;
        }

        return resolved;
    }

    private static Point ResolveOrigin(Size size, Point origin)
    {
        var center = new Point(size.Width / 2, size.Height / 2);

        double x = double.IsNaN(origin.X) || double.IsInfinity(origin.X)
            ? center.X
            : origin.X;
        double y = double.IsNaN(origin.Y) || double.IsInfinity(origin.Y)
            ? center.Y
            : origin.Y;

        return new Point(x, y);
    }
}
