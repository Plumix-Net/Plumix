using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/pointer_signal_resolver_test.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class PointerSignalResolverTests
{
    private static PointerScrollEvent Scroll(Point position, Action<bool>? onRespond = null)
    {
        return new PointerScrollEvent(
            pointer: 1,
            kind: PointerDeviceKind.Mouse,
            position: position,
            buttons: PointerButtons.None,
            scrollDelta: new Point(0.0, 10.0),
            timestampUtc: DateTime.UnixEpoch,
            onRespond: onRespond);
    }

    [Fact]
    public void OnlyTheFirstRegisteredCallbackRuns()
    {
        var resolver = new PointerSignalResolver();
        var log = new List<string>();
        var scroll = Scroll(new Point(10.0, 10.0));

        resolver.Register(scroll, _ => log.Add("first"));
        resolver.Register(scroll, _ => log.Add("second"));
        Assert.Empty(log);

        resolver.Resolve(scroll);
        Assert.Equal(["first"], log);

        // The resolver is reusable for the next event.
        var next = Scroll(new Point(20.0, 20.0));
        resolver.Register(next, _ => log.Add("next"));
        resolver.Resolve(next);
        Assert.Equal(["first", "next"], log);
    }

    [Fact]
    public void RegistrationAcceptsTransformedCopiesOfTheSameEvent()
    {
        var resolver = new PointerSignalResolver();
        var log = new List<string>();
        var scroll = Scroll(new Point(10.0, 10.0));
        var transformed = (PointerScrollEvent)scroll.WithLocalCoordinates(new Point(1.0, 1.0), default);

        resolver.Register(transformed, _ => log.Add("inner"));
        resolver.Register(scroll, _ => log.Add("outer"));
        resolver.Resolve(scroll);
        Assert.Equal(["inner"], log);
    }

    [Fact]
    public void ResolveWithNoRegistrations_AllowsThePlatformDefault()
    {
        var resolver = new PointerSignalResolver();
        bool? allowPlatformDefault = null;
        var scroll = Scroll(new Point(10.0, 10.0), allow => allowPlatformDefault = allow);

        resolver.Resolve(scroll);
        Assert.True(allowPlatformDefault);
    }

    [Fact]
    public void Binding_DispatchesSignalToTheDeepestRegisteredListenerOnly()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var log = new List<string>();

        try
        {
            var inner = new RenderPointerListener(
                onPointerSignal: signal =>
                    binding.PointerSignalResolver.Register(signal, _ => log.Add("inner")),
                behavior: HitTestBehavior.Opaque,
                child: new SignalHitTestBox(new Size(80.0, 80.0)));
            var outer = new RenderPointerListener(
                onPointerSignal: signal =>
                    binding.PointerSignalResolver.Register(signal, _ => log.Add("outer")),
                behavior: HitTestBehavior.Translucent,
                child: inner);
            var root = new RenderView { Child = outer };
            var pipeline = new PipelineOwner(root);
            pipeline.Attach(root);
            pipeline.FlushLayout(new Size(200.0, 200.0));

            binding.HandlePointerEvent(root, Scroll(new Point(10.0, 10.0)));
            Assert.Equal(["inner"], log);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private sealed class SignalHitTestBox(Size size) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return true;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
