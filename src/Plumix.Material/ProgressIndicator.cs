using Avalonia;
using Avalonia.Media;
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
        double? minHeight = null,
        BorderRadius? borderRadius = null,
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

        Value = value;
        BackgroundColor = backgroundColor;
        Color = color;
        MinHeight = minHeight;
        BorderRadius = borderRadius;
        SemanticsLabel = semanticsLabel;
        SemanticsValue = semanticsValue;
    }

    public double? Value { get; }

    public Color? BackgroundColor { get; }

    public Color? Color { get; }

    public double? MinHeight { get; }

    public BorderRadius? BorderRadius { get; }

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
        private AnimationController? _animationController;
        private bool _isMounted;

        private LinearProgressIndicator CurrentWidget => (LinearProgressIndicator)StateWidget;

        public override void InitState()
        {
            _animationController = new AnimationController(IndeterminateDuration);
            _animationController.Changed += HandleAnimationTick;
            _isMounted = true;
            UpdateAnimationStatus();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            UpdateAnimationStatus();
        }

        public override void Dispose()
        {
            _isMounted = false;
            if (_animationController is not null)
            {
                _animationController.Changed -= HandleAnimationTick;
                _animationController.Dispose();
                _animationController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var progressTheme = ProgressIndicatorTheme.Of(context);

            var resolvedValueColor = CurrentWidget.Color
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
                                       ?? (theme.UseMaterial3
                                           ? Plumix.Rendering.BorderRadius.Circular(2.0)
                                           : Plumix.Rendering.BorderRadius.Zero);

            var resolvedValue = CurrentWidget.Value.HasValue
                ? ClampValue(CurrentWidget.Value.Value)
                : (double?)null;

            var animationValue = _animationController?.Evaluate() ?? 0.0;
            var textDirection = Directionality.Of(context);

            Widget child = new LinearProgressIndicatorRenderWidget(
                value: resolvedValue,
                animationValue: animationValue,
                trackColor: resolvedTrackColor,
                valueColor: resolvedValueColor,
                minHeight: resolvedMinHeight,
                borderRadius: resolvedBorderRadius,
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

        private void UpdateAnimationStatus()
        {
            if (_animationController is null)
            {
                return;
            }

            if (CurrentWidget.Value.HasValue)
            {
                if (_animationController.IsAnimating)
                {
                    _animationController.Stop();
                }

                return;
            }

            if (!_animationController.IsAnimating)
            {
                _animationController.Repeat();
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
        TextDirection textDirection,
        Key? key = null) : base(key)
    {
        Value = value;
        AnimationValue = animationValue;
        TrackColor = trackColor;
        ValueColor = valueColor;
        MinHeight = minHeight;
        BorderRadius = borderRadius;
        TextDirection = textDirection;
    }

    public double? Value { get; }

    public double AnimationValue { get; }

    public Color TrackColor { get; }

    public Color ValueColor { get; }

    public double MinHeight { get; }

    public BorderRadius BorderRadius { get; }

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
        indicator.TextDirection = TextDirection;
    }
}

internal sealed class RenderLinearProgressIndicator : RenderBox
{
    private const double IndeterminateDurationMilliseconds = 1800.0;

    private double? _value;
    private double _animationValue;
    private Color _trackColor;
    private Color _valueColor;
    private double _minHeight;
    private BorderRadius _borderRadius;
    private TextDirection _textDirection;

    public RenderLinearProgressIndicator(
        double? value,
        double animationValue,
        Color trackColor,
        Color valueColor,
        double minHeight,
        BorderRadius borderRadius,
        TextDirection textDirection)
    {
        _value = value;
        _animationValue = animationValue;
        _trackColor = trackColor;
        _valueColor = valueColor;
        _minHeight = minHeight;
        _borderRadius = borderRadius;
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

        ctx.DrawRectangle(
            trackBrush,
            null,
            new Rect(offset.X, offset.Y, Size.Width, Size.Height),
            radius,
            radius);

        if (Value.HasValue)
        {
            DrawBar(ctx, offset, valueBrush, 0, LinearProgressIndicator.ClampValue(Value.Value), radius);
            return;
        }

        var t = Math.Clamp(AnimationValue, 0.0, 1.0);
        var line1Head = TransformInterval(t, 0.0, 750.0 / IndeterminateDurationMilliseconds, 0.2, 0.0, 0.8, 1.0);
        var line1Tail = TransformInterval(t, 333.0 / IndeterminateDurationMilliseconds, (333.0 + 750.0) / IndeterminateDurationMilliseconds, 0.4, 0.0, 1.0, 1.0);
        var line2Head = TransformInterval(t, 1000.0 / IndeterminateDurationMilliseconds, (1000.0 + 567.0) / IndeterminateDurationMilliseconds, 0.0, 0.0, 0.65, 1.0);
        var line2Tail = TransformInterval(t, 1267.0 / IndeterminateDurationMilliseconds, (1267.0 + 533.0) / IndeterminateDurationMilliseconds, 0.10, 0.0, 0.45, 1.0);

        DrawBar(ctx, offset, valueBrush, line1Tail, line1Head, radius);
        DrawBar(ctx, offset, valueBrush, line2Tail, line2Head, radius);
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
