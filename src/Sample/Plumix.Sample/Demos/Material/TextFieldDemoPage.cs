using System;
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
        private readonly LabeledGlobalKey<FormState> _formKey = new("text-form");
        private bool _enabled = true;
        private bool _obscure = true;
        private bool _error;
        private string _submitted = "none";
        private string _formStatus = "not validated";

        public override Widget Build(BuildContext context) => new SingleChildScrollView(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("InputDecorator + TextField", fontSize: 20),
                    new Text(
                        "Filled/outlined/state-aware borders, floating labels, hint/helper/error/counter slots, "
                        + "prefix/suffix icons, pointer selection, adaptive context menus, read-only and "
                        + "multiline input.",
                        fontSize: 14, color: Colors.DimGray),
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
                    new Text("Decorator geometry probes", fontSize: 18),
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
                    new Text($"Last submitted email: {_submitted}", fontSize: 13),
                    new Divider(),
                    new Text("TextFormField + Form", fontSize: 18),
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
                                new Text($"Form status: {_formStatus}", fontSize: 13),
                            ])),
                ]));

        public override void Dispose()
        {
            _email.Dispose(); _password.Dispose(); _notes.Dispose(); _readOnly.Dispose(); _formName.Dispose();
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

        private static InputBorder ResolveStateBorder(MaterialState states)
        {
            Color color = states.HasFlag(MaterialState.Disabled)
                ? Colors.Gray
                : states.HasFlag(MaterialState.Error)
                    ? Colors.Crimson
                    : states.HasFlag(MaterialState.Focused)
                        ? Colors.DodgerBlue
                        : states.HasFlag(MaterialState.Hovered)
                            ? Colors.MediumSeaGreen
                            : Colors.SlateGray;
            double width = states.HasFlag(MaterialState.Focused) ? 3 : 1;
            return new OutlineInputBorder(new BorderSide(color, width), BorderRadius.Circular(10));
        }

        private static Widget Control(string label, Action action) => new TextButton(new Text(label, fontSize: 12), action);
    }
}
