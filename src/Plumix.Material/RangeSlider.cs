using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/range_slider.dart

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
        WidgetStateProperty<Color?>? overlayColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        Key? key = null,
        RangeLabels? labels = null,
        WidgetStateProperty<MouseCursor?>? mouseCursor = null,
        EdgeInsetsGeometry? padding = null,
        bool? year2023 = null) : base(key)
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

        if (padding.HasValue && (padding.Value.Left < 0 || padding.Value.Top < 0
                                 || padding.Value.Right < 0 || padding.Value.Bottom < 0
                                 || padding.Value.Start < 0 || padding.Value.End < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "RangeSlider padding cannot be negative.");
        }

        Values = values;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        Labels = labels;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        OverlayColor = overlayColor;
        MouseCursor = mouseCursor;
        MaterialTapTargetSize = materialTapTargetSize;
        SemanticFormatterCallback = semanticFormatterCallback;
        Padding = padding;
        Year2023 = year2023;
    }

    public RangeValues Values { get; }

    public Action<RangeValues>? OnChanged { get; }

    public Action<RangeValues>? OnChangeStart { get; }

    public Action<RangeValues>? OnChangeEnd { get; }

    public double Min { get; }

    public double Max { get; }

    public int? Divisions { get; }

    public RangeLabels? Labels { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public WidgetStateProperty<Color?>? OverlayColor { get; }

    public WidgetStateProperty<MouseCursor?>? MouseCursor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public bool? Year2023 { get; }

    public override State CreateState()
    {
        return new RangeSliderState();
    }

    /// <remarks>
    /// Flutter's <c>_RangeSliderState</c> is private, but <c>startFocusNode</c>/<c>endFocusNode</c> are
    /// public fields on it and its own tests reach them through <c>tester.state(...)</c>. C# has no
    /// <c>dynamic</c>-friendly equivalent for a private nested type, so the state is <c>internal</c>.
    /// </remarks>
    internal sealed class RangeSliderState : State
    {
        private const double DefaultTrackHeight = 4.0;
        private const double DefaultThumbRadius = 10.0;
        private const double PaddedTapTargetExtent = 48.0;
        private const double Epsilon = 0.0001;

        /// <summary>The focus node for the start thumb.</summary>
        /// <remarks>
        /// Flutter's <c>_RangeSliderState.startFocusNode</c>: a range slider always owns both nodes — unlike
        /// <see cref="Slider"/>, it takes no <c>focusNode</c> parameter.
        /// </remarks>
        public FocusNode StartFocusNode { get; } = new FocusNode();

        /// <summary>The focus node for the end thumb.</summary>
        /// <remarks>Flutter's <c>_RangeSliderState.endFocusNode</c>.</remarks>
        public FocusNode EndFocusNode { get; } = new FocusNode();

        private RangeSlider CurrentWidget => (RangeSlider)StateWidget;

        private bool IsInteractive => CurrentWidget.OnChanged is not null && CurrentWidget.Max > CurrentWidget.Min;

        public override void Dispose()
        {
            StartFocusNode.Dispose();
            EndFocusNode.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
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
            var showValueIndicator = sliderTheme.ShowValueIndicator
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
            var thumbColor = ResolveThumbColor(theme, sliderTheme);
            var disabledActiveTrackColor = ResolveDisabledActiveTrackColor(theme, sliderTheme);
            var disabledInactiveTrackColor = ResolveDisabledInactiveTrackColor(theme, sliderTheme);
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

            var focusedStates = BuildStates(interactive: IsInteractive, focused: true);
            var hoveredStates = BuildStates(interactive: IsInteractive, hovered: true);
            var draggedStates = BuildStates(interactive: IsInteractive, dragged: true);
            var overlayFocusedColor = ResolveOverlayColor(theme, sliderTheme, focusedStates);
            var overlayHoveredColor = ResolveOverlayColor(theme, sliderTheme, hoveredStates);
            var overlayDraggedColor = ResolveOverlayColor(theme, sliderTheme, draggedStates);
            RangeSliderValueIndicatorShape valueIndicatorShape = sliderTheme.RangeValueIndicatorShape
                                                                 ?? (theme.UseMaterial3 && !year2023
                                                                     ? new RoundedRectRangeSliderValueIndicatorShape()
                                                                     : new RectangularRangeSliderValueIndicatorShape());
            if (valueIndicatorShape is RectangularRangeSliderValueIndicatorShape
                && !sliderTheme.ValueIndicatorColor.HasValue)
            {
                valueIndicatorColor = AlphaBlend(
                    theme.ColorScheme.OnSurface.WithOpacity(0.60),
                    theme.ColorScheme.Surface.WithOpacity(0.90));
            }

            var effectiveSliderTheme = new SliderThemeData(
                TrackHeight: trackHeight,
                ActiveTrackColor: activeTrackColor,
                InactiveTrackColor: inactiveTrackColor,
                DisabledActiveTrackColor: disabledActiveTrackColor,
                DisabledInactiveTrackColor: disabledInactiveTrackColor,
                ActiveTickMarkColor: activeTickMarkColor,
                InactiveTickMarkColor: inactiveTickMarkColor,
                DisabledActiveTickMarkColor: activeTickMarkColor,
                DisabledInactiveTickMarkColor: inactiveTickMarkColor,
                ThumbColor: thumbColor,
                OverlappingShapeStrokeColor: sliderTheme.OverlappingShapeStrokeColor
                                             ?? theme.ColorScheme.Surface,
                DisabledThumbColor: disabledThumbColor,
                OverlayColor: WidgetStateProperty<Color?>.All(overlayDraggedColor),
                ValueIndicatorColor: valueIndicatorColor,
                ValueIndicatorStrokeColor: sliderTheme.ValueIndicatorStrokeColor,
                OverlayShape: sliderTheme.OverlayShape ?? new RoundSliderOverlayShape(overlayRadius),
                RangeTickMarkShape: sliderTheme.RangeTickMarkShape
                                    ?? new RoundRangeSliderTickMarkShape(tickMarkRadius),
                RangeThumbShape: sliderTheme.RangeThumbShape
                                 ?? (year2023
                                     ? new RoundRangeSliderThumbShape(thumbRadius)
                                     : new HandleRangeSliderThumbShape()),
                RangeTrackShape: sliderTheme.RangeTrackShape
                                 ?? (year2023
                                     ? new RoundedRectRangeSliderTrackShape()
                                     : new GappedRangeSliderTrackShape()),
                RangeValueIndicatorShape: valueIndicatorShape,
                ShowValueIndicator: showValueIndicator,
                ValueIndicatorTextStyle: valueIndicatorTextStyle,
                MinThumbSeparation: sliderTheme.MinThumbSeparation ?? (year2023 ? 8.0 : 0.0),
                ThumbSelector: sliderTheme.ThumbSelector,
                MouseCursor: sliderTheme.MouseCursor,
                Padding: paddingGeometry,
                ThumbSize: sliderTheme.ThumbSize ?? WidgetStateProperty<Size?>.All(thumbSize),
                TrackGap: trackGap,
                Year2023: year2023);

            var normalizedValues = Normalize(CurrentWidget.Values);

            // Dart parks two zero-size `Focus` boxes in a `Row` behind the slider: their order gives Tab
            // traversal start-then-end, and `includeSemantics: false` keeps them out of the semantics tree
            // so the only slider nodes are the two `_RenderRangeSlider.assembleSemanticsNode` produces.
            Widget result = new Stack(
                children:
                [
                    new Row(
                        children:
                        [
                            new Focus(
                                focusNode: StartFocusNode,
                                includeSemantics: false,
                                child: new SizedBox(width: 0, height: 0)),
                            new Focus(
                                focusNode: EndFocusNode,
                                includeSemantics: false,
                                child: new SizedBox(width: 0, height: 0)),
                        ]),
                    new RangeSliderRenderWidget(
                        sliderTheme: effectiveSliderTheme,
                        startValueNormalized: normalizedValues.Start,
                        endValueNormalized: normalizedValues.End,
                        divisions: CurrentWidget.Divisions,
                        isInteractive: IsInteractive,
                        state: this,
                        trackHeight: trackHeight,
                        thumbRadius: thumbRadius,
                        thumbSize: thumbSize,
                        overlayRadius: overlayRadius,
                        minPreferredHeight: minPreferredHeight,
                        activeTrackColor: IsInteractive ? activeTrackColor : disabledActiveTrackColor,
                        inactiveTrackColor: IsInteractive ? inactiveTrackColor : disabledInactiveTrackColor,
                        thumbColor: IsInteractive ? thumbColor : disabledThumbColor,
                        overlayFocusedColor: overlayFocusedColor,
                        overlayHoveredColor: overlayHoveredColor,
                        overlayDraggedColor: overlayDraggedColor,
                        activeTickMarkColor: activeTickMarkColor,
                        inactiveTickMarkColor: inactiveTickMarkColor,
                        tickMarkRadius: tickMarkRadius,
                        labels: CurrentWidget.Labels,
                        showValueIndicator: showValueIndicator,
                        valueIndicatorColor: valueIndicatorColor,
                        valueIndicatorTextStyle: valueIndicatorTextStyle,
                        padding: padding,
                        trackGap: trackGap,
                        textDirection: Directionality.Of(context),
                        min: CurrentWidget.Min,
                        max: CurrentWidget.Max,
                        semanticFormatterCallback: CurrentWidget.SemanticFormatterCallback,
                        adjustmentUnit: ResolveAdjustmentUnit(theme),
                        onChangeStartNormalized: IsInteractive ? HandleChangeStartNormalized : null,
                        onChangedNormalized: IsInteractive ? HandleChangedNormalized : null,
                        onChangeEndNormalized: IsInteractive ? HandleChangeEndNormalized : null),
                ]);

            // Dart's `RangeSlider` never adds `WidgetState.focused` to the cursor state set — only the
            // disabled/hovered/dragged triple — because focus lives on the two thumbs, not the widget.
            var cursorStates = BuildStates(interactive: IsInteractive, focused: false);
            MouseCursor cursor = CurrentWidget.MouseCursor?.Resolve(cursorStates)
                                 ?? sliderTheme.MouseCursor?.Resolve(cursorStates)
                                 ?? (IsInteractive ? SystemMouseCursors.Click : SystemMouseCursors.Basic);
            return new MouseRegion(cursor: cursor, child: result);
        }

        /// <remarks>
        /// Flutter's <c>_RenderRangeSlider._adjustmentUnit</c> maps only iOS to <c>0.1</c>; macOS steps by
        /// <c>0.05</c> here even though <c>_RenderSlider._adjustmentUnit</c> gives it <c>0.1</c>.
        /// </remarks>
        private double ResolveAdjustmentUnit(ThemeData theme)
        {
            return theme.Platform is TargetPlatform.IOS ? 0.1 : 0.05;
        }

        private void HandleChangeStartNormalized(NormalizedRangeValues normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            CurrentWidget.OnChangeStart?.Invoke(Denormalize(SnapNormalized(normalized)));
        }

        private void HandleChangedNormalized(NormalizedRangeValues normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            var nextValues = Denormalize(SnapNormalized(normalized));
            if (Math.Abs(nextValues.Start - CurrentWidget.Values.Start) <= Epsilon
                && Math.Abs(nextValues.End - CurrentWidget.Values.End) <= Epsilon)
            {
                return;
            }

            CurrentWidget.OnChanged?.Invoke(nextValues);
        }

        private void HandleChangeEndNormalized(NormalizedRangeValues normalized)
        {
            if (!IsInteractive)
            {
                return;
            }

            CurrentWidget.OnChangeEnd?.Invoke(Denormalize(SnapNormalized(normalized)));
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
                   ?? (theme.UseMaterial3 && !year2023
                       ? theme.ColorScheme.SecondaryContainer
                       : theme.ColorScheme.Primary.WithOpacity(0.24));
        }

        private Color ResolveThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return CurrentWidget.ActiveColor
                   ?? sliderTheme.ThumbColor
                   ?? theme.ColorScheme.Primary;
        }

        private Color ResolveDisabledActiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            bool useLatest = theme.UseMaterial3
                             && !(CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            return sliderTheme.DisabledActiveTrackColor
                   ?? theme.ColorScheme.OnSurface.WithOpacity(useLatest ? 0.38 : 0.32);
        }

        private Color ResolveDisabledInactiveTrackColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            return sliderTheme.DisabledInactiveTrackColor
                   ?? theme.ColorScheme.OnSurface.WithOpacity(0.12);
        }

        private Color ResolveDisabledThumbColor(ThemeData theme, SliderThemeData sliderTheme)
        {
            bool useLatest = theme.UseMaterial3
                             && !(CurrentWidget.Year2023 ?? sliderTheme.Year2023 ?? true);
            return sliderTheme.DisabledThumbColor
                   ?? (useLatest
                       ? theme.ColorScheme.OnSurface.WithOpacity(0.38)
                       : AlphaBlend(
                           theme.ColorScheme.OnSurface.WithOpacity(0.38),
                           theme.ColorScheme.Surface));
        }

        private Color ResolveActiveTickMarkColor(
            ThemeData theme,
            SliderThemeData sliderTheme,
            bool year2023)
        {
            Color fallback = theme.UseMaterial3 && !year2023
                ? theme.ColorScheme.OnPrimary
                : theme.ColorScheme.OnPrimary.WithOpacity(0.54);
            return IsInteractive
                ? sliderTheme.ActiveTickMarkColor ?? fallback
                : sliderTheme.DisabledActiveTickMarkColor
                  ?? (theme.UseMaterial3 && !year2023
                      ? theme.ColorScheme.OnInverseSurface
                      : theme.ColorScheme.OnPrimary.WithOpacity(0.12));
        }

        private Color ResolveInactiveTickMarkColor(
            ThemeData theme,
            SliderThemeData sliderTheme,
            bool year2023)
        {
            Color fallback = theme.UseMaterial3 && !year2023
                ? theme.ColorScheme.OnSecondaryContainer
                : theme.ColorScheme.Primary.WithOpacity(0.54);
            return IsInteractive
                ? sliderTheme.InactiveTickMarkColor ?? fallback
                : sliderTheme.DisabledInactiveTickMarkColor
                  ?? (theme.UseMaterial3 && !year2023
                      ? theme.ColorScheme.OnSurface
                      : theme.ColorScheme.OnSurface.WithOpacity(0.12));
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
                    : CurrentWidget.ActiveColor.Value.WithOpacity(0.12);
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
                    : baseColor.WithOpacity(0.12);
            }

            if (states.Contains(WidgetState.Dragged))
            {
                return baseColor.WithOpacity(0.10);
            }

            if (states.Contains(WidgetState.Hovered))
            {
                return baseColor.WithOpacity(0.08);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return baseColor.WithOpacity(0.10);
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
        SliderThemeData sliderTheme,
        double startValueNormalized,
        double endValueNormalized,
        int? divisions,
        bool isInteractive,
        RangeSlider.RangeSliderState state,
        double trackHeight,
        double thumbRadius,
        Size thumbSize,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        Color activeTickMarkColor,
        Color inactiveTickMarkColor,
        double tickMarkRadius,
        RangeLabels? labels,
        ShowValueIndicator showValueIndicator,
        Color valueIndicatorColor,
        TextStyle valueIndicatorTextStyle,
        Thickness padding,
        double trackGap,
        TextDirection textDirection,
        double min,
        double max,
        SemanticFormatterCallback? semanticFormatterCallback,
        double adjustmentUnit,
        Action<NormalizedRangeValues>? onChangeStartNormalized,
        Action<NormalizedRangeValues>? onChangedNormalized,
        Action<NormalizedRangeValues>? onChangeEndNormalized,
        Key? key = null) : base(key)
    {
        SliderTheme = sliderTheme;
        StartValueNormalized = startValueNormalized;
        EndValueNormalized = endValueNormalized;
        Divisions = divisions;
        IsInteractive = isInteractive;
        State = state;
        TrackHeight = trackHeight;
        ThumbRadius = thumbRadius;
        ThumbSize = thumbSize;
        OverlayRadius = overlayRadius;
        MinPreferredHeight = minPreferredHeight;
        ActiveTrackColor = activeTrackColor;
        InactiveTrackColor = inactiveTrackColor;
        ThumbColor = thumbColor;
        OverlayFocusedColor = overlayFocusedColor;
        OverlayHoveredColor = overlayHoveredColor;
        OverlayDraggedColor = overlayDraggedColor;
        ActiveTickMarkColor = activeTickMarkColor;
        InactiveTickMarkColor = inactiveTickMarkColor;
        TickMarkRadius = tickMarkRadius;
        Labels = labels;
        ShowValueIndicator = showValueIndicator;
        ValueIndicatorColor = valueIndicatorColor;
        ValueIndicatorTextStyle = valueIndicatorTextStyle;
        Padding = padding;
        TrackGap = trackGap;
        TextDirection = textDirection;
        Min = min;
        Max = max;
        SemanticFormatterCallback = semanticFormatterCallback;
        AdjustmentUnit = adjustmentUnit;
        OnChangeStartNormalized = onChangeStartNormalized;
        OnChangedNormalized = onChangedNormalized;
        OnChangeEndNormalized = onChangeEndNormalized;
    }

    public SliderThemeData SliderTheme { get; }

    public double StartValueNormalized { get; }

    public double EndValueNormalized { get; }

    public int? Divisions { get; }

    public bool IsInteractive { get; }

    /// <summary>The state that owns the two thumb focus nodes.</summary>
    /// <remarks>Flutter passes <c>state: this</c> into <c>_RangeSliderRenderObjectWidget</c> the same way.</remarks>
    public RangeSlider.RangeSliderState State { get; }

    public double TrackHeight { get; }

    public double ThumbRadius { get; }

    public Size ThumbSize { get; }

    public double OverlayRadius { get; }

    public double MinPreferredHeight { get; }

    public Color ActiveTrackColor { get; }

    public Color InactiveTrackColor { get; }

    public Color ThumbColor { get; }

    public Color? OverlayFocusedColor { get; }

    public Color? OverlayHoveredColor { get; }

    public Color? OverlayDraggedColor { get; }

    public Color ActiveTickMarkColor { get; }

    public Color InactiveTickMarkColor { get; }

    public double TickMarkRadius { get; }

    public RangeLabels? Labels { get; }

    public ShowValueIndicator ShowValueIndicator { get; }

    public Color ValueIndicatorColor { get; }

    public TextStyle ValueIndicatorTextStyle { get; }

    public Thickness Padding { get; }

    public double TrackGap { get; }

    public TextDirection TextDirection { get; }

    public double Min { get; }

    public double Max { get; }

    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    public double AdjustmentUnit { get; }

    public Action<NormalizedRangeValues>? OnChangeStartNormalized { get; }

    public Action<NormalizedRangeValues>? OnChangedNormalized { get; }

    public Action<NormalizedRangeValues>? OnChangeEndNormalized { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderRangeSlider(
            sliderTheme: SliderTheme,
            startValueNormalized: StartValueNormalized,
            endValueNormalized: EndValueNormalized,
            divisions: Divisions,
            isInteractive: IsInteractive,
            state: State,
            trackHeight: TrackHeight,
            thumbRadius: ThumbRadius,
            thumbSize: ThumbSize,
            overlayRadius: OverlayRadius,
            minPreferredHeight: MinPreferredHeight,
            activeTrackColor: ActiveTrackColor,
            inactiveTrackColor: InactiveTrackColor,
            thumbColor: ThumbColor,
            overlayFocusedColor: OverlayFocusedColor,
            overlayHoveredColor: OverlayHoveredColor,
            overlayDraggedColor: OverlayDraggedColor,
            activeTickMarkColor: ActiveTickMarkColor,
            inactiveTickMarkColor: InactiveTickMarkColor,
            tickMarkRadius: TickMarkRadius,
            labels: Labels,
            showValueIndicator: ShowValueIndicator,
            valueIndicatorColor: ValueIndicatorColor,
            valueIndicatorTextStyle: ValueIndicatorTextStyle,
            padding: Padding,
            trackGap: TrackGap,
            textDirection: TextDirection,
            min: Min,
            max: Max,
            semanticFormatterCallback: SemanticFormatterCallback,
            adjustmentUnit: AdjustmentUnit,
            onChangeStartNormalized: OnChangeStartNormalized,
            onChangedNormalized: OnChangedNormalized,
            onChangeEndNormalized: OnChangeEndNormalized);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var rangeSlider = (RenderRangeSlider)renderObject;
        rangeSlider.SliderTheme = SliderTheme;
        rangeSlider.StartValueNormalized = StartValueNormalized;
        rangeSlider.EndValueNormalized = EndValueNormalized;
        rangeSlider.Divisions = Divisions;
        rangeSlider.IsInteractive = IsInteractive;
        rangeSlider.State = State;
        rangeSlider.TrackHeight = TrackHeight;
        rangeSlider.ThumbRadius = ThumbRadius;
        rangeSlider.ThumbSize = ThumbSize;
        rangeSlider.OverlayRadius = OverlayRadius;
        rangeSlider.MinPreferredHeight = MinPreferredHeight;
        rangeSlider.ActiveTrackColor = ActiveTrackColor;
        rangeSlider.InactiveTrackColor = InactiveTrackColor;
        rangeSlider.ThumbColor = ThumbColor;
        rangeSlider.OverlayFocusedColor = OverlayFocusedColor;
        rangeSlider.OverlayHoveredColor = OverlayHoveredColor;
        rangeSlider.OverlayDraggedColor = OverlayDraggedColor;
        rangeSlider.ActiveTickMarkColor = ActiveTickMarkColor;
        rangeSlider.InactiveTickMarkColor = InactiveTickMarkColor;
        rangeSlider.TickMarkRadius = TickMarkRadius;
        rangeSlider.Labels = Labels;
        rangeSlider.ShowValueIndicator = ShowValueIndicator;
        rangeSlider.ValueIndicatorColor = ValueIndicatorColor;
        rangeSlider.ValueIndicatorTextStyle = ValueIndicatorTextStyle;
        rangeSlider.Padding = Padding;
        rangeSlider.TrackGap = TrackGap;
        rangeSlider.TextDirection = TextDirection;
        rangeSlider.Min = Min;
        rangeSlider.Max = Max;
        rangeSlider.SemanticFormatterCallback = SemanticFormatterCallback;
        rangeSlider.AdjustmentUnit = AdjustmentUnit;
        rangeSlider.OnChangeStartNormalized = OnChangeStartNormalized;
        rangeSlider.OnChangedNormalized = OnChangedNormalized;
        rangeSlider.OnChangeEndNormalized = OnChangeEndNormalized;
    }
}

