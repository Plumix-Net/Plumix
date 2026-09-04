using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class StackTests
{
    [Fact]
    public void RenderStack_NonPositionedChild_UsesAlignment()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack(alignment: Alignment.BottomRight);
        stack.Insert(child);
        var constrained = new RenderConstrainedBox(
            additionalConstraints: BoxConstraints.TightFor(width: 100, height: 80),
            child: stack);
        var root = new RenderView
        {
            Child = constrained
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(100, 80), stack.Size);
        Assert.Equal(new Point(80, 70), ((StackParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderStack_PositionedChild_UsesLeftTop()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack(textDirection: TextDirection.Ltr);
        stack.Insert(child);
        var parentData = (StackParentData)child.parentData!;
        parentData.Left = 5;
        parentData.Top = 7;
        var root = new RenderView
        {
            Child = stack
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Point(5, 7), parentData.offset);
    }

    [Fact]
    public void RenderStack_PositionedChild_UsesRightBottom()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack(textDirection: TextDirection.Ltr);
        stack.Insert(child);
        var parentData = (StackParentData)child.parentData!;
        parentData.Right = 6;
        parentData.Bottom = 4;
        var root = new RenderView
        {
            Child = stack
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Point(74, 66), parentData.offset);
    }

    [Fact]
    public void StackWidget_PositionedParentData_AppliesAndUpdates()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Stack(
                alignment: Alignment.TopLeft,
                children:
                [
                    new Positioned(
                        left: 3,
                        top: 4,
                        child: new SizedBox(width: 10, height: 10)),
                ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var stack = RequireRenderObject<RenderStack>(root.ChildElement);
        var firstChild = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        var parentData = Assert.IsType<StackParentData>(firstChild.parentData);
        Assert.Equal(3, parentData.Left);
        Assert.Equal(4, parentData.Top);
        Assert.Null(parentData.Right);
        Assert.Null(parentData.Bottom);

        root.Update(new Stack(
            alignment: Alignment.BottomRight,
            children:
            [
                new Positioned(
                    right: 2,
                    bottom: 5,
                    child: new SizedBox(width: 10, height: 10)),
            ]));
        owner.FlushBuild();

        var updatedStack = RequireRenderObject<RenderStack>(root.ChildElement);
        Assert.True(ReferenceEquals(stack, updatedStack));
        Assert.Equal(Alignment.BottomRight, updatedStack.Alignment);
        var updatedFirstChild = Assert.IsAssignableFrom<RenderBox>(updatedStack.FirstChild);
        var updatedParentData = Assert.IsType<StackParentData>(updatedFirstChild.parentData);
        Assert.Null(updatedParentData.Left);
        Assert.Null(updatedParentData.Top);
        Assert.Equal(2, updatedParentData.Right);
        Assert.Equal(5, updatedParentData.Bottom);
    }

    [Fact]
    public void StackAndIndexedStack_ResolveDirectionalAlignmentOnDirectionChange()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildDirectionalStacks(TextDirection.Ltr));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var stack = RequireRenderObject<RenderStack>(root.ChildElement);
        var indexedStack = Assert.IsType<RenderIndexedStack>(stack.FirstChild);
        Assert.Equal(Alignment.BottomRight, stack.Alignment.Resolve(stack.TextDirection));
        Assert.Equal(
            Alignment.BottomRight,
            indexedStack.Alignment.Resolve(indexedStack.TextDirection));

        root.Update(BuildDirectionalStacks(TextDirection.Rtl));
        owner.FlushBuild();

        Assert.Equal(Alignment.BottomLeft, stack.Alignment.Resolve(stack.TextDirection));
        Assert.Equal(
            Alignment.BottomLeft,
            indexedStack.Alignment.Resolve(indexedStack.TextDirection));
        root.Unmount();
    }

    [Fact]
    public void Positioned_Directional_ResolvesStartAndEndFromTextDirection()
    {
        var child = new SizedBox(width: 10, height: 10);

        var ltr = Positioned.Directional(
            textDirection: Plumix.UI.TextDirection.Ltr,
            child: child,
            start: 3,
            end: 7,
            top: 5);
        Assert.Equal(3, ltr.Left);
        Assert.Equal(7, ltr.Right);
        Assert.Equal(5, ltr.Top);

        var rtl = Positioned.Directional(
            textDirection: Plumix.UI.TextDirection.Rtl,
            child: child,
            start: 3,
            end: 7,
            top: 5);
        Assert.Equal(7, rtl.Left);
        Assert.Equal(3, rtl.Right);
        Assert.Equal(5, rtl.Top);
    }

    [Fact]
    public void StackParentData_RectAndPositionedConstraintsMatchFlutter()
    {
        var data = new StackParentData();
        Assert.False(data.IsPositioned);

        data.Width = -100.0;
        Assert.True(data.IsPositioned);
        Assert.Equal(BoxConstraints.TightFor(width: 0.0), data.PositionedChildConstraints(new Size(800, 600)));

        data.Left = 0.0;
        data.Right = 0.0;
        Assert.Equal(BoxConstraints.TightFor(width: 800.0), data.PositionedChildConstraints(new Size(800, 600)));

        data.Rect = new Plumix.Rendering.RelativeRect(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, data.Left);
        Assert.Equal(2.0, data.Top);
        Assert.Equal(3.0, data.Right);
        Assert.Equal(4.0, data.Bottom);
        Assert.Equal(new Plumix.Rendering.RelativeRect(1.0, 2.0, 3.0, 4.0), data.Rect);
        Assert.Equal(
            BoxConstraints.Tight(new Size(796.0, 594.0)),
            data.PositionedChildConstraints(new Size(800, 600)));
    }

    [Fact]
    public void RenderStack_PositionedChildLeavesUnspecifiedAxisUnbounded()
    {
        var child = new ConstraintProbeRenderBox(new Size(30, 40));
        var stack = new RenderStack(textDirection: TextDirection.Ltr);
        stack.Insert(child);
        var data = (StackParentData)child.parentData!;
        data.Left = 10.0;
        data.Top = 20.0;
        data.Width = 30.0;

        stack.Layout(BoxConstraints.Tight(new Size(100, 100)));

        Assert.Equal(BoxConstraints.TightFor(width: 30.0), child.LastConstraints);
        Assert.Equal(new Point(10, 20), data.offset);
    }

    [Fact]
    public void IndexedStack_PositionedChildUsesStackParentDataAndIsADirectRenderChild()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new RawIndexedStack(
            children:
            [
                new Positioned(
                    left: 10.0,
                    top: 20.0,
                    width: 30.0,
                    height: 40.0,
                    child: new SizedBox()),
                new SizedBox(width: 5.0, height: 5.0),
            ],
            index: 0,
            textDirection: TextDirection.Ltr));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var stack = RequireRenderObject<RenderIndexedStack>(root.ChildElement);
        RenderBox child = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        var data = Assert.IsType<StackParentData>(child.parentData);
        stack.Layout(BoxConstraints.Tight(new Size(100, 100)));

        Assert.Same(stack, child.Parent);
        Assert.Equal(new Size(30, 40), child.Size);
        Assert.Equal(new Point(10, 20), data.offset);
        root.Unmount();
    }

    [Fact]
    public void IndexedStackElement_VisitsOnlyTheSelectedChildOnstage()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new RawIndexedStack(
            children:
            [
                new SizedBox(width: 1.0),
                new SizedBox(width: 2.0),
                new SizedBox(width: 3.0),
            ],
            index: 1,
            textDirection: TextDirection.Ltr));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var element = Assert.IsType<IndexedStackElement>(root.ChildElement);
        var onstage = new List<Element>();
        element.DebugVisitOnstageChildren(onstage.Add);

        Element selected = Assert.Single(onstage);
        Assert.Equal(2.0, Assert.IsType<SizedBox>(selected.Widget).Width);
        root.Unmount();
    }

    [Fact]
    public void RenderIndexedStack_UsesOnlyTheSelectedChildForHitTestingSemanticsAndBaseline()
    {
        var first = new ConstraintProbeRenderBox(new Size(30, 20), baseline: 4.0);
        var second = new ConstraintProbeRenderBox(new Size(30, 20), baseline: 7.0);
        var stack = new RenderIndexedStack(
            children: [first, second],
            textDirection: TextDirection.Ltr,
            index: 1);
        ((StackParentData)second.parentData!).Top = 10.0;
        stack.Layout(BoxConstraints.Tight(new Size(100, 100)));

        Assert.True(stack.HitTest(new BoxHitTestResult(), new Point(1, 11)));
        Assert.Equal(0, first.HitTestCount);
        Assert.Equal(1, second.HitTestCount);

        var semanticsChildren = new List<RenderObject>();
        stack.VisitChildrenForSemantics(semanticsChildren.Add);
        Assert.Equal([second], semanticsChildren);
        Assert.Equal(17.0, stack.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.Equal(
            17.0,
            stack.GetDryBaseline(BoxConstraints.Tight(new Size(100, 100)), TextBaseline.Alphabetic));
    }

    [DebugOnlyFact]
    public void RawIndexedStack_ValidatesIndexAndDirectionalAlignmentLikeFlutter()
    {
        _ = new RawIndexedStack();
        _ = new RawIndexedStack(children: [new SizedBox()], index: null);
        Assert.Throws<AssertionError>(() => new RawIndexedStack(children: [new SizedBox()], index: -1));
        Assert.Throws<AssertionError>(() => new RawIndexedStack(children: [new SizedBox()], index: 1));

        var directional = new RawIndexedStack(children: [new SizedBox()]);
        var owner = new BuildOwner();
        var contextRoot = new TestRootElement(new SizedBox());
        contextRoot.Attach(owner);
        contextRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Throws<AssertionError>(() => directional.CreateRenderObject(new BuildContext(contextRoot)));
        contextRoot.Unmount();
    }

    private static Widget BuildDirectionalStacks(TextDirection direction) => new Directionality(
        direction,
        new Stack(
            alignment: AlignmentDirectional.BottomEnd,
            children:
            [
                new IndexedStack(
                    alignment: AlignmentDirectional.BottomEnd,
                    children: [new SizedBox(width: 10, height: 10)]),
            ]));

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

    private sealed class ConstraintProbeRenderBox : RenderBox
    {
        private readonly Size _size;
        private readonly double? _baseline;

        public ConstraintProbeRenderBox(Size size, double? baseline = null)
        {
            _size = size;
            _baseline = baseline;
        }

        public BoxConstraints LastConstraints { get; private set; }
        public int HitTestCount { get; private set; }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_size);

        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) =>
            _baseline;

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => _baseline;

        protected override void PerformLayout()
        {
            LastConstraints = Constraints;
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position)
        {
            HitTestCount += 1;
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

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
