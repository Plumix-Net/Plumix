using Avalonia;
using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/drag_target.dart

public delegate Widget DragTargetBuilder<T>(
    BuildContext context,
    IReadOnlyList<T?> candidateData,
    IReadOnlyList<object?> rejectedData);

public delegate Point DragAnchorStrategy<T>(
    Draggable<T> draggable,
    BuildContext context,
    Point position);

public readonly record struct DraggableDetails(
    bool WasAccepted,
    Velocity Velocity,
    Point Offset)
{
    public DraggableDetails(Velocity velocity, Point offset) : this(
        WasAccepted: false,
        Velocity: velocity,
        Offset: offset)
    {
    }
}

public readonly record struct DragTargetDetails<T>(T Data, Point Offset);

public class Draggable<T> : StatefulWidget
{
    public Draggable(
        Widget child,
        Widget feedback,
        T? data = default,
        Axis? axis = null,
        Widget? childWhenDragging = null,
        Point feedbackOffset = default,
        DragAnchorStrategy<T>? dragAnchorStrategy = null,
        Axis? affinity = null,
        int? maxSimultaneousDrags = null,
        Action? onDragStarted = null,
        Action<DragUpdateDetails>? onDragUpdate = null,
        Action<Velocity, Point>? onDraggableCanceled = null,
        Action<DraggableDetails>? onDragEnd = null,
        Action? onDragCompleted = null,
        bool ignoringFeedbackSemantics = true,
        bool ignoringFeedbackPointer = true,
        bool rootOverlay = false,
        HitTestBehavior hitTestBehavior = HitTestBehavior.DeferToChild,
        Func<PointerButtons, bool>? allowedButtonsFilter = null,
        Key? key = null) : base(key)
    {
        if (maxSimultaneousDrags is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSimultaneousDrags));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        Data = data;
        Axis = axis;
        ChildWhenDragging = childWhenDragging;
        FeedbackOffset = feedbackOffset;
        DragAnchorStrategy = dragAnchorStrategy ?? ChildDragAnchorStrategy;
        Affinity = affinity;
        MaxSimultaneousDrags = maxSimultaneousDrags;
        OnDragStarted = onDragStarted;
        OnDragUpdate = onDragUpdate;
        OnDraggableCanceled = onDraggableCanceled;
        OnDragEnd = onDragEnd;
        OnDragCompleted = onDragCompleted;
        IgnoringFeedbackSemantics = ignoringFeedbackSemantics;
        IgnoringFeedbackPointer = ignoringFeedbackPointer;
        RootOverlay = rootOverlay;
        HitTestBehavior = hitTestBehavior;
        AllowedButtonsFilter = allowedButtonsFilter;
    }

    public T? Data { get; }

    public Axis? Axis { get; }

    public Widget Child { get; }

    public Widget? ChildWhenDragging { get; }

    public Widget Feedback { get; }

    public Point FeedbackOffset { get; }

    public DragAnchorStrategy<T> DragAnchorStrategy { get; }

    public bool IgnoringFeedbackSemantics { get; }

    public bool IgnoringFeedbackPointer { get; }

    public Axis? Affinity { get; }

    public int? MaxSimultaneousDrags { get; }

    public Action? OnDragStarted { get; }

    public Action<DragUpdateDetails>? OnDragUpdate { get; }

    public Action<Velocity, Point>? OnDraggableCanceled { get; }

    public Action? OnDragCompleted { get; }

    public Action<DraggableDetails>? OnDragEnd { get; }

    public bool RootOverlay { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public Func<PointerButtons, bool>? AllowedButtonsFilter { get; }

    public static Point ChildDragAnchorStrategy(
        Draggable<T> draggable,
        BuildContext context,
        Point position)
    {
        _ = draggable;
        if (context.FindRenderObject() is not RenderBox renderBox
            || !renderBox.TryGetTransformFromRoot(out Matrix localToRoot)
            || !localToRoot.TryInvert(out Matrix rootToLocal))
        {
            throw new InvalidOperationException("The Draggable child must have an attached RenderBox.");
        }

        return rootToLocal.Transform(position);
    }

    public static Point PointerDragAnchorStrategy(
        Draggable<T> draggable,
        BuildContext context,
        Point position)
    {
        _ = draggable;
        _ = context;
        _ = position;
        return default;
    }

    internal virtual DraggableGestureRecognizer CreateRecognizer(
        Func<Point, IDragAvatar?> onStart)
    {
        return new DraggableGestureRecognizer(
            affinity: Affinity,
            allowedButtonsFilter: AllowedButtonsFilter,
            onStart: onStart);
    }

    public override State CreateState() => new DraggableState<T>();
}

