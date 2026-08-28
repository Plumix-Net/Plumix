using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity sources (custom-action dispatch regression tests):
// flutter/packages/flutter/lib/src/semantics/semantics.dart
// flutter/packages/flutter/lib/src/rendering/binding.dart

namespace Plumix.Tests;

public sealed class SemanticsCustomActionTests
{
    [Fact]
    public void CustomSemanticsAction_IsGivenACanonicalIdBasedOnTheLabel()
    {
        var first = new CustomSemanticsAction("canonical id label");
        var sameLabel = new CustomSemanticsAction("canonical id label");
        var otherLabel = new CustomSemanticsAction("canonical id other label");

        int id = CustomSemanticsAction.GetIdentifier(first);

        Assert.Equal(id, CustomSemanticsAction.GetIdentifier(sameLabel));
        Assert.NotEqual(id, CustomSemanticsAction.GetIdentifier(otherLabel));
        Assert.Equal(first, CustomSemanticsAction.GetAction(id));
        Assert.Equal(first, sameLabel);
        Assert.Equal(first.GetHashCode(), sameLabel.GetHashCode());
    }

    [Fact]
    public void CustomSemanticsAction_OverridingActionIsDistinctFromALabelledAction()
    {
        CustomSemanticsAction overriding =
            CustomSemanticsAction.OverridingAction("override hint", SemanticsActions.Tap);
        CustomSemanticsAction sameOverride =
            CustomSemanticsAction.OverridingAction("override hint", SemanticsActions.Tap);
        CustomSemanticsAction otherAction =
            CustomSemanticsAction.OverridingAction("override hint", SemanticsActions.LongPress);

        Assert.Null(overriding.Label);
        Assert.Equal("override hint", overriding.Hint);
        Assert.Equal(SemanticsActions.Tap, overriding.Action);
        Assert.Equal(
            CustomSemanticsAction.GetIdentifier(overriding),
            CustomSemanticsAction.GetIdentifier(sameOverride));
        Assert.NotEqual(
            CustomSemanticsAction.GetIdentifier(overriding),
            CustomSemanticsAction.GetIdentifier(otherAction));
        Assert.NotEqual(overriding, new CustomSemanticsAction("override hint"));
    }

    [Fact]
    public void CustomSemanticsAction_GetActionReturnsNullForAnUnregisteredId()
    {
        Assert.Null(CustomSemanticsAction.GetAction(int.MaxValue));
    }

    [Fact]
    public void CustomSemanticsAction_EmptyLabelIsRejected()
    {
        Assert.Throws<ArgumentException>(static () => new CustomSemanticsAction("  "));
        Assert.Throws<ArgumentException>(
            static () => CustomSemanticsAction.OverridingAction(" ", SemanticsActions.Tap));
    }

    [Fact]
    public void CustomSemanticsAction_RegistersTheSharedCustomActionBit()
    {
        var configuration = new SemanticsConfiguration();
        configuration.AddCustomActionHandler(new CustomSemanticsAction("bit"), static () => { });

        Assert.True(configuration.Actions.HasFlag(SemanticsActions.CustomAction));
    }

    [Fact]
    public void CustomSemanticsAction_TwoConfigurationsWithCustomActionsAreIncompatible()
    {
        // Flutter's `isCompatibleWith` tests `_actionsAsBits & other._actionsAsBits`, and the custom
        // action setter ORs in one shared bit, so any two custom-action carriers conflict.
        var first = new SemanticsConfiguration();
        first.AddCustomActionHandler(new CustomSemanticsAction("incompatible first"), static () => { });
        var second = new SemanticsConfiguration();
        second.AddCustomActionHandler(new CustomSemanticsAction("incompatible second"), static () => { });

        Assert.False(first.IsCompatibleWith(second));
    }

    [Fact]
    public void MergeSemantics_DispatchesCustomActionsToTheOwningDescendant()
    {
        int outerCount = 0;
        int innerCount = 0;
        var outerAction = new CustomSemanticsAction("merged outer");
        var innerAction = new CustomSemanticsAction("merged inner");

        var inner = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [innerAction] = () => innerCount += 1,
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var outer = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [outerAction] = () => outerCount += 1,
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        (SemanticsOwner owner, SemanticsNode mergedNode) = BuildMerged(inner, outer);

        Assert.True(owner.PerformCustomAction(mergedNode.Id, innerAction));
        Assert.Equal(1, innerCount);
        Assert.Equal(0, outerCount);

        Assert.True(owner.PerformCustomAction(mergedNode.Id, outerAction));
        Assert.Equal(1, innerCount);
        Assert.Equal(1, outerCount);
    }

    [Fact]
    public void MergeSemantics_UnknownCustomActionIdIsANoOp()
    {
        int count = 0;
        var action = new CustomSemanticsAction("merged unknown id");
        var first = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [action] = () => count += 1,
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [new CustomSemanticsAction("merged unknown id sibling")] = static () => { },
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        (SemanticsOwner owner, SemanticsNode mergedNode) = BuildMerged(first, second);

        Assert.False(owner.PerformAction(mergedNode.Id, SemanticsActions.CustomAction, int.MaxValue));
        Assert.False(owner.PerformAction(mergedNode.Id, SemanticsActions.CustomAction, "not an id"));
        Assert.Equal(0, count);
    }

