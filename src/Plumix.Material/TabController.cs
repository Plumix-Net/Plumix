using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tab_controller.dart

public sealed class TabController : ChangeNotifier
{
    private Plumix.AnimationController? _animationController;
    private double _animationStart;
    private double _animationTarget;
    private double _animationValue;
    private int _index;
    private int _previousIndex;
    private int _indexIsChangingCount;

    public TabController(int length, int initialIndex = 0, TimeSpan? animationDuration = null)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (initialIndex < 0 || (length > 0 && initialIndex >= length) || (length == 0 && initialIndex != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(initialIndex));
        }

        Length = length;
        _index = initialIndex;
        _previousIndex = initialIndex;
        _animationValue = initialIndex;
        AnimationDuration = animationDuration ?? TimeSpan.FromMilliseconds(300);
    }

    public int Length { get; }
    public TimeSpan AnimationDuration { get; }
    public int Index
    {
        get => _index;
        set => ChangeIndex(value, duration: TimeSpan.Zero, Curves.Ease);
    }

    public int PreviousIndex => _previousIndex;
    public bool IndexIsChanging => _indexIsChangingCount != 0;
    public double AnimationValue => _animationValue;

    public double Offset
    {
        get => _animationValue - _index;
        set
        {
            if (!double.IsFinite(value) || value < -1 || value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (IndexIsChanging) throw new InvalidOperationException("Offset cannot be changed while the index is changing.");
            SetAnimationValue(_index + value);
        }
    }

    public void AnimateTo(int value, TimeSpan? duration = null, Curve? curve = null) =>
        ChangeIndex(value, duration ?? AnimationDuration, curve ?? Curves.Ease);

    public override void Dispose()
    {
        StopAnimation(completeIndexChange: false);
        base.Dispose();
    }

    private void ChangeIndex(int value, TimeSpan duration, Curve curve)
    {
        ValidateIndex(value);
        if (value == _index || Length < 2) return;

        StopAnimation(completeIndexChange: true);
        _previousIndex = _index;
        _index = value;
        if (duration <= TimeSpan.Zero)
        {
            _indexIsChangingCount++;
            SetAnimationValue(value, notify: true);
            _indexIsChangingCount--;
            NotifyListeners();
            return;
        }

        _indexIsChangingCount++;
        NotifyListeners();
        _animationStart = _animationValue;
        _animationTarget = value;
        _animationController = new Plumix.AnimationController(duration) { Curve = curve };
        _animationController.Changed += HandleAnimationChanged;
        _animationController.Completed += HandleAnimationCompleted;
        _animationController.Forward(0);
    }

    private void HandleAnimationChanged()
    {
        if (_animationController is null) return;
        SetAnimationValue(
            _animationStart + ((_animationTarget - _animationStart) * _animationController.Evaluate()),
            notify: true);
    }

    private void HandleAnimationCompleted()
    {
        SetAnimationValue(_index, notify: false);
        StopAnimation(completeIndexChange: true);
        NotifyListeners();
    }

    private void StopAnimation(bool completeIndexChange)
    {
        if (_animationController is not null)
        {
            _animationController.Changed -= HandleAnimationChanged;
            _animationController.Completed -= HandleAnimationCompleted;
            _animationController.Dispose();
            _animationController = null;
        }

        if (completeIndexChange) _indexIsChangingCount = 0;
    }

    private void SetAnimationValue(double value, bool notify = true)
    {
        var max = Math.Max(0, Length - 1);
        var effective = Math.Clamp(value, 0, max);
        if (Math.Abs(_animationValue - effective) <= 0.000001) return;
        _animationValue = effective;
        if (notify) NotifyListeners();
    }

    private void ValidateIndex(int value)
    {
        if (value < 0 || (Length > 0 && value >= Length) || (Length == 0 && value != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}

public sealed class DefaultTabController : StatefulWidget
{
    public DefaultTabController(
        int length,
        Widget child,
        int initialIndex = 0,
        TimeSpan? animationDuration = null,
        Key? key = null) : base(key)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (initialIndex < 0 || (length > 0 && initialIndex >= length) || (length == 0 && initialIndex != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(initialIndex));
        }

        Length = length;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        InitialIndex = initialIndex;
        AnimationDuration = animationDuration;
    }

    public int Length { get; }
    public int InitialIndex { get; }
    public TimeSpan? AnimationDuration { get; }
    public Widget Child { get; }

    public static TabController? MaybeOf(BuildContext context) =>
        context.DependOnInherited<TabControllerScope>()?.Controller;

    public static TabController Of(BuildContext context) => MaybeOf(context)
        ?? throw new InvalidOperationException("No DefaultTabController ancestor was found.");

    public override State CreateState() => new DefaultTabControllerState();

    private sealed class DefaultTabControllerState : State
    {
        private TabController? _controller;
        private DefaultTabController CurrentWidget => (DefaultTabController)StateWidget;

        public override void InitState() => _controller = CreateController(CurrentWidget);

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (DefaultTabController)oldWidget;
            if (old.Length == CurrentWidget.Length && old.AnimationDuration == CurrentWidget.AnimationDuration) return;
            var previous = _controller!;
            var index = Math.Min(previous.Index, Math.Max(0, CurrentWidget.Length - 1));
            _controller = new TabController(CurrentWidget.Length, index, CurrentWidget.AnimationDuration);
            previous.Dispose();
        }

        public override void Dispose()
        {
            _controller?.Dispose();
            _controller = null;
        }

        public override Widget Build(BuildContext context) => new TabControllerScope(
            _controller!,
            CurrentWidget.Child);

        private static TabController CreateController(DefaultTabController widget) =>
            new(widget.Length, widget.InitialIndex, widget.AnimationDuration);
    }
}

internal sealed class TabControllerScope : InheritedWidget
{
    public TabControllerScope(TabController controller, Widget child) : base()
    {
        Controller = controller;
        Child = child;
    }

    public TabController Controller { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(((TabControllerScope)oldWidget).Controller, Controller);
}
