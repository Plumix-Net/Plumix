using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/force_press_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `force_press_test.dart` asserts against
/// `ForcePressGestureRecognizer`, driving the recognizer through the arena and pointer router the
/// way Flutter's `GestureTester` does. The device constants are the iPhone X values the Dart tests
/// use (`pressureMin` 0, `pressureMax` 6.66).
/// </summary>
public sealed class ForcePressGestureRecognizerTests : IDisposable
{
    private const double PressureMin = 0.0;
    private const double PressureMax = 6.66;

    private readonly GestureBinding _binding = GestureBinding.Instance;

    public ForcePressGestureRecognizerTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _binding.ResetForTests();
    }

    private static PointerDownEvent Down(
        int pointer,
        double pressure = 0.0,
        double pressureMin = PressureMin,
        double pressureMax = PressureMax)
    {
        return new PointerDownEvent(
            pointer,
            PointerDeviceKind.Touch,
            new Point(10.0, 10.0),
            PointerButtons.Primary,
            DateTime.UnixEpoch)
        {
            Pressure = pressure,
            PressureMin = pressureMin,
            PressureMax = pressureMax,
        };
    }

    private static PointerMoveEvent Move(
        int pointer,
        double pressure,
        Point? position = null,
        Point delta = default,
        double pressureMin = PressureMin,
        double pressureMax = PressureMax)
    {
        var @event = new PointerMoveEvent(
            pointer,
            PointerDeviceKind.Touch,
            position ?? new Point(10.0, 10.0),
            PointerButtons.Primary,
            down: true,
            DateTime.UnixEpoch)
        {
            Pressure = pressure,
            PressureMin = pressureMin,
            PressureMax = pressureMax,
        };
        return delta == default ? @event : (PointerMoveEvent)@event.WithDelta(delta);
    }

    private static PointerUpEvent Up(int pointer)
    {
        return new PointerUpEvent(
            pointer, PointerDeviceKind.Touch, new Point(10.0, 10.0), PointerButtons.None, DateTime.UnixEpoch);
    }

    private void Route(PointerEvent @event)
    {
        _binding.PointerRouter.Route(@event);
        _binding.GestureArena.FlushDefaultResolutions();
    }

    private sealed record Log(List<string> Entries)
    {
        public static Log Attach(ForcePressGestureRecognizer recognizer)
        {
            var log = new Log([]);
            recognizer.OnStart = _ => log.Entries.Add("start");
            recognizer.OnPeak = _ => log.Entries.Add("peak");
            recognizer.OnUpdate = _ => log.Entries.Add("update");
            recognizer.OnEnd = _ => log.Entries.Add("end");
            return log;
        }

        public int Count(string name) => Entries.Count(entry => entry == name);
    }

    [Fact]
    public void Constructor_DefaultsMatchFlutter()
    {
        var recognizer = new ForcePressGestureRecognizer();
        try
        {
            Assert.Equal(0.4, recognizer.StartPressure);
            Assert.Equal(0.85, recognizer.PeakPressure);
            Assert.Equal("force press", recognizer.DebugDescription);
            // The default interpolation is a clamped inverse lerp over the device's bounds.
            Assert.Equal(0.5, recognizer.Interpolation(0.0, 2.0, 1.0));
            Assert.Equal(1.0, recognizer.Interpolation(0.0, 2.0, 4.0));
            Assert.Equal(0.0, recognizer.Interpolation(0.0, 2.0, -4.0));
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void Constructor_RejectsAPeakPressureThatIsNotAboveTheStartPressure()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ForcePressGestureRecognizer(startPressure: 0.5, peakPressure: 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ForcePressGestureRecognizer(startPressure: 0.9, peakPressure: 0.5));
    }

    [Fact]
    public void ForcePress_IsRecognizedAcrossTheStartPeakAndEndThresholds()
    {
        var recognizer = new ForcePressGestureRecognizer();
        Point startGlobalPosition = default;
        recognizer.OnStart = details => startGlobalPosition = details.GlobalPosition;
        Log log = Log.Attach(recognizer);
        recognizer.OnStart = details =>
        {
            startGlobalPosition = details.GlobalPosition;
            log.Entries.Add("start");
        };

        try
        {
            recognizer.AddPointer(Down(1));
            _binding.GestureArena.Close(1);
            Assert.Empty(log.Entries);

            // 2.5 / 6.66 = 0.375, still under the 0.4 start pressure.
            Route(Move(1, pressure: 2.5));
            Assert.Empty(log.Entries);

            // 2.8 / 6.66 = 0.42 — the gesture starts and the same event reports an update.
            Route(Move(1, pressure: 2.8));
            Assert.Equal(1, log.Count("start"));
            Assert.Equal(1, log.Count("update"));
            Assert.Equal(0, log.Count("peak"));
            Assert.Equal(new Point(10.0, 10.0), startGlobalPosition);

            // 5.8 / 6.66 = 0.87 crosses the 0.85 peak pressure exactly once.
            Route(Move(1, pressure: 3.3));
            Route(Move(1, pressure: 5.8));
            Route(Move(1, pressure: 6.0));
            Assert.Equal(1, log.Count("start"));
            Assert.Equal(1, log.Count("peak"));
            Assert.Equal(4, log.Count("update"));

            Route(Up(1));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(1, log.Count("end"));
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.5)]
    public void ADeviceWithoutForceSensingNeverParticipates(double pressureMax)
    {
        var recognizer = new ForcePressGestureRecognizer();
        Log log = Log.Attach(recognizer);

        try
        {
            recognizer.AddPointer(Down(1, pressureMax: pressureMax));
            _binding.GestureArena.Close(1);
            Route(Move(1, pressure: 2.8, pressureMax: pressureMax));
            Route(Move(1, pressure: 6.0, pressureMax: pressureMax));
            Route(Up(1));
            _binding.GestureArena.Sweep(1);

            Assert.Empty(log.Entries);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void BelowTheStartPressure_NoStartOrEndCallbackFires()
    {
        var recognizer = new ForcePressGestureRecognizer();
        Log log = Log.Attach(recognizer);

        try
        {
            recognizer.AddPointer(Down(1));
            _binding.GestureArena.Close(1);
            Route(Move(1, pressure: 2.5));
            Assert.Empty(log.Entries);

            Route(Up(1));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(log.Entries);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ACompetingDragWinsWhenThePointerMovesWithoutPressure()
    {
        var recognizer = new ForcePressGestureRecognizer();
        var drag = new PanGestureRecognizer();
        Log log = Log.Attach(recognizer);
        int panStarts = 0;
        drag.OnStart = _ => panStarts++;

        try
        {
            PointerDownEvent down = Down(1);
            recognizer.AddPointer(down);
            drag.AddPointer(down);
            _binding.GestureArena.Close(1);

            Route(Move(1, pressure: 0.0, position: new Point(30.0, 10.0), delta: new Point(20.0, 0.0)));
            Route(Move(1, pressure: 0.0, position: new Point(60.0, 10.0), delta: new Point(30.0, 0.0)));

            Assert.Empty(log.Entries);
            Assert.Equal(1, panStarts);
        }
        finally
        {
            recognizer.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void AGestureThatWasNeverAcceptedReportsNoEndOnPointerUp()
    {
        var recognizer = new ForcePressGestureRecognizer();
        var drag = new PanGestureRecognizer();
        Log log = Log.Attach(recognizer);
        int panStarts = 0;
        drag.OnStart = _ => panStarts++;

        try
        {
            PointerDownEvent down = Down(1);
            recognizer.AddPointer(down);
            drag.AddPointer(down);
            _binding.GestureArena.Close(1);

            Route(Up(1));
            _binding.GestureArena.Sweep(1);

            Assert.Empty(log.Entries);
            Assert.Equal(0, panStarts);
        }
        finally
        {
            recognizer.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void StartIsReportedOnlyOnceWhenACompetitorIsInTheArena()
    {
        var recognizer = new ForcePressGestureRecognizer();
        var drag = new PanGestureRecognizer();
        Log log = Log.Attach(recognizer);
        int panStarts = 0;
        drag.OnStart = _ => panStarts++;

        try
        {
            PointerDownEvent down = Down(1);
            recognizer.AddPointer(down);
            drag.AddPointer(down);
            _binding.GestureArena.Close(1);
            Assert.Empty(log.Entries);

            // 3.0 / 6.66 = 0.45: the recognizer wins the arena and starts on the same event.
            Route(Move(1, pressure: 3.0));
            Assert.Equal(1, log.Count("start"));
            Assert.Equal(1, log.Count("update"));
            Assert.Equal(0, panStarts);

            Route(Up(1));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(1, log.Count("start"));
            Assert.Equal(1, log.Count("end"));
            Assert.Equal(0, panStarts);
        }
        finally
        {
            recognizer.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void ACustomInterpolationDecidesTheThresholds()
    {
        // Halves every reported pressure, so the raw values that crossed the thresholds above no
        // longer do.
        var recognizer = new ForcePressGestureRecognizer(
            interpolation: (min, max, pressure) => (pressure - min) / (max - min) / 2.0);
        Log log = Log.Attach(recognizer);

        try
        {
            recognizer.AddPointer(Down(1));
            _binding.GestureArena.Close(1);

            // 2.8 / 6.66 / 2 = 0.21, under the start pressure.
            Route(Move(1, pressure: 2.8));
            Assert.Empty(log.Entries);

            // 5.8 / 6.66 / 2 = 0.435, over the start pressure but under the peak.
            Route(Move(1, pressure: 5.8));
            Assert.Equal(1, log.Count("start"));
            Assert.Equal(0, log.Count("peak"));

            Route(Up(1));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(1, log.Count("end"));
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void APressureOutsideTheDeviceBoundsIsClampedByTheDefaultInterpolation()
    {
        var recognizer = new ForcePressGestureRecognizer();
        var pressures = new List<double>();
        recognizer.OnUpdate = details => pressures.Add(details.Pressure);
        recognizer.OnStart = _ => { };

        try
        {
            recognizer.AddPointer(Down(1));
            _binding.GestureArena.Close(1);

            Route(Move(1, pressure: 8.0));
            Route(Move(1, pressure: -3.0));
            _binding.GestureArena.Sweep(1);

            // Both events report an interpolated pressure inside 0..1 rather than throwing.
            Assert.Equal([1.0, 0.0], pressures);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ANaNPressureSuppressesUpdatesWithoutThrowing()
    {
        var recognizer = new ForcePressGestureRecognizer(
            interpolation: (_, _, _) => double.NaN);
        Log log = Log.Attach(recognizer);

        try
        {
            recognizer.AddPointer(Down(1));
            _binding.GestureArena.Close(1);
            Route(Move(1, pressure: 5.0));
            Route(Up(1));
            _binding.GestureArena.Sweep(1);

            Assert.Empty(log.Entries);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void Details_DefaultTheLocalPositionToTheGlobalPosition()
    {
        var details = new ForcePressDetails(globalPosition: new Point(4.0, 8.0), pressure: 0.5);
        Assert.Equal(new Point(4.0, 8.0), details.LocalPosition);
        Assert.Equal(0.5, details.Pressure);

        var relocated = new ForcePressDetails(
            globalPosition: new Point(4.0, 8.0),
            pressure: 0.5,
            localPosition: new Point(1.0, 2.0));
        Assert.Equal(new Point(1.0, 2.0), relocated.LocalPosition);
    }

    [Fact]
    public void Details_DebugFillPropertiesMatchDartAndUseIdentityEquality()
    {
        var details = new ForcePressDetails(globalPosition: default, pressure: 1.0);
        var properties = new DiagnosticPropertiesBuilder();

        details.DebugFillProperties(properties);

        Assert.Equal(
            [
                "globalPosition: Offset(0.0, 0.0)",
                "localPosition: Offset(0.0, 0.0)",
                "pressure: 1.0",
            ],
            properties.Properties
                .Where(property => !property.IsFiltered(DiagnosticLevel.Info))
                .Select(property => property.ToString()));
        Assert.False(details.Equals(new ForcePressDetails(globalPosition: default, pressure: 1.0)));
    }
}
