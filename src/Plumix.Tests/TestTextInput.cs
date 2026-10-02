using System.Collections;
using Avalonia;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>
/// flutter_test's <c>TestTextInput</c> (<c>flutter_test/lib/src/test_text_input.dart</c>): a mock of the
/// platform side of <see cref="SystemChannels.TextInput"/> that records the framework's calls and
/// sends <c>TextInputClient.*</c> calls back the way an engine would.
/// </summary>
/// <remarks>
/// flutter_test's binding registers one for every widget test; a Plumix test creates and disposes its
/// own (<see cref="Register"/> runs in the constructor).
/// </remarks>
internal sealed class TestTextInput : IDisposable
{
    private int? _client;
    private bool _isVisible;
    private MacOSTestTextInputKeyHandler? _keyHandler;

    public TestTextInput(Action? onCleared = null)
    {
        OnCleared = onCleared;
        Plumix.UI.TextInput.EnsureInitialized();
        Register();
    }

    /// <summary>Called when the framework calls <c>TextInput.clearClient</c>.</summary>
    public Action? OnCleared { get; }

    /// <summary>The messages received by the platform side of the channel.</summary>
    public List<MethodCall> Log { get; } = [];

    /// <summary>Whether this instance is the registered platform side of the channel.</summary>
    public bool IsRegistered { get; private set; }

    /// <summary>Whether there are any active clients listening to text input.</summary>
    public bool HasAnyClients => _client is > 0;

    /// <summary>The last client configuration passed to <c>TextInput.setClient</c> or
    /// <c>TextInput.updateConfig</c>.</summary>
    public IDictionary? SetClientArgs { get; private set; }

    /// <summary>The last editing state passed to <c>TextInput.setEditingState</c>.</summary>
    public IDictionary? EditingState { get; private set; }

    /// <summary>Whether the onscreen keyboard is visible to the user.</summary>
    public bool IsVisible
    {
        get
        {
            System.Diagnostics.Debug.Assert(IsRegistered);
            return _isVisible;
        }
    }

    /// <summary>Installs this object as the platform side of the text input channel.</summary>
    public void Register()
    {
        SystemChannels.TextInput.SetPlatformMethodCallHandler(HandleTextInputCall);
        IsRegistered = true;
    }

    /// <summary>Removes this object as the platform side of the text input channel.</summary>
    public void Unregister()
    {
        SystemChannels.TextInput.SetPlatformMethodCallHandler(null);
        IsRegistered = false;
    }

    public void Dispose()
    {
        Unregister();
    }

    /// <summary>Resets any internal state of this object and calls <see cref="Log"/>.Clear().</summary>
    public void Reset()
    {
        Log.Clear();
        _client = null;
        SetClientArgs = null;
        EditingState = null;
        _isVisible = false;
        _keyHandler = null;
    }

    private Task<object?> HandleTextInputCall(MethodCall methodCall)
    {
        Log.Add(methodCall);
        switch (methodCall.Method)
        {
            case "TextInput.setClient":
                var arguments = (IList)methodCall.Arguments!;
                _client = Convert.ToInt32(arguments[0]);
                SetClientArgs = (IDictionary)arguments[1]!;
                break;
            case "TextInput.updateConfig":
                SetClientArgs = (IDictionary)methodCall.Arguments!;
                break;
            case "TextInput.clearClient":
                _client = null;
                _isVisible = false;
                _keyHandler = null;
                OnCleared?.Invoke();
                break;
            case "TextInput.setEditingState":
                EditingState = (IDictionary)methodCall.Arguments!;
                break;
            case "TextInput.show":
                _isVisible = true;
                if (!PlatformDefaults.IsWeb && PlatformDefaults.TargetPlatform == TargetPlatform.MacOS)
                {
                    _keyHandler ??= new MacOSTestTextInputKeyHandler(_client ?? -1);
                }

                break;
            case "TextInput.hide":
                _isVisible = false;
                _keyHandler = null;
                break;
        }

        return Task.FromResult<object?>(null);
    }

