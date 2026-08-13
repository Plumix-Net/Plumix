using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/segmented_buttons_demo_page.dart

public sealed class SegmentedButtonsDemoPage : StatefulWidget
{
    public override State CreateState() => new SegmentedButtonsDemoPageState();

    private sealed class SegmentedButtonsDemoPageState : State
    {
        private readonly List<bool> _toggleSelection = [true, false, false];
        private HashSet<int> _segmentSelection = [0];
        private bool _multiSelection;
        private bool _emptySelection;
        private bool _vertical;
        private bool _showSelectedIcon = true;
        private bool _useThemeOverrides;
        private bool _useWidgetStyle;
        private bool _useStatefulFill;

        public override Widget Build(BuildContext context)
        {
            MaterialStateProperty<Color?>? toggleFill = _useStatefulFill
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? Colors.Teal : Colors.LightBlue)
                : _useWidgetStyle
                    ? MaterialStateProperty<Color?>.All(Color.Parse("#FF673AB7"))
                    : null;
            var theme = Theme.Of(context) with
            {
                ToggleButtonsTheme = _useThemeOverrides
                    ? new ToggleButtonsThemeData(
                        Color: Colors.DarkSlateBlue,
                        SelectedColor: Colors.White,
                        FillColor: Colors.Teal,
                        BorderColor: Colors.Teal,
                        SelectedBorderColor: Colors.Teal,
                        BorderRadius: BorderRadius.Circular(12))
                    : new ToggleButtonsThemeData(),
                SegmentedButtonTheme = _useThemeOverrides
                    ? new SegmentedButtonThemeData(
                        Style: SegmentedButton<int>.StyleFrom(
                            foregroundColor: Colors.DarkSlateBlue,
                            selectedForegroundColor: Colors.White,
                            selectedBackgroundColor: Colors.Teal,
                            side: new BorderSide(Colors.Teal),
                            shape: new StadiumBorder()),
                        SelectedIcon: new Icon(Icons.Star))
                    : new SegmentedButtonThemeData(),
            };

            return new Theme(
                data: theme,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12,
                    children:
                    [
                        new Text("ToggleButtons + SegmentedButton", fontSize: 20),
                        new Text(
                            "Legacy bool-list toggles and Material 3 value-set segments with selection, orientation, themes, and widget styles.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_multiSelection ? "Multi" : "Single", ToggleMultiSelection),
                                ControlButton(_emptySelection ? "Empty allowed" : "Selection required", ToggleEmptySelection),
                                ControlButton(_vertical ? "Vertical" : "Horizontal", () => SetState(() => _vertical = !_vertical)),
                                ControlButton(_showSelectedIcon ? "Check on" : "Check off", () => SetState(() => _showSelectedIcon = !_showSelectedIcon)),
                            ]),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                                ControlButton(_useWidgetStyle ? "Widget style on" : "Widget style off", () => SetState(() => _useWidgetStyle = !_useWidgetStyle)),
                                ControlButton(
                                    _useStatefulFill ? "State fill on" : "State fill off",
                                    () => SetState(() => _useStatefulFill = !_useStatefulFill)),
                            ]),
                        new Text($"ToggleButtons selection: {string.Join(",", _toggleSelection.Select(value => value ? "1" : "0"))}"),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new ToggleButtons(
                                isSelected: _toggleSelection,
                                onPressed: index => SetState(() => _toggleSelection[index] = !_toggleSelection[index]),
                                direction: _vertical ? Axis.Vertical : Axis.Horizontal,
                                borderRadius: _useWidgetStyle ? BorderRadius.Circular(20) : null,
                                selectedColor: _useWidgetStyle ? Colors.White : null,
                                fillColor: toggleFill,
                                children:
                                [
                                    new Icon(Icons.StarOutline),
                                    new Icon(Icons.InfoOutline),
                                    new Icon(Icons.Menu),
                                ])),
                        new Text(
                            _segmentSelection.Count == 0
                                ? "Segmented selection: none"
                                : $"Segmented selection: {string.Join(",", _segmentSelection.Order())}"),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new SegmentedButton<int>(
                                selected: _segmentSelection,
                                onSelectionChanged: selection => SetState(() => _segmentSelection = selection.ToHashSet()),
                                multiSelectionEnabled: _multiSelection,
                                emptySelectionAllowed: _emptySelection,
                                direction: _vertical ? Axis.Vertical : Axis.Horizontal,
                                showSelectedIcon: _showSelectedIcon,
                                style: _useWidgetStyle
                                    ? SegmentedButton<int>.StyleFrom(
                                        selectedForegroundColor: Colors.White,
                                        selectedBackgroundColor: Color.Parse("#FF673AB7"),
                                        side: new BorderSide(Color.Parse("#FF673AB7")),
                                        shape: new StadiumBorder())
                                    : null,
                                segments:
                                [
                                    new ButtonSegment<int>(0, icon: new Icon(Icons.StarOutline), label: new Text("Favorites"), tooltip: "Favorites segment"),
                                    new ButtonSegment<int>(1, icon: new Icon(Icons.InfoOutline), label: new Text("Explore")),
                                    new ButtonSegment<int>(2, icon: new Icon(Icons.Menu), label: new Text("Disabled"), enabled: false),
                                ])),
                    ]));
        }

        private void ToggleMultiSelection()
        {
            SetState(() =>
            {
                _multiSelection = !_multiSelection;
                if (!_multiSelection && _segmentSelection.Count > 1)
                {
                    _segmentSelection = [_segmentSelection.Min()];
                }
            });
        }

        private void ToggleEmptySelection()
        {
            SetState(() =>
            {
                _emptySelection = !_emptySelection;
                if (!_emptySelection && _segmentSelection.Count == 0)
                {
                    _segmentSelection = [0];
                }
            });
        }

        private static Widget ControlButton(string label, Action onPressed)
        {
            return new TextButton(
                child: new Text(label, fontSize: 12),
                onPressed: onPressed);
        }
    }
}
