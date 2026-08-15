using System.Diagnostics;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/router.dart

/// <summary>
/// Information for the <see cref="Router{T}"/> to parse into a delegate configuration.
/// </summary>
/// <remarks>
/// Dart's deprecated <c>location</c> constructor parameter and getter are not ported; the current
/// <c>uri</c> API is the only surface.
/// </remarks>
public sealed class RouteInformation
{
    public RouteInformation(Uri uri, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        Uri = uri;
        State = state;
    }

    /// <summary>The uri location of the application.</summary>
    public Uri Uri { get; }

    /// <summary>The state of the application in the <see cref="Uri"/>.</summary>
    public object? State { get; }
}

/// <summary>A convenient bundle to configure a <see cref="Router{T}"/> widget.</summary>
public class RouterConfig<T>
{
    public RouterConfig(
        RouterDelegate<T> routerDelegate,
        RouteInformationProvider? routeInformationProvider = null,
        RouteInformationParser<T>? routeInformationParser = null,
        BackButtonDispatcher? backButtonDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(routerDelegate);
        if (routeInformationProvider is null != (routeInformationParser is null))
        {
            throw new ArgumentException(
                "The routeInformationProvider and routeInformationParser must both be provided or both be null.");
        }

        RouterDelegate = routerDelegate;
        RouteInformationProvider = routeInformationProvider;
        RouteInformationParser = routeInformationParser;
        BackButtonDispatcher = backButtonDispatcher;
    }

    public RouteInformationProvider? RouteInformationProvider { get; }

    public RouteInformationParser<T>? RouteInformationParser { get; }

    public RouterDelegate<T> RouterDelegate { get; }

    public BackButtonDispatcher? BackButtonDispatcher { get; }
}

/// <summary>
/// The <see cref="Router{T}"/>'s intention when it reports a new <see cref="RouteInformation"/> to the
/// <see cref="RouteInformationProvider"/>.
/// </summary>
public enum RouteInformationReportingType
{
    /// <summary>Router does not have a specific intention.</summary>
    None,

    /// <summary>The accompanying route information was generated during a <c>Router.Neglect</c> call.</summary>
    Neglect,

    /// <summary>The accompanying route information was generated during a <c>Router.Navigate</c> call.</summary>
    Navigate,
}

/// <summary>
/// The static half of Flutter's <c>Router</c>. Dart declares these as statics on the generic class;
/// C# statics on a generic type would force callers to spell the configuration type twice, so they
/// live on the non-generic companion instead.
/// </summary>
public static class Router
{
    /// <summary>Dart's <c>Router.withConfig</c> factory constructor.</summary>
    public static Router<T> WithConfig<T>(RouterConfig<T> config, string? restorationScopeId = null, Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new Router<T>(
            routerDelegate: config.RouterDelegate,
            routeInformationProvider: config.RouteInformationProvider,
            routeInformationParser: config.RouteInformationParser,
            backButtonDispatcher: config.BackButtonDispatcher,
            restorationScopeId: restorationScopeId,
            key: key);
    }

    /// <summary>Retrieves the immediate <see cref="Router{T}"/> ancestor, throwing when there is none.</summary>
    public static Router<T> Of<T>(BuildContext context)
    {
        return (Router<T>)RequireScope(context).RouterState.RouterWidget;
    }

    /// <summary>Retrieves the immediate <see cref="Router{T}"/> ancestor, or null when there is none.</summary>
    public static Router<T>? MaybeOf<T>(BuildContext context)
    {
        var scope = context.DependOnInherited<RouterScope>();
        return scope?.RouterState.RouterWidget as Router<T>;
    }

    /// <summary>
    /// The back-button dispatcher of the immediate ancestor router. Dart reads it through
    /// <c>Router.of&lt;Object?&gt;(context)</c>; C# generics are invariant, so a router configured with a
    /// concrete type cannot be widened to <c>Router&lt;object&gt;</c> and the dispatcher gets its own accessor.
    /// </summary>
    public static BackButtonDispatcher? BackButtonDispatcherOf(BuildContext context)
    {
        return RequireScope(context).BackButtonDispatcher;
    }

    internal static RouterScope RequireScope(BuildContext context)
    {
        var scope = context.DependOnInherited<RouterScope>();
        if (scope is null)
        {
            throw new InvalidOperationException(
                "Router operation requested with a context that does not include a Router.\n"
                + "The context used to retrieve the Router must be that of a widget that "
                + "is a descendant of a Router widget.");
        }

        return scope;
    }

    /// <summary>Forces the router to run <paramref name="callback"/> and create a new history entry.</summary>
    public static void Navigate(BuildContext context, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ScopeOf(context).RouterState.SetStateWithExplicitReportStatus(
            RouteInformationReportingType.Navigate,
            callback);
    }

