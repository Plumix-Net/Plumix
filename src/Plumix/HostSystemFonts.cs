// C#-only infrastructure: the Avalonia host's analogue of the engine's system-font notification.
// Flutter's engine sends `{'type': 'fontsChange'}` on the `flutter/system` channel from
// `Shell::ReloadSystemFonts` (shell/common/shell.cc); on Windows the runner calls
// `FlutterEngine::ReloadSystemFonts` from its `WM_FONTCHANGE` handler. `PaintingBinding.HandleSystemMessage`
// turns that message into `PaintingBinding.SystemFonts` notifications.
//
// Avalonia exposes no font-change event. On Windows the host hooks `WM_FONTCHANGE` through
// `Win32Properties.AddWndProcHookCallback`; macOS, Linux, Android, iOS and the browser have no Avalonia
// signal, so nothing is sent there (docs/ai/BACKLOG.md).

using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix;

internal static class HostSystemFonts
{
    // WinUser.h `WM_FONTCHANGE`.
    private const uint WmFontChange = 0x001D;

    // A top-level keeps its hook for its whole lifetime: the hook holds no host state, and the window
    // handle that receives the broadcast goes away with the top-level.
    private static readonly ConditionalWeakTable<TopLevel, object> s_hookedTopLevels = new();

    /// <summary>Starts forwarding the top-level's native font-change messages, once per top-level.</summary>
    public static void Attach(TopLevel topLevel)
    {
        if (!OperatingSystem.IsWindows() || !s_hookedTopLevels.TryAdd(topLevel, topLevel))
        {
            return;
        }

        Win32Properties.AddWndProcHookCallback(topLevel, HandleWndProc);
    }

    /// <summary>Sends the engine's <c>fontsChange</c> system message to the framework.</summary>
    public static Task NotifySystemFontsChanged() =>
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.System.Name,
            SystemChannels.System.Codec.EncodeMessage(new Dictionary<string, object?> { ["type"] = "fontsChange" }),
            callback: null);

    private static IntPtr HandleWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmFontChange)
        {
            _ = NotifySystemFontsChanged();
        }

        return IntPtr.Zero;
    }
}
