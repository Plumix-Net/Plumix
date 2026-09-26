using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider_theme.dart

/// <summary>
/// Applies a slider theme to descendant [Slider] widgets.
/// </summary>
public sealed class SliderTheme : InheritedTheme
{
    /// Applies the given theme [data] to [child].
    public SliderTheme(SliderThemeData data, Widget child, Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// Specifies the color and shape values for descendant slider widgets.
    public SliderThemeData Data { get; }

    /// Returns the data from the closest [SliderTheme] instance that encloses the given context,
    /// or [ThemeData.SliderTheme] when there is none.
    public static SliderThemeData Of(BuildContext context)
    {
        SliderTheme? inheritedTheme = context.DependOnInheritedWidgetOfExactType<SliderTheme>();
        return inheritedTheme != null ? inheritedTheme.Data : Theme.Of(context).SliderTheme;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new SliderTheme(data: Data, child: child);
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        Data != ((SliderTheme)oldWidget).Data;
}

/// <summary>
/// Describes the conditions under which the value indicator on a [Slider] will be shown.
/// </summary>
public enum ShowValueIndicator
{
    /// The value indicator will only be shown for discrete sliders (sliders where
    /// [Slider.Divisions] is non-null).
    OnlyForDiscrete,

    /// The value indicator will only be shown for continuous sliders (sliders where
    /// [Slider.Divisions] is null).
    OnlyForContinuous,

    /// The value indicator will be shown for all types of sliders while dragging.
    [Obsolete("Use ShowValueIndicator.OnDrag. This feature was deprecated after v3.28.0-1.0.pre.")]
    Always,

    /// The value indicator will be shown for all types of sliders while dragging.
    OnDrag,

    /// The value indicator will be shown for all types of sliders at all times.
    AlwaysVisible,

    /// The value indicator will never be shown.
    Never,
}

/// <summary>
/// Identifier for a thumb.
/// </summary>
public enum Thumb
{
    /// Left-most thumb for [TextDirection.Ltr], otherwise, right-most thumb.
    Start,

    /// Right-most thumb for [TextDirection.Ltr], otherwise, left-most thumb.
    End,
}

/// <summary>
/// Holds the color, shape, and typography values for a Material Design slider theme.
/// </summary>
/// <remarks>
/// Flutter declares an ordinary class that `_SliderDefaultsM2`/`_SliderDefaultsM3` and the
/// `_RangeSliderDefaults*` classes extend to override individual getters, so the record is not
/// sealed and its members are `virtual`. `==` and `hashCode` are Dart's: every getter is compared,
/// the runtime types must match, and `valueIndicatorStrokeColor` is left out of the hash.
/// </remarks>
public partial record SliderThemeData : IDiagnosticable
{
    /// Create a [SliderThemeData] given a set of exact values.
    public SliderThemeData(
        double? TrackHeight = null,
        Color? ActiveTrackColor = null,
        Color? InactiveTrackColor = null,
        Color? SecondaryActiveTrackColor = null,
        Color? DisabledActiveTrackColor = null,
        Color? DisabledInactiveTrackColor = null,
        Color? DisabledSecondaryActiveTrackColor = null,
        Color? ActiveTickMarkColor = null,
        Color? InactiveTickMarkColor = null,
        Color? DisabledActiveTickMarkColor = null,
        Color? DisabledInactiveTickMarkColor = null,
        Color? ThumbColor = null,
        Color? OverlappingShapeStrokeColor = null,
        Color? DisabledThumbColor = null,
        Color? OverlayColor = null,
        Color? ValueIndicatorColor = null,
        Color? ValueIndicatorStrokeColor = null,
        SliderComponentShape? OverlayShape = null,
        SliderTickMarkShape? TickMarkShape = null,
        SliderComponentShape? ThumbShape = null,
        SliderTrackShape? TrackShape = null,
        SliderComponentShape? ValueIndicatorShape = null,
        RangeSliderTickMarkShape? RangeTickMarkShape = null,
        RangeSliderThumbShape? RangeThumbShape = null,
        RangeSliderTrackShape? RangeTrackShape = null,
        RangeSliderValueIndicatorShape? RangeValueIndicatorShape = null,
        ShowValueIndicator? ShowValueIndicator = null,
        TextStyle? ValueIndicatorTextStyle = null,
        double? MinThumbSeparation = null,
        RangeThumbSelector? ThumbSelector = null,
        WidgetStateProperty<MouseCursor?>? MouseCursor = null,
        SliderInteraction? AllowedInteraction = null,
        EdgeInsetsGeometry? Padding = null,
        WidgetStateProperty<Size?>? ThumbSize = null,
        double? TrackGap = null,
        bool? Year2023 = null)
    {
        this.TrackHeight = TrackHeight;
        this.ActiveTrackColor = ActiveTrackColor;
        this.InactiveTrackColor = InactiveTrackColor;
        this.SecondaryActiveTrackColor = SecondaryActiveTrackColor;
        this.DisabledActiveTrackColor = DisabledActiveTrackColor;
        this.DisabledInactiveTrackColor = DisabledInactiveTrackColor;
        this.DisabledSecondaryActiveTrackColor = DisabledSecondaryActiveTrackColor;
        this.ActiveTickMarkColor = ActiveTickMarkColor;
        this.InactiveTickMarkColor = InactiveTickMarkColor;
        this.DisabledActiveTickMarkColor = DisabledActiveTickMarkColor;
        this.DisabledInactiveTickMarkColor = DisabledInactiveTickMarkColor;
        this.ThumbColor = ThumbColor;
        this.OverlappingShapeStrokeColor = OverlappingShapeStrokeColor;
        this.DisabledThumbColor = DisabledThumbColor;
        this.OverlayColor = OverlayColor;
        this.ValueIndicatorColor = ValueIndicatorColor;
        this.ValueIndicatorStrokeColor = ValueIndicatorStrokeColor;
        this.OverlayShape = OverlayShape;
        this.TickMarkShape = TickMarkShape;
        this.ThumbShape = ThumbShape;
        this.TrackShape = TrackShape;
        this.ValueIndicatorShape = ValueIndicatorShape;
        this.RangeTickMarkShape = RangeTickMarkShape;
        this.RangeThumbShape = RangeThumbShape;
        this.RangeTrackShape = RangeTrackShape;
        this.RangeValueIndicatorShape = RangeValueIndicatorShape;
        this.ShowValueIndicator = ShowValueIndicator;
        this.ValueIndicatorTextStyle = ValueIndicatorTextStyle;
        this.MinThumbSeparation = MinThumbSeparation;
        this.ThumbSelector = ThumbSelector;
        this.MouseCursor = MouseCursor;
        this.AllowedInteraction = AllowedInteraction;
        this.Padding = Padding;
        this.ThumbSize = ThumbSize;
        this.TrackGap = TrackGap;
        this.Year2023 = Year2023;
    }

    /// Generates a SliderThemeData from three main colors.
    ///
    /// Usually these are the primary, dark and light colors from a [ThemeData]. The opacities of
    /// these colors will be overridden with the Material Design defaults when assigning them to
    /// the slider theme component colors.
    public static SliderThemeData FromPrimaryColors(
        Color primaryColor,
        Color primaryColorDark,
        Color primaryColorLight,
        TextStyle valueIndicatorTextStyle)
    {
        // These are Material Design defaults, and are used to derive
        // component Colors (with opacity) from base colors.
        const int activeTrackAlpha = 0xff;
        const int inactiveTrackAlpha = 0x3d; // 24% opacity
        const int secondaryActiveTrackAlpha = 0x8a; // 54% opacity
        const int disabledActiveTrackAlpha = 0x52; // 32% opacity
        const int disabledInactiveTrackAlpha = 0x1f; // 12% opacity
        const int disabledSecondaryActiveTrackAlpha = 0x1f; // 12% opacity
        const int activeTickMarkAlpha = 0x8a; // 54% opacity
        const int inactiveTickMarkAlpha = 0x8a; // 54% opacity
        const int disabledActiveTickMarkAlpha = 0x1f; // 12% opacity
        const int disabledInactiveTickMarkAlpha = 0x1f; // 12% opacity
        const int thumbAlpha = 0xff;
        const int disabledThumbAlpha = 0x52; // 32% opacity
        const int overlayAlpha = 0x1f; // 12% opacity
        const int valueIndicatorAlpha = 0xff;

        return new SliderThemeData(
            TrackHeight: 2.0,
            ActiveTrackColor: primaryColor.WithAlpha(activeTrackAlpha),
            InactiveTrackColor: primaryColor.WithAlpha(inactiveTrackAlpha),
            SecondaryActiveTrackColor: primaryColor.WithAlpha(secondaryActiveTrackAlpha),
            DisabledActiveTrackColor: primaryColorDark.WithAlpha(disabledActiveTrackAlpha),
            DisabledInactiveTrackColor: primaryColorDark.WithAlpha(disabledInactiveTrackAlpha),
            DisabledSecondaryActiveTrackColor: primaryColorDark.WithAlpha(
                disabledSecondaryActiveTrackAlpha),
            ActiveTickMarkColor: primaryColorLight.WithAlpha(activeTickMarkAlpha),
            InactiveTickMarkColor: primaryColor.WithAlpha(inactiveTickMarkAlpha),
            DisabledActiveTickMarkColor: primaryColorLight.WithAlpha(disabledActiveTickMarkAlpha),
            DisabledInactiveTickMarkColor: primaryColorDark.WithAlpha(disabledInactiveTickMarkAlpha),
            ThumbColor: primaryColor.WithAlpha(thumbAlpha),
            OverlappingShapeStrokeColor: Colors.White,
            DisabledThumbColor: primaryColorDark.WithAlpha(disabledThumbAlpha),
            OverlayColor: primaryColor.WithAlpha(overlayAlpha),
            ValueIndicatorColor: primaryColor.WithAlpha(valueIndicatorAlpha),
            ValueIndicatorStrokeColor: primaryColor.WithAlpha(valueIndicatorAlpha),
            OverlayShape: new RoundSliderOverlayShape(),
            TickMarkShape: new RoundSliderTickMarkShape(),
            ThumbShape: new RoundSliderThumbShape(),
            TrackShape: new RoundedRectSliderTrackShape(),
            ValueIndicatorShape: new PaddleSliderValueIndicatorShape(),
            RangeTickMarkShape: new RoundRangeSliderTickMarkShape(),
            RangeThumbShape: new RoundRangeSliderThumbShape(),
            RangeTrackShape: new RoundedRectRangeSliderTrackShape(),
            RangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape(),
            ValueIndicatorTextStyle: valueIndicatorTextStyle,
            ShowValueIndicator: Plumix.Material.ShowValueIndicator.OnlyForDiscrete);
    }

    /// The height of the [Slider] track.
    public virtual double? TrackHeight { get; init; }

    /// The color of the [Slider] track between the [Slider.Min] position and the current thumb
    /// position.
    public virtual Color? ActiveTrackColor { get; init; }

    /// The color of the [Slider] track between the current thumb position and the [Slider.Max]
    /// position.
    public virtual Color? InactiveTrackColor { get; init; }

    /// The color of the [Slider] track between the current thumb position and the
    /// [Slider.SecondaryTrackValue] position.
    public virtual Color? SecondaryActiveTrackColor { get; init; }

    /// The color of the [Slider] track between the [Slider.Min] position and the current thumb
    /// position when the [Slider] is disabled.
    public virtual Color? DisabledActiveTrackColor { get; init; }

    /// The color of the [Slider] track between the current thumb position and the
    /// [Slider.SecondaryTrackValue] position when the [Slider] is disabled.
    public virtual Color? DisabledSecondaryActiveTrackColor { get; init; }

    /// The color of the [Slider] track between the current thumb position and the [Slider.Max]
    /// position when the [Slider] is disabled.
    public virtual Color? DisabledInactiveTrackColor { get; init; }

    /// The color of the track's tick marks that are drawn between the [Slider.Min] position and
    /// the current thumb position.
    public virtual Color? ActiveTickMarkColor { get; init; }

    /// The color of the track's tick marks that are drawn between the current thumb position and
    /// the [Slider.Max] position.
    public virtual Color? InactiveTickMarkColor { get; init; }

    /// The color of the track's tick marks that are drawn between the track's starting position
    /// and the current thumb position when the [Slider] is disabled.
    public virtual Color? DisabledActiveTickMarkColor { get; init; }

    /// The color of the track's tick marks that are drawn between the current thumb position and
    /// the track's ending position when the [Slider] is disabled.
    public virtual Color? DisabledInactiveTickMarkColor { get; init; }

    /// The color given to the [ThumbShape] to draw itself with.
    public virtual Color? ThumbColor { get; init; }

    /// The color given to the perimeter of the top [RangeThumbShape] when the thumbs are
    /// overlapping and the top [RangeValueIndicatorShape] when the value indicators are
    /// overlapping.
    public virtual Color? OverlappingShapeStrokeColor { get; init; }

    /// The color given to the [ThumbShape] to draw itself with when the [Slider] is disabled.
    public virtual Color? DisabledThumbColor { get; init; }

    /// The color of the overlay drawn around the slider thumb when it is pressed, which may be a
    /// [WidgetStateColor].
    public virtual Color? OverlayColor { get; init; }

    /// The color given to the [ValueIndicatorShape] to draw itself with.
    public virtual Color? ValueIndicatorColor { get; init; }

    /// The color given to the [ValueIndicatorShape] stroke.
    public virtual Color? ValueIndicatorStrokeColor { get; init; }

    /// The shape that will be used to draw the [Slider]'s overlay.
    public virtual SliderComponentShape? OverlayShape { get; init; }

    /// The shape that will be used to draw the [Slider]'s tick marks.
    public virtual SliderTickMarkShape? TickMarkShape { get; init; }

    /// The shape that will be used to draw the [Slider]'s thumb.
    public virtual SliderComponentShape? ThumbShape { get; init; }

    /// The shape that will be used to draw the [Slider]'s track.
    public virtual SliderTrackShape? TrackShape { get; init; }

    /// The shape that will be used to draw the [Slider]'s value indicator.
    public virtual SliderComponentShape? ValueIndicatorShape { get; init; }

    /// The shape that will be used to draw the [RangeSlider]'s tick marks.
    public virtual RangeSliderTickMarkShape? RangeTickMarkShape { get; init; }

    /// The shape that will be used for the [RangeSlider]'s thumbs.
    public virtual RangeSliderThumbShape? RangeThumbShape { get; init; }

    /// The shape that will be used to draw the [RangeSlider]'s track.
    public virtual RangeSliderTrackShape? RangeTrackShape { get; init; }

    /// The shape that will be used for the [RangeSlider]'s value indicators.
    public virtual RangeSliderValueIndicatorShape? RangeValueIndicatorShape { get; init; }

    /// Whether the value indicator should be shown for different types of sliders.
    public virtual ShowValueIndicator? ShowValueIndicator { get; init; }

    /// The text style for the text on the value indicator.
    public virtual TextStyle? ValueIndicatorTextStyle { get; init; }

    /// Limits the thumb's separation distance.
    public virtual double? MinThumbSeparation { get; init; }

    /// Determines which thumb should be selected when the slider is interacted with.
    public virtual RangeThumbSelector? ThumbSelector { get; init; }

    /// The cursor for a mouse pointer when it enters or is hovering over the widget.
    public virtual WidgetStateProperty<MouseCursor?>? MouseCursor { get; init; }

    /// Allowed way for the user to interact with the [Slider].
    public virtual SliderInteraction? AllowedInteraction { get; init; }

    /// Determines the padding around the [Slider].
    public virtual EdgeInsetsGeometry? Padding { get; init; }

    /// The size of the [HandleThumbShape] thumb.
    public virtual WidgetStateProperty<Size?>? ThumbSize { get; init; }

    /// The size of the gap between the active and inactive tracks of the [GappedSliderTrackShape].
    public virtual double? TrackGap { get; init; }

    /// Overrides the default value of [Slider.Year2023].
    ///
    /// Deprecated in Dart: set this flag to false to opt into the 2024 slider appearance.
    public virtual bool? Year2023 { get; init; }

    /// Creates a copy of this object but with the given fields replaced with the new values.
    public SliderThemeData CopyWith(
        double? trackHeight = null,
        Color? activeTrackColor = null,
        Color? inactiveTrackColor = null,
        Color? secondaryActiveTrackColor = null,
        Color? disabledActiveTrackColor = null,
        Color? disabledInactiveTrackColor = null,
        Color? disabledSecondaryActiveTrackColor = null,
        Color? activeTickMarkColor = null,
        Color? inactiveTickMarkColor = null,
        Color? disabledActiveTickMarkColor = null,
        Color? disabledInactiveTickMarkColor = null,
        Color? thumbColor = null,
        Color? overlappingShapeStrokeColor = null,
        Color? disabledThumbColor = null,
        Color? overlayColor = null,
        Color? valueIndicatorColor = null,
        Color? valueIndicatorStrokeColor = null,
        SliderComponentShape? overlayShape = null,
        SliderTickMarkShape? tickMarkShape = null,
        SliderComponentShape? thumbShape = null,
        SliderTrackShape? trackShape = null,
        SliderComponentShape? valueIndicatorShape = null,
        RangeSliderTickMarkShape? rangeTickMarkShape = null,
        RangeSliderThumbShape? rangeThumbShape = null,
        RangeSliderTrackShape? rangeTrackShape = null,
        RangeSliderValueIndicatorShape? rangeValueIndicatorShape = null,
        ShowValueIndicator? showValueIndicator = null,
        TextStyle? valueIndicatorTextStyle = null,
        double? minThumbSeparation = null,
        RangeThumbSelector? thumbSelector = null,
        WidgetStateProperty<MouseCursor?>? mouseCursor = null,
        SliderInteraction? allowedInteraction = null,
        EdgeInsetsGeometry? padding = null,
        WidgetStateProperty<Size?>? thumbSize = null,
        double? trackGap = null,
        bool? year2023 = null)
    {
        return new SliderThemeData(
            TrackHeight: trackHeight ?? TrackHeight,
            ActiveTrackColor: activeTrackColor ?? ActiveTrackColor,
            InactiveTrackColor: inactiveTrackColor ?? InactiveTrackColor,
            SecondaryActiveTrackColor: secondaryActiveTrackColor ?? SecondaryActiveTrackColor,
            DisabledActiveTrackColor: disabledActiveTrackColor ?? DisabledActiveTrackColor,
            DisabledInactiveTrackColor: disabledInactiveTrackColor ?? DisabledInactiveTrackColor,
            DisabledSecondaryActiveTrackColor:
                disabledSecondaryActiveTrackColor ?? DisabledSecondaryActiveTrackColor,
            ActiveTickMarkColor: activeTickMarkColor ?? ActiveTickMarkColor,
            InactiveTickMarkColor: inactiveTickMarkColor ?? InactiveTickMarkColor,
            DisabledActiveTickMarkColor: disabledActiveTickMarkColor ?? DisabledActiveTickMarkColor,
            DisabledInactiveTickMarkColor:
                disabledInactiveTickMarkColor ?? DisabledInactiveTickMarkColor,
            ThumbColor: thumbColor ?? ThumbColor,
            OverlappingShapeStrokeColor: overlappingShapeStrokeColor ?? OverlappingShapeStrokeColor,
            DisabledThumbColor: disabledThumbColor ?? DisabledThumbColor,
            OverlayColor: overlayColor ?? OverlayColor,
            ValueIndicatorColor: valueIndicatorColor ?? ValueIndicatorColor,
            ValueIndicatorStrokeColor: valueIndicatorStrokeColor ?? ValueIndicatorStrokeColor,
            OverlayShape: overlayShape ?? OverlayShape,
            TickMarkShape: tickMarkShape ?? TickMarkShape,
            ThumbShape: thumbShape ?? ThumbShape,
            TrackShape: trackShape ?? TrackShape,
            ValueIndicatorShape: valueIndicatorShape ?? ValueIndicatorShape,
            RangeTickMarkShape: rangeTickMarkShape ?? RangeTickMarkShape,
            RangeThumbShape: rangeThumbShape ?? RangeThumbShape,
            RangeTrackShape: rangeTrackShape ?? RangeTrackShape,
            RangeValueIndicatorShape: rangeValueIndicatorShape ?? RangeValueIndicatorShape,
            ShowValueIndicator: showValueIndicator ?? ShowValueIndicator,
            ValueIndicatorTextStyle: valueIndicatorTextStyle ?? ValueIndicatorTextStyle,
            MinThumbSeparation: minThumbSeparation ?? MinThumbSeparation,
            ThumbSelector: thumbSelector ?? ThumbSelector,
            MouseCursor: mouseCursor ?? MouseCursor,
            AllowedInteraction: allowedInteraction ?? AllowedInteraction,
            Padding: padding ?? Padding,
            ThumbSize: thumbSize ?? ThumbSize,
            TrackGap: trackGap ?? TrackGap,
            Year2023: year2023 ?? Year2023);
    }

    /// Linearly interpolate between two slider themes.
    public static SliderThemeData Lerp(SliderThemeData a, SliderThemeData b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new SliderThemeData(
            TrackHeight: MaterialThemeLerp.Double(a.TrackHeight, b.TrackHeight, t),
            ActiveTrackColor: Color.Lerp(a.ActiveTrackColor, b.ActiveTrackColor, t),
            InactiveTrackColor: Color.Lerp(a.InactiveTrackColor, b.InactiveTrackColor, t),
            SecondaryActiveTrackColor: Color.Lerp(
                a.SecondaryActiveTrackColor,
                b.SecondaryActiveTrackColor,
                t),
            DisabledActiveTrackColor: Color.Lerp(
                a.DisabledActiveTrackColor,
                b.DisabledActiveTrackColor,
                t),
            DisabledInactiveTrackColor: Color.Lerp(
                a.DisabledInactiveTrackColor,
                b.DisabledInactiveTrackColor,
                t),
            DisabledSecondaryActiveTrackColor: Color.Lerp(
                a.DisabledSecondaryActiveTrackColor,
                b.DisabledSecondaryActiveTrackColor,
                t),
            ActiveTickMarkColor: Color.Lerp(a.ActiveTickMarkColor, b.ActiveTickMarkColor, t),
            InactiveTickMarkColor: Color.Lerp(a.InactiveTickMarkColor, b.InactiveTickMarkColor, t),
            DisabledActiveTickMarkColor: Color.Lerp(
                a.DisabledActiveTickMarkColor,
                b.DisabledActiveTickMarkColor,
                t),
            DisabledInactiveTickMarkColor: Color.Lerp(
                a.DisabledInactiveTickMarkColor,
                b.DisabledInactiveTickMarkColor,
                t),
            ThumbColor: Color.Lerp(a.ThumbColor, b.ThumbColor, t),
            OverlappingShapeStrokeColor: Color.Lerp(
                a.OverlappingShapeStrokeColor,
                b.OverlappingShapeStrokeColor,
                t),
            DisabledThumbColor: Color.Lerp(a.DisabledThumbColor, b.DisabledThumbColor, t),
            OverlayColor: Color.Lerp(a.OverlayColor, b.OverlayColor, t),
            ValueIndicatorColor: Color.Lerp(a.ValueIndicatorColor, b.ValueIndicatorColor, t),
            ValueIndicatorStrokeColor: Color.Lerp(
                a.ValueIndicatorStrokeColor,
                b.ValueIndicatorStrokeColor,
                t),
            OverlayShape: t < 0.5 ? a.OverlayShape : b.OverlayShape,
            TickMarkShape: t < 0.5 ? a.TickMarkShape : b.TickMarkShape,
            ThumbShape: t < 0.5 ? a.ThumbShape : b.ThumbShape,
            TrackShape: t < 0.5 ? a.TrackShape : b.TrackShape,
            ValueIndicatorShape: t < 0.5 ? a.ValueIndicatorShape : b.ValueIndicatorShape,
            RangeTickMarkShape: t < 0.5 ? a.RangeTickMarkShape : b.RangeTickMarkShape,
            RangeThumbShape: t < 0.5 ? a.RangeThumbShape : b.RangeThumbShape,
            RangeTrackShape: t < 0.5 ? a.RangeTrackShape : b.RangeTrackShape,
            RangeValueIndicatorShape: t < 0.5 ? a.RangeValueIndicatorShape : b.RangeValueIndicatorShape,
            ShowValueIndicator: t < 0.5 ? a.ShowValueIndicator : b.ShowValueIndicator,
            ValueIndicatorTextStyle: TextStyle.Lerp(
                a.ValueIndicatorTextStyle,
                b.ValueIndicatorTextStyle,
                t),
            MinThumbSeparation: MaterialThemeLerp.Double(a.MinThumbSeparation, b.MinThumbSeparation, t),
            ThumbSelector: t < 0.5 ? a.ThumbSelector : b.ThumbSelector,
            MouseCursor: t < 0.5 ? a.MouseCursor : b.MouseCursor,
            AllowedInteraction: t < 0.5 ? a.AllowedInteraction : b.AllowedInteraction,
            Padding: EdgeInsetsGeometry.Lerp(a.Padding, b.Padding, t),
            ThumbSize: WidgetStateProperty<Size?>.Lerp(a.ThumbSize, b.ThumbSize, t, MaterialThemeLerp.Size),
            TrackGap: MaterialThemeLerp.Double(a.TrackGap, b.TrackGap, t),
            Year2023: t < 0.5 ? a.Year2023 : b.Year2023);
    }

    public override int GetHashCode()
    {
        var tail = new HashCode();
        tail.Add(TrackShape);
        tail.Add(ValueIndicatorShape);
        tail.Add(RangeTickMarkShape);
        tail.Add(RangeThumbShape);
        tail.Add(RangeTrackShape);
        tail.Add(RangeValueIndicatorShape);
        tail.Add(ShowValueIndicator);
        tail.Add(ValueIndicatorTextStyle);
        tail.Add(MinThumbSeparation);
        tail.Add(ThumbSelector);
        tail.Add(MouseCursor);
        tail.Add(AllowedInteraction);
        tail.Add(Padding);
        tail.Add(ThumbSize);
        tail.Add(TrackGap);
        tail.Add(Year2023);

        var hash = new HashCode();
        hash.Add(TrackHeight);
        hash.Add(ActiveTrackColor);
        hash.Add(InactiveTrackColor);
        hash.Add(SecondaryActiveTrackColor);
        hash.Add(DisabledActiveTrackColor);
        hash.Add(DisabledInactiveTrackColor);
        hash.Add(DisabledSecondaryActiveTrackColor);
        hash.Add(ActiveTickMarkColor);
        hash.Add(InactiveTickMarkColor);
        hash.Add(DisabledActiveTickMarkColor);
        hash.Add(DisabledInactiveTickMarkColor);
        hash.Add(ThumbColor);
        hash.Add(OverlappingShapeStrokeColor);
        hash.Add(DisabledThumbColor);
        hash.Add(OverlayColor);
        hash.Add(ValueIndicatorColor);
        hash.Add(OverlayShape);
        hash.Add(TickMarkShape);
        hash.Add(ThumbShape);
        hash.Add(tail.ToHashCode());
        return hash.ToHashCode();
    }

    public virtual bool Equals(SliderThemeData? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return other.TrackHeight == TrackHeight
               && Equals(other.ActiveTrackColor, ActiveTrackColor)
               && Equals(other.InactiveTrackColor, InactiveTrackColor)
               && Equals(other.SecondaryActiveTrackColor, SecondaryActiveTrackColor)
               && Equals(other.DisabledActiveTrackColor, DisabledActiveTrackColor)
               && Equals(other.DisabledInactiveTrackColor, DisabledInactiveTrackColor)
               && Equals(other.DisabledSecondaryActiveTrackColor, DisabledSecondaryActiveTrackColor)
               && Equals(other.ActiveTickMarkColor, ActiveTickMarkColor)
               && Equals(other.InactiveTickMarkColor, InactiveTickMarkColor)
               && Equals(other.DisabledActiveTickMarkColor, DisabledActiveTickMarkColor)
               && Equals(other.DisabledInactiveTickMarkColor, DisabledInactiveTickMarkColor)
               && Equals(other.ThumbColor, ThumbColor)
               && Equals(other.OverlappingShapeStrokeColor, OverlappingShapeStrokeColor)
               && Equals(other.DisabledThumbColor, DisabledThumbColor)
               && Equals(other.OverlayColor, OverlayColor)
               && Equals(other.ValueIndicatorColor, ValueIndicatorColor)
               && Equals(other.ValueIndicatorStrokeColor, ValueIndicatorStrokeColor)
               && Equals(other.OverlayShape, OverlayShape)
               && Equals(other.TickMarkShape, TickMarkShape)
               && Equals(other.ThumbShape, ThumbShape)
               && Equals(other.TrackShape, TrackShape)
               && Equals(other.ValueIndicatorShape, ValueIndicatorShape)
               && Equals(other.RangeTickMarkShape, RangeTickMarkShape)
               && Equals(other.RangeThumbShape, RangeThumbShape)
               && Equals(other.RangeTrackShape, RangeTrackShape)
               && Equals(other.RangeValueIndicatorShape, RangeValueIndicatorShape)
               && other.ShowValueIndicator == ShowValueIndicator
               && Equals(other.ValueIndicatorTextStyle, ValueIndicatorTextStyle)
               && other.MinThumbSeparation == MinThumbSeparation
               && Equals(other.ThumbSelector, ThumbSelector)
               && Equals(other.MouseCursor, MouseCursor)
               && other.AllowedInteraction == AllowedInteraction
               && Equals(other.Padding, Padding)
               && Equals(other.ThumbSize, ThumbSize)
               && other.TrackGap == TrackGap
               && other.Year2023 == Year2023;
    }

    /// Dart's `Diagnosticable.toString`.
    public override string ToString()
    {
        IDiagnosticable self = this;
        return Constants.KDebugMode
            ? self.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, DiagnosticLevel.Info)
            : self.ToStringShort();
    }

    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var defaultData = new SliderThemeData();
        properties.Add(new DoubleProperty(
            "trackHeight",
            TrackHeight,
            defaultValue: DefaultOf(defaultData.TrackHeight)));
        properties.Add(new ColorProperty(
            "activeTrackColor",
            ActiveTrackColor,
            defaultValue: DefaultOf(defaultData.ActiveTrackColor)));
        properties.Add(new ColorProperty(
            "inactiveTrackColor",
            InactiveTrackColor,
            defaultValue: DefaultOf(defaultData.InactiveTrackColor)));
        properties.Add(new ColorProperty(
            "secondaryActiveTrackColor",
            SecondaryActiveTrackColor,
            defaultValue: DefaultOf(defaultData.SecondaryActiveTrackColor)));
        properties.Add(new ColorProperty(
            "disabledActiveTrackColor",
            DisabledActiveTrackColor,
            defaultValue: DefaultOf(defaultData.DisabledActiveTrackColor)));
        properties.Add(new ColorProperty(
            "disabledInactiveTrackColor",
            DisabledInactiveTrackColor,
            defaultValue: DefaultOf(defaultData.DisabledInactiveTrackColor)));
        properties.Add(new ColorProperty(
            "disabledSecondaryActiveTrackColor",
            DisabledSecondaryActiveTrackColor,
            defaultValue: DefaultOf(defaultData.DisabledSecondaryActiveTrackColor)));
        properties.Add(new ColorProperty(
            "activeTickMarkColor",
            ActiveTickMarkColor,
            defaultValue: DefaultOf(defaultData.ActiveTickMarkColor)));
        properties.Add(new ColorProperty(
            "inactiveTickMarkColor",
            InactiveTickMarkColor,
            defaultValue: DefaultOf(defaultData.InactiveTickMarkColor)));
        properties.Add(new ColorProperty(
            "disabledActiveTickMarkColor",
            DisabledActiveTickMarkColor,
            defaultValue: DefaultOf(defaultData.DisabledActiveTickMarkColor)));
        properties.Add(new ColorProperty(
            "disabledInactiveTickMarkColor",
            DisabledInactiveTickMarkColor,
            defaultValue: DefaultOf(defaultData.DisabledInactiveTickMarkColor)));
        properties.Add(new ColorProperty(
            "thumbColor",
            ThumbColor,
            defaultValue: DefaultOf(defaultData.ThumbColor)));
        properties.Add(new ColorProperty(
            "overlappingShapeStrokeColor",
            OverlappingShapeStrokeColor,
            defaultValue: DefaultOf(defaultData.OverlappingShapeStrokeColor)));
        properties.Add(new ColorProperty(
            "disabledThumbColor",
            DisabledThumbColor,
            defaultValue: DefaultOf(defaultData.DisabledThumbColor)));
        properties.Add(new ColorProperty(
            "overlayColor",
            OverlayColor,
            defaultValue: DefaultOf(defaultData.OverlayColor)));
        properties.Add(new ColorProperty(
            "valueIndicatorColor",
            ValueIndicatorColor,
            defaultValue: DefaultOf(defaultData.ValueIndicatorColor)));
        properties.Add(new ColorProperty(
            "valueIndicatorStrokeColor",
            ValueIndicatorStrokeColor,
            defaultValue: DefaultOf(defaultData.ValueIndicatorStrokeColor)));
        properties.Add(new DiagnosticsProperty<SliderComponentShape>(
            "overlayShape",
            OverlayShape,
            defaultValue: DefaultOf(defaultData.OverlayShape)));
        properties.Add(new DiagnosticsProperty<SliderTickMarkShape>(
            "tickMarkShape",
            TickMarkShape,
            defaultValue: DefaultOf(defaultData.TickMarkShape)));
        properties.Add(new DiagnosticsProperty<SliderComponentShape>(
            "thumbShape",
            ThumbShape,
            defaultValue: DefaultOf(defaultData.ThumbShape)));
        properties.Add(new DiagnosticsProperty<SliderTrackShape>(
            "trackShape",
            TrackShape,
            defaultValue: DefaultOf(defaultData.TrackShape)));
        properties.Add(new DiagnosticsProperty<SliderComponentShape>(
            "valueIndicatorShape",
            ValueIndicatorShape,
            defaultValue: DefaultOf(defaultData.ValueIndicatorShape)));
        properties.Add(new DiagnosticsProperty<RangeSliderTickMarkShape>(
            "rangeTickMarkShape",
            RangeTickMarkShape,
            defaultValue: DefaultOf(defaultData.RangeTickMarkShape)));
        properties.Add(new DiagnosticsProperty<RangeSliderThumbShape>(
            "rangeThumbShape",
            RangeThumbShape,
            defaultValue: DefaultOf(defaultData.RangeThumbShape)));
        properties.Add(new DiagnosticsProperty<RangeSliderTrackShape>(
            "rangeTrackShape",
            RangeTrackShape,
            defaultValue: DefaultOf(defaultData.RangeTrackShape)));
        properties.Add(new DiagnosticsProperty<RangeSliderValueIndicatorShape>(
            "rangeValueIndicatorShape",
            RangeValueIndicatorShape,
            defaultValue: DefaultOf(defaultData.RangeValueIndicatorShape)));
        properties.Add(new EnumProperty<ShowValueIndicator>(
            "showValueIndicator",
            ShowValueIndicator,
            defaultValue: DefaultOf(defaultData.ShowValueIndicator)));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "valueIndicatorTextStyle",
            ValueIndicatorTextStyle,
            defaultValue: DefaultOf(defaultData.ValueIndicatorTextStyle)));
        properties.Add(new DoubleProperty(
            "minThumbSeparation",
            MinThumbSeparation,
            defaultValue: DefaultOf(defaultData.MinThumbSeparation)));
        properties.Add(new DiagnosticsProperty<RangeThumbSelector>(
            "thumbSelector",
            ThumbSelector,
            defaultValue: DefaultOf(defaultData.ThumbSelector)));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<MouseCursor?>>(
            "mouseCursor",
            MouseCursor,
            defaultValue: DefaultOf(defaultData.MouseCursor)));
        properties.Add(new EnumProperty<SliderInteraction>(
            "allowedInteraction",
            AllowedInteraction,
            defaultValue: DefaultOf(defaultData.AllowedInteraction)));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "padding",
            Padding,
            defaultValue: DefaultOf(defaultData.Padding)));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Size?>>(
            "thumbSize",
            ThumbSize,
            defaultValue: DefaultOf(defaultData.ThumbSize)));
        properties.Add(new DoubleProperty(
            "trackGap",
            TrackGap,
            defaultValue: DefaultOf(defaultData.TrackGap)));
        properties.Add(new DiagnosticsProperty<bool?>(
            "year2023",
            Year2023,
            defaultValue: DefaultOf(defaultData.Year2023)));
    }

    // Dart passes `defaultData.x` straight through, where a null default means "null is the
    // boring value"; C# spells that as DiagnosticsDefaults.NullValue.
    private static object DefaultOf(object? value) => value ?? DiagnosticsDefaults.NullValue;
}

/// Signature for a callback that formats a slider value for semantics.
public delegate string SemanticFormatterCallback(double value);
