using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/global_keys_moving_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class GlobalKeysMovingDartParityTests
{
    private readonly List<Item> _items = [new Item(), new Item()];

    // Flutter: global_keys_moving_test.dart: "moving subtrees with global keys - smoketest"
    [Fact]
    public void MovingSubtreesWithGlobalKeysSmoketest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Builder());
        StatefulLeafState leaf = tester.StateList<StatefulLeafState>().First();
        leaf.MarkNeedsBuild();
        tester.Pump();
        Item lastItem = _items[1];
        _items.Remove(lastItem);
        _items.Insert(0, lastItem);
        tester.PumpWidget(Builder()); // this marks the app dirty and rebuilds it
    }

    private Widget Builder()
    {
        return new Column(
        [
            new KeyedWrapper(_items[1].Key1, _items[1].Key2),
            new KeyedWrapper(_items[0].Key1, _items[0].Key2),
        ]);
    }

    private sealed class Item
    {
        public GlobalKey Key1 { get; } = new LabeledGlobalKey<State>(null);

        public GlobalKey Key2 { get; } = new LabeledGlobalKey<State>(null);

        public override string ToString() => $"Item({Key1}, {Key2})";
    }

    private sealed class StatefulLeaf(GlobalKey? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new StatefulLeafState();
    }

    private sealed class StatefulLeafState : State<StatefulLeaf>
    {
        public void MarkNeedsBuild() => SetState(() => { });

        public override Widget Build(BuildContext context) => new Text("leaf", textDirection: TextDirection.Ltr);
    }

    private sealed class KeyedWrapper(Key key1, GlobalKey key2, Key? key = null) : StatelessWidget(key)
    {
        public Key Key1 { get; } = key1;

        public GlobalKey Key2 { get; } = key2;

        public override Widget Build(BuildContext context)
        {
            return new Container(key: Key1, child: new StatefulLeaf(key: Key2));
        }
    }
}
