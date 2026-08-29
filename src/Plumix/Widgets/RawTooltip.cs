using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_tooltip.dart

public delegate Widget TooltipComponentBuilder(
    BuildContext context,
    Animation<double> animation);

public delegate Point TooltipPositionDelegate(TooltipPositionContext context);

public delegate void TooltipTriggeredCallback();

public sealed record TooltipPositionContext
{
    public TooltipPositionContext(
        Point Target,
        Size TargetSize,
        Size TooltipSize,
        double VerticalOffset,
        bool PreferBelow = true) : this(
        Target,
        TargetSize,
        TooltipSize,
        VerticalOffset,
        PreferBelow,
        new Size(double.PositiveInfinity, double.PositiveInfinity))
    {
    }

    public TooltipPositionContext(
        Point Target,
        Size TargetSize,
        Size TooltipSize,
        double VerticalOffset,
        Size OverlaySize) : this(
        Target,
        TargetSize,
        TooltipSize,
        VerticalOffset,
        PreferBelow: true,
        OverlaySize)
    {
    }

    public TooltipPositionContext(
        Point Target,
        Size TargetSize,
        Size TooltipSize,
        double VerticalOffset,
        bool PreferBelow,
        Size OverlaySize)
    {
        this.Target = Target;
        this.TargetSize = TargetSize;
        this.TooltipSize = TooltipSize;
        this.VerticalOffset = VerticalOffset;
        this.PreferBelow = PreferBelow;
        this.OverlaySize = OverlaySize;
    }

    public Point Target { get; init; }

    public Size TargetSize { get; init; }

    public Size TooltipSize { get; init; }

    public double VerticalOffset { get; init; }

    public bool PreferBelow { get; init; }

    public Size OverlaySize { get; init; }
}

public enum TooltipTriggerMode
{
    Manual,
    LongPress,
    Tap,
}

public sealed class RawTooltip : StatefulWidget
{
    private static readonly List<RawTooltipState> OpenedTooltips = [];

    public RawTooltip(
        string? semanticsTooltip,
        TooltipComponentBuilder tooltipBuilder,
        Widget child,
        TimeSpan? hoverDelay = null,
        TimeSpan? touchDelay = null,
        TimeSpan? dismissDelay = null,
        bool enableTapToDismiss = true,
        TooltipTriggerMode triggerMode = TooltipTriggerMode.LongPress,
        bool enableFeedback = true,
        TooltipTriggeredCallback? onTriggered = null,
        AnimationStyle? animationStyle = null,
        TooltipPositionDelegate? positionDelegate = null,
        bool ignorePointer = false,
        Key? key = null) : base(key)
    {
        SemanticsTooltip = semanticsTooltip;
        TooltipBuilder = tooltipBuilder ?? throw new ArgumentNullException(nameof(tooltipBuilder));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        HoverDelay = ValidateDuration(hoverDelay ?? TimeSpan.Zero, nameof(hoverDelay));
        TouchDelay = ValidateDuration(
            touchDelay ?? TimeSpan.FromMilliseconds(1500),
            nameof(touchDelay));
        DismissDelay = ValidateDuration(
            dismissDelay ?? TimeSpan.FromMilliseconds(100),
            nameof(dismissDelay));
        EnableTapToDismiss = enableTapToDismiss;
        TriggerMode = triggerMode;
        EnableFeedback = enableFeedback;
        OnTriggered = onTriggered;
        AnimationStyle = animationStyle ?? new AnimationStyle(
            Duration: TimeSpan.FromMilliseconds(150),
            ReverseDuration: TimeSpan.FromMilliseconds(75),
            Curve: Curves.FastOutSlowIn);
        PositionDelegate = positionDelegate;
        IgnorePointer = ignorePointer;
    }

    public string? SemanticsTooltip { get; }

    public TooltipComponentBuilder TooltipBuilder { get; }

    public TimeSpan HoverDelay { get; }

    public TimeSpan TouchDelay { get; }

    public TimeSpan DismissDelay { get; }

    public bool EnableTapToDismiss { get; }

    public TooltipTriggerMode TriggerMode { get; }

    public bool EnableFeedback { get; }

    public TooltipTriggeredCallback? OnTriggered { get; }

    public AnimationStyle AnimationStyle { get; }

