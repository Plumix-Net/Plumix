using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/heroes.dart

namespace Plumix.Widgets;

/// <summary>
/// Dart's `CreateRectTween`. Plumix's <see cref="Rect"/> is a value type, so the endpoints are
/// non-nullable where Dart uses `Rect?`.
/// </summary>
public delegate Tween<Rect> CreateRectTween(Rect begin, Rect end);

/// <summary>Dart's `HeroPlaceholderBuilder`.</summary>
public delegate Widget HeroPlaceholderBuilder(BuildContext context, Size heroSize, Widget child);

/// <summary>Dart's `HeroFlightShuttleBuilder`.</summary>
public delegate Widget HeroFlightShuttleBuilder(
    BuildContext flightContext,
    Animation<double> animation,
    HeroFlightDirection flightDirection,
    BuildContext fromHeroContext,
    BuildContext toHeroContext);

/// <summary>Dart's `_OnFlightEnded`.</summary>
internal delegate void OnFlightEnded(HeroFlight flight);

/// <summary>The direction in which a hero flies during a route transition.</summary>
public enum HeroFlightDirection
{
    /// <summary>Dart's `HeroFlightDirection.push`: the animation runs 0 -> 1.</summary>
    Push,

    /// <summary>Dart's `HeroFlightDirection.pop`: the animation runs 1 -> 0.</summary>
    Pop,
}

/// <summary>
/// A widget that marks its child as a candidate for hero animations. Dart's `Hero`.
/// </summary>
public sealed class Hero : StatefulWidget
{
    public Hero(
        object tag,
        Widget child,
        Key? key = null,
        CreateRectTween? createRectTween = null,
        HeroFlightShuttleBuilder? flightShuttleBuilder = null,
        HeroPlaceholderBuilder? placeholderBuilder = null,
        bool transitionOnUserGestures = false,
        Curve? curve = null,
        Curve? reverseCurve = null) : base(key)
    {
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        CreateRectTween = createRectTween;
        FlightShuttleBuilder = flightShuttleBuilder;
        PlaceholderBuilder = placeholderBuilder;
        TransitionOnUserGestures = transitionOnUserGestures;
        Curve = curve ?? Curves.FastOutSlowIn;
        ReverseCurve = reverseCurve;
    }

    public object Tag { get; }

    public CreateRectTween? CreateRectTween { get; }

    public Widget Child { get; }

    public HeroFlightShuttleBuilder? FlightShuttleBuilder { get; }

    public HeroPlaceholderBuilder? PlaceholderBuilder { get; }

    public bool TransitionOnUserGestures { get; }

    /// <summary>The curve of the flight. Dart default: `Curves.fastOutSlowIn`.</summary>
    public Curve Curve { get; }

    /// <summary>The curve of a reversed flight; <see langword="null"/> means <see cref="Curve"/> flipped.</summary>
    public Curve? ReverseCurve { get; }

    public override State CreateState() => new HeroState();

    /// <summary>Dart's `Hero._allHeroesFor`.</summary>
    internal static Dictionary<object, HeroState> AllHeroesFor(
        BuildContext context,
        bool isUserGestureTransition,
        NavigatorState navigator)
    {
        var result = new Dictionary<object, HeroState>();

        void InviteHero(StatefulElement hero, object tag)
        {
            if (result.ContainsKey(tag))
            {
                throw new InvalidOperationException(
                    "There are multiple heroes that share the same tag within a subtree.\n"
                    + "Within each subtree for which heroes are to be animated (i.e. a PageRoute subtree), "
                    + "each Hero must have a unique non-null tag.\n"
                    + $"In this case, multiple heroes had the following tag: {tag}");
            }

            var heroWidget = (Hero)hero.Widget;
            var heroState = (HeroState)hero.State;
            if (!isUserGestureTransition || heroWidget.TransitionOnUserGestures)
            {
                result[tag] = heroState;
            }
            else
            {
                // If transition is not allowed, we need to make sure hero is not hidden. A hero can be
                // hidden previously due to hero transition.
                heroState.EndFlight();
            }
        }

        void Visitor(Element element)
        {
            Widget widget = element.Widget;
            if (widget is Hero hero)
            {
                var heroElement = (StatefulElement)element;
                object tag = hero.Tag;
                var heroContext = new BuildContext(heroElement);
                if (ReferenceEquals(Navigator.MaybeOf(heroContext), navigator))
                {
                    InviteHero(heroElement, tag);
                }
                else
                {
                    ModalRoute? heroRoute = ModalRoute.MaybeOf(heroContext);
                    if (heroRoute is PageRoute { IsCurrent: true })
                    {
                        InviteHero(heroElement, tag);
                    }
                }
            }
            else if (widget is HeroMode { Enabled: false })
            {
                return;
            }

            element.VisitChildren(Visitor);
        }

        context.VisitChildElements(Visitor);
        return result;
    }
}

