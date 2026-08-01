using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/slider_theme.dart

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

public sealed partial record SliderThemeData(
    Color? ActiveTrackColor = null,
    Color? InactiveTrackColor = null,
    Color? SecondaryActiveTrackColor = null,
    Color? DisabledActiveTrackColor = null,
    Color? DisabledInactiveTrackColor = null,
    Color? DisabledSecondaryActiveTrackColor = null,
    Color? ThumbColor = null,
    Color? DisabledThumbColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    double? TrackHeight = null,
    double? ThumbRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    Color? ActiveTickMarkColor = null,
    Color? InactiveTickMarkColor = null,
    Color? DisabledActiveTickMarkColor = null,
    Color? DisabledInactiveTickMarkColor = null,
    Color? OverlappingShapeStrokeColor = null,
    Color? ValueIndicatorColor = null,
    Color? ValueIndicatorStrokeColor = null,
    double? OverlayRadius = null,
    double? TickMarkRadius = null,
    ShowValueIndicator? ShowValueIndicator = null,
    TextStyle? ValueIndicatorTextStyle = null,
    double? MinThumbSeparation = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    SliderInteraction? AllowedInteraction = null,
    Thickness? Padding = null,
    MaterialStateProperty<Size?>? ThumbSize = null,
    double? TrackGap = null,
    bool? Year2023 = null);

public sealed class SliderTheme : InheritedWidget
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
