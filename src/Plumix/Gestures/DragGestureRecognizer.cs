using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/monodrag.dart

namespace Plumix.Gestures;

/// <summary>The lifecycle of a <see cref="DragGestureRecognizer"/>. Dart's private `_DragState`.</summary>
internal enum DragState
{
    /// <summary>The recognizer is ready to start recognizing a drag.</summary>
    Ready,

    /// <summary>The sequence of pointer events seen so far is consistent with a drag.</summary>
    Possible,

    /// <summary>The sequence of pointer events has been accepted as a drag.</summary>
    Accepted
}

/// <summary>The axis a one-dimensional drag recognizer works on. Dart's private `_DragDirection`.</summary>
internal enum DragDirection
{
    Horizontal,
    Vertical
}

/// <summary>
/// Recognizes movement. Ports Dart's `DragGestureRecognizer`; the three concrete axes are
/// <see cref="VerticalDragGestureRecognizer"/>, <see cref="HorizontalDragGestureRecognizer"/> and
/// <see cref="PanGestureRecognizer"/>.
/// </summary>
public abstract class DragGestureRecognizer : OneSequenceGestureRecognizer
{
    // gestures/constants.dart: kMinFlingVelocity, kMaxFlingVelocity.
    public const double KMinFlingVelocity = GestureConstants.MinFlingVelocity;
    public const double KMaxFlingVelocity = GestureConstants.MaxFlingVelocity;

    private readonly Dictionary<int, VelocityTracker> _velocityTrackers = [];

    /// <summary>Per-pointer local delta accumulated inside the current frame, Dart's `_moveDeltaBeforeFrame`.</summary>
    private readonly Dictionary<int, Point> _moveDeltaBeforeFrame = [];

    /// <summary>Pointers this recognizer has been accepted for, in the order they were accepted.</summary>
    private readonly List<int> _acceptedActivePointers = [];

    private DragState _state = DragState.Ready;
    private OffsetPair _initialPosition;
    private OffsetPair _pendingDragOffset;
    private OffsetPair _lastPosition;
    private DateTime? _lastPendingEventTimestamp;
    private PointerButtons? _initialButtons;
    private Matrix4? _lastTransform;
    private double _globalDistanceMoved;
    private bool _hasDragThresholdBeenMet;
    private TimeSpan? _frameTimeStamp;
    private Point _lastUpdatedDeltaForPan;
    private int? _activePointer;

