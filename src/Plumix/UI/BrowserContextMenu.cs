using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/browser_context_menu.dart

namespace Plumix.UI;

/// <summary>Controls the browser's context menu on the web platform.</summary>
/// <remarks>Dart's private-constructor singleton is a static class: its only state is the enabled
/// flag.</remarks>
public static class BrowserContextMenu
{
    private static bool _enabled = true;

    private static MethodChannel Channel => SystemChannels.ContextMenu;

    /// <summary>Whether the browser's context menu is enabled.</summary>
    public static bool Enabled => _enabled;

    /// <summary>Disables the browser's context menu. Web only.</summary>
    public static Task DisableContextMenu()
    {
        AssertIsWeb();
        return Invoke();

        static async Task Invoke()
        {
            await Channel.InvokeMethod<object>("disableContextMenu");
            _enabled = false;
        }
    }

    /// <summary>Enables the browser's context menu. Web only.</summary>
    public static Task EnableContextMenu()
    {
        AssertIsWeb();
        return Invoke();

        static async Task Invoke()
        {
            await Channel.InvokeMethod<object>("enableContextMenu");
            _enabled = true;
        }
    }

    private static void AssertIsWeb()
    {
        if (Constants.KDebugMode && !PlatformDefaults.IsWeb)
        {
            throw new AssertionError("This has no effect on platforms other than web.");
        }
    }

    internal static void ResetForTests() => _enabled = true;
}
