using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart; flutter/packages/flutter/lib/src/rendering/semantics.dart (approximate)

namespace Plumix.Rendering;

internal sealed class RenderObjectSemantics
{
    private readonly RenderObject _owner;
    private readonly SemanticsConfigurationProvider _configProvider;
    private bool _needsSemanticsUpdate = true;
    private bool _parentDataDirty = true;
    private bool _isSemanticsBoundary;
    private Matrix _transform = Matrix.Identity;
    private Rect? _clipRect;
    private Rect? _paintClipRect;
    private Rect _rect;
    private bool _rectClippedOut;
    private bool _hidden;
    private bool _hasBlockingSemanticsCache;
    private bool _blocksPreviousSemanticsCache;
    private readonly List<SemanticsNode> _cachedDetachedNodes = [];
    private int _detachedNodeCursor;

    public RenderObjectSemantics(RenderObject owner)
    {
        _owner = owner;
        _configProvider = new SemanticsConfigurationProvider(
            _owner.InvokeDescribeSemanticsConfiguration,
            ValidateSemanticsConfiguration);
    }

    internal bool NeedsSemanticsUpdate => _needsSemanticsUpdate;
    internal bool ParentDataDirty => _parentDataDirty;

    internal void ResetForDetach()
    {
        _parentDataDirty = true;
        _rectClippedOut = false;
        _hidden = false;
        _hasBlockingSemanticsCache = false;
        _blocksPreviousSemanticsCache = false;
        _configProvider.Reset();
        _cachedDetachedNodes.Clear();
        _detachedNodeCursor = 0;
    }

    internal void MarkNeedsSemanticsUpdate()
    {
        var hadProducedSemanticsNode = _owner._semanticsNode != null;
        var wasEffectiveSemanticsBoundary = hadProducedSemanticsNode && _isSemanticsBoundary && !_parentDataDirty;
        var cachedConfiguration = _configProvider.TryGetCachedEffective();
        var wasBlockingSiblings = cachedConfiguration is
        {
            IsExcluded: false,
            IsBlockingSemanticsOfPreviouslyPaintedNodes: true
        };

        _needsSemanticsUpdate = true;
        _hasBlockingSemanticsCache = false;
        _configProvider.Reset();

        var updatedConfiguration = new SemanticsConfiguration();
        _owner.InvokeDescribeSemanticsConfiguration(updatedConfiguration);
        var blocksSiblingsNow = !updatedConfiguration.IsExcluded
                                && updatedConfiguration.IsBlockingSemanticsOfPreviouslyPaintedNodes;
        var mayAffectSiblingOrdering = wasBlockingSiblings || blocksSiblingsNow;

        var pipelineOwner = _owner.Owner;
        if (!_owner.Attached || pipelineOwner == null)
        {
            return;
        }

        var mayProduceSiblingNodes = updatedConfiguration.ChildConfigurationsDelegate != null;
        RenderObject node = _owner;
        var isEffectiveSemanticsBoundary = wasEffectiveSemanticsBoundary && !mayAffectSiblingOrdering;

        while (node.Parent != null && (mayProduceSiblingNodes || !isEffectiveSemanticsBoundary))
        {
            if (!ReferenceEquals(node, _owner) && node.Semantics.ParentDataDirty && !mayProduceSiblingNodes)
            {
                break;
            }

            node.Semantics.MarkParentDataDirty();

            if (isEffectiveSemanticsBoundary)
            {
                mayProduceSiblingNodes = false;
            }

            mayProduceSiblingNodes |= node.Semantics.HasChildSemanticsConfigurationsDelegate();

            node = node.Parent!;
            isEffectiveSemanticsBoundary = node.Semantics._isSemanticsBoundary && !node.Semantics.ParentDataDirty;
        }

        if (!ReferenceEquals(node, _owner) && hadProducedSemanticsNode && node.Semantics.ParentDataDirty)
        {
            pipelineOwner.ForgetSemanticsUpdateFor(_owner);
        }

        if (!node.Semantics.ParentDataDirty)
        {
            pipelineOwner.RequestSemanticsUpdateFor(node);
        }
    }

