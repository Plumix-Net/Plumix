using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scrollable.dart; flutter/packages/flutter/lib/src/widgets/scroll_view.dart; flutter/packages/flutter/lib/src/widgets/sliver.dart (approximate)

namespace Plumix.Widgets;

public delegate Widget IndexedWidgetBuilder(BuildContext context, int index);

public delegate int? ChildIndexGetter(Key key);

public abstract class ScrollNotification : LayoutChangedNotification, IViewportNotification
{
    protected ScrollNotification(
        IScrollMetrics metrics,
        int depth = 0,
        BuildContext? sourceContext = null)
    {
        Metrics = metrics;
        Depth = Math.Max(0, depth);
        if (sourceContext is BuildContext context)
        {
            SetContext(context);
        }
    }

    public IScrollMetrics Metrics { get; }

    public int Depth { get; private set; }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }
}

public sealed class ScrollMetricsNotification : Notification, IViewportNotification
{
    public ScrollMetricsNotification(
        IScrollMetrics metrics,
        BuildContext context,
        int depth = 0)
    {
        Metrics = metrics;
        Depth = Math.Max(0, depth);
        SetContext(context);
    }

    public IScrollMetrics Metrics { get; }

    public int Depth { get; private set; }

    public ScrollUpdateNotification AsScrollUpdate()
    {
        BuildContext sourceContext = Context
                                     ?? throw new InvalidOperationException(
                                         "ScrollMetricsNotification requires a source context.");
        return new ScrollUpdateNotification(
            Metrics,
            depth: Depth,
            sourceContext: sourceContext);
    }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }
}

public sealed class ScrollStartNotification : ScrollNotification
{
    public ScrollStartNotification(
        IScrollMetrics metrics,
        DragStartDetails? dragDetails = null,
        int depth = 0) : base(metrics, depth)
    {
        DragDetails = dragDetails;
    }

    public ScrollStartNotification(
        IScrollMetrics metrics,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        hasDragDetails ? new DragStartDetails(default) : null,
        depth)
    {
    }

