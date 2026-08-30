using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using MouseCursor = Plumix.Widgets.MouseCursor;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/slider.dart

/// <summary>Dart's private <c>_SliderValueChanged</c>: the drag-aware form of <c>ValueChanged</c>.</summary>
internal delegate void CupertinoSliderValueChanged(double value, bool isFastDrag);

/// <summary>An iOS-style slider, used to select from a range of values.</summary>
public sealed class CupertinoSlider : StatefulWidget
{
    /// <summary>
    /// Defines the threshold for determining a "fast" slider drag, measured in slider extent per
    /// second. Estimated on a physical iPhone 15 Pro running iOS 18.
    /// </summary>
    internal const double VelocityThreshold = 1.0;

    /// <summary>Creates an iOS-style slider.</summary>
    public CupertinoSlider(
        double value,
        Action<double>? onChanged,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        CupertinoDynamicColor? activeColor = null,
        CupertinoDynamicColor? thumbColor = null,
        Key? key = null) : base(key)
    {
        if (!(value >= min && value <= max))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Slider value must be between min and max.");
        }

        if (divisions is not null and <= 0)
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
        ThumbColor = thumbColor ?? CupertinoColors.White;
    }

    /// <summary>The currently selected value for this slider.</summary>
    public double Value { get; }

    /// <summary>Called when the user selects a new value; null displays the slider as disabled.</summary>
    public Action<double>? OnChanged { get; }

    /// <summary>Called when the user starts selecting a new value for the slider.</summary>
    public Action<double>? OnChangeStart { get; }

    /// <summary>Called when the user is done selecting a new value for the slider.</summary>
    public Action<double>? OnChangeEnd { get; }

    /// <summary>The minimum value the user can select. Defaults to 0.0.</summary>
    public double Min { get; }

    /// <summary>The maximum value the user can select. Defaults to 1.0.</summary>
    public double Max { get; }

    /// <summary>The number of discrete divisions, or null for a continuous slider.</summary>
    public int? Divisions { get; }

    /// <summary>
    /// The color of the selected portion of the track. Defaults to the <see cref="CupertinoTheme"/>'s
    /// primary color.
    /// </summary>
    public CupertinoDynamicColor? ActiveColor { get; }

    /// <summary>The color of the thumb. Defaults to <see cref="CupertinoColors.White"/>.</summary>
    public CupertinoDynamicColor ThumbColor { get; }

    public override State CreateState() => new CupertinoSliderState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("value", Value));
        properties.Add(new DoubleProperty("min", Min));
        properties.Add(new DoubleProperty("max", Max));
    }

    private sealed class CupertinoSliderState : State
    {
        private CupertinoSlider CurrentWidget => (CupertinoSlider)StateWidget;

        public override Widget Build(BuildContext context)
        {
            return new CupertinoSliderRenderWidget(
                value: (CurrentWidget.Value - CurrentWidget.Min) / (CurrentWidget.Max - CurrentWidget.Min),
                divisions: CurrentWidget.Divisions,
                activeColor: CupertinoDynamicColor.Resolve(
                    CurrentWidget.ActiveColor ?? CupertinoTheme.Of(context).PrimaryColor,
                    context),
                thumbColor: CurrentWidget.ThumbColor,
                onChanged: CurrentWidget.OnChanged is null ? null : HandleChanged,
                onChangeStart: CurrentWidget.OnChangeStart is null ? null : HandleDragStart,
                onChangeEnd: CurrentWidget.OnChangeEnd is null ? null : HandleDragEnd,
                vsync: this);
        }

        private void HandleChanged(double value, bool isFastDrag)
        {
            double lerpValue = Lerp(CurrentWidget.Min, CurrentWidget.Max, value);
            bool isAtEdge = lerpValue == CurrentWidget.Max || lerpValue == CurrentWidget.Min;

            if (lerpValue != CurrentWidget.Value)
            {
                if (isAtEdge)
                {
                    EmitHapticFeedback(isFastDrag);
                }

                CurrentWidget.OnChanged!(lerpValue);
            }
        }

        private void HandleDragStart(double value)
        {
            CurrentWidget.OnChangeStart!(Lerp(CurrentWidget.Min, CurrentWidget.Max, value));
        }

        private void HandleDragEnd(double value)
        {
            CurrentWidget.OnChangeEnd!(Lerp(CurrentWidget.Min, CurrentWidget.Max, value));
        }

        private static void EmitHapticFeedback(bool isFastDrag)
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                    // The values are estimated using a physical iPhone 15 Pro running iOS 18.
                    if (isFastDrag)
                    {
                        _ = HapticFeedback.MediumImpact();
                    }
                    else
                    {
                        _ = HapticFeedback.SelectionClick();
                    }

                    break;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    break;
            }
        }

        private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
    }
}

