using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas (the drawing half; Avalonia brushes/pens stand in for `Paint`)

public partial class Canvas
{


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
            () => new RectangleGeometry(rect.ToAvalonia()),
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


    /// <summary>Fills the canvas's current clip with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPaint(paint)</c>; the paint's colour or shader fills the clip.</remarks>
    public virtual void DrawPaint(Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        IBrush brush = paint.Shader?.Brush is INativeShader nativeShader
            ? nativeShader.Snapshot() : paint.Shader?.Brush ?? new SolidColorBrush(paint.Color.ToAvalonia());
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

    private static readonly Rect DrawPaintBounds = Rect.FromLTWH(-1.0e9, -1.0e9, 2.0e9, 2.0e9);


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
            () => new EllipseGeometry(oval.ToAvalonia()),
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
        IBrush brush = paint.Shader?.Brush is INativeShader nativeShader
            ? nativeShader.Snapshot() : paint.Shader?.Brush ?? new SolidColorBrush(paint.Color.ToAvalonia());
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

    private static Path CreateLinePath(Offset start, Offset end)
    {
        var result = new Path();
        result.MoveTo(start.Dx, start.Dy);
        result.LineTo(end.Dx, end.Dy);
        return result;
    }

    // Paragraph glyph outlines use the same Paint pipeline as framework-owned shapes.
    internal void DrawTextGeometry(Geometry geometry, Paint paint)
    {
        bool filter = paint.ColorFilter is not null || paint.ImageFilter is not null || paint.InvertColors;
        if (filter)
        {
            SaveLayer(geometry.GetRenderBounds(new Pen(Brushes.Black, paint.StrokeWidth)).Inflate(
                PaintRasterizer.BlurExtent(paint.MaskFilter?.Sigma ?? 0.0) + 1.0).ToPlumix(), new Paint
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
                        new Pen(new SolidColorBrush(layerColor.ToAvalonia()), radius * 2.0), geometry);
                }
            }
        }

