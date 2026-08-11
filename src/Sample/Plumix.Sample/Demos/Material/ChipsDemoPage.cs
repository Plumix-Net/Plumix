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
    private bool _filterSelected;
    private bool _inputSelected;
    private bool _inputVisible = true;
    private bool _useLocalTheme;
    private int _actionCount;
    private int _deleteCount;

    public override Widget Build(BuildContext context)
    {
        Widget probes = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 14,
            children:
            [
                new Text("Material chips", fontSize: 20, color: Colors.Black),
                new Text(
                    "Informational, action, choice, filter, and input chips use Wrap for multi-run layouts, "
                    + "with deletion and a copyWith-derived local ChipTheme override.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                        ControlButton(
                            _useLocalTheme ? "Theme override on" : "Theme override off",
                            () => SetState(() => _useLocalTheme = !_useLocalTheme)),
                        ControlButton(
                            _inputVisible ? "Remove input" : "Restore input",
                            () => SetState(() => _inputVisible = !_inputVisible)),
                    ]),
                new Text("Action chips", fontSize: 14, color: Colors.Black),
                new Wrap(
                    spacing: 10,
                    runSpacing: 10,
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
                new Text("Informational chips", fontSize: 14, color: Colors.Black),
                new Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children:
                    [
                        new Chip(
                            label: new Text("Read only"),
                            avatar: new Icon(Icons.InfoOutline)),
                        new Chip(
                            label: new Text("Deletable"),
                            avatar: new Icon(Icons.InfoOutline),
                            onDeleted: _enabled ? HandleDelete : null),
                    ]),
                new Text("Choice chips", fontSize: 14, color: Colors.Black),
                new Wrap(
                    spacing: 10,
                    runSpacing: 10,
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
                new Text("Filter chips", fontSize: 14, color: Colors.Black),
                new Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children:
                    [
                        new FilterChip(
                            label: new Text("Favorites"),
                            avatar: new Icon(Icons.StarOutline),
                            selected: _filterSelected,
                            onSelected: _enabled ? value => SetState(() => _filterSelected = value) : null),
                        FilterChip.Elevated(
                            label: new Text("Elevated"),
                            selected: !_filterSelected,
                            onSelected: _enabled ? value => SetState(() => _filterSelected = !value) : null),
                        new FilterChip(
                            label: new Text("Deletable"),
                            selected: _filterSelected,
                            onSelected: _enabled ? value => SetState(() => _filterSelected = value) : null,
                            onDeleted: _enabled ? HandleDelete : null),
                    ]),
                new Text("Input chips", fontSize: 14, color: Colors.Black),
                new Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children:
                    [
                        _inputVisible
                            ? new InputChip(
                                label: new Text("Ada"),
                                avatar: new CircleAvatar(child: new Text("A")),
                                selected: _inputSelected,
                                isEnabled: _enabled,
                                onSelected: value => SetState(() => _inputSelected = value),
                                onDeleted: () => SetState(() =>
                                {
                                    _inputVisible = false;
                                    _deleteCount++;
                                }))
                            : new Text("Input removed", fontSize: 13, color: Color.Parse("#FF49454F")),
                        new InputChip(
                            label: new Text("Pressable"),
                            avatar: new Icon(Icons.InfoOutline),
                            isEnabled: _enabled,
                            onPressed: HandleAction),
                    ]),
                new Text(
                    $"Actions: {_actionCount} · deletes: {_deleteCount} · choice: {_selected.ToString().ToLowerInvariant()} · "
                    + $"filter: {_filterSelected.ToString().ToLowerInvariant()} · input: {_inputSelected.ToString().ToLowerInvariant()}",
                    fontSize: 13,
                    color: Color.Parse("#FF49454F")),
            ]);

        if (!_useLocalTheme)
        {
            return probes;
        }

        return new ChipTheme(
            data: ChipTheme.Of(context).CopyWith(
                backgroundColor: Color.Parse("#FFFFDDB3"),
                selectedColor: Color.Parse("#FF006C4C"),
                checkmarkColor: Colors.White,
                labelStyle: new TextStyle(Color: Color.Parse("#FF271900")),
                secondaryLabelStyle: new TextStyle(Color: Colors.White),
                shape: ShapeBorder.RoundedRectangle(14)),
            child: probes);
    }

    private void HandleAction()
    {
        SetState(() => _actionCount++);
    }

    private void HandleDelete()
    {
        SetState(() => _deleteCount++);
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
