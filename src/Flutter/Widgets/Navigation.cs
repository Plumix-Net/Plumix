using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Flutter.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/navigator.dart; flutter/packages/flutter/lib/src/widgets/routes.dart (approximate)

namespace Flutter.Widgets;

public sealed record RouteSettings(string? Name = null, object? Arguments = null);
public delegate Route? RouteFactory(RouteSettings settings);
public delegate bool RoutePredicate(Route route);

public sealed class RouteData
{
    private static readonly IReadOnlyDictionary<string, string> EmptyQueryParameters =
        new Dictionary<string, string>();

    public RouteData(
        string name,
        IReadOnlyDictionary<string, string>? queryParameters = null,
        object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        QueryParameters = queryParameters ?? EmptyQueryParameters;
        Arguments = arguments;
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> QueryParameters { get; }

    public object? Arguments { get; }

    public static RouteData FromLocation(string location, object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location cannot be null or whitespace.", nameof(location));
        }

        var normalized = NormalizeLocation(location);
        var queryIndex = normalized.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex >= 0
            ? normalized[..queryIndex]
            : normalized;
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        var query = queryIndex >= 0 ? normalized[(queryIndex + 1)..] : string.Empty;
        return new RouteData(
            name: path,
            queryParameters: ParseQueryParameters(query),
            arguments: arguments);
    }

    private static string NormalizeLocation(string location)
    {
        if (location.Contains("://", StringComparison.Ordinal)
            && Uri.TryCreate(location, UriKind.Absolute, out var absoluteUri))
        {
            var normalized = absoluteUri.PathAndQuery;
            if (!string.IsNullOrEmpty(absoluteUri.Fragment))
            {
                normalized += absoluteUri.Fragment;
            }

            return normalized;
        }

        return location;
    }

    private static IReadOnlyDictionary<string, string> ParseQueryParameters(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return EmptyQueryParameters;
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            var rawKey = separatorIndex < 0 ? pair : pair[..separatorIndex];
            if (rawKey.Length == 0)
            {
                continue;
            }

            var rawValue = separatorIndex < 0 ? string.Empty : pair[(separatorIndex + 1)..];
            var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
            var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            parameters[key] = value;
        }

        return parameters.Count == 0
            ? EmptyQueryParameters
            : parameters;
    }
}

public sealed class LocalHistoryEntry
{
    private bool _removed;

    public LocalHistoryEntry(Action? onRemove = null, bool impliesAppBarDismissal = true)
    {
        OnRemove = onRemove;
        ImpliesAppBarDismissal = impliesAppBarDismissal;
    }

    public Action? OnRemove { get; }

    public bool ImpliesAppBarDismissal { get; }

    internal Route? Owner { get; set; }

    public void Remove()
    {
        if (_removed)
        {
            return;
        }

        var owner = Owner;
        if (owner != null)
        {
            owner.RemoveLocalHistoryEntry(this);
            return;
        }

        MarkRemoved();
    }

    internal void MarkRemoved()
    {
        if (_removed)
        {
            return;
        }

        _removed = true;
        Owner = null;
        OnRemove?.Invoke();
    }
}

public abstract class Route
{
    private List<LocalHistoryEntry>? _localHistoryEntries;

    protected Route(RouteSettings? settings = null)
    {
        Settings = settings ?? new RouteSettings();
    }

    public RouteSettings Settings { get; }

    internal NavigatorState? Navigator { get; private set; }

    public virtual bool ImpliesAppBarDismissal
    {
        get
        {
            var hasDismissalLocalHistory = _localHistoryEntries?.Any(entry => entry.ImpliesAppBarDismissal) == true;
            return hasDismissalLocalHistory || (Navigator?.CanPop ?? false);
        }
    }

    internal void Attach(NavigatorState navigator)
    {
        Navigator = navigator;
        OnAttach();
    }

    internal void Detach()
    {
        OnDetach();
        Navigator = null;
    }

    protected virtual void OnAttach()
    {
    }

    protected virtual void OnDetach()
    {
    }

    public virtual bool WillPop()
    {
        if (_localHistoryEntries is { Count: > 0 })
        {
            var entry = _localHistoryEntries[^1];
            _localHistoryEntries.RemoveAt(_localHistoryEntries.Count - 1);
            entry.MarkRemoved();
            OnLocalHistoryChanged();
            return false;
        }

        return true;
    }

    public virtual void DidPush()
    {
    }

    public virtual void DidPop(Route? previousRoute)
    {
    }

    public virtual void DidPopNext(Route nextRoute)
    {
    }

