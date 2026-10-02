using System.Collections;
using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// The host's platform text input plugin over an isolated channel: the cases of the engine's
/// <c>shell/platform/windows/text_input_plugin_unittest.cc</c> that apply to it, and the macOS
/// plugin's channel, IME and selector behavior (<c>FlutterTextInputPlugin.mm</c>) that it ports.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class HostTextInputPluginTests : IDisposable
{
    private readonly MethodChannel _channel = new("test/textinput", new JsonMethodCodec());
    private readonly HostTextInputPlugin _plugin;
    private readonly List<MethodCall> _log = [];

    public HostTextInputPluginTests()
    {
        _channel.SetMethodCallHandler(call =>
        {
            _log.Add(call);
            return Task.FromResult<object?>(null);
        });
        _plugin = new HostTextInputPlugin(_channel);
        _plugin.Attach();
    }

    public void Dispose()
    {
        _plugin.Detach();
        _channel.SetMethodCallHandler(null);
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    private static Dictionary<string, object?> Config(
        string inputType = "TextInputType.text",
        string inputAction = "TextInputAction.done",
        bool enableDeltaModel = false,
        int? viewId = 0) => new()
    {
        ["inputType"] = new Dictionary<string, object?> { ["name"] = inputType },
        ["inputAction"] = inputAction,
        ["enableDeltaModel"] = enableDeltaModel,
        ["viewId"] = viewId,
    };

    private void Invoke(string method, object? arguments = null) =>
        _channel.InvokeMethod<object>(method, arguments).GetAwaiter().GetResult();

    private void SetClient(int client, Dictionary<string, object?>? config = null) =>
        Invoke("TextInput.setClient", new List<object?> { client, config ?? Config() });

    private void SetEditingState(
        string text,
        int selectionBase,
        int selectionExtent,
        int composingBase = -1,
        int composingExtent = -1,
        string affinity = "TextAffinity.downstream") =>
        Invoke("TextInput.setEditingState", new Dictionary<string, object?>
        {
            ["text"] = text,
            ["selectionBase"] = selectionBase,
            ["selectionExtent"] = selectionExtent,
            ["selectionAffinity"] = affinity,
            ["selectionIsDirectional"] = false,
            ["composingBase"] = composingBase,
            ["composingExtent"] = composingExtent,
        });

    private static IDictionary State(MethodCall call)
    {
        Assert.Equal("TextInputClient.updateEditingState", call.Method);
        return (IDictionary)((IList)call.Arguments!)[1]!;
    }

    private static void AssertState(
        MethodCall call,
        int client,
        string text,
        int selectionBase,
        int selectionExtent,
        int composingBase = -1,
        int composingExtent = -1)
    {
        IDictionary state = State(call);
        Assert.Equal(client, Convert.ToInt32(((IList)call.Arguments!)[0]));
        Assert.Equal(text, state["text"]);
        Assert.Equal(selectionBase, Convert.ToInt32(state["selectionBase"]));
        Assert.Equal(selectionExtent, Convert.ToInt32(state["selectionExtent"]));
        Assert.Equal(composingBase, Convert.ToInt32(state["composingBase"]));
        Assert.Equal(composingExtent, Convert.ToInt32(state["composingExtent"]));
        Assert.Equal(false, state["selectionIsDirectional"]);
    }

    // ------------------------------------------------------------------- attachment

    [Fact]
    public void AttachRequestsTheExistingInputState()
    {
        Assert.Equal("TextInputClient.requestExistingInputState", Assert.Single(_log).Method);
    }

    [Fact]
    public void AttachmentsAreCountedAndTheLastDetachDropsTheClient()
    {
        _plugin.Attach();
        SetClient(1);
        _plugin.Detach();
        Assert.NotNull(_plugin.ActiveModel);

        _plugin.Detach();
        Assert.Null(_plugin.ActiveModel);
        Assert.Throws<MissingPluginException>(() => SetClient(2));
        _plugin.Attach();
    }

    // ------------------------------------------------------- text_input_plugin_unittest.cc

    [Fact]
    public void TextMethodsWorksWithEmptyModel()
    {
        _log.Clear();
        _plugin.InsertText("a");
        _plugin.SetMarkedText("b", 1);
        _plugin.UnmarkText();
        Assert.False(_plugin.SetSelection(0, 0));
        Assert.False(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        Assert.Empty(_log);
    }

    [Fact]
    public void ClearClientResetsComposing()
    {
        int discarded = 0;
        _plugin.MarkedTextDiscarded += () => discarded++;
        SetClient(123);
        _plugin.SetMarkedText("な", 1);
        Assert.True(_plugin.ActiveModel!.Composing);

        _log.Clear();
        Invoke("TextInput.clearClient");

        Assert.Null(_plugin.ActiveModel);
        Assert.Null(_plugin.ClientId);
        Assert.Equal(1, discarded);
        // The macOS plugin commits silently; the framework is detaching the client anyway.
        Assert.Empty(_log);
    }

    [Fact]
    public void VerifyComposingSendStateUpdate()
    {
        SetClient(123);
        _log.Clear();

        _plugin.SetMarkedText("か", 1);

        AssertState(Assert.Single(_log), client: 123, text: "か", 1, 1, composingBase: 0, composingExtent: 1);
    }

    [Fact]
    public void VerifyInputActionNewlineInsertNewLine()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Windows;
        SetClient(123, Config(inputType: "TextInputType.multiline", inputAction: "TextInputAction.newline"));
        _log.Clear();

        Assert.True(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        Assert.Equal(2, _log.Count);
        AssertState(_log[0], client: 123, text: "\n", 1, 1);
        Assert.Equal("TextInputClient.performAction", _log[1].Method);
        Assert.Equal(
            new object?[] { 123L, "TextInputAction.newline" },
            ((IList)_log[1].Arguments!).Cast<object?>().Select(Normalize));
    }

    [Fact]
    public void VerifyInputActionSendDoesNotInsertNewLine()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Windows;
        SetClient(123, Config(inputType: "TextInputType.multiline", inputAction: "TextInputAction.send"));
        _log.Clear();

        Assert.True(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInputClient.performAction", call.Method);
        Assert.Equal("TextInputAction.send", ((IList)call.Arguments!)[1]);
        Assert.Equal(string.Empty, _plugin.ActiveModel!.GetText());
    }

    [Fact]
    public void ModifiedEnterIsLeftToTheFramework()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        SetClient(123, Config(inputType: "TextInputType.multiline", inputAction: "TextInputAction.newline"));
        _log.Clear();

        Assert.False(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter, control: true));
        Assert.False(_plugin.HandleKeyEvent(LogicalKeyboardKey.ArrowLeft));
        Assert.Empty(_log);
    }

    [Fact]
    public void SetClientWithoutAViewIdTargetsTheImplicitView()
    {
        SetClient(123, Config(viewId: 7));
        Assert.Equal(7, _plugin.ViewId);

        Dictionary<string, object?> config = Config();
        config.Remove("viewId");
        SetClient(124, config);
        Assert.Equal(0, _plugin.ViewId);
    }

    [Fact]
    public void SetClientRequiresArguments()
    {
        PlatformException error = Assert.Throws<PlatformException>(
            () => Invoke("TextInput.setClient", new List<object?> { 1 }));
        Assert.Equal("error", error.Code);
        Assert.Equal("Missing arguments", error.ErrorMessage);
    }

    [Fact]
    public void TextEditingWorksWithDeltaModel()
    {
        SetClient(123, Config(enableDeltaModel: true));
        _log.Clear();

        _plugin.InsertText("abc");

        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInputClient.updateEditingStateWithDeltas", call.Method);
        var arguments = (IList)call.Arguments!;
        Assert.Equal(123, Convert.ToInt32(arguments[0]));
        var delta = (IDictionary)((IList)((IDictionary)arguments[1]!)["deltas"]!)[0]!;
        Assert.Equal(string.Empty, delta["oldText"]);
        Assert.Equal("abc", delta["deltaText"]);
        Assert.Equal(0, Convert.ToInt32(delta["deltaStart"]));
        Assert.Equal(0, Convert.ToInt32(delta["deltaEnd"]));
        Assert.Equal(3, Convert.ToInt32(delta["selectionBase"]));
        Assert.Equal(3, Convert.ToInt32(delta["selectionExtent"]));
        Assert.Equal(-1, Convert.ToInt32(delta["composingBase"]));
        Assert.Equal(-1, Convert.ToInt32(delta["composingExtent"]));
        Assert.Equal("TextAffinity.upstream", delta["selectionAffinity"]);
    }

    [Fact]
    public void DeltaModelReportsMarkedTextAndUnmarkAsDeltas()
    {
        SetClient(123, Config(enableDeltaModel: true));
        SetEditingState("ab", 2, 2);
        _log.Clear();

        _plugin.SetMarkedText("c", 1);
        _plugin.UnmarkText();

        Assert.Equal(2, _log.Count);
        var marked = (IDictionary)((IList)((IDictionary)((IList)_log[0].Arguments!)[1]!)["deltas"]!)[0]!;
        Assert.Equal("ab", marked["oldText"]);
        Assert.Equal("c", marked["deltaText"]);
        Assert.Equal(2, Convert.ToInt32(marked["deltaStart"]));
        Assert.Equal(2, Convert.ToInt32(marked["deltaEnd"]));
        Assert.Equal(2, Convert.ToInt32(marked["composingBase"]));
        Assert.Equal(3, Convert.ToInt32(marked["composingExtent"]));

        var unmarked = (IDictionary)((IList)((IDictionary)((IList)_log[1].Arguments!)[1]!)["deltas"]!)[0]!;
        Assert.Equal("abc", unmarked["oldText"]);
        Assert.Equal(string.Empty, unmarked["deltaText"]);
        Assert.Equal(-1, Convert.ToInt32(unmarked["deltaStart"]));
        Assert.Equal(-1, Convert.ToInt32(unmarked["deltaEnd"]));
        Assert.Equal(-1, Convert.ToInt32(unmarked["composingBase"]));
    }

    [Fact]
    public void CompositionCursorPos()
    {
        SetClient(123);
        _log.Clear();

        _plugin.SetMarkedText("abc", 1);
        _plugin.SetMarkedText("abcd", 2);

        Assert.Equal(2, _log.Count);
        AssertState(_log[0], client: 123, text: "abc", 1, 1, composingBase: 0, composingExtent: 3);
        AssertState(_log[1], client: 123, text: "abcd", 2, 2, composingBase: 0, composingExtent: 4);
    }

    [Fact]
    public void TransformCursorRect()
    {
        SetClient(123);
        Assert.Null(_plugin.GetCaretRect());
        int changes = 0;
        _plugin.CaretRectChanged += () => changes++;

        // A scale of 2 and a translation of (10, 20), column-major as Matrix4.storage.
        Invoke("TextInput.setEditableSizeAndTransform", new Dictionary<string, object?>
        {
            ["width"] = 100.0,
            ["height"] = 20.0,
            ["transform"] = new List<object?>
            {
                2.0, 0.0, 0.0, 0.0,
                0.0, 2.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                10.0, 20.0, 0.0, 1.0,
            },
        });
        Invoke("TextInput.setMarkedTextRect", RectJson(new Rect(3, 4, 5, 6)));

        Assert.Equal(new Rect(16, 28, 10, 12), _plugin.GetCaretRect());

        // The caret rect wins over the marked text rect, as the macOS plugin positions only the caret.
        Invoke("TextInput.setCaretRect", RectJson(new Rect(1, 1, 2, 18)));
        Assert.Equal(new Rect(12, 22, 4, 36), _plugin.GetCaretRect());
        Assert.Equal(3, changes);
    }

    [Fact]
    public void PerspectiveCursorRectWithAZeroWIsEmpty()
    {
        SetClient(123);
        Invoke("TextInput.setEditableSizeAndTransform", new Dictionary<string, object?>
        {
            ["width"] = 100.0,
            ["height"] = 20.0,
            ["transform"] = new List<object?>
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, 0.0,
            },
        });
        Invoke("TextInput.setCaretRect", RectJson(new Rect(1, 1, 2, 18)));

        Assert.Equal(default(Rect), _plugin.GetCaretRect());
    }

    [Fact]
    public void SetAndUseMultipleClients()
    {
        SetClient(123);
        Invoke("TextInput.clearClient");
        SetClient(456);
        _log.Clear();

        _plugin.InsertText("x");

        AssertState(Assert.Single(_log), client: 456, text: "x", 1, 1);
    }

    // ------------------------------------------------------------ the macOS plugin

    [Fact]
    public void SetEditingStateSyncsTheModelAndAffinity()
    {
        int changes = 0;
        _plugin.EditingStateChanged += () => changes++;
        SetClient(123);
        Assert.Equal(TextAffinity.Upstream, _plugin.TextAffinity);

        SetEditingState("hello", 1, 4, affinity: "TextAffinity.downstream");

        TextInputModel model = _plugin.ActiveModel!;
        Assert.Equal("hello", model.GetText());
        Assert.Equal(new TextInputModel.Range(1, 4), model.Selection);
        Assert.False(model.Composing);
        Assert.Equal(TextAffinity.Downstream, _plugin.TextAffinity);
        Assert.Equal(1, changes);

        // Flutter's -1/-1 "no selection" becomes 0/0.
        SetEditingState("hey", -1, -1);
        Assert.Equal(new TextInputModel.Range(0, 0), model.Selection);
    }

    [Fact]
    public void SetEditingStateRestoresComposingAndDiscardsMarkedTextWhenItCollapses()
    {
        int discarded = 0;
        _plugin.MarkedTextDiscarded += () => discarded++;
        SetClient(123);

        SetEditingState("abcd", 3, 3, composingBase: 1, composingExtent: 3);
        TextInputModel model = _plugin.ActiveModel!;
        Assert.True(model.Composing);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal(0, discarded);

        SetEditingState("abcd", 4, 4);
        Assert.False(model.Composing);
        Assert.Equal(1, discarded);
    }

    [Fact]
    public void SetEditingStateWithoutAClientIsAnError()
    {
        PlatformException error = Assert.Throws<PlatformException>(() => SetEditingState("a", 0, 0));
        Assert.Equal("Internal Consistency Error", error.Code);
    }

    [Fact]
    public void ShowAndHideTrackVisibility()
    {
        int shown = 0;
        int hidden = 0;
        _plugin.ShowRequested += () => shown++;
        _plugin.HideRequested += () => hidden++;
        SetClient(123);

        Invoke("TextInput.show");
        Assert.True(_plugin.IsShown);
        Invoke("TextInput.hide");
        Assert.False(_plugin.IsShown);
        Assert.Equal(1, shown);
        Assert.Equal(1, hidden);
    }

    [Fact]
    public void UpdateConfigReconfiguresTheAttachedClient()
    {
        int changes = 0;
        _plugin.ClientChanged += () => changes++;
        SetClient(123);
        Invoke("TextInput.updateConfig", Config(inputType: "TextInputType.number", inputAction: "TextInputAction.go"));

        Assert.Equal("TextInputType.number", _plugin.InputType);
        Assert.Equal("TextInputAction.go", _plugin.InputAction);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void UnhandledMethodsAreNotImplemented()
    {
        SetClient(123);
        Assert.Throws<MissingPluginException>(() => Invoke("TextInput.requestAutofill"));
    }

    [Fact]
    public void InsertTextReplacesTheSelection()
    {
        SetClient(123);
        SetEditingState("hello", 1, 4);
        _log.Clear();

        _plugin.InsertText("EY");

        AssertState(Assert.Single(_log), client: 123, text: "hEYo", 3, 3);
    }

    [Fact]
    public void InsertTextCommitsTheMarkedText()
    {
        SetClient(123);
        SetEditingState("ab", 2, 2);
        _plugin.SetMarkedText("ni", 2);
        _log.Clear();

        _plugin.InsertText("に");

        AssertState(Assert.Single(_log), client: 123, text: "abに", 3, 3);
        Assert.False(_plugin.ActiveModel!.Composing);
    }

    [Fact]
    public void InsertTextFromAJapaneseConversionReplacesTheWholeComposingRange()
    {
        SetClient(123);
        // The IME moved the cursor into the middle of its marked text.
        _plugin.SetMarkedText("にほん", 1);
        _log.Clear();

        _plugin.InsertText("日本");

        AssertState(Assert.Single(_log), client: 123, text: "日本", 2, 2);
    }

    [Fact]
    public void InsertTextWithAReplacementRange()
    {
        SetClient(123);
        SetEditingState("hello", 5, 5);
        _log.Clear();

        // AppKit can send a reversed range (a negative length).
        _plugin.InsertText("J", (Location: 1, Length: -1));

        AssertState(Assert.Single(_log), client: 123, text: "Jello", 1, 1);
    }

    [Fact]
    public void ClearingTheMarkedTextRemovesItAndEndsComposing()
    {
        SetClient(123);
        SetEditingState("ab", 2, 2);
        _plugin.SetMarkedText("xy", 2);
        _log.Clear();

        _plugin.SetMarkedText(string.Empty, 0);
        _plugin.UnmarkText();

        Assert.Equal(2, _log.Count);
        AssertState(_log[1], client: 123, text: "ab", 2, 2);
    }

    [Fact]
    public void SetSelectionReportsTheNewSelection()
    {
        SetClient(123);
        SetEditingState("hello", 0, 0);
        _log.Clear();

        Assert.True(_plugin.SetSelection(1, 3));
        Assert.False(_plugin.SetSelection(1, 9));

        AssertState(Assert.Single(_log), client: 123, text: "hello", 1, 3);
    }

    [Fact]
    public void MacOSKeysBecomeOnePerformSelectorsCall()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        SetClient(123);
        Invoke("TextInput.show");
        _log.Clear();

        Assert.True(_plugin.HandleKeyEvent(LogicalKeyboardKey.ArrowUp, alt: true));

        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInputClient.performSelectors", call.Method);
        var arguments = (IList)call.Arguments!;
        Assert.Equal(123, Convert.ToInt32(arguments[0]));
        Assert.Equal(
            ["moveBackward:", "moveToBeginningOfParagraph:"],
            ((IList)arguments[1]!).Cast<object?>().Select(selector => (string)selector!));
    }

    [Fact]
    public void MacOSKeysNeedTheInputShownAndABinding()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        SetClient(123);
        _log.Clear();

        Assert.False(_plugin.HandleKeyEvent(LogicalKeyboardKey.ArrowLeft));
        Invoke("TextInput.show");
        _log.Clear();
        Assert.False(_plugin.HandleKeyEvent(LogicalKeyboardKey.KeyQ));
        Assert.Empty(_log);
    }

    [Fact]
    public void MacOSInsertNewlineInsertsIntoAMultilineClientAndReportsTheActionWithoutForwarding()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        SetClient(123, Config(inputType: "TextInputType.multiline", inputAction: "TextInputAction.newline"));
        Invoke("TextInput.show");
        SetEditingState("ab", 1, 1);
        _plugin.SetMarkedText("x", 1);
        _log.Clear();

        Assert.True(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        Assert.Equal(2, _log.Count);
        AssertState(_log[0], client: 123, text: "ax\nb", 3, 3);
        Assert.Equal("TextInputClient.performAction", _log[1].Method);
        Assert.DoesNotContain(_log, call => call.Method == "TextInputClient.performSelectors");
    }

    [Fact]
    public void MacOSInsertNewlineOnASingleLineClientOnlyReportsTheAction()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        SetClient(123, Config(inputAction: "TextInputAction.search"));
        Invoke("TextInput.show");
        _log.Clear();

        Assert.True(_plugin.HandleKeyEvent(LogicalKeyboardKey.Enter));

        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInputClient.performAction", call.Method);
        Assert.Equal("TextInputAction.search", ((IList)call.Arguments!)[1]);
    }

    private static Dictionary<string, object?> RectJson(Rect rect) => new()
    {
        ["x"] = rect.X,
        ["y"] = rect.Y,
        ["width"] = rect.Width,
        ["height"] = rect.Height,
    };

    private static object? Normalize(object? value) => value switch
    {
        int number => (long)number,
        _ => value,
    };
}
