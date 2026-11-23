using System.Collections;
using CoreGraphics;
using Foundation;
using Plumix.UI;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterTextInputPlugin.mm
// (FlutterTextInputPlugin) with the FlutterTextInputDelegate half of FlutterEngine.mm (the TextInputClient.* and
// ContextMenu.* calls). Like the engine, this plugin owns `flutter/textinput` on iOS: the core's HostTextInputPlugin
// models the desktop embedders. Each focused field gets an input view inside a hidden container in the Flutter view;
// password autofill groups keep one view per field so UIKit can fill and save them together. Not ported: the
// interactive keyboard dismissal (onPointerMoveForInteractiveKeyboard, which screenshots the keyboard) and Scribble.

/// <summary>The engine's text input plugin for the iOS host.</summary>
internal sealed class IosTextInputPlugin : IDisposable
{
    // kUITextInputAccessibilityEnablingDelaySeconds.
    private static readonly TimeSpan AccessibilityEnablingDelay = TimeSpan.FromSeconds(0.5);

    private readonly OptionalMethodChannel _channel = (OptionalMethodChannel)SystemChannels.TextInput;
    private readonly Dictionary<string, IosTextInputView> _autofillContext = [];
    private readonly AccessibilityHider _inputHider = new();
    private readonly WeakReference<PlumixViewController> _viewController;
    private NSTimer? _enableAccessibilityTimer;
    private bool _pendingInputViewRemoval;
    private bool _pendingAutofillRemoval;

    public IosTextInputPlugin(PlumixViewController viewController, UIView viewResponder)
    {
        _viewController = new WeakReference<PlumixViewController>(viewController);
        ViewResponder = viewResponder;
        _channel.SetPlatformMethodCallHandler(HandleMethodCall);
    }

    /// <summary>The focused field's input view.</summary>
    public IosTextInputView? ActiveView { get; private set; }

    public PlumixViewController? ViewController =>
        _viewController.TryGetTarget(out PlumixViewController? viewController) ? viewController : null;

    /// <summary>The view the input views are added to.</summary>
    public UIView? HostView => ViewController?.ViewIfLoaded;

    private UIView ViewResponder { get; }

    public void Dispose()
    {
        _channel.SetPlatformMethodCallHandler(null);
        _autofillContext.Clear();
        ClearTextInputClient();
        HideTextInput();
        RemoveEnableAccessibilityTimer();
    }

    /// <summary>showEditMenu: (iOS 16), from the platform plugin's ContextMenu.showSystemContextMenu.</summary>
    public bool ShowEditMenu(IDictionary? args)
    {
        if (ActiveView is not { IsFirstResponder: true } activeView || HostView is not { } hostView)
        {
            return false;
        }

        var encoded = args?["targetRect"] as IDictionary;
        var globalTargetRect = new CGRect(
            ToDouble(encoded?["x"]), ToDouble(encoded?["y"]), ToDouble(encoded?["width"]),
            ToDouble(encoded?["height"]));
        activeView.ShowEditMenu(hostView.ConvertRectToView(globalTargetRect, activeView), args?["items"] as IList);
        return true;
    }

    public void HideEditMenu() => ActiveView?.HideEditMenu();

    /// <summary>The selection in the host view, for the share sheet's popover on iPad.</summary>
    public CGRect? SelectionRectInView() =>
        ActiveView is { } view && HostView is { } host
            ? view.ConvertRectToView(view.SelectionRectInView(), host)
            : null;

    // FlutterTextInputDelegate, as FlutterEngine implements it.
    public void UpdateEditingClient(int client, Dictionary<string, object?> state, string? tag) =>
        InvokeMethod(
            tag is null ? "TextInputClient.updateEditingState" : "TextInputClient.updateEditingStateWithTag",
            tag is null
                ? new List<object?> { client, state }
                : new List<object?> { client, new Dictionary<string, object?> { [tag] = state } });

    public void UpdateEditingClientWithDeltas(int client, List<Dictionary<string, object?>> deltas) =>
        InvokeMethod(
            "TextInputClient.updateEditingStateWithDeltas",
            new List<object?>
            {
                client, new Dictionary<string, object?> { ["deltas"] = deltas.Cast<object?>().ToList() },
            });

