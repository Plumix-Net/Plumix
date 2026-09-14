using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/recognizer.dart

namespace Plumix.Gestures;

/// <summary>Dart's `AllowedButtonsFilter`: decides whether a button combination competes.</summary>
public delegate bool AllowedButtonsFilter(PointerButtons buttons);

/// <summary>
/// How a drag recognizer combines the offsets of several simultaneously active pointers.
/// Ports Dart's `MultitouchDragStrategy` (`gestures/recognizer.dart`).
/// </summary>
public enum MultitouchDragStrategy
{
    /// <summary>
    /// Only the latest active pointer is tracked; when it goes up the recognizer falls back to the
    /// first of the remaining accepted pointers. This is the Android behavior and the default.
    /// </summary>
    LatestPointer,

    /// <summary>
    /// Every pointer is tracked and the reported delta is the sum of the maximum delta in each
    /// direction; a pan reports the average of all pointer offsets. This is the iOS behavior.
    /// </summary>
    AverageBoundaryPointers,

    /// <summary>Every pointer is tracked and the reported delta is the plain sum of their offsets.</summary>
    SumAllPointers
}

public abstract class GestureRecognizer : DiagnosticableTree, IGestureArenaMember, IDisposable
{
    private readonly Dictionary<int, (PointerDeviceKind Kind, PointerButtons Buttons)> _pointerToEventData = [];

