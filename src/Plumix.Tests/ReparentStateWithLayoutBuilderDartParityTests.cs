using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/reparent_state_with_layout_builder_test.dart
// (a regression test for https://github.com/flutter/flutter/issues/5840).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ReparentStateWithLayoutBuilderDartParityTests
{
    // Flutter: reparent_state_with_layout_builder_test.dart: "reparent state with layout builder"
    [Fact]
    public void ReparentStateWithLayoutBuilder()
    {
        using var tester = new FrameworkDartTester();
        Assert.Equal(0, StatefulCreationCounterState.CreationCount);
        tester.PumpWidget(new Bar());
        Assert.Equal(1, StatefulCreationCounterState.CreationCount);
        BarState s = tester.State<BarState>();
        s.Trigger();
        tester.Pump();
        Assert.Equal(1, StatefulCreationCounterState.CreationCount);
    }

    // Flutter: reparent_state_with_layout_builder_test.dart: "Clean then reparent with dependencies"
    [Fact]
    public void CleanThenReparentWithDependencies()
    {
        using var tester = new FrameworkDartTester();
        int layoutBuilderBuildCount = 0;

        StateSetter? keyedSetState = null;
        StateSetter? layoutBuilderSetState = null;
        StateSetter? childSetState = null;

        GlobalKey key = new LabeledGlobalKey<State>(null);
        Widget keyedWidget = new StatefulBuilder(
            key: key,
            builder: (context, setState) =>
            {
                keyedSetState = setState;
                MediaQuery.Of(context);
                return new Container();
            });

        Widget layoutBuilderChild = keyedWidget;
        Widget deepChild = new Container();

        var green = Avalonia.Media.Color.FromUInt32(0xff00ff00);

        tester.PumpWidget(new MediaQuery(
            data: MediaQueryData.FromView(tester.View),
            child: new Column(
            [
                new StatefulBuilder((_, setState) =>
                {
                    layoutBuilderSetState = setState;
                    return new LayoutBuilder((_, _) =>
                    {
                        layoutBuilderBuildCount += 1;
                        return layoutBuilderChild; // initially keyedWidget above, but then a new Container
                    });
                }),
                new ColoredBox(green, child: new ColoredBox(green, child: new ColoredBox(green,
                    child: new ColoredBox(green, child: new ColoredBox(green, child: new ColoredBox(green,
                        child: new StatefulBuilder((_, setState) =>
                        {
                            childSetState = setState;
                            return deepChild; // initially a Container, but then the keyedWidget above
                        }))))))),
            ])));
        Assert.Equal(1, layoutBuilderBuildCount);

        keyedSetState!(() =>
        {
            /* Change nothing but add the element to the dirty list. */
        });

        childSetState!(() =>
        {
            // The deep child builds in the initial build phase. It takes the child
            // from the LayoutBuilder before the LayoutBuilder has a chance to build.
            deepChild = keyedWidget;
        });

        layoutBuilderSetState!(() =>
        {
            // The layout builder will build in a separate build scope. This delays
            // the removal of the keyed child until this build scope.
            layoutBuilderChild = new Container();
        });

        // The essential part of this test is that this call to pump doesn't throw.
        tester.Pump();
        Assert.Equal(2, layoutBuilderBuildCount);
    }

    private sealed class Bar(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new BarState();
    }

    private sealed class BarState : State<Bar>
    {
        private readonly GlobalKey _fooKey = new LabeledGlobalKey<State>(null);

        private bool _mode;

        public void Trigger() => SetState(() => _mode = !_mode);

        public override Widget Build(BuildContext context)
        {
            if (_mode)
            {
                return new SizedBox(
                    child: new LayoutBuilder((_, _) => new StatefulCreationCounter(key: _fooKey)));
            }

            return new LayoutBuilder((_, _) => new StatefulCreationCounter(key: _fooKey));
        }
    }

    private sealed class StatefulCreationCounter(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new StatefulCreationCounterState();
    }

    private sealed class StatefulCreationCounterState : State<StatefulCreationCounter>
    {
        public static int CreationCount { get; private set; }

        public override void InitState()
        {
            base.InitState();
            CreationCount += 1;
        }

        public override Widget Build(BuildContext context) => new Container();
    }
}
