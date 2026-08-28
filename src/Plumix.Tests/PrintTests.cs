using Plumix.Foundation;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity tests for the ported `foundation/print.dart`, mapped from Flutter's own
/// `test/foundation/print_test.dart`. Flutter drives the throttle with `FakeAsync`; Plumix drives
/// it through the internal timer seam.
/// </summary>
public class PrintTests
{
    [Fact]
    public void DebugPrintSynchronouslyPrintsOneEntryPerCall()
    {
        Assert.Equal(["Hello, world"], Capture(() => Print.DebugPrintSynchronously("Hello, world")));

        // A wrapped message is still a single `print` call, with the wrap points as newlines.
        Assert.Equal(
            ["Hello,\nworld"],
            Capture(() => Print.DebugPrintSynchronously("Hello, world", wrapWidth: 10)));
    }

    [Fact]
    public void DebugPrintSynchronouslyWrapsAtEveryWidthUpToTheWordBoundary()
    {
        for (int i = 0; i < 14; i++)
        {
            int width = i;
            Assert.Equal(
                ["Hello,\nworld"],
                Capture(() => Print.DebugPrintSynchronously("Hello,   world", wrapWidth: width)));
        }
    }

    [Fact]
    public void DebugPrintThrottledPrintsOneEntryPerLine()
    {
        Assert.Equal(["Hello, world"], Capture(() => Print.DebugPrintThrottled("Hello, world")));

        Assert.Equal(
            ["Hello,", "world"],
            Capture(() => Print.DebugPrintThrottled("Hello, world", wrapWidth: 10)));
    }

    [Fact]
    public void DebugPrintCanPrintNull()
    {
        Assert.Equal(["null"], Capture(() => Print.DebugPrintThrottled(null)));
        Assert.Equal(["null"], Capture(() => Print.DebugPrintThrottled(null, wrapWidth: 80)));
        Assert.Equal(["null"], Capture(() => Print.DebugPrintSynchronously(null)));
    }

    [Fact]
    public void DebugPrintThrottlesOverTheCapacity()
    {
        var pending = new List<Action>();
        Action<TimeSpan, Action> previousTimer = Print.ScheduleTimer;
        Print.ScheduleTimer = (_, callback) => pending.Add(callback);
        try
        {
            List<string> log = Capture(() =>
                Print.DebugPrintThrottled(new string('A', 22528) + "\nB"));

            Assert.Single(log);
            Assert.Single(pending);
            Assert.False(Print.DebugPrintDone.IsCompleted);

            List<string> rest = Capture(() => pending[0]());
            Assert.Equal(["B"], rest);
            Assert.True(Print.DebugPrintDone.IsCompleted);
        }
        finally
        {
            Print.ScheduleTimer = previousTimer;
            Print.ResetThrottleForTesting();
        }
    }

    [Fact]
    public void DebugPrintThrottlesAcrossCalls()
    {
        var pending = new List<Action>();
        Action<TimeSpan, Action> previousTimer = Print.ScheduleTimer;
        Print.ScheduleTimer = (_, callback) => pending.Add(callback);
        try
        {
            List<string> log = Capture(() =>
            {
                Print.DebugPrintThrottled(new string('C', 22528));
                Print.DebugPrintThrottled("D");
            });

            Assert.Single(log);
            Assert.Single(pending);

            Assert.Equal(["D"], Capture(() => pending[0]()));
        }
        finally
        {
            Print.ScheduleTimer = previousTimer;
            Print.ResetThrottleForTesting();
        }
    }

    [Fact]
    public void DebugWordWrapKeepsShortAndStackTraceLinesIntact()
    {
        Assert.Equal(["short"], Print.DebugWordWrap("short", 80));
        Assert.Equal(
            ["#0      main (package:test/test.dart:1:1)"],
            Print.DebugWordWrap("#0      main (package:test/test.dart:1:1)", 10));
    }

    [Fact]
    public void DebugWordWrapReusesTheFirstLineIndent()
    {
        Assert.Equal(
            ["  one two", "  three", "  four"],
            Print.DebugWordWrap("  one two three four", 10));

        Assert.Equal(
            ["- one two", "  three"],
            Print.DebugWordWrap("- one two three", 10));

        Assert.Equal(
            ["1. one two", "   three"],
            Print.DebugWordWrap("1. one two three", 11));
    }

    [Fact]
    public void DebugWordWrapPutsAnOverlongWordOnItsOwnLine()
    {
        Assert.Equal(
            ["a", "bbbbbbbbbbbbbbbb", "c"],
            Print.DebugWordWrap("a bbbbbbbbbbbbbbbb c", 5));
    }

    [Fact]
    public void DebugWordWrapAppliesTheWrapIndent()
    {
        Assert.Equal(
            ["one two", ">>three", ">>four"],
            Print.DebugWordWrap("one two three four", 7, wrapIndent: ">>"));
    }

    private static List<string> Capture(Action body)
    {
        var log = new List<string>();
        Action<string> previousSink = Print.PrintLine;
        Print.PrintLine = log.Add;
        try
        {
            body();
        }
        finally
        {
            Print.PrintLine = previousSink;
        }

        return log;
    }
}