    public void UpdateFloatingCursor(string state, int client, double x, double y) =>
        InvokeMethod(
            "TextInputClient.updateFloatingCursor",
            new List<object?> { client, state, new Dictionary<string, object?> { ["X"] = x, ["Y"] = y } });

    public void PerformAction(string action, int client) =>
        InvokeMethod("TextInputClient.performAction", new List<object?> { client, action });

    public void ShowAutocorrectionPromptRect(int start, int end, int client) =>
        InvokeMethod("TextInputClient.showAutocorrectionPromptRect", new List<object?> { client, start, end });

    public void InsertTextPlaceholder(CGSize size, int client) =>
        InvokeMethod(
            "TextInputClient.insertTextPlaceholder",
            new List<object?> { client, (double)size.Width, (double)size.Height });

    public void RemoveTextPlaceholder(int client) =>
        InvokeMethod("TextInputClient.removeTextPlaceholder", new List<object?> { client });

    /// <summary>
    /// Resigning first responder closes the connection, so the framework's focus follows the keyboard.
    /// </summary>
    public void DidResignFirstResponder(int client) =>
        InvokeMethod("TextInputClient.onConnectionClosed", new List<object?> { client });

    public void WillDismissEditMenu(int client) =>
        InvokePlatformMethod("ContextMenu.onDismissSystemContextMenu", new List<object?> { client });

    public void PerformContextMenuCustomAction(string actionId, int client) =>
        InvokePlatformMethod("ContextMenu.onPerformCustomAction", new List<object?> { client, actionId });

    public void SearchWeb(string text) => IosPlatformChannel.SearchWeb(text);

    public void LookUp(string text) =>
        ViewController?.PresentViewController(new UIReferenceLibraryViewController(text), true, null);

    public void Share(string text) => ViewController?.PlatformChannel?.ShowShareViewController(text);

    private Task<object?> HandleMethodCall(MethodCall call)
    {
        object? args = call.Arguments;
        switch (call.Method)
        {
            case "TextInput.show":
                ShowTextInput();
                break;
            case "TextInput.hide":
                HideTextInput();
                break;
            case "TextInput.setClient" when args is IList setClient:
                SetTextInputClient(
                    ToInt(setClient[0]), setClient[1] as IDictionary ?? new Dictionary<string, object?>());
                break;
            case "TextInput.setPlatformViewClient":
                SetPlatformViewTextInputClient();
                break;
            case "TextInput.setEditingState" when args is IDictionary state:
                ActiveView?.SetTextInputState(state);
                break;
            case "TextInput.clearClient":
                ClearTextInputClient();
                break;
            case "TextInput.setEditableSizeAndTransform" when args is IDictionary sizeAndTransform:
                SetEditableSizeAndTransform(sizeAndTransform);
                break;
            case "TextInput.setMarkedTextRect" when args is IDictionary markedRect:
                UpdateMarkedRect(markedRect);
                break;
            case "TextInput.finishAutofillContext":
                TriggerAutofillSave(args is true);
                break;
            case "TextInput.setSelectionRects" or "Scribble.setSelectionRects" when args is IList rects:
                SetSelectionRects(rects);
                break;
            case "TextInput.startLiveTextInput":
                if (ActiveView is { IsFirstResponder: true } liveTextView)
                {
                    liveTextView.CaptureTextFromCamera(null);
                }

                break;
            case "TextInput.updateConfig" when args is IDictionary config:
                UpdateConfig(config);
                break;
            default:
                throw new MissingPluginException(call.Method);
        }

        return Task.FromResult<object?>(null);
    }

    private void ShowTextInput()
    {
        if (ActiveView is not { } activeView)
        {
            return;
        }

        activeView.ViewResponder = ViewResponder;
        AddToInputParentViewIfNeeded(activeView);
        activeView.BecomeFirstResponder();
    }

