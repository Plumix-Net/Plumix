using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/animated_list_demo_page.dart

public sealed class AnimatedListDemoPage : StatefulWidget
{
    public override State CreateState() => new AnimatedListDemoPageState();

    private sealed class AnimatedListDemoPageState : State
    {
        private readonly LabeledGlobalKey<AnimatedListState> _listKey = new("animated-list-demo");
        private readonly LabeledGlobalKey<SliverAnimatedListState> _sliverKey = new("sliver-animated-list-demo");
        private readonly List<int> _items = [1, 2, 3, 4];
        private readonly List<int> _sliverItems = [11, 12, 13, 14];
        private int _nextItem = 5;
        private int _nextSliverItem = 15;
        private string _status = "Insert or remove items to compare both list variants";

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("AnimatedList + SliverAnimatedList", fontSize: 20, color: Colors.Black),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            new TextButton(onPressed: InsertItem, child: new Text("Insert list item")),
                            new TextButton(
                                onPressed: _items.Count == 0 ? null : RemoveItem,
                                child: new Text("Remove list item")),
                            new TextButton(onPressed: InsertSliverItem, child: new Text("Insert sliver item")),
                            new TextButton(
                                onPressed: _sliverItems.Count == 0 ? null : RemoveSliverItem,
                                child: new Text("Remove sliver item")),
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
                                            new Text("AnimatedList.separated", fontSize: 14),
                                            new Expanded(BuildAnimatedList()),
                                        ])),
                                new Expanded(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            new Text("SliverAnimatedList", fontSize: 14),
                                            new Expanded(BuildSliverAnimatedList()),
                                        ])),
                            ])),
                ]);
        }

        private Widget BuildAnimatedList()
        {
            return AnimatedList.Separated(
                itemBuilder: (_, index, animation) => BuildTile(_items[index], animation),
                separatorBuilder: (_, _, animation) => new SizeTransition(
                    sizeFactor: animation,
                    child: new SizedBox(height: 4)),
                removedSeparatorBuilder: (_, _, animation) => new SizeTransition(
                    sizeFactor: animation,
                    child: new SizedBox(height: 4)),
                initialItemCount: _items.Count,
                padding: new Thickness(4),
                key: _listKey);
        }

        private Widget BuildSliverAnimatedList()
        {
            return new CustomScrollView(
                slivers:
                [
                    new SliverPadding(
                        new Thickness(4),
                        new SliverAnimatedList(
                            itemBuilder: (_, index, animation) => BuildTile(
                                _sliverItems[index],
                                animation,
                                new ValueKey<int>(_sliverItems[index])),
                            findChildIndexCallback: key => FindSliverItemIndex(key),
                            initialItemCount: _sliverItems.Count,
                            key: _sliverKey)),
                ]);
        }

        private static Widget BuildTile(int value, Animation<double> animation, Key? key = null)
        {
            return new SizeTransition(
                sizeFactor: animation,
                key: key,
                child: new Container(
                    margin: new Thickness(0, 2),
                    padding: new Thickness(12),
                    decoration: new BoxDecoration(
                        Color: value < 10 ? Color.Parse("#FFEADDFF") : Color.Parse("#FFD7E3FF"),
                        BorderRadius: BorderRadius.Circular(10)),
                    child: new Text($"Item {value}", color: Colors.Black)));
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
            _listKey.CurrentState!.InsertItem(index);
            SetState(() => _status = $"AnimatedList inserted Item {value} at {index}");
        }

        private void RemoveItem()
        {
            int index = _items.Count - 1;
            int value = _items[index];
            _items.RemoveAt(index);
            _listKey.CurrentState!.RemoveItem(index, (_, animation) => BuildTile(value, animation));
            SetState(() => _status = $"AnimatedList removed Item {value} from {index}");
        }

        private void InsertSliverItem()
        {
            int index = Math.Min(1, _sliverItems.Count);
            int value = _nextSliverItem++;
            _sliverItems.Insert(index, value);
            _sliverKey.CurrentState!.InsertItem(index);
            SetState(() => _status = $"SliverAnimatedList inserted Item {value} at {index}");
        }

        private void RemoveSliverItem()
        {
            int index = _sliverItems.Count - 1;
            int value = _sliverItems[index];
            _sliverItems.RemoveAt(index);
            _sliverKey.CurrentState!.RemoveItem(
                index,
                (_, animation) => BuildTile(value, animation, new ValueKey<int>(value)));
            SetState(() => _status = $"SliverAnimatedList removed Item {value} from {index}");
        }
    }
}
