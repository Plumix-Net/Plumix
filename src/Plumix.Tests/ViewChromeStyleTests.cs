using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/view.dart (_updateSystemChrome)
// Tests: flutter/packages/flutter/test/rendering/view_chrome_style_test.dart ('SystemChrome - style')

public sealed class ViewChromeStyleTests : IDisposable
{
    private const double StatusBarHeight = 25.0;
    private const double NavigationBarHeight = 54.0;
    private const double DeviceHeight = 960.0;
    private const double DeviceWidth = 480.0;
    private const double DevicePixelRatio = 2.0;

    private static readonly Color MaterialBlue500 = new(0xFF2196F3);
    private static readonly Color MaterialGreen500 = new(0xFF4CAF50);

    private readonly FrameworkDartTester _tester;

    public ViewChromeStyleTests()
    {
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
        _tester = new FrameworkDartTester();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        _tester.Dispose();
        SystemChrome.SetSystemUIOverlayStyle(new SystemUiOverlayStyle());
        Scheduler.FlushMicrotasks();
        SystemChrome.ResetForTests();
    }

    public static TheoryData<TargetPlatform> Mobile => [TargetPlatform.Android, TargetPlatform.IOS];

    [Fact]
    public void StatusBarColor_IsNotSetForAnUnannotatedView()
    {
        PumpWidget(SizedBox.Expand());

        Assert.Null(SystemChrome.LatestStyle?.StatusBarColor);
    }

