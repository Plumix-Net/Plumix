using System.Globalization;
using System.Text.Json;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/binding.dart

namespace Plumix.Foundation;

/// <summary>
/// Signature for service extensions: the parameters of the request in, the JSON-encodable result
/// out.
/// </summary>
/// <remarks>
/// Flutter's <c>ServiceExtensionCallback</c>. The returned map must not use the keys <c>type</c> or
/// <c>method</c>: the registration wrapper overwrites both.
/// </remarks>
public delegate Task<Dictionary<string, object?>> ServiceExtensionCallback(
    IReadOnlyDictionary<string, string> parameters);

/// <summary>The VM-service side of a registered extension: the wrapped request handler.</summary>
internal delegate Task<ServiceExtensionResponse> ServiceExtensionHandler(
    IReadOnlyDictionary<string, string> parameters);

/// <summary>The answer to a service extension request: a JSON result, or an error code and detail.</summary>
/// <remarks>dart:developer's <c>ServiceExtensionResponse</c>.</remarks>
public sealed class ServiceExtensionResponse
{
    /// <summary>The error code for a failed extension callback.</summary>
    /// <remarks>dart:developer's <c>ServiceExtensionResponse.extensionError</c>.</remarks>
    public const int ExtensionError = -32000;

    /// <summary>The error code for a method no extension is registered under.</summary>
    /// <remarks>The JSON-RPC <c>Method not found</c> code the VM service answers with.</remarks>
    public const int MethodNotFound = -32601;

    private ServiceExtensionResponse(string? result, int? errorCode, string? errorDetail)
    {
        Result = result;
        ErrorCode = errorCode;
        ErrorDetail = errorDetail;
    }

    /// <summary>The JSON-encoded result, or <see langword="null"/> for an error.</summary>
    public string? Result { get; }

    /// <summary>The error code, or <see langword="null"/> for a result.</summary>
    public int? ErrorCode { get; }

    /// <summary>The JSON-encoded error detail, or <see langword="null"/> for a result.</summary>
    public string? ErrorDetail { get; }

    /// <summary>Whether this response is an error.</summary>
    public bool IsError => ErrorCode is not null;

    /// <summary>A successful response carrying <paramref name="result"/>.</summary>
    /// <remarks>dart:developer's <c>ServiceExtensionResponse.result</c>.</remarks>
    public static ServiceExtensionResponse FromResult(string result) => new(result, null, null);

    /// <summary>An error response.</summary>
    /// <remarks>dart:developer's <c>ServiceExtensionResponse.error</c>.</remarks>
    public static ServiceExtensionResponse FromError(int errorCode, string errorDetail) =>
        new(null, errorCode, errorDetail);
}

/// <summary>
/// The service-extension half of Flutter's binding base class: the registry of
/// <c>ext.flutter.*</c> extensions the bindings register, the typed registration wrappers, and the
/// event stream they post state changes to.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>BindingBase</c> registration and event members. Dart hands every extension to the VM
/// service through <c>developer.registerExtension</c> and every event to <c>developer.postEvent</c>;
/// .NET has no VM service, so the registry lives here and a tool (or a test) reaches it through
/// <see cref="InvokeServiceExtensionAsync"/> and <see cref="EventPosted"/>.
/// </para>
/// <para>
/// Plumix has no binding object for these members to live on, so they are static; the bindings'
/// <c>initServiceExtensions</c> call them. The members Dart's composed binding reaches through its
/// mixin chain are spelled out: <see cref="ReassembleApplication"/> locks events on
/// <c>SchedulerBinding</c> (where <c>lockEvents</c> lives) and runs the most derived
/// <c>performReassemble</c>, the widgets binding's, which ends in <see cref="PerformReassemble"/>.
/// Construction checks (<c>checkInstance</c>, <c>debugCheckZone</c>) are not ported (see
/// <c>docs/ai/BACKLOG.md</c>).
/// </para>
/// </remarks>
public static class BindingBase
{
    private const string ExtensionPrefix = "ext.flutter.";

    private static readonly Dictionary<string, ServiceExtensionCallback> Callbacks = [];
    private static readonly Dictionary<string, ServiceExtensionHandler> Handlers = [];

    /// <summary>Raised with every event posted through <see cref="PostEvent"/>.</summary>
    /// <remarks>dart:developer's <c>Extension</c> event stream.</remarks>
    public static event Action<string, IReadOnlyDictionary<string, object?>>? EventPosted;

