using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/heroes.dart (baseline subset)

namespace Flutter.Widgets;

public delegate Flutter.Tween<Rect> CreateRectTween(Rect begin, Rect end);
public delegate Widget HeroFlightShuttleBuilder(
    BuildContext fromHeroContext,
    BuildContext toHeroContext,
    double progress,
    bool isPushTransition);
public delegate Widget HeroPlaceholderBuilder(
    BuildContext context,
    Size placeholderSize,
    Widget child);

public sealed class Hero : StatefulWidget
{
    public Hero(
        object tag,
        Widget child,
        Key? key = null,
        CreateRectTween? createRectTween = null,
        HeroFlightShuttleBuilder? flightShuttleBuilder = null,
        HeroPlaceholderBuilder? placeholderBuilder = null) : base(key)
    {
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        CreateRectTween = createRectTween;
        FlightShuttleBuilder = flightShuttleBuilder;
        PlaceholderBuilder = placeholderBuilder;
    }

    public object Tag { get; }

    public Widget Child { get; }

    public CreateRectTween? CreateRectTween { get; }

    public HeroFlightShuttleBuilder? FlightShuttleBuilder { get; }

    public HeroPlaceholderBuilder? PlaceholderBuilder { get; }

    public override State CreateState()
    {
        return new HeroState();
    }
}

internal sealed class HeroState : State
{
    private HeroControllerScope? _scope;
    private Route? _route;
    private object? _registeredTag;

    private Hero CurrentWidget => (Hero)StateWidget;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        RegisterWithScope();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        RegisterWithScope();
    }

    public override void Deactivate()
    {
        Unregister();
        base.Deactivate();
    }

    public override void Dispose()
    {
        Unregister();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        EnsureNoHeroAncestor();

        var placeholderState = _scope?.Controller.ResolvePlaceholder(_route, CurrentWidget.Tag);
        if (placeholderState == null)
        {
            return CurrentWidget.Child;
        }

        if (CurrentWidget.PlaceholderBuilder != null)
        {
            return CurrentWidget.PlaceholderBuilder(
                context,
                placeholderState.Value.Size,
                CurrentWidget.Child);
        }

        if (placeholderState.Value.IncludeChild)
        {
            return new SizedBox(
                width: placeholderState.Value.Size.Width,
                height: placeholderState.Value.Size.Height,
                child: new Offstage(
                    child: CurrentWidget.Child,
                    offstage: true));
        }

        return new SizedBox(
            width: placeholderState.Value.Size.Width,
            height: placeholderState.Value.Size.Height);
    }

    private void EnsureNoHeroAncestor()
    {
        var ancestor = Element.Parent;
        while (ancestor != null)
        {
            if (ancestor.Widget is Hero)
            {
                throw new InvalidOperationException("A Hero widget cannot be the descendant of another Hero widget.");
            }

            ancestor = ancestor.Parent;
        }
    }

    internal HeroSnapshot? CreateSnapshot(Route expectedRoute)
    {
        if (_route == null || !ReferenceEquals(_route, expectedRoute))
        {
            return null;
        }

        if (Element.RenderObject is not RenderBox renderBox || !renderBox.HasSize)
        {
            return null;
        }

        var bounds = ResolveGlobalBounds(renderBox);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        return new HeroSnapshot(bounds, CurrentWidget.Child);
    }

    internal CreateRectTween? ResolveCreateRectTween()
    {
        return CurrentWidget.CreateRectTween;
    }

    internal HeroFlightShuttleBuilder? ResolveFlightShuttleBuilder()
    {
        return CurrentWidget.FlightShuttleBuilder;
    }

    private void RegisterWithScope()
    {
        var scope = HeroControllerScope.MaybeOf(Context);
        var route = scope?.Route;
        var tag = CurrentWidget.Tag;

        var registrationChanged =
            !ReferenceEquals(_scope, scope)
            || !ReferenceEquals(_route, route)
            || !Equals(_registeredTag, tag);

        if (!registrationChanged)
        {
            return;
        }

        Unregister();

        _scope = scope;
        _route = route;
        _registeredTag = tag;

        if (_scope != null && _route != null)
        {
            _scope.Controller.Register(_route, tag, this);
        }
    }

    private void Unregister()
    {
        if (_scope != null && _route != null && _registeredTag != null)
        {
            _scope.Controller.Unregister(_route, _registeredTag, this);
        }

        _scope = null;
        _route = null;
        _registeredTag = null;
    }

    private static Rect ResolveGlobalBounds(RenderBox renderBox)
    {
        var localRect = new Rect(new Point(0, 0), renderBox.Size);
        var transformToRoot = ResolveRenderObjectTransformToRoot(renderBox);
        return RenderObject.TransformRect(transformToRoot, localRect);
    }

    private static Matrix ResolveRenderObjectTransformToRoot(RenderObject renderObject)
    {
        var transformToRoot = Matrix.Identity;
        RenderObject? child = renderObject;

        while (child?.Parent != null)
        {
            var parent = child.Parent;
            var childOffset = child.parentData is BoxParentData boxParentData
                ? boxParentData.offset
                : default;
            var childToParentTransform = Matrix.CreateTranslation(childOffset.X, childOffset.Y);

            if (parent is RenderTransform renderTransform)
            {
                childToParentTransform *= renderTransform.Transform;
            }

            transformToRoot = childToParentTransform * transformToRoot;
            child = parent;
        }

        return transformToRoot;
    }
}

