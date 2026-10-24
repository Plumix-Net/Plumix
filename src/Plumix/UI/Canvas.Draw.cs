using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas (the drawing half; Avalonia brushes/pens stand in for `Paint`)

public partial class Canvas
{
    // Dart parity source: dart:ui Canvas.drawRect / Canvas.drawRRect.
    public virtual void DrawRectangle(
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
            ShaderDrawing.DrawRectangle(context, brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        });
    }

    // Dart parity source: dart:ui Canvas.drawRRect (corner radii given as a BorderRadius).
    public virtual void DrawRectangle(
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
            ShaderDrawing.DrawRectangle(context, brush, pen, roundedRect, boxShadows);
        });
    }

    /// <summary>Draws a rectangle with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawRect(rect, paint)</c>. The paint's colour (or shader), style, stroke, anti-alias
    /// flag, mask filter and blend mode are honoured; see <see cref="DrawShape"/>.
    /// </remarks>
    public virtual void DrawRect(Rect rect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawRect",
            paint,
            call => call with { Rect = rect },
            () => new RectangleGeometry(rect),
            (context, fill, pen) => ShaderDrawing.DrawRectangle(context, fill, pen, rect),
            () => CreateRectPath(rect));
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
    public virtual void DrawPaint(IBrush brush)
    {
        DebugRecordCall(new CanvasCall("drawPaint", Brush: brush));
        AddDrawCommand(context => ShaderDrawing.DrawRectangle(context, brush, null, DrawPaintBounds));
    }

    /// <summary>Fills the canvas's current clip with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPaint(paint)</c>; the paint's colour or shader fills the clip.</remarks>
    public virtual void DrawPaint(Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        IBrush brush = paint.Shader is INativeShader nativeShader
            ? nativeShader.Snapshot() : paint.Shader ?? new SolidColorBrush(paint.Color);
        DebugRecordCall(new CanvasCall("drawPaint", Brush: brush));
        if (brush is INativeShader shader)
        {
            var snapshot = new Paint(paint) { Style = PaintingStyle.Fill };
            AddDrawCommand(context => ShaderDrawing.Fill(
                context, shader, DrawPaintBounds, snapshot, fillCanvas: true));
            return;
        }

        AddDrawCommand(context => ShaderDrawing.DrawRectangle(context, brush, null, DrawPaintBounds));
    }

    private static readonly Rect DrawPaintBounds = new(-1.0e9, -1.0e9, 2.0e9, 2.0e9);

    // Dart parity source: dart:ui Canvas.drawRRect.
    public virtual void DrawRRect(RRect rrect, IBrush? brush, IPen? pen)
    {
        DebugRecordCall(new CanvasCall("drawRRect", RRect: rrect, Brush: brush, Pen: pen));
        var path = new Path();
        path.AddRRect(rrect);
        if (TryAddNativeSolidDraw(path, brush, pen))
        {
            return;
        }

        Geometry? geometry = null;
        AddDrawCommand(context => ShaderDrawing.DrawGeometry(context, brush, pen, geometry ??= path.ToGeometry()));
    }

    /// <summary>Draws a rounded rectangle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawRRect(rrect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawRRect(RRect rrect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Geometry? geometry = null;
        Geometry Source() => geometry ??= rrect.ToPath().ToGeometry();
        DrawShape(
            "drawRRect",
            paint,
            call => call with { RRect = rrect },
            Source,
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, Source()),
            rrect.ToPath,
            nativeSolid: true);
    }

    /// <summary>Draws the ring between two rounded rectangles with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawDRRect(outer, inner, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawDRRect(RRect outer, RRect inner, Paint paint)
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
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, Source()),
            () => CreateDRRectPath(outer, inner),
            nativeSolid: true);
    }

    /// <summary>Draws an axis-aligned oval with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawOval(rect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawOval(Rect oval, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawOval",
            paint,
            call => call with { Rect = oval },
            () => new EllipseGeometry(oval),
            (context, fill, pen) => ShaderDrawing.DrawEllipse(
                context, fill, pen, oval.Center, oval.Width / 2.0, oval.Height / 2.0),
            () => CreateOvalPath(oval));
    }

    /// <summary>Draws a path with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPath(path, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawPath(Path path, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(paint);
        var snapshot = new Path(path);
        Geometry? geometry = null;
        Geometry Source() => geometry ??= snapshot.ToGeometry();
        DrawShape(
            "drawPath",
            paint,
            call => call with { Path = snapshot },
            Source,
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, Source()),
            () => snapshot,
            nativeSolid: true);
    }

    /// <summary>Draws a rounded superellipse with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawRSuperellipse(rsuperellipse, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawRSuperellipse(RSuperellipse rsuperellipse, Paint paint)
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
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, Source()),
            () => path,
            nativeSolid: true);
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
        Action<DrawingContext, IBrush?, IPen?> draw,
        Func<Path>? nativePathSource = null,
        bool nativeSolid = false)
    {
        IBrush brush = paint.Shader is INativeShader nativeShader
            ? nativeShader.Snapshot() : paint.Shader ?? new SolidColorBrush(paint.Color);
        bool stroke = paint.Style == PaintingStyle.Stroke;
        IPen? pen = stroke ? CreatePen(paint, brush) : null;
        IBrush? fill = stroke ? null : brush;
        MaskFilter? maskFilter = paint.MaskFilter;
        BlendMode blendMode = paint.BlendMode;
        bool isAntiAlias = paint.IsAntiAlias;
        Color color = paint.Color;
        CanvasCall recorded = describe(new CanvasCall(
            method,
            Brush: fill,
            Pen: pen,
            MaskFilter: maskFilter,
            BlendMode: blendMode)
        {
            RecordedColor = color,
        });
        DebugRecordCall(recorded);

        void DrawPlain(DrawingContext context)
        {
            using DrawingContext.PushedState alpha = brush is INativeShader
                ? context.PushOpacity(color.A) : default;
            using DrawingContext.PushedState edge = context.PushRenderOptions(new RenderOptions
            {
                EdgeMode = isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
            });
            draw(context, fill, pen);
        }

        if (maskFilter is null && blendMode == BlendMode.SourceOver)
        {
            INativeShader? native = brush as INativeShader
                ?? (nativeSolid && paint.Shader is null ? new SolidColorNativeShader(color) : null);
            if (native is { } shader && nativePathSource?.Invoke() is { } path)
            {
                var snapshot = new Paint(paint);
                AddDrawCommand(context => ShaderDrawing.DrawPath(context, shader, path, snapshot));
                return;
            }

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

    // SkCanvas::onDrawDRRect: both outlines in one even-odd path.
    private static Path CreateDRRectPath(RRect outer, RRect inner)
    {
        var result = new Path { FillType = PathFillType.EvenOdd };
        result.AddRRect(outer);
        result.AddRRect(inner);
        return result;
    }

    private static Path CreateRectPath(Rect rect)
    {
        var result = new Path();
        result.AddRect(rect);
        return result;
    }

    private static Path CreateOvalPath(Rect rect)
    {
        var result = new Path();
        result.AddOval(rect);
        return result;
    }

    private static Path CreateLinePath(Point start, Point end)
    {
        var result = new Path();
        result.MoveTo(start.X, start.Y);
        result.LineTo(end.X, end.Y);
        return result;
    }

    // Paragraph glyph outlines use the same Paint pipeline as framework-owned shapes.
    internal void DrawTextGeometry(Geometry geometry, Paint paint)
    {
        bool filter = paint.ColorFilter is not null || paint.ImageFilter is not null || paint.InvertColors;
        if (filter)
        {
            SaveLayer(geometry.GetRenderBounds(new Pen(Brushes.Black, paint.StrokeWidth)).Inflate(
                PaintRasterizer.BlurExtent(paint.MaskFilter?.Sigma ?? 0.0) + 1.0), new Paint
            {
                ColorFilter = paint.ColorFilter,
                ImageFilter = paint.ImageFilter,
                InvertColors = paint.InvertColors,
                BlendMode = paint.BlendMode,
            });
        }

        Paint effective = filter ? new Paint(paint) { BlendMode = BlendMode.SourceOver } : paint;
        DrawShape(
            "drawTextGeometry",
            effective,
            call => call,
            () => geometry,
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, geometry));
        if (filter)
        {
            Restore();
        }
    }

    private static Pen CreatePen(Paint paint, IBrush brush) => new(
        brush,
        paint.StrokeWidth,
        lineCap: ToPenLineCap(paint.StrokeCap),
        lineJoin: ToPenLineJoin(paint.StrokeJoin),
        miterLimit: paint.StrokeMiterLimit);

    // Dart parity source: dart:ui Canvas.drawRSuperellipse.
    public virtual void DrawRSuperellipse(RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
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
    public virtual void DrawRSuperellipseShadow(RSuperellipse rsuperellipse, Plumix.Rendering.BoxShadow shadow)
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
    public virtual void DrawRSuperellipseBlur(RSuperellipse rsuperellipse, Color color, double blurSigma)
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
                    ShaderDrawing.DrawGeometry(context, null,
                        new Pen(new SolidColorBrush(layerColor), radius * 2.0), geometry);
                }
            }
        }

        if (maskFilter.Style != BlurStyle.Outer)
        {
            drawShape(context);
        }
    }

    // Dart parity source: dart:ui Canvas.drawDRRect (the ring between two rounded rectangles).
    public virtual void DrawDRRect(RRect outer, RRect inner, IBrush brush)
    {
        if (TryAddNativeSolidDraw(CreateDRRectPath(outer, inner), brush, null))
        {
            return;
        }

        var outerPath = new Path();
        outerPath.AddRRect(outer);
        var innerPath = new Path();
        innerPath.AddRRect(inner);
        Geometry? geometry = null;
        AddDrawCommand(context => ShaderDrawing.DrawGeometry(context,
            brush,
            null,
            geometry ??= new CombinedGeometry(
                GeometryCombineMode.Exclude,
                outerPath.ToGeometry(),
                innerPath.ToGeometry())));
    }

    // Dart parity source: dart:ui Canvas.drawOval.
    public virtual void DrawOval(Rect oval, IBrush? brush, IPen? pen)
    {
        DebugRecordCall(new CanvasCall("drawOval", Rect: oval, Brush: brush, Pen: pen));
        AddDrawCommand(context =>
            ShaderDrawing.DrawEllipse(context, brush, pen, oval.Center, oval.Width / 2.0, oval.Height / 2.0));
    }

    // Dart parity source: dart:ui Canvas.drawPath.
    public virtual void DrawPath(Path path, IBrush? brush, IPen? pen)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("drawPath", Brush: brush, Pen: pen, Path: path));

        if (TryAddNativeSolidDraw(new Path(path), brush, pen))
        {
            return;
        }

        // The backend geometry is built on playback: recording must not need a render backend.
        Geometry? geometry = null;
        AddDrawCommand(context => ShaderDrawing.DrawGeometry(context, brush, pen, geometry ??= path.ToGeometry()));
    }

    // A solid fill or an undashed solid stroke of a path whose curves Avalonia's geometry would
    // approximate goes to Skia as the exact path, the way Dart's canvas draws it with a color paint.
    private bool TryAddNativeSolidDraw(Path path, IBrush? brush, IPen? pen)
    {
        Paint paint;
        if (pen is null && brush is ISolidColorBrush fill)
        {
            paint = new Paint { Color = SolidColor(fill) };
        }
        else if (IsTransparentOrNull(brush) && pen is { Brush: ISolidColorBrush stroke, DashStyle: null })
        {
            paint = new Paint
            {
                Color = SolidColor(stroke),
                Style = PaintingStyle.Stroke,
                StrokeWidth = pen.Thickness,
                StrokeMiterLimit = pen.MiterLimit,
                StrokeCap = pen.LineCap switch
                {
                    PenLineCap.Round => StrokeCap.Round,
                    PenLineCap.Square => StrokeCap.Square,
                    _ => StrokeCap.Butt,
                },
                StrokeJoin = pen.LineJoin switch
                {
                    PenLineJoin.Round => StrokeJoin.Round,
                    PenLineJoin.Bevel => StrokeJoin.Bevel,
                    _ => StrokeJoin.Miter,
                },
            };
        }
        else
        {
            return false;
        }

        var shader = new SolidColorNativeShader(paint.Color);
        AddDrawCommand(context => ShaderDrawing.DrawPath(context, shader, path, paint));
        return true;

        static bool IsTransparentOrNull(IBrush? value) =>
            value is null || (value is ISolidColorBrush solid && (solid.Color.A == 0 || solid.Opacity == 0.0));

        static Color SolidColor(ISolidColorBrush value)
        {
            var color = (Color)value.Color;
            return value.Opacity >= 1.0 ? color : color.WithAlpha((int)Math.Round(color.Alpha * value.Opacity));
        }
    }

    // Dart parity source: dart:ui Canvas.drawCircle.
    public virtual void DrawCircle(IBrush? brush, IPen? pen, Point center, double radius)
    {
        DebugRecordCall(new CanvasCall("drawCircle", Brush: brush, Pen: pen, Center: center, Radius: radius));
        double clampedRadius = Math.Max(0, radius);
        AddDrawCommand(context => ShaderDrawing.DrawEllipse(context, brush, pen, center, clampedRadius, clampedRadius));
    }

    /// <summary>Draws a circle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawCircle(c, radius, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawCircle(Point center, double radius, Paint paint)
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
            (context, fill, pen) => ShaderDrawing.DrawEllipse(context, fill, pen, center, clampedRadius, clampedRadius),
            () => CreateOvalPath(new Rect(center.X - clampedRadius, center.Y - clampedRadius,
                clampedRadius * 2.0, clampedRadius * 2.0)));
    }

    // Dart parity source: dart:ui Canvas.drawArc.
    public virtual void DrawArc(IPen pen, Rect rect, double startAngleRadians, double sweepAngleRadians)
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

            ShaderDrawing.DrawGeometry(context, brush: null, pen: pen, geometry: geometry);
        });
    }

    // Dart parity source: dart:ui Canvas.drawLine.
    public virtual void DrawLine(IPen pen, Point startPoint, Point endPoint)
    {
        DebugRecordCall(new CanvasCall("drawLine", Pen: pen, Offset: startPoint, EndOffset: endPoint));
        if (pen is { Thickness: 0.0, DashStyle: null, Brush: ISolidColorBrush solid })
        {
            // A zero-width stroke is Skia's one-device-pixel hairline, as with Dart's strokeWidth 0.
            var hairline = new HairlineDraw(
                startPoint,
                endPoint,
                (Color)solid.Color,
                isAntiAlias: true,
                pen.LineCap switch
                {
                    PenLineCap.Round => StrokeCap.Round,
                    PenLineCap.Square => StrokeCap.Square,
                    _ => StrokeCap.Butt,
                });
            AddDrawCommand(context => context.Custom(hairline));
            return;
        }

        AddDrawCommand(context => ShaderDrawing.DrawLine(context, pen, startPoint, endPoint));
    }

    /// <summary>Draws a line between the given points with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawLine(p1, p2, paint)</c>: the paint's style is ignored and the line is
    /// always stroked; see <see cref="DrawShape"/> for the mask filter and blend mode.
    /// </remarks>
    public virtual void DrawLine(Point startPoint, Point endPoint, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        if (paint.StrokeWidth == 0.0 && paint.Shader is null
            && paint.MaskFilter is null && paint.BlendMode == BlendMode.SourceOver)
        {
            DebugRecordCall(new CanvasCall(
                "drawLine",
                Pen: CreatePen(paint, new SolidColorBrush(paint.Color)),
                Offset: startPoint,
                EndOffset: endPoint,
                BlendMode: paint.BlendMode) { RecordedColor = paint.Color });
            var hairline = new HairlineDraw(startPoint, endPoint, paint.Color, paint.IsAntiAlias, paint.StrokeCap);
            AddDrawCommand(context => context.Custom(hairline));
            return;
        }

        var stroke = new Paint(paint) { Style = PaintingStyle.Stroke };
        DrawShape(
            "drawLine",
            stroke,
            call => call with { Offset = startPoint, EndOffset = endPoint },
            () => new LineGeometry(startPoint, endPoint),
            (context, _, pen) => ShaderDrawing.DrawLine(context, pen!, startPoint, endPoint),
            () => CreateLinePath(startPoint, endPoint));
    }

    // Dart parity source: dart:ui Canvas.drawPath over a closed polygon contour.
    public virtual void DrawPolygon(IBrush? brush, IPen? pen, IReadOnlyList<Point> points)
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

            ShaderDrawing.DrawGeometry(context, brush, pen, geometry);
        });
    }

    /// <summary>Plumix-only: draws an Avalonia geometry the caller already built.</summary>
    public virtual void DrawGeometry(
        IBrush? brush,
        IPen? pen,
        Geometry geometry,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometryOffset.X == 0.0 && geometryOffset.Y == 0.0)
        {
            AddDrawCommand(context => ShaderDrawing.DrawGeometry(context, brush, pen, geometry));
            return;
        }

        AddDrawCommand(context =>
        {
            using var transform = context.PushTransform(
                Matrix.CreateTranslation(geometryOffset.X, geometryOffset.Y));
            ShaderDrawing.DrawGeometry(context, brush, pen, geometry);
        });
    }

    /// <summary>
    /// Draws a shadow for a <see cref="RectangleGeometry"/> or <see cref="EllipseGeometry"/> occluder, as
    /// <see cref="DrawShadow(Path, Color, double, bool, Point)"/> over the matching path.
    /// </summary>
    /// <remarks>
    /// Plumix-only: Avalonia does not expose a geometry's native path, so any other geometry gets an
    /// offset, stroked approximation (see <c>docs/ai/DIVERGENCES.md</c>); pass a <see cref="Path"/> instead.
    /// </remarks>
    public virtual void DrawShadow(
        Geometry geometry,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (ShadowPathOf(geometry) is { } path)
        {
            DrawShadow(path, color, elevation, transparentOccluder, geometryOffset);
            return;
        }

        DebugRecordCall(new CanvasCall("drawShadow", ShadowColor: color, Elevation: elevation));
        DrawApproximateShadow(geometry, color, elevation, transparentOccluder, geometryOffset);
    }

    /// <summary>
    /// Draws a shadow for a rectangle, rounded rectangle, or arbitrary path, as if it were the outline of a
    /// physical object at <paramref name="elevation"/> above the canvas.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawShadow(path, color, elevation, transparentOccluder)</c>, rendered as the Flutter
    /// engine's Skia backend does (see <see cref="ShadowDraw"/>): the elevation is scaled by the implicit
    /// view's device pixel ratio, and the shadow combines an ambient and a directional spot shadow.
    /// </remarks>
    public virtual void DrawShadow(
        Path path,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("drawShadow", Path: path, ShadowColor: color, Elevation: elevation));
        if (color.Alpha == 0)
        {
            return;
        }

        Path occluder = geometryOffset.X == 0.0 && geometryOffset.Y == 0.0 ? path : path.Shift(geometryOffset);
        double devicePixelRatio = PlatformDispatcher.Instance.EngineDevicePixelRatio;
        var shadow = new ShadowDraw(occluder, color, elevation, transparentOccluder, devicePixelRatio);
        AddDrawCommand(context => context.Custom(shadow));
    }

    private static Path? ShadowPathOf(Geometry geometry)
    {
        switch (geometry)
        {
            case RectangleGeometry { Transform: null or { Value.IsIdentity: true } } rectangle:
            {
                var path = new Path();
                if (rectangle.RadiusX > 0.0 && rectangle.RadiusY > 0.0)
                {
                    path.AddRRect(RRect.FromRectXY(rectangle.Rect, rectangle.RadiusX, rectangle.RadiusY));
                }
                else
                {
                    path.AddRect(rectangle.Rect);
                }

                return path;
            }

            case EllipseGeometry { Transform: null or { Value.IsIdentity: true } } ellipse:
            {
                var path = new Path();
                // An unset Rect means the ellipse is given by its centre and radii (Avalonia's two modes).
                path.AddOval(ellipse.Rect != default
                    ? ellipse.Rect
                    : new Rect(
                        ellipse.Center.X - ellipse.RadiusX,
                        ellipse.Center.Y - ellipse.RadiusY,
                        ellipse.RadiusX * 2.0,
                        ellipse.RadiusY * 2.0));
                return path;
            }

            default:
                return null;
        }
    }

    private void DrawApproximateShadow(
        Geometry geometry,
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
                ShaderDrawing.DrawGeometry(context, transparentOccluder ? shadowBrush : null, shadowPen, geometry);
            }
        });
    }

    /// <summary>
    /// Draws the given image into the canvas with its top-left corner at the given offset, composited
    /// with the given <see cref="Paint"/>.
    /// </summary>
    /// <remarks>Dart's <c>Canvas.drawImage(image, offset, paint)</c>; the image is an Avalonia
    /// <see cref="IImage"/> (see <c>docs/ai/DIVERGENCES.md</c>).</remarks>
    public virtual void DrawImage(IImage image, Point offset, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        Size size = ImageSize(image);
        var destination = new Rect(offset, size);
        RecordImageDraw(
            new CanvasCall("drawImage", Offset: offset, Image: image),
            image,
            paint,
            destination,
            (context, source) => context.DrawImage(source, new Rect(source.Size), destination));
    }

    /// <summary>
    /// Draws the subset of the given image described by <paramref name="src"/> into the canvas in the
    /// axis-aligned rectangle given by <paramref name="dst"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawImageRect(image, src, dst, paint)</c>. The paint's alpha, blend mode, filter
    /// quality and anti-alias flag apply to the draw; its colour filter and inverted colours filter the
    /// image's pixels, and an image filter composites the draw through a rasterized layer.
    /// </remarks>
    public virtual void DrawImageRect(IImage image, Rect src, Rect dst, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        RecordImageDraw(
            new CanvasCall("drawImageRect", Rect: dst, SourceRect: src, Image: image),
            image,
            paint,
            dst,
            (context, source) =>
            {
                if (src.Width <= 0.0 || src.Height <= 0.0 || dst.Width <= 0.0 || dst.Height <= 0.0)
                {
                    return;
                }

                context.DrawImage(source, src, dst);
            });
    }

    /// <summary>
    /// Draws the given image into the canvas using the given <see cref="Paint"/>, splitting it into a 3x3
    /// grid by <paramref name="center"/>: the four corners keep their size, the four sides stretch along
    /// one axis and the middle stretches along both, so the image fills <paramref name="dst"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawImageNine(image, center, dst, paint)</c>. As in Skia's lattice drawing, when
    /// <paramref name="dst"/> is too small for the fixed corners they shrink proportionally and the middle
    /// collapses.
    /// </remarks>
    public virtual void DrawImageNine(IImage image, Rect center, Rect dst, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        Size size = ImageSize(image);
        RecordImageDraw(
            new CanvasCall("drawImageNine", Rect: dst, SourceRect: center, Image: image),
            image,
            paint,
            dst,
            (context, source) =>
            {
                foreach ((Rect patchSource, Rect patchDestination) in NinePatches(size, center, dst))
                {
                    context.DrawImage(source, patchSource, patchDestination);
                }
            });
    }

    /// <summary>The source and destination rect of each visible patch of a nine-patch draw.</summary>
    internal static IEnumerable<(Rect Source, Rect Destination)> NinePatches(Size imageSize, Rect center, Rect dst)
    {
        double[] sourceX = [0.0, center.Left, center.Right, imageSize.Width];
        double[] sourceY = [0.0, center.Top, center.Bottom, imageSize.Height];
        double[] destinationX = LatticeDivisions(dst.Left, dst.Width, center.Left, imageSize.Width - center.Right);
        double[] destinationY = LatticeDivisions(dst.Top, dst.Height, center.Top, imageSize.Height - center.Bottom);
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Rect source = new Rect(
                    new Point(sourceX[column], sourceY[row]),
                    new Point(sourceX[column + 1], sourceY[row + 1]));
                Rect destination = new Rect(
                    new Point(destinationX[column], destinationY[row]),
                    new Point(destinationX[column + 1], destinationY[row + 1]));
                if (source.Width > 0.0 && source.Height > 0.0 && destination.Width > 0.0 && destination.Height > 0.0)
                {
                    yield return (source, destination);
                }
            }
        }
    }

    // Skia's SkLatticeIter: the fixed start/end extents keep their size, scaled down together when the
    // destination is narrower than both, and the middle takes what is left.
    private static double[] LatticeDivisions(double start, double extent, double fixedStart, double fixedEnd)
    {
        double fixedTotal = fixedStart + fixedEnd;
        double scale = fixedTotal > extent && fixedTotal > 0.0 ? extent / fixedTotal : 1.0;
        double first = start + (fixedStart * scale);
        double last = start + extent - (fixedEnd * scale);
        return [start, first, Math.Max(first, last), start + extent];
    }

    // The image's size in dart:ui image pixels: the Avalonia image's own size (decoded bitmaps are 96 DPI,
    // one unit per pixel), as RenderImage and ImageInfo measure it.
    internal static Size ImageSize(IImage image) => image.Size;

    // Records one image draw of a Paint-taking method. The paint is read now, as Dart's canvas does.
    private void RecordImageDraw(
        CanvasCall call,
        IImage image,
        Paint paint,
        Rect bounds,
        Action<DrawingContext, IImage> draw)
    {
        var snapshot = new Paint(paint);
        DebugRecordCall(call with { Paint = snapshot, BlendMode = snapshot.BlendMode, RecordedColor = snapshot.Color });
        double opacity = snapshot.Color.Opacity;
        BitmapBlendingMode blendMode = FilterLayerRasterizer.ToBitmapBlendingMode(snapshot.BlendMode);
        BitmapInterpolationMode interpolation = snapshot.FilterQuality switch
        {
            FilterQuality.None => BitmapInterpolationMode.None,
            FilterQuality.Low => BitmapInterpolationMode.LowQuality,
            FilterQuality.High => BitmapInterpolationMode.HighQuality,
            _ => BitmapInterpolationMode.MediumQuality,
        };
        EdgeMode edgeMode = snapshot.IsAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased;
        ColorFilter? colorFilter = snapshot.ColorFilter;
        bool invertColors = snapshot.InvertColors;

        // Skia applies a draw's image filter to the draw's output, as if the draw had its own layer.
        bool filterLayer = snapshot.ImageFilter is not null;
        if (filterLayer)
        {
            var layerPaint = new Paint { ImageFilter = snapshot.ImageFilter };
            var layer = new CanvasLayer(bounds, layerPaint, Matrix4.Copy(_transform));
            _commands.Add(CanvasCommand.ForSaveLayer(layer));
            AddImageDraw();
            layer.RestoreIndex = _commands.Count;
            _commands.Add(CanvasCommand.ForRestore());
            return;
        }

        AddImageDraw();

        void AddImageDraw()
        {
            AddDrawCommand(context =>
            {
                if (opacity <= 0.0 && blendMode == BitmapBlendingMode.SourceOver)
                {
                    return;
                }

                IImage source = colorFilter is null && !invertColors
                    ? image
                    : FilteredImages.Get(image, colorFilter, invertColors);
                DrawingContext.PushedState? alpha = opacity < 1.0 ? context.PushOpacity(opacity) : null;
                try
                {
                    using DrawingContext.PushedState options = context.PushRenderOptions(new RenderOptions
                    {
                        BitmapInterpolationMode = interpolation,
                        EdgeMode = edgeMode,
                        BitmapBlendingMode = blendMode,
                    });
                    draw(context, source);
                }
                finally
                {
                    alpha?.Dispose();
                }
            });
        }
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
