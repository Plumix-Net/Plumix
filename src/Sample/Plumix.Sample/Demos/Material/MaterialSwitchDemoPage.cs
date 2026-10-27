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
                children:
                [
                    new Text("Switch", style: new TextStyle(FontSize: 20)),
                    new SizedBox(height: 8),
                    new Text(
                        "M2/M3 tokens, thumb icons, track outlines and Switch.adaptive per platform.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new SizedBox(height: 10),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: new Color(0xFFE9F0FF)),
                            BuildControlButton(
                                label: platform.Name(),
                                onTap: () => SetState(() =>
                                    _platformIndex = (_platformIndex + 1) % Platforms.Length),
                                width: 112,
                                background: new Color(0xFFEAF6F7)),
                        ]),
                    new SizedBox(height: 8),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, platform={platform.Name()}",
                        style: new TextStyle(FontSize: 12, Color: new Color(0xFF607D8B))),
                    new SizedBox(height: 10),
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
                                            activeThumbColor: new Color(0xFFFFF8E1),
                                            activeTrackColor: new Color(0xFF00695C),
                                            inactiveThumbColor: new Color(0xFF8D6E63),
                                            inactiveTrackColor: new Color(0xFFD7CCC8))),
                                    BuildRow(
                                        "Thumb icons",
                                        new Switch(
                                            value: _iconed,
                                            onChanged: value => SetState(() => _iconed = value),
                                            thumbIcon: WidgetStateProperty<Icon?>.ResolveWith(
                                                states => states.Contains(WidgetState.Selected)
                                                    ? new Icon(Icons.Check)
                                                    : new Icon(Icons.Close)))),
                                    BuildRow(
                                        "Track outline",
                                        new Switch(
                                            value: _outlined,
                                            onChanged: value => SetState(() => _outlined = value),
                                            trackOutlineColor:
                                                WidgetStateProperty<Color?>.All(
                                                    new Color(0xFF1565C0)),
                                            trackOutlineWidth:
                                                WidgetStateProperty<double?>.All(3.0))),
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
            color: new Color(0xFFF7F9FC),
            padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
            child: new Row(
                spacing: 12,
                children:
                [
                    new SizedBox(
                        width: 140,
                        child: new Text(label, style: new TextStyle(FontSize: 13, Color: MaterialColors.Black))),
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
                style: TextButton.StyleFrom(
                    backgroundColor: background,
                    foregroundColor: MaterialColors.Black,
                    minimumSize: new Size(0, 36),
                    padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 8),
                    shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(8))),
                child: new Text(label, style: new TextStyle(FontSize: 12))));
    }
}
