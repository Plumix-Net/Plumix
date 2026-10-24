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
            ? ColorScheme.FromSeed(new Color(0xFF006495))
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
                    BackgroundColor: new Color(0xFFE0F2F1),
                    IndicatorColor: new Color(0xFF00695C),
                    Height: 76),
                NavigationRailTheme = new NavigationRailThemeData(
                    BackgroundColor: new Color(0xFFF3E5F5),
                    IndicatorColor: new Color(0xFF6A1B9A),
                    MinWidth: 76,
                    MinExtendedWidth: 220),
            };
        }

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Text("NavigationBar + NavigationRail", style: new TextStyle(FontSize: 20)),
                    new SizedBox(height: 14),
                    new Text(
                        "Seed-generated ColorScheme, Material 2021 typography, navigation defaults, "
                        + "theme precedence, and icon-scoped hover/press ripples.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new SizedBox(height: 14),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
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
                            ControlButton($"Bar: {Format(_barLabelBehavior)}", CycleBarLabels),
                            ControlButton($"Rail: {Format(_railLabelType)}", CycleRailLabels),
                        ]),
                    new SizedBox(height: 14),
                    new Text($"Selected destination: {_selectedIndex + 1}"),
                    new SizedBox(height: 14),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            PaletteChip("primary", colorScheme.Primary, colorScheme.OnPrimary),
                            PaletteChip("secondary", colorScheme.Secondary, colorScheme.OnSecondary),
                            PaletteChip("tertiary", colorScheme.Tertiary, colorScheme.OnTertiary),
                        ]),
                    new SizedBox(height: 8),
                    new Text(
                        $"titleMedium · {pageTheme.TextTheme.TitleMedium?.FontSize?.ToString("0") ?? "null"}px",
                        style: pageTheme.TextTheme.TitleMedium),
                    new SizedBox(height: 14),
                    new DecoratedBox(
                        decoration: new BoxDecoration(
                            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(new Color(0x33000000))),
                            BorderRadius: BorderRadius.Circular(12)),
                        child: new NavigationBar(
                            selectedIndex: _selectedIndex,
                            onDestinationSelected: index => SetState(() => _selectedIndex = index),
                            labelBehavior: _barLabelBehavior,
                            destinations:
                            [
                                new NavigationDestination(
                                    new Icon(Icons.StarOutline),
                                    "Favorites",
                                    selectedIcon: new Icon(Icons.Star)),
                                new NavigationDestination(new Icon(Icons.InfoOutline), "Explore"),
                                new NavigationDestination(new Icon(Icons.Menu), "Disabled", enabled: false),
                            ])),
                    new SizedBox(height: 14),
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
                                        new NavigationRailDestination(
                                            new Icon(Icons.StarOutline),
                                            new Text("Favorites"),
                                            new Icon(Icons.Star)),
                                        new NavigationRailDestination(new Icon(Icons.InfoOutline), new Text("Explore")),
                                        new NavigationRailDestination(
                                            new Icon(Icons.Menu),
                                            new Text("Disabled"),
                                            disabled: true),
                                    ]),
                                new Expanded(
                                    child: new ColoredBox(
                                        color: new Color(0xFFF7F2FA),
                                        child: new Center(
                                            child: new Text(
                                                "Rail content area",
                                                style: new TextStyle(Color: new Color(0x8A000000)))))),
                            ])),
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

    private static string Format(Enum value) => value.Name();

    private static Widget PaletteChip(string label, Color color, Color onColor)
    {
        return new Container(
            width: 104,
            height: 48,
            color: color,
            alignment: Alignment.Center,
            child: new Text(label, style: new TextStyle(FontSize: 11, Color: onColor)));
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            child: new Text(label, style: new TextStyle(FontSize: 12)),
            style: TextButton.StyleFrom(
                foregroundColor: new Color(0xFF21005D),
                backgroundColor: new Color(0xFFEADDFF),
                minimumSize: new Size(0, 36)));
    }
}
