using Avalonia;
using Avalonia.Media;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderPhysicalModel)

public sealed class RenderPhysicalModel : RenderProxyBox
{
    private BoxShape _shape;
    private Clip _clipBehavior;
    private BorderRadius? _borderRadius;
    private double _elevation;
    private Color _color;
    private Color _shadowColor;

    public RenderPhysicalModel(
        Color color,
        RenderBox? child = null,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        Color? shadowColor = null)
    {
        ValidateElevation(elevation);
        _color = color;
        _shape = shape;
        _clipBehavior = clipBehavior;
        _borderRadius = borderRadius;
        _elevation = elevation;
        _shadowColor = shadowColor ?? Colors.Black;
        Child = child;
    }

    public BoxShape Shape
    {
        get => _shape;
        set
        {
            if (_shape == value)
            {
                return;
            }

            _shape = value;
            MarkNeedsPaint();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
        }
    }

    public BorderRadius? BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            MarkNeedsPaint();
        }
    }

    public double Elevation
    {
        get => _elevation;
        set
        {
            ValidateElevation(value);
            if (Math.Abs(_elevation - value) < 0.0001)
            {
                return;
            }

            _elevation = value;
            MarkNeedsPaint();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public Color ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor == value)
            {
                return;
            }

            _shadowColor = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        var rect = new Rect(offset, Size);
        double radius = _shape == BoxShape.Circle
            ? Math.Min(rect.Width, rect.Height) / 2.0
            : Math.Min(_borderRadius?.Radius ?? 0.0, Math.Min(rect.Width, rect.Height) / 2.0);
        BoxShadows shadows = BuildShadow();
        context.DrawRectangle(
            new SolidColorBrush(_color),
            null,
            rect,
            radius,
            radius,
            shadows);

        if (_clipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            return;
        }

        if (_shape == BoxShape.Circle)
        {
            context.PushClipGeometry(
                new EllipseGeometry(rect),
                clippedContext => clippedContext.PaintChild(Child, offset));
            return;
        }

        context.PushClipRRect(
            rect,
            _borderRadius ?? Plumix.Rendering.BorderRadius.Zero,
            clippedContext => clippedContext.PaintChild(Child, offset));
    }

    private BoxShadows BuildShadow()
    {
        if (_elevation <= 0.0 || _shadowColor.A == 0)
        {
            return default;
        }

        var shadow = new BoxShadow
        {
            OffsetX = 0.0,
            OffsetY = _elevation * 0.5,
            Blur = Math.Max(1.0, _elevation * 2.0),
            Spread = 0.0,
            Color = _shadowColor,
            IsInset = false,
        };
        return new BoxShadows(shadow);
    }

    private static void ValidateElevation(double elevation)
    {
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }
    }
}
