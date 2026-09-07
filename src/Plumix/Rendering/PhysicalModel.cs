using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;
using Plumix.Foundation;
using Plumix.Painting;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart
// (_RenderPhysicalModelBase, RenderPhysicalModel, RenderPhysicalShape)

/// <summary>The elevation, colour and shadow shared by the two physical-model render objects.</summary>
/// <remarks>Flutter's private <c>_RenderPhysicalModelBase&lt;T&gt;</c>.</remarks>
public abstract class RenderPhysicalModelBase<T> : RenderCustomClip<T>
{
    private double _elevation;
    private Color _color;
    private Color _shadowColor;

    protected RenderPhysicalModelBase(
        RenderBox? child,
        double elevation,
        Color color,
        Color shadowColor,
        CustomClipper<T>? clipper = null,
        Clip clipBehavior = Clip.None) : base(child, clipper, clipBehavior)
    {
        ValidateElevation(elevation);
        _elevation = elevation;
        _color = color;
        _shadowColor = shadowColor;
    }

    /// <summary>The z-coordinate at which to place this material.</summary>
    public double Elevation
    {
        get => _elevation;
        set
        {
            ValidateElevation(value);
            if (_elevation.Equals(value))
            {
                return;
            }

            bool didNeedCompositing = AlwaysNeedsCompositing;
            _elevation = value;
            if (didNeedCompositing != AlwaysNeedsCompositing)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsPaint();
        }
    }

    /// <summary>The shadow colour.</summary>
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

    /// <summary>The background colour.</summary>
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

    /// <summary>
    /// Draws the elevation shadow of <paramref name="geometry"/>, honouring
    /// <see cref="RenderingDebug.DisableShadows"/>.
    /// </summary>
    /// <returns>Whether the caller should still paint its own surface fill.</returns>
    /// <remarks>Flutter inlines this in both <c>paint</c> methods.</remarks>
    private protected void PaintShadow(PaintingContext context, Path path, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableShadows)
        {
            if (_elevation > 0.0)
            {
                context.Canvas.DrawPath(
                    path,
                    brush: null,
                    pen: new Pen(new SolidColorBrush(_shadowColor), _elevation * 2.0));
            }

            return;
        }

        if (_elevation != 0.0)
        {
            context.Canvas.DrawShadow(
                path,
                _shadowColor,
                _elevation,
                transparentOccluder: _color.A != byte.MaxValue,
                geometryOffset: offset);
        }
    }

    private static void ValidateElevation(double elevation)
    {
        if (Constants.KDebugMode && !(elevation >= 0.0))
        {
            throw new AssertionError("Elevation must be non-negative.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter passes <c>color</c> (not <c>shadowColor</c>) as the value of the "shadowColor"
    /// property; the port keeps the upstream behaviour so diagnostics dumps match.
    /// </remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DoubleProperty("elevation", Elevation));
        description.Add(new ColorProperty("color", Color));
        description.Add(new ColorProperty("shadowColor", Color));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's physical render objects inherit <c>_RenderCustomClip.debugPaintSize</c>, which paints
    /// nothing of its own.
    /// </remarks>
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
    }
}

public sealed class RenderPhysicalModel : RenderPhysicalModelBase<RRect>
{
    private BoxShape _shape;
    private BorderRadius? _borderRadius;

    public RenderPhysicalModel(
        Color color,
        RenderBox? child = null,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        Color? shadowColor = null) : base(
            child,
            elevation,
            color,
            shadowColor ?? Colors.Black,
            clipper: null,
            clipBehavior: clipBehavior)
    {
        _shape = shape;
        _borderRadius = borderRadius;
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
            MarkNeedsClip();
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
            MarkNeedsClip();
        }
    }

    /// <inheritdoc />
    protected override RRect DefaultClip
    {
        get
        {
            var rect = new Rect(new Point(0, 0), Size);
            return _shape switch
            {
                BoxShape.Rectangle => (_borderRadius ?? Rendering.BorderRadius.Zero).ToRRect(rect),
                _ => RRect.FromRectXY(rect, rect.Width / 2.0, rect.Height / 2.0),
            };
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            RRect clip = EffectiveClip;
            if (!Rendering.Layer.ContainsRoundedRect(clip.Rect, clip.Radii, position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        RRect clip = EffectiveClip;
        var offsetPath = new Path();
        offsetPath.AddRRect(clip.Shift(offset));
        PaintShadow(context, offsetPath, new Point(0, 0));

        bool usesSaveLayer = ClipBehavior == Clip.AntiAliasWithSaveLayer;
        if (!usesSaveLayer)
        {
            context.Canvas.DrawPath(offsetPath, new SolidColorBrush(Color), pen: null);
        }

        Layer = context.PushClipRRect(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            clip,
            (clippedContext, clippedOffset) =>
            {
                if (usesSaveLayer)
                {
                    // Dart fills the whole clip with `Canvas.drawPaint`; inside the pushed clip,
                    // filling the layout box is the same region.
                    clippedContext.Canvas.DrawRectangle(
                        new SolidColorBrush(Color),
                        null,
                        new Rect(clippedOffset, Size));
                }

                base.Paint(clippedContext, clippedOffset);
            },
            ClipBehavior,
            Layer as ClipRRectLayer);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DiagnosticsProperty<BoxShape>("shape", Shape));
        description.Add(new DiagnosticsProperty<BorderRadius?>("borderRadius", BorderRadius));
    }
}

public sealed class RenderPhysicalShape : RenderPhysicalModelBase<Path>
{
    public RenderPhysicalShape(
        CustomClipper<Path> clipper,
        Color color,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None,
        double elevation = 0.0,
        Color? shadowColor = null) : base(
            child,
            elevation,
            color,
            shadowColor ?? Colors.Black,
            clipper: clipper ?? throw new ArgumentNullException(nameof(clipper)),
            clipBehavior: clipBehavior)
    {
    }

    /// <inheritdoc />
    protected override Path DefaultClip
    {
        get
        {
            var path = new Path();
            path.AddRect(new Rect(default, Size));
            return path;
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null && !EffectiveClip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        Path clip = EffectiveClip;
        Path offsetPath = clip.Shift(offset);
        PaintShadow(context, offsetPath, new Point(0, 0));

        bool usesSaveLayer = ClipBehavior == Clip.AntiAliasWithSaveLayer;
        if (!usesSaveLayer)
        {
            context.Canvas.DrawPath(offsetPath, new SolidColorBrush(Color), pen: null);
        }

        Layer = context.PushClipPath(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            clip,
            (clippedContext, clippedOffset) =>
            {
                if (usesSaveLayer)
                {
                    clippedContext.Canvas.DrawRectangle(
                        new SolidColorBrush(Color),
                        null,
                        new Rect(clippedOffset, Size));
                }

                base.Paint(clippedContext, clippedOffset);
            },
            ClipBehavior,
            Layer as ClipPathLayer);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DiagnosticsProperty<CustomClipper<Path>>("clipper", Clipper));
    }
}