    internal void UpdateSemanticsChildren(Matrix transform, Rect? semanticsClipRect, Rect? paintClipRect)
    {
        if (_needsSemanticsUpdate)
        {
            _needsSemanticsUpdate = false;
            _owner.InvokePerformSemantics();
        }

        _configProvider.Reset();
        var config = _configProvider.Effective;
        _isSemanticsBoundary = config.IsSemanticBoundary;
        _transform = transform;
        _clipRect = semanticsClipRect;
        _paintClipRect = paintClipRect;
        _rectClippedOut = false;
        _hidden = false;
        _parentDataDirty = false;
        _hasBlockingSemanticsCache = false;

        // Start from dirty parent data for all render children. Only children
        // visited for semantics in this pass will become clean.
        _owner.VisitChildren(static child => child.Semantics.MarkParentDataDirty());

        if (config.IsExcluded)
        {
            _owner._semanticsNode = null;
            return;
        }

        var semanticsChildren = new List<(RenderObject child, Matrix transform, Rect? semanticsClipRect, Rect? paintClipRect)>();
        _owner.VisitChildrenForSemantics((child, childOffset, childTransform) =>
        {
            var childMatrix =
                transform
                * Matrix.CreateTranslation(childOffset.X, childOffset.Y)
                * childTransform;

            var localPaintClip = _owner.InvokeDescribeApproximatePaintClip(child);
            var localSemanticsClip = _owner.InvokeDescribeSemanticsClip(child) ?? localPaintClip;
            var semanticsClipForChild = RenderObject.IntersectClip(semanticsClipRect, localSemanticsClip, transform);
            var paintClipForChild = RenderObject.IntersectClip(paintClipRect, localPaintClip, transform);
            semanticsChildren.Add((child, childMatrix, semanticsClipForChild, paintClipForChild));
        });

        var nonBlockedChildren = new List<(RenderObject child, Matrix transform, Rect? semanticsClipRect, Rect? paintClipRect)>();
        foreach (var entry in semanticsChildren)
        {
            if (entry.child.Semantics.BlocksPreviousSemanticsSibling())
            {
                foreach (var droppedSibling in nonBlockedChildren)
                {
                    droppedSibling.child.Semantics.MarkParentDataDirty();
                }

                nonBlockedChildren.Clear();
            }

            nonBlockedChildren.Add(entry);
        }

        foreach (var entry in nonBlockedChildren)
        {
            entry.child.Semantics.UpdateSemanticsChildren(entry.transform, entry.semanticsClipRect, entry.paintClipRect);
        }
    }

    internal void UpdateSemanticsChildrenFromCachedParentData()
    {
        if (_owner.Parent == null)
        {
            UpdateSemanticsChildren(Matrix.Identity, semanticsClipRect: null, paintClipRect: null);
            return;
        }

        UpdateSemanticsChildren(_transform, _clipRect, _paintClipRect);
    }

    internal void EnsureSemanticsGeometry()
    {
        if (_parentDataDirty || !_configProvider.HasEffective)
        {
            return;
        }

        var config = _configProvider.Effective;
        if (config.IsExcluded)
        {
            _rectClippedOut = true;
            _hidden = false;
            return;
        }

        if (HasOwnSemantics(config))
        {
            var localBounds = config.ExplicitRect ?? _owner.SemanticBoundsForSemantics;
            var transformedRect = RenderObject.TransformRect(_transform, localBounds);
            _rect = transformedRect;
            _rectClippedOut = _clipRect.HasValue
                              && !_clipRect.Value.Intersects(transformedRect);
            _hidden = !_rectClippedOut
                      && _paintClipRect.HasValue
                      && !_paintClipRect.Value.Intersects(transformedRect);
        }

        _owner.VisitChildrenForSemantics((child, _, _) => child.Semantics.EnsureSemanticsGeometry());
    }

    internal void EnsureSemanticsNode(
        SemanticsOwner owner,
        List<SemanticsNode> output,
        bool inheritedExplicitChildNodes)
    {
        _detachedNodeCursor = 0;

        if (_parentDataDirty || !_configProvider.HasEffective)
        {
            return;
        }

        var config = _configProvider.Effective.Clone();
        if (config.IsExcluded)
        {
            _owner._semanticsNode = null;
            return;
        }

        var contributesToSemanticsTree = ContributesToSemanticsTree(config);
        var explicitChildNodesForChildren = _owner.Parent == null
                                            || config.ExplicitChildNodes
                                            || (!contributesToSemanticsTree && inheritedExplicitChildNodes);

        var children = new List<SemanticsNode>();
        _owner.VisitChildrenForSemantics((child, _, _) =>
        {
            var childNodes = new List<SemanticsNode>();
            child.Semantics.EnsureSemanticsNode(owner, childNodes, explicitChildNodesForChildren);

            if (childNodes.Any(static node => node.BlocksPreviousNodes))
            {
                children.Clear();
            }

            children.AddRange(childNodes);
        });

        var siblingNodes = ApplyChildConfigurationsDelegate(owner, config, children, explicitChildNodesForChildren);

        if (config.IsMergingSemanticsOfDescendants && !explicitChildNodesForChildren && children.Count > 0)
        {
            MergeChildSemanticsIntoConfiguration(config, children);
        }

        if (!HasOwnSemantics(config))
        {
            _owner._semanticsNode = null;
            output.AddRange(children);
            output.AddRange(siblingNodes);
            return;
        }

        if (_rectClippedOut)
        {
            _owner._semanticsNode = null;
            return;
        }

        var semanticsNode = owner.EnsureNode(_owner);
        ApplySemanticsConfigurationToNode(
            semanticsNode,
            config,
            _rect,
            _hidden,
            config.IsBlockingSemanticsOfPreviouslyPaintedNodes,
            children);

        output.Add(semanticsNode);
        output.AddRange(siblingNodes);
    }

