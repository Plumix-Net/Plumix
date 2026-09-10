using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/menu_anchor.dart

/// <summary>Dart parity source: <c>_SwipeTarget</c>.</summary>
internal sealed class CupertinoMenuSwipeTarget : StatelessWidget
{
    public CupertinoMenuSwipeTarget(
        Widget child,
        Action? onEnter = null,
        Action? onExit = null,
        Action? onCompletion = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnEnter = onEnter;
        OnExit = onExit;
        OnCompletion = onCompletion;
    }

    public Widget Child { get; }

    public Action? OnEnter { get; }

    public Action? OnExit { get; }

    public Action? OnCompletion { get; }

    /// <summary>Whether a swipe stops descending once it reaches this target.</summary>
    public bool IsOpaque => true;

    public override Widget Build(BuildContext context) => new MetaData(
        metaData: this,
        behavior: HitTestBehavior.DeferToChild,
        child: Child);
}

/// <summary>Dart parity source: <c>_SwipeScope</c>.</summary>
internal sealed class CupertinoMenuSwipeScope : InheritedWidget
{
    public CupertinoMenuSwipeScope(CupertinoMenuSwipeRegionState state, Widget child) : base(child)
    {
        State = state;
    }

    public CupertinoMenuSwipeRegionState State { get; }

    public static CupertinoMenuSwipeRegionState? MaybeOf(BuildContext context) =>
        context.DependOnInherited<CupertinoMenuSwipeScope>()?.State;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(((CupertinoMenuSwipeScope)oldWidget).State, State);
}

/// <summary>Dart parity source: <c>_SwipeRegion</c>.</summary>
internal sealed class CupertinoMenuSwipeRegion : StatefulWidget
{
    public CupertinoMenuSwipeRegion(
        bool enabled,
        Action<double> onDistanceChanged,
        Widget child,
        Key? key = null) : base(key)
    {
        Enabled = enabled;
        OnDistanceChanged = onDistanceChanged;
        Child = child;
    }

    public bool Enabled { get; }

    public Action<double> OnDistanceChanged { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoMenuSwipeRegionState();
}

/// <summary>Dart parity source: <c>_SwipeRegionState</c>.</summary>
internal sealed class CupertinoMenuSwipeRegionState : State
{
    private readonly List<RenderCupertinoMenuSwipeSurface> _surfaces = [];
    private MultiDragGestureRecognizer? _recognizer;
    private Point? _position;

    private CupertinoMenuSwipeRegion Current => (CupertinoMenuSwipeRegion)StateWidget;

    private bool IsSwiping => _position.HasValue;

    public override void DidChangeDependencies()
    {
        if (_recognizer is not null)
        {
            _recognizer.GestureSettings = MediaQuery.MaybeGestureSettingsOf(Context);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuSwipeRegion)oldWidget;
        if (previous.Enabled && !Current.Enabled)
        {
            _recognizer?.Dispose();
            _recognizer = null;
            _position = null;
            Current.OnDistanceChanged(0.0);
        }
    }

    public override Widget Build(BuildContext context) =>
        new CupertinoMenuSwipeScope(this, Current.Child);

    public override void Dispose()
    {
        _recognizer?.Dispose();
        _recognizer = null;

        base.Dispose();
    }

    internal void Register(RenderCupertinoMenuSwipeSurface surface)
    {
        if (!_surfaces.Contains(surface))
        {
            _surfaces.Add(surface);
        }
    }

    internal void Unregister(RenderCupertinoMenuSwipeSurface surface) => _surfaces.Remove(surface);

