using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/scale_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `scale_test.dart` asserts against `ScaleGestureRecognizer`,
/// driving the recognizer through the arena and the pointer router the way Flutter's
/// `GestureTester` does (its `route` flushes microtasks, so every `Route` here flushes the arena's
/// default resolutions).
/// </summary>
public sealed class ScaleGestureRecognizerTests : IDisposable
{
    private const double Tolerance = 1e-9;

    private readonly GestureBinding _binding = GestureBinding.Instance;

    public ScaleGestureRecognizerTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _binding.ResetForTests();
    }

    private static PointerDownEvent Down(
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double milliseconds = 0.0)
    {
        return new PointerDownEvent(
            pointer, kind, position, PointerButtons.Primary, DateTime.UnixEpoch.AddMilliseconds(milliseconds));
    }

    private static PointerMoveEvent Move(
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double milliseconds = 0.0)
    {
        return new PointerMoveEvent(
            pointer,
            kind,
            position,
            PointerButtons.Primary,
            down: true,
            DateTime.UnixEpoch.AddMilliseconds(milliseconds));
    }

    private static PointerUpEvent Up(
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerUpEvent(pointer, kind, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private static PointerPanZoomStartEvent PanZoomStart(int pointer, Point position, double milliseconds = 0.0)
    {
        return new PointerPanZoomStartEvent(pointer, position, DateTime.UnixEpoch.AddMilliseconds(milliseconds));
    }

    private static PointerPanZoomUpdateEvent PanZoomUpdate(
        int pointer,
        Point position,
        Point pan = default,
        double scale = 1.0,
        double rotation = 0.0,
        double milliseconds = 0.0)
    {
        return new PointerPanZoomUpdateEvent(
            pointer,
            position,
            DateTime.UnixEpoch.AddMilliseconds(milliseconds),
            pan: pan,
            scale: scale,
            rotation: rotation);
    }

    private static PointerPanZoomEndEvent PanZoomEnd(int pointer, Point position, double milliseconds = 0.0)
    {
        return new PointerPanZoomEndEvent(pointer, position, DateTime.UnixEpoch.AddMilliseconds(milliseconds));
    }

    private void Route(PointerEvent @event)
    {
        _binding.PointerRouter.Route(@event);
        _binding.GestureArena.FlushDefaultResolutions();
    }

    private static void AssertPoint(Point expected, Point? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.X, actual!.Value.X, Tolerance);
        Assert.Equal(expected.Y, actual.Value.Y, Tolerance);
    }

    private static void AssertClose(double expected, double? actual, double tolerance = Tolerance)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected, actual!.Value, tolerance);
    }

    /// <summary>Records everything the three callbacks report, so a test can assert and reset it.</summary>
    private sealed class ScaleLog
    {
        public bool DidStart;
        public bool DidEnd;
        public Point? FocalPoint;
        public Point? Delta;
        public double? Scale;
        public double? HorizontalScale;
        public double? VerticalScale;
        public double? Rotation;
        public int? PointerCount;
        public DateTime? StartTimestamp;
        public DateTime? UpdateTimestamp;
        public PointerDeviceKind? Kind;
        public double? ScaleVelocity;

        public void Attach(ScaleGestureRecognizer recognizer)
        {
            recognizer.OnStart = details =>
            {
                DidStart = true;
                FocalPoint = details.FocalPoint;
                PointerCount = details.PointerCount;
                StartTimestamp = details.SourceTimeStampUtc;
                Kind = details.Kind;
            };
            recognizer.OnUpdate = details =>
            {
                Scale = details.Scale;
                HorizontalScale = details.HorizontalScale;
                VerticalScale = details.VerticalScale;
                Rotation = details.Rotation;
                FocalPoint = details.FocalPoint;
                Delta = details.FocalPointDelta;
                PointerCount = details.PointerCount;
                UpdateTimestamp = details.SourceTimeStampUtc;
            };
            recognizer.OnEnd = details =>
            {
                DidEnd = true;
                ScaleVelocity = details.ScaleVelocity;
                PointerCount = details.PointerCount;
            };
        }

        public void Reset()
        {
            DidStart = false;
            DidEnd = false;
            FocalPoint = null;
            Delta = null;
            Scale = null;
            HorizontalScale = null;
            VerticalScale = null;
            Rotation = null;
            PointerCount = null;
            StartTimestamp = null;
            UpdateTimestamp = null;
        }
    }

    [Fact]
    public void ShouldRecognizeScaleGestures()
    {
        var scale = new ScaleGestureRecognizer();
        var tap = new TapGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);
        bool didTap = false;
        tap.OnTap = () => didTap = true;

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0));
            scale.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.Null(log.FocalPoint);
            Assert.False(log.DidEnd);
            Assert.False(didTap);

            // One-finger panning.
            Route(down);
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.False(log.DidEnd);

            Route(Move(1, new Point(20.0, 30.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(20.0, 30.0), log.Delta);
            Assert.Equal(1, log.PointerCount);
            Assert.False(log.DidEnd);
            Assert.False(didTap);
            log.Reset();

            // Two-finger scaling: the extra pointer ends the gesture in progress.
            PointerDownEvent down2 = Down(2, new Point(10.0, 20.0));
            scale.AddPointer(down2);
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.True(log.DidEnd);
            Assert.Null(log.Scale);
            Assert.Null(log.FocalPoint);
            Assert.False(log.DidStart);
            log.Reset();

            // Zoom in.
            Route(Move(2, new Point(0.0, 10.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(10.0, 20.0), log.FocalPoint);
            AssertClose(2.0, log.Scale);
            AssertClose(2.0, log.HorizontalScale);
            AssertClose(2.0, log.VerticalScale);
            AssertPoint(new Point(-5.0, -5.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            // Zoom out.
            Route(Move(2, new Point(15.0, 25.0)));
            AssertPoint(new Point(17.5, 27.5), log.FocalPoint);
            AssertClose(0.5, log.Scale);
            AssertClose(0.5, log.HorizontalScale);
            AssertClose(0.5, log.VerticalScale);
            AssertPoint(new Point(7.5, 7.5), log.Delta);
            Assert.Equal(2, log.PointerCount);
            Assert.False(didTap);

            // Horizontal scaling.
            Route(Move(2, new Point(0.0, 20.0)));
            AssertClose(2.0, log.HorizontalScale);
            AssertClose(1.0, log.VerticalScale);
            Assert.Equal(2, log.PointerCount);

            // Vertical scaling.
            Route(Move(2, new Point(10.0, 10.0)));
            AssertClose(1.0, log.HorizontalScale);
            AssertClose(2.0, log.VerticalScale);
            AssertPoint(new Point(5.0, -5.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            Route(Move(2, new Point(15.0, 25.0)));
            log.Reset();

            // Three-finger scaling.
            PointerDownEvent down3 = Down(3, new Point(25.0, 35.0));
            scale.AddPointer(down3);
            tap.AddPointer(down3);
            _binding.GestureArena.Close(3);
            Route(down3);
            Assert.True(log.DidEnd);
            Assert.Null(log.Scale);
            Assert.False(log.DidStart);
            log.Reset();

            // Zoom in.
            Route(Move(3, new Point(55.0, 65.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(30.0, 40.0), log.FocalPoint);
            AssertClose(5.0, log.Scale);
            AssertPoint(new Point(10.0, 10.0), log.Delta);
            Assert.Equal(3, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            // Return to the original positions but with different fingers.
            Route(Move(1, new Point(25.0, 35.0)));
            Route(Move(2, new Point(20.0, 30.0)));
            Route(Move(3, new Point(15.0, 25.0)));
            Assert.False(log.DidStart);
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertClose(-13.3, log.Delta!.Value.X, 0.1);
            AssertClose(-13.3, log.Delta.Value.Y, 0.1);
            Assert.Equal(3, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            Route(Up(1, new Point(25.0, 35.0)));
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.True(log.DidEnd);
            log.Reset();

            // Continue scaling with two fingers.
            Route(Move(3, new Point(10.0, 20.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(15.0, 25.0), log.FocalPoint);
            AssertClose(2.0, log.Scale);
            AssertPoint(new Point(-2.5, -2.5), log.Delta);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            // Continue rotating with two fingers.
            Route(Move(3, new Point(30.0, 40.0)));
            AssertPoint(new Point(25.0, 35.0), log.FocalPoint);
            AssertClose(2.0, log.Scale);
            AssertPoint(new Point(10.0, 10.0), log.Delta);
            Route(Move(3, new Point(10.0, 20.0)));
            AssertPoint(new Point(15.0, 25.0), log.FocalPoint);
            AssertClose(2.0, log.Scale);
            AssertPoint(new Point(-10.0, -10.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            Route(Up(2, new Point(20.0, 30.0)));
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.True(log.DidEnd);
            log.Reset();

            // Continue panning with one finger.
            Route(Move(3, new Point(0.0, 0.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(0.0, 0.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(-10.0, -20.0), log.Delta);
            Assert.Equal(1, log.PointerCount);
            log.Reset();

            // We are done.
            Route(Up(3, new Point(0.0, 0.0)));
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.True(log.DidEnd);
            Assert.False(didTap);
        }
        finally
        {
            scale.Dispose();
            tap.Dispose();
        }
    }

    [Fact]
    public void RejectsScaleGesturesFromUnallowedDeviceKinds()
    {
        var scale = new ScaleGestureRecognizer
        {
            SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Touch },
        };
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0), PointerDeviceKind.Mouse);
            scale.AddPointer(down);
            _binding.GestureArena.Close(1);

            Route(down);
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);

            // Using a mouse the scale gesture must not even start.
            Route(Move(1, new Point(20.0, 30.0), PointerDeviceKind.Mouse));
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void ScaleGestureStartedFromAnAllowedDeviceCannotBeEndedFromAnUnallowedOne()
    {
        var scale = new ScaleGestureRecognizer
        {
            SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Touch },
        };
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0));
            scale.AddPointer(down);
            _binding.GestureArena.Close(1);

            // A lone recognizer wins the arena by default, so the gesture starts on the down event.
            Route(down);
            Assert.True(log.DidStart);
            Assert.Null(log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.FocalPoint);
            Assert.False(log.DidEnd);
            log.Reset();

            Route(Move(1, new Point(20.0, 30.0)));
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            Assert.False(log.DidEnd);
            log.Reset();

            // A mouse pointer is ignored entirely.
            PointerDownEvent down2 = Down(2, new Point(10.0, 20.0), PointerDeviceKind.Mouse);
            scale.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.False(log.DidEnd);
            Assert.Null(log.Scale);
            Assert.False(log.DidStart);

            Route(Move(2, new Point(0.0, 10.0), PointerDeviceKind.Mouse));
            Assert.Null(log.Scale);
            Assert.False(log.DidEnd);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void ScaleGestureCompetesWithDrag()
    {
        var scale = new ScaleGestureRecognizer();
        var drag = new HorizontalDragGestureRecognizer();
        var log = new List<string>();
        scale.OnStart = _ => log.Add("scale-start");
        scale.OnUpdate = _ => log.Add("scale-update");
        scale.OnEnd = _ => log.Add("scale-end");
        drag.OnStart = _ => log.Add("drag-start");
        drag.OnEnd = _ => log.Add("drag-end");

        try
        {
            PointerDownEvent down = Down(1, new Point(10.0, 10.0));
            scale.AddPointer(down);
            drag.AddPointer(down);
            _binding.GestureArena.Close(1);
            Assert.Empty(log);

            Route(down);
            Assert.Empty(log);

            // Scale wins once the focal point delta exceeds the pan slop of 18.0 * 2.
            Route(Move(1, new Point(10.0, 50.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            PointerDownEvent down2 = Down(2, new Point(10.0, 20.0));
            scale.AddPointer(down2);
            drag.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Assert.Empty(log);

            // The second pointer joins the scale even though it moves horizontally.
            Route(down2);
            Assert.Equal(["scale-end"], log);
            log.Clear();

            Route(Move(2, new Point(30.0, 20.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            Route(Up(1, new Point(10.0, 50.0)));
            Assert.Equal(["scale-end"], log);
            log.Clear();

            Route(Up(2, new Point(30.0, 20.0)));
            Assert.Empty(log);

            // A fast horizontal move lets the scale win before the horizontal drag does.
            PointerDownEvent down3 = Down(3, new Point(30.0, 30.0));
            scale.AddPointer(down3);
            drag.AddPointer(down3);
            _binding.GestureArena.Close(3);
            Route(down3);
            Assert.Empty(log);

            Route(Move(3, new Point(100.0, 30.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            Route(Up(3, new Point(100.0, 30.0)));
            Assert.Equal(["scale-end"], log);
        }
        finally
        {
            scale.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void ShouldRecognizeRotationGestures()
    {
        var scale = new ScaleGestureRecognizer();
        var tap = new TapGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);
        bool didTap = false;
        tap.OnTap = () => didTap = true;

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0));
            scale.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);

            Route(down);
            Route(Move(1, new Point(20.0, 30.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertPoint(new Point(20.0, 30.0), log.Delta);
            AssertClose(0.0, log.Rotation);
            Assert.Equal(1, log.PointerCount);
            log.Reset();

            PointerDownEvent down2 = Down(2, new Point(30.0, 40.0));
            scale.AddPointer(down2);
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.True(log.DidEnd);
            Assert.Null(log.Rotation);
            log.Reset();

            // Zoom in.
            Route(Move(2, new Point(40.0, 50.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(30.0, 40.0), log.FocalPoint);
            AssertPoint(new Point(5.0, 5.0), log.Delta);
            AssertClose(0.0, log.Rotation);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            // Rotate by half a turn.
            Route(Move(2, new Point(0.0, 10.0)));
            AssertPoint(new Point(10.0, 20.0), log.FocalPoint);
            AssertPoint(new Point(-20.0, -20.0), log.Delta);
            AssertClose(Math.PI, log.Rotation);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            // A third finger reconfigures the gesture.
            PointerDownEvent down3 = Down(3, new Point(25.0, 35.0));
            scale.AddPointer(down3);
            tap.AddPointer(down3);
            _binding.GestureArena.Close(3);
            Route(down3);
            Assert.True(log.DidEnd);
            log.Reset();

            Route(Move(3, new Point(55.0, 65.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(25.0, 35.0), log.FocalPoint);
            AssertClose(0.0, log.Rotation);
            Assert.Equal(3, log.PointerCount);
            log.Reset();

            // Return to the original positions but with different fingers.
            Route(Move(1, new Point(25.0, 35.0)));
            Route(Move(2, new Point(20.0, 30.0)));
            Route(Move(3, new Point(15.0, 25.0)));
            Assert.False(log.DidStart);
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(-13.3, log.Delta!.Value.X, 0.1);
            AssertClose(-13.3, log.Delta.Value.Y, 0.1);
            AssertClose(0.0, log.Rotation);
            Assert.Equal(3, log.PointerCount);
            log.Reset();

            Route(Up(1, new Point(25.0, 35.0)));
            Assert.True(log.DidEnd);
            log.Reset();

            Route(Move(3, new Point(10.0, 20.0)));
            Assert.True(log.DidStart);
            AssertClose(0.0, log.Rotation);
            log.Reset();

            Route(Move(3, new Point(30.0, 40.0)));
            AssertClose(-Math.PI, log.Rotation);
            Route(Move(3, new Point(10.0, 20.0)));
            AssertClose(0.0, log.Rotation);
            Assert.Equal(2, log.PointerCount);
            Assert.False(didTap);
        }
        finally
        {
            scale.Dispose();
            tap.Dispose();
        }
    }

    /// <summary>Regression test for flutter/flutter#78941: the first rotation must be reported.</summary>
    [Fact]
    public void FirstRotationIsReported()
    {
        var scale = new ScaleGestureRecognizer();
        double? updatedRotation = null;
        scale.OnUpdate = details => updatedRotation = details.Rotation;

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0));
            scale.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);

            PointerDownEvent down2 = Down(2, new Point(10.0, 10.0));
            scale.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.Null(updatedRotation);

            // Rotation by 45 degrees.
            Route(Move(2, new Point(0.0, 10.0)));
            AssertClose(Math.PI / 4.0, updatedRotation);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void ReportsThePointerCountOfEveryCallback()
    {
        var scale = new ScaleGestureRecognizer();
        int pointerCountOfStart = 0;
        int pointerCountOfUpdate = 0;
        int pointerCountOfEnd = 0;
        scale.OnStart = details => pointerCountOfStart = details.PointerCount;
        scale.OnUpdate = details => pointerCountOfUpdate = details.PointerCount;
        scale.OnEnd = details => pointerCountOfEnd = details.PointerCount;

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0));
            scale.AddPointer(down);
            _binding.GestureArena.Close(1);

            Route(down);
            Assert.Equal(1, pointerCountOfStart);
            Route(Move(1, new Point(20.0, 30.0)));
            Assert.Equal(1, pointerCountOfUpdate);

            PointerDownEvent down2 = Down(2, new Point(10.0, 20.0));
            scale.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            // The additional pointer going down ends the gesture in progress.
            Assert.Equal(2, pointerCountOfEnd);

            Route(Move(2, new Point(0.0, 10.0)));
            Assert.Equal(2, pointerCountOfStart);
            Assert.Equal(2, pointerCountOfUpdate);

            Route(Up(1, new Point(20.0, 30.0)));
            Assert.Equal(1, pointerCountOfEnd);

            Route(Move(2, new Point(0.0, 10.0)));
            Assert.Equal(1, pointerCountOfStart);
            Assert.Equal(1, pointerCountOfUpdate);

            Route(Up(2, new Point(0.0, 10.0)));
            Assert.Equal(0, pointerCountOfEnd);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void RecognizesScaleGesturesFromPointerPanZoomEvents()
    {
        var scale = new ScaleGestureRecognizer();
        var drag = new HorizontalDragGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerPanZoomStartEvent start = PanZoomStart(2, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(start);
            drag.AddPointerPanZoom(start);
            _binding.GestureArena.Close(2);
            Assert.False(log.DidStart);

            Route(start);
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.False(log.DidEnd);

            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(20.0, 30.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(20.0, 30.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            // Zoom in.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(20.0, 30.0), scale: 2.0));
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(2.0, log.Scale);
            AssertClose(2.0, log.HorizontalScale);
            AssertClose(2.0, log.VerticalScale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            // Zoom out.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(20.0, 30.0)));
            AssertPoint(new Point(20.0, 30.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            Assert.False(log.DidEnd);
            log.Reset();

            // We are done.
            Route(PanZoomEnd(2, new Point(0.0, 0.0)));
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);
            Assert.True(log.DidEnd);
        }
        finally
        {
            scale.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void PointerPanZoomsWorkAlongsideTouches()
    {
        var scale = new ScaleGestureRecognizer();
        var drag = new HorizontalDragGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerPanZoomStartEvent panZoomStart = PanZoomStart(4, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(panZoomStart);
            drag.AddPointerPanZoom(panZoomStart);
            _binding.GestureArena.Close(4);

            Route(panZoomStart);
            Assert.False(log.DidStart);

            Route(PanZoomUpdate(4, new Point(0.0, 0.0), pan: new Point(40.0, 40.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(40.0, 40.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(40.0, 40.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            // Add a touch pointer.
            PointerDownEvent touchStart1 = Down(2, new Point(40.0, 40.0));
            scale.AddPointer(touchStart1);
            drag.AddPointer(touchStart1);
            _binding.GestureArena.Close(2);
            Route(touchStart1);
            Assert.True(log.DidEnd);
            log.Reset();

            Route(Move(2, new Point(10.0, 10.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(25.0, 25.0), log.FocalPoint);
            // One pointer down plus a pan/zoom pans without scaling.
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(-15.0, -15.0), log.Delta);
            Assert.Equal(3, log.PointerCount);
            log.Reset();

            // Add a second touch pointer.
            PointerDownEvent touchStart2 = Down(3, new Point(10.0, 40.0));
            scale.AddPointer(touchStart2);
            drag.AddPointer(touchStart2);
            _binding.GestureArena.Close(3);
            Route(touchStart2);
            Assert.True(log.DidEnd);
            log.Reset();

            // Moving the second pointer causes pan, zoom and rotation at once.
            Route(Move(3, new Point(40.0, 40.0)));
            Assert.True(log.DidStart);
            AssertPoint(new Point(30.0, 30.0), log.FocalPoint);
            AssertClose(Math.Sqrt(2.0), log.Scale);
            AssertClose(1.0, log.HorizontalScale);
            AssertClose(1.0, log.VerticalScale);
            AssertPoint(new Point(10.0, 0.0), log.Delta);
            AssertClose(-Math.PI / 4.0, log.Rotation);
            Assert.Equal(4, log.PointerCount);
            log.Reset();

            // The pan/zoom scale multiplies with the pointer scale and the rotations add up.
            Route(PanZoomUpdate(
                4,
                new Point(0.0, 0.0),
                pan: new Point(40.0, 40.0),
                scale: Math.Sqrt(2.0),
                rotation: Math.PI / 3.0));
            Assert.False(log.DidStart);
            AssertPoint(new Point(30.0, 30.0), log.FocalPoint);
            AssertClose(2.0, log.Scale, 0.0001);
            AssertClose(Math.Sqrt(2.0), log.HorizontalScale);
            AssertClose(Math.Sqrt(2.0), log.VerticalScale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            AssertClose(Math.PI / 12.0, log.Rotation, 0.0001);
            Assert.Equal(4, log.PointerCount);
            log.Reset();

            // Moving the pan/zoom origin moves the focal point.
            Route(PanZoomUpdate(
                4,
                new Point(15.0, 15.0),
                pan: new Point(55.0, 55.0),
                scale: Math.Sqrt(2.0),
                rotation: Math.PI / 3.0));
            AssertPoint(new Point(40.0, 40.0), log.FocalPoint);
            AssertClose(2.0, log.Scale, 0.0001);
            AssertPoint(new Point(10.0, 10.0), log.Delta);
            AssertClose(Math.PI / 12.0, log.Rotation, 0.0001);
            Assert.Equal(4, log.PointerCount);
            log.Reset();

            // We are done.
            Route(PanZoomEnd(4, new Point(15.0, 15.0)));
            Assert.True(log.DidEnd);
            Assert.False(log.DidStart);
            log.Reset();
            Route(Up(2, new Point(10.0, 10.0)));
            Assert.False(log.DidEnd);
            Assert.False(log.DidStart);
            Route(Up(3, new Point(40.0, 40.0)));
            Assert.False(log.DidEnd);
            Assert.False(log.DidStart);
        }
        finally
        {
            scale.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void ScaleGestureCompetesWithDragForATrackpadGesture()
    {
        var scale = new ScaleGestureRecognizer();
        var drag = new HorizontalDragGestureRecognizer();
        var log = new List<string>();
        scale.OnStart = _ => log.Add("scale-start");
        scale.OnUpdate = _ => log.Add("scale-update");
        scale.OnEnd = _ => log.Add("scale-end");
        drag.OnStart = _ => log.Add("drag-start");
        drag.OnEnd = _ => log.Add("drag-end");

        try
        {
            PointerPanZoomStartEvent down = PanZoomStart(2, new Point(10.0, 10.0));
            scale.AddPointerPanZoom(down);
            drag.AddPointerPanZoom(down);
            _binding.GestureArena.Close(2);
            Assert.Empty(log);

            Route(down);
            Assert.Empty(log);

            // A pan of 40 exceeds the pan slop of 18.0 * 2.
            Route(PanZoomUpdate(2, new Point(10.0, 10.0), pan: new Point(10.0, 40.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            PointerPanZoomStartEvent down2 = PanZoomStart(3, new Point(10.0, 20.0));
            scale.AddPointerPanZoom(down2);
            drag.AddPointerPanZoom(down2);
            _binding.GestureArena.Close(3);
            Assert.Empty(log);

            Route(down2);
            Assert.Equal(["scale-end"], log);
            log.Clear();

            Route(PanZoomUpdate(3, new Point(10.0, 20.0), pan: new Point(20.0, 0.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            Route(PanZoomEnd(2, new Point(10.0, 10.0)));
            Assert.Equal(["scale-end"], log);
            log.Clear();

            Route(PanZoomEnd(3, new Point(10.0, 20.0)));
            Assert.Empty(log);

            PointerPanZoomStartEvent down3 = PanZoomStart(4, new Point(30.0, 30.0));
            scale.AddPointerPanZoom(down3);
            drag.AddPointerPanZoom(down3);
            _binding.GestureArena.Close(4);
            Route(down3);
            Assert.Empty(log);

            Route(PanZoomUpdate(4, new Point(30.0, 30.0), pan: new Point(70.0, 0.0)));
            Assert.Equal(["scale-start", "scale-update"], log);
            log.Clear();

            Route(PanZoomEnd(4, new Point(30.0, 30.0)));
            Assert.Equal(["scale-end"], log);
        }
        finally
        {
            scale.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void PanZoomGestureHonoursDragStartBehaviorStart()
    {
        var scale = new ScaleGestureRecognizer { DragStartBehavior = DragStartBehavior.Start };
        var drag = new HorizontalDragGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerPanZoomStartEvent start = PanZoomStart(2, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(start);
            drag.AddPointerPanZoom(start);
            _binding.GestureArena.Close(2);

            Route(start);
            Assert.False(log.DidStart);
            Assert.Null(log.Scale);

            // Zoom enough to win the gesture; the scale is rebased on the accepted value.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), scale: 1.1, rotation: 1.0));
            Assert.True(log.DidStart);
            AssertPoint(new Point(0.0, 0.0), log.FocalPoint);
            AssertClose(1.0, log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            Assert.False(log.DidEnd);
            log.Reset();

            // Zoom in, relative to 1.1.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), scale: 1.21, rotation: 1.5));
            AssertClose(1.1, log.Scale, 0.0001);
            AssertClose(1.1, log.HorizontalScale, 0.0001);
            AssertClose(1.1, log.VerticalScale, 0.0001);
            AssertClose(0.5, log.Rotation, 0.0001);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            log.Reset();

            // Zoom out, relative to 1.1.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), scale: 0.99, rotation: 1.0));
            AssertClose(0.9, log.Scale, 0.0001);
            AssertClose(0.9, log.HorizontalScale, 0.0001);
            AssertClose(0.9, log.VerticalScale, 0.0001);
            AssertClose(0.0, log.Rotation, 0.0001);
            log.Reset();

            Route(PanZoomEnd(2, new Point(0.0, 0.0)));
            Assert.False(log.DidStart);
            Assert.True(log.DidEnd);
        }
        finally
        {
            scale.Dispose();
            drag.Dispose();
        }
    }

    [Fact]
    public void TrackpadScrollCausesScaleConvertsPanIntoScale()
    {
        var scale = new ScaleGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Start,
            TrackpadScrollCausesScale = true,
        };
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerPanZoomStartEvent start = PanZoomStart(2, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(start);
            _binding.GestureArena.Close(2);
            Assert.False(log.DidStart);

            Route(start);
            Assert.True(log.DidStart);
            Assert.Null(log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.FocalPoint);
            Assert.Equal(2, log.PointerCount);
            Assert.False(log.DidEnd);
            log.Reset();

            // Zoom in by scrolling up: 200 pixels is one e-fold.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(0.0, -200.0)));
            Assert.False(log.DidStart);
            AssertPoint(new Point(0.0, 0.0), log.FocalPoint);
            AssertClose(Math.E, log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            Assert.Equal(2, log.PointerCount);
            log.Reset();

            // A horizontal scroll does nothing.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(200.0, -200.0)));
            AssertClose(Math.E, log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            log.Reset();

            Route(PanZoomEnd(2, new Point(0.0, 0.0)));
            Assert.True(log.DidEnd);
            log.Reset();

            // The factor is settable, and it flips which axis scales.
            scale.TrackpadScrollToScaleFactor = new Point(1.0 / 125.0, 0.0);

            PointerPanZoomStartEvent start2 = PanZoomStart(2, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(start2);
            _binding.GestureArena.Close(2);
            Route(start2);
            Assert.True(log.DidStart);
            Assert.Null(log.Scale);
            log.Reset();

            // Zoom in by scrolling left.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(125.0, 0.0)));
            AssertClose(Math.E, log.Scale);
            AssertPoint(new Point(0.0, 0.0), log.Delta);
            log.Reset();

            // A vertical scroll now does nothing.
            Route(PanZoomUpdate(2, new Point(0.0, 0.0), pan: new Point(125.0, 125.0)));
            AssertClose(Math.E, log.Scale);
            log.Reset();

            Route(PanZoomEnd(2, new Point(0.0, 0.0)));
            Assert.True(log.DidEnd);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void ReportsTheScaleVelocityWhenTheGestureEnds()
    {
        var scale = new ScaleGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Start,
            TrackpadScrollCausesScale = true,
        };
        var log = new ScaleLog();
        log.Attach(scale);

        try
        {
            PointerPanZoomStartEvent start = PanZoomStart(2, new Point(0.0, 0.0));
            scale.AddPointerPanZoom(start);
            _binding.GestureArena.Close(2);

            Route(start);
            Assert.True(log.DidStart);
            log.Reset();

            // Zoom in by scrolling up over 2.5 seconds.
            for (int i = 0; i < 100; i++)
            {
                Route(PanZoomUpdate(
                    2,
                    new Point(0.0, 0.0),
                    pan: new Point(0.0, i * -10.0),
                    milliseconds: i * 25.0));
            }

            Route(PanZoomEnd(2, new Point(0.0, 0.0), milliseconds: 2500.0));
            Assert.True(log.DidEnd);
            AssertClose(281.41454098027765, log.ScaleVelocity, 1e-6);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void StartAndUpdateDetailsCarryTheSourceEventTimestamp()
    {
        var scale = new ScaleGestureRecognizer();
        var tap = new TapGestureRecognizer();
        var log = new ScaleLog();
        log.Attach(scale);
        bool didTap = false;
        // Without a callback the tap recognizer refuses the pointer, and a lone scale recognizer
        // would win the arena by default before the gesture ever moves.
        tap.OnTap = () => didTap = true;

        try
        {
            PointerDownEvent down = Down(1, new Point(0.0, 0.0), milliseconds: 10.0);
            scale.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Assert.Null(log.StartTimestamp);

            Route(down);
            Assert.Null(log.StartTimestamp);

            Route(Move(1, new Point(20.0, 30.0), milliseconds: 20.0));
            Assert.True(log.DidStart);
            // The start reports the timestamp of the pointer *down*, the update its own.
            Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(10.0), log.StartTimestamp);
            Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(20.0), log.UpdateTimestamp);
            Assert.Equal(1, scale.PointerCount);
            log.Reset();

            PointerDownEvent down2 = Down(2, new Point(10.0, 20.0), milliseconds: 30.0);
            scale.AddPointer(down2);
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.Equal(2, scale.PointerCount);
            Assert.True(log.DidEnd);
            Assert.Null(log.StartTimestamp);
            log.Reset();

            // A restart in the middle of a sequence reports the timestamp of the event that
            // restarted it, not of the original down.
            Route(Move(2, new Point(0.0, 10.0), milliseconds: 40.0));
            Assert.True(log.DidStart);
            Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(40.0), log.StartTimestamp);
            Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(40.0), log.UpdateTimestamp);
            log.Reset();

            // A plain update leaves the start untouched.
            Route(Move(2, new Point(15.0, 25.0), milliseconds: 50.0));
            Assert.False(log.DidStart);
            Assert.Null(log.StartTimestamp);
            Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(50.0), log.UpdateTimestamp);
            Assert.False(didTap);
        }
        finally
        {
            scale.Dispose();
            tap.Dispose();
        }
    }

    [Fact]
    public void StartDetailsCarryThePointerDeviceKind()
    {
        var scale = new ScaleGestureRecognizer();
        bool didStartScale = false;
        PointerDeviceKind? updatedKind = null;
        scale.OnStart = details =>
        {
            didStartScale = true;
            updatedKind = details.Kind;
        };
        scale.OnEnd = _ => didStartScale = false;

        try
        {
            int pointer = 1;
            foreach (PointerDeviceKind kind in new[]
                     {
                         PointerDeviceKind.Touch,
                         PointerDeviceKind.Mouse,
                         PointerDeviceKind.Stylus,
                         PointerDeviceKind.InvertedStylus,
                         PointerDeviceKind.Unknown,
                     })
            {
                PointerDownEvent down = Down(pointer, new Point(10.0, 20.0), kind);
                scale.AddPointer(down);
                _binding.GestureArena.Close(pointer);
                Route(down);
                Route(Move(pointer, new Point(20.0, 30.0), kind));
                Assert.True(didStartScale);
                Assert.Equal(kind, updatedKind);
                Route(Up(pointer, new Point(20.0, 30.0), kind));
                Assert.False(didStartScale);
                pointer++;
            }

            PointerPanZoomStartEvent panZoomStart = PanZoomStart(pointer, new Point(10.0, 20.0));
            scale.AddPointerPanZoom(panZoomStart);
            _binding.GestureArena.Close(pointer);
            Route(panZoomStart);
            Route(PanZoomUpdate(pointer, new Point(20.0, 30.0)));
            Assert.True(didStartScale);
            Assert.Equal(PointerDeviceKind.Trackpad, updatedKind);
            Route(PanZoomEnd(pointer, new Point(20.0, 30.0)));
            Assert.False(didStartScale);
        }
        finally
        {
            scale.Dispose();
        }
    }

    [Fact]
    public void GestureDetectorRoutesPointersToTheScaleRecognizer()
    {
        var log = new ScaleLog();
        using var harness = new WidgetHarness(new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onScaleStart: details =>
            {
                log.DidStart = true;
                log.FocalPoint = details.FocalPoint;
                log.PointerCount = details.PointerCount;
            },
            onScaleUpdate: details =>
            {
                log.Scale = details.Scale;
                log.FocalPoint = details.FocalPoint;
                log.PointerCount = details.PointerCount;
            },
            onScaleEnd: details =>
            {
                log.DidEnd = true;
                log.PointerCount = details.PointerCount;
            },
            child: new SizedBox(width: 240, height: 120)));

        // A lone recognizer wins the arena when it closes, so the gesture starts on the first down.
        harness.Dispatch(Down(1, new Point(100.0, 60.0)));
        Assert.True(log.DidStart);
        AssertPoint(new Point(100.0, 60.0), log.FocalPoint);
        Assert.Equal(1, log.PointerCount);
        log.Reset();

        // The second finger reconfigures the gesture: it ends and restarts.
        harness.Dispatch(Down(2, new Point(140.0, 60.0)));
        Assert.True(log.DidEnd);
        Assert.False(log.DidStart);
        log.Reset();

        harness.Dispatch(Move(2, new Point(180.0, 60.0)));
        Assert.True(log.DidStart);
        AssertClose(2.0, log.Scale);
        AssertPoint(new Point(140.0, 60.0), log.FocalPoint);
        Assert.Equal(2, log.PointerCount);
        log.Reset();

        harness.Dispatch(Up(1, new Point(100.0, 60.0)));
        Assert.True(log.DidEnd);
        harness.Dispatch(Up(2, new Point(180.0, 60.0)));
    }

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly TestRootElement _root;
        private readonly RenderView _renderView;
        private readonly PipelineOwner _pipeline;

        public WidgetHarness(Widget widget)
        {
            GestureBinding.Instance.ResetForTests();
            _root = new TestRootElement(widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            _renderView = new RenderView
            {
                Child = Assert.IsAssignableFrom<RenderBox>(_root.ChildElement?.RenderObject),
            };
            _pipeline = new PipelineOwner(_renderView);
            _pipeline.Attach(_renderView);
            _owner.FlushBuild();
            _pipeline.FlushLayout(new Size(240, 120));
        }

        public void Dispatch(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(_renderView, @event);
        }

        public void Dispose()
        {
            _root.Unmount();
            GestureBinding.Instance.ResetForTests();
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
