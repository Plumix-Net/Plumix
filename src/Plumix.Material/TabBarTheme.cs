using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tab_bar_theme.dart

public sealed partial record TabBarThemeData(
    BoxDecoration? Indicator = null,
    Color? IndicatorColor = null,
    TabBarIndicatorSize? IndicatorSize = null,
    Color? DividerColor = null,
    double? DividerHeight = null,
    Color? LabelColor = null,
    Thickness? LabelPadding = null,
    TextStyle? LabelStyle = null,
    Color? UnselectedLabelColor = null,
    TextStyle? UnselectedLabelStyle = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    TabAlignment? TabAlignment = null,
    TabIndicatorAnimation? IndicatorAnimation = null,
    BorderRadius? SplashBorderRadius = null);

public sealed class TabBarTheme : InheritedWidget
{
    public TabBarTheme(TabBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TabBarThemeData Data { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((TabBarTheme)oldWidget).Data, Data);

    public static TabBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<TabBarTheme>()?.Data ?? Theme.Of(context).TabBarTheme;
}