    protected DragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
        AllowedButtonsFilter = DefaultButtonAcceptBehavior;
    }

    /// <summary>Dart's `_defaultBuilder`: a plain velocity tracker for the event's device kind.</summary>
    public static GestureVelocityTrackerBuilder DefaultVelocityTrackerBuilder { get; } =
        @event => new VelocityTracker(@event.Kind);

    /// <summary>Called when a pointer contacts the screen and might begin a drag.</summary>
    public Action<DragDownDetails>? OnDown { get; set; }

    /// <summary>Called when the pointer has contacted the screen and has begun to move.</summary>
    public Action<DragStartDetails>? OnStart { get; set; }

    /// <summary>Called when a pointer that is moving has moved again.</summary>
    public Action<DragUpdateDetails>? OnUpdate { get; set; }

    /// <summary>Called when a pointer that was moving is no longer in contact with the screen.</summary>
    public Action<DragEndDetails>? OnEnd { get; set; }

    /// <summary>Called when a pointer that was contacting the screen did not end up dragging.</summary>
    public Action? OnCancel { get; set; }

    /// <summary>Configures which point the drag start is reported from.</summary>
    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Start;

    /// <summary>How several simultaneously active pointers are combined into one drag offset.</summary>
    public MultitouchDragStrategy MultitouchDragStrategy { get; set; } = MultitouchDragStrategy.LatestPointer;

    /// <summary>
    /// Whether the drag callbacks are withheld until the drag distance threshold is met, even after
    /// this recognizer has already won the arena.
    /// </summary>
    public bool OnlyAcceptDragOnThreshold { get; set; }

    /// <summary>Builds the velocity tracker used for each pointer.</summary>
    public GestureVelocityTrackerBuilder VelocityTrackerBuilder { get; set; } =
        DefaultVelocityTrackerBuilder;

    /// <summary>
    /// The minimum distance an input pointer drag must have moved to be considered a fling gesture.
    /// Null falls back to the device's hit slop.
    /// </summary>
    public double? MinFlingDistance { get; set; }

    /// <summary>
    /// The minimum velocity for an input pointer drag to be considered a fling gesture. Null falls
    /// back to <see cref="KMinFlingVelocity"/>.
    /// </summary>
    public double? MinFlingVelocity { get; set; }

    /// <summary>
    /// Fling velocity magnitudes are clamped to this value. Null falls back to
    /// <see cref="KMaxFlingVelocity"/>.
    /// </summary>
    public double? MaxFlingVelocity { get; set; }

    /// <summary>The last global/local position pair this recognizer saw. Dart's `lastPosition`.</summary>
    public OffsetPair LastPosition => _lastPosition;

    /// <summary>
    /// The signed global-space distance accumulated along the primary axis since the pointer went
    /// down. Dart's `globalDistanceMoved`.
    /// </summary>
    public double GlobalDistanceMoved => _globalDistanceMoved;

    /// <summary>Dart's `debugLastPendingEventTimestamp`, exposed for tests.</summary>
    internal DateTime? DebugLastPendingEventTimestamp => _lastPendingEventTimestamp;

    /// <summary>Whether the given velocity estimate is fast and far enough to be a fling.</summary>
    public abstract bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind);

    /// <summary>
    /// The end details for a fling with the given estimate, or null when the gesture is not a fling.
    /// </summary>
    public abstract DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind);

    /// <summary>
    /// Whether <see cref="GlobalDistanceMoved"/> is far enough for this recognizer to claim the
    /// arena. Dart passes `gestureSettings?.touchSlop` even though every built-in override re-reads
    /// <see cref="GestureRecognizer.GestureSettings"/> itself.
    /// </summary>
    public abstract bool HasSufficientGlobalDistanceToAccept(
        PointerDeviceKind pointerDeviceKind,
        double? deviceTouchSlop);

    /// <summary>Projects a delta onto the recognizer's axis. Dart's `_getDeltaForDetails`.</summary>
    protected abstract Point GetDeltaForDetails(Point delta);

    /// <summary>The primary-axis component of an offset, or null for a free-axis recognizer.</summary>
    protected abstract double? GetPrimaryValueFromOffset(Point value);

    /// <summary>The axis this recognizer drags along, or null for a free-axis recognizer.</summary>
    internal virtual DragDirection? GetPrimaryDragAxis() => null;

    /// <summary>The fling velocity floor: the recognizer's own value, or the framework default.</summary>
    protected double EffectiveMinFlingVelocity => MinFlingVelocity ?? KMinFlingVelocity;

    /// <summary>The fling velocity ceiling: the recognizer's own value, or the framework default.</summary>
    protected double EffectiveMaxFlingVelocity => MaxFlingVelocity ?? KMaxFlingVelocity;

    /// <summary>The fling distance floor: the recognizer's own value, or the device's hit slop.</summary>
    protected double EffectiveMinFlingDistance(PointerDeviceKind kind)
    {
        return MinFlingDistance ?? PointerEventUtils.ComputeHitSlop(kind, GestureSettings);
    }

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        if (_initialButtons is null)
        {
            if (OnDown is null && OnStart is null && OnUpdate is null && OnEnd is null && OnCancel is null)
            {
                return false;
            }
        }
        else if (@event.Buttons != _initialButtons)
        {
            return false;
        }

        return base.IsPointerAllowed(@event);
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        if (_state == DragState.Ready)
        {
            _initialButtons = @event.Buttons;
        }

        AddPointerInternal(@event);
    }

    /// <summary>
    /// Joins a trackpad pan/zoom gesture. Dart's `addAllowedPointerPanZoom` fixes the initial
    /// buttons to the primary button, because a trackpad gesture carries none of its own and every
    /// later `buttons` comparison would otherwise reject it.
    /// </summary>
    protected override void AddAllowedPointerPanZoom(PointerPanZoomStartEvent @event)
    {
        base.AddAllowedPointerPanZoom(@event);
        StartTrackingPointer(@event.Pointer, @event.Transform);
        if (_state == DragState.Ready)
        {
            _initialButtons = PointerButtons.Primary;
        }

        AddPointerInternal(@event);
    }

    private void AddPointerInternal(PointerEvent @event)
    {
        _velocityTrackers[@event.Pointer] = VelocityTrackerBuilder(@event);
        switch (_state)
        {
            case DragState.Ready:
                _state = DragState.Possible;
                _initialPosition = new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
                _lastPosition = _initialPosition;
                _pendingDragOffset = OffsetPair.Zero;
                _globalDistanceMoved = 0.0;
                _lastPendingEventTimestamp = @event.TimestampUtc;
                _lastTransform = @event.Transform;
                CheckDown();
                break;
            case DragState.Possible:
                break;
            case DragState.Accepted:
                Resolve(GestureDisposition.Accepted);
                break;
        }
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (_state == DragState.Ready)
        {
            throw new InvalidOperationException("A drag recognizer received an event while it was ready.");
        }

        if (!@event.Synthesized
            && @event is PointerDownEvent or PointerMoveEvent
                or PointerPanZoomStartEvent or PointerPanZoomUpdateEvent)
        {
            // A pan/zoom gesture has a stationary contact position, so its velocity is tracked in
            // pan space: the start contributes the origin and every update its cumulative pan.
            Point trackedPosition = @event switch
            {
                PointerPanZoomStartEvent => default,
                PointerPanZoomUpdateEvent panZoomUpdate => panZoomUpdate.Pan,
                _ => @event.LocalPosition
            };
            VelocityTracker tracker = _velocityTrackers[@event.Pointer];
            tracker.AddPosition(@event.TimestampUtc, trackedPosition);
        }

        if (@event is PointerMoveEvent && @event.Buttons != _initialButtons)
        {
            GiveUpPointer(@event.Pointer);
            return;
        }

        if (@event is PointerMoveEvent or PointerPanZoomUpdateEvent && ShouldTrackMoveEvent(@event.Pointer))
        {
            var panZoom = @event as PointerPanZoomUpdateEvent;
            Point delta = panZoom is null ? @event.Delta : panZoom.PanDelta;
            Point localDelta = panZoom is null ? @event.LocalDelta : panZoom.LocalPanDelta;
            Point position = panZoom is null ? @event.Position : @event.Position + panZoom.Pan;
            Point localPosition = panZoom is null
                ? @event.LocalPosition
                : @event.LocalPosition + panZoom.LocalPan;
            _lastPosition = new OffsetPair(Local: localPosition, Global: position);
            Point resolvedDelta = ResolveLocalDeltaForMultitouch(@event.Pointer, localDelta);

            switch (_state)
            {
                case DragState.Ready:
                case DragState.Possible:
                    _pendingDragOffset += new OffsetPair(Local: localDelta, Global: delta);
                    _lastPendingEventTimestamp = @event.TimestampUtc;
                    _lastTransform = @event.Transform;
                    Point movedLocally = GetDeltaForDetails(localDelta);
                    Matrix4? localToGlobalTransform = @event.Transform is null
                        ? null
                        : Matrix4.TryInvert(@event.Transform);
                    _globalDistanceMoved += PointerEventUtils
                            .TransformDeltaViaPositions(
                                untransformedEndPosition: localPosition,
                                untransformedDelta: movedLocally,
                                transform: localToGlobalTransform)
                            .Distance()
                        * Math.Sign(GetPrimaryValueFromOffset(movedLocally) ?? 1.0);
                    if (HasSufficientGlobalDistanceToAccept(@event.Kind, GestureSettings?.TouchSlop))
                    {
                        _hasDragThresholdBeenMet = true;
                        if (_acceptedActivePointers.Contains(@event.Pointer))
                        {
                            CheckDrag(@event.Pointer);
                        }
                        else
                        {
                            Resolve(GestureDisposition.Accepted);
                        }
                    }

                    break;
                case DragState.Accepted:
                    CheckUpdate(
                        sourceTimeStampUtc: @event.TimestampUtc,
                        delta: GetDeltaForDetails(resolvedDelta),
                        primaryDelta: GetPrimaryValueFromOffset(resolvedDelta),
                        globalPosition: position,
                        localPosition: localPosition,
                        pointer: @event.Pointer);
                    break;
            }

            RecordMoveDeltaForMultitouch(@event.Pointer, localDelta);
        }

        if (@event is PointerUpEvent or PointerCancelEvent or PointerPanZoomEndEvent)
        {
            GiveUpPointer(@event.Pointer);
        }
    }

    private bool ShouldTrackMoveEvent(int pointer)
    {
        return MultitouchDragStrategy switch
        {
            MultitouchDragStrategy.SumAllPointers or MultitouchDragStrategy.AverageBoundaryPointers => true,
            _ => _activePointer is null || pointer == _activePointer
        };
    }

    private void RecordMoveDeltaForMultitouch(int pointer, Point localDelta)
    {
        if (MultitouchDragStrategy != MultitouchDragStrategy.AverageBoundaryPointers)
        {
            return;
        }

        if (_state != DragState.Accepted || localDelta == default)
        {
            return;
        }

        _moveDeltaBeforeFrame[pointer] = _moveDeltaBeforeFrame.TryGetValue(pointer, out Point existing)
            ? existing + localDelta
            : localDelta;
    }

    private double GetSumDelta(int pointer, bool positive, DragDirection axis)
    {
        if (!_moveDeltaBeforeFrame.TryGetValue(pointer, out Point offset))
        {
            return 0.0;
        }

        double value = axis == DragDirection.Vertical ? offset.Y : offset.X;
        return positive ? Math.Max(value, 0.0) : Math.Min(value, 0.0);
    }

    private int? GetMaxSumDeltaPointer(bool positive, DragDirection axis)
    {
        if (_moveDeltaBeforeFrame.Count == 0)
        {
            return null;
        }

        int? ret = null;
        double max = 0.0;
        foreach (int pointer in _moveDeltaBeforeFrame.Keys)
        {
            double sum = GetSumDelta(pointer, positive, axis);
            if (ret is null)
            {
                ret = pointer;
                max = sum;
                continue;
            }

            if (positive ? sum > max : sum < max)
            {
                ret = pointer;
                max = sum;
            }
        }

        return ret;
    }

    private Point ResolveLocalDeltaForMultitouch(int pointer, Point localDelta)
    {
        if (MultitouchDragStrategy != MultitouchDragStrategy.AverageBoundaryPointers)
        {
            if (_frameTimeStamp is not null)
            {
                _moveDeltaBeforeFrame.Clear();
                _frameTimeStamp = null;
                _lastUpdatedDeltaForPan = default;
            }

            return localDelta;
        }

        TimeSpan currentSystemFrameTimeStamp = Scheduler.CurrentFrameTimeStamp;
        if (_frameTimeStamp != currentSystemFrameTimeStamp)
        {
            _moveDeltaBeforeFrame.Clear();
            _lastUpdatedDeltaForPan = default;
            _frameTimeStamp = currentSystemFrameTimeStamp;
        }

        DragDirection? axis = GetPrimaryDragAxis();
        if (_state != DragState.Accepted || localDelta == default
            || (_moveDeltaBeforeFrame.Count == 0 && axis is not null))
        {
            return localDelta;
        }

        double dx;
        double dy;
        switch (axis)
        {
            case DragDirection.Horizontal:
                dx = ResolveDelta(pointer, DragDirection.Horizontal, localDelta);
                dy = 0.0;
                break;
            case DragDirection.Vertical:
                dx = 0.0;
                dy = ResolveDelta(pointer, DragDirection.Vertical, localDelta);
                break;
            default:
                double averageX = ResolveDeltaForPanGesture(DragDirection.Horizontal, localDelta);
                double averageY = ResolveDeltaForPanGesture(DragDirection.Vertical, localDelta);
                var average = new Point(averageX, averageY);
                Point updatedDelta = average - _lastUpdatedDeltaForPan;
                _lastUpdatedDeltaForPan = average;
                dx = updatedDelta.X;
                dy = updatedDelta.Y;
                break;
        }

        return new Point(dx, dy);
    }

    private double ResolveDelta(int pointer, DragDirection axis, Point localDelta)
    {
        bool positive = axis == DragDirection.Horizontal ? localDelta.X > 0 : localDelta.Y > 0;
        double delta = axis == DragDirection.Horizontal ? localDelta.X : localDelta.Y;
        int? maxSumDeltaPointer = GetMaxSumDeltaPointer(positive, axis);
        if (maxSumDeltaPointer == pointer)
        {
            return delta;
        }

        double maxSumDelta = GetSumDelta(maxSumDeltaPointer!.Value, positive, axis);
        double curPointerSumDelta = GetSumDelta(pointer, positive, axis);
        if (positive)
        {
            return curPointerSumDelta + delta > maxSumDelta ? curPointerSumDelta + delta - maxSumDelta : 0.0;
        }

        return curPointerSumDelta + delta < maxSumDelta ? curPointerSumDelta + delta - maxSumDelta : 0.0;
    }

    private double ResolveDeltaForPanGesture(DragDirection axis, Point localDelta)
    {
        double delta = axis == DragDirection.Horizontal ? localDelta.X : localDelta.Y;
        int pointerCount = _acceptedActivePointers.Count;
        double sum = delta;
        foreach (Point offset in _moveDeltaBeforeFrame.Values)
        {
            sum += axis == DragDirection.Horizontal ? offset.X : offset.Y;
        }

        return sum / pointerCount;
    }

    public override void AcceptGesture(int pointer)
    {
        if (_acceptedActivePointers.Contains(pointer))
        {
            return;
        }

        _acceptedActivePointers.Add(pointer);
        _activePointer = pointer;
        if (!OnlyAcceptDragOnThreshold || _hasDragThresholdBeenMet)
        {
            CheckDrag(pointer);
        }
    }

    public override void RejectGesture(int pointer)
    {
        GiveUpPointer(pointer);
    }

    protected override void DidStopTrackingLastPointer(int pointer)
    {
        switch (_state)
        {
            case DragState.Ready:
                break;
            case DragState.Possible:
                Resolve(GestureDisposition.Rejected);
                CheckCancel();
                break;
            case DragState.Accepted:
                CheckEnd(pointer);
                break;
        }

        _hasDragThresholdBeenMet = false;
        _velocityTrackers.Clear();
        _initialButtons = null;
        _state = DragState.Ready;
    }

    private void GiveUpPointer(int pointer)
    {
        StopTrackingPointer(pointer);
        // If we never accepted the pointer, we reject it since we are no longer interested in winning
        // the gesture arena for it.
        if (!_acceptedActivePointers.Remove(pointer))
        {
            ResolvePointer(pointer, GestureDisposition.Rejected);
        }

        _moveDeltaBeforeFrame.Remove(pointer);
        if (_activePointer == pointer)
        {
            _activePointer = _acceptedActivePointers.Count > 0 ? _acceptedActivePointers[0] : null;
        }
    }

    private void CheckDown()
    {
        if (OnDown is null)
        {
            return;
        }

        var details = new DragDownDetails(
            GlobalPosition: _initialPosition.Global,
            LocalPosition: _initialPosition.Local);
        InvokeCallback("onDown", () => OnDown!(details));
    }

    private void CheckDrag(int pointer)
    {
        if (_state == DragState.Accepted)
        {
            return;
        }

        _state = DragState.Accepted;
        OffsetPair delta = _pendingDragOffset;
        DateTime? timestamp = _lastPendingEventTimestamp;
        Matrix4? transform = _lastTransform;
        Point localUpdateDelta;
        switch (DragStartBehavior)
        {
            case DragStartBehavior.Start:
                _initialPosition += delta;
                localUpdateDelta = default;
                break;
            default:
                localUpdateDelta = GetDeltaForDetails(delta.Local);
                break;
        }

        _pendingDragOffset = OffsetPair.Zero;
        _lastPendingEventTimestamp = null;
        _lastTransform = null;
        CheckStart(timestamp, pointer);
        if (localUpdateDelta != default && OnUpdate is not null)
        {
            Matrix4? localToGlobal = transform is not null ? Matrix4.TryInvert(transform) : null;
            Point correctedLocalPosition = _initialPosition.Local + localUpdateDelta;
            Point globalUpdateDelta = PointerEventUtils.TransformDeltaViaPositions(
                untransformedEndPosition: correctedLocalPosition,
                untransformedDelta: localUpdateDelta,
                transform: localToGlobal);
            var updateDelta = new OffsetPair(Local: localUpdateDelta, Global: globalUpdateDelta);
            OffsetPair correctedPosition = _initialPosition + updateDelta;
            CheckUpdate(
                sourceTimeStampUtc: timestamp,
                delta: localUpdateDelta,
                primaryDelta: GetPrimaryValueFromOffset(localUpdateDelta),
                globalPosition: correctedPosition.Global,
                localPosition: correctedPosition.Local,
                pointer: pointer);
        }

        Resolve(GestureDisposition.Accepted);
    }

    private void CheckStart(DateTime? timestamp, int pointer)
    {
        if (OnStart is null)
        {
            return;
        }

        var details = new DragStartDetails(
            GlobalPosition: _initialPosition.Global,
            LocalPosition: _initialPosition.Local,
            SourceTimeStampUtc: timestamp,
            Kind: GetKindForPointer(pointer));
        InvokeCallback("onStart", () => OnStart!(details));
    }

    private void CheckUpdate(
        DateTime? sourceTimeStampUtc,
        Point delta,
        double? primaryDelta,
        Point globalPosition,
        Point localPosition,
        int pointer)
    {
        if (OnUpdate is null)
        {
            return;
        }

        var details = new DragUpdateDetails(
            GlobalPosition: globalPosition,
            LocalPosition: localPosition,
            Delta: delta,
            PrimaryDelta: primaryDelta,
            SourceTimeStampUtc: sourceTimeStampUtc,
            Kind: GetKindForPointer(pointer));
        InvokeCallback("onUpdate", () => OnUpdate!(details));
    }

    private void CheckEnd(int pointer)
    {
        if (OnEnd is null)
        {
            return;
        }

        VelocityTracker tracker = _velocityTrackers[pointer];
        VelocityEstimate? estimate = tracker.GetVelocityEstimate();
        DragEndDetails? details = null;
        Func<string> debugReport;
        if (estimate is null)
        {
            debugReport = () => "Could not estimate velocity.";
        }
        else
        {
            details = ConsiderFling(estimate, tracker.Kind);
            debugReport = details is { } fling
                ? () => $"{estimate}; fling at {fling.Velocity}."
                : () => $"{estimate}; judged to not be a fling.";
        }

        DragEndDetails resolved = details ?? new DragEndDetails(
            velocity: Velocity.Zero,
            primaryVelocity: 0.0,
            globalPosition: _lastPosition.Global,
            localPosition: _lastPosition.Local);
        InvokeCallback("onEnd", () => OnEnd!(resolved), debugReport);
    }

    private void CheckCancel()
    {
        if (OnCancel is null)
        {
            return;
        }

        InvokeCallback("onCancel", () => OnCancel!());
    }

    public override void Dispose()
    {
        _velocityTrackers.Clear();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<DragStartBehavior>("start behavior", DragStartBehavior));
    }

    /// <summary>Dart's `_defaultButtonAcceptBehavior`: a drag only competes for the primary button.</summary>
    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons)
    {
        return buttons == PointerButtons.Primary;
    }
}

