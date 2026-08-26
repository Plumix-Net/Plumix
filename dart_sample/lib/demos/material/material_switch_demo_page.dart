import 'package:material_ui/material_ui.dart';

class MaterialSwitchDemoPage extends StatefulWidget {
  const MaterialSwitchDemoPage({super.key});

  @override
  State<MaterialSwitchDemoPage> createState() => _MaterialSwitchDemoPageState();
}

class _MaterialSwitchDemoPageState extends State<MaterialSwitchDemoPage> {
  static const List<TargetPlatform> _platforms = <TargetPlatform>[
    TargetPlatform.android,
    TargetPlatform.iOS,
    TargetPlatform.macOS,
  ];

  bool _useMaterial3 = true;
  int _platformIndex = 0;
  bool _plain = true;
  bool _colored = false;
  bool _iconed = true;
  bool _outlined = false;
  bool _adaptive = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final TargetPlatform platform = _platforms[_platformIndex];
    final ThemeData pageTheme = baseTheme.copyWith(
      useMaterial3: _useMaterial3,
      platform: platform,
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text('Switch', style: TextStyle(fontSize: 20)),
          const SizedBox(height: 8),
          const Text(
            'M2/M3 tokens, thumb icons, track outlines and Switch.adaptive per platform.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 80,
                background: const Color(0xFFE9F0FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: platform.name,
                onTap: () => setState(
                  () => _platformIndex = (_platformIndex + 1) % _platforms.length,
                ),
                width: 112,
                background: const Color(0xFFEAF6F7),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'useMaterial3=${_useMaterial3 ? "true" : "false"}, '
            'platform=${platform.name}',
            style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  _buildRow(
                    'Plain',
                    Switch(
                      value: _plain,
                      onChanged: (bool value) => setState(() => _plain = value),
                    ),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Custom colors',
                    Switch(
                      value: _colored,
                      onChanged: (bool value) => setState(() => _colored = value),
                      activeThumbColor: const Color(0xFFFFF8E1),
                      activeTrackColor: const Color(0xFF00695C),
                      inactiveThumbColor: const Color(0xFF8D6E63),
                      inactiveTrackColor: const Color(0xFFD7CCC8),
                    ),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Thumb icons',
                    Switch(
                      value: _iconed,
                      onChanged: (bool value) => setState(() => _iconed = value),
                      thumbIcon: WidgetStateProperty.resolveWith<Icon?>(
                        (Set<WidgetState> states) =>
                            states.contains(WidgetState.selected)
                            ? const Icon(Icons.check)
                            : const Icon(Icons.close),
                      ),
                    ),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Track outline',
                    Switch(
                      value: _outlined,
                      onChanged: (bool value) => setState(() => _outlined = value),
                      trackOutlineColor: const WidgetStatePropertyAll<Color?>(
                        Color(0xFF1565C0),
                      ),
                      trackOutlineWidth: const WidgetStatePropertyAll<double?>(3.0),
                    ),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Adaptive',
                    Switch.adaptive(
                      value: _adaptive,
                      onChanged: (bool value) => setState(() => _adaptive = value),
                    ),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Disabled (on)',
                    const Switch(value: true, onChanged: null),
                  ),
                  const SizedBox(height: 14),
                  _buildRow(
                    'Disabled (off)',
                    const Switch(value: false, onChanged: null),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRow(String label, Widget control) {
    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Row(
        children: <Widget>[
          SizedBox(
            width: 140,
            child: Text(
              label,
              style: const TextStyle(fontSize: 13, color: Colors.black),
            ),
          ),
          const SizedBox(width: 12),
          control,
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
}
