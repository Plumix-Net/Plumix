using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: the gesture half of flutter_test's `WidgetController`
// (`startGesture`, `drag`/`dragFrom`, `timedDrag`, `fling`/`flingFrom`, `sendEventToBinding`) and its
// `TestGesture`/`TestPointer` pair, used by the Dart-ported scrollable tests.

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    /// <summary>flutter_test's <c>kDragSlopDefault</c>.</summary>
    public const double DragSlopDefault = 20.0;

    /// <summary>
    /// The instant flutter_test's <c>Duration.zero</c> event time stamp maps to. Dart's test pointers
    /// stamp every event with a caller-supplied <c>Duration</c> (zero by default), so gestures
    /// measure velocity from those stamps and never from the wall clock.
    /// </summary>
    public static readonly DateTime EventTimeOrigin = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static int _nextGesturePointer = 9000;

    /// <summary>Dart's <c>tester.getTopLeft</c>.</summary>
    public Point GetTopLeft(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(default);
    }

    /// <summary>Dart's <c>tester.getBottomRight</c>.</summary>
    public Point GetBottomRight(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(box.Size.Width, box.Size.Height));
    }

    /// <summary>Dart's <c>tester.getRect</c>.</summary>
    public Rect GetRect(Element element) => new(GetTopLeft(element), GetBottomRight(element));

    /// <summary>Dart's <c>tester.getSize</c>.</summary>
    public Size GetSize(Element element) => ((RenderBox)element.FindRenderObject()!).Size;

    /// <summary>Dart's <c>tester.createGesture</c>: a pointer that is not down yet.</summary>
    public TestGesture CreateGesture(
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        PointerButtons buttons = PointerButtons.Primary)
    {
        return new TestGesture(this, Interlocked.Increment(ref _nextGesturePointer), kind, buttons);
    }

    /// <summary>Dart's <c>tester.startGesture</c> with a <c>kind</c>.</summary>
    public TestGesture StartGesture(
        Point downLocation,
        PointerDeviceKind kind,
        PointerButtons buttons = PointerButtons.Primary)
    {
        TestGesture gesture = CreateGesture(kind, buttons);
        gesture.Down(downLocation);
        return gesture;
    }

    /// <summary>Dart's <c>tester.sendEventToBinding</c>.</summary>
    public void SendEventToBinding(PointerEvent @event)
    {
        GestureBinding.Instance.HandlePointerEvent(RenderView, @event);
    }

    /// <summary>Dart's <c>tester.drag(finder, offset)</c>.</summary>
    public void Drag(
        Element element,
        Vector offset,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double touchSlopX = DragSlopDefault,
        double touchSlopY = DragSlopDefault)
    {
        DragFrom(GetCenter(element), offset, kind, touchSlopX, touchSlopY);
    }

    /// <summary>
    /// Dart's <c>tester.dragFrom</c>: when the offset leaves the touch-slop box, the first move stops
    /// on the box edge (and, if needed, on the extension of the other edge) so recognizers see the
    /// slop break exactly where a real finger would.
    /// </summary>
    public void DragFrom(
        Point startLocation,
        Vector offset,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double touchSlopX = DragSlopDefault,
        double touchSlopY = DragSlopDefault)
    {
        TestGesture gesture = StartGesture(startLocation, kind);
        double xSign = Math.Sign(offset.X);
        double ySign = Math.Sign(offset.Y);
        double offsetX = offset.X;
        double offsetY = offset.Y;
        bool separateX = Math.Abs(offsetX) > touchSlopX && touchSlopX > 0;
        bool separateY = Math.Abs(offsetY) > touchSlopY && touchSlopY > 0;
        if (separateX || separateY)
        {
            double offsetSlope = offsetY / offsetX;
            double inverseOffsetSlope = offsetX / offsetY;
            double slopSlope = touchSlopY / touchSlopX;
            double absoluteOffsetSlope = Math.Abs(offsetSlope);
            double signedSlopX = touchSlopX * xSign;
            double signedSlopY = touchSlopY * ySign;
            if (absoluteOffsetSlope != slopSlope)
            {
                if (absoluteOffsetSlope < slopSlope)
                {
                    // The drag goes through the vertical edge of the box.
                    double diffY = Math.Abs(offsetSlope) * touchSlopX * ySign;
                    gesture.MoveBy(new Vector(signedSlopX, diffY));
                    if (Math.Abs(offsetY) <= touchSlopY)
                    {
                        gesture.MoveBy(new Vector(offsetX - signedSlopX, offsetY - diffY));
                    }
                    else
                    {
                        double diffY2 = signedSlopY - diffY;
                        double diffX2 = inverseOffsetSlope * diffY2;
                        gesture.MoveBy(new Vector(diffX2, diffY2));
                        gesture.MoveBy(new Vector(offsetX - diffX2 - signedSlopX, offsetY - signedSlopY));
                    }
                }
                else
                {
                    // The drag goes through the horizontal edge of the box.
                    double diffX = Math.Abs(inverseOffsetSlope) * touchSlopY * xSign;
                    gesture.MoveBy(new Vector(diffX, signedSlopY));
                    if (Math.Abs(offsetX) <= touchSlopX)
                    {
                        gesture.MoveBy(new Vector(offsetX - diffX, offsetY - signedSlopY));
                    }
                    else
                    {
                        double diffX2 = signedSlopX - diffX;
                        double diffY2 = offsetSlope * diffX2;
                        gesture.MoveBy(new Vector(diffX2, diffY2));
                        gesture.MoveBy(new Vector(offsetX - signedSlopX, offsetY - diffY2 - signedSlopY));
                    }
                }
            }
            else
            {
                // The drag goes through the corner of the box.
                gesture.MoveBy(new Vector(signedSlopX, signedSlopY));
                gesture.MoveBy(new Vector(offsetX - signedSlopX, offsetY - signedSlopY));
            }
        }
        else
        {
            gesture.MoveBy(offset);
        }

        gesture.Up();
    }

    /// <summary>Dart's <c>tester.timedDrag(finder, offset, duration)</c>.</summary>
    public void TimedDrag(Element element, Vector offset, TimeSpan duration, double frequency = 60.0)
    {
        TimedDragFrom(GetCenter(element), offset, duration, frequency);
    }

    /// <summary>
    /// Dart's <c>tester.timedDragFrom</c>: <c>duration * frequency</c> evenly timed moves, pumping
    /// the elapsed time between them.
    /// </summary>
    public void TimedDragFrom(Point startLocation, Vector offset, TimeSpan duration, double frequency = 60.0)
    {
        int intervals = (int)(duration.TotalSeconds * frequency) + 1;
        var timeStamps = new List<TimeSpan>();
        var offsets = new List<Vector>();
        for (int i = 0; i < intervals; i += 1)
        {
            timeStamps.Add(duration * i / (intervals - 1));
            offsets.Add(offset * i / (intervals - 1));
        }

        TestGesture gesture = CreateGesture();
        gesture.Down(startLocation, timeStamps[0]);
        for (int i = 1; i < intervals; i += 1)
        {
            gesture.MoveTo(startLocation + offsets[i], timeStamps[i]);
            Pump(timeStamps[i] - timeStamps[i - 1]);
        }

        gesture.Up(timeStamps[^1]);
    }

    /// <summary>Dart's <c>tester.fling(finder, offset, speed)</c>.</summary>
    public void Fling(
        Element element,
        Vector offset,
        double speed,
        PointerButtons buttons = PointerButtons.Primary,
        TimeSpan? frameInterval = null,
        Vector initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        FlingFrom(GetCenter(element), offset, speed, buttons, frameInterval, initialOffset, initialOffsetDelay);
    }

    /// <summary>
    /// Dart's <c>tester.flingFrom</c>: fifty moves spread over <c>|offset| / speed</c> seconds,
    /// pumping whenever more than a frame interval elapsed, then an up.
    /// </summary>
    public void FlingFrom(
        Point startLocation,
        Vector offset,
        double speed,
        PointerButtons buttons = PointerButtons.Primary,
        TimeSpan? frameInterval = null,
        Vector initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        const int moveCount = 50;
        double frameMicroseconds = (frameInterval ?? TimeSpan.FromMilliseconds(16)).TotalMicroseconds;
        TimeSpan delay = initialOffsetDelay ?? TimeSpan.FromSeconds(1);
        TestGesture gesture = CreateGesture(PointerDeviceKind.Touch, buttons);
        double timeStampDelta = 1000000.0 * offset.Length / (moveCount * speed);
        double timeStamp = 0.0;
        double lastTimeStamp = timeStamp;
        gesture.Down(startLocation, Microseconds(timeStamp));
        if (initialOffset.Length > 0.0)
        {
            gesture.MoveTo(startLocation + initialOffset, Microseconds(timeStamp));
            timeStamp += delay.TotalMicroseconds;
            Pump(delay);
        }

        for (int i = 0; i <= moveCount; i += 1)
        {
            Point location = startLocation + initialOffset + (offset * i / moveCount);
            gesture.MoveTo(location, Microseconds(timeStamp));
            timeStamp += timeStampDelta;
            if (timeStamp - lastTimeStamp > frameMicroseconds)
            {
                Pump(TimeSpan.FromMicroseconds(Math.Truncate(timeStamp - lastTimeStamp)));
                lastTimeStamp = timeStamp;
            }
        }

        gesture.Up(Microseconds(timeStamp));
    }

    /// <summary>
    /// Dart's <c>tester.sendEventToBinding(testPointer.scroll(delta))</c> with a mouse (or the given
    /// kind); returns the <c>allowPlatformDefault</c> values the event was answered with.
    /// </summary>
    public List<bool> SendPointerScroll(
        Point location,
        Vector scrollDelta,
        PointerDeviceKind kind = PointerDeviceKind.Mouse)
    {
        var responses = new List<bool>();
        SendEventToBinding(new PointerScrollEvent(
            kind: kind,
            position: location,
            scrollDelta: new Point(scrollDelta.X, scrollDelta.Y),
            timestampUtc: EventTimeOrigin,
            onRespond: responses.Add,
            viewId: View.ViewId));
        return responses;
    }

    /// <summary>Dart's <c>testPointer.scrollInertiaCancel()</c>.</summary>
    public void SendScrollInertiaCancel(Point location, PointerDeviceKind kind = PointerDeviceKind.Trackpad)
    {
        SendEventToBinding(new PointerScrollInertiaCancelEvent(
            kind: kind,
            position: location,
            timestampUtc: EventTimeOrigin,
            viewId: View.ViewId));
    }

    private static TimeSpan Microseconds(double value) => TimeSpan.FromMicroseconds(Math.Round(value));
}

