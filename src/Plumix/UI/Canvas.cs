using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas/Picture/PictureRecorder (recording surface over Avalonia's
// DrawingContext; the paint arguments stay Avalonia brushes/pens, see docs/ai/DIVERGENCES.md)

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
}

/// <summary>One recorded canvas entry.</summary>
internal readonly record struct CanvasCommand(
    CanvasCommandKind Kind,
    Action<DrawingContext>? Draw,
    Func<DrawingContext, DrawingContext.PushedState>? Push)
{
    internal static CanvasCommand ForDraw(Action<DrawingContext> draw) =>
        new(CanvasCommandKind.Draw, draw, null);

    internal static CanvasCommand ForPush(Func<DrawingContext, DrawingContext.PushedState> push) =>
        new(CanvasCommandKind.Push, null, push);

    internal static CanvasCommand ForSave() => new(CanvasCommandKind.Save, null, null);

    internal static CanvasCommand ForRestore() => new(CanvasCommandKind.Restore, null, null);
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

    internal Picture(IReadOnlyList<CanvasCommand> commands)
    {
        _commands = commands;
    }

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

    /// <summary>Replays the recorded operations, translated by <paramref name="offset"/>.</summary>
    public void Playback(DrawingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_commands.Count == 0)
        {
            return;
        }

        var pushed = new List<DrawingContext.PushedState>();
        var saves = new Stack<int>();
        try
        {
            if (offset.X != 0.0 || offset.Y != 0.0)
            {
                pushed.Add(context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)));
            }

            int floor = pushed.Count;
            for (int index = 0; index < _commands.Count; index++)
            {
                CanvasCommand command = _commands[index];
                switch (command.Kind)
                {
                    case CanvasCommandKind.Save:
                        saves.Push(pushed.Count);
                        break;
                    case CanvasCommandKind.Restore:
                        Unwind(pushed, saves.Count > 0 ? saves.Pop() : floor);
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
        return _commands;
    }

    /// <summary>Finishes recording and returns the picture that was recorded.</summary>
    public Picture EndRecording()
    {
        if (_picture is not null)
        {
            throw new InvalidOperationException("PictureRecorder.EndRecording was called more than once.");
        }

        _picture = new Picture(_commands ?? []);
        return _picture;
    }
}

/// <summary>An interface for recording graphical operations.</summary>
/// <remarks>
/// Dart's <c>ui.Canvas</c>. The clip/transform/save stack is ported 1:1; the drawing calls take
/// Avalonia brushes and pens where Dart takes a <c>Paint</c>.
/// </remarks>
public sealed partial class Canvas
{
    private readonly List<CanvasCommand> _commands;
    private int _saveCount = 1;

    /// <summary>Creates a canvas for recording graphical operations into the given recorder.</summary>
    public Canvas(PictureRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        _commands = recorder.BeginRecording();
    }

    /// <summary>Returns the number of items on the save stack.</summary>
    /// <remarks>Dart's <c>Canvas.getSaveCount</c>.</remarks>
    public int GetSaveCount() => _saveCount;

    /// <summary>Saves a copy of the current transform and clip on the save stack.</summary>
    public void Save()
    {
        _saveCount++;
        _commands.Add(CanvasCommand.ForSave());
    }

    /// <summary>
    /// Saves a copy of the current transform and clip on the save stack, and then creates a new group
    /// which subsequent calls will become a part of.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Canvas.saveLayer</c>. Avalonia's public <see cref="DrawingContext"/> exposes no
    /// isolated offscreen surface, so the group is opened as a transparent-preserving opacity push:
    /// the save/restore nesting matches Dart exactly, but blending inside the group is not isolated.
    /// </remarks>
    public void SaveLayer(Rect bounds)
    {
        _saveCount++;
        _commands.Add(CanvasCommand.ForSave());
        _commands.Add(CanvasCommand.ForPush(context => context.PushOpacity(1.0)));
    }

    /// <summary>Pops the current save stack, if there is anything to pop.</summary>
    public void Restore()
    {
        if (_saveCount <= 1)
        {
            return;
        }

        _saveCount--;
        _commands.Add(CanvasCommand.ForRestore());
    }

    /// <summary>Restores the save stack to a previous level as returned by <see cref="GetSaveCount"/>.</summary>
    public void RestoreToCount(int count)
    {
        while (_saveCount > count && _saveCount > 1)
        {
            Restore();
        }
    }

    /// <summary>Adds a translation to the current transform.</summary>
    public void Translate(double dx, double dy)
    {
        if (dx == 0.0 && dy == 0.0)
        {
            return;
        }

        _commands.Add(CanvasCommand.ForPush(context =>
            context.PushTransform(Matrix.CreateTranslation(dx, dy))));
    }

    /// <summary>Adds an axis-aligned scale to the current transform.</summary>
    public void Scale(double sx, double? sy = null)
    {
        double scaleY = sy ?? sx;
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(Matrix.CreateScale(sx, scaleY))));
    }

    /// <summary>Adds a rotation, in radians, to the current transform.</summary>
    public void Rotate(double radians)
    {
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(Matrix.CreateRotation(radians))));
    }

    /// <summary>Multiplies the current transform by the given matrix.</summary>
    /// <remarks>Dart's <c>Canvas.transform</c>, which takes the matrix's column-major storage.</remarks>
    public void Transform(Matrix4 matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        Matrix avaloniaMatrix = matrix.ToAvaloniaMatrix();
        _commands.Add(CanvasCommand.ForPush(context => context.PushTransform(avaloniaMatrix)));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given rectangle.</summary>
    public void ClipRect(Rect rect, bool doAntiAlias = true)
    {
        PushEdgeMode(doAntiAlias);
        _commands.Add(CanvasCommand.ForPush(context => context.PushClip(rect)));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given rounded rect.</summary>
    public void ClipRRect(RRect rrect, bool doAntiAlias = true)
    {
        PushEdgeMode(doAntiAlias);
        _commands.Add(CanvasCommand.ForPush(context => Layer.PushRoundedRectClip(context, rrect)));
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given shape.</summary>
    public void ClipRSuperellipse(RSuperellipse rsuperellipse, bool doAntiAlias = true)
    {
        ClipPath(rsuperellipse.ToPath(), doAntiAlias);
    }

    /// <summary>Reduces the clip region to the intersection of the current clip and the given path.</summary>
    public void ClipPath(Path path, bool doAntiAlias = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        PushEdgeMode(doAntiAlias);

        // The backend geometry is built on playback: recording must not need a render backend.
        Geometry? geometry = null;
        _commands.Add(CanvasCommand.ForPush(context => context.PushGeometryClip(geometry ??= path.ToGeometry())));
    }

    /// <summary>Plumix-only: clips to an Avalonia geometry the caller already built.</summary>
    /// <remarks>
    /// Dart clips through <c>Path</c> only; Plumix models a few shapes (notched app bars, decoration
    /// outlines) as backend geometry, which cannot be shifted, so the offset is applied around it.
    /// </remarks>
    public void ClipGeometry(Geometry geometry, bool doAntiAlias = true, Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        PushEdgeMode(doAntiAlias);
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

    /// <summary>Plumix-only: records a draw call that renders itself onto the backend context.</summary>
    internal void AddDrawCommand(Action<DrawingContext> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _commands.Add(CanvasCommand.ForDraw(draw));
    }

    private void PushEdgeMode(bool doAntiAlias)
    {
        _commands.Add(CanvasCommand.ForPush(context => context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = doAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
        })));
    }
}
