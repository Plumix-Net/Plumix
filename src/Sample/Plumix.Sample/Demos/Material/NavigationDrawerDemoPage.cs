using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/navigation_drawer_demo_page.dart

public sealed class NavigationDrawerDemoPage : StatefulWidget
{
    public override State CreateState() => new NavigationDrawerDemoPageState();

    private sealed class NavigationDrawerDemoPageState : State
    {
        private int? _selectedIndex;
        private bool _thirdEnabled = true;
        private bool _useThemeOverrides;
        private bool _useWidgetOverrides;

        public override Widget Build(BuildContext context)
        {
            var pageTheme = Theme.Of(context) with
            {
                NavigationDrawerTheme = _useThemeOverrides
                    ? new NavigationDrawerThemeData(
                        BackgroundColor: new Color(0xFFF3E5F5),
                        IndicatorColor: new Color(0xFFB2DFDB),
                        TileHeight: 60,
                        IndicatorSize: new Size(270, 48),
                        LabelTextStyle: WidgetStateProperty<TextStyle?>.All(
                            new TextStyle(Color: new Color(0xFF4A148C), FontSize: 13)),
                        IconTheme: WidgetStateProperty<IconThemeData?>.All(
                            new IconThemeData(Color: new Color(0xFF00695C), Size: 22)))
                    : new NavigationDrawerThemeData(),
            };

            return new Theme(
                data: pageTheme,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Text(
                            "NavigationDrawer + NavigationDrawerDestination",
                            style: new TextStyle(FontSize: 20)),
                        new Text(
                            "Header/footer slots, custom children, destination indexing, selection, disabled state, "
                            + "and theme precedence.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                ControlButton(
                                    _useThemeOverrides ? "Theme on" : "Theme off",
                                    () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                                ControlButton(
                                    _useWidgetOverrides ? "Widget on" : "Widget off",
                                    () => SetState(() => _useWidgetOverrides = !_useWidgetOverrides)),
                                ControlButton(
                                    _thirdEnabled ? "Disable third" : "Enable third",
                                    () => SetState(() => _thirdEnabled = !_thirdEnabled)),
                                ControlButton(
                                    _selectedIndex.HasValue ? "Clear selection" : "Select first",
                                    () => SetState(() => _selectedIndex = _selectedIndex.HasValue ? null : 0)),
                            ]),
                        new Text(
                            _selectedIndex.HasValue
                                ? $"Selected destination: {_selectedIndex.Value + 1}"
                                : "Selected destination: none"),
                        new Expanded(
                            child: new Row(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 16,
                                children:
                                [
                                    new NavigationDrawer(
                                        selectedIndex: _selectedIndex,
                                        onDestinationSelected: index => SetState(() => _selectedIndex = index),
                                        backgroundColor: _useWidgetOverrides ? new Color(0xFFFFF8E1) : null,
                                        indicatorColor: _useWidgetOverrides ? new Color(0xFFFFCC80) : null,
                                        tilePadding: EdgeInsets.Symmetric(horizontal: _useWidgetOverrides ? 18 : 12),
                                        header: new Padding(
                                            EdgeInsets.FromLTRB(28, 20, 16, 12),
                                            new Text("Destinations", style: new TextStyle(FontSize: 16))),
                                        footer: new Padding(
                                            EdgeInsets.FromLTRB(28, 12, 16, 20),
                                            new Text(
                                                "Navigation footer",
                                                style: new TextStyle(
                                                    FontSize: 12,
                                                    Color: MaterialColors.Black54))),
                                        children:
                                        [
                                            new Padding(
                                                EdgeInsets.Symmetric(horizontal: 28, vertical: 8),
                                                new Text(
                                                    "Primary",
                                                    style: new TextStyle(
                                                        FontSize: 12,
                                                        Color: MaterialColors.Black54))),
                                            new NavigationDrawerDestination(
                                                icon: new Icon(Icons.StarOutline),
                                                selectedIcon: new Icon(Icons.Star),
                                                label: new Text("Favorites")),
                                            new NavigationDrawerDestination(
                                                icon: new Icon(Icons.InfoOutline),
                                                label: new Text("Explore")),
                                            new Divider(indent: 28, endIndent: 28),
                                            new NavigationDrawerDestination(
                                                icon: new Icon(Icons.Menu),
                                                label: new Text("Downloads"),
                                                enabled: _thirdEnabled),
                                        ]),
                                    new Expanded(
                                        child: new DecoratedBox(
                                            decoration: new BoxDecoration(
                                                Color: new Color(0xFFF7F2FA),
                                                BorderRadius: BorderRadius.Circular(12)),
                                            child: new Center(
                                                child: new Text(
                                                    "The drawer keeps destination indices independent from "
                                                    + "custom children.",
                                                    textAlign: TextAlign.Center,
                                                    style: new TextStyle(Color: MaterialColors.Black54)))))
                                ]))
                    ]));
        }

        private static Widget ControlButton(string label, Action onPressed)
        {
            return new TextButton(onPressed: onPressed, child: new Text(label));
        }
    }
}
