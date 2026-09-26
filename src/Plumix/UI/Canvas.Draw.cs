using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas (the drawing half; Avalonia brushes/pens stand in for `Paint`)

public sealed partial class Canvas
{
    // Dart parity source: dart:ui Canvas.drawRect / Canvas.drawRRect.
    public void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default,
        bool isAntiAlias = true)
    {
        DebugRecordCall(radiusX == 0 && radiusY == 0
            ? new CanvasCall("drawRect", Rect: rect, Brush: brush, Pen: pen)
            : new CanvasCall(
                "drawRRect",
                RRect: RRect.FromRectXY(rect, radiusX, radiusY),
                Brush: brush,
                Pen: pen));
        AddDrawCommand(context =>
        {
            using var renderOptions = context.PushRenderOptions(new RenderOptions
            {
                EdgeMode = isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
            });
            context.DrawRectangle(brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        });
    }

    // Dart parity source: dart:ui Canvas.drawRRect (corner radii given as a BorderRadius).
    public void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        BorderRadius borderRadius,
        BoxShadows boxShadows = default)
    {
        DebugRecordCall(new CanvasCall(
            "drawRRect",
            RRect: RRect.FromRectAndCorners(rect, borderRadius),
            Brush: brush,
            Pen: pen));
        AddDrawCommand(context =>
        {
            var roundedRect = new RoundedRect(
                rect,
                new Vector(borderRadius.TopLeftRadius.X, borderRadius.TopLeftRadius.Y),
                new Vector(borderRadius.TopRightRadius.X, borderRadius.TopRightRadius.Y),
                new Vector(borderRadius.BottomRightRadius.X, borderRadius.BottomRightRadius.Y),
                new Vector(borderRadius.BottomLeftRadius.X, borderRadius.BottomLeftRadius.Y));
            context.DrawRectangle(brush, pen, roundedRect, boxShadows);
        });
    }

    /// <summary>Draws a rectangle with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawRect(rect, paint)</c>. The paint's colour (or shader), style, stroke width
    /// and mask filter are honoured; a <see cref="MaskFilter"/> blur is drawn with the same Gaussian
    /// ring technique as <see cref="DrawRSuperellipseBlur"/>, because Avalonia's drawing context has no
    /// mask-filter paint (<c>BlurStyle.outer</c>/<c>inner</c> are drawn as <c>normal</c>).
    /// </remarks>
    public void DrawRect(Rect rect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        IBrush brush = paint.Shader ?? new SolidColorBrush(paint.Color);
        bool stroke = paint.Style == PaintingStyle.Stroke;
        IPen? pen = stroke
            ? new Pen(
                brush,
                paint.StrokeWidth,
                lineCap: ToPenLineCap(paint.StrokeCap),
                lineJoin: ToPenLineJoin(paint.StrokeJoin))
            : null;
        DebugRecordCall(new CanvasCall(
            "drawRect",
            Rect: rect,
            Brush: stroke ? null : brush,
            Pen: pen,
            MaskFilter: paint.MaskFilter));
        if (paint.MaskFilter is { } maskFilter && !stroke)
        {
            var path = new Path();
            path.AddRect(rect);
            DrawBlurredGeometry(path.ToGeometry(), paint.Color, maskFilter.Sigma);
            return;
        }

        AddDrawCommand(context => context.DrawRectangle(stroke ? null : brush, pen, rect));
    }

    private static PenLineCap ToPenLineCap(StrokeCap cap) => cap switch
    {
        StrokeCap.Round => PenLineCap.Round,
        StrokeCap.Square => PenLineCap.Square,
        _ => PenLineCap.Flat,
    };

    private static PenLineJoin ToPenLineJoin(StrokeJoin join) => join switch
    {
        StrokeJoin.Round => PenLineJoin.Round,
        StrokeJoin.Bevel => PenLineJoin.Bevel,
        _ => PenLineJoin.Miter,
    };

    // Dart parity source: dart:ui Canvas.drawPaint.
    /// <summary>Fills the canvas's current clip with the given brush. Avalonia exposes no clip-bounds
    /// query, so the fill is a rectangle large enough to cover any practical clip.</summary>
    public void DrawPaint(IBrush brush)
    {
        AddDrawCommand(context => context.DrawRectangle(brush, null, DrawPaintBounds));
    }

    private static readonly Rect DrawPaintBounds = new(-1.0e9, -1.0e9, 2.0e9, 2.0e9);

    // Dart parity source: dart:ui Canvas.drawRRect.
    public void DrawRRect(RRect rrect, IBrush? brush, IPen? pen)
    {
        DebugRecordCall(new CanvasCall("drawRRect", RRect: rrect, Brush: brush, Pen: pen));
        var path = new Path();
        path.AddRRect(rrect);
        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(brush, pen, geometry ??= path.ToGeometry()));
    }

    /// <summary>Draws a rounded rectangle with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawRRect(rrect, paint)</c>. Like <see cref="DrawRect(Rect, Paint)"/>, the paint's
    /// colour (or shader), style, stroke width and mask filter are honoured; a blur mask filter is drawn
    /// with the Gaussian ring technique.
    /// </remarks>
    public void DrawRRect(RRect rrect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        IBrush brush = paint.Shader ?? new SolidColorBrush(paint.Color);
        bool stroke = paint.Style == PaintingStyle.Stroke;
        IPen? pen = stroke
            ? new Pen(
                brush,
                paint.StrokeWidth,
                lineCap: ToPenLineCap(paint.StrokeCap),
                lineJoin: ToPenLineJoin(paint.StrokeJoin))
            : null;
        DebugRecordCall(new CanvasCall(
            "drawRRect",
            RRect: rrect,
            Brush: stroke ? null : brush,
            Pen: pen,
            MaskFilter: paint.MaskFilter));
        var path = new Path();
        path.AddRRect(rrect);
        if (paint.MaskFilter is { } maskFilter && !stroke)
        {
            DrawBlurredGeometry(path.ToGeometry(), paint.Color, maskFilter.Sigma);
            return;
        }

        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(
            stroke ? null : brush,
            pen,
            geometry ??= path.ToGeometry()));
    }

    // Dart parity source: dart:ui Canvas.drawRSuperellipse.
    public void DrawRSuperellipse(RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
    {
        var path = new Path();
        path.AddRSuperellipse(rsuperellipse);
        DrawPath(path, brush, pen);
    }

    /// <summary>Draws a blurred box shadow using the exact rounded-superellipse contour.</summary>
    public void DrawRSuperellipseShadow(RSuperellipse rsuperellipse, Plumix.Rendering.BoxShadow shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        RSuperellipse shadowShape = rsuperellipse
            .Inflate(shadow.SpreadRadius)
            .Shift(shadow.Offset);
        DrawRSuperellipseBlur(shadowShape, shadow.Color, shadow.BlurSigma);
    }

    /// <summary>
    /// Fills <paramref name="rsuperellipse"/> with <paramref name="color"/> under a Gaussian blur of
    /// <paramref name="blurSigma"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawRSuperellipse</c> with a <c>Paint.maskFilter</c> of
    /// <c>MaskFilter.blur(BlurStyle.normal, sigma)</c>. Avalonia's path API has no mask-filter paint,
    /// so concentric strokes sampled from the same Gaussian falloff keep the superellipse contour
    /// exact while providing a backend-independent blur.
    /// </remarks>
    public void DrawRSuperellipseBlur(RSuperellipse rsuperellipse, Color color, double blurSigma)
    {
        Geometry geometry = rsuperellipse.ToPath().ToGeometry();
        DrawBlurredGeometry(geometry, color, blurSigma);
    }

    // Concentric strokes sampled from one Gaussian falloff: the contour stays exact while the blur is
    // backend-independent; the shape itself is filled last, under the innermost ring.
    private void DrawBlurredGeometry(Geometry geometry, Color color, double blurSigma)
    {
        if (blurSigma <= 0.0)
        {
            DrawGeometry(new SolidColorBrush(color), null, geometry);
            return;
        }

        double outerRadius = blurSigma * 3.0;
        int steps = Math.Max(2, (int)Math.Ceiling(outerRadius));
        double previousOpacity = 0.0;
        for (int step = 0; step < steps; step++)
        {
            double radius = outerRadius * (steps - step) / steps;
            double targetOpacity = Math.Exp(-(radius * radius) / (2.0 * blurSigma * blurSigma));
            double layerOpacity = 1.0 - ((1.0 - targetOpacity) / (1.0 - previousOpacity));
            previousOpacity = targetOpacity;
            byte layerAlpha = (byte)Math.Clamp(
                (int)Math.Round(color.Alpha * layerOpacity),
                0,
                byte.MaxValue);
            Color layerColor = Color.FromARGB(layerAlpha, color.Red, color.Green, color.Blue);
            if (layerColor.Alpha > 0)
            {
                DrawGeometry(null, new Pen(new SolidColorBrush(layerColor), radius * 2.0), geometry);
            }
        }

        DrawGeometry(new SolidColorBrush(color), null, geometry);
    }

    // Dart parity source: dart:ui Canvas.drawDRRect (the ring between two rounded rectangles).
    public void DrawDRRect(RRect outer, RRect inner, IBrush brush)
    {
        var outerPath = new Path();
        outerPath.AddRRect(outer);
        var innerPath = new Path();
        innerPath.AddRRect(inner);
        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(
            brush,
            null,
            geometry ??= new CombinedGeometry(
                GeometryCombineMode.Exclude,
                outerPath.ToGeometry(),
                innerPath.ToGeometry())));
    }

    // Dart parity source: dart:ui Canvas.drawOval.
    public void DrawOval(Rect oval, IBrush? brush, IPen? pen)
    {
        DebugRecordCall(new CanvasCall("drawOval", Rect: oval, Brush: brush, Pen: pen));
        AddDrawCommand(context =>
            context.DrawEllipse(brush, pen, oval.Center, oval.Width / 2.0, oval.Height / 2.0));
    }

    // Dart parity source: dart:ui Canvas.drawPath.
    public void DrawPath(Path path, IBrush? brush, IPen? pen)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("drawPath", Brush: brush, Pen: pen, Path: path));

        // The backend geometry is built on playback: recording must not need a render backend.
        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(brush, pen, geometry ??= path.ToGeometry()));
    }

    // Dart parity source: dart:ui Canvas.drawCircle.
    public void DrawCircle(IBrush? brush, IPen? pen, Point center, double radius)
    {
        DebugRecordCall(new CanvasCall("drawCircle", Brush: brush, Pen: pen, Center: center, Radius: radius));
        double clampedRadius = Math.Max(0, radius);
        AddDrawCommand(context => context.DrawEllipse(brush, pen, center, clampedRadius, clampedRadius));
    }

    // Dart parity source: dart:ui Canvas.drawArc.
    public void DrawArc(IPen pen, Rect rect, double startAngleRadians, double sweepAngleRadians)
    {
        DebugRecordCall(new CanvasCall("drawArc", Rect: rect, Pen: pen));
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (Math.Abs(sweepAngleRadians) <= 0.0001)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                Point startPoint = PointOnEllipse(rect, startAngleRadians);
                Point endPoint = PointOnEllipse(rect, startAngleRadians + sweepAngleRadians);
                geometryContext.BeginFigure(startPoint, isFilled: false);
                geometryContext.ArcTo(
                    point: endPoint,
                    size: new Size(rect.Width / 2.0, rect.Height / 2.0),
                    rotationAngle: 0.0,
                    isLargeArc: Math.Abs(sweepAngleRadians) > Math.PI,
                    sweepDirection: sweepAngleRadians >= 0
                        ? SweepDirection.Clockwise
                        : SweepDirection.CounterClockwise);
                geometryContext.EndFigure(isClosed: false);
            }

            context.DrawGeometry(brush: null, pen: pen, geometry: geometry);
        });
    }

    // Dart parity source: dart:ui Canvas.drawLine.
    public void DrawLine(IPen pen, Point startPoint, Point endPoint)
    {
        DebugRecordCall(new CanvasCall("drawLine", Pen: pen, Offset: startPoint, EndOffset: endPoint));
        AddDrawCommand(context => context.DrawLine(pen, startPoint, endPoint));
    }

    // Dart parity source: dart:ui Canvas.drawPath over a closed polygon contour.
    public void DrawPolygon(IBrush? brush, IPen? pen, IReadOnlyList<Point> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(points[0], isFilled: true);
                for (int index = 1; index < points.Count; index++)
                {
                    geometryContext.LineTo(points[index]);
                }

                geometryContext.EndFigure(isClosed: true);
            }

            context.DrawGeometry(brush, pen, geometry);
        });
    }

    /// <summary>Plumix-only: draws an Avalonia geometry the caller already built.</summary>
    public void DrawGeometry(
        IBrush? brush,
        IPen? pen,
        Geometry geometry,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometryOffset.X == 0.0 && geometryOffset.Y == 0.0)
        {
            AddDrawCommand(context => context.DrawGeometry(brush, pen, geometry));
            return;
        }

        AddDrawCommand(context =>
        {
            using var transform = context.PushTransform(
                Matrix.CreateTranslation(geometryOffset.X, geometryOffset.Y));
            context.DrawGeometry(brush, pen, geometry);
        });
    }

    // Dart parity source: dart:ui Canvas.drawShadow.
    public void DrawShadow(
        Geometry geometry,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        DebugRecordCall(new CanvasCall("drawShadow", ShadowColor: color, Elevation: elevation));
        DrawShadowCore(() => geometry, color, elevation, transparentOccluder, geometryOffset);
    }

    /// <summary>
    /// <see cref="DrawShadow(Geometry, Color, double, bool, Point)"/> over a <see cref="Path"/>, whose
    /// backend geometry is built on playback so recording needs no render backend.
    /// </summary>
    public void DrawShadow(
        Path path,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("drawShadow", Path: path, ShadowColor: color, Elevation: elevation));
        Geometry? geometry = null;
        DrawShadowCore(() => geometry ??= path.ToGeometry(), color, elevation, transparentOccluder, geometryOffset);
    }

    private void DrawShadowCore(
        Func<Geometry> geometrySource,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset)
    {
        if (elevation <= 0.0 || color.Alpha == 0)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            Geometry geometry = geometrySource();
            Point effectiveOffset = geometryOffset + new Vector(0.0, elevation * 0.5);
            using var transform = context.PushTransform(Matrix.CreateTranslation(
                effectiveOffset.X,
                effectiveOffset.Y));
            int steps = Math.Max(1, (int)Math.Ceiling(elevation * 2.0));
            for (int step = steps; step >= 1; step--)
            {
                double fraction = step / (double)steps;
                byte alpha = (byte)Math.Clamp(
                    (int)Math.Round(color.Alpha * 0.12 * (1.0 - (fraction * 0.75))),
                    1,
                    byte.MaxValue);
                var shadowBrush = new SolidColorBrush(Color.FromARGB(alpha, color.Red, color.Green, color.Blue));
                var shadowPen = new Pen(shadowBrush, step * 2.0);
                context.DrawGeometry(transparentOccluder ? shadowBrush : null, shadowPen, geometry);
            }
        });
    }

    // Dart parity source: dart:ui Canvas.drawImageRect.
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
        AddDrawCommand(context =>
        {
            DrawingContext.PushedState? clip = null;
            DrawingContext.PushedState? alpha = null;
            DrawingContext.PushedState? transform = null;
            DrawingContext.PushedState? renderOptions = null;
            try
            {
                if (ovalClipRect.HasValue)
                {
                    clip = context.PushGeometryClip(new EllipseGeometry(ovalClipRect.Value));
                }
                else if (clipRect.HasValue)
                {
                    clip = clipRadius.HasValue && clipRadius.Value.Radius > 0
                        ? Layer.PushRoundedRectClip(context, clipRect.Value, clipRadius.Value.Radius)
                        : context.PushClip(clipRect.Value);
                }

                if (effectiveOpacity < 1.0)
                {
                    alpha = context.PushOpacity(effectiveOpacity);
                }

                renderOptions = context.PushRenderOptions(new RenderOptions
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
                    double centerX = horizontalFlipAxisX ?? destinationRect.Center.X;
                    transform = context.PushTransform(new Matrix(-1, 0, 0, 1, centerX * 2, 0));
                }

                context.DrawImage(image, sourceRect, destinationRect);
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
