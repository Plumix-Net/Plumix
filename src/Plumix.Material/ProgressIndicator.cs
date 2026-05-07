using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/progress_indicator.dart (linear baseline subset)

public sealed class LinearProgressIndicator : StatefulWidget
{
    private const double DefaultMinHeight = 4.0;
    private static readonly TimeSpan IndeterminateDuration = TimeSpan.FromMilliseconds(1800);

    public LinearProgressIndicator(
        double? value = null,
        Color? backgroundColor = null,
        Color? color = null,
        ValueNotifier<Color?>? valueColor = null,
        double? minHeight = null,
        BorderRadius? borderRadius = null,
        Color? stopIndicatorColor = null,
        double? stopIndicatorRadius = null,
        double? trackGap = null,
        bool? year2023 = null,
        AnimationController? controller = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        Key? key = null) : base(key)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "LinearProgressIndicator value must be finite when provided.");
        }

        if (minHeight.HasValue && (double.IsNaN(minHeight.Value) || double.IsInfinity(minHeight.Value) || minHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(minHeight), "LinearProgressIndicator minHeight must be finite and greater than zero.");
        }

        if (stopIndicatorRadius.HasValue && (double.IsNaN(stopIndicatorRadius.Value) || double.IsInfinity(stopIndicatorRadius.Value) || stopIndicatorRadius.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(stopIndicatorRadius), "LinearProgressIndicator stopIndicatorRadius must be finite and greater than or equal to zero.");
        }

        if (trackGap.HasValue && (double.IsNaN(trackGap.Value) || double.IsInfinity(trackGap.Value) || trackGap.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(trackGap), "LinearProgressIndicator trackGap must be finite and greater than or equal to zero.");
        }

        if (value.HasValue && controller is not null)
        {
            throw new ArgumentException("LinearProgressIndicator cannot set both value and controller.", nameof(controller));
        }

        Value = value;
        BackgroundColor = backgroundColor;
        Color = color;
        ValueColor = valueColor;
        MinHeight = minHeight;
        BorderRadius = borderRadius;
        StopIndicatorColor = stopIndicatorColor;
        StopIndicatorRadius = stopIndicatorRadius;
        TrackGap = trackGap;
        Year2023 = year2023;
        Controller = controller;
        SemanticsLabel = semanticsLabel;
        SemanticsValue = semanticsValue;
    }

    public double? Value { get; }

    public Color? BackgroundColor { get; }

    public Color? Color { get; }

    public ValueNotifier<Color?>? ValueColor { get; }

    public double? MinHeight { get; }

    public BorderRadius? BorderRadius { get; }

    public Color? StopIndicatorColor { get; }

    public double? StopIndicatorRadius { get; }

    public double? TrackGap { get; }

    public bool? Year2023 { get; }

    public AnimationController? Controller { get; }

    public string? SemanticsLabel { get; }

    public string? SemanticsValue { get; }

    public override State CreateState()
    {
        return new LinearProgressIndicatorState();
    }

    internal static double ClampValue(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }

    private sealed class LinearProgressIndicatorState : State
    {
        private AnimationController? _internalController;
        private AnimationController? _activeController;
        private ValueNotifier<Color?>? _activeValueColor;
        private bool _isMounted;

        private LinearProgressIndicator CurrentWidget => (LinearProgressIndicator)StateWidget;

        public override void InitState()
        {
            _internalController = new AnimationController(IndeterminateDuration);
            _isMounted = true;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
        }

        public override void Dispose()
        {
            _isMounted = false;
            if (_activeController is not null)
            {
                _activeController.Changed -= HandleAnimationTick;
                _activeController = null;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleValueColorChanged);
                _activeValueColor = null;
            }

            if (_internalController is not null)
            {
                _internalController.Dispose();
                _internalController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var progressTheme = ProgressIndicatorTheme.Of(context);
            var useYear2023 = ResolveYear2023(theme, progressTheme);
            var animationController = ResolveAnimationController(progressTheme);
            UpdateAnimationBinding(animationController);
            UpdateValueColorBinding(CurrentWidget.ValueColor);
            UpdateAnimationStatus(animationController);

            var resolvedValueColor = CurrentWidget.ValueColor?.Value
                                     ?? CurrentWidget.Color
                                     ?? progressTheme.Color
                                     ?? theme.PrimaryColor;

            var resolvedTrackColor = CurrentWidget.BackgroundColor
                                     ?? progressTheme.LinearTrackColor
                                     ?? (theme.UseMaterial3
                                         ? theme.SecondaryContainerColor
                                         : theme.CanvasColor);

            var resolvedMinHeight = CurrentWidget.MinHeight
                                    ?? progressTheme.LinearMinHeight
                                    ?? DefaultMinHeight;

            var resolvedBorderRadius = CurrentWidget.BorderRadius
                                       ?? progressTheme.BorderRadius
                                       ?? ResolveDefaultBorderRadius(theme, useYear2023);

            Color? resolvedStopIndicatorColor = theme.UseMaterial3 && !useYear2023
                ? CurrentWidget.StopIndicatorColor
                  ?? progressTheme.LinearStopIndicatorColor
                  ?? theme.PrimaryColor
                : null;

            double? resolvedStopIndicatorRadius = theme.UseMaterial3 && !useYear2023
                ? CurrentWidget.StopIndicatorRadius
                  ?? progressTheme.LinearStopIndicatorRadius
                  ?? 2.0
                : null;

            var resolvedTrackGap = theme.UseMaterial3 && !useYear2023
                ? CurrentWidget.TrackGap
                  ?? progressTheme.TrackGap
                  ?? 4.0
                : 0.0;

            var resolvedValue = CurrentWidget.Value.HasValue
                ? ClampValue(CurrentWidget.Value.Value)
                : (double?)null;

            var animationValue = animationController.Evaluate();
            var textDirection = Directionality.Of(context);

            Widget child = new LinearProgressIndicatorRenderWidget(
                value: resolvedValue,
                animationValue: animationValue,
                trackColor: resolvedTrackColor,
                valueColor: resolvedValueColor,
                minHeight: resolvedMinHeight,
                borderRadius: resolvedBorderRadius,
                stopIndicatorColor: resolvedStopIndicatorColor,
                stopIndicatorRadius: resolvedStopIndicatorRadius,
                trackGap: resolvedTrackGap,
                textDirection: textDirection);

            var semanticsLabel = ResolveSemanticsLabel(resolvedValue);
            if (!string.IsNullOrWhiteSpace(semanticsLabel))
            {
                child = new Semantics(
                    label: semanticsLabel,
                    child: child);
            }

            return child;
        }

        private AnimationController ResolveAnimationController(ProgressIndicatorThemeData progressTheme)
        {
            return CurrentWidget.Controller
                   ?? progressTheme.Controller
                   ?? _internalController
                   ?? throw new InvalidOperationException("LinearProgressIndicator internal controller is not initialized.");
        }

        private void UpdateAnimationBinding(AnimationController animationController)
        {
            if (ReferenceEquals(_activeController, animationController))
            {
                return;
            }

            if (_activeController is not null)
            {
                _activeController.Changed -= HandleAnimationTick;
            }

            _activeController = animationController;
            _activeController.Changed += HandleAnimationTick;
        }

        private void UpdateValueColorBinding(ValueNotifier<Color?>? valueColor)
        {
            if (ReferenceEquals(_activeValueColor, valueColor))
            {
                return;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleValueColorChanged);
            }

            _activeValueColor = valueColor;
            _activeValueColor?.AddListener(HandleValueColorChanged);
        }

        private string? ResolveSemanticsLabel(double? resolvedValue)
        {
            var label = CurrentWidget.SemanticsLabel;
            var value = CurrentWidget.SemanticsValue;

            if (string.IsNullOrWhiteSpace(value) && resolvedValue.HasValue)
            {
                value = $"{Math.Round(resolvedValue.Value * 100)}%";
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return value;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return label;
            }

            return $"{label} {value}";
        }

        private void UpdateAnimationStatus(AnimationController animationController)
        {
            if (_internalController is null)
            {
                return;
            }

            var shouldAnimateInternalController = !CurrentWidget.Value.HasValue
                                                  && ReferenceEquals(animationController, _internalController);

            if (!shouldAnimateInternalController)
            {
                if (_internalController.IsAnimating)
                {
                    _internalController.Stop();
                }

                return;
            }

            if (!_internalController.IsAnimating)
            {
                _internalController.Repeat();
            }
        }

        private void HandleAnimationTick()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() => { });
        }

        private void HandleValueColorChanged()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() => { });
        }

        private bool ResolveYear2023(ThemeData theme, ProgressIndicatorThemeData progressTheme)
        {
            if (!theme.UseMaterial3)
            {
                return true;
            }

            return CurrentWidget.Year2023
                   ?? progressTheme.Year2023
                   ?? true;
        }

        private static BorderRadius ResolveDefaultBorderRadius(ThemeData theme, bool useYear2023)
        {
            if (!theme.UseMaterial3 || useYear2023)
            {
                return Plumix.Rendering.BorderRadius.Zero;
            }

            return Plumix.Rendering.BorderRadius.Circular(2.0);
        }
    }
}

