using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// <summary>
/// The parent-supplied context a <see cref="RenderObjectSemantics"/> compiles itself against.
/// </summary>
/// <remarks>Flutter's private <c>_SemanticsParentData</c>.</remarks>
internal sealed record SemanticsParentData(
    bool MergeIntoParent,
    bool BlocksUserActions,
    bool ExplicitChildNodes,
    IReadOnlySet<SemanticsTag>? TagsForChildren,
    AccessibilityFocusBlockType? AccessibilityFocusBlockType = null)
{
    public bool Equals(SemanticsParentData? other)
    {
        if (other is null)
        {
            return false;
        }

        return MergeIntoParent == other.MergeIntoParent
               && BlocksUserActions == other.BlocksUserActions
               && ExplicitChildNodes == other.ExplicitChildNodes
               && AccessibilityFocusBlockType == other.AccessibilityFocusBlockType
               && TagSetsEqual(TagsForChildren, other.TagsForChildren);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            MergeIntoParent,
            BlocksUserActions,
            ExplicitChildNodes,
            AccessibilityFocusBlockType,
            TagsForChildren?.Count ?? 0);
    }

    private static bool TagSetsEqual(IReadOnlySet<SemanticsTag>? left, IReadOnlySet<SemanticsTag>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        return left.All(right.Contains);
    }
}

/// <summary>
/// The geometry of one <see cref="SemanticsNode"/>: its box in its own coordinates plus the
/// transform into the parent node's coordinates.
/// </summary>
/// <remarks>Flutter's private <c>_SemanticsGeometry</c>.</remarks>
internal sealed record SemanticsGeometry(
    Rect? PaintClipRect,
    Rect? SemanticsClipRect,
    Matrix4 Transform,
    Rect Rect,
    bool Hidden)
{
    public static SemanticsGeometry Root(Rect rect) =>
        new(PaintClipRect: null, SemanticsClipRect: null, Transform: Matrix4.Identity(), Rect: rect, Hidden: false);

    public bool IsVisible => Rect.Width > 0 && Rect.Height > 0 && !IsZero(Transform);

    private static bool IsZero(Matrix4 transform) => transform.IsZero();

    /// <summary>
    /// Accumulates the transform and clips from <paramref name="parent"/>'s render object down to
    /// <paramref name="child"/>'s, and resolves the clips back into the child's own coordinates.
    /// </summary>
    /// <remarks>Flutter's <c>_SemanticsGeometry.computeChildGeometry</c>.</remarks>
    public static SemanticsGeometry ComputeChildGeometry(
        Matrix4? parentTransform,
        Rect? parentPaintClipRect,
        Rect? parentSemanticsClipRect,
        RenderObjectSemantics parent,
        RenderObjectSemantics child)
    {
        RenderObject childRenderObject = child.RenderObject;
        RenderObject parentRenderObject = parent.RenderObject;

        var childToCommonAncestor = new List<RenderObject> { childRenderObject };
        while (childRenderObject.Depth > parentRenderObject.Depth && childRenderObject.Parent is { } next)
        {
            childRenderObject = next;
            childToCommonAncestor.Add(childRenderObject);
        }

        Rect? paintClipRect = null;
        Rect? semanticsClipRect = null;
        Matrix4 transform = Matrix4.Identity();
        // Walk from `parent`'s render object down to `child`'s, accumulating the paint transform and
        // both clips in the coordinate space of the render object the walk started from.
        for (int index = childToCommonAncestor.Count - 1; index > 0; index--)
        {
            RenderObject nodeParent = childToCommonAncestor[index];
            RenderObject node = childToCommonAncestor[index - 1];

            Rect? localPaintClipInParent =
                TransformClip(nodeParent.InvokeDescribeApproximatePaintClip(node), transform);
            Rect? localSemanticsClipInParent =
                TransformClip(nodeParent.InvokeDescribeSemanticsClip(node), transform);
            paintClipRect = IntersectRects(paintClipRect, localPaintClipInParent);
            semanticsClipRect = localSemanticsClipInParent
                                ?? IntersectRects(semanticsClipRect, localPaintClipInParent ?? semanticsClipRect);
            nodeParent.ApplyPaintTransform(node, transform);
        }

        semanticsClipRect ??= IntersectRects(paintClipRect, parentSemanticsClipRect);
        paintClipRect = IntersectRects(paintClipRect, parentPaintClipRect);

        if (paintClipRect is not null || semanticsClipRect is not null)
        {
            Matrix4 inverted = Matrix4.Copy(transform);
            bool hasInverse = inverted.Invert() != 0.0;
            semanticsClipRect = hasInverse ? TransformClip(semanticsClipRect, inverted) : null;
            paintClipRect = hasInverse ? TransformClip(paintClipRect, inverted) : null;
        }

        if (parentTransform is { } ancestorTransform)
        {
            MatrixUtils.MultiplyInPlace(ancestorTransform, transform);
        }

        Rect semanticBounds = child.RenderObject.SemanticBoundsForSemantics;
        Rect rect = semanticsClipRect is { } clip ? Intersect(clip, semanticBounds) : semanticBounds;
        bool isRectHidden = false;
        if (paintClipRect is { } paintClip)
        {
            Rect paintRect = Intersect(paintClip, rect);
            isRectHidden = IsEmpty(paintRect) && !IsEmpty(rect);
            if (!isRectHidden)
            {
                rect = paintRect;
            }
        }

        return new SemanticsGeometry(
            PaintClipRect: paintClipRect,
            SemanticsClipRect: semanticsClipRect,
            Transform: transform,
            Rect: rect,
            Hidden: isRectHidden);
    }

    internal static Rect? TransformClip(Rect? rect, Matrix4 transform) =>
        rect is { } value ? SemanticsNode.TransformRect(transform, value) : null;

    internal static Rect? IntersectRects(Rect? a, Rect? b)
    {
        if (a is null)
        {
            return b;
        }

        return b is null ? a : Intersect(a.Value, b.Value);
    }

    internal static Rect Intersect(Rect a, Rect b)
    {
        double left = Math.Max(a.X, b.X);
        double top = Math.Max(a.Y, b.Y);
        double right = Math.Min(a.Right, b.Right);
        double bottom = Math.Min(a.Bottom, b.Bottom);
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }

    internal static bool IsEmpty(Rect rect) => rect.Width <= 0 || rect.Height <= 0;
}

