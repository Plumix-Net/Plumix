// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

// The declarative page maps below are keyed by "the page route below this one", where null means the
// bottom of the stack, exactly as Flutter keys them.
#pragma warning disable CS8714

using Plumix.Foundation;

namespace Plumix.Widgets;

/// <summary>
/// Flutter's <c>_RouteLifecycle</c>. Declaration order is load-bearing: every predicate below is an
/// ordinal comparison against these values.
/// </summary>
internal enum RouteLifecycle
{
    /// <summary>Waiting for the transition delegate to decide what to do with this route.</summary>
    Staging,

    /// <summary>Will run install, didAdd; created by the initial route list or the initial pages.</summary>
    Add,

    /// <summary>Waiting for the top-most route's push to settle.</summary>
    Adding,

    /// <summary>Will run install, didPush; added through <c>Push</c> and friends.</summary>
    Push,

    /// <summary>Will run install, didPush; added through <c>PushReplacement</c> and friends.</summary>
    PushReplace,

    /// <summary>Waiting for the entering transition to settle.</summary>
    Pushing,

    /// <summary>Will run install, didReplace; added through <c>Replace</c> and friends.</summary>
    Replace,

    /// <summary>The route is being harmless.</summary>
    Idle,

    /// <summary>Will call didPop.</summary>
    Pop,

    /// <summary>Will call didComplete.</summary>
    Complete,

    /// <summary>Will run didReplace/didRemove.</summary>
    Remove,

    /// <summary>Waiting for the route to call <c>FinalizeRoute</c>.</summary>
    Popping,

    /// <summary>Waiting for subsequent routes to finish animating.</summary>
    Removing,

    /// <summary>Will dispose the route momentarily.</summary>
    Dispose,

    /// <summary>Waiting for the route's widget subtree to be disposed.</summary>
    Disposing,

    /// <summary>The route has been disposed.</summary>
    Disposed,
}

/// <summary>
/// Flutter's <c>Page</c>: the immutable description a declarative <see cref="Navigator.Pages"/> list uses to
/// create and identify a route. <see cref="RouteSettings"/> is its base, so a page-based route reports the page
/// itself through <see cref="Route.Settings"/>.
/// </summary>
public abstract record Page : RouteSettings
{
    protected Page(
        Key? key = null,
        string? name = null,
        object? arguments = null,
        string? restorationId = null,
        bool canPop = true,
        PopInvokedWithResultCallback<object>? onPopInvoked = null) : base(name, arguments)
    {
        Key = key;
        RestorationId = restorationId;
        CanPop = canPop;
        OnPopInvoked = onPopInvoked;
    }

    /// <summary>The key that keeps this page matched to the same route across page-list updates.</summary>
    public Key? Key { get; }

    /// <summary>The id this page's route state is stored under; <see langword="null"/> disables restoration.</summary>
    public string? RestorationId { get; }

    /// <summary>Whether the route created by this page may be popped. Defaults to <see langword="true"/>.</summary>
    public bool CanPop { get; }

    /// <summary>Invoked after a pop was attempted on this page's route.</summary>
    public PopInvokedWithResultCallback<object>? OnPopInvoked { get; }

    /// <summary>Whether a route created from <paramref name="other"/> can be updated to show this page.</summary>
    public virtual bool CanUpdate(Page other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.GetType() == GetType() && Equals(other.Key, Key);
    }

    /// <summary>
    /// Creates the route for this page. The returned route must report this page from
    /// <see cref="Route.Settings"/>.
    /// </summary>
    public abstract Route CreateRoute(BuildContext context);
}

/// <summary>
/// Flutter's <c>RouteTransitionRecord</c>: the handle a <see cref="TransitionDelegate"/> uses to decide how a
/// route enters or leaves the history during a declarative page update.
/// </summary>
public abstract class RouteTransitionRecord
{
    /// <summary>The route this record decides for.</summary>
    public abstract Route Route { get; }

