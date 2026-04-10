import 'package:flutter/material.dart';

class CheckboxDemoPage extends StatefulWidget {
  const CheckboxDemoPage({super.key});

  @override
  State<CheckboxDemoPage> createState() => _CheckboxDemoPageState();
}

class _CheckboxDemoPageState extends State<CheckboxDemoPage> {
  bool _enabled = true;
  bool _checked = false;
  bool? _tristateValue;
  bool _shrinkWrapTapTarget = false;
  int _changes = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData checkboxTheme = Theme.of(context).copyWith(
      materialTapTargetSize: _shrinkWrapTapTarget
          ? MaterialTapTargetSize.shrinkWrap
          : MaterialTapTargetSize.padded,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Checkbox baseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material Checkbox with bool and bool? (tristate) values, enabled/disabled flow, and tap-target policy toggle.',
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
              label: _shrinkWrapTapTarget ? 'Tap: shrink' : 'Tap: padded',
              onTap: _toggleTapTargetSize,
              width: 128,
              background: const Color(0xFFEAE4FF),
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
          'enabled=$_enabled, checked=$_checked, tristate=${_formatNullableBool(_tristateValue)}, changes=$_changes, tapTarget=${_shrinkWrapTapTarget ? 'shrinkWrap' : 'padded'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Theme(
          data: checkboxTheme,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              _buildCheckboxRow(
                checkbox: Checkbox(
                  value: _checked,
                  onChanged: _enabled ? _onCheckedChanged : null,
                ),
                title: 'Default checkbox',
                subtitle: 'value: false/true',
              ),
              _buildCheckboxRow(
                checkbox: Checkbox(
                  value: _tristateValue,
                  tristate: true,
                  onChanged: _enabled ? _onTristateChanged : null,
                ),
                title: 'Tristate checkbox',
                subtitle: 'cycle: false -> true -> null -> false',
              ),
              _buildCheckboxRow(
                checkbox: Checkbox(
                  value: _checked,
                  onChanged: _enabled ? _onCheckedChanged : null,
                  activeColor: const Color(0xFF00695C),
                  checkColor: Colors.white,
                  fillColor: WidgetStateProperty.resolveWith((
                    Set<WidgetState> states,
                  ) {
                    if (states.contains(WidgetState.disabled)) {
                      return const Color(0x6100695C);
                    }

                    if (states.contains(WidgetState.selected)) {
                      return const Color(0xFF00695C);
                    }

                    return Colors.transparent;
                  }),
                  side: const BorderSide(color: Color(0xFF00695C), width: 2),
                ),
                title: 'Custom colors',
                subtitle: 'active/check/fill/side overrides',
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildCheckboxRow({
    required Widget checkbox,
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
          checkbox,
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

  void _toggleTapTargetSize() {
    setState(() {
      _shrinkWrapTapTarget = !_shrinkWrapTapTarget;
    });
  }

  void _reset() {
    setState(() {
      _enabled = true;
      _checked = false;
      _tristateValue = null;
      _shrinkWrapTapTarget = false;
      _changes = 0;
    });
  }

  void _onCheckedChanged(bool? value) {
    setState(() {
      _checked = value ?? false;
      _changes += 1;
    });
  }

  void _onTristateChanged(bool? value) {
    setState(() {
      _tristateValue = value;
      _changes += 1;
    });
  }

  String _formatNullableBool(bool? value) {
    if (value == null) {
      return 'null';
    }

    return value ? 'true' : 'false';
  }
}
