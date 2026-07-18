import 'package:flutter/material.dart';

class ReorderableListDemoPage extends StatefulWidget {
  const ReorderableListDemoPage({super.key});

  @override
  State<ReorderableListDemoPage> createState() =>
      _ReorderableListDemoPageState();
}

class _ReorderableListDemoPageState extends State<ReorderableListDemoPage> {
  final List<String> _items = <String>[
    'Alpha',
    'Bravo',
    'Charlie',
    'Delta',
    'Echo',
    'Foxtrot',
  ];
  bool _buildDefaultDragHandles = true;
  String _status = 'Drag an item to reorder it';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ReorderableListView',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: _toggleHandles,
              child: Text(
                _buildDefaultDragHandles
                    ? 'Use custom handles'
                    : 'Use default handles',
              ),
            ),
            Expanded(
              child: Text(
                _status,
                style: const TextStyle(fontSize: 12, color: Colors.black54),
              ),
            ),
          ],
        ),
        Expanded(
          child: ReorderableListView(
            onReorderItem: _handleReorder,
            onReorderStart: (int index) {
              setState(() => _status = 'Dragging ${_items[index]}');
            },
            onReorderEnd: (int index) {
              setState(() => _status = 'Dropped at insertion index $index');
            },
            buildDefaultDragHandles: _buildDefaultDragHandles,
            header: const Padding(
              padding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Text(
                'Header (not reorderable)',
                style: TextStyle(fontSize: 13, color: Colors.black),
              ),
            ),
            footer: const Padding(
              padding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Text(
                'Footer (not reorderable)',
                style: TextStyle(fontSize: 13, color: Colors.black),
              ),
            ),
            padding: const EdgeInsets.all(4),
            itemExtent: 58,
            children: <Widget>[
              for (int index = 0; index < _items.length; index++)
                _buildItem(index),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildItem(int index) {
    final String label = _items[index];
    return ListTile(
      key: ValueKey<String>(label),
      leading: CircleAvatar(
        radius: 16,
        backgroundColor: const Color(0xFFEADDFF),
        child: Text(
          '${index + 1}',
          style: const TextStyle(fontSize: 12, color: Color(0xFF21005D)),
        ),
      ),
      title: Text(label),
      subtitle: Text('Stable key: ${label.toLowerCase()}'),
      trailing: _buildDefaultDragHandles
          ? null
          : ReorderableDragStartListener(
              index: index,
              child: const Icon(Icons.drag_handle),
            ),
      tileColor: Colors.white,
      minTileHeight: 58,
    );
  }

  void _toggleHandles() {
    setState(() => _buildDefaultDragHandles = !_buildDefaultDragHandles);
  }

  void _handleReorder(int oldIndex, int newIndex) {
    setState(() {
      final String item = _items.removeAt(oldIndex);
      _items.insert(newIndex, item);
      _status = 'Moved $item: $oldIndex -> $newIndex';
    });
  }
}
