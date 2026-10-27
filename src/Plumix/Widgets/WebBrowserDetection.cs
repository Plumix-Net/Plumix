using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_web_browser_detection_web.dart
// (with the non-web `_web_browser_detection_io.dart` folded in behind `PlatformDefaults.IsWeb`)

namespace Plumix.Widgets;

/// <summary>A class that provides information about the current browser when on the web.</summary>
public static class WebBrowserDetection
{
    /// <summary>Whether the current browser is webkit (Safari). Always returns false on non-web platforms.</summary>
    public static bool IsSafari => PlatformDefaults.IsWeb && BrowserDetection.Instance.IsSafari;
}
