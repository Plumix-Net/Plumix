using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Ports Flutter's <c>test/services/platform_channel_test.dart</c>, <c>system_navigator_test.dart</c>
/// and <c>platform_messages_test.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class PlatformChannelTests : IDisposable
{
    private readonly PlatformBinaryMessenger _messenger = new PlatformBinaryMessenger();

    public void Dispose()
    {
        // The event-channel tests drive the ambient messenger, because Flutter's `EventChannel` sends
        // `listen`/`cancel` through the default one even when the channel carries its own.
        ServicesBinding.Instance.DefaultBinaryMessenger.SetPlatformMessageHandler("ch", null);
        ServicesBinding.Instance.DefaultBinaryMessenger.SetMessageHandler("ch", null);
    }

    // -------------------------------------------------------- binary messenger

    [Fact]
    public void BinaryMessenger_DeliversToThePlatformHandlerAndBackToTheCaller()
    {
        var log = new List<ByteData?>();
        _messenger.SetPlatformMessageHandler("test1", message =>
        {
            log.Add(message);
            return Task.FromResult<ByteData?>(null);
        });

        ByteData message = new StringCodec().EncodeMessage("hello")!;
        Assert.Null(_messenger.Send("test1", message)!.Result);
        Assert.Equal([message], log);

        _messenger.SetPlatformMessageHandler("test1", null);
        log.Clear();
        Assert.Null(_messenger.Send("test1", message)!.Result);
        Assert.Empty(log);
    }

    [Fact]
    public void BinaryMessenger_AnswersAnInboundMessageThroughTheResponseCallback()
    {
        var codec = new StringCodec();
        _messenger.SetMessageHandler("test1", message => Task.FromResult(codec.EncodeMessage("reply")));

        ByteData? reply = null;
        _messenger.HandlePlatformMessage("test1", codec.EncodeMessage("hello"), data => reply = data);
        Assert.Equal("reply", codec.DecodeMessage(reply));

        reply = codec.EncodeMessage("stale");
        _messenger.HandlePlatformMessage("missing", null, data => reply = data);
        Assert.Null(reply);
    }

    // ---------------------------------------------------- BasicMessageChannel

    [Fact]
    public void BasicMessageChannel_SendsAStringMessageAndGetsAReply()
    {
        var channel = new BasicMessageChannel<string?>("ch", new StringCodec(), _messenger);
        var codec = new StringCodec();
        _messenger.SetPlatformMessageHandler(
            "ch",
            message => Task.FromResult(codec.EncodeMessage($"{codec.DecodeMessage(message)} world")));

        Assert.Equal("hello world", channel.Send("hello").Result);
    }

    [Fact]
    public void BasicMessageChannel_ReceivesAStringMessageAndSendsAReply()
    {
        var channel = new BasicMessageChannel<string?>("ch", new StringCodec(), _messenger);
        channel.SetMessageHandler(message => Task.FromResult<string?>($"{message} world"));

        var codec = new StringCodec();
        ByteData? reply = null;
        _messenger.HandlePlatformMessage("ch", codec.EncodeMessage("hello"), data => reply = data);

        Assert.Equal("hello world", codec.DecodeMessage(reply));
    }

    [Fact]
    public void BasicMessageChannel_SendsNullWhenNoPlatformHandlerIsRegistered()
    {
        var channel = new BasicMessageChannel<string?>("ch", new StringCodec(), _messenger);
        Assert.Null(channel.Send("hello").Result);
    }

    // ----------------------------------------------------------- MethodChannel

    [Fact]
    public void MethodChannel_InvokesAMethodAndGetsTheResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        RespondWith(channel, call => $"{call.Arguments} world");

        Assert.Equal("hello world", channel.InvokeMethod<string>("sayHello", "hello").Result);
    }

    [Fact]
    public void MethodChannel_InvokesAListMethodAndGetsTheResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        RespondWith(channel, _ => new List<object?> { "hello", "world" });

        Assert.Equal(["hello", "world"], channel.InvokeListMethod<string>("sayHello").Result!);
    }

    [Fact]
    public void MethodChannel_InvokesAListMethodAndGetsANullResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        RespondWith(channel, _ => null);

        Assert.Null(channel.InvokeListMethod<string>("sayHello").Result);
    }

    [Fact]
    public void MethodChannel_InvokesAMapMethodAndGetsTheResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        RespondWith(channel, _ => new Dictionary<string, object?> { ["hello"] = "world" });

        Dictionary<string, string> result = channel.InvokeMapMethod<string, string>("sayHello").Result!;
        Assert.Equal("world", result["hello"]);
    }

    [Fact]
    public void MethodChannel_InvokesAMapMethodAndGetsANullResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        RespondWith(channel, _ => null);

        Assert.Null(channel.InvokeMapMethod<string, string>("sayHello").Result);
    }

    [Fact]
    public void MethodChannel_InvokesAMethodAndGetsAnError()
    {
        MethodChannel channel = JsonChannel("ch7");
        _messenger.SetPlatformMessageHandler("ch7", _ => Task.FromResult<ByteData?>(
            new JsonMethodCodec().EncodeErrorEnvelope(
                "bad",
                "Something happened",
                new Dictionary<string, object?> { ["a"] = 42, ["b"] = 3.14 })));

        var exception = Assert.Throws<PlatformException>(
            () => Unwrap(channel.InvokeMethod<string>("sayHello", "hello")));

        Assert.Equal("bad", exception.Code);
        Assert.Equal("Something happened", exception.ErrorMessage);
        var details = (IDictionary<string, object?>)exception.Details!;
        Assert.Equal(42, details["a"]);
        Assert.Equal(3.14, details["b"]);
    }

    [Fact]
    public void MethodChannel_InvokesAnUnimplementedMethod()
    {
        MethodChannel channel = JsonChannel("ch7");

        var exception = Assert.Throws<MissingPluginException>(
            () => Unwrap(channel.InvokeMethod<string>("sayHello", "hello")));

        Assert.Contains("sayHello", exception.ErrorMessage);
        Assert.Contains("ch7", exception.ErrorMessage);
    }

    [Fact]
    public void OptionalMethodChannel_InvokesAnUnimplementedMethodAsNull()
    {
        var channel = new OptionalMethodChannel("ch8", new JsonMethodCodec(), _messenger);
        Assert.Null(channel.InvokeMethod<string>("sayHello", "hello").Result);
    }

    [Fact]
    public void MethodChannel_HandlesAMethodCallWithNoRegisteredPlugin()
    {
        MethodChannel channel = JsonChannel("ch7");
        channel.SetMethodCallHandler(null);

        Assert.Null(IncomingCall(channel, new MethodCall("sayHello", "hello")));
    }

    [Fact]
    public void MethodChannel_HandlesAMethodCallOfAnUnimplementedMethod()
    {
        MethodChannel channel = JsonChannel("ch7");
        channel.SetMethodCallHandler(_ => throw new MissingPluginException());

        Assert.Null(IncomingCall(channel, new MethodCall("sayHello", "hello")));
    }

    [Fact]
    public void MethodChannel_HandlesAMethodCallWithASuccessfulResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        channel.SetMethodCallHandler(call => Task.FromResult<object?>($"{call.Arguments}, world"));

        ByteData? envelope = IncomingCall(channel, new MethodCall("sayHello", "hello"));
        Assert.Equal("hello, world", channel.Codec.DecodeEnvelope(envelope!));
    }

    [Fact]
    public void MethodChannel_HandlesAMethodCallWithAnExpressiveErrorResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        channel.SetMethodCallHandler(_ => throw new PlatformException("bad", "sayHello failed"));

        ByteData? envelope = IncomingCall(channel, new MethodCall("sayHello", "hello"));
        var exception = Assert.Throws<PlatformException>(() => channel.Codec.DecodeEnvelope(envelope!));
        Assert.Equal("bad", exception.Code);
        Assert.Equal("sayHello failed", exception.ErrorMessage);
    }

    [Fact]
    public void MethodChannel_HandlesAMethodCallWithAnotherErrorResult()
    {
        MethodChannel channel = JsonChannel("ch7");
        channel.SetMethodCallHandler(_ => throw new InvalidOperationException("bad"));

        ByteData? envelope = IncomingCall(channel, new MethodCall("sayHello", "hello"));
        var exception = Assert.Throws<PlatformException>(() => channel.Codec.DecodeEnvelope(envelope!));
        Assert.Equal("error", exception.Code);
        Assert.Equal("bad", exception.ErrorMessage);
    }

    // ------------------------------------------------------------ EventChannel

    [Fact]
    public void EventChannel_ReceivesAnEventStream()
    {
        var codec = new JsonMethodCodec();
        var channel = new EventChannel("ch", codec);
        var log = new List<string>();
        bool canceled = false;

        ServicesBinding.Instance.DefaultBinaryMessenger.SetPlatformMessageHandler("ch", message =>
        {
            MethodCall call = codec.DecodeMethodCall(message);
            if (call.Method == "listen")
            {
                Emit(codec.EncodeSuccessEnvelope($"{call.Arguments}1"));
                Emit(codec.EncodeSuccessEnvelope($"{call.Arguments}2"));
                Emit(null);
            }
            else
            {
                canceled = true;
            }

            return Task.FromResult<ByteData?>(codec.EncodeSuccessEnvelope(null));
        });

        var observer = new RecordingObserver();
        using (channel.ReceiveBroadcastStream("hello").Subscribe(observer))
        {
            log.AddRange(observer.Values.ConvertAll(value => (string)value!));
        }

        Assert.Equal(["hello1", "hello2"], log);
        Assert.True(observer.IsCompleted);
        Assert.True(canceled);
    }

    [Fact]
    public void EventChannel_ReceivesAnErrorEvent()
    {
        var codec = new JsonMethodCodec();
        var channel = new EventChannel("ch", codec);

        ServicesBinding.Instance.DefaultBinaryMessenger.SetPlatformMessageHandler("ch", message =>
        {
            MethodCall call = codec.DecodeMethodCall(message);
            if (call.Method == "listen")
            {
                Emit(codec.EncodeErrorEnvelope("404", "Not Found.", call.Arguments));
            }

            return Task.FromResult<ByteData?>(codec.EncodeSuccessEnvelope(null));
        });

        var observer = new RecordingObserver();
        using IDisposable subscription = channel.ReceiveBroadcastStream("hello").Subscribe(observer);

        Assert.Empty(observer.Values);
        Exception error = Assert.Single(observer.Errors);
        var exception = Assert.IsType<PlatformException>(error);
        Assert.Equal("404", exception.Code);
        Assert.Equal("Not Found.", exception.ErrorMessage);
        Assert.Equal("hello", exception.Details);
    }

    // -------------------------------------------------------- SystemNavigator

    [Fact]
    public void SystemNavigator_SendsPlatformMessages()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        SystemNavigator.Pop().Wait();

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("SystemNavigator.pop", call.Method);
        Assert.Null(call.Arguments);
    }

    [Fact]
    public void SystemNavigator_SendsNavigationMessages()
    {
        using var navigation = new MockMethodCallHandler(SystemChannels.Navigation);

        SystemNavigator.SelectSingleEntryHistory().Wait();
        Assert.Equal("selectSingleEntryHistory", Assert.Single(navigation.Log).Method);
        Assert.Null(navigation.Log[0].Arguments);
        navigation.Log.Clear();

        SystemNavigator.SelectMultiEntryHistory().Wait();
        Assert.Equal("selectMultiEntryHistory", Assert.Single(navigation.Log).Method);
        navigation.Log.Clear();

        SystemNavigator.RouteInformationUpdated(new Uri("a", UriKind.Relative)).Wait();
        AssertRouteInformation(navigation.Log, uri: "a", state: null, replace: false);
        navigation.Log.Clear();

        SystemNavigator.RouteInformationUpdated(new Uri("a", UriKind.Relative), state: true).Wait();
        AssertRouteInformation(navigation.Log, uri: "a", state: true, replace: false);
        navigation.Log.Clear();

        SystemNavigator
            .RouteInformationUpdated(new Uri("a", UriKind.Relative), state: true, replace: true)
            .Wait();
        AssertRouteInformation(navigation.Log, uri: "a", state: true, replace: true);
    }

    [Fact]
    public void SystemSoundAndHapticFeedback_SendPlatformMessages()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        SystemSound.Play(SystemSoundType.Alert).Wait();
        HapticFeedback.Vibrate().Wait();
        HapticFeedback.SelectionClick().Wait();

        Assert.Equal(
            ["SystemSound.play", "HapticFeedback.vibrate", "HapticFeedback.vibrate"],
            platform.Methods);
        Assert.Equal("SystemSoundType.alert", platform.Log[0].Arguments);
        Assert.Null(platform.Log[1].Arguments);
        Assert.Equal("HapticFeedbackType.selectionClick", platform.Log[2].Arguments);
    }

    // ------------------------------------------------- inbound navigation channel

    [Fact]
    public void WidgetsBinding_HandlesInboundNavigationCalls()
    {
        var observer = new RouteObserver();
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            MethodCodec codec = SystemChannels.Navigation.Codec;

            Assert.Equal(true, Inbound(codec, new MethodCall("popRoute")));
            Assert.Equal(["pop"], observer.Log);

            Assert.Equal(true, Inbound(codec, new MethodCall("pushRoute", "/deep")));
            Assert.Equal(["pop", "push:/deep:"], observer.Log);

            object? pushed = Inbound(codec, new MethodCall("pushRouteInformation", new Dictionary<string, object?>
            {
                ["location"] = "/linked",
                ["state"] = "carried",
            }));

            Assert.Equal(true, pushed);
            Assert.Equal(["pop", "push:/deep:", "push:/linked:carried"], observer.Log);

            // An unknown method is answered with null, the way Flutter's switch default does.
            Assert.Null(Inbound(codec, new MethodCall("unknown")));
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    // --------------------------------------------------------- SystemChannels

    [Fact]
    public void SystemChannels_CarryFlutterNamesAndCodecs()
    {
        Assert.Equal("flutter/navigation", SystemChannels.Navigation.Name);
        Assert.IsType<JsonMethodCodec>(SystemChannels.Navigation.Codec);
        Assert.IsType<OptionalMethodChannel>(SystemChannels.Navigation);

        Assert.Equal("flutter/platform", SystemChannels.Platform.Name);
        Assert.IsType<JsonMethodCodec>(SystemChannels.Platform.Codec);
        Assert.IsType<OptionalMethodChannel>(SystemChannels.Platform);

        Assert.Equal("flutter/restoration", SystemChannels.Restoration.Name);
        Assert.IsType<StandardMethodCodec>(SystemChannels.Restoration.Codec);
        Assert.IsType<OptionalMethodChannel>(SystemChannels.Restoration);

        Assert.Equal("flutter/backgesture", SystemChannels.BackGesture.Name);
        Assert.Equal("flutter/textinput", SystemChannels.TextInput.Name);
        Assert.Equal("flutter/keyevent", SystemChannels.KeyEvent.Name);
        Assert.IsType<JsonMessageCodec>(SystemChannels.KeyEvent.Codec);
        Assert.Equal("flutter/lifecycle", SystemChannels.Lifecycle.Name);
        Assert.IsType<StringCodec>(SystemChannels.Lifecycle.Codec);
        Assert.Equal("flutter/accessibility", SystemChannels.Accessibility.Name);
        Assert.IsType<StandardMessageCodec>(SystemChannels.Accessibility.Codec);
        Assert.Equal("flutter/system", SystemChannels.System.Name);
        Assert.Equal("flutter/mousecursor", SystemChannels.MouseCursor.Name);
        Assert.Equal("flutter/menu", SystemChannels.Menu.Name);

        // Flutter keeps these three non-optional, so an unimplemented method throws.
        Assert.IsNotType<OptionalMethodChannel>(SystemChannels.PlatformViews);
        Assert.IsNotType<OptionalMethodChannel>(SystemChannels.PlatformViews2);
        Assert.IsNotType<OptionalMethodChannel>(SystemChannels.Skia);
    }

    // ------------------------------------------------------------------ helpers

    private static void AssertRouteInformation(List<MethodCall> log, string uri, object? state, bool replace)
    {
        MethodCall call = Assert.Single(log);
        Assert.Equal("routeInformationUpdated", call.Method);
        var arguments = (IDictionary<string, object?>)call.Arguments!;
        Assert.Equal(uri, arguments["uri"]);
        Assert.Equal(state, arguments["state"]);
        Assert.Equal(replace, arguments["replace"]);
    }

    private static void Emit(ByteData? envelope)
    {
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage("ch", envelope, null);
    }

    private static void Unwrap(Task task)
    {
        try
        {
            task.Wait();
        }
        catch (AggregateException exception)
        {
            throw exception.InnerException!;
        }
    }

    private MethodChannel JsonChannel(string name) => new MethodChannel(name, new JsonMethodCodec(), _messenger);

    private void RespondWith(MethodChannel channel, Func<MethodCall, object?> respond)
    {
        _messenger.SetPlatformMessageHandler(channel.Name, message => Task.FromResult<ByteData?>(
            channel.Codec.EncodeSuccessEnvelope(respond(channel.Codec.DecodeMethodCall(message)))));
    }

    private ByteData? IncomingCall(MethodChannel channel, MethodCall call)
    {
        ByteData? reply = null;
        _messenger.HandlePlatformMessage(channel.Name, channel.Codec.EncodeMethodCall(call), data => reply = data);
        return reply;
    }

    private static object? Inbound(MethodCodec codec, MethodCall call)
    {
        ByteData? reply = null;
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.Navigation.Name,
            codec.EncodeMethodCall(call),
            data => reply = data);
        return codec.DecodeEnvelope(reply!);
    }

    private sealed class RouteObserver : WidgetsBindingObserver
    {
        public List<string> Log { get; } = [];

        public Task<bool> DidPopRoute()
        {
            Log.Add("pop");
            return Task.FromResult(true);
        }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            Log.Add($"push:{routeInformation.Uri}:{routeInformation.State}");
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingObserver : IObserver<object?>
    {
        public List<object?> Values { get; } = [];

        public List<Exception> Errors { get; } = [];

        public bool IsCompleted { get; private set; }

        public void OnCompleted() => IsCompleted = true;

        public void OnError(Exception error) => Errors.Add(error);

        public void OnNext(object? value) => Values.Add(value);
    }
}
