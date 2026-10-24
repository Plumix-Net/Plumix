using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/text_field_demo_page.dart

public sealed class TextFieldDemoPage : StatefulWidget
{
    public override State CreateState() => new TextFieldDemoPageState();

    private sealed class TextFieldDemoPageState : State
    {
        private readonly TextEditingController _email = new();
        private readonly TextEditingController _password = new();
        private readonly TextEditingController _notes = new();
        private readonly TextEditingController _readOnly = new("Read-only value");
        private readonly TextEditingController _formName = new();
        private readonly TextEditingController _systemMenu = new("Select a word, then open the menu");
        private readonly LabeledGlobalKey<FormState> _formKey = new("text-form");
        private bool _enabled = true;
        private bool _obscure = true;
        private bool _error;
        private bool _maintainHintSize = true;
        private string _submitted = "none";
        private string _formStatus = "not validated";
        private string _systemMenuAction = "none";

        public override Widget Build(BuildContext context) => new SingleChildScrollView(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("InputDecorator + TextField", style: new TextStyle(FontSize: 20)),
                    new Text(
                        "Filled/outlined/state-aware borders, floating labels, hint/helper/error/counter slots, "
                        + "prefix/suffix icons, pointer selection, adaptive context menus, read-only and "
                        + "multiline input.",
                        style: new TextStyle(FontSize: 14, Color: Colors.DimGray)),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            Control(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                            Control(_obscure ? "Reveal" : "Hide", () => SetState(() => _obscure = !_obscure)),
                            Control(_error ? "Clear error" : "Show error", () => SetState(() => _error = !_error)),
                        ]),
                    new TextField(
                        controller: _email,
                        enabled: _enabled,
                        maxLength: 32,
                        decoration: new InputDecoration(
                            labelText: "Email",
                            hintText: "name@example.com",
                            helperText: "Filled Material field",
                            prefixIcon: new Icon(Icons.Email),
                            suffixText: ".com",
                            filled: true),
                        onSubmitted: value => SetState(() => _submitted = value)),
                    new TextField(
                        controller: _password,
                        enabled: _enabled,
                        obscureText: _obscure,
                        decoration: new InputDecoration(
                            labelText: "Password",
                            errorText: _error ? "At least 8 characters" : null,
                            prefixIcon: new Icon(Icons.Lock),
                            suffixIcon: new Icon(_obscure ? Icons.Visibility : Icons.VisibilityOff),
                            border: new OutlineInputBorder(borderRadius: BorderRadius.Circular(12)))),
                    new TextField(
                        controller: _notes,
                        enabled: _enabled,
                        minLines: 3,
                        maxLines: 3,
                        decoration: new InputDecoration(
                            labelText: "Notes",
                            alignLabelWithHint: true,
                            border: new OutlineInputBorder(),
                            helperText: "Multiline EditableText path")),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            labelText: "State-aware border",
                            hintText: "Focus or hover this field",
                            errorText: _error ? "Error state" : null,
                            border: MaterialStateOutlineInputBorder.ResolveWith(ResolveStateBorder),
                            helperText: "Resolves focus, hover, error, and disabled together")),
                    new TextField(
                        controller: _readOnly,
                        readOnly: true,
                        decoration: InputDecoration.Collapsed("Read only")),
                    new Text("Decorator geometry probes", style: new TextStyle(FontSize: 18)),
                    Control(
                        _maintainHintSize ? "Hint space: retained" : "Hint space: collapses",
                        () => SetState(() => _maintainHintSize = !_maintainHintSize)),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            hint: new Text(
                                "Custom multiline hint\nType to fade this hint away\nClear text to restore the hint"),
                            maintainHintSize: _maintainHintSize,
                            hintFadeDuration: TimeSpan.FromMilliseconds(120),
                            helperText: "Toggle hint space, then enter and clear text",
                            filled: true)),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            filled: true,
                            labelText: "Affix icon buttons",
                            prefixIconColor: MaterialColors.Teal,
                            suffixIconColor: MaterialColors.Indigo,
                            prefixIcon: new IconButton(
                                new Icon(Icons.Email),
                                () => SetState(() => _submitted = "prefix button"),
                                tooltip: "Prefix action"),
                            suffixIcon: new IconButton(
                                new Icon(Icons.Visibility),
                                () => SetState(() => _submitted = "suffix button"),
                                tooltip: "Suffix action"),
                            counterText: "3/10",
                            semanticCounterText: "7 characters remaining")),
                    new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 280,
                            child: new TextField(
                                enabled: _enabled,
                                decoration: new InputDecoration(
                                    labelText: "A long floating label can use the full outline width",
                                    floatingLabelBehavior: FloatingLabelBehavior.Always,
                                    border: new OutlineInputBorder())))),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            labelText: "Centered floating label",
                            hintText: "Gap follows the label",
                            floatingLabelAlignment: FloatingLabelAlignment.Center,
                            floatingLabelBehavior: FloatingLabelBehavior.Always,
                            border: new OutlineInputBorder())),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            labelText: "Shaped border",
                            hintText: "Any ShapeBorder as the input outline",
                            helperText: "ShapedInputBorder cuts the label gap out of the shape",
                            border: new ShapedInputBorder(
                                new StadiumBorder(),
                                new BorderSide(Colors.Indigo, 2.0)))),
                    new TextField(
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            labelText: "Dense + compact density",
                            helperText: "isDense with VisualDensity.Compact",
                            isDense: true,
                            visualDensity: VisualDensity.Compact,
                            filled: true)),
                    new SizedBox(
                        height: 96,
                        child: new TextField(
                            enabled: _enabled,
                            expands: true,
                            maxLines: null,
                            textAlignVertical: TextAlignVertical.Bottom,
                            decoration: new InputDecoration(
                                labelText: "Expanded, bottom aligned",
                                filled: true))),
                    new Text("Ambient InputDecorationTheme probes", style: new TextStyle(FontSize: 18)),
                    new Text(
                        "The theme's fill, floating label and active indicator resolve per state, and "
                        + "an ambient IconButtonTheme colors the affix icons.",
                        style: new TextStyle(FontSize: 13, Color: Colors.DimGray)),
                    new InputDecorationTheme(
                        data: new InputDecorationThemeData(
                            filled: true,
                            fillColor: WidgetStateColor.ResolveWith(ResolveStateFill),
                            floatingLabelStyle: WidgetStateTextStyle.ResolveWith(ResolveStateLabel),
                            activeIndicatorBorder: WidgetStateBorderSide.ResolveWith(ResolveStateIndicator)),
                        child: new IconButtonTheme(
                            new IconButtonThemeData(new ButtonStyle(
                                ForegroundColor: WidgetStateProperty<Color?>.ResolveWith(ResolveAffixColor))),
                            new TextField(
                                enabled: _enabled,
                                decoration: new InputDecoration(
                                    labelText: "State-resolving theme",
                                    hintText: "Focus, hover or break this field",
                                    errorText: _error ? "Error state" : null,
                                    prefixIcon: new Icon(Icons.Lock),
                                    suffixIcon: new Icon(Icons.Visibility),
                                    helperText: "fillColor/floatingLabelStyle/activeIndicatorBorder")))),
                    new TextField(
                        enabled: _enabled,
                        style: new DemoInputTextStyle(),
                        decoration: new InputDecoration(
                            labelText: "Stateful input style",
                            hintText: "Focus to change weight and color",
                            errorText: _error ? "Error state" : null,
                            helperText: "Subclass input style + mapped helper style",
                            helperStyle: WidgetStateTextStyle.FromMap(
                            [
                                new(WidgetState.Disabled, new TextStyle(Color: MaterialColors.Grey)),
                                new(WidgetState.Focused, new TextStyle(Color: MaterialColors.Blue)),
                                new(WidgetStatesConstraint.Any, new TextStyle(Color: Colors.SlateGray)),
                            ]))),
                    new Text($"Last submitted email: {_submitted}", style: new TextStyle(FontSize: 13)),
                    new Divider(),
                    new Text("System context menu", style: new TextStyle(FontSize: 18)),
                    new Text(
                        $"System menu supported here: {(SystemContextMenu.IsSupported(context) ? "true" : "false")} "
                        + "(iOS 16+ only; elsewhere the Flutter-drawn toolbar shows)",
                        style: new TextStyle(FontSize: 13, Color: MaterialColors.Black54)),
                    new TextField(
                        controller: _systemMenu,
                        enabled: _enabled,
                        decoration: new InputDecoration(
                            labelText: "Custom system menu items",
                            helperText: "Copy, Paste and a custom \"Shout\" action",
                            border: new OutlineInputBorder()),
                        contextMenuBuilder: BuildSystemMenu),
                    new Text($"Last system menu action: {_systemMenuAction}", style: new TextStyle(FontSize: 13)),
                    new Divider(),
                    new Text("TextFormField + Form", style: new TextStyle(FontSize: 18)),
                    new Form(
                        key: _formKey,
                        autovalidateMode: AutovalidateMode.OnUserInteraction,
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new TextFormField(
                                    controller: _formName,
                                    decoration: new InputDecoration(
                                        labelText: "Display name",
                                        helperText: "Required form field",
                                        border: new OutlineInputBorder()),
                                    validator: value => string.IsNullOrWhiteSpace(value)
                                        ? "Enter a display name"
                                        : value.Length < 3 ? "Use at least 3 characters" : null,
                                    onSaved: value => SetState(() => _formStatus = $"saved: {value}")),
                                new Row(
                                    spacing: 8,
                                    children:
                                    [
                                        Control("Validate", ValidateForm),
                                        Control("Save", SaveForm),
                                        Control("Reset", ResetForm),
                                    ]),
                                new Text($"Form status: {_formStatus}", style: new TextStyle(FontSize: 13)),
                            ])),
                ]));

        public override void Dispose()
        {
            _email.Dispose(); _password.Dispose(); _notes.Dispose(); _readOnly.Dispose(); _formName.Dispose();
            _systemMenu.Dispose();

            base.Dispose();
        }

        private Widget BuildSystemMenu(BuildContext context, EditableText.EditableTextState editableTextState)
        {
            if (!SystemContextMenu.IsSupportedByField(editableTextState))
            {
                return AdaptiveTextSelectionToolbar.EditableText(editableTextState);
            }

            return SystemContextMenu.EditableText(
                editableTextState,
                items:
                [
                    new IOSSystemContextMenuItemCopy(),
                    new IOSSystemContextMenuItemPaste(),
                    new IOSSystemContextMenuItemCustom(title: "Shout", onPressed: Shout),
                ]);
        }

        private void Shout()
        {
            SetState(() =>
            {
                _systemMenu.Text = _systemMenu.Text.ToUpperInvariant();
                _systemMenuAction = "shout";
            });
        }

        private void ValidateForm()
        {
            bool valid = _formKey.CurrentState?.Validate() == true;
            SetState(() => _formStatus = valid ? "valid" : "invalid");
        }

        private void SaveForm()
        {
            var form = _formKey.CurrentState;
            if (form?.Validate() != true)
            {
                SetState(() => _formStatus = "invalid");
                return;
            }
            form.Save();
        }

        private void ResetForm()
        {
            _formKey.CurrentState?.Reset();
            SetState(() => _formStatus = "reset");
        }

        private static InputBorder ResolveStateBorder(IReadOnlySet<WidgetState> states)
        {
            Color color = states.Contains(WidgetState.Disabled)
                ? Colors.Gray
                : states.Contains(WidgetState.Error)
                    ? Colors.Crimson
                    : states.Contains(WidgetState.Focused)
                        ? Colors.DodgerBlue
                        : states.Contains(WidgetState.Hovered)
                            ? Colors.MediumSeaGreen
                            : Colors.SlateGray;
            double width = states.Contains(WidgetState.Focused) ? 3 : 1;
            return new OutlineInputBorder(new BorderSide(color, width), BorderRadius.Circular(10));
        }

        private static Color ResolveStateFill(IReadOnlySet<WidgetState> states)
        {
            if (states.Contains(WidgetState.Disabled)) return Colors.Gainsboro;
            if (states.Contains(WidgetState.Error)) return Colors.MistyRose;
            if (states.Contains(WidgetState.Focused)) return Colors.AliceBlue;
            if (states.Contains(WidgetState.Hovered)) return Colors.Honeydew;
            return Colors.WhiteSmoke;
        }

        private static TextStyle ResolveStateLabel(IReadOnlySet<WidgetState> states) => new(
            Color: states.Contains(WidgetState.Error) ? Colors.Crimson
                : states.Contains(WidgetState.Focused) ? Colors.DodgerBlue
                : Colors.SlateGray);

        private static BorderSide? ResolveStateIndicator(IReadOnlySet<WidgetState> states) => new BorderSide(
            states.Contains(WidgetState.Error) ? Colors.Crimson
                : states.Contains(WidgetState.Focused) ? Colors.DodgerBlue
                : states.Contains(WidgetState.Hovered) ? Colors.MediumSeaGreen
                : Colors.SlateGray,
            states.Contains(WidgetState.Focused) ? 3 : 1);

        private static Color? ResolveAffixColor(IReadOnlySet<WidgetState> states) =>
            states.Contains(WidgetState.Error) ? Colors.Crimson : Colors.Indigo;

        private static Widget Control(string label, Action action) => new TextButton(new Text(label), action);
    }
    private sealed class DemoInputTextStyle : WidgetStateTextStyle
    {
        public override TextStyle Resolve(IReadOnlySet<WidgetState> states) => new(
            Color: states.Contains(WidgetState.Disabled) ? Colors.Gray
                : states.Contains(WidgetState.Error) ? Colors.Crimson
                : states.Contains(WidgetState.Focused) ? Colors.DodgerBlue : Colors.SlateGray,
            FontWeight: states.Contains(WidgetState.Focused)
                ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal);
    }
}
