import 'package:flutter/material.dart';

class ListTileDemoPage extends StatefulWidget {
  const ListTileDemoPage({super.key});

  @override
  State<ListTileDemoPage> createState() => _ListTileDemoPageState();
}

class _ListTileDemoPageState extends State<ListTileDemoPage> {
  bool _enabled = true;
  bool _selected = false;
  bool _dense = false;
  bool _threeLine = false;
  bool _useThemeOverrides = false;
  int _tapCount = 0;
  int _longPressCount = 0;

  @override
  Widget build(BuildContext context) {
    Widget content = _buildTiles();
    if (_useThemeOverrides) {
      content = ListTileTheme(
        data: const ListTileThemeData(
          textColor: Color(0xFF27526B),
          iconColor: Color(0xFF7A4021),
          tileColor: Color(0xFFF5F9EE),
          selectedTileColor: Color(0xFFE4EEFF),
        ),
        child: content,
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ListTile baseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Leading/title/subtitle/trailing composition with selected, dense, and theme-override probes.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _enabled ? 'Enabled' : 'Disabled',
              onTap: () => setState(() => _enabled = !_enabled),
              width: 108,
              background: const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              label: _selected ? 'Selected' : 'Unselected',
              onTap: () => setState(() => _selected = !_selected),
              width: 120,
              background: const Color(0xFFE9F7EF),
            ),
            _buildControlButton(
              label: _dense ? 'Dense' : 'Regular',
              onTap: () => setState(() => _dense = !_dense),
              width: 98,
              background: const Color(0xFFF8EFE2),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _threeLine ? '3-line' : '2-line',
              onTap: () => setState(() => _threeLine = !_threeLine),
              width: 88,
              background: const Color(0xFFF0E8FF),
            ),
            _buildControlButton(
              label: _useThemeOverrides ? 'Theme on' : 'Theme off',
              onTap: () =>
                  setState(() => _useThemeOverrides = !_useThemeOverrides),
              width: 112,
              background: const Color(0xFFEAF6F7),
            ),
            _buildControlButton(
              label: 'Reset',
              onTap: _resetState,
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'enabled=$_enabled, selected=$_selected, dense=$_dense, threeLine=$_threeLine, theme=$_useThemeOverrides, taps=$_tapCount, longPress=$_longPressCount',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F9FC),
            child: content,
          ),
        ),
      ],
    );
  }

  Widget _buildTiles() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        ListTile(
          title: const Text('One-line tile'),
          leading: const Icon(Icons.menu),
          trailing: const Icon(Icons.info_outline),
          selected: _selected,
          enabled: _enabled,
          dense: _dense,
          tileColor: Colors.white,
          selectedTileColor: const Color(0xFFE6EEFF),
          onTap: _enabled ? _onTap : null,
          onLongPress: _enabled ? _onLongPress : null,
        ),
        ListTile(
          title: const Text('Two-line tile'),
          subtitle: const Text(
            'Subtitle text demonstrates two-line default height.',
          ),
          leading: const Icon(Icons.add),
          trailing: const Text('meta', style: TextStyle(fontSize: 12)),
          selected: _selected,
          enabled: _enabled,
          dense: _dense,
          tileColor: Colors.white,
          selectedTileColor: const Color(0xFFE6EEFF),
          onTap: _enabled ? _onTap : null,
          onLongPress: _enabled ? _onLongPress : null,
        ),
        ListTile(
          title: const Text('Three-line probe'),
          subtitle: const Text(
            'When 3-line is enabled this tile uses the taller baseline height for parity checks.',
          ),
          leading: const Icon(Icons.star_outline),
          trailing: const Icon(Icons.close),
          selected: _selected,
          enabled: _enabled,
          dense: _dense,
          isThreeLine: _threeLine,
          tileColor: Colors.white,
          selectedTileColor: const Color(0xFFE6EEFF),
          onTap: _enabled ? _onTap : null,
          onLongPress: _enabled ? _onLongPress : null,
        ),
      ],
    );
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onTap,
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

  void _onTap() {
    setState(() {
      _tapCount += 1;
    });
  }

  void _onLongPress() {
    setState(() {
      _longPressCount += 1;
    });
  }

  void _resetState() {
    setState(() {
      _enabled = true;
      _selected = false;
      _dense = false;
      _threeLine = false;
      _useThemeOverrides = false;
      _tapCount = 0;
      _longPressCount = 0;
    });
  }
}
