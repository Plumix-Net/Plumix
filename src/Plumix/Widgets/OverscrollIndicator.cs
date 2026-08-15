using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/overscroll_indicator.dart
// - flutter/packages/flutter/lib/src/widgets/stretch_effect.dart

public sealed class OverscrollIndicatorNotification : Notification, IViewportNotification
{
    public OverscrollIndicatorNotification(bool leading)
    {
        Leading = leading;
    }

    public bool Leading { get; }

    public int Depth { get; private set; }

    public double PaintOffset { get; set; }

    public bool Accepted { get; private set; } = true;

    public void DisallowIndicator()
    {
        Accepted = false;
    }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }
}

public sealed class StretchEffect : StatelessWidget
{
    public StretchEffect(
        Axis axis,
        Widget child,
        double stretchStrength = 0.0,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(stretchStrength) || stretchStrength is < -1.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stretchStrength),
                "stretchStrength must be between -1.0 and 1.0.");
        }

        Axis = axis;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        StretchStrength = stretchStrength;
    }

    public double StretchStrength { get; }

    public Axis Axis { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        TextDirection direction = Directionality.Of(context);
        bool isForward = StretchStrength > 0.0;
        Alignment alignment;
        if (Axis == Axis.Vertical)
        {
            alignment = isForward ? Alignment.TopCenter : Alignment.BottomCenter;
        }
        else
        {
            alignment = direction == TextDirection.Rtl
                ? isForward
                    ? Alignment.CenterRight
                    : Alignment.CenterLeft
                : isForward
                    ? Alignment.CenterLeft
                    : Alignment.CenterRight;
        }

        double x = Axis == Axis.Horizontal ? 1.0 + Math.Abs(StretchStrength) : 1.0;
        double y = Axis == Axis.Vertical ? 1.0 + Math.Abs(StretchStrength) : 1.0;
        return new Transform(
            transform: Matrix4.Diagonal3Values(x, y, 1.0),
            alignment: alignment,
            filterQuality: StretchStrength == 0.0 ? null : FilterQuality.Medium,
            child: Child);
    }
}

public sealed class GlowingOverscrollIndicator : StatefulWidget
{
    public GlowingOverscrollIndicator(
        AxisDirection axisDirection,
        Color color,
        Widget? child = null,
        bool showLeading = true,
        bool showTrailing = true,
        ScrollNotificationPredicate? notificationPredicate = null,
        Key? key = null) : base(key)
    {
        AxisDirection = axisDirection;
        Color = color;
        Child = child;
        ShowLeading = showLeading;
        ShowTrailing = showTrailing;
        NotificationPredicate =
            notificationPredicate ?? RawScrollbar.DefaultScrollNotificationPredicate;
    }

    public bool ShowLeading { get; }

    public bool ShowTrailing { get; }

    public AxisDirection AxisDirection { get; }

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

    public Color Color { get; }

    public ScrollNotificationPredicate NotificationPredicate { get; }

    public Widget? Child { get; }

    public override State CreateState() => new GlowingOverscrollIndicatorState();
}

internal sealed class GlowingOverscrollIndicatorState : State
{
    private readonly Dictionary<bool, bool> _accepted = new()
    {
        [false] = true,
        [true] = true,
    };

    private GlowController _leadingController = null!;
    private GlowController _trailingController = null!;
    private MergedListenable _controllers = null!;
    private Type? _lastNotificationType;

    private GlowingOverscrollIndicator CurrentWidget =>
        (GlowingOverscrollIndicator)StateWidget;

    internal GlowController LeadingController => _leadingController;

    internal GlowController TrailingController => _trailingController;

    public override void InitState()
    {
        _leadingController = new GlowController(CurrentWidget.Color, CurrentWidget.Axis, this);
        _trailingController = new GlowController(CurrentWidget.Color, CurrentWidget.Axis, this);
        _controllers = new MergedListenable(_leadingController, _trailingController);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldIndicator = (GlowingOverscrollIndicator)oldWidget;
        if (oldIndicator.Color == CurrentWidget.Color && oldIndicator.Axis == CurrentWidget.Axis)
        {
            return;
        }

        _leadingController.Color = CurrentWidget.Color;
        _leadingController.Axis = CurrentWidget.Axis;
        _trailingController.Color = CurrentWidget.Color;
        _trailingController.Axis = CurrentWidget.Axis;
    }