    protected GestureRecognizer(
        GestureBinding? binding = null,
        object? debugOwner = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        AllowedButtonsFilter? allowedButtonsFilter = null)
    {
        Binding = binding ?? GestureBinding.Instance;
        DebugOwner = debugOwner;
        SupportedDevices = supportedDevices;
        AllowedButtonsFilter = allowedButtonsFilter ?? DefaultButtonAcceptBehavior;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("gestures", "GestureRecognizer", this);
        }
    }

    protected GestureBinding Binding { get; }

    protected PointerRouter PointerRouter => Binding.PointerRouter;

    protected GestureArenaManager GestureArena => Binding.GestureArena;

    /// <summary>The recognizer's owner, used only for diagnostics.</summary>
    public object? DebugOwner { get; init; }

    /// <summary>Device kinds this recognizer accepts; null accepts every kind.</summary>
    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; set; }

    /// <summary>Host-supplied gesture tuning that overrides framework defaults such as touch slop.</summary>
    public DeviceGestureSettings? GestureSettings { get; set; }

    /// <summary>Dart's `allowedButtonsFilter`; the default accepts every button combination.</summary>
    public AllowedButtonsFilter AllowedButtonsFilter { get; init; }

    /// <summary>A short description used by diagnostics.</summary>
    public abstract string DebugDescription { get; }

    /// <summary>Called when this recognizer wins the arena for the given pointer.</summary>
    public abstract void AcceptGesture(int pointer);

    /// <summary>Called when this recognizer loses the arena for the given pointer.</summary>
    public abstract void RejectGesture(int pointer);

    public virtual void AddPointer(PointerDownEvent @event)
    {
        _pointerToEventData[@event.Pointer] = (@event.Kind, @event.Buttons);
        if (IsPointerAllowed(@event))
        {
            AddAllowedPointer(@event);
            return;
        }

        HandleNonAllowedPointer(@event);
    }

    /// <summary>Registers a pointer that passed <see cref="IsPointerAllowed"/>.</summary>
    protected virtual void AddAllowedPointer(PointerDownEvent @event)
    {
    }

    /// <summary>Called for a pointer this recognizer refuses to compete for.</summary>
    protected virtual void HandleNonAllowedPointer(PointerDownEvent @event)
    {
    }

    protected virtual bool IsPointerAllowed(PointerDownEvent @event)
    {
        return (SupportedDevices is null || SupportedDevices.Contains(@event.Kind))
               && AllowedButtonsFilter(@event.Buttons);
    }

    /// <summary>
    /// Registers a trackpad pan/zoom gesture with this recognizer, the pan/zoom counterpart of
    /// <see cref="AddPointer"/>. Ports Dart's `GestureRecognizer.addPointerPanZoom`.
    /// </summary>
    public virtual void AddPointerPanZoom(PointerPanZoomStartEvent @event)
    {
        _pointerToEventData[@event.Pointer] = (@event.Kind, @event.Buttons);
        if (IsPointerPanZoomAllowed(@event))
        {
            AddAllowedPointerPanZoom(@event);
            return;
        }

        HandleNonAllowedPointerPanZoom(@event);
    }

    /// <summary>Registers a pan/zoom gesture that passed <see cref="IsPointerPanZoomAllowed"/>.</summary>
    protected virtual void AddAllowedPointerPanZoom(PointerPanZoomStartEvent @event)
    {
    }

    /// <summary>Called for a pan/zoom gesture this recognizer refuses to compete for.</summary>
    /// <remarks>
    /// Dart's base implementation is a no-op even on `OneSequenceGestureRecognizer`, which does
    /// resolve rejected for a disallowed pointer *down*; a disallowed pan/zoom is simply ignored.
    /// </remarks>
    protected virtual void HandleNonAllowedPointerPanZoom(PointerPanZoomStartEvent @event)
    {
    }

    /// <summary>
    /// Whether this recognizer competes for the given pan/zoom gesture. Unlike
    /// <see cref="IsPointerAllowed"/> this does not consult <see cref="AllowedButtonsFilter"/>: a
    /// trackpad gesture reports no buttons.
    /// </summary>
    protected virtual bool IsPointerPanZoomAllowed(PointerPanZoomStartEvent @event)
    {
        return SupportedDevices is null || SupportedDevices.Contains(@event.Kind);
    }

    /// <summary>The device kind recorded when the given pointer went down.</summary>
    protected PointerDeviceKind GetKindForPointer(int pointer)
    {
        return _pointerToEventData[pointer].Kind;
    }

    /// <summary>The buttons recorded when the given pointer went down.</summary>
    protected PointerButtons GetButtonsForPointer(int pointer)
    {
        return _pointerToEventData[pointer].Buttons;
    }

    /// <summary>
    /// Dart's `invokeCallback`: runs a user callback and, if it throws, reports the error through
    /// <see cref="FlutterError.ReportError"/> and returns the default value instead of propagating.
    /// A recognizer must keep processing the rest of the pointer sequence after a bad callback.
    /// </summary>
    protected T? InvokeCallback<T>(string name, Func<T> callback, Func<string>? debugReport = null)
    {
        T? result = default;
        try
        {
            if (Constants.KDebugMode && GestureDebug.PrintRecognizerCallbacksTrace)
            {
                string? report = debugReport?.Invoke();
                string prefix = GestureDebug.PrintGestureArenaDiagnostics
                    ? new string(' ', 19) + "\u2759 "
                    : string.Empty;
                string suffix = string.IsNullOrEmpty(report) ? string.Empty : $" {report}";
                GestureDebug.Log($"{prefix}{this} calling {name} callback.{suffix}");
            }

            result = callback();
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "gesture",
                context: new ErrorDescription("while handling a gesture"),
                informationCollector: Constants.KDebugMode ? () =>
                [
                    new StringProperty("Handler", name),
                    new DiagnosticsProperty<GestureRecognizer>(
                        "Recognizer",
                        this,
                        style: DiagnosticsTreeStyle.ErrorProperty)
                ] : null));
        }

        return result;
    }

    /// <summary>Dart's `invokeCallback` for callbacks with no return value.</summary>
    protected void InvokeCallback(string name, Action callback, Func<string>? debugReport = null)
    {
        InvokeCallback<object?>(
            name,
            () =>
            {
                callback();
                return null;
            },
            debugReport);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<object>("debugOwner", DebugOwner, defaultValue: null));
    }

    public virtual void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }
    }

    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons) => true;
}