    /// <summary>Forces the router to run <paramref name="callback"/> without a new history entry.</summary>
    public static void Neglect(BuildContext context, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ScopeOf(context).RouterState.SetStateWithExplicitReportStatus(
            RouteInformationReportingType.Neglect,
            callback);
    }

    private static RouterScope ScopeOf(BuildContext context)
    {
        InheritedElement? element = context.GetElementForInheritedWidgetOfExactType<RouterScope>();
        if (element is null)
        {
            throw new InvalidOperationException(
                "Router operation requested with a context that does not include a Router.");
        }

        return (RouterScope)element.Widget;
    }
}

/// <summary>The dispatcher for opening and closing pages of an application.</summary>
public sealed class Router<T> : StatefulWidget
{
    public Router(
        RouterDelegate<T> routerDelegate,
        RouteInformationProvider? routeInformationProvider = null,
        RouteInformationParser<T>? routeInformationParser = null,
        BackButtonDispatcher? backButtonDispatcher = null,
        string? restorationScopeId = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(routerDelegate);
        if (routeInformationProvider is not null && routeInformationParser is null)
        {
            throw new ArgumentException(
                "A routeInformationParser must be provided when a routeInformationProvider is specified.",
                nameof(routeInformationParser));
        }

        RouterDelegate = routerDelegate;
        RouteInformationProvider = routeInformationProvider;
        RouteInformationParser = routeInformationParser;
        BackButtonDispatcher = backButtonDispatcher;
        RestorationScopeId = restorationScopeId;
    }

    public RouteInformationProvider? RouteInformationProvider { get; }

    public RouteInformationParser<T>? RouteInformationParser { get; }

    public RouterDelegate<T> RouterDelegate { get; }

    public BackButtonDispatcher? BackButtonDispatcher { get; }

    public string? RestorationScopeId { get; }

    public override State CreateState() => new RouterState<T>();
}

/// <summary>Non-generic half of Flutter's <c>_RouterState</c>, so <see cref="RouterScope"/> can hold it.</summary>
internal abstract class RouterStateBase : RestorationState
{
    internal StatefulWidget RouterWidget => StateWidget;

    internal abstract void SetStateWithExplicitReportStatus(RouteInformationReportingType status, Action callback);
}

internal sealed class RouterState<T> : RouterStateBase
{
    private readonly RestorableRouteInformation _routeInformation = new();
    private object? _currentRouterTransaction;
    private RouteInformationReportingType? _currentIntentionToReport;
    private bool _routeParsePending;
    private bool _routeInformationReportingTaskScheduled;

    private Router<T> CurrentWidget => (Router<T>)StateWidget;

    protected override string? RestorationId => CurrentWidget.RestorationScopeId;

    public override void InitState()
    {
        base.InitState();
        CurrentWidget.RouteInformationProvider?.AddListener(HandleRouteInformationProviderNotification);
        CurrentWidget.BackButtonDispatcher?.AddCallback(HandleBackButtonDispatcherNotification);
        CurrentWidget.RouterDelegate.AddListener(HandleRouterDelegateNotification);
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(_routeInformation, "route");
        if (_routeInformation.Value is not null)
        {
            if (CurrentWidget.RouteInformationParser is null)
            {
                throw new InvalidOperationException(
                    "A routeInformationParser must be provided to restore a Router from its restoration data.");
            }

            ProcessRouteInformation(
                _routeInformation.Value,
                () => CurrentWidget.RouterDelegate.SetRestoredRoutePath);
        }
        else if (CurrentWidget.RouteInformationProvider is not null)
        {
            ProcessRouteInformation(
                CurrentWidget.RouteInformationProvider.Value,
                () => CurrentWidget.RouterDelegate.SetInitialRoutePath);
        }
    }

