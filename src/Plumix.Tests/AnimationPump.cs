// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/ticker.dart
// (Ticker._startTime), mirrored by flutter_test's WidgetTester.pump() usage in animation tests.

namespace Plumix.Tests;

/// <summary>
/// Frame helpers that mirror how Flutter's own animation tests drive time.
/// </summary>
/// <remarks>
/// A <see cref="Ticker"/> started outside a frame takes its start timestamp from the first frame it
/// sees, so that frame reports zero elapsed time — which is why Flutter's tests always pump once
/// before pumping a duration. <see cref="Prime"/> is that first pump.
/// </remarks>
internal static class AnimationPump
{
    /// <summary>Runs the frame that gives every freshly started ticker its start timestamp.</summary>
    public static void Prime()
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds));
    }

    /// <summary>Primes any freshly started ticker and then advances time by <paramref name="seconds"/>.</summary>
    public static void Advance(double seconds)
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + seconds));
    }
}
