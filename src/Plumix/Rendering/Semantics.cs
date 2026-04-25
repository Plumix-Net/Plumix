using Avalonia;
using System.Text;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/semantics.dart (approximate)

namespace Plumix.Rendering;

[Flags]
public enum SemanticsFlags
{
    None = 0,
    IsButton = 1 << 0,
    IsEnabled = 1 << 1,
    IsSelected = 1 << 2,
    IsChecked = 1 << 3,
    IsTextField = 1 << 4,
    IsFocused = 1 << 5,
    IsHeader = 1 << 6,
    IsLink = 1 << 7,
    IsImage = 1 << 8,
    IsSlider = 1 << 9,
    IsHidden = 1 << 10,
}

[Flags]
public enum SemanticsActions
{
    None = 0,
    Tap = 1 << 0,
    LongPress = 1 << 1,
    ScrollLeft = 1 << 2,
    ScrollRight = 1 << 3,
    ScrollUp = 1 << 4,
    ScrollDown = 1 << 5,
    Increase = 1 << 6,
    Decrease = 1 << 7,
    Focus = 1 << 8,
    Dismiss = 1 << 9,
    ShowOnScreen = 1 << 10,
}

public delegate ChildSemanticsConfigurationsResult ChildSemanticsConfigurationsDelegate(
    List<SemanticsConfiguration> childConfigurations);

public sealed class ChildSemanticsConfigurationsResult
{
    public ChildSemanticsConfigurationsResult(
        List<SemanticsConfiguration> mergeUp,
        List<List<SemanticsConfiguration>> siblingMergeGroups)
    {
        MergeUp = mergeUp;
        SiblingMergeGroups = siblingMergeGroups;
    }

    public List<SemanticsConfiguration> MergeUp { get; }

    public List<List<SemanticsConfiguration>> SiblingMergeGroups { get; }
}

public sealed class SemanticsConfiguration
{
    public bool IsSemanticBoundary { get; set; }
    public bool IsMergingSemanticsOfDescendants { get; set; }
    public bool ExplicitChildNodes { get; set; }
    public bool IsBlockingSemanticsOfPreviouslyPaintedNodes { get; set; }
    public ChildSemanticsConfigurationsDelegate? ChildConfigurationsDelegate { get; set; }
    public bool IsExcluded { get; set; }
    public string? Label { get; set; }
    public SemanticsFlags Flags { get; set; } = SemanticsFlags.None;
    public SemanticsActions Actions { get; set; } = SemanticsActions.None;
    public Rect? ExplicitRect { get; set; }

    private Dictionary<SemanticsActions, Action>? _actionHandlers;
    internal bool HasActionHandlers => _actionHandlers is { Count: > 0 };
    internal IReadOnlyDictionary<SemanticsActions, Action> ActionHandlers => _actionHandlers ?? EmptyHandlers;

    private static readonly IReadOnlyDictionary<SemanticsActions, Action> EmptyHandlers =
        new Dictionary<SemanticsActions, Action>();

    public void AddActionHandler(SemanticsActions action, Action handler)
    {
        if (action == SemanticsActions.None)
        {
            throw new ArgumentException("Action handler cannot be registered for SemanticsActions.None.");
        }

        _actionHandlers ??= [];
        _actionHandlers[action] = handler;
        Actions |= action;
    }

    internal void ReplaceActionHandlers(Dictionary<SemanticsActions, Action> handlers)
    {
        _actionHandlers = handlers.Count == 0 ? null : handlers;
    }

    internal SemanticsConfiguration Clone()
    {
        var clone = new SemanticsConfiguration
        {
            IsSemanticBoundary = IsSemanticBoundary,
            IsMergingSemanticsOfDescendants = IsMergingSemanticsOfDescendants,
            ExplicitChildNodes = ExplicitChildNodes,
            IsBlockingSemanticsOfPreviouslyPaintedNodes = IsBlockingSemanticsOfPreviouslyPaintedNodes,
            ChildConfigurationsDelegate = ChildConfigurationsDelegate,
            IsExcluded = IsExcluded,
            Label = Label,
            Flags = Flags,
            Actions = Actions,
            ExplicitRect = ExplicitRect
        };

        if (_actionHandlers is { Count: > 0 })
        {
            clone._actionHandlers = new Dictionary<SemanticsActions, Action>(_actionHandlers);
        }

        return clone;
    }

