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
        private string? _formValue;
        private string _formStatus = "not validated";
        private string? _modernValue = "two";
        private string _modernStatus = "idle";
        private string? _modernFormValue;
        private string _modernFormStatus = "not validated";
        private string _anchorStatus = "closed";
        private string _menuBarStatus = "closed";
        private readonly MenuController _anchorController = new();
        private readonly MenuController _fileMenuController = new();
        private readonly MenuController _editMenuController = new();
        private readonly LabeledGlobalKey<FormState> _formKey = new("dropdown-form");
        private readonly LabeledGlobalKey<FormState> _modernFormKey = new("dropdown-menu-form");

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
                        new Text("DropdownMenu + DropdownMenuEntry", fontSize: 18),
                        new Text(
                            "Editable Material 3 menu with filtering, search highlighting, disabled-entry traversal, leading/trailing icons, and controller-backed route state.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DropdownMenu<string>(
                                dropdownMenuEntries: BuildModernEntries(),
                                initialSelection: _modernValue,
                                width: 320,
                                menuHeight: 180,
                                leadingIcon: new Icon(Icons.Search),
                                label: new Text("Search a destination"),
                                helperText: "Type to filter, then use arrow keys",
                                enableFilter: true,
                                onSelected: value => SetState(() =>
                                {
                                    _modernValue = value;
                                    _modernStatus = $"selected: {value ?? "none"}";
                                }))),
                        new Text($"Modern value: {_modernValue ?? "none"}", fontSize: 13),
                        new Text($"Modern status: {_modernStatus}", fontSize: 13),
                        new Divider(),
                        new Text("MenuAnchor + MenuItemButton", fontSize: 18),
                        new Text(
                            "Controller-owned anchored menu with enabled/disabled leaf items and close-on-activate policy.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuAnchor(
                                controller: _anchorController,
                                onOpen: () => SetState(() => _anchorStatus = "opened"),
                                onClose: () => SetState(() => _anchorStatus = "closed"),
                                menuChildren:
                                [
                                    new MenuItemButton(
                                        child: new Text("Run action"),
                                        onPressed: () => SetState(() => _anchorStatus = "activated")),
                                    new MenuItemButton(child: new Text("Disabled item")),
                                    new MenuItemButton(
                                        child: new Text("Keep open"),
                                        closeOnActivate: false,
                                        onPressed: () => SetState(() => _anchorStatus = "kept open")),
                                ],
                                builder: (_, controller, _) => new TextButton(
                                    new Text(controller.IsOpen ? "Close menu" : "Open menu"),
                                    () =>
                                    {
                                        if (controller.IsOpen) controller.Close(); else controller.Open();
                                    }))),
                        new Text($"Anchor menu: {_anchorStatus}", fontSize: 13),
                        new Divider(),
                        new Text("MenuBar + SubmenuButton", fontSize: 18),
                        new Text(
                            "Horizontal menu bar with controller-owned sibling closing, nested side submenu, "
                            + "and local menu themes.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuTheme(
                                new MenuThemeData(
                                    Style: new MenuStyle(
                                        BackgroundColor: MaterialStateProperty<Color?>.All(Color.Parse("#FFFFF3E0"))),
                                    SubmenuIcon: MaterialStateProperty<Widget?>.All(new Icon(Icons.InfoOutline))),
                                new MenuBarTheme(
                                    new MenuBarThemeData(new MenuStyle(
                                        BackgroundColor: MaterialStateProperty<Color?>.All(Color.Parse("#FFF3E5F5")))),
                                    new MenuButtonTheme(
                                    new MenuButtonThemeData(new ButtonStyle(
                                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkSlateBlue))),
                                    new MenuBar(
                                        children:
                                        [
                                    new SubmenuButton(
                                        [
                                            new MenuItemButton(
                                                child: new Text("New document"),
                                                onPressed: () => SetState(() => _menuBarStatus = "new document")),
                                            new SubmenuButton(
                                                [
                                                    new MenuItemButton(
                                                        child: new Text("Quarterly report"),
                                                        onPressed: () => SetState(() => _menuBarStatus = "recent report")),
                                                ],
                                                new Text("Recent"),
                                                onOpen: () => SetState(() => _menuBarStatus = "recent opened")),
                                        ],
                                        new Text("File"),
                                        controller: _fileMenuController,
                                        style: new ButtonStyle(
                                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.OrangeRed)),
                                        onOpen: () => SetState(() => _menuBarStatus = "file opened"),
                                        onClose: () => SetState(() => _menuBarStatus = "file closed")),
                                    new SubmenuButton(
                                        [
                                            new MenuItemButton(
                                                child: new Text("Paste"),
                                                onPressed: () => SetState(() => _menuBarStatus = "paste")),
                                        ],
                                        new Text("Edit"),
                                        controller: _editMenuController,
                                        onOpen: () => SetState(() => _menuBarStatus = "edit opened"),
                                        onClose: () => SetState(() => _menuBarStatus = "edit closed")),
                                    new SubmenuButton([], new Text("Disabled")),
                                        ]))))),
                        new Text($"Menu bar: {_menuBarStatus}", fontSize: 13),
                        new Divider(),
                        new Text("DropdownMenuFormField + Form", fontSize: 18),
                        new Form(
                            key: _modernFormKey,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 8,
                                children:
                                [
                                    new DropdownMenuFormField<string>(
                                        dropdownMenuEntries: BuildModernEntries(),
                                        initialSelection: _modernFormValue,
                                        label: new Text("Required destination"),
                                        hintText: "Pick one destination",
                                        enableFilter: true,
                                        onSelected: value => SetState(() =>
                                        {
                                            _modernFormValue = value;
                                            _modernFormStatus = $"changed: {value ?? "none"}";
                                        }),
                                        validator: value => value is null ? "Select a destination" : null),
                                    new Row(
                                        spacing: 8,
                                        children:
                                        [
                                            ControlButton("Validate", ValidateModernForm),
                                            ControlButton("Reset", ResetModernForm),
                                        ]),
                                    new Text($"Modern form status: {_modernFormStatus}", fontSize: 13),
                                ])),
                        new Divider(),
                        new Text("Disabled fallback", fontSize: 15),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DropdownButton<string>(
                                items: BuildItems(),
                                onChanged: null,
                                hint: new Text("Fallback hint"),
                                disabledHint: new Text("Disabled hint"))),
                        new Divider(),
                        new Text("DropdownButtonFormField + Form", fontSize: 18),
                        new Form(
                            key: _formKey,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 8,
                                children:
                                [
                                    new DropdownButtonFormField<string>(
                                        items: BuildItems(),
                                        onChanged: value => SetState(() =>
                                        {
                                            _formValue = value;
                                            _formStatus = $"changed: {value ?? "none"}";
                                        }),
                                        initialValue: _formValue,
                                        decoration: new InputDecoration(
                                            labelText: "Required choice",
                                            hintText: "Pick one item",
                                            border: new OutlineInputBorder()),
                                        validator: value => value is null ? "Select an item" : null),
                                    new Row(
                                        spacing: 8,
                                        children:
                                        [
                                            ControlButton("Validate", ValidateForm),
                                            ControlButton("Reset", ResetForm),
                                        ]),
                                    new Text($"Form status: {_formStatus}", fontSize: 13),
                                ])),
                    ]));
        }

        private void ValidateForm()
        {
            bool valid = _formKey.CurrentState?.Validate() == true;
            SetState(() => _formStatus = valid ? "valid" : "invalid");
        }

        private void ResetForm()
        {
            _formKey.CurrentState?.Reset();
            SetState(() =>
            {
                _formValue = null;
                _formStatus = "reset";
            });
        }

        private void ValidateModernForm()
        {
            bool valid = _modernFormKey.CurrentState?.Validate() == true;
            SetState(() => _modernFormStatus = valid ? "valid" : "invalid");
        }

        private void ResetModernForm()
        {
            _modernFormKey.CurrentState?.Reset();
            SetState(() =>
            {
                _modernFormValue = null;
                _modernFormStatus = "reset";
            });
        }

        private static IReadOnlyList<DropdownMenuItem<string>> BuildItems() =>
        [
            new DropdownMenuItem<string>(new Text("None"), value: null),
            new DropdownMenuItem<string>(new Text("One"), value: "one"),
            new DropdownMenuItem<string>(new Text("Two"), value: "two"),
            new DropdownMenuItem<string>(new Text("Three"), value: "three"),
            new DropdownMenuItem<string>(new Text("Disabled entry"), value: "disabled", enabled: false),
        ];

        private static IReadOnlyList<DropdownMenuEntry<string>> BuildModernEntries() =>
        [
            new DropdownMenuEntry<string>("one", "One", leadingIcon: new Icon(Icons.StarOutline)),
            new DropdownMenuEntry<string>("two", "Two", leadingIcon: new Icon(Icons.Star)),
            new DropdownMenuEntry<string>("three", "Three", trailingIcon: new Icon(Icons.Check)),
            new DropdownMenuEntry<string>("disabled", "Disabled entry", enabled: false),
        ];

        private static Widget ControlButton(string label, Action action) =>
            new TextButton(new Text(label, fontSize: 12), action);
    }
}