    internal void UpdateSemanticsSubtree()
    {
        if (_needsSemanticsUpdate)
        {
            _needsSemanticsUpdate = false;
            _configProvider.Reset();
            _owner.InvokePerformSemantics();
        }

        _owner.VisitChildrenForSemantics((child, _, _) => child.Semantics.UpdateSemanticsSubtree());
    }

    private void MarkParentDataDirty()
    {
        _parentDataDirty = true;
        _hasBlockingSemanticsCache = false;
    }

    private bool HasChildSemanticsConfigurationsDelegate()
    {
        return GetEffectiveSemanticsConfigurationForQueries().ChildConfigurationsDelegate != null;
    }

    private bool BlocksPreviousSemanticsSibling()
    {
        if (_hasBlockingSemanticsCache)
        {
            return _blocksPreviousSemanticsCache;
        }

        var config = GetEffectiveSemanticsConfigurationForQueries();
        if (config.IsExcluded)
        {
            _hasBlockingSemanticsCache = true;
            _blocksPreviousSemanticsCache = false;
            return false;
        }

        if (config.IsBlockingSemanticsOfPreviouslyPaintedNodes)
        {
            _hasBlockingSemanticsCache = true;
            _blocksPreviousSemanticsCache = true;
            return true;
        }

        if (config.IsSemanticBoundary)
        {
            _hasBlockingSemanticsCache = true;
            _blocksPreviousSemanticsCache = false;
            return false;
        }

        var blocksPreviousSibling = false;
        _owner.VisitChildrenForSemantics((child, _, _) =>
        {
            if (!blocksPreviousSibling && child.Semantics.BlocksPreviousSemanticsSibling())
            {
                blocksPreviousSibling = true;
            }
        });

        _hasBlockingSemanticsCache = true;
        _blocksPreviousSemanticsCache = blocksPreviousSibling;
        return blocksPreviousSibling;
    }

    private SemanticsConfiguration GetEffectiveSemanticsConfigurationForQueries()
    {
        if (_configProvider.HasEffective && !_needsSemanticsUpdate)
        {
            return _configProvider.Effective;
        }

        var configuration = new SemanticsConfiguration();
        _owner.InvokeDescribeSemanticsConfiguration(configuration);
        return configuration;
    }

    private static bool HasOwnSemantics(SemanticsConfiguration config)
    {
        return config.IsSemanticBoundary
               || config.IsMergingSemanticsOfDescendants
               || config.IsBlockingSemanticsOfPreviouslyPaintedNodes
               || !string.IsNullOrEmpty(config.Label)
               || config.Flags != SemanticsFlags.None
               || config.Actions != SemanticsActions.None
               || config.HasActionHandlers;
    }

    private static bool ContributesToSemanticsTree(SemanticsConfiguration configuration)
    {
        return HasOwnSemantics(configuration) || configuration.ChildConfigurationsDelegate != null;
    }

    private static void MergeChildSemanticsIntoConfiguration(
        SemanticsConfiguration configuration,
        List<SemanticsNode> children)
    {
        if (children.Count == 0)
        {
            return;
        }

        var childrenToKeep = new List<SemanticsNode>();
        foreach (var child in children)
        {
            var childConfiguration = CreateConfigurationFromSemanticsNode(child);
            if (configuration.IsCompatibleWith(childConfiguration))
            {
                configuration.Absorb(childConfiguration);
            }
            else
            {
                childrenToKeep.Add(child);
            }
        }

        children.Clear();
        children.AddRange(childrenToKeep);
    }

