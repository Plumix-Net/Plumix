using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/snapshot_widget.dart

/// <summary>
/// Dart's <c>_RenderSnapshotWidget</c>: a render object that conditionally converts its child into an
/// image and then paints it in place of the child.
/// </summary>
public sealed class RenderSnapshotWidget : RenderProxyBox
{
    private double _devicePixelRatio;
    private SnapshotPainter _painter;
    private SnapshotController _controller;
    private SnapshotMode _mode;
    private bool _autoresize;
    private IImage? _childRaster;
    private Size? _childRasterSize;

    // Set to true if the snapshot mode was not forced and a platform view
    // was encountered while attempting to snapshot the child.
    private bool _disableSnapshotAttempt;
    private Size? _lastCachedSize;

    public RenderSnapshotWidget(
        double devicePixelRatio,
        SnapshotController controller,
        SnapshotMode mode,
        SnapshotPainter painter,
        bool autoresize,
        RenderBox? child = null)
    {
        _devicePixelRatio = devicePixelRatio;
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _mode = mode;
        _painter = painter ?? throw new ArgumentNullException(nameof(painter));
        _autoresize = autoresize;
        Child = child;
    }

    /// <summary>The device pixel ratio used to create the child image.</summary>
    public double DevicePixelRatio
    {
        get => _devicePixelRatio;
        set
        {
            if (value == DevicePixelRatio)
            {
                return;
            }

            _devicePixelRatio = value;
            if (_childRaster == null)
            {
                return;
            }

            DisposeChildRaster();
            MarkNeedsPaint();
        }
    }

    /// <summary>The painter used to paint the child snapshot or child widgets.</summary>
    public SnapshotPainter Painter
    {
        get => _painter;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, Painter))
            {
                return;
            }

            SnapshotPainter oldPainter = Painter;
            oldPainter.RemoveListener(MarkNeedsPaint);
            _painter = value;
            if (oldPainter.GetType() != Painter.GetType() || Painter.ShouldRepaint(oldPainter))
            {
                MarkNeedsPaint();
            }

            if (Attached)
            {
                Painter.AddListener(MarkNeedsPaint);
            }
        }
    }

    /// <summary>A controller that determines whether to paint the child normally or to paint a snapshotted
    /// version of that child.</summary>
    public SnapshotController Controller
    {
        get => _controller;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, Controller))
            {
                return;
            }

            Controller.RemoveListener(OnRasterValueChanged);
            bool oldValue = Controller.AllowSnapshotting;
            _controller = value;
            if (Attached)
            {
                Controller.AddListener(OnRasterValueChanged);
                if (oldValue != Controller.AllowSnapshotting)
                {
                    OnRasterValueChanged();
                }
            }
        }
    }

    /// <summary>How the snapshot widget will handle platform views in child layers.</summary>
    public SnapshotMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode)
            {
                return;
            }

            _mode = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Whether or not changes in render object size should automatically re-rasterize.</summary>
    public bool Autoresize
    {
        get => _autoresize;
        set
        {
            if (value == Autoresize)
            {
                return;
            }

            _autoresize = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>The current snapshot of the child, if one is held.</summary>
    internal IImage? DebugChildRaster => _childRaster;

    protected override void OnAttach()
    {
        Controller.AddListener(OnRasterValueChanged);
        Painter.AddListener(MarkNeedsPaint);
        base.OnAttach();
    }

    protected override void OnDetach()
    {
        _disableSnapshotAttempt = false;
        Controller.RemoveListener(OnRasterValueChanged);
        Painter.RemoveListener(MarkNeedsPaint);
        DisposeChildRaster();
        _childRasterSize = null;
        base.OnDetach();
    }

    public override void Dispose()
    {
        Controller.RemoveListener(OnRasterValueChanged);
        Painter.RemoveListener(MarkNeedsPaint);
        DisposeChildRaster();
        _childRasterSize = null;
        base.Dispose();
    }

    private void OnRasterValueChanged()
    {
        _disableSnapshotAttempt = false;
        DisposeChildRaster();
        _childRasterSize = null;
        MarkNeedsPaint();
    }

    // Paint [child] with this painting context, then convert to a raster and detach all
    // children from this layer.
    private IImage? PaintAndDetachToImage()
    {
        var offsetLayer = new OffsetLayer();
        var context = new PaintingContext(offsetLayer, new Rect(Size));
        base.Paint(context, default);
        // Dart calls the protected `stopRecordingIfNeeded` here (with an ignore comment).
        context.DebugStopRecordingIfNeeded();
        if (Mode != SnapshotMode.Forced && !offsetLayer.SupportsRasterization())
        {
            offsetLayer.Dispose();
            if (Mode == SnapshotMode.Normal)
            {
                throw new FlutterError("SnapshotWidget used with a child that contains a PlatformView.");
            }

            _disableSnapshotAttempt = true;
            return null;
        }

        IImage image = offsetLayer.ToImageSync(new Rect(Size), pixelRatio: DevicePixelRatio);
        offsetLayer.Dispose();
        _lastCachedSize = Size;
        return image;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            DisposeChildRaster();
            _childRasterSize = null;
            return;
        }

        if (!Controller.AllowSnapshotting || _disableSnapshotAttempt)
        {
            DisposeChildRaster();
            _childRasterSize = null;
            Painter.Paint(context, offset, Size, base.Paint);
            return;
        }

        if (Autoresize && Size != _lastCachedSize && _lastCachedSize != null)
        {
            DisposeChildRaster();
        }

        if (_childRaster == null)
        {
            _childRaster = PaintAndDetachToImage();
            _childRasterSize = Size * DevicePixelRatio;
        }

        if (_childRaster == null)
        {
            Painter.Paint(context, offset, Size, base.Paint);
        }
        else
        {
            Painter.PaintSnapshot(
                context,
                offset,
                Size,
                _childRaster,
                _childRasterSize!.Value,
                DevicePixelRatio);
        }
    }

    // Dart's `_childRaster?.dispose(); _childRaster = null;`. The engine keeps an image alive while a
    // retained picture still references it; an Avalonia bitmap has no such reference count, and the host
    // may redraw the last scene (which can still draw this raster) before the next frame replaces it, so
    // the bitmap is released once the next frame has been rendered.
    private void DisposeChildRaster()
    {
        IImage? raster = _childRaster;
        _childRaster = null;
        if (raster is IDisposable disposable)
        {
            SchedulerBinding.Instance.AddPostFrameCallback(_ => disposable.Dispose());
        }
    }
}
