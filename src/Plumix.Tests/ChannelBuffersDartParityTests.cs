using System.Text;
using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/testing/dart/channel_buffers_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ChannelBuffersDartParityTests : IDisposable
{
    private readonly Action<string> _print = Print.PrintLine;
    private readonly List<string?> _warnings = [];

    public ChannelBuffersDartParityTests()
    {
        EventLoop.FlushMicrotasks();
        Print.PrintLine = _warnings.Add;
    }

    public void Dispose()
    {
        EventLoop.FlushMicrotasks();
        Print.PrintLine = _print;
    }

    [Fact]
    public void DefaultCapacityKeepsNewestAndRepliesNullToDiscardedMessage()
    {
        var buffers = new RecordingBuffers();
        var replies = new List<string?>();
        ByteData newest = Data("two");
        buffers.Push("a", Data("one"), value => replies.Add(Text(value)));
        buffers.Push("a", newest, value => replies.Add(Text(value)));
        Assert.Equal(new string?[] { null }, replies);
        Assert.Equal(Constants.KDebugMode ? 1 : 0, _warnings.Count);
        if (Constants.KDebugMode)
        {
            Assert.Contains("capacity of the a channel is 1 message.", _warnings[0]);
        }
        buffers.SetListener("a", (data, callback) =>
        {
            Assert.Same(newest, data);
            callback(Data("reply"));
        });
        Assert.Equal(new string?[] { null }, replies);
        EventLoop.FlushMicrotasks();
        Assert.Equal(new string?[] { null, "reply" }, replies);
    }

    [Fact]
    public void BufferedDrainUsesOneMicrotaskPerMessageAndLatestListener()
    {
        var buffers = new RecordingBuffers();
        buffers.Resize("a", 3);
        foreach (string message in new[] { "one", "two", "three" })
        {
            buffers.Push("a", Data(message), _ => { });
        }

        var log = new List<string>();
        buffers.SetListener("a", (data, _) =>
        {
            log.Add($"first:{Text(data)}");
            EventLoop.ScheduleMicrotask(() =>
            {
                log.Add("between");
                buffers.SetListener("a", (next, _) => log.Add($"second:{Text(next)}"));
            });
        });
        log.Add("return");
        EventLoop.FlushMicrotasks();
        Assert.Equal(["return", "first:one", "between", "second:two", "second:three"], log);
        buffers.Push("a", Data("four"), _ => { });
        Assert.Equal("second:four", log[^1]);
    }

    [Fact]
    public void RegisteringEvenAnEmptyListenerDefersImmediatePush()
    {
        var buffers = new RecordingBuffers();
        var log = new List<string?>();
        buffers.SetListener("a", (data, _) => log.Add(Text(data)));
        buffers.Push("a", Data("one"), _ => { });
        Assert.Empty(log);
        EventLoop.FlushMicrotasks();
        Assert.Equal(["one"], log);
        buffers.Push("a", Data("two"), _ => { });
        Assert.Equal(["one", "two"], log);
    }

    [Fact]
    public void ClearListenerStopsDrainAndReregistrationResumesFifo()
    {
        var buffers = new RecordingBuffers();
        buffers.Resize("a", 3);
        var log = new List<string?>();
        foreach (string message in new[] { "one", "two", "three" })
        {
            buffers.Push("a", Data(message), _ => { });
        }

        buffers.SetListener("a", (data, _) =>
        {
            log.Add(Text(data));
            buffers.ClearListener("a");
        });
        EventLoop.FlushMicrotasks();
        Assert.Equal(["one"], log);
        buffers.SetListener("a", (data, _) => log.Add(Text(data)));
        buffers.Push("a", Data("four"), _ => { });
        EventLoop.FlushMicrotasks();
        Assert.Equal(["one", "two", "three", "four"], log);
    }

    [Fact]
    public void ResizeImmediatelyDropsOldestAndCapacityZeroDoesNotReplyToNewMessages()
    {
        var buffers = new RecordingBuffers();
        buffers.Resize("a", 3);
        var dropped = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            buffers.Push("a", null, data =>
            {
                Assert.Null(data);
                dropped.Add(index);
            });
        }

        buffers.Resize("a", 1);
        Assert.Equal([0, 1], dropped);
        buffers.Resize("a", 0);
        Assert.Equal([0, 1, 2], dropped);
        buffers.Push("a", null, _ => throw new InvalidOperationException("Must not reply"));
        Assert.Equal(Constants.KDebugMode ? 1 : 0, _warnings.Count);
        buffers.AllowOverflow("a", true);
        buffers.Push("a", null, _ => throw new InvalidOperationException("Must not reply"));
        if (Constants.KDebugMode)
        {
            Assert.Single(_warnings);
        }

        buffers.SetListener("a", (_, _) => throw new InvalidOperationException("Must not drain"));
        EventLoop.FlushMicrotasks();
        Assert.DoesNotContain(buffers.Updates, update => !update.Listening);
    }

    [Fact]
    public void PositiveCapacityOverflowStillWarnsAsInPinnedSource()
    {
        var buffers = new RecordingBuffers();
        buffers.AllowOverflow("a", true);
        buffers.Push("a", null, _ => { });
        buffers.Push("a", null, _ => { });
        Assert.Equal(Constants.KDebugMode ? 1 : 0, _warnings.Count);
    }

    [Fact]
    public void ListenerAndResponseIndependentlyRestoreExecutionContexts()
    {
        var context = new AsyncLocal<string?>();
        var buffers = new RecordingBuffers();
        var log = new List<string?>();
        context.Value = "push";
        buffers.Push("a", null, _ => log.Add(context.Value));
        context.Value = "listener";
        buffers.SetListener("a", (_, callback) =>
        {
            log.Add(context.Value);
            callback(null);
            log.Add(context.Value);
        });
        context.Value = "outside";
        EventLoop.FlushMicrotasks();
        Assert.Equal(["listener", "push", "listener"], log);
        Assert.Equal("outside", context.Value);
    }

    [Fact]
    public void ListenerUpdatesIncludeReplacementAndRepeatedClearButNotAbsentClear()
    {
        var buffers = new RecordingBuffers();
        buffers.ClearListener("a");
        Assert.Empty(buffers.Updates);
        buffers.SetListener("a", (_, _) => { });
        buffers.SetListener("a", (_, _) => { });
        buffers.ClearListener("a");
        buffers.ClearListener("a");
        Assert.Equal([("a", true), ("a", true), ("a", false), ("a", false)], buffers.Updates);
    }

    [Fact]
    public void ControlMessagesHonorByteDataSubviewAndIgnoreTrailingBytes()
    {
        var buffers = new RecordingBuffers();
        var codec = new StandardMethodCodec();
        ByteData resize = codec.EncodeMethodCall(new MethodCall("resize", new object[] { "abcdef", 12345 }));
        byte[] padded = [99, 98, .. resize.ToUint8List(), 97];
        buffers.HandleMessage(ByteData.View(padded, 2, resize.LengthInBytes));
        for (int i = 0; i < 3; i++)
        {
            buffers.Push("abcdef", null, _ => { });
        }

        Assert.Empty(_warnings);
        buffers.HandleMessage(codec.EncodeMethodCall(new MethodCall("resize", new object[] { "abcdef", 0 })));
        buffers.HandleMessage(codec.EncodeMethodCall(new MethodCall("overflow", new object[] { "abcdef", true })));
        buffers.Push("abcdef", null, _ => { });
        Assert.Empty(_warnings);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("resize\ra\rbar")]
    [InlineData("resize\ra")]
    [InlineData("overflow\ra\rtrue")]
    public void MalformedLegacyControlsThrow(string input)
    {
        Assert.ThrowsAny<Exception>(() => new RecordingBuffers().HandleMessage(Data(input)));
    }

    [Fact]
    public void LegacyResizeAndBinaryArgumentValidationMatchTheEngine()
    {
        var buffers = new RecordingBuffers();
        buffers.HandleMessage(Data("resize\ra\r2"));
        buffers.Push("a", null, _ => { });
        buffers.Push("a", null, _ => { });
        Assert.Empty(_warnings);
        var codec = new StandardMethodCodec();
        foreach (var call in new[]
        {
            new MethodCall("unknown"),
            new MethodCall("resize", true),
            new MethodCall("resize", new object[] { "a" }),
            new MethodCall("resize", new object[] { 1, 1 }),
            new MethodCall("resize", new object[] { "a", true }),
            new MethodCall("resize", new object[] { "a\0b", 1 }),
            new MethodCall("resize", new object[] { new string('x', 254), 1 }),
            new MethodCall("overflow", new object[] { "a", 1 }),
        })
        {
            Assert.ThrowsAny<Exception>(() => buffers.HandleMessage(codec.EncodeMethodCall(call)));
        }
    }

    [DebugOnlyFact]
    public void NullCharactersInChannelNamesAssert()
    {
        var buffers = new RecordingBuffers();
        Assert.Contains("U+0000 NULL", Assert.Throws<AssertionError>(
            () => buffers.Push("a\0b", null, _ => { })).Message);
        Assert.Throws<AssertionError>(() => buffers.SetListener("a\0b", (_, _) => { }));
        Assert.Throws<AssertionError>(() => buffers.Resize("a\0b", 1));
    }

    [Fact]
    public void MirroredDemoShowsNewestBufferedMessageThenLiveDeliveryAndStopsListening()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(800, 1200));
        tester.PumpWidget(new MaterialApp(home: new Scaffold(body: new LifecycleUtilitiesDemoPage())));
        tester.Tap(Find.Text("Queue message"));
        tester.Pump();
        tester.Tap(Find.Text("Queue message"));
        tester.Pump();
        Finds.OneWidget(Find.Text("Sent: 2 · Received: none · Listening: false"));
        tester.Tap(Find.Text("Start listening"));
        tester.Pump();
        Finds.OneWidget(Find.Text("Sent: 2 · Received: Message 2 · Listening: true"));
        tester.Tap(Find.Text("Queue message"));
        tester.Pump();
        Finds.OneWidget(Find.Text("Sent: 3 · Received: Message 3 · Listening: true"));
        tester.Tap(Find.Text("Stop listening"));
        tester.Pump();
        tester.Tap(Find.Text("Queue message"));
        tester.Pump();
        Finds.OneWidget(Find.Text("Sent: 4 · Received: Message 3 · Listening: false"));
    }