public sealed class LongPressDraggable<T> : Draggable<T>
{
    public LongPressDraggable(
        Widget child,
        Widget feedback,
        T? data = default,
        Axis? axis = null,
        Widget? childWhenDragging = null,
        Point feedbackOffset = default,
        DragAnchorStrategy<T>? dragAnchorStrategy = null,
        int? maxSimultaneousDrags = null,
        Action? onDragStarted = null,
        Action<DragUpdateDetails>? onDragUpdate = null,
        Action<Velocity, Point>? onDraggableCanceled = null,
        Action<DraggableDetails>? onDragEnd = null,
        Action? onDragCompleted = null,
        bool hapticFeedbackOnStart = true,
        bool ignoringFeedbackSemantics = true,
        bool ignoringFeedbackPointer = true,
        TimeSpan? delay = null,
        Func<PointerButtons, bool>? allowedButtonsFilter = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.DeferToChild,
        bool rootOverlay = false,
        Key? key = null) : base(
        child: child,
        feedback: feedback,
        data: data,
        axis: axis,
        childWhenDragging: childWhenDragging,
        feedbackOffset: feedbackOffset,
        dragAnchorStrategy: dragAnchorStrategy,
        maxSimultaneousDrags: maxSimultaneousDrags,
        onDragStarted: onDragStarted,
        onDragUpdate: onDragUpdate,
        onDraggableCanceled: onDraggableCanceled,
        onDragEnd: onDragEnd,
        onDragCompleted: onDragCompleted,
        ignoringFeedbackSemantics: ignoringFeedbackSemantics,
        ignoringFeedbackPointer: ignoringFeedbackPointer,
        rootOverlay: rootOverlay,
        hitTestBehavior: hitTestBehavior,
        allowedButtonsFilter: allowedButtonsFilter,
        key: key)
    {
        HapticFeedbackOnStart = hapticFeedbackOnStart;
        Delay = delay ?? TimeSpan.FromMilliseconds(500);
    }

    public bool HapticFeedbackOnStart { get; }

    public TimeSpan Delay { get; }

    internal override DraggableGestureRecognizer CreateRecognizer(
        Func<Point, IDragAvatar?> onStart)
    {
        return new DraggableGestureRecognizer(
            affinity: null,
            allowedButtonsFilter: AllowedButtonsFilter,
            onStart: onStart,
            delay: Delay,
            onAvatarStarted: HapticFeedbackOnStart
                ? Plumix.UI.Feedback.ForSelectionClick
                : null);
    }
}

internal sealed class DraggableState<T> : State
{
    private DraggableGestureRecognizer? _recognizer;
    private int _activeCount;

    private Draggable<T> CurrentWidget => (Draggable<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _recognizer = CreateRecognizer();
    }

    public override void Dispose()
    {
        DisposeRecognizerIfInactive();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        bool canDrag = !widget.MaxSimultaneousDrags.HasValue
                       || _activeCount < widget.MaxSimultaneousDrags.Value;
        bool showChild = _activeCount == 0 || widget.ChildWhenDragging is null;
        return new Listener(
            behavior: widget.HitTestBehavior,
            onPointerDown: canDrag ? RoutePointer : null,
            child: showChild ? widget.Child : widget.ChildWhenDragging);
    }

    private DraggableGestureRecognizer CreateRecognizer()
    {
        return CurrentWidget.CreateRecognizer(StartDrag);
    }

    private void RoutePointer(PointerDownEvent @event)
    {
        var widget = CurrentWidget;
        if (widget.MaxSimultaneousDrags.HasValue
            && _activeCount >= widget.MaxSimultaneousDrags.Value)
        {
            return;
        }

        _recognizer?.AddPointer(@event);
    }