/// <summary>Dart's `_HeroState`.</summary>
internal sealed class HeroState : State
{
    private readonly GlobalKey _key;
    private Size? _placeholderSize;
    private bool _shouldIncludeChild = true;

    public HeroState()
    {
        // Identity-based key: a label-based GlobalKey is a record and would compare equal across heroes.
        _key = new GlobalObjectKey<State>(this);
    }

    internal Hero CurrentWidget => (Hero)StateWidget;

    /// <summary>
    /// Dart's `_HeroState.startFlight`. Called when the hero enters a flight; the hero is replaced by a
    /// placeholder of the same size so the surrounding layout does not move.
    /// </summary>
    /// <param name="shouldIncludedChildInPlaceholder">
    /// Dart's `shouldIncludedChildInPlaceholder` (upstream spelling kept).
    /// </param>
    internal void StartFlight(bool shouldIncludedChildInPlaceholder = false)
    {
        _shouldIncludeChild = shouldIncludedChildInPlaceholder;
        if (!Mounted || Context.FindRenderObject() is not RenderBox { HasSize: true } box)
        {
            return;
        }

        SetState(() => _placeholderSize = box.Size);
    }

    /// <summary>Dart's `_HeroState.endFlight`. Safe to call even when the hero is not in flight.</summary>
    internal void EndFlight(bool keepPlaceholder = false)
    {
        if (keepPlaceholder || _placeholderSize is null)
        {
            return;
        }

        _placeholderSize = null;
        if (Mounted)
        {
            // Tell the widget to rebuild if it's mounted. _placeholderSize has already been updated.
            SetState(static () => { });
        }
    }

    public override Widget Build(BuildContext context)
    {
        EnsureNoHeroAncestor();

        bool showPlaceholder = _placeholderSize is not null;

        if (showPlaceholder && CurrentWidget.PlaceholderBuilder is not null)
        {
            return CurrentWidget.PlaceholderBuilder(context, _placeholderSize!.Value, CurrentWidget.Child);
        }

        if (showPlaceholder && !_shouldIncludeChild)
        {
            return new SizedBox(width: _placeholderSize!.Value.Width, height: _placeholderSize!.Value.Height);
        }

        return new SizedBox(
            width: _placeholderSize?.Width,
            height: _placeholderSize?.Height,
            child: new Offstage(
                offstage: showPlaceholder,
                child: new TickerMode(
                    enabled: !showPlaceholder,
                    child: new KeyedSubtree(key: _key, child: CurrentWidget.Child))));
    }

    private void EnsureNoHeroAncestor()
    {
        for (Element? ancestor = Element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor.Widget is Hero)
            {
                throw new InvalidOperationException(
                    "A Hero widget cannot be the descendant of another Hero widget.");
            }
        }
    }
}

/// <summary>Dart's `_HeroFlightManifest`: everything needed to run one flight.</summary>
internal sealed class HeroFlightManifest : IDisposable
{
    private CurvedAnimation? _animation;
    private Rect? _fromHeroLocation;
    private Rect? _toHeroLocation;

