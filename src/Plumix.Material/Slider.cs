using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider.dart

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
        double? secondaryTrackValue = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? secondaryActiveColor = null,
        Color? thumbColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        string? semanticLabel = null,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        Key? key = null,
        string? label = null,
        MouseCursor? mouseCursor = null,
        SliderInteraction? allowedInteraction = null,
        EdgeInsetsGeometry? padding = null,
        ShowValueIndicator? showValueIndicator = null,
        bool? year2023 = null) : base(key)
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

        if (padding.HasValue && (padding.Value.Left < 0 || padding.Value.Top < 0
                                 || padding.Value.Right < 0 || padding.Value.Bottom < 0
                                 || padding.Value.Start < 0 || padding.Value.End < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Slider padding cannot be negative.");
        }

        if (secondaryTrackValue.HasValue)
        {
            if (double.IsNaN(secondaryTrackValue.Value) || double.IsInfinity(secondaryTrackValue.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondaryTrackValue),
                    "Slider secondaryTrackValue must be finite.");
            }

            if (secondaryTrackValue.Value < min || secondaryTrackValue.Value > max)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondaryTrackValue),
                    "Slider secondaryTrackValue must be between min and max.");
            }
        }

        Value = value;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        Label = label;
        SecondaryTrackValue = secondaryTrackValue;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        SecondaryActiveColor = secondaryActiveColor;
        ThumbColor = thumbColor;
        OverlayColor = overlayColor;
        MouseCursor = mouseCursor;
        MaterialTapTargetSize = materialTapTargetSize;
        FocusNode = focusNode;
        Autofocus = autofocus;
        SemanticLabel = semanticLabel;
        SemanticFormatterCallback = semanticFormatterCallback;
        AllowedInteraction = allowedInteraction;
        Padding = padding;
        ShowValueIndicator = showValueIndicator;
        Year2023 = year2023;
    }

    public double Value { get; }

    public Action<double>? OnChanged { get; }

    public Action<double>? OnChangeStart { get; }

    public Action<double>? OnChangeEnd { get; }

    public double Min { get; }

    public double Max { get; }

    public int? Divisions { get; }

    public string? Label { get; }

    public double? SecondaryTrackValue { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public Color? SecondaryActiveColor { get; }

    public Color? ThumbColor { get; }

    public WidgetStateProperty<Color?>? OverlayColor { get; }

    public MouseCursor? MouseCursor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public string? SemanticLabel { get; }

    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    public SliderInteraction? AllowedInteraction { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public ShowValueIndicator? ShowValueIndicator { get; }

    public bool? Year2023 { get; }

    private bool IsAdaptive { get; init; }

    public static Slider Adaptive(
        double value,
        Action<double>? onChanged,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        string? label = null,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        double? secondaryTrackValue = null,
        Color? secondaryActiveColor = null,
        Color? thumbColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        SliderInteraction? allowedInteraction = null,
        ShowValueIndicator? showValueIndicator = null,
        bool? year2023 = null,
        Key? key = null)
    {
        return new Slider(
            value: value,
            onChanged: onChanged,
            onChangeStart: onChangeStart,
            onChangeEnd: onChangeEnd,
            min: min,
            max: max,
            divisions: divisions,
            secondaryTrackValue: secondaryTrackValue,
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            secondaryActiveColor: secondaryActiveColor,
            thumbColor: thumbColor,
            overlayColor: overlayColor,
            focusNode: focusNode,
            autofocus: autofocus,
            semanticFormatterCallback: semanticFormatterCallback,
            key: key,
            label: label,
            mouseCursor: mouseCursor,
            allowedInteraction: allowedInteraction,
            showValueIndicator: showValueIndicator,
            year2023: year2023)
        {
            IsAdaptive = true,
        };
    }

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
            if (CurrentWidget.IsAdaptive && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS)
            {
                return new SizedBox(
                    width: double.PositiveInfinity,
                    child: new CupertinoSlider(
                        value: CurrentWidget.Value,
                        onChanged: CurrentWidget.OnChanged,
                        onChangeStart: CurrentWidget.OnChangeStart,
                        onChangeEnd: CurrentWidget.OnChangeEnd,
                        min: CurrentWidget.Min,
                        max: CurrentWidget.Max,
                        divisions: CurrentWidget.Divisions,
                        activeColor: CurrentWidget.ActiveColor is { } adaptiveActive ? adaptiveActive : null,
                        thumbColor: CurrentWidget.ThumbColor is { } adaptiveThumb
                            ? adaptiveThumb
                            : CupertinoColors.White));
            }

            var sliderTheme = SliderTheme.Of(context);
            bool year2023 = !theme.UseMaterial3 || (CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            double trackHeight = ResolveTrackHeight(sliderTheme, theme, year2023);
            double thumbRadius = ResolveThumbRadius(sliderTheme);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? sliderTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            double minPreferredHeight = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? Math.Max(PaddedTapTargetExtent, thumbRadius * 2)
                : Math.Max(trackHeight, thumbRadius * 2);
            double overlayRadius = sliderTheme.OverlayRadius
                                   ?? Math.Max(thumbRadius, theme.UseMaterial3 ? 20.0 : 16.0);
            TextDirection textDirection = Directionality.Of(context);
            EdgeInsetsGeometry? paddingGeometry = CurrentWidget.Padding ?? sliderTheme.Padding;
            Thickness padding = paddingGeometry?.Resolve(textDirection) ?? new Thickness();
            var allowedInteraction = CurrentWidget.AllowedInteraction
                                     ?? sliderTheme.AllowedInteraction
                                     ?? SliderInteraction.TapAndSlide;
            var showValueIndicator = CurrentWidget.ShowValueIndicator
                                     ?? sliderTheme.ShowValueIndicator
                                     ?? Plumix.Material.ShowValueIndicator.OnlyForDiscrete;
            double tickMarkRadius = sliderTheme.TickMarkRadius ?? Math.Max(1.0, trackHeight / 4.0);
            double trackGap = year2023 ? 0.0 : sliderTheme.TrackGap ?? 6.0;
            var thumbStates = BuildStates(interactive: IsInteractive);
            Size thumbSize = sliderTheme.ThumbSize?.Resolve(thumbStates)
                             ?? (year2023
                                 ? new Size(thumbRadius * 2.0, thumbRadius * 2.0)
                                 : new Size(4.0, 44.0));
            minPreferredHeight = Math.Max(minPreferredHeight, thumbSize.Height);

            var activeTrackColor = ResolveActiveTrackColor(theme, sliderTheme);
            var inactiveTrackColor = ResolveInactiveTrackColor(theme, sliderTheme);
            var secondaryTrackColor = ResolveSecondaryTrackColor(theme, sliderTheme);
            var thumbColor = ResolveThumbColor(theme, sliderTheme);
            var disabledActiveTrackColor = ResolveDisabledActiveTrackColor(theme, sliderTheme);
            var disabledInactiveTrackColor = ResolveDisabledInactiveTrackColor(theme, sliderTheme);
            var disabledSecondaryTrackColor = ResolveDisabledSecondaryTrackColor(theme, sliderTheme);
            var disabledThumbColor = ResolveDisabledThumbColor(theme, sliderTheme);
            var activeTickMarkColor = ResolveActiveTickMarkColor(theme, sliderTheme, year2023);
            var inactiveTickMarkColor = ResolveInactiveTickMarkColor(theme, sliderTheme, year2023);
            var valueIndicatorColor = sliderTheme.ValueIndicatorColor
                                      ?? (theme.UseMaterial3 && !year2023
                                          ? theme.ColorScheme.InverseSurface
                                          : theme.ColorScheme.Primary);
            var valueIndicatorTextStyle = sliderTheme.ValueIndicatorTextStyle
                                          ?? (theme.UseMaterial3 && !year2023
                                              ? theme.TextTheme.LabelLarge.CopyWith(
                                                  color: theme.ColorScheme.OnInverseSurface)
                                              : theme.TextTheme.BodyLarge.CopyWith(
                                                  color: theme.ColorScheme.OnPrimary));
            double? secondaryTrackValueNormalized = NormalizeOptional(CurrentWidget.SecondaryTrackValue);

            var focusedStates = BuildStates(interactive: IsInteractive, focused: true);
            var hoveredStates = BuildStates(interactive: IsInteractive, hovered: true);
            var draggedStates = BuildStates(interactive: IsInteractive, dragged: true);
            var overlayFocusedColor = ResolveOverlayColor(theme, sliderTheme, focusedStates);
            var overlayHoveredColor = ResolveOverlayColor(theme, sliderTheme, hoveredStates);
            var overlayDraggedColor = ResolveOverlayColor(theme, sliderTheme, draggedStates);
            SliderComponentShape valueIndicatorShape = sliderTheme.ValueIndicatorShape
                                                       ?? (theme.UseMaterial3
                                                           ? year2023
                                                               ? new DropSliderValueIndicatorShape()
                                                               : new RoundedRectSliderValueIndicatorShape()
                                                           : new RectangularSliderValueIndicatorShape());
            if (valueIndicatorShape is RectangularSliderValueIndicatorShape
                && !sliderTheme.ValueIndicatorColor.HasValue)
            {
                valueIndicatorColor = AlphaBlend(
                    MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.60),
                    MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Surface, 0.90));
            }

            var effectiveSliderTheme = new SliderThemeData(
                TrackHeight: trackHeight,
                ActiveTrackColor: activeTrackColor,
                InactiveTrackColor: inactiveTrackColor,
                SecondaryActiveTrackColor: secondaryTrackColor,
                DisabledActiveTrackColor: disabledActiveTrackColor,
                DisabledInactiveTrackColor: disabledInactiveTrackColor,
                DisabledSecondaryActiveTrackColor: disabledSecondaryTrackColor,
                ActiveTickMarkColor: activeTickMarkColor,
                InactiveTickMarkColor: inactiveTickMarkColor,
                DisabledActiveTickMarkColor: activeTickMarkColor,
                DisabledInactiveTickMarkColor: inactiveTickMarkColor,
                ThumbColor: thumbColor,
                DisabledThumbColor: disabledThumbColor,
                OverlayColor: WidgetStateProperty<Color?>.All(overlayDraggedColor),
                ValueIndicatorColor: valueIndicatorColor,
                ValueIndicatorStrokeColor: sliderTheme.ValueIndicatorStrokeColor,
                OverlayShape: sliderTheme.OverlayShape ?? new RoundSliderOverlayShape(overlayRadius),
                TickMarkShape: sliderTheme.TickMarkShape ?? new RoundSliderTickMarkShape(tickMarkRadius),
                ThumbShape: sliderTheme.ThumbShape
                            ?? (year2023
                                ? new RoundSliderThumbShape(thumbRadius)
                                : new HandleThumbShape()),
                TrackShape: sliderTheme.TrackShape
                            ?? (year2023
                                ? new RoundedRectSliderTrackShape()
                                : new GappedSliderTrackShape()),
                ValueIndicatorShape: valueIndicatorShape,
                ShowValueIndicator: showValueIndicator,
                ValueIndicatorTextStyle: valueIndicatorTextStyle,
                MouseCursor: sliderTheme.MouseCursor,
                AllowedInteraction: allowedInteraction,
                Padding: paddingGeometry,
                ThumbSize: sliderTheme.ThumbSize ?? WidgetStateProperty<Size?>.All(thumbSize),
                TrackGap: trackGap,
                Year2023: year2023);
            string? semanticsLabel = ResolveSemanticsLabel();

            var semanticsFlags = SemanticsFlags.IsSlider;
            if (IsInteractive)
            {
                semanticsFlags |= SemanticsFlags.IsEnabled;
            }

            Widget result = new Semantics(
                label: semanticsLabel,
                flags: semanticsFlags,
                child: new Focus(
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    canRequestFocus: IsInteractive,
                    onKeyEvent: HandleKeyEvent,
                    child: new SliderRenderWidget(
                        sliderTheme: effectiveSliderTheme,
                        valueNormalized: Normalize(CurrentWidget.Value),
                        secondaryTrackValueNormalized: secondaryTrackValueNormalized,
                        divisions: CurrentWidget.Divisions,
                        isInteractive: IsInteractive,
                        isFocused: _hasFocus,
                        trackHeight: trackHeight,
                        thumbRadius: thumbRadius,
                        thumbSize: thumbSize,
                        overlayRadius: overlayRadius,
                        minPreferredHeight: minPreferredHeight,
                        activeTrackColor: IsInteractive ? activeTrackColor : disabledActiveTrackColor,
                        inactiveTrackColor: IsInteractive ? inactiveTrackColor : disabledInactiveTrackColor,
                        secondaryActiveTrackColor: IsInteractive ? secondaryTrackColor : disabledSecondaryTrackColor,
                        thumbColor: IsInteractive ? thumbColor : disabledThumbColor,
                        overlayFocusedColor: overlayFocusedColor,
                        overlayHoveredColor: overlayHoveredColor,
                        overlayDraggedColor: overlayDraggedColor,
                        activeTickMarkColor: activeTickMarkColor,
                        inactiveTickMarkColor: inactiveTickMarkColor,
                        tickMarkRadius: tickMarkRadius,
                        label: CurrentWidget.Label,
                        showValueIndicator: showValueIndicator,
                        valueIndicatorColor: valueIndicatorColor,
                        valueIndicatorTextStyle: valueIndicatorTextStyle,
                        padding: padding,
                        allowedInteraction: allowedInteraction,
                        trackGap: trackGap,
                        textDirection: Directionality.Of(context),
                        onChangeStartNormalized: IsInteractive ? HandleChangeStartNormalized : null,
                        onChangedNormalized: IsInteractive ? HandleChangedNormalized : null,
                        onChangeEndNormalized: IsInteractive ? HandleChangeEndNormalized : null)));

            var cursorStates = BuildStates(interactive: IsInteractive, focused: _hasFocus);
            MouseCursor cursor = CurrentWidget.MouseCursor
                                 ?? sliderTheme.MouseCursor?.Resolve(cursorStates)
                                 ?? (IsInteractive ? SystemMouseCursors.Click : SystemMouseCursors.Basic);
            return new MouseRegion(cursor: cursor, child: result);
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
            if (!IsSupportedKeyboardKey(@event.LogicalKey))
            {
                return KeyEventResult.Ignored;
            }

            if (!IsInteractive || @event is not KeyDownEvent || HasModifier(@event))
            {
                return KeyEventResult.Handled;
            }

            double normalized = Normalize(CurrentWidget.Value);
            double next = ResolveKeyboardTargetNormalized(normalized, @event.LogicalKey);
            if (Math.Abs(next - normalized) <= 0.0001)
            {
                return KeyEventResult.Handled;
            }

            CurrentWidget.OnChangeStart?.Invoke(CurrentWidget.Value);
            CurrentWidget.OnChanged?.Invoke(Denormalize(next));
            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(next));
            return KeyEventResult.Handled;
        }

        private double ResolveKeyboardTargetNormalized(double currentNormalized, LogicalKeyboardKey key)
        {
            if (key.Equals(LogicalKeyboardKey.Home))
            {
                return 0.0;
            }

            if (key.Equals(LogicalKeyboardKey.End))
            {
                return 1.0;
            }

            double step = ResolveAdjustmentUnit(Theme.Of(Context));
            var direction = Directionality.Of(Context);
            double delta = 0.0;
            if (key.Equals(LogicalKeyboardKey.ArrowRight))
            {
                delta = direction == TextDirection.Rtl ? -step : step;
            }
            else if (key.Equals(LogicalKeyboardKey.ArrowLeft))
            {
                delta = direction == TextDirection.Rtl ? step : -step;
            }
            else if (key.Equals(LogicalKeyboardKey.ArrowUp)
                     || key.Equals(LogicalKeyboardKey.PageUp))
            {
                delta = step;
            }
            else if (key.Equals(LogicalKeyboardKey.ArrowDown)
                     || key.Equals(LogicalKeyboardKey.PageDown))
            {
                delta = -step;
            }

            double next = Math.Clamp(currentNormalized + delta, 0.0, 1.0);
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
            return HardwareKeyboard.Instance.IsShiftPressed
                   || HardwareKeyboard.Instance.IsControlPressed
                   || HardwareKeyboard.Instance.IsAltPressed
                   || HardwareKeyboard.Instance.IsMetaPressed;
        }

        private static bool IsSupportedKeyboardKey(LogicalKeyboardKey key)
        {
            return key.Equals(LogicalKeyboardKey.ArrowLeft)
                   || key.Equals(LogicalKeyboardKey.ArrowRight)
                   || key.Equals(LogicalKeyboardKey.ArrowUp)
                   || key.Equals(LogicalKeyboardKey.ArrowDown)
                   || key.Equals(LogicalKeyboardKey.PageUp)
                   || key.Equals(LogicalKeyboardKey.PageDown)
                   || key.Equals(LogicalKeyboardKey.Home)
                   || key.Equals(LogicalKeyboardKey.End);
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

            double nextValue = Denormalize(SnapNormalized(normalized));
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

        private string? ResolveSemanticsLabel()
        {
            var formatter = CurrentWidget.SemanticFormatterCallback;
            if (formatter is not null)
            {
                return formatter(CurrentWidget.Value);
            }

            return CurrentWidget.SemanticLabel;
        }

        private double Normalize(double value)
        {
            double range = CurrentWidget.Max - CurrentWidget.Min;
            if (range <= 0)
            {
                return 0.0;
            }

            return Math.Clamp((value - CurrentWidget.Min) / range, 0.0, 1.0);
        }

        private double Denormalize(double normalized)
        {
            double clamped = Math.Clamp(normalized, 0.0, 1.0);
            return CurrentWidget.Min + ((CurrentWidget.Max - CurrentWidget.Min) * clamped);
        }

        private double? NormalizeOptional(double? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return Normalize(value.Value);
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

        private static double ResolveTrackHeight(
            SliderThemeData sliderTheme,
            ThemeData theme,
            bool year2023)
        {
            double defaultHeight = theme.UseMaterial3 && !year2023 ? 16.0 : DefaultTrackHeight;
            double resolved = sliderTheme.TrackHeight ?? defaultHeight;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return defaultHeight;
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
                   ?? theme.ColorScheme.Primary;
        }

        private Color ResolveInactiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            bool year2023 = !theme.UseMaterial3 || (CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            return CurrentWidget.InactiveColor
                   ?? sliderTheme.InactiveTrackColor
                   ?? (theme.UseMaterial3
                       ? year2023
                           ? theme.ColorScheme.SurfaceContainerHighest
                           : theme.ColorScheme.SecondaryContainer
                       : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Primary, 0.24));
        }

        private Color ResolveThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.ThumbColor
                   ?? CurrentWidget.ActiveColor
                   ?? sliderTheme.ThumbColor
                   ?? theme.ColorScheme.Primary;
        }

        private Color ResolveSecondaryTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.SecondaryActiveColor
                   ?? sliderTheme.SecondaryActiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Primary, 0.54);
        }

        private Color ResolveDisabledActiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledActiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(
                       theme.ColorScheme.OnSurface,
                       theme.UseMaterial3 ? 0.38 : 0.32);
        }

        private Color ResolveDisabledInactiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledInactiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.12);
        }

        private Color ResolveDisabledSecondaryTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            bool useLatest = theme.UseMaterial3
                             && !(CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            return sliderTheme.DisabledSecondaryActiveTrackColor
                   ?? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, useLatest ? 0.38 : 0.12);
        }

        private Color ResolveDisabledThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            bool useLatest = theme.UseMaterial3
                             && !(CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            return sliderTheme.DisabledThumbColor
                   ?? (useLatest
                       ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38)
                       : AlphaBlend(
                           MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38),
                           theme.ColorScheme.Surface));
        }

        private Color ResolveActiveTickMarkColor(
            ThemeData theme,
            SliderThemeData sliderTheme,
            bool year2023)
        {
            Color fallback = theme.UseMaterial3
                ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimary, year2023 ? 0.38 : 1.0)
                : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimary, 0.54);
            return IsInteractive
                ? sliderTheme.ActiveTickMarkColor ?? fallback
                : sliderTheme.DisabledActiveTickMarkColor
                  ?? (theme.UseMaterial3
                      ? year2023
                          ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38)
                          : theme.ColorScheme.OnInverseSurface
                      : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimary, 0.12));
        }

        private Color ResolveInactiveTickMarkColor(
            ThemeData theme,
            SliderThemeData sliderTheme,
            bool year2023)
        {
            Color fallback = theme.UseMaterial3 && !year2023
                ? theme.ColorScheme.OnSecondaryContainer
                : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Primary, 0.54);
            return IsInteractive
                ? sliderTheme.InactiveTickMarkColor ?? fallback
                : sliderTheme.DisabledInactiveTickMarkColor
                  ?? (theme.UseMaterial3 && !year2023
                      ? theme.ColorScheme.OnSurface
                      : MaterialButtonCore.ApplyOpacity(
                          theme.ColorScheme.OnSurface,
                          theme.UseMaterial3 ? 0.38 : 0.12));
        }

        private Color? ResolveOverlayColor(
            ThemeData theme,
            SliderThemeData sliderTheme,
            IReadOnlySet<WidgetState> states)
        {
            var widgetOverlay = CurrentWidget.OverlayColor?.Resolve(states);
            if (widgetOverlay.HasValue)
            {
                return widgetOverlay.Value;
            }

            if (CurrentWidget.ActiveColor.HasValue)
            {
                return states.Contains(WidgetState.Disabled)
                    ? null
                    : MaterialButtonCore.ApplyOpacity(CurrentWidget.ActiveColor.Value, 0.12);
            }

            var themeOverlay = sliderTheme.OverlayColor?.Resolve(states);
            if (themeOverlay.HasValue)
            {
                return themeOverlay.Value;
            }

            Color baseColor = theme.ColorScheme.Primary;

            if (!theme.UseMaterial3)
            {
                return states.Contains(WidgetState.Disabled)
                    ? null
                    : MaterialButtonCore.ApplyOpacity(baseColor, 0.12);
            }

            if (states.Contains(WidgetState.Dragged))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.10);
            }

            if (states.Contains(WidgetState.Hovered))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.08);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return MaterialButtonCore.ApplyOpacity(baseColor, 0.10);
            }

            return null;
        }

        private static Color AlphaBlend(Color foreground, Color background)
        {
            double alpha = foreground.A / 255.0;
            double backgroundAlpha = background.A / 255.0;
            double outputAlpha = alpha + (backgroundAlpha * (1.0 - alpha));
            if (outputAlpha <= 0.0)
            {
                return Colors.Transparent;
            }

            byte a = (byte)Math.Round(outputAlpha * 255.0);
            byte r = (byte)Math.Round(
                ((foreground.R * alpha) + (background.R * backgroundAlpha * (1.0 - alpha))) / outputAlpha);
            byte g = (byte)Math.Round(
                ((foreground.G * alpha) + (background.G * backgroundAlpha * (1.0 - alpha))) / outputAlpha);
            byte b = (byte)Math.Round(
                ((foreground.B * alpha) + (background.B * backgroundAlpha * (1.0 - alpha))) / outputAlpha);
            return Color.FromArgb(a, r, g, b);
        }

        private static IReadOnlySet<WidgetState> BuildStates(
            bool interactive,
            bool focused = false,
            bool hovered = false,
            bool dragged = false)
        {
            var states = new HashSet<WidgetState>();
            if (!interactive)
            {
                states.Add(WidgetState.Disabled);
            }
            if (focused)
            {
                states.Add(WidgetState.Focused);
            }

            if (hovered)
            {
                states.Add(WidgetState.Hovered);
            }

            if (dragged)
            {
                states.Add(WidgetState.Dragged);
            }

            return states;
        }
    }
}