/// <summary>
/// A piece of semantics that either merges its configuration into an ancestor or forms a node of
/// its own.
/// </summary>
/// <remarks>Flutter's private <c>_SemanticsFragment</c>.</remarks>
internal interface ISemanticsFragment
{
    /// <summary>
    /// The configuration this fragment contributes to its parent, or <c>null</c> when it forms a node.
    /// </summary>
    SemanticsConfiguration? ConfigToMergeUp { get; }

    /// <summary>The render object semantics this fragment belongs to.</summary>
    RenderObjectSemantics Owner { get; }

    /// <summary>Whether this fragment ends up merged into a sibling node instead of the parent.</summary>
    bool MergesToSibling { get; set; }

    /// <summary>Records whether this fragment conflicts with another fragment in the same merge group.</summary>
    void MarkSiblingConfigurationConflict(bool conflict);
}

/// <summary>
/// A fragment a <see cref="SemanticsConfiguration.ChildConfigurationsDelegate"/> produced that is
/// not backed by a render object of its own.
/// </summary>
/// <remarks>Flutter's private <c>_IncompleteSemanticsFragment</c>.</remarks>
internal sealed class IncompleteSemanticsFragment(
    SemanticsConfiguration configToMergeUp,
    RenderObjectSemantics owner) : ISemanticsFragment
{
    public SemanticsConfiguration? ConfigToMergeUp { get; } = configToMergeUp;

    public RenderObjectSemantics Owner { get; } = owner;

    public bool MergesToSibling { get; set; }

    public void MarkSiblingConfigurationConflict(bool conflict)
    {
    }
}

/// <summary>
/// The semantics compiler for one render object: it decides whether the render object merges its
/// configuration up or forms a <see cref="SemanticsNode"/>, computes that node's geometry, and
/// produces the node subtree.
/// </summary>
/// <remarks>
/// Flutter's private <c>_RenderObjectSemantics</c>. The tree is compiled in four phases, and each
/// phase needs the previous one to have finished across the whole tree:
/// <list type="number">
/// <item>walk the render tree and gather the fragments that contribute to semantics
/// (<see cref="UpdateChildren"/>);</item>
/// <item>merge those fragments and decide which render objects form nodes (also
/// <see cref="UpdateChildren"/>);</item>
/// <item>compute the geometry of the render objects that form nodes
/// (<see cref="EnsureGeometry"/>);</item>
/// <item>produce the semantics nodes (<see cref="EnsureSemanticsNode"/>).</item>
/// </list>
/// </remarks>
internal sealed class RenderObjectSemantics : DiagnosticableTree, ISemanticsFragment
{
    private readonly List<RenderObjectSemantics> _children = [];
    private readonly List<ISemanticsFragment> _mergeUp = [];
    private readonly List<List<ISemanticsFragment>> _siblingMergeGroups = [];
    private readonly Dictionary<SemanticsNode, List<ISemanticsFragment>> _producedSiblingNodesAndOwners = [];
    private readonly List<SemanticsNode> _semanticsNodes = [];
    private bool? _blocksPreviousSibling;
    private bool _containsIncompleteFragment;
    private bool _hasSiblingConflict;

    public RenderObjectSemantics(RenderObject renderObject)
    {
        RenderObject = renderObject;
        ConfigProvider = new SemanticsConfigurationProvider(
            renderObject.InvokeDescribeSemanticsConfiguration,
            ValidateSemanticsConfiguration);
    }

    public RenderObject RenderObject { get; }

    public SemanticsConfigurationProvider ConfigProvider { get; }

    /// <summary>The node this render object created, kept so its id stays stable across updates.</summary>
    public SemanticsNode? CachedSemanticsNode { get; private set; }

    /// <summary>Every node this render object produced: its own plus the sibling-group nodes.</summary>
    public IReadOnlyList<SemanticsNode> SemanticsNodes => _semanticsNodes;

    /// <summary>Whether the node subtree below this render object is up to date.</summary>
    public bool Built { get; private set; }

    public SemanticsParentData? ParentData { get; private set; }

    public SemanticsGeometry? Geometry { get; set; }

    /// <summary>The render object semantics whose node is this one's parent in the semantics tree.</summary>
    public RenderObjectSemantics? ParentInSemanticsTree { get; private set; }

    /// <summary>The children that form semantics nodes of their own.</summary>
    public IReadOnlyList<RenderObjectSemantics> NodeFormingChildren => _children;

    /// <summary>The render-object-backed fragments this object currently merges up.</summary>
    public IEnumerable<RenderObjectSemantics> MergeUpRenderObjectSemantics => _mergeUp.OfType<RenderObjectSemantics>();

    /// <summary>The nearest node-forming ancestor whose geometry is up to date.</summary>
    public RenderObjectSemantics? FirstAncestorNodeWithCleanGeometry { get; private set; }

    private object _currentTreeShapeToken = new();