    public HeroFlightManifest(
        HeroFlightDirection type,
        OverlayState overlay,
        Size navigatorSize,
        PageRoute fromRoute,
        PageRoute toRoute,
        HeroState fromHero,
        HeroState toHero,
        CreateRectTween? createRectTween,
        HeroFlightShuttleBuilder shuttleBuilder,
        bool isUserGestureTransition,
        bool isDiverted)
    {
        if (!Equals(fromHero.CurrentWidget.Tag, toHero.CurrentWidget.Tag))
        {
            throw new ArgumentException("Hero flight endpoints must share the same tag.", nameof(toHero));
        }

        Type = type;
        Overlay = overlay;
        NavigatorSize = navigatorSize;
        FromRoute = fromRoute;
        ToRoute = toRoute;
        FromHero = fromHero;
        ToHero = toHero;
        CreateRectTween = createRectTween;
        ShuttleBuilder = shuttleBuilder;
        IsUserGestureTransition = isUserGestureTransition;
        IsDiverted = isDiverted;
    }

    public HeroFlightDirection Type { get; }

    public OverlayState Overlay { get; }

    public Size NavigatorSize { get; }

    public PageRoute FromRoute { get; }

    public PageRoute ToRoute { get; }

    public HeroState FromHero { get; }

    public HeroState ToHero { get; }

    public CreateRectTween? CreateRectTween { get; }

    public HeroFlightShuttleBuilder ShuttleBuilder { get; }

    public bool IsUserGestureTransition { get; }

    public bool IsDiverted { get; }

    public object Tag => FromHero.CurrentWidget.Tag;

    public Animation<double> Animation
    {
        get
        {
            if (_animation is not null)
            {
                return _animation;
            }

            Curve curve;
            Curve reverseCurve;
            Animation<double> parent;
            switch (Type)
            {
                case HeroFlightDirection.Push:
                    parent = ToRoute.Animation;
                    curve = ToHero.CurrentWidget.Curve;
                    reverseCurve = ToHero.CurrentWidget.ReverseCurve ?? Curves.Flipped(curve);
                    break;
                default:
                    parent = FromRoute.Animation;
                    curve = FromHero.CurrentWidget.Curve;
                    reverseCurve = FromHero.CurrentWidget.ReverseCurve ?? Curves.Flipped(curve);
                    break;
            }

            _animation = new CurvedAnimation(
                parent: parent,
                curve: curve,
                reverseCurve: IsDiverted ? null : reverseCurve);
            return _animation;
        }
    }

    /// <summary>The bounding box of <see cref="FromHero"/> in <see cref="FromRoute"/>'s coordinate space.</summary>
    public Rect FromHeroLocation =>
        _fromHeroLocation ??= BoundingBoxFor(FromHero.Context, FromRoute.SubtreeContext);

    /// <summary>The bounding box of <see cref="ToHero"/> in <see cref="ToRoute"/>'s coordinate space.</summary>
    public Rect ToHeroLocation =>
        _toHeroLocation ??= BoundingBoxFor(ToHero.Context, ToRoute.SubtreeContext);

    /// <summary>
    /// Whether both endpoints are measurable. A diverted flight only needs the destination, because it
    /// continues from wherever the previous flight had reached.
    /// </summary>
    public bool IsValid => IsFinite(ToHeroLocation) && (IsDiverted || IsFinite(FromHeroLocation));

    public void Dispose() => _animation?.Dispose();

    public Tween<Rect> CreateHeroRectTween(Rect begin, Rect end)
    {
        CreateRectTween? createRectTween = ToHero.CurrentWidget.CreateRectTween ?? CreateRectTween;
        return createRectTween?.Invoke(begin, end) ?? new RectTween(begin: begin, end: end);
    }

    public override string ToString()
    {
        return $"HeroFlightManifest({Type} tag: {Tag} from route: {FromRoute.Settings} "
            + $"to route: {ToRoute.Settings} with hero: {FromHero} to {ToHero})"
            + (IsValid ? string.Empty : ", INVALID");
    }

    internal static bool IsFinite(Rect rect)
    {
        return double.IsFinite(rect.X)
               && double.IsFinite(rect.Y)
               && double.IsFinite(rect.Width)
               && double.IsFinite(rect.Height);
    }

