using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/material_switch_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class MaterialSwitchDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new MaterialSwitchDemoPageState();
    }
}

internal sealed class MaterialSwitchDemoPageState : State
{
    private static readonly TargetPlatform[] Platforms =
    [
        TargetPlatform.Android,
        TargetPlatform.IOS,
        TargetPlatform.MacOS,
    ];

    private bool _useMaterial3 = true;
    private int _platformIndex;
    private bool _plain = true;
    private bool _colored;
    private bool _iconed = true;
    private bool _outlined;
    private bool _adaptive;

    public override Widget Build(BuildContext context)
    {
        ThemeData baseTheme = Theme.Of(context);
        TargetPlatform platform = Platforms[_platformIndex];
        ThemeData pageTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            Platform = platform
        };

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Switch", fontSize: 20, color: Colors.Black),
                    new Text(
                        "M2/M3 tokens, thumb icons, track outlines and Switch.adaptive per platform.",
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
                                label: platform.ToString(),
                                onTap: () => SetState(() =>
                                    _platformIndex = (_platformIndex + 1) % Platforms.Length),
                                width: 112,
                                background: Color.Parse("#FFEAF6F7")),
                        ]),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, platform={platform}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildRow(
                                        "Plain",
                                        new Switch(
                                            value: _plain,
                                            onChanged: value => SetState(() => _plain = value))),
                                    BuildRow(
                                        "Custom colors",
                                        new Switch(
                                            value: _colored,
                                            onChanged: value => SetState(() => _colored = value),
                                            activeThumbColor: Color.Parse("#FFFFF8E1"),
                                            activeTrackColor: Color.Parse("#FF00695C"),
                                            inactiveThumbColor: Color.Parse("#FF8D6E63"),
                                            inactiveTrackColor: Color.Parse("#FFD7CCC8"))),
                                    BuildRow(
                                        "Thumb icons",
                                        new Switch(
                                            value: _iconed,
                                            onChanged: value => SetState(() => _iconed = value),
                                            thumbIcon: MaterialStateProperty<Icon?>.ResolveWith(
                                                states => states.HasFlag(MaterialState.Selected)
                                                    ? new Icon(Icons.Check)
                                                    : new Icon(Icons.Close)))),
                                    BuildRow(
                                        "Track outline",
                                        new Switch(
                                            value: _outlined,
                                            onChanged: value => SetState(() => _outlined = value),
                                            trackOutlineColor:
                                                MaterialStateProperty<Color?>.All(
                                                    Color.Parse("#FF1565C0")),
                                            trackOutlineWidth:
                                                MaterialStateProperty<double?>.All(3.0))),
                                    BuildRow(
                                        "Adaptive",
                                        Switch.Adaptive(
                                            value: _adaptive,
                                            onChanged: value => SetState(() => _adaptive = value))),
                                    BuildRow(
                                        "Disabled (on)",
                                        new Switch(value: true, onChanged: null)),
                                    BuildRow(
                                        "Disabled (off)",
                                        new Switch(value: false, onChanged: null)),
                                ]))),
                ]));
    }

    private static Widget BuildRow(string label, Widget control)
    {
        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12, 8),
            child: new Row(
                spacing: 12,
                children:
                [
                    new SizedBox(
                        width: 140,
                        child: new Text(label, fontSize: 13, color: Colors.Black)),
                    control,
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