    [Theory]
    [MemberData(nameof(Mobile))]
    public void StatusBarColor_IsSetForAnAnnotatedView(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        SetupTestDevice();
        PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
            value: new SystemUiOverlayStyle(statusBarColor: MaterialBlue500),
            child: SizedBox.Expand()));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.StatusBarColor);
    }

    [Theory]
    [MemberData(nameof(Mobile))]
    public void StatusBarColor_IsNotSetWhenTheViewCoversLessThanHalfOfTheStatusBar(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        SetupTestDevice();
        const double lessThanHalfOfTheStatusBarHeight = StatusBarHeight / 2.0 - 1;
        PumpWidget(new Align(
            alignment: Alignment.TopCenter,
            child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(statusBarColor: MaterialBlue500),
                child: new SizedBox(width: 100, height: lessThanHalfOfTheStatusBarHeight))));

        Assert.Null(SystemChrome.LatestStyle?.StatusBarColor);
    }

    [Theory]
    [MemberData(nameof(Mobile))]
    public void StatusBarColor_IsSetWhenTheViewCoversMoreThanHalfOfTheStatusBar(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        SetupTestDevice();
        const double moreThanHalfOfTheStatusBarHeight = StatusBarHeight / 2.0 + 1;
        PumpWidget(new Align(
            alignment: Alignment.TopCenter,
            child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(statusBarColor: MaterialBlue500),
                child: new SizedBox(width: 100, height: moreThanHalfOfTheStatusBarHeight))));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.StatusBarColor);
    }

    [Fact]
    public void NavigationBarColor_IsNotSetForANonAndroidDevice()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        SetupTestDevice();
        PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
            value: new SystemUiOverlayStyle(systemNavigationBarColor: MaterialBlue500),
            child: SizedBox.Expand()));

        Assert.Null(SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void NavigationBarColor_IsNotSetForAnUnannotatedView()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        PumpWidget(SizedBox.Expand());

        Assert.Null(SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void NavigationBarColor_IsSetForAnAnnotatedView()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
            value: new SystemUiOverlayStyle(systemNavigationBarColor: MaterialBlue500),
            child: SizedBox.Expand()));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void NavigationBarColor_IsNotSetWhenTheViewCoversLessThanHalfOfTheNavigationBar()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        const double lessThanHalfOfTheNavigationBarHeight = NavigationBarHeight / 2.0 - 1;
        PumpWidget(new Align(
            alignment: Alignment.BottomCenter,
            child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(systemNavigationBarColor: MaterialBlue500),
                child: new SizedBox(width: 100, height: lessThanHalfOfTheNavigationBarHeight))));

        Assert.Null(SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void NavigationBarColor_IsSetWhenTheViewCoversMoreThanHalfOfTheNavigationBar()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        const double moreThanHalfOfTheNavigationBarHeight = NavigationBarHeight / 2.0 + 1;
        PumpWidget(new Align(
            alignment: Alignment.BottomCenter,
            child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(systemNavigationBarColor: MaterialBlue500),
                child: new SizedBox(width: 100, height: moreThanHalfOfTheNavigationBarHeight))));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void TopRegionProvidesTheStatusBarStyleAndBottomRegionTheNavigationBarStyle()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        PumpWidget(new Column(children:
        [
            new Expanded(child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    systemNavigationBarColor: MaterialBlue500,
                    statusBarColor: MaterialBlue500),
                child: SizedBox.Expand())),
            new Expanded(child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    systemNavigationBarColor: MaterialGreen500,
                    statusBarColor: MaterialGreen500),
                child: SizedBox.Expand())),
        ]));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.StatusBarColor);
        Assert.Equal(MaterialGreen500, SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void TopOnlyRegionProvidesBothBarStyles()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        PumpWidget(new Column(children:
        [
            new Expanded(child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    systemNavigationBarColor: MaterialBlue500,
                    statusBarColor: MaterialBlue500),
                child: SizedBox.Expand())),
            new Expanded(child: SizedBox.Expand()),
        ]));

        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.StatusBarColor);
        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    [Fact]
    public void BottomOnlyRegionProvidesBothBarStyles()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        PumpWidget(new Column(children:
        [
            new Expanded(child: SizedBox.Expand()),
            new Expanded(child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    systemNavigationBarColor: MaterialGreen500,
                    statusBarColor: MaterialGreen500),
                child: SizedBox.Expand())),
        ]));

        Assert.Equal(MaterialGreen500, SystemChrome.LatestStyle?.StatusBarColor);
        Assert.Equal(MaterialGreen500, SystemChrome.LatestStyle?.SystemNavigationBarColor);
    }

    // view.dart: each frame builds the style from the annotations alone — nothing is merged with the
    // previous one, so a field no annotation sets goes back to null.
    [Fact]
    public void EachFrameRebuildsTheStyleFromTheAnnotationsAlone()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
            value: new SystemUiOverlayStyle(statusBarColor: MaterialBlue500, systemNavigationBarColor: MaterialBlue500),
            child: SizedBox.Expand()));
        Assert.Equal(MaterialBlue500, SystemChrome.LatestStyle?.SystemNavigationBarColor);

        PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
            value: new SystemUiOverlayStyle(statusBarIconBrightness: PlatformBrightness.Light),
            child: SizedBox.Expand()));

        Assert.Equal(
            new SystemUiOverlayStyle(statusBarIconBrightness: PlatformBrightness.Light),
            SystemChrome.LatestStyle);
    }

    // view.dart: `automaticSystemUiAdjustment` false skips the update entirely.
    [Fact]
    public void AutomaticSystemUiAdjustment_FalseLeavesTheSystemChromeAlone()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        SetupTestDevice();
        _tester.RenderView.AutomaticSystemUiAdjustment = false;
        try
        {
            PumpWidget(new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(statusBarColor: MaterialBlue500),
                child: SizedBox.Expand()));

            Assert.Null(SystemChrome.LatestStyle);
        }
        finally
        {
            _tester.RenderView.AutomaticSystemUiAdjustment = true;
        }
    }

    private void SetupTestDevice()
    {
        var padding = new Thickness(
            0,
            StatusBarHeight * DevicePixelRatio,
            0,
            NavigationBarHeight * DevicePixelRatio);
        _tester.View.UpdateMetrics(
            physicalSize: new Size(DeviceWidth * DevicePixelRatio, DeviceHeight * DevicePixelRatio),
            devicePixelRatio: DevicePixelRatio,
            padding: padding,
            viewPadding: padding);
        WidgetsBinding.Instance.HandleMetricsChanged();
    }

    /// <summary><c>pumpWidget</c> then <c>pumpAndSettle</c>, which also drains the microtasks.</summary>
    private void PumpWidget(Widget widget)
    {
        _tester.PumpWidget(widget);
        _tester.PumpAndSettle();
        Scheduler.FlushMicrotasks();
    }
}
