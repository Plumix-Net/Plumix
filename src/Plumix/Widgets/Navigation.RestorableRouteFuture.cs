// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

using Plumix.Foundation;

namespace Plumix.Widgets;

/// <summary>
/// A callback that given a <see cref="BuildContext"/> finds a <see cref="NavigatorState"/>.
/// </summary>
/// <remarks>
/// Used by <see cref="RestorableRouteFuture{T}.NavigatorFinder"/> to determine the navigator to which a new
/// route should be added.
/// </remarks>
public delegate NavigatorState NavigatorFinderCallback(BuildContext context);

/// <summary>
/// A callback that given some <paramref name="arguments"/> and a <paramref name="navigator"/> adds a new
/// restorable route to that navigator and returns the opaque ID of that new route.
/// </summary>
/// <remarks>
/// Usually, this callback calls one of the imperative methods on the navigator that have "restorable" in the
/// name and returns their return value. Used by <see cref="RestorableRouteFuture{T}.OnPresent"/>.
/// </remarks>
public delegate string RoutePresentationCallback(NavigatorState navigator, object? arguments);

/// <summary>
/// A callback to handle the result of a completed <see cref="Route"/>.
/// </summary>
/// <remarks>
/// The return value of the route (which can be null for e.g. void routes) is passed to the callback. Used by
/// <see cref="RestorableRouteFuture{T}.OnComplete"/>.
/// </remarks>
public delegate void RouteCompletionCallback<in T>(T result);

/// <summary>
/// Gives access to a <see cref="Route"/> object and its return value that was added to a navigator via one of
/// its "restorable" API methods.
/// </summary>
/// <remarks>
/// <para>
/// When a <see cref="State"/> object wants access to the return value of a <see cref="Route"/> object it has
/// pushed onto the <see cref="Navigator"/>, a <see cref="RestorableRouteFuture{T}"/> ensures that it will also
/// have access to that value after state restoration.
/// </para>
/// <para>
/// To show a new route on the navigator defined by <see cref="NavigatorFinder"/>, call <see cref="Present"/>,
/// which invokes <see cref="OnPresent"/>. That callback must add a new route to the navigator provided to it
/// using one of the "restorable" API methods. When the newly added route completes, <see cref="OnComplete"/>
/// runs with the return value of the route, which may be null.
/// </para>
/// <para>
/// If the property is restored to a state in which <see cref="Present"/> had been called on it, but the route
/// has not completed yet, the property obtains the restored route object from the navigator again and calls
/// <see cref="OnComplete"/> once it completes.
/// </para>
/// <para>
/// Dart's <c>route</c> getter is typed <c>Route&lt;T&gt;?</c>; Plumix's <see cref="Widgets.Route"/> is not
/// generic, so <see cref="Route"/> is a plain <see cref="Widgets.Route"/> and the route's result is cast to
/// <typeparamref name="T"/> when it is handed to <see cref="OnComplete"/>, as Dart's <c>result as T</c> does.
/// </para>
/// </remarks>
public class RestorableRouteFuture<T> : RestorableProperty<string?>
{
    private readonly Action _notifyListeners;
    private Route? _route;
    private bool _disposed;

    /// <summary>Creates a <see cref="RestorableRouteFuture{T}"/>.</summary>
    public RestorableRouteFuture(
        RoutePresentationCallback onPresent,
        NavigatorFinderCallback? navigatorFinder = null,
        RouteCompletionCallback<T>? onComplete = null)
    {
        OnPresent = onPresent ?? throw new ArgumentNullException(nameof(onPresent));
        NavigatorFinder = navigatorFinder ?? DefaultNavigatorFinder;
        OnComplete = onComplete;
        _notifyListeners = NotifyListeners;
    }

    /// <summary>
    /// A callback that given the <see cref="BuildContext"/> of the <see cref="State"/> object to which this
    /// property is registered returns the <see cref="NavigatorState"/> of the navigator to which the route
    /// instantiated in <see cref="OnPresent"/> is added.
    /// </summary>
    public NavigatorFinderCallback NavigatorFinder { get; }

