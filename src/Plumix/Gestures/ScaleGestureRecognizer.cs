using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/scale.dart

namespace Plumix.Gestures;

/// <summary>The lifecycle of a <see cref="ScaleGestureRecognizer"/>. Dart's private `_ScaleState`.</summary>
internal enum ScaleState
{
    /// <summary>The recognizer is ready to start recognizing a gesture.</summary>
    Ready,

    /// <summary>
    /// The sequence of pointer events seen so far is consistent with a scale gesture but the
    /// gesture has not been accepted definitively.
    /// </summary>
    Possible,

    /// <summary>The sequence of pointer events seen so far has been accepted as a scale gesture.</summary>
    Accepted,

    /// <summary>
    /// The gesture was accepted and the pointers established a focal point and initial scale.
    /// </summary>
    Started
}

/// <summary>Details for a scale gesture that has just started. Ports Dart's `ScaleStartDetails`.</summary>
public readonly record struct ScaleStartDetails
{
    public ScaleStartDetails(
        Point focalPoint = default,
        Point? localFocalPoint = null,
        int pointerCount = 0,
        DateTime? sourceTimeStampUtc = null,
        PointerDeviceKind? kind = null)
    {
        FocalPoint = focalPoint;
        LocalFocalPoint = localFocalPoint ?? focalPoint;
        PointerCount = pointerCount;
        SourceTimeStampUtc = sourceTimeStampUtc;
        Kind = kind;
    }

    /// <summary>The initial focal point of the pointers in contact with the screen, in global space.</summary>
    public Point FocalPoint { get; }

    /// <summary>
    /// <see cref="FocalPoint"/> in the coordinate space of the event receiver; defaults to
    /// <see cref="FocalPoint"/> when the constructor is not given one.
    /// </summary>
    public Point LocalFocalPoint { get; }

    /// <summary>The number of pointers being tracked by the gesture recognizer.</summary>
    public int PointerCount { get; }

    /// <summary>
    /// The timestamp of the source pointer event that triggered the scale event; null when the
    /// gesture came from a proxied source such as accessibility.
    /// </summary>
    public DateTime? SourceTimeStampUtc { get; }

    /// <summary>
    /// The kind of the device that initiated the event. With several pointers on the screen this is
    /// the kind of the pointer that started the gesture.
    /// </summary>
    public PointerDeviceKind? Kind { get; }
}

/// <summary>Details for a scale gesture that is in progress. Ports Dart's `ScaleUpdateDetails`.</summary>
public readonly record struct ScaleUpdateDetails
{
    public ScaleUpdateDetails(
        Point focalPoint = default,
        Point? localFocalPoint = null,
        double scale = 1.0,
        double horizontalScale = 1.0,
        double verticalScale = 1.0,
        double rotation = 0.0,
        int pointerCount = 0,
        Point focalPointDelta = default,
        DateTime? sourceTimeStampUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scale);
        ArgumentOutOfRangeException.ThrowIfNegative(horizontalScale);
        ArgumentOutOfRangeException.ThrowIfNegative(verticalScale);
        FocalPoint = focalPoint;
        LocalFocalPoint = localFocalPoint ?? focalPoint;
        Scale = scale;
        HorizontalScale = horizontalScale;
        VerticalScale = verticalScale;
        Rotation = rotation;
        PointerCount = pointerCount;
        FocalPointDelta = focalPointDelta;
        SourceTimeStampUtc = sourceTimeStampUtc;
    }

    /// <summary>
    /// How far the focal point moved in the coordinate space of the event receiver since the
    /// previous update.
    /// </summary>
    public Point FocalPointDelta { get; }

    /// <summary>The focal point of the pointers in contact with the screen, in global space.</summary>
    public Point FocalPoint { get; }

    /// <summary><see cref="FocalPoint"/> in the coordinate space of the event receiver.</summary>
    public Point LocalFocalPoint { get; }

    /// <summary>The scale implied by the average distance between the pointers on the screen.</summary>
    public double Scale { get; }

    /// <summary>The scale implied by the average horizontal distance between the pointers.</summary>
    public double HorizontalScale { get; }

    /// <summary>The scale implied by the average vertical distance between the pointers.</summary>
    public double VerticalScale { get; }

    /// <summary>The angle in radians implied by the first two pointers to contact the screen.</summary>
    public double Rotation { get; }

    /// <summary>
    /// The number of pointers being tracked by the gesture recognizer. Because platforms do not
    /// report how many fingers a trackpad gesture uses, a trackpad gesture counts as two.
    /// </summary>
    public int PointerCount { get; }

    /// <summary>
    /// The timestamp of the source pointer event that triggered the scale event; null when the
    /// gesture came from a proxied source such as accessibility.
    /// </summary>
    public DateTime? SourceTimeStampUtc { get; }
}