    /// <summary>Dart parity source: <c>_SwipeRegionState.beginSwipe</c>.</summary>
    internal void BeginSwipe(PointerDownEvent @event, TimeSpan delay, Action? onStart)
    {
        if (IsSwiping || !Current.Enabled)
        {
            return;
        }

        _recognizer?.Dispose();
        _recognizer = null;

        Drag? HandleStart(Point position)
        {
            onStart?.Invoke();
            return CreateSwipeHandle(position);
        }

        _recognizer = delay == TimeSpan.Zero
            ? new ImmediateMultiDragGestureRecognizer
            {
                AllowedButtonsFilter = buttons => buttons == PointerButtons.Primary,
                OnStart = HandleStart,
            }
            : new DelayedMultiDragGestureRecognizer(delay)
            {
                AllowedButtonsFilter = buttons => buttons == PointerButtons.Primary,
                OnStart = HandleStart,
            };
        _recognizer.GestureSettings = MediaQuery.MaybeGestureSettingsOf(Context);
        _recognizer.AddPointer(@event);
    }

    private Drag CreateSwipeHandle(Point position)
    {
        _position = position;
        return new CupertinoMenuSwipeHandle(
            viewId: View.MaybeOf(Context)?.ViewId ?? 0,
            initialPosition: position,
            onSwipeUpdate: HandleSwipeUpdate,
            onSwipeEnd: HandleSwipeEnd,
            onSwipeCanceled: HandleSwipeCancel);
    }

    /// <summary>Dart parity source: <c>_SwipeRegionState._handleSwipeUpdate</c>.</summary>
    private void HandleSwipeUpdate(DragUpdateDetails details)
    {
        _position = (_position ?? default) + details.Delta;
        double minimumSquaredDistance = double.PositiveInfinity;
        for (int index = 0; index < _surfaces.Count; index++)
        {
            double squaredDistance = CupertinoMenuMetrics.ComputeSquaredDistanceToRect(
                _position.Value,
                _surfaces[index].ComputeRect());
            if (Math.Floor(squaredDistance) == 0.0)
            {
                Current.OnDistanceChanged(0.0);
                return;
            }

            minimumSquaredDistance = Math.Min(minimumSquaredDistance, squaredDistance);
        }

        Current.OnDistanceChanged(
            double.IsPositiveInfinity(minimumSquaredDistance) ? 0.0 : Math.Sqrt(minimumSquaredDistance));
    }

    private void HandleSwipeEnd(DragEndDetails details) => CompleteSwipe();

    private void HandleSwipeCancel() => CompleteSwipe();

    private void CompleteSwipe()
    {
        _recognizer?.Dispose();
        _recognizer = null;
        _position = null;
        if (Mounted)
        {
            Current.OnDistanceChanged(0.0);
        }
    }
}

/// <summary>Dart parity source: <c>_SwipeSurface</c>.</summary>
internal sealed class CupertinoMenuSwipeSurface : SingleChildRenderObjectWidget
{
    public CupertinoMenuSwipeSurface(
        Widget child,
        TimeSpan? delay = null,
        Action? onStart = null,
        Key? key = null) : base(child, key)
    {
        Delay = delay ?? TimeSpan.Zero;
        OnStart = onStart;
    }

    public TimeSpan Delay { get; }

    public Action? OnStart { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderCupertinoMenuSwipeSurface(
            region: CupertinoMenuSwipeScope.MaybeOf(context),
            delay: Delay,
            onStart: OnStart);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var surface = (RenderCupertinoMenuSwipeSurface)renderObject;
        surface.Region = CupertinoMenuSwipeScope.MaybeOf(context);
        surface.Delay = Delay;
        surface.OnStart = OnStart;
    }
}

/// <summary>Dart parity source: <c>_RenderSwipeSurface</c>.</summary>
internal sealed class RenderCupertinoMenuSwipeSurface : RenderProxyBoxWithHitTestBehavior
{
    private CupertinoMenuSwipeRegionState? _region;

    public RenderCupertinoMenuSwipeSurface(
        CupertinoMenuSwipeRegionState? region,
        TimeSpan delay,
        Action? onStart) : base(HitTestBehavior.Opaque)
    {
        _region = region;
        Delay = delay;
        OnStart = onStart;
        _region?.Register(this);
    }

    public TimeSpan Delay { get; set; }

    public Action? OnStart { get; set; }

    public CupertinoMenuSwipeRegionState? Region
    {
        get => _region;
        set
        {
            if (ReferenceEquals(_region, value))
            {
                return;
            }

            _region?.Unregister(this);
            _region = value;
            _region?.Register(this);
        }
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (@event is PointerDownEvent down)
        {
            _region?.BeginSwipe(down, Delay, OnStart);
        }

        base.HandleEvent(@event, entry);
    }