    /// <summary>
    /// Finds and caches the nearest node-forming ancestor with clean geometry.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>_RenderObjectSemantics.computeAncestorInfo</c>. <paramref name="treeShapeToken"/>
    /// scopes the cache to a single flush, so a set of siblings that share an ancestor only pays for
    /// the lookup once.
    /// </remarks>
    public void ComputeAncestorInfo(object treeShapeToken)
    {
        if (ReferenceEquals(treeShapeToken, _currentTreeShapeToken))
        {
            return;
        }

        _currentTreeShapeToken = treeShapeToken;
        if (IsRoot)
        {
            FirstAncestorNodeWithCleanGeometry = this;
            return;
        }

        FirstAncestorNodeWithCleanGeometry = null;
        if (ParentDataDirty)
        {
            return;
        }

        RenderObjectSemantics? next;
        if (ShouldFormSemanticsNode)
        {
            if (!GeometryDirty)
            {
                FirstAncestorNodeWithCleanGeometry = this;
            }

            next = ParentInSemanticsTree;
        }
        else
        {
            next = this;
            while (!next!.ParentDataDirty && !next.ShouldFormSemanticsNode)
            {
                next = next.Parent;
            }
        }

        if (next == null)
        {
            return;
        }

        if (FirstAncestorNodeWithCleanGeometry == null)
        {
            next.ComputeAncestorInfo(treeShapeToken);
            FirstAncestorNodeWithCleanGeometry = next.FirstAncestorNodeWithCleanGeometry;
        }
    }

    public RenderObjectSemantics Owner => this;

    public bool MergesToSibling { get; set; }

    /// <summary>The parent in render-object order.</summary>
    private RenderObjectSemantics? Parent => RenderObject.Parent?.Semantics;

    public bool IsRoot => RenderObject.Parent == null;

    public bool ParentDataDirty => !IsRoot && ParentData == null;

    public bool GeometryDirty => !IsRoot && Geometry == null;

    /// <summary>
    /// A render object that forms a node contributes everything through that node, so there is
    /// nothing left to merge up.
    /// </summary>
    public SemanticsConfiguration? ConfigToMergeUp =>
        ShouldFormSemanticsNode ? null : ConfigProvider.Effective;

    public bool ContributesToSemanticsTree =>
        ConfigProvider.Effective.HasBeenAnnotated
        || _containsIncompleteFragment
        || ConfigProvider.Effective.IsSemanticBoundary
        || IsRoot;

    private bool NeedsMergingSiblingNodesIntoSelf =>
        ConfigProvider.Effective.IsMergingSemanticsOfDescendants && _producedSiblingNodesAndOwners.Count > 0;

    public bool ShouldFormSemanticsNode
    {
        get
        {
            if (ConfigProvider.Effective.IsSemanticBoundary || IsRoot)
            {
                return true;
            }

            if (!ContributesToSemanticsTree)
            {
                return false;
            }

            return ParentData!.ExplicitChildNodes || _hasSiblingConflict;
        }
    }

    /// <summary>
    /// Whether this render object hides the semantics of everything painted before it under the same
    /// parent, which is what <c>BlockSemantics</c> does.
    /// </summary>
    public bool IsBlockingPreviousSibling
    {
        get
        {
            if (_blocksPreviousSibling.HasValue)
            {
                return _blocksPreviousSibling.Value;
            }

            _blocksPreviousSibling = ConfigProvider.Effective.IsBlockingSemanticsOfPreviouslyPaintedNodes;
            if (_blocksPreviousSibling.Value)
            {
                return true;
            }

            if (ConfigProvider.Effective.IsSemanticBoundary)
            {
                return false;
            }

            RenderObject.VisitChildrenForSemantics(child =>
            {
                if (child.Semantics.IsBlockingPreviousSibling)
                {
                    _blocksPreviousSibling = true;
                }
            });

            return _blocksPreviousSibling!.Value;
        }
    }

    public void MarkSiblingConfigurationConflict(bool conflict)
    {
        _hasSiblingConflict = conflict;
    }

    private static bool ShouldDrop(SemanticsNode node) => node.IsInvisible;

    public void MarkNeedsBuild()
    {
        Built = false;
        if (!ParentDataDirty && !ShouldFormSemanticsNode)
        {
            return;
        }

        foreach (List<ISemanticsFragment> group in _siblingMergeGroups)
        {
            foreach (ISemanticsFragment fragment in group)
            {
                if (fragment is not RenderObjectSemantics semantics || semantics.ParentDataDirty)
                {
                    continue;
                }

                if (!semantics.ShouldFormSemanticsNode)
                {
                    semantics.MarkNeedsBuild();
                }
            }
        }
    }

    // ---- phase 1 and 2 -------------------------------------------------------------------------

