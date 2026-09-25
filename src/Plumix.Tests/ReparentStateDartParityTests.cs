using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/reparent_state_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ReparentStateDartParityTests
{
    private static readonly Avalonia.Media.Color Green = Avalonia.Media.Color.FromUInt32(0xff00ff00);

    // Flutter: reparent_state_test.dart: "can reparent state"
    [Fact]
    public void CanReparentState()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<State> left = new LabeledGlobalKey<State>(null);
        GlobalKey<State> right = new LabeledGlobalKey<State>(null);

        var grandchild = new StateMarker();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new ColoredBox(Green, child: new StateMarker(key: left)),
                new ColoredBox(Green, child: new StateMarker(key: right, child: grandchild)),
            ]));

        var leftState = (StateMarkerState)left.CurrentState!;
        leftState.Marker = "left";
        var rightState = (StateMarkerState)right.CurrentState!;
        rightState.Marker = "right";

        StateMarkerState grandchildState = StateOf(tester, grandchild);
        Assert.NotNull(grandchildState);
        grandchildState.Marker = "grandchild";

        var newGrandchild = new StateMarker();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new ColoredBox(Green, child: new StateMarker(key: right, child: newGrandchild)),
                new ColoredBox(Green, child: new StateMarker(key: left)),
            ]));

        Assert.Same(leftState, left.CurrentState);
        Assert.Equal("left", leftState.Marker);
        Assert.Same(rightState, right.CurrentState);
        Assert.Equal("right", rightState.Marker);

        StateMarkerState newGrandchildState = StateOf(tester, newGrandchild);
        Assert.NotNull(newGrandchildState);
        Assert.Same(grandchildState, newGrandchildState);
        Assert.Equal("grandchild", newGrandchildState.Marker);

        tester.PumpWidget(new Center(
            child: new ColoredBox(Green, child: new StateMarker(key: left, child: new Container()))));

        Assert.Same(leftState, left.CurrentState);
        Assert.Equal("left", leftState.Marker);
        Assert.Null(right.CurrentState);
    }

    // Flutter: reparent_state_test.dart: "can reparent state with multichild widgets"
    [Fact]
    public void CanReparentStateWithMultichildWidgets()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<State> left = new LabeledGlobalKey<State>(null);
        GlobalKey<State> right = new LabeledGlobalKey<State>(null);

        var grandchild = new StateMarker();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(key: left),
                new StateMarker(key: right, child: grandchild),
            ]));

        var leftState = (StateMarkerState)left.CurrentState!;
        leftState.Marker = "left";
        var rightState = (StateMarkerState)right.CurrentState!;
        rightState.Marker = "right";

        StateMarkerState grandchildState = StateOf(tester, grandchild);
        Assert.NotNull(grandchildState);
        grandchildState.Marker = "grandchild";

        var newGrandchild = new StateMarker();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(key: right, child: newGrandchild),
                new StateMarker(key: left),
            ]));

        Assert.Same(leftState, left.CurrentState);
        Assert.Equal("left", leftState.Marker);
        Assert.Same(rightState, right.CurrentState);
        Assert.Equal("right", rightState.Marker);

        StateMarkerState newGrandchildState = StateOf(tester, newGrandchild);
        Assert.NotNull(newGrandchildState);
        Assert.Same(grandchildState, newGrandchildState);
        Assert.Equal("grandchild", newGrandchildState.Marker);

        tester.PumpWidget(new Center(
            child: new ColoredBox(Green, child: new StateMarker(key: left, child: new Container()))));

        Assert.Same(leftState, left.CurrentState);
        Assert.Equal("left", leftState.Marker);
        Assert.Null(right.CurrentState);
    }

    // Flutter: reparent_state_test.dart: "can with scrollable list"
    [Fact]
    public void CanWithScrollableList()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<State> key = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new StateMarker(key: key));

        var keyState = (StateMarkerState)key.CurrentState!;
        keyState.Marker = "marked";

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new ListView(
                itemExtent: 100.0,
                children:
                [
                    new SizedBox(
                        key: Key.Create("container"),
                        height: 100.0,
                        child: new StateMarker(key: key)),
                ])));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);

        tester.PumpWidget(new StateMarker(key: key));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);
    }

    // Flutter: reparent_state_test.dart: "Reparent during update children"
    [Fact]
    public void ReparentDuringUpdateChildren()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<State> key = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(key: key),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        var keyState = (StateMarkerState)key.CurrentState!;
        keyState.Marker = "marked";

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0),
                new StateMarker(key: key),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(key: key),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);
    }

    // Flutter: reparent_state_test.dart: "Reparent to child during update children"
    [Fact]
    public void ReparentToChildDuringUpdateChildren()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<State> key = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0),
                new StateMarker(key: key),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        var keyState = (StateMarkerState)key.CurrentState!;
        keyState.Marker = "marked";

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0, child: new StateMarker(key: key)),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0),
                new StateMarker(key: key),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0),
                new SizedBox(width: 100.0, height: 100.0, child: new StateMarker(key: key)),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SizedBox(width: 100.0, height: 100.0),
                new StateMarker(key: key),
                new SizedBox(width: 100.0, height: 100.0),
            ]));

        Assert.Same(keyState, key.CurrentState);
        Assert.Equal("marked", keyState.Marker);
    }

    // Flutter: reparent_state_test.dart: "Deactivate implies build"
    [Fact]
    public void DeactivateImpliesBuild()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key = new LabeledGlobalKey<State>(null);
        var log = new List<string>();
        var logger = new DeactivateLogger(key: key, log: log);

        tester.PumpWidget(new Container(key: new UniqueKey(), child: logger));

        Assert.Equal(["build"], log);

        tester.PumpWidget(new Container(key: new UniqueKey(), child: logger));

        Assert.Equal(["build", "deactivate", "build"], log);
        log.Clear();

        tester.Pump();
        Assert.Empty(log);
    }

    // Flutter: reparent_state_test.dart: "Reparenting with multiple moves"
    [Fact]
    public void ReparentingWithMultipleMoves()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key1 = new LabeledGlobalKey<State>(null);
        GlobalKey key2 = new LabeledGlobalKey<State>(null);
        GlobalKey key3 = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new Row(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(
                    key: key1,
                    child: new StateMarker(
                        key: key2,
                        child: new StateMarker(
                            key: key3,
                            child: new StateMarker(child: new Container(width: 100.0))))),
            ]));

        tester.PumpWidget(new Row(
            textDirection: TextDirection.Ltr,
            children:
            [
                new StateMarker(
                    key: key2,
                    child: new StateMarker(child: new Container(width: 100.0))),
                new StateMarker(
                    key: key1,
                    child: new StateMarker(
                        key: key3,
                        child: new StateMarker(child: new Container(width: 100.0)))),
            ]));
    }

    /// <summary>Dart's <c>tester.state(find.byWidget(widget))</c>.</summary>
    private static StateMarkerState StateOf(FrameworkDartTester tester, Widget widget)
        => (StateMarkerState)((StatefulElement)tester.ElementOfWidget(widget)).State;

    private sealed class StateMarker(Key? key = null, Widget? child = null) : StatefulWidget(key)
    {
        public Widget? Child { get; } = child;

        public override State CreateState() => new StateMarkerState();
    }

    private sealed class StateMarkerState : State<StateMarker>
    {
        public string? Marker { get; set; }

        public override Widget Build(BuildContext context) => Widget.Child ?? new Container();
    }

    private sealed class DeactivateLogger(Key key, List<string> log) : StatefulWidget(key)
    {
        public List<string> Log { get; } = log;

        public override State CreateState() => new DeactivateLoggerState();
    }

    private sealed class DeactivateLoggerState : State<DeactivateLogger>
    {
        public override void Deactivate()
        {
            Widget.Log.Add("deactivate");
            base.Deactivate();
        }

        public override Widget Build(BuildContext context)
        {
            Widget.Log.Add("build");
            return new Container();
        }
    }
}
