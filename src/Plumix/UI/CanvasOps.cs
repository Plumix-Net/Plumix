namespace Plumix.UI;

// C#-only infrastructure: the typed entries a recording Canvas appends to a Picture, the counterpart of
// the engine's DisplayList ops (display_list/dl_op_records.h). They hold framework data only — paths,
// paints, rects, images — so a picture can be played back by any backend; CanvasPlayback is the
// Avalonia/Skia one (docs/product avalonia-free-core.md, stage 2).

/// <summary>One recorded canvas entry.</summary>
internal abstract class CanvasOp
{
    /// <summary>Whether the entry draws, as opposed to saving, restoring, clipping or transforming.</summary>
    internal virtual bool IsDraw => true;

    /// <summary>
    /// What the backend built from this entry on its first playback (geometry, pens, cached rasters),
    /// reused on every later playback so replaying a retained picture allocates nothing new.
    /// </summary>
    internal object? BackendState { get; set; }
}

/// <summary>An entry that changes the canvas state rather than drawing.</summary>
internal abstract class CanvasStateOp : CanvasOp
{
    internal override bool IsDraw => false;
}

/// <summary>`Canvas.save`: marks the point a later restore unwinds to.</summary>
internal sealed class SaveOp : CanvasStateOp
{
    internal static readonly SaveOp Instance = new();

    private SaveOp()
    {
    }
}

/// <summary>`Canvas.restore`: unwinds every clip and transform made since the matching save.</summary>
internal sealed class RestoreOp : CanvasStateOp
{
    internal static readonly RestoreOp Instance = new();

    private RestoreOp()
    {
    }
}

/// <summary>`Canvas.saveLayer`: a save that also opens an isolated group, composited on restore.</summary>
internal sealed class SaveLayerOp(CanvasLayer layer) : CanvasStateOp
{
    internal CanvasLayer Layer { get; } = layer;
}

/// <summary>The kinds of <see cref="TransformOp"/>: each keeps the arguments it was recorded with.</summary>
internal enum TransformKind
{
    Translate,
    Scale,
    Rotate,
    Matrix,
}

/// <summary>`Canvas.translate`/`scale`/`rotate`/`skew`/`transform`: a transform until the matching restore.</summary>
internal sealed class TransformOp : CanvasStateOp
{
    private TransformOp(TransformKind kind, double x, double y, Matrix4? matrix)
    {
        Kind = kind;
        X = x;
        Y = y;
        Matrix = matrix;
    }

    internal TransformKind Kind { get; }

    /// <summary>The horizontal translation or scale, or the rotation in radians.</summary>
    internal double X { get; }

    /// <summary>The vertical translation or scale.</summary>
    internal double Y { get; }

    /// <summary>The full matrix of a <see cref="TransformKind.Matrix"/> entry.</summary>
    internal Matrix4? Matrix { get; }

    internal static TransformOp Translate(double dx, double dy) => new(TransformKind.Translate, dx, dy, null);

    internal static TransformOp Scale(double sx, double sy) => new(TransformKind.Scale, sx, sy, null);

    internal static TransformOp Rotate(double radians) => new(TransformKind.Rotate, radians, 0.0, null);

    internal static TransformOp ForMatrix(Matrix4 matrix) => new(TransformKind.Matrix, 0.0, 0.0, matrix);
}

/// <summary>`Canvas.clipRect`.</summary>
internal sealed class ClipRectOp(Rect rect, ClipOp clipOp) : CanvasStateOp
{
    internal Rect Rect { get; } = rect;

    internal ClipOp ClipOp { get; } = clipOp;
}

/// <summary>`Canvas.clipRRect`.</summary>
internal sealed class ClipRRectOp(RRect rrect) : CanvasStateOp
{
    internal RRect RRect { get; } = rrect;
}

/// <summary>`Canvas.clipPath` (and `clipRSuperellipse`, recorded as its path).</summary>
internal sealed class ClipPathOp(Path path) : CanvasStateOp
{
    internal Path Path { get; } = path;
}

