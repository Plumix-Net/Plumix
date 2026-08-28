using System.Collections;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

/// <summary>
/// An interface to receive information from the host's text input control.
/// </summary>
public interface ITextInputClient
{
    /// <summary>The current state of the client's text editing value, or <c>null</c> when the client
    /// has none.</summary>
    TextEditingValue? CurrentTextEditingValue { get; }

    /// <summary>The <see cref="IAutofillScope"/> this client belongs to, or <c>null</c> when the
    /// client autofills alone.</summary>
    IAutofillScope? CurrentAutofillScope { get; }

    /// <summary>Requests that this client update its editing state to the given value.</summary>
    void UpdateEditingValue(TextEditingValue value);

    /// <summary>Requests that this client perform the given action.</summary>
    void PerformAction(TextInputActionType action);

    /// <summary>Requests that this client perform the given private command.</summary>
    void PerformPrivateCommand(string action, IDictionary data);

    /// <summary>Updates the floating cursor position and state.</summary>
    void UpdateFloatingCursor(RawFloatingCursorPoint point);

    /// <summary>Requests that this client show a prompt rectangle for the autocorrect suggestion.
    /// </summary>
    void ShowAutocorrectionPromptRect(int start, int end);

    /// <summary>Called when the host closed the input connection.</summary>
    void ConnectionClosed();

    /// <summary>Requests that this client insert the given rich content.</summary>
    void InsertContent(KeyboardInsertedContent content)
    {
    }

    /// <summary>Called when the host's text input control received focus, so that a browser that
    /// blurs and refocuses a field before autofilling it can be handled.</summary>
    /// <returns><c>true</c> when the client handled the notification.</returns>
    bool OnFocusReceived() => false;

    /// <summary>Called when the visual text input control changed.</summary>
    void DidChangeInputControl(TextInputControl? oldControl, TextInputControl? newControl)
    {
    }

    /// <summary>Requests that this client show the editing toolbar.</summary>
    void ShowToolbar()
    {
    }

    /// <summary>Requests that this client add a text placeholder to reserve visual space.</summary>
    void InsertTextPlaceholder(Size size)
    {
    }

    /// <summary>Requests that this client remove the text placeholder.</summary>
    void RemoveTextPlaceholder()
    {
    }

    /// <summary>Performs the specified macOS-style selector from the input control.</summary>
    void PerformSelector(string selectorName)
    {
    }
}

/// <summary>
/// An interface to receive granular information from the host's text input control.
/// </summary>
/// <remarks>Opt in by setting <see cref="TextInputConfiguration.EnableDeltaModel"/>.</remarks>
public interface IDeltaTextInputClient : ITextInputClient
{
    /// <summary>Requests that this client update its editing state by applying the given deltas.
    /// </summary>
    void UpdateEditingValueWithDeltas(IReadOnlyList<TextEditingDelta> textEditingDeltas);
}

/// <summary>
/// An interface for implementing text input controls that receive text editing state changes and
/// visual input control requests.
/// </summary>
/// <remarks>Every member is a no-op by default, the way Dart's <c>TextInputControl</c> mixin is.
/// </remarks>
public class TextInputControl
{
    /// <summary>Requests the control to attach to the given client.</summary>
    public virtual void Attach(ITextInputClient client, TextInputConfiguration configuration)
    {
    }

    /// <summary>Requests the control to detach from the given client.</summary>
    public virtual void Detach(ITextInputClient client)
    {
    }

    /// <summary>Requests that the control be shown.</summary>
    public virtual void Show()
    {
    }

    /// <summary>Requests that the control be hidden.</summary>
    public virtual void Hide()
    {
    }

    /// <summary>Informs the control about the client's configuration.</summary>
    public virtual void UpdateConfig(TextInputConfiguration configuration)
    {
    }

    /// <summary>Informs the control about the client's editing state.</summary>
    public virtual void SetEditingState(TextEditingValue value)
    {
    }

