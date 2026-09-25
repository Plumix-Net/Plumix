using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/stateful_component_test.dart
// Mirrors flutter/packages/flutter/test/widgets/stateful_components_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class StatefulComponentDartParityTests
{
    // ------------------------------------------------------------ stateful_component_test.dart

    // Flutter: stateful_component_test.dart: "Stateful widget smoke test"
    [Fact(Skip = "Parity gap: Focus (via View's FocusTraversalGroup) adds a Listener, a second "
        + "SingleChildRenderObjectElement Dart's _FocusState.build lacks (BACKLOG Focus row)")]
    public void StatefulWidgetSmokeTest()
    {
        using var tester = new FrameworkDartTester();

        void CheckTree(BoxDecoration expectedDecoration)
        {
            Element element = tester.AllElements()
                .Where(element => element is SingleChildRenderObjectElement
                    && element.RenderObject is not RenderView)
                .Single();
            Assert.NotNull(element);
            Assert.IsAssignableFrom<RenderDecoratedBox>(element.RenderObject);
            var renderObject = (RenderDecoratedBox)element.RenderObject!;
            Assert.Equal(expectedDecoration, renderObject.Decoration);
        }

        tester.PumpWidget(new FrameworkDartFlipWidget(
            left: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA),
            right: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)));

        CheckTree(FrameworkDartTestWidgets.BoxDecorationA);

        tester.PumpWidget(new FrameworkDartFlipWidget(
            left: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB),
            right: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA)));

        CheckTree(FrameworkDartTestWidgets.BoxDecorationB);

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);

        tester.Pump();

        CheckTree(FrameworkDartTestWidgets.BoxDecorationA);

        tester.PumpWidget(new FrameworkDartFlipWidget(
            left: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA),
            right: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)));

        CheckTree(FrameworkDartTestWidgets.BoxDecorationB);
    }

    // Flutter: stateful_component_test.dart: "Don't rebuild subwidgets"
    [Fact]
    public void DontRebuildSubwidgets()
    {
        using var tester = new FrameworkDartTester();
        // Dart runs each test file in its own isolate, so the static counter starts at zero.
        TestBuildCounter.BuildCount = 0;
        tester.PumpWidget(new FrameworkDartFlipWidget(
            key: Key.Create("rebuild test"),
            left: new TestBuildCounter(),
            right: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)));

        Assert.Equal(1, TestBuildCounter.BuildCount);

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);

        tester.Pump();

        Assert.Equal(1, TestBuildCounter.BuildCount);
    }

    // ----------------------------------------------------------- stateful_components_test.dart

    // Flutter: stateful_components_test.dart: "resync stateful widget"
    [Fact]
    public void ResyncStatefulWidget()
    {
        using var tester = new FrameworkDartTester();
        Key innerKey = Key.Create("inner");
        Key outerKey = Key.Create("outer");

        var inner1 = new InnerWidget(key: innerKey);
        InnerWidget inner2;
        var outer1 = new OuterContainer(key: outerKey, child: inner1);
        OuterContainer outer2;

        tester.PumpWidget(outer1);

        var innerElement = (StatefulElement)tester.ElementsWithKey(innerKey).Single();
        var innerElementState = (InnerWidgetState)innerElement.State;
        Assert.Same(inner1, innerElementState.Widget);
        Assert.True(innerElementState.DidInitState);
        Assert.True(innerElement.RenderObject!.Attached);

        inner2 = new InnerWidget(key: innerKey);
        outer2 = new OuterContainer(key: outerKey, child: inner2);

        tester.PumpWidget(outer2);

        Assert.Same(innerElement, tester.ElementsWithKey(innerKey).Single());
        Assert.Same(innerElementState, innerElement.State);

        Assert.Same(inner2, innerElementState.Widget);
        Assert.True(innerElementState.DidInitState);
        Assert.True(innerElement.RenderObject!.Attached);

        var outerElement = (StatefulElement)tester.ElementsWithKey(outerKey).Single();
        Assert.Same(outer2, ((OuterContainerState)outerElement.State).Widget);
        outerElement.MarkNeedsBuild();
        tester.Pump();

        Assert.Same(innerElement, tester.ElementsWithKey(innerKey).Single());
        Assert.Same(innerElementState, innerElement.State);
        Assert.Same(inner2, innerElementState.Widget);
        Assert.True(innerElement.RenderObject!.Attached);
    }

    /// <summary>flutter/packages/flutter/test/widgets/test_widgets.dart: <c>TestBuildCounter</c>.</summary>
    private sealed class TestBuildCounter(Key? key = null) : StatelessWidget(key)
    {
        public static int BuildCount;

        public override Widget Build(BuildContext context)
        {
            BuildCount += 1;
            return new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA);
        }
    }

    private sealed class InnerWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new InnerWidgetState();
    }

    private sealed class InnerWidgetState : State<InnerWidget>
    {
        public bool DidInitState { get; private set; }

        public override void InitState()
        {
            base.InitState();
            DidInitState = true;
        }

        public override Widget Build(BuildContext context) => new Container();
    }

    private sealed class OuterContainer(InnerWidget child, Key? key = null) : StatefulWidget(key)
    {
        public InnerWidget Child { get; } = child;

        public override State CreateState() => new OuterContainerState();
    }

    private sealed class OuterContainerState : State<OuterContainer>
    {
        public override Widget Build(BuildContext context) => Widget.Child;
    }
}
