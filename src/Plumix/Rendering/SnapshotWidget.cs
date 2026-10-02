using Avalonia;
using Avalonia.Media;
using Plumix.UI;
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

    public override bool AlwaysNeedsCompositing => Child != null;

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

/// <summary>
/// Plumix-only: the composited layer of <see cref="RenderSnapshotWidget"/>, whose children the
/// rasterizer draws once into a cached snapshot image and from the image afterwards.
/// </summary>
public sealed class SnapshotOffsetLayer : OffsetLayer
{
    private readonly SnapshotRasterCache _cache = new();
    private bool _allowSnapshotting;
    private int _clearVersion;
    private SnapshotMode _mode;

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
            MarkNeedsAddToScene();
        }
    }

    public Size Size
    {
        get => _cache.Size;
        set
        {
            if (_cache.Size == value)
            {
                return;
            }

            _cache.Size = value;
            ClearSnapshot();
        }
    }

    public double PixelRatio
    {
        get => _cache.PixelRatio;
        set
        {
            if (Math.Abs(_cache.PixelRatio - value) <= 0.000001)
            {
                return;
            }

            _cache.PixelRatio = value;
            ClearSnapshot();
        }
    }

    /// <summary>Whether the rasterizer holds a snapshot of the children.</summary>
    internal bool HasSnapshot => _cache.Image != null;

    public void ClearSnapshot()
    {
        _cache.Clear();
        if (!DebugDisposed)
        {
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        if (!AllowSnapshotting || Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            base.AddToScene(builder);
            return;
        }

        _cache.Permissive = Mode == SnapshotMode.Permissive;
        EngineLayer = builder.PushSnapshot(_cache, Offset, EngineLayer as SnapshotEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    public override void Detach()
    {
        _cache.Clear();
        base.Detach();
    }

    protected internal override void Dispose()
    {
        _cache.Clear();
        base.Dispose();
    }
}
