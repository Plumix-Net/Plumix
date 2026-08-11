using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

namespace Plumix.Rendering;

public sealed class PaintingContext
{
    private readonly ContainerLayer _containerLayer;
    private PictureLayer? _currentPictureLayer;

    public PaintingContext(ContainerLayer containerLayer)
    {
        _containerLayer = containerLayer;
    }

    public void PaintChild(RenderObject child, Point offset)
    {
        if (child.IsRepaintBoundary)
        {
            StopRecordingIfNeeded();

            var oldLayer = child._layer as OffsetLayer;
            var layer = child.EnsureCompositedLayer();
            bool shouldRepaint = child.NeedsPaint || oldLayer == null || !ReferenceEquals(oldLayer, layer);
            layer.Offset = offset;
            _containerLayer.Append(layer);
            child._layer = layer;

            if (shouldRepaint)
            {
                child.UpdateCompositedLayerProperties();
                layer.RemoveAllChildren();
                var childContext = new PaintingContext(layer);
                child._paintWithContext(childContext, new Point(0, 0));
            }
            else if (child.NeedsCompositedLayerUpdate)
            {
                child.UpdateCompositedLayerProperties();
            }
        }
        else if (child._wasRepaintBoundary)
        {
            child._layer = null;
            child._paintWithContext(this, offset);
        }
        else
        {
            child._paintWithContext(this, offset);
        }
    }

    public void DrawRectangle(
        IBrush brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedRect = new Rect(rect.Position + sceneOffset, rect.Size);
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

    public void PushTransform(Matrix transform, Action<PaintingContext> painter)
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

    public void PushLayer(ContainerLayer layer, Action<PaintingContext> painter)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(painter);
        StopRecordingIfNeeded();

        layer.RemoveAllChildren();
        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
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

    private void StopRecordingIfNeeded()
    {
        _currentPictureLayer = null;
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
