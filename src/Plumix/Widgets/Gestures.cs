using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/gesture_detector.dart (approximate)

namespace Plumix.Widgets;

public sealed class Listener : SingleChildRenderObjectWidget
{
    public Listener(
        Widget? child = null,
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerEnterEvent>? onPointerEnter = null,
        Action<PointerExitEvent>? onPointerExit = null,
        Action<PointerHoverEvent>? onPointerHover = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action<PointerSignalEvent>? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Key? key = null) : base(child, key)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerEnter = onPointerEnter;
        OnPointerExit = onPointerExit;
        OnPointerHover = onPointerHover;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerSignal = onPointerSignal;
        Behavior = behavior;
    }

    public Action<PointerDownEvent>? OnPointerDown { get; }

    public Action<PointerMoveEvent>? OnPointerMove { get; }

    public Action<PointerEnterEvent>? OnPointerEnter { get; }

    public Action<PointerExitEvent>? OnPointerExit { get; }

    public Action<PointerHoverEvent>? OnPointerHover { get; }

    public Action<PointerUpEvent>? OnPointerUp { get; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; }

    public Action<PointerSignalEvent>? OnPointerSignal { get; }

    public HitTestBehavior Behavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPointerListener(
            onPointerDown: OnPointerDown,
            onPointerMove: OnPointerMove,
            onPointerEnter: OnPointerEnter,
            onPointerExit: OnPointerExit,
            onPointerHover: OnPointerHover,
            onPointerUp: OnPointerUp,
            onPointerCancel: OnPointerCancel,
            onPointerSignal: OnPointerSignal,
            behavior: Behavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var listener = (RenderPointerListener)renderObject;
        listener.OnPointerDown = OnPointerDown;
        listener.OnPointerMove = OnPointerMove;
        listener.OnPointerEnter = OnPointerEnter;
        listener.OnPointerExit = OnPointerExit;
        listener.OnPointerHover = OnPointerHover;
        listener.OnPointerUp = OnPointerUp;
        listener.OnPointerCancel = OnPointerCancel;
        listener.OnPointerSignal = OnPointerSignal;
        listener.Behavior = Behavior;
    }
}

public sealed class RawGestureDetector : StatefulWidget
{
    public RawGestureDetector(
        Widget? child = null,
        IReadOnlyDictionary<Type, IGestureRecognizerFactory>? gestures = null,
        HitTestBehavior? behavior = null,
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action<PointerDownEvent>? onTapDown = null,
        Action<PointerUpEvent>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<PointerDownEvent>? onSecondaryTapDown = null,
        Action<PointerUpEvent>? onSecondaryTapUp = null,
        Action? onSecondaryTapCancel = null,
        Action<DragDownDetails>? onHorizontalDragDown = null,
        Action<DragStartDetails>? onHorizontalDragStart = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragEndDetails>? onHorizontalDragEnd = null,
        Action? onHorizontalDragCancel = null,
        Action<DragDownDetails>? onVerticalDragDown = null,
        Action<DragStartDetails>? onVerticalDragStart = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        Action<DragEndDetails>? onVerticalDragEnd = null,
        Action? onVerticalDragCancel = null,
        Action<DragDownDetails>? onPanDown = null,
        Action<DragStartDetails>? onPanStart = null,
        Action<DragUpdateDetails>? onPanUpdate = null,
        Action<DragEndDetails>? onPanEnd = null,
        Action? onPanCancel = null,
        GestureVelocityTrackerBuilder? velocityTrackerBuilder = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        DeviceGestureSettings? gestureSettings = null,
        double? minFlingDistance = null,
        double? minFlingVelocity = null,
        double? maxFlingVelocity = null,
        bool dragEnabled = true,
        bool excludeFromSemantics = false,
        Key? key = null) : base(key)
    {
        Child = child;
        ExcludeFromSemantics = excludeFromSemantics;
        Gestures = gestures;
        Behavior = behavior;
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnTap = onTap;
        OnDoubleTap = onDoubleTap;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTapCancel = onTapCancel;
        OnLongPress = onLongPress;
        OnLongPressUp = onLongPressUp;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnHorizontalDragDown = onHorizontalDragDown;
        OnHorizontalDragStart = onHorizontalDragStart;
        OnHorizontalDragUpdate = onHorizontalDragUpdate;
        OnHorizontalDragEnd = onHorizontalDragEnd;
        OnHorizontalDragCancel = onHorizontalDragCancel;
        OnVerticalDragDown = onVerticalDragDown;
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
        OnVerticalDragCancel = onVerticalDragCancel;
        OnPanDown = onPanDown;
        OnPanStart = onPanStart;
        OnPanUpdate = onPanUpdate;
        OnPanEnd = onPanEnd;
        OnPanCancel = onPanCancel;
        VelocityTrackerBuilder = velocityTrackerBuilder;
        SupportedDevices = supportedDevices;
        DragStartBehavior = dragStartBehavior;
        GestureSettings = gestureSettings;
        MinFlingDistance = minFlingDistance;
        MinFlingVelocity = minFlingVelocity;
        MaxFlingVelocity = maxFlingVelocity;
        DragEnabled = dragEnabled;
    }

