using System.Diagnostics;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// The ownership, depth, dirty-tracking and traversal-grafting half of <see cref="SemanticsNode"/>.
/// </summary>
/// <remarks>
/// Flutter keeps these members inline in `semantics.dart`; they are split out here only so the
/// annotation half of the node stays readable.
/// </remarks>
public sealed partial class SemanticsNode
{
    private SemanticsOwner? _owner;
    private bool _dirty;
    private bool _dead;
    private int _depth;

    /// <summary>The owner this node is attached to, or <c>null</c> while it is detached.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.owner</c>.</remarks>
    public SemanticsOwner? Owner => _owner;

    /// <summary>Whether this node is currently registered with a <see cref="SemanticsOwner"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.attached</c>.</remarks>
    public bool Attached => _owner is not null;

    /// <summary>
    /// The depth of this node in the semantics tree; strictly greater than every ancestor's.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.depth</c>. Depths are never decreased, so a node moved to a
    /// shallower parent keeps its old, larger depth; all that matters is the ancestor ordering the
    /// two <c>sendSemanticsUpdate</c> sorts and <c>_computeTraversalTransform</c> rely on.
    /// </remarks>
    public int Depth => _depth;

    /// <remarks>Flutter's private <c>SemanticsNode._redepthChild</c>.</remarks>
    private void RedepthChild(SemanticsNode child)
    {
        Debug.Assert(ReferenceEquals(child.Owner, Owner));
        if (child._depth <= _depth)
        {
            child._depth = _depth + 1;
            child.RedepthChildren();
        }
    }

    /// <remarks>Flutter's private <c>SemanticsNode._redepthChildren</c>.</remarks>
    private void RedepthChildren()
    {
        foreach (SemanticsNode child in _children)
        {
            RedepthChild(child);
        }
    }

    /// <remarks>Flutter's private <c>SemanticsNode._updateChildMergeFlagRecursively</c>.</remarks>
    private void UpdateChildMergeFlagRecursively(SemanticsNode child)
    {
        Debug.Assert(ReferenceEquals(child.Owner, Owner));
        bool childShouldMergeToParent = IsPartOfNodeMerging;
        if (childShouldMergeToParent == child.IsMergedIntoParent)
        {
            return;
        }

        child.IsMergedIntoParent = childShouldMergeToParent;
        if (!child.MergeAllDescendantsIntoThisNode)
        {
            // A child that merges its own descendants already flagged them; nothing below it moves.
            child.UpdateChildrenMergeFlags();
        }
    }

    /// <remarks>Flutter's private <c>SemanticsNode._updateChildrenMergeFlags</c>.</remarks>
    internal void UpdateChildrenMergeFlags()
    {
        foreach (SemanticsNode child in _children)
        {
            UpdateChildMergeFlagRecursively(child);
        }
    }

    /// <remarks>Flutter's private <c>SemanticsNode._adoptChild</c>.</remarks>
    private void AdoptChild(SemanticsNode child)
    {
        Debug.Assert(child.Parent is null);
        Debug.Assert(DebugIsNotAncestorOf(child), "Adopting this child would create a cycle.");
        child.Parent = this;
        if (Attached)
        {
            child.Attach(_owner!);
        }

        RedepthChild(child);
        UpdateChildMergeFlagRecursively(child);
    }

    /// <remarks>Flutter's private <c>SemanticsNode._dropChild</c>.</remarks>
    private void DropChild(SemanticsNode child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        Debug.Assert(child.Attached == Attached);
        child.Parent = null;
        if (Attached)
        {
            child.Detach();
        }
    }

    private bool DebugIsNotAncestorOf(SemanticsNode child)
    {
        SemanticsNode node = this;
        while (node.Parent is { } parent)
        {
            node = parent;
        }

        return !ReferenceEquals(node, child);
    }

    /// <summary>Registers this node and its subtree with <paramref name="owner"/>.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.attach</c>. The id is regenerated until it does not collide with
    /// one the owner already holds, which is how an id freed by <see cref="Detach"/> is reused.
    /// </remarks>
    internal void Attach(SemanticsOwner owner)
    {
        Debug.Assert(_owner is null);
        _owner = owner;
        while (owner.ContainsNodeId(Id))
        {
            Id = GenerateNewId();
        }

        owner.RegisterNode(this);
        if (_dirty)
        {
            // Re-enqueue into the new owner's dirty list; the old owner's is not this one's.
            _dirty = false;
            MarkDirty();
        }

        foreach (SemanticsNode child in _children)
        {
            child.Attach(owner);
        }
    }

    /// <summary>Unregisters this node and its subtree from its owner.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.detach</c>.</remarks>
    internal void Detach()
    {
        Debug.Assert(_owner is not null);
        SemanticsOwner owner = _owner!;
        owner.UnregisterNode(this);
        if (_traversalChildIdentifier is { } identifier)
        {
            // The node this one grafted onto has to re-serialize its traversal children without it.
            owner.GetTraversalParentNode(identifier)?.MarkDirty();
        }

        owner.ForgetTraversalRegistrations(this);
        _owner = null;
        Debug.Assert(Parent is null || Attached == Parent.Attached);
        foreach (SemanticsNode child in _children)
        {
            if (ReferenceEquals(child.Parent, this))
            {
                child.Detach();
            }
        }

        // Detached, so this only records the flag; the node is re-enqueued by the next Attach.
        MarkDirty();
    }

