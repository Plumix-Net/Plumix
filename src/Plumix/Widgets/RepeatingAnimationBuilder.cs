using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/repeating_animation_builder.dart

public enum RepeatMode
{
    Restart,
    Reverse,
}

public sealed class RepeatingAnimationBuilder<T> : StatefulWidget
    where T : notnull
{
    public RepeatingAnimationBuilder(
        Animatable<T> animatable,
        TimeSpan duration,
        ValueWidgetBuilder<T> builder,
        Curve? curve = null,
        RepeatMode repeatMode = RepeatMode.Restart,
        bool paused = false,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Animatable = animatable ?? throw new ArgumentNullException(nameof(animatable));
        Duration = duration;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Curve = curve ?? Curves.Linear;
        RepeatMode = repeatMode;
        Paused = paused;
        Child = child;
    }

    public Animatable<T> Animatable { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public ValueWidgetBuilder<T> Builder { get; }

    public Widget? Child { get; }

    public RepeatMode RepeatMode { get; }

    public bool Paused { get; }

    public override State CreateState() => new RepeatingAnimationBuilderState();

    private sealed class RepeatingAnimationBuilderState : State
    {
        private AnimationController? _controller;
        private CurvedAnimation? _curvedAnimation;

        private RepeatingAnimationBuilder<T> CurrentWidget => (RepeatingAnimationBuilder<T>)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this);
            _curvedAnimation = new CurvedAnimation(
                parent: _controller,
                curve: CurrentWidget.Curve);

            if (!CurrentWidget.Paused)
            {
                _controller.Repeat(reverse: CurrentWidget.RepeatMode == RepeatMode.Reverse);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBuilder = (RepeatingAnimationBuilder<T>)oldWidget;
            AnimationController controller = _controller!;
            if (CurrentWidget.Duration != oldBuilder.Duration)
            {
                controller.Duration = CurrentWidget.Duration;
            }
            if (!ReferenceEquals(CurrentWidget.Curve, oldBuilder.Curve))
            {
                _curvedAnimation!.Curve = CurrentWidget.Curve;
            }

            if (CurrentWidget.Paused)
            {
                if (!oldBuilder.Paused || controller.IsAnimating)
                {
                    controller.Stop();
                }
                return;
            }

            bool shouldRestart = oldBuilder.Paused
                || CurrentWidget.RepeatMode != oldBuilder.RepeatMode
                || CurrentWidget.Duration != oldBuilder.Duration
                || !controller.IsAnimating;
            if (shouldRestart)
            {
                controller.Repeat(reverse: CurrentWidget.RepeatMode == RepeatMode.Reverse);
            }
        }

        public override Widget Build(BuildContext context)
        {
            CurvedAnimation curvedAnimation = _curvedAnimation!;
            return new AnimatedBuilder(
                animation: curvedAnimation,
                child: CurrentWidget.Child,
                builder: (builderContext, child) =>
                {
                    T value = CurrentWidget.Animatable.Transform(curvedAnimation.Value);
                    return CurrentWidget.Builder(builderContext, value, child);
                });
        }

        public override void Dispose()
        {
            _curvedAnimation!.Dispose();
            _controller!.Dispose();
            _curvedAnimation = null;
            _controller = null;
        }
    }
}
