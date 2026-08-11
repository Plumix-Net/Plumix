using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/slider.dart

public sealed class CupertinoSlider : StatefulWidget
{
    public CupertinoSlider(
        double value,
        Action<double>? onChanged,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        Color? activeColor = null,
        Color? thumbColor = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(value) || !double.IsFinite(min) || !double.IsFinite(max))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Slider values must be finite.");
        }

        if (max < min || value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Slider value must be between min and max.");
        }

        if (divisions.HasValue && divisions.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisions), "Slider divisions must be greater than zero.");
        }

        Value = value;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        ActiveColor = activeColor;
        ThumbColor = thumbColor ?? Colors.White;
    }

    public double Value { get; }

    public Action<double>? OnChanged { get; }

    public Action<double>? OnChangeStart { get; }

    public Action<double>? OnChangeEnd { get; }

    public double Min { get; }

    public double Max { get; }

    public int? Divisions { get; }

    public Color? ActiveColor { get; }

    public Color ThumbColor { get; }

    public override State CreateState() => new CupertinoSliderState();

    private sealed class CupertinoSliderState : State
    {
        private static readonly Color DefaultActiveColor = Color.FromRgb(0, 122, 255);

        private CupertinoSlider CurrentWidget => (CupertinoSlider)StateWidget;

        public override Widget Build(BuildContext context)
        {
            PlatformBrightness brightness = MediaQuery.MaybeOf(context)?.PlatformBrightness
                                            ?? PlatformBrightness.Light;
            Color trackColor = brightness == PlatformBrightness.Dark
                ? Color.FromArgb(76, 255, 255, 255)
                : Color.FromArgb(51, 0, 0, 0);
            double range = CurrentWidget.Max - CurrentWidget.Min;
            double normalizedValue = range > 0.0
                ? (CurrentWidget.Value - CurrentWidget.Min) / range
                : 0.0;

            return new CupertinoSliderRenderWidget(
                value: normalizedValue,
                divisions: CurrentWidget.Divisions,
                activeColor: CurrentWidget.ActiveColor ?? DefaultActiveColor,
                thumbColor: CurrentWidget.ThumbColor,
                trackColor: trackColor,
                textDirection: Directionality.Of(context),
                onChanged: CurrentWidget.OnChanged is null ? null : HandleChanged,
                onChangeStart: CurrentWidget.OnChangeStart is null ? null : HandleChangeStart,
                onChangeEnd: CurrentWidget.OnChangeEnd is null ? null : HandleChangeEnd);
        }

        private void HandleChanged(double normalizedValue, bool isFastDrag)
        {
            double value = Denormalize(normalizedValue);
            if (Math.Abs(value - CurrentWidget.Value) <= 0.0001)
            {
                return;
            }

            if (Math.Abs(value - CurrentWidget.Min) <= 0.0001
                || Math.Abs(value - CurrentWidget.Max) <= 0.0001)
            {
                if (isFastDrag)
                {
                    HapticFeedback.MediumImpact();
                }
                else
                {
                    HapticFeedback.SelectionClick();
                }
            }

            CurrentWidget.OnChanged?.Invoke(value);
        }

        private void HandleChangeStart(double normalizedValue)
        {
            CurrentWidget.OnChangeStart?.Invoke(Denormalize(normalizedValue));
        }

        private void HandleChangeEnd(double normalizedValue)
        {
            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(normalizedValue));
        }

        private double Denormalize(double normalizedValue)
        {
            return CurrentWidget.Min + (normalizedValue * (CurrentWidget.Max - CurrentWidget.Min));
        }
    }
}

internal delegate void CupertinoSliderValueChanged(double value, bool isFastDrag);

internal sealed class CupertinoSliderRenderWidget : LeafRenderObjectWidget
{
    public CupertinoSliderRenderWidget(
        double value,
        int? divisions,
        Color activeColor,
        Color thumbColor,
        Color trackColor,
        TextDirection textDirection,
        CupertinoSliderValueChanged? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        Key? key = null) : base(key)
    {
        Value = value;
        Divisions = divisions;
        ActiveColor = activeColor;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TextDirection = textDirection;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
    }

