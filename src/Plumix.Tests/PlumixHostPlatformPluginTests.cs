using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only host tests: the platform side of `flutter/platform` that the engine's platform plugins answer
// (PlatformPlugin.java, FlutterPlatformPlugin.mm, the desktop platform handlers) and the accessibility
// feature bits an embedder reports, dispatched to the hooks a platform host installs.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlumixHostPlatformPluginTests : IDisposable
{
    private readonly int _originalFeatures = PlatformDispatcher.Instance.ReportedAccessibilityFeatures;

    public PlumixHostPlatformPluginTests() => PlumixHost.ResetFrameworkHandlesBackForTests();

    public void Dispose()
    {
        PlumixHost.ResetFrameworkHandlesBackForTests();
        PlatformDispatcher.Instance.UpdateAccessibilityFeatures(_originalFeatures);
    }

    [Fact]
    public void SystemSoundPlay_ReachesTheInstalledHandler()
    {
        var sounds = new List<SystemSoundType>();
        PlumixHost.SystemSoundHandler = sounds.Add;
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        try
        {
            SystemSound.Play(SystemSoundType.Click).GetAwaiter().GetResult();
            SystemSound.Play(SystemSoundType.Tick).GetAwaiter().GetResult();
            SystemSound.Play(SystemSoundType.Alert).GetAwaiter().GetResult();

            Assert.Equal([SystemSoundType.Click, SystemSoundType.Tick, SystemSoundType.Alert], sounds);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    [Fact]
    public void HapticFeedbackVibrate_ReachesTheInstalledHandlerWithItsType()
    {
        var types = new List<string?>();
        PlumixHost.HapticFeedbackHandler = types.Add;
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        try
        {
            HapticFeedback.Vibrate().GetAwaiter().GetResult();
            HapticFeedback.HeavyImpact().GetAwaiter().GetResult();
            HapticFeedback.SelectionClick().GetAwaiter().GetResult();

            Assert.Equal(
                [null, "HapticFeedbackType.heavyImpact", "HapticFeedbackType.selectionClick"],
                types);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    [Fact]
    public void SetSystemUIOverlayStyle_ReachesTheInstalledHandlerWithTheDecodedStyle()
    {
        var styles = new List<SystemUiOverlayStyle>();
        PlumixHost.SystemUiOverlayStyleHandler = styles.Add;
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        try
        {
            var style = new SystemUiOverlayStyle(
                statusBarColor: new Color(0x00000000),
                systemNavigationBarColor: new Color(0x00000000),
                statusBarIconBrightness: Brightness.Dark,
                systemNavigationBarContrastEnforced: false);
            SystemChannels.Platform
                .InvokeMethod<object>("SystemChrome.setSystemUIOverlayStyle", style.ToMap())
                .GetAwaiter()
                .GetResult();

            Assert.Equal([style], styles);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    // Flutter paints its own focus highlights; the theme's focus-visible adorner would frame the view.
    [Fact]
    public void Host_HasNoThemeFocusAdorner()
    {
        Assert.Null(new TestPlumixHost().FocusAdorner);
    }

    [Fact]
    public void SystemNavigatorPop_ReachesTheInstalledHandlerWithAnimated()
    {
        var pops = new List<bool>();
        PlumixHost.SystemNavigatorPopHandler = pops.Add;
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        try
        {
            SystemNavigator.Pop().GetAwaiter().GetResult();
            SystemNavigator.Pop(animated: true).GetAwaiter().GetResult();

            Assert.Equal([false, true], pops);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    // Without a platform host's handler and outside a desktop application lifetime, the call stays
    // unimplemented (an empty reply), as on the Windows embedder.
    [Fact]
    public void SystemNavigatorPop_WithoutAHandlerOrDesktopLifetime_IsNotImplemented()
    {
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        try
        {
            var channel = new MethodChannel(SystemChannels.Platform.Name, SystemChannels.Platform.Codec);
            Assert.Throws<MissingPluginException>(
                () => channel.InvokeMethod<object>("SystemNavigator.pop").GetAwaiter().GetResult());
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    // C#-only: the Escape key's popRoute that no route took does not reach SystemNavigator.pop (Flutter maps
    // no key to popRoute), while the system back's does.
    [Fact]
    public void BackKeyPopRoute_ThatNothingPopped_DoesNotPopTheSystemNavigator()
    {
        var pops = new List<bool>();
        PlumixHost.SystemNavigatorPopHandler = pops.Add;
        var observer = new PopRouteObserver();
        WidgetsBinding.Instance.AddObserver(observer);
        var host = new TestPlumixHost();
        host.AttachPlatformChannelHandler();
        var pending = new TaskCompletionSource<bool>();
        try
        {
            observer.Result = Task.FromResult(false);
            Assert.False(host.DispatchKeyDown(Key.Escape));
            Assert.Equal(1, observer.Calls);
            Assert.Empty(pops);

            // A pop an observer answers later is still the key's.
            observer.Result = pending.Task;
            Assert.True(host.DispatchKeyDown(Key.Escape));
            pending.SetResult(false);
            EventLoop.FlushMicrotasks();
            Assert.Equal(2, observer.Calls);
            Assert.Empty(pops);

            observer.Result = Task.FromResult(false);
            Assert.False(host.DispatchBackRequested());
            Assert.Equal(3, observer.Calls);
            Assert.Equal([false], pops);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    // Android delivers its system back through TopLevel.BackRequested only, as FlutterActivity receives it
    // in onBackPressed; an unhandled KEYCODE_BACK key comes back that way.
    [Fact]
    public void BackKeys_WhenNotMapped_DoNotRunPopRoute()
    {
        PlumixHost.MapsBackKeysToPopRoute = false;
        var observer = new PopRouteObserver { Result = Task.FromResult(true) };
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            var host = new TestPlumixHost();
            Assert.False(host.DispatchKeyDown(Key.Escape));
            Assert.False(host.DispatchKeyDown(Key.BrowserBack));
            Assert.Equal(0, observer.Calls);

            Assert.True(host.DispatchBackRequested());
            Assert.Equal(1, observer.Calls);
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    [Fact]
    public void UpdatePlatformAccessibilityFeatures_ReplacesOnlyTheReportedBits()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        const int boldText = 1 << 3;
        const int highContrast = 1 << 5;
        const int reduceMotion = 1 << 4;
        dispatcher.UpdateAccessibilityFeatures(boldText);

        PlumixHost.UpdatePlatformAccessibilityFeatures(highContrast | reduceMotion, highContrast);
        Assert.Equal(boldText | highContrast, dispatcher.ReportedAccessibilityFeatures);

        PlumixHost.UpdatePlatformAccessibilityFeatures(reduceMotion, highContrast | reduceMotion);
        Assert.Equal(boldText | reduceMotion, dispatcher.ReportedAccessibilityFeatures);
        Assert.True(dispatcher.AccessibilityFeatures.ReduceMotion);
        Assert.False(dispatcher.AccessibilityFeatures.HighContrast);
    }

    private sealed class PopRouteObserver : WidgetsBindingObserver
    {
        public Task<bool> Result { get; set; } = Task.FromResult(false);

        public int Calls { get; private set; }

        public Task<bool> DidPopRoute()
        {
            Calls += 1;
            return Result;
        }
    }

    private sealed class TestPlumixHost : PlumixHost
    {
        public new void AttachPlatformChannelHandler() => base.AttachPlatformChannelHandler();

        public new void DetachPlatformChannelHandler() => base.DetachPlatformChannelHandler();

        public bool DispatchBackRequested()
        {
            var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            HandleBackRequested(args);
            return args.Handled;
        }

        public bool DispatchKeyDown(Key key)
        {
            var args = new KeyEventArgs { Key = key, PhysicalKey = PhysicalKey.Escape };
            OnKeyDown(args);
            return args.Handled;
        }
    }
}
