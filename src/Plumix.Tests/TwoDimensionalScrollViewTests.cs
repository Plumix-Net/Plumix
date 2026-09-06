using System.Threading;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/two_dimensional_scroll_view_test.dart and the
// TwoDimensionalScrollable group of two_dimensional_viewport_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TwoDimensionalScrollViewTests
{
    private static readonly Size Surface = new(800, 600);

    [Fact]
    public void TwoDimensionalScrollView_AssertsTheAxisDirectionsDoNotConflict()
    {
        Assert.Throws<AssertionError>(() =>
        {
            var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(),
                horizontalDetails: ScrollableDetails.Vertical()));
            harness.Pump(Surface);
        });

        Assert.Throws<AssertionError>(() =>
        {
            var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(),
                verticalDetails: ScrollableDetails.Horizontal()));
            harness.Pump(Surface);
        });
    }

    [Fact]
    public void TwoDimensionalScrollable_CreatesFallbackControllersWhenTheDetailsHaveNone()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);

        TwoDimensionalScrollableState state = ScrollableState(harness);
        Assert.NotNull(state.VerticalScrollable.Position);
        Assert.NotNull(state.HorizontalScrollable.Position);
        Assert.Equal(0.0, state.VerticalScrollable.Position.Pixels);
        Assert.Equal(0.0, state.HorizontalScrollable.Position.Pixels);
    }

    [Fact]
    public void TwoDimensionalScrollable_OfAndMaybeOfResolveFromAChildContext()
    {
        BuildContext? cellContext = null;
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(builder: (context, vicinity) =>
            {
                cellContext ??= context;
                return TwoDimensionalHarness.DefaultBuilder(context, vicinity);
            })));
        harness.Pump(Surface);

        Assert.NotNull(TwoDimensionalScrollable.MaybeOf(cellContext!));
        Assert.NotNull(TwoDimensionalScrollable.Of(cellContext!));
    }

    [Fact]
    public void TwoDimensionalScrollable_OfThrowsWithoutAnAncestor()
    {
        var harness = new TwoDimensionalRenderHarness(new SizedBox());
        harness.Pump(Surface);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => TwoDimensionalScrollable.Of(harness.RootContext));
        Assert.Contains(
            "TwoDimensionalScrollable.of() was called with a context that does not contain a "
            + "TwoDimensionalScrollable widget.",
            error.Message,
            StringComparison.Ordinal);
        Assert.Null(TwoDimensionalScrollable.MaybeOf(harness.RootContext));
    }

    [Fact]
    public void ScrollableDetailsControllers_SetInitialOffsetsAndClampWithinBounds()
    {
        var verticalController = new ScrollController(initialScrollOffset: 100.0);
        var horizontalController = new ScrollController(initialScrollOffset: 50.0);
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(maxXIndex: 99, maxYIndex: 99),
            verticalDetails: ScrollableDetails.Vertical(controller: verticalController),
            horizontalDetails: ScrollableDetails.Horizontal(controller: horizontalController)));
        harness.Pump(Surface);

        Assert.Equal(100.0, verticalController.Position.Pixels);
        Assert.Equal(19400.0, verticalController.Position.MaxScrollExtent);
        Assert.Equal(50.0, horizontalController.Position.Pixels);
        Assert.Equal(19200.0, horizontalController.Position.MaxScrollExtent);

        verticalController.JumpTo(verticalController.Position.MaxScrollExtent);
        horizontalController.JumpTo(horizontalController.Position.MaxScrollExtent);
        harness.Pump(Surface);
        Assert.Equal(19400.0, verticalController.Position.Pixels);
        Assert.Equal(19200.0, horizontalController.Position.Pixels);

        verticalController.JumpTo(verticalController.Position.MaxScrollExtent + 100.0);
        horizontalController.JumpTo(horizontalController.Position.MaxScrollExtent + 100.0);
        Settle(harness);
        Assert.Equal(19400.0, verticalController.Position.Pixels, 3);
        Assert.Equal(19200.0, horizontalController.Position.Pixels, 3);
    }

    [Fact]
    public void TwoDimensionalScrollable_ReceivesTheDetailsFromTheScrollView()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);

        TwoDimensionalScrollableState state = ScrollableState(harness);
        Assert.Equal(AxisDirection.Down, state.VerticalScrollable.AxisDirection);
        Assert.Equal(AxisDirection.Right, state.HorizontalScrollable.AxisDirection);

        var reversed = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(reverse: true),
            horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
            diagonalDragBehavior: DiagonalDragBehavior.WeightedContinuous,
            dragStartBehavior: DragStartBehavior.Down));
        reversed.Pump(Surface);

        TwoDimensionalScrollableState reversedState = ScrollableState(reversed);
        Assert.Equal(AxisDirection.Up, reversedState.VerticalScrollable.AxisDirection);
        Assert.Equal(AxisDirection.Left, reversedState.HorizontalScrollable.AxisDirection);

        TwoDimensionalScrollable widget = ScrollableWidget(reversed);
        Assert.Equal(DiagonalDragBehavior.WeightedContinuous, widget.DiagonalDragBehavior);
        Assert.Equal(DragStartBehavior.Down, widget.DragStartBehavior);
    }

    [Fact]
    public void InnerScrollables_CarryTheRestorationIdsFlutterAssigns()
    {
        var harness = new TwoDimensionalRenderHarness(
            new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate()));
        harness.Pump(Surface);

        TwoDimensionalScrollableState state = ScrollableState(harness);
        Assert.Equal(
            "OuterVerticalTwoDimensionalScrollable",
            ((Scrollable)state.VerticalScrollable.Context.Widget).RestorationId);
        Assert.Equal(
            "InnerHorizontalTwoDimensionalScrollable",
            ((Scrollable)state.HorizontalScrollable.Context.Widget).RestorationId);
    }

    [Fact]
    public void InnerScrollables_ReceiveThePhysicsClipAndSemanticsFlags()
    {
        var verticalController = new ScrollController();
        var horizontalController = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(),
            verticalDetails: ScrollableDetails.Vertical(
                controller: verticalController,
                physics: new AlwaysScrollableScrollPhysics(),
                decorationClipBehavior: Clip.AntiAliasWithSaveLayer),
            horizontalDetails: ScrollableDetails.Horizontal(
                controller: horizontalController,
                physics: new ClampingScrollPhysics(),
                decorationClipBehavior: Clip.AntiAlias)));
        harness.Pump(Surface);

        TwoDimensionalScrollableState state = ScrollableState(harness);
        var vertical = (Scrollable)state.VerticalScrollable.Context.Widget;
        var horizontal = (Scrollable)state.HorizontalScrollable.Context.Widget;

        Assert.Same(verticalController, vertical.Controller);
        Assert.IsType<AlwaysScrollableScrollPhysics>(vertical.Physics);
        Assert.Equal(Clip.AntiAliasWithSaveLayer, vertical.ClipBehavior);
        Assert.Same(horizontalController, horizontal.Controller);
        Assert.IsType<ClampingScrollPhysics>(horizontal.Physics);
        Assert.Equal(Clip.AntiAlias, horizontal.ClipBehavior);
        Assert.False(vertical.ExcludeFromSemantics);
        Assert.Equal(DragStartBehavior.Start, vertical.DragStartBehavior);
    }

    [Fact]
    public void Primary_AdoptsThePrimaryScrollControllerForTheMainAxisOnly()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new PrimaryScrollController(
            controller: controller,
            child: new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(maxXIndex: 99, maxYIndex: 99),
                primary: true)));
        harness.Pump(Surface);

        Assert.True(controller.HasClients);
        Assert.Equal(Axis.Vertical, controller.Position.Axis);
    }

    [Fact]
    public void Primary_HorizontalMainAxisAdoptsTheControllerOnTheHorizontalAxis()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new PrimaryScrollController(
            controller: controller,
            scrollDirection: Axis.Horizontal,
            child: new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(maxXIndex: 99, maxYIndex: 99),
                mainAxis: Axis.Horizontal,
                primary: true)));
        harness.Pump(Surface);

        Assert.True(controller.HasClients);
        Assert.Equal(Axis.Horizontal, controller.Position.Axis);
    }

    [Fact]
    public void Primary_False_NeverAdoptsTheController()
    {
        var controller = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new PrimaryScrollController(
            controller: controller,
            child: new SimpleBuilderTableView(
                TwoDimensionalHarness.BuilderDelegate(maxXIndex: 99, maxYIndex: 99),
                primary: false)));
        harness.Pump(Surface);

        Assert.False(controller.HasClients);
    }

    [Fact]
    public void Primary_True_RejectsAControllerOnTheMainAxis()
    {
        var controller = new ScrollController();
        var mainAxisController = new ScrollController();

        AssertionError error = Assert.Throws<AssertionError>(() =>
        {
            var harness = new TwoDimensionalRenderHarness(new PrimaryScrollController(
                controller: controller,
                child: new SimpleBuilderTableView(
                    TwoDimensionalHarness.BuilderDelegate(),
                    primary: true,
                    verticalDetails: ScrollableDetails.Vertical(controller: mainAxisController))));
            harness.Pump(Surface);
        });
        Assert.Contains(
            "TwoDimensionalScrollView.primary was explicitly set to true",
            error.Message,
            StringComparison.Ordinal);
    }

    // Diagonal drag behavior -------------------------------------------------------------------

    [Fact]
    public void DiagonalDragBehaviorNone_LocksTheAxisForTheWholeGesture()
    {
        (TwoDimensionalRenderHarness harness, ScrollController vertical, ScrollController horizontal) =
            DragCase(DiagonalDragBehavior.None);

        // Each gesture spends its first move reaching the recognizer's touch slop, so only the
        // remaining 80 pixels are applied — the same accounting Flutter's `tester.drag` does.
        DragPath(harness, [new Point(0.0, -20.0), new Point(0.0, -100.0)]);
        Assert.Equal(80.0, vertical.Position.Pixels, 3);
        Assert.Equal(0.0, horizontal.Position.Pixels, 3);

        DragPath(harness, [new Point(-20.0, 0.0), new Point(-100.0, 0.0)]);
        Assert.Equal(80.0, vertical.Position.Pixels, 3);
        Assert.Equal(80.0, horizontal.Position.Pixels, 3);

        // A diagonal gesture is locked to the axis that won the arena: the horizontal component of
        // the drag below is dropped entirely.
        DragPath(harness, [new Point(0.0, -20.0), new Point(-40.0, -100.0)]);
        Assert.Equal(160.0, vertical.Position.Pixels, 3);
        Assert.Equal(80.0, horizontal.Position.Pixels, 3);

        DragPath(harness, [new Point(-20.0, 0.0), new Point(-100.0, -40.0)]);
        Assert.Equal(160.0, vertical.Position.Pixels, 3);
        Assert.Equal(160.0, horizontal.Position.Pixels, 3);
    }

    [Fact]
    public void DiagonalDragBehaviorFree_MovesBothAxesOnEveryUpdate()
    {
        (TwoDimensionalRenderHarness harness, ScrollController vertical, ScrollController horizontal) =
            DragCase(DiagonalDragBehavior.Free);

        DragPath(harness, DiagonalPath);
        Assert.Equal(90.0, vertical.Position.Pixels, 3);
        Assert.Equal(130.0, horizontal.Position.Pixels, 3);
    }

    [Fact]
    public void DiagonalDragBehaviorWeightedEvent_LocksTheWinnerForTheWholeGesture()
    {
        (TwoDimensionalRenderHarness harness, ScrollController vertical, ScrollController horizontal) =
            DragCase(DiagonalDragBehavior.WeightedEvent);

        // The second step is the first one with a winner, and vertical stays locked from then on,
        // so the two horizontal-dominant steps that follow still move the vertical axis only.
        DragPath(harness, DiagonalPath);
        Assert.Equal(90.0, vertical.Position.Pixels, 3);
        Assert.Equal(30.0, horizontal.Position.Pixels, 3);
    }

    [Fact]
    public void DiagonalDragBehaviorWeightedContinuous_ReevaluatesTheLockPerUpdate()
    {
        (TwoDimensionalRenderHarness harness, ScrollController vertical, ScrollController horizontal) =
            DragCase(DiagonalDragBehavior.WeightedContinuous);

        // The lock is recomputed against the previous update, so the winner flips to horizontal for
        // the last two steps and the vertical component of the final step is dropped.
        DragPath(harness, DiagonalPath);
        Assert.Equal(80.0, vertical.Position.Pixels, 3);
        Assert.Equal(130.0, horizontal.Position.Pixels, 3);
    }

    [Fact]
    public void DiagonalDragBehavior_KeepsDraggingTheOtherAxisWithoutEnoughContent()
    {
        // Regression parity with flutter#144982: an axis whose content fits must not disable the
        // pan recognizer the other axis relies on.
        var vertical = new ScrollController();
        var horizontal = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(maxXIndex: 20, maxYIndex: 1),
            diagonalDragBehavior: DiagonalDragBehavior.Free,
            verticalDetails: ScrollableDetails.Vertical(controller: vertical),
            horizontalDetails: ScrollableDetails.Horizontal(controller: horizontal)));
        harness.Pump(Surface);

        Assert.Equal(0.0, vertical.Position.MaxScrollExtent);
        Assert.Equal(3400.0, horizontal.Position.MaxScrollExtent);

        Drag(harness, new Point(0.0, -200.0));
        Assert.Equal(0.0, vertical.Position.Pixels, 3);

        Drag(harness, new Point(-200.0, 0.0));
        Assert.Equal(0.0, vertical.Position.Pixels, 3);
        Assert.Equal(200.0, horizontal.Position.Pixels, 3);
    }

    // Helpers ----------------------------------------------------------------------------------

    private static (TwoDimensionalRenderHarness, ScrollController, ScrollController) DragCase(
        DiagonalDragBehavior behavior)
    {
        GestureBinding.Instance.ResetForTests();
        var vertical = new ScrollController();
        var horizontal = new ScrollController();
        var harness = new TwoDimensionalRenderHarness(new SimpleBuilderTableView(
            TwoDimensionalHarness.BuilderDelegate(maxXIndex: 99, maxYIndex: 99),
            diagonalDragBehavior: behavior,
            verticalDetails: ScrollableDetails.Vertical(controller: vertical),
            horizontalDetails: ScrollableDetails.Horizontal(controller: horizontal)));
        harness.Pump(Surface);
        return (harness, vertical, horizontal);
    }

    /// <summary>Flutter's `kDragSlopDefault`, the offset `tester.drag` spends reaching recognition.</summary>
    private const double DragSlop = 20.0;

    private static readonly Point Center = new(400.0, 300.0);

    private static int _nextPointer;

    /// <summary>
    /// The four-step gesture the diagonal cases share: a slop step, a vertical-dominant step, a
    /// horizontal-only step, and a horizontal-dominant step that also carries some vertical motion.
    /// </summary>
    private static readonly Point[] DiagonalPath =
    [
        new(-30.0, -30.0),
        new(-30.0, -80.0),
        new(-80.0, -80.0),
        new(-130.0, -90.0),
    ];

    /// <summary>
    /// One drag of <paramref name="offset"/> from the middle of the viewport, split into the slop
    /// move and the remainder exactly as Flutter's `WidgetController.drag` splits it.
    /// </summary>
    private static void Drag(TwoDimensionalRenderHarness harness, Point offset)
    {
        var slop = new Point(
            Math.Sign(offset.X) * Math.Min(Math.Abs(offset.X), DragSlop),
            Math.Sign(offset.Y) * Math.Min(Math.Abs(offset.Y), DragSlop));
        DragPath(harness, [slop, offset]);
    }

    /// <summary>
    /// Drives one pointer gesture from the middle of the viewport through every cumulative offset,
    /// the way Flutter's `startGesture`/`moveTo` sequences do.
    /// </summary>
    private static void DragPath(TwoDimensionalRenderHarness harness, IReadOnlyList<Point> offsets)
    {
        DateTime now = DateTime.UtcNow;
        int pointer = 7 + Interlocked.Increment(ref _nextPointer);
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(pointer, PointerDeviceKind.Touch, Center, PointerButtons.Primary, now));

        int step = 1;
        foreach (Point offset in offsets)
        {
            GestureBinding.Instance.HandlePointerEvent(
                harness.RenderView,
                new PointerMoveEvent(
                    pointer,
                    PointerDeviceKind.Touch,
                    new Point(Center.X + offset.X, Center.Y + offset.Y),
                    PointerButtons.Primary,
                    true,
                    now));
            step++;
        }

        Point last = offsets[^1];
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Touch,
                new Point(Center.X + last.X, Center.Y + last.Y),
                PointerButtons.None,
                now));
        harness.Pump(Surface);
    }

    /// <summary>Runs enough frames for a ballistic settle to finish.</summary>
    private static void Settle(TwoDimensionalRenderHarness harness)
    {
        double clock = Scheduler.CurrentSeconds;
        foreach (double offset in new[] { 0.01, 0.2, 0.4, 0.8, 1.4, 2.4 })
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + offset));
            harness.Pump(Surface);
        }
    }

    private static TwoDimensionalScrollableState ScrollableState(TwoDimensionalRenderHarness harness)
    {
        TwoDimensionalScrollableState? found = null;
        Visit((Element)harness.RootContext, element =>
        {
            if (element is StatefulElement { State: TwoDimensionalScrollableState state })
            {
                found ??= state;
            }
        });
        return found!;
    }

    private static TwoDimensionalScrollable ScrollableWidget(TwoDimensionalRenderHarness harness)
    {
        TwoDimensionalScrollable? found = null;
        Visit((Element)harness.RootContext, element =>
        {
            if (element.Widget is TwoDimensionalScrollable widget)
            {
                found ??= widget;
            }
        });
        return found!;
    }

    private static void Visit(Element element, Action<Element> visitor)
    {
        visitor(element);
        element.VisitChildren(child => Visit(child, visitor));
    }
}
