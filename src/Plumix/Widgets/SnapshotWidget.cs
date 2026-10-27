using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/snapshot_widget.dart

/// <summary>Controls how the <see cref="SnapshotWidget"/> paints its child.</summary>
public enum SnapshotMode
{
    /// <summary>
    /// The child is snapshotted, but only if all descendants can be snapshotted; a platform view
    /// disables snapshotting and the child paints directly.
    /// </summary>
    Permissive,

    /// <summary>An error is thrown if the child contains a platform view.</summary>
    Normal,

    /// <summary>Platform views and other non-snapshottable children are ignored.</summary>
    Forced,
}

/// <summary>A controller for the <see cref="SnapshotWidget"/> that controls when the child image is displayed
/// and when to regenerated the child image.</summary>
public class SnapshotController : ChangeNotifier
{
    private bool _allowSnapshotting;

    /// <summary>Create a new <see cref="SnapshotController"/>.</summary>
    public SnapshotController(bool allowSnapshotting = false)
    {
        _allowSnapshotting = allowSnapshotting;
    }

    /// <summary>Reset the snapshot held by any listening <see cref="SnapshotWidget"/>.</summary>
    public void Clear()
    {
        NotifyListeners();
    }

    /// <summary>Whether a snapshot of this child widget is painted in its place.</summary>
    public bool AllowSnapshotting
    {
        get => _allowSnapshotting;
        set
        {
            if (value == AllowSnapshotting)
            {
                return;
            }

            _allowSnapshotting = value;
            NotifyListeners();
        }
    }
}

/// <summary>
/// A widget that can replace its child with a snapshotted version of the child.
/// </summary>
public sealed class SnapshotWidget : SingleChildRenderObjectWidget
{
    public SnapshotWidget(
        SnapshotController controller,
        Widget? child,
        SnapshotMode mode = SnapshotMode.Normal,
        SnapshotPainter? painter = null,
        bool autoresize = false,
        Key? key = null) : base(child, key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Mode = mode;
        Painter = painter ?? DefaultSnapshotPainter.Instance;
        Autoresize = autoresize;
    }

    /// <summary>The controller that determines when to display the children as a snapshot.</summary>
    public SnapshotController Controller { get; }

    /// <summary>Configuration that controls how the snapshot widget decides to paint its children.</summary>
    public SnapshotMode Mode { get; }

    /// <summary>Whether or not changes in render object size should automatically re-create the snapshot.</summary>
    public bool Autoresize { get; }

    /// <summary>The painter used to paint the child snapshot or child widgets.</summary>
    public SnapshotPainter Painter { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        WidgetsDebug.DebugCheckHasMediaQuery(context);
        return new RenderSnapshotWidget(
            controller: Controller,
            mode: Mode,
            devicePixelRatio: MediaQuery.DevicePixelRatioOf(context),
            painter: Painter,
            autoresize: Autoresize);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        WidgetsDebug.DebugCheckHasMediaQuery(context);
        var snapshot = (RenderSnapshotWidget)renderObject;
        snapshot.Controller = Controller;
        snapshot.Mode = Mode;
        snapshot.DevicePixelRatio = MediaQuery.DevicePixelRatioOf(context);
        snapshot.Painter = Painter;
        snapshot.Autoresize = Autoresize;
    }
}

/// <summary>A painter used to paint either a snapshot or the child widgets that would be a snapshot.</summary>
/// <remarks>
/// The painter can call <see cref="ChangeNotifier.NotifyListeners"/> to have the <see cref="SnapshotWidget"/>
/// re-paint (re-using the same raster). This allows animations to be performed without re-snapshotting of
/// children.
/// </remarks>
public abstract class SnapshotPainter : ChangeNotifier
{
    /// <summary>Creates an instance of <see cref="SnapshotPainter"/>.</summary>
    protected SnapshotPainter()
    {
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            MaybeDispatchObjectCreation(this);
        }
    }

    // Dart's `_DefaultSnapshotPainter implements SnapshotPainter` never runs the SnapshotPainter
    // constructor, so it never reports its creation.
    private protected SnapshotPainter(bool dispatchCreation)
    {
        _ = dispatchCreation;
    }

    /// <summary>Called whenever the <paramref name="image"/> that represents a <see cref="SnapshotWidget"/>s child
    /// should be painted.</summary>
    /// <param name="context">The painting context.</param>
    /// <param name="offset">The offset the child would be painted at.</param>
    /// <param name="size">The logical size of the child.</param>
    /// <param name="image">The raster of the child.</param>
    /// <param name="sourceSize">The physical size of <paramref name="image"/>.</param>
    /// <param name="pixelRatio">The device pixel ratio the raster was taken at.</param>
    public abstract void PaintSnapshot(
        PaintingContext context,
        Point offset,
        Size size,
        IImage image,
        Size sourceSize,
        double pixelRatio);

    /// <summary>Paint the child via <paramref name="painter"/>, applying any effects that would have been
    /// painted in <see cref="PaintSnapshot"/>.</summary>
    public abstract void Paint(PaintingContext context, Point offset, Size size, PaintingContextCallback painter);

    /// <summary>Called whenever a new instance of the snapshot widget delegate class is provided to the
    /// <see cref="RenderSnapshotWidget"/> object, or any time that a new <see cref="SnapshotPainter"/> object
    /// is created with a new instance of the delegate class.</summary>
    public abstract bool ShouldRepaint(SnapshotPainter oldPainter);
}

/// <summary>Dart's <c>_DefaultSnapshotPainter</c>: the const, listener-less default painter.</summary>
internal sealed class DefaultSnapshotPainter : SnapshotPainter
{
    private DefaultSnapshotPainter() : base(dispatchCreation: false)
    {
    }

    /// <summary>Dart's <c>const _DefaultSnapshotPainter()</c>: one canonical instance.</summary>
    public static DefaultSnapshotPainter Instance { get; } = new();

    public override void AddListener(Action listener)
    {
    }

    public override void Dispose()
    {
    }

    public override void NotifyListeners()
    {
    }

    public override void Paint(PaintingContext context, Point offset, Size size, PaintingContextCallback painter)
    {
        painter(context, offset);
    }

    public override void PaintSnapshot(
        PaintingContext context,
        Point offset,
        Size size,
        IImage image,
        Size sourceSize,
        double pixelRatio)
    {
        var src = new Rect(0, 0, sourceSize.Width, sourceSize.Height);
        var dst = new Rect(offset.X, offset.Y, size.Width, size.Height);
        var paint = new Paint { FilterQuality = FilterQuality.Medium };
        context.Canvas.DrawImageRect(image, src, dst, paint);
    }

    public override void RemoveListener(Action listener)
    {
    }

    public override bool ShouldRepaint(SnapshotPainter oldPainter) => false;
}