    /// <summary>
    /// Dart's `_HeroFlightManifest._boundingBoxFor`. Dart asserts the box exists and has a finite size;
    /// Plumix has no assert-stripped build, so an unmeasurable hero yields a non-finite rect and
    /// <see cref="IsValid"/> rejects the flight instead of throwing.
    /// </summary>
    private static Rect BoundingBoxFor(BuildContext context, BuildContext? ancestorContext)
    {
        if (context.FindRenderObject() is not RenderBox { HasSize: true } box)
        {
            return new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
        }

        return MatrixUtils.TransformRect(
            box.GetTransformTo(ancestorContext?.FindRenderObject()),
            new Rect(new Point(0, 0), box.Size));
    }
}

/// <summary>Dart's `_HeroFlight`: one hero in motion, painted by an overlay entry.</summary>
internal sealed class HeroFlight : IDisposable
{
    private static readonly Animatable<double> ReverseTweenValue = new DoubleTween(begin: 1.0, end: 0.0);

    private readonly OnFlightEnded _onFlightEnded;
    private readonly ProxyAnimation _proxyAnimation;
    private Animation<double> _heroOpacity = new ConstantAnimation<double>(1.0, AnimationStatus.Completed);
    private HeroFlightManifest? _manifest;
    private bool _aborted;
    private bool _scheduledPerformAnimationUpdate;

    public HeroFlight(OnFlightEnded onFlightEnded)
    {
        _onFlightEnded = onFlightEnded;
        _proxyAnimation = new ProxyAnimation();
        _proxyAnimation.AddStatusListener(HandleAnimationUpdate);
    }

    public Tween<Rect> HeroRectTween { get; private set; } = new RectTween();

    public Widget? Shuttle { get; private set; }

    public OverlayEntry? OverlayEntry { get; private set; }

    public HeroFlightManifest Manifest => _manifest!;

    internal ProxyAnimation ProxyAnimation => _proxyAnimation;

    private HeroFlightManifest SetManifest
    {
        set
        {
            _manifest?.Dispose();
            _manifest = value;
        }
    }

    /// <summary>Dart's `_HeroFlight.start`.</summary>
    public void Start(HeroFlightManifest initialManifest)
    {
        SetManifest = initialManifest;

        bool shouldIncludeChildInPlaceholder;
        switch (Manifest.Type)
        {
            case HeroFlightDirection.Pop:
                _proxyAnimation.Parent = new ReverseAnimation(Manifest.Animation);
                shouldIncludeChildInPlaceholder = false;
                break;
            default:
                _proxyAnimation.Parent = Manifest.Animation;
                shouldIncludeChildInPlaceholder = true;
                break;
        }

        HeroRectTween = Manifest.CreateHeroRectTween(Manifest.FromHeroLocation, Manifest.ToHeroLocation);
        Manifest.FromHero.StartFlight(shouldIncludedChildInPlaceholder: shouldIncludeChildInPlaceholder);
        Manifest.ToHero.StartFlight();
        OverlayEntry = new OverlayEntry(BuildOverlay);
        Manifest.Overlay.Insert(OverlayEntry);
        _proxyAnimation.AddListener(OnTick);
    }

