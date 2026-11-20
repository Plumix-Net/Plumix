using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit.Sdk;

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
    public Offset GetTopLeft(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(default);
    }

    /// <summary>Dart's <c>tester.getBottomRight</c>.</summary>
    public Offset GetBottomRight(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Offset(box.Size.Width, box.Size.Height));
    }

    /// <summary>Dart's <c>tester.getRect</c>.</summary>
    public Rect GetRect(Element element) => Rect.FromPoints(GetTopLeft(element), GetBottomRight(element));

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
        Offset downLocation,
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
        // Awaiting flutter_test's guarded event dispatcher gives each event a microtask boundary.
        EventLoop.FlushMicrotasks();
    }

    /// <summary>Dart's <c>tester.drag(finder, offset)</c>.</summary>
    public void Drag(
        Element element,
        Offset offset,
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
        Offset startLocation,
        Offset offset,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double touchSlopX = DragSlopDefault,
        double touchSlopY = DragSlopDefault)
    {
        TestGesture gesture = StartGesture(startLocation, kind);
        double xSign = Math.Sign(offset.Dx);
        double ySign = Math.Sign(offset.Dy);
        double offsetX = offset.Dx;
        double offsetY = offset.Dy;
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
                    gesture.MoveBy(new Offset(signedSlopX, diffY));
                    if (Math.Abs(offsetY) <= touchSlopY)
                    {
                        gesture.MoveBy(new Offset(offsetX - signedSlopX, offsetY - diffY));
                    }
                    else
                    {
                        double diffY2 = signedSlopY - diffY;
                        double diffX2 = inverseOffsetSlope * diffY2;
                        gesture.MoveBy(new Offset(diffX2, diffY2));
                        gesture.MoveBy(new Offset(offsetX - diffX2 - signedSlopX, offsetY - signedSlopY));
                    }
                }
                else
                {
                    // The drag goes through the horizontal edge of the box.
                    double diffX = Math.Abs(inverseOffsetSlope) * touchSlopY * xSign;
                    gesture.MoveBy(new Offset(diffX, signedSlopY));
                    if (Math.Abs(offsetX) <= touchSlopX)
                    {
                        gesture.MoveBy(new Offset(offsetX - diffX, offsetY - signedSlopY));
                    }
                    else
                    {
                        double diffX2 = signedSlopX - diffX;
                        double diffY2 = offsetSlope * diffX2;
                        gesture.MoveBy(new Offset(diffX2, diffY2));
                        gesture.MoveBy(new Offset(offsetX - signedSlopX, offsetY - diffY2 - signedSlopY));
                    }
                }
            }
            else
            {
                // The drag goes through the corner of the box.
                gesture.MoveBy(new Offset(signedSlopX, signedSlopY));
                gesture.MoveBy(new Offset(offsetX - signedSlopX, offsetY - signedSlopY));
            }
        }
        else
        {
            gesture.MoveBy(offset);
        }

        gesture.Up();
    }

    /// <summary>Dart's <c>tester.timedDrag(finder, offset, duration)</c>.</summary>
    public void TimedDrag(Element element, Offset offset, TimeSpan duration, double frequency = 60.0)
    {
        TimedDragFrom(GetCenter(element), offset, duration, frequency);
    }

    /// <summary>
    /// Dart's <c>tester.timedDragFrom</c>: a down, <c>duration * frequency + 1</c> evenly timed
    /// moves and an up, replayed through <see cref="HandlePointerEventRecord"/>.
    /// </summary>
    public void TimedDragFrom(Offset startLocation, Offset offset, TimeSpan duration, double frequency = 60.0)
    {
        int intervals = (int)(duration.Ticks / TimeSpan.TicksPerMicrosecond * frequency / 1E6);
        if (intervals <= 1)
        {
            throw new XunitException("timedDragFrom needs more than one interval.");
        }

        var timeStamps = new List<TimeSpan>();
        for (int t = 0; t <= intervals; t += 1)
        {
            timeStamps.Add(TimeSpan.FromTicks(duration.Ticks * t / intervals));
        }

        var offsets = new List<Offset> { startLocation };
        for (int t = 0; t <= intervals; t += 1)
        {
            offsets.Add(startLocation + offset * ((double)t / intervals));
        }

        TestGesture gesture = CreateGesture();
        var records = new List<(TimeSpan TimeDelay, Action Dispatch)>
        {
            (TimeSpan.Zero, () => gesture.Down(startLocation)),
        };
        for (int t = 0; t <= intervals; t += 1)
        {
            int index = t;
            records.Add((timeStamps[index], () => gesture.MoveTo(offsets[index + 1], timeStamps[index])));
        }

        records.Add((duration, () => gesture.Up(duration)));
        HandlePointerEventRecord(records);
    }

    /// <summary>
    /// flutter_test's <c>WidgetTester.handlePointerEventRecord</c>: a record already due is
    /// dispatched at once; otherwise a frame is pumped, the clock advances (without a frame) to the
    /// record's delay, and the record is dispatched. A final frame completes the gesture.
    /// </summary>
    private void HandlePointerEventRecord(IReadOnlyList<(TimeSpan TimeDelay, Action Dispatch)> records)
    {
        TimeSpan? startTime = null;
        foreach ((TimeSpan timeDelay, Action dispatch) in records)
        {
            TimeSpan now = _clock;
            startTime ??= now;
            // So that the first event is promised to receive a zero timeDiff.
            TimeSpan timeDiff = timeDelay - (now - startTime.Value);
            if (timeDiff < TimeSpan.Zero)
            {
                // Flush all past events.
                dispatch();
            }
            else
            {
                Pump();
                Delayed(timeDiff);
                dispatch();
            }
        }

        Pump();
    }

    /// <summary>flutter_test's <c>binding.delayed</c>: advances the clock without a frame.</summary>
    private void Delayed(TimeSpan duration)
    {
        _clock += duration;
        _timers?.Elapse(duration);
    }

    /// <summary>Dart's <c>tester.fling(finder, offset, speed)</c>.</summary>
    public void Fling(
        Element element,
        Offset offset,
        double speed,
        PointerButtons buttons = PointerButtons.Primary,
        TimeSpan? frameInterval = null,
        Offset initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        FlingFrom(GetCenter(element), offset, speed, buttons, frameInterval, initialOffset, initialOffsetDelay);
    }

    /// <summary>
    /// Dart's <c>tester.flingFrom</c>: fifty moves spread over <c>|offset| / speed</c> seconds,
    /// pumping whenever more than a frame interval elapsed, then an up.
    /// </summary>
    public void FlingFrom(
        Offset startLocation,
        Offset offset,
        double speed,
        PointerButtons buttons = PointerButtons.Primary,
        TimeSpan? frameInterval = null,
        Offset initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        const int moveCount = 50;
        double frameMicroseconds = (frameInterval ?? TimeSpan.FromMilliseconds(16)).TotalMicroseconds;
        TimeSpan delay = initialOffsetDelay ?? TimeSpan.FromSeconds(1);
        TestGesture gesture = CreateGesture(PointerDeviceKind.Touch, buttons);
        double timeStampDelta = 1000000.0 * offset.Distance / (moveCount * speed);
        double timeStamp = 0.0;
        double lastTimeStamp = timeStamp;
        gesture.Down(startLocation, Microseconds(timeStamp));
        if (initialOffset.Distance > 0.0)
        {
            gesture.MoveTo(startLocation + initialOffset, Microseconds(timeStamp));
            timeStamp += delay.TotalMicroseconds;
            Pump(delay);
        }

        for (int i = 0; i <= moveCount; i += 1)
        {
            Offset location = startLocation + initialOffset + (offset * i / moveCount);
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
        Offset location,
        Offset scrollDelta,
        PointerDeviceKind kind = PointerDeviceKind.Mouse)
    {
        var responses = new List<bool>();
        SendEventToBinding(new PointerScrollEvent(
            kind: kind,
            position: location,
            scrollDelta: new Offset(scrollDelta.Dx, scrollDelta.Dy),
            timestampUtc: EventTimeOrigin,
            onRespond: responses.Add,
            viewId: View.ViewId));
        return responses;
    }

    /// <summary>Dart's <c>testPointer.scrollInertiaCancel()</c>.</summary>
    public void SendScrollInertiaCancel(Offset location, PointerDeviceKind kind = PointerDeviceKind.Trackpad)
    {
        SendEventToBinding(new PointerScrollInertiaCancelEvent(
            kind: kind,
            position: location,
            timestampUtc: EventTimeOrigin,
            viewId: View.ViewId));
    }

    /// <summary>Dart's <c>tester.trackpadFling(finder, offset, speed)</c>.</summary>
    public void TrackpadFling(
        Element element,
        Offset offset,
        double speed,
        TimeSpan? frameInterval = null,
        Offset initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        TrackpadFlingFrom(GetCenter(element), offset, speed, frameInterval, initialOffset, initialOffsetDelay);
    }

    /// <summary>
    /// Dart's <c>tester.trackpadFlingFrom</c>: a trackpad pan/zoom whose cumulative pan reaches
    /// <paramref name="offset"/> over fifty updates spread over <c>|offset| / speed</c> seconds,
    /// pumping whenever more than a frame interval elapsed, then a pan/zoom end.
    /// </summary>
    public void TrackpadFlingFrom(
        Offset startLocation,
        Offset offset,
        double speed,
        TimeSpan? frameInterval = null,
        Offset initialOffset = default,
        TimeSpan? initialOffsetDelay = null)
    {
        const int moveCount = 50;
        double frameMicroseconds = (frameInterval ?? TimeSpan.FromMilliseconds(16)).TotalMicroseconds;
        TimeSpan delay = initialOffsetDelay ?? TimeSpan.FromSeconds(1);
        var pointer = new TestPointer(this, Interlocked.Increment(ref _nextGesturePointer), PointerDeviceKind.Trackpad);
        double timeStampDelta = 1000000.0 * offset.Distance / (moveCount * speed);
        double timeStamp = 0.0;
        double lastTimeStamp = timeStamp;
        SendEventToBinding(pointer.PanZoomStart(startLocation, Microseconds(timeStamp)));
        if (initialOffset.Distance > 0.0)
        {
            SendEventToBinding(pointer.PanZoomUpdate(
                startLocation,
                pan: (Offset)initialOffset,
                timeStamp: Microseconds(timeStamp)));
            timeStamp += delay.TotalMicroseconds;
            Pump(delay);
        }

        for (int i = 0; i <= moveCount; i += 1)
        {
            Offset pan = initialOffset + (offset * i / moveCount);
            SendEventToBinding(
                pointer.PanZoomUpdate(startLocation, pan: (Offset)pan, timeStamp: Microseconds(timeStamp)));
            timeStamp += timeStampDelta;
            if (timeStamp - lastTimeStamp > frameMicroseconds)
            {
                Pump(TimeSpan.FromMicroseconds(Math.Truncate(timeStamp - lastTimeStamp)));
                lastTimeStamp = timeStamp;
            }
        }

        SendEventToBinding(pointer.PanZoomEnd(Microseconds(timeStamp)));
    }

    private static TimeSpan Microseconds(double value) => TimeSpan.FromMicroseconds(Math.Round(value));
}

