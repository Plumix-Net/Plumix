import 'package:flutter/material.dart';

class EditableTextDemoPage extends StatefulWidget {
  const EditableTextDemoPage({super.key});

  @override
  State<EditableTextDemoPage> createState() => _EditableTextDemoPageState();
}

class _EditableTextDemoPageState extends State<EditableTextDemoPage> {
  late final TextEditingController _nameController;
  late final TextEditingController _notesController;
  bool _enabled = true;
  String _lastChange = '(none)';

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _notesController = TextEditingController();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final int notesLineCount = _countLines(_notesController.text);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'EditableText + Focus/IME',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Baseline input + multiline: Enter adds new line in Notes; ArrowUp/ArrowDown moves caret between lines.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            SizedBox(
              width: 120,
              child: _MenuButton(
                label: _enabled ? 'Disable' : 'Enable',
                onTap: () => setState(() => _enabled = !_enabled),
                background: const Color(0xFFDCE3ED),
              ),
            ),
            SizedBox(
              width: 120,
              child: _MenuButton(
                label: 'Clear',
                onTap: () {
                  setState(() {
                    _nameController.clear();
                    _notesController.clear();
                    _lastChange = '(cleared)';
                  });
                },
                background: const Color(0xFFE9F5EC),
              ),
            ),
          ],
        ),
        SizedBox(
          width: 170,
          child: _MenuButton(
            label: 'Seed notes',
            onTap: () {
              setState(() {
                _notesController.text = 'First line\nSecond line\nThird line';
                _lastChange = '(seeded notes)';
              });
            },
            background: const Color(0xFFF3E8D8),
          ),
        ),
        Text(
          'last change: $_lastChange',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        const Text(
          'Name',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        _buildTextField(
          controller: _nameController,
          placeholder: 'Type your name',
          onChanged: (String value) =>
              setState(() => _lastChange = 'name = $value'),
        ),
        const Text(
          'Notes (multiline)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        _buildTextField(
          controller: _notesController,
          multiline: true,
          placeholder: 'Type notes (Enter creates new line)',
          onChanged: (String value) => setState(
            () => _lastChange = 'notes = ${_escapeMultiline(value)}',
          ),
        ),
        Text(
          'notes lines: $notesLineCount',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Text(
          "current: name='${_nameController.text}', notes='${_escapeMultiline(_notesController.text)}'",
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ],
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String placeholder,
    required ValueChanged<String> onChanged,
    bool multiline = false,
  }) {
    return TextField(
      controller: controller,
      enabled: _enabled,
      maxLines: multiline ? null : 1,
      onChanged: onChanged,
      decoration: InputDecoration(
        hintText: placeholder,
        isDense: true,
        filled: true,
        fillColor: _enabled ? const Color(0xFFE8F0FE) : const Color(0xFFF5F5F5),
        border: const OutlineInputBorder(),
      ),
    );
  }

  String _escapeMultiline(String value) {
    return value.replaceAll('\r', '').replaceAll('\n', r'\n');
  }

  int _countLines(String value) {
    if (value.isEmpty) {
      return 1;
    }

    final String normalized = value.replaceAll('\r', '');
    return '\n'.allMatches(normalized).length + 1;
  }
}

class _MenuButton extends StatelessWidget {
  const _MenuButton({
    required this.label,
    required this.onTap,
    required this.background,
  });

  final String label;
  final VoidCallback onTap;
  final Color background;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        color: background,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }
}
