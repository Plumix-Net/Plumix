using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/sliver.dart; flutter/packages/flutter/lib/src/rendering/viewport.dart; flutter/packages/flutter/lib/src/widgets/scrollable.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollPipelineTests
{
    [Fact]
    public void ScrollPosition_ClampingPhysics_ClampsToContentBounds()
    {
        var position = new ScrollPosition(initialPixels: 10);
        var notifications = 0;
        position.AddListener(() => notifications += 1);

        position.ApplyViewportDimension(120);
        position.ApplyContentDimensions(0, 60);
        position.JumpTo(1000);

        Assert.Equal(60, position.Pixels);
        Assert.True(notifications > 0);
    }

    [Fact]
    public void ScrollPosition_EndDrag_EntersBallisticActivity()
    {
        Scheduler.ResetForTests();
        try
        {
            var position = new ScrollPosition(initialPixels: 40);
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
        var first = controller.CreateScrollPosition();
        var second = controller.CreateScrollPosition();

        first.ApplyViewportDimension(100);
        first.ApplyContentDimensions(0, 200);
        second.ApplyViewportDimension(100);
        second.ApplyContentDimensions(0, 50);

        controller.Attach(first);
        controller.Attach(second);
        controller.JumpTo(120);

        Assert.Equal(120, first.Pixels);
        Assert.Equal(50, second.Pixels);
    }

    [Fact]
    public void RenderViewport_OffsetsChild_AndReportsMetrics()
    {
        double viewportExtent = -1;
        double minExtent = -1;
        double maxExtent = -1;

        var child = new FixedSizeBox(new Size(80, 600));
        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 50,
            onViewportMetricsChanged: (viewport, min, max) =>
            {
                viewportExtent = viewport;
                minExtent = min;
                maxExtent = max;
            },
            child: child);

        var root = new RenderView
        {
            Child = viewport
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        var childParentData = (BoxParentData)child.parentData!;
        Assert.Equal(new Point(0, -50), childParentData.offset);
        Assert.Equal(200, viewportExtent);
        Assert.Equal(0, minExtent);
        Assert.Equal(400, maxExtent);
    }

    [Fact]
    public void RenderViewport_LaysOutMultipleSlivers_AndAggregatesScrollExtent()
    {
        double maxExtent = -1;

        var first = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 120)));
        var second = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 160)));
        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 80,
            onViewportMetricsChanged: (_, _, max) => maxExtent = max);
        viewport.Insert(first);
        viewport.Insert(second, after: first);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 150));

        Assert.Equal(130, maxExtent);

        var firstBoxOffset = ((BoxParentData)((RenderBox)first.Child!).parentData!).offset;
        var secondBoxOffset = ((BoxParentData)((RenderBox)second.Child!).parentData!).offset;
        Assert.Equal(new Point(0, -80), firstBoxOffset);
        Assert.Equal(new Point(0, 0), secondBoxOffset);
    }

    [Fact]
    public void RenderViewport_AppliesScrollOffsetCorrection_FromSliver()
    {
        double maxExtent = -1;
        var correcting = new CorrectingSliver(
            correction: -100,
            scrollExtent: 500);
        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 100,
            onViewportMetricsChanged: (_, _, max) => maxExtent = max);
        viewport.Insert(correcting);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(0, viewport.OffsetPixels);
        Assert.Equal(300, maxExtent);
    }

    [Fact]
    public void RenderSliverPadding_ContributesPaddingToScrollExtent()
    {
        double maxExtent = -1;
        var innerSliver = new RenderSliverToBoxAdapter(new FixedSizeBox(new Size(100, 120)));
        var sliverPadding = new RenderSliverPadding(new Thickness(0, 10, 0, 20), innerSliver);
        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            onViewportMetricsChanged: (_, _, max) => maxExtent = max);
        viewport.Insert(sliverPadding);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(50, maxExtent);
        var sliverParentData = (SliverPhysicalParentData)innerSliver.parentData!;
        Assert.Equal(new Point(0, 10), sliverParentData.offset);

        viewport.OffsetPixels = 15;
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(new Point(0, 0), sliverParentData.offset);
        var innerBoxOffset = ((BoxParentData)((RenderBox)innerSliver.Child!).parentData!).offset;
        Assert.Equal(new Point(0, -5), innerBoxOffset);
    }

    [Fact]
    public void RenderViewport_AxisDirectionUp_MapsUserOffsetFromTrailingEdge()
    {
        var child = new FixedSizeBox(new Size(80, 600));
        var viewport = new RenderViewport(
            axisDirection: AxisDirection.Up,
            offsetPixels: 0,
            child: child);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        var childParentData = (BoxParentData)child.parentData!;
        Assert.Equal(new Point(0, -400), childParentData.offset);

        viewport.OffsetPixels = 400;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(new Point(0, 0), childParentData.offset);
    }

    [Fact]
    public void RenderViewport_PropagatesAxisAndGrowthDirectionToSlivers()
    {
        var sliver = new ConstraintCapturingSliver(scrollExtent: 300);
        var viewport = new RenderViewport(
            axisDirection: AxisDirection.Up,
            growthDirection: GrowthDirection.Reverse,
            offsetPixels: 0);
        viewport.Insert(sliver);

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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.InRange(manager.MaxCreatedIndex, 0, 6);
        Assert.Equal(0, manager.RemoveCount);

        viewport.OffsetPixels = 450;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.InRange(manager.MaxCreatedIndex, 9, 15);

        viewport.OffsetPixels = 0;
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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));

        viewport.OffsetPixels = 600;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverList));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewport.OffsetPixels = 0;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));
    }

    [Fact]
    public void RenderSliverList_VariableExtentChildren_ContinuouslyCoverViewportDuringScroll()
    {
        var childCount = 300;
        var manager = new VariableExtentSliverChildManager(
            childCount,
            index => index % 2 == 0 ? 44 : 4);
        var sliverList = new RenderSliverList(manager);
        manager.AttachOwner(sliverList);

        const double viewportExtent = 220;
        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            cacheExtent: 250);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, viewportExtent));

        var contentExtent = manager.TotalContentExtent;
        var maxOffsetToCheck = Math.Max(0, contentExtent - viewportExtent - 1);

        for (double offset = 0; offset <= maxOffsetToCheck; offset += 37)
        {
            viewport.OffsetPixels = offset;
            pipeline.FlushLayout(new Size(100, viewportExtent));

            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.25), $"No child covers 25% of viewport at offset {offset}.");
            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.5), $"No child covers 50% of viewport at offset {offset}.");
            Assert.True(CoversViewportPosition(sliverList, viewportExtent * 0.75), $"No child covers 75% of viewport at offset {offset}.");
        }
    }

    [Fact]
    public void Scrollable_ListViewSeparated_MaintainsViewportCoverageDuringControllerJumps()
    {
        var controller = new ScrollController();
        var widget = ListView.Separated(
            itemCount: 120,
            controller: controller,
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
        var maxOffsetToCheck = Math.Max(0, position!.MaxScrollExtent - 1);
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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            cacheExtent: 100);
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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            cacheExtent: 1,
            cacheExtentStyle: CacheExtentStyle.Viewport);
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
        double maxExtent = -1;
        var manager = new TestSliverChildManager(childCount: 100, childExtent: 10);
        var sliverList = new RenderSliverFixedExtentList(itemExtent: 40, childManager: manager);
        manager.AttachOwner(sliverList);

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            onViewportMetricsChanged: (_, _, max) => maxExtent = max);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(3800, maxExtent);
        Assert.Contains(4, ActiveIndices(sliverList));
        Assert.DoesNotContain(5, ActiveIndices(sliverList));

        viewport.OffsetPixels = 480;
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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0);
        viewport.Insert(sliverList);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));

        viewport.OffsetPixels = 600;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverList));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewport.OffsetPixels = 0;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverList));
    }

    [Fact]
    public void RenderSliverGrid_ComputesVisibleChildren_AndCrossAxisOffsets()
    {
        double maxExtent = -1;
        var manager = new TestSliverChildManager(childCount: 100, childExtent: 10);
        var sliverGrid = new RenderSliverGrid(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 10,
                crossAxisSpacing: 10,
                mainAxisExtent: 40),
            childManager: manager);
        manager.AttachOwner(sliverGrid);

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0,
            onViewportMetricsChanged: (_, _, max) => maxExtent = max);
        viewport.Insert(sliverGrid);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(2290, maxExtent);
        Assert.Contains(6, ActiveIndices(sliverGrid));
        Assert.DoesNotContain(8, ActiveIndices(sliverGrid));

        var firstChild = sliverGrid.FirstChild!;
        var secondChild = sliverGrid.ChildAfter(firstChild)!;
        var firstParentData = (SliverGridParentData)firstChild.parentData!;
        var secondParentData = (SliverGridParentData)secondChild.parentData!;
        Assert.Equal(0, firstParentData.CrossAxisOffset);
        Assert.Equal(55, secondParentData.CrossAxisOffset);

        viewport.OffsetPixels = 500;
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

        var viewport = new RenderViewport(
            axis: Axis.Vertical,
            offsetPixels: 0);
        viewport.Insert(sliverGrid);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverGrid));

        viewport.OffsetPixels = 600;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.DoesNotContain(0, ActiveIndices(sliverGrid));
        Assert.DoesNotContain(0, manager.RemovedIndices);

        viewport.OffsetPixels = 0;
        pipeline.FlushLayout(new Size(100, 200));
        Assert.Equal(1, manager.CreateCountFor(0));
        Assert.Contains(0, ActiveIndices(sliverGrid));
    }

    private static IReadOnlyList<int> ActiveIndices(RenderSliverMultiBoxAdaptor sliverList)
    {
        var indices = new List<int>();
        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            indices.Add(((SliverMultiBoxAdaptorParentData)child.parentData!).Index);
        }

        return indices;
    }

    private static bool CoversViewportPosition(RenderSliverMultiBoxAdaptor sliverList, double viewportY)
    {
        const double epsilon = 0.0001;
        var sliverMainAxisOffset = SliverOffsetFromViewport(sliverList);

        for (var child = sliverList.FirstChild; child != null; child = sliverList.ChildAfter(child))
        {
            var parentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            if (!child.HasSize)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Active child index {parentData.Index} has no size. Active children: {DescribeActiveChildren(sliverList)}");
            }

            var top = sliverMainAxisOffset + parentData.offset.Y;
            var bottom = top + child.Size.Height;
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
        var offset = 0.0;
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

            var remaining = Math.Max(0, _scrollExtent - constraints.ScrollOffset);
            var paintExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
            var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
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
            var remaining = Math.Max(0, _scrollExtent - constraints.ScrollOffset);
            var paintExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
            var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
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

        public int? ChildCount => _childCount;

        public int MaxCreatedIndex { get; private set; } = -1;

        public int RemoveCount { get; private set; }
        public HashSet<int> RemovedIndices { get; } = [];

        public int CreateCountFor(int index)
        {
            return _createCountByIndex.TryGetValue(index, out var count) ? count : 0;
        }

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner)
        {
            _owner = owner;
        }

        public bool CreateChild(int index, RenderBox? after)
        {
            if (index >= _childCount)
            {
                return false;
            }

            if (_childrenByIndex.ContainsKey(index))
            {
                return true;
            }

            var child = new FixedSizeBox(new Size(100, _childExtent));
            _childrenByIndex[index] = child;
            _indexByChild[child] = index;
            _createCountByIndex[index] = _createCountByIndex.TryGetValue(index, out var createdCount)
                ? createdCount + 1
                : 1;
            MaxCreatedIndex = Math.Max(MaxCreatedIndex, index);
            _owner.Insert(child, after);
            if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.KeepAlive = _keepAliveIndices.Contains(index);
            }

            return true;
        }

        public void RemoveChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out var index))
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
            if (!_indexByChild.TryGetValue(child, out var index))
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

        public int? ChildCount => _childCount;

        public double TotalContentExtent { get; }

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner)
        {
            _owner = owner;
        }

        public bool CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= _childCount)
            {
                return false;
            }

            if (_childrenByIndex.ContainsKey(index))
            {
                return true;
            }

            var extent = Math.Max(0, _extentForIndex(index));
            var child = new FixedSizeBox(new Size(100, extent));
            _childrenByIndex[index] = child;
            _indexByChild[child] = index;
            _owner.Insert(child, after);
            return true;
        }

        public void RemoveChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out var index))
            {
                return;
            }

            _indexByChild.Remove(child);
            _childrenByIndex.Remove(index);
            _owner.Remove(child);
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (!_indexByChild.TryGetValue(child, out var index))
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
