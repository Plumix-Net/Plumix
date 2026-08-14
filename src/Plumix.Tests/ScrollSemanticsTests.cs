using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/scrollable.dart (_ScrollSemantics, _RenderScrollSemantics)
// flutter/packages/flutter/lib/src/rendering/viewport.dart (useTwoPaneSemantics, excludeFromScrolling)
// flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart
// flutter/packages/flutter/lib/src/widgets/pinned_header_sliver.dart
// flutter/packages/flutter/lib/src/widgets/scroll_delegate.dart (semantic indexes)
// Mirrors flutter/packages/flutter/test/widgets/scrollable_semantics_test.dart,
// sliver_semantics_test.dart and pinned_header_sliver_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollSemanticsTests
{
    private const double ItemHeight = 100.0;
    private static readonly Size Surface = new(300, 400);

    [Fact]
    public void ScrollableExposesTheCorrectSemanticActions()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.True(scrolling.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling), harness.SemanticsDump);
        Assert.True(scrolling.Actions.HasFlag(SemanticsActions.ScrollUp), harness.SemanticsDump);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollDown), harness.SemanticsDump);
        Assert.True(scrolling.Actions.HasFlag(SemanticsActions.ScrollToOffset), harness.SemanticsDump);

        controller.JumpTo(500.0);
        harness.Pump(Surface);
        scrolling = RequireScrollingNode(harness);
        Assert.True(scrolling.Actions.HasFlag(SemanticsActions.ScrollUp), harness.SemanticsDump);
        Assert.True(scrolling.Actions.HasFlag(SemanticsActions.ScrollDown), harness.SemanticsDump);

        controller.JumpTo(controller.Position.MaxScrollExtent);
        harness.Pump(Surface);
        scrolling = RequireScrollingNode(harness);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollUp), harness.SemanticsDump);
        Assert.True(scrolling.Actions.HasFlag(SemanticsActions.ScrollDown), harness.SemanticsDump);
    }

    [Fact]
    public void VerticalScrollableRespondsToScrollToOffset()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.True(harness.PerformSemanticsAction(
            scrolling.Id,
            SemanticsActions.ScrollToOffset,
            new Point(123.0, 456.0)));

        Assert.Equal(456.0, controller.Offset);
    }

    [Fact]
    public void HorizontalScrollableRespondsToScrollToOffset()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(
            BuildList(controller, itemCount: 20, axis: Axis.Horizontal));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.True(harness.PerformSemanticsAction(
            scrolling.Id,
            SemanticsActions.ScrollToOffset,
            new Point(123.0, 456.0)));

        Assert.Equal(123.0, controller.Offset);
    }

    [Fact]
    public void UnscrollableScrollableDoesNotExposeScrollToOffset()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 2));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollToOffset), harness.SemanticsDump);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollUp), harness.SemanticsDump);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollDown), harness.SemanticsDump);
    }

    [Fact]
    public void ScrollToOffsetRespectsImplicitScrollingConfiguration()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(
            BuildList(controller, itemCount: 20, physics: new NeverScrollableScrollPhysics()));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.False(scrolling.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling), harness.SemanticsDump);
        Assert.False(scrolling.Actions.HasFlag(SemanticsActions.ScrollToOffset), harness.SemanticsDump);
    }

    [Fact]
    public void ScrollingNodeReportsScrollProgress()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.Equal(0.0, scrolling.ScrollExtentMin);
        Assert.Equal(0.0, scrolling.ScrollPosition);
        Assert.Equal(20 * ItemHeight - Surface.Height, scrolling.ScrollExtentMax);

        controller.JumpTo(394.3);
        harness.Pump(Surface);
        Assert.Equal(394.3, RequireScrollingNode(harness).ScrollPosition);
    }

    [Fact]
    public void ScrollingNodeReportsChildCountAndFirstVisibleIndex()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        Assert.Equal(20, RequireScrollingNode(harness).ScrollChildCount);
        Assert.Equal(0, RequireScrollingNode(harness).ScrollIndex);

        controller.JumpTo(5 * ItemHeight);
        harness.Pump(Surface);
        Assert.Equal(5, RequireScrollingNode(harness).ScrollIndex);
    }

    [Fact]
    public void SemanticIndexOffsetShiftsTheReportedIndexes()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                slivers: [
                    SliverFixedExtentList.Builder(
                        20,
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        ItemHeight,
                        semanticIndexOffset: 5)
                ])));
        harness.Pump(Surface);

        Assert.Equal(5, RequireScrollingNode(harness).ScrollIndex);
    }

    [Fact]
    public void SeparatedListGivesSeparatorsNoSemanticIndex()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(new Directionality(
            TextDirection.Ltr,
            ListView.Separated(
                itemCount: 10,
                itemBuilder: static (_, index) => new Semantics(label: $"item {index}", container: true),
                separatorBuilder: static (_, _) => new Semantics(label: "separator", container: true),
                itemExtent: ItemHeight,
                controller: controller)));
        harness.Pump(Surface);

        // Item 2 sits at index 4 of the delegate; only the items carry an index, so it reports 2.
        controller.JumpTo(4 * ItemHeight);
        harness.Pump(Surface);
        Assert.Equal(2, RequireScrollingNode(harness).ScrollIndex);
        Assert.Equal(10, RequireScrollingNode(harness).ScrollChildCount);
    }

    [Fact]
    public void SemanticScrollDownRunsADragThroughThePhysics()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        // At the leading edge only the forward action is offered; scrolling "up" moves the content up,
        // which advances the offset by 0.8 of the viewport extent through the ordinary drag pipeline.
        SemanticsNode scrolling = RequireScrollingNode(harness);
        Assert.True(harness.PerformSemanticsAction(scrolling.Id, SemanticsActions.ScrollUp));

        Assert.Equal(0.8 * Surface.Height, controller.Offset, 3);
    }

    [Fact]
    public void PinnedHeaderIsASiblingOfTheScrollingNode()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                slivers: [
                    new SliverPersistentHeader(new TestHeaderDelegate(), pinned: true),
                    SliverFixedExtentList.Builder(
                        20,
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        ItemHeight)
                ])));
        harness.Pump(Surface);

        SemanticsNode outer = RequireOuterNode(harness);
        SemanticsNode scrolling = RequireScrollingNode(harness);
        SemanticsNode header = Require(harness, "header");

        // The header hangs off the outer node next to the scrolling node, never inside it.
        Assert.Contains(header, outer.Children);
        Assert.DoesNotContain(header, scrolling.Children);
        Assert.True(header.IsTagged(RenderViewport.ExcludeFromScrolling), harness.SemanticsDump);
        Assert.True(header.IsTagged(RenderViewport.UseTwoPaneSemantics), harness.SemanticsDump);
        Assert.Same(scrolling, outer.Children[0]);

        // Items stay inside the scrolling pane.
        Assert.Contains(Require(harness, "item 0"), scrolling.Children);
    }

    [Fact]
    public void PinnedHeaderSliverTagsOnlyOnceItIsPartiallyScrolledOut()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                slivers: [
                    new PinnedHeaderSliver(new Semantics(label: "header", container: true, child:
                        new SizedBox(height: 60, width: 300))),
                    SliverFixedExtentList.Builder(
                        20,
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        ItemHeight)
                ])));
        harness.Pump(Surface);

        // The sliver's own `Semantics(container: true)` wrapper is the node the tag lands on, exactly
        // as Flutter's own test asserts through `semanticNode.parent.tags`.
        Assert.False(
            RequireHeaderContainer(harness).IsTagged(RenderViewport.ExcludeFromScrolling),
            harness.SemanticsDump);

        controller.JumpTo(20.0);
        harness.Pump(Surface);
        Assert.True(
            RequireHeaderContainer(harness).IsTagged(RenderViewport.ExcludeFromScrolling),
            harness.SemanticsDump);

        // Once tagged it leaves the scrolling pane entirely.
        Assert.Contains(RequireHeaderContainer(harness), RequireOuterNode(harness).Children);
    }

    [Fact]
    public void ChildrenInsideTheCacheExtentAreHiddenRatherThanDropped()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(BuildList(controller, itemCount: 20));
        harness.Pump(Surface);

        // The default 250px cache extent keeps two more items alive below the viewport.
        SemanticsNode onScreen = Require(harness, "item 3");
        SemanticsNode cached = Require(harness, "item 5");
        Assert.False(onScreen.IsHidden, harness.SemanticsDump);
        Assert.True(cached.IsHidden, harness.SemanticsDump);

        // A hidden child never becomes the reported first visible index.
        Assert.Equal(0, RequireScrollingNode(harness).ScrollIndex);
    }

    [Fact]
    public void ExcludeFromSemanticsProducesNoScrollingNode()
    {
        var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(new Directionality(
            TextDirection.Ltr,
            new Scrollable(
                controller: controller,
                excludeFromSemantics: true,
                slivers: [
                    SliverFixedExtentList.Builder(
                        20,
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        ItemHeight)
                ])));
        harness.Pump(Surface);

        Assert.Null(FindScrollingNode(harness.SemanticsRoot));
        Assert.NotNull(Require(harness, "item 0"));
    }

    private static Widget BuildList(
        ScrollController controller,
        int itemCount,
        Axis axis = Axis.Vertical,
        ScrollPhysics? physics = null)
    {
        return new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                scrollDirection: axis,
                physics: physics,
                semanticChildCount: itemCount,
                slivers: [
                    SliverFixedExtentList.Builder(
                        itemCount,
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        ItemHeight)
                ]));
    }

    /// <summary>The inner pane: the node that owns the scroll metrics and the scroll actions.</summary>
    private static SemanticsNode RequireScrollingNode(ScrollSemanticsHarness harness)
    {
        return FindScrollingNode(harness.SemanticsRoot)
               ?? throw new InvalidOperationException($"No scrolling node.\n{harness.SemanticsDump}");
    }

    /// <summary>The outer pane: the parent of the scrolling node.</summary>
    private static SemanticsNode RequireOuterNode(ScrollSemanticsHarness harness)
    {
        SemanticsNode scrolling = RequireScrollingNode(harness);
        return FindParent(harness.SemanticsRoot, scrolling)
               ?? throw new InvalidOperationException($"No outer node.\n{harness.SemanticsDump}");
    }

    private static SemanticsNode RequireHeaderContainer(ScrollSemanticsHarness harness)
    {
        return FindParent(harness.SemanticsRoot, Require(harness, "header"))
               ?? throw new InvalidOperationException($"No header container.\n{harness.SemanticsDump}");
    }

    private static SemanticsNode Require(ScrollSemanticsHarness harness, string label)
    {
        return harness.FindSemanticsNode(label)
               ?? throw new InvalidOperationException($"No node labelled '{label}'.\n{harness.SemanticsDump}");
    }

    private static SemanticsNode? FindScrollingNode(SemanticsNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling) || node.ScrollPosition.HasValue)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindScrollingNode(child) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private static SemanticsNode? FindParent(SemanticsNode? node, SemanticsNode target)
    {
        if (node is null)
        {
            return null;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (ReferenceEquals(child, target))
            {
                return node;
            }

            if (FindParent(child, target) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class TestHeaderDelegate : SliverPersistentHeaderDelegate
    {
        public override double MinExtent => 56.0;

        public override double MaxExtent => 56.0;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new Semantics(label: "header", container: true, child: new SizedBox(height: 56, width: 300));
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }
}
