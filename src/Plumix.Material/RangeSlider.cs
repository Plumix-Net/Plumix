using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/range_slider.dart (approximate)

public readonly record struct RangeValues(double Start, double End);

public delegate string SemanticFormatterCallback(double value);

public sealed class RangeSlider : StatefulWidget
{
    public RangeSlider(
        RangeValues values,
        Action<RangeValues>? onChanged,
        Action<RangeValues>? onChangeStart = null,
        Action<RangeValues>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        Key? key = null) : base(key)
    {
        if (double.IsNaN(min) || double.IsInfinity(min))
        {
            throw new ArgumentOutOfRangeException(nameof(min), "RangeSlider min must be finite.");
        }

        if (double.IsNaN(max) || double.IsInfinity(max))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "RangeSlider max must be finite.");
        }

        if (max < min)
        {
            throw new ArgumentException("RangeSlider max must be greater than or equal to min.", nameof(max));
        }

        if (double.IsNaN(values.Start) || double.IsInfinity(values.Start))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "RangeSlider start value must be finite.");
        }

        if (double.IsNaN(values.End) || double.IsInfinity(values.End))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "RangeSlider end value must be finite.");
        }

        if (values.Start > values.End)
        {
            throw new ArgumentException("RangeSlider start value must be less than or equal to end value.", nameof(values));
        }

        if (values.Start < min || values.Start > max)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "RangeSlider start value must be between min and max.");
        }

        if (values.End < min || values.End > max)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "RangeSlider end value must be between min and max.");
        }

        if (divisions.HasValue && divisions.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisions), "RangeSlider divisions must be greater than zero.");
        }

        Values = values;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        OverlayColor = overlayColor;
        MaterialTapTargetSize = materialTapTargetSize;
        FocusNode = focusNode;
        Autofocus = autofocus;
        SemanticFormatterCallback = semanticFormatterCallback;
    }

    public RangeValues Values { get; }

    public Action<RangeValues>? OnChanged { get; }

    public Action<RangeValues>? OnChangeStart { get; }

    public Action<RangeValues>? OnChangeEnd { get; }

    public double Min { get; }

    public double Max { get; }

    public int? Divisions { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    public override State CreateState()
    {
        return new RangeSliderState();
    }

    private sealed class RangeSliderState : State
    {
        private const double DefaultTrackHeight = 4.0;
        private const double DefaultThumbRadius = 10.0;
        private const double PaddedTapTargetExtent = 48.0;
        private const double Epsilon = 0.0001;

        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _hasFocus;
        private RangeSliderThumb _keyboardThumb = RangeSliderThumb.End;

        private RangeSlider CurrentWidget => (RangeSlider)StateWidget;

        private bool IsInteractive => CurrentWidget.OnChanged is not null && CurrentWidget.Max > CurrentWidget.Min;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldRangeSlider = (RangeSlider)oldWidget;
            if (!ReferenceEquals(oldRangeSlider.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }
        }

        public override void Dispose()
        {
            DetachFocusNode(disposeOwned: true);
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var sliderTheme = SliderTheme.Of(context);
            double trackHeight = ResolveTrackHeight(sliderTheme);
            double thumbRadius = ResolveThumbRadius(sliderTheme);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? sliderTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            double minPreferredHeight = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? Math.Max(PaddedTapTargetExtent, thumbRadius * 2)
                : Math.Max(trackHeight, thumbRadius * 2);
            double overlayRadius = Math.Max(thumbRadius, theme.UseMaterial3 ? 20.0 : 16.0);

            var activeTrackColor = ResolveActiveTrackColor(theme, sliderTheme);
            var inactiveTrackColor = ResolveInactiveTrackColor(theme, sliderTheme);
            var thumbColor = ResolveThumbColor(theme, sliderTheme);
            var disabledActiveTrackColor = ResolveDisabledActiveTrackColor(theme, sliderTheme);
            var disabledInactiveTrackColor = ResolveDisabledInactiveTrackColor(theme, sliderTheme);
            var disabledThumbColor = ResolveDisabledThumbColor(theme, sliderTheme);

            var focusedStates = BuildStates(interactive: IsInteractive, focused: true);
            var hoveredStates = BuildStates(interactive: IsInteractive, hovered: true);
            var draggedStates = BuildStates(interactive: IsInteractive, dragged: true);
            var overlayFocusedColor = ResolveOverlayColor(theme, sliderTheme, focusedStates);
            var overlayHoveredColor = ResolveOverlayColor(theme, sliderTheme, hoveredStates);
            var overlayDraggedColor = ResolveOverlayColor(theme, sliderTheme, draggedStates);

            var semanticsFlags = SemanticsFlags.IsSlider;
            if (IsInteractive)
            {
                semanticsFlags |= SemanticsFlags.IsEnabled;
            }

            string? semanticsLabel = ResolveSemanticsLabel();
            var normalizedValues = Normalize(CurrentWidget.Values);

            return new Semantics(
                label: semanticsLabel,
                flags: semanticsFlags,
                child: new Focus(
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    canRequestFocus: IsInteractive,
                    onKeyEvent: HandleKeyEvent,
                    child: new RangeSliderRenderWidget(
                        startValueNormalized: normalizedValues.Start,
                        endValueNormalized: normalizedValues.End,
                        divisions: CurrentWidget.Divisions,
                        isInteractive: IsInteractive,
                        isFocused: _hasFocus,
                        trackHeight: trackHeight,
                        thumbRadius: thumbRadius,
                        overlayRadius: overlayRadius,
                        minPreferredHeight: minPreferredHeight,
                        activeTrackColor: IsInteractive ? activeTrackColor : disabledActiveTrackColor,
                        inactiveTrackColor: IsInteractive ? inactiveTrackColor : disabledInactiveTrackColor,
                        thumbColor: IsInteractive ? thumbColor : disabledThumbColor,
                        overlayFocusedColor: overlayFocusedColor,
                        overlayHoveredColor: overlayHoveredColor,
                        overlayDraggedColor: overlayDraggedColor,
                        textDirection: Directionality.Of(context),
                        onChangeStartNormalized: IsInteractive ? HandleChangeStartNormalized : null,
                        onChangedNormalized: IsInteractive ? HandleChangedNormalized : null,
                        onChangeEndNormalized: IsInteractive ? HandleChangeEndNormalized : null)));
        }

        private void AttachFocusNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _focusNode.AddListener(HandleFocusChanged);
            _hasFocus = _focusNode.HasFocus;
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode is null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusChanged);
            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
            _hasFocus = false;
        }

        private void HandleFocusChanged()
        {
            bool hasFocus = _focusNode?.HasFocus ?? false;
            if (hasFocus == _hasFocus)
            {
                return;
            }

            SetState(() => _hasFocus = hasFocus);
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsSupportedKeyboardKey(@event.Key))
            {
                return KeyEventResult.Ignored;
            }

            if (!IsInteractive || !@event.IsDown || HasModifier(@event))
            {
                return KeyEventResult.Handled;
            }

            var normalized = Normalize(CurrentWidget.Values);
            var next = ResolveKeyboardTargetNormalized(normalized, _keyboardThumb, @event.Key);
            if (AreEqual(normalized, next))
            {
                return KeyEventResult.Handled;
            }

            CurrentWidget.OnChangeStart?.Invoke(Denormalize(normalized));
            CurrentWidget.OnChanged?.Invoke(Denormalize(next));
            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(next));
            return KeyEventResult.Handled;
        }

        private NormalizedRangeValues ResolveKeyboardTargetNormalized(
            NormalizedRangeValues current,
            RangeSliderThumb thumb,
            string key)
        {
            if (string.Equals(key, "Home", StringComparison.Ordinal))
            {
                return thumb == RangeSliderThumb.Start
                    ? new NormalizedRangeValues(0.0, current.End)
                    : new NormalizedRangeValues(current.Start, current.Start);
            }

            if (string.Equals(key, "End", StringComparison.Ordinal))
            {
                return thumb == RangeSliderThumb.Start
                    ? new NormalizedRangeValues(current.End, current.End)
                    : new NormalizedRangeValues(current.Start, 1.0);
            }

            double step = ResolveAdjustmentUnit(Theme.Of(Context));
            var direction = Directionality.Of(Context);
            double delta = 0.0;
            if (string.Equals(key, "ArrowRight", StringComparison.Ordinal))
            {
                delta = direction == TextDirection.Rtl ? -step : step;
            }
            else if (string.Equals(key, "ArrowLeft", StringComparison.Ordinal))
            {
                delta = direction == TextDirection.Rtl ? step : -step;
            }
            else if (string.Equals(key, "ArrowUp", StringComparison.Ordinal)
                     || string.Equals(key, "PageUp", StringComparison.Ordinal))
            {
                delta = step;
            }
            else if (string.Equals(key, "ArrowDown", StringComparison.Ordinal)
                     || string.Equals(key, "PageDown", StringComparison.Ordinal))
            {
                delta = -step;
            }

            if (Math.Abs(delta) <= Epsilon)
            {
                return current;
            }

            if (thumb == RangeSliderThumb.Start)
            {
                double nextStart = SnapNormalized(Math.Clamp(current.Start + delta, 0.0, current.End));
                return new NormalizedRangeValues(nextStart, current.End);
            }

            double nextEnd = SnapNormalized(Math.Clamp(current.End + delta, current.Start, 1.0));
            return new NormalizedRangeValues(current.Start, nextEnd);
        }

        private static bool HasModifier(KeyEvent @event)
        {
            return @event.IsShiftPressed
                   || @event.IsControlPressed
                   || @event.IsAltPressed
                   || @event.IsMetaPressed;
        }

        private static bool IsSupportedKeyboardKey(string key)
        {
            return string.Equals(key, "ArrowLeft", StringComparison.Ordinal)
                   || string.Equals(key, "ArrowRight", StringComparison.Ordinal)
                   || string.Equals(key, "ArrowUp", StringComparison.Ordinal)
                   || string.Equals(key, "ArrowDown", StringComparison.Ordinal)
                   || string.Equals(key, "PageUp", StringComparison.Ordinal)
                   || string.Equals(key, "PageDown", StringComparison.Ordinal)
                   || string.Equals(key, "Home", StringComparison.Ordinal)
                   || string.Equals(key, "End", StringComparison.Ordinal);
        }

        private double ResolveAdjustmentUnit(ThemeData theme)
        {
            return theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? 0.1
                : 0.05;
        }

        private void HandleChangeStartNormalized(NormalizedRangeValues normalized, RangeSliderThumb thumb)
        {
            if (!IsInteractive)
            {
                return;
            }

            _keyboardThumb = thumb;
            CurrentWidget.OnChangeStart?.Invoke(Denormalize(SnapNormalized(normalized)));
        }

        private void HandleChangedNormalized(NormalizedRangeValues normalized, RangeSliderThumb thumb)
        {
            if (!IsInteractive)
            {
                return;
            }

            _keyboardThumb = thumb;
            var nextValues = Denormalize(SnapNormalized(normalized));
            if (Math.Abs(nextValues.Start - CurrentWidget.Values.Start) <= Epsilon
                && Math.Abs(nextValues.End - CurrentWidget.Values.End) <= Epsilon)
            {
                return;
            }

            CurrentWidget.OnChanged?.Invoke(nextValues);
        }

        private void HandleChangeEndNormalized(NormalizedRangeValues normalized, RangeSliderThumb thumb)
        {
            if (!IsInteractive)
            {
                return;
            }

            _keyboardThumb = thumb;
            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(SnapNormalized(normalized)));
        }

        private string? ResolveSemanticsLabel()
        {
            var formatter = CurrentWidget.SemanticFormatterCallback;
            if (formatter is null)
            {
                return null;
            }

            var values = CurrentWidget.Values;
            return $"{formatter(values.Start)} - {formatter(values.End)}";
        }

        private NormalizedRangeValues Normalize(RangeValues values)
        {
            double range = CurrentWidget.Max - CurrentWidget.Min;
            if (range <= 0)
            {
                return new NormalizedRangeValues(0.0, 0.0);
            }

            double start = Math.Clamp((values.Start - CurrentWidget.Min) / range, 0.0, 1.0);
            double end = Math.Clamp((values.End - CurrentWidget.Min) / range, 0.0, 1.0);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            return new NormalizedRangeValues(start, end);
        }

        private RangeValues Denormalize(NormalizedRangeValues normalized)
        {
            var snapped = SnapNormalized(normalized);
            double start = CurrentWidget.Min + ((CurrentWidget.Max - CurrentWidget.Min) * snapped.Start);
            double end = CurrentWidget.Min + ((CurrentWidget.Max - CurrentWidget.Min) * snapped.End);
            return new RangeValues(start, end);
        }

        private NormalizedRangeValues SnapNormalized(NormalizedRangeValues normalized)
        {
            double start = SnapNormalized(normalized.Start);
            double end = SnapNormalized(normalized.End);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            return new NormalizedRangeValues(start, end);
        }

        private double SnapNormalized(double normalized)
        {
            double clamped = Math.Clamp(normalized, 0.0, 1.0);
            if (!CurrentWidget.Divisions.HasValue || CurrentWidget.Divisions.Value <= 0)
            {
                return clamped;
            }

            int divisions = CurrentWidget.Divisions.Value;
            return Math.Clamp(Math.Round(clamped * divisions) / divisions, 0.0, 1.0);
        }

        private double ResolveTrackHeight(SliderThemeData sliderTheme)
        {
            double resolved = sliderTheme.TrackHeight ?? DefaultTrackHeight;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return DefaultTrackHeight;
            }

            return resolved;
        }

        private double ResolveThumbRadius(SliderThemeData sliderTheme)
        {
            double resolved = sliderTheme.ThumbRadius ?? DefaultThumbRadius;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return DefaultThumbRadius;
            }

            return resolved;
        }

        private Color ResolveActiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.ActiveColor
                   ?? sliderTheme.ActiveTrackColor
                   ?? theme.PrimaryColor;
        }

        private Color ResolveInactiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.InactiveColor
                   ?? sliderTheme.InactiveTrackColor
                   ?? (theme.UseMaterial3
                       ? theme.SurfaceContainerHighestColor
                       : MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.24));
        }

        private Color ResolveThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.ActiveColor
                   ?? sliderTheme.ThumbColor
                   ?? theme.PrimaryColor;
        }

        private Color ResolveDisabledActiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledActiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, theme.UseMaterial3 ? 0.38 : 0.32);
        }

        private Color ResolveDisabledInactiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledInactiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12);
        }

        private Color ResolveDisabledThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledThumbColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
        }

        private Color? ResolveOverlayColor(ThemeData theme, SliderThemeData sliderTheme, MaterialState states)
        {
            var widgetOverlay = CurrentWidget.OverlayColor?.Resolve(states);
            if (widgetOverlay.HasValue)
            {
                return widgetOverlay.Value;
            }

            var themeOverlay = sliderTheme.OverlayColor?.Resolve(states);
            if (themeOverlay.HasValue)
            {
                return themeOverlay.Value;
            }

            var baseColor = CurrentWidget.ActiveColor ?? theme.PrimaryColor;

            if (!theme.UseMaterial3)
            {
                return states.HasFlag(MaterialState.Disabled)
                    ? null
                    : MaterialButtonCore.ApplyOpacity(baseColor, 0.12);
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.10);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.10);
            }

            return null;
        }

        private static MaterialState BuildStates(
            bool interactive,
            bool focused = false,
            bool hovered = false,
            bool dragged = false)
        {
            var states = interactive ? MaterialState.None : MaterialState.Disabled;
            if (focused)
            {
                states |= MaterialState.Focused;
            }

            if (hovered)
            {
                states |= MaterialState.Hovered;
            }

            if (dragged)
            {
                states |= MaterialState.Pressed;
            }

            return states;
        }

        private static bool AreEqual(NormalizedRangeValues a, NormalizedRangeValues b)
        {
            return Math.Abs(a.Start - b.Start) <= Epsilon
                   && Math.Abs(a.End - b.End) <= Epsilon;
        }
    }
}

