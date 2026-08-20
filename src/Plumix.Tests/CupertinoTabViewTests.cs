using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/tab_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTabViewTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 480.0);

    public CupertinoTabViewTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void Constructor_ExposesFlutterDefaultsAndConfiguration()
    {
        var defaults = new CupertinoTabView();

        Assert.Null(defaults.Builder);
        Assert.Null(defaults.NavigatorKey);
        Assert.Null(defaults.DefaultTitle);
        Assert.Null(defaults.Routes);
        Assert.Null(defaults.OnGenerateRoute);
        Assert.Null(defaults.OnUnknownRoute);
        Assert.Empty(defaults.NavigatorObservers);
        Assert.Null(defaults.RestorationScopeId);

        WidgetBuilder builder = _ => new Text("home");
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("tab");
        IReadOnlyDictionary<string, WidgetBuilder> routes = new Dictionary<string, WidgetBuilder>
        {
            ["/second"] = _ => new Text("second"),
        };
        RouteFactory generate = _ => null;
        RouteFactory unknown = settings => new CupertinoPageRoute<object?>(
            _ => new Text(settings.Name ?? "unknown"));
        IReadOnlyList<NavigatorObserver> observers = [new TrackingNavigatorObserver()];
        var configured = new CupertinoTabView(
            builder: builder,
            navigatorKey: navigatorKey,
            defaultTitle: "Root",
            routes: routes,
            onGenerateRoute: generate,
            onUnknownRoute: unknown,
            navigatorObservers: observers,
            restorationScopeId: "tab-restoration");

        Assert.Same(builder, configured.Builder);
        Assert.Same(navigatorKey, configured.NavigatorKey);
        Assert.Equal("Root", configured.DefaultTitle);
        Assert.Same(routes, configured.Routes);
        Assert.Same(generate, configured.OnGenerateRoute);
        Assert.Same(unknown, configured.OnUnknownRoute);
        Assert.Same(observers, configured.NavigatorObservers);
        Assert.Equal("tab-restoration", configured.RestorationScopeId);
    }

    [Fact]
    public void Routing_UsesBuilderRoutesGenerateAndUnknownInFlutterOrder()
    {
        bool generated = false;
        bool unknown = false;
        var generatedRoute = new CupertinoPageRoute<object?>(_ => new Text("generated"));
        var unknownRoute = new CupertinoPageRoute<object?>(_ => new Text("unknown"));
        var tabView = new CupertinoTabView(
            builder: _ => new Text("builder home"),
            defaultTitle: "Root title",
            routes: new Dictionary<string, WidgetBuilder>
            {
                [Navigator.DefaultRouteName] = _ => new Text("routes home"),
                ["/second"] = _ => new Text("routes second"),
            },
            onGenerateRoute: settings =>
            {
                generated = true;
                return string.Equals(settings.Name, "/generated", StringComparison.Ordinal)
                    ? generatedRoute
                    : null;
            },
            onUnknownRoute: _ =>
            {
                unknown = true;
                return unknownRoute;
            });
        using var harness = new CupertinoThemeTestHarness(Wrap(tabView));
        harness.Pump(ViewSize);

        Navigator navigator = Assert.Single(harness.FindWidgets<Navigator>());
        var rootSettings = new RouteSettings(Navigator.DefaultRouteName);
        var rootRoute = Assert.IsType<CupertinoPageRoute<object?>>(navigator.OnGenerateRoute!(rootSettings));
        Assert.Equal("Root title", rootRoute.Title);
        Assert.Same(rootSettings, rootRoute.Settings);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "builder home");

        var namedSettings = new RouteSettings("/second");
        var namedRoute = Assert.IsType<CupertinoPageRoute<object?>>(navigator.OnGenerateRoute(namedSettings));
        Assert.Null(namedRoute.Title);
        Assert.Same(namedSettings, namedRoute.Settings);

        Assert.Same(generatedRoute, navigator.OnGenerateRoute(new RouteSettings("/generated")));
        Assert.True(generated);
        Assert.Null(navigator.OnGenerateRoute(new RouteSettings("/missing")));
        Assert.Same(unknownRoute, navigator.OnUnknownRoute!(new RouteSettings("/missing")));
        Assert.True(unknown);
    }

    [Fact]
    public void NavigatorKey_ControlsIndependentHistoryObserversHeroesAndRestoration()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("tab");
        var observer = new TrackingNavigatorObserver();
        var tabView = new CupertinoTabView(
            navigatorKey: navigatorKey,
            builder: _ => new Text("home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("second"),
            },
            navigatorObservers: [observer],
            restorationScopeId: "tab");
        var harness = new CupertinoThemeTestHarness(Wrap(tabView));
        harness.Pump(ViewSize);

        Navigator navigator = Assert.Single(harness.FindWidgets<Navigator>());
        Assert.Same(navigatorKey.CurrentState, observer.Navigator);
        Assert.Equal("tab", navigator.RestorationScopeId);
        Assert.Same(observer, navigator.Observers[0]);
        HeroController heroController = Assert.IsType<HeroController>(navigator.Observers[1]);
        Assert.False(heroController.IsDisposed);
        Assert.Equal(1, observer.PushCount);

        navigatorKey.CurrentState!.PushNamed("/second");
        harness.Pump(ViewSize);

        Assert.Equal("/second", navigatorKey.CurrentState.CurrentRoute!.Settings.Name);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "second");
        Assert.Equal(2, observer.PushCount);

        harness.PumpWidget(new SizedBox());
        harness.Dispose();

        Assert.True(heroController.IsDisposed);
        Assert.Null(observer.Navigator);
        Assert.Null(navigatorKey.CurrentState);
    }

    [Fact]
    public void UpdatingBuilderPreservesHistoryWhileChangingNavigatorKeyResetsIt()
    {
        var firstKey = new LabeledGlobalKey<NavigatorState>("first tab navigator");
        var secondKey = new LabeledGlobalKey<NavigatorState>("second tab navigator");
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabView(
            navigatorKey: firstKey,
            builder: _ => new Text("first home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("second route"),
            })));
        harness.Pump(ViewSize);
        NavigatorState firstState = firstKey.CurrentState!;
        firstState.PushNamed("/second");
        harness.Pump(ViewSize);

        harness.PumpWidget(Wrap(new CupertinoTabView(
            navigatorKey: firstKey,
            builder: _ => new Text("updated home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("updated second route"),
            })));
        harness.Pump(ViewSize);

        Assert.Same(firstState, firstKey.CurrentState);
        Assert.Equal("/second", firstKey.CurrentState!.CurrentRoute!.Settings.Name);
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "updated home");

        harness.PumpWidget(Wrap(new CupertinoTabView(
            navigatorKey: secondKey,
            builder: _ => new Text("reset home"))));
        harness.Pump(ViewSize);

        Assert.Null(firstKey.CurrentState);
        Assert.NotSame(firstState, secondKey.CurrentState);
        Assert.Equal(Navigator.DefaultRouteName, secondKey.CurrentState!.CurrentRoute!.Settings.Name);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "reset home");
    }

    [Fact]
    public void ChangingTabViewKeyReplacesTheOwnedNavigatorAndClearsHistory()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabView(
            key: new ValueKey<string>("first tab"),
            builder: _ => new Text("home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("second route"),
            })));
        harness.Pump(ViewSize);
        Navigator firstNavigator = Assert.Single(harness.FindWidgets<Navigator>());
        var firstOwnedKey = Assert.IsAssignableFrom<GlobalKey<NavigatorState>>(firstNavigator.Key);
        firstOwnedKey.CurrentState!.PushNamed("/second");
        harness.Pump(ViewSize);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "second route");

        harness.PumpWidget(Wrap(new CupertinoTabView(
            key: new ValueKey<string>("replacement tab"),
            builder: _ => new Text("replacement home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("replacement second route"),
            })));
        harness.Pump(ViewSize);

        Navigator replacementNavigator = Assert.Single(harness.FindWidgets<Navigator>());
        var replacementOwnedKey = Assert.IsAssignableFrom<GlobalKey<NavigatorState>>(replacementNavigator.Key);
        Assert.NotSame(firstOwnedKey, replacementOwnedKey);
        Assert.Null(firstOwnedKey.CurrentState);
        Assert.Equal(Navigator.DefaultRouteName, replacementOwnedKey.CurrentState!.CurrentRoute!.Settings.Name);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "replacement home");
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "second route");
    }

    [Fact]
    public void RestorableNamedHistoryIsRecoveredByANewTabNavigator()
    {
        var rawData = RawRestorationData.Build();
        var firstManager = new MockRestorationManager();
        var firstRootBucket = RestorationBucket.Root(firstManager, rawData);
        var firstKey = new LabeledGlobalKey<NavigatorState>("first restorable tab");
        using var firstHarness = new CupertinoThemeTestHarness(new UnmanagedRestorationScope(
            bucket: firstRootBucket,
            child: Wrap(BuildRestorableTab(firstKey))));
        firstHarness.Pump(ViewSize);

        firstKey.CurrentState!.RestorablePushNamed("/second");
        firstHarness.Pump(ViewSize);
        firstManager.DoSerialization();
        Assert.Equal("/second", firstKey.CurrentState.CurrentRoute!.Settings.Name);
        Assert.NotNull(RawRestorationData.Child(rawData, "tab"));

        var secondManager = new MockRestorationManager();
        var secondRootBucket = RestorationBucket.Root(secondManager, rawData);
        var secondKey = new LabeledGlobalKey<NavigatorState>("second restorable tab");
        using var secondHarness = new CupertinoThemeTestHarness(new UnmanagedRestorationScope(
            bucket: secondRootBucket,
            child: Wrap(BuildRestorableTab(secondKey))));
        secondHarness.Pump(ViewSize);

        Assert.Equal("/second", secondKey.CurrentState!.CurrentRoute!.Settings.Name);
        Assert.Contains(secondHarness.FindWidgets<Text>(), text => text.Data == "second");
    }

    [Fact]
    public void UnknownRoutesThrowAndInactiveOrZeroAreaTabsRemainSafe()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("tab");
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoTabView(
                navigatorKey: navigatorKey,
                builder: _ => new Text("home"),
                onUnknownRoute: _ => null),
            tickerEnabled: false));
        var zeroSize = new Size(0.0, 0.0);
        harness.Pump(zeroSize);

        var popHandler = Assert.Single(harness.FindWidgets<NavigatorPopHandler<object?>>());
        Assert.False(popHandler.Enabled);
        Assert.Equal(zeroSize, harness.RenderView.Size);
        Assert.Throws<InvalidOperationException>(() => navigatorKey.CurrentState!.PushNamed("/missing"));

        using var missingHandlerHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabView(
            navigatorKey: new LabeledGlobalKey<NavigatorState>("missing handler"),
            builder: _ => new Text("home"))));
        missingHandlerHarness.Pump(ViewSize);
        Navigator missingHandlerNavigator = Assert.Single(missingHandlerHarness.FindWidgets<Navigator>());
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => missingHandlerNavigator.OnUnknownRoute!(new RouteSettings("/missing")));
        Assert.Contains("OnUnknownRoute was not set", error.Message, StringComparison.Ordinal);
    }

    private static Widget Wrap(Widget child, bool tickerEnabled = true)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: ViewSize),
            child: new Directionality(
                TextDirection.Ltr,
                new TickerMode(
                    child: child,
                    enabled: tickerEnabled)));
    }

    private static CupertinoTabView BuildRestorableTab(GlobalKey<NavigatorState> navigatorKey)
    {
        return new CupertinoTabView(
            navigatorKey: navigatorKey,
            restorationScopeId: "tab",
            builder: _ => new Text("home"),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/second"] = _ => new Text("second"),
            });
    }

    private sealed class TrackingNavigatorObserver : NavigatorObserver
    {
        public int PushCount { get; private set; }

        public override void DidPush(Route route, Route? previousRoute)
        {
            PushCount++;
        }
    }
}