    public override void DidChangeDependencies()
    {
        _routeParsePending = true;
        base.DidChangeDependencies();

        // The base call may have parsed the route information already; that happens on the first build
        // and whenever state restoration kicks in.
        RouteInformation? currentRouteInformation =
            _routeInformation.Value ?? CurrentWidget.RouteInformationProvider?.Value;
        if (currentRouteInformation is not null && _routeParsePending)
        {
            ProcessRouteInformation(
                currentRouteInformation,
                () => CurrentWidget.RouterDelegate.SetNewRoutePath);
        }

        _routeParsePending = false;
        MaybeNeedToReportRouteInformation();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (Router<T>)oldWidget;
        if (!ReferenceEquals(CurrentWidget.RouteInformationProvider, previous.RouteInformationProvider)
            || !ReferenceEquals(CurrentWidget.BackButtonDispatcher, previous.BackButtonDispatcher)
            || !ReferenceEquals(CurrentWidget.RouteInformationParser, previous.RouteInformationParser)
            || !ReferenceEquals(CurrentWidget.RouterDelegate, previous.RouterDelegate))
        {
            _currentRouterTransaction = new object();
        }

        if (!ReferenceEquals(CurrentWidget.RouteInformationProvider, previous.RouteInformationProvider))
        {
            previous.RouteInformationProvider?.RemoveListener(HandleRouteInformationProviderNotification);
            CurrentWidget.RouteInformationProvider?.AddListener(HandleRouteInformationProviderNotification);
            if (!ReferenceEquals(
                    previous.RouteInformationProvider?.Value,
                    CurrentWidget.RouteInformationProvider?.Value))
            {
                HandleRouteInformationProviderNotification();
            }
        }

        if (!ReferenceEquals(CurrentWidget.BackButtonDispatcher, previous.BackButtonDispatcher))
        {
            previous.BackButtonDispatcher?.RemoveCallback(HandleBackButtonDispatcherNotification);
            CurrentWidget.BackButtonDispatcher?.AddCallback(HandleBackButtonDispatcherNotification);
        }

        if (!ReferenceEquals(CurrentWidget.RouterDelegate, previous.RouterDelegate))
        {
            previous.RouterDelegate.RemoveListener(HandleRouterDelegateNotification);
            CurrentWidget.RouterDelegate.AddListener(HandleRouterDelegateNotification);
            MaybeNeedToReportRouteInformation();
        }
    }

    public override void Dispose()
    {
        _routeInformation.Dispose();
        CurrentWidget.RouteInformationProvider?.RemoveListener(HandleRouteInformationProviderNotification);
        CurrentWidget.BackButtonDispatcher?.RemoveCallback(HandleBackButtonDispatcherNotification);
        CurrentWidget.RouterDelegate.RemoveListener(HandleRouterDelegateNotification);
        _currentRouterTransaction = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new UnmanagedRestorationScope(
            bucket: Bucket,
            child: new RouterScope(
                routeInformationProvider: CurrentWidget.RouteInformationProvider,
                backButtonDispatcher: CurrentWidget.BackButtonDispatcher,
                routeInformationParser: CurrentWidget.RouteInformationParser,
                routerDelegate: CurrentWidget.RouterDelegate,
                routerState: this,
                // The Builder gives RouterDelegate.Build a context below the scope, and keeps inherited
                // lookups made by the delegate from rebuilding the Router itself (which would re-parse).
                child: new Builder(builder: CurrentWidget.RouterDelegate.Build)));
    }

    internal override void SetStateWithExplicitReportStatus(RouteInformationReportingType status, Action callback)
    {
        Debug.Assert(status >= RouteInformationReportingType.Neglect, "Only Neglect and Navigate are explicit.");
        if (_currentIntentionToReport is not null
            && _currentIntentionToReport != RouteInformationReportingType.None
            && _currentIntentionToReport != status)
        {
            Debug.WriteLine(
                "Both Router.Navigate and Router.Neglect have been called in this build cycle, and the "
                + "Router cannot decide whether to report the route information. Please make sure only "
                + "one of them is called within the same build cycle.");
        }

        _currentIntentionToReport = status;
        ScheduleRouteInformationReportingTask();
        callback();
    }

    private void ScheduleRouteInformationReportingTask()
    {
        if (_routeInformationReportingTaskScheduled || CurrentWidget.RouteInformationProvider is null)
        {
            return;
        }

        _routeInformationReportingTaskScheduled = true;

        // Flutter only queues the callback and relies on the surrounding frame; Plumix schedules the frame
        // too, so the very first report still lands when nothing else dirties the tree.
        Scheduler.AddPostFrameCallback(ReportRouteInformation);
    }

    private void ReportRouteInformation(TimeSpan timestamp)
    {
        if (!Mounted)
        {
            return;
        }

        _routeInformationReportingTaskScheduled = false;
        if (_routeInformation.Value is not null)
        {
            CurrentWidget.RouteInformationProvider!.RouterReportsNewRouteInformation(
                _routeInformation.Value,
                _currentIntentionToReport!.Value);
        }

        _currentIntentionToReport = RouteInformationReportingType.None;
    }

    private RouteInformation? RetrieveNewRouteInformation()
    {
        T? configuration = CurrentWidget.RouterDelegate.CurrentConfiguration;
        if (configuration is null)
        {
            return null;
        }

        return CurrentWidget.RouteInformationParser?.RestoreRouteInformation(configuration!);
    }

