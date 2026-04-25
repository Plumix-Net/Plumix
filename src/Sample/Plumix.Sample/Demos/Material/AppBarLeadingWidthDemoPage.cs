using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/app_bar_leading_width_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class AppBarLeadingWidthDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AppBarLeadingWidthDemoPageState();
    }
}

internal sealed class AppBarLeadingWidthDemoPageState : State
{
    private double _themeLeadingWidth = 88;
    private bool _useWidgetOverride;
    private double _widgetLeadingWidth = 120;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AppBar theme leadingWidth", fontSize: 20, color: Colors.Black),
                new Text(
                    "Runtime probe for AppBar leading slot width resolution: widget override beats appBarTheme, appBarTheme beats default 56.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("theme 56", () => SetThemeLeadingWidth(56), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("theme 88", () => SetThemeLeadingWidth(88), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("theme 120", () => SetThemeLeadingWidth(120), width: 102, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("override off", () => SetUseWidgetOverride(false), width: 108, colorHex: "#FFE9F5EC"),
                        BuildButton("override on", () => SetUseWidgetOverride(true), width: 108, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("widget 72", () => SetWidgetLeadingWidth(72), width: 92, colorHex: "#FFF3E8D8"),
                        BuildButton("widget 96", () => SetWidgetLeadingWidth(96), width: 92, colorHex: "#FFF3E8D8"),
                        BuildButton("widget 120", () => SetWidgetLeadingWidth(120), width: 102, colorHex: "#FFF3E8D8"),
                    ]),
                new Text(
                    $"themeLeadingWidth={_themeLeadingWidth:0}, override={(_useWidgetOverride ? "on" : "off")}, widgetLeadingWidth={_widgetLeadingWidth:0}, effective={EffectiveLeadingWidthLabel()}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 300,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Themed app bar preview", fontSize: 11, color: Colors.DimGray),
                            BuildThemedPreview(),
                            new Text("Default app bar reference (leadingWidth = 56)", fontSize: 11, color: Colors.DimGray),
                            BuildDefaultReferencePreview(),
                        ])),
            ]);
    }

    private Widget BuildThemedPreview()
    {
        return new Theme(
            data: ThemeData.Light with
            {
                AppBarTheme = new AppBarThemeData(
                    BackgroundColor: Color.Parse("#FF1E3A5F"),
                    ForegroundColor: Colors.White,
                    LeadingWidth: _themeLeadingWidth),
            },
            child: new AppBar(
                titleText: "Theme leading width",
                leadingWidth: _useWidgetOverride ? _widgetLeadingWidth : null,
                leading: BuildLeadingProbe(),
                actions:
                [
                    new Icon(Icons.Close),
                ]));
    }

    private Widget BuildDefaultReferencePreview()
    {
        return new AppBar(
            titleText: "Default leading width",
            backgroundColor: Color.Parse("#FF1E3A5F"),
            foregroundColor: Colors.White,
            leading: BuildLeadingProbe(),
            actions:
            [
                new Icon(Icons.Close),
            ]);
    }

    private static Widget BuildLeadingProbe()
    {
        return new Container(
            width: 24,
            height: 24,
            color: Color.Parse("#FFFFB703"),
            child: new Center(
                child: new Icon(
                    Icons.Menu,
                    size: 14,
                    color: Colors.Black)));
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private void SetThemeLeadingWidth(double value)
    {
        SetState(() => _themeLeadingWidth = value);
    }

    private void SetUseWidgetOverride(bool value)
    {
        SetState(() => _useWidgetOverride = value);
    }

    private void SetWidgetLeadingWidth(double value)
    {
        SetState(() => _widgetLeadingWidth = value);
    }

    private string EffectiveLeadingWidthLabel()
    {
        return (_useWidgetOverride ? _widgetLeadingWidth : _themeLeadingWidth).ToString("0");
    }
}
