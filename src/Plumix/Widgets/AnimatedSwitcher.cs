using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/animated_switcher.dart
// flutter/packages/flutter/lib/src/widgets/animated_cross_fade.dart

public delegate Widget AnimatedSwitcherTransitionBuilder(Widget child, Animation<double> animation);

public delegate Widget AnimatedSwitcherLayoutBuilder(
    Widget? currentChild,
    IReadOnlyList<Widget> previousChildren);

public sealed class AnimatedSwitcher : StatefulWidget
{
    public AnimatedSwitcher(
        TimeSpan duration,
        Widget? child = null,
        TimeSpan? reverseDuration = null,
        Curve? switchInCurve = null,
        Curve? switchOutCurve = null,
        AnimatedSwitcherTransitionBuilder? transitionBuilder = null,
        AnimatedSwitcherLayoutBuilder? layoutBuilder = null,
        Key? key = null) : base(key)
    {
        ValidateDuration(duration, nameof(duration));
        ValidateDuration(reverseDuration, nameof(reverseDuration));
        Duration = duration;
        Child = child;
        ReverseDuration = reverseDuration;
        SwitchInCurve = switchInCurve ?? Curves.Linear;
        SwitchOutCurve = switchOutCurve ?? Curves.Linear;
        TransitionBuilder = transitionBuilder ?? DefaultTransitionBuilder;
        LayoutBuilder = layoutBuilder ?? DefaultLayoutBuilder;
    }

    public Widget? Child { get; }

    public TimeSpan Duration { get; }

    public TimeSpan? ReverseDuration { get; }

    public Curve SwitchInCurve { get; }

    public Curve SwitchOutCurve { get; }

    public AnimatedSwitcherTransitionBuilder TransitionBuilder { get; }

    public AnimatedSwitcherLayoutBuilder LayoutBuilder { get; }

    public override State CreateState() => new AnimatedSwitcherState();

    public static Widget DefaultTransitionBuilder(Widget child, Animation<double> animation)
    {
        return new FadeTransition(
            key: new ValueKey<Key?>(child.Key),
            opacity: animation,
            child: child);
    }

    public static Widget DefaultLayoutBuilder(
        Widget? currentChild,
        IReadOnlyList<Widget> previousChildren)
    {
        var children = new List<Widget>(previousChildren.Count + (currentChild is null ? 0 : 1));
        children.AddRange(previousChildren);
        if (currentChild is not null)
        {
            children.Add(currentChild);
        }

        return new Stack(
            alignment: Alignment.Center,
            children: children);
    }

