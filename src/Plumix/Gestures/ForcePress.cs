using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/force_press.dart

namespace Plumix.Gestures;

/// <summary>The stage a <see cref="ForcePressGestureRecognizer"/> has reached.</summary>
/// <remarks>Flutter's private <c>_ForceState</c>.</remarks>
internal enum ForceState
{
    /// <summary>No pointer has touched down and the detector is ready for one.</summary>
    Ready,

    /// <summary>A pointer is down, but no force press has been detected yet.</summary>
    Possible,

    /// <summary>
    /// A pointer is down and the gesture was accepted in the arena, but the pressure has not yet
    /// crossed <see cref="ForcePressGestureRecognizer.StartPressure"/>, so it has not started.
    /// </summary>
    Accepted,

    /// <summary>The pressure has crossed <see cref="ForcePressGestureRecognizer.StartPressure"/>.</summary>
    Started,

    /// <summary>
    /// The pressure has crossed <see cref="ForcePressGestureRecognizer.PeakPressure"/>. Updates keep
    /// being reported after this point.
    /// </summary>
    Peaked,
}

/// <summary>
/// Details for the callbacks of a <see cref="ForcePressGestureRecognizer"/>.
/// </summary>
/// <remarks>Flutter's <c>ForcePressDetails</c>.</remarks>
public sealed class ForcePressDetails : Diagnosticable, IPositionedGestureDetails
{
    /// <summary>
    /// Creates the details. An omitted <paramref name="localPosition"/> falls back to
    /// <paramref name="globalPosition"/>, exactly as Dart's constructor does.
    /// </summary>
    public ForcePressDetails(Point globalPosition, double pressure, Point? localPosition = null)
    {
        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
        Pressure = pressure;
    }

    /// <inheritdoc />
    public Point GlobalPosition { get; }

    /// <inheritdoc />
    public Point LocalPosition { get; }

    /// <summary>The pressure of the pointer on the screen.</summary>
    public double Pressure { get; }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(GestureDetailsDiagnostics.OffsetProperty("globalPosition", GlobalPosition));
        properties.Add(GestureDetailsDiagnostics.OffsetProperty("localPosition", LocalPosition));
        properties.Add(new DoubleProperty("pressure", Pressure));
    }
}

/// <summary>
/// Converts a raw device pressure into the range 0.0 to 1.0 given the device's pressure bounds.
/// </summary>
/// <remarks>Flutter's <c>GestureForceInterpolation</c>.</remarks>
public delegate double GestureForceInterpolation(double pressureMin, double pressureMax, double pressure);

/// <summary>
/// Recognizes a force press on devices that have force sensors.
/// </summary>
/// <remarks>
/// Flutter's <c>ForcePressGestureRecognizer</c>. Only the force of a single pointer is used. A tap
/// recognizer wins against this one on pointer up as long as the pressure never exceeded
/// <see cref="StartPressure"/>.
/// </remarks>
public class ForcePressGestureRecognizer : OneSequenceGestureRecognizer
{
    private OffsetPair _lastPosition;
    private double _lastPressure;
    private ForceState _state = ForceState.Ready;

    /// <summary>
    /// Creates a force press gesture recognizer. <paramref name="peakPressure"/> must be greater
    /// than <paramref name="startPressure"/>.
    /// </summary>
    public ForcePressGestureRecognizer(
        double startPressure = 0.4,
        double peakPressure = 0.85,
        GestureForceInterpolation? interpolation = null,
        object? debugOwner = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        AllowedButtonsFilter? allowedButtonsFilter = null,
        GestureBinding? binding = null) : base(binding)
    {
        if (peakPressure <= startPressure)
        {
            throw new ArgumentOutOfRangeException(
                nameof(peakPressure),
                "peakPressure must be greater than startPressure.");
        }

        StartPressure = startPressure;
        PeakPressure = peakPressure;
        Interpolation = interpolation ?? InverseLerp;
        DebugOwner = debugOwner;
        SupportedDevices = supportedDevices;
        if (allowedButtonsFilter is not null)
        {
            AllowedButtonsFilter = allowedButtonsFilter;
        }
    }

    /// <summary>
    /// A pointer pressed with a force exceeding <see cref="StartPressure"/>; every other recognizer
    /// in the arena has been rejected.
    /// </summary>
    public Action<ForcePressDetails>? OnStart { get; set; }

    /// <summary>
    /// A pointer is moving on the plane of the screen, changing pressure, or both. Reported for
    /// every event between <see cref="OnStart"/>/<see cref="OnPeak"/> and <see cref="OnEnd"/>.
    /// </summary>
    public Action<ForcePressDetails>? OnUpdate { get; set; }

    /// <summary>A pointer pressed with a force exceeding <see cref="PeakPressure"/>.</summary>
    public Action<ForcePressDetails>? OnPeak { get; set; }

    /// <summary>The pointer is no longer in contact with the screen.</summary>
    public Action<ForcePressDetails>? OnEnd { get; set; }

