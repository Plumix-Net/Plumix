import 'package:flutter/material.dart';

class ChipsDemoPage extends StatefulWidget {
  const ChipsDemoPage({super.key});

  @override
  State<ChipsDemoPage> createState() => _ChipsDemoPageState();
}

class _ChipsDemoPageState extends State<ChipsDemoPage> {
  bool _enabled = true;
  bool _selected = false;
  bool _useLocalTheme = false;
  int _actionCount = 0;

  @override
  Widget build(BuildContext context) {
    Widget probes = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text('ActionChip + ChoiceChip', style: TextStyle(fontSize: 20)),
        const SizedBox(height: 14),
        const Text(
          'Flat/elevated variants, selected and disabled states, avatar/checkmark, and ChipTheme precedence.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            _controlButton(
              _enabled ? 'Enabled' : 'Disabled',
              () => setState(() => _enabled = !_enabled),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _useLocalTheme ? 'Theme override on' : 'Theme override off',
              () => setState(() => _useLocalTheme = !_useLocalTheme),
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Action chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            ActionChip(
              label: const Text('Suggest'),
              onPressed: _enabled ? _handleAction : null,
            ),
            const SizedBox(width: 10),
            ActionChip(
              avatar: const Icon(Icons.star),
              label: const Text('Assist'),
              onPressed: _enabled ? _handleAction : null,
            ),
            const SizedBox(width: 10),
            ActionChip.elevated(
              label: const Text('Elevated'),
              onPressed: _enabled ? _handleAction : null,
            ),
          ],
        ),
        const SizedBox(height: 14),
        const Text('Choice chips', style: TextStyle(fontSize: 14)),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            ChoiceChip(
              label: const Text('Standard'),
              selected: !_selected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _selected = !value)
                  : null,
            ),
            const SizedBox(width: 10),
            ChoiceChip(
              label: const Text('Selected'),
              selected: _selected,
              onSelected: _enabled
                  ? (bool value) => setState(() => _selected = value)
                  : null,
            ),
            const SizedBox(width: 10),
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
        Text(
          'Actions: $_actionCount · selected: $_selected',
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
