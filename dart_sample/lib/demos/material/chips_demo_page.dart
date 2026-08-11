import 'package:flutter/material.dart';

class ChipsDemoPage extends StatefulWidget {
  const ChipsDemoPage({super.key});

  @override
  State<ChipsDemoPage> createState() => _ChipsDemoPageState();
}

class _ChipsDemoPageState extends State<ChipsDemoPage> {
  bool _enabled = true;
  bool _selected = false;
  bool _filterSelected = false;
  bool _inputSelected = false;
  bool _inputVisible = true;
  bool _useLocalTheme = false;
  int _actionCount = 0;
  int _deleteCount = 0;

  @override
  Widget build(BuildContext context) {
    Widget probes = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text('Material chips', style: TextStyle(fontSize: 20)),
        const SizedBox(height: 14),
        const Text(
          'Informational, action, choice, filter, and input chips use Wrap for multi-run layouts, with deletion and a copyWith-derived local ChipTheme override.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            _controlButton(
              _enabled ? 'Enabled' : 'Disabled',
              () => setState(() => _enabled = !_enabled),
            ),
            _controlButton(
              _useLocalTheme ? 'Theme override on' : 'Theme override off',
              () => setState(() => _useLocalTheme = !_useLocalTheme),
            ),
            _controlButton(
              _inputVisible ? 'Remove input' : 'Restore input',
              () => setState(() => _inputVisible = !_inputVisible),
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Action chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            ActionChip(
              label: const Text('Suggest'),
              onPressed: _enabled ? _handleAction : null,
            ),
            ActionChip(
              avatar: const Icon(Icons.star),
              label: const Text('Assist'),
              onPressed: _enabled ? _handleAction : null,
            ),
            ActionChip.elevated(
              label: const Text('Elevated'),
              onPressed: _enabled ? _handleAction : null,
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Informational chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            const Chip(
              avatar: Icon(Icons.info_outline),
              label: Text('Read only'),
            ),
            Chip(
              avatar: const Icon(Icons.info_outline),
              label: const Text('Deletable'),
              onDeleted: _enabled ? _handleDelete : null,
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Choice chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            ChoiceChip(
              label: const Text('Standard'),
              selected: !_selected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _selected = !value)
                  : null,
            ),
            ChoiceChip(
              label: const Text('Selected'),
              selected: _selected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _selected = value)
                  : null,
            ),
            ChoiceChip.elevated(
              avatar: const Icon(Icons.star_outline),
              label: const Text('Elevated'),
              selected: _selected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _selected = value)
                  : null,
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Filter chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            FilterChip(
              avatar: const Icon(Icons.star_outline),
              label: const Text('Favorites'),
              selected: _filterSelected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _filterSelected = value)
                  : null,
            ),
            FilterChip.elevated(
              label: const Text('Elevated'),
              selected: !_filterSelected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _filterSelected = !value)
                  : null,
            ),
            FilterChip(
              label: const Text('Deletable'),
              selected: _filterSelected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _filterSelected = value)
                  : null,
              onDeleted: _enabled ? _handleDelete : null,
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Input chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: <Widget>[
            if (_inputVisible)
              InputChip(
                avatar: const CircleAvatar(child: Text('A')),
                label: const Text('Ada'),
                selected: _inputSelected,
                isEnabled: _enabled,
                onSelected: (bool value) =>
                    setState(() => _inputSelected = value),
                onDeleted: () => setState(() {
                  _inputVisible = false;
                  _deleteCount++;
                }),
              )
            else
              const Text(
                'Input removed',
                style: TextStyle(fontSize: 13, color: Color(0xFF49454F)),
              ),
            InputChip(
              avatar: const Icon(Icons.info_outline),
              label: const Text('Pressable'),
              isEnabled: _enabled,
              onPressed: _handleAction,
            ),
          ],
        ),
        const SizedBox(height: 14),
        Text(
          'Actions: $_actionCount · deletes: $_deleteCount · choice: $_selected · filter: $_filterSelected · input: $_inputSelected',
          style: const TextStyle(fontSize: 13, color: Color(0xFF49454F)),
        ),
      ],
    );

    if (!_useLocalTheme) {
      return probes;
    }

    return ChipTheme(
      data: ChipTheme.of(context).copyWith(
        backgroundColor: const Color(0xFFFFDDB3),
        selectedColor: const Color(0xFF006C4C),
        checkmarkColor: Colors.white,
        labelStyle: const TextStyle(color: Color(0xFF271900)),
        secondaryLabelStyle: const TextStyle(color: Colors.white),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
      child: probes,
    );
  }

  void _handleAction() {
    setState(() => _actionCount++);
  }

  void _handleDelete() {
    setState(() => _deleteCount++);
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }
}
