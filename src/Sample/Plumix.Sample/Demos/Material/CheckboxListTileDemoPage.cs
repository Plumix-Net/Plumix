using System;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/checkbox_list_tile_demo_page.dart
public sealed class CheckboxListTileDemoPage : StatefulWidget
{
    public override State CreateState() => new CheckboxListTileDemoPageState();
}

internal sealed class CheckboxListTileDemoPageState : State
{
    private bool _checkboxValue;
    private bool? _tristateValue;
    private bool _enabled = true;
    private bool _adaptive;
    private bool _compact;
    private bool _scaled;
    private ListTileControlAffinity _affinity = ListTileControlAffinity.Trailing;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("CheckboxListTile", fontSize: 20, color: Colors.Black),
                new Text(
                    "Whole-tile interaction, tristate cycle, affinity, checkbox scaling, "
                    + "density/alignment, selected styling, disabled state, and the adaptive branch.",
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
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ListTileControlDemos.ControlButton(
                            _compact ? "Compact / top" : "Standard / center",
                            () => SetState(() => _compact = !_compact),
                            144,
                            Color.Parse("#FFF3E5F5")),
                        ListTileControlDemos.ControlButton(
                            _scaled ? "Scale 1.5x" : "Scale 1.0x",
                            () => SetState(() => _scaled = !_scaled),
                            104,
                            Color.Parse("#FFE0F2F1")),
                    ]),
                new Text(
                    $"checkbox={ListTileControlDemos.Lower(_checkboxValue)}, "
                    + $"tristate={ListTileControlDemos.FormatNullable(_tristateValue)}, "
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
                                    BuildCheckboxTile(),
                                    BuildTristateTile(),
                                ])))),
            ]);
    }

    private Widget BuildCheckboxTile()
    {
        Action<bool?>? onChanged = _enabled
            ? value => SetState(() => _checkboxValue = value == true)
            : null;
        return _adaptive
            ? CheckboxListTile.Adaptive(
                value: _checkboxValue,
                onChanged: onChanged,
                title: new Text("Wi-Fi discovery"),
                subtitle: new Text("Tap anywhere on the row to toggle."),
                secondary: new Icon(Icons.InfoOutline),
                titleAlignment: _compact ? ListTileTitleAlignment.Top : ListTileTitleAlignment.Center,
                checkboxScaleFactor: _scaled ? 1.5 : 1.0,
                selected: _checkboxValue)
            : new CheckboxListTile(
                value: _checkboxValue,
                onChanged: onChanged,
                title: new Text("Wi-Fi discovery"),
                subtitle: new Text("Tap anywhere on the row to toggle."),
                secondary: new Icon(Icons.InfoOutline),
                titleAlignment: _compact ? ListTileTitleAlignment.Top : ListTileTitleAlignment.Center,
                checkboxScaleFactor: _scaled ? 1.5 : 1.0,
                selected: _checkboxValue,
                selectedTileColor: Color.Parse("#FFE8DEF8"));
    }

    private Widget BuildTristateTile()
    {
        Action<bool?>? onChanged = _enabled
            ? value => SetState(() => _tristateValue = value)
            : null;
        return _adaptive
            ? CheckboxListTile.Adaptive(
                value: _tristateValue,
                onChanged: onChanged,
                tristate: true,
                title: new Text("Tristate selection"),
                subtitle: new Text("false → true → null → false"),
                checkboxScaleFactor: _scaled ? 1.5 : 1.0,
                secondary: new Icon(Icons.StarOutline))
            : new CheckboxListTile(
                value: _tristateValue,
                onChanged: onChanged,
                tristate: true,
                title: new Text("Tristate selection"),
                subtitle: new Text("false → true → null → false"),
                checkboxScaleFactor: _scaled ? 1.5 : 1.0,
                secondary: new Icon(Icons.StarOutline));
    }
}
