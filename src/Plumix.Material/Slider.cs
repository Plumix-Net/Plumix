using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using MouseCursor = Plumix.UI.MouseCursor;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider.dart

/// <summary>
/// [Slider] uses this callback to paint the value indicator on the overlay.
/// </summary>
/// <remarks>
/// Since the value indicator is painted on the Overlay; this method paints the value indicator in a
/// [RenderBox] that appears in the [Overlay].
/// </remarks>
public delegate void PaintValueIndicator(PaintingContext context, Point offset);

/// <summary>Dart's private <c>_SliderType</c>.</summary>
internal enum SliderType
{
    Material,
    Adaptive,
}

/// <summary>Possible ways for a user to interact with a [Slider].</summary>
public enum SliderInteraction
{
    /// Allows the user to interact with a [Slider] by tapping or sliding anywhere on the track.
    ///
    /// Essentially all possible interactions are allowed. This is different from
    /// [SliderInteraction.SlideOnly] as when you try to slide anywhere other than the thumb, the
    /// thumb will move to the first point of contact.
    TapAndSlide,

    /// Allows the user to interact with a [Slider] by only tapping anywhere on the track.
    ///
    /// Sliding interaction is ignored.
    TapOnly,

    /// Allows the user to interact with a [Slider] only by sliding anywhere on the track.
    ///
    /// Tapping interaction is ignored.
    SlideOnly,

    /// Allows the user to interact with a [Slider] only by sliding the thumb.
    ///
    /// Tapping and sliding interactions on the track are ignored.
    SlideThumb,
}

/// <summary>
/// A Material Design slider, used to select from a range of values.
/// </summary>
/// <remarks>
/// The slider will be disabled if [OnChanged] is null or if the range given by [Min]..[Max] is empty
/// (i.e. if [Min] is equal to [Max]). The slider widget itself does not maintain any state: when the
/// state of the slider changes, the widget calls [OnChanged], and the parent rebuilds the slider with
/// a new [Value].
///
/// By default, a slider will be as wide as possible, centered vertically. When given unbounded
/// constraints, it will attempt to make the track 144 pixels wide (with margins on each side) and
/// will shrink-wrap vertically.
///
/// Requires one of its ancestors to be a [Material] widget and a [MediaQuery] widget. The appearance
/// comes from the [SliderThemeData] of the nearest [SliderTheme] or [ThemeData.SliderTheme].
/// </remarks>
public class Slider : StatefulWidget
{
    /// <summary>Creates a Material Design slider.</summary>
    /// <remarks>
    /// * [value] determines currently selected value for this slider.
    /// * [onChanged] is called while the user is selecting a new value for the slider.
    /// * [onChangeStart] is called when the user starts to select a new value for the slider.
    /// * [onChangeEnd] is called when the user is done selecting a new value for the slider.
    /// </remarks>
    public Slider(
        double value,
        Action<double>? onChanged,
        double? secondaryTrackValue = null,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        string? label = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? secondaryActiveColor = null,
        Color? thumbColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        MouseCursor? mouseCursor = null,
        SemanticFormatterCallback? semanticFormatterCallback = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        SliderInteraction? allowedInteraction = null,
        EdgeInsetsGeometry? padding = null,
        ShowValueIndicator? showValueIndicator = null,
        bool? year2023 = null,
        Key? key = null)
        : this(
            SliderType.Material,
            key,
            value,
            secondaryTrackValue,
            onChanged,
            onChangeStart,
            onChangeEnd,
            min,
            max,
            divisions,
            label,
            activeColor,
            inactiveColor,
            secondaryActiveColor,
            thumbColor,
            overlayColor,
            mouseCursor,
            semanticFormatterCallback,
            focusNode,
            autofocus,
            allowedInteraction,
            padding,
            showValueIndicator,
            year2023)
    {
    }

    private Slider(
        SliderType sliderType,
        Key? key,
        double value,
        double? secondaryTrackValue,
        Action<double>? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        double min,
        double max,
        int? divisions,
        string? label,
        Color? activeColor,
        Color? inactiveColor,
        Color? secondaryActiveColor,
        Color? thumbColor,
        WidgetStateProperty<Color?>? overlayColor,
        MouseCursor? mouseCursor,
        SemanticFormatterCallback? semanticFormatterCallback,
        FocusNode? focusNode,
        bool autofocus,
        SliderInteraction? allowedInteraction,
        EdgeInsetsGeometry? padding,
        ShowValueIndicator? showValueIndicator,
        bool? year2023) : base(key)
    {
        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(min <= max, "min <= max");
            if (!(value >= min && value <= max))
            {
                throw new AssertionError(
                    $"Value {Dart(value)} is not between minimum {Dart(min)} and maximum {Dart(max)}");
            }

            if (!(secondaryTrackValue is null
                  || (secondaryTrackValue >= min && secondaryTrackValue <= max)))
            {
                throw new AssertionError(
                    $"SecondaryValue {Dart(secondaryTrackValue.Value)} is not between {Dart(min)} and {Dart(max)}");
            }

            DebugAssertions.Assert(divisions is null || divisions > 0, "divisions == null || divisions > 0");
        }

        SliderType = sliderType;
        Value = value;
        SecondaryTrackValue = secondaryTrackValue;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        Label = label;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        SecondaryActiveColor = secondaryActiveColor;
        ThumbColor = thumbColor;
        OverlayColor = overlayColor;
        MouseCursor = mouseCursor;
        SemanticFormatterCallback = semanticFormatterCallback;
        FocusNode = focusNode;
        Autofocus = autofocus;
        AllowedInteraction = allowedInteraction;
        Padding = padding;
        ShowValueIndicator = showValueIndicator;
        Year2023 = year2023;
    }

    /// <summary>
    /// Creates an adaptive [Slider] based on the target platform, following Material design's
    /// Cross-platform guidelines.
    /// </summary>
    /// <remarks>
    /// Creates a [CupertinoSlider] if the target platform is iOS or macOS, creates a Material Design
    /// slider otherwise. If a [CupertinoSlider] is created, the following parameters are ignored:
    /// [secondaryTrackValue], [label], [inactiveColor], [secondaryActiveColor],
    /// [semanticFormatterCallback], [showValueIndicator]. The target platform is based on the current
    /// [Theme]: [ThemeData.Platform]. Dart's <c>Slider.adaptive</c> constructor.
    /// </remarks>
    public static Slider Adaptive(
        double value,
        Action<double>? onChanged,
        double? secondaryTrackValue = null,
        Action<double>? onChangeStart = null,
        Action<double>? onChangeEnd = null,
        double min = 0.0,
        double max = 1.0,
        int? divisions = null,
        string? label = null,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
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
            SliderType.Adaptive,
            key,
            value,
            secondaryTrackValue,
            onChanged,
            onChangeStart,
            onChangeEnd,
            min,
            max,
            divisions,
            label,
            activeColor,
            inactiveColor,
            secondaryActiveColor,
            thumbColor,
            overlayColor,
            mouseCursor,
            semanticFormatterCallback,
            focusNode,
            autofocus,
            allowedInteraction,
            padding: null,
            showValueIndicator,
            year2023);
    }

    /// <summary>The currently selected value for this slider.</summary>
    /// <remarks>The slider's thumb is drawn at a position that corresponds to this value.</remarks>
    public double Value { get; }

    /// <summary>The secondary track value for this slider.</summary>
    /// <remarks>
    /// If not null, a secondary track using [SecondaryActiveColor] color is drawn between the thumb and
    /// this value, over the inactive track. If less than [Value], then the secondary track is not shown.
    /// </remarks>
    public double? SecondaryTrackValue { get; }

    /// <summary>
    /// Called during a drag when the user is selecting a new value for the slider by dragging.
    /// </summary>
    /// <remarks>
    /// The slider passes the new value to the callback but does not actually change state until the
    /// parent widget rebuilds the slider with the new value. If null, the slider will be displayed as
    /// disabled.
    /// </remarks>
    public Action<double>? OnChanged { get; }

    /// <summary>Called when the user starts selecting a new value for the slider.</summary>
    /// <remarks>The value passed will be the last [Value] that the slider had before the change began.</remarks>
    public Action<double>? OnChangeStart { get; }

    /// <summary>Called when the user is done selecting a new value for the slider.</summary>
    public Action<double>? OnChangeEnd { get; }

    /// <summary>The minimum value the user can select. Defaults to 0.0; must be &lt;= [Max].</summary>
    public double Min { get; }

    /// <summary>The maximum value the user can select. Defaults to 1.0; must be &gt;= [Min].</summary>
    public double Max { get; }

    /// <summary>The number of discrete divisions. If null, the slider is continuous.</summary>
    public int? Divisions { get; }

    /// <summary>
    /// A label to show above the slider when the slider is active and
    /// [SliderThemeData.ShowValueIndicator] is satisfied.
    /// </summary>
    /// <remarks>
    /// The label is rendered using the active [ThemeData]'s text style (overridable with
    /// [SliderThemeData.ValueIndicatorTextStyle]). If null, then the value indicator will not be
    /// displayed. Ignored if this slider is created with [Slider.Adaptive].
    /// </remarks>
    public string? Label { get; }

    /// <summary>The color to use for the portion of the slider track that is active.</summary>
    /// <remarks>
    /// If null, [SliderThemeData.ActiveTrackColor] of the ambient [SliderTheme] is used. If that is
    /// null, [ColorScheme.Primary] of the surrounding [ThemeData] is used.
    /// </remarks>
    public Color? ActiveColor { get; }

    /// <summary>The color for the inactive portion of the slider track.</summary>
    /// <remarks>Ignored if this slider is created with [Slider.Adaptive].</remarks>
    public Color? InactiveColor { get; }

    /// <summary>
    /// The color to use for the portion of the slider track between the thumb and the
    /// [SecondaryTrackValue].
    /// </summary>
    /// <remarks>Ignored if this slider is created with [Slider.Adaptive].</remarks>
    public Color? SecondaryActiveColor { get; }

    /// <summary>The color of the thumb.</summary>
    /// <remarks>
    /// If this color is null, [Slider] will use [ActiveColor], then [SliderThemeData.ThumbColor], then
    /// [ColorScheme.Primary]. A [CupertinoSlider] will have a white thumb.
    /// </remarks>
    public Color? ThumbColor { get; }

    /// <summary>
    /// The highlight color that's typically used to indicate that the slider thumb is focused, hovered,
    /// or dragged.
    /// </summary>
    public WidgetStateProperty<Color?>? OverlayColor { get; }

    /// <summary>
    /// The cursor for a mouse pointer when it enters or is hovering over the widget.
    /// </summary>
    /// <remarks>
    /// If [MouseCursor] is a [WidgetStateMouseCursor], it is resolved for [WidgetState.Dragged],
    /// [WidgetState.Hovered], [WidgetState.Focused] and [WidgetState.Disabled]. If null, then the value
    /// of [SliderThemeData.MouseCursor] is used. If that is also null, then
    /// [WidgetStateMouseCursor.Clickable] is used.
    /// </remarks>
    public MouseCursor? MouseCursor { get; }

    /// <summary>The callback used to create a semantic value from a slider value.</summary>
    /// <remarks>
    /// Defaults to formatting values as a percentage. Ignored if this slider is created with
    /// [Slider.Adaptive].
    /// </remarks>
    public SemanticFormatterCallback? SemanticFormatterCallback { get; }

    /// <summary>An optional focus node to use as the focus node for this widget.</summary>
    public FocusNode? FocusNode { get; }

    /// <summary>True if this widget will be selected as the initial focus when no other node is focused.</summary>
    public bool Autofocus { get; }

    /// <summary>Allowed way for the user to interact with the [Slider].</summary>
    /// <remarks>Defaults to [SliderInteraction.TapAndSlide].</remarks>
    public SliderInteraction? AllowedInteraction { get; }

    /// <summary>Determines the padding around the [Slider].</summary>
    /// <remarks>
    /// If specified, this padding overrides the default vertical padding of the [Slider], defaults to
    /// the height of the overlay shape, and the horizontal padding, defaults to the width of the thumb
    /// shape or overlay shape, whichever is larger.
    /// </remarks>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>Determines the conditions under which the value indicator is shown.</summary>
    /// <remarks>
    /// If null then the ambient [SliderThemeData.ShowValueIndicator] is used. If that is also null,
    /// defaults to [ShowValueIndicator.OnlyForDiscrete].
    /// </remarks>
    public ShowValueIndicator? ShowValueIndicator { get; }

    /// <summary>
    /// When true, the [Slider] will use the 2023 Material Design 3 appearance. Defaults to true.
    /// </summary>
    /// <remarks>
    /// If this is set to false, the [Slider] will use the latest Material Design 3 appearance, which
    /// was introduced in December 2023. If [ThemeData.UseMaterial3] is false, then this property is
    /// ignored. Deprecated in Dart: set this flag to false to opt into the 2024 slider appearance.
    /// </remarks>
    public bool? Year2023 { get; }

    /// <summary>Dart's private <c>_sliderType</c>.</summary>
    internal SliderType SliderType { get; }

    public override State CreateState() => new SliderState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("value", Value));
        properties.Add(new DoubleProperty("secondaryTrackValue", SecondaryTrackValue));
        properties.Add(new ObjectFlagProperty<Action<double>>("onChanged", OnChanged, ifNull: "disabled"));
        properties.Add(ObjectFlagProperty<Action<double>>.Has("onChangeStart", OnChangeStart));
        properties.Add(ObjectFlagProperty<Action<double>>.Has("onChangeEnd", OnChangeEnd));
        properties.Add(new DoubleProperty("min", Min));
        properties.Add(new DoubleProperty("max", Max));
        properties.Add(new IntProperty("divisions", Divisions));
        properties.Add(new StringProperty("label", Label));
        properties.Add(new ColorProperty("activeColor", ActiveColor));
        properties.Add(new ColorProperty("inactiveColor", InactiveColor));
        properties.Add(new ColorProperty("secondaryActiveColor", SecondaryActiveColor));
        properties.Add(
            ObjectFlagProperty<SemanticFormatterCallback>.Has(
                "semanticFormatterCallback",
                SemanticFormatterCallback));
        properties.Add(ObjectFlagProperty<FocusNode>.Has("focusNode", FocusNode));
        properties.Add(new FlagProperty("autofocus", value: Autofocus, ifTrue: "autofocus"));
    }

    // Dart interpolates doubles with `double.toString`, which keeps a trailing `.0`.
    private static string Dart(double value) => BindingBase.DartDoubleToString(value);
}