    public DragStartDetails? DragDetails { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class ScrollUpdateNotification : ScrollNotification
{
    public ScrollUpdateNotification(
        IScrollMetrics metrics,
        DragUpdateDetails? dragDetails = null,
        double? scrollDelta = null,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        DragDetails = dragDetails;
        ScrollDelta = scrollDelta;
    }

    public ScrollUpdateNotification(
        IScrollMetrics metrics,
        double? scrollDelta,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        hasDragDetails
            ? new DragUpdateDetails(default, default, default, 0.0)
            : null,
        scrollDelta,
        depth)
    {
    }

    public DragUpdateDetails? DragDetails { get; }

    public double? ScrollDelta { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class OverscrollNotification : ScrollNotification
{
    public OverscrollNotification(
        IScrollMetrics metrics,
        double overscroll,
        DragUpdateDetails? dragDetails = null,
        double velocity = 0.0,
        int depth = 0) : base(metrics, depth)
    {
        if (!double.IsFinite(overscroll) || Math.Abs(overscroll) <= double.Epsilon)
        {
            throw new ArgumentOutOfRangeException(nameof(overscroll));
        }

        if (!double.IsFinite(velocity))
        {
            throw new ArgumentOutOfRangeException(nameof(velocity));
        }

        Overscroll = overscroll;
        DragDetails = dragDetails;
        Velocity = velocity;
    }

    public OverscrollNotification(
        IScrollMetrics metrics,
        double overscroll,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        overscroll,
        hasDragDetails
            ? new DragUpdateDetails(default, default, default, 0.0)
            : null,
        velocity: 0.0,
        depth)
    {
    }

    public double Overscroll { get; }

    public DragUpdateDetails? DragDetails { get; }

    public double Velocity { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class ScrollEndNotification : ScrollNotification
{
    public ScrollEndNotification(
        IScrollMetrics metrics,
        DragEndDetails? dragDetails = null,
        int depth = 0) : base(metrics, depth)
    {
        DragDetails = dragDetails;
    }

    public ScrollEndNotification(
        IScrollMetrics metrics,
        int depth) : this(metrics, dragDetails: null, depth)
    {
    }

    public DragEndDetails? DragDetails { get; }
}

/// <summary>
/// A notification that the user has changed the direction in which they are scrolling.
/// </summary>
public sealed class UserScrollNotification : ScrollNotification
{
    public UserScrollNotification(
        IScrollMetrics metrics,
        ScrollDirection direction,
        int depth = 0) : base(metrics, depth)
    {
        Direction = direction;
    }

    /// <summary>The direction in which the user is scrolling.</summary>
    public ScrollDirection Direction { get; }
}

public sealed class KeepAliveNotification : Notification
{
    public KeepAliveNotification(KeepAliveHandle handle)
    {
        Handle = handle;
    }

    public KeepAliveHandle Handle { get; }
}

public sealed class KeepAliveHandle : ChangeNotifier
{
    private bool _released;

    public bool IsReleased => _released;

    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        NotifyListeners();
        base.Dispose();
    }

    public override void Dispose()
    {
        Release();
    }
}

public sealed class AutomaticKeepAlive : StatefulWidget
{
    public AutomaticKeepAlive(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState()
    {
        return new AutomaticKeepAliveState();
    }

    private sealed class AutomaticKeepAliveState : State
    {
        private readonly Dictionary<KeepAliveHandle, Action> _releaseCallbacks = [];
        private bool _keepingAlive;

        private AutomaticKeepAlive CurrentWidget => (AutomaticKeepAlive)Element.Widget;

        public override Widget Build(BuildContext context)
        {
            return new NotificationListener<KeepAliveNotification>(
                onNotification: HandleKeepAliveNotification,
                child: new KeepAlive(
                    keepAlive: _keepingAlive,
                    child: CurrentWidget.Child));
        }

        public override void Dispose()
        {
            foreach (var (handle, callback) in _releaseCallbacks.ToArray())
            {
                handle.RemoveListener(callback);
            }

            _releaseCallbacks.Clear();
        }

        private bool HandleKeepAliveNotification(KeepAliveNotification notification)
        {
            var handle = notification.Handle;
            if (!_releaseCallbacks.ContainsKey(handle))
            {
                Action callback = () => HandleReleased(handle);
                _releaseCallbacks[handle] = callback;
                handle.AddListener(callback);
            }

            if (!_keepingAlive)
            {
                SetState(() => _keepingAlive = true);
            }

            return true;
        }

        private void HandleReleased(KeepAliveHandle handle)
        {
            if (!_releaseCallbacks.Remove(handle, out var callback))
            {
                return;
            }

            handle.RemoveListener(callback);
            if (_releaseCallbacks.Count == 0 && _keepingAlive)
            {
                SetState(() => _keepingAlive = false);
            }
        }
    }
}

public abstract class AutomaticKeepAliveClientMixin : State
{
    private KeepAliveHandle? _keepAliveHandle;

    protected abstract bool WantKeepAlive { get; }

    public void UpdateKeepAlive()
    {
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
        else
        {
            ReleaseKeepAlive();
        }
    }

    protected void EnsureKeepAlive()
    {
        if (_keepAliveHandle != null)
        {
            return;
        }

        var handle = new KeepAliveHandle();
        _keepAliveHandle = handle;
        new KeepAliveNotification(handle).Dispatch(Context);
    }

    public override void InitState()
    {
        base.InitState();
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
    }

    public override void Deactivate()
    {
        ReleaseKeepAlive();
        base.Deactivate();
    }

    public override void Dispose()
    {
        ReleaseKeepAlive();
        base.Dispose();
    }

    private void ReleaseKeepAlive()
    {
        var handle = _keepAliveHandle;
        if (handle == null)
        {
            return;
        }

        _keepAliveHandle = null;
        handle.Release();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/primary_scroll_controller.dart
public sealed class PrimaryScrollController : InheritedWidget
{
    private static readonly IReadOnlySet<TargetPlatform> MobilePlatforms = new HashSet<TargetPlatform>
    {
        TargetPlatform.Android,
        TargetPlatform.IOS,
        TargetPlatform.Fuchsia,
    };

    public PrimaryScrollController(
        ScrollController? controller,
        Widget child,
        IReadOnlySet<TargetPlatform>? automaticallyInheritForPlatforms = null,
        Axis? scrollDirection = Axis.Vertical,
        Key? key = null) : base(key)
    {
        Controller = controller;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        AutomaticallyInheritForPlatforms = automaticallyInheritForPlatforms ?? MobilePlatforms;
        ScrollDirection = scrollDirection;
    }

    public ScrollController? Controller { get; }

    public Widget Child { get; }

    public IReadOnlySet<TargetPlatform> AutomaticallyInheritForPlatforms { get; }

    public Axis? ScrollDirection { get; }

    public static PrimaryScrollController None(Widget child, Key? key = null)
    {
        return new PrimaryScrollController(
            controller: null,
            child: child,
            automaticallyInheritForPlatforms: new HashSet<TargetPlatform>(),
            scrollDirection: null,
            key: key);
    }

    public static bool ShouldInherit(BuildContext context, Axis scrollDirection)
    {
        PrimaryScrollController? result = context.FindAncestorWidgetOfExactType<PrimaryScrollController>();
        if (result == null)
        {
            return false;
        }

        TargetPlatform platform = ScrollConfiguration.Of(context).GetPlatform(context);
        return result.AutomaticallyInheritForPlatforms.Contains(platform)
               && result.ScrollDirection == scrollDirection;
    }

    public static ScrollController? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<PrimaryScrollController>()?.Controller;
    }

    public static ScrollController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("PrimaryScrollController not found in context.");
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((PrimaryScrollController)oldWidget).Controller, Controller);
    }
}

public class ScrollController : ChangeNotifier
{
    private readonly List<ScrollPosition> _positions = [];

    public ScrollController(
        double initialScrollOffset = 0.0,
        ScrollPhysics? physics = null,
        bool keepScrollOffset = true,
        string? debugLabel = null,
        Action<ScrollPosition>? onAttach = null,
        Action<ScrollPosition>? onDetach = null)
    {
        InitialScrollOffset = initialScrollOffset;
        KeepScrollOffset = keepScrollOffset;
        DebugLabel = debugLabel;
        OnAttach = onAttach;
        OnDetach = onDetach;
        Physics = physics ?? new ClampingScrollPhysics();
    }

    public double InitialScrollOffset { get; }

    public bool KeepScrollOffset { get; }

    /// <summary>A label that is used in the <see cref="ToString"/> output. Intended to aid with
    /// identifying scroll controller instances in debug output.</summary>
    public string? DebugLabel { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is attached to the scroll controller.</summary>
    public Action<ScrollPosition>? OnAttach { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is detached from the scroll controller.</summary>
    public Action<ScrollPosition>? OnDetach { get; }

    public ScrollPhysics Physics { get; }

    public bool HasClients => _positions.Count > 0;

    public IReadOnlyList<ScrollPosition> Positions => _positions;

    public double Offset => _positions.Count == 0 ? InitialScrollOffset : _positions[0].Pixels;

    public ScrollPosition? PrimaryPosition => _positions.Count == 0 ? null : _positions[0];

    public ScrollPosition Position => _positions.Count == 1
        ? _positions[0]
        : throw new InvalidOperationException(
            $"ScrollController.Position requires exactly one attached ScrollPosition; found {_positions.Count}.");

    /// <summary>
    /// Creates a <see cref="ScrollPosition"/> for use by a <see cref="Scrollable"/> widget.
    /// </summary>
    /// <remarks>
    /// Subclasses can override this function to customize the <see cref="ScrollPosition"/> used by
    /// the scrollable widgets they control. For example, <see cref="PageController"/> overrides this
    /// function to return a page-oriented scroll position subclass that keeps the same page visible
    /// when the scrollable widget resizes.
    /// <para>
    /// The <paramref name="context"/> is the scrollable's <see cref="IScrollContext"/>; the
    /// <paramref name="oldPosition"/> is the position that is being replaced, if any, whose state the
    /// new one absorbs.
    /// </para>
    /// </remarks>
    public virtual ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new ScrollPosition(
            physics: physics,
            context: context,
            initialPixels: InitialScrollOffset,
            keepScrollOffset: KeepScrollOffset,
            oldPosition: oldPosition,
            debugLabel: DebugLabel);
    }

    internal virtual void Attach(ScrollPosition position)
    {
        if (_positions.Contains(position))
        {
            return;
        }

        _positions.Add(position);
        position.AddListener(NotifyListeners);
        OnAttach?.Invoke(position);
    }

    internal virtual void Detach(ScrollPosition position)
    {
        if (!_positions.Contains(position))
        {
            return;
        }

        OnDetach?.Invoke(position);
        position.RemoveListener(NotifyListeners);
        _positions.Remove(position);
    }

    public void JumpTo(double value)
    {
        foreach (var position in _positions.ToArray())
        {
            position.JumpTo(value);
        }
    }

    public void AnimateTo(double value, TimeSpan duration, Curve? curve = null)
    {
        foreach (var position in _positions.ToArray())
        {
            position.AnimateTo(value, duration, curve);
        }
    }

    public override void Dispose()
    {
        foreach (var position in _positions.ToArray())
        {
            position.RemoveListener(NotifyListeners);
        }

        _positions.Clear();
        base.Dispose();
    }

    public override string ToString()
    {
        var description = new List<string>();
        DebugFillDescription(description);
        return $"{Diagnostics.DescribeIdentity(this)}({string.Join(", ", description)})";
    }

    /// <summary>Add additional information to the given description for use by
    /// <see cref="ToString"/>.</summary>
    protected virtual void DebugFillDescription(List<string> description)
    {
        if (DebugLabel != null)
        {
            description.Add(DebugLabel);
        }

        if (InitialScrollOffset != 0.0)
        {
            description.Add(
                $"initialScrollOffset: {InitialScrollOffset.ToString("F1", CultureInfo.InvariantCulture)}, ");
        }

        if (_positions.Count == 0)
        {
            description.Add("no clients");
        }
        else if (_positions.Count == 1)
        {
            // Don't actually list the client itself, since its toString may refer to us.
            description.Add($"one client, offset {Offset.ToString("F1", CultureInfo.InvariantCulture)}");
        }
        else
        {
            description.Add($"{_positions.Count} clients");
        }
    }
}

public class Scrollable : StatefulWidget
{
    public Scrollable(
        Widget? child = null,
        IReadOnlyList<Widget>? slivers = null,
        Axis axis = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        bool shrinkWrap = false,
        double anchor = 0.0,
        Key? center = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        ScrollIncrementCalculator? incrementCalculator = null,
        bool excludeFromSemantics = false,
        int? semanticChildCount = null,
        Key? key = null,
        Clip clipBehavior = Clip.HardEdge) : base(key)
    {
        if (!double.IsFinite(anchor) || anchor < 0.0 || anchor > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        if (semanticChildCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semanticChildCount));
        }

        ExcludeFromSemantics = excludeFromSemantics;
        SemanticChildCount = semanticChildCount;

        Child = child;
        Slivers = slivers;
        Axis = axis;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
        ShrinkWrap = shrinkWrap;
        Anchor = anchor;
        Center = center;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        DragStartBehavior = dragStartBehavior;
        RestorationId = restorationId;
        IncrementCalculator = incrementCalculator;
    }

    public Widget? Child { get; }

    public IReadOnlyList<Widget>? Slivers { get; }

    public Axis Axis { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public bool ShrinkWrap { get; }

    public double Anchor { get; }

    /// <summary>The key of the sliver that grows forward from the zero scroll offset.</summary>
    public Key? Center { get; }

    public Clip ClipBehavior { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public string? RestorationId { get; }

    /// <summary>Computes the distance a keyboard-driven line or page scroll moves.</summary>
    public ScrollIncrementCalculator? IncrementCalculator { get; }

    /// <summary>
    /// Whether the scrollable contributes no semantics of its own. When true no scroll actions, scroll
    /// metrics or two-pane split are produced and the viewport's nodes are reported as-is.
    /// </summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>
    /// The total number of children the scrollable can show, or <c>null</c> when the count is unknown
    /// or unbounded. Reported to assistive technologies as the scroll child count.
    /// </summary>
    public int? SemanticChildCount { get; }

    internal bool UseSingleChildViewport { get; init; }

    /// <summary>
    /// Builds the viewport this scrollable scrolls, when the default composition is not wanted.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>Scrollable.viewportBuilder</c>, which <c>ScrollView.build</c> supplies from its
    /// overridable <c>buildViewport</c>.
    /// </remarks>
    internal Func<BuildContext, ViewportOffset, Widget>? ViewportBuilder { get; init; }

    public override State CreateState()
    {
        return new ScrollableState();
    }

    public static ScrollableState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<ScrollableState>();
    }

    public static ScrollableState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("Scrollable operation requested with no Scrollable ancestor.");
    }

    /// <summary>
    /// Whether the enclosing scrollable is scrolling fast enough that expensive frame-bound work
    /// (such as decoding an image) should be deferred to a later frame.
    /// </summary>
    /// <param name="context">A context inside the scrollable to consult.</param>
    /// <param name="axis">
    /// When given, scrollables on other axes are skipped and the search continues outwards.
    /// </param>
    /// <remarks>Returns false when there is no enclosing scrollable on the requested axis.</remarks>
    public static bool RecommendDeferredLoadingForContext(BuildContext context, Axis? axis = null)
    {
        ScrollableState? scrollable = MaybeOf(context);
        while (scrollable != null)
        {
            if (axis == null || ScrollDirectionUtils.AxisDirectionToAxis(scrollable.AxisDirection) == axis)
            {
                return scrollable.Position.RecommendDeferredLoading(context);
            }

            context = scrollable.Context;
            scrollable = MaybeOf(context);
        }

        return false;
    }

    /// <summary>
    /// Scrolls every enclosing scrollable, innermost first, so that the render object of
    /// <paramref name="context"/> becomes visible.
    /// </summary>
    /// <remarks>
    /// Each outer scrollable reveals the render object of the scrollable inside it, while the
    /// original target is carried along so the outer scroll keeps that target as visible as it can
    /// (Flutter's `targetRenderObject`).
    /// </remarks>
    public static Task EnsureVisible(
        BuildContext context,
        double alignment = 0.0,
        TimeSpan? duration = null,
        Curve? curve = null,
        ScrollPositionAlignmentPolicy alignmentPolicy = ScrollPositionAlignmentPolicy.Explicit)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!double.IsFinite(alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment), "Alignment must be finite.");
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        TimeSpan effectiveDuration = duration ?? TimeSpan.Zero;
        var futures = new List<Task>();
        RenderObject? targetRenderObject = null;
        ScrollableState? scrollable = MaybeOf(context);
        while (scrollable != null)
        {
            if (context.FindRenderObject() is not { } renderObject)
            {
                break;
            }

            futures.Add(scrollable.PerformEnsureVisible(
                renderObject,
                alignment,
                effectiveDuration,
                curve,
                alignmentPolicy,
                targetRenderObject));
            targetRenderObject ??= renderObject;

            context = scrollable.Context;
            scrollable = MaybeOf(context);
        }

        if (futures.Count == 0 || effectiveDuration == TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return futures.Count == 1 ? futures[0] : Task.WhenAll(futures);
    }

    /// <summary>
    /// State object for a <see cref="Scrollable"/> widget.
    /// </summary>
    /// <remarks>
    /// To manipulate a <see cref="Scrollable"/> widget's scroll position, use the object obtained
    /// from the <see cref="Position"/> property. To be informed of when a <see cref="Scrollable"/>
    /// widget is scrolling, use a <see cref="NotificationListener{T}"/> to listen for
    /// <see cref="ScrollNotification"/>s.
    /// <para>
    /// This class is the <see cref="IScrollContext"/> its <see cref="ScrollPosition"/> drives:
    /// the position asks it for its vsync, axis direction and notification/storage contexts, and
    /// tells it whether the user may drag and whether the viewport should ignore pointer events.
    /// </para>
    /// </remarks>
    public class ScrollableState : State, IScrollContext
    {
        private ScrollController? _fallbackController;
        private ScrollController? _attachedController;
        private ScrollPosition _position = null!;
        private bool _isApplyingDrag;
        private ScrollBehavior _configuration = null!;
        private ScrollPhysics _effectivePhysics = null!;
        private bool _hasPosition;
        private AxisDirection _axisDirection = AxisDirection.Down;
        private double _devicePixelRatio = 1.0;
        // Keys are records, so the identity has to come from a per-state sentinel: two scrollables
        // must never share one global key.
        private readonly GlobalObjectKey<RawGestureDetector.RawGestureDetectorState> _gestureDetectorKey =
            new(new object());
        private readonly GlobalObjectKey<State> _ignorePointerKey = new(new object());

        private IScrollHoldController? _hold;
        private ScrollDragController? _drag;
        private bool _canDrag;
        private bool _shouldIgnorePointer;

        private Scrollable CurrentWidget => (Scrollable)Element.Widget;

        public ScrollPosition Position => _position;

        /// <summary>The direction in which the widget scrolls.</summary>
        public AxisDirection AxisDirection => _axisDirection;

        /// <summary>A <see cref="ITickerProvider"/> to use when animating the scroll position.</summary>
        public ITickerProvider Vsync => this;

        /// <summary>
        /// The device pixel ratio of the view the scrollable is drawn into, refreshed whenever the
        /// dependencies change.
        /// </summary>
        public double DevicePixelRatio => _devicePixelRatio;

        /// <summary>
        /// The <see cref="BuildContext"/> that should be used when dispatching
        /// <see cref="ScrollNotification"/>s: the gesture detector's, which sits inside the widgets
        /// the <see cref="ScrollBehavior"/> wraps around the viewport (scrollbar, overscroll
        /// indicator) so they receive the notifications, and below this state so
        /// <see cref="Scrollable.Of"/> resolves from a notification's context.
        /// </summary>
        public BuildContext? NotificationContext => _gestureDetectorKey.CurrentContext;

        /// <summary>
        /// The <see cref="BuildContext"/> that should be used when searching for a
        /// <see cref="PageStorage"/>: this state's own.
        /// </summary>
        public BuildContext StorageContext => Context;

        /// <summary>The physics this scrollable resolved from its widget and ambient behavior.</summary>
        public ScrollPhysics EffectivePhysics => _effectivePhysics;

        /// <summary>The scrollable's keyboard scroll-distance calculator, if it was given one.</summary>
        public ScrollIncrementCalculator? IncrementCalculator => CurrentWidget.IncrementCalculator;

        /// <summary>The scrollable's current metrics.</summary>
        public IScrollMetrics Metrics => CurrentMetrics();

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            _axisDirection = ResolveAxisDirection(CurrentWidget.Axis, CurrentWidget.Reverse);
            // Ballistic tolerances are expressed in device pixels, so the physics need the view's ratio.
            _devicePixelRatio = MediaQuery.MaybeOf(Context)?.DevicePixelRatio ?? 1.0;
            ScrollBehavior configuration =
                CurrentWidget.ScrollBehavior ?? ScrollConfiguration.Of(Context);
            ScrollPhysics effectivePhysics = ResolvePhysics(CurrentWidget, configuration);
            if (!_hasPosition)
            {
                _configuration = configuration;
                _effectivePhysics = effectivePhysics;
                _position = AttachToController(CurrentWidget.Controller, effectivePhysics);
                _hasPosition = true;
                RestoreScrollOffset();
                _position.AddListener(HandlePositionChanged);
                return;
            }

            bool physicsChanged = !PhysicsChainsMatch(_effectivePhysics, effectivePhysics);
            _configuration = configuration;
            _effectivePhysics = effectivePhysics;
            if (physicsChanged)
            {
                ReplacePosition(CurrentWidget.Controller, effectivePhysics);
            }
        }

        /// <summary>
        /// The physics the position runs: the widget's own (or its behavior's) applied on top of the
        /// ambient configuration's, so a bare <see cref="AlwaysScrollableScrollPhysics"/> still
        /// inherits the platform's ballistics (Flutter's <c>_updatePosition</c>).
        /// </summary>
        private ScrollPhysics ResolvePhysics(Scrollable widget, ScrollBehavior configuration)
        {
            ScrollPhysics? physicsFromWidget = widget.Physics ?? widget.ScrollBehavior?.GetScrollPhysics(Context);
            ScrollPhysics physics = configuration.GetScrollPhysics(Context);
            return physicsFromWidget?.ApplyTo(physics) ?? physics;
        }

        /// <summary>
        /// Whether two physics chains are interchangeable. Flutter's <c>_shouldUpdatePosition</c>
        /// walks both chains comparing <c>runtimeType</c>, because a widget that rebuilds its physics
        /// every build (<see cref="PageView"/> does) must not replace its position every frame.
        /// </summary>
        private static bool PhysicsChainsMatch(ScrollPhysics? left, ScrollPhysics? right)
        {
            while (left != null || right != null)
            {
                if (left?.GetType() != right?.GetType())
                {
                    return false;
                }

                left = left?.Parent;
                right = right?.Parent;
            }

            return true;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldScrollable = (Scrollable)oldWidget;
            var current = CurrentWidget;
            if (!string.Equals(oldScrollable.RestorationId, current.RestorationId, StringComparison.Ordinal))
            {
                SaveScrollOffset(oldScrollable.RestorationId);
                RestoreScrollOffset();
            }

            _axisDirection = ResolveAxisDirection(current.Axis, current.Reverse);
            bool controllerChanged = !ReferenceEquals(oldScrollable.Controller, current.Controller);
            ScrollBehavior configuration = current.ScrollBehavior ?? ScrollConfiguration.Of(Context);
            ScrollPhysics effectivePhysics = ResolvePhysics(current, configuration);
            bool physicsChanged = !PhysicsChainsMatch(_effectivePhysics, effectivePhysics);
            _configuration = configuration;
            _effectivePhysics = effectivePhysics;
            _position.RestorationId = current.RestorationId;

            if (!controllerChanged && !physicsChanged)
            {
                return;
            }

            ReplacePosition(current.Controller, effectivePhysics);
        }

        public override void Dispose()
        {
            if (!_hasPosition)
            {
                _fallbackController?.Dispose();
                return;
            }

            _position.RemoveListener(HandlePositionChanged);
            SaveScrollOffset();
            _attachedController?.Detach(_position);
            _position.Dispose();
            _fallbackController?.Dispose();
            _hasPosition = false;
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            _axisDirection = ResolveAxisDirection(widget.Axis, widget.Reverse);
            AxisDirection axisDirection = _axisDirection;
            ScrollCacheExtent scrollCacheExtent = widget.CacheExtentStyle == CacheExtentStyle.Viewport
                ? ScrollCacheExtent.Viewport(widget.CacheExtent)
                : ScrollCacheExtent.Pixels(widget.CacheExtent);
            Widget viewport;
            if (widget.ViewportBuilder is { } viewportBuilder)
            {
                viewport = viewportBuilder(context, _position);
            }
            else if (widget.UseSingleChildViewport)
            {
                viewport = new SingleChildViewport(
                    child: widget.Child ?? new SizedBox(),
                    axisDirection: axisDirection,
                    offset: _position);
            }
            else if (widget.ShrinkWrap)
            {
                viewport = new ShrinkWrappingViewport(
                    offset: _position,
                    axisDirection: axisDirection,
                    scrollCacheExtent: scrollCacheExtent,
                    clipBehavior: widget.ClipBehavior,
                    slivers: ResolveSlivers(widget));
            }
            else
            {
                viewport = new Viewport(
                    offset: _position,
                    axisDirection: axisDirection,
                    anchor: widget.Anchor,
                    center: widget.Center,
                    scrollCacheExtent: scrollCacheExtent,
                    clipBehavior: widget.ClipBehavior,
                    slivers: ResolveSlivers(widget));
            }

            bool horizontal = widget.Axis == Axis.Horizontal;
            bool vertical = widget.Axis == Axis.Vertical;
            Widget scrollable = new Listener(
                behavior: widget.HitTestBehavior,
                onPointerSignal: HandlePointerSignal,
                child: new RawGestureDetector(
                    key: _gestureDetectorKey,
                    behavior: widget.HitTestBehavior,
                    // The physics decide whether the user may drag at all; when they refuse, the
                    // recognizers are not registered, exactly like Flutter's `setCanDrag(false)`.
                    dragEnabled: _canDrag,
                    onHorizontalDragDown: horizontal ? HandleDragDown : null,
                    onHorizontalDragStart: horizontal ? HandleDragStart : null,
                    onHorizontalDragUpdate: horizontal ? HandleDragUpdate : null,
                    onHorizontalDragEnd: horizontal ? HandleDragEnd : null,
                    onHorizontalDragCancel: horizontal ? HandleDragCancel : null,
                    onVerticalDragDown: vertical ? HandleDragDown : null,
                    onVerticalDragStart: vertical ? HandleDragStart : null,
                    onVerticalDragUpdate: vertical ? HandleDragUpdate : null,
                    onVerticalDragEnd: vertical ? HandleDragEnd : null,
                    onVerticalDragCancel: vertical ? HandleDragCancel : null,
                    velocityTrackerBuilder: _configuration.VelocityTrackerBuilder(context),
                    supportedDevices: _configuration.DragDevices,
                    minFlingDistance: _effectivePhysics.MinFlingDistance,
                    minFlingVelocity: _effectivePhysics.MinFlingVelocity,
                    maxFlingVelocity: _effectivePhysics.MaxFlingVelocity,
                    dragStartBehavior: widget.DragStartBehavior,
                    child: new Semantics(
                        explicitChildNodes: !widget.ExcludeFromSemantics,
                        child: new IgnorePointer(
                            key: _ignorePointerKey,
                            ignoring: _shouldIgnorePointer,
                            child: viewport))));

            if (!widget.ExcludeFromSemantics)
            {
                scrollable = new ScrollSemantics(
                    position: _position,
                    allowImplicitScrolling: _effectivePhysics.AllowImplicitScrolling,
                    axisDirection: axisDirection,
                    semanticChildCount: widget.SemanticChildCount,
                    child: scrollable);
            }

            var details = new ScrollableDetails(
                Direction: axisDirection,
                Controller: _attachedController,
                Physics: _effectivePhysics,
                DecorationClipBehavior: widget.ClipBehavior);
            return _configuration.BuildScrollbar(
                context,
                _configuration.BuildOverscrollIndicator(context, scrollable, details),
                details);
        }

        /// <summary>
        /// Reveals <paramref name="renderObject"/> in this scrollable. Flutter's two-dimensional
        /// scrollables override this to drive both of their positions.
        /// </summary>
        internal Task PerformEnsureVisible(
            RenderObject renderObject,
            double alignment,
            TimeSpan duration,
            Curve? curve,
            ScrollPositionAlignmentPolicy alignmentPolicy,
            RenderObject? targetRenderObject)
        {
            return _position.EnsureVisible(
                renderObject,
                alignment,
                duration,
                curve,
                alignmentPolicy,
                targetRenderObject);
        }

        private IReadOnlyList<Widget> ResolveSlivers(Scrollable widget)
        {
            if (widget.Slivers is { Count: > 0 } slivers)
            {
                return slivers;
            }

            return [new SliverToBoxAdapter(widget.Child ?? new SizedBox())];
        }

        /// <summary>
        /// Filters the semantics actions the gesture handler exposes to the directions the position
        /// can still be scrolled in.
        /// </summary>
        /// <remarks>Flutter's <c>ScrollableState.setSemanticsActions</c>.</remarks>
        public void SetSemanticsActions(SemanticsActions actions)
        {
            _gestureDetectorKey.CurrentState?.ReplaceSemanticsActions(actions);
        }

        /// <summary>
        /// Persists the offset the position reports when scrolling ends. Plumix's
        /// <see cref="Scrollable"/> has not adopted the restoration buckets yet: the offset is kept
        /// through <see cref="PageStorage"/> under the widget's restoration id by the position's own
        /// <see cref="ScrollPosition.SaveScrollOffset"/>, so there is nothing further to record here.
        /// </summary>
        public void SaveOffset(double offset)
        {
        }

        private ScrollPosition AttachToController(
            ScrollController? providedController,
            ScrollPhysics physics,
            ScrollPosition? oldPosition = null)
        {
            _fallbackController ??= new ScrollController();
            _attachedController = providedController ?? _fallbackController;
            // The restoration id must be known before the constructor absorbs the old position and
            // any subclass constructor reads storage (NestedScrollPosition does).
            var position = _attachedController.CreateScrollPosition(physics, this, oldPosition);
            position.RestorationId = CurrentWidget.RestorationId;
            _attachedController.Attach(position);
            return position;
        }

        private void ReplacePosition(ScrollController? controller, ScrollPhysics physics)
        {
            ScrollPosition oldPosition = _position;
            oldPosition.RemoveListener(HandlePositionChanged);
            SaveScrollOffset();
            _attachedController?.Detach(oldPosition);

            _effectivePhysics = physics;
            // The new position absorbs the old one before the old one is disposed, so a drag or
            // ballistic run crossing the replacement is not dropped.
            _position = AttachToController(controller, physics, oldPosition);
            _position.AddListener(HandlePositionChanged);
            oldPosition.Dispose();
            SetState(static () => { });
        }

        private void RestoreScrollOffset()
        {
            _position.RestorationId = CurrentWidget.RestorationId;
            _position.RestoreScrollOffset();
        }

        private void SaveScrollOffset()
        {
            SaveScrollOffset(CurrentWidget.RestorationId);
        }

        private void SaveScrollOffset(string? restorationId)
        {
            string? current = _position.RestorationId;
            _position.RestorationId = restorationId;
            _position.SaveScrollOffset();
            _position.RestorationId = current;
        }

        private void HandlePositionChanged()
        {
            SaveScrollOffset();
            if (_isApplyingDrag)
            {
                return;
            }

            // A correction applied while the fresh dimensions were handed to the position never
            // reaches this point: Flutter's `correctPixels`/`correctBy` deliberately notify nobody.
            new ScrollUpdateNotification(CurrentMetrics()).Dispatch(NotificationContext);
        }

        /// <summary>
        /// Adds or removes the drag gesture recognizers. Turning dragging off also cancels any hold
        /// or drag in flight, so a physics change mid-gesture cannot leave the position captured.
        /// </summary>
        public void SetCanDrag(bool value)
        {
            if (value == _canDrag)
            {
                return;
            }

            _canDrag = value;
            if (!value && (_hold != null || _drag != null))
            {
                HandleDragCancel();
            }

            // Applied straight away rather than through a rebuild: the physics can change their mind
            // during layout, and the next pointer down must already see the new registration.
            _gestureDetectorKey.CurrentState?.SetDragEnabled(value);
        }

        /// <summary>
        /// Whether the viewport's contents should ignore pointer events: true while an activity that
        /// is not the user's own drag moves the position, so a tap during a fling stops the scroll
        /// instead of reaching a child.
        /// </summary>
        public void SetIgnorePointer(bool value)
        {
            if (_shouldIgnorePointer == value)
            {
                return;
            }

            _shouldIgnorePointer = value;
            if (_ignorePointerKey.CurrentContext is { } context
                && context.FindRenderObject() is RenderIgnorePointer renderBox)
            {
                renderBox.Ignoring = _shouldIgnorePointer;
            }
        }

        private void HandleDragDown(DragDownDetails details)
        {
            _hold = _position.Hold(DisposeHold);
        }

        /// <summary>
        /// Replays a semantic scroll request as a complete drag, so it runs through the scroll physics
        /// exactly like a pointer drag would.
        /// </summary>
        /// <remarks>Flutter's <c>_DefaultSemanticsGestureDelegate</c> drag replay.</remarks>
        private void DisposeHold()
        {
            _hold = null;
        }

        private void DisposeDrag()
        {
            _drag = null;
        }

        private void HandleDragStart(DragStartDetails details)
        {
            ScrollViewKeyboardDismissBehavior keyboardDismissBehavior =
                CurrentWidget.KeyboardDismissBehavior
                ?? _configuration.GetKeyboardDismissBehavior(Context);
            FocusNode? primaryFocus = FocusManager.Instance.PrimaryFocus;
            if (keyboardDismissBehavior == ScrollViewKeyboardDismissBehavior.OnDrag
                && primaryFocus != null
                && IsDescendantFocus(primaryFocus))
            {
                primaryFocus.Unfocus();
            }

            _drag = _position.Drag(details, DisposeDrag);
            new ScrollStartNotification(CurrentMetrics(), dragDetails: details).Dispatch(NotificationContext);
        }

        private bool IsDescendantFocus(FocusNode focusNode)
        {
            for (Element? ancestor = focusNode.AttachmentElement; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ReferenceEquals(ancestor, Element))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            ApplyDragOffset(details);
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            _drag?.End(details);
            new ScrollEndNotification(CurrentMetrics(), dragDetails: details).Dispatch(NotificationContext);
        }

        private void HandleDragCancel()
        {
            _hold?.Cancel();
            _drag?.Cancel();
            new ScrollEndNotification(CurrentMetrics()).Dispatch(NotificationContext);
        }

        private void ApplyDragOffset(DragUpdateDetails details)
        {
            if (_drag == null)
            {
                return;
            }

            IScrollMetrics before = _position.CopyWith();
            double applied;
            _isApplyingDrag = true;
            try
            {
                // The controller owns the motion-start threshold and axis reversal; it reports the
                // offset it actually handed to the position.
                applied = _drag.Update(details);
            }
            finally
            {
                _isApplyingDrag = false;
            }

            if (applied == 0.0)
            {
                // The motion-start threshold swallowed this update; nothing moved and nothing
                // overscrolled, so no notification is due.
                return;
            }

            double intendedScrollDelta = -_position.Physics.ApplyPhysicsToUserOffset(before, applied);
            double actualScrollDelta = _position.Pixels - before.Pixels;
            if (Math.Abs(actualScrollDelta) > 0.0001)
            {
                new ScrollUpdateNotification(
                    CurrentMetrics(),
                    dragDetails: details,
                    scrollDelta: actualScrollDelta).Dispatch(NotificationContext);
                SetState(static () => { });
            }

            double overscroll = intendedScrollDelta - actualScrollDelta;
            if (Math.Abs(overscroll) > 0.0001)
            {
                new OverscrollNotification(
                    CurrentMetrics(),
                    overscroll: overscroll,
                    dragDetails: details).Dispatch(NotificationContext);
            }
        }

        private void HandlePointerSignal(PointerSignalEvent @event)
        {
            if (@event is not PointerScrollEvent scroll)
            {
                return;
            }

            if (!_effectivePhysics.ShouldAcceptUserOffset(_position))
            {
                return;
            }

            double delta = PointerSignalEventDelta(scroll);
            double targetPixels = Math.Clamp(
                _position.Pixels + delta,
                _position.MinScrollExtent,
                _position.MaxScrollExtent);
            if (Math.Abs(delta) <= double.Epsilon
                || Math.Abs(targetPixels - _position.Pixels) <= double.Epsilon)
            {
                scroll.Respond(allowPlatformDefault: true);
                return;
            }

            // The start/update/end notifications are dispatched by the position itself, exactly like
            // Flutter's `ScrollPositionWithSingleContext.pointerScroll`.
            _position.ApplyPointerScrollDelta(delta);
            scroll.Respond(allowPlatformDefault: false);
        }

        private double PointerSignalEventDelta(PointerScrollEvent @event)
        {
            bool flipAxes = @event.Kind == PointerDeviceKind.Mouse
                            && _configuration.PointerAxisModifiers.Any(IsLogicalKeyPressed);
            Axis axis = flipAxes
                ? CurrentWidget.Axis == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal
                : CurrentWidget.Axis;
            double delta = axis == Axis.Horizontal ? @event.ScrollDelta.X : @event.ScrollDelta.Y;
            return IsReversedAxisDirection() ? -delta : delta;
        }

        private static bool IsLogicalKeyPressed(LogicalKeyboardKey key)
        {
            return HardwareKeyboard.Instance.IsLogicalKeyPressed(key);
        }

        private IScrollMetrics CurrentMetrics() => _position.CopyWith();

        private bool IsReversedAxisDirection()
        {
            var axisDirection = ResolveAxisDirection(CurrentWidget.Axis, CurrentWidget.Reverse);
            return ScrollDirectionUtils.AxisDirectionIsReversed(axisDirection);
        }

        private AxisDirection ResolveAxisDirection(Axis axis, bool reverse)
        {
            if (axis == Axis.Vertical)
            {
                return reverse ? AxisDirection.Up : AxisDirection.Down;
            }

            AxisDirection readingDirection = Directionality.Of(Context) == TextDirection.Rtl
                ? AxisDirection.Left
                : AxisDirection.Right;
            if (!reverse)
            {
                return readingDirection;
            }

            return readingDirection == AxisDirection.Left ? AxisDirection.Right : AxisDirection.Left;
        }
    }
}

internal sealed class SingleChildViewport : SingleChildRenderObjectWidget
{
    public SingleChildViewport(
        Widget child,
        AxisDirection axisDirection,
        ViewportOffset offset) : base(child)
    {
        AxisDirection = axisDirection;
        Offset = offset;
    }

    public AxisDirection AxisDirection { get; }

    public ViewportOffset Offset { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderSingleChildViewport(
        axisDirection: AxisDirection,
        offset: Offset);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderSingleChildViewport)renderObject;
        viewport.AxisDirection = AxisDirection;
        viewport.Offset = Offset;
    }
}

/// <summary>
/// Maps a child and its position in the delegate onto the index assistive technologies see, or
/// <c>null</c> to give that child no semantic index at all.
/// </summary>
/// <remarks>Flutter's <c>SemanticIndexCallback</c>.</remarks>
public delegate int? SemanticIndexCallback(Widget widget, int localIndex);

public abstract class SliverChildDelegate
{
    /// <remarks>Flutter's <c>_kDefaultSemanticIndexCallback</c>.</remarks>
    public static int? DefaultSemanticIndexCallback(Widget widget, int localIndex) => localIndex;

    public abstract Widget? Build(BuildContext context, int index);

    public virtual int? EstimatedChildCount => null;

    public virtual int? FindIndexByKey(Key key) => null;

    /// <summary>
    /// Wraps <paramref name="child"/> in an <see cref="IndexedSemantics"/> so the enclosing scrollable
    /// can report which child is the first visible one.
    /// </summary>
    private protected static Widget AddSemanticIndex(
        Widget child,
        int index,
        bool addSemanticIndexes,
        SemanticIndexCallback semanticIndexCallback,
        int semanticIndexOffset)
    {
        if (!addSemanticIndexes)
        {
            return child;
        }

        int? semanticIndex = semanticIndexCallback(child, index);
        return semanticIndex is null
            ? child
            : new IndexedSemantics(index: semanticIndex.Value + semanticIndexOffset, child: child);
    }
}

internal sealed record SliverChildKey(Key Value) : LocalKey;

public sealed class SliverChildBuilderDelegate : SliverChildDelegate
{
    private readonly IndexedWidgetBuilder _builder;
    private readonly int? _childCount;
    private readonly bool _addAutomaticKeepAlives;
    private readonly bool _addSemanticIndexes;
    private readonly SemanticIndexCallback _semanticIndexCallback;
    private readonly int _semanticIndexOffset;

    public SliverChildBuilderDelegate(
        IndexedWidgetBuilder builder,
        int? childCount = null,
        bool addAutomaticKeepAlives = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _childCount = childCount;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _addSemanticIndexes = addSemanticIndexes;
        _semanticIndexCallback = semanticIndexCallback ?? DefaultSemanticIndexCallback;
        _semanticIndexOffset = semanticIndexOffset;
        FindChildIndexCallback = findChildIndexCallback;
    }

    public override int? EstimatedChildCount => _childCount;

    public ChildIndexGetter? FindChildIndexCallback { get; }

    public override int? FindIndexByKey(Key key)
    {
        if (FindChildIndexCallback is null)
        {
            return null;
        }

        return FindChildIndexCallback(key is SliverChildKey childKey ? childKey.Value : key);
    }

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || (_childCount.HasValue && index >= _childCount.Value))
        {
            return null;
        }

        var child = _builder(context, index);
        Key? key = child.Key is null ? null : new SliverChildKey(child.Key);
        Widget result = AddSemanticIndex(
            child,
            index,
            _addSemanticIndexes,
            _semanticIndexCallback,
            _semanticIndexOffset);
        result = _addAutomaticKeepAlives ? new AutomaticKeepAlive(result) : result;
        return key is null ? result : new KeyedSubtree(result, key);
    }
}

