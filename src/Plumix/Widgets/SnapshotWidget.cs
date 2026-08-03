using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/snapshot_widget.dart

public enum SnapshotMode
{
    Normal,
    Permissive,
    Forced,
}

public sealed class SnapshotController : ChangeNotifier
{
    private bool _allowSnapshotting;

    public bool AllowSnapshotting
    {
        get => _allowSnapshotting;
        set
        {
            if (_allowSnapshotting == value)
            {
                return;
            }

            _allowSnapshotting = value;
            NotifyListeners();
        }
    }

    public void Clear()
    {
        ClearVersion += 1;
        NotifyListeners();
    }

    internal int ClearVersion { get; private set; }
}

public sealed class SnapshotWidget : SingleChildRenderObjectWidget
{
    public SnapshotWidget(
        SnapshotController controller,
        Widget? child = null,
        SnapshotMode mode = SnapshotMode.Normal,
        bool autoresize = false,
        double pixelRatio = 1.0,
        Key? key = null) : base(child, key)
    {
        if (!double.IsFinite(pixelRatio) || pixelRatio <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelRatio));
        }

        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Mode = mode;
        Autoresize = autoresize;
        PixelRatio = pixelRatio;
    }

    public SnapshotController Controller { get; }

    public SnapshotMode Mode { get; }

    public bool Autoresize { get; }

    public double PixelRatio { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSnapshotWidget(Controller, Mode, Autoresize, PixelRatio);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var snapshot = (RenderSnapshotWidget)renderObject;
        snapshot.Controller = Controller;
        snapshot.Mode = Mode;
        snapshot.Autoresize = Autoresize;
        snapshot.PixelRatio = PixelRatio;
    }
}
