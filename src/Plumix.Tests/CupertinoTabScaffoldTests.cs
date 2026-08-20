using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/tab_scaffold_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTabScaffoldTests : IDisposable
{
    private static readonly Size ViewSize = new(400.0, 600.0);

    public CupertinoTabScaffoldTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ControllerAndScaffold_ExposePinnedDefaultsAndGuards()
    {
        var controller = new CupertinoTabController();
        int notifications = 0;
        controller.AddListener(() => notifications++);

        Assert.Equal(0, controller.Index);
        controller.Index = 0;
        Assert.Equal(0, notifications);
        controller.Index = 2;
        Assert.Equal(1, notifications);
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Index = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTabController(-1));

        CupertinoTabBar tabBar = BuildTabBar(3, currentIndex: 1);
        IndexedWidgetBuilder builder = (_, index) => new Text($"Page {index}");
        var scaffold = new CupertinoTabScaffold(tabBar, builder);

        Assert.Same(tabBar, scaffold.TabBar);
        Assert.Same(builder, scaffold.TabBuilder);
        Assert.Null(scaffold.Controller);
        Assert.Null(scaffold.BackgroundColor);
        Assert.True(scaffold.ResizeToAvoidBottomInset);
        Assert.Null(scaffold.RestorationId);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTabScaffold(
            BuildTabBar(2),
            builder,
            controller: new CupertinoTabController(2)));

        var restorable = new RestorableCupertinoTabController(initialIndex: 2);
        using CupertinoTabController defaultValue = restorable.CreateDefaultValue();
        using CupertinoTabController restoredValue = restorable.FromPrimitives(1);
        Assert.Equal(2, defaultValue.Index);
        Assert.Equal(1, restoredValue.Index);
        Assert.Throws<InvalidOperationException>(() => restorable.FromPrimitives(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RestorableCupertinoTabController(-1));

        controller.Dispose();
    }

    [Fact]
    public void Tabs_AreLazyCachedOffstageAndChainTheUsersTapCallback()
    {
        var built = new List<int>();
        var tapped = new List<int>();
        using var harness = new CupertinoThemeTestHarness(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(2, onTap: tapped.Add),
            tabBuilder: (_, index) =>
            {
                built.Add(index);
                return new Text($"Page {index}");
            })));
        harness.Pump(ViewSize);

        Assert.Equal([0], built);
        Assert.Equal([false, true], harness.FindWidgets<Offstage>().Select(widget => widget.IsOffstage));
        Assert.Equal([true, false], harness.FindWidgets<TickerMode>().Select(widget => widget.Enabled));
        Assert.Equal([true, false], harness.FindWidgets<HeroMode>().Select(widget => widget.Enabled));
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Page 0");
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "Page 1");

        CupertinoTabBar copiedTabBar = Assert.Single(harness.FindWidgets<CupertinoTabBar>());
        copiedTabBar.OnTap!(1);
        harness.Pump(ViewSize);

        Assert.Equal([0, 0, 1], built);
        Assert.Equal([1], tapped);
        Assert.Equal([true, false], harness.FindWidgets<Offstage>().Select(widget => widget.IsOffstage));
        Assert.Equal([false, true], harness.FindWidgets<TickerMode>().Select(widget => widget.Enabled));
        Assert.Equal([false, true], harness.FindWidgets<HeroMode>().Select(widget => widget.Enabled));
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Page 0");
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Page 1");
        Assert.Equal(1, Assert.Single(harness.FindWidgets<CupertinoTabBar>()).CurrentIndex);

        Assert.Single(tapped);
        Assert.Single(built, index => index == 1);
    }

    [Fact]
    public void ExternalControllersSwitchProgrammaticallyMoveListenersAndAreNotOwned()
    {
        var first = new TrackingTabController(initialIndex: 1);
        var second = new TrackingTabController(initialIndex: 0);
        var tapped = new List<int>();
        using var harness = new CupertinoThemeTestHarness(BuildRoot(BuildScaffold(first, tapped)));
        harness.Pump(ViewSize);

        Assert.Equal(1, first.ListenerCount);
        Assert.Equal(1, Assert.Single(harness.FindWidgets<CupertinoTabBar>()).CurrentIndex);

        first.Index = 0;
        harness.Pump(ViewSize);
        Assert.Empty(tapped);
        Assert.Equal(0, Assert.Single(harness.FindWidgets<CupertinoTabBar>()).CurrentIndex);

        harness.PumpWidget(BuildRoot(BuildScaffold(second, tapped)));
        harness.Pump(ViewSize);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);
        Assert.False(first.WasDisposed);

        harness.PumpWidget(BuildRoot(BuildScaffold(controller: null, tapped)));
        harness.Pump(ViewSize);
        Assert.Equal(0, second.ListenerCount);
        Assert.False(second.WasDisposed);

        Assert.Single(harness.FindWidgets<CupertinoTabBar>()).OnTap!(1);
        harness.Pump(ViewSize);
        Assert.Equal([1], tapped);
        Assert.Equal(1, Assert.Single(harness.FindWidgets<CupertinoTabBar>()).CurrentIndex);

        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public void ControllerRejectsAnIndexOutsideTheMountedTabBar()
    {
        using var controller = new CupertinoTabController();
        using var harness = new CupertinoThemeTestHarness(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(2),
            controller: controller,
            tabBuilder: (_, index) => new Text($"Page {index}"))));
        harness.Pump(ViewSize);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => controller.Index = 2);

        Assert.Contains("with 2 tabs", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TabCountChangesClampSelectionAndRetainExistingFocusScopes()
    {
        using var controller = new CupertinoTabController(initialIndex: 2);
        using var harness = new CupertinoThemeTestHarness(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(3),
            controller: controller,
            tabBuilder: (_, index) => new Text($"Page {index}"))));
        harness.Pump(ViewSize);
        IReadOnlyList<FocusScopeNode?> originalNodes = harness.FindWidgets<FocusScope>()
            .Where(scope => scope.FocusScopeNode is not null)
            .Select(scope => scope.FocusScopeNode)
            .ToArray();

        harness.PumpWidget(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(5),
            controller: controller,
            tabBuilder: (_, index) => new Text($"Page {index}"))));
        harness.Pump(ViewSize);
        IReadOnlyList<FocusScopeNode?> expandedNodes = harness.FindWidgets<FocusScope>()
            .Where(scope => scope.FocusScopeNode is not null)
            .Select(scope => scope.FocusScopeNode)
            .ToArray();
        Assert.Equal(5, expandedNodes.Count);
        Assert.Equal(originalNodes, expandedNodes.Take(3));

        using var clampHarness = new CupertinoThemeTestHarness(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(5),
            tabBuilder: (_, index) => new Text($"Page {index}"))));
        clampHarness.Pump(ViewSize);
        Assert.Single(clampHarness.FindWidgets<CupertinoTabBar>()).OnTap!(4);
        clampHarness.Pump(ViewSize);
        clampHarness.PumpWidget(BuildRoot(new CupertinoTabScaffold(
            tabBar: BuildTabBar(2),
            tabBuilder: (_, index) => new Text($"Changed {index}"))));
        clampHarness.Pump(ViewSize);

        Assert.Equal(1, Assert.Single(clampHarness.FindWidgets<CupertinoTabBar>()).CurrentIndex);
        Assert.Equal(2, clampHarness.FindWidgets<Offstage>().Count);
        Assert.Contains(clampHarness.FindWidgets<Text>(), text => text.Data == "Changed 1");
        Assert.DoesNotContain(clampHarness.FindWidgets<Text>(), text => text.Data == "Changed 4");
    }

    [Fact]
    public void SwitchingTabsMovesFocusAndRestoresTheTabsPreviousFocusedChild()
    {
        var focusNodes = new[] { new FocusNode(), new FocusNode() };
        using (var harness = new CupertinoThemeTestHarness(BuildRoot(new CupertinoTabScaffold(
                   tabBar: BuildTabBar(2),
                   tabBuilder: (_, index) => new Focus(
                       focusNode: focusNodes[index],
                       autofocus: true,
                       child: new Text($"Page {index}"))))))
        {
            harness.Pump(ViewSize);
            Assert.True(focusNodes[0].HasFocus);

            Assert.Single(harness.FindWidgets<CupertinoTabBar>()).OnTap!(1);
            harness.Pump(ViewSize);
            Assert.False(focusNodes[0].HasFocus);
            Assert.True(focusNodes[1].HasFocus);

            Assert.Single(harness.FindWidgets<CupertinoTabBar>()).OnTap!(0);
            harness.Pump(ViewSize);
            Assert.True(focusNodes[0].HasFocus);
            Assert.False(focusNodes[1].HasFocus);
        }

        foreach (FocusNode focusNode in focusNodes)
        {
            focusNode.Dispose();
        }
    }

    [Fact]
    public void InsetsAndOpacityMatchFlutterScaffoldLayoutRules()
    {
        MediaQueryData? resizedQuery = null;
        using (var resized = new CupertinoThemeTestHarness(BuildRoot(
                   new CupertinoTabScaffold(
                       tabBar: BuildTabBar(2),
                       tabBuilder: (context, _) => new CaptureContext(
                           captured => resizedQuery = MediaQuery.Of(captured))),
                   viewInsets: new Thickness(0.0, 0.0, 0.0, 200.0))))
        {
            resized.Pump(ViewSize);
            Assert.Equal(0.0, resizedQuery!.ViewInsets.Bottom);
            Assert.Equal(0.0, resizedQuery.Padding.Bottom);
            Assert.Contains(resized.FindWidgets<Padding>(), padding => padding.Insets.Bottom == 200.0);
        }

        MediaQueryData? unresizedQuery = null;
        using (var unresized = new CupertinoThemeTestHarness(BuildRoot(
                   new CupertinoTabScaffold(
                       tabBar: BuildTabBar(2),
                       resizeToAvoidBottomInset: false,
                       tabBuilder: (context, _) => new CaptureContext(
                           captured => unresizedQuery = MediaQuery.Of(captured))),
                   viewInsets: new Thickness(0.0, 0.0, 0.0, 200.0))))
        {
            unresized.Pump(ViewSize);
            Assert.Equal(200.0, unresizedQuery!.ViewInsets.Bottom);
            Assert.Equal(50.0, unresizedQuery.Padding.Bottom);
            Assert.Contains(unresized.FindWidgets<Padding>(), padding => padding.Insets == default);
        }

        Assert.Equal(0.0, ContentBottomPadding(alpha: byte.MaxValue));
        Assert.Equal(70.0, ContentBottomPadding(alpha: 0xAA));

        MediaQueryData? nestedPageQuery = null;
        using var nested = new CupertinoThemeTestHarness(BuildRoot(
            new CupertinoTabScaffold(
                tabBar: BuildTabBar(2),
                tabBuilder: (_, _) => new CupertinoPageScaffold(
                    child: new CaptureContext(context => nestedPageQuery = MediaQuery.Of(context)))),
            viewInsets: new Thickness(0.0, 0.0, 0.0, 200.0)));
        nested.Pump(ViewSize);
        Assert.Equal(0.0, nestedPageQuery!.ViewInsets.Bottom);
        Assert.Equal(0.0, nestedPageQuery.Padding.Bottom);
    }

    [Fact]
    public void ContentStateSurvivesMediaQueryInsetUpdates()
    {
        var states = new List<StateIdentityProbeState>();
        Widget Build(Thickness viewInsets)
        {
            return BuildRoot(
                new CupertinoTabScaffold(
                    tabBar: BuildTabBar(2),
                    tabBuilder: (_, _) => new StateIdentityProbe(states.Add)),
                viewInsets: viewInsets);
        }

        using var harness = new CupertinoThemeTestHarness(Build(default));
        harness.Pump(ViewSize);
        StateIdentityProbeState originalState = Assert.Single(states.Distinct());

        harness.PumpWidget(Build(new Thickness(0.0, 0.0, 0.0, 100.0)));
        harness.Pump(ViewSize);

        Assert.Same(originalState, Assert.Single(states.Distinct()));
    }

    [Fact]
    public void TabBarDisablesTextScalingWithoutChangingContentScaling()
    {
        double? contentScale = null;
        double? tabScale = null;
        IReadOnlyList<BottomNavigationBarItem> items =
        [
            new BottomNavigationBarItem(
                icon: new CaptureContext(context => tabScale = MediaQuery.TextScaleFactorOf(context)),
                label: "First"),
            new BottomNavigationBarItem(new SizedBox(), "Second"),
        ];
        using var harness = new CupertinoThemeTestHarness(BuildRoot(
            new CupertinoTabScaffold(
                tabBar: new CupertinoTabBar(items),
                tabBuilder: (context, _) => new CaptureContext(
                    captured => contentScale = MediaQuery.TextScaleFactorOf(captured))),
            textScaleFactor: 3.0));

        harness.Pump(ViewSize);

        Assert.Equal(3.0, contentScale);
        Assert.Equal(1.0, tabScale);
    }

    [Fact]
    public void BackgroundResolvesDynamicallyAndZeroAreaRemainsSafe()
    {
        Color light = Color.FromUInt32(0xFF123456);
        Color dark = Color.FromUInt32(0xFF654321);
        CupertinoDynamicColor background = CupertinoDynamicColor.WithBrightness(light, dark);

        using (var lightHarness = new CupertinoThemeTestHarness(BuildRoot(
                   new CupertinoTabScaffold(
                       tabBar: BuildTabBar(2),
                       backgroundColor: background,
                       tabBuilder: (_, _) => new SizedBox()),
                   brightness: PlatformBrightness.Light)))
        {
            lightHarness.Pump(ViewSize);
            Assert.Contains(lightHarness.FindWidgets<DecoratedBox>(), widget =>
                Assert.IsType<BoxDecoration>(widget.Decoration).Color == light);
        }

        using var darkHarness = new CupertinoThemeTestHarness(BuildRoot(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoTabScaffold(
                    tabBar: BuildTabBar(2),
                    backgroundColor: background,
                    tabBuilder: (_, _) => new SizedBox())),
            brightness: PlatformBrightness.Dark));
        darkHarness.Pump(ViewSize);

        Assert.Contains(darkHarness.FindWidgets<DecoratedBox>(), widget =>
            Assert.IsType<BoxDecoration>(widget.Decoration).Color == dark);
        Assert.Equal(default, darkHarness.RenderView.Child!.Size);
    }

    [Fact]
    public void InternalController_RestoresTheSelectedTabIndex()
    {
        var rawData = RawRestorationData.Build();
        var manager = new MockRestorationManager();
        using (var first = new CupertinoThemeTestHarness(BuildRoot(
                   new CupertinoTabScaffold(
                       tabBar: BuildTabBar(3),
                       restorationId: "scaffold",
                       tabBuilder: (_, index) => new Text($"Page {index}")),
                   bucket: RestorationBucket.Root(manager, rawData))))
        {
            first.Pump(ViewSize);
            Assert.Single(first.FindWidgets<CupertinoTabBar>()).OnTap!(2);
            first.Pump(ViewSize);
            manager.DoSerialization();
            Assert.Equal(2, Assert.Single(first.FindWidgets<CupertinoTabBar>()).CurrentIndex);
        }

        using var restored = new CupertinoThemeTestHarness(BuildRoot(
            new CupertinoTabScaffold(
                tabBar: BuildTabBar(3),
                restorationId: "scaffold",
                tabBuilder: (_, index) => new Text($"Page {index}")),
            bucket: RestorationBucket.Root(manager, rawData)));
        restored.Pump(ViewSize);

        Assert.Equal(2, Assert.Single(restored.FindWidgets<CupertinoTabBar>()).CurrentIndex);
        Assert.Contains(restored.FindWidgets<Text>(), text => text.Data == "Page 2");
        Assert.DoesNotContain(restored.FindWidgets<Text>(), text => text.Data == "Page 0");
    }

    private static double ContentBottomPadding(byte alpha)
    {
        MediaQueryData? contentQuery = null;
        Color color = Color.FromArgb(alpha, 255, 255, 255);
        using var harness = new CupertinoThemeTestHarness(BuildRoot(
            new CupertinoTabScaffold(
                tabBar: BuildTabBar(2, backgroundColor: color),
                tabBuilder: (context, _) => new CaptureContext(
                    captured => contentQuery = MediaQuery.Of(captured))),
            padding: new Thickness(0.0, 0.0, 0.0, 20.0)));
        harness.Pump(ViewSize);
        return contentQuery!.Padding.Bottom;
    }

    private static CupertinoTabScaffold BuildScaffold(
        CupertinoTabController? controller,
        List<int> tapped)
    {
        return new CupertinoTabScaffold(
            tabBar: BuildTabBar(2, onTap: tapped.Add),
            controller: controller,
            tabBuilder: (_, index) => new Text($"Page {index}"));
    }

    private static CupertinoTabBar BuildTabBar(
        int count,
        int currentIndex = 0,
        Action<int>? onTap = null,
        Color? backgroundColor = null)
    {
        IReadOnlyList<BottomNavigationBarItem> items = Enumerable.Range(0, count)
            .Select(index => new BottomNavigationBarItem(new SizedBox(), $"Tab {index + 1}"))
            .ToArray();
        return new CupertinoTabBar(
            items,
            currentIndex: currentIndex,
            onTap: onTap,
            backgroundColor: backgroundColor is null
                ? null
                : CupertinoDynamicColor.WithBrightness(backgroundColor.Value, backgroundColor.Value));
    }

    private static Widget BuildRoot(
        Widget child,
        Thickness padding = default,
        Thickness viewInsets = default,
        double textScaleFactor = 1.0,
        PlatformBrightness brightness = PlatformBrightness.Light,
        RestorationBucket? bucket = null)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                Size: ViewSize,
                Padding: padding,
                ViewInsets: viewInsets,
                TextScaleFactor: textScaleFactor,
                PlatformBrightness: brightness),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    TextDirection.Ltr,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
                        new UnmanagedRestorationScope(
                            bucket: bucket,
                            child: new FocusScope(child))))));
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;

        public CaptureContext(Action<BuildContext> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return new SizedBox(width: double.PositiveInfinity, height: double.PositiveInfinity);
        }
    }

    private sealed class TrackingTabController : CupertinoTabController
    {
        public TrackingTabController(int initialIndex) : base(initialIndex)
        {
        }

        public int ListenerCount { get; private set; }

        public bool WasDisposed { get; private set; }

        public override void AddListener(Action listener)
        {
            ListenerCount++;
            base.AddListener(listener);
        }

        public override void RemoveListener(Action listener)
        {
            ListenerCount--;
            base.RemoveListener(listener);
        }

        public override void Dispose()
        {
            WasDisposed = true;
            base.Dispose();
        }
    }

    private sealed class StateIdentityProbe : StatefulWidget
    {
        public StateIdentityProbe(Action<StateIdentityProbeState> onBuild)
        {
            OnBuild = onBuild;
        }

        public Action<StateIdentityProbeState> OnBuild { get; }

        public override State CreateState() => new StateIdentityProbeState();
    }

    private sealed class StateIdentityProbeState : State
    {
        public override Widget Build(BuildContext context)
        {
            ((StateIdentityProbe)StateWidget).OnBuild(this);
            return new SizedBox();
        }
    }
}
