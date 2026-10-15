using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/binding.dart
// (initServiceExtensions, reassembleApplication); mirrors the foundation half of
// flutter/packages/flutter/test/foundation/service_extensions_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class FoundationBindingDartParityTests : IDisposable
{
    private readonly Action<Action> _previousTimerRun = PlatformDispatcher.Instance.TimerRun;
    private readonly List<Action> _timers = [];
    private readonly List<IReadOnlyDictionary<string, object?>> _stateChanges = [];
    // The test assembly pins the process default to Android, as flutter_test does.
    private readonly TargetPlatform? _previousDefaultPlatform = PlatformDefaults.DebugDefaultTargetPlatform;

    public FoundationBindingDartParityTests()
    {
        SchedulerBinding.ResetForTests();
        PlatformDispatcher.Instance.TimerRun = _timers.Add;
        BindingBase.EventPosted += OnEvent;
    }

    public void Dispose()
    {
        BindingBase.EventPosted -= OnEvent;
        PlatformDispatcher.Instance.TimerRun = _previousTimerRun;
        PlatformDefaults.DebugDefaultTargetPlatform = _previousDefaultPlatform;
        FoundationDebug.DebugBrightnessOverride = null;
        FoundationDebug.ConnectedVmServiceUri = null;
        FoundationDebug.ActiveDevToolsServerAddress = null;
        SchedulerBinding.ResetForTests();
    }

    private void OnEvent(string kind, IReadOnlyDictionary<string, object?> data)
    {
        if (kind == "Flutter.ServiceExtensionStateChanged")
        {
            _stateChanges.Add(data);
        }
    }

    [DebugOnlyFact]
    public void InitServiceExtensions_RegistersTheFoundationExtensions()
    {
        IReadOnlyCollection<string> methods = BindingBase.ServiceExtensionMethods;

        Assert.Contains("ext.flutter.reassemble", methods);
        Assert.Contains("ext.flutter.exit", methods);
        Assert.Contains("ext.flutter.connectedVmServiceUri", methods);
        Assert.Contains("ext.flutter.activeDevToolsServerAddress", methods);
        Assert.Contains("ext.flutter.platformOverride", methods);
        Assert.Contains("ext.flutter.brightnessOverride", methods);
    }

    [Fact]
    public async Task Exit_TerminatesTheProcessWithCodeZero()
    {
        Action<int> previousExit = BindingBase.ExitProcess;
        int? exitCode = null;
        BindingBase.ExitProcess = code => exitCode = code;
        try
        {
            Dictionary<string, object?> result = await Extension(FoundationServiceExtensions.Exit);

            Assert.Empty(result);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            BindingBase.ExitProcess = previousExit;
        }
    }

    [Theory]
    [InlineData(FoundationServiceExtensions.ConnectedVmServiceUri, "http://127.0.0.1:8181/abc=/")]
    [InlineData(FoundationServiceExtensions.ActiveDevToolsServerAddress, "http://127.0.0.1:9100")]
    public async Task StringExtensions_ReadAndWriteTheirValue(FoundationServiceExtensions extension, string value)
    {
        Assert.Equal(string.Empty, (await Extension(extension))["value"]);
        Assert.Empty(_stateChanges);

        Dictionary<string, object?> result = await Extension(extension, ("value", value));

        Assert.Equal(value, result["value"]);
        Assert.Equal(
            value,
            extension == FoundationServiceExtensions.ConnectedVmServiceUri
                ? FoundationDebug.ConnectedVmServiceUri
                : FoundationDebug.ActiveDevToolsServerAddress);
        IReadOnlyDictionary<string, object?> changed = Assert.Single(_stateChanges);
        Assert.Equal($"ext.flutter.{extension.DartName()}", changed["extension"]);
        Assert.Equal(value, changed["value"]);
    }

    [DebugOnlyFact]
    public async Task PlatformOverride_SetsTheDefaultTargetPlatformAndReassembles()
    {
        Assert.Equal(Diagnostics.EnumName(PlatformDefaults.TargetPlatform), (await Extension(
            FoundationServiceExtensions.PlatformOverride))["value"]);
        Assert.Empty(_stateChanges);

        Task<Dictionary<string, object?>> pending =
            Extension(FoundationServiceExtensions.PlatformOverride, ("value", "iOS"));
        Assert.Equal(TargetPlatform.IOS, PlatformDefaults.DebugDefaultTargetPlatform);
        // reassembleApplication locks events until its warm-up frame ended.
        Assert.True(SchedulerBinding.Instance.Locked);
        Dictionary<string, object?> result = await Drain(pending);

        Assert.Equal("iOS", result["value"]);
        IReadOnlyDictionary<string, object?> changed = Assert.Single(_stateChanges);
        Assert.Equal("ext.flutter.platformOverride", changed["extension"]);
        Assert.Equal("iOS", changed["value"]);
        Assert.False(SchedulerBinding.Instance.Locked);

        // An unknown name clears the override.
        await Drain(Extension(FoundationServiceExtensions.PlatformOverride, ("value", "default")));
        Assert.Null(PlatformDefaults.DebugDefaultTargetPlatform);
    }

    [DebugOnlyFact]
    public async Task BrightnessOverride_SetsDebugBrightnessOverrideAndReassembles()
    {
        string platform = "Brightness." + Diagnostics.EnumName(PlatformDispatcher.Instance.PlatformBrightness);
        Assert.Equal(platform, (await Extension(FoundationServiceExtensions.BrightnessOverride))["value"]);

        Dictionary<string, object?> result =
            await Drain(Extension(FoundationServiceExtensions.BrightnessOverride, ("value", "Brightness.dark")));

        Assert.Equal("Brightness.dark", result["value"]);
        Assert.Equal(PlatformBrightness.Dark, FoundationDebug.DebugBrightnessOverride);
        IReadOnlyDictionary<string, object?> changed = Assert.Single(_stateChanges);
        Assert.Equal("ext.flutter.brightnessOverride", changed["extension"]);
        Assert.Equal("Brightness.dark", changed["value"]);

        result = await Drain(Extension(FoundationServiceExtensions.BrightnessOverride, ("value", "Brightness.foo")));
        Assert.Null(FoundationDebug.DebugBrightnessOverride);
        Assert.Equal(platform, result["value"]);
    }

    [Fact]
    public void DebugBrightnessOverride_ReplacesThePlatformBrightnessOfMediaQueryFromView()
    {
        // media_query.dart, _MediaQueryFromViewState.build.
        using var tester = new FrameworkDartTester();
        PlatformBrightness? seen = null;
        FoundationDebug.DebugBrightnessOverride = PlatformBrightness.Dark;

        tester.PumpWidget(new Builder(context =>
        {
            seen = MediaQuery.PlatformBrightnessOf(context);
            return new SizedBox();
        }));

        Assert.Equal(PlatformBrightness.Dark, seen);
    }

    private static Task<Dictionary<string, object?>> Extension(
        FoundationServiceExtensions extension,
        params (string Key, string Value)[] arguments)
    {
        var parameters = arguments.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        // flutter_test's harness calls the registered callback directly, bypassing the VM wrapper.
        return BindingBase.RegisteredCallbacks[extension.DartName()](parameters);
    }

    /// <summary>Runs the warm-up frame <c>reassembleApplication</c> waits for, then its unlock.</summary>
    private async Task<T> Drain<T>(Task<T> pending)
    {
        for (int turn = 0; turn < 10 && !pending.IsCompleted; turn++)
        {
            while (_timers.Count > 0)
            {
                Action timer = _timers[0];
                _timers.RemoveAt(0);
                timer();
            }

            EventLoop.FlushMicrotasks();
        }

        T result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        HotReloadTests.WaitForUnlock();
        return result;
    }
}
