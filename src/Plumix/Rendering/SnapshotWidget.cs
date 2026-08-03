using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/snapshot_widget.dart

public sealed class RenderSnapshotWidget : RenderProxyBox
{
    private SnapshotController _controller;
    private SnapshotMode _mode;
    private bool _autoresize;
    private double _pixelRatio;
    private Size _lastPaintedSize;

    public RenderSnapshotWidget(
        SnapshotController controller,
        SnapshotMode mode,
        bool autoresize,
        double pixelRatio,
        RenderBox? child = null)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _mode = mode;
        _autoresize = autoresize;
        _pixelRatio = pixelRatio;
        Child = child;
    }

    public SnapshotController Controller
    {
        get => _controller;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_controller, value))
            {
                return;
            }

            if (Attached)
            {
                _controller.RemoveListener(HandleControllerChanged);
            }

            _controller = value;
            if (Attached)
            {
                _controller.AddListener(HandleControllerChanged);
            }

            MarkNeedsCompositedLayerUpdate();
        }
    }

    public SnapshotMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
            {
                return;
            }

            _mode = value;
            MarkNeedsCompositedLayerUpdate();
        }
    }

    public bool Autoresize
    {
        get => _autoresize;
        set
        {
            if (_autoresize == value)
            {
                return;
            }

            _autoresize = value;
            MarkNeedsCompositedLayerUpdate();
        }
    }

    public double PixelRatio
    {
        get => _pixelRatio;
        set
        {
            if (Math.Abs(_pixelRatio - value) <= 0.000001)
            {
                return;
            }

            _pixelRatio = value;
            MarkNeedsCompositedLayerUpdate();
        }
    }

    public override bool IsRepaintBoundary => Child != null;

    protected override bool AlwaysNeedsCompositing => Child != null;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_layer is SnapshotOffsetLayer layer)
        {
            layer.ClearSnapshot();
        }

        _lastPaintedSize = Size;
        base.Paint(ctx, offset);
    }

    protected override void PerformLayout()
    {
        Size oldSize = HasSize ? Size : default;
        base.PerformLayout();
        if (_autoresize && oldSize != Size && _lastPaintedSize != Size)
        {
            MarkNeedsCompositedLayerUpdate();
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as SnapshotOffsetLayer ?? new SnapshotOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        var snapshotLayer = (SnapshotOffsetLayer)layer;
        snapshotLayer.AllowSnapshotting = Controller.AllowSnapshotting;
        snapshotLayer.ClearVersion = Controller.ClearVersion;
        snapshotLayer.Mode = Mode;
        snapshotLayer.Size = Size;
        snapshotLayer.PixelRatio = PixelRatio;
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _controller.AddListener(HandleControllerChanged);
    }

    protected override void OnDetach()
    {
        _controller.RemoveListener(HandleControllerChanged);
        base.OnDetach();
    }

    private void HandleControllerChanged()
    {
        MarkNeedsCompositedLayerUpdate();
    }
}

public sealed class SnapshotOffsetLayer : OffsetLayer
{
    private RenderTargetBitmap? _snapshot;
    private bool _allowSnapshotting;
    private int _clearVersion;
    private Size _size;
    private double _pixelRatio = 1.0;

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
            ClearSnapshot();
        }
    }

    public int ClearVersion
    {
        get => _clearVersion;
        set
        {
            if (_clearVersion == value)
            {
                return;
            }

            _clearVersion = value;
            ClearSnapshot();
        }
    }

    public SnapshotMode Mode { get; set; }

    public Size Size
    {
        get => _size;
        set
        {
            if (_size == value)
            {
                return;
            }

            _size = value;
            ClearSnapshot();
        }
    }

    public double PixelRatio
    {
        get => _pixelRatio;
        set
        {
            if (Math.Abs(_pixelRatio - value) <= 0.000001)
            {
                return;
            }

            _pixelRatio = value;
            ClearSnapshot();
        }
    }

    public void ClearSnapshot()
    {
        _snapshot?.Dispose();
        _snapshot = null;
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        Point sceneOffset = offset + Offset;
        if (!AllowSnapshotting || Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            AddChildrenToScene(context, sceneOffset);
            return;
        }

        try
        {
            _snapshot ??= RasterizeChildren();
        }
        catch when (Mode == SnapshotMode.Permissive)
        {
            ClearSnapshot();
            AddChildrenToScene(context, sceneOffset);
            return;
        }

        var source = new Rect(0.0, 0.0, _snapshot.PixelSize.Width, _snapshot.PixelSize.Height);
        var destination = new Rect(sceneOffset, Size);
        using (context.PushRenderOptions(new RenderOptions
               {
                   BitmapInterpolationMode = BitmapInterpolationMode.MediumQuality,
               }))
        {
            context.DrawImage(_snapshot, source, destination);
        }
    }

    internal override void Detach()
    {
        ClearSnapshot();
        base.Detach();
    }

    private RenderTargetBitmap RasterizeChildren()
    {
        int width = Math.Max(1, (int)Math.Ceiling(Size.Width * PixelRatio));
        int height = Math.Max(1, (int)Math.Ceiling(Size.Height * PixelRatio));
        var bitmap = new RenderTargetBitmap(
            new PixelSize(width, height),
            new Vector(96.0 * PixelRatio, 96.0 * PixelRatio));
        try
        {
            using DrawingContext drawingContext = bitmap.CreateDrawingContext();
            AddChildrenToScene(drawingContext, new Point(0.0, 0.0));
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