    public Widget? Child { get; }

    /// <summary>
    /// The recognizers this detector owns, keyed by recognizer type. When supplied it replaces the
    /// fixed callback set entirely, matching Flutter's `RawGestureDetector.gestures`.
    /// </summary>
    public IReadOnlyDictionary<Type, IGestureRecognizerFactory>? Gestures { get; }

    /// <summary>
    /// How this detector participates in hit testing. Null falls back to
    /// <see cref="DefaultBehavior"/>, exactly as Dart's `behavior ?? _defaultBehavior`.
    /// </summary>
    public HitTestBehavior? Behavior { get; }

    /// <summary>Dart's `_RawGestureDetectorState._defaultBehavior`.</summary>
    public HitTestBehavior DefaultBehavior =>
        Child == null ? HitTestBehavior.Translucent : HitTestBehavior.DeferToChild;

    public Action<PointerDownEvent>? OnPointerDown { get; }

    public Action<PointerMoveEvent>? OnPointerMove { get; }

    public Action<PointerUpEvent>? OnPointerUp { get; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; }

    public Action? OnTap { get; }
    public Action? OnDoubleTap { get; }
    public Action<PointerDownEvent>? OnTapDown { get; }
    public Action<PointerUpEvent>? OnTapUp { get; }
    public Action? OnTapCancel { get; }

    public Action? OnLongPress { get; }
    public Action? OnLongPressUp { get; }
    public Action? OnSecondaryTap { get; }
    public Action<PointerDownEvent>? OnSecondaryTapDown { get; }
    public Action<PointerUpEvent>? OnSecondaryTapUp { get; }
    public Action? OnSecondaryTapCancel { get; }

    public Action<DragDownDetails>? OnHorizontalDragDown { get; }

    public Action<DragStartDetails>? OnHorizontalDragStart { get; }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate { get; }

    public Action<DragEndDetails>? OnHorizontalDragEnd { get; }

    public Action? OnHorizontalDragCancel { get; }

    public Action<DragDownDetails>? OnVerticalDragDown { get; }

    public Action<DragStartDetails>? OnVerticalDragStart { get; }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate { get; }

    public Action<DragEndDetails>? OnVerticalDragEnd { get; }

    public Action? OnVerticalDragCancel { get; }

    public Action<DragDownDetails>? OnPanDown { get; }

    public Action<DragStartDetails>? OnPanStart { get; }

    public Action<DragUpdateDetails>? OnPanUpdate { get; }

    public Action<DragEndDetails>? OnPanEnd { get; }

    public Action? OnPanCancel { get; }

    public GestureVelocityTrackerBuilder? VelocityTrackerBuilder { get; }

    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public DeviceGestureSettings? GestureSettings { get; }

    /// <summary>Drag distance floor below which a release is not treated as a fling.</summary>
    public double? MinFlingDistance { get; }

    /// <summary>Drag velocity floor below which a release is not treated as a fling.</summary>
    public double? MinFlingVelocity { get; }

    /// <summary>Ceiling applied to reported fling velocities.</summary>
    public double? MaxFlingVelocity { get; }

    /// <summary>
    /// Whether the drag recognizers are registered at all. Setting this to false removes them from
    /// the gesture arena instead of merely ignoring their callbacks.
    /// </summary>
    public bool DragEnabled { get; }

    public override State CreateState()
    {
        return new RawGestureDetectorState();
    }

