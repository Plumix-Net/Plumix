using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_manager.dart

namespace Plumix.Widgets;

/// <summary>
/// Tracks the last kind of input interaction to decide <see cref="FocusManager.HighlightMode"/>,
/// and routes key messages through the focus tree.
/// </summary>
/// <remarks>
/// Dart's private <c>_HighlightModeManager</c>, which <see cref="FocusManager"/> owns and forwards
/// its highlight and key-handler API to. Dart's <c>handleKeyMessage</c> reads
/// <c>FocusManager.instance.primaryFocus</c>; Plumix test and host roots can replace the binding's
/// manager, so this one reads the primary focus of the manager that owns it, which is the same
/// object whenever the manager is the binding's.
/// </remarks>
internal sealed class HighlightModeManager
{
    private readonly FocusManager _focusManager;
    private readonly HashedObserverList<OnKeyEventCallback> _earlyKeyEventHandlers = new();
    private readonly HashedObserverList<OnKeyEventCallback> _lateKeyEventHandlers = new();
    private HashedObserverList<Action<FocusHighlightMode>> _listeners = new();
    private bool? _lastInteractionRequiresTraditionalHighlights;
    private FocusHighlightMode? _highlightMode;
    private FocusHighlightStrategy _strategy = FocusHighlightStrategy.Automatic;
    private bool _globalHandlersRegistered;
#pragma warning disable CS0618
    private KeyMessageHandler? _keyMessageHandler;
#pragma warning restore CS0618

    public HighlightModeManager(FocusManager focusManager)
    {
        _focusManager = focusManager;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "_HighlightModeManager", this);
        }
    }

    /// <summary>The current highlight mode, or the platform default before any interaction.</summary>
    public FocusHighlightMode HighlightMode => _highlightMode ?? DefaultModeForPlatform;

    /// <summary>The strategy by which <see cref="HighlightMode"/> is determined.</summary>
    public FocusHighlightStrategy Strategy
    {
        get => _strategy;
        set
        {
            if (_strategy == value)
            {
                return;
            }

            _strategy = value;
            UpdateMode();
        }
    }

    public void AddEarlyKeyEventHandler(OnKeyEventCallback callback) => _earlyKeyEventHandlers.Add(callback);

    public void RemoveEarlyKeyEventHandler(OnKeyEventCallback callback) =>
        _earlyKeyEventHandlers.Remove(callback);

    public void AddLateKeyEventHandler(OnKeyEventCallback callback) => _lateKeyEventHandlers.Add(callback);

    public void RemoveLateKeyEventHandler(OnKeyEventCallback callback) =>
        _lateKeyEventHandlers.Remove(callback);

    public void AddListener(Action<FocusHighlightMode> listener) => _listeners.Add(listener);

    public void RemoveListener(Action<FocusHighlightMode> listener) => _listeners.Remove(listener);

    /// <summary>
    /// Registers the pointer route, the key message handler and the semantics action listener.
    /// </summary>
    /// <remarks>
    /// Dart's <c>registerGlobalHandlers</c>. Plumix's gesture binding broadcasts every pointer event
    /// through <see cref="GestureBinding.PointerEventReceived"/> in place of a global pointer route,
    /// and a second registration is ignored rather than asserted.
    /// </remarks>
    public void RegisterGlobalHandlers()
    {
        if (_globalHandlersRegistered)
        {
            return;
        }

        _globalHandlersRegistered = true;
        RegisterKeyMessageHandler();
        GestureBinding.PointerEventReceived += HandlePointerEvent;
        SemanticsBinding.Instance.AddSemanticsActionListener(HandleSemanticsAction);
    }

    /// <summary>
    /// C#-only: re-installs the key message handler after a test reset cleared the key event manager.
    /// </summary>
    public void ReregisterKeyMessageHandler()
    {
        if (_globalHandlersRegistered)
        {
            RegisterKeyMessageHandler();
        }
    }

    private void RegisterKeyMessageHandler()
    {
#pragma warning disable CS0618
        _keyMessageHandler ??= HandleKeyMessage;
        KeyEventManager.Instance.KeyMessageHandler = _keyMessageHandler;
#pragma warning restore CS0618
    }

    public void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        if (_globalHandlersRegistered)
        {
            GestureBinding.PointerEventReceived -= HandlePointerEvent;
#pragma warning disable CS0618
            if (ReferenceEquals(KeyEventManager.Instance.KeyMessageHandler, _keyMessageHandler))
            {
                KeyEventManager.Instance.KeyMessageHandler = null;
            }
#pragma warning restore CS0618
            SemanticsBinding.Instance.RemoveSemanticsActionListener(HandleSemanticsAction);
            _globalHandlersRegistered = false;
        }

        _listeners = new HashedObserverList<Action<FocusHighlightMode>>();
    }

    /// <summary>C#-only: returns the highlight state to its initial values for the next test.</summary>
    public void ResetForTests()
    {
        _listeners.Clear();
        _earlyKeyEventHandlers.Clear();
        _lateKeyEventHandlers.Clear();
        _highlightMode = null;
        _strategy = FocusHighlightStrategy.Automatic;
        _lastInteractionRequiresTraditionalHighlights = null;
    }

    /// <summary>Notifies the highlight mode listeners of the current <see cref="HighlightMode"/>.</summary>
    public void NotifyListeners()
    {
        if (_listeners.IsEmpty)
        {
            return;
        }

        List<Action<FocusHighlightMode>> localListeners = _listeners.ToList();
        foreach (Action<FocusHighlightMode> listener in localListeners)
        {
            try
            {
                if (_listeners.Contains(listener))
                {
                    listener(HighlightMode);
                }
            }
            catch (Exception exception)
            {
                InformationCollector? collector = null;
                if (Constants.KDebugMode)
                {
                    collector = () =>
                    [
                        new DiagnosticsProperty<HighlightModeManager>(
                            "The HighlightModeManager sending notification was",
                            this,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ];
                }

                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "widgets library",
                    context: new ErrorDescription("while dispatching notifications for HighlightModeManager"),
                    informationCollector: collector));
            }
        }
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager.handlePointerEvent</c>.</summary>
    public void HandlePointerEvent(PointerEvent @event)
    {
        switch (@event.Kind)
        {
            case PointerDeviceKind.Touch:
            case PointerDeviceKind.Stylus:
            case PointerDeviceKind.InvertedStylus:
                if (_lastInteractionRequiresTraditionalHighlights != true)
                {
                    _lastInteractionRequiresTraditionalHighlights = true;
                    UpdateMode();
                }

                break;
            case PointerDeviceKind.Mouse:
            case PointerDeviceKind.Trackpad:
            case PointerDeviceKind.Unknown:
                break;
        }
    }

    /// <summary>
    /// C#-only: the highlight update for a key event the hardware keyboard consumed before it reached
    /// the focus tree (<see cref="FocusManager.HandleKeyEvent"/>).
    /// </summary>
    public void UpdateModeForKeyEvent()
    {
        if (_lastInteractionRequiresTraditionalHighlights == false)
        {
            return;
        }

        _lastInteractionRequiresTraditionalHighlights = false;
        UpdateMode();
    }

