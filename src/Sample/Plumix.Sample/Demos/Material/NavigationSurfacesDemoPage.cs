using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/navigation_surfaces_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class NavigationSurfacesDemoPage : StatefulWidget
{
    public override State CreateState() => new NavigationSurfacesDemoPageState();
}

internal sealed class NavigationSurfacesDemoPageState : State
{
    private int _selectedIndex;
    private bool _useMaterial3 = true;
    private bool _extended;
    private bool _useThemeOverrides;
    private bool _useSeedScheme;
    private NavigationDestinationLabelBehavior _barLabelBehavior = NavigationDestinationLabelBehavior.AlwaysShow;
    private NavigationRailLabelType _railLabelType = NavigationRailLabelType.All;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        ColorScheme colorScheme = _useSeedScheme
            ? ColorScheme.FromSeed(Color.Parse("#FF006495"))
            : baseTheme.ColorScheme;
        var pageTheme = new ThemeData(
            platform: baseTheme.Platform,
            colorScheme: colorScheme,
            useMaterial3: _useMaterial3);
        if (_useThemeOverrides)
        {
            pageTheme = pageTheme with
            {
                NavigationBarTheme = new NavigationBarThemeData(
                    BackgroundColor: Color.Parse("#FFE0F2F1"),
                    IndicatorColor: Color.Parse("#FF00695C"),
                    Height: 76),
                NavigationRailTheme = new NavigationRailThemeData(
                    BackgroundColor: Color.Parse("#FFF3E5F5"),
                    IndicatorColor: Color.Parse("#FF6A1B9A"),
                    MinWidth: 76,
                    MinExtendedWidth: 220),
            };
        }

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 14,
                children:
                [
                    new Text("NavigationBar + NavigationRail", fontSize: 20),
                    new Text(
                        "Seed-generated ColorScheme, Material 2021 typography, navigation defaults, "
                        + "and theme precedence.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(
                                _useMaterial3 ? "Material 3" : "Material 2",
                                () => SetState(() => _useMaterial3 = !_useMaterial3)),
                            ControlButton(
                                _useSeedScheme ? "Seed scheme" : "Baseline scheme",
                                () => SetState(() => _useSeedScheme = !_useSeedScheme)),
                            ControlButton(
                                _useThemeOverrides ? "Theme on" : "Theme off",
                                () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                            ControlButton(
                                _extended ? "Rail extended" : "Rail compact",
                                () => SetState(() => _extended = !_extended)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton($"Bar: {Format(_barLabelBehavior)}", CycleBarLabels),
                            ControlButton($"Rail: {Format(_railLabelType)}", CycleRailLabels),
                        ]),
                    new Text($"Selected destination: {_selectedIndex + 1}", fontSize: 13),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            PaletteChip("primary", colorScheme.Primary, colorScheme.OnPrimary),
                            PaletteChip("secondary", colorScheme.Secondary, colorScheme.OnSecondary),
                            PaletteChip("tertiary", colorScheme.Tertiary, colorScheme.OnTertiary),
                        ]),
                    new DefaultTextStyle(
                        style: pageTheme.TextTheme.TitleMedium,
                        child: new Text(
                            $"titleMedium · {pageTheme.TextTheme.TitleMedium.FontSize:0}px")),
                    new Container(
                        decoration: new BoxDecoration(
                            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#33000000"))),
                            BorderRadius: BorderRadius.Circular(12)),
                        child: new NavigationBar(
                            selectedIndex: _selectedIndex,
                            onDestinationSelected: index => SetState(() => _selectedIndex = index),
                            labelBehavior: _barLabelBehavior,
                            destinations:
                            [
                                new NavigationDestination(new Icon(Icons.StarOutline), "Favorites", selectedIcon: new Icon(Icons.Star)),
                                new NavigationDestination(new Icon(Icons.InfoOutline), "Explore"),
                                new NavigationDestination(new Icon(Icons.Menu), "Disabled", enabled: false),
                            ])),
                    new SizedBox(
                        height: 280,
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new NavigationRail(
                                    selectedIndex: _selectedIndex,
                                    onDestinationSelected: index => SetState(() => _selectedIndex = index),
                                    extended: _extended,
                                    labelType: _extended ? NavigationRailLabelType.None : _railLabelType,
                                    destinations:
                                    [
                                        new NavigationRailDestination(new Icon(Icons.StarOutline), new Text("Favorites"), new Icon(Icons.Star)),
                                        new NavigationRailDestination(new Icon(Icons.InfoOutline), new Text("Explore")),
                                        new NavigationRailDestination(new Icon(Icons.Menu), new Text("Disabled"), disabled: true),
                                    ]),
                                new Expanded(
                                    child: new Container(
                                        color: Color.Parse("#FFF7F2FA"),
                                        alignment: Alignment.Center,
                                        child: new Text("Rail content area", color: Color.Parse("#8A000000"))))
                            ]))
                ]));
    }

    private void CycleBarLabels()
    {
        SetState(() => _barLabelBehavior = _barLabelBehavior switch
        {
            NavigationDestinationLabelBehavior.AlwaysShow => NavigationDestinationLabelBehavior.OnlyShowSelected,
            NavigationDestinationLabelBehavior.OnlyShowSelected => NavigationDestinationLabelBehavior.AlwaysHide,
            _ => NavigationDestinationLabelBehavior.AlwaysShow,
        });
    }

    private void CycleRailLabels()
    {
        SetState(() => _railLabelType = _railLabelType switch
        {
            NavigationRailLabelType.All => NavigationRailLabelType.Selected,
            NavigationRailLabelType.Selected => NavigationRailLabelType.None,
            _ => NavigationRailLabelType.All,
        });
    }

    private static string Format(object value) => value.ToString()!.ToLowerInvariant();

    private static Widget PaletteChip(string label, Color color, Color onColor)
    {
        return new Container(
            width: 104,
            height: 48,
            color: color,
            alignment: Alignment.Center,
            child: new Text(label, fontSize: 11, color: onColor));
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            child: new Text(label, fontSize: 12),
            style: TextButton.StyleFrom(
                foregroundColor: Color.Parse("#FF21005D"),
                backgroundColor: Color.Parse("#FFEADDFF"),
                minimumSize: new Size(64, 36)));
    }
}
