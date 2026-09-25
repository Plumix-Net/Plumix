using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Color = Avalonia.Media.Color;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_test.dart — the editing
// pipeline (`updateEditingValue` → `_formatAndSetValue`, batch edits, the remote value), the
// keyboard actions that are not in editable_text_shortcuts_test.dart (overriding shortcuts and
// actions, read-only shortcuts, home/end, macOS selectors, transpose, obscured code points, undo),
// tap-outside, and `performAction`.
public sealed class EditableTextEditingDartParityTests : IDisposable
{
    private const string TestText =
        "Now is the time for\n"
        + "all good people\n"
        + "to come to the aid\n"
        + "of their country.";

    private readonly RecordingTextInputControl _control = new();
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new(debugLabel: "EditableText Node");

    public EditableTextEditingDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        UI.TextInput.SetInputControl(_control);
    }

    public void Dispose()
    {
        UI.TextInput.RestorePlatformInputControl();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms => new(Enum.GetValues<TargetPlatform>());

    private EditableText Field(
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        Action<string>? onChanged = null,
        bool readOnly = false,
        bool obscureText = false,
        bool multiline = true,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        TextSelectionControls? selectionControls = null,
        bool autofocus = true) =>
        new(
            controller: _controller,
            focusNode: _focusNode,
            autofocus: autofocus,
            style: new TextStyle(FontSize: 10.0),
            padding: new Thickness(0),
            cursorWidth: 0,
            cursorColor: Color.FromUInt32(0xFFFF0000),
            backgroundCursorColor: Color.FromUInt32(0xFFFF0000),
            multiline: multiline,
            maxLines: multiline ? null : 1,
            inputFormatters: inputFormatters,
            onChanged: onChanged,
            readOnly: readOnly,
            obscureText: obscureText,
            onEditingComplete: onEditingComplete,
            onSubmitted: onSubmitted,
            onTapOutside: onTapOutside,
            onTapUpOutside: onTapUpOutside,
            selectionControls: selectionControls);

    private FrameworkDartTester Pump(Widget field, TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new TestWidgetsApp(
            home: new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(width: 400, height: 300, child: field))));
        tester.Pump();
        return tester;
    }

    private static EditableText.EditableTextState State(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>();

    // ------------------------------------------------------------- onChanged and formatters

    [Fact]
    public void OnChangedCallbackOnlyInvokedOnTextChanges()
    {
        int onChangedCount = 0;
        bool preventInput = false;
        TextInputFormatter formatter = TextInputFormatter.WithFunction(
            (oldValue, newValue) => preventInput ? oldValue : newValue);
        using FrameworkDartTester tester = Pump(Field(
            inputFormatters: [formatter],
            onChanged: _ => onChangedCount += 1));
        EditableText.EditableTextState state = State(tester);

        state.UpdateEditingValue(new TextEditingValue("a", composing: new TextRange(0, 1)));
        Assert.Equal(1, onChangedCount);

        state.UpdateEditingValue(new TextEditingValue("a"));
        Assert.Equal(1, onChangedCount);

        state.UpdateEditingValue(new TextEditingValue("ab"));
        Assert.Equal(2, onChangedCount);

        preventInput = true;
        state.UpdateEditingValue(new TextEditingValue("abc"));
        Assert.Equal(2, onChangedCount);
    }

    [Fact]
    public void FormattersAreSkippedIfTextHasNotChanged()
    {
        int called = 0;
        TextInputFormatter formatter = TextInputFormatter.WithFunction((_, newValue) =>
        {
            called += 1;
            return newValue;
        });
        using FrameworkDartTester tester = Pump(Field(inputFormatters: [formatter]));
        EditableText.EditableTextState state = State(tester);

        state.UpdateEditingValue(new TextEditingValue("a"));
        Assert.Equal(1, called);
        state.UpdateEditingValue(new TextEditingValue("a"));
        Assert.Equal(1, called);
        state.UpdateEditingValue(new TextEditingValue("a", TextSelection.Collapsed(1)));
        state.UpdateEditingValue(new TextEditingValue("b"));
        Assert.Equal(2, called);
    }

    [Fact]
    public void FormatterReceivesTheControllerValueAsOldValueAndRunsWhenComposingIsCommitted()
    {
        var oldValues = new List<TextEditingValue>();
        TextInputFormatter formatter = TextInputFormatter.WithFunction((oldValue, newValue) =>
        {
            oldValues.Add(oldValue);
            return newValue;
        });
        using FrameworkDartTester tester = Pump(Field(inputFormatters: [formatter]));
        EditableText.EditableTextState state = State(tester);

        state.UpdateEditingValue(new TextEditingValue("test", TextSelection.Collapsed(4)));
        state.UpdateEditingValue(new TextEditingValue(
            "test",
            TextSelection.Collapsed(4),
            composing: new TextRange(1, 2)));
        // A composing-only change does not format.
        Assert.Single(oldValues);

        // Committing the composing text does, with the composing region still on the old value.
        state.UpdateEditingValue(new TextEditingValue("test", TextSelection.Collapsed(4)));
        Assert.Equal(2, oldValues.Count);
        Assert.Equal("test", oldValues[1].Text);
        Assert.Equal(new TextRange(1, 2), oldValues[1].Composing);
    }

    [Fact]
    public void InputFormattersCanThrowErrors()
    {
        TextInputFormatter formatter = TextInputFormatter.WithFunction(
            (_, _) => throw new InvalidOperationException("Test error message"));
        using FrameworkDartTester tester = Pump(Field(inputFormatters: [formatter]));

        State(tester).UpdateEditingValue(new TextEditingValue("text", TextSelection.Collapsed(4)));

        var error = Assert.IsType<InvalidOperationException>(tester.TakeException());
        Assert.Contains("Test error message", error.Message);
        Assert.Equal("text", _controller.Text);
    }

    // --------------------------------------------------------------- batch editing

    [Fact]
    public void BatchEditingWorks()
    {
        using FrameworkDartTester tester = Pump(Field());
        EditableText.EditableTextState state = State(tester);
        state.UpdateEditingValue(new TextEditingValue("remote value"));
        _control.EditingStates.Clear();

        state.BeginBatchEdit();
        _controller.Text = "new change 1";
        Assert.Equal("new change 1", state.CurrentTextEditingValue.Text);
        Assert.Empty(_control.EditingStates);

        state.BeginBatchEdit();
        _controller.Text = "new change 2";
        Assert.Equal("new change 2", state.CurrentTextEditingValue.Text);
        Assert.Empty(_control.EditingStates);

        state.EndBatchEdit();
        Assert.Empty(_control.EditingStates);

        _controller.Text = "new change 3";
        Assert.Equal("new change 3", state.CurrentTextEditingValue.Text);
        Assert.Empty(_control.EditingStates);

        state.EndBatchEdit();
        TextEditingValue sent = Assert.Single(_control.EditingStates);
        Assert.Equal("new change 3", sent.Text);
    }

    [Fact]
    public void BatchEditsNeedToBeNestedProperly()
    {
        using FrameworkDartTester tester = Pump(Field());
        EditableText.EditableTextState state = State(tester);
        state.UpdateEditingValue(new TextEditingValue("remote value"));

        var error = Assert.Throws<AssertionError>(state.EndBatchEdit);
        Assert.Contains("Unbalanced call to endBatchEdit", error.Message);
    }

    // ------------------------------------------ editing values are not sent more than once

    [Fact]
    public void InputFromTheTextInputPluginIsSentBackOnceAfterListenersAndFormatters()
    {
        _controller.AddListener(() =>
        {
            if (!_controller.Text.EndsWith("listener", StringComparison.Ordinal))
            {
                _controller.Text += " listener";
            }
        });
        using FrameworkDartTester tester = Pump(Field(
            inputFormatters: [new LengthLimitingTextInputFormatter(6)],
            onChanged: _ => _controller.Text += " onChanged"));
        EditableText.EditableTextState state = State(tester);
        _control.EditingStates.Clear();

        state.UpdateEditingValue(new TextEditingValue("remoteremoteremote"));

        const string expected = "remote listener onChanged listener";
        Assert.Equal(expected, _controller.Text);
        TextEditingValue sent = Assert.Single(_control.EditingStates);
        Assert.Equal(expected, sent.Text);
        Assert.Equal(TextSelection.Collapsed(expected.Length), sent.Selection);

        // A duplicate value from the plugin is not sent back.
        _control.EditingStates.Clear();
        state.UpdateEditingValue(new TextEditingValue(expected, TextSelection.Collapsed(expected.Length)));
        Assert.Empty(_control.EditingStates);
    }

    [Fact]
    public void RemoteValueRejectedByTheFormatterIsSentBack()
    {
        TextInputFormatter formatter = TextInputFormatter.WithFunction((_, newValue) =>
            newValue.Text == "I will be modified by the formatter."
                ? new TextEditingValue("Flutter is the best!", TextSelection.Collapsed(20))
                : newValue);
        using FrameworkDartTester tester = Pump(Field(inputFormatters: [formatter]));
        EditableText.EditableTextState state = State(tester);
        _control.EditingStates.Clear();

        state.UpdateEditingValue(new TextEditingValue("a", _controller.Selection));
        Assert.Empty(_control.EditingStates);

        state.UpdateEditingValue(new TextEditingValue("I will be modified by the formatter.", _controller.Selection));
        TextEditingValue sent = Assert.Single(_control.EditingStates);
        Assert.Equal("Flutter is the best!", sent.Text);
        Assert.Equal(TextSelection.Collapsed(20), sent.Selection);
        Assert.Null(sent.Composing);

        // A local change is sent too.
        _controller.Value = new TextEditingValue("To be, or not to be, that is the question.");
        Assert.Equal(2, _control.EditingStates.Count);
    }

    // ------------------------------------------------------------ keyboard actions

    [Fact]
    public void CanChangeBehaviorByOverridingTextEditingShortcuts()
    {
        _controller.Text = "Now is the time for all good people";
        _controller.Selection = TextSelection.Collapsed(0);
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester();
        var moveRight = new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true);
        tester.PumpWidget(new TestWidgetsApp(
            home: new Shortcuts(
                shortcuts: new Dictionary<ShortcutActivator, Intent>
                {
                    [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = moveRight,
                    [new SingleActivator(LogicalKeyboardKey.KeyX, control: true)] = moveRight,
                    [new SingleActivator(LogicalKeyboardKey.KeyC, control: true)] = moveRight,
                    [new SingleActivator(LogicalKeyboardKey.KeyV, control: true)] = moveRight,
                    [new SingleActivator(LogicalKeyboardKey.KeyA, control: true)] = moveRight,
                },
                child: new Align(alignment: Alignment.TopLeft, child: Field()))));
        tester.Pump();

        var keys = new (LogicalKeyboardKey Key, bool Control)[]
        {
            (LogicalKeyboardKey.ArrowRight, false),
            (LogicalKeyboardKey.ArrowLeft, false),
            (LogicalKeyboardKey.KeyX, true),
            (LogicalKeyboardKey.KeyC, true),
            (LogicalKeyboardKey.KeyV, true),
            (LogicalKeyboardKey.KeyA, true),
        };
        foreach ((LogicalKeyboardKey key, bool control) in keys)
        {
            _controller.Selection = TextSelection.Collapsed(0);
            tester.Pump();
            KeySim.SendKeyCombination(key, control: control);
            Assert.Equal(TextSelection.Collapsed(1), _controller.Selection);
        }
    }

    [Fact]
    public void CanChangeTextEditingBehaviorByOverridingActions()
    {
        _controller.Text = TestText;
        _controller.Selection = TextSelection.Collapsed(0);
        bool myIntentWasCalled = false;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(ExtendSelectionByCharacterIntent)] =
                        new CallbackAction<ExtendSelectionByCharacterIntent>(_ =>
                        {
                            myIntentWasCalled = true;
                            return null;
                        }),
                },
                child: new Align(alignment: Alignment.TopLeft, child: Field()))));
        tester.Pump();

        KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowRight);

        Assert.True(myIntentWasCalled);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardShortcutsRespectReadOnly(TargetPlatform platform)
    {
        using var clipboard = new MockClipboardPlatform("read-only");
        _controller.Text = TestText;
        _controller.Selection = new TextSelection(0, 36);
        using FrameworkDartTester tester = Pump(Field(readOnly: true), platform);
        bool apple = platform is TargetPlatform.IOS or TargetPlatform.MacOS;

        // Paste
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyV, control: !apple, meta: apple);
        tester.Pump();
        Assert.Equal(TestText, _controller.Text);

        // Select all
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyA, control: !apple, meta: apple);
        tester.Pump();
        Assert.Equal(new TextSelection(0, TestText.Length), _controller.Selection);

        // Cut
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyX, control: !apple, meta: apple);
        tester.Pump();
        Assert.Equal(TestText, _controller.Text);
        Assert.Equal("read-only", clipboard.Text);

        // Copy
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyC, control: !apple, meta: apple);
        Assert.True(EventLoopPump.SpinUntil(() => clipboard.Text == TestText));

        // Delete and backspace
        KeySim.SendKeyCombination(LogicalKeyboardKey.Delete);
        KeySim.SendKeyCombination(LogicalKeyboardKey.Backspace);
        Assert.Equal(TestText, _controller.Text);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void HomeEndKeys(TargetPlatform platform)
    {
        _controller.Text = TestText;
        // Put the cursor at the end of "good" (on the second line).
        _controller.Selection = TextSelection.Collapsed(23);
        using FrameworkDartTester tester = Pump(Field(), platform);
        bool apple = platform is TargetPlatform.IOS or TargetPlatform.MacOS;

        KeySim.SendKeyCombination(LogicalKeyboardKey.Home);
        tester.Pump();
        // Apple scrolls instead of moving the caret; every other platform goes to the line start.
        Assert.Equal(apple ? TextSelection.Collapsed(23) : TextSelection.Collapsed(20), _controller.Selection);

        _controller.Selection = TextSelection.Collapsed(23);
        tester.Pump();
        KeySim.SendKeyCombination(LogicalKeyboardKey.End);
        tester.Pump();
        Assert.Equal(
            apple ? TextSelection.Collapsed(23) : TextSelection.Collapsed(35, TextAffinity.Upstream),
            _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void MacOSSelectorsWork(TargetPlatform platform)
    {
        _controller.Text = "test\nline2";
        _controller.Selection = TextSelection.Collapsed(_controller.Text.Length);
        using FrameworkDartTester tester = Pump(Field(), platform);
        EditableText.EditableTextState state = State(tester);

        state.PerformSelector("moveLeft:");
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(9), _controller.Selection);

        state.PerformSelector("moveToBeginningOfParagraph:");
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(5), _controller.Selection);

        // These both need to be handled, first moves cursor to the end of previous paragraph,
        // second moves to the beginning of paragraph.
        state.PerformSelector("moveBackward:");
        state.PerformSelector("moveToBeginningOfParagraph:");
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Fact]
    public void CtrlTTransposesNormalCharactersOnMacOS()
    {
        _controller.Text = "Now is the time for";
        _controller.Selection = TextSelection.Collapsed(0);
        using FrameworkDartTester tester = Pump(Field(), TargetPlatform.MacOS);

        // At the start of the text: nothing happens.
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("Now is the time for", _controller.Text);

        // With a non-collapsed selection: nothing happens.
        _controller.Selection = new TextSelection(4, 6);
        tester.Pump();
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("Now is the time for", _controller.Text);

        _controller.Selection = TextSelection.Collapsed(5);
        tester.Pump();
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("Now si the time for", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(6), _controller.Selection);

        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("Now s ithe time for", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(7), _controller.Selection);

        // At the end of the text: the last two characters swap and the caret stays.
        _controller.Text = "of their country.";
        _controller.Selection = TextSelection.Collapsed(_controller.Text.Length);
        tester.Pump();
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("of their countr.y", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(_controller.Text.Length), _controller.Selection);
    }

    [Fact]
    public void CtrlTTransposesExtendedGraphemeClustersOnMacOS()
    {
        _controller.Text = "👨‍👩‍👦😆";
        _controller.Selection = TextSelection.Collapsed(8);
        using FrameworkDartTester tester = Pump(Field(), TargetPlatform.MacOS);

        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("😆👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(10), _controller.Selection);

        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyT, control: true);
        Assert.Equal("👨‍👩‍👦😆", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(10), _controller.Selection);
    }

    [Fact]
    public void CodePointsAreTreatedAsSingleCharactersInObscureMode()
    {
        _controller.Text = "👨‍👩‍👦";
        _controller.Selection = TextSelection.Collapsed(8);
        using FrameworkDartTester tester = Pump(Field(obscureText: true, multiline: false));

        foreach (int expected in new[] { 6, 5, 3, 2, 0 })
        {
            KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowLeft);
            Assert.Equal(TextSelection.Collapsed(expected), _controller.Selection);
        }

        foreach (int expected in new[] { 2, 3, 5, 6, 8 })
        {
            KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowRight);
            Assert.Equal(TextSelection.Collapsed(expected), _controller.Selection);
        }

        foreach (string expected in new[] { "👨‍👩‍", "👨‍👩", "👨‍", "👨", string.Empty })
        {
            KeySim.SendKeyCombination(LogicalKeyboardKey.Backspace);
            Assert.Equal(expected, _controller.Text);
        }
    }

    // -------------------------------------------------------------------- undo

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void CanUndoAndRedoASingleInsertion(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(), platform);
        bool apple = platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        EditableText.EditableTextState state = State(tester);
        tester.Pump(TimeSpan.FromMilliseconds(500));

        state.UserUpdateTextEditingValue(
            new TextEditingValue("1", TextSelection.Collapsed(1)),
            SelectionChangedCause.Keyboard);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal("1", _controller.Text);

        // A redo before any undo does nothing.
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyZ, control: !apple, meta: apple, shift: true);
        Assert.Equal("1", _controller.Text);

        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyZ, control: !apple, meta: apple);
        Assert.Equal(string.Empty, _controller.Text);

        // Undoing again at the start of the history does nothing.
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyZ, control: !apple, meta: apple);
        Assert.Equal(string.Empty, _controller.Text);

        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyZ, control: !apple, meta: apple, shift: true);
        Assert.Equal("1", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(1), _controller.Selection);
    }

    // -------------------------------------------------------------- tap outside

    [Fact]
    public void OnTapOutsideIsCalledUponTapOutside()
    {
        int tapOutsideCount = 0;
        _focusNode.RequestFocus();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Column(children:
            [
                new Text("Outside"),
                new SizedBox(width: 400, height: 100, child: Field(onTapOutside: _ => tapOutsideCount += 1)),
            ])));
        tester.Pump();
        _focusNode.RequestFocus();
        tester.Pump();

        Element outside = tester.ElementsWithText("Outside").First();
        for (int i = 0; i < 3; i++)
        {
            tester.Tap(outside);
            tester.Pump();
        }

        Assert.Equal(3, tapOutsideCount);
        // `onTapOutside` replaces the default unfocus, so the field keeps focus.
        Assert.True(_focusNode.HasFocus);
    }

    [Fact]
    public void OnTapOutsideIsNotCalledUponTapOutsideWhenFieldIsNotFocused()
    {
        int tapOutsideCount = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Column(children:
            [
                new Text("Outside"),
                new SizedBox(
                    width: 400,
                    height: 100,
                    child: Field(onTapOutside: _ => tapOutsideCount += 1, autofocus: false)),
            ])));
        tester.Pump();

        tester.Tap(tester.ElementsWithText("Outside").First());
        tester.Pump();

        Assert.Equal(0, tapOutsideCount);
    }

    [Fact]
    public void OnTapUpOutsideIsCalledUponTapUpOutsideAfterAFocusedTapDown()
    {
        int tapUpOutsideCount = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Column(children:
            [
                new Text("Outside"),
                new SizedBox(width: 400, height: 100, child: Field(onTapUpOutside: _ => tapUpOutsideCount += 1)),
            ])));
        tester.Pump();
        _focusNode.RequestFocus();
        tester.Pump();

        Element outside = tester.ElementsWithText("Outside").First();
        for (int i = 0; i < 3; i++)
        {
            tester.Tap(outside);
            tester.Pump();
            _focusNode.RequestFocus();
            tester.Pump();
        }

        Assert.Equal(3, tapUpOutsideCount);
    }

    [Fact]
    public void DefaultTapOutsideActionUnfocusesOnDesktopButNotForTouchOnMobile()
    {
        foreach ((TargetPlatform platform, bool unfocuses) in new[]
                 {
                     (TargetPlatform.Android, false),
                     (TargetPlatform.Windows, true),
                 })
        {
            using FrameworkDartTester tester = Pump(Field(), platform);
            _focusNode.RequestFocus();
            tester.Pump();
            var action = new EditableTextTapOutsideIntent(
                _focusNode,
                new PointerDownEvent(1, PointerDeviceKind.Touch, new Point(500, 500)));
            // Invoked below the field's own `Actions`, as the field's tap region does.
            _ = Actions.Invoke(_focusNode.Context!, action);
            tester.Pump();
            Assert.Equal(!unfocuses, _focusNode.HasFocus);
        }
    }

    // ---------------------------------------------------------- performAction

    [Fact]
    public void DoneActionWithoutOnEditingCompleteUnfocusesAndSubmits()
    {
        string? submitted = null;
        _controller.Text = "value";
        using FrameworkDartTester tester = Pump(Field(multiline: false, onSubmitted: value => submitted = value));
        _focusNode.RequestFocus();
        tester.Pump();

        State(tester).PerformAction(TextInputActionType.Done);
        tester.Pump();

        Assert.Equal("value", submitted);
        Assert.False(_focusNode.HasFocus);
    }

    [Fact]
    public void NewlineActionOnAMultilineFieldDoesNotFinalizeEditing()
    {
        int completed = 0;
        using FrameworkDartTester tester = Pump(Field(onEditingComplete: () => completed++));

        State(tester).PerformAction(TextInputActionType.Newline);

        Assert.Equal(0, completed);
        Assert.True(_focusNode.HasFocus);
    }

    [Fact]
    public void OnEditingCompleteReplacesTheDefaultUnfocus()
    {
        int completed = 0;
        using FrameworkDartTester tester = Pump(Field(multiline: false, onEditingComplete: () => completed++));

        State(tester).PerformAction(TextInputActionType.Done);
        tester.Pump();

        Assert.Equal(1, completed);
        Assert.True(_focusNode.HasFocus);
    }

    // ------------------------------------------------ the platform text input plugin

    [Fact]
    public void HostPluginEnterInsertsANewlineIntoAMultilineFieldAndReportsTheAction()
    {
        _controller.Text = "ab";
        _controller.Selection = TextSelection.Collapsed(1);
        using FrameworkDartTester tester = Pump(Field());

        Assert.True(HostTextInputPlugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        Assert.Equal("a\nb", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(2), _controller.Selection);
        Assert.True(_focusNode.HasFocus);
    }

    [Fact]
    public void HostPluginEnterSubmitsASingleLineField()
    {
        string? submitted = null;
        _controller.Text = "ab";
        using FrameworkDartTester tester = Pump(Field(multiline: false, onSubmitted: value => submitted = value));

        Assert.True(HostTextInputPlugin.HandleKeyEvent(LogicalKeyboardKey.Enter));
        tester.Pump();

        Assert.Equal("ab", _controller.Text);
        Assert.Equal("ab", submitted);
    }

    [Fact]
    public void HostPluginSendsNothingWithoutAnAttachedClient()
    {
        using FrameworkDartTester tester = Pump(Field(autofocus: false));

        Assert.False(HostTextInputPlugin.HandleKeyEvent(LogicalKeyboardKey.Enter));
        Assert.False(HostTextInputPlugin.HandleKeyEvent(LogicalKeyboardKey.ArrowLeft));
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public List<TextEditingValue> EditingStates { get; } = [];

        public override void SetEditingState(TextEditingValue value) => EditingStates.Add(value);
    }
}
