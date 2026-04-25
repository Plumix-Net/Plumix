using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/app_bar_actions_padding_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class AppBarActionsPaddingDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AppBarActionsPaddingDemoPageState();
    }
}

internal sealed class AppBarActionsPaddingDemoPageState : State
{
    private Thickness _themeActionsPadding = new(6, 2, 6, 2);
    private bool _useWidgetOverride;
    private Thickness _widgetActionsPadding = new(10, 4, 10, 4);

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AppBar theme actionsPadding", fontSize: 20, color: Colors.Black),
                new Text(
                    "Runtime probe for actions row padding resolution: widget override beats appBarTheme, appBarTheme beats default zero padding.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("theme 0", () => SetThemeActionsPadding(new Thickness()), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("theme 6/2", () => SetThemeActionsPadding(new Thickness(6, 2, 6, 2)), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("theme 14/6", () => SetThemeActionsPadding(new Thickness(14, 6, 14, 6)), width: 102, colorHex: "#FFDCE3ED"),
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
                        BuildButton("widget 4/0", () => SetWidgetActionsPadding(new Thickness(4, 0, 4, 0)), width: 92, colorHex: "#FFF3E8D8"),
                        BuildButton("widget 10/4", () => SetWidgetActionsPadding(new Thickness(10, 4, 10, 4)), width: 92, colorHex: "#FFF3E8D8"),
                        BuildButton("widget 16/8", () => SetWidgetActionsPadding(new Thickness(16, 8, 16, 8)), width: 102, colorHex: "#FFF3E8D8"),
                    ]),
                new Text(
                    $"theme={FormatInsets(_themeActionsPadding)}, override={(_useWidgetOverride ? "on" : "off")}, widget={FormatInsets(_widgetActionsPadding)}, effective={FormatInsets(EffectiveActionsPadding())}",
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
                            new Text("Default app bar reference (actionsPadding = 0)", fontSize: 11, color: Colors.DimGray),
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
                    ActionsPadding: _themeActionsPadding),
            },
            child: new AppBar(
                titleText: "Theme actions padding",
                actionsPadding: _useWidgetOverride ? _widgetActionsPadding : null,
                actions:
                [
                    BuildActionBadge(Icons.Close),
                    BuildActionBadge(Icons.Menu),
                ]));
    }

    private Widget BuildDefaultReferencePreview()
    {
        return new AppBar(
            titleText: "Default actions padding",
            backgroundColor: Color.Parse("#FF1E3A5F"),
            foregroundColor: Colors.White,
            actions:
            [
                BuildActionBadge(Icons.Close),
                BuildActionBadge(Icons.Menu),
            ]);
    }

    private static Widget BuildActionBadge(IconData icon)
    {
        return new Container(
            width: 28,
            height: 22,
            color: Color.Parse("#FFFFB703"),
            child: new Center(
                child: new Icon(
                    icon,
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

    private void SetThemeActionsPadding(Thickness value)
    {
        SetState(() => _themeActionsPadding = value);
    }

    private void SetUseWidgetOverride(bool value)
    {
        SetState(() => _useWidgetOverride = value);
    }

    private void SetWidgetActionsPadding(Thickness value)
    {
        SetState(() => _widgetActionsPadding = value);
    }

    private Thickness EffectiveActionsPadding()
    {
        return _useWidgetOverride ? _widgetActionsPadding : _themeActionsPadding;
    }

    private static string FormatInsets(Thickness insets)
    {
        return $"{insets.Left:0}/{insets.Top:0}/{insets.Right:0}/{insets.Bottom:0}";
    }
}
