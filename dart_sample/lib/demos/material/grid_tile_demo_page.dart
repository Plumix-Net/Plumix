import 'package:flutter/material.dart';

class GridTileDemoPage extends StatelessWidget {
  const GridTileDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'GridTile + GridTileBar',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Header/footer overlays, one/two-line bars, slots, transparent background, and RTL.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: GridView.builder(
            itemCount: 4,
            padding: const EdgeInsets.all(12),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              mainAxisExtent: 150,
            ),
            itemBuilder: (BuildContext context, int index) => _buildTile(index),
          ),
        ),
      ],
    );
  }

  Widget _buildTile(int index) {
    const colors = <Color>[
      Color(0xFFD7E3FF),
      Color(0xFFFFD8E4),
      Color(0xFFD9F2E6),
      Color(0xFFFFE2C6),
    ];
    final content = Container(
      color: colors[index],
      alignment: Alignment.center,
      child: Text(
        'Tile ${index + 1}',
        style: const TextStyle(fontSize: 18, color: Colors.black),
      ),
    );

    final Widget tile = switch (index) {
      0 => GridTile(
        header: const GridTileBar(
          backgroundColor: Color(0xCC000000),
          leading: Icon(Icons.star),
          title: Text('Header'),
        ),
        child: content,
      ),
      1 => GridTile(
        footer: const GridTileBar(
          backgroundColor: Color(0xCC000000),
          title: Text('Footer'),
          subtitle: Text('Two lines'),
          trailing: Icon(Icons.info_outline),
        ),
        child: content,
      ),
      2 => GridTile(
        header: const GridTileBar(
          backgroundColor: Color(0xCC000000),
          leading: Icon(Icons.menu),
          title: Text('RTL'),
        ),
        footer: const GridTileBar(
          backgroundColor: Color(0x99000000),
          subtitle: Text('header + footer'),
        ),
        child: content,
      ),
      _ => GridTile(
        footer: const GridTileBar(
          title: Text('Transparent'),
          trailing: Icon(Icons.star_outline),
        ),
        child: content,
      ),
    };

    return Directionality(
      textDirection: index == 2 ? TextDirection.rtl : TextDirection.ltr,
      child: tile,
    );
  }
}
