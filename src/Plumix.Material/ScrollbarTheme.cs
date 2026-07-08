using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/scrollbar_theme.dart

public sealed record ScrollbarThemeData
{
    public ScrollbarThemeData(
        MaterialStateProperty<bool?>? thumbVisibility = null,
        MaterialStateProperty<double?>? thickness = null,
        MaterialStateProperty<bool?>? trackVisibility = null,
        double? radius = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackBorderColor = null,
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

    public MaterialStateProperty<bool?>? ThumbVisibility { get; init; }
    public MaterialStateProperty<double?>? Thickness { get; init; }
    public MaterialStateProperty<bool?>? TrackVisibility { get; init; }
    public bool? Interactive { get; init; }
    public double? Radius { get; init; }
    public MaterialStateProperty<Color?>? ThumbColor { get; init; }
    public MaterialStateProperty<Color?>? TrackColor { get; init; }
    public MaterialStateProperty<Color?>? TrackBorderColor { get; init; }
    public double? CrossAxisMargin { get; init; }
    public double? MainAxisMargin { get; init; }
    public double? MinThumbLength { get; init; }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class ScrollbarTheme : InheritedWidget
{
    public ScrollbarTheme(ScrollbarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScrollbarThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ScrollbarTheme)oldWidget).Data, Data);

    public static ScrollbarThemeData Of(BuildContext context) =>
        context.DependOnInherited<ScrollbarTheme>()?.Data ?? Theme.Of(context).ScrollbarTheme;
}
