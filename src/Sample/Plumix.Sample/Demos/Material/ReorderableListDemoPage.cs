using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
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
        private bool _delayedHandles;
        private string _status = "Drag an item to reorder it";

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text(
                        "ReorderableListView + DragBoundary",
                        style: new TextStyle(
                            FontSize: 20,
                            Color: Colors.Black)),
                    new Text(
                        "An 8% anchor offsets the list; DragBoundary keeps its proxy inside the visible bounds.",
                        style: new TextStyle(FontSize: 12, Color: new Color(0x8A000000))),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: ToggleHandles,
                                child: new Text(
                                    _buildDefaultDragHandles ? "Use custom handles" : "Use default handles")),
                            new TextButton(
                                onPressed: () => SetState(() =>
                                {
                                    _buildDefaultDragHandles = false;
                                    _delayedHandles = !_delayedHandles;
                                }),
                                child: new Text(_delayedHandles ? "Use instant drag" : "Use long press")),
                            new Expanded(new Text(
                                _status,
                                style: new TextStyle(
                                    FontSize: 12,
                                    Color: new Color(0x8A000000)))),
                        ]),
                    new Expanded(
                        new DragBoundary(
                            new ReorderableListView(
                                children: BuildItems(),
                                onReorderItem: HandleReorder,
                                onReorderStart: index => SetState(() => _status = $"Dragging {_items[index]}"),
                                onReorderEnd: index => SetState(() => _status = $"Dropped at insertion index {index}"),
                                buildDefaultDragHandles: _buildDefaultDragHandles,
                                header: new Padding(
                                    EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                                    new Text(
                                        "Header (not reorderable)",
                                        style: new TextStyle(
                                            FontSize: 13,
                                            Color: Colors.Black))),
                                footer: new Padding(
                                    EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                                    new Text(
                                        "Footer (not reorderable)",
                                        style: new TextStyle(
                                            FontSize: 13,
                                            Color: Colors.Black))),
                                padding: EdgeInsets.All(4),
                                itemExtent: 58,
                                anchor: 0.08,
                                dragStartBehavior: DragStartBehavior.Down,
                                keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag,
                                restorationId: "reorderable-demo",
                                clipBehavior: Clip.None,
                                autoScrollerVelocityScalar: 75.0))),
                ]);
        }

        private IReadOnlyList<Widget> BuildItems()
        {
            List<Widget> items = [];
            for (int index = 0; index < _items.Count; index++)
            {
                string label = _items[index];
                Widget? trailing = null;
                if (!_buildDefaultDragHandles)
                {
                    trailing = _delayedHandles
                        ? new ReorderableDelayedDragStartListener(
                            child: new Icon(Icons.DragHandle),
                            index: index)
                        : new ReorderableDragStartListener(
                            child: new Icon(Icons.DragHandle),
                            index: index);
                }

                items.Add(new ListTile(
                    key: new ValueKey<string>(label),
                    leading: new CircleAvatar(
                        radius: 16,
                        backgroundColor: new Color(0xFFEADDFF),
                        child: new Text(
                            (index + 1).ToString(),
                            style: new TextStyle(
                                FontSize: 12,
                                Color: new Color(0xFF21005D)))),
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