    /// <summary>
    /// Gathers the fragments that contribute to the semantics tree, merges the ones that do not form
    /// nodes into this configuration, and records the ones that do in <see cref="_children"/>.
    /// </summary>
    public void UpdateChildren()
    {
        ConfigProvider.Reset();
        IReadOnlySet<SemanticsTag>? tagsForChildren = GetTagsForChildren();
        bool explicitChildNodesForChildren =
            IsRoot
            || ConfigProvider.Effective.ExplicitChildNodes
            // The parent's explicit-child-nodes only propagates through render objects that do not
            // contribute to the semantics tree themselves.
            || (!ContributesToSemanticsTree && (ParentData?.ExplicitChildNodes ?? true));

        bool blocksUserAction = (ParentData?.BlocksUserActions ?? false)
                                || ConfigProvider.Effective.IsBlockingUserActions;

        // Only `BlockSubtree` is inherited: it overrides whatever the child declared, while
        // `BlockNode` stops at the node that declared it.
        AccessibilityFocusBlockType accessibilityFocusBlockType =
            ParentData?.AccessibilityFocusBlockType == Rendering.AccessibilityFocusBlockType.BlockSubtree
                ? Rendering.AccessibilityFocusBlockType.BlockSubtree
                : ConfigProvider.Effective.AccessibilityFocusBlockType;

        _siblingMergeGroups.Clear();
        _mergeUp.Clear();
        var childParentData = new SemanticsParentData(
            MergeIntoParent: (ParentData?.MergeIntoParent ?? false)
                             || ConfigProvider.Effective.IsMergingSemanticsOfDescendants,
            BlocksUserActions: blocksUserAction,
            ExplicitChildNodes: explicitChildNodesForChildren,
            TagsForChildren: tagsForChildren,
            AccessibilityFocusBlockType: accessibilityFocusBlockType);

        (List<ISemanticsFragment> mergeUp, List<List<ISemanticsFragment>> siblingMergeGroups) result =
            CollectChildMergeUpAndSiblingGroup(childParentData);
        _mergeUp.AddRange(result.mergeUp);
        _siblingMergeGroups.AddRange(result.siblingMergeGroups);

        var oldChildren = new HashSet<RenderObjectSemantics>(_children);
        _children.Clear();
        if (!ContributesToSemanticsTree)
        {
            return;
        }

        MarksConflictsInMergeGroup(_mergeUp, isMergeUp: true);
        foreach (List<ISemanticsFragment> group in _siblingMergeGroups)
        {
            MarksConflictsInMergeGroup(group);
        }

        ConfigProvider.AbsorbAll(_mergeUp
            .Select(static fragment => fragment.ConfigToMergeUp)
            .OfType<SemanticsConfiguration>());

        // The merge-up fragments below this render object are invisible to the parent now: they were
        // either absorbed above or will form a node of their own.
        _mergeUp.Clear();
        _mergeUp.Add(this);
        foreach (ISemanticsFragment fragment in result.mergeUp)
        {
            if (fragment is not RenderObjectSemantics childSemantics)
            {
                continue;
            }

            if (childSemantics.ShouldFormSemanticsNode)
            {
                foreach (RenderObjectSemantics grandChild in childSemantics._children)
                {
                    grandChild.ParentInSemanticsTree = childSemantics;
                }

                if (childSemantics.GeometryDirty)
                {
                    RenderObject.Owner?.RequestSemanticsGeometryUpdateFor(childSemantics.RenderObject);
                }

                _children.Add(childSemantics);
            }
            else
            {
                _children.AddRange(childSemantics._children);
                _siblingMergeGroups.AddRange(childSemantics._siblingMergeGroups);
            }
        }

        // Whether this render object forms a node is only known once its own parent has decided,
        // unless it is the root or an explicit boundary.
        if (IsRoot || ConfigProvider.Effective.IsSemanticBoundary)
        {
            foreach (RenderObjectSemantics child in _children)
            {
                child.ParentInSemanticsTree = this;
            }
        }

        oldChildren.ExceptWith(_children);
        foreach (RenderObjectSemantics removedChild in oldChildren)
        {
            if (ReferenceEquals(removedChild.ParentInSemanticsTree, this))
            {
                removedChild.ParentInSemanticsTree = null;
            }
        }

        if (ParentData?.TagsForChildren is { Count: > 0 } tags)
        {
            ConfigProvider.UpdateConfig(configuration =>
            {
                foreach (SemanticsTag tag in tags)
                {
                    configuration.AddTagForChildren(tag);
                }
            });
        }

        if (accessibilityFocusBlockType != ConfigProvider.Effective.AccessibilityFocusBlockType)
        {
            ConfigProvider.UpdateConfig(
                configuration => configuration.AccessibilityFocusBlockType = accessibilityFocusBlockType);
        }

        if (blocksUserAction != ConfigProvider.Effective.IsBlockingUserActions)
        {
            ConfigProvider.UpdateConfig(configuration => configuration.IsBlockingUserActions = blocksUserAction);
        }

        // A node whose accessibility focus is blocked must not report itself as keyboard focusable
        // either, so Flutter clears the tri-state `isFocused` outright.
        if (accessibilityFocusBlockType != Rendering.AccessibilityFocusBlockType.None)
        {
            ConfigProvider.UpdateConfig(configuration => configuration.IsFocused = null);
        }
    }

    private List<RenderObjectSemantics> GetNonBlockedChildren()
    {
        var result = new List<RenderObjectSemantics>();
        RenderObject.VisitChildrenForSemantics(renderChild =>
        {
            if (renderChild.Semantics.IsBlockingPreviousSibling)
            {
                result.Clear();
            }

            result.Add(renderChild.Semantics);
        });

        return result;
    }

    private IReadOnlySet<SemanticsTag>? GetTagsForChildren()
    {
        if (ContributesToSemanticsTree)
        {
            return ConfigProvider.Original.TagsForChildren is { Count: > 0 } own
                ? new HashSet<SemanticsTag>(own)
                : null;
        }

        HashSet<SemanticsTag>? result = null;
        if (ConfigProvider.Original.TagsForChildren is { Count: > 0 } originalTags)
        {
            result = [.. originalTags];
        }

        if (ParentData?.TagsForChildren is { Count: > 0 } inheritedTags)
        {
            if (result == null)
            {
                return inheritedTags;
            }

            result.UnionWith(inheritedTags);
        }

        return result;
    }

