using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/slider.dart (approximate)

public sealed class Slider : StatefulWidget
{
    public Slider(
        double value,
        Action<double>? onChanged,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? thumbColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        string? semanticLabel = null,
        Key? key = null) : base(key)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Slider value must be finite.");
        }

        if (double.IsNaN(min) || double.IsInfinity(min))
        {
            throw new ArgumentOutOfRangeException(nameof(min), "Slider min must be finite.");
        }

        if (double.IsNaN(max) || double.IsInfinity(max))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Slider max must be finite.");
        }

        if (max < min)
        {
            throw new ArgumentException("Slider max must be greater than or equal to min.", nameof(max));
        }

        if (value < min || value > max)
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
        InactiveColor = inactiveColor;
        ThumbColor = thumbColor;
        OverlayColor = overlayColor;
        MaterialTapTargetSize = materialTapTargetSize;
        FocusNode = focusNode;
        Autofocus = autofocus;
        SemanticLabel = semanticLabel;
    }

    public double Value { get; }

    public Action<double>? OnChanged { get; }

    public Action<double>? OnChangeStart { get; }

    public Action<double>? OnChangeEnd { get; }

    public double Min { get; }

    public double Max { get; }

    public int? Divisions { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public Color? ThumbColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public string? SemanticLabel { get; }

    public override State CreateState()
    {
        return new SliderState();
    }

    private sealed class SliderState : State
    {
        private const double DefaultTrackHeight = 4.0;
        private const double DefaultThumbRadius = 10.0;
        private const double PaddedTapTargetExtent = 48.0;

        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _hasFocus;

        private Slider CurrentWidget => (Slider)StateWidget;

        private bool IsInteractive => CurrentWidget.OnChanged is not null && CurrentWidget.Max > CurrentWidget.Min;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldSlider = (Slider)oldWidget;
            if (!ReferenceEquals(oldSlider.FocusNode, CurrentWidget.FocusNode))
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
            var trackHeight = ResolveTrackHeight(sliderTheme);
            var thumbRadius = ResolveThumbRadius(sliderTheme);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? sliderTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            var minPreferredHeight = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? Math.Max(PaddedTapTargetExtent, thumbRadius * 2)
                : Math.Max(trackHeight, thumbRadius * 2);
            var overlayRadius = Math.Max(thumbRadius, theme.UseMaterial3 ? 20.0 : 16.0);

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

            return new Semantics(
                label: CurrentWidget.SemanticLabel,
                flags: semanticsFlags,
                child: new Focus(
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    canRequestFocus: IsInteractive,
                    onKeyEvent: HandleKeyEvent,
                    child: new SliderRenderWidget(
                        valueNormalized: Normalize(CurrentWidget.Value),
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
            var hasFocus = _focusNode?.HasFocus ?? false;
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

            var normalized = Normalize(CurrentWidget.Value);
            var next = ResolveKeyboardTargetNormalized(normalized, @event.Key);
            if (Math.Abs(next - normalized) <= 0.0001)
            {
                return KeyEventResult.Handled;
            }

            CurrentWidget.OnChangeStart?.Invoke(CurrentWidget.Value);
            CurrentWidget.OnChanged?.Invoke(Denormalize(next));
            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(next));
            return KeyEventResult.Handled;
        }

        private double ResolveKeyboardTargetNormalized(double currentNormalized, string key)
        {
            if (string.Equals(key, "Home", StringComparison.Ordinal))
            {
                return 0.0;
            }

            if (string.Equals(key, "End", StringComparison.Ordinal))
            {
                return 1.0;
            }

            var step = ResolveAdjustmentUnit(Theme.Of(Context));
            var direction = Directionality.Of(Context);
            var delta = 0.0;
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

            var next = Math.Clamp(currentNormalized + delta, 0.0, 1.0);
            return SnapNormalized(next);
        }

        private double ResolveAdjustmentUnit(ThemeData theme)
        {
            return theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? 0.1
                : 0.05;
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

        private void HandleChangeStartNormalized(double normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            CurrentWidget.OnChangeStart?.Invoke(Denormalize(SnapNormalized(normalized)));
        }

        private void HandleChangedNormalized(double normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            var nextValue = Denormalize(SnapNormalized(normalized));
            if (Math.Abs(nextValue - CurrentWidget.Value) <= 0.0001)
            {
                return;
            }

            CurrentWidget.OnChanged?.Invoke(nextValue);
        }

        private void HandleChangeEndNormalized(double normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(SnapNormalized(normalized)));
        }

        private double Normalize(double value)
        {
            var range = CurrentWidget.Max - CurrentWidget.Min;
            if (range <= 0)
            {
                return 0.0;
            }

            return Math.Clamp((value - CurrentWidget.Min) / range, 0.0, 1.0);
        }

        private double Denormalize(double normalized)
        {
            var clamped = Math.Clamp(normalized, 0.0, 1.0);
            return CurrentWidget.Min + ((CurrentWidget.Max - CurrentWidget.Min) * clamped);
        }

        private double SnapNormalized(double normalized)
        {
            var clamped = Math.Clamp(normalized, 0.0, 1.0);
            if (!CurrentWidget.Divisions.HasValue || CurrentWidget.Divisions.Value <= 0)
            {
                return clamped;
            }

            var divisions = CurrentWidget.Divisions.Value;
            return Math.Clamp(Math.Round(clamped * divisions) / divisions, 0.0, 1.0);
        }

        private double ResolveTrackHeight(SliderThemeData sliderTheme)
        {
            var resolved = sliderTheme.TrackHeight ?? DefaultTrackHeight;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return DefaultTrackHeight;
            }

            return resolved;
        }

        private double ResolveThumbRadius(SliderThemeData sliderTheme)
        {
            var resolved = sliderTheme.ThumbRadius ?? DefaultThumbRadius;
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
            return CurrentWidget.ThumbColor
                   ?? CurrentWidget.ActiveColor
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
    }
}

internal sealed class SliderRenderWidget : LeafRenderObjectWidget
{
    public SliderRenderWidget(
        double valueNormalized,
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
        Action<double>? onChangeStartNormalized,
        Action<double>? onChangedNormalized,
        Action<double>? onChangeEndNormalized,
        Key? key = null) : base(key)
    {
        ValueNormalized = valueNormalized;
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

    public double ValueNormalized { get; }

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

    public Action<double>? OnChangeStartNormalized { get; }

    public Action<double>? OnChangedNormalized { get; }

    public Action<double>? OnChangeEndNormalized { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSlider(
            valueNormalized: ValueNormalized,
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
        var slider = (RenderSlider)renderObject;
        slider.ValueNormalized = ValueNormalized;
        slider.Divisions = Divisions;
        slider.IsInteractive = IsInteractive;
        slider.IsFocused = IsFocused;
        slider.TrackHeight = TrackHeight;
        slider.ThumbRadius = ThumbRadius;
        slider.OverlayRadius = OverlayRadius;
        slider.MinPreferredHeight = MinPreferredHeight;
        slider.ActiveTrackColor = ActiveTrackColor;
        slider.InactiveTrackColor = InactiveTrackColor;
        slider.ThumbColor = ThumbColor;
        slider.OverlayFocusedColor = OverlayFocusedColor;
        slider.OverlayHoveredColor = OverlayHoveredColor;
        slider.OverlayDraggedColor = OverlayDraggedColor;
        slider.TextDirection = TextDirection;
        slider.OnChangeStartNormalized = OnChangeStartNormalized;
        slider.OnChangedNormalized = OnChangedNormalized;
        slider.OnChangeEndNormalized = OnChangeEndNormalized;
    }
}

internal sealed class RenderSlider : RenderBox
{
    private const double DefaultTrackWidth = 144.0;
    private const double Epsilon = 0.0001;

    private double _valueNormalized;
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
    private Action<double>? _onChangeStartNormalized;
    private Action<double>? _onChangedNormalized;
    private Action<double>? _onChangeEndNormalized;

    private bool _hovered;
    private bool _dragging;
    private int? _activePointer;
    private double? _dragValueNormalized;

    public RenderSlider(
        double valueNormalized,
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
        Action<double>? onChangeStartNormalized,
        Action<double>? onChangedNormalized,
        Action<double>? onChangeEndNormalized)
    {
        _valueNormalized = ClampNormalized(valueNormalized);
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

    public double ValueNormalized
    {
        get => _valueNormalized;
        set
        {
            var normalized = ClampNormalized(value);
            if (Math.Abs(_valueNormalized - normalized) <= Epsilon)
            {
                return;
            }

            _valueNormalized = normalized;
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

    public Action<double>? OnChangeStartNormalized
    {
        get => _onChangeStartNormalized;
        set => _onChangeStartNormalized = value;
    }

    public Action<double>? OnChangedNormalized
    {
        get => _onChangedNormalized;
        set => _onChangedNormalized = value;
    }

    public Action<double>? OnChangeEndNormalized
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
        var desiredWidth = Constraints.HasBoundedWidth ? Constraints.MaxWidth : DefaultTrackWidth;
        if (!double.IsFinite(desiredWidth) || desiredWidth <= 0)
        {
            desiredWidth = DefaultTrackWidth;
        }

        var desiredHeight = Math.Max(MinPreferredHeight, Math.Max(TrackHeight, ThumbRadius * 2.0));
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

        var visualValue = ResolveVisualValue();
        var centerY = offset.Y + (Size.Height / 2.0);
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

            var thumbCenterX = ResolveThumbCenterX(geometry, visualValue);
            if (TextDirection == TextDirection.Ltr)
            {
                var activeWidth = Math.Max(0.0, thumbCenterX - geometry.Left);
                if (activeWidth > 0)
                {
                    var activeRect = new Rect(
                        geometry.Left,
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
            }
            else
            {
                var activeWidth = Math.Max(0.0, geometry.Right - thumbCenterX);
                if (activeWidth > 0)
                {
                    var activeRect = new Rect(
                        thumbCenterX,
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
            }

            var overlayColor = ResolveOverlayColor();
            if (overlayColor.HasValue && overlayColor.Value.A > 0 && OverlayRadius > 0)
            {
                ctx.DrawCircle(
                    brush: new SolidColorBrush(overlayColor.Value),
                    pen: null,
                    center: new Point(thumbCenterX, centerY),
                    radius: OverlayRadius);
            }

            ctx.DrawCircle(
                brush: new SolidColorBrush(ThumbColor),
                pen: null,
                center: new Point(thumbCenterX, centerY),
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
        if (!IsInteractive || !_isPrimaryButton(@event.Buttons))
        {
            return;
        }

        _activePointer = @event.Pointer;
        _dragging = true;
        _hovered = true;
        _dragValueNormalized = ResolveVisualValue();

        OnChangeStartNormalized?.Invoke(_dragValueNormalized.Value);
        UpdateDragValueFromLocalX(@event.LocalPosition.X);
        MarkNeedsPaint();
    }

    private void HandlePointerMove(PointerMoveEvent @event)
    {
        if (!IsInteractive || _activePointer != @event.Pointer)
        {
            return;
        }

        UpdateDragValueFromDeltaX(@event.Delta.X);
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

    private void UpdateDragValueFromLocalX(double localX)
    {
        var next = ResolveNormalizedFromLocalX(localX);
        if (_dragValueNormalized.HasValue && Math.Abs(_dragValueNormalized.Value - next) <= Epsilon)
        {
            return;
        }

        _dragValueNormalized = next;
        OnChangedNormalized?.Invoke(next);
        MarkNeedsPaint();
    }

    private void UpdateDragValueFromDeltaX(double deltaX)
    {
        var current = ResolveVisualValue();
        var geometry = ResolveTrackGeometry(offsetX: 0);
        if (geometry.Width <= Epsilon)
        {
            return;
        }

        var directionMultiplier = TextDirection == TextDirection.Rtl ? -1.0 : 1.0;
        var normalizedDelta = (deltaX / geometry.Width) * directionMultiplier;
        var next = current + normalizedDelta;
        if (Divisions.HasValue && Divisions.Value > 0)
        {
            next = Math.Round(next * Divisions.Value) / Divisions.Value;
        }

        next = ClampNormalized(next);
        if (_dragValueNormalized.HasValue && Math.Abs(_dragValueNormalized.Value - next) <= Epsilon)
        {
            return;
        }

        _dragValueNormalized = next;
        OnChangedNormalized?.Invoke(next);
        MarkNeedsPaint();
    }

    private void EndDragIfNeeded(bool canceled)
    {
        if (!_dragging && _activePointer is null)
        {
            return;
        }

        var finalValue = ResolveVisualValue();
        _activePointer = null;
        _dragging = false;
        _dragValueNormalized = null;

        if (!canceled)
        {
            OnChangeEndNormalized?.Invoke(finalValue);
        }

        MarkNeedsPaint();
    }

    private double ResolveVisualValue()
    {
        return ClampNormalized(_dragging && _dragValueNormalized.HasValue
            ? _dragValueNormalized.Value
            : ValueNormalized);
    }

    private double ResolveNormalizedFromLocalX(double localX)
    {
        var geometry = ResolveTrackGeometry(offsetX: 0);
        if (geometry.Width <= Epsilon)
        {
            return ClampNormalized(ValueNormalized);
        }

        var relative = Math.Clamp(localX - geometry.Left, 0.0, geometry.Width);
        var normalized = relative / geometry.Width;
        if (TextDirection == TextDirection.Rtl)
        {
            normalized = 1.0 - normalized;
        }

        if (Divisions.HasValue && Divisions.Value > 0)
        {
            normalized = Math.Round(normalized * Divisions.Value) / Divisions.Value;
        }

        return ClampNormalized(normalized);
    }

    private double ResolveThumbCenterX(TrackGeometry geometry, double normalizedValue)
    {
        var value = ClampNormalized(normalizedValue);
        var visualValue = TextDirection == TextDirection.Rtl
            ? 1.0 - value
            : value;
        return geometry.Left + (geometry.Width * visualValue);
    }

    private TrackGeometry ResolveTrackGeometry(double offsetX)
    {
        var left = offsetX + ThumbRadius;
        var right = offsetX + Size.Width - ThumbRadius;
        if (right < left)
        {
            var center = offsetX + (Size.Width / 2.0);
            left = center;
            right = center;
        }

        return new TrackGeometry(left, right);
    }

    private Color? ResolveOverlayColor()
    {
        if (!IsInteractive)
        {
            return null;
        }

        if (_dragging && OverlayDraggedColor.HasValue && OverlayDraggedColor.Value.A > 0)
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

    private static bool _isPrimaryButton(PointerButtons buttons)
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

    private readonly record struct TrackGeometry(double Left, double Right)
    {
        public double Width => Math.Max(0, Right - Left);
    }
}
