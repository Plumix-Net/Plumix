using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scrollable.dart (parity tests)

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for the selection layer <see cref="Scrollable"/> wraps around itself when it has
/// an ancestor selection registrar: Dart's <c>_ScrollableSelectionHandler</c> and
/// <c>_ScrollableSelectionContainerDelegate</c> (<c>widgets/scrollable.dart</c>), mirroring
/// <c>test/widgets/scrollable_selection_test.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollableSelectionTests
{
    private const double ItemHeight = 100.0;
    private const int ItemCount = 10;

    private static readonly Size Surface = new(300, 400);

    [Fact]
    public void Scrollable_WrapsItselfInASelectionContainerOnlyWhenARegistrarIsInScope()
    {
        using var withoutRegistrar = new RestorationHarness(new Directionality(
            TextDirection.Ltr,
            BuildList(controller: null)));
        PumpFully(withoutRegistrar);
        Assert.Null(FindDescendantWidget<ScrollableSelectionHandler>(withoutRegistrar));

        using var registrar = new StaticSelectionContainerDelegate();
        using var withRegistrar = new RestorationHarness(Wrap(registrar, BuildList(controller: null)));
        PumpFully(withRegistrar);
        Assert.NotNull(FindDescendantWidget<ScrollableSelectionHandler>(withRegistrar));
    }

    [Fact]
    public void SelectionHandler_IsTheOutermostWrapperAroundTheScrollable()
    {
        using var registrar = new StaticSelectionContainerDelegate();
        using var harness = new RestorationHarness(Wrap(registrar, BuildList(controller: null)));
        PumpFully(harness);

        Element handler = FindDescendantElement<ScrollableSelectionHandler>(harness)!;

        // The handler sits above the SelectionContainer it builds, which sits above the scroll
        // chrome, the scroll semantics and the _ScrollableScope, exactly as Dart nests them.
        Assert.NotNull(FindDescendantElement<SelectionContainer>(handler));
        Assert.NotNull(FindDescendantElement<ScrollSemantics>(handler));
        Assert.NotNull(FindDescendantElement<ScrollableScope>(handler));

        // ... and nothing above it inside the scrollable: the scrollable's own state is a descendant.
        Assert.Null(FindAncestorElement<ScrollableScope>(handler));
    }

    [Fact]
    public void SelectionHandler_RegistersTheScrollableAsASelectableWithTheAncestorRegistrar()
    {
        using var registrar = new StaticSelectionContainerDelegate();
        using var harness = new RestorationHarness(Wrap(registrar, BuildList(controller: null)));
        PumpFully(harness);

        ISelectable selectable = Assert.Single(registrar.Selectables);
        Assert.IsType<SelectionContainerState>(selectable);
    }

    [Theory]
    [InlineData(AxisDirection.Down, 0, 120)]
    [InlineData(AxisDirection.Up, 0, -120)]
    [InlineData(AxisDirection.Right, 120, 0)]
    [InlineData(AxisDirection.Left, -120, 0)]
    public void GetDeltaToScrollOrigin_MatchesTheAxisDirectionSwitch(
        AxisDirection direction,
        double dx,
        double dy)
    {
        using var controller = new ScrollController();
        BuildContext? itemContext = null;
        using var registrar = new StaticSelectionContainerDelegate();
        using var harness = new RestorationHarness(Wrap(
            registrar,
            BuildList(controller: controller, axisDirection: direction, onItemBuilt: c => itemContext = c)));
        PumpFully(harness);

        controller.JumpTo(120);
        PumpFully(harness);

        Scrollable.ScrollableState state = Scrollable.Of(itemContext!);
        Assert.Equal(
            new Point(dx, dy),
            ScrollableSelectionContainerDelegate.GetDeltaToScrollOrigin(state));
    }

    [Fact]
    public void SelectionStartedInsideTheViewport_DoesNotAutoScrollWhileTheEdgeStaysInside()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, 60));
        rig.PumpFrames(4);

        Assert.Equal(0.0, rig.Controller.Offset);
        Assert.False(rig.Delegate.IsAutoScrollingForTests);
    }

    [Fact]
    public void DraggingTheEndEdgePastTheBottom_AutoScrollsForwardAndSettlesAtTheMaxExtent()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, Surface.Height + 20));
        rig.PumpFrames(2);

        double afterFirst = rig.Controller.Offset;
        Assert.True(afterFirst > 0.0, $"expected a forward auto scroll, got {afterFirst}");

        rig.DragEndEdgeFor(frames: 4);
        Assert.True(
            rig.Controller.Offset > afterFirst,
            $"expected the auto scroll to continue, got {rig.Controller.Offset} after {afterFirst}");

        rig.DragEndEdgeFor(frames: 80);
        Assert.Equal(rig.Controller.Position.MaxScrollExtent, rig.Controller.Offset, precision: 6);
    }

    [Fact]
    public void DraggingTheEndEdgePastTheTop_AutoScrollsBackward()
    {
        using SelectionRig rig = SelectionRig.Create();
        rig.Controller.JumpTo(600);
        rig.Pump();

        rig.SelectStartTo(rig.Global(150, 200));
        rig.SelectEndTo(rig.Global(150, -20));
        rig.PumpFrames(2);

        double afterFirst = rig.Controller.Offset;
        Assert.True(afterFirst < 600.0, $"expected a backward auto scroll, got {afterFirst}");

        rig.DragEndEdgeFor(frames: 80);
        Assert.Equal(0.0, rig.Controller.Offset, precision: 6);
    }

    [Fact]
    public void ReleasingTheEdge_StopsTheAutoScrollInsteadOfRunningToTheExtent()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, Surface.Height + 20));
        rig.DragEndEdgeFor(frames: 3);
        double afterDrag = rig.Controller.Offset;
        Assert.True(afterDrag > 0.0);

        // No further edge updates arrive after the pointer is released. The drag target is anchored
        // to the scroll origin, so the viewport catches up with it and the scroll stops well short
        // of the max extent.
        rig.PumpFrames(80);
        Assert.True(
            rig.Controller.Offset < rig.Controller.Position.MaxScrollExtent,
            $"a released drag ran to the extent: {rig.Controller.Offset}");
    }

    [Fact]
    public void NeverScrollableScrollPhysics_SuppressesTheAutoScrollEntirely()
    {
        using SelectionRig rig = SelectionRig.Create(physics: new NeverScrollableScrollPhysics());

        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, Surface.Height + 40));
        rig.DragEndEdgeFor(frames: 60);

        Assert.Equal(0.0, rig.Controller.Offset);
        Assert.False(rig.Delegate.IsAutoScrollingForTests);
    }

    [Fact]
    public void SelectionStartedOutsideTheScrollable_SelectsTheWholeContentWithoutScrolling()
    {
        using SelectionRig rig = SelectionRig.Create();

        // Above the scrollable: Dart clamps the inferred position to the scrollable's origin.
        rig.SelectStartTo(rig.Global(150, -40));
        // Below the scrollable: Dart infers Offset.infinite, which selects through to the end.
        rig.SelectEndTo(rig.Global(150, Surface.Height + 40));
        rig.PumpFrames(20);

        Assert.Equal(0.0, rig.Controller.Offset);
        Assert.False(rig.Delegate.IsAutoScrollingForTests);
        Assert.True(rig.Registrar.Value.HasSelection);
    }

    [Fact]
    public void EdgeUpdatesAreReplayedForSelectablesThatScrollBackIntoView()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.Controller.JumpTo(2 * ItemHeight);
        rig.Pump();
        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, 60));
        rig.PumpFrames(2);

        SelectedContentRange? before = rig.Registrar.GetSelection();
        Assert.NotNull(before);

        // Scroll the selected item out of view and back again. The selectable is destroyed and
        // rebuilt, so the delegate has to replay the edge updates it missed.
        rig.Controller.JumpTo(0);
        rig.PumpFrames(2);
        rig.Controller.JumpTo(2 * ItemHeight);
        rig.PumpFrames(2);

        SelectedContentRange? after = rig.Registrar.GetSelection();
        Assert.NotNull(after);
        Assert.Equal(before!.StartOffset, after!.StartOffset);
        Assert.Equal(before.EndOffset, after.EndOffset);
    }

    [Fact]
    public void ScrollingAfterASelection_DoesNotClearIt()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, 60));
        rig.PumpFrames(2);
        Assert.True(rig.Registrar.Value.HasSelection);

        rig.Controller.JumpTo(50);
        rig.PumpFrames(3);

        Assert.True(rig.Registrar.Value.HasSelection);
    }

    [Fact]
    public void ClearSelection_ResetsTheDragOriginsSoTheNextDragReEvaluatesItsStart()
    {
        using SelectionRig rig = SelectionRig.Create();

        // First drag starts outside the scrollable, so no auto scroll may happen.
        rig.SelectStartTo(rig.Global(150, -40));
        rig.SelectEndTo(rig.Global(150, Surface.Height + 40));
        rig.PumpFrames(4);
        Assert.Equal(0.0, rig.Controller.Offset);

        rig.Registrar.DispatchSelectionEvent(new ClearSelectionEvent());
        rig.PumpFrames(2);
        Assert.False(rig.Registrar.Value.HasSelection);

        // The second drag starts inside, so it does auto scroll.
        rig.SelectStartTo(rig.Global(150, 20));
        rig.SelectEndTo(rig.Global(150, Surface.Height + 40));
        rig.DragEndEdgeFor(frames: 4);
        Assert.True(rig.Controller.Offset > 0.0);
    }

    [Fact]
    public void SelectAll_LeavesTheAutoScrollAloneAndReportsASelection()
    {
        using SelectionRig rig = SelectionRig.Create();

        rig.Registrar.DispatchSelectionEvent(new SelectAllSelectionEvent());
        rig.PumpFrames(4);

        Assert.True(rig.Registrar.Value.HasSelection);
        Assert.Equal(0.0, rig.Controller.Offset);
        Assert.False(rig.Delegate.IsAutoScrollingForTests);
    }

    [Fact]
    public void PositionSwap_RepointsTheDelegateAtTheReplacementPosition()
    {
        using var first = new ScrollController();
        using var second = new ScrollController();
        using var registrar = new StaticSelectionContainerDelegate();

        using var harness = new RestorationHarness(Wrap(registrar, BuildList(controller: first)));
        PumpFully(harness);

        var handlerState = (ScrollableSelectionHandlerState)
            ((StatefulElement)FindDescendantElement<ScrollableSelectionHandler>(harness)!).State;
        Assert.Same(first.Position, handlerState.DelegateForTests.Position);

        harness.Update(Wrap(registrar, BuildList(controller: second)));
        PumpFully(harness);

        // Dart's `didUpdateWidget` re-points only the position; the state is never re-assigned.
        Assert.Same(second.Position, handlerState.DelegateForTests.Position);
    }

    // -------------------------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------------------------

    /// The ambient registrar has to be a real <see cref="SelectionContainer"/>, because the
    /// aggregating delegate resolves its own render box through the container's context.
    private static Widget Wrap(SelectionContainerDelegate registrar, Widget child) => new Directionality(
        TextDirection.Ltr,
        new SelectionContainer(registrar, child));

    /// Builds, lays out, then drains the microtask queue the selection registry schedules its
    /// selectable updates on, and lays out again.
    private static void PumpFully(RestorationHarness harness)
    {
        harness.Pump(Surface);
        Scheduler.PumpFrameForTests();
        harness.Pump(Surface);
    }

    private static Widget BuildList(
        ScrollController? controller,
        AxisDirection axisDirection = AxisDirection.Down,
        ScrollPhysics? physics = null,
        Action<BuildContext>? onItemBuilt = null,
        bool withText = true)
    {
        bool vertical = axisDirection is AxisDirection.Up or AxisDirection.Down;
        bool reverse = axisDirection is AxisDirection.Up or AxisDirection.Left;
        return new SizedBox(
            width: Surface.Width,
            height: Surface.Height,
            child: ListView.Builder(
                itemCount: ItemCount,
                itemExtent: ItemHeight,
                controller: controller,
                physics: physics,
                reverse: reverse,
                scrollDirection: vertical ? Axis.Vertical : Axis.Horizontal,
                itemBuilder: (context, index) =>
                {
                    onItemBuilt?.Invoke(context);
                    return new SizedBox(
                        height: ItemHeight,
                        width: ItemHeight,
                        child: withText ? new Text($"Item {index}") : null);
                }));
    }

    private static T? FindDescendantWidget<T>(RestorationHarness harness) where T : Widget =>
        FindDescendantElement<T>(harness)?.Widget as T;

    private static Element? FindDescendantElement<T>(RestorationHarness harness) where T : Widget
    {
        Element? found = null;
        VisitRenderViewElements(harness, element =>
        {
            if (found is null && element.Widget is T)
            {
                found = element;
            }
        });
        return found;
    }

    private static Element? FindDescendantElement<T>(Element root) where T : Widget
    {
        Element? found = null;
        void Visit(Element element)
        {
            if (found is not null)
            {
                return;
            }

            if (!ReferenceEquals(element, root) && element.Widget is T)
            {
                found = element;
                return;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }

    private static Element? FindAncestorElement<T>(Element start) where T : Widget
    {
        Element? current = start.Parent;
        while (current is not null)
        {
            if (current.Widget is T)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static void VisitRenderViewElements(RestorationHarness harness, Action<Element> visitor)
    {
        Element root = harness.RootElement;

        void Visit(Element element)
        {
            visitor(element);
            element.VisitChildren(Visit);
        }

        Visit(root);
    }

    /// <summary>
    /// The tree Dart's selection tests build: a registrar over a scrollable, plus the
    /// <c>SelectableRegion</c> half of the protocol (the <c>pending</c> retry that keeps feeding the
    /// same edge update while the scrollable auto scrolls).
    /// </summary>
    private sealed class SelectionRig : IDisposable
    {
        private readonly RestorationHarness _harness;
        private Point? _endPosition;

        // One monotonic clock for the whole rig: the auto scroller's ticker reads the frame
        // timestamp, so a per-call counter would make time run backwards between pump loops.
        private int _frame;

        private SelectionRig(
            RestorationHarness harness,
            StaticSelectionContainerDelegate registrar,
            ScrollController controller,
            ScrollableSelectionContainerDelegate scrollableDelegate,
            RenderBox scrollableBox)
        {
            _harness = harness;
            Registrar = registrar;
            Controller = controller;
            Delegate = scrollableDelegate;
            ScrollableBox = scrollableBox;
        }

        public StaticSelectionContainerDelegate Registrar { get; }

        public ScrollController Controller { get; }

        public ScrollableSelectionContainerDelegate Delegate { get; }

        public RenderBox ScrollableBox { get; }

        public static SelectionRig Create(ScrollPhysics? physics = null, bool withText = true)
        {
            var registrar = new StaticSelectionContainerDelegate();
            var controller = new ScrollController();
            var harness = new RestorationHarness(
                Wrap(registrar, BuildList(controller: controller, physics: physics, withText: withText)));
            PumpFully(harness);

            Element handler = FindDescendantElement<ScrollableSelectionHandler>(harness)!;
            var handlerState = (ScrollableSelectionHandlerState)((StatefulElement)handler).State;
            Element scrollableElement = FindDescendantElement<Scrollable>(harness)!;
            var scrollableState = (Scrollable.ScrollableState)((StatefulElement)scrollableElement).State;
            var box = (RenderBox)scrollableState.Context.FindRenderObject()!;
            return new SelectionRig(
                harness,
                registrar,
                controller,
                handlerState.DelegateForTests,
                box);
        }

        /// A point in global coordinates, given relative to the scrollable's top-left corner.
        public Point Global(double dx, double dy) => ScrollableBox.LocalToGlobal(new Point(dx, dy));

        public void Pump() => PumpFully(_harness);

        public void PumpFrames(int frames)
        {
            for (int i = 0; i < frames; i += 1)
            {
                _harness.Pump(Surface);
                Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(50 * ++_frame));
                _harness.Pump(Surface);
            }
        }

        public void SelectStartTo(Point globalPosition)
        {
            Registrar.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(globalPosition));
        }

        public void SelectEndTo(Point globalPosition)
        {
            _endPosition = globalPosition;
            Registrar.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(globalPosition));
        }

        /// Dart's `SelectableRegion._triggerSelectionEndEdgeUpdate` loop: while the pointer is held
        /// down, the same edge update is re-dispatched every frame.
        public void DragEndEdgeFor(int frames)
        {
            for (int i = 0; i < frames; i += 1)
            {
                _harness.Pump(Surface);
                Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(50 * ++_frame));
                _harness.Pump(Surface);
                if (_endPosition is { } position)
                {
                    Registrar.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(position));
                }
            }
        }

        public void Dispose()
        {
            _harness.Dispose();
            Controller.Dispose();
            Registrar.Dispose();
        }
    }
}
