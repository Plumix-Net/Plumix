using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/drag_target.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class DragTargetTests
{
    [Fact]
    public void DraggableAndDragTarget_DefaultsAndGuardsMatchFlutter()
    {
        var child = new SizedBox(width: 20, height: 20);
        var feedback = new SizedBox(width: 20, height: 20);
        var draggable = new Draggable<string>(
            child: child,
            feedback: feedback,
            data: "payload");

        Assert.Same(child, draggable.Child);
        Assert.Same(feedback, draggable.Feedback);
        Assert.Equal("payload", draggable.Data);
        Assert.Null(draggable.Axis);
        Assert.Null(draggable.ChildWhenDragging);
        Assert.Equal(default, draggable.FeedbackOffset);
        Assert.Null(draggable.Affinity);
        Assert.Null(draggable.MaxSimultaneousDrags);
        Assert.True(draggable.IgnoringFeedbackSemantics);
        Assert.True(draggable.IgnoringFeedbackPointer);
        Assert.False(draggable.RootOverlay);
        Assert.Equal(HitTestBehavior.DeferToChild, draggable.HitTestBehavior);
        Assert.Null(draggable.AllowedButtonsFilter);

        var target = new DragTarget<string>(
            builder: (_, _, _) => new SizedBox(width: 40, height: 40));
        Assert.Equal(HitTestBehavior.Translucent, target.HitTestBehavior);
        Assert.Null(target.OnWillAcceptWithDetails);
        Assert.Null(target.OnAcceptWithDetails);
        Assert.Null(target.OnLeave);
        Assert.Null(target.OnMove);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Draggable<string>(
            child,
            feedback,
            maxSimultaneousDrags: -1));
        Assert.Throws<ArgumentException>(() => new DragTarget<string>(
            builder: (_, _, _) => child,
            onWillAccept: _ => true,
            onWillAcceptWithDetails: _ => true));
    }

    [Fact]
    public void LongPressDraggable_DefaultsAndDelayedStartMatchFlutter()
    {
        var child = new SizedBox(width: 20, height: 20);
        var feedback = new SizedBox(width: 18, height: 18);
        var draggable = new LongPressDraggable<string>(
            child: child,
            feedback: feedback,
            data: "payload");

        Assert.Same(child, draggable.Child);
        Assert.Same(feedback, draggable.Feedback);
        Assert.Equal("payload", draggable.Data);
        Assert.True(draggable.HapticFeedbackOnStart);
        Assert.Equal(TimeSpan.FromMilliseconds(500), draggable.Delay);
        Assert.Null(draggable.Affinity);
        Assert.True(draggable.IgnoringFeedbackSemantics);
        Assert.True(draggable.IgnoringFeedbackPointer);
        Assert.False(draggable.RootOverlay);
        Assert.Equal(HitTestBehavior.DeferToChild, draggable.HitTestBehavior);

        int starts = 0;
        var delayedEntry = new OverlayEntry(_ =>
            new LongPressDraggable<string>(
                child: new SizedBox(width: 40, height: 40),
                feedback: new SizedBox(width: 20, height: 20),
                delay: TimeSpan.FromSeconds(5),
                hitTestBehavior: HitTestBehavior.Opaque,
                onDragStarted: () => starts += 1));
        using var harness = new WidgetHarness(new Overlay(initialEntries: [delayedEntry]));
        DateTime now = DateTime.UtcNow;
        harness.Dispatch(new PointerDownEvent(
            10,
            PointerDeviceKind.Touch,
            new Point(10, 10),
            PointerButtons.Primary,
            now));
        Assert.Equal(0, starts);

        harness.Dispatch(new PointerUpEvent(
            10,
            PointerDeviceKind.Touch,
            new Point(10, 10),
            PointerButtons.None,
            now.AddMilliseconds(50)));
        Assert.Equal(0, starts);
    }

    [Fact]
    public void LongPressDraggable_StartsAfterDelayAndEmitsSelectionHaptic()
    {
        int starts = 0;
        var feedbackEvents = new List<FeedbackType>();
        Feedback.ResetForTests();
        Feedback.FeedbackTriggered += feedbackEvents.Add;

        try
        {
            var entry = new OverlayEntry(_ =>
                new LongPressDraggable<string>(
                    child: new SizedBox(width: 40, height: 40),
                    feedback: new SizedBox(width: 20, height: 20),
                    delay: TimeSpan.Zero,
                    hitTestBehavior: HitTestBehavior.Opaque,
                    onDragStarted: () => starts += 1));
            using var harness = new WidgetHarness(new Overlay(initialEntries: [entry]));
            DateTime now = DateTime.UtcNow;

            harness.Dispatch(new PointerDownEvent(
                11,
                PointerDeviceKind.Touch,
                new Point(10, 10),
                PointerButtons.Primary,
                now));
            harness.Pump();

            Assert.Equal(1, starts);
            Assert.Equal([FeedbackType.SelectionClick], feedbackEvents);

            harness.Dispatch(new PointerUpEvent(
                11,
                PointerDeviceKind.Touch,
                new Point(10, 10),
                PointerButtons.None,
                now.AddMilliseconds(20)));
            harness.Pump();
        }
        finally
        {
            Feedback.ResetForTests();
        }
    }

    [Fact]
    public void OverlayEntry_InsertRebuildRemoveLifecycleMatchesFlutter()
    {
        int builds = 0;
        var baseEntry = new OverlayEntry(_ => new SizedBox(width: 20, height: 20));
        using var harness = new WidgetHarness(
            new Overlay(
                initialEntries: [baseEntry]));

        OverlayState state = harness.FindState<OverlayState>();
        Assert.True(baseEntry.Mounted);
        Assert.Single(state.Entries);

        var inserted = new OverlayEntry(_ =>
        {
            builds += 1;
            return new Positioned(
                left: 10,
                top: 12,
                child: new SizedBox(width: 8, height: 9));
        });
        state.Insert(inserted);
        harness.Pump();

        Assert.True(inserted.Mounted);
        Assert.Equal(1, builds);
        Assert.Equal(2, state.Entries.Count);

        inserted.MarkNeedsBuild();
        harness.Pump();
        Assert.Equal(2, builds);

        inserted.Remove();
        harness.Pump();
        Assert.False(inserted.Mounted);
        Assert.Single(state.Entries);
        inserted.Dispose();
    }

    [Fact]
    public void OverlayEntry_OpaqueAndMaintainStateMutationsUpdateVisibilityAndMounting()
    {
        int baseBuilds = 0;
        int mountChanges = 0;
        var baseEntry = new OverlayEntry(_ =>
        {
            baseBuilds += 1;
            return new SizedBox(width: 20, height: 20);
        });
        var opaqueEntry = new OverlayEntry(
            _ => new SizedBox(width: 20, height: 20),
            opaque: true);

        using (var harness = new WidgetHarness(
                   new Overlay(initialEntries: [baseEntry, opaqueEntry])))
        {
            OverlayState state = harness.FindState<OverlayState>();
            Assert.False(baseEntry.Mounted);
            Assert.True(opaqueEntry.Mounted);
            Assert.False(state.DebugIsVisible(baseEntry));

            baseEntry.AddListener(() => mountChanges += 1);
            baseEntry.MaintainState = true;
            harness.Pump();

            Assert.True(baseEntry.Mounted);
            Assert.Equal(1, baseBuilds);
            Assert.Equal(1, mountChanges);
            Assert.False(state.DebugIsVisible(baseEntry));

            opaqueEntry.Opaque = false;
            harness.Pump();
            Assert.True(state.DebugIsVisible(baseEntry));
            Assert.Equal(1, mountChanges);

            opaqueEntry.Opaque = true;
            baseEntry.MaintainState = false;
            harness.Pump();
            Assert.False(baseEntry.Mounted);
            Assert.Equal(2, mountChanges);
        }

        baseEntry.Dispose();
        opaqueEntry.Dispose();
    }

    [Fact]
    public void OverlayState_RearrangePreservesEntriesAndInsertsNewOnesAtomically()
    {
        var first = new OverlayEntry(_ => new SizedBox(width: 10, height: 10));
        var second = new OverlayEntry(_ => new SizedBox(width: 12, height: 12));
        var third = new OverlayEntry(_ => new SizedBox(width: 14, height: 14));
        var inserted = new OverlayEntry(_ => new SizedBox(width: 16, height: 16));

        using (var harness = new WidgetHarness(
                   new Overlay(initialEntries: [first, second, third])))
        {
            OverlayState state = harness.FindState<OverlayState>();
            state.Rearrange([third, first], below: third);
            harness.Pump();
            Assert.Equal([second, third, first], state.Entries);

            state.Rearrange([inserted, first]);
            harness.Pump();
            Assert.Equal([inserted, first, second, third], state.Entries);
            Assert.True(inserted.Mounted);

            Assert.Throws<ArgumentException>(() =>
                state.Rearrange([first, first]));
            Assert.Throws<ArgumentException>(() =>
                state.Rearrange([first], above: third));
        }

        first.Dispose();
        second.Dispose();
        third.Dispose();
        inserted.Dispose();
    }

    [Fact]
    public void Overlay_CanSizeOverlayAndWrapSizeUnboundedTheater()
    {
        Widget wrapped = Overlay.Wrap(
            child: new SizedBox(width: 80, height: 42),
            alwaysSizeToContent: true);
        using var harness = new WidgetHarness(
            new UnconstrainedBox(child: wrapped));

        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        Assert.Equal(new Size(80, 42), theater.Size);
        Assert.True(theater.AlwaysSizeToContent);
        Assert.Equal(Clip.HardEdge, theater.ClipBehavior);
    }

    [Fact]
    public void Draggable_DropOnAcceptingTargetRunsSourceAndTargetLifecycle()
    {
        var candidateSnapshots = new List<IReadOnlyList<string?>>();
        var rejectedSnapshots = new List<IReadOnlyList<object?>>();
        var moves = new List<DragTargetDetails<string>>();
        DragTargetDetails<string>? acceptedDetails = null;
        DraggableDetails? endDetails = null;
        int starts = 0;
        int completions = 0;
        int cancellations = 0;

        var contentEntry = new OverlayEntry(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new Draggable<string>(
                            data: "plum",
                            child: new ColoredBox(Colors.CornflowerBlue),
                            childWhenDragging: new ColoredBox(Colors.LightGray),
                            feedback: new SizedBox(width: 30, height: 30),
                            hitTestBehavior: HitTestBehavior.Opaque,
                            onDragStarted: () => starts += 1,
                            onDragCompleted: () => completions += 1,
                            onDraggableCanceled: (_, _) => cancellations += 1,
                            onDragEnd: details => endDetails = details)),
                    new Positioned(
                        left: 100,
                        top: 0,
                        width: 80,
                        height: 80,
                        child: new DragTarget<string>(
                            builder: (_, candidates, rejected) =>
                            {
                                candidateSnapshots.Add(candidates.ToArray());
                                rejectedSnapshots.Add(rejected.ToArray());
                                return new SizedBox(width: 80, height: 80);
                            },
                            onWillAcceptWithDetails: details => details.Data == "plum",
                            onMove: details => moves.Add(details),
                            onAcceptWithDetails: details => acceptedDetails = details)),
                ]));

        using var harness = new WidgetHarness(
            new Overlay(initialEntries: [contentEntry]));
        DateTime now = DateTime.UtcNow;
        harness.Dispatch(new PointerDownEvent(
            1,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.Primary,
            now));
        harness.Pump();

        Assert.Equal(1, starts);
        Assert.Contains(
            harness.FindWidgets<ColoredBox>(),
            box => box.Color == Colors.LightGray);

        harness.Dispatch(new PointerMoveEvent(
            1,
            PointerDeviceKind.Mouse,
            new Point(120, 20),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(20)));
        harness.Pump();

        Assert.Contains(candidateSnapshots, snapshot => snapshot.SequenceEqual(["plum"]));
        Assert.DoesNotContain(rejectedSnapshots, snapshot => snapshot.Count > 0);
        Assert.NotEmpty(moves);
        Assert.Equal("plum", moves[^1].Data);
        Assert.Equal(new Point(110, 10), moves[^1].Offset);

        harness.Dispatch(new PointerUpEvent(
            1,
            PointerDeviceKind.Mouse,
            new Point(120, 20),
            PointerButtons.None,
            now.AddMilliseconds(40)));
        harness.Pump();

        Assert.Equal("plum", acceptedDetails?.Data);
        Assert.Equal(new Point(110, 10), acceptedDetails?.Offset);
        Assert.Equal(1, completions);
        Assert.Equal(0, cancellations);
        Assert.True(endDetails?.WasAccepted);
        Assert.Equal(new Point(110, 10), endDetails?.Offset);
        Assert.DoesNotContain(
            harness.FindWidgets<ColoredBox>(),
            box => box.Color == Colors.LightGray);
    }

    [Fact]
    public void DragTarget_RejectionThenLeaveReportsRejectedDataAndCancellation()
    {
        var candidateSnapshots = new List<IReadOnlyList<string?>>();
        var rejectedSnapshots = new List<IReadOnlyList<object?>>();
        var leaves = new List<string?>();
        Point? canceledOffset = null;
        bool? wasAccepted = null;

        var entry = new OverlayEntry(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new Draggable<string>(
                            data: "rejected",
                            child: new SizedBox(width: 40, height: 40),
                            feedback: new SizedBox(width: 20, height: 20),
                            hitTestBehavior: HitTestBehavior.Opaque,
                            onDraggableCanceled: (_, offset) => canceledOffset = offset,
                            onDragEnd: details => wasAccepted = details.WasAccepted)),
                    new Positioned(
                        left: 80,
                        top: 0,
                        width: 60,
                        height: 60,
                        child: new DragTarget<string>(
                            builder: (_, candidates, rejected) =>
                            {
                                candidateSnapshots.Add(candidates.ToArray());
                                rejectedSnapshots.Add(rejected.ToArray());
                                return new SizedBox(width: 60, height: 60);
                            },
                            onWillAccept: _ => false,
                            onLeave: data => leaves.Add(data))),
                ]));
        using var harness = new WidgetHarness(new Overlay(initialEntries: [entry]));
        DateTime now = DateTime.UtcNow;

        harness.Dispatch(new PointerDownEvent(
            2,
            PointerDeviceKind.Touch,
            new Point(10, 10),
            PointerButtons.Primary,
            now));
        harness.Dispatch(new PointerMoveEvent(
            2,
            PointerDeviceKind.Touch,
            new Point(100, 20),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(20)));
        harness.Pump();

        Assert.Contains(
            rejectedSnapshots,
            snapshot => snapshot.SequenceEqual(new object?[] { "rejected" }));
        Assert.DoesNotContain(candidateSnapshots, snapshot => snapshot.Count > 0);

        harness.Dispatch(new PointerMoveEvent(
            2,
            PointerDeviceKind.Touch,
            new Point(180, 80),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(40)));
        harness.Dispatch(new PointerUpEvent(
            2,
            PointerDeviceKind.Touch,
            new Point(180, 80),
            PointerButtons.None,
            now.AddMilliseconds(60)));
        harness.Pump();

        Assert.Equal(["rejected"], leaves);
        Assert.False(wasAccepted);
        Assert.Equal(new Point(170, 70), canceledOffset);
    }

    [Fact]
    public void Draggable_AxisAndButtonFilterRestrictMotionAndActivation()
    {
        int starts = 0;
        int updates = 0;
        DraggableDetails? endDetails = null;
        var entry = new OverlayEntry(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new Draggable<int>(
                            data: 7,
                            axis: Axis.Horizontal,
                            child: new SizedBox(width: 40, height: 40),
                            feedback: new SizedBox(width: 20, height: 20),
                            hitTestBehavior: HitTestBehavior.Opaque,
                            allowedButtonsFilter: buttons => buttons == PointerButtons.Primary,
                            onDragStarted: () => starts += 1,
                            onDragUpdate: _ => updates += 1,
                            onDragEnd: details => endDetails = details)),
                ]));
        using var harness = new WidgetHarness(new Overlay(initialEntries: [entry]));
        DateTime now = DateTime.UtcNow;

        harness.Dispatch(new PointerDownEvent(
            3,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.Secondary,
            now));
        harness.Dispatch(new PointerUpEvent(
            3,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.None,
            now.AddMilliseconds(10)));
        Assert.Equal(0, starts);

        harness.Dispatch(new PointerDownEvent(
            4,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.Primary,
            now.AddMilliseconds(20)));
        harness.Dispatch(new PointerMoveEvent(
            4,
            PointerDeviceKind.Mouse,
            new Point(90, 70),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(40)));
        harness.Dispatch(new PointerUpEvent(
            4,
            PointerDeviceKind.Mouse,
            new Point(90, 70),
            PointerButtons.None,
            now.AddMilliseconds(60)));
        harness.Pump();

        Assert.Equal(1, starts);
        Assert.Equal(1, updates);
        Assert.Equal(new Point(80, 0), endDetails?.Offset);
        Assert.Equal(0.0, endDetails?.Velocity.PixelsPerSecond.Y);
    }

    [Fact]
    public void DragTarget_NestedRejectedTargetFallsBackToAcceptingAncestor()
    {
        int innerLeaves = 0;
        int outerAccepts = 0;
        bool? wasAccepted = null;
        var entry = new OverlayEntry(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new Draggable<string>(
                            data: "plum",
                            child: new SizedBox(width: 40, height: 40),
                            feedback: new SizedBox(width: 20, height: 20),
                            hitTestBehavior: HitTestBehavior.Opaque,
                            onDragEnd: details => wasAccepted = details.WasAccepted)),
                    new Positioned(
                        left: 80,
                        top: 0,
                        width: 80,
                        height: 80,
                        child: new DragTarget<string>(
                            onWillAcceptWithDetails: _ => true,
                            onAcceptWithDetails: _ => outerAccepts += 1,
                            builder: (_, _, _) => new DragTarget<string>(
                                onWillAcceptWithDetails: _ => false,
                                onLeave: _ => innerLeaves += 1,
                                builder: (_, _, _) => new SizedBox(width: 80, height: 80)))),
                ]));
        using var harness = new WidgetHarness(new Overlay(initialEntries: [entry]));
        DateTime now = DateTime.UtcNow;

        harness.Dispatch(new PointerDownEvent(
            5,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.Primary,
            now));
        harness.Dispatch(new PointerMoveEvent(
            5,
            PointerDeviceKind.Mouse,
            new Point(100, 20),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(20)));
        harness.Dispatch(new PointerUpEvent(
            5,
            PointerDeviceKind.Mouse,
            new Point(100, 20),
            PointerButtons.None,
            now.AddMilliseconds(40)));
        harness.Pump();

        Assert.Equal(1, outerAccepts);
        Assert.Equal(1, innerLeaves);
        Assert.True(wasAccepted);
    }

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly TestRootElement _root;
        private readonly RenderView _renderView;
        private readonly PipelineOwner _pipeline;

        public WidgetHarness(Widget widget)
        {
            GestureBinding.Instance.ResetForTests();
            _root = new TestRootElement(widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            _renderView = new RenderView
            {
                Child = Assert.IsAssignableFrom<RenderBox>(_root.ChildElement?.RenderObject),
            };
            _pipeline = new PipelineOwner(_renderView);
            _pipeline.Attach(_renderView);
            Pump();
        }

        public void Dispatch(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(_renderView, @event);
        }

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.FlushLayout(new Size(240, 120));
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            Visit(_root);
            return widgets;

            void Visit(Element element)
            {
                if (element.Widget is T widget)
                {
                    widgets.Add(widget);
                }

                element.VisitChildren(Visit);
            }
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

        public T FindRenderObject<T>() where T : RenderObject
        {
            T? result = null;
            Visit(_renderView);
            return Assert.IsType<T>(result);

            void Visit(RenderObject renderObject)
            {
                if (result is not null)
                {
                    return;
                }

                if (renderObject is T typed)
                {
                    result = typed;
                    return;
                }

                renderObject.VisitChildren(Visit);
            }
        }

        public void Dispose()
        {
            GestureBinding.Instance.ResetForTests();
            _root.Unmount();
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