    /// <summary>The pressure required to initiate a force press, where 1.0 is maximum pressure.</summary>
    public double StartPressure { get; }

    /// <summary>The pressure required to peak a force press. Greater than <see cref="StartPressure"/>.</summary>
    public double PeakPressure { get; }

    /// <summary>
    /// Converts the device's raw touch pressure into the range 0.0 to 1.0. May return NaN for values
    /// it does not want to support. Defaults to a clamped linear interpolation.
    /// </summary>
    public GestureForceInterpolation Interpolation { get; }

    /// <inheritdoc />
    public override string DebugDescription => "force press";

    /// <inheritdoc />
    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        // A device whose maximum pressure is at most 1.0 has no force sensing; stay out of the arena.
        if (@event.PressureMax <= 1.0)
        {
            Resolve(GestureDisposition.Rejected);
        }
        else
        {
            base.AddAllowedPointer(@event);
            if (_state == ForceState.Ready)
            {
                _state = ForceState.Possible;
                _lastPosition = OffsetPair.FromEventPosition(@event);
            }
        }
    }

    /// <inheritdoc />
    protected override void HandleEvent(PointerEvent @event)
    {
        // A static pointer whose pressure changes reports PointerMoveEvent.
        if (@event is PointerMoveEvent or PointerDownEvent)
        {
            double pressure = Interpolation(@event.PressureMin, @event.PressureMax, @event.Pressure);
            _lastPosition = OffsetPair.FromEventPosition(@event);
            _lastPressure = pressure;

            if (_state == ForceState.Possible)
            {
                if (pressure > StartPressure)
                {
                    _state = ForceState.Started;
                    Resolve(GestureDisposition.Accepted);
                }
                else if (@event.Delta.DistanceSquared()
                         > PointerEventUtils.ComputeHitSlop(@event.Kind, GestureSettings))
                {
                    Resolve(GestureDisposition.Rejected);
                }
            }

            // When this is the only recognizer in the arena the gesture is accepted immediately, but
            // it must still not start until the pressure crosses the threshold.
            if (pressure > StartPressure && _state == ForceState.Accepted)
            {
                _state = ForceState.Started;
                if (OnStart is { } onStart)
                {
                    InvokeCallback(
                        "onStart",
                        () => onStart(new ForcePressDetails(
                            globalPosition: _lastPosition.Global,
                            localPosition: _lastPosition.Local,
                            pressure: pressure)));
                }
            }

            if (OnPeak is { } onPeak && pressure > PeakPressure && _state == ForceState.Started)
            {
                _state = ForceState.Peaked;
                InvokeCallback(
                    "onPeak",
                    () => onPeak(new ForcePressDetails(
                        globalPosition: @event.Position,
                        localPosition: @event.LocalPosition,
                        pressure: pressure)));
            }

            if (OnUpdate is { } onUpdate
                && !double.IsNaN(pressure)
                && _state is ForceState.Started or ForceState.Peaked)
            {
                InvokeCallback(
                    "onUpdate",
                    () => onUpdate(new ForcePressDetails(
                        globalPosition: @event.Position,
                        localPosition: @event.LocalPosition,
                        pressure: pressure)));
            }
        }

        StopTrackingIfPointerNoLongerDown(@event);
    }

    /// <inheritdoc />
    public override void AcceptGesture(int pointer)
    {
        if (_state == ForceState.Possible)
        {
            _state = ForceState.Accepted;
        }

        if (OnStart is { } onStart && _state == ForceState.Started)
        {
            InvokeCallback(
                "onStart",
                () => onStart(new ForcePressDetails(
                    globalPosition: _lastPosition.Global,
                    localPosition: _lastPosition.Local,
                    pressure: _lastPressure)));
        }
    }

    /// <inheritdoc />
    protected override void DidStopTrackingLastPointer(int pointer)
    {
        bool wasAccepted = _state is ForceState.Started or ForceState.Peaked;
        if (_state == ForceState.Possible)
        {
            Resolve(GestureDisposition.Rejected);
            return;
        }

        if (wasAccepted && OnEnd is { } onEnd)
        {
            InvokeCallback(
                "onEnd",
                () => onEnd(new ForcePressDetails(
                    globalPosition: _lastPosition.Global,
                    localPosition: _lastPosition.Local,
                    pressure: 0.0)));
        }

        _state = ForceState.Ready;
    }

    /// <inheritdoc />
    public override void RejectGesture(int pointer)
    {
        StopTrackingPointer(pointer);
        DidStopTrackingLastPointer(pointer);
    }

    /// <summary>
    /// The default <see cref="Interpolation"/>: a linear interpolation clamped to 0.0..1.0, which
    /// keeps the recognizer working when a device reports a pressure outside its own bounds.
    /// </summary>
    private static double InverseLerp(double min, double max, double t)
    {
        double value = (t - min) / (max - min);
        if (!double.IsNaN(value))
        {
            value = Math.Clamp(value, 0.0, 1.0);
        }

        return value;
    }
}