internal sealed class CupertinoSliderRenderWidget : LeafRenderObjectWidget
{
    public CupertinoSliderRenderWidget(
        double value,
        int? divisions,
        Color activeColor,
        CupertinoDynamicColor thumbColor,
        CupertinoSliderValueChanged? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        ITickerProvider vsync,
        Key? key = null) : base(key)
    {
        Value = value;
        Divisions = divisions;
        ActiveColor = activeColor;
        ThumbColor = thumbColor;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Vsync = vsync;
    }

    public double Value { get; }
    public int? Divisions { get; }
    public Color ActiveColor { get; }
    public CupertinoDynamicColor ThumbColor { get; }
    public CupertinoSliderValueChanged? OnChanged { get; }
    public Action<double>? OnChangeStart { get; }
    public Action<double>? OnChangeEnd { get; }
    public ITickerProvider Vsync { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoSlider(
            value: Value,
            divisions: Divisions,
            activeColor: ActiveColor,
            thumbColor: CupertinoDynamicColor.Resolve(ThumbColor, context),
            trackColor: CupertinoDynamicColor.Resolve(CupertinoColors.SystemFill, context),
            onChanged: OnChanged,
            onChangeStart: OnChangeStart,
            onChangeEnd: OnChangeEnd,
            vsync: Vsync,
            textDirection: Directionality.Of(context),
            cursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var slider = (RenderCupertinoSlider)renderObject;
        // The assignment order matches Dart: `value` is applied while `divisions` still holds the
        // previous value, which is what decides between an animated and an immediate move.
        slider.Value = Value;
        slider.Divisions = Divisions;
        slider.ActiveColor = ActiveColor;
        slider.ThumbColor = CupertinoDynamicColor.Resolve(ThumbColor, context);
        slider.TrackColor = CupertinoDynamicColor.Resolve(CupertinoColors.SystemFill, context);
        slider.OnChanged = OnChanged;
        slider.OnChangeStart = OnChangeStart;
        slider.OnChangeEnd = OnChangeEnd;
        slider.TextDirection = Directionality.Of(context);
        // Ticker provider cannot change since there's a 1:1 relationship between the
        // CupertinoSliderRenderWidget object and the CupertinoSliderState object.
    }
}

internal sealed class RenderCupertinoSlider : RenderBox, IMouseTrackerAnnotation
{
    private const double Padding = 8.0;
    private const double SliderHeight = 2.0 * (CupertinoThumbPainter.Radius + Padding);
    private const double SliderWidth = 176.0; // Matches Material Design slider.
    private const double AdjustmentUnit = 0.1; // Matches iOS implementation of material slider.
    private static readonly TimeSpan DiscreteTransitionDuration = TimeSpan.FromMilliseconds(500.0);

    private static readonly BoxConstraints AdditionalConstraints =
        BoxConstraints.TightFor(width: SliderWidth, height: SliderHeight);

    private readonly AnimationController _position;
    private readonly HorizontalDragGestureRecognizer _drag;

    private double _value;
    private int? _divisions;
    private Color _activeColor;
    private Color _thumbColor;
    private Color _trackColor;
    private CupertinoSliderValueChanged? _onChanged;
    private TextDirection _textDirection;
    private MouseCursor _cursor;
    private double _currentDragValue;
    private DateTime? _lastUpdateTimestamp;

