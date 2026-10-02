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
    /// Dart's <c>Canvas.drawRect(rect, paint)</c>. The paint's colour (or shader), style, stroke, anti-alias
    /// flag, mask filter and blend mode are honoured; see <see cref="DrawShape"/>.
    /// </remarks>
    public void DrawRect(Rect rect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawRect",
            paint,
            call => call with { Rect = rect },
            () => new RectangleGeometry(rect),
            (context, fill, pen) => context.DrawRectangle(fill, pen, rect));
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
        DebugRecordCall(new CanvasCall("drawPaint", Brush: brush));
        AddDrawCommand(context => context.DrawRectangle(brush, null, DrawPaintBounds));
    }

    /// <summary>Fills the canvas's current clip with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPaint(paint)</c>; the paint's colour or shader fills the clip.</remarks>
    public void DrawPaint(Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        IBrush brush = paint.Shader ?? new SolidColorBrush(paint.Color);
        DebugRecordCall(new CanvasCall("drawPaint", Brush: brush));
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
    /// <remarks>Dart's <c>Canvas.drawRRect(rrect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawRRect(RRect rrect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Geometry? geometry = null;
        Geometry Source() => geometry ??= rrect.ToPath().ToGeometry();
        DrawShape(
            "drawRRect",
            paint,
            call => call with { RRect = rrect },
            Source,
            (context, fill, pen) => context.DrawGeometry(fill, pen, Source()));
    }

    /// <summary>Draws the ring between two rounded rectangles with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawDRRect(outer, inner, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawDRRect(RRect outer, RRect inner, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Geometry? geometry = null;
        Geometry Source() => geometry ??= new CombinedGeometry(
            GeometryCombineMode.Exclude,
            outer.ToPath().ToGeometry(),
            inner.ToPath().ToGeometry());
        DrawShape(
            "drawDRRect",
            paint,
            call => call with { RRect = outer },
            Source,
            (context, fill, pen) => context.DrawGeometry(fill, pen, Source()));
    }

    /// <summary>Draws an axis-aligned oval with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawOval(rect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawOval(Rect oval, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawOval",
            paint,
            call => call with { Rect = oval },
            () => new EllipseGeometry(oval),
            (context, fill, pen) => context.DrawEllipse(fill, pen, oval.Center, oval.Width / 2.0, oval.Height / 2.0));
    }

    /// <summary>Draws a path with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPath(path, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawPath(Path path, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(paint);
        Geometry? geometry = null;
        Geometry Source() => geometry ??= path.ToGeometry();
        DrawShape(
            "drawPath",
            paint,
            call => call with { Path = path },
            Source,
            (context, fill, pen) => context.DrawGeometry(fill, pen, Source()));
    }

    /// <summary>Draws a rounded superellipse with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawRSuperellipse(rsuperellipse, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawRSuperellipse(RSuperellipse rsuperellipse, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Path path = rsuperellipse.ToPath();
        Geometry? geometry = null;
        Geometry Source() => geometry ??= path.ToGeometry();
        DrawShape(
            "drawRSuperellipse",
            paint,
            call => call with { Path = path },
            Source,
            (context, fill, pen) => context.DrawGeometry(fill, pen, Source()));
    }

    /// <summary>
    /// Records and plays back one shape draw of a <see cref="Paint"/>-taking method: the shared tail of
    /// <c>drawRect</c>, <c>drawRRect</c>, <c>drawDRRect</c>, <c>drawOval</c>, <c>drawCircle</c>,
    /// <c>drawPath</c> and <c>drawRSuperellipse</c>.
    /// </summary>
    /// <remarks>
    /// The paint is read when the call is made, as Dart's canvas does. A plain paint plays back through
    /// <paramref name="draw"/>. A paint with a <see cref="MaskFilter"/> or a blend mode other than
    /// <see cref="BlendMode.SourceOver"/> goes through <see cref="RasterizedShapeDraw"/>, the raster
    /// backend for both (Avalonia's drawing context has neither); where no raster backend exists the
    /// shape is drawn through <paramref name="draw"/> with a Gaussian-ring blur approximation.
    /// </remarks>
    private void DrawShape(
        string method,
        Paint paint,
        Func<CanvasCall, CanvasCall> describe,
        Func<Geometry> geometrySource,
        Action<DrawingContext, IBrush?, IPen?> draw)
    {
        IBrush brush = paint.Shader ?? new SolidColorBrush(paint.Color);
        bool stroke = paint.Style == PaintingStyle.Stroke;
        IPen? pen = stroke ? CreatePen(paint, brush) : null;
        IBrush? fill = stroke ? null : brush;
        MaskFilter? maskFilter = paint.MaskFilter;
        BlendMode blendMode = paint.BlendMode;
        bool isAntiAlias = paint.IsAntiAlias;
        Color color = paint.Color;
        DebugRecordCall(describe(new CanvasCall(
            method,
            Brush: fill,
            Pen: pen,
            MaskFilter: maskFilter,
            BlendMode: blendMode)
        {
            RecordedColor = color,
        }));

        void DrawPlain(DrawingContext context)
        {
            using DrawingContext.PushedState edge = context.PushRenderOptions(new RenderOptions
            {
                EdgeMode = isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
            });
            draw(context, fill, pen);
        }

        if (maskFilter is null && blendMode == BlendMode.SourceOver)
        {
            AddDrawCommand(DrawPlain);
            return;
        }

        var rasterized = new RasterizedShapeDraw(
            geometrySource,
            brush,
            color,
            pen,
            maskFilter,
            blendMode,
            isAntiAlias,
            context =>
            {
                if (maskFilter is null || stroke)
                {
                    DrawPlain(context);
                    return;
                }

                DrawBlurApproximation(context, geometrySource(), color, maskFilter, DrawPlain);
            });
        AddDrawCommand(rasterized.Draw);
    }

    private static Pen CreatePen(Paint paint, IBrush brush) => new(
        brush,
        paint.StrokeWidth,
        lineCap: ToPenLineCap(paint.StrokeCap),
        lineJoin: ToPenLineJoin(paint.StrokeJoin),
        miterLimit: paint.StrokeMiterLimit);

    // Dart parity source: dart:ui Canvas.drawRSuperellipse.
    public void DrawRSuperellipse(RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
    {
        var path = new Path();
        path.AddRSuperellipse(rsuperellipse);
        DrawPath(path, brush, pen);
    }

    /// <summary>Draws a blurred box shadow using the exact rounded-superellipse contour.</summary>
    /// <remarks>
    /// Plumix-only shorthand for Dart's <c>drawRSuperellipse(shape.inflate(spread).shift(offset),
    /// shadow.toPaint())</c>.
    /// </remarks>
    public void DrawRSuperellipseShadow(RSuperellipse rsuperellipse, Plumix.Rendering.BoxShadow shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        RSuperellipse shadowShape = rsuperellipse
            .Inflate(shadow.SpreadRadius)
            .Shift(shadow.Offset);
        DrawRSuperellipse(shadowShape, shadow.ToPaint());
    }

    /// <summary>
    /// Fills <paramref name="rsuperellipse"/> with <paramref name="color"/> under a Gaussian blur of
    /// <paramref name="blurSigma"/>.
    /// </summary>
    /// <remarks>
    /// Plumix-only shorthand for Dart's <c>Canvas.drawRSuperellipse</c> with a <c>Paint.maskFilter</c> of
    /// <c>MaskFilter.blur(BlurStyle.normal, sigma)</c>.
    /// </remarks>
    public void DrawRSuperellipseBlur(RSuperellipse rsuperellipse, Color color, double blurSigma)
    {
        DrawRSuperellipse(rsuperellipse, new Paint
        {
            Color = color,
            MaskFilter = blurSigma > 0.0 ? MaskFilter.Blur(BlurStyle.Normal, blurSigma) : null,
        });
    }

    // The no-raster-backend stand-in for a blur mask filter: concentric strokes sampled from one Gaussian
    // falloff keep the contour exact; the shape itself is filled last unless the style is outer.
    private static void DrawBlurApproximation(
        DrawingContext context,
        Geometry geometry,
        Color color,
        MaskFilter maskFilter,
        Action<DrawingContext> drawShape)
    {
        double blurSigma = maskFilter.Sigma;
        if (blurSigma > 0.0 && maskFilter.Style != BlurStyle.Inner)
        {
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
                if (layerAlpha > 0)
                {
                    Color layerColor = Color.FromARGB(layerAlpha, color.Red, color.Green, color.Blue);
                    context.DrawGeometry(null, new Pen(new SolidColorBrush(layerColor), radius * 2.0), geometry);
                }
            }
        }

        if (maskFilter.Style != BlurStyle.Outer)
        {
            drawShape(context);
        }
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

    /// <summary>Draws a circle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawCircle(c, radius, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public void DrawCircle(Point center, double radius, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        double clampedRadius = Math.Max(0, radius);
        DrawShape(
            "drawCircle",
            paint,
            call => call with { Center = center, Radius = radius },
            () => new EllipseGeometry(new Rect(
                center.X - clampedRadius,
                center.Y - clampedRadius,
                clampedRadius * 2.0,
                clampedRadius * 2.0)),
            (context, fill, pen) => context.DrawEllipse(fill, pen, center, clampedRadius, clampedRadius));
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

    /// <summary>Draws a line between the given points with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawLine(p1, p2, paint)</c>: the paint's style is ignored and the line is
    /// always stroked; see <see cref="DrawShape"/> for the mask filter and blend mode.
    /// </remarks>
    public void DrawLine(Point startPoint, Point endPoint, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        var stroke = new Paint(paint) { Style = PaintingStyle.Stroke };
        DrawShape(
            "drawLine",
            stroke,
            call => call with { Offset = startPoint, EndOffset = endPoint },
            () => new LineGeometry(startPoint, endPoint),
            (context, _, pen) => context.DrawLine(pen!, startPoint, endPoint));
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

        DebugRecordCall(new CanvasCall("drawImageRect", Rect: destinationRect, SourceRect: sourceRect));
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
                        ? SceneRasterizer.PushRoundedRectClip(context, clipRect.Value, clipRadius.Value.Radius)
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
