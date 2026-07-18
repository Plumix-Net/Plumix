import 'package:flutter/material.dart';

class AnimatedGridDemoPage extends StatefulWidget {
  const AnimatedGridDemoPage({super.key});

  @override
  State<AnimatedGridDemoPage> createState() => _AnimatedGridDemoPageState();
}

class _AnimatedGridDemoPageState extends State<AnimatedGridDemoPage> {
  final GlobalKey<AnimatedGridState> _gridKey = GlobalKey<AnimatedGridState>();
  final GlobalKey<SliverAnimatedGridState> _sliverKey =
      GlobalKey<SliverAnimatedGridState>();
  final List<int> _items = <int>[1, 2, 3, 4, 5, 6];
  final List<int> _sliverItems = <int>[11, 12, 13, 14, 15, 16];
  int _nextItem = 7;
  int _nextSliverItem = 17;
  String _status = 'Insert or remove tiles to compare both grid variants';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AnimatedGrid + SliverAnimatedGrid',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: _insertItem,
              child: const Text('Insert grid tile'),
            ),
            TextButton(
              onPressed: _items.isEmpty ? null : _removeItem,
              child: const Text('Remove grid tile'),
            ),
            TextButton(
              onPressed: _insertSliverItem,
              child: const Text('Insert sliver tile'),
            ),
            TextButton(
              onPressed: _sliverItems.isEmpty ? null : _removeSliverItem,
              child: const Text('Remove sliver tile'),
            ),
          ],
        ),
        Text(
          _status,
          style: const TextStyle(fontSize: 12, color: Colors.black54),
        ),
        Expanded(
          child: Row(
            spacing: 12,
            children: <Widget>[
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    const Text('AnimatedGrid', style: TextStyle(fontSize: 14)),
                    Expanded(child: _buildAnimatedGrid()),
                  ],
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    const Text(
                      'SliverAnimatedGrid',
                      style: TextStyle(fontSize: 14),
                    ),
                    Expanded(child: _buildSliverAnimatedGrid()),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildAnimatedGrid() {
    return AnimatedGrid(
      key: _gridKey,
      initialItemCount: _items.length,
      padding: const EdgeInsets.all(4),
      gridDelegate: _gridDelegate(),
      itemBuilder:
          (BuildContext context, int index, Animation<double> animation) {
            return _buildTile(_items[index], animation);
          },
    );
  }

  Widget _buildSliverAnimatedGrid() {
    return CustomScrollView(
      slivers: <Widget>[
        SliverPadding(
          padding: const EdgeInsets.all(4),
          sliver: SliverAnimatedGrid(
            key: _sliverKey,
            initialItemCount: _sliverItems.length,
            gridDelegate: _gridDelegate(),
            findChildIndexCallback: _findSliverItemIndex,
            itemBuilder:
                (BuildContext context, int index, Animation<double> animation) {
                  final int value = _sliverItems[index];
                  return _buildTile(
                    value,
                    animation,
                    key: ValueKey<int>(value),
                  );
                },
          ),
        ),
      ],
    );
  }

  SliverGridDelegate _gridDelegate() {
    return const SliverGridDelegateWithFixedCrossAxisCount(
      crossAxisCount: 2,
      mainAxisSpacing: 6,
      crossAxisSpacing: 6,
      childAspectRatio: 1.4,
    );
  }

  Widget _buildTile(int value, Animation<double> animation, {Key? key}) {
    return ScaleTransition(
      key: key,
      scale: animation,
      child: Container(
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: value < 10 ? const Color(0xFFEADDFF) : const Color(0xFFD7E3FF),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Text('Tile $value', style: const TextStyle(color: Colors.black)),
      ),
    );
  }

  int? _findSliverItemIndex(Key key) {
    if (key is! ValueKey<int>) {
      return null;
    }
    final int index = _sliverItems.indexOf(key.value);
    return index < 0 ? null : index;
  }

  void _insertItem() {
    final int index = _items.isEmpty ? 0 : 1;
    final int value = _nextItem++;
    _items.insert(index, value);
    _gridKey.currentState!.insertItem(index);
    setState(() => _status = 'AnimatedGrid inserted Tile $value at $index');
  }

  void _removeItem() {
    final int index = _items.length - 1;
    final int value = _items.removeAt(index);
    _gridKey.currentState!.removeItem(
      index,
      (BuildContext context, Animation<double> animation) =>
          _buildTile(value, animation),
    );
    setState(() => _status = 'AnimatedGrid removed Tile $value from $index');
  }

  void _insertSliverItem() {
    final int index = _sliverItems.isEmpty ? 0 : 1;
    final int value = _nextSliverItem++;
    _sliverItems.insert(index, value);
    _sliverKey.currentState!.insertItem(index);
    setState(
      () => _status = 'SliverAnimatedGrid inserted Tile $value at $index',
    );
  }

  void _removeSliverItem() {
    final int index = _sliverItems.length - 1;
    final int value = _sliverItems.removeAt(index);
    _sliverKey.currentState!.removeItem(
      index,
      (BuildContext context, Animation<double> animation) =>
          _buildTile(value, animation, key: ValueKey<int>(value)),
    );
    setState(
      () => _status = 'SliverAnimatedGrid removed Tile $value from $index',
    );
  }
}