    /// <summary>Whether the delegate still has to decide how this route enters.</summary>
    public abstract bool IsWaitingForEnteringDecision { get; }

    /// <summary>Whether the delegate still has to decide how this route leaves.</summary>
    public abstract bool IsWaitingForExitingDecision { get; }

    /// <summary>Enter with the push transition.</summary>
    public abstract void MarkForPush();

    /// <summary>Enter without a transition.</summary>
    public abstract void MarkForAdd();

    /// <summary>Leave with the pop transition.</summary>
    public abstract void MarkForPop(object? result = null);

    /// <summary>Leave without a transition.</summary>
    public abstract void MarkForComplete(object? result = null);
}

/// <summary>
/// Flutter's <c>TransitionDelegate</c>: decides how routes enter and leave when <see cref="Navigator.Pages"/>
/// changes.
/// </summary>
public abstract class TransitionDelegate
{
    /// <summary>
    /// Merges the entering routes with the exiting ones. The result must contain every entry of
    /// <paramref name="newPageRouteHistory"/> in the same relative order plus every exiting route, and every
    /// record must have had a decision made.
    /// </summary>
    public abstract IEnumerable<RouteTransitionRecord> Resolve(
        IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
        IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
        IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPagelessRoutes);

    /// <summary>Flutter's <c>TransitionDelegate._transition</c>: runs <see cref="Resolve"/> and validates it.</summary>
    internal IReadOnlyList<RouteTransitionRecord> Transition(
        IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
        IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
        IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPagelessRoutes)
    {
        IReadOnlyList<RouteTransitionRecord> results =
            Resolve(newPageRouteHistory, locationToExitingPageRoute, pageRouteToPagelessRoutes).ToList();
        Validate(results, newPageRouteHistory, locationToExitingPageRoute, pageRouteToPagelessRoutes);
        return results;
    }

    private void Validate(
        IReadOnlyList<RouteTransitionRecord> results,
        IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
        IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
        IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPagelessRoutes)
    {
        var exitingPageRoutes = new HashSet<RouteTransitionRecord>(locationToExitingPageRoute.Values);
        foreach (RouteTransitionRecord exitingPageRoute in exitingPageRoutes)
        {
            if (exitingPageRoute.IsWaitingForExitingDecision)
            {
                throw new InvalidOperationException(
                    "Every exiting route must have a decision. Try calling MarkForPop or MarkForComplete.");
            }

            if (!pageRouteToPagelessRoutes.TryGetValue(exitingPageRoute, out IReadOnlyList<RouteTransitionRecord>? p))
            {
                continue;
            }

            if (p.Any(pagelessRoute => pagelessRoute.IsWaitingForExitingDecision))
            {
                throw new InvalidOperationException(
                    "Every pageless route of an exiting route must have a decision.");
            }
        }

        int indexOfNextRouteInNewHistory = 0;
        foreach (RouteTransitionRecord record in results)
        {
            if (record.IsWaitingForEnteringDecision || record.IsWaitingForExitingDecision)
            {
                throw new InvalidOperationException(
                    "Every route in the merged result must have a decision.");
            }

            if (indexOfNextRouteInNewHistory >= newPageRouteHistory.Count
                || !ReferenceEquals(record, newPageRouteHistory[indexOfNextRouteInNewHistory]))
            {
                if (!exitingPageRoutes.Remove(record))
                {
                    throw new InvalidOperationException(
                        "The merged result from the transition delegate must preserve the order of the new "
                        + "page routes and may only interleave exiting routes.");
                }

                continue;
            }

            indexOfNextRouteInNewHistory += 1;
        }

        if (indexOfNextRouteInNewHistory != newPageRouteHistory.Count || exitingPageRoutes.Count > 0)
        {
            throw new InvalidOperationException(
                $"The merged result from the {GetType().Name}.Resolve does not include all required routes. "
                + "Do you remember to merge all exiting routes?");
        }
    }
}

