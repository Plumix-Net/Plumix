import 'package:material_ui/material_ui.dart';

class SwitchListTileDemoPage extends StatefulWidget {
  const SwitchListTileDemoPage({super.key});

  @override
  State<SwitchListTileDemoPage> createState() => _SwitchListTileDemoPageState();
}

class _SwitchListTileDemoPageState extends State<SwitchListTileDemoPage> {
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
          'SwitchListTile',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Whole-tile interaction, affinity, density/alignment, selected styling, disabled state, and the adaptive branch (which paints the Cupertino switch).',
          style: TextStyle(fontSize: 14, color: Colors.black54),
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
            _buildControlButton(
              _compact ? 'Compact / top' : 'Standard / center',
              () => setState(() => _compact = !_compact),
              144,
              const Color(0xFFF3E5F5),
            ),
          ],
        ),
        Text(
          'switch=$_switchValue, affinity=${_affinity.name}, adaptive=$_adaptive',
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
                children: <Widget>[_buildSwitchTile()],
              ),
            ),
          ),
        ),
      ],
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