internal enum RangeSliderThumb
{
    Start,
    End,
}

internal readonly record struct NormalizedRangeValues(double Start, double End);

internal sealed class RangeSliderRenderWidget : LeafRenderObjectWidget
{
    public RangeSliderRenderWidget(
        double startValueNormalized,
        double endValueNormalized,
        int? divisions,
        bool isInteractive,
        bool isFocused,
        double trackHeight,
        double thumbRadius,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        TextDirection textDirection,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangeStartNormalized,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangedNormalized,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangeEndNormalized,
        Key? key = null) : base(key)
    {
        StartValueNormalized = startValueNormalized;
        EndValueNormalized = endValueNormalized;
        Divisions = divisions;
        IsInteractive = isInteractive;
        IsFocused = isFocused;
        TrackHeight = trackHeight;
        ThumbRadius = thumbRadius;
        OverlayRadius = overlayRadius;
        MinPreferredHeight = minPreferredHeight;
        ActiveTrackColor = activeTrackColor;
        InactiveTrackColor = inactiveTrackColor;
        ThumbColor = thumbColor;
        OverlayFocusedColor = overlayFocusedColor;
        OverlayHoveredColor = overlayHoveredColor;
        OverlayDraggedColor = overlayDraggedColor;
        TextDirection = textDirection;
        OnChangeStartNormalized = onChangeStartNormalized;
        OnChangedNormalized = onChangedNormalized;
        OnChangeEndNormalized = onChangeEndNormalized;
    }