internal sealed class RenderRangeSlider : RenderBox
{
    private const double DefaultTrackWidth = 144.0;
    private const double Epsilon = 0.0001;

    private SliderThemeData _sliderTheme;
    private double _startValueNormalized;
    private double _endValueNormalized;
    private int? _divisions;
    private bool _isInteractive;
    private RangeSlider.RangeSliderState _state;
    private double _trackHeight;
    private double _thumbRadius;
    private Size _thumbSize;
    private double _overlayRadius;
    private double _minPreferredHeight;
    private Color _activeTrackColor;
    private Color _inactiveTrackColor;
    private Color _thumbColor;
    private Color? _overlayFocusedColor;
    private Color? _overlayHoveredColor;
    private Color? _overlayDraggedColor;
    private Color _activeTickMarkColor;
    private Color _inactiveTickMarkColor;
    private double _tickMarkRadius;
    private RangeLabels? _labels;
    private ShowValueIndicator _showValueIndicator;
    private Color _valueIndicatorColor;
    private TextStyle _valueIndicatorTextStyle;
    private Thickness _padding;
    private double _trackGap;
    private TextDirection _textDirection;
    private double _min;
    private double _max;
    private SemanticFormatterCallback? _semanticFormatterCallback;
    private double _adjustmentUnit;
    private Point _startThumbCenter;
    private Point _endThumbCenter;
    private SemanticsNode? _startSemanticsNode;
    private SemanticsNode? _endSemanticsNode;
    private Action<NormalizedRangeValues>? _onChangeStartNormalized;
    private Action<NormalizedRangeValues>? _onChangedNormalized;
    private Action<NormalizedRangeValues>? _onChangeEndNormalized;