    /// <summary>The full method names (<c>ext.flutter.&lt;name&gt;</c>) of every registered extension.</summary>
    public static IReadOnlyCollection<string> ServiceExtensionMethods => Handlers.Keys;

    /// <summary>
    /// The registered callbacks by short name, before the registration wrapper: what flutter_test's
    /// service-extension harness captures by overriding <c>registerServiceExtension</c>.
    /// </summary>
    internal static IReadOnlyDictionary<string, ServiceExtensionCallback> RegisteredCallbacks => Callbacks;

    /// <summary>
    /// Cause the entire application to redraw, e.g. after a hot reload.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.reassembleApplication</c>: <c>lockEvents(performReassemble)</c>. Events
    /// are locked while the widgets binding reassembles its tree, the renderer binding reassembles every
    /// registered render view and waits for the warm-up frame, and <see cref="PerformReassemble"/> resets
    /// the error count. This is expensive and should not be called except during development.
    /// </remarks>
    public static Task ReassembleApplication() =>
        SchedulerBinding.Instance.LockEvents(static () => Widgets.WidgetsBinding.Instance.PerformReassemble());

    /// <summary>The base of the <c>performReassemble</c> chain: resets the error count.</summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.performReassemble</c>, which every binding's override ends in. It resets
    /// <see cref="FlutterError"/>'s error count, so the next error after a hot reload prints in full.
    /// </remarks>
    public static Task PerformReassemble()
    {
        FlutterError.ResetErrorCount();
        return Task.CompletedTask;
    }

    /// <summary>Registers the foundation library's service extensions.</summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.initServiceExtensions</c>, the first link of every binding's chain:
    /// <c>reassemble</c> (debug), <c>exit</c> (not in release, not on the web),
    /// <c>connectedVmServiceUri</c> and <c>activeDevToolsServerAddress</c> (not in release), and
    /// <c>platformOverride</c> and <c>brightnessOverride</c> (debug), which reassemble the application
    /// after changing the override.
    /// </remarks>
    internal static void InitServiceExtensions()
    {
        if (Constants.KDebugMode)
        {
            RegisterSignalServiceExtension(
                FoundationServiceExtensions.Reassemble.DartName(),
                ReassembleApplication);
        }

        if (!Constants.KReleaseMode)
        {
            if (!Constants.KIsWeb)
            {
                RegisterSignalServiceExtension(FoundationServiceExtensions.Exit.DartName(), ExitApplication);
            }

            // These service extensions are used in profile mode applications.
            RegisterStringServiceExtension(
                FoundationServiceExtensions.ConnectedVmServiceUri.DartName(),
                static () => Task.FromResult(FoundationDebug.ConnectedVmServiceUri ?? string.Empty),
                static uri =>
                {
                    FoundationDebug.ConnectedVmServiceUri = uri;
                    return Task.CompletedTask;
                });
            RegisterStringServiceExtension(
                FoundationServiceExtensions.ActiveDevToolsServerAddress.DartName(),
                static () => Task.FromResult(FoundationDebug.ActiveDevToolsServerAddress ?? string.Empty),
                static serverAddress =>
                {
                    FoundationDebug.ActiveDevToolsServerAddress = serverAddress;
                    return Task.CompletedTask;
                });
        }

        if (Constants.KDebugMode)
        {
            RegisterServiceExtension(
                FoundationServiceExtensions.PlatformOverride.DartName(),
                static async parameters =>
                {
                    if (parameters.TryGetValue("value", out string? value))
                    {
                        PlatformDefaults.DebugDefaultTargetPlatform = null;
                        foreach (TargetPlatform candidate in Enum.GetValues<TargetPlatform>())
                        {
                            if (Diagnostics.EnumName(candidate) == value)
                            {
                                PlatformDefaults.DebugDefaultTargetPlatform = candidate;
                                break;
                            }
                        }

                        PostExtensionStateChangedEvent(
                            FoundationServiceExtensions.PlatformOverride.DartName(),
                            Diagnostics.EnumName(PlatformDefaults.TargetPlatform));
                        await ReassembleApplication();
                    }

                    return new Dictionary<string, object?>
                    {
                        ["value"] = Diagnostics.EnumName(PlatformDefaults.TargetPlatform),
                    };
                });

            RegisterServiceExtension(
                FoundationServiceExtensions.BrightnessOverride.DartName(),
                static async parameters =>
                {
                    if (parameters.TryGetValue("value", out string? value))
                    {
                        FoundationDebug.DebugBrightnessOverride = value switch
                        {
                            "Brightness.light" => Brightness.Light,
                            "Brightness.dark" => Brightness.Dark,
                            _ => null,
                        };
                        PostExtensionStateChangedEvent(
                            FoundationServiceExtensions.BrightnessOverride.DartName(),
                            BrightnessToString(EffectivePlatformBrightness()));
                        await ReassembleApplication();
                    }

                    return new Dictionary<string, object?>
                    {
                        ["value"] = BrightnessToString(EffectivePlatformBrightness()),
                    };
                });
        }
    }

