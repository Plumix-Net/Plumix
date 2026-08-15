using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/platform_channel.dart

/// <summary>A named channel for communicating with platform plugins using asynchronous message
/// passing.</summary>
/// <remarks>
/// Messages are encoded into binary before being sent, and binary messages received are decoded into
/// values. The <see cref="MessageCodec{T}"/> used must be compatible with the one used by the platform
/// plugin. All operations return futures whose values become available on the framework's thread.
/// </remarks>
public class BasicMessageChannel<T>
{
    private readonly BinaryMessenger? _binaryMessenger;

    /// <summary>Creates a <see cref="BasicMessageChannel{T}"/> with the specified
    /// <paramref name="name"/> and <paramref name="codec"/>.</summary>
    public BasicMessageChannel(string name, MessageCodec<T> codec, BinaryMessenger? binaryMessenger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(codec);
        Name = name;
        Codec = codec;
        _binaryMessenger = binaryMessenger;
    }

    /// <summary>The logical channel on which communication happens, not <c>null</c>.</summary>
    public string Name { get; }

    /// <summary>The message codec used by this channel, not <c>null</c>.</summary>
    public MessageCodec<T> Codec { get; }

    /// <summary>The messenger which sends the bytes for this channel.</summary>
    public BinaryMessenger BinaryMessenger => _binaryMessenger ?? ServicesBinding.Instance.DefaultBinaryMessenger;

    /// <summary>Sends the specified <paramref name="message"/> to the platform plugins on this channel.</summary>
    /// <returns>The reply, or <c>null</c> if the reply was empty.</returns>
    public async Task<T?> Send(T message)
    {
        Task<ByteData?>? reply = BinaryMessenger.Send(Name, Codec.EncodeMessage(message));
        return Codec.DecodeMessage(reply is null ? null : await reply.ConfigureAwait(false));
    }

    /// <summary>Sets a callback for receiving messages from the platform plugins on this channel.</summary>
    /// <remarks>The given callback replaces the currently registered callback, if any.</remarks>
    public void SetMessageHandler(Func<T?, Task<T?>>? handler)
    {
        if (handler is null)
        {
            BinaryMessenger.SetMessageHandler(Name, null);
            return;
        }

        BinaryMessenger.SetMessageHandler(Name, async message =>
        {
            T? reply = await handler(Codec.DecodeMessage(message)).ConfigureAwait(false);
            return Codec.EncodeMessage(reply!);
        });
    }
}

/// <summary>A named channel for communicating with platform plugins using asynchronous method
/// calls.</summary>
/// <remarks>
/// Method calls are encoded into binary before being sent, and binary results received are decoded into
/// values. The <see cref="MethodCodec"/> used must be compatible with the one used by the platform
/// plugin.
/// </remarks>
public class MethodChannel
{
    private readonly BinaryMessenger? _binaryMessenger;

    /// <summary>Creates a <see cref="MethodChannel"/> with the specified <paramref name="name"/>.</summary>
    public MethodChannel(string name, MethodCodec? codec = null, BinaryMessenger? binaryMessenger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Codec = codec ?? new StandardMethodCodec();
        _binaryMessenger = binaryMessenger;
    }

    /// <summary>The logical channel on which communication happens, not <c>null</c>.</summary>
    public string Name { get; }

    /// <summary>The message codec used by this channel, not <c>null</c>.</summary>
    public MethodCodec Codec { get; }

    /// <summary>The messenger which sends the bytes for this channel.</summary>
    public BinaryMessenger BinaryMessenger => _binaryMessenger ?? ServicesBinding.Instance.DefaultBinaryMessenger;

    /// <summary>Invokes a <paramref name="method"/> on this channel with the specified
    /// <paramref name="arguments"/>.</summary>
    /// <exception cref="PlatformException">The platform plugin returned an error envelope.</exception>
    /// <exception cref="MissingPluginException">No plugin handles the method on this channel.</exception>
    public virtual Task<T?> InvokeMethod<T>(string method, object? arguments = null)
    {
        return InvokeMethodInternal<T>(method, missingOk: false, arguments);
    }

    /// <summary>An <see cref="InvokeMethod{T}"/> that returns a list of <typeparamref name="T"/>.</summary>
    public async Task<List<T>?> InvokeListMethod<T>(string method, object? arguments = null)
    {
        object? result = await InvokeMethod<object>(method, arguments).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        var list = new List<T>();
        foreach (object? item in (System.Collections.IList)result)
        {
            list.Add((T)item!);
        }

        return list;
    }

