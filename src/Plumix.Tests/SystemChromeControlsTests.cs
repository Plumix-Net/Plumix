using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// The control tests that assert through `SystemChrome.latestStyle`, run through the whole pipeline:
// the apps' own `setSystemUIOverlayStyle` and the annotations `RenderView._updateSystemChrome` reads.
// Dart tests:
// material_ui/test/material_test.dart ('Material uses the dark/light SystemUIOverlayStyle ...')
// cupertino_ui/test/app_test.dart ('CupertinoApp uses the dark/light SystemUIOverlayStyle ...')
// cupertino_ui/test/nav_bar_test.dart ('System navigation bar properties are not overridden',
//   'Can specify custom brightness')
// cupertino_ui/test/sheet_test.dart ('CupertinoSheet causes SystemUiOverlayStyle changes')

public sealed class SystemChromeControlsTests : IDisposable
{
    private readonly FrameworkDartTester _tester;

    public SystemChromeControlsTests()
    {
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
        // flutter_test runs every widget test as Android unless a variant says otherwise.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        _tester = new FrameworkDartTester();
    }

    public void Dispose()
    {
        _tester.Dispose();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
    }

    [Fact]
    public void MaterialApp_UsesTheDarkStyleWhenTheBackgroundIsLight()
    {
        var lightTheme = new ThemeData();
        Pump(new MaterialApp(
            theme: lightTheme,
            home: new Scaffold(body: new Center(child: new Text("test")))));

        Assert.Equal(Brightness.Light, lightTheme.ColorScheme.Brightness);
        Assert.Equal(SystemUiOverlayStyle.Dark, SystemChrome.LatestStyle);
    }