    /// <summary>Whether the detector's gestures are hidden from the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    public sealed class RawGestureDetectorState : State
    {
        private RenderSemanticsGestureHandler? _semanticsGestureHandler;

        private TapGestureRecognizer? _tap;
        private LongPressGestureRecognizer? _longPress;
        private HorizontalDragGestureRecognizer? _horizontalDrag;
        private VerticalDragGestureRecognizer? _verticalDrag;
        private PanGestureRecognizer? _pan;
        private readonly Dictionary<Type, GestureRecognizer> _customRecognizers = [];
        private bool _dragEnabled = true;

        private RawGestureDetector CurrentWidget => (RawGestureDetector)Element.Widget;

        public override void InitState()
        {
            _dragEnabled = CurrentWidget.DragEnabled;
            SyncRecognizers();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _dragEnabled = CurrentWidget.DragEnabled;
            SyncRecognizers();
        }

        /// <summary>
        /// Registers or unregisters the drag recognizers immediately, without waiting for a rebuild.
        /// This is the counterpart of Flutter's <c>replaceGestureRecognizers</c>, which a scrollable
        /// calls from layout when the physics change their mind about accepting user offsets.
        /// </summary>
        public void SetDragEnabled(bool value)
        {
            if (_dragEnabled == value)
            {
                return;
            }

            _dragEnabled = value;
            SyncRecognizers();
            // The recognizer set changed outside a rebuild, so the semantics handler has to be
            // re-assigned (Flutter's `replaceGestureRecognizers` does the same).
            if (_semanticsGestureHandler is { } handler)
            {
                AssignSemantics(handler);
            }
        }

        public override void Dispose()
        {
            DisposeRecognizer(ref _tap);
            DisposeRecognizer(ref _longPress);
            DisposeRecognizer(ref _horizontalDrag);
            DisposeRecognizer(ref _verticalDrag);
            DisposeRecognizer(ref _pan);
            foreach (GestureRecognizer recognizer in _customRecognizers.Values)
            {
                recognizer.Dispose();
            }

            _customRecognizers.Clear();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            Widget result = new Listener(
                child: widget.Child,
                behavior: widget.Behavior ?? widget.DefaultBehavior,
                onPointerDown: HandlePointerDown,
                onPointerMove: widget.OnPointerMove,
                onPointerUp: widget.OnPointerUp,
                onPointerCancel: widget.OnPointerCancel);

            if (!widget.ExcludeFromSemantics)
            {
                result = new GestureSemantics(child: result, assignSemantics: AssignSemantics);
            }

            return result;
        }

        /// <summary>
        /// Mirrors the recognizers this detector currently holds onto the semantics handler, so the
        /// gestures show up as semantics actions on the nearest semantics node.
        /// </summary>
        /// <remarks>Flutter's <c>_DefaultSemanticsGestureDelegate.assignSemantics</c>.</remarks>
        private void AssignSemantics(RenderSemanticsGestureHandler renderObject)
        {
            _semanticsGestureHandler = renderObject;
            renderObject.OnTap = GetTapHandler(renderObject);
            renderObject.OnLongPress = GetLongPressHandler();
            renderObject.OnHorizontalDragUpdate = GetDragUpdateHandler(renderObject, horizontal: true);
            renderObject.OnVerticalDragUpdate = GetDragUpdateHandler(renderObject, horizontal: false);
            renderObject.ValidActions = _validActions;
        }

        private static Point LocalCenterOf(RenderObject renderObject)
        {
            return renderObject is RenderBox box
                ? new Point(box.Size.Width / 2.0, box.Size.Height / 2.0)
                : default;
        }

        /// <summary>
        /// Replays the whole down/up/tap sequence, exactly as Flutter's
        /// <c>_DefaultSemanticsGestureDelegate._getTapHandler</c> does. Plumix's
        /// <c>TapGestureRecognizer.OnTapDown</c>/<c>OnTapUp</c> take real pointer events, which a
        /// semantic tap has none of, so they are synthesized at the render object's center rather
        /// than at Dart's origin — a button whose bounds exclude the origin still activates.
        /// </summary>
        private Action? GetTapHandler(RenderObject renderObject)
        {
            if (ResolveRecognizer(_tap) is not { } tap)
            {
                return null;
            }

            return () =>
            {
                Point globalCenter = renderObject.LocalToGlobal(LocalCenterOf(renderObject));
                DateTime timestamp = DateTime.UtcNow;
                tap.OnTapDown?.Invoke(new PointerDownEvent(
                    0,
                    PointerDeviceKind.Unknown,
                    globalCenter,
                    PointerButtons.Primary,
                    timestamp));
                tap.OnTapUp?.Invoke(new PointerUpEvent(
                    0,
                    PointerDeviceKind.Unknown,
                    globalCenter,
                    PointerButtons.Primary,
                    timestamp));
                tap.OnTap?.Invoke();
            };
        }

        private Action? GetLongPressHandler()
        {
            if (ResolveRecognizer(_longPress) is not { } longPress)
            {
                return null;
            }

            return () =>
            {
                longPress.OnLongPress?.Invoke();
                longPress.OnLongPressUp?.Invoke();
            };
        }

        /// <summary>
        /// The recognizer of the given type this detector currently owns: the one built from the
        /// fixed callback set, or — when the widget supplied a <c>gestures</c> map — the one that
        /// map registered. Flutter keeps a single map and looks the type up in it.
        /// </summary>
        private T? ResolveRecognizer<T>(T? builtIn) where T : GestureRecognizer
        {
            return builtIn
                   ?? (_customRecognizers.TryGetValue(typeof(T), out GestureRecognizer? custom)
                       ? custom as T
                       : null);
        }

        /// <summary>
        /// Replays the whole down/start/update/end sequence, so a semantic scroll runs through the
        /// same drag pipeline a real drag would.
        /// </summary>
        /// <remarks>Flutter's <c>_DefaultSemanticsGestureDelegate._get*DragUpdateHandler</c>.</remarks>
        private Action<DragUpdateDetails>? GetDragUpdateHandler(RenderObject renderObject, bool horizontal)
        {
            var widget = CurrentWidget;
            bool hasDirectionalDrag = horizontal ? _horizontalDrag is not null : _verticalDrag is not null;
            Action<DragDownDetails>? onDown;
            Action<DragStartDetails>? onStart;
            Action<DragUpdateDetails>? onUpdate;
            Action<DragEndDetails>? onEnd;
            if (hasDirectionalDrag)
            {
                onDown = horizontal ? widget.OnHorizontalDragDown : widget.OnVerticalDragDown;
                onStart = horizontal ? widget.OnHorizontalDragStart : widget.OnVerticalDragStart;
                onUpdate = horizontal ? widget.OnHorizontalDragUpdate : widget.OnVerticalDragUpdate;
                onEnd = horizontal ? widget.OnHorizontalDragEnd : widget.OnVerticalDragEnd;
            }
            else if (_pan is not null)
            {
                onDown = widget.OnPanDown;
                onStart = widget.OnPanStart;
                onUpdate = widget.OnPanUpdate;
                onEnd = widget.OnPanEnd;
            }
            else
            {
                return null;
            }

            if (onUpdate is null)
            {
                return null;
            }

            return details =>
            {
                Point localCenter = LocalCenterOf(renderObject);
                Point globalCenter = renderObject.LocalToGlobal(localCenter);
                onDown?.Invoke(new DragDownDetails(
                    GlobalPosition: globalCenter,
                    LocalPosition: localCenter));
                onStart?.Invoke(new DragStartDetails(
                    GlobalPosition: globalCenter,
                    LocalPosition: localCenter));
                onUpdate(details);
                onEnd?.Invoke(new DragEndDetails(primaryVelocity: 0.0));
            };
        }

        private SemanticsActions? _validActions;

        /// <summary>
        /// Filters the semantics actions this detector exposes. <c>Scrollable</c> calls it so that
        /// assistive tools only see the directions the list can still be scrolled in.
        /// </summary>
        /// <remarks>Flutter's <c>RawGestureDetectorState.replaceSemanticsActions</c>.</remarks>
        public void ReplaceSemanticsActions(SemanticsActions actions)
        {
            if (CurrentWidget.ExcludeFromSemantics)
            {
                return;
            }

            _validActions = actions;
            if (_semanticsGestureHandler is { } handler)
            {
                handler.ValidActions = actions;
            }
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            var widget = CurrentWidget;
            widget.OnPointerDown?.Invoke(@event);
            if (widget.SupportedDevices != null && !widget.SupportedDevices.Contains(@event.Kind))
            {
                return;
            }

            if (widget.Gestures is not null)
            {
                foreach (GestureRecognizer recognizer in _customRecognizers.Values.ToArray())
                {
                    recognizer.AddPointer(@event);
                }

                return;
            }

            _tap?.AddPointer(@event);
            _longPress?.AddPointer(@event);
            _horizontalDrag?.AddPointer(@event);
            _verticalDrag?.AddPointer(@event);
            _pan?.AddPointer(@event);
        }

        private void SyncRecognizers()
        {
            var widget = CurrentWidget;

            if (widget.Gestures is { } gestures)
            {
                // The explicit recognizer map replaces the fixed callback set entirely.
                DisposeRecognizer(ref _tap);
                DisposeRecognizer(ref _longPress);
                DisposeRecognizer(ref _horizontalDrag);
                DisposeRecognizer(ref _verticalDrag);
                DisposeRecognizer(ref _pan);
                SyncCustomRecognizers(gestures);
                return;
            }

            if (_customRecognizers.Count > 0)
            {
                foreach (GestureRecognizer recognizer in _customRecognizers.Values)
                {
                    recognizer.Dispose();
                }

                _customRecognizers.Clear();
            }

            if (widget.OnTap != null || widget.OnDoubleTap != null || widget.OnTapDown != null
                || widget.OnTapUp != null || widget.OnSecondaryTap != null
                || widget.OnSecondaryTapDown != null || widget.OnSecondaryTapUp != null)
            {
                _tap ??= new TapGestureRecognizer();
                _tap.OnTap = widget.OnTap;
                _tap.OnDoubleTap = widget.OnDoubleTap;
                _tap.OnTapDown = widget.OnTapDown;
                _tap.OnTapUp = widget.OnTapUp;
                _tap.OnTapCancel = widget.OnTapCancel;
                _tap.OnSecondaryTap = widget.OnSecondaryTap;
                _tap.OnSecondaryTapDown = widget.OnSecondaryTapDown;
                _tap.OnSecondaryTapUp = widget.OnSecondaryTapUp;
                _tap.OnSecondaryTapCancel = widget.OnSecondaryTapCancel;
            }
            else
            {
                DisposeRecognizer(ref _tap);
            }

            if (widget.OnLongPress != null || widget.OnLongPressUp != null)
            {
                _longPress ??= new LongPressGestureRecognizer();
                _longPress.OnLongPress = widget.OnLongPress;
                _longPress.OnLongPressUp = widget.OnLongPressUp;
            }
            else
            {
                DisposeRecognizer(ref _longPress);
            }

            if (_dragEnabled
                && (widget.OnHorizontalDragDown != null
                    || widget.OnHorizontalDragStart != null
                    || widget.OnHorizontalDragUpdate != null
                    || widget.OnHorizontalDragEnd != null
                    || widget.OnHorizontalDragCancel != null))
            {
                _horizontalDrag ??= new HorizontalDragGestureRecognizer();
                _horizontalDrag.OnDown = widget.OnHorizontalDragDown;
                _horizontalDrag.OnStart = widget.OnHorizontalDragStart;
                _horizontalDrag.OnUpdate = widget.OnHorizontalDragUpdate;
                _horizontalDrag.OnEnd = widget.OnHorizontalDragEnd;
                _horizontalDrag.OnCancel = widget.OnHorizontalDragCancel;
                _horizontalDrag.DragStartBehavior = widget.DragStartBehavior;
                _horizontalDrag.SupportedDevices = widget.SupportedDevices;
                _horizontalDrag.GestureSettings = widget.GestureSettings;
                _horizontalDrag.MinFlingDistance = widget.MinFlingDistance;
                _horizontalDrag.MinFlingVelocity = widget.MinFlingVelocity;
                _horizontalDrag.MaxFlingVelocity = widget.MaxFlingVelocity;
                _horizontalDrag.VelocityTrackerBuilder = widget.VelocityTrackerBuilder
                    ?? DragGestureRecognizer.DefaultVelocityTrackerBuilder;
            }
            else
            {
                DisposeRecognizer(ref _horizontalDrag);
            }

            if (_dragEnabled
                && (widget.OnVerticalDragDown != null
                    || widget.OnVerticalDragStart != null
                    || widget.OnVerticalDragUpdate != null
                    || widget.OnVerticalDragEnd != null
                    || widget.OnVerticalDragCancel != null))
            {
                _verticalDrag ??= new VerticalDragGestureRecognizer();
                _verticalDrag.OnDown = widget.OnVerticalDragDown;
                _verticalDrag.OnStart = widget.OnVerticalDragStart;
                _verticalDrag.OnUpdate = widget.OnVerticalDragUpdate;
                _verticalDrag.OnEnd = widget.OnVerticalDragEnd;
                _verticalDrag.OnCancel = widget.OnVerticalDragCancel;
                _verticalDrag.DragStartBehavior = widget.DragStartBehavior;
                _verticalDrag.SupportedDevices = widget.SupportedDevices;
                _verticalDrag.GestureSettings = widget.GestureSettings;
                _verticalDrag.MinFlingDistance = widget.MinFlingDistance;
                _verticalDrag.MinFlingVelocity = widget.MinFlingVelocity;
                _verticalDrag.MaxFlingVelocity = widget.MaxFlingVelocity;
                _verticalDrag.VelocityTrackerBuilder = widget.VelocityTrackerBuilder
                    ?? DragGestureRecognizer.DefaultVelocityTrackerBuilder;
            }
            else
            {
                DisposeRecognizer(ref _verticalDrag);
            }

            if (_dragEnabled
                && (widget.OnPanDown != null
                    || widget.OnPanStart != null
                    || widget.OnPanUpdate != null
                    || widget.OnPanEnd != null
                    || widget.OnPanCancel != null))
            {
                _pan ??= new PanGestureRecognizer();
                _pan.OnDown = widget.OnPanDown;
                _pan.OnStart = widget.OnPanStart;
                _pan.OnUpdate = widget.OnPanUpdate;
                _pan.OnEnd = widget.OnPanEnd;
                _pan.OnCancel = widget.OnPanCancel;
                _pan.DragStartBehavior = widget.DragStartBehavior;
                _pan.SupportedDevices = widget.SupportedDevices;
                _pan.GestureSettings = widget.GestureSettings;
                _pan.MinFlingDistance = widget.MinFlingDistance;
                _pan.MinFlingVelocity = widget.MinFlingVelocity;
                _pan.MaxFlingVelocity = widget.MaxFlingVelocity;
                _pan.VelocityTrackerBuilder = widget.VelocityTrackerBuilder
                    ?? DragGestureRecognizer.DefaultVelocityTrackerBuilder;
            }
            else
            {
                DisposeRecognizer(ref _pan);
            }
        }

        private void SyncCustomRecognizers(IReadOnlyDictionary<Type, IGestureRecognizerFactory> gestures)
        {
            foreach (Type type in _customRecognizers.Keys.ToArray())
            {
                if (gestures.ContainsKey(type))
                {
                    continue;
                }

                _customRecognizers[type].Dispose();
                _customRecognizers.Remove(type);
            }

            foreach ((Type type, IGestureRecognizerFactory factory) in gestures)
            {
                if (!factory.HandlesType(type))
                {
                    throw new InvalidOperationException(
                        $"The gesture recognizer registered for {type.Name} does not build that type.");
                }

                if (!_customRecognizers.TryGetValue(type, out GestureRecognizer? recognizer))
                {
                    recognizer = factory.ConstructorRaw();
                    _customRecognizers[type] = recognizer;
                }

                factory.InitializerRaw(recognizer);
            }
        }

        private static void DisposeRecognizer<T>(ref T? recognizer) where T : GestureRecognizer
        {
            recognizer?.Dispose();
            recognizer = null;
        }
    }
}

