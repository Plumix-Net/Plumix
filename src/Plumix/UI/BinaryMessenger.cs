using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/binary_messenger.dart

/// <summary>Signature for listening to changes in the <see cref="SystemUiMode"/>.</summary>
/// <remarks>Set by <see cref="SystemChrome.SetSystemUIChangeCallback"/>.</remarks>
public delegate Task SystemUiChangeCallback(bool systemOverlaysAreVisible);

/// <summary>A function which decodes and handles a binary message received on a channel.</summary>
/// <remarks>Returning <c>null</c> means "no reply".</remarks>
public delegate Task<ByteData?>? MessageHandler(ByteData? message);

/// <summary>Signature for the callback that receives the reply to a platform message.</summary>
public delegate void PlatformMessageResponseCallback(ByteData? data);

/// <summary>A messenger which sends binary data across the Flutter platform barrier.</summary>
/// <remarks>
/// Flutter's barrier is the engine; Plumix's is the host adapter, so the direction that Dart cannot
/// express — registering the platform side of a channel — lives on
/// <see cref="SetPlatformMessageHandler"/>. See <c>docs/ai/DIVERGENCES.md</c>.
/// </remarks>
public abstract class BinaryMessenger
{
    /// <summary>Queues a message for the framework's handler on <paramref name="channel"/>.</summary>
    /// <remarks>
    /// Dart parity source: <c>BinaryMessenger.handlePlatformMessage</c>. Hosts call this to deliver an
    /// inbound platform message; <paramref name="callback"/> receives the framework's reply.
    /// </remarks>
    public abstract Task HandlePlatformMessage(
        string channel,
        ByteData? data,
        PlatformMessageResponseCallback? callback);

    /// <summary>Sends a binary message to the platform plugins on <paramref name="channel"/>.</summary>
    /// <returns>The reply, or <c>null</c> when no platform handler is registered for the channel.</returns>
    public abstract Task<ByteData?>? Send(string channel, ByteData? message);

    /// <summary>Sets a callback for receiving messages from the platform plugins on
    /// <paramref name="channel"/>.</summary>
    /// <remarks>The given callback replaces the currently registered callback, if any.</remarks>
    public abstract void SetMessageHandler(string channel, MessageHandler? handler);

    /// <summary>
    /// Registers the platform (host) side of <paramref name="channel"/>. Flutter has no equivalent — its
    /// platform side lives in the engine, outside Dart.
    /// </summary>
    public abstract void SetPlatformMessageHandler(string channel, MessageHandler? handler);
}

/// <summary>The default <see cref="BinaryMessenger"/>: an in-process channel registry.</summary>
/// <remarks>
/// Flutter's default messenger hands the bytes to the engine, which routes them to a plugin on the
/// platform thread. Plumix runs the whole application in one process, so both sides are dictionaries of
/// handlers and a send completes on the caller's thread whenever the platform handler is synchronous.
/// </remarks>
public class PlatformBinaryMessenger : BinaryMessenger
{
    private readonly Dictionary<string, MessageHandler> _handlers = [];
    private readonly Dictionary<string, MessageHandler> _platformHandlers = [];

    public override Task HandlePlatformMessage(
        string channel,
        ByteData? data,
        PlatformMessageResponseCallback? callback)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (!_handlers.TryGetValue(channel, out MessageHandler? handler))
        {
            callback?.Invoke(null);
            return Task.CompletedTask;
        }

        Task<ByteData?>? reply = handler(data);
        if (reply is null)
        {
            callback?.Invoke(null);
            return Task.CompletedTask;
        }

        return Respond(reply, callback);
    }

    public override Task<ByteData?>? Send(string channel, ByteData? message)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (!_platformHandlers.TryGetValue(channel, out MessageHandler? handler))
        {
            return Task.FromResult<ByteData?>(null);
        }

        return handler(message) ?? Task.FromResult<ByteData?>(null);
    }

    public override void SetMessageHandler(string channel, MessageHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(channel);
        Register(_handlers, channel, handler);
    }

    public override void SetPlatformMessageHandler(string channel, MessageHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(channel);
        Register(_platformHandlers, channel, handler);
    }

    /// <summary>Whether a framework handler is registered for <paramref name="channel"/>.</summary>
    public bool HasMessageHandler(string channel) => _handlers.ContainsKey(channel);

    private static void Register(Dictionary<string, MessageHandler> handlers, string channel, MessageHandler? handler)
    {
        if (handler is null)
        {
            handlers.Remove(channel);
            return;
        }

        handlers[channel] = handler;
    }

    private static async Task Respond(Task<ByteData?> reply, PlatformMessageResponseCallback? callback)
    {
        ByteData? data = await reply;
        callback?.Invoke(data);
    }
}