public sealed class SliverChildListDelegate : SliverChildDelegate
{
    private readonly IReadOnlyList<Widget> _children;
    private readonly bool _addAutomaticKeepAlives;
    private readonly bool _addSemanticIndexes;
    private readonly SemanticIndexCallback _semanticIndexCallback;
    private readonly int _semanticIndexOffset;

    public SliverChildListDelegate(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0)
    {
        _children = children;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _addSemanticIndexes = addSemanticIndexes;
        _semanticIndexCallback = semanticIndexCallback ?? DefaultSemanticIndexCallback;
        _semanticIndexOffset = semanticIndexOffset;
    }

    public override int? EstimatedChildCount => _children.Count;

    public override int? FindIndexByKey(Key key)
    {
        Key childKey = key is SliverChildKey saltedKey ? saltedKey.Value : key;
        for (int index = 0; index < _children.Count; index++)
        {
            if (Equals(_children[index].Key, childKey))
            {
                return index;
            }
        }

        return null;
    }

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || index >= _children.Count)
        {
            return null;
        }

        var child = _children[index];
        Widget result = AddSemanticIndex(
            child,
            index,
            _addSemanticIndexes,
            _semanticIndexCallback,
            _semanticIndexOffset);
        result = _addAutomaticKeepAlives ? new AutomaticKeepAlive(result) : result;
        return child.Key is null
            ? result
            : new KeyedSubtree(result, new SliverChildKey(child.Key));
    }
}