        if (maskFilter.Style != BlurStyle.Outer)
        {
            drawShape(context);
        }
    }






    /// <summary>Draws a circle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawCircle(c, radius, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawCircle(Offset center, double radius, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        double clampedRadius = Math.Max(0, radius);
        DrawShape(
            "drawCircle",
            paint,
            call => call with { Center = center, Radius = radius },
            () => new EllipseGeometry(Rect.FromLTWH(
                center.Dx - clampedRadius,
                center.Dy - clampedRadius,
                clampedRadius * 2.0,
                clampedRadius * 2.0).ToAvalonia()),
            (context, fill, pen) => ShaderDrawing.DrawEllipse(context, fill, pen, center, clampedRadius, clampedRadius),
            () => CreateOvalPath(Rect.FromLTWH(center.Dx - clampedRadius, center.Dy - clampedRadius,
                clampedRadius * 2.0, clampedRadius * 2.0)));
    }


    /// <summary>
    /// Draws an arc scaled to fit inside <paramref name="rect"/>, from <paramref name="startAngle"/>
    /// through <paramref name="sweepAngle"/> radians clockwise, with the given <see cref="Paint"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawArc(rect, startAngle, sweepAngle, useCenter, paint)</c>: with
    /// <paramref name="useCenter"/> the arc is closed back to the centre (a pie wedge), otherwise it
    /// is an open contour; see <see cref="DrawShape"/>.
    /// </remarks>
    public virtual void DrawArc(Rect rect, double startAngle, double sweepAngle, bool useCenter, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        var path = new Path();
        if (useCenter)
        {
            path.MoveTo(rect.Center.Dx, rect.Center.Dy);
            path.ArcTo(rect, startAngle, sweepAngle, forceMoveTo: false);
            path.Close();
        }
        else
        {
            path.AddArc(rect, startAngle, sweepAngle);
        }

        Geometry? geometry = null;
        Geometry Source() => geometry ??= path.ToGeometry();
        DrawShape(
            "drawArc",
            paint,
            call => call with { Rect = rect, Path = path },
            Source,
            (context, fill, pen) => ShaderDrawing.DrawGeometry(context, fill, pen, Source()),
            () => path,
            nativeSolid: true);
    }


    /// <summary>Draws a line between the given points with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawLine(p1, p2, paint)</c>: the paint's style is ignored and the line is
    /// always stroked; see <see cref="DrawShape"/> for the mask filter and blend mode.
    /// </remarks>
    public virtual void DrawLine(Offset startPoint, Offset endPoint, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        if (paint.StrokeWidth == 0.0 && paint.Shader is null
            && paint.MaskFilter is null && paint.BlendMode == BlendMode.SourceOver)
        {
            DebugRecordCall(new CanvasCall(
                "drawLine",
                Pen: CreatePen(paint, new SolidColorBrush(paint.Color.ToAvalonia())),
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
            () => new LineGeometry(startPoint.ToAvalonia(), endPoint.ToAvalonia()),
            (context, _, pen) => ShaderDrawing.DrawLine(context, pen!, startPoint, endPoint),
            () => CreateLinePath(startPoint, endPoint));
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
        Offset geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("drawShadow", Path: path, ShadowColor: color, Elevation: elevation));
        if (color.Alpha == 0)
        {
            return;
        }

        Path occluder = geometryOffset.Dx == 0.0 && geometryOffset.Dy == 0.0 ? path : path.Shift(geometryOffset);
        double devicePixelRatio = PlatformDispatcher.Instance.EngineDevicePixelRatio;
        var shadow = new ShadowDraw(occluder, color, elevation, transparentOccluder, devicePixelRatio);
        AddDrawCommand(context => context.Custom(shadow));
    }



    /// <summary>
    /// Draws the given image into the canvas with its top-left corner at the given offset, composited
    /// with the given <see cref="Paint"/>.
    /// </summary>
    /// <remarks>Dart's <c>Canvas.drawImage(image, offset, paint)</c>; the image is an Avalonia
    /// <see cref="RasterImage"/> (see <c>docs/ai/DIVERGENCES.md</c>).</remarks>
    public virtual void DrawImage(RasterImage image, Offset offset, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        Size size = ImageSize(image.Native);
        var destination = offset & size;
        RecordImageDraw(
            new CanvasCall("drawImage", Offset: offset, Image: image.Native),
            image.Native,
            paint,
            destination,
            (
                context,
                source) => context.DrawImage(
                    source,
                    (Offset.Zero & source.Size.ToPlumix()).ToAvalonia(),
                    destination.ToAvalonia()));
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
    public virtual void DrawImageRect(RasterImage image, Rect src, Rect dst, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        RecordImageDraw(
            new CanvasCall("drawImageRect", Rect: dst, SourceRect: src, Image: image.Native),
            image.Native,
            paint,
            dst,
            (context, source) =>
            {
                if (src.Width <= 0.0 || src.Height <= 0.0 || dst.Width <= 0.0 || dst.Height <= 0.0)
                {
                    return;
                }

                context.DrawImage(source, src.ToAvalonia(), dst.ToAvalonia());
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
    public virtual void DrawImageNine(RasterImage image, Rect center, Rect dst, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        Size size = ImageSize(image.Native);
        RecordImageDraw(
            new CanvasCall("drawImageNine", Rect: dst, SourceRect: center, Image: image.Native),
            image.Native,
            paint,
            dst,
            (context, source) =>
            {
                foreach ((Rect patchSource, Rect patchDestination) in NinePatches(size, center, dst))
                {
                    context.DrawImage(source, patchSource.ToAvalonia(), patchDestination.ToAvalonia());
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
                Rect source = Rect.FromLTRB(sourceX[column], sourceY[row], sourceX[column + 1], sourceY[row + 1]);
                Rect destination = Rect.FromLTRB(
                    destinationX[column],
                    destinationY[row],
                    destinationX[column + 1],
                    destinationY[row + 1]);
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
    internal static Size ImageSize(IImage image) => image.Size.ToPlumix();

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

    private static Offset PointOnEllipse(Rect rect, double angleRadians)
    {
        double centerX = rect.Left + (rect.Width / 2.0);
        double centerY = rect.Top + (rect.Height / 2.0);
        double radiusX = rect.Width / 2.0;
        double radiusY = rect.Height / 2.0;
        return new Offset(
            centerX + (Math.Cos(angleRadians) * radiusX),
            centerY + (Math.Sin(angleRadians) * radiusY));
    }
}