    // hideTextInput: the input view leaves the hierarchy only after resigning, so switching fields does not flicker.
    private void HideTextInput()
    {
        ActiveView?.ResignFirstResponder();
        if (_pendingInputViewRemoval)
        {
            ActiveView?.RemoveFromSuperview();
            _inputHider.RemoveFromSuperview();
            _pendingInputViewRemoval = false;
        }
    }

    private void SetTextInputClient(int client, IDictionary configuration)
    {
        ResetAllClientIds();
        _pendingAutofillRemoval = false;
        _pendingInputViewRemoval = false;
        // Only the views of the new configuration are visible to autofill.
        ChangeInputViewsAutofillVisibility(false);
        ActiveView = TextInputConfiguration.AutofillTypeOf(configuration) switch
        {
            AutofillType.Regular => UpdateAndShowAutofillViews(null, configuration, isPassword: false),
            AutofillType.Password => UpdateAndShowAutofillViews(
                configuration["fields"] as IList, configuration, isPassword: true),
            _ => CreateInputViewWith(configuration),
        };
        ActiveView.SetTextInputClient(client);
        ActiveView.ReloadInputViews();

        // Views outside the autofill context are decommissioned now and removed 0.1 s later (immediate removal
        // makes the keyboard flicker, flutter/flutter#64628).
        CleanUpViewHierarchy(includeActiveView: false, clearText: true, delayRemoval: true);

        // A field focused during a semantics update would otherwise keep VoiceOver's focus on the hidden view.
        _enableAccessibilityTimer ??= NSTimer.CreateScheduledTimer(
            AccessibilityEnablingDelay, _ => EnableActiveViewAccessibility());
    }

    // createInputViewWith: a new view every time, since UIKit keys the IME's state on the input view's identity.
    private IosTextInputView CreateInputViewWith(IDictionary configuration)
    {
        if (TextInputConfiguration.AutofillIdFromDictionary(configuration) is { } autofillId)
        {
            _autofillContext.Remove(autofillId);
        }

        var newView = new IosTextInputView(this);
        newView.Configure(configuration);
        AddToInputParentViewIfNeeded(newView);
        if (configuration["fields"] is IList fields)
        {
            foreach (object? field in fields)
            {
                if (field is IDictionary dictionary
                    && TextInputConfiguration.AutofillIdFromDictionary(dictionary) is { } fieldId
                    && TextInputConfiguration.AutofillTypeOf(dictionary) == AutofillType.None)
                {
                    _autofillContext.Remove(fieldId);
                }
            }
        }

        return newView;
    }

    private IosTextInputView UpdateAndShowAutofillViews(IList? fields, IDictionary focusedField, bool isPassword)
    {
        IosTextInputView? focused = null;
        string? focusedId = TextInputConfiguration.AutofillIdFromDictionary(focusedField);
        if (fields is null)
        {
            // A field outside an autofill group never joins the context, even when password related.
            focused = GetOrCreateAutofillableView(focusedField);
            if (focusedId is not null)
            {
                _autofillContext.Remove(focusedId);
            }
        }
        else
        {
            foreach (object? item in fields)
            {
                if (item is not IDictionary field
                    || TextInputConfiguration.AutofillIdFromDictionary(field) is not { } autofillId)
                {
                    continue;
                }

                bool hasHints = TextInputConfiguration.AutofillTypeOf(field) != AutofillType.None;
                bool isFocused = autofillId == focusedId;
                if (isFocused)
                {
                    focused = GetOrCreateAutofillableView(field);
                }

                if (hasHints)
                {
                    _autofillContext[autofillId] = isFocused ? focused! : GetOrCreateAutofillableView(field);
                }
                else
                {
                    _autofillContext.Remove(autofillId);
                }
            }
        }

        return focused ?? CreateInputViewWith(focusedField);
    }

    // getOrCreateAutofillableView:isPasswordAutofill: the context's view for the id, else a new one. The engine's
    // FlutterSecureTextInputView (a UITextField masquerade for strong passwords) is not ported.
    private IosTextInputView GetOrCreateAutofillableView(IDictionary field)
    {
        string? autofillId = TextInputConfiguration.AutofillIdFromDictionary(field);
        if (autofillId is null || !_autofillContext.TryGetValue(autofillId, out IosTextInputView? inputView))
        {
            inputView = new IosTextInputView(this);
            AddToInputParentViewIfNeeded(inputView);
        }

        inputView.Configure(field);
        return inputView;
    }

