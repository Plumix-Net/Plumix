using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/range_slider.dart

/// <summary>
/// <see cref="RangeSlider"/> uses this callback to paint the value indicator on the overlay.
/// </summary>
/// <remarks>
/// Since the value indicator is painted on the Overlay; this method paints the value indicator in a
/// <see cref="RenderBox"/> that appears in the <see cref="Overlay"/>.
/// </remarks>
public delegate void PaintRangeValueIndicator(PaintingContext context, Point offset);

/// <summary>
/// A Material Design range slider.
/// </summary>
/// <remarks>
/// Used to select a range from a range of values. The range slider will be disabled if
/// <see cref="OnChanged"/> is null or if the range given by <see cref="Min"/>..<see cref="Max"/> is empty.
/// By default, a slider will be as wide as possible, centered vertically. When given unbounded constraints,
/// it will attempt to make the track 144 pixels wide (including margins on each side) and will shrink-wrap
/// vertically. Requires one of its ancestors to be a <see cref="Material"/> widget and a
/// <see cref="MediaQuery"/> widget.
/// </remarks>
public class RangeSlider : StatefulWidget
{
    /// <summary>Creates a Material Design range slider.</summary>
    /// <remarks>
    /// The <see cref="Min"/> must be less than or equal to the <see cref="Max"/>. The start of
    /// <see cref="Values"/> must be less than or equal to its end, and both must lie in
    /// [<see cref="Min"/>, <see cref="Max"/>]. The <see cref="Divisions"/> parameter must be null or greater
    /// than zero.
    /// </remarks>
    public RangeSlider(
        RangeValues values,
        Action<RangeValues>? onChanged,
        Action<RangeValues>? onChangeStart = null,
        Action<RangeValues>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        RangeLabels? labels = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        WidgetStateProperty<MouseCursor?>? mouseCursor = null,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        EdgeInsetsGeometry? padding = null,
        bool? year2023 = null,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(min <= max, "min <= max");
        DebugAssertions.Assert(values.Start <= values.End, "values.start <= values.end");
        DebugAssertions.Assert(
            values.Start >= min && values.Start <= max,
            "values.start >= min && values.start <= max");
        DebugAssertions.Assert(values.End >= min && values.End <= max, "values.end >= min && values.end <= max");
        DebugAssertions.Assert(divisions == null || divisions > 0, "divisions == null || divisions > 0");
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
        SemanticFormatterCallback = semanticFormatterCallback;
        Padding = padding;
#pragma warning disable CS0618 // Mirrors Flutter's deprecated `year2023` field.
        Year2023 = year2023;
#pragma warning restore CS0618
    }

    /// <summary>The currently selected values for this range slider.</summary>
    public RangeValues Values { get; }

    /// <summary>
    /// Called when the user is selecting a new value for the slider by dragging. If null, the slider will be
    /// displayed as disabled.
    /// </summary>
    public Action<RangeValues>? OnChanged { get; }

    /// <summary>Called when the user starts selecting new values for the slider.</summary>
    public Action<RangeValues>? OnChangeStart { get; }

    /// <summary>Called when the user is done selecting new values for the slider.</summary>
    public Action<RangeValues>? OnChangeEnd { get; }

    /// <summary>The minimum value the user can select. Defaults to 0.0.</summary>
    public double Min { get; }

    /// <summary>The maximum value the user can select. Defaults to 1.0.</summary>
    public double Max { get; }

    /// <summary>The number of discrete divisions. If null, the slider is continuous.</summary>
    public int? Divisions { get; }

    /// <summary>
    /// Labels to show as text in the <see cref="SliderThemeData.RangeValueIndicatorShape"/> when the slider is
    /// active and <see cref="SliderThemeData.ShowValueIndicator"/> is satisfied. If null, then the value
    /// indicator will not be displayed.
    /// </summary>
    public RangeLabels? Labels { get; }

    /// <summary>The color of the track's active segment, i.e. the span of track between the thumbs.</summary>
    public Color? ActiveColor { get; }

    /// <summary>The color of the track's inactive segments.</summary>
    public Color? InactiveColor { get; }

    /// <summary>
    /// The highlight color that's typically used to indicate that the range slider thumb is hovered or
    /// dragged.
    /// </summary>
    public WidgetStateProperty<Color?>? OverlayColor { get; }

    /// <summary>The cursor for a mouse pointer when it enters or is hovering over the widget.</summary>
    public WidgetStateProperty<MouseCursor?>? MouseCursor { get; }

    /// <summary>
    /// The callback used to create a semantic value from the slider's values. Defaults to formatting values
    /// as a percentage.
    /// </summary>
    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    /// <summary>Determines the padding around the <see cref="RangeSlider"/>.</summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>
    /// When true, the <see cref="RangeSlider"/> will use the 2023 Material Design 3 appearance. Defaults to
    /// true. If <see cref="ThemeData.UseMaterial3"/> is false, then this property is ignored.
    /// </summary>
    [Obsolete(
        "Set this flag to false to opt into the 2024 range slider appearance. Defaults to true. "
        + "In the future, this flag will default to false. Use SliderThemeData to customize individual "
        + "properties. This feature was deprecated after v3.30.0-0.1.pre.")]
    public bool? Year2023 { get; }

    // Touch width for the tap boundary of the slider thumbs.
    private const double MinTouchTargetWidth = WidgetConstants.MinInteractiveDimension;