/// <summary>
/// Flutter's <c>DefaultTransitionDelegate</c>: entering routes are placed above the exiting routes at the same
/// location, and only the top-most route animates.
/// </summary>
public sealed class DefaultTransitionDelegate : TransitionDelegate
{
    public override IEnumerable<RouteTransitionRecord> Resolve(
        IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
        IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
        IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPagelessRoutes)
    {
        ArgumentNullException.ThrowIfNull(newPageRouteHistory);
        ArgumentNullException.ThrowIfNull(locationToExitingPageRoute);
        ArgumentNullException.ThrowIfNull(pageRouteToPagelessRoutes);

        var results = new List<RouteTransitionRecord>();

        void HandleExitingRoute(RouteTransitionRecord? location, bool isLast)
        {
            while (true)
            {
                if (!locationToExitingPageRoute.TryGetValue(location!, out RouteTransitionRecord? exitingPageRoute))
                {
                    return;
                }

                if (exitingPageRoute.IsWaitingForExitingDecision)
                {
                    bool hasPagelessRoute = pageRouteToPagelessRoutes.ContainsKey(exitingPageRoute);
                    bool isLastExitingPageRoute = isLast && !locationToExitingPageRoute.ContainsKey(exitingPageRoute);
                    if (isLastExitingPageRoute && !hasPagelessRoute)
                    {
                        exitingPageRoute.MarkForPop(exitingPageRoute.Route.CurrentResult);
                    }
                    else
                    {
                        exitingPageRoute.MarkForComplete(exitingPageRoute.Route.CurrentResult);
                    }

                    if (hasPagelessRoute)
                    {
                        IReadOnlyList<RouteTransitionRecord> pagelessRoutes =
                            pageRouteToPagelessRoutes[exitingPageRoute];
                        foreach (RouteTransitionRecord pagelessRoute in pagelessRoutes)
                        {
                            if (!pagelessRoute.IsWaitingForExitingDecision)
                            {
                                continue;
                            }

                            if (isLastExitingPageRoute && ReferenceEquals(pagelessRoute, pagelessRoutes[^1]))
                            {
                                pagelessRoute.MarkForPop(pagelessRoute.Route.CurrentResult);
                            }
                            else
                            {
                                pagelessRoute.MarkForComplete(pagelessRoute.Route.CurrentResult);
                            }
                        }
                    }
                }

                results.Add(exitingPageRoute);
                location = exitingPageRoute;
            }
        }

        HandleExitingRoute(location: null, isLast: newPageRouteHistory.Count == 0);

        foreach (RouteTransitionRecord pageRoute in newPageRouteHistory)
        {
            bool isLastIteration = ReferenceEquals(newPageRouteHistory[^1], pageRoute);
            if (pageRoute.IsWaitingForEnteringDecision)
            {
                if (!locationToExitingPageRoute.ContainsKey(pageRoute) && isLastIteration)
                {
                    pageRoute.MarkForPush();
                }
                else
                {
                    pageRoute.MarkForAdd();
                }
            }

            results.Add(pageRoute);
            HandleExitingRoute(pageRoute, isLastIteration);
        }

        return results;
    }
}

/// <summary>Flutter's <c>_RoutePlaceholder</c>: the sentinel a not-yet-announced neighbor is compared against.</summary>
internal sealed class RoutePlaceholder
{
    public static readonly RoutePlaceholder NotAnnounced = new();
}

/// <summary>
/// Flutter's <c>_RouteEntry</c>: the navigator's per-route bookkeeping. The navigator only ever mutates
/// <see cref="CurrentState"/>; <see cref="NavigatorState.FlushHistoryUpdates"/> drives every route callback.
/// </summary>
internal sealed class RouteEntry : RouteTransitionRecord
{
    private const int DebugPopAttemptLimit = 100;

    private bool _isWaitingForExitingDecision;

