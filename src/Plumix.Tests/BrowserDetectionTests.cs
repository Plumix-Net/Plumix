using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/web_ui/test/engine/browser_detect_test.dart
// (detectBrowserEngineByVendorAgent) and flutter/packages/flutter/lib/src/widgets/editable_text.dart
// (defaultSelectionWidthStyle).

namespace Plumix.Tests;

public sealed class BrowserDetectionTests : IDisposable
{
    private readonly BrowserEngine? _previousEngineOverride = BrowserDetection.Instance.DebugBrowserEngineOverride;

    public void Dispose()
    {
        BrowserDetection.Instance.DebugBrowserEngineOverride = _previousEngineOverride;
        BrowserDetection.Instance.DebugUserAgentOverride = null;
        PlatformDefaults.DebugIsWebOverride = null;
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    [Fact]
    public void DetectBrowserEngineByVendorAgent_DetectsBlink()
    {
        // Chrome Version 89.0.4389.90 (Official Build) (x86_64) / MacOS
        BrowserEngine engine = BrowserDetection.Instance.DetectBrowserEngineByVendorAgent(
            "Google Inc.",
            "mozilla/5.0 (macintosh; intel mac os x 11_2_3) applewebkit/537.36 "
            + "(khtml, like gecko) chrome/89.0.4389.90 safari/537.36");
        Assert.Equal(BrowserEngine.Blink, engine);
    }

    [Fact]
    public void DetectBrowserEngineByVendorAgent_DetectsFirefox()
    {
        // 85.0.2 (64-bit) / MacOS
        BrowserEngine engine = BrowserDetection.Instance.DetectBrowserEngineByVendorAgent(
            "",
            "mozilla/5.0 (macintosh; intel mac os x 10.16; rv:85.0) gecko/20100101 firefox/85.0");
        Assert.Equal(BrowserEngine.Firefox, engine);
    }

    [Fact]
    public void DetectBrowserEngineByVendorAgent_DetectsSafari()
    {
        BrowserEngine engine = BrowserDetection.Instance.DetectBrowserEngineByVendorAgent(
            "Apple Computer, Inc.",
            "mozilla/5.0 (macintosh; intel mac os x 10_15_6) applewebkit/605.1.15 "
            + "(khtml, like gecko) version/14.0.3 safari/605.1.15");
        Assert.Equal(BrowserEngine.Webkit, engine);
    }

    [Fact]
    public void IsEdge_ReadsTheUserAgent()
    {
        BrowserDetection.Instance.DebugUserAgentOverride =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0";
        Assert.True(BrowserDetection.Instance.IsEdge);
        BrowserDetection.Instance.DebugUserAgentOverride = "Mozilla/5.0 Chrome/120.0.0.0 Safari/537.36";
        Assert.False(BrowserDetection.Instance.IsEdge);
    }

    [Fact]
    public void WebBrowserDetectionIsSafari_IsFalseOffTheWeb()
    {
        BrowserDetection.Instance.DebugBrowserEngineOverride = BrowserEngine.Webkit;
        PlatformDefaults.DebugIsWebOverride = false;
        Assert.False(WebBrowserDetection.IsSafari);
        PlatformDefaults.DebugIsWebOverride = true;
        Assert.True(WebBrowserDetection.IsSafari);
    }

    [Theory]
    [InlineData(false, TargetPlatform.MacOS, BrowserEngine.Blink, BoxWidthStyle.Max)]
    [InlineData(false, TargetPlatform.Android, BrowserEngine.Webkit, BoxWidthStyle.Max)]
    [InlineData(true, TargetPlatform.IOS, BrowserEngine.Blink, BoxWidthStyle.Max)]
    [InlineData(true, TargetPlatform.MacOS, BrowserEngine.Webkit, BoxWidthStyle.Max)]
    [InlineData(true, TargetPlatform.MacOS, BrowserEngine.Blink, BoxWidthStyle.Tight)]
    [InlineData(true, TargetPlatform.Windows, BrowserEngine.Firefox, BoxWidthStyle.Tight)]
    public void DefaultSelectionWidthStyle_IsMaxForIOSAndSafariOnTheWeb(
        bool web, TargetPlatform platform, BrowserEngine engine, BoxWidthStyle expected)
    {
        PlatformDefaults.DebugIsWebOverride = web;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        BrowserDetection.Instance.DebugBrowserEngineOverride = engine;

        Assert.Equal(expected, EditableText.DefaultSelectionWidthStyle);
    }
}
