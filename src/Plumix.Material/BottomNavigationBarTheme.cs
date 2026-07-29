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
    BottomNavigationBarType? Type = null)
{
    public BottomNavigationBarThemeData CopyWith(
        Color? backgroundColor = null,
        double? elevation = null,
        IconThemeData? selectedIconTheme = null,
        IconThemeData? unselectedIconTheme = null,
        Color? selectedItemColor = null,
        Color? unselectedItemColor = null,
        TextStyle? selectedLabelStyle = null,
        TextStyle? unselectedLabelStyle = null,
        bool? showSelectedLabels = null,
        bool? showUnselectedLabels = null,
        BottomNavigationBarType? type = null)
    {
        return new BottomNavigationBarThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            SelectedIconTheme: selectedIconTheme ?? SelectedIconTheme,
            UnselectedIconTheme: unselectedIconTheme ?? UnselectedIconTheme,
            SelectedItemColor: selectedItemColor ?? SelectedItemColor,
            UnselectedItemColor: unselectedItemColor ?? UnselectedItemColor,
            SelectedLabelStyle: selectedLabelStyle ?? SelectedLabelStyle,
            UnselectedLabelStyle: unselectedLabelStyle ?? UnselectedLabelStyle,
            ShowSelectedLabels: showSelectedLabels ?? ShowSelectedLabels,
            ShowUnselectedLabels: showUnselectedLabels ?? ShowUnselectedLabels,
            Type: type ?? Type);
    }

    public static BottomNavigationBarThemeData Lerp(
        BottomNavigationBarThemeData? a,
        BottomNavigationBarThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new BottomNavigationBarThemeData(
            BackgroundColor: MaterialThemeLerp.Color(
                a?.BackgroundColor,
                b?.BackgroundColor,
                clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            SelectedIconTheme: MaterialThemeLerp.IconTheme(
                a?.SelectedIconTheme,
                b?.SelectedIconTheme,
                clampedT),
            UnselectedIconTheme: MaterialThemeLerp.IconTheme(
                a?.UnselectedIconTheme,
                b?.UnselectedIconTheme,
                clampedT),
            SelectedItemColor: MaterialThemeLerp.Color(
                a?.SelectedItemColor,
                b?.SelectedItemColor,
                clampedT),
            UnselectedItemColor: MaterialThemeLerp.Color(
                a?.UnselectedItemColor,
                b?.UnselectedItemColor,
                clampedT),
            SelectedLabelStyle: MaterialThemeLerp.TextStyle(
                a?.SelectedLabelStyle,
                b?.SelectedLabelStyle,
                clampedT),
            UnselectedLabelStyle: MaterialThemeLerp.TextStyle(
                a?.UnselectedLabelStyle,
                b?.UnselectedLabelStyle,
                clampedT),
            ShowSelectedLabels: clampedT < 0.5 ? a?.ShowSelectedLabels : b?.ShowSelectedLabels,
            ShowUnselectedLabels: clampedT < 0.5
                ? a?.ShowUnselectedLabels
                : b?.ShowUnselectedLabels,
            Type: clampedT < 0.5 ? a?.Type : b?.Type);
    }
}

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