    private DragAvatar<T>? StartDrag(Point position)
    {
        var widget = CurrentWidget;
        if (widget.MaxSimultaneousDrags.HasValue
            && _activeCount >= widget.MaxSimultaneousDrags.Value)
        {
            return null;
        }

        Point dragStartPoint = widget.DragAnchorStrategy(widget, Context, position);
        if (Mounted)
        {
            SetState(() => _activeCount += 1);
        }
        else
        {
            _activeCount += 1;
        }

        var avatar = new DragAvatar<T>(
            overlayState: Overlay.Of(Context, rootOverlay: widget.RootOverlay),
            data: widget.Data,
            axis: widget.Axis,
            initialPosition: position,
            dragStartPoint: dragStartPoint,
            feedback: widget.Feedback,
            feedbackOffset: widget.FeedbackOffset,
            ignoringFeedbackSemantics: widget.IgnoringFeedbackSemantics,
            ignoringFeedbackPointer: widget.IgnoringFeedbackPointer,
            onDragUpdate: details =>
            {
                if (Mounted)
                {
                    CurrentWidget.OnDragUpdate?.Invoke(details);
                }
            },
            onDragEnd: HandleDragEnd);
        widget.OnDragStarted?.Invoke();
        return avatar;
    }

    private void HandleDragEnd(Velocity velocity, Point offset, bool wasAccepted)
    {
        if (Mounted)
        {
            SetState(() => _activeCount -= 1);
        }
        else
        {
            _activeCount -= 1;
            DisposeRecognizerIfInactive();
        }

        if (Mounted)
        {
            CurrentWidget.OnDragEnd?.Invoke(
                new DraggableDetails(
                    WasAccepted: wasAccepted,
                    Velocity: velocity,
                    Offset: offset));
        }

        if (wasAccepted)
        {
            CurrentWidget.OnDragCompleted?.Invoke();
        }
        else
        {
            CurrentWidget.OnDraggableCanceled?.Invoke(velocity, offset);
        }
    }

    private void DisposeRecognizerIfInactive()
    {
        if (_activeCount > 0)
        {
            return;
        }

        _recognizer?.Dispose();
        _recognizer = null;
    }
}

#pragma warning disable CS0618

public sealed class DragTarget<T> : StatefulWidget
{
    public DragTarget(
        DragTargetBuilder<T> builder,
        Func<T?, bool>? onWillAccept = null,
        Func<DragTargetDetails<T>, bool>? onWillAcceptWithDetails = null,
        Action<T>? onAccept = null,
        Action<DragTargetDetails<T>>? onAcceptWithDetails = null,
        Action<T?>? onLeave = null,
        Action<DragTargetDetails<T>>? onMove = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Translucent,
        Key? key = null) : base(key)
    {
        if (onWillAccept is not null && onWillAcceptWithDetails is not null)
        {
            throw new ArgumentException("Only one acceptance predicate may be specified.");
        }

        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        OnWillAccept = onWillAccept;
        OnWillAcceptWithDetails = onWillAcceptWithDetails;
        OnAccept = onAccept;
        OnAcceptWithDetails = onAcceptWithDetails;
        OnLeave = onLeave;
        OnMove = onMove;
        HitTestBehavior = hitTestBehavior;
    }

    public DragTargetBuilder<T> Builder { get; }

    [Obsolete("Use OnWillAcceptWithDetails.")]
    public Func<T?, bool>? OnWillAccept { get; }

    public Func<DragTargetDetails<T>, bool>? OnWillAcceptWithDetails { get; }

    [Obsolete("Use OnAcceptWithDetails.")]
    public Action<T>? OnAccept { get; }

    public Action<DragTargetDetails<T>>? OnAcceptWithDetails { get; }

    public Action<T?>? OnLeave { get; }

    public Action<DragTargetDetails<T>>? OnMove { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public override State CreateState() => new DragTargetState<T>();
}

internal interface IDragTargetState
{
    bool IsExpectedDataType(object? data);

    bool DidEnter(IDragAvatar avatar);

    void DidLeave(IDragAvatar avatar);

    void DidDrop(IDragAvatar avatar);

    void DidMove(IDragAvatar avatar);
}

internal sealed class DragTargetState<T> : State, IDragTargetState
{
    private readonly List<IDragAvatar> _candidateAvatars = [];
    private readonly List<IDragAvatar> _rejectedAvatars = [];

    private DragTarget<T> CurrentWidget => (DragTarget<T>)StateWidget;

    public bool IsExpectedDataType(object? data) => data is null || data is T;

