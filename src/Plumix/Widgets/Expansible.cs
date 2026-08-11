using Plumix.Foundation;
using Plumix.Rendering;

#pragma warning disable CS0618

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/expansible.dart
public class ExpansibleController : ChangeNotifier
{
    private bool _isExpanded;

    public bool IsExpanded => _isExpanded;

    public void Expand() => SetExpansionState(true);

    public void Collapse() => SetExpansionState(false);

    public void Toggle()
    {
        if (IsExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
    }

    public static ExpansibleController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "ExpansibleController.Of() was called with a context that does not contain an Expansible.");
    }

    public static ExpansibleController? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<Expansible.ExpansibleState>()?.Controller;
    }

    private void SetExpansionState(bool value)
    {
        if (_isExpanded == value)
        {
            return;
        }

        _isExpanded = value;
        NotifyListeners();
    }
}

[Obsolete("Use ExpansibleController instead.")]
public sealed class ExpansionTileController : ExpansibleController;

public delegate Widget ExpansibleComponentBuilder(BuildContext context, Animation<double> animation);

public delegate Widget ExpansibleBuilder(
    BuildContext context,
    Widget header,
    Widget body,
    Animation<double> animation);

public sealed class Expansible : StatefulWidget
{
    public Expansible(
        ExpansibleController controller,
        ExpansibleComponentBuilder headerBuilder,
        ExpansibleComponentBuilder bodyBuilder,
        ExpansibleBuilder? expansibleBuilder = null,
        AnimationStyle? animationStyle = null,
        TimeSpan? duration = null,
        Curve? curve = null,
        Curve? reverseCurve = null,
        bool maintainState = true,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        HeaderBuilder = headerBuilder ?? throw new ArgumentNullException(nameof(headerBuilder));
        BodyBuilder = bodyBuilder ?? throw new ArgumentNullException(nameof(bodyBuilder));
        ExpansibleBuilder = expansibleBuilder ?? DefaultExpansibleBuilder;
        AnimationStyle = animationStyle;
        Duration = duration ?? TimeSpan.FromMilliseconds(200);
        Curve = curve ?? Curves.Ease;
        ReverseCurve = reverseCurve;
        MaintainState = maintainState;
    }

    public ExpansibleController Controller { get; }

    public ExpansibleComponentBuilder HeaderBuilder { get; }

    public ExpansibleComponentBuilder BodyBuilder { get; }

    public ExpansibleBuilder ExpansibleBuilder { get; }

    public AnimationStyle? AnimationStyle { get; }

    [Obsolete("Use AnimationStyle instead.")]
    public TimeSpan Duration { get; }

    [Obsolete("Use AnimationStyle instead.")]
    public Curve Curve { get; }

    [Obsolete("Use AnimationStyle instead.")]
    public Curve? ReverseCurve { get; }

    public bool MaintainState { get; }

    public override State CreateState() => new ExpansibleState();

