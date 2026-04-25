using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class UnconstrainedLimitedBoxTests
{
    [Fact]
    public void RenderUnconstrainedBox_UnconstrainsBothAxes_AndAlignsChild()
    {
        var child = new FixedSizeRenderBox(new Size(120, 40));
        var unconstrained = new RenderUnconstrainedBox(
            alignment: Alignment.Center,
            child: child);
        var root = new RenderView
        {
            Child = unconstrained
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(80, 80));

        Assert.Equal(new Size(80, 40), unconstrained.Size);
        Assert.Equal(new Size(120, 40), child.Size);
        Assert.Equal(new Point(-20, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderUnconstrainedBox_WithHorizontalAxis_RetainsHorizontalConstraints()
    {
        var child = new FixedSizeRenderBox(new Size(120, 200));
        var unconstrained = new RenderUnconstrainedBox(
            alignment: Alignment.TopLeft,
            constrainedAxis: Axis.Horizontal,
            child: child);
        var root = new RenderView
        {
            Child = unconstrained
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(80, 80));

        Assert.Equal(new Size(80, 80), unconstrained.Size);
        Assert.Equal(new Size(80, 200), child.Size);
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void UnconstrainedBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                constrainedAxis: Axis.Vertical,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderUnconstrainedBox>(root.ChildElement);
        Assert.Equal(Alignment.TopLeft, renderBox.Alignment);
        Assert.Equal(Axis.Vertical, renderBox.ConstrainedAxis);

        root.Update(new UnconstrainedBox(
            alignment: Alignment.BottomRight,
            constrainedAxis: null,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderUnconstrainedBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(Alignment.BottomRight, updated.Alignment);
        Assert.Null(updated.ConstrainedAxis);
    }

    [Fact]
    public void RenderLimitedBox_AppliesLimitsWhenParentIsUnbounded()
    {
        var child = new FixedSizeRenderBox(new Size(300, 200));
        var limited = new RenderLimitedBox(
            maxWidth: 100,
            maxHeight: 80,
            child: child);

        limited.Layout(new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: 0,
            MaxHeight: double.PositiveInfinity));

        Assert.Equal(new Size(100, 80), child.Size);
        Assert.Equal(new Size(100, 80), limited.Size);
    }

    [Fact]
    public void RenderLimitedBox_IgnoresOwnLimitsWhenParentIsBounded()
    {
        var child = new FixedSizeRenderBox(new Size(300, 200));
        var limited = new RenderLimitedBox(
            maxWidth: 100,
            maxHeight: 80,
            child: child);

        limited.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 60, MinHeight: 0, MaxHeight: 50));

        Assert.Equal(new Size(60, 50), child.Size);
        Assert.Equal(new Size(60, 50), limited.Size);
    }

    [Fact]
    public void LimitedBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new LimitedBox(
                maxWidth: 120,
                maxHeight: 70,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderLimitedBox>(root.ChildElement);
        Assert.Equal(120, renderBox.MaxWidth);
        Assert.Equal(70, renderBox.MaxHeight);

        root.Update(new LimitedBox(
            maxWidth: 90,
            maxHeight: 44,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderLimitedBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(90, updated.MaxWidth);
        Assert.Equal(44, updated.MaxHeight);
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
