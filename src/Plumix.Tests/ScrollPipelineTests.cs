using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/sliver.dart; flutter/packages/flutter/lib/src/rendering/viewport.dart; flutter/packages/flutter/lib/src/widgets/scrollable.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollPipelineTests
{
    [Fact]
    public void ScrollView_OnDragKeyboardDismissBehavior_UnfocusesDescendant()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        var focusNode = new FocusNode();
        var harness = new WidgetRenderHarness(
            new SingleChildScrollView(
                keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag,
                child: new Focus(
                    focusNode: focusNode,
                    autofocus: true,
                    child: new SizedBox(width: 100, height: 800))));
        harness.Pump(new Size(200, 240));
        Assert.True(focusNode.HasFocus);

        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                700,
                PointerDeviceKind.Touch,
                new Point(80, 100),
                PointerButtons.Primary,
                now));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                700,
                PointerDeviceKind.Touch,
                new Point(80, 140),
                PointerButtons.Primary,
                true,
                now.AddMilliseconds(16)));
        Scheduler.FlushMicrotasks();

        Assert.False(focusNode.HasFocus);
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                700,
                PointerDeviceKind.Touch,
                new Point(80, 140),
                PointerButtons.None,
                now.AddMilliseconds(32)));
        focusNode.Dispose();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void ScrollPosition_JumpForcesTheRequestedOffsetBeforeBallisticSettling()
    {
        using var position = new ScrollPosition(
            new ClampingScrollPhysics(),
            new TestScrollContext(),
            initialPixels: 10);
        int notifications = 0;
        position.AddListener(() => notifications += 1);

        position.ApplyViewportDimension(120);
        position.ApplyContentDimensions(0, 60);
        position.JumpTo(1000);

        Assert.Equal(1000, position.Pixels);
        Assert.IsType<BallisticScrollActivity>(position.Activity);
        Assert.True(notifications > 0);
    }

    [Fact]
    public void ScrollPosition_EndDrag_EntersBallisticActivity()
    {
        Scheduler.ResetForTests();
        try
        {
            var position = new ScrollPosition(new ClampingScrollPhysics(), new TestScrollContext(), initialPixels: 40);
            position.ApplyViewportDimension(120);
            position.ApplyContentDimensions(0, 800);

            position.BeginDrag();
            position.EndDrag(primaryPointerVelocity: -1200);
            Assert.IsType<BallisticScrollActivity>(position.Activity);
            position.Dispose();
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollController_JumpTo_UpdatesAttachedPositions()
    {
        var controller = new ScrollController();
        var first = controller.CreateScrollPosition(controller.Physics, new TestScrollContext(), null);
        var second = controller.CreateScrollPosition(controller.Physics, new TestScrollContext(), null);

        first.ApplyViewportDimension(100);
        first.ApplyContentDimensions(0, 200);
        second.ApplyViewportDimension(100);
        second.ApplyContentDimensions(0, 50);

        controller.Attach(first);
        controller.Attach(second);
        controller.JumpTo(120);

        Assert.Equal(120, first.Pixels);
        Assert.Equal(120, second.Pixels);
        Assert.IsType<BallisticScrollActivity>(second.Activity);
        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public void RenderViewport_OffsetsChild_AndReportsMetrics()
    {

        var child = new FixedSizeBox(new Size(80, 600));
        var viewportOffset = new TestViewportOffset(50);
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(new RenderSliverToBoxAdapter(child));

        var root = new RenderView
        {
            Child = viewport
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        var childParentData = (BoxParentData)child.parentData!;
        Assert.Equal(new Point(0, -50), childParentData.offset);
        Assert.Equal(200, viewportOffset.ViewportDimension);
        Assert.Equal(0, viewportOffset.MinScrollExtent);
        Assert.Equal(400, viewportOffset.MaxScrollExtent);
    }

    [Fact]
    public void RenderViewport_LaysOutMultipleSlivers_AndAggregatesScrollExtent()
    {

        var first = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 120)));
        var second = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 160)));
        var viewportOffset = new TestViewportOffset(80);
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(first);
        viewport.Insert(second, after: first);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 150));

        Assert.Equal(130, viewportOffset.MaxScrollExtent);

        var firstBoxOffset = ((BoxParentData)((RenderBox)first.Child!).parentData!).offset;
        var secondBoxOffset = ((BoxParentData)((RenderBox)second.Child!).parentData!).offset;
        Assert.Equal(new Point(0, -80), firstBoxOffset);
        Assert.Equal(new Point(0, 0), secondBoxOffset);
    }

    [Fact]
    public void RenderViewport_PaintsFirstSliverAboveFollowingSlivers()
    {
        var paintOrder = new List<string>();
        var first = new PaintTrackingSliver("first", paintOrder);
        var second = new PaintTrackingSliver("second", paintOrder);
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(first);
        viewport.Insert(second, after: first);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(["second", "first"], paintOrder);
    }

    [Fact]
    public void RenderViewport_AppliesScrollOffsetCorrection_FromSliver()
    {
        var correcting = new CorrectingSliver(
            correction: -100,
            scrollExtent: 500);
        var viewportOffset = new TestViewportOffset(100);
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(correcting);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(0, viewportOffset.Pixels);
        Assert.Equal(300, viewportOffset.MaxScrollExtent);
    }

    [Fact]
    public void RenderViewport_OverscrolledOffset_ShiftsChildrenInsteadOfBeingClamped()
    {
        var innerSliver = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 300)));
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(innerSliver);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(200, viewportOffset.MaxScrollExtent);
        var sliverParentData = (SliverPhysicalParentData)innerSliver.parentData!;
        Assert.Equal(new Point(0, 0), sliverParentData.offset);

        // Overscrolled past the leading edge: the offset survives layout and the content is pushed
        // down by exactly the overscroll, which is what makes the iOS rubber band visible.
        viewportOffset.JumpTo(-30);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(-30, viewportOffset.Pixels);
        Assert.Equal(new Point(0, 30), sliverParentData.offset);

        // The leading sliver is told about the overscroll through a negative overlap, which is what
        // overscroll-aware slivers stretch into.
        Assert.Equal(-30, innerSliver.ConstraintsForSliver.Overlap);
        Assert.Equal(0, innerSliver.ConstraintsForSliver.ScrollOffset);

        // Overscrolled past the trailing edge: the offset is kept rather than clamped to the max.
        viewportOffset.JumpTo(240);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(240, viewportOffset.Pixels);
        Assert.Equal(200, viewportOffset.MaxScrollExtent);

        // Back in range, the offset is used as-is again.
        viewportOffset.JumpTo(50);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(50, viewportOffset.Pixels);
        Assert.Equal(new Point(0, 0), sliverParentData.offset);
    }

    [Fact]
    public void RenderSliverPadding_ContributesPaddingToScrollExtent()
    {
        var innerSliver = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 120)));
        var sliverPadding = new RenderSliverPadding(new Thickness(0, 10, 0, 20), innerSliver);
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(sliverPadding);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(50, viewportOffset.MaxScrollExtent);
        var sliverParentData = (SliverPhysicalParentData)innerSliver.parentData!;
        Assert.Equal(new Point(0, 10), sliverParentData.offset);

        viewportOffset.JumpTo(15);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(new Point(0, 0), sliverParentData.offset);
        var innerBoxOffset = ((BoxParentData)((RenderBox)innerSliver.Child!).parentData!).offset;
        Assert.Equal(new Point(0, -5), innerBoxOffset);
    }

    [Fact]
    public void RenderViewport_AxisDirectionUp_PaintsContentFromTheTrailingEdge()
    {
        var child = new FixedSizeBox(new Size(80, 600));
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            axisDirection: AxisDirection.Up);
        viewport.Insert(new RenderSliverToBoxAdapter(child));
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        var childParentData = (BoxParentData)child.parentData!;
        Assert.Equal(new Point(0, -400), childParentData.offset);

        viewportOffset.JumpTo(400);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(new Point(0, 0), childParentData.offset);
    }

    [Fact]
    public void RenderViewport_PropagatesAxisAndGrowthDirectionToSlivers()
    {
        var sliver = new ConstraintCapturingSliver(scrollExtent: 300);
        var center = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 40)));
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            axisDirection: AxisDirection.Up);
        viewport.Insert(sliver);
        viewport.Insert(center, after: sliver);
        // Every sliver laid out before the center child grows in the reverse direction.
        viewport.Center = center;

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 120));

        Assert.Equal(AxisDirection.Up, sliver.LastConstraints.AxisDirection);
        Assert.Equal(GrowthDirection.Reverse, sliver.LastConstraints.GrowthDirection);
        Assert.Equal(Axis.Vertical, sliver.LastConstraints.Axis);
    }

    [Fact]
    public void RenderSliverList_CreatesOnlyNeededChildren_AndTrimsTrailingOnReverseScroll()
    {
        var manager = new TestSliverChildManager(childCount: 200, childExtent: 50);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0));
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.InRange(manager.MaxCreatedIndex, 0, 6);
        Assert.Equal(0, manager.RemoveCount);

        viewportOffset.JumpTo(450);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.InRange(manager.MaxCreatedIndex, 9, 15);

        viewportOffset.JumpTo(0);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.True(manager.RemoveCount > 0);
    }

    [Fact]
    public void RenderSliverList_KeepAliveChild_IsReused_WhenReturningToViewport()
    {
        var manager = new TestSliverChildManager(
            childCount: 200,
            childExtent: 50,
            keepAliveIndices: [0]);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));

        viewportOffset.JumpTo(600);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverList));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewportOffset.JumpTo(0);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));
    }

    [Fact]
    public void RenderSliverList_VariableExtentChildren_ContinuouslyCoverViewportDuringScroll()
    {
        int childCount = 300;
        var manager = new VariableExtentSliverChildManager(
            childCount,
            index => index % 2 == 0 ? 44 : 4);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        const double viewportExtent = 220;
        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Pixels(250));
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, viewportExtent));

        double contentExtent = manager.TotalContentExtent;
        double maxOffsetToCheck = Math.Max(0, contentExtent - viewportExtent - 1);

        for (double offset = 0; offset <= maxOffsetToCheck; offset += 37)
        {
            viewportOffset.JumpTo(offset);
            pipeline.FlushLayout(new Size(100, viewportExtent));

            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.25), $"No child covers 25% of viewport at offset {offset}.");
            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.5), $"No child covers 50% of viewport at offset {offset}.");
            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.75), $"No child covers 75% of viewport at offset {offset}.");
        }
    }

    [Fact]
    public void CustomScrollView_CenterKey_GrowsPrecedingSliversIntoNegativeScrollOffsets()
    {
        var controller = new ScrollController();
        var centerKey = new ValueKey<string>("center");
        var widget = new CustomScrollView(
            controller: controller,
            center: centerKey,
            slivers:
            [
                new SliverToBoxAdapter(child: new SizedBox(height: 300)),
                new SliverToBoxAdapter(key: centerKey, child: new SizedBox(height: 400)),
            ]);

        var harness = new WidgetRenderHarness(widget);
        var viewportSize = new Size(300, 200);
        harness.Pump(viewportSize);

        ScrollPosition position = Assert.IsAssignableFrom<ScrollPosition>(controller.PrimaryPosition);
        // The sliver before the center child occupies negative scroll offsets.
        Assert.Equal(-300, position.MinScrollExtent);
        Assert.Equal(200, position.MaxScrollExtent);
        Assert.Equal(0, position.Pixels);

        controller.JumpTo(-150);
        harness.Pump(viewportSize);
        Assert.Equal(-150, position.Pixels);

        var viewport = Assert.IsType<RenderViewport>(FindRenderObject<RenderViewport>(harness.RenderView)!);
        RenderSliver reverseChild = viewport.FirstChild!;
        Assert.NotNull(viewport.Center);
        Assert.NotSame(reverseChild, viewport.Center);
        Assert.Equal(GrowthDirection.Reverse, reverseChild.ConstraintsForSliver.GrowthDirection);
        Assert.Equal(150.0, reverseChild.Geometry.PaintExtent);
    }

    [Fact]
    public void Scrollable_ListViewSeparated_MaintainsViewportCoverageDuringControllerJumps()
    {
        var controller = new ScrollController();
        var widget = ListView.Separated(
            itemCount: 120,
            controller: controller,
            // Pinned so the jump coverage is checked against boundary-clamping physics rather than
            // the host platform's default (bouncing physics legitimately overscroll a jump that
            // lands past a lazily-estimated max extent).
            physics: new ClampingScrollPhysics(parent: new RangeMaintainingScrollPhysics()),
            padding: new Thickness(12),
            addAutomaticKeepAlives: false,
            itemBuilder: (_, index) => new Container(
                height: 44,
                color: index % 2 == 0 ? Avalonia.Media.Colors.White : Avalonia.Media.Colors.WhiteSmoke),
            separatorBuilder: (_, _) => new SizedBox(height: 4));

        var harness = new WidgetRenderHarness(widget);
        const double viewportWidth = 360;
        const double viewportHeight = 320;
        var viewportSize = new Size(viewportWidth, viewportHeight);

        harness.Pump(viewportSize);
        var position = controller.PrimaryPosition;
        Assert.NotNull(position);
        double contentExtent = (120 * 44) + (119 * 4) + 24;
        double maxOffsetToCheck = Math.Max(0, contentExtent - viewportHeight - 1);
        var viewport = Assert.IsType<RenderViewport>(FindRenderObject<RenderViewport>(harness.RenderView)!);

        for (double offset = 0; offset <= maxOffsetToCheck; offset += 53)
        {
            controller.JumpTo(offset);
            harness.Pump(viewportSize);

            var sliverList = Assert.IsType<RenderSliverList>(FindRenderObject<RenderSliverList>(viewport)!);
            try
            {
                Assert.True(CoversViewportPosition(sliverList, viewportHeight * 0.25), $"No child covers 25% of viewport at offset {offset}.");
                Assert.True(CoversViewportPosition(sliverList, viewportHeight * 0.5), $"No child covers 50% of viewport at offset {offset}.");
                Assert.True(CoversViewportPosition(sliverList, viewportHeight * 0.75), $"No child covers 75% of viewport at offset {offset}.");
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Offset {offset} failed. Active children snapshot: {DescribeActiveChildren(sliverList)}. Details: {ex.Message}");
            }
        }
    }

    [Fact]
    public void RenderViewport_CacheExtent_PreloadsChildrenOutsidePaintRegion()
    {
        var manager = new TestSliverChildManager(childCount: 200, childExtent: 50);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Pixels(100));
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Contains(5, ActiveIndices(sliverList));
        Assert.InRange(manager.MaxCreatedIndex, 5, 10);

        viewport.CacheExtent = 0;
        pipeline.FlushLayout(new Size(100, 200));

        Assert.DoesNotContain(5, ActiveIndices(sliverList));
        Assert.True(manager.RemoveCount > 0);
    }

    [Fact]
    public void RenderViewport_ViewportCacheExtentStyle_ScalesByViewportSize()
    {
        var manager = new TestSliverChildManager(childCount: 200, childExtent: 50);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Viewport(1));
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Contains(7, ActiveIndices(sliverList));
        Assert.InRange(manager.MaxCreatedIndex, 7, 12);
    }

    [Fact]
    public void RenderSliverFixedExtentList_ComputesIndicesFromItemExtent()
    {
        var manager = new TestSliverChildManager(childCount: 100, childExtent: 10);
        var sliverList = new RenderSliverFixedExtentList(itemExtent: 40, childManager: manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0));
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(3800, viewportOffset.MaxScrollExtent);
        Assert.Contains(4, ActiveIndices(sliverList));
        Assert.DoesNotContain(5, ActiveIndices(sliverList));

        viewportOffset.JumpTo(480);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.DoesNotContain(0, ActiveIndices(sliverList));
        Assert.Contains(12, ActiveIndices(sliverList));
        Assert.InRange(manager.MaxCreatedIndex, 16, 20);
    }

    [Fact]
    public void RenderSliverFixedExtentList_KeepAliveChild_IsReused_WhenReturningToViewport()
    {
        var manager = new TestSliverChildManager(
            childCount: 200,
            childExtent: 50,
            keepAliveIndices: [0]);
        var sliverList = new RenderSliverFixedExtentList(itemExtent: 50, childManager: manager);
        manager.AttachOwner(sliverList);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));

        viewportOffset.JumpTo(600);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverList));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewportOffset.JumpTo(0);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));
    }

    [Fact]
    public void SliverVariedExtentList_ExposesSourceShapedConstructorsAndUpdatesBuilder()
    {
        ItemExtentBuilder initialBuilder = (index, _) => 24 + index;
        var childDelegate = new SliverChildListDelegate([new SizedBox(), new SizedBox()]);
        var widget = new SliverVariedExtentList(childDelegate, initialBuilder);

        Assert.Same(childDelegate, widget.Delegate);
        Assert.Same(initialBuilder, widget.ItemExtentBuilder);
        Assert.Throws<ArgumentNullException>(() => new SliverVariedExtentList(childDelegate, null!));
        // Dart's SliverChildBuilderDelegate has no assert on childCount; a negative count simply
        // makes every index fail the `index >= childCount` bounds check, so the sliver is empty.
        Assert.Null(SliverVariedExtentList.Builder(
            itemCount: -1,
            itemBuilder: (_, _) => new SizedBox(),
            itemExtentBuilder: initialBuilder).Delegate.Build(default, 0));

        var renderObject = Assert.IsType<RenderSliverVariedExtentList>(widget.CreateRenderObject(default));
        ItemExtentBuilder updatedBuilder = (index, _) => 48 + index;
        var updatedWidget = new SliverVariedExtentList(childDelegate, updatedBuilder);
        updatedWidget.UpdateRenderObject(default, renderObject);

        Assert.Same(updatedBuilder, renderObject.ItemExtentBuilder);
        Assert.Equal(2, SliverVariedExtentList.FromChildren(
            [new SizedBox(), new SizedBox()],
            initialBuilder).Delegate.EstimatedChildCount);
        Assert.Equal(3, SliverVariedExtentList.Builder(
            itemCount: 3,
            itemBuilder: (_, _) => new SizedBox(),
            itemExtentBuilder: initialBuilder).Delegate.EstimatedChildCount);
    }

    [Fact]
    public void RenderSliverVariedExtentList_QueriesTheBuilderWithTheCurrentLayoutDimensions()
    {
        double[] extents = [30, 50, 20, 40];
        var dimensions = new List<SliverLayoutDimensions>();
        ItemExtentBuilder builder = (index, currentDimensions) =>
        {
            dimensions.Add(currentDimensions);
            return index < extents.Length ? extents[index] : null;
        };
        var manager = new TestSliverChildManager(childCount: extents.Length, childExtent: 5);
        var sliver = new RenderSliverVariedExtentList(builder, manager);
        manager.AttachOwner(sliver);

        sliver.LayoutWithSliverConstraints(new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 100,
            CrossAxisExtent: 120,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 100,
            PrecedingScrollExtent: 17));

        // Dart's `estimateMaxScrollOffset` extrapolates from the average extent of the laid-out
        // children (100 over three children, one child left) rather than summing every extent.
        Assert.Equal(400.0 / 3, sliver.Geometry.ScrollExtent, 9);
        Assert.Equal([0, 1, 2], ActiveIndices(sliver));
        Assert.Equal([30, 50, 20], ActiveChildren(sliver).Select(child => child.Size.Height));
        Assert.Equal([0, 30, 80], ActiveChildren(sliver).Select(
            child => ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset));
        // Dart's `RenderSliverFixedExtentBoxAdaptor` memoizes nothing: every offset query re-walks
        // the builder from index zero, so only the dimensions it is handed are contractual.
        Assert.NotEmpty(dimensions);
        Assert.All(dimensions, current =>
        {
            Assert.Equal(0, current.ScrollOffset);
            Assert.Equal(17, current.PrecedingScrollExtent);
            Assert.Equal(100, current.ViewportMainAxisExtent);
            Assert.Equal(120, current.CrossAxisExtent);
        });

        sliver.LayoutWithSliverConstraints(new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 80,
            RemainingPaintExtent: 60,
            CrossAxisExtent: 120,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 60));

        // `_getChildIndexForScrollOffset` walks until the running position reaches the offset and
        // then steps back one, so the child that ends exactly at the scroll offset stays reified.
        Assert.Equal([1, 2, 3], ActiveIndices(sliver));
        Assert.Equal(new Point(0, -50),
            ((SliverMultiBoxAdaptorParentData)sliver.FirstChild!.parentData!).offset);
        Assert.True(manager.RemoveCount > 0);
    }

    [DebugOnlyFact]
    public void RenderSliverVariedExtentList_RejectsInvalidBuilderExtents()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var manager = new TestSliverChildManager(childCount: 1, childExtent: 10);
        var sliver = new RenderSliverVariedExtentList((_, _) => double.NaN, manager);
        manager.AttachOwner(sliver);

        // Flutter has no extent validation of its own: a NaN item extent reaches the child's box
        // constraints, and `BoxConstraints`'s own assert reports it.
        Assert.Throws<FlutterError>(() => sliver.LayoutWithSliverConstraints(new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 100,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 100)));
    }

    [Fact]
    public void RenderSliverPrototypeExtentList_MeasuresOffstagePrototypeAndExcludesItFromSemantics()
    {
        var manager = new TestSliverChildManager(childCount: 4, childExtent: 5);
        var prototype = new FixedSizeBox(new Size(40, 60));
        var sliver = new RenderSliverPrototypeExtentList(prototype, manager);
        manager.AttachOwner(sliver);
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 120,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 120,
            RemainingCacheExtent: 120);

        sliver.LayoutWithSliverConstraints(constraints);

        Assert.Equal(new Size(100, 60), prototype.Size);
        Assert.Equal(240, sliver.Geometry.ScrollExtent);
        Assert.Equal([0, 1], ActiveIndices(sliver));
        Assert.All(ActiveChildren(sliver), child => Assert.Equal(new Size(100, 60), child.Size));

        var lifecycleChildren = new List<RenderObject>();
        sliver.VisitChildren(lifecycleChildren.Add);
        Assert.Contains(prototype, lifecycleChildren);

        var semanticChildren = new List<RenderObject>();
        sliver.VisitChildrenForSemantics(child => semanticChildren.Add(child));
        Assert.DoesNotContain(prototype, semanticChildren);
        Assert.Equal(2, semanticChildren.Count);

        sliver.PrototypeChild = new FixedSizeBox(new Size(40, 40));
        sliver.LayoutWithSliverConstraints(constraints);
        Assert.Equal(160, sliver.Geometry.ScrollExtent);
        Assert.All(ActiveChildren(sliver), child => Assert.Equal(40, child.Size.Height));
    }

    [Fact]
    public void SliverPrototypeExtentList_ElementOwnsPrototypeOutsideLazyChildList()
    {
        var widget = new CustomScrollView(
            cacheExtent: 0,
            slivers:
            [
                SliverPrototypeExtentList.Builder(
                    itemCount: 5,
                    prototypeItem: new SizedBox(height: 55),
                    itemBuilder: (_, index) => new SizedBox(
                        height: 10,
                        key: new ValueKey<int>(index)),
                    addAutomaticKeepAlives: false),
            ]);
        var harness = new WidgetRenderHarness(widget);

        harness.Pump(new Size(100, 120));

        var sliver = Assert.IsType<RenderSliverPrototypeExtentList>(
            FindRenderObject<RenderSliverPrototypeExtentList>(harness.RenderView));
        Assert.NotNull(sliver.PrototypeChild);
        Assert.Equal(new Size(100, 55), sliver.PrototypeChild!.Size);
        Assert.Equal(275, sliver.Geometry.ScrollExtent);
        Assert.Equal([0, 1, 2], ActiveIndices(sliver));
        Assert.Equal(3, sliver.ChildCount);
    }

    [Fact]
    public void RenderSliverGrid_ComputesVisibleChildren_AndCrossAxisOffsets()
    {
        var manager = new TestSliverChildManager(childCount: 100, childExtent: 10);
        var sliverGrid = new RenderSliverGrid(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 10,
                crossAxisSpacing: 10,
                mainAxisExtent: 40),
            childManager: manager);
        manager.AttachOwner(sliverGrid);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: viewportOffset,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0));
        viewport.Insert(sliverGrid);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        // `TestSliverChildManager.EstimateMaxScrollOffset` is Flutter's own render-test extrapolation
        // (`childCount * (trailing - leading) / reifiedCount`), which is what `RenderSliverGrid` asks
        // for: 100 * 190 / 8 = 2375, less the 200 px viewport. The widget-level `SliverGrid` instead
        // resolves the grid layout's exact extent - see SliverChildManagerTests.
        Assert.Equal(2175, viewportOffset.MaxScrollExtent);
        Assert.Contains(6, ActiveIndices(sliverGrid));
        Assert.DoesNotContain(8, ActiveIndices(sliverGrid));

        var firstChild = sliverGrid.FirstChild!;
        var secondChild = sliverGrid.ChildAfter(firstChild)!;
        var firstParentData = (SliverGridParentData)firstChild.parentData!;
        var secondParentData = (SliverGridParentData)secondChild.parentData!;
        Assert.Equal(0, firstParentData.CrossAxisOffset);
        Assert.Equal(55, secondParentData.CrossAxisOffset);

        viewportOffset.JumpTo(500);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.DoesNotContain(0, ActiveIndices(sliverGrid));
        Assert.Contains(20, ActiveIndices(sliverGrid));
        Assert.InRange(manager.MaxCreatedIndex, 27, 40);
    }

    [Fact]
    public void RenderSliverGrid_KeepAliveChild_IsReused_WhenReturningToViewport()
    {
        var manager = new TestSliverChildManager(
            childCount: 200,
            childExtent: 50,
            keepAliveIndices: [0]);
        var sliverGrid = new RenderSliverGrid(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisExtent: 50),
            childManager: manager);
        manager.AttachOwner(sliverGrid);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset);
        viewport.Insert(sliverGrid);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverGrid));

        viewportOffset.JumpTo(600);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverGrid));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewportOffset.JumpTo(0);
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverGrid));
    }

    private static IReadOnlyList<int> ActiveIndices(RenderSliverMultiBoxAdaptor sliverList)
    {
        var indices = new List<int>();
        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            indices.Add(((SliverMultiBoxAdaptorParentData)child.parentData!).Index!.Value);
        }

        return indices;
    }

    private static IReadOnlyList<RenderBox> ActiveChildren(RenderSliverMultiBoxAdaptor sliverList)
    {
        var children = new List<RenderBox>();
        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            children.Add(child);
        }

        return children;
    }

    private static bool CoversViewportPosition(RenderSliverMultiBoxAdaptor sliverList, double viewportY)
    {
        const double epsilon = 0.0001;
        double sliverMainAxisOffset = SliverOffsetFromViewport(sliverList);

        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            var parentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            if (!child.HasSize)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Active child index {parentData.Index} has no size. Active children: {DescribeActiveChildren(sliverList)}");
            }

            double top = sliverMainAxisOffset + parentData.offset.Y;
            double bottom = top + child.Size.Height;
            if (top <= viewportY + epsilon && bottom >= viewportY - epsilon)
            {
                return true;
            }
        }

        return false;
    }

    private static string DescribeActiveChildren(RenderSliverMultiBoxAdaptor sliverList)
    {
        var parts = new List<string>();
        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            var parentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            parts.Add($"{parentData.Index}(hasSize={child.HasSize})");
        }

        return string.Join(", ", parts);
    }

    private static double SliverOffsetFromViewport(RenderSliver sliver)
    {
        double offset = 0.0;
        RenderObject? current = sliver;

        while (current is RenderSliver currentSliver)
        {
            if (currentSliver.parentData is SliverPhysicalParentData parentData)
            {
                offset += parentData.offset.Y;
            }

            var parent = currentSliver.Parent;
            if (parent is RenderViewport)
            {
                break;
            }

            current = parent;
        }

        return offset;
    }

    [Fact]
    public void Scrollable_NeverScrollableScrollPhysics_IgnoresDragsAndPointerScrolls()
    {
        GestureBinding.Instance.ResetForTests();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            ListView.Builder(
                itemCount: 40,
                itemExtent: 40,
                controller: controller,
                physics: new NeverScrollableScrollPhysics(),
                addAutomaticKeepAlives: false,
                itemBuilder: (_, _) => new SizedBox(height: 40)));
        var viewport = new Size(200, 240);
        harness.Pump(viewport);

        ScrollPosition position = controller.PrimaryPosition!;
        Assert.True(position.MaxScrollExtent > 0);

        DragBy(harness, pointer: 810, from: new Point(80, 200), delta: -120);
        harness.Pump(viewport);
        Assert.Equal(0.0, position.Pixels);

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerScrollEvent(
                pointer: 811,
                kind: PointerDeviceKind.Mouse,
                position: new Point(80, 100),
                buttons: PointerButtons.None,
                scrollDelta: new Point(0, 60),
                timestampUtc: DateTime.UtcNow));
        Assert.Equal(0.0, position.Pixels);

        // The controller still owns the position: only *user* scrolling is refused.
        controller.JumpTo(120);
        Assert.Equal(120.0, position.Pixels);
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Scrollable_AlwaysScrollableScrollPhysics_AcceptsDragsWhenTheContentFits()
    {
        GestureBinding.Instance.ResetForTests();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            ListView.Builder(
                itemCount: 2,
                itemExtent: 40,
                controller: controller,
                physics: new AlwaysScrollableScrollPhysics(parent: new BouncingScrollPhysics()),
                addAutomaticKeepAlives: false,
                itemBuilder: (_, _) => new SizedBox(height: 40)));
        var viewport = new Size(200, 240);
        harness.Pump(viewport);

        ScrollPosition position = controller.PrimaryPosition!;

        // Nothing to scroll to, yet the drag is accepted and rubber-bands the content.
        Assert.Equal(position.MinScrollExtent, position.MaxScrollExtent);
        DragBy(harness, pointer: 812, from: new Point(80, 60), delta: 90);
        Assert.True(position.Pixels < 0.0, $"Expected overscroll, got {position.Pixels}.");
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Scrollable_PhysicsMinFlingVelocity_ReachesTheDragRecognizer()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        try
        {
            // The same gesture is a fling under the default physics and not a fling under physics
            // that raise the floor, which is only possible if the value reaches the recognizer.
            Assert.IsType<BallisticScrollActivity>(FlingAndReadActivity(new ClampingScrollPhysics(), 813));
            Assert.IsType<IdleScrollActivity>(FlingAndReadActivity(new UnflingableScrollPhysics(), 814));
        }
        finally
        {
            GestureBinding.Instance.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scrollable_RecommendDeferredLoadingForContext_FollowsTheActivityVelocity()
    {
        Scheduler.ResetForTests();
        try
        {
            var controller = new ScrollController();
            BuildContext? itemContext = null;
            var harness = new WidgetRenderHarness(
                new View(
                    // The raw view is 200 x 400 physical pixels, so the threshold is 400 logical px/s.
                    view: new FlutterView(new Size(200, 400), devicePixelRatio: 2.0),
                    child: new MediaQuery(
                        // A nested override must not change the raw-view heuristic threshold.
                        data: new MediaQueryData(Size: new Size(5, 5)),
                        child: ListView.Builder(
                            itemCount: 40,
                            itemExtent: 40,
                            controller: controller,
                            physics: new ClampingScrollPhysics(),
                            addAutomaticKeepAlives: false,
                            itemBuilder: (context, _) =>
                            {
                                itemContext ??= context;
                                return new SizedBox(height: 40);
                            }))));
            harness.Pump(new Size(100, 200));

            Assert.NotNull(itemContext);
            Assert.False(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value));

            ScrollPosition position = controller.PrimaryPosition!;

            // Park the position mid-list so a fling in either direction has somewhere to go.
            controller.JumpTo(200.0);
            harness.Pump(new Size(100, 200));
            Scheduler.PumpFrameForTests();

            position.GoBallistic(-300.0);
            Assert.False(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value));

            position.GoBallistic(-5000.0);
            Assert.True(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value));

            // A request for the other axis walks past this scrollable and finds none.
            Assert.False(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value, Axis.Horizontal));
            Assert.True(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value, Axis.Vertical));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scrollable_JumpContributesImpliedVelocityUntilTheNextFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            var controller = new ScrollController();
            BuildContext? itemContext = null;
            var harness = new WidgetRenderHarness(
                new View(
                    view: new FlutterView(new Size(200, 400)),
                    child: ListView.Builder(
                        itemCount: 40,
                        itemExtent: 40,
                        controller: controller,
                        physics: new ClampingScrollPhysics(),
                        addAutomaticKeepAlives: false,
                        itemBuilder: (context, _) =>
                        {
                            itemContext ??= context;
                            return new SizedBox(height: 40);
                        })));
            harness.Pump(new Size(100, 200));

            controller.JumpTo(1000.0);

            Assert.True(Scrollable.RecommendDeferredLoadingForContext(itemContext!.Value));

            Scheduler.PumpFrameForTests();

            Assert.False(Scrollable.RecommendDeferredLoadingForContext(itemContext.Value));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scrollable_RecommendDeferredLoadingForContext_IsFalseOutsideAScrollable()
    {
        BuildContext? context = null;
        var harness = new WidgetRenderHarness(
            new Builder(builder: buildContext =>
            {
                context = buildContext;
                return new SizedBox(width: 10, height: 10);
            }));
        harness.Pump(new Size(100, 100));

        Assert.NotNull(context);
        Assert.False(Scrollable.RecommendDeferredLoadingForContext(context!.Value));
    }

    // ---- ScrollContext / ignore-pointer parity (scroll_context.dart, scroll_activity.dart) ----

    [Fact]
    public void ScrollActivity_ShouldIgnorePointer_FollowsFlutterPerActivity()
    {
        Scheduler.ResetForTests();
        try
        {
            var context = new TestScrollContext();
            using var position = new ScrollPosition(new ClampingScrollPhysics(), context);
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);

            // Idle and hold let pointer events through to the children.
            Assert.IsType<IdleScrollActivity>(position.Activity);
            Assert.False(position.Activity.ShouldIgnorePointer);
            Assert.False(position.ShouldIgnorePointer);
            position.Hold();
            Assert.False(position.Activity.ShouldIgnorePointer);
            Assert.False(context.IgnorePointer);

            // A touch drag ignores them from the moment it is recognized ...
            position.Drag(new DragStartDetails(new Point(0, 0), Kind: PointerDeviceKind.Touch));
            Assert.True(position.Activity.ShouldIgnorePointer);
            Assert.True(context.IgnorePointer);

            // ... and the fling that follows it keeps ignoring them until it stops.
            position.GoBallistic(500);
            Assert.IsType<BallisticScrollActivity>(position.Activity);
            Assert.True(position.Activity.ShouldIgnorePointer);
            Assert.True(context.IgnorePointer);

            // A trackpad drag does not (Flutter's `_kind != PointerDeviceKind.trackpad`) ...
            position.Drag(new DragStartDetails(new Point(0, 0), Kind: PointerDeviceKind.Trackpad));
            Assert.False(position.Activity.ShouldIgnorePointer);
            Assert.False(context.IgnorePointer);
            // ... and neither does the inertia following it: the ballistic activity inherits the
            // position's flag at the moment it starts.
            position.GoBallistic(500);
            Assert.IsType<BallisticScrollActivity>(position.Activity);
            Assert.False(position.Activity.ShouldIgnorePointer);
            Assert.False(context.IgnorePointer);

            // A driven animation always ignores pointers.
            position.AnimateTo(300, TimeSpan.FromMilliseconds(200));
            Assert.IsType<DrivenScrollActivity>(position.Activity);
            Assert.True(position.Activity.ShouldIgnorePointer);
            Assert.True(context.IgnorePointer);
            position.GoIdle();
            Assert.False(context.IgnorePointer);

            // The context is only told about transitions, never re-told the same value.
            Assert.Equal([true, false, true, false], context.IgnorePointerLog);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollPosition_OverscrolledPosition_LetsChildrenReceivePointers()
    {
        Scheduler.ResetForTests();
        try
        {
            var context = new TestScrollContext();
            using var position = new ScrollPosition(new BouncingScrollPhysics(), context);
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);

            // Drag past the leading edge: the drag itself ignores pointers ...
            position.Drag(new DragStartDetails(new Point(0, 0), Kind: PointerDeviceKind.Touch));
            Assert.True(context.IgnorePointer);
            position.ApplyUserOffset(80);
            Assert.True(position.OutOfRange);
            // ... but as soon as the pixels leave the range the flag is dropped, and the ballistic
            // settle that follows starts without it, so children can be tapped while the view
            // springs back (Flutter's `shouldIgnorePointer => !outOfRange && ...`).
            Assert.False(context.IgnorePointer);
            Assert.False(position.ShouldIgnorePointer);
            position.GoBallistic(0);
            Assert.IsType<BallisticScrollActivity>(position.Activity);
            Assert.False(position.Activity.ShouldIgnorePointer);
            Assert.False(context.IgnorePointer);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollPosition_DrivesTheContext_ForCanDragSemanticsAndSaveOffset()
    {
        Scheduler.ResetForTests();
        try
        {
            var context = new TestScrollContext(AxisDirection.Right, devicePixelRatio: 3.0);
            using var position = new ScrollPosition(new ClampingScrollPhysics(), context, initialPixels: 20);
            Assert.Same(context, position.Context);
            Assert.Equal(AxisDirection.Right, position.AxisDirection);
            Assert.Equal(3.0, position.DevicePixelRatio);

            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);
            // Dimensions arriving re-evaluate the physics' shouldAcceptUserOffset through setCanDrag.
            Assert.True(context.CanDrag);
            // At pixels 20 on a horizontal axis both scroll-left and scroll-right are possible.
            Assert.Equal(
                SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight,
                context.SemanticsActions);

            position.JumpTo(0);
            position.ApplyContentDimensions(0, 0);
            Assert.False(context.CanDrag);

            // Ending a scroll persists the offset through the context (restoration hook).
            position.ApplyContentDimensions(0, 1000);
            position.JumpTo(40);
            position.DidEndScroll();
            Assert.Equal(40.0, Assert.Single(context.SavedOffsets));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollPosition_ConstructorAbsorbsTheOldPosition_AndPushesItsIgnoreFlag()
    {
        Scheduler.ResetForTests();
        try
        {
            var oldContext = new TestScrollContext();
            var oldPosition = new ScrollPosition(new ClampingScrollPhysics(), oldContext, initialPixels: 30);
            oldPosition.ApplyViewportDimension(100);
            oldPosition.ApplyContentDimensions(0, 1000);
            oldPosition.Drag(new DragStartDetails(new Point(0, 0), Kind: PointerDeviceKind.Touch));
            oldPosition.GoBallistic(800);
            ScrollActivity ballistic = oldPosition.Activity;
            Assert.IsType<BallisticScrollActivity>(ballistic);

            var context = new TestScrollContext();
            using var position = new ScrollPosition(
                new ClampingScrollPhysics(),
                context,
                initialPixels: 0,
                oldPosition: oldPosition);

            // Dart's base constructor absorbs before initialPixels is looked at: the absorbed
            // pixels and activity win, and the new context learns the activity's ignore flag.
            Assert.Equal(30.0, position.Pixels);
            Assert.Same(ballistic, position.Activity);
            Assert.True(context.IgnorePointer);
            Assert.IsType<IdleScrollActivity>(oldPosition.Activity);
            oldPosition.Dispose();
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollController_CreateScrollPosition_ReceivesTheContextAndTheReplacedPosition()
    {
        var controller = new RecordingScrollController();
        var harness = new WidgetRenderHarness(
            new PhysicsSwitcher(
                controller,
                new ClampingScrollPhysics(),
                out Action<ScrollPhysics> setPhysics));
        harness.Pump(new Size(200, 240));

        (IScrollContext context, ScrollPosition? oldPosition) = Assert.Single(controller.Calls);
        Assert.Null(oldPosition);
        Scrollable.ScrollableState state = Assert.IsAssignableFrom<Scrollable.ScrollableState>(context);
        Assert.Same(state, Scrollable.MaybeOf(controller.PrimaryPosition!.Context.NotificationContext!.Value));
        Assert.Same(state.Position, controller.PrimaryPosition);
        ScrollPosition first = controller.PrimaryPosition!;

        // A physics change replaces the position: the old one is handed to the controller so the
        // new one can absorb it.
        setPhysics(new BouncingScrollPhysics());
        harness.Pump(new Size(200, 240));
        Assert.Equal(2, controller.Calls.Count);
        Assert.Same(first, controller.Calls[1].OldPosition);
        Assert.Same(context, controller.Calls[1].Context);
    }

    [Fact]
    public void Scrollable_HoldDoesNotDisableUserInteraction()
    {
        // Regression test for https://github.com/flutter/flutter/issues/66816.
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
        try
        {
            var controller = new ScrollController();
            var harness = new WidgetRenderHarness(TappableScrollView(controller));
            var viewport = new Size(200, 240);
            harness.Pump(viewport);
            RenderIgnorePointer ignorePointer = ViewportIgnorePointer(harness);
            Assert.False(ignorePointer.Ignoring);

            DateTime now = DateTime.UtcNow;
            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerDownEvent(
                    820, PointerDeviceKind.Touch, new Point(80, 100), PointerButtons.Primary, now));
            Assert.IsType<HoldScrollActivity>(controller.PrimaryPosition!.Activity);
            Assert.False(ignorePointer.Ignoring);

            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerUpEvent(
                    820, PointerDeviceKind.Touch, new Point(80, 100), PointerButtons.None, now.AddMilliseconds(30)));
            Assert.False(ignorePointer.Ignoring);
        }
        finally
        {
            GestureBinding.Instance.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scrollable_DragDisablesUserInteractionWhenRecognized_BallisticUntilItStops()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
        try
        {
            var controller = new ScrollController();
            var harness = new WidgetRenderHarness(TappableScrollView(controller));
            var viewport = new Size(200, 240);
            harness.Pump(viewport);
            RenderIgnorePointer ignorePointer = ViewportIgnorePointer(harness);
            Assert.False(ignorePointer.Ignoring);

            DateTime now = DateTime.UtcNow;
            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerDownEvent(
                    821, PointerDeviceKind.Touch, new Point(80, 200), PointerButtons.Primary, now));
            Assert.False(ignorePointer.Ignoring);

            // Starts ignoring when the drag is recognized.
            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerMoveEvent(
                    821, PointerDeviceKind.Touch, new Point(80, 170), PointerButtons.Primary, true,
                    now.AddMilliseconds(16)));
            Assert.IsType<DragScrollActivity>(controller.PrimaryPosition!.Activity);
            Assert.True(ignorePointer.Ignoring);

            // A fling keeps ignoring while the ballistic activity runs ...
            for (int step = 2; step <= 4; step++)
            {
                GestureBinding.Instance.HandlePointerEvent(
                    harness.RenderView,
                    new PointerMoveEvent(
                        821, PointerDeviceKind.Touch, new Point(80, 200 - (step * 30)), PointerButtons.Primary, true,
                        now.AddMilliseconds(16 * step)));
            }

            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerUpEvent(
                    821, PointerDeviceKind.Touch, new Point(80, 80), PointerButtons.None, now.AddMilliseconds(80)));
            Assert.IsType<BallisticScrollActivity>(controller.PrimaryPosition!.Activity);
            Assert.True(ignorePointer.Ignoring);

            // ... and stops when the activity ends.
            for (int frame = 1; frame <= 600 && controller.PrimaryPosition!.Activity is not IdleScrollActivity; frame++)
            {
                Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16 * frame));
                harness.Pump(viewport);
            }

            Assert.IsType<IdleScrollActivity>(controller.PrimaryPosition!.Activity);
            Assert.False(ignorePointer.Ignoring);
        }
        finally
        {
            GestureBinding.Instance.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scrollable_NotificationContext_SitsInsideTheScrollBehaviorWrappers()
    {
        var controller = new ScrollController();
        var seen = new List<ScrollNotification>();
        var harness = new WidgetRenderHarness(
            new ScrollConfiguration(
                behavior: new NotifyingScrollBehavior(seen.Add),
                child: ListView.Builder(
                    itemCount: 40,
                    itemExtent: 40,
                    controller: controller,
                    addAutomaticKeepAlives: false,
                    itemBuilder: (_, _) => new SizedBox(height: 40))));
        harness.Pump(new Size(200, 240));

        // The scrollbar/overscroll wrappers a ScrollBehavior builds live *between* the state and
        // the gesture detector, so notifications dispatched from the notification context reach them.
        controller.PrimaryPosition!.DidStartScroll();
        ScrollNotification notification = Assert.Single(seen);
        Assert.IsType<ScrollStartNotification>(notification);
        // And Scrollable.of(notification.context) resolves to the scrollable that sent it.
        Assert.Same(controller.PrimaryPosition!.Context, Scrollable.Of(notification.Context!.Value));
    }

    private static Widget TappableScrollView(ScrollController controller) => new CustomScrollView(
        controller: controller,
        physics: new AlwaysScrollableScrollPhysics(),
        slivers:
        [
            new SliverToBoxAdapter(
                child: new SizedBox(height: 2000, child: new GestureDetector(onTap: static () => { }))),
        ]);

    /// <summary>The <see cref="IgnorePointer"/> a scrollable wraps its viewport in.</summary>
    private static RenderIgnorePointer ViewportIgnorePointer(WidgetRenderHarness harness)
    {
        List<RenderIgnorePointer> found = [];
        void Visit(RenderObject node)
        {
            if (node is RenderIgnorePointer ignorePointer && FindDescendant<RenderViewport>(ignorePointer) != null)
            {
                found.Add(ignorePointer);
            }

            node.VisitChildren(Visit);
        }

        Visit(harness.RenderView);
        return Assert.Single(found);
    }

    private static T? FindDescendant<T>(RenderObject root) where T : RenderObject
    {
        T? result = null;
        void Visit(RenderObject node)
        {
            if (result != null)
            {
                return;
            }

            if (node is T match)
            {
                result = match;
                return;
            }

            node.VisitChildren(Visit);
        }

        root.VisitChildren(Visit);
        return result;
    }

    private sealed class RecordingScrollController : ScrollController
    {
        public List<(IScrollContext Context, ScrollPosition? OldPosition)> Calls { get; } = [];

        public override ScrollPosition CreateScrollPosition(
            ScrollPhysics physics,
            IScrollContext context,
            ScrollPosition? oldPosition)
        {
            Calls.Add((context, oldPosition));
            return base.CreateScrollPosition(physics, context, oldPosition);
        }
    }

    /// <summary>A list whose physics can be swapped from outside, forcing a position replacement.</summary>
    private sealed class PhysicsSwitcher : StatefulWidget
    {
        private readonly ScrollController _controller;
        private readonly ScrollPhysics _initialPhysics;
        private readonly Action<Action<ScrollPhysics>> _publish;

        public PhysicsSwitcher(
            ScrollController controller,
            ScrollPhysics initialPhysics,
            out Action<ScrollPhysics> setPhysics)
        {
            _controller = controller;
            _initialPhysics = initialPhysics;
            Action<ScrollPhysics>? setter = null;
            _publish = value => setter = value;
            setPhysics = physics => setter!(physics);
        }

        public override State CreateState() => new PhysicsSwitcherState();

        private sealed class PhysicsSwitcherState : State
        {
            private ScrollPhysics _physics = null!;

            public override void InitState()
            {
                base.InitState();
                var widget = (PhysicsSwitcher)StateWidget;
                _physics = widget._initialPhysics;
                widget._publish(physics => SetState(() => _physics = physics));
            }

            public override Widget Build(BuildContext context)
            {
                var widget = (PhysicsSwitcher)StateWidget;
                return ListView.Builder(
                    itemCount: 40,
                    itemExtent: 40,
                    controller: widget._controller,
                    physics: _physics,
                    addAutomaticKeepAlives: false,
                    itemBuilder: (_, _) => new SizedBox(height: 40));
            }
        }
    }

    private sealed class NotifyingScrollBehavior(Action<ScrollNotification> onNotification) : ScrollBehavior
    {
        public override Widget BuildScrollbar(BuildContext context, Widget child, ScrollableDetails details)
        {
            return new NotificationListener<ScrollNotification>(
                onNotification: notification =>
                {
                    onNotification(notification);
                    return false;
                },
                child: child);
        }
    }

    private static ScrollActivity FlingAndReadActivity(ScrollPhysics physics, int pointer)
    {
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(
            ListView.Builder(
                itemCount: 40,
                itemExtent: 40,
                controller: controller,
                physics: physics,
                addAutomaticKeepAlives: false,
                itemBuilder: (_, _) => new SizedBox(height: 40)));
        harness.Pump(new Size(200, 240));

        DragBy(harness, pointer: pointer, from: new Point(80, 200), delta: -120, stepMilliseconds: 16);
        return controller.PrimaryPosition!.Activity;
    }

    /// <summary>
    /// Drives a full press-move-release pointer sequence down the vertical axis, in four steps so
    /// the velocity tracker has enough samples to estimate a fling.
    /// </summary>
    private static void DragBy(
        WidgetRenderHarness harness,
        int pointer,
        Point from,
        double delta,
        int stepMilliseconds = 30)
    {
        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(pointer, PointerDeviceKind.Touch, from, PointerButtons.Primary, now));

        Point position = from;
        for (int step = 1; step <= 4; step++)
        {
            position = new Point(from.X, from.Y + (delta * step / 4.0));
            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerMoveEvent(
                    pointer,
                    PointerDeviceKind.Touch,
                    position,
                    PointerButtons.Primary,
                    true,
                    now.AddMilliseconds(stepMilliseconds * step)));
        }

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.None,
                now.AddMilliseconds(stepMilliseconds * 5)));
    }

    /// <summary>Physics whose fling floor no ordinary gesture can reach.</summary>
    private sealed class UnflingableScrollPhysics : ScrollPhysics
    {
        public UnflingableScrollPhysics(ScrollPhysics? parent = null) : base(parent)
        {
        }

        public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
        {
            return new UnflingableScrollPhysics(BuildParent(ancestor));
        }

        public override double MinFlingVelocity => 1e6;
    }

    private static TRenderObject? FindRenderObject<TRenderObject>(RenderObject root) where TRenderObject : RenderObject
    {
        if (root is TRenderObject typed)
        {
            return typed;
        }

        TRenderObject? found = null;
        root.VisitChildren(child =>
        {
            if (found != null)
            {
                return;
            }

            found = FindRenderObject<TRenderObject>(child);
        });
        return found;
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

        protected override bool HitTestSelf(Point position)
        {
            return true;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class CorrectingSliver : RenderSliver
    {
        private readonly double _correction;
        private readonly double _scrollExtent;
        private bool _didCorrect;

        public CorrectingSliver(double correction, double scrollExtent)
        {
            _correction = correction;
            _scrollExtent = scrollExtent;
        }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            if (!_didCorrect && Math.Abs(constraints.ScrollOffset) > 0.0001)
            {
                _didCorrect = true;
                Geometry = new SliverGeometry(ScrollOffsetCorrection: _correction);
                return;
            }

            double remaining = Math.Max(0, _scrollExtent - constraints.ScrollOffset);
            double paintExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
            double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: _scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: layoutExtent,
                MaxPaintExtent: _scrollExtent,
                CacheExtent: paintExtent,
                HasVisualOverflow: constraints.ScrollOffset > 0 || remaining > constraints.RemainingPaintExtent);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class PaintTrackingSliver(string name, List<string> paintOrder) : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            Geometry = new SliverGeometry(
                ScrollExtent: 50,
                PaintExtent: Math.Min(50, constraints.RemainingPaintExtent),
                LayoutExtent: Math.Min(50, constraints.RemainingPaintExtent),
                MaxPaintExtent: 50);
        }

        public override void Paint(PaintingContext ctx, Point offset) => paintOrder.Add(name);
    }

    private sealed class ConstraintCapturingSliver : RenderSliver
    {
        private readonly double _scrollExtent;

        public ConstraintCapturingSliver(double scrollExtent)
        {
            _scrollExtent = scrollExtent;
        }

        public SliverConstraints LastConstraints { get; private set; }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            LastConstraints = constraints;
            double remaining = Math.Max(0, _scrollExtent - constraints.ScrollOffset);
            double paintExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
            double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: _scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: layoutExtent,
                MaxPaintExtent: _scrollExtent,
                CacheExtent: paintExtent,
                HasVisualOverflow: constraints.ScrollOffset > 0 || remaining > constraints.RemainingPaintExtent);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class TestSliverChildManager : IRenderSliverBoxChildManager
    {
        private readonly int _childCount;
        private readonly double _childExtent;
        private readonly HashSet<int> _keepAliveIndices;
        private readonly Dictionary<int, RenderBox> _childrenByIndex = [];
        private readonly Dictionary<RenderBox, int> _indexByChild = [];
        private readonly Dictionary<int, int> _createCountByIndex = [];
        private RenderSliverMultiBoxAdaptor _owner = null!;

        public TestSliverChildManager(int childCount, double childExtent, IReadOnlyCollection<int>? keepAliveIndices = null)
        {
            _childCount = childCount;
            _childExtent = childExtent;
            _keepAliveIndices = keepAliveIndices != null
                ? [.. keepAliveIndices]
                : [];
        }

        public int ChildCount => _childCount;

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

        public int MaxCreatedIndex { get; private set; } = -1;

        public int RemoveCount { get; private set; }
        public HashSet<int> RemovedIndices { get; } = [];

        public int CreateCountFor(int index)
        {
            return _createCountByIndex.TryGetValue(index, out int count) ? count : 0;
        }

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner)
        {
            _owner = owner;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index >= _childCount)
            {
                return;
            }

            if (_childrenByIndex.ContainsKey(index))
            {
                return;
            }

            var child = new FixedSizeBox(new Size(100, _childExtent));
            _childrenByIndex[index] = child;
            _indexByChild[child] = index;
            _createCountByIndex[index] = _createCountByIndex.TryGetValue(index, out int createdCount)
                ? createdCount + 1
                : 1;
            MaxCreatedIndex = Math.Max(MaxCreatedIndex, index);
            _owner.Insert(child, after);
            if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.KeepAlive = _keepAliveIndices.Contains(index);
            }

            return;
        }

        public void RemoveChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out int index))
            {
                return;
            }

            _indexByChild.Remove(child);
            _childrenByIndex.Remove(index);
            RemoveCount += 1;
            RemovedIndices.Add(index);
            _owner.Remove(child);
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out int index))
            {
                return;
            }

            if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.Index = index;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }

    private sealed class VariableExtentSliverChildManager : IRenderSliverBoxChildManager
    {
        private readonly int _childCount;
        private readonly Func<int, double> _extentForIndex;
        private readonly Dictionary<int, RenderBox> _childrenByIndex = [];
        private readonly Dictionary<RenderBox, int> _indexByChild = [];
        private RenderSliverMultiBoxAdaptor _owner = null!;

        public VariableExtentSliverChildManager(int childCount, Func<int, double> extentForIndex)
        {
            _childCount = childCount;
            _extentForIndex = extentForIndex;
            TotalContentExtent = Enumerable.Range(0, childCount).Sum(index => Math.Max(0, _extentForIndex(index)));
        }

        public int ChildCount => _childCount;

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

        public double TotalContentExtent { get; }

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner)
        {
            _owner = owner;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= _childCount)
            {
                return;
            }

            if (_childrenByIndex.ContainsKey(index))
            {
                return;
            }

            double extent = Math.Max(0, _extentForIndex(index));
            var child = new FixedSizeBox(new Size(100, extent));
            _childrenByIndex[index] = child;
            _indexByChild[child] = index;
            _owner.Insert(child, after);
            return;
        }

        public void RemoveChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out int index))
            {
                return;
            }

            _indexByChild.Remove(child);
            _childrenByIndex.Remove(index);
            _owner.Remove(child);
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out int index))
            {
                return;
            }

            if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.Index = index;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }
}