    [Fact]
    public void PerformAction_OnAnUnknownNodeIsANoOp()
    {
        (SemanticsOwner owner, SemanticsNode _) = BuildMerged(
            new RenderSemanticsAnnotations(
                label: "unknown node",
                child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)))),
            new RenderSemanticsAnnotations(
                label: "unknown node sibling",
                child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)))));

        Assert.False(owner.PerformAction(-1, SemanticsActions.Tap));
        Assert.False(owner.PerformAction(-1, SemanticsActions.ShowOnScreen));
    }

    [Fact]
    public void PerformActionAt_RoutesCustomActionsThroughMergedDescendants()
    {
        int leftCount = 0;
        int rightCount = 0;
        var leftAction = new CustomSemanticsAction("position left");
        var rightAction = new CustomSemanticsAction("position right");
        var left = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [leftAction] = () => leftCount += 1,
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var right = new RenderSemanticsAnnotations(
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
            {
                [rightAction] = () => rightCount += 1,
            },
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        (SemanticsOwner owner, SemanticsNode mergedNode) = BuildMerged(left, right);

        Assert.True(owner.PerformActionAt(
            new Point(5, 5),
            SemanticsActions.CustomAction,
            CustomSemanticsAction.GetIdentifier(leftAction)));
        Assert.Equal(1, leftCount);
        Assert.Equal(0, rightCount);

        // Nothing lives outside the merged group's box.
        Assert.False(owner.PerformActionAt(
            new Point(5, 500),
            SemanticsActions.CustomAction,
            CustomSemanticsAction.GetIdentifier(leftAction)));
        Assert.Equal(1, leftCount);
    }

    [Fact]
    public void GetSemanticsNode_ReturnsTheNodeInTheTreeOrNull()
    {
        (SemanticsOwner owner, SemanticsNode mergedNode) = BuildMerged(
            new RenderSemanticsAnnotations(
                label: "lookup first",
                child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)))),
            new RenderSemanticsAnnotations(
                label: "lookup second",
                child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)))));

        Assert.Same(mergedNode, owner.GetSemanticsNode(mergedNode.Id));
        Assert.Null(owner.GetSemanticsNode(-1));
    }

    [Fact]
    public void GetRectOfSemanticsNode_ReturnsTheNodeBoxInViewCoordinates()
    {
        var first = new RenderSemanticsAnnotations(
            label: "rect first",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var second = new RenderSemanticsAnnotations(
            label: "rect second",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(30, 10))));
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        var renderView = new RenderView { Child = row };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode? root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        SemanticsNode secondNode = Assert.Single(root!.Children, static node => node.Label == "rect second");

        Rect? rect = pipeline.SemanticsOwner.GetRectOfSemanticsNode(secondNode.Id);
        Assert.NotNull(rect);
        Assert.Equal(new Rect(20, 0, 30, 10), rect!.Value);
        Assert.Null(pipeline.SemanticsOwner.GetRectOfSemanticsNode(-1));
    }

    [Fact]
    public void PerformAction_NotifiesActionListenersBeforeDispatch()
    {
        int tapCount = 0;
        var annotations = new RenderSemanticsAnnotations(
            label: "listener",
            container: true,
            onTap: () => tapCount += 1,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var renderView = new RenderView { Child = annotations };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode node = Assert.Single(pipeline.SemanticsOwner.RootNode!.Children);
        var seen = new List<SemanticsActionEvent>();
        var tapCountWhenNotified = new List<int>();
        void Listener(SemanticsActionEvent actionEvent)
        {
            // Flutter's binding fires every listener before the action reaches its node.
            tapCountWhenNotified.Add(tapCount);
            seen.Add(actionEvent);
        }

        pipeline.SemanticsOwner.AddSemanticsActionListener(Listener);
        Assert.True(pipeline.SemanticsOwner.PerformAction(node.Id, SemanticsActions.Tap));
        Assert.Equal(1, tapCount);
        SemanticsActionEvent observed = Assert.Single(seen);
        Assert.Equal(node.Id, observed.NodeId);
        Assert.Equal(SemanticsActions.Tap, observed.Type);
        Assert.Null(observed.Arguments);
        Assert.Equal([0], tapCountWhenNotified);

        // An action nothing handles is still reported.
        Assert.False(pipeline.SemanticsOwner.PerformAction(-1, SemanticsActions.LongPress));
        Assert.Equal(2, seen.Count);

        pipeline.SemanticsOwner.RemoveSemanticsActionListener(Listener);
        Assert.True(pipeline.SemanticsOwner.PerformAction(node.Id, SemanticsActions.Tap));
        Assert.Equal(2, seen.Count);
    }

    private static (SemanticsOwner Owner, SemanticsNode MergedNode) BuildMerged(
        RenderBox first,
        RenderBox second)
    {
        var merge = new RenderSemanticsAnnotations(
            container: true,
            mergeDescendants: true,
            child: new RenderFlex(
                children: [first, second],
                direction: Axis.Horizontal,
                textDirection: TextDirection.Ltr));
        var renderView = new RenderView { Child = merge };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode? root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        SemanticsNode mergedNode = Assert.Single(root!.Children);
        return (pipeline.SemanticsOwner, mergedNode);
    }
}
