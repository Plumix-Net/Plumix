using System.Collections;
using Avalonia;
using Plumix.Widgets;

// C#-only infrastructure; no Dart parity source. The platform side of `flutter/textinput`, which in
// Flutter is the engine's text input plugin rather than Dart. Plumix hosts are the engine, so this
// class ports that plugin onto the host adapter:
// - the channel protocol, the editing model and the IME entry points follow the macOS embedder's
//   `FlutterTextInputPlugin.mm` (`handleMethodCall:`, `setEditingState:`, `insertText:`,
//   `setMarkedText:`, `unmarkText`, `doCommandBySelector:`, `insertNewline:`), whose
//   `NSTextInputClient` shape is the one Avalonia's `TextInputMethodClient` mirrors (preedit text,
//   committed text, surrounding text);
// - the editing state lives in a `TextInputModel`, the engine's shared
//   `shell/platform/common/text_input_model.cc`;
// - on platforms other than macOS, Enter follows the Windows embedder's `TextInputPlugin::EnterPressed`,
//   and the composing-rect method `TextInput.setMarkedTextRect` is kept as the Windows plugin keeps it.
// AppKit's `interpretKeyEvents:` is not available to an Avalonia host, so `MacOSStandardKeyBindings`
// stands in for it with the table flutter_test's `MacOSTestTextInputKeyHandler` uses.

namespace Plumix.UI;

/// <summary>
/// The host's platform text input plugin: answers the <c>TextInput.*</c> methods the framework sends
/// on <see cref="SystemChannels.TextInput"/>, keeps the attached client's editing state, and reports
/// IME and keyboard edits back as <c>TextInputClient.*</c> calls, the way Flutter's engine does.
/// </summary>
/// <remarks>
/// A host adapter forwards its IME events to <see cref="InsertText"/>, <see cref="SetMarkedText"/>,
/// <see cref="UnmarkText"/> and <see cref="SetSelection"/>, and the keys the framework leaves unhandled
/// to <see cref="HandleKeyEvent"/>; it reads <see cref="ActiveModel"/> and
/// <see cref="GetCaretRect"/> to answer the platform IME.
/// </remarks>
public sealed class HostTextInputPlugin
{
    private const string MultilineInputType = "TextInputType.multiline";
    private const string InputActionNewline = "TextInputAction.newline";
    private const string TextAffinityUpstream = "TextAffinity.upstream";
    private const string TextAffinityDownstream = "TextAffinity.downstream";
    private const string InsertNewlineSelector = "insertNewline:";
    private const string InsertTabSelector = "insertTab:";

    private readonly MethodChannel _channel;
    private int _attachCount;
    private TextInputModel? _activeModel;
    private int? _clientId;
    private string? _inputAction;
    private string? _inputType;
    private bool _enableDeltaModel;
    private TextAffinity _textAffinity = TextAffinity.Upstream;
    private bool _shown;
    private Matrix4 _editableTransform = Matrix4.Identity();
    private Rect? _caretRect;
    private Rect? _markedTextRect;

    /// <summary>Creates a plugin for <paramref name="channel"/>, by default
    /// <see cref="SystemChannels.TextInput"/>.</summary>
    public HostTextInputPlugin(MethodChannel? channel = null)
    {
        _channel = channel ?? SystemChannels.TextInput;
    }

    /// <summary>The plugin every <see cref="PlumixHost"/> shares, as one engine owns one plugin for
    /// all of its views.</summary>
    public static HostTextInputPlugin Instance { get; } = new();

    /// <summary>Raised after <c>TextInput.setClient</c>, <c>TextInput.updateConfig</c> and
    /// <c>TextInput.clearClient</c>.</summary>
    public event Action? ClientChanged;

    /// <summary>Raised whenever the editing state changes, from either side: the macOS plugin's
    /// <c>updateTextAndSelection</c>.</summary>
    public event Action? EditingStateChanged;

    /// <summary>Raised when the editable transform, the caret rect or the marked text rect
    /// changes.</summary>
    public event Action? CaretRectChanged;

    /// <summary>Raised for <c>TextInput.show</c>.</summary>
    public event Action? ShowRequested;

    /// <summary>Raised for <c>TextInput.hide</c>.</summary>
    public event Action? HideRequested;