/// <summary>Dart's private <c>_SliderState</c>.</summary>
internal sealed partial class SliderState : State<Slider>
{
    private static readonly TimeSpan EnableAnimationDuration = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan ValueIndicatorAnimationDuration = TimeSpan.FromMilliseconds(100);

    // Dart's `kRadialReactionDuration` (material_ui/lib/src/constants.dart).
    private static readonly TimeSpan RadialReactionDuration = TimeSpan.FromMilliseconds(100);

    // Keyboard mapping for a focused slider.
    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> TraditionalNavShortcutMap =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = AdjustSliderIntent.Up(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = AdjustSliderIntent.Down(),
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = AdjustSliderIntent.Left(),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] = AdjustSliderIntent.Right(),
        };

    // Keyboard mapping for a focused slider when using directional navigation.
    // The vertical inputs are not handled to allow navigating out of the slider.
    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> DirectionalNavShortcutMap =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = AdjustSliderIntent.Left(),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] = AdjustSliderIntent.Right(),
        };

    private readonly GlobalKey _renderObjectKey = new LabeledGlobalKey<State>(null);

    // Always keep the ValueIndicator visible on the Overlay; otherwise, it cannot be updated during the
    // build phase.
    private readonly OverlayPortalController _valueIndicatorOverlayPortalController =
        ShowController(new OverlayPortalController(debugLabel: "Slider ValueIndicator"));

    // Action mapping for a focused slider.
    private IReadOnlyDictionary<Type, FlutterAction> _actionMap = null!;

    private bool _dragging;

    // For discrete sliders, HandleChanged might receive the same value multiple times. To avoid
    // calling widget.OnChanged repeatedly, the value from HandleChanged is temporarily saved here.
    private double? _currentChangedValue;

    private FocusNode? _focusNode;

    private bool _focused;

    private bool _hovering;

    private readonly LayerLink _layerLink = new();

    /// <summary>
    /// Animation controller that is run when the overlay (a.k.a radial reaction) is shown in response
    /// to user interaction.
    /// </summary>
    internal AnimationController OverlayController { get; private set; } = null!;

    /// <summary>Animation controller that is run when the value indicator is being shown or hidden.</summary>
    internal AnimationController ValueIndicatorController { get; private set; } = null!;

    /// <summary>Animation controller that is run when enabling/disabling the slider.</summary>
    internal AnimationController EnableController { get; private set; } = null!;

    /// <summary>
    /// Animation controller that is run when transitioning between one value and the next on a
    /// discrete slider.
    /// </summary>
    internal AnimationController PositionController { get; private set; } = null!;

    /// <summary>Dart's <c>interactionTimer</c>.</summary>
    internal GestureTimer? InteractionTimer { get; set; }

    private bool Enabled => Widget.OnChanged != null;

    /// <summary>Value Indicator Animation that appears on the Overlay.</summary>
    internal PaintValueIndicator? PaintValueIndicator { get; set; }

    internal FocusNode FocusNode => Widget.FocusNode ?? _focusNode!;

    public override void InitState()
    {
        base.InitState();
        OverlayController = new AnimationController(duration: RadialReactionDuration, vsync: this);
        ValueIndicatorController = new AnimationController(
            duration: ValueIndicatorAnimationDuration,
            vsync: this);
        EnableController = new AnimationController(duration: EnableAnimationDuration, vsync: this);
        PositionController = new AnimationController(duration: TimeSpan.Zero, vsync: this);
        EnableController.SetValue(Widget.OnChanged != null ? 1.0 : 0.0);
        PositionController.SetValue(Convert(Widget.Value));
        _actionMap = new Dictionary<Type, FlutterAction>
        {
            [typeof(AdjustSliderIntent)] = new CallbackAction<AdjustSliderIntent>(
                onInvoke: intent =>
                {
                    ActionHandler(intent);
                    return null;
                }),
        };
        if (Widget.FocusNode == null)
        {
            // Only create a new node if the widget doesn't have one.
            _focusNode ??= new FocusNode();
        }
    }

    public override void Dispose()
    {
        InteractionTimer?.Cancel();
        OverlayController.Dispose();
        ValueIndicatorController.Dispose();
        EnableController.Dispose();
        PositionController.Dispose();
        _focusNode?.Dispose();
        base.Dispose();
    }

    private void HandleChanged(double value)
    {
        DebugAssertions.Assert(Widget.OnChanged != null, "widget.onChanged != null");
        double lerpValue = Lerp(value);
        if (_currentChangedValue != lerpValue)
        {
            _currentChangedValue = lerpValue;
            if (_currentChangedValue != Widget.Value)
            {
                Widget.OnChanged!(_currentChangedValue.Value);
            }
        }
    }

    private void HandleDragStart(double value)
    {
        SetState(() => _dragging = true);
        Widget.OnChangeStart?.Invoke(Lerp(value));
    }

    private void HandleDragEnd(double value)
    {
        SetState(() => _dragging = false);
        _currentChangedValue = null;
        Widget.OnChangeEnd?.Invoke(Lerp(value));
    }

    private void ActionHandler(AdjustSliderIntent intent)
    {
        TextDirection directionality = Directionality.Of(_renderObjectKey.CurrentContext!);
        bool shouldIncrease = intent.Type switch
        {
            SliderAdjustmentType.Up => true,
            SliderAdjustmentType.Down => false,
            SliderAdjustmentType.Left => directionality == TextDirection.Rtl,
            SliderAdjustmentType.Right => directionality == TextDirection.Ltr,
            _ => throw new ArgumentOutOfRangeException(nameof(intent)),
        };

        var slider = (RenderSlider)_renderObjectKey.CurrentContext!.FindRenderObject()!;
        if (shouldIncrease)
        {
            slider.IncreaseAction();
        }
        else
        {
            slider.DecreaseAction();
        }
    }

    private void HandleFocusHighlightChanged(bool focused)
    {
        if (focused != _focused)
        {
            SetState(() => _focused = focused);
        }
    }

    private void HandleHoverChanged(bool hovering)
    {
        if (hovering != _hovering)
        {
            SetState(() => _hovering = hovering);
        }
    }

    /// <summary>
    /// Returns a number between min and max, proportional to value, which must be between 0.0 and 1.0.
    /// </summary>
    internal double Lerp(double value)
    {
        DebugAssertions.Assert(value >= 0.0, "value >= 0.0");
        DebugAssertions.Assert(value <= 1.0, "value <= 1.0");
        return value * (Widget.Max - Widget.Min) + Widget.Min;
    }

    private double Discretize(double value)
    {
        DebugAssertions.Assert(Widget.Divisions != null, "widget.divisions != null");
        DebugAssertions.Assert(value >= 0.0 && value <= 1.0, "value >= 0.0 && value <= 1.0");

        int divisions = Widget.Divisions!.Value;
        return Math.Round(value * divisions, MidpointRounding.AwayFromZero) / divisions;
    }

    private double Convert(double value)
    {
        double ret = Unlerp(value);
        if (Widget.Divisions != null)
        {
            ret = Discretize(ret);
        }

        return ret;
    }

    /// <summary>Returns a number between 0.0 and 1.0, given a value between min and max.</summary>
    private double Unlerp(double value)
    {
        DebugAssertions.Assert(value <= Widget.Max, "value <= widget.max");
        DebugAssertions.Assert(value >= Widget.Min, "value >= widget.min");
        return Widget.Max > Widget.Min ? (value - Widget.Min) / (Widget.Max - Widget.Min) : 0.0;
    }

    public override Widget Build(BuildContext context)
    {
        MaterialDebug.DebugCheckHasMaterial(context);
        // Dart also asserts `debugCheckHasMediaQuery(context)`, which is not ported (docs/ai/BACKLOG.md).
        switch (Widget.SliderType)
        {
            case SliderType.Material:
                return BuildMaterialSlider(context);

            case SliderType.Adaptive:
            {
                ThemeData theme = Theme.Of(context);
                switch (theme.Platform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.Linux:
                    case TargetPlatform.Windows:
                        return BuildMaterialSlider(context);
                    case TargetPlatform.IOS:
                    case TargetPlatform.MacOS:
                        return BuildCupertinoSlider(context);
                }

                break;
            }
        }

        throw new InvalidOperationException($"Unknown slider type {Widget.SliderType}.");
    }

    private Widget BuildMaterialSlider(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        SliderThemeData sliderTheme = SliderTheme.Of(context);
        bool year2023 = Widget.Year2023 ?? sliderTheme.Year2023 ?? true;
        SliderThemeData defaults = theme.UseMaterial3
            ? year2023 ? new SliderDefaultsM3Year2023(context) : new SliderDefaultsM3(context)
            : new SliderDefaultsM2(context);

        // If the widget has active or inactive colors specified, then we plug them in to the slider
        // theme as best we can. If the developer wants more control than that, then they need to use a
        // SliderTheme. The default colors come from the ThemeData.colorScheme. These colors, along with
        // the default shapes and text styles are aligned to the Material Guidelines.

        const ShowValueIndicator defaultShowValueIndicator = ShowValueIndicator.OnlyForDiscrete;
        const SliderInteraction defaultAllowedInteraction = SliderInteraction.TapAndSlide;

        var states = new HashSet<WidgetState>();
        if (!Enabled)
        {
            states.Add(WidgetState.Disabled);
        }

        if (_hovering)
        {
            states.Add(WidgetState.Hovered);
        }

        if (_focused)
        {
            states.Add(WidgetState.Focused);
        }

        if (_dragging)
        {
            states.Add(WidgetState.Dragged);
        }

        // The value indicator's color is not the same as the thumb and active track (which can be
        // defined by activeColor) if the RectangularSliderValueIndicatorShape is used. In all other
        // cases, the value indicator is assumed to be the same as the active color.
        SliderComponentShape valueIndicatorShape =
            sliderTheme.ValueIndicatorShape ?? defaults.ValueIndicatorShape!;
        Color valueIndicatorColor;
        if (valueIndicatorShape is RectangularSliderValueIndicatorShape)
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
                   ?? WidgetStateProperty<Color?>.ResolveAs(defaults.OverlayColor, states);
        }

        TextStyle valueIndicatorTextStyle =
            sliderTheme.ValueIndicatorTextStyle ?? defaults.ValueIndicatorTextStyle!;
        if (MediaQuery.BoldTextOf(context))
        {
            valueIndicatorTextStyle = valueIndicatorTextStyle.Merge(
                new TextStyle(FontWeight: Avalonia.Media.FontWeight.Bold));
        }

        sliderTheme = sliderTheme.CopyWith(
            trackHeight: sliderTheme.TrackHeight ?? defaults.TrackHeight,
            activeTrackColor:
                Widget.ActiveColor ?? sliderTheme.ActiveTrackColor ?? defaults.ActiveTrackColor,
            inactiveTrackColor:
                Widget.InactiveColor ?? sliderTheme.InactiveTrackColor ?? defaults.InactiveTrackColor,
            secondaryActiveTrackColor:
                Widget.SecondaryActiveColor
                ?? sliderTheme.SecondaryActiveTrackColor
                ?? defaults.SecondaryActiveTrackColor,
            disabledActiveTrackColor:
                sliderTheme.DisabledActiveTrackColor ?? defaults.DisabledActiveTrackColor,
            disabledInactiveTrackColor:
                sliderTheme.DisabledInactiveTrackColor ?? defaults.DisabledInactiveTrackColor,
            disabledSecondaryActiveTrackColor:
                sliderTheme.DisabledSecondaryActiveTrackColor
                ?? defaults.DisabledSecondaryActiveTrackColor,
            activeTickMarkColor:
                Widget.InactiveColor ?? sliderTheme.ActiveTickMarkColor ?? defaults.ActiveTickMarkColor,
            inactiveTickMarkColor:
                Widget.ActiveColor ?? sliderTheme.InactiveTickMarkColor ?? defaults.InactiveTickMarkColor,
            disabledActiveTickMarkColor:
                sliderTheme.DisabledActiveTickMarkColor ?? defaults.DisabledActiveTickMarkColor,
            disabledInactiveTickMarkColor:
                sliderTheme.DisabledInactiveTickMarkColor ?? defaults.DisabledInactiveTickMarkColor,
            thumbColor:
                Widget.ThumbColor ?? Widget.ActiveColor ?? sliderTheme.ThumbColor ?? defaults.ThumbColor,
            disabledThumbColor: sliderTheme.DisabledThumbColor ?? defaults.DisabledThumbColor,
            overlayColor: EffectiveOverlayColor(),
            valueIndicatorColor: valueIndicatorColor,
            trackShape: sliderTheme.TrackShape ?? defaults.TrackShape,
            tickMarkShape: sliderTheme.TickMarkShape ?? defaults.TickMarkShape,
            thumbShape: sliderTheme.ThumbShape ?? defaults.ThumbShape,
            overlayShape: sliderTheme.OverlayShape ?? defaults.OverlayShape,
            valueIndicatorShape: valueIndicatorShape,
            showValueIndicator:
                Widget.ShowValueIndicator ?? sliderTheme.ShowValueIndicator ?? defaultShowValueIndicator,
            valueIndicatorTextStyle: valueIndicatorTextStyle,
            padding: Widget.Padding ?? sliderTheme.Padding,
            thumbSize: sliderTheme.ThumbSize ?? defaults.ThumbSize,
            trackGap: sliderTheme.TrackGap ?? defaults.TrackGap);
        MouseCursor effectiveMouseCursor =
            ResolveAsMouseCursor(Widget.MouseCursor, states)
            ?? sliderTheme.MouseCursor?.Resolve(states)
            ?? WidgetStateMouseCursor.Clickable.Resolve(states)!;
        SliderInteraction effectiveAllowedInteraction =
            Widget.AllowedInteraction ?? sliderTheme.AllowedInteraction ?? defaultAllowedInteraction;

        // This size is used as the max bounds for the painting of the value indicators It must be kept
        // in sync with the function with the same name in range_slider.dart.
        Size ScreenSize() => MediaQuery.SizeOf(context);

        Action? handleDidGainAccessibilityFocus = null;
        switch (theme.Platform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.IOS:
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
                break;
            case TargetPlatform.Windows:
                handleDidGainAccessibilityFocus = () =>
                {
                    // Automatically activate the slider when it receives a11y focus.
                    if (!FocusNode.HasFocus && FocusNode.CanRequestFocus)
                    {
                        FocusNode.RequestFocus();
                    }
                };
                break;
        }

        IReadOnlyDictionary<ShortcutActivator, Intent> shortcutMap = MediaQuery.NavigationModeOf(context) switch
        {
            NavigationMode.Directional => DirectionalNavShortcutMap,
            NavigationMode.Traditional => TraditionalNavShortcutMap,
            _ => throw new InvalidOperationException("Unknown navigation mode."),
        };

        double fontSize = sliderTheme.ValueIndicatorTextStyle?.FontSize ?? TextDefaults.DefaultFontSize;
        double fontSizeToScale = fontSize == 0.0 ? TextDefaults.DefaultFontSize : fontSize;
        TextScaler textScaler = theme.UseMaterial3
            // TODO(tahatesser): This is an eye-balled value.
            // This needs to be updated when accessibility
            // guidelines are available on the material specs page
            // https://m3.material.io/components/sliders/accessibility.
            ? MediaQuery.TextScalerOf(context).Clamp(maxScaleFactor: 1.3)
            : MediaQuery.TextScalerOf(context);
        double effectiveTextScale = textScaler.Scale(fontSizeToScale) / fontSizeToScale;

        Widget result = new CompositedTransformTarget(
            link: _layerLink,
            child: new SliderRenderObjectWidget(
                key: _renderObjectKey,
                value: Convert(Widget.Value),
                secondaryTrackValue: Widget.SecondaryTrackValue != null
                    ? Convert(Widget.SecondaryTrackValue.Value)
                    : null,
                divisions: Widget.Divisions,
                label: Widget.Label,
                sliderTheme: sliderTheme,
                textScaleFactor: effectiveTextScale,
                screenSize: ScreenSize(),
                onChanged: Widget.OnChanged != null && Widget.Max > Widget.Min ? HandleChanged : null,
                onChangeStart: HandleDragStart,
                onChangeEnd: HandleDragEnd,
                state: this,
                semanticFormatterCallback: Widget.SemanticFormatterCallback,
                onDidGainAccessibilityFocus: handleDidGainAccessibilityFocus,
                hasFocus: _focused,
                hovering: _hovering,
                allowedInteraction: effectiveAllowedInteraction));

        EdgeInsetsGeometry? padding = Widget.Padding ?? sliderTheme.Padding;
        if (padding != null)
        {
            result = new Padding(padding.Value, child: result);
        }

        result = new OverlayPortal(
            controller: _valueIndicatorOverlayPortalController,
            overlayChildBuilder: _ => BuildValueIndicator(sliderTheme.ShowValueIndicator!.Value),
            child: result);

        return new FocusableActionDetector(
            actions: _actionMap,
            shortcuts: shortcutMap,
            focusNode: FocusNode,
            autofocus: Widget.Autofocus,
            enabled: Enabled,
            onShowFocusHighlight: HandleFocusHighlightChanged,
            onShowHoverHighlight: HandleHoverChanged,
            mouseCursor: effectiveMouseCursor,
            includeFocusSemantics: false,
            child: result);
    }

    private Widget BuildCupertinoSlider(BuildContext context)
    {
        // The render box of a slider has a fixed height but takes up the available width. Wrapping the
        // [CupertinoSlider] in this manner will help maintain the same size.
        return new SizedBox(
            width: double.PositiveInfinity,
            child: new CupertinoSlider(
                value: Widget.Value,
                onChanged: Widget.OnChanged,
                onChangeStart: Widget.OnChangeStart,
                onChangeEnd: Widget.OnChangeEnd,
                min: Widget.Min,
                max: Widget.Max,
                divisions: Widget.Divisions,
                activeColor: Widget.ActiveColor,
                thumbColor: Widget.ThumbColor ?? CupertinoColors.White));
    }

    private Widget BuildValueIndicator(ShowValueIndicator showValueIndicator)
    {
        Widget valueIndicator = new CompositedTransformFollower(
            link: _layerLink,
            child: new ValueIndicatorRenderObjectWidget(state: this));
#pragma warning disable CS0618 // Dart still handles the deprecated ShowValueIndicator.always.
        return showValueIndicator switch
        {
            ShowValueIndicator.Never => SizedBox.Shrink(),
            ShowValueIndicator.OnlyForDiscrete =>
                Widget.Divisions != null ? valueIndicator : SizedBox.Shrink(),
            ShowValueIndicator.OnlyForContinuous =>
                Widget.Divisions == null ? valueIndicator : SizedBox.Shrink(),
            ShowValueIndicator.AlwaysVisible or ShowValueIndicator.Always or ShowValueIndicator.OnDrag =>
                valueIndicator,
            _ => throw new ArgumentOutOfRangeException(nameof(showValueIndicator)),
        };
#pragma warning restore CS0618
    }

    // Dart's `WidgetStateProperty.resolveAs<MouseCursor?>`: Plumix's `WidgetStateMouseCursor` is not a
    // `WidgetStateProperty`, so the state-dependent case is tested for directly.
    private static MouseCursor? ResolveAsMouseCursor(MouseCursor? cursor, IReadOnlySet<WidgetState> states)
    {
        return cursor is WidgetStateMouseCursor stateCursor ? stateCursor.Resolve(states) : cursor;
    }

    // Dart's `OverlayPortalController(...)..show()` field initializer.
    private static OverlayPortalController ShowController(OverlayPortalController controller)
    {
        controller.Show();
        return controller;
    }
}

