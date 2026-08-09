using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/scrollbar_theme.dart

public sealed partial record ScrollbarThemeData
{
    public ScrollbarThemeData(
        WidgetStateProperty<bool?>? thumbVisibility = null,
        WidgetStateProperty<double?>? thickness = null,
        WidgetStateProperty<bool?>? trackVisibility = null,
        double? radius = null,
        WidgetStateProperty<Color?>? thumbColor = null,
        WidgetStateProperty<Color?>? trackColor = null,
        WidgetStateProperty<Color?>? trackBorderColor = null,
        double? crossAxisMargin = null,
        double? mainAxisMargin = null,
        double? minThumbLength = null,
        bool? interactive = null)
    {
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(crossAxisMargin), crossAxisMargin);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        ValidateNonNegative(nameof(minThumbLength), minThumbLength);
        ThumbVisibility = thumbVisibility;
        Thickness = thickness;
        TrackVisibility = trackVisibility;
        Radius = radius;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackBorderColor = trackBorderColor;
        CrossAxisMargin = crossAxisMargin;
        MainAxisMargin = mainAxisMargin;
        MinThumbLength = minThumbLength;
        Interactive = interactive;
    }

    public WidgetStateProperty<bool?>? ThumbVisibility { get; init; }
    public WidgetStateProperty<double?>? Thickness { get; init; }
    public WidgetStateProperty<bool?>? TrackVisibility { get; init; }
    public bool? Interactive { get; init; }
    public double? Radius { get; init; }
    public WidgetStateProperty<Color?>? ThumbColor { get; init; }
    public WidgetStateProperty<Color?>? TrackColor { get; init; }
    public WidgetStateProperty<Color?>? TrackBorderColor { get; init; }
    public double? CrossAxisMargin { get; init; }
    public double? MainAxisMargin { get; init; }
    public double? MinThumbLength { get; init; }

    public ScrollbarThemeData CopyWith(
        WidgetStateProperty<bool?>? thumbVisibility = null,
        WidgetStateProperty<double?>? thickness = null,
        WidgetStateProperty<bool?>? trackVisibility = null,
        bool? interactive = null,
        double? radius = null,
        WidgetStateProperty<Color?>? thumbColor = null,
        WidgetStateProperty<Color?>? trackColor = null,
        WidgetStateProperty<Color?>? trackBorderColor = null,
        double? crossAxisMargin = null,
        double? mainAxisMargin = null,
        double? minThumbLength = null)
    {
        return new ScrollbarThemeData(
            thumbVisibility: thumbVisibility ?? ThumbVisibility,
            thickness: thickness ?? Thickness,
            trackVisibility: trackVisibility ?? TrackVisibility,
            interactive: interactive ?? Interactive,
            radius: radius ?? Radius,
            thumbColor: thumbColor ?? ThumbColor,
            trackColor: trackColor ?? TrackColor,
            trackBorderColor: trackBorderColor ?? TrackBorderColor,
            crossAxisMargin: crossAxisMargin ?? CrossAxisMargin,
            mainAxisMargin: mainAxisMargin ?? MainAxisMargin,
            minThumbLength: minThumbLength ?? MinThumbLength);
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class ScrollbarTheme : InheritedTheme
{
    public ScrollbarTheme(ScrollbarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScrollbarThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new ScrollbarTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ScrollbarTheme)oldWidget).Data, Data);

    public static ScrollbarThemeData Of(BuildContext context) =>
        context.DependOnInherited<ScrollbarTheme>()?.Data ?? Theme.Of(context).ScrollbarTheme;
}