/// <summary>
/// A recognizer that tracks a single sequence of pointer events: it owns one arena entry per pointer
/// and learns when the last of them stops being tracked.
/// </summary>
public abstract class OneSequenceGestureRecognizer : GestureRecognizer
{
    private readonly Dictionary<int, GestureArenaEntry> _entries = [];
    private readonly HashSet<int> _trackedPointers = [];
    private GestureArenaTeam? _team;

    protected OneSequenceGestureRecognizer(
        GestureBinding? binding = null,
        object? debugOwner = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        AllowedButtonsFilter? allowedButtonsFilter = null)
        : base(binding, debugOwner, supportedDevices, allowedButtonsFilter)
    {
    }

    /// <summary>Handles events routed to this recognizer's tracked pointers.</summary>
    protected abstract void HandleEvent(PointerEvent @event);

    /// <summary>Called when this recognizer wins the arena for the given pointer.</summary>
    public override void AcceptGesture(int pointer)
    {
    }

    /// <summary>Called when this recognizer loses the arena for the given pointer.</summary>
    public override void RejectGesture(int pointer)
    {
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        StartTrackingPointer(@event.Pointer, @event.Transform);
    }

    protected override void HandleNonAllowedPointer(PointerDownEvent @event)
    {
        Resolve(GestureDisposition.Rejected);
    }

    /// <summary>Called when the recognizer stops tracking its last pointer.</summary>
    protected abstract void DidStopTrackingLastPointer(int pointer);

    /// <summary>
    /// Resolves every pointer this recognizer is competing for. Dart marks this `@protected`, but
    /// that is advisory and Flutter's own Cupertino context menu calls it from outside; C# keeps it
    /// public to allow the same call sites.
    /// </summary>
    public virtual void Resolve(GestureDisposition disposition)
    {
        var localEntries = _entries.Values.ToList();
        _entries.Clear();
        foreach (GestureArenaEntry entry in localEntries)
        {
            entry.Resolve(disposition);
        }
    }

    /// <summary>Resolves a single pointer this recognizer is competing for.</summary>
    protected virtual void ResolvePointer(int pointer, GestureDisposition disposition)
    {
        if (_entries.Remove(pointer, out GestureArenaEntry entry))
        {
            entry.Resolve(disposition);
        }
    }

    /// <summary>The arena team this recognizer competes through, when one is assigned.</summary>
    public GestureArenaTeam? Team
    {
        get => _team;
        set
        {
            if (Constants.KDebugMode
                && (value is null || _entries.Count > 0 || _trackedPointers.Count > 0 || _team is not null))
            {
                throw new InvalidOperationException(
                    "A gesture recognizer's team can only be assigned once before it tracks pointers.");
            }

            _team = value;
        }
    }

    private GestureArenaEntry AddPointerToArena(int pointer)
    {
        return _team?.Add(pointer, this) ?? GestureArena.Add(pointer, this);
    }

    protected virtual void StartTrackingPointer(int pointer, Matrix4? transform = null)
    {
        PointerRouter.AddRoute(pointer, HandleEvent, transform);
        _trackedPointers.Add(pointer);
        // Upstream intentionally permits replacing an old, unresolved entry for a reused pointer id.
        _entries[pointer] = AddPointerToArena(pointer);
    }

    protected virtual void StopTrackingPointer(int pointer)
    {
        if (_trackedPointers.Contains(pointer))
        {
            PointerRouter.RemoveRoute(pointer, HandleEvent);
            _trackedPointers.Remove(pointer);
            if (_trackedPointers.Count == 0)
            {
                DidStopTrackingLastPointer(pointer);
            }
        }
    }

    /// <summary>Stops tracking the pointer once its sequence has ended.</summary>
    protected void StopTrackingIfPointerNoLongerDown(PointerEvent @event)
    {
        if (@event is PointerUpEvent or PointerCancelEvent or PointerPanZoomEndEvent)
        {
            StopTrackingPointer(@event.Pointer);
        }
    }

    public override void Dispose()
    {
        Resolve(GestureDisposition.Rejected);
        foreach (int pointer in _trackedPointers)
        {
            PointerRouter.RemoveRoute(pointer, HandleEvent);
        }

        _trackedPointers.Clear();
        if (Constants.KDebugMode && _entries.Count != 0)
        {
            throw new InvalidOperationException("A disposed recognizer must have no arena entries.");
        }

        base.Dispose();
    }
}

