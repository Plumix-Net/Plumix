using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas/Picture/PictureRecorder (recording surface over Avalonia's
// DrawingContext; some paint arguments stay Avalonia brushes/pens, see docs/ai/DIVERGENCES.md)

/// <summary>The kinds of entry a <see cref="Canvas"/> records into a <see cref="Picture"/>.</summary>
internal enum CanvasCommandKind
{
    /// <summary>A drawing operation that leaves no state behind.</summary>
    Draw,

    /// <summary>A clip/transform that stays in effect until the matching restore.</summary>
    Push,

    /// <summary>`Canvas.save`: marks the point a later restore unwinds to.</summary>
    Save,

    /// <summary>`Canvas.restore`: unwinds every push made since the matching save.</summary>
    Restore,

    /// <summary>`Canvas.saveLayer`: a save that also opens an isolated group, composited on restore.</summary>
    SaveLayer,
}

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

    /// <summary>The rasterized group and the scale it was rasterized at, reused across playbacks.</summary>
    internal (WriteableBitmap? Bitmap, Rect Destination, double Scale)? Raster { get; set; }

    /// <summary>Whether rasterizing failed once (no raster backend), so playback falls back for good.</summary>
    internal bool RasterUnavailable { get; set; }
}

/// <summary>One recorded canvas entry.</summary>
internal readonly record struct CanvasCommand(
    CanvasCommandKind Kind,
    Action<DrawingContext>? Draw,
    Func<DrawingContext, DrawingContext.PushedState>? Push,
    CanvasLayer? Layer = null)
{
    internal static CanvasCommand ForDraw(Action<DrawingContext> draw) =>
        new(CanvasCommandKind.Draw, draw, null);

    internal static CanvasCommand ForPush(Func<DrawingContext, DrawingContext.PushedState> push) =>
        new(CanvasCommandKind.Push, null, push);

    internal static CanvasCommand ForSave() => new(CanvasCommandKind.Save, null, null);

    internal static CanvasCommand ForSaveLayer(CanvasLayer layer) =>
        new(CanvasCommandKind.SaveLayer, null, null, layer);

    internal static CanvasCommand ForRestore() => new(CanvasCommandKind.Restore, null, null);
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
    IBrush? Brush = null,
    IPen? Pen = null,
    Point? Offset = null,
    Path? Path = null,
    Point? Center = null,
    double? Radius = null,
    Point? EndOffset = null,
    Color? ShadowColor = null,
    double? Elevation = null,
    double? Dx = null,
    double? Dy = null,
    MaskFilter? MaskFilter = null,
    BlendMode? BlendMode = null,
    Rect? SourceRect = null,
    Paint? Paint = null,
    IImage? Image = null)
{
    /// <summary>
    /// The paint colour (Dart's <c>Paint.color</c>): the shadow colour, else the fill brush's, else the
    /// stroke pen's.
    /// </summary>
    public Color? Color => RecordedColor ?? ResolvePaintColor();

    /// <summary>The paint colour captured when the call was recorded.</summary>
    public Color? RecordedColor { get; init; }

    internal Color? ResolvePaintColor() => ShadowColor ?? SolidColor(Brush) ?? SolidColor(Pen?.Brush);

    // A mutable Avalonia brush is thread-affine; one created on another thread reports no colour.
    private static Color? SolidColor(IBrush? brush) => brush switch
    {
        ISolidColorBrush solid when brush is not Avalonia.AvaloniaObject owner || owner.CheckAccess() =>
            (Color)solid.Color,
        _ => null,
    };

    /// <summary>Dart's <c>Paint.style</c>: a pen without a brush strokes, anything else fills.</summary>
    public PaintingStyle Style => Brush is null && Pen is not null ? PaintingStyle.Stroke : PaintingStyle.Fill;

    /// <summary>Dart's <c>Paint.strokeWidth</c>: the pen thickness of a stroke, 0 for a fill.</summary>
    public double StrokeWidth => Brush is null && Pen is not null ? Pen.Thickness : 0.0;
}

