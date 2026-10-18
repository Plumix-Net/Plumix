using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>Creates on-screen test owners that share the binding's process-global focus manager.</summary>
internal static class TestBuildOwner
{
    // Reading an already-pumped result must never block the framework thread.
    internal static T Complete<T>(Task<T> future)
    {
        EventLoop.FlushMicrotasks();
        Xunit.Assert.True(future.IsCompleted, "The future requires more than the current microtask turn.");
        return future.GetAwaiter().GetResult();
    }

    internal static BuildOwner Create() => new(focusManager: FocusManager.Instance);
}