/// <summary>Dart's private <c>_SliderRenderObjectWidget</c>.</summary>
internal sealed class SliderRenderObjectWidget : LeafRenderObjectWidget
{
    public SliderRenderObjectWidget(
        double value,
        double? secondaryTrackValue,
        int? divisions,
        string? label,
        SliderThemeData sliderTheme,
        double textScaleFactor,
        Size screenSize,
        Action<double>? onChanged,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        SliderState state,
        SemanticFormatterCallback? semanticFormatterCallback,
        Action? onDidGainAccessibilityFocus,
        bool hasFocus,
        bool hovering,
        SliderInteraction allowedInteraction,
        Key? key = null) : base(key)
    {
        Value = value;
        SecondaryTrackValue = secondaryTrackValue;
        Divisions = divisions;
        Label = label;
        SliderTheme = sliderTheme;
        TextScaleFactor = textScaleFactor;
        ScreenSize = screenSize;
        OnChanged = onChanged;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        State = state;
        SemanticFormatterCallback = semanticFormatterCallback;
        OnDidGainAccessibilityFocus = onDidGainAccessibilityFocus;
        HasFocus = hasFocus;
        Hovering = hovering;
        AllowedInteraction = allowedInteraction;
    }

    public double Value { get; }
    public double? SecondaryTrackValue { get; }
    public int? Divisions { get; }
    public string? Label { get; }
    public SliderThemeData SliderTheme { get; }
    public double TextScaleFactor { get; }
    public Size ScreenSize { get; }
    public Action<double>? OnChanged { get; }
    public Action<double>? OnChangeStart { get; }
    public Action<double>? OnChangeEnd { get; }
    public SemanticFormatterCallback? SemanticFormatterCallback { get; }
    public Action? OnDidGainAccessibilityFocus { get; }
    public SliderState State { get; }
    public bool HasFocus { get; }
    public bool Hovering { get; }
    public SliderInteraction AllowedInteraction { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSlider(
            value: Value,
            secondaryTrackValue: SecondaryTrackValue,
            divisions: Divisions,
            label: Label,
            sliderTheme: SliderTheme,
            textScaleFactor: TextScaleFactor,
            screenSize: ScreenSize,
            onChanged: OnChanged,
            onChangeStart: OnChangeStart,
            onChangeEnd: OnChangeEnd,
            state: State,
            textDirection: Directionality.Of(context),
            semanticFormatterCallback: SemanticFormatterCallback,
            onDidGainAccessibilityFocus: OnDidGainAccessibilityFocus,
            platform: Theme.Of(context).Platform,
            hasFocus: HasFocus,
            hovering: Hovering,
            gestureSettings: MediaQuery.GestureSettingsOf(context),
            allowedInteraction: AllowedInteraction);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var slider = (RenderSlider)renderObject;
        // We should update the `divisions` ahead of `value`, because the `value` setter dependent on the
        // `divisions`.
        slider.Divisions = Divisions;
        slider.Value = Value;
        slider.SecondaryTrackValue = SecondaryTrackValue;
        slider.Label = Label;
        slider.SliderTheme = SliderTheme;
        slider.TextScaleFactor = TextScaleFactor;
        slider.ScreenSize = ScreenSize;
        slider.OnChanged = OnChanged;
        slider.OnChangeStart = OnChangeStart;
        slider.OnChangeEnd = OnChangeEnd;
        slider.TextDirection = Directionality.Of(context);
        slider.SemanticFormatterCallback = SemanticFormatterCallback;
        slider.OnDidGainAccessibilityFocus = OnDidGainAccessibilityFocus;
        slider.Platform = Theme.Of(context).Platform;
        slider.HasFocus = HasFocus;
        slider.Hovering = Hovering;
        slider.GestureSettings = MediaQuery.GestureSettingsOf(context);
        slider.AllowedInteraction = AllowedInteraction;
        // Ticker provider cannot change since there's a 1:1 relationship between the
        // SliderRenderObjectWidget object and the SliderState object.
    }
}