    public double Value { get; }
    public int? Divisions { get; }
    public Color ActiveColor { get; }
    public Color ThumbColor { get; }
    public Color TrackColor { get; }
    public TextDirection TextDirection { get; }
    public CupertinoSliderValueChanged? OnChanged { get; }
    public Action<double>? OnChangeStart { get; }
    public Action<double>? OnChangeEnd { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoSlider(
            Value,
            Divisions,
            ActiveColor,
            ThumbColor,
            TrackColor,
            TextDirection,
            OnChanged,
            OnChangeStart,
            OnChangeEnd);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var slider = (RenderCupertinoSlider)renderObject;
        slider.Divisions = Divisions;
        slider.Value = Value;
        slider.ActiveColor = ActiveColor;
        slider.ThumbColor = ThumbColor;
        slider.TrackColor = TrackColor;
        slider.TextDirection = TextDirection;
        slider.OnChanged = OnChanged;
        slider.OnChangeStart = OnChangeStart;
        slider.OnChangeEnd = OnChangeEnd;
    }
}

internal sealed class RenderCupertinoSlider : RenderBox
{
    private const double Padding = 8.0;
    private const double ThumbRadius = 14.0;
    private const double PreferredWidth = 176.0;
    private const double PreferredHeight = 44.0;
    private const double Epsilon = 0.0001;

    private double _value;
    private int? _divisions;
    private Color _activeColor;
    private Color _thumbColor;
    private Color _trackColor;
    private TextDirection _textDirection;
    private CupertinoSliderValueChanged? _onChanged;
    private int? _activePointer;
    private double _dragValue;
    private DateTimeOffset? _lastUpdateTimestamp;

    public RenderCupertinoSlider(
        double value,
        int? divisions,
        Color activeColor,
        Color thumbColor,
        Color trackColor,
        TextDirection textDirection,
        CupertinoSliderValueChanged? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd)
    {
        _value = value;
        _divisions = divisions;
        _activeColor = activeColor;
        _thumbColor = thumbColor;
        _trackColor = trackColor;
        _textDirection = textDirection;
        _onChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
    }

