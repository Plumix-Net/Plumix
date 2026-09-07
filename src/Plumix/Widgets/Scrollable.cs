using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable.dart (adapted)
// Adapted only in the drag handlers, which dispatch the scroll notifications Dart's ScrollActivity
// layer owns; see docs/ai/DIVERGENCES.md.

namespace Plumix.Widgets;

/// <summary>
/// Signature used by <see cref="Scrollable"/> to build the viewport through which the scrollable
/// content is displayed.
/// </summary>
public delegate Widget ViewportBuilder(BuildContext context, ViewportOffset position);

/// <summary>
/// A widget that scrolls.
/// </summary>
/// <remarks>
/// <see cref="Scrollable"/> implements the interaction model for a scrollable widget, including
/// gesture recognition, but does not have an opinion about how the viewport, which actually
/// displays the children, is constructed. Instead of using <see cref="Scrollable"/> directly,
/// consider using <see cref="ListView"/> or <see cref="GridView"/>, which combine scrolling,
/// viewporting, and a layout model.
/// </remarks>
public class Scrollable : StatefulWidget
{
    public Scrollable(
        ViewportBuilder viewportBuilder,
        AxisDirection axisDirection = AxisDirection.Down,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        ScrollIncrementCalculator? incrementCalculator = null,
        bool excludeFromSemantics = false,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        ScrollBehavior? scrollBehavior = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(viewportBuilder);
        if (semanticChildCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semanticChildCount));
        }

        ViewportBuilder = viewportBuilder;
        AxisDirection = axisDirection;
        Controller = controller;
        Physics = physics;
        IncrementCalculator = incrementCalculator;
        ExcludeFromSemantics = excludeFromSemantics;
        SemanticChildCount = semanticChildCount;
        DragStartBehavior = dragStartBehavior;
        RestorationId = restorationId;
        ScrollBehavior = scrollBehavior;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
    }

    /// <summary>The direction in which this widget scrolls.</summary>
    public AxisDirection AxisDirection { get; }

    /// <summary>An object that can be used to control the position to which this widget is scrolled.</summary>
    public ScrollController? Controller { get; }

    /// <summary>
    /// How the widget should respond to user input.
    /// </summary>
    /// <remarks>
    /// New physics only take effect if the <em>class</em> of the provided object changes.
    /// </remarks>
    public ScrollPhysics? Physics { get; }

    /// <summary>Builds the viewport through which the scrollable content is displayed.</summary>
    public ViewportBuilder ViewportBuilder { get; }

    /// <summary>
    /// Computes the distance a keyboard-driven line or page scroll moves, or <c>null</c> to use the
    /// defaults of 80% of the scroll window for a page and 50 logical pixels for a line.
    /// </summary>
    public ScrollIncrementCalculator? IncrementCalculator { get; }

    /// <summary>Whether the scroll actions introduced by this widget are exposed in the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>
    /// How the <see cref="Scrollable"/> should behave during hit testing when deciding how the
    /// scroll's <see cref="RawGestureDetector"/> competes with other gestures.
    /// </summary>
    public HitTestBehavior HitTestBehavior { get; }

    /// <summary>
    /// The number of children that will contribute semantic information, or <c>null</c> when the
    /// count is unknown or unbounded.
    /// </summary>
    public int? SemanticChildCount { get; }

    /// <summary>Determines the way that drag start behavior is handled.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>Restoration ID to save and restore the scroll offset of the scrollable.</summary>
    public string? RestorationId { get; }

    /// <summary>A <see cref="ScrollBehavior"/> that will be applied to this widget individually.</summary>
    public ScrollBehavior? ScrollBehavior { get; }

    /// <summary>
    /// The content will be clipped (or not) according to this option.
    /// </summary>
    /// <remarks>
    /// This is passed to decorators in <see cref="ScrollableDetails"/>; it does not clip the
    /// scrollable itself.
    /// </remarks>
    public Clip ClipBehavior { get; }

    /// <summary>The axis along which the scroll view scrolls.</summary>
    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

    public override State CreateState() => new ScrollableState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<AxisDirection>("axisDirection", AxisDirection));
        properties.Add(new DiagnosticsProperty<ScrollPhysics>("physics", Physics));
        properties.Add(new StringProperty("restorationId", RestorationId));
    }

    /// <summary>
    /// The state from the closest instance of this class that encloses the given context, or null
    /// when there is none.
    /// </summary>
    /// <param name="context">The context to search from.</param>
    /// <param name="axis">
    /// When given, scrollables on other axes are skipped and the search continues outwards.
    /// </param>
    /// <remarks>
    /// Calling this method will create a dependency on the closest <see cref="Scrollable"/> in
    /// <paramref name="context"/>, if there is one.
    /// </remarks>
    public static ScrollableState? MaybeOf(BuildContext context, Axis? axis = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        BuildContext originalContext = context;
        InheritedElement? element = context.GetElementForInheritedWidgetOfExactType<ScrollableScope>();
        while (element is not null)
        {
            ScrollableState scrollable = ((ScrollableScope)element.Widget).Scrollable;
            if (axis is null
                || ScrollDirectionUtils.AxisDirectionToAxis(scrollable.AxisDirection) == axis)
            {
                originalContext.DependOnInheritedElement(element);
                return scrollable;
            }

            context = scrollable.Context;
            element = context.GetElementForInheritedWidgetOfExactType<ScrollableScope>();
        }

        return null;
    }

    /// <inheritdoc cref="MaybeOf(BuildContext, Axis?)"/>
    /// <exception cref="InvalidOperationException">There is no enclosing scrollable.</exception>
    public static ScrollableState Of(BuildContext context, Axis? axis = null)
    {
        ScrollableState? scrollableState = MaybeOf(context, axis);
        if (scrollableState is null)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "Scrollable.Of() was called with a context that does not contain a Scrollable widget."),
                new ErrorDescription(
                    "No Scrollable widget ancestor could be found "
                    + (axis is null ? string.Empty : $"for the provided Axis: {axis} ")
                    + "starting from the context that was passed to Scrollable.Of(). This can happen "
                    + "because you are using a widget that looks for a Scrollable ancestor, but no such "
                    + $"ancestor exists.\nThe context used was:\n  {context}"),
                .. axis is null
                    ? Array.Empty<DiagnosticsNode>()
                    : new DiagnosticsNode[]
                    {
                        new ErrorHint(
                            "When specifying an axis, this method will only look for a Scrollable that "
                            + "matches the given Axis."),
                    },
            ]);
        }

        return scrollableState;
    }

    /// <summary>
    /// Whether the enclosing scrollable is scrolling fast enough that expensive frame-bound work
    /// (such as decoding an image) should be deferred to a later frame.
    /// </summary>
    /// <remarks>Creates no dependency; returns false when there is no enclosing scrollable.</remarks>
    public static bool RecommendDeferredLoadingForContext(BuildContext context, Axis? axis = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var widget = context.GetElementForInheritedWidgetOfExactType<ScrollableScope>()?.Widget
                as ScrollableScope;
        while (widget is not null)
        {
            if (axis is null
                || ScrollDirectionUtils.AxisDirectionToAxis(widget.Scrollable.AxisDirection) == axis)
            {
                return widget.Position.RecommendDeferredLoading(context);
            }

            context = widget.Scrollable.Context;
            widget = context.GetElementForInheritedWidgetOfExactType<ScrollableScope>()?.Widget
                as ScrollableScope;
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
    /// (Flutter's <c>targetRenderObject</c>).
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
        Curve effectiveCurve = curve ?? Curves.Ease;
        var futures = new List<Task>();
        RenderObject? targetRenderObject = null;
        ScrollableState? scrollable = MaybeOf(context);
        while (scrollable is not null)
        {
            if (context.FindRenderObject() is not { } renderObject)
            {
                break;
            }

            (IReadOnlyList<Task> newFutures, ScrollableState next) = scrollable.PerformEnsureVisibleInternal(
                renderObject,
                alignment,
                effectiveDuration,
                effectiveCurve,
                alignmentPolicy,
                targetRenderObject);
            futures.AddRange(newFutures);
            targetRenderObject ??= renderObject;

            context = next.Context;
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
    public class ScrollableState : RestorationState, IScrollContext
    {
        private readonly RestorableScrollOffset _persistedScrollOffset = new();
        private bool _persistedScrollOffsetRegistered;

        // Keys are records, so the identity has to come from a per-state sentinel: two scrollables
        // must never share one global key.
        private readonly GlobalObjectKey<State> _scrollSemanticsKey = new(new object());
        private protected readonly GlobalObjectKey<RawGestureDetectorState> _gestureDetectorKey =
            new(new object());
        private readonly GlobalObjectKey<State> _ignorePointerKey = new(new object());

        private ScrollPosition? _position;
        private ScrollPhysics? _physics;
        private ScrollBehavior _configuration = null!;
        private ScrollController? _fallbackScrollController;
        private DeviceGestureSettings? _mediaQueryGestureSettings;
        private double _devicePixelRatio = 1.0;

        private protected IReadOnlyDictionary<Type, IGestureRecognizerFactory> _gestureRecognizers =
            RawGestureDetector.NoGestures;
        private bool _shouldIgnorePointer;
        private protected bool? _lastCanDrag;
        private protected Axis? _lastAxisDirection;

        private IScrollHoldController? _hold;
        private IDrag? _drag;

        private protected Scrollable CurrentWidget => (Scrollable)Element.Widget;

        /// <summary>The ambient scroll behavior this scrollable resolved.</summary>
        private protected ScrollBehavior Configuration => _configuration;

        /// <summary>The controller the position is attached to: the widget's, or the fallback.</summary>
        private protected ScrollController EffectiveScrollController =>
            CurrentWidget.Controller ?? _fallbackScrollController!;

        public ScrollPosition Position => _position!;

        /// <summary>The physics this scrollable resolved from its widget and ambient behavior.</summary>
        public ScrollPhysics? ResolvedPhysics => _physics;

        /// <summary>
        /// The distance from the scroll origin to the leading edge of the viewport, expressed as an
        /// offset in the scrollable's own axis direction.
        /// </summary>
        public Point DeltaToScrollOrigin => AxisDirection switch
        {
            AxisDirection.Up => new Point(0, -Position.Pixels),
            AxisDirection.Down => new Point(0, Position.Pixels),
            AxisDirection.Left => new Point(-Position.Pixels, 0),
            _ => new Point(Position.Pixels, 0),
        };

        /// <summary>The direction in which the widget scrolls.</summary>
        public AxisDirection AxisDirection => CurrentWidget.AxisDirection;

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

        /// <summary>The scrollable's keyboard scroll-distance calculator, if it was given one.</summary>
        public ScrollIncrementCalculator? IncrementCalculator => CurrentWidget.IncrementCalculator;

        /// <inheritdoc />
        protected override string? RestorationId => CurrentWidget.RestorationId;

        /// <inheritdoc />
        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(_persistedScrollOffset, "offset");
            _persistedScrollOffsetRegistered = true;
            Debug.Assert(_position is not null);
            if (_persistedScrollOffset.Value is { } offset)
            {
                Position.RestoreOffset(offset, initialRestore: initialRestore);
            }
        }

        /// <summary>
        /// Persists the offset the position reports when scrolling ends.
        /// </summary>
        /// <remarks>
        /// Flutter's <c>ScrollableState.saveOffset</c>: the value goes into the restoration bucket and
        /// the manager is flushed by hand, because no frame necessarily follows the end of a scroll.
        /// </remarks>
        public void SaveOffset(double offset)
        {
            Debug.Assert(RestorationSerialization.DebugIsSerializableForRestoration(offset));
            if (!_persistedScrollOffsetRegistered)
            {
                return;
            }

            _persistedScrollOffset.Value = offset;
            // Because we change the value of a restorable property, we have to make sure the data is
            // flushed to the engine; a scroll end is not necessarily followed by a frame.
            RestorationManager.Instance.FlushData();
        }

        /// <summary>
        /// Recreates the scroll position for the current physics and controller.
        /// </summary>
        /// <remarks>
        /// Flutter's <c>ScrollableState._updatePosition</c>. Only call this from places that will
        /// definitely trigger a rebuild.
        /// </remarks>
        private void UpdatePosition()
        {
            _configuration = CurrentWidget.ScrollBehavior ?? ScrollConfiguration.Of(Context);
            ScrollPhysics? physicsFromWidget =
                CurrentWidget.Physics ?? CurrentWidget.ScrollBehavior?.GetScrollPhysics(Context);
            _physics = _configuration.GetScrollPhysics(Context);
            _physics = physicsFromWidget?.ApplyTo(_physics) ?? _physics;

            ScrollPosition? oldPosition = _position;
            if (oldPosition is not null)
            {
                EffectiveScrollController.Detach(oldPosition);
                // It's important that we not dispose the old position until after the viewport has
                // had a chance to unregister its listeners from the old position. So, schedule a
                // microtask to do it.
                Scheduler.ScheduleMicrotask(oldPosition.Dispose);
            }

            _position = EffectiveScrollController.CreateScrollPosition(_physics!, this, oldPosition);
            Debug.Assert(_position is not null);
            EffectiveScrollController.Attach(Position);
        }

        public override void InitState()
        {
            if (CurrentWidget.Controller is null)
            {
                _fallbackScrollController = new ScrollController();
            }

            base.InitState();
        }

        public override void DidChangeDependencies()
        {
            _mediaQueryGestureSettings = MediaQuery.MaybeGestureSettingsOf(Context);
            // Ballistic tolerances are expressed in device pixels, so the physics need the view's ratio.
            _devicePixelRatio = MediaQuery.MaybeDevicePixelRatioOf(Context) ?? 1.0;
            UpdatePosition();
            base.DidChangeDependencies();
        }

        /// <summary>
        /// Whether a widget update changed anything the position was built from.
        /// </summary>
        /// <remarks>
        /// Flutter's <c>ScrollableState._shouldUpdatePosition</c> walks both physics chains comparing
        /// runtime types, because a widget that rebuilds its physics every build (<see cref="PageView"/>
        /// does) must not replace its position every frame.
        /// </remarks>
        private bool ShouldUpdatePosition(Scrollable oldWidget)
        {
            Scrollable current = CurrentWidget;
            if ((current.ScrollBehavior is null) != (oldWidget.ScrollBehavior is null))
            {
                return true;
            }

            if (current.ScrollBehavior is not null
                && oldWidget.ScrollBehavior is not null
                && current.ScrollBehavior.ShouldNotify(oldWidget.ScrollBehavior))
            {
                return true;
            }

            ScrollPhysics? newPhysics = current.Physics ?? current.ScrollBehavior?.GetScrollPhysics(Context);
            ScrollPhysics? oldPhysics = oldWidget.Physics ?? oldWidget.ScrollBehavior?.GetScrollPhysics(Context);
            do
            {
                if (newPhysics?.GetType() != oldPhysics?.GetType())
                {
                    return true;
                }

                newPhysics = newPhysics?.Parent;
                oldPhysics = oldPhysics?.Parent;
            }
            while (newPhysics is not null || oldPhysics is not null);

            return current.Controller?.GetType() != oldWidget.Controller?.GetType();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldScrollable = (Scrollable)oldWidget;
            if (!ReferenceEquals(CurrentWidget.Controller, oldScrollable.Controller))
            {
                if (oldScrollable.Controller is null)
                {
                    // The old controller was the fallback; the new widget brought its own.
                    _fallbackScrollController!.Detach(Position);
                    _fallbackScrollController.Dispose();
                    _fallbackScrollController = null;
                }
                else
                {
                    oldScrollable.Controller.Detach(Position);
                    if (CurrentWidget.Controller is null)
                    {
                        _fallbackScrollController = new ScrollController();
                    }
                }

                EffectiveScrollController.Attach(Position);
            }

            if (ShouldUpdatePosition(oldScrollable))
            {
                UpdatePosition();
            }
        }

        public override void Dispose()
        {
            if (CurrentWidget.Controller is not null)
            {
                CurrentWidget.Controller.Detach(Position);
            }
            else
            {
                _fallbackScrollController?.Detach(Position);
                _fallbackScrollController?.Dispose();
            }

            _position?.Dispose();
            _persistedScrollOffset.Dispose();
            base.Dispose();
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
        /// Rebuilds the drag recognizer map and hands it to the detector. Turning dragging off also
        /// cancels any hold or drag in flight, so a physics change mid-gesture cannot leave the
        /// position captured.
        /// </summary>
        /// <remarks>Flutter's <c>ScrollableState.setCanDrag</c>.</remarks>
        public virtual void SetCanDrag(bool value)
        {
            if (value == _lastCanDrag && (!value || CurrentWidget.Axis == _lastAxisDirection))
            {
                return;
            }

            if (!value)
            {
                _gestureRecognizers = RawGestureDetector.NoGestures;
                // Cancel the active hold/drag (if any) because the recognizers are about to be
                // disposed by the RawGestureDetector, so no pointer up will arrive to cancel them.
                HandleDragCancel();
            }
            else
            {
                _gestureRecognizers = CurrentWidget.Axis == Axis.Vertical
                    ? BuildDragRecognizers(() =>
                        new VerticalDragGestureRecognizer { SupportedDevices = _configuration.DragDevices })
                    : BuildDragRecognizers(() =>
                        new HorizontalDragGestureRecognizer { SupportedDevices = _configuration.DragDevices });
            }

            _lastCanDrag = value;
            _lastAxisDirection = CurrentWidget.Axis;
            // Applied straight away rather than through a rebuild: the physics can change their mind
            // during layout, and the next pointer down must already see the new registration.
            _gestureDetectorKey.CurrentState?.ReplaceGestureRecognizers(_gestureRecognizers);
        }

        /// <summary>
        /// The one-entry recognizer map a scrollable registers for its drag axis, configured exactly
        /// as Dart's <c>setCanDrag</c> configures it.
        /// </summary>
        private IReadOnlyDictionary<Type, IGestureRecognizerFactory> BuildDragRecognizers<TRecognizer>(
            Func<TRecognizer> constructor)
            where TRecognizer : DragGestureRecognizer
        {
            return new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TRecognizer)] = new GestureRecognizerFactoryWithHandlers<TRecognizer>(
                    constructor,
                    instance =>
                    {
                        instance.OnDown = HandleDragDown;
                        instance.OnStart = HandleDragStart;
                        instance.OnUpdate = HandleDragUpdate;
                        instance.OnEnd = HandleDragEnd;
                        instance.OnCancel = HandleDragCancel;
                        instance.MinFlingDistance = _physics?.MinFlingDistance;
                        instance.MinFlingVelocity = _physics?.MinFlingVelocity;
                        instance.MaxFlingVelocity = _physics?.MaxFlingVelocity;
                        instance.VelocityTrackerBuilder = _configuration.VelocityTrackerBuilder(Context);
                        instance.DragStartBehavior = CurrentWidget.DragStartBehavior;
                        instance.MultitouchDragStrategy = _configuration.GetMultitouchDragStrategy(Context);
                        instance.GestureSettings = _mediaQueryGestureSettings;
                        instance.SupportedDevices = _configuration.DragDevices;
                    }),
            };
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

        private protected virtual void HandleDragDown(DragDownDetails details)
        {
            Debug.Assert(_drag is null);
            Debug.Assert(_hold is null);
            _hold = Position.Hold(DisposeHold);
        }

        /// <summary>
        /// Replays a drag callback on this scrollable from another one. Flutter's two-dimensional
        /// outer dimension calls its peer's private handlers directly; C#'s access rules do not
        /// reach a sibling instance's <c>private protected</c> members, so the forwarding goes
        /// through these.
        /// </summary>
        internal void ForwardDragDown(DragDownDetails details) => HandleDragDown(details);

        /// <inheritdoc cref="ForwardDragDown"/>
        internal void ForwardDragStart(DragStartDetails details) => HandleDragStart(details);

        /// <inheritdoc cref="ForwardDragDown"/>
        internal void ForwardDragUpdate(DragUpdateDetails details) => HandleDragUpdate(details);

        /// <inheritdoc cref="ForwardDragDown"/>
        internal void ForwardDragEnd(DragEndDetails details) => HandleDragEnd(details);

        /// <inheritdoc cref="ForwardDragDown"/>
        internal void ForwardDragCancel() => HandleDragCancel();

        private void DisposeHold()
        {
            _hold = null;
        }

        private void DisposeDrag()
        {
            _drag = null;
        }

        private protected virtual void HandleDragStart(DragStartDetails details)
        {
            // It's possible for _hold to become null between _handleDragDown and _handleDragStart,
            // for example if some user code calls JumpTo or similar.
            _drag = Position.Drag(details, DisposeDrag);
            Debug.Assert(_drag is not null);
            Debug.Assert(_hold is null);
        }

        private protected virtual void HandleDragUpdate(DragUpdateDetails details)
        {
            // _drag might be null if the drag activity ended and called _disposeDrag.
            Debug.Assert(_hold is null || _drag is null);
            _drag?.Update(details);
        }

        private protected virtual void HandleDragEnd(DragEndDetails details)
        {
            // _drag might be null if the drag activity ended and called _disposeDrag.
            Debug.Assert(_hold is null || _drag is null);
            _drag?.End(details);
            Debug.Assert(_drag is null);
        }

        private protected virtual void HandleDragCancel()
        {
            if (_gestureDetectorKey.CurrentContext is null)
            {
                // The cancel was caused by the GestureDetector getting disposed; this state is about
                // to be disposed too and should not do any work.
                return;
            }

            Debug.Assert(_hold is null || _drag is null);
            _hold?.Cancel();
            _drag?.Cancel();
            Debug.Assert(_hold is null);
            Debug.Assert(_drag is null);
        }

        private double TargetScrollOffsetForPointerScroll(double delta)
        {
            return Math.Min(
                Math.Max(Position.Pixels + delta, Position.MinScrollExtent),
                Position.MaxScrollExtent);
        }

        private double PointerSignalEventDelta(PointerScrollEvent @event)
        {
            bool flipAxes = _configuration.PointerAxisModifiers.Any(IsLogicalKeyPressed)
                            // Axes are only flipped for physical mouse wheel input; trackpads should
                            // not flip the axes.
                            && @event.Kind == PointerDeviceKind.Mouse;
            Axis axis = flipAxes
                ? CurrentWidget.Axis == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal
                : CurrentWidget.Axis;
            double delta = axis == Axis.Horizontal ? @event.ScrollDelta.X : @event.ScrollDelta.Y;
            return ScrollDirectionUtils.AxisDirectionIsReversed(CurrentWidget.AxisDirection) ? -delta : delta;
        }

        private void ReceivedPointerSignal(PointerSignalEvent @event)
        {
            // Interest is expressed through the pointer signal resolver, so only the innermost
            // hit-tested scrollable actually scrolls.
            switch (@event)
            {
                case PointerScrollEvent scroll when _position is not null:
                {
                    if (_physics is not null && !_physics.ShouldAcceptUserOffset(Position))
                    {
                        return;
                    }

                    double delta = PointerSignalEventDelta(scroll);
                    double targetScrollOffset = TargetScrollOffsetForPointerScroll(delta);
                    // Only express interest in the event if it would actually result in a scroll.
                    if (delta != 0.0 && targetScrollOffset != Position.Pixels)
                    {
                        GestureBinding.Instance.PointerSignalResolver.Register(scroll, HandlePointerScroll);
                    }

                    break;
                }
                case PointerScrollInertiaCancelEvent:
                    Position.PointerScroll(0.0);
                    // Don't use the pointer signal resolver, all hit-tested scrollables should stop.
                    break;
            }
        }

        private void HandlePointerScroll(PointerSignalEvent @event)
        {
            var scroll = (PointerScrollEvent)@event;
            double delta = PointerSignalEventDelta(scroll);
            double targetScrollOffset = TargetScrollOffsetForPointerScroll(delta);
            if (delta != 0.0 && targetScrollOffset != Position.Pixels)
            {
                // The start/update/end notifications are dispatched by the position itself, exactly
                // like Flutter's `ScrollPositionWithSingleContext.pointerScroll`.
                Position.PointerScroll(delta);
                // Tell the host this scrollable handled the event, so the platform default (for
                // example native page scrolling on the web) does not also run.
                scroll.Respond(allowPlatformDefault: false);
            }
        }

        /// <summary>
        /// Re-reports the semantics of the scroll pane when the viewport's dimensions change without
        /// the offset moving.
        /// </summary>
        /// <remarks>Flutter's <c>ScrollableState._handleScrollMetricsNotification</c>.</remarks>
        private bool HandleScrollMetricsNotification(ScrollMetricsNotification notification)
        {
            if (notification.Depth == 0)
            {
                RenderObject? scrollSemanticsRenderObject =
                    _scrollSemanticsKey.CurrentContext?.FindRenderObject();
                scrollSemanticsRenderObject?.MarkNeedsSemanticsUpdate();
            }

            return false;
        }

        private static bool IsLogicalKeyPressed(LogicalKeyboardKey key)
        {
            return HardwareKeyboard.Instance.IsLogicalKeyPressed(key);
        }

        /// <summary>
        /// Wraps the scrollable in the decorations the ambient <see cref="ScrollBehavior"/> supplies:
        /// the scrollbar and the overscroll indicator.
        /// </summary>
        /// <remarks>
        /// Flutter's <c>ScrollableState._buildChrome</c>; the two-dimensional dimensions override it
        /// to drop the one-axis scrollbar.
        /// </remarks>
        private protected virtual Widget BuildChrome(BuildContext context, Widget child)
        {
            var details = new ScrollableDetails(
                Direction: CurrentWidget.AxisDirection,
                Controller: EffectiveScrollController,
                DecorationClipBehavior: CurrentWidget.ClipBehavior);
            return _configuration.BuildScrollbar(
                context,
                _configuration.BuildOverscrollIndicator(context, child, details),
                details);
        }

        public override Widget Build(BuildContext context)
        {
            Debug.Assert(_position is not null);
            Scrollable widget = CurrentWidget;
            // The ScrollableScope must be placed above the BuildContext returned by
            // NotificationContext so that a notification's context can find this state through
            // Scrollable.Of.
            Widget result = new ScrollableScope(
                scrollable: this,
                position: Position,
                child: new Listener(
                    onPointerSignal: ReceivedPointerSignal,
                    child: new RawGestureDetector(
                        key: _gestureDetectorKey,
                        gestures: _gestureRecognizers,
                        behavior: widget.HitTestBehavior,
                        excludeFromSemantics: widget.ExcludeFromSemantics,
                        child: new Semantics(
                            explicitChildNodes: !widget.ExcludeFromSemantics,
                            child: new IgnorePointer(
                                key: _ignorePointerKey,
                                ignoring: _shouldIgnorePointer,
                                child: widget.ViewportBuilder(context, Position))))));

            if (!widget.ExcludeFromSemantics)
            {
                result = new NotificationListener<ScrollMetricsNotification>(
                    onNotification: HandleScrollMetricsNotification,
                    child: new ScrollSemantics(
                        key: _scrollSemanticsKey,
                        position: Position,
                        allowImplicitScrolling: _physics!.AllowImplicitScrolling,
                        axisDirection: widget.AxisDirection,
                        semanticChildCount: widget.SemanticChildCount,
                        child: result));
            }

            result = BuildChrome(context, result);

            // Selection is only enabled when there is a parent registrar.
            ISelectionRegistrar? registrar = SelectionContainer.MaybeOf(context);
            if (registrar is not null)
            {
                result = new ScrollableSelectionHandler(
                    state: this,
                    position: Position,
                    registrar: registrar,
                    child: result);
            }

            return result;
        }

        /// <summary>
        /// Reveals <paramref name="renderObject"/> in this scrollable, and reports the scrollable the
        /// enclosing walk should continue from.
        /// </summary>
        /// <remarks>
        /// Flutter's <c>ScrollableState._performEnsureVisible</c>, whose record return lets a
        /// two-dimensional scrollable reveal both of its axes at once and then hand the walk back to
        /// its outer dimension.
        /// </remarks>
        private protected virtual (IReadOnlyList<Task> Futures, ScrollableState Next) PerformEnsureVisible(
            RenderObject renderObject,
            double alignment,
            TimeSpan duration,
            Curve? curve,
            ScrollPositionAlignmentPolicy alignmentPolicy,
            RenderObject? targetRenderObject)
        {
            return (
                [
                    Position.EnsureVisible(
                        renderObject,
                        alignment,
                        duration,
                        curve,
                        alignmentPolicy,
                        targetRenderObject),
                ],
                this);
        }

        internal (IReadOnlyList<Task> Futures, ScrollableState Next) PerformEnsureVisibleInternal(
            RenderObject renderObject,
            double alignment,
            TimeSpan duration,
            Curve? curve,
            ScrollPositionAlignmentPolicy alignmentPolicy,
            RenderObject? targetRenderObject)
        {
            return PerformEnsureVisible(
                renderObject,
                alignment,
                duration,
                curve,
                alignmentPolicy,
                targetRenderObject);
        }

        /// <inheritdoc />
        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<ScrollPosition>("position", _position));
            properties.Add(new DiagnosticsProperty<ScrollPhysics>("effective physics", _physics));
        }
    }
}

