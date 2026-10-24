using System.Reflection;
using Avalonia.Threading;
using Xunit;

// C#-only host infrastructure: Avalonia's UI-thread dispatcher created after its platform
// implementation keeps a clock of its own (AvaloniaDispatcherClock.cs, docs/ai/BACKLOG.md).

namespace Plumix.Tests;

public sealed class AvaloniaDispatcherClockTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void ADispatcherCreatedAfterItsImplementationReadsTheImplementationClock()
    {
        // The dispatcher the test builds registers as the UI thread's only when there is none yet.
        _ = Dispatcher.UIThread;
        (long Skew, long SkewAfterSynchronize, bool Synchronized) result = default;
        var thread = new Thread(() =>
        {
            // `Dispatcher.InitializeUIThreadDispatcher(new AndroidDispatcherImpl())`: the implementation's
            // clock starts, then `Dispatcher.UIThread` creates the dispatcher without one and the
            // implementation replaces the managed placeholder.
            Type implType = typeof(Dispatcher).Assembly.GetType("Avalonia.Controls.Platform.ManagedDispatcherImpl")!;
            object impl = Activator.CreateInstance(implType, [null])!;
            Thread.Sleep(60);
            var dispatcher = (Dispatcher)Activator.CreateInstance(
                typeof(Dispatcher), Members, binder: null, args: [null], culture: null)!;
            typeof(Dispatcher).GetMethod("ReplaceImplementation", Members)!.Invoke(dispatcher, [impl]);

            long Skew() => (long)implType.GetProperty("Now")!.GetValue(impl)!
                           - (long)typeof(Dispatcher).GetProperty("Now", Members)!.GetValue(dispatcher)!;

            long skew = Skew();
            bool synchronized = AvaloniaDispatcherClock.Synchronize(dispatcher);
            result = (skew, Skew(), synchronized);
        });
        thread.Start();
        thread.Join();

        Assert.True(result.Skew >= 50, $"The dispatcher clock lagged by {result.Skew} ms only.");
        Assert.True(result.Synchronized);
        Assert.InRange(result.SkewAfterSynchronize, 0, 1);
    }
}