public sealed class SliverToBoxAdapter : SingleChildRenderObjectWidget
{
    public SliverToBoxAdapter(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverToBoxAdapter();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverIgnorePointer : SingleChildRenderObjectWidget
{
    public SliverIgnorePointer(
        Widget? sliver = null,
        bool ignoring = true,
        bool? ignoringSemantics = null,
        Key? key = null) : base(sliver, key)
    {
        Ignoring = ignoring;
        IgnoringSemantics = ignoringSemantics;
    }

    public bool Ignoring { get; }

    public bool? IgnoringSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverIgnorePointer(
            ignoring: Ignoring,
            ignoringSemantics: IgnoringSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var ignorePointer = (RenderSliverIgnorePointer)renderObject;
        ignorePointer.Ignoring = Ignoring;
        ignorePointer.IgnoringSemantics = IgnoringSemantics;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverOffstage : SingleChildRenderObjectWidget
{
    public SliverOffstage(
        Widget? sliver = null,
        bool offstage = true,
        Key? key = null) : base(sliver, key)
    {
        Offstage = offstage;
    }

    public bool Offstage { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOffstage(offstage: Offstage);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverOffstage)renderObject).Offstage = Offstage;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverOpacity : SingleChildRenderObjectWidget
{
    public SliverOpacity(
        double opacity,
        Widget? sliver = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(sliver, key)
    {
        if (!double.IsFinite(opacity) || opacity < 0.0 || opacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between zero and one.");
        }

        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity { get; }

    public bool AlwaysIncludeSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOpacity(
            opacity: Opacity,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderSliverOpacity)renderObject;
        opacity.Opacity = Opacity;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
    }
}

public sealed class SliverPadding : SingleChildRenderObjectWidget
{
    public SliverPadding(Thickness padding, Widget? sliver = null, Key? key = null) : base(sliver, key)
    {
        Padding = padding;
    }

    public Thickness Padding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPadding(Padding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverPadding)renderObject).Padding = Padding;
    }
}

public sealed class KeepAlive : ParentDataWidget<SliverMultiBoxAdaptorParentData>
{
    public KeepAlive(
        bool keepAlive,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Value = keepAlive;
    }

    public bool Value { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(SliverMultiBoxAdaptorWidget);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (SliverMultiBoxAdaptorParentData)renderObject.parentData!;
        if (parentData.KeepAlive == Value)
        {
            return;
        }

        parentData.KeepAlive = Value;
        if (!Value)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }
}

public abstract class SliverMultiBoxAdaptorWidget : RenderObjectWidget
{
    protected SliverMultiBoxAdaptorWidget(SliverChildDelegate @delegate, Key? key = null) : base(key)
    {
        Delegate = @delegate;
    }

    public SliverChildDelegate Delegate { get; }

    internal override Element CreateElement()
    {
        return new SliverMultiBoxAdaptorElement(this);
    }
}

internal class SliverMultiBoxAdaptorElement : RenderObjectElement, IRenderSliverBoxChildManager
{
    private readonly SortedDictionary<int, Element> _childElements = [];
    private readonly Dictionary<Element, int> _indexByElement = [];
    private readonly Dictionary<RenderBox, Element> _elementByRenderObject = [];
    private bool _didUnderflow;

    public SliverMultiBoxAdaptorElement(SliverMultiBoxAdaptorWidget widget) : base(widget)
    {
    }

    protected SliverMultiBoxAdaptorWidget TypedWidget => (SliverMultiBoxAdaptorWidget)Widget;

    protected RenderSliverMultiBoxAdaptor TypedRenderObject => (RenderSliverMultiBoxAdaptor)RequireRenderObject();

    int? IRenderSliverBoxChildManager.ChildCount => TypedWidget.Delegate.EstimatedChildCount;

    protected override void OnMount()
    {
        base.OnMount();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnDeactivate()
    {
        if (RenderObject is RenderSliverMultiBoxAdaptor renderObject && ReferenceEquals(renderObject.ChildManager, this))
        {
            renderObject.ChildManager = null;
        }

        base.OnDeactivate();
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        SyncChildWidgets();
        TypedRenderObject.MarkNeedsLayout();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        TypedRenderObject.ChildManager = this;
        SyncChildWidgets();
        TypedRenderObject.MarkNeedsLayout();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (var child in _childElements.Values)
        {
            visitor(child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        RemoveMappings(child);
    }

    internal override void Unmount()
    {
        foreach (var child in _childElements.Values.ToArray())
        {
            UnmountChild(child);
        }

        _childElements.Clear();
        _indexByElement.Clear();
        _elementByRenderObject.Clear();
        base.Unmount();
    }

    public bool CreateChild(int index, RenderBox? after)
    {
        if (_childElements.ContainsKey(index))
        {
            return true;
        }

        var widget = TypedWidget.Delegate.Build(new BuildContext(this), index);
        if (widget == null)
        {
            return false;
        }

        var previousElement = after != null && _elementByRenderObject.TryGetValue(after, out var mapped)
            ? mapped
            : PreviousElementForIndex(index);

        var slot = new IndexedSlot<Element?>(index, previousElement);
        var child = UpdateChild(null, widget, slot);
        if (child == null)
        {
            return false;
        }

        AttachMappings(index, child);
        return true;
    }

    public void RemoveChild(RenderBox child)
    {
        if (!_elementByRenderObject.TryGetValue(child, out var element))
        {
            return;
        }

        RemoveMappings(element);
        DeactivateChild(element);
    }

    public void DidAdoptChild(RenderBox child)
    {
        if (!_elementByRenderObject.TryGetValue(child, out var element))
        {
            return;
        }

        if (!_indexByElement.TryGetValue(element, out int index))
        {
            return;
        }

        if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = index;
        }
    }

    public void SetDidUnderflow(bool value)
    {
        _didUnderflow = value;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("SliverMultiBoxAdaptorElement expects IndexedSlot.");
        }

        var renderBox = (RenderBox)child;
        TypedRenderObject.Insert(renderBox, (RenderBox?)indexedSlot.Value?.RenderObject);
        if (renderBox.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = indexedSlot.Index;
        }
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (newSlot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("SliverMultiBoxAdaptorElement expects IndexedSlot.");
        }

        var renderBox = (RenderBox)child;
        TypedRenderObject.Move(renderBox, (RenderBox?)indexedSlot.Value?.RenderObject);
        if (renderBox.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = indexedSlot.Index;
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        TypedRenderObject.Remove((RenderBox)child);
    }

    private void SyncChildWidgets()
    {
        RemapChildrenByKey();

        foreach (int index in _childElements.Keys.ToArray())
        {
            if (!_childElements.TryGetValue(index, out var oldChild))
            {
                continue;
            }

            var updatedWidget = TypedWidget.Delegate.Build(new BuildContext(this), index);
            if (updatedWidget == null)
            {
                RemoveMappings(oldChild);
                DeactivateChild(oldChild);
                continue;
            }

            var updatedChild = UpdateChild(oldChild, updatedWidget, new IndexedSlot<Element?>(index, PreviousElementForIndex(index)));
            if (updatedChild == null)
            {
                RemoveMappings(oldChild);
                continue;
            }

            if (!ReferenceEquals(updatedChild, oldChild))
            {
                RemoveMappings(oldChild);
                AttachMappings(index, updatedChild);
            }
            else
            {
                RefreshRenderObjectMapping(updatedChild);
            }
        }

        if (_didUnderflow)
        {
            TypedRenderObject.MarkNeedsLayout();
        }
    }

    private void RemapChildrenByKey()
    {
        var moves = new List<(int OldIndex, int NewIndex, Element Child)>();
        foreach (var (index, child) in _childElements)
        {
            if (child.Widget.Key is not { } key)
            {
                continue;
            }

            int? newIndex = TypedWidget.Delegate.FindIndexByKey(key);
            if (newIndex.HasValue && newIndex.Value >= 0 && newIndex.Value != index)
            {
                moves.Add((index, newIndex.Value, child));
            }
        }

        if (moves.Count == 0)
        {
            return;
        }

        var movedChildren = moves.Select(static move => move.Child).ToHashSet();
        foreach (var move in moves)
        {
            _childElements.Remove(move.OldIndex);
        }

        foreach (var move in moves)
        {
            if (_childElements.TryGetValue(move.NewIndex, out var displaced)
                && !movedChildren.Contains(displaced))
            {
                RemoveMappings(displaced);
                DeactivateChild(displaced);
            }

            _childElements[move.NewIndex] = move.Child;
            _indexByElement[move.Child] = move.NewIndex;
        }
    }

    private Element? PreviousElementForIndex(int index)
    {
        Element? previous = null;
        foreach (var pair in _childElements)
        {
            if (pair.Key >= index)
            {
                break;
            }

            if (IsActiveRenderListChild(pair.Value))
            {
                previous = pair.Value;
            }
        }

        return previous;
    }

    private static bool IsActiveRenderListChild(Element element)
    {
        if (element.RenderObject is not RenderBox renderBox)
        {
            return false;
        }

        if (renderBox.parentData is not SliverMultiBoxAdaptorParentData parentData)
        {
            return false;
        }

        return !parentData.KeptAlive;
    }

    private void AttachMappings(int index, Element child)
    {
        _childElements[index] = child;
        _indexByElement[child] = index;
        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject[renderBox] = child;
        }
    }

    private void RemoveMappings(Element child)
    {
        if (_indexByElement.TryGetValue(child, out int index))
        {
            _indexByElement.Remove(child);
            _childElements.Remove(index);
        }

        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject.Remove(renderBox);
        }
    }

    private void RefreshRenderObjectMapping(Element child)
    {
        foreach (var key in _elementByRenderObject.Where(pair => ReferenceEquals(pair.Value, child)).Select(pair => pair.Key).ToArray())
        {
            _elementByRenderObject.Remove(key);
        }

        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject[renderBox] = child;
        }
    }
}

public sealed class SliverList : SliverMultiBoxAdaptorWidget
{
    public SliverList(SliverChildDelegate @delegate, Key? key = null) : base(@delegate, key)
    {
    }

    public static SliverList FromChildren(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            key);
    }

    public static SliverList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        bool addAutomaticKeepAlives = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexCallback: semanticIndexCallback,
                semanticIndexOffset: semanticIndexOffset),
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverList();
    }
}

