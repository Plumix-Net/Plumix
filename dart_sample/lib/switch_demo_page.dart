import 'package:flutter/material.dart';

class SwitchDemoPage extends StatefulWidget {
  const SwitchDemoPage({super.key});

  @override
  State<SwitchDemoPage> createState() => _SwitchDemoPageState();
}

class _SwitchDemoPageState extends State<SwitchDemoPage> {
  bool _enabled = true;
  bool _value = true;
  bool _shrinkWrapTapTarget = false;
  bool _showThumbIcons = true;
  int _changes = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData switchTheme = Theme.of(context).copyWith(
      materialTapTargetSize: _shrinkWrapTapTarget
          ? MaterialTapTargetSize.shrinkWrap
          : MaterialTapTargetSize.padded,
      switchTheme: SwitchThemeData(
        thumbIcon: _showThumbIcons
            ? WidgetStateProperty.resolveWith((Set<WidgetState> states) {
                return states.contains(WidgetState.selected)
                    ? const Icon(Icons.check, size: 14)
                    : const Icon(Icons.close, size: 14);
              })
            : null,
      ),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Switch baseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material Switch with value control, drag/tap interaction, thumb icons, and theme/widget color precedence.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _enabled ? 'Enabled' : 'Disabled',
              onTap: _toggleEnabled,
              width: 108,
              background: const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              label: _showThumbIcons ? 'Icons: on' : 'Icons: off',
              onTap: _toggleThumbIcons,
              width: 108,
              background: const Color(0xFFEAE4FF),
            ),
            _buildControlButton(
              label: _shrinkWrapTapTarget ? 'Tap: shrink' : 'Tap: padded',
              onTap: _toggleTapTargetSize,
              width: 128,
              background: const Color(0xFFE8F4E8),
            ),
            _buildControlButton(
              label: 'Reset',
              onTap: _reset,
              width: 80,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'enabled=$_enabled, value=$_value, thumbIcons=$_showThumbIcons, changes=$_changes, tapTarget=${_shrinkWrapTapTarget ? 'shrinkWrap' : 'padded'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Theme(
          data: switchTheme,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              _buildSwitchRow(
                toggle: Switch(
                  value: _value,
                  onChanged: _enabled ? _onValueChanged : null,
                ),
                title: 'Default switch',
                subtitle: 'Tap or drag thumb to toggle on/off',
              ),
              _buildSwitchRow(
                toggle: Switch(
                  value: _value,
                  onChanged: _enabled ? _onValueChanged : null,
                  thumbColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    if (states.contains(WidgetState.disabled)) {
                      return const Color(0x6100695C);
                    }

                    if (states.contains(WidgetState.selected)) {
                      return const Color(0xFFE8F5E9);
                    }

                    return const Color(0xFFB2DFDB);
                  }),
                  trackColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    if (states.contains(WidgetState.disabled)) {
                      return const Color(0x3300695C);
                    }

                    if (states.contains(WidgetState.selected)) {
                      return const Color(0xFF00695C);
                    }

                    return const Color(0xFFB0BEC5);
                  }),
                  trackOutlineColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    return states.contains(WidgetState.selected)
                        ? Colors.transparent
                        : const Color(0xFF455A64);
                  }),
                  trackOutlineWidth: WidgetStateProperty.all(2),
                ),
                title: 'Custom colors',
                subtitle: 'thumb/track/outline overrides',
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildSwitchRow({
    required Widget toggle,
    required String title,
    required String subtitle,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF1F4F9),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFD6DEEA), width: 1),
      ),
      child: Row(
        spacing: 10,
        children: <Widget>[
          toggle,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 2,
              children: <Widget>[
                Text(
                  title,
                  style: const TextStyle(fontSize: 13, color: Colors.black),
                ),
                Text(
                  subtitle,
                  style: const TextStyle(fontSize: 12, color: Colors.black54),
                ),
              ],
            ),
          ),
        ],
      ),
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

  void _toggleEnabled() {
    setState(() {
      _enabled = !_enabled;
    });
  }

  void _toggleThumbIcons() {
    setState(() {
      _showThumbIcons = !_showThumbIcons;
    });
  }

  void _toggleTapTargetSize() {
    setState(() {
      _shrinkWrapTapTarget = !_shrinkWrapTapTarget;
    });
  }

  void _reset() {
    setState(() {
      _enabled = true;
      _value = true;
      _showThumbIcons = true;
      _shrinkWrapTapTarget = false;
      _changes = 0;
    });
  }

  void _onValueChanged(bool value) {
    setState(() {
      _value = value;
      _changes += 1;
    });
  }
}
