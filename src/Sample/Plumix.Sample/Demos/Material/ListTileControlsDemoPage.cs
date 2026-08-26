using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/list_tile_controls_demo_page.dart
public sealed class ListTileControlsDemoPage : StatefulWidget
{
    public override State CreateState() => new ListTileControlsDemoPageState();
}

internal sealed class ListTileControlsDemoPageState : State
{
    private bool _checkboxValue;
    private bool? _tristateValue;
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
                new Text("CheckboxListTile + SwitchListTile", fontSize: 20, color: Colors.Black),
                new Text(
                    "Whole-tile interaction, tristate cycle, affinity, density/alignment, selected styling, "
                    + "disabled state, and adaptive branches.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            _enabled ? "Enabled" : "Disabled",
                            () => SetState(() => _enabled = !_enabled),
                            104,
                            Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            _affinity == ListTileControlAffinity.Leading ? "Leading" : "Trailing",
                            () => SetState(() => _affinity = _affinity == ListTileControlAffinity.Leading
                                ? ListTileControlAffinity.Trailing
                                : ListTileControlAffinity.Leading),
                            104,
                            Color.Parse("#FFE9F7EF")),
                        BuildControlButton(
                            _adaptive ? "Adaptive" : "Material",
                            () => SetState(() => _adaptive = !_adaptive),
                            104,
                            Color.Parse("#FFF8EFE2")),
                    ]),
                new Row(
                    children:
                    [
                        BuildControlButton(
                            _compact ? "Compact / top" : "Standard / center",
                            () => SetState(() => _compact = !_compact),
                            144,
                            Color.Parse("#FFF3E5F5")),
                    ]),
                new Text(
                    $"checkbox={_checkboxValue.ToString().ToLowerInvariant()}, tristate={FormatNullable(_tristateValue)}, switch={_switchValue.ToString().ToLowerInvariant()}, affinity={_affinity.ToString().ToLowerInvariant()}, adaptive={_adaptive.ToString().ToLowerInvariant()}",
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
                                    BuildSwitchTile(),
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
                selected: _checkboxValue)
            : new CheckboxListTile(
                value: _checkboxValue,
                onChanged: onChanged,
                title: new Text("Wi-Fi discovery"),
                subtitle: new Text("Tap anywhere on the row to toggle."),
                secondary: new Icon(Icons.InfoOutline),
                titleAlignment: _compact ? ListTileTitleAlignment.Top : ListTileTitleAlignment.Center,
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
                secondary: new Icon(Icons.StarOutline))
            : new CheckboxListTile(
                value: _tristateValue,
                onChanged: onChanged,
                tristate: true,
                title: new Text("Tristate selection"),
                subtitle: new Text("false → true → null → false"),
                secondary: new Icon(Icons.StarOutline));
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

    private static Widget BuildControlButton(string label, Action onPressed, double width, Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onPressed,
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }

    private static string FormatNullable(bool? value)
    {
        return value.HasValue ? value.Value.ToString().ToLowerInvariant() : "null";
    }
}