internal sealed class LinearProgressIndicatorRenderWidget : LeafRenderObjectWidget
{
    public LinearProgressIndicatorRenderWidget(
        double? value,
        double animationValue,
        Color trackColor,
        Color valueColor,
        double minHeight,
        BorderRadius borderRadius,
        Color? stopIndicatorColor,
        double? stopIndicatorRadius,
        double trackGap,
        TextDirection textDirection,
        Key? key = null) : base(key)
    {
        Value = value;
        AnimationValue = animationValue;
        TrackColor = trackColor;
        ValueColor = valueColor;
        MinHeight = minHeight;
        BorderRadius = borderRadius;
        StopIndicatorColor = stopIndicatorColor;
        StopIndicatorRadius = stopIndicatorRadius;
        TrackGap = trackGap;
        TextDirection = textDirection;
    }

    public double? Value { get; }

    public double AnimationValue { get; }

    public Color TrackColor { get; }

    public Color ValueColor { get; }

    public double MinHeight { get; }

    public BorderRadius BorderRadius { get; }

    public Color? StopIndicatorColor { get; }

    public double? StopIndicatorRadius { get; }

    public double TrackGap { get; }

    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderLinearProgressIndicator(
            value: Value,
            animationValue: AnimationValue,
            trackColor: TrackColor,
            valueColor: ValueColor,
            minHeight: MinHeight,
            borderRadius: BorderRadius,
            stopIndicatorColor: StopIndicatorColor,
            stopIndicatorRadius: StopIndicatorRadius,
            trackGap: TrackGap,
            textDirection: TextDirection);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var indicator = (RenderLinearProgressIndicator)renderObject;
        indicator.Value = Value;
        indicator.AnimationValue = AnimationValue;
        indicator.TrackColor = TrackColor;
        indicator.ValueColor = ValueColor;
        indicator.MinHeight = MinHeight;
        indicator.BorderRadius = BorderRadius;
        indicator.StopIndicatorColor = StopIndicatorColor;
        indicator.StopIndicatorRadius = StopIndicatorRadius;
        indicator.TrackGap = TrackGap;
        indicator.TextDirection = TextDirection;
    }
}

internal sealed class RenderLinearProgressIndicator : RenderBox
{
    private const double IndeterminateDurationMilliseconds = 1800.0;
    private const double TrackGapRampDownThreshold = 0.01;

    private double? _value;
    private double _animationValue;
    private Color _trackColor;
    private Color _valueColor;
    private double _minHeight;
    private BorderRadius _borderRadius;
    private Color? _stopIndicatorColor;
    private double? _stopIndicatorRadius;
    private double _trackGap;
    private TextDirection _textDirection;

    public RenderLinearProgressIndicator(
        double? value,
        double animationValue,
        Color trackColor,
        Color valueColor,
        double minHeight,
        BorderRadius borderRadius,
        Color? stopIndicatorColor,
        double? stopIndicatorRadius,
        double trackGap,
        TextDirection textDirection)
    {
        _value = value;
        _animationValue = animationValue;
        _trackColor = trackColor;
        _valueColor = valueColor;
        _minHeight = minHeight;
        _borderRadius = borderRadius;
        _stopIndicatorColor = stopIndicatorColor;
        _stopIndicatorRadius = stopIndicatorRadius;
        _trackGap = trackGap;
        _textDirection = textDirection;
    }

