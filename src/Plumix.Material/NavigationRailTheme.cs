using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/navigation_rail_theme.dart

public sealed record NavigationRailThemeData(
    Color? BackgroundColor = null,
    double? Elevation = null,
    TextStyle? UnselectedLabelTextStyle = null,
    TextStyle? SelectedLabelTextStyle = null,
    IconThemeData? UnselectedIconTheme = null,
    IconThemeData? SelectedIconTheme = null,
    double? GroupAlignment = null,
    NavigationRailLabelType? LabelType = null,
    bool? UseIndicator = null,
    Color? IndicatorColor = null,
    ShapeBorder? IndicatorShape = null,
    double? MinWidth = null,
    double? MinExtendedWidth = null)
{
    public NavigationRailThemeData CopyWith(
        Color? backgroundColor = null,
        double? elevation = null,
        TextStyle? unselectedLabelTextStyle = null,
        TextStyle? selectedLabelTextStyle = null,
        IconThemeData? unselectedIconTheme = null,
        IconThemeData? selectedIconTheme = null,
        double? groupAlignment = null,
        NavigationRailLabelType? labelType = null,
        bool? useIndicator = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        double? minWidth = null,
        double? minExtendedWidth = null)
    {
        return new NavigationRailThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            UnselectedLabelTextStyle: unselectedLabelTextStyle ?? UnselectedLabelTextStyle,
            SelectedLabelTextStyle: selectedLabelTextStyle ?? SelectedLabelTextStyle,
            UnselectedIconTheme: unselectedIconTheme ?? UnselectedIconTheme,
            SelectedIconTheme: selectedIconTheme ?? SelectedIconTheme,
            GroupAlignment: groupAlignment ?? GroupAlignment,
            LabelType: labelType ?? LabelType,
            UseIndicator: useIndicator ?? UseIndicator,
            IndicatorColor: indicatorColor ?? IndicatorColor,
            IndicatorShape: indicatorShape ?? IndicatorShape,
            MinWidth: minWidth ?? MinWidth,
            MinExtendedWidth: minExtendedWidth ?? MinExtendedWidth);
    }

    public static NavigationRailThemeData? Lerp(
        NavigationRailThemeData? a,
        NavigationRailThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new NavigationRailThemeData(
            BackgroundColor: MaterialThemeLerp.Color(
                a?.BackgroundColor,
                b?.BackgroundColor,
                clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            UnselectedLabelTextStyle: MaterialThemeLerp.TextStyle(
                a?.UnselectedLabelTextStyle,
                b?.UnselectedLabelTextStyle,
                clampedT),
            SelectedLabelTextStyle: MaterialThemeLerp.TextStyle(
                a?.SelectedLabelTextStyle,
                b?.SelectedLabelTextStyle,
                clampedT),
            UnselectedIconTheme: MaterialThemeLerp.IconTheme(
                a?.UnselectedIconTheme,
                b?.UnselectedIconTheme,
                clampedT),
            SelectedIconTheme: MaterialThemeLerp.IconTheme(
                a?.SelectedIconTheme,
                b?.SelectedIconTheme,
                clampedT),
            GroupAlignment: MaterialThemeLerp.Double(a?.GroupAlignment, b?.GroupAlignment, clampedT),
            LabelType: clampedT < 0.5 ? a?.LabelType : b?.LabelType,
            UseIndicator: clampedT < 0.5 ? a?.UseIndicator : b?.UseIndicator,
            IndicatorColor: MaterialThemeLerp.Color(
                a?.IndicatorColor,
                b?.IndicatorColor,
                clampedT),
            IndicatorShape: MaterialThemeLerp.Shape(
                a?.IndicatorShape,
                b?.IndicatorShape,
                clampedT),
            MinWidth: MaterialThemeLerp.Double(a?.MinWidth, b?.MinWidth, clampedT),
            MinExtendedWidth: MaterialThemeLerp.Double(
                a?.MinExtendedWidth,
                b?.MinExtendedWidth,
                clampedT));
    }
}

public sealed class NavigationRailTheme : InheritedTheme
{
    public NavigationRailTheme(NavigationRailThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public NavigationRailThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new NavigationRailTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((NavigationRailTheme)oldWidget).Data, Data);
    }

    public static NavigationRailThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<NavigationRailTheme>()?.Data
               ?? Theme.Of(context).NavigationRailTheme;
    }
}