    public override Widget Build(BuildContext context)
    {
        return new NotificationListener<ScrollNotification>(
            onNotification: HandleScrollNotification,
            child: new RepaintBoundary(
                child: new CustomPaint(
                    foregroundPainter: new GlowingOverscrollIndicatorPainter(
                        CurrentWidget.ShowLeading ? _leadingController : null,
                        CurrentWidget.ShowTrailing ? _trailingController : null,
                        CurrentWidget.AxisDirection,
                        _controllers),
                    child: new RepaintBoundary(
                        child: CurrentWidget.Child))));
    }

    public override void Dispose()
    {
        _controllers.Dispose();
        _leadingController.Dispose();
        _trailingController.Dispose();
    }

    private bool HandleScrollNotification(ScrollNotification notification)
    {
        if (!CurrentWidget.NotificationPredicate(notification)
            || notification.Metrics.Axis != CurrentWidget.Axis)
        {
            return false;
        }

        _leadingController.PaintOffsetScrollPixels = -Math.Min(
            notification.Metrics.Pixels - notification.Metrics.MinScrollExtent,
            _leadingController.PaintOffset);
        _trailingController.PaintOffsetScrollPixels = -Math.Min(
            notification.Metrics.MaxScrollExtent - notification.Metrics.Pixels,
            _trailingController.PaintOffset);

        if (notification is OverscrollNotification overscrollNotification)
        {
            bool isLeading = overscrollNotification.Overscroll < 0.0;
            GlowController controller = isLeading
                ? _leadingController
                : _trailingController;
            if (_lastNotificationType != typeof(OverscrollNotification))
            {
                var confirmation = new OverscrollIndicatorNotification(isLeading);
                confirmation.Dispatch(Context);
                _accepted[isLeading] = confirmation.Accepted;
                if (confirmation.Accepted)
                {
                    controller.PaintOffset = confirmation.PaintOffset;
                }
            }

            if (_accepted[isLeading])
            {
                if (overscrollNotification.Velocity != 0.0)
                {
                    controller.AbsorbImpact(Math.Abs(overscrollNotification.Velocity));
                }
                else if (overscrollNotification.DragDetails is DragUpdateDetails dragDetails)
                {
                    ResolvePullGeometry(
                        overscrollNotification,
                        dragDetails,
                        out double extent,
                        out double crossAxisOffset,
                        out double crossExtent);
                    controller.Pull(
                        Math.Abs(overscrollNotification.Overscroll),
                        extent,
                        crossAxisOffset,
                        crossExtent);
                }
            }
        }
        else if (notification is ScrollEndNotification { DragDetails: not null }
                 || notification is ScrollUpdateNotification { DragDetails: not null })
        {
            _leadingController.ScrollEnd();
            _trailingController.ScrollEnd();
        }

        _lastNotificationType = notification.GetType();
        return false;
    }

    private static void ResolvePullGeometry(
        OverscrollNotification notification,
        DragUpdateDetails dragDetails,
        out double extent,
        out double crossAxisOffset,
        out double crossExtent)
    {
        Size size = notification.Context?.FindRenderObject() is RenderBox { HasSize: true } renderBox
            ? renderBox.Size
            : new Size(
                Math.Max(1.0, notification.Metrics.ViewportDimension),
                Math.Max(1.0, notification.Metrics.ViewportDimension));

        if (notification.Metrics.Axis == Axis.Horizontal)
        {
            extent = Math.Max(1.0, size.Width);
            crossExtent = Math.Max(1.0, size.Height);
            crossAxisOffset = Math.Clamp(dragDetails.LocalPosition.Y, 0.0, crossExtent);
        }
        else
        {
            extent = Math.Max(1.0, size.Height);
            crossExtent = Math.Max(1.0, size.Width);
            crossAxisOffset = Math.Clamp(dragDetails.LocalPosition.X, 0.0, crossExtent);
        }
    }
}

