// C#-only infrastructure; no Dart parity source. Stands in for the engine's platform text input
// plugins (macOS `FlutterTextInputPlugin`, Windows `TextInputPlugin`, Linux
// `FlTextInputHandler`), which receive every key event the framework leaves unhandled while a text
// input client is attached. Flutter's `DefaultTextEditingShortcuts` relies on that hand-off: on
// macOS the arrow, delete, page and home/end keys map to `DoNothingAndStopPropagationTextIntent`
// so that AppKit can turn them into `NSStandardKeyBindingResponding` selectors, which come back to
// `EditableText.performSelector`; on every desktop platform Enter reaches the plugin, which inserts
// the newline for multiline fields and reports the input action. Plumix hosts have no engine, so
// they call this class with the keys the framework did not handle.

using Plumix.Widgets;

namespace Plumix.UI;

/// <summary>
/// The key handling the engine's platform text input plugin performs for the attached
/// <see cref="ITextInputClient"/> when the framework does not handle a key down event.
/// </summary>
public static class HostTextInputPlugin
{
    /// <summary>
    /// Handles a key down (or repeat) event the framework left unhandled. Returns whether the
    /// plugin consumed it; nothing is consumed while no text input client is attached.
    /// </summary>
    public static bool HandleKeyEvent(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false)
    {
        TextInputConnection? connection = TextInput.CurrentConnection;
        TextInputConfiguration? configuration = TextInput.CurrentConfiguration;
        if (connection is null || configuration is null)
        {
            return false;
        }

        bool isEnter = key.Equals(LogicalKeyboardKey.Enter) || key.Equals(LogicalKeyboardKey.NumpadEnter);
        if (isEnter && !control && !alt && !meta)
        {
            // FlutterTextInputPlugin.insertNewline:, TextInputPlugin::EnterPressed and
            // fl_text_input_handler's Return handling agree: a multiline client whose action is
            // newline gets "\n" first, then every client is told the input action.
            InsertNewline(connection.Client, configuration);
            return true;
        }

        if (PlatformDefaults.TargetPlatform != TargetPlatform.MacOS)
        {
            return false;
        }

        IReadOnlyList<string>? selectors = MacOSStandardKeyBindings.SelectorsFor(key, control, shift, alt, meta);
        if (selectors is null)
        {
            return false;
        }

        foreach (string selector in selectors)
        {
            connection.Client.PerformSelector(selector);
        }

        return true;
    }

    private static void InsertNewline(ITextInputClient client, TextInputConfiguration configuration)
    {
        if (configuration.IsMultiline
            && configuration.InputAction == TextInputActionType.Newline
            && client.CurrentTextEditingValue is { } value)
        {
            TextSelection selection = value.Selection;
            TextRange replaced = selection.IsValid
                ? new TextRange(selection.Start, selection.End)
                : new TextRange(value.Text.Length, value.Text.Length);
            string text = string.Concat(value.Text.AsSpan(0, replaced.Start), "\n", value.Text.AsSpan(replaced.End));
            client.UpdateEditingValue(new TextEditingValue(
                text: text,
                selection: TextSelection.Collapsed(replaced.Start + 1)));
        }

        client.PerformAction(configuration.InputAction);
    }
}

/// <summary>
/// The AppKit key bindings (<c>NSStandardKeyBindingResponding</c>) that
/// <c>NSTextInputContext.interpretKeyEvents</c> turns into <c>doCommandBySelector:</c> calls for
/// the keys Flutter's macOS text-editing shortcuts hand to the platform. The table is the one
/// flutter_test's <c>MacOSTestTextInputKeyHandler</c> uses
/// (<c>flutter_test/lib/src/test_text_input_key_handler.dart</c>), which Flutter's own macOS text
/// editing tests run against.
/// </summary>
public static class MacOSStandardKeyBindings
{
    private const int Control = 1;
    private const int Shift = 2;
    private const int Alt = 4;
    private const int Meta = 8;

    private static readonly Dictionary<(LogicalKeyboardKey Key, int Modifiers), string[]> Bindings = Build();

    /// <summary>The selectors AppKit sends for the key, or null when AppKit binds nothing to it.</summary>
    public static IReadOnlyList<string>? SelectorsFor(
        LogicalKeyboardKey key,
        bool control,
        bool shift,
        bool alt,
        bool meta)
    {
        int modifiers = (control ? Control : 0) | (shift ? Shift : 0) | (alt ? Alt : 0) | (meta ? Meta : 0);
        return Bindings.GetValueOrDefault((key, modifiers));
    }