    public TooltipPositionDelegate? PositionDelegate { get; }

    public bool IgnorePointer { get; }

    public Widget Child { get; }

    public static bool DismissAllToolTips()
    {
        if (OpenedTooltips.Count == 0)
        {
            return false;
        }

        foreach (RawTooltipState state in OpenedTooltips.ToArray())
        {
            state.ScheduleDismissTooltip();
        }

        return true;
    }

    public override State CreateState() => new RawTooltipState();

    internal static void AddOpened(RawTooltipState state)
    {
        if (!OpenedTooltips.Contains(state))
        {
            OpenedTooltips.Add(state);
        }
    }

    internal static void RemoveOpened(RawTooltipState state)
    {
        OpenedTooltips.Remove(state);
    }

    internal static IReadOnlyList<RawTooltipState> Opened => OpenedTooltips;

    private static TimeSpan ValidateDuration(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Tooltip durations must be non-negative.");
        }

        return value;
    }
}

public sealed class RawTooltipState : State
{
    private readonly OverlayPortalController _overlayController = new();
    private readonly HashSet<int> _activeHoveringPointers = [];
    private readonly HashSet<int> _triggerPointers = [];
    private AnimationController? _controller;
    private CurvedAnimation? _overlayAnimation;
    private AnimationController? _timer;
    private AnimationStatus _animationStatus = AnimationStatus.Dismissed;

    private RawTooltip CurrentWidget => (RawTooltip)StateWidget;

    private AnimationController Controller => _controller ??= CreateController();

    private CurvedAnimation OverlayAnimation => _overlayAnimation ??= new CurvedAnimation(
        Controller,
        CurrentWidget.AnimationStyle.Curve ?? Curves.FastOutSlowIn,
        CurrentWidget.AnimationStyle.ReverseCurve);

    public override void InitState()
    {
        base.InitState();
        GestureBinding.Instance.PointerRouter.AddGlobalRoute(HandleGlobalPointerEvent);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (_controller is null)
        {
            return;
        }

        _controller.Duration = CurrentWidget.AnimationStyle.Duration
                               ?? TimeSpan.FromMilliseconds(150);
        _controller.ReverseDuration = CurrentWidget.AnimationStyle.ReverseDuration
                                      ?? TimeSpan.FromMilliseconds(75);
        if (_overlayAnimation is not null)
        {
            _overlayAnimation.Curve = CurrentWidget.AnimationStyle.Curve
                                      ?? Curves.FastOutSlowIn;
            _overlayAnimation.ReverseCurve = CurrentWidget.AnimationStyle.ReverseCurve;
        }
    }

    public override void Dispose()
    {
        GestureBinding.Instance.PointerRouter.RemoveGlobalRoute(HandleGlobalPointerEvent);
        RawTooltip.RemoveOpened(this);
        CancelTimer();
        if (_controller is not null)
        {
            _controller.RemoveStatusListener(HandleStatusChanged);
            _controller.Dispose();
            _controller = null;
        }

        _overlayAnimation?.Dispose();
        _overlayAnimation = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        if (CurrentWidget.SemanticsTooltip?.Length == 0)
        {
            return CurrentWidget.Child;
        }

        Widget result = new Semantics(
            tooltip: CurrentWidget.SemanticsTooltip,
            child: CurrentWidget.Child);
        result = new Listener(
            behavior: HitTestBehavior.Opaque,
            onPointerDown: @event =>
            {
                if (CurrentWidget.TriggerMode != TooltipTriggerMode.Manual)
                {
                    _triggerPointers.Add(@event.Pointer);
                }
            },
            onPointerUp: @event => _triggerPointers.Remove(@event.Pointer),
            onPointerCancel: @event => _triggerPointers.Remove(@event.Pointer),
            child: new RawGestureDetector(
                excludeFromSemantics: true,
                behavior: HitTestBehavior.Opaque,
                gestures: BuildTriggerGestures(),
                child: result));
        result = new ExclusiveMouseRegion(
            onEnter: HandleMouseEnter,
            onExit: HandleMouseExit,
            child: result);

        return OverlayPortal.WithLayoutBuilder(
            controller: _overlayController,
            overlayChildBuilder: BuildTooltipOverlay,
            child: result);
    }