public sealed class SliverFixedExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverFixedExtentList(
        SliverChildDelegate @delegate,
        double itemExtent,
        Key? key = null) : base(@delegate, key)
    {
        if (itemExtent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        ItemExtent = itemExtent;
    }

    public double ItemExtent { get; }

    public static SliverFixedExtentList FromChildren(
        IReadOnlyList<Widget> children,
        double itemExtent,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            itemExtent,
            key);
    }

    public static SliverFixedExtentList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        double itemExtent,
        bool addAutomaticKeepAlives = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexCallback: semanticIndexCallback,
                semanticIndexOffset: semanticIndexOffset),
            itemExtent,
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFixedExtentList(ItemExtent);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverFixedExtentList)renderObject).ItemExtent = ItemExtent;
    }
}

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/sliver.dart (SliverVariedExtentList)
// flutter/packages/flutter/lib/src/widgets/sliver_prototype_extent_list.dart (SliverPrototypeExtentList)

/// <summary>
/// Places box children in a linear array and forces each child to the main-axis
/// extent returned by <see cref="ItemExtentBuilder"/>.
/// </summary>
public sealed class SliverVariedExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverVariedExtentList(
        SliverChildDelegate @delegate,
        ItemExtentBuilder itemExtentBuilder,
        Key? key = null) : base(@delegate, key)
    {
        ItemExtentBuilder = itemExtentBuilder ?? throw new ArgumentNullException(nameof(itemExtentBuilder));
    }

    public ItemExtentBuilder ItemExtentBuilder { get; }

    public static SliverVariedExtentList FromChildren(
        IReadOnlyList<Widget> children,
        ItemExtentBuilder itemExtentBuilder,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverVariedExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            itemExtentBuilder,
            key);
    }

    public static SliverVariedExtentList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        ItemExtentBuilder itemExtentBuilder,
        bool addAutomaticKeepAlives = true,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        if (childCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCount));
        }

        return new SliverVariedExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives,
                findChildIndexCallback:                 findChildIndexCallback),
            itemExtentBuilder,
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverVariedExtentList(ItemExtentBuilder);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverVariedExtentList)renderObject).ItemExtentBuilder = ItemExtentBuilder;
    }
}

