using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/grid_view_test.dart,
// flutter/packages/flutter/test/widgets/grid_view_layout_test.dart,
// flutter/packages/flutter/test/widgets/scrollable_grid_test.dart and the RenderSliverGrid case of
// flutter/packages/flutter/test/rendering/sliver_cache_test.dart, plus direct coverage of
// rendering/sliver_grid.dart's layout math, asserts and delegate protocol.

namespace Plumix.Tests;

public sealed class SliverGridTests
{
    private static readonly string[] KStates =
    [
        "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut",
        "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas",
        "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota",
        "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey",
        "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon",
        "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah",
        "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming",
    ];

    private static readonly Color Green = new(0xFF00FF00);

    // ---------- rendering/sliver_grid.dart ----------

    [Fact]
    public void SliverGridGeometry_TrailingScrollOffsetAndToString()
    {
        var geometry = new SliverGridGeometry(
            scrollOffset: 100.0,
            crossAxisOffset: 25.5,
            mainAxisExtent: 50.0,
            crossAxisExtent: 200.0);

        Assert.Equal(150.0, geometry.TrailingScrollOffset);
        Assert.Equal(
            "SliverGridGeometry(scrollOffset: 100.0, crossAxisOffset: 25.5, mainAxisExtent: 50.0, "
            + "crossAxisExtent: 200.0)",
            geometry.ToString());
    }

    [Theory]
    [InlineData(AxisDirection.Down, 200.0, 50.0)]
    [InlineData(AxisDirection.Right, 50.0, 200.0)]
    public void SliverGridGeometry_GetBoxConstraints_IsTightOnBothAxes(
        AxisDirection axisDirection,
        double width,
        double height)
    {
        var geometry = new SliverGridGeometry(0.0, 0.0, mainAxisExtent: 50.0, crossAxisExtent: 200.0);

        BoxConstraints constraints = geometry.GetBoxConstraints(Constraints(axisDirection: axisDirection));

        Assert.Equal(BoxConstraints.Tight(new Size(width, height)), constraints);
    }

    [Fact]
    public void SliverGridRegularTileLayout_IndexesAndGeometry()
    {
        var layout = new SliverGridRegularTileLayout(
            crossAxisCount: 3,
            mainAxisStride: 110.0,
            crossAxisStride: 210.0,
            childMainAxisExtent: 100.0,
            childCrossAxisExtent: 200.0,
            reverseCrossAxis: false);

        Assert.Equal(0, layout.GetMinChildIndexForScrollOffset(0.0));
        Assert.Equal(3, layout.GetMinChildIndexForScrollOffset(110.0));
        Assert.Equal(3, layout.GetMinChildIndexForScrollOffset(219.9));
        Assert.Equal(0, layout.GetMaxChildIndexForScrollOffset(0.0));
        Assert.Equal(2, layout.GetMaxChildIndexForScrollOffset(1.0));
        Assert.Equal(5, layout.GetMaxChildIndexForScrollOffset(220.0));

        SliverGridGeometry geometry = layout.GetGeometryForChildIndex(5);
        Assert.Equal(110.0, geometry.ScrollOffset);
        Assert.Equal(420.0, geometry.CrossAxisOffset);
        Assert.Equal(100.0, geometry.MainAxisExtent);
        Assert.Equal(200.0, geometry.CrossAxisExtent);

        // The trailing main-axis spacing is not part of the scroll extent.
        Assert.Equal(0.0, layout.ComputeMaxScrollOffset(0));
        Assert.Equal(100.0, layout.ComputeMaxScrollOffset(3));
        Assert.Equal(210.0, layout.ComputeMaxScrollOffset(4));
    }

    [Fact]
    public void SliverGridRegularTileLayout_ReverseCrossAxis_MirrorsTheCrossAxisOffsets()
    {
        var layout = new SliverGridRegularTileLayout(
            crossAxisCount: 4,
            mainAxisStride: 194.0,
            crossAxisStride: 202.0,
            childMainAxisExtent: 194.0,
            childCrossAxisExtent: 194.0,
            reverseCrossAxis: true);

        Assert.Equal(606.0, layout.GetGeometryForChildIndex(0).CrossAxisOffset);
        Assert.Equal(404.0, layout.GetGeometryForChildIndex(1).CrossAxisOffset);
        Assert.Equal(0.0, layout.GetGeometryForChildIndex(3).CrossAxisOffset);
        Assert.Equal(606.0, layout.GetGeometryForChildIndex(4).CrossAxisOffset);
    }

    [DebugOnlyFact]
    public void SliverGridRegularTileLayout_AssertsItsArguments()
    {
        Assert.Contains(
            "crossAxisCount > 0",
            Assert.Throws<AssertionError>(() => new SliverGridRegularTileLayout(0, 1, 1, 1, 1, false)).Message);
        Assert.Contains(
            "mainAxisStride >= 0",
            Assert.Throws<AssertionError>(() => new SliverGridRegularTileLayout(1, -1, 1, 1, 1, false)).Message);
        Assert.Contains(
            "crossAxisStride >= 0",
            Assert.Throws<AssertionError>(() => new SliverGridRegularTileLayout(1, 1, -1, 1, 1, false)).Message);
        Assert.Contains(
            "childMainAxisExtent >= 0",
            Assert.Throws<AssertionError>(() => new SliverGridRegularTileLayout(1, 1, 1, -1, 1, false)).Message);
        Assert.Contains(
            "childCrossAxisExtent >= 0",
            Assert.Throws<AssertionError>(() => new SliverGridRegularTileLayout(1, 1, 1, 1, -1, false)).Message);
    }

    /// <remarks>grid_view_test.dart: "SliverGridRegularTileLayout - can handle close to zero
    /// mainAxisStride".</remarks>
    [Fact]
    public void SliverGridRegularTileLayout_CanHandleCloseToZeroMainAxisStride()
    {
        var @delegate = new SliverGridDelegateWithMaxCrossAxisExtent(
            childAspectRatio: 1e300,
            maxCrossAxisExtent: 500.0);
        SliverGridLayout layout = @delegate.GetLayout(new SliverConstraints(
            AxisDirection: AxisDirection.Down,
            GrowthDirection: GrowthDirection.Forward,
            UserScrollDirection: ScrollDirection.Forward,
            ScrollOffset: 100.0,
            PrecedingScrollExtent: 0.0,
            Overlap: 0.0,
            RemainingPaintExtent: 0.0,
            CrossAxisExtent: 500,
            CrossAxisDirection: AxisDirection.Right,
            ViewportMainAxisExtent: 100.0,
            RemainingCacheExtent: 0.0,
            CacheOrigin: 0.0));

        Assert.Equal(0, layout.GetMinChildIndexForScrollOffset(1000.0));
    }