    private (List<ISemanticsFragment>, List<List<ISemanticsFragment>>) CollectChildMergeUpAndSiblingGroup(
        SemanticsParentData childParentData)
    {
        var mergeUp = new List<ISemanticsFragment>();
        var siblingMergeGroups = new List<List<ISemanticsFragment>>();

        var childConfigurations = new List<SemanticsConfiguration>();
        ChildSemanticsConfigurationsDelegate? childConfigurationsDelegate =
            ConfigProvider.Effective.ChildConfigurationsDelegate;
        bool hasChildConfigurationsDelegate = childConfigurationsDelegate != null;
        var configToFragment = new Dictionary<SemanticsConfiguration, ISemanticsFragment>();

        // A delegate may produce incomplete fragments, in which case this render object has to absorb
        // every merge-up from its children before presenting itself to its own parent, so that the
        // parent does not force an incomplete fragment to form a node. Whether the delegate does that
        // is only known after it runs, but the decision to propagate the parent's explicit-child-nodes
        // has to be made before the child fragments are collected — so assume it does, and redo the
        // collection below when the assumption turns out to be wrong.
        bool needsToMakeIncompleteFragmentAssumption =
            hasChildConfigurationsDelegate && childParentData.ExplicitChildNodes;

        SemanticsParentData effectiveChildParentData = needsToMakeIncompleteFragmentAssumption
            ? childParentData with { ExplicitChildNodes = false }
            : childParentData;

        foreach (RenderObjectSemantics childSemantics in GetNonBlockedChildren())
        {
            childSemantics.DidUpdateParentData(effectiveChildParentData);
            foreach (ISemanticsFragment fragment in childSemantics._mergeUp)
            {
                if (hasChildConfigurationsDelegate && fragment.ConfigToMergeUp != null)
                {
                    // This fragment has to go through the delegate to learn whether it merges up.
                    childConfigurations.Add(fragment.ConfigToMergeUp);
                    configToFragment[fragment.ConfigToMergeUp] = fragment;
                }
                else
                {
                    mergeUp.Add(fragment);
                }
            }

            if (!childSemantics.ContributesToSemanticsTree)
            {
                // The sibling merge groups have to travel up to the nearest render object that does
                // contribute, because that is what compiles them.
                siblingMergeGroups.AddRange(childSemantics._siblingMergeGroups);
            }
        }

        _containsIncompleteFragment = false;
        if (childConfigurationsDelegate != null)
        {
            ChildSemanticsConfigurationsResult result = childConfigurationsDelegate(childConfigurations);
            mergeUp.AddRange(result.MergeUp.Select(ResolveFragment));
            foreach (List<SemanticsConfiguration> group in result.SiblingMergeGroups)
            {
                siblingMergeGroups.Add([.. group.Select(ResolveFragment)]);
            }
        }

        if (!_containsIncompleteFragment && needsToMakeIncompleteFragmentAssumption)
        {
            // The assumption was wrong, so the children have to be re-collected with the real value.
            mergeUp.Clear();
            siblingMergeGroups.Clear();
            foreach (RenderObjectSemantics childSemantics in GetNonBlockedChildren())
            {
                childSemantics.DidUpdateParentData(childParentData);
                mergeUp.AddRange(childSemantics._mergeUp);
                if (!childSemantics.ContributesToSemanticsTree)
                {
                    siblingMergeGroups.AddRange(childSemantics._siblingMergeGroups);
                }
            }
        }

        return (mergeUp, siblingMergeGroups);

        ISemanticsFragment ResolveFragment(SemanticsConfiguration configuration)
        {
            if (configToFragment.TryGetValue(configuration, out ISemanticsFragment? fragment))
            {
                return fragment;
            }

            _containsIncompleteFragment = true;
            return new IncompleteSemanticsFragment(configuration, this);
        }
    }

    private void DidUpdateParentData(SemanticsParentData newParentData)
    {
        if (ParentData == newParentData)
        {
            return;
        }

        // A parent-data change can flip whether this render object forms a node.
        MarkNeedsBuild();
        ParentData = newParentData;
        UpdateChildren();
    }

    /// <summary>
    /// Marks every fragment in <paramref name="mergeGroup"/> that cannot be merged with one of its
    /// predecessors, which forces it to form a node of its own.
    /// </summary>
    private void MarksConflictsInMergeGroup(List<ISemanticsFragment> mergeGroup, bool isMergeUp = false)
    {
        var hasSiblingConflict = new HashSet<ISemanticsFragment>();
        for (int i = 0; i < mergeGroup.Count; i++)
        {
            ISemanticsFragment fragment = mergeGroup[i];
            fragment.MarkSiblingConfigurationConflict(false);
            if (fragment.ConfigToMergeUp == null)
            {
                continue;
            }

            if (isMergeUp && !ConfigProvider.Original.IsCompatibleWith(fragment.ConfigToMergeUp))
            {
                hasSiblingConflict.Add(fragment);
            }

            for (int j = 0; j < i; j++)
            {
                ISemanticsFragment siblingFragment = mergeGroup[j];
                if (!fragment.ConfigToMergeUp.IsCompatibleWith(siblingFragment.ConfigToMergeUp))
                {
                    hasSiblingConflict.Add(fragment);
                    hasSiblingConflict.Add(siblingFragment);
                }
            }
        }

        foreach (ISemanticsFragment fragment in hasSiblingConflict)
        {
            fragment.MarkSiblingConfigurationConflict(true);
        }
    }

    // ---- phase 3 -------------------------------------------------------------------------------

    /// <summary>Updates the geometry of this render object and its dirty node-forming descendants.</summary>
    public void EnsureGeometry()
    {
        if (IsRoot)
        {
            if (Geometry?.Rect != RenderObject.SemanticBoundsForSemantics)
            {
                MarkNeedsBuild();
            }

            Geometry = SemanticsGeometry.Root(RenderObject.SemanticBoundsForSemantics);
        }

        UpdateChildGeometry(onlyDirtyChildren: true);
    }