/// <summary>Dart's private <c>_RenderSlider</c>.</summary>
/// <remarks>
/// Dart mixes in <c>RelayoutWhenSystemFontsChangeMixin</c>, which Plumix has not ported; its
/// <c>systemFontsDidChange</c> override is kept as <see cref="SystemFontsDidChange"/>.
/// </remarks>
internal sealed class RenderSlider : RenderBox
{
    private static readonly TimeSpan PositionAnimationDuration = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan MinimumInteractionTime = TimeSpan.FromMilliseconds(500);

    // Dart's `const AlwaysStoppedAnimation<double>(1)`.
    private static readonly AlwaysStoppedAnimation<double> AlwaysComplete = new(1.0);

    // This value is the touch target, 48, multiplied by 3.
    private const double MinPreferredTrackWidth = 144.0;

    private readonly SliderState _state;
    private readonly CurvedAnimation _overlayAnimation;
    private readonly CurvedAnimation _valueIndicatorAnimation;
    private readonly CurvedAnimation _enableAnimation;
    private readonly TextPainter _labelPainter = new();
    private readonly HorizontalDragGestureRecognizer _drag;
    private readonly TapGestureRecognizer _tap;
    private bool _active;
    private double _currentDragValue;

    private double _value;
    private double? _secondaryTrackValue;
    private TargetPlatform _platform;
    private SemanticFormatterCallback? _semanticFormatterCallback;
    private int? _divisions;
    private string? _label;
    private SliderThemeData _sliderTheme;
    private double _textScaleFactor;
    private Size _screenSize;
    private Action<double>? _onChanged;
    private TextDirection _textDirection;
    private bool _hasFocus;
    private bool _hovering;
    private bool _hoveringThumb;
    private SliderInteraction _allowedInteraction;

