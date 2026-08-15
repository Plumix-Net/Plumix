using System.Collections;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

/// <summary>
/// An interface to receive information from the host's text input control.
/// </summary>
/// <remarks>
/// Plumix drives keystrokes through the host's focus adapter, so only the members the autofill
/// workflow needs are ported; the geometry and delta surfaces of Dart's <c>TextInputClient</c>
/// have no counterpart yet.
/// </remarks>
public interface ITextInputClient
{
    /// <summary>The <see cref="IAutofillScope"/> this client belongs to, or <c>null</c> when the
    /// client autofills alone.</summary>
    IAutofillScope? CurrentAutofillScope { get; }

    /// <summary>Requests that this client update its editing state to the given value.</summary>
    void UpdateEditingValue(TextEditingValue value);

    /// <summary>Requests that this client perform the given action.</summary>
    void PerformAction(TextInputActionType action);

    /// <summary>Called when the host closed the input connection.</summary>
    void ConnectionClosed();

    /// <summary>Called when the host's text input control received focus, so that a browser that
    /// blurs and refocuses a field before autofilling it can be handled.</summary>
    /// <returns><c>true</c> when the client handled the notification.</returns>
    bool OnFocusReceived() => false;
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

    /// <summary>Informs the control about the client's editing state.</summary>
    public virtual void SetEditingState(TextEditingValue value)
    {
    }

    /// <summary>Informs the control about the client's configuration.</summary>
    public virtual void UpdateConfig(TextInputConfiguration configuration)
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

    internal TextInputConnection(ITextInputClient client)
    {
        Client = client;
        Id = _nextId++;
    }

    internal int Id { get; }

    internal ITextInputClient Client { get; }

    /// <summary>Whether this connection is currently interacting with the input control.</summary>
    public bool Attached => ReferenceEquals(TextInput.CurrentConnection, this);

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

    /// <summary>Stops interacting with the text input control.</summary>
    public void Close()
    {
        if (Attached)
        {
            TextInput.ClearClient();
        }
    }

    /// <summary>Platform sent us a message informing us that the connection was closed.</summary>
    public void ConnectionClosedReceived()
    {
        TextInput.ConnectionClosedReceived(this);
        Client.ConnectionClosed();
    }

    /// <summary>Resets the connection id counter. For tests only.</summary>
    public static void DebugResetId(int to = 1)
    {
        _nextId = to;
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
    private static readonly List<TextInputControl> Controls = [PlatformControl];
    private static bool _initialized;

    /// <summary>Reports a channel failure the way Flutter's <c>FlutterError.reportError</c> does.
    /// </summary>
    /// <remarks>Plumix has no <c>FlutterError</c>, so failures are routed to this hook instead.
    /// </remarks>
    public static Action<Exception, string>? OnError { get; set; }

    internal static TextInputConnection? CurrentConnection { get; private set; }

    internal static TextInputConnection? LastConnection { get; private set; }

    /// <summary>Begins interacting with the text input control.</summary>
    public static TextInputConnection Attach(ITextInputClient client, TextInputConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);
        EnsureInitialized();
        var connection = new TextInputConnection(client);
        CurrentConnection = connection;
        LastConnection = connection;
        foreach (TextInputControl control in Controls.ToArray())
        {
            control.Attach(client, configuration);
        }

        return connection;
    }

    /// <summary>Finalizes the current autofill context, so that the platform may save or discard the
    /// values the user entered.</summary>
    /// <param name="shouldSave">Whether the platform should save the values it collected.</param>
    public static void FinishAutofillContext(bool shouldSave = true)
    {
        EnsureInitialized();
        foreach (TextInputControl control in Controls.ToArray())
        {
            control.FinishAutofillContext(shouldSave: shouldSave);
        }
    }

    /// <summary>Registers an alternative text input control.</summary>
    public static void SetInputControl(TextInputControl? control)
    {
        EnsureInitialized();
        Controls.Clear();
        Controls.Add(control ?? PlatformControl);
    }

    /// <summary>Restores the default platform text input control.</summary>
    public static void RestorePlatformInputControl() => SetInputControl(null);

