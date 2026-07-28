using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/status_transitions.dart

public abstract class StatusTransitionWidget : StatefulWidget
{
    protected StatusTransitionWidget(Animation<double> animation, Key? key = null) : base(key)
    {
        Animation = animation ?? throw new ArgumentNullException(nameof(animation));
    }

    public Animation<double> Animation { get; }

    public abstract Widget Build(BuildContext context);

    public override State CreateState()
    {
        return new StatusTransitionState();
    }

    private sealed class StatusTransitionState : State
    {
        private StatusTransitionWidget CurrentWidget => (StatusTransitionWidget)StateWidget;

        public override void InitState()
        {
            base.InitState();
            CurrentWidget.Animation.AddStatusListener(HandleAnimationStatusChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldTransition = (StatusTransitionWidget)oldWidget;
            if (ReferenceEquals(oldTransition.Animation, CurrentWidget.Animation))
            {
                return;
            }

            oldTransition.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
            CurrentWidget.Animation.AddStatusListener(HandleAnimationStatusChanged);
        }

        public override void Dispose()
        {
            CurrentWidget.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Build(context);
        }

        private void HandleAnimationStatusChanged(AnimationStatus status)
        {
            SetState(() =>
            {
                // The animation status is the build state and has already changed.
            });
        }
    }
}
