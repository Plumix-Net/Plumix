using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/reparent_state_harder_test.dart
// (a regression test for https://github.com/flutter/flutter/issues/5588).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ReparentStateHarderDartParityTests
{
    // Flutter: reparent_state_harder_test.dart: "Handle GlobalKey reparenting in weird orders"
    [Fact]
    public void HandleGlobalKeyReparentingInWeirdOrders()
    {
        using var tester = new FrameworkDartTester();
        // This is a bit of a weird test so let's try to explain it a bit.
        //
        // Basically what's happening here is that we have a complicated tree, and
        // in one frame, we change it to a slightly different tree with a specific
        // set of mutations:
        //
        // * The keyA subtree is regrafted to be one level higher, but later than
        //   the keyB subtree.
        // * The keyB subtree is, similarly, moved one level deeper, but earlier, than
        //   the keyA subtree.
        // * The keyD subtree is replaced by the previously earlier and shallower
        //   keyC subtree. This happens during a LayoutBuilder layout callback, so it
        //   happens long after A and B have finished their dance.
        //
        // The net result is that when keyC is moved, it has already been marked
        // dirty from being removed then reinserted into the tree (redundantly, as
        // it turns out, though this isn't known at the time), and has already been
        // visited once by the code that tries to clean nodes (though at that point
        // nothing happens since it isn't in the tree).
        //
        // This test verifies that none of the asserts go off during this dance.

        GlobalKey<OrderSwitcherState> keyRoot = new LabeledGlobalKey<OrderSwitcherState>("Root");
        GlobalKey keyA = new LabeledGlobalKey<State>("A");
        GlobalKey keyB = new LabeledGlobalKey<State>("B");
        GlobalKey keyC = new LabeledGlobalKey<State>("C");
        GlobalKey keyD = new LabeledGlobalKey<State>("D");
        tester.PumpWidget(new OrderSwitcher(
            key: keyRoot,
            a: new KeyedSubtree(
                key: keyA,
                child: new RekeyableDummyStatefulWidgetWrapper(initialKey: keyC)),
            b: new KeyedSubtree(
                key: keyB,
                child: new Builder(_ => new Builder(_ => new Builder(_ => new LayoutBuilder(
                    (_, _) => new RekeyableDummyStatefulWidgetWrapper(initialKey: keyD))))))));

        Assert.Single(tester.ElementsWithKey(keyA));
        Assert.Single(tester.ElementsWithKey(keyB));
        Assert.Single(tester.ElementsWithKey(keyC));
        Assert.Single(tester.ElementsWithKey(keyD));
        Assert.Equal(2, tester.ElementsOfType<RekeyableDummyStatefulWidgetWrapper>().Count);
        Assert.Equal(2, tester.ElementsOfType<DummyStatefulWidget>().Count);

        keyRoot.CurrentState!.SwitchChildren();
        List<State> states = tester.ElementsOfType<RekeyableDummyStatefulWidgetWrapper>()
            .Select(element => ((StatefulElement)element).State)
            .ToList();
        var a = (RekeyableDummyStatefulWidgetWrapperState)states[0];
        a.SetChild(null);
        var b = (RekeyableDummyStatefulWidgetWrapperState)states[1];
        b.SetChild(keyC);
        tester.Pump();

        Assert.Single(tester.ElementsWithKey(keyA));
        Assert.Single(tester.ElementsWithKey(keyB));
        Assert.Single(tester.ElementsWithKey(keyC));
        Assert.Empty(tester.ElementsWithKey(keyD));
        Assert.Equal(2, tester.ElementsOfType<RekeyableDummyStatefulWidgetWrapper>().Count);
        Assert.Equal(2, tester.ElementsOfType<DummyStatefulWidget>().Count);
    }

    private sealed class OrderSwitcher(Widget a, Widget b, Key? key = null) : StatefulWidget(key)
    {
        public Widget A { get; } = a;

        public Widget B { get; } = b;

        public override State CreateState() => new OrderSwitcherState();
    }

    private sealed class OrderSwitcherState : State<OrderSwitcher>
    {
        private bool _aFirst = true;

        public void SwitchChildren() => SetState(() => _aFirst = false);

        public override Widget Build(BuildContext context)
        {
            return new Stack(
                textDirection: TextDirection.Ltr,
                children: _aFirst
                    ? [new KeyedSubtree(child: Widget.A), Widget.B]
                    : [new KeyedSubtree(child: Widget.B), Widget.A]);
        }
    }

    private sealed class DummyStatefulWidget(Key? key) : StatefulWidget(key)
    {
        public override State CreateState() => new DummyStatefulWidgetState();
    }

    private sealed class DummyStatefulWidgetState : State<DummyStatefulWidget>
    {
        public override Widget Build(BuildContext context) => new Text("LEAF", textDirection: TextDirection.Ltr);
    }

    private sealed class RekeyableDummyStatefulWidgetWrapper(GlobalKey initialKey, Key? key = null)
        : StatefulWidget(key)
    {
        public GlobalKey InitialKey { get; } = initialKey;

        public override State CreateState() => new RekeyableDummyStatefulWidgetWrapperState();
    }

    private sealed class RekeyableDummyStatefulWidgetWrapperState : State<RekeyableDummyStatefulWidgetWrapper>
    {
        private GlobalKey? _key;

        public override void InitState()
        {
            base.InitState();
            _key = Widget.InitialKey;
        }

        public void SetChild(GlobalKey? value) => SetState(() => _key = value);

        public override Widget Build(BuildContext context) => new DummyStatefulWidget(_key);
    }
}
