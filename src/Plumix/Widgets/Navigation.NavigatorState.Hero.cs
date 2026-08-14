// Dart parity source: flutter/packages/flutter/lib/src/widgets/heroes.dart

using Plumix.Foundation;

namespace Plumix.Widgets;

/// <summary>
/// The navigator's hero-flight choreography. Flutter drives hero flights from a `HeroController`
/// registered as a <see cref="NavigatorObserver"/>; Plumix keeps the controller on the navigator and
/// starts a flight from the push/pop branches of the history flush.
/// </summary>
public sealed partial class NavigatorState
{
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

    private void TryStartHeroTransition(
        Route? fromRoute,
        Route? toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete,
        bool isUserGestureTransition)
    {
        if (fromRoute == null || toRoute == null)
        {
            return;
        }

        if (TryDivertHeroTransition(fromRoute, toRoute, direction, detachFromRouteOnComplete, isUserGestureTransition))
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
            detachFromRouteOnComplete: detachFromRouteOnComplete,
            isUserGestureTransition: isUserGestureTransition);
        SuspendRouteOpacityForFlight(fromRoute);
        SuspendRouteOpacityForFlight(toRoute);

        Scheduler.AddPostFrameCallback(_ => ResolvePendingHeroTransition());
    }

    private bool TryDivertHeroTransition(
        Route fromRoute,
        Route toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete,
        bool isUserGestureTransition)
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
            detachFromRouteOnComplete: detachFromRouteOnComplete,
            isUserGestureTransition: isUserGestureTransition);
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
        var flights = _heroTransitionController.CreateFlights(
            session.FromRoute,
            session.ToRoute,
            session.IsUserGestureTransition);
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
            InsertHeroFlightEntry();
            _heroFlightController.Forward(from: 0.0);
        });
    }

    private void InsertHeroFlightEntry()
    {
        if (_heroFlightEntry is not null)
        {
            return;
        }

        var entry = new OverlayEntry(BuildHeroFlightEntry);
        _heroFlightEntry = entry;
        Overlay?.Insert(entry);
    }

    private Widget BuildHeroFlightEntry(BuildContext context)
    {
        HeroTransitionSession? session = _heroTransitionSession;
        IReadOnlyList<HeroFlightManifest> flights = _heroTransitionController.ActiveFlights;
        if (session is null || flights.Count == 0)
        {
            return new SizedBox();
        }

        return BuildHeroFlightOverlay(
            flights,
            _heroFlightController.Evaluate(),
            isPushTransition: session.Direction == HeroTransitionDirection.Push);
    }

    private void RemoveHeroFlightEntry()
    {
        OverlayEntry? entry = _heroFlightEntry;
        _heroFlightEntry = null;
        if (entry is null)
        {
            return;
        }

        if (entry.Owner is not null)
        {
            entry.Remove();
        }

        entry.Dispose();
    }

    private static void SuspendRouteOpacityForFlight(Route route)
    {
        if (route is TransitionRoute transitionRoute)
        {
            transitionRoute.SuspendEntryOpacityForFlight();
        }
    }

    private static void RestoreRouteOpacityAfterFlight(Route route)
    {
        if (route is TransitionRoute transitionRoute)
        {
            transitionRoute.RestoreEntryOpacityAfterFlight();
        }
    }

    private void HandleHeroFlightTick()
    {
        if (_heroTransitionSession == null || _heroTransitionController.ActiveFlights.Count == 0 || !Element.IsActive)
        {
            return;
        }

        _heroFlightEntry?.MarkNeedsBuild();
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
        RemoveHeroFlightEntry();

        var session = _heroTransitionSession;
        _heroTransitionSession = null;
        if (session != null)
        {
            RestoreRouteOpacityAfterFlight(session.FromRoute);
            RestoreRouteOpacityAfterFlight(session.ToRoute);
        }

        if (!disposeDetachedRoute || session == null || !session.DetachFromRouteOnComplete)
        {
            return;
        }

        _heroDeferredRoutes.Remove(session.FromRoute);
        FinalizeRoute(session.FromRoute);
    }

    /// <summary>
    /// Starts the flight recorded by the push/pop branches of <see cref="FlushHistoryUpdates"/>. Flutter's
    /// <c>HeroController</c> does the same work from <c>didPush</c>/<c>didPop</c>.
    /// </summary>
    private void ResolvePendingHeroTransitions()
    {
        (Route? From, Route To)? push = _pendingHeroPush;
        (Route From, Route? To)? pop = _pendingHeroPop;
        _pendingHeroPush = null;
        _pendingHeroPop = null;

        if (pop is { } popped)
        {
            TryStartHeroTransition(
                fromRoute: popped.From,
                toRoute: popped.To,
                direction: HeroTransitionDirection.Pop,
                detachFromRouteOnComplete: true,
                isUserGestureTransition: UserGestureInProgress);
            return;
        }

        if (push is { } pushed)
        {
            TryStartHeroTransition(
                fromRoute: pushed.From,
                toRoute: pushed.To,
                direction: HeroTransitionDirection.Push,
                detachFromRouteOnComplete: false,
                isUserGestureTransition: false);
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
            bool detachFromRouteOnComplete,
            bool isUserGestureTransition)
        {
            FromRoute = fromRoute;
            ToRoute = toRoute;
            Direction = direction;
            DetachFromRouteOnComplete = detachFromRouteOnComplete;
            IsUserGestureTransition = isUserGestureTransition;
        }

        public Route FromRoute { get; }

        public Route ToRoute { get; }

        public HeroTransitionDirection Direction { get; }

        public bool DetachFromRouteOnComplete { get; }

        public bool IsUserGestureTransition { get; }
    }
}