    public bool EnsureTooltipVisible()
    {
        CancelTimer();
        if (Controller.Status.IsForwardOrCompleted())
        {
            return false;
        }

        ScheduleShowTooltip(TimeSpan.Zero);
        return true;
    }

    internal void ScheduleDismissTooltip(TimeSpan? delay = null)
    {
        CancelTimer();
        if (_controller?.Status.IsForwardOrCompleted() != true)
        {
            return;
        }

        TimeSpan effectiveDelay = delay ?? TimeSpan.Zero;
        if (effectiveDelay > TimeSpan.Zero)
        {
            ScheduleTimer(effectiveDelay, () => Controller.Reverse());
        }
        else
        {
            Controller.Reverse();
        }
    }

    private AnimationController CreateController()
    {
        var controller = new AnimationController(
            duration: CurrentWidget.AnimationStyle.Duration ?? TimeSpan.FromMilliseconds(150), vsync: this)
        {
            ReverseDuration = CurrentWidget.AnimationStyle.ReverseDuration
                              ?? TimeSpan.FromMilliseconds(75),
        };
        controller.AddStatusListener(HandleStatusChanged);
        return controller;
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        bool wasDismissed = _animationStatus == AnimationStatus.Dismissed;
        bool isDismissed = status == AnimationStatus.Dismissed;
        if (!wasDismissed && isDismissed)
        {
            RawTooltip.RemoveOpened(this);
            _overlayController.Hide();
        }
        else if (wasDismissed && !isDismissed)
        {
            _overlayController.Show();
            RawTooltip.AddOpened(this);
            SemanticsService.Tooltip(CurrentWidget.SemanticsTooltip ?? string.Empty);
        }

        _animationStatus = status;
    }

    private void ScheduleShowTooltip(TimeSpan delay, TimeSpan? touchDelay = null)
    {
        void Show()
        {
            Controller.Forward();
            CancelTimer();
            if (touchDelay.HasValue)
            {
                ScheduleTimer(touchDelay.Value, () => Controller.Reverse());
            }
        }

        CancelTimer();
        if (Controller.Status == AnimationStatus.Dismissed && delay > TimeSpan.Zero)
        {
            ScheduleTimer(delay, Show);
        }
        else
        {
            Show();
        }
    }

    private void HandleGlobalPointerEvent(PointerEvent @event)
    {
        if (_triggerPointers.Contains(@event.Pointer))
        {
            if (@event is PointerUpEvent or PointerCancelEvent)
            {
                _triggerPointers.Remove(@event.Pointer);
            }

            return;
        }

        if (@event is PointerDownEvent
            && (_timer is not null || _controller?.Status != AnimationStatus.Dismissed))
        {
            HandleTapToDismiss();
        }
    }

    private void HandleTapToDismiss()
    {
        if (!CurrentWidget.EnableTapToDismiss)
        {
            return;
        }

        ScheduleDismissTooltip();
        _activeHoveringPointers.Clear();
    }

    private void HandleTap()
    {
        bool tooltipCreated = Controller.Status == AnimationStatus.Dismissed;
        if (tooltipCreated && CurrentWidget.EnableFeedback)
        {
            Feedback.ForTap();
        }

        CurrentWidget.OnTriggered?.Invoke();
        ScheduleShowTooltip(
            TimeSpan.Zero,
            _activeHoveringPointers.Count == 0 ? CurrentWidget.TouchDelay : null);
    }

    private void HandleLongPress()
    {
        bool tooltipCreated = Controller.Status == AnimationStatus.Dismissed;
        if (tooltipCreated && CurrentWidget.EnableFeedback)
        {
            Feedback.ForLongPress();
        }

        CurrentWidget.OnTriggered?.Invoke();
        ScheduleShowTooltip(TimeSpan.Zero);
    }

    private void HandlePressUp()
    {
        if (_activeHoveringPointers.Count == 0)
        {
            ScheduleDismissTooltip(CurrentWidget.TouchDelay);
        }
    }

    private void HandleMouseEnter(PointerEnterEvent @event)
    {
        _activeHoveringPointers.Add(@event.Pointer);
        RawTooltipState[] tooltipsToDismiss = RawTooltipStatesToDismiss();
        foreach (RawTooltipState tooltip in tooltipsToDismiss)
        {
            tooltip.ScheduleDismissTooltip();
        }

        ScheduleShowTooltip(
            tooltipsToDismiss.Length > 0 ? TimeSpan.Zero : CurrentWidget.HoverDelay);
    }