    /// <summary>Raised when the platform IME must drop its marked (composing) text: AppKit's
    /// <c>discardMarkedText</c>, Windows' <c>OnResetImeComposing</c>.</summary>
    public event Action? MarkedTextDiscarded;

    /// <summary>The attached client's editing model, or null when no client is attached.</summary>
    public TextInputModel? ActiveModel => _activeModel;

    /// <summary>The attached client's id, or null when no client is attached.</summary>
    public int? ClientId => _clientId;

    /// <summary>The view the attached client edits in. A configuration without a <c>viewId</c> targets
    /// view 0, as the macOS plugin falls back to the implicit view.</summary>
    public int ViewId { get; private set; }

    /// <summary>The attached client's configuration, as the framework encoded it.</summary>
    public IDictionary? Configuration { get; private set; }

    /// <summary>The name of the attached client's <c>inputType</c>.</summary>
    public string? InputType => _inputType;

    /// <summary>The attached client's <c>inputAction</c>.</summary>
    public string? InputAction => _inputAction;

    /// <summary>Whether the framework asked to show the input (the macOS plugin's <c>_shown</c>).</summary>
    public bool IsShown => _shown;

    /// <summary>The selection affinity the plugin reports with every editing state.</summary>
    public TextAffinity TextAffinity => _textAffinity;

    /// <summary>
    /// Registers the plugin as the platform side of its channel. Attachments are counted, so every
    /// host that shares the plugin attaches and detaches it.
    /// </summary>
    /// <remarks>
    /// Every attachment (re)registers the handler on the channel's current messenger. Without an
    /// attached client the plugin then asks the framework for the current connection with
    /// <c>TextInputClient.requestExistingInputState</c>, the way Android's embedding does when it
    /// re-attaches to a running engine, so a client attached before the plugin is not lost.
    /// </remarks>
    public void Attach()
    {
        _attachCount++;
        _channel.SetPlatformMethodCallHandler(HandleMethodCall);
        if (_activeModel is null)
        {
            InvokeMethod("TextInputClient.requestExistingInputState", null);
        }
    }

    /// <summary>Releases one <see cref="Attach"/>; the last one unregisters the plugin and drops the
    /// attached client.</summary>
    public void Detach()
    {
        if (_attachCount == 0 || --_attachCount > 0)
        {
            return;
        }

        _channel.SetPlatformMethodCallHandler(null);
        ClearClient();
    }

    /// <summary>The macOS plugin's <c>handleMethodCall:result:</c>.</summary>
    private Task<object?> HandleMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "TextInput.setClient":
                SetClient(call.Arguments as IList);
                break;
            case "TextInput.updateConfig":
                // The desktop plugins ignore this method; Android's applies the new configuration to
                // the attached client, which every Plumix host needs for soft keyboards.
                if (_activeModel is not null && call.Arguments is IDictionary configuration)
                {
                    ApplyConfiguration(configuration);
                    ClientChanged?.Invoke();
                }

                break;
            case "TextInput.show":
                _shown = true;
                ShowRequested?.Invoke();
                break;
            case "TextInput.hide":
                _shown = false;
                HideRequested?.Invoke();
                break;
            case "TextInput.clearClient":
                ClearClient();
                break;
            case "TextInput.setEditingState":
                if (_activeModel is null)
                {
                    // The macOS plugin asserts a view controller; the Windows plugin reports this.
                    throw new PlatformException(
                        "Internal Consistency Error",
                        "Set editing state has been invoked, but no client is set.");
                }