    private void MaybeNeedToReportRouteInformation()
    {
        _routeInformation.Value = RetrieveNewRouteInformation();
        _currentIntentionToReport ??= RouteInformationReportingType.None;
        ScheduleRouteInformationReportingTask();
    }

    private void ProcessRouteInformation(RouteInformation information, Func<Func<T, Task>> delegateRouteSetter)
    {
        _routeParsePending = false;
        _currentRouterTransaction = new object();
        object? transaction = _currentRouterTransaction;
        Task<T> parsed = CurrentWidget.RouteInformationParser!
            .ParseRouteInformationWithDependencies(information, Context);
        if (parsed.IsCompletedSuccessfully)
        {
            ProcessParsedRouteInformation(transaction, delegateRouteSetter, parsed.Result);
            return;
        }

        _ = AwaitParsedRouteInformationAsync(parsed, transaction, delegateRouteSetter);
    }

    private async Task AwaitParsedRouteInformationAsync(
        Task<T> parsed,
        object? transaction,
        Func<Func<T, Task>> delegateRouteSetter)
    {
        T data = await parsed;
        ProcessParsedRouteInformation(transaction, delegateRouteSetter, data);
    }

    private void ProcessParsedRouteInformation(
        object? transaction,
        Func<Func<T, Task>> delegateRouteSetter,
        T data)
    {
        if (!ReferenceEquals(_currentRouterTransaction, transaction))
        {
            return;
        }

        Task applied = delegateRouteSetter()(data);
        if (applied.IsCompletedSuccessfully)
        {
            // Dart awaits the setter even when it returns a SynchronousFuture, so the rebuild is always
            // at least one microtask away; keeping that keeps SetState out of the ongoing build.
            Scheduler.ScheduleMicrotask(() => RebuildAfterRouteSet(transaction));
            return;
        }

        _ = AwaitRouteSetAsync(applied, transaction);
    }

    private async Task AwaitRouteSetAsync(Task applied, object? transaction)
    {
        await applied;
        RebuildAfterRouteSet(transaction);
    }

    private void RebuildAfterRouteSet(object? transaction)
    {
        if (!Mounted || !ReferenceEquals(_currentRouterTransaction, transaction))
        {
            return;
        }

        Rebuild();
    }

    private void HandleRouteInformationProviderNotification()
    {
        _routeParsePending = true;
        ProcessRouteInformation(
            CurrentWidget.RouteInformationProvider!.Value,
            () => CurrentWidget.RouterDelegate.SetNewRoutePath);
    }

    private Task<bool> HandleBackButtonDispatcherNotification()
    {
        _currentRouterTransaction = new object();
        object? transaction = _currentRouterTransaction;
        Task<bool> popped = CurrentWidget.RouterDelegate.PopRoute();
        if (popped.IsCompletedSuccessfully)
        {
            return Task.FromResult(HandleRoutePopped(transaction, popped.Result));
        }

        return AwaitRoutePoppedAsync(popped, transaction);
    }

    private async Task<bool> AwaitRoutePoppedAsync(Task<bool> popped, object? transaction)
    {
        bool data = await popped;
        return HandleRoutePopped(transaction, data);
    }

    private bool HandleRoutePopped(object? transaction, bool data)
    {
        if (!ReferenceEquals(transaction, _currentRouterTransaction))
        {
            // A rebuild was triggered from a different source. Return true to prevent bubbling.
            return true;
        }

        Rebuild();
        return data;
    }

    private void Rebuild()
    {
        SetState(() =>
        {
            // The router delegate is ready to rebuild.
        });
        MaybeNeedToReportRouteInformation();
    }

    private void HandleRouterDelegateNotification()
    {
        SetState(() =>
        {
            // The router delegate wants to rebuild.
        });
        MaybeNeedToReportRouteInformation();
    }
}

internal sealed class RouterScope : InheritedWidget
{
    public RouterScope(
        RouteInformationProvider? routeInformationProvider,
        BackButtonDispatcher? backButtonDispatcher,
        object? routeInformationParser,
        object routerDelegate,
        RouterStateBase routerState,
        Widget child,
        Key? key = null) : base(key)
    {
        RouteInformationProvider = routeInformationProvider;
        BackButtonDispatcher = backButtonDispatcher;
        RouteInformationParser = routeInformationParser;
        RouterDelegate = routerDelegate;
        RouterState = routerState;
        Child = child;
    }

    public RouteInformationProvider? RouteInformationProvider { get; }

    public BackButtonDispatcher? BackButtonDispatcher { get; }

    public object? RouteInformationParser { get; }

    public object RouterDelegate { get; }