    public double StartValueNormalized { get; }

    public double EndValueNormalized { get; }

    public int? Divisions { get; }

    public bool IsInteractive { get; }

    public bool IsFocused { get; }

    public double TrackHeight { get; }

    public double ThumbRadius { get; }

    public double OverlayRadius { get; }

    public double MinPreferredHeight { get; }

    public Color ActiveTrackColor { get; }

    public Color InactiveTrackColor { get; }

    public Color ThumbColor { get; }

    public Color? OverlayFocusedColor { get; }

    public Color? OverlayHoveredColor { get; }

    public Color? OverlayDraggedColor { get; }

    public TextDirection TextDirection { get; }

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangeStartNormalized { get; }

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangedNormalized { get; }

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangeEndNormalized { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderRangeSlider(
            startValueNormalized: StartValueNormalized,
            endValueNormalized: EndValueNormalized,
            divisions: Divisions,
            isInteractive: IsInteractive,
            isFocused: IsFocused,
            trackHeight: TrackHeight,
            thumbRadius: ThumbRadius,
            overlayRadius: OverlayRadius,
            minPreferredHeight: MinPreferredHeight,
            activeTrackColor: ActiveTrackColor,
            inactiveTrackColor: InactiveTrackColor,
            thumbColor: ThumbColor,
            overlayFocusedColor: OverlayFocusedColor,
            overlayHoveredColor: OverlayHoveredColor,
            overlayDraggedColor: OverlayDraggedColor,
            textDirection: TextDirection,
            onChangeStartNormalized: OnChangeStartNormalized,
            onChangedNormalized: OnChangedNormalized,
            onChangeEndNormalized: OnChangeEndNormalized);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var rangeSlider = (RenderRangeSlider)renderObject;
        rangeSlider.StartValueNormalized = StartValueNormalized;
        rangeSlider.EndValueNormalized = EndValueNormalized;
        rangeSlider.Divisions = Divisions;
        rangeSlider.IsInteractive = IsInteractive;
        rangeSlider.IsFocused = IsFocused;
        rangeSlider.TrackHeight = TrackHeight;
        rangeSlider.ThumbRadius = ThumbRadius;
        rangeSlider.OverlayRadius = OverlayRadius;
        rangeSlider.MinPreferredHeight = MinPreferredHeight;
        rangeSlider.ActiveTrackColor = ActiveTrackColor;
        rangeSlider.InactiveTrackColor = InactiveTrackColor;
        rangeSlider.ThumbColor = ThumbColor;
        rangeSlider.OverlayFocusedColor = OverlayFocusedColor;
        rangeSlider.OverlayHoveredColor = OverlayHoveredColor;
        rangeSlider.OverlayDraggedColor = OverlayDraggedColor;
        rangeSlider.TextDirection = TextDirection;
        rangeSlider.OnChangeStartNormalized = OnChangeStartNormalized;
        rangeSlider.OnChangedNormalized = OnChangedNormalized;
        rangeSlider.OnChangeEndNormalized = OnChangeEndNormalized;
    }
}

