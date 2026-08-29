using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (Canvas-less drawing surface)

namespace Plumix.Rendering;

public class PaintingContext
{
    private readonly ContainerLayer _containerLayer;
    private PictureLayer? _currentPictureLayer;

    public PaintingContext(ContainerLayer containerLayer, Rect estimatedBounds = default)
    {
        _containerLayer = containerLayer;
        EstimatedBounds = estimatedBounds;
    }

    /// <summary>An estimate of the bounds within which this context's drawing takes place.</summary>
    /// <remarks>Flutter's <c>PaintingContext.estimatedBounds</c>.</remarks>
    public Rect EstimatedBounds { get; }

    /// <summary>Repaints the given render object, which must be a repaint boundary.</summary>
    /// <remarks>Flutter's <c>PaintingContext.repaintCompositedChild</c>.</remarks>
    public static void RepaintCompositedChild(RenderObject child, bool debugAlsoPaintedParent = false)
    {
        ArgumentNullException.ThrowIfNull(child);
        Debug.Assert(child.NeedsPaint);
        RepaintCompositedChildInternal(child, debugAlsoPaintedParent);
    }

    /// <remarks>Flutter's <c>PaintingContext._repaintCompositedChild</c>.</remarks>
    private static void RepaintCompositedChildInternal(
        RenderObject child,
        bool debugAlsoPaintedParent = false,
        PaintingContext? childContext = null)
    {
        Debug.Assert(child.IsRepaintBoundary);
        if (Constants.KDebugMode)
        {
            child.DebugRegisterRepaintBoundaryPaint(
                includedParent: debugAlsoPaintedParent,
                includedChild: true);
        }

        var childLayer = child._layer as OffsetLayer;
        if (childLayer is null)
        {
            Debug.Assert(debugAlsoPaintedParent);
            childLayer = child.UpdateCompositedLayerForRepaint();
        }
        else
        {
            Debug.Assert(debugAlsoPaintedParent || childLayer.Attached);
            Point debugOldOffset = childLayer.Offset;
            childLayer.RemoveAllChildren();
            OffsetLayer updatedLayer = child.UpdateCompositedLayerForRepaint();
            if (!ReferenceEquals(updatedLayer, childLayer))
            {
                throw new AssertionError(
                    $"{child} created a new layer instance {updatedLayer} instead of reusing the existing "
                    + $"layer {childLayer}. See the documentation of RenderObject.UpdateCompositedLayer "
                    + "for more information on how to correctly implement this method.");
            }

            Debug.Assert(debugOldOffset == updatedLayer.Offset);
        }

        if (Constants.KDebugMode)
        {
            childLayer.DebugCreator = child.DebugCreator ?? child.GetType();
        }

        childContext ??= new PaintingContext(childLayer, child.PaintBounds);
        child._paintWithContext(childContext, new Point(0, 0));
        Debug.Assert(ReferenceEquals(childLayer, child._layer));
        childContext.StopRecordingIfNeeded();
    }

    /// <summary>Re-runs the composited-layer update of a clean repaint boundary.</summary>
    /// <remarks>Flutter's <c>PaintingContext.updateLayerProperties</c>.</remarks>
    public static void UpdateLayerProperties(RenderObject child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Debug.Assert(child.IsRepaintBoundary && child._wasRepaintBoundary);
        Debug.Assert(!child.NeedsPaint);
        Debug.Assert(child._layer is not null);

        var childLayer = (OffsetLayer)child._layer!;
        Point debugOldOffset = childLayer.Offset;
        OffsetLayer updatedLayer = child.UpdateCompositedLayerForRepaint();
        if (!ReferenceEquals(updatedLayer, childLayer))
        {
            throw new AssertionError(
                $"{child} created a new layer instance {updatedLayer} instead of reusing the existing "
                + $"layer {childLayer}. See the documentation of RenderObject.UpdateCompositedLayer "
                + "for more information on how to correctly implement this method.");
        }

        Debug.Assert(debugOldOffset == updatedLayer.Offset);
    }

