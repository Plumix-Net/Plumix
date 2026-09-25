using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports the TwoDimensionalScrollable tests of
// flutter/packages/flutter/test/widgets/two_dimensional_viewport_test.dart and two_dimensional_scroll_view_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TwoDimensionalScrollableDartParityTests
{
    private const string ViewportTest = "two_dimensional_viewport_test.dart";
    private const string ScrollViewTest = "two_dimensional_scroll_view_test.dart";

    private static readonly Color Amber100 = new Color(0xFFFFF8E1);
    private static readonly Color BlueAccent100 = new Color(0xFF82B1FF);

    // ------------------------------------------------------------------ two_dimensional_viewport_test.dart

    // Flutter: 'two_dimensional_viewport_test.dart: .of, .maybeOf'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void OfMaybeOf(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            BuildContext? capturedContext = null;
            var @delegate = new TwoDimensionalChildBuilderDelegate(
                (context, _) =>
                {
                    capturedContext = context;
                    return SizedBox.Square(dimension: 200);
                },
                maxXIndex: 0,
                maxYIndex: 0);

            tester.PumpWidget(SimpleBuilderTest(@delegate: @delegate));
            tester.PumpAndSettle();

            Assert.NotNull(TwoDimensionalScrollable.Of(capturedContext!));
            Assert.NotNull(TwoDimensionalScrollable.MaybeOf(capturedContext!));

            tester.PumpWidget(new Builder(context =>
            {
                capturedContext = context;
                TwoDimensionalScrollable.Of(context);
                return new Container();
            }));
            tester.PumpAndSettle();
            object? exception = tester.TakeException();
            FlutterError error = Assert.IsAssignableFrom<FlutterError>(exception);
            Assert.Contains(
                "TwoDimensionalScrollable.of() was called with a context that does "
                + "not contain a TwoDimensionalScrollable widget.",
                error.ToString(),
                StringComparison.Ordinal);

            Assert.Null(TwoDimensionalScrollable.MaybeOf(capturedContext!));
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: horizontal and vertical getters'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void HorizontalAndVerticalGetters(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            BuildContext? capturedContext = null;
            var @delegate = new TwoDimensionalChildBuilderDelegate(
                (context, _) =>
                {
                    capturedContext = context;
                    return SizedBox.Square(dimension: 200);
                },
                maxXIndex: 0,
                maxYIndex: 0);

            tester.PumpWidget(SimpleBuilderTest(@delegate: @delegate));
            tester.PumpAndSettle();

            TwoDimensionalScrollableState scrollable = TwoDimensionalScrollable.Of(capturedContext!);
            Assert.Equal(0.0, scrollable.VerticalScrollable.Position.Pixels);
            Assert.Equal(0.0, scrollable.HorizontalScrollable.Position.Pixels);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: creates fallback ScrollControllers if not provided by
    // ScrollableDetails'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void CreatesFallbackScrollControllersIfNotProvidedByScrollableDetails(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            BuildContext? capturedContext = null;
            var @delegate = new TwoDimensionalChildBuilderDelegate(
                (context, _) =>
                {
                    capturedContext = context;
                    return SizedBox.Square(dimension: 200);
                },
                maxXIndex: 0,
                maxYIndex: 0);

            tester.PumpWidget(SimpleBuilderTest(@delegate: @delegate));
            tester.PumpAndSettle();

            // Vertical
            ScrollableState vertical = Scrollable.Of(capturedContext!, axis: Axis.Vertical);
            Assert.NotNull(vertical.Widget.Controller);
            // Horizontal
            ScrollableState horizontal = Scrollable.Of(capturedContext!, axis: Axis.Horizontal);
            Assert.NotNull(horizontal.Widget.Controller);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: asserts the axis directions do not conflict with one
    // another'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void TwoDimensionalScrollable_AssertsTheAxisDirectionsDoNotConflictWithOneAnother(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            var exceptions = new List<object>();
            FlutterExceptionHandler? oldHandler = FlutterError.OnError;
            FlutterError.OnError = details => exceptions.Add(details.Exception);
            try
            {
                // Horizontal mismatch
                tester.PumpWidget(new TwoDimensionalScrollable(
                    horizontalDetails: ScrollableDetails.Horizontal(),
                    verticalDetails: ScrollableDetails.Horizontal(),
                    viewportBuilder: (_, _, _) => new Container()));

                // Vertical mismatch
                tester.PumpWidget(new TwoDimensionalScrollable(
                    horizontalDetails: ScrollableDetails.Vertical(),
                    verticalDetails: ScrollableDetails.Vertical(),
                    viewportBuilder: (_, _, _) => new Container()));

                // Both
                tester.PumpWidget(new TwoDimensionalScrollable(
                    horizontalDetails: ScrollableDetails.Vertical(),
                    verticalDetails: ScrollableDetails.Horizontal(),
                    viewportBuilder: (_, _, _) => new Container()));

                Assert.Equal(3, exceptions.Count);
                foreach (object exception in exceptions)
                {
                    AssertionError assertion = Assert.IsAssignableFrom<AssertionError>(exception);
                    Assert.Contains("are not Axis", assertion.Message, StringComparison.Ordinal);
                }
            }
            finally
            {
                FlutterError.OnError = oldHandler;
            }
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: correctly sets restorationIds'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void CorrectlySetsRestorationIds(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            BuildContext? capturedContext = null;
            Widget ViewportBuilder(BuildContext context, ViewportOffset vertical, ViewportOffset horizontal)
            {
                return SizedBox.Square(
                    dimension: 200,
                    child: new Builder(builderContext =>
                    {
                        capturedContext = builderContext;
                        return new Container();
                    }));
            }

            // with restorationID set
            tester.PumpWidget(new WidgetsApp(
                color: new Color(0xFFFFFFFF),
                restorationScopeId: "Test ID",
                builder: (_, _) => new TwoDimensionalScrollable(
                    restorationId: "Custom Restoration ID",
                    horizontalDetails: ScrollableDetails.Horizontal(),
                    verticalDetails: ScrollableDetails.Vertical(),
                    viewportBuilder: ViewportBuilder)));
            tester.PumpAndSettle();

            Assert.Equal("Custom Restoration ID", RestorationScope.Of(capturedContext!).RestorationId);
            Assert.Equal(
                "OuterVerticalTwoDimensionalScrollable",
                Scrollable.Of(capturedContext!, axis: Axis.Vertical).Widget.RestorationId);
            Assert.Equal(
                "InnerHorizontalTwoDimensionalScrollable",
                Scrollable.Of(capturedContext!, axis: Axis.Horizontal).Widget.RestorationId);

            // default restorationID
            tester.PumpWidget(new TwoDimensionalScrollable(
                horizontalDetails: ScrollableDetails.Horizontal(),
                verticalDetails: ScrollableDetails.Vertical(),
                viewportBuilder: ViewportBuilder));
            tester.PumpAndSettle();

            Assert.Null(RestorationScope.MaybeOf(capturedContext!));
            Assert.Equal(
                "OuterVerticalTwoDimensionalScrollable",
                Scrollable.Of(capturedContext!, axis: Axis.Vertical).Widget.RestorationId);
            Assert.Equal(
                "InnerHorizontalTwoDimensionalScrollable",
                Scrollable.Of(capturedContext!, axis: Axis.Horizontal).Widget.RestorationId);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: Restoration works'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void RestorationWorks(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            Widget app = new WidgetsApp(
                color: new Color(0xFFFFFFFF),
                restorationScopeId: "Test ID",
                builder: (_, _) => new TwoDimensionalScrollable(
                    restorationId: "Custom Restoration ID",
                    horizontalDetails: ScrollableDetails.Horizontal(),
                    verticalDetails: ScrollableDetails.Vertical(),
                    viewportBuilder: (_, verticalPosition, horizontalPosition) => new SimpleBuilderTableViewport(
                        verticalOffset: verticalPosition,
                        verticalAxisDirection: AxisDirection.Down,
                        horizontalOffset: horizontalPosition,
                        horizontalAxisDirection: AxisDirection.Right,
                        @delegate: BuilderDelegate(),
                        mainAxis: Axis.Vertical)));
            tester.PumpWidget(app);
            tester.PumpAndSettle();

            RestoreScrollAndVerify(tester, app);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: Inner Scrollables receive the correct details from
    // TwoDimensionalScrollable'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void InnerScrollablesReceiveTheCorrectDetailsFromTwoDimensionalScrollable(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            // Default
            BuildContext? capturedContext = null;
            Widget ViewportBuilder(BuildContext context, ViewportOffset vertical, ViewportOffset horizontal)
            {
                return SizedBox.Square(
                    dimension: 200,
                    child: new Builder(builderContext =>
                    {
                        capturedContext = builderContext;
                        return new Container();
                    }));
            }

            tester.PumpWidget(new TwoDimensionalScrollable(
                horizontalDetails: ScrollableDetails.Horizontal(),
                verticalDetails: ScrollableDetails.Vertical(),
                viewportBuilder: ViewportBuilder));
            tester.PumpAndSettle();

            // Vertical
            ScrollableState vertical = Scrollable.Of(capturedContext!, axis: Axis.Vertical);
            Assert.NotNull(vertical.Widget.Key);
            Assert.Equal(AxisDirection.Down, vertical.Widget.AxisDirection);
            Assert.NotNull(vertical.Widget.Controller);
            Assert.Null(vertical.Widget.Physics);
            Assert.Equal(Clip.HardEdge, vertical.Widget.ClipBehavior);
            Assert.Null(vertical.Widget.IncrementCalculator);
            Assert.False(vertical.Widget.ExcludeFromSemantics);
            Assert.Equal("OuterVerticalTwoDimensionalScrollable", vertical.Widget.RestorationId);
            Assert.Equal(DragStartBehavior.Start, vertical.Widget.DragStartBehavior);

            // Horizontal
            ScrollableState horizontal = Scrollable.Of(capturedContext!, axis: Axis.Horizontal);
            Assert.NotNull(horizontal.Widget.Key);
            Assert.Equal(AxisDirection.Right, horizontal.Widget.AxisDirection);
            Assert.NotNull(horizontal.Widget.Controller);
            Assert.Null(horizontal.Widget.Physics);
            Assert.Equal(Clip.HardEdge, horizontal.Widget.ClipBehavior);
            Assert.Null(horizontal.Widget.IncrementCalculator);
            Assert.False(horizontal.Widget.ExcludeFromSemantics);
            Assert.Equal("InnerHorizontalTwoDimensionalScrollable", horizontal.Widget.RestorationId);
            Assert.Equal(DragStartBehavior.Start, horizontal.Widget.DragStartBehavior);

            // Customized
            var horizontalController = new ScrollController();
            var verticalController = new ScrollController();
            // Dart's `const ClampingScrollPhysics()`/`const AlwaysScrollableScrollPhysics()` are
            // canonicalized, so its equality check is an identity check against the passed instance.
            var clampingPhysics = new ClampingScrollPhysics();
            var alwaysScrollablePhysics = new AlwaysScrollableScrollPhysics();
            double Calculator(ScrollIncrementDetails _) => 0.0;
            tester.PumpWidget(new TwoDimensionalScrollable(
                incrementCalculator: Calculator,
                excludeFromSemantics: true,
                dragStartBehavior: DragStartBehavior.Down,
                horizontalDetails: ScrollableDetails.Horizontal(
                    reverse: true,
                    controller: horizontalController,
                    physics: clampingPhysics,
                    decorationClipBehavior: Clip.AntiAlias),
                verticalDetails: ScrollableDetails.Vertical(
                    reverse: true,
                    controller: verticalController,
                    physics: alwaysScrollablePhysics,
                    decorationClipBehavior: Clip.AntiAliasWithSaveLayer),
                viewportBuilder: ViewportBuilder));
            tester.PumpAndSettle();

            // Vertical
            vertical = Scrollable.Of(capturedContext!, axis: Axis.Vertical);
            Assert.NotNull(vertical.Widget.Key);
            Assert.Equal(AxisDirection.Up, vertical.Widget.AxisDirection);
            Assert.Same(verticalController, vertical.Widget.Controller);
            Assert.IsType<AlwaysScrollableScrollPhysics>(vertical.Widget.Physics);
            Assert.Same(alwaysScrollablePhysics, vertical.Widget.Physics);
            Assert.Equal(Clip.AntiAliasWithSaveLayer, vertical.Widget.ClipBehavior);
            Assert.Equal(
                0.0,
                vertical.Widget.IncrementCalculator!(
                    new ScrollIncrementDetails(ScrollIncrementType.Line, verticalController.Position)));
            Assert.True(vertical.Widget.ExcludeFromSemantics);
            Assert.Equal("OuterVerticalTwoDimensionalScrollable", vertical.Widget.RestorationId);
            Assert.Equal(DragStartBehavior.Down, vertical.Widget.DragStartBehavior);

            // Horizontal
            horizontal = Scrollable.Of(capturedContext!, axis: Axis.Horizontal);
            Assert.NotNull(horizontal.Widget.Key);
            Assert.Equal(AxisDirection.Left, horizontal.Widget.AxisDirection);
            Assert.Same(horizontalController, horizontal.Widget.Controller);
            Assert.IsType<ClampingScrollPhysics>(horizontal.Widget.Physics);
            Assert.Same(clampingPhysics, horizontal.Widget.Physics);
            Assert.Equal(Clip.AntiAlias, horizontal.Widget.ClipBehavior);
            Assert.Equal(
                0.0,
                horizontal.Widget.IncrementCalculator!(
                    new ScrollIncrementDetails(ScrollIncrementType.Line, horizontalController.Position)));
            Assert.True(horizontal.Widget.ExcludeFromSemantics);
            Assert.Equal("InnerHorizontalTwoDimensionalScrollable", horizontal.Widget.RestorationId);
            Assert.Equal(DragStartBehavior.Down, horizontal.Widget.DragStartBehavior);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: none (default)'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void DiagonalDragBehavior_NoneDefault(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            // Vertical and horizontal axes are locked.
            var verticalController = new ScrollController();
            var horizontalController = new ScrollController();
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                SimpleBuilderTest(
                    verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                    horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController))));
            tester.PumpAndSettle();
            Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();

            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            tester.Drag(FindScrollable(), new Vector(0.0, -100.0));
            tester.PumpAndSettle();
            Assert.Equal(80.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            tester.Drag(FindScrollable(), new Vector(-100.0, 0.0));
            tester.PumpAndSettle();
            Assert.Equal(80.0, verticalController.Position.Pixels);
            Assert.Equal(80.0, horizontalController.Position.Pixels);
            // Drag with and x and y offset, only vertical will accept the gesture
            // since the x is < kTouchSlop
            tester.Drag(FindScrollable(), new Vector(-10.0, -50.0));
            tester.PumpAndSettle();
            Assert.Equal(110.0, verticalController.Position.Pixels);
            Assert.Equal(80.0, horizontalController.Position.Pixels);
            // Drag with and x and y offset, only horizontal will accept the gesture
            // since the y is < kTouchSlop
            tester.Drag(FindScrollable(), new Vector(-50.0, -10.0));
            tester.PumpAndSettle();
            Assert.Equal(110.0, verticalController.Position.Pixels);
            Assert.Equal(110.0, horizontalController.Position.Pixels);
            // Drag with and x and y offset, only vertical will accept the gesture
            //  x is > kTouchSlop, larger offset wins
            tester.Drag(FindScrollable(), new Vector(-20.0, -50.0));
            tester.PumpAndSettle();
            Assert.Equal(140.0, verticalController.Position.Pixels);
            Assert.Equal(110.0, horizontalController.Position.Pixels);
            // Drag with and x and y offset, only horizontal will accept the gesture
            //  y is > kTouchSlop, larger offset wins
            tester.Drag(FindScrollable(), new Vector(-50.0, -20.0));
            tester.PumpAndSettle();
            Assert.Equal(140.0, verticalController.Position.Pixels);
            Assert.Equal(140.0, horizontalController.Position.Pixels);
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: weightedEvent'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void DiagonalDragBehavior_WeightedEvent(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            // For weighted event, the winning axis is locked for the duration of
            // the gesture.
            var verticalController = new ScrollController();
            var horizontalController = new ScrollController();
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                SimpleBuilderTest(
                    diagonalDrag: DiagonalDragBehavior.WeightedEvent,
                    verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                    horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController))));
            tester.PumpAndSettle();
            Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();

            // Locks to vertical axis - simple.
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            TestGesture gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
            // In this case, the vertical axis clearly wins.
            Point secondLocation = tester.GetCenter(FindScrollable()) + new Vector(0.0, -50.0);
            gesture.MoveTo(secondLocation);
            tester.PumpAndSettle();
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move with horizontal diff
            Point thirdLocation = secondLocation + new Vector(-30, -15);
            gesture.MoveTo(thirdLocation);
            tester.PumpAndSettle();
            // Only vertical diff applied
            Assert.Equal(65.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            gesture.Up();
            tester.PumpAndSettle();

            // Lock to vertical axis - scrolls diagonally until certain
            verticalController.JumpTo(0.0);
            horizontalController.JumpTo(0.0);
            tester.Pump();
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
            // In this case, the no one clearly wins, so it moves diagonally.
            secondLocation = tester.GetCenter(FindScrollable()) + new Vector(-50.0, -50.0);
            gesture.MoveTo(secondLocation);
            tester.PumpAndSettle();
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move clearly indicating vertical
            thirdLocation = secondLocation + new Vector(-20, -50);
            gesture.MoveTo(thirdLocation);
            tester.PumpAndSettle();
            // Only vertical diff applied
            Assert.Equal(100.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, and vertical axis has won for the gesture
            // continue only vertical scrolling.
            Point fourthLocation = thirdLocation + new Vector(-30, -30);
            gesture.MoveTo(fourthLocation);
            tester.PumpAndSettle();
            // Only vertical diff applied
            Assert.Equal(130.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            gesture.Up();
            tester.PumpAndSettle();

            // Locks to horizontal axis - simple.
            verticalController.JumpTo(0.0);
            horizontalController.JumpTo(0.0);
            tester.Pump();
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
            // In this case, the horizontal axis clearly wins.
            secondLocation = tester.GetCenter(FindScrollable()) + new Vector(-50.0, 0.0);
            gesture.MoveTo(secondLocation);
            tester.PumpAndSettle();
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move with vertical diff
            thirdLocation = secondLocation + new Vector(-15, -30);
            gesture.MoveTo(thirdLocation);
            tester.PumpAndSettle();
            // Only vertical diff applied
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(65.0, horizontalController.Position.Pixels);
            gesture.Up();
            tester.PumpAndSettle();

            // Lock to horizontal axis - scrolls diagonally until certain
            verticalController.JumpTo(0.0);
            horizontalController.JumpTo(0.0);
            tester.Pump();
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
            // In this case, the no one clearly wins, so it moves diagonally.
            secondLocation = tester.GetCenter(FindScrollable()) + new Vector(-50.0, -50.0);
            gesture.MoveTo(secondLocation);
            tester.PumpAndSettle();
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move clearly indicating horizontal
            thirdLocation = secondLocation + new Vector(-50, -20);
            gesture.MoveTo(thirdLocation);
            tester.PumpAndSettle();
            // Only horizontal diff applied
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(100.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, and horizontal axis has won for the gesture
            // continue only horizontal scrolling.
            fourthLocation = thirdLocation + new Vector(-30, -30);
            gesture.MoveTo(fourthLocation);
            tester.PumpAndSettle();
            // Only horizontal diff applied
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(130.0, horizontalController.Position.Pixels);
            gesture.Up();
            tester.PumpAndSettle();
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: weightedContinuous'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void DiagonalDragBehavior_WeightedContinuous(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            // For weighted continuous, the winning axis can change if the axis
            // differential for the gesture exceeds kTouchSlop. So it can lock, and
            // remain locked, if the user maintains a generally straight gesture,
            // otherwise it will unlock and re-evaluate.
            var verticalController = new ScrollController();
            var horizontalController = new ScrollController();
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                SimpleBuilderTest(
                    diagonalDrag: DiagonalDragBehavior.WeightedContinuous,
                    verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                    horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController))));
            tester.PumpAndSettle();
            Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();

            // Locks to vertical, and then unlocks, resets to horizontal, then
            // unlocks and scrolls diagonally.
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            TestGesture gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
            // In this case, the vertical axis clearly wins.
            Point secondLocation = tester.GetCenter(FindScrollable()) + new Vector(0.0, -50.0);
            gesture.MoveTo(secondLocation);
            tester.PumpAndSettle();
            Assert.Equal(50.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move with horizontal diff, but still
            // dominant vertical
            Point thirdLocation = secondLocation + new Vector(-15, -50);
            gesture.MoveTo(thirdLocation);
            tester.PumpAndSettle();
            // Only vertical diff applied since kTouchSlop was not exceeded in the
            // horizontal axis from one drag event to the next.
            Assert.Equal(100.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move with unlocking horizontal diff
            Point fourthLocation = thirdLocation + new Vector(-50, -15);
            gesture.MoveTo(fourthLocation);
            tester.PumpAndSettle();
            // Only horizontal diff applied
            Assert.Equal(100.0, verticalController.Position.Pixels);
            Assert.Equal(50.0, horizontalController.Position.Pixels);
            // Gesture has not ended yet, move with unlocking diff that results in
            // diagonal move since neither wins.
            Point fifthLocation = fourthLocation + new Vector(-50, -50);
            gesture.MoveTo(fifthLocation);
            tester.PumpAndSettle();
            // Only horizontal diff applied
            Assert.Equal(150.0, verticalController.Position.Pixels);
            Assert.Equal(100.0, horizontalController.Position.Pixels);
            gesture.Up();
            tester.PumpAndSettle();
        });
    }

    // Flutter: 'two_dimensional_viewport_test.dart: free'
    [Fact]
    public void DiagonalDragBehavior_Free()
    {
        using FrameworkDartTester tester = CreateTester();
        // For free, anything goes.
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            SimpleBuilderTest(
                diagonalDrag: DiagonalDragBehavior.Free,
                verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController))));
        tester.PumpAndSettle();
        Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();

        // Nothing locks.
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        TestGesture gesture = tester.StartGesture(tester.GetCenter(FindScrollable()), PointerDeviceKind.Touch);
        Point secondLocation = tester.GetCenter(FindScrollable()) + new Vector(0.0, -50.0);
        gesture.MoveTo(secondLocation);
        tester.PumpAndSettle();
        Assert.Equal(50.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Point thirdLocation = secondLocation + new Vector(-15, -50);
        gesture.MoveTo(thirdLocation);
        tester.PumpAndSettle();
        Assert.Equal(100.0, verticalController.Position.Pixels);
        Assert.Equal(15.0, horizontalController.Position.Pixels);
        Point fourthLocation = thirdLocation + new Vector(-50, -15);
        gesture.MoveTo(fourthLocation);
        tester.PumpAndSettle();
        Assert.Equal(115.0, verticalController.Position.Pixels);
        Assert.Equal(65.0, horizontalController.Position.Pixels);
        Point fifthLocation = fourthLocation + new Vector(-50, -50);
        gesture.MoveTo(fifthLocation);
        tester.PumpAndSettle();
        Assert.Equal(165.0, verticalController.Position.Pixels);
        Assert.Equal(115.0, horizontalController.Position.Pixels);
        gesture.Up();
        tester.PumpAndSettle();
    }

    // ------------------------------------------------------------------ two_dimensional_scroll_view_test.dart

    // Flutter: 'two_dimensional_scroll_view_test.dart: asserts the axis directions do not conflict with one
    // another'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void TwoDimensionalScrollView_AssertsTheAxisDirectionsDoNotConflictWithOneAnother(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            var exceptions = new List<object>();
            FlutterExceptionHandler? oldHandler = FlutterError.OnError;
            FlutterError.OnError = details => exceptions.Add(details.Exception);
            try
            {
                // Horizontal wrong
                tester.PumpWidget(new TestWidgetsApp(
                    home: new SimpleBuilderTableView(
                        new TwoDimensionalChildBuilderDelegate((_, _) => null),
                        horizontalDetails: ScrollableDetails.Vertical())));
                // Horizontal has default const ScrollableDetails.horizontal()

                // Vertical wrong
                tester.PumpWidget(new TestWidgetsApp(
                    home: new SimpleBuilderTableView(
                        new TwoDimensionalChildBuilderDelegate((_, _) => null),
                        verticalDetails: ScrollableDetails.Horizontal())));
                // Horizontal has default const ScrollableDetails.horizontal()

                // Both wrong
                tester.PumpWidget(new TestWidgetsApp(
                    home: new SimpleBuilderTableView(
                        new TwoDimensionalChildBuilderDelegate((_, _) => null),
                        verticalDetails: ScrollableDetails.Horizontal(),
                        horizontalDetails: ScrollableDetails.Vertical())));
            }
            finally
            {
                FlutterError.OnError = oldHandler;
            }

            Assert.Equal(3, exceptions.Count);
            foreach (object exception in exceptions)
            {
                AssertionError assertion = Assert.IsAssignableFrom<AssertionError>(exception);
                Assert.Contains("are not Axis", assertion.Message, StringComparison.Ordinal);
            }
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: ScrollableDetails.controller can set initial scroll
    // positions, modify within bounds'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void ScrollableDetailsControllerCanSetInitialScrollPositionsModifyWithinBounds(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            var verticalController = new ScrollController(initialScrollOffset: 100);
            var horizontalController = new ScrollController(initialScrollOffset: 50);

            tester.PumpWidget(new TestWidgetsApp(
                home: new SimpleBuilderTableView(
                    verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                    horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
                    @delegate: new TwoDimensionalChildBuilderDelegate(
                        TestChildBuilder,
                        maxXIndex: 99,
                        maxYIndex: 99))));
            tester.PumpAndSettle();

            Assert.Equal(100, verticalController.Position.Pixels);
            Assert.Equal(19400, verticalController.Position.MaxScrollExtent);
            Assert.Equal(50, horizontalController.Position.Pixels);
            Assert.Equal(19200, horizontalController.Position.MaxScrollExtent);

            verticalController.JumpTo(verticalController.Position.MaxScrollExtent);
            horizontalController.JumpTo(horizontalController.Position.MaxScrollExtent);
            tester.Pump();

            Assert.Equal(19400, verticalController.Position.Pixels);
            Assert.Equal(19200, horizontalController.Position.Pixels);

            // Out of bounds
            verticalController.JumpTo(verticalController.Position.MaxScrollExtent + 100);
            horizontalController.JumpTo(horizontalController.Position.MaxScrollExtent + 100);
            // Account for varying scroll physics for different platforms (overscroll)
            tester.PumpAndSettle();

            Assert.Equal(19400, verticalController.Position.Pixels);
            Assert.Equal(19200, horizontalController.Position.Pixels);
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: Properly assigns the PrimaryScrollController to the
    // main axis on the correct platform'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void ProperlyAssignsThePrimaryScrollControllerToTheMainAxisOnTheCorrectPlatform(
        TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            ScrollController controller = null!;
            Widget BuildForPrimaryScrollController(
                bool? explicitPrimary = null,
                Axis mainAxis = Axis.Vertical,
                bool addControllerConflict = false)
            {
                var verticalController = new ScrollController();
                var horizontalController = new ScrollController();

                return new TestWidgetsApp(
                    home: new PrimaryScrollController(
                        controller: controller,
                        child: new SimpleBuilderTableView(
                            mainAxis: mainAxis,
                            primary: explicitPrimary,
                            verticalDetails: ScrollableDetails.Vertical(
                                controller: addControllerConflict && mainAxis == Axis.Vertical
                                    ? verticalController
                                    : null),
                            horizontalDetails: ScrollableDetails.Horizontal(
                                controller: addControllerConflict && mainAxis == Axis.Horizontal
                                    ? horizontalController
                                    : null),
                            @delegate: new TwoDimensionalChildBuilderDelegate(
                                TestChildBuilder,
                                maxXIndex: 99,
                                maxYIndex: 99))));
            }

            // Horizontal default - horizontal never automatically adopts PSC
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController(mainAxis: Axis.Horizontal));
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.False(controller.HasClients);
                    break;
            }

            // Horizontal explicitly true
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController(mainAxis: Axis.Horizontal, explicitPrimary: true));
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                // Primary explicitly true is always adopted.
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.True(controller.HasClients);
                    Assert.Equal(Axis.Horizontal, controller.Position.Axis);
                    break;
            }

            // Horizontal explicitly false
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController(mainAxis: Axis.Horizontal, explicitPrimary: false));
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                // Primary explicitly false is never adopted.
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.False(controller.HasClients);
                    break;
            }

            // Vertical default
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController());
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                // Mobile platforms inherit the PSC without explicitly setting
                // primary
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                    Assert.True(controller.HasClients);
                    Assert.Equal(Axis.Vertical, controller.Position.Axis);
                    break;
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.False(controller.HasClients);
                    break;
            }

            // Vertical explicitly true
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController(explicitPrimary: true));
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                // Primary explicitly true is always adopted.
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.True(controller.HasClients);
                    Assert.Equal(Axis.Vertical, controller.Position.Axis);
                    break;
            }

            // Vertical explicitly false
            controller = new ScrollController();
            tester.PumpWidget(BuildForPrimaryScrollController(explicitPrimary: false));
            tester.PumpAndSettle();

            switch (PlatformDefaults.TargetPlatform)
            {
                // Primary explicitly false is never adopted.
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.False(controller.HasClients);
                    break;
            }

            // Assertions
            var exceptions = new List<object>();
            FlutterExceptionHandler? oldHandler = FlutterError.OnError;
            FlutterError.OnError = details => exceptions.Add(details.Exception);
            try
            {
                // Vertical asserts ScrollableDetails.controller has not been provided if
                // primary is explicitly set
                controller = new ScrollController();
                tester.PumpWidget(
                    BuildForPrimaryScrollController(explicitPrimary: true, addControllerConflict: true));
                Assert.Single(exceptions);
                AssertionError assertion = Assert.IsAssignableFrom<AssertionError>(exceptions[0]);
                Assert.Contains(
                    "TwoDimensionalScrollView.primary was explicitly set to true",
                    assertion.Message,
                    StringComparison.Ordinal);
                exceptions.Clear();

                // Horizontal asserts ScrollableDetails.controller has not been provided
                // if primary is explicitly set true
                controller = new ScrollController();
                tester.PumpWidget(BuildForPrimaryScrollController(
                    mainAxis: Axis.Horizontal,
                    explicitPrimary: true,
                    addControllerConflict: true));
                Assert.Single(exceptions);
                assertion = Assert.IsAssignableFrom<AssertionError>(exceptions[0]);
                Assert.Contains(
                    "TwoDimensionalScrollView.primary was explicitly set to true",
                    assertion.Message,
                    StringComparison.Ordinal);
            }
            finally
            {
                FlutterError.OnError = oldHandler;
            }
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: TwoDimensionalScrollable receives the correct details
    // from TwoDimensionalScrollView'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void TwoDimensionalScrollableReceivesTheCorrectDetailsFromTwoDimensionalScrollView(
        TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            BuildContext? capturedContext = null;
            // Default
            tester.PumpWidget(new TestWidgetsApp(
                home: new SimpleBuilderTableView(
                    new TwoDimensionalChildBuilderDelegate((context, vicinity) =>
                    {
                        capturedContext = context;
                        return new Text(vicinity.ToString());
                    }))));
            tester.PumpAndSettle();
            TwoDimensionalScrollableState scrollable = TwoDimensionalScrollable.Of(capturedContext!);
            Assert.Equal(AxisDirection.Down, scrollable.Widget.VerticalDetails.Direction);
            Assert.Equal(AxisDirection.Right, scrollable.Widget.HorizontalDetails.Direction);
            Assert.Equal(DiagonalDragBehavior.None, scrollable.Widget.DiagonalDragBehavior);
            Assert.Equal(DragStartBehavior.Start, scrollable.Widget.DragStartBehavior);

            // Customized
            tester.PumpWidget(new TestWidgetsApp(
                home: new SimpleBuilderTableView(
                    verticalDetails: ScrollableDetails.Vertical(reverse: true),
                    horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
                    diagonalDragBehavior: DiagonalDragBehavior.WeightedContinuous,
                    dragStartBehavior: DragStartBehavior.Down,
                    @delegate: new TwoDimensionalChildBuilderDelegate(TestChildBuilder))));
            tester.PumpAndSettle();
            scrollable = TwoDimensionalScrollable.Of(capturedContext!);
            Assert.Equal(AxisDirection.Up, scrollable.Widget.VerticalDetails.Direction);
            Assert.Equal(AxisDirection.Left, scrollable.Widget.HorizontalDetails.Direction);
            Assert.Equal(DiagonalDragBehavior.WeightedContinuous, scrollable.Widget.DiagonalDragBehavior);
            Assert.Equal(DragStartBehavior.Down, scrollable.Widget.DragStartBehavior);
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: TwoDimensionalScrollable with hitTestBehavior.translucent
    // lets widgets underneath catch the hit'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void TwoDimensionalScrollableWithHitTestBehaviorTranslucentLetsWidgetsUnderneathCatchTheHit(
        TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            bool tapped = false;
            Key key = new UniqueKey();
            tester.PumpWidget(new TestWidgetsApp(
                home: new Stack(
                    children:
                    [
                        Positioned.Fill(
                            child: new GestureDetector(
                                behavior: HitTestBehavior.Opaque,
                                onTap: () => tapped = true,
                                child: new SizedBox(key: key, height: 300))),
                        new SimpleBuilderTableView(
                            hitTestBehavior: HitTestBehavior.Translucent,
                            @delegate: new TwoDimensionalChildBuilderDelegate(
                                (_, _) => new SizedBox(width: 50, height: 50))),
                    ])));
            tester.PumpAndSettle();
            TapAt(tester, tester.GetCenter(tester.ElementsWithKey(key).Single()));
            Assert.True(tapped);
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: Interrupt fling with tap stops scrolling'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void InterruptFlingWithTapStopsScrolling(TargetPlatform platform)
    {
        RunVariant(platform, tester =>
        {
            // Regression test for https://github.com/flutter/flutter/issues/133529
            var log = new List<string>();
            var verticalController = new ScrollController();
            var horizontalController = new ScrollController();

            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                new SimpleBuilderTableView(
                    verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                    horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
                    diagonalDragBehavior: DiagonalDragBehavior.Free,
                    @delegate: new TwoDimensionalChildBuilderDelegate(
                        maxXIndex: 100,
                        maxYIndex: 100,
                        builder: (_, vicinity) => new GestureDetector(
                            onTapUp: _ => log.Add($"Tapped: {vicinity}"),
                            child: new Text($"{vicinity}"))))));

            tester.PumpAndSettle();
            Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();
            Assert.Equal(new List<string>(), log);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Tap once
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Fling the scrollview to get it scrolling, verify that no tap occurs.
            tester.Fling(FindScrollable(), new Vector(0.0, -200.0), 2000.0);
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.True(verticalController.Position.Pixels > 170.0);
            double unchangedOffset = verticalController.Position.Pixels;
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.True(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.True(verticalController.Position.Activity.Velocity > 1500);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Tap to stop the scroll movement, this should stop the fling but not tap anything
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.Equal(unchangedOffset, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Another tap.
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(
                new List<string> { "Tapped: (xIndex: 0, yIndex: 0)", "Tapped: (xIndex: 0, yIndex: 0)" },
                log);
            Assert.Equal(unchangedOffset, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            log.Clear();
            verticalController.JumpTo(0.0);
            tester.Pump();
            // Fling off in the other direction now ----------------------------------
            Assert.Equal(new List<string>(), log);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Tap once
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.Equal(0.0, horizontalController.Position.Pixels);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

            // Fling the scrollview to get it scrolling, verify that no tap occurs.
            tester.Fling(FindScrollable(), new Vector(-200.0, 0.0), 2000.0);
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.True(horizontalController.Position.Pixels > 170.0);
            unchangedOffset = horizontalController.Position.Pixels;
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.True(horizontalController.Position.Activity.IsScrolling);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.True(horizontalController.Position.Activity.Velocity > 1500);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);

            // Tap to stop the scroll movement, this should stop the fling but not tap anything
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
            Assert.Equal(unchangedOffset, horizontalController.Position.Pixels);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);

            // Another tap.
            tester.Tap(FindScrollable());
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(
                new List<string> { "Tapped: (xIndex: 0, yIndex: 0)", "Tapped: (xIndex: 0, yIndex: 0)" },
                log);
            Assert.Equal(unchangedOffset, horizontalController.Position.Pixels);
            Assert.Equal(0.0, verticalController.Position.Pixels);
            Assert.False(horizontalController.Position.Activity.IsScrolling);
            Assert.False(verticalController.Position.Activity.IsScrolling);
            Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);
            Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
        });
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: Fling, wait to stop and tap'
    [Fact]
    public void FlingWaitToStopAndTap()
    {
        using FrameworkDartTester tester = CreateTester();
        // Regression test for https://github.com/flutter/flutter/issues/133529
        var log = new List<string>();
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SimpleBuilderTableView(
                verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
                diagonalDragBehavior: DiagonalDragBehavior.Free,
                @delegate: new TwoDimensionalChildBuilderDelegate(
                    maxXIndex: 100,
                    maxYIndex: 100,
                    builder: (_, vicinity) => new GestureDetector(
                        onTapUp: _ => log.Add($"Tapped: {vicinity}"),
                        child: new Text($"{vicinity}"))))));

        tester.PumpAndSettle();
        Element FindScrollable() => tester.ElementOfType<TwoDimensionalScrollable>();
        Assert.Equal(new List<string>(), log);
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.False(verticalController.Position.Activity.IsScrolling);
        Assert.False(horizontalController.Position.Activity.IsScrolling);
        Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
        Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

        // Tap once
        tester.Tap(FindScrollable());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.False(verticalController.Position.Activity.IsScrolling);
        Assert.False(horizontalController.Position.Activity.IsScrolling);
        Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
        Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

        // Fling the scrollview to get it scrolling, verify that no tap occurs.
        tester.Fling(FindScrollable(), new Vector(0.0, -200.0), 2000.0);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
        Assert.True(verticalController.Position.Pixels > 170.0);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.True(verticalController.Position.Activity.IsScrolling);
        Assert.False(horizontalController.Position.Activity.IsScrolling);
        Assert.True(verticalController.Position.Activity.Velocity > 1500);
        Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

        // Wait for the fling to finish.
        tester.PumpAndSettle();
        Assert.Equal(new List<string> { "Tapped: (xIndex: 0, yIndex: 0)" }, log);
        Assert.True(verticalController.Position.Pixels > 800.0);
        double unchangedOffset = verticalController.Position.Pixels;
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.False(verticalController.Position.Activity.IsScrolling);
        Assert.False(horizontalController.Position.Activity.IsScrolling);
        Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
        Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);

        // Another tap.
        tester.Tap(FindScrollable());
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(
            new List<string> { "Tapped: (xIndex: 0, yIndex: 0)", "Tapped: (xIndex: 0, yIndex: 4)" },
            log);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.Equal(unchangedOffset, verticalController.Position.Pixels);
        Assert.False(horizontalController.Position.Activity.IsScrolling);
        Assert.False(verticalController.Position.Activity.IsScrolling);
        Assert.Equal(0.0, horizontalController.Position.Activity.Velocity);
        Assert.Equal(0.0, verticalController.Position.Activity.Velocity);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.free'
    // (group 'Can drag horizontally when there is not enough vertical content')
    [Fact]
    public void CanDragHorizontallyWhenThereIsNotEnoughVerticalContent_DiagonalDragBehaviorFree()
    {
        DragHorizontallyWithoutVerticalContent(DiagonalDragBehavior.Free);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.weightedEvent'
    // (group 'Can drag horizontally when there is not enough vertical content')
    [Fact]
    public void CanDragHorizontallyWhenThereIsNotEnoughVerticalContent_DiagonalDragBehaviorWeightedEvent()
    {
        DragHorizontallyWithoutVerticalContent(DiagonalDragBehavior.WeightedEvent);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.weightedContinuous'
    // (group 'Can drag horizontally when there is not enough vertical content')
    [Fact]
    public void CanDragHorizontallyWhenThereIsNotEnoughVerticalContent_DiagonalDragBehaviorWeightedContinuous()
    {
        DragHorizontallyWithoutVerticalContent(DiagonalDragBehavior.WeightedContinuous);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.free'
    // (group 'Can drag vertically when there is not enough horizontal content')
    [Fact]
    public void CanDragVerticallyWhenThereIsNotEnoughHorizontalContent_DiagonalDragBehaviorFree()
    {
        DragVerticallyWithoutHorizontalContent(DiagonalDragBehavior.Free);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.weightedEvent'
    // (group 'Can drag vertically when there is not enough horizontal content')
    [Fact]
    public void CanDragVerticallyWhenThereIsNotEnoughHorizontalContent_DiagonalDragBehaviorWeightedEvent()
    {
        DragVerticallyWithoutHorizontalContent(DiagonalDragBehavior.WeightedEvent);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: DiagonalDragBehavior.weightedContinuous'
    // (group 'Can drag vertically when there is not enough horizontal content')
    [Fact]
    public void CanDragVerticallyWhenThereIsNotEnoughHorizontalContent_DiagonalDragBehaviorWeightedContinuous()
    {
        DragVerticallyWithoutHorizontalContent(DiagonalDragBehavior.WeightedContinuous);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: Dismiss keyboard onDrag and keep dismissed on drawer
    // opened'
    [Fact]
    public void DismissKeyboardOnDragAndKeepDismissedOnDrawerOpened()
    {
        using var testTextInput = new TestTextInput();
        using FrameworkDartTester tester = CreateTester();
        var overlayKey = new GlobalObjectKey<DrawerLikeContainerState>(new object());

        tester.PumpWidget(new TestWidgetsApp(
            home: new DrawerLikeContainer(
                key: overlayKey,
                child: new Column(
                    children:
                    [
                        new TestTextField(),
                        new Expanded(
                            child: new SimpleBuilderTableView(
                                keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag,
                                @delegate: new TwoDimensionalChildBuilderDelegate(
                                    TestChildBuilder,
                                    maxXIndex: 99,
                                    maxYIndex: 99))),
                    ]))));

        tester.PumpAndSettle();

        Assert.False(testTextInput.IsVisible);
        Element finder = tester.ElementsOfType<TestTextField>().First();
        tester.Tap(finder);
        // flutter_test's `tap` runs inside `TestAsyncUtils.guard`, whose await drains the microtasks
        // the focus request scheduled.
        Scheduler.FlushMicrotasks();
        Assert.True(testTextInput.IsVisible);

        tester.Drag(tester.ElementsOfType<SimpleBuilderTableView>().First(), new Vector(-40.0, -40.0));
        tester.PumpAndSettle();

        Assert.False(testTextInput.IsVisible);
        overlayKey.CurrentState!.ShowOverlay();
        tester.PumpAndSettle();

        Assert.False(testTextInput.IsVisible);
    }

    // Flutter: 'two_dimensional_scroll_view_test.dart: cacheExtentStyle is passed to viewport'
    [Fact]
    public void CacheExtentStyleIsPassedToViewport()
    {
        using FrameworkDartTester tester = CreateTester();
        // Plumix does not port the deprecated `cacheExtent`/`cacheExtentStyle` pair; Dart folds
        // `cacheExtent: 1.0, cacheExtentStyle: CacheExtentStyle.viewport` into
        // `ScrollCacheExtent.viewport(1.0)`, which is what reaches the viewport here.
        tester.PumpWidget(new TestWidgetsApp(
            home: new SimpleBuilderTableView(
                scrollCacheExtent: ScrollCacheExtent.Viewport(1.0),
                @delegate: new TwoDimensionalChildBuilderDelegate(
                    TestChildBuilder,
                    maxXIndex: 5,
                    maxYIndex: 5))));
        tester.PumpAndSettle();

        var viewport = (SimpleBuilderTableViewport)tester.ElementOfType<SimpleBuilderTableViewport>().Widget;
        Assert.Equal(1.0, viewport.ScrollCacheExtent!.Value);
        Assert.Equal(CacheExtentStyle.Viewport, viewport.ScrollCacheExtent.Style);
    }

    // ------------------------------------------------------------------ helpers

    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true, devicePixelRatio: 3.0);

    /// <summary>Runs <paramref name="body"/> as one <c>TargetPlatformVariant</c> case.</summary>
    private static void RunVariant(TargetPlatform platform, Action<FrameworkDartTester> body)
    {
        RestorationManager previousManager = RestorationManager.Instance;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            // flutter_test's binding owns a TestRestorationManager for every test.
            using var manager = new FlutterTestRestorationManager();
            RestorationManager.Instance = manager;
            using FrameworkDartTester tester = CreateTester();
            body(tester);
        }
        finally
        {
            RestorationManager.Instance = previousManager;
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    /// <summary>Dart's <c>tester.tapAt</c>.</summary>
    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        int pointer = tester.StartGesture(location);
        tester.Up(pointer, location);
    }

    // two_dimensional_scroll_view_test.dart: _testChildBuilder.
    private static Widget? TestChildBuilder(BuildContext context, ChildVicinity vicinity)
    {
        return SizedBox.Square(
            dimension: 200.0,
            child: new Center(child: new Text($"C{vicinity.XIndex}:R{vicinity.YIndex}")));
    }

    // two_dimensional_utils.dart: builderDelegate.
    private static TwoDimensionalChildBuilderDelegate BuilderDelegate()
    {
        return new TwoDimensionalChildBuilderDelegate(
            maxXIndex: 5,
            maxYIndex: 5,
            builder: (_, vicinity) => new Container(
                key: new ValueKey<ChildVicinity>(vicinity),
                color: vicinity.XIndex % 2 == 0 && vicinity.YIndex % 2 == 0
                    ? Amber100
                    : (vicinity.XIndex % 2 != 0 && vicinity.YIndex % 2 != 0 ? BlueAccent100 : null),
                height: 200,
                width: 200,
                child: new Center(child: new Text($"R{vicinity.XIndex}:C{vicinity.YIndex}"))));
    }

    // two_dimensional_utils.dart: simpleBuilderTest.
    private static Widget SimpleBuilderTest(
        Axis mainAxis = Axis.Vertical,
        ScrollableDetails? verticalDetails = null,
        ScrollableDetails? horizontalDetails = null,
        TwoDimensionalChildBuilderDelegate? @delegate = null,
        DiagonalDragBehavior? diagonalDrag = null,
        Clip? clipBehavior = null,
        string? restorationID = null)
    {
        return new TestWidgetsApp(
            restorationScopeId: restorationID,
            home: new Align(
                child: new SimpleBuilderTableView(
                    mainAxis: mainAxis,
                    verticalDetails: verticalDetails ?? ScrollableDetails.Vertical(),
                    horizontalDetails: horizontalDetails ?? ScrollableDetails.Horizontal(),
                    diagonalDragBehavior: diagonalDrag ?? DiagonalDragBehavior.None,
                    clipBehavior: clipBehavior ?? Clip.HardEdge,
                    @delegate: @delegate ?? BuilderDelegate())));
    }

    // two_dimensional_viewport_test.dart: restoreScrollAndVerify.
    private static void RestoreScrollAndVerify(FrameworkDartTester tester, Widget app)
    {
        TwoDimensionalScrollableState FindScrollable() => tester.State<TwoDimensionalScrollableState>();
        var manager = (FlutterTestRestorationManager)RestorationManager.Instance;

        FindScrollable().HorizontalScrollable.Position.JumpTo(100);
        FindScrollable().VerticalScrollable.Position.JumpTo(100);
        tester.Pump();
        RestartAndRestore(tester, manager, app);

        Assert.Equal(100.0, FindScrollable().HorizontalScrollable.Position.Pixels);
        Assert.Equal(100.0, FindScrollable().VerticalScrollable.Position.Pixels);

        TestRestorationData data = GetRestorationData(manager);
        FindScrollable().HorizontalScrollable.Position.JumpTo(0);
        FindScrollable().VerticalScrollable.Position.JumpTo(0);
        tester.Pump();
        RestoreFrom(tester, manager, data);

        Assert.Equal(100.0, FindScrollable().HorizontalScrollable.Position.Pixels);
        Assert.Equal(100.0, FindScrollable().VerticalScrollable.Position.Pixels);
    }

    /// <summary>
    /// flutter_test's <c>WidgetTester.restartAndRestore</c>: tears the tree down, feeds the collected
    /// data back to the manager and mounts the same root widget again.
    /// </summary>
    private static void RestartAndRestore(
        FrameworkDartTester tester,
        FlutterTestRestorationManager manager,
        Widget rootWidget)
    {
        Assert.True(
            manager.DebugRootBucketAccessed,
            "The current widget tree did not inject the root bucket of the RestorationManager and "
            + "therefore no restoration data has been collected to restore from. Did you forget to wrap "
            + "your widget tree in a RootRestorationScope?");
        TestRestorationData restorationData = manager.RestorationData;
        tester.PumpWidget(new Container(key: new UniqueKey()));
        manager.RestoreFrom(restorationData);
        tester.PumpWidget(rootWidget);
    }

    /// <summary>flutter_test's <c>WidgetTester.getRestorationData</c>.</summary>
    private static TestRestorationData GetRestorationData(FlutterTestRestorationManager manager)
    {
        Assert.True(manager.DebugRootBucketAccessed);
        return manager.RestorationData;
    }

    /// <summary>flutter_test's <c>WidgetTester.restoreFrom</c>.</summary>
    private static void RestoreFrom(
        FrameworkDartTester tester,
        FlutterTestRestorationManager manager,
        TestRestorationData data)
    {
        manager.RestoreFrom(data);
        tester.Pump();
    }

    private static void DragHorizontallyWithoutVerticalContent(DiagonalDragBehavior diagonalDragBehavior)
    {
        using FrameworkDartTester tester = CreateTester();
        // Regression test for https://github.com/flutter/flutter/issues/144982
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SimpleBuilderTableView(
                verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
                diagonalDragBehavior: diagonalDragBehavior,
                @delegate: new TwoDimensionalChildBuilderDelegate(
                    maxXIndex: 20,
                    maxYIndex: 1,
                    builder: TestChildBuilder))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.Equal(0.0, verticalController.Position.MaxScrollExtent);
        Assert.Equal(3400.0, horizontalController.Position.MaxScrollExtent);
        // Fling vertically, nothing should happen.
        tester.Fling(tester.ElementOfType<TwoDimensionalScrollable>(), new Vector(0.0, -200.0), 2000.0);
        tester.PumpAndSettle();
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        // Fling horizontally, the horizontal position should change.
        tester.Fling(tester.ElementOfType<TwoDimensionalScrollable>(), new Vector(-200.0, 0.0), 2000.0);
        tester.PumpAndSettle();
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.True(horizontalController.Position.Pixels > 840.0);
    }

    private static void DragVerticallyWithoutHorizontalContent(DiagonalDragBehavior diagonalDragBehavior)
    {
        using FrameworkDartTester tester = CreateTester();
        // Regression test for https://github.com/flutter/flutter/issues/144982
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new SimpleBuilderTableView(
                verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
                horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController),
                diagonalDragBehavior: diagonalDragBehavior,
                @delegate: new TwoDimensionalChildBuilderDelegate(
                    maxXIndex: 1,
                    maxYIndex: 20,
                    builder: TestChildBuilder))));

        tester.PumpAndSettle();
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        Assert.Equal(3600.0, verticalController.Position.MaxScrollExtent);
        Assert.Equal(0.0, horizontalController.Position.MaxScrollExtent);
        // Fling horizontally, nothing should happen.
        tester.Fling(tester.ElementOfType<TwoDimensionalScrollable>(), new Vector(-200.0, 0.0), 2000.0);
        tester.PumpAndSettle();
        Assert.Equal(0.0, verticalController.Position.Pixels);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
        // Fling vertically, the vertical position should change.
        tester.Fling(tester.ElementOfType<TwoDimensionalScrollable>(), new Vector(0.0, -200.0), 2000.0);
        tester.PumpAndSettle();
        Assert.True(verticalController.Position.Pixels > 840.0);
        Assert.Equal(0.0, horizontalController.Position.Pixels);
    }

    /// <summary>flutter_test's <c>TestRestorationData</c>: an opaque snapshot of the encoded data.</summary>
    private sealed class TestRestorationData
    {
        public static readonly TestRestorationData Empty = new(null);

        public TestRestorationData(byte[]? binary)
        {
            Binary = binary;
        }

        public byte[]? Binary { get; }
    }

    /// <summary>
    /// flutter_test's <c>TestRestorationManager</c>: restoration is enabled from the start (with empty
    /// data) so the root bucket is always available synchronously, and whatever the framework sends to
    /// the engine becomes the data the next <see cref="RestoreFrom"/> can hand back.
    /// </summary>
    private sealed class FlutterTestRestorationManager : RestorationManager
    {
        public FlutterTestRestorationManager()
        {
            RestorationData = TestRestorationData.Empty;
            RestoreFrom(TestRestorationData.Empty);
        }

        public TestRestorationData RestorationData { get; private set; }

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

        protected override void SendToEngine(byte[] encodedData)
        {
            RestorationData = new TestRestorationData(encodedData);
        }
    }

    /// <summary>
    /// The part of flutter_test's <c>TestTextInput</c> the keyboard test reads: it answers the
    /// <c>flutter/textinput</c> channel and tracks <c>isVisible</c>.
    /// </summary>
    private sealed class TestTextInput : IDisposable
    {
        public TestTextInput()
        {
            Plumix.UI.TextInput.DebugReset();
            Plumix.UI.TextInput.EnsureInitialized();
            SystemChannels.TextInput.SetPlatformMethodCallHandler(HandleTextInputCall);
        }

        public bool IsVisible { get; private set; }

        public void Dispose()
        {
            SystemChannels.TextInput.SetPlatformMethodCallHandler(null);
            Plumix.UI.TextInput.DebugReset();
            Scheduler.FlushMicrotasks();
        }

        private Task<object?> HandleTextInputCall(MethodCall call)
        {
            switch (call.Method)
            {
                case "TextInput.clearClient":
                case "TextInput.hide":
                    IsVisible = false;
                    break;
                case "TextInput.show":
                    IsVisible = true;
                    break;
            }

            return Task.FromResult<object?>(null);
        }
    }

    /// <summary>
    /// editable_text_tester.dart's <c>TestTextField</c>, reduced to what the keyboard test needs: an
    /// <see cref="EditableText"/> whose single tap requests focus (and so the keyboard).
    /// </summary>
    /// <remarks>
    /// The Dart widget routes taps through a <c>TextSelectionGestureDetectorBuilder</c>, which Plumix
    /// does not port; a single tap there ends in <c>EditableTextState.requestKeyboard</c>, which for an
    /// unfocused field requests focus — exactly what this tap handler does.
    /// </remarks>
    private sealed class TestTextField : StatefulWidget
    {
        public override State CreateState() => new TestTextFieldState();
    }

    private sealed class TestTextFieldState : State<TestTextField>
    {
        private static readonly Color Red = new Color(0xFFF44336);

        private readonly TextEditingController _controller = new();
        private readonly FocusNode _focusNode = new();

        public override void Dispose()
        {
            _focusNode.Dispose();
            _controller.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new GestureDetector(
                behavior: HitTestBehavior.Translucent,
                onTap: () => _focusNode.RequestFocus(),
                child: new EditableText(
                    controller: _controller,
                    focusNode: _focusNode,
                    cursorColor: Red,
                    style: new TextStyle(),
                    rendererIgnoresPointer: true));
        }
    }

    // two_dimensional_scroll_view_test.dart: _DrawerLikeContainer.
    private sealed class DrawerLikeContainer : StatefulWidget
    {
        public DrawerLikeContainer(Widget child, Key? key = null) : base(key)
        {
            Child = child;
        }

        public Widget Child { get; }

        public override State CreateState() => new DrawerLikeContainerState();
    }

    private sealed class DrawerLikeContainerState : State<DrawerLikeContainer>
    {
        private bool _showOverlay;

        public void ShowOverlay()
        {
            SetState(() => _showOverlay = true);
        }

        public override Widget Build(BuildContext context)
        {
            var children = new List<Plumix.Widgets.Widget> { Widget.Child };
            if (_showOverlay)
            {
                children.Add(Positioned.Fill(child: new Container(color: new Color(0x88000000))));
            }

            return new Stack(children: children);
        }
    }
}
