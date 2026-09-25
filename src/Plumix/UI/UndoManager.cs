using System.Collections;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/undo_manager.dart

namespace Plumix.UI;

/// <summary>The direction in which an undo action should be performed, whether undo or redo.</summary>
public enum UndoDirection
{
    /// <summary>Perform an undo action.</summary>
    Undo,

    /// <summary>Perform a redo action.</summary>
    Redo,
}

/// <summary>
/// A low-level interface to the system's undo manager: receives platform undo/redo requests over
/// <see cref="SystemChannels.UndoManager"/> and hands them to the current
/// <see cref="IUndoManagerClient"/>, and reports the client's undo state back to the platform.
/// </summary>
public sealed class UndoManager
{
    private static readonly UndoManager Instance = new();

    private MethodChannel _channel;
    private IUndoManagerClient? _currentClient;

    private UndoManager()
    {
        _channel = SystemChannels.UndoManager;
        _channel.SetMethodCallHandler(HandleUndoManagerInvocation);
    }

    /// <summary>Sets the <see cref="MethodChannel"/> used to communicate with the platform. Tests only.</summary>
    public static void SetChannel(MethodChannel newChannel)
    {
        Instance._channel = newChannel;
        newChannel.SetMethodCallHandler(Instance.HandleUndoManagerInvocation);
    }

    /// <summary>
    /// Receives platform undo/redo events: the client that should respond to them, typically the
    /// focused undo history.
    /// </summary>
    public static IUndoManagerClient? Client
    {
        get => Instance._currentClient;
        set => Instance._currentClient = value;
    }

    /// <summary>Sets the current state of the platform's undo/redo buttons.</summary>
    public static void SetUndoState(bool canUndo = false, bool canRedo = false) =>
        Instance.SetUndoStateCore(canUndo, canRedo);

    private Task<object?> HandleUndoManagerInvocation(MethodCall methodCall)
    {
        string method = methodCall.Method;
        var args = (IList)methodCall.Arguments!;
        if (method == "UndoManagerClient.handleUndo")
        {
            if (_currentClient is null)
            {
                throw new InvalidOperationException("There must be a current UndoManagerClient.");
            }

            _currentClient.HandlePlatformUndo(ToUndoDirection((string)args[0]!));
            return Task.FromResult<object?>(null);
        }

        throw new MissingPluginException();
    }

    private void SetUndoStateCore(bool canUndo, bool canRedo)
    {
        Task<object?> pending = _channel.InvokeMethod<object>(
            "UndoManager.setUndoState",
            new Dictionary<string, object?> { ["canUndo"] = canUndo, ["canRedo"] = canRedo });
        _ = pending.ContinueWith(
            task =>
            {
                Exception error = task.Exception!.GetBaseException();
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: error,
                    stack: error.StackTrace,
                    library: "services library",
                    context: new ErrorDescription("while sending the UndoManager.setUndoState event")));
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static UndoDirection ToUndoDirection(string direction) => direction switch
    {
        "undo" => UndoDirection.Undo,
        "redo" => UndoDirection.Redo,
        _ => throw new FlutterError([new ErrorSummary($"Unknown undo direction: {direction}")]),
    };
}

/// <summary>An object that can respond to undo/redo requests from the platform: Dart's
/// <c>UndoManagerClient</c> mixin.</summary>
public interface IUndoManagerClient
{
    /// <summary>Requests that the client perform an undo or redo operation.</summary>
    void HandlePlatformUndo(UndoDirection direction);

    /// <summary>Reverts the value to the previous state in the undo stack.</summary>
    void Undo();

    /// <summary>Updates the value to the next state in the undo stack.</summary>
    void Redo();

    /// <summary>Whether there is a previous state in the undo stack.</summary>
    bool CanUndo { get; }

    /// <summary>Whether there is a next state in the undo stack.</summary>
    bool CanRedo { get; }
}