    public virtual void DidChangeNext(Route? nextRoute)
    {
    }

    public virtual void DidChangePrevious(Route? previousRoute)
    {
    }

    public virtual void Dispose()
    {
        if (_localHistoryEntries is not { Count: > 0 })
        {
            return;
        }

        foreach (var entry in _localHistoryEntries.ToArray())
        {
            _localHistoryEntries.Remove(entry);
            entry.MarkRemoved();
        }

        OnLocalHistoryChanged();
    }

    public void AddLocalHistoryEntry(LocalHistoryEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry.Owner != null && !ReferenceEquals(entry.Owner, this))
        {
            throw new InvalidOperationException("LocalHistoryEntry is already attached to another route.");
        }

        _localHistoryEntries ??= [];
        if (_localHistoryEntries.Contains(entry))
        {
            return;
        }

        entry.Owner = this;
        _localHistoryEntries.Add(entry);
        OnLocalHistoryChanged();
    }

    internal void RemoveLocalHistoryEntry(LocalHistoryEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (_localHistoryEntries is null)
        {
            return;
        }

        if (_localHistoryEntries.Remove(entry))
        {
            entry.MarkRemoved();
            OnLocalHistoryChanged();
        }
    }

    protected virtual void OnLocalHistoryChanged()
    {
    }

    public abstract Widget BuildPage(BuildContext context);
}

public abstract class ModalRoute : Route
{
    protected ModalRoute(RouteSettings? settings = null) : base(settings)
    {
    }

    public static ModalRoute Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("ModalRoute not found in context.");
    }

    public static ModalRoute? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<RouteScope>()?.Route as ModalRoute;
    }
}

public abstract class PageRoute : ModalRoute
{
    protected PageRoute(RouteSettings? settings = null, bool fullscreenDialog = false) : base(settings)
    {
        FullscreenDialog = fullscreenDialog;
    }

    public bool FullscreenDialog { get; }
}

public sealed class BuilderPageRoute : PageRoute
{
    private readonly Func<BuildContext, Widget> _builder;

    public BuilderPageRoute(
        Func<BuildContext, Widget> builder,
        RouteSettings? settings = null,
        bool fullscreenDialog = false) : base(settings, fullscreenDialog)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public override Widget BuildPage(BuildContext context)
    {
        return _builder(context);
    }
}

public abstract class NavigatorObserver
{
    public NavigatorState? Navigator { get; internal set; }

    public virtual void DidPush(Route route, Route? previousRoute)
    {
    }

    public virtual void DidPop(Route route, Route? previousRoute)
    {
    }

    public virtual void DidRemove(Route route, Route? previousRoute)
    {
    }

    public virtual void DidReplace(Route newRoute, Route? oldRoute)
    {
    }

    public virtual void DidStartUserGesture(Route route, Route? previousRoute)
    {
    }

    public virtual void DidStopUserGesture()
    {
    }
}

public interface RouteAware
{
    void DidPush();

    void DidPop();

    void DidPopNext();

    void DidPushNext();
}

public class RouteObserver<TRoute> : NavigatorObserver where TRoute : Route
{
    private readonly Dictionary<TRoute, HashSet<RouteAware>> _listeners = [];

    public void Subscribe(RouteAware routeAware, TRoute route)
    {
        if (routeAware == null)
        {
            throw new ArgumentNullException(nameof(routeAware));
        }

        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (!_listeners.TryGetValue(route, out var subscribers))
        {
            subscribers = [];
            _listeners[route] = subscribers;
        }

        if (subscribers.Add(routeAware))
        {
            routeAware.DidPush();
        }
    }

    public void Unsubscribe(RouteAware routeAware)
    {
        if (routeAware == null)
        {
            throw new ArgumentNullException(nameof(routeAware));
        }

        foreach (var entry in _listeners.ToArray())
        {
            var subscribers = entry.Value;
            subscribers.Remove(routeAware);
            if (subscribers.Count == 0)
            {
                _listeners.Remove(entry.Key);
            }
        }
    }

    public override void DidPush(Route route, Route? previousRoute)
    {
        if (previousRoute is not TRoute previousTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(previousTypedRoute, out var subscribers))
        {
            return;
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.DidPushNext();
        }
    }

    public override void DidPop(Route route, Route? previousRoute)
    {
        if (previousRoute is TRoute previousTypedRoute
            && _listeners.TryGetValue(previousTypedRoute, out var previousSubscribers))
        {
            foreach (var subscriber in previousSubscribers.ToArray())
            {
                subscriber.DidPopNext();
            }
        }

        if (route is not TRoute typedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(typedRoute, out var subscribers))
        {
            return;
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.DidPop();
        }
    }

