using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/framework_test.dart (the tests not already covered by
// FrameworkParityTests, BuildScopeTests, RootWidgetTests and friends).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FrameworkDartParityTests
{
    private static readonly Avalonia.Media.Color Green = Avalonia.Media.Color.FromUInt32(0xFF00FF00);

    private const string DuplicateKeysStackMessage =
        "Duplicate keys found.\n"
        + "If multiple keyed widgets exist as children of another widget, they must have unique keys.\n"
        + "Stack(alignment: AlignmentDirectional.topStart, textDirection: ltr, fit: loose) has multiple "
        + "children with key [GlobalKey#00000 problematic].";

    private const string MultipleWidgetsContainerMessage =
        "Multiple widgets used the same GlobalKey.\n"
        + "The key [GlobalKey#00000 problematic] was used by multiple widgets. The parents of those "
        + "widgets were:\n"
        + "- Container-[<1>]\n"
        + "- Container-[<2>]\n"
        + "A GlobalKey can only be specified on one widget at a time in the widget tree.";

    // ------------------------------------------------------------------------------------ keys

    // Flutter: framework_test.dart: "UniqueKey control test"
    [Fact]
    public void UniqueKeyControlTest()
    {
        Key key = new UniqueKey();
        AssertHasOneLineDescription(key);
        Assert.NotEqual(key, new UniqueKey());
    }

    // Flutter: framework_test.dart: "ObjectKey control test"
    [Fact]
    public void ObjectKeyControlTest()
    {
        object a = new();
        object b = new();
        Key keyA = new ObjectKey(a);
        Key keyA2 = new ObjectKey(a);
        Key keyB = new ObjectKey(b);

        AssertHasOneLineDescription(keyA);
        Assert.Equal(keyA2, keyA);
        Assert.Equal(keyA2.GetHashCode(), keyA.GetHashCode());
        Assert.NotEqual(keyB, keyA);
    }

    // Flutter: framework_test.dart: "GlobalObjectKey toString test"
    [DebugOnlyFact]
    public void GlobalObjectKeyToStringTest()
    {
        object v1 = 1;
        object v2 = 2;
        object v3 = 3;
        object v4 = 4;
        GlobalKey one = new GlobalObjectKey<State>(v1);
        GlobalKey two = new GlobalObjectKey<TestState>(v2);
        GlobalKey three = new MyGlobalObjectKey<State>(v3);
        GlobalKey four = new MyGlobalObjectKey<TestState>(v4);

        Assert.Equal($"[GlobalObjectKey {Diagnostics.DescribeIdentity(v1)}]", one.ToString());
        Assert.Equal($"[GlobalObjectKey<TestState> {Diagnostics.DescribeIdentity(v2)}]", two.ToString());
        Assert.Equal($"[MyGlobalObjectKey {Diagnostics.DescribeIdentity(v3)}]", three.ToString());
        Assert.Equal($"[MyGlobalObjectKey<TestState> {Diagnostics.DescribeIdentity(v4)}]", four.ToString());
    }

    // Flutter: framework_test.dart: "GlobalObjectKey control test"
    [Fact]
    public void GlobalObjectKeyControlTest()
    {
        object a = new();
        object b = new();
        Key keyA = new GlobalObjectKey<State>(a);
        Key keyA2 = new GlobalObjectKey<State>(a);
        Key keyB = new GlobalObjectKey<State>(b);

        AssertHasOneLineDescription(keyA);
        Assert.Equal(keyA2, keyA);
        Assert.Equal(keyA2.GetHashCode(), keyA.GetHashCode());
        Assert.NotEqual(keyB, keyA);
    }

    // ------------------------------------------------------------------ GlobalKey correct cases

    // Flutter: framework_test.dart:
    // "GlobalKey correct case 1 - can move global key from container widget to layoutbuilder"
    [Fact]
    public void GlobalKeyCorrectCase1CanMoveGlobalKeyFromContainerWidgetToLayoutbuilder()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("correct");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new SizedBox(key: key)),
                new LayoutBuilder(key: new ValueKey<int>(2), builder: (_, _) => new Placeholder()),
            ]));

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Placeholder()),
                new LayoutBuilder(key: new ValueKey<int>(2), builder: (_, _) => new SizedBox(key: key)),
            ]));
    }

    // Flutter: framework_test.dart:
    // "GlobalKey correct case 2 - can move global key from layoutbuilder to container widget"
    [Fact]
    public void GlobalKeyCorrectCase2CanMoveGlobalKeyFromLayoutbuilderToContainerWidget()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("correct");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Placeholder()),
                new LayoutBuilder(key: new ValueKey<int>(2), builder: (_, _) => new SizedBox(key: key)),
            ]));

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new SizedBox(key: key)),
                new LayoutBuilder(key: new ValueKey<int>(2), builder: (_, _) => new Placeholder()),
            ]));
    }

    // Flutter: framework_test.dart:
    // "GlobalKey correct case 3 - can deal with early rebuild in layoutbuilder - move backward"
    [Fact]
    public void GlobalKeyCorrectCase3CanDealWithEarlyRebuildInLayoutbuilderMoveBackward()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new LabeledGlobalKey<State>("Text1");
        Key key2 = new LabeledGlobalKey<State>("Text2");
        Key? rebuiltKeyOfSecondChildBeforeLayout = null;
        Key? rebuiltKeyOfFirstChildAfterLayout = null;
        Key? rebuiltKeyOfSecondChildAfterLayout = null;
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr, key: key1)),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    rebuiltKeyOfSecondChildBeforeLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        // Result will be written during first build and need to clear it to remove noise.
        rebuiltKeyOfSecondChildBeforeLayout = null;

        StatefulStateAt(tester, 1).Rebuild();
        // Reorders the items
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfFirstChildAfterLayout);
                    rebuiltKeyOfFirstChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
            new Stateful(
                new Text("Text1", textDirection: TextDirection.Ltr, key: key1),
                element =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildAfterLayout);
                    rebuiltKeyOfSecondChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
        Assert.Equal(key2, rebuiltKeyOfFirstChildAfterLayout);
        Assert.Equal(key1, rebuiltKeyOfSecondChildAfterLayout);
    }

    // Flutter: framework_test.dart:
    // "GlobalKey correct case 4 - can deal with early rebuild in layoutbuilder - move forward"
    [Fact]
    public void GlobalKeyCorrectCase4CanDealWithEarlyRebuildInLayoutbuilderMoveForward()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new GlobalObjectKey<State>("Text1");
        Key key2 = new GlobalObjectKey<State>("Text2");
        Key key3 = new GlobalObjectKey<State>("Text3");
        Key? rebuiltKeyOfSecondChildBeforeLayout = null;
        Key? rebuiltKeyOfSecondChildAfterLayout = null;
        Key? rebuiltKeyOfThirdChildAfterLayout = null;
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr, key: key1)),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    rebuiltKeyOfSecondChildBeforeLayout = ((Stateful)element.Widget).Child.Key;
                }),
            new Stateful(new Text("Text3", textDirection: TextDirection.Ltr, key: key3)),
        ])));
        // Result will be written during first build and need to clear it to remove noise.
        rebuiltKeyOfSecondChildBeforeLayout = null;

        StatefulStateAt(tester, 1).Rebuild();
        // Reorders the items
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr, key: key1)),
            new Stateful(
                new Text("Text3", textDirection: TextDirection.Ltr, key: key3),
                element =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildAfterLayout);
                    rebuiltKeyOfSecondChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfThirdChildAfterLayout);
                    rebuiltKeyOfThirdChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
        Assert.Equal(key3, rebuiltKeyOfSecondChildAfterLayout);
        Assert.Equal(key2, rebuiltKeyOfThirdChildAfterLayout);
    }

    // Flutter: framework_test.dart:
    // "GlobalKey correct case 5 - can deal with early rebuild in layoutbuilder - only one global key"
    [Fact]
    public void GlobalKeyCorrectCase5CanDealWithEarlyRebuildInLayoutbuilderOnlyOneGlobalKey()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new GlobalObjectKey<State>("Text1");
        Key? rebuiltKeyOfSecondChildBeforeLayout = null;
        Key? rebuiltKeyOfThirdChildAfterLayout = null;
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr)),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key1),
                element =>
                {
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    rebuiltKeyOfSecondChildBeforeLayout = ((Stateful)element.Widget).Child.Key;
                }),
            new Stateful(new Text("Text3", textDirection: TextDirection.Ltr)),
        ])));
        // Result will be written during first build and need to clear it to remove noise.
        rebuiltKeyOfSecondChildBeforeLayout = null;

        StatefulStateAt(tester, 1).Rebuild();
        // Reorders the items
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr)),
            new Stateful(
                new Text("Text3", textDirection: TextDirection.Ltr),
                _ =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                }),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key1),
                element =>
                {
                    // The widget is only built once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfThirdChildAfterLayout);
                    rebuiltKeyOfThirdChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
        Assert.Equal(key1, rebuiltKeyOfThirdChildAfterLayout);
    }

    // ------------------------------------------------------------------ GlobalKey duplication

    // Flutter: framework_test.dart: "GlobalKey duplication 1 - double appearance"
    [DebugOnlyFact]
    public void GlobalKeyDuplication1DoubleAppearance()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new SizedBox(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Placeholder(key: key)),
            ]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(MultipleWidgetsContainerMessage, FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 2 - splitting and changing type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication2SplittingAndChangingType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1)),
                new Container(key: new ValueKey<int>(2)),
                new Container(key: key),
            ]));

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new SizedBox(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Placeholder(key: key)),
            ]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(MultipleWidgetsContainerMessage, FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 3 - splitting and changing type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication3SplittingAndChangingType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr, children: [new Container(key: key)]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children: [new SizedBox(key: key), new Placeholder(key: key)]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(DuplicateKeysStackMessage, FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 4 - splitting and half changing type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication4SplittingAndHalfChangingType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr, children: [new Container(key: key)]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children: [new Container(key: key), new Placeholder(key: key)]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(DuplicateKeysStackMessage, FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 5 - splitting and half changing type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication5SplittingAndHalfChangingType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr, children: [new Container(key: key)]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children: [new Placeholder(key: key), new Container(key: key)]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 6 - splitting and not changing type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication6SplittingAndNotChangingType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr, children: [new Container(key: key)]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children: [new Container(key: key), new Container(key: key)]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 7 - appearing later"
    [DebugOnlyFact]
    public void GlobalKeyDuplication7AppearingLater()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(2)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Container(key: key)),
            ]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 8 - appearing earlier"
    [DebugOnlyFact]
    public void GlobalKeyDuplication8AppearingEarlier()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1)),
                new Container(key: new ValueKey<int>(2), child: new Container(key: key)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Container(key: key)),
            ]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 9 - moving and appearing later"
    [DebugOnlyFact]
    public void GlobalKeyDuplication9MovingAndAppearingLater()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(1)),
                new Container(key: new ValueKey<int>(2)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Container(key: key)),
            ]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 10 - moving and appearing earlier"
    [DebugOnlyFact]
    public void GlobalKeyDuplication10MovingAndAppearingEarlier()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1)),
                new Container(key: new ValueKey<int>(2)),
                new Container(key: new ValueKey<int>(3), child: new Container(key: key)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(2), child: new Container(key: key)),
                new Container(key: new ValueKey<int>(3)),
            ]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 11 - double sibling appearance"
    [DebugOnlyFact]
    public void GlobalKeyDuplication11DoubleSiblingAppearance()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: key),
                new Container(key: key),
            ]));
        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 12 - all kinds of badness at once"
    [DebugOnlyFact]
    public void GlobalKeyDuplication12AllKindsOfBadnessAtOnce()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new LabeledGlobalKey<State>("problematic");
        Key key2 = new LabeledGlobalKey<State>("problematic"); // intentionally the same label
        Key key3 = new LabeledGlobalKey<State>("also problematic");
        tester.PumpWidget(BadnessAtOnce(key1, key2, key3));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(DuplicateKeysStackMessage, FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 13 - all kinds of badness at once"
    [DebugOnlyFact]
    public void GlobalKeyDuplication13AllKindsOfBadnessAtOnce()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new LabeledGlobalKey<State>("problematic");
        Key key2 = new LabeledGlobalKey<State>("problematic"); // intentionally the same label
        Key key3 = new LabeledGlobalKey<State>("also problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children: [new Container(key: key1), new Container(key: key2), new Container(key: key3)]));
        tester.PumpWidget(BadnessAtOnce(key1, key2, key3));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 14 - moving during build - before"
    [Fact]
    public void GlobalKeyDuplication14MovingDuringBuildBefore()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: key),
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
            ]));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 15 - duplicating during build - before"
    [DebugOnlyFact]
    public void GlobalKeyDuplication15DuplicatingDuringBuildBefore()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: key),
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: key),
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
            ]));

        Assert.IsType<FlutterError>(tester.TakeException());
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 16 - moving during build - after"
    [Fact]
    public void GlobalKeyDuplication16MovingDuringBuildAfter()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
                new Container(key: key),
            ]));
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
            ]));
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 17 - duplicating during build - after"
    [DebugOnlyFact]
    public void GlobalKeyDuplication17DuplicatingDuringBuildAfter()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
                new Container(key: key),
            ]));

        int count = 0;
        var exceptions = new List<object>();
        FlutterExceptionHandler? oldHandler = FlutterError.OnError;
        FlutterError.OnError = details =>
        {
            exceptions.Add(details.Exception);
            count += 1;
        };
        try
        {
            tester.PumpWidget(new Stack(
                textDirection: TextDirection.Ltr,
                children:
                [
                    new Container(key: new ValueKey<int>(0)),
                    new Container(key: new ValueKey<int>(1), child: new Container(key: key)),
                    new Container(key: key),
                ]));
        }
        finally
        {
            FlutterError.OnError = oldHandler;
        }

        Assert.All(exceptions, exception => Assert.IsType<FlutterError>(exception));
        Assert.Equal(1, count);
    }

    // Flutter: framework_test.dart: "GlobalKey duplication 18 - subtree build duplicate key with same type"
    [DebugOnlyFact]
    public void GlobalKeyDuplication18SubtreeBuildDuplicateKeyWithSameType()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        var stack = new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SwapKeyWidget(childKey: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
                new Container(key: key),
            ]);
        tester.PumpWidget(stack);
        SwapKeyWidgetState state = tester.State<SwapKeyWidgetState>();
        state.SwapKey(key);
        tester.Pump();

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(
            "Duplicate GlobalKey detected in widget tree.\n"
            + "The following GlobalKey was specified multiple times in the widget tree. This will lead "
            + "to parts of the widget tree being truncated unexpectedly, because the second time a key is "
            + "seen, the previous instance is moved to the new location. The key was:\n"
            + "- [GlobalKey#00000 problematic]\n"
            + "This was determined by noticing that after the widget with the above global key was "
            + "moved out of its previous parent, that previous parent never updated during this frame, "
            + "meaning that it either did not update at all or updated before the widget was moved, in "
            + "either case implying that it still thinks that it should have a child with that global key.\n"
            + "The specific parent that did not update after having one or more children forcibly "
            + "removed due to GlobalKey reparenting is:\n"
            + "- Stack(alignment: AlignmentDirectional.topStart, textDirection: ltr, fit: loose, "
            + "renderObject: RenderStack#00000)\n"
            + "A GlobalKey can only be specified on one widget at a time in the widget tree.",
            FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart:
    // "GlobalKey duplication 19 - subtree build duplicate key with different types"
    [DebugOnlyFact]
    public void GlobalKeyDuplication19SubtreeBuildDuplicateKeyWithDifferentTypes()
    {
        using var tester = new FrameworkDartTester();
        Key key = new LabeledGlobalKey<State>("problematic");
        var stack = new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new SwapKeyWidget(childKey: new ValueKey<int>(0)),
                new Container(key: new ValueKey<int>(1)),
                new ColoredBox(Green, child: new SizedBox(key: key)),
            ]);
        tester.PumpWidget(stack);
        SwapKeyWidgetState state = tester.State<SwapKeyWidgetState>();
        state.SwapKey(key);
        tester.Pump();

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(
            "Multiple widgets used the same GlobalKey.\n"
            + "The key [GlobalKey#00000 problematic] was used by 2 widgets:\n"
            + "  SizedBox-[GlobalKey#00000 problematic]\n"
            + "  Container-[GlobalKey#00000 problematic]\n"
            + "A GlobalKey can only be specified on one widget at a time in the widget tree.",
            FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart:
    // "GlobalKey duplication 20 - real duplication with early rebuild in layoutbuilder will throw"
    [DebugOnlyFact]
    public void GlobalKeyDuplication20RealDuplicationWithEarlyRebuildInLayoutbuilderWillThrow()
    {
        using var tester = new FrameworkDartTester();
        Key key1 = new GlobalObjectKey<State>("Text1");
        Key key2 = new GlobalObjectKey<State>("Text2");
        Key? rebuiltKeyOfSecondChildBeforeLayout = null;
        Key? rebuiltKeyOfFirstChildAfterLayout = null;
        Key? rebuiltKeyOfSecondChildAfterLayout = null;
        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(new Text("Text1", textDirection: TextDirection.Ltr, key: key1)),
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    rebuiltKeyOfSecondChildBeforeLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        // Result will be written during first build and need to clear it to remove noise.
        rebuiltKeyOfSecondChildBeforeLayout = null;

        StatefulStateAt(tester, 1).Rebuild();

        tester.PumpWidget(new LayoutBuilder((_, _) => new Column(
        [
            new Stateful(
                new Text("Text2", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // The widget is only rebuilt once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfFirstChildAfterLayout);
                    rebuiltKeyOfFirstChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
            new Stateful(
                new Text("Text1", textDirection: TextDirection.Ltr, key: key2),
                element =>
                {
                    // The widget is only rebuilt once.
                    Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
                    // We don't want noise to override the result;
                    Assert.Null(rebuiltKeyOfSecondChildAfterLayout);
                    rebuiltKeyOfSecondChildAfterLayout = ((Stateful)element.Widget).Child.Key;
                }),
        ])));
        Assert.Null(rebuiltKeyOfSecondChildBeforeLayout);
        Assert.Equal(key2, rebuiltKeyOfFirstChildAfterLayout);
        Assert.Equal(key2, rebuiltKeyOfSecondChildAfterLayout);

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.Equal(
            "Multiple widgets used the same GlobalKey.\n"
            + "The key [GlobalObjectKey String#00000] was used by multiple widgets. The "
            + "parents of those widgets were:\n"
            + "- Stateful(state: StatefulState#00000)\n"
            + "- Stateful(state: StatefulState#00000)\n"
            + "A GlobalKey can only be specified on one widget at a time in the widget tree.",
            FrameworkDartTester.IgnoringHashCodes(exception.ToString()!));
    }

    // Flutter: framework_test.dart: "GlobalKey - detach and re-attach child to different parents"
    [Fact]
    public void GlobalKeyDetachAndReAttachChildToDifferentParents()
    {
        using var tester = new FrameworkDartTester();
        var scrollController = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(
                child: new SizedBox(
                    height: 100,
                    child: new CustomScrollView(
                        controller: scrollController,
                        slivers:
                        [
                            SliverList.FromChildren(
                            [
                                new Text("child", key: new LabeledGlobalKey<State>(null)),
                            ]),
                        ])))));
        var element = (SliverMultiBoxAdaptorElement)tester.ElementOfType<SliverList>();
        Element? childElement = null;
        // Removing and recreating child with same Global Key should not trigger
        // duplicate key error.
        element.VisitChildren(e => childElement = e);
        element.RemoveChild((RenderBox)childElement!.RenderObject!);
        element.CreateChild(0, after: null);
        element.VisitChildren(e => childElement = e);
        element.RemoveChild((RenderBox)childElement!.RenderObject!);
        element.CreateChild(0, after: null);
    }

    // Flutter: framework_test.dart:
    // "GlobalKey - re-attach child to new parents, and the old parent is deactivated(unmounted)"
    [Fact]
    public void GlobalKeyReAttachChildToNewParentsAndTheOldParentIsDeactivatedUnmounted()
    {
        // This is a regression test for https://github.com/flutter/flutter/issues/62055
        using var tester = new FrameworkDartTester();
        Key key1 = new GlobalObjectKey<State>("key1");
        Key key2 = new GlobalObjectKey<State>("key2");
        StateSetter? setState = null;
        int pageCount = 2;
        var pageController = new PageController();

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new StatefulBuilder((_, setter) =>
            {
                setState = setter;
                var children = new List<Widget>();
                if (pageCount > 0)
                {
                    children.Add(new Text("key1", key: key1));
                }

                if (pageCount > 1)
                {
                    children.Add(new Text("key2", key: key2));
                }

                return new PageView(controller: pageController, children: children);
            })));

        Assert.Equal(0.0, pageController.Page);

        // switch pages 0 -> 1
        _ = pageController.AnimateToPage(1, TimeSpan.FromMilliseconds(300), Curves.Ease);
        tester.PumpAndSettle(); // finish the animation

        Assert.Equal(1.0, pageController.Page);

        // rebuild PageView that only have the 1st page with GlobalKey 'key1'
        setState!(() =>
        {
            pageCount = 1;
            pageController = new PageController();
        });

        tester.Pump(TimeSpan.FromSeconds(1)); // finish the animation

        Assert.Equal(0.0, pageController.Page);
    }

    // Flutter: framework_test.dart: "Defunct setState throws exception"
    [DebugOnlyFact]
    public void DefunctSetStateThrowsException()
    {
        using var tester = new FrameworkDartTester();
        StateSetter? setState = null;

        tester.PumpWidget(new StatefulBuilder((_, setter) =>
        {
            setState = setter;
            return new Container();
        }));

        // Control check that setState doesn't throw an exception.
        setState!(() => { });

        tester.PumpWidget(new Container());

        Assert.ThrowsAny<FlutterError>(() => setState(() => { }));
    }

    // Flutter: framework_test.dart: "setState() after dispose() error explains asynchronous causes"
    [DebugOnlyFact]
    public void SetStateAfterDisposeErrorExplainsAsynchronousCauses()
    {
        using var tester = new FrameworkDartTester();
        StateSetter? setState = null;

        tester.PumpWidget(new StatefulBuilder((_, setter) =>
        {
            setState = setter;
            return new Container();
        }));

        // Remove the widget from the tree so its State becomes defunct.
        tester.PumpWidget(new Container());

        // The error should call out asynchronous gaps (the most common cause) and
        // point at the "mounted" check after an "await".
        FlutterError error = Assert.ThrowsAny<FlutterError>(() => setState!(() => { }));
        Assert.Contains("asynchronous", error.ToString());
        Assert.Contains("await", error.ToString());
    }

    // Flutter: framework_test.dart: "State toString"
    [DebugOnlyFact]
    public void StateToString()
    {
        var state = new TestState();
        Assert.Contains("no widget", state.ToString());
    }

    // ------------------------------------------------------------------ element API and diagnostics

    // Flutter: framework_test.dart: "debugPrintGlobalKeyedWidgetLifecycle control test"
    [DebugOnlyFact]
    public void DebugPrintGlobalKeyedWidgetLifecycleControlTest()
    {
        using var tester = new FrameworkDartTester();
        Assert.False(WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle);

        DebugPrintCallback oldCallback = Print.DebugPrint;
        WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle = true;

        var log = new List<string>();
        Print.DebugPrint = (message, _) => log.Add(message!);
        try
        {
            GlobalKey key = new LabeledGlobalKey<State>(null);
            tester.PumpWidget(new Container(key: key));
            Assert.Empty(log);
            tester.PumpWidget(new Placeholder());
        }
        finally
        {
            Print.DebugPrint = oldCallback;
            WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle = false;
        }

        Assert.Equal(2, log.Count);
        Assert.Matches("Deactivated", log[0]);
        Assert.Matches("Discarding .+ from inactive elements list.", log[1]);
    }

    // Flutter: framework_test.dart: "MultiChildRenderObjectElement.children"
    [Fact]
    public void MultiChildRenderObjectElementChildren()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key0 = new LabeledGlobalKey<State>(null);
        GlobalKey key1 = new LabeledGlobalKey<State>(null);
        GlobalKey key2 = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Column(
            key: key0,
            children:
            [
                new Container(),
                new Container(key: key1),
                new Container(),
                new Container(key: key2),
                new Container(),
            ]));

        var element = (MultiChildRenderObjectElement)key0.CurrentContext!;
        Assert.Equal([null, key1, null, key2, null], element.Children.Select(child => child.Widget.Key));
    }

    // Flutter: framework_test.dart:
    // "Can not attach a non-RenderObjectElement to the MultiChildRenderObjectElement - mount"
    [DebugOnlyFact]
    public void CanNotAttachANonRenderObjectElementToTheMultiChildRenderObjectElementMount()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Column([new Container(), new EmptyWidget()]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(NonRenderObjectChildMessage, exception.ToString()!);
    }

    // Flutter: framework_test.dart:
    // "Can not attach a non-RenderObjectElement to the MultiChildRenderObjectElement - update"
    [DebugOnlyFact]
    public void CanNotAttachANonRenderObjectElementToTheMultiChildRenderObjectElementUpdate()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Column([new Container()]));
        tester.PumpWidget(new Column([new Container(), new EmptyWidget()]));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(NonRenderObjectChildMessage, exception.ToString()!);
    }

    // Flutter: framework_test.dart: "Element diagnostics"
    [Fact]
    public void ElementDiagnostics()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key0 = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Column(
            key: key0,
            children:
            [
                new Container(),
                new Container(key: new LabeledGlobalKey<State>(null)),
                new ColoredBox(Green, child: new Container()),
                new Container(key: new LabeledGlobalKey<State>(null)),
                new Container(),
            ]));
        var element = (MultiChildRenderObjectElement)key0.CurrentContext!;

        AssertHasAGoodToStringDeep(element);
        Assert.Equal(
            "Column-[GlobalKey#00000](direction: vertical, mainAxisAlignment: start, crossAxisAlignment: "
            + "center, renderObject: RenderFlex#00000)\n"
            + "├Container\n"
            + "│└LimitedBox(maxWidth: 0.0, maxHeight: 0.0, renderObject: RenderLimitedBox#00000 relayoutBoundary=up1)\n"
            + "│ └ConstrainedBox(BoxConstraints(biggest), renderObject: RenderConstrainedBox#00000 "
            + "relayoutBoundary=up2)\n"
            + "├Container-[GlobalKey#00000]\n"
            + "│└LimitedBox(maxWidth: 0.0, maxHeight: 0.0, renderObject: RenderLimitedBox#00000 relayoutBoundary=up1)\n"
            + "│ └ConstrainedBox(BoxConstraints(biggest), renderObject: RenderConstrainedBox#00000 "
            + "relayoutBoundary=up2)\n"
            // dart:ui's `Color.toString` for `Color(0xff00ff00)`.
            + "├ColoredBox(color: Color(alpha: 1.0000, red: 0.0000, green: 1.0000, blue: 0.0000, "
            + "colorSpace: ColorSpace.sRGB), renderObject: RenderColoredBox#00000 relayoutBoundary=up1)\n"
            + "│└Container\n"
            + "│ └LimitedBox(maxWidth: 0.0, maxHeight: 0.0, renderObject: RenderLimitedBox#00000 "
            + "relayoutBoundary=up2)\n"
            + "│  └ConstrainedBox(BoxConstraints(biggest), renderObject: RenderConstrainedBox#00000 "
            + "relayoutBoundary=up3)\n"
            + "├Container-[GlobalKey#00000]\n"
            + "│└LimitedBox(maxWidth: 0.0, maxHeight: 0.0, renderObject: RenderLimitedBox#00000 relayoutBoundary=up1)\n"
            + "│ └ConstrainedBox(BoxConstraints(biggest), renderObject: RenderConstrainedBox#00000 "
            + "relayoutBoundary=up2)\n"
            + "└Container\n"
            + " └LimitedBox(maxWidth: 0.0, maxHeight: 0.0, renderObject: RenderLimitedBox#00000 relayoutBoundary=up1)\n"
            + "  └ConstrainedBox(BoxConstraints(biggest), renderObject: RenderConstrainedBox#00000 "
            + "relayoutBoundary=up2)\n",
            FrameworkDartTester.IgnoringHashCodes(element.ToStringDeep(wrapWidth: 200)));
    }

    // Flutter: framework_test.dart: "scheduleBuild while debugBuildingDirtyElements is true"
    [DebugOnlyFact]
    public void ScheduleBuildWhileDebugBuildingDirtyElementsIsTrue()
    {
        // `tester.binding` is flutter_test's WidgetsBinding; Plumix's harness owns no binding, so the
        // test builds its own (a fresh owner, whose onBuildScheduled has not fired yet).
        WithLocalWidgetsBinding(binding =>
        {
            binding.DebugBuildingDirtyElements = true;
            FlutterError? error = null;
            try
            {
                binding.BuildOwner.ScheduleBuildFor(
                    new DirtyElementWithCustomBuildOwner(binding.BuildOwner, new Container()));
            }
            catch (FlutterError e)
            {
                error = e;
            }
            finally
            {
                Assert.NotNull(error);
                Assert.Equal(3, error.Diagnostics.Count);
                Assert.Equal(DiagnosticLevel.Hint, error.Diagnostics[^1].Level);
                Assert.Equal(
                    "This might be because setState() was called from a layout or\n"
                    + "paint callback. If a change is needed to the widget tree, it\n"
                    + "should be applied as the tree is being built. Scheduling a change\n"
                    + "for the subsequent frame instead results in an interface that\n"
                    + "lags behind by one frame. If this was done to make your build\n"
                    + "dependent on a size measured at layout time, consider using a\n"
                    + "LayoutBuilder, CustomSingleChildLayout, or\n"
                    + "CustomMultiChildLayout. If, on the other hand, the one frame\n"
                    + "delay is the desired effect, for example because this is an\n"
                    + "animation, consider scheduling the frame in a post-frame callback\n"
                    + "using SchedulerBinding.addPostFrameCallback or using an\n"
                    + "AnimationController to trigger the animation.\n",
                    FrameworkDartTester.IgnoringHashCodes(error.Diagnostics[^1].ToStringDeep()));
                Assert.Equal(
                    "FlutterError\n"
                    + "   Build scheduled during frame.\n"
                    + "   While the widget tree was being built, laid out, and painted, a\n"
                    + "   new frame was scheduled to rebuild the widget tree.\n"
                    + "   This might be because setState() was called from a layout or\n"
                    + "   paint callback. If a change is needed to the widget tree, it\n"
                    + "   should be applied as the tree is being built. Scheduling a change\n"
                    + "   for the subsequent frame instead results in an interface that\n"
                    + "   lags behind by one frame. If this was done to make your build\n"
                    + "   dependent on a size measured at layout time, consider using a\n"
                    + "   LayoutBuilder, CustomSingleChildLayout, or\n"
                    + "   CustomMultiChildLayout. If, on the other hand, the one frame\n"
                    + "   delay is the desired effect, for example because this is an\n"
                    + "   animation, consider scheduling the frame in a post-frame callback\n"
                    + "   using SchedulerBinding.addPostFrameCallback or using an\n"
                    + "   AnimationController to trigger the animation.\n",
                    error.ToStringDeep());
            }
        });
    }

    // Flutter: framework_test.dart: "didUpdateDependencies is not called on a State that never rebuilds"
    [Fact]
    public void DidUpdateDependenciesIsNotCalledOnAStateThatNeverRebuilds()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<DependentState>(null);

        // Initial build - should call didChangeDependencies, not deactivate
        tester.PumpWidget(new Inherited(1, new DependentStatefulWidget(key: key)));
        DependentState state = key.CurrentState!;
        Assert.NotNull(key.CurrentState);
        Assert.Equal(1, state.DidChangeDependenciesCount);
        Assert.Equal(0, state.DeactivatedCount);

        // Rebuild with updated value - should call didChangeDependencies
        tester.PumpWidget(new Inherited(2, new DependentStatefulWidget(key: key)));
        Assert.NotNull(key.CurrentState);
        Assert.Equal(2, state.DidChangeDependenciesCount);
        Assert.Equal(0, state.DeactivatedCount);

        // reparent it - should call deactivate and didChangeDependencies
        tester.PumpWidget(new Inherited(3, new SizedBox(child: new DependentStatefulWidget(key: key))));
        Assert.NotNull(key.CurrentState);
        Assert.Equal(3, state.DidChangeDependenciesCount);
        Assert.Equal(1, state.DeactivatedCount);

        // Remove it - should call deactivate, but not didChangeDependencies
        tester.PumpWidget(new Inherited(4, new SizedBox()));
        Assert.Null(key.CurrentState);
        Assert.Equal(3, state.DidChangeDependenciesCount);
        Assert.Equal(2, state.DeactivatedCount);
    }

    // Flutter: framework_test.dart: "StatefulElement subclass can decorate State.build"
    [Fact]
    public void StatefulElementSubclassCanDecorateStateBuild()
    {
        using var tester = new FrameworkDartTester();
        bool? isDidChangeDependenciesDecorated = null;
        bool? isBuildDecorated = null;

        Widget child = new Decorate(
            didChangeDependencies: value => isDidChangeDependenciesDecorated = value,
            build: value => isBuildDecorated = value);

        tester.PumpWidget(new Inherited(0, child));

        Assert.True(isBuildDecorated);
        Assert.False(isDidChangeDependenciesDecorated);

        tester.PumpWidget(new Inherited(1, child));

        Assert.True(isBuildDecorated);
        Assert.False(isDidChangeDependenciesDecorated);
    }

    // Flutter: framework_test.dart: group "BuildContext.debugDoingbuild": "StatelessWidget"
    [DebugOnlyFact]
    public void BuildContextDebugDoingbuildStatelessWidget()
    {
        using var tester = new FrameworkDartTester();
        bool? debugDoingBuildOnBuild = null;
        tester.PumpWidget(new StatelessWidgetSpy(onBuild: context =>
        {
            debugDoingBuildOnBuild = context.DebugDoingBuild;
        }));

        Element context = tester.ElementOfType<StatelessWidgetSpy>();

        Assert.False(context.DebugDoingBuild);
        Assert.True(debugDoingBuildOnBuild);
    }

    // Flutter: framework_test.dart: group "BuildContext.debugDoingbuild": "StatefulWidget"
    [DebugOnlyFact]
    public void BuildContextDebugDoingbuildStatefulWidget()
    {
        using var tester = new FrameworkDartTester();
        bool? debugDoingBuildOnBuild = null;
        bool? debugDoingBuildOnInitState = null;
        bool? debugDoingBuildOnDidChangeDependencies = null;
        bool? debugDoingBuildOnDidUpdateWidget = null;
        bool? debugDoingBuildOnDispose = null;
        bool? debugDoingBuildOnDeactivate = null;

        tester.PumpWidget(new Inherited(
            0,
            new StatefulWidgetSpy(
                onInitState: context => debugDoingBuildOnInitState = context.DebugDoingBuild,
                onDidChangeDependencies: context =>
                {
                    context.DependOnInheritedWidgetOfExactType<Inherited>();
                    debugDoingBuildOnDidChangeDependencies = context.DebugDoingBuild;
                },
                onBuild: context => debugDoingBuildOnBuild = context.DebugDoingBuild)));

        Element context = tester.ElementOfType<StatefulWidgetSpy>();

        Assert.False(context.DebugDoingBuild);
        Assert.True(debugDoingBuildOnBuild);
        Assert.False(debugDoingBuildOnInitState);
        Assert.False(debugDoingBuildOnDidChangeDependencies);

        tester.PumpWidget(new Inherited(
            1,
            new StatefulWidgetSpy(
                onDidUpdateWidget: c => debugDoingBuildOnDidUpdateWidget = c.DebugDoingBuild,
                onDidChangeDependencies: c => debugDoingBuildOnDidChangeDependencies = c.DebugDoingBuild,
                onBuild: c => debugDoingBuildOnBuild = c.DebugDoingBuild,
                onDispose: c => debugDoingBuildOnDispose = c.DebugDoingBuild,
                onDeactivate: c => debugDoingBuildOnDeactivate = c.DebugDoingBuild)));

        Assert.False(context.DebugDoingBuild);
        Assert.True(debugDoingBuildOnBuild);
        Assert.False(debugDoingBuildOnDidUpdateWidget);
        Assert.False(debugDoingBuildOnDidChangeDependencies);
        Assert.Null(debugDoingBuildOnDeactivate);
        Assert.Null(debugDoingBuildOnDispose);

        tester.PumpWidget(new Container());

        Assert.False(context.DebugDoingBuild);
        Assert.False(debugDoingBuildOnDispose);
        Assert.False(debugDoingBuildOnDeactivate);
    }

    // Flutter: framework_test.dart: group "BuildContext.debugDoingbuild": "RenderObjectWidget"
    [DebugOnlyFact]
    public void BuildContextDebugDoingbuildRenderObjectWidget()
    {
        using var tester = new FrameworkDartTester();
        bool? debugDoingBuildOnCreateRenderObject = null;
        bool? debugDoingBuildOnUpdateRenderObject = null;
        bool? debugDoingBuildOnDidUnmountRenderObject = null;
        var notifier = new ValueNotifier<int>(0);

        BuildContext? spyContext = null;

        Widget Build()
        {
            return new ValueListenableBuilder<int>(
                notifier,
                (_, value, child) => new Inherited(value, child!),
                child: new RenderObjectWidgetSpy(
                    onCreateRenderObject: context =>
                    {
                        spyContext = context;
                        context.DependOnInheritedWidgetOfExactType<Inherited>();
                        debugDoingBuildOnCreateRenderObject = context.DebugDoingBuild;
                    },
                    onUpdateRenderObject: context => debugDoingBuildOnUpdateRenderObject = context.DebugDoingBuild,
                    onDidUnmountRenderObject: () =>
                        debugDoingBuildOnDidUnmountRenderObject = spyContext!.DebugDoingBuild));
        }

        tester.PumpWidget(Build());

        spyContext = tester.ElementOfType<RenderObjectWidgetSpy>();

        Assert.False(spyContext.DebugDoingBuild);
        Assert.True(debugDoingBuildOnCreateRenderObject);
        Assert.Null(debugDoingBuildOnUpdateRenderObject);
        Assert.Null(debugDoingBuildOnDidUnmountRenderObject);

        tester.PumpWidget(Build());

        Assert.False(spyContext.DebugDoingBuild);
        Assert.True(debugDoingBuildOnUpdateRenderObject);
        Assert.Null(debugDoingBuildOnDidUnmountRenderObject);

        notifier.Value++;
        debugDoingBuildOnUpdateRenderObject = false;
        tester.Pump();

        Assert.False(spyContext.DebugDoingBuild);
        Assert.True(debugDoingBuildOnUpdateRenderObject);
        Assert.Null(debugDoingBuildOnDidUnmountRenderObject);

        tester.PumpWidget(new Container());

        Assert.False(spyContext.DebugDoingBuild);
        Assert.False(debugDoingBuildOnDidUnmountRenderObject);
    }

    // Flutter: framework_test.dart:
    // "A widget whose element has an invalid visitChildren implementation triggers a useful error message"
    [DebugOnlyFact]
    public void AWidgetWhoseElementHasAnInvalidVisitChildrenImplementationTriggersAUsefulErrorMessage()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new WidgetWithNoVisitChildren(new StatefulLeaf(key: key)));
        ((StatefulLeafState)key.CurrentState!).MarkNeedsBuild();
        tester.PumpWidget(new Container());

        object? exception = tester.TakeException();
        Assert.Equal(
            "Tried to build dirty widget in the wrong build scope.\n"
            + "A widget which was marked as dirty and is still active was scheduled to be built, "
            + "but the current build scope unexpectedly does not contain that widget.\n"
            + "Sometimes this is detected when an element is removed from the widget tree, but "
            + "the element somehow did not get marked as inactive. In that case, it might be "
            + "caused by an ancestor element failing to implement visitChildren correctly, thus "
            + "preventing some or all of its descendants from being correctly deactivated.\n"
            + "The root of the build scope was:\n"
            + "  [root]\n"
            + "The offending element (which does not appear to be a descendant of the root of "
            + "the build scope) was:\n"
            + "  StatefulLeaf-[GlobalKey#00000]",
            FrameworkDartTester.IgnoringHashCodes(Assert.IsType<FlutterError>(exception).Message));
    }

    // Flutter: framework_test.dart:
    // "Can create BuildOwner that does not interfere with pointer router or raw key event handler"
    [Fact]
    public void CanCreateBuildOwnerThatDoesNotInterfereWithPointerRouterOrRawKeyEventHandler()
    {
        // flutter_test's binding has its BuildOwner's focus manager registered as the global handler;
        // a local binding sets up the same state. Plumix has no `RawKeyboard.keyEventHandler`: the key
        // handler that `FocusManager.registerGlobalHandlers` installs is the KeyEventManager's.
        WithLocalWidgetsBinding(_ =>
        {
            int pointerRouterCount = GestureBinding.Instance.PointerRouter.DebugGlobalRouteCount;
#pragma warning disable CS0618
            Func<KeyMessage, bool>? rawKeyEventHandler = KeyEventManager.Instance.KeyMessageHandler;
            Assert.NotNull(rawKeyEventHandler);
            var focusManager = new FocusManager();
            try
            {
                GC.KeepAlive(new BuildOwner(focusManager: focusManager));
                Assert.Equal(pointerRouterCount, GestureBinding.Instance.PointerRouter.DebugGlobalRouteCount);
                Assert.Same(rawKeyEventHandler, KeyEventManager.Instance.KeyMessageHandler);
            }
            finally
            {
                focusManager.Dispose();
            }
#pragma warning restore CS0618
        });
    }

    // Flutter: framework_test.dart: "Can access debugFillProperties without _LateInitializationError"
    [DebugOnlyFact]
    public void CanAccessDebugFillPropertiesWithoutLateInitializationError()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new TestRenderObjectElement().DebugFillProperties(builder);
        Assert.Contains(builder.Properties, property => property.Name == "renderObject" && property.Value == null);
    }

    // Flutter: framework_test.dart: "debugFillProperties sorts dependencies in alphabetical order"
    [DebugOnlyFact]
    public void DebugFillPropertiesSortsDependenciesInAlphabeticalOrder()
    {
        var builder = new DiagnosticPropertiesBuilder();
        var element = new TestRenderObjectElement();

        var focusTraversalOrder = new TestInheritedElement(
            new FocusTraversalOrder(new LexicalFocusOrder(string.Empty), new Placeholder()));
        var directionality = new TestInheritedElement(new Directionality(TextDirection.Ltr, new Placeholder()));
        var mediaQuery = new TestInheritedElement(new MediaQuery(new MediaQueryData(), new Placeholder()));

        // Dependencies are added out of alphabetical order.
        element.DependOnInheritedElement(focusTraversalOrder);
        element.DependOnInheritedElement(directionality);
        element.DependOnInheritedElement(mediaQuery);

        // Dependencies will be sorted by [debugFillProperties].
        element.DebugFillProperties(builder);

        Assert.Contains(builder.Properties, property => property.Name == "dependencies" && property.Value != null);
        var dependenciesProperty = (DiagnosticsProperty<HashSet<InheritedElement>>)builder.Properties
            .First(property => property.Name == "dependencies");
        Assert.NotNull(dependenciesProperty);

        HashSet<InheritedElement> dependencies = Assert.IsType<HashSet<InheritedElement>>(dependenciesProperty.Value);
        Assert.Equal(3, dependencies.Count);
        Assert.Equal("[Directionality, FocusTraversalOrder, MediaQuery]", dependenciesProperty.ToDescription());
    }

    // Flutter: framework_test.dart: "BuildOwner.globalKeyCount keeps track of in-use global keys"
    [Fact]
    public void BuildOwnerGlobalKeyCountKeepsTrackOfInUseGlobalKeys()
    {
        using var tester = new FrameworkDartTester();
        int initialCount = tester.Owner.GlobalKeyCount;
        GlobalKey key1 = new LabeledGlobalKey<State>(null);
        GlobalKey key2 = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: key1));
        Assert.Equal(initialCount + 1, tester.Owner.GlobalKeyCount);
        tester.PumpWidget(new Container(key: key1, child: new Container()));
        Assert.Equal(initialCount + 1, tester.Owner.GlobalKeyCount);
        tester.PumpWidget(new Container(key: key1, child: new Container(key: key2)));
        Assert.Equal(initialCount + 2, tester.Owner.GlobalKeyCount);
        tester.PumpWidget(new Container());
        Assert.Equal(initialCount + 0, tester.Owner.GlobalKeyCount);
    }

    // Flutter: framework_test.dart: "Widget and State properties are nulled out when unmounted"
    [Fact]
    public void WidgetAndStatePropertiesAreNulledOutWhenUnmounted()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new StatefulLeaf());
        var element = (StatefulElement)tester.ElementOfType<StatefulLeaf>();
        Assert.IsAssignableFrom<State<StatefulLeaf>>(element.State);
        Assert.IsType<StatefulLeaf>(element.Widget);
        // Replace the widget tree to unmount the element.
        tester.PumpWidget(new Container());
        // Accessing state/widget now throws because they have been nulled out to reduce severity of
        // memory leaks when an Element (e.g. in the form of a BuildContext) is retained past its useful
        // life. See also https://github.com/flutter/flutter/issues/79605.
        Assert.ThrowsAny<Exception>(() => element.State);
        Assert.ThrowsAny<Exception>(() => element.Widget);
    }

    // Flutter: framework_test.dart: "LayerLink can be swapped between parent and child container layers"
    [Fact]
    public void LayerLinkCanBeSwappedBetweenParentAndChildContainerLayers()
    {
        // Regression test for https://github.com/flutter/flutter/issues/96959.
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        tester.PumpWidget(new TestLeaderLayerWidget(
            link: link,
            child: new TestLeaderLayerWidget(child: new Placeholder())));
        Assert.Null(tester.TakeException());

        // Swaps the layer link.
        tester.PumpWidget(new TestLeaderLayerWidget(
            child: new TestLeaderLayerWidget(link: link, child: new Placeholder())));
        Assert.Null(tester.TakeException());
    }

    // Flutter: framework_test.dart: "Deactivate and activate are called correctly"
    [Fact]
    public void DeactivateAndActivateAreCalledCorrectly()
    {
        using var tester = new FrameworkDartTester();
        var states = new List<string>();
        Widget Build(Key? key = null)
        {
            return new StatefulWidgetSpy(
                key: key,
                onInitState: _ => states.Add("initState"),
                onDidUpdateWidget: _ => states.Add("didUpdateWidget"),
                onDeactivate: _ => states.Add("deactivate"),
                onActivate: _ => states.Add("activate"),
                onBuild: _ => states.Add("build"),
                onDispose: _ => states.Add("dispose"));
        }

        void Pump(Widget widget)
        {
            states.Clear();
            tester.PumpWidget(widget);
        }

        Pump(Build());
        Assert.Equal(["initState", "build"], states);
        Pump(new Container(child: Build()));
        Assert.Equal(["deactivate", "initState", "build", "dispose"], states);
        Pump(new Container());
        Assert.Equal(["deactivate", "dispose"], states);

        GlobalKey key = new LabeledGlobalKey<State>(null);
        Pump(Build(key));
        Assert.Equal(["initState", "build"], states);
        Pump(new Container(child: Build(key)));
        Assert.Equal(["deactivate", "activate", "didUpdateWidget", "build"], states);
        Pump(new Container());
        Assert.Equal(["deactivate", "dispose"], states);
    }

    // Flutter: framework_test.dart:
    // "Element.deactivate reports its deactivation to the InheritedElement it depends on"
    [Fact]
    public void ElementDeactivateReportsItsDeactivationToTheInheritedElementItDependsOn()
    {
        using var tester = new FrameworkDartTester();
        var removedDependentWidgetKeys = new List<Key>();

        InheritedElement ElementCreator(Inherited widget)
        {
            return new InheritedElementSpy(
                widget,
                onRemoveDependent: dependent => removedDependentWidgetKeys.Add(dependent.Widget.Key!));
        }

        Widget Builder(BuildContext context)
        {
            context.DependOnInheritedWidgetOfExactType<Inherited>();
            return new Container();
        }

        tester.PumpWidget(new Inherited(
            0,
            new Column([new Builder(Builder, key: Key.Create("dependent"))]),
            elementCreator: ElementCreator));

        Assert.Empty(removedDependentWidgetKeys);

        tester.PumpWidget(new Inherited(0, new Column([new Container()]), elementCreator: ElementCreator));

        Assert.Single(removedDependentWidgetKeys);
        Assert.Equal(Key.Create("dependent"), removedDependentWidgetKeys.First());
    }

    // Flutter: framework_test.dart: "RenderObjectElement.unmount disposes of its renderObject"
    [DebugOnlyFact]
    public void RenderObjectElementUnmountDisposesOfItsRenderObject()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Placeholder());
        RenderObjectElement element = tester.AllElements().OfType<RenderObjectElement>().Last();
        RenderObject renderObject = element.RenderObject;
        Assert.False(renderObject.DebugDisposed);

        tester.PumpWidget(new Container());

        Assert.Throws<AssertionError>(() => element.RenderObject);
        Assert.True(renderObject.DebugDisposed);
    }

    // Flutter: framework_test.dart: "Getting the render object of an unmounted element throws"
    [DebugOnlyFact]
    public void GettingTheRenderObjectOfAnUnmountedElementThrows()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new StatefulLeaf());
        var element = (StatefulElement)tester.ElementOfType<StatefulLeaf>();
        Assert.IsAssignableFrom<State<StatefulLeaf>>(element.State);
        Assert.IsType<StatefulLeaf>(element.Widget);
        // Replace the widget tree to unmount the element.
        tester.PumpWidget(new Container());

        FlutterError error = Assert.ThrowsAny<FlutterError>(() => element.FindRenderObject());
        Assert.Equal(
            "Cannot get renderObject of inactive element.\n"
            + "In order for an element to have a valid renderObject, it must be active, which means it is "
            + "part of the tree.\n"
            + "Instead, this element is in the _ElementLifecycle.defunct state.\n"
            + "If you called this method from a State object, consider guarding it with State.mounted.\n"
            + "The findRenderObject() method was called for the following element:\n"
            + "  StatefulElement#00000(DEFUNCT)",
            FrameworkDartTester.IgnoringHashCodes(error.Message));
    }

    // Flutter: framework_test.dart: "Elements use the identity hashCode"
    [Fact]
    public void ElementsUseTheIdentityHashCode()
    {
        var statefulElement = new StatefulElement(new StatefulLeaf());
        Assert.Equal(RuntimeHelpers.GetHashCode(statefulElement), statefulElement.GetHashCode());

        var statelessElement = new StatelessElement(new Placeholder());
        Assert.Equal(RuntimeHelpers.GetHashCode(statelessElement), statelessElement.GetHashCode());

        var inheritedElement = new InheritedElement(new Directionality(TextDirection.Ltr, new Placeholder()));
        Assert.Equal(RuntimeHelpers.GetHashCode(inheritedElement), inheritedElement.GetHashCode());
    }

    // Flutter: framework_test.dart: "doesDependOnInheritedElement"
    [Fact]
    public void DoesDependOnInheritedElement()
    {
        var ancestor = new TestInheritedElement(new Directionality(TextDirection.Ltr, new Placeholder()));
        var child = new TestInheritedElement(new Directionality(TextDirection.Ltr, new Placeholder()));
        Assert.False(child.DoesDependOnInheritedElement(ancestor));
        child.DependOnInheritedElement(ancestor);
        Assert.True(child.DoesDependOnInheritedElement(ancestor));
    }

    // Flutter: framework_test.dart: "MultiChildRenderObjectElement.updateChildren test"
    [Fact]
    public void MultiChildRenderObjectElementUpdateChildrenTest()
    {
        // Regression test for https://github.com/flutter/flutter/issues/120762.
        using var tester = new FrameworkDartTester();
        GlobalKey globalKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Column([new SizedBox(), new SizedBox(key: globalKey), new SizedBox()]));
        Assert.Null(tester.TakeException());

        tester.PumpWidget(new Column(
            [new SizedBox(), new SizedBox(), new SizedBox(child: new SizedBox(key: globalKey))]));
        Assert.Null(tester.TakeException());
    }

    // Flutter: framework_test.dart: "BuildScope segregates dirty elements"
    [Fact]
    public void BuildScopeSegregatesDirtyElements()
    {
        using var tester = new FrameworkDartTester();
        var buildScope = new BuildScope();
        tester.PumpWidget(new StatefulBuilder((_, _) =>
            new CustomBuildScopeWidget(buildScope: buildScope, child: new NullLeaf())));
        Element rootElement = tester.ElementOfType<StatefulBuilder>();
        Element scopeElement = tester.ElementOfType<CustomBuildScopeWidget>();
        Element leafElement = tester.ElementOfType<NullLeaf>();
        Assert.False(rootElement.Dirty);
        Assert.False(scopeElement.Dirty);
        Assert.False(leafElement.Dirty);

        rootElement.MarkNeedsBuild();
        tester.Pump();
        Assert.False(rootElement.Dirty);
        Assert.False(scopeElement.Dirty);
        Assert.False(leafElement.Dirty);

        scopeElement.MarkNeedsBuild();
        tester.Pump();
        Assert.False(rootElement.Dirty);
        Assert.True(scopeElement.Dirty);
        Assert.False(leafElement.Dirty);

        scopeElement.Owner!.BuildScope(scopeElement);
        tester.Pump();
        Assert.False(rootElement.Dirty);
        Assert.False(scopeElement.Dirty);
        Assert.False(leafElement.Dirty);

        leafElement.MarkNeedsBuild();
        tester.Pump();
        Assert.False(rootElement.Dirty);
        Assert.False(scopeElement.Dirty);
        Assert.True(leafElement.Dirty);

        scopeElement.Owner!.BuildScope(scopeElement);
        tester.Pump();
        Assert.False(rootElement.Dirty);
        Assert.False(scopeElement.Dirty);
        Assert.False(leafElement.Dirty);
    }

    // Flutter: framework_test.dart: "reparenting Element to another BuildScope"
    [Fact]
    public void ReparentingElementToAnotherBuildScope()
    {
        using var tester = new FrameworkDartTester();
        var buildScope = new BuildScope();
        GlobalKey key = new LabeledGlobalKey<State>("key");
        tester.PumpWidget(new DummyMultiChildWidget(
        [
            new CustomBuildScopeWidget(
                // This widget does not call updateChild when rebuild.
                buildScope: buildScope,
                child: new NullLeaf()),
            new NullLeaf(key: key),
        ]));
        Element scopeElement = tester.ElementOfType<CustomBuildScopeWidget>();
        Element keyedWidget = tester.ElementsWithKey(key).Single();

        Assert.False(scopeElement.Dirty);
        Assert.False(keyedWidget.Dirty);

        // Mark the Element with a GlobalKey dirty and reparent it to another BuildScope.
        // the Element should not rebuild.
        keyedWidget.MarkNeedsBuild();
        tester.PumpWidget(new DummyMultiChildWidget(
        [
            new CustomBuildScopeWidget(
                // This widget does not call updateChild when rebuild.
                buildScope: buildScope,
                child: new NullLeaf(key: key)),
            new NullLeaf(),
        ]));

        Assert.False(scopeElement.Dirty);
        Assert.True(keyedWidget.Dirty);
    }

    // Flutter: framework_test.dart: "Calling scheduleBuildFor on an Element already in dirty list throws"
    [DebugOnlyFact]
    public void CallingScheduleBuildForOnAnElementAlreadyInDirtyListThrows()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new SizedBox());
        Element element = tester.ElementOfType<SizedBox>();
        element.MarkNeedsBuild();
        FlutterError error = Assert.ThrowsAny<FlutterError>(() => element.Owner!.ScheduleBuildFor(element));
        Assert.Contains(
            "The BuildOwner.scheduleBuildFor() method called on an Element that is already in the dirty list.",
            error.Message);
    }

    // Flutter: framework_test.dart: "widget is not active if throw in deactivated"
    [DebugOnlyFact]
    public void WidgetIsNotActiveIfThrowInDeactivated()
    {
        using var tester = new FrameworkDartTester();
        FlutterExceptionHandler? onError = FlutterError.OnError;
        FlutterError.OnError = _ => { };
        Element element;
        try
        {
            Widget child = new Placeholder();
            tester.PumpWidget(new StatefulWidgetSpy(
                onDeactivate: _ => throw new InvalidOperationException("kaboom"),
                child: child));
            element = tester.ElementOfWidget(child);
            Assert.True(element.DebugIsActive);

            tester.PumpWidget(new SizedBox());
        }
        finally
        {
            FlutterError.OnError = onError;
        }

        Assert.False(element.DebugIsActive);
        Assert.False(element.DebugIsDefunct);
    }

    // Flutter: framework_test.dart: "widget is not active if throw in activated"
    [Fact]
    public void WidgetIsNotActiveIfThrowInActivated()
    {
        using var tester = new FrameworkDartTester();
        FlutterExceptionHandler? onError = FlutterError.OnError;
        FlutterError.OnError = _ => { };
        Element element;
        try
        {
            Widget child = new Placeholder();
            Widget widget = new StatefulWidgetSpy(
                key: new LabeledGlobalKey<State>(null),
                onActivate: _ => throw new InvalidOperationException("kaboom"),
                child: child);
            tester.PumpWidget(widget);
            element = tester.ElementOfWidget(child);

            tester.PumpWidget(new MetaData(child: widget));
        }
        finally
        {
            FlutterError.OnError = onError;
        }

        Assert.False(element.DebugIsActive);
        Assert.False(element.DebugIsDefunct);
    }

    // Flutter: framework_test.dart: "widget is unmounted if throw in dispose"
    [DebugOnlyFact]
    public void WidgetIsUnmountedIfThrowInDispose()
    {
        using var tester = new FrameworkDartTester();
        FlutterExceptionHandler? onError = FlutterError.OnError;
        FlutterError.OnError = _ => { };
        Element element;
        try
        {
            Widget child = new Placeholder();
            Widget widget = new StatefulWidgetSpy(
                onDispose: _ => throw new InvalidOperationException("kaboom"),
                child: child);
            tester.PumpWidget(widget);
            element = tester.ElementOfWidget(child);
            tester.PumpWidget(child);
        }
        finally
        {
            FlutterError.OnError = onError;
        }

        Assert.True(element.DebugIsDefunct);
    }

    // ------------------------------------------------------------------------------------ helpers

    private const string NonRenderObjectChildMessage =
        "The children of `MultiChildRenderObjectElement` must each has an associated render object.\n"
        + "This typically means that the `EmptyWidget` or its children\n"
        + "are not a subtype of `RenderObjectWidget`.\n"
        + "The following element does not have an associated render object:\n"
        + "  EmptyWidget\n"
        + "debugCreator: EmptyWidget ← Column ← ";

    private static Stack BadnessAtOnce(Key key1, Key key2, Key key3)
    {
        return new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Container(key: key1),
                new Container(key: key1),
                new Container(key: key2),
                new Container(key: key1),
                new Container(key: key1),
                new Container(key: key2),
                new Container(key: key1),
                new Container(key: key1),
                new Row(
                [
                    new Container(key: key1),
                    new Container(key: key1),
                    new Container(key: key2),
                    new Container(key: key2),
                    new Container(key: key2),
                    new Container(key: key3),
                    new Container(key: key2),
                ]),
                new Row([new Container(key: key1), new Container(key: key1), new Container(key: key3)]),
                new Container(key: key3),
            ]);
    }

    private static StatefulState StatefulStateAt(FrameworkDartTester tester, int index)
        => (StatefulState)((StatefulElement)tester.ElementsOfType<Stateful>()[index]).State;

    /// <summary>
    /// Runs <paramref name="body"/> against a freshly constructed <see cref="WidgetsBinding"/>, the
    /// stand-in for flutter_test's <c>tester.binding</c>. Its build owner registers a new focus
    /// manager as the global input handler, so the handler it replaced is restored afterwards.
    /// </summary>
    private static void WithLocalWidgetsBinding(Action<WidgetsBinding> body)
    {
#pragma warning disable CS0618
        Func<KeyMessage, bool>? previousKeyMessageHandler = KeyEventManager.Instance.KeyMessageHandler;
        var binding = new WidgetsBinding();
        try
        {
            body(binding);
        }
        finally
        {
            binding.DebugBuildingDirtyElements = false;
            binding.FocusManager.Dispose();
            KeyEventManager.Instance.KeyMessageHandler = previousKeyMessageHandler;
        }
#pragma warning restore CS0618
    }

    /// <summary>Flutter's <c>hasOneLineDescription</c> matcher.</summary>
    private static void AssertHasOneLineDescription(object value)
    {
        string description = value.ToString()!;
        Assert.NotEmpty(description);
        Assert.DoesNotContain('\n', description);
        Assert.False(description.StartsWith("Instance of '", StringComparison.Ordinal));
    }

    /// <summary>Flutter's <c>hasAGoodToStringDeep</c> matcher.</summary>
    private static void AssertHasAGoodToStringDeep(DiagnosticableTree value)
    {
        const string prefixLineOne = "PREFIX_LINE_ONE____";
        const string prefixOtherLines = "PREFIX_OTHER_LINES_";
        string description = value.ToStringDeep();
        Assert.EndsWith("\n", description);
        Assert.NotEmpty(description.Trim());
        Assert.DoesNotContain("Instance of ", description);
        string[] lines = description.Split('\n');
        Assert.True(lines.Length > 2, "Does not have multiple lines.");
        for (int i = 0; i < lines.Length - 1; i++)
        {
            Assert.NotEmpty(lines[i]);
            Assert.Equal(lines[i].TrimEnd(), lines[i]);
        }

        Assert.False(Regex.IsMatch(lines[^2], "^[│├└ ]*$"), "Last line is all tree connector characters.");

        string[] linesWithPrefixes = value.ToStringDeep(prefixLineOne, prefixOtherLines).Split('\n');
        Assert.StartsWith(prefixLineOne, linesWithPrefixes[0]);
        for (int i = 1; i < linesWithPrefixes.Length - 1; i++)
        {
            Assert.StartsWith(prefixOtherLines, linesWithPrefixes[i]);
        }
    }

    private sealed class TestState : State
    {
        public override Widget Build(BuildContext context) => new SizedBox();
    }

    private sealed class MyGlobalObjectKey<T>(object value) : GlobalObjectKey<T>(value) where T : State;

    private sealed class TestInheritedElement(InheritedWidget widget) : InheritedElement(widget);

    private sealed class WidgetWithNoVisitChildren(Widget child) : StatelessWidget
    {
        public override Widget Build(BuildContext context) => child;

        public override Element CreateElement() => new WidgetWithNoVisitChildrenElement(this);
    }

    private sealed class WidgetWithNoVisitChildrenElement(WidgetWithNoVisitChildren widget) : StatelessElement(widget)
    {
        public override void VisitChildren(Action<Element> visitor)
        {
            // This implementation is intentionally buggy, to test that an error message is
            // shown when this situation occurs.
            // The superclass has the correct implementation (calling `visitor(_child)`), so
            // we don't call it here.
        }
    }

    private sealed class StatefulLeaf(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new StatefulLeafState();
    }

    private sealed class StatefulLeafState : State<StatefulLeaf>
    {
        public void MarkNeedsBuild() => SetState(() => { });

        public override Widget Build(BuildContext context) => SizedBox.Shrink();
    }

    private sealed class Decorate(Action<bool> didChangeDependencies, Action<bool> build) : StatefulWidget
    {
        public Action<bool> DidChangeDependenciesCallback { get; } = didChangeDependencies;

        public Action<bool> BuildCallback { get; } = build;

        public override State CreateState() => new DecorateState();

        public override Element CreateElement() => new DecorateElement(this);
    }

    private sealed class DecorateElement(Decorate widget) : StatefulElement(widget)
    {
        public bool IsDecorated { get; private set; }

        public override Widget Build()
        {
            try
            {
                IsDecorated = true;
                return base.Build();
            }
            finally
            {
                IsDecorated = false;
            }
        }
    }

    private sealed class DecorateState : State<Decorate>
    {
        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            Widget.DidChangeDependenciesCallback(((DecorateElement)Context).IsDecorated);
        }

        public override Widget Build(BuildContext context)
        {
            var element = (DecorateElement)context;
            element.DependOnInheritedWidgetOfExactType<Inherited>();
            Widget.BuildCallback(element.IsDecorated);
            return new Container();
        }
    }

    private sealed class Inherited(
        int? value,
        Widget child,
        Key? key = null,
        Func<Inherited, InheritedElement>? elementCreator = null) : InheritedWidget(child, key)
    {
        public int? Value { get; } = value;

        public override bool UpdateShouldNotify(InheritedWidget oldWidget) => ((Inherited)oldWidget).Value != Value;

        public override Element CreateElement() => elementCreator?.Invoke(this) ?? base.CreateElement();
    }

    private sealed class InheritedElementSpy(InheritedWidget widget, Action<Element>? onRemoveDependent = null)
        : InheritedElement(widget)
    {
        public override void RemoveDependent(Element dependent)
        {
            base.RemoveDependent(dependent);
            onRemoveDependent?.Invoke(dependent);
        }
    }

    private sealed class DependentStatefulWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new DependentState();
    }

    private sealed class DependentState : State<DependentStatefulWidget>
    {
        public int DidChangeDependenciesCount { get; private set; }

        public int DeactivatedCount { get; private set; }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            DidChangeDependenciesCount += 1;
        }

        public override Widget Build(BuildContext context)
        {
            context.DependOnInheritedWidgetOfExactType<Inherited>();
            return new SizedBox();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            DeactivatedCount += 1;
        }
    }

    private sealed class SwapKeyWidget(Key? childKey = null, Key? key = null) : StatefulWidget(key)
    {
        public Key? ChildKey { get; } = childKey;

        public override State CreateState() => new SwapKeyWidgetState();
    }

    private sealed class SwapKeyWidgetState : State<SwapKeyWidget>
    {
        private Key? _key;

        public override void InitState()
        {
            base.InitState();
            _key = Widget.ChildKey;
        }

        public void SwapKey(Key newKey) => SetState(() => _key = newKey);

        public override Widget Build(BuildContext context) => new Container(key: _key);
    }

    private delegate void ElementRebuildCallback(StatefulElement element);

    private sealed class Stateful(Text child, ElementRebuildCallback? onElementRebuild = null) : StatefulWidget
    {
        public Text Child { get; } = child;

        public ElementRebuildCallback? OnElementRebuild { get; } = onElementRebuild;

        public override State CreateState() => new StatefulState();

        public override Element CreateElement() => new StatefulElementSpy(this);
    }

    private sealed class StatefulState : State<Stateful>
    {
        public void Rebuild() => SetState(() => { });

        public override Widget Build(BuildContext context) => Widget.Child;
    }

    private sealed class StatefulElementSpy(Stateful widget) : StatefulElement(widget)
    {
        protected override void PerformRebuild()
        {
            ((Stateful)Widget).OnElementRebuild?.Invoke(this);
            base.PerformRebuild();
        }
    }

    private sealed class DirtyElementWithCustomBuildOwner : Element
    {
        // Dart mixes in RootElementMixin for `assignOwner`; Plumix's counterpart is Element.Attach.
        // Dart's `dirty => true` override is the initial state of a fresh element.
        public DirtyElementWithCustomBuildOwner(BuildOwner buildOwner, Widget widget) : base(widget)
        {
            Attach(buildOwner);
        }

        public override bool DebugDoingBuild => throw new NotImplementedException();
    }

    private sealed class StatelessWidgetSpy(Action<BuildContext> onBuild, Key? key = null) : StatelessWidget(key)
    {
        public override Widget Build(BuildContext context)
        {
            onBuild(context);
            return new Container();
        }
    }

    private sealed class StatefulWidgetSpy(
        Key? key = null,
        Action<BuildContext>? onBuild = null,
        Action<BuildContext>? onInitState = null,
        Action<BuildContext>? onDidChangeDependencies = null,
        Action<BuildContext>? onDispose = null,
        Action<BuildContext>? onDeactivate = null,
        Action<BuildContext>? onActivate = null,
        Action<BuildContext>? onDidUpdateWidget = null,
        Widget? child = null) : StatefulWidget(key)
    {
        public Action<BuildContext>? OnBuild { get; } = onBuild;
        public Action<BuildContext>? OnInitState { get; } = onInitState;
        public Action<BuildContext>? OnDidChangeDependencies { get; } = onDidChangeDependencies;
        public Action<BuildContext>? OnDispose { get; } = onDispose;
        public Action<BuildContext>? OnDeactivate { get; } = onDeactivate;
        public Action<BuildContext>? OnActivate { get; } = onActivate;
        public Action<BuildContext>? OnDidUpdateWidget { get; } = onDidUpdateWidget;
        public Widget Child { get; } = child ?? new SizedBox();

        public override State CreateState() => new StatefulWidgetSpyState();
    }

    private sealed class StatefulWidgetSpyState : State<StatefulWidgetSpy>
    {
        public override void InitState()
        {
            base.InitState();
            Widget.OnInitState?.Invoke(Context);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            Widget.OnDeactivate?.Invoke(Context);
        }

        public override void Activate()
        {
            base.Activate();
            Widget.OnActivate?.Invoke(Context);
        }

        public override void Dispose()
        {
            base.Dispose();
            Widget.OnDispose?.Invoke(Context);
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            Widget.OnDidChangeDependencies?.Invoke(Context);
        }

        public override void DidUpdateWidget(StatefulWidgetSpy oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            Widget.OnDidUpdateWidget?.Invoke(Context);
        }

        public override Widget Build(BuildContext context)
        {
            Widget.OnBuild?.Invoke(context);
            return Widget.Child;
        }
    }

    private sealed class RenderObjectWidgetSpy(
        Action<BuildContext>? onCreateRenderObject = null,
        Action<BuildContext>? onUpdateRenderObject = null,
        Action? onDidUnmountRenderObject = null) : LeafRenderObjectWidget
    {
        public override RenderObject CreateRenderObject(BuildContext context)
        {
            onCreateRenderObject?.Invoke(context);
            return new FakeLeafRenderObject();
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            onUpdateRenderObject?.Invoke(context);
        }

        public override void DidUnmountRenderObject(RenderObject renderObject)
        {
            base.DidUnmountRenderObject(renderObject);
            onDidUnmountRenderObject?.Invoke();
        }
    }

    private sealed class FakeLeafRenderObject : RenderBox
    {
        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;

        protected override void PerformLayout()
        {
            Size = Constraints.Biggest;
        }

        // Dart's RenderBox.paint is a no-op; Plumix declares it abstract.
        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class TestRenderObjectElement() : RenderObjectElement(new Table())
    {
        public override void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public override void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }

    private sealed class EmptyWidget : Widget
    {
        public override Element CreateElement() => new EmptyElement(this);
    }

    private sealed class EmptyElement(EmptyWidget widget) : Element(widget)
    {
        public override bool DebugDoingBuild => false;
    }

    private sealed class CustomBuildScopeWidget(BuildScope? buildScope, Widget child) : ProxyWidget(child)
    {
        public BuildScope? CustomBuildScope { get; } = buildScope;

        public override Element CreateElement() => new CustomBuildScopeElement(this);
    }

    private sealed class CustomBuildScopeElement(CustomBuildScopeWidget widget) : Element(widget)
    {
        private Element? _child;

        public override BuildScope BuildScope => ((CustomBuildScopeWidget)Widget).CustomBuildScope ?? base.BuildScope;

        public override bool DebugDoingBuild => throw new NotImplementedException();

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild(force: true);
            // Only does tree walk on mount.
            _child = UpdateChild(_child, ((CustomBuildScopeWidget)Widget).Child, Slot);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            Element? child = _child;
            if (child != null)
            {
                visitor(child);
            }
        }
    }

    private sealed class DummyMultiChildWidget(IReadOnlyList<Widget> children) : Widget
    {
        public IReadOnlyList<Widget> Children { get; } = children;

        public override Element CreateElement() => new DummyMultiChildElement(this);
    }

    private sealed class DummyMultiChildElement(DummyMultiChildWidget widget) : Element(widget)
    {
        private readonly HashSet<Element> _forgottenChildren = [];
        private List<Element> _children = [];

        public override bool DebugDoingBuild => throw new NotImplementedException();

        public override Element? RenderObjectAttachingChild => null;

        protected override void OnMount()
        {
            base.OnMount();
            IReadOnlyList<Widget> childWidgets = ((DummyMultiChildWidget)Widget).Children;

            Element? previousChild = null;
            var children = new List<Element>(childWidgets.Count);
            for (int i = 0; i < childWidgets.Count; i++)
            {
                Element child = previousChild = InflateWidget(
                    childWidgets[i],
                    new IndexedSlot<Element?>(i, previousChild));
                children.Add(child);
            }

            _children = children;
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            _children = UpdateChildren(
                _children,
                ((DummyMultiChildWidget)newWidget).Children,
                forgottenChildren: _forgottenChildren);
            _forgottenChildren.Clear();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            foreach (Element child in _children)
            {
                if (!_forgottenChildren.Contains(child))
                {
                    visitor(child);
                }
            }
        }

        public override void ForgetChild(Element child)
        {
            DebugAssertions.Assert(_children.Contains(child));
            DebugAssertions.Assert(!_forgottenChildren.Contains(child));
            _forgottenChildren.Add(child);
            base.ForgetChild(child);
        }
    }

    private sealed class NullLeaf(Key? key = null) : Widget(key)
    {
        public override Element CreateElement() => new NullLeafElement(this);
    }

    // Dart's `_NullElement` (framework_test.dart's own, not framework.dart's private one).
    private sealed class NullLeafElement(NullLeaf widget) : Element(widget)
    {
        protected override void OnMount()
        {
            base.OnMount();
            Rebuild(force: true);
        }

        public override bool DebugDoingBuild => throw new NotImplementedException();
    }

    private sealed class TestLeaderLayerWidget(LayerLink? link = null, Widget? child = null)
        : SingleChildRenderObjectWidget(child)
    {
        public LayerLink? Link { get; } = link;

        public override RenderObject CreateRenderObject(BuildContext context)
            => new RenderTestLeaderLayerWidget(Link);

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderTestLeaderLayerWidget)renderObject).Link = Link;
        }
    }

    private sealed class RenderTestLeaderLayerWidget(LayerLink? link) : RenderProxyBox
    {
        private LayerLink? _link = link;

        public LayerLink? Link
        {
            get => _link;
            set
            {
                if (ReferenceEquals(_link, value))
                {
                    return;
                }

                _link = value;
                MarkNeedsPaint();
            }
        }

        public override bool IsRepaintBoundary => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            base.Paint(ctx, offset);
            if (_link != null)
            {
                ctx.PushLayer(new LeaderLayer(_link, offset), (_, _) => { }, default);
            }
        }
    }
}
