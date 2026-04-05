using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/app_bar_icon_theme_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class AppBarIconThemeDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AppBarIconThemeDemoPageState();
    }
}

internal sealed class AppBarIconThemeDemoPageState : State
{
    private static readonly Color AppBarBackground = Color.Parse("#FF1E3A5F");
    private static readonly Color ForegroundWhite = Colors.White;
    private static readonly Color ForegroundMint = Color.Parse("#FFB8FFF1");
    private static readonly Color ThemeIconColor = Color.Parse("#FF2A9D8F");
    private static readonly Color ThemeActionsIconColor = Color.Parse("#FFF4A261");
    private static readonly Color WidgetIconColor = Color.Parse("#FF8E44AD");
    private static readonly Color WidgetActionsIconColor = Color.Parse("#FFE63946");

    private Color _foregroundColor = ForegroundWhite;
    private bool _themeIconEnabled = true;
    private bool _themeActionsIconEnabled;
    private bool _widgetIconOverrideEnabled;
    private bool _widgetActionsIconOverrideEnabled;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AppBar iconTheme/actionsIconTheme", fontSize: 20, color: Colors.Black),
                new Text(
                    "Runtime probe for icon-theme precedence: widget override > appBarTheme > iconTheme fallback > foreground fallback.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("fg white", () => SetForeground(ForegroundWhite), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("fg mint", () => SetForeground(ForegroundMint), width: 92, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("theme icon off", () => SetThemeIconEnabled(false), width: 108, colorHex: "#FFE9F5EC"),
                        BuildButton("theme icon on", () => SetThemeIconEnabled(true), width: 108, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("theme actions off", () => SetThemeActionsIconEnabled(false), width: 118, colorHex: "#FFF3E8D8"),
                        BuildButton("theme actions on", () => SetThemeActionsIconEnabled(true), width: 118, colorHex: "#FFF3E8D8"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("widget icon off", () => SetWidgetIconOverrideEnabled(false), width: 114, colorHex: "#FFE4ECF7"),
                        BuildButton("widget icon on", () => SetWidgetIconOverrideEnabled(true), width: 114, colorHex: "#FFE4ECF7"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("widget actions off", () => SetWidgetActionsIconOverrideEnabled(false), width: 126, colorHex: "#FFF5E8ED"),
                        BuildButton("widget actions on", () => SetWidgetActionsIconOverrideEnabled(true), width: 126, colorHex: "#FFF5E8ED"),
                    ]),
                new Text(
                    $"fg={ColorLabel(_foregroundColor)}, themeIcon={OnOff(_themeIconEnabled)}, themeActions={OnOff(_themeActionsIconEnabled)}, widgetIcon={OnOff(_widgetIconOverrideEnabled)}, widgetActions={OnOff(_widgetActionsIconOverrideEnabled)}, expectedLeading={ColorLabel(ResolveExpectedLeadingColor())}, expectedActions={ColorLabel(ResolveExpectedActionsColor())}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 320,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Themed app bar preview", fontSize: 11, color: Colors.DimGray),
                            BuildThemedPreview(),
                            new Text("Default app bar reference (no icon theme overrides)", fontSize: 11, color: Colors.DimGray),
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
                    BackgroundColor: AppBarBackground,
                    ForegroundColor: _foregroundColor,
                    IconTheme: _themeIconEnabled ? new IconThemeData(Color: ThemeIconColor, Size: 18) : null,
                    ActionsIconTheme: _themeActionsIconEnabled ? new IconThemeData(Color: ThemeActionsIconColor, Size: 16) : null),
            },
            child: new AppBar(
                titleText: "Theme icon chain",
                iconTheme: _widgetIconOverrideEnabled ? new IconThemeData(Color: WidgetIconColor, Size: 20) : null,
                actionsIconTheme: _widgetActionsIconOverrideEnabled ? new IconThemeData(Color: WidgetActionsIconColor, Size: 22) : null,
                leading: new Icon(Icons.Menu),
                actions:
                [
                    new Icon(Icons.Close),
                    new Icon(Icons.InfoOutline),
                ]));
    }

    private Widget BuildDefaultReferencePreview()
    {
        return new AppBar(
            titleText: "Default icon chain",
            backgroundColor: AppBarBackground,
            foregroundColor: _foregroundColor,
            leading: new Icon(Icons.Menu),
            actions:
            [
                new Icon(Icons.Close),
                new Icon(Icons.InfoOutline),
            ]);
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

    private void SetForeground(Color value)
    {
        SetState(() => _foregroundColor = value);
    }

    private void SetThemeIconEnabled(bool value)
    {
        SetState(() => _themeIconEnabled = value);
    }

    private void SetThemeActionsIconEnabled(bool value)
    {
        SetState(() => _themeActionsIconEnabled = value);
    }

    private void SetWidgetIconOverrideEnabled(bool value)
    {
        SetState(() => _widgetIconOverrideEnabled = value);
    }

    private void SetWidgetActionsIconOverrideEnabled(bool value)
    {
        SetState(() => _widgetActionsIconOverrideEnabled = value);
    }

    private Color ResolveExpectedLeadingColor()
    {
        if (_widgetIconOverrideEnabled)
        {
            return WidgetIconColor;
        }

        if (_themeIconEnabled)
        {
            return ThemeIconColor;
        }

        return _foregroundColor;
    }

    private Color ResolveExpectedActionsColor()
    {
        if (_widgetActionsIconOverrideEnabled)
        {
            return WidgetActionsIconColor;
        }

        if (_themeActionsIconEnabled)
        {
            return ThemeActionsIconColor;
        }

        if (_widgetIconOverrideEnabled)
        {
            return WidgetIconColor;
        }

        if (_themeIconEnabled)
        {
            return ThemeIconColor;
        }

        return _foregroundColor;
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private static string ColorLabel(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
