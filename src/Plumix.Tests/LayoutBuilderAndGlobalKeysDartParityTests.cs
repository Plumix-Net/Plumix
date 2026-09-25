using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/layout_builder_and_global_keys_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class LayoutBuilderAndGlobalKeysDartParityTests
{
    // Flutter: layout_builder_and_global_keys_test.dart: "Moving global key inside a LayoutBuilder"
    [Fact]
    public void MovingGlobalKeyInsideALayoutBuilder()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<StatefulWrapperState> key = new LabeledGlobalKey<StatefulWrapperState>(null);
        tester.PumpWidget(new LayoutBuilder((_, _) => new Wrapper(
            child: new StatefulWrapper(key: key, child: new Container(height: 100.0)))));
        tester.PumpWidget(new LayoutBuilder((_, _) =>
        {
            key.CurrentState!.Trigger();
            return new StatefulWrapper(key: key, child: new Container(height: 100.0));
        }));

        Assert.Null(tester.TakeException());
    }

    // Flutter: layout_builder_and_global_keys_test.dart: "Moving GlobalKeys out of LayoutBuilder"
    [Fact]
    public void MovingGlobalKeysOutOfLayoutBuilder()
    {
        using var tester = new FrameworkDartTester();
        // Regression test for https://github.com/flutter/flutter/issues/146379.
        GlobalKey widgetKey = new LabeledGlobalKey<State>("widget key");
        Widget widgetWithKey = new Builder(context =>
        {
            Directionality.Of(context);
            return new SizedBox(key: widgetKey);
        });

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Row(
            [
                new LayoutBuilder((_, _) => widgetWithKey),
            ])));

        tester.PumpWidget(new Directionality(
            TextDirection.Rtl,
            new Row(
            [
                new LayoutBuilder((_, _) => new Placeholder()),
                widgetWithKey,
            ])));

        Assert.Null(tester.TakeException());
        Assert.Single(tester.ElementsWithKey(widgetKey));
    }

    // Flutter: layout_builder_and_global_keys_test.dart: "Moving global key inside a SliverLayoutBuilder"
    [Fact]
    public void MovingGlobalKeyInsideASliverLayoutBuilder()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey<StatefulWrapperState> key = new LabeledGlobalKey<StatefulWrapperState>(null);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                slivers:
                [
                    new SliverLayoutBuilder((_, _) => new SliverToBoxAdapter(
                        child: new Wrapper(
                            child: new StatefulWrapper(key: key, child: new Container(height: 100.0))))),
                ])));

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                slivers:
                [
                    new SliverLayoutBuilder((_, _) =>
                    {
                        key.CurrentState!.Trigger();
                        return new SliverToBoxAdapter(
                            child: new StatefulWrapper(key: key, child: new Container(height: 100.0)));
                    }),
                ])));

        Assert.Null(tester.TakeException());
    }

    private sealed class Wrapper(Widget child, Key? key = null) : StatelessWidget(key)
    {
        public Widget Child { get; } = child;

        public override Widget Build(BuildContext context) => Child;
    }

    private sealed class StatefulWrapper(Widget child, Key? key = null) : StatefulWidget(key)
    {
        public Widget Child { get; } = child;

        public override State CreateState() => new StatefulWrapperState();
    }

    private sealed class StatefulWrapperState : State<StatefulWrapper>
    {
        public void Trigger()
        {
            SetState(() =>
            {
                /* for test purposes */
            });
        }

        public override Widget Build(BuildContext context) => Widget.Child;
    }
}