public sealed class StretchingOverscrollIndicator : StatefulWidget
{
    public StretchingOverscrollIndicator(
        AxisDirection axisDirection,
        Widget? child = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        AxisDirection = axisDirection;
        Child = child;
        NotificationPredicate =
            notificationPredicate ?? RawScrollbar.DefaultScrollNotificationPredicate;
        ClipBehavior = clipBehavior;
    }

    public AxisDirection AxisDirection { get; }

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

    public ScrollNotificationPredicate NotificationPredicate { get; }

    public Clip ClipBehavior { get; }

    public Widget? Child { get; }

    public override State CreateState() => new StretchingOverscrollIndicatorState();
}

internal sealed class StretchingOverscrollIndicatorState : State
{
    private StretchController _stretchController = null!;
    private ScrollNotification? _lastNotification;
    private OverscrollNotification? _lastOverscrollNotification;
    private double _totalOverscroll;
    private bool _accepted = true;

    private StretchingOverscrollIndicator CurrentWidget =>
        (StretchingOverscrollIndicator)StateWidget;

    internal StretchController StretchController => _stretchController;

    public override void InitState()
    {
        base.InitState();
        _stretchController = new StretchController(this);
    }

    public override Widget Build(BuildContext context)
    {
        return new NotificationListener<ScrollNotification>(
            onNotification: HandleScrollNotification,
            child: new AnimatedBuilder(
                animation: _stretchController,
                builder: BuildStretch,
                child: CurrentWidget.Child));
    }

    public override void Dispose()
    {
        _stretchController.Dispose();
    }

    private Widget BuildStretch(BuildContext context, Widget? child)
    {
        double stretch = _stretchController.Overscroll;
        double mainAxisSize = CurrentWidget.Axis == Axis.Horizontal
            ? MediaQuery.WidthOf(context)
            : MediaQuery.HeightOf(context);
        double viewportDimension =
            _lastOverscrollNotification?.Metrics.ViewportDimension ?? mainAxisSize;
        double overscroll = -stretch;
        if (CurrentWidget.AxisDirection is AxisDirection.Up or AxisDirection.Left)
        {
            overscroll = -overscroll;
        }

        Widget transform = new StretchEffect(
            stretchStrength: overscroll,
            axis: CurrentWidget.Axis,
            child: child ?? new SizedBox());
        return new ClipRect(
            clipBehavior: stretch != 0.0
                          && Math.Abs(viewportDimension - mainAxisSize) > 0.0001
                ? CurrentWidget.ClipBehavior
                : Clip.None,
            child: transform);
    }

    private bool HandleScrollNotification(ScrollNotification notification)
    {
        if (!CurrentWidget.NotificationPredicate(notification)
            || notification.Metrics.Axis != CurrentWidget.Axis)
        {
            return false;
        }

        if (notification is OverscrollNotification overscrollNotification)
        {
            _lastOverscrollNotification = overscrollNotification;
            if (_lastNotification is not OverscrollNotification)
            {
                var confirmation = new OverscrollIndicatorNotification(
                    leading: overscrollNotification.Overscroll < 0.0);
                confirmation.Dispatch(Context);
                _accepted = confirmation.Accepted;
            }

            if (_accepted)
            {
                _totalOverscroll += overscrollNotification.Overscroll;
                if (overscrollNotification.Velocity != 0.0)
                {
                    _stretchController.AbsorbImpact(overscrollNotification.Velocity);
                }
                else if (overscrollNotification.DragDetails != null)
                {
                    double viewportDimension = Math.Max(
                        1.0,
                        overscrollNotification.Metrics.ViewportDimension);
                    double distanceForPull = _totalOverscroll / viewportDimension;
                    _stretchController.Pull(Math.Clamp(distanceForPull, -1.0, 1.0));
                }
            }
        }
        else if (notification is ScrollEndNotification scrollEnd)
        {
            double velocity = CurrentWidget.Axis == Axis.Vertical
                ? scrollEnd.DragDetails?.Velocity.PixelsPerSecond.Y ?? 0.0
                : scrollEnd.DragDetails?.Velocity.PixelsPerSecond.X ?? 0.0;
            if (notification.Metrics.AxisDirection is AxisDirection.Left or AxisDirection.Up)
            {
                velocity = -velocity;
            }

            _totalOverscroll = 0.0;
            _stretchController.ScrollEnd(velocity);
        }
        else if (notification is ScrollUpdateNotification)
        {
            _totalOverscroll = 0.0;
            _stretchController.ScrollEnd(0.0);
        }

        _lastNotification = notification;
        return false;
    }
}

