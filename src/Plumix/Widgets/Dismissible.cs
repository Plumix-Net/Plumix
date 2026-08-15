using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/dismissible.dart

public delegate void DismissDirectionCallback(DismissDirection direction);

public delegate Task<bool?> ConfirmDismissCallback(DismissDirection direction);

public delegate void DismissUpdateCallback(DismissUpdateDetails details);

public enum DismissDirection
{
    Vertical,
    Horizontal,
    EndToStart,
    StartToEnd,
    Up,
    Down,
    None,
}

public sealed class DismissUpdateDetails
{
    public DismissUpdateDetails(
        DismissDirection direction = DismissDirection.Horizontal,
        bool reached = false,
        bool previousReached = false,
        double progress = 0.0)
    {
        Direction = direction;
        Reached = reached;
        PreviousReached = previousReached;
        Progress = progress;
    }

    public DismissDirection Direction { get; }

    public bool Reached { get; }

    public bool PreviousReached { get; }

    public double Progress { get; }
}

public sealed class Dismissible : StatefulWidget
{
    public Dismissible(
        Key key,
        Widget child,
        Widget? background = null,
        Widget? secondaryBackground = null,
        ConfirmDismissCallback? confirmDismiss = null,
        Action? onResize = null,
        DismissUpdateCallback? onUpdate = null,
        DismissDirectionCallback? onDismissed = null,
        DismissDirection direction = DismissDirection.Horizontal,
        IReadOnlyDictionary<DismissDirection, double>? dismissThresholds = null,
        TimeSpan? movementDuration = null,
        double crossAxisEndOffset = 0.0,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        HitTestBehavior behavior = HitTestBehavior.Opaque) : this(
            key: key,
            child: child,
            resizeDuration: DefaultResizeDuration,
            background: background,
            secondaryBackground: secondaryBackground,
            confirmDismiss: confirmDismiss,
            onResize: onResize,
            onUpdate: onUpdate,
            onDismissed: onDismissed,
            direction: direction,
            dismissThresholds: dismissThresholds,
            movementDuration: movementDuration,
            crossAxisEndOffset: crossAxisEndOffset,
            dragStartBehavior: dragStartBehavior,
            behavior: behavior)
    {
    }

    public Dismissible(
        Key key,
        Widget child,
        TimeSpan? resizeDuration,
        Widget? background = null,
        Widget? secondaryBackground = null,
        ConfirmDismissCallback? confirmDismiss = null,
        Action? onResize = null,
        DismissUpdateCallback? onUpdate = null,
        DismissDirectionCallback? onDismissed = null,
        DismissDirection direction = DismissDirection.Horizontal,
        IReadOnlyDictionary<DismissDirection, double>? dismissThresholds = null,
        TimeSpan? movementDuration = null,
        double crossAxisEndOffset = 0.0,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        HitTestBehavior behavior = HitTestBehavior.Opaque) : base(
            key ?? throw new ArgumentNullException(nameof(key)))
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        if (secondaryBackground is not null && background is null)
        {
            throw new ArgumentException(
                "A secondary background can only be provided with a primary background.",
                nameof(secondaryBackground));
        }

