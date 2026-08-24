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

    public SemanticsNode? PumpAndGetSemantics(Size size)
    {
        Pump(size);
        _pipeline.RequestSemanticsUpdate();
        _pipeline.FlushSemantics();
        return _pipeline.SemanticsOwner.RootNode;
    }

    public IReadOnlyList<T> FindWidgets<T>() where T : Widget
    {
        var result = new List<T>();
        Visit(_root, result);
        return result;
    }

    public T FindState<T>() where T : State
    {
        T? result = null;
        VisitStates(_root, state => result ??= state as T);
        return result ?? throw new InvalidOperationException($"State {typeof(T).Name} was not found.");
    }

    public void Dispose() => _root.Unmount();

    private static void Visit<T>(Element element, List<T> result) where T : Widget
    {
        if (element.Widget is T widget)
        {
            result.Add(widget);
        }

        element.VisitChildren(child => Visit(child, result));
    }

    private static void VisitStates(Element element, Action<State> visitor)
    {
        if (element is StatefulElement statefulElement)
        {
            visitor(statefulElement.State);
        }

        element.VisitChildren(child => VisitStates(child, visitor));
    }

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