    /// <summary>An <see cref="InvokeMethod{T}"/> that returns a map of
    /// <typeparamref name="TKey"/>/<typeparamref name="TValue"/>.</summary>
    public async Task<Dictionary<TKey, TValue>?> InvokeMapMethod<TKey, TValue>(
        string method,
        object? arguments = null)
        where TKey : notnull
    {
        object? result = await InvokeMethod<object>(method, arguments).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        var map = new Dictionary<TKey, TValue>();
        foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)result)
        {
            map[(TKey)entry.Key] = (TValue)entry.Value!;
        }

        return map;
    }

    /// <summary>Sets a callback for receiving method calls on this channel.</summary>
    /// <remarks>
    /// The given callback replaces the currently registered callback, if any. A handler that throws a
    /// <see cref="PlatformException"/> produces an error envelope; one that throws a
    /// <see cref="MissingPluginException"/> produces an empty reply; any other exception produces an
    /// <c>error</c> envelope carrying its message.
    /// </remarks>
    public void SetMethodCallHandler(Func<MethodCall, Task<object?>>? handler)
    {
        BinaryMessenger.SetMessageHandler(
            Name,
            handler is null ? null : message => HandleAsMethodCall(message, handler));
    }

    /// <summary>
    /// Registers the platform (host) side of this channel. Flutter's platform side lives in the engine,
    /// so it has no Dart equivalent; the reply envelopes follow the same rules as
    /// <see cref="SetMethodCallHandler"/>, and a handler that throws
    /// <see cref="MissingPluginException"/> leaves the method unimplemented.
    /// </summary>
    public void SetPlatformMethodCallHandler(Func<MethodCall, Task<object?>>? handler)
    {
        BinaryMessenger.SetPlatformMessageHandler(
            Name,
            handler is null ? null : message => HandleAsMethodCall(message, handler));
    }

    /// <summary>The shared body of <see cref="InvokeMethod{T}"/> and
    /// <see cref="OptionalMethodChannel.InvokeMethod{T}"/>.</summary>
    protected async Task<T?> InvokeMethodInternal<T>(string method, bool missingOk, object? arguments)
    {
        ArgumentNullException.ThrowIfNull(method);
        ByteData input = Codec.EncodeMethodCall(new MethodCall(method, arguments));
        Task<ByteData?>? send = BinaryMessenger.Send(Name, input);
        ByteData? result = send is null ? null : await send.ConfigureAwait(false);
        if (result is null)
        {
            if (missingOk)
            {
                return default;
            }

            throw new MissingPluginException(
                $"No implementation found for method {method} on channel {Name}");
        }

        return (T?)Codec.DecodeEnvelope(result);
    }

    private async Task<ByteData?> HandleAsMethodCall(ByteData? message, Func<MethodCall, Task<object?>> handler)
    {
        MethodCall call = Codec.DecodeMethodCall(message);
        try
        {
            return Codec.EncodeSuccessEnvelope(await handler(call).ConfigureAwait(false));
        }
        catch (PlatformException exception)
        {
            return Codec.EncodeErrorEnvelope(exception.Code, exception.ErrorMessage, exception.Details);
        }
        catch (MissingPluginException)
        {
            return null;
        }
        catch (Exception error)
        {
            // Dart sends `error.toString()`; .NET's `ToString()` carries the type and the stack trace, so
            // the envelope carries the message instead. See `docs/ai/DIVERGENCES.md`.
            return Codec.EncodeErrorEnvelope("error", error.Message);
        }
    }
}

/// <summary>A <see cref="MethodChannel"/> that ignores missing platform plugins.</summary>
/// <remarks>
/// When invoking methods, unhandled calls are returned as <c>null</c> instead of throwing
/// <see cref="MissingPluginException"/>.
/// </remarks>
public class OptionalMethodChannel : MethodChannel
{
    public OptionalMethodChannel(string name, MethodCodec? codec = null, BinaryMessenger? binaryMessenger = null)
        : base(name, codec, binaryMessenger)
    {
    }

    public override Task<T?> InvokeMethod<T>(string method, object? arguments = null)
        where T : default
    {
        return InvokeMethodInternal<T>(method, missingOk: true, arguments);
    }
}

