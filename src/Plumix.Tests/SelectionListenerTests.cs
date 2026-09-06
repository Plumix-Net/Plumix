using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// Parity coverage for the `SelectionListener`/`SelectionListenerNotifier`/`SelectionDetails`
/// observer surface of `widgets/selectable_region.dart`.
[Collection(SchedulerTestCollection.Name)]
public sealed class SelectionListenerTests
{
    [Fact]
    public void SelectionListener_ComposesASelectionContainerAndRegistersTheNotifier()
    {
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();
        Assert.False(notifier.Registered);

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(
                    notifier,
                    new Column(children: [new Text("How are you?")])))));
        harness.Pump(new Size(400, 400));

        Assert.True(notifier.Registered);
        Assert.Single(registrar.Selectables);
    }

    [Fact]
    public void SelectionNotifier_SelectionThrowsBeforeRegistration()
    {
        var notifier = new SelectionListenerNotifier();

        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => notifier.Selection);
        Assert.Equal("Selection client has not been registered to this notifier.", error.Message);
    }

    [DebugOnlyFact]
    public void SelectionNotifier_RejectsASecondRegistration()
    {
        var notifier = new SelectionListenerNotifier();
        using var first = new SelectionListenerDelegate(notifier);

        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => new SelectionListenerDelegate(notifier));
        Assert.Equal(
            "This SelectionListenerNotifier is already registered to another SelectionListener. "
            + "Try providing a new SelectionListenerNotifier.",
            error.Message);
    }

    [Fact]
    public void SelectionListenerDelegate_SwallowsOnlyTheInitialNoSelectionNotification()
    {
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();
        int notifications = 0;
        notifier.AddListener(() => notifications += 1);

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(
                    notifier,
                    new Column(children: [new Text("How are you?")])))));
        harness.Pump(new Size(400, 400));

        // Content arriving changes the geometry, but the initial no-selection value is swallowed.
        Assert.Equal(0, notifications);

        ISelectable selectable = registrar.Selectables.Single();
        selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());
        Assert.Equal(1, notifications);

        // After the initial notification, no-selection geometry changes are forwarded too.
        selectable.DispatchSelectionEvent(new ClearSelectionEvent());
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void SelectionListener_ReportsRangeAndStatusAcrossSelectables()
    {
        // Mirrors `selectable_region_test.dart` > 'onSelectionChanged SelectedContentRange is
        // accurate': offsets accumulate across the three texts (12 + 14 + 16 characters).
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(
                    notifier,
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        children:
                        [
                            new Text("How are you?"),
                            new Text("Good, and you?"),
                            new Text("Fine, thank you."),
                        ])))));
        harness.Pump(new Size(400, 400));

        ISelectable selectable = registrar.Selectables.Single();
        selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());

        Assert.Equal(SelectionStatus.Uncollapsed, notifier.Selection.Status);
        Assert.Equal(new SelectedContentRange(0, 42), notifier.Selection.Range);

        selectable.DispatchSelectionEvent(new ClearSelectionEvent());
        Assert.Equal(SelectionStatus.None, notifier.Selection.Status);
        Assert.Null(notifier.Selection.Range);
    }

    [Fact]
    public void SelectionListener_CountsWidgetSpanContentAcrossParagraphs()
    {
        // Mirrors `selectable_region_test.dart` > 'SelectionListener onSelectionChanged is
        // accurate with WidgetSpans': the placeholder belongs to no fragment, so offsets run
        // through 'Hello world, ' (13) into the nested text (18).
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(
                    notifier,
                    new Column(children:
                    [
                        Text.Rich(new TextSpan(children:
                        [
                            new TextSpan("Hello world, "),
                            new WidgetSpan(new Text("how are you today.")),
                        ])),
                    ])))));
        harness.Pump(new Size(400, 400));

        ISelectable selectable = registrar.Selectables.Single();
        selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());

        Assert.Equal(SelectionStatus.Uncollapsed, notifier.Selection.Status);
        Assert.Equal(new SelectedContentRange(0, 31), notifier.Selection.Range);
    }

    [Fact]
    public void SelectionListener_CollapsedAndBackwardsSelectionsReportDartRanges()
    {
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(
                    notifier,
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        children:
                        [
                            new Text("How are you?"),
                            new Text("Good, and you?"),
                            new Text("Fine, thank you."),
                        ])))));
        harness.Pump(new Size(400, 400));

        ISelectable selectable = registrar.Selectables.Single();

        // A collapsed selection reports a collapsed range, not null.
        selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(new Point(1, 4)));
        selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(new Point(1, 4)));
        Assert.Equal(SelectionStatus.Collapsed, notifier.Selection.Status);
        SelectedContentRange collapsed = Assert.IsType<SelectedContentRange>(notifier.Selection.Range);
        Assert.Equal(collapsed.StartOffset, collapsed.EndOffset);

        // A backwards selection reports startOffset > endOffset.
        selectable.DispatchSelectionEvent(new ClearSelectionEvent());
        selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(new Point(390, 396)));
        selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(new Point(1, 4)));
        Assert.Equal(SelectionStatus.Uncollapsed, notifier.Selection.Status);
        SelectedContentRange backwards = Assert.IsType<SelectedContentRange>(notifier.Selection.Range);
        Assert.True(
            backwards.StartOffset > backwards.EndOffset,
            $"Expected a backwards range, got {backwards}.");
    }

    [Fact]
    public void SelectionListener_SwapsNotifiersOnWidgetUpdate()
    {
        var registrar = new RecordingRegistrar();
        var firstNotifier = new SelectionListenerNotifier();
        var secondNotifier = new SelectionListenerNotifier();
        int firstNotifications = 0;
        int secondNotifications = 0;
        firstNotifier.AddListener(() => firstNotifications += 1);
        secondNotifier.AddListener(() => secondNotifications += 1);
        SelectionListenerNotifier currentNotifier = firstNotifier;
        StateSetter? setState = null;

        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new StatefulBuilder((context, setter) =>
                {
                    setState = setter;
                    return new SelectionListener(
                        currentNotifier,
                        new Column(children: [new Text("How are you?")]));
                }))));
        harness.Pump(new Size(400, 400));

        Assert.True(firstNotifier.Registered);
        Assert.False(secondNotifier.Registered);

        setState!(() => currentNotifier = secondNotifier);
        harness.Pump(new Size(400, 400));

        Assert.False(firstNotifier.Registered);
        Assert.True(secondNotifier.Registered);

        ISelectable selectable = registrar.Selectables.Single();
        selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());
        Assert.Equal(0, firstNotifications);
        Assert.Equal(1, secondNotifications);
    }

    [Fact]
    public void SelectionListener_UnmountingUnregistersTheNotifier()
    {
        var registrar = new RecordingRegistrar();
        var notifier = new SelectionListenerNotifier();

        var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionListener(notifier, new Text("How are you?")))));
        harness.Pump(new Size(400, 400));
        Assert.True(notifier.Registered);

        harness.Dispose();
        Assert.False(notifier.Registered);

        // Disposing the notifier afterwards, as Flutter's tests do in their teardown, is safe.
        notifier.Dispose();
    }

    [Fact]
    public void SelectionNotifier_DisposeUnregistersItself()
    {
        var notifier = new SelectionListenerNotifier();
        using var listenerDelegate = new SelectionListenerDelegate(notifier);
        Assert.True(notifier.Registered);

        notifier.Dispose();
        Assert.False(notifier.Registered);
    }

    private sealed class RecordingRegistrar : ISelectionRegistrar
    {
        public List<ISelectable> Selectables { get; } = [];

        public void Add(ISelectable selectable) => Selectables.Add(selectable);

        public void Remove(ISelectable selectable) => Selectables.Remove(selectable);
    }

    private sealed class WidgetRenderHarness : IDisposable
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
            Scheduler.PumpFrameForTests();
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _rootElement.Unmount();
            FocusManager.Instance.ResetForTests();
            GestureBinding.Instance.ResetForTests();
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

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