/// <summary>
/// The <see cref="InheritedWidget"/> a <see cref="Scrollable"/> publishes so that descendants can
/// find its state and depend on its position.
/// </summary>
/// <remarks>Flutter's private <c>_ScrollableScope</c>.</remarks>
internal sealed class ScrollableScope : InheritedWidget
{
    public ScrollableScope(
        Scrollable.ScrollableState scrollable,
        ScrollPosition position,
        Widget child,
        Key? key = null) : base(key)
    {
        Scrollable = scrollable;
        Position = position;
        Child = child;
    }

    public Scrollable.ScrollableState Scrollable { get; }

    public ScrollPosition Position { get; }

    /// <summary>The subtree this scope wraps.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(Position, ((ScrollableScope)oldWidget).Position);
    }
}

/// <summary>
/// The restorable scroll offset a <see cref="Scrollable"/> persists.
/// </summary>
/// <remarks>
/// Flutter's private <c>_RestorableScrollOffset</c>: not a <see cref="RestorableDoubleN"/>, because
/// the property must also override <see cref="RestorableProperty.Enabled"/> so a scrollable that
/// never scrolled writes nothing.
/// </remarks>
internal sealed class RestorableScrollOffset : RestorableValue<double?>
{
    public override double? CreateDefaultValue() => null;

    protected override void DidUpdateValue(double? oldValue) => NotifyListeners();

    public override double? FromPrimitives(object? data) => (double?)data;

    public override object? ToPrimitives() => Value;

    public override bool Enabled => Value is not null;
}
