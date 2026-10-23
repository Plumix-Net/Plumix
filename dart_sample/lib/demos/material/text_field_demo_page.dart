import 'package:material_ui/material_ui.dart';

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
  final TextEditingController _systemMenu = TextEditingController(
    text: 'Select a word, then open the menu',
  );
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  bool _enabled = true;
  bool _obscure = true;
  bool _error = false;
  bool _maintainHintSize = true;
  String _submitted = 'none';
  String _formStatus = 'not validated';
  String _systemMenuAction = 'none';

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
            'Filled/outlined/state-aware borders, floating labels, hint/helper/error/counter slots, '
            'prefix/suffix icons, pointer selection, adaptive context menus, read-only and multiline input.',
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
          const Text(
            'Decorator geometry probes',
            style: TextStyle(fontSize: 18),
          ),
          TextButton(
            onPressed: () =>
                setState(() => _maintainHintSize = !_maintainHintSize),
            child: Text(
              _maintainHintSize
                  ? 'Hint space: retained'
                  : 'Hint space: collapses',
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: InputDecoration(
              hint: const Text(
                'Custom multiline hint\nType to fade this hint away\nClear text to restore the hint',
              ),
              maintainHintSize: _maintainHintSize,
              hintFadeDuration: const Duration(milliseconds: 120),
              helperText: 'Toggle hint space, then enter and clear text',
              filled: true,
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: InputDecoration(
              filled: true,
              labelText: 'Affix icon buttons',
              prefixIconColor: Colors.teal,
              suffixIconColor: Colors.indigo,
              prefixIcon: IconButton(
                icon: const Icon(Icons.email),
                onPressed: () => setState(() => _submitted = 'prefix button'),
                tooltip: 'Prefix action',
              ),
              suffixIcon: IconButton(
                icon: const Icon(Icons.visibility),
                onPressed: () => setState(() => _submitted = 'suffix button'),
                tooltip: 'Suffix action',
              ),
              counterText: '3/10',
              semanticCounterText: '7 characters remaining',
            ),
          ),
          Align(
            alignment: Alignment.topLeft,
            child: SizedBox(
              width: 280,
              child: TextField(
                enabled: _enabled,
                decoration: const InputDecoration(
                  labelText:
                      'A long floating label can use the full outline width',
                  floatingLabelBehavior: FloatingLabelBehavior.always,
                  border: OutlineInputBorder(),
                ),
              ),
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: const InputDecoration(
              labelText: 'Centered floating label',
              hintText: 'Gap follows the label',
              floatingLabelAlignment: FloatingLabelAlignment.center,
              floatingLabelBehavior: FloatingLabelBehavior.always,
              border: OutlineInputBorder(),
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: const InputDecoration(
              labelText: 'Shaped border',
              hintText: 'Any ShapeBorder as the input outline',
              helperText:
                  'ShapedInputBorder cuts the label gap out of the shape',
              border: ShapedInputBorder(
                shape: StadiumBorder(),
                borderSide: BorderSide(color: Colors.indigo, width: 2.0),
              ),
            ),
          ),
          TextField(
            enabled: _enabled,
            decoration: const InputDecoration(
              labelText: 'Dense + compact density',
              helperText: 'isDense with VisualDensity.compact',
              isDense: true,
              visualDensity: VisualDensity.compact,
              filled: true,
            ),
          ),
          SizedBox(
            height: 96,
            child: TextField(
              enabled: _enabled,
              expands: true,
              maxLines: null,
              textAlignVertical: TextAlignVertical.bottom,
              decoration: const InputDecoration(
                labelText: 'Expanded, bottom aligned',
                filled: true,
              ),
            ),
          ),
          const Text(
            'Ambient InputDecorationTheme probes',
            style: TextStyle(fontSize: 18),
          ),
          const Text(
            "The theme's fill, floating label and active indicator resolve per state, and an "
            'ambient IconButtonTheme colors the affix icons.',
            style: TextStyle(fontSize: 13, color: Colors.black54),
          ),
          InputDecorationTheme(
            data: InputDecorationThemeData(
              filled: true,
              fillColor: WidgetStateColor.resolveWith(_resolveStateFill),
              floatingLabelStyle: WidgetStateTextStyle.resolveWith(
                _resolveStateLabel,
              ),
              activeIndicatorBorder: WidgetStateBorderSide.resolveWith(
                _resolveStateIndicator,
              ),
            ),
            child: IconButtonTheme(
              data: IconButtonThemeData(
                style: ButtonStyle(
                  foregroundColor: WidgetStateProperty.resolveWith(
                    _resolveAffixColor,
                  ),
                ),
              ),
              child: TextField(
                enabled: _enabled,
                decoration: InputDecoration(
                  labelText: 'State-resolving theme',
                  hintText: 'Focus, hover or break this field',
                  errorText: _error ? 'Error state' : null,
                  prefixIcon: const Icon(Icons.lock),
                  suffixIcon: const Icon(Icons.visibility),
                  helperText:
                      'fillColor/floatingLabelStyle/activeIndicatorBorder',
                ),
              ),
            ),
          ),
          TextField(
            enabled: _enabled,
            style: const _DemoInputTextStyle(),
            decoration: InputDecoration(
              labelText: 'Stateful input style',
              hintText: 'Focus to change weight and color',
              errorText: _error ? 'Error state' : null,
              helperText: 'Subclass input style + mapped helper style',
              helperStyle: WidgetStateTextStyle.fromMap(
                const <WidgetStatesConstraint, TextStyle>{
                  WidgetState.disabled: TextStyle(color: Colors.grey),
                  WidgetState.focused: TextStyle(color: Colors.blue),
                  WidgetState.any: TextStyle(color: Colors.blueGrey),
                },
              ),
            ),
          ),
          Text(
            'Last submitted email: $_submitted',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text('System context menu', style: TextStyle(fontSize: 18)),
          Text(
            'System menu supported here: ${SystemContextMenu.isSupported(context)} '
            '(iOS 16+ only; elsewhere the Flutter-drawn toolbar shows)',
            style: const TextStyle(fontSize: 13, color: Colors.black54),
          ),
          TextField(
            controller: _systemMenu,
            enabled: _enabled,
            decoration: const InputDecoration(
              labelText: 'Custom system menu items',
              helperText: 'Copy, Paste and a custom "Shout" action',
              border: OutlineInputBorder(),
            ),
            contextMenuBuilder: _buildSystemMenu,
          ),
          Text(
            'Last system menu action: $_systemMenuAction',
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
    _systemMenu.dispose();
    super.dispose();
  }

  Widget _buildSystemMenu(
    BuildContext context,
    EditableTextState editableTextState,
  ) {
    if (!SystemContextMenu.isSupportedByField(editableTextState)) {
      return AdaptiveTextSelectionToolbar.editableText(
        editableTextState: editableTextState,
      );
    }
    return SystemContextMenu.editableText(
      editableTextState: editableTextState,
      items: <IOSSystemContextMenuItem>[
        const IOSSystemContextMenuItemCopy(),
        const IOSSystemContextMenuItemPaste(),
        IOSSystemContextMenuItemCustom(title: 'Shout', onPressed: _shout),
      ],
    );
  }

  void _shout() {
    setState(() {
      _systemMenu.text = _systemMenu.text.toUpperCase();
      _systemMenuAction = 'shout';
    });
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

  Color _resolveStateFill(Set<WidgetState> states) {
    if (states.contains(WidgetState.disabled)) {
      return const Color(0xFFDCDCDC);
    }
    if (states.contains(WidgetState.error)) {
      return const Color(0xFFFFE4E1);
    }
    if (states.contains(WidgetState.focused)) {
      return const Color(0xFFF0F8FF);
    }
    if (states.contains(WidgetState.hovered)) {
      return const Color(0xFFF0FFF0);
    }
    return const Color(0xFFF5F5F5);
  }

  TextStyle _resolveStateLabel(Set<WidgetState> states) => TextStyle(
    color: states.contains(WidgetState.error)
        ? const Color(0xFFDC143C)
        : states.contains(WidgetState.focused)
        ? const Color(0xFF1E90FF)
        : const Color(0xFF708090),
  );

  BorderSide _resolveStateIndicator(Set<WidgetState> states) => BorderSide(
    color: states.contains(WidgetState.error)
        ? const Color(0xFFDC143C)
        : states.contains(WidgetState.focused)
        ? const Color(0xFF1E90FF)
        : states.contains(WidgetState.hovered)
        ? const Color(0xFF3CB371)
        : const Color(0xFF708090),
    width: states.contains(WidgetState.focused) ? 3 : 1,
  );

  Color _resolveAffixColor(Set<WidgetState> states) =>
      states.contains(WidgetState.error)
      ? const Color(0xFFDC143C)
      : const Color(0xFF4B0082);

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

class _DemoInputTextStyle extends WidgetStateTextStyle {
  const _DemoInputTextStyle();

  @override
  TextStyle resolve(Set<WidgetState> states) => TextStyle(
    color: states.contains(WidgetState.disabled)
        ? Colors.grey
        : states.contains(WidgetState.error)
        ? Colors.red
        : states.contains(WidgetState.focused)
        ? Colors.blue
        : Colors.blueGrey,
    fontWeight: states.contains(WidgetState.focused)
        ? FontWeight.bold
        : FontWeight.normal,
  );
}