    private List<SemanticsNode> ApplyChildConfigurationsDelegate(
        SemanticsOwner owner,
        SemanticsConfiguration configuration,
        List<SemanticsNode> children,
        bool forceExplicitChildNodes)
    {
        if (configuration.ChildConfigurationsDelegate == null)
        {
            return [];
        }

        var childConfigurations = new List<SemanticsConfiguration>(children.Count);
        var configurationToChildNode = new Dictionary<SemanticsConfiguration, SemanticsNode>(children.Count);

        foreach (var child in children)
        {
            var childConfiguration = CreateConfigurationFromSemanticsNode(child);
            childConfigurations.Add(childConfiguration);
            configurationToChildNode[childConfiguration] = child;
        }

        var result = configuration.ChildConfigurationsDelegate(childConfigurations);
        var consumedChildren = new HashSet<SemanticsNode>();
        var consumedIncompleteConfigurations = new HashSet<SemanticsConfiguration>();
        var incompleteConfigurationNodes = new Dictionary<SemanticsConfiguration, SemanticsNode>();
        var siblingNodes = new List<SemanticsNode>();

        foreach (var mergeConfiguration in result.MergeUp)
        {
            if (!TryResolveDelegateNode(
                    owner,
                    mergeConfiguration,
                    configurationToChildNode,
                    incompleteConfigurationNodes,
                    out var node,
                    out var fromChild))
            {
                continue;
            }

            if (fromChild)
            {
                if (consumedChildren.Contains(node))
                {
                    continue;
                }
            }
            else
            {
                if (consumedIncompleteConfigurations.Contains(mergeConfiguration))
                {
                    continue;
                }
            }

            if (forceExplicitChildNodes)
            {
                if (fromChild)
                {
                    continue;
                }

                children.Add(node);
                consumedIncompleteConfigurations.Add(mergeConfiguration);
                continue;
            }

            if (!configuration.IsCompatibleWith(mergeConfiguration))
            {
                if (!fromChild)
                {
                    siblingNodes.Add(node);
                    consumedIncompleteConfigurations.Add(mergeConfiguration);
                }

                continue;
            }

            configuration.Absorb(mergeConfiguration);
            if (fromChild)
            {
                consumedChildren.Add(node);
            }
            else
            {
                consumedIncompleteConfigurations.Add(mergeConfiguration);
            }
        }

        foreach (var siblingGroup in result.SiblingMergeGroups)
        {
            SemanticsNode? mergedNode = null;
            SemanticsConfiguration? mergedConfiguration = null;
            var mergedRect = default(Rect);
            var mergedIsHidden = false;
            var mergedBlocksPreviousNodes = false;

            foreach (var groupConfiguration in siblingGroup)
            {
                if (!TryResolveDelegateNode(
                        owner,
                        groupConfiguration,
                        configurationToChildNode,
                        incompleteConfigurationNodes,
                        out var groupNode,
                        out var fromChild))
                {
                    continue;
                }

                if (fromChild)
                {
                    if (consumedChildren.Contains(groupNode))
                    {
                        continue;
                    }
                }
                else
                {
                    if (consumedIncompleteConfigurations.Contains(groupConfiguration))
                    {
                        continue;
                    }
                }

                if (mergedNode == null || mergedConfiguration == null)
                {
                    mergedNode = groupNode;
                    mergedConfiguration = CreateConfigurationFromSemanticsNode(groupNode);
                    mergedRect = groupNode.Rect;
                    mergedIsHidden = groupNode.IsHidden;
                    mergedBlocksPreviousNodes = groupNode.BlocksPreviousNodes;

                    if (fromChild)
                    {
                        consumedChildren.Add(groupNode);
                    }
                    else
                    {
                        consumedIncompleteConfigurations.Add(groupConfiguration);
                    }

                    continue;
                }

                if (!mergedConfiguration.IsCompatibleWith(groupConfiguration))
                {
                    ApplySemanticsConfigurationToNode(
                        mergedNode,
                        mergedConfiguration,
                        mergedRect,
                        mergedIsHidden,
                        mergedBlocksPreviousNodes,
                        []);
                    siblingNodes.Add(mergedNode);

                    mergedNode = groupNode;
                    mergedConfiguration = CreateConfigurationFromSemanticsNode(groupNode);
                    mergedRect = groupNode.Rect;
                    mergedIsHidden = groupNode.IsHidden;
                    mergedBlocksPreviousNodes = groupNode.BlocksPreviousNodes;
                }
                else
                {
                    mergedConfiguration.Absorb(groupConfiguration);
                    mergedRect = ExpandRectToInclude(mergedRect, groupNode.Rect);
                    mergedIsHidden &= groupNode.IsHidden;
                    mergedBlocksPreviousNodes |= groupNode.BlocksPreviousNodes;
                }

                if (fromChild)
                {
                    consumedChildren.Add(groupNode);
                }
                else
                {
                    consumedIncompleteConfigurations.Add(groupConfiguration);
                }
            }

            if (mergedNode != null && mergedConfiguration != null)
            {
                ApplySemanticsConfigurationToNode(
                    mergedNode,
                    mergedConfiguration,
                    mergedRect,
                    mergedIsHidden,
                    mergedBlocksPreviousNodes,
                    []);
                siblingNodes.Add(mergedNode);
            }
        }

        if (consumedChildren.Count > 0)
        {
            children.RemoveAll(consumedChildren.Contains);
        }

        return siblingNodes;
    }

