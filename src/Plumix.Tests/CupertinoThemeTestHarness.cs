using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>
/// A minimal pump-and-repump host for the Cupertino theme/colors tests: it keeps one root element
/// alive across pumps so reusing a child widget instance exercises inherited-dependency updates the
/// same way Flutter's `const` subtrees do.
/// </summary>
internal sealed class CupertinoThemeTestHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly PipelineOwner _pipeline;
    private readonly RootElement _root;

    public CupertinoThemeTestHarness(Widget widget)
    {
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        _root = new RootElement(RenderView, widget);
        _root.Attach(_owner);
        _root.Mount(parent: null, newSlot: null);
        _owner.FlushBuild();
    }

    public RenderView RenderView { get; }

    public void PumpWidget(Widget widget)
    {
        _root.Update(widget);
        _owner.FlushBuild();
    }

    public void Layout(Size size)
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(size);
    }

    public void Pump(Size size)
    {
        Layout(size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    public void Dispose() => _root.Unmount();

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public RootElement(RenderView renderView, Widget widget) : base(widget)
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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            _renderView.Child = child as RenderBox;
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
