using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/tween_animation_builder.dart

/// <summary>
/// Widget builder that animates a property of a <see cref="Widget"/> to a target value whenever the
/// target value changes.
/// </summary>
/// <remarks>
/// The <see cref="Tween"/> is mutated in place as the animation runs, as in Dart: its
/// <see cref="Tween{T}.Begin"/> is set to its <see cref="Tween{T}.End"/> when it has none, and on every
/// retarget it becomes the current value.
/// </remarks>
public class TweenAnimationBuilder<T> : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a <see cref="TweenAnimationBuilder{T}"/>.</summary>
    public TweenAnimationBuilder(
        Tween<T> tween,
        TimeSpan duration,
        ValueWidgetBuilder<T> builder,
        Curve? curve = null,
        Action? onEnd = null,
        Widget? child = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        Tween = tween;
        Builder = builder;
        Child = child;
    }

    /// <summary>Defines the target value for the animation.</summary>
    public Tween<T> Tween { get; }

    /// <summary>Called every time the animation value changes.</summary>
    public ValueWidgetBuilder<T> Builder { get; }

    /// <summary>The child widget to pass to the <see cref="Builder"/>.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new TweenAnimationBuilderState();

    private sealed class TweenAnimationBuilderState : AnimatedWidgetBaseState<TweenAnimationBuilder<T>>
    {
        private Tween<T>? _currentTween;

        public override void InitState()
        {
            _currentTween = Widget.Tween;
            if (!_currentTween.HasBeginValue && _currentTween.HasEndValue)
            {
                // Dart's `_currentTween!.begin ??= _currentTween!.end`.
                _currentTween.SetBeginValue(_currentTween.GetEndValue());
            }

            base.InitState();
            if (!Equals(_currentTween.Begin, _currentTween.End))
            {
                Controller.Forward();
            }
        }

        protected override void ForEachTween(TweenVisitor visitor)
        {
            if (Constants.KDebugMode && !Widget.Tween.HasEndValue)
            {
                throw new AssertionError(
                    "Tween provided to TweenAnimationBuilder must have non-null Tween.end value.");
            }

            _currentTween = visitor.Visit(
                _currentTween,
                Widget.Tween.End,
                _ =>
                {
                    DebugAssertions.Assert(false);
                    throw new InvalidOperationException(
                        "Constructor will never be called because null is never provided as current tween.");
                });
        }

        public override Widget Build(BuildContext context)
        {
            return Widget.Builder(context, _currentTween!.Evaluate(Animation), Widget.Child);
        }
    }
}
