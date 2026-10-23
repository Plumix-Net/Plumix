using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/reorderable_list_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class ReorderableListDartParityTests
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(250);
    private static Widget Wrap(Widget child) => new MaterialApp(home: child, debugShowCheckedModeBanner: false);
    private static Key ItemKey(int index) => new ValueKey<string>($"item-{index}");

    private static Widget Item(int index, double extent = 100, Axis axis = Axis.Vertical,
        bool delayed = false, bool enabled = true)
    {
        Widget child = new ColoredBox(new Color(0xFFFFFFFF), child: new SizedBox(
            width: axis == Axis.Horizontal ? extent : double.PositiveInfinity,
            height: axis == Axis.Vertical ? extent : double.PositiveInfinity,
            child: new Text($"item {index}")), key: ItemKey(index));
        return delayed
            ? new ReorderableDelayedDragStartListener(child, index, enabled, new ValueKey<int>(index))
            : new ReorderableDragStartListener(child, index, enabled, new ValueKey<int>(index));
    }

    private static Point Position(FrameworkDartTester tester, int index) =>
        tester.GetTopLeft(Find.ByKey(ItemKey(index)));

    // Flutter: all variable-extent drop cases, each axis and reverse direction.
    public static IEnumerable<object[]> DropCases()
    {
        foreach ((int from, int to) in new[] { (3, 1), (3, 3), (3, 5), (0, 5), (5, 0) })
        {
            foreach (bool reverse in new[] { false, true })
            {
                foreach (Axis axis in new[] { Axis.Vertical, Axis.Horizontal })
                {
                    yield return [from, to, reverse, axis];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(DropCases))]
    public void VariableExtentDrop_AnimatesToExactInsertionTarget(int from, int to, bool reverse, Axis axis)
    {
        using var tester = new FrameworkDartTester();
        double[] extents = [20, 50, 30, 80, 100, 30];
        List<(int, int)> reorders = [];
        tester.PumpWidget(Wrap(new CustomScrollView(
            reverse: reverse,
            scrollDirection: axis,
            slivers: [new SliverReorderableList(
                (_, index) => Item(index, extents[index], axis), extents.Length,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)))])));
        var remaining = extents.ToList();
        remaining.RemoveAt(from);
        double offset = remaining.Take(to).Sum();
        double viewport = axis == Axis.Vertical ? 600 : 800;
        double targetMain = reverse ? viewport - offset - extents[from] : offset;
        Point target = axis == Axis.Vertical ? new Point(0, targetMain) : new Point(targetMain, 0);
        Point initial = Position(tester, from);
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(from))), PointerDeviceKind.Mouse);
        Vector step = (target - initial) / 20;
        for (int tick = 0; tick < 20; tick++)
        {
            gesture.MoveBy(step);
            tester.Pump(TimeSpan.FromMilliseconds(50));
        }

        AssertClose(target, Position(tester, from));
        Point animationStart = Position(tester, from);
        gesture.Up();
        var state = tester.State<SliverReorderableListState>(Find.ByType<SliverReorderableList>());
        AssertClose(target, state.FinalDropPosition!.Value);
        tester.Pump();
        Assert.Empty(reorders);
        tester.Pump(TimeSpan.FromMilliseconds(125));
        Point expected = target + ((animationStart - target) * Curves.EaseOut.Transform(0.5));
        AssertClose(expected, Position(tester, from));
        tester.Pump(TimeSpan.FromMilliseconds(125));
        tester.PumpAndSettle();
        if (from != to)
        {
            Assert.Equal([(from, to)], reorders);
        }
        else
        {
            Assert.Empty(reorders);
        }

    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Listeners_EnabledAndDelayedFollowTheGestureClock(bool delayed, bool enabled)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        List<int> starts = [];
        List<int> ends = [];
        List<(int, int)> reorders = [];
        tester.PumpWidget(Wrap(new ReorderableList(
            (_, index) => Item(index, delayed: delayed, enabled: enabled), 5,
            onReorderStart: starts.Add, onReorderEnd: ends.Add,
            onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)))));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(499));
        if (delayed)
        {
            Assert.Empty(starts);
        }

        tester.Pump(TimeSpan.FromMilliseconds(1));
        gesture.MoveBy(new Vector(0, 60));
        tester.Pump();
        tester.Pump(AnimationDuration);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal(enabled ? [0] : Array.Empty<int>(), starts);
        Assert.Equal(enabled ? [2] : Array.Empty<int>(), ends);
        Assert.Equal(enabled ? [(0, 1)] : Array.Empty<(int, int)>(), reorders);
    }

    [Fact]
    public void GestureSettings_CompeteWithScrollableAtTheConfiguredSlop()
    {
        using var tester = new FrameworkDartTester();
        List<(int, int)> reorders = [];
        tester.PumpWidget(Wrap(new MediaQuery(
            new MediaQueryData(GestureSettings: new DeviceGestureSettings(8)),
            new CustomScrollView(slivers: [new SliverReorderableList(
                (_, index) => Item(index), 5,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)))]))));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0, 10));
        tester.Pump();
        gesture.MoveBy(new Vector(0, 40));
        tester.Pump();
        tester.Pump(AnimationDuration);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal([(0, 1)], reorders);
    }

    [Fact]
    public void SimultaneousFingers_ReplacePendingAndActiveDragWithoutStaleCallbacks()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        List<int> starts = [];
        tester.PumpWidget(Wrap(new ReorderableList(
            (_, index) => Item(index, delayed: true), 5,
            // Isolate pending long-press replacement from the scroll drag that wins the first
            // pointer's arena when its pending reorder recognizer is disposed.
            physics: new NeverScrollableScrollPhysics(),
            onReorderStart: starts.Add, onReorderItem: (_, _) => { })));
        var first = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Touch);
        var second = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(1))), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([1], starts);
        second.MoveBy(new Vector(0, 30));
        tester.Pump();
        first.Up();
        second.Up();
        tester.PumpAndSettle();
        var third = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        var fourth = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(2))), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        third.Up();
        fourth.Up();
        tester.PumpAndSettle();
        Assert.Equal([1, 0, 2], starts);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void Drag_RestrictsTheAxisAndRetainsRawPositionBeyondBoundary(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new Center(new SizedBox(width: 300, height: 300,
            child: new DragBoundary(new ReorderableList(
                (_, index) => Item(index, 100, axis), 3,
                scrollDirection: axis, onReorderItem: (_, _) => { }))))));
        Point initial = Position(tester, 0);
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        gesture.MoveBy(new Vector(500, 500));
        tester.Pump();
        Point clamped = axis == Axis.Vertical ? initial + new Vector(0, 200) : initial + new Vector(200, 0);
        AssertClose(clamped, Position(tester, 0));
        gesture.MoveBy(new Vector(-100, -100));
        tester.Pump();
        AssertClose(clamped, Position(tester, 0));
        gesture.MoveBy(new Vector(-300, -300));
        tester.Pump();
        AssertClose(axis == Axis.Vertical ? initial + new Vector(0, 100) : initial + new Vector(100, 0),
            Position(tester, 0));
        gesture.Cancel();
        tester.PumpAndSettle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAndRemoval_DisposeTheDragAndOverlayExactlyOnce(bool remove)
    {
        using var tester = new FrameworkDartTester();
        var listKey = new LabeledGlobalKey<ReorderableListState>("list");
        List<ObjectEvent> events = [];
        List<int> ends = [];
        List<(int, int)> reorders = [];
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        try
        {
            tester.PumpWidget(Wrap(new ReorderableList(
                (_, index) => Item(index), 5, key: listKey, onReorderEnd: ends.Add,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)))));
            var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
            gesture.MoveBy(new Vector(0, 60));
            tester.Pump();
            if (remove)
            {
                tester.PumpWidget(Wrap(new SizedBox()));
            }
            else
            {
                listKey.CurrentState!.CancelReorder();
                tester.Pump();
            }

            gesture.Up();
            tester.PumpAndSettle();
            Assert.Empty(ends);
            Assert.Empty(reorders);
            if (Constants.KDebugMode)
            {
                ObjectCreated created = Assert.Single(events.OfType<ObjectCreated>(), e => e.ClassName == "_DragInfo");
                Assert.Equal("package:flutter/widgets.dart", created.Library);
                Assert.Single(events.OfType<ObjectDisposed>(), e => ReferenceEquals(e.Object, created.Object));
            }
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }
    }

    [Fact]
    public void Proxy_PreservesOriginalConstraintsThemesAndRemovesNestedTopPadding()
    {
        using var tester = new FrameworkDartTester();
        List<BoxConstraints> constraints = [];
        tester.PumpWidget(Wrap(new MediaQuery(new MediaQueryData(Padding: new Thickness(0, 50, 0, 0)),
            new DefaultTextStyle(new TextStyle(FontSize: 29), new ReorderableList(
                (_, index) => new LayoutBuilder((context, value) =>
                {
                    if (index == 0)
                    {
                        constraints.Add(value);
                    }

                    return new ReorderableDragStartListener(new SizedBox(height: 100,
                        child: new ListView(children: [new Text($"nested {index}")])), index);
                }, key: new ValueKey<int>(index)), 5, onReorderItem: (_, _) => { })))));
        BoxConstraints before = constraints[0];
        var gesture = tester.StartGesture(tester.GetCenter(Find.Text("nested 0")), PointerDeviceKind.Mouse);
        gesture.MoveBy(new Vector(0, 20));
        tester.Pump();
        Assert.All(constraints, value => Assert.Equal(before, value));
        Element nested = tester.Element(Find.Text("nested 0"));
        Assert.Equal(0, MediaQuery.Of(nested).Padding.Top);
        Assert.Equal(29, DefaultTextStyle.Of(nested).Style.FontSize);
        gesture.Cancel();
        tester.PumpAndSettle();
    }

    [Fact]
    public void InterruptedGapAnimation_KeepsTheCurrentPosition()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new ReorderableList(
            (_, index) => Item(index), 5, onReorderItem: (_, _) => { })));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        gesture.MoveBy(new Vector(0, 60));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Point before = Position(tester, 1);
        gesture.MoveBy(new Vector(0, -60));
        tester.Pump();
        AssertClose(before, Position(tester, 1));
        gesture.Cancel();
        tester.PumpAndSettle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalPositionDrop_DoesNotJumpToTheNextItem(bool reverse)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new ReorderableList(
            (_, index) => Item(index), 5, reverse: reverse, onReorderItem: (_, _) => { })));
        Point original = Position(tester, 0);
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        gesture.MoveBy(new Vector(0, reverse ? -100 : 100));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(300));
        gesture.MoveBy(new Vector(0, reverse ? 90 : -90));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(300));
        gesture.Up();
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(125));
        Assert.InRange(Math.Abs(Position(tester, 0).Y - original.Y), 0, 10);
        tester.PumpAndSettle();
    }

    [Fact]
    public void CoreList_ForwardsTheFullScrollViewContract()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new ReorderableList((_, index) => Item(index), 5,
            onReorderItem: (_, _) => { }, anchor: 0.25,
            dragStartBehavior: DragStartBehavior.Down,
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag,
            restorationId: "core-reorder", clipBehavior: Clip.None)));
        CustomScrollView view = tester.Widget<CustomScrollView>(Find.ByType<CustomScrollView>());
        Assert.Equal(0.25, view.Anchor);
        Assert.Equal(DragStartBehavior.Down, view.DragStartBehavior);
        Assert.Equal(ScrollViewKeyboardDismissBehavior.OnDrag, view.KeyboardDismissBehavior);
        Assert.Equal("core-reorder", view.RestorationId);
        Assert.Equal(Clip.None, view.ClipBehavior);
        var core = tester.Widget<ReorderableList>(Find.ByType<ReorderableList>());
        Assert.Null(core.AutoScrollerVelocityScalar);
        Assert.Equal(50, tester.Widget<SliverReorderableList>(Find.ByType<SliverReorderableList>())
            .AutoScrollerVelocityScalar);
    }

    [Fact]
    public void EmptyAndZeroAreaWidgets_DoNotBuildItemsOrRequireListAncestors()
    {
        using var tester = new FrameworkDartTester();
        int builds = 0;
        tester.PumpWidget(Wrap(new ReorderableList((_, index) =>
        {
            builds++;
            return Item(index);
        }, 0, onReorderItem: (_, _) => { })));
        Assert.Equal(0, builds);
        tester.PumpWidget(Wrap(new Center(new SizedBox(width: 0, height: 0,
            child: new ReorderableList((_, index) => Item(index), 0, onReorderItem: (_, _) => { })))));
        tester.PumpWidget(new SizedBox(width: 0, height: 0,
            child: new ReorderableDragStartListener(new SizedBox(), 0)));
        tester.PumpWidget(new SizedBox(width: 0, height: 0,
            child: new ReorderableDelayedDragStartListener(new SizedBox(), 0)));
        Assert.Null(tester.TakeException());
    }

    private static void AssertClose(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 5);
        Assert.Equal(expected.Y, actual.Y, precision: 5);
    }

    [Fact]
    public void CustomProxyDecorator_TracksPickupAndIsRemovedAfterDrop()
    {
        using var tester = new FrameworkDartTester();
        var proxyKey = new ValueKey<string>("decorated-proxy");
        tester.PumpWidget(Wrap(new ReorderableList((_, index) => Item(index), 5,
            onReorderItem: (_, _) => { },
            proxyDecorator: (child, index, animation) => new AnimatedBuilder(animation,
                (_, _) => new Opacity(1 - animation.Value * 0.5, child, key: proxyKey)))));
        Assert.False(tester.Any(Find.ByKey(proxyKey)));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(1, tester.Widget<Opacity>(Find.ByKey(proxyKey)).Value);
        tester.Pump(TimeSpan.FromMilliseconds(125));
        Assert.InRange(tester.Widget<Opacity>(Find.ByKey(proxyKey)).Value, 0.51, 0.99);
        tester.Pump(TimeSpan.FromMilliseconds(125));
        Assert.Equal(0.5, tester.Widget<Opacity>(Find.ByKey(proxyKey)).Value);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.False(tester.Any(Find.ByKey(proxyKey)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SmallDrag_PreservesNeighbourPositionForEveryExtentStrategy(int extentStrategy)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new ReorderableList((_, index) => Item(index), 5,
            onReorderItem: (_, _) => { }, itemExtent: extentStrategy == 1 ? 100 : null,
            prototypeItem: extentStrategy == 2 ? new SizedBox(width: 100, height: 100) : null)));
        Point initial = Position(tester, 1);
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        gesture.MoveBy(new Vector(0, 20));
        tester.Pump();
        AssertClose(initial, Position(tester, 1));
        gesture.Up();
        tester.PumpAndSettle();
    }

    [Fact]
    public void FindChildIndexCallback_UnwrapsTheUserKeyOnMutation()
    {
        using var tester = new FrameworkDartTester();
        List<int> model = [0, 1, 2];
        List<Key> lookups = [];
        StateSetter? rebuild = null;
        tester.PumpWidget(Wrap(new StatefulBuilder((context, setState) =>
        {
            rebuild = setState;
            return new CustomScrollView(slivers: [new SliverReorderableList(
                (_, index) => Item(model[index]), model.Count,
                onReorderItem: (_, _) => { },
                findChildIndexCallback: key =>
                {
                    lookups.Add(key);
                    int index = model.IndexOf(((ValueKey<int>)key).Value);
                    return index < 0 ? null : index;
                })]);
        })));
        Assert.Empty(lookups);
        rebuild!(() => model.Reverse());
        tester.PumpAndSettle();
        Assert.NotEmpty(lookups);
        Assert.All(lookups, key => Assert.IsType<ValueKey<int>>(key));
        Assert.True(Position(tester, 2).Y < Position(tester, 0).Y);
    }

    [Fact]
    public void DraggedItemCountChange_RemovesProxyAndCancelsTheReorder()
    {
        using var tester = new FrameworkDartTester();
        int count = 5;
        StateSetter? rebuild = null;
        int reorders = 0;
        tester.PumpWidget(Wrap(new StatefulBuilder((context, setState) =>
        {
            rebuild = setState;
            return new ReorderableList((_, index) => Item(index), count, onReorderItem: (_, _) => reorders++);
        })));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        gesture.MoveBy(new Vector(0, 60));
        tester.Pump();
        rebuild!(() => count = 4);
        tester.Pump();
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal(0, reorders);
        AssertClose(new Point(0, 0), Position(tester, 0));
    }

    [Fact]
    public void NewlyBuiltItem_DoesNotAcquireAnExistingAnimatedGap()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(300, 200));
        tester.PumpWidget(Wrap(new ReorderableList(
            (_, index) => Item(index, 100, Axis.Horizontal), 10,
            scrollDirection: Axis.Horizontal, scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            onReorderItem: (_, _) => { })));
        var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        gesture.MoveBy(new Vector(10, 0));
        tester.Pump();
        AssertClose(new Point(100, 0), Position(tester, 1));
        AssertClose(new Point(200, 0), Position(tester, 2));
        if (tester.Any(Find.ByKey(ItemKey(3))))
        {
            Assert.True(Position(tester, 3).X >= 300);
        }

        gesture.Cancel();
        tester.PumpAndSettle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropAndPointerCancel_DispatchMatchingDragInfoMemoryEvents(bool cancel)
    {
        using var tester = new FrameworkDartTester();
        List<ObjectEvent> events = [];
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        try
        {
            tester.PumpWidget(Wrap(new ReorderableList((_, index) => Item(index), 5,
                onReorderItem: (_, _) => { })));
            var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
            gesture.MoveBy(new Vector(0, 60));
            tester.Pump();
            tester.Pump(AnimationDuration);
            if (cancel)
            {
                gesture.Cancel();
            }
            else
            {
                gesture.Up();
            }

            tester.PumpAndSettle();
            if (Constants.KDebugMode)
            {
                ObjectCreated created = Assert.Single(events.OfType<ObjectCreated>(), e => e.ClassName == "_DragInfo");
                Assert.Single(events.OfType<ObjectDisposed>(), e => ReferenceEquals(e.Object, created.Object));
            }
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }
    }

    // Flutter: SliverReorderableList auto scrolls speed is configurable.
    [Fact]
    public void EdgeAutoScroll_TouchingEdgeDoesNotScrollAndVelocityScalarSetsTheSpeed()
    {
        double Run(double velocityScalar)
        {
            using var tester = new FrameworkDartTester();
            using var controller = new ScrollController();
            tester.PumpWidget(Wrap(new CustomScrollView(controller: controller,
                slivers: [new SliverReorderableList((_, index) => Item(index), 10,
                    autoScrollerVelocityScalar: velocityScalar, onReorderItem: (_, _) => { })])));
            var gesture = tester.StartGesture(tester.GetCenter(Find.ByKey(ItemKey(0))), PointerDeviceKind.Mouse);
            gesture.MoveBy(new Vector(0, 500));
            tester.Pump();
            for (int tick = 0; tick < 4; tick++)
            {
                tester.Pump(TimeSpan.FromMilliseconds(51));
            }

            Assert.Equal(0, controller.Offset);
            gesture.MoveBy(new Vector(0, 50));
            tester.Pump();
            double interval = Math.Round(1000 / velocityScalar);
            int ticks = (int)Math.Ceiling(600 / interval);
            for (int tick = 0; tick < ticks; tick++)
            {
                tester.Pump(TimeSpan.FromMilliseconds(interval + 1));
            }

            double offset = controller.Offset;
            Assert.True(offset > 0);
            gesture.Cancel();
            tester.PumpAndSettle();
            return offset;
        }

        Assert.Equal(4, Run(20) / Run(5), precision: 5);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrimaryControllerAndCache_AreResolvedByTheScrollView(bool modernCache)
    {
        using var tester = new FrameworkDartTester();
        using var controller = new ScrollController();
        tester.PumpWidget(Wrap(new PrimaryScrollController(controller,
            new ReorderableList((_, index) => Item(index), 10,
                onReorderItem: (_, _) => { }, primary: true, cacheExtent: 123,
                scrollCacheExtent: modernCache ? ScrollCacheExtent.Viewport(1.5) : null))));
        Assert.True(controller.HasClients);
        CustomScrollView view = tester.Widget<CustomScrollView>(Find.ByType<CustomScrollView>());
        Assert.Null(view.Controller);
#pragma warning disable CS0618
        Assert.Null(view.CacheExtent);
#pragma warning restore CS0618
        Assert.Equal(modernCache ? 1.5 : 123, view.ScrollCacheExtent!.Value);
        Assert.Equal(modernCache ? CacheExtentStyle.Viewport : CacheExtentStyle.Pixel, view.ScrollCacheExtent.Style);
    }

    // Flutter: Do not crash when dragging with two fingers simultaneously.
    [Fact]
    public void SimultaneousImmediateFingers_CompeteWithScrollingWithoutCrashing()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new ReorderableList((_, index) => Item(index), 3,
            onReorderItem: (_, _) => { })));
        Point start = tester.GetCenter(Find.ByKey(ItemKey(0)));
        var first = tester.StartGesture(start, PointerDeviceKind.Touch);
        var second = tester.StartGesture(start, PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        first.MoveBy(new Vector(0, 100));
        second.MoveBy(new Vector(0, 100));
        tester.PumpAndSettle();
        first.Up();
        second.Up();
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
    }
}