/// <summary>The lifecycle of a <see cref="PrimaryPointerGestureRecognizer"/>.</summary>
public enum GestureRecognizerState
{
    /// <summary>The recognizer is ready to start recognizing a gesture.</summary>
    Ready,

    /// <summary>The sequence of pointer events seen so far is consistent with the gesture.</summary>
    Possible,

    /// <summary>The gesture was rejected; the recognizer waits for the pointer sequence to end.</summary>
    Defunct
}

/// <summary>
/// A recognizer that considers events only from the first pointer that went down while it was
/// ready. Ports Dart's `PrimaryPointerGestureRecognizer` (`gestures/recognizer.dart`).
/// </summary>
public abstract class PrimaryPointerGestureRecognizer : OneSequenceGestureRecognizer
{
    /// <summary>
    /// Dart's `_unsetTouchSlop`: distinguishes "not specified" (fall back to the device touch slop)
    /// from an explicit null (never reject on move).
    /// </summary>
    private protected const double UnsetTouchSlop = -1.0;

    private readonly double? _preAcceptSlopTolerance;
    private readonly double? _postAcceptSlopTolerance;
    private bool _gestureAccepted;
    private GestureTimer? _timer;

    protected PrimaryPointerGestureRecognizer(
        TimeSpan? deadline = null,
        double? preAcceptSlopTolerance = UnsetTouchSlop,
        double? postAcceptSlopTolerance = UnsetTouchSlop,
        GestureBinding? binding = null,
        object? debugOwner = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        AllowedButtonsFilter? allowedButtonsFilter = null)
        : base(binding, debugOwner, supportedDevices, allowedButtonsFilter)
    {
        if (Constants.KDebugMode
            && preAcceptSlopTolerance is { } pre && pre != UnsetTouchSlop && !(pre >= 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preAcceptSlopTolerance),
                "The preAcceptSlopTolerance must be unspecified, positive, or null.");
        }

        if (Constants.KDebugMode
            && postAcceptSlopTolerance is { } post && post != UnsetTouchSlop && !(post >= 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(postAcceptSlopTolerance),
                "The postAcceptSlopTolerance must be unspecified, positive, or null.");
        }

