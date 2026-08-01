using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources (reference):
// flutter/packages/flutter/lib/src/material/progress_indicator.dart (RefreshProgressIndicator)
// flutter/packages/flutter/lib/src/material/refresh_indicator.dart (RefreshIndicator)

public sealed class RefreshProgressIndicator : CircularProgressIndicator
{
    public const double DefaultStrokeWidth = 2.5;
    internal const double IndicatorSize = 41.0;
    private static readonly TimeSpan IndeterminateDuration = TimeSpan.FromMilliseconds(1333.0 * 2222.0);

    public RefreshProgressIndicator(
        double? value = null,
        Color? backgroundColor = null,
        Color? color = null,
        IValueListenable<Color?>? valueColor = null,
        double strokeWidth = DefaultStrokeWidth,
        double? strokeAlign = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        StrokeCap? strokeCap = null,
        double elevation = 2.0,
        Thickness? indicatorMargin = null,
        Thickness? indicatorPadding = null,
        Key? key = null) : base(
        value: value,
        backgroundColor: backgroundColor,
        color: color,
        valueColor: valueColor,
        strokeWidth: strokeWidth,
        strokeAlign: strokeAlign,
        strokeCap: strokeCap,
        semanticsLabel: semanticsLabel,
        semanticsValue: semanticsValue,
        key: key)
    {
        if (!double.IsFinite(elevation) || elevation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        Elevation = elevation;
        IndicatorMargin = indicatorMargin ?? new Thickness(4);
        IndicatorPadding = indicatorPadding ?? new Thickness(12);
    }

    public double Elevation { get; }

    public Thickness IndicatorMargin { get; }

    public Thickness IndicatorPadding { get; }

    public override State CreateState() => new RefreshProgressIndicatorState();

    private sealed class RefreshProgressIndicatorState : State
    {
        private const double FullSweep = (Math.PI * 2.0) - 0.001;
        private AnimationController? _controller;
        private IValueListenable<Color?>? _activeValueColor;
        private bool _mounted;
        private double? _lastValue;

        private RefreshProgressIndicator CurrentWidget => (RefreshProgressIndicator)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(IndeterminateDuration, this);
            _controller.Changed += HandleChanged;
            _mounted = true;
        }

        public override void Dispose()
        {
            _mounted = false;
            if (_controller is not null)
            {
                _controller.Changed -= HandleChanged;
                _controller.Dispose();
                _controller = null;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleChanged);
                _activeValueColor = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            BindValueColor(widget.ValueColor);

            var theme = Theme.Of(context);
            var indicatorTheme = ProgressIndicatorTheme.Of(context);
            double? value = widget.Value.HasValue
                ? Math.Clamp(widget.Value.Value, 0.0, 1.0)
                : (double?)null;

            if (value.HasValue)
            {
                _lastValue = value;
                _controller?.Stop();
            }
            else if (_controller is { IsAnimating: false })
            {
                _controller.Repeat();
            }

            double animationValue = _controller?.Evaluate() ?? 0.0;
            double arcStart = -Math.PI / 2.0;
            double arcSweep;
            double arrowheadScale;

            if (value.HasValue)
            {
                double converted = TransformInterval(value.Value, 0.1, 0.33);
                arrowheadScale = converted;
                double headValue = 1.05 * Curves.FastOutSlowIn(converted);
                double offsetValue = converted * 0.5;
                double rotationValue = converted * 0.3;
                arcSweep = Math.Max(0.001, Math.Min(FullSweep, headValue * Math.PI * 1.5));
                arcStart += (rotationValue * Math.PI * 2.0)
                            + (offsetValue * Math.PI * 0.5)
                            + (Math.PI * ResolveAdditionalRotation(value.Value));
            }
            else
            {
                arrowheadScale = 0.0;
                ResolveIndeterminateArc(animationValue, out arcStart, out arcSweep);
                if (_lastValue.HasValue)
                {
                    arcStart += Math.PI * ResolveAdditionalRotation(_lastValue.Value);
                }
            }

            var resolvedValueColor = widget.ValueColor?.Value
                                     ?? widget.Color
                                     ?? indicatorTheme.Color
                                     ?? theme.PrimaryColor;
            var resolvedBackground = widget.BackgroundColor
                                     ?? indicatorTheme.RefreshBackgroundColor
                                     ?? theme.CanvasColor;
            double resolvedStrokeAlign = widget.StrokeAlign
                                         ?? indicatorTheme.StrokeAlign
                                         ?? 0.0;
            var resolvedStrokeCap = widget.StrokeCap
                                    ?? indicatorTheme.StrokeCap;

            Widget child = new CircularProgressIndicatorRenderWidget(
                value: null,
                arcStart: arcStart,
                arcSweep: arcSweep,
                trackColor: null,
                valueColor: resolvedValueColor,
                strokeWidth: widget.StrokeWidth ?? DefaultStrokeWidth,
                strokeAlign: resolvedStrokeAlign,
                indicatorSize: 0,
                strokeCap: resolvedStrokeCap,
                trackGap: null,
                year2023: true,
                arrowheadScale: arrowheadScale);

            child = new Container(
                width: IndicatorSize,
                height: IndicatorSize,
                padding: widget.IndicatorPadding,
                decoration: new BoxDecoration(
                    Color: resolvedBackground,
                    BoxShadows: BuildBoxShadows(theme.ShadowColor, widget.Elevation),
                    Shape: BoxShape.Circle),
                child: child);
            child = new Padding(widget.IndicatorMargin, child);

            string? semanticsLabel = ResolveSemanticsLabel(value);
            if (!string.IsNullOrWhiteSpace(semanticsLabel))
            {
                child = new Semantics(label: semanticsLabel, child: child);
            }

            return child;
        }

        private void BindValueColor(IValueListenable<Color?>? valueColor)
        {
            if (ReferenceEquals(_activeValueColor, valueColor))
            {
                return;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleChanged);
            }

            _activeValueColor = valueColor;
            _activeValueColor?.AddListener(HandleChanged);
        }