    internal bool HasBeenAnnotated =>
        !string.IsNullOrWhiteSpace(Label)
        || Flags != SemanticsFlags.None
        || Actions != SemanticsActions.None
        || HasActionHandlers;

    internal bool IsCompatibleWith(SemanticsConfiguration? other)
    {
        if (other == null || !other.HasBeenAnnotated || !HasBeenAnnotated)
        {
            return true;
        }

        if ((Actions & other.Actions) != SemanticsActions.None)
        {
            return false;
        }

        if ((Flags & other.Flags) != SemanticsFlags.None)
        {
            return false;
        }

        return true;
    }

    internal void Absorb(SemanticsConfiguration child)
    {
        if (ExplicitChildNodes)
        {
            return;
        }

        if (!child.HasBeenAnnotated)
        {
            return;
        }

        Flags |= child.Flags;
        Actions |= child.Actions;

        if (!string.IsNullOrWhiteSpace(child.Label))
        {
            if (string.IsNullOrWhiteSpace(Label))
            {
                Label = child.Label;
            }
            else
            {
                Label = $"{Label} {child.Label}";
            }
        }

        if (child.HasActionHandlers)
        {
            _actionHandlers ??= [];
            foreach (var pair in child.ActionHandlers)
            {
                _actionHandlers.TryAdd(pair.Key, pair.Value);
            }
        }
    }
}

public sealed class SemanticsNode
{
    private readonly List<SemanticsNode> _children = [];
    private readonly Dictionary<SemanticsActions, Action> _actionHandlers = [];

    internal SemanticsNode(int id)
    {
        Id = id;
    }

    public int Id { get; }
    public Rect Rect { get; internal set; }
    public string? Label { get; internal set; }
    public SemanticsFlags Flags { get; internal set; }
    public SemanticsActions Actions { get; internal set; }
    public bool IsHidden { get; internal set; }
    public IReadOnlyList<SemanticsNode> Children => _children;
    internal bool BlocksPreviousNodes { get; set; }

    internal void ReplaceChildren(List<SemanticsNode> children)
    {
        _children.Clear();
        _children.AddRange(children);
    }

    internal void SetActionHandlers(IReadOnlyDictionary<SemanticsActions, Action> handlers)
    {
        _actionHandlers.Clear();
        foreach (var pair in handlers)
        {
            _actionHandlers[pair.Key] = pair.Value;
        }
    }

    internal void CopyActionHandlersTo(Dictionary<SemanticsActions, Action> target)
    {
        foreach (var pair in _actionHandlers)
        {
            target.TryAdd(pair.Key, pair.Value);
        }
    }

    internal bool PerformAction(SemanticsActions action)
    {
        if (_actionHandlers.TryGetValue(action, out var handler))
        {
            handler();
            return true;
        }

        return false;
    }
}

public sealed class SemanticsOwner
{
    private int _nextNodeId;
    private SemanticsNode? _syntheticRoot;
    private readonly Dictionary<int, SemanticsNode> _index = [];
    private readonly Dictionary<RenderObject, SemanticsNode> _nodesByRenderObject = [];

    public SemanticsNode? RootNode { get; private set; }

    internal SemanticsNode EnsureNode(RenderObject renderObject)
    {
        if (renderObject._semanticsNode != null)
        {
            _nodesByRenderObject[renderObject] = renderObject._semanticsNode;
            return renderObject._semanticsNode;
        }

        if (_nodesByRenderObject.TryGetValue(renderObject, out var existing))
        {
            renderObject._semanticsNode = existing;
            return existing;
        }

        var node = new SemanticsNode(++_nextNodeId);
        renderObject._semanticsNode = node;
        _nodesByRenderObject[renderObject] = node;
        return node;
    }

    internal SemanticsNode CreateDetachedNode()
    {
        return new SemanticsNode(++_nextNodeId);
    }