    /// <summary>Dart's `_HeroFlight.divert`: retarget a flight that is already in the air.</summary>
    public void Divert(HeroFlightManifest newManifest)
    {
        if (Manifest.Type == HeroFlightDirection.Push && newManifest.Type == HeroFlightDirection.Pop)
        {
            // A push flight was interrupted by a pop. The same heroRect tween is used in reverse, so the
            // pop flight path is the same (in reverse) as the push flight path.
            _proxyAnimation.Parent = new ReverseAnimation(newManifest.Animation);
            HeroRectTween = new ReverseTween<Rect>(HeroRectTween);
        }
        else if (Manifest.Type == HeroFlightDirection.Pop && newManifest.Type == HeroFlightDirection.Push)
        {
            // A pop flight was interrupted by a push.
            _proxyAnimation.Parent = newManifest.Animation.Drive(
                new DoubleTween(begin: Manifest.Animation.Value, end: 1.0));
            if (!ReferenceEquals(Manifest.FromHero, newManifest.ToHero))
            {
                Manifest.FromHero.EndFlight(keepPlaceholder: true);
                newManifest.ToHero.StartFlight();
                HeroRectTween = Manifest.CreateHeroRectTween(
                    HeroRectTween.GetEndValue(),
                    newManifest.ToHeroLocation);
            }
            else
            {
                HeroRectTween = Manifest.CreateHeroRectTween(
                    HeroRectTween.GetEndValue(),
                    HeroRectTween.GetBeginValue());
            }
        }
        else
        {
            // A push or pop flight is heading to a new route, i.e. the same type of flight.
            HeroRectTween = Manifest.CreateHeroRectTween(
                HeroRectTween.Evaluate(_proxyAnimation.Value),
                newManifest.ToHeroLocation);
            Shuttle = null;

            _proxyAnimation.Parent = newManifest.Type == HeroFlightDirection.Pop
                ? new ReverseAnimation(newManifest.Animation)
                : newManifest.Animation;

            Manifest.FromHero.EndFlight(keepPlaceholder: true);
            Manifest.ToHero.EndFlight(keepPlaceholder: true);

            // Let the heroes in each of the routes rebuild with their placeholders.
            newManifest.FromHero.StartFlight(
                shouldIncludedChildInPlaceholder: newManifest.Type == HeroFlightDirection.Push);
            newManifest.ToHero.StartFlight();

            // Let the transition overlay on top of the routes also rebuild since we cleared the old shuttle.
            OverlayEntry?.MarkNeedsBuild();
        }

        SetManifest = newManifest;
    }

    /// <summary>Dart's `_HeroFlight.abort`: the flight fades out on the next tick.</summary>
    public void Abort() => _aborted = true;

    public void Dispose()
    {
        if (OverlayEntry is not null)
        {
            RemoveOverlayEntry();
            _proxyAnimation.Parent = null;
            _proxyAnimation.RemoveListener(OnTick);
            _proxyAnimation.RemoveStatusListener(HandleAnimationUpdate);
        }

        _manifest?.Dispose();
    }

    public override string ToString()
    {
        return $"HeroFlight(for: {Manifest.Tag}, from: {Manifest.FromRoute.Settings}, "
            + $"to: {Manifest.ToRoute.Settings} {_proxyAnimation.Parent})";
    }

    /// <summary>Dart's `_HeroFlight._handleAnimationUpdate`.</summary>
    internal void HandleAnimationUpdate(AnimationStatus status)
    {
        NavigatorState? navigator = Manifest.FromRoute.Navigator;
        if (navigator?.UserGestureInProgress != true)
        {
            PerformAnimationUpdate(status);
            return;
        }

        if (_scheduledPerformAnimationUpdate)
        {
            return;
        }

        void DelayedPerformAnimationUpdate()
        {
            if (navigator.UserGestureInProgress)
            {
                return;
            }

            _scheduledPerformAnimationUpdate = false;
            navigator.UserGestureInProgressNotifier.RemoveListener(DelayedPerformAnimationUpdate);
            PerformAnimationUpdate(_proxyAnimation.Status);
        }

        _scheduledPerformAnimationUpdate = true;
        navigator.UserGestureInProgressNotifier.AddListener(DelayedPerformAnimationUpdate);
    }

    private void PerformAnimationUpdate(AnimationStatus status)
    {
        if (status is AnimationStatus.Forward or AnimationStatus.Reverse)
        {
            return;
        }

        _proxyAnimation.Parent = null;

        RemoveOverlayEntry();
        Manifest.FromHero.EndFlight(keepPlaceholder: status == AnimationStatus.Completed);
        Manifest.ToHero.EndFlight(keepPlaceholder: status == AnimationStatus.Dismissed);
        _onFlightEnded(this);
        _proxyAnimation.RemoveListener(OnTick);
    }

