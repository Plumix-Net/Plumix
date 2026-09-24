using System.Text;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Xunit.Sdk;

// Ports flutter/packages/flutter/test/widgets/scrollable_semantics_test.dart and
// scrollable_semantics_traversal_order_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollSemanticsDartParityTests
{
    private const SemanticsActions ScrollUp = SemanticsActions.ScrollUp;
    private const SemanticsActions ScrollDown = SemanticsActions.ScrollDown;
    private const SemanticsActions ScrollToOffset = SemanticsActions.ScrollToOffset;
    private const SemanticsFlags IsHidden = SemanticsFlags.IsHidden;
    private const SemanticsFlags HasImplicitScrolling = SemanticsFlags.HasImplicitScrolling;

    // ---------------------------------------------------------------------------------------------
    // scrollable_semantics_test.dart
    // ---------------------------------------------------------------------------------------------

    // Flutter: 'scrollable_semantics_test.dart: scrollable exposes the correct semantic actions'
    [Fact]
    public void ScrollableExposesTheCorrectSemanticActions()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(children: GenerateTexts(80))));

        semantics.ExpectIncludesNodeWith(actions: ScrollUp | ScrollToOffset);

        FlingUp(tester);
        semantics.ExpectIncludesNodeWith(actions: ScrollUp | ScrollDown | ScrollToOffset);

        FlingDown(tester, repetitions: 2);
        semantics.ExpectIncludesNodeWith(actions: ScrollUp | ScrollToOffset);

        FlingUp(tester, repetitions: 5);
        semantics.ExpectIncludesNodeWith(actions: ScrollDown | ScrollToOffset);

        FlingDown(tester);
        semantics.ExpectIncludesNodeWith(actions: ScrollUp | ScrollDown | ScrollToOffset);
    }

    // Flutter: 'scrollable_semantics_test.dart: Vertical scrollable responds to scrollToOffset'
    [Fact]
    public void VerticalScrollableRespondsToScrollToOffset()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        var controller = new ScrollController();
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(controller: controller, children: GenerateTexts(60))));
        SemanticsOwner semanticsOwner = semantics.Owner;
        int scrollableId = semantics.NodesWith(actions: ScrollUp | ScrollToOffset).Single().Id;

        Assert.Equal(0.0, controller.Offset);
        semanticsOwner.PerformAction(scrollableId, SemanticsActions.ScrollToOffset, new Point(123.0, 456.0));
        Assert.Equal(456.0, controller.Offset);
        controller.Dispose();
    }

    // Flutter: 'scrollable_semantics_test.dart: Horizontal scrollable responds to scrollToOffset'
    [Fact]
    public void HorizontalScrollableRespondsToScrollToOffset()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        var controller = new ScrollController();
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(
                controller: controller,
                scrollDirection: Axis.Horizontal,
                children: GenerateTexts(60))));
        SemanticsOwner semanticsOwner = semantics.Owner;
        int scrollableId = semantics
            .NodesWith(actions: SemanticsActions.ScrollLeft | ScrollToOffset)
            .Single()
            .Id;

        Assert.Equal(0.0, controller.Offset);
        semanticsOwner.PerformAction(scrollableId, SemanticsActions.ScrollToOffset, new Point(123.0, 456.0));
        Assert.Equal(123.0, controller.Offset);
        controller.Dispose();
    }

    // Flutter: 'scrollable_semantics_test.dart: Unscrollable scrollable does not respond to scrollToOffset'
    [Fact]
    public void UnscrollableScrollableDoesNotRespondToScrollToOffset()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(children: GenerateTexts(3))));
        Assert.Empty(semantics.NodesWith(actions: ScrollUp | ScrollToOffset));
    }

    // Flutter: 'scrollable_semantics_test.dart: Scrollable exposes implicit scrolling before dimensions are available'
    [Fact]
    public void ScrollableExposesImplicitScrollingBeforeDimensionsAreAvailable()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        var controller = new NoDimensionsDuringSemanticsScrollController();
        try
        {
            PumpWidget(tester, new Directionality(
                TextDirection.Ltr,
                new ListView(controller: controller, children: GenerateTexts(60))));

            semantics.ExpectIncludesNodeWith(flags: HasImplicitScrolling);
            Assert.Empty(semantics.NodesWith(actions: ScrollUp | ScrollToOffset));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_test.dart: scrollToOffset respects implicit scrolling configuration'
    [Fact]
    public void ScrollToOffsetRespectsImplicitScrollingConfiguration()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);
        ScrollPhysics physics = new NoImplicitScrollingScrollPhysics();
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(physics: physics, children: GenerateTexts(60))));
        Assert.Empty(semantics.NodesWith(actions: ScrollUp | ScrollToOffset));
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen works in scrollable'
    [Fact]
    public void ShowOnScreenWorksInScrollable()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        const double kItemHeight = 40.0;

        List<Widget> containers = Enumerable.Range(0, 80)
            .Select(i => (Widget)new MergeSemantics(
                child: new SizedBox(
                    height: kItemHeight,
                    child: new Text($"container {i}", textDirection: TextDirection.Ltr))))
            .ToList();

        var scrollController = new ScrollController(initialScrollOffset: kItemHeight / 2);
        try
        {
            PumpWidget(tester, new Directionality(
                TextDirection.Ltr,
                new ListView(controller: scrollController, children: containers)));

            Assert.Equal(kItemHeight / 2, scrollController.Offset);

            int firstContainerId = DebugSemantics(tester, containers[0]).Id;
            semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
            Pump(tester);
            Pump(tester, TimeSpan.FromSeconds(5));

            Assert.Equal(0.0, scrollController.Offset);
        }
        finally
        {
            scrollController.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen works with pinned app bar and sliver list'
    [Fact]
    public void ShowOnScreenWorksWithPinnedAppBarAndSliverList()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        const double kItemHeight = 100.0;
        const double kExpandedAppBarHeight = 56.0;

        List<Widget> containers = Enumerable.Range(0, 80)
            .Select(i => (Widget)new MergeSemantics(
                child: new SizedBox(height: kItemHeight, child: new Text($"container {i}"))))
            .ToList();

        var scrollController = new ScrollController(initialScrollOffset: kItemHeight / 2);
        try
        {
            PumpWidget(tester, new Directionality(
                TextDirection.Ltr,
                new Localizations(
                    locale: new Locale("en", "us"),
                    delegates: [DefaultWidgetsLocalizations.Delegate, DefaultMaterialLocalizations.Delegate],
                    child: new MediaQuery(
                        data: new MediaQueryData(),
                        child: new Scrollable(
                            controller: scrollController,
                            viewportBuilder: (_, offset) => new Viewport(
                                offset: offset,
                                slivers:
                                [
                                    new SliverAppBar(
                                        pinned: true,
                                        expandedHeight: kExpandedAppBarHeight,
                                        flexibleSpace: new FlexibleSpaceBar(title: new Text("App Bar"))),
                                    SliverList.FromChildren(containers),
                                ]))))));

            Assert.Equal(kItemHeight / 2, scrollController.Offset);

            int firstContainerId = DebugSemantics(tester, containers[0]).Id;
            semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
            Pump(tester);
            Pump(tester, TimeSpan.FromSeconds(5));
            Assert.Equal(kExpandedAppBarHeight, tester.GetTopLeft(tester.ElementOfWidget(containers[0])).Y);
        }
        finally
        {
            scrollController.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen works with pinned app bar and individual slivers'
    [Fact]
    public void ShowOnScreenWorksWithPinnedAppBarAndIndividualSlivers()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        const double kItemHeight = 100.0;
        const double kExpandedAppBarHeight = 256.0;

        var children = new List<Widget>();
        List<Widget> slivers = Enumerable.Range(0, 30)
            .Select(i =>
            {
                Widget child = new MergeSemantics(child: new SizedBox(height: 72.0, child: new Text($"Item {i}")));
                children.Add(child);
                return (Widget)new SliverToBoxAdapter(child: child);
            })
            .ToList();

        var scrollController = new ScrollController(initialScrollOffset: 2.5 * kItemHeight);
        try
        {
            PumpWidget(tester, new Directionality(
                TextDirection.Ltr,
                new MediaQuery(
                    data: new MediaQueryData(),
                    child: new Localizations(
                        locale: new Locale("en", "us"),
                        delegates: [DefaultWidgetsLocalizations.Delegate, DefaultMaterialLocalizations.Delegate],
                        child: new Scrollable(
                            controller: scrollController,
                            viewportBuilder: (_, offset) => new Viewport(
                                offset: offset,
                                slivers:
                                [
                                    new SliverAppBar(
                                        pinned: true,
                                        expandedHeight: kExpandedAppBarHeight,
                                        flexibleSpace: new FlexibleSpaceBar(title: new Text("App Bar"))),
                                    .. slivers,
                                ]))))));

            Assert.Equal(2.5 * kItemHeight, scrollController.Offset);

            int id0 = DebugSemantics(tester, children[0]).Id;
            semantics.Owner.PerformAction(id0, SemanticsActions.ShowOnScreen);
            Pump(tester);
            Pump(tester, TimeSpan.FromSeconds(5));
            Assert.Equal(
                MaterialConstants.ToolbarHeight,
                tester.GetTopLeft(tester.ElementOfWidget(children[0])).Y);
        }
        finally
        {
            scrollController.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_test.dart: correct scrollProgress'
    [Fact]
    public void CorrectScrollProgress()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(children: GenerateTexts(80))));

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 0.0,
            scrollExtentMax: 520.0,
            actions: ScrollUp | ScrollToOffset);

        FlingUp(tester);

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 394.3,
            scrollExtentMax: 520.0,
            actions: ScrollUp | ScrollDown | ScrollToOffset);

        FlingUp(tester);

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 520.0,
            scrollExtentMax: 520.0,
            actions: ScrollDown | ScrollToOffset);
    }

    // Flutter: 'scrollable_semantics_test.dart: correct scrollProgress for unbound'
    [Fact]
    public void CorrectScrollProgressForUnbound()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            ListView.Builder(
                dragStartBehavior: DragStartBehavior.Down,
                itemExtent: 20.0,
                itemBuilder: (_, index) => new Text($"entry {index}"))));

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 0.0,
            scrollExtentMax: double.PositiveInfinity,
            actions: ScrollUp | ScrollToOffset);

        FlingUp(tester);

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 394.3,
            scrollExtentMax: double.PositiveInfinity,
            actions: ScrollUp | ScrollDown | ScrollToOffset);

        FlingUp(tester);

        semantics.ExpectIncludesNodeWith(
            scrollExtentMin: 0.0,
            scrollPosition: 788.6,
            scrollExtentMax: double.PositiveInfinity,
            actions: ScrollUp | ScrollDown | ScrollToOffset);
    }

    // Flutter: 'scrollable_semantics_test.dart: Semantics tree is populated mid-scroll'
    [Fact]
    public void SemanticsTreeIsPopulatedMidScroll()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        List<Widget> children = Enumerable.Range(0, 80)
            .Select(i => (Widget)new SizedBox(height: 40.0, child: new Text($"Item {i}")))
            .ToList();
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(children: children)));

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<ListView>()),
            PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(0.0, -40.0));
        Pump(tester);

        semantics.ExpectIncludesNodeWith(label: "Item 1");
        semantics.ExpectIncludesNodeWith(label: "Item 2");
        semantics.ExpectIncludesNodeWith(label: "Item 3");
    }

    // Flutter: 'scrollable_semantics_test.dart: Can toggle semantics on, off, on without crash'
    [Fact]
    public void CanToggleSemanticsOnOffOnWithoutCrash()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new ListView(
                children: Enumerable.Range(0, 40)
                    .Select(i => (Widget)new SizedBox(height: 400.0, child: new Text($"item {i}")))
                    .ToList())));

        TestSemantics expectedSemantics = TestSemantics.Root(
        [
            TestSemantics.RootChild(
            [
                new TestSemantics(
                    flags: HasImplicitScrolling,
                    actions: ScrollUp | ScrollToOffset,
                    children:
                    [
                        new TestSemantics(label: "item 0", textDirection: TextDirection.Ltr),
                        new TestSemantics(label: "item 1", textDirection: TextDirection.Ltr),
                        new TestSemantics(flags: IsHidden, label: "item 2"),
                    ]),
            ]),
        ]);

        // Start with semantics off.
        Assert.Null(ViewPipelineOwner(tester).SemanticsOwner);

        // Semantics on
        var semantics = new SemanticsTester(tester);
        PumpAndSettle(tester);
        Assert.NotNull(ViewPipelineOwner(tester).SemanticsOwner);
        semantics.ExpectHasSemantics(expectedSemantics);

        // Semantics off
        semantics.Dispose();
        PumpAndSettle(tester);
        Assert.Null(ViewPipelineOwner(tester).SemanticsOwner);

        // Semantics on
        semantics = new SemanticsTester(tester);
        PumpAndSettle(tester);
        Assert.NotNull(ViewPipelineOwner(tester).SemanticsOwner);
        semantics.ExpectHasSemantics(expectedSemantics);

        semantics.Dispose();
    }

    // group('showOnScreen'): the shared setUp.
    private sealed class ShowOnScreenGroup
    {
        public const double KItemHeight = 100.0;

        public ShowOnScreenGroup()
        {
            Children = Enumerable.Range(0, 10)
                .Select(i => (Widget)new MergeSemantics(
                    child: new SizedBox(height: KItemHeight, child: new Text($"container {i}"))))
                .ToList();

            ScrollController = new ScrollController(initialScrollOffset: KItemHeight / 2);

            WidgetUnderTest = new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        height: 2 * KItemHeight,
                        child: new ListView(controller: ScrollController, children: Children))));
        }

        public List<Widget> Children { get; }

        public ScrollController ScrollController { get; }

        public Widget WidgetUnderTest { get; }
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen brings item above leading edge to leading edge'
    [Fact]
    public void ShowOnScreenBringsItemAboveLeadingEdgeToLeadingEdge()
    {
        var group = new ShowOnScreenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(ShowOnScreenGroup.KItemHeight / 2, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, group.Children[0]).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(0.0, group.ScrollController.Offset);
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen brings item below trailing edge to trailing edge'
    [Fact]
    public void ShowOnScreenBringsItemBelowTrailingEdgeToTrailingEdge()
    {
        var group = new ShowOnScreenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(ShowOnScreenGroup.KItemHeight / 2, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, group.Children[2]).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(ShowOnScreenGroup.KItemHeight, group.ScrollController.Offset);
    }

    // Flutter: 'scrollable_semantics_test.dart: showOnScreen does not change position of items already fully on-screen'
    [Fact]
    public void ShowOnScreenDoesNotChangePositionOfItemsAlreadyFullyOnScreen()
    {
        var group = new ShowOnScreenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(ShowOnScreenGroup.KItemHeight / 2, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, group.Children[1]).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(ShowOnScreenGroup.KItemHeight / 2, group.ScrollController.Offset);
    }

    // group('showOnScreen with negative children'): the shared setUp.
    private sealed class NegativeChildrenGroup : IDisposable
    {
        public const double KItemHeight = 100.0;

        public NegativeChildrenGroup()
        {
            Key center = new LabeledGlobalKey<State>(null);

            Children = Enumerable.Range(0, 10)
                .Select(i => (Widget)new SliverToBoxAdapter(
                    key: i == 5 ? center : null,
                    child: new MergeSemantics(
                        key: new ValueKey<int>(i),
                        child: new SizedBox(height: KItemHeight, child: new Text($"container {i}")))))
                .ToList();

            ScrollController = new ScrollController(initialScrollOffset: -2.5 * KItemHeight);

            // 'container 0' is at offset -500
            // 'container 1' is at offset -400
            // 'container 2' is at offset -300
            // 'container 3' is at offset -200
            // 'container 4' is at offset -100
            // 'container 5' is at offset 0

            WidgetUnderTest = new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        height: 2 * KItemHeight,
                        child: new Scrollable(
                            controller: ScrollController,
                            viewportBuilder: (_, offset) => new Viewport(
                                scrollCacheExtent: ScrollCacheExtent.Pixels(0.0),
                                offset: offset,
                                center: center,
                                slivers: Children)))));
        }

        public List<Widget> Children { get; }

        public ScrollController ScrollController { get; }

        public Widget WidgetUnderTest { get; }

        // tearDown
        public void Dispose() => ScrollController.Dispose();
    }

    // Flutter: 'scrollable_semantics_test.dart:
    //   showOnScreen with negative children brings item above leading edge to leading edge'
    [Fact]
    public void ShowOnScreenWithNegativeChildrenBringsItemAboveLeadingEdgeToLeadingEdge()
    {
        using var group = new NegativeChildrenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(-250.0, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, new ValueKey<int>(2)).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(-300.0, group.ScrollController.Offset);
    }

    // Flutter: 'scrollable_semantics_test.dart:
    //   showOnScreen with negative children brings item below trailing edge to trailing edge'
    [Fact]
    public void ShowOnScreenWithNegativeChildrenBringsItemBelowTrailingEdgeToTrailingEdge()
    {
        using var group = new NegativeChildrenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(-250.0, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, new ValueKey<int>(4)).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(-200.0, group.ScrollController.Offset);
    }

    // Flutter: 'scrollable_semantics_test.dart:
    //   showOnScreen with negative children does not change position of items already fully on-screen'
    [Fact]
    public void ShowOnScreenWithNegativeChildrenDoesNotChangePositionOfItemsAlreadyFullyOnScreen()
    {
        using var group = new NegativeChildrenGroup();
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        PumpWidget(tester, group.WidgetUnderTest);

        Assert.Equal(-250.0, group.ScrollController.Offset);

        int firstContainerId = DebugSemantics(tester, new ValueKey<int>(3)).Id;
        semantics.Owner.PerformAction(firstContainerId, SemanticsActions.ShowOnScreen);
        PumpAndSettle(tester);

        Assert.Equal(-250.0, group.ScrollController.Offset);
    }

    // Flutter: 'scrollable_semantics_test.dart:
    //   transform of inner node from useTwoPaneSemantics scrolls correctly with nested scrollables'
    [Fact]
    public void TransformOfInnerNodeFromUseTwoPaneSemanticsScrollsCorrectlyWithNestedScrollables()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester); // enables semantics tree generation

        // Context: https://github.com/flutter/flutter/issues/61631
        PumpWidget(tester, new Directionality(
            TextDirection.Ltr,
            new SingleChildScrollView(
                child: new ListView(shrinkWrap: true, children: GenerateTexts(50)))));

        SemanticsNode rootScrollNode = semantics.NodesWith(actions: ScrollUp | ScrollToOffset).Single();
        SemanticsNode innerListPane = semantics.NodesWith(ancestor: rootScrollNode, scrollExtentMax: 0).Single();
        SemanticsNode outerListPane = innerListPane.Parent!;
        List<SemanticsNode> hiddenNodes = semantics.NodesWith(flags: IsHidden).ToList();

        // This test is only valid if some children are offscreen.
        // Increase the number of Text children if this assert fails.
        Assert.True(hiddenNodes.Count >= 3, semantics.Dump);

        // Scroll to end -> beginning -> middle to test both directions.
        var targetNodes = new List<SemanticsNode>
        {
            hiddenNodes[^1],
            hiddenNodes[0],
            hiddenNodes[hiddenNodes.Count / 2],
        };

        Assert.Equal(NodeGlobalRect(outerListPane), NodeGlobalRect(innerListPane));

        foreach (SemanticsNode node in targetNodes)
        {
            semantics.Owner.PerformAction(node.Id, SemanticsActions.ShowOnScreen);
            PumpAndSettle(tester);

            Assert.Equal(NodeGlobalRect(outerListPane), NodeGlobalRect(innerListPane));
        }
    }

    // ---------------------------------------------------------------------------------------------
    // scrollable_semantics_traversal_order_test.dart
    // ---------------------------------------------------------------------------------------------

    // The rows every traversal test but the grid and the center-child one builds.
    private static List<Widget> TwoLabelRows()
    {
        return Enumerable.Range(0, 30)
            .Select(i => (Widget)new SizedBox(
                height: 200.0,
                child: new Row(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children:
                    [
                        new Semantics(container: true, child: new Text($"Item {i}a")),
                        new Semantics(container: true, child: new Text($"item {i}b")),
                    ])))
            .ToList();
    }

    private static Widget TraversalHost(Widget scrollView)
    {
        return new Semantics(
            textDirection: TextDirection.Ltr,
            child: new Directionality(
                TextDirection.Ltr,
                new MediaQuery(data: new MediaQueryData(), child: scrollView)));
    }

    private static TestSemantics Leaf(string label, bool hidden = false) =>
        new(flags: hidden ? IsHidden : SemanticsFlags.None, label: label, textDirection: TextDirection.Ltr);

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order of SliverList'
    [Fact]
    public void TraversalOrderOfSliverList()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        var controller = new ScrollController(initialScrollOffset: 3000.0);
        try
        {
            List<Widget> listChildren = TwoLabelRows();
            PumpWidget(tester, TraversalHost(new CustomScrollView(
                controller: controller,
                semanticChildCount: 30,
                slivers: [SliverList.FromChildren(listChildren)])));

            TestSemantics Row(int i, bool hidden) => new(
                flags: hidden ? IsHidden : SemanticsFlags.None,
                children: [Leaf($"Item {i}a", hidden), Leaf($"item {i}b", hidden)]);

            semantics.ExpectHasSemantics(TestSemantics.Root(
            [
                new TestSemantics(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new TestSemantics(
                            children:
                            [
                                new TestSemantics(
                                    scrollIndex: 15,
                                    scrollChildren: 30,
                                    flags: HasImplicitScrolling,
                                    actions: ScrollUp | ScrollDown | ScrollToOffset,
                                    children:
                                    [
                                        Row(13, hidden: true),
                                        Row(14, hidden: true),
                                        Row(15, hidden: false),
                                        Row(16, hidden: false),
                                        Row(17, hidden: false),
                                        Row(18, hidden: true),
                                        Row(19, hidden: true),
                                    ]),
                            ]),
                    ]),
            ]));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order of SliverFixedExtentList'
    [Fact]
    public void TraversalOrderOfSliverFixedExtentList()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        var controller = new ScrollController(initialScrollOffset: 3000.0);
        try
        {
            List<Widget> listChildren = TwoLabelRows();
            PumpWidget(tester, TraversalHost(new CustomScrollView(
                controller: controller,
                slivers:
                [
                    SliverFixedExtentList.FromChildren(
                        listChildren,
                        itemExtent: 200.0,
                        addSemanticIndexes: false),
                ])));

            semantics.ExpectHasSemantics(TestSemantics.Root(
            [
                new TestSemantics(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new TestSemantics(
                            children:
                            [
                                new TestSemantics(
                                    flags: HasImplicitScrolling,
                                    actions: ScrollUp | ScrollDown | ScrollToOffset,
                                    children: FlatRows(i => $"Item {i}a", i => $"item {i}b", 13, 19, 15, 17)),
                            ]),
                    ]),
            ]));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order of SliverGrid'
    [Fact]
    public void TraversalOrderOfSliverGrid()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        var controller = new ScrollController(initialScrollOffset: 1600.0);
        try
        {
            List<Widget> listChildren = Enumerable.Range(0, 30)
                .Select(i => (Widget)new SizedBox(height: 200.0, child: new Text($"Item {i}")))
                .ToList();
            PumpWidget(tester, TraversalHost(new CustomScrollView(
                controller: controller,
                slivers:
                [
                    SliverGrid.Count(
                        crossAxisCount: 2,
                        crossAxisSpacing: 400.0,
                        children: listChildren),
                ])));

            var gridChildren = new List<TestSemantics>();
            for (int i = 12; i <= 25; i += 1)
            {
                gridChildren.Add(Leaf($"Item {i}", hidden: i < 16 || i > 21));
            }

            semantics.ExpectHasSemantics(TestSemantics.Root(
            [
                new TestSemantics(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new TestSemantics(
                            children:
                            [
                                new TestSemantics(
                                    flags: HasImplicitScrolling,
                                    actions: ScrollUp | ScrollDown | ScrollToOffset,
                                    children: gridChildren),
                            ]),
                    ]),
            ]));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order of List of individual slivers'
    [Fact]
    public void TraversalOrderOfListOfIndividualSlivers()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        var controller = new ScrollController(initialScrollOffset: 3000.0);
        try
        {
            List<Widget> listChildren = TwoLabelRows()
                .Select(row => (Widget)new SliverToBoxAdapter(child: row))
                .ToList();
            PumpWidget(tester, TraversalHost(new CustomScrollView(controller: controller, slivers: listChildren)));

            semantics.ExpectHasSemantics(TestSemantics.Root(
            [
                new TestSemantics(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new TestSemantics(
                            children:
                            [
                                new TestSemantics(
                                    flags: HasImplicitScrolling,
                                    actions: ScrollUp | ScrollDown | ScrollToOffset,
                                    children: FlatRows(i => $"Item {i}a", i => $"item {i}b", 13, 19, 15, 17)),
                            ]),
                    ]),
            ]));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order of in a SingleChildScrollView'
    [Fact]
    public void TraversalOrderOfInASingleChildScrollView()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        var controller = new ScrollController(initialScrollOffset: 3000.0);
        try
        {
            List<Widget> listChildren = TwoLabelRows();
            PumpWidget(tester, TraversalHost(new SingleChildScrollView(
                controller: controller,
                child: new Column(children: listChildren))));

            List<TestSemantics> children = FlatRows(i => $"Item {i}a", i => $"item {i}b", 0, 29, 15, 17);

            semantics.ExpectHasSemantics(TestSemantics.Root(
            [
                new TestSemantics(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new TestSemantics(
                            flags: HasImplicitScrolling,
                            actions: ScrollUp | ScrollDown | ScrollToOffset,
                            children: children),
                    ]),
            ]));
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: 'scrollable_semantics_traversal_order_test.dart: Traversal Order with center child'
    [Fact]
    public void TraversalOrderWithCenterChild()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var semantics = new SemanticsTester(tester);

        PumpWidget(tester, TraversalHost(new Scrollable(
            viewportBuilder: (_, offset) => new Viewport(
                offset: offset,
                center: new ValueKey<int>(0),
                slivers: Enumerable.Range(0, 30)
                    .Select(i =>
                    {
                        int item = i - 15;
                        return (Widget)new SliverToBoxAdapter(
                            key: new ValueKey<int>(item),
                            child: new SizedBox(
                                height: 200.0,
                                child: new Row(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    children:
                                    [
                                        new Semantics(container: true, child: new Text($"{item}a")),
                                        new Semantics(container: true, child: new Text($"{item}b")),
                                    ])));
                    })
                    .ToList()))));

        semantics.ExpectHasSemantics(TestSemantics.Root(
        [
            new TestSemantics(
                textDirection: TextDirection.Ltr,
                children:
                [
                    new TestSemantics(
                        children:
                        [
                            new TestSemantics(
                                flags: HasImplicitScrolling,
                                actions: ScrollUp | ScrollDown | ScrollToOffset,
                                children: FlatRows(i => $"{i}a", i => $"{i}b", -2, 4, 0, 2)),
                        ]),
                ]),
        ]));
    }

    /// <summary>Two leaves per index in <c>[first, last]</c>, hidden outside <c>[firstShown, lastShown]</c>.</summary>
    private static List<TestSemantics> FlatRows(
        Func<int, string> a,
        Func<int, string> b,
        int first,
        int last,
        int firstShown,
        int lastShown)
    {
        var children = new List<TestSemantics>();
        for (int index = first; index <= last; index += 1)
        {
            bool isHidden = index < firstShown || index > lastShown;
            children.Add(Leaf(a(index), isHidden));
            children.Add(Leaf(b(index), isHidden));
        }

        return children;
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers (flutter_test / semantics_tester.dart subset)
    // ---------------------------------------------------------------------------------------------

    private static List<Widget> GenerateTexts(int count) =>
        Enumerable.Range(0, count).Select(i => (Widget)new Text($"{i}")).ToList();

    private static PipelineOwner ViewPipelineOwner(FrameworkDartTester tester) => tester.RenderView.Owner!;

    // flutter_test's frame runs `flushSemantics` after compositing; the C# tester's frame stops at
    // composite, so every pump here finishes with the view's semantics flush.
    private static void FlushSemantics(FrameworkDartTester tester) => ViewPipelineOwner(tester).FlushSemantics();

    private static void PumpWidget(FrameworkDartTester tester, Widget widget)
    {
        tester.PumpWidget(widget);
        FlushSemantics(tester);
    }

    private static void Pump(FrameworkDartTester tester, TimeSpan? duration = null)
    {
        tester.Pump(duration);
        FlushSemantics(tester);
    }

    private static void PumpAndSettle(FrameworkDartTester tester)
    {
        tester.PumpAndSettle();
        FlushSemantics(tester);
    }

    private static void FlingUp(FrameworkDartTester tester, int repetitions = 1) =>
        Fling(tester, new Vector(0.0, -200.0), repetitions);

    private static void FlingDown(FrameworkDartTester tester, int repetitions = 1) =>
        Fling(tester, new Vector(0.0, 200.0), repetitions);

    private static void Fling(FrameworkDartTester tester, Vector offset, int repetitions)
    {
        while (repetitions-- > 0)
        {
            tester.Fling(tester.ElementOfType<ListView>(), offset, 1000.0);
            Pump(tester);
            Pump(tester, TimeSpan.FromSeconds(5));
        }
    }

    // Dart's `tester.renderObject(find.byWidget(widget)).debugSemantics!`.
    private static SemanticsNode DebugSemantics(FrameworkDartTester tester, Widget widget) =>
        tester.ElementOfWidget(widget).FindRenderObject()!.SemanticsNode!;

    // Dart's `tester.renderObject(find.byKey(key)).debugSemantics!`.
    private static SemanticsNode DebugSemantics(FrameworkDartTester tester, Key key) =>
        tester.ElementsWithKey(key).Single().FindRenderObject()!.SemanticsNode!;

    private static Rect NodeGlobalRect(SemanticsNode node)
    {
        Matrix4 globalTransform = node.Transform ?? Matrix4.Identity();
        for (SemanticsNode? parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.Transform != null)
            {
                globalTransform = parent.Transform.Multiplied(globalTransform);
            }
        }

        return MatrixUtils.TransformRect(globalTransform, node.Rect);
    }

    // Dart's `nearEqual(a, b, epsilon)` for nullable values.
    private static bool NearEqual(double? a, double b, double epsilon)
    {
        if (a is not { } value)
        {
            return false;
        }

        return (value > b - epsilon && value < b + epsilon) || value == b;
    }

    /// <summary>semantics_tester.dart's <c>SemanticsTester</c>: keeps semantics on for the view.</summary>
    private sealed class SemanticsTester : IDisposable
    {
        private readonly FrameworkDartTester _tester;
        private SemanticsHandle? _handle;

        public SemanticsTester(FrameworkDartTester tester)
        {
            _tester = tester;
            PipelineOwner owner = ViewPipelineOwner(tester);
            bool createsOwner = owner.SemanticsOwner is null;
            _handle = owner.EnsureSemantics();
            if (createsOwner)
            {
                // flutter_test's view is the binding's `_ReusableRenderView` (`ReusableRenderView`
                // here), whose `scheduleInitialSemantics` clears every cached configuration first; the
                // C# tester mounts a plain `RenderView`, so do the same clear here.
                tester.RenderView.ClearSemantics();
                tester.RenderView.ScheduleInitialSemantics();
            }
        }

        public SemanticsOwner Owner => ViewPipelineOwner(_tester).SemanticsOwner!;

        public string Dump => Owner.DebugDumpTree();

        public List<SemanticsNode> NodesWith(
            string? label = null,
            SemanticsActions? actions = null,
            SemanticsFlags? flags = null,
            double? scrollPosition = null,
            double? scrollExtentMax = null,
            double? scrollExtentMin = null,
            SemanticsNode? ancestor = null)
        {
            bool CheckNode(SemanticsNode node)
            {
                SemanticsData data = node.GetSemanticsData();
                if (label != null && node.Label != label)
                {
                    return false;
                }

                if (actions is { } expectedActions && data.Actions != expectedActions)
                {
                    return false;
                }

                if (flags is { } expectedFlags && data.Flags != expectedFlags)
                {
                    return false;
                }

                if (scrollPosition is { } position && !NearEqual(node.ScrollPosition, position, 0.1))
                {
                    return false;
                }

                if (scrollExtentMax is { } max && !NearEqual(node.ScrollExtentMax, max, 0.1))
                {
                    return false;
                }

                if (scrollExtentMin is { } min && !NearEqual(node.ScrollExtentMin, min, 0.1))
                {
                    return false;
                }

                return true;
            }

            var result = new List<SemanticsNode>();
            void Visit(SemanticsNode node)
            {
                if (CheckNode(node))
                {
                    result.Add(node);
                }

                foreach (SemanticsNode child in node.Children)
                {
                    Visit(child);
                }
            }

            Visit(ancestor ?? Owner.RootNode!);
            return result;
        }

        /// <summary>Dart's <c>expect(semantics, includesNodeWith(...))</c>.</summary>
        public void ExpectIncludesNodeWith(
            string? label = null,
            SemanticsActions? actions = null,
            SemanticsFlags? flags = null,
            double? scrollPosition = null,
            double? scrollExtentMax = null,
            double? scrollExtentMin = null)
        {
            List<SemanticsNode> nodes = NodesWith(
                label: label,
                actions: actions,
                flags: flags,
                scrollPosition: scrollPosition,
                scrollExtentMax: scrollExtentMax,
                scrollExtentMin: scrollExtentMin);
            if (nodes.Count == 0)
            {
                throw new XunitException(
                    $"Expected a node with label={label} actions={actions} flags={flags} "
                    + $"scrollPosition={scrollPosition} scrollExtentMax={scrollExtentMax} "
                    + $"scrollExtentMin={scrollExtentMin}.\n{Dump}");
            }
        }

        /// <summary>
        /// Dart's <c>expect(semantics, hasSemantics(expected, ignoreId: true, ignoreRect: true,
        /// ignoreTransform: true))</c> in inverse-hit-test child order.
        /// </summary>
        public void ExpectHasSemantics(TestSemantics expected)
        {
            string? failure = expected.Match(Owner.RootNode!, "root");
            if (failure != null)
            {
                throw new XunitException($"{failure}\n{Dump}");
            }
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }

    /// <summary>
    /// semantics_tester.dart's <c>TestSemantics</c>, reduced to what these tests compare (ids, rects
    /// and transforms are always ignored).
    /// </summary>
    private sealed class TestSemantics(
        SemanticsFlags flags = SemanticsFlags.None,
        SemanticsActions actions = SemanticsActions.None,
        string label = "",
        TextDirection? textDirection = null,
        int? scrollIndex = null,
        int? scrollChildren = null,
        IReadOnlyList<TestSemantics>? children = null)
    {
        private readonly IReadOnlyList<TestSemantics> _children = children ?? [];

        public static TestSemantics Root(IReadOnlyList<TestSemantics> children) => new(children: children);

        public static TestSemantics RootChild(IReadOnlyList<TestSemantics> children) => new(children: children);

        public string? Match(SemanticsNode node, string path)
        {
            SemanticsData data = node.GetSemanticsData();
            var errors = new StringBuilder();
            if (data.Flags != flags)
            {
                errors.Append($" flags: expected {flags} but found {data.Flags};");
            }

            if (data.Actions != actions)
            {
                errors.Append($" actions: expected {actions} but found {data.Actions};");
            }

            if (data.Label != label)
            {
                errors.Append($" label: expected \"{label}\" but found \"{data.Label}\";");
            }

            if (data.Value != string.Empty || data.Hint != string.Empty || data.Tooltip != string.Empty)
            {
                errors.Append(" expected no value/hint/tooltip;");
            }

            if (textDirection != null && textDirection != data.TextDirection)
            {
                errors.Append($" textDirection: expected {textDirection} but found {data.TextDirection};");
            }

            if ((data.Label != string.Empty || data.Value != string.Empty || data.Hint != string.Empty)
                && data.TextDirection == null)
            {
                errors.Append(" a node with a label, value, or hint must have a textDirection;");
            }

            if (scrollIndex != null && scrollIndex != data.ScrollIndex)
            {
                errors.Append($" scrollIndex: expected {scrollIndex} but found {data.ScrollIndex};");
            }

            if (scrollChildren != null && scrollChildren != data.ScrollChildCount)
            {
                errors.Append($" scrollChildren: expected {scrollChildren} but found {data.ScrollChildCount};");
            }

            if (data.Role != SemanticsRole.None)
            {
                errors.Append($" role: expected None but found {data.Role};");
            }

            int childrenCount = node.MergeAllDescendantsIntoThisNode ? 0 : node.Children.Count;
            if (_children.Count != childrenCount)
            {
                errors.Append($" expected {_children.Count} children but found {childrenCount};");
            }

            if (errors.Length > 0)
            {
                return $"Node #{node.Id} at {path}:{errors}";
            }

            for (int i = 0; i < _children.Count; i += 1)
            {
                string? childFailure = _children[i].Match(node.Children[i], $"{path}/{i}");
                if (childFailure != null)
                {
                    return childFailure;
                }
            }

            return null;
        }
    }

    // scrollable_semantics_test.dart: _NoImplicitScrollingScrollPhysics.
    private sealed class NoImplicitScrollingScrollPhysics : ScrollPhysics
    {
        public override bool AllowImplicitScrolling => false;

        public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) => this;
    }

    // scrollable_semantics_test.dart: _NoDimensionsDuringSemanticsScrollController.
    private sealed class NoDimensionsDuringSemanticsScrollController : ScrollController
    {
        public override ScrollPosition CreateScrollPosition(
            ScrollPhysics physics,
            IScrollContext context,
            ScrollPosition? oldPosition)
        {
            return new NoDimensionsDuringSemanticsScrollPosition(
                physics: physics,
                context: context,
                oldPosition: oldPosition,
                initialPixels: InitialScrollOffset,
                keepScrollOffset: KeepScrollOffset,
                debugLabel: DebugLabel);
        }
    }

    // scrollable_semantics_test.dart: _NoDimensionsDuringSemanticsScrollPosition.
    private sealed class NoDimensionsDuringSemanticsScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition,
        double? initialPixels,
        bool keepScrollOffset,
        string? debugLabel) : ScrollPositionWithSingleContext(
        physics: physics,
        context: context,
        initialPixels: initialPixels,
        keepScrollOffset: keepScrollOffset,
        oldPosition: oldPosition,
        debugLabel: debugLabel)
    {
        private bool _useRealDimensionsForLayout;

        public override bool HaveDimensions => _useRealDimensionsForLayout && base.HaveDimensions;

        public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
        {
            // Let ScrollPosition update its layout state normally, then hide dimensions
            // again so semantics sees the transient no-dimensions state.
            _useRealDimensionsForLayout = true;
            bool result = base.ApplyContentDimensions(minScrollExtent, maxScrollExtent);
            _useRealDimensionsForLayout = false;
            return result;
        }
    }
}
