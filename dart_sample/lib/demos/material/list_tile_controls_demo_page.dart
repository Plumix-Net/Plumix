import 'package:flutter/material.dart';

class ListTileControlsDemoPage extends StatefulWidget {
  const ListTileControlsDemoPage({super.key});

  @override
  State<ListTileControlsDemoPage> createState() =>
      _ListTileControlsDemoPageState();
}

class _ListTileControlsDemoPageState extends State<ListTileControlsDemoPage> {
  bool _checkboxValue = false;
  bool? _tristateValue;
  bool _switchValue = true;
  bool _enabled = true;
  bool _adaptive = false;
  bool _compact = false;
  ListTileControlAffinity _affinity = ListTileControlAffinity.trailing;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'CheckboxListTile + SwitchListTile',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Whole-tile interaction, tristate cycle, affinity, density/alignment, selected styling, disabled state, and adaptive branches.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          children: <Widget>[
            _buildControlButton(
              _compact ? 'Compact / top' : 'Standard / center',
              () => setState(() => _compact = !_compact),
              144,
              const Color(0xFFF3E5F5),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              _enabled ? 'Enabled' : 'Disabled',
              () => setState(() => _enabled = !_enabled),
              104,
              const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              _affinity == ListTileControlAffinity.leading
                  ? 'Leading'
                  : 'Trailing',
              () => setState(() {
                _affinity = _affinity == ListTileControlAffinity.leading
                    ? ListTileControlAffinity.trailing
                    : ListTileControlAffinity.leading;
              }),
              104,
              const Color(0xFFE9F7EF),
            ),
            _buildControlButton(
              _adaptive ? 'Adaptive' : 'Material',
              () => setState(() => _adaptive = !_adaptive),
              104,
              const Color(0xFFF8EFE2),
            ),
          ],
        ),
        Text(
          'checkbox=$_checkboxValue, tristate=$_tristateValue, switch=$_switchValue, affinity=${_affinity.name}, adaptive=$_adaptive',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F9FC),
            child: ListTileTheme(
              data: ListTileThemeData(
                controlAffinity: _affinity,
                visualDensity: _compact
                    ? VisualDensity.compact
                    : VisualDensity.standard,
                titleAlignment: _compact
                    ? ListTileTitleAlignment.top
                    : ListTileTitleAlignment.center,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  _buildCheckboxTile(),
                  _buildTristateTile(),
                  _buildSwitchTile(),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildCheckboxTile() {
    final ValueChanged<bool?>? onChanged = _enabled
        ? (bool? value) => setState(() => _checkboxValue = value ?? false)
        : null;
    if (_adaptive) {
      return CheckboxListTile.adaptive(
        value: _checkboxValue,
        onChanged: onChanged,
        title: const Text('Wi-Fi discovery'),
        subtitle: const Text('Tap anywhere on the row to toggle.'),
        secondary: const Icon(Icons.info_outline),
        titleAlignment: _compact
            ? ListTileTitleAlignment.top
            : ListTileTitleAlignment.center,
        selected: _checkboxValue,
      );
    }
    return CheckboxListTile(
      value: _checkboxValue,
      onChanged: onChanged,
      title: const Text('Wi-Fi discovery'),
      subtitle: const Text('Tap anywhere on the row to toggle.'),
      secondary: const Icon(Icons.info_outline),
      titleAlignment: _compact
          ? ListTileTitleAlignment.top
          : ListTileTitleAlignment.center,
      selected: _checkboxValue,
      selectedTileColor: const Color(0xFFE8DEF8),
    );
  }

  Widget _buildTristateTile() {
    final ValueChanged<bool?>? onChanged = _enabled
        ? (bool? value) => setState(() => _tristateValue = value)
        : null;
    if (_adaptive) {
      return CheckboxListTile.adaptive(
        value: _tristateValue,
        onChanged: onChanged,
        tristate: true,
        title: const Text('Tristate selection'),
        subtitle: const Text('false → true → null → false'),
        secondary: const Icon(Icons.star_outline),
      );
    }
    return CheckboxListTile(
      value: _tristateValue,
      onChanged: onChanged,
      tristate: true,
      title: const Text('Tristate selection'),
      subtitle: const Text('false → true → null → false'),
      secondary: const Icon(Icons.star_outline),
    );
  }

  Widget _buildSwitchTile() {
    final ValueChanged<bool>? onChanged = _enabled
        ? (bool value) => setState(() => _switchValue = value)
        : null;
    if (_adaptive) {
      return SwitchListTile.adaptive(
        value: _switchValue,
        onChanged: onChanged,
        title: const Text('Background sync'),
        subtitle: const Text('The embedded switch remains draggable.'),
        secondary: const Icon(Icons.menu),
        selected: _switchValue,
      );
    }
    return SwitchListTile(
      value: _switchValue,
      onChanged: onChanged,
      title: const Text('Background sync'),
      subtitle: const Text('The embedded switch remains draggable.'),
      secondary: const Icon(Icons.menu),
      selected: _switchValue,
      selectedTileColor: const Color(0xFFE8DEF8),
    );
  }

  Widget _buildControlButton(
    String label,
    VoidCallback onPressed,
    double width,
    Color background,
  ) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onPressed,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}