    public RouteEntry(
        Route route,
        RouteLifecycle initialState,
        bool pageBased,
        RestorationInformation? restorationInformation = null)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (pageBased && route.Settings is not Page)
        {
            throw new ArgumentException(
                "A page-based route must report its Page from Route.Settings.", nameof(route));
        }

        if (initialState is not (RouteLifecycle.Staging or RouteLifecycle.Add or RouteLifecycle.Push
            or RouteLifecycle.PushReplace or RouteLifecycle.Replace))
        {
            throw new ArgumentOutOfRangeException(nameof(initialState));
        }

        RouteValue = route;
        PageBased = pageBased;
        RestorationInformation = restorationInformation;
        CurrentState = initialState;
    }

    private Route RouteValue { get; }

    public override Route Route => RouteValue;

    public RestorationInformation? RestorationInformation { get; }

    public bool PageBased { get; }

    public RouteLifecycle CurrentState { get; set; }

    public object? LastAnnouncedPreviousRoute { get; set; } = RoutePlaceholder.NotAnnounced;

    public object? LastAnnouncedPoppedNextRoute { get; set; } = RoutePlaceholder.NotAnnounced;

    public object? LastAnnouncedNextRoute { get; set; } = RoutePlaceholder.NotAnnounced;

    /// <summary>True when the route was removed imperatively rather than through the pages API.</summary>
    public bool ImperativeRemoval { get; set; }

    public object? PendingResult { get; set; }

    private bool ReportRemovalToObserver { get; set; } = true;

    public bool WillBePresent =>
        CurrentState <= RouteLifecycle.Idle && CurrentState >= RouteLifecycle.Add;

    public bool IsPresent =>
        CurrentState <= RouteLifecycle.Remove && CurrentState >= RouteLifecycle.Add;

    public bool IsPresentForRestoration => CurrentState <= RouteLifecycle.Idle;

    public bool SuitableForAnnouncement =>
        CurrentState <= RouteLifecycle.Removing && CurrentState >= RouteLifecycle.Push;

    public bool SuitableForTransitionAnimation =>
        CurrentState <= RouteLifecycle.Remove && CurrentState >= RouteLifecycle.Push;

    public override bool IsWaitingForEnteringDecision => CurrentState == RouteLifecycle.Staging;

    public override bool IsWaitingForExitingDecision => _isWaitingForExitingDecision;

    public static bool IsPresentPredicate(RouteEntry entry) => entry.IsPresent;

    public static bool SuitableForTransitionAnimationPredicate(RouteEntry entry) =>
        entry.SuitableForTransitionAnimation;

    public static bool WillBePresentPredicate(RouteEntry entry) => entry.WillBePresent;

    public static Func<RouteEntry, bool> IsRoutePredicate(Route route) =>
        entry => ReferenceEquals(entry.Route, route);

    public string? RestorationId
    {
        get
        {
            if (PageBased)
            {
                var page = (Page)Route.Settings;
                return page.RestorationId is not null ? $"p+{page.RestorationId}" : null;
            }

            return RestorationInformation is not null
                ? $"r+{RestorationInformation.RestorationScopeId}"
                : null;
        }
    }

    public bool RestorationEnabled
    {
        get => Route.RestorationScopeId.Value is not null;
        set => Route.UpdateRestorationId(value ? RestorationId : null);
    }

    public bool CanUpdateFrom(Page page)
    {
        if (!WillBePresent || !PageBased)
        {
            return false;
        }

        return page.CanUpdate((Page)Route.Settings);
    }

    public void HandleAdd(NavigatorState navigator, Route? previousPresent)
    {
        CurrentState = RouteLifecycle.Adding;
        navigator.EnqueueRouteAddition(new NavigatorPushObservation(Route, previousPresent));
    }

    public void HandlePush(NavigatorState navigator, bool isNewFirst, Route? previous, Route? previousPresent)
    {
        if (Route.Installed)
        {
            throw new InvalidOperationException(
                "The pushed route has already been used. When pushing a route, a new Route object must be provided.");
        }

        RouteLifecycle previousState = CurrentState;
        Route.Attach(navigator);
        if (CurrentState is RouteLifecycle.Push or RouteLifecycle.PushReplace)
        {
            Route.DidPush();
            CurrentState = RouteLifecycle.Pushing;
            navigator.WhenPushSettles(this);
        }
        else
        {
            Route.DidReplace(previous);
            CurrentState = RouteLifecycle.Idle;
        }

        if (isNewFirst)
        {
            Route.DidChangeNext(null);
        }

        if (previousState is RouteLifecycle.Replace or RouteLifecycle.PushReplace)
        {
            navigator.EnqueueRouteAddition(new NavigatorReplaceObservation(Route, previousPresent));
            if (previousPresent?.Settings is Page removedPage)
            {
                navigator.NotifyDidRemovePage(removedPage);
            }
        }
        else
        {
            navigator.EnqueueRouteAddition(new NavigatorPushObservation(Route, previousPresent));
        }
    }

    public void HandleDidPopNext(Route poppedRoute)
    {
        Route.DidPopNext(poppedRoute);
        LastAnnouncedPoppedNextRoute = poppedRoute;
    }

    public bool HandlePop(NavigatorState navigator, Route? previousPresent)
    {
        CurrentState = RouteLifecycle.Popping;
        if (Route.PopCompleted)
        {
            return true;
        }

        if (!Route.DidPop(PendingResult))
        {
            CurrentState = RouteLifecycle.Idle;
            return false;
        }

        Route.OnPopInvokedWithResult(didPop: true, PendingResult);
        if (PageBased && ImperativeRemoval && Route.Settings is Page page)
        {
            navigator.NotifyDidRemovePage(page);
        }

        PendingResult = null;
        return true;
    }

    public void HandleComplete()
    {
        Route.DidComplete(PendingResult);
        PendingResult = null;
        CurrentState = RouteLifecycle.Remove;
    }

    public void HandleRemoval(NavigatorState navigator, Route? previousPresent)
    {
        CurrentState = Route.IsInstalledIn(navigator) ? RouteLifecycle.Removing : RouteLifecycle.Dispose;
        if (ReportRemovalToObserver)
        {
            navigator.EnqueueRouteDeletion(new NavigatorRemoveObservation(Route, previousPresent));
        }
    }

    public void DidAdd(NavigatorState navigator, bool isNewFirst)
    {
        Route.Attach(navigator);
        Route.DidAdd();
        CurrentState = RouteLifecycle.Idle;
        if (isNewFirst)
        {
            Route.DidChangeNext(null);
        }
    }

    public void Pop(object? result, bool imperativeRemoval)
    {
        PendingResult = result;
        CurrentState = RouteLifecycle.Pop;
        ImperativeRemoval = imperativeRemoval;
    }

    public void Complete(object? result, bool isReplaced, bool imperativeRemoval)
    {
        if (CurrentState >= RouteLifecycle.Remove)
        {
            return;
        }

        ReportRemovalToObserver = !isReplaced;
        PendingResult = result;
        CurrentState = RouteLifecycle.Complete;
        ImperativeRemoval = imperativeRemoval;
    }

    public void Finalize()
    {
        CurrentState = RouteLifecycle.Dispose;
    }

    public void ForcedDispose()
    {
        CurrentState = RouteLifecycle.Disposed;
        Route.Dispose();
        Route.Detach();
    }

    /// <summary>
    /// Flutter's <c>_RouteEntry.dispose</c>: disposal waits until every overlay entry widget unmounted, because
    /// widgets in the route's subtree can still reference the route while they are mounted.
    /// </summary>
    public void Dispose(NavigatorState navigator)
    {
        CurrentState = RouteLifecycle.Disposing;
        OverlayEntry[] mountedEntries = Route.OverlayEntries.Where(entry => entry.Mounted).ToArray();
        foreach (OverlayEntry entry in Route.OverlayEntries.ToArray())
        {
            if (entry.Owner is not null)
            {
                entry.Remove();
            }
        }

        if (mountedEntries.Length == 0)
        {
            ForcedDispose();
            return;
        }

        int remaining = mountedEntries.Length;
        navigator.AddEntryWaitingForSubtreeDisposal(this);
        foreach (OverlayEntry entry in mountedEntries)
        {
            Action? listener = null;
            listener = () =>
            {
                if (entry.Mounted)
                {
                    return;
                }

                entry.RemoveListener(listener!);
                remaining -= 1;
                if (remaining != 0)
                {
                    return;
                }

                if (!navigator.RemoveEntryWaitingForSubtreeDisposal(this))
                {
                    return;
                }

                ForcedDispose();
            };

            entry.AddListener(listener);
        }
    }

    public void MarkNeedsExitingDecision() => _isWaitingForExitingDecision = true;

    public override void MarkForPush()
    {
        ThrowIfDecided(entering: true);
        CurrentState = RouteLifecycle.Push;
    }

    public override void MarkForAdd()
    {
        ThrowIfDecided(entering: true);
        CurrentState = RouteLifecycle.Add;
    }

    public override void MarkForPop(object? result = null)
    {
        ThrowIfDecided(entering: false);
        int attempt = 0;
        while (Route.WillHandlePopInternally)
        {
            attempt += 1;
            if (attempt >= DebugPopAttemptLimit)
            {
                throw new InvalidOperationException(
                    $"Attempted to pop {Route} {DebugPopAttemptLimit} times, but still failed.");
            }

            Route.DidPop(result);
        }

        Pop(result, imperativeRemoval: false);
        _isWaitingForExitingDecision = false;
    }

    public override void MarkForComplete(object? result = null)
    {
        ThrowIfDecided(entering: false);
        Complete(result, isReplaced: false, imperativeRemoval: false);
        _isWaitingForExitingDecision = false;
    }

    /// <summary>
    /// Flutter's <c>_RouteEntry.shouldAnnounceChangeToNext</c>: suppresses a "next became null" announcement
    /// that <c>didPopNext</c> already made.
    /// </summary>
    public bool ShouldAnnounceChangeToNext(Route? nextRoute)
    {
        return !(nextRoute is null && ReferenceEquals(LastAnnouncedPoppedNextRoute, LastAnnouncedNextRoute));
    }

    private void ThrowIfDecided(bool entering)
    {
        if (entering && (!IsWaitingForEnteringDecision || IsWaitingForExitingDecision))
        {
            throw new InvalidOperationException("This route is not waiting for an entering decision.");
        }

        if (!entering && (IsWaitingForEnteringDecision || !IsWaitingForExitingDecision || !IsPresent))
        {
            throw new InvalidOperationException("This route is not waiting for an exiting decision.");
        }
    }
}

