using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/semantics/semantics.dart
// (SemanticsOwner.sendSemanticsUpdate, SemanticsNode attach/detach/_markDirty, traversal grafting)

namespace Plumix.Tests;

public sealed class SemanticsDirtyPipelineTests
{
    private static RenderSemanticsAnnotations Annotated(
        string label,
        Size size,
        object? traversalParentIdentifier = null,
        object? traversalChildIdentifier = null,
        RenderBox? child = null)
    {
        return new RenderSemanticsAnnotations(
            label: label,
            container: true,
            traversalParentIdentifier: traversalParentIdentifier,
            traversalChildIdentifier: traversalChildIdentifier,
            child: child ?? new RenderConstrainedBox(BoxConstraints.Tight(size)));
    }

    private static (PipelineOwner Pipeline, List<SemanticsUpdate> Updates) Pump(
        RenderBox child,
        Size viewSize)
    {
        var renderView = new RenderView { Child = child };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        var updates = new List<SemanticsUpdate>();
        pipeline.SemanticsOwner!.OnSemanticsUpdate = updates.Add;
        pipeline.FlushLayout(viewSize);
        pipeline.FlushSemantics();
        return (pipeline, updates);
    }

    [Fact]
    public void FlushSemantics_SendsExactlyOneUpdateCarryingTheChangedNodes()
    {
        (PipelineOwner pipeline, List<SemanticsUpdate> updates) =
            Pump(Annotated("A", new Size(20, 10)), new Size(100, 40));

        SemanticsUpdate update = Assert.Single(updates);
        Assert.Contains(update.Nodes, node => node.Node.Label == "A");
        Assert.Contains(update.Nodes, node => node.Id == 0);
        Assert.Equal(0, pipeline.SemanticsOwner!.RootNode!.Id);
    }

    [Fact]
    public void SendSemanticsUpdate_WithNothingDirty_ProducesNoUpdateAtAll()
    {
        (PipelineOwner pipeline, List<SemanticsUpdate> updates) =
            Pump(Annotated("A", new Size(20, 10)), new Size(100, 40));
        Assert.Single(updates);

        pipeline.SemanticsOwner!.SendSemanticsUpdate();

        Assert.Single(updates);
    }

    [Fact]
    public void ChangingOneAnnotation_ResendsOnlyTheNodeThatChanged()
    {
        RenderSemanticsAnnotations first = Annotated("first", new Size(20, 10));
        RenderSemanticsAnnotations second = Annotated("second", new Size(20, 10));
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        (PipelineOwner pipeline, List<SemanticsUpdate> updates) = Pump(row, new Size(100, 40));
        Assert.Single(updates);

        second.Label = "changed";
        pipeline.FlushLayout(new Size(100, 40));
        pipeline.FlushSemantics();

        Assert.Equal(2, updates.Count);
        SemanticsNodeUpdate resent = Assert.Single(updates[1].Nodes);
        Assert.Equal("changed", resent.Node.Label);
    }

    [Fact]
    public void UpdateWith_MarksTheNodeDirtyWhenTheRoleChanges()
    {
        var node = new SemanticsNode();
        Assert.False(node.DebugIsDirty);

        node.UpdateWith(new SemanticsConfiguration { Role = SemanticsRole.Tab });

        Assert.True(node.DebugIsDirty);
    }

    [Fact]
    public void UpdateWith_DoesNotDirtyTheNodeWhenNothingCompareableChanged()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        root.Rect = new Rect(0, 0, 100, 100);
        var config = new SemanticsConfiguration { Label = "same" };
        root.UpdateWith(config);
        owner.SendSemanticsUpdate();
        Assert.False(root.DebugIsDirty);

        // Only the action handler identity changes, and Flutter compares the action bits, not the map.
        var again = new SemanticsConfiguration { Label = "same" };
        again.AddActionHandler(SemanticsActions.Tap, () => { });
        again.Actions = SemanticsActions.None;
        again.ReplaceActionHandlers([]);
        root.UpdateWith(again);