    /// <summary>Simulates the user hiding the onscreen keyboard.</summary>
    public void Hide()
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        _isVisible = false;
    }

    /// <summary>Simulates the user changing the text of the focused text field, and resetting the
    /// selection to the end of the text.</summary>
    public void EnterText(string text)
    {
        UpdateEditingValue(new TextEditingValue(text, TextSelection.Collapsed(text.Length)));
    }

    /// <summary>Simulates the user changing the <see cref="TextEditingValue"/> to the given value.</summary>
    public void UpdateEditingValue(TextEditingValue value)
    {
        Send("TextInputClient.updateEditingState", new List<object?> { _client ?? -1, value.ToJson() });
    }

    /// <summary>Simulates the user pressing one of the <see cref="TextInputActionType"/> buttons.</summary>
    public void ReceiveAction(TextInputActionType action)
    {
        Exception? failure = null;
        Send(
            "TextInputClient.performAction",
            new List<object?> { _client ?? -1, action.ToDartName() },
            data =>
            {
                System.Diagnostics.Debug.Assert(data is not null);
                try
                {
                    SystemChannels.TextInput.Codec.DecodeEnvelope(data!);
                }
                catch (Exception error)
                {
                    failure = error;
                }
            });
        if (failure is not null)
        {
            throw failure;
        }
    }

    /// <summary>Simulates the user closing the text input connection.</summary>
    public void CloseConnection()
    {
        Send("TextInputClient.onConnectionClosed", new List<object?> { _client ?? -1 });
    }

    /// <summary>Simulates a scribble interaction starting.</summary>
    public void StartScribbleInteraction()
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.scribbleInteractionBegan", new List<object?> { _client ?? -1 });
    }

    /// <summary>Simulates a scribble interaction finishing.</summary>
    public void FinishScribbleInteraction()
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.scribbleInteractionFinished", new List<object?> { _client ?? -1 });
    }

    /// <summary>Simulates a scribble interaction focusing an element.</summary>
    public void ScribbleFocusElement(string elementIdentifier, Point offset)
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.focusElement", new List<object?> { elementIdentifier, offset.X, offset.Y });
    }

    /// <summary>Simulates iOS asking for the list of scribble elements during UIIndirectScribbleInteraction.
    /// </summary>
    public List<IList> ScribbleRequestElementsInRect(Rect rect)
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        List<IList> response = [];
        Send(
            "TextInputClient.requestElementsInRect",
            new List<object?> { rect.Left, rect.Top, rect.Width, rect.Height },
            data =>
            {
                foreach (object? element in (IList)SystemChannels.TextInput.Codec.DecodeEnvelope(data!)!)
                {
                    response.Add((IList)element!);
                }
            });
        return response;
    }

    /// <summary>Simulates iOS inserting a UITextPlaceholder during a long press with the pencil.</summary>
    public void ScribbleInsertPlaceholder()
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.insertTextPlaceholder", new List<object?> { _client ?? -1, 0.0, 0.0 });
    }

    /// <summary>Simulates iOS removing a UITextPlaceholder after a long press with the pencil is
    /// released.</summary>
    public void ScribbleRemovePlaceholder()
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.removeTextPlaceholder", new List<object?> { _client ?? -1 });
    }

    /// <summary>Gives text input chance to respond to an unhandled key down event.</summary>
    public void HandleKeyDownEvent(LogicalKeyboardKey key)
    {
        _keyHandler?.HandleKeyDownEvent(key);
    }

    /// <summary>Gives text input chance to respond to an unhandled key up event.</summary>
    public void HandleKeyUpEvent(LogicalKeyboardKey key)
    {
        _keyHandler?.HandleKeyUpEvent(key);
    }

    /// <summary>Simulates iOS responding to an undo or redo gesture or button.</summary>
    public void HandleKeyboardUndo(string direction)
    {
        System.Diagnostics.Debug.Assert(IsRegistered);
        Send("TextInputClient.handleUndo", new List<object?> { direction });
    }

    internal static void Send(string method, object? arguments, PlatformMessageResponseCallback? callback = null)
    {
        Task task = SystemChannels.TextInput.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.TextInput.Name,
            SystemChannels.TextInput.Codec.EncodeMethodCall(new MethodCall(method, arguments)),
            callback ?? (_ => { }));
        task.GetAwaiter().GetResult();
    }
}

/// <summary>
/// flutter_test's <c>MacOSTestTextInputKeyHandler</c>
/// (<c>flutter_test/lib/src/test_text_input_key_handler.dart</c>): sends the AppKit selectors of the
/// pressed key to the framework as <c>TextInputClient.performSelectors</c>, tracking the modifiers from
/// the key events it sees.
/// </summary>
internal sealed class MacOSTestTextInputKeyHandler
{
    private readonly int _client;
    private bool _shift;
    private bool _alt;
    private bool _meta;
    private bool _control;

    public MacOSTestTextInputKeyHandler(int client)
    {
        _client = client;
    }

