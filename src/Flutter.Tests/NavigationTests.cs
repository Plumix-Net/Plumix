using Flutter.Rendering;
using Flutter.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/navigator.dart; flutter/packages/flutter/lib/src/widgets/routes.dart (parity regression tests)

namespace Flutter.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class NavigationTests
{
    public NavigationTests()
    {
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void Navigator_PushAndPop_UpdatesCurrentRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: name => currentPageName = name,
                    captureState: state => navigatorState ??= state)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState!.CanPop);

        navigatorState.Push(BuildRoute(
            name: "details",
            onBuild: name => currentPageName = name,
            captureState: _ => { }));
        owner.FlushBuild();

        Assert.Equal("details", currentPageName);
        Assert.True(navigatorState.CanPop);

        Assert.True(navigatorState.MaybePop());
        owner.FlushBuild();

        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void Navigator_PushReplacement_ReplacesTopRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: name => currentPageName = name,
                    captureState: state => navigatorState ??= state)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.Equal("root", currentPageName);

        navigatorState!.PushReplacement(BuildRoute(
            name: "replacement",
            onBuild: name => currentPageName = name,
            captureState: _ => { }));
        owner.FlushBuild();

        Assert.Equal("replacement", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void NavigatorObserver_ReceivesPushPopAndReplace()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var observer = new RecordingObserver();

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: _ => { },
                    captureState: state => navigatorState ??= state),
                observers: [observer]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.Push(BuildRoute(
            name: "details",
            onBuild: _ => { },
            captureState: _ => { }));
        owner.FlushBuild();

        navigatorState.PushReplacement(BuildRoute(
            name: "replacement",
            onBuild: _ => { },
            captureState: _ => { }));
        owner.FlushBuild();

        Assert.True(navigatorState.MaybePop());
        owner.FlushBuild();

        Assert.Equal(
            [
                "push:root:null",
                "push:details:root",
                "replace:replacement:details",
                "pop:replacement:root"
            ],
            observer.Events);
    }

    [Fact]
    public void ModalRoute_Of_ReturnsCurrentRouteInsidePageBuilder()
    {
        var owner = new BuildOwner();
        ModalRoute? capturedRoute = null;

        var initialRoute = new BuilderPageRoute(
            builder: context =>
            {
                capturedRoute = ModalRoute.Of(context);
                return new SizedBox(width: 1, height: 1);
            },
            settings: new RouteSettings(Name: "/root"));

        var root = new TestRootElement(
            new Navigator(initialRoute: initialRoute));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedRoute);
        Assert.Same(initialRoute, capturedRoute);
        Assert.Equal("/root", capturedRoute!.Settings.Name);
    }

    [Fact]
    public void RouteObserver_Subscribe_ReceivesPushNextPopNextAndPop()
    {
        var owner = new BuildOwner();
        var routeObserver = new RouteObserver<PageRoute>();
        NavigatorState? navigatorState = null;
        PageRoute? rootRoute = null;
        PageRoute? detailsRoute = null;

        Route CreateRoute(string routeName, RouteSettings settings)
        {
            var route = new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);

            if (routeName == "/")
            {
                rootRoute = route;
            }
            else if (routeName == "/details")
            {
                detailsRoute = route;
            }

            return route;
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => CreateRoute("/", settings),
                "/details" => CreateRoute("/details", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/",
                observers: [routeObserver]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.NotNull(rootRoute);

        var rootAware = new RecordingRouteAware();
        routeObserver.Subscribe(rootAware, rootRoute!);
        Assert.Equal(["didPush"], rootAware.Events);

        navigatorState!.PushNamed("/details");
        owner.FlushBuild();

        Assert.NotNull(detailsRoute);
        Assert.Equal(["didPush", "didPushNext"], rootAware.Events);

        var detailsAware = new RecordingRouteAware();
        routeObserver.Subscribe(detailsAware, detailsRoute!);
        Assert.Equal(["didPush"], detailsAware.Events);

        navigatorState.Pop();
        owner.FlushBuild();

        Assert.Equal(["didPush", "didPushNext", "didPopNext"], rootAware.Events);
        Assert.Equal(["didPush", "didPop"], detailsAware.Events);
    }

    [Fact]
    public void RouteObserver_DidReplace_RebindsSubscribersToReplacementRoute()
    {
        var owner = new BuildOwner();
        var routeObserver = new RouteObserver<PageRoute>();
        NavigatorState? navigatorState = null;
        PageRoute? detailsRoute = null;

        Route CreateRoute(string routeName, RouteSettings settings)
        {
            var route = new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);

            if (routeName == "/details")
            {
                detailsRoute = route;
            }

            return route;
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => CreateRoute("/", settings),
                "/details" => CreateRoute("/details", settings),
                "/replacement" => CreateRoute("/replacement", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/",
                observers: [routeObserver]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushNamed("/details");
        owner.FlushBuild();
        Assert.NotNull(detailsRoute);

        var aware = new RecordingRouteAware();
        routeObserver.Subscribe(aware, detailsRoute!);
        Assert.Equal(["didPush"], aware.Events);

        navigatorState.PushReplacementNamed("/replacement");
        owner.FlushBuild();

        navigatorState.Pop();
        owner.FlushBuild();

        Assert.Equal(["didPush", "didPop"], aware.Events);
    }

    [Fact]
    public void RouteObserver_Unsubscribe_StopsFurtherNotifications()
    {
        var owner = new BuildOwner();
        var routeObserver = new RouteObserver<PageRoute>();
        NavigatorState? navigatorState = null;
        PageRoute? rootRoute = null;

        Route CreateRoute(string routeName, RouteSettings settings)
        {
            var route = new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);

            if (routeName == "/")
            {
                rootRoute = route;
            }

            return route;
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => CreateRoute("/", settings),
                "/details" => CreateRoute("/details", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/",
                observers: [routeObserver]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(rootRoute);
        Assert.NotNull(navigatorState);

        var aware = new RecordingRouteAware();
        routeObserver.Subscribe(aware, rootRoute!);
        routeObserver.Unsubscribe(aware);

        navigatorState!.PushNamed("/details");
        owner.FlushBuild();

        Assert.Equal(["didPush"], aware.Events);
    }

    [Fact]
    public void Navigator_TryHandleBackButton_PopsWhenPossible()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: name => currentPageName = name,
                    captureState: state => navigatorState ??= state)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.Push(BuildRoute(
            name: "details",
            onBuild: name => currentPageName = name,
            captureState: _ => { }));
        owner.FlushBuild();
        Assert.Equal("details", currentPageName);

        Assert.True(Navigator.TryHandleBackButton());
        owner.FlushBuild();

        Assert.Equal("root", currentPageName);
        Assert.False(Navigator.TryHandleBackButton());
    }

    [Fact]
    public void Navigator_MaybePop_HandlesLocalHistoryOnRootRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        ModalRoute? rootRoute = null;
        var localHistoryRemoved = false;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        rootRoute ??= ModalRoute.Of(context);
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: new RouteSettings(Name: "/"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.NotNull(rootRoute);
        Assert.False(navigatorState!.CanPop);

        rootRoute!.AddLocalHistoryEntry(new LocalHistoryEntry(onRemove: () => localHistoryRemoved = true));
        Assert.True(rootRoute.ImpliesAppBarDismissal);

        Assert.True(navigatorState.MaybePop());
        Assert.True(localHistoryRemoved);
        Assert.Same(rootRoute, navigatorState.CurrentRoute);
        Assert.False(navigatorState.CanPop);
        Assert.False(navigatorState.MaybePop());
    }

    [Fact]
    public void NavigatorObserver_UserGestureStartStop_ReceivesCallbacks()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var observer = new RecordingObserver();

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: _ => { },
                    captureState: state => navigatorState ??= state),
                observers: [observer]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.Push(BuildRoute(
            name: "details",
            onBuild: _ => { },
            captureState: _ => { }));
        owner.FlushBuild();

        navigatorState.StartUserGesture();
        navigatorState.StopUserGesture();

        Assert.Equal(
            [
                "push:root:null",
                "push:details:root",
                "gesture-start:details:root",
                "gesture-stop"
            ],
            observer.Events);
    }

    [Fact]
    public void Navigator_UserGesture_NestedStartStop_NotifiesOnce()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var observer = new RecordingObserver();

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: _ => { },
                    captureState: state => navigatorState ??= state),
                observers: [observer]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.StartUserGesture();
        navigatorState.StartUserGesture();
        Assert.True(navigatorState.UserGestureInProgress);
        navigatorState.StopUserGesture();
        Assert.True(navigatorState.UserGestureInProgress);
        navigatorState.StopUserGesture();
        Assert.False(navigatorState.UserGestureInProgress);

        Assert.Equal(
            [
                "push:root:null",
                "gesture-start:root:null",
                "gesture-stop"
            ],
            observer.Events);
    }

    [Fact]
    public void Navigator_MaybePopFromUserGesture_PopsAndStopsGesture()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;
        var observer = new RecordingObserver();

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: name => currentPageName = name,
                    captureState: state => navigatorState ??= state),
                observers: [observer]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.Push(BuildRoute(
            name: "details",
            onBuild: name => currentPageName = name,
            captureState: _ => { }));
        owner.FlushBuild();

        Assert.True(navigatorState.MaybePopFromUserGesture());
        owner.FlushBuild();

        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState.UserGestureInProgress);
        Assert.Equal(
            [
                "push:root:null",
                "push:details:root",
                "gesture-start:details:root",
                "pop:details:root",
                "gesture-stop"
            ],
            observer.Events);
    }

    [Fact]
    public void Navigator_NamedRoutes_ResolvesInitialAndPushNamed()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;
        object? latestArguments = null;

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        currentPageName = "menu";
                        latestArguments = settings.Arguments;
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        currentPageName = "details";
                        latestArguments = settings.Arguments;
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.Equal("menu", currentPageName);
        Assert.Null(latestArguments);

        navigatorState!.PushNamed("/details", arguments: 42);
        owner.FlushBuild();

        Assert.Equal("details", currentPageName);
        Assert.Equal(42, latestArguments);

        Assert.Throws<InvalidOperationException>(() => navigatorState.PushNamed("/missing"));
    }

    [Fact]
    public void Navigator_StaticApi_PushNamedAndPop_WorkThroughBuildContext()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;
        BuildContext? detailsContext = null;
        var currentPageName = string.Empty;
        object? latestArguments = null;

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext = context;
                        currentPageName = "root";
                        latestArguments = settings.Arguments;
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: context =>
                    {
                        detailsContext = context;
                        currentPageName = "details";
                        latestArguments = settings.Arguments;
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Assert.Equal("root", currentPageName);
        Assert.False(Navigator.CanPop(rootContext.Value));

        Navigator.PushNamed(rootContext.Value, "/details", arguments: 99);
        owner.FlushBuild();

        Assert.True(detailsContext.HasValue);
        Assert.Equal("details", currentPageName);
        Assert.Equal(99, latestArguments);
        Assert.True(Navigator.CanPop(detailsContext.Value));

        Navigator.Pop(detailsContext.Value);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Assert.Equal("root", currentPageName);
        Assert.False(Navigator.CanPop(rootContext.Value));
    }

    [Fact]
    public void Navigator_PushReplacementNamed_ReplacesTopRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        currentPageName = "root";
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                "/replacement" => new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        currentPageName = "replacement";
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushReplacementNamed("/replacement");
        owner.FlushBuild();

        Assert.Equal("replacement", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void Navigator_PopUntil_PopsUntilPredicateMatches()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: BuildRoute(
                    name: "root",
                    onBuild: name => currentPageName = name,
                    captureState: state => navigatorState ??= state)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.Push(BuildRoute("a", name => currentPageName = name, _ => { }));
        owner.FlushBuild();
        navigatorState.Push(BuildRoute("b", name => currentPageName = name, _ => { }));
        owner.FlushBuild();
        navigatorState.Push(BuildRoute("c", name => currentPageName = name, _ => { }));
        owner.FlushBuild();
        Assert.Equal("c", currentPageName);

        navigatorState.PopUntil(route => route.Settings.Name == "a");
        owner.FlushBuild();

        Assert.Equal("a", currentPageName);
        Assert.True(navigatorState.CanPop);
    }

    [Fact]
    public void Navigator_PushNamedAndRemoveUntil_RemovesIntermediateRoutes()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;

        Route BuildNamedRoute(string pageName, RouteSettings settings)
        {
            return new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    currentPageName = pageName;
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => BuildNamedRoute("root", settings),
                "/a" => BuildNamedRoute("a", settings),
                "/b" => BuildNamedRoute("b", settings),
                "/target" => BuildNamedRoute("target", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushNamed("/a");
        owner.FlushBuild();
        navigatorState.PushNamed("/b");
        owner.FlushBuild();
        Assert.Equal("b", currentPageName);

        navigatorState.PushNamedAndRemoveUntil("/target", route => route.Settings.Name == "/");
        owner.FlushBuild();

        Assert.Equal("target", currentPageName);
        Assert.True(navigatorState.CanPop);

        Assert.True(navigatorState.MaybePop());
        owner.FlushBuild();
        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void NavigatorObserver_DidRemove_ReceivesEventsFromPushNamedAndRemoveUntil()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var observer = new RecordingObserver();

        Route BuildNamedRoute(RouteSettings settings)
        {
            return new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => BuildNamedRoute(settings),
                "/a" => BuildNamedRoute(settings),
                "/b" => BuildNamedRoute(settings),
                "/target" => BuildNamedRoute(settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/",
                observers: [observer]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushNamed("/a");
        owner.FlushBuild();
        navigatorState.PushNamed("/b");
        owner.FlushBuild();
        navigatorState.PushNamedAndRemoveUntil("/target", route => route.Settings.Name == "/");
        owner.FlushBuild();

        Assert.Equal(
            [
                "push:/:null",
                "push:/a:/",
                "push:/b:/a",
                "push:/target:/b",
                "remove:/b:/a",
                "remove:/a:/"
            ],
            observer.Events);
    }

    [Fact]
    public void Navigator_RemoveRoute_RemovesSpecificRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;
        var routesByName = new Dictionary<string, Route>(StringComparer.Ordinal);

        Route BuildNamedRoute(string pageName, RouteSettings settings)
        {
            var route = new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    currentPageName = pageName;
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);

            routesByName[settings.Name ?? string.Empty] = route;
            return route;
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => BuildNamedRoute("root", settings),
                "/a" => BuildNamedRoute("a", settings),
                "/b" => BuildNamedRoute("b", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushNamed("/a");
        owner.FlushBuild();
        navigatorState.PushNamed("/b");
        owner.FlushBuild();
        Assert.Equal("b", currentPageName);

        navigatorState.RemoveRoute(routesByName["/a"]);
        owner.FlushBuild();
        Assert.Equal("b", currentPageName);
        Assert.True(navigatorState.CanPop);

        navigatorState.Pop();
        owner.FlushBuild();
        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void Navigator_RemoveRouteBelow_RemovesRouteUnderAnchor()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        var currentPageName = string.Empty;
        var routesByName = new Dictionary<string, Route>(StringComparer.Ordinal);

        Route BuildNamedRoute(string pageName, RouteSettings settings)
        {
            var route = new BuilderPageRoute(
                builder: context =>
                {
                    navigatorState ??= Navigator.Of(context);
                    currentPageName = pageName;
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);

            routesByName[settings.Name ?? string.Empty] = route;
            return route;
        }

        Route? OnGenerateRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => BuildNamedRoute("root", settings),
                "/a" => BuildNamedRoute("a", settings),
                "/b" => BuildNamedRoute("b", settings),
                _ => null
            };
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteName: "/"));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        navigatorState!.PushNamed("/a");
        owner.FlushBuild();
        navigatorState.PushNamed("/b");
        owner.FlushBuild();

        navigatorState.RemoveRouteBelow(routesByName["/b"]);
        owner.FlushBuild();

        navigatorState.Pop();
        owner.FlushBuild();

        Assert.Equal("root", currentPageName);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void Navigator_RemoveRoute_ThrowsWhenRemovingLastRoute()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        Route? rootRoute = null;

        var root = new TestRootElement(
            new Navigator(
                initialRoute: new BuilderPageRoute(
                    builder: context =>
                    {
                        navigatorState ??= Navigator.Of(context);
                        rootRoute ??= ModalRoute.Of(context);
                        return new SizedBox(width: 1, height: 1);
                    },
                    settings: new RouteSettings(Name: "/"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigatorState);
        Assert.NotNull(rootRoute);
        Assert.Throws<InvalidOperationException>(() => navigatorState!.RemoveRoute(rootRoute!));
    }

    [Fact]
    public void Navigator_InitialRouteData_PassesParsedQueryAndArguments()
    {
        var owner = new BuildOwner();
        RouteData? capturedRouteData = null;
        var currentPageName = string.Empty;

        Route? OnGenerateRoute(RouteSettings settings)
        {
            if (settings.Name != "/details")
            {
                return null;
            }

            return new BuilderPageRoute(
                builder: _ =>
                {
                    capturedRouteData = Assert.IsType<RouteData>(settings.Arguments);
                    currentPageName = settings.Name ?? string.Empty;
                    return new SizedBox(width: 1, height: 1);
                },
                settings: settings);
        }

        var root = new TestRootElement(
            new Navigator(
                onGenerateRoute: OnGenerateRoute,
                initialRouteData: RouteData.FromLocation("/details?id=42&tab=main", arguments: "payload")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal("/details", currentPageName);
        Assert.NotNull(capturedRouteData);
        Assert.Equal("/details", capturedRouteData!.Name);
        Assert.Equal("42", capturedRouteData.QueryParameters["id"]);
        Assert.Equal("main", capturedRouteData.QueryParameters["tab"]);
        Assert.Equal("payload", capturedRouteData.Arguments);
    }

    private static Route BuildRoute(
        string name,
        Action<string> onBuild,
        Action<NavigatorState> captureState)
    {
        return new BuilderPageRoute(
            builder: context =>
            {
                captureState(Navigator.Of(context));
                onBuild(name);
                return new SizedBox(width: 1, height: 1);
            },
            settings: new RouteSettings(Name: name));
    }

    private sealed class RecordingObserver : NavigatorObserver
    {
        public List<string> Events { get; } = [];

        public override void DidPush(Route route, Route? previousRoute)
        {
            Events.Add($"push:{route.Settings.Name}:{previousRoute?.Settings.Name ?? "null"}");
        }

        public override void DidPop(Route route, Route? previousRoute)
        {
            Events.Add($"pop:{route.Settings.Name}:{previousRoute?.Settings.Name ?? "null"}");
        }

        public override void DidRemove(Route route, Route? previousRoute)
        {
            Events.Add($"remove:{route.Settings.Name}:{previousRoute?.Settings.Name ?? "null"}");
        }

        public override void DidReplace(Route newRoute, Route? oldRoute)
        {
            Events.Add($"replace:{newRoute.Settings.Name}:{oldRoute?.Settings.Name ?? "null"}");
        }

        public override void DidStartUserGesture(Route route, Route? previousRoute)
        {
            Events.Add($"gesture-start:{route.Settings.Name}:{previousRoute?.Settings.Name ?? "null"}");
        }

        public override void DidStopUserGesture()
        {
            Events.Add("gesture-stop");
        }
    }

    private sealed class RecordingRouteAware : RouteAware
    {
        public List<string> Events { get; } = [];

        public void DidPush()
        {
            Events.Add("didPush");
        }

        public void DidPop()
        {
            Events.Add("didPop");
        }

        public void DidPopNext()
        {
            Events.Add("didPopNext");
        }

        public void DidPushNext()
        {
            Events.Add("didPushNext");
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
