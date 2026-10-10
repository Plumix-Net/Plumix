using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from widgets/slivers_block_test.dart and two_dimensional_viewport_test.dart,
// plus regressions for the corresponding element forgetChild contracts.

namespace Plumix.Tests;

public sealed class ViewportChildLifecycleTests
{
    [Fact]
    public void GridDemo_CounterKeepsItsValueWhenMovedOutAndBack()
    {
        using var tester = new FrameworkDartTester();
        PumpWidget(tester, new TwoDimensionalScrollViewDemoPage());
        Element counter = tester.AllElements().Single(element =>
            element.Widget.Key is GlobalKey && element.Parent?.Widget is TwoDimensionalViewport);
        Assert.Equal(new Size(140, 44), ((RenderBox)counter.RenderObject!).Size);
        tester.Tap(tester.ElementsWithText("Count: 0").Single());
        tester.Pump();
        Assert.Single(tester.ElementsWithText("Count: 1"));

        tester.Tap(tester.ElementsWithText("Move counter above grid").Single());
        tester.Pump();
        Assert.Single(tester.ElementsWithText("Count: 1"));
        tester.Tap(tester.ElementsWithText("Count: 1").Single());
        tester.Pump();

        tester.Tap(tester.ElementsWithText("Return counter to grid").Single());
        tester.Pump();
        Assert.Single(tester.ElementsWithText("Count: 2"));
    }

    [DebugOnlyFact]
    public void SliverForgetChild_RequiresANonNullRegisteredSlot()
    {
        using var tester = new FrameworkDartTester();
        PumpWidget(tester, new CustomScrollView(slivers:
        [
            new SliverList(new RawSliverDelegate((_, _) => new SizedBox(height: 100))),
        ]));
        var element = (SliverMultiBoxAdaptorElement)tester.ElementOfType<SliverList>();
        Assert.Throws<AssertionError>(() => element.ForgetChild(new SizedBox().CreateElement()));
        Assert.Throws<AssertionError>(() => element.ForgetChild(new SlotProbe(99)));
        Assert.Equal(2, Children(element).Count);

        Element child = Children(element)[0];
        element.RemoveChild((RenderBox)child.RenderObject!);
        Assert.Throws<AssertionError>(() => element.ForgetChild(child));
        tester.Pump();
    }

    [Fact]
    public void SliverCreateChild_RetakesGlobalKeyAndRemovesItsOldSlot()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>("moving child");
        bool moved = false;
        var childDelegate = new RawSliverDelegate((_, index) => moved
            ? index == 0
                ? new Padding(new Thickness(0), child: new LifecycleProbe(key))
                : new SizedBox(height: 100)
            : index == 1 ? new LifecycleProbe(key) : new SizedBox(height: 100));
        PumpWidget(tester, new CustomScrollView(slivers: [new SliverList(childDelegate)]));
        var source = (SliverMultiBoxAdaptorElement)tester.ElementOfType<SliverList>();
        Element child = tester.ElementsWithKey(key).Single();
        var state = ((StatefulElement)child).State;

        moved = true;
        source.CreateChild(0, after: null);
        Assert.Same(child, tester.ElementsWithKey(key).Single());
        Assert.Same(state, ((StatefulElement)child).State);
        Assert.Single(Children(source));
        Assert.Same(Children(source)[0], child.Parent);
        Assert.Equal(0, child.Parent!.Slot);