    public override State CreateState() => new RangeSliderState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("valueStart", Values.Start));
        properties.Add(new DoubleProperty("valueEnd", Values.End));
        properties.Add(new ObjectFlagProperty<Action<RangeValues>>("onChanged", OnChanged, ifNull: "disabled"));
        properties.Add(ObjectFlagProperty<Action<RangeValues>>.Has("onChangeStart", OnChangeStart));
        properties.Add(ObjectFlagProperty<Action<RangeValues>>.Has("onChangeEnd", OnChangeEnd));
        properties.Add(new DoubleProperty("min", Min));
        properties.Add(new DoubleProperty("max", Max));
        properties.Add(new IntProperty("divisions", Divisions));
        properties.Add(new StringProperty("labelStart", Labels?.Start));
        properties.Add(new StringProperty("labelEnd", Labels?.End));
        properties.Add(new ColorProperty("activeColor", ActiveColor));
        properties.Add(new ColorProperty("inactiveColor", InactiveColor));
        properties.Add(
            ObjectFlagProperty<SemanticFormatterCallback>.Has("semanticFormatterCallback", SemanticFormatterCallback));
    }

    /// <summary>Flutter's private <c>_RangeSliderState</c>.</summary>
    /// <remarks>
    /// Internal rather than private: <see cref="RenderRangeSlider"/> drives its controllers, and Flutter's own
    /// tests reach <c>startFocusNode</c>/<c>endFocusNode</c> through <c>tester.state(...)</c>.
    /// </remarks>
    internal sealed class RangeSliderState : State<RangeSlider>
    {
        private static readonly TimeSpan EnableAnimationDuration = TimeSpan.FromMilliseconds(75);
        private static readonly TimeSpan ValueIndicatorAnimationDuration = TimeSpan.FromMilliseconds(100);

        public FocusNode StartFocusNode { get; } = new FocusNode();

        public FocusNode EndFocusNode { get; } = new FocusNode();

        // Animation controller that is run when the overlay (a.k.a radial reaction)
        // changes visibility in response to user interaction.
        public AnimationController OverlayController { get; private set; } = null!;

        // Animation controller that is run when the value indicators change visibility.
        public AnimationController ValueIndicatorController { get; private set; } = null!;

        // Animation controller that is run when enabling/disabling the slider.
        public AnimationController EnableController { get; private set; } = null!;

        // Animation controllers that are run when transitioning between one value
        // and the next on a discrete slider.
        public AnimationController StartPositionController { get; private set; } = null!;

        public AnimationController EndPositionController { get; private set; } = null!;

        public GestureTimer? InteractionTimer { get; set; }

        // Value Indicator paint Animation that appears on the Overlay.
        public PaintRangeValueIndicator? PaintTopValueIndicator { get; set; }

        public PaintRangeValueIndicator? PaintBottomValueIndicator { get; set; }

        private bool Enabled => Widget.OnChanged != null;

        private bool _dragging;

        private bool _hovering;

        private bool _showHoverHighlight;

        private void HandleHoverChanged(bool hovering)
        {
            if (hovering != _hovering)
            {
                SetState(() =>
                {
                    _hovering = hovering;
                    _showHoverHighlight = hovering && Enabled;
                });
            }
        }

        // Always keep the ValueIndicator visible on the Overlay; otherwise, it cannot be updated during the
        // build phase.
        private readonly OverlayPortalController _valueIndicatorOverlayPortalController =
            CreateShownOverlayPortalController();

        private static OverlayPortalController CreateShownOverlayPortalController()
        {
            var controller = new OverlayPortalController(debugLabel: "RangeSlider ValueIndicator");
            controller.Show();
            return controller;
        }

        public override void InitState()
        {
            base.InitState();
            OverlayController = new AnimationController(duration: KRadialReactionDuration, vsync: this);
            ValueIndicatorController = new AnimationController(
                duration: ValueIndicatorAnimationDuration,
                vsync: this);
            EnableController = new AnimationController(
                duration: EnableAnimationDuration,
                vsync: this,
                value: Enabled ? 1.0 : 0.0);
            StartPositionController = new AnimationController(
                duration: TimeSpan.Zero,
                vsync: this,
                value: Unlerp(Widget.Values.Start));
            EndPositionController = new AnimationController(
                duration: TimeSpan.Zero,
                vsync: this,
                value: Unlerp(Widget.Values.End));
        }

        public override void DidUpdateWidget(RangeSlider oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            if (oldWidget.OnChanged == Widget.OnChanged)
            {
                return;
            }

            bool wasEnabled = oldWidget.OnChanged != null;
            bool isEnabled = Enabled;
            if (wasEnabled != isEnabled)
            {
                if (isEnabled)
                {
                    EnableController.Forward();
                }
                else
                {
                    EnableController.Reverse();
                }

                _showHoverHighlight = _hovering && isEnabled;
            }
        }

        public override void Dispose()
        {
            InteractionTimer?.Cancel();
            OverlayController.Dispose();
            ValueIndicatorController.Dispose();
            EnableController.Dispose();
            StartPositionController.Dispose();
            EndPositionController.Dispose();
            StartFocusNode.Dispose();
            EndFocusNode.Dispose();
            base.Dispose();
        }

        private void HandleChanged(RangeValues values)
        {
            DebugAssertions.Assert(Enabled, "_enabled");
            RangeValues lerpValues = LerpRangeValues(values);
            if (lerpValues != Widget.Values)
            {
                Widget.OnChanged!(lerpValues);
            }
        }

        private void HandleDragStart(RangeValues values)
        {
            SetState(() => _dragging = true);
            Widget.OnChangeStart?.Invoke(LerpRangeValues(values));
        }

        private void HandleDragEnd(RangeValues values)
        {
            SetState(() => _dragging = false);
            Widget.OnChangeEnd?.Invoke(LerpRangeValues(values));
        }

        // Returns a number between min and max, proportional to value, which must
        // be between 0.0 and 1.0.
        internal double Lerp(double value) => LerpDouble(Widget.Min, Widget.Max, value);

        // Returns a new range value with the start and end lerped.
        private RangeValues LerpRangeValues(RangeValues values)
        {
            return new RangeValues(Lerp(values.Start), Lerp(values.End));
        }

        // Returns a number between 0.0 and 1.0, given a value between min and max.
        private double Unlerp(double value)
        {
            DebugAssertions.Assert(value <= Widget.Max, "value <= widget.max");
            DebugAssertions.Assert(value >= Widget.Min, "value >= widget.min");
            return Widget.Max > Widget.Min ? (value - Widget.Min) / (Widget.Max - Widget.Min) : 0.0;
        }

        // Returns a new range value with the start and end unlerped.
        private RangeValues UnlerpRangeValues(RangeValues values)
        {
            return new RangeValues(Unlerp(values.Start), Unlerp(values.End));
        }

        // Finds the closest thumb. If both thumbs are close to each other and within
        // the touch radius, neither is selected immediately while the drag
        // displacement is zero. The first non-zero displacement determines which
        // thumb is selected: a negative displacement selects the left thumb,
        // a positive one selects the right thumb.
        // If only one or zero thumbs are within the touch radius,
        // the closest one is selected.
        private Thumb? DefaultRangeThumbSelector(
            TextDirection textDirection,
            RangeValues values,
            double tapValue,
            Size thumbSize,
            Size trackSize,
            double dx) // The horizontal delta or displacement of the drag update.
        {
            double touchRadius = Math.Max(thumbSize.Width, MinTouchTargetWidth) / 2;
            bool inStartTouchTarget = Math.Abs(tapValue - values.Start) * trackSize.Width < touchRadius;
            bool inEndTouchTarget = Math.Abs(tapValue - values.End) * trackSize.Width < touchRadius;

            // Use dx if the thumb touch targets overlap. If dx is 0 and the drag
            // position is in both touch targets, no thumb is selected because it is
            // ambiguous to which thumb should be selected. If the dx is non-zero, the
            // thumb selection is determined by the direction of the dx. The left thumb
            // is chosen for negative dx, and the right thumb is chosen for positive dx.
            if (inStartTouchTarget && inEndTouchTarget)
            {
                (bool towardsStart, bool towardsEnd) = textDirection switch
                {
                    TextDirection.Ltr => (dx < 0, dx > 0),
                    TextDirection.Rtl => (dx > 0, dx < 0),
                    _ => throw new ArgumentOutOfRangeException(nameof(textDirection)),
                };
                if (towardsStart)
                {
                    return Thumb.Start;
                }

                if (towardsEnd)
                {
                    return Thumb.End;
                }
            }
            else
            {
                // Choose the closest thumb and snap position.
                if (tapValue * 2 < values.Start + values.End)
                {
                    return Thumb.Start;
                }

                return Thumb.End;
            }

            return null;
        }

        public override Widget Build(BuildContext context)
        {
            MaterialDebug.DebugCheckHasMaterial(context);
            // Dart also asserts debugCheckHasMediaQuery(context), which is not ported; the MediaQuery lookups
            // below throw the MediaQuery error themselves.

            ThemeData theme = Theme.Of(context);
            SliderThemeData sliderTheme = SliderTheme.Of(context);
#pragma warning disable CS0618 // Mirrors Flutter's deprecated `year2023` flag.
            bool year2023 = Widget.Year2023 ?? sliderTheme.Year2023 ?? true;
#pragma warning restore CS0618
            SliderThemeData defaults = theme.UseMaterial3 && !year2023
                ? new RangeSliderDefaultsM3(context)
                : new RangeSliderDefaultsM2(context);

            // If the widget has active or inactive colors specified, then we plug them
            // in to the slider theme as best we can. If the developer wants more
            // control than that, then they need to use a SliderTheme. The default
            // colors come from the ThemeData.colorScheme. These colors, along with
            // the default shapes and text styles are aligned to the Material
            // Guidelines.

            var states = new HashSet<WidgetState>();
            if (!Enabled)
            {
                states.Add(WidgetState.Disabled);
            }

            if (_hovering)
            {
                states.Add(WidgetState.Hovered);
            }

            if (_dragging)
            {
                states.Add(WidgetState.Dragged);
            }

            // The value indicator's color is not the same as the thumb and active track
            // (which can be defined by activeColor) if the
            // RectangularSliderValueIndicatorShape is used. In all other cases, the
            // value indicator is assumed to be the same as the active color.
            RangeSliderValueIndicatorShape valueIndicatorShape =
                sliderTheme.RangeValueIndicatorShape ?? defaults.RangeValueIndicatorShape!;
            Color valueIndicatorColor;
            if (valueIndicatorShape is RectangularRangeSliderValueIndicatorShape)
            {
                valueIndicatorColor =
                    sliderTheme.ValueIndicatorColor
                    ?? Color.AlphaBlend(
                        theme.ColorScheme.OnSurface.WithOpacity(0.60),
                        theme.ColorScheme.Surface.WithOpacity(0.90));
            }
            else
            {
                valueIndicatorColor =
                    Widget.ActiveColor ?? sliderTheme.ValueIndicatorColor ?? defaults.ValueIndicatorColor!;
            }

            Color? EffectiveOverlayColor()
            {
                return Widget.OverlayColor?.Resolve(states)
                       ?? Widget.ActiveColor?.WithOpacity(0.12)
                       ?? WidgetStateProperty<Color?>.ResolveAs(sliderTheme.OverlayColor, states)
                       ?? defaults.OverlayColor;
            }

            sliderTheme = sliderTheme.CopyWith(
                trackHeight: sliderTheme.TrackHeight ?? defaults.TrackHeight,
                activeTrackColor:
                    Widget.ActiveColor ?? sliderTheme.ActiveTrackColor ?? defaults.ActiveTrackColor,
                inactiveTrackColor:
                    Widget.InactiveColor ?? sliderTheme.InactiveTrackColor ?? defaults.InactiveTrackColor,
                disabledActiveTrackColor:
                    sliderTheme.DisabledActiveTrackColor ?? defaults.DisabledActiveTrackColor,
                disabledInactiveTrackColor:
                    sliderTheme.DisabledInactiveTrackColor ?? defaults.DisabledInactiveTrackColor,
                activeTickMarkColor:
                    Widget.InactiveColor ?? sliderTheme.ActiveTickMarkColor ?? defaults.ActiveTickMarkColor,
                inactiveTickMarkColor:
                    Widget.ActiveColor ?? sliderTheme.InactiveTickMarkColor ?? defaults.InactiveTickMarkColor,
                disabledActiveTickMarkColor:
                    sliderTheme.DisabledActiveTickMarkColor ?? defaults.DisabledActiveTickMarkColor,
                disabledInactiveTickMarkColor:
                    sliderTheme.DisabledInactiveTickMarkColor ?? defaults.DisabledInactiveTickMarkColor,
                thumbColor: Widget.ActiveColor ?? sliderTheme.ThumbColor ?? defaults.ThumbColor,
                overlappingShapeStrokeColor:
                    sliderTheme.OverlappingShapeStrokeColor ?? defaults.OverlappingShapeStrokeColor,
                disabledThumbColor: sliderTheme.DisabledThumbColor ?? defaults.DisabledThumbColor,
                overlayColor: EffectiveOverlayColor(),
                valueIndicatorColor: valueIndicatorColor,
                rangeTrackShape: sliderTheme.RangeTrackShape ?? defaults.RangeTrackShape,
                rangeTickMarkShape: sliderTheme.RangeTickMarkShape ?? defaults.RangeTickMarkShape,
                rangeThumbShape: sliderTheme.RangeThumbShape ?? defaults.RangeThumbShape,
                overlayShape: sliderTheme.OverlayShape ?? defaults.OverlayShape,
                rangeValueIndicatorShape: valueIndicatorShape,
                showValueIndicator: sliderTheme.ShowValueIndicator ?? defaults.ShowValueIndicator,
                valueIndicatorTextStyle:
                    sliderTheme.ValueIndicatorTextStyle ?? defaults.ValueIndicatorTextStyle,
                minThumbSeparation: sliderTheme.MinThumbSeparation ?? defaults.MinThumbSeparation,
                thumbSelector: sliderTheme.ThumbSelector ?? new RangeThumbSelector(DefaultRangeThumbSelector),
                padding: Widget.Padding ?? sliderTheme.Padding,
                thumbSize: sliderTheme.ThumbSize ?? defaults.ThumbSize,
                trackGap: sliderTheme.TrackGap ?? defaults.TrackGap);
            MouseCursor effectiveMouseCursor =
                Widget.MouseCursor?.Resolve(states)
                ?? sliderTheme.MouseCursor?.Resolve(states)
                ?? WidgetStateMouseCursor.Clickable.Resolve(states)!;

            // This size is used as the max bounds for the painting of the value
            // indicators. It must be kept in sync with the function with the same name
            // in slider.dart.
            Size ScreenSize() => MediaQuery.SizeOf(context);

            double fontSize = sliderTheme.ValueIndicatorTextStyle?.FontSize ?? TextDefaults.DefaultFontSize;
            double fontSizeToScale = fontSize == 0.0 ? TextDefaults.DefaultFontSize : fontSize;
            double effectiveTextScale =
                MediaQuery.TextScalerOf(context).Scale(fontSizeToScale) / fontSizeToScale;

            SliderThemeData effectiveSliderTheme = sliderTheme;
            Widget result = new CompositedTransformTarget(
                link: _layerLink,
                child: new OverlayPortal(
                    controller: _valueIndicatorOverlayPortalController,
                    overlayChildBuilder: (BuildContext overlayContext) =>
                        BuildValueIndicator(effectiveSliderTheme.ShowValueIndicator!.Value),
                    child: new RangeSliderRenderObjectWidget(
                        values: UnlerpRangeValues(Widget.Values),
                        divisions: Widget.Divisions,
                        labels: Widget.Labels,
                        sliderTheme: sliderTheme,
                        textScaleFactor: effectiveTextScale,
                        screenSize: ScreenSize(),
                        onChanged: Enabled && (Widget.Max > Widget.Min) ? HandleChanged : null,
                        onChangeStart: HandleDragStart,
                        onChangeEnd: HandleDragEnd,
                        state: this,
                        semanticFormatterCallback: Widget.SemanticFormatterCallback,
                        hovering: _showHoverHighlight)));

            EdgeInsetsGeometry? padding = Widget.Padding ?? sliderTheme.Padding;
            if (padding != null)
            {
                result = new Padding(padding.Value, child: result);
            }

            return new Stack(
                children:
                [
                    // Adds two invisible focus nodes to the range slider for its two thumbs.
                    new Row(
                        children:
                        [
                            new Focus(
                                focusNode: StartFocusNode,
                                includeSemantics: false,
                                child: SizedBox.Shrink()),
                            new Focus(focusNode: EndFocusNode, includeSemantics: false, child: SizedBox.Shrink()),
                        ]),
                    new MouseRegion(
                        onEnter: _ => HandleHoverChanged(true),
                        onExit: _ => HandleHoverChanged(false),
                        cursor: effectiveMouseCursor,
                        child: result),
                ]);
        }

        private readonly LayerLink _layerLink = new();

        private Widget BuildValueIndicator(ShowValueIndicator showValueIndicator)
        {
            Widget valueIndicator = new CompositedTransformFollower(
                link: _layerLink,
                child: new ValueIndicatorRenderObjectWidget(state: this));
#pragma warning disable CS0618 // Dart's switch lists the deprecated `ShowValueIndicator.always`.
            return showValueIndicator switch
            {
                ShowValueIndicator.Never => SizedBox.Shrink(),
                ShowValueIndicator.OnlyForDiscrete =>
                    Widget.Divisions != null ? valueIndicator : SizedBox.Shrink(),
                ShowValueIndicator.OnlyForContinuous =>
                    Widget.Divisions == null ? valueIndicator : SizedBox.Shrink(),
                ShowValueIndicator.AlwaysVisible
                    or ShowValueIndicator.Always
                    or ShowValueIndicator.OnDrag => valueIndicator,
                _ => throw new ArgumentOutOfRangeException(nameof(showValueIndicator)),
            };
#pragma warning restore CS0618
        }
    }

    /// <summary>Flutter's private <c>_ValueIndicatorRenderObjectWidget</c> (range variant).</summary>
    private sealed class ValueIndicatorRenderObjectWidget : LeafRenderObjectWidget
    {
        public ValueIndicatorRenderObjectWidget(RangeSliderState state)
        {
            State = state;
        }

        public RangeSliderState State { get; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderValueIndicator(state: State);
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderValueIndicator)renderObject)._state = State;
        }
    }

    /// <summary>Flutter's private <c>_RenderValueIndicator</c> (range variant).</summary>
    /// <remarks>
    /// Dart mixes in <c>RelayoutWhenSystemFontsChangeMixin</c>, which Plumix has not ported; this render object
    /// has no <c>systemFontsDidChange</c> override of its own, so nothing is lost beyond the relayout.
    /// </remarks>
    private sealed class RenderValueIndicator : RenderBox
    {
        public RenderValueIndicator(RangeSliderState state)
        {
            _state = state;
            _valueIndicatorAnimation = new CurvedAnimation(
                parent: _state.ValueIndicatorController,
                curve: Curves.FastOutSlowIn);
        }

        private readonly CurvedAnimation _valueIndicatorAnimation;

        internal RangeSliderState _state;

        protected override bool SizedByParent => true;

        protected override void OnAttach()
        {
            base.OnAttach();
            _valueIndicatorAnimation.AddListener(MarkNeedsPaint);
            _state.StartPositionController.AddListener(MarkNeedsPaint);
            _state.EndPositionController.AddListener(MarkNeedsPaint);
        }

        protected override void OnDetach()
        {
            _valueIndicatorAnimation.RemoveListener(MarkNeedsPaint);
            _state.StartPositionController.RemoveListener(MarkNeedsPaint);
            _state.EndPositionController.RemoveListener(MarkNeedsPaint);
            base.OnDetach();
        }

        public override void Paint(PaintingContext context, Point offset)
        {
            _state.PaintBottomValueIndicator?.Invoke(context, offset);
            _state.PaintTopValueIndicator?.Invoke(context, offset);
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints)
        {
            return constraints.Smallest;
        }

        public override void Dispose()
        {
            _valueIndicatorAnimation.Dispose();
            base.Dispose();
        }
    }

    /// <summary>Material's <c>kRadialReactionDuration</c> (constants.dart).</summary>
    private static readonly TimeSpan KRadialReactionDuration = TimeSpan.FromMilliseconds(100);

    /// <summary>dart:ui's <c>lerpDouble</c> for non-null operands.</summary>
    private static double LerpDouble(double a, double b, double t)
    {
        if (a == b || (double.IsNaN(a) && double.IsNaN(b)))
        {
            return a;
        }

        return (a * (1.0 - t)) + (b * t);
    }
}