    public RenderSlider(
        double value,
        double? secondaryTrackValue,
        int? divisions,
        string? label,
        SliderThemeData sliderTheme,
        double textScaleFactor,
        Size screenSize,
        TargetPlatform platform,
        Action<double>? onChanged,
        SemanticFormatterCallback? semanticFormatterCallback,
        Action? onDidGainAccessibilityFocus,
        Action<double>? onChangeStart,
        Action<double>? onChangeEnd,
        SliderState state,
        TextDirection textDirection,
        bool hasFocus,
        bool hovering,
        DeviceGestureSettings? gestureSettings,
        SliderInteraction allowedInteraction)
    {
        DebugAssertions.Assert(value >= 0.0 && value <= 1.0, "_value >= 0.0 && _value <= 1.0");
        DebugAssertions.Assert(
            secondaryTrackValue is null || (secondaryTrackValue >= 0.0 && secondaryTrackValue <= 1.0),
            "_secondaryTrackValue == null || (_secondaryTrackValue >= 0.0 && _secondaryTrackValue <= 1.0)");
        _value = value;
        _secondaryTrackValue = secondaryTrackValue;
        _divisions = divisions;
        _label = label;
        _sliderTheme = sliderTheme;
        _textScaleFactor = textScaleFactor;
        _screenSize = screenSize;
        _platform = platform;
        _onChanged = onChanged;
        _semanticFormatterCallback = semanticFormatterCallback;
        OnDidGainAccessibilityFocus = onDidGainAccessibilityFocus;
        OnChangeStart = onChangeStart;
        OnChangeEnd = onChangeEnd;
        _state = state;
        _textDirection = textDirection;
        _hasFocus = hasFocus;
        _hovering = hovering;
        _allowedInteraction = allowedInteraction;

        UpdateLabelPainter();
        var team = new GestureArenaTeam();
        _drag = new HorizontalDragGestureRecognizer
        {
            Team = team,
            OnStart = HandleDragStart,
            OnUpdate = HandleDragUpdate,
            OnEnd = HandleDragEnd,
            OnCancel = EndInteraction,
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

    // Compute the largest width and height needed to paint the slider shapes, other than the track
    // shape. It is assumed that these shapes are vertically centered on the track.
    private double MaxSliderPartWidth => SliderPartSizes.Select(size => size.Width).Aggregate(Math.Max);

    private double MaxSliderPartHeight => SliderPartSizes.Select(size => size.Height).Aggregate(Math.Max);

    private double ThumbSizeHeight =>
        _sliderTheme.ThumbShape!.GetPreferredSize(IsInteractive, IsDiscrete).Height;

    private double OverlayHeight =>
        _sliderTheme.OverlayShape!.GetPreferredSize(IsInteractive, IsDiscrete).Height;

    private List<Size> SliderPartSizes =>
    [
        new Size(
            _sliderTheme.OverlayShape!.GetPreferredSize(IsInteractive, IsDiscrete).Width,
            _sliderTheme.Padding != null ? ThumbSizeHeight : OverlayHeight),
        _sliderTheme.ThumbShape!.GetPreferredSize(IsInteractive, IsDiscrete),
        _sliderTheme.TickMarkShape!.GetPreferredSize(isEnabled: IsInteractive, sliderTheme: SliderTheme),
    ];

    private double MinPreferredTrackHeight => _sliderTheme.TrackHeight!.Value;

    public Action? OnDidGainAccessibilityFocus { get; set; }

    public Rect? OverlayRect { get; set; }

    // This rect is used in gesture calculations, where the gesture coordinates are relative to the
    // sliders origin. Therefore, the offset is passed as (0,0).
    private Rect TrackRect => _sliderTheme.TrackShape!.GetPreferredRect(
        parentBox: this,
        sliderTheme: _sliderTheme,
        isDiscrete: false);

    public bool IsInteractive => OnChanged != null;

    public bool IsDiscrete => Divisions != null && Divisions > 0;

    public double Value
    {
        get => _value;
        set
        {
            DebugAssertions.Assert(value >= 0.0 && value <= 1.0, "newValue >= 0.0 && newValue <= 1.0");
            double convertedValue = IsDiscrete ? Discretize(value) : value;
            if (convertedValue == _value)
            {
                return;
            }

            _value = convertedValue;
            if (IsDiscrete)
            {
                // Reset the duration to match the distance that we're traveling, so that whatever the
                // distance, we still do it in PositionAnimationDuration, and if we get re-targeted in
                // the middle, it still takes that long to get to the new location.
                double distance = Math.Abs(_value - _state.PositionController.Value);
                _state.PositionController.Duration = distance != 0.0
                    ? DurationTimes(PositionAnimationDuration, 1.0 / distance)
                    : TimeSpan.Zero;
                _state.PositionController.AnimateTo(convertedValue, curve: Curves.EaseInOut);
            }
            else
            {
                _state.PositionController.SetValue(convertedValue);
            }

            MarkNeedsSemanticsUpdate();
        }
    }

    public double? SecondaryTrackValue
    {
        get => _secondaryTrackValue;
        set
        {
            DebugAssertions.Assert(
                value is null || (value >= 0.0 && value <= 1.0),
                "newValue == null || (newValue >= 0.0 && newValue <= 1.0)");
            if (value == _secondaryTrackValue)
            {
                return;
            }

            _secondaryTrackValue = value;
            MarkNeedsPaint();
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

    public string? Label
    {
        get => _label;
        set
        {
            if (value == _label)
            {
                return;
            }

            _label = value;
            UpdateLabelPainter();
        }
    }

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
            UpdateLabelPainter();
        }
    }

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
            UpdateLabelPainter();
        }
    }

    public Size ScreenSize
    {
        get => _screenSize;
        set
        {
            if (value == _screenSize)
            {
                return;
            }

            _screenSize = value;
            MarkNeedsPaint();
        }
    }

    public Action<double>? OnChanged
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
                if (IsInteractive)
                {
                    _state.EnableController.Forward();
                }
                else
                {
                    _state.EnableController.Reverse();
                }

                MarkNeedsPaint();
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
            if (value == _textDirection)
            {
                return;
            }