/// <summary>
/// Owns the framework's platform-channel infrastructure. Dart parity source: <c>ServicesBinding</c>.
/// </summary>
/// <remarks>
/// Plumix has no binding hierarchy, so this is a single replaceable instance the way
/// <see cref="RestorationManager.Instance"/> is.
/// </remarks>
public class ServicesBinding
{
    private SystemUiChangeCallback? _systemUiChangeCallback;
    private ISystemContextMenuClient? _systemContextMenuClient;

    /// <summary>
    /// Dart's <c>ServicesBinding.initInstances</c>: the framework side of <c>flutter/platform</c>.
    /// </summary>
    /// <remarks>
    /// The handler is registered on this binding's own messenger, because
    /// <see cref="SystemChannels.Platform"/> resolves <see cref="Instance"/>, which is not assigned
    /// yet while the binding is being constructed.
    /// </remarks>
    public ServicesBinding()
    {
        new MethodChannel(SystemChannels.Platform.Name, SystemChannels.Platform.Codec, DefaultBinaryMessenger)
            .SetMethodCallHandler(HandlePlatformMessage);
        new BasicMessageChannel<object?>(
                SystemChannels.System.Name, SystemChannels.System.Codec, DefaultBinaryMessenger)
            .SetMessageHandler(async message =>
            {
                await Painting.PaintingBinding.Instance.HandleSystemMessage(message!);
                return null;
            });
        new BasicMessageChannel<object?>(
                SystemChannels.Accessibility.Name, SystemChannels.Accessibility.Codec, DefaultBinaryMessenger)
            .SetMessageHandler(async message =>
            {
                await HandleAccessibilityMessage(message!);
                return null;
            });
        RestorationManager = CreateRestorationManager();
        _ = InitKeyboard();
    }

    /// <summary>Notifies when the accessibility focus on the system has changed.</summary>
    /// <remarks>
    /// Flutter's <c>ServicesBinding.accessibilityFocus</c>: the id of the semantics node the platform
    /// last reported focused through a <c>didGainFocus</c> message on <c>flutter/accessibility</c>.
    /// </remarks>
    public ValueNotifier<int?> AccessibilityFocus { get; } = new(null);