/// <summary>An object representing a sequence of recorded graphical operations.</summary>
/// <remarks>
/// Dart's <c>ui.Picture</c>. Plumix records Avalonia draw calls rather than a Skia display list, so
/// <see cref="Playback"/> replays them onto a live <see cref="DrawingContext"/> instead of being
/// handed to a scene builder.
/// </remarks>
public sealed class Picture
{
    private readonly IReadOnlyList<CanvasCommand> _commands;
    private bool _disposed;

    internal Picture(IReadOnlyList<CanvasCommand> commands, IReadOnlyList<CanvasCall>? debugCalls = null)
    {
        _commands = commands;
        DebugCalls = debugCalls ?? [];
    }

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
                if (_commands[index].Kind == CanvasCommandKind.Draw)
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
    }

    /// <summary>Rasterizes this picture into a new image of the specified pixel size.</summary>
    public Avalonia.Media.Imaging.Bitmap ToImageSync(int width, int height)
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
    public Task<Avalonia.Media.Imaging.Bitmap> ToImage(int width, int height) =>
        Task.FromResult(ToImageSync(width, height));

    /// <summary>Replays the recorded operations, translated by <paramref name="offset"/>.</summary>
    public void Playback(DrawingContext context, Point offset)
    {
        Playback(context, offset, PaintRasterizer.RasterScale);
    }

    /// <summary>
    /// Replays the recorded operations, translated by <paramref name="offset"/>, rasterizing any layer that
    /// needs it at <paramref name="deviceScale"/> device pixels per picture unit.
    /// </summary>
    internal void Playback(DrawingContext context, Point offset, double deviceScale)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_commands.Count == 0)
        {
            return;
        }

        DrawingContext.PushedState? translation = null;
        try
        {
            if (offset.X != 0.0 || offset.Y != 0.0)
            {
                translation = context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y));
            }

            PlayRange(context, 0, _commands.Count, deviceScale);
        }
        finally
        {
            translation?.Dispose();
        }
    }

    // Replays commands [start, end): a balanced range, so every save in it is restored in it (or at its end).
    private void PlayRange(DrawingContext context, int start, int end, double deviceScale)
    {
        var pushed = new List<DrawingContext.PushedState>();
        var saves = new Stack<int>();
        try
        {
            for (int index = start; index < end; index++)
            {
                CanvasCommand command = _commands[index];
                switch (command.Kind)
                {
                    case CanvasCommandKind.Save:
                        saves.Push(pushed.Count);
                        break;
                    case CanvasCommandKind.SaveLayer:
                        CanvasLayer layer = command.Layer!;
                        int restore = Math.Min(layer.RestoreIndex < 0 ? end : layer.RestoreIndex, end);
                        if (layer.NeedsRaster
                            && TryDrawRasterizedLayer(context, layer, index + 1, restore, deviceScale))
                        {
                            // The group, and its restore, were composited as one image.
                            index = restore;
                            break;
                        }

                        saves.Push(pushed.Count);
                        pushed.Add(PushIsolatedGroup(context, layer));
                        break;
                    case CanvasCommandKind.Restore:
                        Unwind(pushed, saves.Count > 0 ? saves.Pop() : 0);
                        break;
                    case CanvasCommandKind.Push:
                        pushed.Add(command.Push!(context));
                        break;
                    default:
                        command.Draw!(context);
                        break;
                }
            }
        }
        finally
        {
            Unwind(pushed, 0);
        }
    }

    // An alpha-only layer: an opacity mask of the paint's alpha opens an isolated offscreen group on the
    // backend (Skia's saveLayer), which is composited with that alpha when the group is popped.
    private static DrawingContext.PushedState PushIsolatedGroup(DrawingContext context, CanvasLayer layer)
    {
        var mask = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(
            Avalonia.Media.Color.FromArgb(layer.Alpha, 0xFF, 0xFF, 0xFF));
        return context.PushOpacityMask(mask, ClampLayerBounds(layer.Bounds));
    }

    // Avalonia needs finite group bounds; an unbounded layer is bounded by the largest finite rect instead.
    private static Rect ClampLayerBounds(Rect? bounds)
    {
        Rect largest = Canvas.LargestRect;
        if (bounds is not { } rect || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height))
        {
            return largest;
        }

        return rect.Intersect(largest);
    }

    private bool TryDrawRasterizedLayer(
        DrawingContext context,
        CanvasLayer layer,
        int start,
        int end,
        double deviceScale)
    {
        if (layer.RasterUnavailable)
        {
            return false;
        }

        double scale = Math.Clamp(deviceScale * layer.Transform.GetMaxScaleOnAxis(), 0.25, 8.0);
        if (layer.Raster is not { } raster || raster.Scale != scale)
        {
            if (layer.Bounds is not { } bounds || !PaintRasterizer.CanRasterize(bounds))
            {
                return false;
            }

            try
            {
                (WriteableBitmap? bitmap, Rect destination) = FilterLayerRasterizer.RasterizeSaveLayer(
                    drawingContext => PlayRange(drawingContext, start, end, deviceScale),
                    bounds,
                    scale,
                    layer);
                raster = (bitmap, destination, scale);
                layer.Raster = raster;
            }
            catch (InvalidOperationException)
            {
                layer.RasterUnavailable = true;
                return false;
            }
            catch (NotSupportedException)
            {
                layer.RasterUnavailable = true;
                return false;
            }
        }

        if (raster.Bitmap is { } image)
        {
            FilterLayerRasterizer.DrawSaveLayer(context, image, raster.Destination, layer.BlendMode);
        }

        return true;
    }

    private static void Unwind(List<DrawingContext.PushedState> pushed, int target)
    {
        for (int index = pushed.Count - 1; index >= target; index--)
        {
            pushed[index].Dispose();
            pushed.RemoveAt(index);
        }
    }
}