    public RouterStateBase RouterState { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var previous = (RouterScope)oldWidget;
        return !ReferenceEquals(RouteInformationProvider, previous.RouteInformationProvider)
               || !ReferenceEquals(BackButtonDispatcher, previous.BackButtonDispatcher)
               || !ReferenceEquals(RouteInformationParser, previous.RouteInformationParser)
               || !ReferenceEquals(RouterDelegate, previous.RouterDelegate)
               || !ReferenceEquals(RouterState, previous.RouterState);
    }
}

/// <summary>
/// A class that invokes a single callback which then returns a value.
/// </summary>
/// <remarks>
/// Dart declares this as the private <c>_CallbackHookProvider</c>. C# cannot expose a public type
/// deriving from an internal one, so the class is public with an internal constructor.
/// </remarks>
public abstract class CallbackHookProvider<T>
{
    private readonly List<Func<T>> _callbacks = [];

    internal CallbackHookProvider()
    {
    }

    /// <summary>Whether a callback is currently registered.</summary>
    protected virtual bool HasCallbacks => _callbacks.Count > 0;

    /// <summary>Registers the callback to be called when the object changes.</summary>
    public virtual void AddCallback(Func<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
    }

    /// <summary>Removes a previously registered callback; ignored when it is not registered.</summary>
    public virtual void RemoveCallback(Func<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _ = _callbacks.Remove(callback);
    }

    /// <summary>
    /// Calls the single registered callback and returns its result. Returns <paramref name="defaultValue"/>
    /// when no callback is registered, when more than one is, or when the callback throws.
    /// </summary>
    public virtual T InvokeCallback(T defaultValue)
    {
        if (_callbacks.Count == 0)
        {
            return defaultValue;
        }

        try
        {
            if (_callbacks.Count > 1)
            {
                throw new InvalidOperationException(
                    $"More than one callback is registered on {GetType().Name}.");
            }

            return _callbacks[0]();
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Exception while invoking the callback for {GetType().Name}: {exception}");
            return defaultValue;
        }
    }
}

/// <summary>Reports back-button presses to a <see cref="Router{T}"/>.</summary>
public abstract class BackButtonDispatcher : CallbackHookProvider<Task<bool>>
{
    private readonly List<ChildBackButtonDispatcher> _children = [];

    protected override bool HasCallbacks => base.HasCallbacks || _children.Count > 0;

    /// <summary>
    /// Handles a pop route request, giving deferred children priority in reverse registration order and
    /// falling back to this dispatcher's own callback.
    /// </summary>
    public override Task<bool> InvokeCallback(Task<bool> defaultValue)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);
        if (_children.Count == 0)
        {
            return base.InvokeCallback(defaultValue);
        }

        ChildBackButtonDispatcher[] children = [.. _children];
        return NotifyChildren(children, children.Length - 1, defaultValue);
    }

    /// <summary>Creates a <see cref="ChildBackButtonDispatcher"/> that is a direct descendant of this one.</summary>
    public ChildBackButtonDispatcher CreateChildBackButtonDispatcher()
    {
        return new ChildBackButtonDispatcher(this);
    }

    /// <summary>Makes this dispatcher take priority among its peers.</summary>
    public virtual void TakePriority() => _children.Clear();

    /// <summary>Marks the given child as taking priority over this object and the other children.</summary>
    public virtual void DeferTo(ChildBackButtonDispatcher child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Debug.Assert(HasCallbacks, "A BackButtonDispatcher must have a callback before it can defer.");
        _ = _children.Remove(child);
        _children.Add(child);
    }

    /// <summary>Removes the given child from the list of children this object might defer to.</summary>
    public void Forget(ChildBackButtonDispatcher child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _ = _children.Remove(child);
    }

    private Task<bool> NotifyChildren(
        ChildBackButtonDispatcher[] children,
        int childIndex,
        Task<bool> defaultValue)
    {
        Task<bool> handled = children[childIndex].NotifiedByParent(defaultValue);
        if (handled.IsCompletedSuccessfully)
        {
            return NotifyNextChild(children, childIndex, defaultValue, handled.Result);
        }

        return AwaitChildAsync(children, childIndex, defaultValue, handled);
    }

    private async Task<bool> AwaitChildAsync(
        ChildBackButtonDispatcher[] children,
        int childIndex,
        Task<bool> defaultValue,
        Task<bool> handled)
    {
        bool result = await handled;
        return await NotifyNextChild(children, childIndex, defaultValue, result);
    }

    private Task<bool> NotifyNextChild(
        ChildBackButtonDispatcher[] children,
        int childIndex,
        Task<bool> defaultValue,
        bool result)
    {
        if (result)
        {
            return Task.FromResult(true);
        }

        if (childIndex > 0)
        {
            return NotifyChildren(children, childIndex - 1, defaultValue);
        }

        return base.InvokeCallback(defaultValue);
    }
}

