namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_channels.dart

/// <summary>Platform channels used by the Flutter system.</summary>
public static class SystemChannels
{
    /// <summary>A JSON <see cref="MethodChannel"/> for navigation.</summary>
    /// <remarks>
    /// The following incoming methods are defined: <c>popRoute</c>, <c>pushRoute</c> (with a single
    /// string argument) and <c>pushRouteInformation</c> (with a map argument carrying <c>location</c>
    /// and <c>state</c>). The framework sends <c>selectSingleEntryHistory</c>,
    /// <c>selectMultiEntryHistory</c>, <c>routeInformationUpdated</c> and the deprecated
    /// <c>routeUpdated</c>.
    /// </remarks>
    public static MethodChannel Navigation { get; } =
        new OptionalMethodChannel("flutter/navigation", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for handling predictive back gestures.</summary>
    /// <remarks>
    /// Incoming: <c>startBackGesture</c>, <c>updateBackGestureProgress</c>, <c>commitBackGesture</c>,
    /// <c>cancelBackGesture</c>. No outgoing methods.
    /// </remarks>
    public static MethodChannel BackGesture { get; } = new OptionalMethodChannel("flutter/backgesture");

    /// <summary>A JSON <see cref="MethodChannel"/> for invoking miscellaneous platform methods.</summary>
    /// <remarks>
    /// Outgoing: <c>Clipboard.setData</c>, <c>Clipboard.getData</c>, <c>HapticFeedback.vibrate</c>,
    /// <c>SystemSound.play</c>, <c>SystemChrome.setPreferredOrientations</c>,
    /// <c>SystemChrome.setApplicationSwitcherDescription</c>,
    /// <c>SystemChrome.setEnabledSystemUIOverlays</c>, <c>SystemChrome.setEnabledSystemUIMode</c>,
    /// <c>SystemChrome.setSystemUIOverlayStyle</c>, <c>SystemNavigator.pop</c>,
    /// <c>System.exitApplication</c>. Incoming: <c>SystemChrome.systemUIChange</c>,
    /// <c>System.requestAppExit</c>, <c>System.initializationComplete</c>.
    /// </remarks>
    public static MethodChannel Platform { get; } =
        new OptionalMethodChannel("flutter/platform", new JsonMethodCodec());

    /// <summary>A JSON <see cref="MethodChannel"/> for handling status-bar taps.</summary>
    public static OptionalMethodChannel StatusBar { get; } =
        new OptionalMethodChannel("flutter/status_bar", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for handling text processing actions.</summary>
    public static MethodChannel ProcessText { get; } = new OptionalMethodChannel("flutter/processtext");

    /// <summary>A JSON <see cref="MethodChannel"/> for handling text input.</summary>
    /// <remarks>
    /// Outgoing: <c>TextInput.setClient</c>, <c>TextInput.show</c>, <c>TextInput.setEditingState</c>,
    /// <c>TextInput.clearClient</c>, <c>TextInput.hide</c>. Incoming:
    /// <c>TextInputClient.updateEditingState</c>, <c>TextInputClient.updateEditingStateWithTag</c>,
    /// <c>TextInputClient.performAction</c>, <c>TextInputClient.requestExistingInputState</c>,
    /// <c>TextInputClient.onConnectionClosed</c>.
    /// </remarks>
    public static MethodChannel TextInput { get; } =
        new OptionalMethodChannel("flutter/textinput", new JsonMethodCodec());

    /// <summary>A JSON <see cref="MethodChannel"/> for handling stylus handwriting.</summary>
    public static MethodChannel Scribe { get; } =
        new OptionalMethodChannel("flutter/scribe", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for handling spell check.</summary>
    /// <remarks>Outgoing: <c>SpellCheck.initiateSpellCheck</c>.</remarks>
    public static MethodChannel SpellCheck { get; } = new OptionalMethodChannel("flutter/spellcheck");

    /// <summary>A JSON <see cref="MethodChannel"/> for handling undo events.</summary>
    public static MethodChannel UndoManager { get; } =
        new OptionalMethodChannel("flutter/undomanager", new JsonMethodCodec());

    /// <summary>A JSON <see cref="BasicMessageChannel{T}"/> for keyboard events.</summary>
    public static BasicMessageChannel<object?> KeyEvent { get; } =
        new BasicMessageChannel<object?>("flutter/keyevent", new JsonMessageCodec());

    /// <summary>A string <see cref="BasicMessageChannel{T}"/> for lifecycle events.</summary>
    public static BasicMessageChannel<string?> Lifecycle { get; } =
        new BasicMessageChannel<string?>("flutter/lifecycle", new StringCodec());

    /// <summary>A JSON <see cref="BasicMessageChannel{T}"/> for system events.</summary>
    public static BasicMessageChannel<object?> System { get; } =
        new BasicMessageChannel<object?>("flutter/system", new JsonMessageCodec());

    /// <summary>A <see cref="BasicMessageChannel{T}"/> for accessibility events.</summary>
    public static BasicMessageChannel<object?> Accessibility { get; } =
        new BasicMessageChannel<object?>("flutter/accessibility", new StandardMessageCodec());

    /// <summary>A <see cref="MethodChannel"/> for controlling platform views.</summary>
    public static MethodChannel PlatformViews { get; } = new MethodChannel("flutter/platform_views");

    /// <summary>A <see cref="MethodChannel"/> for controlling platform views, second generation.</summary>
    public static MethodChannel PlatformViews2 { get; } = new MethodChannel("flutter/platform_views_2");

    /// <summary>A JSON <see cref="MethodChannel"/> for configuring the Skia graphics library.</summary>
    /// <remarks>Outgoing: <c>Skia.setResourceCacheMaxBytes</c>.</remarks>
    public static MethodChannel Skia { get; } = new MethodChannel("flutter/skia", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for configuring the browser's context menu.</summary>
    public static MethodChannel ContextMenu { get; } =
        new OptionalMethodChannel("flutter/contextmenu", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for configuring mouse cursors.</summary>
    /// <remarks>Outgoing: <c>activateSystemCursor</c>, with an integer <c>device</c> and a string
    /// <c>kind</c>.</remarks>
    public static MethodChannel MouseCursor { get; } = new OptionalMethodChannel("flutter/mousecursor");

    /// <summary>A <see cref="MethodChannel"/> for synchronizing restoration data with the engine.</summary>
    /// <remarks>
    /// Outgoing: <c>get</c> (returns a map with an <c>enabled</c> bool and optional <c>data</c> bytes) and
    /// <c>put</c> (sends the current restoration data as bytes). Incoming: <c>push</c>, carrying the same
    /// shape as the <c>get</c> result.
    /// </remarks>
    public static MethodChannel Restoration { get; } = new OptionalMethodChannel("flutter/restoration");

    /// <summary>A <see cref="MethodChannel"/> for installing and manipulating deferred components.</summary>
    public static MethodChannel DeferredComponent { get; } =
        new OptionalMethodChannel("flutter/deferredcomponent");

    /// <summary>A JSON <see cref="MethodChannel"/> for retrieving localized resources.</summary>
    /// <remarks>Outgoing: <c>Localization.getStringResource</c>.</remarks>
    public static MethodChannel Localization { get; } =
        new OptionalMethodChannel("flutter/localization", new JsonMethodCodec());

    /// <summary>A <see cref="MethodChannel"/> for platform menus.</summary>
    /// <remarks>
    /// Outgoing: <c>Menu.setMenus</c>. Incoming: <c>Menu.selectedCallback</c>, <c>Menu.opened</c>,
    /// <c>Menu.closed</c>.
    /// </remarks>
    public static MethodChannel Menu { get; } = new OptionalMethodChannel("flutter/menu");

    /// <summary>A <see cref="MethodChannel"/> for accessing the keyboard's pressed-state.</summary>
    /// <remarks>Outgoing: <c>getKeyboardState</c>.</remarks>
    public static MethodChannel Keyboard { get; } = new OptionalMethodChannel("flutter/keyboard");

    /// <summary>A <see cref="MethodChannel"/> for setting the content sensitivity of a view.</summary>
    public static MethodChannel SensitiveContent { get; } =
        new OptionalMethodChannel("flutter/sensitivecontent");
}