internal sealed class StretchController : ChangeNotifier
{
    private const double ExponentialScalar = Math.E / 0.33;
    private const double StretchIntensity = 0.016;
    private const double FlingVelocityFriction = 1.0 / 6000.0;
    private const double AbsorbImpactVelocityFriction = 1.0 / 3000.0;
    private const double MaxFlingVelocity = 0.5;
    private const double MaxAbsorbImpactVelocity = 1.25;
    private const double NaturalFrequency = 24.657;
    private const double DampingRatio = 0.98;
    private const double TimeCorrectionFactor = 0.8;
    private readonly Ticker _ticker;
    private double _initialOverscroll;
    private double _initialVelocity;
    private double _elapsedSeconds;
    private double _interruptedOverscroll;
    private double _overscroll;

    public StretchController(ITickerProvider? vsync = null)
    {
        _ticker = vsync?.CreateTicker(HandleTick) ?? new Ticker(HandleTick);
    }

    public double Overscroll
    {
        get => _overscroll;
        private set
        {
            double next = Math.Clamp(value, -1.0, 1.0);
            if (_overscroll == next)
            {
                return;
            }

            _overscroll = next;
            NotifyListeners();
        }
    }

    public void AbsorbImpact(double velocity)
    {
        if (velocity == 0.0)
        {
            return;
        }

        double scaledVelocity = Math.Clamp(
            velocity * AbsorbImpactVelocityFriction,
            -MaxAbsorbImpactVelocity,
            MaxAbsorbImpactVelocity);
        Animate(scaledVelocity);
    }

    public void ScrollEnd(double velocity)
    {
        if (velocity == 0.0 && Overscroll == 0.0)
        {
            return;
        }

        double scaledVelocity = Math.Clamp(
            -(velocity * FlingVelocityFriction),
            -MaxFlingVelocity,
            MaxFlingVelocity);
        if (!_ticker.IsActive)
        {
            Animate(scaledVelocity);
        }
    }

    public void Pull(double normalizedOverscroll)
    {
        if (_ticker.IsActive)
        {
            _interruptedOverscroll = Overscroll;
            _ticker.Stop();
        }
        else
        {
            _interruptedOverscroll = 0.0;
        }

        double absoluteDistance = Math.Abs(normalizedOverscroll);
        double linearIntensity = StretchIntensity * absoluteDistance;
        double exponentialIntensity =
            StretchIntensity * (1.0 - Math.Exp(-absoluteDistance * ExponentialScalar));
        double newOverscroll = Math.Sign(normalizedOverscroll)
                               * (linearIntensity + exponentialIntensity);
        Overscroll = newOverscroll + _interruptedOverscroll;
    }

    public override void Dispose()
    {
        _ticker.Dispose();
        base.Dispose();
    }

    private void Animate(double velocity)
    {
        _ticker.Stop();
        _initialOverscroll = Overscroll;
        _initialVelocity = velocity * TimeCorrectionFactor;
        _elapsedSeconds = 0.0;
        _ticker.Start();
    }