/// <summary>Records a <see cref="Picture"/> containing the drawing operations of a <see cref="Canvas"/>.</summary>
/// <remarks>Dart's <c>ui.PictureRecorder</c>.</remarks>
public sealed class PictureRecorder
{
    private List<CanvasCommand>? _commands;
    private List<CanvasCall>? _debugCalls;
    private Picture? _picture;

    /// <summary>Whether this object is currently recording commands.</summary>
    public bool IsRecording => _commands is not null && _picture is null;

    internal List<CanvasCommand> BeginRecording()
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

        _picture = new Picture(_commands ?? [], _debugCalls);
        return _picture;
    }
}

/// <summary>An interface for recording graphical operations.</summary>
/// <remarks>
/// Dart's <c>ui.Canvas</c>. The clip/transform/save stack, its queries and <c>saveLayer</c> are ported 1:1;
/// some drawing calls still take Avalonia brushes and pens where Dart takes a <c>Paint</c>.
/// </remarks>
public partial class Canvas
{
    // Dart's Rect.largest: the default cull rect of a recording canvas.
    internal static readonly Rect LargestRect = new(-1.0e9, -1.0e9, 2.0e9, 2.0e9);

    private readonly List<CanvasCommand> _commands;
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

    /// <summary>Plumix-only: appends a debug description of a call; a no-op outside debug builds.</summary>
    internal void DebugRecordCall(CanvasCall call)
    {
        if (Constants.KDebugMode)
        {
            // Avalonia brushes are thread-affine: read the paint colour while recording, on the
            // painting thread, so tests can inspect it from any thread.
            _debugCalls.Add(call with { RecordedColor = call.RecordedColor ?? call.ResolvePaintColor() });
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
        _commands.Add(CanvasCommand.ForSave());
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
        _commands.Add(CanvasCommand.ForSaveLayer(layer));
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

        _commands.Add(CanvasCommand.ForRestore());
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
        _commands.Add(CanvasCommand.ForPush(context =>
            context.PushTransform(Matrix.CreateTranslation(dx, dy))));
    }

    /// <summary>Adds an axis-aligned scale to the current transform.</summary>
    public virtual void Scale(double sx, double? sy = null)
    {
        double scaleY = sy ?? sx;
        DebugRecordCall(new CanvasCall("scale", Dx: sx, Dy: scaleY));
        _transform.Multiply(Matrix4.Diagonal3Values(sx, scaleY, 1.0));
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(Matrix.CreateScale(sx, scaleY))));
    }

    /// <summary>Adds a rotation, in radians, to the current transform.</summary>
    public virtual void Rotate(double radians)
    {
        DebugRecordCall(new CanvasCall("rotate", Radius: radians));
        _transform.Multiply(Matrix4.RotationZ(radians));
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(Matrix.CreateRotation(radians))));
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
        Matrix avaloniaMatrix = skew.ToAvaloniaMatrix();
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(avaloniaMatrix)));
    }

    /// <summary>Multiplies the current transform by the given matrix.</summary>
    /// <remarks>Dart's <c>Canvas.transform</c>, which takes the matrix's column-major storage.</remarks>
    public virtual void Transform(Matrix4 matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        _transform.Multiply(matrix);
        Matrix avaloniaMatrix = matrix.ToAvaloniaMatrix();
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(avaloniaMatrix)));
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
            Geometry? difference = null;
            Geometry Difference() => difference ??= new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(LargestRect),
                new RectangleGeometry(rect));
            _commands.Add(CanvasCommand.ForPush(context => context.PushGeometryClip(Difference())));
            return;
        }

        IntersectClipBounds(rect, doAntiAlias);
        _commands.Add(CanvasCommand.ForPush(context => context.PushClip(rect)));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given rounded rect.</summary>
    public virtual void ClipRRect(RRect rrect, bool doAntiAlias = true)
    {
        DebugRecordCall(new CanvasCall("clipRRect", RRect: rrect));
        IntersectClipBounds(rrect.Rect, doAntiAlias);
        _commands.Add(CanvasCommand.ForPush(context => SceneRasterizer.PushRoundedRectClip(context, rrect)));
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

        // The backend geometry is built on playback: recording must not need a render backend.
        Geometry? geometry = null;
        _commands.Add(CanvasCommand.ForPush(context => context.PushGeometryClip(geometry ??= path.ToGeometry())));
    }

    /// <summary>Plumix-only: clips to an Avalonia geometry the caller already built.</summary>
    /// <remarks>
    /// Dart clips through <c>Path</c> only; Plumix models a few shapes (notched app bars, decoration
    /// outlines) as backend geometry, which cannot be shifted, so the offset is applied around it.
    /// </remarks>
    public virtual void ClipGeometry(Geometry geometry, bool doAntiAlias = true, Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        Rect geometryBounds = geometry.Bounds;
        IntersectClipBounds(
            new Rect(geometryBounds.Position + geometryOffset, geometryBounds.Size),
            doAntiAlias);
        if (geometryOffset.X != 0.0 || geometryOffset.Y != 0.0)
        {
            _commands.Add(CanvasCommand.ForPush(context =>
                context.PushTransform(Matrix.CreateTranslation(geometryOffset.X, geometryOffset.Y))));
            _commands.Add(CanvasCommand.ForPush(context => context.PushGeometryClip(geometry)));
            _commands.Add(CanvasCommand.ForPush(context =>
                context.PushTransform(Matrix.CreateTranslation(-geometryOffset.X, -geometryOffset.Y))));
            return;
        }

        _commands.Add(CanvasCommand.ForPush(context => context.PushGeometryClip(geometry)));
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
            bounds = new Rect(left, top, Math.Ceiling(bounds.Right) - left, Math.Ceiling(bounds.Bottom) - top);
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
    public virtual void DrawParagraph(Paragraph paragraph, Point offset)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        DebugRecordCall(new CanvasCall("drawParagraph", Offset: offset));
        Action<DrawingContext>? draw = paragraph.CreateDrawAction(offset);
        AddDrawCommand(draw ?? (static _ => { }));
    }

    /// <summary>
    /// Plumix-only: multiplies the alpha of everything drawn until the matching restore by the alpha of
    /// <paramref name="mask"/> over <paramref name="bounds"/>.
    /// </summary>
    /// <remarks>
    /// Stands in for Dart's <c>saveLayer</c> + <c>drawRect(BlendMode.modulate, shader)</c> fade, which
    /// needs an isolated layer Avalonia's drawing context does not expose.
    /// </remarks>
    internal virtual void PushOpacityMask(IBrush mask, Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(mask);
        _commands.Add(CanvasCommand.ForPush(context => context.PushOpacityMask(mask, bounds)));
    }

    /// <summary>Plumix-only: records a draw call that renders itself onto the backend context.</summary>
    internal virtual void AddDrawCommand(Action<DrawingContext> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _commands.Add(CanvasCommand.ForDraw(draw));
    }
}