internal sealed class SliderRenderWidget : LeafRenderObjectWidget
{
    public SliderRenderWidget(
        SliderThemeData sliderTheme,
        double valueNormalized,
        double? secondaryTrackValueNormalized,
        int? divisions,
        bool isInteractive,
        bool isFocused,
        double trackHeight,
        double thumbRadius,
        Size thumbSize,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color secondaryActiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        Color activeTickMarkColor,
        Color inactiveTickMarkColor,
        double tickMarkRadius,
        string? label,
        ShowValueIndicator showValueIndicator,
        Color valueIndicatorColor,
        TextStyle valueIndicatorTextStyle,
        Thickness padding,
        SliderInteraction allowedInteraction,
        double trackGap,
        TextDirection textDirection,
        Action<double>? onChangeStartNormalized,
        Action<double>? onChangedNormalized,
        Action<double>? onChangeEndNormalized,
        Key? key = null) : base(key)
    {
        SliderTheme = sliderTheme;
        ValueNormalized = valueNormalized;
        SecondaryTrackValueNormalized = secondaryTrackValueNormalized;
        Divisions = divisions;
        IsInteractive = isInteractive;
        IsFocused = isFocused;
        TrackHeight = trackHeight;
        ThumbRadius = thumbRadius;
        ThumbSize = thumbSize;
        OverlayRadius = overlayRadius;
        MinPreferredHeight = minPreferredHeight;
        ActiveTrackColor = activeTrackColor;
        InactiveTrackColor = inactiveTrackColor;
        SecondaryActiveTrackColor = secondaryActiveTrackColor;
        ThumbColor = thumbColor;
        OverlayFocusedColor = overlayFocusedColor;
        OverlayHoveredColor = overlayHoveredColor;
        OverlayDraggedColor = overlayDraggedColor;
        ActiveTickMarkColor = activeTickMarkColor;
        InactiveTickMarkColor = inactiveTickMarkColor;
        TickMarkRadius = tickMarkRadius;
        Label = label;
        ShowValueIndicator = showValueIndicator;
        ValueIndicatorColor = valueIndicatorColor;
        ValueIndicatorTextStyle = valueIndicatorTextStyle;
        Padding = padding;
        AllowedInteraction = allowedInteraction;
        TrackGap = trackGap;
        TextDirection = textDirection;
        OnChangeStartNormalized = onChangeStartNormalized;
        OnChangedNormalized = onChangedNormalized;
        OnChangeEndNormalized = onChangeEndNormalized;
    }

