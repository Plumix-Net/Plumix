using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas/Picture/PictureRecorder (a recording canvas; the picture is a list
// of typed entries, CanvasOps.cs, that a backend plays back — CanvasPlayback.cs for Avalonia/Skia)

/// <summary>Defines how a new clip region should be merged with the existing clip region.</summary>
/// <remarks>Dart's <c>ClipOp</c>; used by <see cref="Canvas.ClipRect"/>.</remarks>
public enum ClipOp
{
    /// <summary>Subtract the new region from the existing region.</summary>
    Difference,

    /// <summary>Intersect the new region from the existing region.</summary>
    Intersect,
}

/// <summary>
/// What a recorded <c>Canvas.saveLayer</c> needs on playback: the layer's bounds and the parts of its paint
/// that apply when the layer is composited back (Dart's <c>saveLayer(bounds, paint)</c>).
/// </summary>
internal sealed class CanvasLayer
{
    internal CanvasLayer(Rect? bounds, Paint paint, Matrix4 transform)
    {
        Bounds = bounds;
        Alpha = (byte)paint.Color.Alpha;
        BlendMode = paint.BlendMode;
        ColorFilter = paint.ColorFilter;
        ImageFilter = paint.ImageFilter;
        InvertColors = paint.InvertColors;
        Transform = transform;
    }

    /// <summary>The layer bounds in the canvas's local coordinates, or null for the current clip.</summary>
    internal Rect? Bounds { get; }

    /// <summary>The alpha of the paint's colour, applied to the whole group.</summary>
    internal byte Alpha { get; }

    internal BlendMode BlendMode { get; }

    internal ColorFilter? ColorFilter { get; }

    internal ImageFilter? ImageFilter { get; }

    internal bool InvertColors { get; }

    /// <summary>The canvas transform when the layer was saved (Dart's <c>getTransform</c>).</summary>
    internal Matrix4 Transform { get; }

    /// <summary>The index of the matching restore in the picture's commands; the end when never restored.</summary>
    internal int RestoreIndex { get; set; } = -1;

    /// <summary>
    /// Whether compositing the layer needs more than an alpha: a blend mode other than source-over, a colour
    /// or image filter, or inverted colours. Such a layer is rasterized; an alpha-only layer is a backend
    /// isolated group.
    /// </summary>
    internal bool NeedsRaster =>
        BlendMode != BlendMode.SourceOver || ColorFilter is not null || ImageFilter is not null || InvertColors;

}

/// <summary>
/// Plumix-only: a debug description of one recorded canvas call, the counterpart of the entries
/// flutter_test's <c>TestRecordingCanvas</c> keeps for its <c>paints</c> matcher.
/// </summary>
/// <remarks>Recorded only in debug builds; <see cref="Method"/> is the Dart method name
/// (<c>drawRect</c>, <c>drawPath</c>, <c>drawCircle</c>, <c>drawParagraph</c>, <c>save</c>, ...).</remarks>
internal readonly record struct CanvasCall(
    string Method,
    Rect? Rect = null,
    RRect? RRect = null,
    Offset? Offset = null,
    Path? Path = null,
    Offset? Center = null,
    double? Radius = null,
    Offset? EndOffset = null,
    Color? ShadowColor = null,
    double? Elevation = null,
    double? Dx = null,
    double? Dy = null,
    MaskFilter? MaskFilter = null,
    BlendMode? BlendMode = null,
    Rect? SourceRect = null,
    Paint? Paint = null,
    RasterImage? Image = null)
{
    /// <summary>
    /// The paint colour (Dart's <c>Paint.color</c>): the colour captured with the call, else the shadow
    /// colour, else the colour of a paint without a shader.
    /// </summary>
    public Color? Color => RecordedColor ?? ShadowColor ?? (Paint is { Shader: null } paint ? paint.Color : null);

    /// <summary>The paint colour captured when the call was recorded.</summary>
    public Color? RecordedColor { get; init; }

    /// <summary>Dart's <c>Paint.style</c> of the call's paint; a call without a paint fills.</summary>
    public PaintingStyle Style => Paint?.Style ?? PaintingStyle.Fill;

    /// <summary>Dart's <c>Paint.strokeWidth</c> of a stroking call, 0 for a fill.</summary>
    public double StrokeWidth => Paint is { Style: PaintingStyle.Stroke } paint ? paint.StrokeWidth : 0.0;
}

/// <summary>Signature for <see cref="Picture"/> lifecycle events.</summary>
/// <remarks>Dart's <c>ui.PictureEventCallback</c>.</remarks>
public delegate void PictureEventCallback(Picture picture);

/// <summary>An object representing a sequence of recorded graphical operations.</summary>
/// <remarks>
/// Dart's <c>ui.Picture</c>: the typed entries a <see cref="Canvas"/> recorded (the engine's display
/// list), which the host backend plays back.
/// </remarks>
public sealed class Picture
{
    private readonly IReadOnlyList<CanvasOp> _commands;
    private bool _disposed;

    internal Picture(IReadOnlyList<CanvasOp> commands, IReadOnlyList<CanvasCall>? debugCalls = null)
    {
        _commands = commands;
        DebugCalls = debugCalls ?? [];
    }

    /// <summary>A callback that is invoked to report a picture creation.</summary>
    /// <remarks>
    /// It's preferred to use <see cref="FlutterMemoryAllocations"/> than to use
    /// <see cref="OnCreate"/> directly because <see cref="FlutterMemoryAllocations"/> allows
    /// multiple callbacks.
    /// </remarks>
    public static PictureEventCallback? OnCreate { get; set; }

    /// <summary>A callback that is invoked to report the picture disposal.</summary>
    /// <remarks>
    /// It's preferred to use <see cref="FlutterMemoryAllocations"/> than to use
    /// <see cref="OnDispose"/> directly because <see cref="FlutterMemoryAllocations"/> allows
    /// multiple callbacks.
    /// </remarks>
    public static PictureEventCallback? OnDispose { get; set; }

    /// <summary>Plumix-only: the debug descriptions of the recorded calls, in order.</summary>
    internal IReadOnlyList<CanvasCall> DebugCalls { get; }

    /// <summary>An empty picture, the state of a <see cref="PictureLayer"/> that never recorded.</summary>
    public static Picture Empty { get; } = new([]);

    /// <summary>The number of recorded entries; Plumix-only, used by tests and diagnostics.</summary>
    public int CommandCount => _commands.Count;

    /// <summary>The number of recorded draw calls, ignoring clip/transform/save entries.</summary>
    public int DrawCommandCount
    {
        get
        {
            int count = 0;
            for (int index = 0; index < _commands.Count; index++)
            {
                if (_commands[index].IsDraw)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Whether nothing was recorded into this picture.</summary>
    public bool IsEmpty => _commands.Count == 0;

    /// <summary>Whether <see cref="Dispose"/> has been called.</summary>
    /// <remarks>Dart's <c>Picture.debugDisposed</c>; only available in debug builds.</remarks>
    public bool DebugDisposed => Constants.KDebugMode
        ? _disposed
        : throw new InvalidOperationException("Picture.debugDisposed is only available when asserts are enabled.");

    /// <summary>Releases this picture. It may no longer be added to a scene.</summary>
    /// <remarks>
    /// Dart's <c>Picture.dispose</c>. A scene that already holds the picture keeps drawing it, as the
    /// engine's display-list layer keeps its own reference.
    /// </remarks>
    public void Dispose()
    {
        if (Constants.KDebugMode)
        {
            if (_disposed)
            {
                throw new AssertionError("A Picture cannot be disposed more than once.");
            }

            _disposed = true;
        }

        OnDispose?.Invoke(this);
    }

    /// <summary>Rasterizes this picture into a new image of the specified pixel size.</summary>
    public RasterImage ToImageSync(int width, int height)
    {
        if (Constants.KDebugMode && _disposed)
        {
            throw new AssertionError("A disposed Picture cannot be rasterized.");
        }

        var builder = new SceneBuilder();
        builder.AddPicture(default, this);
        using var scene = builder.Build();
        return scene.ToImageSync(width, height);
    }

    /// <summary>Rasterizes this picture into a new image.</summary>
    public Task<RasterImage> ToImage(int width, int height) =>
        Task.FromResult(ToImageSync(width, height));

    /// <summary>The recorded entries, in order.</summary>
    internal IReadOnlyList<CanvasOp> Ops => _commands;

    /// <summary>Replays the recorded operations, translated by <paramref name="offset"/>.</summary>
    internal void Playback(Avalonia.Media.DrawingContext context, Offset offset)
    {
        Playback(context, offset, PaintRasterizer.RasterScale);
    }

    /// <summary>
    /// Replays the recorded operations, translated by <paramref name="offset"/>, rasterizing any layer that
    /// needs it at <paramref name="deviceScale"/> device pixels per picture unit.
    /// </summary>
    internal void Playback(Avalonia.Media.DrawingContext context, Offset offset, double deviceScale) =>
        CanvasPlayback.Play(_commands, context, offset, deviceScale);
}

/// <summary>Records a <see cref="Picture"/> containing the drawing operations of a <see cref="Canvas"/>.</summary>
/// <remarks>Dart's <c>ui.PictureRecorder</c>.</remarks>
public sealed class PictureRecorder
{
    private List<CanvasOp>? _commands;
    private List<CanvasCall>? _debugCalls;
    private Picture? _picture;

    /// <summary>Whether this object is currently recording commands.</summary>
    public bool IsRecording => _commands is not null && _picture is null;

    internal List<CanvasOp> BeginRecording()
    {
        if (_commands is not null)
        {
            throw new InvalidOperationException("PictureRecorder is already associated with a Canvas.");
        }

        _commands = [];
        _debugCalls = [];
        return _commands;
    }

    /// <summary>The debug call log the canvas attached by <see cref="BeginRecording"/> appends to.</summary>
    internal List<CanvasCall> DebugCallsForRecording => _debugCalls ??= [];

    /// <summary>Finishes recording and returns the picture that was recorded.</summary>
    public Picture EndRecording()
    {
        if (_picture is not null)
        {
            throw new InvalidOperationException("PictureRecorder.EndRecording was called more than once.");
        }

        Picture picture = new(_commands ?? [], _debugCalls);
        _picture = picture;
        // We invoke the handler here, not in the Picture constructor, as Dart does.
        Picture.OnCreate?.Invoke(picture);
        return picture;
    }
}

/// <summary>An interface for recording graphical operations.</summary>
/// <remarks>
/// Dart's <c>ui.Canvas</c>. The clip/transform/save stack, its queries and <c>saveLayer</c> are ported 1:1;
/// every call appends a typed entry (<see cref="CanvasOp"/>) to the picture.
/// </remarks>
public partial class Canvas
{
    // Dart's Rect.largest: the default cull rect of a recording canvas.
    internal static readonly Rect LargestRect = Rect.FromLTWH(-1.0e9, -1.0e9, 2.0e9, 2.0e9);

    private readonly List<CanvasOp> _commands;
    private readonly List<CanvasCall> _debugCalls;

    // One entry per save/saveLayer: the transform and clip to restore, and the layer it opened (if any).
    private readonly Stack<(Matrix4 Transform, Rect ClipBounds, CanvasLayer? Layer)> _saveStack = new();
    private Matrix4 _transform = Matrix4.Identity();
    private Rect _destinationClipBounds;
    private int _saveCount = 1;

    /// <summary>Creates a canvas for recording graphical operations into the given recorder.</summary>
    /// <remarks>
    /// Dart's <c>Canvas(recorder, [cullRect])</c>: <paramref name="cullRect"/> bounds what the picture can
    /// draw; it defaults to the largest rect, so nothing is culled.
    /// </remarks>
    public Canvas(PictureRecorder recorder, Rect? cullRect = null)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        _commands = recorder.BeginRecording();
        _debugCalls = recorder.DebugCallsForRecording;
        _destinationClipBounds = cullRect ?? LargestRect;
    }

    /// <summary>
    /// Creates a canvas attached to no recorder, for an implementation that overrides every call (Dart's
    /// <c>implements Canvas</c>, such as the widget inspector's multicast canvas).
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas</c> is an interface class; Plumix's records commands, so an implementation
    /// that forwards elsewhere starts from an empty, unused command list.
    /// </remarks>
    protected Canvas()
    {
        _commands = [];
        _debugCalls = [];
        _destinationClipBounds = LargestRect;
    }

    /// <summary>Plumix-only: the debug descriptions of the calls recorded so far, in order.</summary>
    internal IReadOnlyList<CanvasCall> DebugCalls => _debugCalls;

    /// <summary>
    /// Plumix-only: whether <see cref="DebugRecordCall"/> records outside debug builds too.
    /// </summary>
    /// <remarks>
    /// The test assembly turns it on: flutter_test's <c>TestRecordingCanvas</c>, which the
    /// <c>paints</c> matcher reads, records in every build mode, so the ported paint tests keep their
    /// subject under <c>-c Profile</c> and <c>-c Release</c>. Apps never set it.
    /// </remarks>
    internal static bool DebugRecordCallsInEveryBuildMode { get; set; }

    /// <summary>
    /// Plumix-only: appends a debug description of a call; a no-op outside debug builds unless
    /// <see cref="DebugRecordCallsInEveryBuildMode"/> is set.
    /// </summary>
    internal void DebugRecordCall(CanvasCall call)
    {
        if (Constants.KDebugMode || DebugRecordCallsInEveryBuildMode)
        {
            _debugCalls.Add(call);
        }
    }

    /// <summary>Returns the number of items on the save stack.</summary>
    /// <remarks>Dart's <c>Canvas.getSaveCount</c>.</remarks>
    public virtual int GetSaveCount() => _saveCount;

    /// <summary>Saves a copy of the current transform and clip on the save stack.</summary>
    public virtual void Save()
    {
        DebugRecordCall(new CanvasCall("save"));
        _saveStack.Push((Matrix4.Copy(_transform), _destinationClipBounds, null));
        _saveCount++;
        AddOp(SaveOp.Instance);
    }

    /// <summary>
    /// Saves a copy of the current transform and clip on the save stack, and then creates a new group
    /// which subsequent calls will become a part of. When the save stack is later popped, the group will be
    /// flattened into a layer and have the given <paramref name="paint"/>'s <c>ColorFilter</c> and
    /// <c>BlendMode</c> applied.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.saveLayer(bounds, paint)</c>. A null <paramref name="bounds"/> bounds the layer by the
    /// current clip. A layer whose paint only carries an alpha is an isolated backend group (Skia's
    /// <c>saveLayer</c>, through an opacity mask); one with a blend mode, colour or image filter or inverted
    /// colours is rasterized on playback and composited as an image (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public virtual void SaveLayer(Rect? bounds, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        DebugRecordCall(new CanvasCall("saveLayer", Rect: bounds, BlendMode: paint.BlendMode, Paint: new Paint(paint))
        {
            RecordedColor = paint.Color,
        });
        Rect? layerBounds = bounds ?? FiniteOrNull(GetLocalClipBounds());
        var layer = new CanvasLayer(layerBounds, paint, Matrix4.Copy(_transform));
        _saveStack.Push((Matrix4.Copy(_transform), _destinationClipBounds, layer));
        _saveCount++;
        AddOp(new SaveLayerOp(layer));
    }

    /// <summary>Pops the current save stack, if there is anything to pop.</summary>
    public virtual void Restore()
    {
        DebugRecordCall(new CanvasCall("restore"));
        if (_saveCount <= 1)
        {
            return;
        }

        _saveCount--;
        (Matrix4 transform, Rect clipBounds, CanvasLayer? layer) = _saveStack.Pop();
        _transform = transform;
        _destinationClipBounds = clipBounds;
        if (layer is not null)
        {
            layer.RestoreIndex = _commands.Count;
        }

        AddOp(RestoreOp.Instance);
    }

    /// <summary>Restores the save stack to a previous level as returned by <see cref="GetSaveCount"/>.</summary>
    public virtual void RestoreToCount(int count)
    {
        while (_saveCount > count && _saveCount > 1)
        {
            Restore();
        }
    }

    /// <summary>Adds a translation to the current transform.</summary>
    public virtual void Translate(double dx, double dy)
    {
        DebugRecordCall(new CanvasCall("translate", Dx: dx, Dy: dy));
        if (dx == 0.0 && dy == 0.0)
        {
            return;
        }

        _transform.Multiply(Matrix4.TranslationValues(dx, dy, 0.0));
        AddOp(TransformOp.Translate(dx, dy));
    }

    /// <summary>Adds an axis-aligned scale to the current transform.</summary>
    public virtual void Scale(double sx, double? sy = null)
    {
        double scaleY = sy ?? sx;
        DebugRecordCall(new CanvasCall("scale", Dx: sx, Dy: scaleY));
        _transform.Multiply(Matrix4.Diagonal3Values(sx, scaleY, 1.0));
        AddOp(TransformOp.Scale(sx, scaleY));
    }

    /// <summary>Adds a rotation, in radians, to the current transform.</summary>
    public virtual void Rotate(double radians)
    {
        DebugRecordCall(new CanvasCall("rotate", Radius: radians));
        _transform.Multiply(Matrix4.RotationZ(radians));
        AddOp(TransformOp.Rotate(radians));
    }

    /// <summary>
    /// Adds an axis-aligned skew to the current transform, with the first argument being the horizontal
    /// skew in rise over run units clockwise around the origin, and the second argument being the vertical
    /// skew in rise over run units clockwise around the origin.
    /// </summary>
    /// <remarks>Dart's <c>Canvas.skew</c>.</remarks>
    public virtual void Skew(double sx, double sy)
    {
        DebugRecordCall(new CanvasCall("skew", Dx: sx, Dy: sy));
        Matrix4 skew = Matrix4.Identity();
        skew.SetEntry(0, 1, sx);
        skew.SetEntry(1, 0, sy);
        _transform.Multiply(skew);
        AddOp(TransformOp.ForMatrix(skew));
    }

    /// <summary>Multiplies the current transform by the given matrix.</summary>
    /// <remarks>Dart's <c>Canvas.transform</c>, which takes the matrix's column-major storage.</remarks>
    public virtual void Transform(Matrix4 matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        _transform.Multiply(matrix);
        AddOp(TransformOp.ForMatrix(Matrix4.Copy(matrix)));
    }

    /// <summary>Returns the current transform including the combined result of all transform methods
    /// executed since the creation of this canvas object, and respecting the save/restore history.</summary>
    /// <remarks>Dart's <c>Canvas.getTransform</c>: the 16 values of the matrix, in column-major order.</remarks>
    public virtual double[] GetTransform() => (double[])_transform.Storage.Clone();

    /// <summary>
    /// Returns the conservative bounds of the combined result of all clip methods executed within the
    /// current save stack of this canvas, measured relative to the origin and coordinate space of the
    /// current transform.
    /// </summary>
    /// <remarks>Dart's <c>Canvas.getLocalClipBounds</c>; empty when the transform cannot be inverted.</remarks>
    public virtual Rect GetLocalClipBounds()
    {
        if (Matrix4.TryInvert(_transform) is not { } inverse)
        {
            return default;
        }

        return MatrixUtils.TransformRect(inverse, _destinationClipBounds);
    }

    /// <summary>
    /// Returns the conservative bounds of the combined result of all clip methods executed within the
    /// current save stack of this canvas, measured relative to the origin and coordinate space of the
    /// recording surface (the picture).
    /// </summary>
    /// <remarks>Dart's <c>Canvas.getDestinationClipBounds</c>.</remarks>
    public virtual Rect GetDestinationClipBounds() => _destinationClipBounds;

    /// <summary>Reduces the clip region to the intersection of the current clip and the given rectangle.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.clipRect(rect, clipOp:, doAntiAlias:)</c>. A <see cref="ClipOp.Difference"/> clip
    /// removes the rectangle from the clip and, as in the engine, leaves the clip bounds unchanged.
    /// </remarks>
    public virtual void ClipRect(Rect rect, bool doAntiAlias = true, ClipOp clipOp = ClipOp.Intersect)
    {
        DebugRecordCall(new CanvasCall("clipRect", Rect: rect));
        if (clipOp == ClipOp.Difference)
        {
            AddOp(new ClipRectOp(rect, ClipOp.Difference));
            return;
        }

        IntersectClipBounds(rect, doAntiAlias);
        AddOp(new ClipRectOp(rect, ClipOp.Intersect));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given rounded rect.</summary>
    public virtual void ClipRRect(RRect rrect, bool doAntiAlias = true)
    {
        DebugRecordCall(new CanvasCall("clipRRect", RRect: rrect));
        IntersectClipBounds(rrect.Rect, doAntiAlias);
        AddOp(new ClipRRectOp(rrect));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given shape.</summary>
    public virtual void ClipRSuperellipse(RSuperellipse rsuperellipse, bool doAntiAlias = true)
    {
        ClipPath(rsuperellipse.ToPath(), doAntiAlias);
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given path.</summary>
    public virtual void ClipPath(Path path, bool doAntiAlias = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugRecordCall(new CanvasCall("clipPath", Path: path));
        IntersectClipBounds(path.GetBounds(), doAntiAlias);
        AddOp(new ClipPathOp(new Path(path)));
    }

    // The engine's clip-coverage tracking: the clip's bounds in destination space, rounded out to whole
    // pixels for an anti-aliased clip, intersected with the current coverage.
    private void IntersectClipBounds(Rect localBounds, bool doAntiAlias)
    {
        Rect bounds = MatrixUtils.TransformRect(_transform, localBounds);
        if (doAntiAlias)
        {
            double left = Math.Floor(bounds.Left);
            double top = Math.Floor(bounds.Top);
            bounds = Rect.FromLTWH(left, top, Math.Ceiling(bounds.Right) - left, Math.Ceiling(bounds.Bottom) - top);
        }

        Rect intersection = _destinationClipBounds.Intersect(bounds);
        _destinationClipBounds = intersection.Width > 0.0 && intersection.Height > 0.0 ? intersection : default;
    }

    private static Rect? FiniteOrNull(Rect rect) =>
        double.IsFinite(rect.Width) && double.IsFinite(rect.Height) ? rect : null;

    /// <summary>Draws the text in the given <see cref="Paragraph"/> into this canvas at the given offset.</summary>
    /// <remarks>
    /// Dart's <c>Canvas.drawParagraph</c>. The paragraph's current layout is captured when the call is
    /// recorded, so a later relayout of the same paragraph does not change the recorded picture.
    /// </remarks>
    public virtual void DrawParagraph(Paragraph paragraph, Offset offset)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        DebugRecordCall(new CanvasCall("drawParagraph", Offset: offset));
        Action<Avalonia.Media.DrawingContext>? draw = paragraph.CreateDrawAction(offset);
        AddOp(new BackendOp(draw ?? (static _ => { }), isDraw: true));
    }

    /// <summary>
    /// Plumix-only: multiplies the alpha of everything drawn until the matching restore by a linear fade
    /// from opaque at <paramref name="from"/> to transparent at <paramref name="to"/> (both relative to a
    /// box of <paramref name="size"/>), over <paramref name="bounds"/>.
    /// </summary>
    /// <remarks>
    /// Stands in for Dart's <c>saveLayer</c> + <c>drawRect(BlendMode.modulate, shader)</c> fade, which
    /// needs an isolated layer Avalonia's drawing context does not expose.
    /// </remarks>
    internal virtual void PushFadeMask(Offset from, Offset to, Size size, Rect bounds)
    {
        AddOp(new FadeMaskOp(from, to, size, bounds));
    }

    /// <summary>Plumix-only: appends a recorded entry to the picture.</summary>
    internal virtual void AddOp(CanvasOp op)
    {
        ArgumentNullException.ThrowIfNull(op);
        _commands.Add(op);
    }
}
