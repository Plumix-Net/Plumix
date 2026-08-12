using Avalonia;
using System.Text;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/semantics.dart (approximate)
// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

public abstract record SemanticsSortKey(string? Name) : IComparable<SemanticsSortKey>
{
    public abstract int CompareTo(SemanticsSortKey? other);
}

public sealed record OrdinalSortKey(double Order, string? GroupName = null)
    : SemanticsSortKey(GroupName)
{
    public override int CompareTo(SemanticsSortKey? other)
    {
        if (other is null)
        {
            return -1;
        }

        if (other is not OrdinalSortKey ordinal)
        {
            return string.Compare(GetType().FullName, other.GetType().FullName, StringComparison.Ordinal);
        }

        int nameComparison = string.Compare(Name, ordinal.Name, StringComparison.Ordinal);
        return nameComparison != 0 ? nameComparison : Order.CompareTo(ordinal.Order);
    }
}

/// <summary>
/// How a semantics node participates in the platform's accessibility hit testing.
/// </summary>
public enum SemanticsHitTestBehavior
{
    /// <summary>Defer to the platform's default hit-test behavior inference.</summary>
    Defer,

    /// <summary>Consume pointer events within the node's bounds, blocking nodes behind it.</summary>
    Opaque,

    /// <summary>Let pointer events pass through to the elements behind the node.</summary>
    Transparent,
}

public enum SemanticsInputType
{
    None,
    Text,
    Url,
    Phone,
    Search,
    Email,
}

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
    HasExpandedState = 1 << 11,
    IsExpanded = 1 << 12,
    IsInMutuallyExclusiveGroup = 1 << 13,
    IsLiveRegion = 1 << 14,
    IsDialog = 1 << 15,
    IsAlertDialog = 1 << 16,
    ScopesRoute = 1 << 17,
    NamesRoute = 1 << 18,
    HasCheckedState = 1 << 19,
    IsInvalid = 1 << 20,
    IsFocusable = 1 << 21,
    IsCheckStateMixed = 1 << 22,
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

public sealed record CustomSemanticsAction
{
    public CustomSemanticsAction(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A custom semantics action label cannot be empty.", nameof(label));
        }