    /// <summary>Informs the control about the client's size and transform.</summary>
    public virtual void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform)
    {
    }

    /// <summary>Informs the control about the composing area of the client.</summary>
    public virtual void SetComposingRect(Rect rect)
    {
    }

    /// <summary>Informs the control about the caret area of the client.</summary>
    public virtual void SetCaretRect(Rect rect)
    {
    }

    /// <summary>Informs the control about the selection area of the client.</summary>
    public virtual void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects)
    {
    }

    /// <summary>Informs the control about the client's text style.</summary>
    public virtual void UpdateStyle(TextInputStyle style)
    {
    }

    /// <summary>Requests autofill from the control.</summary>
    public virtual void RequestAutofill()
    {
    }

    /// <summary>Requests that the autofill context be finalized.</summary>
    public virtual void FinishAutofillContext(bool shouldSave = true)
    {
    }
}

/// <summary>
/// An interface for interacting with a text input control.
/// </summary>
public class TextInputConnection
{
    private static int _nextId = 1;

    private Size? _cachedSize;
    private Matrix4? _cachedTransform;
    private Rect? _cachedRect;
    private Rect? _cachedCaretRect;
    private IReadOnlyList<SelectionRect> _cachedSelectionRects = [];

    internal TextInputConnection(ITextInputClient client)
    {
        Client = client;
        Id = _nextId++;
    }

    internal int Id { get; }

    internal ITextInputClient Client { get; }

    /// <summary>Whether this connection is currently interacting with the input control.</summary>
    public bool Attached => ReferenceEquals(TextInput.CurrentConnection, this);

    /// <summary>Whether a scribble interaction is currently happening.</summary>
    public bool ScribbleInProgress => TextInput.ScribbleInProgress;

    /// <summary>Requests that the text input control become visible.</summary>
    public void Show()
    {
        Require(Attached);
        TextInput.Show();
    }

    /// <summary>Requests the platform autofill UI to appear.</summary>
    public void RequestAutofill()
    {
        Require(Attached);
        TextInput.RequestAutofill();
    }

    /// <summary>Requests that the text input control change its internal state to match the given
    /// configuration.</summary>
    public void UpdateConfig(TextInputConfiguration configuration)
    {
        Require(Attached);
        TextInput.UpdateConfig(configuration);
    }

    /// <summary>Requests that the text input control change its internal state to match the given
    /// state.</summary>
    public void SetEditingState(TextEditingValue value)
    {
        Require(Attached);
        TextInput.SetEditingState(value);
    }

    /// <summary>Sends the size and transform of the editable text area to the text input control.
    /// </summary>
    public void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform)
    {
        if (editableBoxSize.Equals(_cachedSize) && transform == _cachedTransform)
        {
            return;
        }

        _cachedSize = editableBoxSize;
        _cachedTransform = transform;
        TextInput.SetEditableSizeAndTransform(editableBoxSize, transform);
    }

    /// <summary>Sends the coordinates of the composing region to the text input control.</summary>
    public void SetComposingRect(Rect rect)
    {
        if (_cachedRect is not null && rect.Equals(_cachedRect.Value))
        {
            return;
        }

        _cachedRect = rect;
        TextInput.SetComposingTextRect(SanitizeRect(rect));
    }

    /// <summary>Sends the coordinates of the caret to the text input control.</summary>
    public void SetCaretRect(Rect rect)
    {
        if (_cachedCaretRect is not null && rect.Equals(_cachedCaretRect.Value))
        {
            return;
        }

        _cachedCaretRect = rect;
        TextInput.SetCaretRect(SanitizeRect(rect));
    }

    /// <summary>Sends the coordinates of each character to the text input control.</summary>
    public void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects)
    {
        ArgumentNullException.ThrowIfNull(selectionRects);
        if (_cachedSelectionRects.SequenceEqual(selectionRects))
        {
            return;
        }

        _cachedSelectionRects = selectionRects;
        TextInput.SetSelectionRects(selectionRects);
    }

    /// <summary>Sends the text style of the editable to the text input control.</summary>
    [Obsolete("Use UpdateStyle instead. Mirrors Flutter's deprecation after v3.41.0-0.0.pre.")]
    public void SetStyle(
        string? fontFamily,
        double? fontSize,
        FontWeight? fontWeight,
        TextDirection textDirection,
        TextAlign textAlign)
    {
        UpdateStyle(
            new TextInputStyle(
                textDirection: textDirection,
                textAlign: textAlign,
                fontFamily: fontFamily,
                fontSize: fontSize,
                fontWeight: fontWeight));
    }

    /// <summary>Sends the text style of the editable to the text input control.</summary>
    public void UpdateStyle(TextInputStyle style)
    {
        Require(Attached);
        TextInput.UpdateStyle(style);
    }

    /// <summary>Stops interacting with the text input control.</summary>
    public void Close()
    {
        if (Attached)
        {
            TextInput.ClearClient();
        }
    }

    /// <summary>Platform sent us a message informing us that the connection was closed.</summary>
    /// <remarks>Like Dart, this only drops the connection; the client is notified through the
    /// inbound <c>TextInputClient.onConnectionClosed</c> message.</remarks>
    public void ConnectionClosedReceived()
    {
        TextInput.DropConnection(this);
    }

    /// <summary>Resets the connection id counter. For tests only.</summary>
    public static void DebugResetId(int to = 1)
    {
        _nextId = to;
    }

    private static Rect SanitizeRect(Rect rect)
    {
        bool isFinite = double.IsFinite(rect.X)
                        && double.IsFinite(rect.Y)
                        && double.IsFinite(rect.Right)
                        && double.IsFinite(rect.Bottom);
        return isFinite ? rect : new Rect(0.0, 0.0, -1.0, -1.0);
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("The text input connection is not attached.");
        }
    }
}

