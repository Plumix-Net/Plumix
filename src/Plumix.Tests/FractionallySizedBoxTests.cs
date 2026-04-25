using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class FractionallySizedBoxTests
{
    [Fact]
    public void RenderFractionallySizedBox_AppliesWidthAndHeightFactors()
    {
        var child = new FixedSizeRenderBox(new Size(20, 20));
        var fractionallySizedBox = new RenderFractionallySizedBox(
            widthFactor: 0.5,
            heightFactor: 0.25,
            child: child);

        fractionallySizedBox.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 120));

        Assert.Equal(new Size(100, 30), child.Size);
        Assert.Equal(new Size(100, 30), fractionallySizedBox.Size);
    }

    [Fact]
    public void RenderFractionallySizedBox_PassesThroughAxis_WhenFactorIsNull()
    {
        var child = new FixedSizeRenderBox(new Size(20, 40));
        var fractionallySizedBox = new RenderFractionallySizedBox(
            widthFactor: 0.5,
            heightFactor: null,
            child: child);

        fractionallySizedBox.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 120));

        Assert.Equal(100, child.Size.Width);
        Assert.Equal(40, child.Size.Height);
        Assert.Equal(new Size(100, 40), fractionallySizedBox.Size);
    }

    [Fact]
    public void RenderFractionallySizedBox_AlignsChild_WhenParentConstraintsAreTight()
    {
        var child = new FixedSizeRenderBox(new Size(20, 20));
        var fractionallySizedBox = new RenderFractionallySizedBox(
            alignment: Alignment.BottomRight,
            widthFactor: 0.5,
            heightFactor: 0.5,
            child: child);

        fractionallySizedBox.Layout(BoxConstraints.TightFor(width: 200, height: 120));

        Assert.Equal(new Size(100, 60), child.Size);
        Assert.Equal(new Size(200, 120), fractionallySizedBox.Size);
        Assert.Equal(new Point(100, 60), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void FractionallySizedBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new FractionallySizedBox(
                alignment: Alignment.TopLeft,
                widthFactor: 0.7,
                heightFactor: 0.6,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderFractionallySizedBox>(root.ChildElement);
        Assert.Equal(Alignment.TopLeft, renderBox.Alignment);
        Assert.Equal(0.7, renderBox.WidthFactor);
        Assert.Equal(0.6, renderBox.HeightFactor);

        root.Update(new FractionallySizedBox(
            alignment: Alignment.BottomRight,
            widthFactor: null,
            heightFactor: 0.8,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderFractionallySizedBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(Alignment.BottomRight, updated.Alignment);
        Assert.Null(updated.WidthFactor);
        Assert.Equal(0.8, updated.HeightFactor);
    }

    [Fact]
    public void FractionallySizedBox_Throws_ForNegativeFactor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FractionallySizedBox(widthFactor: -0.1));
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
