using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/heroes.dart
// Flutter tests: widgets/heroes_test.dart and navigator_test.dart (HeroController ownership).

namespace Plumix.Tests;

public sealed partial class HeroNavigatorTests
{
    [Theory]
    [InlineData(44.0, 44.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.0, 44.0)]
    [InlineData(44.0, 0.0)]
    public void Manifest_BoundsAreRelativeToRoute_AndZeroAreaIsValid(double width, double height)
    {
        Scheduler.ResetForTests();
        try
        {
            var route = GeometryRoute(width, height);
            using var harness = new WidgetRenderHarness(
                new Padding(new EdgeInsets(40, 30, 0, 0), new Navigator(initialRoute: route)));
            harness.Pump(new Size(320, 240));
            HeroState hero = Assert.Single(harness.HeroStates);
            using var manifest = GeometryManifest(harness, route, route, hero, hero);

            Assert.True(manifest.IsValid);
            Assert.Equal(new Rect(20, 60, width, height), manifest.ToHeroLocation);
            Assert.Equal(manifest.ToHeroLocation, manifest.FromHeroLocation);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BuildModeGates_ManifestRejectsUnlaidOutBoxWithoutSilentlySkippingFlight()
    {
        Scheduler.ResetForTests();
        try
        {
            var route = GeometryRoute();
            // Build the route but deliberately do not flush layout before measuring its hero.
            using var harness = new WidgetRenderHarness(new Navigator(initialRoute: route));
            HeroState hero = Assert.Single(harness.HeroStates);
            using var manifest = GeometryManifest(harness, route, route, hero, hero);
            if (Constants.KDebugMode)
            {
                Assert.Throws<AssertionError>(() => manifest.IsValid);
                Assert.Throws<AssertionError>(() => hero.StartFlight());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => manifest.IsValid);
                Assert.Throws<InvalidOperationException>(() => hero.StartFlight());
            }
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BuildModeGates_MissingRouteContextAssertsInDebug_AndMeasuresGloballyOtherwise()
    {
        Scheduler.ResetForTests();
        try
        {
            var route = GeometryRoute();
            using var harness = new WidgetRenderHarness(
                new Padding(new EdgeInsets(40, 30, 0, 0), new Navigator(initialRoute: route)));
            harness.Pump(new Size(320, 240));
            HeroState hero = Assert.Single(harness.HeroStates);
            var uninstalledRoute = GeometryRoute();
            using var manifest = GeometryManifest(harness, route, uninstalledRoute, hero, hero);
            if (Constants.KDebugMode)
            {
                Assert.Throws<AssertionError>(() => manifest.ToHeroLocation);
            }
            else
            {
                Assert.Equal(new Rect(60, 90, 44, 44), manifest.ToHeroLocation);
            }
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void Manifest_RejectsUnpaintableBounds_ButDivertedFlightDoesNotMeasureSource(
        bool unpaintableDestination, bool diverted, bool valid)
    {
        Scheduler.ResetForTests();
        try
        {
            Widget SizedHero() => new Hero(SharedHeroTag, new SizedBox(width: 44, height: 44));
            Widget UnpaintableHero() => new Transform(Matrix4.Zero(), child: SizedHero());
            var route = new PageRouteBuilder(pageBuilder: (_, _, _) => new Stack(children:
            [
                new Positioned(left: 20, top: 60, child: UnpaintableHero()),
                new Positioned(left: 100, top: 60,
                    child: unpaintableDestination ? UnpaintableHero() : SizedHero()),
            ]));
            using var harness = new WidgetRenderHarness(new Navigator(initialRoute: route));
            harness.Pump(new Size(320, 240));
            Assert.Equal(2, harness.HeroStates.Count);
            using var manifest = GeometryManifest(
                harness, route, route, harness.HeroStates[0], harness.HeroStates[1], diverted);

            Assert.Equal(valid, manifest.IsValid);
            if (!unpaintableDestination)
            {
                Assert.Equal(new Rect(100, 60, 44, 44), manifest.ToHeroLocation);
            }
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Hero_DefaultsAndZeroAreaCompositionMatchFlutter()
    {
        using var tester = new FrameworkDartTester();
        var child = new SizedBox(width: 100, height: 100);
        var hero = new Hero(SharedHeroTag, child);
        Assert.Same(Curves.FastOutSlowIn, hero.Curve);
        Assert.Null(hero.ReverseCurve);
        Assert.False(hero.TransitionOnUserGestures);
        Assert.Null(hero.CreateRectTween);
        Assert.Null(hero.PlaceholderBuilder);
        Assert.Null(hero.FlightShuttleBuilder);
        Assert.True(new HeroMode(child).Enabled);

        PumpHeroWidget(tester, new Center(child: new SizedBox(width: 0, height: 0, child: hero)));
        Assert.Equal(default, ((RenderBox)tester.ElementOfType<Hero>().FindRenderObject()!).Size);
        Assert.False(Assert.Single(tester.ElementsOfType<Offstage>()).Widget is Offstage { IsOffstage: true });
        Assert.True(((TickerMode)tester.ElementOfType<TickerMode>().Widget).Enabled);
        Assert.Same(child, ((KeyedSubtree)tester.ElementOfType<KeyedSubtree>().Widget).Child);
    }

    [DebugOnlyFact]
    public void HeroAndHeroMode_ExposeFlutterDiagnostics()
    {
        var hero = new Hero(SharedHeroTag, new SizedBox());
        var properties = new DiagnosticPropertiesBuilder();
        hero.DebugFillProperties(properties);
        Assert.Equal(SharedHeroTag,
            Assert.IsType<DiagnosticsProperty<object>>(Assert.Single(properties.Properties,
                property => property.Name == "tag")).TypedValue);

        foreach (bool enabled in new[] { true, false })
        {
            properties = new DiagnosticPropertiesBuilder();
            new HeroMode(new SizedBox(), enabled).DebugFillProperties(properties);
            var mode = Assert.IsType<FlagProperty>(Assert.Single(properties.Properties,
                property => property.Name == "mode"));
            Assert.True(mode.ShowName);
            Assert.Equal(enabled ? "enabled" : "disabled", mode.ToDescription());
        }
    }

    [Fact]
    public void HeroController_DispatchesPairedMemoryEventsInDebug()
    {
        List<ObjectEvent> events = [];
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        HeroController controller;
        try
        {
            controller = new HeroController();
            controller.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }

        if (Constants.KDebugMode && FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            Assert.Collection(events,
                item =>
                {
                    var created = Assert.IsType<ObjectCreated>(item);
                    Assert.Same(controller, item.Object);
                    Assert.Equal("HeroController", created.ClassName);
                    Assert.Equal("package:flutter/widgets.dart", created.Library);
                },
                item =>
                {
                    Assert.IsType<ObjectDisposed>(item);
                    Assert.Same(controller, item.Object);
                });
        }
        else
        {
            Assert.Empty(events);
        }
    }

    [Fact]
    public void BuildModeGates_SharedHeroControllerReportsOwnershipAfterFrame()
    {
        using var controller = new HeroController();
        using var tester = new FrameworkDartTester();
        PumpHeroWidget(tester, new HeroControllerScope(controller, new Stack(children:
        [
            new Navigator(initialRoute: GeometryRoute()),
            new Navigator(initialRoute: GeometryRoute()),
        ])));

        object? exception = tester.TakeException();
        if (Constants.KDebugMode)
        {
            var error = Assert.IsType<FlutterError>(exception);
            Assert.Contains("A HeroController can not be shared by multiple Navigators.", error.Message);
            Assert.Contains("HeroControllerScope.none", error.Message);
        }
        else
        {
            Assert.Null(exception);
        }
    }

    [Fact]
    public void HeroController_CanMoveBetweenNavigatorsDuringSameBuild()
    {
        using var controller = new HeroController();
        using var tester = new FrameworkDartTester();
        Widget Navigators(bool firstOwnsController) => new Stack(children:
        [
            firstOwnsController
                ? new HeroControllerScope(controller, new Navigator(initialRoute: GeometryRoute()))
                : HeroControllerScope.None(new Navigator(initialRoute: GeometryRoute())),
            firstOwnsController
                ? HeroControllerScope.None(new Navigator(initialRoute: GeometryRoute()))
                : new HeroControllerScope(controller, new Navigator(initialRoute: GeometryRoute())),
        ]);

        PumpHeroWidget(tester, Navigators(false));
        NavigatorState previousOwner = controller.Navigator!;
        PumpHeroWidget(tester, Navigators(true));
        Assert.Null(tester.TakeException());
        Assert.NotSame(previousOwner, controller.Navigator);
        Assert.NotNull(controller.Navigator);
    }

    [Fact]
    public void BuildModeGates_HeroControllerScopeOfMissingAncestor()
    {
        using var tester = new FrameworkDartTester();
        PumpHeroWidget(tester, new SizedBox());
        Element context = tester.ElementOfType<SizedBox>();
        Assert.Null(HeroControllerScope.MaybeOf(context));
        if (Constants.KDebugMode)
        {
            var error = Assert.Throws<FlutterError>(() => HeroControllerScope.Of(context));
            Assert.Contains("The context used was:", error.Message);
        }
        else
        {
            Assert.Throws<NullReferenceException>(() => HeroControllerScope.Of(context));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Flight_PushAndPopUseHeroCurveAtQuartiles(bool linear, bool explicitReverseCurve)
    {
        using var flight = new FlightFixture(
            curve: linear ? Curves.Linear : Curves.FastOutSlowIn,
            reverseCurve: explicitReverseCurve ? Curves.Linear : null);
        flight.Push();
        for (int quarter = 1; quarter <= 4; quarter++)
        {
            flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
            double t = quarter / 4.0;
            double expected = 100 + 100 * (linear ? t : Curves.FastOutSlowIn.Transform(t));
            Assert.Equal(expected, flight.ChildBox.Size.Height, 6);
        }

        flight.Tester.PumpAndSettle();
        flight.Navigator.Pop();
        flight.Tester.Pump();
        flight.Tester.Pump();
        for (int quarter = 1; quarter <= 4; quarter++)
        {
            flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
            double t = quarter / 4.0;
            double progress = explicitReverseCurve || linear ? t : Curves.FastOutSlowIn.Transform(t);
            // A pop uses the reverse curve while the route animation decreases from 1 to 0.
            double expected = 200 - 100 * progress;
            if (quarter < 4)
            {
                Assert.Equal(expected, flight.ChildBox.Size.Height, 6);
            }
            else
            {
                flight.Tester.PumpAndSettle();
                Assert.Equal(100, flight.ChildBox.Size.Height);
            }
        }
    }

    [Fact]
    public void Flight_IgnoresPointerDuringAnimation_AndRestoresDestinationAfterwards()
    {
        using var flight = new FlightFixture();
        flight.Push();
        flight.Tester.TapAt(flight.ChildBox.LocalToGlobal(new Point(20, 20)));
        Assert.Equal(0, flight.Taps);
        flight.Tester.PumpAndSettle();
        flight.Tester.TapAt(flight.ChildBox.LocalToGlobal(new Point(20, 20)));
        Assert.Equal(1, flight.Taps);
    }

    [Fact]
    public void Flight_RetargetsMovingDestination_WithoutChangingItsCapturedSize()
    {
        using var flight = new FlightFixture();
        flight.Push();
        flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
        flight.MoveDestination(100);
        flight.Tester.Pump();
        flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
        flight.Tester.PumpAndSettle();
        Assert.Equal(100, flight.ChildBox.LocalToGlobal(default).Y, 6);
        Assert.Equal(200, flight.ChildBox.Size.Height);
        Assert.Null(flight.Tester.TakeException());
    }

    [Fact]
    public void Flight_UnpaintableDestinationFades_AndDoesNotAssertOnLocalSize()
    {
        using var flight = new FlightFixture();
        flight.Push();
        flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
        flight.HideDestination();
        flight.Tester.Pump();
        flight.Tester.Pump(TimeSpan.FromMilliseconds(100));
        flight.Tester.Pump(TimeSpan.FromMilliseconds(50));
        var fade = (FadeTransition)flight.Tester.ElementOfType<FadeTransition>().Widget;
        Assert.InRange(fade.Opacity.Value, 0.0, 0.999);
        flight.Tester.PumpAndSettle();
        Assert.Null(flight.Tester.TakeException());
    }

    [Fact]
    public void Flight_PopOnFirstFrameDoesNotLeaveHiddenHero()
    {
        using var flight = new FlightFixture();
        flight.Navigator.Push(flight.DestinationRoute);
        flight.Tester.Pump();
        flight.Navigator.Pop();
        flight.Tester.PumpAndSettle();
        Assert.Null(flight.Tester.TakeException());
        Assert.Single(flight.Tester.OnstageElements(), element => element.Widget is Hero);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildModeGates_NestedHeroesOnlyAssertInDebug(bool useBuilder)
    {
        using var tester = new FrameworkDartTester();
        var nested = new Hero("inner", new SizedBox(width: 20, height: 20));
        Widget child = useBuilder ? new Builder(_ => nested) : nested;
        PumpHeroWidget(tester, new Hero("outer", child));
        object? error = tester.TakeException();
        if (Constants.KDebugMode)
        {
            var assertion = Assert.IsType<AssertionError>(error);
            Assert.Contains("A Hero widget cannot be the descendant of another Hero widget.", assertion.Message);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(2, tester.StateList<HeroState>().Count);
        }
    }

    [Fact]
    public void Flight_DispatchesPairedMemoryEvents()
    {
        using var flight = new FlightFixture();
        List<ObjectEvent> events = [];
        void Listener(ObjectEvent item)
        {
            if (item.Object is HeroFlight)
            {
                events.Add(item);
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        try
        {
            flight.Push();
            flight.Tester.PumpAndSettle();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }

        if (Constants.KDebugMode && FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            Assert.Collection(events,
                item => Assert.Equal("_HeroFlight", Assert.IsType<ObjectCreated>(item).ClassName),
                item => Assert.IsType<ObjectDisposed>(item));
            Assert.Same(events[0].Object, events[1].Object);
        }
        else
        {
            Assert.Empty(events);
        }
    }

    private sealed class FlightFixture : IDisposable
    {
        private readonly HeroController _controller = new();
        private readonly Key _childKey = Key.Create("flight-destination");
        private StateSetter? _setDestinationState;
        private double _destinationY = 60;
        private bool _hidden;

        public FlightFixture(Curve? curve = null, Curve? reverseCurve = null)
        {
            Tester = new FrameworkDartTester();
            var sourceRoute = new PageRouteBuilder(
                pageBuilder: (context, _, _) =>
                {
                    Navigator = Plumix.Widgets.Navigator.Of(context);
                    return Page(100, childKey: null, curve, reverseCurve);
                },
                transitionDuration: TimeSpan.FromMilliseconds(400),
                reverseTransitionDuration: TimeSpan.FromMilliseconds(400));
            DestinationRoute = new PageRouteBuilder(
                pageBuilder: (_, _, _) => new StatefulBuilder((_, setState) =>
                {
                    _setDestinationState = setState;
                    return Page(200, _childKey, curve, reverseCurve);
                }),
                transitionDuration: TimeSpan.FromMilliseconds(400),
                reverseTransitionDuration: TimeSpan.FromMilliseconds(400));
            PumpHeroWidget(Tester, new HeroControllerScope(_controller, new Navigator(initialRoute: sourceRoute)));
        }

        public FrameworkDartTester Tester { get; }
        public NavigatorState Navigator { get; private set; } = null!;
        public PageRouteBuilder DestinationRoute { get; }
        public int Taps { get; private set; }
        public RenderBox ChildBox
        {
            get
            {
                Element? overlay = Tester.ElementsOfType<FadeTransition>().FirstOrDefault();
                BuildContext context = overlay ?? Tester.StateList<HeroState>()
                    .Single(hero => ModalRoute.MaybeOf(hero.Context)?.IsCurrent == true).Context;
                return (RenderBox)context.FindRenderObject()!;
            }
        }

        public void Push()
        {
            Navigator.Push(DestinationRoute);
            Tester.Pump();
            Tester.Pump();
        }

        public void MoveDestination(double y) => _setDestinationState!(() => _destinationY = y);
        public void HideDestination() => _setDestinationState!(() => _hidden = true);

        private Widget Page(double height, Key? childKey, Curve? curve, Curve? reverseCurve)
        {
            Widget hero = new Hero(
                SharedHeroTag,
                new GestureDetector(
                    onTap: () => Taps++,
                    behavior: HitTestBehavior.Opaque,
                    child: new SizedBox(width: 100, height: height, key: childKey)),
                curve: curve,
                reverseCurve: reverseCurve);
            if (_hidden && childKey is not null)
            {
                hero = new Transform(Matrix4.Zero(), child: hero);
            }

            return new Stack(children:
            [
                new Positioned(left: 20, top: childKey is null ? 20 : _destinationY, child: hero),
            ]);
        }

        public void Dispose()
        {
            Tester.Dispose();
            _controller.Dispose();
        }
    }

    private static void PumpHeroWidget(FrameworkDartTester tester, Widget widget) =>
        tester.PumpWidget(new Directionality(TextDirection.Ltr, widget));

    private static PageRouteBuilder GeometryRoute(double width = 44, double height = 44) =>
        new(pageBuilder: (_, _, _) => new Stack(children:
        [
            new Positioned(left: 20, top: 60,
                child: new Hero(SharedHeroTag, new SizedBox(width: width, height: height))),
        ]));

    private static HeroFlightManifest GeometryManifest(
        WidgetRenderHarness harness,
        PageRoute fromRoute,
        PageRoute toRoute,
        HeroState fromHero,
        HeroState toHero,
        bool diverted = false) => new(
            HeroFlightDirection.Push,
            harness.HeroController.Navigator!.Overlay!,
            new Size(320, 240),
            fromRoute,
            toRoute,
            fromHero,
            toHero,
            createRectTween: null,
            shuttleBuilder: (_, _, _, _, _) => new SizedBox(),
            isUserGestureTransition: false,
            isDiverted: diverted);
}
