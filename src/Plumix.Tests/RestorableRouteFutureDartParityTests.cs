using System.Globalization;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports the RestorableRouteFuture cases of flutter/packages/flutter/test/widgets/navigator_restoration_test.dart
// together with the fixtures they use (TestWidget, RouteWidget, RouteFutureWidget, findRoute).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RestorableRouteFutureDartParityTests
{
    // Flutter: 'navigator_restoration_test.dart: RestorableRouteFuture'
    [Fact]
    public void RestorableRouteFuture()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestWidget();
        tester.PumpWidget(widget);
        Assert.Single(FindRoute(tester, "home"));

        tester.State<NavigatorState>().RestorablePush(RouteFutureBuilder);
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Return value: null"));

        RestorableRouteFuture<int> routeFuture = OnstageRouteFutureState(tester).RouteFuture;
        Assert.Null(routeFuture.Route);
        Assert.False(routeFuture.IsPresent);
        Assert.False(routeFuture.Enabled);

        routeFuture.Present("Foo");
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Route: Foo"));
        Assert.Equal("Foo", routeFuture.Route!.Settings.Name);
        Assert.True(routeFuture.IsPresent);
        Assert.True(routeFuture.Enabled);

        tester.RestartAndRestore();

        Assert.Single(tester.OnstageElementsWithText("Route: Foo"));
        RestorableRouteFuture<int> restoredRouteFuture =
            tester.State<RouteFutureWidgetState>().RouteFuture;
        Assert.Equal("Foo", restoredRouteFuture.Route!.Settings.Name);
        Assert.True(restoredRouteFuture.IsPresent);
        Assert.True(restoredRouteFuture.Enabled);

        tester.State<NavigatorState>().Pop(10);
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Return value: 10"));
        Assert.Null(restoredRouteFuture.Route);
        Assert.False(restoredRouteFuture.IsPresent);
        Assert.False(restoredRouteFuture.Enabled);
    }

    // Flutter: 'navigator_restoration_test.dart: RestorableRouteFuture in unrestorable context'
    [Fact]
    public void RestorableRouteFutureInUnrestorableContext()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        Widget widget = new TestWidget();
        tester.PumpWidget(widget);
        Assert.Single(FindRoute(tester, "home"));

        tester.State<NavigatorState>().PushNamed("unrestorable");
        tester.PumpAndSettle();
        Assert.Single(FindRoute(tester, "unrestorable"));

        tester.State<NavigatorState>().RestorablePush(RouteFutureBuilder);
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Return value: null"));

        RestorableRouteFuture<int> routeFuture = OnstageRouteFutureState(tester).RouteFuture;
        Assert.Null(routeFuture.Route);
        Assert.False(routeFuture.IsPresent);
        Assert.False(routeFuture.Enabled);

        routeFuture.Present("Foo");
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Route: Foo"));
        Assert.Equal("Foo", routeFuture.Route!.Settings.Name);
        Assert.True(routeFuture.IsPresent);
        Assert.False(routeFuture.Enabled);

        tester.RestartAndRestore();

        Assert.Single(FindRoute(tester, "home"));
    }

    // C#-only: Dart's `present` asserts `!isPresent`; the assert has no dedicated Flutter test.
    [DebugOnlyFact]
    public void PresentWhilePresentAsserts()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new TestWidget());
        tester.State<NavigatorState>().RestorablePush(RouteFutureBuilder);
        tester.PumpAndSettle();

        RestorableRouteFuture<int> routeFuture = OnstageRouteFutureState(tester).RouteFuture;
        routeFuture.Present("Foo");
        tester.PumpAndSettle();

        AssertionError error = Assert.Throws<AssertionError>(() => routeFuture.Present("Bar"));
        Assert.Contains("!isPresent", error.Message);
    }

    // navigator_restoration_test.dart: _routeFutureBuilder. Anonymous restorable routes must be built by a
    // static method, Plumix's stand-in for Dart's `@pragma('vm:entry-point')` top-level function.
    internal static Route RouteFutureBuilder(BuildContext context, object? arguments)
    {
        return new MaterialPageRoute(builder: _ => new RouteFutureWidget());
    }

    private static RouteFutureWidgetState OnstageRouteFutureState(FrameworkDartTester tester)
    {
        Element element = tester.OnstageElements().Single(element => element.Widget is RouteFutureWidget);
        return (RouteFutureWidgetState)((StatefulElement)element).State;
    }

    // navigator_restoration_test.dart: findRoute (skipOffstage: true).
    private static IReadOnlyList<Element> FindRoute(FrameworkDartTester tester, string name)
    {
        return tester.OnstageElements()
            .Where(element => element.Widget is RouteWidget routeWidget && routeWidget.Name == name)
            .ToList();
    }

    // navigator_restoration_test.dart: TestWidget.
    private sealed class TestWidget(string? restorationId = "app") : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new RootRestorationScope(
                restorationId: restorationId,
                child: new Directionality(
                    TextDirection.Ltr,
                    new MediaQuery(
                        data: MediaQueryData.FromView(View.Of(context)),
                        child: new Navigator(
                            initialRouteName: "home",
                            restorationScopeId: "app",
                            onGenerateRoute: settings => new MaterialPageRoute(
                                settings: settings,
                                builder: _ => new RouteWidget(
                                    name: settings.Name!,
                                    arguments: settings.Arguments))))));
        }
    }

    // navigator_restoration_test.dart: RouteWidget.
    private sealed class RouteWidget(string name, object? arguments = null) : StatefulWidget
    {
        public string Name { get; } = name;

        public object? Arguments { get; } = arguments;

        public override State CreateState() => new RouteWidgetState();
    }

    private sealed class RouteWidgetState : RestorationState<RouteWidget>
    {
        private readonly RestorableInt _counter = new(0);

        protected override string? RestorationId => "stateful";

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(_counter, "counter");
        }

        public override void Dispose()
        {
            base.Dispose();
            _counter.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var children = new List<Widget>
            {
                new GestureDetector(
                    child: new Text($"Route: {Widget.Name}"),
                    onTap: () => SetState(() => _counter.Value++)),
            };
            if (Widget.Arguments is not null)
            {
                children.Add(new Text($"Arguments(home): {Widget.Arguments}"));
            }

            children.Add(new Text($"Counter({Widget.Name}): {_counter.Value}"));
            return new Center(child: new Column(children: children));
        }
    }

    // navigator_restoration_test.dart: RouteFutureWidget.
    private sealed class RouteFutureWidget : StatefulWidget
    {
        public override State CreateState() => new RouteFutureWidgetState();
    }

    private sealed class RouteFutureWidgetState : RestorationState<RouteFutureWidget>
    {
        private int? _value;

        public RestorableRouteFuture<int> RouteFuture { get; private set; } = null!;

        protected override string? RestorationId => "routefuturewidget";

        public override void InitState()
        {
            base.InitState();
            RouteFuture = new RestorableRouteFuture<int>(
                onPresent: (navigatorState, arguments) => navigatorState.RestorablePushNamed((string)arguments!),
                onComplete: i => SetState(() => _value = i));
        }

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(RouteFuture, "routeFuture");
        }

        public override void Dispose()
        {
            base.Dispose();
            RouteFuture.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            string value = _value?.ToString(CultureInfo.InvariantCulture) ?? "null";
            return new Center(child: new Text($"Return value: {value}"));
        }
    }
}
