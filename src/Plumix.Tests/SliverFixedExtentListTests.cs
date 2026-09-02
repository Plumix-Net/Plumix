using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/rendering/sliver_fixed_extent_layout_test.dart.

namespace Plumix.Tests;

public sealed class SliverFixedExtentListTests
{
    private const double GenericItemExtent = 600.0;
    private const double ExtraValueToNotHaveRoundingIssues = 1e-10;
    private const double ExtraValueToHaveRoundingIssues = 1e-11;

    [Fact]
    public void RenderSliverFixedExtentList_ReifiesTheChildAtTheRoundedScrollOffset()
    {
        List<RenderBox> children =
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ];
        var manager = new ListChildManager(children);
        var offset = new TestViewportOffset();
        PipelineOwner pipeline = Harness(manager.CreateFillViewport(), offset, cacheExtent: 0);

        pipeline.FlushLayout(new Size(800, 600));
        Assert.True(children[0].Attached);
        Assert.False(children[1].Attached);

        offset.JumpTo(600);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(children[0].Attached);
        Assert.True(children[1].Attached);

        // Simulate double precision error: 1199.999999999998 rounds to the third page.
        offset.JumpTo(1199.999999999998);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(children[1].Attached);
        Assert.True(children[2].Attached);
    }

    [Theory]
    // Regression coverage for https://github.com/flutter/flutter/issues/68182.
    [InlineData(1234.0, 0.0, 0)]
    [InlineData(0.0, GenericItemExtent, 0)]
    [InlineData(GenericItemExtent, GenericItemExtent, 0)]
    [InlineData(GenericItemExtent + 1, GenericItemExtent, 1)]
    [InlineData(GenericItemExtent + ExtraValueToNotHaveRoundingIssues, GenericItemExtent, 1)]
    [InlineData(GenericItemExtent * 4.5, GenericItemExtent, 4)]
    [InlineData(414.0 * 6, 414.0, 5)]
    [InlineData(
        (411.42857142857144 * 6) + ExtraValueToHaveRoundingIssues,
        411.42857142857144,
        5)]
    [InlineData(GenericItemExtent + ExtraValueToHaveRoundingIssues, GenericItemExtent, 0)]
    public void GetMaxChildIndexForScrollOffset_AbsorbsDivisionRoundingError(
        double scrollOffset,
        double itemExtent,
        int expected)
    {
        var sliver = new RenderSliverFixedExtentList(itemExtent);

        Assert.Equal(expected, sliver.GetMaxChildIndexForScrollOffset(scrollOffset, itemExtent));
    }

    [Fact]
    public void RenderSliverMultiBoxAdaptor_PaintsChildOnlyWhileItIsReified()
    {
        List<RenderBox> children =
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ];
        var manager = new ListChildManager(children);
        RenderSliverFillViewport sliver = manager.CreateFillViewport();
        var offset = new TestViewportOffset();
        PipelineOwner pipeline = Harness(sliver, offset, cacheExtent: 0);

        pipeline.FlushLayout(new Size(800, 600));
        Assert.True(sliver.PaintsChild(children[0]));
        Assert.False(sliver.PaintsChild(children[1]));
        Assert.False(sliver.PaintsChild(children[2]));

        offset.JumpTo(600);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(sliver.PaintsChild(children[0]));
        Assert.True(sliver.PaintsChild(children[1]));
        Assert.False(sliver.PaintsChild(children[2]));

        offset.JumpTo(1200);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(sliver.PaintsChild(children[0]));
        Assert.False(sliver.PaintsChild(children[1]));
        Assert.True(sliver.PaintsChild(children[2]));
    }

    [Theory]
    [InlineData(0.0, 0, 0)]
    [InlineData(1200.0, 2, 1)]
    public void RenderSliverFillViewport_IgnoresTheDeprecatedItemExtentArgument(
        double scrollOffset,
        int expectedMinIndex,
        int expectedMaxIndex)
    {
        List<RenderBox> children =
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ];
        var manager = new ListChildManager(children);
        RenderSliverFillViewport sliver = manager.CreateFillViewport();
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset(scrollOffset), cacheExtent: 100);
        pipeline.FlushLayout(new Size(800, 600));

        // The item extent that fills the viewport is 600; the 150 passed to each hook is the
        // deprecated argument Flutter keeps for source compatibility and no override reads.
        Assert.Equal(scrollOffset, sliver.ConstraintsForSliver.ScrollOffset);
        Assert.Equal(600.0, sliver.ItemExtent);
        Assert.Equal(6000.0, sliver.IndexToLayoutOffset(150.0, 10));
        Assert.Equal(expectedMinIndex, sliver.GetMinChildIndexForScrollOffset(scrollOffset, 150.0));
        Assert.Equal(expectedMaxIndex, sliver.GetMaxChildIndexForScrollOffset(scrollOffset, 150.0));
        Assert.Equal(1800.0, sliver.ComputeMaxScrollOffset(sliver.ConstraintsForSliver, 150.0));
    }

    [Theory]
    [InlineData(0.0, 0, 0)]
    [InlineData(45.0, 1, 1)]
    public void RenderSliverFixedExtentList_IgnoresTheDeprecatedItemExtentArgument(
        double scrollOffset,
        int expectedMinIndex,
        int expectedMaxIndex)
    {
        List<RenderBox> children =
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ];
        var manager = new ListChildManager(children);
        RenderSliverFixedExtentList sliver = manager.CreateFixedExtentList(30.0);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset(scrollOffset), cacheExtent: 100);
        pipeline.FlushLayout(new Size(800, 600));

        Assert.Equal(scrollOffset, sliver.ConstraintsForSliver.ScrollOffset);
        Assert.Equal(600.0, sliver.ConstraintsForSliver.ViewportMainAxisExtent);
        Assert.Equal(30.0, sliver.ItemExtent);
        Assert.Equal(300.0, sliver.IndexToLayoutOffset(150.0, 10));
        Assert.Equal(expectedMinIndex, sliver.GetMinChildIndexForScrollOffset(scrollOffset, 150.0));
        Assert.Equal(expectedMaxIndex, sliver.GetMaxChildIndexForScrollOffset(scrollOffset, 150.0));
        Assert.Equal(90.0, sliver.ComputeMaxScrollOffset(sliver.ConstraintsForSliver, 150.0));
    }

    [Fact]
    public void RenderSliverMultiBoxAdaptor_CountsLeadingAndTrailingGarbage()
    {
        List<RenderBox> children =
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ];
        var manager = new ListChildManager(children);
        RenderSliverFixedExtentList sliver = manager.CreateFixedExtentList(30.0);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset(), cacheExtent: 100);
        pipeline.FlushLayout(new Size(800, 600));

        // Keeping only the middle child of three leaves one leading and one trailing child.
        Assert.Equal(1, sliver.CalculateLeadingGarbage(firstIndex: 1));
        Assert.Equal(1, sliver.CalculateTrailingGarbage(lastIndex: 1));
    }

    [Fact]
    public void RenderSliverFixedExtentBoxAdaptor_SetsThePaintExtentFromTheItemExtent()
    {
        List<RenderBox> children = [new FixedSizeBox(new Size(400, 100))];
        var manager = new ListChildManager(children);
        RenderSliverFixedExtentList sliver = manager.CreateFixedExtentList(30.0);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset(), cacheExtent: 100);
        pipeline.FlushLayout(new Size(800, 600));

        // The children measure 100 tall, but a fixed-extent sliver reports the configured extent.
        Assert.Equal(30.0, sliver.PaintExtentOf(sliver.FirstChild!));
    }

    [DebugOnlyFact]
    public void RenderSliverFixedExtentBoxAdaptor_ReportsAScrollExtentThatIsNotAMultipleOfTheItemExtent()
    {
        using RenderErrorRethrowScope renderErrors = RenderErrorRethrowScope.Enter();
        var manager = new SingleChildManager();
        var sliver = new NonMultipleFixedExtentList(itemExtent: 100.0, totalExtent: 250.0, manager);
        manager.Setup(sliver, new FixedSizeBox(new Size(400, 100)));
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset());

        FlutterError error = Assert.Throws<FlutterError>(() => pipeline.FlushLayout(new Size(800, 600)));

        Assert.Contains("returned a value that is not an even multiple of its itemExtent", error.Message);
    }

    [DebugOnlyFact]
    public void RenderSliverFixedExtentBoxAdaptor_ToleratesAScrollExtentWithinPrecisionErrorTolerance()
    {
        using RenderErrorRethrowScope renderErrors = RenderErrorRethrowScope.Enter();
        var manager = new SingleChildManager();
        var sliver = new NonMultipleFixedExtentList(
            itemExtent: 100.0,
            totalExtent: 200.0000000000001,
            manager);
        manager.Setup(sliver, new FixedSizeBox(new Size(400, 100)));
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset());

        pipeline.FlushLayout(new Size(800, 600));

        Assert.Equal(200.0000000000001, sliver.Geometry.ScrollExtent);
    }

    [DebugOnlyFact]
    public void RenderSliverFixedExtentList_MarksNeedsLayoutOnlyWhenTheExtentChanges()
    {
        var manager = new ListChildManager([new FixedSizeBox(new Size(400, 100))]);
        RenderSliverFixedExtentList sliver = manager.CreateFixedExtentList(30.0);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset());
        pipeline.FlushLayout(new Size(800, 600));

        sliver.SetItemExtent(30.0);
        Assert.False(sliver.DebugNeedsLayout);

        sliver.SetItemExtent(44.0);
        Assert.True(sliver.DebugNeedsLayout);
        Assert.Equal(44.0, sliver.ItemExtent);
    }

    [DebugOnlyFact]
    public void RenderSliverVariedExtentList_MarksNeedsLayoutOnlyWhenTheBuilderChanges()
    {
        ItemExtentBuilder builder = (_, _) => 30.0;
        var manager = new ListChildManager([new FixedSizeBox(new Size(400, 100))]);
        var sliver = new RenderSliverVariedExtentList(builder, manager);
        manager.Attach(sliver);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset());
        pipeline.FlushLayout(new Size(800, 600));

        sliver.SetItemExtentBuilder(builder);
        Assert.False(sliver.DebugNeedsLayout);

        sliver.SetItemExtentBuilder((_, _) => 44.0);
        Assert.True(sliver.DebugNeedsLayout);
    }

    [Fact]
    public void RenderSliverVariedExtentList_ReadsEveryExtentFromTheBuilder()
    {
        var manager = new ListChildManager(
        [
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
            new FixedSizeBox(new Size(400, 100)),
        ]);
        ItemExtentBuilder builder = (index, _) => 100.0 + (index * 50);
        var sliver = new RenderSliverVariedExtentList(builder, manager);
        manager.Attach(sliver);
        PipelineOwner pipeline = Harness(sliver, new TestViewportOffset());
        pipeline.FlushLayout(new Size(800, 600));

        Assert.Null(sliver.ItemExtent);
        Assert.Same(builder, sliver.ItemExtentBuilder);
        Assert.Equal([100.0, 150.0, 200.0], Children(sliver).Select(child => child.Size.Height));
        Assert.Equal([0.0, 100.0, 250.0], Children(sliver).Select(
            child => ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset));
        Assert.Equal(450.0, sliver.Geometry.ScrollExtent);
        Assert.Equal(150.0, sliver.PaintExtentOf(sliver.ChildAfter(sliver.FirstChild!)!));

        ItemExtentBuilder replacement = (_, _) => 60.0;
        sliver.SetItemExtentBuilder(replacement);
        Assert.Same(replacement, sliver.ItemExtentBuilder);
        Assert.Throws<ArgumentNullException>(() => sliver.SetItemExtentBuilder(null!));
    }

    private static IEnumerable<RenderBox> Children(RenderSliverMultiBoxAdaptor sliver)
    {
        for (RenderBox? child = sliver.FirstChild; child is not null; child = sliver.ChildAfter(child))
        {
            yield return child;
        }
    }

    private static PipelineOwner Harness(RenderSliver sliver, ViewportOffset offset, double? cacheExtent = null)
    {
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: cacheExtent is null ? null : ScrollCacheExtent.Pixels(cacheExtent.Value),
            children: [sliver]);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        return pipeline;
    }

    /// <summary>Flutter's <c>RenderSizedBox</c> test double.</summary>
    private sealed class FixedSizeBox : RenderBox
    {
        private readonly Size _size;

        public FixedSizeBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>Flutter's <c>NonMultipleFixedExtentList</c>: reports a bogus scroll extent.</summary>
    private sealed class NonMultipleFixedExtentList : RenderSliverFixedExtentList
    {
        private readonly double _totalExtent;

        public NonMultipleFixedExtentList(
            double itemExtent,
            double totalExtent,
            IRenderSliverBoxChildManager childManager)
            : base(itemExtent, childManager)
        {
            _totalExtent = totalExtent;
        }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            base.PerformSliverLayout(constraints);
            Geometry = Geometry with { ScrollExtent = _totalExtent, MaxPaintExtent = _totalExtent };
        }
    }

    /// <summary>Flutter's <c>TestRenderSliverBoxChildManager</c>: hands out a fixed child list.</summary>
    private sealed class ListChildManager : IRenderSliverBoxChildManager
    {
        private readonly List<RenderBox> _children;
        private int? _currentlyUpdatingChildIndex;
        private RenderSliverMultiBoxAdaptor? _renderObject;

        public ListChildManager(List<RenderBox> children)
        {
            _children = children;
        }

        public int ChildCount => _children.Count;

        public int? EstimatedChildCount => ChildCount;

        /// <remarks>
        /// The body Flutter's own `RenderSliverBoxChildManager` test doubles use.
        /// </remarks>
        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null)
        {
            Assert.True(lastIndex >= firstIndex);
            return ChildCount
                   * (trailingScrollOffset!.Value - leadingScrollOffset!.Value)
                   / (lastIndex!.Value - firstIndex!.Value + 1);
        }

        public RenderSliverFillViewport CreateFillViewport()
        {
            var sliver = new RenderSliverFillViewport(childManager: this);
            _renderObject = sliver;
            return sliver;
        }

        public RenderSliverFixedExtentList CreateFixedExtentList(double itemExtent)
        {
            var sliver = new RenderSliverFixedExtentList(itemExtent, this);
            _renderObject = sliver;
            return sliver;
        }

        public void Attach(RenderSliverMultiBoxAdaptor renderObject)
        {
            _renderObject = renderObject;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= _children.Count)
            {
                return;
            }

            try
            {
                _currentlyUpdatingChildIndex = index;
                _renderObject!.Insert(_children[index], after);
            }
            finally
            {
                _currentlyUpdatingChildIndex = null;
            }

            return;
        }

        public void RemoveChild(RenderBox child) => _renderObject!.Remove(child);

        public void DidAdoptChild(RenderBox child)
        {
            if (_currentlyUpdatingChildIndex is not null
                && child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.Index = _currentlyUpdatingChildIndex.Value;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }

    /// <summary>Flutter's <c>TestChildManagerSimple</c>: one child, always at index zero.</summary>
    private sealed class SingleChildManager : IRenderSliverBoxChildManager
    {
        private RenderBox? _child;
        private RenderSliverMultiBoxAdaptor? _renderObject;

        public int ChildCount => 1;

        public int? EstimatedChildCount => ChildCount;

        /// <remarks>
        /// The body Flutter's own `RenderSliverBoxChildManager` test doubles use.
        /// </remarks>
        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null)
        {
            Assert.True(lastIndex >= firstIndex);
            return ChildCount
                   * (trailingScrollOffset!.Value - leadingScrollOffset!.Value)
                   / (lastIndex!.Value - firstIndex!.Value + 1);
        }

        public void Setup(RenderSliverMultiBoxAdaptor renderObject, RenderBox child)
        {
            _renderObject = renderObject;
            _child = child;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index != 0 || _child is null)
            {
                return;
            }

            _renderObject!.Insert(_child, after);
            return;
        }

        public void RemoveChild(RenderBox child)
        {
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.Index = 0;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }
}
