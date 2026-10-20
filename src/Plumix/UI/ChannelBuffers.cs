using System.Globalization;
using System.Text;
using Plumix.Foundation;

// Dart parity source: flutter/engine/src/flutter/lib/ui/channel_buffers.dart

namespace Plumix.UI;

public delegate void ChannelCallback(ByteData? data, PlatformMessageResponseCallback callback);

[Obsolete("Migrate to ChannelCallback instead.")]
public delegate Task DrainChannelCallback(ByteData? data, PlatformMessageResponseCallback callback);

/// <summary>Buffers platform messages until the framework registers a listener.</summary>
public class ChannelBuffers
{
    public const int KDefaultBufferSize = 1;
    public const string KControlChannelName = "dev.flutter/channel-buffers";

    private readonly Dictionary<string, Channel> _channels = [];
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public void Push(string name, ByteData? data, PlatformMessageResponseCallback callback)
    {
        CheckName(name);
        ArgumentNullException.ThrowIfNull(callback);
        Channel channel = GetChannel(name);
        if (channel.Push(new StoredMessage(data, callback)) && Constants.KDebugMode)
        {
            Print.PrintLine(
                $"A message on the {name} channel was discarded before it could be handled.\n"
                + "This happens when a plugin sends messages to the framework side before the "
                + "framework has had an opportunity to register a listener. See the ChannelBuffers "
                + "API documentation for details on how to configure the channel to expect more "
                + "messages, or to expect messages to get discarded:\n"
                + "  https://api.flutter.dev/flutter/dart-ui/ChannelBuffers-class.html\n"
                + $"The capacity of the {name} channel is {channel.Capacity} "
                + $"message{(channel.Capacity != 1 ? "s" : string.Empty)}.");
        }
    }

    public void SetListener(string name, ChannelCallback callback)
    {
        CheckName(name);
        ArgumentNullException.ThrowIfNull(callback);
        GetChannel(name).SetListener(callback);
        SendChannelUpdate(name, listening: true);
    }

    public void ClearListener(string name)
    {
        if (_channels.TryGetValue(name, out Channel? channel))
        {
            channel.ClearListener();
            SendChannelUpdate(name, listening: false);
        }
    }

    /// <summary>Notifies the embedder when a channel gains or loses a listener.</summary>
    public virtual void SendChannelUpdate(string name, bool listening) =>
        PlatformDispatcher.Instance.SendChannelUpdate(name, listening);

    [Obsolete("Migrate to SetListener instead.")]
    public async Task Drain(string name, DrainChannelCallback callback)
    {
        if (_channels.TryGetValue(name, out Channel? channel))
        {
            while (channel.Length != 0)
            {
                StoredMessage message = channel.Pop();
                await callback(message.Data, message.Invoke);
            }
        }
    }

    /// <summary>Handles the engine's StandardMethodCodec and legacy resize control messages.</summary>
    public void HandleMessage(ByteData data)
    {
        ReadOnlySpan<byte> bytes = data.Buffer.AsSpan(data.OffsetInBytes, data.LengthInBytes);
        if (bytes[0] != 0x07)
        {
            string[] parts = Utf8.GetString(bytes).Split('\r');
            if (parts.Length == 3 && parts[0] == "resize")
            {
                Resize(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture));
                return;
            }

            throw new ArgumentException($"Unrecognized message [{string.Join(", ", parts)}] "
                + $"sent to {KControlChannelName}.");
        }

        int methodNameLength = bytes[1];
        if (methodNameLength >= 254)
        {
            throw new ArgumentException($"Unrecognized message sent to {KControlChannelName} (method name too long)");
        }

        int index = 2;
        string methodName = Utf8.GetString(bytes.Slice(index, methodNameLength));
        index += methodNameLength;
        if (methodName is not ("resize" or "overflow"))
        {
            throw new ArgumentException($"Unrecognized method '{methodName}' sent to {KControlChannelName}");
        }

        string description = methodName == "resize" ? "new capacity" : "flag state";
        if (bytes[index++] != 0x0C || bytes[index++] < 0x02)
        {
            throw InvalidArguments(methodName,
                $"arguments must be a two-element list, channel name and {description}");
        }

        if (bytes[index++] != 0x07)
        {
            throw InvalidArguments(methodName, "first argument must be a string");
        }

        int channelNameLength = bytes[index++];
        if (channelNameLength >= 254)
        {
            throw InvalidArguments(methodName, "channel name must be less than 254 characters long");
        }

