using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/tabs_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class TabsDemoPage : StatefulWidget
{
    public override State CreateState() => new TabsDemoPageState();
}

internal sealed class TabsDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _isScrollable;
    private bool _useThemeOverrides;
    private bool _isPrimary = true;
    private bool _useElasticIndicator = true;
    private bool _useCenterAlignment;
    private bool _useClampedTextScaling;

    public override Widget Build(BuildContext context)
    {
        var ambient = Theme.Of(context);
        var pageTheme = ambient with
        {
            UseMaterial3 = _useMaterial3,
            TabBarTheme = _useThemeOverrides
                ? new TabBarThemeData(
                    IndicatorColor: new Color(0xFF00695C),
                    DividerColor: new Color(0xFF80CBC4),
                    LabelColor: new Color(0xFF00695C),
                    UnselectedLabelColor: new Color(0xFF455A64),
                    IndicatorSize: TabBarIndicatorSize.Tab)
                : new TabBarThemeData(),
        };

        TabAlignment alignment = _useCenterAlignment
            ? TabAlignment.Center
            : _isScrollable
                ? _useMaterial3 ? TabAlignment.StartOffset : TabAlignment.Start
                : TabAlignment.Fill;
        var indicatorAnimation = _useElasticIndicator
            ? TabIndicatorAnimation.Elastic
            : TabIndicatorAnimation.Linear;
        TextScaler? tabTextScaler = _useClampedTextScaling
            ? TextScaler.Linear(2.0).Clamp(maxScaleFactor: 1.25)
            : null;

        Widget[] tabs =
        [
            new Tab(text: "HOME", icon: new Icon(Icons.StarOutline)),
            new Tab(text: "EXPLORE", icon: new Icon(Icons.InfoOutline)),
            new Tab(text: "SAVED", icon: new Icon(Icons.Check)),
            new Tab(text: "MORE", icon: new Icon(Icons.Menu)),
        ];

        return new Theme(
            pageTheme,
            new DefaultTabController(
                length: 4,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                ControlButton(_useMaterial3 ? "Material 3" : "Material 2",
                                    () => SetState(() => _useMaterial3 = !_useMaterial3)),
                                ControlButton(_isScrollable ? "Scrollable" : "Fill",
                                    () => SetState(() => _isScrollable = !_isScrollable)),
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off",
                                    () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                                ControlButton(_isPrimary ? "Primary" : "Secondary",
                                    () => SetState(() => _isPrimary = !_isPrimary)),
                                ControlButton(_useElasticIndicator ? "Elastic" : "Linear",
                                    () => SetState(() => _useElasticIndicator = !_useElasticIndicator)),
                                ControlButton(_useCenterAlignment ? "Center" : "Default align",
                                    () => SetState(() => _useCenterAlignment = !_useCenterAlignment)),
                                ControlButton(_useClampedTextScaling ? "Scale 1.25×" : "Ambient scale",
                                    () => SetState(() => _useClampedTextScaling = !_useClampedTextScaling)),
                            ]),
                        new Text("Tap tabs or swipe pages; the indicator and labels share one TabController.",
                            fontSize: 13,
                            color: new Color(0x8A000000)),
                        new Center(
                            child: new TabPageSelector(
                                indicatorSize: _useThemeOverrides ? 16 : 12,
                                color: _useThemeOverrides ? new Color(0xFFB2DFDB) : null,
                                selectedColor: _useThemeOverrides ? new Color(0xFF00695C) : null,
                                borderStyle: _useThemeOverrides ? BorderStyle.None : null)),
                        new Text("Custom UnderlineTabIndicator: 6px inset, rounded, 5px weight.",
                            fontSize: 13,
                            color: new Color(0x8A000000)),
                        new TabBar(
                            indicator: new UnderlineTabIndicator(
                                borderRadius: BorderRadius.Circular(5),
                                borderSide: new BorderSide(new Color(0xFF6750A4), 5),
                                insets: EdgeInsetsGeometry.Symmetric(horizontal: 6)),
                            indicatorSize: TabBarIndicatorSize.Label,
                            tabs: [new Tab(text: "ONE"), new Tab(text: "TWO"), new Tab(text: "THREE"),
                                new Tab(text: "FOUR")]),
                        new Expanded(
                            child: new Scaffold(
                                appBar: new AppBar(
                                    title: new Text("Tabs preview"),
                                    automaticallyImplyLeading: false,
                                    bottom: _isPrimary
                                        ? new TabBar(
                                            isScrollable: _isScrollable,
                                            tabAlignment: alignment,
                                            indicatorAnimation: indicatorAnimation,
                                            textScaler: tabTextScaler,
                                            tabs: tabs)
                                        : TabBar.Secondary(
                                            isScrollable: _isScrollable,
                                            tabAlignment: alignment,
                                            indicatorAnimation: indicatorAnimation,
                                            textScaler: tabTextScaler,
                                            tabs: tabs)),
                                body: new TabBarView(
                                    children:
                                    [
                                        Page("HOME", "Primary filled tab layout", new Color(0xFFE8DEF8)),
                                        Page("EXPLORE", "Swipe keeps indicator animation synchronized",
                                            new Color(0xFFD0BCFF)),
                                        Page("SAVED", "Selected semantics follow the active page",
                                            new Color(0xFFB2DFDB)),
                                        Page("MORE", "Scrollable and themed paths use the same controller",
                                            new Color(0xFFFFD8E4)),
                                    ])))
                    ])));
    }

    private static Widget Page(string title, string subtitle, Color color) => new ColoredBox(
        color,
        child: new Center(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                spacing: 8,
                children:
                [
                    new Text(title, fontSize: 22),
                    new Text(subtitle, fontSize: 13, color: new Color(0x8A000000)),
                ])));

    private static Widget ControlButton(string label, Action onPressed) => new TextButton(
        onPressed: onPressed,
        child: new Text(label, fontSize: 12),
        style: TextButton.StyleFrom(
            foregroundColor: new Color(0xFF21005D),
            backgroundColor: new Color(0xFFEADDFF),
            minimumSize: new Size(64, 36)));
}
