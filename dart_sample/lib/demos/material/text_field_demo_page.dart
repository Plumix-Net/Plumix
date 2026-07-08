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
  bool _enabled = true;
  bool _obscure = true;
  bool _error = false;
  String _submitted = 'none';

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
            'Filled/outlined borders, floating labels, hint/helper/error/counter slots, prefix/suffix icons, focus, submit, read-only and multiline input.',
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
            controller: _readOnly,
            readOnly: true,
            decoration: const InputDecoration.collapsed(hintText: 'Read only'),
          ),
          Text(
            'Last submitted email: $_submitted',
            style: const TextStyle(fontSize: 13),
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
    super.dispose();
  }
}