    public override void DidReplace(Route newRoute, Route? oldRoute)
    {
        if (oldRoute is not TRoute oldTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(oldTypedRoute, out var oldSubscribers))
        {
            return;
        }

        _listeners.Remove(oldTypedRoute);

        if (newRoute is not TRoute newTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(newTypedRoute, out var newSubscribers))
        {
            _listeners[newTypedRoute] = oldSubscribers;
            return;
        }

        foreach (var subscriber in oldSubscribers)
        {
            newSubscribers.Add(subscriber);
        }
    }
}

public sealed class Navigator : StatefulWidget
{
    public Navigator(
        Route initialRoute,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null) : base(key)
    {
        InitialRoute = initialRoute ?? throw new ArgumentNullException(nameof(initialRoute));
        Observers = observers ?? [];
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        RouteData initialRouteData,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null) : base(key)
    {
        OnGenerateRoute = onGenerateRoute ?? throw new ArgumentNullException(nameof(onGenerateRoute));
        InitialRouteData = initialRouteData ?? throw new ArgumentNullException(nameof(initialRouteData));
        Observers = observers ?? [];
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        string initialRouteName = "/",
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null) : base(key)
    {
        if (string.IsNullOrWhiteSpace(initialRouteName))
        {
            throw new ArgumentException("initialRouteName cannot be null or whitespace.", nameof(initialRouteName));
        }

        OnGenerateRoute = onGenerateRoute ?? throw new ArgumentNullException(nameof(onGenerateRoute));
        InitialRouteName = initialRouteName;
        Observers = observers ?? [];
    }

    public Route? InitialRoute { get; }

    public string? InitialRouteName { get; }

    public RouteData? InitialRouteData { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public IReadOnlyList<NavigatorObserver> Observers { get; }

    public override State CreateState()
    {
        return new NavigatorState();
    }

    public static NavigatorState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("Navigator not found in context.");
    }

    public static NavigatorState? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<NavigatorScope>()?.Navigator;
    }

    public static bool CanPop(BuildContext context)
    {
        return MaybeOf(context)?.CanPop ?? false;
    }

    public static bool MaybePop(BuildContext context, object? result = null)
    {
        var navigator = MaybeOf(context);
        if (navigator == null)
        {
            return false;
        }

        return navigator.MaybePop(result);
    }

    public static void Pop(BuildContext context, object? result = null)
    {
        Of(context).Pop(result);
    }

    public static void PopUntil(BuildContext context, RoutePredicate predicate)
    {
        Of(context).PopUntil(predicate);
    }

    public static void Push(BuildContext context, Route route)
    {
        Of(context).Push(route);
    }

    public static void PushAndRemoveUntil(BuildContext context, Route newRoute, RoutePredicate predicate)
    {
        Of(context).PushAndRemoveUntil(newRoute, predicate);
    }

    public static void PushNamed(BuildContext context, string routeName, object? arguments = null)
    {
        Of(context).PushNamed(routeName, arguments);
    }

    public static void PushNamed(BuildContext context, RouteData routeData)
    {
        Of(context).PushNamed(routeData);
    }

    public static void PushNamedAndRemoveUntil(
        BuildContext context,
        string routeName,
        RoutePredicate predicate,
        object? arguments = null)
    {
        Of(context).PushNamedAndRemoveUntil(routeName, predicate, arguments);
    }

    public static void PushNamedAndRemoveUntil(BuildContext context, RouteData routeData, RoutePredicate predicate)
    {
        Of(context).PushNamedAndRemoveUntil(routeData, predicate);
    }

    public static void PushReplacement(BuildContext context, Route newRoute, object? result = null)
    {
        Of(context).PushReplacement(newRoute, result);
    }

    public static void PushReplacementNamed(
        BuildContext context,
        string routeName,
        object? arguments = null,
        object? result = null)
    {
        Of(context).PushReplacementNamed(routeName, arguments, result);
    }

    public static void PushReplacementNamed(BuildContext context, RouteData routeData, object? result = null)
    {
        Of(context).PushReplacementNamed(routeData, result);
    }

    public static void RemoveRoute(BuildContext context, Route route, object? result = null)
    {
        Of(context).RemoveRoute(route, result);
    }

    public static void RemoveRouteBelow(BuildContext context, Route anchorRoute, object? result = null)
    {
        Of(context).RemoveRouteBelow(anchorRoute, result);
    }

    public static void StartUserGesture(BuildContext context)
    {
        Of(context).StartUserGesture();
    }

    public static void StopUserGesture(BuildContext context)
    {
        Of(context).StopUserGesture();
    }

    public static bool MaybePopFromUserGesture(BuildContext context, object? result = null)
    {
        return Of(context).MaybePopFromUserGesture(result);
    }

    public static bool TryHandleBackButton()
    {
        return NavigatorBackButtonDispatcher.DispatchBackButton();
    }
}

