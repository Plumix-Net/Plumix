using Plumix.UI;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/ui/hooks.dart (`_beginFrame` calls
// `PlatformDispatcher._beginFrame`, then `_updateFrameData` with the animator's frame number).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FrameClockFrameDataTests : IDisposable
{
    private readonly PlatformDispatcher _dispatcher = PlatformDispatcher.Instance;
    private readonly Action<TimeSpan>? _previousBeginFrame = PlatformDispatcher.Instance.OnBeginFrame;
    private readonly Action? _previousDrawFrame = PlatformDispatcher.Instance.OnDrawFrame;
    private readonly Action? _previousFrameDataChanged = PlatformDispatcher.Instance.OnFrameDataChanged;
    private readonly FrameData _previousFrameData = PlatformDispatcher.Instance.FrameData;

    public FrameClockFrameDataTests()
    {
        SchedulerBinding.ResetForTests();
        FrameClock.ResetForTests();
    }

    public void Dispose()
    {
        FrameClock.ResetForTests();
        _dispatcher.OnBeginFrame = _previousBeginFrame;
        _dispatcher.OnDrawFrame = _previousDrawFrame;
        _dispatcher.OnFrameDataChanged = _previousFrameDataChanged;
        _dispatcher.FrameData = _previousFrameData;
        SchedulerBinding.ResetForTests();
    }

    [Fact]
    public void Vsync_ReportsTheFrameNumberAfterOnBeginFrameAndBeforeOnDrawFrame()
    {
        _dispatcher.FrameData = new FrameData(frameNumber: -1);
        List<string> log = [];
        _dispatcher.OnBeginFrame = _ => log.Add($"begin {_dispatcher.FrameData.FrameNumber}");
        _dispatcher.OnFrameDataChanged = () => log.Add($"frameData {_dispatcher.FrameData.FrameNumber}");
        _dispatcher.OnDrawFrame = () => log.Add($"draw {_dispatcher.FrameData.FrameNumber}");

        FrameClock.RequestFrame();
        FrameClock.Tick();
        int first = _dispatcher.FrameData.FrameNumber;
        FrameClock.RequestFrame();
        FrameClock.Tick();
        int second = _dispatcher.FrameData.FrameNumber;

        Assert.True(first > 0);
        Assert.True(second > first);
        Assert.Equal(
            [
                "begin -1", $"frameData {first}", $"draw {first}",
                $"begin {first}", $"frameData {second}", $"draw {second}",
            ],
            log);
    }

    [Fact]
    public void UpdateFrameData_NotifiesOnlyAChangedFrameNumber()
    {
        _dispatcher.FrameData = new FrameData(frameNumber: -1);
        int notifications = 0;
        _dispatcher.OnFrameDataChanged = () => notifications++;

        _dispatcher.UpdateFrameData(7);
        _dispatcher.UpdateFrameData(7);

        Assert.Equal(7, _dispatcher.FrameData.FrameNumber);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void TickWithoutARequest_ProducesNoFrame()
    {
        _dispatcher.FrameData = new FrameData(frameNumber: -1);
        int begins = 0;
        _dispatcher.OnBeginFrame = _ => begins++;

        FrameClock.Tick();

        Assert.Equal(0, begins);
        Assert.Equal(-1, _dispatcher.FrameData.FrameNumber);
    }
}