/// <summary>
/// Places box children in a linear array and derives their common main-axis
/// extent from an offstage prototype child.
/// </summary>
public sealed class SliverPrototypeExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverPrototypeExtentList(
        SliverChildDelegate @delegate,
        Widget prototypeItem,
        Key? key = null) : base(@delegate, key)
    {
        PrototypeItem = prototypeItem ?? throw new ArgumentNullException(nameof(prototypeItem));
    }

    public Widget PrototypeItem { get; }

    public static SliverPrototypeExtentList FromChildren(
        IReadOnlyList<Widget> children,
        Widget prototypeItem,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverPrototypeExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            prototypeItem,
            key);
    }

    public static SliverPrototypeExtentList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        Widget prototypeItem,
        bool addAutomaticKeepAlives = true,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        if (childCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCount));
        }

        return new SliverPrototypeExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives,
                findChildIndexCallback:                 findChildIndexCallback),
            prototypeItem,
            key);
    }

    internal override Element CreateElement()
    {
        return new SliverPrototypeExtentListElement(this);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPrototypeExtentList();
    }
}

internal sealed class SliverPrototypeExtentListElement : SliverMultiBoxAdaptorElement
{
    private static readonly object PrototypeSlot = new();
    private Element? _prototype;

    public SliverPrototypeExtentListElement(SliverPrototypeExtentList widget) : base(widget)
    {
    }

