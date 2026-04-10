using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/app_bar_text_styles_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class AppBarTextStylesDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AppBarTextStylesDemoPageState();
    }
}

internal sealed class AppBarTextStylesDemoPageState : State
{
    private static readonly Color AppBarBackground = Color.Parse("#FF1E3A5F");
    private static readonly Color ForegroundWhite = Colors.White;
    private static readonly Color ForegroundMint = Color.Parse("#FFB8FFF1");
    private static readonly TextStyle ThemeTitleStyle = new(
        FontSize: 22,
        Color: Color.Parse("#FF90E0EF"),
        FontWeight: FontWeight.Bold);
    private static readonly TextStyle ThemeToolbarStyle = new(
        FontSize: 14,
        Color: Color.Parse("#FFF4A261"),
        FontWeight: FontWeight.SemiBold);
    private static readonly TextStyle WidgetTitleStyle = new(
        FontSize: 19,
        Color: Color.Parse("#FF8E44AD"),
        FontWeight: FontWeight.Normal);
    private static readonly TextStyle WidgetToolbarStyle = new(
        FontSize: 16,
        Color: Color.Parse("#FFE63946"),
        FontWeight: FontWeight.Bold);

    private Color _foregroundColor = ForegroundWhite;
    private bool _themeTitleStyleEnabled = true;
    private bool _themeToolbarStyleEnabled = true;
    private bool _widgetTitleOverrideEnabled;
    private bool _widgetToolbarOverrideEnabled;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AppBar title/toolbar text styles", fontSize: 20, color: Colors.Black),
                new Text(
                    "Runtime probe for titleTextStyle and toolbarTextStyle precedence: widget override > appBarTheme > foreground fallback.",
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
                        BuildButton("theme title off", () => SetThemeTitleStyleEnabled(false), width: 114, colorHex: "#FFE9F5EC"),
                        BuildButton("theme title on", () => SetThemeTitleStyleEnabled(true), width: 114, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("theme toolbar off", () => SetThemeToolbarStyleEnabled(false), width: 126, colorHex: "#FFF3E8D8"),
                        BuildButton("theme toolbar on", () => SetThemeToolbarStyleEnabled(true), width: 126, colorHex: "#FFF3E8D8"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("widget title off", () => SetWidgetTitleOverrideEnabled(false), width: 118, colorHex: "#FFE4ECF7"),
                        BuildButton("widget title on", () => SetWidgetTitleOverrideEnabled(true), width: 118, colorHex: "#FFE4ECF7"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("widget toolbar off", () => SetWidgetToolbarOverrideEnabled(false), width: 130, colorHex: "#FFF5E8ED"),
                        BuildButton("widget toolbar on", () => SetWidgetToolbarOverrideEnabled(true), width: 130, colorHex: "#FFF5E8ED"),
                    ]),
                new Text(
                    $"fg={ColorLabel(_foregroundColor)}, themeTitle={OnOff(_themeTitleStyleEnabled)}, themeToolbar={OnOff(_themeToolbarStyleEnabled)}, widgetTitle={OnOff(_widgetTitleOverrideEnabled)}, widgetToolbar={OnOff(_widgetToolbarOverrideEnabled)}, expectedTitle={DescribeStyle(ResolveExpectedTitleStyle(), _foregroundColor)}, expectedActions={DescribeStyle(ResolveExpectedToolbarStyle(), _foregroundColor)}",
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
                            new Text("Default app bar reference (no text style overrides)", fontSize: 11, color: Colors.DimGray),
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
                    TitleTextStyle: _themeTitleStyleEnabled ? ThemeTitleStyle : null,
                    ToolbarTextStyle: _themeToolbarStyleEnabled ? ThemeToolbarStyle : null),
            },
            child: new AppBar(
                titleText: "Theme text styles",
                titleTextStyle: _widgetTitleOverrideEnabled ? WidgetTitleStyle : null,
                toolbarTextStyle: _widgetToolbarOverrideEnabled ? WidgetToolbarStyle : null,
                actions:
                [
                    new Text("A1"),
                    new Text("A2"),
                ]));
    }

    private Widget BuildDefaultReferencePreview()
    {
        return new AppBar(
            titleText: "Default text styles",
            backgroundColor: AppBarBackground,
            foregroundColor: _foregroundColor,
            actions:
            [
                new Text("A1"),
                new Text("A2"),
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

    private void SetThemeTitleStyleEnabled(bool value)
    {
        SetState(() => _themeTitleStyleEnabled = value);
    }

    private void SetThemeToolbarStyleEnabled(bool value)
    {
        SetState(() => _themeToolbarStyleEnabled = value);
    }

    private void SetWidgetTitleOverrideEnabled(bool value)
    {
        SetState(() => _widgetTitleOverrideEnabled = value);
    }

    private void SetWidgetToolbarOverrideEnabled(bool value)
    {
        SetState(() => _widgetToolbarOverrideEnabled = value);
    }

    private TextStyle? ResolveExpectedTitleStyle()
    {
        if (_widgetTitleOverrideEnabled)
        {
            return WidgetTitleStyle;
        }

        if (_themeTitleStyleEnabled)
        {
            return ThemeTitleStyle;
        }

        return null;
    }

    private TextStyle? ResolveExpectedToolbarStyle()
    {
        if (_widgetToolbarOverrideEnabled)
        {
            return WidgetToolbarStyle;
        }

        if (_themeToolbarStyleEnabled)
        {
            return ThemeToolbarStyle;
        }

        return null;
    }

    private static string DescribeStyle(TextStyle? style, Color fallbackColor)
    {
        var color = style?.Color ?? fallbackColor;
        var fontSize = style?.FontSize;
        var sizeLabel = fontSize.HasValue ? fontSize.Value.ToString("0") : "base";
        return $"{ColorLabel(color)}:{sizeLabel}";
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private static string ColorLabel(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
