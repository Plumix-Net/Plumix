using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Shared harness for the mouse-tracking tests (C#-only test infrastructure; Flutter's own tests use
// `WidgetTester` plus `mouse_tracker_test_utils.dart`).

namespace Plumix.Tests;

/// <summary>
/// Mounts a widget under a <see cref="RenderView"/> and drives pointer events through the real
/// <see cref="GestureBinding"/>, so <see cref="MouseTracker"/> runs exactly as it does on a host.
/// </summary>
internal sealed class MouseTrackingHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly PipelineOwner _pipeline;
    private readonly HarnessRootElement _rootElement;
    private Size _size;

    public MouseTrackingHarness(Widget widget, Size? size = null, int viewId = 0)
    {
        _size = size ?? new Size(200, 200);
        RenderView = new RenderView(new FlutterView(new Size(800, 600), viewId: viewId));
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        RendererBinding.Instance.AddRenderView(RenderView);
        _rootElement = new HarnessRootElement(RenderView, new Directionality(TextDirection.Ltr, child: widget));
        _rootElement.Attach(_owner);
        _owner.BuildScope(_rootElement, () => _rootElement.Mount(parent: null, newSlot: null));
        _owner.FlushBuild();
        Layout();
    }

    public RenderView RenderView { get; }

    public MouseTracker MouseTracker => GestureBinding.Instance.MouseTracker;

    /// <summary>Flushes build, layout and paint, without the post-frame device update.</summary>
    public void Layout(Size? size = null)
    {
        _size = size ?? _size;
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(_size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    /// <summary>
    /// Produces a frame the way a host does: draw, then the single post-frame
    /// <see cref="MouseTracker.UpdateAllDevices"/>.
    /// </summary>
    public void PumpFrame(Size? size = null)
    {
        Layout(size);
        MouseTracker.UpdateAllDevices();
    }

    /// <summary>Rebuilds with <paramref name="widget"/> and produces a frame.</summary>
    public void Update(Widget widget)
    {
        _rootElement.Update(new Directionality(TextDirection.Ltr, child: widget));
        _owner.FlushBuild();
        PumpFrame();
    }

    /// <summary>Sends one pointer event through the binding and flushes the rebuilds it caused.</summary>
    public void SendPointer(PointerEvent @event)
    {
        GestureBinding.Instance.HandlePointerEvent(RenderView, @event);
        _owner.FlushBuild();
    }

    public void Dispose()
    {
        try
        {
            _rootElement.UnmountRoot();
        }
        finally
        {
            RendererBinding.Instance.RemoveRenderView(RenderView);
        }
    }

    public static PointerAddedEvent Added(
        Point position,
        int device = 1,
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        int viewId = 0)
        => new(device, kind, position, timestampUtc: DateTime.UtcNow) { ViewId = viewId };

    public static PointerRemovedEvent Removed(
        Point position,
        int device = 1,
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        int viewId = 0)
        => new(device, kind, position, timestampUtc: DateTime.UtcNow) { ViewId = viewId };

    public static PointerHoverEvent Hover(
        Point position,
        int device = 1,
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        int viewId = 0)
        => new(device, kind, position, PointerButtons.None, DateTime.UtcNow) { ViewId = viewId };

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
        {
            _renderView = renderView;
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }


        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            _renderView.Child = (RenderBox)child;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
            }
        }
    }
}
