using Android.Content;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;
using Plumix.UI;
using KeyEvent = Android.Views.KeyEvent;
using Math = System.Math;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The prototype's text input: the SurfaceView is the
// IME's editor, and its InputConnection forwards commits, composing text, deletions and editor actions to the
// core's text input plugin (UI/HostTextInputPlugin.cs), the way the engine's Android embedding forwards an
// InputConnectionAdaptor to `flutter/textinput`. The keyboard is shown and hidden when the framework asks.

/// <summary>The surface Plumix renders into, and the soft keyboard's editor.</summary>
internal sealed class PlumixSurfaceView : SurfaceView
{
    private readonly HostTextInputPlugin _plugin = HostTextInputPlugin.Instance;
    private readonly InputMethodManager _ime;

    public PlumixSurfaceView(Context context)
        : base(context)
    {
        Focusable = true;
        FocusableInTouchMode = true;
        _ime = (InputMethodManager)context.GetSystemService(Context.InputMethodService)!;
        _plugin.ShowRequested += ShowKeyboard;
        _plugin.HideRequested += HideKeyboard;
        _plugin.ClientChanged += () => _ime.RestartInput(this);
        _plugin.EditingStateChanged += ReportSelection;
    }

    public override bool OnCheckIsTextEditor() => _plugin.ClientId is not null;

    /// <summary>
    /// Hardware keys (and the key events `adb shell input` injects) while a client is attached: the
    /// editing keys go to the plugin's key handling, printable keys are inserted as text.
    /// </summary>
    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (_plugin.ActiveModel is null || e is null)
        {
            return base.OnKeyDown(keyCode, e);
        }

        LogicalKeyboardKey? key = keyCode switch
        {
            Keycode.Enter or Keycode.NumpadEnter => LogicalKeyboardKey.Enter,
            Keycode.DpadLeft => LogicalKeyboardKey.ArrowLeft,
            Keycode.DpadRight => LogicalKeyboardKey.ArrowRight,
            Keycode.DpadUp => LogicalKeyboardKey.ArrowUp,
            Keycode.DpadDown => LogicalKeyboardKey.ArrowDown,
            Keycode.MoveHome => LogicalKeyboardKey.Home,
            Keycode.MoveEnd => LogicalKeyboardKey.End,
            _ => null,
        };
        if (keyCode == Keycode.Del)
        {
            _plugin.DeleteBackward();
            return true;
        }

        if (key is not null)
        {
            return _plugin.HandleKeyEvent(key, e.IsCtrlPressed, e.IsShiftPressed, e.IsAltPressed, e.IsMetaPressed);
        }

        int unicode = e.UnicodeChar;
        if (unicode > 0 && !e.IsCtrlPressed)
        {
            _plugin.InsertText(char.ConvertFromUtf32(unicode));
            return true;
        }

        return base.OnKeyDown(keyCode, e);
    }

    public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
    {
        if (outAttrs is null || _plugin.ActiveModel is null)
        {
            return null;
        }

        outAttrs.InputType = InputTypeOf(_plugin.InputType);
        outAttrs.ImeOptions = (ImeFlags)ImeActionOf(_plugin.InputAction) | ImeFlags.NoExtractUi;
        TextInputModel.Range selection = _plugin.ActiveModel.Selection;
        outAttrs.InitialSelStart = selection.Start;
        outAttrs.InitialSelEnd = selection.End;
        return new Connection(this, _plugin);
    }

    private void ShowKeyboard()
    {
        RequestFocus();
        _ime.RestartInput(this);
        _ime.ShowSoftInput(this, ShowFlags.Implicit);
    }

    private void HideKeyboard() => _ime.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);

    private void ReportSelection()
    {
        if (_plugin.ActiveModel is not { } model)
        {
            return;
        }

        TextInputModel.Range composing = model.Composing ? model.ComposingRange : new TextInputModel.Range(-1);
        _ime.UpdateSelection(this, model.Selection.Start, model.Selection.End, composing.Start, composing.End);
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

    /// <summary>The IME's view of the attached client's text, edited through the plugin.</summary>
    private sealed class Connection(View view, HostTextInputPlugin plugin) : BaseInputConnection(view, false)
    {
        public override bool CommitText(ICharSequence? text, int newCursorPosition)
        {
            plugin.InsertText(text?.ToString() ?? string.Empty);
            return true;
        }

        public override bool SetComposingText(ICharSequence? text, int newCursorPosition)
        {
            string value = text?.ToString() ?? string.Empty;
            plugin.SetMarkedText(value, value.Length);
            return true;
        }

        public override bool FinishComposingText()
        {
            plugin.UnmarkText();
            return true;
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (plugin.ActiveModel is not { } model)
            {
                return false;
            }

            // InputConnection.deleteSurroundingText keeps the selection: the text after it goes first, so the
            // range before it keeps its offsets.
            TextInputModel.Range selection = model.Selection;
            int end = Math.Min(model.GetText().Length, selection.End + afterLength);
            if (end > selection.End)
            {
                plugin.InsertText(string.Empty, (selection.End, end - selection.End));
            }

            int start = Math.Max(0, selection.Start - beforeLength);
            if (start < selection.Start)
            {
                plugin.InsertText(string.Empty, (start, selection.Start - start));
            }

            return true;
        }

        public override bool SetSelection(int start, int end) => plugin.SetSelection(start, end);

        public override bool PerformEditorAction(ImeAction actionCode) =>
            plugin.HandleKeyEvent(LogicalKeyboardKey.Enter);

        public override bool SendKeyEvent(KeyEvent? e)
        {
            if (e is null || e.Action != KeyEventActions.Down)
            {
                return true;
            }

            switch (e.KeyCode)
            {
                case Keycode.Del:
                    plugin.DeleteBackward();
                    return true;
                case Keycode.Enter:
                case Keycode.NumpadEnter:
                    return plugin.HandleKeyEvent(LogicalKeyboardKey.Enter);
                default:
                    int unicode = e.UnicodeChar;
                    if (unicode > 0)
                    {
                        plugin.InsertText(char.ConvertFromUtf32(unicode));
                    }

                    return true;
            }
        }

        public override ICharSequence? GetTextBeforeCursorFormatted(int length, GetTextFlags flags)
        {
            if (plugin.ActiveModel is not { } model)
            {
                return null;
            }

            int cursor = model.Selection.Start;
            return new Java.Lang.String(model.GetText()[Math.Max(0, cursor - length)..cursor]);
        }

        public override ICharSequence? GetTextAfterCursorFormatted(int length, GetTextFlags flags)
        {
            if (plugin.ActiveModel is not { } model)
            {
                return null;
            }

            string text = model.GetText();
            int cursor = model.Selection.End;
            return new Java.Lang.String(text[cursor..Math.Min(text.Length, cursor + length)]);
        }
    }
}
