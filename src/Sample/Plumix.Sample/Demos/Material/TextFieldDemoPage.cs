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
        private bool _enabled = true;
        private bool _obscure = true;
        private bool _error;
        private string _submitted = "none";

        public override Widget Build(BuildContext context) => new SingleChildScrollView(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("InputDecorator + TextField", fontSize: 20),
                    new Text("Filled/outlined borders, floating labels, hint/helper/error/counter slots, prefix/suffix icons, focus, submit, read-only and multiline input.",
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
                        controller: _readOnly,
                        readOnly: true,
                        decoration: InputDecoration.Collapsed("Read only")),
                    new Text($"Last submitted email: {_submitted}", fontSize: 13),
                ]));

        public override void Dispose()
        {
            _email.Dispose(); _password.Dispose(); _notes.Dispose(); _readOnly.Dispose();
        }

        private static Widget Control(string label, Action action) => new TextButton(new Text(label, fontSize: 12), action);
    }
}