/// <summary>Flutter's <c>_NavigatorObservation</c> family: one queued observer callback.</summary>
internal abstract class NavigatorObservation
{
    protected NavigatorObservation(Route primaryRoute, Route? secondaryRoute)
    {
        PrimaryRoute = primaryRoute;
        SecondaryRoute = secondaryRoute;
    }

    protected Route PrimaryRoute { get; }

    protected Route? SecondaryRoute { get; }

    public abstract void Notify(NavigatorObserver observer);
}

internal sealed class NavigatorPushObservation : NavigatorObservation
{
    public NavigatorPushObservation(Route primaryRoute, Route? secondaryRoute)
        : base(primaryRoute, secondaryRoute)
    {
    }

    public override void Notify(NavigatorObserver observer) => observer.DidPush(PrimaryRoute, SecondaryRoute);
}

internal sealed class NavigatorPopObservation : NavigatorObservation
{
    public NavigatorPopObservation(Route primaryRoute, Route? secondaryRoute)
        : base(primaryRoute, secondaryRoute)
    {
    }

    public override void Notify(NavigatorObserver observer) => observer.DidPop(PrimaryRoute, SecondaryRoute);
}

internal sealed class NavigatorRemoveObservation : NavigatorObservation
{
    public NavigatorRemoveObservation(Route primaryRoute, Route? secondaryRoute)
        : base(primaryRoute, secondaryRoute)
    {
    }