            _textDirection = value;
            UpdateLabelPainter();
        }
    }

    /// <summary>True if this slider has the input focus.</summary>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (value == _hasFocus)
            {
                return;
            }

            _hasFocus = value;
            UpdateForFocus(_hasFocus);
            MarkNeedsSemanticsUpdate();
        }
    }

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

    /// <summary>True if the slider is interactive and the slider thumb is being hovered over by a pointer.</summary>
    public bool HoveringThumb
    {
        get => _hoveringThumb;
        set
        {
            if (value == _hoveringThumb)
            {
                return;
            }

            _hoveringThumb = value;
            UpdateForHover(_hovering);
        }
    }

    public SliderInteraction AllowedInteraction
    {
        get => _allowedInteraction;
        set
        {
            if (value == _allowedInteraction)
            {
                return;
            }

            _allowedInteraction = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    private void UpdateForFocus(bool focused)
    {
        if (focused)
        {
            _state.OverlayController.Forward();
            if (ShouldShowValueIndicatorWhenDragged)
            {
                _state.ValueIndicatorController.Forward();
            }
        }
        else
        {
            _state.OverlayController.Reverse();
            if (ShouldShowValueIndicatorWhenDragged)
            {
                _state.ValueIndicatorController.Reverse();
            }
        }
    }

    private void UpdateForHover(bool hovered)
    {
        // Only show overlay when pointer is hovering the thumb.
        if (hovered && HoveringThumb)
        {
            _state.OverlayController.Forward();
        }
        else
        {
            // Only remove overlay when Slider is inactive and unfocused.
            if (!_active && !HasFocus)
            {
                _state.OverlayController.Reverse();
            }
        }
    }

    public bool ShouldAlwaysShowValueIndicator =>
        _sliderTheme.ShowValueIndicator == ShowValueIndicator.AlwaysVisible;

#pragma warning disable CS0618 // Dart still handles the deprecated ShowValueIndicator.always.
    public bool ShouldShowValueIndicatorWhenDragged => _sliderTheme.ShowValueIndicator!.Value switch
    {
        ShowValueIndicator.OnlyForDiscrete => IsDiscrete,
        ShowValueIndicator.OnlyForContinuous => !IsDiscrete,
        ShowValueIndicator.Always or ShowValueIndicator.OnDrag => true,
        ShowValueIndicator.Never or ShowValueIndicator.AlwaysVisible => false,
        _ => throw new InvalidOperationException("Unknown ShowValueIndicator value."),
    };
#pragma warning restore CS0618

    private double AdjustmentUnit
    {
        get
        {
            switch (_platform)
            {
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                    // Matches iOS implementation of material slider.
                    return 0.1;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Linux:
                case TargetPlatform.Windows:
                default:
                    // Matches Android implementation of material slider.
                    return 0.05;
            }
        }
    }

    private void UpdateLabelPainter()
    {
        if (Label != null)
        {
            _labelPainter.Text = new TextSpan(style: _sliderTheme.ValueIndicatorTextStyle, text: Label);
            _labelPainter.TextDirection = TextDirection;
            // Dart's deprecated `textScaleFactor` setter, which assigns a linear scaler.
            _labelPainter.TextScaler = TextScaler.Linear(TextScaleFactor);
            _labelPainter.Layout();
        }
        else
        {
            _labelPainter.Text = null;
        }

        // Changing the textDirection can result in the layout changing, because the bidi algorithm
        // might line up the glyphs differently which can result in different ligatures, different
        // shapes, etc. So we always markNeedsLayout.
        MarkNeedsLayout();
    }

    /// <summary>
    /// Dart's <c>systemFontsDidChange</c> override. <c>RelayoutWhenSystemFontsChangeMixin</c> is not
    /// ported, so nothing calls this yet.
    /// </summary>
    public void SystemFontsDidChange()
    {
        _labelPainter.MarkNeedsLayout();
        UpdateLabelPainter();
    }

    protected override void OnAttach()
    {
        _overlayAnimation.AddListener(MarkNeedsPaint);
        _valueIndicatorAnimation.AddListener(MarkNeedsPaint);
        _enableAnimation.AddListener(MarkNeedsPaint);
        _state.PositionController.AddListener(MarkNeedsPaint);
    }

    protected override void OnDetach()
    {
        _overlayAnimation.RemoveListener(MarkNeedsPaint);
        _valueIndicatorAnimation.RemoveListener(MarkNeedsPaint);
        _enableAnimation.RemoveListener(MarkNeedsPaint);
        _state.PositionController.RemoveListener(MarkNeedsPaint);
    }

    public override void Dispose()
    {
        _drag.Dispose();
        _tap.Dispose();
        _labelPainter.Dispose();
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
            _ => visualPosition,
        };
    }

    private double GetValueFromGlobalPosition(Point globalPosition)
    {
        Rect trackRect = TrackRect;
        double visualPosition = (GlobalToLocal(globalPosition).X - trackRect.Left) / trackRect.Width;
        return GetValueFromVisualPosition(visualPosition);
    }

    private double Discretize(double value)
    {
        double result = ClampDouble(value, 0.0, 1.0);
        if (IsDiscrete)
        {
            result = Math.Round(result * Divisions!.Value, MidpointRounding.AwayFromZero) / Divisions!.Value;
        }

        return result;
    }

    private void StartInteraction(Point globalPosition)
    {
        if (!_state.Mounted)
        {
            return;
        }

        if (!_active && IsInteractive)
        {
            switch (AllowedInteraction)
            {
                case SliderInteraction.TapAndSlide:
                case SliderInteraction.TapOnly:
                    _active = true;
                    _currentDragValue = GetValueFromGlobalPosition(globalPosition);
                    break;
                case SliderInteraction.SlideThumb:
                    if (IsPointerOnOverlay(globalPosition))
                    {
                        _active = true;
                        _currentDragValue = Value;
                    }

                    break;
                case SliderInteraction.SlideOnly:
                    _active = true;
                    _currentDragValue = Value;
                    break;
            }

            if (_active)
            {
                // We supply the *current* value as the start location, so that if we have a tap, it
                // consists of a call to onChangeStart with the previous value and a call to
                // onChangeEnd with the new value.
                OnChangeStart?.Invoke(Discretize(Value));
                OnChanged!(Discretize(_currentDragValue));
                _state.OverlayController.Forward();
                if (ShouldShowValueIndicatorWhenDragged)
                {
                    _state.ValueIndicatorController.Forward();
                    _state.InteractionTimer?.Cancel();
                    _state.InteractionTimer = GestureTimer.Start(
                        DurationTimes(MinimumInteractionTime, Scheduler.TimeDilation),
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
    }

    private void EndInteraction()
    {
        if (!_state.Mounted)
        {
            return;
        }

        if (_active && _state.Mounted)
        {
            OnChangeEnd?.Invoke(Discretize(_currentDragValue));
            _active = false;
            _currentDragValue = 0.0;
            _state.OverlayController.Reverse();
            if (ShouldShowValueIndicatorWhenDragged && _state.InteractionTimer == null)
            {
                _state.ValueIndicatorController.Reverse();
            }
        }
    }

    private void HandleDragStart(DragStartDetails details)
    {
        StartInteraction(details.GlobalPosition);
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!_state.Mounted)
        {
            return;
        }

        switch (AllowedInteraction)
        {
            case SliderInteraction.TapAndSlide:
            case SliderInteraction.SlideOnly:
            case SliderInteraction.SlideThumb:
                if (_active && IsInteractive)
                {
                    double valueDelta = details.PrimaryDelta!.Value / TrackRect.Width;
                    _currentDragValue += TextDirection switch
                    {
                        TextDirection.Rtl => -valueDelta,
                        _ => valueDelta,
                    };
                    OnChanged!(Discretize(_currentDragValue));
                }

                break;
            case SliderInteraction.TapOnly:
                // cannot slide (drag) as its tapOnly.
                break;
        }
    }

    private void HandleDragEnd(DragEndDetails details)
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

    private bool IsPointerOnOverlay(Point globalPosition)
    {
        return OverlayRect!.Value.ContainsHalfOpen(GlobalToLocal(globalPosition));
    }

    protected override bool HitTestSelf(Point position) => true;

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (!_state.Mounted)
        {
            return;
        }

        DebugAssertions.Assert(DebugHandleEvent(@event, entry));
        if (@event is PointerDownEvent downEvent && IsInteractive)
        {
            // We need to add the drag first so that it has priority.
            _drag.AddPointer(downEvent);
            _tap.AddPointer(downEvent);
        }

        if (IsInteractive && OverlayRect != null)
        {
            HoveringThumb = OverlayRect.Value.ContainsHalfOpen(@event.LocalPosition);
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        MinPreferredTrackWidth + MaxSliderPartWidth;

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        MinPreferredTrackWidth + MaxSliderPartWidth;

    protected override double ComputeMinIntrinsicHeight(double width) =>
        Math.Max(MinPreferredTrackHeight, MaxSliderPartHeight);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        Math.Max(MinPreferredTrackHeight, MaxSliderPartHeight);

    protected override bool SizedByParent => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return new Size(
            constraints.HasBoundedWidth
                ? constraints.MaxWidth
                : MinPreferredTrackWidth + MaxSliderPartWidth,
            constraints.HasBoundedHeight
                ? constraints.MaxHeight
                : Math.Max(MinPreferredTrackHeight, MaxSliderPartHeight));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        double controllerValue = _state.PositionController.Value;

        // The visual position is the position of the thumb from 0 to 1 from left to right. In left to
        // right, this is the same as the value, but it is reversed for right to left text.
        (double visualPosition, double? secondaryVisualPosition) = TextDirection switch
        {
            TextDirection.Rtl when _secondaryTrackValue == null => (1.0 - controllerValue, (double?)null),
            TextDirection.Rtl => (1.0 - controllerValue, 1.0 - _secondaryTrackValue!.Value),
            _ => (controllerValue, _secondaryTrackValue),
        };

        Rect trackRect = _sliderTheme.TrackShape!.GetPreferredRect(
            parentBox: this,
            offset: offset,
            sliderTheme: _sliderTheme,
            isDiscrete: IsDiscrete);

        Point thumbCenter = CalcThumbCenter(trackRect: trackRect, visualPosition: visualPosition);

        if (IsInteractive)
        {
            Size overlaySize = SliderTheme.OverlayShape!.GetPreferredSize(IsInteractive, false);
            double radius = overlaySize.Width / 2.0;
            // Dart's `Rect.fromCircle(center: thumbCenter, radius: radius)`.
            OverlayRect = new Rect(thumbCenter.X - radius, thumbCenter.Y - radius, radius * 2.0, radius * 2.0);
        }

        Point? secondaryOffset = secondaryVisualPosition != null
            ? new Point(trackRect.Left + secondaryVisualPosition.Value * trackRect.Width, trackRect.Center.Y)
            : null;

        // If [Slider.year2023] is false, the thumb uses handle thumb shape and gapped track shape.
        // The handle width and track gap are adjusted when the thumb is pressed.
        double? thumbWidth = _sliderTheme.ThumbSize?.Resolve(new HashSet<WidgetState>())?.Width;
        double? thumbHeight = _sliderTheme.ThumbSize?.Resolve(new HashSet<WidgetState>())?.Height;
        double? trackGap = _sliderTheme.TrackGap;
        double? pressedThumbWidth = _sliderTheme.ThumbSize
            ?.Resolve(new HashSet<WidgetState> { WidgetState.Pressed })
            ?.Width;
        double delta;
        if (_active && thumbWidth != null && pressedThumbWidth != null && trackGap != null)
        {
            delta = thumbWidth.Value - pressedThumbWidth.Value;
            if (thumbWidth > 0.0)
            {
                thumbWidth = pressedThumbWidth;
            }

            if (trackGap > 0.0)
            {
                trackGap = trackGap - delta / 2;
            }
        }

        _sliderTheme.TrackShape!.Paint(
            context,
            offset,
            parentBox: this,
            sliderTheme: _sliderTheme.CopyWith(trackGap: trackGap),
            enableAnimation: _enableAnimation,
            textDirection: _textDirection,
            thumbCenter: thumbCenter,
            secondaryOffset: secondaryOffset,
            isDiscrete: IsDiscrete,
            isEnabled: IsInteractive);

        if (!_overlayAnimation.Status.IsDismissed())
        {
            _sliderTheme.OverlayShape!.Paint(
                context,
                thumbCenter,
                activationAnimation: _overlayAnimation,
                enableAnimation: _enableAnimation,
                isDiscrete: IsDiscrete,
                labelPainter: _labelPainter,
                parentBox: this,
                sliderTheme: _sliderTheme,
                textDirection: _textDirection,
                value: _value,
                textScaleFactor: _textScaleFactor,
                sizeWithOverflow: ScreenSize.IsEmpty ? Size : ScreenSize);
        }

        if (IsDiscrete)
        {
            double tickMarkWidth = _sliderTheme.TickMarkShape!
                .GetPreferredSize(isEnabled: IsInteractive, sliderTheme: _sliderTheme)
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
                    // The ticks are mapped to be within the track, so the tick mark width must be
                    // subtracted from the track width.
                    double dx = trackRect.Left + value * adjustedTrackWidth + discreteTrackPadding / 2;
                    var tickMarkOffset = new Point(dx, dy);
                    _sliderTheme.TickMarkShape!.Paint(
                        context,
                        tickMarkOffset,
                        parentBox: this,
                        sliderTheme: _sliderTheme,
                        enableAnimation: _enableAnimation,
                        textDirection: _textDirection,
                        thumbCenter: thumbCenter,
                        isEnabled: IsInteractive);
                }
            }
        }

        if (IsInteractive
            && Label != null
            && ((ShouldShowValueIndicatorWhenDragged && !_valueIndicatorAnimation.Status.IsDismissed())
                || ShouldAlwaysShowValueIndicator))
        {
            _state.PaintValueIndicator = (PaintingContext paintingContext, Point paintOffset) =>
            {
                if (Attached && _labelPainter.Text != null)
                {
                    _sliderTheme.ValueIndicatorShape?.Paint(
                        paintingContext,
                        paintOffset + thumbCenter,
                        activationAnimation: ShouldAlwaysShowValueIndicator
                            ? AlwaysComplete
                            : _valueIndicatorAnimation,
                        enableAnimation: ShouldAlwaysShowValueIndicator
                            ? AlwaysComplete
                            : _enableAnimation,
                        isDiscrete: IsDiscrete,
                        labelPainter: _labelPainter,
                        parentBox: this,
                        sliderTheme: _sliderTheme,
                        textDirection: _textDirection,
                        value: _value,
                        textScaleFactor: TextScaleFactor,
                        sizeWithOverflow: ScreenSize.IsEmpty ? Size : ScreenSize);
                }
            };
        }
        else
        {
            _state.PaintValueIndicator = null;
        }

        _sliderTheme.ThumbShape!.Paint(
            context,
            thumbCenter,
            activationAnimation: _overlayAnimation,
            enableAnimation: _enableAnimation,
            isDiscrete: IsDiscrete,
            labelPainter: _labelPainter,
            parentBox: this,
            sliderTheme: thumbWidth != null && thumbHeight != null
                ? _sliderTheme.CopyWith(
                    thumbSize: new WidgetStatePropertyAll<Size?>(new Size(thumbWidth.Value, thumbHeight.Value)))
                : _sliderTheme,
            textDirection: _textDirection,
            value: _value,
            textScaleFactor: TextScaleFactor,
            sizeWithOverflow: ScreenSize.IsEmpty ? Size : ScreenSize);
    }

    /// <summary>
    /// Calculates the local coordinate center of the [Slider] thumb given its physical placement on
    /// the track from 0.0 (left) to 1.0 (right).
    /// </summary>
    /// <remarks>
    /// The [visualPosition] is provided by the caller so semantics can use the raw logical value while
    /// paint can use the smoothly animated value.
    /// </remarks>
    private Point CalcThumbCenter(Rect trackRect, double visualPosition)
    {
        double padding = _sliderTheme.TrackShape!.IsRounded ? trackRect.Height : 0.0;
        double thumbPosition = IsDiscrete
            ? trackRect.Left + visualPosition * (trackRect.Width - padding) + padding / 2
            : trackRect.Left + visualPosition * trackRect.Width;
        // Apply padding to trackRect.left and trackRect.right if the track height is greater than the
        // thumb radius to ensure the thumb is drawn within the track.
        Size thumbPreferredSize = _sliderTheme.ThumbShape!.GetPreferredSize(IsInteractive, IsDiscrete);
        double thumbPadding = padding > thumbPreferredSize.Width / 2 ? padding / 2 : 0;
        return new Point(
            ClampDouble(thumbPosition, trackRect.Left + thumbPadding, trackRect.Right - thumbPadding),
            trackRect.Center.Y);
    }

    private Point SemanticThumbCenter
    {
        get
        {
            double visualPosition = TextDirection switch
            {
                TextDirection.Rtl => 1.0 - _value,
                _ => _value,
            };
            return CalcThumbCenter(trackRect: TrackRect, visualPosition: visualPosition);
        }
    }

    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        Point center = SemanticThumbCenter;
        const double extent = WidgetConstants.MinInteractiveDimension;
        // Dart's `Rect.fromCenter(center: ..., width: kMinInteractiveDimension, height: ...)`.
        node.Rect = new Rect(center.X - extent / 2.0, center.Y - extent / 2.0, extent, extent);

        node.UpdateWith(config: config);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration config)
    {
        base.DescribeSemanticsConfiguration(config);

        // The Slider widget has its own Focus widget. We mark the Focus widget with
        // "includeFocusSemantics: false" and we want that semantics node to collect the semantics
        // information here so that it's all in the same node.
        config.IsSemanticBoundary = true;

        config.IsEnabled = IsInteractive;
        if (Label != null)
        {
            config.Label = Label;
        }

        config.IsSlider = true;
        config.IsFocusable = IsInteractive;
        config.IsFocused = HasFocus;

        if (OnDidGainAccessibilityFocus != null)
        {
            config.OnDidGainAccessibilityFocus = OnDidGainAccessibilityFocus;
        }

        config.TextDirection = TextDirection;
        if (IsInteractive)
        {
            config.OnIncrease = IncreaseAction;
            config.OnDecrease = DecreaseAction;
            config.OnFocus = OnFocusAction;
        }

        if (SemanticFormatterCallback != null)
        {
            config.Value = SemanticFormatterCallback(_state.Lerp(Value));
            config.IncreasedValue = SemanticFormatterCallback(
                _state.Lerp(ClampDouble(Value + SemanticActionUnit, 0.0, 1.0)));
            config.DecreasedValue = SemanticFormatterCallback(
                _state.Lerp(ClampDouble(Value - SemanticActionUnit, 0.0, 1.0)));
        }
        else
        {
            config.Value = $"{DartRound(Value * 100)}%";
            config.IncreasedValue = $"{DartRound(ClampDouble(Value + SemanticActionUnit, 0.0, 1.0) * 100)}%";
            config.DecreasedValue = $"{DartRound(ClampDouble(Value - SemanticActionUnit, 0.0, 1.0) * 100)}%";
        }
    }

    private double SemanticActionUnit => Divisions != null ? 1.0 / Divisions.Value : AdjustmentUnit;

    public void OnFocusAction()
    {
        if (IsInteractive)
        {
            if (!_state.Mounted)
            {
                return;
            }

            if (!HasFocus)
            {
                _state.FocusNode.RequestFocus();
            }
        }
    }

    public void IncreaseAction()
    {
        if (IsInteractive)
        {
            OnChangeStart!(CurrentValue);
            double increase = IncreaseValue();
            OnChanged!(increase);
            OnChangeEnd!(increase);
            if (!_state.Mounted)
            {
                return;
            }
        }
    }

    public void DecreaseAction()
    {
        if (IsInteractive)
        {
            OnChangeStart!(CurrentValue);
            double decrease = DecreaseValue();
            OnChanged!(decrease);
            OnChangeEnd!(decrease);
            if (!_state.Mounted)
            {
                return;
            }
        }
    }

    public double CurrentValue => ClampDouble(Value, 0.0, 1.0);

    public double IncreaseValue()
    {
        return ClampDouble(Value + SemanticActionUnit, 0.0, 1.0);
    }

    public double DecreaseValue()
    {
        return ClampDouble(Value - SemanticActionUnit, 0.0, 1.0);
    }

    // Dart's `clampDouble` (foundation/math.dart).
    private static double ClampDouble(double x, double min, double max)
    {
        DebugAssertions.Assert(
            min <= max && !double.IsNaN(max) && !double.IsNaN(min),
            "min <= max && !max.isNaN && !min.isNaN");
        if (x < min)
        {
            return min;
        }

        if (x > max)
        {
            return max;
        }

        if (double.IsNaN(x))
        {
            return max;
        }

        return x;
    }

    // Dart's `double.round()`: half away from zero, returned as an integer.
    private static long DartRound(double value) => (long)Math.Round(value, MidpointRounding.AwayFromZero);

    // Dart's `Duration * num`: the microsecond count times the factor, rounded.
    private static TimeSpan DurationTimes(TimeSpan duration, double factor)
    {
        long microseconds = (long)Math.Round(
            duration.Ticks / (double)TimeSpan.TicksPerMicrosecond * factor,
            MidpointRounding.AwayFromZero);
        return TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMicrosecond);
    }
}

