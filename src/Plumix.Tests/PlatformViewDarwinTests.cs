using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.PlatformViewTestKit;

namespace Plumix.Tests;

// Flutter's test/widgets/platform_view_test.dart, group('UiKitView') and group('AppKitView'). The AppKitView
// group also repeats ten UiKitView tests verbatim (same names, same FakeIosPlatformViewsController); they
// are ported once, here, under the UiKitView names.
public sealed class PlatformViewDarwinTests : IDisposable
{
    private readonly FrameworkDartTester _tester = new(fakeGestureTimers: true);

    public void Dispose()
    {
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(null);
        _tester.Dispose();
    }

    private static UiKitView WebView(
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        Key? key = null) =>
        new(
            viewType: "webview",
            layoutDirection: TextDirection.Ltr,
            hitTestBehavior: hitTestBehavior,
            gestureRecognizers: gestureRecognizers,
            key: key);

    private static AppKitView AppKitWebView(Key? key = null) =>
        new(viewType: "webview", layoutDirection: TextDirection.Ltr, key: key);

    private static FakeIosPlatformViewsController Ios(params string[] viewTypes)
    {
        var controller = new FakeIosPlatformViewsController(dartAsyncReplies: true);
        foreach (string viewType in viewTypes)
        {
            controller.RegisterViewType(viewType);
        }

        return controller;
    }

    private static FakeMacosPlatformViewsController Macos(params string[] viewTypes)
    {
        var controller = new FakeMacosPlatformViewsController(dartAsyncReplies: true);
        foreach (string viewType in viewTypes)
        {
            controller.RegisterViewType(viewType);
        }

        return controller;
    }

