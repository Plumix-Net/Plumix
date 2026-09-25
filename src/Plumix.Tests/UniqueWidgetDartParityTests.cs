using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/unique_widget_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class UniqueWidgetDartParityTests
{
    // Flutter: unique_widget_test.dart: "Unique widget control test"
    [Fact]
    public void UniqueWidgetControlTest()
    {
        using var tester = new FrameworkDartTester();
        var widget = new TestUniqueWidget(new LabeledGlobalKey<TestUniqueWidgetState>(null));

        tester.PumpWidget(widget);

        TestUniqueWidgetState state = widget.CurrentState!;

        Assert.NotNull(state);

        tester.PumpWidget(new Container(child: widget));

        Assert.Equal(state, widget.CurrentState);
    }

    private sealed class TestUniqueWidget(GlobalKey<TestUniqueWidgetState> key)
        : UniqueWidget<TestUniqueWidgetState>(key)
    {
        public override TestUniqueWidgetState CreateState() => new();
    }

    private sealed class TestUniqueWidgetState : State<TestUniqueWidget>
    {
        public override Widget Build(BuildContext context) => new Container();
    }
}
