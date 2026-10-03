using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.PlatformViewTestKit;

namespace Plumix.Tests;

// Flutter's test/widgets/platform_view_test.dart, group('Common PlatformView') and the top-level tests.
public sealed class PlatformViewLinkTests : IDisposable
{
    private readonly FrameworkDartTester _tester = new(fakeGestureTimers: true);
    private FakePlatformViewController _controller = new(0);

    public void Dispose() => _tester.Dispose();

    private PlatformViewSurface Surface(
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque) =>
        new(
            controller: _controller,
            hitTestBehavior: hitTestBehavior,
            gestureRecognizers: gestureRecognizers ?? NoRecognizers());

    private List<string> WidgetTypeNames() =>
        _tester.AllElements().Select(element => element.Widget.GetType().Name).ToList();

    private static void AssertContainsAllInOrder(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        int index = 0;
        foreach (string name in actual)
        {
            if (index < expected.Count && name == expected[index])
            {
                index++;
            }
        }

        Assert.True(
            index == expected.Count,
            $"Expected [{string.Join(", ", expected)}] in order in [{string.Join(", ", actual)}].");
    }

    // The C# names of Dart's `Focus`, `_FocusInheritedScope`, `Semantics`, `PlatformViewSurface` chain.
    private static readonly string[] CreatedSurfaceChain =
        ["PlatformViewLink", "Focus", "FocusInheritedScope", "Semantics", "PlatformViewSurface"];

    private static readonly string[] PlaceholderChain = ["PlatformViewLink", "PlatformViewPlaceHolder"];

    [Fact]
    public void PlatformViewSurfaceShouldCreatePlatformViewLayer()
    {
        _tester.PumpWidget(Surface());
        Assert.NotNull(Layers(_tester).OfType<PlatformViewLayer>().First());
    }

    [Fact]
    public void PlatformViewSurfaceCanLoseGestureArenas()
    {
        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new Container(
                margin: EdgeInsets.All(10.0),
                child: new GestureDetector(
                    onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                    child: new SizedBox(width: 200.0, height: 100.0, child: Surface())))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.True(verticalDragAcceptedByParent);
        Assert.Empty(_controller.DispatchedPointerEvents);
    }