    private SliverPrototypeExtentList PrototypeWidget => (SliverPrototypeExtentList)Widget;

    private RenderSliverPrototypeExtentList PrototypeRenderObject =>
        (RenderSliverPrototypeExtentList)TypedRenderObject;

    protected override void OnMount()
    {
        base.OnMount();
        _prototype = UpdateChild(_prototype, PrototypeWidget.PrototypeItem, PrototypeSlot);
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _prototype = UpdateChild(_prototype, PrototypeWidget.PrototypeItem, PrototypeSlot);
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_prototype != null)
        {
            visitor(_prototype);
        }

        base.VisitChildren(visitor);
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _prototype))
        {
            _prototype = null;
            return;
        }

        base.ForgetChild(child);
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(slot, PrototypeSlot))
        {
            PrototypeRenderObject.PrototypeChild = (RenderBox)child;
            return;
        }

        base.InsertRenderObjectChild(child, slot);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (ReferenceEquals(newSlot, PrototypeSlot))
        {
            throw new InvalidOperationException("A SliverPrototypeExtentList prototype cannot move.");
        }

        base.MoveRenderObjectChild(child, oldSlot, newSlot);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(child, PrototypeRenderObject.PrototypeChild))
        {
            PrototypeRenderObject.PrototypeChild = null;
            return;
        }

        base.RemoveRenderObjectChild(child, slot);
    }

    internal override void Unmount()
    {
        if (_prototype != null)
        {
            UnmountChild(_prototype);
            _prototype = null;
        }

        base.Unmount();
    }
}

public sealed class SliverVariableExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverVariableExtentList(SliverChildDelegate @delegate, SliverVariableExtentLayout layout, Key? key = null) : base(@delegate, key)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public SliverVariableExtentLayout Layout { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderSliverVariableExtentList(Layout);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) => ((RenderSliverVariableExtentList)renderObject).ExtentLayout = Layout;
}

public sealed class SliverGrid : SliverMultiBoxAdaptorWidget
{
    public SliverGrid(
        SliverChildDelegate @delegate,
        SliverGridDelegate gridDelegate,
        Key? key = null) : base(@delegate, key)
    {
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
    }

    public SliverGridDelegate GridDelegate { get; }

    public static SliverGrid FromChildren(
        IReadOnlyList<Widget> children,
        SliverGridDelegate gridDelegate,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            gridDelegate,
            key);
    }

    public static SliverGrid Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            gridDelegate,
            key);
    }

    public static SliverGrid Count(
        int crossAxisCount,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    public static SliverGrid Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverGrid(GridDelegate);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverGrid)renderObject).GridDelegate = GridDelegate;
    }
}

public class CustomScrollView : StatelessWidget
{
    public CustomScrollView(
        IReadOnlyList<Widget> slivers,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        bool shrinkWrap = false,
        double anchor = 0.0,
        Key? center = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        int? semanticChildCount = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null,
        Clip clipBehavior = Clip.HardEdge) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        if (!double.IsFinite(anchor) || anchor < 0.0 || anchor > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        if (semanticChildCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semanticChildCount));
        }

        SemanticChildCount = semanticChildCount;
        Slivers = slivers;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
        ShrinkWrap = shrinkWrap;
        Anchor = anchor;
        Center = center;
        DragStartBehavior = dragStartBehavior;
        RestorationId = restorationId;
        HitTestBehavior = hitTestBehavior;
        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<Widget> Slivers { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public bool ShrinkWrap { get; }

    public double Anchor { get; }

    /// <summary>
    /// The key of the first sliver laid out in the forward growth direction; every sliver before it
    /// grows in the reverse direction and occupies negative scroll offsets.
    /// </summary>
    public Key? Center { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public string? RestorationId { get; }

    /// <summary>
    /// The number of children an assistive technology should be told this view can show, or
    /// <c>null</c> when the count is unknown or unbounded.
    /// </summary>
    public int? SemanticChildCount { get; }

    public Clip ClipBehavior { get; }

    /// <summary>How the scroll view should behave during hit testing.</summary>
    public HitTestBehavior HitTestBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        bool usePrimary = Primary
                          ?? (Controller == null
                              && PrimaryScrollController.ShouldInherit(context, ScrollDirection));
        ScrollController? effectiveController = Controller;
        if (effectiveController == null && usePrimary)
        {
            effectiveController = PrimaryScrollController.MaybeOf(context);
        }

        Widget scrollable = new Scrollable(
            slivers: Slivers,
            axis: ScrollDirection,
            reverse: Reverse,
            controller: effectiveController,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            keyboardDismissBehavior: KeyboardDismissBehavior,
            cacheExtent: CacheExtent,
            cacheExtentStyle: CacheExtentStyle,
            shrinkWrap: ShrinkWrap,
            anchor: Anchor,
            center: Center,
            dragStartBehavior: DragStartBehavior,
            restorationId: RestorationId,
            semanticChildCount: SemanticChildCount,
            hitTestBehavior: HitTestBehavior,
            clipBehavior: ClipBehavior)
        {
            ViewportBuilder = HasCustomViewport
                ? (viewportContext, offset) => BuildViewport(
                    viewportContext,
                    offset,
                    GetDirection(viewportContext),
                    Slivers)
                : null,
        };
        // Further descendant scroll views must not inherit the same PrimaryScrollController.
        return usePrimary && effectiveController != null
            ? PrimaryScrollController.None(scrollable)
            : scrollable;
    }

    /// <summary>
    /// Whether <see cref="BuildViewport"/> is overridden, so the scrollable must be handed a viewport
    /// builder instead of composing its own viewport.
    /// </summary>
    protected virtual bool HasCustomViewport => false;

    /// <summary>The axis direction this view scrolls in.</summary>
    /// <remarks>Flutter's <c>ScrollView.getDirection</c>.</remarks>
    protected AxisDirection GetDirection(BuildContext context)
    {
        return ScrollDirectionUtils.GetAxisDirectionFromAxisReverseAndDirectionality(
            context,
            ScrollDirection,
            Reverse);
    }

    /// <summary>
    /// Builds the viewport that holds <paramref name="slivers"/>. Subclasses override this together
    /// with <see cref="HasCustomViewport"/> to supply their own viewport render object.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollView.buildViewport</c>.</remarks>
    protected virtual Widget BuildViewport(
        BuildContext context,
        ViewportOffset offset,
        AxisDirection axisDirection,
        IReadOnlyList<Widget> slivers)
    {
        if (ShrinkWrap)
        {
            return new ShrinkWrappingViewport(
                offset: offset,
                axisDirection: axisDirection,
                clipBehavior: ClipBehavior,
                slivers: slivers);
        }

        return new Viewport(
            offset: offset,
            axisDirection: axisDirection,
            anchor: Anchor,
            center: Center,
            clipBehavior: ClipBehavior,
            slivers: slivers);
    }
}

