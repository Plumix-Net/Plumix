using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/viewport.dart;
// flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart;
// flutter/packages/flutter/lib/src/widgets/scrollable.dart;
// flutter/packages/flutter/lib/src/widgets/scroll_position.dart (reveal-protocol parity tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ViewportRevealTests
{
    private const double Tolerance = 0.0001;

    [Fact]
    public void RevealedOffset_ClampOffsetReturnsNullWhenAlreadyVisible()
    {
        var leading = new RevealedOffset(500.0, new Rect(0, 0, 300, 100));
        var trailing = new RevealedOffset(400.0, new Rect(0, 100, 300, 100));

        Assert.Null(RevealedOffset.ClampOffset(leading, trailing, currentOffset: 450.0));
        Assert.Same(leading, RevealedOffset.ClampOffset(leading, trailing, currentOffset: 600.0));
        Assert.Same(trailing, RevealedOffset.ClampOffset(leading, trailing, currentOffset: 300.0));
    }

    [Fact]
    public void RevealedOffset_ClampOffsetHandlesInvertedEdges()
    {
        // A reversed axis makes the leading edge offset the smaller one.
        var leading = new RevealedOffset(400.0, new Rect(0, 100, 300, 100));
        var trailing = new RevealedOffset(500.0, new Rect(0, 0, 300, 100));

        Assert.Null(RevealedOffset.ClampOffset(leading, trailing, currentOffset: 450.0));
        Assert.Same(trailing, RevealedOffset.ClampOffset(leading, trailing, currentOffset: 600.0));
        Assert.Same(leading, RevealedOffset.ClampOffset(leading, trailing, currentOffset: 300.0));
    }

    [Fact]
    public void RenderAbstractViewport_MaybeOfWalksTheParentChainAndOfThrowsWithoutOne()
    {
        var target = new SizedBoxKeyProbe();
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                child: new Column(children: BuildTiles(20, target))));
        harness.Pump(new Size(300, 200));

        RenderObject renderTarget = target.RequireRenderObject();
        IRenderAbstractViewport? viewport = RenderAbstractViewport.MaybeOf(renderTarget);
        Assert.NotNull(viewport);
        Assert.IsType<RenderSingleChildViewport>(viewport);
        Assert.Same(viewport, RenderAbstractViewport.Of(renderTarget));

        Assert.Null(RenderAbstractViewport.MaybeOf(harness.RenderView));
        Assert.Throws<InvalidOperationException>(() => RenderAbstractViewport.Of(harness.RenderView));
        Assert.Null(RenderAbstractViewport.MaybeOf(null));
    }

    [Fact]
    public void RenderAbstractViewport_DefaultCacheExtentMatchesFlutter()
    {
        Assert.Equal(250.0, RenderAbstractViewport.DefaultCacheExtent);
    }

    // Flutter: "SingleChildScrollView getOffsetToReveal - down".
    // 200x300 viewport, twenty 100-tall children, target index 5, offset 300.
    [Fact]
    public void SingleChildViewport_GetOffsetToRevealDown()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                controller: controller,
                child: new Column(children: BuildTiles(20, target, index: 5))));
        harness.Pump(new Size(300, 200));

        var viewport = (RenderSingleChildViewport)RenderAbstractViewport.Of(target.RequireRenderObject());

        RevealedOffset leading = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0);
        Assert.Equal(500.0, leading.Offset, Tolerance);
        Assert.Equal(new Rect(0, 0, 300, 100), leading.Rect);

        RevealedOffset trailing = viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0);
        Assert.Equal(400.0, trailing.Offset, Tolerance);
        Assert.Equal(new Rect(0, 100, 300, 100), trailing.Rect);

        var rect = new Rect(40, 40, 10, 10);
        RevealedOffset leadingRect = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0, rect);
        Assert.Equal(540.0, leadingRect.Offset, Tolerance);
        Assert.Equal(new Rect(40, 0, 10, 10), leadingRect.Rect);

        RevealedOffset trailingRect = viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0, rect);
        Assert.Equal(350.0, trailingRect.Offset, Tolerance);
        Assert.Equal(new Rect(40, 190, 10, 10), trailingRect.Rect);

        controller.Dispose();
    }

    // Flutter: "SingleChildScrollView getOffsetToReveal - right".
    [Fact]
    public void SingleChildViewport_GetOffsetToRevealRight()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                scrollDirection: Axis.Horizontal,
                controller: controller,
                child: new Row(children: BuildTiles(20, target, index: 5, horizontal: true))));
        harness.Pump(new Size(200, 300));

        var viewport = (RenderSingleChildViewport)RenderAbstractViewport.Of(target.RequireRenderObject());

        RevealedOffset leading = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0);
        Assert.Equal(500.0, leading.Offset, Tolerance);
        Assert.Equal(new Rect(0, 0, 100, 300), leading.Rect);

        RevealedOffset trailing = viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0);
        Assert.Equal(400.0, trailing.Offset, Tolerance);
        Assert.Equal(new Rect(100, 0, 100, 300), trailing.Rect);

        var rect = new Rect(40, 40, 10, 10);
        Assert.Equal(
            540.0,
            viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0, rect).Offset,
            Tolerance);
        Assert.Equal(
            350.0,
            viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0, rect).Offset,
            Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void SingleChildViewport_GetOffsetToRevealCentersAtHalfAlignment()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                controller: controller,
                child: new Column(children: BuildTiles(20, target, index: 5))));
        harness.Pump(new Size(300, 200));

        var viewport = (RenderSingleChildViewport)RenderAbstractViewport.Of(target.RequireRenderObject());
        // Leading edge 500, viewport 200, target 100 => 500 - (200 - 100) * 0.5.
        Assert.Equal(450.0, viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.5).Offset, Tolerance);

        controller.Dispose();
    }

    // Flutter: "getOffsetToReveal - will not assert on axis mismatch".
    [Fact]
    public void GetOffsetToReveal_DoesNotRejectAMismatchedAxis()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                controller: controller,
                child: new Column(children: BuildTiles(20, target, index: 5))));
        harness.Pump(new Size(300, 200));

        IRenderAbstractViewport viewport = RenderAbstractViewport.Of(target.RequireRenderObject());
        RevealedOffset horizontal = viewport.GetOffsetToReveal(
            target.RequireRenderObject(),
            0.0,
            axis: Axis.Horizontal);
        RevealedOffset vertical = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0);
        Assert.Equal(vertical.Offset, horizontal.Offset, Tolerance);

        controller.Dispose();
    }

    // Flutter: "Viewport getOffsetToReveal - down", the sliver-viewport variant of the same numbers.
    [Fact]
    public void SliverViewport_GetOffsetToRevealDown()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new ListView(
                controller: controller,
                children: BuildTiles(20, target, index: 5)));
        harness.Pump(new Size(300, 200));

        var viewport = (RenderViewport)RenderAbstractViewport.Of(target.RequireRenderObject());

        RevealedOffset leading = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0);
        Assert.Equal(500.0, leading.Offset, Tolerance);
        Assert.Equal(new Rect(0, 0, 300, 100), leading.Rect);

        RevealedOffset trailing = viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0);
        Assert.Equal(400.0, trailing.Offset, Tolerance);
        Assert.Equal(new Rect(0, 100, 300, 100), trailing.Rect);

        var rect = new Rect(40, 40, 10, 10);
        RevealedOffset leadingRect = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0, rect);
        Assert.Equal(540.0, leadingRect.Offset, Tolerance);
        Assert.Equal(new Rect(40, 0, 10, 10), leadingRect.Rect);

        RevealedOffset trailingRect = viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0, rect);
        Assert.Equal(350.0, trailingRect.Offset, Tolerance);
        Assert.Equal(new Rect(40, 190, 10, 10), trailingRect.Rect);

        controller.Dispose();
    }

    // Flutter: "Viewport getOffsetToReveal Sliver - down": the padding around the target sliver
    // contributes through RenderSliverPadding.childScrollOffset.
    [Fact]
    public void SliverViewport_GetOffsetToRevealAccountsForSliverPadding()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var slivers = new List<Widget>();
        for (int index = 0; index < 20; index++)
        {
            Widget child = index == 5
                ? target.Wrap(new SizedBox(height: 100, width: 300))
                : new SizedBox(height: 100, width: 300);
            slivers.Add(new SliverPadding(
                padding: new Thickness(0, 22, 0, 23),
                sliver: new SliverToBoxAdapter(child)));
        }

        var harness = new WidgetRenderHarness(
            new CustomScrollView(controller: controller, slivers: slivers));
        harness.Pump(new Size(300, 200));

        var viewport = (RenderViewport)RenderAbstractViewport.Of(target.RequireRenderObject());
        // Five preceding slivers of 100 + 22 + 23, then this sliver's own leading padding.
        Assert.Equal(
            (5 * 145.0) + 22.0,
            viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0).Offset,
            Tolerance);
        Assert.Equal(
            (5 * 145.0) + 22.0 - 100.0,
            viewport.GetOffsetToReveal(target.RequireRenderObject(), 1.0).Offset,
            Tolerance);

        controller.Dispose();
    }

    /// <remarks>
    /// Flutter's pivot walk stops at real boxes because its `RenderSliver` is not a `RenderBox`.
    /// Plumix's is, so the walk has to exclude slivers explicitly: with a padded list the inner
    /// sliver would otherwise become the pivot and the tile's paint offset inside it would be added
    /// on top of its child scroll offset.
    /// </remarks>
    [Fact]
    public void SliverViewport_GetOffsetToRevealDoesNotCountANestedSliverTwice()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new CustomScrollView(
                controller: controller,
                slivers:
                [
                    new SliverPadding(
                        padding: new Thickness(0, 22, 0, 23),
                        sliver: new SliverList(
                            new SliverChildListDelegate(BuildTiles(20, target, index: 5)))),
                ]));
        harness.Pump(new Size(300, 200));

        RenderObject renderTarget = target.RequireRenderObject();
        var viewport = (RenderViewport)RenderAbstractViewport.Of(renderTarget);

        // The leading padding plus five preceding 100-pixel tiles, counted once.
        Assert.Equal(522.0, viewport.GetOffsetToReveal(renderTarget, 0.0).Offset, Tolerance);
        Assert.Equal(422.0, viewport.GetOffsetToReveal(renderTarget, 1.0).Offset, Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void SliverViewport_GetOffsetToRevealReportsTheCurrentOffsetForAForeignTarget()
    {
        var outside = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        var harness = new WidgetRenderHarness(
            new Column(
                children:
                [
                    outside.Wrap(new SizedBox(height: 20, width: 300)),
                    new SizedBox(
                        height: 200,
                        width: 300,
                        child: new ListView(controller: controller, children: BuildTiles(20))),
                ]));
        harness.Pump(new Size(300, 400));

        var viewport = FindDescendant<RenderViewport>(harness.RenderView);
        Assert.NotNull(viewport);
        RevealedOffset revealed = viewport.GetOffsetToReveal(outside.RequireRenderObject(), 0.0);
        Assert.Equal(300.0, revealed.Offset, Tolerance);

        controller.Dispose();
    }

    // Flutter: "ListView ensureVisible" — the target lands exactly at the leading edge.
    [Fact]
    public void EnsureVisible_ScrollsTheTargetToTheLeadingEdge()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 4)));
        harness.Pump(new Size(300, 200));

        _ = Scrollable.EnsureVisible(target.RequireContext());
        harness.Pump(new Size(300, 200));

        Assert.Equal(400.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    [Fact]
    public void EnsureVisible_ScrollsTheTargetToTheTrailingEdgeAtAlignmentOne()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 4)));
        harness.Pump(new Size(300, 200));

        _ = Scrollable.EnsureVisible(target.RequireContext(), alignment: 1.0);
        harness.Pump(new Size(300, 200));

        Assert.Equal(300.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    [Fact]
    public void EnsureVisible_ClampsTheTargetIntoTheScrollExtents()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 0)));
        harness.Pump(new Size(300, 200));

        // Aligning the first tile's trailing edge would need a negative offset.
        _ = Scrollable.EnsureVisible(target.RequireContext(), alignment: 1.0);
        harness.Pump(new Size(300, 200));

        Assert.Equal(0.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    // Flutter: "ensureVisible does not change position of items already fully on-screen".
    [Fact]
    public void EnsureVisible_LeavesAnAlreadyVisibleTargetAlone()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 500.0);
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 5)));
        harness.Pump(new Size(300, 200));

        _ = Scrollable.EnsureVisible(target.RequireContext());
        harness.Pump(new Size(300, 200));

        Assert.Equal(500.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    // Flutter: the keepVisibleAtStart / keepVisibleAtEnd policies never scroll the "wrong" way.
    [Fact]
    public void EnsureVisible_KeepVisibleAtStartNeverScrollsForwards()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 800.0);
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 5)));
        harness.Pump(new Size(300, 200));

        // The leading edge of tile 5 is at 500, which is before the current offset, so it moves.
        _ = Scrollable.EnsureVisible(
            target.RequireContext(),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        harness.Pump(new Size(300, 200));
        Assert.Equal(500.0, controller.Offset, Tolerance);

        // Now it is already at the start, so a second request cannot push it forwards.
        _ = Scrollable.EnsureVisible(
            target.RequireContext(),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        harness.Pump(new Size(300, 200));
        Assert.Equal(500.0, controller.Offset, Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void EnsureVisible_KeepVisibleAtEndNeverScrollsBackwards()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 4)));
        harness.Pump(new Size(300, 200));

        _ = Scrollable.EnsureVisible(
            target.RequireContext(),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        harness.Pump(new Size(300, 200));
        Assert.Equal(300.0, controller.Offset, Tolerance);

        _ = Scrollable.EnsureVisible(
            target.RequireContext(),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        harness.Pump(new Size(300, 200));
        Assert.Equal(300.0, controller.Offset, Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void EnsureVisible_IsANoOpWithoutAnEnclosingScrollable()
    {
        var target = new SizedBoxKeyProbe();
        var harness = new WidgetRenderHarness(
            new Column(children: [target.Wrap(new SizedBox(height: 40, width: 100))]));
        harness.Pump(new Size(300, 200));

        Task result = Scrollable.EnsureVisible(target.RequireContext());
        Assert.True(result.IsCompletedSuccessfully);
    }

    [Fact]
    public void EnsureVisible_RejectsANonFiniteAlignmentAndANegativeDuration()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 4)));
        harness.Pump(new Size(300, 200));

        BuildContext context = target.RequireContext();
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                Task ignored = Scrollable.EnsureVisible(context, alignment: double.NaN);
                Assert.NotNull(ignored);
            });
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                Task ignored = Scrollable.EnsureVisible(context, duration: TimeSpan.FromMilliseconds(-1));
                Assert.NotNull(ignored);
            });

        controller.Dispose();
    }

    // Flutter: "Nested Viewports showOnScreen" — the inner viewport reveals the target and the outer
    // one then reveals the inner viewport's new rectangle.
    [Fact]
    public void ShowOnScreen_RevealsThroughNestedScrollables()
    {
        var target = new SizedBoxKeyProbe();
        var outerController = new ScrollController();
        var innerController = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(
                controller: outerController,
                children:
                [
                    new SizedBox(height: 200, width: 300),
                    new SizedBox(
                        height: 200,
                        width: 300,
                        child: new ListView(
                            controller: innerController,
                            children: BuildTiles(10, target, index: 4))),
                    new SizedBox(height: 200, width: 300),
                ]));
        harness.Pump(new Size(300, 200));

        target.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));

        // Tile 4 sits at 400 inside the inner list, whose viewport is 200 tall.
        Assert.Equal(300.0, innerController.Offset, Tolerance);
        // The inner list itself starts at 200 in the outer list.
        Assert.Equal(200.0, outerController.Offset, Tolerance);

        innerController.Dispose();
        outerController.Dispose();
    }

    [Fact]
    public void ShowOnScreen_DoesNotMoveAnAlreadyVisibleTarget()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 500.0);
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20, target, index: 5)));
        harness.Pump(new Size(300, 200));

        target.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));

        Assert.Equal(500.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    // Flutter: "brings item above leading edge to leading edge" / "below trailing edge to trailing
    // edge" — showOnScreen scrolls the shorter way, unlike ensureVisible's explicit alignment.
    [Fact]
    public void ShowOnScreen_MovesToTheNearestEdge()
    {
        var above = new SizedBoxKeyProbe();
        var below = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 500.0);
        var children = BuildTiles(20);
        children[4] = above.Wrap(new SizedBox(height: 100, width: 300));
        children[7] = below.Wrap(new SizedBox(height: 100, width: 300));
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: children));
        harness.Pump(new Size(300, 200));

        above.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));
        Assert.Equal(400.0, controller.Offset, Tolerance);

        controller.JumpTo(500.0);
        harness.Pump(new Size(300, 200));
        below.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));
        // Tile 7 spans 700..800; aligning its trailing edge with the 200-tall viewport gives 600.
        Assert.Equal(600.0, controller.Offset, Tolerance);

        controller.Dispose();
    }

    // Flutter: "allowImplicitScrolling=false for inner viewport" — the inner viewport refuses to
    // scroll and the request passes through to the outer one unchanged.
    [Fact]
    public void ShowOnScreen_SkipsAViewportThatForbidsImplicitScrolling()
    {
        var target = new SizedBoxKeyProbe();
        var outerController = new ScrollController();
        var innerController = new ScrollController();
        var harness = new WidgetRenderHarness(
            new ListView(
                controller: outerController,
                children:
                [
                    new SizedBox(height: 50, width: 300),
                    new SizedBox(
                        height: 200,
                        width: 300,
                        child: new ListView(
                            controller: innerController,
                            physics: new PageScrollPhysics(),
                            children: BuildTiles(10, target, index: 1))),
                ]));
        harness.Pump(new Size(300, 200));

        target.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));

        Assert.Equal(0.0, innerController.Offset, Tolerance);
        Assert.Equal(50.0, outerController.Offset, Tolerance);

        innerController.Dispose();
        outerController.Dispose();
    }

    // Flutter: "showOnScreen works in scrollable" — the semantics action drives the same protocol.
    [Fact]
    public void SemanticsShowOnScreenAction_ScrollsTheNodeIntoView()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 500.0);
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                controller: controller,
                child: new Column(children: BuildTiles(20, target, index: 3, semanticsLabel: "Target"))));
        harness.Pump(new Size(300, 200));

        SemanticsNode? node = harness.FindSemanticsNode("Target");
        Assert.NotNull(node);
        int? nodeId = node.Id;
        // No explicit handler is registered, so the node falls back to its render object's request.
        Assert.True(harness.PerformSemanticsAction(nodeId.Value, SemanticsActions.ShowOnScreen));
        harness.Pump(new Size(300, 200));

        // Tile 3 spans 300..400; at offset 500 it is above the viewport, so it moves to 300.
        Assert.Equal(300.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    [Fact]
    public void SemanticsConfiguration_OnShowOnScreenReplacesTheDefaultRequest()
    {
        var configuration = new SemanticsConfiguration();
        Assert.Null(configuration.OnShowOnScreen);
        Assert.False(configuration.Actions.HasFlag(SemanticsActions.ShowOnScreen));

        int invocations = 0;
        configuration.OnShowOnScreen = () => invocations += 1;
        Assert.True(configuration.Actions.HasFlag(SemanticsActions.ShowOnScreen));
        configuration.OnShowOnScreen!();
        Assert.Equal(1, invocations);

        configuration.OnShowOnScreen = null;
        Assert.False(configuration.Actions.HasFlag(SemanticsActions.ShowOnScreen));
        Assert.Null(configuration.OnShowOnScreen);
    }

    // Flutter: "showOnScreen should not scroll if the rect is already visible, even if it does not
    // scroll linearly" — a pinned header reports an infinite leading-edge offset.
    [Fact]
    public void SliverViewport_PinnedHeaderReportsAnInfiniteLeadingRevealOffset()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController(initialScrollOffset: 400.0);
        var harness = new WidgetRenderHarness(
            new CustomScrollView(
                controller: controller,
                slivers:
                [
                    new SliverPersistentHeader(
                        pinned: true,
                        @delegate: new FixedHeaderDelegate(100.0, target)),
                    new SliverToBoxAdapter(new SizedBox(height: 2000, width: 300)),
                ]));
        harness.Pump(new Size(300, 200));

        var viewport = (RenderViewport)RenderAbstractViewport.Of(target.RequireRenderObject());
        RevealedOffset leading = viewport.GetOffsetToReveal(target.RequireRenderObject(), 0.0);
        Assert.True(double.IsPositiveInfinity(leading.Offset));

        // The pinned header is always visible, so the request must leave the offset alone.
        target.RequireRenderObject().ShowOnScreen();
        harness.Pump(new Size(300, 200));
        Assert.Equal(400.0, controller.Offset, Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void SliverViewport_PinnedHeaderExtentIsRemovedFromTheRevealOfLaterSlivers()
    {
        var target = new SizedBoxKeyProbe();
        var controller = new ScrollController();
        var children = BuildTiles(20, target, index: 3);
        var harness = new WidgetRenderHarness(
            new CustomScrollView(
                controller: controller,
                slivers:
                [
                    new SliverPersistentHeader(
                        pinned: true,
                        @delegate: new FixedHeaderDelegate(50.0)),
                    new SliverList(new SliverChildListDelegate(children)),
                ]));
        harness.Pump(new Size(300, 200));

        _ = Scrollable.EnsureVisible(target.RequireContext());
        harness.Pump(new Size(300, 200));

        // The header contributes 50 of scroll extent, and pins 50 that the reveal must discount.
        Assert.Equal(300.0, controller.Offset, Tolerance);
        controller.Dispose();
    }

    [Fact]
    public void RenderSliverPadding_ReportsItsLeadingPaddingAsTheChildScrollOffset()
    {
        var harness = new WidgetRenderHarness(
            new CustomScrollView(
                slivers:
                [
                    new SliverPadding(
                        padding: new Thickness(11, 22, 13, 23),
                        sliver: new SliverToBoxAdapter(new SizedBox(height: 100, width: 300))),
                ]));
        harness.Pump(new Size(300, 200));

        var padding = FindDescendant<RenderSliverPadding>(harness.RenderView);
        Assert.NotNull(padding);
        RenderSliver child = Assert.IsType<RenderSliverToBoxAdapter>(padding.Child);
        Assert.Equal(22.0, padding.ChildScrollOffset(child));
        Assert.Equal(11.0, padding.ChildCrossAxisPosition(child));
    }

    [Fact]
    public void RenderSliverMultiBoxAdaptor_ReportsChildScrollOffsetsAndMainAxisPositions()
    {
        var controller = new ScrollController(initialScrollOffset: 250.0);
        var harness = new WidgetRenderHarness(
            new ListView(controller: controller, children: BuildTiles(20)));
        harness.Pump(new Size(300, 200));

        var list = FindDescendant<RenderSliverList>(harness.RenderView);
        Assert.NotNull(list);
        RenderBox? first = list.FirstChild;
        Assert.NotNull(first);
        double scrollOffset = list.ChildScrollOffset(first) ?? double.NaN;
        Assert.Equal(scrollOffset - 250.0, list.ChildMainAxisPosition(first), Tolerance);

        controller.Dispose();
    }

    [Fact]
    public void RenderSliverBase_ChildMainAxisPositionThrowsWhenUnimplemented()
    {
        var sliver = new BareSliver();
        Assert.Throws<InvalidOperationException>(() => sliver.ChildMainAxisPosition(sliver));
        Assert.Equal(0.0, sliver.ChildCrossAxisPosition(sliver));
    }

    [Fact]
    public void PersistentHeaderShowOnScreenConfiguration_ValidatesAndComparesByValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PersistentHeaderShowOnScreenConfiguration(200.0, 100.0));

        var defaults = new PersistentHeaderShowOnScreenConfiguration();
        Assert.True(double.IsNegativeInfinity(defaults.MinShowOnScreenExtent));
        Assert.True(double.IsPositiveInfinity(defaults.MaxShowOnScreenExtent));
        Assert.Equal(
            new PersistentHeaderShowOnScreenConfiguration(10.0, 20.0),
            new PersistentHeaderShowOnScreenConfiguration(10.0, 20.0));
        Assert.NotEqual(
            new PersistentHeaderShowOnScreenConfiguration(10.0, 20.0),
            new PersistentHeaderShowOnScreenConfiguration(10.0, 30.0));
    }

    [Fact]
    public void ScrollPosition_MoveToJumpsWithoutADurationAndAnimatesWithOne()
    {
        using var position = new ScrollPosition(new ClampingScrollPhysics(), new TestScrollContext(), initialPixels: 0);
        position.ApplyViewportDimension(200);
        position.ApplyContentDimensions(0, 1000);

        position.MoveTo(120.0);
        Assert.Equal(120.0, position.Pixels, Tolerance);

        Task animated = position.MoveTo(300.0, TimeSpan.FromMilliseconds(200));
        Assert.False(animated.IsCompleted);
        Assert.Equal(120.0, position.Pixels, Tolerance);
    }

    private static List<Widget> BuildTiles(
        int count,
        SizedBoxKeyProbe? probe = null,
        int index = 0,
        bool horizontal = false,
        string? semanticsLabel = null)
    {
        var children = new List<Widget>(count);
        for (int i = 0; i < count; i++)
        {
            Widget tile = horizontal
                ? new SizedBox(width: 100, height: 300)
                : new SizedBox(height: 100, width: 300);
            if (probe != null && i == index)
            {
                tile = semanticsLabel is null
                    ? probe.Wrap(tile)
                    : probe.Wrap(new Semantics(label: semanticsLabel, container: true, child: tile));
            }

            children.Add(tile);
        }

        return children;
    }

    private static T? FindDescendant<T>(RenderObject root) where T : RenderObject
    {
        if (root is T match)
        {
            return match;
        }

        T? found = null;
        root.VisitChildren(child =>
        {
            found ??= FindDescendant<T>(child);
        });
        return found;
    }

    /// <summary>A header of a fixed extent, optionally carrying the probe on its content.</summary>
    private sealed class FixedHeaderDelegate : SliverPersistentHeaderDelegate
    {
        private readonly double _extent;
        private readonly SizedBoxKeyProbe? _probe;

        public FixedHeaderDelegate(double extent, SizedBoxKeyProbe? probe = null)
        {
            _extent = extent;
            _probe = probe;
        }

        public override double MinExtent => _extent;

        public override double MaxExtent => _extent;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            Widget child = new SizedBox(height: _extent, width: 300);
            return _probe is null ? child : _probe.Wrap(child);
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    private sealed class BareSliver : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            Geometry = default;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>Captures the build context of one widget in the tree, to address it from a test.</summary>
    private sealed class SizedBoxKeyProbe
    {
        private BuildContext? _context;

        public Widget Wrap(Widget child)
        {
            return new Builder(context =>
            {
                _context = context;
                return child;
            });
        }

        public BuildContext RequireContext()
        {
            return _context ?? throw new InvalidOperationException("The probe was never built.");
        }

        public RenderObject RequireRenderObject()
        {
            return RequireContext().FindRenderObject()
                   ?? throw new InvalidOperationException("The probe has no render object.");
        }
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
            _pipeline.FlushSemantics();
        }

        public SemanticsNode? FindSemanticsNode(string label)
        {
            return FindSemanticsNode(_pipeline.SemanticsOwner.RootNode, label);
        }

        public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
        {
            return _pipeline.SemanticsOwner.PerformAction(nodeId, action);
        }

        private static SemanticsNode? FindSemanticsNode(SemanticsNode? node, string label)
        {
            if (node is null)
            {
                return null;
            }

            if (node.Label == label)
            {
                return node;
            }

            foreach (SemanticsNode child in node.Children)
            {
                if (FindSemanticsNode(child, label) is { } match)
                {
                    return match;
                }
            }

            return null;
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
}
