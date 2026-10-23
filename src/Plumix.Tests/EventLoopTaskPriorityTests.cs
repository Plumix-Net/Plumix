using Avalonia.Threading;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only host infrastructure: dart:async's Timer.run is an event-loop task of the engine; Plumix
// posts it to Avalonia's dispatcher, whose low priorities are idle work on Android.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class EventLoopTaskPriorityTests : IDisposable
{
    private readonly Action<Action> _previousTimerRun = PlatformDispatcher.Instance.TimerRun;
    private readonly Action<Action, DispatcherPriority> _previousPost = PlatformDispatcher.PostToDispatcher;
    private readonly List<(Action Job, DispatcherPriority Priority)> _looper = [];
    private TimeSpan _vsync;

    public EventLoopTaskPriorityTests()
    {
        SchedulerBinding.ResetForTests();
        WidgetsBinding.Instance.ResetRootForTests();
        FrameClock.ResetForTests();
        PlatformDispatcher.PostToDispatcher = (job, priority) => _looper.Add((job, priority));
        PlatformDispatcher.Instance.TimerRun = PlatformDispatcher.DefaultTimerRun;
    }

    public void Dispose()
    {
        WidgetsBinding.Instance.ResetRootForTests();
        SchedulerBinding.ResetForTests();
        FrameClock.ResetForTests();
        PlatformDispatcher.Instance.TimerRun = _previousTimerRun;
        PlatformDispatcher.PostToDispatcher = _previousPost;
    }

    [Fact]
    public void TimerRun_IsForegroundWork()
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

    // Avalonia's Android dispatcher on a looper that is never idle: jobs above Input run in priority
    // order (FIFO within a priority); jobs at or below Input wait for an idle handler that never runs.
    private void RunJobsOfABusyLooper()
    {
        while (_looper.FindIndex(item => item.Priority > DispatcherPriority.Input) is var next && next >= 0)
        {
            int index = next;
            for (int candidate = next + 1; candidate < _looper.Count; candidate++)
            {
                if (_looper[candidate].Priority > _looper[index].Priority)
                {
                    index = candidate;
                }
            }

            Action job = _looper[index].Job;
            _looper.RemoveAt(index);
            job();
        }
    }

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
