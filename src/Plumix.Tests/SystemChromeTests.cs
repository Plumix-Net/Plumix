using System.Collections;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/services/system_chrome.dart
// flutter/packages/flutter/lib/src/services/binding.dart (_handlePlatformMessage, setSystemUiChangeCallback)
// Tests: flutter/packages/flutter/test/services/system_chrome_test.dart

public sealed class SystemChromeTests : IDisposable
{
    public SystemChromeTests()
    {
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
    }

    // 'SystemChrome overlay style test'
    [Fact]
    public void OverlayStyle_IsSentOnceInAMicrotaskAndDeduplicated()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        // The first call is a cache miss and will queue a microtask.
        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Dark);
        Scheduler.FlushMicrotasks();
        platform.Log.Clear();

        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Light);
        Assert.Equal(1, Scheduler.MicrotaskCount);
        Scheduler.FlushMicrotasks();
        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setSystemUIOverlayStyle", call.Method);
        AssertMap(
            new Dictionary<string, object?>
            {
                ["systemNavigationBarColor"] = 4278190080L,
                ["systemNavigationBarDividerColor"] = null,
                ["systemStatusBarContrastEnforced"] = null,
                ["statusBarColor"] = null,
                ["statusBarBrightness"] = "Brightness.dark",
                ["statusBarIconBrightness"] = "Brightness.light",
                ["systemNavigationBarIconBrightness"] = "Brightness.light",
                ["systemNavigationBarContrastEnforced"] = null,
            },
            (IDictionary)call.Arguments!);
        Assert.Equal(0, Scheduler.MicrotaskCount);

        // The second call with the same value should be a no-op.
        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Light);
        Assert.Equal(0, Scheduler.MicrotaskCount);

        platform.Log.Clear();
        SystemChrome.SetSystemUIOverlayStyle(new SystemUiOverlayStyle(
            systemStatusBarContrastEnforced: false,
            systemNavigationBarContrastEnforced: true));
        Assert.Equal(1, Scheduler.MicrotaskCount);
        Scheduler.FlushMicrotasks();
        call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setSystemUIOverlayStyle", call.Method);
        AssertMap(
            new Dictionary<string, object?>
            {
                ["systemNavigationBarColor"] = null,
                ["systemNavigationBarDividerColor"] = null,
                ["systemStatusBarContrastEnforced"] = false,
                ["statusBarColor"] = null,
                ["statusBarBrightness"] = null,
                ["statusBarIconBrightness"] = null,
                ["systemNavigationBarIconBrightness"] = null,
                ["systemNavigationBarContrastEnforced"] = true,
            },
            (IDictionary)call.Arguments!);
    }

    // The coalescing half of `setSystemUIOverlayStyle`: the last call of a turn wins, and a turn that
    // ends on the style already sent sends nothing.
    [Fact]
    public void OverlayStyle_CallsInOneTurnCoalesceToTheLast()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Dark);
        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Light);
        Assert.Equal(1, Scheduler.MicrotaskCount);
        Scheduler.FlushMicrotasks();
        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("Brightness.light", ((IDictionary)call.Arguments!)["statusBarIconBrightness"]);
        Assert.Same(SystemUiOverlayStyle.Light, SystemChrome.LatestStyle);

        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Dark);
        SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Light);
        Scheduler.FlushMicrotasks();
        Assert.Single(platform.Log);
        Assert.Same(SystemUiOverlayStyle.Light, SystemChrome.LatestStyle);
    }

    // 'setPreferredOrientations control test'
    [Fact]
    public async Task SetPreferredOrientations_SendsTheEnumNames()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetPreferredOrientations([DeviceOrientation.PortraitUp]);

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setPreferredOrientations", call.Method);
        Assert.Equal(["DeviceOrientation.portraitUp"], ((IList)call.Arguments!).Cast<object>());
    }

    [Fact]
    public async Task SetPreferredOrientations_EmptyListDefersToTheSystem()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetPreferredOrientations([
            DeviceOrientation.LandscapeLeft,
            DeviceOrientation.PortraitDown,
            DeviceOrientation.LandscapeRight,
        ]);
        await SystemChrome.SetPreferredOrientations([]);

        Assert.Equal(
            ["DeviceOrientation.landscapeLeft", "DeviceOrientation.portraitDown", "DeviceOrientation.landscapeRight"],
            ((IList)platform.Log[0].Arguments!).Cast<object>());
        Assert.Empty((IList)platform.Log[1].Arguments!);
    }

    // 'setApplicationSwitcherDescription control test'
    [Fact]
    public async Task SetApplicationSwitcherDescription_SendsLabelAndColor()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetApplicationSwitcherDescription(
            new ApplicationSwitcherDescription(label: "Example label", primaryColor: 0xFF00FF00));

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setApplicationSwitcherDescription", call.Method);
        platform.AssertLastApplicationSwitcherDescription("Example label", 4278255360);
    }

    [Fact]
    public async Task SetApplicationSwitcherDescription_PassesNullsThrough()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetApplicationSwitcherDescription(new ApplicationSwitcherDescription());

        platform.AssertLastApplicationSwitcherDescription(null, null);
    }

    // 'setApplicationSwitcherDescription missing plugin'
    [Fact]
    public async Task SetApplicationSwitcherDescription_MissingPluginIsIgnored()
    {
        var log = new List<ByteData?>();
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        messenger.SetPlatformMessageHandler("flutter/platform", message =>
        {
            log.Add(message);
            return null;
        });
        try
        {
            await SystemChrome.SetApplicationSwitcherDescription(
                new ApplicationSwitcherDescription(label: "Example label", primaryColor: 0xFF00FF00));
        }
        finally
        {
            messenger.SetPlatformMessageHandler("flutter/platform", null);
        }

        Assert.NotEmpty(log);
    }

    // 'setEnabledSystemUIMode control test'
    [Fact]
    public async Task SetEnabledSystemUIMode_SendsTheModeName()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetEnabledSystemUIMode(SystemUiMode.LeanBack);

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setEnabledSystemUIMode", call.Method);
        Assert.Equal("SystemUiMode.leanBack", call.Arguments);
    }

    [Fact]
    public async Task SetEnabledSystemUIMode_IgnoresOverlaysOutsideManualMode()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetEnabledSystemUIMode(SystemUiMode.ImmersiveSticky, [SystemUiOverlay.Top]);
        await SystemChrome.SetEnabledSystemUIMode(SystemUiMode.EdgeToEdge);

        Assert.Equal(["SystemChrome.setEnabledSystemUIMode", "SystemChrome.setEnabledSystemUIMode"], platform.Methods);
        Assert.Equal("SystemUiMode.immersiveSticky", platform.Log[0].Arguments);
        Assert.Equal("SystemUiMode.edgeToEdge", platform.Log[1].Arguments);
    }

    // 'setEnabledSystemUIMode asserts for overlays in manual configuration'
    [DebugOnlyFact]
    public async Task SetEnabledSystemUIMode_ManualWithoutOverlaysAsserts()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        AssertionError error = await Assert.ThrowsAsync<AssertionError>(
            () => SystemChrome.SetEnabledSystemUIMode(SystemUiMode.Manual));

        Assert.Contains("mode == SystemUiMode.manual && overlays != null", error.ToString());
        Assert.Empty(platform.Log);
    }

    // 'setEnabledSystemUIMode passes correct overlays for manual configuration'
    [Fact]
    public async Task SetEnabledSystemUIMode_ManualSendsTheOverlays()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetEnabledSystemUIMode(SystemUiMode.Manual, [SystemUiOverlay.Top]);

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setEnabledSystemUIOverlays", call.Method);
        Assert.Equal(["SystemUiOverlay.top"], ((IList)call.Arguments!).Cast<object>());
    }

    // 'setSystemUIChangeCallback control test'
    [Fact]
    public async Task SetSystemUIChangeCallback_RegistersTheListenerOnlyForACallback()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await SystemChrome.SetSystemUIChangeCallback(null);
        Assert.Empty(platform.Log);

        await SystemChrome.SetSystemUIChangeCallback(_ => Task.CompletedTask);
        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemChrome.setSystemUIChangeListener", call.Method);
        Assert.Null(call.Arguments);
        await SystemChrome.SetSystemUIChangeCallback(null);
    }

    // services/binding.dart `_handlePlatformMessage`: 'SystemChrome.systemUIChange' carries
    // `[systemOverlaysAreVisible]` to the callback `setSystemUIChangeCallback` installed.
    [Fact]
    public async Task SystemUIChange_FromThePlatformReachesTheCallback()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var visibility = new List<bool>();
        await SystemChrome.SetSystemUIChangeCallback(visible =>
        {
            visibility.Add(visible);
            return Task.CompletedTask;
        });
        try
        {
            await SendPlatformMessage("SystemChrome.systemUIChange", new List<object?> { false });
            await SendPlatformMessage("SystemChrome.systemUIChange", new List<object?> { true });
            Assert.Equal([false, true], visibility);

            await SystemChrome.SetSystemUIChangeCallback(null);
            await SendPlatformMessage("SystemChrome.systemUIChange", new List<object?> { false });
            Assert.Equal([false, true], visibility);
        }
        finally
        {
            await SystemChrome.SetSystemUIChangeCallback(null);
        }
    }

    // services/binding.dart `_handlePlatformMessage`: 'System.requestAppExit' answers with the
    // binding's `handleRequestAppExit` response name; any other method is an error.
    [Fact]
    public async Task PlatformMessages_RequestAppExitAnswersAndUnknownMethodsFail()
    {
        object? reply = await SendPlatformMessage("System.requestAppExit", null);
        Assert.Equal("exit", ((IDictionary)reply!)["response"]);

        PlatformException error = await Assert.ThrowsAsync<PlatformException>(
            () => SendPlatformMessage("SystemChrome.unknown", null));
        Assert.Contains("Method \"SystemChrome.unknown\" not handled.", error.ErrorMessage);
    }

    // SystemUiOverlayStyle 'toString default values should be null'
    [DebugOnlyFact]
    public void OverlayStyle_ToStringOfDefaultsPrintsNulls()
    {
        string text = new SystemUiOverlayStyle().ToString();

        Assert.StartsWith("SystemUiOverlayStyle#", text);
        Assert.Contains("systemNavigationBarColor: null", text);
        Assert.Contains("systemNavigationBarDividerColor: null", text);
        Assert.Contains("systemStatusBarContrastEnforced: null", text);
        Assert.Contains("statusBarColor: null", text);
        Assert.Contains("statusBarBrightness: null", text);
        Assert.Contains("statusBarIconBrightness: null", text);
        Assert.Contains("systemNavigationBarIconBrightness: null", text);
        Assert.Contains("systemNavigationBarContrastEnforced: null", text);
    }

    // SystemUiOverlayStyle 'toString works as intended with actual values'
    [DebugOnlyFact]
    public void OverlayStyle_ToStringPrintsDartValues()
    {
        string text = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            systemNavigationBarDividerColor: new Color(0xFF654321),
            systemStatusBarContrastEnforced: true,
            statusBarColor: new Color(0xFFABCDEF),
            statusBarBrightness: PlatformBrightness.Dark,
            statusBarIconBrightness: PlatformBrightness.Light,
            systemNavigationBarIconBrightness: PlatformBrightness.Dark,
            systemNavigationBarContrastEnforced: false).ToString();

        Assert.StartsWith("SystemUiOverlayStyle#", text);
        Assert.Contains(
            "systemNavigationBarColor: Color(alpha: 1.0000, red: 0.0706, green: 0.2039, blue: 0.3373, "
            + "colorSpace: ColorSpace.sRGB)",
            text);
        Assert.Contains(
            "systemNavigationBarDividerColor: Color(alpha: 1.0000, red: 0.3961, green: 0.2627, blue: 0.1294, "
            + "colorSpace: ColorSpace.sRGB)",
            text);
        Assert.Contains("systemStatusBarContrastEnforced: true", text);
        Assert.Contains(
            "statusBarColor: Color(alpha: 1.0000, red: 0.6706, green: 0.8039, blue: 0.9373, "
            + "colorSpace: ColorSpace.sRGB)",
            text);
        Assert.Contains("statusBarBrightness: Brightness.dark", text);
        Assert.Contains("statusBarIconBrightness: Brightness.light", text);
        Assert.Contains("systemNavigationBarIconBrightness: Brightness.dark", text);
        Assert.Contains("systemNavigationBarContrastEnforced: false", text);
    }

    // SystemUiOverlayStyle '==, hashCode basics'
    [Fact]
    public void OverlayStyle_EqualityAndHashCode()
    {
        var a = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            statusBarBrightness: PlatformBrightness.Dark);
        var b = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            statusBarBrightness: PlatformBrightness.Dark);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        var c = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF654321),
            statusBarBrightness: PlatformBrightness.Dark);
        Assert.NotEqual(a, c);
        Assert.True(a != c);
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    // SystemUiOverlayStyle 'copyWith can override properties'
    [Fact]
    public void OverlayStyle_CopyWithOverridesOnlyTheGivenFields()
    {
        var original = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            statusBarBrightness: PlatformBrightness.Dark,
            systemStatusBarContrastEnforced: true);

        SystemUiOverlayStyle copy = original.CopyWith(
            systemNavigationBarColor: new Color(0xFF654321),
            statusBarIconBrightness: PlatformBrightness.Light);

        Assert.Equal(new Color(0xFF654321), copy.SystemNavigationBarColor);
        Assert.Equal(PlatformBrightness.Dark, copy.StatusBarBrightness);
        Assert.Equal(true, copy.SystemStatusBarContrastEnforced);
        Assert.Equal(PlatformBrightness.Light, copy.StatusBarIconBrightness);
        Assert.Null(copy.SystemNavigationBarDividerColor);
    }

    // SystemUiOverlayStyle 'SystemUiOverlayStyle implements debugFillProperties'
    [DebugOnlyFact]
    public void OverlayStyle_DebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            systemNavigationBarDividerColor: new Color(0xFF654321),
            systemNavigationBarIconBrightness: PlatformBrightness.Light,
            systemNavigationBarContrastEnforced: true,
            statusBarColor: new Color(0xFFABCDEF),
            statusBarBrightness: PlatformBrightness.Dark,
            statusBarIconBrightness: PlatformBrightness.Light,
            systemStatusBarContrastEnforced: false).DebugFillProperties(builder);

        Assert.Equal(
            [
                "systemNavigationBarColor: Color(alpha: 1.0000, red: 0.0706, green: 0.2039, blue: 0.3373, "
                + "colorSpace: ColorSpace.sRGB)",
                "systemNavigationBarDividerColor: Color(alpha: 1.0000, red: 0.3961, green: 0.2627, blue: 0.1294, "
                + "colorSpace: ColorSpace.sRGB)",
                "systemNavigationBarIconBrightness: Brightness.light",
                "systemNavigationBarContrastEnforced: true",
                "statusBarColor: Color(alpha: 1.0000, red: 0.6706, green: 0.8039, blue: 0.9373, "
                + "colorSpace: ColorSpace.sRGB)",
                "statusBarBrightness: Brightness.dark",
                "statusBarIconBrightness: Brightness.light",
                "systemStatusBarContrastEnforced: false",
            ],
            builder.Properties
                .Where(property => !property.IsFiltered(DiagnosticLevel.Info))
                .Select(property => property.ToString())
                .ToArray());
    }

    [Fact]
    public void OverlayStyle_LightAndDarkConstants()
    {
        Assert.Equal(new Color(0xFF000000), SystemUiOverlayStyle.Light.SystemNavigationBarColor);
        Assert.Equal(PlatformBrightness.Light, SystemUiOverlayStyle.Light.SystemNavigationBarIconBrightness);
        Assert.Equal(PlatformBrightness.Light, SystemUiOverlayStyle.Light.StatusBarIconBrightness);
        Assert.Equal(PlatformBrightness.Dark, SystemUiOverlayStyle.Light.StatusBarBrightness);
        Assert.Null(SystemUiOverlayStyle.Light.StatusBarColor);
        Assert.Null(SystemUiOverlayStyle.Light.SystemNavigationBarDividerColor);

        Assert.Equal(new Color(0xFF000000), SystemUiOverlayStyle.Dark.SystemNavigationBarColor);
        // Dart keeps light navigation bar icons in both constants.
        Assert.Equal(PlatformBrightness.Light, SystemUiOverlayStyle.Dark.SystemNavigationBarIconBrightness);
        Assert.Equal(PlatformBrightness.Dark, SystemUiOverlayStyle.Dark.StatusBarIconBrightness);
        Assert.Equal(PlatformBrightness.Light, SystemUiOverlayStyle.Dark.StatusBarBrightness);
    }

    // The host decodes the platform message back into the style it was built from.
    [Fact]
    public void OverlayStyle_MapRoundTripsThroughTheJsonCodec()
    {
        var style = new SystemUiOverlayStyle(
            systemNavigationBarColor: new Color(0xFF123456),
            systemNavigationBarDividerColor: new Color(0x10654321),
            systemNavigationBarIconBrightness: PlatformBrightness.Dark,
            systemNavigationBarContrastEnforced: false,
            statusBarColor: new Color(0x00000000),
            statusBarBrightness: PlatformBrightness.Light,
            statusBarIconBrightness: PlatformBrightness.Dark,
            systemStatusBarContrastEnforced: true);
        var codec = new JsonMethodCodec();

        MethodCall decoded = codec.DecodeMethodCall(
            codec.EncodeMethodCall(new MethodCall("SystemChrome.setSystemUIOverlayStyle", style.ToMap())));

        Assert.Equal(style, SystemUiOverlayStyle.FromMap((IDictionary)decoded.Arguments!));
        Assert.Equal(new SystemUiOverlayStyle(), SystemUiOverlayStyle.FromMap(new SystemUiOverlayStyle().ToMap()));
    }

    // 'SystemChrome handles detached lifecycle state'
    [Fact]
    public void DetachedLifecycleState_ForgetsTheLatestStyle()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var style = new SystemUiOverlayStyle();

        SystemChrome.SetSystemUIOverlayStyle(style);
        Scheduler.FlushMicrotasks();
        Assert.Single(platform.Log);

        SystemChrome.SetSystemUIOverlayStyle(style);
        Scheduler.FlushMicrotasks();
        Assert.Single(platform.Log);

        SystemChrome.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);
        Scheduler.FlushMicrotasks();
        SystemChrome.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        SystemChrome.SetSystemUIOverlayStyle(style);
        Scheduler.FlushMicrotasks();
        Assert.Equal(2, platform.Log.Count);
    }

    // services/binding.dart `_handleLifecycleMessage`: every generated lifecycle transition also
    // reaches `SystemChrome.handleAppLifecycleStateChanged`.
    [Fact]
    public void DetachedLifecycleState_FromTheBindingForgetsTheLatestStyle()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        WidgetsBinding binding = WidgetsBinding.Instance;
        AppLifecycleState? previous = binding.LifecycleState;
        try
        {
            binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Dark);
            Scheduler.FlushMicrotasks();
            Assert.Same(SystemUiOverlayStyle.Dark, SystemChrome.LatestStyle);

            binding.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);
            Scheduler.FlushMicrotasks();
            Assert.Null(SystemChrome.LatestStyle);
        }
        finally
        {
            binding.ResetLifecycleStateForTests();
            if (previous is { } state)
            {
                binding.HandleAppLifecycleStateChanged(state);
            }
        }
    }

    // 'SystemChrome reports error when setSystemUIOverlayStyle fails'
    [Fact]
    public void OverlayStyle_PlatformFailureIsReported()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            call => call.Method == "SystemChrome.setSystemUIOverlayStyle"
                ? throw new PlatformException("error", "Failed to set system UI overlay style")
                : null);
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        try
        {
            SystemChrome.SetSystemUIOverlayStyle(SystemUiOverlayStyle.Light);
            Scheduler.FlushMicrotasks();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(errors);
        PlatformException exception = Assert.IsType<PlatformException>(details.Exception);
        Assert.Equal("Failed to set system UI overlay style", exception.ErrorMessage);
        Assert.Contains("while setting the system UI overlay style", details.Context!.ToString());
        // `_latestStyle` is recorded as soon as the message is sent, even when it fails.
        Assert.Same(SystemUiOverlayStyle.Light, SystemChrome.LatestStyle);
    }

    private static Task<object?> SendPlatformMessage(string method, object? arguments)
    {
        var codec = new JsonMethodCodec();
        var reply = new TaskCompletionSource<object?>();
        _ = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            "flutter/platform",
            codec.EncodeMethodCall(new MethodCall(method, arguments)),
            data =>
            {
                try
                {
                    reply.SetResult(data is null ? null : codec.DecodeEnvelope(data));
                }
                catch (Exception error)
                {
                    reply.SetException(error);
                }
            });
        return reply.Task;
    }

    private static void AssertMap(IReadOnlyDictionary<string, object?> expected, IDictionary actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach ((string key, object? value) in expected)
        {
            Assert.True(actual.Contains(key), key);
            object? actualValue = actual[key] is int intValue ? (long)intValue : actual[key];
            Assert.Equal(value, actualValue);
        }
    }
}
