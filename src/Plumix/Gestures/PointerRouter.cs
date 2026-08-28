using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/pointer_router.dart

namespace Plumix.Gestures;

/// <summary>A callback that receives a <see cref="PointerEvent"/>.</summary>
public delegate void PointerRoute(PointerEvent @event);

/// <summary>A routing table for <see cref="PointerEvent"/> events.</summary>
public sealed class PointerRouter
{
    // Dart keys these by the route in an insertion-ordered LinkedHashMap; the documented contract is
    // that routes run in the order they were added, so C# keeps ordered lists instead of a
    // Dictionary (whose order is unspecified once an entry is removed).
    private readonly Dictionary<int, List<RouteEntry>> _routeMap = [];
    private readonly List<RouteEntry> _globalRoutes = [];

    /// <summary>
    /// Adds a route to the routing table. Whenever this object routes a <see cref="PointerEvent"/>
    /// corresponding to <paramref name="pointer"/>, it calls <paramref name="route"/>.
    /// </summary>
    /// <remarks>
    /// Routes added reentrantly within <see cref="Route"/> take effect when routing the next event.
    /// </remarks>
    public void AddRoute(int pointer, PointerRoute route, Matrix4? transform = null)
    {
        if (!_routeMap.TryGetValue(pointer, out List<RouteEntry>? routes))
        {
            routes = [];
            _routeMap[pointer] = routes;
        }

        if (IndexOf(routes, route) >= 0)
        {
            throw new InvalidOperationException(
                $"A pointer route is already registered for pointer {pointer}.");
        }

        routes.Add(new RouteEntry(route, transform));
    }

    /// <summary>
    /// Removes a route from the routing table. Requires that the route was previously added.
    /// </summary>
    /// <remarks>Routes removed reentrantly within <see cref="Route"/> take effect immediately.</remarks>
    public void RemoveRoute(int pointer, PointerRoute route)
    {
        if (!_routeMap.TryGetValue(pointer, out List<RouteEntry>? routes))
        {
            throw new InvalidOperationException($"No pointer routes are registered for pointer {pointer}.");
        }

        int index = IndexOf(routes, route);
        if (index < 0)
        {
            throw new InvalidOperationException($"The pointer route is not registered for pointer {pointer}.");
        }

        routes.RemoveAt(index);
        if (routes.Count == 0)
        {
            _routeMap.Remove(pointer);
        }
    }

    /// <summary>
    /// Adds a route to the global entry in the routing table: it is called for every routed event.
    /// </summary>
    public void AddGlobalRoute(PointerRoute route, Matrix4? transform = null)
    {
        if (IndexOf(_globalRoutes, route) >= 0)
        {
            throw new InvalidOperationException("The pointer route is already registered as a global route.");
        }

        _globalRoutes.Add(new RouteEntry(route, transform));
    }

    /// <summary>Removes a route previously added via <see cref="AddGlobalRoute"/>.</summary>
    public void RemoveGlobalRoute(PointerRoute route)
    {
        int index = IndexOf(_globalRoutes, route);
        if (index < 0)
        {
            throw new InvalidOperationException("The pointer route is not registered as a global route.");
        }

        _globalRoutes.RemoveAt(index);
    }

    /// <summary>The number of global routes that have been registered.</summary>
    public int DebugGlobalRouteCount => _globalRoutes.Count;

    /// <summary>
    /// Calls the routes registered for this pointer event, in the order in which they were added.
    /// </summary>
    public void Route(PointerEvent @event)
    {
        _routeMap.TryGetValue(@event.Pointer, out List<RouteEntry>? routes);
        RouteEntry[] copiedGlobalRoutes = [.. _globalRoutes];
        if (routes is not null)
        {
            DispatchEventToRoutes(@event, routes, [.. routes]);
        }

        DispatchEventToRoutes(@event, _globalRoutes, copiedGlobalRoutes);
    }

    private void DispatchEventToRoutes(
        PointerEvent @event,
        List<RouteEntry> referenceRoutes,
        RouteEntry[] copiedRoutes)
    {
        foreach (RouteEntry entry in copiedRoutes)
        {
            if (IndexOf(referenceRoutes, entry.Route) >= 0)
            {
                Dispatch(@event, entry.Route, entry.Transform);
            }
        }
    }

    private void Dispatch(PointerEvent @event, PointerRoute route, Matrix4? transform)
    {
        try
        {
            @event = @event.Transformed(transform);
            route(@event);
        }
        catch (Exception exception)
        {
            PointerEvent reported = @event;
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                library: "gesture library",
                context: new ErrorDescription("while routing a pointer event"),
                informationCollector: () =>
                [
                    new DiagnosticsProperty<PointerRouter>("router", this, level: DiagnosticLevel.Debug),
                    new DiagnosticsProperty<PointerRoute>("route", route, level: DiagnosticLevel.Debug),
                    new DiagnosticsProperty<PointerEvent>("event", reported, level: DiagnosticLevel.Debug)
                ]));
        }
    }

    private static int IndexOf(List<RouteEntry> routes, PointerRoute route)
    {
        for (int i = 0; i < routes.Count; i++)
        {
            if (routes[i].Route.Equals(route))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Drops every per-pointer route, so a test that abandons a pointer sequence does not leak it
    /// into the next one. Global routes are owned by mounted widgets whose lifetime spans a reset
    /// (`RawTooltip`), so they are left in place: removing them here would make their disposal fail
    /// the "route was previously added" contract.
    /// </summary>
    internal void Reset()
    {
        _routeMap.Clear();
    }

    private readonly record struct RouteEntry(PointerRoute Route, Matrix4? Transform);
}
