using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/global_keys_duplicated_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class GlobalKeysDuplicatedDartParityTests
{
    // Dart's `GlobalObjectKey(0)` compares with `identical(0, 0)`, which holds for Dart's canonical
    // ints; a C# box of 0 is a fresh object each time, so every key shares this one box.
    private static readonly object Zero = 0;

    // Flutter: global_keys_duplicated_test.dart: "GlobalKey children of one node"
    [DebugOnlyFact]
    public void GlobalKeyChildrenOfOneNode()
    {
        using var tester = new FrameworkDartTester();
        // This is actually a test of the regular duplicate key logic, which
        // happens before the duplicate GlobalKey logic.
        tester.PumpWidget(new Stack(
            children:
            [
                new DummyWidget(key: new GlobalObjectKey<State>(Zero)),
                new DummyWidget(key: new GlobalObjectKey<State>(Zero)),
            ]));
        object? error = tester.TakeException();
        Assert.IsAssignableFrom<FlutterError>(error);
        string message = error!.ToString()!;
        Assert.StartsWith("Duplicate keys found.\n", message);
        Assert.Contains("Stack", message);
        Assert.Contains($"[GlobalObjectKey {Diagnostics.DescribeIdentity(Zero)}]", message);
    }

    // Flutter: global_keys_duplicated_test.dart: "GlobalKey children of two nodes - A"
    [DebugOnlyFact]
    public void GlobalKeyChildrenOfTwoNodesA()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new DummyWidget(child: new DummyWidget(key: new GlobalObjectKey<State>(Zero))),
                new DummyWidget(child: new DummyWidget(key: new GlobalObjectKey<State>(Zero))),
            ]));
        object? error = tester.TakeException();
        Assert.IsAssignableFrom<FlutterError>(error);
        string message = error!.ToString()!;
        Assert.StartsWith("Multiple widgets used the same GlobalKey.\n", message);
        Assert.Contains("different widgets that both had the following description", message);
        Assert.Contains("DummyWidget", message);
        Assert.Contains($"[GlobalObjectKey {Diagnostics.DescribeIdentity(Zero)}]", message);
        Assert.EndsWith(
            "\nA GlobalKey can only be specified on one widget at a time in the widget tree.",
            message);
    }

    // Flutter: global_keys_duplicated_test.dart: "GlobalKey children of two different nodes - B"
    [DebugOnlyFact]
    public void GlobalKeyChildrenOfTwoDifferentNodesB()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new DummyWidget(child: new DummyWidget(key: new GlobalObjectKey<State>(Zero))),
                new DummyWidget(
                    key: Key.Create("x"),
                    child: new DummyWidget(key: new GlobalObjectKey<State>(Zero))),
            ]));
        object? error = tester.TakeException();
        Assert.IsAssignableFrom<FlutterError>(error);
        string message = error!.ToString()!;
        Assert.StartsWith("Multiple widgets used the same GlobalKey.\n", message);
        Assert.DoesNotContain("different widgets that both had the following description", message);
        Assert.Contains("DummyWidget", message);
        Assert.Contains("DummyWidget-[<'x'>]", message);
        Assert.Contains($"[GlobalObjectKey {Diagnostics.DescribeIdentity(Zero)}]", message);
        Assert.EndsWith(
            "\nA GlobalKey can only be specified on one widget at a time in the widget tree.",
            message);
    }

    // Flutter: global_keys_duplicated_test.dart: "GlobalKey children of two nodes - C"
    [DebugOnlyFact]
    public void GlobalKeyChildrenOfTwoNodesC()
    {
        using var tester = new FrameworkDartTester();
        StateSetter? nestedSetState = null;
        bool flag = false;
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new DummyWidget(child: new DummyWidget(key: new GlobalObjectKey<State>(Zero))),
                new DummyWidget(
                    child: new StatefulBuilder((_, setState) =>
                    {
                        nestedSetState = setState;
                        if (flag)
                        {
                            return new DummyWidget(key: new GlobalObjectKey<State>(Zero));
                        }

                        return new DummyWidget();
                    })),
            ]));
        nestedSetState!(() => flag = true);
        tester.Pump();
        object? error = tester.TakeException();
        string message = error!.ToString()!;
        Assert.StartsWith("Duplicate GlobalKey detected in widget tree.\n", message);
        Assert.Contains("The following GlobalKey was specified multiple times", message);
        // The following line is verifying the grammar is correct in this common case.
        // We should probably also verify the three other combinations that can be generated...
        Assert.Contains(
            "This was determined by noticing that after the widget with the above global key was moved out of "
            + "its previous parent, that previous parent never updated during this frame, meaning that it "
            + "either did not update at all or updated before the widget was moved, in either case implying "
            + "that it still thinks that it should have a child with that global key.",
            string.Join(" ", message.Split('\n')));
        Assert.Contains($"[GlobalObjectKey {Diagnostics.DescribeIdentity(Zero)}]", message);
        Assert.Contains("DummyWidget", message);
        Assert.EndsWith(
            "\nA GlobalKey can only be specified on one widget at a time in the widget tree.",
            message);
        Assert.IsAssignableFrom<FlutterError>(error);
    }

    private sealed class DummyWidget(Key? key = null, Widget? child = null) : StatelessWidget(key)
    {
        public Widget? Child { get; } = child;

        public override Widget Build(BuildContext context)
        {
            return Child
                ?? new LimitedBox(
                    maxWidth: 0.0,
                    maxHeight: 0.0,
                    child: new ConstrainedBox(BoxConstraints.Expand()));
        }
    }
}