public sealed class NavigatorState : State
{
    private const double HeroTransitionDurationMilliseconds = 300;
    private readonly List<Route> _history = [];
    private readonly List<NavigatorObserver> _observers = [];
    private readonly Func<bool> _backButtonHandler;
    private readonly HeroTransitionController _heroTransitionController = new();
    private readonly Flutter.AnimationController _heroFlightController;
    private int _userGestureCount;
    private HeroTransitionSession? _heroTransitionSession;

    public NavigatorState()
    {
        _backButtonHandler = HandleBackButton;
        _heroFlightController = new Flutter.AnimationController(
            TimeSpan.FromMilliseconds(HeroTransitionDurationMilliseconds))
        {
            Curve = Flutter.Curves.EaseInOut,
        };
        _heroFlightController.Changed += HandleHeroFlightTick;
        _heroFlightController.Completed += HandleHeroFlightCompleted;
        _heroFlightController.Dismissed += HandleHeroFlightCompleted;
    }

    private Navigator CurrentWidget => (Navigator)Element.Widget;

    public bool CanPop => _history.Count > 1;

    public Route? CurrentRoute => _history.Count == 0 ? null : _history[^1];

    public bool UserGestureInProgress => _userGestureCount > 0;

    public override void InitState()
    {
        base.InitState();
        SyncObservers(Array.Empty<NavigatorObserver>(), CurrentWidget.Observers);
        PushInitialRoute();
        NavigatorBackButtonDispatcher.AddHandler(_backButtonHandler);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldNavigator = (Navigator)oldWidget;
        SyncObservers(oldNavigator.Observers, CurrentWidget.Observers);
        if (_history.Count == 0)
        {
            PushInitialRoute();
        }
    }

    public override void Activate()
    {
        base.Activate();
        NavigatorBackButtonDispatcher.AddHandler(_backButtonHandler);
    }

    public override void Deactivate()
    {
        NavigatorBackButtonDispatcher.RemoveHandler(_backButtonHandler);
        base.Deactivate();
    }

    public override void Dispose()
    {
        NavigatorBackButtonDispatcher.RemoveHandler(_backButtonHandler);
        StopUserGesture();
        CancelHeroTransition(disposeDetachedRoute: true);
        _heroFlightController.Changed -= HandleHeroFlightTick;
        _heroFlightController.Completed -= HandleHeroFlightCompleted;
        _heroFlightController.Dismissed -= HandleHeroFlightCompleted;
        _heroFlightController.Dispose();

        foreach (var route in _history.ToArray())
        {
            route.Dispose();
            route.Detach();
        }

        _history.Clear();
        foreach (var observer in _observers)
        {
            if (ReferenceEquals(observer.Navigator, this))
            {
                observer.Navigator = null;
            }
        }

        _observers.Clear();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        Widget child;
        if (_heroTransitionSession == null)
        {
            var route = CurrentRoute;
            child = route == null
                ? new SizedBox()
                : BuildRouteHost(route);
        }
        else
        {
            child = BuildHeroTransitionHost(_heroTransitionSession);
        }

        return new NavigatorScope(this, child);
    }

    private ActiveRouteHost BuildRouteHost(Route route)
    {
        return new ActiveRouteHost(
            route: route,
            heroTransitionController: _heroTransitionController,
            key: new ObjectKey(route));
    }

    private Widget BuildHeroTransitionHost(HeroTransitionSession session)
    {
        var children = new List<Widget>();
        if (session.Direction == HeroTransitionDirection.Pop)
        {
            children.Add(BuildRouteHost(session.ToRoute));
            children.Add(BuildRouteHost(session.FromRoute));
        }
        else
        {
            children.Add(BuildRouteHost(session.FromRoute));
            children.Add(BuildRouteHost(session.ToRoute));
        }

        var flights = _heroTransitionController.ActiveFlights;
        if (flights.Count > 0)
        {
            children.Add(
                BuildHeroFlightOverlay(
                    flights,
                    _heroFlightController.Evaluate(),
                    isPushTransition: session.Direction == HeroTransitionDirection.Push));
        }

        return new Stack(children: children);
    }

