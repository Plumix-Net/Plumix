using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/visual_density_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class VisualDensityDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new VisualDensityDemoPageState();
    }
}

internal sealed class VisualDensityDemoPageState : State
{
    private static readonly IReadOnlyList<(string Name, VisualDensity Density)> Profiles =
    [
        ("standard", VisualDensity.Standard),
        ("comfortable", VisualDensity.Comfortable),
        ("compact", VisualDensity.Compact),
        ("(3, 3)", new VisualDensity(horizontal: 3, vertical: 3)),
        ("(-3, -3)", new VisualDensity(horizontal: -3, vertical: -3)),
    ];

    private static readonly IReadOnlyList<TargetPlatform> Platforms =
    [
        TargetPlatform.Android,
        TargetPlatform.IOS,
        TargetPlatform.Fuchsia,
        TargetPlatform.Linux,
        TargetPlatform.MacOS,
        TargetPlatform.Windows,
    ];

    private int _profileIndex;
    private int _platformIndex;

    public override Widget Build(BuildContext context)
    {
        (string name, VisualDensity density) = Profiles[_profileIndex];
        TargetPlatform platform = Platforms[_platformIndex];
        var platformTheme = new ThemeData(platform: platform);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("VisualDensity + platform defaults", fontSize: 20, color: Colors.Black),
                new Text(
                    "Density is unitless: one unit is four logical pixels per axis. ThemeData takes "
                    + "its default from the theme's platform, not the host.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: $"density: {name}",
                            onTap: () => SetState(() =>
                                _profileIndex = (_profileIndex + 1) % Profiles.Count),
                            width: 180,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: $"platform: {platform}",
                            onTap: () => SetState(() =>
                                _platformIndex = (_platformIndex + 1) % Platforms.Count),
                            width: 180,
                            background: Color.Parse("#FFEAF6F7")),
                    ]),
                new Expanded(
                    child: new SingleChildScrollView(
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 14,
                            children:
                            [
                                BuildPlatformDefaults(platform, platformTheme),
                                BuildDensityFacts(density),
                                BuildSizedProbes(density),
                            ]))),
            ]);
    }

    private Widget BuildPlatformDefaults(TargetPlatform platform, ThemeData theme)
    {
        return BuildCard(
            "ThemeData(platform: " + platform + ") defaults",
            [
                BuildFactRow("visualDensity", theme.VisualDensity.ToString()),
                BuildFactRow("materialTapTargetSize", theme.MaterialTapTargetSize.ToString()),
                BuildFactRow(
                    "VisualDensity.DefaultDensityForPlatform",
                    VisualDensity.DefaultDensityForPlatform(platform).ToString()),
            ]);
    }

    private Widget BuildDensityFacts(VisualDensity density)
    {
        Vector adjustment = density.BaseSizeAdjustment;
        BoxConstraints effective = density.EffectiveConstraints(
            BoxConstraints.TightFor(width: 48, height: 48));
        return BuildCard(
            $"{density}",
            [
                BuildFactRow("baseSizeAdjustment", $"({adjustment.X:0.#}, {adjustment.Y:0.#})"),
                BuildFactRow(
                    "effectiveConstraints(48x48)",
                    $"min {effective.MinWidth:0.#}x{effective.MinHeight:0.#}, "
                    + $"max {effective.MaxWidth:0.#}x{effective.MaxHeight:0.#}"),
            ]);
    }

    private Widget BuildSizedProbes(VisualDensity density)
    {
        List<Widget> probes = [];
        foreach ((string name, VisualDensity profile) in Profiles)
        {
            bool selected = profile == density;
            probes.Add(new Padding(
                insets: new Thickness(0, 0, 0, 8),
                child: new Row(
                    spacing: 10,
                    children:
                    [
                        new SizedBox(
                            width: 110,
                            child: new Text(
                                name,
                                fontSize: 12,
                                color: selected ? Colors.Black : Color.Parse("#FF607D8B"))),
                        new Theme(
                            data: ThemeData.Light with { VisualDensity = profile },
                            child: new ElevatedButton(
                                onPressed: () => { },
                                child: new Text("Button", fontSize: 12))),
                    ])));
        }

        return BuildCard("The same button under each density", probes);
    }

    private Widget BuildCard(string title, IReadOnlyList<Widget> children)
    {
        List<Widget> rows = [new Text(title, fontSize: 14, color: Colors.Black)];
        foreach (Widget child in children)
        {
            rows.Add(child);
        }

        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children: rows));
    }

    private Widget BuildFactRow(string label, string value)
    {
        return new Row(
            spacing: 10,
            children:
            [
                new Expanded(child: new Text(label, fontSize: 12, color: Colors.Black)),
                new Text(value, fontSize: 12, color: Color.Parse("#FF607D8B")),
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
