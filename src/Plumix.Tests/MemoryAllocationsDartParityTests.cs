using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/test/foundation/memory_allocations_test.dart
// flutter/packages/flutter/test/rendering/memory_allocations_test.dart
// flutter/packages/flutter/test/widgets/memory_allocations_test.dart
// and the per-class "dispatches memory events" tests named on each test below.

namespace Plumix.Tests;

/// <remarks>
/// Listeners are only registered while <see cref="FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled"/>
/// (debug builds), so the registry's own tests are debug-only; the publisher tests assert that nothing
/// is dispatched in the other configurations.
/// </remarks>
public sealed class MemoryAllocationsDartParityTests
{
    private static readonly FlutterMemoryAllocations Ma = FlutterMemoryAllocations.Instance;

    // Dart's setUp: no listener is left over and the dart:ui hooks are unset.
    public MemoryAllocationsDartParityTests()
    {
        Assert.False(Ma.HasListeners);
        CheckSdkHandlersNotSet();
    }

    [DebugOnlyFact]
    public void AddListenerAndRemoveListener_AddAndRemoveListeners()
    {
        ObjectEvent @event = new ObjectDisposed("object");
        ObjectEvent? receivedEvent = null;
        void Listener(ObjectEvent e) => receivedEvent = e;
        Assert.False(Ma.HasListeners);

        Ma.AddListener(Listener);
        CheckSdkHandlersSet();
        Ma.DispatchObjectEvent(@event);
        Assert.Same(@event, receivedEvent);
        Assert.True(Ma.HasListeners);
        receivedEvent = null;

        Ma.RemoveListener(Listener);
        Ma.DispatchObjectEvent(@event);
        Assert.Null(receivedEvent);
        Assert.False(Ma.HasListeners);
        CheckSdkHandlersNotSet();
    }

    [DebugOnlyFact]
    public void DispatchObjectEvent_HandlesBadListeners()
    {
        ObjectEvent @event = new ObjectDisposed("object");
        var log = new List<string>();
        void BadListener1(ObjectEvent e)
        {
            log.Add("badListener1");
            throw new ArgumentException();
        }

        void Listener1(ObjectEvent e) => log.Add("listener1");
        void BadListener2(ObjectEvent e)
        {
            log.Add("badListener2");
            throw new ArgumentException();
        }

        void Listener2(ObjectEvent e) => log.Add("listener2");

        Ma.AddListener(BadListener1);
        CheckSdkHandlersSet();
        Ma.AddListener(Listener1);
        Ma.AddListener(BadListener2);
        Ma.AddListener(Listener2);

        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previousOnError = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        try
        {
            Ma.DispatchObjectEvent(@event);
        }
        finally
        {
            FlutterError.OnError = previousOnError;
        }

        Assert.Equal(["badListener1", "listener1", "badListener2", "listener2"], log);
        // Dart: tester.takeException() contains 'Multiple exceptions (2)'.
        Assert.Equal(2, errors.Count);
        Assert.All(errors, details =>
        {
            Assert.IsType<ArgumentException>(details.Exception);
            Assert.Equal("foundation library", details.Library);
            Assert.Equal(
                "MemoryAllocations while dispatching notifications for String",
                details.Context!.ToString());
        });

        Ma.RemoveListener(BadListener1);
        CheckSdkHandlersSet();
        Ma.RemoveListener(Listener1);
        Ma.RemoveListener(BadListener2);
        Ma.RemoveListener(Listener2);
        CheckSdkHandlersNotSet();

        log.Clear();
        Assert.False(Ma.HasListeners);
        Ma.DispatchObjectEvent(@event);
        Assert.Empty(log);
    }

    [DebugOnlyFact]
    public void DispatchObjectEvent_DoesNotInvokeConcurrentlyAddedListeners()
    {
        ObjectEvent @event = new ObjectDisposed("object");
        var log = new List<string>();

        void Listener2(ObjectEvent e) => log.Add("listener2");
        void Listener1(ObjectEvent e)
        {
            log.Add("listener1");
            Ma.AddListener(Listener2);
        }

        Ma.AddListener(Listener1);
        CheckSdkHandlersSet();

        Ma.DispatchObjectEvent(@event);
        Assert.Equal(["listener1"], log);
        log.Clear();

        Ma.DispatchObjectEvent(@event);
        Assert.Equal(["listener1", "listener2"], log);
        log.Clear();

        Ma.RemoveListener(Listener1);
        Ma.RemoveListener(Listener2);
        CheckSdkHandlersNotSet();

        Assert.False(Ma.HasListeners);
        Ma.DispatchObjectEvent(@event);
        Assert.Empty(log);
    }

