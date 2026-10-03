using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: flutter_test's keyboard and text entry — `sendKeyEvent`/`sendKeyDownEvent`/
// `sendKeyUpEvent` (controller.dart over event_simulation.dart), `enterText`/`showKeyboard` and
// `testTextInput` (widget_tester.dart, binding.dart).

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    private TestTextInput? _testTextInput;
    private EditableText.EditableTextState? _focusedEditable;

    /// <summary>
    /// Dart's <c>tester.testTextInput</c>: the mock platform side of the text input channel. Registered
    /// on first use (or from the start with <c>registerTestTextInput: true</c>) and unregistered when the
    /// tester is disposed.
    /// </summary>
    public TestTextInput TestTextInput
    {
        get
        {
            if (_testTextInput is null)
            {
                _testTextInput = new TestTextInput(onCleared: () => _focusedEditable = null);
                if (Plumix.UI.TextInput.CurrentConnection is { } connection)
                {
                    _testTextInput.AdoptClient(connection.Id);
                }
            }

            return _testTextInput;
        }
    }

    /// <summary>
    /// Dart's <c>tester.sendKeyEvent(key)</c>: a key down then a key up, as a physical keyboard sends them
    /// (<c>simulateKeyDownEvent</c>/<c>simulateKeyUpEvent</c>). Returns whether the down was handled.
    /// </summary>
    /// <remarks>
    /// Modifiers held with <see cref="SendKeyDownEvent"/> stay held: <c>SendKeyDownEvent(ShiftLeft);
    /// SendKeyEvent(Tab); SendKeyUpEvent(ShiftLeft)</c> is a shift+tab.
    /// </remarks>
    public bool SendKeyEvent(LogicalKeyboardKey key, string? character = null, PhysicalKeyboardKey? physicalKey = null)
    {
        bool handled = SendKeyDownEvent(key, character, physicalKey);
        SendKeyUpEvent(key, physicalKey);
        return handled;
    }

    /// <summary>
    /// Dart's <c>tester.sendKeyDownEvent(key)</c>. A one-character key label (lower-cased) is the event's
    /// character unless one is given; a down the framework leaves unhandled goes on to
    /// <see cref="TestTextInput"/>.
    /// </summary>
    public bool SendKeyDownEvent(
        LogicalKeyboardKey key,
        string? character = null,
        PhysicalKeyboardKey? physicalKey = null)
    {
        LogicalKeyboardKey logicalKey = KeySynonym(key);
        bool handled = FocusManager.Instance.HandleKeyEvent(new KeyDownEvent(
            physicalKey ?? KeySim.PhysicalFor(logicalKey),
            logicalKey,
            character: character ?? KeyLabelCharacter(key)));
        Scheduler.FlushMicrotasks();
        if (!handled)
        {
            _testTextInput?.HandleKeyDownEvent(key);
        }

        return handled;
    }

    /// <summary>Dart's <c>tester.sendKeyUpEvent(key)</c>.</summary>
    public bool SendKeyUpEvent(LogicalKeyboardKey key, PhysicalKeyboardKey? physicalKey = null)
    {
        LogicalKeyboardKey logicalKey = KeySynonym(key);
        bool handled = FocusManager.Instance.HandleKeyEvent(new KeyUpEvent(
            physicalKey ?? KeySim.PhysicalFor(logicalKey),
            logicalKey));
        Scheduler.FlushMicrotasks();
        if (!handled)
        {
            _testTextInput?.HandleKeyUpEvent(key);
        }

        return handled;
    }

    /// <summary>
    /// Dart's <c>tester.showKeyboard(finder)</c>: the <see cref="EditableText"/> at or below the single
    /// match requests the keyboard (focusing it and opening its input connection), then a frame is
    /// pumped.
    /// </summary>
    public void ShowKeyboard(Finder finder)
    {
        _ = TestTextInput;
        EditableText.EditableTextState editable = State<EditableText.EditableTextState>(Find.Descendant(
            of: finder,
            matching: Find.ByType<EditableText>(skipOffstage: finder.SkipOffstage),
            matchRoot: true,
            skipOffstage: finder.SkipOffstage));
        // Dart's `binding.focusedEditable = editable`, which requests the keyboard when it changes.
        if (!ReferenceEquals(_focusedEditable, editable))
        {
            _focusedEditable = editable;
            editable.RequestKeyboard();
        }

        Pump();
    }

    /// <summary>Dart's <c>tester.showKeyboard</c> for an element already found.</summary>
    public void ShowKeyboard(Element element) => ShowKeyboard(Find.ByElement(element));

    /// <summary>
    /// Dart's <c>tester.enterText(finder, text)</c>: <see cref="ShowKeyboard(Finder)"/>, then the platform
    /// replaces the field's text with <paramref name="text"/> (caret at the end). Pump afterwards to see it.
    /// </summary>
    public void EnterText(Finder finder, string text)
    {
        ShowKeyboard(finder);
        TestTextInput.EnterText(text);
        Scheduler.FlushMicrotasks();
    }

    /// <summary>Dart's <c>tester.enterText</c> for an element already found.</summary>
    public void EnterText(Element element, string text) => EnterText(Find.ByElement(element), text);

    // event_simulation.dart's `_getKeySynonym`: the generic modifiers travel as their left keys.
    private static LogicalKeyboardKey KeySynonym(LogicalKeyboardKey key)
    {
        if (key == LogicalKeyboardKey.Shift)
        {
            return LogicalKeyboardKey.ShiftLeft;
        }

        if (key == LogicalKeyboardKey.Alt)
        {
            return LogicalKeyboardKey.AltLeft;
        }

        if (key == LogicalKeyboardKey.Meta)
        {
            return LogicalKeyboardKey.MetaLeft;
        }

        return key == LogicalKeyboardKey.Control ? LogicalKeyboardKey.ControlLeft : key;
    }

    // event_simulation.dart's `_keyLabel`.
    private static string? KeyLabelCharacter(LogicalKeyboardKey key)
    {
        string keyLabel = key.KeyLabel;
        return keyLabel.Length == 1 ? keyLabel.ToLowerInvariant() : null;
    }
}