public sealed class GestureDetector : StatelessWidget
{
    public GestureDetector(
        Widget? child = null,
        HitTestBehavior? behavior = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action<PointerDownEvent>? onTapDown = null,
        Action<PointerUpEvent>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<PointerDownEvent>? onSecondaryTapDown = null,
        Action<PointerUpEvent>? onSecondaryTapUp = null,
        Action? onSecondaryTapCancel = null,
        Action<DragStartDetails>? onHorizontalDragStart = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragEndDetails>? onHorizontalDragEnd = null,
        Action? onHorizontalDragCancel = null,
        Action<DragStartDetails>? onVerticalDragStart = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        Action<DragEndDetails>? onVerticalDragEnd = null,
        Action? onVerticalDragCancel = null,
        Action<DragStartDetails>? onPanStart = null,
        Action<DragUpdateDetails>? onPanUpdate = null,
        Action<DragEndDetails>? onPanEnd = null,
        Action? onPanCancel = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool excludeFromSemantics = false,
        Key? key = null) : base(key)
    {
        Child = child;
        ExcludeFromSemantics = excludeFromSemantics;
        Behavior = behavior;
        OnTap = onTap;
        OnDoubleTap = onDoubleTap;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTapCancel = onTapCancel;
        OnLongPress = onLongPress;
        OnLongPressUp = onLongPressUp;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnHorizontalDragStart = onHorizontalDragStart;
        OnHorizontalDragUpdate = onHorizontalDragUpdate;
        OnHorizontalDragEnd = onHorizontalDragEnd;
        OnHorizontalDragCancel = onHorizontalDragCancel;
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
        OnVerticalDragCancel = onVerticalDragCancel;
        OnPanStart = onPanStart;
        OnPanUpdate = onPanUpdate;
        OnPanEnd = onPanEnd;
        OnPanCancel = onPanCancel;
        DragStartBehavior = dragStartBehavior;
    }

