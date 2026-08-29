using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class CustomMultiChildLayoutTests
{
    [Fact]
    public void LayoutId_UsesIdAsDefaultKeyAndExposesSourceContract()
    {
        var child = new SizedBox(width: 10, height: 12);
        object id = new object();
        var layoutId = new LayoutId(id, child);
        var explicitKey = new ValueKey<string>("slot");
        var keyedLayoutId = new LayoutId(id, child, explicitKey);

        Assert.Same(id, layoutId.Id);
        Assert.Same(child, layoutId.Child);
        Assert.Equal(new ValueKey<object>(id), layoutId.Key);
        Assert.Same(explicitKey, keyedLayoutId.Key);
        Assert.Equal(typeof(CustomMultiChildLayout), layoutId.DebugTypicalAncestorWidgetType);
        Assert.Throws<ArgumentNullException>(() => new LayoutId(null!, child));
    }

    [Fact]
    public void RenderCustomMultiChildLayout_UsesDelegateSizeConstraintsAndPositions()
    {
        var leader = new FixedSizeRenderBox(new Size(60, 40), hitTestSelf: true);
        var follower = new FixedSizeRenderBox(new Size(90, 70), hitTestSelf: true);
        var layoutDelegate = new FollowTheLeaderDelegate();
        var layout = new RenderCustomMultiChildLayoutBox(layoutDelegate, [leader, follower]);
        ((MultiChildLayoutParentData)leader.parentData!).Id = TestSlot.Leader;
        ((MultiChildLayoutParentData)follower.parentData!).Id = TestSlot.Follower;

        layout.Layout(new BoxConstraints(MaxWidth: 200, MaxHeight: 120));

        Assert.Equal(new Size(160, 100), layout.Size);
        Assert.Equal(new Size(60, 40), leader.Size);
        Assert.Equal(new Size(60, 40), follower.Size);
        Assert.Equal(new Point(0, 0), ((MultiChildLayoutParentData)leader.parentData!).offset);
        Assert.Equal(new Point(100, 60), ((MultiChildLayoutParentData)follower.parentData!).offset);
        Assert.True(layoutDelegate.SawLeader);
        Assert.True(layoutDelegate.SawFollower);

        var hitResult = new BoxHitTestResult();
        Assert.True(layout.HitTest(hitResult, new Point(110, 70)));
        Assert.Same(follower, hitResult.Path[0].Target);
    }

    [Fact]
    public void RenderCustomMultiChildLayout_RelayoutListenableSkipsWidgetBuild()
    {
        using var relayout = new ChangeNotifier();
        var child = new FixedSizeRenderBox(new Size(20, 20));
        var layoutDelegate = new RelayoutDelegate(relayout);
        var layout = new RenderCustomMultiChildLayoutBox(layoutDelegate, [child]);
        ((MultiChildLayoutParentData)child.parentData!).Id = TestSlot.Leader;
        var renderView = new RenderView { Child = layout };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 60));
        int initialLayoutCount = layoutDelegate.LayoutCount;

        relayout.NotifyListeners();

        pipeline.FlushLayout(new Size(100, 60));
        Assert.Equal(initialLayoutCount + 1, layoutDelegate.LayoutCount);
    }

    [Fact]
    public void RenderCustomMultiChildLayout_DelegateReplacementUsesShouldRelayout()
    {
        var child = new FixedSizeRenderBox(new Size(20, 20));
        var first = new ConfigurableDelegate(width: 80, shouldRelayout: false);
        var layout = new RenderCustomMultiChildLayoutBox(first, [child]);
        ((MultiChildLayoutParentData)child.parentData!).Id = TestSlot.Leader;
        var renderView = new RenderView { Child = layout };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(120, 60));
        int childLayoutCount = child.LayoutCount;

        var noRelayout = new ConfigurableDelegate(width: 80, shouldRelayout: false);
        layout.Delegate = noRelayout;
        pipeline.FlushLayout(new Size(120, 60));
        Assert.Equal(childLayoutCount, child.LayoutCount);

        var relayout = new ConfigurableDelegate(width: 50, shouldRelayout: true);
        layout.Delegate = relayout;
        pipeline.FlushLayout(new Size(120, 60));
        Assert.Equal(childLayoutCount + 1, child.LayoutCount);
        Assert.Equal(new Size(50, 60), layout.Size);
    }

    [Fact]
    public void RenderCustomMultiChildLayout_EnforcesLayoutIdAndExactlyOnceContracts()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var untaggedChild = new FixedSizeRenderBox(new Size(10, 10));
        var untagged = new RenderCustomMultiChildLayoutBox(new LayOutAllDelegate(), [untaggedChild]);
        Assert.Throws<InvalidOperationException>(() => untagged.Layout(BoxConstraints.Tight(new Size(40, 40))));

        var first = new FixedSizeRenderBox(new Size(10, 10));
        var second = new FixedSizeRenderBox(new Size(10, 10));
        var duplicate = new RenderCustomMultiChildLayoutBox(new LayOutAllDelegate(), [first, second]);
        ((MultiChildLayoutParentData)first.parentData!).Id = TestSlot.Leader;
        ((MultiChildLayoutParentData)second.parentData!).Id = TestSlot.Leader;
        Assert.Throws<InvalidOperationException>(() => duplicate.Layout(BoxConstraints.Tight(new Size(40, 40))));

        var omittedChild = new FixedSizeRenderBox(new Size(10, 10));
        var omitted = new RenderCustomMultiChildLayoutBox(new OmitChildDelegate(), [omittedChild]);
        ((MultiChildLayoutParentData)omittedChild.parentData!).Id = TestSlot.Leader;
        Assert.Throws<InvalidOperationException>(() => omitted.Layout(BoxConstraints.Tight(new Size(40, 40))));

        var repeatedChild = new FixedSizeRenderBox(new Size(10, 10));
        var repeated = new RenderCustomMultiChildLayoutBox(new RepeatChildDelegate(), [repeatedChild]);
        ((MultiChildLayoutParentData)repeatedChild.parentData!).Id = TestSlot.Leader;
        Assert.Throws<InvalidOperationException>(() => repeated.Layout(BoxConstraints.Tight(new Size(40, 40))));

        var invalidChild = new FixedSizeRenderBox(new Size(10, 10));
        var invalid = new RenderCustomMultiChildLayoutBox(new InvalidConstraintsDelegate(), [invalidChild]);
        ((MultiChildLayoutParentData)invalidChild.parentData!).Id = TestSlot.Leader;
        Assert.Throws<InvalidOperationException>(() => invalid.Layout(BoxConstraints.Tight(new Size(40, 40))));
    }

    [Theory]
    [InlineData(TextDirection.Ltr, true, 0, 100, 270)]
    [InlineData(TextDirection.Ltr, false, 0, 56, 270)]
    [InlineData(TextDirection.Rtl, true, 260, 100, 0)]
    [InlineData(TextDirection.Rtl, false, 260, 144, 0)]
    public void NavigationToolbar_LayoutsSlotsForDirectionAndCenterPolicy(
        TextDirection textDirection,
        bool centerMiddle,
        double expectedLeadingX,
        double expectedMiddleX,
        double expectedTrailingX)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            textDirection,
            new NavigationToolbar(
                leading: new SizedBox(width: 40),
                middle: new SizedBox(width: 100, height: 20),
                trailing: new SizedBox(width: 30, height: 20),
                centerMiddle: centerMiddle)));
        Mount(root, owner);
        var layout = FindRenderObject<RenderCustomMultiChildLayoutBox>(root);

        layout.Layout(BoxConstraints.Tight(new Size(300, 56)));

        RenderBox leading = layout.FirstChild!;
        RenderBox middle = layout.ChildAfter(leading)!;
        RenderBox trailing = layout.ChildAfter(middle)!;
        Assert.Equal(new Point(expectedLeadingX, 0), OffsetOf(leading));
        Assert.Equal(new Point(expectedMiddleX, 18), OffsetOf(middle));
        Assert.Equal(new Point(expectedTrailingX, 18), OffsetOf(trailing));
        Assert.Equal(new Size(40, 56), leading.Size);
        Assert.Equal(new Size(100, 20), middle.Size);
        Assert.Equal(new Size(30, 20), trailing.Size);
    }

    [Fact]
    public void NavigationToolbar_ClampsCenteredMiddleBetweenLargeEdgeSlots()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new NavigationToolbar(
            leading: new SizedBox(width: 120),
            middle: new SizedBox(width: 100, height: 20),
            trailing: new SizedBox(width: 80, height: 20)));
        Mount(root, owner);
        var layout = FindRenderObject<RenderCustomMultiChildLayoutBox>(root);

        layout.Layout(BoxConstraints.Tight(new Size(300, 56)));

        RenderBox leading = layout.FirstChild!;
        RenderBox middle = layout.ChildAfter(leading)!;
        RenderBox trailing = layout.ChildAfter(middle)!;
        Assert.Equal(new Point(0, 0), OffsetOf(leading));
        Assert.Equal(new Point(136, 18), OffsetOf(middle));
        Assert.Equal(new Point(220, 18), OffsetOf(trailing));
        Assert.Equal(new Size(68, 20), middle.Size);
    }

    [Fact]
    public void NavigationToolbar_ExposesFlutterDefaultsAndAllowsMissingSlots()
    {
        var toolbar = new NavigationToolbar(middle: new SizedBox(width: 40, height: 20));
        Assert.Null(toolbar.Leading);
        Assert.NotNull(toolbar.Middle);
        Assert.Null(toolbar.Trailing);
        Assert.True(toolbar.CenterMiddle);
        Assert.Equal(NavigationToolbar.KMiddleSpacing, toolbar.MiddleSpacing);

        var owner = new BuildOwner();
        var root = new TestRootElement(toolbar);
        Mount(root, owner);
        var layout = FindRenderObject<RenderCustomMultiChildLayoutBox>(root);
        layout.Layout(BoxConstraints.Tight(new Size(200, 56)));

        RenderBox middle = Assert.IsAssignableFrom<RenderBox>(layout.FirstChild);
        Assert.Equal(new Point(80, 18), OffsetOf(middle));
        Assert.Equal(1, layout.ChildCount);
    }

    private static Point OffsetOf(RenderBox child)
    {
        return ((MultiChildLayoutParentData)child.parentData!).offset;
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T FindRenderObject<T>(Element root) where T : RenderObject
    {
        T? match = null;

        void Visit(Element element)
        {
            if (match is not null)
            {
                return;
            }

            if (element.RenderObject is T typed)
            {
                match = typed;
                return;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        return match ?? throw new InvalidOperationException($"No render object of type {typeof(T).Name} found.");
    }

    private enum TestSlot
    {
        Leader,
        Follower
    }

    private sealed class FollowTheLeaderDelegate : MultiChildLayoutDelegate
    {
        public bool SawLeader { get; private set; }
        public bool SawFollower { get; private set; }

        public override Size GetSize(BoxConstraints constraints) => new Size(160, 100);

        public override void PerformLayout(Size size)
        {
            SawLeader = HasChild(TestSlot.Leader);
            SawFollower = HasChild(TestSlot.Follower);
            Size leaderSize = LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
            PositionChild(TestSlot.Leader, default);
            LayoutChild(TestSlot.Follower, BoxConstraints.Tight(leaderSize));
            PositionChild(
                TestSlot.Follower,
                new Point(size.Width - leaderSize.Width, size.Height - leaderSize.Height));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class RelayoutDelegate(IListenable relayout) : MultiChildLayoutDelegate(relayout)
    {
        public int LayoutCount { get; private set; }

        public override void PerformLayout(Size size)
        {
            LayoutCount++;
            LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class ConfigurableDelegate(double width, bool shouldRelayout) : MultiChildLayoutDelegate
    {
        public override Size GetSize(BoxConstraints constraints)
        {
            return new Size(width, constraints.Biggest.Height);
        }

        public override void PerformLayout(Size size)
        {
            LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => shouldRelayout;
    }

    private sealed class LayOutAllDelegate : MultiChildLayoutDelegate
    {
        public override void PerformLayout(Size size)
        {
            LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class OmitChildDelegate : MultiChildLayoutDelegate
    {
        public override void PerformLayout(Size size)
        {
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class RepeatChildDelegate : MultiChildLayoutDelegate
    {
        public override void PerformLayout(Size size)
        {
            LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
            LayoutChild(TestSlot.Leader, BoxConstraints.Loose(size));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class InvalidConstraintsDelegate : MultiChildLayoutDelegate
    {
        public override void PerformLayout(Size size)
        {
            LayoutChild(TestSlot.Leader, new BoxConstraints(MinWidth: 20, MaxWidth: 10));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly bool _hitTestSelf;

        public FixedSizeRenderBox(Size desiredSize, bool hitTestSelf = false)
        {
            _desiredSize = desiredSize;
            _hitTestSelf = hitTestSelf;
        }

        public int LayoutCount { get; private set; }

        protected override void PerformLayout()
        {
            LayoutCount++;
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override bool HitTestSelf(Point position) => _hitTestSelf;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