    public double? Value
    {
        get => _value;
        set
        {
            if (_value.HasValue == value.HasValue
                && (!_value.HasValue || Math.Abs(_value.Value - value!.Value) <= 0.0001))
            {
                return;
            }

            _value = value;
            MarkNeedsPaint();
        }
    }

    public double AnimationValue
    {
        get => _animationValue;
        set
        {
            if (Math.Abs(_animationValue - value) <= 0.0001)
            {
                return;
            }

            _animationValue = value;
            MarkNeedsPaint();
        }
    }

    public Color TrackColor
    {
        get => _trackColor;
        set
        {
            if (_trackColor == value)
            {
                return;
            }

            _trackColor = value;
            MarkNeedsPaint();
        }
    }

    public Color ValueColor
    {
        get => _valueColor;
        set
        {
            if (_valueColor == value)
            {
                return;
            }

            _valueColor = value;
            MarkNeedsPaint();
        }
    }

    public double MinHeight
    {
        get => _minHeight;
        set
        {
            if (Math.Abs(_minHeight - value) <= 0.0001)
            {
                return;
            }

            _minHeight = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public BorderRadius BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius.Equals(value))
            {
                return;
            }

            _borderRadius = value;
            MarkNeedsPaint();
        }
    }

    public Color? StopIndicatorColor
    {
        get => _stopIndicatorColor;
        set
        {
            if (_stopIndicatorColor == value)
            {
                return;
            }

            _stopIndicatorColor = value;
            MarkNeedsPaint();
        }
    }

    public double? StopIndicatorRadius
    {
        get => _stopIndicatorRadius;
        set
        {
            if (!_stopIndicatorRadius.HasValue && !value.HasValue)
            {
                return;
            }

            if (_stopIndicatorRadius.HasValue && value.HasValue && Math.Abs(_stopIndicatorRadius.Value - value.Value) <= 0.0001)
            {
                return;
            }

            _stopIndicatorRadius = value;
            MarkNeedsPaint();
        }
    }

    public double TrackGap
    {
        get => _trackGap;
        set
        {
            if (Math.Abs(_trackGap - value) <= 0.0001)
            {
                return;
            }

            _trackGap = value;
            MarkNeedsPaint();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        var resolvedMinHeight = Math.Max(0, MinHeight);
        var width = Constraints.HasBoundedWidth
            ? Constraints.MaxWidth
            : Constraints.ConstrainWidth(0);
        Size = Constraints.Constrain(new Size(width, resolvedMinHeight));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        var trackBrush = new SolidColorBrush(TrackColor);
        var valueBrush = new SolidColorBrush(ValueColor);
        var radius = ResolveRadius(Size.Width, Size.Height, BorderRadius.Radius);

        var effectiveTrackGap = Size.Width > 0
            ? Math.Clamp(TrackGap, 0.0, Size.Width)
            : 0.0;
        var trackGapFraction = Size.Width > 0
            ? effectiveTrackGap / Size.Width
            : 0.0;

        if (Value.HasValue)
        {
            var effectiveValue = LinearProgressIndicator.ClampValue(Value.Value);
            var trackStartFraction = trackGapFraction > 0
                ? effectiveValue + GetEffectiveTrackGapFraction(effectiveValue, trackGapFraction)
                : 0.0;

            if (trackStartFraction < 1.0)
            {
                DrawBar(ctx, offset, trackBrush, trackStartFraction, 1.0, radius);
            }

            DrawStopIndicator(ctx, offset);

            if (effectiveValue > 0)
            {
                DrawBar(ctx, offset, valueBrush, 0.0, effectiveValue, radius);
            }

            return;
        }

        var t = Math.Clamp(AnimationValue, 0.0, 1.0);
        var line1Head = TransformInterval(t, 0.0, 750.0 / IndeterminateDurationMilliseconds, 0.2, 0.0, 0.8, 1.0);
        var line1Tail = TransformInterval(t, 333.0 / IndeterminateDurationMilliseconds, (333.0 + 750.0) / IndeterminateDurationMilliseconds, 0.4, 0.0, 1.0, 1.0);
        var line2Head = TransformInterval(t, 1000.0 / IndeterminateDurationMilliseconds, (1000.0 + 567.0) / IndeterminateDurationMilliseconds, 0.0, 0.0, 0.65, 1.0);
        var line2Tail = TransformInterval(t, 1267.0 / IndeterminateDurationMilliseconds, (1267.0 + 533.0) / IndeterminateDurationMilliseconds, 0.10, 0.0, 0.45, 1.0);

        if (line1Head < 1 - trackGapFraction)
        {
            var trackStartFraction = line1Head > 0
                ? line1Head + GetEffectiveTrackGapFraction(line1Head, trackGapFraction)
                : 0.0;
            DrawBar(ctx, offset, trackBrush, trackStartFraction, 1.0, radius);
        }

        DrawBar(ctx, offset, valueBrush, line1Tail, line1Head, radius);

        if (line1Tail > trackGapFraction)
        {
            var trackStartFraction = line2Head > 0
                ? line2Head + GetEffectiveTrackGapFraction(line2Head, trackGapFraction)
                : 0.0;
            var trackEndFraction = line1Tail < 1
                ? line1Tail - GetEffectiveTrackGapFraction(1 - line1Tail, trackGapFraction)
                : 1.0;

            DrawBar(ctx, offset, trackBrush, trackStartFraction, trackEndFraction, radius);
        }

        DrawBar(ctx, offset, valueBrush, line2Tail, line2Head, radius);

        if (line2Tail > trackGapFraction)
        {
            var trackEndFraction = line2Tail < 1
                ? line2Tail - GetEffectiveTrackGapFraction(1 - line2Tail, trackGapFraction)
                : 1.0;
            DrawBar(ctx, offset, trackBrush, 0.0, trackEndFraction, radius);
        }
    }

    private void DrawBar(
        PaintingContext ctx,
        Point offset,
        IBrush brush,
        double startFraction,
        double endFraction,
        double radius)
    {
        startFraction = Math.Clamp(startFraction, 0.0, 1.0);
        endFraction = Math.Clamp(endFraction, 0.0, 1.0);
        var widthFraction = endFraction - startFraction;
        if (widthFraction <= 0)
        {
            return;
        }

        var width = Size.Width * widthFraction;
        var left = TextDirection == TextDirection.Rtl
            ? offset.X + (Size.Width - width - (Size.Width * startFraction))
            : offset.X + (Size.Width * startFraction);

        var barRect = new Rect(left, offset.Y, width, Size.Height);
        var barRadius = ResolveRadius(barRect.Width, barRect.Height, radius);
        ctx.DrawRectangle(
            brush,
            null,
            barRect,
            barRadius,
            barRadius);
    }

    private void DrawStopIndicator(PaintingContext ctx, Point offset)
    {
        if (!_stopIndicatorRadius.HasValue || _stopIndicatorRadius.Value <= 0 || _stopIndicatorColor is null)
        {
            return;
        }

        var maxRadius = Size.Height / 2.0;
        var radius = Math.Min(_stopIndicatorRadius.Value, maxRadius);
        if (radius <= 0)
        {
            return;
        }

        var centerX = TextDirection == TextDirection.Rtl
            ? offset.X + maxRadius
            : offset.X + (Size.Width - maxRadius);
        var center = new Point(centerX, offset.Y + maxRadius);
        ctx.DrawCircle(new SolidColorBrush(_stopIndicatorColor.Value), null, center, radius);
    }

    private static double GetEffectiveTrackGapFraction(double currentValue, double trackGapFraction)
    {
        if (trackGapFraction <= 0)
        {
            return 0;
        }

        return trackGapFraction * Math.Clamp(currentValue, 0.0, TrackGapRampDownThreshold) / TrackGapRampDownThreshold;
    }

    private static double TransformInterval(
        double value,
        double begin,
        double end,
        double x1,
        double y1,
        double x2,
        double y2)
    {
        var transformed = Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
        if (transformed <= 0 || transformed >= 1)
        {
            return transformed;
        }

        return TransformCubic(transformed, x1, y1, x2, y2);
    }

    private static double TransformCubic(double t, double x1, double y1, double x2, double y2)
    {
        // Solve x(s) = t for cubic-bezier control points, then evaluate y(s).
        var low = 0.0;
        var high = 1.0;
        for (var i = 0; i < 12; i++)
        {
            var mid = (low + high) * 0.5;
            var estimate = EvaluateCubic(mid, x1, x2);
            if (estimate < t)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var solved = (low + high) * 0.5;
        return EvaluateCubic(solved, y1, y2);
    }

    private static double EvaluateCubic(double t, double c1, double c2)
    {
        var mt = 1 - t;
        return (3 * c1 * mt * mt * t) + (3 * c2 * mt * t * t) + (t * t * t);
    }

    private static double ResolveRadius(double width, double height, double radius)
    {
        if (radius <= 0)
        {
            return 0;
        }

        return Math.Min(radius, Math.Min(width / 2.0, height / 2.0));
    }
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/progress_indicator.dart (circular baseline subset)

public sealed class CircularProgressIndicator : StatefulWidget
{
    private const double DefaultStrokeWidth = 4.0;
    private const double DefaultM2Size = 36.0;
    private const double DefaultM3Year2023Size = 36.0;
    private const double DefaultM3Size = 40.0;
    private static readonly BoxConstraints DefaultM2Constraints = new(MinWidth: DefaultM2Size, MinHeight: DefaultM2Size);
    private static readonly BoxConstraints DefaultM3Year2023Constraints = new(MinWidth: DefaultM3Year2023Size, MinHeight: DefaultM3Year2023Size);
    private static readonly BoxConstraints DefaultM3Constraints = new(MinWidth: DefaultM3Size, MinHeight: DefaultM3Size);
    private static readonly TimeSpan IndeterminateDuration = TimeSpan.FromMilliseconds(1333.0 * 2222.0);
    private readonly CircularProgressIndicatorType _indicatorType;

    private enum CircularProgressIndicatorType
    {
        Material,
        Adaptive
    }

    public CircularProgressIndicator(
        double? value = null,
        Color? backgroundColor = null,
        Color? color = null,
        ValueNotifier<Color?>? valueColor = null,
        double? strokeWidth = null,
        double? strokeAlign = null,
        BoxConstraints? constraints = null,
        double? size = null,
        StrokeCap? strokeCap = null,
        double? trackGap = null,
        bool? year2023 = null,
        AnimationController? controller = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        Key? key = null)
        : this(
            value: value,
            backgroundColor: backgroundColor,
            color: color,
            valueColor: valueColor,
            strokeWidth: strokeWidth,
            strokeAlign: strokeAlign,
            constraints: constraints,
            size: size,
            strokeCap: strokeCap,
            trackGap: trackGap,
            year2023: year2023,
            controller: controller,
            semanticsLabel: semanticsLabel,
            semanticsValue: semanticsValue,
            indicatorType: CircularProgressIndicatorType.Material,
            key: key)
    {
    }

    private CircularProgressIndicator(
        double? value,
        Color? backgroundColor,
        Color? color,
        ValueNotifier<Color?>? valueColor,
        double? strokeWidth,
        double? strokeAlign,
        BoxConstraints? constraints,
        double? size,
        StrokeCap? strokeCap,
        double? trackGap,
        bool? year2023,
        AnimationController? controller,
        string? semanticsLabel,
        string? semanticsValue,
        CircularProgressIndicatorType indicatorType,
        Key? key = null)
        : base(key)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "CircularProgressIndicator value must be finite when provided.");
        }

        if (strokeWidth.HasValue && (double.IsNaN(strokeWidth.Value) || double.IsInfinity(strokeWidth.Value) || strokeWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(strokeWidth), "CircularProgressIndicator strokeWidth must be finite and greater than zero.");
        }

        if (strokeAlign.HasValue && (double.IsNaN(strokeAlign.Value) || double.IsInfinity(strokeAlign.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(strokeAlign), "CircularProgressIndicator strokeAlign must be finite when provided.");
        }

        if (size.HasValue && (double.IsNaN(size.Value) || double.IsInfinity(size.Value) || size.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "CircularProgressIndicator size must be finite and greater than zero.");
        }

        if (trackGap.HasValue && (double.IsNaN(trackGap.Value) || double.IsInfinity(trackGap.Value) || trackGap.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(trackGap), "CircularProgressIndicator trackGap must be finite and greater than or equal to zero.");
        }

        if (value.HasValue && controller is not null)
        {
            throw new ArgumentException("CircularProgressIndicator cannot set both value and controller.", nameof(controller));
        }

        Value = value;
        BackgroundColor = backgroundColor;
        Color = color;
        ValueColor = valueColor;
        StrokeWidth = strokeWidth;
        StrokeAlign = strokeAlign;
        Constraints = constraints;
        Size = size;
        StrokeCap = strokeCap;
        TrackGap = trackGap;
        Year2023 = year2023;
        Controller = controller;
        SemanticsLabel = semanticsLabel;
        SemanticsValue = semanticsValue;
        _indicatorType = indicatorType;
    }

    public static CircularProgressIndicator Adaptive(
        double? value = null,
        Color? backgroundColor = null,
        Color? color = null,
        ValueNotifier<Color?>? valueColor = null,
        double? strokeWidth = null,
        double? strokeAlign = null,
        BoxConstraints? constraints = null,
        double? size = null,
        StrokeCap? strokeCap = null,
        double? trackGap = null,
        bool? year2023 = null,
        AnimationController? controller = null,
        string? semanticsLabel = null,
        string? semanticsValue = null,
        Key? key = null)
    {
        return new CircularProgressIndicator(
            value: value,
            backgroundColor: backgroundColor,
            color: color,
            valueColor: valueColor,
            strokeWidth: strokeWidth,
            strokeAlign: strokeAlign,
            constraints: constraints,
            size: size,
            strokeCap: strokeCap,
            trackGap: trackGap,
            year2023: year2023,
            controller: controller,
            semanticsLabel: semanticsLabel,
            semanticsValue: semanticsValue,
            indicatorType: CircularProgressIndicatorType.Adaptive,
            key: key);
    }

    public double? Value { get; }

    public Color? BackgroundColor { get; }

    public Color? Color { get; }

    public ValueNotifier<Color?>? ValueColor { get; }

    public double? StrokeWidth { get; }

    public double? StrokeAlign { get; }

    public BoxConstraints? Constraints { get; }

    public double? Size { get; }

    public StrokeCap? StrokeCap { get; }

    public double? TrackGap { get; }

    public bool? Year2023 { get; }

    public AnimationController? Controller { get; }

    public string? SemanticsLabel { get; }

    public string? SemanticsValue { get; }

    public override State CreateState()
    {
        return new CircularProgressIndicatorState();
    }

    internal static double ClampValue(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }

    private sealed class CircularProgressIndicatorState : State
    {
        private const int PathCount = 2222;
        private const int RotationCount = 1333;
        private const double ArcStart = -Math.PI / 2.0;
        private const double FullSweep = (Math.PI * 2.0) - 0.001;
        private const double MinIndeterminateSweep = 0.001;

        private AnimationController? _internalController;
        private AnimationController? _activeController;
        private ValueNotifier<Color?>? _activeValueColor;
        private bool _isMounted;

        private CircularProgressIndicator CurrentWidget => (CircularProgressIndicator)StateWidget;

        public override void InitState()
        {
            _internalController = new AnimationController(IndeterminateDuration);
            _isMounted = true;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
        }

        public override void Dispose()
        {
            _isMounted = false;
            if (_activeController is not null)
            {
                _activeController.Changed -= HandleAnimationTick;
                _activeController = null;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleValueColorChanged);
                _activeValueColor = null;
            }

            if (_internalController is not null)
            {
                _internalController.Dispose();
                _internalController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var progressTheme = ProgressIndicatorTheme.Of(context);
            var useYear2023 = ResolveYear2023(theme, progressTheme);
            var animationController = ResolveAnimationController(progressTheme);
            UpdateAnimationBinding(animationController);
            UpdateValueColorBinding(CurrentWidget.ValueColor);
            UpdateAnimationStatus(animationController);

            var resolvedValue = CurrentWidget.Value.HasValue
                ? ClampValue(CurrentWidget.Value.Value)
                : (double?)null;

            if (IsAdaptiveCupertino(theme))
            {
                return BuildAdaptiveCupertinoIndicator(theme, resolvedValue);
            }

            var resolvedValueColor = CurrentWidget.ValueColor?.Value
                                     ?? CurrentWidget.Color
                                     ?? progressTheme.Color
                                     ?? theme.PrimaryColor;

            var resolvedTrackColor = CurrentWidget.BackgroundColor
                                     ?? progressTheme.CircularTrackColor
                                     ?? ResolveDefaultTrackColor(theme, resolvedValue, useYear2023);

            var resolvedStrokeWidth = CurrentWidget.StrokeWidth
                                      ?? progressTheme.CircularStrokeWidth
                                      ?? DefaultStrokeWidth;

            var resolvedStrokeAlign = CurrentWidget.StrokeAlign
                                      ?? progressTheme.CircularStrokeAlign
                                      ?? ResolveDefaultStrokeAlign(theme, useYear2023);

            var resolvedConstraints = ResolveConstraints(CurrentWidget, progressTheme, theme, useYear2023, out var resolvedIndicatorSize);

            var resolvedTrackGap = theme.UseMaterial3 && !useYear2023
                ? CurrentWidget.TrackGap
                  ?? progressTheme.TrackGap
                  ?? 4.0
                : (double?)null;

            var resolvedStrokeCap = CurrentWidget.StrokeCap
                                    ?? progressTheme.CircularStrokeCap;

            var animationValue = animationController.Evaluate();
            var arcStart = ArcStart;
            var arcSweep = resolvedValue.HasValue
                ? ResolveDeterminateSweep(resolvedValue.Value)
                : ResolveIndeterminateSweep(animationValue, out arcStart);

            Widget child = new CircularProgressIndicatorRenderWidget(
                value: resolvedValue,
                arcStart: arcStart,
                arcSweep: arcSweep,
                trackColor: resolvedTrackColor,
                valueColor: resolvedValueColor,
                strokeWidth: resolvedStrokeWidth,
                strokeAlign: resolvedStrokeAlign,
                indicatorSize: resolvedIndicatorSize,
                strokeCap: resolvedStrokeCap,
                trackGap: resolvedTrackGap,
                year2023: useYear2023);
            child = new ConstrainedBox(
                constraints: resolvedConstraints,
                child: child);

            var semanticsLabel = ResolveSemanticsLabel(resolvedValue);
            if (!string.IsNullOrWhiteSpace(semanticsLabel))
            {
                child = new Semantics(
                    label: semanticsLabel,
                    child: child);
            }

            return child;
        }

        private Widget BuildAdaptiveCupertinoIndicator(ThemeData theme, double? resolvedValue)
        {
            var isDark = theme.Brightness == Brightness.Dark;
            var tickColor = CurrentWidget.BackgroundColor;
            if (resolvedValue.HasValue)
            {
                return CupertinoActivityIndicator.PartiallyRevealed(
                    color: tickColor,
                    progress: resolvedValue.Value,
                    isDark: isDark,
                    key: CurrentWidget.Key);
            }

            return new CupertinoActivityIndicator(
                color: tickColor,
                isDark: isDark,
                key: CurrentWidget.Key);
        }

        private AnimationController ResolveAnimationController(ProgressIndicatorThemeData progressTheme)
        {
            return CurrentWidget.Controller
                   ?? progressTheme.Controller
                   ?? _internalController
                   ?? throw new InvalidOperationException("CircularProgressIndicator internal controller is not initialized.");
        }

        private void UpdateAnimationBinding(AnimationController animationController)
        {
            if (ReferenceEquals(_activeController, animationController))
            {
                return;
            }

            if (_activeController is not null)
            {
                _activeController.Changed -= HandleAnimationTick;
            }

            _activeController = animationController;
            _activeController.Changed += HandleAnimationTick;
        }

        private void UpdateValueColorBinding(ValueNotifier<Color?>? valueColor)
        {
            if (ReferenceEquals(_activeValueColor, valueColor))
            {
                return;
            }

            if (_activeValueColor is not null)
            {
                _activeValueColor.RemoveListener(HandleValueColorChanged);
            }

            _activeValueColor = valueColor;
            _activeValueColor?.AddListener(HandleValueColorChanged);
        }

        private string? ResolveSemanticsLabel(double? resolvedValue)
        {
            var label = CurrentWidget.SemanticsLabel;
            var value = CurrentWidget.SemanticsValue;

            if (string.IsNullOrWhiteSpace(value) && resolvedValue.HasValue)
            {
                value = $"{Math.Round(resolvedValue.Value * 100)}%";
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return value;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return label;
            }

            return $"{label} {value}";
        }

        private void UpdateAnimationStatus(AnimationController animationController)
        {
            if (_internalController is null)
            {
                return;
            }

            var shouldAnimateInternalController = !CurrentWidget.Value.HasValue
                                                  && ReferenceEquals(animationController, _internalController);

            if (!shouldAnimateInternalController)
            {
                if (_internalController.IsAnimating)
                {
                    _internalController.Stop();
                }

                return;
            }

            if (!_internalController.IsAnimating)
            {
                _internalController.Repeat();
            }
        }

        private void HandleAnimationTick()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() => { });
        }

        private void HandleValueColorChanged()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() => { });
        }

        private bool ResolveYear2023(ThemeData theme, ProgressIndicatorThemeData progressTheme)
        {
            if (!theme.UseMaterial3)
            {
                return true;
            }

            return CurrentWidget.Year2023
                   ?? progressTheme.Year2023
                   ?? true;
        }

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            return CurrentWidget._indicatorType == CircularProgressIndicatorType.Adaptive
                   && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        }

        private static Color? ResolveDefaultTrackColor(ThemeData theme, double? resolvedValue, bool useYear2023)
        {
            if (!theme.UseMaterial3 || useYear2023)
            {
                return null;
            }

            return resolvedValue.HasValue
                ? theme.SecondaryContainerColor
                : null;
        }

        private static double ResolveDefaultStrokeAlign(ThemeData theme, bool useYear2023)
        {
            if (!theme.UseMaterial3 || useYear2023)
            {
                return 0.0;
            }

            return -1.0;
        }

        private static BoxConstraints ResolveConstraints(
            CircularProgressIndicator widget,
            ProgressIndicatorThemeData progressTheme,
            ThemeData theme,
            bool useYear2023,
            out double resolvedIndicatorSize)
        {
            if (widget.Constraints.HasValue)
            {
                resolvedIndicatorSize = 0.0;
                return widget.Constraints.Value;
            }

            if (widget.Size.HasValue)
            {
                resolvedIndicatorSize = widget.Size.Value;
                return new BoxConstraints(
                    MinWidth: widget.Size.Value,
                    MinHeight: widget.Size.Value);
            }

            if (progressTheme.CircularConstraints.HasValue)
            {
                resolvedIndicatorSize = 0.0;
                return progressTheme.CircularConstraints.Value;
            }

            if (progressTheme.CircularSize.HasValue)
            {
                resolvedIndicatorSize = progressTheme.CircularSize.Value;
                return new BoxConstraints(
                    MinWidth: progressTheme.CircularSize.Value,
                    MinHeight: progressTheme.CircularSize.Value);
            }

            resolvedIndicatorSize = 0.0;
            if (!theme.UseMaterial3)
            {
                return DefaultM2Constraints;
            }

            return useYear2023 ? DefaultM3Year2023Constraints : DefaultM3Constraints;
        }

        private static double ResolveDeterminateSweep(double value)
        {
            var clampedValue = ClampValue(value);
            if (clampedValue <= 0)
            {
                return 0;
            }

            return Math.Min(clampedValue * FullSweep, FullSweep);
        }

        private static double ResolveIndeterminateSweep(double animationValue, out double arcStart)
        {
            var t = Math.Clamp(animationValue, 0.0, 1.0);
            // Flutter parity: CurveTween(interval + fastOutSlowIn).chain(CurveTween(SawTooth(pathCount)))
            // means SawTooth is applied first, then interval/curve on the sawtooth output.
            var sawTooth = EvaluateSawTooth(t, PathCount);
            var headValue = TransformInterval(sawTooth, 0.0, 0.5, 0.4, 0.0, 0.2, 1.0);
            var tailValue = TransformInterval(sawTooth, 0.5, 1.0, 0.4, 0.0, 0.2, 1.0);
            var offsetValue = EvaluateSawTooth(t, PathCount);
            var rotationValue = EvaluateSawTooth(t, RotationCount);

            arcStart = ArcStart
                       + (tailValue * 1.5 * Math.PI)
                       + (rotationValue * Math.PI * 2.0)
                       + (offsetValue * 0.5 * Math.PI);

            var sweep = Math.Max((headValue * 1.5 * Math.PI) - (tailValue * 1.5 * Math.PI), MinIndeterminateSweep);
            return Math.Min(sweep, FullSweep);
        }

        private static double EvaluateSawTooth(double value, int count)
        {
            var transformed = Math.Clamp(value, 0.0, 1.0) * count;
            return transformed - Math.Floor(transformed);
        }

        private static double TransformInterval(
            double value,
            double begin,
            double end,
            double x1,
            double y1,
            double x2,
            double y2)
        {
            var transformed = Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
            if (transformed <= 0 || transformed >= 1)
            {
                return transformed;
            }

            return TransformCubic(transformed, x1, y1, x2, y2);
        }

        private static double TransformCubic(double t, double x1, double y1, double x2, double y2)
        {
            var low = 0.0;
            var high = 1.0;
            for (var i = 0; i < 12; i++)
            {
                var mid = (low + high) * 0.5;
                var estimate = EvaluateCubic(mid, x1, x2);
                if (estimate < t)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            var solved = (low + high) * 0.5;
            return EvaluateCubic(solved, y1, y2);
        }

        private static double EvaluateCubic(double t, double c1, double c2)
        {
            var mt = 1 - t;
            return (3 * c1 * mt * mt * t) + (3 * c2 * mt * t * t) + (t * t * t);
        }
    }
}