/// <summary>
/// flutter_test's <c>TestGesture</c> over a <c>TestPointer</c>: events are dispatched straight to the
/// gesture binding with Dart's default <c>Duration.zero</c> time stamps unless given.
/// </summary>
internal sealed class TestGesture
{
    private readonly FrameworkDartTester _tester;
    private readonly PointerDeviceKind _kind;
    private readonly PointerButtons _buttons;
    private bool _isDown;
    private bool _isAdded;

    public TestGesture(FrameworkDartTester tester, int pointer, PointerDeviceKind kind, PointerButtons buttons)
    {
        _tester = tester;
        Pointer = pointer;
        _kind = kind;
        _buttons = buttons;
    }

    public int Pointer { get; }

    /// <summary>The pointer's last location.</summary>
    public Point Location { get; private set; }

    /// <summary>Dart's <c>TestGesture.addPointer</c>.</summary>
    public void AddPointer(Point location = default, TimeSpan timeStamp = default)
    {
        Location = location;
        _isAdded = true;
        Send(new PointerAddedEvent(
            pointer: Pointer,
            kind: _kind,
            position: location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.removePointer</c>.</summary>
    public void RemovePointer(TimeSpan timeStamp = default)
    {
        _isAdded = false;
        Send(new PointerRemovedEvent(
            pointer: Pointer,
            kind: _kind,
            position: Location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.down</c>.</summary>
    public void Down(Point location, TimeSpan timeStamp = default)
    {
        Location = location;
        _isDown = true;
        _isAdded = true;
        Send(new PointerDownEvent(
            pointer: Pointer,
            kind: _kind,
            position: location,
            buttons: _buttons,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.moveBy</c>.</summary>
    public void MoveBy(Vector offset, TimeSpan timeStamp = default) => MoveTo(Location + offset, timeStamp);

    /// <summary>Dart's <c>TestGesture.moveTo</c>: a move while down, a hover otherwise.</summary>
    public void MoveTo(Point location, TimeSpan timeStamp = default)
    {
        Location = location;
        if (_isDown)
        {
            Send(new PointerMoveEvent(
                pointer: Pointer,
                kind: _kind,
                position: location,
                buttons: _buttons,
                timestampUtc: Stamp(timeStamp),
                viewId: _tester.View.ViewId,
                device: Pointer));
        }
        else
        {
            if (!_isAdded)
            {
                AddPointer(location, timeStamp);
            }

            Send(new PointerHoverEvent(
                pointer: Pointer,
                kind: _kind,
                position: location,
                timestampUtc: Stamp(timeStamp),
                viewId: _tester.View.ViewId,
                device: Pointer));
        }
    }

    /// <summary>Dart's <c>TestGesture.up</c>.</summary>
    public void Up(TimeSpan timeStamp = default)
    {
        _isDown = false;
        Send(new PointerUpEvent(
            pointer: Pointer,
            kind: _kind,
            position: Location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.cancel</c>.</summary>
    public void Cancel(TimeSpan timeStamp = default)
    {
        _isDown = false;
        Send(new PointerCancelEvent(
            pointer: Pointer,
            kind: _kind,
            position: Location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    private static DateTime Stamp(TimeSpan timeStamp) => FrameworkDartTester.EventTimeOrigin + timeStamp;

    private void Send(PointerEvent @event) => _tester.SendEventToBinding(@event);
}
