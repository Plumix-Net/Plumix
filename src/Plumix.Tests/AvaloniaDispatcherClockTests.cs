using System.Reflection;
using System.Runtime.ExceptionServices;
using Avalonia.Threading;
using Xunit;

// C#-only host infrastructure: Avalonia's UI-thread dispatcher created before it receives its platform
// implementation starts on another clock (AvaloniaDispatcherClock.cs, docs/ai/BACKLOG.md). Avalonia
// 12.1.4 needs the workaround, 12.1.6 converts due times itself; timers must be armed right on both.

namespace Plumix.Tests;

public sealed class AvaloniaDispatcherClockTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const long ImplementationClock = 1_000_000;
    private const long TimerInterval = 250;

    [Fact]
    public void ADispatcherGivenARunningImplementationArmsTimersOnTheImplementationClock()
    {
        // The dispatcher the test builds registers as the UI thread's only when there is none yet.
        _ = Dispatcher.UIThread;
        (long Skew, bool Synchronized, long? ArmedDueTime) result = default;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                // `Dispatcher.InitializeUIThreadDispatcher(new AndroidDispatcherImpl())`: the
                // implementation's clock is running, then `Dispatcher.UIThread` creates the dispatcher
                // without one and the implementation replaces the managed placeholder.
                IDispatcherImpl impl = DispatchProxy.Create<IDispatcherImpl, FixedClockDispatcherImpl>();
                var dispatcher = (Dispatcher)Activator.CreateInstance(
                    typeof(Dispatcher), Members, binder: null, args: [null], culture: null)!;
                typeof(Dispatcher).GetMethod("ReplaceImplementation", Members)!.Invoke(dispatcher, [impl]);
                long skew = ImplementationClock
                            - (long)typeof(Dispatcher).GetProperty("Now", Members)!.GetValue(dispatcher)!;

                bool synchronized = AvaloniaDispatcherClock.Synchronize(dispatcher);
                var timer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(TimerInterval), DispatcherPriority.Normal, dispatcher);
                timer.Start();
                result = (skew, synchronized, ((FixedClockDispatcherImpl)(object)impl).ArmedDueTime);
                timer.Stop();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.Start();
        thread.Join();
        failure?.Throw();

        Assert.True(result.Skew > ImplementationClock / 2, $"The clocks differed by {result.Skew} ms only.");
        Assert.True(result.Synchronized);
        // Starting a timer also queues its inactive operation, so the earliest deadline may be sooner.
        Assert.InRange(
            result.ArmedDueTime.GetValueOrDefault(), ImplementationClock, ImplementationClock + TimerInterval);
    }

    /// <summary>
    /// A stopped platform clock that records what it is armed with. A proxy, because Avalonia's reference
    /// assembly keeps <see cref="IDispatcherImpl"/> from being implemented in source.
    /// </summary>
    public class FixedClockDispatcherImpl : DispatchProxy
    {
        internal long? ArmedDueTime { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name)
            {
                case "get_CurrentThreadIsLoopThread":
                    return true;
                case "get_Now":
                    return ImplementationClock;
                case "UpdateTimer":
                    ArmedDueTime = (long?)args![0];
                    return null;
                default:
                    return null;
            }
        }
    }
}
