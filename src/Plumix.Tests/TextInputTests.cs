using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using AvaloniaInputElement = Avalonia.Input.InputElement;
using AvaloniaTextSelection = Avalonia.Input.TextInput.TextSelection;
using TextInputMethodClientRequestedEventArgs = Avalonia.Input.TextInput.TextInputMethodClientRequestedEventArgs;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/editable_text.dart; flutter/packages/flutter/test/widgets/editable_text_test.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextInputTests : IDisposable
{
    public TextInputTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void FocusManager_HandleTextInput_InvokesPrimaryFocusCallback()
    {
        var manager = new FocusManager();
        string? captured = null;
        var node = new FocusNode
        {
            OnTextInput = (_, text) =>
            {
                captured = text;
                return true;
            }
        };

        manager.RegisterNode(node);
        manager.RequestFocus(node);
        Scheduler.FlushMicrotasks();

        bool handled = manager.HandleTextInput("A");

        Assert.True(handled);
        Assert.Equal("A", captured);
    }

    [Fact]
    public void FocusManager_HandleTextComposition_InvokesPrimaryFocusCallback()
    {
        var manager = new FocusManager();
        var captured = new List<(string Text, bool IsCommit)>();
        var node = new FocusNode
        {
            OnTextComposition = (_, text, isCommit) =>
            {
                captured.Add((text, isCommit));
                return true;
            }
        };

        manager.RegisterNode(node);
        manager.RequestFocus(node);
        Scheduler.FlushMicrotasks();

        bool updateHandled = manager.HandleTextCompositionUpdate("pre");
        bool commitHandled = manager.HandleTextCompositionCommit("final");

        Assert.True(updateHandled);
        Assert.True(commitHandled);
        Assert.Equal(2, captured.Count);
        Assert.Equal(("pre", false), captured[0]);
        Assert.Equal(("final", true), captured[1]);
    }

    [Fact]
    public void PlumixHost_TextInputMethodClientRequested_ProvidesPreeditBridge()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        var host = new PlumixHost();
        var requestArgs = new TextInputMethodClientRequestedEventArgs
        {
            RoutedEvent = AvaloniaInputElement.TextInputMethodClientRequestedEvent
        };

        host.RaiseEvent(requestArgs);

        Assert.NotNull(requestArgs.Client);
        Assert.True(requestArgs.Client!.SupportsPreedit);

        requestArgs.Client.SetPreeditText("ni");
        Assert.Equal("ni", controller.Text);
        Assert.Equal(new TextRange(0, 2), controller.Composing);
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        requestArgs.Client.SetPreeditText(null);
        Assert.Equal(string.Empty, controller.Text);
        Assert.Null(controller.Composing);
        Assert.Equal(TextSelection.Collapsed(0), controller.Selection);
    }

    [Fact]
    public void PlumixHost_TextInputMethodClient_ReflectsSurroundingTextSelectionAndCursorGeometry()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        Assert.True(FocusManager.Instance.HandleTextInput("abcd"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));

        var host = new PlumixHost();
        var requestArgs = new TextInputMethodClientRequestedEventArgs
        {
            RoutedEvent = AvaloniaInputElement.TextInputMethodClientRequestedEvent
        };

        host.RaiseEvent(requestArgs);
        var client = requestArgs.Client;

        Assert.NotNull(client);
        Assert.True(client!.SupportsSurroundingText);
        Assert.Equal("abcd", client.SurroundingText);
        Assert.Equal(new AvaloniaTextSelection(3, 3), client.Selection);

        var cursorRect = client.CursorRectangle;
        Assert.True(cursorRect.Width > 0);
        Assert.True(cursorRect.Height > 0);

        client.Selection = new AvaloniaTextSelection(1, 3);
        Assert.Equal(1, controller.Selection.Start);
        Assert.Equal(3, controller.Selection.End);
    }

    [Fact]
    public void EditableText_Multiline_EnterAndVerticalCaretNavigation_Work()
    {
        // Vertical caret movement reads the laid-out RenderEditable (Dart's
        // `startVerticalCaretMovement`), so the field is pumped through a real render tree.
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController();
        tester.PumpWidget(new Directionality(
            Plumix.UI.TextDirection.Ltr,
            new DefaultTextEditingShortcuts(new EditableText(
                controller: controller,
                autofocus: true,
                multiline: true))));
        tester.Pump();

        Assert.True(FocusManager.Instance.HandleTextInput("ab"));
        // Enter is left to the platform text input plugin, which inserts the newline into a
        // multiline field and then reports the newline action.
        Assert.False(KeySim.SendKeyCombination(LogicalKeyboardKey.Enter));
        Assert.True(FocusManager.Instance.HandleTextInput("cd"));
        Assert.Equal("ab\ncd", controller.Text);
        Assert.Equal(TextSelection.Collapsed(5), controller.Selection);
        tester.Pump();

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp)));
        // The run lands at the end of the first line, before the hard break: upstream, as Dart's
        // `getPositionForOffset` reports it.
        Assert.Equal(TextSelection.Collapsed(2, TextAffinity.Upstream), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.Equal(TextSelection.Collapsed(5, TextAffinity.Upstream), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp, shift: true)));
        Assert.Equal(2, controller.Selection.Start);
        Assert.Equal(5, controller.Selection.End);
    }

    [Fact]
    public void EditableText_HitTestBetweenTypingAndTheNextLayout_ReadsALaidOutPainter()
    {
        // The controller listener pushes the new text into the RenderEditable before the rebuild
        // (C#-only); a hover hit test that arrives before the next frame used to find the painter
        // invalidated and throw "Text layout not available".
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("hello");
        var focusNode = new FocusNode();
        tester.PumpWidget(new Directionality(
            Plumix.UI.TextDirection.Ltr,
            new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(
                    width: 200,
                    child: new EditableText(controller: controller, focusNode: focusNode)))));
        focusNode.RequestFocus();
        tester.Pump();

        Assert.True(FocusManager.Instance.HandleTextInput("a"));
        var result = new BoxHitTestResult();
        tester.RenderView.HitTest(result, new Avalonia.Point(10, 10));

        Assert.Contains(result.Path, entry => entry.Target is RenderEditable);
    }

    [Fact]
    public void TextEditingController_WordNavigationAndDeletion_Work()
    {
        const string initialText = "alpha beta_gamma delta";
        int betaStart = initialText.IndexOf("beta_gamma", StringComparison.Ordinal);
        int deltaStart = initialText.IndexOf("delta", StringComparison.Ordinal);

        var controller = new TextEditingController(initialText);
        controller.Selection = TextSelection.Collapsed(initialText.Length);

        Assert.True(controller.MoveCaretToPreviousWord());
        Assert.Equal(TextSelection.Collapsed(deltaStart), controller.Selection);

        Assert.True(controller.MoveCaretToPreviousWord());
        Assert.Equal(TextSelection.Collapsed(betaStart), controller.Selection);

        Assert.True(controller.MoveCaretToPreviousWord());
        Assert.Equal(TextSelection.Collapsed(0), controller.Selection);

        Assert.False(controller.MoveCaretToPreviousWord());

        Assert.True(controller.MoveCaretToNextWord());
        Assert.Equal(TextSelection.Collapsed(5), controller.Selection);

        Assert.True(controller.MoveCaretToNextWord());
        Assert.Equal(TextSelection.Collapsed(betaStart), controller.Selection);

        Assert.True(controller.MoveCaretToNextWord(extendSelection: true));
        Assert.Equal(betaStart, controller.Selection.Start);
        Assert.Equal(deltaStart - 1, controller.Selection.End);

        Assert.True(controller.MoveCaretToNextWord(extendSelection: true));
        Assert.Equal(betaStart, controller.Selection.Start);
        Assert.Equal(deltaStart, controller.Selection.End);

        controller.Selection = TextSelection.Collapsed(deltaStart);
        Assert.True(controller.DeleteBackwardByWord());
        Assert.Equal("alpha delta", controller.Text);
        Assert.Equal(TextSelection.Collapsed(6), controller.Selection);

        Assert.True(controller.DeleteForwardByWord());
        Assert.Equal("alpha ", controller.Text);
        Assert.Equal(TextSelection.Collapsed(6), controller.Selection);

        Assert.False(controller.DeleteForwardByWord());
    }

    [Fact]
    public void TextEditingController_ParagraphNavigation_Work()
    {
        const string initialText = "aa\nbbb\ncccc";
        int secondParagraphStart = initialText.IndexOf("bbb", StringComparison.Ordinal);
        int thirdParagraphStart = initialText.IndexOf("cccc", StringComparison.Ordinal);

        var controller = new TextEditingController(initialText);
        controller.Selection = TextSelection.Collapsed(initialText.Length);

        Assert.True(controller.MoveCaretToParagraphStart());
        Assert.Equal(TextSelection.Collapsed(thirdParagraphStart), controller.Selection);

        Assert.True(controller.MoveCaretToParagraphStart());
        Assert.Equal(TextSelection.Collapsed(secondParagraphStart), controller.Selection);

        Assert.True(controller.MoveCaretToParagraphStart());
        Assert.Equal(TextSelection.Collapsed(0), controller.Selection);

        Assert.False(controller.MoveCaretToParagraphStart());

        Assert.True(controller.MoveCaretToParagraphEnd());
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        Assert.True(controller.MoveCaretToParagraphEnd());
        Assert.Equal(TextSelection.Collapsed(6), controller.Selection);

        Assert.True(controller.MoveCaretToParagraphEnd(extendSelection: true));
        Assert.Equal(6, controller.Selection.Start);
        Assert.Equal(initialText.Length, controller.Selection.End);
    }

    [Fact]
    public void TextEditingController_InsertAndSelectionReplacement_Work()
    {
        var controller = new TextEditingController("abcd");
        controller.Selection = TextSelection.Collapsed(2);

        Assert.True(controller.Insert("X"));
        Assert.Equal("abXcd", controller.Text);
        Assert.Equal(TextSelection.Collapsed(3), controller.Selection);

        controller.Selection = new TextSelection(1, 4);
        Assert.True(controller.Insert("!"));
        Assert.Equal("a!d", controller.Text);
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);
    }

    [Fact]
    public void TextEditingController_CompositionLifecycle_UpdateCommitAndClear_Work()
    {
        var controller = new TextEditingController("ab");
        controller.Selection = TextSelection.Collapsed(1);

        Assert.True(controller.SetComposing("ni"));
        Assert.Equal("anib", controller.Text);
        Assert.Equal(new TextRange(1, 3), controller.Composing);
        Assert.Equal(TextSelection.Collapsed(3), controller.Selection);

        Assert.True(controller.SetComposing("N"));
        Assert.Equal("aNb", controller.Text);
        Assert.Equal(new TextRange(1, 2), controller.Composing);
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        Assert.True(controller.ClearComposing());
        Assert.Null(controller.Composing);
        Assert.Equal("aNb", controller.Text);

        Assert.True(controller.SetComposing("x"));
        Assert.True(controller.CommitComposing("!"));
        Assert.Equal("aN!b", controller.Text);
        Assert.Null(controller.Composing);
        Assert.Equal(TextSelection.Collapsed(3), controller.Selection);
    }

    [Fact]
    public void EditableText_TextInputAndBackspace_UpdateController()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true,
                placeholder: "Type here"));

        Assert.Equal(string.Empty, controller.Text);

        bool textHandled = FocusManager.Instance.HandleTextInput("Hi");
        Assert.True(textHandled);
        Assert.Equal("Hi", controller.Text);

        bool backspaceHandled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Backspace));
        Assert.True(backspaceHandled);
        Assert.Equal("H", controller.Text);
    }

    [Fact]
    public void EditableText_CompositionUpdateCommitAndEscape_Work()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        Assert.True(FocusManager.Instance.HandleTextCompositionUpdate("ni"));
        Assert.Equal("ni", controller.Text);
        Assert.Equal(new TextRange(0, 2), controller.Composing);
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        Assert.True(FocusManager.Instance.HandleTextCompositionCommit("N"));
        Assert.Equal("N", controller.Text);
        Assert.Null(controller.Composing);
        Assert.Equal(TextSelection.Collapsed(1), controller.Selection);

        Assert.True(FocusManager.Instance.HandleTextCompositionUpdate("yz"));
        Assert.Equal("Nyz", controller.Text);
        Assert.Equal(new TextRange(1, 3), controller.Composing);

        // Escape is not a text editing shortcut: Dart's `EditableText` leaves the composing region
        // to the platform IME and lets the key bubble up to `DismissIntent`.
        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Escape)));
        Assert.Equal("Nyz", controller.Text);
        Assert.Equal(new TextRange(1, 3), controller.Composing);

        controller.Clear();
        Assert.True(FocusManager.Instance.HandleTextCompositionUpdate("x"));
        Assert.True(FocusManager.Instance.HandleTextInput("X"));
        Assert.Equal("X", controller.Text);
        Assert.Null(controller.Composing);
    }

    [Fact]
    public void EditableText_ArrowAndSelectionKeys_UpdateControllerSelection()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        Assert.True(FocusManager.Instance.HandleTextInput("abcd"));
        Assert.Equal(TextSelection.Collapsed(4), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal(TextSelection.Collapsed(3), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft, shift: true)));
        Assert.Equal(2, controller.Selection.Start);
        Assert.Equal(3, controller.Selection.End);
        Assert.False(controller.Selection.IsCollapsed);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.Equal(0, controller.Selection.Start);
        Assert.Equal(4, controller.Selection.End);

        Assert.True(FocusManager.Instance.HandleTextInput("Z"));
        Assert.Equal("Z", controller.Text);
        Assert.Equal(TextSelection.Collapsed(1), controller.Selection);
    }

    [Fact]
    public void EditableText_ClipboardShortcuts_CopyCutPaste_Work()
    {
        using var clipboard = new MockClipboardPlatform();
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        Assert.True(FocusManager.Instance.HandleTextInput("alpha"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyC, control: true)));
        Assert.True(EventLoopPump.SpinUntil(() => clipboard.Text == "alpha"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyX, control: true)));
        Assert.Equal(string.Empty, controller.Text);
        Assert.Equal("alpha", clipboard.Text);

        clipboard.Text = "beta";
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyV, control: true)));
        Assert.True(EventLoopPump.SpinUntil(() => controller.Text == "beta"));
        Assert.Equal(TextSelection.Collapsed(4), controller.Selection);
    }

    [Fact]
    public void EditableText_PasteWaitsForPlatformClipboardAndUsesItsLatestText()
    {
        var reply = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        SystemChannels.Platform.SetPlatformMethodCallHandler(call => call.Method switch
        {
            "Clipboard.getData" => reply.Task,
            _ => Task.FromResult<object?>(null),
        });
        try
        {
            var controller = new TextEditingController("before");
            using FrameworkDartTester tester = PumpField(new EditableText(controller: controller, autofocus: true));

            Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyV, control: true)));
            Assert.Equal("before", controller.Text);

            reply.SetResult(new Dictionary<string, object?> { ["text"] = " after" });
            Assert.True(EventLoopPump.SpinUntil(() => controller.Text == "before after"));
            Assert.Equal(TextSelection.Collapsed(12), controller.Selection);
        }
        finally
        {
            SystemChannels.Platform.SetPlatformMethodCallHandler(null);
        }
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void EditableText_ClipboardShortcutsRespectEditingPolicies(
        bool readOnly,
        bool obscureText,
        bool enableInteractiveSelection)
    {
        using var clipboard = new MockClipboardPlatform("external");
        var controller = new TextEditingController("alpha", new TextSelection(0, 5));
        using FrameworkDartTester tester = PumpField(new EditableText(
            controller: controller,
            autofocus: true,
            readOnly: readOnly,
            obscureText: obscureText,
            enableInteractiveSelection: enableInteractiveSelection));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyX, control: true)));
        Assert.Equal("alpha", controller.Text);
        Assert.Equal("external", clipboard.Text);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyC, control: true)));
        Assert.Equal(obscureText || !enableInteractiveSelection ? "external" : "alpha", clipboard.Text);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyV, control: true)));
        if (readOnly || !enableInteractiveSelection)
        {
            Assert.Equal("alpha", controller.Text);
        }
        else
        {
            Assert.True(EventLoopPump.SpinUntil(() => controller.Text == "external"));
        }
    }

    [Fact]
    public void EditableText_PasteReplacesReversedSelectionAndCollapsesAfterInsertedText()
    {
        using var clipboard = new MockClipboardPlatform("xy");
        var controller = new TextEditingController("abcdef", new TextSelection(5, 2));
        using FrameworkDartTester tester = PumpField(new EditableText(controller: controller, autofocus: true));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyV, control: true)));
        Assert.True(EventLoopPump.SpinUntil(() => controller.Text == "abxyf"));
        Assert.Equal(TextSelection.Collapsed(4), controller.Selection);
    }

    [Fact]
    public void TextEditingController_GraphemeNavigationAndDeletion_Work()
    {
        const string familyEmoji = "👨‍👩‍👧‍👦";
        const string combining = "e\u0301";
        string text = $"A{familyEmoji}{combining}B";
        int familyStart = 1;
        int combiningStart = familyStart + familyEmoji.Length;
        int endOffset = text.Length;

        var controller = new TextEditingController(text);
        controller.Selection = TextSelection.Collapsed(endOffset);

        Assert.True(controller.MoveCaretLeft());
        Assert.Equal(TextSelection.Collapsed(endOffset - 1), controller.Selection);

        Assert.True(controller.MoveCaretLeft());
        Assert.Equal(TextSelection.Collapsed(combiningStart), controller.Selection);

        Assert.True(controller.MoveCaretLeft());
        Assert.Equal(TextSelection.Collapsed(familyStart), controller.Selection);

        Assert.True(controller.DeleteForward());
        Assert.Equal($"A{combining}B", controller.Text);
        Assert.Equal(TextSelection.Collapsed(1), controller.Selection);

        Assert.True(controller.DeleteForward());
        Assert.Equal("AB", controller.Text);
        Assert.Equal(TextSelection.Collapsed(1), controller.Selection);
    }

    [Fact]
    public void EditableText_DeleteForward_AndBackspaceOnSelection_Work()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true));

        Assert.True(FocusManager.Instance.HandleTextInput("abcd"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Delete)));
        Assert.Equal("abd", controller.Text);
        Assert.Equal(TextSelection.Collapsed(2), controller.Selection);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft, shift: true)));
        Assert.Equal(1, controller.Selection.Start);
        Assert.Equal(2, controller.Selection.End);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Backspace)));
        Assert.Equal("ad", controller.Text);
        Assert.Equal(TextSelection.Collapsed(1), controller.Selection);
    }

    [Fact]
    public void EditableText_Disabled_IgnoresTextInput()
    {
        var controller = new TextEditingController();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                enabled: false,
                autofocus: true));

        bool textHandled = FocusManager.Instance.HandleTextInput("x");
        bool compositionUpdateHandled = FocusManager.Instance.HandleTextCompositionUpdate("y");
        bool compositionCommitHandled = FocusManager.Instance.HandleTextCompositionCommit("z");
        bool keyHandled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Backspace));

        Assert.False(textHandled);
        Assert.False(compositionUpdateHandled);
        Assert.False(compositionCommitHandled);
        Assert.False(keyHandled);
        Assert.Equal(string.Empty, controller.Text);
        Assert.Null(controller.Composing);
    }

    [Fact]
    public void EditableText_OnChanged_IsRaisedOnTextMutation()
    {
        var controller = new TextEditingController();
        var changes = new List<string>();
        using FrameworkDartTester tester = PumpField(
            new EditableText(
                controller: controller,
                autofocus: true,
                onChanged: value => changes.Add(value)));

        Assert.True(FocusManager.Instance.HandleTextInput("a"));
        Assert.True(FocusManager.Instance.HandleTextInput("b"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Backspace)));

        Assert.Equal(new[] { "a", "ab", "b" }, changes);
    }

    /// Pumps <paramref name="field"/> under the text-editing shortcuts `WidgetsApp` provides and lays
    /// it out, so key events reach the field's actions and the actions can read its geometry.
    private static FrameworkDartTester PumpField(Widget field)
    {
        var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(Plumix.UI.TextDirection.Ltr, new DefaultTextEditingShortcuts(field)));
        tester.Pump();
        return tester;
    }

}