    // Dart's `_handleAccessibilityMessage`.
    private Task HandleAccessibilityMessage(object accessibilityMessage)
    {
        var message = (System.Collections.IDictionary)accessibilityMessage;
        string type = (string)message["type"]!;
        switch (type)
        {
            case "didGainFocus":
                AccessibilityFocus.Value = Convert.ToInt32(message["nodeId"]);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>The global singleton instance of <see cref="HardwareKeyboard"/>.</summary>
    /// <remarks>
    /// Plumix's keyboards are process singletons, so this is <see cref="HardwareKeyboard.Instance"/>.
    /// </remarks>
    public HardwareKeyboard Keyboard => HardwareKeyboard.Instance;

    /// <summary>The global singleton instance of <see cref="UI.KeyEventManager"/>, which is used
    /// internally to dispatch key messages.</summary>
#pragma warning disable CS0618
    [Obsolete("No longer supported. Add a handler to HardwareKeyboard instead. "
              + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
    public KeyEventManager KeyEventManager => KeyEventManager.Instance;

    // Dart's `_initKeyboard`: once the keyboard state is synchronized with the platform, key data and
    // raw key messages are routed to the key event manager. The channels are bound to this binding's
    // own messenger for the same reason as the constructor's.
    private async Task InitKeyboard()
    {
        await Keyboard.SyncKeyboardState(new OptionalMethodChannel(
            SystemChannels.Keyboard.Name,
            SystemChannels.Keyboard.Codec,
            DefaultBinaryMessenger));
        PlatformDispatcher.Instance.OnKeyData = KeyEventManager.HandleKeyData;
        new BasicMessageChannel<object?>(
                SystemChannels.KeyEvent.Name,
                SystemChannels.KeyEvent.Codec,
                DefaultBinaryMessenger)
            .SetMessageHandler(KeyEventManager.HandleRawKeyMessage);
    }
#pragma warning restore CS0618

    /// <summary>The ambient services binding.</summary>
    public static ServicesBinding Instance { get; set; } = new ServicesBinding();

    /// <summary>The messenger every channel uses unless it was given one explicitly.</summary>
    public virtual BinaryMessenger DefaultBinaryMessenger { get; } = new PlatformBinaryMessenger();

    /// <summary>The state restoration manager owned by this services binding.</summary>
    public RestorationManager RestorationManager { get; internal set; }

    /// <summary>Creates the restoration manager, as in Dart's services binding.</summary>
    protected virtual RestorationManager CreateRestorationManager() => new RestorationManager(DefaultBinaryMessenger);

    public virtual Rendering.AssetBundle RootBundle => Rendering.PlatformAssetBundle.Root;

    public virtual void Evict(string asset) => RootBundle.Evict(asset);

    public virtual void HandleMemoryPressure() => RootBundle.Clear();

    public virtual Task HandleSystemMessage(object systemMessage)
    {
        var message = (System.Collections.IDictionary)systemMessage;
        if ((string)message["type"]! == "memoryPressure")
        {
            // The bindings are separate singletons: dispatch to Dart's most derived override, the
            // widgets binding's, which runs the painting binding's (and this one's) first.
            WidgetsBinding.Instance.HandleMemoryPressure();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the callback for the <c>SystemChrome.systemUIChange</c> method call received on the
    /// <see cref="SystemChannels.Platform"/> channel.
    /// </summary>
    /// <remarks>
    /// This is typically not called directly. System UI changes that this method responds to are
    /// associated with <see cref="SystemUiMode"/>s, which are configured using
    /// <see cref="SystemChrome"/>. Use <see cref="SystemChrome.SetSystemUIChangeCallback"/> to
    /// configure along with other <see cref="SystemChrome"/> settings.
    /// </remarks>
    public void SetSystemUiChangeCallback(SystemUiChangeCallback? callback)
    {
        _systemUiChangeCallback = callback;
    }

    /// <summary>
    /// Registers a <see cref="ISystemContextMenuClient"/> that will receive system context menu
    /// calls from the engine.
    /// </summary>
    /// <remarks>
    /// Dart's static <c>ServicesBinding.systemContextMenuClient</c> setter; like Dart there is no
    /// getter. This API is in place to receive the system's hide calls, so only one client can be
    /// registered at a time.
    /// </remarks>
    public static ISystemContextMenuClient? SystemContextMenuClient
    {
        set => Instance._systemContextMenuClient = value;
    }

    /// <summary>Dart's private <c>ServicesBinding._handlePlatformMessage</c>.</summary>
    /// <remarks>
    /// <c>System.requestAppExit</c> is answered by <see cref="WidgetsBinding.HandleRequestAppExit"/>, which is
    /// Dart's override of the services binding's method (the bindings are separate singletons in Plumix).
    /// </remarks>
    private async Task<object?> HandlePlatformMessage(MethodCall methodCall)
    {
        string method = methodCall.Method;
        switch (method)
        {
            case "SystemChrome.systemUIChange":
                var args = (System.Collections.IList)methodCall.Arguments!;
                if (_systemUiChangeCallback is { } callback)
                {
                    await callback((bool)args[0]!);
                }

                return null;
            case "System.requestAppExit":
                AppExitResponse response = await WidgetsBinding.Instance.HandleRequestAppExit();
                return new Dictionary<string, object?> { ["response"] = Diagnostics.EnumName(response) };
            // Called when the system dismisses the system context menu, such as when the user taps
            // outside the menu. Not called when Flutter shows a new system context menu while an
            // old one is still visible.
            case "ContextMenu.onDismissSystemContextMenu":
                if (_systemContextMenuClient is null)
                {
                    AssertNoClient("onDismissSystemContextMenu", hint: null);
                    return null;
                }

                _systemContextMenuClient.HandleSystemHide();
                _systemContextMenuClient = null;
                return null;
            case "ContextMenu.onPerformCustomAction":
                if (_systemContextMenuClient is null)
                {
                    AssertNoClient(
                        "onPerformCustomAction",
                        "ServicesBinding.systemContextMenuClient shouldn't be cleared unless the menu is hidden.");
                    return null;
                }

                var actionArgs = (System.Collections.IList)methodCall.Arguments!;
                string callbackId = (string)actionArgs[1]!;
                _systemContextMenuClient.HandleCustomContextMenuAction(callbackId);
                return null;
            default:
                throw new AssertionError($"Method \"{method}\" not handled.");
        }
    }

    private static void AssertNoClient(string method, string? hint)
    {
        if (Constants.KDebugMode)
        {
            string message = $"Platform sent {method} when no SystemContextMenuClient was registered.";
            throw new AssertionError(hint is null ? message : $"{message} {hint}");
        }
    }

    internal static void ResetForTests()
    {
        Instance = new ServicesBinding();
    }
}

/// <summary>Error reporting for the services layer.</summary>
/// <remarks>
/// Dart calls <c>FlutterError.reportError</c> inline at every services-layer catch site; this
/// helper exists only because C# has no way to spell the `FlutterErrorDetails` literal as tersely.
/// </remarks>
public static class ServicesDebug
{
    /// Reports `exception` to <see cref="FlutterError.ReportError"/> with Dart's context string.
    internal static void ReportError(Exception exception, string context)
    {
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "services library",
            context: new ErrorDescription(context)));
    }
}