    private void RemoveOverlayEntry()
    {
        OverlayEntry? entry = OverlayEntry;
        OverlayEntry = null;
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

    /// <summary>Dart's `_HeroFlight._buildOverlay`.</summary>
    private Widget BuildOverlay(BuildContext context)
    {
        Shuttle ??= Manifest.ShuttleBuilder(
            context,
            Manifest.Animation,
            Manifest.Type,
            Manifest.FromHero.Context,
            Manifest.ToHero.Context);

        return new AnimatedBuilder(
            animation: _proxyAnimation,
            child: Shuttle,
            builder: (BuildContext _, Widget? child) =>
            {
                Rect rect = HeroRectTween.Evaluate(_proxyAnimation.Value);
                Rendering.RelativeRect offsets = Rendering.RelativeRect.FromSize(
                    rect,
                    Manifest.NavigatorSize);
                return new Positioned(
                    top: offsets.Top,
                    right: offsets.Right,
                    bottom: offsets.Bottom,
                    left: offsets.Left,
                    child: new IgnorePointer(
                        child: new FadeTransition(opacity: _heroOpacity, child: child)));
            });
    }

    /// <summary>Dart's `_HeroFlight.onTick`: retargets the flight while the destination hero moves.</summary>
    private void OnTick()
    {
        RenderBox? toHeroBox = !_aborted && Manifest.ToHero.Mounted
            ? Manifest.ToHero.Context.FindRenderObject() as RenderBox
            : null;
        Point? toHeroOrigin = toHeroBox is { Attached: true, HasSize: true }
            ? toHeroBox.LocalToGlobal(
                new Point(0, 0),
                Manifest.ToRoute.SubtreeContext?.FindRenderObject() as RenderBox)
            : null;

        if (toHeroOrigin is { } origin && double.IsFinite(origin.X) && double.IsFinite(origin.Y))
        {
            Rect end = HeroRectTween.GetEndValue();
            if (origin != end.TopLeft)
            {
                var heroRectEnd = new Rect(origin, end.Size);
                HeroRectTween = Manifest.CreateHeroRectTween(HeroRectTween.GetBeginValue(), heroRectEnd);
            }
        }
        else if (_heroOpacity.Status == AnimationStatus.Completed)
        {
            // The toHero no longer exists or it's no longer the flight's destination. Continue flying
            // while fading out.
            _heroOpacity = _proxyAnimation.Drive(
                ReverseTweenValue.Chain(new CurveTween(Curves.Interval(_proxyAnimation.Value, 1.0))));
        }

        // Update _aborted for the next animation tick.
        _aborted = toHeroOrigin is not { } next
                   || !double.IsFinite(next.X)
                   || !double.IsFinite(next.Y);
    }
}

/// <summary>
/// Dart's `HeroController`: a <see cref="NavigatorObserver"/> that runs hero flights between routes.
/// </summary>
public sealed class HeroController : NavigatorObserver, IDisposable
{
    private readonly Dictionary<object, HeroFlight> _flights = [];

    public HeroController(CreateRectTween? createRectTween = null)
    {
        CreateRectTween = createRectTween;
    }

    /// <summary>Used to create <see cref="Rect"/> tweens for heroes that do not supply their own.</summary>
    public CreateRectTween? CreateRectTween { get; }

    /// <summary>C#-only: lets the app widgets' tests assert the controller was disposed with its state.</summary>
    internal bool IsDisposed { get; private set; }

    public override void DidChangeTop(Route topRoute, Route? previousTopRoute)
    {
        if (previousTopRoute is null || Navigator is null)
        {
            return;
        }

        // Don't trigger another flight when a pop is committed as a user gesture back swipe is snapped.
        if (!Navigator.UserGestureInProgress)
        {
            MaybeStartHeroTransition(
                fromRoute: previousTopRoute,
                toRoute: topRoute,
                isUserGestureTransition: false);
        }
    }

    public override void DidStartUserGesture(Route route, Route? previousRoute)
    {
        MaybeStartHeroTransition(
            fromRoute: route,
            toRoute: previousRoute,
            isUserGestureTransition: true);
    }

    public override void DidStopUserGesture()
    {
        if (Navigator?.UserGestureInProgress != false)
        {
            return;
        }

        static bool IsInvalidFlight(HeroFlight flight)
        {
            return flight.Manifest.IsUserGestureTransition
                   && flight.Manifest.Type == HeroFlightDirection.Pop
                   && flight.ProxyAnimation.Status == AnimationStatus.Dismissed;
        }

        HeroFlight[] invalidFlights = _flights.Values.Where(IsInvalidFlight).ToArray();

        // Treat these invalidated flights as dismissed.
        foreach (HeroFlight flight in invalidFlights)
        {
            flight.HandleAnimationUpdate(AnimationStatus.Dismissed);
        }
    }

