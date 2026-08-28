using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/recognizer.dart
// Structure delta (see docs/ai/DIVERGENCES.md): Dart keeps `_entries`, `_trackedPointers`, `team`,
// `startTrackingPointer`, `stopTrackingPointer` and `_addPointerToArena` on
// `OneSequenceGestureRecognizer` and lets `MultiDragGestureRecognizer`, `DoubleTapGestureRecognizer`,
// `MultiTapGestureRecognizer` and `SerialTapGestureRecognizer` hand-roll the same routing. Plumix
// hoists them to `GestureRecognizer` so those four share one implementation; behavior is identical.

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

public abstract class GestureRecognizer : Diagnosticable, IDisposable
{
    private readonly HashSet<int> _trackedPointers = [];
    private readonly Dictionary<int, (PointerDeviceKind Kind, PointerButtons Buttons)> _pointerToEventData = [];
    private readonly PointerRoute _route;
    private GestureArenaTeam? _team;

    protected GestureRecognizer(GestureBinding? binding = null)
    {
        Binding = binding ?? GestureBinding.Instance;
        _route = HandleRoutedEvent;
    }

    protected GestureBinding Binding { get; }

    protected PointerRouter PointerRouter => Binding.PointerRouter;

    protected GestureArenaManager GestureArena => Binding.GestureArena;

    /// <summary>The recognizer's owner, used only for diagnostics.</summary>
    public object? DebugOwner { get; set; }

    /// <summary>Device kinds this recognizer accepts; null accepts every kind.</summary>
    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; set; }

    /// <summary>Host-supplied gesture tuning that overrides framework defaults such as touch slop.</summary>
    public DeviceGestureSettings? GestureSettings { get; set; }

    /// <summary>The arena team this recognizer competes through, when one is assigned.</summary>
    public GestureArenaTeam? Team
    {
        get => _team;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_team is not null || HasTrackedPointers)
            {
                throw new InvalidOperationException(
                    "A gesture recognizer's team can only be assigned once before it tracks pointers.");
            }

            _team = value;
        }
    }

    /// <summary>Dart's `allowedButtonsFilter`; the default accepts every button combination.</summary>
    public AllowedButtonsFilter AllowedButtonsFilter { get; set; } = DefaultButtonAcceptBehavior;

    /// <summary>A short description used by diagnostics.</summary>
    public virtual string DebugDescription => GetType().Name;

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

    /// <summary>The device kind recorded when the given pointer went down.</summary>
    protected PointerDeviceKind GetKindForPointer(int pointer)
    {
        return _pointerToEventData.TryGetValue(pointer, out var data) ? data.Kind : PointerDeviceKind.Unknown;
    }

    /// <summary>The buttons recorded when the given pointer went down.</summary>
    protected PointerButtons GetButtonsForPointer(int pointer)
    {
        return _pointerToEventData.TryGetValue(pointer, out var data) ? data.Buttons : PointerButtons.None;
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
            if (GestureDebug.PrintRecognizerCallbacksTrace)
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
                library: "gesture",
                context: new ErrorDescription("while handling a gesture"),
                informationCollector: () =>
                [
                    new StringProperty("Handler", name),
                    new DiagnosticsProperty<GestureRecognizer>(
                        "Recognizer",
                        this,
                        style: DiagnosticsTreeStyle.ErrorProperty)
                ]));
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
        foreach (int pointer in _trackedPointers.ToArray())
        {
            PointerRouter.RemoveRoute(pointer, _route);
        }

        _trackedPointers.Clear();
        _pointerToEventData.Clear();
    }

    protected virtual void StartTrackingPointer(int pointer, Matrix4? transform = null)
    {
        if (_trackedPointers.Add(pointer))
        {
            PointerRouter.AddRoute(pointer, _route, transform);
        }
    }

    protected virtual void StopTrackingPointer(int pointer)
    {
        if (_trackedPointers.Remove(pointer))
        {
            PointerRouter.RemoveRoute(pointer, _route);
        }
    }

    protected bool IsTrackingPointer(int pointer)
    {
        return _trackedPointers.Contains(pointer);
    }

    /// <summary>Whether any pointer is currently routed to this recognizer.</summary>
    protected bool HasTrackedPointers => _trackedPointers.Count > 0;

    /// <summary>Adds this recognizer, or its team, to the arena for <paramref name="pointer"/>.</summary>
    protected GestureArenaEntry AddPointerToArena(int pointer, IGestureArenaMember member)
    {
        return _team?.Add(pointer, member) ?? GestureArena.Add(pointer, member);
    }

    protected abstract void HandleEvent(PointerEvent @event);

    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons) => true;

    private void HandleRoutedEvent(PointerEvent @event)
    {
        if (!IsTrackingPointer(@event.Pointer))
        {
            return;
        }

        HandleEvent(@event);
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
}

/// <summary>
/// A recognizer that tracks a single sequence of pointer events: it owns one arena entry per pointer
/// and learns when the last of them stops being tracked.
/// </summary>
public abstract class OneSequenceGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private readonly Dictionary<int, GestureArenaEntry> _entries = [];

    protected OneSequenceGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    /// <summary>Called when this recognizer wins the arena for the given pointer.</summary>
    public virtual void AcceptGesture(int pointer)
    {
    }

    /// <summary>Called when this recognizer loses the arena for the given pointer.</summary>
    public virtual void RejectGesture(int pointer)
    {
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        StartTrackingPointer(@event.Pointer);
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
    protected void ResolvePointer(int pointer, GestureDisposition disposition)
    {
        if (_entries.Remove(pointer, out GestureArenaEntry entry))
        {
            entry.Resolve(disposition);
        }
    }

    protected override void StartTrackingPointer(int pointer, Matrix4? transform = null)
    {
        base.StartTrackingPointer(pointer, transform);
        // A reused pointer id starts a fresh arena, so the entry is always replaced.
        _entries[pointer] = AddPointerToArena(pointer, this);
    }

    protected override void StopTrackingPointer(int pointer)
    {
        bool wasTracking = IsTrackingPointer(pointer);
        base.StopTrackingPointer(pointer);
        if (wasTracking && !HasTrackedPointers)
        {
            DidStopTrackingLastPointer(pointer);
        }
    }

    /// <summary>Stops tracking the pointer once its sequence has ended.</summary>
    protected void StopTrackingIfPointerNoLongerDown(PointerEvent @event)
    {
        if (@event is PointerUpEvent or PointerCancelEvent)
        {
            StopTrackingPointer(@event.Pointer);
        }
    }

    public override void Dispose()
    {
        Resolve(GestureDisposition.Rejected);
        base.Dispose();
        _entries.Clear();
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
        GestureBinding? binding = null) : base(binding)
    {
        if (preAcceptSlopTolerance is { } pre && pre != UnsetTouchSlop && pre < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(preAcceptSlopTolerance),
                "The preAcceptSlopTolerance must be unspecified, positive, or null.");
        }

        if (postAcceptSlopTolerance is { } post && post != UnsetTouchSlop && post < 0.0)
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
        if (Deadline is not null)
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