    [Theory]
    [InlineData(AxisDirection.Right, false)]
    [InlineData(AxisDirection.Left, true)]
    public void GridDelegates_ReverseTheCrossAxisForAReversedCrossAxisDirection(
        AxisDirection crossAxisDirection,
        bool reversed)
    {
        SliverConstraints constraints = Constraints(crossAxisDirection: crossAxisDirection);

        var fixedCount = (SliverGridRegularTileLayout)new SliverGridDelegateWithFixedCrossAxisCount(4)
            .GetLayout(constraints);
        var maxExtent = (SliverGridRegularTileLayout)new SliverGridDelegateWithMaxCrossAxisExtent(200.0)
            .GetLayout(constraints);

        Assert.Equal(reversed, fixedCount.ReverseCrossAxis);
        Assert.Equal(reversed, maxExtent.ReverseCrossAxis);
    }

    [Fact]
    public void GridDelegateWithFixedCrossAxisCount_GetLayout_SplitsTheCrossAxis()
    {
        var @delegate = new SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 4,
            mainAxisSpacing: 10.0,
            crossAxisSpacing: 8.0,
            childAspectRatio: 2.0);

        var layout = (SliverGridRegularTileLayout)@delegate.GetLayout(Constraints());

        Assert.Equal(4, layout.CrossAxisCount);
        Assert.Equal(194.0, layout.ChildCrossAxisExtent);
        Assert.Equal(97.0, layout.ChildMainAxisExtent);
        Assert.Equal(202.0, layout.CrossAxisStride);
        Assert.Equal(107.0, layout.MainAxisStride);