/// <summary>
/// Plumix-only: multiplies the alpha of what follows, until the matching restore, by a linear fade
/// from opaque at <see cref="From"/> to transparent at <see cref="To"/>, over <see cref="Bounds"/>.
/// </summary>
/// <remarks>
/// Stands in for Dart's <c>saveLayer</c> + <c>drawRect(BlendMode.modulate, shader)</c> fade of
/// <c>TextOverflow.fade</c>; <see cref="Size"/> is the box the fade's end points are relative to.
/// </remarks>
internal sealed class FadeMaskOp(Offset from, Offset to, Size size, Rect bounds) : CanvasStateOp
{
    internal Offset From { get; } = from;

    internal Offset To { get; } = to;

    internal Size Size { get; } = size;

    internal Rect Bounds { get; } = bounds;
}

/// <summary>The shapes a <see cref="DrawShapeOp"/> can describe.</summary>
internal enum CanvasShapeKind
{
    /// <summary>`drawRect`: <see cref="DrawShapeOp.Rect"/>.</summary>
    Rect,

    /// <summary>`drawRRect`: <see cref="DrawShapeOp.RRect"/>.</summary>
    RRect,

    /// <summary>
    /// `drawDRRect`: the ring between <see cref="DrawShapeOp.RRect"/> and <see cref="DrawShapeOp.Inner"/>.
    /// </summary>
    DRRect,

    /// <summary>`drawOval`: the oval inscribed in <see cref="DrawShapeOp.Rect"/>.</summary>
    Oval,

    /// <summary>`drawCircle`: <see cref="DrawShapeOp.Start"/> and <see cref="DrawShapeOp.Radius"/>.</summary>
    Circle,

    /// <summary>`drawPath`, `drawRSuperellipse`, `drawArc`: <see cref="DrawShapeOp.Path"/>.</summary>
    Path,

    /// <summary>
    /// `drawLine`: from <see cref="DrawShapeOp.Start"/> to <see cref="DrawShapeOp.End"/>, always stroked.
    /// </summary>
    Line,

    /// <summary>
    /// Plumix-only: paragraph glyph outlines, held by the backend that shaped them
    /// (<see cref="DrawShapeOp.BackendShape"/>).
    /// </summary>
    BackendShape,
}

/// <summary>
/// One shape draw of a <see cref="Paint"/>-taking method: the shape and the paint as they were when the
/// call was made (Dart's canvas reads the paint at call time).
/// </summary>
internal sealed class DrawShapeOp : CanvasOp
{
    internal DrawShapeOp(CanvasShapeKind kind, Paint paint)
    {
        Kind = kind;
        Paint = paint;
    }

    internal CanvasShapeKind Kind { get; }

    /// <summary>A snapshot of the paint, owned by this entry.</summary>
    internal Paint Paint { get; }

    internal Rect Rect { get; set; }

    internal RRect RRect { get; set; }

    internal RRect Inner { get; set; }

    internal Path? Path { get; set; }

    internal Offset Start { get; set; }

    internal Offset End { get; set; }

    internal double Radius { get; set; }

    /// <summary>The backend's own outline of a <see cref="CanvasShapeKind.BackendShape"/> entry.</summary>
    internal object? BackendShape { get; set; }

    /// <summary>
    /// Whether a solid colour fill or stroke goes to the backend as the exact path rather than its
    /// approximated geometry: the shapes with curves Avalonia's geometry would flatten.
    /// </summary>
    internal bool NativeSolid => Kind is CanvasShapeKind.RRect or CanvasShapeKind.DRRect or CanvasShapeKind.Path;

