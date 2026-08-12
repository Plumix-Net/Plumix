using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

/// Hosts one `InputDecorator` under loose vertical constraints so the decorator's own measured
/// size is observable, mirroring how Flutter's input_decorator_test.dart wraps the widget.
internal sealed class DecoratorHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly RootElement _root;
    private readonly PipelineOwner _pipeline;
    private readonly Size _viewSize;

    public DecoratorHarness(
        Widget decorator,
        ThemeData? theme = null,
        TextDirection textDirection = TextDirection.Ltr,
        double width = 800.0,
        double height = 600.0)
    {
        _viewSize = new Size(width, height);
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        Widget wrapped = new Directionality(
            textDirection,
            new MediaQuery(
                new MediaQueryData(Size: _viewSize),
                new Theme(
                    theme ?? ThemeData.Light,
                    new Align(alignment: Alignment.TopLeft, child: decorator))));
        _root = new RootElement(RenderView, wrapped);
        _root.Attach(_owner);
        _root.Mount(null, null);
        _owner.FlushBuild();
    }

    public RenderView RenderView { get; }

    public RenderDecoration Decorator => Find<RenderDecoration>(RenderView)
                                         ?? throw new InvalidOperationException("No RenderDecoration mounted.");

    public void Pump()
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(_viewSize);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    public SemanticsNode PumpSemantics()
    {
        Pump();
        _pipeline.RequestSemanticsUpdate();
        _pipeline.FlushSemantics();
        return _pipeline.SemanticsOwner.RootNode
               ?? throw new InvalidOperationException("The semantics tree is empty.");
    }

    public void Update(Widget decorator, ThemeData? theme = null, TextDirection textDirection = TextDirection.Ltr)
    {
        _root.Update(new Directionality(
            textDirection,
            new MediaQuery(
                new MediaQueryData(Size: _viewSize),
                new Theme(
                    theme ?? ThemeData.Light,
                    new Align(alignment: Alignment.TopLeft, child: decorator)))));
        Pump();
    }

    public void Dispose() => _root.Unmount();

    public static Point OffsetOf(RenderBox? box) =>
        box is null ? default : ((BoxParentData)box.parentData!).offset;

    public static Rect RectOf(RenderBox? box) =>
        box is null ? default : new Rect(OffsetOf(box), box.Size);

    public static T? Find<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T typed)
        {
            return typed;
        }

        T? found = null;
        root.VisitChildren(child => found ??= Find<T>(child));
        return found;
    }

    public static List<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T typed)
        {
            result.Add(typed);
        }

        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _view;
        private Element? _child;

        public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;

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
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_view.Child, child))
            {
                _view.Child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
