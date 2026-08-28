using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// The node registry and the incremental dirty pipeline of <see cref="SemanticsOwner"/>.
/// </summary>
public sealed partial class SemanticsOwner
{
    private readonly Dictionary<int, SemanticsNode> _nodes = [];
    private readonly List<SemanticsNode> _dirtyNodes = [];
    private readonly HashSet<SemanticsNode> _detachedNodes = [];
    private readonly Dictionary<object, SemanticsNode> _traversalParentNodes = [];
    private readonly Dictionary<object, List<SemanticsNode>> _traversalChildNodes = [];

    /// <summary>Invoked with every batch of changed nodes, before listeners are notified.</summary>
    /// <remarks>Flutter's <c>SemanticsOwner.onSemanticsUpdate</c>.</remarks>
    public Action<SemanticsUpdate>? OnSemanticsUpdate { get; set; }

    /// <summary>The root of the semantics tree, which always carries id <c>0</c>.</summary>
    /// <remarks>Flutter's <c>SemanticsOwner.rootSemanticsNode</c>.</remarks>
    public SemanticsNode? RootNode => _nodes.GetValueOrDefault(0);

    /// <summary>The node with the given <paramref name="id"/>, or <c>null</c> when unknown.</summary>
    /// <remarks>Flutter's <c>SemanticsOwner.getSemanticsNode</c>.</remarks>
    public SemanticsNode? GetSemanticsNode(int id) => _nodes.GetValueOrDefault(id);

    internal bool ContainsNodeId(int id) => _nodes.ContainsKey(id);

    internal void RegisterNode(SemanticsNode node)
    {
        _nodes[node.Id] = node;
        _detachedNodes.Remove(node);
    }

    internal void UnregisterNode(SemanticsNode node)
    {
        Debug.Assert(_nodes.ContainsKey(node.Id));
        Debug.Assert(!_detachedNodes.Contains(node));
        if (_nodes.TryGetValue(node.Id, out SemanticsNode? registered) && ReferenceEquals(registered, node))
        {
            _nodes.Remove(node.Id);
        }

        _detachedNodes.Add(node);
    }

    internal bool DebugIsDetached(SemanticsNode node) => _detachedNodes.Contains(node);

    internal void AddDirtyNode(SemanticsNode node) => _dirtyNodes.Add(node);

    internal SemanticsNode? GetTraversalParentNode(object identifier) =>
        _traversalParentNodes.GetValueOrDefault(identifier);

    internal IReadOnlyList<SemanticsNode>? GetTraversalChildNodes(object identifier) =>
        _traversalChildNodes.GetValueOrDefault(identifier);

    /// <summary>Drops every traversal registration <paramref name="node"/> takes part in.</summary>
    internal void ForgetTraversalRegistrations(SemanticsNode node)
    {
        foreach (object identifier in _traversalParentNodes
                     .Where(entry => ReferenceEquals(entry.Value, node))
                     .Select(static entry => entry.Key)
                     .ToArray())
        {
            _traversalParentNodes.Remove(identifier);
        }

        foreach ((object identifier, List<SemanticsNode> children) in _traversalChildNodes.ToArray())
        {
            children.RemoveAll(child => ReferenceEquals(child, node));
            if (children.Count == 0)
            {
                _traversalChildNodes.Remove(identifier);
            }
        }
    }

