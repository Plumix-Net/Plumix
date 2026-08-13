using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/progress_indicator_theme.dart

public enum StrokeCap
{
    Butt,
    Round,
    Square,
}

public sealed partial record ProgressIndicatorThemeData(
    Color? Color = null,
    Color? LinearTrackColor = null,
    double? LinearMinHeight = null,
    Color? CircularTrackColor = null,
    Color? RefreshBackgroundColor = null,
    BorderRadiusGeometry? BorderRadius = null,
    Color? StopIndicatorColor = null,
    double? StopIndicatorRadius = null,
    double? StrokeWidth = null,
    double? StrokeAlign = null,
    StrokeCap? StrokeCap = null,
    BoxConstraints? Constraints = null,
    double? TrackGap = null,
    EdgeInsetsGeometry? CircularTrackPadding = null,
    bool? Year2023 = null,
    AnimationController? Controller = null)
{
    public ProgressIndicatorThemeData CopyWith(
        Color? color = null,
        Color? linearTrackColor = null,
        double? linearMinHeight = null,
        Color? circularTrackColor = null,
        Color? refreshBackgroundColor = null,
        BorderRadiusGeometry? borderRadius = null,
        Color? stopIndicatorColor = null,
        double? stopIndicatorRadius = null,
        double? strokeWidth = null,
        double? strokeAlign = null,
        StrokeCap? strokeCap = null,
        BoxConstraints? constraints = null,
        double? trackGap = null,
        EdgeInsetsGeometry? circularTrackPadding = null,
        bool? year2023 = null,
        AnimationController? controller = null)
    {
        return new ProgressIndicatorThemeData(
            Color: color ?? Color,
            LinearTrackColor: linearTrackColor ?? LinearTrackColor,
            LinearMinHeight: linearMinHeight ?? LinearMinHeight,
            CircularTrackColor: circularTrackColor ?? CircularTrackColor,
            RefreshBackgroundColor: refreshBackgroundColor ?? RefreshBackgroundColor,
            BorderRadius: borderRadius ?? BorderRadius,
            StopIndicatorColor: stopIndicatorColor ?? StopIndicatorColor,
            StopIndicatorRadius: stopIndicatorRadius ?? StopIndicatorRadius,
            StrokeWidth: strokeWidth ?? StrokeWidth,
            StrokeAlign: strokeAlign ?? StrokeAlign,
            StrokeCap: strokeCap ?? StrokeCap,
            Constraints: constraints ?? Constraints,
            TrackGap: trackGap ?? TrackGap,
            CircularTrackPadding: circularTrackPadding ?? CircularTrackPadding,
            Year2023: year2023 ?? Year2023,
            Controller: controller ?? Controller);
    }
}

public sealed class ProgressIndicatorTheme : InheritedTheme
{
    public ProgressIndicatorTheme(
        ProgressIndicatorThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ProgressIndicatorThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ProgressIndicatorTheme(data: Data, child: child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ProgressIndicatorTheme)oldWidget).Data, Data);
    }

    public static ProgressIndicatorThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ProgressIndicatorTheme>()?.Data
               ?? Theme.Of(context).ProgressIndicatorTheme;
    }
}
