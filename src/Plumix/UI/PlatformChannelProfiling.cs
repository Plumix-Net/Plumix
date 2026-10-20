using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Plumix.Developer;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/platform_channel.dart

namespace Plumix.UI;

public static class PlatformChannelProfiling
{
    public const bool KProfilePlatformChannels = false;
    public static bool DebugProfilePlatformChannels { get; set; }
    public static bool ShouldProfilePlatformChannels =>
        KProfilePlatformChannels || (!Constants.KReleaseMode && DebugProfilePlatformChannels);

    private static readonly ConditionalWeakTable<object, ProfiledBinaryMessenger> Messengers = new();
    private static readonly Dictionary<string, PlatformChannelStats> Stats = [];
    private static bool _running;
    private static int _generation;

    internal static Action<TimeSpan, Action> ScheduleTimer { get; set; } = DefaultScheduleTimer;

    internal static BinaryMessenger Wrap(object channel, BinaryMessenger proxy, object codec) =>
        ShouldProfilePlatformChannels
            ? Messengers.GetValue(channel, key => new ProfiledBinaryMessenger(
                proxy, TypeName(key.GetType()), TypeName(codec.GetType())))
            : proxy;

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        return type.Name.Split('`')[0] + "<"
            + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
    }

    private static void Record(string type, string channel, string codec, ByteData? bytes, bool upstream)
    {
        if (!Stats.TryGetValue(channel, out PlatformChannelStats? stats))
        {
            Stats[channel] = stats = new PlatformChannelStats(channel, codec, type);
        }

        if (upstream)
        {
            stats.AddUpStream(bytes?.LengthInBytes ?? 0);
        }
        else
        {
            stats.AddDownStream(bytes?.LengthInBytes ?? 0);
        }

        if (!_running)
        {
            _running = true;
            int generation = _generation;
            ScheduleTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (generation == _generation)
                {
                    PrintStats();
                }
            });
        }
    }

    private static void PrintStats()
    {
        _running = false;
        var log = new StringBuilder("Platform Channel Stats:\n");
        foreach (PlatformChannelStats stats in Stats.Values.OrderByDescending(s => s.UpBytes + s.DownBytes))
        {
            log.Append("  (name:\"").Append(stats.Channel).Append("\" type:\"").Append(stats.Type)
                .Append("\" codec:\"").Append(stats.Codec).Append("\" upBytes:").Append(stats.UpBytes)
                .Append(" upBytes_avg:").Append(stats.AverageUpPayload.ToString("F1", CultureInfo.InvariantCulture))
                .Append(" downBytes:").Append(stats.DownBytes)
                .Append(" downBytes_avg:").Append(stats.AverageDownPayload.ToString("F1", CultureInfo.InvariantCulture))
                .Append(")\n");
        }

        Print.DebugPrint(log.ToString());
        Stats.Clear();
    }

    private static void DefaultScheduleTimer(TimeSpan delay, Action callback)
    {
        // A Dart timer returns to the isolate. Delay on the pool, then return through the event loop.
        _ = Task.Delay(delay).ContinueWith(
            _ => EventLoop.ScheduleMicrotask(callback), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    internal static void ResetForTests()
    {
        DebugProfilePlatformChannels = false;
        Stats.Clear();
        Messengers.Clear();
        _running = false;
        _generation++;
    }

    private sealed class PlatformChannelStats(string channel, string codec, string type)
    {
        public string Channel { get; } = channel;
        public string Codec { get; } = codec;
        public string Type { get; } = type;
        public long UpBytes { get; private set; }
        public long DownBytes { get; private set; }
        private int _upCount;
        private int _downCount;
        public double AverageUpPayload => (double)UpBytes / _upCount;
        public double AverageDownPayload => (double)DownBytes / _downCount;

        public void AddUpStream(int bytes)
        {
            _upCount++;
            UpBytes += bytes;
        }

        public void AddDownStream(int bytes)
        {
            _downCount++;
            DownBytes += bytes;
        }
    }

    internal sealed class ProfiledBinaryMessenger(
        BinaryMessenger proxy, string channelTypeName, string codecTypeName) : BinaryMessenger
    {
        public override Task HandlePlatformMessage(
            string channel, ByteData? data, PlatformMessageResponseCallback? callback) =>
            proxy.HandlePlatformMessage(channel, data, callback);

        public async Task<ByteData?> SendWithPostfix(string channel, string postfix, ByteData? message)
        {
            string name = channel + postfix;
            Record(channelTypeName, name, codecTypeName, message, upstream: true);
            var timeline = new TimelineTask();
            timeline.Start($"Platform Channel send {name}");
            ByteData? result;
            try
            {
                Task<ByteData?>? reply = proxy.Send(channel, message);
                result = reply is null ? null : await reply;
            }
            finally
            {
                timeline.Finish();
            }

            Record(channelTypeName, name, codecTypeName, result, upstream: false);
            return result;
        }

        public override Task<ByteData?> Send(string channel, ByteData? message) =>
            SendWithPostfix(channel, string.Empty, message);

        public override void SetMessageHandler(string channel, MessageHandler? handler) =>
            proxy.SetMessageHandler(channel, handler);

        public override void SetPlatformMessageHandler(string channel, MessageHandler? handler) =>
            proxy.SetPlatformMessageHandler(channel, handler);
    }
}
