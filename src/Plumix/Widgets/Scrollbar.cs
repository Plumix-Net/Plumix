using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollbar.dart

public delegate bool ScrollNotificationPredicate(ScrollNotification notification);

public enum ScrollbarOrientation
{
    Left,
    Right,
    Top,
    Bottom,
}

[Flags]
internal enum ScrollbarInteractionState
{
    None = 0,
    Hovered = 1 << 0,
    Dragged = 1 << 1,
}

public readonly record struct ScrollbarGeometry(
    Rect TrackRect,
    Rect ThumbRect,
    Axis Axis,
    bool IsReversed,
    double TrackMainAxisStart,
    double TrackMainAxisExtent,
    double ThumbMainAxisOffset,
    double ThumbMainAxisExtent)
{
    public double MaxThumbTravel => Math.Max(0, TrackMainAxisExtent - ThumbMainAxisExtent);
}

public sealed class RawScrollbar : StatefulWidget
{
    public RawScrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        ShapeBorder? shape = null,
        double? radius = null,
        double? thickness = null,
        Color? thumbColor = null,
        double minThumbLength = 18,
        double? minOverscrollLength = null,
        bool? trackVisibility = null,
        double? trackRadius = null,
        Color? trackColor = null,
        Color? trackBorderColor = null,
        TimeSpan? fadeDuration = null,
        TimeSpan? timeToFade = null,
        TimeSpan? pressDuration = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        bool? interactive = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        double mainAxisMargin = 0,
        double crossAxisMargin = 0,
        Thickness? padding = null,
        Key? key = null) : this(
        child,
        controller,
        thumbVisibility,
        shape,
        radius,
        thickness,
        thumbColor,
        minThumbLength,
        minOverscrollLength,
        trackVisibility,
        trackRadius,
        trackColor,
        trackBorderColor,
        fadeDuration,
        timeToFade,
        pressDuration,
        notificationPredicate,
        interactive,
        scrollbarOrientation,
        mainAxisMargin,
        crossAxisMargin,
        padding,
        thumbColorResolver: null,
        trackColorResolver: null,
        trackBorderColorResolver: null,
        thicknessResolver: null,
        radiusResolver: null,
        thumbVisibilityResolver: null,
        trackVisibilityResolver: null,
        trackTapEnabled: true,
        interactionChanged: null,
        key)
    {
    }

    internal RawScrollbar(
        Widget child,
        ScrollController? controller,
        bool? thumbVisibility,
        ShapeBorder? shape,
        double? radius,
        double? thickness,
        Color? thumbColor,
        double minThumbLength,
        double? minOverscrollLength,
        bool? trackVisibility,
        double? trackRadius,
        Color? trackColor,
        Color? trackBorderColor,
        TimeSpan? fadeDuration,
        TimeSpan? timeToFade,
        TimeSpan? pressDuration,
        ScrollNotificationPredicate? notificationPredicate,
        bool? interactive,
        ScrollbarOrientation? scrollbarOrientation,
        double mainAxisMargin,
        double crossAxisMargin,
        Thickness? padding,
        Func<ScrollbarInteractionState, Color?>? thumbColorResolver,
        Func<ScrollbarInteractionState, Color?>? trackColorResolver,
        Func<ScrollbarInteractionState, Color?>? trackBorderColorResolver,
        Func<ScrollbarInteractionState, double?>? thicknessResolver,
        Func<ScrollbarInteractionState, double?>? radiusResolver,
        Func<ScrollbarInteractionState, bool?>? thumbVisibilityResolver,
        Func<ScrollbarInteractionState, bool?>? trackVisibilityResolver,
        bool trackTapEnabled,
        Action<ScrollbarInteractionState>? interactionChanged,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (thumbVisibility == false && trackVisibility == true)
        {
            throw new ArgumentException("A scrollbar track cannot be visible without its thumb.");
        }

        ValidateNonNegative(nameof(minThumbLength), minThumbLength);
        ValidateNonNegative(nameof(minOverscrollLength), minOverscrollLength);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        ValidateNonNegative(nameof(crossAxisMargin), crossAxisMargin);
        ValidatePositive(nameof(thickness), thickness);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(trackRadius), trackRadius);
        ValidateDuration(nameof(fadeDuration), fadeDuration);
        ValidateDuration(nameof(timeToFade), timeToFade);
        ValidateDuration(nameof(pressDuration), pressDuration);
        if (minOverscrollLength > minThumbLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minOverscrollLength));
        }

        if (shape is not null && radius.HasValue)
        {
            throw new ArgumentException("Only one of shape and radius may be provided.");
        }

        Child = child;
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        Shape = shape;
        Radius = radius;
        Thickness = thickness;
        ThumbColor = thumbColor;
        MinThumbLength = minThumbLength;
        MinOverscrollLength = minOverscrollLength;
        TrackVisibility = trackVisibility;
        TrackRadius = trackRadius;
        TrackColor = trackColor;
        TrackBorderColor = trackBorderColor;
        FadeDuration = fadeDuration ?? TimeSpan.FromMilliseconds(300);
        TimeToFade = timeToFade ?? TimeSpan.FromMilliseconds(600);
        PressDuration = pressDuration ?? TimeSpan.Zero;
        NotificationPredicate = notificationPredicate ?? DefaultScrollNotificationPredicate;
        Interactive = interactive;
        ScrollbarOrientation = scrollbarOrientation;
        MainAxisMargin = mainAxisMargin;
        CrossAxisMargin = crossAxisMargin;
        Padding = padding;
        ThumbColorResolver = thumbColorResolver;
        TrackColorResolver = trackColorResolver;
        TrackBorderColorResolver = trackBorderColorResolver;
        ThicknessResolver = thicknessResolver;
        RadiusResolver = radiusResolver;
        ThumbVisibilityResolver = thumbVisibilityResolver;
        TrackVisibilityResolver = trackVisibilityResolver;
        TrackTapEnabled = trackTapEnabled;
        InteractionChanged = interactionChanged;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public ShapeBorder? Shape { get; }
    public double? Radius { get; }
    public double? Thickness { get; }
    public Color? ThumbColor { get; }
    public double MinThumbLength { get; }
    public double? MinOverscrollLength { get; }
    public bool? TrackVisibility { get; }
    public double? TrackRadius { get; }
    public Color? TrackColor { get; }
    public Color? TrackBorderColor { get; }
    public TimeSpan FadeDuration { get; }
    public TimeSpan TimeToFade { get; }
    public TimeSpan PressDuration { get; }
    public ScrollNotificationPredicate NotificationPredicate { get; }
    public bool? Interactive { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }
    public double MainAxisMargin { get; }
    public double CrossAxisMargin { get; }
    public Thickness? Padding { get; }

    internal Func<ScrollbarInteractionState, Color?>? ThumbColorResolver { get; }
    internal Func<ScrollbarInteractionState, Color?>? TrackColorResolver { get; }
    internal Func<ScrollbarInteractionState, Color?>? TrackBorderColorResolver { get; }
    internal Func<ScrollbarInteractionState, double?>? ThicknessResolver { get; }
    internal Func<ScrollbarInteractionState, double?>? RadiusResolver { get; }
    internal Func<ScrollbarInteractionState, bool?>? ThumbVisibilityResolver { get; }
    internal Func<ScrollbarInteractionState, bool?>? TrackVisibilityResolver { get; }
    internal bool TrackTapEnabled { get; }
    internal Action<ScrollbarInteractionState>? InteractionChanged { get; }

    public static bool DefaultScrollNotificationPredicate(ScrollNotification notification) =>
        notification.Depth == 0;

    public override State CreateState() => new RawScrollbarState();

    private static void ValidateNonNegative(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue) ValidateNonNegative(name, value.Value);
    }

    private static void ValidatePositive(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateDuration(string name, TimeSpan? value)
    {
        if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(name);
    }

    private sealed class RawScrollbarState : State
    {
        private ScrollController? _controller;
        private AnimationController? _fadeController;
        private AnimationController? _fadeDelayController;
        private AnimationController? _pressController;
        private AxisDirection _axisDirection = AxisDirection.Down;
        private ScrollbarInteractionState _interactionState;
        private int _paintRevision;
        private int? _activePointer;
        private bool _draggingThumb;
        private bool _pendingThumbPress;
        private double _dragOffsetWithinThumb;
        private double _lastPointerAxisOffset;
        private double _pendingThumbStart;
        private double _pendingThumbExtent;

        private RawScrollbar CurrentWidget => (RawScrollbar)StateWidget;

        public override void InitState()
        {
            CreateFadeControllers();
            CreatePressController();
            AttachController(CurrentWidget.Controller);
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            if (CurrentWidget.Controller is null)
            {
                AttachController(PrimaryScrollController.MaybeOf(Context));
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (RawScrollbar)oldWidget;
            if (!ReferenceEquals(old.Controller, CurrentWidget.Controller))
            {
                AttachController(CurrentWidget.Controller ?? PrimaryScrollController.MaybeOf(Context));
            }

            if (old.FadeDuration != CurrentWidget.FadeDuration || old.TimeToFade != CurrentWidget.TimeToFade)
            {
                DisposeFadeControllers();
                CreateFadeControllers();
            }

            if (old.PressDuration != CurrentWidget.PressDuration)
            {
                DisposePressController();
                CreatePressController();
            }

            if (CurrentWidget.ThumbVisibility == true)
            {
                CancelFade();
            }
        }

        public override void Dispose()
        {
            AttachController(null);
            DisposeFadeControllers();
            DisposePressController();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var states = _interactionState;
            bool forcedVisible = widget.ThumbVisibility
                                 ?? widget.ThumbVisibilityResolver?.Invoke(states)
                                 ?? false;
            bool trackVisible = widget.TrackVisibility
                                ?? widget.TrackVisibilityResolver?.Invoke(states)
                                ?? false;
            double opacity = forcedVisible ? 1 : 1 - (_fadeController?.Evaluate() ?? 1);
            double thickness = widget.Thickness
                               ?? widget.ThicknessResolver?.Invoke(states)
                               ?? 6;
            double radius = widget.Shape?.BorderRadius.Radius
                            ?? widget.Radius
                            ?? widget.RadiusResolver?.Invoke(states)
                            ?? 0;
            var padding = widget.Padding ?? MediaQuery.MaybePaddingOf(context) ?? default;
            bool interactive = widget.Interactive ?? true;
            var effectiveOrientation = widget.ScrollbarOrientation;
            if (!effectiveOrientation.HasValue)
            {
                effectiveOrientation = _axisDirection is AxisDirection.Left or AxisDirection.Right
                    ? global::Plumix.Widgets.ScrollbarOrientation.Bottom
                    : Directionality.Of(context) == TextDirection.Rtl
                        ? global::Plumix.Widgets.ScrollbarOrientation.Left
                        : global::Plumix.Widgets.ScrollbarOrientation.Right;
            }

            Widget result = new RawScrollbarOverlay(
                positionProvider: () => _controller?.PrimaryPosition,
                axisDirection: _axisDirection,
                orientation: effectiveOrientation,
                thickness: thickness,
                thumbColor: widget.ThumbColor
                            ?? widget.ThumbColorResolver?.Invoke(states)
                            ?? Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC),
                radius: radius,
                thumbBorder: widget.Shape?.Side,
                minThumbLength: widget.MinThumbLength,
                minOverscrollLength: widget.MinOverscrollLength ?? widget.MinThumbLength,
                trackVisible: trackVisible && (forcedVisible || opacity > 0.001),
                trackRadius: widget.TrackRadius ?? 0,
                trackColor: widget.TrackColor
                            ?? widget.TrackColorResolver?.Invoke(states)
                            ?? Color.FromArgb(0x08, 0, 0, 0),
                trackBorderColor: widget.TrackBorderColor
                                  ?? widget.TrackBorderColorResolver?.Invoke(states)
                                  ?? Color.FromArgb(0x1A, 0, 0, 0),
                mainAxisMargin: widget.MainAxisMargin,
                crossAxisMargin: widget.CrossAxisMargin,
                padding: padding,
                opacity: Math.Clamp(opacity, 0, 1),
                interactive: interactive,
                paintRevision: _paintRevision,
                onPointerDown: HandlePointerDown,
                onPointerMove: HandlePointerMove,
                onPointerUp: HandlePointerUp,
                onPointerCancel: HandlePointerCancel,
                onPointerHover: HandlePointerHover,
                onPointerExit: HandlePointerExit,
                child: new NotificationListener<ScrollNotification>(
                    onNotification: HandleScrollNotification,
                    child: widget.Child));

            return result;
        }

        private bool HandleScrollNotification(ScrollNotification notification)
        {
            if (!CurrentWidget.NotificationPredicate(notification)) return false;
            _axisDirection = notification.Metrics.AxisDirection;
            ShowTemporarily();
            return false;
        }

        private void AttachController(ScrollController? controller)
        {
            if (ReferenceEquals(_controller, controller)) return;
            _controller?.RemoveListener(HandleControllerChanged);
            _controller = controller;
            _controller?.AddListener(HandleControllerChanged);
            if (Mounted) SetState(() => _paintRevision++);
        }

        private void HandleControllerChanged()
        {
            ShowTemporarily();
        }

        private void ShowTemporarily()
        {
            if (!Mounted) return;
            SetState(() => _paintRevision++);
            if (CurrentWidget.ThumbVisibility == true) return;
            _fadeController?.Stop();
            SetFadeValue(0);
            _fadeDelayController?.Forward(0);
        }

        private void CancelFade()
        {
            _fadeDelayController?.Stop();
            _fadeController?.Stop();
            SetFadeValue(0);
        }

        private void CreateFadeControllers()
        {
            _fadeController = new AnimationController(CurrentWidget.FadeDuration, this)
            {
                Curve = Curves.FastOutSlowIn,
            };
            SetFadeValue(1);
            _fadeController.Changed += HandleFadeTick;
            _fadeDelayController = new AnimationController(CurrentWidget.TimeToFade, this);
            _fadeDelayController.Completed += HandleFadeDelayCompleted;
        }

        private void DisposeFadeControllers()
        {
            if (_fadeController is not null)
            {
                _fadeController.Changed -= HandleFadeTick;
                _fadeController.Dispose();
                _fadeController = null;
            }

            if (_fadeDelayController is not null)
            {
                _fadeDelayController.Completed -= HandleFadeDelayCompleted;
                _fadeDelayController.Dispose();
                _fadeDelayController = null;
            }
        }

        private void HandleFadeTick()
        {
            if (Mounted) SetState(() => _paintRevision++);
        }

        private void HandleFadeDelayCompleted()
        {
            if (_interactionState.HasFlag(ScrollbarInteractionState.Dragged)) return;
            _fadeController?.Forward(0);
        }

        private void CreatePressController()
        {
            _pressController = new AnimationController(CurrentWidget.PressDuration, this);
            _pressController.Completed += HandlePressDurationCompleted;
        }

        private void DisposePressController()
        {
            if (_pressController is null) return;
            _pressController.Completed -= HandlePressDurationCompleted;
            _pressController.Dispose();
            _pressController = null;
        }

        private void HandlePressDurationCompleted()
        {
            if (!_pendingThumbPress || !_activePointer.HasValue) return;
            BeginThumbDrag(_lastPointerAxisOffset, _pendingThumbStart, _pendingThumbExtent);
        }

        private void SetFadeValue(double value)
        {
            if (_fadeController is null) return;
            _fadeController.Forward(value);
            _fadeController.Stop();
        }

        private void HandlePointerDown(PointerDownEvent @event, ScrollbarGeometry geometry)
        {
            if (_activePointer.HasValue || _controller?.PrimaryPosition is not { } position) return;
            _activePointer = @event.Pointer;
            double axisOffset = AxisOffset(@event.LocalPosition, geometry.Axis);
            _lastPointerAxisOffset = axisOffset;
            double thumbStart = geometry.TrackMainAxisStart + geometry.ThumbMainAxisOffset;
            double thumbEnd = thumbStart + geometry.ThumbMainAxisExtent;
            if (axisOffset >= thumbStart && axisOffset <= thumbEnd)
            {
                CancelFade();
                if (CurrentWidget.PressDuration <= TimeSpan.Zero)
                {
                    BeginThumbDrag(axisOffset, thumbStart, geometry.ThumbMainAxisExtent);
                }
                else
                {
                    _pendingThumbPress = true;
                    _pendingThumbStart = thumbStart;
                    _pendingThumbExtent = geometry.ThumbMainAxisExtent;
                    _pressController?.Forward(0);
                }
                return;
            }

            if (!CurrentWidget.TrackTapEnabled) return;

            int direction = axisOffset < thumbStart ? -1 : 1;
            position.JumpTo(position.Pixels + (direction * position.ViewportDimension));
            ShowTemporarily();
        }

        private void HandlePointerMove(PointerMoveEvent @event, ScrollbarGeometry geometry)
        {
            if (_activePointer != @event.Pointer) return;
            _lastPointerAxisOffset = AxisOffset(@event.LocalPosition, geometry.Axis);
            if (!_draggingThumb || _controller?.PrimaryPosition is not { } position)
            {
                return;
            }

            double axisOffset = _lastPointerAxisOffset;
            double thumbOffset = Math.Clamp(
                axisOffset - _dragOffsetWithinThumb - geometry.TrackMainAxisStart,
                0,
                geometry.MaxThumbTravel);
            double fraction = geometry.MaxThumbTravel <= 0 ? 0 : thumbOffset / geometry.MaxThumbTravel;
            if (geometry.IsReversed) fraction = 1 - fraction;
            position.JumpTo(position.MinScrollExtent + (fraction * (position.MaxScrollExtent - position.MinScrollExtent)));
        }

        private void HandlePointerUp(PointerUpEvent @event, ScrollbarGeometry geometry) => EndPointer(@event.Pointer);

        private void HandlePointerCancel(PointerCancelEvent @event, ScrollbarGeometry geometry) => EndPointer(@event.Pointer);

        private void EndPointer(int pointer)
        {
            if (_activePointer != pointer) return;
            _activePointer = null;
            _pressController?.Stop();
            _pendingThumbPress = false;
            if (_draggingThumb)
            {
                _draggingThumb = false;
                SetInteractionState(_interactionState & ~ScrollbarInteractionState.Dragged);
            }

            ShowTemporarily();
        }

        private void BeginThumbDrag(double axisOffset, double thumbStart, double thumbExtent)
        {
            _pendingThumbPress = false;
            _pressController?.Stop();
            _draggingThumb = true;
            _dragOffsetWithinThumb = Math.Clamp(axisOffset - thumbStart, 0, thumbExtent);
            SetInteractionState(_interactionState | ScrollbarInteractionState.Dragged);
        }

        private void HandlePointerHover(PointerHoverEvent @event, ScrollbarGeometry geometry)
        {
            if (@event.Kind != PointerDeviceKind.Mouse ||
                !(CurrentWidget.Interactive ?? true) ||
                !IsPointerOverScrollbar(
                    @event.LocalPosition,
                    geometry,
                    includeHoverPadding: IsScrollbarTransparent()))
            {
                EndHover();
                return;
            }

            _fadeDelayController?.Stop();
            if (_fadeController is { Value: > 0 } fadeController)
            {
                fadeController.Reverse();
            }
            SetInteractionState(_interactionState | ScrollbarInteractionState.Hovered);
        }

        private void HandlePointerExit(PointerExitEvent @event, ScrollbarGeometry geometry)
        {
            EndHover();
        }

        private void EndHover()
        {
            if (!_interactionState.HasFlag(ScrollbarInteractionState.Hovered)) return;
            SetInteractionState(_interactionState & ~ScrollbarInteractionState.Hovered);
            if (CurrentWidget.ThumbVisibility != true)
            {
                _fadeDelayController?.Forward(0);
            }
        }

        private bool IsScrollbarTransparent()
        {
            bool forcedVisible = CurrentWidget.ThumbVisibility
                                 ?? CurrentWidget.ThumbVisibilityResolver?.Invoke(_interactionState)
                                 ?? false;
            return !forcedVisible && 1 - (_fadeController?.Evaluate() ?? 1) <= 0.001;
        }

        private static bool IsPointerOverScrollbar(
            Point position,
            ScrollbarGeometry geometry,
            bool includeHoverPadding)
        {
            if (!includeHoverPadding) return geometry.TrackRect.Contains(position);

            const double minInteractiveSize = 48;
            var center = geometry.ThumbRect.Center;
            var paddedThumb = new Rect(
                center.X - (minInteractiveSize / 2),
                center.Y - (minInteractiveSize / 2),
                minInteractiveSize,
                minInteractiveSize);
            var track = geometry.TrackRect;
            var hoverRect = new Rect(
                Math.Min(track.Left, paddedThumb.Left),
                Math.Min(track.Top, paddedThumb.Top),
                Math.Max(track.Right, paddedThumb.Right) - Math.Min(track.Left, paddedThumb.Left),
                Math.Max(track.Bottom, paddedThumb.Bottom) - Math.Min(track.Top, paddedThumb.Top));
            return hoverRect.Contains(position);
        }

        private void SetInteractionState(ScrollbarInteractionState value)
        {
            if (_interactionState == value) return;
            SetState(() =>
            {
                _interactionState = value;
                _paintRevision++;
            });
            CurrentWidget.InteractionChanged?.Invoke(value);
        }

        private static double AxisOffset(Point point, Axis axis) => axis == Axis.Vertical ? point.Y : point.X;
    }
}