    /// <remarks>Flutter's <c>PaintingContext.debugInstrumentRepaintCompositedChild</c>.</remarks>
    public static void DebugInstrumentRepaintCompositedChild(
        RenderObject child,
        PaintingContext customContext,
        bool debugAlsoPaintedParent = false)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        RepaintCompositedChildInternal(child, debugAlsoPaintedParent, customContext);
    }

    public void PaintChild(RenderObject child, Point offset)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (Constants.KDebugMode)
        {
            RenderingDebug.OnProfilePaint?.Invoke(child);
        }

        if (child.IsRepaintBoundary)
        {
            StopRecordingIfNeeded();
            CompositeChild(child, offset);
        }
        else if (child._wasRepaintBoundary)
        {
            Debug.Assert(child._layer is OffsetLayer);
            child._layer = null;
            child._paintWithContext(this, offset);
        }
        else
        {
            child._paintWithContext(this, offset);
        }
    }

    /// <remarks>Flutter's <c>PaintingContext._compositeChild</c>.</remarks>
    private void CompositeChild(RenderObject child, Point offset)
    {
        Debug.Assert(child.IsRepaintBoundary);

        // Create a layer for our child, and paint the child into it.
        if (child.NeedsPaint || !child._wasRepaintBoundary)
        {
            RepaintCompositedChild(child, debugAlsoPaintedParent: true);
        }
        else
        {
            if (child.NeedsCompositedLayerUpdate)
            {
                UpdateLayerProperties(child);
            }

            if (Constants.KDebugMode)
            {
                child.DebugRegisterRepaintBoundaryPaint();
                if (child._layer is { } childDebugLayer)
                {
                    childDebugLayer.DebugCreator = child.DebugCreator ?? child;
                }
            }
        }

        Debug.Assert(child._layer is OffsetLayer);
        var childOffsetLayer = (OffsetLayer)child._layer!;
        childOffsetLayer.Offset = offset;
        AppendLayer(childOffsetLayer);
    }

    /// <summary>Adds a composited leaf layer to the recording.</summary>
    /// <remarks>Flutter's <c>PaintingContext.addLayer</c>.</remarks>
    public void AddLayer(Layer layer)
    {
        StopRecordingIfNeeded();
        AppendLayer(layer);
    }

    /// <summary>Appends the given layer, detaching it from any previous parent first.</summary>
    /// <remarks>Flutter's <c>PaintingContext.appendLayer</c>.</remarks>
    protected virtual void AppendLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        layer.Remove();
        _containerLayer.Append(layer);
    }

    /// <summary>Creates a painting context for a child layer.</summary>
    /// <remarks>Flutter's <c>PaintingContext.createChildContext</c>.</remarks>
    protected virtual PaintingContext CreateChildContext(ContainerLayer childLayer, Rect bounds)
    {
        return new PaintingContext(childLayer, bounds);
    }

    public void DrawRectangle(
        IBrush brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default,
        bool isAntiAlias = true)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedRect = new Rect(rect.Position + sceneOffset, rect.Size);
            using var renderOptions = drawingContext.PushRenderOptions(new RenderOptions
            {
                EdgeMode = isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
            });
            drawingContext.DrawRectangle(brush, pen, new RoundedRect(translatedRect, radiusX, radiusY), boxShadows);
        });
    }

    public void DrawRectangle(
        IBrush brush,
        IPen? pen,
        Rect rect,
        BorderRadius borderRadius,
        BoxShadows boxShadows = default)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedRect = new Rect(rect.Position + sceneOffset, rect.Size);
            var roundedRect = new RoundedRect(
                translatedRect,
                new Vector(borderRadius.TopLeftRadius.X, borderRadius.TopLeftRadius.Y),
                new Vector(borderRadius.TopRightRadius.X, borderRadius.TopRightRadius.Y),
                new Vector(borderRadius.BottomRightRadius.X, borderRadius.BottomRightRadius.Y),
                new Vector(borderRadius.BottomLeftRadius.X, borderRadius.BottomLeftRadius.Y));
            drawingContext.DrawRectangle(brush, pen, roundedRect, boxShadows);
        });
    }

    // Dart parity source: dart:ui Canvas.drawRRect.
    public void DrawRRect(Plumix.UI.RRect rrect, IBrush? brush, IPen? pen)
    {
        var path = new Plumix.UI.Path();
        path.AddRRect(rrect);
        DrawGeometry(brush, pen, path.ToGeometry());
    }

    // Dart parity source: dart:ui Canvas.drawRSuperellipse.
    public void DrawRSuperellipse(Plumix.UI.RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
    {
        var path = new Plumix.UI.Path();
        path.AddRSuperellipse(rsuperellipse);
        DrawGeometry(brush, pen, path.ToGeometry());
    }

    /// <summary>Draws a blurred box shadow using the exact rounded-superellipse contour.</summary>
    public void DrawRSuperellipseShadow(Plumix.UI.RSuperellipse rsuperellipse, BoxShadow shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        Plumix.UI.RSuperellipse shadowShape = rsuperellipse
            .Inflate(shadow.SpreadRadius)
            .Shift(shadow.Offset);
        Geometry geometry = shadowShape.ToPath().ToGeometry();
        if (shadow.BlurRadius <= 0.0)
        {
            DrawGeometry(new SolidColorBrush(shadow.Color), null, geometry);
            return;
        }

        // Avalonia's path API has no mask-filter paint. Concentric strokes sampled from the same
        // Gaussian falloff keep the superellipse contour exact while providing backend-independent blur.
        int steps = Math.Max(2, (int)Math.Ceiling(shadow.BlurRadius * 2.0));
        double sigma = shadow.BlurSigma;
        double outerRadius = Math.Max(shadow.BlurRadius, sigma * 3.0);
        double previousOpacity = 0.0;
        for (int step = 0; step < steps; step++)
        {
            double radius = outerRadius * (steps - step) / steps;
            double targetOpacity = Math.Exp(-(radius * radius) / (2.0 * sigma * sigma));
            double layerOpacity = 1.0 - ((1.0 - targetOpacity) / (1.0 - previousOpacity));
            previousOpacity = targetOpacity;
            byte layerAlpha = (byte)Math.Clamp(
                (int)Math.Round(shadow.Color.A * layerOpacity),
                0,
                byte.MaxValue);
            Color layerColor = Color.FromArgb(layerAlpha, shadow.Color.R, shadow.Color.G, shadow.Color.B);
            if (layerColor.A > 0)
            {
                DrawGeometry(null, new Pen(new SolidColorBrush(layerColor), radius * 2.0), geometry);
            }
        }

        DrawGeometry(new SolidColorBrush(shadow.Color), null, geometry);
    }

    // Dart parity source: dart:ui Canvas.drawDRRect (the ring between two rounded rectangles).
    public void DrawDRRect(Plumix.UI.RRect outer, Plumix.UI.RRect inner, IBrush brush)
    {
        var outerPath = new Plumix.UI.Path();
        outerPath.AddRRect(outer);
        var innerPath = new Plumix.UI.Path();
        innerPath.AddRRect(inner);
        DrawGeometry(
            brush,
            null,
            new CombinedGeometry(GeometryCombineMode.Exclude, outerPath.ToGeometry(), innerPath.ToGeometry()));
    }

    // Dart parity source: dart:ui Canvas.drawOval.
    public void DrawOval(Rect oval, IBrush? brush, IPen? pen)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translated = new Rect(oval.Position + sceneOffset, oval.Size);
            drawingContext.DrawEllipse(brush, pen, translated.Center, translated.Width / 2.0, translated.Height / 2.0);
        });
    }

    // Dart parity source: dart:ui Canvas.drawPath.
    public void DrawPath(Plumix.UI.Path path, IBrush? brush, IPen? pen)
    {
        ArgumentNullException.ThrowIfNull(path);
        DrawGeometry(brush, pen, path.ToGeometry());
    }

    public void DrawCircle(IBrush brush, IPen? pen, Point center, double radius)
    {
        double clampedRadius = Math.Max(0, radius);
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedCenter = center + sceneOffset;
            drawingContext.DrawEllipse(brush, pen, translatedCenter, clampedRadius, clampedRadius);
        });
    }

    public void DrawArc(IPen pen, Rect rect, double startAngleRadians, double sweepAngleRadians)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (Math.Abs(sweepAngleRadians) <= 0.0001)
        {
            return;
        }

        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedRect = new Rect(rect.Position + sceneOffset, rect.Size);
            if (translatedRect.Width <= 0 || translatedRect.Height <= 0)
            {
                return;
            }

            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                var startPoint = PointOnEllipse(translatedRect, startAngleRadians);
                var endPoint = PointOnEllipse(translatedRect, startAngleRadians + sweepAngleRadians);
                geometryContext.BeginFigure(startPoint, isFilled: false);
                geometryContext.ArcTo(
                    point: endPoint,
                    size: new Size(translatedRect.Width / 2.0, translatedRect.Height / 2.0),
                    rotationAngle: 0.0,
                    isLargeArc: Math.Abs(sweepAngleRadians) > Math.PI,
                    sweepDirection: sweepAngleRadians >= 0
                        ? SweepDirection.Clockwise
                        : SweepDirection.CounterClockwise);
                geometryContext.EndFigure(isClosed: false);
            }

            drawingContext.DrawGeometry(brush: null, pen: pen, geometry: geometry);
        });
    }

    public void DrawLine(IPen pen, Point startPoint, Point endPoint)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            drawingContext.DrawLine(pen, startPoint + sceneOffset, endPoint + sceneOffset);
        });
    }

    public void DrawPolygon(IBrush brush, IPen? pen, IReadOnlyList<Point> points)
    {
        if (points.Count < 3) return;
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(points[0] + sceneOffset, isFilled: true);
                for (int index = 1; index < points.Count; index++)
                {
                    geometryContext.LineTo(points[index] + sceneOffset);
                }
                geometryContext.EndFigure(isClosed: true);
            }
            drawingContext.DrawGeometry(brush, pen, geometry);
        });
    }

    public void DrawGeometry(
        IBrush? brush,
        IPen? pen,
        Geometry geometry,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            Point effectiveOffset = sceneOffset + geometryOffset;
            using var transform = drawingContext.PushTransform(Matrix.CreateTranslation(
                effectiveOffset.X,
                effectiveOffset.Y));
            drawingContext.DrawGeometry(brush, pen, geometry);
        });
    }

    public void DrawShadow(
        Geometry geometry,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (elevation <= 0.0 || color.A == 0)
        {
            return;
        }

        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            Point effectiveOffset = sceneOffset + geometryOffset + new Vector(0.0, elevation * 0.5);
            using var transform = drawingContext.PushTransform(Matrix.CreateTranslation(
                effectiveOffset.X,
                effectiveOffset.Y));
            int steps = Math.Max(1, (int)Math.Ceiling(elevation * 2.0));
            for (int step = steps; step >= 1; step--)
            {
                double fraction = step / (double)steps;
                byte alpha = (byte)Math.Clamp(
                    (int)Math.Round(color.A * 0.12 * (1.0 - (fraction * 0.75))),
                    1,
                    byte.MaxValue);
                var shadowBrush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                var shadowPen = new Pen(shadowBrush, step * 2.0);
                drawingContext.DrawGeometry(
                    transparentOccluder ? shadowBrush : null,
                    shadowPen,
                    geometry);
            }
        });
    }

    public void DrawTextLayout(TextLayout layout, Point point)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) => layout.Draw(drawingContext, point + sceneOffset));
    }

    public void DrawTextLayoutWithHorizontalFade(
        TextLayout layout,
        Point point,
        Rect bounds,
        bool fadeTowardRight)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedBounds = new Rect(bounds.Position + sceneOffset, bounds.Size);
            var mask = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = fadeTowardRight
                    ? new GradientStops
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Colors.White, 0.8),
                        new GradientStop(Colors.Transparent, 1),
                    }
                    : new GradientStops
                    {
                        new GradientStop(Colors.Transparent, 0),
                        new GradientStop(Colors.White, 0.2),
                        new GradientStop(Colors.White, 1),
                    },
            };
            using var clip = drawingContext.PushClip(translatedBounds);
            using var opacityMask = drawingContext.PushOpacityMask(mask, translatedBounds);
            layout.Draw(drawingContext, point + sceneOffset);
        });
    }

    public void DrawImage(
        IImage image,
        Rect sourceRect,
        Rect destinationRect,
        double opacity = 1.0,
        Rect? clipRect = null,
        BorderRadius? clipRadius = null,
        bool flipHorizontally = false,
        double? horizontalFlipAxisX = null,
        Rect? ovalClipRect = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0
            || destinationRect.Width <= 0 || destinationRect.Height <= 0)
        {
            return;
        }

        double effectiveOpacity = Math.Clamp(opacity, 0.0, 1.0);
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedDestination = new Rect(destinationRect.Position + sceneOffset, destinationRect.Size);
            DrawingContext.PushedState? clip = null;
            DrawingContext.PushedState? alpha = null;
            DrawingContext.PushedState? transform = null;
            DrawingContext.PushedState? renderOptions = null;
            try
            {
                if (ovalClipRect.HasValue)
                {
                    var translatedOval = new Rect(ovalClipRect.Value.Position + sceneOffset, ovalClipRect.Value.Size);
                    clip = drawingContext.PushGeometryClip(new EllipseGeometry(translatedOval));
                }
                else if (clipRect.HasValue)
                {
                    var translatedClip = new Rect(clipRect.Value.Position + sceneOffset, clipRect.Value.Size);
                    clip = clipRadius.HasValue && clipRadius.Value.Radius > 0
                        ? Layer.PushRoundedRectClip(drawingContext, translatedClip, clipRadius.Value.Radius)
                        : drawingContext.PushClip(translatedClip);
                }

                if (effectiveOpacity < 1.0)
                {
                    alpha = drawingContext.PushOpacity(effectiveOpacity);
                }

                renderOptions = drawingContext.PushRenderOptions(new RenderOptions
                {
                    BitmapInterpolationMode = filterQuality switch
                    {
                        FilterQuality.None => BitmapInterpolationMode.None,
                        FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                        FilterQuality.High => BitmapInterpolationMode.HighQuality,
                        _ => BitmapInterpolationMode.MediumQuality,
                    },
                    EdgeMode = isAntiAlias || ovalClipRect.HasValue || clipRadius?.Radius > 0
                        ? EdgeMode.Antialias
                        : EdgeMode.Aliased,
                    BitmapBlendingMode = blendMode,
                });

                if (flipHorizontally)
                {
                    double centerX = horizontalFlipAxisX.HasValue
                        ? horizontalFlipAxisX.Value + sceneOffset.X
                        : translatedDestination.Center.X;
                    transform = drawingContext.PushTransform(new Matrix(-1, 0, 0, 1, centerX * 2, 0));
                }

                drawingContext.DrawImage(image, sourceRect, translatedDestination);
            }
            finally
            {
                transform?.Dispose();
                renderOptions?.Dispose();
                alpha?.Dispose();
                clip?.Dispose();
            }
        });
    }

    public void PushClipRect(
        Rect clipRect,
        Action<PaintingContext> painter,
        Clip clipBehavior = Clip.HardEdge)
    {
        // Dart's `PaintingContext.pushClipRect` paints directly when there is
        // nothing to clip, rather than pushing a no-op clip layer.
        if (clipBehavior == Clip.None)
        {
            painter(this);
            return;
        }

        StopRecordingIfNeeded();

        var layer = new ClipRectLayer
        {
            ClipRect = clipRect,
            ClipBehavior = clipBehavior,
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushClipRRect(Rect clipRect, BorderRadius borderRadius, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new ClipRRectLayer
        {
            ClipRect = clipRect,
            BorderRadius = borderRadius
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushClipGeometry(
        Geometry geometry,
        Action<PaintingContext> painter,
        Clip clipBehavior = Clip.AntiAlias,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(painter);
        StopRecordingIfNeeded();

        var layer = new ClipGeometryLayer
        {
            Geometry = geometry,
            ClipBehavior = clipBehavior,
            GeometryOffset = geometryOffset,
        };
        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushClipPath(
        Plumix.UI.Path path,
        Action<PaintingContext> painter,
        Clip clipBehavior = Clip.AntiAlias,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        PushClipGeometry(path.ToGeometry(), painter, clipBehavior, geometryOffset);
    }

    public void PushTransform(Matrix4 transform, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new TransformLayer
        {
            Transform = transform
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushLayer(
        ContainerLayer layer,
        Action<PaintingContext> painter,
        Rect? childPaintBounds = null)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(painter);

        // If a layer is being reused it may already have children; remove them so `painter` can add
        // the children that are relevant for this frame.
        if (layer.HasChildren)
        {
            layer.RemoveAllChildren();
        }

        StopRecordingIfNeeded();
        AppendLayer(layer);

        PaintingContext childContext = CreateChildContext(layer, childPaintBounds ?? EstimatedBounds);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushOpacity(double opacity, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new OpacityLayer
        {
            Opacity = opacity
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public ColorFilterLayer PushColorFilter(
        Point offset,
        ColorFilter colorFilter,
        Action<PaintingContext> painter,
        ColorFilterLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(colorFilter);
        ArgumentNullException.ThrowIfNull(painter);
        StopRecordingIfNeeded();

        ColorFilterLayer layer = oldLayer ?? new ColorFilterLayer();
        layer.ColorFilter = colorFilter;
        PushLayer(layer, painter);
        return layer;
    }

    public void PushMagnifier(
        Rect lensRect,
        Point focalPointOffset,
        double magnificationScale,
        MagnifierDecoration decoration,
        Clip clipBehavior,
        Action<PaintingContext> painter)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        ArgumentNullException.ThrowIfNull(painter);
        StopRecordingIfNeeded();

        var layer = new MagnifierLayer
        {
            LensRect = lensRect,
            FocalPointOffset = focalPointOffset,
            MagnificationScale = magnificationScale,
            Decoration = decoration,
            ClipBehavior = clipBehavior,
        };
        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    private PictureLayer EnsurePictureLayer()
    {
        if (_currentPictureLayer != null)
        {
            return _currentPictureLayer;
        }

        _currentPictureLayer = new PictureLayer();
        _containerLayer.Append(_currentPictureLayer);
        return _currentPictureLayer;
    }

    /// <summary>Ends the current picture recording, if one is in progress.</summary>
    /// <remarks>
    /// Flutter's <c>PaintingContext.stopRecordingIfNeeded</c>. Plumix's <see cref="PictureLayer"/>
    /// accumulates draw commands instead of a <c>ui.Picture</c>, so "stopping" only means that the
    /// next draw call starts a fresh picture layer.
    /// </remarks>
    protected virtual void StopRecordingIfNeeded()
    {
        if (_currentPictureLayer is null)
        {
            return;
        }

        if (Constants.KDebugMode)
        {
            if (RenderingDebug.RepaintRainbowEnabled)
            {
                var pen = new Pen(new SolidColorBrush(RenderingDebug.CurrentRepaintColor.ToColor()), 6.0);
                DrawGeometry(null, pen, new RectangleGeometry(DeflateRect(EstimatedBounds, 3.0)));
            }

            if (RenderingDebug.PaintLayerBordersEnabled)
            {
                var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFFFF9800)), 1.0);
                DrawGeometry(null, pen, new RectangleGeometry(EstimatedBounds));
            }
        }

        _currentPictureLayer = null;
    }

    /// <remarks>Dart's <c>Rect.deflate</c>.</remarks>
    private static Rect DeflateRect(Rect rect, double delta)
    {
        return new Rect(
            rect.X + delta,
            rect.Y + delta,
            Math.Max(0.0, rect.Width - (delta * 2.0)),
            Math.Max(0.0, rect.Height - (delta * 2.0)));
    }

    private static Point PointOnEllipse(Rect rect, double angleRadians)
    {
        double centerX = rect.X + (rect.Width / 2.0);
        double centerY = rect.Y + (rect.Height / 2.0);
        double radiusX = rect.Width / 2.0;
        double radiusY = rect.Height / 2.0;
        return new Point(
            centerX + (Math.Cos(angleRadians) * radiusX),
            centerY + (Math.Sin(angleRadians) * radiusY));
    }
}