internal sealed class RenderRangeSlider : RenderBox
{
    private const double DefaultTrackWidth = 144.0;
    private const double Epsilon = 0.0001;

    private double _startValueNormalized;
    private double _endValueNormalized;
    private int? _divisions;
    private bool _isInteractive;
    private bool _isFocused;
    private double _trackHeight;
    private double _thumbRadius;
    private double _overlayRadius;
    private double _minPreferredHeight;
    private Color _activeTrackColor;
    private Color _inactiveTrackColor;
    private Color _thumbColor;
    private Color? _overlayFocusedColor;
    private Color? _overlayHoveredColor;
    private Color? _overlayDraggedColor;
    private TextDirection _textDirection;
    private Action<NormalizedRangeValues, RangeSliderThumb>? _onChangeStartNormalized;
    private Action<NormalizedRangeValues, RangeSliderThumb>? _onChangedNormalized;
    private Action<NormalizedRangeValues, RangeSliderThumb>? _onChangeEndNormalized;

    private bool _hovered;
    private bool _dragging;
    private int? _activePointer;
    private RangeSliderThumb? _activeThumb;
    private NormalizedRangeValues? _dragValues;
    private double? _lastGlobalPointerX;

    public RenderRangeSlider(
        double startValueNormalized,
        double endValueNormalized,
        int? divisions,
        bool isInteractive,
        bool isFocused,
        double trackHeight,
        double thumbRadius,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        TextDirection textDirection,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangeStartNormalized,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangedNormalized,
        Action<NormalizedRangeValues, RangeSliderThumb>? onChangeEndNormalized)
    {
        var initial = OrderAndClamp(startValueNormalized, endValueNormalized);
        _startValueNormalized = initial.Start;
        _endValueNormalized = initial.End;
        _divisions = divisions;
        _isInteractive = isInteractive;
        _isFocused = isFocused;
        _trackHeight = trackHeight;
        _thumbRadius = thumbRadius;
        _overlayRadius = overlayRadius;
        _minPreferredHeight = minPreferredHeight;
        _activeTrackColor = activeTrackColor;
        _inactiveTrackColor = inactiveTrackColor;
        _thumbColor = thumbColor;
        _overlayFocusedColor = overlayFocusedColor;
        _overlayHoveredColor = overlayHoveredColor;
        _overlayDraggedColor = overlayDraggedColor;
        _textDirection = textDirection;
        _onChangeStartNormalized = onChangeStartNormalized;
        _onChangedNormalized = onChangedNormalized;
        _onChangeEndNormalized = onChangeEndNormalized;
    }