    public double Value
    {
        get => _value;
        set
        {
            double next = ClampNormalized(value);
            if (Math.Abs(next - _value) <= Epsilon)
            {
                return;
            }

            _value = next;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public int? Divisions
    {
        get => _divisions;
        set
        {
            if (_divisions == value)
            {
                return;
            }

            _divisions = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Color ActiveColor
    {
        get => _activeColor;
        set
        {
            if (_activeColor == value)
            {
                return;
            }

            _activeColor = value;
            MarkNeedsPaint();
        }
    }

    public Color ThumbColor
    {
        get => _thumbColor;
        set
        {
            if (_thumbColor == value)
            {
                return;
            }

            _thumbColor = value;
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

    public CupertinoSliderValueChanged? OnChanged
    {
        get => _onChanged;
        set
        {
            bool wasInteractive = IsInteractive;
            _onChanged = value;
            if (wasInteractive != IsInteractive)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public Action<double>? OnChangeStart { get; set; }
    public Action<double>? OnChangeEnd { get; set; }

    private bool IsInteractive => OnChanged is not null;

    protected override bool HitTestSelf(Point position)
    {
        return IsInteractive && Math.Abs(position.X - ResolveThumbCenterX()) < ThumbRadius + Padding;
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(new Size(PreferredWidth, PreferredHeight));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            return;
        }

        double centerY = offset.Y + (Size.Height / 2.0);
        double trackLeft = offset.X + Padding;
        double trackRight = offset.X + Size.Width - Padding;
        double thumbCenterX = offset.X + ResolveThumbCenterX();
        Color leftColor = TextDirection == TextDirection.Ltr ? ActiveColor : TrackColor;
        Color rightColor = TextDirection == TextDirection.Ltr ? TrackColor : ActiveColor;

        if (thumbCenterX > trackLeft)
        {
            context.DrawRectangle(
                new SolidColorBrush(leftColor),
                null,
                new Rect(trackLeft, centerY - 1.0, thumbCenterX - trackLeft, 2.0),
                1.0,
                1.0);
        }

        if (thumbCenterX < trackRight)
        {
            context.DrawRectangle(
                new SolidColorBrush(rightColor),
                null,
                new Rect(thumbCenterX, centerY - 1.0, trackRight - thumbCenterX, 2.0),
                1.0,
                1.0);
        }

        context.DrawCircle(
            new SolidColorBrush(ThumbColor),
            null,
            new Point(thumbCenterX, centerY),
            ThumbRadius);
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        switch (@event)
        {
            case PointerDownEvent downEvent when IsInteractive && IsPrimaryButton(downEvent.Buttons):
                _activePointer = downEvent.Pointer;
                _dragValue = Value;
                _lastUpdateTimestamp = downEvent.TimestampUtc;
                OnChangeStart?.Invoke(Discretize(_dragValue));
                OnChanged?.Invoke(Discretize(_dragValue), false);
                break;
            case PointerMoveEvent moveEvent when _activePointer == moveEvent.Pointer:
                HandlePointerMove(moveEvent);
                break;
            case PointerUpEvent upEvent when _activePointer == upEvent.Pointer:
                EndInteraction();
                break;
            case PointerCancelEvent cancelEvent when _activePointer == cancelEvent.Pointer:
                EndInteraction();
                break;
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = IsInteractive;
        configuration.Flags |= SemanticsFlags.IsSlider;
        if (!IsInteractive)
        {
            return;
        }

        configuration.Flags |= SemanticsFlags.IsEnabled;
        double unit = Divisions.HasValue ? 1.0 / Divisions.Value : 0.1;
        configuration.Value = $"{Math.Round(Value * 100.0):0}%";
        configuration.IncreasedValue = $"{Math.Round(Math.Clamp(Value + unit, 0.0, 1.0) * 100.0):0}%";
        configuration.DecreasedValue = $"{Math.Round(Math.Clamp(Value - unit, 0.0, 1.0) * 100.0):0}%";
        configuration.AddActionHandler(
            SemanticsActions.Increase,
            () => OnChanged?.Invoke(Discretize(Math.Clamp(Value + unit, 0.0, 1.0)), false));
        configuration.AddActionHandler(
            SemanticsActions.Decrease,
            () => OnChanged?.Invoke(Discretize(Math.Clamp(Value - unit, 0.0, 1.0)), false));
    }

    private void HandlePointerMove(PointerMoveEvent @event)
    {
        double extent = Math.Max(Padding, Size.Width - (2.0 * (Padding + ThumbRadius)));
        double delta = @event.Delta.X / extent;
        _dragValue = ClampNormalized(_dragValue + (TextDirection == TextDirection.Rtl ? -delta : delta));
        bool isFastDrag = false;
        if (_lastUpdateTimestamp.HasValue)
        {
            double seconds = (@event.TimestampUtc - _lastUpdateTimestamp.Value).TotalSeconds;
            isFastDrag = seconds > 0.0 && Math.Abs(delta) / seconds > 1.0;
        }

        _lastUpdateTimestamp = @event.TimestampUtc;
        OnChanged?.Invoke(Discretize(_dragValue), isFastDrag);
    }

    private void EndInteraction()
    {
        OnChangeEnd?.Invoke(Discretize(_dragValue));
        _activePointer = null;
        _dragValue = 0.0;
        _lastUpdateTimestamp = null;
    }

    private double ResolveThumbCenterX()
    {
        double visualValue = TextDirection == TextDirection.Rtl ? 1.0 - Value : Value;
        double left = Padding + ThumbRadius;
        double right = Math.Max(left, Size.Width - Padding - ThumbRadius);
        return left + ((right - left) * visualValue);
    }

    private double Discretize(double value)
    {
        double clamped = ClampNormalized(value);
        return Divisions.HasValue
            ? Math.Round(clamped * Divisions.Value) / Divisions.Value
            : clamped;
    }

    private static bool IsPrimaryButton(PointerButtons buttons)
    {
        return buttons == PointerButtons.None || buttons.HasFlag(PointerButtons.Primary);
    }

    private static double ClampNormalized(double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 0.0;
    }
}