/// <summary>Flutter's private <c>_RangeSliderRenderObjectWidget</c>.</summary>
internal sealed class RangeSliderRenderObjectWidget : LeafRenderObjectWidget
{
    public RangeSliderRenderObjectWidget(
        RangeValues values,
        int? divisions,
        RangeLabels? labels,
        SliderThemeData sliderTheme,
        double textScaleFactor,
        Size screenSize,
        Action<RangeValues>? onChanged,
        Action<RangeValues>? onChangeStart,
        Action<RangeValues>? onChangeEnd,
        RangeSlider.RangeSliderState state,
        SemanticFormatterCallback? semanticFormatterCallback,
        bool hovering)
    {
        Values = values;
        Divisions = divisions;
        Labels = labels;
        SliderTheme = sliderTheme;
        TextScaleFactor = textScaleFactor;
        ScreenSize = screenSize;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        State = state;
        SemanticFormatterCallback = semanticFormatterCallback;
        Hovering = hovering;
    }

    public RangeValues Values { get; }

    public int? Divisions { get; }

    public RangeLabels? Labels { get; }

    public SliderThemeData SliderTheme { get; }

    public double TextScaleFactor { get; }

    public Size ScreenSize { get; }

    public Action<RangeValues>? OnChanged { get; }

    public Action<RangeValues>? OnChangeStart { get; }

    public Action<RangeValues>? OnChangeEnd { get; }

    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    public RangeSlider.RangeSliderState State { get; }

    public bool Hovering { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderRangeSlider(
            values: Values,
            divisions: Divisions,
            labels: Labels,
            sliderTheme: SliderTheme,
            theme: Theme.Of(context),
            textScaleFactor: TextScaleFactor,
            screenSize: ScreenSize,
            onChanged: OnChanged,
            onChangeStart: OnChangeStart,
            onChangeEnd: OnChangeEnd,
            state: State,
            textDirection: Directionality.Of(context),
            semanticFormatterCallback: SemanticFormatterCallback,
            platform: Theme.Of(context).Platform,
            hovering: Hovering,
            gestureSettings: MediaQuery.GestureSettingsOf(context));
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var slider = (RenderRangeSlider)renderObject;
        // We should update the `divisions` ahead of `values`, because the `values`
        // setter dependent on the `divisions`.
        slider.Divisions = Divisions;
        slider.Values = Values;
        slider.Labels = Labels;
        slider.SliderTheme = SliderTheme;
        slider.Theme = Theme.Of(context);
        slider.TextScaleFactor = TextScaleFactor;
        slider.ScreenSize = ScreenSize;
        slider.OnChanged = OnChanged;
        slider.OnChangeStart = OnChangeStart;
        slider.OnChangeEnd = OnChangeEnd;
        slider.TextDirection = Directionality.Of(context);
        slider.SemanticFormatterCallback = SemanticFormatterCallback;
        slider.Platform = Theme.Of(context).Platform;
        slider.Hovering = Hovering;
        slider.GestureSettings = MediaQuery.GestureSettingsOf(context);
    }
}

