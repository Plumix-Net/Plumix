using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/animated_size.dart

/// <summary>Animates its own size whenever its child's laid-out size changes.</summary>
public sealed class AnimatedSize : StatefulWidget
{
    public AnimatedSize(
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        Curve? curve = null,
        TimeSpan? reverseDuration = null,
        Clip clipBehavior = Clip.HardEdge,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        if (reverseDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(reverseDuration));
        }

        Duration = duration;
        Child = child;
        Alignment = alignment == default ? Alignment.Center : alignment;
        Curve = curve ?? Curves.Linear;
        ReverseDuration = reverseDuration;
        ClipBehavior = clipBehavior;
        OnEnd = onEnd;
    }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Alignment Alignment { get; }

    public Curve Curve { get; }

    public TimeSpan? ReverseDuration { get; }

    public Clip ClipBehavior { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedSizeState();

    private sealed class AnimatedSizeState : State
    {
        private AnimationController? _controller;

        private AnimatedSize CurrentWidget => (AnimatedSize)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(CurrentWidget.Duration)
            {
                Curve = CurrentWidget.Curve,
            };
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
        }

        public override Widget Build(BuildContext context)
        {
            return new AnimatedSizeRenderObjectWidget(
                controller: _controller!,
                duration: CurrentWidget.Duration,
                reverseDuration: CurrentWidget.ReverseDuration,
                alignment: CurrentWidget.Alignment,
                clipBehavior: CurrentWidget.ClipBehavior,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private void HandleCompleted() => CurrentWidget.OnEnd?.Invoke();
    }
}

internal sealed class AnimatedSizeRenderObjectWidget : SingleChildRenderObjectWidget
{
    public AnimatedSizeRenderObjectWidget(
        AnimationController controller,
        TimeSpan duration,
        TimeSpan? reverseDuration,
        Alignment alignment,
        Clip clipBehavior,
        Widget? child,
        Key? key = null) : base(child, key)
    {
        Controller = controller;
        Duration = duration;
        ReverseDuration = reverseDuration;
        Alignment = alignment;
        ClipBehavior = clipBehavior;
    }

    public AnimationController Controller { get; }

    public TimeSpan Duration { get; }

    public TimeSpan? ReverseDuration { get; }

    public Alignment Alignment { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAnimatedSize(
            controller: Controller,
            duration: Duration,
            reverseDuration: ReverseDuration,
            alignment: Alignment,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var animatedSize = (RenderAnimatedSize)renderObject;
        animatedSize.Controller = Controller;
        animatedSize.Duration = Duration;
        animatedSize.ReverseDuration = ReverseDuration;
        animatedSize.Alignment = Alignment;
        animatedSize.ClipBehavior = ClipBehavior;
    }
}