        Deadline = deadline;
        _preAcceptSlopTolerance = preAcceptSlopTolerance;
        _postAcceptSlopTolerance = postAcceptSlopTolerance;
    }

    /// <summary>If non-null, <see cref="DidExceedDeadline"/> fires this long after the down.</summary>
    public TimeSpan? Deadline { get; }

    /// <summary>
    /// The distance the primary pointer may travel before acceptance without the gesture being
    /// rejected. Null means it is never rejected for moving before acceptance. Resolved lazily
    /// against <see cref="GestureRecognizer.GestureSettings"/>, exactly like Dart.
    /// </summary>
    public double? PreAcceptSlopTolerance =>
        _preAcceptSlopTolerance == UnsetTouchSlop ? DefaultTouchSlop : _preAcceptSlopTolerance;

    /// <summary>
    /// The distance the primary pointer may travel after acceptance before the gesture is rejected.
    /// Null means it is never rejected for moving after acceptance.
    /// </summary>
    public double? PostAcceptSlopTolerance =>
        _postAcceptSlopTolerance == UnsetTouchSlop ? DefaultTouchSlop : _postAcceptSlopTolerance;

    private double DefaultTouchSlop => GestureSettings?.TouchSlop ?? GestureConstants.TouchSlop;

    /// <summary>The current lifecycle state of the recognizer.</summary>
    public GestureRecognizerState State { get; private set; } = GestureRecognizerState.Ready;

    /// <summary>
    /// The most recently tracked primary pointer; deliberately retained after tracking stops.
    /// </summary>
    public int? PrimaryPointer { get; private set; }

    /// <summary>Where the primary pointer went down; non-null only while tracking.</summary>
    public OffsetPair? InitialPosition { get; private set; }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        if (State == GestureRecognizerState.Ready)
        {
            State = GestureRecognizerState.Possible;
            PrimaryPointer = @event.Pointer;
            InitialPosition = new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
            if (Deadline is { } deadline)
            {
                _timer = GestureTimer.Start(deadline, () => DidExceedDeadlineWithEvent(@event));
            }
        }
    }

    protected override void HandleNonAllowedPointer(PointerDownEvent @event)
    {
        // A disallowed extra pointer must not reject a gesture that has already been accepted.
        if (!_gestureAccepted)
        {
            base.HandleNonAllowedPointer(@event);
        }
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (Constants.KDebugMode && State == GestureRecognizerState.Ready)
        {
            throw new InvalidOperationException("A primary recognizer cannot handle events while ready.");
        }

        if (State == GestureRecognizerState.Possible && @event.Pointer == PrimaryPointer)
        {
            bool isPreAcceptSlopPastTolerance = !_gestureAccepted
                && PreAcceptSlopTolerance is { } preTolerance
                && GetGlobalDistance(@event) > preTolerance;
            bool isPostAcceptSlopPastTolerance = _gestureAccepted
                && PostAcceptSlopTolerance is { } postTolerance
                && GetGlobalDistance(@event) > postTolerance;

            if (@event is PointerMoveEvent && (isPreAcceptSlopPastTolerance || isPostAcceptSlopPastTolerance))
            {
                Resolve(GestureDisposition.Rejected);
                StopTrackingPointer(PrimaryPointer!.Value);
            }
            else
            {
                HandlePrimaryPointer(@event);
            }
        }

        StopTrackingIfPointerNoLongerDown(@event);
    }

    /// <summary>Override to handle events for the primary pointer while the gesture is possible.</summary>
    protected abstract void HandlePrimaryPointer(PointerEvent @event);

    /// <summary>
    /// Fires when <see cref="Deadline"/> elapses before the gesture resolves. Subclasses that
    /// supply a deadline must override this or <see cref="DidExceedDeadlineWithEvent"/>.
    /// </summary>
    protected virtual void DidExceedDeadline()
    {
        if (Constants.KDebugMode && Deadline is not null)
        {
            throw new InvalidOperationException(
                $"{DebugDescription} supplies a deadline but overrides neither DidExceedDeadline() "
                + "nor DidExceedDeadlineWithEvent().");
        }
    }

    /// <summary>Same as <see cref="DidExceedDeadline"/>, carrying the original down event.</summary>
    protected virtual void DidExceedDeadlineWithEvent(PointerDownEvent @event)
    {
        DidExceedDeadline();
    }

    public override void AcceptGesture(int pointer)
    {
        if (pointer == PrimaryPointer)
        {
            StopTimer();
            _gestureAccepted = true;
        }
    }

    public override void RejectGesture(int pointer)
    {
        if (pointer == PrimaryPointer && State == GestureRecognizerState.Possible)
        {
            StopTimer();
            State = GestureRecognizerState.Defunct;
        }
    }

    protected override void DidStopTrackingLastPointer(int pointer)
    {
        if (Constants.KDebugMode && State == GestureRecognizerState.Ready)
        {
            throw new InvalidOperationException("A primary recognizer must be tracking before it stops.");
        }

        StopTimer();
        State = GestureRecognizerState.Ready;
        InitialPosition = null;
        _gestureAccepted = false;
    }

    public override void Dispose()
    {
        StopTimer();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<GestureRecognizerState>("state", State));
    }

    private void StopTimer()
    {
        _timer?.Cancel();
        _timer = null;
    }

    private double GetGlobalDistance(PointerEvent @event)
    {
        return (@event.Position - InitialPosition!.Value.Global).Distance();
    }
}

