using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class AlignTests
{
    [Fact]
    public void RenderAlign_CentersChildWithinAvailableSpace()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var align = new RenderAlign(
            alignment: Alignment.Center,
            child: child);
        var root = new RenderView
        {
            Child = align
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(100, 80), align.Size);
        Assert.Equal(new Point(40, 35), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderAlign_AppliesTopRightAlignment()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var align = new RenderAlign(
            alignment: Alignment.TopRight,
            child: child);
        var root = new RenderView
        {
            Child = align
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Point(80, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderAlign_WidthHeightFactors_ShrinkWrapToChild()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var align = new RenderAlign(
            alignment: Alignment.Center,
            widthFactor: 2,
            heightFactor: 3,
            child: child);
        var root = new RenderView
        {
            Child = align
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(40, 30), align.Size);
        Assert.Equal(new Point(10, 10), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void AlignWidget_CreatesRenderAlign_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Align(
                alignment: Alignment.TopLeft,
                widthFactor: 1,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderAlign = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Equal(Alignment.TopLeft, renderAlign.Alignment);
        Assert.Equal(1, renderAlign.WidthFactor);
        Assert.Null(renderAlign.HeightFactor);

        root.Update(new Align(
            alignment: Alignment.Center,
            widthFactor: 2,
            heightFactor: 2,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.True(ReferenceEquals(renderAlign, updated));
        Assert.Equal(Alignment.Center, updated.Alignment);
        Assert.Equal(2, updated.WidthFactor);
        Assert.Equal(2, updated.HeightFactor);
    }

    [Fact]
    public void CenterWidget_CreatesRenderAlign_WithCenterAlignment()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Center(
                widthFactor: 2,
                heightFactor: 2,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderAlign = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Equal(Alignment.Center, renderAlign.Alignment);
        Assert.Equal(2, renderAlign.WidthFactor);
        Assert.Equal(2, renderAlign.HeightFactor);
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
