using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/list_view_misc_test.dart,
// list_view_test.dart, list_view_relayout_test.dart and slivers_test.dart — the behaviours those
// files assert about RenderSliverBoxChildManager and SliverChildDelegate.

namespace Plumix.Tests;

public sealed class SliverChildManagerTests
{
    [Fact]
    public void ChildManagerContract_CarriesDartsDefaultImplementations()
    {
        IRenderSliverBoxChildManager manager = new MinimalChildManager();

        Assert.Null(manager.EstimatedChildCount);
        Assert.True(manager.DebugAssertChildListLocked());
        manager.DidStartLayout();
        manager.DidFinishLayout();
    }

    [Fact]
    public void SliverChildDelegate_Defaults_MatchDart()
    {
        var childDelegate = new ProbeChildDelegate(realCount: 3);

        Assert.Null(childDelegate.EstimateMaxScrollOffset(0, 1, 0, 200));
        childDelegate.DidFinishLayout(0, 1);

        var listDelegate = new SliverChildListDelegate([new SizedBox(height: 10)]);
        Assert.Null(listDelegate.EstimateMaxScrollOffset(0, 0, 0, 10));
        Assert.Equal(1, listDelegate.EstimatedChildCount);
        Assert.Null(new SliverChildBuilderDelegate((_, _) => new SizedBox()).EstimatedChildCount);
    }