    private bool _hovered;
    private bool _dragging;
    private int? _activePointer;
    private RangeSliderThumb? _activeThumb;
    private NormalizedRangeValues? _dragValues;
    private double? _lastGlobalPointerX;

    public RenderRangeSlider(
        SliderThemeData sliderTheme,
        double startValueNormalized,
        double endValueNormalized,
        int? divisions,
        bool isInteractive,
        RangeSlider.RangeSliderState state,
        double trackHeight,
        double thumbRadius,
        Size thumbSize,
        double overlayRadius,
        double minPreferredHeight,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color thumbColor,
        Color? overlayFocusedColor,
        Color? overlayHoveredColor,
        Color? overlayDraggedColor,
        Color activeTickMarkColor,
        Color inactiveTickMarkColor,
        double tickMarkRadius,
        RangeLabels? labels,
        ShowValueIndicator showValueIndicator,
        Color valueIndicatorColor,
        TextStyle valueIndicatorTextStyle,
        Thickness padding,
        double trackGap,
        TextDirection textDirection,
        double min,
        double max,
        SemanticFormatterCallback? semanticFormatterCallback,
        double adjustmentUnit,
        Action<NormalizedRangeValues>? onChangeStartNormalized,
        Action<NormalizedRangeValues>? onChangedNormalized,
        Action<NormalizedRangeValues>? onChangeEndNormalized)
    {
        _sliderTheme = sliderTheme;
        var initial = OrderAndClamp(startValueNormalized, endValueNormalized);
        _startValueNormalized = initial.Start;
        _endValueNormalized = initial.End;
        _divisions = divisions;
        _isInteractive = isInteractive;
        _state = state;
        _trackHeight = trackHeight;
        _thumbRadius = thumbRadius;
        _thumbSize = thumbSize;
        _overlayRadius = overlayRadius;
        _minPreferredHeight = minPreferredHeight;
        _activeTrackColor = activeTrackColor;
        _inactiveTrackColor = inactiveTrackColor;
        _thumbColor = thumbColor;
        _overlayFocusedColor = overlayFocusedColor;
        _overlayHoveredColor = overlayHoveredColor;
        _overlayDraggedColor = overlayDraggedColor;
        _activeTickMarkColor = activeTickMarkColor;
        _inactiveTickMarkColor = inactiveTickMarkColor;
        _tickMarkRadius = tickMarkRadius;
        _labels = labels;
        _showValueIndicator = showValueIndicator;
        _valueIndicatorColor = valueIndicatorColor;
        _valueIndicatorTextStyle = valueIndicatorTextStyle;
        _padding = padding;
        _trackGap = trackGap;
        _textDirection = textDirection;
        _min = min;
        _max = max;
        _semanticFormatterCallback = semanticFormatterCallback;
        _adjustmentUnit = adjustmentUnit;
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

    /// <summary>The state whose two focus nodes drive the per-thumb focus overlays and semantics.</summary>
    public RangeSlider.RangeSliderState State
    {
        get => _state;
        set
        {
            if (ReferenceEquals(_state, value))
            {
                return;
            }

            if (Attached)
            {
                DetachFocusListeners(_state);
                AttachFocusListeners(value);
            }

            _state = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>Whether either thumb currently holds focus.</summary>
    /// <remarks>
    /// Dart has no `isFocused` on `_RenderRangeSlider`; it reads `_state.startFocusNode.hasFocus` and
    /// `_state.endFocusNode.hasFocus` at each use site. Plumix keeps this aggregate only for the overlay
    /// colour resolution, which is per-widget rather than per-thumb.
    /// </remarks>
    private bool IsFocused => _state.StartFocusNode.HasFocus || _state.EndFocusNode.HasFocus;

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

    public RangeLabels? Labels
    {
        get => _labels;
        set { if (_labels != value) { _labels = value; MarkNeedsPaint(); } }
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

    public Action<NormalizedRangeValues>? OnChangeStartNormalized
    {
        get => _onChangeStartNormalized;
        set => _onChangeStartNormalized = value;
    }

    public Action<NormalizedRangeValues>? OnChangedNormalized
    {
        get => _onChangedNormalized;
        set => _onChangedNormalized = value;
    }

    public Action<NormalizedRangeValues>? OnChangeEndNormalized
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
            Size thumb = SliderTheme.RangeThumbShape!.GetPreferredSize(IsInteractive, discrete);
            Size tick = SliderTheme.RangeTickMarkShape!.GetPreferredSize(SliderTheme, IsInteractive);
            double overlayHeight = SliderTheme.Padding.HasValue ? thumb.Height : overlay.Height;
            return new Size(
                Math.Max(overlay.Width, Math.Max(thumb.Width, tick.Width)),
                Math.Max(overlayHeight, Math.Max(thumb.Height, tick.Height)));
        }
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>_RenderRangeSlider.sizedByParent</c>.</remarks>
    protected override bool SizedByParent => true;

    /// <inheritdoc />
    /// <remarks>Flutter's <c>_RenderRangeSlider.computeDryLayout</c>.</remarks>
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        double desiredWidth = constraints.HasBoundedWidth ? constraints.MaxWidth : PreferredWidth;
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

        return constraints.Constrain(new Size(desiredWidth, desiredHeight));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        var values = ResolveVisualValues();
        bool discrete = Divisions.HasValue;
        RangeSliderTrackShape trackShape = SliderTheme.RangeTrackShape!;
        Rect trackRect = trackShape.GetPreferredRect(this, offset, SliderTheme, IsInteractive, discrete);
        var geometry = new TrackGeometry(trackRect.Left, trackRect.Right);
        var startCenter = new Point(
            ResolveThumbCenterX(geometry, values.Start),
            trackRect.Center.Y);
        var endCenter = new Point(
            ResolveThumbCenterX(geometry, values.End),
            trackRect.Center.Y);
        // Flutter caches the two thumb centres in `paint` and reuses them for the semantics rects, so the
        // rects follow the position animation. Plumix caches them without the paint `offset`, because a
        // `SemanticsNode.Rect` is in its owning render object's own coordinate system and the parent node's
        // transform already carries the offset — adding it here would count the position twice.
        _startThumbCenter = new Point(startCenter.X - offset.X, startCenter.Y - offset.Y);
        _endThumbCenter = new Point(endCenter.X - offset.X, endCenter.Y - offset.Y);

        var enableAnimation = new ConstantAnimation<double>(IsInteractive ? 1.0 : 0.0);
        bool active = _dragging || _hovered;
        var activationAnimation = new ConstantAnimation<double>(active ? 1.0 : 0.0);
        trackShape.Paint(
            ctx,
            offset,
            startCenter,
            endCenter,
            enableAnimation,
            discrete,
            IsInteractive,
            this,
            SliderTheme,
            TextDirection);

        Color? overlayColor = ResolveOverlayColor();
        SliderThemeData paintTheme = SliderTheme.CopyWith(
            overlayColor: WidgetStateProperty<Color?>.All(overlayColor));

        // Flutter paints a fully-activated overlay under each *focused* thumb, before the
        // activation-driven overlay, so keyboard focus highlights the thumb that actually has it rather
        // than whichever thumb was dragged last.
        if (overlayColor is { A: > 0 })
        {
            if (_state.StartFocusNode.HasFocus)
            {
                PaintThumbOverlay(ctx, paintTheme, startCenter, values.Start, discrete, enableAnimation);
            }

            if (_state.EndFocusNode.HasFocus)
            {
                PaintThumbOverlay(ctx, paintTheme, endCenter, values.End, discrete, enableAnimation);
            }
        }

        if (active && overlayColor is { A: > 0 })
        {
            Point overlayCenter = _activeThumb == RangeSliderThumb.Start ? startCenter : endCenter;
            paintTheme.OverlayShape!.Paint(
                ctx,
                overlayCenter,
                activationAnimation,
                enableAnimation,
                discrete,
                null,
                this,
                paintTheme,
                TextDirection,
                _activeThumb == RangeSliderThumb.Start ? values.Start : values.End,
                1.0,
                Size);
        }

        PaintTickMarks(ctx, geometry, trackRect.Center.Y, values);
        bool startIsTop = _activeThumb == RangeSliderThumb.Start;
        Point bottomCenter = startIsTop ? endCenter : startCenter;
        Point topCenter = startIsTop ? startCenter : endCenter;
        Thumb bottomThumb = startIsTop ? Thumb.End : Thumb.Start;
        Thumb topThumb = startIsTop ? Thumb.Start : Thumb.End;
        RangeSliderThumbShape thumbShape = paintTheme.RangeThumbShape!;
        double distance = Math.Abs(startCenter.X - endCenter.X);
        bool overlaps = distance < thumbShape.GetPreferredSize(IsInteractive, discrete).Width;

        PaintRangeValueIndicator(
            ctx,
            bottomCenter,
            bottomThumb,
            isOnTop: false,
            activationAnimation,
            enableAnimation,
            paintTheme,
            discrete);
        thumbShape.Paint(
            ctx,
            bottomCenter,
            activationAnimation,
            enableAnimation,
            discrete,
            isOnTop: false,
            isPressed: false,
            paintTheme,
            TextDirection,
            bottomThumb);
        PaintRangeValueIndicator(
            ctx,
            topCenter,
            topThumb,
            overlaps,
            activationAnimation,
            enableAnimation,
            paintTheme,
            discrete);
        thumbShape.Paint(
            ctx,
            topCenter,
            activationAnimation,
            enableAnimation,
            discrete,
            overlaps,
            isPressed: _dragging,
            paintTheme,
            TextDirection,
            topThumb);
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

    private void PaintTickMarks(
        PaintingContext context,
        TrackGeometry geometry,
        double centerY,
        NormalizedRangeValues values)
    {
        if (!Divisions.HasValue || Divisions.Value <= 0 || TickMarkRadius <= 0)
        {
            return;
        }

        int divisions = Divisions.Value;
        RangeSliderTickMarkShape tickShape = SliderTheme.RangeTickMarkShape!;
        Size tickSize = tickShape.GetPreferredSize(SliderTheme, IsInteractive);
        double spacing = geometry.Width / divisions;
        if (spacing < tickSize.Width * 3.0)
        {
            return;
        }

        var enableAnimation = new ConstantAnimation<double>(IsInteractive ? 1.0 : 0.0);
        var startCenter = new Point(ResolveThumbCenterX(geometry, values.Start), centerY);
        var endCenter = new Point(ResolveThumbCenterX(geometry, values.End), centerY);
        for (int index = 0; index <= divisions; index++)
        {
            double value = (double)index / divisions;
            double centerX = ResolveThumbCenterX(geometry, value);
            tickShape.Paint(
                context,
                new Point(centerX, centerY),
                startCenter,
                endCenter,
                enableAnimation,
                SliderTheme,
                TextDirection);
        }
    }

    private void PaintRangeValueIndicator(
        PaintingContext context,
        Point center,
        Thumb thumb,
        bool isOnTop,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        bool isDiscrete)
    {
        if (!ShouldShowValueIndicator() || !Labels.HasValue)
        {
            return;
        }

        string label = thumb == Thumb.Start ? Labels.Value.Start : Labels.Value.End;
        TextLayout? layout = CreateLabelLayout(label);
        if (layout is null)
        {
            return;
        }

        sliderTheme.RangeValueIndicatorShape!.Paint(
            context,
            center,
            new ConstantAnimation<double>(1.0),
            enableAnimation,
            isDiscrete,
            isOnTop,
            layout,
            this,
            sliderTheme,
            TextDirection,
            thumb,
            thumb == Thumb.Start ? StartValueNormalized : EndValueNormalized,
            1.0,
            Size);
    }

    private TextLayout? CreateLabelLayout(string label)
    {
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

    private bool ShouldShowValueIndicator()
    {
        if (!IsInteractive || !Labels.HasValue)
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
            // Flutter's `_startInteraction` moves focus onto the thumb the pointer picked, before the drag
            // starts. `Slider` deliberately does not do this — only `RangeSlider`.
            switch (_activeThumb.Value)
            {
                case RangeSliderThumb.Start:
                    _state.StartFocusNode.RequestFocus();
                    break;
                case RangeSliderThumb.End:
                    _state.EndFocusNode.RequestFocus();
                    break;
            }

            OnChangeStartNormalized?.Invoke(_dragValues.Value);
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
        OnChangedNormalized?.Invoke(next);
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
        OnChangedNormalized?.Invoke(next);
        MarkNeedsPaint();
    }

    private void EndDragIfNeeded(bool canceled)
    {
        if (!_dragging && _activePointer is null)
        {
            return;
        }

        var finalValues = ResolveVisualValues();
        _activePointer = null;
        _dragging = false;
        _activeThumb = null;
        _dragValues = null;
        _lastGlobalPointerX = null;

        if (!canceled)
        {
            OnChangeEndNormalized?.Invoke(finalValues);
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
        Rect trackRect = SliderTheme.RangeTrackShape!.GetPreferredRect(
            this,
            default,
            SliderTheme,
            IsInteractive,
            Divisions.HasValue);
        Size thumbSize = SliderTheme.RangeThumbShape!.GetPreferredSize(IsInteractive, Divisions.HasValue);
        Thumb? selectedThumb = SliderTheme.ThumbSelector?.Invoke(
            TextDirection,
            new RangeValues(values.Start, values.End),
            tapValue,
            thumbSize,
            trackRect.Size,
            localX - trackRect.Left);
        if (selectedThumb.HasValue)
        {
            return selectedThumb.Value == Thumb.Start ? RangeSliderThumb.Start : RangeSliderThumb.End;
        }

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
        var geometry = ResolveTrackGeometry(offsetX: 0.0);
        double minimumSeparation = Divisions.HasValue || geometry.Width <= Epsilon
            ? 0.0
            : (SliderTheme.MinThumbSeparation ?? 0.0) / geometry.Width;
        if (thumb == RangeSliderThumb.Start)
        {
            double nextStart = Math.Clamp(nextValue, 0.0, Math.Max(0.0, current.End - minimumSeparation));
            return OrderAndClamp(nextStart, current.End);
        }

        double nextEnd = Math.Clamp(nextValue, Math.Min(1.0, current.Start + minimumSeparation), 1.0);
        return OrderAndClamp(current.Start, nextEnd);
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        AttachFocusListeners(_state);
    }

    protected override void OnDetach()
    {
        DetachFocusListeners(_state);
        base.OnDetach();
    }

    /// <remarks>
    /// Flutter subscribes both focus nodes to <c>markNeedsPaint</c> and <c>markNeedsSemanticsUpdate</c> in
    /// <c>attach</c>: the paint listener drives the per-thumb focus overlay, the semantics listener is what
    /// makes keyboard focus show up as <c>isFocused</c> on the matching thumb node.
    /// </remarks>
    private void AttachFocusListeners(RangeSlider.RangeSliderState state)
    {
        state.StartFocusNode.AddListener(MarkNeedsPaint);
        state.StartFocusNode.AddListener(MarkNeedsSemanticsUpdate);
        state.EndFocusNode.AddListener(MarkNeedsPaint);
        state.EndFocusNode.AddListener(MarkNeedsSemanticsUpdate);
    }

    private void DetachFocusListeners(RangeSlider.RangeSliderState state)
    {
        state.StartFocusNode.RemoveListener(MarkNeedsPaint);
        state.StartFocusNode.RemoveListener(MarkNeedsSemanticsUpdate);
        state.EndFocusNode.RemoveListener(MarkNeedsPaint);
        state.EndFocusNode.RemoveListener(MarkNeedsSemanticsUpdate);
    }

    /// <summary>The lower bound the normalized values are mapped back onto for semantics.</summary>
    public double Min
    {
        get => _min;
        set
        {
            if (_min.Equals(value))
            {
                return;
            }

            _min = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The upper bound the normalized values are mapped back onto for semantics.</summary>
    public double Max
    {
        get => _max;
        set
        {
            if (_max.Equals(value))
            {
                return;
            }

            _max = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>_RenderRangeSlider.semanticFormatterCallback</c>.</remarks>
    public SemanticFormatterCallback? SemanticFormatterCallback
    {
        get => _semanticFormatterCallback;
        set
        {
            if (_semanticFormatterCallback == value)
            {
                return;
            }

            _semanticFormatterCallback = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The platform-derived step a semantics adjustment moves a continuous thumb by.</summary>
    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider._adjustmentUnit</c> maps macOS to <c>0.05</c>, unlike
    /// <c>_RenderSlider._adjustmentUnit</c>, which maps it to <c>0.1</c>. Plumix resolves it in the state and
    /// reproduces that asymmetry there.
    /// </remarks>
    public double AdjustmentUnit
    {
        get => _adjustmentUnit;
        set
        {
            if (_adjustmentUnit.Equals(value))
            {
                return;
            }

            _adjustmentUnit = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>_RenderRangeSlider._semanticActionUnit</c>.</remarks>
    private double SemanticActionUnit => Divisions is { } divisions ? 1.0 / divisions : AdjustmentUnit;

    /// <summary>
    /// The minimum gap the two thumbs must keep, in normalized units.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider._minThumbSeparationValue</c>: zero for a discrete slider, and
    /// otherwise the theme's separation divided by the track width — so it is layout-dependent, and the
    /// increase/decrease values that read it change with the slider's size.
    /// </remarks>
    private double MinThumbSeparationValue
    {
        get
        {
            if (Divisions is { } divisions && divisions > 0)
            {
                return 0.0;
            }

            double trackWidth = ResolveTrackGeometry(offsetX: 0).Width;
            return trackWidth > 0 ? (SliderTheme.MinThumbSeparation ?? 0.0) / trackWidth : 0.0;
        }
    }

    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider._increasedStartValue</c>. The <c>toStringAsFixed(2)</c> round-trip is
    /// Dart's own guard against <c>0.4 + 0.2 == 0.6000000000000001</c>, and the separation bound saturates to
    /// the current value rather than clamping to the neighbour.
    /// </remarks>
    private double IncreasedStartValue
    {
        get
        {
            double increased = RoundToTwoDecimals(StartValueNormalized + SemanticActionUnit);
            return increased <= EndValueNormalized - MinThumbSeparationValue ? increased : StartValueNormalized;
        }
    }

    /// <remarks>Flutter's <c>_RenderRangeSlider._decreasedStartValue</c> — clamped, no separation bound.</remarks>
    private double DecreasedStartValue => Math.Clamp(StartValueNormalized - SemanticActionUnit, 0.0, 1.0);

    /// <remarks>Flutter's <c>_RenderRangeSlider._increasedEndValue</c> — clamped, no separation bound.</remarks>
    private double IncreasedEndValue => Math.Clamp(EndValueNormalized + SemanticActionUnit, 0.0, 1.0);

    /// <remarks>Flutter's <c>_RenderRangeSlider._decreasedEndValue</c> — separation-bounded, not clamped.</remarks>
    private double DecreasedEndValue
    {
        get
        {
            double decreased = EndValueNormalized - SemanticActionUnit;
            return decreased >= StartValueNormalized + MinThumbSeparationValue ? decreased : EndValueNormalized;
        }
    }

    /// <remarks>Dart's <c>double.parse(value.toStringAsFixed(2))</c>, which rounds half away from zero.</remarks>
    private static double RoundToTwoDecimals(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private void IncreaseStartAction() => InvokeRangeChange(IncreasedStartValue, EndValueNormalized);

    private void DecreaseStartAction() => InvokeRangeChange(DecreasedStartValue, EndValueNormalized);

    private void IncreaseEndAction() => InvokeRangeChange(StartValueNormalized, IncreasedEndValue);

    private void DecreaseEndAction() => InvokeRangeChange(StartValueNormalized, DecreasedEndValue);

    /// <remarks>
    /// Flutter's four range actions call <c>onChanged</c> only — unlike <c>Slider</c>, they never fire
    /// <c>onChangeStart</c>/<c>onChangeEnd</c>.
    /// </remarks>
    private void InvokeRangeChange(double startNormalized, double endNormalized)
    {
        if (!IsInteractive)
        {
            return;
        }

        OnChangedNormalized?.Invoke(new NormalizedRangeValues(startNormalized, endNormalized));
    }

    /// <remarks>Flutter's <c>_RangeSliderState._lerp</c>.</remarks>
    private double Lerp(double normalized) => Min + ((Max - Min) * normalized);

    private string FormatSemanticValue(double normalized)
    {
        return SemanticFormatterCallback is { } formatter
            ? formatter(Lerp(normalized))
            : FormattableString.Invariant($"{Math.Round(normalized * 100.0, MidpointRounding.AwayFromZero)}%");
    }

    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider.describeSemanticsConfiguration</c> sets nothing but the boundary flag:
    /// every readable property lives on the two thumb nodes that
    /// <see cref="AssembleSemanticsNode"/> builds.
    /// </remarks>
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
    }

    /// <remarks>Flutter's <c>_RenderRangeSlider._createSemanticsConfiguration</c>.</remarks>
    private SemanticsConfiguration CreateThumbSemanticsConfiguration(
        double value,
        double increasedValue,
        double decreasedValue,
        Action increaseAction,
        Action decreaseAction,
        bool focused)
    {
        var configuration = new SemanticsConfiguration
        {
            IsEnabled = IsInteractive,
            TextDirection = TextDirection,
            IsSlider = true,
            IsFocused = focused,
            Value = FormatSemanticValue(value),
            IncreasedValue = FormatSemanticValue(increasedValue),
            DecreasedValue = FormatSemanticValue(decreasedValue),
        };

        if (IsInteractive)
        {
            configuration.OnIncrease = increaseAction;
            configuration.OnDecrease = decreaseAction;
        }

        return configuration;
    }

    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider.assembleSemanticsNode</c>. The rects are 48x48 boxes centred on each
    /// thumb; under RTL the two are swapped, so the <em>start</em> node carries the rect that sits where the
    /// end thumb is painted. That looks like a bug but is contractual — Flutter's own RTL test asserts it.
    /// </remarks>
    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        SemanticsConfiguration startConfiguration = CreateThumbSemanticsConfiguration(
            StartValueNormalized,
            IncreasedStartValue,
            DecreasedStartValue,
            IncreaseStartAction,
            DecreaseStartAction,
            focused: _state.StartFocusNode.HasFocus);
        SemanticsConfiguration endConfiguration = CreateThumbSemanticsConfiguration(
            EndValueNormalized,
            IncreasedEndValue,
            DecreasedEndValue,
            IncreaseEndAction,
            DecreaseEndAction,
            focused: _state.EndFocusNode.HasFocus);

        Rect leftRect = ThumbSemanticsRect(_startThumbCenter);
        Rect rightRect = ThumbSemanticsRect(_endThumbCenter);

        _startSemanticsNode ??= Owner!.SemanticsOwner.CreateDetachedNode(this);
        _endSemanticsNode ??= Owner!.SemanticsOwner.CreateDetachedNode(this);

        if (TextDirection == TextDirection.Rtl)
        {
            _startSemanticsNode.Rect = rightRect;
            _endSemanticsNode.Rect = leftRect;
        }
        else
        {
            _startSemanticsNode.Rect = leftRect;
            _endSemanticsNode.Rect = rightRect;
        }

        _startSemanticsNode.UpdateWith(startConfiguration);
        _endSemanticsNode.UpdateWith(endConfiguration);
        node.UpdateWith(config, [_startSemanticsNode, _endSemanticsNode]);
    }

    private static Rect ThumbSemanticsRect(Point center)
    {
        double extent = WidgetConstants.MinInteractiveDimension;
        return new Rect(center.X - (extent / 2.0), center.Y - (extent / 2.0), extent, extent);
    }

    /// <remarks>
    /// Flutter's <c>_RenderRangeSlider.clearSemantics</c> drops both synthesized nodes so a later semantics
    /// pass rebuilds them against the new owner instead of reusing nodes the old owner disposed.
    /// </remarks>
    protected override void ClearOwnSemantics()
    {
        base.ClearOwnSemantics();
        _startSemanticsNode = null;
        _endSemanticsNode = null;
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
        Rect preferredRect = SliderTheme.RangeTrackShape!.GetPreferredRect(
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

    private void PaintThumbOverlay(
        PaintingContext ctx,
        SliderThemeData paintTheme,
        Point center,
        double value,
        bool discrete,
        Animation<double> enableAnimation)
    {
        paintTheme.OverlayShape!.Paint(
            ctx,
            center,
            new ConstantAnimation<double>(1.0),
            enableAnimation,
            discrete,
            null,
            this,
            paintTheme,
            TextDirection,
            value,
            1.0,
            Size);
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