    private static void ValidateSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        if (configuration.ExplicitChildNodes && configuration.ChildConfigurationsDelegate != null)
        {
            throw new InvalidOperationException(
                "SemanticsConfiguration with ExplicitChildNodes=true cannot have a non-null ChildConfigurationsDelegate.");
        }
    }

    private bool TryResolveDelegateNode(
        SemanticsOwner owner,
        SemanticsConfiguration configuration,
        Dictionary<SemanticsConfiguration, SemanticsNode> configurationToChildNode,
        Dictionary<SemanticsConfiguration, SemanticsNode> incompleteConfigurationNodes,
        out SemanticsNode node,
        out bool fromChild)
    {
        if (configurationToChildNode.TryGetValue(configuration, out node!))
        {
            fromChild = true;
            return true;
        }

        fromChild = false;
        if (incompleteConfigurationNodes.TryGetValue(configuration, out node!))
        {
            return true;
        }

        var incompleteNode = CreateIncompleteSemanticsNode(owner, configuration);
        if (incompleteNode == null)
        {
            node = null!;
            return false;
        }

        node = incompleteNode;
        incompleteConfigurationNodes[configuration] = node;
        return true;
    }

    private SemanticsNode? CreateIncompleteSemanticsNode(
        SemanticsOwner owner,
        SemanticsConfiguration configuration)
    {
        if (configuration.IsExcluded || !HasOwnSemantics(configuration))
        {
            return null;
        }

        var rect = configuration.ExplicitRect.HasValue
            ? RenderObject.TransformRect(_transform, configuration.ExplicitRect.Value)
            : _rect;
        if (_clipRect.HasValue && !_clipRect.Value.Intersects(rect))
        {
            return null;
        }

        var isHidden = _paintClipRect.HasValue
                       && !_paintClipRect.Value.Intersects(rect);
        var node = GetOrCreateDetachedNode(owner);
        ApplySemanticsConfigurationToNode(
            node,
            configuration,
            rect,
            isHidden,
            configuration.IsBlockingSemanticsOfPreviouslyPaintedNodes,
            []);
        return node;
    }

    private SemanticsNode GetOrCreateDetachedNode(SemanticsOwner owner)
    {
        if (_detachedNodeCursor < _cachedDetachedNodes.Count)
        {
            return _cachedDetachedNodes[_detachedNodeCursor++];
        }

        var node = owner.CreateDetachedNode();
        _cachedDetachedNodes.Add(node);
        _detachedNodeCursor += 1;
        return node;
    }

    private static SemanticsConfiguration CreateConfigurationFromSemanticsNode(SemanticsNode node)
    {
        var configuration = new SemanticsConfiguration
        {
            Label = node.Label,
            Flags = node.Flags,
            Actions = node.Actions
        };

        var handlers = new Dictionary<SemanticsActions, Action>();
        node.CopyActionHandlersTo(handlers);
        configuration.ReplaceActionHandlers(handlers);
        return configuration;
    }

    private static Rect ExpandRectToInclude(Rect current, Rect addition)
    {
        var minX = Math.Min(current.X, addition.X);
        var minY = Math.Min(current.Y, addition.Y);
        var maxX = Math.Max(current.Right, addition.Right);
        var maxY = Math.Max(current.Bottom, addition.Bottom);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private static void ApplySemanticsConfigurationToNode(
        SemanticsNode node,
        SemanticsConfiguration configuration,
        Rect rect,
        bool isHidden,
        bool blocksPreviousNodes,
        List<SemanticsNode> children)
    {
        node.Rect = rect;
        node.Label = configuration.Label;
        node.Flags = configuration.Flags;
        node.Actions = configuration.Actions;
        node.IsHidden = isHidden;
        node.BlocksPreviousNodes = blocksPreviousNodes;
        node.ReplaceChildren(children);
        node.SetActionHandlers(configuration.ActionHandlers);
    }
}
