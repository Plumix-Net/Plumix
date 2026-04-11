import 'dart:math';

import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class GridViewDemoPage extends StatelessWidget {
  const GridViewDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'GridView + SliverGrid',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'GridView uses SliverGrid with fixed-cross-axis delegate.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: GridView.builder(
            itemCount: 60,
            padding: const EdgeInsets.all(12),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 3,
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
              mainAxisExtent: 84,
            ),
            itemBuilder: (BuildContext context, int index) {
              return GridTileItem(index: index);
            },
          ),
        ),
        SizedBox(
          height: 170,
          child: CustomScrollView(
            slivers: <Widget>[
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(12, 0, 12, 10),
                sliver: SliverGrid.builder(
                  itemCount: 12,
                  gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                    maxCrossAxisExtent: 140,
                    crossAxisSpacing: 8,
                    mainAxisSpacing: 8,
                    childAspectRatio: 1.8,
                  ),
                  itemBuilder: (BuildContext context, int index) {
                    return Container(
                      color: index.isEven
                          ? const Color(0xFFEAF4FF)
                          : const Color(0xFFE8F5E9),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 6,
                      ),
                      child: Text(
                        'sliver tile #$index',
                        style: const TextStyle(
                          fontSize: 12,
                          color: Colors.black,
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class GridTileItem extends StatefulWidget {
  const GridTileItem({required this.index, super.key});

  final int index;

  @override
  State<GridTileItem> createState() => _GridTileItemState();
}

class _GridTileItemState extends State<GridTileItem> {
  late final int _token = Random().nextInt(9000) + 1000;
  int _taps = 0;

  @override
  Widget build(BuildContext context) {
    final Color background = widget.index.isEven
        ? const Color(0xFFEAF4FF)
        : Colors.white;
    return CounterTapButton(
      label: 'tile ${widget.index}\nstate=$_token\ntaps=$_taps',
      onTap: () => setState(() => _taps += 1),
      background: background,
      foreground: Colors.black,
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
    );
  }
}
