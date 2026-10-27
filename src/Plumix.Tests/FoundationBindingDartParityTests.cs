using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/binding.dart
// (initServiceExtensions, reassembleApplication, checkInstance, debugBindingType, debugCheckZone); mirrors
// the foundation half of flutter/packages/flutter/test/foundation/service_extensions_test.dart,
// foundation/binding_test.dart and foundation/binding_2_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class FoundationBindingDartParityTests : IDisposable
{
    // The warm-up frame reassembleApplication waits for is two Timer.run turns on this fake clock.
    private readonly FakeGestureTimers _timers = new();
    private readonly List<IReadOnlyDictionary<string, object?>> _stateChanges = [];
    // The test assembly pins the process default to Android, as flutter_test does.
    private readonly TargetPlatform? _previousDefaultPlatform = PlatformDefaults.DebugDefaultTargetPlatform;

    public FoundationBindingDartParityTests()
    {
        SchedulerBinding.ResetForTests();
        BindingBase.EventPosted += OnEvent;
    }

    public void Dispose()
    {
        BindingBase.EventPosted -= OnEvent;
        _timers.Dispose();
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

    [NonReleaseFact]
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

    [NonReleaseTheory]
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
        Assert.Equal(Brightness.Dark, FoundationDebug.DebugBrightnessOverride);
        IReadOnlyDictionary<string, object?> changed = Assert.Single(_stateChanges);
        Assert.Equal("ext.flutter.brightnessOverride", changed["extension"]);
        Assert.Equal("Brightness.dark", changed["value"]);

        result = await Drain(Extension(FoundationServiceExtensions.BrightnessOverride, ("value", "Brightness.foo")));
        Assert.Null(FoundationDebug.DebugBrightnessOverride);
        Assert.Equal(platform, result["value"]);
    }

    [NonReleaseFact]
    public void DebugBrightnessOverride_ReplacesThePlatformBrightnessOfMediaQueryFromView()
    {
        // media_query.dart, _MediaQueryFromViewState.build.
        using var tester = new FrameworkDartTester();
        Brightness? seen = null;
        FoundationDebug.DebugBrightnessOverride = Brightness.Dark;

        tester.PumpWidget(new Builder(context =>
        {
            seen = MediaQuery.PlatformBrightnessOf(context);
            return new SizedBox();
        }));

        Assert.Equal(Brightness.Dark, seen);
    }

    // binding_test.dart: 'BindingBase.debugBindingType'. Dart's test first expects null, before its custom
    // binding is constructed; this process initializes WidgetsFlutterBinding on first use, so only the
    // initialized half is observable.
    [DebugOnlyFact]
    public void DebugBindingType_IsTheTypeOfTheInitializedBinding()
    {
        WidgetsFlutterBinding.EnsureInitialized();
        Assert.Equal(typeof(WidgetsFlutterBinding), BindingBase.DebugBindingType());
    }

    // debugBindingType's contract: the binding type is not tracked in release builds.
    [Fact]
    public void BuildModeGates_DebugBindingTypeIsNullOutsideDebug()
    {
        WidgetsFlutterBinding.EnsureInitialized();
        Assert.Equal(Constants.KDebugMode ? typeof(WidgetsFlutterBinding) : null, BindingBase.DebugBindingType());
    }

    // checkInstance: returns its argument once the binding exists.
    [Fact]
    public void CheckInstance_ReturnsTheInstance()
    {
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        Assert.Same(binding, BindingBase.CheckInstance<WidgetsBinding>(binding));
        Assert.Same(binding, WidgetsBinding.Instance);
    }

    // checkInstance: the two failures a null instance reports, depending on whether a binding initialized.
    [DebugOnlyFact]
    public void CheckInstance_ExplainsANullInstance()
    {
        Type? initializedType = BindingBase.DebugInitializedType;
        try
        {
            BindingBase.DebugInitializedType = null;
            FlutterError notInitialized =
                Assert.Throws<FlutterError>(static () => BindingBase.CheckInstance<SchedulerBinding>(null));
            Assert.Equal("Binding has not yet been initialized.", notInitialized.Diagnostics[0].ToString());
            Assert.Equal(
                "The \"instance\" getter on the SchedulerBinding binding mixin is only available once that "
                + "binding has been initialized.",
                notInitialized.Diagnostics[1].ToString());

            BindingBase.DebugInitializedType = typeof(WidgetsFlutterBinding);
            FlutterError notMixedIn =
                Assert.Throws<FlutterError>(static () => BindingBase.CheckInstance<SchedulerBinding>(null));
            Assert.Equal(
                "Binding mixin instance is null but bindings are already initialized.",
                notMixedIn.Diagnostics[0].ToString());
            Assert.Equal(
                "The binding that was initialized was of the type \"WidgetsFlutterBinding\". ",
                notMixedIn.Diagnostics[^1].ToString());
        }
        finally
        {
            BindingBase.DebugInitializedType = initializedType;
        }
    }

    // binding_2_test.dart: 'BindingBase.debugCheckZone'. The first call (in the binding's own zone) passes
    // silently. Dart's other two calls run in a forked zone and expect the "Zone mismatch" error, thrown
    // or reported per debugZoneErrorsAreFatal; .NET has no zones to fork, so every caller is in the
    // binding's zone and the check passes in both modes.
    [Fact]
    public void DebugCheckZone_PassesInTheBindingZone()
    {
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        FlutterExceptionHandler? lastHandler = FlutterError.OnError;
        List<FlutterErrorDetails> errors = [];
        FlutterError.OnError = errors.Add;
        try
        {
            Assert.True(binding.DebugCheckZone("test1"));
            BindingBase.DebugZoneErrorsAreFatal = true;
            Assert.True(binding.DebugCheckZone("test2"));
            BindingBase.DebugZoneErrorsAreFatal = false;
            Assert.True(binding.DebugCheckZone("test3"));
            Assert.Empty(errors);
        }
        finally
        {
            BindingBase.DebugZoneErrorsAreFatal = false;
            FlutterError.OnError = lastHandler;
        }
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
            _timers.RunDueTimers();
            EventLoop.FlushMicrotasks();
        }

        T result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        HotReloadTests.WaitForUnlock();
        return result;
    }
}
