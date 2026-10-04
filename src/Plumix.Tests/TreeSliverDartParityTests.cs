using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/sliver_tree_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TreeSliverDartParityTests
{
    // widgets/sliver_tree_test.dart: simpleNodeSet.
    private static List<TreeSliverNode<string>> SimpleNodeSet() =>
    [
        new("Root 0"),
        new("Root 1", expanded: true, children: [new("Child 1:0"), new("Child 1:1")]),
        new("Root 2", children: [new("Child 2:0"), new("Child 2:1")]),
        new("Root 3"),
    ];

    private static Widget Wrap(Widget sliver) =>
        new Directionality(TextDirection.Ltr, new CustomScrollView(slivers: [sliver]));

    // ------------------------------------------------------------------ widgets/sliver_tree_test.dart

    // Flutter: 'sliver_tree_test.dart: TreeSliverNode getters, toString'
    [Fact]
    public void TreeSliverNodeGettersToString()
    {
        List<TreeSliverNode<string>> children = [new("child")];
        var node = new TreeSliverNode<string>("parent", children: children, expanded: true);
        Assert.Equal("parent", node.Content);
        Assert.Same(children, node.Children);
        Assert.True(node.IsExpanded);
        Assert.Equal("child", node.Children[0].Content);
        Assert.Empty(node.Children[0].Children);
        Assert.False(node.Children[0].IsExpanded);
        Assert.Null(node.Depth);
        Assert.Null(node.Parent);
        Assert.Null(node.Children[0].Depth);
        Assert.Null(node.Children[0].Parent);

        Assert.Equal("TreeSliverNode: parent, depth: null, parent, expanded: true", node.ToString());
        Assert.Equal("TreeSliverNode: child, depth: null, leaf", node.Children[0].ToString());
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliverNode sets ups parent and depth properties'
    [Fact]
    public void TreeSliverNodeSetsUpParentAndDepthProperties()
    {
        using var tester = new FrameworkDartTester();
        var node = new TreeSliverNode<string>("parent", children: [new("child")], expanded: true);
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: [node])));

        Assert.Equal(0, node.Depth);
        Assert.Null(node.Parent);
        Assert.Equal(1, node.Children[0].Depth);
        Assert.Same(node, node.Children[0].Parent);

        Assert.Equal("TreeSliverNode: parent, depth: root, parent, expanded: true", node.ToString());
        Assert.Equal("TreeSliverNode: child, depth: 1, leaf", node.Children[0].ToString());
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can set controller on TreeSliver'
    [Fact]
    public void CanSetControllerOnTreeSliver()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TreeSliverController();
        TreeSliverController? returnedController = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: SimpleNodeSet(),
            controller: controller,
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                returnedController ??= TreeSliverController.Of(context);
                return TreeSliver.DefaultTreeNodeBuilder(context, node, toggleAnimationStyle);
            })));

        Assert.Same(controller, returnedController);
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can get default controller on TreeSliver'
    [Fact]
    public void CanGetDefaultControllerOnTreeSliver()
    {
        using var tester = new FrameworkDartTester();
        TreeSliverController? returnedController = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: SimpleNodeSet(),
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                returnedController ??= TreeSliverController.MaybeOf(context);
                return TreeSliver.DefaultTreeNodeBuilder(context, node, toggleAnimationStyle);
            })));

        Assert.NotNull(returnedController);
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can get node for TreeSliverNode.content'
    [Fact]
    public void CanGetNodeForContent()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        Assert.Same(simpleNodeSet[0], controller.GetNodeFor("Root 0"));
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can get isExpanded for a node'
    [Fact]
    public void CanGetIsExpandedForANode()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        Assert.False(controller.IsExpanded(simpleNodeSet[0]));
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can get isActive for a node'
    [Fact]
    public void CanGetIsActiveForANode()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        Assert.True(controller.IsActive(simpleNodeSet[0]));
        Assert.True(controller.IsActive(simpleNodeSet[1]));
        // The parent 'Root 2' is not expanded, so its children are not active.
        Assert.False(controller.IsExpanded(simpleNodeSet[2]));
        Assert.False(controller.IsActive(simpleNodeSet[2].Children[0]));
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can toggleNode, to collapse or expand'
    [Fact]
    public void CanToggleNodeToCollapseOrExpand()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        // The parent 'Root 2' is not expanded, so its children are not active.
        Assert.False(controller.IsExpanded(simpleNodeSet[2]));
        Assert.False(controller.IsActive(simpleNodeSet[2].Children[0]));
        // Toggle 'Root 2' to expand it
        controller.ToggleNode(simpleNodeSet[2]);
        Assert.True(controller.IsExpanded(simpleNodeSet[2]));
        Assert.True(controller.IsActive(simpleNodeSet[2].Children[0]));

        // The parent 'Root 1' is expanded, so its children are active.
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        Assert.True(controller.IsActive(simpleNodeSet[1].Children[0]));
        // Toggle 'Root 1' to collapse it
        controller.ToggleNode(simpleNodeSet[1]);
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        // Nodes are not removed from the active list until the collapse animation completes.
        Assert.True(controller.IsActive(simpleNodeSet[1].Children[0]));
        tester.PumpAndSettle();
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        Assert.False(controller.IsActive(simpleNodeSet[1].Children[0]));
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can expandNode, then collapseAll'
    [Fact]
    public void CanExpandNodeThenCollapseAll()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        // The parent 'Root 2' is not expanded, so its children are not active.
        Assert.False(controller.IsExpanded(simpleNodeSet[2]));
        Assert.False(controller.IsActive(simpleNodeSet[2].Children[0]));
        // Expand 'Root 2'
        controller.ExpandNode(simpleNodeSet[2]);
        Assert.True(controller.IsExpanded(simpleNodeSet[2]));
        Assert.True(controller.IsActive(simpleNodeSet[2].Children[0]));

        // Both parents from our simple node set are expanded.
        // 'Root 1'
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        // 'Root 2'
        Assert.True(controller.IsExpanded(simpleNodeSet[2]));
        // Collapse both.
        controller.CollapseAll();
        tester.PumpAndSettle();
        // Both parents from our simple node set have collapsed.
        // 'Root 1'
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        // 'Root 2'
        Assert.False(controller.IsExpanded(simpleNodeSet[2]));
    }

    // Flutter: 'sliver_tree_test.dart: TreeController Can collapseNode, then expandAll'
    [Fact]
    public void CanCollapseNodeThenExpandAll()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, controller: controller)));

        // The parent 'Root 1' is expanded, so its children are active.
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        Assert.True(controller.IsActive(simpleNodeSet[1].Children[0]));
        // Collapse 'Root 1'
        controller.CollapseNode(simpleNodeSet[1]);
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        // Nodes are not removed from the active list until the collapse animation completes.
        Assert.True(controller.IsActive(simpleNodeSet[1].Children[0]));
        tester.PumpAndSettle();
        Assert.False(controller.IsActive(simpleNodeSet[1].Children[0]));

        // Both parents from our simple node set are collapsed.
        // 'Root 1'
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        // 'Root 2'
        Assert.False(controller.IsExpanded(simpleNodeSet[2]));
        // Expand both.
        controller.ExpandAll();
        // Both parents from our simple node set are expanded.
        // 'Root 1'
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        // 'Root 2'
        Assert.True(controller.IsExpanded(simpleNodeSet[2]));
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliverIndentationType values are properly reflected'
    [Fact]
    public void TreeSliverIndentationTypeValuesAreProperlyReflected()
    {
        Assert.Equal(10.0, TreeSliverIndentationType.Standard.Value);
        Assert.Equal(0.0, TreeSliverIndentationType.None.Value);
        Assert.Equal(50.0, TreeSliverIndentationType.Custom(50.0).Value);
    }

    // Flutter: 'sliver_tree_test.dart: .toggleNodeWith, onNodeToggle'
    [Fact]
    public void ToggleNodeWithOnNodeToggle()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        // The default node builder wraps the leading icon with toggleNodeWith.
        bool toggled = false;
        TreeSliverNode? toggledNode = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: simpleNodeSet,
            controller: controller,
            onNodeToggle: node =>
            {
                toggled = true;
                toggledNode = node;
            })));
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        tester.Tap(Find.ByType<Icon>().First);
        tester.Pump();
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        Assert.True(toggled);
        Assert.Same(simpleNodeSet[1], toggledNode);
        tester.PumpAndSettle();
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        toggled = false;
        toggledNode = null;

        // Use toggleNodeWith to make the whole row trigger the node state.
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: simpleNodeSet,
            controller: controller,
            onNodeToggle: node =>
            {
                toggled = true;
                toggledNode = node;
            },
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                TimeSpan animationDuration = toggleAnimationStyle.Duration ?? TreeSliver.DefaultAnimationDuration;
                Curve animationCurve = toggleAnimationStyle.Curve ?? TreeSliver.DefaultAnimationCurve;
                // This makes the whole row trigger toggling.
                return TreeSliver.WrapChildToToggleNode(
                    node,
                    new Padding(
                        EdgeInsets.All(8.0),
                        new Row(
                        [
                            // Icon for parent nodes
                            SizedBox.Square(
                                dimension: 30.0,
                                child: node.Children.Count > 0
                                    ? new AnimatedRotation(
                                        turns: node.IsExpanded ? 0.25 : 0.0,
                                        duration: animationDuration,
                                        curve: animationCurve,
                                        child: new Icon(new IconData(0x25BA), size: 14))
                                    : null),
                            // Spacer
                            new SizedBox(width: 8.0),
                            // Content
                            new Text(node.Content!.ToString()!),
                        ])));
            })));
        // Still collapsed from earlier
        Assert.False(controller.IsExpanded(simpleNodeSet[1]));
        // Tapping on the text instead of the Icon.
        tester.Tap(Find.Text("Root 1"));
        tester.Pump();
        Assert.True(controller.IsExpanded(simpleNodeSet[1]));
        Assert.True(toggled);
        Assert.Same(simpleNodeSet[1], toggledNode);
    }

    // Flutter: 'sliver_tree_test.dart: AnimationStyle is piped through to node builder'
    [Fact]
    public void AnimationStyleIsPipedThroughToNodeBuilder()
    {
        using var tester = new FrameworkDartTester();
        AnimationStyle? style = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: SimpleNodeSet(),
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                style ??= toggleAnimationStyle;
                return new Text(node.Content!.ToString()!);
            })));
        // Default
        Assert.Equal(TreeSliver.DefaultToggleAnimationStyle, style);

        style = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: SimpleNodeSet(),
            toggleAnimationStyle: AnimationStyle.NoAnimation,
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                style ??= toggleAnimationStyle;
                return new Text(node.Content!.ToString()!);
            })));
        Assert.Null(style!.Curve);
        Assert.Equal(TimeSpan.Zero, style.Duration);

        style = null;
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: SimpleNodeSet(),
            toggleAnimationStyle: new AnimationStyle(
                Curve: Curves.EaseIn,
                Duration: TimeSpan.FromMilliseconds(200)),
            treeNodeBuilder: (context, node, toggleAnimationStyle) =>
            {
                style ??= toggleAnimationStyle;
                return new Text(node.Content!.ToString()!);
            })));
        Assert.Same(Curves.EaseIn, style!.Curve);
        Assert.Equal(TimeSpan.FromMilliseconds(200), style.Duration);
    }

    // Flutter: 'sliver_tree_test.dart: Adding more root TreeViewNodes are reflected in the tree'
    [Fact]
    public void AddingMoreRootNodesAreReflectedInTheTree()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(BuildAddingTree(
            simpleNodeSet,
            controller,
            "Add root",
            () => simpleNodeSet.Add(new TreeSliverNode<string>("Added root"))));
        tester.Pump();

        ExpectSimpleNodeSetRows();
        Finds.Nothing(Find.Text("Added root"));

        tester.Tap(Find.Text("Add root"));
        tester.Pump();

        ExpectSimpleNodeSetRows();
        // Node was added
        Finds.OneWidget(Find.Text("Added root"));
    }

    // Flutter: 'sliver_tree_test.dart: Adding more TreeViewNodes below the root are reflected in the tree'
    [Fact]
    public void AddingMoreNodesBelowTheRootAreReflectedInTheTree()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var controller = new TreeSliverController();
        tester.PumpWidget(BuildAddingTree(
            simpleNodeSet,
            controller,
            "Add child",
            () => simpleNodeSet[1].Children.Add(new TreeSliverNode<string>("Added child"))));
        tester.Pump();

        ExpectSimpleNodeSetRows();
        Finds.Nothing(Find.Text("Added child"));

        tester.Tap(Find.Text("Add child"));
        tester.Pump();

        ExpectSimpleNodeSetRows();
        // Child node was added
        Finds.OneWidget(Find.Text("Added child"));
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliverNode should close all children when collapsed when
    // animation is disabled'
    [Fact]
    public void TreeSliverNodeShouldCloseAllChildrenWhenCollapsedWhenAnimationIsDisabled()
    {
        // Regression test for https://github.com/flutter/flutter/issues/153889
        using var tester = new FrameworkDartTester();
        var controller = new TreeSliverController();
        List<TreeSliverNode<string>> tree = TreeSliverFixtures.SetUpNodes();
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: tree,
            controller: controller,
            toggleAnimationStyle: AnimationStyle.NoAnimation,
            treeNodeBuilder: (context, node, animationStyle) =>
            {
                Widget child = new GestureDetector(
                    behavior: HitTestBehavior.Translucent,
                    onTap: () => controller.ToggleNode(node),
                    child: TreeSliver.DefaultTreeNodeBuilder(context, node, animationStyle));

                return child;
            })));

        foreach (string label in new[] { "First", "Second", "Third", "Fourth", "gamma", "delta", "epsilon" })
        {
            Finds.OneWidget(Find.Text(label));
        }

        foreach (string label in new[] { "alpha", "beta", "kappa", "uno", "dos", "tres" })
        {
            Finds.Nothing(Find.Text(label));
        }

        tester.Tap(Find.Text("Second"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("alpha"));

        tester.Tap(Find.Text("alpha"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("uno"));
        Finds.OneWidget(Find.Text("dos"));
        Finds.OneWidget(Find.Text("tres"));

        tester.Tap(Find.Text("alpha"));
        tester.PumpAndSettle();
        Finds.Nothing(Find.Text("uno"));
        Finds.Nothing(Find.Text("dos"));
        Finds.Nothing(Find.Text("tres"));
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliverNode should close all children when collapsed when
    // animation is completed'
    [Fact]
    public void TreeSliverNodeShouldCloseAllChildrenWhenCollapsedWhenAnimationIsCompleted()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TreeSliverController();
        List<TreeSliverNode<string>> tree =
        [
            new(
                "First",
                expanded: true,
                children:
                [
                    new("alpha", expanded: true, children: [new("uno"), new("dos"), new("tres")]),
                    new("beta"),
                    new("kappa"),
                ]),
        ];

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                shrinkWrap: true,
                slivers:
                [
                    new TreeSliver<string>(
                        tree: tree,
                        controller: controller,
                        toggleAnimationStyle: new AnimationStyle(
                            Curve: Curves.EaseInOut,
                            Duration: TimeSpan.FromMilliseconds(200)),
                        treeNodeBuilder: (context, node, animationStyle) =>
                        {
                            Widget child = new GestureDetector(
                                key: new ValueKey<string>((string)node.Content!),
                                behavior: HitTestBehavior.Translucent,
                                onTap: () => controller.ToggleNode(node),
                                child: TreeSliver.DefaultTreeNodeBuilder(context, node, animationStyle));

                            return child;
                        }),
                ])));

        Finds.OneWidget(Find.Text("alpha"));
        Finds.OneWidget(Find.Text("uno"));
        Finds.OneWidget(Find.Text("dos"));
        Finds.OneWidget(Find.Text("tres"));

        // Using runAsync to handle collapse and animations.
        tester.Tap(Find.Text("alpha"));
        tester.PumpAndSettle();

        Finds.Nothing(Find.Text("uno"));
        Finds.Nothing(Find.Text("dos"));
        Finds.Nothing(Find.Text("tres"));
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliver and PinnedHeaderSliver can render correctly when used
    // together.'
    [Fact]
    public void TreeSliverAndPinnedHeaderSliverRenderCorrectlyTogether()
    {
        // Plumix has no golden-image comparison: 'sliver_tree.pined_header.0.png' is not compared, the
        // test's geometry expectation is.
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Align(
                alignment: Alignment.TopLeft,
                child: new RepaintBoundary(
                    key: new ValueKey<string>("sliver_tree_pined_header"),
                    child: SizedBox.Square(
                        dimension: 20,
                        child: new CustomScrollView(
                            slivers:
                            [
                                new PinnedHeaderSliver(child: new SizedBox(height: 10)),
                                new TreeSliver<object>(
                                    tree: [new TreeSliverNode<object>(new object())],
                                    treeRowExtentBuilder: (_, _) => 10,
                                    treeNodeBuilder: (_, _, _) => new ColoredBox(new Color(0xFFF44336))),
                            ]))))));

        Assert.Equal(new Point(0, 10), tester.GetTopLeft(Find.ByType<ColoredBox>()));
    }

    // Flutter: 'sliver_tree_test.dart: The child node positions of TreeSliver are correct.'
    [Fact]
    public void TheChildNodePositionsOfTreeSliverAreCorrect()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new TreeSliver<object>(
            indentation: TreeSliverIndentationType.Custom(20),
            tree:
            [
                new TreeSliverNode<object>(
                    new ValueKey<int>(0),
                    expanded: true,
                    children: [new TreeSliverNode<object>(new ValueKey<int>(1))]),
            ],
            treeRowExtentBuilder: (_, _) => 20,
            treeNodeBuilder: (_, node, _) => new Container(key: (Key)node.Content!))));

        Assert.Equal(new Point(20, 20), tester.GetTopLeft(Find.ByKey(new ValueKey<int>(1))));
    }

    // Flutter: 'sliver_tree_test.dart: TreeSliver renders correctly after scrolling.'
    [Fact]
    public void TreeSliverRendersCorrectlyAfterScrolling()
    {
        // Plumix has no golden-image comparison: 'sliver_tree.scrolling.1.png' is not compared, the
        // test's geometry expectation is.
        using var tester = new FrameworkDartTester();
        var controller = new ScrollController();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Align(
                alignment: Alignment.TopLeft,
                child: new RepaintBoundary(
                    key: new ValueKey<string>("sliver_scrolling"),
                    child: SizedBox.Square(
                        dimension: 20,
                        child: new CustomScrollView(
                            controller: controller,
                            slivers:
                            [
                                new TreeSliver<object>(
                                    tree: [new TreeSliverNode<object>(new object())],
                                    treeRowExtentBuilder: (_, _) => 10,
                                    treeNodeBuilder: (_, _, _) => new ColoredBox(new Color(0xFFF44336))),
                                new SliverToBoxAdapter(child: new SizedBox(height: 20)),
                            ]))))));

        controller.JumpTo(5);
        tester.PumpAndSettle();

        Assert.Equal(new Point(0, -5), tester.GetTopLeft(Find.ByType<ColoredBox>()));
        controller.Dispose();
    }

    // ------------------------------------------------------------------ C#-side coverage of the spec

    // sliver_tree.dart: TreeSliverController.of throws a FlutterError naming the missing TreeSliver.
    [Fact]
    public void ControllerOfWithoutTreeSliverThrows()
    {
        using var tester = new FrameworkDartTester();
        BuildContext? captured = null;
        tester.PumpWidget(new Builder(context =>
        {
            captured = context;
            return new Container();
        }));

        Assert.Null(TreeSliverController.MaybeOf(captured!));
        FlutterError error = Assert.Throws<FlutterError>(() => TreeSliverController.Of(captured!));
        Assert.Contains(
            "TreeController.of() called with a context that does not contain a TreeSliver.",
            error.ToString(),
            StringComparison.Ordinal);
    }

    // sliver_tree.dart: _TreeSliverState.didUpdateWidget moves the state between controllers.
    [Fact]
    public void DidUpdateWidgetSwapsControllers()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        var first = new TreeSliverController();
        var second = new TreeSliverController();
        TreeSliverController? current = null;
        Widget Build(TreeSliverController? controller) => Wrap(new TreeSliver<string>(
            tree: simpleNodeSet,
            controller: controller,
            treeNodeBuilder: (context, node, style) =>
            {
                current = TreeSliverController.Of(context);
                return new Text(node.Content!.ToString()!);
            }));

        tester.PumpWidget(Build(null));
        TreeSliverController? internalController = current;
        Assert.NotNull(internalController);
        Assert.NotSame(first, internalController);

        tester.PumpWidget(Build(first));
        Assert.Same(first, current);
        Assert.True(first.IsExpanded(simpleNodeSet[1]));

        tester.PumpWidget(Build(second));
        Assert.Same(second, current);
        Assert.Null(first.State);
        Assert.True(second.IsActive(simpleNodeSet[1].Children[0]));

        tester.PumpWidget(Build(null));
        Assert.NotSame(second, current);
        Assert.Null(second.State);

        tester.PumpWidget(new Container());
        Assert.Null(current!.State);
    }

    // sliver_tree.dart: each row is KeyedSubtree -> AutomaticKeepAlive -> _TreeNodeParentDataWidget ->
    // IndexedSemantics -> RepaintBoundary -> the node builder's widget, and the depth reaches the
    // render sliver as TreeSliverNodeParentData.
    [Fact]
    public void RowCompositionAndParentData()
    {
        using var tester = new FrameworkDartTester();
        List<TreeSliverNode<string>> simpleNodeSet = SimpleNodeSet();
        tester.PumpWidget(Wrap(new TreeSliver<string>(tree: simpleNodeSet, semanticIndexOffset: 3)));

        Finder child = Find.Text("Child 1:0");
        IndexedSemantics indexed = tester.Widget<IndexedSemantics>(
            Find.Ancestor(child, Find.ByType<IndexedSemantics>()));
        Assert.Equal(2 + 3, indexed.Index);
        Assert.IsType<RepaintBoundary>(indexed.Child);
        Finds.OneWidget(Find.Ancestor(child, Find.ByType<AutomaticKeepAlive>()));

        RenderTreeSliver sliver = tester.RenderObject<RenderTreeSliver>(Find.ByType<TreeSliver<string>>());
        Assert.Equal(10.0, sliver.Indentation);
        Assert.Empty(sliver.ActiveAnimations);
        var depths = new List<int>();
        for (RenderBox? row = sliver.FirstChild; row is not null; row = sliver.ChildAfter(row))
        {
            depths.Add(((TreeSliverNodeParentData)row.parentData!).Depth);
        }

        Assert.Equal([0, 0, 1, 1, 0, 0], depths);
        Assert.Equal(40.0 * 6, sliver.Geometry!.ScrollExtent);

        // Without boundaries and semantic indexes the node builder's widget is the parent-data child.
        tester.PumpWidget(Wrap(new TreeSliver<string>(
            tree: simpleNodeSet,
            addRepaintBoundaries: false,
            addSemanticIndexes: false,
            addAutomaticKeepAlives: false)));
        Finds.Nothing(Find.Ancestor(child, Find.ByType<IndexedSemantics>()));
        Finds.Nothing(Find.Ancestor(child, Find.ByType<AutomaticKeepAlive>()));
    }

    // ------------------------------------------------------------------ helpers

    private static void ExpectSimpleNodeSetRows()
    {
        Finds.OneWidget(Find.Text("Root 0"));
        Finds.OneWidget(Find.Text("Root 1"));
        Finds.OneWidget(Find.Text("Child 1:0"));
        Finds.OneWidget(Find.Text("Child 1:1"));
        Finds.OneWidget(Find.Text("Root 2"));
        Finds.Nothing(Find.Text("Child 2:0"));
        Finds.Nothing(Find.Text("Child 2:1"));
        Finds.OneWidget(Find.Text("Root 3"));
    }

    private static Widget BuildAddingTree(
        List<TreeSliverNode<string>> simpleNodeSet,
        TreeSliverController controller,
        string buttonLabel,
        Action add)
    {
        return new Directionality(
            TextDirection.Ltr,
            new StatefulBuilder((context, setState) => new Column(
            [
                new TestButton(new Text(buttonLabel), onPressed: () => setState(add)),
                new Expanded(new CustomScrollView(
                    slivers: [new TreeSliver<string>(tree: simpleNodeSet, controller: controller)])),
            ])));
    }

    // button_tester.dart: TestButton.
    private sealed class TestButton(Widget child, Action? onPressed = null) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new Semantics(
                label: "button",
                button: true,
                enabled: onPressed != null,
                onTap: onPressed,
                focusable: true,
                child: new FocusableActionDetector(
                    enabled: onPressed != null,
                    child: new GestureDetector(onTap: onPressed, child: child)));
        }
    }
}
