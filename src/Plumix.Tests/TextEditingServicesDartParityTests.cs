using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/services/scribe_test.dart,
// flutter/packages/flutter/test/services/browser_context_menu_test.dart and the notifier tests of
// flutter/packages/flutter/test/widgets/text_selection_test.dart; `LiveText` and
// `LiveTextInputStatusNotifier` follow services/live_text.dart and widgets/text_selection.dart.
[Collection(SchedulerTestCollection.Name)]
public sealed class TextEditingServicesDartParityTests : IDisposable
{
    private readonly FlutterExceptionHandler? _previousOnError = FlutterError.OnError;
    private readonly List<FlutterErrorDetails> _errors = [];

    public TextEditingServicesDartParityTests()
    {
        Scheduler.ResetForTests();
        FlutterError.OnError = _errors.Add;
    }

    public void Dispose()
    {
        FlutterError.OnError = _previousOnError;
        PlatformDefaults.DebugIsWebOverride = null;
        BrowserContextMenu.ResetForTests();
        Scheduler.ResetForTests();
    }

    // ------------------------------------------------------------------------ Scribe

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScribeIsStylusHandwritingAvailableCallsThroughToPlatformChannel(bool available)
    {
        using var scribe = new MockMethodCallHandler(SystemChannels.Scribe, _ => available);

        Assert.Equal(available, await Scribe.IsStylusHandwritingAvailable());
        Assert.Equal(["Scribe.isStylusHandwritingAvailable"], scribe.Methods);
    }

    [Fact]
    public async Task ScribeIsStylusHandwritingAvailableThrowsWhenThePlatformReturnsNull()
    {
        using var scribe = new MockMethodCallHandler(SystemChannels.Scribe, _ => null);

        await Assert.ThrowsAsync<FlutterError>(Scribe.IsStylusHandwritingAvailable);
        Assert.Equal(["Scribe.isStylusHandwritingAvailable"], scribe.Methods);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScribeIsFeatureAvailableCallsThroughToPlatformChannel(bool available)
    {
        using var scribe = new MockMethodCallHandler(SystemChannels.Scribe, _ => available);

        Assert.Equal(available, await Scribe.IsFeatureAvailable());
        Assert.Equal(["Scribe.isFeatureAvailable"], scribe.Methods);
    }

    [Fact]
    public async Task ScribeIsFeatureAvailableThrowsWhenThePlatformReturnsNull()
    {
        using var scribe = new MockMethodCallHandler(SystemChannels.Scribe, _ => null);

        FlutterError error = await Assert.ThrowsAsync<FlutterError>(Scribe.IsFeatureAvailable);
        Assert.Contains("MethodChannel.invokeMethod unexpectedly returned null.", error.ToString());
        Assert.Equal(["Scribe.isFeatureAvailable"], scribe.Methods);
    }

    [Fact]
    public async Task ScribeStartStylusHandwritingCallsThroughToPlatformChannel()
    {
        using var scribe = new MockMethodCallHandler(SystemChannels.Scribe);

        await Scribe.StartStylusHandwriting();
        MethodCall call = Assert.Single(scribe.Log);
        Assert.Equal("Scribe.startStylusHandwriting", call.Method);
        Assert.Null(call.Arguments);
    }

    // ---------------------------------------------------------------------- LiveText

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    public async Task LiveTextIsLiveTextInputAvailableAsksThePlatformChannel(bool? answer, bool expected)
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform, _ => answer);

