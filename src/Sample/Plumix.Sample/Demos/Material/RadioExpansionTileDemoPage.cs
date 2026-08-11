using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/radio_expansion_tile_demo_page.dart
public sealed class RadioExpansionTileDemoPage : StatefulWidget
{
    public override State CreateState() => new RadioExpansionTileDemoPageState();
}

internal sealed class RadioExpansionTileDemoPageState : State
{
    private readonly ExpansibleController _expansionController = new();
    private string? _selectedSchedule = "daily";
    private bool _toggleable;
    private bool _adaptive;
    private bool _maintainState;
    private bool _useMaterial3 = true;
    private bool _expanded;
    private ListTileControlAffinity _affinity = ListTileControlAffinity.Leading;

    public override void Dispose()
    {
        _expansionController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("RadioListTile + ExpansionTile", fontSize: 20, color: Colors.Black),
                new Text(
                    "RadioGroup selection and controller-driven expansion with animated arrow/body/theme transitions.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            _toggleable ? "Toggleable" : "Single select",
                            () => SetState(() => _toggleable = !_toggleable),
                            116,
                            Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            _adaptive ? "Adaptive" : "Material",
                            () => SetState(() => _adaptive = !_adaptive),
                            104,
                            Color.Parse("#FFE9F7EF")),
                        BuildControlButton(
                            _affinity == ListTileControlAffinity.Leading ? "Leading" : "Trailing",
                            () => SetState(() => _affinity = _affinity == ListTileControlAffinity.Leading
                                ? ListTileControlAffinity.Trailing
                                : ListTileControlAffinity.Leading),
                            104,
                            Color.Parse("#FFF8EFE2")),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            _expanded ? "Collapse" : "Expand",
                            () => _expansionController.Toggle(),
                            100,
                            Color.Parse("#FFF0E8FF")),
                        BuildControlButton(
                            _maintainState ? "Maintain on" : "Maintain off",
                            () => SetState(() => _maintainState = !_maintainState),
                            112,
                            Color.Parse("#FFEAF6F7")),
                        BuildControlButton(
                            _useMaterial3 ? "Material 3" : "Material 2",
                            () => SetState(() => _useMaterial3 = !_useMaterial3),
                            104,
                            Color.Parse("#FFFFF2CC")),
                    ]),
                new Text(
                    $"selected={_selectedSchedule ?? "null"}, expanded={_expanded.ToString().ToLowerInvariant()}, "
                    + $"affinity={_affinity.ToString().ToLowerInvariant()}, "
                    + $"adaptive={_adaptive.ToString().ToLowerInvariant()}, "
                    + $"material3={_useMaterial3.ToString().ToLowerInvariant()}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF7F9FC"),
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                BuildRadioGroup(context),
                                new Theme(
                                    data: BuildControlTheme(context),
                                    child: new ExpansionTile(
                                        title: new Text("Advanced schedule options"),
                                        subtitle: new Text("Tap row or use the controller button."),
                                        leading: new Icon(Icons.InfoOutline),
                                        controller: _expansionController,
                                        controlAffinity: _affinity,
                                        maintainState: _maintainState,
                                        backgroundColor: Color.Parse("#FFF0E8FF"),
                                        collapsedBackgroundColor: Colors.White,
                                        shape: ShapeBorder.RoundedRectangle(12),
                                        collapsedShape: ShapeBorder.RoundedRectangle(4),
                                        onExpansionChanged: value => SetState(() => _expanded = value),
                                        childrenPadding: EdgeInsetsGeometry.FromLTRB(20, 8, 20, 12),
                                        children:
                                        [
                                            new Text("Sync only while charging", fontSize: 13),
                                            new Text("Retry window: 15 minutes", fontSize: 13),
                                        ])),
                            ]))),
            ]);
    }

    private Widget BuildRadioGroup(BuildContext context)
    {
        return new Theme(
            data: BuildControlTheme(context),
            child: new RadioGroup<string>(
                groupValue: _selectedSchedule,
                onChanged: value => SetState(() => _selectedSchedule = value),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children:
                    [
                        BuildRadioTile("daily", "Daily", Icons.Star),
                        BuildRadioTile("weekly", "Weekly", Icons.StarOutline),
                        BuildRadioTile("paused", "Paused (disabled)", Icons.InfoOutline, enabled: false),
                    ])));
    }

    private ThemeData BuildControlTheme(BuildContext context)
    {
        ThemeData ambient = Theme.Of(context);
        return ambient with
        {
            UseMaterial3 = _useMaterial3,
            ColorScheme = ambient.ColorScheme.CopyWith(
                primary: Color.Parse("#FF6750A4"),
                secondary: Color.Parse("#FF006C4C"),
                onSurface: Color.Parse("#FF1D1B20"),
                onSurfaceVariant: Color.Parse("#FF49454F"))
        };
    }

    private Widget BuildRadioTile(
        string value,
        string label,
        IconData icon,
        bool? enabled = null)
    {
        return _adaptive
            ? RadioListTile<string>.Adaptive(
                value: value,
                toggleable: _toggleable,
                title: new Text(label),
                secondary: new Icon(icon),
                selected: _selectedSchedule == value,
                enabled: enabled,
                controlAffinity: _affinity,
                useCupertinoCheckmarkStyle: true)
            : new RadioListTile<string>(
                value: value,
                toggleable: _toggleable,
                title: new Text(label),
                secondary: new Icon(icon),
                selected: _selectedSchedule == value,
                enabled: enabled,
                controlAffinity: _affinity,
                selectedTileColor: Color.Parse("#FFE8DEF8"));
    }

    private static Widget BuildControlButton(string label, Action onPressed, double width, Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onPressed,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(label, fontSize: 12)));
    }
}