    [DebugOnlyFact]
    public void DispatchObjectEvent_DoesNotInvokeConcurrentlyRemovedListeners()
    {
        ObjectEvent @event = new ObjectDisposed("object");
        var log = new List<string>();

        void Listener2(ObjectEvent e) => log.Add("listener2");
        void Listener1(ObjectEvent e)
        {
            log.Add("listener1");
            Ma.RemoveListener(Listener2);
            Assert.False(Ma.HasListeners);
        }

        Ma.AddListener(Listener1);
        Ma.AddListener(Listener2);

        Ma.DispatchObjectEvent(@event);
        Assert.Equal(["listener1"], log);
        log.Clear();

        Ma.RemoveListener(Listener1);
        CheckSdkHandlersNotSet();

        Assert.False(Ma.HasListeners);
    }

    [DebugOnlyFact]
    public void LastRemoveListener_UnsubscribesFromFlutterSdkEvents()
    {
        void Listener1(ObjectEvent e)
        {
        }

        void Listener2(ObjectEvent e)
        {
        }

        Ma.AddListener(Listener1);
        CheckSdkHandlersSet();

        Ma.AddListener(Listener2);
        CheckSdkHandlersSet();

        Ma.RemoveListener(Listener1);
        CheckSdkHandlersSet();

        Ma.RemoveListener(Listener2);
        CheckSdkHandlersNotSet();
    }

    [DebugOnlyFact]
    public void KFlutterMemoryAllocationsEnabled_IsTrueInDebugMode()
    {
        Assert.True(FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled);
    }

    [Fact]
    public void FoundationPublishers_DispatchEventsInDebugMode()
    {
        int eventCount = 0;
        void Listener(ObjectEvent e) => eventCount++;
        Ma.AddListener(Listener);

        int expectedEventCount = ActivateFoundationObjectsAndReturnCountOfEvents();
        Assert.Equal(Constants.KDebugMode ? expectedEventCount : 0, eventCount);

        Ma.RemoveListener(Listener);
        CheckSdkHandlersNotSet();
        Assert.False(Ma.HasListeners);
    }

    // rendering/memory_allocations_test.dart: "Publishers dispatch events in debug mode".
    [Fact]
    public void RenderingPublishers_DispatchEventsInDebugMode()
    {
        int eventCount = 0;
        void Listener(ObjectEvent e) => eventCount++;
        Ma.AddListener(Listener);

        int count = 0;
        RenderObject renderObject = new TestRenderObject();
        count++;
        Layer layer = new TestLayer();
        count++;

        renderObject.Dispose();
        count++;
        layer.Dispose();
        count++;

        Assert.Equal(Constants.KDebugMode ? count : 0, eventCount);

        Ma.RemoveListener(Listener);
        Assert.False(Ma.HasListeners);
    }

    // widgets/memory_allocations_test.dart: "Publishers dispatch events in debug mode".
    [Fact]
    public void WidgetsPublishers_DispatchEventsInDebugMode()
    {
        int creations = 0;
        int disposals = 0;
        void Listener(ObjectEvent e)
        {
            if (e is ObjectDisposed)
            {
                disposals++;
            }

            if (e is ObjectCreated)
            {
                creations++;
            }
        }

        Ma.AddListener(Listener);

        int expectedCreations = 0;
        int expectedDisposals = 0;
        var element = new TestElement();
        expectedCreations++;
        RenderObject renderObject = new TestRenderObject();
        expectedCreations++;

        element.MakeInactive();
        // The new FocusManager, its root FocusScopeNode, its _HighlightModeManager and the
        // element's render object.
        expectedCreations += 4;
        element.Unmount();
        // The element and its render object.
        expectedDisposals += 2;
        renderObject.Dispose();
        expectedDisposals += 1;

        Assert.Equal(Constants.KDebugMode ? expectedCreations : 0, creations);
        Assert.Equal(Constants.KDebugMode ? expectedDisposals : 0, disposals);

        Ma.RemoveListener(Listener);
        Assert.False(Ma.HasListeners);
    }