        Label = label;
    }

    public string Label { get; }
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
    public bool IsBlockingUserActions { get; set; }
    public ChildSemanticsConfigurationsDelegate? ChildConfigurationsDelegate { get; set; }
    public bool IsExcluded { get; set; }
    public string? Label { get; set; }
    public string? Hint { get; set; }
    public string? OnTapHint { get; set; }
    public string? Tooltip { get; set; }
    public string? Value { get; set; }
    public string? IncreasedValue { get; set; }
    public string? DecreasedValue { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public SemanticsRole Role { get; set; }
    public SemanticsInputType InputType { get; set; }
    public SemanticsHitTestBehavior HitTestBehavior { get; set; } = SemanticsHitTestBehavior.Defer;
    public SemanticsFlags Flags { get; set; } = SemanticsFlags.None;
    public SemanticsActions Actions { get; set; } = SemanticsActions.None;
    public Rect? ExplicitRect { get; set; }
    public int? IndexInParent { get; set; }
    public SemanticsSortKey? SortKey { get; set; }

    private Dictionary<SemanticsActions, Action>? _actionHandlers;
    private Dictionary<CustomSemanticsAction, Action>? _customActionHandlers;
    internal bool HasActionHandlers => _actionHandlers is { Count: > 0 };
    internal bool HasCustomActionHandlers => _customActionHandlers is { Count: > 0 };
    internal IReadOnlyDictionary<SemanticsActions, Action> ActionHandlers => _actionHandlers ?? EmptyHandlers;
    internal IReadOnlyDictionary<CustomSemanticsAction, Action> CustomActionHandlers =>
        _customActionHandlers ?? EmptyCustomHandlers;

    private static readonly IReadOnlyDictionary<SemanticsActions, Action> EmptyHandlers =
        new Dictionary<SemanticsActions, Action>();
    private static readonly IReadOnlyDictionary<CustomSemanticsAction, Action> EmptyCustomHandlers =
        new Dictionary<CustomSemanticsAction, Action>();

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

    public void AddCustomActionHandler(CustomSemanticsAction action, Action handler)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(handler);
        _customActionHandlers ??= [];
        _customActionHandlers[action] = handler;
    }

    internal void ReplaceActionHandlers(Dictionary<SemanticsActions, Action> handlers)
    {
        _actionHandlers = handlers.Count == 0 ? null : handlers;
    }

    internal void ReplaceCustomActionHandlers(Dictionary<CustomSemanticsAction, Action> handlers)
    {
        _customActionHandlers = handlers.Count == 0 ? null : handlers;
    }

    internal SemanticsConfiguration Clone()
    {
        var clone = new SemanticsConfiguration
        {
            IsSemanticBoundary = IsSemanticBoundary,
            IsMergingSemanticsOfDescendants = IsMergingSemanticsOfDescendants,
            ExplicitChildNodes = ExplicitChildNodes,
            IsBlockingSemanticsOfPreviouslyPaintedNodes = IsBlockingSemanticsOfPreviouslyPaintedNodes,
            IsBlockingUserActions = IsBlockingUserActions,
            ChildConfigurationsDelegate = ChildConfigurationsDelegate,
            IsExcluded = IsExcluded,
            Label = Label,
            Hint = Hint,
            OnTapHint = OnTapHint,
            Tooltip = Tooltip,
            Value = Value,
            IncreasedValue = IncreasedValue,
            DecreasedValue = DecreasedValue,
            MinValue = MinValue,
            MaxValue = MaxValue,
            Role = Role,
            InputType = InputType,
            HitTestBehavior = HitTestBehavior,
            Flags = Flags,
            Actions = Actions,
            ExplicitRect = ExplicitRect,
            IndexInParent = IndexInParent,
            SortKey = SortKey
        };

        if (_actionHandlers is { Count: > 0 })
        {
            clone._actionHandlers = new Dictionary<SemanticsActions, Action>(_actionHandlers);
        }

        if (_customActionHandlers is { Count: > 0 })
        {
            clone._customActionHandlers = new Dictionary<CustomSemanticsAction, Action>(_customActionHandlers);
        }

        return clone;
    }

    internal void ClearActionHandlers()
    {
        Actions = SemanticsActions.None;
        _actionHandlers = null;
        _customActionHandlers = null;
    }

    internal bool HasBeenAnnotated =>
        !string.IsNullOrWhiteSpace(Label)
        || !string.IsNullOrWhiteSpace(Hint)
        || !string.IsNullOrWhiteSpace(OnTapHint)
        || !string.IsNullOrWhiteSpace(Tooltip)
        || !string.IsNullOrWhiteSpace(Value)
        || !string.IsNullOrWhiteSpace(IncreasedValue)
        || !string.IsNullOrWhiteSpace(DecreasedValue)
        || !string.IsNullOrWhiteSpace(MinValue)
        || !string.IsNullOrWhiteSpace(MaxValue)
        || Role != SemanticsRole.None
        || InputType != SemanticsInputType.None
        || HitTestBehavior != SemanticsHitTestBehavior.Defer
        || Flags != SemanticsFlags.None
        || Actions != SemanticsActions.None
        || IndexInParent.HasValue
        || HasActionHandlers
        || HasCustomActionHandlers;

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

        if (CustomActionHandlers.Keys.Any(other.CustomActionHandlers.ContainsKey))
        {
            return false;
        }

        if ((Flags & other.Flags) != SemanticsFlags.None)
        {
            return false;
        }

        if (Role != SemanticsRole.None
            && other.Role != SemanticsRole.None
            && Role != other.Role)
        {
            return false;
        }

        if (InputType != SemanticsInputType.None
            && other.InputType != SemanticsInputType.None
            && InputType != other.InputType)
        {
            return false;
        }

        if (HitTestBehavior != SemanticsHitTestBehavior.Defer
            || other.HitTestBehavior != SemanticsHitTestBehavior.Defer)
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
        IndexInParent ??= child.IndexInParent;
        SortKey ??= child.SortKey;
        if (Role == SemanticsRole.None)
        {
            Role = child.Role;
        }
        if (InputType == SemanticsInputType.None)
        {
            InputType = child.InputType;
        }

        if (HitTestBehavior == SemanticsHitTestBehavior.Defer
            && child.HitTestBehavior != SemanticsHitTestBehavior.Defer)
        {
            HitTestBehavior = child.HitTestBehavior;
        }

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

        Value ??= child.Value;
        IncreasedValue ??= child.IncreasedValue;
        DecreasedValue ??= child.DecreasedValue;
        MinValue ??= child.MinValue;
        MaxValue ??= child.MaxValue;

        if (!string.IsNullOrWhiteSpace(child.Hint))
        {
            Hint = string.IsNullOrWhiteSpace(Hint) ? child.Hint : $"{Hint} {child.Hint}";
        }

        OnTapHint ??= child.OnTapHint;

        if (!string.IsNullOrWhiteSpace(child.Tooltip))
        {
            Tooltip = string.IsNullOrWhiteSpace(Tooltip)
                ? child.Tooltip
                : $"{Tooltip} {child.Tooltip}";
        }

        if (child.HasActionHandlers)
        {
            _actionHandlers ??= [];
            foreach (var pair in child.ActionHandlers)
            {
                _actionHandlers.TryAdd(pair.Key, pair.Value);
            }
        }


        if (child.HasCustomActionHandlers)
        {
            _customActionHandlers ??= [];
            foreach (var pair in child.CustomActionHandlers)
            {
                _customActionHandlers.TryAdd(pair.Key, pair.Value);
            }
        }
    }
}

