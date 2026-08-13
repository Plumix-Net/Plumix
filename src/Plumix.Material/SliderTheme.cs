using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider_theme.dart

public enum ShowValueIndicator
{
    OnlyForDiscrete,
    OnlyForContinuous,
    Always,
    OnDrag,
    AlwaysVisible,
    Never,
}

public enum SliderInteraction
{
    TapAndSlide,
    TapOnly,
    SlideOnly,
    SlideThumb,
}

public enum Thumb
{
    Start,
    End,
}

public readonly record struct RangeLabels(string Start, string End);

public delegate Thumb? RangeThumbSelector(
    TextDirection textDirection,
    RangeValues values,
    double tapValue,
    Size thumbSize,
    Size trackSize,
    double dx);

public sealed partial record SliderThemeData(
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
    WidgetStateProperty<Color?>? OverlayColor = null,
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
    bool? Year2023 = null,
    double? ThumbRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    double? OverlayRadius = null,
    double? TickMarkRadius = null)
{
    public static SliderThemeData FromPrimaryColors(
        Color primaryColor,
        Color primaryColorDark,
        Color primaryColorLight,
        TextStyle valueIndicatorTextStyle)
    {
        return new SliderThemeData(
            TrackHeight: 2.0,
            ActiveTrackColor: WithAlpha(primaryColor, 0xff),
            InactiveTrackColor: WithAlpha(primaryColor, 0x3d),
            SecondaryActiveTrackColor: WithAlpha(primaryColor, 0x8a),
            DisabledActiveTrackColor: WithAlpha(primaryColorDark, 0x52),
            DisabledInactiveTrackColor: WithAlpha(primaryColorDark, 0x1f),
            DisabledSecondaryActiveTrackColor: WithAlpha(primaryColorDark, 0x1f),
            ActiveTickMarkColor: WithAlpha(primaryColorLight, 0x8a),
            InactiveTickMarkColor: WithAlpha(primaryColor, 0x8a),
            DisabledActiveTickMarkColor: WithAlpha(primaryColorLight, 0x1f),
            DisabledInactiveTickMarkColor: WithAlpha(primaryColorDark, 0x1f),
            ThumbColor: WithAlpha(primaryColor, 0xff),
            OverlappingShapeStrokeColor: Colors.White,
            DisabledThumbColor: WithAlpha(primaryColorDark, 0x52),
            OverlayColor: WidgetStateProperty<Color?>.All(WithAlpha(primaryColor, 0x1f)),
            ValueIndicatorColor: WithAlpha(primaryColor, 0xff),
            ValueIndicatorStrokeColor: WithAlpha(primaryColor, 0xff),
            OverlayShape: new RoundSliderOverlayShape(),
            TickMarkShape: new RoundSliderTickMarkShape(),
            ThumbShape: new RoundSliderThumbShape(),
            TrackShape: new RoundedRectSliderTrackShape(),
            ValueIndicatorShape: new PaddleSliderValueIndicatorShape(),
            RangeTickMarkShape: new RoundRangeSliderTickMarkShape(),
            RangeThumbShape: new RoundRangeSliderThumbShape(),
            RangeTrackShape: new RoundedRectRangeSliderTrackShape(),
            RangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape(),
            ShowValueIndicator: Plumix.Material.ShowValueIndicator.OnlyForDiscrete,
            ValueIndicatorTextStyle: valueIndicatorTextStyle);
    }

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
        WidgetStateProperty<Color?>? overlayColor = null,
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
        return this with
        {
            TrackHeight = trackHeight ?? TrackHeight,
            ActiveTrackColor = activeTrackColor ?? ActiveTrackColor,
            InactiveTrackColor = inactiveTrackColor ?? InactiveTrackColor,
            SecondaryActiveTrackColor = secondaryActiveTrackColor ?? SecondaryActiveTrackColor,
            DisabledActiveTrackColor = disabledActiveTrackColor ?? DisabledActiveTrackColor,
            DisabledInactiveTrackColor = disabledInactiveTrackColor ?? DisabledInactiveTrackColor,
            DisabledSecondaryActiveTrackColor = disabledSecondaryActiveTrackColor ?? DisabledSecondaryActiveTrackColor,
            ActiveTickMarkColor = activeTickMarkColor ?? ActiveTickMarkColor,
            InactiveTickMarkColor = inactiveTickMarkColor ?? InactiveTickMarkColor,
            DisabledActiveTickMarkColor = disabledActiveTickMarkColor ?? DisabledActiveTickMarkColor,
            DisabledInactiveTickMarkColor = disabledInactiveTickMarkColor ?? DisabledInactiveTickMarkColor,
            ThumbColor = thumbColor ?? ThumbColor,
            OverlappingShapeStrokeColor = overlappingShapeStrokeColor ?? OverlappingShapeStrokeColor,
            DisabledThumbColor = disabledThumbColor ?? DisabledThumbColor,
            OverlayColor = overlayColor ?? OverlayColor,
            ValueIndicatorColor = valueIndicatorColor ?? ValueIndicatorColor,
            ValueIndicatorStrokeColor = valueIndicatorStrokeColor ?? ValueIndicatorStrokeColor,
            OverlayShape = overlayShape ?? OverlayShape,
            TickMarkShape = tickMarkShape ?? TickMarkShape,
            ThumbShape = thumbShape ?? ThumbShape,
            TrackShape = trackShape ?? TrackShape,
            ValueIndicatorShape = valueIndicatorShape ?? ValueIndicatorShape,
            RangeTickMarkShape = rangeTickMarkShape ?? RangeTickMarkShape,
            RangeThumbShape = rangeThumbShape ?? RangeThumbShape,
            RangeTrackShape = rangeTrackShape ?? RangeTrackShape,
            RangeValueIndicatorShape = rangeValueIndicatorShape ?? RangeValueIndicatorShape,
            ShowValueIndicator = showValueIndicator ?? ShowValueIndicator,
            ValueIndicatorTextStyle = valueIndicatorTextStyle ?? ValueIndicatorTextStyle,
            MinThumbSeparation = minThumbSeparation ?? MinThumbSeparation,
            ThumbSelector = thumbSelector ?? ThumbSelector,
            MouseCursor = mouseCursor ?? MouseCursor,
            AllowedInteraction = allowedInteraction ?? AllowedInteraction,
            Padding = padding ?? Padding,
            ThumbSize = thumbSize ?? ThumbSize,
            TrackGap = trackGap ?? TrackGap,
            Year2023 = year2023 ?? Year2023,
        };
    }

    private static Color WithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class SliderTheme : InheritedTheme
{
    public SliderTheme(
        SliderThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SliderThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new SliderTheme(Data, child);
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SliderTheme)oldWidget).Data, Data);
    }

    public static SliderThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<SliderTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SliderTheme;
    }
}