        var withExtent = (SliverGridRegularTileLayout)new SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 4,
            childAspectRatio: 2.0,
            mainAxisExtent: 30.0).GetLayout(Constraints());
        Assert.Equal(30.0, withExtent.ChildMainAxisExtent);
    }

    [Fact]
    public void GridDelegateWithMaxCrossAxisExtent_GetLayout_RoundsTheCountUpToAtLeastOneColumn()
    {
        var layout = (SliverGridRegularTileLayout)new SliverGridDelegateWithMaxCrossAxisExtent(
            maxCrossAxisExtent: 300.0).GetLayout(Constraints());
        Assert.Equal(3, layout.CrossAxisCount);
        Assert.Equal(800.0 / 3.0, layout.ChildCrossAxisExtent);

        var narrow = (SliverGridRegularTileLayout)new SliverGridDelegateWithMaxCrossAxisExtent(
            maxCrossAxisExtent: 300.0,
            crossAxisSpacing: 10.0).GetLayout(Constraints(crossAxisExtent: 1.0));
        Assert.Equal(1, narrow.CrossAxisCount);
        Assert.Equal(1.0, narrow.ChildCrossAxisExtent);
    }

    [Fact]
    public void GridDelegates_ShouldRelayout_ComparesEveryField()
    {
        var fixedCount = new SliverGridDelegateWithFixedCrossAxisCount(2, 1.0, 2.0, 3.0, 4.0);
        Assert.False(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(2, 1.0, 2.0, 3.0, 4.0)));
        Assert.True(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(3, 1.0, 2.0, 3.0, 4.0)));
        Assert.True(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(2, 1.5, 2.0, 3.0, 4.0)));
        Assert.True(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(2, 1.0, 2.5, 3.0, 4.0)));
        Assert.True(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(2, 1.0, 2.0, 3.5, 4.0)));
        Assert.True(fixedCount.ShouldRelayout(new SliverGridDelegateWithFixedCrossAxisCount(2, 1.0, 2.0, 3.0)));

        var maxExtent = new SliverGridDelegateWithMaxCrossAxisExtent(20.0, 1.0, 2.0, 3.0, 4.0);
        Assert.False(maxExtent.ShouldRelayout(new SliverGridDelegateWithMaxCrossAxisExtent(20.0, 1.0, 2.0, 3.0, 4.0)));
        Assert.True(maxExtent.ShouldRelayout(new SliverGridDelegateWithMaxCrossAxisExtent(21.0, 1.0, 2.0, 3.0, 4.0)));
        Assert.True(maxExtent.ShouldRelayout(new SliverGridDelegateWithMaxCrossAxisExtent(20.0, 1.0, 2.0, 3.0, 5.0)));
    }

    [DebugOnlyFact]
    public void GridDelegates_AssertTheirArguments()
    {
        Assert.Contains(
            "crossAxisCount > 0",
            Assert.Throws<AssertionError>(() => new SliverGridDelegateWithFixedCrossAxisCount(0)).Message);
        Assert.Contains(
            "mainAxisSpacing >= 0",
            Assert.Throws<AssertionError>(
                () => new SliverGridDelegateWithFixedCrossAxisCount(1, mainAxisSpacing: -1)).Message);
        Assert.Contains(
            "crossAxisSpacing >= 0",
            Assert.Throws<AssertionError>(
                () => new SliverGridDelegateWithMaxCrossAxisExtent(1, crossAxisSpacing: -1)).Message);
        Assert.Contains(
            "childAspectRatio > 0",
            Assert.Throws<AssertionError>(
                () => new SliverGridDelegateWithMaxCrossAxisExtent(1, childAspectRatio: 0)).Message);
    }

    [Fact]
    public void SliverGridParentData_ToString_PrintsTheNullableCrossAxisOffset()
    {
        Assert.Equal("crossAxisOffset=null; index=null; layoutOffset=None", new SliverGridParentData().ToString());
        Assert.Equal(
            "crossAxisOffset=12.5; index=2; layoutOffset=100.0",
            new SliverGridParentData { CrossAxisOffset = 12.5, Index = 2, LayoutOffset = 100.0 }.ToString());
    }

    [Fact]
    public void RenderSliverGrid_GridDelegate_RelayoutsOnlyWhenTheDelegateAsksTo()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(4));
        RenderSliverGrid grid = manager.CreateRenderSliverGrid();
        var viewport = new RenderViewport(
            offset: ViewportOffset.Zero(),
            crossAxisDirection: AxisDirection.Right,
            children: [grid]);
        PipelineOwner pipeline = Layout(viewport);
        Assert.False(grid.DebugNeedsLayout);

        var same = new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, childAspectRatio: 4.0);
        grid.GridDelegate = same;
        Assert.Same(same, grid.GridDelegate);
        Assert.False(grid.DebugNeedsLayout);

        grid.GridDelegate = new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3, childAspectRatio: 4.0);
        Assert.True(grid.DebugNeedsLayout);
        pipeline.FlushLayout(new Size(800, 600));

        // A different delegate type always relayouts, without asking the new delegate.
        grid.GridDelegate = new SliverGridDelegateWithMaxCrossAxisExtent(maxCrossAxisExtent: 800.0 / 3.0);
        Assert.True(grid.DebugNeedsLayout);
    }

    /// <remarks>sliver_cache_test.dart: "RenderSliverGrid calculates correct geometry".</remarks>
    [Fact]
    public void RenderSliverGrid_CalculatesCorrectGeometry()
    {
        // Viewport is 800x600, each grid element is 400x100, giving us space for 12 visible children.
        List<RenderBox> children = SizedChildren(60);
        var manager = new TestRenderSliverBoxChildManager(children);
        RenderSliverGrid inner = manager.CreateRenderSliverGrid();
        var root = new RenderViewport(
            offset: ViewportOffset.Zero(),
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(250.0),
            children: [inner]);
        PipelineOwner pipeline = Layout(root);

        ExpectSliverConstraints(inner, cacheOrigin: 0.0, remainingPaintExtent: 600.0,
            remainingCacheExtent: 600.0 + 250.0, scrollOffset: 0.0);
        ExpectSliverGeometry(inner, paintExtent: 600.0, cacheExtent: 850.0, visible: true);
        Assert.All(children[..18], r => Assert.True(r.Attached));
        Assert.DoesNotContain(children[18..], r => r.Attached);

        // Scroll half an item down.
        root.Offset = ViewportOffset.Fixed(50.0);
        pipeline.FlushLayout(new Size(800, 600));

        ExpectSliverConstraints(inner, cacheOrigin: -50.0, remainingPaintExtent: 600.0,
            remainingCacheExtent: 50.0 + 600.0 + 250.0, scrollOffset: 50.0);
        ExpectSliverGeometry(inner, paintExtent: 600.0, cacheExtent: 900.0, visible: true);
        Assert.All(children[..18], r => Assert.True(r.Attached));
        Assert.DoesNotContain(children[18..], r => r.Attached);

        // Scroll to the middle.
        root.Offset = ViewportOffset.Fixed(1500.0);
        pipeline.FlushLayout(new Size(800, 600));

        ExpectSliverConstraints(inner, cacheOrigin: -250.0, remainingPaintExtent: 600.0,
            remainingCacheExtent: 250.0 + 600.0 + 250.0, scrollOffset: 1500.0);
        ExpectSliverGeometry(inner, paintExtent: 600.0, cacheExtent: 1100.0, visible: true);
        Assert.DoesNotContain(children[..24], r => r.Attached);
        Assert.All(children[24..48], r => Assert.True(r.Attached));
        Assert.DoesNotContain(children[48..], r => r.Attached);

        // Scroll to the end.
        root.Offset = ViewportOffset.Fixed(2400.0);
        pipeline.FlushLayout(new Size(800, 600));

        ExpectSliverConstraints(inner, cacheOrigin: -250.0, remainingPaintExtent: 600.0,
            remainingCacheExtent: 250.0 + 600.0 + 250.0, scrollOffset: 2400.0);
        ExpectSliverGeometry(inner, paintExtent: 600.0, cacheExtent: 850.0, visible: true);
        Assert.DoesNotContain(children[..42], r => r.Attached);
        Assert.All(children[42..], r => Assert.True(r.Attached));
    }

    [Fact]
    public void RenderSliverGrid_PastTheEnd_ReportsTheLayoutExtentOfEveryChild()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverGrid inner = manager.CreateRenderSliverGrid();
        var root = new RenderViewport(
            offset: ViewportOffset.Fixed(1000.0),
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0.0),
            children: [inner, new RenderSliverToBoxAdapter(new RenderConstrainedBox(
                BoxConstraints.Tight(new Size(800, 2000))))]);
        Layout(root);

        // Five children at two per 100px row: three rows.
        Assert.Equal(300.0, inner.Geometry!.ScrollExtent);
        Assert.Equal(300.0, inner.Geometry.MaxPaintExtent);
        Assert.Equal(0.0, inner.Geometry.PaintExtent);
        Assert.DoesNotContain(manager.Children, r => r.Attached);
    }

    [Fact]
    public void RenderSliverGrid_ReportsVisualOverflowUnderAnOverlap()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(2));
        RenderSliverGrid inner = manager.CreateRenderSliverGrid();
        var pinned = new RenderSliverPinnedBox(20.0);
        var root = new RenderViewport(
            offset: ViewportOffset.Zero(),
            crossAxisDirection: AxisDirection.Right,
            children: [pinned, inner]);
        Layout(root);

        // One 100px row fits the paint extent; only the overlap reports the overflow.
        Assert.Equal(0.0, inner.Constraints.ScrollOffset);
        Assert.NotEqual(0.0, inner.Constraints.Overlap);
        Assert.Equal(100.0, inner.Geometry!.PaintExtent);
        Assert.True(inner.Geometry.HasVisualOverflow);
    }

    // ---------- widgets/grid_view_test.dart ----------

    [Fact]
    public void GridViewBuilder_RespectsFindChildIndexCallback()
    {
        using var tester = new FrameworkDartTester();
        bool finderCalled = false;
        int itemCount = 7;
        StateSetter? stateSetter = null;

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new StatefulBuilder((_, setState) =>
            {
                stateSetter = setState;
                return GridView.Builder(
                    itemCount: itemCount,
                    itemBuilder: (_, index) => new Container(key: new ValueKey<string>($"{index}"), height: 2000.0),
                    findChildIndexCallback: _ =>
                    {
                        finderCalled = true;
                        return null;
                    },
                    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4));
            })));
        Assert.False(finderCalled);

        // Trigger update.
        stateSetter!(() => itemCount = 77);
        tester.Pump();

        Assert.True(finderCalled);
    }

    [Fact]
    public void EmptyGridView()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Count(dragStartBehavior: DragStartBehavior.Down, crossAxisCount: 4)));
    }

    [Fact]
    public void GridViewCount_ControlTest()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Count(
                dragStartBehavior: DragStartBehavior.Down,
                crossAxisCount: 4,
                children: [.. KStates.Select(state => (Widget)StateTile(state, log))])));

        Assert.Equal(new Size(200.0, 200.0), tester.GetSize(FindText(tester, "Arkansas").Single()));

        for (int i = 0; i < 8; ++i)
        {
            tester.Tap(FindText(tester, KStates[i]).Single());
            Assert.Equal([KStates[i]], log);
            log.Clear();
        }

        Assert.Empty(FindText(tester, KStates[12]));
        Assert.Empty(FindText(tester, "Nevada"));

        tester.Drag(FindText(tester, "Arkansas").Single(), new Vector(0.0, -200.0));
        tester.Pump();

        for (int i = 0; i < 4; ++i)
        {
            Assert.Empty(FindText(tester, KStates[i]));
        }

        for (int i = 4; i < 12; ++i)
        {
            tester.Tap(FindText(tester, KStates[i]).Single());
            Assert.Equal([KStates[i]], log);
            log.Clear();
        }

        tester.Drag(FindText(tester, "Delaware").Single(), new Vector(0.0, -4000.0));
        tester.Pump();

        Assert.Empty(FindText(tester, "Alabama"));
        Assert.Empty(FindText(tester, "Pennsylvania"));

        Assert.Equal(new Point(300.0, 100.0), tester.GetCenter(FindText(tester, "Tennessee").Single()));

        tester.Tap(FindText(tester, "Tennessee").Single());
        Assert.Equal(["Tennessee"], log);
        log.Clear();

        tester.Drag(FindText(tester, "Tennessee").Single(), new Vector(0.0, 200.0));
        tester.Pump();

        tester.Tap(FindText(tester, "Tennessee").Single());
        Assert.Equal(["Tennessee"], log);
        log.Clear();

        tester.Tap(FindText(tester, "Pennsylvania").Single());
        Assert.Equal(["Pennsylvania"], log);
        log.Clear();
    }

    [Fact]
    public void GridViewExtent_ControlTest()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Extent(
                dragStartBehavior: DragStartBehavior.Down,
                maxCrossAxisExtent: 200.0,
                children: [.. KStates.Select(state => (Widget)StateTile(state, log))])));

        Assert.Equal(new Size(200.0, 200.0), tester.GetSize(FindText(tester, "Arkansas").Single()));

        for (int i = 0; i < 8; ++i)
        {
            tester.Tap(FindText(tester, KStates[i]).Single());
            Assert.Equal([KStates[i]], log);
            log.Clear();
        }

        Assert.Empty(FindText(tester, "Nevada"));

        tester.Drag(FindText(tester, "Arkansas").Single(), new Vector(0.0, -4000.0));
        tester.Pump();

        Assert.Empty(FindText(tester, "Alabama"));

        Assert.Equal(new Point(300.0, 100.0), tester.GetCenter(FindText(tester, "Tennessee").Single()));

        tester.Tap(FindText(tester, "Tennessee").Single());
        Assert.Equal(["Tennessee"], log);
        log.Clear();
    }

    [Fact]
    public void GridView_LargeScrollJump()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<int>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Extent(
                scrollDirection: Axis.Horizontal,
                maxCrossAxisExtent: 200.0,
                childAspectRatio: 0.75,
                children: LoggingChildren(80, log))));

        Assert.Equal(new Size(200.0 / 0.75, 200.0), tester.GetSize(FindText(tester, "4").Single()));

        Assert.Equal(
            [
                0, 1, 2, // col 0
                3, 4, 5, // col 1
                6, 7, 8, // col 2
                9, 10, 11, // col 3 (in cached area)
            ],
            log);
        log.Clear();

        AssertFound(tester, 0, 9, 80);

        ScrollPosition position = tester.State<ScrollableState>().Position;
        position.JumpTo(3025.0);

        Assert.Empty(log);
        tester.Pump();

        Assert.Equal(
            [
                30, 31, 32, // col 10 (in cached area)
                33, 34, 35, // col 11
                36, 37, 38, // col 12
                39, 40, 41, // col 13
                42, 43, 44, // col 14
                45, 46, 47, // col 15 (in cached area)
            ],
            log);
        log.Clear();

        AssertFound(tester, 33, 45, 80);

        position.JumpTo(975.0);

        Assert.Empty(log);
        tester.Pump();

        Assert.Equal(
            [
                6, 7, 8, // col 2 (in cached area)
                9, 10, 11, // col 3
                12, 13, 14, // col 4
                15, 16, 17, // col 5
                18, 19, 20, // col 6
                21, 22, 23, // col 7 (in cached area)
            ],
            log);
        log.Clear();

        AssertFound(tester, 9, 21, 80);
    }

    [Fact]
    public void GridView_ChangeCrossAxisCount()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<int>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4),
                children: LoggingChildren(40, log))));

        Assert.Equal(new Size(200.0, 200.0), tester.GetSize(FindText(tester, "4").Single()));
        Assert.Equal([.. Enumerable.Range(0, 20)], log);
        AssertFound(tester, 0, 12, 40);
        log.Clear();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2),
                children: LoggingChildren(40, log))));

        Assert.Equal([.. Enumerable.Range(0, 20)], log);
        log.Clear();

        Assert.Equal(new Size(400.0, 400.0), tester.GetSize(FindText(tester, "3").Single()));
        Assert.Empty(FindText(tester, "4"));
    }

    [Fact]
    public void GridView_ChangeMaxChildCrossAxisExtent()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<int>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(maxCrossAxisExtent: 200.0),
                children: LoggingChildren(40, log))));

        Assert.Equal(new Size(200.0, 200.0), tester.GetSize(FindText(tester, "4").Single()));
        Assert.Equal([.. Enumerable.Range(0, 20)], log);
        AssertFound(tester, 0, 12, 40);
        log.Clear();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(maxCrossAxisExtent: 400.0),
                children: LoggingChildren(40, log))));

        Assert.Equal([.. Enumerable.Range(0, 20)], log);
        log.Clear();

        Assert.Equal(new Size(400.0, 400.0), tester.GetSize(FindText(tester, "3").Single()));
        Assert.Empty(FindText(tester, "4"));
    }

    [DebugOnlyFact]
    public void OneLineGridView_Paints()
    {
        using var tester = new FrameworkDartTester();
        var container = new Container(decoration: new BoxDecoration(Color: Green));

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(child: new SizedBox(
                height: 200.0,
                child: GridView.Count(
                    cacheExtent: 0.0,
                    crossAxisCount: 2,
                    children: [container, container, container, container])))));

        var context = new TestRecordingPaintingContext();
        RenderObject grid = tester.ElementOfType<GridView>().FindRenderObject()!;
        grid.Paint(context, default);

        // `paints..rect(color: green)..rect(color: green)` and not three of them.
        Assert.Equal(2, context.Calls.Count(call => call.Method == "drawRect" && call.Color == Green));
    }

    [Fact]
    public void GridView_InZeroContext()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(child: SizedBox.Shrink(child: GridView.Count(
                crossAxisCount: 4,
                children: TextChildren(20))))));

        Assert.Empty(FindText(tester, "0"));
        Assert.Empty(FindText(tester, "1"));
    }

    [Fact]
    public void GridView_InUnboundedContext()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(child: GridView.Count(
                crossAxisCount: 4,
                shrinkWrap: true,
                children: TextChildren(20)))));

        Assert.Single(FindText(tester, "0"));
        Assert.Single(FindText(tester, "19"));
    }

    [Fact]
    public void GridViewBuilder_ControlTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Builder(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4),
                shrinkWrap: true,
                itemCount: 20,
                itemBuilder: (_, index) => new Text($"{index}"))));

        Assert.Single(FindText(tester, "0"));
        Assert.Single(FindText(tester, "11"));
        Assert.Empty(FindText(tester, "12"));
    }

    [Fact]
    public void GridViewBuilder_WithUndefinedItemCount()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Builder(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4),
                shrinkWrap: true,
                itemBuilder: (_, index) => new Text($"{index}"))));

        Assert.Single(FindText(tester, "0"));
        Assert.Single(FindText(tester, "11"));
        tester.Drag(tester.ElementOfType<GridView>(), new Vector(0.0, -300.0));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Single(FindText(tester, "13"));
    }

    [Fact]
    public void GridView_CrossAxisLayout()
    {
        using var tester = new FrameworkDartTester();
        var target = new UniqueKey();

        Widget Build(TextDirection textDirection) => new Directionality(
            textDirection,
            GridView.Count(crossAxisCount: 4, children: [new Container(key: target)]));

        tester.PumpWidget(Build(TextDirection.Ltr));

        Assert.Equal(new Point(0, 0), tester.GetTopLeft(FindKey(tester, target).Single()));
        Assert.Equal(new Point(200.0, 200.0), tester.GetBottomRight(FindKey(tester, target).Single()));

        tester.PumpWidget(Build(TextDirection.Rtl));

        Assert.Equal(new Point(600.0, 0.0), tester.GetTopLeft(FindKey(tester, target).Single()));
        Assert.Equal(new Point(800.0, 200.0), tester.GetBottomRight(FindKey(tester, target).Single()));
    }

    /// <remarks>Regression test for https://github.com/flutter/flutter/issues/27151.</remarks>
    [Fact]
    public void GridView_CrossAxisSpacing()
    {
        using var tester = new FrameworkDartTester();
        var target = new UniqueKey();

        Widget Build(TextDirection textDirection) => new Directionality(
            textDirection,
            GridView.Count(crossAxisCount: 4, crossAxisSpacing: 8.0, children: [new Container(key: target)]));

        tester.PumpWidget(Build(TextDirection.Ltr));

        Assert.Equal(new Point(0, 0), tester.GetTopLeft(FindKey(tester, target).Single()));
        Assert.Equal(new Point(194.0, 194.0), tester.GetBottomRight(FindKey(tester, target).Single()));

        tester.PumpWidget(Build(TextDirection.Rtl));

        Assert.Equal(new Point(606.0, 0.0), tester.GetTopLeft(FindKey(tester, target).Single()));
        Assert.Equal(new Point(800.0, 194.0), tester.GetBottomRight(FindKey(tester, target).Single()));
    }

    [Fact]
    public void GridView_DoesNotCacheItemBuilderCalls()
    {
        using var tester = new FrameworkDartTester();
        var counters = new Dictionary<int, int>();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Builder(
                itemCount: 1000,
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                itemBuilder: (_, index) =>
                {
                    counters[index] = counters.GetValueOrDefault(index) + 1;
                    return new SizedBox(key: new ValueKey<int>(index), width: 200, height: 200);
                })));

        Assert.Single(FindKey(tester, new ValueKey<int>(4)));
        Assert.Equal(1, counters[4]);

        tester.Fling(tester.ElementOfType<GridView>(), new Vector(0, -300), 5000);
        tester.PumpAndSettle();

        Assert.Empty(FindKey(tester, new ValueKey<int>(4)));
        Assert.Equal(1, counters[4]);

        tester.Fling(tester.ElementOfType<GridView>(), new Vector(0, 300), 5000);
        tester.PumpAndSettle();

        Assert.Single(FindKey(tester, new ValueKey<int>(4)));
        Assert.Equal(2, counters[4]);
    }

    [Fact]
    public void GridView_DoesNotReportVisualOverflowUnnecessarily()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                children: [new Container(height: 200.0)])));

        // 1st, check that the render object has received the default clip behavior.
        RenderViewport renderObject = FirstViewport(tester);
        Assert.Equal(Clip.HardEdge, renderObject.ClipBehavior);

        // The context will get Clip.none because there is no actual visual overflow.
        var context = new TestClipPaintingContext();
        renderObject.Paint(context, default);
        Assert.Equal(Clip.None, context.ClipBehavior);
    }

    [Fact]
    public void GridView_RespectsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                children: TallChildren(13))));

        // 1st, check that the render object has received the default clip behavior.
        RenderViewport renderObject = FirstViewport(tester);
        Assert.Equal(Clip.HardEdge, renderObject.ClipBehavior);

        // 2nd, check that the painting context has received the default clip behavior.
        var context = new TestClipPaintingContext();
        renderObject.Paint(context, default);
        Assert.Equal(Clip.HardEdge, context.ClipBehavior);

        // 3rd, pump a new widget to check that the render object can update its clip behavior.
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                clipBehavior: Clip.AntiAlias,
                children: TallChildren(13))));
        Assert.Equal(Clip.AntiAlias, renderObject.ClipBehavior);

        // 4th, check that a non-default clip behavior can be sent to the painting context.
        renderObject.Paint(context, default);
        Assert.Equal(Clip.AntiAlias, context.ClipBehavior);
    }

    [Fact]
    public void GridViewBuilder_RespectsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Builder(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                itemCount: 10,
                itemBuilder: (_, _) => new Container(height: 2000.0),
                clipBehavior: Clip.AntiAlias)));

        Assert.Equal(Clip.AntiAlias, FirstViewport(tester).ClipBehavior);
    }

    [Fact]
    public void GridViewCustom_RespectsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Custom(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 3),
                childrenDelegate: new SliverChildBuilderDelegate((_, _) => new Container(height: 2000.0), 1),
                clipBehavior: Clip.AntiAlias)));

        Assert.Equal(Clip.AntiAlias, FirstViewport(tester).ClipBehavior);
    }

    [Fact]
    public void GridViewCount_RespectsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Count(
                crossAxisCount: 3,
                clipBehavior: Clip.AntiAlias,
                children: [new Container(height: 2000.0)])));

        Assert.Equal(Clip.AntiAlias, FirstViewport(tester).ClipBehavior);
    }

    [Fact]
    public void GridViewExtent_RespectsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Extent(
                maxCrossAxisExtent: 1000,
                clipBehavior: Clip.AntiAlias,
                children: [new Container(height: 2000.0)])));

        Assert.Equal(Clip.AntiAlias, FirstViewport(tester).ClipBehavior);
    }

    [Fact]
    public void GridViewCount_RespectsMainAxisExtent()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Count(crossAxisCount: 4, mainAxisExtent: 100.0, children: BuilderTextChildren(20))));

        Assert.Equal(new Size(200.0, 100.0), tester.GetSize(FindText(tester, "4").Single()));
    }

    [Fact]
    public void GridViewExtent_RespectsMainAxisExtent()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Extent(maxCrossAxisExtent: 200.0, mainAxisExtent: 100.0, children: BuilderTextChildren(20))));

        Assert.Equal(new Size(200.0, 100.0), tester.GetSize(FindText(tester, "4").Single()));
    }

    [Fact]
    public void SliverGridDelegateWithFixedCrossAxisCount_MainAxisExtentWorksAsExpected()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4, mainAxisExtent: 100.0),
                children: BuilderTextChildren(20))));

        Assert.Equal(new Size(200.0, 100.0), tester.GetSize(FindText(tester, "4").Single()));
    }

    [Fact]
    public void SliverGridDelegateWithMaxCrossAxisExtent_MainAxisExtentWorksAsExpected()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new GridView(
                gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
                    maxCrossAxisExtent: 200.0,
                    mainAxisExtent: 100.0),
                children: BuilderTextChildren(20))));

        Assert.Equal(new Size(200.0, 100.0), tester.GetSize(FindText(tester, "4").Single()));
    }

    [DebugOnlyFact]
    public void SliverGridDelegateWithMaxCrossAxisExtent_ThrowsAssertionErrorWhenMaxCrossAxisExtentIsZero()
    {
        Assert.Throws<AssertionError>(() => new Directionality(
            TextDirection.Ltr,
            GridView.Extent(maxCrossAxisExtent: 0)));
    }

    /// <remarks>Regression test for https://github.com/flutter/flutter/issues/130685.</remarks>
    [Fact]
    public void SliverGrid_SetsCorrectExtentForNullReturningBuilderDelegate()
    {
        using var tester = new FrameworkDartTester();
        var controller = new ScrollController();
        try
        {
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                GridView.Builder(
                    controller: controller,
                    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 3,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16),
                    itemBuilder: (_, index) => index == 12
                        ? null
                        : new Container(
                            height: 100,
                            width: 100,
                            color: new Color(0xFFFF8A80),
                            alignment: Alignment.Center,
                            child: new Text($"item {index + 1}")))));
            tester.PumpAndSettle();

            Assert.Equal(double.PositiveInfinity, controller.Position.MaxScrollExtent);
            Assert.Equal(0.0, controller.Position.Pixels);
            tester.Fling(tester.ElementOfType<GridView>(), new Vector(0.0, -1300.0), 100.0);
            tester.PumpAndSettle();

            // The actual extent of the children is 472.0. This should be reflected when the builder
            // returns null (meaning we have reached the end).
            Assert.Equal(472.0, controller.Position.MaxScrollExtent);
            Assert.Equal(472.0, controller.Position.Pixels);
        }
        finally
        {
            controller.Dispose();
        }
    }

    [DebugOnlyFact]
    public void SliverGridDelegate_MainAxisExtentAddAssert()
    {
        AssertionError fixedCount = Assert.Throws<AssertionError>(() => new SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 3,
            mainAxisSpacing: 8,
            crossAxisSpacing: 8,
            mainAxisExtent: -100));
        Assert.Contains("mainAxisExtent == null || mainAxisExtent >= 0", fixedCount.ToString());

        AssertionError maxExtent = Assert.Throws<AssertionError>(() => new SliverGridDelegateWithMaxCrossAxisExtent(
            maxCrossAxisExtent: 100,
            mainAxisSpacing: 8,
            crossAxisSpacing: 8,
            mainAxisExtent: -100));
        Assert.Contains("mainAxisExtent == null || mainAxisExtent >= 0", maxExtent.ToString());
    }

    // ---------- widgets/grid_view_layout_test.dart ----------

    [Fact]
    public void GridViewLayout_EmptyGridView()
    {
        using var tester = new FrameworkDartTester();
        List<Widget> children =
        [
            new DecoratedBox(new BoxDecoration()),
            new DecoratedBox(new BoxDecoration()),
            new DecoratedBox(new BoxDecoration()),
            new DecoratedBox(new BoxDecoration()),
        ];

        Widget Build(double maxCrossAxisExtent) => new Directionality(
            TextDirection.Ltr,
            new Center(child: new SizedBox(
                width: 200.0,
                child: GridView.Extent(maxCrossAxisExtent: maxCrossAxisExtent, shrinkWrap: true, children: children))));

        tester.PumpWidget(Build(100.0));

        List<RenderBox> boxes = DecoratedBoxes(tester);
        Assert.Equal(4, boxes.Count);
        Assert.All(boxes, box => Assert.Equal(new Size(100.0, 100.0), box.Size));

        var grid = (RenderBox)tester.ElementOfType<GridView>().FindRenderObject()!;
        Assert.Equal(new Size(200.0, 200.0), grid.Size);
        Assert.False(grid.DebugNeedsLayout);

        tester.PumpWidget(Build(60.0));

        Assert.All(DecoratedBoxes(tester), box => Assert.Equal(new Size(50.0, 50.0), box.Size));
        Assert.Equal(new Size(200.0, 50.0), grid.Size);
    }

    // ---------- widgets/scrollable_grid_test.dart ----------

    [Fact]
    public void GridView_DefaultControl()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(child: GridView.Count(crossAxisCount: 1))));
    }

    /// <remarks>Tests https://github.com/flutter/flutter/issues/5522.</remarks>
    [Fact]
    public void GridView_DisplaysCorrectChildrenWithNonzeroPadding()
    {
        using var tester = new FrameworkDartTester();
        Widget testWidget = new Directionality(
            TextDirection.Ltr,
            new Align(child: new SizedBox(
                height: 800.0,
                width: 300.0, // forces the grid children to be 300..300
                child: GridView.Count(
                    crossAxisCount: 1,
                    padding: new Thickness(0.0, 100.0, 0.0, 0.0),
                    children:
                    [
                        .. Enumerable.Range(0, 10)
                            .Select(index => (Widget)new Text($"{index}", key: new ValueKey<int>(index))),
                    ]))));

        tester.PumpWidget(testWidget);

        // Screen is 600px high, and has the following items:
        //   100..400 = 0
        //   400..700 = 1
        tester.Pump();
        AssertTexts(tester, found: ["0", "1"], missing: ["2", "3"]);

        tester.Drag(FindText(tester, "1").Single(), new Vector(0.0, -500.0));
        tester.Pump();

        //  -100..300 = 1
        //   300..600 = 2
        //   600..600 = 3
        AssertTexts(tester, found: ["1", "2", "3"], missing: ["0", "4", "5"]);

        tester.Drag(FindText(tester, "1").Single(), new Vector(0.0, 150.0));
        tester.Pump();

        // Child '0' is now back onscreen, but by less than `padding.top`.
        //  -250..050 = 0
        //   050..450 = 1
        //   450..750 = 2
        AssertTexts(tester, found: ["0", "1", "2"], missing: ["3", "4"]);
    }

    /// <remarks>Regression test for https://github.com/flutter/flutter/issues/9506.</remarks>
    [Fact]
    public void GridViewCount_FixedItemExtent_ScrollToEnd_Append_Scroll()
    {
        // The held pointer overscrolls into the Android glow, whose pull recedes on a timer.
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        Widget BuildFrame(int itemCount) => new Directionality(
            TextDirection.Ltr,
            GridView.Count(
                crossAxisCount: itemCount,
                children:
                [
                    .. Enumerable.Range(0, itemCount)
                        .Select(index => (Widget)new SizedBox(height: 200.0, child: new Text($"item {index}"))),
                ]));

        tester.PumpWidget(BuildFrame(3));
        AssertTexts(tester, found: ["item 0", "item 1", "item 2"], missing: []);

        tester.PumpWidget(BuildFrame(4));
        TestGesture gesture = tester.StartGesture(new Point(0.0, 300.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, -200.0));
        tester.PumpAndSettle();
        Assert.Single(FindText(tester, "item 3"));
    }

    // ---------- helpers ----------

    private static SliverConstraints Constraints(
        AxisDirection axisDirection = AxisDirection.Down,
        AxisDirection crossAxisDirection = AxisDirection.Right,
        double crossAxisExtent = 800.0)
    {
        return new SliverConstraints(
            AxisDirection: axisDirection,
            GrowthDirection: GrowthDirection.Forward,
            UserScrollDirection: ScrollDirection.Idle,
            ScrollOffset: 0.0,
            PrecedingScrollExtent: 0.0,
            Overlap: 0.0,
            RemainingPaintExtent: 600.0,
            CrossAxisExtent: crossAxisExtent,
            CrossAxisDirection: crossAxisDirection,
            ViewportMainAxisExtent: 600.0,
            RemainingCacheExtent: 600.0,
            CacheOrigin: 0.0);
    }

    private static Widget StateTile(string state, List<string> log) => new GestureDetector(
        dragStartBehavior: DragStartBehavior.Down,
        onTap: () => log.Add(state),
        child: new ColoredBox(new Color(0xFF0000FF), child: new Text(state)));

    private static List<Widget> LoggingChildren(int count, List<int> log) =>
    [
        .. Enumerable.Range(0, count).Select(i => (Widget)new Builder(_ =>
        {
            log.Add(i);
            return new Text($"{i}");
        })),
    ];

    private static List<Widget> TextChildren(int count) =>
        [.. Enumerable.Range(0, count).Select(i => (Widget)new Text($"{i}"))];

    private static List<Widget> BuilderTextChildren(int count) =>
        [.. Enumerable.Range(0, count).Select(i => (Widget)new Builder(_ => new Text($"{i}")))];

    private static List<Widget> TallChildren(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => (Widget)new Container(height: 2000.0))];

    private static RenderViewport FirstViewport(FrameworkDartTester tester) =>
        (RenderViewport)tester.ElementsOfType<Viewport>()[0].FindRenderObject()!;

    private static List<RenderBox> DecoratedBoxes(FrameworkDartTester tester) =>
        [.. FindOnstage(tester, e => e.Widget is DecoratedBox).Select(e => (RenderBox)e.FindRenderObject()!)];

    /// <summary>Asserts <c>find.text('$i')</c> finds exactly the indices in [from, to).</summary>
    private static void AssertFound(FrameworkDartTester tester, int from, int to, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Assert.True(
                FindText(tester, $"{i}").Count == (i >= from && i < to ? 1 : 0),
                $"find.text('{i}') expected {(i >= from && i < to ? "one widget" : "nothing")}");
        }
    }

    private static void AssertTexts(FrameworkDartTester tester, string[] found, string[] missing)
    {
        foreach (string text in found)
        {
            Assert.Single(FindText(tester, text));
        }

        foreach (string text in missing)
        {
            Assert.Empty(FindText(tester, text));
        }
    }

    // flutter_test finders skip offstage elements (a sliver's cached children) by default.
    private static List<Element> FindOnstage(FrameworkDartTester tester, Func<Element, bool> predicate)
    {
        var result = new List<Element>();
        void Visit(Element element)
        {
            if (predicate(element))
            {
                result.Add(element);
            }

            element.DebugVisitOnstageChildren(Visit);
        }

        tester.Root.DebugVisitOnstageChildren(Visit);
        return result;
    }

    private static List<Element> FindText(FrameworkDartTester tester, string text) =>
        FindOnstage(tester, element => element.Widget is Text { Data: var data } && data == text);

    private static List<Element> FindKey(FrameworkDartTester tester, Key key) =>
        FindOnstage(tester, element => Equals(element.Widget.Key, key));

    private static void ExpectSliverConstraints(
        RenderSliver sliver,
        double cacheOrigin,
        double remainingPaintExtent,
        double remainingCacheExtent,
        double scrollOffset)
    {
        Assert.Equal(cacheOrigin, sliver.Constraints.CacheOrigin);
        Assert.Equal(remainingPaintExtent, sliver.Constraints.RemainingPaintExtent);
        Assert.Equal(remainingCacheExtent, sliver.Constraints.RemainingCacheExtent);
        Assert.Equal(scrollOffset, sliver.Constraints.ScrollOffset);
    }

    private static void ExpectSliverGeometry(RenderSliver sliver, double paintExtent, double cacheExtent, bool visible)
    {
        Assert.Equal(paintExtent, sliver.Geometry!.PaintExtent);
        Assert.Equal(cacheExtent, sliver.Geometry.CacheExtent);
        Assert.Equal(visible, sliver.Geometry.Visible);
    }

    private static PipelineOwner Layout(RenderViewport viewport)
    {
        var root = new RenderView(new FlutterView(new Size(800, 600))) { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(800, 600));
        return pipeline;
    }

    private static List<RenderBox> SizedChildren(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => (RenderBox)new RenderConstrainedBox(
            BoxConstraints.Tight(new Size(400.0, 100.0))))];

    /// <summary>A pinned 20px header, so the sliver after it lays out under an overlap.</summary>
    private sealed class RenderSliverPinnedBox(double extent) : RenderSliver
    {
        protected override void PerformLayout()
        {
            Geometry = new SliverGeometry(
                ScrollExtent: extent,
                PaintExtent: extent,
                MaxPaintExtent: extent,
                LayoutExtent: 0.0);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// rendering_tester.dart's `TestClipPaintingContext`: records the clip behavior and paints nothing.
    private sealed class TestClipPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        public Clip ClipBehavior { get; private set; } = Clip.None;

        public override ClipRectLayer? PushClipRect(
            bool needsCompositing,
            Point offset,
            Rect clipRect,
            PaintingContextCallback painter,
            Clip clipBehavior = Clip.HardEdge,
            ClipRectLayer? oldLayer = null)
        {
            ClipBehavior = clipBehavior;
            return null;
        }
    }

    /// flutter_test's `TestRecordingPaintingContext`: children paint inline, layers are not pushed, and
    /// clips go straight to the recording canvas.
    private sealed class TestRecordingPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        private readonly Canvas _canvas = new(new PictureRecorder());

        public override Canvas Canvas => _canvas;

        public IReadOnlyList<CanvasCall> Calls => _canvas.DebugCalls;

        public override void PaintChild(RenderObject child, Point offset) => child.Paint(this, offset);

        public override void PushLayer(
            ContainerLayer childLayer,
            PaintingContextCallback painter,
            Point offset,
            Rect? childPaintBounds = null)
        {
            painter(this, offset);
        }

        public override ClipRectLayer? PushClipRect(
            bool needsCompositing,
            Point offset,
            Rect clipRect,
            PaintingContextCallback painter,
            Clip clipBehavior = Clip.HardEdge,
            ClipRectLayer? oldLayer = null)
        {
            Rect shifted = clipRect.Translate(new Vector(offset.X, offset.Y));
            ClipRectAndPaint(shifted, clipBehavior, shifted, () => painter(this, offset));
            return null;
        }
    }

    /// <summary>sliver_cache_test.dart's <c>TestRenderSliverBoxChildManager</c>.</summary>
    private sealed class TestRenderSliverBoxChildManager(List<RenderBox> children) : IRenderSliverBoxChildManager
    {
        private RenderSliverMultiBoxAdaptor? _renderObject;
        private int? _currentlyUpdatingChildIndex;

        public List<RenderBox> Children { get; } = children;

        public int ChildCount => Children.Count;

        public RenderSliverGrid CreateRenderSliverGrid()
        {
            Assert.Null(_renderObject);
            var grid = new RenderSliverGrid(
                childManager: this,
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    childAspectRatio: 4.0));
            _renderObject = grid;
            return grid;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= Children.Count)
            {
                return;
            }

            try
            {
                _currentlyUpdatingChildIndex = index;
                _renderObject!.Insert(Children[index], after);
            }
            finally
            {
                _currentlyUpdatingChildIndex = null;
            }
        }

        public void RemoveChild(RenderBox child) => _renderObject!.Remove(child);

        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null)
        {
            Assert.True(lastIndex >= firstIndex);
            return Children.Count
                   * (trailingScrollOffset!.Value - leadingScrollOffset!.Value)
                   / (lastIndex!.Value - firstIndex!.Value + 1);
        }

        public void DidAdoptChild(RenderBox child)
        {
            Assert.NotNull(_currentlyUpdatingChildIndex);
            ((SliverMultiBoxAdaptorParentData)child.parentData!).Index = _currentlyUpdatingChildIndex;
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }
}