#pragma warning disable CS0618
    [Fact]
    public void DeprecatedDrainStartsSynchronouslyAndAwaitsEachCallback()
    {
        var buffers = new RecordingBuffers();
        buffers.Resize("a", 2);
        buffers.Push("a", Data("one"), _ => { });
        buffers.Push("a", Data("two"), _ => { });
        var first = new TaskCompletionSource();
        var log = new List<string?>();
        Task drain = Task.CompletedTask;
        EventLoop.RunAsync(() => drain = buffers.Drain("a", (data, _) =>
        {
            log.Add(Text(data));
            return log.Count == 1 ? first.Task : Task.CompletedTask;
        }));
        Assert.Equal(["one"], log);
        Assert.False(drain.IsCompleted);
        first.SetResult();
        EventLoop.FlushMicrotasks();
        Assert.True(drain.IsCompletedSuccessfully);
        Assert.Equal(["one", "two"], log);
        Assert.True(buffers.Drain("absent", (_, _) => throw new Exception()).IsCompletedSuccessfully);
    }
#pragma warning restore CS0618

    private static ByteData Data(string text) => ByteData.SublistView(Encoding.UTF8.GetBytes(text));
    private static string? Text(ByteData? data) => data is null ? null : Encoding.UTF8.GetString(data.ToUint8List());

    private sealed class RecordingBuffers : ChannelBuffers
    {
        public List<(string Name, bool Listening)> Updates { get; } = [];
        public override void SendChannelUpdate(string name, bool listening) => Updates.Add((name, listening));
    }
}
