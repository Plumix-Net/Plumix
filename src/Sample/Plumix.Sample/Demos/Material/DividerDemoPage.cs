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
                    Color: new Color(0xFF00695C),
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
                children:
                [
                    new Text("Divider baseline", style: new TextStyle(FontSize: 20)),
                    new SizedBox(height: 8),
                    new Text(
                        "M2/M3 tokens, directional indents, asymmetric theme radii, and widget overrides.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new SizedBox(height: 10),
                    new Row(
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: new Color(0xFFE9F0FF)),
                            new SizedBox(width: 8),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: new Color(0xFFEAF6F7)),
                            new SizedBox(width: 8),
                            BuildControlButton(
                                label: _useWidgetOverrides ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetOverrides = !_useWidgetOverrides),
                                width: 118,
                                background: new Color(0xFFF0E8FF)),
                        ]),
                    new SizedBox(height: 8),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, "
                        + $"theme={(_useThemeOverrides ? "true" : "false")}, "
                        + $"widget={(_useWidgetOverrides ? "true" : "false")}",
                        style: new TextStyle(FontSize: 12, Color: new Color(0xFF607D8B))),
                    new SizedBox(height: 10),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                children:
                                [
                                    BuildHorizontalPreview(),
                                    new SizedBox(height: 14),
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
                color: new Color(0xFF1565C0),
                radius: BorderRadius.Circular(3));
        }
        else
        {
            dividerWidget = new Divider();
        }

        return new Container(
            color: new Color(0xFFF7F9FC),
            padding: EdgeInsets.All(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Text("Horizontal Divider", style: new TextStyle(FontSize: 14, Color: MaterialColors.Black)),
                    new SizedBox(height: 8),
                    new Container(
                        color: new Color(0xFFE3F2FD),
                        padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 6),
                        child: new Text(
                            "Before divider",
                            style: new TextStyle(
                                FontSize: 12,
                                Color: new Color(0xFF0D47A1)))),
                    dividerWidget,
                    new Container(
                        color: new Color(0xFFE8F5E9),
                        padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 6),
                        child: new Text(
                            "After divider",
                            style: new TextStyle(
                                FontSize: 12,
                                Color: new Color(0xFF1B5E20)))),
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
                color: new Color(0xFF6A1B9A),
                radius: BorderRadius.Circular(3));
        }
        else
        {
            dividerWidget = new VerticalDivider();
        }

        return new Container(
            color: new Color(0xFFF7F9FC),
            padding: EdgeInsets.All(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Text("Vertical Divider", style: new TextStyle(FontSize: 14, Color: MaterialColors.Black)),
                    new SizedBox(height: 8),
                    new SizedBox(
                        height: 96,
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(
                                    child: new Container(
                                        color: new Color(0xFFFFF8E1),
                                        alignment: Alignment.Center,
                                        child: new Text(
                                            "Start",
                                            style: new TextStyle(
                                                FontSize: 12,
                                                Color: new Color(0xFF5D4037))))),
                                dividerWidget,
                                new Expanded(
                                    child: new Container(
                                        color: new Color(0xFFFCE4EC),
                                        alignment: Alignment.Center,
                                        child: new Text(
                                            "End",
                                            style: new TextStyle(
                                                FontSize: 12,
                                                Color: new Color(0xFF880E4F))))),
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
                style: TextButton.StyleFrom(
                    backgroundColor: background,
                    foregroundColor: MaterialColors.Black,
                    minimumSize: new Size(0, 36),
                    padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 8),
                    shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(8))),
                child: new Text(label, style: new TextStyle(FontSize: 12))));
    }
}