/// <summary>Details for a scale gesture that has ended. Ports Dart's `ScaleEndDetails`.</summary>
public readonly record struct ScaleEndDetails
{
    public ScaleEndDetails(Velocity velocity = default, double scaleVelocity = 0.0, int pointerCount = 0)
    {
        Velocity = velocity;
        ScaleVelocity = scaleVelocity;
        PointerCount = pointerCount;
    }

    /// <summary>The velocity of the last pointer to be lifted off of the screen.</summary>
    public Velocity Velocity { get; }

    /// <summary>The final velocity of the scale factor reported by the gesture.</summary>
    public double ScaleVelocity { get; }

    /// <summary>The number of pointers being tracked by the gesture recognizer.</summary>
    public int PointerCount { get; }
}

/// <summary>
/// Recognizes a scale gesture: tracks the pointers in contact with the screen and reports their
/// focal point, scale and rotation. Ports Dart's `ScaleGestureRecognizer` (`gestures/scale.dart`).
/// </summary>
public class ScaleGestureRecognizer : OneSequenceGestureRecognizer
{
    /// <summary>Dart's `kDefaultMouseScrollToScaleFactor`: mouse-wheel pixels per unit of scale.</summary>
    public const double KDefaultMouseScrollToScaleFactor = 200.0;

    /// <summary>
    /// Dart's `kDefaultTrackpadScrollToScaleFactor`; matches
    /// <see cref="KDefaultMouseScrollToScaleFactor"/> and the convention that scrolling up zooms in.
    /// </summary>
    public static Point KDefaultTrackpadScrollToScaleFactor { get; } =
        new(0.0, -1.0 / KDefaultMouseScrollToScaleFactor);

    private readonly Dictionary<int, Point> _pointerLocations = [];

    /// <summary>A queue that keeps the pointers in the order they entered. Dart's `_pointerQueue`.</summary>
    private readonly List<int> _pointerQueue = [];

    private readonly Dictionary<int, VelocityTracker> _velocityTrackers = [];
    private readonly Dictionary<int, PointerPanZoomData> _pointerPanZooms = [];

    private ScaleState _state = ScaleState.Ready;
    private Matrix4? _lastTransform;
    private Point _initialFocalPoint;
    private Point? _currentFocalPoint;
    private double _initialSpan;
    private double _currentSpan;
    private double _initialHorizontalSpan;
    private double _currentHorizontalSpan;
    private double _initialVerticalSpan;
    private double _currentVerticalSpan;
    private Point _localFocalPoint;
    private LineBetweenPointers? _initialLine;
    private LineBetweenPointers? _currentLine;
    private VelocityTracker? _scaleVelocityTracker;
    private Point _delta;
    private double _initialPanZoomScaleFactor = 1.0;
    private double _initialPanZoomRotationFactor;
    private DateTime? _initialEventTimestamp;

    public ScaleGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    /// <summary>
    /// Which point all calculations start from: the position where the pointer first contacted the
    /// screen (<see cref="UI.DragStartBehavior.Down"/>, the default) or the position where the scale
    /// gesture was accepted (<see cref="UI.DragStartBehavior.Start"/>).
    /// </summary>
    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Down;

    /// <summary>
    /// The pointers in contact with the screen established a focal point and an initial scale of
    /// 1.0. Not called until this recognizer has won the gesture arena.
    /// </summary>
    public Action<ScaleStartDetails>? OnStart { get; set; }

