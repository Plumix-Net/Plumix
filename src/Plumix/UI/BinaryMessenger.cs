using System.Diagnostics;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/binary_messenger.dart

/// <summary>A function which decodes and handles a binary message received on a channel.</summary>
/// <remarks>Returning <c>null</c> means "no reply".</remarks>
public delegate Task<ByteData?>? MessageHandler(ByteData? message);

/// <summary>Signature for the callback that receives the reply to a platform message.</summary>
public delegate void PlatformMessageResponseCallback(ByteData? data);

/// <summary>A messenger which sends binary data across the Flutter platform barrier.</summary>
/// <remarks>
/// Flutter's barrier is the engine; Plumix's is the host adapter, so the direction that Dart cannot
/// express — registering the platform side of a channel — lives on
/// <see cref="SetPlatformMessageHandler"/>. See <c>docs/ai/DIVERGENCES.md</c>.
/// </remarks>
public abstract class BinaryMessenger
{
    /// <summary>Queues a message for the framework's handler on <paramref name="channel"/>.</summary>
    /// <remarks>
    /// Dart parity source: <c>BinaryMessenger.handlePlatformMessage</c>. Hosts call this to deliver an
    /// inbound platform message; <paramref name="callback"/> receives the framework's reply.
    /// </remarks>
    public abstract Task HandlePlatformMessage(
        string channel,
        ByteData? data,
        PlatformMessageResponseCallback? callback);

    /// <summary>Sends a binary message to the platform plugins on <paramref name="channel"/>.</summary>
    /// <returns>The reply, or <c>null</c> when no platform handler is registered for the channel.</returns>
    public abstract Task<ByteData?>? Send(string channel, ByteData? message);

    /// <summary>Sets a callback for receiving messages from the platform plugins on
    /// <paramref name="channel"/>.</summary>
    /// <remarks>The given callback replaces the currently registered callback, if any.</remarks>
    public abstract void SetMessageHandler(string channel, MessageHandler? handler);

    /// <summary>
    /// Registers the platform (host) side of <paramref name="channel"/>. Flutter has no equivalent — its
    /// platform side lives in the engine, outside Dart.
    /// </summary>
    public abstract void SetPlatformMessageHandler(string channel, MessageHandler? handler);
}

/// <summary>The default <see cref="BinaryMessenger"/>: an in-process channel registry.</summary>
/// <remarks>
/// Flutter's default messenger hands the bytes to the engine, which routes them to a plugin on the
/// platform thread. Plumix runs the whole application in one process, so both sides are dictionaries of
/// handlers and a send completes on the caller's thread whenever the platform handler is synchronous.
/// </remarks>
public class PlatformBinaryMessenger : BinaryMessenger
{
    private readonly Dictionary<string, MessageHandler> _handlers = [];
    private readonly Dictionary<string, MessageHandler> _platformHandlers = [];

    public override Task HandlePlatformMessage(
        string channel,
        ByteData? data,
        PlatformMessageResponseCallback? callback)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (!_handlers.TryGetValue(channel, out MessageHandler? handler))
        {
            callback?.Invoke(null);
            return Task.CompletedTask;
        }

        Task<ByteData?>? reply = handler(data);
        if (reply is null)
        {
            callback?.Invoke(null);
            return Task.CompletedTask;
        }

        return Respond(reply, callback);
    }

    public override Task<ByteData?>? Send(string channel, ByteData? message)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (!_platformHandlers.TryGetValue(channel, out MessageHandler? handler))
        {
            return Task.FromResult<ByteData?>(null);
        }

        return handler(message) ?? Task.FromResult<ByteData?>(null);
    }

    public override void SetMessageHandler(string channel, MessageHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(channel);
        Register(_handlers, channel, handler);
    }

    public override void SetPlatformMessageHandler(string channel, MessageHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(channel);
        Register(_platformHandlers, channel, handler);
    }

    /// <summary>Whether a framework handler is registered for <paramref name="channel"/>.</summary>
    public bool HasMessageHandler(string channel) => _handlers.ContainsKey(channel);

    private static void Register(Dictionary<string, MessageHandler> handlers, string channel, MessageHandler? handler)
    {
        if (handler is null)
        {
            handlers.Remove(channel);
            return;
        }

        handlers[channel] = handler;
    }

    private static async Task Respond(Task<ByteData?> reply, PlatformMessageResponseCallback? callback)
    {
        ByteData? data = await reply.ConfigureAwait(false);
        callback?.Invoke(data);
    }
}

/// <summary>
/// Owns the framework's platform-channel infrastructure. Dart parity source: <c>ServicesBinding</c>.
/// </summary>
/// <remarks>
/// Plumix has no binding hierarchy, so this is a single replaceable instance the way
/// <see cref="RestorationManager.Instance"/> is.
/// </remarks>
public class ServicesBinding
{
    /// <summary>The ambient services binding.</summary>
    public static ServicesBinding Instance { get; set; } = new ServicesBinding();

    /// <summary>The messenger every channel uses unless it was given one explicitly.</summary>
    public virtual BinaryMessenger DefaultBinaryMessenger { get; } = new PlatformBinaryMessenger();

    internal static void ResetForTests()
    {
        Instance = new ServicesBinding();
    }
}

/// <summary>Error reporting for the services layer.</summary>
/// <remarks>
/// Dart calls <c>FlutterError.reportError</c> inline at every services-layer catch site; this
/// helper exists only because C# has no way to spell the `FlutterErrorDetails` literal as tersely.
/// </remarks>
public static class ServicesDebug
{
    /// Reports `exception` to <see cref="FlutterError.ReportError"/> with Dart's context string.
    internal static void ReportError(Exception exception, string context)
    {
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "services library",
            context: new ErrorDescription(context)));
    }
}
