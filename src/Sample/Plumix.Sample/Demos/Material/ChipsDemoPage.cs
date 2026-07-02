using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/chips_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ChipsDemoPage : StatefulWidget
{
    public override State CreateState() => new ChipsDemoPageState();
}

internal sealed class ChipsDemoPageState : State
{
    private bool _enabled = true;
    private bool _selected;
    private bool _useLocalTheme;
    private int _actionCount;

    public override Widget Build(BuildContext context)
    {
        Widget probes = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 14,
            children:
            [
                new Text("ActionChip + ChoiceChip", fontSize: 20, color: Colors.Black),
                new Text(
                    "Flat/elevated variants, selected and disabled states, avatar/checkmark, and ChipTheme precedence.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                        ControlButton(_useLocalTheme ? "Theme override on" : "Theme override off", () => SetState(() => _useLocalTheme = !_useLocalTheme)),
                    ]),
                new Text("Action chips", fontSize: 14, color: Colors.Black),
                new Row(
                    spacing: 10,
                    children:
                    [
                        new ActionChip(
                            label: new Text("Suggest"),
                            onPressed: _enabled ? HandleAction : null),
                        new ActionChip(
                            label: new Text("Assist"),
                            avatar: new Icon(Icons.Star),
                            onPressed: _enabled ? HandleAction : null),
                        ActionChip.Elevated(
                            label: new Text("Elevated"),
                            onPressed: _enabled ? HandleAction : null),
                    ]),
                new Text("Choice chips", fontSize: 14, color: Colors.Black),
                new Row(
                    spacing: 10,
                    children:
                    [
                        new ChoiceChip(
                            label: new Text("Standard"),
                            selected: !_selected,
                            onSelected: _enabled ? value => SetState(() => _selected = !value) : null),
                        new ChoiceChip(
                            label: new Text("Selected"),
                            selected: _selected,
                            onSelected: _enabled ? value => SetState(() => _selected = value) : null),
                        ChoiceChip.Elevated(
                            label: new Text("Elevated"),
                            avatar: new Icon(Icons.StarOutline),
                            selected: _selected,
                            onSelected: _enabled ? value => SetState(() => _selected = value) : null),
                    ]),
                new Text(
                    $"Actions: {_actionCount} · selected: {_selected.ToString().ToLowerInvariant()}",
                    fontSize: 13,
                    color: Color.Parse("#FF49454F")),
            ]);

        if (!_useLocalTheme)
        {
            return probes;
        }

        return new ChipTheme(
            data: new ChipThemeData(
                BackgroundColor: Color.Parse("#FFFFDDB3"),
                SelectedColor: Color.Parse("#FF006C4C"),
                CheckmarkColor: Colors.White,
                LabelStyle: new TextStyle(Color: Color.Parse("#FF271900")),
                SecondaryLabelStyle: new TextStyle(Color: Colors.White),
                Shape: ShapeBorder.RoundedRectangle(14)),
            child: probes);
    }

    private void HandleAction()
    {
        SetState(() => _actionCount++);
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            backgroundColor: Color.Parse("#FFEADDFF"),
            foregroundColor: Color.Parse("#FF21005D"),
            minHeight: 36,
            child: new Text(label, fontSize: 12));
    }
}