    public SliderThemeData SliderTheme { get; }

    public double ValueNormalized { get; }

    public double? SecondaryTrackValueNormalized { get; }

    public int? Divisions { get; }

    public bool IsInteractive { get; }

    public bool IsFocused { get; }

    public double TrackHeight { get; }

    public double ThumbRadius { get; }

    public Size ThumbSize { get; }

    public double OverlayRadius { get; }

    public double MinPreferredHeight { get; }

    public Color ActiveTrackColor { get; }

    public Color InactiveTrackColor { get; }

    public Color SecondaryActiveTrackColor { get; }

    public Color ThumbColor { get; }

    public Color? OverlayFocusedColor { get; }

    public Color? OverlayHoveredColor { get; }

    public Color? OverlayDraggedColor { get; }

    public Color ActiveTickMarkColor { get; }

    public Color InactiveTickMarkColor { get; }

    public double TickMarkRadius { get; }

    public string? Label { get; }

    public ShowValueIndicator ShowValueIndicator { get; }

    public Color ValueIndicatorColor { get; }

    public TextStyle ValueIndicatorTextStyle { get; }

    public Thickness Padding { get; }

    public SliderInteraction AllowedInteraction { get; }

    public double TrackGap { get; }

    public TextDirection TextDirection { get; }

    public Action<double>? OnChangeStartNormalized { get; }

