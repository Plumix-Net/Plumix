using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Flutter's test/services/platform_views_test.dart.
public sealed class PlatformViewsServiceTests
{
    public sealed class Android : IDisposable
    {
        private readonly FakeAndroidPlatformViewsController _viewsController = new();

        public void Dispose() => _viewsController.Dispose();

        [Fact]
        public async Task CreateAndroidViewOfUnregisteredType()
        {
            await Assert.ThrowsAsync<PlatformException>(() => PlatformViewsService
                .InitAndroidView(id: 0, viewType: "web", layoutDirection: TextDirection.Ltr)
                .SetSize(new Size(100.0, 100.0)));

            _viewsController.RegisterViewType("web");

            await PlatformViewsService
                .InitSurfaceAndroidView(id: 0, viewType: "web", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(1.0, 1.0));

            await PlatformViewsService
                .InitAndroidView(id: 1, viewType: "web", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(1.0, 1.0));
        }

        [Fact]
        public async Task CreateVdFallbackAndroidViews()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            await PlatformViewsService
                .InitAndroidView(id: 1, viewType: "webview", layoutDirection: TextDirection.Rtl)
                .Create(size: new Size(200.0, 300.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionLtr),
                    new FakeAndroidPlatformView(
                        1,
                        "webview",
                        new Size(200.0, 300.0),
                        AndroidViewController.AndroidLayoutDirectionRtl),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task CreateHcFallbackAndroidViews()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitSurfaceAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            await PlatformViewsService
                .InitSurfaceAndroidView(id: 1, viewType: "webview", layoutDirection: TextDirection.Rtl)
                .Create(size: new Size(200.0, 300.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionLtr,
                        hybridFallback: true),
                    new FakeAndroidPlatformView(
                        1,
                        "webview",
                        new Size(200.0, 300.0),
                        AndroidViewController.AndroidLayoutDirectionRtl,
                        hybridFallback: true),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task CreateHcOnlyAndroidViews()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitExpensiveAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            await PlatformViewsService
                .InitExpensiveAndroidView(id: 1, viewType: "webview", layoutDirection: TextDirection.Rtl)
                .Create(size: new Size(200.0, 300.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        null,
                        AndroidViewController.AndroidLayoutDirectionLtr,
                        hybrid: true),
                    new FakeAndroidPlatformView(
                        1,
                        "webview",
                        null,
                        AndroidViewController.AndroidLayoutDirectionRtl,
                        hybrid: true),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task DefaultViewDoesNotUseViewCompositionByDefault()
        {
            _viewsController.RegisterViewType("webview");

            AndroidViewController controller = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await controller.Create(size: new Size(100.0, 100.0));
            Assert.False(controller.RequiresViewComposition);
        }

        [Fact]
        public async Task DefaultViewDoesNotUseViewCompositionInFallbackMode()
        {
            _viewsController.RegisterViewType("webview");
            _viewsController.AllowTextureLayerMode = false;

            AndroidViewController controller = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await controller.Create(size: new Size(100.0, 100.0));
            _viewsController.AllowTextureLayerMode = true;
            Assert.False(controller.RequiresViewComposition);
        }

        [Fact]
        public async Task SurfaceViewDoesNotUseViewCompositionByDefault()
        {
            _viewsController.RegisterViewType("webview");

            AndroidViewController controller = PlatformViewsService.InitSurfaceAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await controller.Create(size: new Size(100.0, 100.0));
            Assert.False(controller.RequiresViewComposition);
        }

        [Fact]
        public async Task SurfaceViewDoesUsesViewCompositionInFallbackMode()
        {
            _viewsController.RegisterViewType("webview");
            _viewsController.AllowTextureLayerMode = false;

            AndroidViewController controller = PlatformViewsService.InitSurfaceAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await controller.Create(size: new Size(100.0, 100.0));
            _viewsController.AllowTextureLayerMode = true;
            Assert.True(controller.RequiresViewComposition);
        }

        [Fact]
        public async Task ExpensiveViewUsesViewComposition()
        {
            _viewsController.RegisterViewType("webview");

            AndroidViewController controller = PlatformViewsService.InitExpensiveAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await controller.Create(size: new Size(100.0, 100.0));
            Assert.True(controller.RequiresViewComposition);
        }

        [Fact]
        public async Task ReuseAndroidViewId()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            await Assert.ThrowsAsync<PlatformException>(() => PlatformViewsService
                .InitAndroidView(id: 0, viewType: "web", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0)));
        }

        [Fact]
        public async Task DisposeAndroidView()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await viewController.Create(size: new Size(200.0, 300.0));
            await viewController.Dispose();

            AndroidViewController surfaceViewController = PlatformViewsService.InitSurfaceAndroidView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await surfaceViewController.Create(size: new Size(200.0, 300.0));
            await surfaceViewController.Dispose();

            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionLtr),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task DisposeAndroidViewTwice()
        {
            _viewsController.RegisterViewType("webview");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await viewController.Create(size: new Size(200.0, 300.0));
            await viewController.Dispose();
            await viewController.Dispose();
        }