// Compatibility wrapper retained for existing Plumix.Widgets call sites. Material applications
// should use Plumix.Material.Scrollbar, which supplies Flutter Material defaults and theming.
public sealed class Scrollbar : StatelessWidget
{
    public Scrollbar(
        Widget child,
        ScrollController? controller = null,
        double thickness = 4,
        Color? thumbColor = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Controller = controller;
        Thickness = thickness;
        ThumbColor = thumbColor ?? Color.Parse("#AA5A6B82");
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public double Thickness { get; }
    public Color ThumbColor { get; }

    public override Widget Build(BuildContext context) => new RawScrollbar(
        child: Child,
        controller: Controller,
        thickness: Thickness,
        thumbColor: ThumbColor);
}

internal sealed class RawScrollbarOverlay : SingleChildRenderObjectWidget
{
    public RawScrollbarOverlay(
        Func<ScrollPosition?> positionProvider,
        AxisDirection axisDirection,
        ScrollbarOrientation? orientation,
        double thickness,
        Color thumbColor,
        double radius,
        BorderSide? thumbBorder,
        double minThumbLength,
        double minOverscrollLength,
        bool trackVisible,
        double trackRadius,
        Color trackColor,
        Color trackBorderColor,
        double mainAxisMargin,
        double crossAxisMargin,
        Thickness padding,
        double opacity,
        bool interactive,
        int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit,
        Widget child) : base(child)
    {
        PositionProvider = positionProvider;
        AxisDirection = axisDirection;
        Orientation = orientation;
        Thickness = thickness;
        ThumbColor = thumbColor;
        Radius = radius;
        ThumbBorder = thumbBorder;
        MinThumbLength = minThumbLength;
        MinOverscrollLength = minOverscrollLength;
        TrackVisible = trackVisible;
        TrackRadius = trackRadius;
        TrackColor = trackColor;
        TrackBorderColor = trackBorderColor;
        MainAxisMargin = mainAxisMargin;
        CrossAxisMargin = crossAxisMargin;
        Padding = padding;
        Opacity = opacity;
        Interactive = interactive;
        PaintRevision = paintRevision;
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerHover = onPointerHover;
        OnPointerExit = onPointerExit;
    }