internal readonly record struct HeroSnapshot(Rect Bounds, Widget Child);

internal sealed class HeroFlightManifest
{
    public HeroFlightManifest(
        object tag,
        Route fromRoute,
        Route toRoute,
        HeroState fromHero,
        HeroState toHero,
        Rect fromBounds,
        Rect toBounds,
        Widget defaultShuttle,
        HeroFlightShuttleBuilder? shuttleBuilder,
        Flutter.Tween<Rect> rectTween)
    {
        Tag = tag;
        FromRoute = fromRoute;
        ToRoute = toRoute;
        FromHero = fromHero;
        ToHero = toHero;
        FromBounds = fromBounds;
        ToBounds = toBounds;
        DefaultShuttle = defaultShuttle;
        ShuttleBuilder = shuttleBuilder;
        RectTween = rectTween ?? new Flutter.RectTween();
    }

    public object Tag { get; }

    public Route FromRoute { get; }

    public Route ToRoute { get; }

    public HeroState FromHero { get; }

    public HeroState ToHero { get; }

    public Rect FromBounds { get; }

    public Rect ToBounds { get; }

    public Widget DefaultShuttle { get; }

    public HeroFlightShuttleBuilder? ShuttleBuilder { get; }

    public Flutter.Tween<Rect> RectTween { get; }

    public Widget BuildShuttle(double progress, bool isPushTransition)
    {
        return ShuttleBuilder?.Invoke(
                   FromHero.Context,
                   ToHero.Context,
                   progress,
                   isPushTransition)
               ?? DefaultShuttle;
    }
}

internal sealed class HeroTransitionController
{
    private readonly Dictionary<Route, Dictionary<object, HeroState>> _heroesByRoute = [];
    private readonly Dictionary<(Route Route, object Tag), HeroPlaceholderState> _hiddenHeroes = [];
    private IReadOnlyList<HeroFlightManifest> _activeFlights = [];

    public IReadOnlyList<HeroFlightManifest> ActiveFlights => _activeFlights;

    public bool HasHeroes(Route route)
    {
        return _heroesByRoute.TryGetValue(route, out var heroes) && heroes.Count > 0;
    }

    public HeroPlaceholderState? ResolvePlaceholder(Route? route, object tag)
    {
        if (route == null)
        {
            return null;
        }

        return _hiddenHeroes.TryGetValue((route, tag), out var placeholderState)
            ? placeholderState
            : null;
    }

    public void Register(Route route, object tag, HeroState heroState)
    {
        if (!_heroesByRoute.TryGetValue(route, out var heroes))
        {
            heroes = [];
            _heroesByRoute[route] = heroes;
        }

        if (heroes.TryGetValue(tag, out var existingHeroState) && !ReferenceEquals(existingHeroState, heroState))
        {
            throw new InvalidOperationException(
                $"There are multiple heroes that share the same tag within one route subtree. Tag: {tag}.");
        }

        heroes[tag] = heroState;
    }