    /// <remarks>Flutter's private <c>SemanticsNode._markDirty</c>.</remarks>
    internal void MarkDirty()
    {
        if (_dirty)
        {
            return;
        }

        _dirty = true;
        if (Attached)
        {
            Debug.Assert(!_owner!.DebugIsDetached(this));
            _owner!.AddDirtyNode(this);
        }
    }

    /// <summary>Whether this node is waiting to be sent to the platform, or <c>null</c> in release.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.debugIsDirty</c>.</remarks>
    public bool? DebugIsDirty => Constants.KDebugMode ? _dirty : null;

    internal bool IsDirty => _dirty;

    internal void ClearDirty() => _dirty = false;

    /// <summary>
    /// Whether <paramref name="config"/> annotates anything differently from what this node holds.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>SemanticsNode._isDifferentFromCurrentSemanticAnnotation</c>. Fields
    /// Flutter assigns but does not compare (the action handler map, the scroll child count/index,
    /// the input type and the tags) are deliberately absent here too — only the action <em>bits</em>
    /// participate, so swapping a handler never re-sends the node.
    /// </remarks>
    private bool IsDifferentFromCurrentSemanticAnnotation(SemanticsConfiguration config)
    {
        return Label != config.Label
               || Hint != config.Hint
               || Value != config.Value
               || IncreasedValue != config.IncreasedValue
               || DecreasedValue != config.DecreasedValue
               || Tooltip != config.Tooltip
               || Flags != config.Flags
               || TextDirection != config.TextDirection
               || !Equals(SortKey, config.SortKey)
               || ScrollPosition != config.ScrollPosition
               || ScrollExtentMax != config.ScrollExtentMax
               || ScrollExtentMin != config.ScrollExtentMin
               || Actions != (config.IsBlockingUserActions ? SemanticsActions.None : config.Actions)
               || IndexInParent != config.IndexInParent
               || MergeAllDescendantsIntoThisNode != config.IsMergingSemanticsOfDescendants
               || AreUserActionsBlocked != config.IsBlockingUserActions
               || Role != config.Role
               || HitTestBehavior != config.HitTestBehavior
               || !Equals(_traversalChildIdentifier, config.TraversalChildIdentifier)
               || !Equals(_traversalParentIdentifier, config.TraversalParentIdentifier)
               || MinValue != config.MinValue
               || MaxValue != config.MaxValue
               || !MapEquals(_customActionHandlers, config.CustomActionHandlers);
    }

    /// <remarks>Flutter's <c>mapEquals</c> from `foundation/collections.dart`.</remarks>
    private static bool MapEquals(
        IReadOnlyDictionary<CustomSemanticsAction, Action> a,
        IReadOnlyDictionary<CustomSemanticsAction, Action> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach ((CustomSemanticsAction action, Action handler) in a)
        {
            if (!b.TryGetValue(action, out Action? other) || !Equals(handler, other))
            {
                return false;
            }
        }

        return true;
    }

    // ---- Traversal grafting -------------------------------------------------------------------

    private object? _traversalParentIdentifier;
    private object? _traversalChildIdentifier;
    private SemanticsNode? _traversalParent;
    private Matrix4? _traversalChildTransform;

    /// <summary>
    /// The identifier other nodes name in their <see cref="TraversalChildIdentifier"/> to be
    /// traversed as this node's children.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.traversalParentIdentifier</c>. It must be unique across the tree.
    /// </remarks>
    public object? TraversalParentIdentifier => _traversalParentIdentifier;

    /// <summary>
    /// The <see cref="TraversalParentIdentifier"/> of the node this one is traversed under, even
    /// though it sits elsewhere in paint order.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.traversalChildIdentifier</c>. Many nodes may share one value.
    /// </remarks>
    public object? TraversalChildIdentifier => _traversalChildIdentifier;

    internal object? TraversalParentIdentifierValue
    {
        get => _traversalParentIdentifier;
        set => _traversalParentIdentifier = value;
    }

    internal object? TraversalChildIdentifierValue
    {
        get => _traversalChildIdentifier;
        set => _traversalChildIdentifier = value;
    }

    internal bool IsTraversalParent => _traversalParentIdentifier is not null;

    internal bool IsTraversalChild => _traversalChildIdentifier is not null;

    /// <remarks>Flutter's <c>SemanticsNode.traversalParent</c>; falls back to the paint parent.</remarks>
    internal SemanticsNode? TraversalParent
    {
        get => _traversalParent ?? Parent;
        set
        {
            if (ReferenceEquals(_traversalParent, value))
            {
                return;
            }

            _traversalParent = value;
            MarkDirty();
        }
    }

