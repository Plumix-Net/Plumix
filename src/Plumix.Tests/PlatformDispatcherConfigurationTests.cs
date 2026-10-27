using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/ui/platform_dispatcher.dart
// (onPlatformConfigurationChanged: `_updateUserSettingsData`, `_updateLocales`, `_updateSemanticsEnabled`,
// `_updateAccessibilityFeatures`). The engine has no framework-level test of the callback; these pin the
// engine's order: the configuration callback first, then the specific one, and neither for a non-change.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlatformDispatcherConfigurationTests : IDisposable
{
    private readonly PlatformDispatcher _dispatcher = PlatformDispatcher.Instance;
    private readonly List<string> _calls = [];
    private readonly Action? _onTextScaleFactorChanged;
    private readonly Action? _onLocaleChanged;
    private readonly Action? _onSemanticsEnabledChanged;
    private readonly Action? _onAccessibilityFeaturesChanged;
    private readonly Action? _onPlatformBrightnessChanged;
    private readonly IReadOnlyList<Locale> _locales;
    private readonly bool _semanticsEnabled;
    private readonly int _accessibilityFeatures;

    public PlatformDispatcherConfigurationTests()
    {
        _onTextScaleFactorChanged = _dispatcher.OnTextScaleFactorChanged;
        _onLocaleChanged = _dispatcher.OnLocaleChanged;
        _onSemanticsEnabledChanged = _dispatcher.OnSemanticsEnabledChanged;
        _onAccessibilityFeaturesChanged = _dispatcher.OnAccessibilityFeaturesChanged;
        _onPlatformBrightnessChanged = _dispatcher.OnPlatformBrightnessChanged;
        _locales = _dispatcher.Locales;
        _semanticsEnabled = _dispatcher.SemanticsEnabled;
        _accessibilityFeatures = _dispatcher.ReportedAccessibilityFeatures;
        _dispatcher.ResetScaledFontSizeForTests();
        _dispatcher.UpdateTextScaleFactor(1.0);

        _dispatcher.OnPlatformConfigurationChanged = () => _calls.Add("configuration");
        _dispatcher.OnTextScaleFactorChanged = () => _calls.Add("textScaleFactor");
        _dispatcher.OnLocaleChanged = () => _calls.Add("locale");
        _dispatcher.OnSemanticsEnabledChanged = () => _calls.Add("semanticsEnabled");
        _dispatcher.OnAccessibilityFeaturesChanged = () => _calls.Add("accessibilityFeatures");
        _dispatcher.OnPlatformBrightnessChanged = () => _calls.Add("platformBrightness");
    }

    public void Dispose()
    {
        _dispatcher.OnPlatformConfigurationChanged = null;
        _dispatcher.OnTextScaleFactorChanged = null;
        _dispatcher.OnLocaleChanged = null;
        _dispatcher.OnSemanticsEnabledChanged = null;
        _dispatcher.OnAccessibilityFeaturesChanged = null;
        _dispatcher.ResetScaledFontSizeForTests();
        _dispatcher.UpdateTextScaleFactor(1.0);
        _dispatcher.UpdateLocales(_locales);
        _dispatcher.UpdateSemanticsEnabled(_semanticsEnabled);
        _dispatcher.UpdateAccessibilityFeatures(_accessibilityFeatures);
        _dispatcher.OnTextScaleFactorChanged = _onTextScaleFactorChanged;
        _dispatcher.OnLocaleChanged = _onLocaleChanged;
        _dispatcher.OnSemanticsEnabledChanged = _onSemanticsEnabledChanged;
        _dispatcher.OnAccessibilityFeaturesChanged = _onAccessibilityFeaturesChanged;
        _dispatcher.OnPlatformBrightnessChanged = _onPlatformBrightnessChanged;
    }

    [Fact]
    public void UserSettings_NotifyTheConfigurationBeforeTheTextScaleFactor()
    {
        _dispatcher.UpdateTextScaleFactor(1.0);
        Assert.Empty(_calls);

        _dispatcher.UpdateTextScaleFactor(2.0);
        Assert.Equal(["configuration", "textScaleFactor"], _calls);
    }

    [Fact]
    public void UserSettings_WithAConfigurationId_NotifyTheConfigurationEvenWhenUnchanged()
    {
        // The font scaling function stands in for `_updateUserSettingsData`'s configurationId.
        _dispatcher.UpdateTextScaleFactor(1.0, static size => size);
        Assert.Equal(["configuration"], _calls);
    }

    [Fact]
    public void PlatformBrightness_NotifiesTheConfigurationFirst()
    {
        _dispatcher.NotifyPlatformBrightnessChanged();
        Assert.Equal(["configuration", "platformBrightness"], _calls);
    }

    [Fact]
    public void Locales_NotifyTheConfigurationBeforeTheLocale()
    {
        IReadOnlyList<Locale> locales = [new Locale("xx"), new Locale("yy")];
        _dispatcher.UpdateLocales(locales);
        _dispatcher.UpdateLocales(locales);
        Assert.Equal(["configuration", "locale"], _calls);
    }

    [Fact]
    public void SemanticsEnabled_NotifiesTheConfigurationBeforeSemanticsEnabled()
    {
        _dispatcher.UpdateSemanticsEnabled(!_semanticsEnabled);
        _dispatcher.UpdateSemanticsEnabled(!_semanticsEnabled);
        Assert.Equal(["configuration", "semanticsEnabled"], _calls);
    }

    [Fact]
    public void AccessibilityFeatures_NotifyTheConfigurationBeforeTheFeatures()
    {
        int features = _accessibilityFeatures ^ 1;
        _dispatcher.UpdateAccessibilityFeatures(features);
        _dispatcher.UpdateAccessibilityFeatures(features);
        Assert.Equal(["configuration", "accessibilityFeatures"], _calls);
    }
}