    public void Unregister(Route route, object tag, HeroState heroState)
    {
        if (!_heroesByRoute.TryGetValue(route, out var heroes))
        {
            return;
        }

        if (heroes.TryGetValue(tag, out var registeredState) && ReferenceEquals(registeredState, heroState))
        {
            heroes.Remove(tag);
        }

        if (heroes.Count == 0)
        {
            _heroesByRoute.Remove(route);
        }
    }

    public IReadOnlyList<HeroFlightManifest> CreateFlights(Route fromRoute, Route toRoute)
    {
        if (!_heroesByRoute.TryGetValue(fromRoute, out var fromHeroes) || fromHeroes.Count == 0)
        {
            return [];
        }

        if (!_heroesByRoute.TryGetValue(toRoute, out var toHeroes) || toHeroes.Count == 0)
        {
            return [];
        }

        var flights = new List<HeroFlightManifest>();
        foreach (var (tag, fromHero) in fromHeroes)
        {
            if (!toHeroes.TryGetValue(tag, out var toHero))
            {
                continue;
            }

            var fromSnapshot = fromHero.CreateSnapshot(fromRoute);
            var toSnapshot = toHero.CreateSnapshot(toRoute);
            if (fromSnapshot == null || toSnapshot == null)
            {
                continue;
            }

            var rectTweenFactory = toHero.ResolveCreateRectTween();
            var rectTween = rectTweenFactory?.Invoke(fromSnapshot.Value.Bounds, toSnapshot.Value.Bounds)
                ?? new Flutter.RectTween();
            var shuttleBuilder = toHero.ResolveFlightShuttleBuilder() ?? fromHero.ResolveFlightShuttleBuilder();

            flights.Add(
                new HeroFlightManifest(
                    tag: tag,
                    fromRoute: fromRoute,
                    toRoute: toRoute,
                    fromHero: fromHero,
                    toHero: toHero,
                    fromBounds: fromSnapshot.Value.Bounds,
                    toBounds: toSnapshot.Value.Bounds,
                    defaultShuttle: toSnapshot.Value.Child,
                    shuttleBuilder: shuttleBuilder,
                    rectTween: rectTween));
        }

        return flights;
    }

    public void ActivateFlights(IReadOnlyList<HeroFlightManifest> flights, bool isPushTransition)
    {
        if (flights.Count == 0)
        {
            _activeFlights = [];
            _hiddenHeroes.Clear();
            return;
        }

        _activeFlights = flights.ToArray();
        UpdateActiveFlightPlaceholders(isPushTransition);
    }

    public void UpdateActiveFlightPlaceholders(bool isPushTransition)
    {
        _hiddenHeroes.Clear();
        if (_activeFlights.Count == 0)
        {
            return;
        }

        foreach (var flight in _activeFlights)
        {
            _hiddenHeroes[(flight.FromRoute, flight.Tag)] = new HeroPlaceholderState(
                new Size(flight.FromBounds.Width, flight.FromBounds.Height),
                IncludeChild: isPushTransition);
            _hiddenHeroes[(flight.ToRoute, flight.Tag)] = new HeroPlaceholderState(
                new Size(flight.ToBounds.Width, flight.ToBounds.Height),
                IncludeChild: false);
        }
    }

    public void ClearFlights()
    {
        _activeFlights = [];
        _hiddenHeroes.Clear();
    }
}

internal readonly record struct HeroPlaceholderState(Size Size, bool IncludeChild);

internal sealed class HeroControllerScope : InheritedWidget
{
    public HeroControllerScope(
        HeroTransitionController controller,
        Route route,
        Widget child,
        Key? key = null) : base(key)
    {
        Controller = controller;
        Route = route;
        Child = child;
    }

    public HeroTransitionController Controller { get; }

    public Route Route { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (HeroControllerScope)oldWidget;
        return !ReferenceEquals(oldScope.Controller, Controller)
               || !ReferenceEquals(oldScope.Route, Route);
    }

    public static HeroControllerScope? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<HeroControllerScope>();
    }
}
