using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/color_palette_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ColorPaletteDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ColorPaletteDemoPageState();
    }
}

internal sealed class ColorPaletteDemoPageState : State
{
    private static readonly IReadOnlyList<(string Name, MaterialColor Swatch)> Swatches =
    [
        ("blue", MaterialColors.Blue),
        ("green", MaterialColors.Green),
        ("deepOrange", MaterialColors.DeepOrange),
        ("grey", MaterialColors.Grey),
    ];

    private static readonly int[] Shades = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900];

    private int _swatchIndex;
    private bool _useMaterial3;

    public override Widget Build(BuildContext context)
    {
        (string name, MaterialColor swatch) = Swatches[_swatchIndex];
        var pageTheme = new ThemeData(
            useMaterial3: _useMaterial3,
            primarySwatch: swatch);

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Colors + primarySwatch", fontSize: 20, color: Colors.Black),
                    new Text(
                        "MaterialColor shades, ColorScheme.fromSwatch and the primarySwatch-derived theme colors.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: $"swatch: {name}",
                                onTap: () => SetState(() =>
                                    _swatchIndex = (_swatchIndex + 1) % Swatches.Count),
                                width: 150,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: Color.Parse("#FFEAF6F7")),
                        ]),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildShadeStrip(name, swatch),
                                    BuildThemeProbe(pageTheme),
                                ]))),
                ]));
    }

    private Widget BuildShadeStrip(string name, MaterialColor swatch)
    {
        List<Widget> tiles = [];
        foreach (int shade in Shades)
        {
            Color color = swatch[shade]!.Value;
            tiles.Add(new Expanded(
                child: new Container(
                    height: 56,
                    color: color,
                    alignment: Alignment.Center,
                    child: new Text(
                        shade.ToString(),
                        fontSize: 11,
                        color: ThemeData.EstimateBrightnessForColor(color) == Brightness.Dark
                            ? MaterialColors.White
                            : MaterialColors.Black))));
        }

        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text($"Colors.{name} shades", fontSize: 14, color: Colors.Black),
                    new Row(children: tiles),
                ]));
    }

    private Widget BuildThemeProbe(ThemeData theme)
    {
        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text("Theme colors", fontSize: 14, color: Colors.Black),
                    BuildSwatchRow("colorScheme.primary", theme.ColorScheme.Primary),
                    BuildSwatchRow("colorScheme.secondary", theme.ColorScheme.Secondary),
                    BuildSwatchRow("primaryColor", theme.PrimaryColor),
                    BuildSwatchRow("primaryColorLight", theme.PrimaryColorLight),
                    BuildSwatchRow("primaryColorDark", theme.PrimaryColorDark),
                    BuildSwatchRow("canvasColor", theme.CanvasColor),
                    BuildSwatchRow("cardColor", theme.CardColor),
                ]));
    }

    private Widget BuildSwatchRow(string label, Color color)
    {
        return new Row(
            spacing: 10,
            children:
            [
                new Container(width: 28, height: 20, color: color),
                new Expanded(child: new Text(label, fontSize: 12, color: Colors.Black)),
                new Text(
                    $"#{color.ToUInt32():X8}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
            ]);
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
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }
}
