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
//
// Avalonia 12.1.6 has the upstream fix (AvaloniaUI/Avalonia#22496): `Dispatcher.Now` is the
// implementation's clock plus `_clockOffset`, and due times are converted back when they are armed.
// The two clocks still read differently there, on purpose, so nothing may be re-pointed.

namespace Plumix;

/// <summary>
/// Makes sure Avalonia's UI-thread dispatcher arms its timers on its platform implementation's clock.
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
    /// Re-points <paramref name="dispatcher"/>'s clock at its platform implementation's where Avalonia
    /// has the defect; returns whether its timers are now armed on the implementation's clock. An
    /// Avalonia with the fix is left as it is, and so is one whose private members are unknown, which
    /// returns <see langword="false"/>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, typeof(Dispatcher))]
    internal static bool Synchronize(Dispatcher dispatcher)
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        if (typeof(Dispatcher).GetField("_clockOffset", Instance)?.FieldType == typeof(long))
        {
            return true;
        }

        FieldInfo? timeProviderField = typeof(Dispatcher).GetField("_timeProvider", Instance);
        FieldInfo? implField = typeof(Dispatcher).GetField("_impl", Instance);
        // IDispatcherImpl is [PrivateApi]; a constant name keeps it (and its Now) analyzable for the trimmer.
        Type? implType = Type.GetType("Avalonia.Threading.IDispatcherImpl, Avalonia.Base");
        if (timeProviderField?.FieldType != typeof(Func<long>)
            || implType is null
            || implField?.FieldType != implType
            || implField.GetValue(dispatcher) is not { } impl
            || implType.GetProperty("Now")?.GetMethod is not { } nowGetter
            || nowGetter.ReturnType != typeof(long))
        {
            return false;
        }

        timeProviderField.SetValue(dispatcher, Delegate.CreateDelegate(typeof(Func<long>), impl, nowGetter));
        return true;
    }
}