    [Fact]
    public void MaterialApp_UsesTheLightStyleWhenTheBackgroundIsDark()
    {
        ThemeData darkTheme = ThemeData.Dark;
        Pump(new MaterialApp(
            theme: darkTheme,
            home: new Scaffold(body: new Center(child: new Text("test")))));

        Assert.Equal(Brightness.Dark, darkTheme.ColorScheme.Brightness);
        Assert.Equal(SystemUiOverlayStyle.Light, SystemChrome.LatestStyle);
    }

    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void CupertinoApp_StyleFollowsTheThemeBrightness(PlatformBrightness brightness)
    {
        Pump(new CupertinoApp(
            theme: new CupertinoThemeData(brightness: brightness),
            home: new CupertinoPageScaffold(child: new Text("Hello"))));

        Assert.Equal(
            brightness == PlatformBrightness.Dark ? SystemUiOverlayStyle.Light : SystemUiOverlayStyle.Dark,
            SystemChrome.LatestStyle);
    }

    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void CupertinoApp_StyleFollowsThePlatformBrightnessWithoutAThemeBrightness(PlatformBrightness brightness)
    {
        Pump(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness),
            child: new CupertinoApp(builder: (_, _) => new Placeholder())));

        Assert.Equal(
            brightness == PlatformBrightness.Dark ? SystemUiOverlayStyle.Light : SystemUiOverlayStyle.Dark,
            SystemChrome.LatestStyle);
    }

    [Fact]
    public void CupertinoNavigationBar_DoesNotOverrideSystemNavigationBarProperties()
    {
        Pump(new CupertinoApp(home: new CupertinoNavigationBar(backgroundColor: new Color(0xF0F9F9F9))));

        AssertSameStatusBarStyle(SystemChrome.LatestStyle!, SystemUiOverlayStyle.Dark);
    }

    [Fact]
    public void CupertinoNavigationBar_CanSpecifyACustomBrightness()
    {
        Pump(new CupertinoApp(home: new CupertinoNavigationBar(
            backgroundColor: new Color(0xF0F9F9F9),
            brightness: PlatformBrightness.Dark)));
        AssertSameStatusBarStyle(SystemChrome.LatestStyle!, SystemUiOverlayStyle.Light);

        Pump(new CupertinoApp(home: new CupertinoNavigationBar(
            backgroundColor: new Color(0xF01D1D1D),
            brightness: PlatformBrightness.Light)));
        AssertSameStatusBarStyle(SystemChrome.LatestStyle!, SystemUiOverlayStyle.Dark);

        Pump(new CupertinoApp(home: new CustomScrollView(slivers:
        [
            new CupertinoSliverNavigationBar(
                largeTitle: new Text("Title"),
                backgroundColor: new Color(0xF0F9F9F9),
                brightness: PlatformBrightness.Dark),
        ])));
        AssertSameStatusBarStyle(SystemChrome.LatestStyle!, SystemUiOverlayStyle.Light);

        Pump(new CupertinoApp(home: new CustomScrollView(slivers:
        [
            new CupertinoSliverNavigationBar(
                largeTitle: new Text("Title"),
                backgroundColor: new Color(0xF01D1D1D),
                brightness: PlatformBrightness.Light),
        ])));
        AssertSameStatusBarStyle(SystemChrome.LatestStyle!, SystemUiOverlayStyle.Dark);
    }

    [Fact]
    public void CupertinoSheet_ChangesTheSystemUiOverlayStyle()
    {
        var scaffoldKey = new LabeledGlobalKey<State>("scaffold");
        Pump(new CupertinoApp(home: new CupertinoPageScaffold(
            key: scaffoldKey,
            navigationBar: new CupertinoNavigationBar(middle: new Text("SystemUiOverlayStyle")),
            child: new Center(child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new Text("Page 1"),
                    new CupertinoButton(
                        onPressed: () => Navigator.Push(
                            scaffoldKey.CurrentContext!,
                            new CupertinoSheetRoute<object?>(
                                builder: _ => new CupertinoPageScaffold(child: new Text("Page 2")))),
                        child: new Text("Push Page 2")),
                ])))));

        Assert.Equal(PlatformBrightness.Light, SystemChrome.LatestStyle!.StatusBarBrightness);
        Assert.Equal(PlatformBrightness.Dark, SystemChrome.LatestStyle!.StatusBarIconBrightness);

        _tester.Tap(_tester.ElementsWithText("Push Page 2").Single());
        Settle();

        Assert.Equal(PlatformBrightness.Dark, SystemChrome.LatestStyle!.StatusBarBrightness);
        Assert.Equal(PlatformBrightness.Light, SystemChrome.LatestStyle!.StatusBarIconBrightness);

        // Returning to the previous page reverts the system UI.
        Navigator.Of(scaffoldKey.CurrentContext!).Pop();
        Settle();

        Assert.Equal(PlatformBrightness.Light, SystemChrome.LatestStyle!.StatusBarBrightness);
        Assert.Equal(PlatformBrightness.Dark, SystemChrome.LatestStyle!.StatusBarIconBrightness);
    }

    /// <summary>
    /// nav_bar_test.dart's <c>expectSameStatusBarStyle</c>: the status bar fields match and no system
    /// navigation bar field is set.
    /// </summary>
    private static void AssertSameStatusBarStyle(SystemUiOverlayStyle style, SystemUiOverlayStyle expectedStyle)
    {
        Assert.Equal(expectedStyle.StatusBarColor, style.StatusBarColor);
        Assert.Equal(expectedStyle.StatusBarBrightness, style.StatusBarBrightness);
        Assert.Equal(expectedStyle.StatusBarIconBrightness, style.StatusBarIconBrightness);
        Assert.Equal(expectedStyle.SystemStatusBarContrastEnforced, style.SystemStatusBarContrastEnforced);
        Assert.Null(style.SystemNavigationBarColor);
        Assert.Null(style.SystemNavigationBarContrastEnforced);
        Assert.Null(style.SystemNavigationBarDividerColor);
        Assert.Null(style.SystemNavigationBarIconBrightness);
    }

    /// <summary><c>pumpWidget</c>, whose fake-async zone drains the microtasks after the frame.</summary>
    private void Pump(Widget widget)
    {
        _tester.PumpWidget(widget);
        Scheduler.FlushMicrotasks();
    }

    private void Settle()
    {
        _tester.PumpAndSettle();
        Scheduler.FlushMicrotasks();
    }
}
