using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (IntrinsicWidth, IntrinsicHeight)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIntrinsicWidth, RenderIntrinsicHeight)

public sealed class IntrinsicWidgetsTests
{
    [Fact]
    public void IntrinsicWidth_AcceptsZeroStepsAndNormalizesThemForRenderObject()
    {
        var owner = new BuildOwner();
        var widget = new IntrinsicWidth(
            stepWidth: 0.0,
            stepHeight: 0.0,
            child: new SizedBox(width: 20.0, height: 10.0));
        var root = new TestRootElement(widget);

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(0.0, widget.StepWidth);
        Assert.Equal(0.0, widget.StepHeight);
        var renderObject = RequireRenderObject<RenderIntrinsicWidth>(root.ChildElement);
        Assert.Null(renderObject.StepWidth);
        Assert.Null(renderObject.StepHeight);

        root.Update(new IntrinsicWidth(
            stepWidth: 12.0,
            stepHeight: 8.0,
            child: new SizedBox(width: 20.0, height: 10.0)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderIntrinsicWidth>(root.ChildElement);
        Assert.Same(renderObject, updated);
        Assert.Equal(12.0, updated.StepWidth);
        Assert.Equal(8.0, updated.StepHeight);
    }

    [Fact]
    public void IntrinsicWidth_RejectsInvalidWidgetAndRenderSteps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntrinsicWidth(stepWidth: -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntrinsicWidth(stepHeight: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IntrinsicWidth(stepWidth: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RenderIntrinsicWidth(stepWidth: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RenderIntrinsicWidth(stepHeight: -1.0));
    }

    [Fact]
    public void RenderIntrinsicWidth_RoundsBothAxesToConfiguredSteps()
    {
        var child = new DesiredSizeBox(new Size(70.0, 21.0));
        var intrinsic = new RenderIntrinsicWidth(
            stepWidth: 56.0,
            stepHeight: 10.0,
            child: child);

        intrinsic.Layout(new BoxConstraints(MaxWidth: 200.0, MaxHeight: 100.0));

        Assert.Equal(new Size(112.0, 30.0), intrinsic.Size);
        Assert.Equal(intrinsic.Size, child.Size);
        Assert.Equal(default, ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void IntrinsicHeightWidget_CreatesRenderIntrinsicHeight()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new IntrinsicHeight(
            child: new SizedBox(width: 20.0, height: 10.0)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RequireRenderObject<RenderIntrinsicHeight>(root.ChildElement);
    }

    [Fact]
    public void RenderIntrinsicHeight_StretchesRowChildrenToTallestHeight()
    {
        var shortChild = new DesiredSizeBox(new Size(40.0, 10.0));
        var tallChild = new DesiredSizeBox(new Size(50.0, 30.0));
        var row = new RenderFlex(
            children: [shortChild, tallChild],
            direction: Axis.Horizontal,
            crossAxisAlignment: CrossAxisAlignment.Stretch);
        var intrinsic = new RenderIntrinsicHeight(row);

        intrinsic.Layout(new BoxConstraints(MaxWidth: 200.0, MaxHeight: 100.0));

        Assert.Equal(30.0, intrinsic.Size.Height);
        Assert.Equal(30.0, row.Size.Height);
        Assert.Equal(30.0, shortChild.Size.Height);
        Assert.Equal(30.0, tallChild.Size.Height);
    }

    [Fact]
    public void RenderIntrinsicHeight_ClampsIntrinsicResultToParentConstraints()
    {
        var child = new DesiredSizeBox(new Size(30.0, 80.0));
        var intrinsic = new RenderIntrinsicHeight(child);

        intrinsic.Layout(new BoxConstraints(
            MinWidth: 0.0,
            MaxWidth: 100.0,
            MinHeight: 20.0,
            MaxHeight: 50.0));

        Assert.Equal(50.0, intrinsic.Size.Height);
        Assert.Equal(50.0, child.Size.Height);
    }

    [Fact]
    public void RenderIntrinsicHeight_TightHeightSkipsSpeculativeLayout()
    {
        var child = new CountingDesiredSizeBox(new Size(30.0, 80.0));
        var intrinsic = new RenderIntrinsicHeight(child);

        intrinsic.Layout(BoxConstraints.TightFor(width: 60.0, height: 40.0));

        Assert.Equal(1, child.LayoutCount);
        Assert.Equal(new Size(60.0, 40.0), intrinsic.Size);
    }

    [Fact]
    public void IntrinsicRenderObjectsWithoutChildrenUseSmallestSize()
    {
        var constraints = new BoxConstraints(
            MinWidth: 12.0,
            MaxWidth: 100.0,
            MinHeight: 8.0,
            MaxHeight: 80.0);
        var width = new RenderIntrinsicWidth();
        var height = new RenderIntrinsicHeight();

        width.Layout(constraints);
        height.Layout(constraints);

        Assert.Equal(constraints.Smallest, width.Size);
        Assert.Equal(constraints.Smallest, height.Size);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private class DesiredSizeBox : RenderBox
    {
        private readonly Size _desiredSize;

        public DesiredSizeBox(Size desiredSize)
        {
            _desiredSize = desiredSize;
        }

        protected override double ComputeMaxIntrinsicWidth(double height) => _desiredSize.Width;

        protected override double ComputeMaxIntrinsicHeight(double width) => _desiredSize.Height;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class CountingDesiredSizeBox : DesiredSizeBox
    {
        public CountingDesiredSizeBox(Size desiredSize) : base(desiredSize)
        {
        }

        public int LayoutCount { get; private set; }

        protected override void PerformLayout()
        {
            LayoutCount++;
            base.PerformLayout();
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

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