        private string? ResolveSemanticsLabel(double? value)
        {
            string? label = CurrentWidget.SemanticsLabel;
            string? semanticsValue = CurrentWidget.SemanticsValue;
            if (string.IsNullOrWhiteSpace(semanticsValue) && value.HasValue)
            {
                semanticsValue = $"{Math.Round(value.Value * 100)}%";
            }

            if (string.IsNullOrWhiteSpace(label)) return semanticsValue;
            if (string.IsNullOrWhiteSpace(semanticsValue)) return label;
            return $"{label} {semanticsValue}";
        }

        private void HandleChanged()
        {
            if (_mounted)
            {
                SetState(static () => { });
            }
        }

        private static double TransformInterval(double value, double begin, double end)
        {
            return Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
        }

        private static double ResolveAdditionalRotation(double value)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            if (value <= 0.33)
            {
                return -0.1 + ((-0.2 + 0.1) * (value / 0.33));
            }

            return -0.2 + ((1.35 + 0.2) * ((value - 0.33) / 0.67));
        }

        private static void ResolveIndeterminateArc(double value, out double start, out double sweep)
        {
            double t = Math.Clamp(value, 0.0, 1.0);
            double pathValue = SawTooth(t, 2222);
            double head = Curves.FastOutSlowIn(Math.Clamp(pathValue / 0.5, 0.0, 1.0));
            double tail = Curves.FastOutSlowIn(Math.Clamp((pathValue - 0.5) / 0.5, 0.0, 1.0));
            double rotationValue = SawTooth(t, 1333);
            start = -Math.PI / 2.0
                    + (tail * Math.PI * 1.5)
                    + (rotationValue * Math.PI * 2.0)
                    + (pathValue * Math.PI * 0.5);
            sweep = Math.Clamp((head - tail) * Math.PI * 1.5, 0.001, FullSweep);
        }

        private static double SawTooth(double value, int count)
        {
            double transformed = Math.Clamp(value, 0.0, 1.0) * count;
            return transformed - Math.Floor(transformed);
        }

        private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
        {
            if (elevation <= 0 || shadowColor.A == 0) return null;

            static Color WithOpacity(Color color, double opacity) => Avalonia.Media.Color.FromArgb(
                (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255),
                color.R,
                color.G,
                color.B);

            var keyShadow = new BoxShadow
            {
                OffsetY = Math.Max(1, Math.Round(elevation)),
                Blur = Math.Max(2, elevation * 2.4),
                Color = WithOpacity(shadowColor, 0.20)
            };
            var ambientShadow = new BoxShadow
            {
                OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
                Blur = Math.Max(3, elevation * 3.2),
                Color = WithOpacity(shadowColor, 0.14)
            };
            return new BoxShadows(keyShadow, [ambientShadow]);
        }
    }
}