    private void SetPlatformViewTextInputClient()
    {
        RemoveEnableAccessibilityTimer();
        if (ActiveView is { } activeView)
        {
            activeView.AccessibilityEnabled = false;
            activeView.RemoveFromSuperview();
        }

        _inputHider.RemoveFromSuperview();
    }

    private void ClearTextInputClient()
    {
        if (ActiveView is { } activeView)
        {
            activeView.SetTextInputClient(0);
            activeView.Frame = CGRect.Empty;
            activeView.AccessibilityEnabled = false;
        }

        RemoveEnableAccessibilityTimer();
        // Removal is deferred so the keyboard does not flicker between fields (flutter/flutter#180842).
        if (_autofillContext.Count == 0)
        {
            _pendingAutofillRemoval = false;
            if (ActiveView?.IsFirstResponder == true)
            {
                _pendingInputViewRemoval = true;
            }
            else
            {
                ActiveView?.RemoveFromSuperview();
                _inputHider.RemoveFromSuperview();
                _pendingInputViewRemoval = false;
            }
        }
        else
        {
            // The autofill context's views go when finishAutofillContext saves them.
            _pendingAutofillRemoval = true;
            _pendingInputViewRemoval = false;
        }
    }

    private void SetEditableSizeAndTransform(IDictionary dictionary)
    {
        if (ActiveView is not { } activeView || dictionary["transform"] is not IList transform)
        {
            return;
        }

        activeView.SetEditableTransform(transform);
        const int leftIndex = 12;
        const int topIndex = 13;
        int left = ToInt(transform[leftIndex]);
        int top = ToInt(transform[topIndex]);
        if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
        {
            // Where a Scribble-interactable element would be.
            int width = ToInt(dictionary["width"]);
            int height = ToInt(dictionary["height"]);
            _inputHider.Frame = new CGRect(left, top, width, height);
            activeView.Frame = new CGRect(0, 0, width, height);
            activeView.TintColor = UIColor.Clear;
        }
        else if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            // The system auto-correction highlight otherwise shows at the top left corner.
            _inputHider.Frame = new CGRect(left, top, 0, 0);
        }
    }

    private void UpdateMarkedRect(IDictionary dictionary)
    {
        var rect = new CGRect(
            ToDouble(dictionary["x"]), ToDouble(dictionary["y"]), ToDouble(dictionary["width"]),
            ToDouble(dictionary["height"]));
        if (ActiveView is { } activeView)
        {
            activeView.MarkedRect = rect.Width < 0 && rect.Height < 0 ? IosTextInputView.InvalidFirstRect : rect;
        }
    }

    // setSelectionRects: [x, y, width, height, position, direction] per character; direction 1 is LTR.
    private void SetSelectionRects(IList encodedRects)
    {
        List<FlutterTextSelectionRect> rects = new(encodedRects.Count);
        foreach (object? item in encodedRects)
        {
            if (item is not IList encoded || encoded.Count < 6)
            {
                continue;
            }

            rects.Add(new FlutterTextSelectionRect(
                new CGRect(ToDouble(encoded[0]), ToDouble(encoded[1]), ToDouble(encoded[2]), ToDouble(encoded[3])),
                ToInt(encoded[4]),
                ToInt(encoded[5]) == 1 ? NSWritingDirection.LeftToRight : NSWritingDirection.RightToLeft));
        }

        if (ActiveView is { } activeView)
        {
            activeView.SelectionRects = rects;
        }
    }

    private void TriggerAutofillSave(bool saveEntries)
    {
        ActiveView?.ResignFirstResponder();
        if (saveEntries)
        {
            // The context's fields become visible and are removed, which triggers UIKit's save.
            CleanUpViewHierarchy(includeActiveView: true, clearText: true, delayRemoval: false);
            _autofillContext.Clear();
            ChangeInputViewsAutofillVisibility(true);
        }
        else
        {
            _autofillContext.Clear();
        }

        CleanUpViewHierarchy(includeActiveView: true, clearText: !saveEntries, delayRemoval: false);
        if (_pendingAutofillRemoval)
        {
            ActiveView?.RemoveFromSuperview();
            _inputHider.RemoveFromSuperview();
            _pendingAutofillRemoval = false;
            _pendingInputViewRemoval = false;
        }

        if (ActiveView is { } activeView)
        {
            AddToInputParentViewIfNeeded(activeView);
        }
    }

    // updateConfig: a changed secureTextEntry needs reloadInputViews (the spacebar cursor drag depends on it).
    private void UpdateConfig(IDictionary dictionary)
    {
        bool isSecureTextEntry = dictionary["obscureText"] is true;
        foreach (IosTextInputView inputView in TextInputViews)
        {
            if (inputView.SecureTextEntry != isSecureTextEntry)
            {
                inputView.SecureTextEntry = isSecureTextEntry;
                inputView.ReloadInputViews();
            }
        }
    }

    private IEnumerable<IosTextInputView> TextInputViews => _inputHider.Subviews.OfType<IosTextInputView>().ToList();

    private void CleanUpViewHierarchy(bool includeActiveView, bool clearText, bool delayRemoval)
    {
        foreach (IosTextInputView inputView in TextInputViews)
        {
            if ((!includeActiveView && inputView == ActiveView)
                || (inputView.AutofillId is { } id && _autofillContext.TryGetValue(id, out IosTextInputView? kept)
                    && kept == inputView))
            {
                continue;
            }

            if (clearText)
            {
                inputView.ReplaceRangeLocal(new NSRange(0, inputView.Text.Length), string.Empty);
            }

            if (delayRemoval)
            {
                NSTimer.CreateScheduledTimer(0.1, _ => inputView.RemoveFromSuperview());
            }
            else
            {
                inputView.RemoveFromSuperview();
            }
        }
    }

    private void ChangeInputViewsAutofillVisibility(bool visible)
    {
        foreach (IosTextInputView inputView in TextInputViews)
        {
            inputView.IsVisibleToAutofill = visible;
        }
    }

    private void ResetAllClientIds()
    {
        foreach (IosTextInputView inputView in TextInputViews)
        {
            inputView.SetTextInputClient(0);
        }
    }

    private void AddToInputParentViewIfNeeded(IosTextInputView inputView)
    {
        if (!inputView.IsDescendantOfView(_inputHider))
        {
            _inputHider.AddSubview(inputView);
        }

        // A view controller whose view left the engine keeps the hider out (flutter/flutter#106404).
        if (HostView is { } parentView && _inputHider.Superview != parentView)
        {
            parentView.AddSubview(_inputHider);
        }
    }

    private void EnableActiveViewAccessibility()
    {
        if (ActiveView is { IsFirstResponder: true } activeView)
        {
            activeView.AccessibilityEnabled = true;
        }

        RemoveEnableAccessibilityTimer();
    }

    private void RemoveEnableAccessibilityTimer()
    {
        _enableAccessibilityTimer?.Invalidate();
        _enableAccessibilityTimer = null;
    }

    private void InvokeMethod(string method, object? arguments) =>
        _ = _channel.BinaryMessenger.HandlePlatformMessage(
            _channel.Name, _channel.Codec.EncodeMethodCall(new MethodCall(method, arguments)), null);

    private static void InvokePlatformMethod(string method, object? arguments) =>
        _ = SystemChannels.Platform.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.Platform.Name,
            SystemChannels.Platform.Codec.EncodeMethodCall(new MethodCall(method, arguments)),
            null);

    private static int ToInt(object? value) =>
        value is null ? 0 : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

    private static double ToDouble(object? value) =>
        value is null ? 0 : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// FlutterTextInputViewAccessibilityHider: keeps the input views out of VoiceOver, which reaches the field through
    /// the semantics tree instead.
    /// </summary>
    private sealed class AccessibilityHider : UIView
    {
        public override bool AccessibilityElementsHidden
        {
            get => true;
            set { }
        }
    }
}