    /// <summary>Dart parity source: <c>_RenderSwipeSurface.computeRect</c>.</summary>
    public Rect ComputeRect() => HasSize
        ? new Rect(LocalToGlobal(default), Size)
        : default;

    public override void Dispose()
    {
        _region?.Unregister(this);
        _region = null;

        base.Dispose();
    }
}

/// <summary>Dart parity source: <c>_SwipeHandle</c>.</summary>
internal sealed class CupertinoMenuSwipeHandle : Drag
{
    private readonly List<CupertinoMenuSwipeTarget> _enteredTargets = [];
    private readonly int _viewId;
    private readonly Action<DragUpdateDetails> _onSwipeUpdate;
    private readonly Action<DragEndDetails> _onSwipeEnd;
    private readonly Action _onSwipeCanceled;
    private Point _position;

    public CupertinoMenuSwipeHandle(
        int viewId,
        Point initialPosition,
        Action<DragUpdateDetails> onSwipeUpdate,
        Action<DragEndDetails> onSwipeEnd,
        Action onSwipeCanceled)
    {
        _viewId = viewId;
        _position = initialPosition;
        _onSwipeUpdate = onSwipeUpdate;
        _onSwipeEnd = onSwipeEnd;
        _onSwipeCanceled = onSwipeCanceled;
        UpdateSwipe();
    }

    public override void Update(DragUpdateDetails details)
    {
        Point next = _position + details.Delta;
        if (next == _position)
        {
            return;
        }

        _position = next;
        UpdateSwipe();
        _onSwipeUpdate(details);
    }

    public override void End(DragEndDetails details)
    {
        LeaveAllEntered(pointerUp: true);
        _onSwipeEnd(details);
    }

    public override void Cancel()
    {
        LeaveAllEntered();
        _onSwipeCanceled();
    }

    /// <summary>Dart parity source: <c>_SwipeHandle._updateSwipe</c>.</summary>
    private void UpdateSwipe()
    {
        HitTestResult result = GestureBinding.Instance.HitTestInView(_position, _viewId);
        var targets = new List<CupertinoMenuSwipeTarget>();
        foreach (HitTestEntry entry in result.Path)
        {
            if (entry.Target is RenderMetaData { MetaData: CupertinoMenuSwipeTarget target })
            {
                targets.Add(target);
            }
        }

        var hitTargets = new List<CupertinoMenuSwipeTarget>();
        var newlyEnteredTargets = new List<CupertinoMenuSwipeTarget>();
        bool hitExistingTarget = false;
        foreach (CupertinoMenuSwipeTarget target in targets)
        {
            if (_enteredTargets.Contains(target))
            {
                hitExistingTarget = true;
                hitTargets.Add(target);
            }
            else if (!hitExistingTarget)
            {
                hitTargets.Add(target);
                newlyEnteredTargets.Add(target);
            }

            if (target.IsOpaque)
            {
                break;
            }
        }

        for (int index = _enteredTargets.Count - 1; index >= 0; index--)
        {
            CupertinoMenuSwipeTarget target = _enteredTargets[index];
            if (!hitTargets.Contains(target))
            {
                target.OnExit?.Invoke();
            }
        }

        for (int index = newlyEnteredTargets.Count - 1; index >= 0; index--)
        {
            newlyEnteredTargets[index].OnEnter?.Invoke();
        }

        _enteredTargets.Clear();
        _enteredTargets.AddRange(hitTargets);
    }

    /// <summary>Dart parity source: <c>_SwipeHandle._leaveAllEntered</c>.</summary>
    private void LeaveAllEntered(bool pointerUp = false)
    {
        foreach (CupertinoMenuSwipeTarget target in _enteredTargets)
        {
            target.OnExit?.Invoke();
            if (pointerUp)
            {
                target.OnCompletion?.Invoke();
            }
        }

        _enteredTargets.Clear();
    }
}
