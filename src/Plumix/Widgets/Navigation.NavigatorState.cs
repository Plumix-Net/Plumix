// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

using Plumix.Foundation;
using Plumix.UI;

#pragma warning disable CS8714

namespace Plumix.Widgets;

public sealed partial class NavigatorState : RestorationState
{
    private const double HeroTransitionDurationMilliseconds = 300;

    private readonly List<RouteEntry> _history = [];
    private readonly HashSet<RouteEntry> _entriesWaitingForSubtreeDisposal = [];
    private readonly HashSet<Route> _heroDeferredRoutes = [];
    private readonly List<NavigatorObservation> _observedRouteAdditions = [];
    private readonly Queue<NavigatorObservation> _observedRouteDeletions = new();
    private readonly HistoryProperty _serializableHistory = new();
    private readonly RestorableInt _rawNextPagelessRestorationScopeId = new(0);
    private readonly List<NavigatorObserver> _observers = [];
    private readonly Func<bool> _backButtonHandler;
    private readonly HeroTransitionController _heroTransitionController = new();
    private readonly Plumix.AnimationController _heroFlightController;

    private GlobalKey<OverlayState> _overlayKey;
    private OverlayEntry? _heroFlightEntry;
    private int _userGestureCount;
    private HeroTransitionSession? _heroTransitionSession;
    private RouteEntry? _lastTopmostRoute;
    private string? _lastAnnouncedRouteName;
    private bool? _lastCanHandlePop;
    private bool _navigationNotificationPending;
    private bool _flushingHistory;
    private bool _updatingPage;
    private (Route? From, Route To)? _pendingHeroPush;
    private (Route From, Route? To)? _pendingHeroPop;

    public NavigatorState()
    {
        // Identity-based key: a label-based GlobalKey is a record and would collide across navigators.
        _overlayKey = new GlobalObjectKey<OverlayState>(this);
        _backButtonHandler = HandleBackButton;
        _heroFlightController = new Plumix.AnimationController(
            duration: TimeSpan.FromMilliseconds(HeroTransitionDurationMilliseconds))
        {
            Curve = Plumix.Curves.EaseInOut,
        };
        _heroFlightController.Changed += HandleHeroFlightTick;
        _heroFlightController.Completed += HandleHeroFlightCompleted;
        _heroFlightController.Dismissed += HandleHeroFlightCompleted;
    }

    /// <summary>The focus node the navigator installs above its overlay; routes focus its enclosing scope.</summary>
    public FocusNode FocusNode { get; } = new();

    internal Navigator NavigatorWidget => (Navigator)Element.Widget;

    private Navigator CurrentWidget => NavigatorWidget;

    protected override string? RestorationId => CurrentWidget.RestorationScopeId;

    public bool CanPop
    {
        get
        {
            bool seenOne = false;
            foreach (RouteEntry entry in _history)
            {
                if (!entry.IsPresent)
                {
                    continue;
                }

                if (entry.Route.WillHandlePopInternally)
                {
                    return true;
                }

                if (seenOne)
                {
                    return true;
                }

                seenOne = true;
            }

            return false;
        }
    }