/// <summary>The default back-button dispatcher for the root router.</summary>
public class RootBackButtonDispatcher : BackButtonDispatcher, WidgetsBindingObserver
{
    public override void AddCallback(Func<Task<bool>> callback)
    {
        if (!HasCallbacks)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }

        base.AddCallback(callback);
    }

    public override void RemoveCallback(Func<Task<bool>> callback)
    {
        base.RemoveCallback(callback);
        if (!HasCallbacks)
        {
            _ = WidgetsBinding.Instance.RemoveObserver(this);
        }
    }

    public Task<bool> DidPopRoute() => InvokeCallback(Task.FromResult(false));
}

/// <summary>
/// A <see cref="BackButtonDispatcher"/> that listens to a parent dispatcher and can take priority from it.
/// </summary>
public class ChildBackButtonDispatcher : BackButtonDispatcher
{
    public ChildBackButtonDispatcher(BackButtonDispatcher parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        Parent = parent;
    }

    /// <summary>The dispatcher this object attempts to take priority over.</summary>
    public BackButtonDispatcher Parent { get; }

    /// <summary>Called by the parent when it lets this child handle the pop request.</summary>
    protected internal Task<bool> NotifiedByParent(Task<bool> defaultValue) => InvokeCallback(defaultValue);

    public override void TakePriority()
    {
        Parent.DeferTo(this);
        base.TakePriority();
    }

    public override void DeferTo(ChildBackButtonDispatcher child)
    {
        Debug.Assert(HasCallbacks, "A ChildBackButtonDispatcher must have a callback before it can defer.");
        Parent.DeferTo(this);
        base.DeferTo(child);
    }

    public override void RemoveCallback(Func<Task<bool>> callback)
    {
        base.RemoveCallback(callback);
        if (!HasCallbacks)
        {
            Parent.Forget(this);
        }
    }
}

/// <summary>Registers a callback for when the back button is pressed.</summary>
public sealed class BackButtonListener : StatefulWidget
{
    public BackButtonListener(Widget child, Func<Task<bool>> onBackButtonPressed, Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(onBackButtonPressed);
        Child = child;
        OnBackButtonPressed = onBackButtonPressed;
    }

    public Widget Child { get; }

    public Func<Task<bool>> OnBackButtonPressed { get; }

    public override State CreateState() => new BackButtonListenerState();

    private sealed class BackButtonListenerState : State
    {
        private BackButtonDispatcher? _dispatcher;

        private BackButtonListener CurrentWidget => (BackButtonListener)StateWidget;

        public override void DidChangeDependencies()
        {
            _dispatcher?.RemoveCallback(CurrentWidget.OnBackButtonPressed);

            BackButtonDispatcher rootBackDispatcher = Router.BackButtonDispatcherOf(Context)
                                                      ?? throw new InvalidOperationException(
                                                          "The parent router must have a backButtonDispatcher "
                                                          + "to use this widget.");
            _dispatcher = rootBackDispatcher.CreateChildBackButtonDispatcher();
            _dispatcher.AddCallback(CurrentWidget.OnBackButtonPressed);
            _dispatcher.TakePriority();
            base.DidChangeDependencies();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var previous = (BackButtonListener)oldWidget;
            if (!ReferenceEquals(previous.OnBackButtonPressed, CurrentWidget.OnBackButtonPressed))
            {
                _dispatcher?.RemoveCallback(previous.OnBackButtonPressed);
                _dispatcher?.AddCallback(CurrentWidget.OnBackButtonPressed);
                _dispatcher?.TakePriority();
            }
        }

        public override void Dispose()
        {
            _dispatcher?.RemoveCallback(CurrentWidget.OnBackButtonPressed);
            base.Dispose();
        }

        public override Widget Build(BuildContext context) => CurrentWidget.Child;
    }
}

/// <summary>Parses route information into configurations for a <see cref="RouterDelegate{T}"/>.</summary>
public abstract class RouteInformationParser<T>
{
    /// <summary>Converts the given route information into parsed data for the router delegate.</summary>
    public virtual Task<T> ParseRouteInformation(RouteInformation routeInformation)
    {
        throw new NotImplementedException(
            "One of ParseRouteInformation or ParseRouteInformationWithDependencies must be implemented.");
    }

