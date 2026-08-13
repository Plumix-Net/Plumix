using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/bar_controls_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class BarControlsDemoPage : StatefulWidget
{
    public override State CreateState() => new BarControlsDemoPageState();
}

internal sealed class BarControlsDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _useThemeOverrides;
    private bool _showNotch = true;
    private bool _narrowButtonBar;
    private bool _overflowUp;
    private bool _useRtl;
    private int _actionCount;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var theme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            BottomAppBarTheme = _useThemeOverrides
                ? new BottomAppBarThemeData(
                    Color: Color.Parse("#FFE8F5E9"),
                    Elevation: 6,
                    Height: 72,
                    ShadowColor: Color.Parse("#66000000"),
                    Padding: new Thickness(10, 8))
                : new BottomAppBarThemeData(),
            ButtonBarTheme = _useThemeOverrides
                ? new ButtonBarThemeData(
                    Alignment: MainAxisAlignment.Center,
                    ButtonMinWidth: 72,
                    ButtonHeight: 40,
                    ButtonPadding: EdgeInsetsGeometry.DirectionalOnly(start: 14, top: 2, end: 6, bottom: 2),
                    LayoutBehavior: ButtonBarLayoutBehavior.Constrained,
                    OverflowDirection: _overflowUp ? VerticalDirection.Up : VerticalDirection.Down)
                : new ButtonBarThemeData(
                    OverflowDirection: _overflowUp ? VerticalDirection.Up : VerticalDirection.Down),
        };

        return new Theme(
            data: theme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("BottomAppBar + ButtonBar", fontSize: 20, color: Colors.Black),
                    new Text(
                        "M2/M3 bottom-surface defaults, FAB notch geometry, SafeArea, theme precedence, and legacy ButtonBar row-to-column overflow.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildToggle(_useMaterial3 ? "M3" : "M2", () => _useMaterial3 = !_useMaterial3, 68),
                            BuildToggle(_useThemeOverrides ? "theme=on" : "theme=off", () => _useThemeOverrides = !_useThemeOverrides, 104),
                            BuildToggle(_showNotch ? "notch=on" : "notch=off", () => _showNotch = !_showNotch, 104),
                            BuildToggle(_narrowButtonBar ? "bar=narrow" : "bar=wide", () => _narrowButtonBar = !_narrowButtonBar, 106),
                            BuildToggle(_overflowUp ? "overflow=up" : "overflow=down", () => _overflowUp = !_overflowUp, 118),
                            BuildToggle(_useRtl ? "RTL" : "LTR", () => _useRtl = !_useRtl, 68),
                        ]),
                    new Text($"actionCount={_actionCount}", fontSize: 12, color: Color.Parse("#FF607D8B")),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new SizedBox(
                            width: _narrowButtonBar ? 190 : 520,
                            child: new Container(
                                color: Color.Parse("#FFF7F9FC"),
                                child: new Directionality(
                                    _useRtl ? TextDirection.Rtl : TextDirection.Ltr,
                                    new ButtonBar(
                                        overflowButtonSpacing: 8,
                                        children:
                                        [
                                            BuildAction("CANCEL"),
                                            BuildAction("LATER"),
                                            BuildAction("CONFIRM"),
                                        ]))))),
                    new Expanded(
                        child: new Scaffold(
                            backgroundColor: Color.Parse("#FFF4F6FA"),
                            body: new Center(
                                child: new Text(
                                    "The FAB and notch share Scaffold geometry",
                                    fontSize: 14,
                                    color: Colors.DimGray)),
                            floatingActionButton: new FloatingActionButton(
                                child: new Icon(Icons.Add),
                                onPressed: () => SetState(() => _actionCount += 1)),
                            floatingActionButtonLocation: FloatingActionButtonLocation.CenterDocked,
                            bottomNavigationBar: new BottomAppBar(
                                shape: _showNotch ? new CircularNotchedRectangle() : null,
                                notchMargin: 4,
                                child: new Row(
                                    children:
                                    [
                                        new IconButton(new Icon(Icons.Menu), () => SetState(() => _actionCount += 1)),
                                        new Spacer(),
                                        new IconButton(new Icon(Icons.InfoOutline), () => SetState(() => _actionCount += 1)),
                                    ])))),
                ]));
    }

    private Widget BuildAction(string label) => new TextButton(
        onPressed: () => SetState(() => _actionCount += 1),
        child: new Text(label, fontSize: 12));

    private Widget BuildToggle(string label, Action update, double width) => new SizedBox(
        width: width,
        child: new TextButton(
            onPressed: () => SetState(update),
            minHeight: 36,
            padding: new Thickness(8, 6),
            backgroundColor: Color.Parse("#FFE9F0FF"),
            foregroundColor: Colors.Black,
            borderRadius: BorderRadius.Circular(8),
            child: new Text(label, fontSize: 11)));
}
