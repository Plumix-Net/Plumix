using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class FittedBoxTests
{
    [Fact]
    public void RenderFittedBox_Contain_PreservesAspectRatioInLayout()
    {
        var child = new FixedSizeRenderBox(new Size(120, 60));
        var fittedBox = new RenderFittedBox(
            fit: BoxFit.Contain,
            child: child);

        fittedBox.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 100, MinHeight: 0, MaxHeight: 100));

        Assert.Equal(new Size(100, 50), fittedBox.Size);
    }

    [Fact]
    public void RenderFittedBox_ScaleDown_DoesNotGrowChildWhenSpaceIsLarger()
    {
        var child = new FixedSizeRenderBox(new Size(40, 20));
        var fittedBox = new RenderFittedBox(
            fit: BoxFit.ScaleDown,
            child: child);

        fittedBox.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 200));

        Assert.Equal(new Size(40, 20), fittedBox.Size);
    }

    [Fact]
    public void RenderFittedBox_HitTest_UsesTransformAndAlignment()
    {
        var child = new HitTestRenderBox(new Size(20, 10));
        var fittedBox = new RenderFittedBox(
            fit: BoxFit.None,
            alignment: Alignment.BottomRight,
            child: child);

        fittedBox.Layout(BoxConstraints.TightFor(width: 100, height: 80));

        var hitResult = new BoxHitTestResult();
        var hit = fittedBox.HitTest(hitResult, new Point(90, 75));
        Assert.True(hit);

        var missResult = new BoxHitTestResult();
        var miss = fittedBox.HitTest(missResult, new Point(10, 10));
        Assert.False(miss);
    }

    [Fact]
    public void FittedBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new FittedBox(
                fit: BoxFit.Fill,
                alignment: Alignment.TopLeft,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderFittedBox>(root.ChildElement);
        Assert.Equal(BoxFit.Fill, renderBox.Fit);
        Assert.Equal(Alignment.TopLeft, renderBox.Alignment);

        root.Update(new FittedBox(
            fit: BoxFit.None,
            alignment: Alignment.BottomRight,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderFittedBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(BoxFit.None, updated.Fit);
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

    private sealed class HitTestRenderBox : RenderBox
    {
        private readonly Size _size;

        public HitTestRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return true;
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