    /// <summary>
    /// Sends every node that changed since the last call to <see cref="OnSemanticsUpdate"/>, then
    /// notifies listeners.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsOwner.sendSemanticsUpdate</c>. Calling it with nothing dirty is legal
    /// and produces no update at all.
    /// </remarks>
    public void SendSemanticsUpdate()
    {
        DebugVerifyNoInvisibleNodes();
        if (_dirtyNodes.Count == 0)
        {
            return;
        }

        var customSemanticsActionIds = new HashSet<int>();
        var visitedNodes = new List<SemanticsNode>();
        while (_dirtyNodes.Count > 0)
        {
            List<SemanticsNode> localDirtyNodes =
                [.. _dirtyNodes.Where(node => !_detachedNodes.Contains(node))];
            _dirtyNodes.Clear();
            _detachedNodes.Clear();
            localDirtyNodes.Sort(static (a, b) => a.Depth - b.Depth);
            visitedNodes.AddRange(localDirtyNodes);
            foreach (SemanticsNode node in localDirtyNodes)
            {
                Debug.Assert(node.IsDirty);
                Debug.Assert(node.Parent is null
                             || !node.Parent.IsPartOfNodeMerging
                             || node.IsMergedIntoParent);
                if (node.IsPartOfNodeMerging)
                {
                    Debug.Assert(node.MergeAllDescendantsIntoThisNode || node.Parent is not null);
                    if (node.Parent is { } parent && parent.IsPartOfNodeMerging)
                    {
                        // One level per iteration, until the walk reaches the merge root.
                        parent.MarkDirty();
                        node.ClearDirty();
                    }
                }

                ForgetTraversalRegistrations(node);
                if (node.IsTraversalParent)
                {
                    DebugAssertTraversalParentIsUnique(node);
                    _traversalParentNodes[node.TraversalParentIdentifier!] = node;
                }
                else if (node.IsTraversalChild)
                {
                    if (!_traversalChildNodes.TryGetValue(
                            node.TraversalChildIdentifier!,
                            out List<SemanticsNode>? siblings))
                    {
                        siblings = [];
                        _traversalChildNodes[node.TraversalChildIdentifier!] = siblings;
                    }

                    siblings.Add(node);
                }

                if (!PlatformDefaults.IsWeb
                    && node.IsTraversalChild
                    && GetTraversalParentNode(node.TraversalChildIdentifier!) is { } traversalParent
                    && !visitedNodes.Contains(traversalParent))
                {
                    // The graft parent has to re-serialize its traversal children with this one in.
                    traversalParent.MarkDirty();
                }
            }
        }

        visitedNodes.Sort(static (a, b) => a.Depth - b.Depth);
        var builder = new SemanticsUpdateBuilder();
        foreach (SemanticsNode node in visitedNodes)
        {
            Debug.Assert(node.Parent?.IsDirty != true);
            if (node.IsDirty && node.Attached)
            {
                node.AddToUpdate(builder, customSemanticsActionIds);
            }
        }

        _dirtyNodes.Clear();
        foreach (int actionId in customSemanticsActionIds.Order())
        {
            if (CustomSemanticsAction.GetAction(actionId) is not { } action)
            {
                continue;
            }

            builder.UpdateCustomAction(
                actionId,
                action.Label,
                action.Hint,
                action.Action is { } overridden ? (int)overridden : -1);
        }

        OnSemanticsUpdate?.Invoke(builder.Build());
        NotifyListeners();
    }

    public override void Dispose()
    {
        _dirtyNodes.Clear();
        _nodes.Clear();
        _detachedNodes.Clear();
        _traversalChildNodes.Clear();
        _traversalParentNodes.Clear();
        base.Dispose();
    }

    [Conditional("DEBUG")]
    private void DebugAssertTraversalParentIsUnique(SemanticsNode node)
    {
        bool isUnique = !_traversalParentNodes.TryGetValue(
                            node.TraversalParentIdentifier!,
                            out SemanticsNode? existing)
                        || ReferenceEquals(existing, node);
        Debug.Assert(
            isUnique,
            "The traversalParentIdentifier must be unique. No two semantics nodes can share the "
            + "same traversalParentIdentifier.");
    }

    /// <remarks>
    /// Flutter's debug block at the top of <c>sendSemanticsUpdate</c>: an invisible node must never
    /// reach the platform, except a childless root and anything under a merging ancestor.
    /// </remarks>
    [Conditional("DEBUG")]
    private void DebugVerifyNoInvisibleNodes()
    {
        if (RootNode is not { } root)
        {
            return;
        }

        var invisibleNodes = new List<SemanticsNode>();
        if (root.Children.Count > 0 && IsRectEmpty(root.Rect))
        {
            invisibleNodes.Add(root);
        }
        else if (!root.MergeAllDescendantsIntoThisNode)
        {
            FindInvisibleNodes(root, invisibleNodes);
        }

        if (invisibleNodes.Count == 0)
        {
            return;
        }

        var message = new System.Text.StringBuilder(
            "Invisible SemanticsNodes should not be added to the tree.\n"
            + "The following invisible SemanticsNodes were added to the tree:\n");
        foreach (SemanticsNode node in invisibleNodes)
        {
            message.Append("  #").Append(node.Id);
            message.Append(node.Parent is { } parent
                ? $" which was added as a child of #{parent.Id}\n"
                : " which was added as the root SemanticsNode\n");
        }

        throw new FlutterError(message.ToString());
    }

    private static bool IsRectEmpty(Rect rect) => rect.Width <= 0.0 || rect.Height <= 0.0;

    private static void FindInvisibleNodes(SemanticsNode node, List<SemanticsNode> invisibleNodes)
    {
        foreach (SemanticsNode child in node.Children)
        {
            if (IsRectEmpty(child.Rect))
            {
                invisibleNodes.Add(child);
            }
            else if (!child.MergeAllDescendantsIntoThisNode)
            {
                FindInvisibleNodes(child, invisibleNodes);
            }
        }
    }
}