    public double StartValueNormalized
    {
        get => _startValueNormalized;
        set
        {
            var normalized = OrderAndClamp(value, _endValueNormalized);
            if (Math.Abs(_startValueNormalized - normalized.Start) <= Epsilon
                && Math.Abs(_endValueNormalized - normalized.End) <= Epsilon)
            {
                return;
            }

            _startValueNormalized = normalized.Start;
            _endValueNormalized = normalized.End;
            if (!_dragging)
            {
                MarkNeedsPaint();
            }
        }
    }

    public double EndValueNormalized
    {
        get => _endValueNormalized;
        set
        {
            var normalized = OrderAndClamp(_startValueNormalized, value);
            if (Math.Abs(_startValueNormalized - normalized.Start) <= Epsilon
                && Math.Abs(_endValueNormalized - normalized.End) <= Epsilon)
            {
                return;
            }

            _startValueNormalized = normalized.Start;
            _endValueNormalized = normalized.End;
            if (!_dragging)
            {
                MarkNeedsPaint();
            }
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
        }
    }

    public bool IsInteractive
    {
        get => _isInteractive;
        set
        {
            if (_isInteractive == value)
            {
                return;
            }

            _isInteractive = value;
            if (!_isInteractive)
            {
                EndDragIfNeeded(canceled: true);
            }

            MarkNeedsPaint();
        }
    }

    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            if (_isFocused == value)
            {
                return;
            }