    /// <summary>The pointers in contact with the screen indicated a new focal point and/or scale.</summary>
    public Action<ScaleUpdateDetails>? OnUpdate { get; set; }

    /// <summary>The pointers are no longer in contact with the screen.</summary>
    public Action<ScaleEndDetails>? OnEnd { get; set; }

    /// <summary>Whether scrolling up/down on a trackpad scales instead of panning. Defaults to false.</summary>
    public bool TrackpadScrollCausesScale { get; set; }

    /// <summary>
    /// Controls the direction and magnitude of the scale a trackpad scroll converts to. Incoming
    /// trackpad pan offsets are multiplied by this factor, so a larger divisor scrolls less.
    /// Defaults to <see cref="KDefaultTrackpadScrollToScaleFactor"/>.
    /// </summary>
    public Point TrackpadScrollToScaleFactor { get; set; } = KDefaultTrackpadScrollToScaleFactor;

    /// <summary>
    /// The number of pointers being tracked by the gesture recognizer. The pan/zoom protocol does
    /// not carry the exact number of fingers on the trackpad, but it is always at least two.
    /// </summary>
    public int PointerCount => (2 * _pointerPanZooms.Count) + _pointerQueue.Count;

    /// <inheritdoc />
    public override string DebugDescription => "scale";

    private double PointerScaleFactor => _initialSpan > 0.0 ? _currentSpan / _initialSpan : 1.0;

    private double PointerHorizontalScaleFactor =>
        _initialHorizontalSpan > 0.0 ? _currentHorizontalSpan / _initialHorizontalSpan : 1.0;

    private double PointerVerticalScaleFactor =>
        _initialVerticalSpan > 0.0 ? _currentVerticalSpan / _initialVerticalSpan : 1.0;

    private double ScaleFactor => CombineWithPanZooms(PointerScaleFactor);

    private double HorizontalScaleFactor => CombineWithPanZooms(PointerHorizontalScaleFactor);

