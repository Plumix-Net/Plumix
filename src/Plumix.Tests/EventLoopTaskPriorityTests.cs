using Avalonia.Threading;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only host infrastructure: dart:async's Timer.run is an event-loop task of the engine; Plumix's
// timer queue wakes up on Avalonia's dispatcher, whose low priorities are idle work on Android.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class EventLoopTaskPriorityTests : IDisposable
{
    // The warm-up frame's Timer.run turns, on a fake clock: the queue's dispatcher wake-up runs at
    // EventLoopTaskPriority, which EventLoopTask_IsForegroundWork pins above the idle priorities.
    private readonly FakeGestureTimers _looper = new();
    private TimeSpan _vsync;

    public EventLoopTaskPriorityTests()
    {
        SchedulerBinding.ResetForTests();
        WidgetsBinding.Instance.ResetRootForTests();
        FrameClock.ResetForTests();
    }

    public void Dispose()
    {
        WidgetsBinding.Instance.ResetRootForTests();
        SchedulerBinding.ResetForTests();
        FrameClock.ResetForTests();
        _looper.Dispose();
    }

    [Fact]
    public void EventLoopTask_IsForegroundWork()
    {
        // Avalonia runs jobs at or below Input only when the platform reports no pending input, and
        // Android only from the looper's idle handler.
        Assert.True(PlatformDispatcher.EventLoopTaskPriority > DispatcherPriority.Input);
        Assert.True(PlatformDispatcher.EventLoopTaskPriority < DispatcherPriority.Send);
    }

    public static TheoryData<string> Indicators() => ["circular", "refresh"];

    [Theory]
    [MemberData(nameof(Indicators))]
    public void IndeterminateProgressIndicatorAsFirstFrame_DrawsWhileTheLooperIsNeverIdle(string indicator)
    {
        int frames = 0;
        SchedulerBinding.Instance.AddPersistentFrameCallback(_ => frames++);
        var host = new WidgetHost();
        // RefreshIndicator shows RefreshProgressIndicator while it refreshes.
        Widget spinner = indicator == "refresh" ? new RefreshProgressIndicator() : new CircularProgressIndicator();
        host.RunApplication(new MaterialApp(home: new Scaffold(body: new Center(child: spinner))));

        // The spinner's ticker requests a vsync on every frame, so an Android looper never goes idle:
        // only jobs above Input run, and vsync frames are dropped until the warm-up frame has drawn.
        for (int turn = 0; turn < 5 && frames == 0; turn++)
        {
            RunJobsOfABusyLooper();
            Vsync();
        }

        Assert.True(frames > 0, "The warm-up frame never drew.");
        int warmUpFrames = frames;
        for (int turn = 0; turn < 3; turn++)
        {
            RunJobsOfABusyLooper();
            Vsync();
        }

        Assert.True(frames > warmUpFrames, "The indicator stopped producing frames after the warm-up frame.");
        Assert.True(SchedulerBinding.Instance.TransientCallbackCount > 0);
    }

    // A busy Android looper still runs the event-loop turns, which are above Input.
    private void RunJobsOfABusyLooper() => _looper.RunDueTimers();

    private void Vsync()
    {
        if (!SchedulerBinding.Instance.HasScheduledFrame)
        {
            return;
        }

        _vsync += TimeSpan.FromMilliseconds(16);
        PlatformDispatcher.Instance.OnBeginFrame?.Invoke(_vsync);
        EventLoop.FlushMicrotasks();
        PlatformDispatcher.Instance.OnDrawFrame?.Invoke();
    }
}