                SetEditingState((IDictionary)call.Arguments!);
                break;
            case "TextInput.setEditableSizeAndTransform":
                SetEditableTransform((IList)((IDictionary)call.Arguments!)["transform"]!);
                break;
            case "TextInput.setCaretRect":
                _caretRect = RectFromJson((IDictionary)call.Arguments!);
                CaretRectChanged?.Invoke();
                break;
            case "TextInput.setMarkedTextRect":
                _markedTextRect = RectFromJson((IDictionary)call.Arguments!);
                CaretRectChanged?.Invoke();
                break;
            default:
                throw new MissingPluginException();
        }

        return Task.FromResult<object?>(null);
    }

    private void SetClient(IList? arguments)
    {
        if (arguments is not { Count: >= 2 } || arguments[0] is null || arguments[1] is null)
        {
            throw new PlatformException(
                "error",
                "Missing arguments",
                "Missing arguments while trying to set a text input client");
        }

        _clientId = Convert.ToInt32(arguments[0]);
        ApplyConfiguration((IDictionary)arguments[1]!);
        _textAffinity = TextAffinity.Upstream;
        _activeModel = new TextInputModel();
        ClientChanged?.Invoke();
    }

    private void ApplyConfiguration(IDictionary configuration)
    {
        Configuration = configuration;
        _inputAction = configuration["inputAction"] as string;
        _enableDeltaModel = configuration["enableDeltaModel"] is true;
        _inputType = (configuration["inputType"] as IDictionary)?["name"] as string;
        ViewId = configuration["viewId"] is { } viewId ? Convert.ToInt32(viewId) : 0;
    }

    /// <summary>The <c>TextInput.clearClient</c> case: an active mark region is committed, composing
    /// ends, and the IME drops its marked text.</summary>
    private void ClearClient()
    {
        if (_activeModel is { Composing: true })
        {
            _activeModel.CommitComposing();
            _activeModel.EndComposing();
        }

        bool hadClient = _activeModel is not null;
        MarkedTextDiscarded?.Invoke();
        _clientId = null;
        _inputAction = null;
        _enableDeltaModel = false;
        _inputType = null;
        _activeModel = null;
        Configuration = null;
        if (hadClient)
        {
            ClientChanged?.Invoke();
        }
    }

    /// <summary>The macOS plugin's <c>setEditingState:</c>.</summary>
    private void SetEditingState(IDictionary state)
    {
        TextInputModel model = _activeModel!;
        if (state["selectionAffinity"] is string selectionAffinity)
        {
            _textAffinity = selectionAffinity == TextAffinityUpstream
                ? TextAffinity.Upstream
                : TextAffinity.Downstream;
        }

        string text = state["text"] as string ?? string.Empty;
        TextInputModel.Range selectedRange = RangeFromBaseExtent(
            state["selectionBase"],
            state["selectionExtent"],
            model.Selection);
        model.SetSelection(selectedRange);
        TextInputModel.Range composingRange = RangeFromBaseExtent(
            state["composingBase"],
            state["composingExtent"],
            model.ComposingRange);
        bool wasComposing = model.Composing;
        model.SetText(text, selectedRange, composingRange);
        if (composingRange.Collapsed && wasComposing)
        {
            MarkedTextDiscarded?.Invoke();
        }

        EditingStateChanged?.Invoke();
    }

    /// <summary>The macOS plugin's <c>RangeFromBaseExtent</c>: a missing value keeps
    /// <paramref name="range"/>, and Flutter's <c>-1</c>/<c>-1</c> "no range" becomes 0/0.</summary>
    private static TextInputModel.Range RangeFromBaseExtent(object? @base, object? extent, TextInputModel.Range range)
    {
        if (@base is null || extent is null)
        {
            return range;
        }

        int baseOffset = Convert.ToInt32(@base);
        int extentOffset = Convert.ToInt32(extent);
        if (baseOffset == -1 && extentOffset == -1)
        {
            return new TextInputModel.Range(0, 0);
        }

        return new TextInputModel.Range(baseOffset, extentOffset);
    }

    private void SetEditableTransform(IList matrix)
    {
        double[] values = new double[16];
        for (int i = 0; i < 16; i++)
        {
            values[i] = Convert.ToDouble(matrix[i]);
        }

        _editableTransform = Matrix4.FromList(values);
        CaretRectChanged?.Invoke();
    }

    private static Rect RectFromJson(IDictionary rect) => new(
        Convert.ToDouble(rect["x"]),
        Convert.ToDouble(rect["y"]),
        Convert.ToDouble(rect["width"]),
        Convert.ToDouble(rect["height"]));

    /// <summary>
    /// The caret rect in the coordinate space the framework's editable transform maps into (the
    /// view's physical pixels), or null before the framework sent one: the macOS plugin's
    /// <c>firstRectForCharacterRange:</c>, which only positions the caret. Without a
    /// <c>TextInput.setCaretRect</c> the Windows plugin's marked text rect is used.
    /// </summary>
    public Rect? GetCaretRect()
    {
        Rect? rect = _caretRect ?? _markedTextRect;
        return rect is { } incomingRect ? RectFromFrameworkTransform(incomingRect) : null;
    }

    /// <summary>The macOS plugin's <c>screenRectFromFrameworkTransform:</c> up to the view: the
    /// bounding box of the transformed corners, with the perspective divide.</summary>
    private Rect RectFromFrameworkTransform(Rect incomingRect)
    {
        Point[] points =
        [
            incomingRect.TopLeft,
            incomingRect.BottomLeft,
            incomingRect.TopRight,
            incomingRect.BottomRight,
        ];
        double[] m = _editableTransform.Storage;
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = -double.MaxValue;
        double maxY = -double.MaxValue;
        foreach (Point point in points)
        {
            double x = m[0] * point.X + m[4] * point.Y + m[12];
            double y = m[1] * point.X + m[5] * point.Y + m[13];
            double w = m[3] * point.X + m[7] * point.Y + m[15];
            if (w == 0.0)
            {
                return default;
            }

            if (w != 1.0)
            {
                x /= w;
                y /= w;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Commits <paramref name="text"/>: the macOS plugin's <c>insertText:replacementRange:</c>. The
    /// text replaces <paramref name="replacementRange"/> (a location and a possibly negative length,
    /// as AppKit sends it) when given, otherwise the composing text or the selection.
    /// </summary>
    public void InsertText(string text, (int Location, int Length)? replacementRange = null)
    {
        TextInputModel? model = _activeModel;
        if (model is null)
        {
            return;
        }

        if (replacementRange is { } range)
        {
            // The selected range can actually have negative numbers, since it can start at the end of
            // the range if the user selected the text going backwards.
            int textLength = model.TextRange.End;
            int @base = Math.Clamp(range.Location, 0, textLength);
            int extent = Math.Clamp(range.Location + range.Length, 0, textLength);
            model.SetSelection(new TextInputModel.Range(@base, extent));
        }
        else if (model.Composing && model.ComposingRange != model.Selection)
        {
            // When confirmed by Japanese IME, string replaces range of composing_range. If selection
            // != composing_range, the range of selection is only a part of composing_range. Since
            // AddText is processed first for selection, the cursor is placed at the beginning of
            // composing_range.
            model.SetSelection(new TextInputModel.Range(model.ComposingRange.Start));
        }

        TextInputModel.Range oldSelection = model.Selection;
        TextInputModel.Range composingBeforeChange = model.ComposingRange;
        string textBeforeChange = model.GetText();
        model.AddText(text);
        TextInputModel.Range replacedRange;
        if (model.Composing)
        {
            replacedRange = composingBeforeChange;
            model.CommitComposing();
            model.EndComposing();
        }
        else
        {
            replacedRange = replacementRange is { } replacement
                ? new TextInputModel.Range(replacement.Location, replacement.Location + replacement.Length)
                : new TextInputModel.Range(oldSelection.Base, oldSelection.Extent);
        }

        if (_enableDeltaModel)
        {
            UpdateEditStateWithDelta(textBeforeChange, replacedRange, text);
        }
        else
        {
            UpdateEditState();
        }
    }

    /// <summary>
    /// Sets the IME's marked (composing) text: the macOS plugin's
    /// <c>setMarkedText:selectedRange:replacementRange:</c>. <paramref name="selectedLocation"/> and
    /// <paramref name="selectedLength"/> place the selection inside the marked text.
    /// </summary>
    public void SetMarkedText(
        string text,
        int selectedLocation,
        int selectedLength = 0,
        (int Location, int Length)? replacementRange = null)
    {
        TextInputModel? model = _activeModel;
        if (model is null)
        {
            return;
        }

        string textBeforeChange = model.GetText();
        if (!model.Composing)
        {
            model.BeginComposing();
        }

        if (replacementRange is { } range)
        {
            // The replacement range is in practice relative to the text, not to the marked text (for
            // example when switching between equivalent characters after a long key press).
            model.SetComposingRange(new TextInputModel.Range(range.Location, range.Location + range.Length), 0);
        }

        TextInputModel.Range composingBeforeChange = model.ComposingRange;
        TextInputModel.Range selectionBeforeChange = model.Selection;
        model.UpdateComposingText(
            text,
            new TextInputModel.Range(selectedLocation, selectedLocation + selectedLength));
        if (_enableDeltaModel)
        {
            UpdateEditStateWithDelta(
                textBeforeChange,
                selectionBeforeChange.Collapsed ? composingBeforeChange : selectionBeforeChange,
                text);
        }
        else
        {
            UpdateEditState();
        }
    }

    /// <summary>Commits the marked text and ends composing: the macOS plugin's
    /// <c>unmarkText</c>.</summary>
    public void UnmarkText()
    {
        TextInputModel? model = _activeModel;
        if (model is null)
        {
            return;
        }

        model.CommitComposing();
        model.EndComposing();
        if (_enableDeltaModel)
        {
            UpdateEditStateWithNonTextDelta();
        }
        else
        {
            UpdateEditState();
        }
    }

    /// <summary>
    /// Moves the selection on behalf of the platform IME (Android's <c>InputConnection.setSelection</c>,
    /// Avalonia's <c>TextInputMethodClient.Selection</c> setter). Returns false, sending nothing, when
    /// the model rejects the range.
    /// </summary>
    public bool SetSelection(int baseOffset, int extentOffset)
    {
        TextInputModel? model = _activeModel;
        if (model is null || !model.SetSelection(new TextInputModel.Range(baseOffset, extentOffset)))
        {
            return false;
        }

        if (_enableDeltaModel)
        {
            UpdateEditStateWithNonTextDelta();
        }
        else
        {
            UpdateEditState();
        }

        return true;
    }

    /// <summary>
    /// Handles a key down (or repeat) event the framework left unhandled. Returns whether the plugin
    /// consumed it; nothing is consumed while no client is attached.
    /// </summary>
    /// <remarks>
    /// On macOS the key goes through AppKit's key bindings (the macOS plugin's <c>handleKeyEvent:</c>,
    /// which needs the input shown) and each selector through <see cref="DoCommandBySelector"/>; the
    /// selectors one key produces reach the framework as one <c>TextInputClient.performSelectors</c>
    /// call. Elsewhere only Enter is handled, as the Windows plugin's <c>KeyboardHook</c> does.
    /// </remarks>
    public bool HandleKeyEvent(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false)
    {
        if (_activeModel is null)
        {
            return false;
        }

        if (PlatformDefaults.TargetPlatform != TargetPlatform.MacOS)
        {
            bool isEnter = key.Equals(LogicalKeyboardKey.Enter) || key.Equals(LogicalKeyboardKey.NumpadEnter);
            if (!isEnter || control || alt || meta)
            {
                return false;
            }

            EnterPressed(_activeModel);
            return true;
        }

        if (!_shown)
        {
            return false;
        }

        IReadOnlyList<string>? selectors = MacOSStandardKeyBindings.SelectorsFor(key, control, shift, alt, meta);
        if (selectors is null)
        {
            return false;
        }

        var pendingSelectors = new List<object?>();
        foreach (string selector in selectors)
        {
            if (DoCommandBySelector(selector))
            {
                pendingSelectors.Add(selector);
            }
        }

        if (pendingSelectors.Count > 0 && _clientId is { } clientId)
        {
            InvokeMethod("TextInputClient.performSelectors", new List<object?> { clientId, pendingSelectors });
        }

        return true;
    }

    /// <summary>
    /// The macOS plugin's <c>doCommandBySelector:</c>: runs the plugin's own implementation of
    /// <paramref name="selector"/> and returns whether it must also be sent to the framework.
    /// </summary>
    /// <remarks>
    /// The plugin implements <c>insertNewline:</c> (newline and input action, never forwarded) and
    /// <c>insertTab:</c> (a no-op that keeps AppKit from inserting a tab character). The plugin is an
    /// <c>NSTextView</c>, whose own selector implementations edit only its mirror string; Plumix has
    /// no such mirror, so every other selector only goes to the framework.
    /// </remarks>
    private bool DoCommandBySelector(string selector)
    {
        if (selector == InsertNewlineSelector)
        {
            InsertNewline();
        }

        if (_clientId is null)
        {
            // The macOS may still call selector even if it is no longer a first responder.
            return false;
        }

        // insertNewline: is already handled through text insertion (multiline) or action.
        return selector != InsertNewlineSelector;
    }

    /// <summary>The macOS plugin's <c>insertNewline:</c>.</summary>
    private void InsertNewline()
    {
        TextInputModel? model = _activeModel;
        if (model is null)
        {
            return;
        }

        if (model.Composing)
        {
            model.CommitComposing();
            model.EndComposing();
        }

        if (_inputType == MultilineInputType && _inputAction == InputActionNewline)
        {
            TextInputModel.Range selected = model.Selection;
            InsertText("\n", (selected.Start, selected.Length));
        }

        InvokeMethod("TextInputClient.performAction", new List<object?> { _clientId, _inputAction });
    }

    /// <summary>The Windows plugin's <c>EnterPressed</c>: a multiline client whose action is newline
    /// gets <c>"\n"</c> first, then every client is told the input action.</summary>
    private void EnterPressed(TextInputModel model)
    {
        if (_inputType == MultilineInputType && _inputAction == InputActionNewline)
        {
            TextInputModel.Range selectionBeforeChange = model.Selection;
            string textBeforeChange = model.GetText();
            model.AddText("\n");
            if (_enableDeltaModel)
            {
                UpdateEditStateWithDelta(textBeforeChange, selectionBeforeChange, "\n");
            }
            else
            {
                UpdateEditState();
            }
        }

        InvokeMethod("TextInputClient.performAction", new List<object?> { _clientId, _inputAction ?? string.Empty });
    }

    /// <summary>The macOS plugin's <c>editingState</c>.</summary>
    private Dictionary<string, object?> EditingState(TextInputModel model)
    {
        return new Dictionary<string, object?>
        {
            ["selectionBase"] = model.Selection.Base,
            ["selectionExtent"] = model.Selection.Extent,
            ["selectionAffinity"] = TextAffinityString,
            ["selectionIsDirectional"] = false,
            ["composingBase"] = model.Composing ? model.ComposingRange.Base : -1,
            ["composingExtent"] = model.Composing ? model.ComposingRange.Extent : -1,
            ["text"] = model.GetText(),
        };
    }

    private string TextAffinityString =>
        _textAffinity == TextAffinity.Upstream ? TextAffinityUpstream : TextAffinityDownstream;

    /// <summary>The macOS plugin's <c>updateEditState</c>.</summary>
    private void UpdateEditState()
    {
        if (_activeModel is not { } model)
        {
            return;
        }

        InvokeMethod(
            "TextInputClient.updateEditingState",
            new List<object?> { _clientId, EditingState(model) });
        EditingStateChanged?.Invoke();
    }

    /// <summary>The macOS plugin's <c>updateEditStateWithDelta:</c> for the engine's
    /// <c>TextEditingDelta(text_before_change, range, text)</c>.</summary>
    private void UpdateEditStateWithDelta(string oldText, TextInputModel.Range range, string deltaText) =>
        SendDelta(oldText, deltaText, range.Start, range.Start + range.Length);

    /// <summary>The engine's <c>TextEditingDelta(text)</c>: a non-text update.</summary>
    private void UpdateEditStateWithNonTextDelta() =>
        SendDelta(_activeModel!.GetText(), string.Empty, -1, -1);

    private void SendDelta(string oldText, string deltaText, int deltaStart, int deltaEnd)
    {
        TextInputModel model = _activeModel!;
        var delta = new Dictionary<string, object?>
        {
            ["oldText"] = oldText,
            ["deltaText"] = deltaText,
            ["deltaStart"] = deltaStart,
            ["deltaEnd"] = deltaEnd,
            ["selectionBase"] = model.Selection.Base,
            ["selectionExtent"] = model.Selection.Extent,
            ["selectionAffinity"] = TextAffinityString,
            ["selectionIsDirectional"] = false,
            ["composingBase"] = model.Composing ? model.ComposingRange.Base : -1,
            ["composingExtent"] = model.Composing ? model.ComposingRange.Extent : -1,
        };
        InvokeMethod(
            "TextInputClient.updateEditingStateWithDeltas",
            new List<object?>
            {
                _clientId,
                new Dictionary<string, object?> { ["deltas"] = new List<object?> { delta } },
            });
        EditingStateChanged?.Invoke();
    }

    /// <summary>Sends a method call to the framework's side of the channel, as the engine's
    /// <c>FlutterMethodChannel invokeMethod:arguments:</c> does; the reply is not awaited.</summary>
    private void InvokeMethod(string method, object? arguments)
    {
        _ = _channel.BinaryMessenger.HandlePlatformMessage(
            _channel.Name,
            _channel.Codec.EncodeMethodCall(new MethodCall(method, arguments)),
            null);
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
