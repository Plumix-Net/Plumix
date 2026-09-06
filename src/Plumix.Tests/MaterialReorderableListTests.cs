using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/reorderable_list.dart
// material_ui/lib/src/reorderable_list.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialReorderableListTests
{
    [Fact]
    public void ReorderableLists_ValidateCallbacksKeysAndExtentContracts()
    {
        IndexedWidgetBuilder builder = (_, index) => new SizedBox(key: new ValueKey<int>(index));
        ReorderCallback callback = (_, _) => { };
        ReorderDragBoundaryProvider boundaryProvider = _ =>
            new FixedRectDragBoundaryDelegate(new Rect(0, 0, 100, 100));
        ChildIndexGetter childIndexGetter = _ => 0;

        Assert.Throws<ArgumentException>(() => new ReorderableList(builder, 1));
        Assert.Throws<ArgumentException>(() => new ReorderableList(
            builder,
            1,
            onReorder: callback,
            onReorderItem: callback));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReorderableList(
            builder,
            -1,
            onReorderItem: callback));
        Assert.Throws<ArgumentException>(() => new ReorderableList(
            builder,
            1,
            onReorderItem: callback,
            itemExtent: 40,
            itemExtentBuilder: (_, _) => 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReorderableList(
            builder,
            1,
            onReorderItem: callback,
            autoScrollerVelocityScalar: 0));
        Assert.Throws<ArgumentException>(() => new ReorderableListView(
            [new SizedBox()],
            onReorderItem: callback));

        ReorderableListView list = ReorderableListView.Builder(
            builder,
            3,
            onReorderItem: callback,
            dragBoundaryProvider: boundaryProvider);
        Assert.Equal(Axis.Vertical, list.ScrollDirection);
        Assert.True(list.BuildDefaultDragHandles);
        Assert.False(list.Reverse);
        Assert.False(list.ShrinkWrap);
        Assert.Null(list.Padding);
        Assert.Equal(0.0, list.Anchor);
        Assert.Equal(DragStartBehavior.Start, list.DragStartBehavior);
        Assert.Null(list.KeyboardDismissBehavior);
        Assert.Null(list.RestorationId);
        Assert.Equal(Clip.HardEdge, list.ClipBehavior);
        Assert.Null(list.AutoScrollerVelocityScalar);
        Assert.Same(boundaryProvider, list.DragBoundaryProvider);
#pragma warning disable CS0618
        Assert.Null(list.CacheExtent);
#pragma warning restore CS0618
        Assert.Null(list.ScrollCacheExtent);

        var sliver = new SliverReorderableList(
            builder,
            3,
            findChildIndexCallback: childIndexGetter,
            onReorderItem: callback);
        Assert.Same(childIndexGetter, sliver.FindChildIndexCallback);

        ReorderableListView cachedList = ReorderableListView.Builder(
            builder,
            3,
            onReorderItem: callback,
            scrollCacheExtent: ScrollCacheExtent.Viewport(1.5));
        Assert.Equal(1.5, cachedList.ScrollCacheExtent!.Value);
        Assert.Equal(CacheExtentStyle.Viewport, cachedList.ScrollCacheExtent.Style);
        Assert.Throws<ArgumentOutOfRangeException>(() => ReorderableListView.Builder(
            builder,
            3,
            onReorderItem: callback,
            anchor: -0.01));
    }

    [Fact]
    public void ReorderableListView_ForwardsScrollViewContractsAndAnchorGeometry()
    {
        var itemKey = new LabeledGlobalKey<State>("anchored-reorderable-item");
        Widget list = ReorderableListView.Builder(
            (_, _) => new SizedBox(height: 20, key: itemKey),
            1,
            onReorderItem: (_, _) => { },
            buildDefaultDragHandles: false,
            padding: new Thickness(4),
            itemExtent: 20,
            anchor: 0.25,
            dragStartBehavior: DragStartBehavior.Down,
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag,
            restorationId: "reorderable-items",
            clipBehavior: Clip.None,
            autoScrollerVelocityScalar: 75.0);

        var configured = Assert.IsType<ReorderableListView>(list);
        Assert.Equal(new Thickness(4), configured.Padding);
        Assert.Equal(0.25, configured.Anchor);
        Assert.Equal(DragStartBehavior.Down, configured.DragStartBehavior);
        Assert.Equal(ScrollViewKeyboardDismissBehavior.OnDrag, configured.KeyboardDismissBehavior);
        Assert.Equal("reorderable-items", configured.RestorationId);
        Assert.Equal(Clip.None, configured.ClipBehavior);
        Assert.Equal(75.0, configured.AutoScrollerVelocityScalar);

        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 100));

        BuildContext itemContext = itemKey.CurrentContext!;
        Scrollable scrollable = Assert.IsType<Scrollable>(
            itemContext.FindAncestorWidgetOfExactType<Scrollable>());
        Assert.Equal(0.25, scrollable.Anchor);
        Assert.Equal(DragStartBehavior.Down, scrollable.DragStartBehavior);
        Assert.Equal(ScrollViewKeyboardDismissBehavior.OnDrag, scrollable.KeyboardDismissBehavior);
        Assert.Equal("reorderable-items", scrollable.RestorationId);
        Assert.Equal(Clip.None, scrollable.ClipBehavior);

        RenderViewport viewport = Assert.Single(FindDescendants<RenderViewport>(harness.RenderView));
        Assert.Equal(0.25, viewport.Anchor);
        Assert.Equal(Clip.None, viewport.ClipBehavior);
        Assert.Equal(new Point(4, 29), itemContext.FindRenderObject()!.GetPaintOffsetToRoot());

        SliverReorderableList sliver = Assert.IsType<SliverReorderableList>(
            itemContext.FindAncestorWidgetOfExactType<SliverReorderableList>());
        Assert.Equal(75.0, sliver.AutoScrollerVelocityScalar);
    }

    [Fact]
    public void ReorderableListView_HorizontalAxisDirectionFollowsRtlAndReverse()
    {
        Widget Build(bool reverse) => new Directionality(
            TextDirection.Rtl,
            new ReorderableListView(
                [new SizedBox(width: 40, key: new ValueKey<int>(0))],
                onReorderItem: (_, _) => { },
                buildDefaultDragHandles: false,
                scrollDirection: Axis.Horizontal,
                reverse: reverse,
                itemExtent: 40));

        using WidgetRenderHarness readingOrder = new(Wrap(Build(reverse: false)));
        readingOrder.Pump(new Size(120, 60));
        Assert.Equal(
            AxisDirection.Left,
            Assert.Single(FindDescendants<RenderViewport>(readingOrder.RenderView)).AxisDirection);

        using WidgetRenderHarness reversed = new(Wrap(Build(reverse: true)));
        reversed.Pump(new Size(120, 60));
        Assert.Equal(
            AxisDirection.Right,
            Assert.Single(FindDescendants<RenderViewport>(reversed.RenderView)).AxisDirection);
    }

    [Fact]
    public void ReorderableListView_RestorationIdPersistsOffsetInPageStorage()
    {
        var bucket = new PageStorageBucket();
        Widget Build(string identity) => new PageStorage(
            bucket,
            ReorderableListView.Builder(
                (_, index) => new SizedBox(height: 20, key: new ValueKey<int>(index)),
                10,
                onReorderItem: (_, _) => { },
                buildDefaultDragHandles: false,
                itemExtent: 20,
                restorationId: "reorderable-items",
                key: new ValueKey<string>(identity)));

        using WidgetRenderHarness harness = new(Wrap(Build("first")));
        harness.Pump(new Size(120, 80));
        Scrollable.ScrollableState initial = harness.FindState<Scrollable.ScrollableState>();
        initial.Position.JumpTo(40.0);
        harness.Pump(new Size(120, 80));

        harness.UpdateWidget(Wrap(Build("second")));
        harness.Pump(new Size(120, 80));

        Scrollable.ScrollableState restored = harness.FindState<Scrollable.ScrollableState>();
        Assert.NotSame(initial, restored);
        Assert.Equal(40.0, restored.Position.Pixels);
    }

    [Fact]
    public void DragBoundary_ProvidesGlobalLocalAndFreeRectDelegatesLikeFlutter()
    {
        var key = new LabeledGlobalKey<State>("drag-boundary-child");
        Widget widget = new Align(
            alignment: Alignment.TopLeft,
            child: new Padding(
                new Thickness(40, 30, 0, 0),
                new DragBoundary(new SizedBox(width: 100, height: 100, key: key))));
        using WidgetRenderHarness harness = new(Wrap(widget));
        harness.Pump(new Size(240, 200));

        BuildContext context = key.CurrentContext!;
        DragBoundaryDelegate<Rect> global = DragBoundary.ForRectOf(context);
        Assert.False(global.IsWithinBoundary(new Rect(10, 10, 20, 20)));
        Assert.True(global.IsWithinBoundary(new Rect(40, 30, 20, 20)));
        Assert.Equal(
            new Rect(40, 30, 20, 20),
            global.NearestPositionWithinBoundary(new Rect(10, 10, 20, 20)));

        DragBoundaryDelegate<Rect> local = DragBoundary.ForRectOf(
            context,
            useGlobalPosition: false);
        Assert.True(local.IsWithinBoundary(new Rect(50, 50, 20, 20)));
        Assert.False(local.IsWithinBoundary(new Rect(90, 90, 20, 20)));
        Assert.Equal(
            new Rect(80, 80, 20, 20),
            local.NearestPositionWithinBoundary(new Rect(90, 90, 20, 20)));
        Assert.Throws<InvalidOperationException>(() =>
            local.NearestPositionWithinBoundary(new Rect(0, 0, 101, 20)));

        var freeKey = new LabeledGlobalKey<State>("free-drag-boundary-child");
        using WidgetRenderHarness freeHarness = new(Wrap(new SizedBox(
            width: 100,
            height: 100,
            key: freeKey)));
        freeHarness.Pump(new Size(100, 100));

        BuildContext freeContext = freeKey.CurrentContext!;
        Assert.Null(DragBoundary.ForRectMaybeOf(freeContext));
        DragBoundaryDelegate<Rect> free = DragBoundary.ForRectOf(freeContext);
        var unrestricted = new Rect(300, 300, 300, 300);
        Assert.True(free.IsWithinBoundary(unrestricted));
        Assert.Equal(unrestricted, free.NearestPositionWithinBoundary(unrestricted));
    }

    [Fact]
    public void SliverReorderableList_UsesNearestAncestorDragBoundary()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var listKey = new LabeledGlobalKey<SliverReorderableListState>("bounded-reorderable");

        try
        {
            Widget list = new DragBoundary(
                new SizedBox(
                    height: 150,
                    child: new CustomScrollView(
                        slivers:
                        [
                            new SliverReorderableList(
                                itemBuilder: (_, index) => new ReorderableDragStartListener(
                                    child: new SizedBox(height: 50),
                                    index: index,
                                    key: new ValueKey<int>(index)),
                                itemCount: 3,
                                onReorderItem: (_, _) => { },
                                itemExtent: 50,
                                key: listKey),
                        ])));
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 75, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 75, new Point(100, -400), start.AddMilliseconds(100));

            Assert.Equal(0, listKey.CurrentState!.DragTranslation.Y, precision: 6);

            DispatchMove(binding, harness.RenderView, 75, new Point(100, 800), start.AddMilliseconds(200));

            Assert.Equal(100, listKey.CurrentState.DragTranslation.Y, precision: 6);
            DispatchUp(binding, harness.RenderView, 75, new Point(100, 800), start.AddMilliseconds(300));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void SliverReorderableList_ExplicitNullBoundaryProviderOverridesAncestor()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var listKey = new LabeledGlobalKey<SliverReorderableListState>("unbounded-reorderable");

        try
        {
            Widget list = new DragBoundary(
                new SizedBox(
                    height: 150,
                    child: new CustomScrollView(
                        slivers:
                        [
                            new SliverReorderableList(
                                itemBuilder: (_, index) => new ReorderableDragStartListener(
                                    child: new SizedBox(height: 50),
                                    index: index,
                                    key: new ValueKey<int>(index)),
                                itemCount: 3,
                                onReorderItem: (_, _) => { },
                                itemExtent: 50,
                                dragBoundaryProvider: _ => null,
                                key: listKey),
                        ])));
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 76, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 76, new Point(100, -400), start.AddMilliseconds(100));

            Assert.Equal(-425, listKey.CurrentState!.DragTranslation.Y, precision: 6);
            DispatchUp(binding, harness.RenderView, 76, new Point(100, -400), start.AddMilliseconds(200));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CoreReorderableList_ImmediateDragReportsAdjustedItemIndexAndLifecycle()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<int> starts = [];
        List<int> ends = [];
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(height: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                onReorderStart: starts.Add,
                onReorderEnd: ends.Add,
                itemExtent: 50);
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 71, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 71, new Point(100, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 71, new Point(100, 115), start.AddMilliseconds(200));
            CompleteDropAnimation();

            Assert.Equal([0], starts);
            Assert.Equal([3], ends);
            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CoreReorderableList_DeprecatedCallbackRetainsInsertionIndex()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
