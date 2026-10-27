using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Plumix.UI;

// C#-only host infrastructure: the desktop embedders' answers to `SystemNavigator.pop` and `SystemSound.play`.
// macOS: flutter/engine/src/flutter/shell/platform/darwin/macos/framework/Source/FlutterEngine.mm
// (`handleMethodCall:` — `[NSApp terminate:]`; `playSystemSound:` — `NSBeep` for alerts only).
// Windows: flutter/engine/src/flutter/shell/platform/windows/platform_handler.cc (`SystemSoundPlay` —
// `MessageBeep(MB_OK)` for alerts; no `SystemNavigator.pop`).
// Linux: flutter/engine/src/flutter/shell/platform/linux/fl_platform_handler.cc (`system_navigator_pop` —
// `quit_application`; `system_sound_play` — `gdk_display_beep` for alerts). No desktop embedder answers
// `HapticFeedback.vibrate`.

namespace Plumix;

/// <summary>The desktop embedders' platform plugin answers, used where no platform host installed its own.</summary>
/// <remarks>
/// They apply only to a classic desktop application lifetime on macOS, Windows or Linux, the processes a
/// desktop embedder runs; elsewhere (a headless test run, a mobile or browser host without handlers) the
/// calls stay unanswered.
/// </remarks>
internal static class DesktopPlatformPlugin
{
    private static IClassicDesktopStyleApplicationLifetime? DesktopLifetime =>
        OperatingSystem.IsMacOS() || OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
            ? Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            : null;

    /// <summary>
    /// Answers <c>SystemNavigator.pop</c> as the running desktop embedder does; false where the embedder
    /// does not implement it (Windows, or no desktop embedder).
    /// </summary>
    /// <remarks>
    /// macOS's <c>[NSApp terminate:]</c> asks the application delegate first, whose termination handler
    /// sends the framework <c>System.requestAppExit</c>; Avalonia's <c>TryShutdown</c> likewise closes the
    /// windows, whose closing the host hands to <c>WidgetsBinding.HandleRequestAppExit</c>. Linux's
    /// <c>quit_application</c> quits unconditionally. Both run after the current platform message.
    /// </remarks>
    internal static bool PopSystemNavigator()
    {
        if (DesktopLifetime is not { } lifetime)
        {
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            Dispatcher.UIThread.Post(() => lifetime.TryShutdown());
            return true;
        }

        if (OperatingSystem.IsLinux())
        {
            Dispatcher.UIThread.Post(() => lifetime.Shutdown());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Answers <c>SystemSound.play</c> as the running desktop embedder does: the system alert beep for
    /// <see cref="SystemSoundType.Alert"/>; desktops make no click or tick sounds.
    /// </summary>
    internal static void PlaySystemSound(SystemSoundType type)
    {
        if (type != SystemSoundType.Alert || DesktopLifetime is null)
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                AppKit.NSBeep();
            }
            else if (OperatingSystem.IsWindows())
            {
                _ = User32.MessageBeep(User32.MbOk);
            }
            else if (OperatingSystem.IsLinux())
            {
                BeepX11();
            }
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // No beep where the system library is missing (a Wayland-only session has no libX11).
        }
    }

    // gdk_display_beep on an X11 display: the default display's bell at its base volume. Avalonia does not
    // expose its own display connection, so the beep opens one of its own.
    private static void BeepX11()
    {
        IntPtr display = X11.XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            return;
        }

        try
        {
            _ = X11.XBell(display, 0);
            _ = X11.XFlush(display);
        }
        finally
        {
            _ = X11.XCloseDisplay(display);
        }
    }

    private static unsafe class AppKit
    {
        // Resolved at run time: a static import of the AppKit framework would make the iOS linker look for
        // it when an iOS application links this assembly.
        private const string Library = "/System/Library/Frameworks/AppKit.framework/AppKit";

        internal static void NSBeep()
        {
            if (!NativeLibrary.TryLoad(Library, out IntPtr library)
                || !NativeLibrary.TryGetExport(library, "NSBeep", out IntPtr beep))
            {
                return;
            }

            ((delegate* unmanaged<void>)beep)();
        }
    }

    private static class User32
    {
        internal const uint MbOk = 0;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MessageBeep(uint type);
    }

    private static class X11
    {
        private const string Library = "libX11.so.6";

        [DllImport(Library)]
        internal static extern IntPtr XOpenDisplay(IntPtr displayName);

        [DllImport(Library)]
        internal static extern int XBell(IntPtr display, int percent);

        [DllImport(Library)]
        internal static extern int XFlush(IntPtr display);

        [DllImport(Library)]
        internal static extern int XCloseDisplay(IntPtr display);
    }
}
