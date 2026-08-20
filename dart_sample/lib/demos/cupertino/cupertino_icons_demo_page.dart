import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoIconsDemoPage extends StatelessWidget {
  const CupertinoIconsDemoPage({super.key});

  static const List<(String, IconData)> _samples = <(String, IconData)>[
    ('heart_fill', CupertinoIcons.heart_fill),
    ('bell_fill', CupertinoIcons.bell_fill),
    ('camera', CupertinoIcons.camera),
    ('person_2_fill', CupertinoIcons.person_2_fill),
    ('map_fill', CupertinoIcons.map_fill),
    ('gear', CupertinoIcons.gear),
    ('search', CupertinoIcons.search),
    ('plus_circle', CupertinoIcons.plus_circle),
    ('chevron_back', CupertinoIcons.chevron_back),
    ('arrow_left_right', CupertinoIcons.arrow_left_right),
    ('waveform_path_ecg', CupertinoIcons.waveform_path_ecg),
    ('videocam_circle_fill', CupertinoIcons.videocam_circle_fill),
  ];

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino icons',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Representative legacy, SF Symbols, directional, alias, and '
          'high-range glyphs.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          children: <Widget>[
            for (final (String name, IconData icon) in _samples)
              _buildTile(name, icon),
          ],
        ),
      ],
    );
  }

  static Widget _buildTile(String name, IconData icon) {
    return Container(
      width: 132,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFF2F2F7),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        spacing: 8,
        children: <Widget>[
          Icon(icon, size: 34, color: const Color(0xFF007AFF)),
          Text(
            name,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 11, color: Colors.black),
          ),
        ],
      ),
    );
  }
}
