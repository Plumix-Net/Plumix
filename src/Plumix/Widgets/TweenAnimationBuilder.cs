using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/tween_animation_builder.dart

public sealed class TweenAnimationBuilder<T> : StatefulWidget
{
    public TweenAnimationBuilder(
        Tween<T> tween,
        TimeSpan duration,
        ValueWidgetBuilder<T> builder,
        Curve? curve = null,
        Action? onEnd = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Tween = tween ?? throw new ArgumentNullException(nameof(tween));
        if (!Tween.HasEndValue)
        {
            throw new ArgumentException(
                "Tween provided to TweenAnimationBuilder must have a non-null end value.",
                nameof(tween));
        }

        Duration = duration;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
        Child = child;
    }

    public Tween<T> Tween { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public ValueWidgetBuilder<T> Builder { get; }

    public Action? OnEnd { get; }

    public Widget? Child { get; }

    public override State CreateState() => new TweenAnimationBuilderState();

    private sealed class TweenAnimationBuilderState : State
    {
        private AnimationController? _controller;
        private Tween<T>? _currentTween;

        private TweenAnimationBuilder<T> CurrentWidget => (TweenAnimationBuilder<T>)StateWidget;

        public override void InitState()
        {
            _currentTween = CurrentWidget.Tween;
            if (!_currentTween.HasBeginValue)
            {
                _currentTween.SetBeginValue(_currentTween.GetEndValue());
            }

            _controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
            {
                Curve = CurrentWidget.Curve,
            };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;

            if (!EqualityComparer<T>.Default.Equals(
                    _currentTween.GetBeginValue(),
                    _currentTween.GetEndValue()))
            {
                _controller.Forward();
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;

            T target = CurrentWidget.Tween.GetEndValue();
            if (EqualityComparer<T>.Default.Equals(target, _currentTween!.GetEndValue()))
            {
                return;
            }

            T current = _currentTween.Evaluate(_controller.Evaluate());
            _currentTween.SetBeginValue(current);
            _currentTween.SetEndValue(target);
            _controller.Forward(from: 0.0);
        }

        public override Widget Build(BuildContext context)
        {
            T value = _currentTween!.Evaluate(_controller!.Evaluate());
            return CurrentWidget.Builder(context, value, CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
            _currentTween = null;
        }

        private void HandleChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private void HandleCompleted()
        {
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}