    /// <summary>
    /// A callback that adds a new <see cref="Widgets.Route"/> to the provided navigator.
    /// </summary>
    /// <remarks>
    /// The callback must use one of the API methods on the <see cref="NavigatorState"/> that have "restorable"
    /// in their name (e.g. <see cref="NavigatorState.RestorablePush"/>,
    /// <see cref="NavigatorState.RestorablePushNamed"/>) and return the opaque ID returned by those methods.
    /// </remarks>
    public RoutePresentationCallback OnPresent { get; }

    /// <summary>
    /// A callback that is invoked when the <see cref="Widgets.Route"/> added via <see cref="OnPresent"/>
    /// completes. The return value of that route is passed to this method.
    /// </summary>
    public RouteCompletionCallback<T>? OnComplete { get; }

    /// <summary>
    /// Whether the <see cref="Widgets.Route"/> created by <see cref="Present"/> is currently shown.
    /// </summary>
    /// <remarks>Returns true after <see cref="Present"/> has been called until the route completes.</remarks>
    public bool IsPresent => Route is not null;

    /// <summary>The route that <see cref="Present"/> added to the navigator; null when no route is shown.</summary>
    public Route? Route => _route;

    /// <inheritdoc/>
    public override bool Enabled => Route?.RestorationScopeId.Value is not null;

    private NavigatorState Navigator
    {
        get
        {
            NavigatorState navigator = NavigatorFinder(State.Context);
            return navigator;
        }
    }

    /// <summary>
    /// Shows the route created by <see cref="OnPresent"/> and invokes <see cref="OnComplete"/> when it
    /// completes.
    /// </summary>
    /// <param name="arguments">
    /// Passed to <see cref="OnPresent"/> to customize the route. It must be serializable via the
    /// <see cref="UI.StandardMessageCodec"/>. Often, a dictionary is used to pass key-value pairs.
    /// </param>
    public void Present(object? arguments = null)
    {
        if (Constants.KDebugMode && IsPresent)
        {
            throw new AssertionError("'!isPresent': is not true.");
        }

        if (Constants.KDebugMode && !IsRegistered)
        {
            throw new AssertionError("'isRegistered': is not true.");
        }

        string routeId = OnPresent(Navigator, arguments);
        HookOntoRouteFuture(routeId);
        NotifyListeners();
    }

    /// <inheritdoc/>
    public override string? CreateDefaultValue() => null;

    /// <inheritdoc/>
    public override void InitWithValue(string? value)
    {
        if (value is not null)
        {
            HookOntoRouteFuture(value);
        }
    }

    /// <inheritdoc/>
    public override object? ToPrimitives()
    {
        if (Constants.KDebugMode && Route is null)
        {
            throw new AssertionError("'route != null': is not true.");
        }

        if (Constants.KDebugMode && !Enabled)
        {
            throw new AssertionError("'enabled': is not true.");
        }

        return Route?.RestorationScopeId.Value;
    }

    /// <inheritdoc/>
    public override string? FromPrimitives(object? data)
    {
        if (Constants.KDebugMode && data is null)
        {
            throw new AssertionError("'data != null': is not true.");
        }

        return (string)data!;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();
        _route?.RestorationScopeId.RemoveListener(_notifyListeners);
        _disposed = true;
    }

    private void HookOntoRouteFuture(string id)
    {
        _route = Navigator.GetRouteById(id);
        if (Constants.KDebugMode && _route is null)
        {
            throw new AssertionError("'_route != null': is not true.");
        }

        Route route = Route!;
        route.RestorationScopeId.AddListener(_notifyListeners);
        // Dart's `route!.popped.then(...)`: the continuation runs as a microtask once the route pops.
        Scheduler.RunAsync(async () =>
        {
            object? result = await route.Popped;
            if (_disposed)
            {
                return;
            }

            _route?.RestorationScopeId.RemoveListener(_notifyListeners);
            _route = null;
            NotifyListeners();
            OnComplete?.Invoke((T)result!);
        });
    }

    private static NavigatorState DefaultNavigatorFinder(BuildContext context) => Widgets.Navigator.Of(context);
}
