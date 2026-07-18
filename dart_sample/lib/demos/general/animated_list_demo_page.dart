import 'package:flutter/material.dart';

class AnimatedListDemoPage extends StatefulWidget {
  const AnimatedListDemoPage({super.key});

  @override
  State<AnimatedListDemoPage> createState() => _AnimatedListDemoPageState();
}

class _AnimatedListDemoPageState extends State<AnimatedListDemoPage> {
  final GlobalKey<AnimatedListState> _listKey = GlobalKey<AnimatedListState>();
  final GlobalKey<SliverAnimatedListState> _sliverKey =
      GlobalKey<SliverAnimatedListState>();
  final List<int> _items = <int>[1, 2, 3, 4];
  final List<int> _sliverItems = <int>[11, 12, 13, 14];
  int _nextItem = 5;
  int _nextSliverItem = 15;
  String _status = 'Insert or remove items to compare both list variants';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AnimatedList + SliverAnimatedList',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: _insertItem,
              child: const Text('Insert list item'),
            ),
            TextButton(
              onPressed: _items.isEmpty ? null : _removeItem,
              child: const Text('Remove list item'),
            ),
            TextButton(
              onPressed: _insertSliverItem,
              child: const Text('Insert sliver item'),
            ),
            TextButton(
              onPressed: _sliverItems.isEmpty ? null : _removeSliverItem,
              child: const Text('Remove sliver item'),
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
                    const Text(
                      'AnimatedList.separated',
                      style: TextStyle(fontSize: 14),
                    ),
                    Expanded(child: _buildAnimatedList()),
                  ],
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    const Text(
                      'SliverAnimatedList',
                      style: TextStyle(fontSize: 14),
                    ),
                    Expanded(child: _buildSliverAnimatedList()),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildAnimatedList() {
    return AnimatedList.separated(
      key: _listKey,
      initialItemCount: _items.length,
      padding: const EdgeInsets.all(4),
      itemBuilder:
          (BuildContext context, int index, Animation<double> animation) {
            return _buildTile(_items[index], animation);
          },
      separatorBuilder:
          (BuildContext context, int index, Animation<double> animation) {
            return SizeTransition(
              sizeFactor: animation,
              child: const SizedBox(height: 4),
            );
          },
      removedSeparatorBuilder:
          (BuildContext context, int index, Animation<double> animation) {
            return SizeTransition(
              sizeFactor: animation,
              child: const SizedBox(height: 4),
            );
          },
    );
  }

  Widget _buildSliverAnimatedList() {
    return CustomScrollView(
      slivers: <Widget>[
        SliverPadding(
          padding: const EdgeInsets.all(4),
          sliver: SliverAnimatedList(
            key: _sliverKey,
            initialItemCount: _sliverItems.length,
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

  Widget _buildTile(int value, Animation<double> animation, {Key? key}) {
    return SizeTransition(
      key: key,
      sizeFactor: animation,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 2),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: value < 10 ? const Color(0xFFEADDFF) : const Color(0xFFD7E3FF),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Text('Item $value', style: const TextStyle(color: Colors.black)),
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
    _listKey.currentState!.insertItem(index);
    setState(() => _status = 'AnimatedList inserted Item $value at $index');
  }

  void _removeItem() {
    final int index = _items.length - 1;
    final int value = _items.removeAt(index);
    _listKey.currentState!.removeItem(
      index,
      (BuildContext context, Animation<double> animation) =>
          _buildTile(value, animation),
    );
    setState(() => _status = 'AnimatedList removed Item $value from $index');
  }

  void _insertSliverItem() {
    final int index = _sliverItems.isEmpty ? 0 : 1;
    final int value = _nextSliverItem++;
    _sliverItems.insert(index, value);
    _sliverKey.currentState!.insertItem(index);
    setState(
      () => _status = 'SliverAnimatedList inserted Item $value at $index',
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
      () => _status = 'SliverAnimatedList removed Item $value from $index',
    );
  }
}