    public Route? CurrentRoute => LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate)?.Route;

    public bool UserGestureInProgress => _userGestureCount > 0;

    /// <summary>The overlay this navigator installs its routes into.</summary>
    public OverlayState? Overlay => _overlayKey.CurrentState;

    /// <summary>Notifies while a user-driven route gesture is in progress.</summary>
    public ValueNotifier<bool> UserGestureInProgressNotifier { get; } = new(false);

    internal HeroTransitionController HeroTransitionController => _heroTransitionController;

    internal IReadOnlyList<RouteEntry> HistoryEntries => _history;

    public override void InitState()
    {
        base.InitState();
        SyncObservers(Array.Empty<NavigatorObserver>(), CurrentWidget.Observers);
        ValidatePagesApi();
        NavigatorBackButtonDispatcher.AddHandler(_backButtonHandler);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldNavigator = (Navigator)oldWidget;
        SyncObservers(oldNavigator.Observers, CurrentWidget.Observers);
        ValidatePagesApi();

        if (CurrentWidget.UsingPagesApi && !ReferenceEquals(oldNavigator.Pages, CurrentWidget.Pages)
            && !RestorePending)
        {
            UpdatePages();
        }

        foreach (RouteEntry entry in _history.ToArray())
        {
            if (entry.Route.IsInstalledIn(this))
            {
                entry.Route.ChangedExternalState();
            }
        }

        if (_history.Count == 0)
        {
            PushInitialRoutes();
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
        FocusNode.Dispose();

        ForcedDisposeAllRouteEntries();
        UserGestureInProgressNotifier.Dispose();
        _rawNextPagelessRestorationScopeId.Dispose();
        _serializableHistory.Dispose();
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
        bool routeBlocksPop = !CanPop && CurrentRoute?.PopDisposition == RoutePopDisposition.DoNotPop;
        ScheduleNavigationNotification(CanPop || routeBlocksPop);
        return new NavigatorScope(
            this,
            new FocusTraversalGroup(
                policy: FocusTraversalGroup.MaybeOf(context),
                child: new Focus(
                    focusNode: FocusNode,
                    autofocus: true,
                    skipTraversal: true,
                    child: new UnmanagedRestorationScope(
                        bucket: Bucket,
                        child: new Overlay(
                            initialEntries: Overlay is null ? AllRouteOverlayEntries() : [],
                            clipBehavior: CurrentWidget.ClipBehavior,
                            key: _overlayKey)))));
    }

    // -------------------------------------------------------------------------------------------------
    // Restoration
    // -------------------------------------------------------------------------------------------------

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(_rawNextPagelessRestorationScopeId, "id");
        RegisterForRestoration(_serializableHistory, "history");

        ForcedDisposeAllRouteEntries();
        _overlayKey = new GlobalObjectKey<OverlayState>(new object());

        _history.AddRange(_serializableHistory.RestoreEntriesForPage(null, this));

        foreach (Page page in CurrentWidget.Pages ?? [])
        {
            var entry = new RouteEntry(page.CreateRoute(Context), RouteLifecycle.Add, pageBased: true);
            if (!ReferenceEquals(entry.Route.Settings, page))
            {
                throw new InvalidOperationException(
                    "The Settings of a page-based Route must return the Page object. Please set the settings to "
                    + "the Page in the Page.CreateRoute method.");
            }

            _history.Add(entry);
            _history.AddRange(_serializableHistory.RestoreEntriesForPage(entry, this));
        }

        if (!_serializableHistory.HasData && !CurrentWidget.UsingPagesApi)
        {
            _history.AddRange(CreateInitialRouteEntries());
        }

        if (_history.Count == 0)
        {
            throw new InvalidOperationException("The Navigator could not generate any initial route.");
        }

        FlushWithLock();
    }

    protected override void DidToggleBucket(RestorationBucket? oldBucket)
    {
        base.DidToggleBucket(oldBucket);
        if (Bucket is not null)
        {
            _serializableHistory.Update(_history);
        }
        else
        {
            _serializableHistory.Clear();
        }
    }

    private int NextPagelessRestorationScopeId => _rawNextPagelessRestorationScopeId.Value++;

    /// <summary>Flutter's <c>NavigatorState._getRouteById</c>.</summary>
    public Route? GetRouteById(string id) =>
        FirstRouteEntryWhereOrNull(entry => string.Equals(entry.RestorationId, id, StringComparison.Ordinal))?.Route;

    public string RestorablePushNamed(string routeName, object? arguments = null)
    {
        var entry = RestorationInformation
            .Named(routeName, arguments, NextPagelessRestorationScopeId)
            .ToRouteEntry(this, RouteLifecycle.Push);
        PushEntry(entry);
        return entry.RestorationId!;
    }

    public string RestorablePushReplacementNamed(string routeName, object? arguments = null, object? result = null)
    {
        var entry = RestorationInformation
            .Named(routeName, arguments, NextPagelessRestorationScopeId)
            .ToRouteEntry(this, RouteLifecycle.PushReplace);
        PushReplacementEntry(entry, result);
        return entry.RestorationId!;
    }

    public string RestorablePushNamedAndRemoveUntil(
        string newRouteName,
        RoutePredicate predicate,
        object? arguments = null)
    {
        var entry = RestorationInformation
            .Named(newRouteName, arguments, NextPagelessRestorationScopeId)
            .ToRouteEntry(this, RouteLifecycle.Push);
        PushEntryAndRemoveUntil(entry, predicate);
        return entry.RestorationId!;
    }

    public string RestorablePush(RestorableRouteBuilder routeBuilder, object? arguments = null)
    {
        var entry = CreateAnonymousEntry(routeBuilder, arguments, RouteLifecycle.Push);
        PushEntry(entry);
        return entry.RestorationId!;
    }

    public string RestorablePushReplacement(
        RestorableRouteBuilder routeBuilder,
        object? arguments = null,
        object? result = null)
    {
        var entry = CreateAnonymousEntry(routeBuilder, arguments, RouteLifecycle.PushReplace);
        PushReplacementEntry(entry, result);
        return entry.RestorationId!;
    }

    public string RestorablePushAndRemoveUntil(
        RestorableRouteBuilder newRouteBuilder,
        RoutePredicate predicate,
        object? arguments = null)
    {
        var entry = CreateAnonymousEntry(newRouteBuilder, arguments, RouteLifecycle.Push);
        PushEntryAndRemoveUntil(entry, predicate);
        return entry.RestorationId!;
    }

    public string RestorableReplace(Route oldRoute, RestorableRouteBuilder newRouteBuilder, object? arguments = null)
    {
        var entry = CreateAnonymousEntry(newRouteBuilder, arguments, RouteLifecycle.Replace);
        ReplaceEntry(entry, oldRoute);
        return entry.RestorationId!;
    }

    public string RestorableReplaceRouteBelow(
        Route anchorRoute,
        RestorableRouteBuilder newRouteBuilder,
        object? arguments = null)
    {
        var entry = CreateAnonymousEntry(newRouteBuilder, arguments, RouteLifecycle.Replace);
        ReplaceEntryBelow(entry, anchorRoute);
        return entry.RestorationId!;
    }

    private RouteEntry CreateAnonymousEntry(
        RestorableRouteBuilder routeBuilder,
        object? arguments,
        RouteLifecycle initialState)
    {
        ArgumentNullException.ThrowIfNull(routeBuilder);
        return RestorationInformation
            .Anonymous(routeBuilder, arguments, NextPagelessRestorationScopeId)
            .ToRouteEntry(this, initialState);
    }

    // -------------------------------------------------------------------------------------------------
    // Imperative history operations
    // -------------------------------------------------------------------------------------------------

    public void Push(Route route)
    {
        ArgumentNullException.ThrowIfNull(route);
        PushEntry(new RouteEntry(route, RouteLifecycle.Push, pageBased: false));
    }

    public void PushAndRemoveUntil(Route newRoute, RoutePredicate predicate)
    {
        ArgumentNullException.ThrowIfNull(newRoute);
        ArgumentNullException.ThrowIfNull(predicate);
        PushEntryAndRemoveUntil(new RouteEntry(newRoute, RouteLifecycle.Push, pageBased: false), predicate);
    }

    public void PushNamed(string routeName, object? arguments = null)
    {
        Push(ResolveRouteLocation(routeName, arguments));
    }

    public void PushNamed(RouteData routeData)
    {
        ArgumentNullException.ThrowIfNull(routeData);
        Push(ResolveRouteData(routeData));
    }

    public void PushNamedAndRemoveUntil(string routeName, RoutePredicate predicate, object? arguments = null)
    {
        PushAndRemoveUntil(ResolveRouteLocation(routeName, arguments), predicate);
    }

    public void PushNamedAndRemoveUntil(RouteData routeData, RoutePredicate predicate)
    {
        ArgumentNullException.ThrowIfNull(routeData);
        PushAndRemoveUntil(ResolveRouteData(routeData), predicate);
    }

    public void PushReplacement(Route newRoute, object? result = null)
    {
        ArgumentNullException.ThrowIfNull(newRoute);
        PushReplacementEntry(new RouteEntry(newRoute, RouteLifecycle.PushReplace, pageBased: false), result);
    }

    public void PushReplacementNamed(string routeName, object? arguments = null, object? result = null)
    {
        PushReplacement(ResolveRouteLocation(routeName, arguments), result);
    }

    public void PushReplacementNamed(RouteData routeData, object? result = null)
    {
        ArgumentNullException.ThrowIfNull(routeData);
        PushReplacement(ResolveRouteData(routeData), result);
    }

    /// <summary>Flutter's <c>Navigator.replace</c>: swaps <paramref name="oldRoute"/> without a transition.</summary>
    public void Replace(Route oldRoute, Route newRoute)
    {
        ArgumentNullException.ThrowIfNull(oldRoute);
        ArgumentNullException.ThrowIfNull(newRoute);
        if (ReferenceEquals(oldRoute, newRoute))
        {
            return;
        }

        ReplaceEntry(new RouteEntry(newRoute, RouteLifecycle.Replace, pageBased: false), oldRoute);
    }

    /// <summary>Flutter's <c>Navigator.replaceRouteBelow</c>.</summary>
    public void ReplaceRouteBelow(Route anchorRoute, Route newRoute)
    {
        ArgumentNullException.ThrowIfNull(anchorRoute);
        ArgumentNullException.ThrowIfNull(newRoute);
        ReplaceEntryBelow(new RouteEntry(newRoute, RouteLifecycle.Replace, pageBased: false), anchorRoute);
    }

    public bool MaybePop(object? result = null)
    {
        RouteEntry? lastEntry = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate);
        if (lastEntry is null)
        {
            return false;
        }

        Route route = lastEntry.Route;
        if (!route.WillPop(result))
        {
            return true;
        }

        if (!ReferenceEquals(lastEntry, LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate)))
        {
            return true;
        }

        switch (route.PopDisposition)
        {
            case RoutePopDisposition.Bubble:
                return false;
            case RoutePopDisposition.DoNotPop:
                route.OnPopInvokedWithResult(didPop: false, result);
                return true;
            case RoutePopDisposition.Pop:
                Pop(result);
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Pop(object? result = null)
    {
        RouteEntry entry = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate)
                           ?? throw new InvalidOperationException("Navigator cannot pop without a current route.");
        if (!CanPop)
        {
            throw new InvalidOperationException("Navigator cannot pop the current route.");
        }

        // A page-based route runs its own pop so the transition plays; `HandlePop` then asks the owner to drop
        // the page through `OnDidRemovePage`.
        entry.Pop(result, imperativeRemoval: true);

        if (entry.CurrentState == RouteLifecycle.Pop)
        {
            FlushWithLock(rearrangeOverlay: false);
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
        ArgumentNullException.ThrowIfNull(predicate);
        RouteEntry? candidate = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate);
        while (candidate is not null && !predicate(candidate.Route))
        {
            if (!CanPop || !candidate.Route.WillPop(result: null))
            {
                return;
            }

            Pop();
            candidate = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate);
        }
    }

    public void RemoveRoute(Route route, object? result = null)
    {
        ArgumentNullException.ThrowIfNull(route);
        RouteEntry entry = _history.FirstOrDefault(candidate => ReferenceEquals(candidate.Route, route))
                           ?? throw new InvalidOperationException("Route is not present in Navigator history.");
        if (_history.Count(candidate => candidate.IsPresent) <= 1)
        {
            throw new InvalidOperationException("Navigator cannot remove the last route.");
        }

        entry.Complete(result, isReplaced: false, imperativeRemoval: true);
        FlushWithLock(rearrangeOverlay: false);
    }

    public void RemoveRouteBelow(Route anchorRoute, object? result = null)
    {
        ArgumentNullException.ThrowIfNull(anchorRoute);
        int anchorIndex = _history.FindIndex(entry => ReferenceEquals(entry.Route, anchorRoute) && entry.IsPresent);
        if (anchorIndex < 0)
        {
            throw new InvalidOperationException("Anchor route is not present in Navigator history.");
        }

        int index = GetIndexBefore(anchorIndex - 1, RouteEntry.IsPresentPredicate);
        if (index < 0)
        {
            throw new InvalidOperationException("Anchor route does not have a route below it.");
        }

        _history[index].Complete(result, isReplaced: false, imperativeRemoval: true);
        FlushWithLock(rearrangeOverlay: false);
    }

    public void StartUserGesture()
    {
        RouteEntry? entry = LastRouteEntryWhereOrNull(RouteEntry.WillBePresentPredicate);
        if (entry is null)
        {
            return;
        }

        _userGestureCount += 1;
        UserGestureInProgressNotifier.Value = true;
        if (_userGestureCount > 1)
        {
            return;
        }

        Route? previousRoute = entry.Route.WillHandlePopInternally
            ? null
            : GetRouteBefore(_history.IndexOf(entry) - 1, RouteEntry.WillBePresentPredicate)?.Route;
        NotifyObserversStartUserGesture(entry.Route, previousRoute);
    }

    public void StopUserGesture()
    {
        if (_userGestureCount == 0)
        {
            return;
        }

        _userGestureCount -= 1;
        UserGestureInProgressNotifier.Value = _userGestureCount > 0;
        if (_userGestureCount > 0)
        {
            return;
        }

        NotifyObserversStopUserGesture();
    }

    private void PushEntry(RouteEntry entry)
    {
        _history.Add(entry);
        FlushWithLock();
    }

    private void PushReplacementEntry(RouteEntry entry, object? result)
    {
        RouteEntry? oldEntry = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate);
        oldEntry?.Complete(result, isReplaced: true, imperativeRemoval: true);
        _history.Add(entry);
        FlushWithLock();
    }

    private void PushEntryAndRemoveUntil(RouteEntry entry, RoutePredicate predicate)
    {
        int index = _history.Count - 1;
        _history.Add(entry);
        while (index >= 0 && !predicate(_history[index].Route))
        {
            if (_history[index].IsPresent)
            {
                _history[index].Complete(null, isReplaced: false, imperativeRemoval: true);
            }

            index -= 1;
        }

        FlushWithLock();
    }

    private void ReplaceEntry(RouteEntry entry, Route oldRoute)
    {
        int index = _history.FindIndex(candidate =>
            ReferenceEquals(candidate.Route, oldRoute) && candidate.IsPresent);
        if (index < 0)
        {
            throw new InvalidOperationException("The old route is not present in Navigator history.");
        }

        _history.Insert(index + 1, entry);
        _history[index].Complete(null, isReplaced: true, imperativeRemoval: true);
        FlushWithLock();
    }

    private void ReplaceEntryBelow(RouteEntry entry, Route anchorRoute)
    {
        int anchorIndex = _history.FindIndex(candidate =>
            ReferenceEquals(candidate.Route, anchorRoute) && candidate.IsPresent);
        if (anchorIndex < 0)
        {
            throw new InvalidOperationException("The anchor route is not present in Navigator history.");
        }

        int index = GetIndexBefore(anchorIndex - 1, RouteEntry.IsPresentPredicate);
        if (index < 0)
        {
            throw new InvalidOperationException("Anchor route does not have a route below it.");
        }

        _history.Insert(index + 1, entry);
        _history[index].Complete(null, isReplaced: true, imperativeRemoval: true);
        FlushWithLock();
    }

    // -------------------------------------------------------------------------------------------------
    // Declarative pages
    // -------------------------------------------------------------------------------------------------

    /// <summary>Flutter's <c>NavigatorState._updatePages</c>.</summary>
    private void UpdatePages()
    {
        CheckDuplicatedPageKeys();
        _updatingPage = true;

        IReadOnlyList<Page> pages = CurrentWidget.Pages!;
        bool needsExplicitDecision = false;
        int newPagesBottom = 0;
        int oldEntriesBottom = 0;
        int newPagesTop = pages.Count - 1;
        int oldEntriesTop = _history.Count - 1;

        var newHistory = new List<RouteEntry>();
        var pageRouteToPagelessRoutes = new RouteRecordMap<RouteEntry, List<RouteEntry>>();
        RouteEntry? previousOldPageRouteEntry = null;

        // Phase 1: sync from the bottom while the pages still match.
        while (oldEntriesBottom <= oldEntriesTop)
        {
            RouteEntry oldEntry = _history[oldEntriesBottom];
            if (!oldEntry.PageBased)
            {
                AddPagelessRoute(pageRouteToPagelessRoutes, previousOldPageRouteEntry, oldEntry);
                oldEntriesBottom += 1;
                continue;
            }

            if (newPagesBottom > newPagesTop)
            {
                break;
            }

            Page newPage = pages[newPagesBottom];
            if (!oldEntry.CanUpdateFrom(newPage))
            {
                break;
            }

            previousOldPageRouteEntry = oldEntry;
            oldEntry.Route.UpdateSettings(newPage);
            newHistory.Add(oldEntry);
            newPagesBottom += 1;
            oldEntriesBottom += 1;
        }

        // Phase 2: scan from the top without syncing, collecting the pageless routes above the last match.
        var unattachedPagelessRoutes = new List<RouteEntry>();
        while (oldEntriesBottom <= oldEntriesTop && newPagesBottom <= newPagesTop)
        {
            RouteEntry oldEntry = _history[oldEntriesTop];
            if (!oldEntry.PageBased)
            {
                unattachedPagelessRoutes.Add(oldEntry);
                oldEntriesTop -= 1;
                continue;
            }

            if (!oldEntry.CanUpdateFrom(pages[newPagesTop]))
            {
                break;
            }

            if (unattachedPagelessRoutes.Count > 0)
            {
                pageRouteToPagelessRoutes.TryAdd(oldEntry, [.. unattachedPagelessRoutes]);
                unattachedPagelessRoutes.Clear();
            }

            oldEntriesTop -= 1;
            newPagesTop -= 1;
        }

        oldEntriesTop += unattachedPagelessRoutes.Count;

        // Phase 3a: index the old middle by page key.
        int oldEntriesBottomToScan = oldEntriesBottom;
        var pageKeyToOldEntry = new Dictionary<Key, RouteEntry>();
        var phantomEntries = new HashSet<RouteEntry>();
        while (oldEntriesBottomToScan <= oldEntriesTop)
        {
            RouteEntry oldEntry = _history[oldEntriesBottomToScan];
            oldEntriesBottomToScan += 1;
            if (!oldEntry.PageBased)
            {
                continue;
            }

            var page = (Page)oldEntry.Route.Settings;
            if (page.Key is null)
            {
                continue;
            }

            if (!oldEntry.WillBePresent)
            {
                phantomEntries.Add(oldEntry);
                continue;
            }

            pageKeyToOldEntry[page.Key] = oldEntry;
        }

        // Phase 3b: walk the new middle, reusing keyed entries and creating the rest.
        while (newPagesBottom <= newPagesTop)
        {
            Page nextPage = pages[newPagesBottom];
            newPagesBottom += 1;
            if (nextPage.Key is null
                || !pageKeyToOldEntry.TryGetValue(nextPage.Key, out RouteEntry? matchingEntry)
                || !matchingEntry.CanUpdateFrom(nextPage))
            {
                var newEntry = new RouteEntry(
                    nextPage.CreateRoute(Context),
                    RouteLifecycle.Staging,
                    pageBased: true);
                needsExplicitDecision = true;
                if (!ReferenceEquals(newEntry.Route.Settings, nextPage))
                {
                    throw new InvalidOperationException(
                        "The Settings of a page-based Route must return the Page object. Please set the settings "
                        + "to the Page in the Page.CreateRoute method.");
                }

                newHistory.Add(newEntry);
                continue;
            }

            pageKeyToOldEntry.Remove(nextPage.Key);
            matchingEntry.Route.UpdateSettings(nextPage);
            newHistory.Add(matchingEntry);
        }

        // Phase 3c: everything left in the old middle is leaving.
        var locationToExitingPageRoute = new RouteRecordMap<RouteTransitionRecord, RouteTransitionRecord>();
        while (oldEntriesBottom <= oldEntriesTop)
        {
            RouteEntry potentialEntryToRemove = _history[oldEntriesBottom];
            oldEntriesBottom += 1;
            if (!potentialEntryToRemove.PageBased)
            {
                AddPagelessRoute(pageRouteToPagelessRoutes, previousOldPageRouteEntry, potentialEntryToRemove);
                if (previousOldPageRouteEntry!.IsWaitingForExitingDecision && potentialEntryToRemove.WillBePresent)
                {
                    potentialEntryToRemove.MarkNeedsExitingDecision();
                }

                continue;
            }

            var potentialPageToRemove = (Page)potentialEntryToRemove.Route.Settings;
            if (potentialPageToRemove.Key is null
                || pageKeyToOldEntry.ContainsKey(potentialPageToRemove.Key)
                || phantomEntries.Contains(potentialEntryToRemove))
            {
                locationToExitingPageRoute[previousOldPageRouteEntry] = potentialEntryToRemove;
                if (potentialEntryToRemove.WillBePresent)
                {
                    potentialEntryToRemove.MarkNeedsExitingDecision();
                }
            }

            previousOldPageRouteEntry = potentialEntryToRemove;
        }

        newPagesTop = pages.Count - 1;
        oldEntriesTop = _history.Count - 1;

        // Phase 4: sync the top region skipped by phase 2.
        while (oldEntriesBottom <= oldEntriesTop && newPagesBottom <= newPagesTop)
        {
            RouteEntry oldEntry = _history[oldEntriesBottom];
            if (!oldEntry.PageBased)
            {
                AddPagelessRoute(pageRouteToPagelessRoutes, previousOldPageRouteEntry, oldEntry);
                oldEntriesBottom += 1;
                continue;
            }

            previousOldPageRouteEntry = oldEntry;
            oldEntry.Route.UpdateSettings(pages[newPagesBottom]);
            newHistory.Add(oldEntry);
            oldEntriesBottom += 1;
            newPagesBottom += 1;
        }

        // Phase 5: let the transition delegate merge entering and exiting routes.
        needsExplicitDecision = needsExplicitDecision || locationToExitingPageRoute.Count > 0;
        IReadOnlyList<RouteEntry> results = newHistory;
        if (needsExplicitDecision)
        {
            results = CurrentWidget.TransitionDelegate
                .Transition(
                    newHistory,
                    locationToExitingPageRoute,
                    pageRouteToPagelessRoutes.Project<RouteTransitionRecord, IReadOnlyList<RouteTransitionRecord>>())
                .Cast<RouteEntry>()
                .ToList();
        }

        // Phase 6: rebuild the history, re-attaching each page route's pageless routes.
        _history.Clear();
        if (pageRouteToPagelessRoutes.TryGetValue(null, out List<RouteEntry>? bottomPageless))
        {
            _history.AddRange(bottomPageless);
        }

        foreach (RouteEntry result in results)
        {
            _history.Add(result);
            if (pageRouteToPagelessRoutes.TryGetValue(result, out List<RouteEntry>? pageless))
            {
                _history.AddRange(pageless);
            }
        }

        _updatingPage = false;
        FlushWithLock();
    }

    private static void AddPagelessRoute(
        RouteRecordMap<RouteEntry, List<RouteEntry>> pageRouteToPagelessRoutes,
        RouteEntry? page,
        RouteEntry entry)
    {
        if (!pageRouteToPagelessRoutes.TryGetValue(page, out List<RouteEntry>? routes))
        {
            routes = [];
            pageRouteToPagelessRoutes[page] = routes;
        }

        routes.Add(entry);
    }

    private void CheckDuplicatedPageKeys()
    {
        var keys = new HashSet<Key>();
        foreach (Page page in CurrentWidget.Pages!)
        {
            if (page.Key is not null && !keys.Add(page.Key))
            {
                throw new InvalidOperationException($"The Navigator.Pages list contains the duplicate key {page.Key}.");
            }
        }
    }

    private void ValidatePagesApi()
    {
        if (!CurrentWidget.UsingPagesApi)
        {
            return;
        }

        if (CurrentWidget.Pages!.Count == 0)
        {
            throw new InvalidOperationException(
                "The Navigator.Pages must not be empty to use the Navigator.Pages API.");
        }

        if (CurrentWidget.OnDidRemovePage is null)
        {
            throw new InvalidOperationException(
                "OnDidRemovePage must be provided to use the Navigator.Pages API.");
        }
    }

    // -------------------------------------------------------------------------------------------------
    // The flush
    // -------------------------------------------------------------------------------------------------

    private void FlushWithLock(bool rearrangeOverlay = true)
    {
        SetState(() => FlushHistoryUpdates(rearrangeOverlay));
    }

    /// <summary>Flutter's <c>NavigatorState._flushHistoryUpdates</c>.</summary>
    internal void FlushHistoryUpdates(bool rearrangeOverlay = true)
    {
        if (_updatingPage)
        {
            return;
        }

        _flushingHistory = true;
        int index = _history.Count - 1;
        RouteEntry? next = null;
        RouteEntry? entry = index >= 0 ? _history[index] : null;
        RouteEntry? previous = index > 0 ? _history[index - 1] : null;
        bool canRemoveOrAdd = false;
        Route? poppedRoute = null;
        bool seenTopActiveRoute = false;
        var toBeDisposed = new List<RouteEntry>();

        while (index >= 0)
        {
            bool advance = true;
            switch (entry!.CurrentState)
            {
                case RouteLifecycle.Add:
                    entry.HandleAdd(this, GetRouteBefore(index - 1, RouteEntry.IsPresentPredicate)?.Route);
                    advance = false;
                    break;

                case RouteLifecycle.Adding:
                    if (canRemoveOrAdd || next is null)
                    {
                        entry.DidAdd(this, isNewFirst: next is null);
                        advance = false;
                    }

                    break;

                case RouteLifecycle.Push:
                case RouteLifecycle.PushReplace:
                case RouteLifecycle.Replace:
                    Route? previousPresentForPush = GetRouteBefore(index - 1, RouteEntry.IsPresentPredicate)?.Route;
                    if (entry.CurrentState == RouteLifecycle.Push)
                    {
                        _pendingHeroPush = (previousPresentForPush, entry.Route);
                    }

                    entry.HandlePush(
                        this,
                        isNewFirst: next is null,
                        previous: previous?.Route,
                        previousPresent: previousPresentForPush);
                    if (entry.CurrentState == RouteLifecycle.Idle)
                    {
                        advance = false;
                    }

                    break;

                case RouteLifecycle.Pushing:
                    if (!seenTopActiveRoute && poppedRoute is not null)
                    {
                        entry.HandleDidPopNext(poppedRoute);
                    }

                    seenTopActiveRoute = true;
                    break;

                case RouteLifecycle.Idle:
                    if (!seenTopActiveRoute && poppedRoute is not null)
                    {
                        entry.HandleDidPopNext(poppedRoute);
                    }

                    seenTopActiveRoute = true;
                    canRemoveOrAdd = true;
                    break;

                case RouteLifecycle.Pop:
                    Route? previousPresentForPop = GetRouteBefore(index, RouteEntry.WillBePresentPredicate)?.Route;
                    bool willFlyHeroes = !seenTopActiveRoute
                                         && previousPresentForPop is not null
                                         && _heroTransitionController.HasHeroes(entry.Route);
                    if (willFlyHeroes)
                    {
                        // Registered before the route pops, because a zero-duration route finalizes itself
                        // from inside `HandlePop`.
                        _heroDeferredRoutes.Add(entry.Route);
                    }

                    if (!entry.HandlePop(this, previousPresentForPop))
                    {
                        _heroDeferredRoutes.Remove(entry.Route);
                        advance = false;
                        break;
                    }

                    if (!seenTopActiveRoute)
                    {
                        if (poppedRoute is not null)
                        {
                            entry.HandleDidPopNext(poppedRoute);
                        }

                        poppedRoute = entry.Route;
                        _pendingHeroPop = (entry.Route, previousPresentForPop);
                    }

                    _observedRouteDeletions.Enqueue(
                        new NavigatorPopObservation(entry.Route, previousPresentForPop));
                    if (entry.CurrentState == RouteLifecycle.Dispose)
                    {
                        advance = false;
                        break;
                    }

                    canRemoveOrAdd = true;
                    break;

                case RouteLifecycle.Popping:
                    break;

                case RouteLifecycle.Complete:
                    entry.HandleComplete();
                    advance = false;
                    break;

                case RouteLifecycle.Remove:
                    if (!seenTopActiveRoute && entry.Route.Installed)
                    {
                        if (poppedRoute is not null)
                        {
                            entry.HandleDidPopNext(poppedRoute);
                        }

                        poppedRoute = null;
                    }

                    entry.HandleRemoval(this, GetRouteBefore(index, RouteEntry.WillBePresentPredicate)?.Route);
                    advance = false;
                    break;

                case RouteLifecycle.Removing:
                    if (!canRemoveOrAdd && next is not null)
                    {
                        break;
                    }

                    entry.CurrentState = RouteLifecycle.Dispose;
                    advance = false;
                    break;

                case RouteLifecycle.Dispose:
                    toBeDisposed.Add(_history[index]);
                    _history.RemoveAt(index);
                    entry = next;
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected route lifecycle {entry.CurrentState}.");
            }

            if (!advance)
            {
                continue;
            }

            index -= 1;
            next = entry;
            entry = previous;
            previous = index > 0 ? _history[index - 1] : null;
        }

        FlushObserverNotifications();
        FlushRouteAnnouncement();

        RouteEntry? lastEntry = LastRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate);
        if (lastEntry is not null && !ReferenceEquals(_lastTopmostRoute, lastEntry))
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.DidChangeTop(lastEntry.Route, _lastTopmostRoute?.Route);
            }
        }

        _lastTopmostRoute = lastEntry;

        if (CurrentWidget.ReportsRouteUpdateToEngine)
        {
            string? routeName = lastEntry?.Route.Settings.Name;
            if (routeName is not null && !string.Equals(routeName, _lastAnnouncedRouteName, StringComparison.Ordinal))
            {
                _lastAnnouncedRouteName = routeName;
            }
        }

        foreach (RouteEntry disposed in toBeDisposed)
        {
            disposed.Dispose(this);
        }

        if (rearrangeOverlay)
        {
            Overlay?.Rearrange(AllRouteOverlayEntries());
        }

        if (Bucket is not null)
        {
            _serializableHistory.Update(_history);
        }

        _flushingHistory = false;
        ResolvePendingHeroTransitions();
    }

    /// <summary>Flutter's <c>_flushObserverNotifications</c>: additions drain LIFO, deletions FIFO.</summary>
    private void FlushObserverNotifications()
    {
        if (_observers.Count == 0)
        {
            _observedRouteDeletions.Clear();
            _observedRouteAdditions.Clear();
            return;
        }

        while (_observedRouteAdditions.Count > 0)
        {
            NavigatorObservation observation = _observedRouteAdditions[^1];
            _observedRouteAdditions.RemoveAt(_observedRouteAdditions.Count - 1);
            foreach (var observer in _observers.ToArray())
            {
                observation.Notify(observer);
            }
        }

        while (_observedRouteDeletions.Count > 0)
        {
            NavigatorObservation observation = _observedRouteDeletions.Dequeue();
            foreach (var observer in _observers.ToArray())
            {
                observation.Notify(observer);
            }
        }
    }

    /// <summary>Flutter's <c>_flushRouteAnnouncement</c>.</summary>
    private void FlushRouteAnnouncement()
    {
        int index = _history.Count - 1;
        while (index >= 0)
        {
            RouteEntry entry = _history[index];
            if (!entry.SuitableForAnnouncement)
            {
                index -= 1;
                continue;
            }

            RouteEntry? next = GetRouteAfter(index + 1, RouteEntry.SuitableForTransitionAnimationPredicate);
            if (!ReferenceEquals(next?.Route, entry.LastAnnouncedNextRoute))
            {
                if (entry.ShouldAnnounceChangeToNext(next?.Route))
                {
                    entry.Route.DidChangeNext(next?.Route);
                }

                entry.LastAnnouncedNextRoute = next?.Route;
            }

            RouteEntry? previous = GetRouteBefore(index - 1, RouteEntry.SuitableForTransitionAnimationPredicate);
            if (!ReferenceEquals(previous?.Route, entry.LastAnnouncedPreviousRoute))
            {
                entry.Route.DidChangePrevious(previous?.Route);
                entry.LastAnnouncedPreviousRoute = previous?.Route;
            }

            index -= 1;
        }
    }

    // -------------------------------------------------------------------------------------------------
    // Entry helpers used by RouteEntry and Route
    // -------------------------------------------------------------------------------------------------

    internal void EnqueueRouteAddition(NavigatorObservation observation) => _observedRouteAdditions.Add(observation);

    internal void EnqueueRouteDeletion(NavigatorObservation observation) => _observedRouteDeletions.Enqueue(observation);

    internal void NotifyDidRemovePage(Page page) => CurrentWidget.OnDidRemovePage?.Invoke(page);

    internal void AddEntryWaitingForSubtreeDisposal(RouteEntry entry) => _entriesWaitingForSubtreeDisposal.Add(entry);

    internal bool RemoveEntryWaitingForSubtreeDisposal(RouteEntry entry) =>
        _entriesWaitingForSubtreeDisposal.Remove(entry);

    /// <summary>Moves a route out of <c>pushing</c> once its entering transition settled.</summary>
    internal void WhenPushSettles(RouteEntry entry)
    {
        if (entry.Route.PushSettlesImmediately)
        {
            entry.CurrentState = RouteLifecycle.Idle;
            return;
        }

        Action? handler = null;
        handler = () =>
        {
            entry.Route.PushSettled -= handler!;
            if (entry.CurrentState != RouteLifecycle.Pushing)
            {
                return;
            }

            entry.CurrentState = RouteLifecycle.Idle;
            if (Mounted)
            {
                FlushWithLock();
            }
        };

        entry.Route.PushSettled += handler;
    }

    internal RouteEntry? FirstRouteEntryWhereOrNull(Func<RouteEntry, bool> test)
    {
        foreach (RouteEntry entry in _history)
        {
            if (test(entry))
            {
                return entry;
            }
        }

        return null;
    }

    internal RouteEntry? LastRouteEntryWhereOrNull(Func<RouteEntry, bool> test)
    {
        RouteEntry? result = null;
        foreach (RouteEntry entry in _history)
        {
            if (test(entry))
            {
                result = entry;
            }
        }

        return result;
    }

    private int GetIndexBefore(int index, Func<RouteEntry, bool> test)
    {
        while (index >= 0 && !test(_history[index]))
        {
            index -= 1;
        }

        return index;
    }

    private RouteEntry? GetRouteBefore(int index, Func<RouteEntry, bool> test)
    {
        index = GetIndexBefore(index, test);
        return index >= 0 ? _history[index] : null;
    }

    private RouteEntry? GetRouteAfter(int index, Func<RouteEntry, bool> test)
    {
        while (index < _history.Count && !test(_history[index]))
        {
            index += 1;
        }

        return index < _history.Count ? _history[index] : null;
    }

    internal void NotifyRouteChanged()
    {
        if (Element.IsActive)
        {
            SetState(() => { });
        }
    }

    internal void FinalizeRoute(Route route)
    {
        RouteEntry? entry = _history.FirstOrDefault(candidate => ReferenceEquals(candidate.Route, route));
        if (entry is null || entry.CurrentState >= RouteLifecycle.Dispose)
        {
            return;
        }

        if (_heroDeferredRoutes.Contains(route))
        {
            // A hero flight still paints this route's shuttle; disposal waits for the flight to finish.
            return;
        }

        entry.Finalize();
        if (!_flushingHistory)
        {
            FlushWithLock(rearrangeOverlay: false);
        }
    }

    internal bool IsFirst(Route route) =>
        ReferenceEquals(FirstRouteEntryWhereOrNull(RouteEntry.IsPresentPredicate)?.Route, route);

    internal bool IsActive(Route route) =>
        FirstRouteEntryWhereOrNull(RouteEntry.IsRoutePredicate(route))?.IsPresent ?? false;

    internal bool HasActiveRouteBelow(Route route)
    {
        foreach (RouteEntry entry in _history)
        {
            if (ReferenceEquals(entry.Route, route))
            {
                return false;
            }

            if (entry.IsPresent)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The overlay entries of every route this navigator owns, ordered bottom-most first, mirroring
    /// Flutter's <c>_allRouteOverlayEntries</c>.
    /// </summary>
    private List<OverlayEntry> AllRouteOverlayEntries()
    {
        var entries = new List<OverlayEntry>();
        foreach (RouteEntry entry in _history)
        {
            entries.AddRange(entry.Route.OverlayEntries);
        }

        return entries;
    }

    private void ForcedDisposeAllRouteEntries()
    {
        _entriesWaitingForSubtreeDisposal.Clear();
        foreach (RouteEntry entry in _history.ToArray())
        {
            if (entry.CurrentState < RouteLifecycle.Disposed)
            {
                entry.ForcedDispose();
            }
        }

        _history.Clear();
        _observedRouteAdditions.Clear();
        _observedRouteDeletions.Clear();
        _lastTopmostRoute = null;
    }

    // -------------------------------------------------------------------------------------------------
    // Initial routes and named-route resolution
    // -------------------------------------------------------------------------------------------------

    private void PushInitialRoutes()
    {
        if (_history.Count > 0)
        {
            return;
        }

        if (CurrentWidget.UsingPagesApi)
        {
            foreach (Page page in CurrentWidget.Pages!)
            {
                _history.Add(new RouteEntry(page.CreateRoute(Context), RouteLifecycle.Add, pageBased: true));
            }
        }
        else
        {
            _history.AddRange(CreateInitialRouteEntries());
        }

        if (_history.Count == 0)
        {
            throw new InvalidOperationException("The Navigator could not generate any initial route.");
        }

        FlushWithLock();
    }

    /// <summary>
    /// Flutter's <c>onGenerateInitialRoutes</c> result, wrapped in entries that carry named restoration
    /// information so the initial stack survives a restoration.
    /// </summary>
    private List<RouteEntry> CreateInitialRouteEntries()
    {
        var entries = new List<RouteEntry>();
        foreach (Route initialRoute in ResolveInitialRoutes())
        {
            entries.Add(new RouteEntry(
                initialRoute,
                RouteLifecycle.Add,
                pageBased: false,
                restorationInformation: initialRoute.Settings.Name is { } name
                    ? RestorationInformation.Named(name, null, NextPagelessRestorationScopeId)
                    : null));
        }

        return entries;
    }

    private IReadOnlyList<Route> ResolveInitialRoutes()
    {
        if (CurrentWidget.InitialRoute != null)
        {
            return [CurrentWidget.InitialRoute];
        }

        if (CurrentWidget.InitialRouteData != null)
        {
            return [ResolveRouteData(CurrentWidget.InitialRouteData)];
        }

        string? routeName = CurrentWidget.InitialRouteName;
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new InvalidOperationException("Navigator requires either InitialRoute or InitialRouteName.");
        }

        if (CurrentWidget.OnGenerateInitialRoutes != null)
        {
            IReadOnlyList<Route> generated = CurrentWidget.OnGenerateInitialRoutes(this, routeName);
            if (generated.Count == 0)
            {
                throw new InvalidOperationException("onGenerateInitialRoutes must return at least one route.");
            }

            return generated;
        }

        if (!routeName.StartsWith('/') || routeName == Navigator.DefaultRouteName)
        {
            Route? directRoute = RouteNamed(routeName, arguments: null, allowNull: true);
            return directRoute != null
                ? [directRoute]
                : [ResolveDefaultInitialRoute()];
        }

        var routeNames = new List<string> { Navigator.DefaultRouteName };
        string[] segments = routeName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string currentPath = string.Empty;
        foreach (string segment in segments)
        {
            currentPath += "/" + segment;
            routeNames.Add(currentPath);
        }

        var routes = new List<Route>(routeNames.Count);
        foreach (string candidateName in routeNames)
        {
            Route? candidate = RouteNamed(candidateName, arguments: null, allowNull: true);
            if (candidate != null)
            {
                routes.Add(candidate);
                continue;
            }

            if (string.Equals(candidateName, routeName, StringComparison.Ordinal))
            {
                foreach (Route generatedRoute in routes)
                {
                    generatedRoute.Dispose();
                }

                return [ResolveDefaultInitialRoute()];
            }
        }

        return routes.Count > 0 ? routes : [ResolveDefaultInitialRoute()];
    }

    /// <summary>Flutter's <c>NavigatorState._routeNamed</c>.</summary>
    internal Route? RouteNamed(string routeName, object? arguments, bool allowNull)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new ArgumentException("routeName cannot be null or whitespace.", nameof(routeName));
        }

        var settings = new RouteSettings(Name: routeName, Arguments: arguments);
        Route? route = CurrentWidget.OnGenerateRoute?.Invoke(settings);
        if (route is null && !allowNull)
        {
            route = CurrentWidget.OnUnknownRoute?.Invoke(settings);
        }

        if (route is null && !allowNull)
        {
            throw new InvalidOperationException(
                $"Neither onGenerateRoute nor onUnknownRoute generated route '{routeName}'.");
        }

        return route;
    }

    private Route ResolveDefaultInitialRoute()
    {
        return RouteNamed(Navigator.DefaultRouteName, arguments: null, allowNull: true)
               ?? RouteNamed(Navigator.DefaultRouteName, arguments: null, allowNull: false)!;
    }

    private Route ResolveRouteData(RouteData routeData) =>
        RouteNamed(routeData.Name, routeData, allowNull: false)!;

    private Route ResolveRouteLocation(string routeName, object? arguments)
    {
        if (routeName.IndexOf('?') >= 0 || routeName.Contains("://", StringComparison.Ordinal))
        {
            return ResolveRouteData(RouteData.FromLocation(routeName, arguments));
        }

        return RouteNamed(routeName, arguments, allowNull: false)!;
    }

    private bool HandleBackButton() => MaybePop();

    private void ScheduleNavigationNotification(bool canHandlePop)
    {
        if (_lastCanHandlePop == canHandlePop)
        {
            return;
        }

        _lastCanHandlePop = canHandlePop;
        if (_navigationNotificationPending)
        {
            return;
        }

        _navigationNotificationPending = true;
        Scheduler.AddPostFrameCallback(_ =>
        {
            _navigationNotificationPending = false;
            if (!Mounted)
            {
                return;
            }

            new NavigationNotification(_lastCanHandlePop ?? false).Dispatch(Context);
        });
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
}
