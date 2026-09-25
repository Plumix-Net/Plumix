using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/scrollable_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollableDartParityTests
{
    private const double DragOffset = 200.0;

    private static readonly TargetPlatform[] AllPlatforms =
    [
        TargetPlatform.Android,
        TargetPlatform.Fuchsia,
        TargetPlatform.IOS,
        TargetPlatform.Linux,
        TargetPlatform.MacOS,
        TargetPlatform.Windows,
    ];

    public static IEnumerable<object[]> AllPlatformData => AllPlatforms.Select(platform => new object[] { platform });

    // Flutter: 'hitTestBehavior is respected'
    [Fact]
    public void HitTestBehaviorIsRespected()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        HitTestBehavior? GetBehavior<TWidget>() where TWidget : Widget
        {
            Element of = tester.ElementOfType<TWidget>();
            var widget = (RawGestureDetector)Descendants<RawGestureDetector>(of).Single().Widget;
            return widget.Behavior;
        }

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(hitTestBehavior: HitTestBehavior.Translucent)));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<SingleChildScrollView>());

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(hitTestBehavior: HitTestBehavior.Translucent)));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<CustomScrollView>());

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new ListView(hitTestBehavior: HitTestBehavior.Translucent)));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<ListView>());

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            GridView.Extent(maxCrossAxisExtent: 1, hitTestBehavior: HitTestBehavior.Translucent)));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<GridView>());

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new PageView(hitTestBehavior: HitTestBehavior.Translucent)));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<PageView>());

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new ListWheelScrollView(
                itemExtent: 10,
                hitTestBehavior: HitTestBehavior.Translucent,
                children: [])));
        Assert.Equal(HitTestBehavior.Translucent, GetBehavior<ListWheelScrollView>());
    }

    // Flutter: 'hitTestBehavior.translucent lets widgets underneath catch the hit'
    [Fact]
    public void HitTestBehaviorTranslucentLetsWidgetsUnderneathCatchTheHit()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        Key key = new UniqueKey();
        bool tapped = false;
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Stack(
                children:
                [
                    Positioned.Fill(new GestureDetector(
                        behavior: HitTestBehavior.Opaque,
                        onTap: () => tapped = true,
                        child: new SizedBox(key: key, height: 300))),
                    new SingleChildScrollView(hitTestBehavior: HitTestBehavior.Translucent),
                ])));
        TapAt(tester, tester.GetCenter(tester.ElementsWithKey(key).Single()));
        Assert.True(tapped);
    }

    // Flutter: 'Holding scroll'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void HoldingScroll(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, 200.0), touchSlopY: 0.0);
            Assert.Equal(-200.0, GetScrollOffset(tester));
            tester.Pump(); // trigger ballistic
            tester.Pump(TimeSpan.FromMilliseconds(10));
            Assert.True(GetScrollOffset(tester) > -200.0);
            Assert.True(GetScrollOffset(tester) < 0.0);
            double heldPosition = GetScrollOffset(tester);
            // Hold and let go while in overscroll.
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            Assert.Equal(1, tester.PumpAndSettle());
            Assert.Equal(heldPosition, GetScrollOffset(tester));
            gesture.Up();
            // Once the hold is let go, it should still snap back to origin.
            Assert.Equal(3, PumpAndSettle(tester, TimeSpan.FromMinutes(1)));
            Assert.Equal(0.0, GetScrollOffset(tester));
        });
    }

    // Flutter: 'Repeated flings builds momentum'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void RepeatedFlingsBuildsMomentum(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump(); // trigger fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            // Repeat the exact same motion.
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump();
            // On iOS, the velocity will be larger than the velocity of the last fling by a
            // non-trivial amount.
            Assert.True(GetScrollVelocity(tester) > 1100.0);
        });
    }

    // Flutter: 'Repeated flings do not build momentum on Android'
    [Fact]
    public void RepeatedFlingsDoNotBuildMomentumOnAndroid()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester);
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
        tester.Pump(); // trigger fling
        tester.Pump(TimeSpan.FromMilliseconds(10));
        // Repeat the exact same motion.
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
        tester.Pump();
        // On Android, there is no momentum build. The final velocity is the same as the
        // velocity of the last fling.
        AssertMoreOrLessEquals(1000.0, GetScrollVelocity(tester));
    }

    // Flutter: 'A slower final fling does not apply carried momentum'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void ASlowerFinalFlingDoesNotApplyCarriedMomentum(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump(); // trigger fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            // Repeat the exact same motion to build momentum.
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump(); // trigger the second fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            // Make a final fling that is much slower.
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 200.0);
            tester.Pump(); // trigger the third fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            // expect that there is no carried velocity
            Assert.True(GetScrollVelocity(tester) < 200.0);
        });
    }

    // Flutter: 'No iOS/macOS momentum build with flings in opposite directions'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void NoIOSMacOSMomentumBuildWithFlingsInOppositeDirections(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump(); // trigger fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            // Repeat the exact same motion in the opposite direction.
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, DragOffset), 1000.0);
            tester.Pump();
            // The only applied velocity to the scrollable is the second fling that was in the
            // opposite direction.
            Assert.Equal(-1000.0, GetScrollVelocity(tester));
        });
    }

    // Flutter: 'No iOS/macOS momentum kept on hold gestures'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void NoIOSMacOSMomentumKeptOnHoldGestures(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
            tester.Pump(); // trigger fling
            tester.Pump(TimeSpan.FromMilliseconds(10));
            Assert.True(GetScrollVelocity(tester) > 0.0);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            tester.Pump(TimeSpan.FromMilliseconds(40));
            gesture.Up();
            // After a hold longer than 2 frames, previous velocity is lost.
            Assert.Equal(0.0, GetScrollVelocity(tester));
        });
    }

    // Flutter: 'Drags creeping unaffected on Android'
    [Fact]
    public void DragsCreepingUnaffectedOnAndroid()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester);
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Touch);
        try
        {
            gesture.MoveBy(new Vector(0.0, -0.5));
            Assert.Equal(0.5, GetScrollOffset(tester));
            gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(10));
            Assert.Equal(1.0, GetScrollOffset(tester));
            gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(20));
            Assert.Equal(1.5, GetScrollOffset(tester));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'Drags creeping must break threshold on iOS/macOS'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void DragsCreepingMustBreakThresholdOnIOSMacOS(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            try
            {
                gesture.MoveBy(new Vector(0.0, -0.5));
                Assert.Equal(0.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(10));
                Assert.Equal(0.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(20));
                Assert.Equal(0.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(30));
                // Now -2.5 in total.
                Assert.Equal(0.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(40));
                // Now -3.5, just reached threshold.
                Assert.Equal(0.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(50));
                // -0.5 over threshold transferred.
                Assert.Equal(0.5, GetScrollOffset(tester));
            }
            finally
            {
                gesture.RemovePointer();
            }
        });
    }

    // Flutter: 'Big drag over threshold magnitude preserved on iOS/macOS'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void BigDragOverThresholdMagnitudePreservedOnIOSMacOS(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            try
            {
                gesture.MoveBy(new Vector(0.0, -30.0));
                // No offset lost from threshold.
                Assert.Equal(30.0, GetScrollOffset(tester));
            }
            finally
            {
                gesture.RemovePointer();
            }
        });
    }

    // Flutter: 'Slow threshold breaks are attenuated on iOS/macOS'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void SlowThresholdBreaksAreAttenuatedOnIOSMacOS(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            try
            {
                // This is a typical 'hesitant' iOS scroll start.
                gesture.MoveBy(new Vector(0.0, -10.0));
                AssertMoreOrLessEquals(1.1666666666666667, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -10.0), TimeSpan.FromMilliseconds(20));
                // Subsequent motions unaffected.
                AssertMoreOrLessEquals(11.16666666666666673, GetScrollOffset(tester));
            }
            finally
            {
                gesture.RemovePointer();
            }
        });
    }

    // Flutter: 'Small continuing motion preserved on iOS/macOS'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void SmallContinuingMotionPreservedOnIOSMacOS(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            try
            {
                gesture.MoveBy(new Vector(0.0, -30.0)); // Break threshold.
                Assert.Equal(30.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(20));
                Assert.Equal(30.5, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(40));
                Assert.Equal(31.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(60));
                Assert.Equal(31.5, GetScrollOffset(tester));
            }
            finally
            {
                gesture.RemovePointer();
            }
        });
    }

    // Flutter: 'Motion stop resets threshold on iOS/macOS'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void MotionStopResetsThresholdOnIOSMacOS(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Touch);
            try
            {
                gesture.MoveBy(new Vector(0.0, -30.0)); // Break threshold.
                Assert.Equal(30.0, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -0.5), TimeSpan.FromMilliseconds(20));
                Assert.Equal(30.5, GetScrollOffset(tester));
                gesture.MoveBy(default, TimeSpan.FromMilliseconds(21));
                // Stationary too long, threshold reset.
                gesture.MoveBy(default, TimeSpan.FromMilliseconds(120));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(140));
                Assert.Equal(30.5, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(150));
                Assert.Equal(30.5, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(160));
                Assert.Equal(30.5, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(170));
                // New threshold broken.
                Assert.Equal(31.5, GetScrollOffset(tester));
                gesture.MoveBy(new Vector(0.0, -1.0), TimeSpan.FromMilliseconds(180));
                Assert.Equal(32.5, GetScrollOffset(tester));
            }
            finally
            {
                gesture.RemovePointer();
            }
        });
    }

    // Flutter: 'Scroll pointer signals are handled on Fuchsia'
    [Fact]
    public void ScrollPointerSignalsAreHandledOnFuchsia()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester);
        Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
        Assert.Equal(20.0, GetScrollOffset(tester));
        // Pointer signals should not cause overscroll.
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, -30.0));
        Assert.Equal(0.0, GetScrollOffset(tester));
    }

    // Flutter: 'Scroll pointer signals are handled when there is competition'
    [Fact]
    public void ScrollPointerSignalsAreHandledWhenThereIsCompetition()
    {
        // This is a regression test. When there are multiple scrollables listening
        // to the same event, for example when scrollables are nested, there used
        // to be exceptions at scrolling events.

        // Pump a nested scrollable. The outer scrollable contains a sliver of a
        // 300-pixel-long scrollable followed by a 2000-pixel-long content.
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                slivers:
                [
                    new SliverToBoxAdapter(new SizedBox(
                        height: 300,
                        child: new CustomScrollView(
                            slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))]))),
                    new SliverToBoxAdapter(new SizedBox(height: 2000.0)),
                ])));

        Point scrollEventLocation = tester.GetCenter(tester.ElementsOfType<Viewport>()[^1]);
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
        Assert.Equal(20.0, GetScrollOffset(tester));
        // Pointer signals should not cause overscroll.
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, -30.0));
        Assert.Equal(0.0, GetScrollOffset(tester));
    }

    // Flutter: 'Scroll pointer signals are ignored when scrolling is disabled'
    [Fact]
    public void ScrollPointerSignalsAreIgnoredWhenScrollingIsDisabled()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester, scrollable: false);
        Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
        Assert.Equal(0.0, GetScrollOffset(tester));
    }

    // Flutter: 'Engine is notified of ignored pointer signals (no scroll physics)'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void EngineIsNotifiedOfIgnoredPointerSignalsNoScrollPhysics(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester, scrollable: false);
            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());

            bool allowedPlatformDefault = false;
            foreach (bool allow in tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0)))
            {
                allowedPlatformDefault = allow;
            }

            // Engine should be notified of ignored scroll pointer signals.
            Assert.True(allowedPlatformDefault);
        });
    }

    // Flutter: 'Engine is notified of accepted and rejected scroll events'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void EngineIsNotifiedOfAcceptedAndRejectedScrollEvents(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester, scrollDirection: Axis.Horizontal);

            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());

            // Horizontal input is accepted
            SendKeyDown(LogicalKeyboardKey.ShiftLeft, shift: true);
            List<bool> accepted = tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 10.0));
            SendKeyUp(LogicalKeyboardKey.ShiftLeft);
            tester.Pump();
            // Engine should be notified when scroll is accepted.
            Assert.NotEmpty(accepted);
            // Accepted scroll should be reported as handled to the engine.
            Assert.False(accepted[^1]);

            // Vertical input not accepted
            List<bool> rejected = tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Engine should be notified when scroll is rejected.
            Assert.NotEmpty(rejected);
            // Engine should be notified when scroll is rejected by the scrollable.
            Assert.True(rejected[^1]);
        });
    }

    // Flutter: 'Holding scroll and Scroll pointer signal will update ScrollDirection.forward /
    // ScrollDirection.reverse'
    [Fact]
    public void HoldingScrollAndScrollPointerSignalWillUpdateScrollDirection()
    {
        ScrollDirection? lastUserScrollingDirection = null;

        using var controller = new ScrollController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        PumpTest(tester, controller: controller);

        controller.AddListener(() =>
        {
            if (controller.Position.UserScrollDirection != ScrollDirection.Idle)
            {
                lastUserScrollingDirection = controller.Position.UserScrollDirection;
            }
        });

        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, -20.0), touchSlopY: 0.0);

        Assert.Equal(ScrollDirection.Reverse, lastUserScrollingDirection);

        Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));

        Assert.Equal(ScrollDirection.Reverse, lastUserScrollingDirection);

        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, 20.0), touchSlopY: 0.0);

        Assert.Equal(ScrollDirection.Forward, lastUserScrollingDirection);

        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, -20.0));

        Assert.Equal(ScrollDirection.Forward, lastUserScrollingDirection);
    }

    // Flutter: 'Scrolls in correct direction when scroll axis is reversed'
    [Fact]
    public void ScrollsInCorrectDirectionWhenScrollAxisIsReversed()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester, reverse: true);

        Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
        tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, -20.0));

        Assert.Equal(20.0, GetScrollOffset(tester));
    }

    // Flutter: 'Scrolls horizontally when shift is pressed by default'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void ScrollsHorizontallyWhenShiftIsPressedByDefault(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester, scrollDirection: Axis.Horizontal);

            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(0.0, GetScrollOffset(tester));

            SendKeyDown(LogicalKeyboardKey.ShiftLeft, shift: true);
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input flipped to horizontal and accepted.
            Assert.Equal(20.0, GetScrollOffset(tester));
            SendKeyUp(LogicalKeyboardKey.ShiftLeft);
            tester.Pump();

            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(20.0, GetScrollOffset(tester));
        });
    }

    // Flutter: 'Scroll axis is not flipped for trackpad'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void ScrollAxisIsNotFlippedForTrackpad(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester, scrollDirection: Axis.Horizontal);

            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0), PointerDeviceKind.Trackpad);
            // Vertical input not accepted
            Assert.Equal(0.0, GetScrollOffset(tester));

            SendKeyDown(LogicalKeyboardKey.ShiftLeft, shift: true);
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0), PointerDeviceKind.Trackpad);
            // Vertical input not flipped.
            Assert.Equal(0.0, GetScrollOffset(tester));
            SendKeyUp(LogicalKeyboardKey.ShiftLeft);
            tester.Pump();

            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0), PointerDeviceKind.Trackpad);
            // Vertical input not accepted
            Assert.Equal(0.0, GetScrollOffset(tester));
        });
    }

    // Flutter: 'Scrolls horizontally when custom key is pressed'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void ScrollsHorizontallyWhenCustomKeyIsPressed(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(
                tester,
                scrollDirection: Axis.Horizontal,
                axisModifier: new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.AltLeft });

            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(0.0, GetScrollOffset(tester));

            SendKeyDown(LogicalKeyboardKey.AltLeft, alt: true);
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input flipped to horizontal and accepted.
            Assert.Equal(20.0, GetScrollOffset(tester));
            SendKeyUp(LogicalKeyboardKey.AltLeft);
            tester.Pump();

            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(20.0, GetScrollOffset(tester));
        });
    }

    // Flutter: 'Still scrolls horizontally when other keys are pressed at the same time'
    [Theory]
    [MemberData(nameof(AllPlatformData))]
    public void StillScrollsHorizontallyWhenOtherKeysArePressedAtTheSameTime(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(
                tester,
                scrollDirection: Axis.Horizontal,
                axisModifier: new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.AltLeft });

            Point scrollEventLocation = tester.GetCenter(tester.ElementOfType<Viewport>());
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(0.0, GetScrollOffset(tester));

            SendKeyDown(LogicalKeyboardKey.AltLeft, alt: true);
            SendKeyDown(LogicalKeyboardKey.Space, alt: true);
            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical flipped & accepted.
            Assert.Equal(20.0, GetScrollOffset(tester));
            SendKeyUp(LogicalKeyboardKey.AltLeft);
            SendKeyUp(LogicalKeyboardKey.Space);
            tester.Pump();

            tester.SendPointerScroll(scrollEventLocation, new Vector(0.0, 20.0));
            // Vertical input not accepted
            Assert.Equal(20.0, GetScrollOffset(tester));
        });
    }

    // group 'setCanDrag to false with active drag gesture: ': pumpTestWidget.
    private static void PumpCanDragTestWidget(FrameworkDartTester tester, bool canDrag)
    {
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                physics: canDrag ? new AlwaysScrollableScrollPhysics() : new NeverScrollableScrollPhysics(),
                slivers:
                [
                    new SliverToBoxAdapter(new SizedBox(height: 2000, child: new GestureDetector(onTap: () => { }))),
                ])));
    }

    private static RenderIgnorePointer ScrollIgnorePointer(FrameworkDartTester tester)
    {
        Element ignorePointer = Descendants<IgnorePointer>(tester.ElementOfType<CustomScrollView>()).Single();
        return (RenderIgnorePointer)ignorePointer.FindRenderObject()!;
    }

    // Flutter: 'setCanDrag to false with active drag gesture: Hold does not disable user interaction'
    [Fact]
    public void SetCanDragFalseHoldDoesNotDisableUserInteraction()
    {
        // Regression test for https://github.com/flutter/flutter/issues/66816.
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpCanDragTestWidget(tester, canDrag: true);
        RenderIgnorePointer renderIgnorePointer = ScrollIgnorePointer(tester);

        Assert.False(renderIgnorePointer.Ignoring);

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Viewport>()),
            PointerDeviceKind.Touch);
        Assert.False(renderIgnorePointer.Ignoring);

        PumpCanDragTestWidget(tester, canDrag: false);
        Assert.False(renderIgnorePointer.Ignoring);

        gesture.Up();
        Assert.False(renderIgnorePointer.Ignoring);
    }

    // Flutter: 'setCanDrag to false with active drag gesture: Drag disables user interaction when
    // recognized'
    [Fact]
    public void SetCanDragFalseDragDisablesUserInteractionWhenRecognized()
    {
        // Regression test for https://github.com/flutter/flutter/issues/66816.
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpCanDragTestWidget(tester, canDrag: true);
        RenderIgnorePointer renderIgnorePointer = ScrollIgnorePointer(tester);
        Assert.False(renderIgnorePointer.Ignoring);

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Viewport>()),
            PointerDeviceKind.Touch);
        Assert.False(renderIgnorePointer.Ignoring);

        gesture.MoveBy(new Vector(0, -100));
        // Starts ignoring when the drag is recognized.
        Assert.True(renderIgnorePointer.Ignoring);

        PumpCanDragTestWidget(tester, canDrag: false);
        Assert.False(renderIgnorePointer.Ignoring);

        gesture.Up();
        Assert.False(renderIgnorePointer.Ignoring);
    }

    // Flutter: 'setCanDrag to false with active drag gesture: Ballistic disables user interaction
    // until it stops'
    [Fact]
    public void SetCanDragFalseBallisticDisablesUserInteractionUntilItStops()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpCanDragTestWidget(tester, canDrag: true);
        RenderIgnorePointer renderIgnorePointer = ScrollIgnorePointer(tester);
        Assert.False(renderIgnorePointer.Ignoring);

        // Starts ignoring when the drag is recognized.
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0, -100), 1000);
        Assert.True(renderIgnorePointer.Ignoring);
        tester.Pump();

        // When the activity ends we should stop ignoring pointers.
        tester.PumpAndSettle();
        Assert.False(renderIgnorePointer.Ignoring);
    }

    // Flutter: 'Can recommendDeferredLoadingForContext - animation'
    [Fact]
    public void CanRecommendDeferredLoadingForContextAnimation()
    {
        var widgetTracker = new List<string>();
        int cheapWidgets = 0;
        int expensiveWidgets = 0;
        using var controller = new ScrollController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            ListView.Builder(
                controller: controller,
                itemBuilder: (context, _) =>
                {
                    if (Scrollable.RecommendDeferredLoadingForContext(context))
                    {
                        cheapWidgets += 1;
                        widgetTracker.Add("cheap");
                        return new SizedBox(height: 50.0);
                    }

                    widgetTracker.Add("expensive");
                    expensiveWidgets += 1;
                    return new SizedBox(height: 50.0);
                })));

        tester.PumpAndSettle();

        Assert.Equal(17, expensiveWidgets);
        Assert.Equal(0, cheapWidgets);

        // The position value here is different from the maximum velocity we will
        // reach, which is controlled by a combination of curve, duration, and
        // position.
        // This is just meant to be a pretty good simulation. A linear curve
        // with these same parameters will never back off on the velocity enough
        // to reset here.
        controller.AnimateTo(5000, duration: TimeSpan.FromSeconds(2), curve: Curves.Linear);

        Assert.Equal(17, expensiveWidgets);
        Assert.True(widgetTracker.All(type => type == "expensive"));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(17, expensiveWidgets);
        Assert.Equal(25, cheapWidgets);
        Assert.True(widgetTracker.Skip(17).All(type => type == "cheap"));

        tester.PumpAndSettle();

        Assert.Equal(22, expensiveWidgets);
        Assert.Equal(95, cheapWidgets);
        Assert.True(widgetTracker.Skip(17).Skip(25).Take(70).All(type => type == "cheap"));
        Assert.True(widgetTracker.Skip(17).Skip(25).Skip(70).All(type => type == "expensive"));
    }

    // Flutter: 'Can recommendDeferredLoadingForContext - ballistics'
    [Fact]
    public void CanRecommendDeferredLoadingForContextBallistics()
    {
        int cheapWidgets = 0;
        int expensiveWidgets = 0;
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            ListView.Builder(
                itemBuilder: (context, index) =>
                {
                    if (Scrollable.RecommendDeferredLoadingForContext(context))
                    {
                        cheapWidgets += 1;
                        return new SizedBox(height: 50.0);
                    }

                    expensiveWidgets += 1;
                    return new SizedBox(key: new ValueKey<string>($"Box {index}"), height: 50.0);
                })));

        tester.PumpAndSettle();
        Assert.Single(FindByKey(tester, new ValueKey<string>("Box 0")));
        Assert.Empty(FindByKey(tester, new ValueKey<string>("Box 52")));

        Assert.Equal(17, expensiveWidgets);
        Assert.Equal(0, cheapWidgets);

        // Getting the tester to simulate a life-like fling is difficult.
        // Instead, just manually drive the activity with a ballistic simulation as
        // if the user has flung the list.
        Scrollable.Of(FirstOfType<SizedBox>(tester)).Position.Activity.Delegate.GoBallistic(4000);

        tester.PumpAndSettle();
        Assert.Empty(FindByKey(tester, new ValueKey<string>("Box 0")));
        Assert.Single(FindByKey(tester, new ValueKey<string>("Box 52")));

        Assert.Equal(40, expensiveWidgets);
        Assert.Equal(21, cheapWidgets);
    }

    // Flutter: 'Can recommendDeferredLoadingForContext - override heuristic'
    [Fact]
    public void CanRecommendDeferredLoadingForContextOverrideHeuristic()
    {
        int cheapWidgets = 0;
        int expensiveWidgets = 0;
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            ListView.Builder(
                physics: new SuperPessimisticScrollPhysics(),
                itemBuilder: (context, index) =>
                {
                    if (Scrollable.RecommendDeferredLoadingForContext(context))
                    {
                        cheapWidgets += 1;
                        return new SizedBox(key: new ValueKey<string>($"Cheap box {index}"), height: 50.0);
                    }

                    expensiveWidgets += 1;
                    return new SizedBox(key: new ValueKey<string>($"Box {index}"), height: 50.0);
                })));
        tester.PumpAndSettle();

        ScrollPosition position = Scrollable.Of(FirstOfType<SizedBox>(tester)).Position;
        var physics = (SuperPessimisticScrollPhysics)position.Physics;

        Assert.Single(FindByKey(tester, new ValueKey<string>("Box 0")));
        Assert.Empty(FindByKey(tester, new ValueKey<string>("Cheap box 52")));

        Assert.Equal(17, physics.Count);
        Assert.Equal(17, expensiveWidgets);
        Assert.Equal(0, cheapWidgets);

        // Getting the tester to simulate a life-like fling is difficult.
        // Instead, just manually drive the activity with a ballistic simulation as
        // if the user has flung the list.
        position.Activity.Delegate.GoBallistic(4000);

        tester.PumpAndSettle();

        Assert.Empty(FindByKey(tester, new ValueKey<string>("Box 0")));
        Assert.Single(FindByKey(tester, new ValueKey<string>("Cheap box 52")));

        Assert.Equal(17, expensiveWidgets);
        Assert.Equal(44, cheapWidgets);
        Assert.Equal(44 + 17, physics.Count);
    }

    // Flutter: 'Can recommendDeferredLoadingForContext - override heuristic and always return true'
    [Fact]
    public void CanRecommendDeferredLoadingForContextOverrideHeuristicAndAlwaysReturnTrue()
    {
        int cheapWidgets = 0;
        int expensiveWidgets = 0;
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            ListView.Builder(
                physics: new ExtraSuperPessimisticScrollPhysics(),
                itemBuilder: (context, index) =>
                {
                    if (Scrollable.RecommendDeferredLoadingForContext(context))
                    {
                        cheapWidgets += 1;
                        return new SizedBox(key: new ValueKey<string>($"Cheap box {index}"), height: 50.0);
                    }

                    expensiveWidgets += 1;
                    return new SizedBox(key: new ValueKey<string>($"Box {index}"), height: 50.0);
                })));
        tester.PumpAndSettle();

        ScrollPosition position = Scrollable.Of(FirstOfType<SizedBox>(tester)).Position;

        Assert.Single(FindByKey(tester, new ValueKey<string>("Cheap box 0")));
        Assert.Empty(FindByKey(tester, new ValueKey<string>("Cheap box 52")));

        Assert.Equal(0, expensiveWidgets);
        Assert.Equal(17, cheapWidgets);

        // Getting the tester to simulate a life-like fling is difficult.
        // Instead, just manually drive the activity with a ballistic simulation as
        // if the user has flung the list.
        position.Activity.Delegate.GoBallistic(4000);

        tester.PumpAndSettle();

        Assert.Empty(FindByKey(tester, new ValueKey<string>("Cheap box 0")));
        Assert.Single(FindByKey(tester, new ValueKey<string>("Cheap box 52")));

        Assert.Equal(0, expensiveWidgets);
        Assert.Equal(61, cheapWidgets);
    }

    // Flutter: 'ensureVisible does not move PageViews'
    [Fact]
    public void EnsureVisibleDoesNotMovePageViews()
    {
        using var controller = new PageController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new PageView(
                controller: controller,
                children: Enumerable.Range(0, 3).Select(pageIndex => (Widget)new ListView(
                    key: new ValueKey<string>($"list_{pageIndex}"),
                    children: Enumerable.Range(0, 100).Select(listIndex => (Widget)new Row(
                        children:
                        [
                            new Container(
                                key: new ValueKey<string>($"{pageIndex}_{listIndex}_0"),
                                color: new Color(0xFFFF0000),
                                width: 200,
                                height: 10),
                            new Container(
                                key: new ValueKey<string>($"{pageIndex}_{listIndex}_1"),
                                color: new Color(0xFF0000FF),
                                width: 200,
                                height: 10),
                            new Container(
                                key: new ValueKey<string>($"{pageIndex}_{listIndex}_2"),
                                color: new Color(0xFF00FF00),
                                width: 200,
                                height: 10),
                        ])).ToList())).ToList())));

        var targetMidRightPage0 = new ValueKey<string>("0_25_2");
        var targetMidRightPage1 = new ValueKey<string>("1_25_2");
        var targetMidLeftPage1 = new ValueKey<string>("1_25_0");

        Assert.Single(FindByKey(tester, new ValueKey<string>("list_0")));
        Assert.Empty(FindByKey(tester, new ValueKey<string>("list_1")));
        Assert.Single(FindByKey(tester, targetMidRightPage0));
        Assert.Empty(FindByKey(tester, targetMidRightPage1));
        Assert.Empty(FindByKey(tester, targetMidLeftPage1));

        Scrollable.EnsureVisible(FindByKey(tester, targetMidRightPage0).Single());
        tester.PumpAndSettle();
        Assert.Single(FindByKey(tester, targetMidRightPage0));
        Assert.Empty(FindByKey(tester, targetMidRightPage1));
        Assert.Empty(FindByKey(tester, targetMidLeftPage1));

        controller.JumpToPage(1);
        tester.PumpAndSettle();

        Assert.Empty(FindByKey(tester, new ValueKey<string>("list_0")));
        Assert.Single(FindByKey(tester, new ValueKey<string>("list_1")));
        Scrollable.EnsureVisible(FindByKey(tester, targetMidRightPage1).Single());
        tester.PumpAndSettle();

        Assert.Empty(FindByKey(tester, targetMidRightPage0));
        Assert.Single(FindByKey(tester, targetMidRightPage1));
        Assert.Single(FindByKey(tester, targetMidLeftPage1));

        Scrollable.EnsureVisible(FindByKey(tester, targetMidLeftPage1).Single());
        tester.PumpAndSettle();

        Assert.Empty(FindByKey(tester, targetMidRightPage0));
        Assert.Single(FindByKey(tester, targetMidRightPage1));
        Assert.Single(FindByKey(tester, targetMidLeftPage1));
    }

    // Flutter: 'PointerScroll on nested NeverScrollable ListView goes to outer Scrollable.'
    [Fact]
    public void PointerScrollOnNestedNeverScrollableListViewGoesToOuterScrollable()
    {
        // Regression test for https://github.com/flutter/flutter/issues/70948
        using var outerController = new ScrollController();
        using var innerController = new ScrollController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(
                controller: outerController,
                child: new Row(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new Column(
                            children: Enumerable.Range(0, 100)
                                .Select(i => (Widget)new Text($"SingleChildScrollView {i}"))
                                .ToList()),
                        new SizedBox(
                            height: 3000,
                            width: 400,
                            child: ListView.Builder(
                                controller: innerController,
                                physics: new NeverScrollableScrollPhysics(),
                                itemCount: 100,
                                itemBuilder: (_, index) => new Text($"Nested NeverScrollable ListView {index}"))),
                    ]))));
        Assert.Equal(0.0, outerController.Position.Pixels);
        Assert.Equal(0.0, innerController.Position.Pixels);
        Point outerScrollable = tester.GetCenter(FindByText(tester, "SingleChildScrollView 3").Single());
        // Hover over the outer scroll view and create a pointer scroll.
        tester.SendPointerScroll(outerScrollable, new Vector(0.0, 20.0));
        tester.Pump(TimeSpan.FromMilliseconds(250));
        Assert.Equal(20.0, outerController.Position.Pixels);
        Assert.Equal(0.0, innerController.Position.Pixels);

        Point innerScrollable = tester.GetCenter(FindByText(tester, "Nested NeverScrollable ListView 20").Single());
        // Hover over the inner scroll view and create a pointer scroll.
        // This inner scroll view is not scrollable, and so the outer should scroll.
        tester.SendPointerScroll(innerScrollable, new Vector(0.0, -20.0));
        tester.Pump(TimeSpan.FromMilliseconds(250));
        Assert.Equal(0.0, outerController.Position.Pixels);
        Assert.Equal(0.0, innerController.Position.Pixels);
    }

    // Regression test for https://github.com/flutter/flutter/issues/71949
    // Flutter: 'Zero offset pointer scroll should not trigger an assertion.'
    [Fact]
    public void ZeroOffsetPointerScrollShouldNotTriggerAnAssertion()
    {
        using var controller = new ScrollController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        Widget Build(double height)
        {
            return new Directionality(
                TextDirection.Ltr,
                new Align(
                    child: new SizedBox(
                        width: double.PositiveInfinity,
                        height: height,
                        child: new SingleChildScrollView(
                            controller: controller,
                            child: new SizedBox(width: double.PositiveInfinity, height: 300.0)))));
        }

        tester.PumpWidget(Build(200.0));
        Assert.Equal(0.0, controller.Position.Pixels);

        controller.JumpTo(100.0);
        Assert.Equal(100.0, controller.Position.Pixels);

        // Make the outer constraints larger that the scrollable widget is no longer able to scroll.
        tester.PumpWidget(Build(300.0));
        Assert.Equal(0.0, controller.Position.Pixels);
        Assert.Equal(0.0, controller.Position.MaxScrollExtent);

        // Hover over the scroll view and create a zero offset pointer scroll.
        Point scrollable = tester.GetCenter(tester.ElementOfType<SingleChildScrollView>());
        tester.SendPointerScroll(scrollable, default);

        Assert.Null(tester.TakeException());
    }

    // Flutter: 'Accepts drag with unknown device kind by default'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Android)]
    public void AcceptsDragWithUnknownDeviceKindByDefault(TargetPlatform platform)
    {
        // Regression test for https://github.com/flutter/flutter/issues/90912.
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                new CustomScrollView(slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))])));
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Unknown);
            Assert.Equal(0.0, GetScrollOffset(tester));
            gesture.MoveBy(new Vector(0.0, -200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(200, GetScrollOffset(tester));

            gesture.MoveBy(new Vector(0.0, 200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(0.0, GetScrollOffset(tester));

            gesture.RemovePointer();
            tester.Pump();
        });
    }

    // Flutter: 'Does not scroll with mouse pointer drag when behavior is configured to ignore them'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Android)]
    public void DoesNotScrollWithMousePointerDragWhenBehaviorIsConfiguredToIgnoreThem(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester, enableMouseDrag: false);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Mouse);

            gesture.MoveBy(new Vector(0.0, -200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(0.0, GetScrollOffset(tester));

            gesture.MoveBy(new Vector(0.0, 200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(0.0, GetScrollOffset(tester));

            gesture.RemovePointer();
            tester.Pump();
        });
    }

    // Flutter: "Support updating 'ScrollBehavior.dragDevices' at runtime"
    [Fact]
    public void SupportUpdatingScrollBehaviorDragDevicesAtRuntime()
    {
        // Regression test for https://github.com/flutter/flutter/issues/111716
        Widget BuildFrame(IReadOnlySet<PointerDeviceKind>? dragDevices)
        {
            return new Directionality(
                TextDirection.Ltr,
                new ScrollConfiguration(
                    new NoScrollbarBehavior().CopyWith(dragDevices: dragDevices),
                    ListView.Builder(
                        itemCount: 1000,
                        itemBuilder: (_, index) => new Text($"Item {index}"))));
        }

        using var tester = NewTester();

        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(BuildFrame(new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse }));
        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, -100.0), PointerDeviceKind.Mouse);

        // Matching device should allow user scrolling.
        Assert.Equal(100.0, GetScrollOffset(tester));

        tester.PumpWidget(BuildFrame(new HashSet<PointerDeviceKind> { PointerDeviceKind.Stylus }));
        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, -100.0), PointerDeviceKind.Mouse);

        // Non-matching device should not allow user scrolling.
        Assert.Equal(100.0, GetScrollOffset(tester));

        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, -100.0), PointerDeviceKind.Stylus);

        // Matching device should allow user scrolling.
        Assert.Equal(200.0, GetScrollOffset(tester));
    }

    // Flutter: 'Does scroll with mouse pointer drag when behavior is not configured to ignore them'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Android)]
    public void DoesScrollWithMousePointerDragWhenBehaviorIsNotConfiguredToIgnoreThem(TargetPlatform platform)
    {
        WithPlatform(platform, () =>
        {
            using var tester = NewTester();
            using DartSemantics dartSemantics = new(tester);
            PumpTest(tester);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Scrollable>()),
                PointerDeviceKind.Mouse);

            gesture.MoveBy(new Vector(0.0, -200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(200.0, GetScrollOffset(tester));

            gesture.MoveBy(new Vector(0.0, 200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(0.0, GetScrollOffset(tester));

            gesture.RemovePointer();
            tester.Pump();
        });
    }

    // Flutter: 'Updated content dimensions correctly reflect in semantics'
    [Fact]
    public void UpdatedContentDimensionsCorrectlyReflectInSemantics()
    {
        // Regression test for https://github.com/flutter/flutter/issues/40419.
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        SemanticsHandle handle = EnsureSemantics(tester);
        var listView = new UniqueKey();
        Widget Build(bool enabled)
        {
            return new Directionality(
                TextDirection.Ltr,
                new TickerMode(
                    enabled: enabled,
                    child: ListView.Builder(
                        key: listView,
                        itemCount: 100,
                        itemBuilder: (_, index) => new Text($"Item {index}"))));
        }

        tester.PumpWidget(Build(true));

        SemanticsNode scrollableNode = GetSemantics(
            Descendants<RawGestureDetector>(tester.ElementsWithKey(listView).Single()).Single());
        SemanticsNode? syntheticScrollableNode = null;
        foreach (SemanticsNode node in scrollableNode.Children)
        {
            syntheticScrollableNode = node;
        }

        Assert.True(syntheticScrollableNode!.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling));
        // Disabled the ticker mode to trigger didChangeDependencies on Scrollable.
        // This can happen when a route is push or pop from top.
        // It will reconstruct the scroll position and apply content dimensions.
        tester.PumpWidget(Build(false));
        tester.Pump();
        // The correct workflow will be the following:
        // 1. _RenderScrollSemantics receives a new scroll position without content
        //    dimensions and creates a SemanticsNode without implicit scroll.
        // 2. The content dimensions are applied to the scroll position during the
        //    layout phase, and the scroll position marks the semantics node of
        //    _RenderScrollSemantics dirty.
        // 3. The _RenderScrollSemantics rebuilds its semantics node with implicit
        //    scroll.
        scrollableNode = GetSemantics(
            Descendants<RawGestureDetector>(tester.ElementsWithKey(listView).Single()).Single());
        syntheticScrollableNode = null;
        foreach (SemanticsNode node in scrollableNode.Children)
        {
            syntheticScrollableNode = node;
        }

        Assert.True(syntheticScrollableNode!.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling));
        handle.Dispose();
    }

    // Flutter: 'Two panel semantics is added to the sibling nodes of direct children'
    [Fact]
    public void TwoPanelSemanticsIsAddedToTheSiblingNodesOfDirectChildren()
    {
        using var focusNode = new FocusNode();
        using var controller = new TextEditingController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        SemanticsHandle handle = EnsureSemantics(tester);
        var key = new UniqueKey();

        // Use a WidgetsApp, since the text field requires an overlay.
        tester.PumpWidget(new TestWidgetsApp(
            home: new ListView(
                key: key,
                children:
                [
                    new Semantics(tagForChildren: new SemanticsTag("tag"), child: new Text("prefix")),
                    new TestTextField(autofocus: true, controller: controller, focusNode: focusNode),
                ])));
        // Wait for focus.
        tester.PumpAndSettle();

        SemanticsNode scrollableNode = GetSemantics(tester.ElementsWithKey(key).Single());
        SemanticsNode? intermediateNode = null;
        foreach (SemanticsNode node in scrollableNode.Children)
        {
            intermediateNode = node;
        }

        SemanticsNode? syntheticScrollableNode = null;
        foreach (SemanticsNode node in intermediateNode!.Children)
        {
            syntheticScrollableNode = node;
        }

        Assert.True(syntheticScrollableNode!.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling));

        int numberOfChild = 0;
        foreach (SemanticsNode node in syntheticScrollableNode!.Children)
        {
            Assert.True(node.IsTagged(RenderViewport.UseTwoPaneSemantics));
            numberOfChild += 1;
        }

        Assert.Equal(2, numberOfChild);

        handle.Dispose();
    }

    // Flutter: 'Scroll inertia cancel event'
    [Fact]
    public void ScrollInertiaCancelEvent()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        PumpTest(tester);
        tester.Fling(tester.ElementOfType<Scrollable>(), new Vector(0.0, -DragOffset), 1000.0);
        Assert.Equal(DragOffset, GetScrollOffset(tester));
        tester.Pump(); // trigger fling
        Assert.Equal(DragOffset, GetScrollOffset(tester));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Point center = tester.GetCenter(tester.ElementOfType<Scrollable>());
        tester.SendEventToBinding(new PointerHoverEvent(
            pointer: 1,
            kind: PointerDeviceKind.Mouse,
            position: center,
            timestampUtc: FrameworkDartTester.EventTimeOrigin,
            viewId: tester.View.ViewId,
            device: 1));
        tester.SendScrollInertiaCancel(center, PointerDeviceKind.Mouse); // Cancel partway through.
        tester.Pump();
        Assert.Equal(344.0642, GetScrollOffset(tester), 0.0001);
        tester.Pump(TimeSpan.FromMilliseconds(4800));
        Assert.Equal(344.0642, GetScrollOffset(tester), 0.0001);
    }

    // Flutter: 'Swapping viewports in a scrollable does not crash'
    [Fact]
    public void SwappingViewportsInAScrollableDoesNotCrash()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        SemanticsHandle semantics = EnsureSemantics(tester);
        var key = new LabeledGlobalKey<State>(null);
        var key1 = new LabeledGlobalKey<State>(null);
        var offsets = new List<ViewportOffset>();
        Widget BuildScrollable(bool withViewPort)
        {
            return new Directionality(
                TextDirection.Ltr,
                new Scrollable(
                    key: key,
                    viewportBuilder: (_, _) =>
                    {
                        if (withViewPort)
                        {
                            ViewportOffset offset = ViewportOffset.Zero();
                            offsets.Add(offset);
                            return new Viewport(
                                slivers:
                                [
                                    new SliverToBoxAdapter(
                                        new Semantics(key: key1, container: true, child: new Text("text1"))),
                                ],
                                offset: offset);
                        }

                        return new Semantics(key: key1, container: true, child: new Text("text1"));
                    }));
        }

        var twoPane = new HashSet<SemanticsTag> { RenderViewport.UseTwoPaneSemantics };
        try
        {
            // This should cache the inner node in Scrollable with the children text1.
            tester.PumpWidget(BuildScrollable(true));
            Assert.True(IncludesNodeWithTags(tester, twoPane));
            // This does not use two panel, this should clear cached inner node.
            tester.PumpWidget(BuildScrollable(false));
            Assert.False(IncludesNodeWithTags(tester, twoPane));
            // If the inner node was cleared in the previous step, this should not crash.
            tester.PumpWidget(BuildScrollable(true));
            Assert.True(IncludesNodeWithTags(tester, twoPane));
            Assert.Null(tester.TakeException());
        }
        finally
        {
            semantics.Dispose();
            foreach (ViewportOffset offset in offsets)
            {
                offset.Dispose();
            }
        }
    }

    // Flutter: 'deltaToScrollOrigin getter'
    [Fact]
    public void DeltaToScrollOriginGetter()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))])));
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Unknown);
        try
        {
            Assert.Equal(0.0, GetScrollOffset(tester));
            gesture.MoveBy(new Vector(0.0, -200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(200, GetScrollOffset(tester));
            ScrollableState scrollable = tester.State<ScrollableState>();
            Assert.Equal(new Point(0.0, 200), scrollable.DeltaToScrollOrigin);
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'resolvedPhysics getter'
    [Fact]
    public void ResolvedPhysicsGetter()
    {
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                physics: new AlwaysScrollableScrollPhysics(),
                slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))])));
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Unknown);
        try
        {
            Assert.Equal(0.0, GetScrollOffset(tester));
            gesture.MoveBy(new Vector(0.0, -200));
            tester.Pump();
            tester.PumpAndSettle();

            Assert.Equal(200, GetScrollOffset(tester));
            ScrollableState scrollable = tester.State<ScrollableState>();
            string Types(ScrollPhysics? value) => value!.Parent == null
                ? value.GetType().Name
                : $"{value.GetType().Name} {Types(value.Parent)}";

            Assert.Equal(
                "AlwaysScrollableScrollPhysics ClampingScrollPhysics RangeMaintainingScrollPhysics",
                Types(scrollable.ResolvedPhysics));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'dragDevices change updates widget'
    [Fact]
    public void DragDevicesChangeUpdatesWidget()
    {
        bool enable = false;
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Builder(_ => new StatefulBuilder((_, setState) => new Directionality(
            TextDirection.Ltr,
            new Column(
                children:
                [
                    new TestButton(
                        onPressed: () => setState(() => enable = !enable),
                        child: new Text("Toggle dragDevices")),
                    new Expanded(new Scrollable(
                        scrollBehavior: new NoScrollbarBehavior().CopyWith(
                            dragDevices: enable
                                ? new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse }
                                : new HashSet<PointerDeviceKind>()),
                        viewportBuilder: (_, position) => new Viewport(
                            offset: position,
                            slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))]))),
                ])))));

        // Gesture should not work.
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Mouse);
        Assert.Equal(0.0, GetScrollOffset(tester));
        gesture.MoveBy(new Vector(0.0, -200));
        tester.PumpAndSettle();
        Assert.Equal(0.0, GetScrollOffset(tester));

        // Change state to include mouse pointer device.
        tester.Tap(FindByText(tester, "Toggle dragDevices").Single());
        tester.Pump();
        gesture.Up();

        // Gesture should work after state change.
        gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Mouse);
        Assert.Equal(0.0, GetScrollOffset(tester));
        gesture.MoveBy(new Vector(0.0, -200));
        tester.PumpAndSettle();
        Assert.Equal(200, GetScrollOffset(tester));
        gesture.Up();
    }

    // Flutter: 'dragDevices change updates widget when oldWidget scrollBehavior is null'
    [Fact]
    public void DragDevicesChangeUpdatesWidgetWhenOldWidgetScrollBehaviorIsNull()
    {
        ScrollBehavior? scrollBehavior = null;
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Builder(_ => new StatefulBuilder((_, setState) => new Directionality(
            TextDirection.Ltr,
            new Column(
                children:
                [
                    new TestButton(
                        onPressed: () => setState(() => scrollBehavior = new NoScrollbarBehavior().CopyWith(
                            dragDevices: new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse })),
                        child: new Text("Update ScrollBehavior")),
                    new Expanded(new Scrollable(
                        physics: new ScrollPhysics(),
                        scrollBehavior: scrollBehavior,
                        viewportBuilder: (_, position) => new Viewport(
                            offset: position,
                            slivers: [new SliverToBoxAdapter(new SizedBox(height: 2000.0))]))),
                ])))));

        // Gesture should not work.
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Mouse);
        Assert.Equal(0.0, GetScrollOffset(tester));
        gesture.MoveBy(new Vector(0.0, -200));
        tester.PumpAndSettle();
        Assert.Equal(0.0, GetScrollOffset(tester));
        gesture.Up();

        // Change state to include mouse pointer device.
        tester.Tap(FindByText(tester, "Update ScrollBehavior").Single());
        tester.Pump();

        // Gesture should work after state change.
        gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<Scrollable>()),
            PointerDeviceKind.Mouse);
        Assert.Equal(0.0, GetScrollOffset(tester));
        gesture.MoveBy(new Vector(0.0, -200));
        tester.PumpAndSettle();
        Assert.Equal(200, GetScrollOffset(tester));
        gesture.Up();
    }

    // Flutter: 'Diagonal scroll in nested scrollables: horizontal handles, vertical at edge should NOT
    // call respond(true)'
    [Fact]
    public void DiagonalScrollInNestedScrollablesHorizontalHandlesVerticalAtEdgeShouldNotCallRespondTrue()
    {
        // Regression test for https://github.com/flutter/flutter/issues/152588
        using var verticalController = new ScrollController();
        using var horizontalController = new ScrollController();
        using var tester = NewTester();
        using DartSemantics dartSemantics = new(tester);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new ListView(
                controller: verticalController,
                children:
                [
                    new SizedBox(
                        height: 200,
                        child: new ListView(
                            controller: horizontalController,
                            scrollDirection: Axis.Horizontal,
                            children:
                            [
                                new Container(width: 1000, height: 200, color: new Color(0xFFFF0000)),
                            ])),
                    new Container(height: 1000, color: new Color(0xFF0000FF)),
                ])));

        Assert.Equal(0.0, verticalController.Offset);

        Point location = tester.GetCenter(tester.ElementsOfType<ListView>()[^1]);

        // Simulate diagonal scroll: dx: 20, dy: -20 (trying to scroll up/back vertically, which is at edge)
        List<bool> onRespondCalls = tester.SendPointerScroll(location, new Vector(20.0, -20.0));

        Assert.Equal(20.0, horizontalController.Offset);
        Assert.Equal(0.0, verticalController.Offset);

        // ONLY the horizontal child should have handled the event and called respond(false). Vertical
        // parent should NOT have called respond(true) because the event was handled by the child.
        Assert.Equal([false], onRespondCalls);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers (scrollable_test.dart top level, plus the flutter_test pieces the shared harness lacks).

    private static FrameworkDartTester NewTester() => new(fakeGestureTimers: true, devicePixelRatio: 3.0);

    private static void WithPlatform(TargetPlatform platform, Action body)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            body();
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    // scrollable_test.dart: pumpTest. The Dart `platform` argument is unused there, so it is omitted.
    private static void PumpTest(
        FrameworkDartTester tester,
        bool scrollable = true,
        bool reverse = false,
        IReadOnlySet<LogicalKeyboardKey>? axisModifier = null,
        Axis scrollDirection = Axis.Vertical,
        ScrollController? controller = null,
        bool enableMouseDrag = true)
    {
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new ScrollConfiguration(
                new NoScrollbarBehavior().CopyWith(
                    dragDevices: enableMouseDrag ? Enum.GetValues<PointerDeviceKind>().ToHashSet() : null,
                    pointerAxisModifiers: axisModifier),
                new CustomScrollView(
                    controller: controller,
                    reverse: reverse,
                    scrollDirection: scrollDirection,
                    physics: scrollable ? null : new NeverScrollableScrollPhysics(),
                    slivers:
                    [
                        new SliverToBoxAdapter(new SizedBox(
                            height: scrollDirection == Axis.Vertical ? 2000.0 : null,
                            width: scrollDirection == Axis.Horizontal ? 2000.0 : null)),
                    ]))));
    }

    // scrollable_test.dart: getScrollOffset.
    private static double GetScrollOffset(FrameworkDartTester tester, bool last = true)
    {
        IReadOnlyList<Element> viewports = tester.ElementsOfType<Viewport>();
        Element viewport = last ? viewports[^1] : viewports.Single();
        return ((RenderViewport)viewport.FindRenderObject()!).Offset.Pixels;
    }

    // scrollable_test.dart: getScrollVelocity.
    private static double GetScrollVelocity(FrameworkDartTester tester)
    {
        var viewport = (RenderViewport)tester.ElementOfType<Viewport>().FindRenderObject()!;
        var position = (ScrollPosition)viewport.Offset;
        return position.Activity.Velocity;
    }

    // flutter_test: moreOrLessEquals (default epsilon 1e-10).
    private static void AssertMoreOrLessEquals(double expected, double actual, double epsilon = 1e-10)
    {
        Assert.True(Math.Abs(expected - actual) <= epsilon, $"Expected {expected} (+/- {epsilon}), got {actual}.");
    }

    // flutter_test: WidgetTester.pumpAndSettle(duration).
    private static int PumpAndSettle(FrameworkDartTester tester, TimeSpan duration)
    {
        int count = 0;
        do
        {
            if (count > 1000)
            {
                throw new Xunit.Sdk.XunitException("pumpAndSettle timed out");
            }

            tester.Pump(duration);
            count += 1;
        }
        while (Scheduler.HasScheduledFrame || Scheduler.TransientCallbackCount > 0);

        return count;
    }

    // flutter_test: WidgetTester.tapAt.
    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    // flutter_test: WidgetTester.sendKeyDownEvent / sendKeyUpEvent.
    private static void SendKeyDown(LogicalKeyboardKey key, bool shift = false, bool alt = false)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(key, shift: shift, alt: alt));
    }

    private static void SendKeyUp(LogicalKeyboardKey key)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(key));
    }

    // flutter_test: find.descendant(of: element, matching: find.byType(T)).
    private static List<Element> Descendants<TWidget>(Element of) where TWidget : Widget
    {
        var result = new List<Element>();
        void Visit(Element element)
        {
            if (element.Widget.GetType() == typeof(TWidget))
            {
                result.Add(element);
            }

            element.VisitChildren(Visit);
        }

        of.VisitChildren(Visit);
        return result;
    }

    // flutter_test finders skip offstage elements by default.
    private static List<Element> OnstageElements(FrameworkDartTester tester)
    {
        var result = new List<Element>();
        void Visit(Element element)
        {
            result.Add(element);
            element.DebugVisitOnstageChildren(Visit);
        }

        tester.Root.DebugVisitOnstageChildren(Visit);
        return result;
    }

    // flutter_test: find.byKey(key).
    private static List<Element> FindByKey(FrameworkDartTester tester, Key key)
        => OnstageElements(tester).Where(element => Equals(element.Widget.Key, key)).ToList();

    // flutter_test: find.text(text).
    private static List<Element> FindByText(FrameworkDartTester tester, string text)
        => OnstageElements(tester).Where(element => element.Widget is Text { Data: var data } && data == text).ToList();

    // flutter_test: find.byType(T).evaluate().first.
    private static Element FirstOfType<TWidget>(FrameworkDartTester tester) where TWidget : Widget
        => OnstageElements(tester).First(element => element.Widget.GetType() == typeof(TWidget));

    // flutter_test: tester.ensureSemantics(). DartSemantics already flushes every frame.
    private static SemanticsHandle EnsureSemantics(FrameworkDartTester tester)
    {
        return tester.RenderView.Owner!.EnsureSemantics();
    }

    // flutter_test: WidgetController.getSemantics.
    private static SemanticsNode GetSemantics(Element element)
    {
        RenderObject? renderObject = element.FindRenderObject();
        SemanticsNode? result = renderObject?.SemanticsNode;
        while (renderObject is not null && (result is null || result.IsMergedIntoParent))
        {
            renderObject = renderObject.Parent;
            result = renderObject?.SemanticsNode;
        }

        return result ?? throw new InvalidOperationException("No semantics node found.");
    }

    // semantics_tester.dart: includesNodeWith(tags: ...).
    private static bool IncludesNodeWithTags(FrameworkDartTester tester, IReadOnlySet<SemanticsTag> tags)
    {
        SemanticsNode? root = tester.RenderView.Owner!.SemanticsOwner!.RootNode;
        bool Visit(SemanticsNode node)
        {
            IReadOnlySet<SemanticsTag>? actual = node.GetSemanticsData().Tags;
            if (actual is not null && actual.SetEquals(tags))
            {
                return true;
            }

            return node.Children.Any(Visit);
        }

        return root is not null && Visit(root);
    }

    /// <summary>
    /// flutter_test's default <c>testWidgets(semanticsEnabled: true)</c>: keeps a semantics handle open on
    /// the view's pipeline owner and flushes its semantics at the end of every frame, the way
    /// <c>drawFrame</c> does. The shared harness frame does not flush semantics, so this re-arms a
    /// post-frame callback each frame. Frame scheduling matches Dart: a semantics update requested while
    /// the scheduler is idle (for example from a ScrollMetricsNotification microtask) schedules a frame.
    /// </summary>
    private sealed class DartSemantics : IDisposable
    {
        private readonly FrameworkDartTester _tester;
        private readonly SemanticsHandle _handle;
        private bool _disposed;

        public DartSemantics(FrameworkDartTester tester)
        {
            _tester = tester;
            _handle = tester.RenderView.Owner!.EnsureSemantics();
            Arm();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _handle.Dispose();
        }

        private void Arm()
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (_disposed)
                {
                    return;
                }

                _tester.RenderView.Owner?.FlushSemantics();
                Arm();
            });
        }
    }

    private sealed class NoScrollbarBehavior : ScrollBehavior
    {
        public override Widget BuildScrollbar(BuildContext context, Widget child, ScrollableDetails details) => child;
    }

    // ignore: must_be_immutable
    private sealed class SuperPessimisticScrollPhysics(ScrollPhysics? parent = null) : ScrollPhysics(parent)
    {
        public int Count { get; private set; }

        public override bool RecommendDeferredLoading(double velocity, IScrollMetrics metrics, BuildContext context)
        {
            Count++;
            return velocity > 1;
        }

        public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
            => new SuperPessimisticScrollPhysics(BuildParent(ancestor));
    }

    private sealed class ExtraSuperPessimisticScrollPhysics(ScrollPhysics? parent = null) : ScrollPhysics(parent)
    {
        public override bool RecommendDeferredLoading(double velocity, IScrollMetrics metrics, BuildContext context)
            => true;

        public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
            => new ExtraSuperPessimisticScrollPhysics(BuildParent(ancestor));
    }

    // button_tester.dart: TestButton.
    private sealed class TestButton(Widget child, Action? onPressed = null) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new Semantics(
                label: "button",
                button: true,
                enabled: onPressed != null,
                onTap: onPressed,
                focusable: true,
                child: new FocusableActionDetector(
                    enabled: onPressed != null,
                    child: new GestureDetector(onTap: onPressed, child: child)));
        }
    }

    // editable_text_tester.dart: TestTextField. Plumix has no TextSelectionGestureDetectorBuilder; the
    // Dart builder's detector excludes itself from semantics, so the semantics tree is unaffected.
    private sealed class TestTextField(
        bool autofocus,
        TextEditingController controller,
        FocusNode focusNode) : StatelessWidget
    {
        private static readonly Color Red = new Color(0xFFF44336);

        public override Widget Build(BuildContext context)
        {
            return new Semantics(
                enabled: true,
                onTap: () => focusNode.RequestFocus(),
                child: new EditableText(
                    controller,
                    focusNode: focusNode,
                    autofocus: autofocus,
                    cursorColor: Red,
                    maxLines: 1,
                    rendererIgnoresPointer: true,
                    style: new TextStyle()));
        }
    }
}