/// <summary>Recognizes movement in the vertical direction.</summary>
public class VerticalDragGestureRecognizer : DragGestureRecognizer
{
    public VerticalDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "vertical drag";

    public override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        return Math.Abs(estimate.PixelsPerSecond.Y) > EffectiveMinFlingVelocity
               && Math.Abs(estimate.Offset.Y) > EffectiveMinFlingDistance(kind);
    }

    public override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        double maxVelocity = EffectiveMaxFlingVelocity;
        double dy = Math.Clamp(estimate.PixelsPerSecond.Y, -maxVelocity, maxVelocity);
        return new DragEndDetails(
            velocity: new Velocity(new Vector(0.0, dy)),
            primaryVelocity: dy,
            globalPosition: LastPosition.Global,
            localPosition: LastPosition.Local);
    }

    public override bool HasSufficientGlobalDistanceToAccept(
        PointerDeviceKind pointerDeviceKind,
        double? deviceTouchSlop)
    {
        return Math.Abs(GlobalDistanceMoved)
               > PointerEventUtils.ComputeHitSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => new(0.0, delta.Y);

    protected override double? GetPrimaryValueFromOffset(Point value) => value.Y;

    internal override DragDirection? GetPrimaryDragAxis() => DragDirection.Vertical;
}

/// <summary>Recognizes movement in the horizontal direction.</summary>
public class HorizontalDragGestureRecognizer : DragGestureRecognizer
{
    public HorizontalDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "horizontal drag";