    private void HandleMouseExit(PointerExitEvent @event)
    {
        _activeHoveringPointers.Remove(@event.Pointer);
        if (_activeHoveringPointers.Count == 0)
        {
            ScheduleDismissTooltip(CurrentWidget.DismissDelay);
        }
    }

    private RawTooltipState[] RawTooltipStatesToDismiss()
    {
        return GetOpenedTooltips()
            .Where(tooltip => !ReferenceEquals(tooltip, this)
                              && tooltip._activeHoveringPointers.Count == 0)
            .ToArray();
    }

    private static IEnumerable<RawTooltipState> GetOpenedTooltips()
    {
        return RawTooltip.Opened;
    }

    private Widget BuildTooltipOverlay(
        BuildContext context,
        OverlayChildLayoutInfo layoutInfo)
    {
        if (layoutInfo.ChildPaintTransform.Determinant() == 0.0)
        {
            return new SizedBox();
        }

        Point target = MatrixUtils.TransformPoint(
            layoutInfo.ChildPaintTransform,
            new Point(
                layoutInfo.ChildSize.Width / 2.0,
                layoutInfo.ChildSize.Height / 2.0));
        Widget tooltip = new IgnorePointer(
            ignoring: CurrentWidget.IgnorePointer,
            child: new ExclusiveMouseRegion(
                onEnter: HandleMouseEnter,
                onExit: HandleMouseExit,
                child: CurrentWidget.TooltipBuilder(context, OverlayAnimation)));

        double bottomViewInset = MediaQuery.MaybeViewInsetsOf(context)?.Bottom ?? 0.0;
        Widget overlayChild = new Padding(
            insets: new Thickness(0, 0, 0, bottomViewInset),
            child: new CustomSingleChildLayout(
                layoutDelegate: new RawTooltipPositionLayoutDelegate(
                    target,
                    layoutInfo.ChildSize,
                    CurrentWidget.PositionDelegate),
                child: tooltip));
        if (SelectionContainer.MaybeOf(context) is not null)
        {
            overlayChild = SelectionContainer.Disabled(overlayChild);
        }

        return overlayChild;
    }

    private void ScheduleTimer(TimeSpan delay, Action callback)
    {
        CancelTimer();
        if (delay <= TimeSpan.Zero)
        {
            callback();
            return;
        }

        var timer = new AnimationController(duration: delay, vsync: this);
        Action? completed = null;
        completed = () =>
        {
            timer.Completed -= completed;
            timer.Dispose();
            if (ReferenceEquals(_timer, timer))
            {
                _timer = null;
            }

            if (Mounted)
            {
                callback();
            }
        };
        timer.Completed += completed;
        _timer = timer;
        timer.Forward(from: 0);
    }

    private void CancelTimer()
    {
        AnimationController? timer = _timer;
        _timer = null;
        timer?.Dispose();
    }

