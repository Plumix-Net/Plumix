using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
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
        private bool _rightToLeft;
        private bool _wideMenu;
        private bool _variableRows;
        private string _status = "idle";
        private string? _formValue;
        private string _formStatus = "not validated";
        private string? _modernValue = "two";
        private string _modernStatus = "idle";
        private string? _selectOnlyValue = "one";
        private string? _modernFormValue;
        private string _modernFormStatus = "not validated";
        private string _anchorStatus = "closed";
        private bool? _menuCheckbox = false;
        private string? _menuRadio = "one";
        private string _menuBarStatus = "closed";
        private readonly MenuController _anchorController = new();
        private readonly MenuController _fileMenuController = new();
        private readonly MenuController _editMenuController = new();
        private readonly MenuController _animatedMenuController = new();
        private readonly MenuController _alignedMenuController = new();
        private readonly MenuController _outlinedMenuController = new();
        private AlignmentGeometry _menuAlignment = AlignmentDirectional.BottomStart;
        private string _alignedMenuStatus = "idle";
        private bool _outlinedMenuSized;
        private string _animatedMenuStatus = "dismissed";
        private string _animatedMenuPick = "none";
        private readonly LabeledGlobalKey<FormState> _formKey = new("dropdown-form");
        private readonly LabeledGlobalKey<FormState> _modernFormKey = new("dropdown-menu-form");

        public override Widget Build(BuildContext context)
        {
            Widget dropdown = new DropdownButton<string>(
                items: BuildItems(_variableRows),
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
                alignment: AlignmentDirectional.CenterEnd,
                itemHeight: _variableRows ? null : 48,
                menuWidth: _wideMenu ? 280 : null,
                dropdownColor: new Color(0xFFFFF8E1),
                menuMaxHeight: 180,
                borderRadius: BorderRadius.Circular(10),
                padding: EdgeInsets.Symmetric(horizontal: 8, vertical: 4));
            if (_hideUnderline) dropdown = new DropdownButtonHideUnderline(dropdown);
            dropdown = new ButtonTheme(new ButtonThemeData(AlignedDropdown: _aligned), dropdown);
            dropdown = new Directionality(
                _rightToLeft ? Plumix.UI.TextDirection.Rtl : Plumix.UI.TextDirection.Ltr,
                dropdown);
            dropdown = _expanded
                ? new SizedBox(width: 320, child: dropdown)
                : dropdown;

            return new SingleChildScrollView(
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 14,
                    children:
                    [
                        new Text("DropdownButton + DropdownMenuItem", style: new TextStyle(FontSize: 20)),
                        new Text(
                            "Controlled selection with nullable/disabled entries, logical end alignment, "
                            + "route geometry, keyboard focus, and underline policy.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                ControlButton(
                                    _enabled ? "Enabled" : "Disabled",
                                    () => SetState(() => _enabled = !_enabled)),
                                ControlButton(_dense ? "Dense" : "Regular", () => SetState(() => _dense = !_dense)),
                                ControlButton(
                                    _expanded ? "Expanded" : "Compact",
                                    () => SetState(() => _expanded = !_expanded)),
                                ControlButton(
                                    _hideUnderline ? "Underline off" : "Underline on",
                                    () => SetState(() => _hideUnderline = !_hideUnderline)),
                                ControlButton(
                                    _aligned ? "Aligned theme" : "Unaligned theme",
                                    () => SetState(() => _aligned = !_aligned)),
                                ControlButton(
                                    _rightToLeft ? "Direction: RTL" : "Direction: LTR",
                                    () => SetState(() => _rightToLeft = !_rightToLeft)),
                            ]),
                        new Wrap(
                            spacing: 8,
                            children:
                            [
                                ControlButton(
                                    _wideMenu ? "Menu width: 280" : "Menu width: button",
                                    () => SetState(() => _wideMenu = !_wideMenu)),
                                ControlButton(
                                    _variableRows ? "Rows: variable" : "Rows: 48px",
                                    () => SetState(() => _variableRows = !_variableRows)),
                            ]),
                        new Align(alignment: Alignment.CenterLeft, child: dropdown),
                        new Text($"Value: {_value ?? "none"}", style: new TextStyle(FontSize: 13)),
                        new Text($"Status: {_status}", style: new TextStyle(FontSize: 13)),
                        new Divider(),
                        new Text("DropdownMenu + DropdownMenuEntry", style: new TextStyle(FontSize: 18)),
                        new Text(
                            "Editable Material 3 menu with filtering, search highlighting, disabled-entry traversal, leading/trailing icons, and controller-backed route state.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
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
                        new Text($"Modern value: {_modernValue ?? "none"}", style: new TextStyle(FontSize: 13)),
                        new Text($"Modern status: {_modernStatus}", style: new TextStyle(FontSize: 13)),
                        new Text(
                            "Select-only variant: read-only field, no trailing icon, and a selection"
                            + " that keeps the menu open.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DropdownMenu<string>(
                                dropdownMenuEntries: BuildModernEntries(),
                                initialSelection: _selectOnlyValue,
                                width: 320,
                                selectOnly: true,
                                showTrailingIcon: false,
                                closeBehavior: DropdownMenuCloseBehavior.None,
                                label: new Text("Select only"),
                                onSelected: value => SetState(() => _selectOnlyValue = value))),
                        new Text(
                            $"Select-only value: {_selectOnlyValue ?? "none"}",
                            style: new TextStyle(
                                FontSize: 13)),
                        new Divider(),
                        new Text("MenuAnchor + MenuItemButton", style: new TextStyle(FontSize: 18)),
                        new Text(
                            "Animated overlay menu with consumed outside taps, close-on-activate"
                            + " policy and display-only shortcut labels.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuAnchor(
                                controller: _anchorController,
                                consumeOutsideTap: true,
                                animated: true,
                                onOpen: () => SetState(() => _anchorStatus = "opened"),
                                onClose: () => SetState(() => _anchorStatus = "closed"),
                                menuChildren:
                                [
                                    new MenuItemButton(
                                        child: new Text("Run action"),
                                        shortcut: new SingleActivator(LogicalKeyboardKey.KeyR, control: true),
                                        onPressed: () => SetState(() => _anchorStatus = "activated")),
                                    new MenuItemButton(
                                        child: new Text("Disabled item"),
                                        shortcut: new SingleActivator(LogicalKeyboardKey.Escape)),
                                    new MenuItemButton(
                                        child: new Text("Keep open"),
                                        shortcut: new SingleActivator(LogicalKeyboardKey.ArrowRight, shift: true),
                                        closeOnActivate: false,
                                        onPressed: () => SetState(() => _anchorStatus = "kept open")),
                                    new CheckboxMenuButton(
                                        _menuCheckbox,
                                        value => SetState(() => _menuCheckbox = value),
                                        new Text("Pin menu"),
                                        shortcut: new CharacterActivator("p", control: true),
                                        closeOnActivate: false),
                                    new RadioMenuButton<string>(
                                        "one",
                                        _menuRadio,
                                        value => SetState(() => _menuRadio = value),
                                        new Text("Layout one"),
                                        closeOnActivate: false),
                                    new RadioMenuButton<string>(
                                        "two",
                                        _menuRadio,
                                        value => SetState(() => _menuRadio = value),
                                        new Text("Layout two"),
                                        closeOnActivate: false),
                                ],
                                builder: (_, controller, _) => new TextButton(
                                    new Text(controller.IsOpen ? "Close menu" : "Open menu"),
                                    () =>
                                    {
                                        if (controller.IsOpen) controller.Close(); else controller.Open();
                                    }))),
                        new Text($"Anchor menu: {_anchorStatus}", style: new TextStyle(FontSize: 13)),
                        new Text(
                            "Menu choices: pinned="
                            + (_menuCheckbox switch { true => "true", false => "false", null => "null" })
                            + $", layout={_menuRadio}",
                            style: new TextStyle(FontSize: 13)),
                        new Divider(),
                        new Text("MenuStyle alignment", style: new TextStyle(FontSize: 18)),
                        new Text(
                            "MenuStyle.alignment places the panel within the anchor rect; menu items"
                            + " space their leading and trailing icons by visual density.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                new TextButton(
                                    new Text("Bottom start"),
                                    () => SetState(() =>
                                        _menuAlignment = AlignmentDirectional.BottomStart)),
                                new TextButton(
                                    new Text("Top end"),
                                    () => SetState(() => _menuAlignment = AlignmentDirectional.TopEnd)),
                            ]),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuAnchor(
                                controller: _alignedMenuController,
                                style: new MenuStyle(alignment: _menuAlignment),
                                menuChildren:
                                [
                                    new MenuItemButton(
                                        child: new Text("Cut"),
                                        leadingIcon: new Icon(Icons.ChevronLeft, size: 18),
                                        onPressed: () => SetState(() => _alignedMenuStatus = "cut")),
                                    new MenuItemButton(
                                        child: new Text("Copy"),
                                        trailingIcon: new Icon(Icons.ChevronRight, size: 18),
                                        onPressed: () => SetState(() => _alignedMenuStatus = "copy")),
                                ],
                                builder: (_, controller, _) => new TextButton(
                                    new Text(controller.IsOpen ? "Close aligned menu" : "Open aligned menu"),
                                    () =>
                                    {
                                        if (controller.IsOpen) controller.Close(); else controller.Open();
                                    }))),
                        new Text($"Aligned menu: {_alignedMenuStatus}", style: new TextStyle(FontSize: 13)),
                        new Divider(),
                        new Text("MenuStyle surface tokens", style: new TextStyle(FontSize: 18)),
                        new Text(
                            "The panel defaults come straight from the ColorScheme (surfaceContainer "
                            + "fill, shadow role, elevation 3, 4px corners). A MenuStyle.side is folded "
                            + "into the resolved shape, and a fixedSize is clamped by minimumSize and "
                            + "maximumSize before it tightens the panel.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Grey)),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                new TextButton(
                                    new Text(_outlinedMenuSized ? "Drop fixed size" : "Apply fixed size"),
                                    () => SetState(() => _outlinedMenuSized = !_outlinedMenuSized)),
                            ]),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuAnchor(
                                controller: _outlinedMenuController,
                                style: new MenuStyle(
                                    side: WidgetStateProperty<BorderSide?>.All(
                                        new BorderSide(MaterialColors.Pink, 2)),
                                    fixedSize: _outlinedMenuSized
                                        ? WidgetStateProperty<Size?>.All(new Size(420, 200))
                                        : null,
                                    maximumSize: WidgetStateProperty<Size?>.All(new Size(220, 120))),
                                menuChildren:
                                [
                                    new MenuItemButton(child: new Text("Outlined one"), onPressed: () => { }),
                                    new MenuItemButton(child: new Text("Outlined two"), onPressed: () => { }),
                                ],
                                builder: (_, controller, _) => new TextButton(
                                    new Text(controller.IsOpen ? "Close outlined menu" : "Open outlined menu"),
                                    () =>
                                    {
                                        if (controller.IsOpen) controller.Close(); else controller.Open();
                                    }))),
                        new Divider(),
                        new Text("MenuBar + SubmenuButton", style: new TextStyle(FontSize: 18)),
                        new Text(
                            "Horizontal menu bar with controller-owned sibling closing, nested side submenu, "
                            + "local menu themes, Alt-key accelerators, and a focus-local Alt+N override.",
                            style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuTheme(
                                new MenuThemeData(
                                    style: new MenuStyle(
                                        backgroundColor: WidgetStateProperty<Color?>.All(
                                            new Color(0xFFFFF3E0))),
                                    submenuIcon: WidgetStateProperty<Widget?>.All(
                                        new Icon(Icons.InfoOutline))),
                                new MenuBarTheme(
                                    new MenuBarThemeData(new MenuStyle(
                                        backgroundColor: WidgetStateProperty<Color?>.All(
                                            new Color(0xFFF3E5F5)))),
                                    new MenuButtonTheme(
                                    new MenuButtonThemeData(new ButtonStyle(
                                        ForegroundColor: WidgetStateProperty<Color?>.All(MaterialColors.DeepPurple))),
                                    new CallbackShortcuts(
                                        bindings: new Dictionary<ShortcutActivator, Action>
                                        {
                                            [new CharacterActivator("n", alt: true)] = () => SetState(
                                                () => _menuBarStatus = "focus-local Alt+N override"),
                                        },
                                        child: new MenuBar(
                                        children:
                                        [
                                    new SubmenuButton(
                                        [
                                            new MenuItemButton(
                                                child: new MenuAcceleratorLabel("&New document"),
                                                onPressed: () => SetState(() => _menuBarStatus = "new document")),
                                            new SubmenuButton(
                                                [
                                                    new MenuItemButton(
                                                        child: new MenuAcceleratorLabel("&Quarterly report"),
                                                        onPressed: () => SetState(
                                                            () => _menuBarStatus = "recent report")),
                                                ],
                                                new MenuAcceleratorLabel("&Recent"),
                                                onOpen: () => SetState(() => _menuBarStatus = "recent opened")),
                                        ],
                                        new MenuAcceleratorLabel("&File"),
                                        controller: _fileMenuController,
                                        style: new ButtonStyle(
                                            ForegroundColor:
                                                WidgetStateProperty<Color?>.All(MaterialColors.DeepOrange)),
                                        onOpen: () => SetState(() => _menuBarStatus = "file opened"),
                                        onClose: () => SetState(() => _menuBarStatus = "file closed")),
                                    new SubmenuButton(
                                        [
                                            new MenuItemButton(
                                                child: new MenuAcceleratorLabel("&Paste"),
                                                onPressed: () => SetState(() => _menuBarStatus = "paste")),
                                        ],
                                        new MenuAcceleratorLabel("&Edit"),
                                        controller: _editMenuController,
                                        onOpen: () => SetState(() => _menuBarStatus = "edit opened"),
                                        onClose: () => SetState(() => _menuBarStatus = "edit closed")),
                                    new SubmenuButton([], new MenuAcceleratorLabel("&Disabled")),
                                        ])))))),
                        new Text($"Menu bar: {_menuBarStatus}", style: new TextStyle(FontSize: 13)),
                        new Divider(),
                        new Text("Animated MenuAnchor", style: new TextStyle(FontSize: 18)),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new MenuAnchor(
                                controller: _animatedMenuController,
                                animated: true,
                                onAnimationStatusChanged: status =>
                                    SetState(() => _animatedMenuStatus = status.Name()),
                                menuChildren:
                                [
                                    AnimatedMenuItem("First"),
                                    AnimatedMenuItem("Second"),
                                    AnimatedMenuItem("Third"),
                                    AnimatedMenuItem("Fourth"),
                                ],
                                builder: (_, controller, _) => ControlButton(
                                    controller.IsOpen ? "Hide staggered menu" : "Show staggered menu",
                                    () =>
                                    {
                                        if (controller.IsOpen)
                                        {
                                            controller.Close();
                                        }
                                        else
                                        {
                                            controller.Open();
                                        }
                                    }))),
                        new Text(
                            $"Animated menu: {_animatedMenuStatus} / {_animatedMenuPick}",
                            style: new TextStyle(FontSize: 13)),
                        new Divider(),
                        new Text("DropdownMenuFormField + Form", style: new TextStyle(FontSize: 18)),
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
                                    new Wrap(
                                        spacing: 8,
                                        runSpacing: 8,
                                        children:
                                        [
                                            ControlButton("Validate", ValidateModernForm),
                                            ControlButton("Reset", ResetModernForm),
                                        ]),
                                    new Text(
                                        $"Modern form status: {_modernFormStatus}",
                                        style: new TextStyle(
                                            FontSize: 13)),
                                ])),
                        new Divider(),
                        new Text("Disabled fallback", style: new TextStyle(FontSize: 15)),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DropdownButton<string>(
                                items: BuildItems(),
                                onChanged: null,
                                hint: new Text("Fallback hint"),
                                disabledHint: new Text("Disabled hint"))),
                        new Divider(),
                        new Text("DropdownButtonFormField + Form", style: new TextStyle(FontSize: 18)),
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
                                    new Wrap(
                                        spacing: 8,
                                        runSpacing: 8,
                                        children:
                                        [
                                            ControlButton("Validate", ValidateForm),
                                            ControlButton("Reset", ResetForm),
                                        ]),
                                    new Text($"Form status: {_formStatus}", style: new TextStyle(FontSize: 13)),
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

        private static IReadOnlyList<DropdownMenuItem<string>> BuildItems(bool variableRows = false) =>
        [
            new DropdownMenuItem<string>(new Text("None"), value: null),
            new DropdownMenuItem<string>(new Text("One"), value: "one"),
            new DropdownMenuItem<string>(
                variableRows ? new SizedBox(height: 76, child: new Text("Two (tall row)")) : new Text("Two"),
                value: "two"),
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

        private Widget AnimatedMenuItem(string label) => new MenuItemButton(
            child: new Text(label),
            onPressed: () => SetState(() => _animatedMenuPick = label.ToLowerInvariant()));

        private static Widget ControlButton(string label, Action action) =>
            new TextButton(new Text(label, style: new TextStyle(FontSize: 12)), action);
    }
}
