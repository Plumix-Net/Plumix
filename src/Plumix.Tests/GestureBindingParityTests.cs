using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/gesture_binding_test.dart

namespace Plumix.Tests;

public sealed class GestureBindingParityTests
{
    [Fact]
    public void PointerRouterRunsAfterHitTestTargets()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var calls = new List<string>();
        var inner = new RenderPointerListener(
            onPointerDown: _ => calls.Add("inner"),
            behavior: HitTestBehavior.Opaque);
        var outer = new RenderPointerListener(
            onPointerDown: _ => calls.Add("outer"),
            behavior: HitTestBehavior.Translucent,
            child: inner);
        RenderView root = BuildRoot(outer);
        PointerRoute route = _ => calls.Add("router");
        binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            binding.HandlePointerEvent(root, Down());
            Assert.Equal(["inner", "outer", "router"], calls);
        }
        finally
        {
            binding.PointerRouter.RemoveGlobalRoute(route);
            binding.ResetForTests();
        }
    }

    [Fact]
    public void CancelPointerPrecedesAnUpQueuedDuringDownDispatch()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var calls = new List<string>();
        RenderView? root = null;
        var listener = new RenderPointerListener(
            onPointerDown: _ =>
            {
                calls.Add("down");
                binding.HandlePointerEvent(root!, Up());
                binding.CancelPointer(7);
            },
            onPointerCancel: _ => calls.Add("cancel"),
            onPointerUp: _ => calls.Add("up"),
            behavior: HitTestBehavior.Opaque);
        root = BuildRoot(listener);

        try
        {
            binding.HandlePointerEvent(root, Down());
            Assert.Equal(["down", "cancel"], calls);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void HandlerErrorIsReportedAndLaterTargetsStillReceiveEvent()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var calls = new List<string>();
        FlutterErrorDetails? reported = null;
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => reported = details;
        var inner = new RenderPointerListener(
            onPointerDown: _ => throw new InvalidOperationException("probe"),
            behavior: HitTestBehavior.Opaque);
        var outer = new RenderPointerListener(
            onPointerDown: _ => calls.Add("outer"),
            behavior: HitTestBehavior.Translucent,
            child: inner);
        PointerRoute route = _ => calls.Add("router");
        binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            binding.HandlePointerEvent(BuildRoot(outer), Down());
            Assert.Equal(["outer", "router"], calls);
            var details = Assert.IsType<FlutterErrorDetailsForPointerEventDispatcher>(reported);
            Assert.IsType<PointerDownEvent>(details.Event);
            Assert.Same(inner, details.HitTestEntry?.Target);
        }
        finally
        {
            FlutterError.OnError = previous;
            binding.PointerRouter.RemoveGlobalRoute(route);
            binding.ResetForTests();
        }
    }

    [Fact]
    public void AddedAndRemovedEventsRouteWithoutHitTestCallbacks()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var calls = new List<string>();
        var listener = new RenderPointerListener(
            onPointerHover: _ => calls.Add("hover"),
            behavior: HitTestBehavior.Opaque);
        RenderView root = BuildRoot(listener);
        PointerRoute route = @event => calls.Add(@event.GetType().Name);
        binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            binding.HandlePointerEvent(root, new PointerAddedEvent(7, PointerDeviceKind.Touch, default));
            binding.HandlePointerEvent(root, new PointerRemovedEvent(7, PointerDeviceKind.Touch, default));
            Assert.Equal(["PointerAddedEvent", "PointerRemovedEvent"], calls);
        }
        finally
        {
            binding.PointerRouter.RemoveGlobalRoute(route);
            binding.ResetForTests();
        }
    }

    [Fact]
    public void PlatformHitTestReportsNativeTargetsAnywhereInThePath()
    {
        _ = GestureBinding.Instance;
        var native = new NativeHitTestBox();
        var listener = new RenderPointerListener(behavior: HitTestBehavior.Opaque, child: native);
        var root = new RenderView(new FlutterView(new Size(800, 600), viewId: 7007))
        {
            Child = listener,
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(200, 200));
        RendererBinding.Instance.AddRenderView(root);

        try
        {
            Func<HitTestRequest, HitTestResponse> query = Assert.IsType<
                Func<HitTestRequest, HitTestResponse>>(PlatformDispatcher.Instance.OnHitTest);
            Assert.True(query(new HitTestRequest(7007, new Point(10, 10))).HasPlatformView);
            Assert.False(query(new HitTestRequest(7008, new Point(10, 10))).HasPlatformView);
        }
        finally
        {
            RendererBinding.Instance.RemoveRenderView(root);
            pipeline.RootNode = null;
        }
    }

    [Fact]
    public void DisablingResamplingFlushesQueuedTouchEventsBeforeTheNextEvent()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        var calls = new List<string>();
        var listener = new RenderPointerListener(
            onPointerDown: _ => calls.Add("down"),
            onPointerUp: _ => calls.Add("up"),
            behavior: HitTestBehavior.Opaque);
        RenderView root = BuildRoot(listener);
        DateTime future = DateTime.UtcNow.AddSeconds(5);

        try
        {
            binding.ResamplingEnabled = true;
            binding.HandlePointerEvent(root, new PointerDownEvent(
                7, PointerDeviceKind.Touch, new Point(10, 10), PointerButtons.Primary, future));
            Assert.Empty(calls);

            binding.ResamplingEnabled = false;
            binding.HandlePointerEvent(root, new PointerUpEvent(
                7, PointerDeviceKind.Touch, new Point(10, 10), PointerButtons.None,
                future.AddMilliseconds(10)));
            Assert.Equal(["down", "up"], calls);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static RenderView BuildRoot(RenderBox child)
    {
        var root = new RenderView(new FlutterView(new Size(800, 600))) { Child = child };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(200, 200));
        return root;
    }

    private static PointerDownEvent Down() =>
        new(7, PointerDeviceKind.Touch, new Point(10, 10), PointerButtons.Primary, DateTime.UnixEpoch);

    private static PointerUpEvent Up() =>
        new(7, PointerDeviceKind.Touch, new Point(10, 10), PointerButtons.None, DateTime.UnixEpoch);

    private sealed class NativeHitTestBox : RenderBox, INativeHitTestTarget
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(80, 80));
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
