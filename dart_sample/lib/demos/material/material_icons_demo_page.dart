import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';

class MaterialIconsDemoPage extends StatelessWidget {
  const MaterialIconsDemoPage({super.key});

  static const List<(String, IconData)> _variants = <(String, IconData)>[
    ('alarm', Icons.alarm),
    ('alarm_outlined', Icons.alarm_outlined),
    ('alarm_rounded', Icons.alarm_rounded),
    ('alarm_sharp', Icons.alarm_sharp),
  ];

  static const List<(String, IconData)> _samples = <(String, IconData)>[
    ('home', Icons.home),
    ('favorite', Icons.favorite),
    ('shopping_cart_outlined', Icons.shopping_cart_outlined),
    ('cloud_upload_rounded', Icons.cloud_upload_rounded),
    ('rocket_launch', Icons.rocket_launch),
    ('bookmark_outline', Icons.bookmark_outline),
    ('zoom_out_map_rounded', Icons.zoom_out_map_rounded),
    ('auto_awesome_rounded', Icons.auto_awesome_rounded),
  ];

  @override
  Widget build(BuildContext context) {
    final List<(String, IconData)> adaptive = <(String, IconData)>[
      ('adaptive.arrow_back', Icons.adaptive.arrow_back),
      ('adaptive.flip_camera', Icons.adaptive.flip_camera),
      ('adaptive.more', Icons.adaptive.more),
      ('adaptive.share', Icons.adaptive.share),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Material icons',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The full material_ui catalog: base, outlined, rounded, and sharp '
          'variants, aliases, high-range glyphs, and directional mirroring.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        const Text(
          'Style variants of one icon',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        _buildRow(_variants),
        const Text(
          'Catalog samples',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        _buildRow(_samples),
        Text(
          'Icons.adaptive on $defaultTargetPlatform',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        _buildRow(adaptive),
        const Text(
          'arrow_back mirrors with text direction',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        Row(
          spacing: 12,
          children: <Widget>[
            const Directionality(
              textDirection: TextDirection.ltr,
              child: _IconTile(name: 'ltr', icon: Icons.arrow_back),
            ),
            const Directionality(
              textDirection: TextDirection.rtl,
              child: _IconTile(name: 'rtl', icon: Icons.arrow_back),
            ),
          ],
        ),
      ],
    );
  }

  static Widget _buildRow(List<(String, IconData)> icons) {
    return Wrap(
      spacing: 12,
      runSpacing: 12,
      children: <Widget>[
        for (final (String name, IconData icon) in icons)
          _IconTile(name: name, icon: icon),
      ],
    );
  }
}

class _IconTile extends StatelessWidget {
  const _IconTile({required this.name, required this.icon});

  final String name;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 148,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFEDE7F6),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        spacing: 8,
        children: <Widget>[
          Icon(icon, size: 34, color: const Color(0xFF6200EE)),
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