/// <summary>Flutter's private <c>_RenderRangeSlider</c>.</summary>
/// <remarks>
/// Dart mixes in <c>RelayoutWhenSystemFontsChangeMixin</c>, which Plumix has not ported (tracked in
/// docs/ai/BACKLOG.md); <see cref="SystemFontsDidChange"/> keeps Dart's body but nothing calls it yet.
/// </remarks>
internal sealed class RenderRangeSlider : RenderBox
{
    public RenderRangeSlider(
        RangeValues values,
        int? divisions,
        RangeLabels? labels,
        SliderThemeData sliderTheme,
        ThemeData? theme,
        double textScaleFactor,
        Size screenSize,
        TargetPlatform platform,
        Action<RangeValues>? onChanged,
        SemanticFormatterCallback? semanticFormatterCallback,
        Action<RangeValues>? onChangeStart,
        Action<RangeValues>? onChangeEnd,
        RangeSlider.RangeSliderState state,
        TextDirection textDirection,
        bool hovering,
        DeviceGestureSettings? gestureSettings)
    {
        DebugAssertions.Assert(
            values.Start >= 0.0 && values.Start <= 1.0,
            "_values.start >= 0.0 && _values.start <= 1.0");
        DebugAssertions.Assert(values.End >= 0.0 && values.End <= 1.0, "_values.end >= 0.0 && _values.end <= 1.0");
        _values = values;
        _divisions = divisions;
        _labels = labels;
        _sliderTheme = sliderTheme;
        _theme = theme;
        _textScaleFactor = textScaleFactor;
        _screenSize = screenSize;
        _platform = platform;
        _onChanged = onChanged;
        _semanticFormatterCallback = semanticFormatterCallback;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        _state = state;
        _textDirection = textDirection;
        _hovering = hovering;
        UpdateLabelPainters();
        var team = new GestureArenaTeam();
        _drag = new HorizontalDragGestureRecognizer
        {
            Team = team,
            OnStart = HandleDragStart,
            OnUpdate = HandleDragUpdate,
            OnEnd = HandleDragEnd,
            OnCancel = HandleDragCancel,
            GestureSettings = gestureSettings,
        };
        _tap = new TapGestureRecognizer
        {
            Team = team,
            OnTapDown = HandleTapDown,
            OnTapUp = HandleTapUp,
            GestureSettings = gestureSettings,
        };
        _overlayAnimation = new CurvedAnimation(parent: _state.OverlayController, curve: Curves.FastOutSlowIn);
        _valueIndicatorAnimation = new CurvedAnimation(
            parent: _state.ValueIndicatorController,
            curve: Curves.FastOutSlowIn);
        _enableAnimation = new CurvedAnimation(parent: _state.EnableController, curve: Curves.EaseInOut);
    }

    // Keep track of the last selected thumb so they can be drawn in the
    // right order.
    private Thumb? _lastThumbSelection;

    private static readonly TimeSpan PositionAnimationDuration = TimeSpan.FromMilliseconds(75);

    // This value is the touch target, 48, multiplied by 3.
    private const double MinPreferredTrackWidth = 144.0;

    // Compute the largest width and height needed to paint the slider shapes,
    // other than the track shape. It is assumed that these shapes are vertically
    // centered on the track.
    private double MaxSliderPartWidth => SliderPartSizes.Select(static size => size.Width).Aggregate(Math.Max);

    private double MaxSliderPartHeight => SliderPartSizes.Select(static size => size.Height).Aggregate(Math.Max);

    private double ThumbSizeHeight => _sliderTheme.RangeThumbShape!.GetPreferredSize(IsEnabled, IsDiscrete).Height;

    private double OverlayHeight => _sliderTheme.OverlayShape!.GetPreferredSize(IsEnabled, IsDiscrete).Height;

    private List<Size> SliderPartSizes =>
    [
        new Size(
            _sliderTheme.OverlayShape!.GetPreferredSize(IsEnabled, IsDiscrete).Width,
            _sliderTheme.Padding != null ? ThumbSizeHeight : OverlayHeight),
        _sliderTheme.RangeThumbShape!.GetPreferredSize(IsEnabled, IsDiscrete),
        _sliderTheme.RangeTickMarkShape!.GetPreferredSize(isEnabled: IsEnabled, sliderTheme: SliderTheme),
    ];

    private double? MinPreferredTrackHeight => _sliderTheme.TrackHeight;

    // This rect is used in gesture calculations, where the gesture coordinates
    // are relative to the sliders origin. Therefore, the offset is passed as
    // (0,0).
    private Rect TrackRect => _sliderTheme.RangeTrackShape!.GetPreferredRect(
        parentBox: this,
        sliderTheme: _sliderTheme,
        isDiscrete: false);

    private static readonly TimeSpan MinimumInteractionTime = TimeSpan.FromMilliseconds(500);

    private readonly RangeSlider.RangeSliderState _state;
    private readonly CurvedAnimation _overlayAnimation;
    private readonly CurvedAnimation _valueIndicatorAnimation;
    private readonly CurvedAnimation _enableAnimation;
    private readonly TextPainter _startLabelPainter = new();
    private readonly TextPainter _endLabelPainter = new();
    private readonly HorizontalDragGestureRecognizer _drag;
    private readonly TapGestureRecognizer _tap;
    private bool _active;
    private RangeValues _newValues = null!;
    private Point _startThumbCenter;
    private Point _endThumbCenter;

    internal Rect? OverlayStartRect { get; set; }

    internal Rect? OverlayEndRect { get; set; }

    public bool IsEnabled => OnChanged != null;

    public bool IsDiscrete => Divisions != null && Divisions > 0;

    private double MinThumbSeparationValue =>
        IsDiscrete ? 0 : SliderTheme.MinThumbSeparation!.Value / TrackRect.Width;

    public RangeValues Values
    {
        get => _values;
        set
        {
            DebugAssertions.Assert(
                value.Start >= 0.0 && value.Start <= 1.0,
                "newValues.start >= 0.0 && newValues.start <= 1.0");
            DebugAssertions.Assert(
                value.End >= 0.0 && value.End <= 1.0,
                "newValues.end >= 0.0 && newValues.end <= 1.0");
            DebugAssertions.Assert(value.Start <= value.End, "newValues.start <= newValues.end");
            RangeValues convertedValues = IsDiscrete ? DiscretizeRangeValues(value) : value;
            if (convertedValues == _values)
            {
                return;
            }

            _values = convertedValues;
            if (IsDiscrete)
            {
                // Reset the duration to match the distance that we're traveling, so that
                // whatever the distance, we still do it in _positionAnimationDuration,
                // and if we get re-targeted in the middle, it still takes that long to
                // get to the new location.
                double startDistance = Math.Abs(_values.Start - _state.StartPositionController.Value);
                _state.StartPositionController.Duration = startDistance != 0.0
                    ? MultiplyDuration(PositionAnimationDuration, 1.0 / startDistance)
                    : TimeSpan.Zero;
                _state.StartPositionController.AnimateTo(_values.Start, curve: Curves.EaseInOut);
                double endDistance = Math.Abs(_values.End - _state.EndPositionController.Value);
                _state.EndPositionController.Duration = endDistance != 0.0
                    ? MultiplyDuration(PositionAnimationDuration, 1.0 / endDistance)
                    : TimeSpan.Zero;
                _state.EndPositionController.AnimateTo(_values.End, curve: Curves.EaseInOut);
            }
            else
            {
                _state.StartPositionController.SetValue(convertedValues.Start);
                _state.EndPositionController.SetValue(convertedValues.End);
            }

            MarkNeedsSemanticsUpdate();
        }
    }

    private RangeValues _values;

    private TargetPlatform _platform;