    /// <summary>
    /// Converts the given route information into parsed data, with a context that may be used to look
    /// inherited widgets up. A dependency registered here re-parses when it changes.
    /// </summary>
    public virtual Task<T> ParseRouteInformationWithDependencies(
        RouteInformation routeInformation,
        BuildContext context)
    {
        return ParseRouteInformation(routeInformation);
    }

    /// <summary>Restores the route information from the given configuration, or null to skip reporting.</summary>
    public virtual RouteInformation? RestoreRouteInformation(T configuration) => null;
}

/// <summary>
/// The delegate that configures a widget subtree from a routing configuration.
/// </summary>
/// <remarks>
/// Dart declares <c>RouterDelegate</c> as <c>extends Listenable</c> and expects implementations to mix
/// <c>ChangeNotifier</c> in. C# has no mixins, so the notifier is folded into the base class.
/// </remarks>
public abstract class RouterDelegate<T> : ChangeNotifier
{
    /// <summary>Called by the router at startup with the initial route information.</summary>
    public virtual Task SetInitialRoutePath(T configuration) => SetNewRoutePath(configuration);

    /// <summary>Called by the router when state restoration restores a configuration.</summary>
    public virtual Task SetRestoredRoutePath(T configuration) => SetNewRoutePath(configuration);

    /// <summary>Called by the router when a new route has been parsed.</summary>
    public abstract Task SetNewRoutePath(T configuration);

    /// <summary>Called when the back button asks the delegate to pop the current route.</summary>
    public abstract Task<bool> PopRoute();

    /// <summary>The configuration the router reports to the route information provider.</summary>
    public virtual T? CurrentConfiguration => default;

    /// <summary>Builds the widget subtree for the current configuration.</summary>
    public abstract Widget Build(BuildContext context);
}

/// <summary>
/// Wires <see cref="RouterDelegate{T}.PopRoute"/> to the <see cref="Navigator"/> the delegate builds.
/// </summary>
/// <remarks>Dart declares this as a mixin; C# has no mixins, so it is an intermediate base class.</remarks>
public abstract class PopNavigatorRouterDelegateMixin<T> : RouterDelegate<T>
{
    /// <summary>The key used for retrieving the current navigator.</summary>
    public abstract GlobalKey<NavigatorState>? NavigatorKey { get; }

    public override Task<bool> PopRoute()
    {
        NavigatorState? navigator = NavigatorKey?.CurrentState;
        return Task.FromResult(navigator?.MaybePop() ?? false);
    }
}

/// <summary>Provides route information to a <see cref="Router{T}"/> and receives its reports.</summary>
/// <remarks>
/// Dart declares this as <c>extends ValueListenable&lt;RouteInformation&gt;</c> and expects
/// implementations to mix <c>ChangeNotifier</c> in; C# folds the notifier into the base class.
/// </remarks>
public abstract class RouteInformationProvider : ChangeNotifier, IValueListenable<RouteInformation>
{
    public abstract RouteInformation Value { get; }

    /// <summary>Called by the router when it has new route information to report.</summary>
    public virtual void RouterReportsNewRouteInformation(
        RouteInformation routeInformation,
        RouteInformationReportingType type = RouteInformationReportingType.None)
    {
    }
}

/// <summary>The route information provider that propagates platform route information changes.</summary>
public class PlatformRouteInformationProvider : RouteInformationProvider, WidgetsBindingObserver
{
    private RouteInformation _value;
    private RouteInformation _valueInEngine;

    public PlatformRouteInformationProvider(RouteInformation initialRouteInformation)
    {
        ArgumentNullException.ThrowIfNull(initialRouteInformation);
        _value = initialRouteInformation;
        _valueInEngine = new RouteInformation(
            new Uri(SystemNavigator.DefaultRouteName, UriKind.RelativeOrAbsolute));
    }

    public override RouteInformation Value => _value;

    public override void RouterReportsNewRouteInformation(
        RouteInformation routeInformation,
        RouteInformationReportingType type = RouteInformationReportingType.None)
    {
        ArgumentNullException.ThrowIfNull(routeInformation);
        SystemNavigator.SelectMultiEntryHistory();
        bool replace = type switch
        {
            RouteInformationReportingType.Neglect => true,
            RouteInformationReportingType.Navigate => false,
            _ => AreLocationsEqual(_valueInEngine.Uri, routeInformation.Uri),
        };
        SystemNavigator.RouteInformationUpdated(routeInformation.Uri, routeInformation.State, replace);
        _value = routeInformation;
        _valueInEngine = routeInformation;
    }

    public override void AddListener(Action listener)
    {
        if (!HasListeners)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }

