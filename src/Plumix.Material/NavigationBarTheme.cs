using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_bar_theme.dart

public sealed record NavigationBarThemeData(
    double? Height = null,
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    Color? IndicatorColor = null,
    ShapeBorder? IndicatorShape = null,
    MaterialStateProperty<TextStyle?>? LabelTextStyle = null,
    MaterialStateProperty<IconThemeData?>? IconTheme = null,
    NavigationDestinationLabelBehavior? LabelBehavior = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    Thickness? LabelPadding = null);

public sealed class NavigationBarTheme : InheritedWidget
{
    public NavigationBarTheme(NavigationBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public NavigationBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((NavigationBarTheme)oldWidget).Data, Data);
    }

    public static NavigationBarThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<NavigationBarTheme>()?.Data
               ?? Theme.Of(context).NavigationBarTheme;
    }
}