public enum RefreshIndicatorStatus
{
    Drag,
    Armed,
    Snap,
    Refresh,
    Done,
    Canceled,
}

public enum RefreshIndicatorTriggerMode
{
    Anywhere,
    OnEdge,
}

internal enum RefreshIndicatorType
{
    Material,
    Adaptive,
    NoSpinner,
}

public sealed class RefreshIndicator : StatefulWidget
{
    public RefreshIndicator(
        Func<Task> onRefresh,
        Widget child,
        double displacement = 40.0,
        double edgeOffset = 0.0,
        Color? color = null,
        Color? backgroundColor = null,
        Func<ScrollNotification, bool>? notificationPredicate = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        double strokeWidth = RefreshProgressIndicator.DefaultStrokeWidth,
        RefreshIndicatorTriggerMode triggerMode = RefreshIndicatorTriggerMode.OnEdge,
        double elevation = 2.0,
        Key? key = null) : this(
        onRefresh,
        child,
        displacement,
        edgeOffset,
        color,
        backgroundColor,
        notificationPredicate,
        semanticsLabel,
        semanticsValue,
        strokeWidth,
        triggerMode,
        elevation,
        indicatorType: RefreshIndicatorType.Material,
        onStatusChange: null,
        key: key)
    {
    }

    private RefreshIndicator(
        Func<Task> onRefresh,
        Widget child,
        double displacement,
        double edgeOffset,
        Color? color,
        Color? backgroundColor,
        Func<ScrollNotification, bool>? notificationPredicate,
        string? semanticsLabel,
        string? semanticsValue,
        double strokeWidth,
        RefreshIndicatorTriggerMode triggerMode,
        double elevation,
        RefreshIndicatorType indicatorType,
        Action<RefreshIndicatorStatus?>? onStatusChange,
        Key? key) : base(key)
    {
        ArgumentNullException.ThrowIfNull(onRefresh);
        ArgumentNullException.ThrowIfNull(child);
        if (!double.IsFinite(elevation) || elevation < 0) throw new ArgumentOutOfRangeException(nameof(elevation));
        if (indicatorType != RefreshIndicatorType.NoSpinner && (!double.IsFinite(strokeWidth) || strokeWidth <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        }

        OnRefresh = onRefresh;
        Child = child;
        Displacement = displacement;
        EdgeOffset = edgeOffset;
        Color = color;
        BackgroundColor = backgroundColor;
        NotificationPredicate = notificationPredicate ?? DefaultScrollNotificationPredicate;
        SemanticsLabel = semanticsLabel;
        SemanticsValue = semanticsValue;
        StrokeWidth = strokeWidth;
        TriggerMode = triggerMode;
        Elevation = elevation;
        IndicatorType = indicatorType;
        OnStatusChange = onStatusChange;
    }

    public Widget Child { get; }
    public double Displacement { get; }
    public double EdgeOffset { get; }
    public Func<Task> OnRefresh { get; }
    public Action<RefreshIndicatorStatus?>? OnStatusChange { get; }
    public Color? Color { get; }
    public Color? BackgroundColor { get; }
    public Func<ScrollNotification, bool> NotificationPredicate { get; }
    public string? SemanticsLabel { get; }
    public string? SemanticsValue { get; }
    public double StrokeWidth { get; }
    public RefreshIndicatorTriggerMode TriggerMode { get; }
    public double Elevation { get; }
    internal RefreshIndicatorType IndicatorType { get; }

    public static RefreshIndicator Adaptive(
        Func<Task> onRefresh,
        Widget child,
        double displacement = 40.0,
        double edgeOffset = 0.0,
        Color? color = null,
        Color? backgroundColor = null,
        Func<ScrollNotification, bool>? notificationPredicate = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        double strokeWidth = RefreshProgressIndicator.DefaultStrokeWidth,
        RefreshIndicatorTriggerMode triggerMode = RefreshIndicatorTriggerMode.OnEdge,
        double elevation = 2.0,
        Key? key = null) => new(
        onRefresh,
        child,
        displacement,
        edgeOffset,
        color,
        backgroundColor,
        notificationPredicate,
        semanticsLabel,
        semanticsValue,
        strokeWidth,
        triggerMode,
        elevation,
        RefreshIndicatorType.Adaptive,
        onStatusChange: null,
        key);

    public static RefreshIndicator NoSpinner(
        Func<Task> onRefresh,
        Widget child,
        Action<RefreshIndicatorStatus?>? onStatusChange = null,
        Func<ScrollNotification, bool>? notificationPredicate = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        RefreshIndicatorTriggerMode triggerMode = RefreshIndicatorTriggerMode.OnEdge,
        double elevation = 2.0,
        Key? key = null) => new(
        onRefresh,
        child,
        displacement: 0,
        edgeOffset: 0,
        color: null,
        backgroundColor: null,
        notificationPredicate,
        semanticsLabel,
        semanticsValue,
        strokeWidth: 0,
        triggerMode,
        elevation,
        RefreshIndicatorType.NoSpinner,
        onStatusChange,
        key);

    public static bool DefaultScrollNotificationPredicate(ScrollNotification notification) => notification.Depth == 0;

    public override State CreateState() => new RefreshIndicatorState();
}

public sealed class RefreshIndicatorState : State
{
    private const double DragContainerExtentPercentage = 0.25;
    private const double DragSizeFactorLimit = 1.5;
    private static readonly TimeSpan SnapDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ScaleDuration = TimeSpan.FromMilliseconds(200);

