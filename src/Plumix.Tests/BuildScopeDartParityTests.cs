using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/build_scope_test.dart (BuildScopeTests covers the
// BuildScope/BuildOwner mechanics these tests exercise, but none of these four tests exactly).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class BuildScopeDartParityTests
{
    // Flutter: build_scope_test.dart: "Legal times for setState"
    [Fact]
    public void LegalTimesForSetState()
    {
        using var tester = new FrameworkDartTester();
        // Dart runs each test file in its own isolate, so the static counter starts at zero.
        ProbeWidgetState.BuildCount = 0;
        GlobalKey flipKey = new LabeledGlobalKey<State>(null);
        Assert.Equal(0, ProbeWidgetState.BuildCount);
        tester.PumpWidget(new ProbeWidget(key: Key.Create("a")));
        Assert.Equal(1, ProbeWidgetState.BuildCount);
        tester.PumpWidget(new ProbeWidget(key: Key.Create("b")));
        Assert.Equal(2, ProbeWidgetState.BuildCount);
        tester.PumpWidget(new FrameworkDartFlipWidget(
            key: flipKey,
            left: new Container(),
            right: new ProbeWidget(key: Key.Create("c"))));
        Assert.Equal(2, ProbeWidgetState.BuildCount);
        var flipState1 = (FrameworkDartFlipWidgetState)((GlobalKey<State>)flipKey).CurrentState!;
        flipState1.Flip();
        tester.Pump();
        Assert.Equal(3, ProbeWidgetState.BuildCount);
        var flipState2 = (FrameworkDartFlipWidgetState)((GlobalKey<State>)flipKey).CurrentState!;
        flipState2.Flip();
        tester.Pump();
        Assert.Equal(3, ProbeWidgetState.BuildCount);
        tester.PumpWidget(new Container());
        Assert.Equal(3, ProbeWidgetState.BuildCount);
    }

    // Flutter: build_scope_test.dart: "Setting parent state during build is forbidden"
    [DebugOnlyFact]
    public void SettingParentStateDuringBuildIsForbidden()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new BadWidgetParent());
        Assert.IsType<FlutterError>(tester.TakeException());
        tester.PumpWidget(new Container());
    }

    // Flutter: build_scope_test.dart: "Setting state during dispose is forbidden"
    [DebugOnlyFact]
    public void SettingStateDuringDisposeIsForbidden()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new BadDisposeWidget());
        Assert.Null(tester.TakeException());
        tester.PumpWidget(new Container());
        Assert.NotNull(tester.TakeException());
    }

    // Flutter: build_scope_test.dart: "Dirty element list sort order"
    [Fact]
    public void DirtyElementListSortOrder()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key1 = new LabeledGlobalKey<State>("key1");
        GlobalKey key2 = new LabeledGlobalKey<State>("key2");

        bool didMiddle = false;
        Widget? middle = null;
        var setStates = new List<StateSetter>();
        Widget Builder(BuildContext context, StateSetter setState)
        {
            setStates.Add(setState);
            bool returnMiddle = !didMiddle;
            didMiddle = true;
            return new Wrapper(
                child: new Wrapper(child: new StatefulWrapper(child: returnMiddle ? middle! : new Container())));
        }

        Widget part1 = new Wrapper(
            child: new KeyedSubtree(
                key: key1,
                child: new StatefulBuilder(builder: Builder)));
        Widget part2 = new Wrapper(
            child: new KeyedSubtree(
                key: key2,
                child: new StatefulBuilder(builder: Builder)));

        middle = part2;
        tester.PumpWidget(part1);

        foreach (StatefulWrapperState state in tester.StateList<StatefulWrapperState>())
        {
            Assert.NotNull(state.Built);
            state.OldBuilt = state.Built!.Value;
            state.Trigger();
        }

        foreach (StateSetter setState in setStates)
        {
            setState(() => { });
        }

        StatefulWrapperState.BuildId = 0;
        middle = part1;
        didMiddle = false;
        tester.PumpWidget(part2);

        foreach (StatefulWrapperState state in tester.StateList<StatefulWrapperState>())
        {
            Assert.NotNull(state.Built);
            Assert.NotEqual(state.OldBuilt, state.Built);
        }
    }

    private sealed class ProbeWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new ProbeWidgetState();
    }

    private sealed class ProbeWidgetState : State<ProbeWidget>
    {
        public static int BuildCount;

        public override void InitState()
        {
            base.InitState();
            SetState(() => { });
        }

        public override void DidUpdateWidget(ProbeWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            SetState(() => { });
        }

        public override Widget Build(BuildContext context)
        {
            SetState(() => { });
            BuildCount++;
            return new Container();
        }
    }

    private sealed class BadWidget(BadWidgetParentState parentState, Key? key = null) : StatelessWidget(key)
    {
        public BadWidgetParentState ParentState { get; } = parentState;

        public override Widget Build(BuildContext context)
        {
            ParentState.MarkNeedsBuild();
            return new Container();
        }
    }

    private sealed class BadWidgetParent(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new BadWidgetParentState();
    }

    private sealed class BadWidgetParentState : State<BadWidgetParent>
    {
        public void MarkNeedsBuild()
        {
            SetState(() =>
            {
                // Our state didn't really change, but we're doing something pathological
                // here to trigger an interesting scenario to test.
            });
        }

        public override Widget Build(BuildContext context) => new BadWidget(this);
    }

    private sealed class BadDisposeWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new BadDisposeWidgetState();
    }

    private sealed class BadDisposeWidgetState : State<BadDisposeWidget>
    {
        public override Widget Build(BuildContext context) => new Container();

        public override void Dispose()
        {
            SetState(() =>
            {
                /* This is invalid behavior. */
            });
            base.Dispose();
        }
    }

    private sealed class StatefulWrapper(Widget child, Key? key = null) : StatefulWidget(key)
    {
        public Widget Child { get; } = child;

        public override State CreateState() => new StatefulWrapperState();
    }

    private sealed class StatefulWrapperState : State<StatefulWrapper>
    {
        public static int BuildId;

        public int? Built { get; private set; }

        public int OldBuilt { get; set; }

        public void Trigger() => SetState(() => Built = null);

        public override Widget Build(BuildContext context)
        {
            BuildId += 1;
            Built = BuildId;
            return Widget.Child;
        }
    }

    private sealed class Wrapper(Widget child, Key? key = null) : StatelessWidget(key)
    {
        public Widget Child { get; } = child;

        public override Widget Build(BuildContext context) => Child;
    }
}