        TimeSpan resolvedMovementDuration = movementDuration ?? TimeSpan.FromMilliseconds(200);
        if (resizeDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(resizeDuration));
        }
        if (resolvedMovementDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(movementDuration));
        }
        if (!double.IsFinite(crossAxisEndOffset))
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisEndOffset));
        }

        var thresholds = dismissThresholds is null
            ? new Dictionary<DismissDirection, double>()
            : new Dictionary<DismissDirection, double>(dismissThresholds);
        foreach (var threshold in thresholds)
        {
            if (double.IsNaN(threshold.Value) || threshold.Value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dismissThresholds),
                    "Dismiss thresholds must be non-negative.");
            }
        }

        Background = background;
        SecondaryBackground = secondaryBackground;
        ConfirmDismiss = confirmDismiss;
        OnResize = onResize;
        OnUpdate = onUpdate;
        OnDismissed = onDismissed;
        Direction = direction;
        ResizeDuration = resizeDuration;
        DismissThresholds = thresholds;
        MovementDuration = resolvedMovementDuration;
        CrossAxisEndOffset = crossAxisEndOffset;
        DragStartBehavior = dragStartBehavior;
        Behavior = behavior;
    }

    public static TimeSpan DefaultResizeDuration { get; } = TimeSpan.FromMilliseconds(300);

    public static TimeSpan DefaultMovementDuration { get; } = TimeSpan.FromMilliseconds(200);

    public Widget Child { get; }

    public Widget? Background { get; }

    public Widget? SecondaryBackground { get; }

    public ConfirmDismissCallback? ConfirmDismiss { get; }

    public Action? OnResize { get; }

    public DismissUpdateCallback? OnUpdate { get; }

    public DismissDirectionCallback? OnDismissed { get; }

    public DismissDirection Direction { get; }

    public TimeSpan? ResizeDuration { get; }

    public IReadOnlyDictionary<DismissDirection, double> DismissThresholds { get; }

    public TimeSpan MovementDuration { get; }

    public double CrossAxisEndOffset { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public HitTestBehavior Behavior { get; }

    public override State CreateState() => new DismissibleState();

    private sealed class DismissibleState : AutomaticKeepAliveClientMixin
    {
        private const double MinFlingVelocity = 700.0;
        private const double MinFlingVelocityDelta = 400.0;
        private const double FlingVelocityScale = 1.0 / 300.0;
        private const double DefaultDismissThreshold = 0.4;

        private readonly AnimationController _moveController = new(duration: DefaultMovementDuration);
        private readonly LabeledGlobalKey<State> _contentKey = new("Dismissible");
        private Animation<Vector> _moveAnimation = null!;
        private AnimationController? _resizeController;
        private MappedDoubleAnimation? _resizeAnimation;
        private double _dragExtent;
        private bool _confirming;
        private bool _dragUnderway;
        private bool _handlingMoveCompletion;
        private Size? _sizePriorToCollapse;
        private bool _dismissThresholdReached;

        private Dismissible CurrentWidget => (Dismissible)StateWidget;

        protected override bool WantKeepAlive =>
            _moveController.IsAnimating || (_resizeController?.IsAnimating ?? false);

        private bool DirectionIsXAxis => CurrentWidget.Direction is
            DismissDirection.Horizontal or
            DismissDirection.EndToStart or
            DismissDirection.StartToEnd;

        private DismissDirection CurrentDismissDirection => ExtentToDirection(_dragExtent);

        private double DismissThreshold => CurrentWidget.DismissThresholds.TryGetValue(
            CurrentDismissDirection,
            out double threshold)
                ? threshold
                : DefaultDismissThreshold;

        public override void InitState()
        {
            base.InitState();
            _moveController.Duration = CurrentWidget.MovementDuration;
            _moveController.AddStatusListener(HandleDismissStatusChanged);
            _moveController.AddListener(HandleDismissUpdateValueChanged);
            UpdateMoveAnimation();
        }

        public override void Dispose()
        {
            _moveController.RemoveStatusListener(HandleDismissStatusChanged);
            _moveController.RemoveListener(HandleDismissUpdateValueChanged);
            _moveController.Dispose();
            _resizeAnimation?.Dispose();
            _resizeController?.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            Widget? background = CurrentWidget.Background;
            if (CurrentWidget.SecondaryBackground is not null)
            {
                DismissDirection direction = CurrentDismissDirection;
                if (direction is DismissDirection.EndToStart or DismissDirection.Up)
                {
                    background = CurrentWidget.SecondaryBackground;
                }
            }

            if (_resizeAnimation is not null)
            {
                Size collapsedFrom = _sizePriorToCollapse ?? default;
                return new SizeTransition(
                    sizeFactor: _resizeAnimation,
                    axis: DirectionIsXAxis ? Axis.Vertical : Axis.Horizontal,
                    child: new SizedBox(
                        width: collapsedFrom.Width,
                        height: collapsedFrom.Height,
                        child: background));
            }

            Widget content = new SlideTransition(
                position: _moveAnimation,
                child: new KeyedSubtree(
                    key: _contentKey,
                    child: CurrentWidget.Child));

            if (background is not null)
            {
                var children = new List<Widget>();
                if (_moveController.Value > 0.0)
                {
                    children.Add(new Positioned(
                        left: 0,
                        top: 0,
                        right: 0,
                        bottom: 0,
                        child: new ClipRect(
                            clipper: new DismissibleClipper(
                                axis: DirectionIsXAxis ? Axis.Horizontal : Axis.Vertical,
                                moveAnimation: _moveAnimation),
                            child: background)));
                }

                children.Add(content);
                content = new Stack(children: children);
            }

            if (CurrentWidget.Direction == DismissDirection.None)
            {
                return content;
            }

            return new GestureDetector(
                onHorizontalDragStart: DirectionIsXAxis ? HandleDragStart : null,
                onHorizontalDragUpdate: DirectionIsXAxis ? HandleDragUpdate : null,
                onHorizontalDragEnd: DirectionIsXAxis ? HandleDragEnd : null,
                onVerticalDragStart: DirectionIsXAxis ? null : HandleDragStart,
                onVerticalDragUpdate: DirectionIsXAxis ? null : HandleDragUpdate,
                onVerticalDragEnd: DirectionIsXAxis ? null : HandleDragEnd,
                behavior: CurrentWidget.Behavior,
                dragStartBehavior: CurrentWidget.DragStartBehavior,
                child: content);
        }

        private double OverallDragAxisExtent
        {
            get
            {
                RenderBox? renderBox = _contentKey.CurrentContext?.FindRenderObject() as RenderBox;
                if (renderBox is null || !renderBox.HasSize)
                {
                    return 0.0;
                }

                return DirectionIsXAxis ? renderBox.Size.Width : renderBox.Size.Height;
            }
        }

        private DismissDirection ExtentToDirection(double extent)
        {
            if (extent == 0.0)
            {
                return DismissDirection.None;
            }

            if (DirectionIsXAxis)
            {
                TextDirection textDirection = Directionality.Of(Context);
                bool towardStart = textDirection == TextDirection.Rtl ? extent < 0.0 : extent > 0.0;
                return towardStart ? DismissDirection.StartToEnd : DismissDirection.EndToStart;
            }

            return extent > 0.0 ? DismissDirection.Down : DismissDirection.Up;
        }

        private void HandleDragStart(DragStartDetails details)
        {
            if (_confirming)
            {
                return;
            }

            _dragUnderway = true;
            double extent = OverallDragAxisExtent;
            if (_moveController.IsAnimating && extent > 0.0)
            {
                _dragExtent = _moveController.Value * extent * Math.Sign(_dragExtent);
                _moveController.Stop();
            }
            else
            {
                _dragExtent = 0.0;
                _moveController.SetValue(0.0);
            }

            SetState(UpdateMoveAnimation);
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            if (!_dragUnderway || _moveController.IsAnimating)
            {
                return;
            }

            double delta = details.PrimaryDelta;
            double oldDragExtent = _dragExtent;
            switch (CurrentWidget.Direction)
            {
                case DismissDirection.Horizontal:
                case DismissDirection.Vertical:
                    _dragExtent += delta;
                    break;
                case DismissDirection.Up when _dragExtent + delta < 0.0:
                case DismissDirection.StartToEnd
                    when Directionality.Of(Context) == TextDirection.Rtl && _dragExtent + delta < 0.0:
                case DismissDirection.EndToStart
                    when Directionality.Of(Context) == TextDirection.Ltr && _dragExtent + delta < 0.0:
                    _dragExtent += delta;
                    break;
                case DismissDirection.Down when _dragExtent + delta > 0.0:
                case DismissDirection.StartToEnd
                    when Directionality.Of(Context) == TextDirection.Ltr && _dragExtent + delta > 0.0:
                case DismissDirection.EndToStart
                    when Directionality.Of(Context) == TextDirection.Rtl && _dragExtent + delta > 0.0:
                    _dragExtent += delta;
                    break;
                case DismissDirection.None:
                    _dragExtent = 0.0;
                    break;
            }

            if (Math.Sign(oldDragExtent) != Math.Sign(_dragExtent))
            {
                SetState(UpdateMoveAnimation);
            }

            double extent = OverallDragAxisExtent;
            if (!_moveController.IsAnimating && extent > 0.0)
            {
                _moveController.SetValue(Math.Abs(_dragExtent) / extent);
            }
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            if (!_dragUnderway || _moveController.IsAnimating)
            {
                return;
            }

            _dragUnderway = false;
            if (_moveController.Value >= 1.0)
            {
                _ = HandleMoveCompletedAsync();
                return;
            }

            double flingVelocity = DirectionIsXAxis
                ? details.Velocity.PixelsPerSecond.X
                : details.Velocity.PixelsPerSecond.Y;
            switch (DescribeFlingGesture(details.Velocity))
            {
                case FlingGestureKind.Forward:
                    if (DismissThreshold >= 1.0)
                    {
                        _moveController.Reverse();
                        break;
                    }

                    _dragExtent = Math.Sign(flingVelocity);
                    _moveController.Fling(Math.Abs(flingVelocity) * FlingVelocityScale);
                    break;
                case FlingGestureKind.Reverse:
                    _dragExtent = Math.Sign(flingVelocity);
                    _moveController.Fling(-Math.Abs(flingVelocity) * FlingVelocityScale);
                    break;
                case FlingGestureKind.None:
                    if (_moveController.Value > 0.0)
                    {
                        if (_moveController.Value > DismissThreshold)
                        {
                            _moveController.Forward();
                        }
                        else
                        {
                            _moveController.Reverse();
                        }
                    }
                    break;
            }
        }

        private FlingGestureKind DescribeFlingGesture(Velocity velocity)
        {
            if (_dragExtent == 0.0)
            {
                return FlingGestureKind.None;
            }

            double velocityX = velocity.PixelsPerSecond.X;
            double velocityY = velocity.PixelsPerSecond.Y;
            DismissDirection flingDirection;
            if (DirectionIsXAxis)
            {
                if (Math.Abs(velocityX) - Math.Abs(velocityY) < MinFlingVelocityDelta
                    || Math.Abs(velocityX) < MinFlingVelocity)
                {
                    return FlingGestureKind.None;
                }

                flingDirection = ExtentToDirection(velocityX);
            }
            else
            {
                if (Math.Abs(velocityY) - Math.Abs(velocityX) < MinFlingVelocityDelta
                    || Math.Abs(velocityY) < MinFlingVelocity)
                {
                    return FlingGestureKind.None;
                }

                flingDirection = ExtentToDirection(velocityY);
            }

            return flingDirection == CurrentDismissDirection
                ? FlingGestureKind.Forward
                : FlingGestureKind.Reverse;
        }

        private void HandleDismissUpdateValueChanged()
        {
            if (CurrentWidget.OnUpdate is null)
            {
                return;
            }

            bool previousReached = _dismissThresholdReached;
            _dismissThresholdReached = _moveController.Value > DismissThreshold;
            CurrentWidget.OnUpdate(new DismissUpdateDetails(
                direction: CurrentDismissDirection,
                reached: _dismissThresholdReached,
                previousReached: previousReached,
                progress: _moveController.Value));
        }

        private void HandleDismissStatusChanged(AnimationStatus status)
        {
            if (status == AnimationStatus.Completed && !_dragUnderway)
            {
                _ = HandleMoveCompletedAsync();
            }

            if (Mounted)
            {
                UpdateKeepAlive();
            }
        }

        private async Task HandleMoveCompletedAsync()
        {
            if (_handlingMoveCompletion || !Mounted)
            {
                return;
            }

            _handlingMoveCompletion = true;
            try
            {
                if (DismissThreshold >= 1.0)
                {
                    _moveController.Reverse();
                    return;
                }

                bool result = await ConfirmStartResizeAnimationAsync();
                if (!Mounted)
                {
                    return;
                }

                if (result)
                {
                    StartResizeAnimation();
                }
                else
                {
                    _moveController.Reverse();
                }
            }
            finally
            {
                _handlingMoveCompletion = false;
            }
        }

        private async Task<bool> ConfirmStartResizeAnimationAsync()
        {
            if (CurrentWidget.ConfirmDismiss is null)
            {
                return true;
            }

            _confirming = true;
            DismissDirection direction = CurrentDismissDirection;
            try
            {
                return await CurrentWidget.ConfirmDismiss(direction) ?? false;
            }
            finally
            {
                _confirming = false;
            }
        }

        private void StartResizeAnimation()
        {
            if (CurrentWidget.ResizeDuration is null)
            {
                CurrentWidget.OnDismissed?.Invoke(CurrentDismissDirection);
                return;
            }

            _sizePriorToCollapse = Context.FindRenderObject() is RenderBox renderBox && renderBox.HasSize
                ? renderBox.Size
                : _contentKey.CurrentContext?.FindRenderObject() is RenderBox content && content.HasSize
                    ? content.Size
                    : default;
            _resizeController = new AnimationController(duration: CurrentWidget.ResizeDuration.Value, vsync: this);
            _resizeController.AddListener(HandleResizeProgressChanged);
            _resizeController.AddStatusListener(HandleResizeStatusChanged);
            _resizeAnimation = new MappedDoubleAnimation(
                _resizeController,
                value => 1.0 - Curves.Ease(Math.Clamp((value - 0.4) / 0.6, 0.0, 1.0)));
            SetState(() => { });
            _resizeController.Forward();
        }

        private void HandleResizeProgressChanged()
        {
            if (_resizeController?.Status == AnimationStatus.Completed)
            {
                CurrentWidget.OnDismissed?.Invoke(CurrentDismissDirection);
            }
            else
            {
                CurrentWidget.OnResize?.Invoke();
            }
        }

        private void HandleResizeStatusChanged(AnimationStatus status)
        {
            if (Mounted)
            {
                UpdateKeepAlive();
            }
        }

        private void UpdateMoveAnimation()
        {
            double end = Math.Sign(_dragExtent);
            Vector target = DirectionIsXAxis
                ? new Vector(end, CurrentWidget.CrossAxisEndOffset)
                : new Vector(CurrentWidget.CrossAxisEndOffset, end);
            _moveAnimation = new DismissibleMoveAnimation(_moveController, target);
        }
    }

    private sealed class DismissibleMoveAnimation(AnimationController parent, Vector end) : Animation<Vector>
    {
        public override Vector Value => new(end.X * parent.Value, end.Y * parent.Value);

        public override AnimationStatus Status => parent.Status;

        public override void AddListener(Action listener) => parent.AddListener(listener);

        public override void RemoveListener(Action listener) => parent.RemoveListener(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) => parent.AddStatusListener(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            parent.RemoveStatusListener(listener);
        }
    }

    private sealed class DismissibleClipper : CustomClipper<Rect>
    {
        public DismissibleClipper(Axis axis, Animation<Vector> moveAnimation) : base(moveAnimation)
        {
            Axis = axis;
            MoveAnimation = moveAnimation;
        }

        public Axis Axis { get; }

        public Animation<Vector> MoveAnimation { get; }

        public override Rect GetClip(Size size)
        {
            if (Axis == Axis.Horizontal)
            {
                double offset = MoveAnimation.Value.X * size.Width;
                return offset < 0.0
                    ? new Rect(size.Width + offset, 0.0, -offset, size.Height)
                    : new Rect(0.0, 0.0, offset, size.Height);
            }

            double verticalOffset = MoveAnimation.Value.Y * size.Height;
            return verticalOffset < 0.0
                ? new Rect(0.0, size.Height + verticalOffset, size.Width, -verticalOffset)
                : new Rect(0.0, 0.0, size.Width, verticalOffset);
        }

        public override Rect GetApproximateClipRect(Size size) => GetClip(size);

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper)
        {
            return oldClipper is not DismissibleClipper oldDismissibleClipper
                   || oldDismissibleClipper.Axis != Axis
                   || oldDismissibleClipper.MoveAnimation.Value != MoveAnimation.Value;
        }
    }

    private enum FlingGestureKind
    {
        None,
        Forward,
        Reverse,
    }
}
