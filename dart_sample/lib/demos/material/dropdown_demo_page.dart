import 'package:flutter/material.dart';

class DropdownDemoPage extends StatefulWidget {
  const DropdownDemoPage({super.key});

  @override
  State<DropdownDemoPage> createState() => _DropdownDemoPageState();
}

class _DropdownDemoPageState extends State<DropdownDemoPage> {
  String? _value = 'two';
  bool _enabled = true;
  bool _dense = false;
  bool _expanded = false;
  bool _hideUnderline = false;
  bool _aligned = false;
  String _status = 'idle';
  String? _formValue;
  String _formStatus = 'not validated';
  String? _modernValue = 'two';
  String _modernStatus = 'idle';
  String? _modernFormValue;
  String _modernFormStatus = 'not validated';
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  final GlobalKey<FormState> _modernFormKey = GlobalKey<FormState>();

  @override
  Widget build(BuildContext context) {
    Widget dropdown = DropdownButton<String>(
      items: _buildItems(),
      onChanged: _enabled
          ? (String? value) => setState(() {
              _value = value;
              _status = 'selected: ${value ?? 'none'}';
            })
          : null,
      selectedItemBuilder: (_) => const <Widget>[
        Text('No selection'),
        Text('Compact one'),
        Text('Compact two'),
        Text('Compact three'),
        Text('Disabled entry'),
      ],
      value: _value,
      hint: const Text('Choose a value'),
      disabledHint: const Text('Dropdown disabled'),
      onTap: () => setState(() => _status = 'opened'),
      isDense: _dense,
      isExpanded: _expanded,
      dropdownColor: Colors.amber.shade50,
      menuMaxHeight: 180,
      borderRadius: BorderRadius.circular(10),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
    );
    if (_hideUnderline) {
      dropdown = DropdownButtonHideUnderline(child: dropdown);
    }
    dropdown = ButtonTheme(alignedDropdown: _aligned, child: dropdown);
    if (_expanded) dropdown = SizedBox(width: 320, child: dropdown);

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 14,
        children: <Widget>[
          const Text(
            'DropdownButton + DropdownMenuItem',
            style: TextStyle(fontSize: 20),
          ),
          const Text(
            'Controlled selection with nullable/disabled entries, selectedItemBuilder, route geometry, keyboard focus, and underline policy.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _controlButton(
                _enabled ? 'Enabled' : 'Disabled',
                () => setState(() => _enabled = !_enabled),
              ),
              _controlButton(
                _dense ? 'Dense' : 'Regular',
                () => setState(() => _dense = !_dense),
              ),
              _controlButton(
                _expanded ? 'Expanded' : 'Compact',
                () => setState(() => _expanded = !_expanded),
              ),
              _controlButton(
                _hideUnderline ? 'Underline off' : 'Underline on',
                () => setState(() => _hideUnderline = !_hideUnderline),
              ),
              _controlButton(
                _aligned ? 'Aligned theme' : 'Unaligned theme',
                () => setState(() => _aligned = !_aligned),
              ),
            ],
          ),
          Align(alignment: Alignment.centerLeft, child: dropdown),
          Text(
            'Value: ${_value ?? 'none'}',
            style: const TextStyle(fontSize: 13),
          ),
          Text('Status: $_status', style: const TextStyle(fontSize: 13)),
          const Divider(),
          const Text(
            'DropdownMenu + DropdownMenuEntry',
            style: TextStyle(fontSize: 18),
          ),
          const Text(
            'Editable Material 3 menu with filtering, search highlighting, disabled-entry traversal, leading/trailing icons, and controller-backed route state.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: DropdownMenu<String>(
              dropdownMenuEntries: _buildModernEntries(),
              initialSelection: _modernValue,
              width: 320,
              menuHeight: 180,
              leadingIcon: const Icon(Icons.search),
              label: const Text('Search a destination'),
              helperText: 'Type to filter, then use arrow keys',
              enableFilter: true,
              onSelected: (String? value) => setState(() {
                _modernValue = value;
                _modernStatus = 'selected: ${value ?? 'none'}';
              }),
            ),
          ),
          Text(
            'Modern value: ${_modernValue ?? 'none'}',
            style: const TextStyle(fontSize: 13),
          ),
          Text(
            'Modern status: $_modernStatus',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text(
            'DropdownMenuFormField + Form',
            style: TextStyle(fontSize: 18),
          ),
          Form(
            key: _modernFormKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 8,
              children: <Widget>[
                DropdownMenuFormField<String>(
                  dropdownMenuEntries: _buildModernEntries(),
                  initialSelection: _modernFormValue,
                  label: const Text('Required destination'),
                  hintText: 'Pick one destination',
                  enableFilter: true,
                  onSelected: (String? value) => setState(() {
                    _modernFormValue = value;
                    _modernFormStatus = 'changed: ${value ?? 'none'}';
                  }),
                  validator: (String? value) =>
                      value == null ? 'Select a destination' : null,
                ),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: <Widget>[
                    _controlButton('Validate', _validateModernForm),
                    _controlButton('Reset', _resetModernForm),
                  ],
                ),
                Text(
                  'Modern form status: $_modernFormStatus',
                  style: const TextStyle(fontSize: 13),
                ),
              ],
            ),
          ),
          const Divider(),
          const Text('Disabled fallback', style: TextStyle(fontSize: 15)),
          Align(
            alignment: Alignment.centerLeft,
            child: DropdownButton<String>(
              items: _buildItems(),
              onChanged: null,
              hint: const Text('Fallback hint'),
              disabledHint: const Text('Disabled hint'),
            ),
          ),
          const Divider(),
          const Text(
            'DropdownButtonFormField + Form',
            style: TextStyle(fontSize: 18),
          ),
          Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 8,
              children: <Widget>[
                DropdownButtonFormField<String>(
                  items: _buildItems(),
                  initialValue: _formValue,
                  onChanged: (String? value) => setState(() {
                    _formValue = value;
                    _formStatus = 'changed: ${value ?? 'none'}';
                  }),
                  decoration: const InputDecoration(
                    labelText: 'Required choice',
                    hintText: 'Pick one item',
                    border: OutlineInputBorder(),
                  ),
                  validator: (String? value) =>
                      value == null ? 'Select an item' : null,
                ),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: <Widget>[
                    _controlButton('Validate', _validateForm),
                    _controlButton('Reset', _resetForm),
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

  void _validateForm() {
    final bool valid = _formKey.currentState?.validate() ?? false;
    setState(() => _formStatus = valid ? 'valid' : 'invalid');
  }

  void _resetForm() {
    _formKey.currentState?.reset();
    setState(() {
      _formValue = null;
      _formStatus = 'reset';
    });
  }

  void _validateModernForm() {
    final bool valid = _modernFormKey.currentState?.validate() ?? false;
    setState(() => _modernFormStatus = valid ? 'valid' : 'invalid');
  }

  void _resetModernForm() {
    _modernFormKey.currentState?.reset();
    setState(() {
      _modernFormValue = null;
      _modernFormStatus = 'reset';
    });
  }

  List<DropdownMenuItem<String>> _buildItems() =>
      const <DropdownMenuItem<String>>[
        DropdownMenuItem<String>(value: null, child: Text('None')),
        DropdownMenuItem<String>(value: 'one', child: Text('One')),
        DropdownMenuItem<String>(value: 'two', child: Text('Two')),
        DropdownMenuItem<String>(value: 'three', child: Text('Three')),
        DropdownMenuItem<String>(
          value: 'disabled',
          enabled: false,
          child: Text('Disabled entry'),
        ),
      ];

  List<DropdownMenuEntry<String>> _buildModernEntries() =>
      const <DropdownMenuEntry<String>>[
        DropdownMenuEntry<String>(
          value: 'one',
          label: 'One',
          leadingIcon: Icon(Icons.star_outline),
        ),
        DropdownMenuEntry<String>(
          value: 'two',
          label: 'Two',
          leadingIcon: Icon(Icons.star),
        ),
        DropdownMenuEntry<String>(
          value: 'three',
          label: 'Three',
          trailingIcon: Icon(Icons.check),
        ),
        DropdownMenuEntry<String>(
          value: 'disabled',
          label: 'Disabled entry',
          enabled: false,
        ),
      ];

  Widget _controlButton(String label, VoidCallback onPressed) => TextButton(
    onPressed: onPressed,
    child: Text(label, style: const TextStyle(fontSize: 12)),
  );
}
