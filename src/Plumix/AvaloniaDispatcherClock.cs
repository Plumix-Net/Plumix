using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Threading;

// C#-only host infrastructure; no Dart parity source. A workaround for an Avalonia 12 dispatcher defect
// (docs/ai/BACKLOG.md, "Avalonia dispatcher clock"): `Dispatcher.InitializeUIThreadDispatcher(impl)`
// receives an implementation whose clock is already running, and only then does `Dispatcher.UIThread`
// lazily create the dispatcher, without an implementation, so the dispatcher keeps a stopwatch of its
// own as its clock; `ReplaceImplementation` never re-points it. Timer due times are computed on that
// clock and armed on the implementation's, which runs ahead by however long the dispatcher took to
// come up. Android's implementation posts a timer that looks overdue at once, the dispatcher finds it
// not yet due and arms it again, so every timer busy-spins the main looper for that skew before it
// fires. A 16 ms frame timer then never lets the looper go idle, and Avalonia's render pass, which it
// moves to Input priority after a second without idle time, never runs again.

namespace Plumix;

/// <summary>
/// Makes Avalonia's UI-thread dispatcher read its timers' time from its platform implementation's
/// clock, the one the platform arms them on.
/// </summary>
internal static class AvaloniaDispatcherClock
{
    private static bool s_synchronized;

    /// <summary>Synchronizes <see cref="Dispatcher.UIThread"/> once per process.</summary>
    internal static void EnsureSynchronized()
    {
        if (s_synchronized)
        {
            return;
        }

        s_synchronized = true;
        Synchronize(Dispatcher.UIThread);
    }

    /// <summary>
    /// Re-points <paramref name="dispatcher"/>'s clock at its platform implementation's; returns whether
    /// it did. On an Avalonia without the defect the two are already the same clock, and one whose
    /// private members changed is left as it is.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, typeof(Dispatcher))]
    internal static bool Synchronize(Dispatcher dispatcher)
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo? timeProviderField = typeof(Dispatcher).GetField("_timeProvider", Instance);
        FieldInfo? implField = typeof(Dispatcher).GetField("_impl", Instance);
        if (timeProviderField?.FieldType != typeof(Func<long>)
            || implField?.GetValue(dispatcher) is not { } impl
            || implField.FieldType.GetProperty("Now")?.GetMethod is not { } nowGetter
            || nowGetter.ReturnType != typeof(long))
        {
            return false;
        }

        timeProviderField.SetValue(dispatcher, Delegate.CreateDelegate(typeof(Func<long>), impl, nowGetter));
        return true;
    }
}
