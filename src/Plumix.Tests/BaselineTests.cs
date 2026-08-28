using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (Baseline, IgnoreBaseline)
// flutter/packages/flutter/lib/src/rendering/shifted_box.dart (RenderBaseline)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIgnoreBaseline)

public sealed class BaselineTests
{
    [Fact]
    public void BaselineWidget_CreatesAndUpdatesRenderBaseline()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Baseline(
            baseline: 18,
            baselineType: TextBaseline.Alphabetic,
            child: new SizedBox(width: 20, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var baseline = RequireRenderObject<RenderBaseline>(root.ChildElement);
        Assert.Equal(18, baseline.Baseline);
        Assert.Equal(TextBaseline.Alphabetic, baseline.BaselineType);

        root.Update(new Baseline(
            baseline: 24,
            baselineType: TextBaseline.Ideographic,
            child: new SizedBox(width: 20, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderBaseline>(root.ChildElement);
        Assert.Same(baseline, updated);
        Assert.Equal(24, updated.Baseline);
        Assert.Equal(TextBaseline.Ideographic, updated.BaselineType);
    }

    [Fact]
    public void Baseline_OffsetsRealChildBaselineAndSizesToContainChild()
    {
        var child = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 8, ideographicBaseline: 9);
        var baseline = new RenderBaseline(
            baseline: 15,
            baselineType: TextBaseline.Alphabetic,
            child: child);

        baseline.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(new Size(20, 17), baseline.Size);
        Assert.Equal(new Point(0, 7), ((BoxParentData)child.parentData!).offset);
        Assert.Equal(15, baseline.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.Equal(16, baseline.GetDistanceToBaseline(TextBaseline.Ideographic, onlyReal: true));
    }

    [Fact]
    public void Baseline_UsesChildBottomWhenChildHasNoBaseline()
    {
        var child = new FixedBaselineBox(new Size(20, 10));
        var baseline = new RenderBaseline(
            baseline: 15,
            baselineType: TextBaseline.Alphabetic,
            child: child);

        baseline.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(new Size(20, 15), baseline.Size);
        Assert.Equal(new Point(0, 5), ((BoxParentData)child.parentData!).offset);
        Assert.Null(baseline.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.Equal(15, baseline.GetDistanceToBaseline(TextBaseline.Alphabetic));
    }

    [Fact]
    public void Baseline_PreservesNegativeTopOffsetWhenTargetPrecedesChildBaseline()
    {
        var child = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 8);
        var baseline = new RenderBaseline(
            baseline: 5,
            baselineType: TextBaseline.Alphabetic,
            child: child);

        baseline.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(new Size(20, 7), baseline.Size);
        Assert.Equal(new Point(0, -3), ((BoxParentData)child.parentData!).offset);
        Assert.Equal(5, baseline.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
    }

    [Fact]
    public void ProxyBoxes_ForwardChildBaselineIncludingPaintOffset()
    {
        var child = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 7);
        var padding = new RenderPadding(new Thickness(2, 4, 6, 8), child);

        padding.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(11, padding.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
    }

    [Fact]
    public void IgnoreBaseline_LaysOutChildButSuppressesRealBaseline()
    {
        var child = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 7);
        var ignore = new RenderIgnoreBaseline(child);

        ignore.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(child.Size, ignore.Size);
        Assert.Null(ignore.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.Equal(ignore.Size.Height, ignore.GetDistanceToBaseline(TextBaseline.Alphabetic));
    }

    [Fact]
    public void IgnoreBaselineWidget_CreatesRenderIgnoreBaseline()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new IgnoreBaseline(
            child: new SizedBox(width: 20, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RequireRenderObject<RenderIgnoreBaseline>(root.ChildElement);
    }

    [Fact]
    public void Flex_BaselineAlignmentSkipsIgnoredChildAndAlignsRealBaselines()
    {
        var first = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 8);
        var ignoredChild = new FixedBaselineBox(new Size(20, 52), alphabeticBaseline: 40);
        var ignored = new RenderIgnoreBaseline(ignoredChild);
        var last = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 5);
        var flex = new RenderFlex(
            children: [first, ignored, last],
            direction: Axis.Horizontal,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic,
            textDirection: TextDirection.Ltr);

        flex.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(52, flex.Size.Height);
        Assert.Equal(0, ((FlexParentData)first.parentData!).offset.Y);
        Assert.Equal(0, ((FlexParentData)ignored.parentData!).offset.Y);
        Assert.Equal(3, ((FlexParentData)last.parentData!).offset.Y);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class FixedBaselineBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly double? _alphabeticBaseline;
        private readonly double? _ideographicBaseline;

        public FixedBaselineBox(
            Size desiredSize,
            double? alphabeticBaseline = null,
            double? ideographicBaseline = null)
        {
            _desiredSize = desiredSize;
            _alphabeticBaseline = alphabeticBaseline;
            _ideographicBaseline = ideographicBaseline;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
            baseline == TextBaseline.Alphabetic ? _alphabeticBaseline : _ideographicBaseline;
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
