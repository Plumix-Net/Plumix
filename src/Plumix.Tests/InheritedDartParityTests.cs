using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/inherited_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InheritedDartParityTests
{
    // Flutter: inherited_test.dart: "Inherited notifies dependents"
    [Fact]
    public void InheritedNotifiesDependents()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<TestInherited>();

        var builder = new Builder(context =>
        {
            log.Add(context.DependOnInheritedWidgetOfExactType<TestInherited>()!);
            return new Container();
        });

        var first = new TestInherited(child: builder);
        tester.PumpWidget(first);

        Assert.Equal([first], log);

        var second = new TestInherited(shouldNotify: false, child: builder);
        tester.PumpWidget(second);

        Assert.Equal([first], log);

        var third = new TestInherited(child: builder);
        tester.PumpWidget(third);

        Assert.Equal([first, third], log);
    }

    // Flutter: inherited_test.dart: "Update inherited when reparenting state"
    [Fact]
    public void UpdateInheritedWhenReparentingState()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey globalKey = new LabeledGlobalKey<State>(null);
        var log = new List<TestInherited>();

        TestInherited Build()
        {
            return new TestInherited(
                key: new UniqueKey(),
                child: new Container(
                    key: globalKey,
                    child: new Builder(context =>
                    {
                        log.Add(context.DependOnInheritedWidgetOfExactType<TestInherited>()!);
                        return new Container();
                    })));
        }

        TestInherited first = Build();
        tester.PumpWidget(first);

        Assert.Equal([first], log);

        TestInherited second = Build();
        tester.PumpWidget(second);

        Assert.Equal([first, second], log);
    }

    // Flutter: inherited_test.dart: "Update inherited when removing node"
    [Fact]
    public void UpdateInheritedWhenRemovingNode()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();

        tester.PumpWidget(new ValueInherited(
            value: 1,
            child: new FrameworkDartFlipWidget(
                left: new ValueInherited(
                    value: 2,
                    child: new ValueInherited(
                        value: 3,
                        child: new Builder(context =>
                        {
                            ValueInherited v = context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
                            log.Add($"a: {v.Value}");
                            return new Text(string.Empty, textDirection: TextDirection.Ltr);
                        }))),
                right: new ValueInherited(
                    value: 2,
                    child: new Builder(context =>
                    {
                        ValueInherited v = context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
                        log.Add($"b: {v.Value}");
                        return new Text(string.Empty, textDirection: TextDirection.Ltr);
                    })))));

        AssertFlipLog(tester, log, ["a: 3"], ["b: 2"], ["a: 3"]);
    }

    // Flutter: inherited_test.dart: "Update inherited when removing node and child has global key"
    [Fact]
    public void UpdateInheritedWhenRemovingNodeAndChildHasGlobalKey()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();

        Key key = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(new ValueInherited(
            value: 1,
            child: new FrameworkDartFlipWidget(
                left: new ValueInherited(
                    value: 2,
                    child: new ValueInherited(
                        value: 3,
                        child: new Container(
                            key: key,
                            child: new Builder(context =>
                            {
                                ValueInherited v =
                                    context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
                                log.Add($"a: {v.Value}");
                                return new Text(string.Empty, textDirection: TextDirection.Ltr);
                            })))),
                right: new ValueInherited(
                    value: 2,
                    child: new Container(
                        key: key,
                        child: new Builder(context =>
                        {
                            ValueInherited v = context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
                            log.Add($"b: {v.Value}");
                            return new Text(string.Empty, textDirection: TextDirection.Ltr);
                        }))))));

        AssertFlipLog(tester, log, ["a: 3"], ["b: 2"], ["a: 3"]);
    }

    // Flutter: inherited_test.dart:
    // "Update inherited when removing node and child has global key with constant child"
    [Fact]
    public void UpdateInheritedWhenRemovingNodeAndChildHasGlobalKeyWithConstantChild()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<int>();

        Key key = new LabeledGlobalKey<State>(null);

        Widget child = new Builder(context =>
        {
            ValueInherited v = context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
            log.Add(v.Value);
            return new Text(string.Empty, textDirection: TextDirection.Ltr);
        });

        tester.PumpWidget(new ValueInherited(
            value: 1,
            child: new FrameworkDartFlipWidget(
                left: new ValueInherited(
                    value: 2,
                    child: new ValueInherited(
                        value: 3,
                        child: new Container(key: key, child: child))),
                right: new ValueInherited(
                    value: 2,
                    child: new Container(key: key, child: child)))));

        AssertFlipLog(tester, log, [3], [2], [3]);
    }

    // Flutter: inherited_test.dart:
    // "Update inherited when removing node and child has global key with constant child, minimised"
    [Fact]
    public void UpdateInheritedWhenRemovingNodeAndChildHasGlobalKeyWithConstantChildMinimised()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<int>();

        Widget child = new Builder(
            key: new LabeledGlobalKey<State>(null),
            builder: context =>
            {
                ValueInherited v = context.DependOnInheritedWidgetOfExactType<ValueInherited>()!;
                log.Add(v.Value);
                return new Text(string.Empty, textDirection: TextDirection.Ltr);
            });

        tester.PumpWidget(new ValueInherited(
            value: 2,
            child: new FrameworkDartFlipWidget(
                left: new ValueInherited(value: 3, child: child),
                right: child)));

        AssertFlipLog(tester, log, [3], [2], [3]);
    }

    // Flutter: inherited_test.dart:
    // "Inherited widget notifies descendants when descendant previously failed to find a match"
    [Fact]
    public void InheritedWidgetNotifiesDescendantsWhenDescendantPreviouslyFailedToFindAMatch()
    {
        using var tester = new FrameworkDartTester();
        int? inheritedValue = -1;

        Widget inner = new Container(
            key: new LabeledGlobalKey<State>(null),
            child: new Builder(context =>
            {
                ValueInherited? widget = context.DependOnInheritedWidgetOfExactType<ValueInherited>();
                inheritedValue = widget?.Value;
                return new Container();
            }));

        tester.PumpWidget(inner);
        Assert.Null(inheritedValue);

        inheritedValue = -2;
        tester.PumpWidget(new ValueInherited(value: 3, child: inner));
        Assert.Equal(3, inheritedValue);
    }

    // Flutter: inherited_test.dart:
    // "Inherited widget doesn't notify descendants when descendant did not previously fail to find a
    // match and had no dependencies"
    [Fact]
    public void InheritedWidgetDoesNotNotifyDescendantsWithoutFailedLookupOrDependencies()
    {
        using var tester = new FrameworkDartTester();
        int buildCount = 0;

        Widget inner = new Container(
            key: new LabeledGlobalKey<State>(null),
            child: new Builder(_ =>
            {
                buildCount += 1;
                return new Container();
            }));

        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);

        tester.PumpWidget(new ValueInherited(value: 3, child: inner));
        Assert.Equal(1, buildCount);
    }

    // Flutter: inherited_test.dart:
    // "Inherited widget does notify descendants when descendant did not previously fail to find a
    // match but did have other dependencies"
    [Fact]
    public void InheritedWidgetDoesNotifyDescendantsThatHadOtherDependencies()
    {
        using var tester = new FrameworkDartTester();
        int buildCount = 0;

        Widget inner = new Container(
            key: new LabeledGlobalKey<State>(null),
            child: new TestInherited(
                shouldNotify: false,
                child: new Builder(context =>
                {
                    context.DependOnInheritedWidgetOfExactType<TestInherited>();
                    buildCount += 1;
                    return new Container();
                })));

        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);

        tester.PumpWidget(new ValueInherited(value: 3, child: inner));
        Assert.Equal(2, buildCount);
    }

    // Flutter: inherited_test.dart: "BuildContext.getInheritedWidgetOfExactType doesn't create a dependency"
    [Fact]
    public void GetInheritedWidgetOfExactTypeDoesNotCreateADependency()
    {
        using var notifier = new ChangeNotifier();
        using var tester = new FrameworkDartTester();
        int buildCount = 0;
        GlobalKey inheritedKey = new LabeledGlobalKey<State>(null);

        Widget builder = new Builder(context =>
        {
            Assert.Same(
                inheritedKey.CurrentWidget,
                context.GetInheritedWidgetOfExactType<ChangeNotifierInherited>());
            buildCount += 1;
            return new Container();
        });

        Widget inner = new ChangeNotifierInherited(
            key: inheritedKey,
            notifier: notifier,
            child: builder);

        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);
        notifier.NotifyListeners();
        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);
    }

    // Flutter: inherited_test.dart: "initState() dependency on Inherited asserts"
    [DebugOnlyFact]
    public void InitStateDependencyOnInheritedAsserts()
    {
        // This is a regression test for https://github.com/flutter/flutter/issues/5491
        using var tester = new FrameworkDartTester();
        bool exceptionCaught = false;

        var parent = new TestInherited(
            child: new ExpectFail(() =>
            {
                exceptionCaught = true;
            }));
        tester.PumpWidget(parent);

        Assert.True(exceptionCaught);
    }

    // Flutter: inherited_test.dart: "InheritedNotifier"
    [Fact]
    public void InheritedNotifier()
    {
        using var notifier = new ChangeNotifier();
        using var tester = new FrameworkDartTester();
        int buildCount = 0;

        Widget builder = new Builder(context =>
        {
            context.DependOnInheritedWidgetOfExactType<ChangeNotifierInherited>();
            buildCount += 1;
            return new Container();
        });

        Widget inner = new ChangeNotifierInherited(notifier: notifier, child: builder);
        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);

        tester.PumpWidget(inner);
        Assert.Equal(1, buildCount);

        tester.Pump();
        Assert.Equal(1, buildCount);

        notifier.NotifyListeners();
        tester.Pump();
        Assert.Equal(2, buildCount);

        tester.PumpWidget(inner);
        Assert.Equal(2, buildCount);

        tester.PumpWidget(new ChangeNotifierInherited(child: builder));
        Assert.Equal(3, buildCount);
    }

    // Flutter: inherited_test.dart: "InheritedWidgets can trigger RenderObject updates"
    [Fact]
    public void InheritedWidgetsCanTriggerRenderObjectUpdates()
    {
        using var tester = new FrameworkDartTester();
        var cardThemeData = new CardThemeData(Color: MaterialColors.White);
        StateSetter? setState = null;

        // Verifies that the "themed card" is rendered
        // with the appropriate inherited theme data.
        void ExpectCardToMatchTheme()
        {
            var renderShape = (RenderPhysicalShape)tester.ElementOfType<ThemedCard>().FindRenderObject()!;

            if (cardThemeData.Color is { } color)
            {
                Assert.Equal(color, renderShape.Color);
            }

            if (cardThemeData.Elevation is { } elevation)
            {
                Assert.Equal(elevation, renderShape.Elevation);
            }

            if (cardThemeData.ShadowColor is { } shadowColor)
            {
                Assert.Equal(shadowColor, renderShape.ShadowColor);
            }

            if (cardThemeData.Shape is { } shape)
            {
                CustomClipper<Path>? clipper = renderShape.Clipper;
                Assert.IsType<ShapeBorderClipper>(clipper);
                Assert.Equal(shape, ((ShapeBorderClipper)clipper!).Shape);
            }

            if (cardThemeData.ClipBehavior is { } clipBehavior)
            {
                Assert.Equal(clipBehavior, renderShape.ClipBehavior);
            }
        }

        tester.PumpWidget(new StatefulBuilder((_, stateSetter) =>
        {
            setState = stateSetter;
            return new Theme(
                data: new ThemeData(cardTheme: cardThemeData),
                child: new ThemedCard());
        }));
        ExpectCardToMatchTheme();

        setState!(() =>
        {
            cardThemeData = new CardThemeData(
                Shape: new BeveledRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(20))));
        });
        tester.Pump();
        ExpectCardToMatchTheme();

        setState(() =>
        {
            cardThemeData = new CardThemeData(ClipBehavior: Clip.HardEdge);
        });
        tester.Pump();
        ExpectCardToMatchTheme();

        setState(() =>
        {
            cardThemeData = new CardThemeData(
                Elevation: 5.0,
                ShadowColor: MaterialColors.BlueGrey,
                Shape: new ContinuousRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(8.0))),
                ClipBehavior: Clip.AntiAliasWithSaveLayer);
        });
        tester.Pump();
        ExpectCardToMatchTheme();
    }

    /// <summary>
    /// The shared tail of the "Update inherited when removing node" tests: the first pump's log, an
    /// idle pump that must not rebuild, then two flips.
    /// </summary>
    private static void AssertFlipLog<T>(
        FrameworkDartTester tester,
        List<T> log,
        T[] initial,
        T[] afterFirstFlip,
        T[] afterSecondFlip)
    {
        Assert.Equal(initial, log);
        log.Clear();

        tester.Pump();

        Assert.Empty(log);
        log.Clear();

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);
        tester.Pump();

        Assert.Equal(afterFirstFlip, log);
        log.Clear();

        FrameworkDartTestWidgets.FlipStatefulWidget(tester);
        tester.Pump();

        Assert.Equal(afterSecondFlip, log);
        log.Clear();
    }

    private sealed class TestInherited(Widget child, bool shouldNotify = true, Key? key = null)
        : InheritedWidget(child, key)
    {
        public bool ShouldNotify { get; } = shouldNotify;

        public override bool UpdateShouldNotify(InheritedWidget oldWidget) => ShouldNotify;
    }

    private sealed class ValueInherited(Widget child, int value, Key? key = null) : InheritedWidget(child, key)
    {
        public int Value { get; } = value;

        public override bool UpdateShouldNotify(InheritedWidget oldWidget)
            => Value != ((ValueInherited)oldWidget).Value;
    }

    private sealed class ExpectFail(Action onError, Key? key = null) : StatefulWidget(key)
    {
        public Action OnError { get; } = onError;

        public override State CreateState() => new ExpectFailState();
    }

    private sealed class ExpectFailState : State<ExpectFail>
    {
        public override void InitState()
        {
            base.InitState();
            try
            {
                Context.DependOnInheritedWidgetOfExactType<TestInherited>(); // should fail
            }
            catch (Exception)
            {
                Widget.OnError();
            }
        }

        public override Widget Build(BuildContext context) => new Container();
    }

    private sealed class ChangeNotifierInherited(Widget child, ChangeNotifier? notifier = null, Key? key = null)
        : InheritedNotifier<ChangeNotifier>(notifier, child, key);

    private sealed class ThemedCard(Key? key = null)
        : SingleChildRenderObjectWidget(child: SizedBox.Expand(), key: key)
    {
        public override RenderObject CreateRenderObject(BuildContext context)
        {
            CardThemeData cardTheme = CardTheme.Of(context);

            return new RenderPhysicalShape(
                clipper: new ShapeBorderClipper(shape: cardTheme.Shape ?? new RoundedRectangleBorder()),
                clipBehavior: cardTheme.ClipBehavior ?? Clip.AntiAlias,
                color: cardTheme.Color ?? MaterialColors.White,
                elevation: cardTheme.Elevation ?? 0.0,
                shadowColor: cardTheme.ShadowColor ?? MaterialColors.Black);
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            CardThemeData cardTheme = CardTheme.Of(context);

            var shape = (RenderPhysicalShape)renderObject;
            shape.Clipper = new ShapeBorderClipper(shape: cardTheme.Shape ?? new RoundedRectangleBorder());
            shape.ClipBehavior = cardTheme.ClipBehavior ?? Clip.AntiAlias;
            shape.Color = cardTheme.Color ?? MaterialColors.White;
            shape.Elevation = cardTheme.Elevation ?? 0.0;
            shape.ShadowColor = cardTheme.ShadowColor ?? MaterialColors.Black;
        }
    }
}