    private AnimationController? _transitionController;
    private RefreshIndicatorStatus? _status;
    private bool? _isIndicatorAtTop;
    private double? _dragOffset;
    private double _positionValue;
    private double _scaleValue;
    private double _transitionFrom;
    private double _transitionTo;
    private TaskCompletionSource? _pendingRefresh;
    private bool _mounted;

    private RefreshIndicator CurrentWidget => (RefreshIndicator)StateWidget;

    public RefreshIndicatorStatus? Status => _status;

    public double PositionValue => _positionValue;

    public override void InitState()
    {
        _transitionController = new AnimationController(SnapDuration, this);
        _transitionController.Changed += HandleTransitionChanged;
        _transitionController.Completed += HandleTransitionCompleted;
        _mounted = true;
    }

    public override void Dispose()
    {
        _mounted = false;
        if (_transitionController is not null)
        {
            _transitionController.Changed -= HandleTransitionChanged;
            _transitionController.Completed -= HandleTransitionCompleted;
            _transitionController.Dispose();
            _transitionController = null;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var notificationChild = new NotificationListener<ScrollNotification>(
            onNotification: HandleScrollNotification,
            child: CurrentWidget.Child);
        var children = new List<Widget> { notificationChild };

        if (_status is not null && CurrentWidget.IndicatorType != RefreshIndicatorType.NoSpinner)
        {
            bool atTop = _isIndicatorAtTop ?? true;
            double revealFactor = Math.Clamp(_positionValue * DragSizeFactorLimit, 0.0, DragSizeFactorLimit);
            Widget indicator = BuildIndicator(context);
            if (_status == RefreshIndicatorStatus.Done)
            {
                double scale = Math.Clamp(1.0 - _scaleValue, 0.0, 1.0);
                const double center = 24.5;
                indicator = new Opacity(
                    scale,
                    new Plumix.Widgets.Transform(
                        Matrix.CreateTranslation(center, center)
                        * Matrix.CreateScale(scale, scale)
                        * Matrix.CreateTranslation(-center, -center),
                        indicator));
            }

            Widget transition = new Padding(
                atTop
                    ? new Thickness(0, CurrentWidget.Displacement, 0, 0)
                    : new Thickness(0, 0, 0, CurrentWidget.Displacement),
                new Align(
                    alignment: atTop ? Alignment.TopCenter : Alignment.BottomCenter,
                    child: indicator));
            transition = new ClipRect(
                child: new Align(
                    alignment: atTop ? Alignment.BottomCenter : Alignment.TopCenter,
                    heightFactor: revealFactor,
                    child: transition));

            children.Add(new Positioned(
                left: 0,
                right: 0,
                top: atTop ? CurrentWidget.EdgeOffset : null,
                bottom: atTop ? null : CurrentWidget.EdgeOffset,
                child: transition));
        }

        return new Stack(children: children);
    }

    public Task Show(bool atTop = true)
    {
        if (_status is RefreshIndicatorStatus.Refresh or RefreshIndicatorStatus.Snap)
        {
            return _pendingRefresh?.Task ?? Task.CompletedTask;
        }

        if (_status is null)
        {
            Start(atTop ? AxisDirection.Down : AxisDirection.Up);
        }

        BeginSnap();
        return _pendingRefresh?.Task ?? Task.CompletedTask;
    }

    private Widget BuildIndicator(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        bool showIndeterminate = _status is RefreshIndicatorStatus.Refresh or RefreshIndicatorStatus.Done;

        if (widget.IndicatorType == RefreshIndicatorType.Adaptive
            && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS)
        {
            return new CupertinoActivityIndicator(color: widget.Color, isDark: theme.Brightness == Brightness.Dark);
        }

        var color = widget.Color ?? theme.PrimaryColor;
        if (!showIndeterminate)
        {
            color = WithOpacity(color, Math.Clamp(_positionValue * DragSizeFactorLimit, 0.0, 1.0));
        }

        return new RefreshProgressIndicator(
            value: showIndeterminate ? null : Math.Clamp(_positionValue * 0.75, 0.0, 0.75),
            valueColor: new AlwaysStoppedAnimation<Color?>(color),
            backgroundColor: widget.BackgroundColor,
            strokeWidth: widget.StrokeWidth,
            elevation: widget.Elevation,
            semanticsLabel: widget.SemanticsLabel ?? MaterialLocalizations.Of(context).RefreshIndicatorSemanticLabel,
            semanticsValue: widget.SemanticsValue);
    }

    private bool HandleScrollNotification(ScrollNotification notification)
    {
        if (!CurrentWidget.NotificationPredicate(notification)) return false;

        if (ShouldStart(notification))
        {
            SetStatus(RefreshIndicatorStatus.Drag);
            return false;
        }

        bool? atTopNow = notification.Metrics.AxisDirection switch
        {
            AxisDirection.Down or AxisDirection.Up => true,
            _ => (bool?)null,
        };
        if (atTopNow != _isIndicatorAtTop)
        {
            if (_status is RefreshIndicatorStatus.Drag or RefreshIndicatorStatus.Armed)
            {
                BeginDismiss(RefreshIndicatorStatus.Canceled);
            }
            return false;
        }

        if (notification is ScrollUpdateNotification update)
        {
            if (_status is RefreshIndicatorStatus.Drag or RefreshIndicatorStatus.Armed)
            {
                double delta = update.ScrollDelta ?? 0.0;
                _dragOffset = notification.Metrics.AxisDirection == AxisDirection.Down
                    ? (_dragOffset ?? 0) - delta
                    : (_dragOffset ?? 0) + delta;
                CheckDragOffset(notification.Metrics.ViewportDimension);
            }

            if (_status == RefreshIndicatorStatus.Armed && !update.HasDragDetails)
            {
                BeginSnap();
            }
        }
        else if (notification is OverscrollNotification overscroll
                 && _status is RefreshIndicatorStatus.Drag or RefreshIndicatorStatus.Armed)
        {
            _dragOffset = notification.Metrics.AxisDirection == AxisDirection.Down
                ? (_dragOffset ?? 0) - overscroll.Overscroll
                : (_dragOffset ?? 0) + overscroll.Overscroll;
            CheckDragOffset(notification.Metrics.ViewportDimension);
        }
        else if (notification is ScrollEndNotification)
        {
            if (_status == RefreshIndicatorStatus.Armed && _positionValue >= 1.0)
            {
                BeginSnap();
            }
            else if (_status is RefreshIndicatorStatus.Armed or RefreshIndicatorStatus.Drag)
            {
                BeginDismiss(RefreshIndicatorStatus.Canceled);
            }
        }

        return false;
    }

    private bool ShouldStart(ScrollNotification notification)
    {
        bool isDragStart = notification is ScrollStartNotification { HasDragDetails: true };
        bool isAnywhereUpdate = notification is ScrollUpdateNotification { HasDragDetails: true }
                                && CurrentWidget.TriggerMode == RefreshIndicatorTriggerMode.Anywhere;
        if (!isDragStart && !isAnywhereUpdate) return false;

        var metrics = notification.Metrics;
        bool atSupportedEdge = (metrics.AxisDirection == AxisDirection.Down && metrics.ExtentBefore <= 0.0001)
                               || (metrics.AxisDirection == AxisDirection.Up && metrics.ExtentAfter <= 0.0001);
        return atSupportedEdge && _status is null && Start(metrics.AxisDirection);
    }

    private bool Start(AxisDirection direction)
    {
        if (direction is AxisDirection.Left or AxisDirection.Right) return false;
        _isIndicatorAtTop = true;
        _dragOffset = 0;
        _positionValue = 0;
        _scaleValue = 0;
        return true;
    }

    private void CheckDragOffset(double containerExtent)
    {
        if (containerExtent <= 0) return;
        double newValue = (_dragOffset ?? 0) / (containerExtent * DragContainerExtentPercentage);
        if (_status == RefreshIndicatorStatus.Armed)
        {
            newValue = Math.Max(newValue, 1.0 / DragSizeFactorLimit);
        }

        _positionValue = Math.Clamp(newValue, 0.0, 1.0);
        if (_status == RefreshIndicatorStatus.Drag && _positionValue >= 1.0 / DragSizeFactorLimit)
        {
            SetStatus(RefreshIndicatorStatus.Armed);
        }
        else
        {
            SetState(static () => { });
        }
    }

    private void BeginSnap()
    {
        _pendingRefresh ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetStatus(RefreshIndicatorStatus.Snap);
        BeginTransition(_positionValue, 1.0 / DragSizeFactorLimit, SnapDuration);
    }

    private void BeginDismiss(RefreshIndicatorStatus status)
    {
        SetStatus(status);
        if (status == RefreshIndicatorStatus.Done)
        {
            _scaleValue = 0;
            BeginTransition(0, 1, ScaleDuration);
        }
        else
        {
            BeginTransition(_positionValue, 0, ScaleDuration);
        }
    }

    private void BeginTransition(double from, double to, TimeSpan duration)
    {
        if (_transitionController is null) return;
        _transitionFrom = from;
        _transitionTo = to;
        _transitionController.Duration = duration;
        _transitionController.Forward(from: 0);
    }

    private void HandleTransitionChanged()
    {
        if (!_mounted || _transitionController is null) return;
        double value = _transitionFrom + ((_transitionTo - _transitionFrom) * _transitionController.Evaluate());
        if (_status == RefreshIndicatorStatus.Done)
        {
            _scaleValue = value;
        }
        else
        {
            _positionValue = value;
        }
        SetState(static () => { });
    }

    private void HandleTransitionCompleted()
    {
        if (!_mounted) return;
        if (_status == RefreshIndicatorStatus.Snap)
        {
            _positionValue = 1.0 / DragSizeFactorLimit;
            SetStatus(RefreshIndicatorStatus.Refresh);
            _ = RunRefreshAsync();
        }
        else if (_status is RefreshIndicatorStatus.Canceled or RefreshIndicatorStatus.Done)
        {
            ResetToIdle();
        }
    }

    private async Task RunRefreshAsync()
    {
        try
        {
            await CurrentWidget.OnRefresh().ConfigureAwait(false);
        }
        catch
        {
            // Flutter completes the show future and dismisses even when the refresh future fails.
        }

        _pendingRefresh?.TrySetResult();
        if (_mounted && _status == RefreshIndicatorStatus.Refresh)
        {
            BeginDismiss(RefreshIndicatorStatus.Done);
        }
    }

    private void ResetToIdle()
    {
        _status = null;
        _dragOffset = null;
        _isIndicatorAtTop = null;
        _positionValue = 0;
        _scaleValue = 0;
        _pendingRefresh = null;
        SetState(static () => { });
    }

    private void SetStatus(RefreshIndicatorStatus status)
    {
        _status = status;
        CurrentWidget.OnStatusChange?.Invoke(status);
        SetState(static () => { });
    }

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255),
        color.R,
        color.G,
        color.B);
}