/// <summary>
/// flutter_test's <c>TestPointer</c>: builds pointer events for one simulated pointer without
/// dispatching them (pass them to <see cref="FrameworkDartTester.SendEventToBinding"/>). The device
/// defaults to 1 for a mouse and 0 otherwise, as in Dart.
/// </summary>
internal sealed class TestPointer
{
    private readonly FrameworkDartTester _tester;
    private Offset? _pan;

    public TestPointer(FrameworkDartTester tester, int pointer = 1, PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        _tester = tester;
        Pointer = pointer;
        Kind = kind;
        Device = kind == PointerDeviceKind.Mouse ? 1 : 0;
    }

    public int Pointer { get; }

    public PointerDeviceKind Kind { get; }

    public int Device { get; }

    /// <summary>The last location the pointer was given.</summary>
    public Offset? Location { get; private set; }

    /// <summary>Dart's <c>TestPointer.hover</c>.</summary>
    public PointerHoverEvent Hover(Offset newLocation, TimeSpan timeStamp = default)
    {
        Offset delta = Location is { } location ? (Offset)(newLocation - location) : default;
        Location = newLocation;
        return new PointerHoverEvent(
            pointer: Pointer,
            kind: Kind,
            position: newLocation,
            delta: delta,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Device);
    }

    /// <summary>Dart's <c>TestPointer.scroll</c>: a scroll signal at the current location.</summary>
    public PointerScrollEvent Scroll(Offset scrollDelta, TimeSpan timeStamp = default)
    {
        Offset location = Location ?? throw new InvalidOperationException("scroll needs a location (hover first).");
        return new PointerScrollEvent(
            kind: Kind,
            position: location,
            scrollDelta: new Offset(scrollDelta.Dx, scrollDelta.Dy),
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Device);
    }