    [Fact]
    public void PlatformViewSurfaceGestureRecognizersDispatchEvents()
    {
        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: Surface(Recognizers(
                        new Factory<VerticalDragGestureRecognizer>(() => new VerticalDragGestureRecognizer())))))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(3, _controller.DispatchedPointerEvents.Count);
    }

    [Fact]
    public void PlatformViewSurfaceCanClaimGestureAfterAllPointersAreUp()
    {
        bool verticalDragAcceptedByParent = false;
        // The long press recognizer rejects the gesture after the PlatformViewSurface gets the pointer up
        // event. This test makes sure that the PlatformViewSurface can win the gesture after it got the
        // pointer up event.
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                onLongPress: () => { },
                child: new SizedBox(width: 200.0, height: 100.0, child: Surface()))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(2, _controller.DispatchedPointerEvents.Count);
    }

    [Fact]
    public void PlatformViewSurfaceRebuiltDuringGesture()
    {
        _tester.PumpWidget(TopLeft(200.0, 100.0, Surface()));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));

        _tester.PumpWidget(TopLeft(200.0, 100.0, Surface()));

        gesture.Up();

        Assert.Equal(3, _controller.DispatchedPointerEvents.Count);
    }

    [Fact]
    public void PlatformViewSurfaceWithEagerGestureRecognizer()
    {
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => { },
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: Surface(Recognizers(
                        new Factory<OneSequenceGestureRecognizer>(() => new EagerGestureRecognizer())))))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);

        // Normally (without the eager gesture recognizer) after just the pointer down event no gesture
        // recognizer will win the arena (so no motion events will be dispatched to the PlatformViewSurface).
        // Here we assert that with the eager recognizer in the gesture team the pointer down event is
        // immediately dispatched.
        Assert.Single(_controller.DispatchedPointerEvents);

        // Finish gesture to release resources.
        gesture.Up();
        _tester.PumpAndSettle();
    }

    [Fact]
    public void PlatformViewRenderBoxReconstructedWithSameGestureRecognizers()
    {
        var factory = new CountingEagerFactory();
        PlatformViewSurface platformViewSurface = Surface(Recognizers(
            new Factory<OneSequenceGestureRecognizer>(factory.Construct)));

        _tester.PumpWidget(platformViewSurface);
        _tester.PumpWidget(SizedBox.Shrink());
        _tester.PumpWidget(platformViewSurface);

        Assert.Equal(2, factory.Invocations);
    }

    [Fact]
    public void PlatformViewSurfaceRebuiltWithSameGestureRecognizers()
    {
        var factory = new CountingEagerFactory();

        _tester.PumpWidget(Surface(Recognizers(new Factory<OneSequenceGestureRecognizer>(factory.Construct))));

        _tester.PumpWidget(Surface(Recognizers(new Factory<OneSequenceGestureRecognizer>(factory.Construct))));
        Assert.Equal(1, factory.Invocations);
    }

    [Fact]
    public void PlatformViewLinkWidgetInitCreatesAPlaceholderBeforeCreationAndASurfaceAfter()
    {
        // Flutter: 'PlatformViewLink Widget init, should create a placeholder widget before
        // onPlatformViewCreated and a PlatformViewSurface after'.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        PlatformViewCreatedCallback? onPlatformViewCreatedCallBack = null;
        int createdPlatformViewId = -1;

        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                onPlatformViewCreatedCallBack = parameters.OnPlatformViewCreated;
                createdPlatformViewId = parameters.Id;
                var controller = new FakePlatformViewController(parameters.Id);
                _ = controller.Create();
                return controller;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(platformViewLink);

        AssertContainsAllInOrder(PlaceholderChain, WidgetTypeNames());

        onPlatformViewCreatedCallBack!(createdPlatformViewId);

        _tester.Pump();

        AssertContainsAllInOrder(CreatedSurfaceChain, WidgetTypeNames());

        Assert.Equal(currentViewId + 1, createdPlatformViewId);
    }

    [Fact]
    public void PlatformViewLinkWidgetShouldNotTriggerCreationWithAnEmptySize()
    {
        FakeAndroidViewController? controller = null;

        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                controller = new FakeAndroidViewController(parameters.Id, requiresSize: true);
                _ = controller.Create();
                // This test should be simulating one of the texture-based display
                // modes, where `create` is a no-op when not provided a size, and
                // creation is triggered via a later call to setSize, or to `create`
                // with a size.
                Assert.True(controller.AwaitingCreation);
                return controller;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(new Center(child: new SizedBox(height: 0, child: platformViewLink)));

        AssertContainsAllInOrder(
            ["Center", "SizedBox", "PlatformViewLink", "PlatformViewPlaceHolder"],
            WidgetTypeNames());

        // 'create' should not have been called by PlatformViewLink, since its
        // size is empty.
        Assert.True(controller!.AwaitingCreation);
    }

    [Fact]
    public void PlatformViewLinkCallsCreateWhenNeededForAndroidTextureDisplayModes()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        PlatformViewCreatedCallback? onPlatformViewCreatedCallBack = null;
        int createdPlatformViewId = -1;
        FakeAndroidViewController? controller = null;

        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                onPlatformViewCreatedCallBack = parameters.OnPlatformViewCreated;
                createdPlatformViewId = parameters.Id;
                controller = new FakeAndroidViewController(parameters.Id, requiresSize: true);
                _ = controller.Create();
                // This test should be simulating one of the texture-based display
                // modes, where `create` is a no-op when not provided a size, and
                // creation is triggered via a later call to setSize, or to `create`
                // with a size.
                Assert.True(controller.AwaitingCreation);
                return controller;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(platformViewLink);

        AssertContainsAllInOrder(PlaceholderChain, WidgetTypeNames());

        // Layout should have triggered a create call. Simulate the callback
        // that the real controller would make after creation.
        Assert.False(controller!.AwaitingCreation);
        onPlatformViewCreatedCallBack!(createdPlatformViewId);

        _tester.Pump();

        AssertContainsAllInOrder(CreatedSurfaceChain, WidgetTypeNames());

        Assert.Equal(currentViewId + 1, createdPlatformViewId);
    }

    [Fact]
    public void PlatformViewLinkIncludesOffsetInCreateCallWhenUsingTextureLayer()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(400, 200));
        FakeAndroidViewController? controller = null;

        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                controller = new FakeAndroidViewController(parameters.Id, requiresSize: true);
                _ = controller.Create();
                // This test should be simulating one of the texture-based display
                // modes, where `create` is a no-op when not provided a size, and
                // creation is triggered via a later call to setSize, or to `create`
                // with a size.
                Assert.True(controller.AwaitingCreation);
                return controller;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        tester.PumpWidget(new Container(
            constraints: BoxConstraints.Expand(),
            alignment: Alignment.Center,
            child: new SizedBox(width: 100, height: 50, child: platformViewLink)));

        Assert.Equal(new Point(150, 75), controller!.CreatePosition);
    }

    [Fact]
    public void PlatformViewLinkDoesNotDoubleCallCreateForAndroidHybridComposition()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        PlatformViewCreatedCallback? onPlatformViewCreatedCallBack = null;
        int createdPlatformViewId = -1;
        FakeAndroidViewController? controller = null;

        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                onPlatformViewCreatedCallBack = parameters.OnPlatformViewCreated;
                createdPlatformViewId = parameters.Id;
                controller = new FakeAndroidViewController(parameters.Id);
                _ = controller.Create();
                // This test should be simulating Hybrid Composition mode, where
                // `create` takes effect immediately.
                Assert.False(controller.AwaitingCreation);
                return controller;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(platformViewLink);

        AssertContainsAllInOrder(PlaceholderChain, WidgetTypeNames());

        onPlatformViewCreatedCallBack!(createdPlatformViewId);

        _tester.Pump();

        AssertContainsAllInOrder(CreatedSurfaceChain, WidgetTypeNames());

        Assert.Equal(currentViewId + 1, createdPlatformViewId);
    }

    [Fact]
    public void PlatformViewLinkWidgetDispose()
    {
        FakePlatformViewController? disposedController = null;
        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                disposedController = new FakePlatformViewController(parameters.Id);
                parameters.OnPlatformViewCreated(parameters.Id);
                return disposedController;
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(platformViewLink);

        _tester.PumpWidget(new Container());

        Assert.True(disposedController!.Disposed);
    }

    [Fact]
    public void PlatformViewLinkHandlesOnPlatformViewCreatedWhenDisposed()
    {
        PlatformViewCreationParams? creationParams = null;
        FakePlatformViewController? controller = null;
        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                creationParams = parameters;
                return controller = new FakePlatformViewController(parameters.Id);
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(platformViewLink);
        _tester.PumpWidget(new Container());

        Assert.True(controller!.Disposed);
        creationParams!.OnPlatformViewCreated(creationParams.Id);
    }

    [Fact]
    public void PlatformViewLinkPlaceholderDoesNotCrashWhenDetachedDuringFastScroll()
    {
        var scrollController = new ScrollController();

        _tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: ListView.Builder(
                controller: scrollController,
                itemCount: 200,
                itemBuilder: (context, index) => new SizedBox(
                    height: 100,
                    child: new PlatformViewLink(
                        viewType: "webview",
                        onCreatePlatformView: parameters =>
                        {
                            var controller = new FakeAndroidViewController(parameters.Id, requiresSize: true);
                            _ = controller.Create();
                            return controller;
                        },
                        surfaceFactory: (context, controller) => new PlatformViewSurface(
                            gestureRecognizers: NoRecognizers(),
                            controller: controller,
                            hitTestBehavior: PlatformViewHitTestBehavior.Opaque))))));

        // Scroll far enough that the placeholder's post-frame callback fires after it has been detached.
        scrollController.JumpTo(5000);
        _tester.Pump();

        Assert.Null(_tester.TakeException());
        scrollController.Dispose();
    }

    [Fact]
    public void PlatformViewLinkWidgetSurvivesWidgetTreeChange()
    {
        var key = new LabeledGlobalKey<State>("link");
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        var ids = new List<int>();

        PlatformViewLink CreatePlatformViewLink() =>
            new(
                key: key,
                viewType: "webview",
                onCreatePlatformView: parameters =>
                {
                    ids.Add(parameters.Id);
                    var controller = new FakePlatformViewController(parameters.Id);
                    parameters.OnPlatformViewCreated(parameters.Id);
                    return controller;
                },
                surfaceFactory: (context, controller) => new PlatformViewSurface(
                    gestureRecognizers: NoRecognizers(),
                    controller: controller,
                    hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

        _tester.PumpWidget(Centered(200.0, 100.0, CreatePlatformViewLink()));

        _tester.PumpWidget(Centered(200.0, 100.0, CreatePlatformViewLink()));

        Assert.Equal([currentViewId + 1], ids);
    }

    [Fact]
    public void PlatformViewLinkReInitializesWhenViewTypeChanges()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        var ids = new List<int>();
        var surfaceViewIds = new List<int>();
        var viewTypes = new List<string>();

        PlatformViewLink CreatePlatformViewLink(string viewType) =>
            new(
                viewType: viewType,
                onCreatePlatformView: parameters =>
                {
                    ids.Add(parameters.Id);
                    viewTypes.Add(parameters.ViewType);
                    _controller = new FakePlatformViewController(parameters.Id);
                    parameters.OnPlatformViewCreated(parameters.Id);
                    return _controller;
                },
                surfaceFactory: (context, controller) =>
                {
                    surfaceViewIds.Add(controller.ViewId);
                    return new PlatformViewSurface(
                        gestureRecognizers: NoRecognizers(),
                        controller: controller,
                        hitTestBehavior: PlatformViewHitTestBehavior.Opaque);
                });

        _tester.PumpWidget(Centered(200.0, 100.0, CreatePlatformViewLink("webview")));

        _tester.PumpWidget(Centered(200.0, 100.0, CreatePlatformViewLink("maps")));

        Assert.Equal([currentViewId + 1, currentViewId + 2], ids.Order());
        Assert.Equal([currentViewId + 1, currentViewId + 2], surfaceViewIds.Order());
        Assert.Equal(["maps", "webview"], viewTypes.Order());
    }

    [Fact]
    public void PlatformViewLinkCanTakeAnyWidgetToReturnInTheSurfaceFactory()
    {
        var platformViewLink = new PlatformViewLink(
            viewType: "webview",
            onCreatePlatformView: parameters =>
            {
                parameters.OnPlatformViewCreated(parameters.Id);
                return new FakePlatformViewController(parameters.Id);
            },
            surfaceFactory: (context, controller) => new Container());

        _tester.PumpWidget(platformViewLink);

        Assert.NotEmpty(_tester.ElementsOfType<Container>());
    }

    [Fact]
    public void PlatformViewLinkManagesTheFocusProperly()
    {
        var containerKey = new LabeledGlobalKey<State>("container");
        Action<bool>? focusChanged = null;
        FakePlatformViewController? controller = null;
        _tester.PumpWidget(new Center(child: new Column(children:
        [
            new SizedBox(
                width: 300,
                height: 300,
                child: new PlatformViewLink(
                    viewType: "webview",
                    onCreatePlatformView: parameters =>
                    {
                        parameters.OnPlatformViewCreated(parameters.Id);
                        focusChanged = parameters.OnFocusChanged;
                        controller = new FakePlatformViewController(parameters.Id);
                        return controller;
                    },
                    surfaceFactory: (context, controller) => new PlatformViewSurface(
                        gestureRecognizers: NoRecognizers(),
                        controller: controller,
                        hitTestBehavior: PlatformViewHitTestBehavior.Opaque))),
            new Focus(debugLabel: "container", child: new Container(key: containerKey)),
        ])));
        FocusNode platformViewFocusNode = FocusNodeBelow<PlatformViewLink>(_tester);
        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _tester.Pump();

        Assert.True(containerFocusNode.HasFocus);
        Assert.False(platformViewFocusNode.HasFocus);

        // ask the platform view to gain focus
        focusChanged!(true);
        _tester.Pump();

        Assert.False(containerFocusNode.HasFocus);
        Assert.True(platformViewFocusNode.HasFocus);
        Assert.False(controller!.FocusCleared);
        // ask the container to gain focus, and the platform view should clear focus.
        containerFocusNode.RequestFocus();
        _tester.Pump();

        Assert.True(containerFocusNode.HasFocus);
        Assert.False(platformViewFocusNode.HasFocus);
        Assert.True(controller.FocusCleared);
    }

    private PlatformViewLink FocusableLink(Action<int> onId) =>
        new(
            viewType: "test",
            onCreatePlatformView: parameters =>
            {
                onId(parameters.Id);
                parameters.OnPlatformViewCreated(parameters.Id);
                return new FakePlatformViewController(parameters.Id);
            },
            surfaceFactory: (context, controller) => new PlatformViewSurface(
                gestureRecognizers: NoRecognizers(),
                controller: controller,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque));

    [Fact]
    public void PlatformViewLinkSetsAPlatformViewTextInputClientWhenFocused()
    {
        int viewId = -1;
        _tester.PumpWidget(new SizedBox(width: 300, height: 300, child: FocusableLink(id => viewId = id)));

        FocusNode focusNode = FocusNodeBelow<PlatformViewLink>(_tester);
        Assert.False(focusNode.HasFocus);

        using MockMethodCallHandler textInput = MockTextInput();

        focusNode.RequestFocus();
        _tester.Pump();

        Assert.True(focusNode.HasFocus);
        System.Collections.IDictionary? lastPlatformViewTextClient = LastSetPlatformViewClient(textInput);
        Assert.NotNull(lastPlatformViewTextClient);
        Assert.True(lastPlatformViewTextClient!.Contains("platformViewId"));
        Assert.Equal(viewId, Convert.ToInt32(lastPlatformViewTextClient["platformViewId"]));
    }

    [Fact]
    public void PlatformViewLinkFocusChangeReportsErrorWhenChannelFails()
    {
        _tester.PumpWidget(new SizedBox(width: 300, height: 300, child: FocusableLink(_ => { })));

        FocusNode focusNode = FocusNodeBelow<PlatformViewLink>(_tester);
        using var errors = new ErrorCapture();
        using MockMethodCallHandler textInput = MockTextInput(call =>
            call.Method == "TextInput.setPlatformViewClient" ? throw new Exception("Channel failed") : null);

        focusNode.RequestFocus();
        _tester.Pump();

        FlutterErrorDetails details = Assert.Single(errors.Errors);
        Assert.Contains("Channel failed", details.Exception.ToString());
        Assert.Contains("while handling framework focus changed on platform view", details.Context!.ToString());
    }

    [Fact]
    public void PlatformViewLinkFocusNodeIsLabelledBeforeTheIdIsAllocated()
    {
        _tester.PumpWidget(new SizedBox(width: 300, height: 300, child: FocusableLink(_ => { })));

        Assert.Equal("PlatformView(id: null)", FocusNodeBelow<PlatformViewLink>(_tester).DebugLabel);
    }

    // Top-level tests.

    [Fact]
    public void PlatformViewsRespectHitTestBehavior()
    {
        var logs = new List<string>();

        Widget Scaffold(Widget target) =>
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Center(child: SizedBox.Square(
                    dimension: 600,
                    child: new MouseRegion(
                        onEnter: _ => logs.Add("enter1"),
                        onExit: _ => logs.Add("exit1"),
                        cursor: SystemMouseCursors.Forbidden,
                        child: new Stack(children:
                        [
                            new Center(child: SizedBox.Square(
                                dimension: 400,
                                child: new MouseRegion(
                                    onEnter: _ => logs.Add("enter2"),
                                    onExit: _ => logs.Add("exit2"),
                                    cursor: SystemMouseCursors.Text))),
                            new Center(child: new SizedBox(width: 200, height: 200, child: target)),
                        ])))));

        TestGesture gesture = _tester.CreateGesture(kind: PointerDeviceKind.Mouse);

        // Test: Opaque
        _tester.PumpWidget(Scaffold(Surface(hitTestBehavior: PlatformViewHitTestBehavior.Opaque)));
        logs.Clear();

        gesture.MoveTo(new Point(400, 300));
        Assert.Equal(["enter1"], logs);
        Assert.Single(_controller.DispatchedPointerEvents);
        Assert.IsType<PointerHoverEvent>(_controller.DispatchedPointerEvents[0]);
        logs.Clear();
        _controller.ClearTestingVariables();

        // Test: changing no option does not trigger events
        _tester.PumpWidget(Scaffold(Surface(hitTestBehavior: PlatformViewHitTestBehavior.Opaque)));
        Assert.Empty(logs);
        Assert.Empty(_controller.DispatchedPointerEvents);

        // Test: Translucent
        _tester.PumpWidget(Scaffold(Surface(hitTestBehavior: PlatformViewHitTestBehavior.Translucent)));
        Assert.Equal(["enter2"], logs);
        Assert.Empty(_controller.DispatchedPointerEvents);
        logs.Clear();

        gesture.MoveBy(new Vector(1, 1));
        Assert.Empty(logs);
        PointerEvent hover = Assert.Single(_controller.DispatchedPointerEvents);
        Assert.IsType<PointerHoverEvent>(hover);
        Assert.Equal(new Point(401, 301), hover.Position);
        Assert.Equal(new Point(101, 101), hover.LocalPosition);
        _controller.ClearTestingVariables();

        // Test: Transparent
        _tester.PumpWidget(Scaffold(Surface(hitTestBehavior: PlatformViewHitTestBehavior.Transparent)));
        Assert.Empty(logs);
        Assert.Empty(_controller.DispatchedPointerEvents);

        gesture.MoveBy(new Vector(1, 1));
        Assert.Empty(logs);
        Assert.Empty(_controller.DispatchedPointerEvents);

        // Test: Opaque
        _tester.PumpWidget(Scaffold(Surface(hitTestBehavior: PlatformViewHitTestBehavior.Opaque)));
        Assert.Equal(["exit2"], logs);
        Assert.Empty(_controller.DispatchedPointerEvents);
        logs.Clear();

        gesture.MoveBy(new Vector(1, 1));
        Assert.Empty(logs);
        Assert.Single(_controller.DispatchedPointerEvents);
        Assert.IsType<PointerHoverEvent>(_controller.DispatchedPointerEvents[0]);
    }

    [Fact]
    public void HtmlElementViewCanBeInstantiated()
    {
        var htmlElementView = new HtmlElementView(viewType: "webview");

        _tester.PumpWidget(new Center(child: new SizedBox(width: 100, height: 100, child: htmlElementView)));
        _tester.PumpAndSettle();

        // This file runs on non-web platforms, so we expect `HtmlElementView` to
        // fail.
        object? exception = _tester.TakeException();
        Assert.IsType<NotImplementedException>(exception);
        Assert.Contains("HtmlElementView is only available on Flutter Web", exception!.ToString());
    }

    [Fact]
    public void PlatformViewLinkDemoPage_CreatesOnLayoutAndMovesFocusBothWays()
    {
        _tester.PumpWidget(new Directionality(TextDirection.Ltr, new PlatformViewLinkDemoPage()));
        _tester.Pump();

        Assert.Contains(_tester.AllElements(), element => element.Widget is Text text
            && text.Data is { } data && data.StartsWith("create #", StringComparison.Ordinal));
        Assert.NotEmpty(_tester.ElementsWithText("outside focus target"));
        Assert.Single(_tester.AllElements(), element => element.Widget is Text text
            && text.Data is { } data && data.EndsWith(": not focused", StringComparison.Ordinal));

        _tester.Tap(_tester.ElementsWithText("platform requests focus").Single());
        _tester.Pump();
        Assert.Single(_tester.AllElements(), element => element.Widget is Text text
            && text.Data is { } data && data.EndsWith(": focused", StringComparison.Ordinal));

        _tester.Tap(_tester.ElementsWithText("focus outside").Single());
        _tester.Pump();
        Assert.Contains(_tester.AllElements(), element => element.Widget is Text text
            && text.Data is { } data && data.StartsWith("clearFocus #", StringComparison.Ordinal));
    }

    [Fact]
    public void HtmlElementViewFromTagNameThrowsOffTheWeb()
    {
        NotImplementedException error = Assert.Throws<NotImplementedException>(
            () => HtmlElementView.FromTagName(tagName: "div"));
        Assert.Equal("HtmlElementView is only available on Flutter Web", error.Message);
    }
}