    private static Widget BuildHeroFlightOverlay(
        IReadOnlyList<HeroFlightManifest> flights,
        double progress,
        bool isPushTransition)
    {
        var overlayChildren = new List<Widget>(flights.Count);
        foreach (var flight in flights)
        {
            var rect = flight.RectTween.Evaluate(progress, flight.FromBounds, flight.ToBounds);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            overlayChildren.Add(
                new Positioned(
                    left: rect.X,
                    top: rect.Y,
                    width: rect.Width,
                    height: rect.Height,
                    child: new SizedBox(
                        width: rect.Width,
                        height: rect.Height,
                        child: flight.BuildShuttle(progress, isPushTransition))));
        }

        if (overlayChildren.Count == 0)
        {
            return new SizedBox();
        }

        return new Stack(children: overlayChildren);
    }

    public void Push(Route route)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var previousRoute = CurrentRoute;
            InstallRoute(route);
            previousRoute?.DidChangeNext(route);
            route.DidChangePrevious(previousRoute);
            route.DidPush();
            NotifyObserversPush(route, previousRoute);
            TryStartHeroTransition(
                fromRoute: previousRoute,
                toRoute: route,
                direction: HeroTransitionDirection.Push,
                detachFromRouteOnComplete: false);
        });
    }

    public void PushAndRemoveUntil(Route newRoute, RoutePredicate predicate)
    {
        if (newRoute == null)
        {
            throw new ArgumentNullException(nameof(newRoute));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var previousRoute = CurrentRoute;
            InstallRoute(newRoute);
            previousRoute?.DidChangeNext(newRoute);
            newRoute.DidChangePrevious(previousRoute);
            newRoute.DidPush();
            NotifyObserversPush(newRoute, previousRoute);
            RemoveRoutesBelowTopUntil(predicate);
        });
    }

    public void PushNamed(string routeName, object? arguments = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        Push(route);
    }

    public void PushNamed(RouteData routeData)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        Push(ResolveRouteData(routeData));
    }

    public void PushNamedAndRemoveUntil(string routeName, RoutePredicate predicate, object? arguments = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        PushAndRemoveUntil(route, predicate);
    }

    public void PushNamedAndRemoveUntil(RouteData routeData, RoutePredicate predicate)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        var route = ResolveRouteData(routeData);
        PushAndRemoveUntil(route, predicate);
    }

    public bool MaybePop(object? result = null)
    {
        var route = CurrentRoute;
        if (route == null)
        {
            return false;
        }

        if (!route.WillPop())
        {
            return true;
        }

        if (!CanPop)
        {
            return false;
        }

        SetState(() =>
        {
            PopCurrentRoute(result);
        });
        return true;
    }

    public void Pop(object? result = null)
    {
        if (!MaybePop(result))
        {
            throw new InvalidOperationException("Navigator cannot pop the current route.");
        }
    }

    public bool MaybePopFromUserGesture(object? result = null)
    {
        StartUserGesture();
        try
        {
            return MaybePop(result);
        }
        finally
        {
            StopUserGesture();
        }
    }

    public void PopUntil(RoutePredicate predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            while (_history.Count > 0)
            {
                var route = _history[^1];
                if (predicate(route))
                {
                    break;
                }

                if (_history.Count <= 1 || !route.WillPop())
                {
                    break;
                }

                PopCurrentRoute(result: null);
            }
        });
    }

    public void PushReplacement(Route newRoute, object? result = null)
    {
        if (newRoute == null)
        {
            throw new ArgumentNullException(nameof(newRoute));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var oldRoute = CurrentRoute;
            if (oldRoute == null)
            {
                InstallRoute(newRoute);
                newRoute.DidPush();
                NotifyObserversPush(newRoute, previousRoute: null);
                _ = result;
                return;
            }

            if (ReferenceEquals(oldRoute, newRoute))
            {
                throw new InvalidOperationException("Cannot replace a route with itself.");
            }

            var previousRoute = _history.Count > 1 ? _history[^2] : null;
            _history[^1] = newRoute;
            InstallRoute(newRoute);
            previousRoute?.DidChangeNext(newRoute);
            newRoute.DidChangePrevious(previousRoute);
            newRoute.DidPush();

            oldRoute.DidPop(previousRoute);
            oldRoute.Dispose();
            oldRoute.Detach();
            previousRoute?.DidPopNext(oldRoute);
            NotifyObserversReplace(newRoute, oldRoute);
            _ = result;
        });
    }

    public void PushReplacementNamed(string routeName, object? arguments = null, object? result = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        PushReplacement(route, result);
    }

    public void PushReplacementNamed(RouteData routeData, object? result = null)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        PushReplacement(ResolveRouteData(routeData), result);
    }

    public void RemoveRoute(Route route, object? result = null)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var index = _history.FindIndex(existing => ReferenceEquals(existing, route));
            if (index < 0)
            {
                throw new InvalidOperationException("Route is not present in Navigator history.");
            }

            if (_history.Count <= 1)
            {
                throw new InvalidOperationException("Navigator cannot remove the last route.");
            }

            RemoveRouteAt(index, result);
        });
    }

    public void RemoveRouteBelow(Route anchorRoute, object? result = null)
    {
        if (anchorRoute == null)
        {
            throw new ArgumentNullException(nameof(anchorRoute));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var anchorIndex = _history.FindIndex(existing => ReferenceEquals(existing, anchorRoute));
            if (anchorIndex < 0)
            {
                throw new InvalidOperationException("Anchor route is not present in Navigator history.");
            }

            var removeIndex = anchorIndex - 1;
            if (removeIndex < 0)
            {
                throw new InvalidOperationException("Anchor route does not have a route below it.");
            }

            RemoveRouteAt(removeIndex, result);
        });
    }

    public void StartUserGesture()
    {
        var route = CurrentRoute;
        if (route == null)
        {
            return;
        }

        _userGestureCount += 1;
        if (_userGestureCount > 1)
        {
            return;
        }

        var previousRoute = _history.Count > 1 ? _history[^2] : null;
        NotifyObserversStartUserGesture(route, previousRoute);
    }

    public void StopUserGesture()
    {
        if (_userGestureCount == 0)
        {
            return;
        }

        _userGestureCount -= 1;
        if (_userGestureCount > 0)
        {
            return;
        }

        NotifyObserversStopUserGesture();
    }

    private void TryStartHeroTransition(
        Route? fromRoute,
        Route? toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete)
    {
        if (fromRoute == null || toRoute == null)
        {
            return;
        }

        if (TryDivertHeroTransition(fromRoute, toRoute, direction, detachFromRouteOnComplete))
        {
            return;
        }

        if (!_heroTransitionController.HasHeroes(fromRoute))
        {
            return;
        }

        CancelHeroTransition(disposeDetachedRoute: true);
        _heroTransitionSession = new HeroTransitionSession(
            fromRoute: fromRoute,
            toRoute: toRoute,
            direction: direction,
            detachFromRouteOnComplete: detachFromRouteOnComplete);

        Scheduler.AddPostFrameCallback(_ => ResolvePendingHeroTransition());
    }

    private bool TryDivertHeroTransition(
        Route fromRoute,
        Route toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete)
    {
        var activeSession = _heroTransitionSession;
        if (activeSession == null
            || activeSession.Direction != HeroTransitionDirection.Push
            || direction != HeroTransitionDirection.Pop)
        {
            return false;
        }

        if (!ReferenceEquals(activeSession.FromRoute, toRoute)
            || !ReferenceEquals(activeSession.ToRoute, fromRoute))
        {
            return false;
        }

        if (_heroTransitionController.ActiveFlights.Count == 0)
        {
            return false;
        }

        _heroTransitionSession = new HeroTransitionSession(
            fromRoute: fromRoute,
            toRoute: toRoute,
            direction: direction,
            detachFromRouteOnComplete: detachFromRouteOnComplete);
        _heroTransitionController.UpdateActiveFlightPlaceholders(isPushTransition: false);
        _heroFlightController.Reverse(from: _heroFlightController.Value);
        return true;
    }

    private void ResolvePendingHeroTransition()
    {
        if (_heroTransitionSession == null || !Element.IsActive)
        {
            return;
        }

        var session = _heroTransitionSession;
        var flights = _heroTransitionController.CreateFlights(session.FromRoute, session.ToRoute);
        if (flights.Count == 0)
        {
            SetState(() => CancelHeroTransition(disposeDetachedRoute: true));
            return;
        }

        SetState(() =>
        {
            if (_heroTransitionSession == null || !ReferenceEquals(_heroTransitionSession, session))
            {
                return;
            }

            _heroTransitionController.ActivateFlights(
                flights,
                isPushTransition: session.Direction == HeroTransitionDirection.Push);
            _heroFlightController.Forward(from: 0.0);
        });
    }

    private void HandleHeroFlightTick()
    {
        if (_heroTransitionSession == null || _heroTransitionController.ActiveFlights.Count == 0 || !Element.IsActive)
        {
            return;
        }

        SetState(() => { });
    }

    private void HandleHeroFlightCompleted()
    {
        if (_heroTransitionSession == null)
        {
            return;
        }

        if (!Element.IsActive)
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            return;
        }

        SetState(() => CancelHeroTransition(disposeDetachedRoute: true));
    }

    private void CancelHeroTransition(bool disposeDetachedRoute)
    {
        _heroFlightController.Stop();
        _heroTransitionController.ClearFlights();

        var session = _heroTransitionSession;
        _heroTransitionSession = null;

        if (!disposeDetachedRoute || session == null || !session.DetachFromRouteOnComplete)
        {
            return;
        }

        session.FromRoute.Dispose();
        session.FromRoute.Detach();
    }

    private void PushInitialRoute()
    {
        if (_history.Count > 0)
        {
            return;
        }

        var initialRoute = ResolveInitialRoute();
        InstallRoute(initialRoute);
        initialRoute.DidPush();
        NotifyObserversPush(initialRoute, previousRoute: null);
    }

    private void PopCurrentRoute(object? result)
    {
        if (_history.Count == 0)
        {
            return;
        }

        var route = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        var previousRoute = CurrentRoute;

        route.DidPop(previousRoute);
        previousRoute?.DidPopNext(route);
        previousRoute?.DidChangeNext(nextRoute: null);

        _ = result;
        NotifyObserversPop(route, previousRoute);

        var shouldAnimateHero = previousRoute != null && _heroTransitionController.HasHeroes(route);
        if (shouldAnimateHero)
        {
            TryStartHeroTransition(
                fromRoute: route,
                toRoute: previousRoute,
                direction: HeroTransitionDirection.Pop,
                detachFromRouteOnComplete: true);
            return;
        }

        CancelHeroTransition(disposeDetachedRoute: true);
        route.Dispose();
        route.Detach();
    }

    private void InstallRoute(Route route)
    {
        if (route.Navigator != null && !ReferenceEquals(route.Navigator, this))
        {
            throw new InvalidOperationException("Route is already attached to another Navigator.");
        }

        if (!ReferenceEquals(route.Navigator, this))
        {
            route.Attach(this);
        }

        if (_history.Count == 0 || !ReferenceEquals(_history[^1], route))
        {
            _history.Add(route);
        }
    }

    private void RemoveRoutesBelowTopUntil(RoutePredicate predicate)
    {
        while (_history.Count > 1)
        {
            var routeBelowTop = _history[^2];
            if (predicate(routeBelowTop))
            {
                break;
            }

            RemoveRouteAt(_history.Count - 2, result: null);
        }
    }

    private void RemoveRouteAt(int index, object? result)
    {
        if (index < 0 || index >= _history.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var wasTopRoute = index == _history.Count - 1;
        var previousRoute = index > 0 ? _history[index - 1] : null;
        var nextRoute = index + 1 < _history.Count ? _history[index + 1] : null;
        var removedRoute = _history[index];
        _history.RemoveAt(index);

        removedRoute.Dispose();
        removedRoute.Detach();

        previousRoute?.DidChangeNext(nextRoute);
        nextRoute?.DidChangePrevious(previousRoute);
        if (wasTopRoute)
        {
            previousRoute?.DidPopNext(removedRoute);
        }

        _ = result;
        NotifyObserversRemove(removedRoute, previousRoute);
    }

    private Route ResolveInitialRoute()
    {
        if (CurrentWidget.InitialRoute != null)
        {
            return CurrentWidget.InitialRoute;
        }

        if (CurrentWidget.InitialRouteData != null)
        {
            return ResolveRouteData(CurrentWidget.InitialRouteData);
        }

        var routeName = CurrentWidget.InitialRouteName;
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new InvalidOperationException("Navigator requires either InitialRoute or InitialRouteName.");
        }

        return ResolveRouteLocation(routeName, arguments: null);
    }

    private Route ResolveNamedRoute(string routeName, object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new ArgumentException("routeName cannot be null or whitespace.", nameof(routeName));
        }

        var routeFactory = CurrentWidget.OnGenerateRoute;
        if (routeFactory == null)
        {
            throw new InvalidOperationException(
                $"Navigator does not have onGenerateRoute. Cannot resolve route '{routeName}'.");
        }

        var settings = new RouteSettings(Name: routeName, Arguments: arguments);
        var route = routeFactory(settings);
        if (route == null)
        {
            throw new InvalidOperationException($"onGenerateRoute returned null for route '{routeName}'.");
        }

        return route;
    }

    private Route ResolveRouteData(RouteData routeData)
    {
        return ResolveNamedRoute(routeData.Name, routeData);
    }

    private Route ResolveRouteLocation(string routeName, object? arguments)
    {
        if (routeName.IndexOf('?') >= 0 || routeName.Contains("://", StringComparison.Ordinal))
        {
            return ResolveRouteData(RouteData.FromLocation(routeName, arguments));
        }

        return ResolveNamedRoute(routeName, arguments);
    }

    private void SyncObservers(
        IReadOnlyList<NavigatorObserver> oldObservers,
        IReadOnlyList<NavigatorObserver> newObservers)
    {
        foreach (var oldObserver in oldObservers)
        {
            if (newObservers.Contains(oldObserver))
            {
                continue;
            }

            _observers.Remove(oldObserver);
            if (ReferenceEquals(oldObserver.Navigator, this))
            {
                oldObserver.Navigator = null;
            }
        }

        foreach (var observer in newObservers)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            observer.Navigator = this;
        }
    }

    private void NotifyObserversPush(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidPush(route, previousRoute);
        }
    }

    private void NotifyObserversPop(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidPop(route, previousRoute);
        }
    }

    private void NotifyObserversRemove(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidRemove(route, previousRoute);
        }
    }

    private void NotifyObserversReplace(Route newRoute, Route? oldRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidReplace(newRoute, oldRoute);
        }
    }

    private void NotifyObserversStartUserGesture(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidStartUserGesture(route, previousRoute);
        }
    }

    private void NotifyObserversStopUserGesture()
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidStopUserGesture();
        }
    }

    private enum HeroTransitionDirection
    {
        Push,
        Pop,
    }

    private sealed class HeroTransitionSession
    {
        public HeroTransitionSession(
            Route fromRoute,
            Route toRoute,
            HeroTransitionDirection direction,
            bool detachFromRouteOnComplete)
        {
            FromRoute = fromRoute;
            ToRoute = toRoute;
            Direction = direction;
            DetachFromRouteOnComplete = detachFromRouteOnComplete;
        }

        public Route FromRoute { get; }

        public Route ToRoute { get; }

        public HeroTransitionDirection Direction { get; }

        public bool DetachFromRouteOnComplete { get; }
    }

    private bool HandleBackButton()
    {
        if (!Element.IsActive)
        {
            return false;
        }

        return MaybePop();
    }
}

