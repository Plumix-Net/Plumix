using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/list_tile_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ListTileDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ListTileDemoPageState();
    }
}

internal sealed class ListTileDemoPageState : State
{
    private bool _enabled = true;
    private bool _selected;
    private bool _dense;
    private bool _threeLine;
    private bool _useMaterial3 = true;
    private bool _useThemeOverrides;
    private int _tapCount;
    private int _longPressCount;

    public override Widget Build(BuildContext context)
    {
        var content = BuildTiles();
        if (_useThemeOverrides)
        {
            content = new ListTileTheme(
                data: new ListTileThemeData(
                    TextColor: new Color(0xFF27526B),
                    IconColor: new Color(0xFF7A4021),
                    TileColor: new Color(0xFFF5F9EE),
                    SelectedTileColor: new Color(0xFFE4EEFF),
                    Dense: _dense),
                child: content);
        }

        content = new Theme(
            data: Theme.Of(context) with { UseMaterial3 = _useMaterial3 },
            child: content);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListTile baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Exact M2/M3 padding, typography, slot geometry, states, and theme precedence.",
                    fontSize: 14,
                    color: new Color(0x8A000000)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _enabled ? "Enabled" : "Disabled",
                            onTap: () => SetState(() => _enabled = !_enabled),
                            width: 108,
                            background: new Color(0xFFE9F0FF)),
                        BuildControlButton(
                            label: _selected ? "Selected" : "Unselected",
                            onTap: () => SetState(() => _selected = !_selected),
                            width: 120,
                            background: new Color(0xFFE9F7EF)),
                        BuildControlButton(
                            label: _dense ? "Dense" : "Regular",
                            onTap: () => SetState(() => _dense = !_dense),
                            width: 98,
                            background: new Color(0xFFF8EFE2)),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _threeLine ? "3-line" : "2-line",
                            onTap: () => SetState(() => _threeLine = !_threeLine),
                            width: 88,
                            background: new Color(0xFFF0E8FF)),
                        BuildControlButton(
                            label: _useThemeOverrides ? "Theme on" : "Theme off",
                            onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                            width: 112,
                            background: new Color(0xFFEAF6F7)),
                        BuildControlButton(
                            label: _useMaterial3 ? "M3" : "M2",
                            onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                            width: 64,
                            background: new Color(0xFFFFF3CD)),
                        BuildControlButton(
                            label: "Reset",
                            onTap: ResetState,
                            width: 76,
                            background: new Color(0xFFF3E8D8)),
                    ]),
                new Text(
                    $"material={(_useMaterial3 ? "M3" : "M2")}, enabled={(_enabled ? "true" : "false")}, "
                    + $"selected={(_selected ? "true" : "false")}, dense={(_dense ? "true" : "false")}, "
                    + $"threeLine={(_threeLine ? "true" : "false")}, "
                    + $"theme={(_useThemeOverrides ? "true" : "false")}, taps={_tapCount}, "
                    + $"longPress={_longPressCount}",
                    fontSize: 12,
                    color: new Color(0xFF607D8B)),
                new Expanded(
                    child: new Container(
                        color: new Color(0xFFF7F9FC),
                        child: content)),
            ]);
    }

    private Widget BuildTiles()
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new ListTile(
                    title: new Text("One-line tile"),
                    leading: new Icon(Icons.Menu),
                    trailing: new Icon(Icons.InfoOutline),
                    selected: _selected,
                    enabled: _enabled,
                    dense: _dense,
                    tileColor: new Color(0xFFFFFFFF),
                    selectedTileColor: new Color(0xFFE6EEFF),
                    onTap: _enabled ? OnTap : null,
                    onLongPress: _enabled ? OnLongPress : null),
                new ListTile(
                    title: new Text("Two-line tile"),
                    subtitle: new Text("Subtitle text demonstrates two-line default height."),
                    leading: new Icon(Icons.Add),
                    trailing: new Text("meta", fontSize: 12),
                    selected: _selected,
                    enabled: _enabled,
                    dense: _dense,
                    tileColor: new Color(0xFFFFFFFF),
                    selectedTileColor: new Color(0xFFE6EEFF),
                    onTap: _enabled ? OnTap : null,
                    onLongPress: _enabled ? OnLongPress : null),
                new ListTile(
                    title: new Text("Three-line probe"),
                    subtitle: new Text("When 3-line is enabled this tile uses the taller baseline height for parity checks."),
                    leading: new Icon(Icons.StarOutline),
                    trailing: new Icon(Icons.Close),
                    selected: _selected,
                    enabled: _enabled,
                    dense: _dense,
                    isThreeLine: _threeLine,
                    tileColor: new Color(0xFFFFFFFF),
                    selectedTileColor: new Color(0xFFE6EEFF),
                    onTap: _enabled ? OnTap : null,
                    onLongPress: _enabled ? OnLongPress : null),
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

    private void OnTap()
    {
        SetState(() => _tapCount += 1);
    }

    private void OnLongPress()
    {
        SetState(() => _longPressCount += 1);
    }

    private void ResetState()
    {
        SetState(() =>
        {
            _enabled = true;
            _selected = false;
            _dense = false;
            _threeLine = false;
            _useMaterial3 = true;
            _useThemeOverrides = false;
            _tapCount = 0;
            _longPressCount = 0;
        });
    }
}
