using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class DecoratedBoxTests
{
    [Fact]
    public void RenderDecoratedBox_LayoutsLikeProxyBox()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var decorated = new RenderDecoratedBox(
            decoration: new BoxDecoration(
                Color: Colors.CornflowerBlue,
                Border: new BorderSide(Colors.MidnightBlue, 2),
                BorderRadius: BorderRadius.Circular(8)),
            child: child);
        var root = new RenderView
        {
            Child = decorated
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(20, 10), decorated.Size);
    }

    [Fact]
    public void DecoratedBoxWidget_CreatesRenderDecoratedBox_AndUpdatesDecoration()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: Colors.CadetBlue,
                    Border: new BorderSide(Colors.Black, 1),
                    BorderRadius: BorderRadius.Circular(6)),
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderDecorated = RequireRenderObject<RenderDecoratedBox>(root.ChildElement);
        Assert.Equal(Colors.CadetBlue, renderDecorated.Decoration.Color);
        Assert.Equal(new BorderSide(Colors.Black, 1), renderDecorated.Decoration.Border);
        Assert.Equal(BorderRadius.Circular(6), renderDecorated.Decoration.EffectiveBorderRadius);

        root.Update(new DecoratedBox(
            decoration: new BoxDecoration(
                Color: Colors.OrangeRed,
                Border: new BorderSide(Colors.White, 3),
                BorderRadius: BorderRadius.Circular(12)),
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderDecoratedBox>(root.ChildElement);
        Assert.True(ReferenceEquals(renderDecorated, updated));
        Assert.Equal(Colors.OrangeRed, updated.Decoration.Color);
        Assert.Equal(new BorderSide(Colors.White, 3), updated.Decoration.Border);
        Assert.Equal(BorderRadius.Circular(12), updated.Decoration.EffectiveBorderRadius);
    }

    [Fact]
    public void Container_DecorationTakesPrecedenceOverColor()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                color: Colors.Green,
                decoration: new BoxDecoration(Color: Colors.Red),
                child: new SizedBox(width: 8, height: 8)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = RequireRenderObject<RenderDecoratedBox>(root.ChildElement);
        Assert.Equal(Colors.Red, decorated.Decoration.Color);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _size;

        public FixedSizeRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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
            if (_child != null)
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

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
