using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/two_dimensional_viewport_test.dart.

namespace Plumix.Tests;

public sealed class TwoDimensionalViewportTests
{
    private static readonly Size Surface = new(800, 600);

    // ChildVicinity ----------------------------------------------------------------------------

    [Fact]
    public void ChildVicinity_IsComparableAndPrintsLikeDart()
    {
        var baseline = new ChildVicinity(xIndex: 0, yIndex: 0);

        Assert.Equal(new ChildVicinity(0, 0), baseline);
        Assert.NotEqual(new ChildVicinity(0, 2), baseline);
        Assert.NotEqual(new ChildVicinity(3, 0), baseline);
        Assert.NotEqual(new ChildVicinity(20, 30), baseline);

        // Dart returns the raw index difference rather than a normalized -1/0/1.
        Assert.Equal(0, baseline.CompareTo(new ChildVicinity(0, 0)));
        Assert.Equal(-2, baseline.CompareTo(new ChildVicinity(0, 2)));
        Assert.Equal(-3, baseline.CompareTo(new ChildVicinity(3, 0)));
        Assert.Equal(-20, baseline.CompareTo(new ChildVicinity(20, 30)));

        Assert.Equal("(xIndex: 0, yIndex: 0)", baseline.ToString());
        Assert.Equal("(xIndex: 0, yIndex: 2)", new ChildVicinity(0, 2).ToString());
        Assert.Equal("(xIndex: 3, yIndex: 0)", new ChildVicinity(3, 0).ToString());
        Assert.Equal("(xIndex: 20, yIndex: 30)", new ChildVicinity(20, 30).ToString());

        Assert.Equal(-1, ChildVicinity.Invalid.XIndex);
        Assert.Equal(-1, ChildVicinity.Invalid.YIndex);
    }

    [Fact]
    public void ChildVicinity_EqualityIgnoresTheRuntimeType()
    {
        // Dart's `operator ==` only checks `other is ChildVicinity`, so a subclass with the same
        // indices compares equal to its base.
        Assert.Equal(new ChildVicinity(1, 1), new TestVicinity(1, 1));
        Assert.Equal(new ChildVicinity(1, 1).GetHashCode(), new TestVicinity(1, 1).GetHashCode());
    }

    // Parent data ------------------------------------------------------------------------------

    [Fact]
    public void TwoDimensionalViewportParentData_DefaultsAndToString()
    {
        var parentData = new TwoDimensionalViewportParentData();
        Assert.Equal(ChildVicinity.Invalid, parentData.Vicinity);

        parentData.Vicinity = new ChildVicinity(xIndex: 10, yIndex: 10);
        parentData.PaintOffset = new Point(20.0, 20.0);
        parentData.LayoutOffset = new Point(20.0, 20.0);

        Assert.Equal(
            "vicinity=(xIndex: 10, yIndex: 10); layoutOffset=Offset(20.0, 20.0); "
            + "paintOffset=Offset(20.0, 20.0); not visible; ",
            parentData.ToString());
    }

    // Delegates --------------------------------------------------------------------------------

    [Fact]
    public void BuilderDelegate_ReturnsNullPastTheMaxIndices()
    {
        TwoDimensionalChildBuilderDelegate builderDelegate =
            TwoDimensionalHarness.BuilderDelegate(maxXIndex: 0, maxYIndex: 0);
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(builderDelegate));
        harness.Pump(Surface);