    private void UpdateChildGeometry(bool onlyDirtyChildren = false)
    {
        SemanticsGeometry parentGeometry = Geometry!;
        foreach (RenderObjectSemantics child in _children)
        {
            if (onlyDirtyChildren && !child.GeometryDirty)
            {
                continue;
            }

            child.UpdateGeometry(SemanticsGeometry.ComputeChildGeometry(
                parentTransform: null,
                parentPaintClipRect: parentGeometry.PaintClipRect,
                parentSemanticsClipRect: parentGeometry.SemanticsClipRect,
                parent: this,
                child: child));
        }

        foreach (RenderObjectSemantics explicitSiblingChild in _siblingMergeGroups
                     .SelectMany(static group => group)
                     .OfType<RenderObjectSemantics>()
                     .SelectMany(static siblingChild => siblingChild.ShouldFormSemanticsNode
                         ? [siblingChild]
                         : siblingChild._children))
        {
            if (onlyDirtyChildren && !explicitSiblingChild.GeometryDirty)
            {
                continue;
            }

            explicitSiblingChild.UpdateGeometry(SemanticsGeometry.ComputeChildGeometry(
                parentTransform: parentGeometry.Transform,
                parentPaintClipRect: parentGeometry.PaintClipRect,
                parentSemanticsClipRect: parentGeometry.SemanticsClipRect,
                parent: this,
                child: explicitSiblingChild));
        }
    }

    private void UpdateGeometry(SemanticsGeometry newGeometry)
    {
        SemanticsGeometry? currentGeometry = Geometry;
        Geometry = newGeometry;
        if (currentGeometry != null)
        {
            bool isSemanticsHidden = ConfigProvider.Original.IsHidden
                                     || (!(ParentData?.MergeIntoParent ?? false) && newGeometry.Hidden);
            bool sizeChanged = currentGeometry.Rect.Size != newGeometry.Rect.Size;
            bool visibilityChanged = ConfigProvider.Effective.IsHidden != isSemanticsHidden;
            if (!sizeChanged && !visibilityChanged)
            {
                return;
            }
        }

        MarkNeedsBuild();
        UpdateChildGeometry();
    }

    // ---- phase 4 -------------------------------------------------------------------------------

    /// <summary>Produces the semantics nodes for this render object and its subtree.</summary>
    public void EnsureSemanticsNode(SemanticsOwner owner)
    {
        _owner = owner;
        if (!Built)
        {
            BuildSemantics(owner, usedSemanticsIds: []);
        }
        else
        {
            BuildSemanticsSubtree(owner, usedSemanticsIds: []);
        }
    }

    private SemanticsOwner? _owner;

    private void BuildSemantics(SemanticsOwner owner, HashSet<int> usedSemanticsIds)
    {
        _owner = owner;
        if (CachedSemanticsNode != null)
        {
            // Everything in `_semanticsNodes` other than the produced node is a sibling node this
            // render object owns, so it is also responsible for clearing their stale tags.
            foreach (SemanticsNode node in _semanticsNodes)
            {
                if (!ReferenceEquals(node, CachedSemanticsNode))
                {
                    node.ClearTags();
                }
            }
        }

        if (!Built)
        {
            ProduceSemanticsNode(owner, usedSemanticsIds);
        }

        SemanticsNode producedNode = CachedSemanticsNode!;
        foreach (SemanticsNode node in _semanticsNodes)
        {
            if (ReferenceEquals(node, producedNode))
            {
                continue;
            }

            if (ParentData?.TagsForChildren is { Count: > 0 } tags)
            {
                node.AddTags(tags);
            }
        }
    }

    private void BuildSemanticsSubtree(SemanticsOwner owner, HashSet<int> usedSemanticsIds)
    {
        var children = new List<SemanticsNode>();
        foreach (RenderObjectSemantics child in _children)
        {
            if (child.ParentDataDirty)
            {
                continue;
            }

            // A cached node may have been part of a sibling merge group before this update, in which
            // case it keeps being reused there and this render object needs a fresh one.
            if (child.CachedSemanticsNode != null && usedSemanticsIds.Contains(child.CachedSemanticsNode.Id))
            {
                child.MarkNeedsBuild();
                child.CachedSemanticsNode = null;
            }

            child.BuildSemantics(owner, usedSemanticsIds);
            children.AddRange(child.SemanticsNodes);
        }

        SemanticsNode node = CachedSemanticsNode!;
        children.RemoveAll(ShouldDrop);
        bool isSemanticsHidden = ConfigProvider.Original.IsHidden
                                 || (!(ParentData?.MergeIntoParent ?? false) && Geometry!.Hidden);
        if (ConfigProvider.Effective.IsHidden != isSemanticsHidden)
        {
            ConfigProvider.UpdateConfig(configuration => configuration.IsHidden = isSemanticsHidden);
        }

        if (ConfigProvider.Effective.IsSemanticBoundary)
        {
            if (NeedsMergingSiblingNodesIntoSelf)
            {
                // The sibling nodes have to merge into this node, so the configuration and the real
                // children move onto an inner node and this node holds the inner node plus the siblings.
                SemanticsNode innerNode = owner.CreateDetachedNode(RenderObject);
                innerNode.Rect = Geometry!.Rect;
                RenderObject.InvokeAssembleSemanticsNode(innerNode, ConfigProvider.Effective, children);

                var mergingConfig = new SemanticsConfiguration
                {
                    IsSemanticBoundary = true,
                    IsMergingSemanticsOfDescendants = true
                };
                node.UpdateWith(mergingConfig, [innerNode, .. _producedSiblingNodesAndOwners.Keys]);
            }
            else
            {
                RenderObject.InvokeAssembleSemanticsNode(node, ConfigProvider.Effective, children);
            }
        }
        else
        {
            node.UpdateWith(ConfigProvider.Effective, children);
        }
    }

    private void ProduceSemanticsNode(SemanticsOwner owner, HashSet<int> usedSemanticsIds)
    {
        _semanticsNodes.Clear();
        _producedSiblingNodesAndOwners.Clear();

        SemanticsNode node = CachedSemanticsNode ??= owner.CreateNodeFor(RenderObject);
        node.IsMergedIntoParent = ParentData?.MergeIntoParent ?? false;
        node.ReplaceTags(ParentData?.TagsForChildren);
        UpdateSemanticsNodeGeometry();

        MergeSiblingGroup(owner, usedSemanticsIds);
        BuildSemanticsSubtree(owner, usedSemanticsIds);

        _semanticsNodes.Add(node);
        if (!NeedsMergingSiblingNodesIntoSelf)
        {
            _semanticsNodes.AddRange(_producedSiblingNodesAndOwners.Keys);
        }

        Built = true;
    }