public sealed class SingleChildScrollView : StatelessWidget
{
    public SingleChildScrollView(
        Widget child,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Thickness? padding = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Key? key = null) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        Child = child;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
        Padding = padding;
        KeyboardDismissBehavior = keyboardDismissBehavior;
    }

    public Widget Child { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public Thickness? Padding { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        Widget child = Child;
        if (Padding.HasValue)
        {
            child = new Padding(Padding.Value, child);
        }

        bool usePrimary = Primary
                          ?? (Controller == null
                              && PrimaryScrollController.ShouldInherit(context, ScrollDirection));
        ScrollController? effectiveController = usePrimary
            ? PrimaryScrollController.MaybeOf(context)
            : Controller;
        Widget scrollable = new Scrollable(
            child: child,
            axis: ScrollDirection,
            reverse: Reverse,
            controller: effectiveController,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            keyboardDismissBehavior: KeyboardDismissBehavior,
            cacheExtent: CacheExtent,
            cacheExtentStyle: CacheExtentStyle,
            shrinkWrap: true)
        {
            UseSingleChildViewport = true,
        };
        // Further descendant scroll views must not inherit the same PrimaryScrollController.
        return usePrimary && effectiveController != null
            ? PrimaryScrollController.None(scrollable)
            : scrollable;
    }
}

public sealed class ListView : StatelessWidget
{
    private readonly IReadOnlyList<Widget>? _children;
    private readonly IndexedWidgetBuilder? _itemBuilder;
    private readonly IndexedWidgetBuilder? _separatorBuilder;
    private readonly int _itemCount;
    private readonly double? _itemExtent;
    private readonly Thickness _padding;
    private readonly bool _addAutomaticKeepAlives;
    private readonly bool _shrinkWrap;
    private readonly double _cacheExtent;
    private readonly CacheExtentStyle _cacheExtentStyle;

    public ListView(
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        int? semanticChildCount = null,
        Key? key = null,
        bool shrinkWrap = false) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        if (itemExtent.HasValue && itemExtent.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        _children = children ?? [];
        SemanticChildCount = semanticChildCount ?? _children.Count;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        _itemExtent = itemExtent;
        _padding = padding ?? default;
        _shrinkWrap = shrinkWrap;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    private ListView(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        IndexedWidgetBuilder? separatorBuilder,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        bool? primary,
        ScrollPhysics? physics,
        ScrollBehavior? scrollBehavior,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior,
        double? itemExtent,
        Thickness? padding,
        bool shrinkWrap,
        bool addAutomaticKeepAlives,
        double cacheExtent,
        CacheExtentStyle cacheExtentStyle,
        int? semanticChildCount,
        Key? key) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        if (semanticChildCount is < 0 || semanticChildCount > itemCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(semanticChildCount),
                "semanticChildCount must be between 0 and itemCount.");
        }

        SemanticChildCount = separatorBuilder is null ? semanticChildCount ?? itemCount : itemCount;

        if (itemExtent.HasValue && itemExtent.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        _itemCount = itemCount;
        _itemBuilder = itemBuilder;
        _separatorBuilder = separatorBuilder;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        _itemExtent = itemExtent;
        _padding = padding ?? default;
        _shrinkWrap = shrinkWrap;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public static ListView Builder(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        int? semanticChildCount = null,
        Key? key = null,
        bool shrinkWrap = false)
    {
        return new ListView(
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            separatorBuilder: null,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            itemExtent: itemExtent,
            padding: padding,
            shrinkWrap: shrinkWrap,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            semanticChildCount: semanticChildCount,
            key: key);
    }

    public static ListView Separated(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        IndexedWidgetBuilder separatorBuilder,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null,
        bool shrinkWrap = false)
    {
        return new ListView(
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            separatorBuilder: separatorBuilder,
            semanticChildCount: null,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            itemExtent: itemExtent,
            padding: padding,
            shrinkWrap: shrinkWrap,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget sliver;
        if (_itemBuilder != null)
        {
            int childCount = _itemCount;
            IndexedWidgetBuilder effectiveItemBuilder = _itemBuilder;

            if (_separatorBuilder != null)
            {
                var itemBuilder = _itemBuilder;
                var separatorBuilder = _separatorBuilder;
                childCount = SeparatedChildCount(_itemCount);
                effectiveItemBuilder = (buildContext, index) =>
                {
                    int itemIndex = index / 2;
                    return index % 2 == 0
                        ? itemBuilder(buildContext, itemIndex)
                        : separatorBuilder(buildContext, itemIndex);
                };
            }

            // A separated list gives its separators no semantic index at all, so item n keeps index n.
            SemanticIndexCallback? semanticIndexCallback = _separatorBuilder is null
                ? null
                : static (_, localIndex) => localIndex % 2 == 0 ? localIndex / 2 : null;
            sliver = _itemExtent.HasValue
                ? SliverFixedExtentList.Builder(
                    childCount,
                    effectiveItemBuilder,
                    _itemExtent.Value,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives,
                    semanticIndexCallback: semanticIndexCallback)
                : SliverList.Builder(
                    childCount,
                    effectiveItemBuilder,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives,
                    semanticIndexCallback: semanticIndexCallback);
        }
        else
        {
            sliver = _itemExtent.HasValue
                ? SliverFixedExtentList.FromChildren(
                    _children ?? [],
                    _itemExtent.Value,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives)
                : SliverList.FromChildren(
                    _children ?? [],
                    addAutomaticKeepAlives: _addAutomaticKeepAlives);
        }

        if (HasNonZeroPadding(_padding))
        {
            sliver = new SliverPadding(_padding, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: ScrollDirection,
            reverse: Reverse,
            controller: Controller,
            primary: Primary,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            keyboardDismissBehavior: KeyboardDismissBehavior,
            cacheExtent: _cacheExtent,
            cacheExtentStyle: _cacheExtentStyle,
            semanticChildCount: SemanticChildCount,
            shrinkWrap: _shrinkWrap);
    }

    /// <summary>
    /// The number of children reported to assistive technologies. Defaults to the child count for a
    /// list built from an explicit child list, and to <c>itemCount</c> for the builder constructors.
    /// </summary>
    public int? SemanticChildCount { get; }

    private static int SeparatedChildCount(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0;
        }

        return itemCount * 2 - 1;
    }

    private static bool HasNonZeroPadding(Thickness padding)
    {
        return Math.Abs(padding.Left) > 0.0001
               || Math.Abs(padding.Top) > 0.0001
               || Math.Abs(padding.Right) > 0.0001
               || Math.Abs(padding.Bottom) > 0.0001;
    }
}

public sealed class GridView : StatelessWidget
{
    private readonly SliverGridDelegate _gridDelegate;
    private readonly IReadOnlyList<Widget>? _children;
    private readonly IndexedWidgetBuilder? _itemBuilder;
    private readonly int _itemCount;
    private readonly Thickness _padding;
    private readonly bool _addAutomaticKeepAlives;
    private readonly double _cacheExtent;
    private readonly CacheExtentStyle _cacheExtentStyle;

    public GridView(
        SliverGridDelegate gridDelegate,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        _children = children ?? [];
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    private GridView(
        SliverGridDelegate gridDelegate,
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        bool? primary,
        ScrollPhysics? physics,
        ScrollBehavior? scrollBehavior,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior,
        Thickness? padding,
        bool addAutomaticKeepAlives,
        double cacheExtent,
        CacheExtentStyle cacheExtentStyle,
        Key? key) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        _itemCount = itemCount;
        _itemBuilder = itemBuilder;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public static GridView Builder(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: gridDelegate,
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public static GridView Count(
        int crossAxisCount,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Thickness? padding = null,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public static GridView Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Thickness? padding = null,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget sliver = _itemBuilder != null
            ? SliverGrid.Builder(
                childCount: _itemCount,
                itemBuilder: _itemBuilder,
                gridDelegate: _gridDelegate,
                addAutomaticKeepAlives: _addAutomaticKeepAlives)
            : SliverGrid.FromChildren(
                _children ?? [],
                _gridDelegate,
                addAutomaticKeepAlives: _addAutomaticKeepAlives);

        if (HasNonZeroPadding(_padding))
        {
            sliver = new SliverPadding(_padding, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: ScrollDirection,
            reverse: Reverse,
            controller: Controller,
            primary: Primary,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            keyboardDismissBehavior: KeyboardDismissBehavior,
            cacheExtent: _cacheExtent,
            cacheExtentStyle: _cacheExtentStyle);
    }

    private static bool HasNonZeroPadding(Thickness padding)
    {
        return Math.Abs(padding.Left) > 0.0001
               || Math.Abs(padding.Top) > 0.0001
               || Math.Abs(padding.Right) > 0.0001
               || Math.Abs(padding.Bottom) > 0.0001;
    }
}
