using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/set_state_1_test.dart
// Mirrors flutter/packages/flutter/test/widgets/set_state_2_test.dart
// Mirrors flutter/packages/flutter/test/widgets/set_state_3_test.dart
// Mirrors flutter/packages/flutter/test/widgets/set_state_4_test.dart
// Mirrors flutter/packages/flutter/test/widgets/set_state_5_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SetStateDartParityTests
{
    // Flutter: set_state_1_test.dart: "setState() smoke test"
    [Fact]
    public void SetStateSmokeTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Outside());
        Point location = tester.GetCenter(tester.ElementsWithText("INSIDE").Single());
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        tester.Pump();
        gesture.Up();
        tester.Pump();
    }

    // Flutter: set_state_2_test.dart: "setState() overbuild test"
    [Fact]
    public void SetStateOverbuildTest()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();
        var inner = new Builder(_ =>
        {
            log.Add("inner");
            return new Text("inner", textDirection: TextDirection.Ltr);
        });
        int value = 0;
        tester.PumpWidget(new Builder(_ =>
        {
            log.Add("outer");
            return new StatefulBuilder((_, setState) =>
            {
                log.Add("stateful");
                return new GestureDetector(
                    onTap: () => setState(() => value += 1),
                    child: new Builder(_ =>
                    {
                        log.Add($"middle {value}");
                        return inner;
                    }));
            });
        }));
        log.Add("---");
        tester.Tap(tester.ElementsWithText("inner").Single());
        tester.Pump();
        log.Add("---");
        Assert.Equal(
            ["outer", "stateful", "middle 0", "inner", "---", "stateful", "middle 1", "---"],
            log);
    }

    // Flutter: set_state_3_test.dart: "three-way setState() smoke test"
    [Fact]
    public void ThreeWaySetStateSmokeTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Changer3(new Wrapper3(new Leaf3())));
        tester.PumpWidget(new Changer3(new Wrapper3(new Leaf3())));
        Changer3State.Changer!.Test();
        tester.Pump();
    }

    // Flutter: set_state_4_test.dart: "setState() catches being used with an async callback"
    [DebugOnlyFact]
    public void SetStateCatchesBeingUsedWithAnAsyncCallback()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Changer4());
        Changer4State s = tester.State<Changer4State>();
        Assert.Null(Record.Exception(s.Test0) as FlutterError);
        Assert.Null(Record.Exception(s.Test1) as FlutterError);
        Assert.Throws<FlutterError>(s.Test2);
    }

    // Flutter: set_state_5_test.dart: "setState() catches being used inside a constructor"
    [DebugOnlyFact]
    public void SetStateCatchesBeingUsedInsideAConstructor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new BadWidget());
        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // ------------------------------------------------------------------ set_state_1_test.dart

    private sealed class Inside(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new InsideState();
    }

    private sealed class InsideState : State<Inside>
    {
        public override Widget Build(BuildContext context)
        {
            return new Listener(
                onPointerDown: HandlePointerDown,
                child: new Text("INSIDE", textDirection: TextDirection.Ltr));
        }

        private void HandlePointerDown(PointerDownEvent @event) => SetState(() => { });
    }

    private sealed class Middle(Inside? child = null, Key? key = null) : StatefulWidget(key)
    {
        public Inside? Child { get; } = child;

        public override State CreateState() => new MiddleState();
    }

    private sealed class MiddleState : State<Middle>
    {
        public override Widget Build(BuildContext context)
            => new Listener(onPointerDown: HandlePointerDown, child: Widget.Child);

        private void HandlePointerDown(PointerDownEvent @event) => SetState(() => { });
    }

    private sealed class Outside(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new OutsideState();
    }

    private sealed class OutsideState : State<Outside>
    {
        public override Widget Build(BuildContext context) => new Middle(child: new Inside());
    }

    // ------------------------------------------------------------------ set_state_3_test.dart

    private sealed class Changer3(Widget child, Key? key = null) : StatefulWidget(key)
    {
        public Widget Child { get; } = child;

        public override State CreateState() => new Changer3State();
    }

    private sealed class Changer3State : State<Changer3>
    {
        /// <summary>Dart's top-level <c>late ChangerState changer</c>.</summary>
        public static Changer3State? Changer;

        private bool _state;

        public override void InitState()
        {
            base.InitState();
            Changer = this;
        }

        public void Test() => SetState(() => _state = true);

        public override Widget Build(BuildContext context) => _state ? new Wrapper3(Widget.Child) : Widget.Child;
    }

    private sealed class Wrapper3(Widget child, Key? key = null) : StatelessWidget(key)
    {
        public Widget Child { get; } = child;

        public override Widget Build(BuildContext context) => Child;
    }

    private sealed class Leaf3(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new Leaf3State();
    }

    private sealed class Leaf3State : State<Leaf3>
    {
        public override Widget Build(BuildContext context) => new Text("leaf", textDirection: TextDirection.Ltr);
    }

    // ------------------------------------------------------------------ set_state_4_test.dart

    private sealed class Changer4(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new Changer4State();
    }

    private sealed class Changer4State : State<Changer4>
    {
        public void Test0() => SetState(() => { });

        // Dart's `setState(() => 1)`: a callback that yields a non-Future value. A C# Action cannot
        // return one, so the value is computed and discarded.
        public void Test1() => SetState(() => _ = 1);

#pragma warning disable CS1998 // Dart's `setState(() async {})`: an async callback with no await.
        public void Test2() => SetState(async () => { });
#pragma warning restore CS1998

        public override Widget Build(BuildContext context) => new Text("test", textDirection: TextDirection.Ltr);
    }

    // ------------------------------------------------------------------ set_state_5_test.dart

    private sealed class BadWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new BadWidgetState();
    }

    private sealed class BadWidgetState : State<BadWidget>
    {
        private int _count;

        public BadWidgetState()
        {
            SetState(() => _count = 1);
        }

        public override Widget Build(BuildContext context) => new Text(_count.ToString());
    }
}