#pragma warning disable CS0618
    // KeyEvent.FLAG_SOFT_KEYBOARD
    private const int KAndroidSoftKeyboardFlag = 0x00000002;

    // KeyCharacterMap.VIRTUAL_KEYBOARD
    private const int KAndroidVirtualKeyboardDeviceId = -1;

    private static bool IsKeyMessageFromAndroidIme(KeyMessage message)
    {
        RawKeyEvent? rawEvent = message.RawEvent;
        if (rawEvent == null)
        {
            return false;
        }

        RawKeyEventData data = rawEvent.Data;
        if (data is not RawKeyEventDataAndroid android)
        {
            return false;
        }

        return (android.Flags & KAndroidSoftKeyboardFlag) != 0
               || android.DeviceId == KAndroidVirtualKeyboardDeviceId;
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager.handleKeyMessage</c>.</summary>
    public bool HandleKeyMessage(KeyMessage message)
    {
        // Update highlightMode first, since things responding to the keys might
        // look at the highlight mode, and it should be accurate.
        if (_lastInteractionRequiresTraditionalHighlights != false)
        {
            // Android sends raw key events for "Backspace" key events from virtual
            // keyboards. These shouldn't switch the highlight mode to traditional.
            // See: https://github.com/flutter/flutter/issues/180746
            bool isFromVirtualKeyboard = IsKeyMessageFromAndroidIme(message);
            if (!isFromVirtualKeyboard)
            {
                _lastInteractionRequiresTraditionalHighlights = false;
                UpdateMode();
            }
        }

        System.Diagnostics.Debug.Assert(FocusManager.FocusDebug(() => $"Received key event {message}"));
        if (_focusManager.PrimaryFocus == null)
        {
            System.Diagnostics.Debug.Assert(
                FocusManager.FocusDebug(() => $"No primary focus for key event, ignored: {message}"));
            return false;
        }

        bool handled = false;
        // Check to see if any of the early handlers handle the key. If so, then
        // return early.
        if (_earlyKeyEventHandlers.IsNotEmpty)
        {
            var results = new List<KeyEventResult>();
            // Make a copy to prevent problems if the list is modified during iteration.
            foreach (OnKeyEventCallback callback in _earlyKeyEventHandlers.ToList())
            {
                foreach (KeyEvent keyEvent in message.Events)
                {
                    results.Add(callback(keyEvent));
                }
            }

            switch (KeyEventResults.Combine(results))
            {
                case KeyEventResult.Ignored:
                    break;
                case KeyEventResult.Handled:
                    System.Diagnostics.Debug.Assert(
                        FocusManager.FocusDebug(() => $"Key event {message} handled by early key event callback."));
                    handled = true;
                    break;
                case KeyEventResult.SkipRemainingHandlers:
                    System.Diagnostics.Debug.Assert(
                        FocusManager.FocusDebug(
                            () => $"Key event {message} propagation stopped by early key event callback."));
                    handled = false;
                    break;
            }
        }

        if (handled)
        {
            return true;
        }

        // Walk the current focus from the leaf to the root, calling each node's
        // onKeyEvent on the way up, and if one responds that they handled it or
        // want to stop propagation, stop.
        FocusNode primaryFocus = _focusManager.PrimaryFocus;
        foreach (FocusNode node in Enumerable.Repeat(primaryFocus, 1).Concat(primaryFocus.Ancestors))
        {
            var results = new List<KeyEventResult>();
            foreach (KeyEvent keyEvent in message.Events)
            {
                results.Add(node.HandleKeyEvent(keyEvent));
            }

            if (node.OnKey != null && message.RawEvent != null)
            {
                results.Add(node.OnKey(node, message.RawEvent));
            }

            KeyEventResult result = KeyEventResults.Combine(results);
            if (result == KeyEventResult.Ignored)
            {
                continue;
            }

            if (result == KeyEventResult.Handled)
            {
                System.Diagnostics.Debug.Assert(
                    FocusManager.FocusDebug(() => $"Node {node} handled key event {message}."));
                handled = true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(
                    FocusManager.FocusDebug(() => $"Node {node} stopped key event propagation: {message}."));
                handled = false;
            }

            // Only KeyEventResult.ignored will continue the for loop. All other
            // options will stop the event propagation.
            break;
        }

        // Check to see if any late key event handlers want to handle the event.
        if (!handled && _lateKeyEventHandlers.IsNotEmpty)
        {
            var results = new List<KeyEventResult>();
            // Make a copy to prevent problems if the list is modified during iteration.
            foreach (OnKeyEventCallback callback in _lateKeyEventHandlers.ToList())
            {
                foreach (KeyEvent keyEvent in message.Events)
                {
                    results.Add(callback(keyEvent));
                }
            }

            switch (KeyEventResults.Combine(results))
            {
                case KeyEventResult.Ignored:
                    break;
                case KeyEventResult.Handled:
                    System.Diagnostics.Debug.Assert(
                        FocusManager.FocusDebug(() => $"Key event {message} handled by late key event callback."));
                    handled = true;
                    break;
                case KeyEventResult.SkipRemainingHandlers:
                    System.Diagnostics.Debug.Assert(
                        FocusManager.FocusDebug(
                            () => $"Key event {message} propagation stopped by late key event callback."));
                    handled = false;
                    break;
            }
        }

        if (!handled)
        {
            System.Diagnostics.Debug.Assert(
                FocusManager.FocusDebug(() => $"Key event not handled by focus system: {message}."));
        }

        return handled;
    }
#pragma warning restore CS0618

    /// <summary>Dart parity source: <c>_HighlightModeManager.handleSemanticsAction</c>.</summary>
    public void HandleSemanticsAction(SemanticsActionEvent semanticsActionEvent)
    {
        if (Constants.KIsWeb
            && semanticsActionEvent.Type == SemanticsActions.Focus
            && _lastInteractionRequiresTraditionalHighlights != true)
        {
            _lastInteractionRequiresTraditionalHighlights = true;
            UpdateMode();
        }
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager.updateMode</c>.</summary>
    public void UpdateMode()
    {
        FocusHighlightMode newMode;
        switch (Strategy)
        {
            case FocusHighlightStrategy.Automatic:
                if (_lastInteractionRequiresTraditionalHighlights == null)
                {
                    // If we don't have any information about the last interaction yet,
                    // then just rely on the default value for the platform, which will be
                    // determined based on the target platform if _highlightMode is not
                    // set.
                    return;
                }

                newMode = _lastInteractionRequiresTraditionalHighlights.Value
                    ? FocusHighlightMode.Touch
                    : FocusHighlightMode.Traditional;
                break;
            case FocusHighlightStrategy.AlwaysTouch:
                newMode = FocusHighlightMode.Touch;
                break;
            case FocusHighlightStrategy.AlwaysTraditional:
            default:
                newMode = FocusHighlightMode.Traditional;
                break;
        }

        // We can't just compare newMode with _highlightMode here, since
        // _highlightMode could be null, so we want to compare with the return value
        // for the getter, since that's what clients will be looking at.
        FocusHighlightMode oldMode = HighlightMode;
        _highlightMode = newMode;
        if (HighlightMode != oldMode)
        {
            NotifyListeners();
        }
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager._defaultModeForPlatform</c>.</summary>
    private static FocusHighlightMode DefaultModeForPlatform
    {
        get
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                    // Plumix-only guard: a manager can outlive (or precede) the renderer binding.
                    if (RendererBinding.HasInstance && RendererBinding.Instance.MouseTracker.MouseIsConnected)
                    {
                        return FocusHighlightMode.Traditional;
                    }

                    return FocusHighlightMode.Touch;
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                default:
                    return FocusHighlightMode.Traditional;
            }
        }
    }
}
