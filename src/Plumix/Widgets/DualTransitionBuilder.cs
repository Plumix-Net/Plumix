using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/dual_transition_builder.dart

public delegate Widget AnimatedTransitionBuilder(
    BuildContext context,
    Animation<double> animation,
    Widget? child);

public sealed class DualTransitionBuilder : StatefulWidget
{
    public DualTransitionBuilder(
        Animation<double> animation,
        AnimatedTransitionBuilder forwardBuilder,
        AnimatedTransitionBuilder reverseBuilder,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Animation = animation ?? throw new ArgumentNullException(nameof(animation));
        ForwardBuilder = forwardBuilder ?? throw new ArgumentNullException(nameof(forwardBuilder));
        ReverseBuilder = reverseBuilder ?? throw new ArgumentNullException(nameof(reverseBuilder));
        Child = child;
    }

    public Animation<double> Animation { get; }

    public AnimatedTransitionBuilder ForwardBuilder { get; }

    public AnimatedTransitionBuilder ReverseBuilder { get; }

    public Widget? Child { get; }

    public override State CreateState() => new DualTransitionBuilderState();

    private sealed class DualTransitionBuilderState : State
    {
        private static readonly Animation<double> AlwaysCompleteAnimation = new ConstantAnimation(
            value: 1.0,
            status: AnimationStatus.Completed);
        private static readonly Animation<double> AlwaysDismissedAnimation = new ConstantAnimation(
            value: 0.0,
            status: AnimationStatus.Dismissed);

        private readonly ProxyAnimation _forwardAnimation = new();
        private readonly ProxyAnimation _reverseAnimation = new();
        private AnimationStatus _effectiveAnimationStatus;

        private DualTransitionBuilder CurrentWidget => (DualTransitionBuilder)StateWidget;

        public override void InitState()
        {
            _effectiveAnimationStatus = CurrentWidget.Animation.Status;
            CurrentWidget.Animation.AddStatusListener(HandleAnimationStatusChanged);
            UpdateAnimations();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBuilder = (DualTransitionBuilder)oldWidget;
            if (ReferenceEquals(oldBuilder.Animation, CurrentWidget.Animation))
            {
                return;
            }

            oldBuilder.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
            CurrentWidget.Animation.AddStatusListener(HandleAnimationStatusChanged);
            HandleAnimationStatusChanged(CurrentWidget.Animation.Status);
        }

        public override Widget Build(BuildContext context)
        {
            Widget reverseTransition = CurrentWidget.ReverseBuilder(
                context,
                _reverseAnimation,
                CurrentWidget.Child);
            return CurrentWidget.ForwardBuilder(context, _forwardAnimation, reverseTransition);
        }

        public override void Dispose()
        {
            CurrentWidget.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
            _forwardAnimation.Parent = null;
            _reverseAnimation.Parent = null;
        }

        private void HandleAnimationStatusChanged(AnimationStatus animationStatus)
        {
            AnimationStatus previousStatus = _effectiveAnimationStatus;
            _effectiveAnimationStatus = CalculateEffectiveAnimationStatus(
                lastEffective: _effectiveAnimationStatus,
                current: animationStatus);
            if (previousStatus != _effectiveAnimationStatus)
            {
                UpdateAnimations();
            }
        }

        private static AnimationStatus CalculateEffectiveAnimationStatus(
            AnimationStatus lastEffective,
            AnimationStatus current)
        {
            return current switch
            {
                AnimationStatus.Dismissed or AnimationStatus.Completed => current,
                AnimationStatus.Forward when lastEffective == AnimationStatus.Reverse => lastEffective,
                AnimationStatus.Forward => current,
                AnimationStatus.Reverse when lastEffective == AnimationStatus.Forward => lastEffective,
                AnimationStatus.Reverse => current,
                _ => throw new ArgumentOutOfRangeException(nameof(current)),
            };
        }

        private void UpdateAnimations()
        {
            switch (_effectiveAnimationStatus)
            {
                case AnimationStatus.Dismissed:
                case AnimationStatus.Forward:
                    _forwardAnimation.Parent = CurrentWidget.Animation;
                    _reverseAnimation.Parent = AlwaysDismissedAnimation;
                    break;
                case AnimationStatus.Reverse:
                case AnimationStatus.Completed:
                    _forwardAnimation.Parent = AlwaysCompleteAnimation;
                    _reverseAnimation.Parent = new ReverseAnimation(CurrentWidget.Animation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private sealed class ConstantAnimation : Animation<double>
        {
            public ConstantAnimation(double value, AnimationStatus status)
            {
                Value = value;
                Status = status;
            }

            public override double Value { get; }

            public override AnimationStatus Status { get; }

            public override void AddListener(Action listener)
            {
            }

            public override void RemoveListener(Action listener)
            {
            }

            public override void AddStatusListener(Action<AnimationStatus> listener)
            {
            }

            public override void RemoveStatusListener(Action<AnimationStatus> listener)
            {
            }
        }
    }
}