    private void HandleTick(TimeSpan elapsed)
    {
        _elapsedSeconds = elapsed.TotalSeconds;
        double angularFrequency = NaturalFrequency * TimeCorrectionFactor;
        double damping = DampingRatio * angularFrequency;
        double dampedFrequency =
            angularFrequency * Math.Sqrt(Math.Max(0.0, 1.0 - DampingRatio * DampingRatio));
        double exponential = Math.Exp(-damping * _elapsedSeconds);
        double cosine = Math.Cos(dampedFrequency * _elapsedSeconds);
        double sine = Math.Sin(dampedFrequency * _elapsedSeconds);
        double sineCoefficient = dampedFrequency <= 0.000001
            ? 0.0
            : (_initialVelocity + damping * _initialOverscroll) / dampedFrequency;
        double value = exponential * (_initialOverscroll * cosine + sineCoefficient * sine);
        Overscroll = value;
        if (_elapsedSeconds >= 2.0 || Math.Abs(value) <= 0.0001)
        {
            _ticker.Stop();
            _interruptedOverscroll = 0.0;
            Overscroll = 0.0;
        }
    }
}

internal enum GlowState
{
    Idle,
    Absorb,
    Pull,
    Recede,
}

internal sealed class GlowController : ChangeNotifier
{
    private const double MaxOpacity = 0.5;
    private const double PullOpacityGlowFactor = 0.8;
    private const double VelocityGlowFactor = 0.00006;
    private const double Sqrt3 = 1.73205080757;
    private const double WidthToHeightFactor = 0.75 * (2.0 - Sqrt3);
    private const double MinVelocity = 100.0;
    private const double MaxVelocity = 10000.0;
    private readonly Ticker _ticker;
    private Axis _axis;
    private Color _color;
    private double _displacement = 0.5;
    private double _displacementTarget = 0.5;
    private double _glowOpacity;
    private double _glowSize;
    private double _phaseDuration;
    private double _phaseElapsed;
    // The ticker reports time since it started, so the per-frame delta this controller's exponential
    // smoothing needs is derived from the previous elapsed value.
    private TimeSpan _lastElapsed;
    private double _phaseStartOpacity;
    private double _phaseStartSize;
    private double _phaseTargetOpacity;
    private double _phaseTargetSize;
    private double _pullDistance;
    private double _pullHoldDeadline;
    private GlowState _state;