    public Func<ScrollPosition?> PositionProvider { get; }
    public AxisDirection AxisDirection { get; }
    public ScrollbarOrientation? Orientation { get; }
    public double Thickness { get; }
    public Color ThumbColor { get; }
    public double Radius { get; }
    public BorderSide? ThumbBorder { get; }
    public double MinThumbLength { get; }
    public double MinOverscrollLength { get; }
    public bool TrackVisible { get; }
    public double TrackRadius { get; }
    public Color TrackColor { get; }
    public Color TrackBorderColor { get; }
    public double MainAxisMargin { get; }
    public double CrossAxisMargin { get; }
    public Thickness Padding { get; }
    public double Opacity { get; }
    public bool Interactive { get; }
    public int PaintRevision { get; }
    public Action<PointerDownEvent, ScrollbarGeometry> OnPointerDown { get; }
    public Action<PointerMoveEvent, ScrollbarGeometry> OnPointerMove { get; }
    public Action<PointerUpEvent, ScrollbarGeometry> OnPointerUp { get; }
    public Action<PointerCancelEvent, ScrollbarGeometry> OnPointerCancel { get; }
    public Action<PointerHoverEvent, ScrollbarGeometry> OnPointerHover { get; }
    public Action<PointerExitEvent, ScrollbarGeometry> OnPointerExit { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderRawScrollbarOverlay(
        PositionProvider, AxisDirection, Orientation, Thickness, ThumbColor, Radius, ThumbBorder, MinThumbLength,
        MinOverscrollLength, TrackVisible, TrackRadius, TrackColor, TrackBorderColor, MainAxisMargin,
        CrossAxisMargin, Padding, Opacity, Interactive, PaintRevision, OnPointerDown, OnPointerMove,
        OnPointerUp, OnPointerCancel, OnPointerHover, OnPointerExit);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var overlay = (RenderRawScrollbarOverlay)renderObject;
        overlay.Update(
            PositionProvider, AxisDirection, Orientation, Thickness, ThumbColor, Radius, ThumbBorder, MinThumbLength,
            MinOverscrollLength, TrackVisible, TrackRadius, TrackColor, TrackBorderColor, MainAxisMargin,
            CrossAxisMargin, Padding, Opacity, Interactive, PaintRevision, OnPointerDown, OnPointerMove,
            OnPointerUp, OnPointerCancel, OnPointerHover, OnPointerExit);
    }
}

internal sealed class RenderRawScrollbarOverlay : RenderProxyBox
{
    private Func<ScrollPosition?> _positionProvider;
    private AxisDirection _axisDirection;
    private ScrollbarOrientation? _orientation;
    private double _thickness;
    private Color _thumbColor;
    private double _radius;
    private BorderSide? _thumbBorder;
    private double _minThumbLength;
    private bool _trackVisible;
    private double _trackRadius;
    private Color _trackColor;
    private Color _trackBorderColor;
    private double _mainAxisMargin;
    private double _crossAxisMargin;
    private Thickness _padding;
    private double _opacity;
    private bool _interactive;
    private int _paintRevision;
    private Action<PointerDownEvent, ScrollbarGeometry> _onPointerDown;
    private Action<PointerMoveEvent, ScrollbarGeometry> _onPointerMove;
    private Action<PointerUpEvent, ScrollbarGeometry> _onPointerUp;
    private Action<PointerCancelEvent, ScrollbarGeometry> _onPointerCancel;
    private Action<PointerHoverEvent, ScrollbarGeometry> _onPointerHover;
    private Action<PointerExitEvent, ScrollbarGeometry> _onPointerExit;