    /// <summary>Replaces <c>dart:io</c>'s <c>exit</c> for the <c>exit</c> extension; a test captures it.</summary>
    internal static Action<int> ExitProcess { get; set; } = Environment.Exit;

    /// <remarks>Flutter's private top-level <c>_exitApplication</c>: <c>exit(0)</c>.</remarks>
    private static Task ExitApplication()
    {
        ExitProcess(0);
        return Task.CompletedTask;
    }

    private static Brightness EffectivePlatformBrightness() =>
        FoundationDebug.DebugBrightnessOverride ?? PlatformDispatcher.Instance.PlatformBrightness;

    /// <summary>dart:ui's <c>Brightness.toString()</c>, e.g. <c>Brightness.dark</c>.</summary>
    private static string BrightnessToString(Brightness brightness) =>
        "Brightness." + Diagnostics.EnumName(brightness);

    /// <summary>Registers a service extension that takes no arguments and returns an empty map.</summary>
    /// <remarks>Flutter's <c>BindingBase.registerSignalServiceExtension</c>.</remarks>
    public static void RegisterSignalServiceExtension(string name, Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        RegisterServiceExtension(name, async _ =>
        {
            await callback();
            return [];
        });
    }

    /// <summary>
    /// Registers a service extension that reads and, given an <c>enabled</c> argument, writes a
    /// boolean.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.registerBoolServiceExtension</c>: only the exact string <c>true</c>
    /// enables; every set posts a <c>Flutter.ServiceExtensionStateChanged</c> event.
    /// </remarks>
    public static void RegisterBoolServiceExtension(string name, Func<Task<bool>> getter, Func<bool, Task> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        RegisterServiceExtension(name, async parameters =>
        {
            if (parameters.TryGetValue("enabled", out string? enabled))
            {
                await setter(enabled == "true");
                PostExtensionStateChangedEvent(name, await getter() ? "true" : "false");
            }

            return new Dictionary<string, object?> { ["enabled"] = await getter() ? "true" : "false" };
        });
    }

    /// <summary>
    /// Registers a service extension that reads and, given an argument named after the extension,
    /// writes a double.
    /// </summary>
    /// <remarks>Flutter's <c>BindingBase.registerNumericServiceExtension</c>.</remarks>
    public static void RegisterNumericServiceExtension(
        string name,
        Func<Task<double>> getter,
        Func<double, Task> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        RegisterServiceExtension(name, async parameters =>
        {
            if (parameters.TryGetValue(name, out string? value))
            {
                await setter(double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
                PostExtensionStateChangedEvent(name, DartDoubleToString(await getter()));
            }

            return new Dictionary<string, object?> { [name] = DartDoubleToString(await getter()) };
        });
    }

    /// <summary>
    /// Registers a service extension that reads and, given a <c>value</c> argument, writes a string.
    /// </summary>
    /// <remarks>Flutter's <c>BindingBase.registerStringServiceExtension</c>.</remarks>
    public static void RegisterStringServiceExtension(
        string name,
        Func<Task<string>> getter,
        Func<string, Task> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        RegisterServiceExtension(name, async parameters =>
        {
            if (parameters.TryGetValue("value", out string? value))
            {
                await setter(value);
                PostExtensionStateChangedEvent(name, await getter());
            }

            return new Dictionary<string, object?> { ["value"] = await getter() };
        });
    }

    /// <summary>Registers a service extension method under <c>ext.flutter.&lt;name&gt;</c>.</summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.registerServiceExtension</c>. A request first waits one outer
    /// event-loop turn (the extension may arrive mid-frame), then runs <paramref name="callback"/>; an
    /// exception is reported through <see cref="FlutterError.ReportError"/> and answered with an
    /// <see cref="ServiceExtensionResponse.ExtensionError"/> response, and a result gets the
    /// <c>type</c> and <c>method</c> keys before it is JSON-encoded. Registering a name twice throws,
    /// as <c>developer.registerExtension</c> does.
    /// </remarks>
    public static void RegisterServiceExtension(string name, ServiceExtensionCallback callback)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);
        string methodName = ExtensionPrefix + name;
        if (Handlers.ContainsKey(methodName))
        {
            throw new ArgumentException($"Extension already registered: {methodName}", nameof(name));
        }