    /// <summary>
    /// The transform the platform sorts this node with: the graft-corrected one when the node is
    /// traversed under a parent it does not paint under.
    /// </summary>
    /// <remarks>Flutter's private <c>SemanticsNode._traversalTransform</c>.</remarks>
    internal Matrix4? TraversalTransform =>
        PlatformDefaults.IsWeb ? Transform : _traversalChildTransform ?? Transform;

    /// <remarks>Flutter's private <c>SemanticsNode._computeTraversalTransform</c>.</remarks>
    private static Matrix4 ComputeTraversalTransform(SemanticsNode parent, SemanticsNode child)
    {
        Matrix4 traversalTransform = Matrix4.Identity();
        Matrix4? parentToCommonAncestorTransform = null;
        SemanticsNode fromNode = child;
        SemanticsNode toNode = parent;
        while (!ReferenceEquals(fromNode, toNode))
        {
            int fromDepth = fromNode.Depth;
            int toDepth = toNode.Depth;
            if (fromDepth >= toDepth)
            {
                if (fromNode.Transform is { } fromTransform)
                {
                    traversalTransform.Multiply(fromTransform);
                }

                if (fromNode.Parent is not { } fromParent)
                {
                    return traversalTransform;
                }

                fromNode = fromParent;
            }

            if (fromDepth <= toDepth)
            {
                parentToCommonAncestorTransform ??= Matrix4.Identity();
                if (toNode.Transform is { } toTransform)
                {
                    parentToCommonAncestorTransform.Multiply(toTransform);
                }

                if (toNode.Parent is not { } toParent)
                {
                    break;
                }

                toNode = toParent;
            }
        }

        if (parentToCommonAncestorTransform is not null)
        {
            if (parentToCommonAncestorTransform.Invert() != 0.0)
            {
                traversalTransform.Multiply(parentToCommonAncestorTransform);
            }
            else
            {
                traversalTransform.SetZero();
            }
        }

        return traversalTransform;
    }

    /// <summary>
    /// This node's children after grafting: children traversed elsewhere are removed, and nodes
    /// naming this node's <see cref="TraversalParentIdentifier"/> are appended.
    /// </summary>
    /// <remarks>Flutter's private <c>SemanticsNode._updateChildrenInTraversalOrder</c>.</remarks>
    internal IReadOnlyList<SemanticsNode> UpdateChildrenInTraversalOrder()
    {
        if (PlatformDefaults.IsWeb)
        {
            // The web engine grafts through ARIA `aria-owns`, so both child lists stay identical.
            return _children;
        }

        var updatedChildren = new List<SemanticsNode>(_children.Count);
        foreach (SemanticsNode child in _children)
        {
            if (child.IsTraversalChild && !IsTraversalParent)
            {
                SemanticsNode? traversalParent = _owner?.GetTraversalParentNode(child.TraversalChildIdentifier!);
                for (SemanticsNode? node = traversalParent; node is not null; node = node.Parent)
                {
                    if (ReferenceEquals(node, child))
                    {
                        throw new FlutterError(
                            $"The traversalParent {traversalParent!.Id} cannot be the child of the "
                            + $"traversalChild {child.Id} in hit-test order");
                    }
                }

                continue;
            }

            updatedChildren.Add(child);
        }

        if (IsTraversalParent
            && _owner?.GetTraversalChildNodes(_traversalParentIdentifier!) is { } traversalChildren)
        {
            for (SemanticsNode? node = Parent; node is not null; node = node.Parent)
            {
                if (traversalChildren.Contains(node))
                {
                    throw new FlutterError(
                        $"The traversalParent {Id} cannot be the child of the traversalChild "
                        + $"{node.Id} in hit-test order");
                }
            }

            foreach (SemanticsNode child in traversalChildren)
            {
                if (child.Attached)
                {
                    updatedChildren.Add(child);
                }
            }
        }

        return updatedChildren;
    }

    /// <summary>
    /// This node's children in hit-test order, with grafted children whose traversal parent never
    /// registered dropped so the two trees stay in sync.
    /// </summary>
    /// <remarks>Flutter's private <c>SemanticsNode._childrenInHitTestOrder</c>.</remarks>
    public IReadOnlyList<SemanticsNode> ChildrenInHitTestOrder
    {
        get
        {
            if (PlatformDefaults.IsWeb || IsTraversalParent)
            {
                return _children;
            }

            var result = new List<SemanticsNode>(_children.Count);
            foreach (SemanticsNode child in _children)
            {
                if (child.IsTraversalChild
                    && _owner?.GetTraversalParentNode(child.TraversalChildIdentifier!) is null)
                {
                    continue;
                }

                result.Add(child);
            }

            return result;
        }
    }

    /// <remarks>Flutter's private <c>SemanticsNode._computeTraversalTransform</c> call in `_addToUpdate`.</remarks>
    internal void ResolveTraversalParent()
    {
        if (_traversalChildIdentifier is not { } identifier)
        {
            return;
        }

        TraversalParent = _owner?.GetTraversalParentNode(identifier);
        if (!PlatformDefaults.IsWeb && TraversalParent is { } traversalParent)
        {
            _traversalChildTransform = ComputeTraversalTransform(traversalParent, this);
        }
    }
}