    internal void UpdateRoot(List<SemanticsNode> roots)
    {
        if (roots.Count == 0)
        {
            RootNode = null;
            _index.Clear();
            foreach (var pair in _nodesByRenderObject)
            {
                pair.Key._semanticsNode = null;
            }

            _nodesByRenderObject.Clear();
            return;
        }

        if (roots.Count == 1)
        {
            RootNode = roots[0];
            RebuildIndex();
            PruneUnusedRenderObjectNodes();
            return;
        }

        _syntheticRoot ??= new SemanticsNode(++_nextNodeId);
        _syntheticRoot.ReplaceChildren(roots);
        _syntheticRoot.Rect = UnionBounds(roots);
        _syntheticRoot.Label = null;
        _syntheticRoot.Flags = SemanticsFlags.None;
        _syntheticRoot.Actions = SemanticsActions.None;
        _syntheticRoot.IsHidden = false;
        _syntheticRoot.SetActionHandlers(new Dictionary<SemanticsActions, Action>());
        RootNode = _syntheticRoot;
        RebuildIndex();
        PruneUnusedRenderObjectNodes();
        return;
    }

    public bool PerformAction(int nodeId, SemanticsActions action)
    {
        if (action == SemanticsActions.None)
        {
            return false;
        }

        return _index.TryGetValue(nodeId, out var node) && node.PerformAction(action);
    }

    public string DebugDumpTree()
    {
        if (RootNode == null)
        {
            return "<empty>";
        }

        var builder = new StringBuilder();
        WriteNode(builder, RootNode, depth: 0);
        return builder.ToString().TrimEnd();
    }

    private void RebuildIndex()
    {
        _index.Clear();
        if (RootNode == null)
        {
            return;
        }

        VisitNode(RootNode, node => _index[node.Id] = node);
    }

    private static void VisitNode(SemanticsNode node, Action<SemanticsNode> visitor)
    {
        visitor(node);
        foreach (var child in node.Children)
        {
            VisitNode(child, visitor);
        }
    }

    private static void WriteNode(StringBuilder builder, SemanticsNode node, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append('#').Append(node.Id);
        builder.Append(" rect=").Append(node.Rect);

        if (!string.IsNullOrEmpty(node.Label))
        {
            builder.Append(" label=\"").Append(node.Label).Append('"');
        }

        if (node.Flags != SemanticsFlags.None)
        {
            builder.Append(" flags=").Append(node.Flags);
        }

        if (node.Actions != SemanticsActions.None)
        {
            builder.Append(" actions=").Append(node.Actions);
        }

        if (node.IsHidden)
        {
            builder.Append(" hidden");
        }

        builder.AppendLine();

        foreach (var child in node.Children)
        {
            WriteNode(builder, child, depth + 1);
        }
    }

    private static Rect UnionBounds(List<SemanticsNode> nodes)
    {
        var first = nodes[0].Rect;
        var minX = first.X;
        var minY = first.Y;
        var maxX = first.Right;
        var maxY = first.Bottom;

        for (var index = 1; index < nodes.Count; index++)
        {
            var rect = nodes[index].Rect;
            minX = Math.Min(minX, rect.X);
            minY = Math.Min(minY, rect.Y);
            maxX = Math.Max(maxX, rect.Right);
            maxY = Math.Max(maxY, rect.Bottom);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private void PruneUnusedRenderObjectNodes()
    {
        if (_nodesByRenderObject.Count == 0)
        {
            return;
        }

        var liveNodeIds = new HashSet<int>(_index.Keys);
        List<RenderObject>? staleOwners = null;

        foreach (var pair in _nodesByRenderObject)
        {
            if (liveNodeIds.Contains(pair.Value.Id))
            {
                continue;
            }

            pair.Key._semanticsNode = null;
            staleOwners ??= [];
            staleOwners.Add(pair.Key);
        }

        if (staleOwners == null)
        {
            return;
        }

        foreach (var owner in staleOwners)
        {
            _nodesByRenderObject.Remove(owner);
        }
    }
}