internal sealed class CircularProgressIndicatorRenderWidget : LeafRenderObjectWidget
{
    public CircularProgressIndicatorRenderWidget(
        double? value,
        double arcStart,
        double arcSweep,
        Color? trackColor,
        Color valueColor,
        double strokeWidth,
        double strokeAlign,
        double indicatorSize,
        StrokeCap? strokeCap,
        double? trackGap,
        bool year2023,
        Key? key = null) : base(key)
    {
        Value = value;
        ArcStart = arcStart;
        ArcSweep = arcSweep;
        TrackColor = trackColor;
        ValueColor = valueColor;
        StrokeWidth = strokeWidth;
        StrokeAlign = strokeAlign;
        IndicatorSize = indicatorSize;
        StrokeCap = strokeCap;
        TrackGap = trackGap;
        Year2023 = year2023;
    }

    public double? Value { get; }

    public double ArcStart { get; }

    public double ArcSweep { get; }

    public Color? TrackColor { get; }

    public Color ValueColor { get; }

    public double StrokeWidth { get; }

    public double StrokeAlign { get; }

    public double IndicatorSize { get; }

    public StrokeCap? StrokeCap { get; }

    public double? TrackGap { get; }

    public bool Year2023 { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCircularProgressIndicator(
            value: Value,
            arcStart: ArcStart,
            arcSweep: ArcSweep,
            trackColor: TrackColor,
            valueColor: ValueColor,
            strokeWidth: StrokeWidth,
            strokeAlign: StrokeAlign,
            indicatorSize: IndicatorSize,
            strokeCap: StrokeCap,
            trackGap: TrackGap,
            year2023: Year2023);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var indicator = (RenderCircularProgressIndicator)renderObject;
        indicator.Value = Value;
        indicator.ArcStart = ArcStart;
        indicator.ArcSweep = ArcSweep;
        indicator.TrackColor = TrackColor;
        indicator.ValueColor = ValueColor;
        indicator.StrokeWidth = StrokeWidth;
        indicator.StrokeAlign = StrokeAlign;
        indicator.IndicatorSize = IndicatorSize;
        indicator.StrokeCap = StrokeCap;
        indicator.TrackGap = TrackGap;
        indicator.Year2023 = Year2023;
    }
}