#pragma warning disable CS0618
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(height: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorder: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                itemExtent: 50);
#pragma warning restore CS0618
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 72, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 72, new Point(100, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 72, new Point(100, 115), start.AddMilliseconds(200));
            CompleteDropAnimation();

            Assert.Equal([(0, 3)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CoreReorderableList_HorizontalDragUsesMainAxisGeometry()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(width: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                itemExtent: 50,
                scrollDirection: Axis.Horizontal);
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(150, 100));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 74, new Point(25, 50), start);
            DispatchMove(binding, harness.RenderView, 74, new Point(115, 50), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 74, new Point(115, 50), start.AddMilliseconds(200));
            CompleteDropAnimation();

            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableListView_BuildsDesktopHandlesAndMobileLongPressItems()
    {
        IReadOnlyList<Widget> children =
        [
            new SizedBox(height: 48, key: new ValueKey<int>(0)),
            new SizedBox(height: 48, key: new ValueKey<int>(1)),
            new SizedBox(height: 48, key: new ValueKey<int>(2)),
        ];

        ThemeData desktopTheme = ThemeData.Light with { Platform = TargetPlatform.Windows };
        using WidgetRenderHarness desktop = new(Wrap(
            new ReorderableListView(children, onReorderItem: (_, _) => { }, itemExtent: 48),
            desktopTheme));
        desktop.Pump(new Size(240, 144));

        string dragGlyph = char.ConvertFromUtf32(Icons.DragHandle.CodePoint);
        Assert.Equal(3, FindDescendants<RenderParagraph>(desktop.RenderView)
            .Count(paragraph => paragraph.PlainText == dragGlyph));

        ThemeData mobileTheme = ThemeData.Light with { Platform = TargetPlatform.Android };
        using WidgetRenderHarness mobile = new(Wrap(
            new ReorderableListView(children, onReorderItem: (_, _) => { }, itemExtent: 48),
            mobileTheme));
        mobile.Pump(new Size(240, 144));

        Assert.DoesNotContain(
            FindDescendants<RenderParagraph>(mobile.RenderView),
            paragraph => paragraph.PlainText == dragGlyph);
    }

    [Fact]
    public void ReorderableListView_DesktopHandleResolvesDraggedMouseCursorState()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<IReadOnlySet<WidgetState>> resolvedStates = [];
        WidgetStateMouseCursor cursor = WidgetStateMouseCursor.ResolveWith(states =>
        {
            resolvedStates.Add(states);
            return states.Contains(WidgetState.Dragged)
                ? SystemMouseCursors.Grabbing
                : SystemMouseCursors.Grab;
        });

        try
        {
            ThemeData theme = ThemeData.Light with { Platform = TargetPlatform.Windows };
            Widget list = new ReorderableListView(
                [
                    new SizedBox(height: 50, key: new ValueKey<int>(0)),
                    new SizedBox(height: 50, key: new ValueKey<int>(1)),
                ],
                onReorderItem: (_, _) => { },
                itemExtent: 50,
                mouseCursor: cursor);
            using WidgetRenderHarness harness = new(Wrap(list, theme));
            harness.Pump(new Size(240, 100));

            Assert.Contains(resolvedStates, states => states.Count == 0);
            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 95, new Point(220, 25), start);
            DispatchMove(binding, harness.RenderView, 95, new Point(220, 75), start.AddMilliseconds(100));
            harness.Pump(new Size(240, 100));

            Assert.Contains(resolvedStates, states => states.Contains(WidgetState.Dragged));

            DispatchUp(binding, harness.RenderView, 95, new Point(220, 75), start.AddMilliseconds(200));
            CompleteDropAnimation();
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableListView_DesktopHandleOwnsDragAndReportsAdjustedIndex()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            IReadOnlyList<Widget> children =
            [
                new SizedBox(height: 50, key: new ValueKey<int>(0)),
                new SizedBox(height: 50, key: new ValueKey<int>(1)),
                new SizedBox(height: 50, key: new ValueKey<int>(2)),
            ];
            ThemeData theme = ThemeData.Light with { Platform = TargetPlatform.Windows };
            using WidgetRenderHarness harness = new(Wrap(
                new ReorderableListView(
                    children,
                    onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                    itemExtent: 50),
                theme));
            harness.Pump(new Size(240, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 73, new Point(220, 25), start);
            DispatchMove(binding, harness.RenderView, 73, new Point(220, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 73, new Point(220, 115), start.AddMilliseconds(200));
            CompleteDropAnimation();

            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableListView_HeaderFooterSplitPaddingLikeFlutter()
    {
        Widget list = new ReorderableListView(
            [new SizedBox(height: 40, key: new ValueKey<int>(0))],
            onReorderItem: (_, _) => { },
            buildDefaultDragHandles: false,
            padding: new Thickness(10),
            header: new SizedBox(height: 20),
            footer: new SizedBox(height: 30),
            itemExtent: 40);
        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 120));

        List<RenderSliverPadding> paddings = FindDescendants<RenderSliverPadding>(harness.RenderView);
        Assert.Equal(3, paddings.Count);
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 0, 10, 10));
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 0, 10, 0));
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 10, 10, 0));
    }

    [Fact]
    public void SliverReorderableList_ItemExtentBuilderControlsEachChildExtent()
    {
        Widget list = new CustomScrollView(
            slivers:
            [
                new SliverReorderableList(
                    itemBuilder: (_, index) => new SizedBox(key: new ValueKey<int>(index)),
                    itemCount: 3,
                    onReorderItem: (_, _) => { },
                    itemExtentBuilder: (index, dimensions) => 30 + (index * 20)),
            ]);
        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 150));

        RenderSliverVariedExtentList sliver = Assert.Single(
            FindDescendants<RenderSliverVariedExtentList>(harness.RenderView));
        List<double> heights = [];
        for (RenderBox? child = sliver.FirstChild; child is not null; child = sliver.ChildAfter(child))
        {
            heights.Add(child.Size.Height);
        }

        Assert.Equal([30, 50, 70], heights);
        Assert.Equal(150, sliver.Geometry.ScrollExtent);
    }

    [Fact]
    public void SliverReorderableList_PrototypeItemControlsEveryChildExtent()
    {
        Widget list = new CustomScrollView(
            cacheExtent: 0,
            slivers:
            [
                new SliverReorderableList(
                    itemBuilder: (_, index) => new SizedBox(
                        height: 12,
                        key: new ValueKey<int>(index)),
                    itemCount: 4,
                    onReorderItem: (_, _) => { },
                    prototypeItem: new SizedBox(height: 44)),
            ]);
        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 120));

        RenderSliverPrototypeExtentList sliver = Assert.Single(
            FindDescendants<RenderSliverPrototypeExtentList>(harness.RenderView));
        List<double> heights = [];
        for (RenderBox? child = sliver.FirstChild; child is not null; child = sliver.ChildAfter(child))
        {
            heights.Add(child.Size.Height);
        }

        Assert.Equal([44, 44, 44], heights);
        Assert.Equal(176, sliver.Geometry.ScrollExtent);
        Assert.Equal(new Size(200, 44), sliver.PrototypeChild!.Size);
    }

    [Fact]
    public void ReorderableList_UsesOverlayCapturedThemesAndCompletesCallbackAfterDropAnimation()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            Widget list = new DefaultTextStyle(
                new TextStyle(FontSize: 31),
                new ReorderableList(
                    itemBuilder: (_, index) => new ReorderableDragStartListener(
                        child: new Text($"Item {index}"),
                        index: index,
                        key: new ValueKey<int>(index)),
                    itemCount: 3,
                    onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                    itemExtent: 50));
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));
            int baselineEntries = harness.FindState<OverlayState>().Entries.Count;

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 80, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 80, new Point(100, 115), start.AddMilliseconds(100));
            harness.Pump(new Size(200, 150));

            Assert.Equal(baselineEntries + 1, harness.FindState<OverlayState>().Entries.Count);
            RenderParagraph proxyText = Assert.Single(
                FindDescendants<RenderParagraph>(harness.RenderView),
                paragraph => paragraph.PlainText == "Item 0");
            Assert.Equal(31, proxyText.FontSize);

            AnimationPump.Prime();
            double pickupClock = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(pickupClock + 0.01));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(pickupClock + 0.30));

            DispatchUp(binding, harness.RenderView, 80, new Point(100, 115), start.AddMilliseconds(200));
            Assert.Empty(reorders);
            Assert.Equal(baselineEntries + 1, harness.FindState<OverlayState>().Entries.Count);

            AnimationPump.Prime();
            double clock = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.13));
            Assert.Empty(reorders);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.30));
            Assert.Equal([(0, 2)], reorders);
            Assert.Equal(baselineEntries, harness.FindState<OverlayState>().Entries.Count);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableList_DropRebuildsMutatedKeyedItemsWithoutCorruptingSliverChildren()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();

        try
        {
            using WidgetRenderHarness harness = new(Wrap(new MutableReorderableList()));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 82, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 82, new Point(100, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 82, new Point(100, 115), start.AddMilliseconds(200));
            CompleteDropAnimation();
            harness.Pump(new Size(200, 150));

            Assert.Equal(
                ["Bravo", "Charlie", "Alpha"],
                FindDescendants<RenderParagraph>(harness.RenderView)
                    .Select(paragraph => paragraph.PlainText)
                    .Where(text => text is "Alpha" or "Bravo" or "Charlie"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableList_StationaryEdgeDragContinuesAutoScrolling()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var controller = new ScrollController();

        try
        {
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(height: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 20,
                onReorderItem: (_, _) => { },
                itemExtent: 50,
                controller: controller);
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 81, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 81, new Point(100, 190), start.AddMilliseconds(100));

            double clock = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
            harness.Pump(new Size(200, 150));
            double firstOffset = controller.Offset;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.05));
            harness.Pump(new Size(200, 150));

            Assert.True(firstOffset > 0.0);
            Assert.True(controller.Offset > firstOffset);
            DispatchUp(binding, harness.RenderView, 81, new Point(100, 190), start.AddMilliseconds(200));
        }
        finally
        {
            controller.Dispose();
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableList_ProvidesAndInvokesLocalizedCustomSemanticsActions()
    {
        List<(int OldIndex, int NewIndex)> reorders = [];
        Widget list = new ReorderableList(
            itemBuilder: (_, index) => new SizedBox(
                height: 50,
                key: new ValueKey<int>(index)),
            itemCount: 3,
            onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
            itemExtent: 50);
        using WidgetRenderHarness harness = new(Wrap(list));
        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(200, 150)));
        List<SemanticsNode> items = FlattenSemantics(root)
            .Where(node => node.CustomSemanticsActions.Count > 0)
            .OrderBy(node => node.Rect.Top)
            .ToList();

        Assert.Equal([2, 4, 2], items.Select(node => node.CustomSemanticsActions.Count));
        CustomSemanticsAction moveDown = Assert.Single(
            items[0].CustomSemanticsActions.Keys,
            action => action.Label == "Move down");
        Assert.True(harness.PerformCustomSemanticsAction(items[0].Id, moveDown));
        Assert.Equal([(0, 1)], reorders);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(new Size(360, 640)),
                new Localizations(
                    locale: new Locale("en"),
                    delegates: [DefaultWidgetsLocalizations.Delegate],
                    child: new Theme(
                        theme ?? ThemeData.Light,
                        Overlay.Wrap(child)))));
    }

    private static void DispatchDown(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerDownEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, timestamp));
    }

    private static void DispatchMove(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                down: true,
                timestamp));
    }

    private static void DispatchUp(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerUpEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, timestamp));
    }

    private static void CompleteDropAnimation()
    {
        // One frame to give the pickup and drop tickers their start timestamps, then one past the
        // 250ms proxy animation.
        AnimationPump.Advance(0.01);
        AnimationPump.Advance(0.30);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        List<T> result = [];
        if (root is null)
        {
            return result;
        }

        if (root is T value)
        {
            result.Add(value);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static IEnumerable<SemanticsNode> FlattenSemantics(SemanticsNode root)
    {
        yield return root;
        foreach (SemanticsNode child in root.Children)
        {
            foreach (SemanticsNode descendant in FlattenSemantics(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class FixedRectDragBoundaryDelegate : DragBoundaryDelegate<Rect>
    {
        private readonly Rect _boundary;

        public FixedRectDragBoundaryDelegate(Rect boundary)
        {
            _boundary = boundary;
        }

        public override bool IsWithinBoundary(Rect draggedObject)
        {
            return _boundary.Contains(draggedObject.TopLeft)
                   && _boundary.Contains(draggedObject.BottomRight);
        }

        public override Rect NearestPositionWithinBoundary(Rect draggedObject)
        {
            return new Rect(
                Math.Clamp(draggedObject.X, _boundary.Left, _boundary.Right - draggedObject.Width),
                Math.Clamp(draggedObject.Y, _boundary.Top, _boundary.Bottom - draggedObject.Height),
                draggedObject.Width,
                draggedObject.Height);
        }
    }

    private sealed class MutableReorderableList : StatefulWidget
    {
        public override State CreateState() => new MutableReorderableListState();

        private sealed class MutableReorderableListState : State
        {
            private readonly List<string> _items = ["Alpha", "Bravo", "Charlie"];

            public override Widget Build(BuildContext context)
            {
                return new ReorderableList(
                    itemBuilder: (_, index) => new ReorderableDragStartListener(
                        child: new SizedBox(height: 50, child: new Text(_items[index])),
                        index: index,
                        key: new ValueKey<string>(_items[index])),
                    itemCount: _items.Count,
                    onReorderItem: HandleReorder,
                    itemExtent: 50);
            }

            private void HandleReorder(int oldIndex, int newIndex)
            {
                SetState(() =>
                {
                    string item = _items[oldIndex];
                    _items.RemoveAt(oldIndex);
                    _items.Insert(newIndex, item);
                });
            }
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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

        public void UpdateWidget(Widget widget)
        {
            _root.Update(widget);
            _owner.FlushBuild();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public bool PerformCustomSemanticsAction(int nodeId, CustomSemanticsAction action)
        {
            return _pipeline.SemanticsOwner!.PerformCustomAction(nodeId, action);
        }

        public T FindState<T>() where T : State
        {
            T? result = null;
            Visit(_root);
            return Assert.IsType<T>(result);

            void Visit(Element element)
            {
                if (result is not null)
                {
                    return;
                }

                if (element is StatefulElement { State: T state })
                {
                    result = state;
                    return;
                }

                element.VisitChildren(Visit);
            }
        }

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild(force: true);
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _view.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child))
                {
                    _view.Child = null;
                }
            }
        }
    }
}