    /// <summary>The shape as a <see cref="UI.Path"/>, or null for a backend-held shape.</summary>
    internal Path? ToPath()
    {
        switch (Kind)
        {
            case CanvasShapeKind.Rect:
            {
                var path = new Path();
                path.AddRect(Rect);
                return path;
            }

            case CanvasShapeKind.RRect:
                return RRect.ToPath();
            case CanvasShapeKind.DRRect:
            {
                // SkCanvas::onDrawDRRect: both outlines in one even-odd path.
                var path = new Path { FillType = PathFillType.EvenOdd };
                path.AddRRect(RRect);
                path.AddRRect(Inner);
                return path;
            }

            case CanvasShapeKind.Oval:
            {
                var path = new Path();
                path.AddOval(Rect);
                return path;
            }

            case CanvasShapeKind.Circle:
            {
                var path = new Path();
                path.AddOval(CircleBounds);
                return path;
            }

            case CanvasShapeKind.Path:
                return Path;
            case CanvasShapeKind.Line:
            {
                var path = new Path();
                path.MoveTo(Start.Dx, Start.Dy);
                path.LineTo(End.Dx, End.Dy);
                return path;
            }

            default:
                return null;
        }
    }

    /// <summary>The bounds of a <see cref="CanvasShapeKind.Circle"/> entry.</summary>
    internal Rect CircleBounds => Rect.FromLTWH(Start.Dx - Radius, Start.Dy - Radius, Radius * 2.0, Radius * 2.0);
}

/// <summary>`drawLine` with a zero-width solid paint: Skia's one-device-pixel hairline.</summary>
internal sealed class DrawHairlineOp(Offset start, Offset end, Color color, bool isAntiAlias, StrokeCap cap)
    : CanvasOp
{
    internal Offset Start { get; } = start;

    internal Offset End { get; } = end;

    internal Color Color { get; } = color;

    internal bool IsAntiAlias { get; } = isAntiAlias;

    internal StrokeCap Cap { get; } = cap;
}

/// <summary>`drawPaint`: fills the clip with a snapshot of the paint.</summary>
internal sealed class DrawPaintOp(Paint paint) : CanvasOp
{
    internal Paint Paint { get; } = paint;
}

/// <summary>`drawShadow`, with the device pixel ratio the elevation is scaled by.</summary>
internal sealed class DrawShadowOp(Path path, Color color, double elevation, bool transparentOccluder, double dpr)
    : CanvasOp
{
    internal Path Path { get; } = path;

    internal Color Color { get; } = color;

    internal double Elevation { get; } = elevation;

    internal bool TransparentOccluder { get; } = transparentOccluder;

    internal double DevicePixelRatio { get; } = dpr;
}

/// <summary>The image draws a <see cref="DrawImageOp"/> can describe.</summary>
internal enum ImageDrawKind
{
    /// <summary>`drawImage`: the whole image at <see cref="DrawImageOp.Destination"/>'s origin.</summary>
    Image,

    /// <summary>`drawImageRect`: <see cref="DrawImageOp.Source"/> into <see cref="DrawImageOp.Destination"/>.</summary>
    ImageRect,

    /// <summary>`drawImageNine`: split by <see cref="DrawImageOp.Source"/> (the centre) into a 3x3 grid.</summary>
    ImageNine,
}

/// <summary>`drawImage`/`drawImageRect`/`drawImageNine` with a snapshot of the paint.</summary>
internal sealed class DrawImageOp(ImageDrawKind kind, RasterImage image, Rect source, Rect destination, Paint paint)
    : CanvasOp
{
    internal ImageDrawKind Kind { get; } = kind;

    internal RasterImage Image { get; } = image;

    /// <summary>The source rect of an image-rect draw, the centre of a nine-patch draw.</summary>
    internal Rect Source { get; } = source;

    internal Rect Destination { get; } = destination;

    internal Paint Paint { get; } = paint;
}

/// <summary>
/// Plumix-only: an entry whose content only the backend that recorded it can interpret — a laid-out
/// paragraph or a clip to backend geometry. Each goes once text and decoration outlines are typed data
/// too (docs/product avalonia-free-core.md, stage 3).
/// </summary>
internal sealed class BackendOp(object payload, bool isDraw) : CanvasOp
{
    internal object Payload { get; } = payload;

    internal override bool IsDraw { get; } = isDraw;
}
