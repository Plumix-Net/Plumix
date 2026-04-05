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
  String? _materialGroupValue = 'first';
  int _materialChanges = 0;
  String? _adaptiveGroupValue = 'adaptive-first';
  int _adaptiveChanges = 0;
  TargetPlatform _adaptivePlatform = TargetPlatform.iOS;
  bool _adaptiveUseCheckmarkStyle = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData radioTheme = Theme.of(context).copyWith(
      materialTapTargetSize: _shrinkWrapTapTarget
          ? MaterialTapTargetSize.shrinkWrap
          : MaterialTapTargetSize.padded,
    );
    final ThemeData adaptiveTheme = radioTheme.copyWith(
      platform: _adaptivePlatform,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Radio baseline + adaptive',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material Radio plus adaptive Cupertino probe with platform/checkmark toggles.',
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
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: 'Adaptive: ${_formatPlatform(_adaptivePlatform)}',
              onTap: _cycleAdaptivePlatform,
              width: 156,
              background: const Color(0xFFE7F4FF),
            ),
            _buildControlButton(
              label: _adaptiveUseCheckmarkStyle
                  ? 'Adaptive: checkmark'
                  : 'Adaptive: dot',
              onTap: _toggleAdaptiveCheckmarkStyle,
              width: 164,
              background: const Color(0xFFEFE7FF),
            ),
          ],
        ),
        Text(
          'enabled=$_enabled, toggleable=$_toggleable, materialValue=${_materialGroupValue ?? 'null'}, materialChanges=$_materialChanges, adaptiveValue=${_adaptiveGroupValue ?? 'null'}, adaptiveChanges=$_adaptiveChanges, adaptivePlatform=${_formatPlatform(_adaptivePlatform)}, adaptiveStyle=${_adaptiveUseCheckmarkStyle ? 'checkmark' : 'dot'}, tapTarget=${_shrinkWrapTapTarget ? 'shrinkWrap' : 'padded'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Theme(
          data: radioTheme,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'Material path',
                style: TextStyle(fontSize: 13, color: Color(0xFF37474F)),
              ),
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'first',
                  groupValue: _materialGroupValue,
                  onChanged: _enabled ? _onMaterialChanged : null,
                  toggleable: _toggleable,
                ),
                title: 'Default radio #1',
                subtitle: 'value: first',
              ),
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'second',
                  groupValue: _materialGroupValue,
                  onChanged: _enabled ? _onMaterialChanged : null,
                  toggleable: _toggleable,
                ),
                title: 'Default radio #2',
                subtitle: 'value: second',
              ),
              _buildRadioRow(
                radio: Radio<String>(
                  value: 'custom',
                  groupValue: _materialGroupValue,
                  onChanged: _enabled ? _onMaterialChanged : null,
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
              const Text(
                'Adaptive path',
                style: TextStyle(fontSize: 13, color: Color(0xFF37474F)),
              ),
              Theme(
                data: adaptiveTheme,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  spacing: 8,
                  children: <Widget>[
                    _buildRadioRow(
                      radio: Radio<String>.adaptive(
                        value: 'adaptive-first',
                        groupValue: _adaptiveGroupValue,
                        onChanged: _enabled ? _onAdaptiveChanged : null,
                        toggleable: _toggleable,
                      ),
                      title: 'Adaptive default #1',
                      subtitle: 'value: adaptive-first',
                    ),
                    _buildRadioRow(
                      radio: Radio<String>.adaptive(
                        value: 'adaptive-second',
                        groupValue: _adaptiveGroupValue,
                        onChanged: _enabled ? _onAdaptiveChanged : null,
                        toggleable: _toggleable,
                        activeColor: const Color(0xFF00695C),
                        fillColor: WidgetStateProperty.all(
                          const Color(0xFF8E24AA),
                        ),
                        useCupertinoCheckmarkStyle: _adaptiveUseCheckmarkStyle,
                      ),
                      title: 'Adaptive style probe',
                      subtitle:
                          'checkmark style + fillColor ignore on iOS/macOS',
                    ),
                  ],
                ),
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

  void _cycleAdaptivePlatform() {
    setState(() {
      _adaptivePlatform = switch (_adaptivePlatform) {
        TargetPlatform.iOS => TargetPlatform.macOS,
        TargetPlatform.macOS => TargetPlatform.android,
        _ => TargetPlatform.iOS,
      };
    });
  }

  void _toggleAdaptiveCheckmarkStyle() {
    setState(() {
      _adaptiveUseCheckmarkStyle = !_adaptiveUseCheckmarkStyle;
    });
  }

  void _reset() {
    setState(() {
      _enabled = true;
      _toggleable = true;
      _shrinkWrapTapTarget = false;
      _materialGroupValue = 'first';
      _materialChanges = 0;
      _adaptiveGroupValue = 'adaptive-first';
      _adaptiveChanges = 0;
      _adaptivePlatform = TargetPlatform.iOS;
      _adaptiveUseCheckmarkStyle = false;
    });
  }

  void _onMaterialChanged(String? value) {
    setState(() {
      _materialGroupValue = value;
      _materialChanges += 1;
    });
  }

  void _onAdaptiveChanged(String? value) {
    setState(() {
      _adaptiveGroupValue = value;
      _adaptiveChanges += 1;
    });
  }

  static String _formatPlatform(TargetPlatform platform) {
    return switch (platform) {
      TargetPlatform.iOS => 'iOS',
      TargetPlatform.macOS => 'macOS',
      TargetPlatform.android => 'Android',
      TargetPlatform.fuchsia => 'Fuchsia',
      TargetPlatform.linux => 'Linux',
      TargetPlatform.windows => 'Windows',
    };
  }
}