        [Fact]
        public async Task DisposeClearsFocusCallbacks()
        {
            bool didFocus = false;
            _viewsController.RegisterViewType("webview");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr,
                onFocus: () => didFocus = true);
            await viewController.Create(size: new Size(100.0, 100.0));
            await viewController.Dispose();
            ByteData message = SystemChannels.PlatformViews.Codec.EncodeMethodCall(new MethodCall("viewFocused", 0));
            await ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                SystemChannels.PlatformViews.Name,
                message,
                _ => { });
            Assert.False(didFocus);
        }

        [Fact]
        public async Task ResizeAndroidView()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService
                .InitAndroidView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr)
                .Create(size: new Size(100.0, 100.0));
            AndroidViewController androidView = PlatformViewsService.InitAndroidView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await androidView.SetSize(new Size(200.0, 300.0));
            await androidView.SetSize(new Size(500.0, 500.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionLtr),
                    new FakeAndroidPlatformView(
                        1,
                        "webview",
                        new Size(500.0, 500.0),
                        AndroidViewController.AndroidLayoutDirectionLtr),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task OnPlatformViewCreatedCallback()
        {
            _viewsController.RegisterViewType("webview");
            var createdViews = new List<int>();
            void Callback(int id) => createdViews.Add(id);

            AndroidViewController controller1 = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            controller1.AddOnPlatformViewCreatedListener(Callback);
            Assert.Empty(createdViews);

            await controller1.Create(size: new Size(100.0, 100.0));
            Assert.Equal([0], createdViews);

            AndroidViewController controller2 = PlatformViewsService.InitAndroidView(
                id: 5,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            controller2.AddOnPlatformViewCreatedListener(Callback);
            Assert.Equal([0], createdViews);

            await controller2.Create(size: new Size(100.0, 200.0));
            Assert.Equal([0, 5], createdViews);

            AndroidViewController controller3 = PlatformViewsService.InitAndroidView(
                id: 10,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            controller3.AddOnPlatformViewCreatedListener(Callback);
            Assert.Equal([0, 5], createdViews);

            // Dart's channel replies arrive in a later microtask, so `dispose` runs while `create` awaits
            // the platform; Plumix's mock replies synchronously, so the fake holds the replies back.
            _viewsController.ReplyDelay = new TaskCompletionSource();
            Task create = controller3.Create(size: new Size(100.0, 200.0));
            Task dispose = controller3.Dispose();
            _viewsController.ReplyDelay.SetResult();
            await Task.WhenAll(create, dispose);
            Assert.Equal([0, 5], createdViews);
        }

        [Fact]
        public async Task ChangeAndroidViewsDirectionalityBeforeCreation()
        {
            _viewsController.RegisterViewType("webview");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Rtl);
            await viewController.SetLayoutDirection(TextDirection.Ltr);
            await viewController.Create(size: new Size(100.0, 100.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionLtr),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task ChangeAndroidViewsDirectionalityAfterCreation()
        {
            _viewsController.RegisterViewType("webview");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 0,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await viewController.SetLayoutDirection(TextDirection.Rtl);
            await viewController.Create(size: new Size(100.0, 100.0));
            AssertUnordered(
                [
                    new FakeAndroidPlatformView(
                        0,
                        "webview",
                        new Size(100.0, 100.0),
                        AndroidViewController.AndroidLayoutDirectionRtl),
                ],
                _viewsController.Views);
        }

        [Fact]
        public async Task SetAndroidViewsOffsetIfViewIsCreated()
        {
            _viewsController.RegisterViewType("web");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 7,
                viewType: "web",
                layoutDirection: TextDirection.Ltr);
            await viewController.Create(size: new Size(100.0, 100.0));
            await viewController.SetOffset(new Point(10, 20));
            Assert.Equal(new Dictionary<int, Point> { [7] = new Point(10, 20) }, _viewsController.Offsets);
        }

        [Fact]
        public async Task DoesNotSetAndroidViewsOffsetIfViewIsNotCreated()
        {
            _viewsController.RegisterViewType("web");
            AndroidViewController viewController = PlatformViewsService.InitAndroidView(
                id: 7,
                viewType: "web",
                layoutDirection: TextDirection.Ltr);
            await viewController.SetOffset(new Point(10, 20));
            Assert.Empty(_viewsController.Offsets);
        }

        // Flutter: 'motion event converter does not duplicate move events'. The test replaces the
        // fake's handler with a logging one, as Dart does.
        [Fact]
        public async Task MotionEventConverterDoesNotDuplicateMoveEvents()
        {
            var log = new List<MethodCall>();
            SystemChannels.PlatformViews.SetPlatformMethodCallHandler(call =>
            {
                log.Add(call);
                return Task.FromResult<object?>(null);
            });

            AndroidViewController controller = PlatformViewsService.InitSurfaceAndroidView(
                id: 7,
                viewType: "web",
                layoutDirection: TextDirection.Ltr);
            controller.PointTransformer = position => position;

            DateTime oneMillisecond = default(DateTime) + TimeSpan.FromMilliseconds(1);
            DateTime twoMilliseconds = default(DateTime) + TimeSpan.FromMilliseconds(2);
            for (int i = 0; i < 10; i++)
            {
                await controller.DispatchPointerEvent(new PointerDownEvent(pointer: i, timestampUtc: oneMillisecond));
            }

            for (int i = 0; i < 10; i++)
            {
                await controller.DispatchPointerEvent(new PointerMoveEvent(
                    pointer: i,
                    timestampUtc: twoMilliseconds,
                    platformData: 2 | (10 << 8)));
            }

            List<MethodCall> moves = log
                .Where(call => call.Method == "touch")
                .Where(call => Convert.ToInt32(((IList)call.Arguments!)[3]) == AndroidViewController.ActionMove)
                .ToList();
            MethodCall move = Assert.Single(moves);
            Assert.Equal(10, Convert.ToInt32(((IList)move.Arguments!)[4]));

            // The downs still send `touch` calls: the first a plain down, the rest pointer downs.
            List<int> downActions = log
                .Where(call => call.Method == "touch")
                .Select(call => Convert.ToInt32(((IList)call.Arguments!)[3]))
                .Where(action => action != AndroidViewController.ActionMove)
                .ToList();
            Assert.Equal(
                Enumerable.Range(0, 10)
                    .Select(i => i == 0
                        ? AndroidViewController.ActionDown
                        : AndroidViewController.PointerAction(i, AndroidViewController.ActionPointerDown)),
                downActions);
        }

        [Fact]
        public async Task TouchMessageCarriesTheMotionEventLayout()
        {
            var log = new List<MethodCall>();
            SystemChannels.PlatformViews.SetPlatformMethodCallHandler(call =>
            {
                log.Add(call);
                return Task.FromResult<object?>(call.Method == "create" ? 3 : null);
            });

            AndroidViewController controller = PlatformViewsService.InitAndroidView(
                id: 4,
                viewType: "web",
                layoutDirection: TextDirection.Ltr);
            controller.PointTransformer = position => new Point(position.X - 5, position.Y - 5);
            await controller.DispatchPointerEvent(new PointerDownEvent(
                pointer: 9,
                kind: PointerDeviceKind.Mouse,
                position: new Point(15, 25),
                embedderId: 42));

            MethodCall touch = Assert.Single(log, call => call.Method == "touch");
            var args = (IList)touch.Arguments!;
            Assert.Equal(16, args.Count);
            Assert.Equal(4, Convert.ToInt32(args[0]));
            Assert.Equal(AndroidViewController.ActionDown, Convert.ToInt32(args[3]));
            Assert.Equal(1, Convert.ToInt32(args[4]));
            var properties = (IList)((IList)args[5]!)[0]!;
            Assert.Equal(0, Convert.ToInt32(properties[0]));
            Assert.Equal(AndroidPointerProperties.ToolTypeMouse, Convert.ToInt32(properties[1]));
            var coords = (IList)((IList)args[6]!)[0]!;
            Assert.Equal(9, coords.Count);
            Assert.Equal(10.0, Convert.ToDouble(coords[7]));
            Assert.Equal(20.0, Convert.ToDouble(coords[8]));
            Assert.Equal(1.0, Convert.ToDouble(args[9]));
            Assert.Equal(1.0, Convert.ToDouble(args[10]));
            Assert.Equal(AndroidViewController.InputDeviceSourceMouse, Convert.ToInt32(args[13]));
            Assert.Equal(42, Convert.ToInt32(args[15]));
        }

        [Fact]
        public async Task CreateMessageCarriesDartsArgumentKeys()
        {
            var log = new List<MethodCall>();
            SystemChannels.PlatformViews.SetPlatformMethodCallHandler(call =>
            {
                log.Add(call);
                return Task.FromResult<object?>(null);
            });

            await PlatformViewsService
                .InitSurfaceAndroidView(
                    id: 2,
                    viewType: "web",
                    layoutDirection: TextDirection.Rtl,
                    creationParams: "params",
                    creationParamsCodec: new StringCodec())
                .Create(size: new Size(10, 20), position: new Point(1, 2));

            var args = (IDictionary)Assert.Single(log).Arguments!;
            Assert.Equal(
                ["id", "viewType", "direction", "width", "height", "hybridFallback", "left", "top", "params"],
                args.Keys.Cast<string>());
            Assert.Equal(AndroidViewController.AndroidLayoutDirectionRtl, Convert.ToInt32(args["direction"]));
            Assert.Equal("params", new StringCodec().DecodeMessage(ByteData.SublistView((byte[])args["params"]!)));
        }

        private static void AssertUnordered(
            IEnumerable<FakeAndroidPlatformView> expected,
            IEnumerable<FakeAndroidPlatformView> actual)
        {
            var actualList = actual.ToList();
            var expectedList = expected.ToList();
            Assert.Equal(expectedList.Count, actualList.Count);
            foreach (FakeAndroidPlatformView view in expectedList)
            {
                Assert.Contains(view, actualList);
            }
        }
    }

    public sealed class Ios : IDisposable
    {
        private readonly FakeIosPlatformViewsController _viewsController = new();

        public void Dispose() => _viewsController.Dispose();

        [Fact]
        public async Task CreateIosViewOfUnregisteredType()
        {
            await Assert.ThrowsAsync<PlatformException>(() => PlatformViewsService.InitUiKitView(
                id: 0,
                viewType: "web",
                layoutDirection: TextDirection.Ltr));
        }

        [Fact]
        public async Task CreateIosViews()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService.InitUiKitView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr);
            await PlatformViewsService.InitUiKitView(id: 1, viewType: "webview", layoutDirection: TextDirection.Rtl);
            Assert.Equal(
                new HashSet<FakeUiKitView> { new(0, "webview"), new(1, "webview") },
                _viewsController.Views.ToHashSet());
        }

        [Fact]
        public async Task ReuseIosViewId()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService.InitUiKitView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr);
            await Assert.ThrowsAsync<PlatformException>(() => PlatformViewsService.InitUiKitView(
                id: 0,
                viewType: "web",
                layoutDirection: TextDirection.Ltr));
        }

        [Fact]
        public async Task DisposeIosView()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService.InitUiKitView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr);
            UiKitViewController viewController = await PlatformViewsService.InitUiKitView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);

            await viewController.Dispose();
            Assert.Equal([new FakeUiKitView(0, "webview")], _viewsController.Views);
        }

        [Fact]
        public async Task DisposeInexistingIosView()
        {
            _viewsController.RegisterViewType("webview");
            await PlatformViewsService.InitUiKitView(id: 0, viewType: "webview", layoutDirection: TextDirection.Ltr);
            UiKitViewController viewController = await PlatformViewsService.InitUiKitView(
                id: 1,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr);
            await viewController.Dispose();
            PlatformException error = await Assert.ThrowsAsync<PlatformException>(() => viewController.Dispose());
            Assert.Equal("Trying to dispose a platform view with unknown id: 1", error.ErrorMessage);
        }

        [Fact]
        public async Task CreateSendsTheGestureBlockingPolicyAndParams()
        {
            var log = new List<MethodCall>();
            SystemChannels.PlatformViews.SetPlatformMethodCallHandler(call =>
            {
                log.Add(call);
                return Task.FromResult<object?>(null);
            });

            await PlatformViewsService.InitUiKitView(
                id: 3,
                viewType: "webview",
                layoutDirection: TextDirection.Ltr,
                gestureBlockingPolicy: UiKitViewGestureBlockingPolicy.WaitUntilTouchesEnded,
                creationParams: "p",
                creationParamsCodec: new StringCodec());
            var args = (IDictionary)Assert.Single(log).Arguments!;
            Assert.Equal(["id", "viewType", "gestureBlockingPolicy", "params"], args.Keys.Cast<string>());
            Assert.Equal("waitUntilTouchesEnded", args["gestureBlockingPolicy"]);
        }
    }

    [Fact]
    public void ToStringWorksAsIntended()
    {
        var androidPointerProperties = new AndroidPointerProperties(id: 0, toolType: 0);
        Assert.Equal("AndroidPointerProperties(id: 0, toolType: 0)", androidPointerProperties.ToString());

        var androidPointerCoords = new AndroidPointerCoords(
            orientation: 0.0,
            pressure: 0.0,
            size: 0.0,
            toolMajor: 0.0,
            toolMinor: 0.0,
            touchMajor: 0.0,
            touchMinor: 0.0,
            x: 0.0,
            y: 0.0);
        Assert.Equal(
            "AndroidPointerCoords(orientation: 0.0, pressure: 0.0, size: 0.0, toolMajor: 0.0, toolMinor: 0.0, "
            + "touchMajor: 0.0, touchMinor: 0.0, x: 0.0, y: 0.0)",
            androidPointerCoords.ToString());

        var androidMotionEvent = new AndroidMotionEvent(
            downTime: 0,
            eventTime: 0,
            action: 0,
            pointerCount: 0,
            pointerProperties: [],
            pointerCoords: [],
            metaState: 0,
            buttonState: 0,
            xPrecision: 0.0,
            yPrecision: 0.0,
            deviceId: 0,
            edgeFlags: 0,
            source: 0,
            flags: 0,
            motionEventId: 0);

        Assert.Equal(
            "AndroidPointerEvent(downTime: 0, eventTime: 0, action: 0, pointerCount: 0, pointerProperties: [], "
            + "pointerCoords: [], metaState: 0, buttonState: 0, xPrecision: 0.0, yPrecision: 0.0, deviceId: 0, "
            + "edgeFlags: 0, source: 0, flags: 0, motionEventId: 0)",
            androidMotionEvent.ToString());
    }

    [Fact]
    public void PlatformViewsRegistry_HandsOutIncreasingIds()
    {
        int first = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        Assert.Equal(first + 1, PlatformViewsRegistry.Instance.GetNextPlatformViewId());
    }

    [Fact]
    public void PointerAction_MasksThePointerIndexIntoTheSecondByte()
    {
        Assert.Equal(0x0105, AndroidViewController.PointerAction(1, AndroidViewController.ActionPointerDown));
        Assert.Equal(0x0006, AndroidViewController.PointerAction(256, AndroidViewController.ActionPointerUp));
    }
}