internal sealed class RenderCircularProgressIndicator : RenderBox
{
    private const double TwoPi = Math.PI * 2.0;
    private const double DeterminateStartAngle = -Math.PI / 2.0;
    private const double FullSweep = TwoPi - 0.001;
    private const double MinSweep = 0.001;

    private double? _value;
    private double _arcStart;
    private double _arcSweep;
    private Color? _trackColor;
    private Color _valueColor;
    private double _strokeWidth;
    private double _strokeAlign;
    private double _indicatorSize;
    private StrokeCap? _strokeCap;
    private double? _trackGap;
    private bool _year2023;

    public RenderCircularProgressIndicator(
        double? value,
        double arcStart,
        double arcSweep,
        Color? trackColor,
        Color valueColor,
        double strokeWidth,
        double strokeAlign,
        double indicatorSize,
        StrokeCap? strokeCap,
        double? trackGap,
        bool year2023)
    {
        _value = value;
        _arcStart = arcStart;
        _arcSweep = arcSweep;
        _trackColor = trackColor;
        _valueColor = valueColor;
        _strokeWidth = strokeWidth;
        _strokeAlign = strokeAlign;
        _indicatorSize = indicatorSize;
        _strokeCap = strokeCap;
        _trackGap = trackGap;
        _year2023 = year2023;
    }

