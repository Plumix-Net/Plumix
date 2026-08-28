using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity sources (accessibility focus blocking regression tests):
// flutter/packages/flutter/lib/src/semantics/semantics.dart
// flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Tests;

public sealed class SemanticsAccessibilityFocusBlockTests
{
    [Fact]
    public void BlockSubtree_IsAppliedToTheSubtree()
    {
        // The second child explicitly declares `None`; `BlockSubtree` from the parent still wins.
        var first = new RenderSemanticsAnnotations(
            label: "subtree child 0",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            label: "subtree child 1",
            container: true,
            accessibilityFocusBlockType: AccessibilityFocusBlockType.None,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        SemanticsNode container = BuildContainer(
            AccessibilityFocusBlockType.BlockSubtree,
            first,
            second);

        Assert.True(container.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.All(
            container.Children,
            static node => Assert.True(node.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked)));
        Assert.Equal(2, container.Children.Count);
    }

    [Fact]
    public void BlockNode_IsNotAppliedToTheSubtree()
    {
        var first = new RenderSemanticsAnnotations(
            label: "node child 0",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            label: "node child 1",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        SemanticsNode container = BuildContainer(AccessibilityFocusBlockType.BlockNode, first, second);

        Assert.True(container.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.All(
            container.Children,
            static node => Assert.False(node.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked)));
    }

    [Fact]
    public void BlockNode_DoesNotMergeUpOrMergeDown()
    {
        var first = new RenderSemanticsAnnotations(
            label: "semantics label 1",
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            label: "semantics label 2",
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var blocked = new RenderSemanticsAnnotations(
            label: "semantics label 0",
            accessibilityFocusBlockType: AccessibilityFocusBlockType.BlockNode,
            child: new RenderFlex(
                children: [first, second],
                direction: Axis.Horizontal,
                textDirection: TextDirection.Ltr));
        SemanticsNode root = BuildRoot(blocked);

        SemanticsNode blockedNode = Assert.Single(root.Children);
        Assert.Equal("semantics label 0", blockedNode.Label);
        Assert.True(blockedNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.Collection(
            blockedNode.Children,
            static node => Assert.Equal("semantics label 1", node.Label),
            static node => Assert.Equal("semantics label 2", node.Label));
    }

    [Fact]
    public void BlockSubtree_DoesNotMergeUpButAbsorbsItsDescendants()
    {
        var first = new RenderSemanticsAnnotations(
            label: "semantics label 1",
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            label: "semantics label 2",
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var blocked = new RenderSemanticsAnnotations(
            label: "semantics label 0",
            accessibilityFocusBlockType: AccessibilityFocusBlockType.BlockSubtree,
            child: new RenderFlex(
                children: [first, second],
                direction: Axis.Horizontal,
                textDirection: TextDirection.Ltr));
        SemanticsNode root = BuildRoot(blocked);

        SemanticsNode blockedNode = Assert.Single(root.Children);
        // Every descendant inherits `BlockSubtree`, so nothing conflicts and everything folds in.
        Assert.Equal("semantics label 0\nsemantics label 1\nsemantics label 2", blockedNode.Label);
        Assert.True(blockedNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.Empty(blockedNode.Children);
    }

    [Fact]
    public void BlockedNode_AlsoBlocksKeyboardFocus()
    {
        var blocked = new RenderSemanticsAnnotations(
            label: "focused and blocked",
            container: true,
            flags: SemanticsFlags.IsFocusable | SemanticsFlags.IsFocused,
            accessibilityFocusBlockType: AccessibilityFocusBlockType.BlockSubtree,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        SemanticsNode root = BuildRoot(blocked);

        SemanticsNode blockedNode = Assert.Single(root.Children);
        Assert.True(blockedNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.False(blockedNode.Flags.HasFlag(SemanticsFlags.IsFocused));
        Assert.False(blockedNode.Flags.HasFlag(SemanticsFlags.IsFocusable));
    }

    [Fact]
    public void UpdatingBlockTypeOnTheParentUpdatesTheChildrenSemantics()
    {
        var child = new RenderSemanticsAnnotations(
            label: "updated child",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var parent = new RenderSemanticsAnnotations(
            label: "updated parent",
            container: true,
            child: child);
        var renderView = new RenderView { Child = parent };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode parentNode = Assert.Single(pipeline.SemanticsOwner.RootNode!.Children);
        SemanticsNode childNode = Assert.Single(parentNode.Children);
        Assert.False(parentNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.False(childNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));

        parent.AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockSubtree;
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        parentNode = Assert.Single(pipeline.SemanticsOwner.RootNode!.Children);
        childNode = Assert.Single(parentNode.Children);
        Assert.True(parentNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.True(childNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
    }

    [Fact]
    public void MergeSemantics_MergesChildrenWithBlockNode()
    {
        // `MergeSemantics` folds nodes through the node tree, not through `Absorb`, so a blocked and
        // an unblocked child still end up on one node that reports the block.
        var blocked = new RenderSemanticsAnnotations(
            label: "node1",
            container: true,
            accessibilityFocusBlockType: AccessibilityFocusBlockType.BlockNode,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var plain = new RenderSemanticsAnnotations(
            label: "node2",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var merge = new RenderSemanticsAnnotations(
            container: true,
            mergeDescendants: true,
            child: new RenderFlex(
                children: [blocked, plain],
                direction: Axis.Horizontal,
                textDirection: TextDirection.Ltr));
        SemanticsNode root = BuildRoot(merge);

        SemanticsNode mergedNode = Assert.Single(root.Children);
        Assert.All(mergedNode.Children, static node => Assert.True(node.IsMergedIntoParent));
        Assert.True(mergedNode.MergeAllDescendantsIntoThisNode);
        Assert.True(mergedNode.IsPartOfNodeMerging);
        SemanticsNode blockedNode = Assert.Single(mergedNode.Children, static node => node.Label == "node1");
        SemanticsNode plainNode = Assert.Single(mergedNode.Children, static node => node.Label == "node2");
        Assert.True(blockedNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
        Assert.False(plainNode.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));
    }

    [Fact]
    public void Configuration_BlockTypeDrivesTheDerivedFlagAndMergesOnAbsorb()
    {
        var configuration = new SemanticsConfiguration
        {
            AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockNode,
        };
        Assert.True(configuration.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));

        configuration.AccessibilityFocusBlockType = AccessibilityFocusBlockType.None;
        Assert.False(configuration.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked));

        // `_merge`: blockSubtree wins over blockNode, which wins over none.
        var parent = new SemanticsConfiguration
        {
            Label = "parent",
            AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockNode,
        };
        var child = new SemanticsConfiguration
        {
            Label = "child",
            AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockSubtree,
        };
        parent.Absorb(child);
        Assert.Equal(AccessibilityFocusBlockType.BlockSubtree, parent.AccessibilityFocusBlockType);
        Assert.Equal(AccessibilityFocusBlockType.BlockSubtree, parent.Clone().AccessibilityFocusBlockType);
    }

    [Fact]
    public void Configuration_BlockedAndUnblockedConfigurationsConflict()
    {
        var blocked = new SemanticsConfiguration
        {
            Label = "blocked",
            AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockNode,
        };
        var plain = new SemanticsConfiguration { Label = "plain" };
        var alsoBlocked = new SemanticsConfiguration
        {
            Label = "also blocked",
            AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockNode,
        };

        // Unlike every other flag, `isAccessibilityFocusBlocked` conflicts on inequality.
        Assert.False(blocked.IsCompatibleWith(plain));
        Assert.False(plain.IsCompatibleWith(blocked));
        Assert.True(blocked.IsCompatibleWith(alsoBlocked));
    }

    private static SemanticsNode BuildContainer(
        AccessibilityFocusBlockType blockType,
        params RenderBox[] children)
    {
        var container = new RenderSemanticsAnnotations(
            label: "container",
            container: true,
            explicitChildNodes: true,
            accessibilityFocusBlockType: blockType,
            child: new RenderFlex(
                children: [.. children],
                direction: Axis.Horizontal,
                textDirection: TextDirection.Ltr));
        return Assert.Single(BuildRoot(container).Children);
    }

    private static SemanticsNode BuildRoot(RenderBox child)
    {
        var renderView = new RenderView { Child = child };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode? root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        return root!;
    }
}
