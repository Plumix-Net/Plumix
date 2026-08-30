using Plumix.Widgets;

// Test infrastructure (C#-only): the slice of `WidgetsApp` that makes traversal work, so tests can
// install it without mounting a whole app.

namespace Plumix.Tests;

internal static class AppTraversalScope
{
    /// <summary>
    /// Wraps <paramref name="child"/> in what <c>WidgetsApp</c> installs above every application:
    /// the default shortcut map, the default actions and the root <see cref="FocusTraversalGroup"/>.
    /// Flutter's own focus tests reach these through <c>MaterialApp</c>/<c>WidgetsApp</c>; without
    /// them Tab and the arrow keys move nothing and <c>FocusTraversalGroup.Of</c> throws.
    /// </summary>
    public static Widget Wrap(Widget child)
    {
        return new Shortcuts(
            shortcuts: WidgetsApp.DefaultShortcuts,
            debugLabel: "<Default WidgetsApp Shortcuts>",
            child: new Actions(
                actions: WidgetsApp.DefaultActions,
                child: new FocusTraversalGroup(
                    policy: new ReadingOrderTraversalPolicy(),
                    child: new FocusScope(child))));
    }
}
