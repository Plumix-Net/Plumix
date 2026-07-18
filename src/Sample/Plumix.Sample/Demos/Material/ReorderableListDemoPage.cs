using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/reorderable_list_demo_page.dart

public sealed class ReorderableListDemoPage : StatefulWidget
{
    public override State CreateState() => new ReorderableListDemoPageState();

    private sealed class ReorderableListDemoPageState : State
    {
        private readonly List<string> _items = ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot"];
        private bool _buildDefaultDragHandles = true;
        private string _status = "Drag an item to reorder it";

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("ReorderableListView", fontSize: 20, color: Colors.Black),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: ToggleHandles,
                                child: new Text(
                                    _buildDefaultDragHandles ? "Use custom handles" : "Use default handles")),
                            new Expanded(new Text(_status, fontSize: 12, color: Color.Parse("#8A000000"))),
                        ]),
                    new Expanded(
                        new ReorderableListView(
                            children: BuildItems(),
                            onReorderItem: HandleReorder,
                            onReorderStart: index => SetState(() => _status = $"Dragging {_items[index]}"),
                            onReorderEnd: index => SetState(() => _status = $"Dropped at insertion index {index}"),
                            buildDefaultDragHandles: _buildDefaultDragHandles,
                            header: new Padding(
                                new Thickness(12, 8),
                                new Text("Header (not reorderable)", fontSize: 13, color: Colors.Black)),
                            footer: new Padding(
                                new Thickness(12, 8),
                                new Text("Footer (not reorderable)", fontSize: 13, color: Colors.Black)),
                            padding: new Thickness(4),
                            itemExtent: 58)),
                ]);
        }

        private IReadOnlyList<Widget> BuildItems()
        {
            List<Widget> items = [];
            for (int index = 0; index < _items.Count; index++)
            {
                string label = _items[index];
                Widget? trailing = _buildDefaultDragHandles
                    ? null
                    : new ReorderableDragStartListener(
                        child: new Icon(Icons.DragHandle),
                        index: index);
                items.Add(new ListTile(
                    key: new ValueKey<string>(label),
                    leading: new CircleAvatar(
                        radius: 16,
                        backgroundColor: Color.Parse("#FFEADDFF"),
                        child: new Text((index + 1).ToString(), fontSize: 12, color: Color.Parse("#FF21005D"))),
                    title: new Text(label),
                    subtitle: new Text($"Stable key: {label.ToLowerInvariant()}"),
                    trailing: trailing,
                    tileColor: Colors.White,
                    minTileHeight: 58));
            }

            return items;
        }

        private void ToggleHandles()
        {
            SetState(() => _buildDefaultDragHandles = !_buildDefaultDragHandles);
        }

        private void HandleReorder(int oldIndex, int newIndex)
        {
            SetState(() =>
            {
                string item = _items[oldIndex];
                _items.RemoveAt(oldIndex);
                _items.Insert(newIndex, item);
                _status = $"Moved {item}: {oldIndex} -> {newIndex}";
            });
        }
    }
}
