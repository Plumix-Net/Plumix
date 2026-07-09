using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
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

    public override Widget Build(BuildContext context)
    {
        var ambient = Theme.Of(context);
        var pageTheme = ambient with
        {
            UseMaterial3 = _useMaterial3,
            TabBarTheme = _useThemeOverrides
                ? new TabBarThemeData(
                    IndicatorColor: Color.Parse("#FF00695C"),
                    DividerColor: Color.Parse("#FF80CBC4"),
                    LabelColor: Color.Parse("#FF00695C"),
                    UnselectedLabelColor: Color.Parse("#FF455A64"),
                    IndicatorSize: TabBarIndicatorSize.Tab)
                : new TabBarThemeData(),
        };

        return new Theme(
            pageTheme,
            new DefaultTabController(
                length: 4,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_useMaterial3 ? "Material 3" : "Material 2",
                                    () => SetState(() => _useMaterial3 = !_useMaterial3)),
                                ControlButton(_isScrollable ? "Scrollable" : "Fill",
                                    () => SetState(() => _isScrollable = !_isScrollable)),
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off",
                                    () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                            ]),
                        new Text("Tap tabs or swipe pages; the indicator and labels share one TabController.",
                            fontSize: 13,
                            color: Color.Parse("#8A000000")),
                        new Center(
                            child: new TabPageSelector(
                                indicatorSize: _useThemeOverrides ? 16 : 12,
                                color: _useThemeOverrides ? Color.Parse("#FFB2DFDB") : null,
                                selectedColor: _useThemeOverrides ? Color.Parse("#FF00695C") : null,
                                borderStyle: _useThemeOverrides ? BorderStyle.None : null)),
                        new Expanded(
                            child: new Scaffold(
                                appBar: new AppBar(
                                    titleText: "Tabs preview",
                                    automaticallyImplyLeading: false,
                                    bottom: new TabBar(
                                        isScrollable: _isScrollable,
                                        tabs:
                                        [
                                            new Tab(text: "HOME", icon: new Icon(Icons.StarOutline)),
                                            new Tab(text: "EXPLORE", icon: new Icon(Icons.InfoOutline)),
                                            new Tab(text: "SAVED", icon: new Icon(Icons.Check)),
                                            new Tab(text: "MORE", icon: new Icon(Icons.Menu)),
                                        ])),
                                body: new TabBarView(
                                    children:
                                    [
                                        Page("HOME", "Primary filled tab layout", Color.Parse("#FFE8DEF8")),
                                        Page("EXPLORE", "Swipe keeps indicator animation synchronized", Color.Parse("#FFD0BCFF")),
                                        Page("SAVED", "Selected semantics follow the active page", Color.Parse("#FFB2DFDB")),
                                        Page("MORE", "Scrollable and themed paths use the same controller", Color.Parse("#FFFFD8E4")),
                                    ])))
                    ])));
    }

    private static Widget Page(string title, string subtitle, Color color) => new ColoredBox(
        color,
        new Center(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                spacing: 8,
                children:
                [
                    new Text(title, fontSize: 22),
                    new Text(subtitle, fontSize: 13, color: Color.Parse("#8A000000")),
                ])));

    private static Widget ControlButton(string label, Action onPressed) => new TextButton(
        onPressed: onPressed,
        backgroundColor: Color.Parse("#FFEADDFF"),
        foregroundColor: Color.Parse("#FF21005D"),
        minHeight: 36,
        child: new Text(label, fontSize: 12));
}