    public bool DidEnter(IDragAvatar avatar)
    {
        bool resolvedWillAccept =
            (CurrentWidget.OnWillAccept is null && CurrentWidget.OnWillAcceptWithDetails is null)
            || (CurrentWidget.OnWillAccept is not null
                && CurrentWidget.OnWillAccept((T?)avatar.Data))
            || (CurrentWidget.OnWillAcceptWithDetails is not null
                && avatar.Data is T data
                && CurrentWidget.OnWillAcceptWithDetails(
                    new DragTargetDetails<T>(data, avatar.LastOffset)));

        SetState(() =>
        {
            if (resolvedWillAccept)
            {
                _candidateAvatars.Add(avatar);
            }
            else
            {
                _rejectedAvatars.Add(avatar);
            }
        });
        return resolvedWillAccept;
    }

    public void DidLeave(IDragAvatar avatar)
    {
        if (!Mounted)
        {
            return;
        }

        SetState(() =>
        {
            _candidateAvatars.Remove(avatar);
            _rejectedAvatars.Remove(avatar);
        });
        CurrentWidget.OnLeave?.Invoke((T?)avatar.Data);
    }

    public void DidDrop(IDragAvatar avatar)
    {
        if (!Mounted)
        {
            return;
        }

        SetState(() => _candidateAvatars.Remove(avatar));
        if (avatar.Data is T data)
        {
            CurrentWidget.OnAccept?.Invoke(data);
            CurrentWidget.OnAcceptWithDetails?.Invoke(
                new DragTargetDetails<T>(data, avatar.LastOffset));
        }
    }

    public void DidMove(IDragAvatar avatar)
    {
        if (!Mounted || avatar.Data is not T data)
        {
            return;
        }

        CurrentWidget.OnMove?.Invoke(new DragTargetDetails<T>(data, avatar.LastOffset));
    }

    public override Widget Build(BuildContext context)
    {
        IReadOnlyList<T?> candidates = _candidateAvatars
            .Select(avatar => (T?)avatar.Data)
            .ToArray();
        IReadOnlyList<object?> rejected = _rejectedAvatars
            .Select(avatar => avatar.Data)
            .ToArray();
        return new MetaData(
            metaData: this,
            behavior: CurrentWidget.HitTestBehavior,
            child: CurrentWidget.Builder(context, candidates, rejected));
    }
}

#pragma warning restore CS0618

internal interface IDragAvatar
{
    object? Data { get; }

    Point LastOffset { get; }

    void Update(DragUpdateDetails details);

    void End(DragEndDetails details);

    void Cancel();
}

internal sealed class DragAvatar<T> : IDragAvatar
{
    private readonly OverlayState _overlayState;
    private readonly Axis? _axis;
    private readonly Point _dragStartPoint;
    private readonly Widget _feedback;
    private readonly Point _feedbackOffset;
    private readonly bool _ignoringFeedbackSemantics;
    private readonly bool _ignoringFeedbackPointer;
    private readonly Action<DragUpdateDetails>? _onDragUpdate;
    private readonly Action<Velocity, Point, bool>? _onDragEnd;
    private readonly List<IDragTargetState> _enteredTargets = [];
    private readonly OverlayEntry _entry;
    private IDragTargetState? _activeTarget;
    private Point _position;
    private Point _overlayOffset;
    private bool _finished;

    public DragAvatar(
        OverlayState overlayState,
        T? data,
        Axis? axis,
        Point initialPosition,
        Point dragStartPoint,
        Widget feedback,
        Point feedbackOffset,
        bool ignoringFeedbackSemantics,
        bool ignoringFeedbackPointer,
        Action<DragUpdateDetails>? onDragUpdate,
        Action<Velocity, Point, bool>? onDragEnd)
    {
        _overlayState = overlayState;
        Data = data;
        _axis = axis;
        _position = initialPosition;
        _dragStartPoint = dragStartPoint;
        _feedback = feedback;
        _feedbackOffset = feedbackOffset;
        _ignoringFeedbackSemantics = ignoringFeedbackSemantics;
        _ignoringFeedbackPointer = ignoringFeedbackPointer;
        _onDragUpdate = onDragUpdate;
        _onDragEnd = onDragEnd;
        _entry = new OverlayEntry(BuildFeedback);
        _overlayState.Insert(_entry);
        UpdateDrag(initialPosition);
    }

    public object? Data { get; }

    public Point LastOffset { get; private set; }