    public RenderRawScrollbarOverlay(
        Func<ScrollPosition?> positionProvider, AxisDirection axisDirection, ScrollbarOrientation? orientation,
        double thickness, Color thumbColor, double radius, BorderSide? thumbBorder, double minThumbLength, double minOverscrollLength,
        bool trackVisible, double trackRadius, Color trackColor, Color trackBorderColor, double mainAxisMargin,
        double crossAxisMargin, Thickness padding, double opacity, bool interactive, int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit)
    {
        _positionProvider = positionProvider;
        _axisDirection = axisDirection;
        _orientation = orientation;
        _thickness = thickness;
        _thumbColor = thumbColor;
        _radius = radius;
        _thumbBorder = thumbBorder;
        _minThumbLength = minThumbLength;
        _trackVisible = trackVisible;
        _trackRadius = trackRadius;
        _trackColor = trackColor;
        _trackBorderColor = trackBorderColor;
        _mainAxisMargin = mainAxisMargin;
        _crossAxisMargin = crossAxisMargin;
        _padding = padding;
        _opacity = opacity;
        _interactive = interactive;
        _paintRevision = paintRevision;
        _onPointerDown = onPointerDown;
        _onPointerMove = onPointerMove;
        _onPointerUp = onPointerUp;
        _onPointerCancel = onPointerCancel;
        _onPointerHover = onPointerHover;
        _onPointerExit = onPointerExit;
    }

