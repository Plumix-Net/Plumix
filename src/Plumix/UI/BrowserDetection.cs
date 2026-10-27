// Dart parity source: flutter/engine/src/flutter/lib/web_ui/lib/ui_web/src/ui_web/browser_detection.dart
// (BrowserEngine and the engine detection of BrowserDetection; the navigator is reported by the host)

namespace Plumix.UI;

/// <summary>The HTML engine used by the current browser.</summary>
/// <remarks>dart:ui_web's <c>BrowserEngine</c>.</remarks>
public enum BrowserEngine
{
    /// <summary>
    /// The engine that powers Chrome, Samsung Internet Browser, UC Browser, Microsoft Edge, Opera, and
    /// others. Blink is assumed when a more precise browser engine wasn't detected.
    /// </summary>
    Blink,

    /// <summary>The engine that powers Safari.</summary>
    Webkit,

    /// <summary>The engine that powers Firefox.</summary>
    Firefox,
}

/// <summary>The core browser detection functionality of the Flutter web engine.</summary>
/// <remarks>
/// dart:ui_web's <c>BrowserDetection</c>. The web engine reads <c>window.navigator</c> itself; Plumix's
/// core has no DOM access, so a browser host reports the navigator's <c>vendor</c> and <c>userAgent</c>
/// through <see cref="ReportNavigator"/> before the app starts. Until it does, both read as empty strings.
/// </remarks>
public sealed class BrowserDetection
{
    private string _vendor = string.Empty;
    private string _userAgent = string.Empty;
    private BrowserEngine? _browserEngine;

    private BrowserDetection()
    {
    }

    /// <summary>The singleton instance of the <see cref="BrowserDetection"/> class.</summary>
    public static BrowserDetection Instance { get; } = new();

    /// <summary>The user agent of the current browser.</summary>
    public string UserAgent => DebugUserAgentOverride ?? _userAgent;

    /// <summary>Override value for <see cref="UserAgent"/>; <see langword="null"/> uses the reported one.</summary>
    public string? DebugUserAgentOverride { get; set; }

    /// <summary>The <see cref="UI.BrowserEngine"/> used by the current browser.</summary>
    /// <remarks>This is used to implement browser-specific behavior.</remarks>
    public BrowserEngine BrowserEngine => DebugBrowserEngineOverride ?? (_browserEngine ??= DetectBrowserEngine());

    /// <summary>
    /// Override the value of <see cref="BrowserEngine"/>; <see langword="null"/> lets it detect the browser
    /// that the app is running on.
    /// </summary>
    public BrowserEngine? DebugBrowserEngineOverride { get; set; }

    /// <summary>Whether the current <see cref="BrowserEngine"/> is <see cref="UI.BrowserEngine.Blink"/>.</summary>
    public bool IsChromium => BrowserEngine == BrowserEngine.Blink;

    /// <summary>Whether the current <see cref="BrowserEngine"/> is <see cref="UI.BrowserEngine.Webkit"/>.</summary>
    public bool IsSafari => BrowserEngine == BrowserEngine.Webkit;

    /// <summary>Whether the current <see cref="BrowserEngine"/> is <see cref="UI.BrowserEngine.Firefox"/>.</summary>
    public bool IsFirefox => BrowserEngine == BrowserEngine.Firefox;

    /// <summary>Whether the current browser is Edge.</summary>
    public bool IsEdge => UserAgent.Contains("Edg/", StringComparison.Ordinal);

    /// <summary>
    /// Reports the browser's <c>navigator.vendor</c> and <c>navigator.userAgent</c>, which the web engine
    /// reads from the DOM. C#-only: the browser host calls it.
    /// </summary>
    public void ReportNavigator(string vendor, string userAgent)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(userAgent);
        _vendor = vendor;
        _userAgent = userAgent;
        _browserEngine = null;
    }

    /// <summary>Detects the browser engine for a given vendor and agent string.</summary>
    public BrowserEngine DetectBrowserEngineByVendorAgent(string vendor, string agent)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(agent);
        if (vendor == "Google Inc.")
        {
            return BrowserEngine.Blink;
        }

        if (vendor == "Apple Computer, Inc.")
        {
            return BrowserEngine.Webkit;
        }

        if (agent.Contains("Edg/", StringComparison.Ordinal))
        {
            // Chromium based Microsoft Edge has `Edg` in the user-agent.
            // https://docs.microsoft.com/en-us/microsoft-edge/web-platform/user-agent-string
            return BrowserEngine.Blink;
        }

        if (vendor.Length == 0 && agent.Contains("firefox", StringComparison.Ordinal))
        {
            // An empty string means firefox:
            // https://developer.mozilla.org/en-US/docs/Web/API/Navigator/vendor
            return BrowserEngine.Firefox;
        }

        // Assume Blink otherwise, but issue a warning.
        Console.WriteLine(
            "WARNING: failed to detect current browser engine. Assuming this is a Chromium-compatible browser.");
        return BrowserEngine.Blink;
    }

    // The engine lower-cases the user agent before matching, so its `Edg/` check never matches; kept 1:1.
    private BrowserEngine DetectBrowserEngine() =>
        DetectBrowserEngineByVendorAgent(_vendor, UserAgent.ToLowerInvariant());
}
