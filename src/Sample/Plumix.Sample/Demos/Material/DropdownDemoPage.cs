using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/dropdown_demo_page.dart

public sealed class DropdownDemoPage : StatefulWidget
{
    public override State CreateState() => new DropdownDemoPageState();

    private sealed class DropdownDemoPageState : State
    {
        private string? _value = "two";
        private bool _enabled = true;
        private bool _dense;
        private bool _expanded;
        private bool _hideUnderline;
        private bool _aligned;
        private string _status = "idle";

        public override Widget Build(BuildContext context)
        {
            Widget dropdown = new DropdownButton<string>(
                items: BuildItems(),
                onChanged: _enabled
                    ? value => SetState(() =>
                    {
                        _value = value;
                        _status = $"selected: {value ?? "none"}";
                    })
                    : null,
                selectedItemBuilder: _ =>
                [
                    new Text("No selection"),
                    new Text("Compact one"),
                    new Text("Compact two"),
                    new Text("Compact three"),
                    new Text("Disabled entry"),
                ],
                value: _value,
                hint: new Text("Choose a value"),
                disabledHint: new Text("Dropdown disabled"),
                onTap: () => SetState(() => _status = "opened"),
                isDense: _dense,
                isExpanded: _expanded,
                dropdownColor: Color.Parse("#FFFFF8E1"),
                menuMaxHeight: 180,
                borderRadius: BorderRadius.Circular(10),
                padding: new Thickness(8, 4));
            if (_hideUnderline) dropdown = new DropdownButtonHideUnderline(dropdown);
            dropdown = new ButtonTheme(new ButtonThemeData(AlignedDropdown: _aligned), dropdown);
            dropdown = _expanded
                ? new SizedBox(width: 320, child: dropdown)
                : dropdown;

            return new SingleChildScrollView(
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 14,
                    children:
                    [
                        new Text("DropdownButton + DropdownMenuItem", fontSize: 20),
                        new Text(
                            "Controlled selection with nullable/disabled entries, selectedItemBuilder, route geometry, keyboard focus, and underline policy.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                                ControlButton(_dense ? "Dense" : "Regular", () => SetState(() => _dense = !_dense)),
                                ControlButton(_expanded ? "Expanded" : "Compact", () => SetState(() => _expanded = !_expanded)),
                            ]),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_hideUnderline ? "Underline off" : "Underline on", () => SetState(() => _hideUnderline = !_hideUnderline)),
                                ControlButton(_aligned ? "Aligned theme" : "Unaligned theme", () => SetState(() => _aligned = !_aligned)),
                            ]),
                        new Align(alignment: Alignment.CenterLeft, child: dropdown),
                        new Text($"Value: {_value ?? "none"}", fontSize: 13),
                        new Text($"Status: {_status}", fontSize: 13),
                        new Divider(),
                        new Text("Disabled fallback", fontSize: 15),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DropdownButton<string>(
                                items: BuildItems(),
                                onChanged: null,
                                hint: new Text("Fallback hint"),
                                disabledHint: new Text("Disabled hint"))),
                    ]));
        }

        private static IReadOnlyList<DropdownMenuItem<string>> BuildItems() =>
        [
            new DropdownMenuItem<string>(new Text("None"), value: null),
            new DropdownMenuItem<string>(new Text("One"), value: "one"),
            new DropdownMenuItem<string>(new Text("Two"), value: "two"),
            new DropdownMenuItem<string>(new Text("Three"), value: "three"),
            new DropdownMenuItem<string>(new Text("Disabled entry"), value: "disabled", enabled: false),
        ];

        private static Widget ControlButton(string label, Action action) =>
            new TextButton(new Text(label, fontSize: 12), action);
    }
}
