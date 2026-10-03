using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports dart:ui's `AccessibilityFeatures` (sky_engine/lib/ui/window.dart) contract, the
// accessibility-feature cases of flutter/packages/flutter/test/widgets/media_query_test.dart,
// binding_test.dart's 'didChangeAccessibilityFeatures' and flutter_test's
// platform_dispatcher_test.dart 'TestPlatformDispatcher can fake accessibility features'.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AccessibilityFeaturesDartParityTests
{
    // window.dart: with no flag reported, only the three inverted getters are on.
    [Fact]
    public void DefaultFeaturesHaveOnlyTheInvertedFlagsOn()
    {
        var features = new AccessibilityFeatures(0);

        Assert.False(features.AccessibleNavigation);
        Assert.False(features.InvertColors);
        Assert.False(features.DisableAnimations);
        Assert.False(features.BoldText);
        Assert.False(features.ReduceMotion);
        Assert.False(features.HighContrast);
        Assert.False(features.OnOffSwitchLabels);
        Assert.True(features.SupportsAnnounce);
        Assert.True(features.AutoPlayAnimatedImages);
        Assert.True(features.AutoPlayVideos);
        Assert.False(features.DeterministicCursor);
        Assert.Equal(
            "AccessibilityFeatures[supportsAnnounce, autoPlayAnimatedImages, autoPlayVideos]",
            features.ToString());
    }

    // window.dart: each index bit drives one getter; the no-announce and no-auto-play bits clear theirs.
    [Fact]
    public void EveryIndexBitDrivesOneGetter()
    {
        Assert.True(new AccessibilityFeatures(1 << 0).AccessibleNavigation);
        Assert.True(new AccessibilityFeatures(1 << 1).InvertColors);
        Assert.True(new AccessibilityFeatures(1 << 2).DisableAnimations);
        Assert.True(new AccessibilityFeatures(1 << 3).BoldText);
        Assert.True(new AccessibilityFeatures(1 << 4).ReduceMotion);
        Assert.True(new AccessibilityFeatures(1 << 5).HighContrast);
        Assert.True(new AccessibilityFeatures(1 << 6).OnOffSwitchLabels);
        Assert.False(new AccessibilityFeatures(1 << 7).SupportsAnnounce);
        Assert.False(new AccessibilityFeatures(1 << 8).AutoPlayAnimatedImages);
        Assert.False(new AccessibilityFeatures(1 << 9).AutoPlayVideos);
        Assert.True(new AccessibilityFeatures(1 << 10).DeterministicCursor);

        var all = new AccessibilityFeatures((1 << 11) - 1);
        Assert.Equal(
            "AccessibilityFeatures[accessibleNavigation, invertColors, disableAnimations, boldText, "
            + "reduceMotion, highContrast, onOffSwitchLabels, deterministicCursor]",
            all.ToString());
    }

    // window.dart: `==` compares the runtime type and the index; `hashCode` is the index's.
    [Fact]
    public void EqualityComparesRuntimeTypeAndIndex()
    {
        var a = new AccessibilityFeatures(5);
        var b = new AccessibilityFeatures(5);
        var c = new AccessibilityFeatures(4);

        Assert.True(a == b);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a != c);
        Assert.NotEqual(a, c);

        // A fake never equals a real instance, even with the same flags (Dart's runtimeType check).
        var fake = new FakeAccessibilityFeatures(supportsAnnounce: true, autoPlayAnimatedImages: true,
            autoPlayVideos: true);
        Assert.False(fake.Equals(new AccessibilityFeatures(0)));
        Assert.False(new AccessibilityFeatures(0).Equals(fake));
        Assert.Equal(new FakeAccessibilityFeatures(boldText: true), new FakeAccessibilityFeatures(boldText: true));
        Assert.NotEqual(new FakeAccessibilityFeatures(boldText: true), new FakeAccessibilityFeatures());
    }

    // platform_dispatcher.dart `_updateAccessibilityFeatures`: a change is stored and announced once;
    // the same bitfield again announces nothing.
    [Fact]
    public void UpdateAccessibilityFeaturesNotifiesOnlyOnChange()
    {
        using var tester = new FrameworkDartTester();
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        int original = dispatcher.ReportedAccessibilityFeatures;
        Action? previous = dispatcher.OnAccessibilityFeaturesChanged;
        int calls = 0;
        dispatcher.OnAccessibilityFeaturesChanged = () => calls++;
        try
        {
            dispatcher.UpdateAccessibilityFeatures(original ^ (1 << 3));
            Assert.Equal(1, calls);
            Assert.Equal(!new AccessibilityFeatures(original).BoldText, dispatcher.AccessibilityFeatures.BoldText);

            dispatcher.UpdateAccessibilityFeatures(original ^ (1 << 3));
            Assert.Equal(1, calls);
        }
        finally
        {
            dispatcher.OnAccessibilityFeaturesChanged = previous;
            dispatcher.UpdateAccessibilityFeatures(original);
            SemanticsBinding.Instance.HandleAccessibilityFeaturesChanged();
        }
    }

    // semantics/binding.dart: the binding takes the dispatcher's value when the features change; a
    // change reported during the persistent callbacks is handled after the frame.
    [Fact]
    public void SemanticsBindingDefersAChangeReportedDuringPersistentCallbacks()
    {
        using var tester = new FrameworkDartTester();
        bool? duringBuild = null;
        tester.PumpWidget(new Builder(_ =>
        {
            if (duringBuild is null)
            {
                tester.PlatformDispatcher.AccessibilityFeaturesTestValue =
                    new FakeAccessibilityFeatures(boldText: true);
                duringBuild = SemanticsBinding.Instance.AccessibilityFeatures.BoldText;
            }

            return new SizedBox();
        }));

        Assert.False(duringBuild);
        Assert.True(SemanticsBinding.Instance.AccessibilityFeatures.BoldText);
        Assert.True(WidgetsBinding.Instance.AccessibilityFeatures.BoldText);

        tester.PlatformDispatcher.ClearAccessibilityFeaturesTestValue();
        Assert.False(SemanticsBinding.Instance.AccessibilityFeatures.BoldText);
    }

    // semantics/binding.dart `disableAnimations`, with the `debugSemanticsDisableAnimations` override.
    [DebugOnlyFact]
    public void DisableAnimationsFollowsTheFeaturesUnlessTheDebugOverrideIsSet()
    {
        using var tester = new FrameworkDartTester();
        Assert.False(SemanticsBinding.Instance.DisableAnimations);

        tester.PlatformDispatcher.AccessibilityFeaturesTestValue =
            new FakeAccessibilityFeatures(disableAnimations: true);
        Assert.True(SemanticsBinding.Instance.DisableAnimations);

        SemanticsDebug.DebugSemanticsDisableAnimations = false;
        try
        {
            Assert.False(SemanticsBinding.Instance.DisableAnimations);
        }
        finally
        {
            SemanticsDebug.DebugSemanticsDisableAnimations = null;
        }
    }

    // Flutter: 'platform_dispatcher_test.dart: TestPlatformDispatcher can fake accessibility features'
    [Fact]
    public void TestPlatformDispatcherCanFakeAccessibilityFeatures()
    {
        using var tester = new FrameworkDartTester();
        AccessibilityFeatures realValue = PlatformDispatcher.Instance.AccessibilityFeatures;
        AccessibilityFeatures fakeValue = new FakeAccessibilityFeatures();

        AccessibilityFeatures propertyBeforeFaking = WidgetsBinding.Instance.AccessibilityFeatures;
        tester.Binding.PlatformDispatcher.AccessibilityFeaturesTestValue = fakeValue;
        AccessibilityFeatures propertyAfterFaking = tester.View.FlutterView.PlatformDispatcher.AccessibilityFeatures;

        Assert.False(realValue == fakeValue);
        Assert.Equal(realValue, propertyBeforeFaking);
        Assert.Equal(fakeValue, propertyAfterFaking);
    }

    // C#-only: a test value does not outlive its tester, as flutter_test resets the dispatcher.
    [Fact]
    public void TesterDisposeClearsTheAccessibilityFeaturesTestValue()
    {
        AccessibilityFeatures real = PlatformDispatcher.Instance.AccessibilityFeatures;
        using (var tester = new FrameworkDartTester())
        {
            tester.PlatformDispatcher.AccessibilityFeaturesTestValue = FakeAccessibilityFeatures.AllOn;
            Assert.Same(FakeAccessibilityFeatures.AllOn, PlatformDispatcher.Instance.AccessibilityFeatures);
        }

        Assert.Equal(real, PlatformDispatcher.Instance.AccessibilityFeatures);
        Assert.Equal(real, SemanticsBinding.Instance.AccessibilityFeatures);
    }

    // Flutter: 'binding_test.dart: didChangeAccessibilityFeatures'
    [Fact]
    public void DidChangeAccessibilityFeatures()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? oldHandler = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        var throwingObserver = new ThrowingObserver();
        var loggingObserver = new LoggingObserver(log);
        WidgetsBinding.Instance.AddObserver(throwingObserver);
        WidgetsBinding.Instance.AddObserver(loggingObserver);
        try
        {
            WidgetsBinding.Instance.HandleAccessibilityFeaturesChanged();
            Assert.Contains("didChangeAccessibilityFeatures", log);
            Assert.Single(errors);
        }
        finally
        {
            FlutterError.OnError = oldHandler;
            WidgetsBinding.Instance.RemoveObserver(throwingObserver);
            WidgetsBinding.Instance.RemoveObserver(loggingObserver);
        }
    }

    // Flutter: 'media_query_test.dart: MediaQueryData.fromView is sane'
    [Fact]
    public void MediaQueryDataFromViewIsSane()
    {
        using var tester = new FrameworkDartTester();
        tester.PlatformDispatcher.AccessibilityFeaturesTestValue = new FakeAccessibilityFeatures();
        MediaQueryData data = MediaQueryData.FromView(tester.View);
        AssertHasOneLineDescription(data);
        Assert.Equal(data.CopyWith().GetHashCode(), data.GetHashCode());
        Assert.Equal(LogicalSize(tester), data.Size);
        Assert.False(data.AccessibleNavigation);
        Assert.False(data.InvertColors);
        Assert.False(data.DisableAnimations);
        Assert.False(data.BoldText);
        Assert.False(data.HighContrast);
        Assert.False(data.OnOffSwitchLabels);
        Assert.False(data.SupportsAnnounce);
        Assert.Equal(PlatformBrightness.Light, data.PlatformBrightness);
        Assert.Null(data.GestureSettings?.TouchSlop);
        Assert.Empty(data.DisplayFeatures ?? []);
        Assert.Null(data.DisplayCornerRadii);
    }

    // Flutter: 'media_query_test.dart: MediaQueryData.fromView uses platformData if provided'
    [Fact]
    public void MediaQueryDataFromViewUsesPlatformDataIfProvided()
    {
        using var tester = new FrameworkDartTester();
        MediaQueryData platformData = AllOnPlatformData();

        MediaQueryData data = MediaQueryData.FromView(tester.View, platformData: platformData);
        AssertHasOneLineDescription(data);
        Assert.Equal(data.CopyWith().GetHashCode(), data.GetHashCode());
        AssertViewData(tester, data);
        AssertPlatformData(platformData, data);
    }

    // Flutter: 'media_query_test.dart: MediaQueryData.fromView uses data from platformDispatcher if no
    // platformData is provided'. Plumix's dispatcher carries no text scale factor, brightness or
    // 24-hour setting to fake (docs/ai/DIVERGENCES.md, MediaQueryData.FromView), so those three
    // expectations read the defaults a non-implicit view gets.
    [Fact]
    public void MediaQueryDataFromViewUsesDataFromPlatformDispatcherIfNoPlatformDataIsProvided()
    {
        using var tester = new FrameworkDartTester();
        tester.PlatformDispatcher.AccessibilityFeaturesTestValue = FakeAccessibilityFeatures.AllOn;

        MediaQueryData data = MediaQueryData.FromView(tester.View);
        AssertHasOneLineDescription(data);
        Assert.Equal(data.CopyWith().GetHashCode(), data.GetHashCode());
        AssertViewData(tester, data);
        AssertDispatcherFeatures(tester, data);
        Assert.Equal(TextScaler.NoScaling, data.TextScaler);
        Assert.Equal(PlatformBrightness.Light, data.PlatformBrightness);
        Assert.False(data.AlwaysUse24HourFormat);
        Assert.Equal(NavigationMode.Traditional, data.NavigationMode);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.fromView injects a new MediaQuery with data from view,
    // preserving platform-specific data'
    [Fact]
    public void MediaQueryFromViewInjectsANewMediaQueryPreservingPlatformSpecificData()
    {
        using var tester = new FrameworkDartTester();
        MediaQueryData platformData = AllOnPlatformData();

        MediaQueryData? data = null;
        tester.PumpWidget(new MediaQuery(
            data: platformData,
            child: MediaQuery.FromView(
                view: tester.View,
                child: new Builder(context =>
                {
                    data = MediaQuery.Of(context);
                    return new Placeholder();
                }))));

        Assert.NotEqual(platformData, data);
        AssertViewData(tester, data!);
        AssertPlatformData(platformData, data!);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.fromView injects a new MediaQuery with data from view
    // when no surrounding MediaQuery exists'. The harness always wraps the tree in a `View`
    // (no `wrapWithView: false`), so the outer `MediaQuery.maybeOf` is the tester view's own
    // `MediaQuery.fromView`, which has no parent either; the inner query is built the same way. The
    // text scale factor and brightness are not fakeable (see the test above).
    [Fact]
    public void MediaQueryFromViewInjectsANewMediaQueryWhenNoSurroundingMediaQueryExists()
    {
        using var tester = new FrameworkDartTester();
        tester.PlatformDispatcher.AccessibilityFeaturesTestValue = FakeAccessibilityFeatures.AllOn;

        MediaQueryData? data = null;
        tester.PumpWidget(MediaQuery.FromView(
            view: tester.View,
            child: new Builder(context =>
            {
                data = MediaQuery.Of(context);
                return new SizedBox();
            })));

        AssertViewData(tester, data!);
        AssertDispatcherFeatures(tester, data!);
        Assert.False(data!.AlwaysUse24HourFormat);
        Assert.Equal(NavigationMode.Traditional, data.NavigationMode);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.fromView updates on notifications (no parent data)'.
    // The text scale factor and brightness steps are not fakeable (see above); the accessibility and
    // device-pixel-ratio steps run as in Dart.
    [Fact]
    public void MediaQueryFromViewUpdatesOnNotificationsNoParentData()
    {
        using var tester = new FrameworkDartTester();
        try
        {
            tester.PlatformDispatcher.AccessibilityFeaturesTestValue = FakeAccessibilityFeatures.AllOn;
            tester.View.DevicePixelRatio = 44;

            MediaQueryData? data = null;
            int rebuildCount = 0;
            tester.PumpWidget(MediaQuery.FromView(
                view: tester.View,
                child: new Builder(context =>
                {
                    rebuildCount++;
                    data = MediaQuery.Of(context);
                    return new SizedBox();
                })));

            Assert.Equal(1, rebuildCount);

            Assert.True(data!.AccessibleNavigation);
            tester.PlatformDispatcher.AccessibilityFeaturesTestValue = new FakeAccessibilityFeatures();
            tester.PumpAndSettle();
            Assert.False(data!.AccessibleNavigation);
            Assert.Equal(2, rebuildCount);

            Assert.Equal(44, data!.DevicePixelRatio);
            tester.View.DevicePixelRatio = 55;
            tester.Pump();
            Assert.Equal(55, data!.DevicePixelRatio);
            Assert.Equal(3, rebuildCount);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'media_query_test.dart: MediaQuery.fromView updates on notifications (with parent data)'.
    // The text scale factor and brightness steps are not fakeable (see above).
    [Fact]
    public void MediaQueryFromViewUpdatesOnNotificationsWithParentData()
    {
        using var tester = new FrameworkDartTester();
        try
        {
            tester.PlatformDispatcher.AccessibilityFeaturesTestValue = FakeAccessibilityFeatures.AllOn;
            tester.View.DevicePixelRatio = 44;

            MediaQueryData? data = null;
            int rebuildCount = 0;
            tester.PumpWidget(new MediaQuery(
                data: new MediaQueryData(
                    TextScaler: TextScaler.Linear(44),
                    PlatformBrightness: PlatformBrightness.Dark,
                    AccessibleNavigation: true),
                child: MediaQuery.FromView(
                    view: tester.View,
                    child: new Builder(context =>
                    {
                        rebuildCount++;
                        data = MediaQuery.Of(context);
                        return new Placeholder();
                    }))));

            Assert.Equal(1, rebuildCount);
            Assert.Equal(10 * 44, data!.TextScaler.Scale(10));
            Assert.Equal(PlatformBrightness.Dark, data!.PlatformBrightness);

            Assert.True(data!.AccessibleNavigation);
            tester.PlatformDispatcher.AccessibilityFeaturesTestValue = new FakeAccessibilityFeatures();
            tester.PumpAndSettle();
            Assert.True(data!.AccessibleNavigation);
            Assert.Equal(1, rebuildCount);

            Assert.Equal(44, data!.DevicePixelRatio);
            tester.View.DevicePixelRatio = 55;
            tester.Pump();
            Assert.Equal(55, data!.DevicePixelRatio);
            Assert.Equal(2, rebuildCount);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    private static MediaQueryData AllOnPlatformData() => new(
        TextScaler: TextScaler.Linear(1234),
        PlatformBrightness: PlatformBrightness.Dark,
        AccessibleNavigation: true,
        InvertColors: true,
        DisableAnimations: true,
        BoldText: true,
        HighContrast: true,
        OnOffSwitchLabels: true,
        SupportsAnnounce: true,
        AlwaysUse24HourFormat: true,
        NavigationMode: NavigationMode.Directional);

    private static Size LogicalSize(FrameworkDartTester tester) => new(
        tester.View.PhysicalSize.Width / tester.View.DevicePixelRatio,
        tester.View.PhysicalSize.Height / tester.View.DevicePixelRatio);

    private static Thickness FromViewPadding(Thickness padding, double devicePixelRatio) => new(
        padding.Left / devicePixelRatio,
        padding.Top / devicePixelRatio,
        padding.Right / devicePixelRatio,
        padding.Bottom / devicePixelRatio);

    private static void AssertHasOneLineDescription(MediaQueryData data)
    {
        string description = data.ToString();
        Assert.False(string.IsNullOrEmpty(description));
        Assert.DoesNotContain('\n', description);
    }

    private static void AssertViewData(FrameworkDartTester tester, MediaQueryData data)
    {
        double devicePixelRatio = tester.View.DevicePixelRatio;
        Assert.Equal(LogicalSize(tester), data.Size);
        Assert.Equal(devicePixelRatio, data.DevicePixelRatio);
        Assert.Equal(FromViewPadding(tester.View.Padding, devicePixelRatio), data.Padding);
        Assert.Equal(FromViewPadding(tester.View.ViewPadding, devicePixelRatio), data.ViewPadding);
        Assert.Equal(FromViewPadding(tester.View.ViewInsets, devicePixelRatio), data.ViewInsets);
        Assert.Equal(FromViewPadding(tester.View.SystemGestureInsets, devicePixelRatio), data.SystemGestureInsets);
        Assert.Equal(tester.View.FlutterView.GestureSettings, data.GestureSettings);
        Assert.Equal(tester.View.FlutterView.DisplayFeatures, data.DisplayFeatures);
    }

    private static void AssertPlatformData(MediaQueryData platformData, MediaQueryData data)
    {
        Assert.Equal(TextScaler.Linear(platformData.TextScaleFactor), data.TextScaler);
        Assert.Equal(platformData.PlatformBrightness, data.PlatformBrightness);
        Assert.Equal(platformData.AccessibleNavigation, data.AccessibleNavigation);
        Assert.Equal(platformData.InvertColors, data.InvertColors);
        Assert.Equal(platformData.DisableAnimations, data.DisableAnimations);
        Assert.Equal(platformData.BoldText, data.BoldText);
        Assert.Equal(platformData.HighContrast, data.HighContrast);
        Assert.Equal(platformData.OnOffSwitchLabels, data.OnOffSwitchLabels);
        Assert.Equal(platformData.SupportsAnnounce, data.SupportsAnnounce);
        Assert.Equal(platformData.AlwaysUse24HourFormat, data.AlwaysUse24HourFormat);
        Assert.Equal(platformData.NavigationMode, data.NavigationMode);
    }

    private static void AssertDispatcherFeatures(FrameworkDartTester tester, MediaQueryData data)
    {
        AccessibilityFeatures features = tester.PlatformDispatcher.AccessibilityFeatures;
        Assert.Equal(features.AccessibleNavigation, data.AccessibleNavigation);
        Assert.Equal(features.InvertColors, data.InvertColors);
        Assert.Equal(features.DisableAnimations, data.DisableAnimations);
        Assert.Equal(features.BoldText, data.BoldText);
        Assert.Equal(features.HighContrast, data.HighContrast);
        Assert.Equal(features.OnOffSwitchLabels, data.OnOffSwitchLabels);
        Assert.Equal(features.SupportsAnnounce, data.SupportsAnnounce);
        Assert.True(data.AccessibleNavigation);
        Assert.True(data.SupportsAnnounce);
    }

    // binding_test.dart's `ThrowingObserver` and `LoggingObserver`, reduced to the hook under test.
    private sealed class ThrowingObserver : WidgetsBindingObserver
    {
        public void DidChangeAccessibilityFeatures() => throw new InvalidOperationException("Exception");
    }

    private sealed class LoggingObserver(List<string> log) : WidgetsBindingObserver
    {
        public void DidChangeAccessibilityFeatures() => log.Add("didChangeAccessibilityFeatures");
    }
}
