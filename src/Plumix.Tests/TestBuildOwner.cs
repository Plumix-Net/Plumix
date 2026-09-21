using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>Creates on-screen test owners that share the binding's process-global focus manager.</summary>
internal static class TestBuildOwner
{
    internal static BuildOwner Create() => new(focusManager: FocusManager.Instance);
}