/// <summary>
/// A pair of positions for the same point: one in the global (root) coordinate space and one in the
/// receiving render object's local space.
/// </summary>
public readonly record struct OffsetPair(Point Local, Point Global)
{
    public static OffsetPair Zero { get; } = new(default, default);

    /// <summary>The event's position pair.</summary>
    public static OffsetPair FromEventPosition(PointerEvent @event)
    {
        return new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
    }

    /// <summary>The event's delta pair.</summary>
    public static OffsetPair FromEventDelta(PointerEvent @event)
    {
        return new OffsetPair(Local: @event.LocalDelta, Global: @event.Delta);
    }

    public static OffsetPair operator +(OffsetPair left, OffsetPair right)
    {
        return new OffsetPair(Local: left.Local + right.Local, Global: left.Global + right.Global);
    }

    public static OffsetPair operator -(OffsetPair left, OffsetPair right)
    {
        return new OffsetPair(Local: left.Local - right.Local, Global: left.Global - right.Global);
    }

    public override string ToString() => FormattableString.Invariant(
        $"OffsetPair(local: Offset({Local.X:0.0}, {Local.Y:0.0}), global: Offset({Global.X:0.0}, {Global.Y:0.0}))");
}

public readonly record struct DragDownDetails(
    Point GlobalPosition,
    Point LocalPosition = default) : IPositionedGestureDetails;

public readonly record struct DragStartDetails(
    Point GlobalPosition,
    Point LocalPosition = default,
    DateTime? SourceTimeStampUtc = null,
    PointerDeviceKind? Kind = null) : IPositionedGestureDetails;

/// <summary>
/// Details for `GestureDragUpdateCallback`. <paramref name="PrimaryDelta"/> is null when the
/// recognizer has no primary axis (`PanGestureRecognizer`), matching Dart.
/// </summary>
public readonly record struct DragUpdateDetails(
    Point GlobalPosition,
    Point LocalPosition,
    Point Delta,
    double? PrimaryDelta,
    DateTime? SourceTimeStampUtc = null,
    PointerDeviceKind? Kind = null) : IPositionedGestureDetails;

public readonly record struct Velocity(Vector PixelsPerSecond)
{
    public static Velocity Zero { get; } = new(default);

    public static Velocity operator -(Velocity value) => new(-value.PixelsPerSecond);

    public static Velocity operator -(Velocity left, Velocity right)
    {
        return new Velocity(left.PixelsPerSecond - right.PixelsPerSecond);
    }

    public static Velocity operator +(Velocity left, Velocity right)
    {
        return new Velocity(left.PixelsPerSecond + right.PixelsPerSecond);
    }

    public Velocity ClampMagnitude(double minimumValue, double maximumValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumValue);
        if (maximumValue < minimumValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumValue));
        }

        double magnitude = PixelsPerSecond.Length;
        if (magnitude > maximumValue)
        {
            return new Velocity(PixelsPerSecond / magnitude * maximumValue);
        }

        if (magnitude < minimumValue && magnitude > 0.0)
        {
            return new Velocity(PixelsPerSecond / magnitude * minimumValue);
        }

        return this;
    }
}

public readonly record struct DragEndDetails : IPositionedGestureDetails
{
    public DragEndDetails(double primaryVelocity) : this(
        velocity: new Velocity(new Vector(primaryVelocity, 0)),
        primaryVelocity: primaryVelocity)
    {
    }

    public DragEndDetails(
        Velocity velocity,
        double? primaryVelocity,
        Point globalPosition = default,
        Point localPosition = default)
    {
        Velocity = velocity;
        PrimaryVelocity = primaryVelocity;
        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
    }

    public Velocity Velocity { get; }

    /// <summary>
    /// The velocity along the recognizer's primary axis, or null when the recognizer has no primary
    /// axis (`PanGestureRecognizer`). Dart asserts it matches one component of
    /// <see cref="Velocity"/> with the other exactly zero.
    /// </summary>
    public double? PrimaryVelocity { get; }

    /// <summary>The global position the pointer was at when it stopped contacting the screen.</summary>
    public Point GlobalPosition { get; }

    /// <summary>The local position in the receiving object's coordinate system.</summary>
    public Point LocalPosition { get; }
}