            _isFocused = value;
            MarkNeedsPaint();
        }
    }

    public double TrackHeight
    {
        get => _trackHeight;
        set
        {
            if (Math.Abs(_trackHeight - value) <= Epsilon)
            {
                return;
            }

            _trackHeight = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double ThumbRadius
    {
        get => _thumbRadius;
        set
        {
            if (Math.Abs(_thumbRadius - value) <= Epsilon)
            {
                return;
            }

            _thumbRadius = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double OverlayRadius
    {
        get => _overlayRadius;
        set
        {
            if (Math.Abs(_overlayRadius - value) <= Epsilon)
            {
                return;
            }

            _overlayRadius = value;
            MarkNeedsPaint();
        }
    }

    public double MinPreferredHeight
    {
        get => _minPreferredHeight;
        set
        {
            if (Math.Abs(_minPreferredHeight - value) <= Epsilon)
            {
                return;
            }

            _minPreferredHeight = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public Color ActiveTrackColor
    {
        get => _activeTrackColor;
        set
        {
            if (_activeTrackColor == value)
            {
                return;
            }

            _activeTrackColor = value;
            MarkNeedsPaint();
        }
    }

    public Color InactiveTrackColor
    {
        get => _inactiveTrackColor;
        set
        {
            if (_inactiveTrackColor == value)
            {
                return;
            }

            _inactiveTrackColor = value;
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

    public Color? OverlayFocusedColor
    {
        get => _overlayFocusedColor;
        set
        {
            if (_overlayFocusedColor == value)
            {
                return;
            }

            _overlayFocusedColor = value;
            MarkNeedsPaint();
        }
    }

    public Color? OverlayHoveredColor
    {
        get => _overlayHoveredColor;
        set
        {
            if (_overlayHoveredColor == value)
            {
                return;
            }

            _overlayHoveredColor = value;
            MarkNeedsPaint();
        }
    }

    public Color? OverlayDraggedColor
    {
        get => _overlayDraggedColor;
        set
        {
            if (_overlayDraggedColor == value)
            {
                return;
            }

            _overlayDraggedColor = value;
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

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangeStartNormalized
    {
        get => _onChangeStartNormalized;
        set => _onChangeStartNormalized = value;
    }

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangedNormalized
    {
        get => _onChangedNormalized;
        set => _onChangedNormalized = value;
    }

    public Action<NormalizedRangeValues, RangeSliderThumb>? OnChangeEndNormalized
    {
        get => _onChangeEndNormalized;
        set => _onChangeEndNormalized = value;
    }

    protected override bool HitTestSelf(Point position)
    {
        return true;
    }

    protected override void PerformLayout()
    {
        double desiredWidth = Constraints.HasBoundedWidth ? Constraints.MaxWidth : DefaultTrackWidth;
        if (!double.IsFinite(desiredWidth) || desiredWidth <= 0)
        {
            desiredWidth = DefaultTrackWidth;
        }

        double desiredHeight = Math.Max(MinPreferredHeight, Math.Max(TrackHeight, ThumbRadius * 2.0));
        if (!double.IsFinite(desiredHeight) || desiredHeight <= 0)
        {
            desiredHeight = Math.Max(TrackHeight, ThumbRadius * 2.0);
        }

        Size = Constraints.Constrain(new Size(desiredWidth, desiredHeight));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        var values = ResolveVisualValues();
        double centerY = offset.Y + (Size.Height / 2.0);
        var geometry = ResolveTrackGeometry(offset.X);

        if (geometry.Width > 0 && TrackHeight > 0)
        {
            var trackRect = new Rect(
                geometry.Left,
                centerY - (TrackHeight / 2.0),
                geometry.Width,
                TrackHeight);
            ctx.DrawRectangle(
                brush: new SolidColorBrush(InactiveTrackColor),
                pen: null,
                rect: trackRect,
                radiusX: TrackHeight / 2.0,
                radiusY: TrackHeight / 2.0);

            double startThumbCenterX = ResolveThumbCenterX(geometry, values.Start);
            double endThumbCenterX = ResolveThumbCenterX(geometry, values.End);
            double activeLeft = Math.Min(startThumbCenterX, endThumbCenterX);
            double activeRight = Math.Max(startThumbCenterX, endThumbCenterX);
            double activeWidth = Math.Max(0.0, activeRight - activeLeft);
            if (activeWidth > 0)
            {
                var activeRect = new Rect(
                    activeLeft,
                    centerY - (TrackHeight / 2.0),
                    activeWidth,
                    TrackHeight);
                ctx.DrawRectangle(
                    brush: new SolidColorBrush(ActiveTrackColor),
                    pen: null,
                    rect: activeRect,
                    radiusX: TrackHeight / 2.0,
                    radiusY: TrackHeight / 2.0);
            }

            var overlayColor = ResolveOverlayColor();
            if (overlayColor.HasValue && overlayColor.Value.A > 0 && OverlayRadius > 0)
            {
                double overlayCenterX = ResolveOverlayCenterX(startThumbCenterX, endThumbCenterX);
                ctx.DrawCircle(
                    brush: new SolidColorBrush(overlayColor.Value),
                    pen: null,
                    center: new Point(overlayCenterX, centerY),
                    radius: OverlayRadius);
            }

            ctx.DrawCircle(
                brush: new SolidColorBrush(ThumbColor),
                pen: null,
                center: new Point(startThumbCenterX, centerY),
                radius: ThumbRadius);
            ctx.DrawCircle(
                brush: new SolidColorBrush(ThumbColor),
                pen: null,
                center: new Point(endThumbCenterX, centerY),
                radius: ThumbRadius);
        }
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        switch (@event)
        {
            case PointerDownEvent downEvent:
                HandlePointerDown(downEvent);
                break;
            case PointerMoveEvent moveEvent:
                HandlePointerMove(moveEvent);
                break;
            case PointerUpEvent upEvent:
                HandlePointerUp(upEvent);
                break;
            case PointerCancelEvent cancelEvent:
                HandlePointerCancel(cancelEvent);
                break;
            case PointerEnterEvent:
                HandlePointerEnter();
                break;
            case PointerExitEvent:
                HandlePointerExit();
                break;
        }
    }

    private void HandlePointerDown(PointerDownEvent @event)
    {
        if (!IsInteractive || !IsPrimaryButton(@event.Buttons))
        {
            return;
        }

        _activePointer = @event.Pointer;
        _dragging = true;
        _hovered = true;
        _dragValues = ResolveVisualValues();
        _activeThumb = ResolveClosestThumb(@event.LocalPosition.X, _dragValues.Value);
        _lastGlobalPointerX = @event.Position.X;

        if (_activeThumb.HasValue)
        {
            OnChangeStartNormalized?.Invoke(_dragValues.Value, _activeThumb.Value);
            UpdateActiveThumbFromLocalX(@event.LocalPosition.X);
        }

        MarkNeedsPaint();
    }

    private void HandlePointerMove(PointerMoveEvent @event)
    {
        if (!IsInteractive || _activePointer != @event.Pointer)
        {
            return;
        }

        double deltaX = _lastGlobalPointerX.HasValue
            ? @event.Position.X - _lastGlobalPointerX.Value
            : @event.Delta.X;
        _lastGlobalPointerX = @event.Position.X;
        UpdateActiveThumbFromDeltaX(deltaX);
    }

    private void HandlePointerUp(PointerUpEvent @event)
    {
        if (_activePointer != @event.Pointer)
        {
            return;
        }

        EndDragIfNeeded(canceled: false);
    }

    private void HandlePointerCancel(PointerCancelEvent @event)
    {
        if (_activePointer != @event.Pointer)
        {
            return;
        }

        EndDragIfNeeded(canceled: true);
    }

    private void HandlePointerEnter()
    {
        if (!IsInteractive || _hovered)
        {
            return;
        }

        _hovered = true;
        MarkNeedsPaint();
    }

    private void HandlePointerExit()
    {
        if (!_hovered)
        {
            return;
        }

        _hovered = false;
        MarkNeedsPaint();
    }

    private void UpdateActiveThumbFromLocalX(double localX)
    {
        if (!_activeThumb.HasValue)
        {
            return;
        }

        var current = ResolveVisualValues();
        double nextNormalized = ResolveNormalizedFromLocalX(localX);
        var next = UpdateThumbValue(current, _activeThumb.Value, nextNormalized);
        if (AreEqual(current, next))
        {
            return;
        }

        _dragValues = next;
        OnChangedNormalized?.Invoke(next, _activeThumb.Value);
        MarkNeedsPaint();
    }

    private void UpdateActiveThumbFromDeltaX(double deltaX)
    {
        if (!_activeThumb.HasValue)
        {
            return;
        }

        var current = ResolveVisualValues();
        var geometry = ResolveTrackGeometry(offsetX: 0);
        if (geometry.Width <= Epsilon)
        {
            return;
        }

        double directionMultiplier = TextDirection == TextDirection.Rtl ? -1.0 : 1.0;
        double normalizedDelta = (deltaX / geometry.Width) * directionMultiplier;
        if (Math.Abs(normalizedDelta) <= Epsilon)
        {
            return;
        }

        double baseValue = _activeThumb.Value == RangeSliderThumb.Start ? current.Start : current.End;
        var next = UpdateThumbValue(current, _activeThumb.Value, baseValue + normalizedDelta);
        if (AreEqual(current, next))
        {
            return;
        }

        _dragValues = next;
        OnChangedNormalized?.Invoke(next, _activeThumb.Value);
        MarkNeedsPaint();
    }

    private void EndDragIfNeeded(bool canceled)
    {
        if (!_dragging && _activePointer is null)
        {
            return;
        }

        var finalValues = ResolveVisualValues();
        var finalThumb = _activeThumb ?? RangeSliderThumb.End;
        _activePointer = null;
        _dragging = false;
        _activeThumb = null;
        _dragValues = null;
        _lastGlobalPointerX = null;

        if (!canceled)
        {
            OnChangeEndNormalized?.Invoke(finalValues, finalThumb);
        }

        MarkNeedsPaint();
    }

    private NormalizedRangeValues ResolveVisualValues()
    {
        if (_dragging && _dragValues.HasValue)
        {
            return OrderAndClamp(_dragValues.Value.Start, _dragValues.Value.End);
        }

        return OrderAndClamp(StartValueNormalized, EndValueNormalized);
    }

    private double ResolveNormalizedFromLocalX(double localX)
    {
        var geometry = ResolveTrackGeometry(offsetX: 0);
        if (geometry.Width <= Epsilon)
        {
            return 0.0;
        }

        double relative = Math.Clamp(localX - geometry.Left, 0.0, geometry.Width);
        double normalized = relative / geometry.Width;
        if (TextDirection == TextDirection.Rtl)
        {
            normalized = 1.0 - normalized;
        }

        return SnapNormalized(normalized);
    }

    private RangeSliderThumb ResolveClosestThumb(double localX, NormalizedRangeValues values)
    {
        double tapValue = ResolveNormalizedFromLocalX(localX);
        double distanceToStart = Math.Abs(tapValue - values.Start);
        double distanceToEnd = Math.Abs(tapValue - values.End);
        if (Math.Abs(distanceToStart - distanceToEnd) <= Epsilon)
        {
            return tapValue <= ((values.Start + values.End) / 2.0)
                ? RangeSliderThumb.Start
                : RangeSliderThumb.End;
        }

        return distanceToStart < distanceToEnd
            ? RangeSliderThumb.Start
            : RangeSliderThumb.End;
    }

    private NormalizedRangeValues UpdateThumbValue(
        NormalizedRangeValues current,
        RangeSliderThumb thumb,
        double nextRawValue)
    {
        double nextValue = SnapNormalized(nextRawValue);
        if (thumb == RangeSliderThumb.Start)
        {
            double nextStart = Math.Clamp(nextValue, 0.0, current.End);
            return OrderAndClamp(nextStart, current.End);
        }

        double nextEnd = Math.Clamp(nextValue, current.Start, 1.0);
        return OrderAndClamp(current.Start, nextEnd);
    }

    private double ResolveThumbCenterX(TrackGeometry geometry, double normalizedValue)
    {
        double value = ClampNormalized(normalizedValue);
        double visualValue = TextDirection == TextDirection.Rtl
            ? 1.0 - value
            : value;
        return geometry.Left + (geometry.Width * visualValue);
    }

    private TrackGeometry ResolveTrackGeometry(double offsetX)
    {
        double left = offsetX + ThumbRadius;
        double right = offsetX + Size.Width - ThumbRadius;
        if (right < left)
        {
            double center = offsetX + (Size.Width / 2.0);
            left = center;
            right = center;
        }

        return new TrackGeometry(left, right);
    }

    private double ResolveOverlayCenterX(double startThumbCenterX, double endThumbCenterX)
    {
        if (_activeThumb == RangeSliderThumb.Start)
        {
            return startThumbCenterX;
        }

        if (_activeThumb == RangeSliderThumb.End)
        {
            return endThumbCenterX;
        }

        return (startThumbCenterX + endThumbCenterX) / 2.0;
    }

    private Color? ResolveOverlayColor()
    {
        if (!IsInteractive)
        {
            return null;
        }

        if (_dragging && _activeThumb.HasValue && OverlayDraggedColor.HasValue && OverlayDraggedColor.Value.A > 0)
        {
            return OverlayDraggedColor.Value;
        }

        if (_hovered && OverlayHoveredColor.HasValue && OverlayHoveredColor.Value.A > 0)
        {
            return OverlayHoveredColor.Value;
        }

        if (IsFocused && OverlayFocusedColor.HasValue && OverlayFocusedColor.Value.A > 0)
        {
            return OverlayFocusedColor.Value;
        }

        return null;
    }

    private double SnapNormalized(double normalized)
    {
        double clamped = ClampNormalized(normalized);
        if (!Divisions.HasValue || Divisions.Value <= 0)
        {
            return clamped;
        }

        int divisions = Divisions.Value;
        return Math.Clamp(Math.Round(clamped * divisions) / divisions, 0.0, 1.0);
    }

    private static bool IsPrimaryButton(PointerButtons buttons)
    {
        return buttons.HasFlag(PointerButtons.Primary);
    }

    private static double ClampNormalized(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0.0;
        }

        return Math.Clamp(value, 0.0, 1.0);
    }

    private static NormalizedRangeValues OrderAndClamp(double start, double end)
    {
        double clampedStart = ClampNormalized(start);
        double clampedEnd = ClampNormalized(end);
        if (clampedEnd < clampedStart)
        {
            (clampedStart, clampedEnd) = (clampedEnd, clampedStart);
        }

        return new NormalizedRangeValues(clampedStart, clampedEnd);
    }

    private static bool AreEqual(NormalizedRangeValues a, NormalizedRangeValues b)
    {
        return Math.Abs(a.Start - b.Start) <= Epsilon
               && Math.Abs(a.End - b.End) <= Epsilon;
    }

    private readonly record struct TrackGeometry(double Left, double Right)
    {
        public double Width => Math.Max(0, Right - Left);
    }
}
