using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/divider_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class DividerDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new DividerDemoPageState();
    }
}

internal sealed class DividerDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _useThemeOverrides;
    private bool _useWidgetOverrides;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var pageTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            DividerTheme = _useThemeOverrides
                ? new DividerThemeData(
                    Color: Color.Parse("#FF00695C"),
                    Space: 28,
                    Thickness: 3,
                    Indent: 24,
                    EndIndent: 12,
                    Radius: BorderRadius.Only(
                        topLeft: 1,
                        topRight: 4,
                        bottomRight: 2,
                        bottomLeft: 6))
                : new DividerThemeData()
        };

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Divider baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "M2/M3 tokens, directional indents, asymmetric theme radii, and widget overrides.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: Color.Parse("#FFEAF6F7")),
                            BuildControlButton(
                                label: _useWidgetOverrides ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetOverrides = !_useWidgetOverrides),
                                width: 118,
                                background: Color.Parse("#FFF0E8FF")),
                        ]),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, widget={(_useWidgetOverrides ? "true" : "false")}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildHorizontalPreview(),
                                    BuildVerticalPreview(),
                                ]))),
                ]));
    }

    private Widget BuildHorizontalPreview()
    {
        Divider dividerWidget;
        if (_useWidgetOverrides)
        {
            dividerWidget = new Divider(
                height: 30,
                thickness: 5,
                indent: 18,
                endIndent: 30,
                color: Color.Parse("#FF1565C0"),
                radius: BorderRadius.Circular(3));
        }
        else
        {
            dividerWidget = new Divider();
        }

        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text("Horizontal Divider", fontSize: 14, color: Colors.Black),
                    new Container(
                        color: Color.Parse("#FFE3F2FD"),
                        padding: new Thickness(10, 6),
                        child: new Text("Before divider", fontSize: 12, color: Color.Parse("#FF0D47A1"))),
                    dividerWidget,
                    new Container(
                        color: Color.Parse("#FFE8F5E9"),
                        padding: new Thickness(10, 6),
                        child: new Text("After divider", fontSize: 12, color: Color.Parse("#FF1B5E20"))),
                ]));
    }

    private Widget BuildVerticalPreview()
    {
        VerticalDivider dividerWidget;
        if (_useWidgetOverrides)
        {
            dividerWidget = new VerticalDivider(
                width: 30,
                thickness: 5,
                indent: 12,
                endIndent: 20,
                color: Color.Parse("#FF6A1B9A"),
                radius: BorderRadius.Circular(3));
        }
        else
        {
            dividerWidget = new VerticalDivider();
        }

        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text("Vertical Divider", fontSize: 14, color: Colors.Black),
                    new SizedBox(
                        height: 96,
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(
                                    child: new Container(
                                        color: Color.Parse("#FFFFF8E1"),
                                        alignment: Alignment.Center,
                                        child: new Text("Start", fontSize: 12, color: Color.Parse("#FF5D4037")))),
                                dividerWidget,
                                new Expanded(
                                    child: new Container(
                                        color: Color.Parse("#FFFCE4EC"),
                                        alignment: Alignment.Center,
                                        child: new Text("End", fontSize: 12, color: Color.Parse("#FF880E4F")))),
                            ])),
                ]));
    }

    private Widget BuildControlButton(
        string label,
        Action onTap,
        double width,
        Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(label, fontSize: 12)));
    }
}