    internal ScrollbarGeometry? Geometry => ComputeGeometry();
    internal Color ThumbColor => _thumbColor;
    internal Color TrackColor => _trackColor;
    internal Color TrackBorderColor => _trackBorderColor;
    internal double Thickness => _thickness;
    internal double Opacity => _opacity;
    internal bool TrackVisible => _trackVisible;

    public void Update(
        Func<ScrollPosition?> positionProvider, AxisDirection axisDirection, ScrollbarOrientation? orientation,
        double thickness, Color thumbColor, double radius, BorderSide? thumbBorder, double minThumbLength, double minOverscrollLength,
        bool trackVisible, double trackRadius, Color trackColor, Color trackBorderColor, double mainAxisMargin,
        double crossAxisMargin, Thickness padding, double opacity, bool interactive, int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit)
    {
        _positionProvider = positionProvider;
        _axisDirection = axisDirection;
        _orientation = orientation;
        _thickness = thickness;
        _thumbColor = thumbColor;
        _radius = radius;
        _thumbBorder = thumbBorder;
        _minThumbLength = minThumbLength;
        _trackVisible = trackVisible;
        _trackRadius = trackRadius;
        _trackColor = trackColor;
        _trackBorderColor = trackBorderColor;
        _mainAxisMargin = mainAxisMargin;
        _crossAxisMargin = crossAxisMargin;
        _padding = padding;
        _opacity = opacity;
        _interactive = interactive;
        _paintRevision = paintRevision;
        _onPointerDown = onPointerDown;
        _onPointerMove = onPointerMove;
        _onPointerUp = onPointerUp;
        _onPointerCancel = onPointerCancel;
        _onPointerHover = onPointerHover;
        _onPointerExit = onPointerExit;
        MarkNeedsPaint();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        base.Paint(context, offset);
        var geometry = ComputeGeometry();
        if (!geometry.HasValue || _opacity <= 0.001) return;

        var value = geometry.Value;
        if (_trackVisible)
        {
            var trackBrush = new SolidColorBrush(ApplyOpacity(_trackColor, _opacity));
            var borderColor = ApplyOpacity(_trackBorderColor, _opacity);
            IPen? pen = borderColor.A == 0 ? null : new Pen(new SolidColorBrush(borderColor), 1);
            context.DrawRectangle(
                trackBrush,
                pen,
                Translate(value.TrackRect, offset),
                _trackRadius,
                _trackRadius);
        }

        context.DrawRectangle(
            new SolidColorBrush(ApplyOpacity(_thumbColor, _opacity)),
            _thumbBorder is { } thumbBorder
                ? new Pen(
                    new SolidColorBrush(ApplyOpacity(thumbBorder.Color, _opacity)),
                    thumbBorder.Width)
                : null,
            Translate(value.ThumbRect, offset),
            _radius,
            _radius);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var geometry = ComputeGeometry();
        if (_interactive && _opacity > 0.001 && geometry is { } value && value.TrackRect.Contains(position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }

        return base.HitTest(result, position);
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (entry is not BoxHitTestEntry || ComputeGeometry() is not { } geometry) return;
        var localEvent = @event;
        switch (localEvent)
        {
            case PointerDownEvent down when IsInteractivePointerDown(down, geometry):
                _onPointerDown(down, geometry);
                break;
            case PointerMoveEvent move: _onPointerMove(move, geometry); break;
            case PointerUpEvent up: _onPointerUp(up, geometry); break;
            case PointerCancelEvent cancel: _onPointerCancel(cancel, geometry); break;
            case PointerHoverEvent hover: _onPointerHover(hover, geometry); break;
            case PointerExitEvent exit: _onPointerExit(exit, geometry); break;
        }
    }

    private bool IsInteractivePointerDown(PointerDownEvent @event, ScrollbarGeometry geometry)
    {
        return _interactive &&
               _opacity > 0.001 &&
               geometry.TrackRect.Contains(@event.LocalPosition);
    }

    private ScrollbarGeometry? ComputeGeometry()
    {
        var position = _positionProvider();
        if (position is null || position.ViewportDimension <= 0 || position.MaxScrollExtent <= position.MinScrollExtent)
        {
            return null;
        }

        var orientation = _orientation ?? (_axisDirection is AxisDirection.Left or AxisDirection.Right
            ? ScrollbarOrientation.Bottom
            : ScrollbarOrientation.Right);
        var axis = orientation is ScrollbarOrientation.Left or ScrollbarOrientation.Right
            ? Axis.Vertical
            : Axis.Horizontal;
        bool reversed = _axisDirection is AxisDirection.Up or AxisDirection.Left;
        double leadingPadding = axis == Axis.Vertical ? _padding.Top : _padding.Left;
        double trailingPadding = axis == Axis.Vertical ? _padding.Bottom : _padding.Right;
        double mainExtent = axis == Axis.Vertical ? Size.Height : Size.Width;
        double trackStart = leadingPadding + _mainAxisMargin;
        double trackExtent = Math.Max(0, mainExtent - leadingPadding - trailingPadding - (2 * _mainAxisMargin));
        if (trackExtent <= 0) return null;

        double totalContentExtent = position.MaxScrollExtent - position.MinScrollExtent + position.ViewportDimension;
        double thumbExtent = Math.Clamp(
            Math.Max(_minThumbLength, trackExtent * position.ViewportDimension / totalContentExtent),
            0,
            trackExtent);
        double fraction = Math.Clamp(
            (position.Pixels - position.MinScrollExtent) / (position.MaxScrollExtent - position.MinScrollExtent),
            0,
            1);
        if (reversed) fraction = 1 - fraction;
        double thumbOffset = fraction * Math.Max(0, trackExtent - thumbExtent);

        Rect trackRect;
        Rect thumbRect;
        if (axis == Axis.Vertical)
        {
            double x = orientation == ScrollbarOrientation.Left
                ? _padding.Left + _crossAxisMargin
                : Size.Width - _padding.Right - _crossAxisMargin - _thickness;
            trackRect = new Rect(x, trackStart, _thickness, trackExtent);
            thumbRect = new Rect(x, trackStart + thumbOffset, _thickness, thumbExtent);
        }
        else
        {
            double y = orientation == ScrollbarOrientation.Top
                ? _padding.Top + _crossAxisMargin
                : Size.Height - _padding.Bottom - _crossAxisMargin - _thickness;
            trackRect = new Rect(trackStart, y, trackExtent, _thickness);
            thumbRect = new Rect(trackStart + thumbOffset, y, thumbExtent, _thickness);
        }

        return new ScrollbarGeometry(
            trackRect,
            thumbRect,
            axis,
            reversed,
            trackStart,
            trackExtent,
            thumbOffset,
            thumbExtent);
    }

    private static Rect Translate(Rect rect, Point offset) => new(rect.Position + offset, rect.Size);

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(color.A * opacity), 0, 255), color.R, color.G, color.B);
}
