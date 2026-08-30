using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;
using Plumix.Foundation;
using Plumix.Painting;

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
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        var rect = new Rect(new Point(0, 0), Size);
        double radius = _shape == BoxShape.Circle
            ? Math.Min(rect.Width, rect.Height) / 2.0
            : Math.Min(_borderRadius?.Radius ?? 0.0, Math.Min(rect.Width, rect.Height) / 2.0);
        BoxShadows shadows = BuildShadow();
        context.Canvas.DrawRectangle(
            new SolidColorBrush(_color),
            null,
            new Rect(rect.Position + offset, rect.Size),
            radius,
            radius,
            shadows);

        if (_clipBehavior == Clip.None)
        {
            Layer = null;
            context.PaintChild(Child, offset);
            return;
        }

        if (_shape == BoxShape.Circle)
        {
            var ovalPath = new Plumix.UI.Path();
            ovalPath.AddOval(rect);
            Layer = context.PushClipPath(
                NeedsCompositing,
                offset,
                rect,
                ovalPath,
                (clippedContext, clippedOffset) => clippedContext.PaintChild(Child, clippedOffset),
                _clipBehavior,
                Layer as ClipPathLayer);
            return;
        }

        Layer = context.PushClipRRect(
            NeedsCompositing,
            offset,
            rect,
            RRect.FromRectAndCorners(rect, _borderRadius ?? Plumix.Rendering.BorderRadius.Zero),
            (clippedContext, clippedOffset) => clippedContext.PaintChild(Child, clippedOffset),
            _clipBehavior,
            Layer as ClipRRectLayer);
    }

    private BoxShadows BuildShadow()
    {
        if (_elevation <= 0.0 || _shadowColor.A == 0)
        {
            return default;
        }

        if (Constants.KDebugMode && RenderingDebug.DisablePhysicalShapeLayers)
        {
            return default;
        }

        var shadow = new BoxShadow(
            color: _shadowColor,
            offset: new Point(0.0, _elevation * 0.5),
            blurRadius: Math.Max(1.0, _elevation * 2.0));
        return new BoxShadows(shadow.ToAvalonia());
    }

    private static void ValidateElevation(double elevation)
    {
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DoubleProperty("elevation", Elevation));
        description.Add(new ColorProperty("color", Color));
        description.Add(new ColorProperty("shadowColor", Color));
        description.Add(new DiagnosticsProperty<BoxShape>("shape", Shape));
        description.Add(new DiagnosticsProperty<BorderRadius?>("borderRadius", BorderRadius));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderPhysicalShape)
public sealed class RenderPhysicalShape : RenderCustomClip<Path>
{
    private double _elevation;
    private Color _color;
    private Color _shadowColor;

    public RenderPhysicalShape(
        CustomClipper<Path> clipper,
        Color color,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None,
        double elevation = 0.0,
        Color? shadowColor = null) : base(
            child: child,
            clipper: clipper ?? throw new ArgumentNullException(nameof(clipper)),
            clipBehavior: clipBehavior)
    {
        ValidateElevation(elevation);
        _elevation = elevation;
        _color = color;
        _shadowColor = shadowColor ?? Colors.Black;
    }

    public double Elevation
    {
        get => _elevation;
        set
        {
            ValidateElevation(value);
            if (_elevation == value)
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

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        Path clip = EffectiveClip;
        context.Canvas.DrawGeometry(null, RenderCustomClipDebug.DebugPen, clip.ToGeometry(), geometryOffset: offset);
        RenderCustomClipDebug.PaintScissors(context, offset, clip.GetBounds().Width);
    }

    protected override Path DefaultClip
    {
        get
        {
            var path = new Path();
            path.AddRect(new Rect(default, Size));
            return path;
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null && !EffectiveClip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        Plumix.UI.Path clip = EffectiveClip;
        Geometry geometry = clip.ToGeometry();
        bool physicalShapesDisabled = Constants.KDebugMode && RenderingDebug.DisablePhysicalShapeLayers;
        if (_elevation > 0.0 && _shadowColor.A > 0 && !physicalShapesDisabled)
        {
            context.Canvas.DrawShadow(
                geometry,
                _shadowColor,
                _elevation,
                transparentOccluder: _color.A != byte.MaxValue,
                geometryOffset: offset);
        }

        context.Canvas.DrawGeometry(
            new SolidColorBrush(_color),
            null,
            geometry,
            geometryOffset: offset);
        if (ClipBehavior == Clip.None)
        {
            Layer = null;
            base.Paint(context, offset);
            return;
        }

        Layer = context.PushClipPath(
            NeedsCompositing,
            offset,
            clip.GetBounds(),
            clip,
            base.Paint,
            ClipBehavior,
            Layer as ClipPathLayer);
    }

    private static void ValidateElevation(double elevation)
    {
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DoubleProperty("elevation", Elevation));
        description.Add(new ColorProperty("color", Color));
        description.Add(new ColorProperty("shadowColor", Color));
        description.Add(new DiagnosticsProperty<CustomClipper<Path>>("clipper", Clipper));
    }
}