    public void Dispose()
    {
        foreach (HeroFlight flight in _flights.Values)
        {
            flight.Dispose();
        }

        IsDisposed = true;
        Navigator = null;
    }

    /// <summary>Dart's `HeroController._maybeStartHeroTransition`.</summary>
    private void MaybeStartHeroTransition(Route? fromRoute, Route? toRoute, bool isUserGestureTransition)
    {
        if (ReferenceEquals(toRoute, fromRoute) || toRoute is not PageRoute to || fromRoute is not PageRoute from)
        {
            return;
        }

        if (!to.HasAnimation || !from.HasAnimation)
        {
            return;
        }

        AnimationStatus oldStatus = from.Animation.Status;
        AnimationStatus newStatus = to.Animation.Status;
        HeroFlightDirection? flightType;
        if (isUserGestureTransition || oldStatus == AnimationStatus.Reverse)
        {
            flightType = HeroFlightDirection.Pop;
        }
        else if (newStatus == AnimationStatus.Forward)
        {
            flightType = HeroFlightDirection.Push;
        }
        else
        {
            flightType = null;
        }

        // A user gesture may have already completed the pop, or we might be the initial route.
        switch (flightType)
        {
            case HeroFlightDirection.Pop when from.Animation.Value == 0.0:
            case HeroFlightDirection.Push when to.Animation.Value == 1.0:
                return;
        }

        var toRouteRenderBox = to.SubtreeContext?.FindRenderObject() as RenderBox;
        bool hasValidSize = toRouteRenderBox is { HasSize: true }
                            && double.IsFinite(toRouteRenderBox.Size.Width)
                            && double.IsFinite(toRouteRenderBox.Size.Height);
        if (isUserGestureTransition
            && flightType == HeroFlightDirection.Pop
            && to.MaintainState
            && hasValidSize)
        {
            StartHeroTransition(from, to, flightType, isUserGestureTransition);
        }
        else
        {
            // Putting a route offstage changes its animation value to 1.0. Once this frame completes,
            // we'll know where the heroes in the `to` route are going to end up, and the `to` route will
            // go back onstage.
            to.Offstage = to.Animation.Value == 0.0;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (from.Navigator is null || to.Navigator is null)
                {
                    return;
                }

                StartHeroTransition(from, to, flightType, isUserGestureTransition);
            });
        }
    }

    /// <summary>Dart's `HeroController._startHeroTransition`.</summary>
    private void StartHeroTransition(
        PageRoute from,
        PageRoute to,
        HeroFlightDirection? flightType,
        bool isUserGestureTransition)
    {
        to.Offstage = false;

        NavigatorState? navigator = Navigator;
        OverlayState? overlay = navigator?.Overlay;
        if (navigator is null || overlay is null)
        {
            return;
        }

        if (navigator.Context.FindRenderObject() is not RenderBox { HasSize: true } navigatorRenderObject)
        {
            return;
        }

        BuildContext? fromSubtreeContext = from.SubtreeContext;
        Dictionary<object, HeroState> fromHeroes = fromSubtreeContext is { } fromContext
            ? Hero.AllHeroesFor(fromContext, isUserGestureTransition, navigator)
            : [];
        BuildContext? toSubtreeContext = to.SubtreeContext;
        Dictionary<object, HeroState> toHeroes = toSubtreeContext is { } toContext
            ? Hero.AllHeroesFor(toContext, isUserGestureTransition, navigator)
            : [];

        foreach ((object tag, HeroState fromHero) in fromHeroes)
        {
            toHeroes.TryGetValue(tag, out HeroState? toHero);
            _flights.TryGetValue(tag, out HeroFlight? existingFlight);
            HeroFlightManifest? manifest = toHero is null || flightType is null
                ? null
                : new HeroFlightManifest(
                    type: flightType.Value,
                    overlay: overlay,
                    navigatorSize: navigatorRenderObject.Size,
                    fromRoute: from,
                    toRoute: to,
                    fromHero: fromHero,
                    toHero: toHero,
                    createRectTween: CreateRectTween,
                    shuttleBuilder: toHero.CurrentWidget.FlightShuttleBuilder
                                    ?? fromHero.CurrentWidget.FlightShuttleBuilder
                                    ?? DefaultHeroFlightShuttleBuilder,
                    isUserGestureTransition: isUserGestureTransition,
                    isDiverted: existingFlight is not null);

            // Only proceed with a valid manifest. Otherwise abort the existing flight, and call
            // EndFlight when this loop finishes.
            if (manifest is not null && manifest.IsValid)
            {
                toHeroes.Remove(tag);
                if (existingFlight is not null)
                {
                    existingFlight.Divert(manifest);
                }
                else
                {
                    var flight = new HeroFlight(HandleFlightEnded);
                    _flights[tag] = flight;
                    flight.Start(manifest);
                }
            }
            else
            {
                manifest?.Dispose();
                existingFlight?.Abort();
            }
        }

        // The remaining entries in toHeroes are those that failed to participate in a new flight.
        foreach (HeroState toHero in toHeroes.Values)
        {
            toHero.EndFlight();
        }
    }

    private void HandleFlightEnded(HeroFlight flight)
    {
        if (_flights.Remove(flight.Manifest.Tag, out HeroFlight? removed))
        {
            removed.Dispose();
        }
    }

    /// <summary>Dart's `HeroController._defaultHeroFlightShuttleBuilder`.</summary>
    private Widget DefaultHeroFlightShuttleBuilder(
        BuildContext flightContext,
        Animation<double> animation,
        HeroFlightDirection flightDirection,
        BuildContext fromHeroContext,
        BuildContext toHeroContext)
    {
        var toHero = (Hero)toHeroContext.Widget;

        MediaQueryData? toMediaQueryData = MediaQuery.MaybeOf(toHeroContext);
        MediaQueryData? fromMediaQueryData = MediaQuery.MaybeOf(fromHeroContext);

        if (toMediaQueryData is null || fromMediaQueryData is null)
        {
            return toHero.Child;
        }

        Thickness fromHeroPadding = fromMediaQueryData.Padding;
        Thickness toHeroPadding = toMediaQueryData.Padding;

        return new AnimatedBuilder(
            animation: animation,
            builder: (BuildContext _, Widget? _) => new MediaQuery(
                data: toMediaQueryData with
                {
                    Padding = flightDirection == HeroFlightDirection.Push
                        ? new EdgeInsetsTween(begin: fromHeroPadding, end: toHeroPadding)
                            .Evaluate(animation.Value)
                        : new EdgeInsetsTween(begin: toHeroPadding, end: fromHeroPadding)
                            .Evaluate(animation.Value),
                },
                child: toHero.Child));
    }
}