    private static void ValidateDuration(TimeSpan? duration, string parameterName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private sealed class ChildEntry : IDisposable
    {
        private readonly Action<AnimationStatus> _statusListener;

        public ChildEntry(
            AnimationController controller,
            CurvedAnimation animation,
            Widget transition,
            Widget widgetChild,
            Action<ChildEntry> onDismissed)
        {
            Controller = controller;
            Animation = animation;
            Transition = transition;
            WidgetChild = widgetChild;
            _statusListener = status =>
            {
                if (status == AnimationStatus.Dismissed)
                {
                    onDismissed(this);
                }
            };
            Animation.AddStatusListener(_statusListener);
        }

        public AnimationController Controller { get; }

        public CurvedAnimation Animation { get; }

        public Widget Transition { get; set; }

        public Widget WidgetChild { get; set; }

        public void Dispose()
        {
            Animation.RemoveStatusListener(_statusListener);
            Animation.Dispose();
            Controller.Dispose();
        }
    }

    private sealed class AnimatedSwitcherState : State
    {
        private ChildEntry? _currentEntry;
        private readonly List<ChildEntry> _outgoingEntries = [];
        private IReadOnlyList<Widget>? _outgoingWidgets = Array.Empty<Widget>();
        private int _childNumber;

        private AnimatedSwitcher CurrentWidget => (AnimatedSwitcher)StateWidget;

        public override void InitState()
        {
            AddEntryForNewChild(animate: false);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldSwitcher = (AnimatedSwitcher)oldWidget;
            if (CurrentWidget.TransitionBuilder != oldSwitcher.TransitionBuilder)
            {
                foreach (var entry in _outgoingEntries)
                {
                    UpdateTransitionForEntry(entry);
                }

                if (_currentEntry is not null)
                {
                    UpdateTransitionForEntry(_currentEntry);
                }

                MarkChildWidgetCacheAsDirty();
            }

            bool hasNewChild = CurrentWidget.Child is not null;
            bool hasOldChild = _currentEntry is not null;
            if (hasNewChild != hasOldChild
                || (hasNewChild && !Widget.CanUpdate(_currentEntry!.WidgetChild, CurrentWidget.Child!)))
            {
                _childNumber++;
                AddEntryForNewChild(animate: true);
                return;
            }

            if (_currentEntry is not null)
            {
                _currentEntry.WidgetChild = CurrentWidget.Child!;
                UpdateTransitionForEntry(_currentEntry);
                MarkChildWidgetCacheAsDirty();
            }
        }

        public override Widget Build(BuildContext context)
        {
            RebuildOutgoingWidgetsIfNeeded();
            var outgoingWidgets = _outgoingWidgets!
                .Where(outgoing => outgoing.Key != _currentEntry?.Transition.Key)
                .Distinct()
                .ToArray();
            return CurrentWidget.LayoutBuilder(_currentEntry?.Transition, outgoingWidgets);
        }

        public override void Dispose()
        {
            _currentEntry?.Dispose();
            foreach (var entry in _outgoingEntries.ToArray())
            {
                entry.Dispose();
            }

            _outgoingEntries.Clear();
        }

        private void AddEntryForNewChild(bool animate)
        {
            if (_currentEntry is not null)
            {
                _outgoingEntries.Add(_currentEntry);
                _currentEntry.Controller.Reverse();
                MarkChildWidgetCacheAsDirty();
                _currentEntry = null;
            }

            if (CurrentWidget.Child is null)
            {
                return;
            }

            var controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
            {
                ReverseDuration = CurrentWidget.ReverseDuration,
            };
            var animation = new CurvedAnimation(
                parent: controller,
                curve: CurrentWidget.SwitchInCurve,
                reverseCurve: CurrentWidget.SwitchOutCurve);
            Widget transition = new KeyedSubtree(
                key: new ValueKey<int>(_childNumber),
                child: CurrentWidget.TransitionBuilder(CurrentWidget.Child, animation));
            _currentEntry = new ChildEntry(
                controller: controller,
                animation: animation,
                transition: transition,
                widgetChild: CurrentWidget.Child,
                onDismissed: HandleEntryDismissed);

            if (animate)
            {
                controller.Forward();
            }
            else
            {
                controller.SetValue(1.0);
            }
        }

        private void HandleEntryDismissed(ChildEntry entry)
        {
            if (!Mounted || !_outgoingEntries.Contains(entry))
            {
                return;
            }

            SetState(() =>
            {
                _ = _outgoingEntries.Remove(entry);
                MarkChildWidgetCacheAsDirty();
            });
            entry.Dispose();
        }

        private void MarkChildWidgetCacheAsDirty()
        {
            _outgoingWidgets = null;
        }

        private void UpdateTransitionForEntry(ChildEntry entry)
        {
            entry.Transition = new KeyedSubtree(
                key: entry.Transition.Key,
                child: CurrentWidget.TransitionBuilder(entry.WidgetChild, entry.Animation));
        }

        private void RebuildOutgoingWidgetsIfNeeded()
        {
            _outgoingWidgets ??= _outgoingEntries
                .Select(entry => entry.Transition)
                .ToArray();
        }
    }
}

public enum CrossFadeState
{
    ShowFirst,
    ShowSecond,
}

public delegate Widget AnimatedCrossFadeBuilder(
    Widget topChild,
    Key topChildKey,
    Widget bottomChild,
    Key bottomChildKey);

public sealed class AnimatedCrossFade : StatefulWidget
{
    public AnimatedCrossFade(
        Widget firstChild,
        Widget secondChild,
        CrossFadeState crossFadeState,
        TimeSpan duration,
        Curve? firstCurve = null,
        Curve? secondCurve = null,
        Curve? sizeCurve = null,
        AlignmentGeometry? alignment = null,
        TimeSpan? reverseDuration = null,
        AnimatedCrossFadeBuilder? layoutBuilder = null,
        bool excludeBottomFocus = true,
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

        FirstChild = firstChild ?? throw new ArgumentNullException(nameof(firstChild));
        SecondChild = secondChild ?? throw new ArgumentNullException(nameof(secondChild));
        CrossFadeState = crossFadeState;
        Duration = duration;
        FirstCurve = firstCurve ?? Curves.Linear;
        SecondCurve = secondCurve ?? Curves.Linear;
        SizeCurve = sizeCurve ?? Curves.Linear;
        Alignment = alignment ?? (AlignmentGeometry)Plumix.Rendering.Alignment.TopCenter;
        ReverseDuration = reverseDuration;
        LayoutBuilder = layoutBuilder ?? DefaultLayoutBuilder;
        ExcludeBottomFocus = excludeBottomFocus;
        OnEnd = onEnd;
    }

    public Widget FirstChild { get; }

    public Widget SecondChild { get; }

    public CrossFadeState CrossFadeState { get; }

    public TimeSpan Duration { get; }

    public TimeSpan? ReverseDuration { get; }

    public Curve FirstCurve { get; }

    public Curve SecondCurve { get; }

    public Curve SizeCurve { get; }

    public AlignmentGeometry Alignment { get; }

    public AnimatedCrossFadeBuilder LayoutBuilder { get; }