    // widgets/memory_allocations_test.dart: "State dispatches events in debug mode".
    [Fact]
    public void State_DispatchesEventsInDebugMode()
    {
        using var tester = new FrameworkDartTester();
        bool stateCreated = false;
        bool stateDisposed = false;

        Assert.False(Ma.HasListeners);

        void Listener(ObjectEvent e)
        {
            if (e is ObjectCreated && e.Object is State)
            {
                stateCreated = true;
            }

            if (e is ObjectDisposed && e.Object is State)
            {
                stateDisposed = true;
            }
        }

        Ma.AddListener(Listener);
        try
        {
            tester.PumpWidget(new TestStatefulWidget());
            Assert.Equal(Constants.KDebugMode, stateCreated);
            Assert.False(stateDisposed);
            tester.PumpWidget(SizedBox.Shrink());

            Assert.Equal(Constants.KDebugMode, stateCreated);
            Assert.Equal(Constants.KDebugMode, stateDisposed);
        }
        finally
        {
            Ma.RemoveListener(Listener);
        }

        Assert.False(Ma.HasListeners);
    }

    // scheduler/ticker_test.dart: "Ticker dispatches memory events".
    [Fact]
    public void Ticker_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => new Ticker(_ => { }).Dispose(), typeof(Ticker)),
            "package:flutter/scheduler.dart",
            "Ticker");
    }

    // semantics/semantics_test.dart: "SemanticsOwner dispatches memory events".
    [Fact]
    public void SemanticsOwner_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new SemanticsOwner { OnSemanticsUpdate = _ => { } }.Dispose(),
                typeof(SemanticsOwner)),
            "package:flutter/semantics.dart",
            "SemanticsOwner");
    }

    // semantics/semantics_binding_test.dart: "SemanticsHandle dispatches memory events".
    [Fact]
    public void SemanticsHandle_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => SemanticsBinding.Instance.EnsureSemantics().Dispose(), typeof(SemanticsHandle)),
            "package:flutter/semantics.dart",
            "SemanticsHandle");
    }

    // rendering/object.dart's _LocalSemanticsHandle, the handle PipelineOwner.ensureSemantics returns.
    [Fact]
    public void PipelineOwnerEnsureSemanticsHandle_DispatchesMemoryEvents()
    {
        var owner = new PipelineOwner(onSemanticsUpdate: _ => { });
        SemanticsHandle? handle = null;
        List<ObjectEvent> events = EventsFor(
            () =>
            {
                handle = owner.EnsureSemantics();
                return handle;
            },
            () => handle!.Dispose());
        owner.Dispose();

        AssertCreateAndDispose(events, "package:flutter/rendering.dart", "_LocalSemanticsHandle");
    }

    // gestures/multidrag_test.dart: "$MultiDragPointerState dispatches memory events".
    [Fact]
    public void MultiDragPointerState_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new TestMultiDragPointerState(default, PointerDeviceKind.Touch, null).Dispose(),
                typeof(TestMultiDragPointerState)),
            "package:flutter/gestures.dart",
            "MultiDragPointerState");
    }

    // rendering/object_test.dart: "PipelineOwner dispatches memory events".
    [Fact]
    public void PipelineOwner_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => new PipelineOwner().Dispose(), typeof(PipelineOwner)),
            "package:flutter/rendering.dart",
            "PipelineOwner");
    }

    // services/restoration_test.dart: "$RestorationManager dispatches memory events".
    [Fact]
    public void RestorationManager_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => new RestorationManager().Dispose(), typeof(RestorationManager)),
            "package:flutter/foundation.dart",
            "ChangeNotifier");
    }

    // services/restoration_bucket_test.dart: "$RestorationBucket dispatches memory events".
    [Fact]
    public void RestorationBucket_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => RestorationBucket.Empty(restorationId: "child1", debugOwner: null).Dispose(),
                typeof(RestorationBucket)),
            "package:flutter/services.dart",
            "RestorationBucket");

        var manager1 = new MockRestorationManager();
        try
        {
            AssertCreateAndDispose(
                MemoryEvents(
                    () => RestorationBucket.Root(manager: manager1, rawData: null).Dispose(),
                    typeof(RestorationBucket)),
                "package:flutter/services.dart",
                "RestorationBucket");
        }
        finally
        {
            manager1.Dispose();
        }

        var manager2 = new MockRestorationManager();
        RestorationBucket parent = RestorationBucket.Root(manager: manager2, rawData: CreateRawDataSet());
        try
        {
            AssertCreateAndDispose(
                MemoryEvents(
                    () => RestorationBucket.Child(restorationId: "child1", parent: parent, debugOwner: null)
                        .Dispose(),
                    typeof(RestorationBucket)),
                "package:flutter/services.dart",
                "RestorationBucket");
        }
        finally
        {
            parent.Dispose();
            manager2.Dispose();
        }
    }

    // widgets/navigator_test.dart: "$Route  dispatches memory events".
    [Fact]
    public void Route_DispatchesMemoryEvents()
    {
        using var tester = new FrameworkDartTester();
        MaterialPageRoute? pushed = null;

        void CreateAndDisposeRoute()
        {
            var nav = new LabeledGlobalKey<NavigatorState>("nav");
            tester.PumpWidget(new MaterialApp(
                navigatorKey: nav,
                home: new Scaffold(body: new Text("home"))));

            // This should create a route.
            pushed = new MaterialPageRoute(builder: _ => new Placeholder());
            _ = nav.CurrentState!.Push(pushed);
            tester.PumpAndSettle();

            nav.CurrentState!.Pop();
            // This should dispose the route.
            tester.PumpAndSettle();
        }

        // Dart filters on the runtime type MaterialPageRoute<void>, which leaves out the home route
        // (a MaterialPageRoute<dynamic>); the C# route is not generic, so filter on the pushed route.
        var all = new List<ObjectEvent>();
        Ma.AddListener(all.Add);
        try
        {
            CreateAndDisposeRoute();
        }
        finally
        {
            Ma.RemoveListener(all.Add);
        }

        List<ObjectEvent> events = all.Where(e => ReferenceEquals(e.Object, pushed)).ToList();

        AssertCreateAndDispose(events, "package:flutter/widgets.dart", "Route<T>");
    }

    // widgets/scroll_activity_test.dart: "$ScrollActivity dispatches memory events".
    [Fact]
    public void ScrollActivity_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new TestScrollActivity(new TestScrollActivityDelegate()).Dispose(),
                typeof(TestScrollActivity)),
            "package:flutter/widgets.dart",
            "ScrollActivity");
    }

    // widgets/scroll_activity_test.dart: "$ScrollDragController dispatches memory events".
    [Fact]
    public void ScrollDragController_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new ScrollDragController(new TestScrollActivityDelegate(), new DragStartDetails(default))
                    .Dispose(),
                typeof(ScrollDragController)),
            "package:flutter/widgets.dart",
            "ScrollDragController");
    }

    // widgets/banner_test.dart: "BannerPainter dispatches memory events".
    [Fact]
    public void BannerPainter_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new BannerPainter(
                    message: "foo",
                    textDirection: TextDirection.Rtl,
                    location: BannerLocation.TopStart,
                    layoutDirection: TextDirection.Ltr).Dispose(),
                typeof(BannerPainter)),
            "package:flutter/widgets.dart",
            "BannerPainter");
    }

    // widgets/overlay_test.dart: "OverlayEntry dispatches memory events".
    [Fact]
    public void OverlayEntry_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => new OverlayEntry(_ => new Container()).Dispose(), typeof(OverlayEntry)),
            "package:flutter/widgets.dart",
            "OverlayEntry");
    }

    // widgets/disposable_build_context_test.dart: "DisposableBuildContext dispatches memory events".
    [Fact]
    public void DisposableBuildContext_DispatchesMemoryEvents()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<TestStatefulWidgetState>("state");
        tester.PumpWidget(new TestStatefulWidget(key));

        TestStatefulWidgetState state = key.CurrentState!;

        AssertCreateAndDispose(
            MemoryEvents(
                () => new DisposableBuildContext<TestStatefulWidgetState>(state).Dispose(),
                typeof(DisposableBuildContext<TestStatefulWidgetState>)),
            "package:flutter/widgets.dart",
            "DisposableBuildContext");
    }

    // widgets/app_lifecycle_listener_test.dart: "AppLifecycleListener dispatches memory events".
    [Fact]
    public void AppLifecycleListener_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new AppLifecycleListener(binding: WidgetsBinding.Instance).Dispose(),
                typeof(AppLifecycleListener)),
            "package:flutter/widgets.dart",
            "AppLifecycleListener");
    }

    // widgets/scroll_delegate_test.dart: "TwoDimensionalChildBuilderDelegate dispatches memory events".
    [Fact]
    public void TwoDimensionalChildBuilderDelegate_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new TwoDimensionalChildBuilderDelegate((_, _) => null).Dispose(),
                typeof(TwoDimensionalChildBuilderDelegate)),
            "package:flutter/foundation.dart",
            "ChangeNotifier");
    }

    // widgets/scroll_delegate_test.dart: "TwoDimensionalChildListDelegate dispatches memory events".
    [Fact]
    public void TwoDimensionalChildListDelegate_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(
                () => new TwoDimensionalChildListDelegate(children: []).Dispose(),
                typeof(TwoDimensionalChildListDelegate)),
            "package:flutter/foundation.dart",
            "ChangeNotifier");
    }

    // widgets/widget_states_controller_test.dart: "WidgetStatesController dispatches memory events".
    [Fact]
    public void WidgetStatesController_DispatchesMemoryEvents()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => new WidgetStatesController().Dispose(), typeof(WidgetStatesController)),
            "package:flutter/foundation.dart",
            "ChangeNotifier");
    }

    // The remaining debugMaybeDispatchCreated publishers, which Flutter covers only through
    // leak tracking: each reports its own library and class name once and its disposal once.
    [Fact]
    public void FrameworkPublishers_ReportTheirDartLibraryAndClassName()
    {
        List<ObjectEvent> highlightEvents = EventsFor(
            () => new FocusManager(),
            manager => ((FocusManager)manager).Dispose(),
            e => e.Object.GetType() == typeof(HighlightModeManager));
        AssertCreateAndDispose(highlightEvents, "package:flutter/widgets.dart", "_HighlightModeManager");

        AssertCreateAndDispose(
            MemoryEvents(
                () => new DraggableSheetExtent(
                    minSize: 0.25,
                    maxSize: 1.0,
                    snap: false,
                    snapSizes: [],
                    initialSize: 0.5).Dispose(),
                typeof(DraggableSheetExtent)),
            "package:flutter/widgets.dart",
            "_DraggableSheetExtent");
    }

    // The ChangeNotifier subclasses whose Dart constructors call
    // ChangeNotifier.maybeDispatchObjectCreation: they report their creation without a listener.
    [Fact]
    public void ChangeNotifiersDispatchingCreationInTheirConstructors_DispatchCreateAndDispose()
    {
        (Func<IDisposable> Create, Type Type)[] notifiers =
        [
            (() => new ScrollController(), typeof(ScrollController)),
            (() => new FocusNode(), typeof(FocusNode)),
            (() => new ShortcutManager(), typeof(ShortcutManager)),
            (() => new ShortcutRegistry(), typeof(ShortcutRegistry)),
            (() => new DraggableScrollableController(), typeof(DraggableScrollableController)),
            (() => new SliverOverlapAbsorberHandle(), typeof(SliverOverlapAbsorberHandle)),
            (() => new RestorableInt(0), typeof(RestorableInt)),
            (() => new TabController(length: 2), typeof(TabController)),
        ];

        foreach ((Func<IDisposable> create, Type type) in notifiers)
        {
            AssertCreateAndDispose(
                MemoryEvents(() => create().Dispose(), type),
                "package:flutter/foundation.dart",
                "ChangeNotifier");
        }
    }

    [Fact]
    public void Picture_DispatchesCreateAndDisposeThroughTheSdkHooks()
    {
        AssertCreateAndDispose(
            MemoryEvents(() => CreatePicture().Dispose(), typeof(Picture)),
            "dart:ui",
            "Picture");
    }

    // Dart's _createRawDataSet ("v" and "c" are RestorationBucket's values and children map keys).
    private static Dictionary<object, object?> CreateRawDataSet()
    {
        return new Dictionary<object, object?>
        {
            ["v"] = new Dictionary<object, object?> { ["value1"] = 10, ["value2"] = "Hello" },
            ["c"] = new Dictionary<object, object?>
            {
                ["child1"] = new Dictionary<object, object?>
                {
                    ["v"] = new Dictionary<object, object?> { ["foo"] = 22 },
                },
            },
        };
    }

    private static void CheckSdkHandlersSet()
    {
        Assert.NotNull(Picture.OnCreate);
        Assert.NotNull(Picture.OnDispose);
    }

    private static void CheckSdkHandlersNotSet()
    {
        Assert.Null(Picture.OnCreate);
        Assert.Null(Picture.OnDispose);
    }

    // Dart's _activateFlutterObjectsAndReturnCountOfEvents. Its ui.Image half has no counterpart:
    // Plumix's image is Avalonia's IImage, which has no handle class with lifecycle hooks.
    private static int ActivateFoundationObjectsAndReturnCountOfEvents()
    {
        int count = 0;

        var valueNotifier = new ValueNotifier<bool>(true);
        count++;
        var changeNotifier = new ChangeNotifier();
        changeNotifier.AddListener(() => { });
        count++;
        Picture picture = CreatePicture();
        count++;

        valueNotifier.Dispose();
        count++;
        changeNotifier.Dispose();
        count++;
        picture.Dispose();
        count++;

        return count;
    }

    private static Picture CreatePicture()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var rect = new Rect(0.0, 0.0, 100.0, 100.0);
        canvas.ClipRect(rect);
        return recorder.EndRecording();
    }

    // leak_tracker_testing's memoryEvents: the events whose object's runtime type is exactly type.
    private static List<ObjectEvent> MemoryEvents(Action callback, Type type)
    {
        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent e)
        {
            if (e.Object.GetType() == type)
            {
                events.Add(e);
            }
        }

        Ma.AddListener(Listener);
        try
        {
            callback();
        }
        finally
        {
            Ma.RemoveListener(Listener);
        }

        return events;
    }

    // The events about the one object create returns, including its disposal by dispose.
    private static List<ObjectEvent> EventsFor(
        Func<object> create,
        Action dispose)
    {
        return EventsFor(create, _ => dispose(), null);
    }

    private static List<ObjectEvent> EventsFor(
        Func<object> create,
        Action<object> dispose,
        Func<ObjectEvent, bool>? filter)
    {
        var all = new List<ObjectEvent>();
        Ma.AddListener(all.Add);
        object created;
        try
        {
            created = create();
            dispose(created);
        }
        finally
        {
            Ma.RemoveListener(all.Add);
        }

        return all.Where(e => filter?.Invoke(e) ?? ReferenceEquals(e.Object, created)).ToList();
    }

    // leak_tracker_testing's areCreateAndDispose, plus the reported library and class name.
    private static void AssertCreateAndDispose(List<ObjectEvent> events, string library, string className)
    {
        if (!Constants.KDebugMode)
        {
            Assert.Empty(events);
            return;
        }

        Assert.Collection(
            events,
            e =>
            {
                ObjectCreated created = Assert.IsType<ObjectCreated>(e);
                Assert.Equal(library, created.Library);
                Assert.Equal(className, created.ClassName);
            },
            e => Assert.IsType<ObjectDisposed>(e));
        Assert.Same(events[0].Object, events[1].Object);
    }

    private sealed class TestRenderObject : RenderObject
    {
        public override Rect PaintBounds => throw new NotImplementedException();

        protected override Rect SemanticBounds => throw new NotImplementedException();

        protected override void PerformLayout()
        {
        }

        protected override void PerformResize()
        {
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class TestLayer : Layer
    {
        protected internal override void AddToScene(SceneBuilder builder)
        {
        }
    }

    private sealed class TestLeafRenderObjectWidget : LeafRenderObjectWidget
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new TestRenderObject();
    }

    // Dart's _TestElement: a RenderObjectElement with RootElementMixin.
    private sealed class TestElement() : LeafRenderObjectElement(new TestLeafRenderObjectWidget())
    {
        public void MakeInactive()
        {
            var newFocusManager = new FocusManager();
            Attach(new BuildOwner(focusManager: newFocusManager));
            Mount(null, null);
            Deactivate();
        }
    }

    private sealed class TestStatefulWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new TestStatefulWidgetState();
    }

    private sealed class TestStatefulWidgetState : State<TestStatefulWidget>
    {
        public override Widget Build(BuildContext context) => new Container();
    }

    private sealed class TestMultiDragPointerState(
        Point initialPosition,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings) : MultiDragPointerState(initialPosition, kind, gestureSettings)
    {
        protected internal override void Accepted(GestureMultiDragStartCallback starter)
        {
        }
    }

    private sealed class TestScrollActivityDelegate : IScrollActivityDelegate
    {
        public AxisDirection AxisDirection => AxisDirection.Down;

        public double SetPixels(double pixels) => 0.0;

        public void ApplyUserOffset(double delta)
        {
        }

        public void GoIdle()
        {
        }

        public void GoBallistic(double velocity)
        {
        }
    }

    private sealed class TestScrollActivity(IScrollActivityDelegate @delegate) : ScrollActivity(@delegate)
    {
        public override bool IsScrolling => false;

        public override bool ShouldIgnorePointer => true;

        public override double Velocity => 0.0;
    }
}
