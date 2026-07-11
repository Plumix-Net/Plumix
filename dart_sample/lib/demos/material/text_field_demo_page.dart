import 'package:flutter/material.dart';

class TextFieldDemoPage extends StatefulWidget {
  const TextFieldDemoPage({super.key});

  @override
  State<TextFieldDemoPage> createState() => _TextFieldDemoPageState();
}

class _TextFieldDemoPageState extends State<TextFieldDemoPage> {
  final TextEditingController _email = TextEditingController();
  final TextEditingController _password = TextEditingController();
  final TextEditingController _notes = TextEditingController();
  final TextEditingController _readOnly = TextEditingController(
    text: 'Read-only value',
  );
  final TextEditingController _formName = TextEditingController();
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  bool _enabled = true;
  bool _obscure = true;
  bool _error = false;
  String _submitted = 'none';
  String _formStatus = 'not validated';

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'InputDecorator + TextField',
            style: TextStyle(fontSize: 20),
          ),
          const Text(
            'Filled/outlined/state-aware borders, floating labels, hint/helper/error/counter slots, prefix/suffix icons, focus, submit, read-only and multiline input.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: () => setState(() => _enabled = !_enabled),
                child: Text(_enabled ? 'Enabled' : 'Disabled'),
              ),
              TextButton(
                onPressed: () => setState(() => _obscure = !_obscure),
                child: Text(_obscure ? 'Reveal' : 'Hide'),
              ),
              TextButton(
                onPressed: () => setState(() => _error = !_error),
                child: Text(_error ? 'Clear error' : 'Show error'),
              ),
            ],
          ),
          TextField(
            controller: _email,
            enabled: _enabled,
            maxLength: 32,
            decoration: const InputDecoration(
              labelText: 'Email',
              hintText: 'name@example.com',
              helperText: 'Filled Material field',
              prefixIcon: Icon(Icons.email),
              suffixText: '.com',
              filled: true,
            ),
            onSubmitted: (String value) => setState(() => _submitted = value),
          ),
          TextField(
            controller: _password,
            enabled: _enabled,
            obscureText: _obscure,
            decoration: InputDecoration(
              labelText: 'Password',
              errorText: _error ? 'At least 8 characters' : null,
              prefixIcon: const Icon(Icons.lock),
              suffixIcon: Icon(
                _obscure ? Icons.visibility : Icons.visibility_off,
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
          ),
          TextField(
            controller: _notes,
            enabled: _enabled,
            minLines: 3,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Notes',
              alignLabelWithHint: true,
              border: OutlineInputBorder(),
              helperText: 'Multiline EditableText path',
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: InputDecoration(
              labelText: 'State-aware border',
              hintText: 'Focus or hover this field',
              errorText: _error ? 'Error state' : null,
              border: WidgetStateInputBorder.resolveWith(_resolveStateBorder),
              helperText: 'Resolves focus, hover, error, and disabled together',
            ),
          ),
          TextField(
            controller: _readOnly,
            readOnly: true,
            decoration: const InputDecoration.collapsed(hintText: 'Read only'),
          ),
          Text(
            'Last submitted email: $_submitted',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text('TextFormField + Form', style: TextStyle(fontSize: 18)),
          Form(
            key: _formKey,
            autovalidateMode: AutovalidateMode.onUserInteraction,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 8,
              children: <Widget>[
                TextFormField(
                  controller: _formName,
                  decoration: const InputDecoration(
                    labelText: 'Display name',
                    helperText: 'Required form field',
                    border: OutlineInputBorder(),
                  ),
                  validator: (String? value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Enter a display name';
                    }
                    return value.length < 3
                        ? 'Use at least 3 characters'
                        : null;
                  },
                  onSaved: (String? value) =>
                      setState(() => _formStatus = 'saved: $value'),
                ),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: <Widget>[
                    _control('Validate', _validateForm),
                    _control('Save', _saveForm),
                    _control('Reset', _resetForm),
                  ],
                ),
                Text(
                  'Form status: $_formStatus',
                  style: const TextStyle(fontSize: 13),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    _notes.dispose();
    _readOnly.dispose();
    _formName.dispose();
    super.dispose();
  }

  void _validateForm() {
    final bool valid = _formKey.currentState?.validate() ?? false;
    setState(() => _formStatus = valid ? 'valid' : 'invalid');
  }

  void _saveForm() {
    final FormState? form = _formKey.currentState;
    if (form?.validate() != true) {
      setState(() => _formStatus = 'invalid');
      return;
    }
    form!.save();
  }

  void _resetForm() {
    _formKey.currentState?.reset();
    setState(() => _formStatus = 'reset');
  }

  InputBorder _resolveStateBorder(Set<WidgetState> states) {
    final Color color = states.contains(WidgetState.disabled)
        ? Colors.grey
        : states.contains(WidgetState.error)
        ? Colors.red
        : states.contains(WidgetState.focused)
        ? Colors.blue
        : states.contains(WidgetState.hovered)
        ? Colors.green
        : Colors.blueGrey;
    final double width = states.contains(WidgetState.focused) ? 3 : 1;
    return OutlineInputBorder(
      borderSide: BorderSide(color: color, width: width),
      borderRadius: BorderRadius.circular(10),
    );
  }

  Widget _control(String label, VoidCallback onPressed) => TextButton(
    onPressed: onPressed,
    child: Text(label, style: const TextStyle(fontSize: 12)),
  );
}