    public Widget? Child { get; }

    /// <summary>Whether the detector's gestures are hidden from the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>
    /// How this detector participates in hit testing. Null defers to
    /// <see cref="RawGestureDetector.DefaultBehavior"/>.
    /// </summary>
    public HitTestBehavior? Behavior { get; }

    public Action? OnTap { get; }
    public Action? OnDoubleTap { get; }
    public Action<PointerDownEvent>? OnTapDown { get; }
    public Action<PointerUpEvent>? OnTapUp { get; }
    public Action? OnTapCancel { get; }

    public Action? OnLongPress { get; }
    public Action? OnLongPressUp { get; }
    public Action? OnSecondaryTap { get; }
    public Action<PointerDownEvent>? OnSecondaryTapDown { get; }
    public Action<PointerUpEvent>? OnSecondaryTapUp { get; }
    public Action? OnSecondaryTapCancel { get; }

    public Action<DragStartDetails>? OnHorizontalDragStart { get; }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate { get; }

    public Action<DragEndDetails>? OnHorizontalDragEnd { get; }

    public Action? OnHorizontalDragCancel { get; }

    public Action<DragStartDetails>? OnVerticalDragStart { get; }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate { get; }

    public Action<DragEndDetails>? OnVerticalDragEnd { get; }

