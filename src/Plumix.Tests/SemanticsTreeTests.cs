using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/semantics.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class SemanticsTreeTests
{
    [Fact]
    public void Button_ProducesSemanticsNode_WithButtonFlagsAndTapAction()
    {
        var button = new RenderButton(
            label: "Increment",
            onPressed: static () => { },
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14);

        var renderView = new RenderView
        {
            Child = button
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(200, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var semanticsButton = Assert.Single(root.Children);

        Assert.Equal("Increment", semanticsButton.Label);
        Assert.True(semanticsButton.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(semanticsButton.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(semanticsButton.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void Button_SemanticsNode_UpdatesEnabledState_WithoutChangingIdentity()
    {
        var button = new RenderButton(
            label: "Save",
            onPressed: static () => { },
            background: Colors.SeaGreen,
            foreground: Colors.White,
            fontSize: 14);

        var renderView = new RenderView
        {
            Child = button
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        var firstNode = Assert.Single(firstRoot.Children);
        var firstId = firstNode.Id;

        button.OnPressed = null;
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        var updatedNode = Assert.Single(updatedRoot.Children);
        Assert.Equal(firstId, updatedNode.Id);
        Assert.True(updatedNode.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.False(updatedNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.Equal(SemanticsActions.None, updatedNode.Actions);
    }

    [Fact]
    public void SemanticsOwner_PerformAction_InvokesTapHandler()
    {
        var tapCount = 0;
        var button = new RenderButton(
            label: "Tap me",
            onPressed: () => tapCount += 1,
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14);

        var renderView = new RenderView
        {
            Child = button
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var buttonNode = Assert.Single(root.Children);

        Assert.True(pipeline.SemanticsOwner.PerformAction(buttonNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, tapCount);

        button.OnPressed = null;
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        var disabledNode = Assert.Single(updatedRoot.Children);

        Assert.False(pipeline.SemanticsOwner.PerformAction(disabledNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, tapCount);
    }

    [Fact]
    public void SemanticsOwner_PerformAction_InvokesLongPressHandler()
    {
        var longPressCount = 0;
        var box = new MultiActionSemanticBox(
            label: "LongPress",
            size: new Size(16, 10),
            action: SemanticsActions.LongPress,
            handler: () => longPressCount += 1);

        var renderView = new RenderView
        {
            Child = box
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var node = Assert.Single(root.Children);
        Assert.True(node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.True(pipeline.SemanticsOwner.PerformAction(node.Id, SemanticsActions.LongPress));
        Assert.Equal(1, longPressCount);
        Assert.False(pipeline.SemanticsOwner.PerformAction(node.Id, SemanticsActions.ShowOnScreen));
    }

    [Fact]
    public void MergeSemantics_MergesDescendantsIntoSingleNode()
    {
        var merge = new MergeSemanticsRenderBox(
            new RenderFlex(
                children:
                [
                    new RenderParagraph("One"),
                    new RenderParagraph("Two"),
                ],
                direction: Axis.Horizontal));

        var renderView = new RenderView
        {
            Child = merge
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var mergedNode = Assert.Single(root.Children);
        Assert.Equal("Group One Two", mergedNode.Label);
        Assert.Empty(mergedNode.Children);
    }

    [Fact]
    public void MergeSemantics_ConflictingActionsRemainAsSeparateChildNode()
    {
        var firstTapCount = 0;
        var secondTapCount = 0;
        var first = new ActionSemanticBox("First", new Size(20, 10), () => firstTapCount += 1);
        var second = new ActionSemanticBox("Second", new Size(20, 10), () => secondTapCount += 1);
        var merge = new MergeSemanticsRenderBox(
            new RenderFlex(
                children: [first, second],
                direction: Axis.Horizontal));

        var renderView = new RenderView
        {
            Child = merge
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var mergedNode = Assert.Single(root.Children);
        Assert.Equal("Group First", mergedNode.Label);
        Assert.True(mergedNode.Actions.HasFlag(SemanticsActions.Tap));

        var conflictingNode = Assert.Single(mergedNode.Children);
        Assert.Equal("Second", conflictingNode.Label);
        Assert.True(conflictingNode.Actions.HasFlag(SemanticsActions.Tap));

        Assert.True(pipeline.SemanticsOwner.PerformAction(mergedNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, firstTapCount);
        Assert.Equal(0, secondTapCount);

        Assert.True(pipeline.SemanticsOwner.PerformAction(conflictingNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, firstTapCount);
        Assert.Equal(1, secondTapCount);
    }

    [Fact]
    public void MergeSemantics_PrunesMergedChildNodeCache()
    {
        var first = new FixedSemanticBox("First", new Size(20, 10));
        var second = new FixedSemanticBox("Second", new Size(20, 10));
        var merge = new MergeSemanticsRenderBox(
            new RenderFlex(
                children: [first, second],
                direction: Axis.Horizontal));

        var renderView = new RenderView
        {
            Child = merge
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var mergedNode = Assert.Single(root.Children);
        Assert.Empty(mergedNode.Children);

        Assert.Null(first._semanticsNode);
        Assert.Null(second._semanticsNode);
    }

    [Fact]
    public void ExcludeSemantics_RemovesSubtreeFromSemanticsTree()
    {
        var excluded = new ExcludeSemanticsRenderBox(new RenderParagraph("Hidden"));
        var renderView = new RenderView
        {
            Child = excluded
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void DebugDumpTree_ContainsNodeActionsAndLabels()
    {
        var button = new RenderButton(
            label: "Save",
            onPressed: static () => { },
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14);

        var renderView = new RenderView
        {
            Child = button
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var dump = pipeline.SemanticsOwner.DebugDumpTree();
        Assert.Contains("label=\"Save\"", dump);
        Assert.Contains("actions=Tap", dump);
    }

    [Fact]
    public void SemanticsFlush_UpdatesDirtySubtree_WithoutForcingAncestorPerformSemantics()
    {
        var leaf = new CountingSemanticLeafRenderBox("A");
        var boundary = new CountingSemanticBoundaryRenderBox(leaf);
        var renderView = new RenderView
        {
            Child = boundary
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        boundary.ResetSemanticsCounter();
        leaf.ResetSemanticsCounter();

        leaf.Label = "B";
        pipeline.FlushSemantics();

        Assert.Equal(0, boundary.SemanticsUpdateCount);
        Assert.Equal(1, leaf.SemanticsUpdateCount);

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var updatedLeaf = Assert.Single(root.Children[0].Children);
        Assert.Equal("B", updatedLeaf.Label);
    }

    [Fact]
    public void BlockingSemantics_DropsPreviouslyPaintedSiblings()
    {
        var back = new FixedSemanticBox("Back", new Size(20, 10));
        var front = new FixedSemanticBox("Front", new Size(20, 10), blocksPreviousNodes: true);
        var row = new RenderFlex(
            children: [back, front],
            direction: Axis.Horizontal);

        var renderView = new RenderView
        {
            Child = row
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var survivingNode = Assert.Single(root.Children);
        Assert.Equal("Front", survivingNode.Label);
    }

    [Fact]
    public void BlockingSemantics_LeavesBlockedSiblingParentDataDirty_UntilUnblocked()
    {
        var back = new FixedSemanticBox("Back", new Size(20, 10));
        var front = new MutableBlockingSemanticBox("Front", new Size(20, 10), blocksPreviousNodes: true);
        var row = new RenderFlex(
            children: [back, front],
            direction: Axis.Horizontal);

        var renderView = new RenderView
        {
            Child = row
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        Assert.True(back.SemanticsParentDataDirty);
        Assert.False(front.SemanticsParentDataDirty);

        front.BlocksPreviousNodes = false;
        pipeline.FlushSemantics();

        Assert.False(back.SemanticsParentDataDirty);
        Assert.False(front.SemanticsParentDataDirty);

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void BlockingSemantics_BlockedSiblingDirtyNode_DoesNotRebuildUntilUnblocked()
    {
        var back = new CountingSemanticLeafRenderBox("Back");
        var front = new MutableBlockingSemanticBox("Front", new Size(20, 10), blocksPreviousNodes: true);
        var row = new RenderFlex(
            children: [back, front],
            direction: Axis.Horizontal);

        var renderView = new RenderView
        {
            Child = row
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        back.ResetSemanticsCounter();
        back.Label = "Back 2";
        pipeline.FlushSemantics();

        Assert.Equal(0, back.SemanticsUpdateCount);
        Assert.True(back.SemanticsParentDataDirty);

        front.BlocksPreviousNodes = false;
        pipeline.FlushSemantics();

        Assert.Equal(1, back.SemanticsUpdateCount);
        Assert.False(back.SemanticsParentDataDirty);

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Contains(root.Children, node => node.Label == "Back 2");
        Assert.Contains(root.Children, node => node.Label == "Front");
    }

    [Fact]
    public void VisitChildrenForSemantics_OmittedChildStaysParentDataDirty_UntilVisible()
    {
        var first = new FixedSemanticBox("First", new Size(20, 10));
        var second = new FixedSemanticBox("Second", new Size(20, 10));
        var row = new SelectiveSemanticsFlex(first, second)
        {
            IncludeSecondInSemantics = false
        };

        var renderView = new RenderView
        {
            Child = row
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        Assert.False(first.SemanticsParentDataDirty);
        Assert.True(second.SemanticsParentDataDirty);

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var onlyNode = Assert.Single(root.Children);
        Assert.Equal("First", onlyNode.Label);

        row.IncludeSecondInSemantics = true;
        pipeline.FlushSemantics();

        Assert.False(second.SemanticsParentDataDirty);

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        Assert.Equal(2, updatedRoot.Children.Count);
    }

    [Fact]
    public void RenderTransform_AppliesTranslationToSemanticsRect()
    {
        var leaf = new FixedSemanticBox("Moved", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(30, 12), leaf);
        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var moved = Assert.Single(root.Children);
        Assert.Equal(new Rect(30, 12, 12, 8), moved.Rect);
    }

    [Fact]
    public void RenderTransform_ChangingTransform_UpdatesSemanticsWithoutLayout()
    {
        var leaf = new FixedSemanticBox("Moved", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(10, 6), leaf);
        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        var firstNode = Assert.Single(firstRoot.Children);
        var firstId = firstNode.Id;
        Assert.Equal(new Rect(10, 6, 12, 8), firstNode.Rect);

        transform.Transform = Matrix.CreateTranslation(44, 18);
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        var updatedNode = Assert.Single(updatedRoot.Children);
        Assert.Equal(firstId, updatedNode.Id);
        Assert.Equal(new Rect(44, 18, 12, 8), updatedNode.Rect);
    }

    [Fact]
    public void RenderClipRect_ExcludesSemanticsOutsideClip()
    {
        var leaf = new FixedSemanticBox("Hidden", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(40, 0), leaf);
        var clip = new RenderClipRect(transform)
        {
            ClipRect = new Rect(0, 0, 20, 20)
        };

        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void RenderClipRect_ChangingClip_UpdatesSemanticsWithoutLayout()
    {
        var leaf = new FixedSemanticBox("Clipped", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(20, 0), leaf);
        var clip = new RenderClipRect(transform)
        {
            ClipRect = new Rect(0, 0, 64, 32)
        };

        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        Assert.Single(firstRoot.Children);

        clip.ClipRect = new Rect(0, 0, 8, 8);
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        Assert.Empty(updatedRoot.Children);
    }

    [Fact]
    public void DistinctSemanticsClip_OutsidePaintClip_NodeStaysAsHidden()
    {
        var leaf = new FixedSemanticBox("HiddenByPaint", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(30, 0), leaf);
        var clip = new DistinctSemanticsClipRenderBox(transform)
        {
            PaintClipRect = new Rect(0, 0, 20, 20),
            SemanticsClipRect = new Rect(0, 0, 64, 20)
        };

        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var hiddenNode = Assert.Single(root.Children);
        Assert.Equal("HiddenByPaint", hiddenNode.Label);
        Assert.True(hiddenNode.IsHidden);
    }

    [Fact]
    public void DistinctSemanticsClip_OutsideSemanticsClip_DropsNode()
    {
        var leaf = new FixedSemanticBox("DroppedBySemanticsClip", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(80, 0), leaf);
        var clip = new DistinctSemanticsClipRenderBox(transform)
        {
            PaintClipRect = new Rect(0, 0, 20, 20),
            SemanticsClipRect = new Rect(0, 0, 64, 20)
        };

        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void SemanticsParentDataDirty_BoundaryStaysClean_NonBoundaryCanBecomeDirty()
    {
        var leaf = new FixedSemanticBox("State", new Size(12, 8));
        var transform = new RenderTransform(Matrix.Identity, leaf);
        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        Assert.True(leaf.SemanticsParentDataDirty);
        Assert.True(transform.SemanticsParentDataDirty);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();
        Assert.False(leaf.SemanticsParentDataDirty);
        Assert.False(transform.SemanticsParentDataDirty);

        leaf.MarkNeedsSemanticsUpdate();
        Assert.False(leaf.SemanticsParentDataDirty);

        transform.Transform = Matrix.CreateTranslation(1, 0);
        Assert.True(transform.SemanticsParentDataDirty);

        pipeline.FlushSemantics();
        Assert.False(leaf.SemanticsParentDataDirty);
        Assert.False(transform.SemanticsParentDataDirty);
    }

    [Fact]
    public void MarkNeedsSemanticsUpdate_WhenAncestorAlreadyQueued_DoesNotGrowPendingQueue()
    {
        var descendant = new RenderTransform(Matrix.Identity, new FixedSemanticBox("Leaf", new Size(12, 8)));
        var ancestor = new MutableSemanticBoundaryRenderBox("Ancestor", descendant);
        var renderView = new RenderView
        {
            Child = ancestor
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        ancestor.Label = "Ancestor 2";
        Assert.Equal(1, pipeline.PendingSemanticsNodeCount);

        descendant.Transform = Matrix.CreateTranslation(4, 0);
        Assert.Equal(1, pipeline.PendingSemanticsNodeCount);

        pipeline.FlushSemantics();
        Assert.False(descendant.SemanticsParentDataDirty);
    }

    [Fact]
    public void MarkNeedsSemanticsUpdate_BoundaryWithoutChildDelegate_DoesNotDirtyAncestorParentData()
    {
        var boundary = new DelegatingSemanticBoundaryRenderBox(
            label: "Child",
            child: new FixedSemanticBox("Leaf", new Size(12, 8)),
            hasChildConfigurationsDelegate: false);
        var ancestor = new MutableSemanticBoundaryRenderBox("Ancestor", boundary);
        var renderView = new RenderView
        {
            Child = ancestor
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        Assert.False(boundary.SemanticsParentDataDirty);
        Assert.False(ancestor.SemanticsParentDataDirty);

        boundary.Label = "Child 2";

        Assert.False(boundary.SemanticsParentDataDirty);
        Assert.False(ancestor.SemanticsParentDataDirty);
    }

    [Fact]
    public void MarkNeedsSemanticsUpdate_BoundaryWithChildDelegate_DirtiesAncestorParentData()
    {
        var boundary = new DelegatingSemanticBoundaryRenderBox(
            label: "Child",
            child: new FixedSemanticBox("Leaf", new Size(12, 8)),
            hasChildConfigurationsDelegate: true);
        var ancestor = new MutableSemanticBoundaryRenderBox("Ancestor", boundary);
        var renderView = new RenderView
        {
            Child = ancestor
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        Assert.False(boundary.SemanticsParentDataDirty);
        Assert.False(ancestor.SemanticsParentDataDirty);

        boundary.Label = "Child 2";

        Assert.True(boundary.SemanticsParentDataDirty);
        Assert.True(ancestor.SemanticsParentDataDirty);

        pipeline.FlushSemantics();
        Assert.False(boundary.SemanticsParentDataDirty);
        Assert.False(ancestor.SemanticsParentDataDirty);
    }

    [Fact]
    public void ChildConfigurationsDelegate_MergeUp_AbsorbsChildrenIntoBoundaryNode()
    {
        var row = new RenderFlex(
            children:
            [
                new FixedSemanticBox("One", new Size(12, 8)),
                new FixedSemanticBox("Two", new Size(12, 8))
            ],
            direction: Axis.Horizontal);
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", row, static childConfigurations =>
            new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(childConfigurations),
                new List<List<SemanticsConfiguration>>()));

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var parentNode = Assert.Single(root.Children);
        Assert.Equal("Parent One Two", parentNode.Label);
        Assert.Empty(parentNode.Children);
    }

    [Fact]
    public void ChildConfigurationsDelegate_SiblingMergeGroup_ProducesSiblingNode()
    {
        var row = new RenderFlex(
            children:
            [
                new FixedSemanticBox("One", new Size(12, 8)),
                new FixedSemanticBox("Two", new Size(12, 8))
            ],
            direction: Axis.Horizontal);
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", row, static childConfigurations =>
            new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(),
                new List<List<SemanticsConfiguration>>
                {
                    new List<SemanticsConfiguration>(childConfigurations)
                }));

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);

        var parentNode = Assert.Single(root.Children.Where(node => node.Label == "Parent"));
        Assert.Empty(parentNode.Children);

        var siblingNode = Assert.Single(root.Children.Where(node => node.Label == "One Two"));
        Assert.True(siblingNode.Flags == SemanticsFlags.None);
    }

    [Fact]
    public void ChildConfigurationsDelegate_SiblingMergeGroup_WithConflicts_LeavesSeparateNodes()
    {
        var firstTapCount = 0;
        var secondTapCount = 0;
        var first = new ActionSemanticBox("First", new Size(12, 8), () => firstTapCount += 1);
        var second = new ActionSemanticBox("Second", new Size(12, 8), () => secondTapCount += 1);
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal);
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", row, static childConfigurations =>
            new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(),
                new List<List<SemanticsConfiguration>>
                {
                    new List<SemanticsConfiguration>(childConfigurations)
                }));

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Equal(3, root.Children.Count);

        Assert.Single(root.Children.Where(node => node.Label == "Parent"));
        var firstNode = Assert.Single(root.Children.Where(node => node.Label == "First"));
        var secondNode = Assert.Single(root.Children.Where(node => node.Label == "Second"));

        Assert.True(pipeline.SemanticsOwner.PerformAction(firstNode.Id, SemanticsActions.Tap));
        Assert.True(pipeline.SemanticsOwner.PerformAction(secondNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, firstTapCount);
        Assert.Equal(1, secondTapCount);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteMergeUp_WithoutChildSemantics_AbsorbsIntoBoundaryNode()
    {
        var childWithoutSemantics = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10));
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", childWithoutSemantics, static _ =>
        {
            var synthetic = new SemanticsConfiguration
            {
                Label = "Synthetic"
            };

            return new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration> { synthetic },
                new List<List<SemanticsConfiguration>>());
        });

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var parentNode = Assert.Single(root.Children);
        Assert.Equal("Parent Synthetic", parentNode.Label);
        Assert.Empty(parentNode.Children);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteSiblingGroup_WithoutChildSemantics_ProducesActionableSiblingNode()
    {
        var tapCount = 0;
        var childWithoutSemantics = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10));
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", childWithoutSemantics, _ =>
        {
            var synthetic = new SemanticsConfiguration
            {
                Label = "Synthetic Action"
            };
            synthetic.AddActionHandler(SemanticsActions.Tap, () => tapCount += 1);

            return new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(),
                new List<List<SemanticsConfiguration>>
                {
                    new List<SemanticsConfiguration> { synthetic }
                });
        });

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);

        var siblingNode = Assert.Single(root.Children.Where(node => node.Label == "Synthetic Action"));
        Assert.True(siblingNode.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(pipeline.SemanticsOwner.PerformAction(siblingNode.Id, SemanticsActions.Tap));
        Assert.Equal(1, tapCount);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteSiblingGroup_ReusesNodeIdentityAcrossFlushes()
    {
        var childWithoutSemantics = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10));
        var delegated = new ChildDelegateSemanticBoundaryRenderBox("Parent", childWithoutSemantics, static _ =>
        {
            var synthetic = new SemanticsConfiguration
            {
                Label = "Synthetic Stable"
            };

            return new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(),
                new List<List<SemanticsConfiguration>>
                {
                    new List<SemanticsConfiguration> { synthetic }
                });
        });
        var transform = new RenderTransform(Matrix.Identity, delegated);

        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        var firstNode = FindNodeByLabel(firstRoot, "Synthetic Stable");
        Assert.NotNull(firstNode);
        var firstId = firstNode.Id;

        transform.Transform = Matrix.CreateTranslation(6, 0);
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        var updatedNode = FindNodeByLabel(updatedRoot, "Synthetic Stable");
        Assert.NotNull(updatedNode);
        Assert.Equal(firstId, updatedNode.Id);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteSiblingGroup_ConflictToggle_ReusesIdsAcrossMergeAndSplit()
    {
        var delegated = new MutableSyntheticSiblingGroupRenderBox(
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10)),
            conflictingActions: false);
        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        var merged = FindNodeByLabel(firstRoot, "Synthetic A Synthetic B");
        Assert.NotNull(merged);
        var mergedId = merged.Id;

        delegated.ConflictingActions = true;
        pipeline.FlushSemantics();

        var splitRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(splitRoot);
        var firstSplit = FindNodeByLabel(splitRoot, "Synthetic A");
        var secondSplit = FindNodeByLabel(splitRoot, "Synthetic B");
        Assert.NotNull(firstSplit);
        Assert.NotNull(secondSplit);
        Assert.Equal(mergedId, firstSplit.Id);
        var secondSplitId = secondSplit.Id;

        delegated.ConflictingActions = false;
        pipeline.FlushSemantics();

        var mergedAgainRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(mergedAgainRoot);
        var mergedAgain = FindNodeByLabel(mergedAgainRoot, "Synthetic A Synthetic B");
        Assert.NotNull(mergedAgain);
        Assert.Equal(mergedId, mergedAgain.Id);

        delegated.ConflictingActions = true;
        pipeline.FlushSemantics();

        var splitAgainRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(splitAgainRoot);
        var secondSplitAgain = FindNodeByLabel(splitAgainRoot, "Synthetic B");
        Assert.NotNull(secondSplitAgain);
        Assert.Equal(secondSplitId, secondSplitAgain.Id);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteMergeUp_ConflictToggle_ReusesSiblingNodeIdAcrossReappearance()
    {
        var delegated = new MutableSyntheticMergeUpConflictRenderBox(
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10)),
            parentTapConflict: false);
        var transform = new RenderTransform(Matrix.Identity, delegated);

        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        Assert.NotNull(FindNodeByLabel(firstRoot, "Parent Synthetic MergeUp"));
        Assert.Null(FindNodeByLabel(firstRoot, "Synthetic MergeUp"));

        delegated.ParentTapConflict = true;
        pipeline.FlushSemantics();

        var conflictRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(conflictRoot);
        var firstSibling = FindNodeByLabel(conflictRoot, "Synthetic MergeUp");
        Assert.NotNull(firstSibling);
        var siblingId = firstSibling.Id;

        delegated.ParentTapConflict = false;
        pipeline.FlushSemantics();

        var mergedAgainRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(mergedAgainRoot);
        Assert.NotNull(FindNodeByLabel(mergedAgainRoot, "Parent Synthetic MergeUp"));
        Assert.Null(FindNodeByLabel(mergedAgainRoot, "Synthetic MergeUp"));

        delegated.ParentTapConflict = true;
        pipeline.FlushSemantics();

        var conflictAgainRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(conflictAgainRoot);
        var secondSibling = FindNodeByLabel(conflictAgainRoot, "Synthetic MergeUp");
        Assert.NotNull(secondSibling);
        Assert.Equal(siblingId, secondSibling.Id);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteSiblingGroup_TransformUpdatesSyntheticNodeRect()
    {
        var delegated = new SyntheticGeometrySiblingRenderBox(
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10)),
            label: "Synthetic Geo",
            explicitRect: new Rect(0, 0, 10, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(10, 6), delegated);

        var renderView = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var firstRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(firstRoot);
        var firstNode = FindNodeByLabel(firstRoot, "Synthetic Geo");
        Assert.NotNull(firstNode);
        Assert.Equal(new Rect(10, 6, 10, 8), firstNode.Rect);
        var firstId = firstNode.Id;

        transform.Transform = Matrix.CreateTranslation(25, 3);
        pipeline.FlushSemantics();

        var updatedRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(updatedRoot);
        var updatedNode = FindNodeByLabel(updatedRoot, "Synthetic Geo");
        Assert.NotNull(updatedNode);
        Assert.Equal(firstId, updatedNode.Id);
        Assert.Equal(new Rect(25, 3, 10, 8), updatedNode.Rect);
    }

    [Fact]
    public void ChildConfigurationsDelegate_IncompleteSiblingGroup_ClipAndHiddenStateUpdate()
    {
        var delegated = new SyntheticGeometrySiblingRenderBox(
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 10)),
            label: "Synthetic Clip",
            explicitRect: new Rect(0, 0, 12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(30, 0), delegated);
        var clip = new DistinctSemanticsClipRenderBox(transform)
        {
            PaintClipRect = new Rect(0, 0, 120, 40),
            SemanticsClipRect = new Rect(0, 0, 120, 40)
        };

        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        pipeline.FlushSemantics();

        var initialRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(initialRoot);
        var initialNode = FindNodeByLabel(initialRoot, "Synthetic Clip");
        Assert.NotNull(initialNode);
        Assert.False(initialNode.IsHidden);
        var initialId = initialNode.Id;

        clip.PaintClipRect = new Rect(0, 0, 10, 10);
        pipeline.FlushSemantics();

        var hiddenRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(hiddenRoot);
        var hiddenNode = FindNodeByLabel(hiddenRoot, "Synthetic Clip");
        Assert.NotNull(hiddenNode);
        Assert.Equal(initialId, hiddenNode.Id);
        Assert.True(hiddenNode.IsHidden);

        clip.SemanticsClipRect = new Rect(0, 0, 10, 10);
        pipeline.FlushSemantics();

        var clippedOutRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(clippedOutRoot);
        Assert.Null(FindNodeByLabel(clippedOutRoot, "Synthetic Clip"));

        clip.PaintClipRect = new Rect(0, 0, 120, 40);
        clip.SemanticsClipRect = new Rect(0, 0, 120, 40);
        pipeline.FlushSemantics();

        var restoredRoot = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(restoredRoot);
        var restoredNode = FindNodeByLabel(restoredRoot, "Synthetic Clip");
        Assert.NotNull(restoredNode);
        Assert.Equal(initialId, restoredNode.Id);
        Assert.False(restoredNode.IsHidden);
    }

    [Fact]
    public void ChildConfigurationsDelegate_WithExplicitChildNodes_ThrowsInvalidOperation()
    {
        var row = new RenderFlex(children: [new FixedSemanticBox("One", new Size(12, 8))], direction: Axis.Horizontal);
        var delegated = new ChildDelegateSemanticBoundaryRenderBox(
            label: "Parent",
            child: row,
            childDelegate: static _ => new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(),
                new List<List<SemanticsConfiguration>>()),
            explicitChildNodes: true);

        var renderView = new RenderView
        {
            Child = delegated
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(220, 120));
        Assert.Throws<InvalidOperationException>(() => pipeline.FlushSemantics());
    }

    private static SemanticsNode? FindNodeByLabel(SemanticsNode? node, string label)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Label == label)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindNodeByLabel(child, label);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class MutableSemanticBoundaryRenderBox : RenderProxyBox
    {
        private string _label;

        public MutableSemanticBoundaryRenderBox(string label, RenderBox child)
        {
            _label = label;
            Child = child;
        }

        public string Label
        {
            get => _label;
            set
            {
                if (_label == value)
                {
                    return;
                }

                _label = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
        }
    }

    private sealed class MutableSyntheticSiblingGroupRenderBox : RenderProxyBox
    {
        private bool _conflictingActions;

        public MutableSyntheticSiblingGroupRenderBox(RenderBox child, bool conflictingActions)
        {
            Child = child;
            _conflictingActions = conflictingActions;
        }

        public bool ConflictingActions
        {
            get => _conflictingActions;
            set
            {
                if (_conflictingActions == value)
                {
                    return;
                }

                _conflictingActions = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = "Parent";
            configuration.ChildConfigurationsDelegate = _ =>
            {
                var first = new SemanticsConfiguration
                {
                    Label = "Synthetic A"
                };
                first.AddActionHandler(SemanticsActions.Tap, static () => { });

                var second = new SemanticsConfiguration
                {
                    Label = "Synthetic B"
                };
                if (_conflictingActions)
                {
                    second.AddActionHandler(SemanticsActions.Tap, static () => { });
                }

                return new ChildSemanticsConfigurationsResult(
                    new List<SemanticsConfiguration>(),
                    new List<List<SemanticsConfiguration>>
                    {
                        new List<SemanticsConfiguration> { first, second }
                    });
            };
        }
    }

    private sealed class MutableSyntheticMergeUpConflictRenderBox : RenderProxyBox
    {
        private bool _parentTapConflict;

        public MutableSyntheticMergeUpConflictRenderBox(RenderBox child, bool parentTapConflict)
        {
            Child = child;
            _parentTapConflict = parentTapConflict;
        }

        public bool ParentTapConflict
        {
            get => _parentTapConflict;
            set
            {
                if (_parentTapConflict == value)
                {
                    return;
                }

                _parentTapConflict = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = "Parent";

            if (_parentTapConflict)
            {
                configuration.AddActionHandler(SemanticsActions.Tap, static () => { });
            }

            configuration.ChildConfigurationsDelegate = static _ =>
            {
                var synthetic = new SemanticsConfiguration
                {
                    Label = "Synthetic MergeUp"
                };
                synthetic.AddActionHandler(SemanticsActions.Tap, static () => { });

                return new ChildSemanticsConfigurationsResult(
                    new List<SemanticsConfiguration> { synthetic },
                    new List<List<SemanticsConfiguration>>());
            };
        }
    }

    private sealed class SyntheticGeometrySiblingRenderBox : RenderProxyBox
    {
        private readonly string _label;
        private readonly Rect _explicitRect;

        public SyntheticGeometrySiblingRenderBox(RenderBox child, string label, Rect explicitRect)
        {
            Child = child;
            _label = label;
            _explicitRect = explicitRect;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = "Parent";
            configuration.ChildConfigurationsDelegate = _ =>
            {
                var synthetic = new SemanticsConfiguration
                {
                    Label = _label,
                    ExplicitRect = _explicitRect
                };

                return new ChildSemanticsConfigurationsResult(
                    new List<SemanticsConfiguration>(),
                    new List<List<SemanticsConfiguration>>
                    {
                        new List<SemanticsConfiguration> { synthetic }
                    });
            };
        }
    }

    private sealed class DelegatingSemanticBoundaryRenderBox : RenderProxyBox
    {
        private static readonly ChildSemanticsConfigurationsDelegate MergeAllDelegate = static childConfigurations =>
            new ChildSemanticsConfigurationsResult(
                new List<SemanticsConfiguration>(childConfigurations),
                new List<List<SemanticsConfiguration>>());

        private string _label;
        private bool _hasChildConfigurationsDelegate;

        public DelegatingSemanticBoundaryRenderBox(
            string label,
            RenderBox child,
            bool hasChildConfigurationsDelegate)
        {
            _label = label;
            _hasChildConfigurationsDelegate = hasChildConfigurationsDelegate;
            Child = child;
        }

        public string Label
        {
            get => _label;
            set
            {
                if (_label == value)
                {
                    return;
                }

                _label = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        public bool HasChildConfigurationsDelegate
        {
            get => _hasChildConfigurationsDelegate;
            set
            {
                if (_hasChildConfigurationsDelegate == value)
                {
                    return;
                }

                _hasChildConfigurationsDelegate = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
            if (_hasChildConfigurationsDelegate)
            {
                configuration.ChildConfigurationsDelegate = MergeAllDelegate;
            }
        }
    }

    private sealed class ChildDelegateSemanticBoundaryRenderBox : RenderProxyBox
    {
        private readonly string _label;
        private readonly ChildSemanticsConfigurationsDelegate _childDelegate;
        private readonly bool _explicitChildNodes;

        public ChildDelegateSemanticBoundaryRenderBox(
            string label,
            RenderBox child,
            ChildSemanticsConfigurationsDelegate childDelegate,
            bool explicitChildNodes = false)
        {
            _label = label;
            _childDelegate = childDelegate;
            _explicitChildNodes = explicitChildNodes;
            Child = child;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
            configuration.ExplicitChildNodes = _explicitChildNodes;
            configuration.ChildConfigurationsDelegate = _childDelegate;
        }
    }

    private sealed class MergeSemanticsRenderBox : RenderProxyBox
    {
        public MergeSemanticsRenderBox(RenderBox child)
        {
            Child = child;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.IsMergingSemanticsOfDescendants = true;
            configuration.Label = "Group";
        }
    }

    private sealed class ExcludeSemanticsRenderBox : RenderProxyBox
    {
        public ExcludeSemanticsRenderBox(RenderBox child)
        {
            Child = child;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsExcluded = true;
        }
    }

    private sealed class CountingSemanticBoundaryRenderBox : RenderProxyBox
    {
        public int SemanticsUpdateCount { get; private set; }

        public CountingSemanticBoundaryRenderBox(RenderBox child)
        {
            Child = child;
        }

        public void ResetSemanticsCounter()
        {
            SemanticsUpdateCount = 0;
        }

        protected override void PerformLayout()
        {
            Child!.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.PaintChild(Child!, offset);
        }

        protected override void PerformSemantics()
        {
            SemanticsUpdateCount += 1;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = "Boundary";
        }
    }

    private sealed class CountingSemanticLeafRenderBox : RenderBox
    {
        private string _label;

        public CountingSemanticLeafRenderBox(string label)
        {
            _label = label;
        }

        public int SemanticsUpdateCount { get; private set; }

        public string Label
        {
            get => _label;
            set
            {
                if (_label == value)
                {
                    return;
                }

                _label = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        public void ResetSemanticsCounter()
        {
            SemanticsUpdateCount = 0;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(20, 10));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void PerformSemantics()
        {
            SemanticsUpdateCount += 1;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = Label;
        }
    }

    private sealed class FixedSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;
        private readonly bool _blocksPreviousNodes;

        public FixedSemanticBox(string label, Size size, bool blocksPreviousNodes = false)
        {
            _label = label;
            _size = size;
            _blocksPreviousNodes = blocksPreviousNodes;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.IsBlockingSemanticsOfPreviouslyPaintedNodes = _blocksPreviousNodes;
            configuration.Label = _label;
        }
    }

    private sealed class ActionSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;
        private readonly Action _onTap;

        public ActionSemanticBox(string label, Size size, Action onTap)
        {
            _label = label;
            _size = size;
            _onTap = onTap;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
            configuration.AddActionHandler(SemanticsActions.Tap, _onTap);
        }
    }

    private sealed class MultiActionSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;
        private readonly SemanticsActions _action;
        private readonly Action _handler;

        public MultiActionSemanticBox(
            string label,
            Size size,
            SemanticsActions action,
            Action handler)
        {
            _label = label;
            _size = size;
            _action = action;
            _handler = handler;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
            configuration.AddActionHandler(_action, _handler);
        }
    }

    private sealed class MutableBlockingSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;
        private bool _blocksPreviousNodes;

        public MutableBlockingSemanticBox(string label, Size size, bool blocksPreviousNodes)
        {
            _label = label;
            _size = size;
            _blocksPreviousNodes = blocksPreviousNodes;
        }

        public bool BlocksPreviousNodes
        {
            get => _blocksPreviousNodes;
            set
            {
                if (_blocksPreviousNodes == value)
                {
                    return;
                }

                _blocksPreviousNodes = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.IsBlockingSemanticsOfPreviouslyPaintedNodes = _blocksPreviousNodes;
            configuration.Label = _label;
        }
    }

    private sealed class SelectiveSemanticsFlex : RenderFlex
    {
        private bool _includeSecondInSemantics = true;

        public SelectiveSemanticsFlex(RenderBox first, RenderBox second)
            : base(children: [first, second], direction: Axis.Horizontal)
        {
        }

        public bool IncludeSecondInSemantics
        {
            get => _includeSecondInSemantics;
            set
            {
                if (_includeSecondInSemantics == value)
                {
                    return;
                }

                _includeSecondInSemantics = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
        {
            var firstChild = FirstChild;
            if (firstChild == null)
            {
                return;
            }

            var firstParentData = (FlexParentData)firstChild.parentData!;
            visitor(firstChild, firstParentData.offset, Matrix.Identity);

            if (!_includeSecondInSemantics)
            {
                return;
            }

            var secondChild = ChildAfter(firstChild);
            if (secondChild == null)
            {
                return;
            }

            var secondParentData = (FlexParentData)secondChild.parentData!;
            visitor(secondChild, secondParentData.offset, Matrix.Identity);
        }
    }

    private sealed class DistinctSemanticsClipRenderBox : RenderProxyBox
    {
        private Rect _paintClipRect;
        private Rect _semanticsClipRect;

        public DistinctSemanticsClipRenderBox(RenderBox child)
        {
            Child = child;
        }

        public Rect PaintClipRect
        {
            get => _paintClipRect;
            set
            {
                if (_paintClipRect == value)
                {
                    return;
                }

                _paintClipRect = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        public Rect SemanticsClipRect
        {
            get => _semanticsClipRect;
            set
            {
                if (_semanticsClipRect == value)
                {
                    return;
                }

                _semanticsClipRect = value;
                MarkNeedsSemanticsUpdate();
            }
        }

        protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
        {
            return _paintClipRect;
        }

        protected override Rect? DescribeSemanticsClip(RenderObject? child)
        {
            return _semanticsClipRect;
        }
    }
}
