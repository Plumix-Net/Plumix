using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_rail_theme.dart

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
    double? MinExtendedWidth = null);

public sealed class NavigationRailTheme : InheritedWidget
{
    public NavigationRailTheme(NavigationRailThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public NavigationRailThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

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