    public Action? OnVerticalDragCancel { get; }

    public Action<DragStartDetails>? OnPanStart { get; }

    public Action<DragUpdateDetails>? OnPanUpdate { get; }

    public Action<DragEndDetails>? OnPanEnd { get; }

    public Action? OnPanCancel { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        return new RawGestureDetector(
            child: Child,
            excludeFromSemantics: ExcludeFromSemantics,
            behavior: Behavior,
            onTap: OnTap,
            onDoubleTap: OnDoubleTap,
            onTapDown: OnTapDown,
            onTapUp: OnTapUp,
            onTapCancel: OnTapCancel,
            onLongPress: OnLongPress,
            onLongPressUp: OnLongPressUp,
            onSecondaryTap: OnSecondaryTap,
            onSecondaryTapDown: OnSecondaryTapDown,
            onSecondaryTapUp: OnSecondaryTapUp,
            onSecondaryTapCancel: OnSecondaryTapCancel,
            onHorizontalDragStart: OnHorizontalDragStart,
            onHorizontalDragUpdate: OnHorizontalDragUpdate,
            onHorizontalDragEnd: OnHorizontalDragEnd,
            onHorizontalDragCancel: OnHorizontalDragCancel,
            onVerticalDragStart: OnVerticalDragStart,
            onVerticalDragUpdate: OnVerticalDragUpdate,
            onVerticalDragEnd: OnVerticalDragEnd,
            onVerticalDragCancel: OnVerticalDragCancel,
            onPanStart: OnPanStart,
            onPanUpdate: OnPanUpdate,
            onPanEnd: OnPanEnd,
            onPanCancel: OnPanCancel,
            dragStartBehavior: DragStartBehavior);
    }
}

/// <summary>
/// Hosts the <see cref="RenderSemanticsGestureHandler"/> a <see cref="RawGestureDetector"/> puts
/// between its listener and the semantics tree.
/// </summary>
/// <remarks>Flutter's private <c>_GestureSemantics</c>.</remarks>
internal sealed class GestureSemantics : SingleChildRenderObjectWidget
{
    public GestureSemantics(
        Action<RenderSemanticsGestureHandler> assignSemantics,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        AssignSemantics = assignSemantics;
    }

    public Action<RenderSemanticsGestureHandler> AssignSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var renderObject = new RenderSemanticsGestureHandler();
        AssignSemantics(renderObject);
        return renderObject;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        AssignSemantics((RenderSemanticsGestureHandler)renderObject);
    }
}