    public GlowController(Color color, Axis axis, ITickerProvider? vsync = null)
    {
        _color = color;
        _axis = axis;
        _ticker = vsync?.CreateTicker(HandleTick) ?? new Ticker(HandleTick);
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            NotifyListeners();
        }
    }

    public Axis Axis
    {
        get => _axis;
        set
        {
            if (_axis == value)
            {
                return;
            }

            _axis = value;
            NotifyListeners();
        }
    }

    public double PaintOffset { get; set; }

    public double PaintOffsetScrollPixels { get; set; }

    internal GlowState State => _state;

    internal double GlowOpacity => _glowOpacity;

    internal double GlowSize => _glowSize;

    internal double Displacement => _displacement;

    public void AbsorbImpact(double velocity)
    {
        velocity = Math.Clamp(velocity, MinVelocity, MaxVelocity);
        _displacement = 0.5;
        _displacementTarget = 0.5;
        BeginPhase(
            GlowState.Absorb,
            _state == GlowState.Idle ? 0.3 : _glowOpacity,
            Math.Clamp(velocity * VelocityGlowFactor, _glowOpacity, MaxOpacity),
            _glowSize,
            Math.Min(0.025 + 7.5e-7 * velocity * velocity, 1.0),
            (0.15 + velocity * 0.02) / 1000.0);
    }

    public void Pull(
        double overscroll,
        double extent,
        double crossAxisOffset,
        double crossExtent)
    {
        _pullDistance += overscroll / 200.0;
        double safeExtent = Math.Max(1.0, extent);
        double safeCrossExtent = Math.Max(1.0, crossExtent);
        double targetOpacity = Math.Min(
            _glowOpacity + overscroll / safeExtent * PullOpacityGlowFactor,
            MaxOpacity);
        double height = Math.Min(safeExtent, safeCrossExtent * WidthToHeightFactor);
        double denominator = 0.7 * Math.Sqrt(Math.Max(0.0, _pullDistance * height));
        double targetSize = denominator <= 0.0
            ? _glowSize
            : Math.Max(1.0 - 1.0 / denominator, _glowSize);
        _displacementTarget = Math.Clamp(crossAxisOffset / safeCrossExtent, 0.0, 1.0);
        _pullHoldDeadline = Scheduler.CurrentSeconds + 0.167;
        BeginPhase(
            GlowState.Pull,
            _glowOpacity,
            targetOpacity,
            _glowSize,
            targetSize,
            0.167);
    }

    public void ScrollEnd()
    {
        if (_state == GlowState.Pull)
        {
            Recede(0.6);
        }
    }

    public void Paint(PaintingContext context, Size size)
    {
        if (_glowOpacity <= 0.0 || size.Width <= 0.0 || size.Height <= 0.0)
        {
            return;
        }

        double baseGlowScale = size.Width > size.Height
            ? size.Height / size.Width
            : 1.0;
        double radius = size.Width * 1.5;
        double height = Math.Min(size.Height, size.Width * WidthToHeightFactor);
        double scaleY = _glowSize * baseGlowScale;
        if (scaleY <= 0.0)
        {
            return;
        }

        var clipRect = new Rect(0.0, 0.0, size.Width, height);
        var center = new Point(
            size.Width / 2.0 * (0.5 + _displacement),
            height - radius);
        byte alpha = (byte)Math.Round(Math.Clamp(_glowOpacity, 0.0, 1.0) * byte.MaxValue);
        var brush = new SolidColorBrush(
            Avalonia.Media.Color.FromArgb(alpha, Color.R, Color.G, Color.B));
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(1.0, scaleY, 1.0, 1);
        transform.TranslateByDouble(0.0, PaintOffset + PaintOffsetScrollPixels, 0, 1);
        context.PushTransform(transform, transformed =>
        {
            transformed.PushClipRect(
                clipRect,
                clipped => clipped.DrawCircle(brush, null, center, radius));
        });
    }

    public override void Dispose()
    {
        _ticker.Dispose();
        base.Dispose();
    }

    private void BeginPhase(
        GlowState state,
        double startOpacity,
        double targetOpacity,
        double startSize,
        double targetSize,
        double duration)
    {
        _state = state;
        _phaseStartOpacity = startOpacity;
        _phaseTargetOpacity = targetOpacity;
        _phaseStartSize = startSize;
        _phaseTargetSize = targetSize;
        _phaseDuration = Math.Max(duration, 0.000001);
        _phaseElapsed = 0.0;
        if (!_ticker.IsActive)
        {
            _lastElapsed = TimeSpan.Zero;
            _ticker.Start();
        }
    }

    private void HandleTick(TimeSpan elapsed)
    {
        double seconds = (elapsed - _lastElapsed).TotalSeconds;
        _lastElapsed = elapsed;
        if (Math.Abs(_displacementTarget - _displacement) > 0.0001)
        {
            _displacement = _displacementTarget
                            - (_displacementTarget - _displacement)
                            * Math.Pow(2.0, -(seconds * 1_000_000.0) / (1_000_000.0 / 60.0));
        }

        _phaseElapsed += seconds;
        double t = Math.Clamp(_phaseElapsed / _phaseDuration, 0.0, 1.0);
        double curved = 1.0 - (1.0 - t) * (1.0 - t);
        _glowOpacity = Lerp(_phaseStartOpacity, _phaseTargetOpacity, curved);
        _glowSize = Lerp(_phaseStartSize, _phaseTargetSize, curved);
        NotifyListeners();

        if (t < 1.0)
        {
            return;
        }

        if (_state == GlowState.Absorb)
        {
            Recede(0.6);
        }
        else if (_state == GlowState.Recede)
        {
            _state = GlowState.Idle;
            _pullDistance = 0.0;
            _glowOpacity = 0.0;
            _glowSize = 0.0;
            _ticker.Stop();
            NotifyListeners();
        }
        else if (_state == GlowState.Pull && Scheduler.CurrentSeconds >= _pullHoldDeadline)
        {
            Recede(2.0);
        }
    }

    private void Recede(double duration)
    {
        if (_state is GlowState.Recede or GlowState.Idle)
        {
            return;
        }

        BeginPhase(
            GlowState.Recede,
            _glowOpacity,
            0.0,
            _glowSize,
            0.0,
            duration);
    }

    private static double Lerp(double begin, double end, double t)
    {
        return begin + (end - begin) * t;
    }
}