        BuildContext context = FindCellContext(harness);
        Assert.NotNull(builderDelegate.Build(context, new ChildVicinity(0, 0)));
        Assert.Null(builderDelegate.Build(context, new ChildVicinity(1, 0)));
        Assert.Null(builderDelegate.Build(context, new ChildVicinity(0, 1)));
        Assert.Null(builderDelegate.Build(context, new ChildVicinity(1, 1)));
    }

    [Fact]
    public void BuilderDelegate_MaxIndexAssertions()
    {
        TwoDimensionalChildBuilderDelegate builderDelegate = TwoDimensionalHarness.BuilderDelegate();

        builderDelegate.MaxXIndex = -1;
        Assert.Equal(-1, builderDelegate.MaxXIndex);
        Assert.Throws<AssertionError>(() => builderDelegate.MaxXIndex = -2);

        builderDelegate.MaxYIndex = -1;
        Assert.Equal(-1, builderDelegate.MaxYIndex);
        Assert.Throws<AssertionError>(() => builderDelegate.MaxYIndex = -2);
    }

    [Fact]
    public void BuilderDelegate_MaxIndexSettersNotifyOnlyWhenTheValueChanges()
    {
        TwoDimensionalChildBuilderDelegate builderDelegate = TwoDimensionalHarness.BuilderDelegate();
        int notifications = 0;
        builderDelegate.AddListener(() => notifications++);

        builderDelegate.MaxXIndex = 5;
        builderDelegate.MaxYIndex = 5;
        Assert.Equal(0, notifications);

        builderDelegate.MaxXIndex = 4;
        Assert.Equal(1, notifications);
        builderDelegate.MaxYIndex = 4;
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void BuilderDelegate_ShouldRebuildIsAlwaysTrue()
    {
        TwoDimensionalChildBuilderDelegate builderDelegate = TwoDimensionalHarness.BuilderDelegate();
        Assert.True(builderDelegate.ShouldRebuild(builderDelegate));
    }

    [Fact]
    public void BuilderDelegate_WrapsChildrenInKeepAliveAndRepaintBoundary()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate(maxXIndex: 0, maxYIndex: 0)));
        harness.Pump(Surface);
        BuildContext context = FindCellContext(harness);

        Widget wrapped = TwoDimensionalHarness.BuilderDelegate(maxXIndex: 0, maxYIndex: 0)
            .Build(context, new ChildVicinity(0, 0))!;
        var keepAlive = Assert.IsType<AutomaticKeepAlive>(wrapped);
        var selection = Assert.IsType<SelectionKeepAlive>(keepAlive.Child);
        Assert.IsType<RepaintBoundary>(selection.Child);

        Widget noBoundaries = TwoDimensionalHarness
            .BuilderDelegate(maxXIndex: 0, maxYIndex: 0, addRepaintBoundaries: false)
            .Build(context, new ChildVicinity(0, 0))!;
        var plainKeepAlive = Assert.IsType<AutomaticKeepAlive>(noBoundaries);
        var plainSelection = Assert.IsType<SelectionKeepAlive>(plainKeepAlive.Child);
        Assert.IsType<Container>(plainSelection.Child);

        Widget bare = TwoDimensionalHarness
            .BuilderDelegate(
                maxXIndex: 0,
                maxYIndex: 0,
                addRepaintBoundaries: false,
                addAutomaticKeepAlives: false)
            .Build(context, new ChildVicinity(0, 0))!;
        Assert.IsType<Container>(bare);
    }

    [Fact]
    public void BuilderDelegate_ReportsABuilderThatThrowsAndKeepsLayingOut()
    {
        var errors = new List<object>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => errors.Add(details.Exception);
        try
        {
            var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(
                    maxXIndex: 0,
                    maxYIndex: 0,
                    builder: (_, _) => throw new InvalidOperationException("Builder error!"))));
            harness.Pump(Surface);

            Assert.Single(errors);
            Assert.Contains(
                "Builder error!",
                Assert.IsAssignableFrom<Exception>(errors[0]).Message,
                StringComparison.Ordinal);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void ListDelegate_ReturnsNullOutsideTheListBounds()
    {
        var listDelegate = new TwoDimensionalChildListDelegate(TwoDimensionalHarness.Children(1, 1));
        var harness = new TwoDimensionalRenderHarness(new SimpleListTableView(listDelegate));
        harness.Pump(Surface);
        BuildContext context = FindCellContext(harness);

        Assert.NotNull(listDelegate.Build(context, new ChildVicinity(0, 0)));
        Assert.Null(listDelegate.Build(context, new ChildVicinity(1, 0)));
        Assert.Null(listDelegate.Build(context, new ChildVicinity(0, 1)));
        Assert.Null(listDelegate.Build(context, new ChildVicinity(1, 1)));
    }

    [Fact]
    public void ListDelegate_ShouldRebuildComparesTheListIdentity()
    {
        IReadOnlyList<IReadOnlyList<Widget>> children = TwoDimensionalHarness.Children(2, 2);
        var listDelegate = new TwoDimensionalChildListDelegate(children);
        Assert.False(listDelegate.ShouldRebuild(listDelegate));
        Assert.True(listDelegate.ShouldRebuild(
            new TwoDimensionalChildListDelegate(TwoDimensionalHarness.Children(2, 2))));
    }

    // Widget-level asserts ---------------------------------------------------------------------

    [Fact]
    public void TwoDimensionalViewport_AssertsAgainstAnAxisMismatch()
    {
        var offset = new TestViewportOffset();
        AssertionError vertical = Assert.Throws<AssertionError>(() => new SimpleBuilderTableViewport(
            verticalOffset: offset,
            verticalAxisDirection: AxisDirection.Left,
            horizontalOffset: offset,
            horizontalAxisDirection: AxisDirection.Right,
            @delegate: TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Vertical));
        Assert.Contains("AxisDirection is not Axis.", vertical.Message, StringComparison.Ordinal);

        AssertionError horizontal = Assert.Throws<AssertionError>(() => new SimpleBuilderTableViewport(
            verticalOffset: offset,
            verticalAxisDirection: AxisDirection.Down,
            horizontalOffset: offset,
            horizontalAxisDirection: AxisDirection.Down,
            @delegate: TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Vertical));
        Assert.Contains("AxisDirection is not Axis.", horizontal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_AssertsAgainstAnAxisMismatch()
    {
        var offset = new TestViewportOffset();
        Assert.Throws<AssertionError>(() => new RenderSimpleBuilderTableViewport(
            horizontalOffset: offset,
            horizontalAxisDirection: AxisDirection.Right,
            verticalOffset: offset,
            verticalAxisDirection: AxisDirection.Left,
            @delegate: TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Vertical,
            childManager: new StubChildManager()));
        Assert.Throws<AssertionError>(() => new RenderSimpleBuilderTableViewport(
            horizontalOffset: offset,
            horizontalAxisDirection: AxisDirection.Up,
            verticalOffset: offset,
            verticalAxisDirection: AxisDirection.Down,
            @delegate: TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Vertical,
            childManager: new StubChildManager()));
    }

    // Render object ----------------------------------------------------------------------------

    [Fact]
    public void RenderTwoDimensionalViewport_Getters()
    {
        var offset = new TestViewportOffset();
        TwoDimensionalChildBuilderDelegate builderDelegate = TwoDimensionalHarness.BuilderDelegate();
        var viewport = new RenderSimpleBuilderTableViewport(
            horizontalOffset: new TestViewportOffset(20.0),
            horizontalAxisDirection: AxisDirection.Right,
            verticalOffset: new TestViewportOffset(10.0),
            verticalAxisDirection: AxisDirection.Down,
            @delegate: builderDelegate,
            mainAxis: Axis.Vertical,
            childManager: new StubChildManager());

        Assert.Equal(Clip.HardEdge, viewport.ClipBehavior);
        Assert.Equal(RenderAbstractViewport.DefaultCacheExtent, viewport.CacheExtent);
        Assert.True(viewport.IsRepaintBoundary);
        Assert.Equal(20.0, viewport.HorizontalOffset.Pixels);
        Assert.Equal(AxisDirection.Right, viewport.HorizontalAxisDirection);
        Assert.Equal(10.0, viewport.VerticalOffset.Pixels);
        Assert.Equal(AxisDirection.Down, viewport.VerticalAxisDirection);
        Assert.Same(builderDelegate, viewport.Delegate);
        Assert.Equal(Axis.Vertical, viewport.MainAxis);
        Assert.NotNull(offset);

        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);
        Assert.Equal(new Size(800, 600), Viewport(harness).ViewportDimension);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_OrdersChildrenByMainAxis()
    {
        var rowMajor = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        rowMajor.Pump(Surface);
        RenderSimpleBuilderTableViewport vertical = Viewport(rowMajor);

        Assert.Equal(new ChildVicinity(0, 0), vertical.ParentDataOf(vertical.FirstChild!).Vicinity);
        Assert.Equal(
            new ChildVicinity(1, 0),
            vertical.ParentDataOf(vertical.ChildAfter(vertical.FirstChild!)!).Vicinity);
        Assert.Null(vertical.ChildBefore(vertical.FirstChild!));
        Assert.Equal(new ChildVicinity(4, 3), vertical.ParentDataOf(vertical.LastChild!).Vicinity);
        Assert.Null(vertical.ChildAfter(vertical.LastChild!));
        Assert.Equal(
            new ChildVicinity(3, 3),
            vertical.ParentDataOf(vertical.ChildBefore(vertical.LastChild!)!).Vicinity);

        var columnMajor = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Horizontal));
        columnMajor.Pump(Surface);
        RenderSimpleBuilderTableViewport horizontal = Viewport(columnMajor);

        Assert.Equal(new ChildVicinity(0, 0), horizontal.ParentDataOf(horizontal.FirstChild!).Vicinity);
        Assert.Equal(
            new ChildVicinity(0, 1),
            horizontal.ParentDataOf(horizontal.ChildAfter(horizontal.FirstChild!)!).Vicinity);
        Assert.Equal(new ChildVicinity(4, 3), horizontal.ParentDataOf(horizontal.LastChild!).Vicinity);
        Assert.Equal(
            new ChildVicinity(4, 2),
            horizontal.ParentDataOf(horizontal.ChildBefore(horizontal.LastChild!)!).Vicinity);
    }

    [Theory]
    [InlineData(false, false, 0.0, 0.0, 1000.0, 1000.0)]
    [InlineData(true, false, 0.0, 400.0, 1000.0, -600.0)]
    [InlineData(false, true, 600.0, 0.0, -400.0, 1000.0)]
    [InlineData(true, true, 600.0, 400.0, -400.0, -600.0)]
    public void RenderTwoDimensionalViewport_SetsUpParentData(
        bool reverseVertical,
        bool reverseHorizontal,
        double firstPaintX,
        double firstPaintY,
        double lastPaintX,
        double lastPaintY)
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(reverse: reverseVertical),
            horizontalDetails: ScrollableDetails.Horizontal(reverse: reverseHorizontal),
            useCacheExtent: true));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);

        TestExtendedParentData first = viewport.ParentDataOf(viewport.FirstChild!);
        Assert.Equal(new ChildVicinity(0, 0), first.Vicinity);
        Assert.True(first.IsVisible);
        Assert.Equal(new Point(firstPaintX, firstPaintY), first.PaintOffset);
        Assert.Equal(new Point(0.0, 0.0), first.LayoutOffset);

        TestExtendedParentData last = viewport.ParentDataOf(viewport.LastChild!);
        Assert.Equal(new ChildVicinity(5, 5), last.Vicinity);
        Assert.False(last.IsVisible);
        Assert.Equal(new Point(lastPaintX, lastPaintY), last.PaintOffset);
        Assert.Equal(new Point(1000.0, 1000.0), last.LayoutOffset);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_PartiallyVisibleChildKeepsItsLayoutOffset()
    {
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
            horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
            useCacheExtent: true));
        harness.Pump(Surface);

        verticalController.JumpTo(50.0);
        horizontalController.JumpTo(50.0);
        harness.Pump(Surface);

        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        TestExtendedParentData first = viewport.ParentDataOf(viewport.FirstChild!);
        Assert.Equal(new ChildVicinity(0, 0), first.Vicinity);
        Assert.True(first.IsVisible);
        Assert.Equal(new Point(-50.0, -50.0), first.PaintOffset);
        Assert.Equal(new Point(-50.0, -50.0), first.LayoutOffset);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_DebugDescribeChildrenNamesEachVicinity()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);

        List<DiagnosticsNode> children = Viewport(harness).DebugDescribeChildren();
        Assert.Equal(20, children.Count);
        Assert.Equal("(xIndex: 0, yIndex: 0)", children[0].Name);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_CacheExtentStyleWidensTheLaidOutRange()
    {
        var pixels = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            useCacheExtent: true));
        pixels.Pump(Surface);
        Assert.Equal(36, Viewport(pixels).DebugDescribeChildren().Count);

        var viewportStyle = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            scrollCacheExtent: ScrollCacheExtent.Viewport(1.0),
            useCacheExtent: true));
        viewportStyle.Pump(Surface);
        Assert.Equal(36, Viewport(viewportStyle).DebugDescribeChildren().Count);

        var none = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        none.Pump(Surface);
        Assert.Equal(20, Viewport(none).DebugDescribeChildren().Count);
    }

    [DebugOnlyFact]
    public void RenderTwoDimensionalViewport_ComputeDryLayoutAssertsBothAxesAreBounded()
    {
        var viewport = new RenderSimpleBuilderTableViewport(
            horizontalOffset: new TestViewportOffset(),
            horizontalAxisDirection: AxisDirection.Right,
            verticalOffset: new TestViewportOffset(),
            verticalAxisDirection: AxisDirection.Down,
            @delegate: TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Vertical,
            childManager: new StubChildManager());

        FlutterError error = Assert.Throws<FlutterError>(() => viewport.GetDryLayout(
            new BoxConstraints(
                MaxWidth: double.PositiveInfinity,
                MaxHeight: double.PositiveInfinity)));
        Assert.Contains("unbounded", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_ResizesWithItsParent()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);
        Assert.Equal(new Size(800, 600), Viewport(harness).ViewportDimension);

        harness.Pump(new Size(300, 300));
        Assert.Equal(new Size(300, 300), Viewport(harness).ViewportDimension);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_RebuildsWhenTheDelegateChanges()
    {
        var firstKey = new ValueKey<string>("first");
        var secondKey = new ValueKey<string>("second");
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(
                maxXIndex: 0,
                maxYIndex: 0,
                builder: (_, _) => new Container(key: firstKey, height: 200, width: 200))));
        harness.Pump(Surface);
        Assert.Equal(new Size(200, 200), Viewport(harness).FirstChild!.Size);

        harness.Replace(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(
                maxXIndex: 0,
                maxYIndex: 0,
                builder: (_, _) => new Container(key: secondKey, height: 300, width: 300))));
        harness.Pump(Surface);

        // The delegate is a new instance, so every child is rebuilt from it.
        Assert.Equal(new Size(200, 200), Viewport(harness).FirstChild!.Size);
        Assert.Equal(
            new Size(200, 200),
            FindRenderObject<RenderConstrainedBox>(Viewport(harness).FirstChild)!.Size);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_GetChildForOnlySeesLiveChildren()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);

        Assert.Same(viewport.FirstChild, viewport.TestGetChildFor(new ChildVicinity(0, 0)));
        Assert.Null(viewport.TestGetChildFor(new ChildVicinity(10, 10)));
    }

    [Fact]
    public void RenderTwoDimensionalViewport_RejectsAnInvalidVicinity()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);

        AssertionError error = Assert.Throws<AssertionError>(
            () => Viewport(harness).BuildOrObtainChildFor(ChildVicinity.Invalid));
        Assert.Contains("ChildVicinity.invalid", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderTwoDimensionalViewport_RequiresContentDimensions()
    {
        using RenderErrorRethrowScope scope = RenderErrorRethrowScope.Enter();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            applyDimensions: false));

        FlutterError error = Assert.Throws<FlutterError>(() => harness.Pump(Surface));
        Assert.Contains("was not given content dimensions", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_RequiresALayoutOffsetPerChild()
    {
        using RenderErrorRethrowScope scope = RenderErrorRethrowScope.Enter();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            setLayoutOffset: false));

        AssertionError error = Assert.Throws<AssertionError>(() => harness.Pump(Surface));
        Assert.Contains("was not provided a layoutOffset", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_RequiresEveryChildToBeLaidOut()
    {
        using RenderErrorRethrowScope scope = RenderErrorRethrowScope.Enter();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            forgetToLayoutChild: true));

        AssertionError error = Assert.Throws<AssertionError>(() => harness.Pump(Surface));
        Assert.Contains("HasSize", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_ReusesChildrenItAlreadyBuilt()
    {
        var built = new List<ChildVicinity>();
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(builder: (context, vicinity) =>
            {
                built.Add(vicinity);
                return TwoDimensionalHarness.DefaultBuilder(context, vicinity);
            }),
            verticalDetails: ScrollableDetails.Vertical(controller: controller)));
        harness.Pump(Surface);

        Assert.Equal(20, built.Count);
        Assert.Equal(new ChildVicinity(0, 0), built[0]);

        built.Clear();
        controller.JumpTo(1.0);
        harness.Pump(Surface);

        Assert.Equal(5, built.Count);
        Assert.DoesNotContain(new ChildVicinity(0, 0), built);
    }

    [DebugOnlyFact]
    public void RenderTwoDimensionalViewport_DoesNotSupportIntrinsics()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);

        Assert.Contains(
            "does not support returning intrinsic dimensions",
            Assert.Throws<FlutterError>(() => viewport.GetMinIntrinsicWidth(100)).Message,
            StringComparison.Ordinal);
        Assert.Throws<FlutterError>(() => viewport.GetMaxIntrinsicWidth(100));
        Assert.Throws<FlutterError>(() => viewport.GetMinIntrinsicHeight(100));
        Assert.Throws<FlutterError>(() => viewport.GetMaxIntrinsicHeight(100));
    }

    [Fact]
    public void RenderTwoDimensionalViewport_DoesNotThrowWhenNoChildIsLaidOut()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(
                maxXIndex: 50,
                maxYIndex: 50,
                builder: (context, vicinity) => vicinity.XIndex <= 10
                    ? null
                    : TwoDimensionalHarness.DefaultBuilder(context, vicinity))));

        harness.Pump(Surface);
        Assert.Null(Viewport(harness).FirstChild);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_ReordersKeyedChildrenWithoutFailingItsAsserts()
    {
        var firstKey = new ValueKey<int>(1);
        var secondKey = new ValueKey<int>(2);

        Widget BuildFirst() => new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate(
            maxXIndex: 1,
            maxYIndex: 2,
            builder: (_, vicinity) => new Container(
                key: vicinity == new ChildVicinity(1, 1)
                    ? firstKey
                    : vicinity == new ChildVicinity(1, 2) ? secondKey : null,
                height: 200,
                width: 200)));

        Widget BuildSecond() => new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate(
            maxXIndex: 1,
            maxYIndex: 2,
            builder: (_, vicinity) => new Container(
                key: vicinity == new ChildVicinity(0, 0)
                    ? firstKey
                    : vicinity == new ChildVicinity(1, 1) ? secondKey : null,
                height: 200,
                width: 200)));

        var harness = new TwoDimensionalRenderHarness(BuildFirst());
        harness.Pump(Surface);
        Assert.Equal(new Rect(200, 200, 200, 200), CellRect(harness, new ChildVicinity(1, 1)));

        harness.Replace(BuildSecond());
        harness.Pump(Surface);
        Assert.Equal(new Rect(0, 0, 200, 200), CellRect(harness, new ChildVicinity(0, 0)));

        harness.Replace(BuildFirst());
        harness.Pump(Surface);
        Assert.Equal(new Rect(200, 200, 200, 200), CellRect(harness, new ChildVicinity(1, 1)));
    }

    // Reveal -----------------------------------------------------------------------------------

    [Fact]
    public void RenderTwoDimensionalViewport_GetOffsetToReveal()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            useCacheExtent: true));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        RenderBox target = viewport.TestGetChildFor(new ChildVicinity(5, 5))!;

        RevealedOffset vertical = viewport.GetOffsetToReveal(target, 1.0, axis: Axis.Vertical);
        Assert.Equal(600.0, vertical.Offset);
        Assert.Equal(new Rect(1000.0, 400.0, 200.0, 200.0), vertical.Rect);

        RevealedOffset horizontal = viewport.GetOffsetToReveal(target, 1.0, axis: Axis.Horizontal);
        Assert.Equal(400.0, horizontal.Offset);
        Assert.Equal(new Rect(600.0, 1000.0, 200.0, 200.0), horizontal.Rect);

        // Omitting the axis falls back to MainAxis, which defaults to vertical.
        RevealedOffset implied = viewport.GetOffsetToReveal(target, 1.0);
        Assert.Equal(vertical.Offset, implied.Offset);
        Assert.Equal(vertical.Rect, implied.Rect);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_GetOffsetToRevealUsesTheHorizontalMainAxis()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            mainAxis: Axis.Horizontal,
            useCacheExtent: true));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        RenderBox target = viewport.TestGetChildFor(new ChildVicinity(5, 5))!;

        RevealedOffset implied = viewport.GetOffsetToReveal(target, 1.0);
        Assert.Equal(400.0, implied.Offset);
        Assert.Equal(new Rect(600.0, 1000.0, 200.0, 200.0), implied.Rect);
    }

    [Fact]
    public void RenderTwoDimensionalViewport_ShowOnScreenRevealsBothAxesAndIsIdempotent()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            useCacheExtent: true));
        harness.Pump(Surface);

        Assert.Equal(new Rect(200.0, 200.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(1, 1)));
        Assert.Equal(new Rect(1000.0, 800.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(5, 4)));

        Viewport(harness).TestGetChildFor(new ChildVicinity(5, 4))!.ShowOnScreen();
        harness.Pump(Surface);
        Assert.Equal(new Rect(600.0, 200.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(5, 4)));

        Viewport(harness).TestGetChildFor(new ChildVicinity(5, 4))!.ShowOnScreen();
        harness.Pump(Surface);
        Assert.Equal(new Rect(600.0, 200.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(5, 4)));
    }

    [Fact]
    public void RenderTwoDimensionalViewport_ShowOnScreenRevealsInBothReversedAxes()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(reverse: true),
            horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
            useCacheExtent: true));
        harness.Pump(Surface);

        Assert.Equal(new Rect(400.0, 200.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(1, 1)));
        Assert.Equal(new Rect(-400.0, -400.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(5, 4)));

        Viewport(harness).TestGetChildFor(new ChildVicinity(5, 4))!.ShowOnScreen();
        harness.Pump(Surface);
        Assert.Equal(new Rect(0.0, 200.0, 200.0, 200.0), CellRect(harness, new ChildVicinity(5, 4)));
    }

    // Hit testing ------------------------------------------------------------------------------

    [Fact]
    public void RenderTwoDimensionalViewport_HitTestsOnlyVisibleChildren()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            useCacheExtent: true));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);

        Assert.Same(
            viewport.TestGetChildFor(new ChildVicinity(0, 0)),
            HitCell(viewport, new Point(100.0, 100.0)));
        Assert.Same(
            viewport.TestGetChildFor(new ChildVicinity(2, 2)),
            HitCell(viewport, new Point(500.0, 500.0)));
        // (5, 5) exists only inside the cache extent, so it cannot be hit.
        Assert.Null(HitCell(viewport, new Point(1100.0, 1100.0)));
    }

    [Fact]
    public void RenderTwoDimensionalViewport_HitTestsThroughReversedAxes()
    {
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(reverse: true),
            horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
            useCacheExtent: true));
        harness.Pump(Surface);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);

        Assert.Same(
            viewport.TestGetChildFor(new ChildVicinity(0, 0)),
            HitCell(viewport, new Point(700.0, 500.0)));
    }

    // Keep alive -------------------------------------------------------------------------------

    [Fact]
    public void BuilderDelegate_KeepsACheckedChildAliveOffscreen()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(builder: (context, vicinity) =>
                vicinity == new ChildVicinity(0, 0)
                    ? new KeepAliveProbe(new SizedBox(width: 200, height: 200))
                    : TwoDimensionalHarness.DefaultBuilder(context, vicinity)),
            verticalDetails: ScrollableDetails.Vertical(controller: controller)));
        harness.Pump(Surface);

        KeepAliveProbeState probe = KeepAliveProbeState.Last!;
        Assert.False(probe.KeepAliveWanted);
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        Assert.False(viewport.ParentDataOf(viewport.TestGetChildFor(new ChildVicinity(0, 0))!).KeepAlive);

        // Without a keep-alive request the child is dropped as soon as it leaves the layout range.
        controller.JumpTo(600.0);
        harness.Pump(Surface);
        Assert.Null(Viewport(harness).TestGetChildFor(new ChildVicinity(0, 0)));

        controller.JumpTo(0.0);
        harness.Pump(Surface);
        KeepAliveProbeState.Last!.SetKeepAlive(true);
        harness.Pump(Surface);
        viewport = Viewport(harness);
        Assert.True(viewport.ParentDataOf(viewport.TestGetChildFor(new ChildVicinity(0, 0))!).KeepAlive);

        // Now the same child survives being scrolled out of the laid-out range, state and all.
        KeepAliveProbeState kept = KeepAliveProbeState.Last!;
        controller.JumpTo(600.0);
        harness.Pump(Surface);
        Assert.NotNull(Viewport(harness).TestGetChildFor(new ChildVicinity(0, 0)));
        Assert.Same(kept, KeepAliveProbeState.Last);
        Assert.True(kept.KeepAliveWanted);

        controller.JumpTo(0.0);
        harness.Pump(Surface);
        Assert.NotNull(Viewport(harness).TestGetChildFor(new ChildVicinity(0, 0)));
        Assert.Same(kept, KeepAliveProbeState.Last);
    }

    [Fact]
    public void BuilderDelegate_WithoutAutomaticKeepAlivesDropsTheChild()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(
                addAutomaticKeepAlives: false,
                builder: (context, vicinity) => vicinity == new ChildVicinity(0, 0)
                    ? new KeepAliveProbe(new SizedBox(width: 200, height: 200))
                    : TwoDimensionalHarness.DefaultBuilder(context, vicinity)),
            verticalDetails: ScrollableDetails.Vertical(controller: controller)));
        harness.Pump(Surface);
        KeepAliveProbeState.Last!.SetKeepAlive(true);
        harness.Pump(Surface);

        // The delegate wrapped nothing in an AutomaticKeepAlive, so the request has no listener and
        // the parent data never records it.
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        Assert.False(viewport.ParentDataOf(viewport.TestGetChildFor(new ChildVicinity(0, 0))!).KeepAlive);

        controller.JumpTo(600.0);
        harness.Pump(Surface);
        Assert.Null(Viewport(harness).TestGetChildFor(new ChildVicinity(0, 0)));
    }

    [Fact]
    public void KeepAlive_ComposesWithAnAdditionalParentDataWidget()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(
                addRepaintBoundaries: false,
                builder: (context, vicinity) => new TestParentDataWidget(
                    testValue: 20,
                    child: vicinity == new ChildVicinity(0, 0)
                        ? new KeepAliveProbe(new SizedBox(width: 200, height: 200))
                        : TwoDimensionalHarness.DefaultBuilder(context, vicinity))),
            verticalDetails: ScrollableDetails.Vertical(controller: controller)));
        harness.Pump(Surface);

        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        TestExtendedParentData parentData =
            viewport.ParentDataOf(viewport.TestGetChildFor(new ChildVicinity(0, 0))!);
        Assert.Equal(20, parentData.TestValue);
        Assert.False(parentData.KeepAlive);

        KeepAliveProbeState.Last!.SetKeepAlive(true);
        harness.Pump(Surface);
        viewport = Viewport(harness);
        parentData = viewport.ParentDataOf(viewport.TestGetChildFor(new ChildVicinity(0, 0))!);
        Assert.Equal(20, parentData.TestValue);
        Assert.True(parentData.KeepAlive);
    }

    // Helpers ----------------------------------------------------------------------------------

    private static RenderSimpleBuilderTableViewport Viewport(TwoDimensionalRenderHarness harness) =>
        FindRenderObject<RenderSimpleBuilderTableViewport>(harness.RenderView)!;

    private static Rect CellRect(TwoDimensionalRenderHarness harness, ChildVicinity vicinity)
    {
        RenderSimpleBuilderTableViewport viewport = Viewport(harness);
        RenderBox child = viewport.TestGetChildFor(vicinity)!;
        Point offset = viewport.ParentDataOf(child).PaintOffset!.Value;
        return new Rect(offset.X, offset.Y, child.Size.Width, child.Size.Height);
    }

    private static RenderBox? HitCell(RenderSimpleBuilderTableViewport viewport, Point position)
    {
        var result = new BoxHitTestResult();
        viewport.HitTest(result, position);
        foreach (HitTestEntry entry in result.Path)
        {
            if (entry.Target is RenderBox box && ReferenceEquals(box.Parent, viewport))
            {
                return box;
            }
        }

        return null;
    }

    private static BuildContext FindCellContext(TwoDimensionalRenderHarness harness)
    {
        // The delegates never read the context, so any context inside the tree is enough.
        return harness.RootContext;
    }

    private static T? FindRenderObject<T>(RenderObject? node) where T : RenderObject
    {
        if (node is T match)
        {
            return match;
        }

        T? found = null;
        node?.VisitChildren(child => found ??= FindRenderObject<T>(child));
        return found;
    }

    private sealed class TestVicinity(int xIndex, int yIndex) : ChildVicinity(xIndex, yIndex);

    private sealed class StubChildManager : ITwoDimensionalChildManager
    {
        public void StartLayout()
        {
        }

        public void BuildChild(ChildVicinity vicinity)
        {
        }

        public void ReuseChild(ChildVicinity vicinity)
        {
        }

        public void EndLayout()
        {
        }
    }
}

/// <summary>A child that can be asked to keep itself alive, mirroring Flutter's KeepAliveCheckBox.</summary>
internal sealed class KeepAliveProbe : StatefulWidget
{
    public KeepAliveProbe(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new KeepAliveProbeState();
}

internal sealed class KeepAliveProbeState : AutomaticKeepAliveClientMixin
{
    private bool _keepAlive;

    public static KeepAliveProbeState? Last { get; private set; }

    public bool KeepAliveWanted => _keepAlive;

    protected override bool WantKeepAlive => _keepAlive;

    public void SetKeepAlive(bool value)
    {
        SetState(() => _keepAlive = value);
        UpdateKeepAlive();
    }

    public override void InitState()
    {
        base.InitState();
        Last = this;
    }

    public override Widget Build(BuildContext context)
    {
        return ((KeepAliveProbe)StateWidget).Child;
    }
}
