using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/expansible.dart
public class ExpansibleController : ChangeNotifier
{
    private bool _isExpanded;

    public bool IsExpanded => _isExpanded;

    public void Expand() => SetExpansionState(true);

    public void Collapse() => SetExpansionState(false);

    public void Toggle() => SetExpansionState(!_isExpanded);

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

public delegate Widget ExpansibleComponentBuilder(BuildContext context, AnimationController animation);

public delegate Widget ExpansibleBuilder(
    BuildContext context,
    Widget header,
    Widget body,
    AnimationController animation);

public sealed class Expansible : StatefulWidget
{
    public Expansible(
        ExpansibleController controller,
        ExpansibleComponentBuilder headerBuilder,
        ExpansibleComponentBuilder bodyBuilder,
        ExpansibleBuilder? expansibleBuilder = null,
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
        Duration = duration ?? TimeSpan.FromMilliseconds(200);
        Curve = curve ?? Curves.EaseInOut;
        ReverseCurve = reverseCurve;
        MaintainState = maintainState;
    }

    public ExpansibleController Controller { get; }

    public ExpansibleComponentBuilder HeaderBuilder { get; }

    public ExpansibleComponentBuilder BodyBuilder { get; }

    public ExpansibleBuilder ExpansibleBuilder { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Curve? ReverseCurve { get; }

    public bool MaintainState { get; }

    public override State CreateState() => new ExpansibleState();

    private static Widget DefaultExpansibleBuilder(
        BuildContext context,
        Widget header,
        Widget body,
        AnimationController animation)
    {
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            children: [header, body]);
    }

    private sealed class ExpansibleState : State
    {
        private AnimationController? _animation;

        private Expansible CurrentWidget => (Expansible)StateWidget;

        public override void InitState()
        {
            CreateAnimation(initialValue: CurrentWidget.Controller.IsExpanded ? 1.0 : 0.0);
            CurrentWidget.Controller.AddListener(HandleControllerChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldExpansible = (Expansible)oldWidget;
            if (!ReferenceEquals(oldExpansible.Controller, CurrentWidget.Controller))
            {
                oldExpansible.Controller.RemoveListener(HandleControllerChanged);
                CurrentWidget.Controller.AddListener(HandleControllerChanged);
                if (oldExpansible.Controller.IsExpanded != CurrentWidget.Controller.IsExpanded)
                {
                    HandleControllerChanged();
                }
            }

            if (oldExpansible.Duration != CurrentWidget.Duration)
            {
                double value = _animation?.Value ?? 0;
                bool wasAnimating = _animation?.IsAnimating == true;
                DisposeAnimation();
                CreateAnimation(value);
                if (wasAnimating)
                {
                    if (CurrentWidget.Controller.IsExpanded)
                    {
                        _animation!.Forward();
                    }
                    else
                    {
                        _animation!.Reverse();
                    }
                }
            }
            else if (!Equals(oldExpansible.Curve, CurrentWidget.Curve)
                     || !Equals(oldExpansible.ReverseCurve, CurrentWidget.ReverseCurve))
            {
                _animation!.Curve = CurrentWidget.Controller.IsExpanded
                    ? CurrentWidget.Curve
                    : CurrentWidget.ReverseCurve ?? CurrentWidget.Curve;
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

            Widget body = shouldRemoveBody
                ? new SizedBox()
                : new Offstage(
                    offstage: closed,
                    child: CurrentWidget.BodyBuilder(context, animation));
            body = new ClipRect(
                child: new Align(
                    alignment: Alignment.TopCenter,
                    heightFactor: animation.Evaluate(),
                    child: body));

            var header = CurrentWidget.HeaderBuilder(context, animation);
            return CurrentWidget.ExpansibleBuilder(context, header, body, animation);
        }

        private void CreateAnimation(double initialValue)
        {
            _animation = new AnimationController(CurrentWidget.Duration, this)
            {
                Curve = CurrentWidget.Controller.IsExpanded
                    ? CurrentWidget.Curve
                    : CurrentWidget.ReverseCurve ?? CurrentWidget.Curve
            };
            _animation.Changed += HandleAnimationChanged;
            _animation.Dismissed += HandleAnimationSettled;
            _animation.Completed += HandleAnimationSettled;
            if (initialValue >= 1)
            {
                _animation.Forward(from: 1);
                _animation.Stop();
            }
            else if (initialValue > 0)
            {
                _animation.Forward(from: initialValue);
                _animation.Stop();
            }
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
            var animation = _animation!;
            SetState(() =>
            {
                animation.Curve = CurrentWidget.Controller.IsExpanded
                    ? CurrentWidget.Curve
                    : CurrentWidget.ReverseCurve ?? CurrentWidget.Curve;
                if (CurrentWidget.Controller.IsExpanded)
                {
                    animation.Forward();
                }
                else
                {
                    animation.Reverse();
                }
            });
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
