using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class OverflowBoxTests
{
    [Fact]
    public void RenderConstrainedOverflowBox_MaxFit_UsesParentBiggestSize()
    {
        var child = new FixedSizeRenderBox(new Size(190, 86));
        var overflow = new RenderConstrainedOverflowBox(
            alignment: Alignment.Center,
            maxWidth: 140,
            maxHeight: 70,
            fit: OverflowBoxFit.Max,
            child: child);
        var root = new RenderView
        {
            Child = overflow
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 60));

        Assert.Equal(new Size(100, 60), overflow.Size);
        Assert.Equal(new Size(140, 70), child.Size);
        Assert.Equal(new Point(-20, -5), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderConstrainedOverflowBox_DeferToChild_FollowsChildSizeWithinParentConstraints()
    {
        var child = new FixedSizeRenderBox(new Size(300, 200));
        var overflow = new RenderConstrainedOverflowBox(
            alignment: Alignment.TopLeft,
            maxWidth: 72,
            maxHeight: 44,
            fit: OverflowBoxFit.DeferToChild,
            child: child);
        var root = new RenderView
        {
            Child = overflow
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(140, 100));

        Assert.Equal(new Size(72, 44), overflow.Size);
        Assert.Equal(new Size(72, 44), child.Size);
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderSizedOverflowBox_UsesRequestedSize_AndAlignsOverflowingChild()
    {
        var child = new FixedSizeRenderBox(new Size(140, 92));
        var sizedOverflow = new RenderSizedOverflowBox(
            requestedSize: new Size(90, 44),
            alignment: Alignment.Center,
            child: child);
        var root = new RenderView
        {
            Child = sizedOverflow
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(120, 100));

        Assert.Equal(new Size(90, 44), sizedOverflow.Size);
        Assert.Equal(new Size(120, 92), child.Size);
        Assert.Equal(new Point(-15, -24), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void OverflowBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new OverflowBox(
                alignment: Alignment.TopLeft,
                minWidth: 10,
                maxWidth: 70,
                minHeight: 5,
                maxHeight: 40,
                fit: OverflowBoxFit.DeferToChild,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderConstrainedOverflowBox>(root.ChildElement);
        Assert.Equal(Alignment.TopLeft, renderBox.Alignment);
        Assert.Equal(10, renderBox.MinWidth);
        Assert.Equal(70, renderBox.MaxWidth);
        Assert.Equal(5, renderBox.MinHeight);
        Assert.Equal(40, renderBox.MaxHeight);
        Assert.Equal(OverflowBoxFit.DeferToChild, renderBox.Fit);

        root.Update(new OverflowBox(
            alignment: Alignment.BottomRight,
            minWidth: null,
            maxWidth: 120,
            minHeight: null,
            maxHeight: 80,
            fit: OverflowBoxFit.Max,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderConstrainedOverflowBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(Alignment.BottomRight, updated.Alignment);
        Assert.Null(updated.MinWidth);
        Assert.Equal(120, updated.MaxWidth);
        Assert.Null(updated.MinHeight);
        Assert.Equal(80, updated.MaxHeight);
        Assert.Equal(OverflowBoxFit.Max, updated.Fit);
    }

    [Fact]
    public void SizedOverflowBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new SizedOverflowBox(
                size: new Size(80, 40),
                alignment: Alignment.TopLeft,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderSizedOverflowBox>(root.ChildElement);
        Assert.Equal(new Size(80, 40), renderBox.RequestedSize);
        Assert.Equal(Alignment.TopLeft, renderBox.Alignment);

        root.Update(new SizedOverflowBox(
            size: new Size(120, 50),
            alignment: Alignment.BottomRight,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderSizedOverflowBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(new Size(120, 50), updated.RequestedSize);
        Assert.Equal(Alignment.BottomRight, updated.Alignment);
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
