using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tab_controller.dart

/// <summary>
/// Coordinates tab selection between a <see cref="TabBar"/> and a <see cref="TabBarView"/>.
/// </summary>
public sealed class TabController : ChangeNotifier
{
    private readonly TimeSpan _animationDuration;
    private Plumix.AnimationController? _animationController;
    private int _index;
    private int _previousIndex;
    private int _indexIsChangingCount;

    public TabController(
        int length,
        int initialIndex = 0,
        TimeSpan? animationDuration = null,
        ITickerProvider? vsync = null)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (initialIndex < 0 || (length != 0 && initialIndex >= length))
        {
            throw new ArgumentOutOfRangeException(nameof(initialIndex));
        }

        Length = length;
        _index = initialIndex;
        _previousIndex = initialIndex;
        _animationDuration = animationDuration ?? MaterialConstants.TabScrollDuration;
        _animationController = Plumix.AnimationController.Unbounded(
            value: initialIndex,
            vsync: vsync);
    }

    private TabController(
        int index,
        int previousIndex,
        Plumix.AnimationController? animationController,
        TimeSpan animationDuration,
        int length)
    {
        _index = index;
        _previousIndex = previousIndex;
        _animationController = animationController;
        _animationDuration = animationDuration;
        Length = length;
    }

    /// <summary>
    /// An animation whose value represents the current position of the <see cref="TabBarView"/>'s
    /// selected tab. Null after <see cref="Dispose"/>.
    /// </summary>
    public Animation<double>? Animation => _animationController;

    /// <summary>The duration of the index-change animation started by <see cref="AnimateTo"/>.</summary>
    public TimeSpan AnimationDuration => _animationDuration;

    /// <summary>The total number of tabs.</summary>
    public int Length { get; }

    /// <summary>The index of the currently selected tab.</summary>
    public int Index
    {
        get => _index;
        set => ChangeIndex(value);
    }

    /// <summary>The index of the previously selected tab.</summary>
    public int PreviousIndex => _previousIndex;

    /// <summary>True while animating from <see cref="PreviousIndex"/> to <see cref="Index"/>.</summary>
    public bool IndexIsChanging => _indexIsChangingCount != 0;

    /// <summary>
    /// The difference between the <see cref="Animation"/> value and <see cref="Index"/>, in the
    /// range <c>[-1, 1]</c>.
    /// </summary>
    public double Offset
    {
        get => RequireAnimationController().Value - _index;
        set
        {
            if (!double.IsFinite(value) || value < -1.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (IndexIsChanging)
            {
                throw new InvalidOperationException(
                    "TabController.Offset cannot be changed while the index is changing.");
            }

            if (value == Offset)
            {
                return;
            }

            SetControllerValue(value + _index);
        }
    }

    /// <summary>Immediately sets <see cref="Index"/> and animates the selection to it.</summary>
    public void AnimateTo(int value, TimeSpan? duration = null, Curve? curve = null)
    {
        ChangeIndex(value, duration ?? _animationDuration, curve ?? Curves.Ease);
    }

    public override void Dispose()
    {
        // Cleared first so the settle continuation of an in-flight animation sees a disposed
        // controller and stays silent, as Dart's `whenCompleteOrCancel` microtask does.
        Plumix.AnimationController? controller = _animationController;
        _animationController = null;
        controller?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Dart parity: <c>TabController._copyWithAndDispose</c>. Hands the live animation controller to
    /// a fresh controller and disposes this one, so listeners bound to the animation survive a
    /// <see cref="DefaultTabController"/> length or duration change.
    /// </summary>
    internal TabController CopyWithAndDispose(
        int? index,
        int? length,
        int? previousIndex,
        TimeSpan? animationDuration)
    {
        if (index is { } newIndex)
        {
            RequireAnimationController().SetValue(newIndex);
        }

        var result = new TabController(
            index: index ?? _index,
            previousIndex: previousIndex ?? _previousIndex,
            animationController: _animationController,
            animationDuration: animationDuration ?? _animationDuration,
            length: length ?? Length);
        _animationController = null;
        Dispose();
        return result;
    }

    private void ChangeIndex(int value, TimeSpan? duration = null, Curve? curve = null)
    {
        if (value < 0 || (Length != 0 && value >= Length))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == _index || Length < 2)
        {
            return;
        }

        _previousIndex = _index;
        _index = value;
        if (duration is { } effectiveDuration && effectiveDuration > TimeSpan.Zero)
        {
            _indexIsChangingCount += 1;
            NotifyListeners();
            Task animation = RequireAnimationController().AnimateTo(_index, effectiveDuration, curve);
            _ = animation.ContinueWith(
                _ => HandleIndexChangeSettled(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return;
        }

        _indexIsChangingCount += 1;
        SetControllerValue(_index);
        _indexIsChangingCount -= 1;
        NotifyListeners();
    }

    private void HandleIndexChangeSettled()
    {
        // Dart only skips the notification when the controller was disposed mid-animation.
        if (_animationController is null)
        {
            return;
        }

        _indexIsChangingCount -= 1;
        NotifyListeners();
    }

    /// <summary>
    /// Mirrors Dart's <c>AnimationController.value</c> setter, which stops any running animation
    /// before adopting the new value.
    /// </summary>
    private void SetControllerValue(double value)
    {
        Plumix.AnimationController controller = RequireAnimationController();
        controller.Stop();
        controller.SetValue(value);
    }

    private Plumix.AnimationController RequireAnimationController()
    {
        return _animationController
               ?? throw new InvalidOperationException("This TabController has already been disposed.");
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/material/tab_controller.dart (_TabControllerScope)
internal sealed class TabControllerScope : InheritedWidget
{
    public TabControllerScope(TabController controller, bool enabled, Widget child, Key? key = null) : base(key)
    {
        Controller = controller;
        Enabled = enabled;
        Child = child;
    }

    public TabController Controller { get; }

    public bool Enabled { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (TabControllerScope)oldWidget;
        return Enabled != old.Enabled || !ReferenceEquals(old.Controller, Controller);
    }
}

/// <summary>
/// The <see cref="TabController"/> for descendant widgets that do not specify one explicitly.
/// </summary>
public sealed class DefaultTabController : StatefulWidget
{
    public DefaultTabController(
        int length,
        Widget child,
        int initialIndex = 0,
        TimeSpan? animationDuration = null,
        Key? key = null) : base(key)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (length != 0 && (initialIndex < 0 || initialIndex >= length))
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
        ?? throw new InvalidOperationException(
            "DefaultTabController.Of() was called with a context that does not contain a "
            + "DefaultTabController widget.\nNo DefaultTabController widget ancestor could be found "
            + "starting from the context that was passed to DefaultTabController.Of().");

    public override State CreateState() => new DefaultTabControllerState();

    private sealed class DefaultTabControllerState : State
    {
        private TabController? _controller;

        private DefaultTabController Current => (DefaultTabController)StateWidget;

        public override void InitState()
        {
            _controller = new TabController(
                length: Current.Length,
                initialIndex: Current.InitialIndex,
                animationDuration: Current.AnimationDuration,
                vsync: this);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (DefaultTabController)oldWidget;
            if (old.Length != Current.Length)
            {
                int? newIndex = null;
                int previousIndex = _controller!.PreviousIndex;
                if (_controller.Index >= Current.Length)
                {
                    newIndex = Math.Max(0, Current.Length - 1);
                    previousIndex = _controller.Index;
                }

                _controller = _controller.CopyWithAndDispose(
                    index: newIndex,
                    length: Current.Length,
                    previousIndex: previousIndex,
                    animationDuration: Current.AnimationDuration);
            }

            if (old.AnimationDuration != Current.AnimationDuration)
            {
                _controller = _controller!.CopyWithAndDispose(
                    index: _controller.Index,
                    length: Current.Length,
                    previousIndex: _controller.PreviousIndex,
                    animationDuration: Current.AnimationDuration);
            }
        }

        public override void Dispose()
        {
            _controller?.Dispose();
            _controller = null;
        }

        public override Widget Build(BuildContext context) => new TabControllerScope(
            controller: _controller!,
            enabled: TickerMode.Of(context),
            child: Current.Child);
    }
}