public sealed class SemanticsNode
{
    private readonly List<SemanticsNode> _children = [];
    private readonly Dictionary<SemanticsActions, Action> _actionHandlers = [];
    private readonly Dictionary<CustomSemanticsAction, Action> _customActionHandlers = [];

    internal SemanticsNode(int id)
    {
        Id = id;
    }

    public int Id { get; }
    public Rect Rect { get; set; }
    public string? Label { get; internal set; }
    public string? Hint { get; internal set; }
    public string? OnTapHint { get; internal set; }
    public string? Tooltip { get; internal set; }
    public string? Value { get; internal set; }
    public string? IncreasedValue { get; internal set; }
    public string? DecreasedValue { get; internal set; }
    public string? MinValue { get; internal set; }
    public string? MaxValue { get; internal set; }
    public SemanticsRole Role { get; internal set; }
    public SemanticsInputType InputType { get; internal set; }
    public SemanticsHitTestBehavior HitTestBehavior { get; internal set; } = SemanticsHitTestBehavior.Defer;
    public SemanticsFlags Flags { get; internal set; }
    public SemanticsActions Actions { get; internal set; }
    public int? IndexInParent { get; set; }
    public SemanticsSortKey? SortKey { get; internal set; }
    public bool IsHidden { get; internal set; }
    public IReadOnlyList<SemanticsNode> Children => _children;
    public IReadOnlyDictionary<CustomSemanticsAction, Action> CustomSemanticsActions => _customActionHandlers;
    internal bool BlocksPreviousNodes { get; set; }
    internal bool IsSemanticBoundary { get; set; }

    /// <summary>
    /// Reconfigures this node with <paramref name="config"/> and replaces its children.
    /// </summary>
    /// <remarks>
    /// Geometry (<see cref="Rect"/>, <see cref="IsHidden"/>) is owned by the semantics compiler and is not
    /// touched here, so a render object that synthesizes extra nodes from
    /// <c>RenderObject.AssembleSemanticsNode</c> assigns it explicitly.
    /// </remarks>
    public void UpdateWith(
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode>? childrenInInversePaintOrder = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        Label = config.Label;
        Hint = config.Hint;
        OnTapHint = config.OnTapHint;
        Tooltip = config.Tooltip;
        Value = config.Value;
        IncreasedValue = config.IncreasedValue;
        DecreasedValue = config.DecreasedValue;
        MinValue = config.MinValue;
        MaxValue = config.MaxValue;
        Role = config.Role;
        InputType = config.InputType;
        HitTestBehavior = config.HitTestBehavior;
        Flags = config.Flags;
        Actions = config.Actions;
        IndexInParent = config.IndexInParent;
        SortKey = config.SortKey;
        IsSemanticBoundary = config.IsSemanticBoundary;
        ReplaceChildren(SortChildren(childrenInInversePaintOrder ?? []));
        SetActionHandlers(config.ActionHandlers);
        SetCustomActionHandlers(config.CustomActionHandlers);
    }