    /// <summary>Dart's <c>TestPointer.scale</c>: a scale signal at the current location.</summary>
    public PointerScaleEvent Scale(double scale, TimeSpan timeStamp = default)
    {
        Offset location = Location ?? throw new InvalidOperationException("scale needs a location (hover first).");
        return new PointerScaleEvent(
            kind: Kind,
            position: location,
            scale: scale,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Device);
    }

    /// <summary>Dart's <c>TestPointer.panZoomStart</c>.</summary>
    public PointerPanZoomStartEvent PanZoomStart(Offset location, TimeSpan timeStamp = default)
    {
        Location = location;
        _pan = default(Offset);
        return new PointerPanZoomStartEvent(
            pointer: Pointer,
            position: location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Device);
    }

    /// <summary>Dart's <c>TestPointer.panZoomUpdate</c>: <paramref name="pan"/> is cumulative.</summary>
    public PointerPanZoomUpdateEvent PanZoomUpdate(
        Offset location,
        Offset pan = default,
        double scale = 1.0,
        double rotation = 0.0,
        TimeSpan timeStamp = default)
    {
        Offset previousPan = _pan ?? throw new InvalidOperationException("panZoomUpdate needs panZoomStart.");
        Location = location;
        _pan = pan;
        return new PointerPanZoomUpdateEvent(
            pointer: Pointer,
            position: location,
            timestampUtc: Stamp(timeStamp),
            pan: pan,
            panDelta: new Offset(pan.Dx - previousPan.Dx, pan.Dy - previousPan.Dy),
            scale: scale,
            rotation: rotation,
            viewId: _tester.View.ViewId,
            device: Device);
    }