    private double VerticalScaleFactor => CombineWithPanZooms(PointerVerticalScaleFactor);

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        _velocityTrackers[@event.Pointer] = new VelocityTracker(@event.Kind);
        _initialEventTimestamp = @event.TimestampUtc;
        if (_state == ScaleState.Ready)
        {
            _state = ScaleState.Possible;
            _initialSpan = 0.0;
            _currentSpan = 0.0;
            _initialHorizontalSpan = 0.0;
            _currentHorizontalSpan = 0.0;
            _initialVerticalSpan = 0.0;
            _currentVerticalSpan = 0.0;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Unlike every other recognizer this one accepts a pan/zoom gesture from any device, ignoring
    /// <see cref="GestureRecognizer.SupportedDevices"/>, exactly as Dart does.
    /// </remarks>
    protected override bool IsPointerPanZoomAllowed(PointerPanZoomStartEvent @event) => true;

    protected override void AddAllowedPointerPanZoom(PointerPanZoomStartEvent @event)
    {
        base.AddAllowedPointerPanZoom(@event);
        StartTrackingPointer(@event.Pointer, @event.Transform);
        _velocityTrackers[@event.Pointer] = new VelocityTracker(@event.Kind);
        _initialEventTimestamp = @event.TimestampUtc;
        if (_state == ScaleState.Ready)
        {
            _state = ScaleState.Possible;
            _initialPanZoomScaleFactor = 1.0;
            _initialPanZoomRotationFactor = 0.0;
        }
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        bool didChangeConfiguration = false;
        bool shouldStartIfAccepted = false;
        switch (@event)
        {
            case PointerMoveEvent move:
                VelocityTracker tracker = _velocityTrackers[move.Pointer];
                if (!move.Synthesized)
                {
                    tracker.AddPosition(move.TimestampUtc, move.Position);
                }

                _pointerLocations[move.Pointer] = move.Position;
                shouldStartIfAccepted = true;
                _lastTransform = move.Transform;
                break;
            case PointerDownEvent down:
                _pointerLocations[down.Pointer] = down.Position;
                _pointerQueue.Add(down.Pointer);
                didChangeConfiguration = true;
                shouldStartIfAccepted = true;
                _lastTransform = down.Transform;
                break;
            case PointerUpEvent or PointerCancelEvent:
                _pointerLocations.Remove(@event.Pointer);
                _pointerQueue.Remove(@event.Pointer);
                didChangeConfiguration = true;
                _lastTransform = @event.Transform;
                break;
            case PointerPanZoomStartEvent panZoomStart:
                _pointerPanZooms[panZoomStart.Pointer] = PointerPanZoomData.FromStartEvent(this, panZoomStart);
                didChangeConfiguration = true;
                shouldStartIfAccepted = true;
                _lastTransform = panZoomStart.Transform;
                break;
            case PointerPanZoomUpdateEvent panZoomUpdate:
                if (!panZoomUpdate.Synthesized && !TrackpadScrollCausesScale)
                {
                    _velocityTrackers[panZoomUpdate.Pointer]
                        .AddPosition(panZoomUpdate.TimestampUtc, panZoomUpdate.Pan);
                }

                _pointerPanZooms[panZoomUpdate.Pointer] = PointerPanZoomData.FromUpdateEvent(this, panZoomUpdate);
                _lastTransform = panZoomUpdate.Transform;
                shouldStartIfAccepted = true;
                break;
            case PointerPanZoomEndEvent panZoomEnd:
                _pointerPanZooms.Remove(panZoomEnd.Pointer);
                didChangeConfiguration = true;
                break;
        }

        UpdateLines();
        Update();

        if (!didChangeConfiguration || Reconfigure(@event.Pointer))
        {
            AdvanceStateMachine(shouldStartIfAccepted, @event);
        }

        StopTrackingIfPointerNoLongerDown(@event);
    }

    public override void AcceptGesture(int pointer)
    {
        if (_state != ScaleState.Possible)
        {
            return;
        }

        _state = ScaleState.Started;
        DispatchOnStartCallbackIfNeeded();
        if (DragStartBehavior == DragStartBehavior.Start)
        {
            _initialFocalPoint = _currentFocalPoint!.Value;
            _initialSpan = _currentSpan;
            _initialLine = _currentLine;
            _initialHorizontalSpan = _currentHorizontalSpan;
            _initialVerticalSpan = _currentVerticalSpan;
            CaptureInitialPanZoomFactors();
        }
    }

    public override void RejectGesture(int pointer)
    {
        _pointerPanZooms.Remove(pointer);
        _pointerLocations.Remove(pointer);
        _pointerQueue.Remove(pointer);
        StopTrackingPointer(pointer);
    }

    protected override void DidStopTrackingLastPointer(int pointer)
    {
        switch (_state)
        {
            case ScaleState.Possible:
                Resolve(GestureDisposition.Rejected);
                break;
            case ScaleState.Ready:
                // Dart asserts here: the recognizer cannot have seen a pointer yet.
                break;
            case ScaleState.Accepted:
                break;
            case ScaleState.Started:
                // Dart asserts here: the state must be `accepted` once the user is done.
                break;
        }

        _state = ScaleState.Ready;
    }

    public override void Dispose()
    {
        _velocityTrackers.Clear();
        base.Dispose();
    }

    private double CombineWithPanZooms(double pointerScaleFactor)
    {
        double scale = pointerScaleFactor;
        foreach (PointerPanZoomData panZoom in _pointerPanZooms.Values)
        {
            scale *= panZoom.Scale / _initialPanZoomScaleFactor;
        }

        return scale;
    }

    private double ComputeRotationFactor()
    {
        double factor = 0.0;
        if (_initialLine is { } initialLine && _currentLine is { } currentLine)
        {
            double fx = initialLine.PointerStartLocation.X;
            double fy = initialLine.PointerStartLocation.Y;
            double sx = initialLine.PointerEndLocation.X;
            double sy = initialLine.PointerEndLocation.Y;

            double nfx = currentLine.PointerStartLocation.X;
            double nfy = currentLine.PointerStartLocation.Y;
            double nsx = currentLine.PointerEndLocation.X;
            double nsy = currentLine.PointerEndLocation.Y;

            double angle1 = Math.Atan2(fy - sy, fx - sx);
            double angle2 = Math.Atan2(nfy - nsy, nfx - nsx);

            factor = angle2 - angle1;
        }

        foreach (PointerPanZoomData panZoom in _pointerPanZooms.Values)
        {
            factor += panZoom.Rotation;
        }

        factor -= _initialPanZoomRotationFactor;
        return factor;
    }

    private void Update()
    {
        Point? previousFocalPoint = _currentFocalPoint;

        // Compute the focal point.
        var focalPoint = new Point(0.0, 0.0);
        foreach (Point location in _pointerLocations.Values)
        {
            focalPoint += location;
        }

        foreach (PointerPanZoomData panZoom in _pointerPanZooms.Values)
        {
            focalPoint += panZoom.FocalPoint;
        }

        _currentFocalPoint = focalPoint / Math.Max(1, _pointerLocations.Count + _pointerPanZooms.Count);

        if (previousFocalPoint is null)
        {
            _localFocalPoint = PointerEvent.TransformPosition(_lastTransform, _currentFocalPoint.Value);
            _delta = default;
        }
        else
        {
            Point localPreviousFocalPoint = _localFocalPoint;
            _localFocalPoint = PointerEvent.TransformPosition(_lastTransform, _currentFocalPoint.Value);
            _delta = _localFocalPoint - localPreviousFocalPoint;
        }

        int count = _pointerLocations.Count;

        var pointerFocalPoint = new Point(0.0, 0.0);
        foreach (Point location in _pointerLocations.Values)
        {
            pointerFocalPoint += location;
        }

        if (count > 0)
        {
            pointerFocalPoint /= count;
        }

        // Span is the average deviation from the focal point; the horizontal and vertical spans are
        // the average deviations from the focal point's horizontal and vertical coordinates.
        double totalDeviation = 0.0;
        double totalHorizontalDeviation = 0.0;
        double totalVerticalDeviation = 0.0;
        foreach (Point location in _pointerLocations.Values)
        {
            totalDeviation += (pointerFocalPoint - location).Distance();
            totalHorizontalDeviation += Math.Abs(pointerFocalPoint.X - location.X);
            totalVerticalDeviation += Math.Abs(pointerFocalPoint.Y - location.Y);
        }

        _currentSpan = count > 0 ? totalDeviation / count : 0.0;
        _currentHorizontalSpan = count > 0 ? totalHorizontalDeviation / count : 0.0;
        _currentVerticalSpan = count > 0 ? totalVerticalDeviation / count : 0.0;
    }

    /// <summary>Updates the initial and current lines from the pointers registered right now.</summary>
    private void UpdateLines()
    {
        int count = _pointerLocations.Count;

        // With a single pointer registered there is no line to rotate, so the initial line follows
        // the current one.
        if (count < 2)
        {
            _initialLine = _currentLine;
        }
        else if (_initialLine is { } initialLine
                 && initialLine.PointerStartId == _pointerQueue[0]
                 && initialLine.PointerEndId == _pointerQueue[1])
        {
            // Rotation updated: set the current line.
            _currentLine = new LineBetweenPointers(
                pointerStartId: _pointerQueue[0],
                pointerStartLocation: _pointerLocations[_pointerQueue[0]],
                pointerEndId: _pointerQueue[1],
                pointerEndLocation: _pointerLocations[_pointerQueue[1]]);
        }
        else
        {
            // A new rotation process is on the way: set the initial line.
            _initialLine = new LineBetweenPointers(
                pointerStartId: _pointerQueue[0],
                pointerStartLocation: _pointerLocations[_pointerQueue[0]],
                pointerEndId: _pointerQueue[1],
                pointerEndLocation: _pointerLocations[_pointerQueue[1]]);
            _currentLine = _initialLine;
        }
    }

    private bool Reconfigure(int pointer)
    {
        _initialFocalPoint = _currentFocalPoint!.Value;
        _initialSpan = _currentSpan;
        _initialLine = _currentLine;
        _initialHorizontalSpan = _currentHorizontalSpan;
        _initialVerticalSpan = _currentVerticalSpan;
        CaptureInitialPanZoomFactors();
        if (_state == ScaleState.Started)
        {
            if (OnEnd is { } onEnd)
            {
                VelocityTracker tracker = _velocityTrackers[pointer];

                Velocity velocity = tracker.GetVelocity();
                if (IsFlingGesture(velocity))
                {
                    Vector pixelsPerSecond = velocity.PixelsPerSecond;
                    if (pixelsPerSecond.SquaredLength
                        > GestureConstants.MaxFlingVelocity * GestureConstants.MaxFlingVelocity)
                    {
                        velocity = new Velocity(
                            pixelsPerSecond / pixelsPerSecond.Length * GestureConstants.MaxFlingVelocity);
                    }

                    Velocity endVelocity = velocity;
                    InvokeCallback("onEnd", () => onEnd(new ScaleEndDetails(
                        velocity: endVelocity,
                        scaleVelocity: ScaleVelocity,
                        pointerCount: PointerCount)));
                }
                else
                {
                    InvokeCallback("onEnd", () => onEnd(new ScaleEndDetails(
                        scaleVelocity: ScaleVelocity,
                        pointerCount: PointerCount)));
                }
            }

            _state = ScaleState.Accepted;
            // An arbitrary device kind: this tracker only ever sees synthetic scale samples.
            _scaleVelocityTracker = new VelocityTracker(PointerDeviceKind.Touch);
            return false;
        }

        _scaleVelocityTracker = new VelocityTracker(PointerDeviceKind.Touch);
        return true;
    }

    private void AdvanceStateMachine(bool shouldStartIfAccepted, PointerEvent @event)
    {
        if (_state == ScaleState.Ready)
        {
            _state = ScaleState.Possible;
        }

        if (_state == ScaleState.Possible)
        {
            double spanDelta = Math.Abs(_currentSpan - _initialSpan);
            double focalPointDelta = (_currentFocalPoint!.Value - _initialFocalPoint).Distance();
            if (spanDelta > PointerEventUtils.ComputeScaleSlop(@event.Kind)
                || focalPointDelta > PointerEventUtils.ComputePanSlop(@event.Kind, GestureSettings)
                || Math.Max(ScaleFactor / PointerScaleFactor, PointerScaleFactor / ScaleFactor) > 1.05)
            {
                Resolve(GestureDisposition.Accepted);
            }
        }
        else if (_state >= ScaleState.Accepted)
        {
            Resolve(GestureDisposition.Accepted);
        }

        if (_state == ScaleState.Accepted && shouldStartIfAccepted)
        {
            _initialEventTimestamp = @event.TimestampUtc;
            _state = ScaleState.Started;
            DispatchOnStartCallbackIfNeeded();
        }

        if (_state == ScaleState.Started)
        {
            _scaleVelocityTracker?.AddPosition(@event.TimestampUtc, new Point(ScaleFactor, 0.0));
            if (OnUpdate is { } onUpdate)
            {
                InvokeCallback("onUpdate", () => onUpdate(new ScaleUpdateDetails(
                    scale: ScaleFactor,
                    horizontalScale: HorizontalScaleFactor,
                    verticalScale: VerticalScaleFactor,
                    focalPoint: _currentFocalPoint!.Value,
                    localFocalPoint: _localFocalPoint,
                    rotation: ComputeRotationFactor(),
                    pointerCount: PointerCount,
                    focalPointDelta: _delta,
                    sourceTimeStampUtc: @event.TimestampUtc)));
            }
        }
    }

    private void DispatchOnStartCallbackIfNeeded()
    {
        if (OnStart is { } onStart)
        {
            PointerDeviceKind? kind = _pointerQueue.Count > 0
                ? GetKindForPointer(_pointerQueue[0])
                : _pointerPanZooms.Count > 0
                    ? GetKindForPointer(_pointerPanZooms.Keys.First())
                    : null;
            InvokeCallback("onStart", () => onStart(new ScaleStartDetails(
                focalPoint: _currentFocalPoint!.Value,
                localFocalPoint: _localFocalPoint,
                pointerCount: PointerCount,
                sourceTimeStampUtc: _initialEventTimestamp,
                kind: kind)));
        }

        _initialEventTimestamp = null;
    }

    /// <summary>
    /// Dart's `_scaleVelocityTracker?.getVelocity().pixelsPerSecond.dx ?? -1`: the scale velocity
    /// reported to <see cref="OnEnd"/>, or -1 when no sample has been collected.
    /// </summary>
    private double ScaleVelocity => _scaleVelocityTracker?.GetVelocity().PixelsPerSecond.X ?? -1.0;

    private void CaptureInitialPanZoomFactors()
    {
        if (_pointerPanZooms.Count == 0)
        {
            _initialPanZoomScaleFactor = 1.0;
            _initialPanZoomRotationFactor = 0.0;
        }
        else
        {
            _initialPanZoomScaleFactor = ScaleFactor / PointerScaleFactor;
            _initialPanZoomRotationFactor = _pointerPanZooms.Values.Sum(panZoom => panZoom.Rotation);
        }
    }

    private static bool IsFlingGesture(Velocity velocity)
    {
        double speedSquared = velocity.PixelsPerSecond.SquaredLength;
        return speedSquared > GestureConstants.MinFlingVelocity * GestureConstants.MinFlingVelocity;
    }

    /// <summary>
    /// One trackpad pan/zoom gesture as the recognizer sees it. Ports Dart's `_PointerPanZoomData`.
    /// </summary>
    private sealed class PointerPanZoomData
    {
        private readonly ScaleGestureRecognizer _parent;
        private readonly Point _position;
        private readonly Point _pan;
        private readonly double _scale;
        private readonly double _rotation;

        private PointerPanZoomData(
            ScaleGestureRecognizer parent,
            Point position,
            Point pan,
            double scale,
            double rotation)
        {
            _parent = parent;
            _position = position;
            _pan = pan;
            _scale = scale;
            _rotation = rotation;
        }

        public static PointerPanZoomData FromStartEvent(
            ScaleGestureRecognizer parent,
            PointerPanZoomStartEvent @event)
        {
            return new PointerPanZoomData(parent, @event.Position, pan: default, scale: 1.0, rotation: 0.0);
        }

        public static PointerPanZoomData FromUpdateEvent(
            ScaleGestureRecognizer parent,
            PointerPanZoomUpdateEvent @event)
        {
            return new PointerPanZoomData(parent, @event.Position, @event.Pan, @event.Scale, @event.Rotation);
        }

        public Point FocalPoint => _parent.TrackpadScrollCausesScale ? _position : _position + _pan;

        public double Scale
        {
            get
            {
                if (_parent.TrackpadScrollCausesScale)
                {
                    return _scale * Math.Exp(
                        (_pan.X * _parent.TrackpadScrollToScaleFactor.X)
                        + (_pan.Y * _parent.TrackpadScrollToScaleFactor.Y));
                }

                return _scale;
            }
        }

        public double Rotation => _rotation;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"PointerPanZoomData(parent: {_parent}, _position: {_position}, _pan: {_pan}, "
                   + $"_scale: {_scale}, _rotation: {_rotation})";
        }
    }

    /// <summary>
    /// The line between two pointers on the screen, used to track a scale gesture's rotation. Ports
    /// Dart's `_LineBetweenPointers`.
    /// </summary>
    private sealed class LineBetweenPointers
    {
        public LineBetweenPointers(
            Point pointerStartLocation = default,
            int pointerStartId = 0,
            Point pointerEndLocation = default,
            int pointerEndId = 1)
        {
            if (pointerStartId == pointerEndId)
            {
                throw new ArgumentException(
                    "A line between pointers needs two distinct pointers.",
                    nameof(pointerEndId));
            }

            PointerStartLocation = pointerStartLocation;
            PointerStartId = pointerStartId;
            PointerEndLocation = pointerEndLocation;
            PointerEndId = pointerEndId;
        }

        /// <summary>Where the pointer that marks the start of the line is.</summary>
        public Point PointerStartLocation { get; }

        /// <summary>The id of the pointer that marks the start of the line.</summary>
        public int PointerStartId { get; }

        /// <summary>Where the pointer that marks the end of the line is.</summary>
        public Point PointerEndLocation { get; }

        /// <summary>The id of the pointer that marks the end of the line.</summary>
        public int PointerEndId { get; }
    }
}