internal sealed class GlowingOverscrollIndicatorPainter : CustomPainter
{
    public GlowingOverscrollIndicatorPainter(
        GlowController? leadingController,
        GlowController? trailingController,
        AxisDirection axisDirection,
        IListenable repaint) : base(repaint)
    {
        LeadingController = leadingController;
        TrailingController = trailingController;
        AxisDirection = axisDirection;
    }

    internal GlowController? LeadingController { get; }

    internal GlowController? TrailingController { get; }

    internal AxisDirection AxisDirection { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        PaintSide(context, size, LeadingController, leading: true);
        PaintSide(context, size, TrailingController, leading: false);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not GlowingOverscrollIndicatorPainter oldPainter
               || !ReferenceEquals(oldPainter.LeadingController, LeadingController)
               || !ReferenceEquals(oldPainter.TrailingController, TrailingController);
    }

    private void PaintSide(
        PaintingContext context,
        Size size,
        GlowController? controller,
        bool leading)
    {
        if (controller == null)
        {
            return;
        }

        AxisDirection edge = ResolveEdge(AxisDirection, leading);
        switch (edge)
        {
            case AxisDirection.Up:
                controller.Paint(context, size);
                break;
            case AxisDirection.Down:
                context.PushTransform(
                    new Matrix4(
                        1.0, 0.0, 0.0, 0.0,
                        0.0, -1.0, 0.0, 0.0,
                        0.0, 0.0, 1.0, 0.0,
                        0.0, size.Height, 0.0, 1.0),
                    transformed => controller.Paint(transformed, size));
                break;
            case AxisDirection.Left:
                context.PushTransform(
                    new Matrix4(
                        0.0, 1.0, 0.0, 0.0,
                        1.0, 0.0, 0.0, 0.0,
                        0.0, 0.0, 1.0, 0.0,
                        0.0, 0.0, 0.0, 1.0),
                    transformed => controller.Paint(
                        transformed,
                        new Size(size.Height, size.Width)));
                break;
            case AxisDirection.Right:
                context.PushTransform(
                    new Matrix4(
                        0.0, 1.0, 0.0, 0.0,
                        -1.0, 0.0, 0.0, 0.0,
                        0.0, 0.0, 1.0, 0.0,
                        size.Width, 0.0, 0.0, 1.0),
                    transformed => controller.Paint(
                        transformed,
                        new Size(size.Height, size.Width)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static AxisDirection ResolveEdge(AxisDirection direction, bool leading)
    {
        return direction switch
        {
            AxisDirection.Down => leading ? AxisDirection.Up : AxisDirection.Down,
            AxisDirection.Up => leading ? AxisDirection.Down : AxisDirection.Up,
            AxisDirection.Right => leading ? AxisDirection.Left : AxisDirection.Right,
            AxisDirection.Left => leading ? AxisDirection.Right : AxisDirection.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}

internal sealed class MergedListenable : IListenable, IDisposable
{
    private readonly IReadOnlyList<IListenable> _children;
    private readonly List<Action> _listeners = [];

    public MergedListenable(params IListenable[] children)
    {
        _children = children;
        foreach (IListenable child in _children)
        {
            child.AddListener(NotifyListeners);
        }
    }

    public void AddListener(Action listener)
    {
        _listeners.Add(listener);
    }

    public void RemoveListener(Action listener)
    {
        _listeners.Remove(listener);
    }

    public void Dispose()
    {
        foreach (IListenable child in _children)
        {
            child.RemoveListener(NotifyListeners);
        }

        _listeners.Clear();
    }

    private void NotifyListeners()
    {
        foreach (Action listener in _listeners.ToArray())
        {
            listener();
        }
    }
}