        Assert.False(root.DebugIsDirty);
    }

    [Fact]
    public void UpdateWith_MarksTheNodeDirtyWhenTheCustomActionMapChanges()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        root.Rect = new Rect(0, 0, 100, 100);
        var config = new SemanticsConfiguration();
        config.AddCustomActionHandler(new CustomSemanticsAction("first"), () => { });
        root.UpdateWith(config);
        owner.SendSemanticsUpdate();
        Assert.False(root.DebugIsDirty);

        var changed = new SemanticsConfiguration();
        changed.AddCustomActionHandler(new CustomSemanticsAction("second"), () => { });
        root.UpdateWith(changed);

        Assert.True(root.DebugIsDirty);
        owner.SendSemanticsUpdate();
        Assert.False(root.DebugIsDirty);
    }

    [Fact]
    public void SendSemanticsUpdate_OmitsNodesMergedIntoAnAncestor()
    {
        var owner = new SemanticsOwner();
        var updates = new List<SemanticsUpdate>();
        owner.OnSemanticsUpdate = updates.Add;
        SemanticsNode root = SemanticsNode.Root(owner);
        var child = new SemanticsNode();
        root.Rect = new Rect(0, 0, 100, 100);
        child.Rect = new Rect(0, 0, 50, 50);
        root.UpdateWith(
            new SemanticsConfiguration { Label = "root", IsMergingSemanticsOfDescendants = true },
            [child]);
        child.UpdateWith(new SemanticsConfiguration { Label = "child" });

        owner.SendSemanticsUpdate();

        SemanticsNodeUpdate sent = Assert.Single(updates[0].Nodes);
        Assert.Equal(0, sent.Id);
        Assert.True(child.IsMergedIntoParent);
        Assert.Empty(sent.ChildrenInTraversalOrder);
        Assert.Empty(sent.ChildrenInHitTestOrder);
    }

    [Fact]
    public void ChangingAMergedDescendant_ResendsTheMergeRootInstead()
    {
        var owner = new SemanticsOwner();
        var updates = new List<SemanticsUpdate>();
        owner.OnSemanticsUpdate = updates.Add;
        SemanticsNode root = SemanticsNode.Root(owner);
        var child = new SemanticsNode();
        root.Rect = new Rect(0, 0, 100, 100);
        child.Rect = new Rect(0, 0, 50, 50);
        root.UpdateWith(
            new SemanticsConfiguration { Label = "root", IsMergingSemanticsOfDescendants = true },
            [child]);
        child.UpdateWith(new SemanticsConfiguration { Label = "child" });
        owner.SendSemanticsUpdate();

        child.UpdateWith(new SemanticsConfiguration { Label = "changed" });
        owner.SendSemanticsUpdate();

        SemanticsNodeUpdate resent = Assert.Single(updates[1].Nodes);
        Assert.Equal(0, resent.Id);
        Assert.False(child.DebugIsDirty);
    }

    [Fact]
    public void IsMergedIntoParent_PropagatesDownToTheNextMergeBoundaryOnly()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        var node1 = new SemanticsNode();
        var node11 = new SemanticsNode();
        var node12 = new SemanticsNode();
        root.Rect = new Rect(0, 0, 100, 100);
        node1.Rect = new Rect(0, 0, 50, 50);
        node11.Rect = new Rect(0, 0, 20, 20);
        node12.Rect = new Rect(0, 0, 20, 20);
        node1.UpdateWith(
            new SemanticsConfiguration { IsMergingSemanticsOfDescendants = true },
            [node11, node12]);
        root.UpdateWith(new SemanticsConfiguration { IsMergingSemanticsOfDescendants = true }, [node1]);

        Assert.True(node1.IsMergedIntoParent);
        Assert.True(node11.IsMergedIntoParent);
        Assert.True(node12.IsMergedIntoParent);
        // A node that merges its own descendants is a merge root, never merged into its parent.
        Assert.False(root.IsMergedIntoParent);

        root.UpdateWith(new SemanticsConfiguration(), [node1]);

        Assert.False(node1.IsMergedIntoParent);
        Assert.True(node11.IsMergedIntoParent);
        Assert.True(node12.IsMergedIntoParent);
    }

    [Fact]
    public void AttachAndDetach_MaintainTheOwnerIndexIncrementally()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        var child = new SemanticsNode();
        var grandChild = new SemanticsNode();
        root.Rect = new Rect(0, 0, 100, 100);
        child.Rect = new Rect(0, 0, 50, 50);
        grandChild.Rect = new Rect(0, 0, 20, 20);
        child.UpdateWith(new SemanticsConfiguration(), [grandChild]);
        root.UpdateWith(new SemanticsConfiguration(), [child]);

        Assert.Same(child, owner.GetSemanticsNode(child.Id));
        Assert.Same(grandChild, owner.GetSemanticsNode(grandChild.Id));
        Assert.True(grandChild.Attached);
        Assert.True(root.Depth < child.Depth && child.Depth < grandChild.Depth);

        root.UpdateWith(new SemanticsConfiguration(), []);

        Assert.Null(owner.GetSemanticsNode(child.Id));
        Assert.Null(owner.GetSemanticsNode(grandChild.Id));
        Assert.False(child.Attached);
        Assert.False(grandChild.Attached);
    }

    [Fact]
    public void Attach_RegeneratesTheIdWhenTheOwnerAlreadyHoldsIt()
    {
        var owner = new SemanticsOwner();
        SemanticsNode.DebugResetSemanticsIdCounter();
        var first = new SemanticsNode();
        first.Attach(owner);
        SemanticsNode.DebugResetSemanticsIdCounter();
        var second = new SemanticsNode();

        second.Attach(owner);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Same(first, owner.GetSemanticsNode(first.Id));
        Assert.Same(second, owner.GetSemanticsNode(second.Id));
        Assert.InRange(first.Id, 0, (1 << 16) - 2);
        Assert.InRange(second.Id, 0, (1 << 16) - 2);
    }

    [Fact]
    public void SendSemanticsUpdate_ThrowsWhenAnInvisibleNodeReachesTheTree()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        var child = new SemanticsNode();
        root.Rect = new Rect(0, 0, 100, 100);
        root.UpdateWith(new SemanticsConfiguration(), [child]);

        FlutterError error = Assert.Throws<FlutterError>(owner.SendSemanticsUpdate);

        Assert.Contains("Invisible SemanticsNodes should not be added to the tree.", error.Message);
    }

    [Fact]
    public void SendSemanticsUpdate_AcceptsAnInvisibleRootWithoutChildren()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        root.UpdateWith(new SemanticsConfiguration { Label = "empty" });

        owner.SendSemanticsUpdate();

        Assert.Equal(0, root.Id);
    }

    [Fact]
    public void TraversalGrafting_MovesAChildUnderItsTraversalParentInReadingOrderOnly()
    {
        RenderSemanticsAnnotations anchor =
            Annotated("anchor", new Size(20, 10), traversalParentIdentifier: "menu");
        RenderSemanticsAnnotations item =
            Annotated("item", new Size(20, 10), traversalChildIdentifier: "menu");
        var row = new RenderFlex(
            children: [anchor, item],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        (PipelineOwner pipeline, _) = Pump(row, new Size(100, 40));

        SemanticsNode root = pipeline.SemanticsOwner!.RootNode!;
        Assert.Equal(2, root.Children.Count);
        Assert.Equal(2, root.ChildrenInHitTestOrder.Count);

        SemanticsNode traversalChild = Assert.Single(root.ChildrenInTraversalOrder);
        Assert.Equal("anchor", traversalChild.Label);
        Assert.Equal("item", Assert.Single(traversalChild.ChildrenInTraversalOrder).Label);
    }

    [Fact]
    public void TraversalGrafting_KeepsSeveralChildrenSharingOneIdentifierInOrder()
    {
        RenderSemanticsAnnotations anchor =
            Annotated("anchor", new Size(20, 10), traversalParentIdentifier: "menu");
        RenderSemanticsAnnotations firstItem =
            Annotated("first", new Size(20, 10), traversalChildIdentifier: "menu");
        RenderSemanticsAnnotations secondItem =
            Annotated("second", new Size(20, 10), traversalChildIdentifier: "menu");
        var row = new RenderFlex(
            children: [anchor, firstItem, secondItem],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        (PipelineOwner pipeline, _) = Pump(row, new Size(200, 40));

        SemanticsNode anchorNode = Assert.Single(pipeline.SemanticsOwner!.RootNode!.ChildrenInTraversalOrder);
        Assert.Collection(
            anchorNode.ChildrenInTraversalOrder,
            node => Assert.Equal("first", node.Label),
            node => Assert.Equal("second", node.Label));
    }

    [Fact]
    public void TraversalGrafting_DropsAChildWhoseTraversalParentNeverRegistered()
    {
        RenderSemanticsAnnotations orphan =
            Annotated("orphan", new Size(20, 10), traversalChildIdentifier: "missing");
        (PipelineOwner pipeline, _) = Pump(orphan, new Size(100, 40));

        SemanticsNode root = pipeline.SemanticsOwner!.RootNode!;
        Assert.Single(root.Children);
        Assert.Empty(root.ChildrenInTraversalOrder);
        Assert.Empty(root.ChildrenInHitTestOrder);
    }

    [Fact]
    public void TraversalGrafting_ThrowsWhenTheTraversalParentIsNestedInsideItsTraversalChild()
    {
        RenderSemanticsAnnotations anchor =
            Annotated("anchor", new Size(20, 10), traversalParentIdentifier: "menu");
        RenderSemanticsAnnotations item = Annotated(
            "item",
            new Size(20, 10),
            traversalChildIdentifier: "menu",
            child: anchor);
        FlutterError error = Assert.Throws<FlutterError>(() => Pump(item, new Size(100, 40)));

        Assert.Contains("cannot be the child of the traversalChild", error.Message);
    }

    [Fact]
    public void TraversalGrafting_ReportsTheGraftCorrectedTransformInTheUpdate()
    {
        RenderSemanticsAnnotations anchor =
            Annotated("anchor", new Size(20, 10), traversalParentIdentifier: "menu");
        RenderSemanticsAnnotations item =
            Annotated("item", new Size(20, 10), traversalChildIdentifier: "menu");
        var row = new RenderFlex(
            // The leading spacer offsets the anchor, so its own transform is not the identity and
            // the grafted node's traversal transform differs from its paint transform.
            children: [new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))), anchor, item],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        (PipelineOwner pipeline, List<SemanticsUpdate> updates) = Pump(row, new Size(100, 40));

        SemanticsNode anchorNode = Assert.Single(pipeline.SemanticsOwner!.RootNode!.ChildrenInTraversalOrder);
        SemanticsNodeUpdate itemUpdate = Assert.Single(
            updates[0].Nodes,
            node => node.Node.Label == "item");

        Assert.Equal(anchorNode.Id, itemUpdate.TraversalParentId);
        // The traversal transform maps the grafted node into its traversal parent's coordinates,
        // so it differs from the paint-tree transform the hit test still uses.
        Assert.NotNull(itemUpdate.Transform);
        Assert.False(MatrixUtils.MatrixEquals(itemUpdate.Transform, itemUpdate.HitTestTransform));
    }

    [Fact]
    public void SemanticsOwner_NotifiesListenersAfterTheUpdateIsBuilt()
    {
        var owner = new SemanticsOwner();
        var order = new List<string>();
        owner.OnSemanticsUpdate = _ => order.Add("update");
        owner.AddListener(() => order.Add("listener"));
        SemanticsNode root = SemanticsNode.Root(owner);
        root.Rect = new Rect(0, 0, 100, 100);
        root.UpdateWith(new SemanticsConfiguration { Label = "root" });

        owner.SendSemanticsUpdate();

        Assert.Equal(["update", "listener"], order);
    }

    [Fact]
    public void SemanticsOwner_Dispose_ClearsEveryRegistration()
    {
        var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        root.Rect = new Rect(0, 0, 100, 100);
        root.UpdateWith(new SemanticsConfiguration { Label = "root" });

        owner.Dispose();

        Assert.Null(owner.RootNode);
        Assert.Null(owner.GetSemanticsNode(0));
    }
}