        string channelName = Utf8.GetString(bytes.Slice(index, channelNameLength));
        index += channelNameLength;
        if (methodName == "resize")
        {
            if (channelName.Contains('\0'))
            {
                throw InvalidArguments(methodName, "channel name must not contain any null bytes");
            }

            if (bytes[index++] != 0x03)
            {
                throw InvalidArguments(methodName, "second argument must be an integer in the range 0 to 2147483647");
            }

            Resize(channelName, data.GetUint32(index, Endian.Host));
        }
        else
        {
            if (bytes[index] is not (0x01 or 0x02))
            {
                throw InvalidArguments(methodName, "second argument must be a boolean");
            }

            AllowOverflow(channelName, bytes[index] == 0x01);
        }
    }

    public void Resize(string name, long newSize)
    {
        if (_channels.TryGetValue(name, out Channel? channel))
        {
            channel.Capacity = newSize;
        }
        else
        {
            CheckName(name);
            _channels[name] = new Channel(newSize);
        }
    }

    public void AllowOverflow(string name, bool allowed)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        _channels.TryGetValue(name, out Channel? channel);
        if (channel is null && allowed)
        {
            CheckName(name);
            _channels[name] = channel = new Channel();
        }

        if (channel is not null)
        {
            channel.DebugEnableDiscardWarnings = !allowed;
        }
    }

    internal bool HasListener(string name) =>
        _channels.TryGetValue(name, out Channel? channel) && channel.HasListener;

    private Channel GetChannel(string name)
    {
        if (!_channels.TryGetValue(name, out Channel? channel))
        {
            _channels[name] = channel = new Channel();
        }

        return channel;
    }

    private static void CheckName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Constants.KDebugMode && name.Contains('\0'))
        {
            throw new AssertionError("Channel names must not contain U+0000 NULL characters.");
        }
    }

    private static ArgumentException InvalidArguments(string method, string reason) =>
        new($"Invalid arguments for '{method}' method sent to {KControlChannelName} ({reason})");

    // ExecutionContext carries AsyncLocal values like Dart's Zone. The framework event loop owns
    // scheduling; capture each response separately from the listener's registration context.
    private sealed class CallbackRecord(ChannelCallback callback)
    {
        private readonly ExecutionContext? _context = ExecutionContext.Capture();

        public void Invoke(ByteData? data, PlatformMessageResponseCallback response)
        {
            if (_context is null)
            {
                callback(data, response);
            }
            else
            {
                ExecutionContext.Run(_context, _ => callback(data, response), null);
            }
        }
    }

    private sealed class StoredMessage(ByteData? data, PlatformMessageResponseCallback callback)
    {
        private readonly ExecutionContext? _context = ExecutionContext.Capture();
        public ByteData? Data { get; } = data;

        public void Invoke(ByteData? reply)
        {
            if (_context is null)
            {
                callback(reply);
            }
            else
            {
                ExecutionContext.Run(_context, _ => callback(reply), null);
            }
        }
    }

    private sealed class Channel(long capacity = KDefaultBufferSize)
    {
        private readonly Queue<StoredMessage> _queue = [];
        private long _capacity = capacity;
        private bool _draining;
        private CallbackRecord? _listener;

        public int Length => _queue.Count;
        public bool HasListener => _listener is not null;
        public bool DebugEnableDiscardWarnings { get; set; } = true;

        public long Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                DropOverflowMessages(value);
            }
        }

        public bool Push(StoredMessage message)
        {
            if (!_draining && _listener is not null)
            {
                _listener.Invoke(message.Data, message.Invoke);
                return false;
            }

            if (_capacity <= 0)
            {
                return DebugEnableDiscardWarnings;
            }

            bool result = DropOverflowMessages(_capacity - 1);
            _queue.Enqueue(message);
            return result;
        }

        public StoredMessage Pop() => _queue.Dequeue();

        private bool DropOverflowMessages(long limit)
        {
            bool result = false;
            while (_queue.Count > limit)
            {
                _queue.Dequeue().Invoke(null);
                result = true;
            }

            return result;
        }

        public void SetListener(ChannelCallback callback)
        {
            bool needDrain = _listener is null;
            _listener = new CallbackRecord(callback);
            if (needDrain && !_draining)
            {
                _draining = true;
                EventLoop.ScheduleMicrotask(DrainStep);
            }
        }

        public void ClearListener() => _listener = null;

        private void DrainStep()
        {
            if (_queue.Count != 0 && _listener is not null)
            {
                StoredMessage message = Pop();
                _listener.Invoke(message.Data, message.Invoke);
                EventLoop.ScheduleMicrotask(DrainStep);
            }
            else
            {
                _draining = false;
            }
        }
    }
}