    public void Update(DragUpdateDetails details)
    {
        Point oldPosition = _position;
        Point restrictedDelta = RestrictAxis(details.Delta);
        _position += new Vector(restrictedDelta.X, restrictedDelta.Y);
        UpdateDrag(_position);
        if (_position != oldPosition)
        {
            _onDragUpdate?.Invoke(details);
        }
    }

    public void End(DragEndDetails details)
    {
        FinishDrag(dropped: true, RestrictVelocityAxis(details.Velocity));
    }

    public void Cancel()
    {
        FinishDrag(dropped: false, Velocity.Zero);
    }

    private void UpdateDrag(Point globalPosition)
    {
        LastOffset = globalPosition - new Vector(_dragStartPoint.X, _dragStartPoint.Y);
        if (_overlayState.Context.FindRenderObject() is RenderBox overlayBox
            && overlayBox.TryGetTransformFromRoot(out Matrix overlayToRoot)
            && overlayToRoot.TryInvert(out Matrix rootToOverlay))
        {
            Point overlayPosition = rootToOverlay.Transform(globalPosition);
            _overlayOffset = overlayPosition - new Vector(_dragStartPoint.X, _dragStartPoint.Y);
            _entry.MarkNeedsBuild();
        }

        RenderObject? overlayRenderObject = _overlayState.Context.FindRenderObject();
        RenderObject? root = overlayRenderObject?.Owner?.Root;
        if (root is not RenderBox rootBox)
        {
            return;
        }

        var result = new BoxHitTestResult();
        Point targetPosition = globalPosition + new Vector(_feedbackOffset.X, _feedbackOffset.Y);
        rootBox.HitTest(result, targetPosition);
        List<IDragTargetState> targets = GetDragTargets(result.Path);

        bool listsMatch = targets.Count >= _enteredTargets.Count && _enteredTargets.Count > 0;
        if (listsMatch)
        {
            for (int index = 0; index < _enteredTargets.Count; index++)
            {
                if (!ReferenceEquals(targets[index], _enteredTargets[index]))
                {
                    listsMatch = false;
                    break;
                }
            }
        }

        if (listsMatch)
        {
            foreach (IDragTargetState target in _enteredTargets)
            {
                target.DidMove(this);
            }

            return;
        }

        LeaveAllEntered();
        IDragTargetState? newTarget = null;
        foreach (IDragTargetState target in targets)
        {
            _enteredTargets.Add(target);
            if (target.DidEnter(this))
            {
                newTarget = target;
                break;
            }
        }

        foreach (IDragTargetState target in _enteredTargets)
        {
            target.DidMove(this);
        }

        _activeTarget = newTarget;
    }

    private List<IDragTargetState> GetDragTargets(IReadOnlyList<HitTestEntry> path)
    {
        var targets = new List<IDragTargetState>();
        foreach (HitTestEntry entry in path)
        {
            if (entry.Target is RenderMetaData { MetaData: IDragTargetState target }
                && target.IsExpectedDataType(Data))
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    private void LeaveAllEntered()
    {
        foreach (IDragTargetState target in _enteredTargets)
        {
            target.DidLeave(this);
        }

        _enteredTargets.Clear();
    }

    private void FinishDrag(bool dropped, Velocity velocity)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        bool wasAccepted = dropped && _activeTarget is not null;
        if (wasAccepted)
        {
            _activeTarget!.DidDrop(this);
            _enteredTargets.Remove(_activeTarget);
        }

        LeaveAllEntered();
        _activeTarget = null;
        _entry.Remove();
        _entry.Dispose();
        _onDragEnd?.Invoke(velocity, LastOffset, wasAccepted);
    }

    private Widget BuildFeedback(BuildContext context)
    {
        _ = context;
        return new Positioned(
            left: _overlayOffset.X,
            top: _overlayOffset.Y,
            child: new ExcludeSemantics(
                excluding: _ignoringFeedbackSemantics,
                child: new IgnorePointer(
                    ignoring: _ignoringFeedbackPointer,
                    child: _feedback)));
    }

    private Velocity RestrictVelocityAxis(Velocity velocity)
    {
        Vector pixelsPerSecond = velocity.PixelsPerSecond;
        return _axis switch
        {
            Axis.Horizontal => new Velocity(new Vector(pixelsPerSecond.X, 0.0)),
            Axis.Vertical => new Velocity(new Vector(0.0, pixelsPerSecond.Y)),
            _ => velocity,
        };
    }

    private Point RestrictAxis(Point offset)
    {
        return _axis switch
        {
            Axis.Horizontal => new Point(offset.X, 0.0),
            Axis.Vertical => new Point(0.0, offset.Y),
            _ => offset,
        };
    }
}

internal class DraggableGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private readonly Axis? _affinity;
    private readonly Func<PointerButtons, bool>? _allowedButtonsFilter;
    private readonly Func<Point, IDragAvatar?> _onStart;
    private readonly TimeSpan? _delay;
    private readonly Action? _onAvatarStarted;
    private readonly Dictionary<int, DragTracker> _trackers = [];

