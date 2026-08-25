using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/theme_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CupertinoThemeDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CupertinoThemeDemoPageState();
    }
}

internal sealed class CupertinoThemeDemoPageState : State
{
    private static readonly (string Name, CupertinoDynamicColor Color)[] Swatches =
    [
        ("systemBlue", CupertinoColors.SystemBlue),
        ("systemRed", CupertinoColors.SystemRed),
        ("systemGreen", CupertinoColors.SystemGreen),
        ("systemIndigo", CupertinoColors.SystemIndigo),
        ("label", CupertinoColors.Label),
        ("secondaryLabel", CupertinoColors.SecondaryLabel),
        ("separator", CupertinoColors.Separator),
        ("systemFill", CupertinoColors.SystemFill),
        ("systemBackground", CupertinoColors.SystemBackground),
        ("secondarySystemBackground", CupertinoColors.SecondarySystemBackground),
    ];

    private bool _dark;
    private bool _highContrast;
    private bool _elevated;
    private bool _cupertinoOverride;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("CupertinoTheme + dynamic colors", fontSize: 20, color: Colors.Black),
                new Text(
                    "CupertinoDynamicColor resolves against brightness, accessibility contrast and interface elevation.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _dark ? "Brightness: dark" : "Brightness: light",
                            onTap: ToggleBrightness,
                            width: 168,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: _highContrast ? "Contrast: high" : "Contrast: normal",
                            onTap: ToggleHighContrast,
                            width: 160,
                            background: Color.Parse("#FFEAE4FF")),
                        BuildControlButton(
                            label: _elevated ? "Level: elevated" : "Level: base",
                            onTap: ToggleElevation,
                            width: 148,
                            background: Color.Parse("#FFE8F4E8")),
                    ]),
                new Text(
                    $"brightness={(_dark ? "dark" : "light")}, highContrast={(_highContrast ? "true" : "false")}, level={(_elevated ? "elevated" : "base")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                BuildProbe(context),
                new Text("MaterialBasedCupertinoThemeData", fontSize: 20, color: Colors.Black),
                new Text(
                    "A Material Theme installs a CupertinoTheme that defers to the Material ThemeData; "
                    + "ThemeData.cupertinoOverrideTheme preempts individual attributes.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                BuildControlButton(
                    label: _cupertinoOverride ? "Override: systemPink" : "Override: none (cascaded)",
                    onTap: ToggleCupertinoOverride,
                    width: 232,
                    background: Color.Parse("#FFFDE7F3")),
                BuildBridgeProbe(),
            ]);
    }

    private Widget BuildBridgeProbe()
    {
        NoDefaultCupertinoThemeData? overrideTheme = _cupertinoOverride
            ? new CupertinoThemeData(primaryColor: CupertinoColors.SystemPink)
            : null;
        return new Row(
            spacing: 8,
            children:
            [
                new Expanded(BuildBridgeCard(
                    "ThemeData.light()",
                    new ThemeData(
                        colorScheme: ColorScheme.Light(primary: Color.Parse("#FF2E7D32")),
                        cupertinoOverrideTheme: overrideTheme))),
                new Expanded(BuildBridgeCard(
                    "ThemeData.dark()",
                    new ThemeData(
                        colorScheme: ColorScheme.Dark(primary: Color.Parse("#FF80CBC4")),
                        cupertinoOverrideTheme: overrideTheme))),
            ]);
    }

    private static Widget BuildBridgeCard(string label, ThemeData data)
    {
        return new Theme(
            data,
            new Builder(context =>
            {
                CupertinoThemeData theme = CupertinoTheme.Of(context);
                return new Container(
                    padding: new Thickness(12),
                    decoration: new BoxDecoration(
                        Color: theme.ScaffoldBackgroundColor,
                        BorderRadius: BorderRadius.Circular(12),
                        Border: Border.FromBorderSide(new BorderSide(Color.Parse("#33000000"), 1))),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 6,
                        children:
                        [
                            new Text(
                                label,
                                style: theme.TextTheme.NavTitleTextStyle.CopyWith(fontSize: 14)),
                            new Text(
                                $"brightness: {CupertinoTheme.BrightnessOf(context)}",
                                style: theme.TextTheme.TextStyle.CopyWith(fontSize: 12)),
                            new Text(
                                "actionTextStyle follows primaryColor",
                                style: theme.TextTheme.ActionTextStyle.CopyWith(fontSize: 12)),
                            new CupertinoSwitch(value: true, onChanged: _ => { }),
                        ]));
            }));
    }

    private Widget BuildProbe(BuildContext context)
    {
        return new MediaQuery(
            MediaQuery.Of(context) with
            {
                PlatformBrightness = _dark ? PlatformBrightness.Dark : PlatformBrightness.Light,
                HighContrast = _highContrast,
            },
            new CupertinoUserInterfaceLevel(
                _elevated ? CupertinoUserInterfaceLevelData.Elevated : CupertinoUserInterfaceLevelData.Base,
                new CupertinoTheme(
                    // No explicit brightness: the theme defers to the MediaQuery above.
                    new CupertinoThemeData(),
                    new Builder(BuildResolvedTheme))));
    }

    private static Widget BuildResolvedTheme(BuildContext context)
    {
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        Color labelColor = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color separator = CupertinoDynamicColor.Resolve(CupertinoColors.Separator, context);

        return new Container(
            padding: new Thickness(12),
            decoration: new BoxDecoration(
                Color: theme.ScaffoldBackgroundColor,
                BorderRadius: BorderRadius.Circular(12),
                Border: Border.FromBorderSide(new BorderSide(separator, 1))),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text("navTitleTextStyle", style: theme.TextTheme.NavTitleTextStyle),
                    new Text("textStyle — body copy", style: theme.TextTheme.TextStyle),
                    new Text("actionTextStyle — primaryColor", style: theme.TextTheme.ActionTextStyle),
                    new Text("TABLABELTEXTSTYLE", style: theme.TextTheme.TabLabelTextStyle),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            .. Swatches.Select(swatch =>
                                BuildSwatch(context, swatch.Name, swatch.Color, labelColor, separator)),
                        ]),
                ]));
    }

    private static Widget BuildSwatch(
        BuildContext context,
        string name,
        CupertinoDynamicColor color,
        Color labelColor,
        Color separator)
    {
        return new SizedBox(
            width: 150,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 4,
                children:
                [
                    new Container(
                        height: 28,
                        decoration: new BoxDecoration(
                            Color: CupertinoDynamicColor.Resolve(color, context),
                            BorderRadius: BorderRadius.Circular(6),
                            Border: Border.FromBorderSide(new BorderSide(separator, 1)))),
                    new Text(name, fontSize: 11, color: labelColor),
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
                child: new Text(
                    label,
                    fontSize: 12)));
    }

    private void ToggleBrightness()
    {
        SetState(() => _dark = !_dark);
    }

    private void ToggleHighContrast()
    {
        SetState(() => _highContrast = !_highContrast);
    }

    private void ToggleElevation()
    {
        SetState(() => _elevated = !_elevated);
    }

    private void ToggleCupertinoOverride()
    {
        SetState(() => _cupertinoOverride = !_cupertinoOverride);
    }
}
