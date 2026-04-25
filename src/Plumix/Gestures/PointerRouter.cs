using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/pointer_router.dart (approximate)

namespace Plumix.Gestures;

public delegate void PointerRoute(PointerEvent @event);

public sealed class PointerRouter
{
    private readonly Dictionary<int, List<PointerRoute>> _routeMap = [];
    private readonly List<PointerRoute> _globalRoutes = [];

    public void AddRoute(int pointer, PointerRoute route)
    {
        if (!_routeMap.TryGetValue(pointer, out var routes))
        {
            routes = [];
            _routeMap[pointer] = routes;
        }

        if (!routes.Contains(route))
        {
            routes.Add(route);
        }
    }

    public void RemoveRoute(int pointer, PointerRoute route)
    {
        if (!_routeMap.TryGetValue(pointer, out var routes))
        {
            return;
        }

        routes.Remove(route);
        if (routes.Count == 0)
        {
            _routeMap.Remove(pointer);
        }
    }

    public void AddGlobalRoute(PointerRoute route)
    {
        if (!_globalRoutes.Contains(route))
        {
            _globalRoutes.Add(route);
        }
    }

    public void RemoveGlobalRoute(PointerRoute route)
    {
        _globalRoutes.Remove(route);
    }

    public void Route(PointerEvent @event)
    {
        if (_routeMap.TryGetValue(@event.Pointer, out var routes) && routes.Count > 0)
        {
            foreach (var route in routes.ToArray())
            {
                route(@event);
            }
        }

        if (_globalRoutes.Count == 0)
        {
            return;
        }

        foreach (var route in _globalRoutes.ToArray())
        {
            route(@event);
        }
    }

    internal void Reset()
    {
        _routeMap.Clear();
        _globalRoutes.Clear();
    }
}
