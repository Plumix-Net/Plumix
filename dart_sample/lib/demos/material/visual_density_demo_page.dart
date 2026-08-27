import 'package:material_ui/material_ui.dart';

class VisualDensityDemoPage extends StatefulWidget {
  const VisualDensityDemoPage({super.key});

  @override
  State<VisualDensityDemoPage> createState() => _VisualDensityDemoPageState();
}

class _VisualDensityDemoPageState extends State<VisualDensityDemoPage> {
  static const List<(String, VisualDensity)> _profiles = <(String, VisualDensity)>[
    ('standard', VisualDensity.standard),
    ('comfortable', VisualDensity.comfortable),
    ('compact', VisualDensity.compact),
    ('(3, 3)', VisualDensity(horizontal: 3, vertical: 3)),
    ('(-3, -3)', VisualDensity(horizontal: -3, vertical: -3)),
  ];

  static const List<TargetPlatform> _platforms = <TargetPlatform>[
    TargetPlatform.android,
    TargetPlatform.iOS,
    TargetPlatform.fuchsia,
    TargetPlatform.linux,
    TargetPlatform.macOS,
    TargetPlatform.windows,
  ];

  int _profileIndex = 0;
  int _platformIndex = 0;

  @override
  Widget build(BuildContext context) {
    final (String name, VisualDensity density) = _profiles[_profileIndex];
    final TargetPlatform platform = _platforms[_platformIndex];
    final ThemeData platformTheme = ThemeData(platform: platform);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'VisualDensity + platform defaults',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Density is unitless: one unit is four logical pixels per axis. ThemeData takes '
          "its default from the theme's platform, not the host.",
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: 'density: $name',
              onTap: () => setState(
                () => _profileIndex = (_profileIndex + 1) % _profiles.length,
              ),
              width: 180,
              background: const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              label: 'platform: ${platform.name}',
              onTap: () => setState(
                () => _platformIndex = (_platformIndex + 1) % _platforms.length,
              ),
              width: 180,
              background: const Color(0xFFEAF6F7),
            ),
          ],
        ),
        Expanded(
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 14,
              children: <Widget>[
                _buildPlatformDefaults(platform, platformTheme),
                _buildDensityFacts(density),
                _buildSizedProbes(density),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildPlatformDefaults(TargetPlatform platform, ThemeData theme) {
    return _buildCard('ThemeData(platform: ${platform.name}) defaults', <Widget>[
      _buildFactRow('visualDensity', theme.visualDensity.toString()),
      _buildFactRow('materialTapTargetSize', theme.materialTapTargetSize.name),
      _buildFactRow(
        'VisualDensity.defaultDensityForPlatform',
        VisualDensity.defaultDensityForPlatform(platform).toString(),
      ),
    ]);
  }

  Widget _buildDensityFacts(VisualDensity density) {
    final Offset adjustment = density.baseSizeAdjustment;
    final BoxConstraints effective = density.effectiveConstraints(
      BoxConstraints.tightFor(width: 48, height: 48),
    );
    return _buildCard('$density', <Widget>[
      _buildFactRow(
        'baseSizeAdjustment',
        '(${adjustment.dx.toStringAsFixed(0)}, ${adjustment.dy.toStringAsFixed(0)})',
      ),
      _buildFactRow(
        'effectiveConstraints(48x48)',
        'min ${effective.minWidth.toStringAsFixed(0)}x${effective.minHeight.toStringAsFixed(0)}, '
            'max ${effective.maxWidth.toStringAsFixed(0)}x${effective.maxHeight.toStringAsFixed(0)}',
      ),
    ]);
  }

  Widget _buildSizedProbes(VisualDensity density) {
    final List<Widget> probes = <Widget>[];
    for (final (String name, VisualDensity profile) in _profiles) {
      final bool selected = profile == density;
      probes.add(
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Row(
            spacing: 10,
            children: <Widget>[
              SizedBox(
                width: 110,
                child: Text(
                  name,
                  style: TextStyle(
                    fontSize: 12,
                    color: selected ? Colors.black : const Color(0xFF607D8B),
                  ),
                ),
              ),
              Theme(
                data: ThemeData.light().copyWith(visualDensity: profile),
                child: ElevatedButton(
                  onPressed: () {},
                  child: const Text('Button', style: TextStyle(fontSize: 12)),
                ),
              ),
            ],
          ),
        ),
      );
    }

    return _buildCard('The same button under each density', probes);
  }

  Widget _buildCard(String title, List<Widget> children) {
    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Text(title, style: const TextStyle(fontSize: 14, color: Colors.black)),
          ...children,
        ],
      ),
    );
  }

  Widget _buildFactRow(String label, String value) {
    return Row(
      spacing: 10,
      children: <Widget>[
        Expanded(
          child: Text(label, style: const TextStyle(fontSize: 12, color: Colors.black)),
        ),
        Text(value, style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B))),
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
          foregroundColor: Colors.black,
          backgroundColor: background,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          minimumSize: const Size(64, 36),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}
