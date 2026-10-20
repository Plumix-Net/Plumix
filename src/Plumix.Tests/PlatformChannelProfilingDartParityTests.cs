using System.Diagnostics.Tracing;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/services/platform_channel.dart
// Flutter tests: foundation/service_extensions_test.dart (profilePlatformChannels and evict).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlatformChannelProfilingDartParityTests : IDisposable
{
    private readonly Action<TimeSpan, Action> _timer = PlatformChannelProfiling.ScheduleTimer;
    private readonly DebugPrintCallback _print = Print.DebugPrint;
    private readonly List<(TimeSpan Delay, Action Callback)> _timers = [];
    private readonly List<string?> _logs = [];
    private readonly ServicesBinding _services = ServicesBinding.Instance;

    public PlatformChannelProfilingDartParityTests()
    {
        _ = WidgetsBinding.Instance;
        EventLoop.FlushMicrotasks();
        PlatformChannelProfiling.ResetForTests();
        PlatformChannelProfiling.ScheduleTimer = (delay, callback) => _timers.Add((delay, callback));
        Print.DebugPrint = (message, _) => _logs.Add(message);
    }

    public void Dispose()
    {
        EventLoop.FlushMicrotasks();
        PlatformChannelProfiling.ResetForTests();
        PlatformChannelProfiling.ScheduleTimer = _timer;
        Print.DebugPrint = _print;
        ServicesBinding.Instance = _services;
    }

    [NonReleaseFact]
    public void BasicChannelProfilesBytesOncePerWindowAndSortsCombinedTraffic()
    {
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        var messenger = new PlatformBinaryMessenger();
        var small = new BasicMessageChannel<string?>("small", new StringCodec(), messenger);
        var large = new BasicMessageChannel<string?>("large", new StringCodec(), messenger);
        messenger.SetPlatformMessageHandler("small", _ => Task.FromResult(new StringCodec().EncodeMessage("123")));
        messenger.SetPlatformMessageHandler("large", _ => Task.FromResult<ByteData?>(null));

        Assert.Equal("123", small.Send("1").GetAwaiter().GetResult());
        Assert.Equal("123", small.Send("123").GetAwaiter().GetResult());
        Assert.Null(large.Send("123456789012345").GetAwaiter().GetResult());
        Assert.Empty(_logs);
        Assert.Equal(TimeSpan.FromSeconds(1), Assert.Single(_timers).Delay);
        _timers[0].Callback();
        string log = Assert.Single(_logs)!;
        Assert.StartsWith("Platform Channel Stats:\n", log);
        Assert.True(log.IndexOf("name:\"large\"", StringComparison.Ordinal)
            < log.IndexOf("name:\"small\"", StringComparison.Ordinal));
        Assert.Contains("upBytes:4 upBytes_avg:2.0 downBytes:6 downBytes_avg:3.0", log);
        Assert.Contains("upBytes:15 upBytes_avg:15.0 downBytes:0 downBytes_avg:0.0", log);

        small.Send("12").GetAwaiter().GetResult();
        Assert.Equal(2, _timers.Count);
        _timers[1].Callback();
        Assert.Contains("upBytes:2 upBytes_avg:2.0", _logs[1]);
        Assert.DoesNotContain("large", _logs[1]);
    }

    [NonReleaseFact]
    public void MethodNamesAppearInTimelineAndStatsButNeverInTransportNames()
    {
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        var messenger = new PlatformBinaryMessenger();
        var channel = new MethodChannel("methods", binaryMessenger: messenger);
        var calls = new List<string>();
        messenger.SetPlatformMessageHandler("methods", message =>
        {
            calls.Add(channel.Codec.DecodeMethodCall(message).Method);
            return Task.FromResult<ByteData?>(channel.Codec.EncodeSuccessEnvelope(42));
        });
        using var listener = new ChannelTimelineListener();
        Assert.Equal(42, channel.InvokeMethod<int>("answer").GetAwaiter().GetResult());
        Assert.Equal(["answer"], calls);
        Assert.Equal(["Platform Channel send methods#answer"], listener.Begins);
        Assert.Equal(listener.Begins, listener.Ends);
        _timers.Single().Callback();
        Assert.Contains("name:\"methods#answer\" type:\"MethodChannel\" codec:\"StandardMethodCodec\"", _logs[0]);
    }

    [NonReleaseFact]
    public void TransportFailureFinishesTimelineWithoutCountingAResponse()
    {
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        var messenger = new PlatformBinaryMessenger();
        var channel = new BasicMessageChannel<string?>("failed", new StringCodec(), messenger);
        messenger.SetPlatformMessageHandler("failed", _ => Task.FromException<ByteData?>(new Exception("failure")));
        using var listener = new ChannelTimelineListener();
        Assert.Throws<Exception>(() => channel.Send("one").GetAwaiter().GetResult());
        Assert.Equal(listener.Begins, listener.Ends);
        Assert.Single(listener.Ends);
        _timers.Single().Callback();
        Assert.Contains("upBytes:3 upBytes_avg:3.0 downBytes:0 downBytes_avg:NaN", _logs[0]);
    }

    [NonReleaseFact]
    public void FlagsSwitchBetweenOriginalAndPerObjectCachedWrappers()
    {
        var messenger = new PlatformBinaryMessenger();
        var first = new MethodChannel("same", binaryMessenger: messenger);
        var second = new MethodChannel("same", binaryMessenger: messenger);
        var events = new UI.EventChannel("same", binaryMessenger: messenger);
        Assert.False(PlatformChannelProfiling.ShouldProfilePlatformChannels);
        Assert.Same(messenger, first.BinaryMessenger);
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        BinaryMessenger wrapper = first.BinaryMessenger;
        Assert.NotSame(messenger, wrapper);
        Assert.Same(wrapper, first.BinaryMessenger);
        Assert.NotSame(wrapper, second.BinaryMessenger);
        Assert.Same(messenger, events.BinaryMessenger);
        PlatformChannelProfiling.DebugProfilePlatformChannels = false;
        Assert.Same(messenger, first.BinaryMessenger);
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        Assert.Same(wrapper, first.BinaryMessenger);
    }

    [NonReleaseFact]
    public void InboundHandlersAndMissingRepliesDoNotCreateUnrelatedStats()
    {
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        var messenger = new PlatformBinaryMessenger();
        var channel = new BasicMessageChannel<string?>("inbound", new StringCodec(), messenger);
        channel.SetMessageHandler(message => Task.FromResult(message));
        ByteData? reply = null;
        channel.BinaryMessenger.HandlePlatformMessage("inbound", new StringCodec().EncodeMessage("one"),
            response => reply = response);
        Assert.Equal("one", new StringCodec().DecodeMessage(reply));
        Assert.Empty(_timers);
        Assert.Null(channel.Send(null).GetAwaiter().GetResult());
        _timers.Single().Callback();
        Assert.Contains("upBytes:0 upBytes_avg:0.0 downBytes:0 downBytes_avg:0.0", _logs[0]);
    }

    [NonReleaseFact]
    public void CachedDefaultWrapperKeepsItsOriginalProxy()
    {
        var first = new ServicesBinding();
        ServicesBinding.Instance = first;
        PlatformChannelProfiling.DebugProfilePlatformChannels = true;
        var channel = new MethodChannel("cache");
        first.DefaultBinaryMessenger.SetPlatformMessageHandler("cache",
            _ => Task.FromResult<ByteData?>(channel.Codec.EncodeSuccessEnvelope("first")));
        BinaryMessenger wrapper = channel.BinaryMessenger;
        var second = new ServicesBinding();
        ServicesBinding.Instance = second;
        second.DefaultBinaryMessenger.SetPlatformMessageHandler("cache",
            _ => Task.FromResult<ByteData?>(channel.Codec.EncodeSuccessEnvelope("second")));
        Assert.Same(wrapper, channel.BinaryMessenger);
        Assert.Equal("first", channel.InvokeMethod<string>("get").GetAwaiter().GetResult());
        PlatformChannelProfiling.DebugProfilePlatformChannels = false;
        Assert.Equal("second", channel.InvokeMethod<string>("get").GetAwaiter().GetResult());
    }

    [NonReleaseFact]
    public void ProfileExtensionQueriesSetsAndPublishesStateChanges()
    {
        var events = new List<IReadOnlyDictionary<string, object?>>();
        void OnEvent(string name, IReadOnlyDictionary<string, object?> data)
        {
            if (name == "Flutter.ServiceExtensionStateChanged")
            {
                events.Add(data);
            }
        }

        BindingBase.EventPosted += OnEvent;
        try
        {
            ServiceExtensionCallback extension = BindingBase.RegisteredCallbacks["profilePlatformChannels"];
            Assert.Equal("false", extension(new Dictionary<string, string>()).Result["enabled"]);
            Assert.Equal("true", extension(new Dictionary<string, string> { ["enabled"] = "true" }).Result["enabled"]);
            Assert.True(PlatformChannelProfiling.DebugProfilePlatformChannels);
            Assert.Equal("true", extension(new Dictionary<string, string>()).Result["enabled"]);
            Assert.Equal("false",
                extension(new Dictionary<string, string> { ["enabled"] = "false" }).Result["enabled"]);
            Assert.False(PlatformChannelProfiling.DebugProfilePlatformChannels);
            Assert.Equal(2, events.Count);
            Assert.Equal("ext.flutter.profilePlatformChannels", events[0]["extension"]);
            Assert.Equal("true", events[0]["value"]);
            Assert.Equal("false", events[1]["value"]);
        }
        finally
        {
            BindingBase.EventPosted -= OnEvent;
        }
    }

    private sealed class ChannelTimelineListener : EventListener
    {
        public List<string> Begins { get; } = [];
        public List<string> Ends { get; } = [];

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Plumix-Timeline")
            {
                EnableEvents(source, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs args)
        {
            if (args.Payload is { Count: >= 2 }
                && args.Payload[1] is string name && name.StartsWith("Platform Channel send", StringComparison.Ordinal))
            {
                if (args.EventId == TimelineEventSource.AsyncBeginId)
                {
                    Begins.Add(name);
                }
                else if (args.EventId == TimelineEventSource.AsyncEndId)
                {
                    Ends.Add(name);
                }
            }
        }
    }
}
