using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_inspector.dart
// (`_WidgetInspectorButtonState`). Flutter has no test of the on-device buttons' tooltip timers; these
// check that they run on the tester's pump clock, as `Timer`s do under flutter_test's FakeAsync.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class WidgetInspectorButtonTests
{
    private static readonly TimeSpan Delay = WidgetInspectorButton.TooltipDelayDuration;

    [Fact]
    public void LongPress_ShowsTheTooltipAfterTheDelayAndHidesItAfterTheLongPressDuration()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Button(log));

        // The long press fires at the long-press timeout while the pointer is down; the tooltip
        // shows one delay after it.
        TestGesture gesture = tester.StartGesture(tester.GetCenter(Find.Text("button")), PointerDeviceKind.Touch);
        tester.Pump(Gestures.GestureConstants.LongPressTimeout);
        Assert.Empty(log);
        tester.Pump(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Empty(log);
        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible"], log);
        gesture.Up();

        tester.Pump(WidgetInspectorButton.TooltipShownOnLongPressDuration - TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible"], log);
        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible", "hidden"], log);
    }

    [Fact]
    public void Hover_ShowsAndHidesTheTooltipOneDelayAfterEnterAndExit()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: Button(log)));
        TestGesture mouse = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        mouse.AddPointer(location: new Avalonia.Point(1, 1));
        tester.Pump();

        mouse.MoveTo(tester.GetCenter(Find.Text("button")));
        tester.Pump(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Empty(log);
        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible"], log);

        // Leaving starts the hidden timer, which fires one delay later.
        mouse.MoveTo(new Avalonia.Point(1, 1));
        tester.Pump(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible"], log);
        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["visible", "hidden"], log);
    }

    [Fact]
    public void Dispose_CancelsThePendingTooltipTimers()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Button(log));
        tester.LongPress(Find.Text("button"));
        Assert.Equal(["visible"], log);

        tester.PumpWidget(new SizedBox());
        tester.Pump(WidgetInspectorButton.TooltipShownOnLongPressDuration);

        Assert.Equal(["visible"], log);
    }

    private static Widget Button(List<string> log) => new Directionality(
        TextDirection.Ltr,
        new WidgetInspectorButton(
            button: new SizedBox(width: 50.0, height: 50.0, child: new Text("button")),
            onTooltipVisible: () => log.Add("visible"),
            onTooltipHidden: () => log.Add("hidden")));
}