/// <summary>A named channel for communicating with platform plugins using event streams.</summary>
/// <remarks>
/// Stream setup requests are encoded into binary before being sent, and binary events and errors
/// received are decoded into values. Dart's <c>Stream</c> is an <see cref="IObservable{T}"/> here, the
/// way <c>StreamBuilder</c> already models it.
/// </remarks>
public class EventChannel
{
    private readonly BinaryMessenger? _binaryMessenger;

    /// <summary>Creates an <see cref="EventChannel"/> with the specified <paramref name="name"/>.</summary>
    public EventChannel(string name, MethodCodec? codec = null, BinaryMessenger? binaryMessenger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Codec = codec ?? new StandardMethodCodec();
        _binaryMessenger = binaryMessenger;
    }

    /// <summary>The logical channel on which communication happens, not <c>null</c>.</summary>
    public string Name { get; }

    /// <summary>The message codec used by this channel, not <c>null</c>.</summary>
    public MethodCodec Codec { get; }

    /// <summary>The messenger used by this channel to send binary messages.</summary>
    public BinaryMessenger BinaryMessenger => _binaryMessenger ?? ServicesBinding.Instance.DefaultBinaryMessenger;

    /// <summary>Sets up a broadcast stream for receiving events on this channel.</summary>
    /// <remarks>
    /// The first subscription activates the platform stream with a <c>listen</c> method call; the last
    /// cancellation deactivates it with <c>cancel</c>. Errors reported by the platform surface as
    /// <see cref="PlatformException"/>s on the stream; failures of <c>listen</c>/<c>cancel</c> itself are
    /// reported through <see cref="ServicesDebug"/>, exactly like Flutter's <c>FlutterError.reportError</c>.
    /// </remarks>
    public IObservable<object?> ReceiveBroadcastStream(object? arguments = null)
    {
        var methodChannel = new MethodChannel(Name, Codec);
        var controller = new BroadcastObservable<object?>();
        controller.OnListen = async () =>
        {
            BinaryMessenger.SetMessageHandler(Name, reply =>
            {
                if (reply is null)
                {
                    controller.Close();
                }
                else
                {
                    try
                    {
                        controller.Add(Codec.DecodeEnvelope(reply));
                    }
                    catch (PlatformException exception)
                    {
                        controller.AddError(exception);
                    }
                }

                return null;
            });

            try
            {
                await methodChannel.InvokeMethod<object>("listen", arguments).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ServicesDebug.ReportError(exception, $"while activating platform stream on channel {Name}");
            }
        };

        controller.OnCancel = async () =>
        {
            BinaryMessenger.SetMessageHandler(Name, null);
            try
            {
                await methodChannel.InvokeMethod<object>("cancel", arguments).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ServicesDebug.ReportError(exception, $"while de-activating platform stream on channel {Name}");
            }
        };

        return controller;
    }
}

/// <summary>
/// C#-only helper: Dart's <c>StreamController.broadcast</c>, reduced to what <see cref="EventChannel"/>
/// needs. Unlike the <see cref="IObserver{T}"/> contract, an error does not end the subscription — Dart
/// broadcast streams keep delivering after <c>addError</c>.
/// </summary>
internal sealed class BroadcastObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = [];
    private bool _isClosed;

    public Func<Task>? OnListen { get; set; }

    public Func<Task>? OnCancel { get; set; }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (_isClosed)
        {
            observer.OnCompleted();
            return new Subscription(this, observer);
        }

        _observers.Add(observer);
        if (_observers.Count == 1)
        {
            _ = OnListen?.Invoke();
        }

        return new Subscription(this, observer);
    }

    public void Add(T value)
    {
        foreach (IObserver<T> observer in _observers.ToArray())
        {
            observer.OnNext(value);
        }
    }

    public void AddError(Exception error)
    {
        foreach (IObserver<T> observer in _observers.ToArray())
        {
            observer.OnError(error);
        }
    }

    public void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        foreach (IObserver<T> observer in _observers.ToArray())
        {
            observer.OnCompleted();
        }

        bool hadObservers = _observers.Count > 0;
        _observers.Clear();
        if (hadObservers)
        {
            _ = OnCancel?.Invoke();
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly BroadcastObservable<T> _owner;
        private IObserver<T>? _observer;

        public Subscription(BroadcastObservable<T> owner, IObserver<T> observer)
        {
            _owner = owner;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observer is null)
            {
                return;
            }

            IObserver<T> observer = _observer;
            _observer = null;
            if (_owner._observers.Remove(observer) && _owner._observers.Count == 0)
            {
                _ = _owner.OnCancel?.Invoke();
            }
        }
    }
}