    private static Dictionary<(LogicalKeyboardKey Key, int Modifiers), string[]> Build()
    {
        var map = new Dictionary<(LogicalKeyboardKey Key, int Modifiers), string[]>();

        void Bind(LogicalKeyboardKey key, int modifiers, params string[] selectors) =>
            map[(key, modifiers)] = selectors;

        foreach (int shift in new[] { Shift, 0 })
        {
            Bind(LogicalKeyboardKey.Backspace, shift, "deleteBackward:");
            Bind(LogicalKeyboardKey.Backspace, Alt | shift, "deleteWordBackward:");
            Bind(LogicalKeyboardKey.Backspace, Meta | shift, "deleteToBeginningOfLine:");
            Bind(LogicalKeyboardKey.Backspace, Control | shift, "deleteBackwardByDecomposingPreviousCharacter:");
            Bind(LogicalKeyboardKey.Delete, shift, "deleteForward:");
            Bind(LogicalKeyboardKey.Delete, Alt | shift, "deleteWordForward:");
            Bind(LogicalKeyboardKey.Delete, Meta | shift, "deleteToEndOfLine:");
        }

        Bind(LogicalKeyboardKey.ArrowLeft, 0, "moveLeft:");
        Bind(LogicalKeyboardKey.ArrowRight, 0, "moveRight:");
        Bind(LogicalKeyboardKey.ArrowUp, 0, "moveUp:");
        Bind(LogicalKeyboardKey.ArrowDown, 0, "moveDown:");
        Bind(LogicalKeyboardKey.ArrowLeft, Shift, "moveLeftAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowRight, Shift, "moveRightAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowUp, Shift, "moveUpAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowDown, Shift, "moveDownAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowLeft, Alt, "moveWordLeft:");
        Bind(LogicalKeyboardKey.ArrowRight, Alt, "moveWordRight:");
        Bind(LogicalKeyboardKey.ArrowUp, Alt, "moveBackward:", "moveToBeginningOfParagraph:");
        Bind(LogicalKeyboardKey.ArrowDown, Alt, "moveForward:", "moveToEndOfParagraph:");
        Bind(LogicalKeyboardKey.ArrowLeft, Alt | Shift, "moveWordLeftAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowRight, Alt | Shift, "moveWordRightAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowUp, Alt | Shift, "moveParagraphBackwardAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowDown, Alt | Shift, "moveParagraphForwardAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowLeft, Meta, "moveToLeftEndOfLine:");
        Bind(LogicalKeyboardKey.ArrowRight, Meta, "moveToRightEndOfLine:");
        Bind(LogicalKeyboardKey.ArrowUp, Meta, "moveToBeginningOfDocument:");
        Bind(LogicalKeyboardKey.ArrowDown, Meta, "moveToEndOfDocument:");
        Bind(LogicalKeyboardKey.ArrowLeft, Meta | Shift, "moveToLeftEndOfLineAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowRight, Meta | Shift, "moveToRightEndOfLineAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowUp, Meta | Shift, "moveToBeginningOfDocumentAndModifySelection:");
        Bind(LogicalKeyboardKey.ArrowDown, Meta | Shift, "moveToEndOfDocumentAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyA, Control | Shift, "moveToBeginningOfParagraphAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyA, Control, "moveToBeginningOfParagraph:");
        Bind(LogicalKeyboardKey.KeyB, Control | Shift, "moveBackwardAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyB, Control, "moveBackward:");
        Bind(LogicalKeyboardKey.KeyE, Control | Shift, "moveToEndOfParagraphAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyE, Control, "moveToEndOfParagraph:");
        Bind(LogicalKeyboardKey.KeyF, Control | Shift, "moveForwardAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyF, Control, "moveForward:");
        Bind(LogicalKeyboardKey.KeyK, Control, "deleteToEndOfParagraph");
        Bind(LogicalKeyboardKey.KeyL, Control, "centerSelectionInVisibleArea");
        Bind(LogicalKeyboardKey.KeyN, Control, "moveDown:");
        Bind(LogicalKeyboardKey.KeyN, Control | Shift, "moveDownAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyO, Control, "insertNewlineIgnoringFieldEditor:");
        Bind(LogicalKeyboardKey.KeyP, Control, "moveUp:");
        Bind(LogicalKeyboardKey.KeyP, Control | Shift, "moveUpAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyT, Control, "transpose:");
        Bind(LogicalKeyboardKey.KeyV, Control, "pageDown:");
        Bind(LogicalKeyboardKey.KeyV, Control | Shift, "pageDownAndModifySelection:");
        Bind(LogicalKeyboardKey.KeyY, Control, "yank:");
        Bind(LogicalKeyboardKey.QuoteSingle, Control, "insertSingleQuoteIgnoringSubstitution:");
        Bind(LogicalKeyboardKey.Quote, Control, "insertDoubleQuoteIgnoringSubstitution:");
        Bind(LogicalKeyboardKey.Home, 0, "scrollToBeginningOfDocument:");
        Bind(LogicalKeyboardKey.End, 0, "scrollToEndOfDocument:");
        Bind(LogicalKeyboardKey.Home, Shift, "moveToBeginningOfDocumentAndModifySelection:");
        Bind(LogicalKeyboardKey.End, Shift, "moveToEndOfDocumentAndModifySelection:");
        Bind(LogicalKeyboardKey.PageUp, 0, "scrollPageUp:");
        Bind(LogicalKeyboardKey.PageDown, 0, "scrollPageDown:");
        Bind(LogicalKeyboardKey.PageUp, Shift, "pageUpAndModifySelection:");
        Bind(LogicalKeyboardKey.PageDown, Shift, "pageDownAndModifySelection:");
        Bind(LogicalKeyboardKey.Escape, 0, "cancelOperation:");
        Bind(LogicalKeyboardKey.Enter, 0, "insertNewline:");
        Bind(LogicalKeyboardKey.Enter, Alt, "insertNewlineIgnoringFieldEditor:");
        Bind(LogicalKeyboardKey.Enter, Control, "insertLineBreak:");
        Bind(LogicalKeyboardKey.Tab, 0, "insertTab:");
        Bind(LogicalKeyboardKey.Tab, Alt, "insertTabIgnoringFieldEditor:");
        Bind(LogicalKeyboardKey.Tab, Shift, "insertBacktab:");
        return map;
    }
}