    // In the AndroidView group in Dart, but it runs a UiKitView.
    [Fact]
    public void FocusChangeReportsErrorWhenChannelFails()
    {
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));
        _tester.PumpAndSettle();

        FocusNode focusNode = FocusNodeBelow<UiKitView>(_tester);
        using var errors = new ErrorCapture();
        using MockMethodCallHandler textInput = MockTextInput(call =>
            call.Method == "TextInput.setPlatformViewClient" ? throw new Exception("Channel failed") : null);

        focusNode.RequestFocus();
        _tester.Pump();

        FlutterErrorDetails details = Assert.Single(errors.Errors);
        Assert.IsAssignableFrom<Exception>(details.Exception);
        Assert.Contains("Channel failed", details.Exception.ToString());
        Assert.Contains("while setting the platform view client", details.Context!.ToString());
    }

    // group('UiKitView')

    [Fact]
    public void CreateUIView()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        Assert.Equal([new FakeUiKitView(currentViewId + 1, "webview")], viewsController.Views);
    }

    [Fact]
    public void UiKitViewReportsErrorWhenCreationFails()
    {
        using var errors = new ErrorCapture();
        using var platformViews = new MockMethodCallHandler(SystemChannels.PlatformViews, call =>
            call.Method == "create"
                ? throw new PlatformException(code: "CREATION_ERROR", message: "Failed to create view")
                : null);

        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));
        _tester.Pump();

        Assert.NotEmpty(errors.Errors);
        var exception = Assert.IsType<PlatformException>(errors.Errors[0].Exception);
        Assert.Equal("CREATION_ERROR", exception.Code);
        Assert.Contains("while creating a Darwin platform view", errors.Errors[0].Context!.ToString());
    }

    [Fact]
    public void ChangeUIViewViewType()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview", "maps");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new UiKitView(viewType: "maps", layoutDirection: TextDirection.Ltr)));

        Assert.Equal([new FakeUiKitView(currentViewId + 2, "maps")], viewsController.Views);
    }

    [Fact]
    public void DisposeUIView()
    {
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _tester.PumpWidget(Centered(200.0, 100.0, null));

        Assert.Empty(viewsController.Views);
    }

    [Fact]
    public void DisposeUIViewBeforeCreationCompleted()
    {
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        viewsController.CreationDelay = new TaskCompletionSource();
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _tester.PumpWidget(Centered(200.0, 100.0, null));

        viewsController.CreationDelay.SetResult();

        Assert.Empty(viewsController.Views);
    }

    [Fact]
    public void UIViewSurvivesWidgetTreeChange()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        var key = new LabeledGlobalKey<State>("view");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView(key: key)));

        _tester.PumpWidget(Centered(200.0, 100.0, WebView(key: key)));

        Assert.Equal([new FakeUiKitView(currentViewId + 1, "webview")], viewsController.Views);
    }

    [Fact]
    public void CreateUIViewWithParams()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new UiKitView(
                viewType: "webview",
                layoutDirection: TextDirection.Ltr,
                creationParams: "creation parameters",
                creationParamsCodec: new StringCodec())));

        FakeUiKitView fakeView = viewsController.Views.First();
        string? actualParams = new StringCodec().DecodeMessage(ByteData.SublistView(fakeView.CreationParams!));
        Assert.Equal("creation parameters", actualParams);
        Assert.Equal(
            [new FakeUiKitView(currentViewId + 1, "webview", fakeView.CreationParams)],
            viewsController.Views);
    }

    [Fact]
    public void UiKitViewAcceptsGestures()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        Assert.Equal(0, viewsController.GesturesAccepted[currentViewId + 1]);

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
    }

    [Theory]
    [InlineData(PlatformViewHitTestBehavior.Transparent, 0, 1)]
    [InlineData(PlatformViewHitTestBehavior.Translucent, 1, 1)]
    [InlineData(PlatformViewHitTestBehavior.Opaque, 1, 0)]
    public void UiKitViewHitTestBehavior(
        PlatformViewHitTestBehavior behavior,
        int expectedAccepted,
        int expectedParentDowns)
    {
        // Flutter: 'UiKitView transparent/translucent/opaque hit test behavior'.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        int numPointerDownsOnParent = 0;
        _tester.PumpWidget(ParentListenerStack(WebView(hitTestBehavior: behavior), () => numPointerDownsOnParent++));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(expectedAccepted, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(expectedParentDowns, numPointerDownsOnParent);
    }

    [Fact]
    public void UiKitViewCanLoseGestureArenas()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new Container(
                margin: EdgeInsets.All(10.0),
                child: new GestureDetector(
                    onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                    child: new SizedBox(width: 200.0, height: 100.0, child: WebView())))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.True(verticalDragAcceptedByParent);
        Assert.Equal(0, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(1, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewTapGestureRecognizers()
    {
        // Flutter's name; the test claims a vertical drag through a VerticalDragGestureRecognizer.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        bool gestureAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => gestureAcceptedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<VerticalDragGestureRecognizer>(() => new VerticalDragGestureRecognizer())))))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.False(gestureAcceptedByParent);
        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewLongPressGestureRecognizers()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        bool gestureAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onLongPress: () => gestureAcceptedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<LongPressGestureRecognizer>(() => new LongPressGestureRecognizer())))))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        LongPressAt(_tester, new Point(50.0, 50.0));

        Assert.False(gestureAcceptedByParent);
        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewDragGestureRecognizers()
    {
        // Flutter's name; the test taps through a TapGestureRecognizer.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<TapGestureRecognizer>(() => new TapGestureRecognizer())))))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TapAt(_tester, new Point(50.0, 50.0));

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewCanClaimGestureAfterAllPointersAreUp()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        bool verticalDragAcceptedByParent = false;
        // The long press recognizer rejects the gesture after the AndroidView gets the pointer up event.
        // This test makes sure that the Android view can win the gesture after it got the pointer up
        // event.
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                onLongPress: () => { },
                child: new SizedBox(width: 200.0, height: 100.0, child: WebView()))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewRebuiltDuringGesture()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));

        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        gesture.Up();

        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewWithEagerGestureRecognizer()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => { },
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<OneSequenceGestureRecognizer>(() => new EagerGestureRecognizer())))))));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);

        // Normally (without the eager gesture recognizer) after just the pointer down event no gesture
        // recognizer will win the arena (so no motion events will be dispatched to the Android view).
        // Here we assert that with the eager recognizer in the gesture team the pointer down event is
        // immediately dispatched.
        Assert.Equal(1, viewsController.GesturesAccepted[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesRejected[currentViewId + 1]);

        // Finish gesture to release resources.
        gesture.Up();
        _tester.PumpAndSettle();
    }

    [Fact]
    public void UiKitViewRejectsGesturesAbsorbedBySiblings()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(new Stack(
            alignment: Alignment.TopLeft,
            children:
            [
                WebView(),
                new Container(color: Color.FromARGB(255, 255, 255, 255), width: 100, height: 100),
            ]));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(1, viewsController.GesturesRejected[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesAccepted[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewRejectsGesturesAbsorbedBySiblingsOutsideTheViewBoundsButInsideItsFrame()
    {
        // Flutter: 'UiKitView rejects gestures absorbed by siblings if the touch is outside of the
        // platform view bounds but inside platform view frame'.
        // UiKitView is positioned at (left=0, top=100, right=300, bottom=600).
        // Opaque container is on top of the UiKitView positioned at (left=0, top=500, right=300,
        // bottom=600). Touch on (550, 150) is expected to be absorbed by the container.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(new SizedBox(
            width: 300,
            height: 600,
            child: new Stack(
                alignment: Alignment.TopLeft,
                children:
                [
                    Transform.Translate(
                        offset: new Point(0, 100),
                        child: new SizedBox(width: 300, height: 500, child: WebView())),
                    Transform.Translate(
                        offset: new Point(0, 500),
                        child: new Container(
                            color: Color.FromARGB(255, 255, 255, 255),
                            width: 300,
                            height: 100)),
                ])));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        TestGesture gesture = _tester.StartGesture(new Point(150, 550), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(1, viewsController.GesturesRejected[currentViewId + 1]);
        Assert.Equal(0, viewsController.GesturesAccepted[currentViewId + 1]);
    }

    [Fact]
    public void UiKitViewRebuiltWithSameGestureRecognizers()
    {
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        var factory = new CountingEagerFactory();
        _tester.PumpWidget(WebView(gestureRecognizers: Recognizers(
            new Factory<EagerGestureRecognizer>(factory.Construct))));

        _tester.PumpWidget(WebView(
            hitTestBehavior: PlatformViewHitTestBehavior.Translucent,
            gestureRecognizers: Recognizers(new Factory<EagerGestureRecognizer>(factory.Construct))));

        Assert.Equal(1, factory.Invocations);
    }

    private Widget FocusColumn(Widget view, Key containerKey) =>
        new Center(child: new Column(children:
        [
            new SizedBox(width: 200.0, height: 100.0, child: view),
            new Focus(debugLabel: "container", child: new Container(key: containerKey)),
        ]));

    [Fact]
    public void UiKitViewCanTakeInputFocus()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        var containerKey = new LabeledGlobalKey<State>("container");
        _tester.PumpWidget(FocusColumn(WebView(), containerKey));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        FocusNode uiKitViewFocusNode = FocusNodeBelow<UiKitView>(_tester);
        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _tester.Pump();

        Assert.True(containerFocusNode.HasFocus);
        Assert.False(uiKitViewFocusNode.HasFocus);

        viewsController.InvokeViewFocused(currentViewId + 1);
        _tester.Pump();

        Assert.False(containerFocusNode.HasFocus);
        Assert.True(uiKitViewFocusNode.HasFocus);
    }

    [Fact]
    public void UiKitViewSendsTextInputSetPlatformViewClientWhenFocused()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(WebView());

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        FocusNode focusNode = FocusNodeBelow<UiKitView>(_tester);
        Assert.False(focusNode.HasFocus);

        using MockMethodCallHandler textInput = MockTextInput();

        focusNode.RequestFocus();
        _tester.Pump();

        Assert.True(focusNode.HasFocus);
        Assert.Equal(
            currentViewId + 1,
            Convert.ToInt32(LastSetPlatformViewClient(textInput)!["platformViewId"]));
    }

    [Fact]
    public void FocusNodeIsDisposedOnUIViewDispose()
    {
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));
        _tester.Pump();

        FocusNode node = _tester.State<UiKitViewState>().FocusNode!;
        ChangeNotifier.DebugAssertNotDisposed(node);

        _tester.PumpWidget(Centered(200.0, 100.0, null));
        Assert.ThrowsAny<Exception>(() => ChangeNotifier.DebugAssertNotDisposed(node));
    }

    [Fact]
    public void UiKitViewHasCorrectSemantics()
    {
        using SemanticsScope semantics = SemanticsScope.Ensure(_tester);
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        Assert.True(currentViewId >= 0);
        using FakeIosPlatformViewsController viewsController = Ios("webview");

        _tester.PumpWidget(new Semantics(
            container: true,
            child: new Align(
                alignment: Alignment.BottomRight,
                child: new SizedBox(width: 200.0, height: 100.0, child: WebView()))));
        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();
        semantics.Flush();

        RenderObject renderObject = _tester.ElementOfType<UiKitPlatformView>().FindRenderObject()!;
        AssertPlatformViewSemantics(renderObject, platformViewId: currentViewId + 1);
    }

    // group('AppKitView')

    [Fact]
    public void CreateAppView()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView()));

        Assert.Equal([new FakeAppKitView(currentViewId + 1, "webview")], viewsController.Views);
    }

    [Fact]
    public void ChangeAppKitViewViewType()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview", "maps");
        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView()));

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AppKitView(viewType: "maps", layoutDirection: TextDirection.Ltr)));

        Assert.Equal([new FakeAppKitView(currentViewId + 2, "maps")], viewsController.Views);
    }

    [Fact]
    public void DisposeAppKitView()
    {
        using FakeMacosPlatformViewsController viewsController = Macos("webview");
        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView()));

        _tester.PumpWidget(Centered(200.0, 100.0, null));

        Assert.Empty(viewsController.Views);
    }

    [Fact]
    public void DisposeAppKitViewBeforeCreationCompleted()
    {
        using FakeMacosPlatformViewsController viewsController = Macos("webview");
        viewsController.CreationDelay = new TaskCompletionSource();
        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView()));

        _tester.PumpWidget(Centered(200.0, 100.0, null));

        viewsController.CreationDelay.SetResult();

        Assert.Empty(viewsController.Views);
    }

    [Fact]
    public void AppKitViewSurvivesWidgetTreeChange()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview");
        var key = new LabeledGlobalKey<State>("view");
        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView(key: key)));

        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView(key: key)));

        Assert.Equal([new FakeAppKitView(currentViewId + 1, "webview")], viewsController.Views);
    }

    [Fact]
    public void CreateAppKitViewWithParams()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AppKitView(
                viewType: "webview",
                layoutDirection: TextDirection.Ltr,
                creationParams: "creation parameters",
                creationParamsCodec: new StringCodec())));

        FakeAppKitView fakeView = viewsController.Views.First();
        string? actualParams = new StringCodec().DecodeMessage(ByteData.SublistView(fakeView.CreationParams!));
        Assert.Equal("creation parameters", actualParams);
        Assert.Equal(
            [new FakeAppKitView(currentViewId + 1, "webview", fakeView.CreationParams)],
            viewsController.Views);
    }

    // TODO(schectman): Enable once gesture recognizers are implemented for AppKitView.
    // https://github.com/flutter/flutter/issues/128519
    [Fact(Skip = "Skipped in Flutter (flutter/flutter#128519): AppKitView has no gesture recognizers.")]
    public void AppKitViewAcceptsGestures()
    {
    }

    [Fact(Skip = "Skipped in Flutter (flutter/flutter#128519): AppKitView has no gesture recognizers.")]
    public void AppKitViewTransparentHitTestBehavior()
    {
    }

    [Fact(Skip = "Skipped in Flutter (flutter/flutter#128519): AppKitView has no gesture recognizers.")]
    public void AppKitViewTranslucentHitTestBehavior()
    {
    }

    [Fact(Skip = "Skipped in Flutter (flutter/flutter#128519): AppKitView has no gesture recognizers.")]
    public void AppKitViewOpaqueHitTestBehavior()
    {
    }

    [Fact]
    public void AppKitViewCanTakeInputFocus()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        var containerKey = new LabeledGlobalKey<State>("container");
        _tester.PumpWidget(FocusColumn(AppKitWebView(), containerKey));

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        FocusNode appKitViewFocusNode = FocusNodeBelow<AppKitView>(_tester);
        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _tester.Pump();

        Assert.True(containerFocusNode.HasFocus);
        Assert.False(appKitViewFocusNode.HasFocus);

        viewsController.InvokeViewFocused(currentViewId + 1);
        _tester.Pump();

        Assert.False(containerFocusNode.HasFocus);
        Assert.True(appKitViewFocusNode.HasFocus);
    }

    [Fact]
    public void AppKitViewSendsTextInputSetPlatformViewClientWhenFocused()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        _tester.PumpWidget(AppKitWebView());

        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();

        FocusNode focusNode = FocusNodeBelow<AppKitView>(_tester);
        Assert.False(focusNode.HasFocus);

        using MockMethodCallHandler textInput = MockTextInput();

        focusNode.RequestFocus();
        _tester.Pump();

        Assert.True(focusNode.HasFocus);
        Assert.Equal(
            currentViewId + 1,
            Convert.ToInt32(LastSetPlatformViewClient(textInput)!["platformViewId"]));
    }

    [Fact]
    public void FocusNodeIsDisposedOnAppKitViewDispose()
    {
        // Flutter reuses the UiKitView test's name: 'FocusNode is disposed on UIView dispose'.
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, AppKitWebView()));
        _tester.Pump();

        FocusNode node = _tester.State<AppKitViewState>().FocusNode!;
        ChangeNotifier.DebugAssertNotDisposed(node);

        _tester.PumpWidget(Centered(200.0, 100.0, null));
        Assert.ThrowsAny<Exception>(() => ChangeNotifier.DebugAssertNotDisposed(node));
    }

    [Fact]
    public void AppKitViewHasCorrectSemantics()
    {
        using SemanticsScope semantics = SemanticsScope.Ensure(_tester);
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        Assert.True(currentViewId >= 0);
        using FakeMacosPlatformViewsController viewsController = Macos("webview");

        _tester.PumpWidget(new Semantics(
            container: true,
            child: new Align(
                alignment: Alignment.BottomRight,
                child: new SizedBox(width: 200.0, height: 100.0, child: AppKitWebView()))));
        // First frame is before the platform view was created so the render object
        // is not yet in the tree.
        _tester.Pump();
        semantics.Flush();

        RenderObject renderObject = _tester.ElementOfType<AppKitPlatformView>().FindRenderObject()!;
        AssertPlatformViewSemantics(renderObject, platformViewId: currentViewId + 1);
    }
}