        Assert.Equal(expected, await LiveText.IsLiveTextInputAvailable());
        Assert.Equal(["LiveText.isLiveTextInputAvailable"], platform.Methods);
    }

    [Fact]
    public async Task LiveTextStartLiveTextInputUsesTheTextInputChannel()
    {
        using var textInput = new MockMethodCallHandler(SystemChannels.TextInput);

        await LiveText.StartLiveTextInput();
        Assert.Equal(["TextInput.startLiveTextInput"], textInput.Methods);
    }

    // ----------------------------------------------------------- BrowserContextMenu

    [Fact]
    public void BrowserContextMenuAssertsOnNonWebPlatforms()
    {
        PlatformDefaults.DebugIsWebOverride = false;

        // Dart's assert throws synchronously, before any future exists.
        Assert.Throws<AssertionError>(() => { _ = BrowserContextMenu.DisableContextMenu(); });
        Assert.Throws<AssertionError>(() => { _ = BrowserContextMenu.EnableContextMenu(); });
    }

    [Fact]
    public async Task BrowserContextMenuSendsTheMethodsAndTracksTheEnabledFlagOnTheWeb()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var contextMenu = new MockMethodCallHandler(SystemChannels.ContextMenu);
        Assert.True(BrowserContextMenu.Enabled);

        await BrowserContextMenu.DisableContextMenu();
        Assert.False(BrowserContextMenu.Enabled);
        MethodCall disable = Assert.Single(contextMenu.Log);
        Assert.Equal("disableContextMenu", disable.Method);
        Assert.Null(disable.Arguments);

        await BrowserContextMenu.EnableContextMenu();
        Assert.True(BrowserContextMenu.Enabled);
        Assert.Equal(["disableContextMenu", "enableContextMenu"], contextMenu.Methods);
    }

    // ------------------------------------------------------ status notifiers

    [Fact]
    public async Task ClipboardStatusNotifierRecoversFromAClipboardApiFailure()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            _ => throw new PlatformException(code: "ERROR"));
        var notifier = new ClipboardStatusNotifier();
        Assert.Equal(ClipboardStatus.Unknown, notifier.Value);

        await notifier.Update();

        Assert.Equal(ClipboardStatus.Unknown, notifier.Value);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal("widget library", error.Library);
        Assert.Contains("while checking if the clipboard has strings", error.Context!.ToString());
        notifier.Dispose();
    }

    [Fact]
    public async Task ClipboardStatusNotifierUpdateSetsTheValueFromTheClipboardContents()
    {
        using var clipboard = new MockClipboardPlatform();
        var notifier = new ClipboardStatusNotifier();
        Assert.Equal(ClipboardStatus.Unknown, notifier.Value);

        await notifier.Update();
        Assert.Equal(ClipboardStatus.NotPasteable, notifier.Value);

        clipboard.Text = "pasteablestring";
        await notifier.Update();
        Assert.Equal(ClipboardStatus.Pasteable, notifier.Value);
        notifier.Dispose();
    }

    [Theory]
    [InlineData(true, LiveTextInputStatus.Enabled)]
    [InlineData(false, LiveTextInputStatus.Disabled)]
    public async Task LiveTextInputStatusNotifierUpdateAsksLiveText(bool available, LiveTextInputStatus expected)
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform, _ => available);
        var notifier = new LiveTextInputStatusNotifier();
        int notifications = 0;
        notifier.AddListener(() => notifications += 1);
        Scheduler.FlushMicrotasks();

        await notifier.Update();

        Assert.Equal(expected, notifier.Value);
        Assert.Equal(1, notifications);
        Assert.All(platform.Methods, method => Assert.Equal("LiveText.isLiveTextInputAvailable", method));
        notifier.Dispose();
    }

    [Fact]
    public async Task LiveTextInputStatusNotifierReportsAPlatformFailureAndFallsBackToUnknown()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform, _ => true);
        var notifier = new LiveTextInputStatusNotifier();
        await notifier.Update();
        Assert.Equal(LiveTextInputStatus.Enabled, notifier.Value);

        platform.Respond = _ => throw new PlatformException(code: "ERROR");
        await notifier.Update();

        Assert.Equal(LiveTextInputStatus.Unknown, notifier.Value);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal("widget library", error.Library);
        Assert.Contains("while checking the availability of Live Text input", error.Context!.ToString());
        notifier.Dispose();
    }

    [Fact]
    public async Task LiveTextInputStatusNotifierDoesNothingOnceDisposed()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform, _ => true);
        var notifier = new LiveTextInputStatusNotifier();
        notifier.Dispose();

        await notifier.Update();

        Assert.Empty(platform.Log);
    }
}