    /// <summary>
    /// The tap/long-press recognizers the current <see cref="TooltipTriggerMode"/> needs, restricted
    /// to the pointer kinds a tooltip may be triggered from.
    /// </summary>
    private IReadOnlyDictionary<Type, IGestureRecognizerFactory> BuildTriggerGestures()
    {
        var gestures = new Dictionary<Type, IGestureRecognizerFactory>();
        if (CurrentWidget.TriggerMode == TooltipTriggerMode.Tap)
        {
            gestures[typeof(TapGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                    () => new TapGestureRecognizer { SupportedDevices = TriggerDeviceKinds },
                    instance =>
                    {
                        instance.OnTap = HandleTap;
                        instance.OnTapCancel = HandleTapToDismiss;
                        instance.SupportedDevices = TriggerDeviceKinds;
                    });
        }

        if (CurrentWidget.TriggerMode == TooltipTriggerMode.LongPress)
        {
            gestures[typeof(LongPressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                    () => new LongPressGestureRecognizer { SupportedDevices = TriggerDeviceKinds },
                    instance =>
                    {
                        instance.OnLongPress = HandleLongPress;
                        instance.OnLongPressUp = HandlePressUp;
                        instance.SupportedDevices = TriggerDeviceKinds;
                    });
        }

        return gestures;
    }

    private static IReadOnlySet<PointerDeviceKind> TriggerDeviceKinds { get; } =
        new HashSet<PointerDeviceKind>
        {
            PointerDeviceKind.InvertedStylus,
            PointerDeviceKind.Stylus,
            PointerDeviceKind.Touch,
            PointerDeviceKind.Trackpad,
            PointerDeviceKind.Unknown,
        };
}

internal sealed class RawTooltipPositionLayoutDelegate : SingleChildLayoutDelegate
{
    public RawTooltipPositionLayoutDelegate(
        Point target,
        Size targetSize,
        TooltipPositionDelegate? positionDelegate)
    {
        Target = target;
        TargetSize = targetSize;
        PositionDelegate = positionDelegate;
    }

    public Point Target { get; }

    public Size TargetSize { get; }

    public TooltipPositionDelegate? PositionDelegate { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) =>
        constraints.Loosen();

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        var context = new TooltipPositionContext(
            Target,
            TargetSize,
            childSize,
            VerticalOffset: 0,
            OverlaySize: size);
        return PositionDelegate?.Invoke(context)
               ?? PositionDependentBox(
                   size,
                   childSize,
                   Target,
                   preferBelow: true);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not RawTooltipPositionLayoutDelegate oldTooltip
               || Target != oldTooltip.Target
               || TargetSize != oldTooltip.TargetSize
               || PositionDelegate != oldTooltip.PositionDelegate;
    }

    public static Point PositionDependentBox(
        Size size,
        Size childSize,
        Point target,
        bool preferBelow,
        double verticalOffset = 0,
        double margin = 10)
    {
        bool fitsBelow = target.Y + verticalOffset + childSize.Height <= size.Height - margin;
        bool fitsAbove = target.Y - verticalOffset - childSize.Height >= margin;
        bool tooltipBelow = fitsAbove == fitsBelow ? preferBelow : fitsBelow;
        double y = tooltipBelow
            ? Math.Min(target.Y + verticalOffset, size.Height - margin)
            : Math.Max(target.Y - verticalOffset - childSize.Height, margin);
        double flexibleSpace = size.Width - childSize.Width;
        double x = flexibleSpace <= 2 * margin
            ? flexibleSpace / 2.0
            : Math.Clamp(target.X - childSize.Width / 2.0, margin, flexibleSpace - margin);
        return new Point(x, y);
    }
}

internal sealed class ExclusiveMouseRegion : SingleChildRenderObjectWidget
{
    public ExclusiveMouseRegion(
        Action<PointerEnterEvent>? onEnter = null,
        Action<PointerExitEvent>? onExit = null,
        Widget? child = null) : base(child)
    {
        OnEnter = onEnter;
        OnExit = onExit;
    }

    public Action<PointerEnterEvent>? OnEnter { get; }

    public Action<PointerExitEvent>? OnExit { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderExclusiveMouseRegion(OnEnter, OnExit);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var region = (RenderExclusiveMouseRegion)renderObject;
        region.OnPointerEnter = OnEnter;
        region.OnPointerExit = OnExit;
    }
}

internal sealed class RenderExclusiveMouseRegion : RenderPointerListener
{
    [ThreadStatic]
    private static bool _isOutermostMouseRegion;

    [ThreadStatic]
    private static bool _foundInnermostMouseRegion;

    public RenderExclusiveMouseRegion(
        Action<PointerEnterEvent>? onEnter,
        Action<PointerExitEvent>? onExit) : base(
        onPointerEnter: onEnter,
        onPointerExit: onExit)
    {
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        bool outermost = !_isOutermostMouseRegion;
        if (outermost)
        {
            _isOutermostMouseRegion = true;
            _foundInnermostMouseRegion = false;
        }

        bool hit = false;
        if (position.X >= 0 && position.Y >= 0 && position.X <= Size.Width && position.Y <= Size.Height)
        {
            hit = HitTestChildren(result, position) || HitTestSelf(position);
            if ((hit || Behavior == HitTestBehavior.Translucent) && !_foundInnermostMouseRegion)
            {
                _foundInnermostMouseRegion = true;
                result.Add(new BoxHitTestEntry(this, position));
            }
        }

        if (outermost)
        {
            _isOutermostMouseRegion = false;
            _foundInnermostMouseRegion = false;
        }

        return hit;
    }
}
