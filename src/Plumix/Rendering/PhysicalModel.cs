using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart
// (_RenderPhysicalModelBase, RenderPhysicalModel, RenderPhysicalShape)

/// <summary>A physical model layer casts a shadow based on its <see cref="Elevation"/>.</summary>
/// <remarks>
/// Flutter's private <c>_RenderPhysicalModelBase&lt;T&gt;</c>; public only because its public subclasses
/// require it, with a <c>private protected</c> constructor so it cannot be extended outside this assembly.
/// </remarks>
public abstract class RenderPhysicalModelBase<T> : RenderCustomClip<T>
{
    private double _elevation;
    private Color _shadowColor;
    private Color _color;

    /// <summary>The <paramref name="elevation"/> parameter must be non-negative.</summary>
    private protected RenderPhysicalModelBase(
        RenderBox? child,
        double elevation,
        Color color,
        Color shadowColor,
        Clip clipBehavior = Clip.None,
        CustomClipper<T>? clipper = null) : base(child, clipper, clipBehavior)
    {
        Debug.Assert(elevation >= 0.0);
        _elevation = elevation;
        _color = color;
        _shadowColor = shadowColor;
    }

    /// <summary>The z-coordinate relative to the parent at which to place this material.</summary>
    public double Elevation
    {
        get => _elevation;
        set
        {
            Debug.Assert(value >= 0.0);
            if (Elevation == value)
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

    /// <summary>The shadow color.</summary>
    public Color ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (ShadowColor == value)
            {
                return;
            }

            _shadowColor = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>The background color.</summary>
    public Color Color
    {
        get => _color;
        set
        {
            if (Color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
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
}

/// <summary>Creates a physical model layer that clips its child to a rounded rectangle.</summary>
public class RenderPhysicalModel : RenderPhysicalModelBase<RRect>
{
    private BoxShape _shape;
    private BorderRadius? _borderRadius;

    /// <summary>Creates a rounded-rectangular clip.</summary>
    /// <remarks>
    /// Dart's <c>shadowColor</c> defaults to <c>const Color(0xFF000000)</c>; C# cannot use a non-constant
    /// struct default, so <see langword="null"/> stands for that value.
    /// </remarks>
    public RenderPhysicalModel(
        Color color,
        RenderBox? child = null,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        Color? shadowColor = null) : base(
            child: child,
            elevation: elevation,
            color: color,
            shadowColor: shadowColor ?? Color.FromUInt32(0xFF000000),
            clipBehavior: clipBehavior)
    {
        Debug.Assert(elevation >= 0.0);
        _shape = shape;
        _borderRadius = borderRadius;
    }

    /// <summary>The shape of the layer.</summary>
    public BoxShape Shape
    {
        get => _shape;
        set
        {
            if (Shape == value)
            {
                return;
            }

            _shape = value;
            MarkNeedsClip();
        }
    }

    /// <summary>The border radius of the rounded corners.</summary>
    public BorderRadius? BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (BorderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            MarkNeedsClip();
        }
    }

    private protected override RRect DefaultClip
    {
        get
        {
            Debug.Assert(HasSize);
            var rect = new Rect(new Point(0, 0), Size);
            return _shape switch
            {
                BoxShape.Rectangle => (BorderRadius ?? Rendering.BorderRadius.Zero).ToRRect(rect),
                BoxShape.Circle => RRect.FromRectXY(rect, rect.Width / 2, rect.Height / 2),
                _ => throw new InvalidOperationException($"Unknown BoxShape {_shape}."),
            };
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.Contains(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            Layer = null;
            return;
        }

        UpdateClip();
        RRect offsetRRect = _clip.Shift(offset);
        var offsetRRectAsPath = new Path();
        offsetRRectAsPath.AddRRect(offsetRRect);
        bool paintShadows = true;
        if (Constants.KDebugMode)
        {
            if (RenderingDebug.DisableShadows)
            {
                if (Elevation > 0.0)
                {
                    context.Canvas.DrawRRect(
                        offsetRRect,
                        brush: null,
                        pen: new Pen(new SolidColorBrush(ShadowColor), Elevation * 2.0));
                }

                paintShadows = false;
            }
        }

        Canvas canvas = context.Canvas;
        if (Elevation != 0.0 && paintShadows)
        {
            canvas.DrawShadow(offsetRRectAsPath, ShadowColor, Elevation, Color.A != 0xFF);
        }

        bool usesSaveLayer = ClipBehavior == Clip.AntiAliasWithSaveLayer;
        if (!usesSaveLayer)
        {
            canvas.DrawRRect(offsetRRect, new SolidColorBrush(Color), pen: null);
        }

        Layer = context.PushClipRRect(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            _clip,
            (clippedContext, clippedOffset) =>
            {
                if (usesSaveLayer)
                {
                    // If we want to avoid the bleeding edge artifact
                    // (https://github.com/flutter/flutter/issues/18057#issue-328003931)
                    // using saveLayer, we have to call drawPaint instead of drawPath as
                    // anti-aliased drawPath will always have such artifacts.
                    clippedContext.Canvas.DrawPaint(new SolidColorBrush(Color));
                }

                base.Paint(clippedContext, clippedOffset);
            },
            oldLayer: Layer as ClipRRectLayer,
            clipBehavior: ClipBehavior);

        if (Constants.KDebugMode && Layer is not null)
        {
            Layer.DebugCreator = DebugCreator;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DiagnosticsProperty<BoxShape>("shape", Shape));
        description.Add(new DiagnosticsProperty<BorderRadius?>("borderRadius", BorderRadius));
    }
}

/// <summary>Creates a physical shape layer that clips its child to a <see cref="Path"/>.</summary>
public class RenderPhysicalShape : RenderPhysicalModelBase<Path>
{
    /// <summary>Creates an arbitrary shape clip.</summary>
    /// <remarks>
    /// Dart's <c>shadowColor</c> defaults to <c>const Color(0xFF000000)</c>; C# cannot use a non-constant
    /// struct default, so <see langword="null"/> stands for that value.
    /// </remarks>
    public RenderPhysicalShape(
        CustomClipper<Path> clipper,
        Color color,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None,
        double elevation = 0.0,
        Color? shadowColor = null) : base(
            child: child,
            elevation: elevation,
            color: color,
            shadowColor: shadowColor ?? Color.FromUInt32(0xFF000000),
            clipBehavior: clipBehavior,
            clipper: clipper)
    {
        Debug.Assert(elevation >= 0.0);
    }

    private protected override Path DefaultClip
    {
        get
        {
            var path = new Path();
            path.AddRect(new Rect(new Point(0, 0), Size));
            return path;
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.Contains(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            Layer = null;
            return;
        }

        UpdateClip();
        Path offsetPath = _clip.Shift(offset);
        bool paintShadows = true;
        if (Constants.KDebugMode)
        {
            if (RenderingDebug.DisableShadows)
            {
                if (Elevation > 0.0)
                {
                    context.Canvas.DrawPath(
                        offsetPath,
                        brush: null,
                        pen: new Pen(new SolidColorBrush(ShadowColor), Elevation * 2.0));
                }

                paintShadows = false;
            }
        }

        Canvas canvas = context.Canvas;
        if (Elevation != 0.0 && paintShadows)
        {
            canvas.DrawShadow(offsetPath, ShadowColor, Elevation, Color.A != 0xFF);
        }

        bool usesSaveLayer = ClipBehavior == Clip.AntiAliasWithSaveLayer;
        if (!usesSaveLayer)
        {
            canvas.DrawPath(offsetPath, new SolidColorBrush(Color), pen: null);
        }

        Layer = context.PushClipPath(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            _clip,
            (clippedContext, clippedOffset) =>
            {
                if (usesSaveLayer)
                {
                    // If we want to avoid the bleeding edge artifact
                    // (https://github.com/flutter/flutter/issues/18057#issue-328003931)
                    // using saveLayer, we have to call drawPaint instead of drawPath as
                    // anti-aliased drawPath will always have such artifacts.
                    clippedContext.Canvas.DrawPaint(new SolidColorBrush(Color));
                }

                base.Paint(clippedContext, clippedOffset);
            },
            oldLayer: Layer as ClipPathLayer,
            clipBehavior: ClipBehavior);

        if (Constants.KDebugMode && Layer is not null)
        {
            Layer.DebugCreator = DebugCreator;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DiagnosticsProperty<CustomClipper<Path>>("clipper", Clipper));
    }
}