        Callbacks[name] = callback;
        Handlers[methodName] = async parameters =>
        {
            if (Constants.KDebugMode && FoundationDebug.DebugInstrumentationEnabled)
            {
                Print.DebugPrint(
                    $"service extension method received: {methodName}({FormatParameters(parameters)})");
            }

            // VM service extensions are delivered out of band, possibly in the middle of a
            // microtask loop or a frame; give that work a chance to finish first.
            await FoundationDebug.DebugInstrumentAction<bool>("Wait for outer event loop", WaitForOuterEventLoop);
            Dictionary<string, object?> result;
            try
            {
                result = await callback(parameters);
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    context: new ErrorDescription($"during a service extension callback for \"{methodName}\"")));
                return ServiceExtensionResponse.FromError(
                    ServiceExtensionResponse.ExtensionError,
                    JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["exception"] = exception.ToString(),
                        ["stack"] = exception.StackTrace ?? string.Empty,
                        ["method"] = methodName,
                    }));
            }

            result["type"] = "_extensionType";
            result["method"] = methodName;
            return ServiceExtensionResponse.FromResult(JsonSerializer.Serialize(result));
        };
    }

    /// <summary>
    /// Runs the extension registered under <paramref name="method"/> (<c>ext.flutter.&lt;name&gt;</c>)
    /// with <paramref name="parameters"/>, the way the VM service delivers a request.
    /// </summary>
    /// <remarks>
    /// dart:developer's side of <c>registerExtension</c>. An unknown method is answered with
    /// <see cref="ServiceExtensionResponse.MethodNotFound"/>. The VM runs the extension in the
    /// isolate, so the handler starts on the framework thread (<see cref="EventLoop.RunAsync"/>) and
    /// every <c>await</c> inside it resumes as a microtask there, not on a thread-pool thread that
    /// would race the frames the extension waits for.
    /// </remarks>
    public static Task<ServiceExtensionResponse> InvokeServiceExtensionAsync(
        string method,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (!Handlers.TryGetValue(method, out ServiceExtensionHandler? handler))
        {
            return Task.FromResult(ServiceExtensionResponse.FromError(
                ServiceExtensionResponse.MethodNotFound,
                JsonSerializer.Serialize(new Dictionary<string, string> { ["method"] = method })));
        }

        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        return handler(parameters ?? new Dictionary<string, string>());
    }

    /// <summary>Posts an event to the extension event stream.</summary>
    /// <remarks>Flutter's <c>BindingBase.postEvent</c> over <c>developer.postEvent</c>.</remarks>
    public static void PostEvent(string eventKind, IReadOnlyDictionary<string, object?> eventData)
    {
        Developer.DeveloperService.PostEvent(eventKind, eventData);
    }

    /// <summary>Raises <see cref="EventPosted"/> for an event posted to the <c>Extension</c> stream.</summary>
    internal static void RaiseEventPosted(string eventKind, IReadOnlyDictionary<string, object?> eventData)
    {
        EventPosted?.Invoke(eventKind, eventData);
    }

    /// <remarks>Flutter's private <c>BindingBase._postExtensionStateChangedEvent</c>.</remarks>
    private static void PostExtensionStateChangedEvent(string name, object? value)
    {
        PostEvent(
            "Flutter.ServiceExtensionStateChanged",
            new Dictionary<string, object?> { ["extension"] = ExtensionPrefix + name, ["value"] = value });
    }

    /// <summary>Dart's <c>double.toString()</c>: integral values keep a <c>.0</c>.</summary>
    internal static string DartDoubleToString(double value)
    {
        if (double.IsFinite(value) && Math.Floor(value) == value && Math.Abs(value) < 1e21)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>Dart's <c>Future&lt;void&gt;.delayed(Duration.zero)</c>: one turn of the outer event loop.</summary>
    private static Task<bool> WaitForOuterEventLoop()
    {
        var completer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Timer.Run(() => completer.SetResult(true));
        return completer.Task;
    }

    private static string FormatParameters(IReadOnlyDictionary<string, string> parameters) =>
        "{" + string.Join(", ", parameters.Select(static pair => $"{pair.Key}: {pair.Value}")) + "}";

    /// <summary>Drops every registration and event listener, for a test that re-registers.</summary>
    internal static void ResetForTests()
    {
        Callbacks.Clear();
        Handlers.Clear();
        EventPosted = null;
    }
}
