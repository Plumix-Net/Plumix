using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/list_tile_demo_page.dart (exact sample parity)

namespace Flutter.Net;

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
                    TextColor: Color.Parse("#FF27526B"),
                    IconColor: Color.Parse("#FF7A4021"),
                    TileColor: Color.Parse("#FFF5F9EE"),
                    SelectedTileColor: Color.Parse("#FFE4EEFF"),
                    Dense: _dense),
                child: content);
        }

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListTile baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Leading/title/subtitle/trailing composition with selected, dense, and theme-override probes.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _enabled ? "Enabled" : "Disabled",
                            onTap: () => SetState(() => _enabled = !_enabled),
                            width: 108,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: _selected ? "Selected" : "Unselected",
                            onTap: () => SetState(() => _selected = !_selected),
                            width: 120,
                            background: Color.Parse("#FFE9F7EF")),
                        BuildControlButton(
                            label: _dense ? "Dense" : "Regular",
                            onTap: () => SetState(() => _dense = !_dense),
                            width: 98,
                            background: Color.Parse("#FFF8EFE2")),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _threeLine ? "3-line" : "2-line",
                            onTap: () => SetState(() => _threeLine = !_threeLine),
                            width: 88,
                            background: Color.Parse("#FFF0E8FF")),
                        BuildControlButton(
                            label: _useThemeOverrides ? "Theme on" : "Theme off",
                            onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                            width: 112,
                            background: Color.Parse("#FFEAF6F7")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: ResetState,
                            width: 88,
                            background: Color.Parse("#FFF3E8D8")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, selected={(_selected ? "true" : "false")}, dense={(_dense ? "true" : "false")}, threeLine={(_threeLine ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, taps={_tapCount}, longPress={_longPressCount}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF7F9FC"),
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
                    tileColor: Color.Parse("#FFFFFFFF"),
                    selectedTileColor: Color.Parse("#FFE6EEFF"),
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
                    tileColor: Color.Parse("#FFFFFFFF"),
                    selectedTileColor: Color.Parse("#FFE6EEFF"),
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
                    tileColor: Color.Parse("#FFFFFFFF"),
                    selectedTileColor: Color.Parse("#FFE6EEFF"),
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
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(label, fontSize: 12)));
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
            _useThemeOverrides = false;
            _tapCount = 0;
            _longPressCount = 0;
        });
    }
}
