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
                        BackgroundColor: Color.Parse("#FFF3E5F5"),
                        IndicatorColor: Color.Parse("#FFB2DFDB"),
                        TileHeight: 60,
                        IndicatorSize: new Size(270, 48),
                        LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                            new TextStyle(Color: Color.Parse("#FF4A148C"), FontSize: 13)),
                        IconTheme: MaterialStateProperty<IconThemeData?>.All(
                            new IconThemeData(Color: Color.Parse("#FF00695C"), Size: 22)))
                    : new NavigationDrawerThemeData(),
            };

            return new Theme(
                data: pageTheme,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Text("NavigationDrawer + NavigationDrawerDestination", fontSize: 20),
                        new Text(
                            "Header/footer slots, custom children, destination indexing, selection, disabled state, and theme precedence.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Row(
                            spacing: 8,
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
                                : "Selected destination: none",
                            fontSize: 13),
                        new Expanded(
                            child: new Row(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 16,
                                children:
                                [
                                    new NavigationDrawer(
                                        selectedIndex: _selectedIndex,
                                        onDestinationSelected: index => SetState(() => _selectedIndex = index),
                                        backgroundColor: _useWidgetOverrides ? Color.Parse("#FFFFF8E1") : null,
                                        indicatorColor: _useWidgetOverrides ? Color.Parse("#FFFFCC80") : null,
                                        tilePadding: _useWidgetOverrides ? new Thickness(18, 0) : null,
                                        header: new Padding(
                                            new Thickness(28, 20, 16, 12),
                                            new Text("Destinations", fontSize: 16)),
                                        footer: new Padding(
                                            new Thickness(28, 12, 16, 20),
                                            new Text("Navigation footer", fontSize: 12, color: Colors.DimGray)),
                                        children:
                                        [
                                            new Padding(
                                                new Thickness(28, 8),
                                                new Text("Primary", fontSize: 12, color: Colors.DimGray)),
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
                                        child: new Container(
                                            alignment: Alignment.Center,
                                            decoration: new BoxDecoration(
                                                Color: Color.Parse("#FFF7F2FA"),
                                                BorderRadius: BorderRadius.Circular(12)),
                                            child: new Text(
                                                "The drawer keeps destination indices independent from custom children.",
                                                color: Colors.DimGray,
                                                textAlign: TextAlign.Center)))
                                ]))
                    ]));
        }

        private static Widget ControlButton(string label, Action onPressed)
        {
            return new TextButton(
                child: new Text(label, fontSize: 12),
                onPressed: onPressed);
        }
    }
}