    private static Widget DefaultExpansibleBuilder(
        BuildContext context,
        Widget header,
        Widget body,
        Animation<double> animation)
    {
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            children: [header, body]);
    }

    public sealed class ExpansibleState : State
    {
        private AnimationController? _animation;

        private Expansible CurrentWidget => (Expansible)StateWidget;

        internal ExpansibleController Controller => CurrentWidget.Controller;

        private TimeSpan EffectiveDuration => CurrentWidget.AnimationStyle?.Duration ?? CurrentWidget.Duration;

        private Curve EffectiveCurve => CurrentWidget.AnimationStyle?.Curve ?? CurrentWidget.Curve;

        private Curve? EffectiveReverseCurve =>
            CurrentWidget.AnimationStyle?.ReverseCurve ?? CurrentWidget.ReverseCurve;

        public override void InitState()
        {
            bool initiallyExpanded = PageStorage.MaybeOf(Context)?.ReadState(Context) as bool?
                                     ?? CurrentWidget.Controller.IsExpanded;
            CreateAnimation(initiallyExpanded ? 1.0 : 0.0);
            if (initiallyExpanded)
            {
                CurrentWidget.Controller.Expand();
            }
            else
            {
                CurrentWidget.Controller.Collapse();
            }

            CurrentWidget.Controller.AddListener(HandleControllerChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldExpansible = (Expansible)oldWidget;
            TimeSpan oldDuration = oldExpansible.AnimationStyle?.Duration ?? oldExpansible.Duration;
            Curve oldCurve = oldExpansible.AnimationStyle?.Curve ?? oldExpansible.Curve;
            Curve? oldReverseCurve = oldExpansible.AnimationStyle?.ReverseCurve ?? oldExpansible.ReverseCurve;

            if (!ReferenceEquals(oldExpansible.Controller, CurrentWidget.Controller))
            {
                oldExpansible.Controller.RemoveListener(HandleControllerChanged);
                CurrentWidget.Controller.AddListener(HandleControllerChanged);
                if (oldExpansible.Controller.IsExpanded != CurrentWidget.Controller.IsExpanded)
                {
                    HandleControllerChanged();
                }
            }

            if (oldDuration != EffectiveDuration)
            {
                _animation!.Duration = NormalizeDuration(EffectiveDuration);
            }

            if (!Equals(oldCurve, EffectiveCurve)
                || !Equals(oldReverseCurve, EffectiveReverseCurve))
            {
                UpdateAnimationCurve();
            }
        }

        public override void Dispose()
        {
            CurrentWidget.Controller.RemoveListener(HandleControllerChanged);
            DisposeAnimation();
        }

        public override Widget Build(BuildContext context)
        {
            var animation = _animation!;
            bool closed = !CurrentWidget.Controller.IsExpanded && animation.Value <= 0.0001;
            bool shouldRemoveBody = closed && !CurrentWidget.MaintainState;

            Widget? retainedBody = shouldRemoveBody
                ? null
                : new Offstage(
                    offstage: closed,
                    child: new TickerMode(
                        enabled: !closed,
                        child: CurrentWidget.BodyBuilder(context, animation)));
            Widget body = new ClipRect(
                child: new Align(
                    alignment: Alignment.TopCenter,
                    heightFactor: animation.Evaluate(),
                    child: retainedBody));
            Widget header = CurrentWidget.HeaderBuilder(context, animation);
            return CurrentWidget.ExpansibleBuilder(context, header, body, animation);
        }

        private void CreateAnimation(double initialValue)
        {
            _animation = new AnimationController(NormalizeDuration(EffectiveDuration), this);
            UpdateAnimationCurve();
            _animation.Changed += HandleAnimationChanged;
            _animation.Dismissed += HandleAnimationSettled;
            _animation.Completed += HandleAnimationSettled;
            _animation.SetValue(initialValue);
        }

        private void DisposeAnimation()
        {
            if (_animation is null)
            {
                return;
            }

            _animation.Changed -= HandleAnimationChanged;
            _animation.Dismissed -= HandleAnimationSettled;
            _animation.Completed -= HandleAnimationSettled;
            _animation.Dispose();
            _animation = null;
        }

        private void HandleControllerChanged()
        {
            SetState(() =>
            {
                UpdateAnimationCurve();
                if (EffectiveDuration <= TimeSpan.Zero)
                {
                    _animation!.Stop();
                    _animation.SetValue(CurrentWidget.Controller.IsExpanded ? 1.0 : 0.0);
                }
                else if (CurrentWidget.Controller.IsExpanded)
                {
                    _animation!.Forward();
                }
                else
                {
                    _animation!.Reverse();
                }

                PageStorage.MaybeOf(Context)?.WriteState(Context, CurrentWidget.Controller.IsExpanded);
            });
        }

        private void UpdateAnimationCurve()
        {
            _animation!.Curve = CurrentWidget.Controller.IsExpanded
                ? EffectiveCurve
                : EffectiveReverseCurve ?? EffectiveCurve;
        }

        private static TimeSpan NormalizeDuration(TimeSpan duration)
        {
            return duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : duration;
        }

        private void HandleAnimationChanged()
        {
            SetState(() => { });
        }

        private void HandleAnimationSettled()
        {
            SetState(() => { });
        }
    }
}

#pragma warning restore CS0618
