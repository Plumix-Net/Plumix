using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/bottom_navigation_bar_theme.dart (approximate)

public sealed record BottomNavigationBarThemeData(
    Color? BackgroundColor = null,
    double? Elevation = null,
    IconThemeData? SelectedIconTheme = null,
    IconThemeData? UnselectedIconTheme = null,
    Color? SelectedItemColor = null,
    Color? UnselectedItemColor = null,
    TextStyle? SelectedLabelStyle = null,
    TextStyle? UnselectedLabelStyle = null,
    bool? ShowSelectedLabels = null,
    bool? ShowUnselectedLabels = null,
    BottomNavigationBarType? Type = null);

public sealed class BottomNavigationBarTheme : InheritedWidget
{
    public BottomNavigationBarTheme(
        BottomNavigationBarThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BottomNavigationBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((BottomNavigationBarTheme)oldWidget).Data, Data);
    }

    public static BottomNavigationBarThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<BottomNavigationBarTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).BottomNavigationBarTheme;
    }
}