/// <summary>
/// Dart's `HeroControllerScope`: supplies the <see cref="HeroController"/> a descendant
/// <see cref="Navigator"/> registers as an observer.
/// </summary>
public sealed class HeroControllerScope : InheritedWidget
{
    public HeroControllerScope(
        HeroController controller,
        Widget child,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    private HeroControllerScope(Widget child, Key? key) : base(key)
    {
        Controller = null;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public HeroController? Controller { get; }

    public Widget Child { get; }

    /// <summary>Dart's `HeroControllerScope.none`: hides an ancestor controller from the subtree.</summary>
    public static HeroControllerScope None(Widget child, Key? key = null) => new(child, key);

    public static HeroController? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<HeroControllerScope>()?.Controller;
    }

    public static HeroController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "HeroControllerScope.Of() was called with a context that does not contain a "
                   + "HeroControllerScope widget.");
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((HeroControllerScope)oldWidget).Controller, Controller);
    }
}

/// <summary>Dart's `HeroMode`: enables or disables hero flights for a subtree.</summary>
public sealed class HeroMode : StatelessWidget
{
    public HeroMode(
        Widget child,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Enabled = enabled;
    }

    public Widget Child { get; }

    public bool Enabled { get; }

    public override Widget Build(BuildContext context) => Child;
}