    public Action<double>? OnChangedNormalized { get; }

    public Action<double>? OnChangeEndNormalized { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSlider(
            sliderTheme: SliderTheme,
            valueNormalized: ValueNormalized,
            secondaryTrackValueNormalized: SecondaryTrackValueNormalized,
            divisions: Divisions,
            isInteractive: IsInteractive,
            isFocused: IsFocused,
            trackHeight: TrackHeight,
            thumbRadius: ThumbRadius,
            thumbSize: ThumbSize,
            overlayRadius: OverlayRadius,
            minPreferredHeight: MinPreferredHeight,
            activeTrackColor: ActiveTrackColor,
            inactiveTrackColor: InactiveTrackColor,
            secondaryActiveTrackColor: SecondaryActiveTrackColor,
            thumbColor: ThumbColor,
            overlayFocusedColor: OverlayFocusedColor,
            overlayHoveredColor: OverlayHoveredColor,
            overlayDraggedColor: OverlayDraggedColor,
            activeTickMarkColor: ActiveTickMarkColor,
            inactiveTickMarkColor: InactiveTickMarkColor,
            tickMarkRadius: TickMarkRadius,
            label: Label,
            showValueIndicator: ShowValueIndicator,
            valueIndicatorColor: ValueIndicatorColor,
            valueIndicatorTextStyle: ValueIndicatorTextStyle,
            padding: Padding,
            allowedInteraction: AllowedInteraction,
            trackGap: TrackGap,
            textDirection: TextDirection,
            onChangeStartNormalized: OnChangeStartNormalized,
            onChangedNormalized: OnChangedNormalized,
            onChangeEndNormalized: OnChangeEndNormalized);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var slider = (RenderSlider)renderObject;
        slider.SliderTheme = SliderTheme;
        slider.ValueNormalized = ValueNormalized;
        slider.SecondaryTrackValueNormalized = SecondaryTrackValueNormalized;
        slider.Divisions = Divisions;
        slider.IsInteractive = IsInteractive;
        slider.IsFocused = IsFocused;
        slider.TrackHeight = TrackHeight;
        slider.ThumbRadius = ThumbRadius;
        slider.ThumbSize = ThumbSize;
        slider.OverlayRadius = OverlayRadius;
        slider.MinPreferredHeight = MinPreferredHeight;
        slider.ActiveTrackColor = ActiveTrackColor;
        slider.InactiveTrackColor = InactiveTrackColor;
        slider.SecondaryActiveTrackColor = SecondaryActiveTrackColor;
        slider.ThumbColor = ThumbColor;
        slider.OverlayFocusedColor = OverlayFocusedColor;
        slider.OverlayHoveredColor = OverlayHoveredColor;
        slider.OverlayDraggedColor = OverlayDraggedColor;
        slider.ActiveTickMarkColor = ActiveTickMarkColor;
        slider.InactiveTickMarkColor = InactiveTickMarkColor;
        slider.TickMarkRadius = TickMarkRadius;
        slider.Label = Label;
        slider.ShowValueIndicator = ShowValueIndicator;
        slider.ValueIndicatorColor = ValueIndicatorColor;
        slider.ValueIndicatorTextStyle = ValueIndicatorTextStyle;
        slider.Padding = Padding;
        slider.AllowedInteraction = AllowedInteraction;
        slider.TrackGap = TrackGap;
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
    private SliderThemeData _sliderTheme;
    private double? _secondaryTrackValueNormalized;
    private int? _divisions;
    private bool _isInteractive;
    private bool _isFocused;
    private double _trackHeight;
    private double _thumbRadius;
    private Size _thumbSize;
    private double _overlayRadius;
    private double _minPreferredHeight;
    private Color _activeTrackColor;
    private Color _inactiveTrackColor;
    private Color _secondaryActiveTrackColor;
    private Color _thumbColor;
    private Color? _overlayFocusedColor;
    private Color? _overlayHoveredColor;
    private Color? _overlayDraggedColor;
    private Color _activeTickMarkColor;
    private Color _inactiveTickMarkColor;
    private double _tickMarkRadius;
    private string? _label;
    private ShowValueIndicator _showValueIndicator;
    private Color _valueIndicatorColor;
    private TextStyle _valueIndicatorTextStyle;
    private Thickness _padding;
    private SliderInteraction _allowedInteraction;
    private double _trackGap;
    private TextDirection _textDirection;
    private Action<double>? _onChangeStartNormalized;
    private Action<double>? _onChangedNormalized;
    private Action<double>? _onChangeEndNormalized;

    private bool _hovered;
    private bool _dragging;
    private int? _activePointer;
    private double? _dragValueNormalized;

    public RenderSlider(
        SliderThemeData sliderTheme,
        double valueNormalized,
        double? secondaryTrackValueNormalized,
        int? divisions,
        bool isInteractive,
        bool isFocused,
        double trackHeight,
        double thumbRadius,
        Size thumbSize,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color secondaryActiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        Color activeTickMarkColor,
        Color inactiveTickMarkColor,
        double tickMarkRadius,
        string? label,
        ShowValueIndicator showValueIndicator,
        Color valueIndicatorColor,
        TextStyle valueIndicatorTextStyle,
        Thickness padding,
        SliderInteraction allowedInteraction,
        double trackGap,
        TextDirection textDirection,
        Action<double>? onChangeStartNormalized,
        Action<double>? onChangedNormalized,
        Action<double>? onChangeEndNormalized)
    {
        _sliderTheme = sliderTheme;
        _valueNormalized = ClampNormalized(valueNormalized);
        _secondaryTrackValueNormalized = ClampNormalizedNullable(secondaryTrackValueNormalized);
        _divisions = divisions;
        _isInteractive = isInteractive;
        _isFocused = isFocused;
        _trackHeight = trackHeight;
        _thumbRadius = thumbRadius;
        _thumbSize = thumbSize;
        _overlayRadius = overlayRadius;
        _minPreferredHeight = minPreferredHeight;
        _activeTrackColor = activeTrackColor;
        _inactiveTrackColor = inactiveTrackColor;
        _secondaryActiveTrackColor = secondaryActiveTrackColor;
        _thumbColor = thumbColor;
        _overlayFocusedColor = overlayFocusedColor;
        _overlayHoveredColor = overlayHoveredColor;
        _overlayDraggedColor = overlayDraggedColor;
        _activeTickMarkColor = activeTickMarkColor;
        _inactiveTickMarkColor = inactiveTickMarkColor;
        _tickMarkRadius = tickMarkRadius;
        _label = label;
        _showValueIndicator = showValueIndicator;
        _valueIndicatorColor = valueIndicatorColor;
        _valueIndicatorTextStyle = valueIndicatorTextStyle;
        _padding = padding;
        _allowedInteraction = allowedInteraction;
        _trackGap = trackGap;
        _textDirection = textDirection;
        _onChangeStartNormalized = onChangeStartNormalized;
        _onChangedNormalized = onChangedNormalized;
        _onChangeEndNormalized = onChangeEndNormalized;
    }

    public SliderThemeData SliderTheme
    {
        get => _sliderTheme;
        set
        {
            if (Equals(_sliderTheme, value))
            {
                return;
            }

            _sliderTheme = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double ValueNormalized
    {
        get => _valueNormalized;
        set
        {
            double normalized = ClampNormalized(value);
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

    public double? SecondaryTrackValueNormalized
    {
        get => _secondaryTrackValueNormalized;
        set
        {
            double? normalized = ClampNormalizedNullable(value);
            if (_secondaryTrackValueNormalized.HasValue == normalized.HasValue
                && (!_secondaryTrackValueNormalized.HasValue
                    || Math.Abs(_secondaryTrackValueNormalized.Value - normalized!.Value) <= Epsilon))
            {
                return;
            }

            _secondaryTrackValueNormalized = normalized;
            MarkNeedsPaint();
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

    public Size ThumbSize
    {
        get => _thumbSize;
        set
        {
            if (_thumbSize == value) return;
            _thumbSize = value;
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

    public Color SecondaryActiveTrackColor
    {
        get => _secondaryActiveTrackColor;
        set
        {
            if (_secondaryActiveTrackColor == value)
            {
                return;
            }

            _secondaryActiveTrackColor = value;
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

    public Color ActiveTickMarkColor
    {
        get => _activeTickMarkColor;
        set { if (_activeTickMarkColor != value) { _activeTickMarkColor = value; MarkNeedsPaint(); } }
    }

    public Color InactiveTickMarkColor
    {
        get => _inactiveTickMarkColor;
        set { if (_inactiveTickMarkColor != value) { _inactiveTickMarkColor = value; MarkNeedsPaint(); } }
    }

    public double TickMarkRadius
    {
        get => _tickMarkRadius;
        set { if (Math.Abs(_tickMarkRadius - value) > Epsilon) { _tickMarkRadius = value; MarkNeedsPaint(); } }
    }

    public string? Label
    {
        get => _label;
        set { if (_label != value) { _label = value; MarkNeedsPaint(); } }
    }

    public ShowValueIndicator ShowValueIndicator
    {
        get => _showValueIndicator;
        set { if (_showValueIndicator != value) { _showValueIndicator = value; MarkNeedsPaint(); } }
    }

    public Color ValueIndicatorColor
    {
        get => _valueIndicatorColor;
        set { if (_valueIndicatorColor != value) { _valueIndicatorColor = value; MarkNeedsPaint(); } }
    }

    public TextStyle ValueIndicatorTextStyle
    {
        get => _valueIndicatorTextStyle;
        set { if (!Equals(_valueIndicatorTextStyle, value)) { _valueIndicatorTextStyle = value; MarkNeedsPaint(); } }
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value) return;
            _padding = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public SliderInteraction AllowedInteraction
    {
        get => _allowedInteraction;
        set => _allowedInteraction = value;
    }

    public double TrackGap
    {
        get => _trackGap;
        set { if (Math.Abs(_trackGap - value) > Epsilon) { _trackGap = value; MarkNeedsPaint(); } }
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

    protected override double ComputeMinIntrinsicWidth(double height) => PreferredWidth;

    protected override double ComputeMaxIntrinsicWidth(double height) => PreferredWidth;

    protected override double ComputeMinIntrinsicHeight(double width) => PreferredHeight;

    protected override double ComputeMaxIntrinsicHeight(double width) => PreferredHeight;

    private double PreferredWidth => DefaultTrackWidth + MaxSliderPartSize.Width;

    private double PreferredHeight => Math.Max(TrackHeight, MaxSliderPartSize.Height);

    private Size MaxSliderPartSize
    {
        get
        {
            bool discrete = Divisions.HasValue;
            Size overlay = SliderTheme.OverlayShape!.GetPreferredSize(IsInteractive, discrete);
            Size thumb = SliderTheme.ThumbShape!.GetPreferredSize(IsInteractive, discrete);
            Size tick = SliderTheme.TickMarkShape!.GetPreferredSize(SliderTheme, IsInteractive);
            double overlayHeight = SliderTheme.Padding.HasValue ? thumb.Height : overlay.Height;
            return new Size(
                Math.Max(overlay.Width, Math.Max(thumb.Width, tick.Width)),
                Math.Max(overlayHeight, Math.Max(thumb.Height, tick.Height)));
        }
    }

    protected override void PerformLayout()
    {
        double desiredWidth = Constraints.HasBoundedWidth ? Constraints.MaxWidth : PreferredWidth;
        if (!double.IsFinite(desiredWidth) || desiredWidth <= 0)
        {
            desiredWidth = DefaultTrackWidth;
        }

        double contentHeight = Math.Max(MinPreferredHeight, PreferredHeight);
        double desiredHeight = contentHeight + Padding.Top + Padding.Bottom;
        if (!double.IsFinite(desiredHeight) || desiredHeight <= 0)
        {
            desiredHeight = Math.Max(TrackHeight, ThumbSize.Height);
        }

        Size = Constraints.Constrain(new Size(desiredWidth, desiredHeight));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        double visualValue = ResolveVisualValue();
        SliderTrackShape trackShape = SliderTheme.TrackShape!;
        bool discrete = Divisions.HasValue;
        Rect trackRect = trackShape.GetPreferredRect(this, offset, SliderTheme, IsInteractive, discrete);
        var geometry = new TrackGeometry(trackRect.Left, trackRect.Right);
        double thumbCenterX = ResolveThumbCenterX(geometry, visualValue);
        var thumbCenter = new Point(thumbCenterX, trackRect.Center.Y);
        Point? secondaryOffset = null;
        if (ShouldShowSecondaryTrack(visualValue))
        {
            double secondaryVisualValue = TextDirection == TextDirection.Rtl
                ? 1.0 - SecondaryTrackValueNormalized!.Value
                : SecondaryTrackValueNormalized!.Value;
            secondaryOffset = new Point(
                ResolveThumbCenterX(geometry, secondaryVisualValue),
                trackRect.Center.Y);
        }

        var enableAnimation = new ConstantAnimation<double>(IsInteractive ? 1.0 : 0.0);
        bool active = _dragging || _hovered || IsFocused;
        var activationAnimation = new ConstantAnimation<double>(active ? 1.0 : 0.0);
        trackShape.Paint(
            ctx,
            offset,
            thumbCenter,
            secondaryOffset,
            enableAnimation,
            discrete,
            IsInteractive,
            this,
            SliderTheme,
            TextDirection);

        Color? overlayColor = ResolveOverlayColor();
        SliderThemeData paintTheme = SliderTheme.CopyWith(
            overlayColor: WidgetStateProperty<Color?>.All(overlayColor));
        if (active && overlayColor is { A: > 0 })
        {
            paintTheme.OverlayShape!.Paint(
                ctx,
                thumbCenter,
                activationAnimation,
                enableAnimation,
                discrete,
                null,
                this,
                paintTheme,
                TextDirection,
                ValueNormalized,
                1.0,
                Size);
        }

        PaintTickMarks(ctx, geometry, trackRect.Center.Y, visualValue);
        TextLayout? labelLayout = CreateLabelLayout(Label);
        if (ShouldShowValueIndicator() && labelLayout is not null)
        {
            paintTheme.ValueIndicatorShape!.Paint(
                ctx,
                thumbCenter,
                new ConstantAnimation<double>(1.0),
                enableAnimation,
                discrete,
                labelLayout,
                this,
                paintTheme,
                TextDirection,
                ValueNormalized,
                1.0,
                Size);
        }

        paintTheme.ThumbShape!.Paint(
            ctx,
            thumbCenter,
            activationAnimation,
            enableAnimation,
            discrete,
            labelLayout,
            this,
            paintTheme,
            TextDirection,
            ValueNormalized,
            1.0,
            Size);
    }

    private void PaintTrackSegment(
        PaintingContext context,
        double start,
        double end,
        double centerY,
        Color color)
    {
        double left = start;
        double width = end - start;
        if (width <= Epsilon)
        {
            return;
        }

        context.DrawRectangle(
            brush: new SolidColorBrush(color),
            pen: null,
            rect: new Rect(left, centerY - (TrackHeight / 2.0), width, TrackHeight),
            radiusX: TrackHeight / 2.0,
            radiusY: TrackHeight / 2.0);
    }

    private void PaintThumb(PaintingContext context, Point center)
    {
        var thumbRect = new Rect(
            center.X - (ThumbSize.Width / 2.0),
            center.Y - (ThumbSize.Height / 2.0),
            ThumbSize.Width,
            ThumbSize.Height);
        double radius = Math.Min(ThumbSize.Width, ThumbSize.Height) / 2.0;
        context.DrawRectangle(
            brush: new SolidColorBrush(ThumbColor),
            pen: null,
            rect: thumbRect,
            radiusX: radius,
            radiusY: radius);
    }

    private void PaintTickMarks(PaintingContext context, TrackGeometry geometry, double centerY, double visualValue)
    {
        if (!Divisions.HasValue || Divisions.Value <= 0 || TickMarkRadius <= 0)
        {
            return;
        }

        int divisions = Divisions.Value;
        SliderTickMarkShape tickShape = SliderTheme.TickMarkShape!;
        Size tickSize = tickShape.GetPreferredSize(SliderTheme, IsInteractive);
        double spacing = geometry.Width / divisions;
        if (spacing < tickSize.Width * 3.0)
        {
            return;
        }

        var enableAnimation = new ConstantAnimation<double>(IsInteractive ? 1.0 : 0.0);
        double thumbCenterX = ResolveThumbCenterX(geometry, visualValue);
        for (int index = 0; index <= divisions; index++)
        {
            double value = (double)index / divisions;
            double centerX = ResolveThumbCenterX(geometry, value);
            tickShape.Paint(
                context,
                new Point(centerX, centerY),
                new Point(thumbCenterX, centerY),
                enableAnimation,
                SliderTheme,
                TextDirection);
        }
    }

    private TextLayout? CreateLabelLayout(string? label)
    {
        if (label is null)
        {
            return null;
        }

        try
        {
            var typeface = new Typeface(
                ValueIndicatorTextStyle.FontFamily ?? FontFamily.Default,
                ValueIndicatorTextStyle.FontStyle ?? FontStyle.Normal,
                ValueIndicatorTextStyle.FontWeight ?? FontWeight.Normal,
                FontStretch.Normal);
            return new TextLayout(
                label,
                typeface,
                ValueIndicatorTextStyle.FontSize ?? 14.0,
                new SolidColorBrush(ValueIndicatorTextStyle.Color ?? Colors.White));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            return null;
        }
    }

    private bool ShouldShowValueIndicator()
    {
        if (!IsInteractive || string.IsNullOrEmpty(Label))
        {
            return false;
        }

        return ShowValueIndicator switch
        {
            Plumix.Material.ShowValueIndicator.AlwaysVisible => true,
            Plumix.Material.ShowValueIndicator.Never => false,
            Plumix.Material.ShowValueIndicator.OnlyForDiscrete => _dragging && Divisions.HasValue,
            Plumix.Material.ShowValueIndicator.OnlyForContinuous => _dragging && !Divisions.HasValue,
            _ => _dragging,
        };
    }

    private void PaintValueIndicator(PaintingContext context, Point thumbCenter, string label)
    {
        const double horizontalPadding = 8.0;
        const double verticalPadding = 4.0;
        try
        {
            var typeface = new Typeface(
                ValueIndicatorTextStyle.FontFamily ?? FontFamily.Default,
                ValueIndicatorTextStyle.FontStyle ?? FontStyle.Normal,
                ValueIndicatorTextStyle.FontWeight ?? FontWeight.Normal,
                FontStretch.Normal);
            var textLayout = new TextLayout(
                text: label,
                typeface: typeface,
                fontSize: ValueIndicatorTextStyle.FontSize ?? 14.0,
                foreground: new SolidColorBrush(ValueIndicatorTextStyle.Color ?? Colors.White));
            double width = Math.Max(32.0, textLayout.Width + (horizontalPadding * 2.0));
            double height = textLayout.Height + (verticalPadding * 2.0);
            double bottom = thumbCenter.Y - (ThumbSize.Height / 2.0) - 8.0;
            var indicatorRect = new Rect(thumbCenter.X - (width / 2.0), bottom - height, width, height);
            context.DrawRectangle(
                brush: new SolidColorBrush(ValueIndicatorColor),
                pen: null,
                rect: indicatorRect,
                radiusX: height / 2.0,
                radiusY: height / 2.0);
            context.DrawTextLayout(
                textLayout,
                new Point(indicatorRect.X + ((width - textLayout.Width) / 2.0), indicatorRect.Y + verticalPadding));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            // Host-less tests may not have a font manager; the indicator surface still remains testable.
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

        if (AllowedInteraction == SliderInteraction.SlideThumb)
        {
            var geometry = ResolveTrackGeometry(offsetX: 0);
            double thumbCenterX = ResolveThumbCenterX(geometry, ResolveVisualValue());
            double touchRadius = Math.Max(ThumbSize.Width / 2.0, 24.0);
            if (Math.Abs(@event.LocalPosition.X - thumbCenterX) > touchRadius)
            {
                return;
            }
        }

        _activePointer = @event.Pointer;
        _dragging = true;
        _hovered = true;
        _dragValueNormalized = ResolveVisualValue();

        OnChangeStartNormalized?.Invoke(_dragValueNormalized.Value);
        if (AllowedInteraction != SliderInteraction.SlideOnly)
        {
            UpdateDragValueFromLocalX(@event.LocalPosition.X);
        }
        MarkNeedsPaint();
    }

    private void HandlePointerMove(PointerMoveEvent @event)
    {
        if (!IsInteractive || _activePointer != @event.Pointer)
        {
            return;
        }

        if (AllowedInteraction == SliderInteraction.TapOnly)
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
        double next = ResolveNormalizedFromLocalX(localX);
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
        double current = ResolveVisualValue();
        var geometry = ResolveTrackGeometry(offsetX: 0);
        if (geometry.Width <= Epsilon)
        {
            return;
        }

        double directionMultiplier = TextDirection == TextDirection.Rtl ? -1.0 : 1.0;
        double normalizedDelta = (deltaX / geometry.Width) * directionMultiplier;
        double next = current + normalizedDelta;
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

        double finalValue = ResolveVisualValue();
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

        double relative = Math.Clamp(localX - geometry.Left, 0.0, geometry.Width);
        double normalized = relative / geometry.Width;
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
        double value = ClampNormalized(normalizedValue);
        double visualValue = TextDirection == TextDirection.Rtl
            ? 1.0 - value
            : value;
        return geometry.Left + (geometry.Width * visualValue);
    }

    private TrackGeometry ResolveTrackGeometry(double offsetX)
    {
        Rect preferredRect = SliderTheme.TrackShape!.GetPreferredRect(
            this,
            new Point(offsetX, 0.0),
            SliderTheme,
            IsInteractive,
            Divisions.HasValue);
        double left = preferredRect.Left;
        double right = preferredRect.Right;
        if (right < left)
        {
            double center = offsetX + (Size.Width / 2.0);
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

    private bool ShouldShowSecondaryTrack(double visualValue)
    {
        if (!_secondaryTrackValueNormalized.HasValue)
        {
            return false;
        }

        return _secondaryTrackValueNormalized.Value > ClampNormalized(visualValue) + Epsilon;
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

    private static double? ClampNormalizedNullable(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ClampNormalized(value.Value);
    }

    private readonly record struct TrackGeometry(double Left, double Right)
    {
        public double Width => Math.Max(0, Right - Left);
    }
}
