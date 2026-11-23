using Android.Content;
using Android.Text;
using Android.Util;
using Android.Views.Autofill;
using Android.Views;
using Android.Views.Accessibility;
using Android.Views.InputMethods;
using Java.Lang;
using Plumix.UI;
using KeyEvent = Android.Views.KeyEvent;
using Math = System.Math;

namespace Plumix.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/TextInputPlugin.java (the editor
// side: createInputConnection, setTextInputClient, setTextInputEditingState, didChangeEditingState). The
// SurfaceView is the IME's editor over a mirrored editable (AndroidEditingState) that the IME edits through
// InputConnectionAdaptor; the editable's changes go to the core's text input plugin (UI/HostTextInputPlugin.cs)
// and the framework's state comes back into it. The keyboard is shown and hidden when the framework asks.

/// <summary>The surface Plumix renders into, and the soft keyboard's editor.</summary>
internal sealed class PlumixSurfaceView : SurfaceView
{
    private readonly HostTextInputPlugin _plugin = HostTextInputPlugin.Instance;
    private readonly InputMethodManager _ime;
    private readonly AndroidEditingState _editable;
    private InputConnectionAdaptor? _connection;
    private bool _restartInputPending;
    private readonly AndroidKeyboardManager _keyboardManager;
    private readonly AndroidAutofill _autofill;
    private string _notifiedText = string.Empty;

    public PlumixSurfaceView(Context context)
        : base(context)
    {
        Focusable = true;
        FocusableInTouchMode = true;
        _ime = (InputMethodManager)context.GetSystemService(Context.InputMethodService)!;
        _editable = new AndroidEditingState(this);
        _editable.Changed += SendEditableState;
        _keyboardManager = new AndroidKeyboardManager(this, OnTextInputKeyEvent);
        _autofill = new AndroidAutofill(this, _plugin);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            // FlutterView: the text input client's fields are reported as this view's virtual children.
            ImportantForAutofill = ImportantForAutofill.Yes;
        }
    }

    /// <summary>The engine's keyboard manager for this view; the IME's key events go through it too.</summary>
    public AndroidKeyboardManager KeyboardManager => _keyboardManager;

    // The plugin is process-wide and outlives this view's Activity: only an attached view listens to it.
    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _plugin.ShowRequested += ShowKeyboard;
        _plugin.HideRequested += HideKeyboard;
        _plugin.ClientChanged += OnClientChanged;
        _plugin.EditingStateChanged += ApplyFrameworkState;
        _plugin.AutofillRequested += _autofill.NotifyViewEntered;
        _plugin.AutofillContextFinished += _autofill.FinishAutofillContext;
        SystemChannels.Keyboard.SetPlatformMethodCallHandler(HandleKeyboardMethodCall);
    }

    protected override void OnDetachedFromWindow()
    {
        _plugin.ShowRequested -= ShowKeyboard;
        _plugin.HideRequested -= HideKeyboard;
        _plugin.ClientChanged -= OnClientChanged;
        _plugin.EditingStateChanged -= ApplyFrameworkState;
        _plugin.AutofillRequested -= _autofill.NotifyViewEntered;
        _plugin.AutofillContextFinished -= _autofill.FinishAutofillContext;
        SystemChannels.Keyboard.SetPlatformMethodCallHandler(null);
        base.OnDetachedFromWindow();
    }

    /// <summary>The virtual-view tree accessibility services read; set by the Activity.</summary>
    public PlumixAccessibilityNodeProvider? AccessibilityProvider { get; set; }

    // FlutterView.getAccessibilityNodeProvider: the bridge only while an accessibility service is on.
    public override AccessibilityNodeProvider? AccessibilityNodeProvider =>
        AccessibilityProvider is { IsAccessibilityEnabled: true } provider ? provider : base.AccessibilityNodeProvider;

    /// <summary>The view's pointer input; set by the Activity.</summary>
    public AndroidTouchProcessor? TouchProcessor { get; set; }

    // FlutterView.onHoverEvent: touch exploration goes to the accessibility bridge. An unhandled hover falls
    // through to onGenericMotionEvent, where a mouse or stylus hover reaches the framework.
    public override bool OnHoverEvent(MotionEvent? e) =>
        e is not null && AccessibilityProvider?.OnHoverEvent(e) == true;

    // FlutterView.onTouchEvent: unbuffered from Android 10, so moves arrive as they happen, not once a frame.
    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null || TouchProcessor is not { } touchProcessor)
        {
            return base.OnTouchEvent(e);
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            RequestUnbufferedDispatch(e);
        }

        return touchProcessor.OnTouchEvent(e);
    }

    // FlutterView.onGenericMotionEvent: mouse and stylus hovers and wheel scrolls.
    public override bool OnGenericMotionEvent(MotionEvent? e) =>
        (e is not null && TouchProcessor?.OnGenericMotionEvent(e, Context!) == true) || base.OnGenericMotionEvent(e);

    public override bool OnCheckIsTextEditor() => _plugin.ClientId is not null;

    // FlutterView.dispatchKeyEvent: every key goes to the keyboard manager first; Android handles it only when
    // the manager sends it back through the view tree.
    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e is null)
        {
            return base.DispatchKeyEvent(e);
        }

        if (e.Action == KeyEventActions.Down && e.RepeatCount == 0)
        {
            KeyDispatcherState?.StartTracking(e, this);
        }
        else if (e.Action == KeyEventActions.Up)
        {
            KeyDispatcherState?.HandleUpEvent(e);
        }

        return _keyboardManager.HandleEvent(e) || base.DispatchKeyEvent(e);
    }

    // TextInputPlugin.handleKeyEvent: what the framework left goes to the connection while the IME takes text.
    private bool OnTextInputKeyEvent(KeyEvent e) =>
        _ime.IsAcceptingText && _plugin.ActiveModel is not null && _connection?.HandleKeyEvent(e) == true;

    // KeyboardChannel: the framework asks for the pressed keys when it starts listening.
    private Task<object?> HandleKeyboardMethodCall(MethodCall call) => call.Method == "getKeyboardState"
        ? Task.FromResult<object?>(_keyboardManager.GetKeyboardState())
        : throw new MissingPluginException(call.Method);

    // TextInputPlugin.createInputConnection.
    public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
    {
        if (outAttrs is null || _plugin.ActiveModel is null)
        {
            _connection = null;
            return null;
        }

        outAttrs.InputType = InputTypeOf(_plugin.InputType);
        outAttrs.ImeOptions = (ImeFlags)ImeActionOf(_plugin.InputAction) | ImeFlags.NoExtractUi;
        outAttrs.InitialSelStart = _editable.SelectionStart;
        outAttrs.InitialSelEnd = _editable.SelectionEnd;
        _connection = new InputConnectionAdaptor(this, _editable, outAttrs, _plugin, _ime);
        return _connection;
    }

    // TextInputPlugin.showTextInput.
    private void ShowKeyboard()
    {
        RequestFocus();
        _ime.ShowSoftInput(this, 0);
    }

    // TextInputPlugin.hideTextInput.
    private void HideKeyboard()
    {
        _autofill.NotifyViewExited();
        _ime.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);
    }

    // FlutterView.onProvideAutofillVirtualStructure and autofill: the text input plugin's autofill group.
    public override void OnProvideAutofillVirtualStructure(ViewStructure? structure, AutofillFlags flags)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        base.OnProvideAutofillVirtualStructure(structure, flags);
        if (structure is not null)
        {
            _autofill.OnProvideAutofillVirtualStructure(structure, _editable.ToString());
        }
    }

    public override void Autofill(SparseArray? values)
    {
        if (values is not null)
        {
            _autofill.Autofill(values, text => _editable.SetEditingState(text, text.Length, text.Length, -1, -1));
        }
    }

    // TextInputPlugin.setTextInputClient restarts the IME once the client's state arrives;
    // clearTextInputClient restarts it at once, so the IME drops the old configuration.
    private void OnClientChanged()
    {
        // The previous field is left; the new configuration replaces the autofill group.
        _autofill.NotifyViewExited();
        _autofill.UpdateConfiguration(_plugin.Configuration);
        if (_plugin.ClientId is null)
        {
            PostRestartInput();
        }
        else
        {
            _restartInputPending = true;
        }
    }

    // The engine restarts the IME on the platform thread, after the framework's channel call returned; here the
    // call is synchronous, and restarting closes the connection, which finishes composing and would send the
    // editable back into the framework while it is still mid-update.
    private void PostRestartInput() => Post(() => _ime.RestartInput(this));

    // TextInputPlugin.didChangeEditingState: the IME's edits go to the framework (nothing is sent when they
    // match what it already has).
    private void SendEditableState()
    {
        string text = _editable.ToString();
        if (text != _notifiedText)
        {
            // Notify the autofill manager of the value change.
            _notifiedText = text;
            _autofill.NotifyValueChanged(text);
        }

        _plugin.UpdateEditingStateFromHost(
            text,
            _editable.SelectionStart,
            _editable.SelectionEnd,
            _editable.ComposingStart,
            _editable.ComposingEnd);
    }

    // TextInputPlugin.setTextInputEditingState: the framework's state replaces the editable's, restarting the
    // IME when the framework moved a composing region itself (IMEs do not expect editors to commit it).
    private void ApplyFrameworkState()
    {
        if (_plugin.ActiveModel is not { } model)
        {
            return;
        }

        string text = model.GetText();
        TextInputModel.Range selection = model.Selection;
        (int Start, int End) composing = model.Composing
            ? (model.ComposingRange.Start, model.ComposingRange.End)
            : (-1, -1);
        (int Start, int End) editableComposing = (_editable.ComposingStart, _editable.ComposingEnd);
        if (text != _editable.ToString() || selection.Base != _editable.SelectionStart
            || selection.Extent != _editable.SelectionEnd || composing != editableComposing)
        {
            _restartInputPending |= editableComposing.Start >= 0 && composing != editableComposing;
            _editable.SetEditingState(text, selection.Base, selection.Extent, composing.Start, composing.End);
        }

        if (_restartInputPending)
        {
            _restartInputPending = false;
            PostRestartInput();
        }
    }

    private static InputTypes InputTypeOf(string? inputType) => inputType switch
    {
        "TextInputType.number" => InputTypes.ClassNumber,
        "TextInputType.phone" => InputTypes.ClassPhone,
        "TextInputType.datetime" => InputTypes.ClassDatetime,
        "TextInputType.emailAddress" => InputTypes.ClassText | InputTypes.TextVariationEmailAddress,
        "TextInputType.url" => InputTypes.ClassText | InputTypes.TextVariationUri,
        "TextInputType.visiblePassword" => InputTypes.ClassText | InputTypes.TextVariationVisiblePassword,
        "TextInputType.multiline" => InputTypes.ClassText | InputTypes.TextFlagMultiLine,
        _ => InputTypes.ClassText,
    };

    private static ImeAction ImeActionOf(string? inputAction) => inputAction switch
    {
        "TextInputAction.done" => ImeAction.Done,
        "TextInputAction.go" => ImeAction.Go,
        "TextInputAction.search" => ImeAction.Search,
        "TextInputAction.send" => ImeAction.Send,
        "TextInputAction.next" => ImeAction.Next,
        "TextInputAction.previous" => ImeAction.Previous,
        "TextInputAction.newline" => ImeAction.None,
        _ => ImeAction.Done,
    };
}