        tester.Pump();
        Assert.Equal(2, Children(source).Count);
        Assert.Same(state, ((StatefulElement)tester.ElementsWithKey(key).Single()).State);
        Assert.Equal(new Point(0, 0), tester.GetTopLeft(child));
    }

    [Fact]
    public void SliverGlobalKeyReordering_MatchesFlutterOffsetsAndRemoval()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>("moving child");
        foreach (string order in new[] { "abc", "cab", "acb", "ab", "acb" })
        {
            PumpWidget(tester, new CustomScrollView(slivers:
            [
                SliverList.FromChildren(order.Select(letter => (Widget)new SizedBox(
                    height: letter == 'a' ? 251 : letter == 'b' ? 252 : 253,
                    key: letter == 'c' ? key : null)).ToList()),
            ]));
            var element = (SliverMultiBoxAdaptorElement)tester.ElementOfType<SliverList>();
            double offset = 0;
            List<Element> children = Children(element);
            Assert.Equal(order.Length, children.Count);
            for (int index = 0; index < order.Length; index++)
            {
                Assert.Equal(new Point(0, offset), tester.GetTopLeft(children[index]));
                offset += order[index] == 'a' ? 251 : order[index] == 'b' ? 252 : 253;
            }

            Assert.Equal(order.Contains('c') ? 1 : 0, tester.ElementsWithKey(key).Count);
        }
    }

    [DebugOnlyFact]
    public void TwoDimensionalForgetChild_RejectsExtractionDuringLayoutBeforeChangingMaps()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>("moving child");
        bool checkDuringLayout = false;
        bool checkedContract = false;
        Element? child = null;
        var childDelegate = new RawTableDelegate((context, _) =>
        {
            if (checkDuringLayout && !checkedContract)
            {
                checkedContract = true;
                Assert.Throws<AssertionError>(() => ((Element)context).ForgetChild(child!));
                Assert.Contains(child!, Children((Element)context));
            }

            return new LifecycleProbe(key);
        });
        PumpWidget(tester, new SimpleBuilderTableView(childDelegate));
        Element viewport = tester.ElementOfType<SimpleBuilderTableViewport>();
        child = Children(viewport).Single();
        var state = ((StatefulElement)child).State;

        checkDuringLayout = true;
        childDelegate.NotifyListeners();
        tester.Pump();
        Assert.True(checkedContract);
        Assert.Same(child, Children(viewport).Single());
        Assert.Same(state, ((StatefulElement)child).State);
    }

    [Fact]
    public void TwoDimensionalGlobalKeyRetake_ClearsBothMapsAndPreservesState()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>("moving child");
        Widget Build(bool outside) => new Column(children:
        [
            new SizedBox(height: 100, child: outside ? new LifecycleProbe(key) : null),
            new Expanded(child: new SimpleBuilderTableView(new RawTableDelegate((_, _) =>
                outside ? new SizedBox() : new LifecycleProbe(key)))),
        ]);
        PumpWidget(tester, Build(outside: false));
        Element child = tester.ElementsWithKey(key).Single();
        var state = ((StatefulElement)child).State;
        Element oldParent = child.Parent!;
        Assert.IsType<TwoDimensionalViewportElement>(oldParent);

        PumpWidget(tester, Build(outside: true));
        Assert.Same(child, tester.ElementsWithKey(key).Single());
        Assert.Same(state, ((StatefulElement)child).State);
        Assert.DoesNotContain(child, Children(oldParent));
        Assert.Single(Children(oldParent));

        PumpWidget(tester, Build(outside: false));
        Assert.Same(child, tester.ElementsWithKey(key).Single());
        Assert.Same(oldParent, child.Parent);
        Assert.Same(state, ((StatefulElement)child).State);
        Assert.Same(child, Children(oldParent).Single());
        Assert.Equal(new Point(0, 100), tester.GetTopLeft(child));
    }

    [Fact]
    public void TwoDimensionalEndLayout_DisposesObsoleteKeyedAndUnkeyedChildren()
    {
        using var tester = new FrameworkDartTester();
        var key = new ValueKey<string>("keyed");
        PumpWidget(tester, new SimpleBuilderTableView(new TwoDimensionalChildBuilderDelegate(
            (_, vicinity) => new LifecycleProbe(vicinity.XIndex == 0 ? key : null),
            maxXIndex: 1,
            maxYIndex: 0,
            addAutomaticKeepAlives: false,
            addRepaintBoundaries: false)));
        LifecycleProbeState[] states = tester.StateList<LifecycleProbeState>().ToArray();
        Assert.Equal(2, states.Length);

        PumpWidget(tester, new SimpleBuilderTableView(new TwoDimensionalChildBuilderDelegate(
            (_, _) => new SizedBox(), maxXIndex: 1, maxYIndex: 0)));
        Assert.All(states, state => Assert.True(state.Disposed));
    }

    [Fact]
    public void TwoDimensionalElementDiagnostics_SortsAndNamesChildrenByVicinity()
    {
        using var tester = new FrameworkDartTester();
        PumpWidget(tester, new SimpleBuilderTableView(TwoDimensionalHarness.BuilderDelegate(
            maxXIndex: 1, maxYIndex: 1)));
        Element viewport = tester.ElementOfType<SimpleBuilderTableViewport>();
        ((RenderBox)viewport.RenderObject!).MarkNeedsLayout();
        tester.Pump();
        List<DiagnosticsNode> nodes = viewport.DebugDescribeChildren();
        ChildVicinity[] expected = [new(0, 0), new(0, 1), new(1, 0), new(1, 1)];
        Assert.Equal(expected.Select(vicinity => vicinity.ToString()), nodes.Select(node => node.Name));
        Assert.Equal(
            Children(viewport).OrderBy(child => (ChildVicinity)child.Slot!),
            nodes.Select(node => node.Value));
    }

    private static void PumpWidget(FrameworkDartTester tester, Widget widget) =>
        tester.PumpWidget(new Directionality(TextDirection.Ltr, widget));

    private static List<Element> Children(Element element)
    {
        var children = new List<Element>();
        element.VisitChildren(children.Add);
        return children;
    }

    private sealed class RawSliverDelegate(Func<BuildContext, int, Widget?> builder) : SliverChildDelegate
    {
        public override int? EstimatedChildCount => 2;

        public override Widget? Build(BuildContext context, int index) =>
            index is >= 0 and < 2 ? builder(context, index) : null;

        public override bool ShouldRebuild(SliverChildDelegate oldDelegate) => true;
    }

    // Omit delegate wrappers so the GlobalKey belongs directly to the viewport element.
    private sealed class RawTableDelegate(TwoDimensionalIndexedWidgetBuilder builder)
        : TwoDimensionalChildBuilderDelegate(builder, maxXIndex: 0, maxYIndex: 0)
    {
        public override Widget? Build(BuildContext context, ChildVicinity vicinity) =>
            vicinity == new ChildVicinity(0, 0) ? Builder(context, vicinity) : null;
    }

    private sealed class LifecycleProbe(Key? key) : StatefulWidget(key)
    {
        public override State CreateState() => new LifecycleProbeState();
    }

    private sealed class SlotProbe : Element
    {
        public SlotProbe(int slot) : base(new SizedBox())
        {
            Slot = slot;
        }
    }

    private sealed class LifecycleProbeState : State<LifecycleProbe>
    {
        public bool Disposed { get; private set; }

        public override Widget Build(BuildContext context) => new SizedBox(height: 100);

        public override void Dispose()
        {
            Disposed = true;
            base.Dispose();
        }
    }
}