    private void MergeSiblingGroup(SemanticsOwner owner, HashSet<int> usedSemanticsIds)
    {
        foreach (List<ISemanticsFragment> group in _siblingMergeGroups)
        {
            SemanticsConfiguration? configuration = null;
            SemanticsNode? node = null;
            var explicitChildren = new List<RenderObjectSemantics>();
            foreach (ISemanticsFragment fragment in group)
            {
                if (fragment is RenderObjectSemantics renderObjectFragment)
                {
                    if (renderObjectFragment.ShouldFormSemanticsNode)
                    {
                        explicitChildren.Add(renderObjectFragment);
                        continue;
                    }

                    explicitChildren.AddRange(renderObjectFragment._children);
                }

                if (fragment.ConfigToMergeUp != null)
                {
                    fragment.MergesToSibling = true;
                    if (CanCarrySiblingNode(fragment))
                    {
                        node ??= fragment.Owner.CachedSemanticsNode;
                    }

                    configuration ??= new SemanticsConfiguration();
                    configuration.Absorb(fragment.ConfigToMergeUp);
                }
            }

            var childrenNodes = new List<SemanticsNode>();
            foreach (RenderObjectSemantics explicitChild in explicitChildren)
            {
                explicitChild.BuildSemantics(owner, usedSemanticsIds);
                childrenNodes.AddRange(explicitChild.SemanticsNodes);
            }

            // Null when every fragment in the group formed a node of its own.
            if (configuration == null)
            {
                continue;
            }

            if (node == null || usedSemanticsIds.Contains(node.Id))
            {
                node = owner.CreateDetachedNode(RenderObject);
            }

            usedSemanticsIds.Add(node.Id);
            foreach (ISemanticsFragment fragment in group)
            {
                if (fragment.ConfigToMergeUp != null && CanCarrySiblingNode(fragment))
                {
                    fragment.Owner.Built = true;
                    fragment.Owner.CachedSemanticsNode = node;
                }
            }

            node.UpdateWith(configuration, childrenNodes);
            _producedSiblingNodesAndOwners[node] = group;

            // Only tags are added here, never cleared: some of them belong to the parent fragment
            // that takes these nodes as its siblings, and it is that fragment's job to clean up.
            var tags = new HashSet<SemanticsTag>();
            foreach (ISemanticsFragment fragment in group)
            {
                if (fragment.Owner.ParentData?.TagsForChildren is { Count: > 0 } fragmentTags)
                {
                    tags.UnionWith(fragmentTags);
                }
            }

            if (tags.Count > 0)
            {
                node.AddTags(tags);
            }

            node.IsMergedIntoParent = ParentData?.MergeIntoParent ?? false;
        }

        UpdateSiblingNodesGeometries();
        return;

        // An incomplete fragment's owner is the render object that ran the delegate. When that render
        // object also forms a node of its own, letting the fragment donate or adopt
        // `CachedSemanticsNode` would hand the sibling group the owner's own node.
        bool CanCarrySiblingNode(ISemanticsFragment fragment)
        {
            return !ReferenceEquals(fragment.Owner, this) || !ShouldFormSemanticsNode;
        }
    }

    private void UpdateSemanticsNodeGeometry()
    {
        SemanticsNode node = CachedSemanticsNode!;
        SemanticsGeometry nodeGeometry = Geometry!;
        node.Rect = nodeGeometry.Rect;
        node.Transform = nodeGeometry.Transform;
        node.ParentSemanticsClipRect = nodeGeometry.SemanticsClipRect;
        node.ParentPaintClipRect = nodeGeometry.PaintClipRect;
    }

    private void UpdateSiblingNodesGeometries()
    {
        SemanticsGeometry mainGeometry = Geometry!;
        foreach ((SemanticsNode node, List<ISemanticsFragment> group) in _producedSiblingNodesAndOwners)
        {
            Rect? rect = null;
            Rect? semanticsClipRect = null;
            Rect? paintClipRect = null;
            foreach (ISemanticsFragment fragment in group)
            {
                if (fragment.Owner.ShouldFormSemanticsNode)
                {
                    continue;
                }

                SemanticsGeometry parentGeometry = SemanticsGeometry.ComputeChildGeometry(
                    parentTransform: mainGeometry.Transform,
                    parentPaintClipRect: mainGeometry.PaintClipRect,
                    parentSemanticsClipRect: mainGeometry.SemanticsClipRect,
                    parent: this,
                    child: fragment.Owner);

                Rect semanticBounds = fragment.Owner.RenderObject.SemanticBoundsForSemantics;
                Rect rectInFragmentOwnerCoordinates = parentGeometry.SemanticsClipRect is { } clip
                    ? SemanticsGeometry.Intersect(clip, semanticBounds)
                    : semanticBounds;
                Rect rectInParentCoordinates =
                    SemanticsNode.TransformRect(parentGeometry.Transform, rectInFragmentOwnerCoordinates);
                rect = rect is { } current
                    ? ExpandToInclude(current, rectInParentCoordinates)
                    : rectInParentCoordinates;

                if (parentGeometry.SemanticsClipRect is { } fragmentSemanticsClip)
                {
                    Rect transformed =
                        SemanticsNode.TransformRect(parentGeometry.Transform, fragmentSemanticsClip);
                    semanticsClipRect = semanticsClipRect is { } existing
                        ? SemanticsGeometry.Intersect(existing, transformed)
                        : transformed;
                }

                if (parentGeometry.PaintClipRect is { } fragmentPaintClip)
                {
                    Rect transformed = SemanticsNode.TransformRect(parentGeometry.Transform, fragmentPaintClip);
                    paintClipRect = paintClipRect is { } existing
                        ? SemanticsGeometry.Intersect(existing, transformed)
                        : transformed;
                }
            }

            node.Rect = rect ?? node.Rect;
            // The transform is already accounted for in the rect above.
            node.Transform = null;
            node.ParentSemanticsClipRect = semanticsClipRect;
            node.ParentPaintClipRect = paintClipRect;
        }
    }

