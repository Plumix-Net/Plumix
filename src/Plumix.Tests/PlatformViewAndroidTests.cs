using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.PlatformViewTestKit;

namespace Plumix.Tests;

// Flutter's test/widgets/platform_view_test.dart, group('AndroidView') and group('AndroidViewSurface').
public sealed class PlatformViewAndroidTests : IDisposable
{
    private const int Ltr = AndroidViewController.AndroidLayoutDirectionLtr;
    private const int Rtl = AndroidViewController.AndroidLayoutDirectionRtl;

    private readonly FrameworkDartTester _tester = new(fakeGestureTimers: true);
    private readonly FakeAndroidPlatformViewsController _viewsController = new(dartAsyncReplies: true);

    public void Dispose()
    {
        _viewsController.Dispose();
        _tester.Dispose();
    }

    private static AndroidView WebView(
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) =>
        new(
            viewType: "webview",
            layoutDirection: TextDirection.Ltr,
            hitTestBehavior: hitTestBehavior,
            gestureRecognizers: gestureRecognizers,
            clipBehavior: clipBehavior,
            key: key);

    private List<FakeAndroidMotionEvent>? MotionEvents(int id) =>
        _viewsController.MotionEvents.GetValueOrDefault(id);

    private static FakeAndroidMotionEvent Motion(int action, Point position) => new(action, [0], [position]);

    private void AssertViews(params FakeAndroidPlatformView[] expected)
    {
        List<FakeAndroidPlatformView> actual = _viewsController.Views.ToList();
        Assert.Equal(expected.Length, actual.Count);
        foreach (FakeAndroidPlatformView view in expected)
        {
            Assert.Contains(view, actual);
        }
    }