internal sealed class NavigatorScope : InheritedWidget
{
    public NavigatorScope(
        NavigatorState navigator,
        Widget child,
        Key? key = null) : base(key)
    {
        Navigator = navigator;
        Child = child;
    }

    public NavigatorState Navigator { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((NavigatorScope)oldWidget).Navigator, Navigator);
    }
}

internal sealed class RouteScope : InheritedWidget
{
    public RouteScope(
        Route route,
        Widget child,
        Key? key = null) : base(key)
    {
        Route = route;
        Child = child;
    }

    public Route Route { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((RouteScope)oldWidget).Route, Route);
    }
}

internal sealed class ActiveRouteHost : StatelessWidget
{
    private readonly Route _route;
    private readonly HeroTransitionController _heroTransitionController;

    public ActiveRouteHost(
        Route route,
        HeroTransitionController heroTransitionController,
        Key? key = null) : base(key)
    {
        _route = route;
        _heroTransitionController = heroTransitionController;
    }

    public override Widget Build(BuildContext context)
    {
        return new RouteScope(
            route: _route,
            child: new HeroControllerScope(
                controller: _heroTransitionController,
                route: _route,
                child: new RoutePageHost(_route)));
    }
}

internal sealed class RoutePageHost : StatelessWidget
{
    private readonly Route _route;

    public RoutePageHost(Route route)
    {
        _route = route;
    }

    public override Widget Build(BuildContext context)
    {
        return _route.BuildPage(context);
    }
}

internal static class NavigatorBackButtonDispatcher
{
    private static readonly List<Func<bool>> Handlers = [];

    public static void AddHandler(Func<bool> handler)
    {
        RemoveHandler(handler);
        Handlers.Add(handler);
    }

    public static void RemoveHandler(Func<bool> handler)
    {
        Handlers.RemoveAll(existing => ReferenceEquals(existing, handler));
    }

    public static bool DispatchBackButton()
    {
        for (var index = Handlers.Count - 1; index >= 0; index -= 1)
        {
            if (Handlers[index]())
            {
                return true;
            }
        }

        return false;
    }

    internal static void ResetForTests()
    {
        Handlers.Clear();
    }
}