    /// <summary>Registers the inbound <c>flutter/textinput</c> handler.</summary>
    /// <remarks>Dart does this lazily when <c>TextInput._instance</c> is first created.</remarks>
    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        SystemChannels.TextInput.SetMethodCallHandler(HandleTextInputInvocation);
    }

    internal static void Show() => Fan(control => control.Show(), "while showing the text input control");

    internal static void Hide() => Fan(control => control.Hide(), "while hiding the text input control");

    internal static void RequestAutofill() =>
        Fan(control => control.RequestAutofill(), "while requesting autofill");

    internal static void UpdateConfig(TextInputConfiguration configuration) =>
        Fan(control => control.UpdateConfig(configuration), "while updating the text input configuration");

    internal static void SetEditingState(TextEditingValue value) =>
        Fan(control => control.SetEditingState(value), "while setting the editing state");

    internal static void ClearClient()
    {
        ITextInputClient? client = CurrentConnection?.Client;
        CurrentConnection = null;
        if (client is null)
        {
            return;
        }

        Fan(control => control.Detach(client), "while detaching the text input client");
        Hide();
    }

    internal static void ConnectionClosedReceived(TextInputConnection connection)
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
        Controls.Clear();
        Controls.Add(PlatformControl);
        TextInputConnection.DebugResetId();
    }

    private static void Fan(Action<TextInputControl> action, string context)
    {
        foreach (TextInputControl control in Controls.ToArray())
        {
            try
            {
                action(control);
            }
            catch (Exception exception)
            {
                OnError?.Invoke(exception, context);
            }
        }
    }

    private static Task<object?> HandleTextInputInvocation(MethodCall call)
    {
        IList? arguments = call.Arguments as IList;
        switch (call.Method)
        {
            case "TextInputClient.onFocusReceived":
                if (LastConnection is not null && ArgumentId(arguments) == LastConnection.Id)
                {
                    LastConnection.Client.OnFocusReceived();
                }

                return Task.FromResult<object?>(null);
        }

        TextInputConnection? connection = CurrentConnection;
        if (connection is null)
        {
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
                    IAutofillClient? tagged = scope?.GetAutofillClient(tag);
                    if (tagged is null || !tagged.TextInputConfiguration.AutofillConfiguration.Enabled)
                    {
                        continue;
                    }

                    tagged.Autofill(TextEditingValue.FromJson((IDictionary)entry.Value!));
                }
            }

            return Task.FromResult<object?>(null);
        }

        if (ArgumentId(arguments) != connection.Id)
        {
            return Task.FromResult<object?>(null);
        }

        switch (call.Method)
        {
            case "TextInputClient.updateEditingState":
                connection.Client.UpdateEditingValue(
                    TextEditingValue.FromJson((IDictionary)arguments![1]!));
                break;
            case "TextInputClient.performAction":
                connection.Client.PerformAction(ParseAction((string)arguments![1]!));
                break;
            case "TextInputClient.onConnectionClosed":
                connection.ConnectionClosedReceived();
                break;
            default:
                throw new MissingPluginException();
        }

        return Task.FromResult<object?>(null);
    }

    private static int ArgumentId(IList? arguments)
    {
        return arguments is { Count: > 0 } && arguments[0] is not null
            ? Convert.ToInt32(arguments[0])
            : -1;
    }

    private static TextInputActionType ParseAction(string action)
    {
        string name = action.StartsWith("TextInputAction.", StringComparison.Ordinal)
            ? action["TextInputAction.".Length..]
            : action;
        return Enum.TryParse(name, ignoreCase: true, out TextInputActionType parsed)
            ? parsed
            : TextInputActionType.Unspecified;
    }

    private sealed class PlatformTextInputControl : TextInputControl
    {
        public override void Attach(ITextInputClient client, TextInputConfiguration configuration)
        {
            Invoke(
                "TextInput.setClient",
                new List<object?> { CurrentConnection!.Id, configuration.ToJson() },
                "while attaching the text input client");
        }

        public override void Detach(ITextInputClient client) =>
            Invoke("TextInput.clearClient", null, "while detaching the text input client");

        public override void Show() => Invoke("TextInput.show", null, "while showing the text input control");

        public override void Hide() => Invoke("TextInput.hide", null, "while hiding the text input control");

        public override void SetEditingState(TextEditingValue value) =>
            Invoke("TextInput.setEditingState", value.ToJson(), "while setting the editing state");

        public override void UpdateConfig(TextInputConfiguration configuration) =>
            Invoke(
                "TextInput.updateConfig",
                configuration.ToJson(),
                "while updating the text input configuration");

        public override void RequestAutofill() =>
            Invoke("TextInput.requestAutofill", null, "while requesting autofill");

        public override void FinishAutofillContext(bool shouldSave = true) =>
            Invoke("TextInput.finishAutofillContext", shouldSave, "while finishing autofill context");

        private static void Invoke(string method, object? arguments, string context)
        {
            Task<object?> pending = SystemChannels.TextInput.InvokeMethod<object>(method, arguments);
            _ = pending.ContinueWith(
                task => OnError?.Invoke(task.Exception!.GetBaseException(), context),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