    public RenderCupertinoSlider(
        double value,
        int? divisions,
        Color activeColor,
        Color thumbColor,
        Color trackColor,
        CupertinoSliderValueChanged? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        ITickerProvider vsync,
        TextDirection textDirection,
        MouseCursor? cursor = null)
    {
        if (!(value >= 0.0 && value <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The normalized slider value must be in [0, 1].");
        }

        _value = value;
        _divisions = divisions;
        _activeColor = activeColor;
        _thumbColor = thumbColor;
        _trackColor = trackColor;
        _onChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        _textDirection = textDirection;
        _cursor = cursor ?? MouseCursor.Defer;
        _drag = new HorizontalDragGestureRecognizer
        {
            OnStart = HandleDragStart,
            OnUpdate = HandleDragUpdate,
            OnEnd = HandleDragEnd,
        };
        // The ticker this controller runs on belongs to the hosting state, which disposes it when
        // the state is disposed; Plumix's `RenderObject` has no `dispose` hook of its own.
        _position = new AnimationController(
            value: value,
            duration: DiscreteTransitionDuration,
            vsync: vsync);
        _position.AddListener(MarkNeedsPaint);
    }

    public double Value
    {
        get => _value;
        set
        {
            if (!(value >= 0.0 && value <= 1.0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The normalized slider value must be in [0, 1].");
            }

            if (value == _value)
            {
                return;
            }

            _value = value;
            if (Divisions is not null)
            {
                _position.AnimateTo(value, curve: Curves.FastOutSlowIn);
            }
            else
            {
                _position.SetValue(value);
            }

            MarkNeedsSemanticsUpdate();
        }
    }

    public int? Divisions
    {
        get => _divisions;
        set
        {
            if (value == _divisions)
            {
                return;
            }

            _divisions = value;
            MarkNeedsPaint();
        }
    }

    public Color ActiveColor
    {
        get => _activeColor;
        set
        {
            if (value == _activeColor)
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
            if (value == _thumbColor)
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
            if (value == _trackColor)
            {
                return;
            }

            _trackColor = value;
            MarkNeedsPaint();
        }
    }

    public CupertinoSliderValueChanged? OnChanged
    {
        get => _onChanged;
        set
        {
            if (value == _onChanged)
            {
                return;
            }

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

    /// <summary>The animated track split; only <see cref="Value"/> positions the thumb.</summary>
    internal double PositionValue => _position.Value;

    public MouseCursor Cursor
    {
        get => _cursor;
        set
        {
            if (_cursor == value)
            {
                return;
            }

            _cursor = value;
            // A repaint is needed in order to trigger a device update of the mouse tracker so that
            // this new value can be found.
            MarkNeedsPaint();
        }
    }

    public PointerEnterEventListener? OnEnter { get; set; }

    public PointerHoverEventListener? OnHover { get; set; }

    public PointerExitEventListener? OnExit { get; set; }

    public bool ValidForMouseTracker => false;

    public bool IsInteractive => OnChanged is not null;

    private double DiscretizedCurrentDragValue
    {
        get
        {
            double dragValue = Math.Clamp(_currentDragValue, 0.0, 1.0);
            if (Divisions is { } divisions)
            {
                dragValue = Math.Round(dragValue * divisions, MidpointRounding.AwayFromZero) / divisions;
            }

            return dragValue;
        }
    }

    private double TrackLeft => Padding;

    private double TrackRight => Size.Width - Padding;

    private double ThumbCenter
    {
        get
        {
            double visualPosition = TextDirection == TextDirection.Rtl ? 1.0 - _value : _value;
            return Lerp(
                TrackLeft + CupertinoThumbPainter.Radius,
                TrackRight - CupertinoThumbPainter.Radius,
                visualPosition);
        }
    }

    private double SemanticActionUnit => Divisions is { } divisions ? 1.0 / divisions : AdjustmentUnit;

    protected override bool HitTestSelf(Point position)
    {
        return Math.Abs(position.X - ThumbCenter) < CupertinoThumbPainter.Radius + Padding;
    }

    // Dart derives the render object from `RenderConstrainedBox`, whose childless layout is exactly
    // `additionalConstraints.enforce(constraints).constrain(Size.zero)`; Plumix's
    // `RenderConstrainedBox` is sealed, so the same formula is applied here directly.
    protected override void PerformLayout()
    {
        Size = AdditionalConstraints.Enforce(Constraints).Constrain(new Size());
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return AdditionalConstraints.Enforce(constraints).Smallest;
    }

    protected override double ComputeMinIntrinsicWidth(double height) => AdditionalConstraints.MinWidth;

    protected override double ComputeMaxIntrinsicWidth(double height) => AdditionalConstraints.MinWidth;

    protected override double ComputeMinIntrinsicHeight(double width) => AdditionalConstraints.MinHeight;

    protected override double ComputeMaxIntrinsicHeight(double width) => AdditionalConstraints.MinHeight;

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        DebugHandleEvent(@event, entry);
        if (@event is PointerDownEvent downEvent && IsInteractive)
        {
            _drag.AddPointer(downEvent);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        double visualPosition;
        Color leftColor;
        Color rightColor;
        if (TextDirection == TextDirection.Rtl)
        {
            visualPosition = 1.0 - _position.Value;
            leftColor = _activeColor;
            rightColor = TrackColor;
        }
        else
        {
            visualPosition = _position.Value;
            leftColor = TrackColor;
            rightColor = _activeColor;
        }

        double trackCenter = offset.Y + (Size.Height / 2.0);
        double trackLeft = offset.X + TrackLeft;
        double trackTop = trackCenter - 1.0;
        double trackBottom = trackCenter + 1.0;
        double trackRight = offset.X + TrackRight;
        double trackActive = offset.X + ThumbCenter;

        if (visualPosition > 0.0)
        {
            // Use RRect instead of RSuperellipse here since the radius is too small to make enough
            // visual difference.
            context.Canvas.DrawRRect(
                RRect.FromLTRBXY(trackLeft, trackTop, trackActive, trackBottom, 1.0, 1.0),
                new SolidColorBrush(rightColor),
                null);
        }

        if (visualPosition < 1.0)
        {
            context.Canvas.DrawRRect(
                RRect.FromLTRBXY(trackActive, trackTop, trackRight, trackBottom, 1.0, 1.0),
                new SolidColorBrush(leftColor),
                null);
        }

        var thumbCenter = new Point(trackActive, trackCenter);
        new CupertinoThumbPainter(color: ThumbColor).Paint(
            context,
            new Rect(
                thumbCenter.X - CupertinoThumbPainter.Radius,
                thumbCenter.Y - CupertinoThumbPainter.Radius,
                2.0 * CupertinoThumbPainter.Radius,
                2.0 * CupertinoThumbPainter.Radius));
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);

        configuration.IsSemanticBoundary = IsInteractive;
        configuration.Flags |= SemanticsFlags.IsSlider;
        if (!IsInteractive)
        {
            return;
        }

        configuration.TextDirection = TextDirection;
        configuration.AddActionHandler(SemanticsActions.Increase, IncreaseAction);
        configuration.AddActionHandler(SemanticsActions.Decrease, DecreaseAction);
        configuration.Value = Percent(Value);
        configuration.IncreasedValue = Percent(Math.Clamp(Value + SemanticActionUnit, 0.0, 1.0));
        configuration.DecreasedValue = Percent(Math.Clamp(Value - SemanticActionUnit, 0.0, 1.0));
    }

    private void HandleDragStart(DragStartDetails details) => StartInteraction(details);

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!IsInteractive)
        {
            return;
        }

        double extent = Math.Max(
            Padding,
            Size.Width - (2.0 * (Padding + CupertinoThumbPainter.Radius)));
        double valueDelta = (details.PrimaryDelta ?? 0.0) / extent;
        _currentDragValue += TextDirection == TextDirection.Rtl ? -valueDelta : valueDelta;

        // Default to false if no source timestamp is available.
        bool isFast = false;
        DateTime? currentTimestamp = details.SourceTimeStampUtc;
        if (currentTimestamp is { } current && _lastUpdateTimestamp is { } last)
        {
            double timeDelta = (long)(current - last).TotalMilliseconds;
            double velocity = Math.Abs(valueDelta) * 1000.0 / timeDelta;
            // Velocity is in units of slider extent per second. A value of 0.5 means the user is
            // dragging at 50% of the slider extent per second.
            isFast = velocity > CupertinoSlider.VelocityThreshold;
        }

        _lastUpdateTimestamp = currentTimestamp;
        OnChanged!(DiscretizedCurrentDragValue, isFast);
    }

    private void HandleDragEnd(DragEndDetails details) => EndInteraction();

    private void StartInteraction(DragStartDetails details)
    {
        if (!IsInteractive)
        {
            return;
        }

        // Dart reports the *previous* drag accumulator here, before it is seeded with the current
        // value; `EndInteraction` resets it to 0, so a second drag starts from 0 as well.
        OnChangeStart?.Invoke(DiscretizedCurrentDragValue);
        _currentDragValue = _value;
        _lastUpdateTimestamp = details.SourceTimeStampUtc;
        OnChanged!(DiscretizedCurrentDragValue, false);
    }

    private void EndInteraction()
    {
        OnChangeEnd?.Invoke(DiscretizedCurrentDragValue);
        _currentDragValue = 0.0;
        _lastUpdateTimestamp = null;
    }

    private void IncreaseAction()
    {
        if (IsInteractive)
        {
            OnChanged!(Math.Clamp(Value + SemanticActionUnit, 0.0, 1.0), false);
        }
    }

    private void DecreaseAction()
    {
        if (IsInteractive)
        {
            OnChanged!(Math.Clamp(Value - SemanticActionUnit, 0.0, 1.0), false);
        }
    }

    private static string Percent(double value)
    {
        return $"{Math.Round(value * 100.0, MidpointRounding.AwayFromZero)}%";
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