/// <summary>
/// Provides access to the host's text input control.
/// </summary>
public static class TextInput
{
    private static readonly TextInputControl PlatformControl = new PlatformTextInputControl();
    private static readonly List<TextInputControl> InputControls = [PlatformControl];
    private static readonly Dictionary<string, IScribbleClient> ScribbleClientRegistry = [];
    private static TextInputControl? _currentControl = PlatformControl;
    private static TextInputConfiguration? _currentConfiguration;
    private static bool _hidePending;
    private static bool _scribbleInProgress;
    private static bool _initialized;

    internal static TextInputConnection? CurrentConnection { get; private set; }

    internal static TextInputConnection? LastConnection { get; private set; }

    internal static TextInputControl? CurrentControl => _currentControl;

    /// <summary>Whether a scribble interaction is currently happening.</summary>
    public static bool ScribbleInProgress => _scribbleInProgress;

    /// <summary>The scribble clients registered with the service. For tests only.</summary>
    internal static IReadOnlyDictionary<string, IScribbleClient> ScribbleClients => ScribbleClientRegistry;

    /// <summary>Begins interacting with the text input control.</summary>
    public static TextInputConnection Attach(ITextInputClient client, TextInputConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);
        EnsureInitialized();
        var connection = new TextInputConnection(client);
        AttachConnection(connection, configuration);
        return connection;
    }

    /// <summary>Finalizes the current autofill context, so that the platform may save or discard the
    /// values the user entered.</summary>
    /// <param name="shouldSave">Whether the platform should save the values it collected.</param>
    public static void FinishAutofillContext(bool shouldSave = true)
    {
        EnsureInitialized();
        foreach (TextInputControl control in InputControls.ToArray())
        {
            control.FinishAutofillContext(shouldSave: shouldSave);
        }
    }

    /// <summary>Sets the current visual text input control.</summary>
    /// <remarks>The platform control always stays registered, so it keeps receiving the editing
    /// state; passing <c>null</c> removes the visual control the way Dart does.</remarks>
    public static void SetInputControl(TextInputControl? control)
    {
        EnsureInitialized();
        TextInputControl? oldControl = _currentControl;
        if (ReferenceEquals(control, oldControl))
        {
            return;
        }

        if (control is not null)
        {
            AddInputControl(control);
        }

        if (oldControl is not null)
        {
            RemoveInputControl(oldControl);
        }

        _currentControl = control;
        CurrentConnection?.Client.DidChangeInputControl(oldControl, control);
    }

    /// <summary>Restores the default platform text input control.</summary>
    public static void RestorePlatformInputControl() => SetInputControl(PlatformControl);

    /// <summary>Registers a scribble client with the given element identifier.</summary>
    public static void RegisterScribbleElement(string elementIdentifier, IScribbleClient scribbleClient)
    {
        ScribbleClientRegistry[elementIdentifier] = scribbleClient;
    }

    /// <summary>Unregisters the scribble client with the given element identifier.</summary>
    public static void UnregisterScribbleElement(string elementIdentifier)
    {
        ScribbleClientRegistry.Remove(elementIdentifier);
    }

    /// <summary>Pushes an editing state produced by a custom input control into the framework.
    /// </summary>
    public static void UpdateEditingValue(TextEditingValue value) =>
        UpdateEditingValue(value, exclude: _currentControl);

    /// <summary>Registers the inbound <c>flutter/textinput</c> handler.</summary>
    /// <remarks>Dart does this lazily when <c>TextInput._instance</c> is first created.</remarks>
    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        SystemChannels.TextInput.SetMethodCallHandler(LoudlyHandleTextInputInvocation);
    }

    internal static void Show() => Fan(control => control.Show());

    internal static void Hide() => Fan(control => control.Hide());

    internal static void RequestAutofill() => Fan(control => control.RequestAutofill());

    internal static void UpdateConfig(TextInputConfiguration configuration) =>
        Fan(control => control.UpdateConfig(configuration));

    internal static void SetEditingState(TextEditingValue value) =>
        Fan(control => control.SetEditingState(value));

    internal static void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform) =>
        Fan(control => control.SetEditableSizeAndTransform(editableBoxSize, transform));

    internal static void SetComposingTextRect(Rect rect) => Fan(control => control.SetComposingRect(rect));

    internal static void SetCaretRect(Rect rect) => Fan(control => control.SetCaretRect(rect));

    internal static void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects) =>
        Fan(control => control.SetSelectionRects(selectionRects));

    internal static void UpdateStyle(TextInputStyle style) => Fan(control => control.UpdateStyle(style));

    internal static void ClearClient()
    {
        ITextInputClient client = CurrentConnection!.Client;
        Fan(control => control.Detach(client));
        CurrentConnection = null;
        ScheduleHide();
    }

    internal static void DropConnection(TextInputConnection connection)
    {
        if (ReferenceEquals(CurrentConnection, connection))
        {
            CurrentConnection = null;
        }
    }

    /// <summary>Resets the connection state. For tests only.</summary>
    internal static void DebugReset()
    {
        CurrentConnection = null;
        LastConnection = null;
        _currentConfiguration = null;
        _currentControl = PlatformControl;
        _hidePending = false;
        _scribbleInProgress = false;
        InputControls.Clear();
        InputControls.Add(PlatformControl);
        ScribbleClientRegistry.Clear();
        TextInputConnection.DebugResetId();
    }

    private static void AttachConnection(TextInputConnection connection, TextInputConfiguration configuration)
    {
        CurrentConnection = connection;
        _currentConfiguration = configuration;
        LastConnection = connection;
        ITextInputClient client = connection.Client;
        Fan(control => control.Attach(client, configuration));
    }

    private static void AddInputControl(TextInputControl control)
    {
        if (!ReferenceEquals(control, PlatformControl) && !InputControls.Contains(control))
        {
            InputControls.Add(control);
        }
    }

    private static void RemoveInputControl(TextInputControl control)
    {
        if (!ReferenceEquals(control, PlatformControl))
        {
            InputControls.Remove(control);
        }
    }

    private static void ScheduleHide()
    {
        if (_hidePending)
        {
            return;
        }

        _hidePending = true;
        Scheduler.ScheduleMicrotask(() =>
        {
            _hidePending = false;
            if (CurrentConnection is null)
            {
                Hide();
            }
        });
    }

    private static void UpdateEditingValue(TextEditingValue value, TextInputControl? exclude)
    {
        if (CurrentConnection is null)
        {
            return;
        }

        foreach (TextInputControl control in InputControls.ToArray())
        {
            if (!ReferenceEquals(control, exclude))
            {
                control.SetEditingState(value);
            }
        }

        CurrentConnection!.Client.UpdateEditingValue(value);
    }

    private static void Fan(Action<TextInputControl> action)
    {
        foreach (TextInputControl control in InputControls.ToArray())
        {
            action(control);
        }
    }

    /// Dart's file-private `_reportError` from `services/text_input.dart`.
    internal static void ReportError(
        Exception exception,
        string context,
        InformationCollector? informationCollector = null)
    {
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "services library",
            context: new ErrorDescription(context),
            informationCollector: informationCollector));
    }

    private static async Task<object?> LoudlyHandleTextInputInvocation(MethodCall call)
    {
        try
        {
            return await HandleTextInputInvocation(call).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ReportError(
                exception,
                $"during method call {call.Method}",
                () => [new DiagnosticsProperty<MethodCall>(
                    "call",
                    call,
                    style: DiagnosticsTreeStyle.ErrorProperty)]);
            throw;
        }
    }

    private static Task<object?> HandleTextInputInvocation(MethodCall call)
    {
        IList? arguments = call.Arguments as IList;
        switch (call.Method)
        {
            case "TextInputClient.focusElement":
            {
                string identifier = (string)arguments![0]!;
                if (ScribbleClientRegistry.TryGetValue(identifier, out IScribbleClient? scribbleClient))
                {
                    scribbleClient.OnScribbleFocus(
                        new Point(Convert.ToDouble(arguments[1]), Convert.ToDouble(arguments[2])));
                }

                return Task.FromResult<object?>(null);
            }

            case "TextInputClient.requestElementsInRect":
            {
                var rect = new Rect(
                    Convert.ToDouble(arguments![0]),
                    Convert.ToDouble(arguments[1]),
                    Convert.ToDouble(arguments[2]),
                    Convert.ToDouble(arguments[3]));
                var elements = new List<object?>();
                foreach (IScribbleClient client in ScribbleClientRegistry.Values)
                {
                    if (!client.IsInScribbleRect(rect))
                    {
                        continue;
                    }

                    Rect bounds = client.Bounds;
                    if (bounds == default || HasNaN(bounds) || IsInfinite(bounds))
                    {
                        continue;
                    }

                    elements.Add(
                        new List<object?>
                        {
                            client.ElementIdentifier,
                            bounds.X,
                            bounds.Y,
                            bounds.Width,
                            bounds.Height,
                        });
                }

                return Task.FromResult<object?>(elements);
            }

            case "TextInputClient.scribbleInteractionBegan":
                _scribbleInProgress = true;
                return Task.FromResult<object?>(null);

            case "TextInputClient.scribbleInteractionFinished":
                _scribbleInProgress = false;
                return Task.FromResult<object?>(null);

            case "TextInputClient.onFocusReceived":
                if (LastConnection is not null && ArgumentId(arguments) == LastConnection.Id)
                {
                    return Task.FromResult<object?>(LastConnection.Client.OnFocusReceived());
                }

                return Task.FromResult<object?>(false);
        }

        TextInputConnection? connection = CurrentConnection;
        if (connection is null)
        {
            return Task.FromResult<object?>(null);
        }

        if (call.Method == "TextInputClient.requestExistingInputState")
        {
            AttachConnection(connection, _currentConfiguration!);
            TextEditingValue? editingValue = connection.Client.CurrentTextEditingValue;
            if (editingValue is not null)
            {
                SetEditingState(editingValue.Value);
            }

            return Task.FromResult<object?>(null);
        }

        if (call.Method == "TextInputClient.updateEditingStateWithTag")
        {
            IAutofillScope? scope = connection.Client.CurrentAutofillScope;
            if (arguments is { Count: > 1 } && arguments[1] is IDictionary values)
            {
                foreach (DictionaryEntry entry in values)
                {
                    string tag = (string)entry.Key;
                    var textEditingValue = TextEditingValue.FromJson((IDictionary)entry.Value!);
                    IAutofillClient? tagged = scope?.GetAutofillClient(tag);
                    if (tagged is null || !tagged.TextInputConfiguration.AutofillConfiguration.Enabled)
                    {
                        continue;
                    }

                    tagged.Autofill(textEditingValue);
                }
            }

            return Task.FromResult<object?>(null);
        }

        int clientId = ArgumentId(arguments);
        if (clientId != connection.Id && clientId != DebugAnyClientId)
        {
            return Task.FromResult<object?>(null);
        }

        switch (call.Method)
        {
            case "TextInputClient.updateEditingState":
                UpdateEditingValue(
                    TextEditingValue.FromJson((IDictionary)arguments![1]!),
                    exclude: PlatformControl);
                break;
            case "TextInputClient.updateEditingStateWithDeltas":
            {
                if (connection.Client is not IDeltaTextInputClient deltaClient)
                {
                    throw new InvalidOperationException(
                        "You must be using a IDeltaTextInputClient if "
                        + "TextInputConfiguration.EnableDeltaModel is set to true");
                }

                var encoded = (IDictionary)arguments![1]!;
                var deltas = new List<TextEditingDelta>();
                foreach (object? entry in (IList)encoded["deltas"]!)
                {
                    deltas.Add(TextEditingDelta.FromJson((IDictionary)entry!));
                }

                deltaClient.UpdateEditingValueWithDeltas(deltas);
                break;
            }

            case "TextInputClient.performAction":
                if ((string)arguments![1]! == "TextInputAction.commitContent")
                {
                    connection.Client.InsertContent(
                        KeyboardInsertedContent.FromJson((IDictionary)arguments[2]!));
                }
                else
                {
                    connection.Client.PerformAction(TextInputActions.Parse((string)arguments[1]!));
                }

                break;
            case "TextInputClient.performSelectors":
                foreach (object? selector in (IList)arguments![1]!)
                {
                    connection.Client.PerformSelector((string)selector!);
                }

                break;
            case "TextInputClient.performPrivateCommand":
            {
                var command = (IDictionary)arguments![1]!;
                IDictionary data = command.Contains("data") && command["data"] is IDictionary payload
                    ? payload
                    : new Dictionary<string, object?>();
                connection.Client.PerformPrivateCommand((string)command["action"]!, data);
                break;
            }

            case "TextInputClient.updateFloatingCursor":
                connection.Client.UpdateFloatingCursor(
                    ToTextPoint(
                        ToTextCursorAction((string)arguments![1]!),
                        (IDictionary)arguments[2]!));
                break;
            case "TextInputClient.onConnectionClosed":
                connection.Client.ConnectionClosed();
                break;
            case "TextInputClient.showAutocorrectionPromptRect":
                connection.Client.ShowAutocorrectionPromptRect(
                    Convert.ToInt32(arguments![1]),
                    Convert.ToInt32(arguments[2]));
                break;
            case "TextInputClient.showToolbar":
                connection.Client.ShowToolbar();
                break;
            case "TextInputClient.insertTextPlaceholder":
                connection.Client.InsertTextPlaceholder(
                    new Size(Convert.ToDouble(arguments![1]), Convert.ToDouble(arguments[2])));
                break;
            case "TextInputClient.removeTextPlaceholder":
                connection.Client.RemoveTextPlaceholder();
                break;
            default:
                throw new MissingPluginException();
        }

        return Task.FromResult<object?>(null);
    }

    /// <summary>The client id Dart accepts from any connection in debug builds.</summary>
    /// <remarks>Plumix has no assert elision, so it is accepted in every build.</remarks>
    private const int DebugAnyClientId = -1;

    private static bool HasNaN(Rect rect) =>
        double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsNaN(rect.Width) || double.IsNaN(rect.Height);

    private static bool IsInfinite(Rect rect) =>
        double.IsInfinity(rect.X)
        || double.IsInfinity(rect.Y)
        || double.IsInfinity(rect.Width)
        || double.IsInfinity(rect.Height);

    private static FloatingCursorDragState ToTextCursorAction(string state)
    {
        return state switch
        {
            "FloatingCursorDragState.start" => FloatingCursorDragState.Start,
            "FloatingCursorDragState.update" => FloatingCursorDragState.Update,
            "FloatingCursorDragState.end" => FloatingCursorDragState.End,
            _ => throw new ArgumentException($"Unknown text cursor action: {state}", nameof(state)),
        };
    }

    private static RawFloatingCursorPoint ToTextPoint(FloatingCursorDragState state, IDictionary encoded)
    {
        var offset = state == FloatingCursorDragState.Update
            ? new Point(Convert.ToDouble(encoded["X"]), Convert.ToDouble(encoded["Y"]))
            : default(Point);
        return new RawFloatingCursorPoint(state, offset);
    }

    private static int ArgumentId(IList? arguments)
    {
        return arguments is { Count: > 0 } && arguments[0] is not null
            ? Convert.ToInt32(arguments[0])
            : int.MinValue;
    }

    private sealed class PlatformTextInputControl : TextInputControl
    {
        public override void Attach(ITextInputClient client, TextInputConfiguration configuration)
        {
            Invoke(
                "TextInput.setClient",
                new List<object?> { CurrentConnection!.Id, ConfigurationToJson(configuration) },
                "while attaching the text input client");
        }

        public override void Detach(ITextInputClient client) =>
            Invoke("TextInput.clearClient", null, "while detaching the text input client");

        public override void Show() => Invoke("TextInput.show", null, "while showing the text input client");

        public override void Hide() => Invoke("TextInput.hide", null, "while hiding the text input client");

        public override void UpdateConfig(TextInputConfiguration configuration) =>
            Invoke(
                "TextInput.updateConfig",
                ConfigurationToJson(configuration),
                "while updating text input configuration");

        public override void SetEditingState(TextEditingValue value) =>
            Invoke("TextInput.setEditingState", value.ToJson(), "while setting text input editing state");

        public override void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform) =>
            Invoke(
                "TextInput.setEditableSizeAndTransform",
                new Dictionary<string, object?>
                {
                    ["width"] = editableBoxSize.Width,
                    ["height"] = editableBoxSize.Height,
                    ["transform"] = transform.Storage,
                },
                "while setting text input size and transform");

        public override void SetComposingRect(Rect rect) =>
            Invoke("TextInput.setMarkedTextRect", RectToJson(rect), "while setting text input composing rect");

        public override void SetCaretRect(Rect rect) =>
            Invoke("TextInput.setCaretRect", RectToJson(rect), "while setting text input caret rect");

        public override void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects) =>
            Invoke(
                "TextInput.setSelectionRects",
                selectionRects
                    .Select(rect => new List<object?>
                    {
                        rect.Bounds.X,
                        rect.Bounds.Y,
                        rect.Bounds.Width,
                        rect.Bounds.Height,
                        rect.Position,
                        (int)rect.Direction,
                    })
                    .ToList(),
                "while setting text input selection rects");

        public override void UpdateStyle(TextInputStyle style) =>
            Invoke("TextInput.setStyle", style.ToJson(), "while updating text input style");

        public override void RequestAutofill() =>
            Invoke("TextInput.requestAutofill", null, "while requesting autofill");

        public override void FinishAutofillContext(bool shouldSave = true) =>
            Invoke("TextInput.finishAutofillContext", shouldSave, "while finishing autofill context");

        /// <summary>Replaces the input type with <c>TextInputType.none</c> while a custom control is
        /// installed, so the platform keyboard stays hidden.</summary>
        private static Dictionary<string, object?> ConfigurationToJson(TextInputConfiguration configuration)
        {
            Dictionary<string, object?> json = configuration.ToJson();
            if (ReferenceEquals(CurrentControl, PlatformControlInstance))
            {
                return json;
            }

            Dictionary<string, object?> none = TextInputType.None.ToJson();
            if (OperatingSystem.IsBrowser())
            {
                none["isMultiline"] = configuration.IsMultiline;
            }

            json["inputType"] = none;
            return json;
        }

        private static TextInputControl PlatformControlInstance => PlatformControl;

        private static Dictionary<string, object?> RectToJson(Rect rect) =>
            new()
            {
                ["width"] = rect.Width,
                ["height"] = rect.Height,
                ["x"] = rect.X,
                ["y"] = rect.Y,
            };

        private static void Invoke(string method, object? arguments, string context)
        {
            Task<object?> pending = SystemChannels.TextInput.InvokeMethod<object>(method, arguments);
            _ = pending.ContinueWith(
                task => ReportError(task.Exception!.GetBaseException(), context),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