    public override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        return Math.Abs(estimate.PixelsPerSecond.X) > EffectiveMinFlingVelocity
               && Math.Abs(estimate.Offset.X) > EffectiveMinFlingDistance(kind);
    }

    public override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        double maxVelocity = EffectiveMaxFlingVelocity;
        double dx = Math.Clamp(estimate.PixelsPerSecond.X, -maxVelocity, maxVelocity);
        return new DragEndDetails(
            velocity: new Velocity(new Vector(dx, 0.0)),
            primaryVelocity: dx,
            globalPosition: LastPosition.Global,
            localPosition: LastPosition.Local);
    }

    public override bool HasSufficientGlobalDistanceToAccept(
        PointerDeviceKind pointerDeviceKind,
        double? deviceTouchSlop)
    {
        return Math.Abs(GlobalDistanceMoved)
               > PointerEventUtils.ComputeHitSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => new(delta.X, 0.0);

    protected override double? GetPrimaryValueFromOffset(Point value) => value.X;

    internal override DragDirection? GetPrimaryDragAxis() => DragDirection.Horizontal;
}

/// <summary>Recognizes movement both horizontally and vertically.</summary>
public class PanGestureRecognizer : DragGestureRecognizer
{
    public PanGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "pan";

    public override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        double minVelocity = EffectiveMinFlingVelocity;
        double minDistance = EffectiveMinFlingDistance(kind);
        return estimate.PixelsPerSecond.SquaredLength > minVelocity * minVelocity
               && estimate.Offset.SquaredLength > minDistance * minDistance;
    }

    public override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        Velocity velocity = new Velocity(estimate.PixelsPerSecond)
            .ClampMagnitude(EffectiveMinFlingVelocity, EffectiveMaxFlingVelocity);
        return new DragEndDetails(
            velocity: velocity,
            primaryVelocity: null,
            globalPosition: LastPosition.Global,
            localPosition: LastPosition.Local);
    }

    public override bool HasSufficientGlobalDistanceToAccept(
        PointerDeviceKind pointerDeviceKind,
        double? deviceTouchSlop)
    {
        return Math.Abs(GlobalDistanceMoved)
               > PointerEventUtils.ComputePanSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => delta;

    protected override double? GetPrimaryValueFromOffset(Point value) => null;
}
