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
/// </para>
/// <para>
/// The construction checks are static too: <see cref="CheckInstance{T}"/> and
/// <see cref="DebugBindingType"/> read the type the composed binding (<c>WidgetsBinding</c>, whose
/// <c>InitInstances</c> is the first link of Plumix's chain) recorded, and the instance member
/// <c>debugCheckZone</c> lives on <c>WidgetsBinding</c>, the object Dart's <c>runApp</c> calls it on.
/// </para>
/// </remarks>
public static class BindingBase
{
    private const string ExtensionPrefix = "ext.flutter.";

    private static readonly Dictionary<string, ServiceExtensionCallback> Callbacks = [];
    private static readonly Dictionary<string, ServiceExtensionHandler> Handlers = [];

    /// <summary>
    /// Dart's private <c>BindingBase._debugInitializedType</c>: the runtime type of the binding whose
    /// <c>initInstances</c> ran, recorded in debug builds only.
    /// </summary>
    /// <remarks>
    /// Dart's base <c>initInstances</c> records it; Plumix's bindings have no common base instance, so the
    /// composed binding (<c>WidgetsBinding.InitInstances</c>) records it. Tests set it to reach both
    /// failure messages of <see cref="CheckInstance{T}"/>.
    /// </remarks>
    internal static Type? DebugInitializedType { get; set; }

    /// <summary>
    /// A method that shows a useful error message if the given binding instance is not initialized.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Flutter's <c>BindingBase.checkInstance</c>. This method either returns the argument or throws an
    /// exception. In release mode it always returns the argument.
    /// </para>
    /// <para>
    /// The type argument <typeparamref name="T"/> should be the kind of binding (e.g.
    /// <c>SchedulerBinding</c>) that is calling the method. It is used in error messages. Plumix's
    /// bindings do not derive from a <c>BindingBase</c> instance class, so <typeparamref name="T"/> is any
    /// class, and Dart's checks of the <c>BindingBase()</c> constructor having run (the "did not complete
    /// initialization", "initialized without calling initInstances" and "does not extend BindingBase"
    /// failures) have no constructor to observe.
    /// </para>
    /// </remarks>
    public static T CheckInstance<T>(T? instance)
        where T : class
    {
        if (Constants.KDebugMode)
        {
            string type = Diagnostics.DescribeType(typeof(T));
            if (DebugInitializedType == null && instance == null)
            {
                throw new FlutterError(
                [
                    new ErrorSummary("Binding has not yet been initialized."),
                    new ErrorDescription(
                        $"The \"instance\" getter on the {type} binding mixin is only available once that binding "
                        + "has been initialized."),
                    new ErrorHint(
                        "Typically, this is done by calling \"WidgetsFlutterBinding.ensureInitialized()\" or "
                        + "\"runApp()\" (the latter calls the former). Typically this call is done in the "
                        + "\"void main()\" method. The \"ensureInitialized\" method is idempotent; calling it "
                        + "multiple times is not harmful. After calling that method, the \"instance\" getter will "
                        + "return the binding."),
                    new ErrorHint(
                        "In a test, one can call \"TestWidgetsFlutterBinding.ensureInitialized()\" as the first "
                        + "line in the test's \"main()\" method to initialize the binding."),
                    new ErrorHint(
                        $"If {type} is a custom binding mixin, there must also be a custom binding class, like "
                        + "WidgetsFlutterBinding, but that mixes in the selected binding, and that is the class "
                        + "that must be constructed before using the \"instance\" getter."),
                ]);
            }

            if (instance == null)
            {
                throw new FlutterError(
                [
                    new ErrorSummary("Binding mixin instance is null but bindings are already initialized."),
                    new ErrorDescription(
                        $"The \"instance\" property of the {type} binding mixin was accessed, but that binding "
                        + "was not initialized when the \"initInstances()\" method was called."),
                    new ErrorHint(
                        $"This probably indicates that the {type} mixin was not mixed into the class that was "
                        + "used to initialize the binding. If this is a custom binding mixin, there must also be "
                        + "a custom binding class, like WidgetsFlutterBinding, but that mixes in the selected "
                        + "binding. If this is a test binding, check that the binding being initialized is the "
                        + "same as the one into which the test binding is mixed."),
                    new ErrorHint(
                        $"It is also possible that {type} does not implement \"initInstances()\" to assign a "
                        + "value to \"instance\". See the documentation of the BindingBase class for more "
                        + "details."),
                    new ErrorHint(
                        "The binding that was initialized was of the type "
                        + $"\"{Diagnostics.DescribeType(DebugInitializedType!)}\". "),
                ]);
            }
        }

        return instance!;
    }

    /// <summary>In debug builds, the type of the current binding, if any, or else null.</summary>
    /// <remarks>
    /// <para>
    /// Flutter's <c>BindingBase.debugBindingType</c>. This may be useful in asserts to verify that the
    /// binding has not been initialized before the point in the application code that wants to initialize
    /// the binding, or to verify that the binding is the one that is expected.
    /// </para>
    /// <para>
    /// If this returns null, the binding has not been initialized. If this returns a non-null value, it
    /// returns the type of the binding instance. This method only returns a useful value in debug builds.
    /// In release builds, the return value is always null; to improve startup performance, the type of
    /// the binding is not tracked in release builds.
    /// </para>
    /// </remarks>
    public static Type? DebugBindingType() => DebugInitializedType;

    /// <summary>
    /// Whether <c>WidgetsBinding.DebugCheckZone</c> should throw (true) or just report the error (false).
    /// </summary>
    /// <remarks>
    /// Flutter's <c>BindingBase.debugZoneErrorsAreFatal</c>. .NET has no zones, so the check it configures
    /// never finds a mismatch and this flag has no effect; it is kept so code written against Flutter's
    /// API compiles unchanged.
    /// </remarks>
    public static bool DebugZoneErrorsAreFatal { get; set; }

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