/// <summary>Dart's private <c>_AdjustSliderIntent</c>.</summary>
internal sealed class AdjustSliderIntent : Intent
{
    public AdjustSliderIntent(SliderAdjustmentType type)
    {
        Type = type;
    }

    public static AdjustSliderIntent Right() => new(SliderAdjustmentType.Right);

    public static AdjustSliderIntent Left() => new(SliderAdjustmentType.Left);

    public static AdjustSliderIntent Up() => new(SliderAdjustmentType.Up);

    public static AdjustSliderIntent Down() => new(SliderAdjustmentType.Down);

    public SliderAdjustmentType Type { get; }
}

/// <summary>Dart's private <c>_SliderAdjustmentType</c>.</summary>
internal enum SliderAdjustmentType
{
    Right,
    Left,
    Up,
    Down,
}

// Dart's library-private `_ValueIndicatorRenderObjectWidget` and `_RenderValueIndicator`. range_slider.dart
// declares private classes with the same names, so here they are nested in `SliderState`, the only type that
// uses them, to keep Dart's names without colliding in `Plumix.Material`.
internal sealed partial class SliderState
{
    /// <summary>Dart's private <c>_ValueIndicatorRenderObjectWidget</c>.</summary>
    private sealed class ValueIndicatorRenderObjectWidget : LeafRenderObjectWidget
    {
        public ValueIndicatorRenderObjectWidget(SliderState state)
        {
            State = state;
        }

