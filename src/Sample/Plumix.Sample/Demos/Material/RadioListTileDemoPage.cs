using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/radio_list_tile_demo_page.dart
public sealed class RadioListTileDemoPage : StatefulWidget
{
    public override State CreateState() => new RadioListTileDemoPageState();
}

internal sealed class RadioListTileDemoPageState : State
{
    private string? _radioValue = "standard";
    private bool _enabled = true;
    private bool _adaptive;
    private bool _toggleable = true;
    private bool _scaled;
    private ListTileControlAffinity _affinity = ListTileControlAffinity.Platform;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("RadioListTile", fontSize: 20, color: Colors.Black),
                new Text(
                    "RadioGroup selection, toggleable clearing, radio scaling, and the affinity rule "
                    + "that puts the radio first on 'platform' — unlike the checkbox and switch tiles.",
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
                            _affinity == ListTileControlAffinity.Trailing ? "Trailing" : "Platform",
                            () => SetState(() => _affinity = _affinity == ListTileControlAffinity.Trailing
                                ? ListTileControlAffinity.Platform
                                : ListTileControlAffinity.Trailing),
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
                            _toggleable ? "Toggleable" : "Sticky",
                            () => SetState(() => _toggleable = !_toggleable),
                            104,
                            Color.Parse("#FFF3E5F5")),
                        ListTileControlDemos.ControlButton(
                            _scaled ? "Scale 1.5x" : "Scale 1.0x",
                            () => SetState(() => _scaled = !_scaled),
                            104,
                            Color.Parse("#FFE0F2F1")),
                    ]),
                new Text(
                    $"radio={_radioValue ?? "null"}, affinity={_affinity.ToString().ToLowerInvariant()}, "
                    + $"toggleable={ListTileControlDemos.Lower(_toggleable)}, "
                    + $"adaptive={ListTileControlDemos.Lower(_adaptive)}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF7F9FC"),
                        child: new RadioGroup<string>(
                            groupValue: _radioValue,
                            onChanged: value => SetState(() => _radioValue = value),
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children:
                                [
                                    BuildRadioTile("standard", "Standard sync", "Every change, on any network."),
                                    BuildRadioTile("metered", "Metered sync", "Only on unmetered connections."),
                                    BuildRadioTile("manual", "Manual sync", "Nothing until you ask for it."),
                                ])))),
            ]);
    }

    private Widget BuildRadioTile(string value, string title, string subtitle)
    {
        return _adaptive
            ? RadioListTile<string>.Adaptive(
                value: value,
                toggleable: _toggleable,
                enabled: _enabled,
                controlAffinity: _affinity,
                title: new Text(title),
                subtitle: new Text(subtitle),
                secondary: new Icon(Icons.Done),
                radioScaleFactor: _scaled ? 1.5 : 1.0,
                selected: _radioValue == value)
            : new RadioListTile<string>(
                value: value,
                toggleable: _toggleable,
                enabled: _enabled,
                controlAffinity: _affinity,
                title: new Text(title),
                subtitle: new Text(subtitle),
                secondary: new Icon(Icons.Done),
                radioScaleFactor: _scaled ? 1.5 : 1.0,
                selected: _radioValue == value,
                selectedTileColor: Color.Parse("#FFE8DEF8"));
    }
}
