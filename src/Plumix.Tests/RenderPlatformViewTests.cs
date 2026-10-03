using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Flutter's test/rendering/platform_view_test.dart.
public sealed class RenderPlatformViewTests : IDisposable
{
    private readonly RenderingHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(null);
    }

    private static IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> VerticalDrag() =>
        new HashSet<IFactory<OneSequenceGestureRecognizer>>
        {
            new Factory<VerticalDragGestureRecognizer>(() => new VerticalDragGestureRecognizer()),
        };

    private static IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> NoRecognizers() =>
        new HashSet<IFactory<OneSequenceGestureRecognizer>>();

    // group('PlatformViewRenderBox')

    [Fact]
    public void PlatformViewRenderBox_LayoutShouldSizeToMaxConstraint()
    {
        var fakePlatformViewController = new FakePlatformViewController(0);
        var platformViewRenderBox = new PlatformViewRenderBox(
            controller: fakePlatformViewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: VerticalDrag());
        _harness.Layout(platformViewRenderBox);
        platformViewRenderBox.Layout(
            new BoxConstraints(MinWidth: 50, MinHeight: 50, MaxWidth: 100, MaxHeight: 100));
        Assert.Equal(new Size(100, 100), platformViewRenderBox.Size);
    }

    [Fact]
    public void PlatformViewRenderBox_SendSemanticsUpdateIfIdIsChanged()
    {
        var fakePlatformViewController = new FakePlatformViewController(0);
        var platformViewRenderBox = new PlatformViewRenderBox(
            controller: fakePlatformViewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: VerticalDrag());
        var tree = new RenderConstrainedBox(
            additionalConstraints: BoxConstraints.TightFor(height: 20.0, width: 20.0),
            child: platformViewRenderBox);
        int semanticsUpdateCount = 0;
        SemanticsHandle semanticsHandle = _harness.EnsureSemantics(() => ++semanticsUpdateCount);
        _harness.Layout(tree, phase: RenderingHarness.Phase.FlushSemantics);
        // Initial semantics update
        Assert.Equal(1, semanticsUpdateCount);

        semanticsUpdateCount = 0;

        // Request semantics update even though nothing changed.
        platformViewRenderBox.MarkNeedsSemanticsUpdate();
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);
        Assert.Equal(0, semanticsUpdateCount);

        semanticsUpdateCount = 0;

        var updatedFakePlatformViewController = new FakePlatformViewController(10);
        platformViewRenderBox.Controller = updatedFakePlatformViewController;
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);
        // Update id should update the semantics.
        Assert.Equal(1, semanticsUpdateCount);
        semanticsHandle.Dispose();
    }

    [Theory]
    [InlineData(PointerDeviceKind.Mouse)]
    [InlineData(PointerDeviceKind.Touch)]
    public void PlatformViewRenderBox_HoverEventsAreDispatchedViaDispatchPointerEvent(PointerDeviceKind kind)
    {
        // Flutter: 'mouse hover events are dispatched via PlatformViewController.dispatchPointerEvent'
        // and 'touch hover events ...'; the Dart packets of the second test do not pass a kind, so both
        // send mouse events. The touch variant here sends real touch hovers.
        var fakePlatformViewController = new FakePlatformViewController(0);
        var platformViewRenderBox = new PlatformViewRenderBox(
            controller: fakePlatformViewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: VerticalDrag());
        _harness.Layout(platformViewRenderBox);
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);

        _harness.Send(new PointerAddedEvent(kind: kind, position: default));
        _harness.Send(new PointerHoverEvent(kind: kind, position: new Point(10, 10)));
        _harness.Send(new PointerRemovedEvent(kind: kind, position: new Point(10, 10)));

        Assert.NotEmpty(fakePlatformViewController.DispatchedPointerEvents);
        Assert.IsType<PointerHoverEvent>(fakePlatformViewController.DispatchedPointerEvents[0]);
    }

    [Fact]
    public void PlatformViewRenderBox_HasTransparentHitTestBehaviorInSemantics()
    {
        var box = new DescribingPlatformViewRenderBox(new FakePlatformViewController(0));
        var config = new SemanticsConfiguration();
        box.Describe(config);
        Assert.Equal(SemanticsHitTestBehavior.Transparent, config.HitTestBehavior);
        Assert.True(config.IsSemanticBoundary);
        Assert.Equal(0, config.PlatformViewId);
    }

    [Fact]
    public void PlatformViewRenderBox_HitTestBehaviorDecidesTheHitTestResult()
    {
        var box = new PlatformViewRenderBox(
            controller: new FakePlatformViewController(0),
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());
        _harness.Layout(box);

        var opaque = new BoxHitTestResult();
        Assert.True(box.HitTest(opaque, new Point(10, 10)));
        Assert.Single(opaque.Path);

        box.HitTestBehavior = PlatformViewHitTestBehavior.Translucent;
        var translucent = new BoxHitTestResult();
        Assert.False(box.HitTest(translucent, new Point(10, 10)));
        Assert.Single(translucent.Path);

        box.HitTestBehavior = PlatformViewHitTestBehavior.Transparent;
        var transparent = new BoxHitTestResult();
        Assert.False(box.HitTest(transparent, new Point(10, 10)));
        Assert.Empty(transparent.Path);

        box.HitTestBehavior = PlatformViewHitTestBehavior.Opaque;
        Assert.False(box.HitTest(new BoxHitTestResult(), new Point(800, 10)));
    }

    [Fact]
    public void PlatformViewRenderBox_PaintsAPlatformViewLayerAndIsARepaintBoundary()
    {
        var box = new PlatformViewRenderBox(
            controller: new FakePlatformViewController(7),
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());
        _harness.Layout(box, phase: RenderingHarness.Phase.Paint);

        Assert.True(box.IsRepaintBoundary);
        Assert.True(box.AlwaysNeedsCompositing);
        Assert.Equal(MouseCursor.Uncontrolled, box.Cursor);
        Assert.True(box.ValidForMouseTracker);
        var layer = Assert.IsType<PlatformViewLayer>(((ContainerLayer)box.DebugLayer!).FirstChild);
        Assert.Equal(7, layer.ViewId);
        Assert.Equal(new Rect(0, 0, 800, 600), layer.Rect);
    }

    [Fact]
    public void MultiFingerTouchTest()
    {
        using var viewsController = new FakeAndroidPlatformViewsController();
        viewsController.RegisterViewType("webview");
        AndroidViewController viewController = PlatformViewsService.InitAndroidView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Rtl);
        var platformViewRenderBox = new PlatformViewRenderBox(
            controller: viewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: VerticalDrag());
        _harness.Layout(platformViewRenderBox);
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);

        viewController.PointTransformer = offset => platformViewRenderBox.GlobalToLocal(offset);

        _harness.Send(new PointerAddedEvent(pointer: 1, position: default));
        _harness.Send(new PointerDownEvent(pointer: 1, position: new Point(10, 10)));
        _harness.Send(new PointerRemovedEvent(pointer: 1, position: new Point(10, 10)));
        Scheduler.FlushMicrotasks();

        _harness.Send(new PointerAddedEvent(pointer: 2, position: default));
        _harness.Send(new PointerDownEvent(pointer: 2, position: new Point(20, 10)));
        _harness.Send(new PointerCancelEvent(pointer: 2, position: new Point(20, 10)));
        Scheduler.FlushMicrotasks();

        _harness.Send(new PointerAddedEvent(pointer: 1, position: default));
        _harness.Send(new PointerMoveEvent(pointer: 1, position: new Point(10, 10)));
        _harness.Send(new PointerRemovedEvent(pointer: 1, position: new Point(10, 10)));
        Scheduler.FlushMicrotasks();
    }

    [Fact]
    public void CreatedCallbackIsResetWhenControllerIsChanged()
    {
        using var viewsController = new FakeAndroidPlatformViewsController();
        viewsController.RegisterViewType("webview");
        AndroidViewController firstController = PlatformViewsService.InitAndroidView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Rtl);
        var renderBox = new RenderAndroidView(
            viewController: firstController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());
        _harness.Layout(renderBox);
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);

        Assert.Single(firstController.CreatedCallbacks);

        AndroidViewController secondController = PlatformViewsService.InitAndroidView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Rtl);
        // Reset controller.
        renderBox.Controller = secondController;

        Assert.Empty(firstController.CreatedCallbacks);
        Assert.Single(secondController.CreatedCallbacks);
    }

    [Fact]
    public void RenderObjectChangedItsVisualAppearanceAfterTextureIsCreated()
    {
        var viewCreation = new TaskCompletionSource();
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(async call =>
        {
            Assert.Equal("create", call.Method);
            await viewCreation.Task;
            return 0;
        });

        AndroidViewController viewController = PlatformViewsService.InitAndroidView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Rtl);
        var renderBox = new RenderAndroidView(
            viewController: viewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());

        _harness.Layout(renderBox);
        _harness.PumpFrame(RenderingHarness.Phase.Paint);

        Assert.NotNull(renderBox.DebugLayer);
        Assert.False(((ContainerLayer)renderBox.DebugLayer!).HasChildren);
        Assert.False(viewController.IsCreated);
        Assert.False(renderBox.DebugNeedsPaint);

        viewCreation.SetResult();
        Scheduler.FlushMicrotasks();

        Assert.True(viewController.IsCreated);
        Assert.True(renderBox.DebugNeedsPaint);
        Assert.False(((ContainerLayer)renderBox.DebugLayer!).HasChildren);

        _harness.PumpFrame(RenderingHarness.Phase.Paint);
        Assert.True(((ContainerLayer)renderBox.DebugLayer!).HasChildren);
        Assert.IsType<TextureLayer>(((ContainerLayer)renderBox.DebugLayer!).FirstChild);
    }

    [Fact]
    public void MarkNeedsPaintDoesNotGetCalledOnADisposedRenderObject()
    {
        var viewCreation = new TaskCompletionSource();
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(async call =>
        {
            Assert.Equal("create", call.Method);
            await viewCreation.Task;
            return 0;
        });

        AndroidViewController viewController = PlatformViewsService.InitAndroidView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Rtl);
        var renderBox = new RenderAndroidView(
            viewController: viewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());

        _harness.Layout(renderBox);
        _harness.PumpFrame(RenderingHarness.Phase.Paint);

        Assert.NotNull(renderBox.DebugLayer);
        Assert.False(((ContainerLayer)renderBox.DebugLayer!).HasChildren);
        Assert.False(viewController.IsCreated);
        Assert.False(renderBox.DebugNeedsPaint);

        _harness.DetachChild();
        renderBox.Dispose();
        viewCreation.SetResult();
        Scheduler.FlushMicrotasks();

        Assert.True(viewController.IsCreated);
        Assert.False(renderBox.DebugNeedsPaint);
        Assert.Null(renderBox.DebugLayer);

        _harness.PumpFrame(RenderingHarness.Phase.Paint);
        Assert.Null(renderBox.DebugLayer);
    }

    [Fact]
    public async Task MarkNeedsPaintDoesNotGetCalledWhenSettingTheSameViewController()
    {
        var viewCreation = new TaskCompletionSource();
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(async call =>
        {
            Assert.Equal("create", call.Method);
            await viewCreation.Task;
            return 0;
        });

        Task<UiKitViewController> pending = PlatformViewsService.InitUiKitView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Ltr);
        viewCreation.SetResult();
        UiKitViewController viewController = await pending;

        var renderBox = new RenderUiKitView(
            viewController: viewController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());

        _harness.Layout(renderBox);
        _harness.PumpFrame(RenderingHarness.Phase.Paint);

        Assert.False(renderBox.DebugNeedsPaint);

        renderBox.ViewController = viewController;

        Assert.False(renderBox.DebugNeedsPaint);
    }

    // group('RenderDarwinPlatformView')

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DarwinView_DoesNotHandlePointerEventsWhenNotLaidOut(bool appKit)
    {
        InstallDarwinHandler();
        RenderBox renderBox = await CreateDarwinView(appKit);

        renderBox.Attach(_harness.Owner);
        Assert.True(renderBox.DebugNeedsLayout);
        Assert.Equal(0, _rejections);

        GestureBinding.Instance.PointerRouter.Route(new PointerDownEvent(position: new Point(10, 10)));

        Assert.Equal(0, _rejections);

        renderBox.Detach();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DarwinView_HandlesPointerEventsWhenLaidOut(bool appKit)
    {
        InstallDarwinHandler();
        RenderBox renderBox = await CreateDarwinView(appKit);

        Assert.Equal(0, _rejections);

        GestureBinding.Instance.PointerRouter.Route(new PointerDownEvent(position: new Point(10, 10)));

        Assert.Equal(0, _rejections);

        _harness.Layout(renderBox);
        _harness.PumpFrame(RenderingHarness.Phase.FlushSemantics);
        Assert.False(renderBox.DebugNeedsLayout);

        GestureBinding.Instance.PointerRouter.Route(new PointerDownEvent(position: new Point(10, 10)));

        Assert.Equal(1, _rejections);
    }

    [Fact]
    public async Task UiKitView_DescribesItsPlatformViewIdInSemantics()
    {
        InstallDarwinHandler();
        var renderBox = (RenderUiKitView)await CreateDarwinView(appKit: false);
        Assert.Equal(PlatformViewHitTestBehavior.Opaque, renderBox.HitTestBehavior);
        _harness.Layout(renderBox, phase: RenderingHarness.Phase.Paint);
        var layer = Assert.IsType<PlatformViewLayer>(((ContainerLayer)renderBox.DebugLayer!).FirstChild);
        Assert.Equal(renderBox.ViewController.Id, layer.ViewId);
    }

    private int _rejections;

    // The group's setUp: 'create' awaits a completer, 'rejectGesture' is counted, anything else throws.
    private void InstallDarwinHandler()
    {
        _rejections = 0;
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(call =>
        {
            switch (call.Method)
            {
                case "create":
                    return Task.FromResult<object?>(0);
                case "rejectGesture":
                    _rejections++;
                    return Task.FromResult<object?>(0);
                default:
                    throw new NotSupportedException($"Unexpected method call {call.Method}.");
            }
        });
    }

    private static async Task<RenderBox> CreateDarwinView(bool appKit)
    {
        if (appKit)
        {
            AppKitViewController appKitController = await PlatformViewsService.InitAppKitView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            return new RenderAppKitView(
                viewController: appKitController,
                hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
                gestureRecognizers: NoRecognizers());
        }

        UiKitViewController uiKitController = await PlatformViewsService.InitUiKitView(
            id: 0,
            viewType: "webview",
            layoutDirection: TextDirection.Ltr);
        return new RenderUiKitView(
            viewController: uiKitController,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: NoRecognizers());
    }

    private sealed class DescribingPlatformViewRenderBox(PlatformViewController controller)
        : PlatformViewRenderBox(
            controller: controller,
            hitTestBehavior: PlatformViewHitTestBehavior.Opaque,
            gestureRecognizers: new HashSet<IFactory<OneSequenceGestureRecognizer>>())
    {
        public void Describe(SemanticsConfiguration config) => DescribeSemanticsConfiguration(config);
    }

    /// <summary>rendering_tester.dart's <c>layout</c>/<c>pumpFrame</c> over an 800x600 render view.</summary>
    private sealed class RenderingHarness : IDisposable
    {
        private readonly RenderView _view;

        public RenderingHarness()
        {
            GestureBinding.Instance.ResetForTests();
            _view = new RenderView(new FlutterView(new Size(800, 600)));
            Owner = new PipelineOwner(_view);
            Owner.Attach(_view);
        }

        public enum Phase
        {
            Layout,
            Paint,
            FlushSemantics,
        }

        public PipelineOwner Owner { get; }

        public SemanticsHandle EnsureSemantics(Action listener)
        {
            SemanticsHandle handle = Owner.EnsureSemantics(listener);
            _view.ScheduleInitialSemantics();
            return handle;
        }

        public void Layout(RenderBox box, Phase phase = Phase.Layout)
        {
            _view.Child = box;
            PumpFrame(phase);
        }

        public void DetachChild() => _view.Child = null;

        public void PumpFrame(Phase phase = Phase.Layout)
        {
            Owner.FlushLayout();
            if (phase == Phase.Layout)
            {
                return;
            }

            Owner.FlushCompositingBits();
            Owner.FlushPaint();
            if (phase == Phase.Paint)
            {
                return;
            }

            Owner.FlushSemantics();
        }

        public void Send(PointerEvent @event) => GestureBinding.Instance.HandlePointerEvent(_view, @event);

        public void Dispose()
        {
            _view.Child = null;
        }
    }
}
