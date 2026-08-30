using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;

// Test infrastructure (C#-only): a root element that owns a RenderView so focus nodes laid out by the
// harness report the real geometry Dart's directional traversal reads from `FocusNode.rect`.

namespace Plumix.Tests;

internal sealed class FocusLayoutHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly PipelineOwner _pipeline;
    private readonly HarnessRootElement _rootElement;

    public FocusLayoutHarness(Widget widget)
    {
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        _rootElement = new HarnessRootElement(RenderView, widget);
        _rootElement.Attach(_owner);
        _rootElement.Mount(parent: null, newSlot: null);
        _owner.FlushBuild();
        Scheduler.FlushMicrotasks();
    }

    /// <summary>
    /// Mounts the tree under the traversal scope <c>WidgetsApp</c> installs, the way Flutter's own
    /// focus tests reach it through <c>MaterialApp</c>/<c>WidgetsApp</c>. Traversal throws without a
    /// <see cref="FocusTraversalGroup"/> in scope, so every test that moves the focus goes here.
    /// </summary>
    public static FocusLayoutHarness WithTraversalGroup(Widget widget) =>
        new(AppTraversalScope.Wrap(widget));

    public RenderView RenderView { get; }

    public void Layout(Size size)
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(size);
        Scheduler.FlushMicrotasks();
    }

    public void Update(Widget widget, Size size)
    {
        _rootElement.Update(widget);
        Layout(size);
    }

    public void Dispose()
    {
        _rootElement.Unmount();
        Scheduler.FlushMicrotasks();
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
        {
            _renderView = renderView;
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        internal override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) =>
            _renderView.Child = (RenderBox)child;

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
