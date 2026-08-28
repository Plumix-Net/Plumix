using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using System.Reflection;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/nested_scroll_view.dart (parity tests
// mirroring flutter/packages/flutter/test/widgets/nested_scroll_view_test.dart)

namespace Plumix.Tests;

/// <summary>
/// Covers <see cref="NestedScrollView"/>, its coordinator's split of a scroll between the outer and
/// inner positions, and the <see cref="SliverOverlapAbsorber"/>/<see cref="SliverOverlapInjector"/>
/// pair, against the behavior Flutter's own <c>nested_scroll_view_test.dart</c> asserts.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class NestedScrollViewTests : IDisposable
{
    private const double ViewportExtent = 600.0;
    private const double HeaderExtent = 200.0;

    // The coordinator schedules its shadow updates as post-frame callbacks, so a test must not leave
    // any of them queued on the process-wide scheduler for the next one to run.
    public NestedScrollViewTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    // ---------------------------------------------------------------------------------------------
    // SliverOverlapAbsorberHandle
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void OverlapAbsorberHandle_StartsEmptyAndOrphan()
    {
        var handle = new SliverOverlapAbsorberHandle();
        Assert.Null(handle.LayoutExtent);
        Assert.Null(handle.ScrollExtent);
        Assert.Equal("SliverOverlapAbsorberHandle(, orphan)", handle.ToString());
    }

    [Fact]
    public void OverlapAbsorber_ReportsTheChildsObstructionExtentAndRemovesItFromTheGeometry()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var child = new StubSliver(new SliverGeometry(
            ScrollExtent: 200,
            PaintExtent: 120,
            LayoutExtent: 120,
            MaxPaintExtent: 200,
            MaxScrollObstructionExtent: 56));
        var absorber = new RenderSliverOverlapAbsorber(handle, child);
        Attach(absorber);

        absorber.LayoutWithSliverConstraints(Constraints());

        // The absorbed obstruction leaves the outer view's scroll and layout extents.
        Assert.Equal(144, absorber.Geometry.ScrollExtent, precision: 6);
        Assert.Equal(64, absorber.Geometry.LayoutExtent, precision: 6);
        // Everything else is the child's geometry unchanged.
        Assert.Equal(120, absorber.Geometry.PaintExtent, precision: 6);
        Assert.Equal(200, absorber.Geometry.MaxPaintExtent, precision: 6);
        Assert.Equal(56, absorber.Geometry.MaxScrollObstructionExtent, precision: 6);
        Assert.Equal(56, handle.LayoutExtent);
        Assert.Equal(56, handle.ScrollExtent);
        Assert.Equal("SliverOverlapAbsorberHandle(56)", handle.ToString());
    }

    [Fact]
    public void OverlapAbsorber_LayoutExtentNeverGoesNegative()
    {
        var handle = new SliverOverlapAbsorberHandle();
        // A pinned header that has been scrolled away paints less than it obstructs.
        var child = new StubSliver(new SliverGeometry(
            ScrollExtent: 56,
            PaintExtent: 20,
            MaxPaintExtent: 56,
            MaxScrollObstructionExtent: 56));
        var absorber = new RenderSliverOverlapAbsorber(handle, child);
        Attach(absorber);

        absorber.LayoutWithSliverConstraints(Constraints());

        Assert.Equal(0, absorber.Geometry.ScrollExtent, precision: 6);
        Assert.Equal(0, absorber.Geometry.LayoutExtent, precision: 6);
    }

    [Fact]
    public void OverlapAbsorber_WithoutAChildProducesZeroGeometry()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var absorber = new RenderSliverOverlapAbsorber(handle);
        Attach(absorber);

        absorber.LayoutWithSliverConstraints(Constraints());

        Assert.Equal(new SliverGeometry(), absorber.Geometry);
        Assert.Null(handle.LayoutExtent);
    }

    [Fact]
    public void OverlapAbsorber_RejectsAHandleSharedWithAnotherAbsorber()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var first = new RenderSliverOverlapAbsorber(handle, new StubSliver(new SliverGeometry()));
        var second = new RenderSliverOverlapAbsorber(handle, new StubSliver(new SliverGeometry()));
        Attach(first);
        Attach(second);

        Assert.Equal(2, CountWriters(handle));
        Assert.Contains("2 WRITERS ASSIGNED", handle.ToString());
        Assert.Throws<InvalidOperationException>(
            () => first.LayoutWithSliverConstraints(Constraints()));
    }

    [Fact]
    public void OverlapAbsorber_MovingToANewHandleTransfersTheExtentsAndTheWriter()
    {
        var first = new SliverOverlapAbsorberHandle();
        var second = new SliverOverlapAbsorberHandle();
        var absorber = new RenderSliverOverlapAbsorber(
            first,
            new StubSliver(new SliverGeometry(
                ScrollExtent: 100,
                PaintExtent: 100,
                MaxPaintExtent: 100,
                MaxScrollObstructionExtent: 40)));
        Attach(absorber);
        absorber.LayoutWithSliverConstraints(Constraints());

        absorber.Handle = second;

        Assert.Equal(0, CountWriters(first));
        Assert.Equal(1, CountWriters(second));
        Assert.Equal(40, second.LayoutExtent);
        Assert.Equal(40, second.ScrollExtent);
    }

    // ---------------------------------------------------------------------------------------------
    // SliverOverlapInjector
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void OverlapInjector_WithoutAnAbsorberFailsWithFluttersMessage()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var injector = new RenderSliverOverlapInjector(handle);
        Attach(injector);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => injector.LayoutWithSliverConstraints(Constraints()));
        Assert.Contains("SliverOverlapInjector has found no absorbed extent to inject.", error.Message);
    }

    [Fact]
    public void OverlapInjector_ReproducesTheAbsorbedExtent()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var absorber = new RenderSliverOverlapAbsorber(
            handle,
            new StubSliver(new SliverGeometry(
                ScrollExtent: 56,
                PaintExtent: 56,
                MaxPaintExtent: 56,
                MaxScrollObstructionExtent: 56)));
        Attach(absorber);
        absorber.LayoutWithSliverConstraints(Constraints());

        var injector = new RenderSliverOverlapInjector(handle);
        Attach(injector);
        injector.LayoutWithSliverConstraints(Constraints());

        Assert.Equal(56, injector.Geometry.ScrollExtent, precision: 6);
        Assert.Equal(56, injector.Geometry.PaintExtent, precision: 6);
        Assert.Equal(56, injector.Geometry.LayoutExtent, precision: 6);
        Assert.Equal(56, injector.Geometry.MaxPaintExtent, precision: 6);

        // Scrolled halfway through, the injected gap keeps its scroll extent but lays out less.
        injector.LayoutWithSliverConstraints(Constraints(scrollOffset: 20));
        Assert.Equal(56, injector.Geometry.ScrollExtent, precision: 6);
        Assert.Equal(56, injector.Geometry.PaintExtent, precision: 6);
        Assert.Equal(36, injector.Geometry.LayoutExtent, precision: 6);

        // Scrolled past, the layout extent clamps at zero.
        injector.LayoutWithSliverConstraints(Constraints(scrollOffset: 80));
        Assert.Equal(0, injector.Geometry.LayoutExtent, precision: 6);
    }

    [Fact]
    public void OverlapInjector_ClampsToTheRemainingPaintExtent()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var absorber = new RenderSliverOverlapAbsorber(
            handle,
            new StubSliver(new SliverGeometry(
                ScrollExtent: 100,
                PaintExtent: 100,
                MaxPaintExtent: 100,
                MaxScrollObstructionExtent: 100)));
        Attach(absorber);
        absorber.LayoutWithSliverConstraints(Constraints());

        var injector = new RenderSliverOverlapInjector(handle);
        Attach(injector);
        injector.LayoutWithSliverConstraints(Constraints(remainingPaintExtent: 30));

        Assert.Equal(30, injector.Geometry.PaintExtent, precision: 6);
        Assert.Equal(30, injector.Geometry.LayoutExtent, precision: 6);
        Assert.Equal(100, injector.Geometry.ScrollExtent, precision: 6);
    }

    [Fact]
    public void OverlapInjector_RelaysOutWhenTheHandleAnnouncesANewLayout()
    {
        var handle = new SliverOverlapAbsorberHandle();
        var absorber = new RenderSliverOverlapAbsorber(
            handle,
            new StubSliver(new SliverGeometry(
                ScrollExtent: 10,
                MaxPaintExtent: 10,
                MaxScrollObstructionExtent: 10)));
        Attach(absorber);
        absorber.LayoutWithSliverConstraints(Constraints());
        var injector = new RenderSliverOverlapInjector(handle);
        Attach(injector);
        injector.LayoutWithSliverConstraints(Constraints());
        Assert.False(NeedsLayout(injector));

        var viewport = new RenderNestedScrollViewViewport(
            new ScrollPosition(new ClampingScrollPhysics(), new TestScrollContext()),
            handle);
        viewport.MarkNeedsLayout();

        Assert.True(NeedsLayout(injector));
    }

    // ---------------------------------------------------------------------------------------------
    // NestedScrollView composition and controllers
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void NestedScrollView_ExposesFlutterDefaults()
    {
        var view = new NestedScrollView(
            headerSliverBuilder: (_, _) => [],
            body: new SizedBox());

        Assert.Null(view.Controller);
        Assert.Equal(Axis.Vertical, view.ScrollDirection);
        Assert.False(view.Reverse);
        Assert.Null(view.Physics);
        Assert.Equal(DragStartBehavior.Start, view.DragStartBehavior);
        Assert.False(view.FloatHeaderSlivers);
        Assert.Equal(Clip.HardEdge, view.ClipBehavior);
        Assert.Equal(HitTestBehavior.Opaque, view.HitTestBehavior);
        Assert.Null(view.RestorationId);
        Assert.Null(view.ScrollBehavior);
    }

    [Fact]
    public void SliverOverlapAbsorberHandleFor_RequiresANestedScrollViewAncestor()
    {
        Exception? captured = null;
        var harness = new WidgetRenderHarness(new Builder(context =>
        {
            try
            {
                NestedScrollView.SliverOverlapAbsorberHandleFor(context);
            }
            catch (InvalidOperationException error)
            {
                captured = error;
            }

            return new SizedBox();
        }));
        harness.Pump(new Size(800, ViewportExtent));

        Assert.NotNull(captured);
        Assert.Contains("must be called with a context that contains a NestedScrollView", captured.Message);
    }

    [Fact]
    public void SliverOverlapAbsorberHandleFor_ReturnsTheStatesHandle()
    {
        SliverOverlapAbsorberHandle? seen = null;
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(
            key: key,
            header: context =>
            {
                seen = NestedScrollView.SliverOverlapAbsorberHandleFor(context);
                return [FixedHeader()];
            }));
        harness.Pump(new Size(800, ViewportExtent));

        Assert.NotNull(seen);
        Assert.Same(seen, HandleOf(key.CurrentState!));
    }

    [Fact]
    public void OuterControllerMatchesTheSuppliedController()
    {
        var controller = new ScrollController();
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key, controller: controller));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, -20.0);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(20.0, controller.Offset, precision: 6);
        Assert.Equal(controller.Offset, key.CurrentState!.OuterController.Offset, precision: 6);
    }

    [Fact]
    public void ControllerInitialScrollOffset_SeedsTheOuterPosition()
    {
        var controller = new ScrollController(initialScrollOffset: 50.0);
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key, controller: controller));
        harness.Pump(new Size(800, ViewportExtent));

        ScrollPosition outer = key.CurrentState!.OuterController.Position;
        Assert.Equal(0.0, outer.MinScrollExtent, precision: 6);
        Assert.Equal(50.0, outer.Pixels, precision: 6);
        Assert.Equal(HeaderExtent, outer.MaxScrollExtent, precision: 6);
    }

    // ---------------------------------------------------------------------------------------------
    // The outer/inner split
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void ScrollingByLessThanTheOuterExtentDoesNotScrollTheInnerBody()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, -(HeaderExtent - 50.0));
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(HeaderExtent - 50.0, key.CurrentState!.OuterController.Offset, precision: 6);
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    [Fact]
    public void ScrollingByExactlyTheOuterExtentDoesNotScrollTheInnerBody()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, -HeaderExtent);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(HeaderExtent, key.CurrentState!.OuterController.Offset, precision: 6);
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    [Fact]
    public void ScrollingByMoreThanTheOuterExtentHandsTheRemainderToTheInnerBody()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, -(HeaderExtent + 50.0));
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(HeaderExtent, key.CurrentState!.OuterController.Offset, precision: 6);
        Assert.Equal(50.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    [Fact]
    public void DraggingBackScrollsTheInnerBodyBeforeTheHeaderReturns()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));
        Drag(key, -(HeaderExtent + 100.0));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, 60.0);
        harness.Pump(new Size(800, ViewportExtent));

        // The body unwinds first; the header only comes back once the body is at its leading edge.
        Assert.Equal(HeaderExtent, key.CurrentState!.OuterController.Offset, precision: 6);
        Assert.Equal(40.0, key.CurrentState.InnerController.Offset, precision: 6);

        Drag(key, 100.0);
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);
        Assert.Equal(HeaderExtent - 60.0, key.CurrentState.OuterController.Offset, precision: 6);
    }

    [Fact]
    public void FloatHeaderSlivers_FloatsTheHeaderInBeforeTheBodyUnwinds()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(
            BuildNestedScrollView(key: key, floatHeaderSlivers: true));
        harness.Pump(new Size(800, ViewportExtent));
        Drag(key, -(HeaderExtent + 100.0));
        harness.Pump(new Size(800, ViewportExtent));

        Drag(key, 60.0);
        harness.Pump(new Size(800, ViewportExtent));

        // With floating headers the outer view takes the drag first.
        Assert.Equal(HeaderExtent - 60.0, key.CurrentState!.OuterController.Offset, precision: 6);
        Assert.Equal(100.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    [Fact]
    public void NeverScrollableScrollPhysics_RefusesDragsWhenTheBodyDoesNotScroll()
    {
        // Flutter's regression test uses a non-scrolling body, so the outer physics alone decide.
        var key = NewKey();
        var harness = new WidgetRenderHarness(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new NestedScrollView(
                key: key,
                physics: new NeverScrollableScrollPhysics(),
                headerSliverBuilder: (_, _) => [FixedHeader()],
                body: new SizedBox())));
        harness.Pump(new Size(800, ViewportExtent));

        ScrollPosition outer = key.CurrentState!.OuterController.Position;
        Assert.False(outer.Physics.ShouldAcceptUserOffset(outer));
    }

    // ---------------------------------------------------------------------------------------------
    // Programmatic control
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void InnerControllerJumpTo_DrivesTheOuterViewToItsMaximumFirst()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        key.CurrentState!.InnerController.JumpTo(100.0);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(HeaderExtent, key.CurrentState.OuterController.Offset, precision: 6);
        Assert.Equal(100.0, key.CurrentState.InnerController.Offset, precision: 6);

        // Returning the body to its leading edge leaves the header collapsed.
        key.CurrentState.InnerController.JumpTo(0.0);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(HeaderExtent, key.CurrentState.OuterController.Offset, precision: 6);
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    [Fact]
    public void OuterControllerJumpTo_LeavesTheInnerBodyAlone()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        key.CurrentState!.OuterController.JumpTo(100.0);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(100.0, key.CurrentState.OuterController.Offset, precision: 6);
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);
    }

    // ---------------------------------------------------------------------------------------------
    // Pointer signals
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void PointerScroll_MovesTheOuterViewAndNeverOverscrolls()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(key: key));
        harness.Pump(new Size(800, ViewportExtent));

        PointerScroll(key, 20.0);
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal(20.0, key.CurrentState!.OuterController.Offset, precision: 6);

        PointerScroll(key, -40.0);
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal(0.0, key.CurrentState.OuterController.Offset, precision: 6);
        Assert.Equal(0.0, key.CurrentState.InnerController.Offset, precision: 6);

        // A huge scroll saturates the header and hands the rest to the body instead of detaching it.
        PointerScroll(key, 1000000.0);
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal(HeaderExtent, key.CurrentState.OuterController.Offset, precision: 6);
        double innerOffset = key.CurrentState.InnerController.Offset;
        Assert.True(innerOffset > 0.0);
        Assert.True(double.IsFinite(innerOffset));
    }

    [Fact]
    public void PointerScroll_DispatchesOneStartAndEndPerNestedPosition()
    {
        var key = NewKey();
        int starts = 0;
        int ends = 0;
        var harness = new WidgetRenderHarness(new NotificationListener<ScrollNotification>(
            onNotification: notification =>
            {
                switch (notification)
                {
                    case ScrollStartNotification:
                        starts += 1;
                        break;
                    case ScrollEndNotification:
                        ends += 1;
                        break;
                }

                return false;
            },
            child: BuildNestedScrollView(key: key)));
        harness.Pump(new Size(800, ViewportExtent));
        starts = 0;
        ends = 0;

        PointerScroll(key, 20.0);
        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(2, starts);
        Assert.Equal(2, ends);
    }

    // ---------------------------------------------------------------------------------------------
    // Shadow / user scroll direction
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void HeaderSliverBuilder_SeesInnerBoxIsScrolledOnlyOnceTheBodyMoves()
    {
        var key = NewKey();
        var seen = new List<bool>();
        var harness = new WidgetRenderHarness(BuildNestedScrollView(
            key: key,
            header: (_, isScrolled) =>
            {
                seen.Add(isScrolled);
                return [FixedHeader()];
            }));
        harness.Pump(new Size(800, ViewportExtent));
        Assert.All(seen, value => Assert.False(value));

        // Scrolling the header away alone does not count as a scrolled body.
        Drag(key, -HeaderExtent);
        harness.Pump(new Size(800, ViewportExtent));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(800, ViewportExtent));
        Assert.DoesNotContain(true, seen);

        Drag(key, -50.0);
        harness.Pump(new Size(800, ViewportExtent));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Contains(true, seen);
    }

    [Fact]
    public void UserScrollNotification_ReportsReverseThenIdleForADragAndBack()
    {
        var key = NewKey();
        var directions = new List<ScrollDirection>();
        var harness = new WidgetRenderHarness(new NotificationListener<UserScrollNotification>(
            onNotification: notification =>
            {
                if (notification.Depth == 0)
                {
                    directions.Add(notification.Direction);
                }

                return false;
            },
            child: BuildNestedScrollView(key: key)));
        harness.Pump(new Size(800, ViewportExtent));
        directions.Clear();

        ScrollDragController drag = StartDrag(key);
        drag.Update(new DragUpdateDetails(new Point(0, 0), new Point(0, 0), new Point(0, -20), -20));
        drag.End(new DragEndDetails(0.0));
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal([ScrollDirection.Reverse, ScrollDirection.Idle], directions);

        directions.Clear();
        drag = StartDrag(key);
        drag.Update(new DragUpdateDetails(new Point(0, 0), new Point(0, 0), new Point(0, 20), 20));
        drag.End(new DragEndDetails(0.0));
        harness.Pump(new Size(800, ViewportExtent));
        Assert.Equal([ScrollDirection.Forward, ScrollDirection.Idle], directions);
    }

    // ---------------------------------------------------------------------------------------------
    // Absorber / injector inside a live NestedScrollView
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void PinnedHeader_AbsorbsItsOverlapAndTheInjectorPushesTheBodyDown()
    {
        const double pinnedExtent = 56.0;
        var key = NewKey();
        var bodyKey = new LabeledGlobalKey<State>("nested-scroll-view-body");
        SliverOverlapAbsorberHandle? handle = null;
        var harness = new WidgetRenderHarness(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new NestedScrollView(
                key: key,
                headerSliverBuilder: (context, _) =>
                {
                    handle = NestedScrollView.SliverOverlapAbsorberHandleFor(context);
                    return
                    [
                        new SliverOverlapAbsorber(
                            handle: handle,
                            sliver: new SliverPersistentHeader(
                                pinned: true,
                                @delegate: new FixedHeaderDelegate(pinnedExtent))),
                    ];
                },
                body: new Builder(context => new CustomScrollView(
                [
                    new SliverOverlapInjector(
                        NestedScrollView.SliverOverlapAbsorberHandleFor(context)),
                    new SliverToBoxAdapter(new SizedBox(key: bodyKey, height: 2000)),
                ])))));

        harness.Pump(new Size(800, ViewportExtent));

        // The pinned header obstructs its whole extent, and the absorber takes it out of the outer
        // view: the outer view has nothing left to scroll.
        Assert.NotNull(handle);
        Assert.Equal(pinnedExtent, handle.LayoutExtent);
        Assert.Equal(pinnedExtent, handle.ScrollExtent);
        Assert.Equal(0.0, key.CurrentState!.OuterController.Position.MaxScrollExtent, precision: 6);

        // The injector reproduces the overlap inside the body, so the body's first box starts below
        // the header rather than under it.
        RenderObject body = bodyKey.CurrentContext!.Value.FindRenderObject()!;
        Assert.Equal(pinnedExtent, GlobalTopOf(body, harness.RenderView), precision: 6);
    }

    // ---------------------------------------------------------------------------------------------
    // Degenerate layouts
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void NestedScrollView_LaysOutAtZeroAreaWithoutFailing()
    {
        var key = NewKey();
        var harness = new WidgetRenderHarness(
            new SizedBox(width: 0, height: 0, child: BuildNestedScrollView(key: key)));

        harness.Pump(new Size(800, ViewportExtent));

        Assert.Equal(0.0, key.CurrentState!.OuterController.Offset, precision: 6);
    }

    [Fact]
    public void EmptyHeader_WithAShortBodyRefusesDragsAndWithALongBodyAcceptsThem()
    {
        foreach ((int itemCount, bool expected) in new[] { (1, false), (30, true) })
        {
            var key = NewKey();
            var harness = new WidgetRenderHarness(BuildNestedScrollView(
                key: key,
                header: (_, _) => [],
                bodyItemCount: itemCount));
            harness.Pump(new Size(800, ViewportExtent));

            ScrollPosition inner = key.CurrentState!.InnerController.Position;
            ScrollPosition outer = key.CurrentState.OuterController.Position;
            bool canDrag = outer.Physics.ShouldAcceptUserOffset(outer)
                           || inner.Physics.ShouldAcceptUserOffset(inner);
            Assert.Equal(expected, canDrag);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static int _keySeed;

    private static LabeledGlobalKey<NestedScrollViewState> NewKey()
    {
        return new LabeledGlobalKey<NestedScrollViewState>($"nested-scroll-view-{_keySeed++}");
    }

    private static Widget FixedHeader(double extent = HeaderExtent)
    {
        return new SliverToBoxAdapter(new SizedBox(height: extent));
    }

    private static Widget BuildNestedScrollView(
        LabeledGlobalKey<NestedScrollViewState> key,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        bool floatHeaderSlivers = false,
        int bodyItemCount = 30,
        NestedScrollViewHeaderSliversBuilder? header = null,
        Func<BuildContext, IReadOnlyList<Widget>>? headerFromContext = null)
    {
        header ??= headerFromContext != null
            ? (context, _) => headerFromContext(context)
            : (_, _) => [FixedHeader()];
        return new Directionality(
            textDirection: TextDirection.Ltr,
            child: new NestedScrollView(
                key: key,
                controller: controller,
                physics: physics,
                floatHeaderSlivers: floatHeaderSlivers,
                headerSliverBuilder: header,
                body: ListView.Builder(
                    itemCount: bodyItemCount,
                    itemBuilder: (_, _) => new SizedBox(height: 100))));
    }

    private static Widget BuildNestedScrollView(
        LabeledGlobalKey<NestedScrollViewState> key,
        Func<BuildContext, IReadOnlyList<Widget>> header)
    {
        return BuildNestedScrollView(key: key, headerFromContext: header);
    }

    private static SliverOverlapAbsorberHandle HandleOf(NestedScrollViewState state)
    {
        return (SliverOverlapAbsorberHandle)typeof(NestedScrollViewState)
            .GetProperty("AbsorberHandle", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state)!;
    }

    private static ScrollDragController StartDrag(LabeledGlobalKey<NestedScrollViewState> key)
    {
        ScrollPosition outer = key.CurrentState!.OuterController.Position;
        outer.Hold();
        return outer.Drag(new DragStartDetails(new Point(0, 0)));
    }

    private static void Drag(LabeledGlobalKey<NestedScrollViewState> key, double delta)
    {
        ScrollDragController drag = StartDrag(key);
        drag.Update(new DragUpdateDetails(
            new Point(0, 0),
            new Point(0, 0),
            new Point(0, delta),
            delta));
        drag.End(new DragEndDetails(0.0));
    }

    private static void PointerScroll(LabeledGlobalKey<NestedScrollViewState> key, double delta)
    {
        key.CurrentState!.OuterController.Position.ApplyPointerScrollDelta(delta);
    }

    private static SliverConstraints Constraints(
        double scrollOffset = 0,
        double remainingPaintExtent = ViewportExtent,
        double overlap = 0)
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: scrollOffset,
            RemainingPaintExtent: remainingPaintExtent,
            CrossAxisExtent: 800,
            ViewportMainAxisExtent: ViewportExtent,
            RemainingCacheExtent: remainingPaintExtent,
            Overlap: overlap);
    }

    private static void Attach(RenderObject renderObject)
    {
        var view = new RenderView();
        var pipeline = new PipelineOwner(view);
        renderObject.Attach(pipeline);
    }

    private static int CountWriters(SliverOverlapAbsorberHandle handle)
    {
        return (int)typeof(SliverOverlapAbsorberHandle)
            .GetField("Writers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(handle)!;
    }

    private static bool NeedsLayout(RenderObject renderObject)
    {
        return (bool)typeof(RenderObject)
            .GetField("_needsLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(renderObject)!;
    }

    private sealed class WidgetRenderHarness
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
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

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

    private static double GlobalTopOf(RenderObject target, RenderView view)
    {
        return RenderObject.TransformRect(target.GetTransformTo(view), target.PaintBounds).Y;
    }

    /// <summary>A persistent header that never shrinks, standing in for a pinned app bar.</summary>
    private sealed class FixedHeaderDelegate(double extent) : SliverPersistentHeaderDelegate
    {
        public override double MinExtent => extent;

        public override double MaxExtent => extent;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new SizedBox(height: extent);
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    /// <summary>A sliver that reports a fixed geometry, standing in for a real header.</summary>
    private sealed class StubSliver(SliverGeometry geometry) : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            Geometry = geometry;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
