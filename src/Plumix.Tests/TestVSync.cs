using Plumix.Foundation;

// C#-only test infrastructure: flutter/packages/flutter_test/lib/src/test_vsync.dart (`TestVSync`).

namespace Plumix.Tests;

/// <summary>
/// flutter_test's <c>TestVSync</c>: a ticker provider that creates plain tickers, which the test's
/// pumps (or <see cref="Scheduler.PumpFrameForTests"/>) drive.
/// </summary>
internal sealed class TestVSync : ITickerProvider
{
    public Ticker CreateTicker(TickerCallback onTick) => new(onTick);
}
