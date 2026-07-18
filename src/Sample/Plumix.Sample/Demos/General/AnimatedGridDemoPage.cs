using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/animated_grid_demo_page.dart

public sealed class AnimatedGridDemoPage : StatefulWidget
{
    public override State CreateState() => new AnimatedGridDemoPageState();

    private sealed class AnimatedGridDemoPageState : State
    {
        private readonly LabeledGlobalKey<AnimatedGridState> _gridKey = new("animated-grid-demo");
        private readonly LabeledGlobalKey<SliverAnimatedGridState> _sliverKey = new("sliver-animated-grid-demo");
        private readonly List<int> _items = [1, 2, 3, 4, 5, 6];
        private readonly List<int> _sliverItems = [11, 12, 13, 14, 15, 16];
        private int _nextItem = 7;
        private int _nextSliverItem = 17;
        private string _status = "Insert or remove tiles to compare both grid variants";

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("AnimatedGrid + SliverAnimatedGrid", fontSize: 20, color: Colors.Black),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            new TextButton(onPressed: InsertItem, child: new Text("Insert grid tile")),
                            new TextButton(
                                onPressed: _items.Count == 0 ? null : RemoveItem,
                                child: new Text("Remove grid tile")),
                            new TextButton(onPressed: InsertSliverItem, child: new Text("Insert sliver tile")),
                            new TextButton(
                                onPressed: _sliverItems.Count == 0 ? null : RemoveSliverItem,
                                child: new Text("Remove sliver tile")),
                        ]),
                    new Text(_status, fontSize: 12, color: Color.Parse("#8A000000")),
                    new Expanded(
                        new Row(
                            spacing: 12,
                            children:
                            [
                                new Expanded(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            new Text("AnimatedGrid", fontSize: 14),
                                            new Expanded(BuildAnimatedGrid()),
                                        ])),
                                new Expanded(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            new Text("SliverAnimatedGrid", fontSize: 14),
                                            new Expanded(BuildSliverAnimatedGrid()),
                                        ])),
                            ])),
                ]);
        }

        private Widget BuildAnimatedGrid()
        {
            return new AnimatedGrid(
                itemBuilder: (_, index, animation) => BuildTile(_items[index], animation),
                gridDelegate: CreateGridDelegate(),
                initialItemCount: _items.Count,
                padding: new Thickness(4),
                key: _gridKey);
        }

        private Widget BuildSliverAnimatedGrid()
        {
            return new CustomScrollView(
                slivers:
                [
                    new SliverPadding(
                        new Thickness(4),
                        new SliverAnimatedGrid(
                            itemBuilder: (_, index, animation) => BuildTile(
                                _sliverItems[index],
                                animation,
                                new ValueKey<int>(_sliverItems[index])),
                            gridDelegate: CreateGridDelegate(),
                            findChildIndexCallback: FindSliverItemIndex,
                            initialItemCount: _sliverItems.Count,
                            key: _sliverKey)),
                ]);
        }

        private static SliverGridDelegate CreateGridDelegate()
        {
            return new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 6,
                crossAxisSpacing: 6,
                childAspectRatio: 1.4);
        }

        private static Widget BuildTile(int value, Animation<double> animation, Key? key = null)
        {
            return new ScaleTransition(
                scale: animation,
                key: key,
                child: new Container(
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: value < 10 ? Color.Parse("#FFEADDFF") : Color.Parse("#FFD7E3FF"),
                        BorderRadius: BorderRadius.Circular(10)),
                    child: new Text($"Tile {value}", color: Colors.Black)));
        }

        private int? FindSliverItemIndex(Key key)
        {
            if (key is not ValueKey<int> valueKey)
            {
                return null;
            }

            int index = _sliverItems.IndexOf(valueKey.Value);
            return index < 0 ? null : index;
        }

        private void InsertItem()
        {
            int index = Math.Min(1, _items.Count);
            int value = _nextItem++;
            _items.Insert(index, value);
            _gridKey.CurrentState!.InsertItem(index);
            SetState(() => _status = $"AnimatedGrid inserted Tile {value} at {index}");
        }

        private void RemoveItem()
        {
            int index = _items.Count - 1;
            int value = _items[index];
            _items.RemoveAt(index);
            _gridKey.CurrentState!.RemoveItem(index, (_, animation) => BuildTile(value, animation));
            SetState(() => _status = $"AnimatedGrid removed Tile {value} from {index}");
        }

        private void InsertSliverItem()
        {
            int index = Math.Min(1, _sliverItems.Count);
            int value = _nextSliverItem++;
            _sliverItems.Insert(index, value);
            _sliverKey.CurrentState!.InsertItem(index);
            SetState(() => _status = $"SliverAnimatedGrid inserted Tile {value} at {index}");
        }

        private void RemoveSliverItem()
        {
            int index = _sliverItems.Count - 1;
            int value = _sliverItems[index];
            _sliverItems.RemoveAt(index);
            _sliverKey.CurrentState!.RemoveItem(
                index,
                (_, animation) => BuildTile(value, animation, new ValueKey<int>(value)));
            SetState(() => _status = $"SliverAnimatedGrid removed Tile {value} from {index}");
        }
    }
}