    private static Rect ExpandToInclude(Rect current, Rect addition)
    {
        double minX = Math.Min(current.X, addition.X);
        double minY = Math.Min(current.Y, addition.Y);
        double maxX = Math.Max(current.Right, addition.Right);
        double maxY = Math.Max(current.Bottom, addition.Bottom);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    // ---- invalidation --------------------------------------------------------------------------

    /// <summary>Marks this render object's semantics information as changed.</summary>
    public void MarkNeedsUpdate()
    {
        PipelineOwner? pipelineOwner = RenderObject.Owner;
        pipelineOwner?.RequestSemanticsGeometryUpdateFor(RenderObject);

        SemanticsNode? producedSemanticsNode = CachedSemanticsNode;
        bool wasSemanticsBoundary = producedSemanticsNode != null && ConfigProvider.WasSemanticsBoundary;

        ConfigProvider.Clear();
        _containsIncompleteFragment = false;

        bool mayProduceSiblingNodes = ConfigProvider.Effective.ChildConfigurationsDelegate != null;
        bool isEffectiveSemanticsBoundary = ConfigProvider.Effective.IsSemanticBoundary && wasSemanticsBoundary;
        RenderObject node = RenderObject;

        // Sibling nodes attach to the parent of the immediate semantics node, so dirtying this
        // boundary is not enough: the first parent boundary that cannot produce a sibling node has to
        // be found.
        while (node.Parent != null && (mayProduceSiblingNodes || !isEffectiveSemanticsBoundary))
        {
            if (!ReferenceEquals(node, RenderObject) && node.Semantics.ParentDataDirty && !mayProduceSiblingNodes)
            {
                break;
            }

            node.Semantics.ParentData = null;
            node.Semantics._blocksPreviousSibling = null;
            if (isEffectiveSemanticsBoundary)
            {
                mayProduceSiblingNodes = false;
            }

            mayProduceSiblingNodes |= node.Semantics.ConfigProvider.Effective.ChildConfigurationsDelegate != null;

            node = node.Parent!;
            isEffectiveSemanticsBoundary =
                node.Semantics.ConfigProvider.Effective.IsSemanticBoundary && node.Semantics.Built;
        }

        if (pipelineOwner == null)
        {
            return;
        }

        if (!ReferenceEquals(node, RenderObject) && producedSemanticsNode != null && node.Semantics.ParentDataDirty)
        {
            // This render object's node is no longer guaranteed to stay in the tree, so the ancestor
            // requested below owns the update instead.
            pipelineOwner.ForgetSemanticsUpdateFor(RenderObject);
        }

        if (!node.Semantics.ParentDataDirty || node.Semantics.IsRoot)
        {
            pipelineOwner.RequestSemanticsUpdateFor(node);
        }
    }

    /// <summary>Drops every cache, as if this object had just been created.</summary>
    public void Clear()
    {
        Built = false;
        CachedSemanticsNode = null;
        ParentData = null;
        Geometry = null;
        _blocksPreviousSibling = null;
        _containsIncompleteFragment = false;
        _hasSiblingConflict = false;
        _mergeUp.Clear();
        _siblingMergeGroups.Clear();
        _children.Clear();
        _semanticsNodes.Clear();
        _producedSiblingNodesAndOwners.Clear();
        ConfigProvider.Clear();
    }

    private static void ValidateSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        if (configuration.ExplicitChildNodes && configuration.ChildConfigurationsDelegate != null)
        {
            throw new InvalidOperationException(
                "SemanticsConfiguration with ExplicitChildNodes=true cannot have a non-null "
                + "ChildConfigurationsDelegate.");
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() =>
        [.. _children.Select(static child => child.ToDiagnosticsNode())];

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("owner", Diagnostics.DescribeIdentity(RenderObject)));
        properties.Add(new FlagProperty("noParentData", value: ParentDataDirty, ifTrue: "NO PARENT DATA"));
        properties.Add(new FlagProperty("geometry", value: GeometryDirty, ifTrue: "NO GEOMETRY"));
        properties.Add(new FlagProperty(
            "semanticsBlock",
            value: ConfigProvider.Effective.IsBlockingSemanticsOfPreviouslyPaintedNodes,
            ifTrue: "BLOCK PREVIOUS"));
        if (!ParentDataDirty && ContributesToSemanticsTree)
        {
            string semanticsNodeStatus;
            if (Built)
            {
                semanticsNodeStatus = $"formed {CachedSemanticsNode?.Id}";
            }
            else if (ShouldFormSemanticsNode)
            {
                semanticsNodeStatus = "needs build";
            }
            else
            {
                semanticsNodeStatus = "no semantics node";
            }

            properties.Add(new StringProperty("formedSemanticsNode", semanticsNodeStatus, quoted: false));
        }

        properties.Add(new FlagProperty(
            "isSemanticBoundary",
            value: ConfigProvider.Effective.IsSemanticBoundary,
            ifTrue: "semantic boundary"));
        properties.Add(new FlagProperty(
            "blocksSemantics",
            value: IsBlockingPreviousSibling,
            ifTrue: "BLOCKS SEMANTICS"));
        if (ContributesToSemanticsTree && _siblingMergeGroups.Count > 0)
        {
            properties.Add(new StringProperty(
                "Sibling group",
                $"[{string.Join(", ", _siblingMergeGroups.Select(static group => $"[{group.Count}]"))}]",
                quoted: false));
        }
    }
}