    public TargetPlatform Platform
    {
        get => _platform;
        set
        {
            if (_platform == value)
            {
                return;
            }

            _platform = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public DeviceGestureSettings? GestureSettings
    {
        get => _drag.GestureSettings;
        set
        {
            _drag.GestureSettings = value;
            _tap.GestureSettings = value;
        }
    }

    private SemanticFormatterCallback? _semanticFormatterCallback;

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

    private int? _divisions;

    public RangeLabels? Labels
    {
        get => _labels;
        set
        {
            if (value == _labels)
            {
                return;
            }

            _labels = value;
            UpdateLabelPainters();
        }
    }

    private RangeLabels? _labels;

    public SliderThemeData SliderTheme
    {
        get => _sliderTheme;
        set
        {
            if (value == _sliderTheme)
            {
                return;
            }

            _sliderTheme = value;
            MarkNeedsPaint();
        }
    }

    private SliderThemeData _sliderTheme;

    public ThemeData? Theme
    {
        get => _theme;
        set
        {
            if (value == _theme)
            {
                return;
            }

            _theme = value;
            MarkNeedsPaint();
        }
    }

    private ThemeData? _theme;

    public double TextScaleFactor
    {
        get => _textScaleFactor;
        set
        {
            if (value == _textScaleFactor)
            {
                return;
            }

            _textScaleFactor = value;
            UpdateLabelPainters();
        }
    }

    private double _textScaleFactor;

    public Size ScreenSize
    {
        get => _screenSize;
        set
        {
            if (value == ScreenSize)
            {
                return;
            }

            _screenSize = value;
            MarkNeedsPaint();
        }
    }

    private Size _screenSize;

    public Action<RangeValues>? OnChanged
    {
        get => _onChanged;
        set
        {
            if (value == _onChanged)
            {
                return;
            }

            bool wasEnabled = IsEnabled;
            _onChanged = value;
            if (wasEnabled != IsEnabled)
            {
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    private Action<RangeValues>? _onChanged;

    public Action<RangeValues>? OnChangeStart { get; set; }

    public Action<RangeValues>? OnChangeEnd { get; set; }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (value == _textDirection)
            {
                return;
            }

            _textDirection = value;
            UpdateLabelPainters();
        }
    }

    private TextDirection _textDirection;

    /// <summary>True if this slider is being hovered over by a pointer.</summary>
    public bool Hovering
    {
        get => _hovering;
        set
        {
            if (value == _hovering)
            {
                return;
            }

            _hovering = value;
            UpdateForHover(_hovering);
        }
    }

    private bool _hovering;

    /// <summary>True if the slider is interactive and the start thumb is being hovered over by a pointer.</summary>
    private bool _hoveringStartThumb;

    public bool HoveringStartThumb
    {
        get => _hoveringStartThumb;
        set
        {
            if (value == _hoveringStartThumb)
            {
                return;
            }

            _hoveringStartThumb = value;
            UpdateForHover(_hovering);
        }
    }

    /// <summary>True if the slider is interactive and the end thumb is being hovered over by a pointer.</summary>
    private bool _hoveringEndThumb;

    public bool HoveringEndThumb
    {
        get => _hoveringEndThumb;
        set
        {
            if (value == _hoveringEndThumb)
            {
                return;
            }

            _hoveringEndThumb = value;
            UpdateForHover(_hovering);
        }
    }

    private void UpdateForHover(bool hovered)
    {
        // Only show overlay when pointer is hovering the thumb.
        if (hovered && (HoveringStartThumb || HoveringEndThumb))
        {
            _state.OverlayController.Forward();
        }
        else
        {
            _state.OverlayController.Reverse();
        }
    }

    public bool ShouldAlwaysShowValueIndicator =>
        _sliderTheme.ShowValueIndicator == ShowValueIndicator.AlwaysVisible;

#pragma warning disable CS0618 // Dart's switch lists the deprecated `ShowValueIndicator.always`.
    public bool ShouldShowValueIndicatorWhenDragged => _sliderTheme.ShowValueIndicator!.Value switch
    {
        ShowValueIndicator.OnlyForDiscrete => IsDiscrete,
        ShowValueIndicator.OnlyForContinuous => !IsDiscrete,
        ShowValueIndicator.AlwaysVisible or ShowValueIndicator.Always or ShowValueIndicator.OnDrag => true,
        ShowValueIndicator.Never => false,
        _ => throw new InvalidOperationException(),
    };
#pragma warning restore CS0618

    private Size ThumbSize => _sliderTheme.RangeThumbShape!.GetPreferredSize(IsEnabled, IsDiscrete);

    private double AdjustmentUnit
    {
        get
        {
            switch (_platform)
            {
                case TargetPlatform.IOS:
                    // Matches iOS implementation of material slider.
                    return 0.1;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                default:
                    // Matches Android implementation of material slider.
                    return 0.05;
            }
        }
    }

    private void UpdateLabelPainters()
    {
        UpdateLabelPainter(Thumb.Start);
        UpdateLabelPainter(Thumb.End);
    }

    private void UpdateLabelPainter(Thumb thumb)
    {
        RangeLabels? labels = Labels;
        if (labels == null)
        {
            return;
        }

        (string text, TextPainter labelPainter) = thumb switch
        {
            Thumb.Start => (labels.Start, _startLabelPainter),
            Thumb.End => (labels.End, _endLabelPainter),
            _ => throw new ArgumentOutOfRangeException(nameof(thumb)),
        };

        labelPainter.Text = new TextSpan(style: _sliderTheme.ValueIndicatorTextStyle, text: text);
        labelPainter.TextDirection = TextDirection;
        labelPainter.TextScaler = TextScaler.Linear(TextScaleFactor);
        labelPainter.Layout();
        // Changing the textDirection can result in the layout changing, because the
        // bidi algorithm might line up the glyphs differently which can result in
        // different ligatures, different shapes, etc. So we always markNeedsLayout.
        MarkNeedsLayout();
    }

    /// <summary>Dart's <c>systemFontsDidChange</c> override.</summary>
    /// <remarks>
    /// <c>RelayoutWhenSystemFontsChangeMixin</c> is not ported, so nothing calls this yet.
    /// </remarks>
    internal void SystemFontsDidChange()
    {
        _startLabelPainter.MarkNeedsLayout();
        _endLabelPainter.MarkNeedsLayout();
        UpdateLabelPainters();
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _overlayAnimation.AddListener(MarkNeedsPaint);
        _valueIndicatorAnimation.AddListener(MarkNeedsPaint);
        _enableAnimation.AddListener(MarkNeedsPaint);
        _state.StartPositionController.AddListener(MarkNeedsPaint);
        _state.EndPositionController.AddListener(MarkNeedsPaint);
        _state.StartFocusNode.AddListener(MarkNeedsPaint);
        _state.StartFocusNode.AddListener(MarkNeedsSemanticsUpdate);
        _state.EndFocusNode.AddListener(MarkNeedsPaint);
        _state.EndFocusNode.AddListener(MarkNeedsSemanticsUpdate);
    }

    protected override void OnDetach()
    {
        _overlayAnimation.RemoveListener(MarkNeedsPaint);
        _valueIndicatorAnimation.RemoveListener(MarkNeedsPaint);
        _enableAnimation.RemoveListener(MarkNeedsPaint);
        _state.StartPositionController.RemoveListener(MarkNeedsPaint);
        _state.EndPositionController.RemoveListener(MarkNeedsPaint);
        _state.StartFocusNode.RemoveListener(MarkNeedsPaint);
        _state.StartFocusNode.RemoveListener(MarkNeedsSemanticsUpdate);
        _state.EndFocusNode.RemoveListener(MarkNeedsPaint);
        _state.EndFocusNode.RemoveListener(MarkNeedsSemanticsUpdate);
        base.OnDetach();
    }

    public override void Dispose()
    {
        _drag.Dispose();
        _tap.Dispose();
        _startLabelPainter.Dispose();
        _endLabelPainter.Dispose();
        _enableAnimation.Dispose();
        _valueIndicatorAnimation.Dispose();
        _overlayAnimation.Dispose();
        base.Dispose();
    }

    private double GetValueFromVisualPosition(double visualPosition)
    {
        return TextDirection switch
        {
            TextDirection.Rtl => 1.0 - visualPosition,
            TextDirection.Ltr => visualPosition,
            _ => throw new InvalidOperationException(),
        };
    }

    private double GetValueFromGlobalPosition(Point globalPosition)
    {
        double visualPosition = (GlobalToLocal(globalPosition).X - TrackRect.Left) / TrackRect.Width;
        return GetValueFromVisualPosition(visualPosition);
    }

    private double Discretize(double value)
    {
        double result = BoxConstraints.ClampDouble(value, 0.0, 1.0);
        if (IsDiscrete)
        {
            result = DartRound(result * Divisions!.Value) / Divisions!.Value;
        }

        return result;
    }

    private RangeValues DiscretizeRangeValues(RangeValues values)
    {
        return new RangeValues(Discretize(values.Start), Discretize(values.End));
    }

    private void StartInteraction(Point globalPosition)
    {
        if (_active)
        {
            return;
        }

        double tapValue = BoxConstraints.ClampDouble(GetValueFromGlobalPosition(globalPosition), 0.0, 1.0);
        _lastThumbSelection = SliderTheme.ThumbSelector!(
            TextDirection,
            Values,
            tapValue,
            ThumbSize,
            Size,
            0);

        if (_lastThumbSelection != null)
        {
            switch (_lastThumbSelection.Value)
            {
                case Thumb.Start:
                    _state.StartFocusNode.RequestFocus();
                    break;
                case Thumb.End:
                    _state.EndFocusNode.RequestFocus();
                    break;
            }

            _active = true;
            // We supply the *current* values as the start locations, so that if we have
            // a tap, it consists of a call to onChangeStart with the previous value and
            // a call to onChangeEnd with the new value.
            RangeValues currentValues = DiscretizeRangeValues(Values);
            _newValues = _lastThumbSelection.Value switch
            {
                Thumb.Start => new RangeValues(tapValue, currentValues.End),
                Thumb.End => new RangeValues(currentValues.Start, tapValue),
                _ => throw new InvalidOperationException(),
            };
            UpdateLabelPainter(_lastThumbSelection.Value);

            OnChangeStart?.Invoke(currentValues);

            OnChanged!(DiscretizeRangeValues(_newValues));

            _state.OverlayController.Forward();
            if (ShouldShowValueIndicatorWhenDragged)
            {
                _state.ValueIndicatorController.Forward();
                _state.InteractionTimer?.Cancel();
                _state.InteractionTimer = GestureTimer.Start(
                    MultiplyDuration(MinimumInteractionTime, Scheduler.TimeDilation),
                    () =>
                    {
                        _state.InteractionTimer = null;
                        if (!_active && _state.ValueIndicatorController.Status.IsCompleted())
                        {
                            _state.ValueIndicatorController.Reverse();
                        }
                    });
            }
        }
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!_state.Mounted)
        {
            return;
        }

        double dragValue = GetValueFromGlobalPosition(details.GlobalPosition);

        // If no selection has been made yet, test for thumb selection again now
        // that the value of dx can be non-zero. If this is the first selection of
        // the interaction, then onChangeStart must be called.
        bool shouldCallOnChangeStart = false;
        if (_lastThumbSelection == null)
        {
            _lastThumbSelection = SliderTheme.ThumbSelector!(
                TextDirection,
                Values,
                dragValue,
                ThumbSize,
                Size,
                details.Delta.X);
            if (_lastThumbSelection != null)
            {
                shouldCallOnChangeStart = true;
                _active = true;
                _state.OverlayController.Forward();
                if (ShouldShowValueIndicatorWhenDragged)
                {
                    _state.ValueIndicatorController.Forward();
                }
            }
        }

        if (IsEnabled && _lastThumbSelection != null)
        {
            RangeValues currentValues = DiscretizeRangeValues(Values);
            if (OnChangeStart != null && shouldCallOnChangeStart)
            {
                OnChangeStart(currentValues);
            }

            double currentDragValue = Discretize(dragValue);

            _newValues = _lastThumbSelection.Value switch
            {
                Thumb.Start => new RangeValues(
                    Math.Min(currentDragValue, currentValues.End - MinThumbSeparationValue),
                    currentValues.End),
                Thumb.End => new RangeValues(
                    currentValues.Start,
                    Math.Max(currentDragValue, currentValues.Start + MinThumbSeparationValue)),
                _ => throw new InvalidOperationException(),
            };
            OnChanged!(DiscretizeRangeValues(_newValues));
        }
    }

    private void EndInteraction()
    {
        if (!_state.Mounted)
        {
            return;
        }

        if (ShouldShowValueIndicatorWhenDragged && _state.InteractionTimer == null)
        {
            _state.ValueIndicatorController.Reverse();
        }

        if (_active && _state.Mounted && _lastThumbSelection != null)
        {
            RangeValues discreteValues = DiscretizeRangeValues(_newValues);
            OnChangeEnd?.Invoke(discreteValues);
            _active = false;
        }

        _state.OverlayController.Reverse();
    }

    private void HandleDragStart(DragStartDetails details)
    {
        StartInteraction(details.GlobalPosition);
    }

    private void HandleDragEnd(DragEndDetails details)
    {
        EndInteraction();
    }

    private void HandleDragCancel()
    {
        EndInteraction();
    }

    private void HandleTapDown(TapDownDetails details)
    {
        StartInteraction(details.GlobalPosition);
    }

    private void HandleTapUp(TapUpDetails details)
    {
        EndInteraction();
    }

    protected override bool HitTestSelf(Point position) => true;

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (Constants.KDebugMode)
        {
            DebugHandleEvent(@event, entry);
        }

        if (@event is PointerDownEvent downEvent && IsEnabled)
        {
            // We need to add the drag first so that it has priority.
            _drag.AddPointer(downEvent);
            _tap.AddPointer(downEvent);
        }

        if (IsEnabled)
        {
            if (OverlayStartRect != null)
            {
                HoveringStartThumb = OverlayStartRect.Value.ContainsHalfOpen(@event.LocalPosition);
            }

            if (OverlayEndRect != null)
            {
                HoveringEndThumb = OverlayEndRect.Value.ContainsHalfOpen(@event.LocalPosition);
            }
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        MinPreferredTrackWidth + MaxSliderPartWidth;

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        MinPreferredTrackWidth + MaxSliderPartWidth;

    protected override double ComputeMinIntrinsicHeight(double width) =>
        Math.Max(MinPreferredTrackHeight!.Value, MaxSliderPartHeight);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        Math.Max(MinPreferredTrackHeight!.Value, MaxSliderPartHeight);

    protected override bool SizedByParent => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return new Size(
            constraints.HasBoundedWidth
                ? constraints.MaxWidth
                : MinPreferredTrackWidth + MaxSliderPartWidth,
            constraints.HasBoundedHeight
                ? constraints.MaxHeight
                : Math.Max(MinPreferredTrackHeight!.Value, MaxSliderPartHeight));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        double startValue = _state.StartPositionController.Value;
        double endValue = _state.EndPositionController.Value;

        // The visual position is the position of the thumb from 0 to 1 from left
        // to right. In left to right, this is the same as the value, but it is
        // reversed for right to left text.
        (double startVisualPosition, double endVisualPosition) = TextDirection switch
        {
            TextDirection.Rtl => (1.0 - startValue, 1.0 - endValue),
            TextDirection.Ltr => (startValue, endValue),
            _ => throw new InvalidOperationException(),
        };

        Rect trackRect = _sliderTheme.RangeTrackShape!.GetPreferredRect(
            parentBox: this,
            offset: offset,
            sliderTheme: _sliderTheme,
            isDiscrete: IsDiscrete);
        double padding = _sliderTheme.RangeTrackShape!.IsRounded ? trackRect.Height : 0.0;
        double thumbYOffset = trackRect.Center.Y;
        double startThumbPosition = IsDiscrete
            ? trackRect.Left + (startVisualPosition * (trackRect.Width - padding)) + (padding / 2)
            : trackRect.Left + (startVisualPosition * trackRect.Width);
        double endThumbPosition = IsDiscrete
            ? trackRect.Left + (endVisualPosition * (trackRect.Width - padding)) + (padding / 2)
            : trackRect.Left + (endVisualPosition * trackRect.Width);
        Size thumbPreferredSize = _sliderTheme.RangeThumbShape!.GetPreferredSize(IsEnabled, IsDiscrete);
        double thumbPadding = padding > thumbPreferredSize.Width / 2 ? padding / 2 : 0;
        _startThumbCenter = new Point(
            BoxConstraints.ClampDouble(
                startThumbPosition,
                trackRect.Left + thumbPadding,
                trackRect.Right - thumbPadding),
            thumbYOffset);
        _endThumbCenter = new Point(
            BoxConstraints.ClampDouble(endThumbPosition, trackRect.Left + thumbPadding, trackRect.Right - thumbPadding),
            thumbYOffset);
        if (IsEnabled)
        {
            Size overlaySize = SliderTheme.OverlayShape!.GetPreferredSize(IsEnabled, false);
            OverlayStartRect = RectFromCircle(center: _startThumbCenter, radius: overlaySize.Width / 2.0);
            OverlayEndRect = RectFromCircle(center: _endThumbCenter, radius: overlaySize.Width / 2.0);
        }

        // If [RangeSlider.year2023] is false, the thumbs uses handle thumb shape and gapped track shape.
        // The handle width and track gaps are adjusted when the thumb is pressed.
        var noStates = new HashSet<WidgetState>();
        double? thumbWidth = _sliderTheme.ThumbSize?.Resolve(noStates)?.Width;
        double? thumbHeight = _sliderTheme.ThumbSize?.Resolve(noStates)?.Height;
        double? trackGap = _sliderTheme.TrackGap;
        double? pressedThumbWidth = _sliderTheme.ThumbSize?.Resolve(
            new HashSet<WidgetState> { WidgetState.Pressed })?.Width;
        double delta;
        if (_active && thumbWidth != null && pressedThumbWidth != null && trackGap != null)
        {
            delta = thumbWidth.Value - pressedThumbWidth.Value;
            thumbWidth = pressedThumbWidth;
            if (trackGap > 0.0)
            {
                trackGap = trackGap.Value - (delta / 2);
            }
        }

        _sliderTheme.RangeTrackShape!.Paint(
            context,
            offset,
            parentBox: this,
            sliderTheme: _sliderTheme.CopyWith(trackGap: trackGap),
            enableAnimation: _enableAnimation,
            textDirection: _textDirection,
            startThumbCenter: _startThumbCenter,
            endThumbCenter: _endThumbCenter,
            isDiscrete: IsDiscrete,
            isEnabled: IsEnabled);

        bool startThumbSelected = _lastThumbSelection == Thumb.Start && !HoveringEndThumb;
        bool endThumbSelected = _lastThumbSelection == Thumb.End && !HoveringStartThumb;
        Size resolvedscreenSize = ScreenSize.IsEmpty ? Size : ScreenSize;

        if (_state.StartFocusNode.HasFocus)
        {
            _sliderTheme.OverlayShape!.Paint(
                context,
                _startThumbCenter,
                activationAnimation: new AlwaysStoppedAnimation<double>(1.0),
                enableAnimation: _enableAnimation,
                isDiscrete: IsDiscrete,
                labelPainter: _startLabelPainter,
                parentBox: this,
                sliderTheme: _sliderTheme,
                textDirection: _textDirection,
                value: startValue,
                textScaleFactor: _textScaleFactor,
                sizeWithOverflow: resolvedscreenSize);
        }

        if (_state.EndFocusNode.HasFocus)
        {
            _sliderTheme.OverlayShape!.Paint(
                context,
                _endThumbCenter,
                activationAnimation: new AlwaysStoppedAnimation<double>(1.0),
                enableAnimation: _enableAnimation,
                isDiscrete: IsDiscrete,
                labelPainter: _endLabelPainter,
                parentBox: this,
                sliderTheme: _sliderTheme,
                textDirection: _textDirection,
                value: endValue,
                textScaleFactor: _textScaleFactor,
                sizeWithOverflow: resolvedscreenSize);
        }

        if (!_overlayAnimation.Status.IsDismissed())
        {
            if (startThumbSelected || HoveringStartThumb)
            {
                _sliderTheme.OverlayShape!.Paint(
                    context,
                    _startThumbCenter,
                    activationAnimation: _overlayAnimation,
                    enableAnimation: _enableAnimation,
                    isDiscrete: IsDiscrete,
                    labelPainter: _startLabelPainter,
                    parentBox: this,
                    sliderTheme: _sliderTheme,
                    textDirection: _textDirection,
                    value: startValue,
                    textScaleFactor: _textScaleFactor,
                    sizeWithOverflow: resolvedscreenSize);
            }

            if (endThumbSelected || HoveringEndThumb)
            {
                _sliderTheme.OverlayShape!.Paint(
                    context,
                    _endThumbCenter,
                    activationAnimation: _overlayAnimation,
                    enableAnimation: _enableAnimation,
                    isDiscrete: IsDiscrete,
                    labelPainter: _endLabelPainter,
                    parentBox: this,
                    sliderTheme: _sliderTheme,
                    textDirection: _textDirection,
                    value: endValue,
                    textScaleFactor: _textScaleFactor,
                    sizeWithOverflow: resolvedscreenSize);
            }
        }

        if (IsDiscrete)
        {
            double tickMarkWidth = _sliderTheme.RangeTickMarkShape!
                .GetPreferredSize(isEnabled: IsEnabled, sliderTheme: _sliderTheme)
                .Width;
            double discreteTrackPadding = trackRect.Height;
            double adjustedTrackWidth = trackRect.Width - discreteTrackPadding;
            // If the tick marks would be too dense, don't bother painting them.
            if (adjustedTrackWidth / Divisions!.Value >= 3.0 * tickMarkWidth)
            {
                double dy = trackRect.Center.Y;
                for (int i = 0; i <= Divisions!.Value; i++)
                {
                    double value = (double)i / Divisions!.Value;
                    // The ticks are mapped to be within the track, so the tick mark width
                    // must be subtracted from the track width.
                    double dx = trackRect.Left + (value * adjustedTrackWidth) + (discreteTrackPadding / 2);
                    var tickMarkOffset = new Point(dx, dy);
                    _sliderTheme.RangeTickMarkShape!.Paint(
                        context,
                        tickMarkOffset,
                        parentBox: this,
                        sliderTheme: _sliderTheme,
                        enableAnimation: _enableAnimation,
                        textDirection: _textDirection,
                        startThumbCenter: _startThumbCenter,
                        endThumbCenter: _endThumbCenter,
                        isEnabled: IsEnabled);
                }
            }
        }

        double thumbDelta = Math.Abs(_endThumbCenter.X - _startThumbCenter.X);

        bool isLastThumbStart = _lastThumbSelection == Thumb.Start;
        Thumb bottomThumb = isLastThumbStart ? Thumb.End : Thumb.Start;
        Thumb topThumb = isLastThumbStart ? Thumb.Start : Thumb.End;
        Point bottomThumbCenter = isLastThumbStart ? _endThumbCenter : _startThumbCenter;
        Point topThumbCenter = isLastThumbStart ? _startThumbCenter : _endThumbCenter;
        TextPainter bottomLabelPainter = isLastThumbStart ? _endLabelPainter : _startLabelPainter;
        TextPainter topLabelPainter = isLastThumbStart ? _startLabelPainter : _endLabelPainter;
        double bottomValue = isLastThumbStart ? endValue : startValue;
        double topValue = isLastThumbStart ? startValue : endValue;
        bool shouldPaintValueIndicators =
            IsEnabled
            && Labels != null
            && ((ShouldShowValueIndicatorWhenDragged && !_valueIndicatorAnimation.Status.IsDismissed())
                || ShouldAlwaysShowValueIndicator);

        if (shouldPaintValueIndicators)
        {
            _state.PaintBottomValueIndicator = (PaintingContext indicatorContext, Point indicatorOffset) =>
            {
                if (Attached)
                {
                    _sliderTheme.RangeValueIndicatorShape!.Paint(
                        indicatorContext,
                        bottomThumbCenter,
                        activationAnimation: ShouldAlwaysShowValueIndicator
                            ? new AlwaysStoppedAnimation<double>(1)
                            : _valueIndicatorAnimation,
                        enableAnimation: ShouldAlwaysShowValueIndicator
                            ? new AlwaysStoppedAnimation<double>(1)
                            : _enableAnimation,
                        isDiscrete: IsDiscrete,
                        isOnTop: false,
                        labelPainter: bottomLabelPainter,
                        parentBox: this,
                        sliderTheme: _sliderTheme,
                        textDirection: _textDirection,
                        thumb: bottomThumb,
                        value: bottomValue,
                        textScaleFactor: TextScaleFactor,
                        sizeWithOverflow: resolvedscreenSize);
                }
            };
        }

        _sliderTheme.RangeThumbShape!.Paint(
            context,
            bottomThumbCenter,
            activationAnimation: _valueIndicatorAnimation,
            enableAnimation: _enableAnimation,
            isDiscrete: IsDiscrete,
            isOnTop: false,
            textDirection: TextDirection,
            sliderTheme: thumbWidth != null && thumbHeight != null
                ? _sliderTheme.CopyWith(
                    thumbSize: new WidgetStatePropertyAll<Size?>(new Size(thumbWidth.Value, thumbHeight.Value)))
                : _sliderTheme,
            thumb: bottomThumb,
            isPressed: bottomThumb == Thumb.Start ? startThumbSelected : endThumbSelected);

        if (shouldPaintValueIndicators)
        {
            double startOffset = SliderTheme.RangeValueIndicatorShape!.GetHorizontalShift(
                parentBox: this,
                center: _startThumbCenter,
                labelPainter: _startLabelPainter,
                activationAnimation: _valueIndicatorAnimation,
                textScaleFactor: TextScaleFactor,
                sizeWithOverflow: resolvedscreenSize);
            double endOffset = SliderTheme.RangeValueIndicatorShape!.GetHorizontalShift(
                parentBox: this,
                center: _endThumbCenter,
                labelPainter: _endLabelPainter,
                activationAnimation: _valueIndicatorAnimation,
                textScaleFactor: TextScaleFactor,
                sizeWithOverflow: resolvedscreenSize);
            double startHalfWidth =
                SliderTheme.RangeValueIndicatorShape!
                    .GetPreferredSize(
                        IsEnabled,
                        IsDiscrete,
                        labelPainter: _startLabelPainter,
                        textScaleFactor: TextScaleFactor)
                    .Width
                / 2;
            double endHalfWidth =
                SliderTheme.RangeValueIndicatorShape!
                    .GetPreferredSize(
                        IsEnabled,
                        IsDiscrete,
                        labelPainter: _endLabelPainter,
                        textScaleFactor: TextScaleFactor)
                    .Width
                / 2;
            double innerOverflow =
                startHalfWidth
                + endHalfWidth
                + TextDirection switch
                {
                    TextDirection.Ltr => startOffset - endOffset,
                    TextDirection.Rtl => endOffset - startOffset,
                    _ => throw new InvalidOperationException(),
                };

            _state.PaintTopValueIndicator = (PaintingContext indicatorContext, Point indicatorOffset) =>
            {
                if (Attached)
                {
                    _sliderTheme.RangeValueIndicatorShape!.Paint(
                        indicatorContext,
                        topThumbCenter,
                        activationAnimation: ShouldAlwaysShowValueIndicator
                            ? new AlwaysStoppedAnimation<double>(1)
                            : _valueIndicatorAnimation,
                        enableAnimation: ShouldAlwaysShowValueIndicator
                            ? new AlwaysStoppedAnimation<double>(1)
                            : _enableAnimation,
                        isDiscrete: IsDiscrete,
                        isOnTop: thumbDelta < innerOverflow,
                        labelPainter: topLabelPainter,
                        parentBox: this,
                        sliderTheme: _sliderTheme,
                        textDirection: _textDirection,
                        thumb: topThumb,
                        value: topValue,
                        textScaleFactor: TextScaleFactor,
                        sizeWithOverflow: resolvedscreenSize);
                }
            };
        }

        _sliderTheme.RangeThumbShape!.Paint(
            context,
            topThumbCenter,
            activationAnimation: _overlayAnimation,
            enableAnimation: _enableAnimation,
            isDiscrete: IsDiscrete,
            isOnTop: thumbDelta < SliderTheme.RangeThumbShape!.GetPreferredSize(IsEnabled, IsDiscrete).Width,
            textDirection: TextDirection,
            sliderTheme: thumbWidth != null && thumbHeight != null
                ? _sliderTheme.CopyWith(
                    thumbSize: new WidgetStatePropertyAll<Size?>(new Size(thumbWidth.Value, thumbHeight.Value)))
                : _sliderTheme,
            thumb: topThumb,
            isPressed: topThumb == Thumb.Start ? startThumbSelected : endThumbSelected);
    }

    /// <summary>Describe the semantics of the start thumb.</summary>
    private SemanticsNode? _startSemanticsNode;

    /// <summary>Describe the semantics of the end thumb.</summary>
    private SemanticsNode? _endSemanticsNode;

    // Create the semantics configuration for a single value.
    private SemanticsConfiguration CreateSemanticsConfiguration(
        double value,
        double increasedValue,
        double decreasedValue,
        Action increaseAction,
        Action decreaseAction,
        bool focused)
    {
        var config = new SemanticsConfiguration();
        config.IsEnabled = IsEnabled;
        config.TextDirection = TextDirection;
        config.IsSlider = true;
        config.IsFocusable = true;
        config.IsFocused = focused;
        if (IsEnabled)
        {
            config.OnIncrease = increaseAction;
            config.OnDecrease = decreaseAction;
        }

        if (SemanticFormatterCallback != null)
        {
            config.Value = SemanticFormatterCallback(_state.Lerp(value));
            config.IncreasedValue = SemanticFormatterCallback(_state.Lerp(increasedValue));
            config.DecreasedValue = SemanticFormatterCallback(_state.Lerp(decreasedValue));
        }
        else
        {
            config.Value = $"{FormatPercent(value)}%";
            config.IncreasedValue = $"{FormatPercent(increasedValue)}%";
            config.DecreasedValue = $"{FormatPercent(decreasedValue)}%";
        }

        return config;
    }

    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        DebugAssertions.Assert(children.Count == 0, "children.isEmpty");

        SemanticsConfiguration startSemanticsConfiguration = CreateSemanticsConfiguration(
            Values.Start,
            IncreasedStartValue,
            DecreasedStartValue,
            IncreaseStartAction,
            DecreaseStartAction,
            focused: _state.StartFocusNode.HasFocus);
        SemanticsConfiguration endSemanticsConfiguration = CreateSemanticsConfiguration(
            Values.End,
            IncreasedEndValue,
            DecreasedEndValue,
            IncreaseEndAction,
            DecreaseEndAction,
            focused: _state.EndFocusNode.HasFocus);

        // Split the semantics node area between the start and end nodes.
        Rect leftRect = RectFromCenter(
            center: _startThumbCenter,
            width: WidgetConstants.MinInteractiveDimension,
            height: WidgetConstants.MinInteractiveDimension);
        Rect rightRect = RectFromCenter(
            center: _endThumbCenter,
            width: WidgetConstants.MinInteractiveDimension,
            height: WidgetConstants.MinInteractiveDimension);

        _startSemanticsNode ??= new SemanticsNode();
        _endSemanticsNode ??= new SemanticsNode();

        switch (TextDirection)
        {
            case TextDirection.Ltr:
                _startSemanticsNode.Rect = leftRect;
                _endSemanticsNode.Rect = rightRect;
                break;
            case TextDirection.Rtl:
                _startSemanticsNode.Rect = rightRect;
                _endSemanticsNode.Rect = leftRect;
                break;
        }

        _startSemanticsNode.UpdateWith(config: startSemanticsConfiguration);
        _endSemanticsNode.UpdateWith(config: endSemanticsConfiguration);

        List<SemanticsNode> finalChildren = [_startSemanticsNode, _endSemanticsNode];

        node.UpdateWith(config: config, childrenInInversePaintOrder: finalChildren);
    }

    /// <remarks>Dart overrides <c>clearSemantics</c>; Plumix's per-object half of it is overridable.</remarks>
    protected override void ClearOwnSemantics()
    {
        base.ClearOwnSemantics();
        _startSemanticsNode = null;
        _endSemanticsNode = null;
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration config)
    {
        base.DescribeSemanticsConfiguration(config);
        config.IsSemanticBoundary = true;
    }

    private double SemanticActionUnit => Divisions != null ? 1.0 / Divisions.Value : AdjustmentUnit;

    private void IncreaseStartAction()
    {
        if (IsEnabled)
        {
            OnChanged!(new RangeValues(IncreasedStartValue, Values.End));
        }
    }

    private void DecreaseStartAction()
    {
        if (IsEnabled)
        {
            OnChanged!(new RangeValues(DecreasedStartValue, Values.End));
        }
    }

    private void IncreaseEndAction()
    {
        if (IsEnabled)
        {
            OnChanged!(new RangeValues(Values.Start, IncreasedEndValue));
        }
    }

    private void DecreaseEndAction()
    {
        if (IsEnabled)
        {
            OnChanged!(new RangeValues(Values.Start, DecreasedEndValue));
        }
    }

    private double IncreasedStartValue
    {
        get
        {
            // Due to floating-point operations, this value can actually be greater than
            // expected (e.g. 0.4 + 0.2 = 0.600000000001), so we limit to 2 decimal points.
            double increasedStartValue = double.Parse(
                Diagnostics.ToStringAsFixed(Values.Start + SemanticActionUnit, 2),
                CultureInfo.InvariantCulture);
            return increasedStartValue <= Values.End - MinThumbSeparationValue
                ? increasedStartValue
                : Values.Start;
        }
    }

    private double DecreasedStartValue =>
        BoxConstraints.ClampDouble(Values.Start - SemanticActionUnit, 0.0, 1.0);

    private double IncreasedEndValue =>
        BoxConstraints.ClampDouble(Values.End + SemanticActionUnit, 0.0, 1.0);

    private double DecreasedEndValue
    {
        get
        {
            double decreasedEndValue = Values.End - SemanticActionUnit;
            return decreasedEndValue >= Values.Start + MinThumbSeparationValue
                ? decreasedEndValue
                : Values.End;
        }
    }

    /// <summary>Dart's <c>(value * 100).round()</c>, printed as an integer.</summary>
    private static string FormatPercent(double value) =>
        ((long)DartRound(value * 100)).ToString(CultureInfo.InvariantCulture);

    /// <summary>Dart's <c>double.round()</c>: half away from zero.</summary>
    private static double DartRound(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    /// <summary>Dart's <c>Duration * num</c>: the product rounded to whole microseconds.</summary>
    private static TimeSpan MultiplyDuration(TimeSpan duration, double factor) =>
        TimeSpan.FromMicroseconds((long)DartRound(duration.Ticks / TimeSpan.TicksPerMicrosecond * factor));

    /// <summary>Dart's <c>Rect.fromCircle</c>.</summary>
    private static Rect RectFromCircle(Point center, double radius) =>
        RectFromCenter(center, radius * 2, radius * 2);

    /// <summary>Dart's <c>Rect.fromCenter</c>, built from its left/top/right/bottom edges.</summary>
    private static Rect RectFromCenter(Point center, double width, double height) =>
        new(
            new Point(center.X - (width / 2), center.Y - (height / 2)),
            new Point(center.X + (width / 2), center.Y + (height / 2)));
}

/// <summary>Flutter's private <c>_RangeSliderDefaultsM2</c>.</summary>
/// <remarks>
/// The shape getters return one shared instance each, standing in for Dart's canonicalized <c>const</c>
/// constructors so that rebuilding does not make <see cref="SliderThemeData"/> compare unequal.
/// </remarks>
internal sealed record RangeSliderDefaultsM2 : SliderThemeData
{
    private static readonly RangeSliderTrackShape DefaultRangeTrackShape = new RoundedRectRangeSliderTrackShape();
    private static readonly RangeSliderTickMarkShape DefaultRangeTickMarkShape = new RoundRangeSliderTickMarkShape();
    private static readonly RangeSliderThumbShape DefaultRangeThumbShape = new RoundRangeSliderThumbShape();
    private static readonly SliderComponentShape DefaultOverlayShape = new RoundSliderOverlayShape();

    private static readonly RangeSliderValueIndicatorShape DefaultRangeValueIndicatorShape =
        new RectangularRangeSliderValueIndicatorShape();

    private readonly BuildContext _context;
    private ColorScheme? _colors;

    public RangeSliderDefaultsM2(BuildContext context) : base(TrackHeight: 4)
    {
        _context = context;
    }

    // Dart also declares `late final SliderThemeData sliderTheme = SliderTheme.of(context)`, which nothing
    // reads; it is not ported.
    private ColorScheme Scheme => _colors ??= Plumix.Material.Theme.Of(_context).ColorScheme;

    public override Color? ActiveTrackColor => Scheme.Primary;

    public override Color? InactiveTrackColor => Scheme.Primary.WithOpacity(0.24);

    public override Color? DisabledActiveTrackColor => Scheme.OnSurface.WithOpacity(0.32);

    public override Color? DisabledInactiveTrackColor => Scheme.OnSurface.WithOpacity(0.12);

    public override Color? ActiveTickMarkColor => Scheme.OnPrimary.WithOpacity(0.54);

    public override Color? InactiveTickMarkColor => Scheme.Primary.WithOpacity(0.54);

    public override Color? DisabledActiveTickMarkColor => Scheme.OnPrimary.WithOpacity(0.12);

    public override Color? DisabledInactiveTickMarkColor => Scheme.OnSurface.WithOpacity(0.12);

    public override Color? ThumbColor => Scheme.Primary;

    public override Color? OverlappingShapeStrokeColor => Scheme.Surface;

    public override Color? DisabledThumbColor =>
        Color.AlphaBlend(Scheme.OnSurface.WithOpacity(.38), Scheme.Surface);

    public override Color? OverlayColor => Scheme.Primary.WithOpacity(0.12);

    public override TextStyle? ValueIndicatorTextStyle =>
        Plumix.Material.Theme.Of(_context).TextTheme.BodyLarge.CopyWith(color: Scheme.OnPrimary);

    public override Color? ValueIndicatorColor => Scheme.Primary;

    public override RangeSliderTrackShape? RangeTrackShape => DefaultRangeTrackShape;

    public override RangeSliderTickMarkShape? RangeTickMarkShape => DefaultRangeTickMarkShape;

    public override RangeSliderThumbShape? RangeThumbShape => DefaultRangeThumbShape;

    public override SliderComponentShape? OverlayShape => DefaultOverlayShape;

    public override RangeSliderValueIndicatorShape? RangeValueIndicatorShape => DefaultRangeValueIndicatorShape;

    public override ShowValueIndicator? ShowValueIndicator => Plumix.Material.ShowValueIndicator.OnlyForDiscrete;

    public override double? MinThumbSeparation => 8;
}

// BEGIN GENERATED TOKEN PROPERTIES - RangeSlider

// Do not edit by hand. The code between the "BEGIN GENERATED" and
// "END GENERATED" comments are generated from data in the Material
// Design token database by the script:
//   dev/tools/gen_defaults/bin/gen_defaults.dart.

/// <summary>Flutter's private <c>_RangeSliderDefaultsM3</c>.</summary>
internal sealed record RangeSliderDefaultsM3 : SliderThemeData
{
    private static readonly RangeSliderTrackShape DefaultRangeTrackShape = new GappedRangeSliderTrackShape();

    private static readonly RangeSliderTickMarkShape DefaultRangeTickMarkShape =
        new RoundRangeSliderTickMarkShape(tickMarkRadius: 4.0 / 2);

    private static readonly RangeSliderThumbShape DefaultRangeThumbShape = new HandleRangeSliderThumbShape();
    private static readonly SliderComponentShape DefaultOverlayShape = new RoundSliderOverlayShape();

    private static readonly RangeSliderValueIndicatorShape DefaultRangeValueIndicatorShape =
        new RoundedRectRangeSliderValueIndicatorShape();

    private readonly BuildContext _context;
    private ColorScheme? _colors;

    public RangeSliderDefaultsM3(BuildContext context) : base(TrackHeight: 16.0)
    {
        _context = context;
    }

    private ColorScheme Scheme => _colors ??= Plumix.Material.Theme.Of(_context).ColorScheme;

    public override Color? ActiveTrackColor => Scheme.Primary;

    public override Color? InactiveTrackColor => Scheme.SecondaryContainer;

    public override Color? DisabledActiveTrackColor => Scheme.OnSurface.WithOpacity(0.38);

    public override Color? DisabledInactiveTrackColor => Scheme.OnSurface.WithOpacity(0.12);

    public override Color? ActiveTickMarkColor => Scheme.OnPrimary.WithOpacity(1.0);

    public override Color? InactiveTickMarkColor => Scheme.OnSecondaryContainer.WithOpacity(1.0);

    public override Color? DisabledActiveTickMarkColor => Scheme.OnInverseSurface;

    public override Color? DisabledInactiveTickMarkColor => Scheme.OnSurface;

    public override Color? ThumbColor => Scheme.Primary;

    public override Color? OverlappingShapeStrokeColor => Scheme.Surface;

    public override Color? DisabledThumbColor => Scheme.OnSurface.WithOpacity(0.38);

    public override Color? OverlayColor => Scheme.Primary.WithOpacity(0.12);

    public override TextStyle? ValueIndicatorTextStyle =>
        Plumix.Material.Theme.Of(_context).TextTheme.LabelLarge.CopyWith(color: Scheme.OnInverseSurface);

    public override Color? ValueIndicatorColor => Scheme.InverseSurface;

    public override RangeSliderTrackShape? RangeTrackShape => DefaultRangeTrackShape;

    public override RangeSliderTickMarkShape? RangeTickMarkShape => DefaultRangeTickMarkShape;

    public override RangeSliderThumbShape? RangeThumbShape => DefaultRangeThumbShape;

    public override SliderComponentShape? OverlayShape => DefaultOverlayShape;

    public override RangeSliderValueIndicatorShape? RangeValueIndicatorShape => DefaultRangeValueIndicatorShape;

    public override ShowValueIndicator? ShowValueIndicator => Plumix.Material.ShowValueIndicator.OnlyForDiscrete;

    public override double? MinThumbSeparation => 0;

    public override WidgetStateProperty<Size?>? ThumbSize
    {
        get
        {
            return WidgetStateProperty<Size?>.ResolveWith(states =>
            {
                if (states.Contains(WidgetState.Disabled))
                {
                    return new Size(4.0, 44.0);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return new Size(4.0, 44.0);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return new Size(2.0, 44.0);
                }

                if (states.Contains(WidgetState.Pressed))
                {
                    return new Size(2.0, 44.0);
                }

                return new Size(4.0, 44.0);
            });
        }
    }

    public override double? TrackGap => 6.0;
}

// END GENERATED TOKEN PROPERTIES - RangeSlider
