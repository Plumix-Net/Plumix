using System.Diagnostics;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>One node of a <see cref="SemanticsUpdate"/>, as handed to the platform.</summary>
/// <param name="Node">The live node this entry was serialized from.</param>
/// <param name="Id">The node's stable accessibility id.</param>
/// <param name="Transform">
/// The transform assistive technologies sort with: <see cref="SemanticsNode.TraversalTransform"/>,
/// which differs from <paramref name="HitTestTransform"/> only for a grafted node.
/// </param>
/// <param name="HitTestTransform">The node's own paint-tree transform.</param>
/// <param name="TraversalParentId">The id this node is traversed under, or <c>-1</c>.</param>
/// <param name="ChildrenInTraversalOrder">Child ids in reading order, after grafting.</param>
/// <param name="ChildrenInHitTestOrder">Child ids in inverse paint order, orphans dropped.</param>
/// <param name="AdditionalActions">Ids of the custom actions this node handles, ascending.</param>
/// <remarks>
/// Flutter passes these as the ~40 named arguments of `SemanticsUpdateBuilder.updateNode`, whose
/// remaining arguments are the flattened `SemanticsData` of the node. Plumix's hosts read the
/// annotations off <paramref name="Node"/> instead, so this record carries only what the drain loop
/// computes and the node itself cannot answer (see `docs/ai/DIVERGENCES.md`).
/// </remarks>
public sealed record SemanticsNodeUpdate(
    SemanticsNode Node,
    int Id,
    Matrix4? Transform,
    Matrix4? HitTestTransform,
    int TraversalParentId,
    IReadOnlyList<int> ChildrenInTraversalOrder,
    IReadOnlyList<int> ChildrenInHitTestOrder,
    IReadOnlyList<int> AdditionalActions);

/// <summary>A custom action referenced by at least one node in a <see cref="SemanticsUpdate"/>.</summary>
/// <remarks>Flutter's <c>SemanticsUpdateBuilder.updateCustomAction</c> arguments.</remarks>
public sealed record CustomSemanticsActionUpdate(int Id, string? Label, string? Hint, int OverrideId);

/// <summary>The set of semantics nodes that changed since the last flush.</summary>
/// <remarks>
/// Flutter's <c>SemanticsUpdate</c> (an opaque `dart:ui` object). Only nodes that were actually
/// dirty are present: a node merged into an ancestor, or detached before the update was built, is
/// omitted.
/// </remarks>
public sealed class SemanticsUpdate
{
    internal SemanticsUpdate(
        IReadOnlyList<SemanticsNodeUpdate> nodes,
        IReadOnlyList<CustomSemanticsActionUpdate> customActions)
    {
        Nodes = nodes;
        CustomActions = customActions;
    }

    /// <summary>The updated nodes, shallowest first.</summary>
    public IReadOnlyList<SemanticsNodeUpdate> Nodes { get; }

    /// <summary>The custom actions the updated nodes reference.</summary>
    public IReadOnlyList<CustomSemanticsActionUpdate> CustomActions { get; }
}

/// <summary>Accumulates the nodes and custom actions of one <see cref="SemanticsUpdate"/>.</summary>
/// <remarks>Flutter's <c>SemanticsUpdateBuilder</c>.</remarks>
public sealed class SemanticsUpdateBuilder
{
    private readonly List<SemanticsNodeUpdate> _nodes = [];
    private readonly List<CustomSemanticsActionUpdate> _customActions = [];
    private readonly HashSet<int> _seenNodeIds = [];

    /// <remarks>Flutter's <c>SemanticsUpdateBuilder.updateNode</c>.</remarks>
    public void UpdateNode(SemanticsNodeUpdate node)
    {
        ArgumentNullException.ThrowIfNull(node);
        bool isFirstUpdateForNode = _seenNodeIds.Add(node.Id);
        Debug.Assert(isFirstUpdateForNode, $"Node {node.Id} was added to one semantics update twice.");
        _nodes.Add(node);
    }

    /// <remarks>Flutter's <c>SemanticsUpdateBuilder.updateCustomAction</c>.</remarks>
    public void UpdateCustomAction(int id, string? label, string? hint, int overrideId) =>
        _customActions.Add(new CustomSemanticsActionUpdate(id, label, hint, overrideId));

    /// <remarks>Flutter's <c>SemanticsUpdateBuilder.build</c>.</remarks>
    public SemanticsUpdate Build() => new(_nodes, _customActions);
}

public sealed partial class SemanticsNode
{
    private static readonly IReadOnlyList<int> EmptyChildList = [];

    /// <summary>Serializes this node into <paramref name="builder"/> and clears its dirty flag.</summary>
    /// <remarks>Flutter's private <c>SemanticsNode._addToUpdate</c>.</remarks>
    internal void AddToUpdate(SemanticsUpdateBuilder builder, HashSet<int> customSemanticsActionIdsUpdate)
    {
        Debug.Assert(_dirty);

        IReadOnlyList<int> childrenInTraversalOrder;
        IReadOnlyList<int> childrenInHitTestOrder;
        if (_children.Count == 0 || MergeAllDescendantsIntoThisNode)
        {
            // A childless traversal parent still reports the nodes grafted onto it.
            childrenInTraversalOrder = !PlatformDefaults.IsWeb
                                       && IsTraversalParent
                                       && _owner?.GetTraversalChildNodes(_traversalParentIdentifier!) is
                                           { } traversalChildren
                ? [.. traversalChildren.Select(static child => child.Attached ? child.Id : 0)]
                : EmptyChildList;
            childrenInHitTestOrder = EmptyChildList;
        }
        else
        {
            childrenInTraversalOrder = [.. ChildrenInTraversalOrder.Select(static child => child.Id)];
            childrenInHitTestOrder = [.. ChildrenInHitTestOrder.Reverse().Select(static child => child.Id)];
        }

        var additionalActions = new List<int>(_customActionHandlers.Count);
        foreach (CustomSemanticsAction action in _customActionHandlers.Keys)
        {
            additionalActions.Add(CustomSemanticsAction.GetIdentifier(action));
        }

        additionalActions.Sort();
        customSemanticsActionIdsUpdate.UnionWith(additionalActions);

        int traversalParentId = -1;
        if (_traversalChildIdentifier is { } identifier
            && _owner?.GetTraversalParentNode(identifier) is { } traversalParentNode)
        {
            traversalParentId = traversalParentNode.Id;
        }

        ResolveTraversalParent();

        builder.UpdateNode(new SemanticsNodeUpdate(
            this,
            Id,
            TraversalTransform,
            Transform,
            traversalParentId,
            childrenInTraversalOrder,
            childrenInHitTestOrder,
            additionalActions));
        _dirty = false;
    }
}