    public DraggableGestureRecognizer(
        Axis? affinity,
        Func<PointerButtons, bool>? allowedButtonsFilter,
        Func<Point, IDragAvatar?> onStart,
        TimeSpan? delay = null,
        Action? onAvatarStarted = null)
    {
        _affinity = affinity;
        _allowedButtonsFilter = allowedButtonsFilter;
        _onStart = onStart;
        _delay = delay;
        _onAvatarStarted = onAvatarStarted;
    }

    public override void AddPointer(PointerDownEvent @event)
    {
        bool buttonsAllowed = _allowedButtonsFilter?.Invoke(@event.Buttons)
                              ?? @event.Buttons == PointerButtons.Primary;
        if (_trackers.ContainsKey(@event.Pointer)
            || !buttonsAllowed)
        {
            return;
        }

        var entry = GestureArena.Add(@event.Pointer, this);
        _trackers[@event.Pointer] = new DragTracker(
            @event.Position,
            @event.TimestampUtc,
            entry);
        StartTrackingPointer(@event.Pointer);
        if (_delay.HasValue)
        {
            StartDelayTimer(@event.Pointer, _trackers[@event.Pointer]);
        }
        else if (_affinity is null)
        {
            entry.Resolve(GestureDisposition.Accepted);
        }
    }

