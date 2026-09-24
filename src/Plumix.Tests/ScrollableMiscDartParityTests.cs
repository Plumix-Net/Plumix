using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/scrollable_fling_test.dart, scrollable_of_test.dart,
// scrollable_restoration_test.dart, scrollable_dispose_test.dart, scrollable_animations_test.dart
// and the Scrollable parts of scrollable_helpers_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollableMiscDartParityTests
{
    private const double DragOffset = 213.82;

    private static readonly Color TestFontColor = Color.FromUInt32(0xFF00FF00);

    // ---------------------------------------------------------------- scrollable_fling_test.dart

    // scrollable_fling_test.dart: pumpTest.
    private static void PumpFlingTest(FrameworkDartTester tester, TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        tester.PumpWidget(new Container());
        tester.PumpWidget(new TestWidgetsApp(
            home: new ColoredBox(
                color: Color.FromUInt32(0xFF111111),
                child: ListView.Builder(
                    dragStartBehavior: DragStartBehavior.Down,
                    itemBuilder: (_, index) => new Text($"{index}", color: TestFontColor)))));
    }

    private static double CurrentOffset(FrameworkDartTester tester) =>
        tester.State<ScrollableState>().Position.Pixels;

    // Flutter: 'scrollable_fling_test.dart: Flings on different platforms'
    [Fact]
    public void FlingsOnDifferentPlatforms()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        try
        {
            PumpFlingTest(tester, TargetPlatform.Android);
            tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -DragOffset), 1000.0);
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(); // trigger fling
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(TimeSpan.FromSeconds(5));
            double androidResult = CurrentOffset(tester);
            // Regression test for https://github.com/flutter/flutter/issues/83632.
            Assert.True(androidResult > 408.0, $"{androidResult}");
            Assert.True(androidResult < 409.0, $"{androidResult}");

            PumpFlingTest(tester, TargetPlatform.Linux);
            tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -DragOffset), 1000.0);
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(); // trigger fling
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(TimeSpan.FromSeconds(5));
            double linuxResult = CurrentOffset(tester);

            PumpFlingTest(tester, TargetPlatform.Windows);
            tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -DragOffset), 1000.0);
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(); // trigger fling
            Assert.Equal(DragOffset, CurrentOffset(tester));
            tester.Pump(TimeSpan.FromSeconds(5));
            double windowsResult = CurrentOffset(tester);

            PumpFlingTest(tester, TargetPlatform.IOS);
            tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -DragOffset), 1000.0);
            // Scroll starts ease into the scroll on iOS.
            AssertMoreOrLessEquals(210.71026666666666, CurrentOffset(tester));
            tester.Pump(); // trigger fling
            AssertMoreOrLessEquals(210.71026666666666, CurrentOffset(tester));
            tester.Pump(TimeSpan.FromSeconds(5));
            double iOSResult = CurrentOffset(tester);

            PumpFlingTest(tester, TargetPlatform.MacOS);
            tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -DragOffset), 1000.0);
            // Scroll starts ease into the scroll on iOS.
            AssertMoreOrLessEquals(210.71026666666666, CurrentOffset(tester));
            tester.Pump(); // trigger fling
            AssertMoreOrLessEquals(210.71026666666666, CurrentOffset(tester));
            tester.Pump(TimeSpan.FromSeconds(5));
            double macOSResult = CurrentOffset(tester);

            Assert.True(androidResult < iOSResult); // iOS is slipperier than Android
            Assert.True(macOSResult < iOSResult); // iOS is slipperier than macOS
            Assert.True(macOSResult < androidResult); // Android is slipperier than macOS
            Assert.True(linuxResult < iOSResult); // iOS is slipperier than Linux
            Assert.True(macOSResult < linuxResult); // Linux is slipperier than macOS
            Assert.True(windowsResult < iOSResult); // iOS is slipperier than Windows
            Assert.True(macOSResult < windowsResult); // Windows is slipperier than macOS
            Assert.Equal(androidResult, windowsResult);
            Assert.Equal(androidResult, windowsResult);
            Assert.Equal(androidResult, linuxResult);
            Assert.Equal(androidResult, linuxResult);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    private static Widget BuildTapLogList(List<string> log)
    {
        return new Directionality(
            TextDirection.Ltr,
            new ListView(
                dragStartBehavior: DragStartBehavior.Down,
                children: Enumerable.Range(0, 250)
                    .Select(i => (Widget)new GestureDetector(
                        onTap: () => log.Add($"tap {i}"),
                        child: new Text($"{i}", color: TestFontColor)))
                    .ToList()));
    }

    // Flutter: 'scrollable_fling_test.dart: fling and tap to stop'
    [Fact]
    public void FlingAndTapToStop()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var log = new List<string>();
        tester.PumpWidget(BuildTapLogList(log));

        Assert.Equal(new List<string>(), log);
        tester.Tap(tester.ElementOfType<Scrollable>());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -200.0), 1000.0);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Tap(tester.ElementOfType<Scrollable>()); // should stop the fling but not tap anything
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Tap(tester.ElementOfType<Scrollable>());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21", "tap 35" }, log);
    }

    // Flutter: 'scrollable_fling_test.dart: fling and wait and tap'
    [Fact]
    public void FlingAndWaitAndTap()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var log = new List<string>();
        tester.PumpWidget(BuildTapLogList(log));

        Assert.Equal(new List<string>(), log);
        tester.Tap(tester.ElementOfType<Scrollable>());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -200.0), 1000.0);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Pump(TimeSpan.FromSeconds(50)); // long wait, so the fling will have ended at the end of it
        Assert.Equal(new List<string> { "tap 21" }, log);
        tester.Tap(tester.ElementOfType<Scrollable>());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "tap 21", "tap 49" }, log);
    }

    // ---------------------------------------------------------------- scrollable_of_test.dart

    // Flutter: 'scrollable_of_test.dart: Scrollable.of() dependent rebuilds when Scrollable position changes'
    [Fact]
    public void ScrollableOfDependentRebuildsWhenScrollablePositionChanges()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        string logValue = string.Empty;
        using var controller = new ScrollController();

        // Changing the SingleChildScrollView's physics causes the
        // ScrollController's ScrollPosition to be rebuilt.

        Widget BuildFrame(ScrollPhysics? physics)
        {
            return new SingleChildScrollView(
                controller: controller,
                physics: physics,
                child: new ScrollPositionListener(
                    log: s => logValue = s,
                    child: new SizedBox(height: 400.0)));
        }

        tester.PumpWidget(BuildFrame(null));
        Assert.Equal("didChangeDependencies 0.0", logValue);

        controller.JumpTo(100.0);
        Assert.Equal("listener 100.0", logValue);

        tester.PumpWidget(BuildFrame(new ClampingScrollPhysics()));
        Assert.Equal("didChangeDependencies 100.0", logValue);

        controller.JumpTo(200.0);
        Assert.Equal("listener 200.0", logValue);

        controller.JumpTo(300.0);
        Assert.Equal("listener 300.0", logValue);

        tester.PumpWidget(BuildFrame(new BouncingScrollPhysics()));
        Assert.Equal("didChangeDependencies 300.0", logValue);

        controller.JumpTo(400.0);
        Assert.Equal("listener 400.0", logValue);
    }

    // Flutter: 'scrollable_of_test.dart: Scrollable.of() is possible using ScrollNotification context'
    [Fact]
    public void ScrollableOfIsPossibleUsingScrollNotificationContext()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        ScrollNotification? notification = null;

        tester.PumpWidget(new NotificationListener<ScrollNotification>(
            onNotification: value =>
            {
                notification = value;
                return false;
            },
            child: new SingleChildScrollView(child: new SizedBox(height: 1200.0))));

        TestGesture gesture = tester.StartGesture(new Point(100.0, 100.0), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromSeconds(1));

        var scrollableElement = (StatefulElement)tester.ElementsOfType<Scrollable>().First();
        Assert.NotNull(notification);
        Assert.Same(scrollableElement.State, Scrollable.Of(notification!.Context!));

        // Finish gesture to release resources.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: 'scrollable_of_test.dart: Static Scrollable methods can target a specific axis'
    [Fact]
    public void StaticScrollableMethodsCanTargetASpecificAxis()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var horizontalController = new TestScrollController(deferLoading: true);
        using var verticalController = new TestScrollController(deferLoading: false);
        AxisDirection? foundAxisDirection = null;
        bool? foundRecommendation = null;

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(
                scrollDirection: Axis.Horizontal,
                controller: horizontalController,
                child: new SingleChildScrollView(
                    controller: verticalController,
                    child: new Builder(context =>
                    {
                        // Dart's `late final` locals: assigned exactly once.
                        Assert.Null(foundAxisDirection);
                        foundAxisDirection = Scrollable.Of(context, axis: Axis.Horizontal).AxisDirection;
                        foundRecommendation = Scrollable.RecommendDeferredLoadingForContext(
                            context,
                            axis: Axis.Horizontal);
                        return new SizedBox(height: 1200.0, width: 1200.0);
                    })))));
        tester.PumpAndSettle();

        Assert.Equal(AxisDirection.Right, foundAxisDirection);
        Assert.True(foundRecommendation);
    }

    // Flutter: 'scrollable_of_test.dart: Axis targeting scrollables establishes the correct dependencies'
    [Fact]
    public void AxisTargetingScrollablesEstablishesTheCorrectDependencies()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var verticalKey = new LabeledGlobalKey<TestScrollableState>(null);
        var childKey = new LabeledGlobalKey<TestChildState>(null);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(
                scrollDirection: Axis.Horizontal,
                child: new TestScrollable(
                    key: verticalKey,
                    child: new TestChild(key: childKey)))));
        tester.PumpAndSettle();

        Assert.Equal(1, verticalKey.CurrentState!.DependenciesChanged);
        Assert.Equal(1, childKey.CurrentState!.DependenciesChanged);

        using var controller = new ScrollController();

        // Change the horizontal ScrollView, adding a controller
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(
                scrollDirection: Axis.Horizontal,
                controller: controller,
                child: new TestScrollable(
                    key: verticalKey,
                    child: new TestChild(key: childKey)))));
        tester.PumpAndSettle();
        Assert.Equal(1, verticalKey.CurrentState!.DependenciesChanged);
        Assert.Equal(2, childKey.CurrentState!.DependenciesChanged);
    }

    // ---------------------------------------------------------------- scrollable_dispose_test.dart

    // Flutter: 'scrollable_dispose_test.dart: simultaneously dispose a widget and end the scroll animation'
    [Fact]
    public void SimultaneouslyDisposeAWidgetAndEndTheScrollAnimation()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new FrameworkDartFlipWidget(
                left: new ListView(
                    children: Enumerable.Range(0, 250).Select(i => (Widget)new Text($"{i}")).ToList()),
                right: new Container())));

        tester.Fling(tester.ElementOfType<ListView>(), new Vector(0.0, -200.0), 1000.0);
        tester.Pump();

        tester.State<FrameworkDartFlipWidgetState>().Flip();
        tester.Pump(TimeSpan.FromHours(5));
    }

    // Flutter: 'scrollable_dispose_test.dart: Disposing a (nested) Scrollable while holding in overscroll does not
    // crash'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void DisposingANestedScrollableWhileHoldingInOverscrollDoesNotCrash(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            // Regression test for https://github.com/flutter/flutter/issues/27707.
            using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
            using var controller = new ScrollController();
            Key outerContainer = new LabeledGlobalKey<State>(null);

            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new Container(
                        key: outerContainer,
                        color: Color.FromUInt32(0xFF0000FF),
                        width: 400.0,
                        child: new SingleChildScrollView(
                            scrollDirection: Axis.Horizontal,
                            child: new SizedBox(
                                width: 500.0,
                                child: ListView.Builder(
                                    controller: controller,
                                    itemBuilder: (_, index) => new Container(
                                        color: index % 2 == 0
                                            ? Color.FromUInt32(0xFFFF0000)
                                            : Color.FromUInt32(0xFF00FF00),
                                        height: 200.0,
                                        child: new Text($"Hello {index}")))))))));

            // Go into overscroll.
            double lastScrollOffset;
            tester.Fling(FindText(tester, "Hello 0").Single(), new Vector(0.0, 1000.0), 1000.0);
            tester.Pump(TimeSpan.FromMilliseconds(100));
            lastScrollOffset = controller.Offset;
            Assert.True(lastScrollOffset < 0.0, $"{lastScrollOffset}");

            // Reduce the overscroll a little, but don't let it go back to 0.0.
            tester.Pump(TimeSpan.FromMilliseconds(100));
            Assert.True(controller.Offset > lastScrollOffset, $"{controller.Offset}");
            Assert.True(controller.Offset < 0.0, $"{controller.Offset}");
            double currentOffset = controller.Offset;

            // Start a hold activity by putting one pointer down.
            TestGesture gesture = tester.StartGesture(
                tester.GetTopLeft(tester.ElementsWithKey(outerContainer).Single()) + new Vector(50.0, 50.0),
                PointerDeviceKind.Touch);
            tester.PumpAndSettle(); // This shouldn't change the scroll offset because of the down event above.
            Assert.Equal(currentOffset, controller.Offset);

            // Dispose the scrollables while the finger is still down, this should not crash.
            tester.PumpWidget(new SizedBox());
            tester.PumpAndSettle();
            Assert.False(controller.HasClients);

            // Finish gesture to release resources.
            gesture.Up();
            tester.PumpAndSettle();
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    // ---------------------------------------------------------------- scrollable_animations_test.dart

    private static Widget BuildEightyItemList(ScrollController controller)
    {
        return new Directionality(
            TextDirection.Ltr,
            new ListView(
                controller: controller,
                children: Enumerable.Range(0, 80)
                    .Select(i => (Widget)new Text($"{i}", textDirection: TextDirection.Ltr))
                    .ToList()));
    }

    private static void ExpectNoAnimation()
    {
        Assert.True(Scheduler.TransientCallbackCount == 0, "Expected no animation.");
    }

    // Flutter: 'scrollable_animations_test.dart: Does not animate if already at target position'
    [Fact]
    public void DoesNotAnimateIfAlreadyAtTargetPosition()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(BuildEightyItemList(controller));

        ExpectNoAnimation();
        double currentPosition = controller.Position.Pixels;
        _ = controller.Position.AnimateTo(currentPosition, TimeSpan.FromSeconds(10), Curves.Linear);

        ExpectNoAnimation();
        Assert.Equal(currentPosition, controller.Position.Pixels);
    }

    // Flutter: 'scrollable_animations_test.dart: Does not animate if already at target position within tolerance'
    [Fact]
    public void DoesNotAnimateIfAlreadyAtTargetPositionWithinTolerance()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(BuildEightyItemList(controller));

        ExpectNoAnimation();

        double halfTolerance = controller.Position.Physics.ToleranceFor(controller.Position).Distance / 2;
        Assert.NotEqual(0.0, halfTolerance);
        double targetPosition = controller.Position.Pixels + halfTolerance;
        _ = controller.Position.AnimateTo(targetPosition, TimeSpan.FromSeconds(10), Curves.Linear);

        ExpectNoAnimation();
        Assert.Equal(targetPosition, controller.Position.Pixels);
    }

    // Flutter: 'scrollable_animations_test.dart: Animates if going to a position outside of tolerance'
    [Fact]
    public void AnimatesIfGoingToAPositionOutsideOfTolerance()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(BuildEightyItemList(controller));

        ExpectNoAnimation();

        double doubleTolerance = controller.Position.Physics.ToleranceFor(controller.Position).Distance * 2;
        Assert.NotEqual(0.0, doubleTolerance);
        double targetPosition = controller.Position.Pixels + doubleTolerance;
        _ = controller.Position.AnimateTo(targetPosition, TimeSpan.FromSeconds(10), Curves.Linear);

        Assert.True(Scheduler.TransientCallbackCount == 1, "Expected an animation.");
    }

    // Flutter: 'scrollable_animations_test.dart: HoldActivity can interrupt ScrollPosition.animateTo'
    [Fact]
    public void HoldActivityCanInterruptScrollPositionAnimateTo()
    {
        const double animationExtent = 100.0;
        const double dragExtent = 30.0;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new NotificationListener<ScrollNotification>(
                onNotification: notification =>
                {
                    _ = controller.Position.AnimateTo(animationExtent, TimeSpan.FromSeconds(1), Curves.Linear);
                    return true;
                },
                child: new ListView(
                    controller: controller,
                    dragStartBehavior: DragStartBehavior.Down,
                    children: Enumerable.Range(0, 80)
                        .Select(i => (Widget)new Text($"{i}", textDirection: TextDirection.Ltr))
                        .ToList()))));

        ExpectNoAnimation();

        // Drag to initiate the scroll animation.
        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, 1.0));
        tester.Pump();

        // Pump to halfway through the animation.
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(animationExtent / 2, controller.Position.Pixels);

        // Interrupt the scroll animation.
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, dragExtent));
        gesture.Up();

        tester.Pump(TimeSpan.FromMilliseconds(500));

        // The drag stops the animation, and the drag extent is respected.
        Assert.Equal((animationExtent / 2) - dragExtent, controller.Position.Pixels);
    }

    // ---------------------------------------------------------------- scrollable_restoration_test.dart

    // Flutter: 'scrollable_restoration_test.dart: CustomScrollView restoration'
    [Fact]
    public void CustomScrollViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new CustomScrollView(
                restorationId: "list",
                cacheExtent: 0,
                slivers:
                [
                    SliverList.Builder(
                        itemCount: 50,
                        itemBuilder: (_, index) => new SizedBox(height: 50, child: new Text($"Tile {index}"))),
                ]));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListView restoration'
    [Fact]
    public void ListViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new ListView(
                restorationId: "list",
                cacheExtent: 0,
                children: FiftyTiles()));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListView.builder restoration'
    [Fact]
    public void ListViewBuilderRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: ListView.Builder(
                restorationId: "list",
                cacheExtent: 0,
                itemBuilder: (_, index) => new SizedBox(height: 50, child: new Text($"Tile {index}"))));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListView.separated restoration'
    [Fact]
    public void ListViewSeparatedRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: ListView.Separated(
                restorationId: "list",
                cacheExtent: 0,
                itemCount: 50,
                separatorBuilder: (_, _) => SizedBox.Shrink(),
                itemBuilder: (_, index) => new SizedBox(height: 50, child: new Text($"Tile {index}"))));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListView.custom restoration'
    [Fact]
    public void ListViewCustomRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: ListView.Custom(
                restorationId: "list",
                cacheExtent: 0,
                childrenDelegate: new SliverChildListDelegate(FiftyTiles())));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: GridView restoration'
    [Fact]
    public void GridViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new GridView(
                restorationId: "grid",
                cacheExtent: 0,
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 1),
                children: FiftyTiles()));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: GridView.builder restoration'
    [Fact]
    public void GridViewBuilderRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: GridView.Builder(
                restorationId: "grid",
                cacheExtent: 0,
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 1),
                itemBuilder: (_, index) => new SizedBox(height: 50, child: new Text($"Tile {index}"))));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: GridView.custom restoration'
    [Fact]
    public void GridViewCustomRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: GridView.Custom(
                restorationId: "grid",
                cacheExtent: 0,
                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 1),
                childrenDelegate: new SliverChildListDelegate(FiftyTiles())));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: GridView.count restoration'
    [Fact]
    public void GridViewCountRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: GridView.Count(
                restorationId: "grid",
                cacheExtent: 0,
                crossAxisCount: 1,
                children: FiftyTiles()));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: GridView.extent restoration'
    [Fact]
    public void GridViewExtentRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: GridView.Extent(
                restorationId: "grid",
                cacheExtent: 0,
                maxCrossAxisExtent: 50,
                children: FiftyTiles()));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: SingleChildScrollView restoration'
    [Fact]
    public void SingleChildScrollViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new SingleChildScrollView(
                restorationId: "single",
                child: new Column(children: FiftyTiles())));
        tester.PumpWidget(widget);

        Assert.Equal(new Point(0, 0), tester.GetTopLeft(TextElement(tester, "Tile 0")));
        Assert.Equal(new Point(0, 50), tester.GetTopLeft(TextElement(tester, "Tile 1")));

        tester.State<ScrollableState>().Position.JumpTo(525);
        tester.Pump();

        Assert.Equal(new Point(0, -525), tester.GetTopLeft(TextElement(tester, "Tile 0")));
        Assert.Equal(new Point(0, -475), tester.GetTopLeft(TextElement(tester, "Tile 1")));

        restoration.RestartAndRestore(tester, widget);

        Assert.Equal(525, tester.State<ScrollableState>().Position.Pixels);
        Assert.Equal(new Point(0, -525), tester.GetTopLeft(TextElement(tester, "Tile 0")));
        Assert.Equal(new Point(0, -475), tester.GetTopLeft(TextElement(tester, "Tile 1")));

        TestRestorationData data = restoration.GetRestorationData();
        tester.State<ScrollableState>().Position.JumpTo(0);
        tester.Pump();

        Assert.Equal(new Point(0, 0), tester.GetTopLeft(TextElement(tester, "Tile 0")));
        Assert.Equal(new Point(0, 50), tester.GetTopLeft(TextElement(tester, "Tile 1")));

        restoration.RestoreFrom(tester, data);

        Assert.Equal(525, tester.State<ScrollableState>().Position.Pixels);
        Assert.Equal(new Point(0, -525), tester.GetTopLeft(TextElement(tester, "Tile 0")));
        Assert.Equal(new Point(0, -475), tester.GetTopLeft(TextElement(tester, "Tile 1")));
    }

    // Flutter: 'scrollable_restoration_test.dart: PageView restoration'
    [Fact]
    public void PageViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new PageView(
                restorationId: "pager",
                children: Enumerable.Range(0, 50).Select(index => (Widget)new Text($"Tile {index}")).ToList()));
        tester.PumpWidget(widget);

        PageViewScrollAndRestore(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: PageView.builder restoration'
    [Fact]
    public void PageViewBuilderRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: PageView.Builder(
                restorationId: "pager",
                itemBuilder: (_, index) => new SizedBox(height: 50, child: new Text($"Tile {index}"))));
        tester.PumpWidget(widget);

        PageViewScrollAndRestore(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: PageView.custom restoration'
    [Fact]
    public void PageViewCustomRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new PageView(
                restorationId: "pager",
                childrenDelegate: new SliverChildListDelegate(FiftyTiles())));
        tester.PumpWidget(widget);

        PageViewScrollAndRestore(tester, restoration, widget);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListWheelScrollView restoration'
    [Fact]
    public void ListWheelScrollViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new ListWheelScrollView(
                restorationId: "wheel",
                itemExtent: 50,
                children: Enumerable.Range(0, 50).Select(index => (Widget)new Text($"Tile {index}")).ToList()));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget, secondOffset: 542);
    }

    // Flutter: 'scrollable_restoration_test.dart: ListWheelScrollView.useDelegate restoration'
    [Fact]
    public void ListWheelScrollViewUseDelegateRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            child: new ListWheelScrollView(
                restorationId: "wheel",
                itemExtent: 50,
                childDelegate: new ListWheelChildListDelegate(children: FiftyTiles())));
        tester.PumpWidget(widget);

        RestoreScrollAndVerify(tester, restoration, widget, secondOffset: 542);
    }

    // Flutter: 'scrollable_restoration_test.dart: NestedScrollView restoration'
    [Fact]
    public void NestedScrollViewRestoration()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestHarness(
            height: 200,
            child: new NestedScrollView(
                restorationId: "outer",
                headerSliverBuilder: (context, _) =>
                [
                    new SliverOverlapAbsorber(
                        handle: NestedScrollView.SliverOverlapAbsorberHandleFor(context),
                        sliver: new SliverPersistentHeader(
                            pinned: true,
                            @delegate: new TestHeaderDelegate(minExtent: 56.0, maxExtent: 150.0))),
                ],
                body: new ListView(
                    restorationId: "inner",
                    cacheExtent: 0,
                    children: FiftyTiles())));
        tester.PumpWidget(widget);

        double HeaderPaintExtent() =>
            ((RenderSliver)tester.ElementOfType<SliverPersistentHeader>().FindRenderObject()!)
            .Geometry!.PaintExtent;

        Assert.Equal(150, HeaderPaintExtent());
        Assert.Single(FindText(tester, "Tile 0"));
        Assert.Empty(FindText(tester, "Tile 10"));

        tester.Drag(tester.ElementOfType<NestedScrollView>(), new Vector(0, -500));
        tester.Pump();

        Assert.Equal(56, HeaderPaintExtent());
        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));

        restoration.RestartAndRestore(tester, widget);

        Assert.Equal(56, HeaderPaintExtent());
        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));

        TestRestorationData data = restoration.GetRestorationData();
        tester.Drag(tester.ElementOfType<NestedScrollView>(), new Vector(0, 600));
        tester.Pump();

        Assert.Equal(150, HeaderPaintExtent());
        Assert.Single(FindText(tester, "Tile 0"));
        Assert.Empty(FindText(tester, "Tile 10"));

        restoration.RestoreFrom(tester, data);

        Assert.Equal(56, HeaderPaintExtent());
        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));
    }

    // Flutter: 'scrollable_restoration_test.dart: RestorationData is flushed even if no frame is scheduled'
    [Fact]
    public void RestorationDataIsFlushedEvenIfNoFrameIsScheduled()
    {
        using var restoration = new TestRestorationBinding();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new TestHarness(
            child: new ListView(
                restorationId: "list",
                cacheExtent: 0,
                children: FiftyTiles())));

        Assert.Single(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 1"));
        Assert.Empty(FindText(tester, "Tile 10"));
        Assert.Empty(FindText(tester, "Tile 11"));
        Assert.Empty(FindText(tester, "Tile 12"));

        TestRestorationData initialData = restoration.GetRestorationData();
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<ListView>()),
            PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0, -525));
        tester.Pump();

        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Empty(FindText(tester, "Tile 1"));
        Assert.Single(FindText(tester, "Tile 10"));
        Assert.Single(FindText(tester, "Tile 11"));
        Assert.Single(FindText(tester, "Tile 12"));

        // Restoration data hasn't changed.
        Assert.Same(initialData, restoration.GetRestorationData());

        // Restoration data changes with up event.
        gesture.Up();
        tester.Pump();
        Assert.NotSame(initialData, restoration.GetRestorationData());
    }

    private static List<Widget> FiftyTiles() =>
        Enumerable.Range(0, 50)
            .Select(index => (Widget)new SizedBox(height: 50, child: new Text($"Tile {index}")))
            .ToList();

    private static Element TextElement(FrameworkDartTester tester, string text) =>
        FindText(tester, text).Single();

    // scrollable_restoration_test.dart: pageViewScrollAndRestore.
    private static void PageViewScrollAndRestore(
        FrameworkDartTester tester,
        TestRestorationBinding restoration,
        Widget widget)
    {
        Assert.Single(FindText(tester, "Tile 0"));
        Assert.Empty(FindText(tester, "Tile 10"));

        tester.State<ScrollableState>().Position.JumpTo(50.0 * 10);
        tester.PumpAndSettle();

        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));

        restoration.RestartAndRestore(tester, widget);

        Assert.Equal(50.0 * 10, tester.State<ScrollableState>().Position.Pixels);
        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));

        TestRestorationData data = restoration.GetRestorationData();
        tester.State<ScrollableState>().Position.JumpTo(0);
        tester.Pump();

        Assert.Single(FindText(tester, "Tile 0"));
        Assert.Empty(FindText(tester, "Tile 10"));

        restoration.RestoreFrom(tester, data);

        Assert.Equal(50.0 * 10, tester.State<ScrollableState>().Position.Pixels);
        Assert.Empty(FindText(tester, "Tile 0"));
        Assert.Single(FindText(tester, "Tile 10"));
    }

    // scrollable_restoration_test.dart: restoreScrollAndVerify.
    private static void RestoreScrollAndVerify(
        FrameworkDartTester tester,
        TestRestorationBinding restoration,
        Widget widget,
        double secondOffset = 525)
    {
        // Dart's `find.byElementPredicate((Element e) => e.widget is Scrollable)`.
        ScrollableState FindScrollable() => tester.AllElements()
            .Where(element => element.Widget is Scrollable)
            .Cast<StatefulElement>()
            .Select(element => (ScrollableState)element.State)
            .Single();

        void ExpectTiles(bool top)
        {
            Assert.Equal(top ? 1 : 0, FindText(tester, "Tile 0").Count);
            Assert.Equal(top ? 1 : 0, FindText(tester, "Tile 1").Count);
            Assert.Equal(top ? 0 : 1, FindText(tester, "Tile 10").Count);
            Assert.Equal(top ? 0 : 1, FindText(tester, "Tile 11").Count);
            Assert.Equal(top ? 0 : 1, FindText(tester, "Tile 12").Count);
        }

        ExpectTiles(top: true);

        FindScrollable().Position.JumpTo(secondOffset);
        tester.Pump();

        ExpectTiles(top: false);

        restoration.RestartAndRestore(tester, widget);

        Assert.Equal(secondOffset, FindScrollable().Position.Pixels);
        ExpectTiles(top: false);

        TestRestorationData data = restoration.GetRestorationData();
        FindScrollable().Position.JumpTo(0);
        tester.Pump();

        ExpectTiles(top: true);

        restoration.RestoreFrom(tester, data);

        Assert.Equal(secondOffset, FindScrollable().Position.Pixels);
        ExpectTiles(top: false);
    }

    // ---------------------------------------------------------------- scrollable_helpers_test.dart

    // scrollable_helpers_test.dart: modifierKey, evaluated with the default (Android) target platform.
    private static readonly LogicalKeyboardKey ModifierKey = PlatformDefaults.TargetPlatform == TargetPlatform.MacOS
        ? LogicalKeyboardKey.MetaLeft
        : LogicalKeyboardKey.ControlLeft;

    private static List<Widget> TwentyFocusableBoxes(Axis axis, Func<int, Widget, Widget> wrap)
    {
        return Enumerable.Range(0, 20)
            .Select(index => (Widget)new SliverToBoxAdapter(
                child: wrap(
                    index,
                    axis == Axis.Vertical
                        ? new SizedBox(key: new ValueKey<string>($"Box {index}"), height: 50.0)
                        : new SizedBox(key: new ValueKey<string>($"Box {index}"), width: 50.0))))
            .ToList();
    }

    private static Rect BoxRect(FrameworkDartTester tester, string key) =>
        tester.GetRect(tester.ElementsWithKey(new ValueKey<string>(key)).Single());

    private static Rect Ltrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    // Flutter: "scrollable_helpers_test.dart: Keyboard scrolling doesn't happen if scroll physics are set to
    // NeverScrollableScrollPhysics"
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void KeyboardScrollingDoesNotHappenIfScrollPhysicsAreSetToNeverScrollableScrollPhysics(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                physics: new NeverScrollableScrollPhysics(),
                slivers: TwentyFocusableBoxes(
                    Axis.Vertical,
                    (index, box) => new Focus(autofocus: index == 0, child: box)))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageDown);
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageUp);
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Vertical scrollables are scrolled when activated via keyboard.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void VerticalScrollablesAreScrolledWhenActivatedViaKeyboard(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                controller: controller,
                slivers: TwentyFocusableBoxes(
                    Axis.Vertical,
                    (index, box) => new Focus(autofocus: index == 0, child: box)))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, -50.0, 800.0, 0.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageDown);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, -400.0, 800.0, -350.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageUp);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Horizontal scrollables are scrolled when activated via keyboard.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void HorizontalScrollablesAreScrolledWhenActivatedViaKeyboard(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                controller: controller,
                scrollDirection: Axis.Horizontal,
                slivers: TwentyFocusableBoxes(
                    Axis.Horizontal,
                    (index, box) => new Focus(autofocus: index == 0, child: box)))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 50.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(-50.0, 0.0, 0.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 0.0, 50.0, 600.0), BoxRect(tester, "Box 0"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Horizontal scrollables are scrolled the correct direction in RTL
    // locales.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void HorizontalScrollablesAreScrolledTheCorrectDirectionInRtlLocales(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Directionality(
                TextDirection.Rtl,
                new CustomScrollView(
                    controller: controller,
                    scrollDirection: Axis.Horizontal,
                    slivers: TwentyFocusableBoxes(
                        Axis.Horizontal,
                        (index, box) => new Focus(autofocus: index == 0, child: box))))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(750.0, 0.0, 800.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(800.0, 0.0, 850.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(750.0, 0.0, 800.0, 600.0), BoxRect(tester, "Box 0"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Reversed vertical scrollables are scrolled when activated via keyboard.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void ReversedVerticalScrollablesAreScrolledWhenActivatedViaKeyboard(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        using var focusNode = new FocusNode(debugLabel: "SizedBox");
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                controller: controller,
                reverse: true,
                slivers: TwentyFocusableBoxes(
                    Axis.Vertical,
                    (_, box) => new Focus(focusNode: focusNode, child: box)))));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 550.0, 800.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 600.0, 800.0, 650.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 550.0, 800.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageUp);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 950.0, 800.0, 1000.0), BoxRect(tester, "Box 0"));
        keys.SendKeyEvent(LogicalKeyboardKey.PageDown);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(0.0, 550.0, 800.0, 600.0), BoxRect(tester, "Box 0"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Reversed horizontal scrollables are scrolled when activated via
    // keyboard.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void ReversedHorizontalScrollablesAreScrolledWhenActivatedViaKeyboard(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        using var focusNode = new FocusNode(debugLabel: "SizedBox");
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                controller: controller,
                scrollDirection: Axis.Horizontal,
                reverse: true,
                slivers: TwentyFocusableBoxes(
                    Axis.Horizontal,
                    (_, box) => new Focus(focusNode: focusNode, child: box)))));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(750.0, 0.0, 800.0, 600.00), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
        Assert.Equal(Ltrb(800.0, 0.0, 850.0, 600.0), BoxRect(tester, "Box 0"));
        keys.SendKeyDownEvent(ModifierKey);
        keys.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        keys.SendKeyUpEvent(ModifierKey);
        tester.PumpAndSettle();
    }

    // Flutter: 'scrollable_helpers_test.dart: Custom scrollables with a center sliver are scrolled when activated via
    // keyboard.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void CustomScrollablesWithACenterSliverAreScrolledWhenActivatedViaKeyboard(string transitMode)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        List<string> items = Enumerable.Range(0, 20).Select(index => $"Item {index}").ToList();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                controller: controller,
                center: new ValueKey<string>("Center"),
                slivers: items.Select(item => (Widget)new SliverToBoxAdapter(
                    key: item == "Item 10" ? new ValueKey<string>("Center") : null,
                    child: new Focus(
                        autofocus: item == "Item 10",
                        child: new Container(
                            key: new ValueKey<string>(item),
                            alignment: Alignment.Center,
                            height: 100,
                            child: new Text(item))))).ToList())));

        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 100.0), BoxRect(tester, "Item 10"));
        for (int i = 0; i < 10; ++i)
        {
            keys.SendKeyDownEvent(ModifierKey);
            keys.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            keys.SendKeyUpEvent(ModifierKey);
            tester.PumpAndSettle();
        }

        // Starts at #10 already, so doesn't work out to 500.0 because it hits bottom.
        Assert.Equal(400.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, -400.0, 800.0, -300.0), BoxRect(tester, "Item 10"));
        for (int i = 0; i < 10; ++i)
        {
            keys.SendKeyDownEvent(ModifierKey);
            keys.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            keys.SendKeyUpEvent(ModifierKey);
            tester.PumpAndSettle();
        }

        // Goes up two past "center" where it started, so negative.
        Assert.Equal(-100.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 100.0, 800.0, 200.0), BoxRect(tester, "Item 10"));
    }

    // Flutter: 'scrollable_helpers_test.dart: Can scroll using intents only'
    [Fact]
    public void CanScrollUsingIntentsOnly()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new ListView(
                children:
                [
                    new SizedBox(height: 600.0, child: new Text("The cow as white as milk")),
                    new SizedBox(height: 600.0, child: new Text("The cape as red as blood")),
                    new SizedBox(height: 600.0, child: new Text("The hair as yellow as corn")),
                ])));
        Assert.Single(FindText(tester, "The cow as white as milk"));
        Assert.Empty(FindText(tester, "The cape as red as blood"));
        Assert.Empty(FindText(tester, "The hair as yellow as corn"));
        Actions.Invoke(
            tester.ElementOfType<SliverList>(),
            new ScrollIntent(direction: AxisDirection.Down, type: ScrollIncrementType.Page));
        tester.Pump(); // start scroll
        tester.Pump(TimeSpan.FromMilliseconds(1000)); // end scroll
        Assert.Single(FindText(tester, "The cow as white as milk"));
        Assert.Single(FindText(tester, "The cape as red as blood"));
        Assert.Empty(FindText(tester, "The hair as yellow as corn"));
        Actions.Invoke(
            tester.ElementOfType<SliverList>(),
            new ScrollIntent(direction: AxisDirection.Down, type: ScrollIncrementType.Page));
        tester.Pump(); // start scroll
        tester.Pump(TimeSpan.FromMilliseconds(1000)); // end scroll
        Assert.Empty(FindText(tester, "The cow as white as milk"));
        Assert.Single(FindText(tester, "The cape as red as blood"));
        Assert.Single(FindText(tester, "The hair as yellow as corn"));
    }

    // Regression test for https://github.com/flutter/flutter/issues/158063.
    // Flutter: 'scrollable_helpers_test.dart: Invoking a ScrollAction when notificationContext is null does not cause
    // an exception.'
    [Theory]
    [InlineData(KeySender.RawKeyData)]
    [InlineData(KeySender.KeyDataThenRawKeyData)]
    public void InvokingAScrollActionWhenNotificationContextIsNullDoesNotCauseAnException(string transitMode)
    {
        LogicalKeyboardKey[] keysWithModifier = [LogicalKeyboardKey.ArrowDown, LogicalKeyboardKey.ArrowUp];
        LogicalKeyboardKey[] allKeys =
        [
            .. keysWithModifier,
            LogicalKeyboardKey.PageDown,
            LogicalKeyboardKey.PageUp,
        ];
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var keys = new KeySender(transitMode);
        using var controller = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new PrimaryScrollController(
                controller: controller,
                child: new Focus(
                    autofocus: true,
                    child: new NoNotificationContextScrollable(
                        controller: controller,
                        viewportBuilder: (_, offset) => new Viewport(
                            offset: offset,
                            slivers: Enumerable.Range(0, 20)
                                .Select(index => (Widget)new SliverToBoxAdapter(
                                    child: new SizedBox(key: new ValueKey<string>($"Box {index}"), height: 50.0)))
                                .ToList()))))));

        // Verify the initial scroll offset.
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));

        foreach (LogicalKeyboardKey key in allKeys)
        {
            // The default web shortcuts do not use a modifier key for ScrollActions.
            if (keysWithModifier.Contains(key))
            {
                keys.SendKeyDownEvent(ModifierKey);
            }

            keys.SendKeyEvent(key);
            Assert.Null(tester.TakeException());

            if (keysWithModifier.Contains(key))
            {
                keys.SendKeyUpEvent(ModifierKey);
            }

            // No scrollable is found, so the scroll position should not change.
            tester.PumpAndSettle();
            Assert.Equal(0.0, controller.Position.Pixels);
            Assert.Equal(Ltrb(0.0, 0.0, 800.0, 50.0), BoxRect(tester, "Box 0"));
        }
    }

    // Flutter: 'scrollable_helpers_test.dart: EdgeDraggingAutoScroller handles drag target size correctly with
    // Transform.scale'
    [Fact]
    public void EdgeDraggingAutoScrollerHandlesDragTargetSizeCorrectlyWithTransformScale()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(
                child: Plumix.Widgets.Transform.Scale(
                    scale: 0.5,
                    child: SizedBox.Square(
                        dimension: 400.0,
                        child: ListView.Builder(
                            controller: controller,
                            itemCount: 20,
                            itemBuilder: (_, index) => new SizedBox(
                                height: 100,
                                child: new Center(child: new Text($"Item {index}")))))))));
        tester.PumpAndSettle();

        ScrollableState scrollableState = tester.State<ScrollableState>();
        var scroller = new EdgeDraggingAutoScroller(scrollableState, velocityScalar: 1.0);
        var scrollRenderBox = (RenderBox)scrollableState.Context.FindRenderObject()!;
        var dragTarget = new Rect(0, 0, scrollRenderBox.Size.Width, scrollRenderBox.Size.Height);

        scroller.StartAutoScrollIfNecessary(dragTarget);
        tester.Pump();

        Assert.Null(tester.TakeException());

        scroller.StopAutoScroll();
        tester.PumpAndSettle();
    }

    // ---------------------------------------------------------------- shared helpers

    /// <summary>
    /// Dart's <c>find.text(text)</c> with its default <c>skipOffstage: true</c>: the walk only
    /// descends through <see cref="Element.DebugVisitOnstageChildren"/>, so children a sliver keeps
    /// alive in its cache extent are not found.
    /// </summary>
    private static List<Element> FindText(FrameworkDartTester tester, string text)
    {
        var result = new List<Element>();
        void Visit(Element element)
        {
            if (element.Widget is Text { Data: var data } && data == text)
            {
                result.Add(element);
            }

            element.DebugVisitOnstageChildren(Visit);
        }

        tester.Root.DebugVisitOnstageChildren(Visit);
        return result;
    }

    private static void AssertMoreOrLessEquals(double expected, double actual, double epsilon = 1e-10)
    {
        Assert.True(Math.Abs(expected - actual) <= epsilon, $"Expected {expected} (+/- {epsilon}), got {actual}.");
    }

    // scrollable_of_test.dart: ScrollPositionListener.
    private sealed class ScrollPositionListener(Widget child, Action<string> log, Key? key = null)
        : StatefulWidget(key)
    {
        public Widget Child { get; } = child;

        public Action<string> Log { get; } = log;

        public override State CreateState() => new ScrollPositionListenerState();
    }

    private sealed class ScrollPositionListenerState : State<ScrollPositionListener>
    {
        private ScrollPosition? _position;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            _position?.RemoveListener(Listener);
            _position = Scrollable.MaybeOf(Context)?.Position;
            _position?.AddListener(Listener);
            Widget.Log($"didChangeDependencies {Fixed1(_position?.Pixels)}");
        }

        public override void Dispose()
        {
            _position?.RemoveListener(Listener);
            base.Dispose();
        }

        public override Widget Build(BuildContext context) => Widget.Child;

        private void Listener()
        {
            Widget.Log($"listener {Fixed1(_position?.Pixels)}");
        }

        // Dart's `double?.toStringAsFixed(1)` interpolated into a string.
        private static string Fixed1(double? value) =>
            value?.ToString("F1", CultureInfo.InvariantCulture) ?? "null";
    }

    // scrollable_of_test.dart: TestScrollController.
    private sealed class TestScrollController(bool deferLoading) : ScrollController
    {
        public bool DeferLoading { get; } = deferLoading;

        public override ScrollPosition CreateScrollPosition(
            ScrollPhysics physics,
            IScrollContext context,
            ScrollPosition? oldPosition)
        {
            return new TestScrollPosition(
                physics: physics,
                context: context,
                oldPosition: oldPosition,
                deferLoading: DeferLoading);
        }
    }

    // scrollable_of_test.dart: TestScrollPosition.
    private sealed class TestScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition,
        bool deferLoading)
        : ScrollPositionWithSingleContext(physics: physics, context: context, oldPosition: oldPosition)
    {
        public bool DeferLoading { get; } = deferLoading;

        public override bool RecommendDeferredLoading(BuildContext context) => DeferLoading;
    }

    // scrollable_of_test.dart: TestScrollable.
    private sealed class TestScrollable(Widget child, Key? key = null) : StatefulWidget(key)
    {
        public Widget Child { get; } = child;

        public override State CreateState() => new TestScrollableState();
    }

    private sealed class TestScrollableState : State<TestScrollable>
    {
        public int DependenciesChanged { get; private set; }

        public override void DidChangeDependencies()
        {
            DependenciesChanged += 1;
            base.DidChangeDependencies();
        }

        public override Widget Build(BuildContext context) => Widget.Child;
    }

    // scrollable_of_test.dart: TestChild.
    private sealed class TestChild(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new TestChildState();
    }

    private sealed class TestChildState : State<TestChild>
    {
        private ScrollableState? _scrollable;

        public int DependenciesChanged { get; private set; }

        public override void DidChangeDependencies()
        {
            DependenciesChanged += 1;
            _scrollable = Scrollable.Of(Context, axis: Axis.Horizontal);
            base.DidChangeDependencies();
        }

        public override Widget Build(BuildContext context)
        {
            return SizedBox.Square(dimension: 1000, child: new Text(_scrollable!.AxisDirection.ToString()));
        }
    }

    // scrollable_restoration_test.dart: TestHarness.
    private sealed class TestHarness(Widget child, double height = 100, Key? key = null) : StatelessWidget(key)
    {
        public Widget Child { get; } = child;

        public double Height { get; } = height;

        public override Widget Build(BuildContext context)
        {
            return new RootRestorationScope(
                restorationId: "root",
                child: new Directionality(
                    TextDirection.Ltr,
                    new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(height: Height, width: 50, child: Child))));
        }
    }

    // scrollable_restoration_test.dart: _TestHeaderDelegate.
    private sealed class TestHeaderDelegate(double minExtent, double maxExtent) : SliverPersistentHeaderDelegate
    {
        public override double MinExtent { get; } = minExtent;

        public override double MaxExtent { get; } = maxExtent;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent) =>
            SizedBox.Expand();

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    /// <summary>flutter_test's <c>TestRestorationData</c>: an opaque, identity-compared snapshot.</summary>
    private sealed class TestRestorationData(byte[]? binary)
    {
        public static readonly TestRestorationData Empty = new(null);

        public byte[]? Binary { get; } = binary;
    }

    /// <summary>
    /// flutter_test's <c>TestRestorationManager</c>: starts from empty data, keeps what the framework
    /// sends to the engine and restores from a snapshot on request.
    /// </summary>
    private sealed class FlutterTestRestorationManager : RestorationManager
    {
        public FlutterTestRestorationManager()
        {
            // Ensures that the root bucket is always available synchronously.
            RestoreFrom(TestRestorationData.Empty);
        }

        public TestRestorationData RestorationData { get; private set; } = TestRestorationData.Empty;

        public bool DebugRootBucketAccessed { get; private set; }

        public override void GetRootBucket(Action<RestorationBucket?> callback)
        {
            DebugRootBucketAccessed = true;
            base.GetRootBucket(callback);
        }

        public void RestoreFrom(TestRestorationData data)
        {
            RestorationData = data;
            HandleRestorationUpdateFromEngine(enabled: true, data: data.Binary);
        }

        protected override void InitChannels()
        {
        }

        protected override void GetRootBucketFromEngine()
        {
            HandleRestorationUpdateFromEngine(enabled: true, data: RestorationData.Binary);
        }

        protected override void SendToEngine(byte[] encodedData)
        {
            RestorationData = new TestRestorationData(encodedData);
        }
    }

    /// <summary>
    /// Installs a fresh <see cref="FlutterTestRestorationManager"/> as the ambient manager (Dart's
    /// per-test <c>binding.restorationManager</c>) and hosts <c>WidgetTester</c>'s
    /// <c>restartAndRestore</c>, <c>getRestorationData</c> and <c>restoreFrom</c>.
    /// </summary>
    private sealed class TestRestorationBinding : IDisposable
    {
        private readonly RestorationManager _previous = RestorationManager.Instance;
        private readonly FlutterTestRestorationManager _manager = new();

        public TestRestorationBinding()
        {
            RestorationManager.Instance = _manager;
        }

        /// <summary>
        /// Dart's <c>tester.restartAndRestore</c>: tears the tree down, restores the last data the
        /// framework sent to the engine and mounts the same widget again.
        /// </summary>
        public void RestartAndRestore(FrameworkDartTester tester, Widget widget)
        {
            Assert.True(
                _manager.DebugRootBucketAccessed,
                "Did you forget to wrap your widget tree in a RootRestorationScope?");
            TestRestorationData restorationData = _manager.RestorationData;
            tester.PumpWidget(new Container(key: new UniqueKey()));
            _manager.RestoreFrom(restorationData);
            tester.PumpWidget(widget);
        }

        /// <summary>Dart's <c>tester.getRestorationData</c>.</summary>
        public TestRestorationData GetRestorationData()
        {
            Assert.True(
                _manager.DebugRootBucketAccessed,
                "Did you forget to wrap your widget tree in a RootRestorationScope?");
            return _manager.RestorationData;
        }

        /// <summary>Dart's <c>tester.restoreFrom</c>.</summary>
        public void RestoreFrom(FrameworkDartTester tester, TestRestorationData data)
        {
            _manager.RestoreFrom(data);
            tester.Pump();
        }

        public void Dispose()
        {
            RestorationManager.Instance = _previous;
            _manager.Dispose();
        }
    }

    /// <summary>
    /// flutter_test's <c>sendKeyEvent</c>/<c>sendKeyDownEvent</c>/<c>sendKeyUpEvent</c> under a
    /// <c>KeySimulatorTransitModeVariant</c>: <see cref="RawKeyData"/> drives the legacy raw message
    /// through <see cref="KeyEventManager"/>, <see cref="KeyDataThenRawKeyData"/> the regularized
    /// <see cref="KeyEvent"/> stream through the focus manager.
    /// </summary>
    private sealed class KeySender(string transitMode)
    {
        public const string RawKeyData = "rawKeyData";
        public const string KeyDataThenRawKeyData = "keyDataThenRawKeyData";

        private readonly HashSet<LogicalKeyboardKey> _modifiersDown = [];

        public void SendKeyEvent(LogicalKeyboardKey key)
        {
            SendKeyDownEvent(key);
            SendKeyUpEvent(key);
        }

        public void SendKeyDownEvent(LogicalKeyboardKey key)
        {
            bool isModifier = IsModifier(key);
            if (isModifier)
            {
                _modifiersDown.Add(key);
            }

            if (transitMode == RawKeyData)
            {
                KeySim.DispatchRaw(key, down: true, control: Control, meta: Meta);
            }
            else if (isModifier)
            {
                FocusManager.Instance.HandleKeyEvent(new KeyDownEvent(KeySim.PhysicalFor(key), key));
            }
            else
            {
                FocusManager.Instance.HandleKeyEvent(KeySim.Down(key, control: Control, meta: Meta));
            }

            Scheduler.FlushMicrotasks();
        }

        public void SendKeyUpEvent(LogicalKeyboardKey key)
        {
            bool isModifier = IsModifier(key);
            if (isModifier)
            {
                _modifiersDown.Remove(key);
            }

            if (transitMode == RawKeyData)
            {
                KeySim.DispatchRaw(key, down: false, control: Control, meta: Meta);
            }
            else if (isModifier)
            {
                FocusManager.Instance.HandleKeyEvent(new KeyUpEvent(KeySim.PhysicalFor(key), key));
            }
            else
            {
                FocusManager.Instance.HandleKeyEvent(KeySim.Up(key, control: Control, meta: Meta));
            }

            Scheduler.FlushMicrotasks();
        }

        private bool Control => _modifiersDown.Contains(LogicalKeyboardKey.ControlLeft);

        private bool Meta => _modifiersDown.Contains(LogicalKeyboardKey.MetaLeft);

        private static bool IsModifier(LogicalKeyboardKey key) =>
            key == LogicalKeyboardKey.ControlLeft || key == LogicalKeyboardKey.MetaLeft;
    }

    // scrollable_helpers_test.dart: _NoNotificationContextScrollable.
    private sealed class NoNotificationContextScrollable(ScrollController? controller, ViewportBuilder viewportBuilder)
        : Scrollable(viewportBuilder, controller: controller)
    {
        public override State CreateState() => new NoNotificationContextScrollableState();
    }

    private sealed class NoNotificationContextScrollableState : ScrollableState
    {
        public override BuildContext? NotificationContext => null;
    }
}
