using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class ContainerTests
{
    [Fact]
    public void Container_WithMargin_WrapsRootInRenderPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                margin: new Thickness(4),
                width: 30,
                height: 20,
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var outer = RequireRenderObject<RenderPadding>(root.ChildElement);
        Assert.Equal(new Thickness(4), outer.Padding);
        Assert.IsType<RenderConstrainedBox>(outer.Child);
    }

    [Fact]
    public void Container_WithAlignment_UsesRenderAlign()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                alignment: Alignment.BottomRight,
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Equal(Alignment.BottomRight, align.Alignment);
    }

    [Fact]
    public void Container_WithAlignmentAndPadding_WrapOrder_IsPaddingOutsideAlign()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                alignment: Alignment.TopLeft,
                padding: new Thickness(6, 4, 2, 1),
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = RequireRenderObject<RenderPadding>(root.ChildElement);
        Assert.Equal(new Thickness(6, 4, 2, 1), padding.Padding);
        var align = Assert.IsType<RenderAlign>(padding.Child);
        Assert.Equal(Alignment.TopLeft, align.Alignment);
    }

    [Fact]
    public void Container_WithConstraints_UsesRenderConstrainedBox()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                constraints: new BoxConstraints(MinWidth: 20, MaxWidth: 40, MinHeight: 12, MaxHeight: 22),
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = RequireRenderObject<RenderConstrainedBox>(root.ChildElement);
        Assert.Equal(new BoxConstraints(MinWidth: 20, MaxWidth: 40, MinHeight: 12, MaxHeight: 22), constrained.AdditionalConstraints);
    }

    [Fact]
    public void Container_WithWidthHeightAndConstraints_TightensToClampedConstraints()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                width: 150,
                height: 90,
                constraints: new BoxConstraints(MinWidth: 80, MaxWidth: 120, MinHeight: 50, MaxHeight: 76),
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = RequireRenderObject<RenderConstrainedBox>(root.ChildElement);
        Assert.Equal(
            new BoxConstraints(MinWidth: 120, MaxWidth: 120, MinHeight: 76, MaxHeight: 76),
            constrained.AdditionalConstraints);
    }

    [Fact]
    public void Container_WithTransformAndMargin_WrapOrder_IsTransformOutsideMargin()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Container(
                margin: new Thickness(5),
                transform: Matrix.CreateTranslation(12, 0),
                child: new SizedBox(width: 8, height: 6)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var transform = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.Equal(Matrix.CreateTranslation(12, 0), transform.Transform);
        var marginPadding = Assert.IsType<RenderPadding>(transform.Child);
        Assert.Equal(new Thickness(5), marginPadding.Padding);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
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
