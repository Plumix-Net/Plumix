using System;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/switch_list_tile_demo_page.dart
public sealed class SwitchListTileDemoPage : StatefulWidget
{
    public override State CreateState() => new SwitchListTileDemoPageState();
}

internal sealed class SwitchListTileDemoPageState : State
{
    private bool _switchValue = true;
    private bool _enabled = true;
    private bool _adaptive;
    private bool _compact;
    private ListTileControlAffinity _affinity = ListTileControlAffinity.Trailing;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("SwitchListTile", fontSize: 20, color: Colors.Black),
                new Text(
                    "Whole-tile interaction, affinity, density/alignment, selected styling, "
                    + "disabled state, and the adaptive branch (which paints the Cupertino switch).",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ListTileControlDemos.ControlButton(
                            _enabled ? "Enabled" : "Disabled",
                            () => SetState(() => _enabled = !_enabled),
                            104,
                            Color.Parse("#FFE9F0FF")),
                        ListTileControlDemos.ControlButton(
                            _affinity == ListTileControlAffinity.Leading ? "Leading" : "Trailing",
                            () => SetState(() => _affinity = _affinity == ListTileControlAffinity.Leading
                                ? ListTileControlAffinity.Trailing
                                : ListTileControlAffinity.Leading),
                            104,
                            Color.Parse("#FFE9F7EF")),
                        ListTileControlDemos.ControlButton(
                            _adaptive ? "Adaptive" : "Material",
                            () => SetState(() => _adaptive = !_adaptive),
                            104,
                            Color.Parse("#FFF8EFE2")),
                        ListTileControlDemos.ControlButton(
                            _compact ? "Compact / top" : "Standard / center",
                            () => SetState(() => _compact = !_compact),
                            144,
                            Color.Parse("#FFF3E5F5")),
                    ]),
                new Text(
                    $"switch={ListTileControlDemos.Lower(_switchValue)}, "
                    + $"affinity={_affinity.ToString().ToLowerInvariant()}, "
                    + $"adaptive={ListTileControlDemos.Lower(_adaptive)}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF7F9FC"),
                        child: new ListTileTheme(
                            data: new ListTileThemeData(
                                ControlAffinity: _affinity,
                                VisualDensity: _compact ? VisualDensity.Compact : VisualDensity.Standard,
                                TitleAlignment: _compact
                                    ? ListTileTitleAlignment.Top
                                    : ListTileTitleAlignment.Center),
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children:
                                [
                                    BuildSwitchTile(),
                                ])))),
            ]);
    }

    private Widget BuildSwitchTile()
    {
        Action<bool>? onChanged = _enabled
            ? value => SetState(() => _switchValue = value)
            : null;
        return _adaptive
            ? SwitchListTile.Adaptive(
                value: _switchValue,
                onChanged: onChanged,
                title: new Text("Background sync"),
                subtitle: new Text("The embedded switch remains draggable."),
                secondary: new Icon(Icons.Menu),
                selected: _switchValue)
            : new SwitchListTile(
                value: _switchValue,
                onChanged: onChanged,
                title: new Text("Background sync"),
                subtitle: new Text("The embedded switch remains draggable."),
                secondary: new Icon(Icons.Menu),
                selected: _switchValue,
                selectedTileColor: Color.Parse("#FFE8DEF8"));
    }
}
