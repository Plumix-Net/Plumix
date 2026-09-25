import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

class EditableTextDemoPage extends StatefulWidget {
  const EditableTextDemoPage({super.key});

  @override
  State<EditableTextDemoPage> createState() => _EditableTextDemoPageState();
}

class _EditableTextDemoPageState extends State<EditableTextDemoPage> {
  late final TextEditingController _nameController;
  late final TextEditingController _notesController;
  late final TextEditingController _pinController;
  late final TextEditingController _caretController;
  late final TextEditingController _longLineController;
  late final TextEditingController _scrollingNotesController;
  late final TextEditingController _undoableController;
  late final UndoHistoryController _undoController;
  bool _enabled = true;
  String _lastChange = '(none)';

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _notesController = TextEditingController();
    _pinController = TextEditingController();
    _caretController = TextEditingController(text: 'Wide rounded caret');
    _longLineController = TextEditingController(
      text:
          'This single line is far wider than its field, so typing at the end scrolls to the caret',
    );
    _scrollingNotesController = TextEditingController(
      text: 'Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8',
    );
    _undoableController = TextEditingController();
    _undoController = UndoHistoryController();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _notesController.dispose();
    _pinController.dispose();
    _caretController.dispose();
    _longLineController.dispose();
    _scrollingNotesController.dispose();
    _undoableController.dispose();
    _undoController.dispose();
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
        const Text(
          'PIN (obscured)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        _buildTextField(
          controller: _pinController,
          placeholder: 'Type a PIN',
          obscureText: true,
          onChanged: (String value) =>
              setState(() => _lastChange = 'pin length = ${value.length}'),
        ),
        const Text(
          'Caret (width 3, radius 2)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        _buildTextField(
          controller: _caretController,
          placeholder: '',
          cursorWidth: 3,
          cursorRadius: const Radius.circular(2),
          cursorColor: const Color(0xFFD81B60),
          onChanged: (String value) =>
              setState(() => _lastChange = 'caret = $value'),
        ),
        const Text(
          'Long line (scrolls to the caret)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: SizedBox(
            width: 220,
            child: _buildTextField(
              controller: _longLineController,
              placeholder: '',
              onChanged: (String value) =>
                  setState(() => _lastChange = 'long line = $value'),
            ),
          ),
        ),
        const Text(
          'Notes (3 lines, scrolls)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        _buildTextField(
          controller: _scrollingNotesController,
          placeholder: '',
          multiline: true,
          maxLines: 3,
          onChanged: (String value) => setState(
            () => _lastChange = 'scrolling notes = ${_escapeMultiline(value)}',
          ),
        ),
        const Text(
          'Undo history (a formatter upper-cases input)',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        ValueListenableBuilder<UndoHistoryValue>(
          valueListenable: _undoController,
          builder:
              (BuildContext context, UndoHistoryValue value, Widget? child) {
                return Row(
                  spacing: 8,
                  children: <Widget>[
                    SizedBox(
                      width: 120,
                      child: _MenuButton(
                        label: value.canUndo ? 'Undo' : 'Undo (-)',
                        onTap: _undoController.undo,
                        background: const Color(0xFFDCE3ED),
                      ),
                    ),
                    SizedBox(
                      width: 120,
                      child: _MenuButton(
                        label: value.canRedo ? 'Redo' : 'Redo (-)',
                        onTap: _undoController.redo,
                        background: const Color(0xFFDCE3ED),
                      ),
                    ),
                  ],
                );
              },
        ),
        _buildTextField(
          controller: _undoableController,
          placeholder: 'Type, then undo/redo (Ctrl+Z / Ctrl+Shift+Z)',
          undoController: _undoController,
          inputFormatters: <TextInputFormatter>[_upperCaseFormatter],
          onChanged: (String value) =>
              setState(() => _lastChange = 'undoable = $value'),
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
    int? maxLines,
    bool obscureText = false,
    double cursorWidth = 2.0,
    Radius? cursorRadius,
    Color? cursorColor,
    UndoHistoryController? undoController,
    List<TextInputFormatter>? inputFormatters,
  }) {
    return TextField(
      controller: controller,
      undoController: undoController,
      inputFormatters: inputFormatters,
      enabled: _enabled,
      maxLines: maxLines ?? (multiline ? null : 1),
      obscureText: obscureText,
      cursorWidth: cursorWidth,
      cursorRadius: cursorRadius,
      cursorColor: cursorColor,
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

  static final TextInputFormatter _upperCaseFormatter =
      TextInputFormatter.withFunction(
        (TextEditingValue oldValue, TextEditingValue newValue) =>
            newValue.copyWith(text: newValue.text.toUpperCase()),
      );

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