        base.AddListener(listener);
    }

    public override void RemoveListener(Action listener)
    {
        base.RemoveListener(listener);
        if (!HasListeners)
        {
            _ = WidgetsBinding.Instance.RemoveObserver(this);
        }
    }

    public override void Dispose()
    {
        if (HasListeners)
        {
            _ = WidgetsBinding.Instance.RemoveObserver(this);
        }

        base.Dispose();
    }

    public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
    {
        PlatformReportsNewRouteInformation(routeInformation);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Flutter compares locations ignoring scheme/host and query-parameter order, so a semantically
    /// identical uri replaces the current history entry instead of pushing a new one.
    /// </summary>
    internal static bool AreLocationsEqual(Uri left, Uri right)
    {
        if (!string.Equals(GetPath(left), GetPath(right), StringComparison.Ordinal)
            || !string.Equals(GetFragment(left), GetFragment(right), StringComparison.Ordinal))
        {
            return false;
        }

        return HaveSameQueryParameters(GetQuery(left), GetQuery(right));
    }

    private static bool HaveSameQueryParameters(string leftQuery, string rightQuery)
    {
        Dictionary<string, List<string>> left = ParseQuery(leftQuery);
        Dictionary<string, List<string>> right = ParseQuery(rightQuery);
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach ((string key, List<string> leftValues) in left)
        {
            if (!right.TryGetValue(key, out List<string>? rightValues) || leftValues.Count != rightValues.Count)
            {
                return false;
            }

            leftValues.Sort(StringComparer.Ordinal);
            rightValues.Sort(StringComparer.Ordinal);
            for (int index = 0; index < leftValues.Count; index += 1)
            {
                if (!string.Equals(leftValues[index], rightValues[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static Dictionary<string, List<string>> ParseQuery(string query)
    {
        var parameters = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (query.Length == 0)
        {
            return parameters;
        }

        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=', StringComparison.Ordinal);
            string key = separator < 0 ? pair : pair[..separator];
            string value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            key = Uri.UnescapeDataString(key);
            value = Uri.UnescapeDataString(value);
            if (!parameters.TryGetValue(key, out List<string>? values))
            {
                values = [];
                parameters[key] = values;
            }

            values.Add(value);
        }

        return parameters;
    }

    private static string GetPath(Uri uri)
    {
        string path = uri.IsAbsoluteUri ? uri.AbsolutePath : SplitRelative(uri).Path;
        return path;
    }

    private static string GetQuery(Uri uri)
    {
        string query = uri.IsAbsoluteUri ? uri.Query : SplitRelative(uri).Query;
        return query.StartsWith('?') ? query[1..] : query;
    }

    private static string GetFragment(Uri uri)
    {
        string fragment = uri.IsAbsoluteUri ? uri.Fragment : SplitRelative(uri).Fragment;
        return fragment.StartsWith('#') ? fragment[1..] : fragment;
    }

    private static (string Path, string Query, string Fragment) SplitRelative(Uri uri)
    {
        string text = uri.OriginalString;
        string fragment = string.Empty;
        int hashIndex = text.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex >= 0)
        {
            fragment = text[(hashIndex + 1)..];
            text = text[..hashIndex];
        }

        string query = string.Empty;
        int questionIndex = text.IndexOf('?', StringComparison.Ordinal);
        if (questionIndex >= 0)
        {
            query = text[(questionIndex + 1)..];
            text = text[..questionIndex];
        }

        return (text, query, fragment);
    }

    private void PlatformReportsNewRouteInformation(RouteInformation routeInformation)
    {
        ArgumentNullException.ThrowIfNull(routeInformation);
        if (ReferenceEquals(_value, routeInformation))
        {
            return;
        }

        _value = routeInformation;
        _valueInEngine = routeInformation;
        NotifyListeners();
    }
}

/// <summary>The router's restorable copy of the last route information it reported.</summary>
internal sealed class RestorableRouteInformation : RestorableValue<RouteInformation?>
{
    public override RouteInformation? CreateDefaultValue() => null;

    protected override void DidUpdateValue(RouteInformation? oldValue)
    {
        NotifyListeners();
    }

    public override RouteInformation? FromPrimitives(object? data)
    {
        // Dart's `List<Object?>`: the restoration data round-trips through `StandardMessageCodec`, which
        // hands every list back as a `List<object?>` regardless of what was stored.
        if (data is not System.Collections.IList { Count: 2 } serialized)
        {
            return null;
        }

        if (serialized[0] is not string uri)
        {
            return null;
        }

        return new RouteInformation(new Uri(uri, UriKind.RelativeOrAbsolute), serialized[1]);
    }

    public override object? ToPrimitives()
    {
        return Value is null ? null : new List<object?> { Value.Uri.ToString(), Value.State };
    }
}