    /// <remarks>
    /// Flutter's `list_view_misc_test.dart` "SliverBlockChildListDelegate.estimateMaxScrollOffset
    /// hits end": `lastIndex == childCount - 1` short-circuits to the trailing offset.
    /// </remarks>
    [Fact]
    public void EstimateMaxScrollOffset_HitsEnd_ReturnsTrailingScrollOffset()
    {
        var widget = new CustomScrollView(
            slivers:
            [
                SliverList.FromChildren(
                    [
                        new SizedBox(height: 100),
                        new SizedBox(height: 100),
                        new SizedBox(height: 100),
                        new SizedBox(height: 100),
                        new SizedBox(height: 100),
                    ],
                    addAutomaticKeepAlives: false),
            ]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverList>(FindRenderObject<RenderSliverList>(harness.RenderView));
        IRenderSliverBoxChildManager manager = Assert.IsAssignableFrom<IRenderSliverBoxChildManager>(
            sliver.ChildManager);

        Assert.Equal(5, manager.EstimatedChildCount);
        Assert.Equal(5, manager.ChildCount);
        Assert.Equal(
            26.0,
            manager.EstimateMaxScrollOffset(
                sliver.ConstraintsForSliver,
                firstIndex: 3,
                lastIndex: 4,
                leadingScrollOffset: 25.0,
                trailingScrollOffset: 26.0));
    }

    /// <remarks>
    /// The extrapolation branch of `SliverMultiBoxAdaptorElement._extrapolateMaxScrollOffset`:
    /// average extent of the reified range times the remaining count.
    /// </remarks>
    [Fact]
    public void EstimateMaxScrollOffset_ExtrapolatesFromTheReifiedRange()
    {
        var widget = new CustomScrollView(
            slivers:
            [
                SliverFixedExtentList.Builder(
                    childCount: 10,
                    itemBuilder: (_, _) => new SizedBox(height: 100),
                    itemExtent: 100,
                    addAutomaticKeepAlives: false),
            ]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        IRenderSliverBoxChildManager manager = Assert.IsAssignableFrom<IRenderSliverBoxChildManager>(
            sliver.ChildManager);

        // reifiedCount = 2, averageExtent = (200 - 0) / 2 = 100, remainingCount = 10 - 1 - 1 = 8.
        Assert.Equal(
            1000.0,
            manager.EstimateMaxScrollOffset(
                sliver.ConstraintsForSliver,
                firstIndex: 0,
                lastIndex: 1,
                leadingScrollOffset: 0.0,
                trailingScrollOffset: 200.0));
    }

    [Fact]
    public void EstimateMaxScrollOffset_WithoutAnEstimatedChildCount_IsInfinite()
    {
        var childDelegate = new ProbeChildDelegate(realCount: 7);
        var widget = new CustomScrollView(
            slivers: [new SliverFixedExtentList(childDelegate, itemExtent: 200)]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        IRenderSliverBoxChildManager manager = Assert.IsAssignableFrom<IRenderSliverBoxChildManager>(
            sliver.ChildManager);

        Assert.Null(manager.EstimatedChildCount);
        Assert.Equal(
            double.PositiveInfinity,
            manager.EstimateMaxScrollOffset(
                sliver.ConstraintsForSliver,
                firstIndex: 0,
                lastIndex: 1,
                leadingScrollOffset: 0.0,
                trailingScrollOffset: 400.0));
        Assert.Equal(double.PositiveInfinity, sliver.Geometry.ScrollExtent);
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "SliverFixedExtentList with SliverChildBuilderDelegate
    /// auto-correct scroll offset": with no estimated child count, `childCount` finds the end of a
    /// finite list with an open-ended doubling probe followed by a binary search.
    /// </remarks>
    [Fact]
    public void ChildCount_ProbesForTheEndOfAnUnboundedDelegate()
    {
        var childDelegate = new ProbeChildDelegate(realCount: 7);
        var widget = new CustomScrollView(
            slivers: [new SliverFixedExtentList(childDelegate, itemExtent: 200)]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        IRenderSliverBoxChildManager manager = Assert.IsAssignableFrom<IRenderSliverBoxChildManager>(
            sliver.ChildManager);

        Assert.Null(manager.EstimatedChildCount);
        Assert.Equal(7, manager.ChildCount);

        // The probe is a search, not a linear walk over every index up to the bound.
        childDelegate.ResetBuildCount();
        Assert.Equal(7, manager.ChildCount);
        Assert.InRange(childDelegate.BuildCount, 1, 12);

        // The precise count is what `computeMaxScrollOffset` is built on.
        Assert.Equal(1400.0, sliver.ComputeMaxScrollOffset(sliver.ConstraintsForSliver, 200.0));
    }

    [Fact]
    public void ChildCount_OnATrulyUnboundedDelegate_Throws()
    {
        var childDelegate = new ProbeChildDelegate(realCount: int.MaxValue);
        var widget = new CustomScrollView(
            slivers: [new SliverFixedExtentList(childDelegate, itemExtent: 200)]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        IRenderSliverBoxChildManager manager = Assert.IsAssignableFrom<IRenderSliverBoxChildManager>(
            sliver.ChildManager);

        // The `ErrorDescription` half is elided outside a debug build; the summary is always there.
        var error = Assert.Throws<FlutterError>(() => manager.ChildCount);
        Assert.Contains(
            "Could not find the number of children in",
            error.Message,
            StringComparison.Ordinal);
    }

    /// <remarks>
    /// Flutter's `list_view_test.dart` "didFinishLayout has correct indices": the delegate is told
    /// the index range that was included in the layout that just finished.
    /// </remarks>
    [Fact]
    public void DidFinishLayout_ReportsTheBuiltIndexRange()
    {
        var childDelegate = new RecordingChildDelegate(childCount: 20, childExtent: 110);
        double itemExtent = 110;
        StateSetter? setState = null;

        var widget = new StatefulBuilder((_, setter) =>
        {
            setState = setter;
            return new CustomScrollView(
                slivers: [new SliverFixedExtentList(childDelegate, itemExtent: itemExtent)]);
        });

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        Assert.NotEmpty(childDelegate.Layouts);
        (int First, int Last) reported = childDelegate.Layouts[^1];
        Assert.Equal(ActiveIndices(sliver)[0], reported.First);
        Assert.Equal(ActiveIndices(sliver)[^1], reported.Last);
        Assert.Equal((0, 7), reported);

        // A taller item extent re-lays-out and reports a shorter range.
        childDelegate.Layouts.Clear();
        setState!(() => itemExtent = 210);
        harness.Pump(new Size(800, 600));

        Assert.NotEmpty(childDelegate.Layouts);
        Assert.Equal((0, 4), childDelegate.Layouts[^1]);
    }

    /// <remarks>
    /// Flutter's `RenderSliverBoxChildManager` contract: `didStartLayout` is the first manager call
    /// of every `performLayout` and `didFinishLayout` is the last, with every child mutation between.
    /// </remarks>
    [Fact]
    public void LayoutHooks_BracketEveryChildMutationOfALayoutPass()
    {
        var manager = new RecordingChildManager(childCount: 20, childExtent: 100);
        var sliver = new RenderSliverList(manager);
        manager.AttachOwner(sliver);

        var viewportOffset = new TestViewportOffset();
        var viewport = new RenderViewport(offset: viewportOffset, scrollCacheExtent: ScrollCacheExtent.Pixels(0));
        viewport.Insert(sliver);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 400));

        Assert.Equal("DidStartLayout", manager.Calls[0]);
        Assert.Equal("DidFinishLayout", manager.Calls[^1]);
        Assert.Equal(1, manager.Calls.Count(call => call == "DidStartLayout"));
        Assert.Equal(1, manager.Calls.Count(call => call == "DidFinishLayout"));
        Assert.Contains("CreateChild", manager.Calls);
        Assert.True(
            manager.Calls.IndexOf("CreateChild") > 0,
            "child creation must happen after DidStartLayout");
    }

    [Fact]
    public void LayoutHooks_FireOnEveryAdaptorFamily()
    {
        foreach (bool fixedExtent in new[] { true, false })
        {
            var manager = new RecordingChildManager(childCount: 20, childExtent: 100);
            RenderSliverMultiBoxAdaptor sliver = fixedExtent
                ? new RenderSliverFixedExtentList(itemExtent: 100, childManager: manager)
                : new RenderSliverGrid(
                    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        mainAxisExtent: 100),
                    childManager: manager);
            manager.AttachOwner(sliver);

            var viewport = new RenderViewport(
                offset: new TestViewportOffset(),
                scrollCacheExtent: ScrollCacheExtent.Pixels(0));
            viewport.Insert(sliver);
            var root = new RenderView { Child = viewport };
            var pipeline = new PipelineOwner(root);
            pipeline.Attach(root);
            pipeline.FlushLayout(new Size(100, 400));

            Assert.Equal("DidStartLayout", manager.Calls[0]);
            Assert.Equal("DidFinishLayout", manager.Calls[^1]);
        }
    }

    /// <remarks>
    /// `SliverChildDelegate.estimateMaxScrollOffset` wins over the element's extrapolation.
    /// </remarks>
    [Fact]
    public void DelegateEstimateMaxScrollOffset_OverridesTheExtrapolation()
    {
        var childDelegate = new FixedEstimateChildDelegate(childCount: 100, estimate: 4242);
        var widget = new CustomScrollView(
            slivers: [new SliverFixedExtentList(childDelegate, itemExtent: 100)]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverFixedExtentList>(
            FindRenderObject<RenderSliverFixedExtentList>(harness.RenderView));
        Assert.Equal(4242.0, sliver.Geometry.ScrollExtent);
    }

    /// <remarks>
    /// Flutter's `SliverGrid.estimateMaxScrollOffset`: with no delegate estimate, the grid layout
    /// knows the exact extent of a known number of children, so no extrapolation happens.
    /// </remarks>
    [Fact]
    public void SliverGrid_EstimateMaxScrollOffset_FallsBackToTheGridLayoutExtent()
    {
        var widget = new CustomScrollView(
            slivers:
            [
                SliverGrid.Builder(
                    childCount: 100,
                    itemBuilder: (_, _) => new SizedBox(height: 40),
                    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        mainAxisSpacing: 10,
                        crossAxisSpacing: 10,
                        mainAxisExtent: 40),
                    addAutomaticKeepAlives: false),
            ]);

        var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(100, 200));

        var sliver = Assert.IsType<RenderSliverGrid>(FindRenderObject<RenderSliverGrid>(harness.RenderView));

        // 50 rows of stride 50, less the trailing 10 px of main-axis spacing.
        Assert.Equal(2490.0, sliver.Geometry.ScrollExtent);
    }

    /// <remarks>
    /// Flutter's `list_view_relayout_test.dart` "Underflowing ListView contentExtent should track
    /// additional children": the `_didUnderflow` look-ahead in `performRebuild` keeps the max scroll
    /// offset current when the layout phase would otherwise report the stale one.
    /// </remarks>
    [Fact]
    public void UnderflowingSliverList_TracksAdditionalChildren()
    {
        int childCount = 1;
        StateSetter? setState = null;

        var widget = new StatefulBuilder((_, setter) =>
        {
            setState = setter;
            return new CustomScrollView(
                slivers:
                [
                    SliverList.FromChildren(
                        [.. Enumerable.Range(0, childCount).Select(_ => (Widget)new SizedBox(height: 100))],
                        addAutomaticKeepAlives: false),
                ]);
        });

        var harness = new WidgetRenderHarness(widget);
        var viewportSize = new Size(800, 600);
        harness.Pump(viewportSize);

        var sliver = Assert.IsType<RenderSliverList>(FindRenderObject<RenderSliverList>(harness.RenderView));
        Assert.Equal(100.0, sliver.Geometry.ScrollExtent);

        setState!(() => childCount = 3);
        harness.Pump(viewportSize);
        Assert.Equal(300.0, sliver.Geometry.ScrollExtent);

        setState(() => childCount = 0);
        harness.Pump(viewportSize);
        Assert.Equal(0.0, sliver.Geometry.ScrollExtent);
    }

    private static IReadOnlyList<int> ActiveIndices(RenderSliverMultiBoxAdaptor sliver)
    {
        var indices = new List<int>();
        for (var child = sliver.FirstChild; child != null; child = sliver.ChildAfter(child))
        {
            indices.Add(((SliverMultiBoxAdaptorParentData)child.parentData!).Index);
        }

        return indices;
    }

    private static TRenderObject? FindRenderObject<TRenderObject>(RenderObject root)
        where TRenderObject : RenderObject
    {
        if (root is TRenderObject typed)
        {
            return typed;
        }

        TRenderObject? found = null;
        root.VisitChildren(child => found ??= FindRenderObject<TRenderObject>(child));
        return found;
    }

    private sealed class FixedSizeBox(Size size) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
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
        }

        private sealed class HarnessRootElement(RenderView renderView, Widget widget) : Element(widget), IRenderObjectHost
        {
            private Element? _child;

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
                renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(renderView.Child, renderBox))
                {
                    renderView.Child = null;
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

    /// <summary>A manager that implements only the members Dart leaves abstract.</summary>
    private sealed class MinimalChildManager : IRenderSliverBoxChildManager
    {
        public int ChildCount => 0;

        public bool CreateChild(int index, RenderBox? after) => false;

        public void RemoveChild(RenderBox child)
        {
        }

        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null) => 0;

        public void DidAdoptChild(RenderBox child)
        {
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }

    /// <summary>A delegate that reports no estimated child count but is finite.</summary>
    private sealed class ProbeChildDelegate(int realCount) : SliverChildDelegate
    {
        public int BuildCount { get; private set; }

        public override int? EstimatedChildCount => null;

        public override Widget? Build(BuildContext context, int index)
        {
            BuildCount += 1;
            return index < 0 || index >= realCount ? null : new SizedBox(height: 200);
        }

        public void ResetBuildCount() => BuildCount = 0;
    }

    private sealed class RecordingChildDelegate(int childCount, double childExtent) : SliverChildDelegate
    {
        public List<(int First, int Last)> Layouts { get; } = [];

        public override int? EstimatedChildCount => childCount;

        public override Widget? Build(BuildContext context, int index)
        {
            return index < 0 || index >= childCount ? null : new SizedBox(height: childExtent);
        }

        public override void DidFinishLayout(int firstIndex, int lastIndex)
        {
            Layouts.Add((firstIndex, lastIndex));
        }
    }

    private sealed class FixedEstimateChildDelegate(int childCount, double estimate) : SliverChildDelegate
    {
        public override int? EstimatedChildCount => childCount;

        public override Widget? Build(BuildContext context, int index)
        {
            return index < 0 || index >= childCount ? null : new SizedBox(height: 100);
        }

        public override double? EstimateMaxScrollOffset(
            int firstIndex,
            int lastIndex,
            double leadingScrollOffset,
            double trailingScrollOffset) => estimate;
    }

    private sealed class RecordingChildManager(int childCount, double childExtent) : IRenderSliverBoxChildManager
    {
        private readonly Dictionary<int, RenderBox> _childrenByIndex = [];
        private readonly Dictionary<RenderBox, int> _indexByChild = [];
        private RenderSliverMultiBoxAdaptor _owner = null!;

        public List<string> Calls { get; } = [];

        public int ChildCount => childCount;

        public int? EstimatedChildCount => childCount;

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner) => _owner = owner;

        public bool CreateChild(int index, RenderBox? after)
        {
            Calls.Add("CreateChild");
            if (index < 0 || index >= childCount)
            {
                return false;
            }

            if (_childrenByIndex.ContainsKey(index))
            {
                return true;
            }

            var child = new FixedSizeBox(new Size(100, childExtent));
            _childrenByIndex[index] = child;
            _indexByChild[child] = index;
            _owner.Insert(child, after);
            return true;
        }

        public void RemoveChild(RenderBox child)
        {
            Calls.Add("RemoveChild");
            if (!_indexByChild.TryGetValue(child, out int index))
            {
                return;
            }

            _indexByChild.Remove(child);
            _childrenByIndex.Remove(index);
            _owner.Remove(child);
        }

        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null)
        {
            Calls.Add("EstimateMaxScrollOffset");
            Assert.True(lastIndex >= firstIndex);
            return childCount
                   * (trailingScrollOffset!.Value - leadingScrollOffset!.Value)
                   / (lastIndex!.Value - firstIndex!.Value + 1);
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (_indexByChild.TryGetValue(child, out int index)
                && child.parentData is SliverMultiBoxAdaptorParentData parentData)
            {
                parentData.Index = index;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }

        public void DidStartLayout() => Calls.Add("DidStartLayout");

        public void DidFinishLayout() => Calls.Add("DidFinishLayout");
    }
}