        public SliderState State { get; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderValueIndicator(state: State);
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderValueIndicator)renderObject).State = State;
        }
    }

    /// <summary>Dart's private <c>_RenderValueIndicator</c>.</summary>
    /// <remarks>
    /// Dart mixes in <c>RelayoutWhenSystemFontsChangeMixin</c>, which Plumix has not ported.
    /// </remarks>
    private sealed class RenderValueIndicator : RenderBox
    {
        private readonly CurvedAnimation _valueIndicatorAnimation;

        public RenderValueIndicator(SliderState state)
        {
            State = state;
            _valueIndicatorAnimation = new CurvedAnimation(
                parent: State.ValueIndicatorController,
                curve: Curves.FastOutSlowIn);
        }

        /// <summary>Dart's <c>_state</c>, reassigned by the widget's <c>updateRenderObject</c>.</summary>
        public SliderState State { get; set; }

        protected override bool SizedByParent => true;

        protected override void OnAttach()
        {
            _valueIndicatorAnimation.AddListener(MarkNeedsPaint);
            State.PositionController.AddListener(MarkNeedsPaint);
        }

        protected override void OnDetach()
        {
            _valueIndicatorAnimation.RemoveListener(MarkNeedsPaint);
            State.PositionController.RemoveListener(MarkNeedsPaint);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
            State.PaintValueIndicator?.Invoke(context, offset);
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
}

/// <summary>Dart's private <c>_SliderDefaultsM2</c>.</summary>
internal sealed record SliderDefaultsM2 : SliderThemeData
{
    private readonly BuildContext _context;
    private readonly ColorScheme _colors;
    private readonly SliderThemeData _sliderTheme;

    // Dart reads its two `late final` fields lazily; both only register dependencies that the slider's
    // build has already registered, so they are read up front.
    public SliderDefaultsM2(BuildContext context) : base(TrackHeight: 4.0)
    {
        _context = context;
        _colors = Theme.Of(context).ColorScheme;
        _sliderTheme = Plumix.Material.SliderTheme.Of(context);
    }

    public override Color? ActiveTrackColor => _colors.Primary;

    public override Color? InactiveTrackColor => _colors.Primary.WithOpacity(0.24);

    public override Color? SecondaryActiveTrackColor => _colors.Primary.WithOpacity(0.54);

    public override Color? DisabledActiveTrackColor => _colors.OnSurface.WithOpacity(0.32);

    public override Color? DisabledInactiveTrackColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? DisabledSecondaryActiveTrackColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? ActiveTickMarkColor => _colors.OnPrimary.WithOpacity(0.54);

    public override Color? InactiveTickMarkColor => _colors.Primary.WithOpacity(0.54);

    public override Color? DisabledActiveTickMarkColor => _colors.OnPrimary.WithOpacity(0.12);

    public override Color? DisabledInactiveTickMarkColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? ThumbColor => _colors.Primary;

    public override Color? DisabledThumbColor =>
        Color.AlphaBlend(_colors.OnSurface.WithOpacity(.38), _colors.Surface);

    public override Color? OverlayColor => _colors.Primary.WithOpacity(0.12);

    public override TextStyle? ValueIndicatorTextStyle =>
        Theme.Of(_context).TextTheme.BodyLarge.CopyWith(color: _colors.OnPrimary);

    public override Color? ValueIndicatorColor
    {
        get
        {
            if (_sliderTheme.ValueIndicatorShape is RoundedRectSliderValueIndicatorShape)
            {
                return _colors.InverseSurface;
            }

            return _colors.Primary;
        }
    }

    public override SliderComponentShape? ValueIndicatorShape => SliderConstShapes.RectangularValueIndicator;

    public override SliderComponentShape? ThumbShape => SliderConstShapes.RoundThumb;

    public override SliderTrackShape? TrackShape => SliderConstShapes.RoundedRectTrack;

    public override SliderComponentShape? OverlayShape => SliderConstShapes.RoundOverlay;

    public override SliderTickMarkShape? TickMarkShape => SliderConstShapes.RoundTickMark;
}

/// <summary>Dart's private <c>_SliderDefaultsM3Year2023</c>.</summary>
internal sealed record SliderDefaultsM3Year2023 : SliderThemeData
{
    private readonly BuildContext _context;
    private readonly ColorScheme _colors;

    public SliderDefaultsM3Year2023(BuildContext context) : base(TrackHeight: 4.0)
    {
        _context = context;
        _colors = Theme.Of(context).ColorScheme;
    }

    public override Color? ActiveTrackColor => _colors.Primary;

    public override Color? InactiveTrackColor => _colors.SurfaceContainerHighest;

    public override Color? SecondaryActiveTrackColor => _colors.Primary.WithOpacity(0.54);

    public override Color? DisabledActiveTrackColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? DisabledInactiveTrackColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? DisabledSecondaryActiveTrackColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? ActiveTickMarkColor => _colors.OnPrimary.WithOpacity(0.38);

    public override Color? InactiveTickMarkColor => _colors.OnSurfaceVariant.WithOpacity(0.38);

    public override Color? DisabledActiveTickMarkColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? DisabledInactiveTickMarkColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? ThumbColor => _colors.Primary;

    public override Color? DisabledThumbColor =>
        Color.AlphaBlend(_colors.OnSurface.WithOpacity(0.38), _colors.Surface);

    public override Color? OverlayColor => WidgetStateColor.ResolveWith(states =>
    {
        if (states.Contains(WidgetState.Dragged))
        {
            return _colors.Primary.WithOpacity(0.1);
        }

        if (states.Contains(WidgetState.Hovered))
        {
            return _colors.Primary.WithOpacity(0.08);
        }

        if (states.Contains(WidgetState.Focused))
        {
            return _colors.Primary.WithOpacity(0.1);
        }

        return Colors.Transparent;
    });

    public override TextStyle? ValueIndicatorTextStyle =>
        Theme.Of(_context).TextTheme.LabelMedium.CopyWith(color: _colors.OnPrimary);

    public override Color? ValueIndicatorColor => _colors.Primary;

    public override SliderComponentShape? ValueIndicatorShape => SliderConstShapes.DropValueIndicator;

    public override SliderComponentShape? ThumbShape => SliderConstShapes.RoundThumb;

    public override SliderTrackShape? TrackShape => SliderConstShapes.RoundedRectTrack;

    public override SliderComponentShape? OverlayShape => SliderConstShapes.RoundOverlay;

    public override SliderTickMarkShape? TickMarkShape => SliderConstShapes.RoundTickMark;
}

// BEGIN GENERATED TOKEN PROPERTIES - Slider

// Do not edit by hand. The code between the "BEGIN GENERATED" and
// "END GENERATED" comments are generated from data in the Material
// Design token database by the script:
//   dev/tools/gen_defaults/bin/gen_defaults.dart.

/// <summary>Dart's private <c>_SliderDefaultsM3</c>.</summary>
internal sealed record SliderDefaultsM3 : SliderThemeData
{
    private readonly BuildContext _context;
    private readonly ColorScheme _colors;

    public SliderDefaultsM3(BuildContext context) : base(TrackHeight: 16.0)
    {
        _context = context;
        _colors = Theme.Of(context).ColorScheme;
    }

    public override Color? ActiveTrackColor => _colors.Primary;

    public override Color? InactiveTrackColor => _colors.SecondaryContainer;

    public override Color? SecondaryActiveTrackColor => _colors.Primary.WithOpacity(0.54);

    public override Color? DisabledActiveTrackColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? DisabledInactiveTrackColor => _colors.OnSurface.WithOpacity(0.12);

    public override Color? DisabledSecondaryActiveTrackColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? ActiveTickMarkColor => _colors.OnPrimary.WithOpacity(1.0);

    public override Color? InactiveTickMarkColor => _colors.OnSecondaryContainer.WithOpacity(1.0);

    public override Color? DisabledActiveTickMarkColor => _colors.OnInverseSurface;

    public override Color? DisabledInactiveTickMarkColor => _colors.OnSurface;

    public override Color? ThumbColor => _colors.Primary;

    public override Color? DisabledThumbColor => _colors.OnSurface.WithOpacity(0.38);

    public override Color? OverlayColor => WidgetStateColor.ResolveWith(states =>
    {
        if (states.Contains(WidgetState.Dragged))
        {
            return _colors.Primary.WithOpacity(0.1);
        }

        if (states.Contains(WidgetState.Hovered))
        {
            return _colors.Primary.WithOpacity(0.08);
        }

        if (states.Contains(WidgetState.Focused))
        {
            return _colors.Primary.WithOpacity(0.1);
        }

        return Colors.Transparent;
    });

    public override TextStyle? ValueIndicatorTextStyle => Theme.Of(_context).TextTheme.LabelLarge.CopyWith(
        color: _colors.OnInverseSurface);

    public override Color? ValueIndicatorColor => _colors.InverseSurface;

    public override SliderComponentShape? ValueIndicatorShape => SliderConstShapes.RoundedRectValueIndicator;

    public override SliderComponentShape? ThumbShape => SliderConstShapes.HandleThumb;

    public override SliderTrackShape? TrackShape => SliderConstShapes.GappedTrack;

    public override SliderComponentShape? OverlayShape => SliderConstShapes.RoundOverlay;

    public override SliderTickMarkShape? TickMarkShape => SliderConstShapes.RoundTickMarkRadius2;

    public override WidgetStateProperty<Size?>? ThumbSize => WidgetStateProperty<Size?>.ResolveWith(states =>
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

    public override double? TrackGap => 6.0;
}

// END GENERATED TOKEN PROPERTIES - Slider

// C#-only: Dart's defaults return `const` shapes, which Dart canonicalizes, so every read of a default
// getter yields the identical instance and a rebuilt `SliderThemeData` still compares equal. C# has no
// const objects; these shared instances stand in for them.
file static class SliderConstShapes
{
    public static readonly SliderComponentShape RectangularValueIndicator =
        new RectangularSliderValueIndicatorShape();

    public static readonly SliderComponentShape DropValueIndicator = new DropSliderValueIndicatorShape();

    public static readonly SliderComponentShape RoundedRectValueIndicator =
        new RoundedRectSliderValueIndicatorShape();

    public static readonly SliderComponentShape RoundThumb = new RoundSliderThumbShape();

    public static readonly SliderComponentShape HandleThumb = new HandleThumbShape();

    public static readonly SliderTrackShape RoundedRectTrack = new RoundedRectSliderTrackShape();

    public static readonly SliderTrackShape GappedTrack = new GappedSliderTrackShape();

    public static readonly SliderComponentShape RoundOverlay = new RoundSliderOverlayShape();

    public static readonly SliderTickMarkShape RoundTickMark = new RoundSliderTickMarkShape();

    public static readonly SliderTickMarkShape RoundTickMarkRadius2 =
        new RoundSliderTickMarkShape(tickMarkRadius: 4.0 / 2);
}