    [Fact]
    public void CreateAndroidView()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "webview", new Size(200.0, 100.0), Ltr));
    }

    [Fact]
    public void CreateAndroidViewWithParams()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AndroidView(
                viewType: "webview",
                layoutDirection: TextDirection.Ltr,
                creationParams: "creation parameters",
                creationParamsCodec: new StringCodec())));

        FakeAndroidPlatformView fakeView = _viewsController.Views.First();
        byte[] rawCreationParams = fakeView.CreationParams!;
        string? actualParams = new StringCodec().DecodeMessage(ByteData.SublistView(rawCreationParams));
        Assert.Equal("creation parameters", actualParams);
        AssertViews(new FakeAndroidPlatformView(
            currentViewId + 1,
            "webview",
            new Size(200.0, 100.0),
            Ltr,
            creationParams: fakeView.CreationParams));
    }

    [Fact]
    public void ZeroSizedAndroidViewIsNotCreated()
    {
        _viewsController.RegisterViewType("webview");

        _tester.PumpWidget(new Center(child: SizedBox.Shrink(child: WebView())));

        Assert.Empty(_viewsController.Views);
    }

    [Fact]
    public void ResizeAndroidView()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _viewsController.ResizeCompleter = new TaskCompletionSource();

        _tester.PumpWidget(Centered(100.0, 50.0, WebView()));

        List<Layer> layers = Layers(_tester);
        var clipRectLayer = Assert.IsType<ClipRectLayer>(layers[^2]);
        Assert.Equal(new Rect(0.0, 0.0, 100.0, 50.0), clipRectLayer.ClipRect);

        // Resize is still in progress.
        // We should clip the texture to the size of the widget.
        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "webview", new Size(200.0, 100.0), Ltr));

        _viewsController.ResizeCompleter.SetResult();
        _tester.Pump();

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "webview", new Size(100.0, 50.0), Ltr));
    }

    [Fact]
    public void ChangeAndroidViewType()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _viewsController.RegisterViewType("maps");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AndroidView(viewType: "maps", layoutDirection: TextDirection.Ltr)));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 2, "maps", new Size(200.0, 100.0), Ltr));
    }

    [Fact]
    public void DisposeAndroidView()
    {
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        _tester.PumpWidget(Centered(200.0, 100.0, null));

        Assert.Empty(_viewsController.Views);
    }

    [Fact]
    public void AndroidViewSurvivesWidgetTreeChange()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        var key = new LabeledGlobalKey<State>("view");
        _tester.PumpWidget(Centered(200.0, 100.0, WebView(key: key)));

        _tester.PumpWidget(Centered(200.0, 100.0, WebView(key: key)));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "webview", new Size(200.0, 100.0), Ltr));
    }

    [Fact]
    public void AndroidViewGetsTouchEvents()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 50.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Theory]
    [InlineData(PlatformViewHitTestBehavior.Transparent, false, 1)]
    [InlineData(PlatformViewHitTestBehavior.Translucent, true, 1)]
    [InlineData(PlatformViewHitTestBehavior.Opaque, true, 0)]
    public void AndroidViewHitTestBehavior(
        PlatformViewHitTestBehavior behavior,
        bool viewGetsTheDown,
        int expectedParentDowns)
    {
        // Flutter: 'Android view transparent/translucent/opaque hit test behavior'.
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");

        int numPointerDownsOnParent = 0;
        _tester.PumpWidget(ParentListenerStack(
            new AndroidView(viewType: "webview", hitTestBehavior: behavior, layoutDirection: TextDirection.Ltr),
            () => numPointerDownsOnParent++));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);

        if (viewGetsTheDown)
        {
            Assert.Equal(
                [Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0))],
                MotionEvents(currentViewId + 1)!);
        }
        else
        {
            Assert.Null(MotionEvents(currentViewId + 1));
        }

        Assert.Equal(expectedParentDowns, numPointerDownsOnParent);

        // Finish gesture to release resources.
        gesture.Up();
        _tester.PumpAndSettle();
    }

    [Fact]
    public void AndroidViewTouchEventsAreInVirtualDisplaysCoordinateSystem()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new Container(
                margin: EdgeInsets.All(10.0),
                child: new SizedBox(width: 200.0, height: 100.0, child: WebView()))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(40.0, 40.0)),
                Motion(AndroidViewController.ActionUp, new Point(40.0, 40.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewDirectionality()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("maps");
        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AndroidView(viewType: "maps", layoutDirection: TextDirection.Rtl)));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "maps", new Size(200.0, 100.0), Rtl));

        _tester.PumpWidget(Centered(
            200.0,
            100.0,
            new AndroidView(viewType: "maps", layoutDirection: TextDirection.Ltr)));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "maps", new Size(200.0, 100.0), Ltr));
    }

    [Fact]
    public void AndroidViewAmbientDirectionality()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("maps");
        _tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Rtl,
            child: Centered(200.0, 100.0, new AndroidView(viewType: "maps"))));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "maps", new Size(200.0, 100.0), Rtl));

        _tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: Centered(200.0, 100.0, new AndroidView(viewType: "maps"))));

        AssertViews(new FakeAndroidPlatformView(currentViewId + 1, "maps", new Size(200.0, 100.0), Ltr));
    }

    [Fact]
    public void AndroidViewCanLoseGestureArenas()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");

        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new Container(
                margin: EdgeInsets.All(10.0),
                child: new GestureDetector(
                    onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                    child: new SizedBox(width: 200.0, height: 100.0, child: WebView())))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.True(verticalDragAcceptedByParent);
        Assert.Null(MotionEvents(currentViewId + 1));
    }

    [Fact]
    public void AndroidViewDragGestureRecognizer()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        bool verticalDragAcceptedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => verticalDragAcceptedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<VerticalDragGestureRecognizer>(() => new VerticalDragGestureRecognizer())))))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));
        gesture.Up();

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionMove, new Point(50.0, 150.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 150.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewLongPressGestureRecognizer()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        bool longPressAccessedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onLongPress: () => longPressAccessedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<LongPressGestureRecognizer>(() => new LongPressGestureRecognizer())))))));

        LongPressAt(_tester, new Point(50.0, 50.0));

        Assert.False(longPressAccessedByParent);
        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 50.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewTapGestureRecognizer()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        bool tapAccessedByParent = false;
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onTap: () => tapAccessedByParent = true,
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<TapGestureRecognizer>(() => new TapGestureRecognizer())))))));

        TapAt(_tester, new Point(50.0, 50.0));

        Assert.False(tapAccessedByParent);
        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 50.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewCanClaimGestureAfterAllPointersAreUp()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
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

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.Up();

        Assert.False(verticalDragAcceptedByParent);
        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 50.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewRebuiltDuringGesture()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, 100.0));

        _tester.PumpWidget(TopLeft(200.0, 100.0, WebView()));

        gesture.Up();

        Assert.Equal(
            [
                Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0)),
                Motion(AndroidViewController.ActionMove, new Point(50.0, 150.0)),
                Motion(AndroidViewController.ActionUp, new Point(50.0, 150.0)),
            ],
            MotionEvents(currentViewId + 1)!);
    }

    [Fact]
    public void AndroidViewWithEagerGestureRecognizer()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        _tester.PumpWidget(new Align(
            alignment: Alignment.TopLeft,
            child: new GestureDetector(
                onVerticalDragStart: _ => { },
                child: new SizedBox(
                    width: 200.0,
                    height: 100.0,
                    child: WebView(gestureRecognizers: Recognizers(
                        new Factory<OneSequenceGestureRecognizer>(() => new EagerGestureRecognizer())))))));

        TestGesture gesture = _tester.StartGesture(new Point(50.0, 50.0), PointerDeviceKind.Touch);

        // Normally (without the eager gesture recognizer) after just the pointer down event no gesture
        // recognizer will win the arena (so no motion events will be dispatched to the Android view).
        // Here we assert that with the eager recognizer in the gesture team the pointer down event is
        // immediately dispatched.
        Assert.Equal(
            [Motion(AndroidViewController.ActionDown, new Point(50.0, 50.0))],
            MotionEvents(currentViewId + 1)!);

        // Finish gesture to release resources.
        gesture.Up();
        _tester.PumpAndSettle();
    }

    // This test makes sure it doesn't crash.
    // https://github.com/flutter/flutter/issues/21514
    [Fact]
    public void RenderAndroidViewReconstructedWithSameGestureRecognizersDoesNotCrash()
    {
        _viewsController.RegisterViewType("webview");

        AndroidView androidView = WebView(gestureRecognizers: Recognizers(
            new Factory<EagerGestureRecognizer>(() => new EagerGestureRecognizer())));

        _tester.PumpWidget(androidView);
        _tester.PumpWidget(SizedBox.Shrink());
        _tester.PumpWidget(androidView);
    }

    [Fact]
    public void AndroidViewRebuiltWithSameGestureRecognizers()
    {
        _viewsController.RegisterViewType("webview");

        var factory = new CountingEagerFactory();
        _tester.PumpWidget(WebView(gestureRecognizers: Recognizers(
            new Factory<EagerGestureRecognizer>(factory.Construct))));

        _tester.PumpWidget(WebView(
            hitTestBehavior: PlatformViewHitTestBehavior.Translucent,
            gestureRecognizers: Recognizers(new Factory<EagerGestureRecognizer>(factory.Construct))));

        Assert.Equal(1, factory.Invocations);
    }

    [Fact]
    public void AndroidViewHasCorrectSemantics()
    {
        using SemanticsScope semantics = SemanticsScope.Ensure(_tester);
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        Assert.True(currentViewId >= 0);

        _viewsController.RegisterViewType("webview");
        _viewsController.CreateCompleter = new TaskCompletionSource();

        _tester.PumpWidget(new Semantics(
            container: true,
            child: new Align(
                alignment: Alignment.BottomRight,
                child: new SizedBox(width: 200.0, height: 100.0, child: WebView()))));
        semantics.Flush();

        RenderObject renderObject = _tester.ElementOfType<AndroidPlatformView>().FindRenderObject()!;
        // Platform view has not been created yet, no platformViewId.
        AssertPlatformViewSemantics(renderObject, platformViewId: null);

        _viewsController.CreateCompleter.SetResult();
        _tester.PumpAndSettle();
        semantics.Flush();

        AssertPlatformViewSemantics(renderObject, platformViewId: currentViewId + 1);
    }

    private Widget FocusColumn(Key containerKey) =>
        new Center(child: new Column(children:
        [
            new SizedBox(width: 200.0, height: 100.0, child: WebView()),
            new Focus(debugLabel: "container", child: new Container(key: containerKey)),
        ]));

    [Fact]
    public void AndroidViewCanTakeInputFocus()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        var containerKey = new LabeledGlobalKey<State>("container");
        _viewsController.CreateCompleter = new TaskCompletionSource();

        _tester.PumpWidget(FocusColumn(containerKey));

        FocusNode androidViewFocusNode = FocusNodeBelow<AndroidView>(_tester);
        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _viewsController.CreateCompleter.SetResult();

        _tester.Pump();

        Assert.True(containerFocusNode.HasFocus);
        Assert.False(androidViewFocusNode.HasFocus);

        _viewsController.InvokeViewFocused(currentViewId + 1);

        _tester.Pump();

        Assert.False(containerFocusNode.HasFocus);
        Assert.True(androidViewFocusNode.HasFocus);
    }

    [Fact]
    public void AndroidViewSetsAPlatformViewTextInputClientWhenFocused()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        var containerKey = new LabeledGlobalKey<State>("container");
        _viewsController.CreateCompleter = new TaskCompletionSource();

        _tester.PumpWidget(FocusColumn(containerKey));

        _viewsController.CreateCompleter.SetResult();

        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _tester.Pump();

        using MockMethodCallHandler textInput = MockTextInput();

        _viewsController.InvokeViewFocused(currentViewId + 1);
        _tester.Pump();

        IDictionary? lastPlatformViewTextClient = LastSetPlatformViewClient(textInput);
        Assert.NotNull(lastPlatformViewTextClient);
        Assert.True(lastPlatformViewTextClient!.Contains("platformViewId"));
        Assert.Equal(currentViewId + 1, Convert.ToInt32(lastPlatformViewTextClient["platformViewId"]));
    }

    [Fact]
    public void AndroidViewClearsPlatformFocusWhenUnfocused()
    {
        int currentViewId = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _viewsController.RegisterViewType("webview");
        var containerKey = new LabeledGlobalKey<State>("container");
        _viewsController.CreateCompleter = new TaskCompletionSource();

        _tester.PumpWidget(FocusColumn(containerKey));

        _viewsController.CreateCompleter.SetResult();

        FocusNode containerFocusNode = Focus.Of(_tester.ElementsWithKey(containerKey).Single());

        containerFocusNode.RequestFocus();
        _tester.Pump();

        _viewsController.InvokeViewFocused(currentViewId + 1);
        _tester.Pump();

        _viewsController.LastClearedFocusViewId = null;

        containerFocusNode.RequestFocus();
        _tester.Pump();

        Assert.Equal(currentViewId + 1, _viewsController.LastClearedFocusViewId);
    }

    [Fact]
    public void CanSetAndUpdateClipBehavior()
    {
        _viewsController.RegisterViewType("webview");

        _tester.PumpWidget(Centered(200.0, 100.0, WebView()));

        // By default, clipBehavior should be Clip.hardEdge
        var renderObject = RenderOf<AndroidPlatformView, RenderAndroidView>(_tester);
        Assert.Equal(Clip.HardEdge, renderObject.ClipBehavior);

        foreach (Clip clip in Enum.GetValues<Clip>())
        {
            _tester.PumpWidget(Centered(200.0, 100.0, WebView(clipBehavior: clip)));
            Assert.Equal(clip, renderObject.ClipBehavior);
        }
    }

    [Fact]
    public void ClipIsHandledCorrectlyDuringResizing()
    {
        // Regressing test for https://github.com/flutter/flutter/issues/67343
        _viewsController.RegisterViewType("webview");

        Widget BuildView(double width, double height, Clip clipBehavior) =>
            Centered(width, height, WebView(clipBehavior: clipBehavior));

        List<ClipRectLayer> ClipLayers() => Layers(_tester).OfType<ClipRectLayer>().ToList();

        _tester.PumpWidget(BuildView(200.0, 200.0, Clip.None));
        // Resize the view.
        _tester.PumpWidget(BuildView(100.0, 100.0, Clip.None));
        // No clip happen when the clip behavior is `Clip.none` .
        Assert.Empty(ClipLayers());

        // No clip when only the clip behavior changes while the size remains the same.
        _tester.PumpWidget(BuildView(100.0, 100.0, Clip.HardEdge));
        Assert.Empty(ClipLayers());

        // Resize trigger clip when the clip behavior is not Clip.none .
        _tester.PumpWidget(BuildView(50.0, 100.0, Clip.HardEdge));
        Assert.Equal(new Rect(0.0, 0.0, 50.0, 100.0), Assert.Single(ClipLayers()).ClipRect);

        _tester.PumpWidget(BuildView(50.0, 50.0, Clip.HardEdge));
        Assert.Equal(new Rect(0.0, 0.0, 50.0, 50.0), Assert.Single(ClipLayers()).ClipRect);
    }

    [Fact]
    public void OffsetIsSentToThePlatform()
    {
        _viewsController.RegisterViewType("webview");

        _tester.PumpWidget(new Padding(new EdgeInsets(10, 20, 0, 0), child: WebView()));
        _tester.Pump();

        Assert.Equal([new Point(10, 20)], _viewsController.Offsets.Values);
    }

    [Fact]
    public void OnFocusChangeReportsErrorWhenClearFocusFails()
    {
        _viewsController.RegisterViewType("webview");
        using var errors = new ErrorCapture();

        _tester.PumpWidget(new SizedBox(width: 200.0, height: 200.0, child: WebView()));
        _tester.Pump();

        FocusNode focusNode = FocusNodeBelow<AndroidView>(_tester);
        focusNode.RequestFocus();
        _tester.Pump();

        using var platformViews = new MockMethodCallHandler(SystemChannels.PlatformViews, call =>
            call.Method == "clearFocus" ? throw new Exception("clearFocus failed") : null);

        focusNode.Unfocus();
        _tester.PumpAndSettle();

        FlutterErrorDetails details = Assert.Single(errors.Errors);
        Assert.Contains("clearFocus failed", details.Exception.ToString());
        Assert.Contains("while clearing the platform view focus", details.Context!.ToString());
    }

    [Fact]
    public void OnFocusChangeReportsErrorWhenSetPlatformViewClientFails()
    {
        _viewsController.RegisterViewType("webview");
        using var errors = new ErrorCapture();

        _tester.PumpWidget(new SizedBox(width: 200.0, height: 200.0, child: WebView()));
        _tester.Pump();

        using MockMethodCallHandler textInput = MockTextInput(call =>
            call.Method == "TextInput.setPlatformViewClient"
                ? throw new Exception("setPlatformViewClient failed")
                : null);

        FocusNode focusNode = FocusNodeBelow<AndroidView>(_tester);
        focusNode.RequestFocus();
        _tester.PumpAndSettle();

        FlutterErrorDetails details = Assert.Single(errors.Errors);
        Assert.Contains("setPlatformViewClient failed", details.Exception.ToString());
        Assert.Contains("while setting the platform view client", details.Context!.ToString());
    }

    // group('AndroidViewSurface')

    [Fact]
    public void AndroidViewSurfaceSetsPointTransformerOfViewController()
    {
        var controller = new FakeAndroidViewController(0);
        _tester.PumpWidget(new AndroidViewSurface(
            controller: controller,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers()));
        Assert.True(controller.PointTransformerIsSet);
    }

    [Fact]
    public void AndroidViewSurfaceDefaultsToTextureBasedRendering()
    {
        var controller = new FakeAndroidViewController(0);
        _tester.PumpWidget(new AndroidViewSurface(
            controller: controller,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers()));

        Assert.Single(_tester.ElementsOfType<TextureBasedAndroidViewSurface>());
    }

    [Fact]
    public void AndroidViewSurfaceUsesViewBasedRenderingWhenInitiallyRequired()
    {
        var controller = new FakeAndroidViewController(0) { RequiresViewCompositionValue = true };
        _tester.PumpWidget(new AndroidViewSurface(
            controller: controller,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers()));

        Assert.Single(_tester.ElementsOfType<PlatformLayerBasedAndroidViewSurface>());
    }

    [Fact]
    public void AndroidViewSurfaceCanSwitchToViewBasedRenderingAfterCreation()
    {
        var controller = new FakeAndroidViewController(0);
        var surface = new AndroidViewSurface(
            controller: controller,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());
        _tester.PumpWidget(surface);

        Assert.Single(_tester.ElementsOfType<TextureBasedAndroidViewSurface>());
        Assert.Empty(_tester.ElementsOfType<PlatformLayerBasedAndroidViewSurface>());

        // Simulate a creation-time switch to view composition.
        controller.RequiresViewCompositionValue = true;
        foreach (PlatformViewCreatedCallback callback in controller.CreatedCallbacks.ToList())
        {
            callback(controller.ViewId);
        }

        _tester.PumpWidget(surface);

        Assert.Empty(_tester.ElementsOfType<TextureBasedAndroidViewSurface>());
        Assert.Single(_tester.ElementsOfType<PlatformLayerBasedAndroidViewSurface>());
    }
}

/// <summary>flutter_test's <c>tester.ensureSemantics()</c> plus the semantics flush the C# tester's
/// frames do not run.</summary>
internal sealed class SemanticsScope : IDisposable
{
    private readonly FrameworkDartTester _tester;
    private readonly SemanticsHandle _handle;

    private SemanticsScope(FrameworkDartTester tester, SemanticsHandle handle)
    {
        _tester = tester;
        _handle = handle;
    }

    public static SemanticsScope Ensure(FrameworkDartTester tester)
    {
        PipelineOwner owner = tester.RenderView.Owner!;
        bool createsOwner = owner.SemanticsOwner is null;
        SemanticsHandle handle = owner.EnsureSemantics();
        if (createsOwner)
        {
            tester.RenderView.ClearSemantics();
            tester.RenderView.ScheduleInitialSemantics();
        }

        return new SemanticsScope(tester, handle);
    }

    public void Flush() => _tester.RenderView.Owner!.FlushSemantics();

    public void Dispose() => _handle.Dispose();
}