    /// <summary>Dart's <c>TestPointer.panZoomEnd</c>.</summary>
    public PointerPanZoomEndEvent PanZoomEnd(TimeSpan timeStamp = default)
    {
        _pan = null;
        return new PointerPanZoomEndEvent(
            pointer: Pointer,
            position: Location ?? default,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Device);
    }

    private static DateTime Stamp(TimeSpan timeStamp) => FrameworkDartTester.EventTimeOrigin + timeStamp;
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
    private Offset? _pan;

    public TestGesture(FrameworkDartTester tester, int pointer, PointerDeviceKind kind, PointerButtons buttons)
    {
        _tester = tester;
        Pointer = pointer;
        _kind = kind;
        _buttons = buttons;
    }

    public int Pointer { get; }

    /// <summary>The pointer's last location.</summary>
    public Offset Location { get; private set; }

    /// <summary>Dart's <c>TestGesture.addPointer</c>.</summary>
    public void AddPointer(Offset location = default, TimeSpan timeStamp = default)
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
    public void Down(Offset location, TimeSpan timeStamp = default)
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
    public void MoveBy(Offset offset, TimeSpan timeStamp = default) => MoveTo(Location + offset, timeStamp);

    /// <summary>Dart's <c>TestGesture.moveTo</c>: a move while down, a hover otherwise.</summary>
    public void MoveTo(Offset location, TimeSpan timeStamp = default)
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

    /// <summary>Dart's <c>TestGesture.panZoomStart</c>: begins a trackpad pan/zoom at <paramref name="location"/>.
    /// </summary>
    public void PanZoomStart(Offset location, TimeSpan timeStamp = default)
    {
        if (_kind != PointerDeviceKind.Trackpad || _pan is not null)
        {
            throw new InvalidOperationException("panZoomStart needs an idle trackpad gesture.");
        }

        if (!_isAdded)
        {
            AddPointer(location, timeStamp);
        }

        Location = location;
        _pan = default(Offset);
        Send(new PointerPanZoomStartEvent(
            pointer: Pointer,
            position: location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.panZoomUpdate</c>.</summary>
    public void PanZoomUpdate(
        Offset location,
        Offset pan = default,
        double scale = 1.0,
        double rotation = 0.0,
        TimeSpan timeStamp = default)
    {
        Offset previousPan = _pan ?? throw new InvalidOperationException("panZoomUpdate needs panZoomStart.");
        Location = location;
        _pan = pan;
        Send(new PointerPanZoomUpdateEvent(
            pointer: Pointer,
            position: location,
            timestampUtc: Stamp(timeStamp),
            pan: pan,
            panDelta: new Offset(pan.Dx - previousPan.Dx, pan.Dy - previousPan.Dy),
            scale: scale,
            rotation: rotation,
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    /// <summary>Dart's <c>TestGesture.panZoomEnd</c>.</summary>
    public void PanZoomEnd(TimeSpan timeStamp = default)
    {
        if (_pan is null)
        {
            throw new InvalidOperationException("panZoomEnd needs panZoomStart.");
        }

        _pan = null;
        Send(new PointerPanZoomEndEvent(
            pointer: Pointer,
            position: Location,
            timestampUtc: Stamp(timeStamp),
            viewId: _tester.View.ViewId,
            device: Pointer));
    }

    private static DateTime Stamp(TimeSpan timeStamp) => FrameworkDartTester.EventTimeOrigin + timeStamp;

    private void Send(PointerEvent @event) => _tester.SendEventToBinding(@event);
}
