using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/page_view.dart (parity regression tests
// mapped from flutter/packages/flutter/test/widgets/page_view_test.dart)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PageViewTests
{
    private static readonly Size Viewport = new(800, 600);

    // ------------------------------------------------------------------ API and defaults

    [Fact]
    public void PageView_DefaultsMatchFlutter()
    {
        var view = new PageView(children: [new SizedBox(), new SizedBox()]);

        Assert.Equal(Axis.Horizontal, view.ScrollDirection);
        Assert.False(view.Reverse);
        Assert.Null(view.Controller);
        Assert.Null(view.Physics);
        Assert.True(view.PageSnapping);
        Assert.Null(view.OnPageChanged);
        Assert.Equal(DragStartBehavior.Start, view.DragStartBehavior);
        Assert.False(view.AllowImplicitScrolling);
        Assert.Equal(ScrollCacheExtent.Viewport(0.0), view.ScrollCacheExtent);
        Assert.Null(view.RestorationId);
        Assert.Equal(Clip.HardEdge, view.ClipBehavior);
        Assert.Equal(HitTestBehavior.Opaque, view.HitTestBehavior);
        Assert.Null(view.ScrollBehavior);
        Assert.True(view.PadEnds);
    }

    [Fact]
    public void PageView_ScrollCacheExtentFollowsAllowImplicitScrolling()
    {
        Assert.Equal(
            ScrollCacheExtent.Viewport(1.0),
            new PageView(allowImplicitScrolling: true).ScrollCacheExtent);
        Assert.Equal(
            ScrollCacheExtent.Pixels(200),
            new PageView(allowImplicitScrolling: true, scrollCacheExtent: ScrollCacheExtent.Pixels(200))
                .ScrollCacheExtent);
    }

    [Fact]
    public void PageView_AssertsWhenScrollCacheExtentAndImplicitScrollingDisagree()
    {
        Assert.Throws<ArgumentException>(() => new PageView(
            allowImplicitScrolling: true,
            scrollCacheExtent: ScrollCacheExtent.Viewport(0.0)));
        Assert.Throws<ArgumentException>(() => new PageView(
            allowImplicitScrolling: false,
            scrollCacheExtent: ScrollCacheExtent.Viewport(2.0)));
    }

    [Fact]
    public void PageController_DefaultsMatchFlutter()
    {
        var controller = new PageController();

        Assert.Equal(0, controller.InitialPage);
        Assert.True(controller.KeepPage);
        Assert.Equal(1.0, controller.ViewportFraction);
        Assert.True(controller.KeepScrollOffset);
        Assert.False(controller.HasClients);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageController(viewportFraction: 0.0));
        controller.Dispose();
    }

    [Fact]
    public void PageController_CannotReadPageWhileUnattached()
    {
        var controller = new PageController();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => controller.Page);
        Assert.Equal(
            "PageController.page cannot be accessed before a PageView is built with it.",
            error.Message);
        controller.Dispose();
    }

    [Fact]
    public void PageController_AnimateAndJumpAssertWhenNotAttached()
    {
        var controller = new PageController();
        Assert.Equal(
            "PageController is not attached to a PageView.",
            Assert.Throws<InvalidOperationException>(
                () => { _ = controller.AnimateToPage(1, TimeSpan.FromMilliseconds(50)); }).Message);
        Assert.Equal(
            "PageController is not attached to a PageView.",
            Assert.Throws<InvalidOperationException>(() => controller.JumpToPage(1)).Message);
        controller.Dispose();
    }

    [Fact]
    public void PageController_AssertsWhenMultiplePageViewsShareIt()
    {
        var controller = new PageController();
        using var harness = Harness(new Column(children:
        [
            new SizedBox(width: 800, height: 300, child: Pages(controller, 3)),
            new SizedBox(width: 800, height: 300, child: Pages(controller, 3)),
        ]));
        harness.Pump(Viewport);

        Assert.Equal(
            "Multiple PageViews are attached to the same PageController.",
            Assert.Throws<InvalidOperationException>(() => controller.JumpToPage(1)).Message);
        Assert.Equal(
            "Multiple PageViews are attached to the same PageController.",
            Assert.Throws<InvalidOperationException>(
                () => { _ = controller.AnimateToPage(1, TimeSpan.FromMilliseconds(50)); }).Message);
        Assert.Equal(
            "The page property cannot be read when multiple PageViews are attached to the same "
            + "PageController.",
            Assert.Throws<InvalidOperationException>(() => controller.Page).Message);
        controller.Dispose();
    }

    [Fact]
    public void PageMetrics_PageClampsPixelsIntoRange()
    {
        var metrics = new PageMetrics(
            minScrollExtent: 100,
            maxScrollExtent: 200,
            pixels: 150,
            viewportDimension: 25,
            axisDirection: AxisDirection.Right,
            viewportFraction: 1.0,
            devicePixelRatio: 1.0);

        Assert.Equal(6.0, metrics.Page!.Value, precision: 6);
        Assert.Equal(4.0, metrics.CopyWith(pixels: 50).Page!.Value, precision: 6);
    }

    // ------------------------------------------------------------------ layout and geometry

    [Fact]
    public void PageView_InitialPageIsVisibleOnTheFirstLayout()
    {
        var controller = new PageController(initialPage: 4);
        using var harness = Harness(Pages(controller, 6));
        harness.Pump(Viewport);

        Assert.Equal(4.0, controller.Page);
        Assert.Equal(3200, Position(controller).Pixels, precision: 3);
        Assert.Equal(0, PageLeft(Single(harness)), precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ViewportFractionBelowOnePadsBothEnds()
    {
        var controller = new PageController(viewportFraction: 7.0 / 8.0);
        using var harness = Harness(Pages(controller, 12));
        harness.Pump(Viewport);

        List<RenderBox> pages = LaidOutPages(harness);
        Assert.All(pages, page => Assert.Equal(700, page.Size.Width, precision: 3));
        Assert.Equal([50, 750], pages.Select(page => Math.Round(PageLeft(page), 3)));

        controller.JumpToPage(10);
        harness.Pump(Viewport);
        Assert.Equal([-650, 50, 750], LaidOutPages(harness).Select(page => Math.Round(PageLeft(page), 3)));
        controller.Dispose();
    }

    [Fact]
    public void PageView_SmallViewportFractionShowsEveryVisiblePage()
    {
        var controller = new PageController(viewportFraction: 1.0 / 8.0);
        using var harness = Harness(Pages(controller, 20));
        harness.Pump(Viewport);

        Assert.Equal(
            [350, 450, 550, 650, 750],
            LaidOutPages(harness).Select(page => Math.Round(PageLeft(page), 3)));
        controller.Dispose();
    }

    [Fact]
    public void PageView_LargeViewportFractionCentersThroughTheInitialPageOffset()
    {
        var controller = new PageController(viewportFraction: 5.0 / 4.0);
        using var harness = Harness(Pages(controller, 3));
        harness.Pump(Viewport);

        // _initialPageOffset shifts the min scroll extent so page 0 is centered: 800 * 0.25 / 2.
        Assert.Equal(100, Position(controller).MinScrollExtent, precision: 3);
        RenderBox first = LaidOutPages(harness)[0];
        Assert.Equal(1000, first.Size.Width, precision: 3);
        Assert.Equal(-100, PageLeft(first), precision: 3);

        controller.JumpToPage(2);
        harness.Pump(Viewport);
        Assert.Equal(-100, PageLeft(LaidOutPages(harness)[^1]), precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_PadEndsFalseRemovesTheEndPadding()
    {
        var controller = new PageController(viewportFraction: 0.5);
        using var harness = Harness(Pages(controller, 4, padEnds: false));
        harness.Pump(Viewport);

        Assert.Equal(0, PageLeft(LaidOutPages(harness)[0]), precision: 3);
        Assert.False(new PageView(padEnds: false).PadEnds);
        controller.Dispose();
    }

    [Fact]
    public void PageView_KeepsTheSamePageWhenTheViewportResizes()
    {
        var controller = new PageController(initialPage: 2);
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);
        Assert.Equal(2.0, controller.Page);

        harness.Pump(new Size(450, 400));
        Assert.Equal(2.0, controller.Page);
        Assert.Equal(900, Position(controller).Pixels, precision: 3);

        harness.Pump(new Size(250, 100));
        Assert.Equal(2.0, controller.Page);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ZeroSizeViewportKeepsThePageAndRestoresItOnResize()
    {
        var controller = new PageController(initialPage: 3);
        using var harness = Harness(Pages(controller, 6));
        harness.Pump(new Size(0, 0));

        Assert.Equal(3.0, controller.Page);
        harness.Pump(Viewport);
        Assert.Equal(3.0, controller.Page);
        Assert.Equal(2400, Position(controller).Pixels, precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ControllerChangesThePageWhileTheViewportIsZeroSized()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 6));
        harness.Pump(new Size(0, 0));

        controller.JumpToPage(2);
        Assert.Equal(2.0, controller.Page);
        _ = controller.AnimateToPage(4, TimeSpan.FromMilliseconds(100));
        Assert.Equal(4.0, controller.Page);

        harness.Pump(Viewport);
        Assert.Equal(4.0, controller.Page);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ChangingTheControllerViewportFractionRelaysOutTheSamePage()
    {
        var wide = new PageController(viewportFraction: 5.0 / 4.0);
        using var harness = Harness(Pages(wide, 12));
        harness.Pump(Viewport);

        // Page 0 is centered: 800 * 0.25 / 2 to the left of the viewport.
        Assert.Equal(-100, PageLeft(LaidOutPages(harness)[0]), precision: 3);

        var wider = new PageController(viewportFraction: 4.0);
        harness.Replace(Pages(wider, 12));
        harness.Pump(Viewport);
        wider.JumpToPage(10);
        harness.Pump(Viewport);

        // The new fraction re-lays the pages out: -(4 - 1) * 800 / 2.
        Assert.Equal(10.0, wider.Page);
        Assert.Equal(-1200, PageLeft(LaidOutPages(harness)[0]), precision: 3);
        wide.Dispose();
        wider.Dispose();
    }

    [Fact]
    public void PageView_HandlesRoundingErrorWhenReadingThePage()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);

        Position(controller).JumpTo(799.99999999999);
        Assert.Equal(1.0, controller.Page);
        controller.Dispose();
    }

    [Fact]
    public void PageView_SurvivesFractionalViewportWidthsAndFarJumps()
    {
        var controller = new PageController(initialPage: 152);
        using var harness = Harness(PagesBuilder(controller, 366));
        harness.Pump(new Size(392.72727272727275, 800));

        controller.JumpToPage(365);
        harness.Pump(new Size(392.72727272727275, 800));
        Assert.Equal(365.0, controller.Page!.Value, 6);
        controller.Dispose();
    }

    [Fact]
    public void PageView_VerticalAndReverseResolveTheAxisDirection()
    {
        var vertical = new PageController(initialPage: 1);
        using var verticalHarness = Harness(Pages(vertical, 3, scrollDirection: Axis.Vertical));
        verticalHarness.Pump(Viewport);
        Assert.Equal(0, PageTop(Single(verticalHarness)), precision: 3);
        Assert.Equal(600, Position(vertical).Pixels, precision: 3);

        var reversed = new PageController(initialPage: 1);
        using var reversedHarness = Harness(Pages(reversed, 3, reverse: true));
        reversedHarness.Pump(Viewport);
        Assert.Equal(0, PageLeft(Single(reversedHarness)), precision: 3);
        vertical.Dispose();
        reversed.Dispose();
    }

    // ------------------------------------------------------------------ laziness

    [Fact]
    public void PageView_BuildsOnlyTheVisiblePageByDefault()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);

        Assert.Single(LaidOutPages(harness));
        controller.Dispose();
    }

    [Fact]
    public void PageView_AllowImplicitScrollingBuildsTheNeighborOffstage()
    {
        var controller = new PageController(initialPage: 1);
        using var harness = Harness(Pages(controller, 5, allowImplicitScrolling: true));
        harness.Pump(Viewport);

        Assert.Equal(3, LaidOutPages(harness).Count);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ScrollCacheExtentWidensTheBuiltWindow()
    {
        var oneViewport = new PageController();
        using var narrow = Harness(Pages(
            oneViewport,
            5,
            allowImplicitScrolling: true,
            scrollCacheExtent: ScrollCacheExtent.Viewport(1.0)));
        narrow.Pump(Viewport);
        Assert.Equal(2, LaidOutPages(narrow).Count);

        var twoViewports = new PageController();
        using var wide = Harness(Pages(
            twoViewports,
            5,
            allowImplicitScrolling: true,
            scrollCacheExtent: ScrollCacheExtent.Viewport(2.0)));
        wide.Pump(Viewport);
        Assert.Equal(3, LaidOutPages(wide).Count);

        oneViewport.Dispose();
        twoViewports.Dispose();
    }

    // ------------------------------------------------------------------ page reporting

    /// <remarks>
    /// Dart parity: <c>page_view_test.dart</c>'s "PageView showOnScreen scrolls when
    /// allowImplicitScrolling is true". The reveal is the viewport's, not the page view's — the page
    /// view only widens the cache extent so the neighbour has a render object to reveal.
    /// </remarks>
    [Fact]
    public void PageView_ShowOnScreenRevealsACachedPage()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(
            controller,
            4,
            allowImplicitScrolling: true,
            scrollCacheExtent: ScrollCacheExtent.Viewport(1.0)));
        harness.Pump(Viewport);

        List<RenderBox> pages = LaidOutPages(harness);
        Assert.Equal(2, pages.Count);
        Assert.Equal(0.0, controller.Page);

        pages[1].ShowOnScreen();
        Settle(harness);

        Assert.Equal(1.0, controller.Page!.Value, precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_NotificationMetricsAreThePageMetricsSubclass()
    {
        IScrollMetrics? captured = null;
        var controller = new PageController(viewportFraction: 0.5);
        using var harness = Harness(new NotificationListener<ScrollUpdateNotification>(
            onNotification: notification =>
            {
                captured ??= notification.Metrics;
                return false;
            },
            child: Pages(controller, 5)));
        harness.Pump(Viewport);

        Position(controller).JumpTo(400);
        harness.Pump(Viewport);

        var metrics = Assert.IsType<PageMetrics>(captured);
        Assert.Equal(0.5, metrics.ViewportFraction);
        Assert.Equal(1.0, metrics.Page!.Value, precision: 3);

        // Dart's PageMetrics.copyWith keeps the fraction unless it is overridden.
        Assert.Equal(0.5, metrics.CopyWith(pixels: 0.0).ViewportFraction);
        Assert.Equal(0.25, metrics.CopyWith(viewportFraction: 0.25).ViewportFraction);
        Assert.Equal(0.0, metrics.CopyWith(pixels: 0.0).Page!.Value, precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ReportsThePageChangeOnceAtTheHalfwayPoint()
    {
        var reported = new List<int>();
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5, onPageChanged: reported.Add));
        harness.Pump(Viewport);
        Assert.Empty(reported);

        Position(controller).JumpTo(380);
        harness.Pump(Viewport);
        Assert.Empty(reported);

        Position(controller).JumpTo(420);
        harness.Pump(Viewport);
        Assert.Equal([1], reported);

        Position(controller).JumpTo(600);
        harness.Pump(Viewport);
        Assert.Equal([1], reported);
        controller.Dispose();
    }

    [Fact]
    public void PageView_ReportsThePageChangeForADragAcrossTheHalfwayPoint()
    {
        GestureBinding.Instance.ResetForTests();
        var reported = new List<int>();
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5, onPageChanged: reported.Add));
        harness.Pump(Viewport);

        Drag(harness, from: new Point(700, 300), to: new Point(240, 300));
        harness.Pump(Viewport);

        Assert.Equal([1], reported);
        controller.Dispose();
    }

    [Fact]
    public void PageView_SnapsToAWholePageAfterAFling()
    {
        GestureBinding.Instance.ResetForTests();
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);

        Drag(harness, from: new Point(700, 300), to: new Point(240, 300));
        Settle(harness);

        Assert.Equal(1.0, controller.Page!.Value, 3);
        Assert.Equal(800, Position(controller).Pixels, precision: 3);
        controller.Dispose();
    }

    [Fact]
    public void PageView_WithSnappingDisabledStaysBetweenPages()
    {
        GestureBinding.Instance.ResetForTests();
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5, pageSnapping: false));
        harness.Pump(Viewport);

        Drag(harness, from: new Point(700, 300), to: new Point(400, 300));
        Settle(harness);

        Assert.InRange(Position(controller).Pixels, 1, 799);
        controller.Dispose();
    }

    // ------------------------------------------------------------------ controller lifecycle

    [Fact]
    public void PageController_OnAttachAndOnDetachFireOnce()
    {
        int attached = 0;
        int detached = 0;
        var controller = new PageController(
            onAttach: _ => attached++,
            onDetach: _ => detached++);
        var harness = Harness(Pages(controller, 3));
        harness.Pump(Viewport);
        Assert.Equal(1, attached);
        Assert.Equal(0, detached);

        harness.Dispose();
        Assert.Equal(1, attached);
        Assert.Equal(1, detached);
        controller.Dispose();
    }

    [Fact]
    public void PageView_SwappingTheControllerHandsControlToTheNewOne()
    {
        var first = new PageController();
        using var harness = Harness(Pages(null, 5));
        harness.Pump(Viewport);

        harness.Replace(Pages(first, 5));
        harness.Pump(Viewport);
        first.JumpToPage(2);
        harness.Pump(Viewport);
        Assert.Equal(2.0, first.Page);

        var second = new PageController();
        harness.Replace(Pages(second, 5));
        harness.Pump(Viewport);
        Assert.Throws<InvalidOperationException>(() => first.JumpToPage(1));
        second.JumpToPage(3);
        harness.Pump(Viewport);
        Assert.Equal(3.0, second.Page);

        harness.Replace(Pages(null, 5));
        harness.Pump(Viewport);
        Assert.Throws<InvalidOperationException>(() => second.JumpToPage(1));
        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public void PageController_NextAndPreviousPageCompleteWhenTheAnimationEnds()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);

        Task forward = controller.NextPage(TimeSpan.FromMilliseconds(300));
        Assert.False(forward.IsCompleted);
        Settle(harness);
        Assert.True(forward.IsCompleted);
        Assert.Equal(1.0, controller.Page!.Value, 3);

        Task back = controller.PreviousPage(TimeSpan.FromMilliseconds(300));
        Settle(harness);
        Assert.True(back.IsCompleted);
        Assert.Equal(0.0, controller.Page!.Value, 3);
        controller.Dispose();
    }

    // ------------------------------------------------------------------ page storage

    [Fact]
    public void PageView_RestoresItsPageThroughPageStorage()
    {
        var bucket = new PageStorageBucket();
        var controller = new PageController();
        using var harness = Harness(new PageStorage(
            bucket,
            Pages(controller, 5, key: new PageStorageKey<string>("pages"))));
        harness.Pump(Viewport);
        controller.JumpToPage(2);
        harness.Pump(Viewport);
        Assert.Equal(2.0, controller.Page);

        var restoredController = new PageController();
        using var restored = Harness(new PageStorage(
            bucket,
            Pages(restoredController, 5, key: new PageStorageKey<string>("pages"))));
        restored.Pump(Viewport);
        Assert.Equal(2.0, restoredController.Page);

        var freshController = new PageController(keepPage: false);
        using var fresh = Harness(new PageStorage(
            bucket,
            Pages(freshController, 5, key: new PageStorageKey<string>("pages"))));
        fresh.Pump(Viewport);
        Assert.Equal(0.0, freshController.Page);

        controller.Dispose();
        restoredController.Dispose();
        freshController.Dispose();
    }

    // ------------------------------------------------------------------ physics

    [Fact]
    public void PageScrollPhysics_ForbidsImplicitScrollingAndChainsThroughApplyTo()
    {
        var physics = new PageScrollPhysics();
        Assert.False(physics.AllowImplicitScrolling);

        var clamping = new ClampingScrollPhysics();
        ScrollPhysics chained = physics.ApplyTo(clamping);
        Assert.IsType<PageScrollPhysics>(chained);
        Assert.Same(clamping, chained.Parent);
    }

    [Fact]
    public void PageScrollPhysics_TargetsTheNearestPageAndBiasesWithVelocity()
    {
        var controller = new PageController();
        using var harness = Harness(Pages(controller, 5));
        harness.Pump(Viewport);
        ScrollPosition position = Position(controller);
        var physics = new PageScrollPhysics(new ClampingScrollPhysics());

        position.JumpTo(300);
        Simulation? settleBack = physics.CreateBallisticSimulation(position, 0.0);
        Assert.NotNull(settleBack);
        Assert.Equal(0, settleBack!.X(10), precision: 1);

        Simulation? flingForward = physics.CreateBallisticSimulation(position, 2000.0);
        Assert.NotNull(flingForward);
        Assert.Equal(800, flingForward!.X(10), precision: 1);
        controller.Dispose();
    }

    [Fact]
    public void ForceImplicitScrollPhysics_OverridesTheImplicitScrollingFlagThroughTheChain()
    {
        ScrollPhysics physics = new ForceImplicitScrollPhysics(true)
            .ApplyTo(new PageScrollPhysics().ApplyTo(new ClampingScrollPhysics()));

        Assert.True(physics.AllowImplicitScrolling);
        Assert.IsType<PageScrollPhysics>(physics.Parent);
        Assert.IsType<ClampingScrollPhysics>(physics.Parent!.Parent);
    }

    // ------------------------------------------------------------------ sample

    [Fact]
    public void PageViewDemoPage_RendersLazyPagesAtDesktopSize()
    {
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new Material.Theme(Material.ThemeData.Light, new PageViewDemoPage())));
        harness.Pump(new Size(1000, 700));

        // viewportFraction 0.85 pads both ends, so the leading neighbor is partly visible too.
        Assert.Equal(2, LaidOutPages(harness).Count);
    }

    // ------------------------------------------------------------------ helpers

    private static Widget Pages(
        PageController? controller,
        int count,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        bool pageSnapping = true,
        bool padEnds = true,
        bool allowImplicitScrolling = false,
        ScrollCacheExtent? scrollCacheExtent = null,
        Action<int>? onPageChanged = null,
        Key? key = null)
    {
        return new PageView(
            children: Enumerable.Range(0, count)
                .Select(index => (Widget)new SizedBox(child: new ColoredBox(Colors.Red)))
                .ToArray(),
            controller: controller,
            scrollDirection: scrollDirection,
            reverse: reverse,
            pageSnapping: pageSnapping,
            padEnds: padEnds,
            allowImplicitScrolling: allowImplicitScrolling,
            scrollCacheExtent: scrollCacheExtent,
            onPageChanged: onPageChanged,
            key: key);
    }

    private static Widget PagesBuilder(PageController controller, int count) => PageView.Builder(
        itemBuilder: (_, _) => new ColoredBox(Colors.Green),
        itemCount: count,
        controller: controller);

    private static PagePosition Position(PageController controller) =>
        Assert.IsType<PagePosition>(Assert.Single(controller.Positions));

    private static RenderSliverFillViewport Fill(WidgetRenderHarness harness) =>
        Assert.IsType<RenderSliverFillViewport>(FindDescendant<RenderSliverFillViewport>(harness.RenderView));

    private static List<RenderBox> LaidOutPages(WidgetRenderHarness harness)
    {
        RenderSliverFillViewport fill = Fill(harness);
        var pages = new List<RenderBox>();
        for (RenderBox? child = fill.FirstChild; child != null; child = fill.ChildAfter(child))
        {
            pages.Add(child);
        }

        return pages;
    }

    private static RenderBox Single(WidgetRenderHarness harness) => Assert.Single(LaidOutPages(harness));

    private static double PageLeft(RenderBox page) => page.GetPaintOffsetToRoot().X;

    private static double PageTop(RenderBox page) => page.GetPaintOffsetToRoot().Y;

    private static void Drag(WidgetRenderHarness harness, Point from, Point to)
    {
        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            91, PointerDeviceKind.Touch, from, PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            91, PointerDeviceKind.Touch, to, PointerButtons.Primary, true, now.AddMilliseconds(40)));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            91, PointerDeviceKind.Touch, to, PointerButtons.None, now.AddMilliseconds(50)));
    }

    private static void Settle(WidgetRenderHarness harness)
    {
        double clock = Scheduler.CurrentSeconds;
        foreach (double step in new[] { 0.01, 0.2, 0.4, 0.8, 1.4, 2.4 })
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + step));
            harness.Pump(Viewport);
        }
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static WidgetRenderHarness Harness(Widget child) =>
        new(new Directionality(TextDirection.Ltr, child));

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Replace(Widget child)
        {
            _rootElement.Update(new Directionality(TextDirection.Ltr, child));
            _owner.FlushBuild();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

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

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
                {
                    _renderView.Child = null;
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
        }
    }
}
