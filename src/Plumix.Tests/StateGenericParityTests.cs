using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class StateGenericParityTests
{
    // Flutter framework.dart: StatefulElement rejects a createState result whose T is not the widget type.
    [DebugOnlyFact]
    public void BuiltInStatesRejectTheWrongWidgetAcrossDirectAndSharedBases()
    {
        AssertWrongState(() => new FormState());
        AssertWrongState(() => new Scrollable.ScrollableState());
        AssertWrongState(() => new RawMenuAnchorState());
        AssertWrongState(() => new FormFieldState<string>());
    }

    // Flutter framework.dart: @optionalTypeArgs allows a raw State with no narrowed widget type.
    [Fact]
    public void RawStateRemainsValid()
    {
        var widget = new StateFactoryWidget(() => new RawState());
        var element = new StatefulElement(widget);
        Assert.IsType<RawState>(element.State);
    }

    // Flutter did_update_widget_test.dart: "Can call setState from didUpdateWidget".
    [Fact]
    public void TypedRestorationStateReceivesOldWidgetAfterCurrentWidgetChanges()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new RestorableProbe(1));
        var state = tester.State<RestorableProbeState>();

        tester.PumpWidget(new RestorableProbe(2));

        Assert.Same(state, tester.State<RestorableProbeState>());
        Assert.Equal((1, 2), state.LastUpdate);
        Assert.Null(tester.TakeException());
    }

    private static void AssertWrongState(Func<State> factory)
    {
        var error = Assert.Throws<FlutterError>(() => new StatefulElement(new StateFactoryWidget(factory)));
        Assert.Contains("StatefulWidget.createState must return a subtype of State<", error.Message);
    }

    private sealed class StateFactoryWidget(Func<State> factory) : StatefulWidget
    {
        public override State CreateState() => factory();
    }

    private sealed class RawState : State
    {
        public override Widget Build(BuildContext context) => SizedBox.Shrink();
    }

    private sealed class RestorableProbe(int value) : StatefulWidget
    {
        public int Value { get; } = value;

        public override State CreateState() => new RestorableProbeState();
    }

    private sealed class RestorableProbeState : RestorationState<RestorableProbe>
    {
        public (int Old, int Current)? LastUpdate { get; private set; }

        protected override string? RestorationId => null;

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
        }

        public override void DidUpdateWidget(RestorableProbe oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            LastUpdate = (oldWidget.Value, Widget.Value);
            SetState(() => { });
        }

        public override Widget Build(BuildContext context) => SizedBox.Shrink();
    }
}