    public bool ExcludeBottomFocus { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedCrossFadeState();

    public static Widget DefaultLayoutBuilder(
        Widget topChild,
        Key topChildKey,
        Widget bottomChild,
        Key bottomChildKey)
    {
        return new Stack(
            clipBehavior: Clip.None,
            children:
            [
                new Positioned(
                    key: bottomChildKey,
                    left: 0.0,
                    top: 0.0,
                    right: 0.0,
                    child: bottomChild),
                new Positioned(
                    key: topChildKey,
                    child: topChild),
            ]);
    }

    private sealed class AnimatedCrossFadeState : State
    {
        private AnimationController? _controller;
        private MappedDoubleAnimation? _firstAnimation;
        private MappedDoubleAnimation? _secondAnimation;

        private AnimatedCrossFade CurrentWidget => (AnimatedCrossFade)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
            {
                ReverseDuration = CurrentWidget.ReverseDuration,
            };
            if (CurrentWidget.CrossFadeState == CrossFadeState.ShowSecond)
            {
                _controller.SetValue(1.0);
            }

            _firstAnimation = InitAnimation(CurrentWidget.FirstCurve, inverted: true);
            _secondAnimation = InitAnimation(CurrentWidget.SecondCurve, inverted: false);
            _controller.AddStatusListener(HandleStatusChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldCrossFade = (AnimatedCrossFade)oldWidget;
            _controller!.Duration = CurrentWidget.Duration;
            _controller.ReverseDuration = CurrentWidget.ReverseDuration;
            if (CurrentWidget.FirstCurve != oldCrossFade.FirstCurve)
            {
                _firstAnimation!.Dispose();
                _firstAnimation = InitAnimation(CurrentWidget.FirstCurve, inverted: true);
            }
            if (CurrentWidget.SecondCurve != oldCrossFade.SecondCurve)
            {
                _secondAnimation!.Dispose();
                _secondAnimation = InitAnimation(CurrentWidget.SecondCurve, inverted: false);
            }
            if (CurrentWidget.CrossFadeState == oldCrossFade.CrossFadeState)
            {
                return;
            }

            if (CurrentWidget.CrossFadeState == CrossFadeState.ShowFirst)
            {
                _controller.Reverse();
            }
            else
            {
                _controller.Forward();
            }
        }

        public override Widget Build(BuildContext context)
        {
            var firstKey = new ValueKey<CrossFadeState>(CrossFadeState.ShowFirst);
            var secondKey = new ValueKey<CrossFadeState>(CrossFadeState.ShowSecond);
            bool secondIsTop = _controller!.Status.IsForwardOrCompleted();
            Key topKey = secondIsTop ? secondKey : firstKey;
            Widget topChild = secondIsTop ? CurrentWidget.SecondChild : CurrentWidget.FirstChild;
            Animation<double> topAnimation = secondIsTop ? _secondAnimation! : _firstAnimation!;
            Key bottomKey = secondIsTop ? firstKey : secondKey;
            Widget bottomChild = secondIsTop ? CurrentWidget.FirstChild : CurrentWidget.SecondChild;
            Animation<double> bottomAnimation = secondIsTop ? _firstAnimation! : _secondAnimation!;

            bottomChild = new TickerMode(
                key: bottomKey,
                enabled: _controller.IsAnimating,
                child: new IgnorePointer(
                    child: new ExcludeSemantics(
                        child: new ExcludeFocus(
                            excluding: CurrentWidget.ExcludeBottomFocus,
                            child: new FadeTransition(
                                opacity: bottomAnimation,
                                child: bottomChild)))));
            topChild = new TickerMode(
                key: topKey,
                enabled: true,
                child: new IgnorePointer(
                    ignoring: false,
                    child: new ExcludeSemantics(
                        excluding: false,
                        child: new ExcludeFocus(
                            excluding: false,
                            child: new FadeTransition(
                                opacity: topAnimation,
                                child: topChild)))));

            return new ClipRect(
                child: new AnimatedSize(
                    alignment: CurrentWidget.Alignment,
                    duration: CurrentWidget.Duration,
                    reverseDuration: CurrentWidget.ReverseDuration,
                    curve: CurrentWidget.SizeCurve,
                    child: CurrentWidget.LayoutBuilder(topChild, topKey, bottomChild, bottomKey)));
        }

        public override void Dispose()
        {
            _controller!.RemoveStatusListener(HandleStatusChanged);
            _firstAnimation!.Dispose();
            _secondAnimation!.Dispose();
            _controller.Dispose();
            _firstAnimation = null;
            _secondAnimation = null;
            _controller = null;
        }

        private MappedDoubleAnimation InitAnimation(Curve curve, bool inverted)
        {
            return new MappedDoubleAnimation(
                _controller!,
                value => inverted ? 1.0 - curve(value) : curve(value));
        }

        private void HandleStatusChanged(AnimationStatus status)
        {
            if (!Mounted)
            {
                return;
            }

            SetState(() => { });
            if (status is AnimationStatus.Completed or AnimationStatus.Dismissed)
            {
                CurrentWidget.OnEnd?.Invoke();
            }
        }
    }
}