    public double? Value
    {
        get => _value;
        set
        {
            if (_value.HasValue == value.HasValue
                && (!_value.HasValue || Math.Abs(_value.Value - value!.Value) <= 0.0001))
            {
                return;
            }

            _value = value;
            MarkNeedsPaint();
        }
    }

    public double ArcStart
    {
        get => _arcStart;
        set
        {
            if (Math.Abs(_arcStart - value) <= 0.0001)
            {
                return;
            }

            _arcStart = value;
            MarkNeedsPaint();
        }
    }

    public double ArcSweep
    {
        get => _arcSweep;
        set
        {
            if (Math.Abs(_arcSweep - value) <= 0.0001)
            {
                return;
            }

            _arcSweep = value;
            MarkNeedsPaint();
        }
    }

    public Color? TrackColor
    {
        get => _trackColor;
        set
        {
            if (_trackColor == value)
            {
                return;
            }

            _trackColor = value;
            MarkNeedsPaint();
        }
    }

    public Color ValueColor
    {
        get => _valueColor;
        set
        {
            if (_valueColor == value)
            {
                return;
            }

            _valueColor = value;
            MarkNeedsPaint();
        }
    }

    public double StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if (Math.Abs(_strokeWidth - value) <= 0.0001)
            {
                return;
            }

