import 'package:flutter/material.dart';

class RadioDemoPage extends StatefulWidget {
  const RadioDemoPage({super.key});

  @override
  State<RadioDemoPage> createState() => _RadioDemoPageState();
}

class _RadioDemoPageState extends State<RadioDemoPage> {
  bool _enabled = true;
  bool _toggleable = true;
  bool _shrinkWrapTapTarget = false;
  String? _groupValue = 'first';
  int _changes = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData radioTheme = Theme.of(context).copyWith(
      materialTapTargetSize: _shrinkWrapTapTarget
          ? MaterialTapTargetSize.shrinkWrap
          : MaterialTapTargetSize.padded,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Radio baseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material Radio with group selection, toggleable mode, and tap-target policy toggle.',
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
              label: _toggleable ? 'Toggleable' : 'No toggle',
              onTap: _toggleToggleable,
              width: 116,
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
          'enabled=$_enabled, toggleable=$_toggleable, groupValue=${_groupValue ?? 'null'}, changes=$_changes, tapTarget=${_shrinkWrapTapTarget ? 'shrinkWrap' : 'padded'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Theme(
          data: radioTheme,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'first',
                  groupValue: _groupValue,
                  onChanged: _enabled ? _onChanged : null,
                  toggleable: _toggleable,
                ),
                title: 'Default radio #1',
                subtitle: 'value: first',
              ),
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'second',
                  groupValue: _groupValue,
                  onChanged: _enabled ? _onChanged : null,
                  toggleable: _toggleable,
                ),
                title: 'Default radio #2',
                subtitle: 'value: second',
              ),
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'custom',
                  groupValue: _groupValue,
                  onChanged: _enabled ? _onChanged : null,
                  toggleable: _toggleable,
                  activeColor: const Color(0xFF00695C),
                  fillColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    if (states.contains(WidgetState.disabled)) {
                      return const Color(0x6100695C);
                    }

                    if (states.contains(WidgetState.selected)) {
                      return const Color(0xFF00695C);
                    }

                    return const Color(0xFF455A64);
                  }),
                  backgroundColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    return states.contains(WidgetState.selected)
                        ? const Color(0x1400695C)
                        : Colors.transparent;
                  }),
                  overlayColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    if (states.contains(WidgetState.pressed)) {
                      return const Color(0x3300695C);
                    }

                    if (states.contains(WidgetState.hovered)) {
                      return const Color(0x2200695C);
                    }

                    if (states.contains(WidgetState.focused)) {
                      return const Color(0x2900695C);
                    }

                    return null;
                  }),
                  side: const BorderSide(color: Color(0xFF00695C), width: 2),
                  innerRadius: WidgetStateProperty.all(5.0),
                ),
                title: 'Custom colors',
                subtitle: 'fill/overlay/side/background overrides',
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildRadioRow({
    required Widget radio,
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
          radio,
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

  void _toggleToggleable() {
    setState(() {
      _toggleable = !_toggleable;
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
      _toggleable = true;
      _shrinkWrapTapTarget = false;
      _groupValue = 'first';
      _changes = 0;
    });
  }

  void _onChanged(String? value) {
    setState(() {
      _groupValue = value;
      _changes += 1;
    });
  }
}
