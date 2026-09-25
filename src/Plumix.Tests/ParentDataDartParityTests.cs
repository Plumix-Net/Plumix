using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/parent_data_test.dart. FrameworkParityTests only
// checks fragments of the "Incorrect use of ParentDataWidget" reports on synthetic widgets, so every
// Dart test is ported here.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ParentDataDartParityTests
{
    private static readonly TestParentData KNonPositioned = new();

    private const string CompetingPrefix =
        "Incorrect use of ParentDataWidget.\n"
        + "Competing ParentDataWidgets are providing parent data to the same RenderObject:\n";

    private const string CompetingBody =
        "A RenderObject can receive parent data from multiple "
        + "ParentDataWidgets, but the Type of ParentData must be unique to "
        + "prevent one overwriting another.\n"
        + "Usually, this indicates that one or more of the offending ParentDataWidgets listed "
        + "above isn't placed inside a dedicated compatible ancestor widget that it isn't "
        + "sharing with another ParentDataWidget of the same type.\n"
        + "Otherwise, separating aspects of ParentData to prevent conflicts can "
        + "be done using mixins, mixing them all in on the full ParentData "
        + "Object, such as KeepAlive does with KeepAliveParentDataMixin.\n"
        + "The ownership chain for the RenderObject that received the parent data was:\n";

    // Flutter: parent_data_test.dart: "ParentDataWidget control test"
    [Fact]
    public void ParentDataWidgetControlTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA),
                new Positioned(
                    top: 10.0,
                    left: 10.0,
                    child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)),
                new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationC),
            ]));

        CheckTree(tester, [KNonPositioned, new TestParentData(top: 10.0, left: 10.0), KNonPositioned]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    bottom: 5.0,
                    right: 7.0,
                    child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA)),
                new Positioned(
                    top: 10.0,
                    left: 10.0,
                    child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)),
                new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationC),
            ]));

        CheckTree(
            tester,
            [new TestParentData(bottom: 5.0, right: 7.0), new TestParentData(top: 10.0, left: 10.0), KNonPositioned]);

        var kDecoratedBoxA = new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationA);
        var kDecoratedBoxB = new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB);
        var kDecoratedBoxC = new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationC);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(bottom: 5.0, right: 7.0, child: kDecoratedBoxA),
                new Positioned(top: 10.0, left: 10.0, child: kDecoratedBoxB),
                kDecoratedBoxC,
            ]));

        CheckTree(
            tester,
            [new TestParentData(bottom: 5.0, right: 7.0), new TestParentData(top: 10.0, left: 10.0), KNonPositioned]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(bottom: 6.0, right: 8.0, child: kDecoratedBoxA),
                new Positioned(left: 10.0, right: 10.0, child: kDecoratedBoxB),
                kDecoratedBoxC,
            ]));

        CheckTree(
            tester,
            [new TestParentData(bottom: 6.0, right: 8.0), new TestParentData(left: 10.0, right: 10.0), KNonPositioned]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                kDecoratedBoxA,
                new Positioned(left: 11.0, right: 12.0, child: new Container(child: kDecoratedBoxB)),
                kDecoratedBoxC,
            ]));

        CheckTree(tester, [KNonPositioned, new TestParentData(left: 11.0, right: 12.0), KNonPositioned]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                kDecoratedBoxA,
                new Positioned(right: 10.0, child: new Container(child: kDecoratedBoxB)),
                new DummyWidget(child: new Positioned(top: 8.0, child: kDecoratedBoxC)),
            ]));

        CheckTree(tester, [KNonPositioned, new TestParentData(right: 10.0), new TestParentData(top: 8.0)]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    right: 10.0,
                    child: new FrameworkDartFlipWidget(left: kDecoratedBoxA, right: kDecoratedBoxB)),
            ]));

        CheckTree(tester, [new TestParentData(right: 10.0)]);

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);
        tester.Pump();

        CheckTree(tester, [new TestParentData(right: 10.0)]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    top: 7.0,
                    child: new FrameworkDartFlipWidget(left: kDecoratedBoxA, right: kDecoratedBoxB)),
            ]));

        CheckTree(tester, [new TestParentData(top: 7.0)]);

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);
        tester.Pump();

        CheckTree(tester, [new TestParentData(top: 7.0)]);

        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr));

        CheckTree(tester, []);
    }

    // Flutter: parent_data_test.dart: "ParentData overwrite with custom ParentDataWidget subclasses"
    [DebugOnlyFact]
    public void ParentDataOverwriteWithCustomParentDataWidgetSubclasses()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Stack(
                children:
                [
                    new CustomPositionedWidget(
                        bottom: 8.0,
                        child: new Positioned(
                            top: 6.0,
                            left: 7.0,
                            child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB))),
                ])));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(
            CompetingPrefix
            + "- Positioned(left: 7.0, top: 6.0), which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + "- CustomPositionedWidget, which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + CompetingBody
            // End of chain omitted, not relevant for test.
            + "  DecoratedBox ← Positioned ← CustomPositionedWidget ← Stack ← Directionality ← ",
            exception!.ToString(),
            StringComparison.Ordinal);

        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Stack(
                children:
                [
                    new SubclassPositioned(
                        bottom: 8.0,
                        child: new Positioned(
                            top: 6.0,
                            left: 7.0,
                            child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB))),
                ])));

        exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(
            CompetingPrefix
            + "- Positioned(left: 7.0, top: 6.0), which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + "- SubclassPositioned(bottom: 8.0), which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + CompetingBody
            // End of chain omitted, not relevant for test.
            + "  DecoratedBox ← Positioned ← SubclassPositioned ← Stack ← Directionality ← ",
            exception!.ToString(),
            StringComparison.Ordinal);
    }

    // Flutter: parent_data_test.dart: "ParentDataWidget conflicting data"
    [DebugOnlyFact]
    public void ParentDataWidgetConflictingData()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Stack(
                children:
                [
                    new Positioned(
                        top: 5.0,
                        bottom: 8.0,
                        child: new Positioned(
                            top: 6.0,
                            left: 7.0,
                            child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB))),
                ])));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(
            CompetingPrefix
            + "- Positioned(left: 7.0, top: 6.0), which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + "- Positioned(top: 5.0, bottom: 8.0), which writes ParentData of type "
            + "StackParentData, (typically placed directly inside a Stack widget)\n"
            + CompetingBody
            // End of chain omitted, not relevant for test.
            + "  DecoratedBox ← Positioned ← Positioned ← Stack ← Directionality ← ",
            exception!.ToString(),
            StringComparison.Ordinal);

        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr));
        CheckTree(tester, []);

        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new DummyWidget(
                child: new Row(
                [
                    new Positioned(
                        top: 6.0,
                        left: 7.0,
                        child: new DecoratedBox(decoration: FrameworkDartTestWidgets.BoxDecorationB)),
                ]))));

        exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(
            "Incorrect use of ParentDataWidget.\n"
            + "The ParentDataWidget Positioned(left: 7.0, top: 6.0) wants to apply ParentData of type "
            + "StackParentData to a RenderObject, which has been set up to accept ParentData of "
            + "incompatible type FlexParentData.\n"
            + "Usually, this means that the Positioned widget has the wrong ancestor RenderObjectWidget. "
            + "Typically, Positioned widgets are placed directly inside Stack widgets.\n"
            + "The offending Positioned is currently placed inside a Row widget.\n"
            + "The ownership chain for the RenderObject that received the incompatible parent data was:\n"
            // End of chain omitted, not relevant for test.
            + "  DecoratedBox ← Positioned ← Row ← DummyWidget ← Directionality ← ",
            exception!.ToString(),
            StringComparison.Ordinal);

        tester.PumpWidget(new Stack(textDirection: TextDirection.Ltr));
        CheckTree(tester, []);
    }

    // Flutter: parent_data_test.dart: "ParentDataWidget interacts with global keys"
    [Fact]
    public void ParentDataWidgetInteractsWithGlobalKeys()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    top: 10.0,
                    left: 10.0,
                    child: new DecoratedBox(key: key, decoration: FrameworkDartTestWidgets.BoxDecorationA)),
            ]));

        CheckTree(tester, [new TestParentData(top: 10.0, left: 10.0)]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    top: 10.0,
                    left: 10.0,
                    child: new DecoratedBox(
                        decoration: FrameworkDartTestWidgets.BoxDecorationB,
                        child: new DecoratedBox(key: key, decoration: FrameworkDartTestWidgets.BoxDecorationA))),
            ]));

        CheckTree(tester, [new TestParentData(top: 10.0, left: 10.0)]);

        tester.PumpWidget(new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    top: 10.0,
                    left: 10.0,
                    child: new DecoratedBox(key: key, decoration: FrameworkDartTestWidgets.BoxDecorationA)),
            ]));

        CheckTree(tester, [new TestParentData(top: 10.0, left: 10.0)]);
    }

    // Flutter: parent_data_test.dart: "Parent data invalid ancestor"
    [DebugOnlyFact]
    public void ParentDataInvalidAncestor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Row(
            [
                new Stack(
                    textDirection: TextDirection.Ltr,
                    children: [new Expanded(child: new Container())]),
            ])));

        object? exception = tester.TakeException();
        Assert.IsType<FlutterError>(exception);
        Assert.StartsWith(
            "Incorrect use of ParentDataWidget.\n"
            + "The ParentDataWidget Expanded(flex: 1) wants to apply ParentData of type "
            + "FlexParentData to a RenderObject, which has been set up to accept ParentData of "
            + "incompatible type StackParentData.\n"
            + "Usually, this means that the Expanded widget has the wrong ancestor RenderObjectWidget. "
            + "Typically, Expanded widgets are placed directly inside Flex widgets.\n"
            + "The offending Expanded is currently placed inside a Stack widget.\n"
            + "The ownership chain for the RenderObject that received the incompatible parent data was:\n"
            // Omitted end of debugCreator chain because it's irrelevant for test.
            + "  LimitedBox ← Container ← Expanded ← Stack ← Row ← Directionality ← ",
            exception!.ToString(),
            StringComparison.Ordinal);
    }

    // Flutter: parent_data_test.dart: "ParentDataWidget can be used with different ancestor RenderObjectWidgets"
    [Fact]
    public void ParentDataWidgetCanBeUsedWithDifferentAncestorRenderObjectWidgets()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new OneAncestorWidget(child: new Container()));
        var parentData = (DummyParentData)tester.ElementOfType<Container>().RenderObject!.parentData!;
        Assert.Null(parentData.String);

        tester.PumpWidget(new OneAncestorWidget(
            child: new TestParentDataWidget(@string: "Foo", child: new Container())));
        parentData = (DummyParentData)tester.ElementOfType<Container>().RenderObject!.parentData!;
        Assert.Equal("Foo", parentData.String);

        tester.PumpWidget(new AnotherAncestorWidget(
            child: new TestParentDataWidget(@string: "Bar", child: new Container())));
        parentData = (DummyParentData)tester.ElementOfType<Container>().RenderObject!.parentData!;
        Assert.Equal("Bar", parentData.String);
    }

    /// <summary>Dart's top-level <c>checkTree</c>.</summary>
    private static void CheckTree(FrameworkDartTester tester, IReadOnlyList<TestParentData> expectedParentData)
    {
        MultiChildRenderObjectElement element = tester.AllElements().OfType<MultiChildRenderObjectElement>().Single();
        Assert.NotNull(element);
        Assert.IsAssignableFrom<RenderStack>(element.RenderObject);
        var renderObject = (RenderStack)element.RenderObject!;
        try
        {
            RenderObject? child = renderObject.FirstChild;
            foreach (TestParentData expected in expectedParentData)
            {
                Assert.IsAssignableFrom<RenderDecoratedBox>(child);
                var decoratedBox = (RenderDecoratedBox)child!;
                Assert.IsAssignableFrom<StackParentData>(decoratedBox.parentData);
                var parentData = (StackParentData)decoratedBox.parentData!;
                Assert.Equal(expected.Top, parentData.Top);
                Assert.Equal(expected.Right, parentData.Right);
                Assert.Equal(expected.Bottom, parentData.Bottom);
                Assert.Equal(expected.Left, parentData.Left);
                var decoratedBoxParentData = decoratedBox.parentData as StackParentData;
                child = decoratedBoxParentData?.nextSibling;
            }

            Assert.Null(child);
        }
        catch (Exception)
        {
            Print.DebugPrint(renderObject.ToStringDeep());
            throw;
        }
    }

    /// <summary>Dart's <c>TestParentData</c>.</summary>
    private sealed class TestParentData(double? top = null, double? right = null, double? bottom = null,
        double? left = null)
    {
        public double? Top { get; } = top;

        public double? Right { get; } = right;

        public double? Bottom { get; } = bottom;

        public double? Left { get; } = left;
    }

    /// <summary>Dart's <c>SubclassPositioned</c>.</summary>
    private sealed class SubclassPositioned(
        Widget child,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Key? key = null)
        : Positioned(child, left, top, right, bottom, width, height, key)
    {
        public override void ApplyParentData(RenderObject renderObject)
        {
            DebugAssertions.Assert(renderObject.parentData is StackParentData);
            var parentData = (StackParentData)renderObject.parentData!;
            parentData.Bottom = Bottom;
        }
    }

    /// <summary>Dart's <c>CustomPositionedWidget</c>.</summary>
    private sealed class CustomPositionedWidget(double bottom, Widget child, Key? key = null)
        : ParentDataWidget<StackParentData>(child, key)
    {
        public double Bottom { get; } = bottom;

        public override void ApplyParentData(RenderObject renderObject)
        {
            DebugAssertions.Assert(renderObject.parentData is StackParentData);
            var parentData = (StackParentData)renderObject.parentData!;
            parentData.Bottom = Bottom;
        }

        public override Type DebugTypicalAncestorWidgetClass => typeof(Stack);
    }

    /// <summary>Dart's <c>TestParentDataWidget</c>.</summary>
    private sealed class TestParentDataWidget(string @string, Widget child, Key? key = null)
        : ParentDataWidget<DummyParentData>(child, key)
    {
        public string String { get; } = @string;

        public override void ApplyParentData(RenderObject renderObject)
        {
            DebugAssertions.Assert(renderObject.parentData is DummyParentData);
            var parentData = (DummyParentData)renderObject.parentData!;
            parentData.String = String;
        }

        public override Type DebugTypicalAncestorWidgetClass => typeof(OneAncestorWidget);
    }

    /// <summary>Dart's <c>DummyParentData</c>.</summary>
    private sealed class DummyParentData : ParentData
    {
        public string? String { get; set; }
    }

    /// <summary>Dart's <c>OneAncestorWidget</c>.</summary>
    private sealed class OneAncestorWidget(Widget child, Key? key = null) : SingleChildRenderObjectWidget(child, key)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RenderOne();
    }

    /// <summary>Dart's <c>AnotherAncestorWidget</c>.</summary>
    private sealed class AnotherAncestorWidget(Widget child, Key? key = null)
        : SingleChildRenderObjectWidget(child, key)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RenderAnother();
    }

    /// <summary>Dart's <c>RenderOne</c>.</summary>
    private sealed class RenderOne : RenderProxyBox
    {
        public override void SetupParentData(RenderObject child)
        {
            if (child.parentData is not DummyParentData)
            {
                child.parentData = new DummyParentData();
            }
        }
    }

    /// <summary>Dart's <c>RenderAnother</c>.</summary>
    private sealed class RenderAnother : RenderProxyBox
    {
        public override void SetupParentData(RenderObject child)
        {
            if (child.parentData is not DummyParentData)
            {
                child.parentData = new DummyParentData();
            }
        }
    }

    /// <summary>Dart's <c>DummyWidget</c>.</summary>
    private sealed class DummyWidget(Widget child, Key? key = null) : StatelessWidget(key)
    {
        public Widget Child { get; } = child;

        public override Widget Build(BuildContext context) => Child;
    }
}