            _strokeWidth = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double StrokeAlign
    {
        get => _strokeAlign;
        set
        {
            if (Math.Abs(_strokeAlign - value) <= 0.0001)
            {
                return;
            }

            _strokeAlign = value;
            MarkNeedsPaint();
        }
    }

    public double IndicatorSize
    {
        get => _indicatorSize;
        set
        {
            if (Math.Abs(_indicatorSize - value) <= 0.0001)
            {
                return;
            }

            _indicatorSize = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public StrokeCap? StrokeCap
    {
        get => _strokeCap;
        set
        {
            if (_strokeCap == value)
            {
                return;
            }

            _strokeCap = value;
            MarkNeedsPaint();
        }
    }

    public double? TrackGap
    {
        get => _trackGap;
        set
        {
            if (!_trackGap.HasValue && !value.HasValue)
            {
                return;
            }

            if (_trackGap.HasValue && value.HasValue && Math.Abs(_trackGap.Value - value.Value) <= 0.0001)
            {
                return;
            }

            _trackGap = value;
            MarkNeedsPaint();
        }
    }

    public bool Year2023
    {
        get => _year2023;
        set
        {
            if (_year2023 == value)
            {
                return;
            }

            _year2023 = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        var side = Math.Max(0, IndicatorSize);
        Size = Constraints.Constrain(new Size(side, side));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        var diameter = Math.Min(Size.Width, Size.Height);
        if (diameter <= 0)
        {
            return;
        }

        var strokeWidth = Math.Min(Math.Max(0, StrokeWidth), diameter);
        if (strokeWidth <= 0)
        {
            return;
        }

        var strokeOffset = strokeWidth / 2.0 * -StrokeAlign;
        var arcDiameter = diameter - (strokeOffset * 2.0);
        if (arcDiameter <= 0 || double.IsNaN(arcDiameter) || double.IsInfinity(arcDiameter))
        {
            return;
        }

        var left = offset.X + ((Size.Width - diameter) / 2.0) + strokeOffset;
        var top = offset.Y + ((Size.Height - diameter) / 2.0) + strokeOffset;
        var arcRect = new Rect(left, top, arcDiameter, arcDiameter);
        var resolvedValue = Value.HasValue
            ? CircularProgressIndicator.ClampValue(Value.Value)
            : (double?)null;

        if (TrackColor.HasValue)
        {
            var trackPen = new Pen(
                new SolidColorBrush(TrackColor.Value),
                strokeWidth,
                lineCap: ResolveTrackLineCap(StrokeCap));
            if (TryResolveTrackGapArc(arcRect, strokeWidth, resolvedValue, out var gapArcStart, out var gapArcSweep))
            {
                ctx.DrawArc(trackPen, arcRect, startAngleRadians: gapArcStart, sweepAngleRadians: gapArcSweep);
            }
            else
            {
                ctx.DrawArc(trackPen, arcRect, startAngleRadians: 0, sweepAngleRadians: FullSweep);
            }
        }

        var sweep = resolvedValue.HasValue
            ? ResolveDeterminateSweep(resolvedValue.Value)
            : Math.Clamp(ArcSweep, MinSweep, FullSweep);
        if (sweep <= MinSweep)
        {
            return;
        }

        var start = Value.HasValue
            ? DeterminateStartAngle
            : ArcStart;
        var lineCap = ResolveIndicatorLineCap(resolvedValue, StrokeCap, Year2023);
        var indicatorPen = new Pen(new SolidColorBrush(ValueColor), strokeWidth, lineCap: lineCap);
        ctx.DrawArc(indicatorPen, arcRect, startAngleRadians: start, sweepAngleRadians: sweep);
    }

    private bool TryResolveTrackGapArc(
        Rect arcRect,
        double strokeWidth,
        double? resolvedValue,
        out double gapArcStart,
        out double gapArcSweep)
    {
        gapArcStart = 0.0;
        gapArcSweep = 0.0;

        if (!_trackGap.HasValue
            || double.IsNaN(_trackGap.Value)
            || double.IsInfinity(_trackGap.Value)
            || _trackGap.Value <= 0
            || !resolvedValue.HasValue
            || resolvedValue.Value <= MinSweep)
        {
            return false;
        }

        var arcRadius = Math.Min(arcRect.Width, arcRect.Height) / 2.0;
        if (arcRadius <= 0)
        {
            return false;
        }

        var clampedTrackGap = Math.Max(0, _trackGap.Value);
        var strokeRadius = strokeWidth / arcRadius;
        var gapRadius = clampedTrackGap / arcRadius;
        var startGap = strokeRadius + gapRadius;
        var endGap = startGap * 2.0;
        var startSweep = DeterminateStartAngle + startGap;
        var trackSweep = Math.Max(0.0, TwoPi - (resolvedValue.Value * TwoPi) - endGap);
        if (trackSweep <= MinSweep)
        {
            return false;
        }

        // Flutter parity draws the gapped background arc on a horizontally mirrored canvas.
        gapArcStart = Math.PI - startSweep;
        gapArcSweep = -trackSweep;
        return true;
    }

    private static PenLineCap ResolveTrackLineCap(StrokeCap? strokeCap)
    {
        return ToPenLineCap(strokeCap ?? Plumix.Material.StrokeCap.Round);
    }

    private static PenLineCap ResolveIndicatorLineCap(double? resolvedValue, StrokeCap? strokeCap, bool useYear2023)
    {
        if (strokeCap.HasValue)
        {
            return ToPenLineCap(strokeCap.Value);
        }

        if (!useYear2023)
        {
            return PenLineCap.Round;
        }

        // Flutter parity: indeterminate + null cap uses square; determinate + null cap uses butt.
        return resolvedValue.HasValue ? PenLineCap.Flat : PenLineCap.Square;
    }

    private static PenLineCap ToPenLineCap(StrokeCap strokeCap)
    {
        return strokeCap switch
        {
            Plumix.Material.StrokeCap.Butt => PenLineCap.Flat,
            Plumix.Material.StrokeCap.Round => PenLineCap.Round,
            Plumix.Material.StrokeCap.Square => PenLineCap.Square,
            _ => PenLineCap.Flat
        };
    }

    private static double ResolveDeterminateSweep(double value)
    {
        var clamped = CircularProgressIndicator.ClampValue(value);
        if (clamped <= 0)
        {
            return 0;
        }

        return Math.Min(clamped * FullSweep, FullSweep);
    }
}