    public override void Notify(NavigatorObserver observer) => observer.DidRemove(PrimaryRoute, SecondaryRoute);
}

internal sealed class NavigatorReplaceObservation : NavigatorObservation
{
    public NavigatorReplaceObservation(Route primaryRoute, Route? secondaryRoute)
        : base(primaryRoute, secondaryRoute)
    {
    }

    public override void Notify(NavigatorObserver observer) => observer.DidReplace(PrimaryRoute, SecondaryRoute);
}

/// <summary>
/// The declarative page maps Flutter keys by "the page route below this one", using <see langword="null"/>
/// for the bottom of the stack. .NET dictionaries reject a null key, so this map keeps that slot separately
/// while still presenting the Flutter-shaped <see cref="IReadOnlyDictionary{TKey,TValue}"/> contract to a
/// <see cref="TransitionDelegate"/>.
/// </summary>
public sealed class RouteRecordMap<TKey, TValue> : IReadOnlyDictionary<TKey?, TValue>
    where TKey : class
{
    private readonly Dictionary<TKey, TValue> _entries = [];
    private bool _hasBottomEntry;
    private TValue? _bottomEntry;

    public int Count => _entries.Count + (_hasBottomEntry ? 1 : 0);

    public IEnumerable<TKey?> Keys
    {
        get
        {
            if (_hasBottomEntry)
            {
                yield return null;
            }

            foreach (TKey key in _entries.Keys)
            {
                yield return key;
            }
        }
    }

    public IEnumerable<TValue> Values
    {
        get
        {
            if (_hasBottomEntry)
            {
                yield return _bottomEntry!;
            }

            foreach (TValue value in _entries.Values)
            {
                yield return value;
            }
        }
    }

    public TValue this[TKey? key]
    {
        get => TryGetValue(key, out TValue? value) ? value : throw new KeyNotFoundException();
        set
        {
            if (key is null)
            {
                _hasBottomEntry = true;
                _bottomEntry = value;
                return;
            }

            _entries[key] = value;
        }
    }

    public bool ContainsKey(TKey? key) => key is null ? _hasBottomEntry : _entries.ContainsKey(key);

    public bool TryGetValue(TKey? key, out TValue value)
    {
        if (key is null)
        {
            value = _hasBottomEntry ? _bottomEntry! : default!;
            return _hasBottomEntry;
        }

        return _entries.TryGetValue(key, out value!);
    }

    /// <summary>Adds <paramref name="value"/> only when <paramref name="key"/> has no entry yet.</summary>
    public bool TryAdd(TKey? key, TValue value)
    {
        if (ContainsKey(key))
        {
            return false;
        }

        this[key] = value;
        return true;
    }

    /// <summary>Reinterprets this map through the base types a <see cref="TransitionDelegate"/> receives.</summary>
    internal RouteRecordMap<TProjectedKey, TProjectedValue> Project<TProjectedKey, TProjectedValue>()
        where TProjectedKey : class
    {
        var result = new RouteRecordMap<TProjectedKey, TProjectedValue>();
        foreach (TKey? key in Keys)
        {
            result[(TProjectedKey?)(object?)key] = (TProjectedValue)(object)this[key]!;
        }

        return result;
    }

    public IEnumerator<KeyValuePair<TKey?, TValue>> GetEnumerator()
    {
        if (_hasBottomEntry)
        {
            yield return new KeyValuePair<TKey?, TValue>(null, _bottomEntry!);
        }

        foreach ((TKey key, TValue value) in _entries)
        {
            yield return new KeyValuePair<TKey?, TValue>(key, value);
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