    public void HandleKeyDownEvent(LogicalKeyboardKey key)
    {
        if (IsAny(key, LogicalKeyboardKey.Shift, LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.ShiftRight))
        {
            _shift = true;
        }
        else if (IsAny(key, LogicalKeyboardKey.Alt, LogicalKeyboardKey.AltLeft, LogicalKeyboardKey.AltRight))
        {
            _alt = true;
        }
        else if (IsAny(key, LogicalKeyboardKey.Meta, LogicalKeyboardKey.MetaLeft, LogicalKeyboardKey.MetaRight))
        {
            _meta = true;
        }
        else if (IsAny(
                     key,
                     LogicalKeyboardKey.Control,
                     LogicalKeyboardKey.ControlLeft,
                     LogicalKeyboardKey.ControlRight))
        {
            _control = true;
        }
        else if (MacOSStandardKeyBindings.SelectorsFor(key, _control, _shift, _alt, _meta) is { } selectors)
        {
            SendSelectors(_client, selectors);
        }
    }

    public void HandleKeyUpEvent(LogicalKeyboardKey key)
    {
        if (IsAny(key, LogicalKeyboardKey.Shift, LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.ShiftRight))
        {
            _shift = false;
        }
        else if (IsAny(key, LogicalKeyboardKey.Alt, LogicalKeyboardKey.AltLeft, LogicalKeyboardKey.AltRight))
        {
            _alt = false;
        }
        else if (IsAny(key, LogicalKeyboardKey.Meta, LogicalKeyboardKey.MetaLeft, LogicalKeyboardKey.MetaRight))
        {
            _meta = false;
        }
        else if (IsAny(
                     key,
                     LogicalKeyboardKey.Control,
                     LogicalKeyboardKey.ControlLeft,
                     LogicalKeyboardKey.ControlRight))
        {
            _control = false;
        }
    }

    /// <summary>The handler's <c>_sendSelectors</c>.</summary>
    internal static void SendSelectors(int client, IReadOnlyList<string> selectors)
    {
        TestTextInput.Send(
            "TextInputClient.performSelectors",
            new List<object?> { client, selectors.Cast<object?>().ToList() });
    }

    private static bool IsAny(LogicalKeyboardKey key, params LogicalKeyboardKey[] candidates) =>
        candidates.Any(candidate => candidate.Equals(key));
}

/// <summary>
/// Drives the real host text input plugin (<see cref="HostTextInputPlugin"/>) the way a
/// <see cref="PlumixHost"/> does, so a test types through the same <c>flutter/textinput</c> round trip
/// the desktop host uses.
/// </summary>
/// <remarks>
/// Each call attaches a fresh plugin, which asks the framework for the current client with
/// <c>TextInputClient.requestExistingInputState</c>, applies the IME edit, and detaches again. A
/// method returns false when the framework had no client attached, so nothing could be typed.
/// </remarks>
internal static class HostTextInput
{
    /// <summary>Commits <paramref name="text"/> (Avalonia's <c>TextInput</c> event).</summary>
    public static bool InsertText(string text) => WithPlugin(plugin => plugin.InsertText(text));

    /// <summary>Sets the IME's marked text with the cursor at its end (a preedit update).</summary>
    public static bool SetMarkedText(string text) =>
        WithPlugin(plugin => plugin.SetMarkedText(text, text.Length));

    /// <summary>Clears the IME's marked text and ends composing (an empty preedit).</summary>
    public static bool ClearMarkedText() => WithPlugin(plugin =>
    {
        plugin.SetMarkedText(string.Empty, 0);
        plugin.UnmarkText();
    });

    /// <summary>Commits the marked text as it is.</summary>
    public static bool UnmarkText() => WithPlugin(plugin => plugin.UnmarkText());

    /// <summary>Moves the selection on behalf of the IME.</summary>
    public static bool SetSelection(int baseOffset, int extentOffset)
    {
        bool moved = false;
        bool attached = WithPlugin(plugin => moved = plugin.SetSelection(baseOffset, extentOffset));
        return attached && moved;
    }

    /// <summary>Hands a key the framework left unhandled to the plugin; returns whether it consumed
    /// it.</summary>
    public static bool HandleKeyEvent(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false)
    {
        bool handled = false;
        WithPlugin(plugin => handled = plugin.HandleKeyEvent(key, control, shift, alt, meta));
        return handled;
    }

    private static bool WithPlugin(Action<HostTextInputPlugin> action)
    {
        var plugin = new HostTextInputPlugin();
        plugin.Attach();
        try
        {
            if (plugin.ActiveModel is null)
            {
                return false;
            }

            action(plugin);
            return true;
        }
        finally
        {
            plugin.Detach();
        }
    }
}