    public void AcceptGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out DragTracker? tracker))
        {
            return;
        }

        tracker.Accepted = true;
        if (!_delay.HasValue || tracker.DelayPassed)
        {
            StartAvatar(pointer, tracker);
        }
    }

    public void RejectGesture(int pointer)
    {
        Cleanup(pointer);
    }

    public override void Dispose()
    {
        foreach ((int pointer, DragTracker tracker) in _trackers.ToArray())
        {
            tracker.Avatar?.Cancel();
            Cleanup(pointer);
        }

        base.Dispose();
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (!_trackers.TryGetValue(@event.Pointer, out DragTracker? tracker))
        {
            return;
        }

        switch (@event)
        {
            case PointerMoveEvent move:
                HandleMove(move, tracker);
                break;
            case PointerUpEvent up:
                tracker.RecordPosition(up.Position, up.TimestampUtc);
                if (tracker.Avatar is not null)
                {
                    tracker.Avatar.End(
                        new DragEndDetails(
                            new Velocity(tracker.EstimateVelocity()),
                            PrimaryValue(tracker.EstimateVelocity())));
                }
                else
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                }

                Cleanup(up.Pointer);
                break;
            case PointerCancelEvent cancel:
                tracker.Avatar?.Cancel();
                tracker.Entry.Resolve(GestureDisposition.Rejected);
                Cleanup(cancel.Pointer);
                break;
        }
    }

    private void HandleMove(PointerMoveEvent move, DragTracker tracker)
    {
        if (_delay.HasValue
            && !tracker.DelayPassed
            && Distance(tracker.InitialPosition, move.Position) > TouchSlop)
        {
            tracker.Entry.Resolve(GestureDisposition.Rejected);
            Cleanup(move.Pointer);
            return;
        }

        Point totalDelta = move.Position - new Vector(
            tracker.InitialPosition.X,
            tracker.InitialPosition.Y);
        if (!tracker.Accepted && _affinity.HasValue)
        {
            double primary = Math.Abs(PrimaryValue(totalDelta));
            double cross = Math.Abs(CrossValue(totalDelta));
            if (primary > TouchSlop && primary > cross)
            {
                tracker.Entry.Resolve(GestureDisposition.Accepted);
            }
            else if (cross > TouchSlop && cross > primary)
            {
                tracker.Entry.Resolve(GestureDisposition.Rejected);
                Cleanup(move.Pointer);
                return;
            }
        }

        if (tracker.Avatar is not null)
        {
            Point delta = move.Position - new Vector(
                tracker.LastPosition.X,
                tracker.LastPosition.Y);
            tracker.Avatar.Update(
                new DragUpdateDetails(
                    GlobalPosition: move.Position,
                    LocalPosition: move.LocalPosition,
                    Delta: delta,
                    PrimaryDelta: PrimaryValue(delta)));
        }

        tracker.RecordPosition(move.Position, move.TimestampUtc);
    }

    private double PrimaryValue(Point offset)
    {
        return _affinity == Axis.Vertical ? offset.Y : offset.X;
    }

    private double PrimaryValue(Vector offset)
    {
        return _affinity == Axis.Vertical ? offset.Y : offset.X;
    }

    private double CrossValue(Point offset)
    {
        return _affinity == Axis.Vertical ? offset.X : offset.Y;
    }

    private void Cleanup(int pointer)
    {
        if (_trackers.TryGetValue(pointer, out DragTracker? tracker))
        {
            tracker.DelayCancellation.Cancel();
            tracker.DelayCancellation.Dispose();
        }

        StopTrackingPointer(pointer);
        _trackers.Remove(pointer);
    }

    private void StartDelayTimer(int pointer, DragTracker tracker)
    {
        TimeSpan delay = _delay!.Value;
        if (delay <= TimeSpan.Zero)
        {
            HandleDelayPassed(pointer, tracker);
            return;
        }

        CancellationToken token = tracker.DelayCancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() => HandleDelayPassed(pointer, tracker));
        });
    }

    private void HandleDelayPassed(int pointer, DragTracker tracker)
    {
        if (!_trackers.TryGetValue(pointer, out DragTracker? activeTracker)
            || !ReferenceEquals(activeTracker, tracker))
        {
            return;
        }

        tracker.DelayPassed = true;
        if (tracker.Accepted)
        {
            StartAvatar(pointer, tracker);
        }
        else
        {
            tracker.Entry.Resolve(GestureDisposition.Accepted);
        }
    }

    private void StartAvatar(int pointer, DragTracker tracker)
    {
        if (tracker.Avatar is not null)
        {
            return;
        }

        tracker.Avatar = _onStart(tracker.InitialPosition);
        if (tracker.Avatar is null)
        {
            Cleanup(pointer);
            return;
        }

        _onAvatarStarted?.Invoke();
    }

    private static double Distance(Point first, Point second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class DragTracker
    {
        private readonly List<VelocitySample> _samples = [];

        public DragTracker(
            Point initialPosition,
            DateTime timestampUtc,
            GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            LastPosition = initialPosition;
            Entry = entry;
            _samples.Add(new VelocitySample(initialPosition, timestampUtc));
        }

        public Point InitialPosition { get; }

        public Point LastPosition { get; private set; }

        public GestureArenaEntry Entry { get; }

        public bool Accepted { get; set; }

        public IDragAvatar? Avatar { get; set; }

        public CancellationTokenSource DelayCancellation { get; } = new();

        public bool DelayPassed { get; set; }

        public void RecordPosition(Point position, DateTime timestampUtc)
        {
            LastPosition = position;
            if (_samples.Count > 0 && timestampUtc <= _samples[^1].TimestampUtc)
            {
                _samples[^1] = new VelocitySample(position, timestampUtc);
            }
            else
            {
                _samples.Add(new VelocitySample(position, timestampUtc));
            }

            const int maxVelocitySamples = 4;
            if (_samples.Count > maxVelocitySamples)
            {
                _samples.RemoveRange(0, _samples.Count - maxVelocitySamples);
            }
        }

        public Vector EstimateVelocity()
        {
            if (_samples.Count < 2)
            {
                return default;
            }

            VelocitySample newest = _samples[^1];
            for (int index = _samples.Count - 2; index >= 0; index--)
            {
                VelocitySample older = _samples[index];
                double elapsedSeconds = (newest.TimestampUtc - older.TimestampUtc).TotalSeconds;
                if (elapsedSeconds <= 0.0)
                {
                    continue;
                }

                Vector delta = newest.Position - older.Position;
                return new Vector(
                    delta.X / elapsedSeconds,
                    delta.Y / elapsedSeconds);
            }

            return default;
        }
    }

    private readonly record struct VelocitySample(Point Position, DateTime TimestampUtc);
}
