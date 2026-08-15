using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/router.dart
// flutter/packages/flutter/test/widgets/router_test.dart
// flutter/packages/flutter/test/widgets/router_restoration_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class RouterTests : IDisposable
{
    private readonly RestorationManager _previousManager = RestorationManager.Instance;

    public RouterTests()
    {
        ResetEnvironment();
    }

    public void Dispose()
    {
        RestorationManager.Instance = _previousManager;
        ResetEnvironment();
    }

    private static void ResetEnvironment()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
        WidgetsBinding.Instance.ResetObserversForTests();
        SystemNavigator.ResetForTests();
    }

    private static void Pump(RestorationHarness harness)
    {
        harness.FlushBuild();
        Scheduler.FlushMicrotasks();
        harness.FlushBuild();
        Scheduler.PumpFrameForTests();
        harness.FlushBuild();
    }

    private static async Task PumpUntil(RestorationHarness harness, Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        Pump(harness);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
            Pump(harness);
        }

        Assert.True(condition(), "Timed out waiting for the router to settle.");
    }

    /// <summary>
    /// Reads a back-button result that Flutter delivers through a <c>SynchronousFuture</c>: the task must
    /// already be complete, so reading it never blocks.
    /// </summary>
    private static bool SynchronousResultOf(Task<bool> task)
    {
        Assert.True(task.IsCompletedSuccessfully, "The back button result was not delivered synchronously.");
        return task.GetAwaiter().GetResult();
    }

    private static RouteInformation Info(string uri, object? state = null)
    {
        return new RouteInformation(new Uri(uri, UriKind.RelativeOrAbsolute), state);
    }

    // ---------------------------------------------------------------- basics

    [Fact]
    public void Router_ParsesTheInitialInformationAndReparsesOnProviderUpdates()
    {
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser();
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate(
            (_, information) =>
            {
                built = information.Uri.ToString();
                return new SizedBox();
            });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser));
        Pump(harness);

        Assert.Equal("initial", built);
        Assert.Equal(1, parser.ParseCount);
        Assert.Equal(["initial"], routerDelegate.InitialRoutePaths.Select(path => path.Uri.ToString()));

        provider.SetValue(Info("update"));
        Pump(harness);

        Assert.Equal("update", built);
        Assert.Equal(2, parser.ParseCount);
        Assert.Equal(["update"], routerDelegate.NewRoutePaths.Select(path => path.Uri.ToString()));
    }

    [Fact]
    public async Task Router_WaitsForAnAsynchronousParseBeforeRebuilding()
    {
        var parseCompleter = new TaskCompletionSource<RouteInformation>();
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser((_, _) => parseCompleter.Task);
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate((_, information) =>
        {
            built = information.Uri.ToString();
            return new SizedBox();
        });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser));
        Pump(harness);

        Assert.Equal("waiting", built);
        Assert.Empty(routerDelegate.InitialRoutePaths);

        parseCompleter.SetResult(Info("parsed"));
        await PumpUntil(harness, () => built == "parsed");
        Assert.Equal(["parsed"], routerDelegate.InitialRoutePaths.Select(path => path.Uri.ToString()));
    }

    [Fact]
    public async Task Router_WaitsForAnAsynchronousDelegateBeforeRebuilding()
    {
        var setCompleter = new TaskCompletionSource();
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate(
            (_, information) =>
            {
                built = information.Uri.ToString();
                return new SizedBox();
            },
            onSetNewRoutePath: _ => setCompleter.Task);

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: new SimpleRouteInformationParser()));
        Pump(harness);
        Assert.Equal("initial", built);

        provider.SetValue(Info("update"));
        Pump(harness);
        Assert.Equal(["update"], routerDelegate.NewRoutePaths.Select(path => path.Uri.ToString()));
        Assert.Equal("initial", built);

        setCompleter.SetResult();
        await PumpUntil(harness, () => built == "update");
        Assert.Equal("update", built);
    }

    [Fact]
    public async Task Router_DropsAParseThatWasInterruptedByANewerOne()
    {
        var firstParse = new TaskCompletionSource<RouteInformation>();
        var secondParse = new TaskCompletionSource<RouteInformation>();
        var pending = new Queue<TaskCompletionSource<RouteInformation>>([firstParse, secondParse]);
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser((_, _) => pending.Dequeue().Task);
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate(
            (_, information) =>
            {
                built = information.Uri.ToString();
                return new SizedBox();
            });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser));
        Pump(harness);

        provider.SetValue(Info("second"));
        Pump(harness);

        firstParse.SetResult(Info("stale"));
        await Task.Delay(20);
        Pump(harness);
        Assert.Equal("waiting", built);

        secondParse.SetResult(Info("fresh"));
        await PumpUntil(harness, () => built == "fresh");
        Assert.Equal("fresh", built);
        Assert.DoesNotContain(routerDelegate.NewRoutePaths, path => path.Uri.ToString() == "stale");
    }

    [Fact]
    public void Router_MaybeOfIsNullOutsideARouterAndOfThrows()
    {
        Router<RouteInformation>? seen = null;
        InvalidOperationException? failure = null;

        using var harness = new RestorationHarness(new Builder(context =>
        {
            seen = Router.MaybeOf<RouteInformation>(context);
            failure = Record.Exception(() => Router.Of<RouteInformation>(context))
                as InvalidOperationException;
            return new SizedBox();
        }));

        Assert.Null(seen);
        Assert.NotNull(failure);
        Assert.StartsWith("Router operation requested", failure!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Router_OfReturnsTheEnclosingRouterWidget()
    {
        var routerDelegate = new SimpleRouterDelegate((context, _) =>
        {
            Assert.NotNull(Router.MaybeOf<RouteInformation>(context));
            return new SizedBox();
        });
        var router = new Router<RouteInformation>(routerDelegate: routerDelegate);

        using var harness = new RestorationHarness(router);
        Pump(harness);
    }

    [Fact]
    public void Router_ThrowsWhenAProviderIsGivenWithoutAParser()
    {
        ArgumentException failure = Assert.Throws<ArgumentException>(() => new Router<RouteInformation>(
            routerDelegate: new SimpleRouterDelegate((_, _) => new SizedBox()),
            routeInformationProvider: new SimpleRouteInformationProvider(Info("initial"))));

        Assert.StartsWith(
            "A routeInformationParser must be provided when a routeInformationProvider is specified.",
            failure.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RouterConfig_RequiresProviderAndParserTogether()
    {
        _ = Assert.Throws<ArgumentException>(() => new RouterConfig<RouteInformation>(
            routerDelegate: new SimpleRouterDelegate((_, _) => new SizedBox()),
            routeInformationParser: new SimpleRouteInformationParser()));
    }

    [Fact]
    public void Router_WithConfigForwardsEveryDelegateAndBuilds()
    {
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser();
        var dispatcher = new RootBackButtonDispatcher();
        bool built = false;
        var routerDelegate = new SimpleRouterDelegate((_, _) =>
        {
            built = true;
            return new SizedBox();
        });
        var config = new RouterConfig<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser,
            backButtonDispatcher: dispatcher);

        Router<RouteInformation> router = Router.WithConfig(config);
        Assert.Same(routerDelegate, router.RouterDelegate);
        Assert.Same(parser, router.RouteInformationParser);
        Assert.Same(provider, router.RouteInformationProvider);
        Assert.Same(dispatcher, router.BackButtonDispatcher);

        using var harness = new RestorationHarness(router);
        Pump(harness);
        Assert.True(built);
    }

    [Fact]
    public void Router_PrefersItsOwnConfigurationOverAStaleProviderValueWhenReparsing()
    {
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser();
        var dependency = new ValueNotifier<int>(0);
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate(
            (_, information) =>
            {
                built = information.Uri.ToString();
                return new SizedBox();
            },
            reportConfiguration: true);

        using var harness = new RestorationHarness(new InheritedNotifierProbe(
            notifier: dependency,
            child: new Router<RouteInformation>(
                routerDelegate: routerDelegate,
                routeInformationProvider: provider,
                routeInformationParser: new DependentRouteInformationParser(parser))));
        Pump(harness);
        Assert.Equal("initial", built);

        // The delegate moves to a new configuration and the parser's dependency invalidates in the same
        // frame; the router must re-parse its own configuration, not the provider's stale value.
        routerDelegate.SetRouteInformation(Info("update"));
        dependency.Value = 1;
        Pump(harness);

        Assert.Equal("update", built);
        Assert.Equal("update", routerDelegate.CurrentConfiguration!.Uri.ToString());
    }

    // ------------------------------------------------------------------- pop

    [Fact]
    public void BackButtonDispatcher_InvokesTheRouterDelegateSynchronouslyAndRebuilds()
    {
        var dispatcher = new RootBackButtonDispatcher();
        int popCount = 0;
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate(
            (_, information) =>
            {
                built = information.Uri.ToString();
                return new SizedBox();
            },
            onPopRoute: () =>
            {
                popCount += 1;
                return Task.FromResult(true);
            });
        routerDelegate.SetRouteInformationWithoutNotifying(Info("initial"));

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);
        Assert.Equal("initial", built);

        Task<bool> result = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.True(result.IsCompletedSuccessfully);
        Assert.True(SynchronousResultOf(result));
        Assert.Equal(1, popCount);
    }

    [Fact]
    public void BackButtonDispatcher_ReturnsTheDefaultWhenNoCallbackIsRegistered()
    {
        var dispatcher = new RootBackButtonDispatcher();
        Task<bool> result = dispatcher.InvokeCallback(Task.FromResult(false));

        Assert.True(result.IsCompletedSuccessfully);
        Assert.False(SynchronousResultOf(result));
    }

    [Fact]
    public void PopNavigatorRouterDelegate_PopsThroughTheNavigatorItBuilds()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("router navigator");
        var dispatcher = new RootBackButtonDispatcher();
        var routerDelegate = new NavigatorRouterDelegate(navigatorKey);

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);

        NavigatorState navigator = navigatorKey.CurrentState!;
        navigator.Push(NavigatorRouterDelegate.BuildRoute("pushed"));
        Pump(harness);
        Assert.True(navigator.CanPop);

        Task<bool> popped = dispatcher.InvokeCallback(Task.FromResult(false));
        Pump(harness);
        Assert.True(popped.IsCompletedSuccessfully);
        Assert.True(SynchronousResultOf(popped));
        Assert.False(navigator.CanPop);

        Task<bool> secondPop = dispatcher.InvokeCallback(Task.FromResult(false));
        Pump(harness);
        Assert.False(SynchronousResultOf(secondPop));
    }

    [Fact]
    public void ChildBackButtonDispatcher_HandlesThePopOnlyAfterItTakesPriority()
    {
        var root = new RootBackButtonDispatcher();
        int rootPops = 0;
        int childPops = 0;
        root.AddCallback(() =>
        {
            rootPops += 1;
            return Task.FromResult(true);
        });

        ChildBackButtonDispatcher child = root.CreateChildBackButtonDispatcher();
        child.AddCallback(() =>
        {
            childPops += 1;
            return Task.FromResult(true);
        });

        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, rootPops);
        Assert.Equal(0, childPops);

        child.TakePriority();
        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, rootPops);
        Assert.Equal(1, childPops);
    }

    [Fact]
    public void ChildBackButtonDispatcher_TheLastChildToTakePriorityWins()
    {
        var root = new RootBackButtonDispatcher();
        int rootPops = 0;
        root.AddCallback(() =>
        {
            rootPops += 1;
            return Task.FromResult(true);
        });

        ChildBackButtonDispatcher first = root.CreateChildBackButtonDispatcher();
        ChildBackButtonDispatcher second = root.CreateChildBackButtonDispatcher();
        int firstPops = 0;
        int secondPops = 0;
        first.AddCallback(() =>
        {
            firstPops += 1;
            return Task.FromResult(true);
        });
        second.AddCallback(() =>
        {
            secondPops += 1;
            return Task.FromResult(true);
        });

        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, rootPops);

        first.TakePriority();
        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, firstPops);
        Assert.Equal(0, secondPops);

        second.TakePriority();
        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, firstPops);
        Assert.Equal(1, secondPops);
    }

    [Fact]
    public void ChildBackButtonDispatcher_FallsThroughToTheNextChildAndThenToTheParent()
    {
        var root = new RootBackButtonDispatcher();
        int rootPops = 0;
        root.AddCallback(() =>
        {
            rootPops += 1;
            return Task.FromResult(true);
        });

        ChildBackButtonDispatcher first = root.CreateChildBackButtonDispatcher();
        ChildBackButtonDispatcher second = root.CreateChildBackButtonDispatcher();
        first.AddCallback(() => Task.FromResult(false));
        second.AddCallback(() => Task.FromResult(false));
        first.TakePriority();
        second.TakePriority();

        Task<bool> result = root.InvokeCallback(Task.FromResult(false));
        Assert.True(result.IsCompletedSuccessfully);
        Assert.True(SynchronousResultOf(result));
        Assert.Equal(1, rootPops);
    }

    [Fact]
    public void ChildBackButtonDispatcher_TakePriorityPropagatesUpTheWholeChain()
    {
        var root = new RootBackButtonDispatcher();
        root.AddCallback(() => Task.FromResult(false));
        ChildBackButtonDispatcher level1 = root.CreateChildBackButtonDispatcher();
        level1.AddCallback(() => Task.FromResult(false));
        ChildBackButtonDispatcher level2 = level1.CreateChildBackButtonDispatcher();
        level2.AddCallback(() => Task.FromResult(false));
        ChildBackButtonDispatcher level3 = level2.CreateChildBackButtonDispatcher();

        int deepestPops = 0;
        level3.AddCallback(() =>
        {
            deepestPops += 1;
            return Task.FromResult(true);
        });
        level3.TakePriority();

        Task<bool> result = root.InvokeCallback(Task.FromResult(false));
        Assert.True(SynchronousResultOf(result));
        Assert.Equal(1, deepestPops);
    }

    [Fact]
    public void ChildBackButtonDispatcher_RemovingItsLastCallbackDetachesItFromTheParent()
    {
        var root = new RootBackButtonDispatcher();
        int rootPops = 0;
        root.AddCallback(() =>
        {
            rootPops += 1;
            return Task.FromResult(true);
        });

        ChildBackButtonDispatcher child = root.CreateChildBackButtonDispatcher();
        Task<bool> ChildCallback() => Task.FromResult(true);
        child.AddCallback(ChildCallback);
        child.TakePriority();
        child.RemoveCallback(ChildCallback);

        _ = root.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, rootPops);
    }

    [Fact]
    public void RootBackButtonDispatcher_RegistersWithTheBindingWhileItHasCallbacks()
    {
        var dispatcher = new RootBackButtonDispatcher();
        Assert.False(WidgetsBinding.Instance.HandlePopRoute());

        Task<bool> Callback() => Task.FromResult(true);
        dispatcher.AddCallback(Callback);
        Assert.True(WidgetsBinding.Instance.HandlePopRoute());
        Assert.True(Navigator.TryHandleBackButton());

        dispatcher.RemoveCallback(Callback);
        Assert.False(WidgetsBinding.Instance.HandlePopRoute());
    }

    // ------------------------------------------------------- BackButtonListener

    [Fact]
    public void BackButtonListener_TakesPriorityOverTheRouterDelegate()
    {
        var dispatcher = new RootBackButtonDispatcher();
        int delegatePops = 0;
        int listenerPops = 0;
        var routerDelegate = new SimpleRouterDelegate(
            (_, _) => new BackButtonListener(
                child: new SizedBox(),
                onBackButtonPressed: () =>
                {
                    listenerPops += 1;
                    return Task.FromResult(true);
                }),
            onPopRoute: () =>
            {
                delegatePops += 1;
                return Task.FromResult(true);
            });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);

        _ = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, listenerPops);
        Assert.Equal(0, delegatePops);
    }

    [Fact]
    public void BackButtonListener_UsesTheUpdatedCallbackAfterARebuild()
    {
        var dispatcher = new RootBackButtonDispatcher();
        int firstCalls = 0;
        int secondCalls = 0;
        bool useSecond = false;
        var routerDelegate = new SimpleRouterDelegate((_, _) => new BackButtonListener(
            child: new SizedBox(),
            onBackButtonPressed: useSecond
                ? () =>
                {
                    secondCalls += 1;
                    return Task.FromResult(true);
                }
                : () =>
                {
                    firstCalls += 1;
                    return Task.FromResult(true);
                }));

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);

        _ = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, firstCalls);

        useSecond = true;
        routerDelegate.NotifyListeners();
        Pump(harness);

        _ = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void BackButtonListener_ClearsItsCallbackWhenItLeavesTheTree()
    {
        var dispatcher = new RootBackButtonDispatcher();
        int delegatePops = 0;
        int listenerPops = 0;
        bool showListener = true;
        var routerDelegate = new SimpleRouterDelegate(
            (_, _) => showListener
                ? new BackButtonListener(
                    child: new SizedBox(),
                    onBackButtonPressed: () =>
                    {
                        listenerPops += 1;
                        return Task.FromResult(true);
                    })
                : new SizedBox(),
            onPopRoute: () =>
            {
                delegatePops += 1;
                return Task.FromResult(true);
            });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);

        _ = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, listenerPops);

        showListener = false;
        routerDelegate.NotifyListeners();
        Pump(harness);

        _ = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.Equal(1, listenerPops);
        Assert.Equal(1, delegatePops);
    }

    [Fact]
    public void BackButtonListener_TheInnermostOneWinsAndFallsOutwardWhenItDeclines()
    {
        var dispatcher = new RootBackButtonDispatcher();
        var order = new List<string>();
        var routerDelegate = new SimpleRouterDelegate((_, _) => new BackButtonListener(
            onBackButtonPressed: () =>
            {
                order.Add("outer");
                return Task.FromResult(true);
            },
            child: new BackButtonListener(
                onBackButtonPressed: () =>
                {
                    order.Add("inner");
                    return Task.FromResult(false);
                },
                child: new SizedBox())));

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            backButtonDispatcher: dispatcher));
        Pump(harness);

        Task<bool> result = dispatcher.InvokeCallback(Task.FromResult(false));
        Assert.True(result.IsCompletedSuccessfully);
        Assert.True(SynchronousResultOf(result));
        Assert.Equal(["inner", "outer"], order);
    }

    // ------------------------------------------------------------- reporting

    [Fact]
    public void Router_ReportsTheParsedConfigurationOncePerFrame()
    {
        var reports = new List<(string Uri, RouteInformationReportingType Type)>();
        var provider = new SimpleRouteInformationProvider(
            Info("initial"),
            (information, type) => reports.Add((information.Uri.ToString(), type)));
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox(), reportConfiguration: true);

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: new SimpleRouteInformationParser()));
        Pump(harness);

        Assert.Equal([("initial", RouteInformationReportingType.None)], reports);

        reports.Clear();
        routerDelegate.SetRouteInformation(Info("update"));
        Pump(harness);
        Assert.Equal([("update", RouteInformationReportingType.None)], reports);

        // A change that only touches the state still reports.
        reports.Clear();
        routerDelegate.SetRouteInformation(Info("update", state: "extra"));
        Pump(harness);
        Assert.Equal([("update", RouteInformationReportingType.None)], reports);
    }

    [Fact]
    public void Router_ReportsTheRedirectedLocationRatherThanTheOneTheHostGave()
    {
        var reports = new List<string>();
        var provider = new SimpleRouteInformationProvider(
            Info("/home"),
            (information, _) => reports.Add(information.Uri.ToString()));
        var parser = new SimpleRouteInformationParser((information, _) =>
            Task.FromResult(information.Uri.ToString() == "/doesNotExist" ? Info("/404") : information));
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox(), reportConfiguration: true);

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser));
        Pump(harness);

        provider.SetValue(Info("/doesNotExist"));
        Pump(harness);

        Assert.Equal(["/home", "/404"], reports);
    }

    [Fact]
    public void Router_NeglectAndNavigateSetTheReportingType()
    {
        var reports = new List<(string Uri, RouteInformationReportingType Type)>();
        var provider = new SimpleRouteInformationProvider(
            Info("initial"),
            (information, type) => reports.Add((information.Uri.ToString(), type)));
        Action? neglect = null;
        Action? navigate = null;
        var routerDelegate = new SimpleRouterDelegate(
            (context, _) =>
            {
                neglect = () => Router.Neglect(context, () => { });
                navigate = () => Router.Navigate(context, () => { });
                return new SizedBox();
            },
            reportConfiguration: true);

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: new SimpleRouteInformationParser()));
        Pump(harness);
        reports.Clear();

        neglect!();
        routerDelegate.SetRouteInformation(Info("neglected"));
        Pump(harness);
        Assert.Equal([("neglected", RouteInformationReportingType.Neglect)], reports);

        // Navigate reports even though the configuration did not change.
        reports.Clear();
        navigate!();
        routerDelegate.NotifyListeners();
        Pump(harness);
        Assert.Equal([("neglected", RouteInformationReportingType.Navigate)], reports);
    }

    // ------------------------------------------------------ parser dependencies

    [Fact]
    public void RouteInformationParser_ReparsesWhenADependencyItReadChanges()
    {
        var dependency = new ValueNotifier<int>(1);
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new DependencyReadingParser(readWithDependency: true);
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate((_, information) =>
        {
            built = information.Uri.ToString();
            return new SizedBox();
        });

        using var harness = new RestorationHarness(new InheritedNotifierProbe(
            notifier: dependency,
            child: new Router<RouteInformation>(
                routerDelegate: routerDelegate,
                routeInformationProvider: provider,
                routeInformationParser: parser)));
        Pump(harness);
        Assert.Equal("initial-1", built);
        Assert.Equal(1, parser.ParseCount);

        dependency.Value = 2;
        Pump(harness);
        Assert.Equal("initial-2", built);
        Assert.Equal(2, parser.ParseCount);
    }

    [Fact]
    public void RouteInformationParser_DoesNotReparseWhenItReadsWithoutADependency()
    {
        var dependency = new ValueNotifier<int>(1);
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new DependencyReadingParser(readWithDependency: false);
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate((_, information) =>
        {
            built = information.Uri.ToString();
            return new SizedBox();
        });

        using var harness = new RestorationHarness(new InheritedNotifierProbe(
            notifier: dependency,
            child: new Router<RouteInformation>(
                routerDelegate: routerDelegate,
                routeInformationProvider: provider,
                routeInformationParser: parser)));
        Pump(harness);
        Assert.Equal("initial-1", built);

        dependency.Value = 2;
        Pump(harness);
        Assert.Equal("initial-1", built);
        Assert.Equal(1, parser.ParseCount);
    }

    [Fact]
    public void RouterDelegate_InheritedLookupsInBuildDoNotReparse()
    {
        var dependency = new ValueNotifier<int>(1);
        var provider = new SimpleRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser();
        int seen = 0;
        var routerDelegate = new SimpleRouterDelegate((context, _) =>
        {
            seen = InheritedNotifierProbe.Of(context);
            return new SizedBox();
        });

        using var harness = new RestorationHarness(new InheritedNotifierProbe(
            notifier: dependency,
            child: new Router<RouteInformation>(
                routerDelegate: routerDelegate,
                routeInformationProvider: provider,
                routeInformationParser: parser)));
        Pump(harness);
        Assert.Equal(1, seen);
        Assert.Equal(1, parser.ParseCount);

        dependency.Value = 5;
        Pump(harness);
        Assert.Equal(5, seen);
        Assert.Equal(1, parser.ParseCount);
    }

    // --------------------------------------- PlatformRouteInformationProvider

    [Fact]
    public void PlatformRouteInformationProvider_ReportsThroughSystemNavigator()
    {
        var provider = new PlatformRouteInformationProvider(Info("initial"));
        using var navigation = new MockMethodCallHandler(SystemChannels.Navigation);

        provider.RouterReportsNewRouteInformation(Info("a", state: true));
        provider.RouterReportsNewRouteInformation(Info("a", state: false));
        provider.RouterReportsNewRouteInformation(Info("b"), RouteInformationReportingType.Neglect);
        provider.RouterReportsNewRouteInformation(Info("b"), RouteInformationReportingType.Navigate);

        List<MethodCall> modes = navigation.Log.FindAll(call => call.Method == "selectMultiEntryHistory");
        List<MethodCall> updates = navigation.Log.FindAll(call => call.Method == "routeInformationUpdated");
        Assert.Equal(4, modes.Count);
        Assert.Equal([false, true, true, false], updates.Select(RouteUpdateReplace));
        Assert.Equal(["a", "a", "b", "b"], updates.Select(RouteUpdateUri));
    }

    private static bool RouteUpdateReplace(MethodCall call) =>
        (bool)((IDictionary<string, object?>)call.Arguments!)["replace"]!;

    private static string RouteUpdateUri(MethodCall call) =>
        (string)((IDictionary<string, object?>)call.Arguments!)["uri"]!;

    [Theory]
    [InlineData("initial?a=ws/abcd", "initial?a=ws%2Fabcd")]
    [InlineData("initial?a=1&b=2", "initial?b=2&a=1")]
    [InlineData("initial?a=1&a=2", "initial?a=2&a=1")]
    public void PlatformRouteInformationProvider_TreatsSemanticallyEqualUrisAsTheSameEntry(
        string first,
        string second)
    {
        var provider = new PlatformRouteInformationProvider(Info(first));
        using var navigation = new MockMethodCallHandler(SystemChannels.Navigation);

        provider.RouterReportsNewRouteInformation(Info(first));
        provider.RouterReportsNewRouteInformation(Info(second));

        List<MethodCall> updates = navigation.Log.FindAll(call => call.Method == "routeInformationUpdated");
        Assert.Equal(2, updates.Count);
        Assert.True(RouteUpdateReplace(updates[1]));
    }

    [Fact]
    public void PlatformRouteInformationProvider_PushesHostRouteInformationIntoTheRouter()
    {
        var provider = new PlatformRouteInformationProvider(Info("initial"));
        var parser = new SimpleRouteInformationParser();
        string? built = null;
        var routerDelegate = new SimpleRouterDelegate((_, information) =>
        {
            built = $"{information.Uri}:{information.State}";
            return new SizedBox();
        });

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: parser));
        Pump(harness);
        Assert.Equal("initial:", built);

        Assert.True(WidgetsBinding.Instance.HandlePushRouteInformation(Info("pushed", state: "state")));
        Pump(harness);
        Assert.Equal("pushed:state", built);
    }

    [Fact]
    public void PlatformRouteInformationProvider_RegistersWithTheBindingOnlyWhileListenedTo()
    {
        var provider = new PlatformRouteInformationProvider(Info("initial"));
        void Listener()
        {
        }

        Assert.False(WidgetsBinding.Instance.HandlePushRouteInformation(Info("a")));
        provider.AddListener(Listener);
        Assert.True(WidgetsBinding.Instance.HandlePushRouteInformation(Info("a")));
        provider.RemoveListener(Listener);
        Assert.False(WidgetsBinding.Instance.HandlePushRouteInformation(Info("b")));
    }

    // ----------------------------------------------------------- restoration

    [Fact]
    public void Router_WithoutAProviderDoesNotSetAnyRoutePathOnTheFirstBuild()
    {
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox());

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            restorationScopeId: "router"));
        Pump(harness);

        Assert.Empty(routerDelegate.NewRoutePaths);
        Assert.Empty(routerDelegate.RestoredRoutePaths);
        Assert.Empty(routerDelegate.InitialRoutePaths);
    }

    [Fact]
    public void Router_UsesSetInitialRoutePathForTheFirstProviderValue()
    {
        var provider = new SimpleRouteInformationProvider(Info("/home"));
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox());

        using var harness = new RestorationHarness(new Router<RouteInformation>(
            routerDelegate: routerDelegate,
            routeInformationProvider: provider,
            routeInformationParser: new SimpleRouteInformationParser(),
            restorationScopeId: "router"));
        Pump(harness);

        Assert.Equal(["/home"], routerDelegate.InitialRoutePaths.Select(path => path.Uri.ToString()));
        Assert.Empty(routerDelegate.RestoredRoutePaths);

        provider.SetValue(Info("/next"));
        Pump(harness);
        Assert.Equal(["/next"], routerDelegate.NewRoutePaths.Select(path => path.Uri.ToString()));
    }

    [Fact]
    public void Router_RestoresItsConfigurationThroughSetRestoredRoutePath()
    {
        var manager = new TestRestorationManager
        {
            Data = RawRestorationData.Build(children: new Dictionary<object, object?>
            {
                ["root"] = RawRestorationData.Build(children: new Dictionary<object, object?>
                {
                    ["router"] = RawRestorationData.Build(values: new Dictionary<object, object?>
                    {
                        ["route"] = new object?[] { "/restored", null },
                    }),
                }),
            }),
        };
        RestorationManager.Instance = manager;

        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox(), reportConfiguration: true);

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root",
            child: new Router<RouteInformation>(
                routerDelegate: routerDelegate,
                routeInformationParser: new SimpleRouteInformationParser(),
                restorationScopeId: "router")));
        Pump(harness);

        Assert.Equal(["/restored"], routerDelegate.RestoredRoutePaths.Select(path => path.Uri.ToString()));
        Assert.Empty(routerDelegate.InitialRoutePaths);
    }

    // ------------------------------------------------------------- app hosts

    [Fact]
    public void WidgetsAppRouter_BuildsARouterWithDefaultProviderAndDispatcher()
    {
        var parser = new SimpleRouteInformationParser();
        bool built = false;
        var routerDelegate = new SimpleRouterDelegate((_, _) =>
        {
            built = true;
            return new SizedBox();
        });

        WidgetsApp app = WidgetsApp.Router(
            color: Avalonia.Media.Colors.Blue,
            routerDelegate: routerDelegate,
            routeInformationParser: parser);

        Assert.Same(routerDelegate, app.RouterDelegate);
        Assert.Same(parser, app.RouteInformationParser);
        Assert.Null(app.RouterConfig);

        using var harness = new RestorationHarness(app);
        Pump(harness);

        Assert.True(built);
        Router<RouteInformation>? router = harness.FindWidget<Router<RouteInformation>>();
        Assert.NotNull(router);
        Assert.IsType<PlatformRouteInformationProvider>(router!.RouteInformationProvider);
        Assert.IsType<RootBackButtonDispatcher>(router.BackButtonDispatcher);
        Assert.Equal("router", router.RestorationScopeId);
    }

    [Fact]
    public void WidgetsAppRouter_ValidatesTheDelegateCombinations()
    {
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox());
        var config = new RouterConfig<RouteInformation>(routerDelegate: routerDelegate);

        _ = Assert.Throws<ArgumentException>(() => WidgetsApp.Router<RouteInformation>(
            color: Avalonia.Media.Colors.Blue));
        _ = Assert.Throws<ArgumentException>(() => WidgetsApp.Router(
            color: Avalonia.Media.Colors.Blue,
            routerDelegate: routerDelegate,
            routerConfig: config));
        _ = Assert.Throws<ArgumentException>(() => WidgetsApp.Router(
            color: Avalonia.Media.Colors.Blue,
            routerDelegate: routerDelegate,
            routeInformationProvider: new SimpleRouteInformationProvider(Info("initial"))));
    }

    [Fact]
    public void WidgetsAppRouter_WithConfigDoesNotCreateDefaultDelegates()
    {
        var routerDelegate = new SimpleRouterDelegate((_, _) => new SizedBox());
        var config = new RouterConfig<RouteInformation>(routerDelegate: routerDelegate);

        WidgetsApp app = WidgetsApp.Router(color: Avalonia.Media.Colors.Blue, routerConfig: config);
        Assert.Same(config, app.RouterConfig);

        using var harness = new RestorationHarness(app);
        Pump(harness);

        Router<RouteInformation>? router = harness.FindWidget<Router<RouteInformation>>();
        Assert.NotNull(router);
        Assert.Null(router!.RouteInformationProvider);
        Assert.Null(router.BackButtonDispatcher);
    }

    // ---------------------------------------------------------------- doubles

    private sealed class SimpleRouteInformationParser : RouteInformationParser<RouteInformation>
    {
        private readonly Func<RouteInformation, BuildContext, Task<RouteInformation>>? _onParse;

        public SimpleRouteInformationParser(
            Func<RouteInformation, BuildContext, Task<RouteInformation>>? onParse = null)
        {
            _onParse = onParse;
        }

        public int ParseCount { get; private set; }

        public override Task<RouteInformation> ParseRouteInformationWithDependencies(
            RouteInformation routeInformation,
            BuildContext context)
        {
            ParseCount += 1;
            return _onParse?.Invoke(routeInformation, context) ?? Task.FromResult(routeInformation);
        }

        public override RouteInformation? RestoreRouteInformation(RouteInformation configuration) => configuration;
    }

    /// <summary>Reads an inherited value while parsing, with or without registering a dependency.</summary>
    private sealed class DependencyReadingParser : RouteInformationParser<RouteInformation>
    {
        private readonly bool _readWithDependency;

        public DependencyReadingParser(bool readWithDependency)
        {
            _readWithDependency = readWithDependency;
        }

        public int ParseCount { get; private set; }

        public override Task<RouteInformation> ParseRouteInformationWithDependencies(
            RouteInformation routeInformation,
            BuildContext context)
        {
            ParseCount += 1;
            int value = _readWithDependency
                ? InheritedNotifierProbe.Of(context)
                : InheritedNotifierProbe.Read(context);
            return Task.FromResult(Info($"{routeInformation.Uri}-{value}"));
        }

        public override RouteInformation? RestoreRouteInformation(RouteInformation configuration) => configuration;
    }

    /// <summary>Registers the parser dependency without changing what is parsed.</summary>
    private sealed class DependentRouteInformationParser : RouteInformationParser<RouteInformation>
    {
        private readonly SimpleRouteInformationParser _inner;

        public DependentRouteInformationParser(SimpleRouteInformationParser inner)
        {
            _inner = inner;
        }

        public override Task<RouteInformation> ParseRouteInformationWithDependencies(
            RouteInformation routeInformation,
            BuildContext context)
        {
            _ = InheritedNotifierProbe.Of(context);
            return _inner.ParseRouteInformationWithDependencies(routeInformation, context);
        }

        public override RouteInformation? RestoreRouteInformation(RouteInformation configuration) => configuration;
    }

    private sealed class SimpleRouteInformationProvider : RouteInformationProvider
    {
        private readonly Action<RouteInformation, RouteInformationReportingType>? _onReport;
        private RouteInformation _value;

        public SimpleRouteInformationProvider(
            RouteInformation value,
            Action<RouteInformation, RouteInformationReportingType>? onReport = null)
        {
            _value = value;
            _onReport = onReport;
        }

        public override RouteInformation Value => _value;

        public void SetValue(RouteInformation value)
        {
            _value = value;
            NotifyListeners();
        }

        public override void RouterReportsNewRouteInformation(
            RouteInformation routeInformation,
            RouteInformationReportingType type = RouteInformationReportingType.None)
        {
            _value = routeInformation;
            _onReport?.Invoke(routeInformation, type);
        }
    }

    private sealed class SimpleRouterDelegate : RouterDelegate<RouteInformation>
    {
        private readonly Func<BuildContext, RouteInformation, Widget> _builder;
        private readonly Func<Task<bool>>? _onPopRoute;
        private readonly Func<RouteInformation, Task>? _onSetNewRoutePath;
        private readonly bool _reportConfiguration;
        private RouteInformation _routeInformation = new(new Uri("waiting", UriKind.Relative));

        public SimpleRouterDelegate(
            Func<BuildContext, RouteInformation, Widget> builder,
            Func<Task<bool>>? onPopRoute = null,
            Func<RouteInformation, Task>? onSetNewRoutePath = null,
            bool reportConfiguration = false)
        {
            _builder = builder;
            _onPopRoute = onPopRoute;
            _onSetNewRoutePath = onSetNewRoutePath;
            _reportConfiguration = reportConfiguration;
        }

        public List<RouteInformation> NewRoutePaths { get; } = [];

        public List<RouteInformation> InitialRoutePaths { get; } = [];

        public List<RouteInformation> RestoredRoutePaths { get; } = [];

        public override RouteInformation? CurrentConfiguration => _reportConfiguration ? _routeInformation : null;

        public void SetRouteInformation(RouteInformation information)
        {
            _routeInformation = information;
            NotifyListeners();
        }

        public void SetRouteInformationWithoutNotifying(RouteInformation information)
        {
            _routeInformation = information;
        }

        public override Task SetInitialRoutePath(RouteInformation configuration)
        {
            InitialRoutePaths.Add(configuration);
            _routeInformation = configuration;
            return Task.CompletedTask;
        }

        public override Task SetRestoredRoutePath(RouteInformation configuration)
        {
            RestoredRoutePaths.Add(configuration);
            _routeInformation = configuration;
            return Task.CompletedTask;
        }

        public override Task SetNewRoutePath(RouteInformation configuration)
        {
            NewRoutePaths.Add(configuration);
            if (_onSetNewRoutePath is not null)
            {
                return ApplyLaterAsync(configuration);
            }

            _routeInformation = configuration;
            return Task.CompletedTask;
        }

        public override Task<bool> PopRoute() => _onPopRoute?.Invoke() ?? Task.FromResult(true);

        public override Widget Build(BuildContext context) => _builder(context, _routeInformation);

        private async Task ApplyLaterAsync(RouteInformation configuration)
        {
            await _onSetNewRoutePath!(configuration).ConfigureAwait(false);
            _routeInformation = configuration;
        }
    }

    private sealed class NavigatorRouterDelegate : PopNavigatorRouterDelegateMixin<RouteInformation>
    {
        public NavigatorRouterDelegate(GlobalKey<NavigatorState> navigatorKey)
        {
            NavigatorKey = navigatorKey;
        }

        public override GlobalKey<NavigatorState>? NavigatorKey { get; }

        public override Task SetNewRoutePath(RouteInformation configuration) => Task.CompletedTask;

        public override Widget Build(BuildContext context)
        {
            return new Navigator(initialRoute: BuildRoute("home"), key: NavigatorKey);
        }

        public static Route BuildRoute(string name)
        {
            return new PageRouteBuilder(
                pageBuilder: (_, _, _) => new SizedBox(),
                settings: new RouteSettings(Name: name));
        }
    }

    /// <summary>Exposes an int through an inherited widget, with and without dependency registration.</summary>
    private sealed class InheritedNotifierProbe : StatefulWidget
    {
        public InheritedNotifierProbe(ValueNotifier<int> notifier, Widget child, Key? key = null) : base(key)
        {
            Notifier = notifier;
            Child = child;
        }

        public ValueNotifier<int> Notifier { get; }

        public Widget Child { get; }

        public static int Of(BuildContext context) => context.DependOnInherited<ProbeScope>()!.Value;

        public static int Read(BuildContext context) => context.GetInherited<ProbeScope>()!.Value;

        public override State CreateState() => new ProbeState();

        internal sealed class ProbeScope : InheritedWidget
        {
            public ProbeScope(int value, Widget child, Key? key = null) : base(key)
            {
                Value = value;
                Child = child;
            }

            public int Value { get; }

            public Widget Child { get; }

            public override Widget Build(BuildContext context) => Child;

            protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
            {
                return ((ProbeScope)oldWidget).Value != Value;
            }
        }

        private sealed class ProbeState : State
        {
            private InheritedNotifierProbe CurrentWidget => (InheritedNotifierProbe)StateWidget;

            public override void InitState()
            {
                base.InitState();
                CurrentWidget.Notifier.AddListener(HandleChanged);
            }

            public override void Dispose()
            {
                CurrentWidget.Notifier.RemoveListener(HandleChanged);
                base.Dispose();
            }

            public override Widget Build(BuildContext context)
            {
                return new ProbeScope(CurrentWidget.Notifier.Value, CurrentWidget.Child);
            }

            private void HandleChanged() => SetState(() => { });
        }
    }
}
