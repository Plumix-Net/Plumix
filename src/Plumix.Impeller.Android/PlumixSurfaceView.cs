using Android.Content;
using Android.Text;
using Android.Views;
using Android.Views.Accessibility;
using Android.Views.InputMethods;
using Java.Lang;
using Plumix.UI;
using KeyEvent = Android.Views.KeyEvent;
using Math = System.Math;

namespace Plumix.Impeller.Android;

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

    public PlumixSurfaceView(Context context)
        : base(context)
    {
        Focusable = true;
        FocusableInTouchMode = true;
        _ime = (InputMethodManager)context.GetSystemService(Context.InputMethodService)!;
        _editable = new AndroidEditingState(this);
        _editable.Changed += SendEditableState;
    }

    // The plugin is process-wide and outlives this view's Activity: only an attached view listens to it.
    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _plugin.ShowRequested += ShowKeyboard;
        _plugin.HideRequested += HideKeyboard;
        _plugin.ClientChanged += OnClientChanged;
        _plugin.EditingStateChanged += ApplyFrameworkState;
    }

    protected override void OnDetachedFromWindow()
    {
        _plugin.ShowRequested -= ShowKeyboard;
        _plugin.HideRequested -= HideKeyboard;
        _plugin.ClientChanged -= OnClientChanged;
        _plugin.EditingStateChanged -= ApplyFrameworkState;
        base.OnDetachedFromWindow();
    }

    /// <summary>The virtual-view tree accessibility services read; set by the Activity.</summary>
    public PlumixAccessibilityNodeProvider? AccessibilityProvider { get; set; }

    // FlutterView.getAccessibilityNodeProvider: the bridge only while an accessibility service is on.
    public override AccessibilityNodeProvider? AccessibilityNodeProvider =>
        AccessibilityProvider is { IsAccessibilityEnabled: true } provider ? provider : base.AccessibilityNodeProvider;

    // FlutterView.onHoverEvent: touch exploration goes to the accessibility bridge.
    public override bool OnHoverEvent(MotionEvent? e) =>
        (e is not null && AccessibilityProvider?.OnHoverEvent(e) == true) || base.OnHoverEvent(e);

    public override bool OnCheckIsTextEditor() => _plugin.ClientId is not null;

    /// <summary>
    /// Hardware keys (and the key events `adb shell input` injects) while a client is attached go to the
    /// connection's key handling, as the engine's keyboard manager hands the framework's unhandled keys to
    /// <c>InputConnectionAdaptor.handleKeyEvent</c>.
    /// </summary>
    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e) =>
        (e is not null && _plugin.ActiveModel is not null && _connection?.HandleKeyEvent(e) == true)
        || base.OnKeyDown(keyCode, e);

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

    private void HideKeyboard() => _ime.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);

    // TextInputPlugin.setTextInputClient restarts the IME once the client's state arrives;
    // clearTextInputClient restarts it at once, so the IME drops the old configuration.
    private void OnClientChanged()
    {
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
    private void SendEditableState() => _plugin.UpdateEditingStateFromHost(
        _editable.ToString(),
        _editable.SelectionStart,
        _editable.SelectionEnd,
        _editable.ComposingStart,
        _editable.ComposingEnd);

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