    internal void ReplaceChildren(IReadOnlyList<SemanticsNode> children)
    {
        _children.Clear();
        _children.AddRange(children);
    }

    private static IReadOnlyList<SemanticsNode> SortChildren(IReadOnlyList<SemanticsNode> children)
    {
        if (children.Count < 2 || children.All(static child => child.SortKey is null))
        {
            return children;
        }

        return children
            .Select(static (child, index) => (child, index))
            .OrderBy(static pair => pair.child.SortKey is null ? 1 : 0)
            .ThenBy(static pair => pair.child.SortKey, SemanticsSortKeyComparer.Instance)
            .ThenBy(static pair => pair.index)
            .Select(static pair => pair.child)
            .ToList();
    }

    private sealed class SemanticsSortKeyComparer : IComparer<SemanticsSortKey?>
    {
        public static SemanticsSortKeyComparer Instance { get; } = new();

        public int Compare(SemanticsSortKey? x, SemanticsSortKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            return x.CompareTo(y);
        }
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

    internal void SetCustomActionHandlers(IReadOnlyDictionary<CustomSemanticsAction, Action> handlers)
    {
        _customActionHandlers.Clear();
        foreach (var pair in handlers)
        {
            _customActionHandlers[pair.Key] = pair.Value;
        }
    }

    internal void CopyCustomActionHandlersTo(Dictionary<CustomSemanticsAction, Action> target)
    {
        foreach (var pair in _customActionHandlers)
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

    internal bool PerformCustomAction(CustomSemanticsAction action)
    {
        if (_customActionHandlers.TryGetValue(action, out var handler))
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
        _syntheticRoot.Hint = null;
        _syntheticRoot.OnTapHint = null;
        _syntheticRoot.Tooltip = null;
        _syntheticRoot.Value = null;
        _syntheticRoot.IncreasedValue = null;
        _syntheticRoot.DecreasedValue = null;
        _syntheticRoot.MinValue = null;
        _syntheticRoot.MaxValue = null;
        _syntheticRoot.Role = SemanticsRole.None;
        _syntheticRoot.InputType = SemanticsInputType.None;
        _syntheticRoot.HitTestBehavior = SemanticsHitTestBehavior.Defer;
        _syntheticRoot.Flags = SemanticsFlags.None;
        _syntheticRoot.Actions = SemanticsActions.None;
        _syntheticRoot.IndexInParent = null;
        _syntheticRoot.IsHidden = false;
        _syntheticRoot.SetActionHandlers(new Dictionary<SemanticsActions, Action>());
        _syntheticRoot.SetCustomActionHandlers(new Dictionary<CustomSemanticsAction, Action>());
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

    public bool PerformCustomAction(int nodeId, CustomSemanticsAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _index.TryGetValue(nodeId, out var node) && node.PerformCustomAction(action);
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

        if (!string.IsNullOrEmpty(node.Value))
        {
            builder.Append(" value=\"").Append(node.Value).Append('"');
        }

        if (!string.IsNullOrEmpty(node.OnTapHint))
        {
            builder.Append(" onTapHint=\"").Append(node.OnTapHint).Append('"');
        }

        if (node.Flags != SemanticsFlags.None)
        {
            builder.Append(" flags=").Append(node.Flags);
        }

        if (node.InputType != SemanticsInputType.None)
        {
            builder.Append(" inputType=").Append(node.InputType);
        }

        if (node.HitTestBehavior != SemanticsHitTestBehavior.Defer)
        {
            builder.Append(" hitTestBehavior=").Append(node.HitTestBehavior);
        }

        if (node.Actions != SemanticsActions.None)
        {
            builder.Append(" actions=").Append(node.Actions);
        }

        if (node.IndexInParent.HasValue)
        {
            builder.Append(" indexInParent=").Append(node.IndexInParent.Value);
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
        double minX = first.X;
        double minY = first.Y;
        double maxX = first.Right;
        double maxY = first.Bottom;

        for (int index = 1; index < nodes.Count; index++)
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
