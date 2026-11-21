using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas (the drawing half: each call records a typed entry, CanvasOps.cs)

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
        DrawShape("drawRect", call => call with { Rect = rect }, CanvasShapeKind.Rect, paint, op => op.Rect = rect);
    }

    /// <summary>Fills the canvas's current clip with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPaint(paint)</c>; the paint's colour or shader fills the clip.</remarks>
    public virtual void DrawPaint(Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Paint snapshot = Snapshot(paint);
        snapshot.Style = PaintingStyle.Fill;
        DebugRecordCall(new CanvasCall("drawPaint", Paint: snapshot));
        AddOp(new DrawPaintOp(snapshot));
    }

    /// <summary>Draws a rounded rectangle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawRRect(rrect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawRRect(RRect rrect, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawRRect", call => call with { RRect = rrect }, CanvasShapeKind.RRect, paint, op => op.RRect = rrect);
    }

    /// <summary>Draws the ring between two rounded rectangles with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawDRRect(outer, inner, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawDRRect(RRect outer, RRect inner, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape("drawDRRect", call => call with { RRect = outer }, CanvasShapeKind.DRRect, paint, op =>
        {
            op.RRect = outer;
            op.Inner = inner;
        });
    }

    /// <summary>Draws an axis-aligned oval with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawOval(rect, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawOval(Rect oval, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape("drawOval", call => call with { Rect = oval }, CanvasShapeKind.Oval, paint, op => op.Rect = oval);
    }

    /// <summary>Draws a path with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawPath(path, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawPath(Path path, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(paint);
        var snapshot = new Path(path);
        DrawShape(
            "drawPath", call => call with { Path = snapshot }, CanvasShapeKind.Path, paint, op => op.Path = snapshot);
    }

    /// <summary>Draws a rounded superellipse with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawRSuperellipse(rsuperellipse, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawRSuperellipse(RSuperellipse rsuperellipse, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        Path path = rsuperellipse.ToPath();
        DrawShape(
            "drawRSuperellipse", call => call with { Path = path }, CanvasShapeKind.Path, paint, op => op.Path = path);
    }

    /// <summary>Draws a circle with the given <see cref="Paint"/>.</summary>
    /// <remarks>Dart's <c>Canvas.drawCircle(c, radius, paint)</c>; see <see cref="DrawShape"/>.</remarks>
    public virtual void DrawCircle(Offset center, double radius, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DrawShape(
            "drawCircle",
            call => call with { Center = center, Radius = radius },
            CanvasShapeKind.Circle,
            paint,
            op =>
            {
                op.Start = center;
                op.Radius = Math.Max(0, radius);
            });
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

        DrawShape(
            "drawArc",
            call => call with { Rect = rect, Path = path },
            CanvasShapeKind.Path,
            paint,
            op => op.Path = path);
    }

    /// <summary>Draws a line between the given points with the given <see cref="Paint"/>.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawLine(p1, p2, paint)</c>: the paint's style is ignored and the line is
    /// always stroked; see <see cref="DrawShape"/> for the mask filter and blend mode. A zero-width
    /// solid stroke is Skia's one-device-pixel hairline.
    /// </remarks>
    public virtual void DrawLine(Offset startPoint, Offset endPoint, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        var stroke = new Paint(paint) { Style = PaintingStyle.Stroke };
        if (paint.StrokeWidth == 0.0 && paint.Shader is null
            && paint.MaskFilter is null && paint.BlendMode == BlendMode.SourceOver)
        {
            DebugRecordCall(new CanvasCall(
                "drawLine",
                Offset: startPoint,
                EndOffset: endPoint,
                BlendMode: paint.BlendMode,
                Paint: stroke) { RecordedColor = paint.Color });
            AddOp(new DrawHairlineOp(startPoint, endPoint, paint.Color, paint.IsAntiAlias, paint.StrokeCap));
            return;
        }

        DrawShape(
            "drawLine",
            call => call with { Offset = startPoint, EndOffset = endPoint },
            CanvasShapeKind.Line,
            stroke,
            op =>
            {
                op.Start = startPoint;
                op.End = endPoint;
            });
    }

    /// <summary>
    /// Records one shape draw of a <see cref="Paint"/>-taking method: the shared tail of <c>drawRect</c>,
    /// <c>drawRRect</c>, <c>drawDRRect</c>, <c>drawOval</c>, <c>drawCircle</c>, <c>drawPath</c>,
    /// <c>drawArc</c>, <c>drawLine</c> and <c>drawRSuperellipse</c>.
    /// </summary>
    /// <remarks>
    /// The paint is read when the call is made, as Dart's canvas does: the entry keeps a snapshot of it,
    /// with a fragment shader's current uniforms. How a mask filter or blend mode is honoured is the
    /// backend's business (Plumix.Avalonia's CanvasPlayback).
    /// </remarks>
    private void DrawShape(
        string method,
        Func<CanvasCall, CanvasCall> describe,
        CanvasShapeKind kind,
        Paint paint,
        Action<DrawShapeOp> shape)
    {
        Paint snapshot = Snapshot(paint);
        DebugRecordCall(describe(new CanvasCall(
            method,
            MaskFilter: snapshot.MaskFilter,
            BlendMode: snapshot.BlendMode,
            Paint: snapshot)
        {
            RecordedColor = snapshot.Color,
        }));
        var op = new DrawShapeOp(kind, snapshot);
        shape(op);
        AddOp(op);
    }

    // The paint as it is now, owning a snapshot of its shader.
    private static Paint Snapshot(Paint paint)
    {
        var snapshot = new Paint(paint);
        if (paint.Shader is { } shader)
        {
            snapshot.Shader = shader.Snapshot();
        }

        return snapshot;
    }

    // Skia's three-sigma blur pad, as the host's mask-filter rasterizer (PaintRasterizer.BlurExtent) pads.
    private static double BlurExtent(double sigma) => sigma > 0.0 ? Math.Ceiling(sigma * 3.0) : 0.0;

    // Paragraph glyph outlines use the same Paint pipeline as framework-owned shapes.
    internal void DrawTextGeometry(Avalonia.Media.Geometry geometry, Paint paint)
    {
        bool filter = paint.ColorFilter is not null || paint.ImageFilter is not null || paint.InvertColors;
        if (filter)
        {
            Avalonia.Rect bounds = geometry.GetRenderBounds(
                new Avalonia.Media.Pen(Avalonia.Media.Brushes.Black, paint.StrokeWidth));
            SaveLayer(
                bounds.Inflate(BlurExtent(paint.MaskFilter?.Sigma ?? 0.0) + 1.0).ToPlumix(),
                new Paint
                {
                    ColorFilter = paint.ColorFilter,
                    ImageFilter = paint.ImageFilter,
                    InvertColors = paint.InvertColors,
                    BlendMode = paint.BlendMode,
                });
        }

        Paint effective = filter ? new Paint(paint) { BlendMode = BlendMode.SourceOver } : paint;
        DrawShape("drawTextGeometry", call => call, CanvasShapeKind.BackendShape, effective,
            op => op.BackendShape = geometry);
        if (filter)
        {
            Restore();
        }
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

    /// <summary>
    /// Draws a shadow for a rectangle, rounded rectangle, or arbitrary path, as if it were the outline of a
    /// physical object at <paramref name="elevation"/> above the canvas.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawShadow(path, color, elevation, transparentOccluder)</c>, rendered as the Flutter
    /// engine's Skia backend does (see Plumix.Avalonia's ShadowDraw): the elevation is scaled by the implicit
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
        AddOp(new DrawShadowOp(occluder, color, elevation, transparentOccluder, devicePixelRatio));
    }

    /// <summary>
    /// Draws the given image into the canvas with its top-left corner at the given offset, composited
    /// with the given <see cref="Paint"/>.
    /// </summary>
    /// <remarks>Dart's <c>Canvas.drawImage(image, offset, paint)</c>.</remarks>
    public virtual void DrawImage(RasterImage image, Offset offset, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(paint);
        RecordImageDraw(
            new CanvasCall("drawImage", Offset: offset, Image: image),
            ImageDrawKind.Image,
            image,
            default,
            offset & ImageSize(image),
            paint);
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
            new CanvasCall("drawImageRect", Rect: dst, SourceRect: src, Image: image),
            ImageDrawKind.ImageRect,
            image,
            src,
            dst,
            paint);
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
        RecordImageDraw(
            new CanvasCall("drawImageNine", Rect: dst, SourceRect: center, Image: image),
            ImageDrawKind.ImageNine,
            image,
            center,
            dst,
            paint);
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

    // The image's size in dart:ui image pixels: the backend image's own size (decoded bitmaps are 96 DPI,
    // one unit per pixel), as RenderImage and ImageInfo measure it.
    internal static Size ImageSize(RasterImage image) => image.LogicalSize;

    // Records one image draw of a Paint-taking method. The paint is read now, as Dart's canvas does.
    private void RecordImageDraw(
        CanvasCall call,
        ImageDrawKind kind,
        RasterImage image,
        Rect source,
        Rect destination,
        Paint paint)
    {
        var snapshot = new Paint(paint);
        DebugRecordCall(call with { Paint = snapshot, BlendMode = snapshot.BlendMode, RecordedColor = snapshot.Color });
        var draw = new DrawImageOp(kind, image, source, destination, snapshot);

        // Skia applies a draw's image filter to the draw's output, as if the draw had its own layer.
        if (snapshot.ImageFilter is null)
        {
            AddOp(draw);
            return;
        }

        var layerPaint = new Paint { ImageFilter = snapshot.ImageFilter };
        var layer = new CanvasLayer(destination, layerPaint, Matrix4.Copy(_transform));
        AddOp(new SaveLayerOp(layer));
        AddOp(draw);
        layer.RestoreIndex = _commands.Count;
        AddOp(RestoreOp.Instance);
    }
}
