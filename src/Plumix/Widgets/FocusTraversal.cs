using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_traversal.dart

namespace Plumix.Widgets;

/// <summary>Dart parity source: <c>TraversalRequestFocusCallback</c>.</summary>
public delegate void TraversalRequestFocusCallback(
    FocusNode node,
    ScrollPositionAlignmentPolicy? alignmentPolicy = null,
    double? alignment = null,
    TimeSpan? duration = null,
    Curve? curve = null);

/// <summary>Dart parity source: <c>TraversalDirection</c>.</summary>
public enum TraversalDirection
{
    Up,
    Right,
    Down,
    Left,
}

/// <summary>Dart parity source: <c>TraversalEdgeBehavior</c>.</summary>
public enum TraversalEdgeBehavior
{
    ClosedLoop,
    LeaveFlutterView,
    ParentScope,
    Stop,
}

/// <summary>Dart parity source: <c>_FocusTraversalGroupInfo</c>.</summary>
internal sealed class FocusTraversalGroupInfo
{
    internal FocusTraversalGroupInfo(
        FocusTraversalGroupNode? group,
        FocusTraversalPolicy? defaultPolicy = null,
        List<FocusNode>? members = null)
    {
        GroupNode = group;
        Policy = group?.Policy ?? defaultPolicy ?? new ReadingOrderTraversalPolicy();
        Members = members ?? [];
    }

    internal FocusNode? GroupNode { get; }

    internal FocusTraversalPolicy Policy { get; }

    internal List<FocusNode> Members { get; }
}

/// <summary>Dart parity source: <c>FocusTraversalPolicy</c>.</summary>
public abstract class FocusTraversalPolicy
{
    protected FocusTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
    {
        RequestFocusCallback = requestFocusCallback ?? DefaultTraversalRequestFocusCallback;
    }

    public TraversalRequestFocusCallback RequestFocusCallback { get; }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.defaultTraversalRequestFocusCallback</c>.</summary>
    public static void DefaultTraversalRequestFocusCallback(
        FocusNode node,
        ScrollPositionAlignmentPolicy? alignmentPolicy = null,
        double? alignment = null,
        TimeSpan? duration = null,
        Curve? curve = null)
    {
        node.RequestFocus();
        if (node.Context is not { } context)
        {
            return;
        }

        _ = Scrollable.EnsureVisible(
            context,
            alignment: alignment ?? 1.0,
            duration: duration ?? TimeSpan.Zero,
            curve: curve ?? Curves.Ease,
            alignmentPolicy: alignmentPolicy ?? ScrollPositionAlignmentPolicy.Explicit);
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._requestTabTraversalFocus</c>.</summary>
    private bool RequestTabTraversalFocus(
        FocusNode node,
        bool forward,
        ScrollPositionAlignmentPolicy? alignmentPolicy = null,
        double? alignment = null,
        TimeSpan? duration = null,
        Curve? curve = null)
    {
        if (node is FocusScopeNode scopeNode)
        {
            if (scopeNode.FocusedChild is { } focusedChild)
            {
                return RequestTabTraversalFocus(
                    focusedChild, forward, alignmentPolicy, alignment, duration, curve);
            }

            List<FocusNode> sortedChildren = SortAllDescendants(scopeNode, scopeNode);
            if (sortedChildren.Count > 0)
            {
                RequestTabTraversalFocus(
                    forward ? sortedChildren[0] : sortedChildren[^1],
                    forward,
                    alignmentPolicy,
                    alignment,
                    duration,
                    curve);
                return true;
            }
        }

        bool nodeHadPrimaryFocus = node.HasPrimaryFocus;
        RequestFocusCallback(node, alignmentPolicy, alignment, duration, curve);
        return !nodeHadPrimaryFocus;
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.findFirstFocus</c>.</summary>
    public FocusNode? FindFirstFocus(FocusNode currentNode, bool ignoreCurrentFocus = false) =>
        FindInitialFocus(currentNode, fromEnd: false, ignoreCurrentFocus: ignoreCurrentFocus);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.findLastFocus</c>.</summary>
    public FocusNode FindLastFocus(FocusNode currentNode, bool ignoreCurrentFocus = false) =>
        FindInitialFocus(currentNode, fromEnd: true, ignoreCurrentFocus: ignoreCurrentFocus);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._findInitialFocus</c>.</summary>
    private FocusNode FindInitialFocus(FocusNode currentNode, bool fromEnd, bool ignoreCurrentFocus)
    {
        FocusScopeNode scope = currentNode.NearestScope ?? FocusManager.Instance.RootScope;
        FocusNode? candidate = scope.FocusedChild;
        if (ignoreCurrentFocus || (candidate == null && scope.Descendants.Count > 0))
        {
            List<FocusNode> sorted = SortAllDescendants(scope, currentNode)
                .Where(CanRequestTraversalFocus)
                .ToList();
            candidate = sorted.Count == 0 ? null : fromEnd ? sorted[^1] : sorted[0];
        }

        return candidate ?? currentNode;
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.findFirstFocusInDirection</c>.</summary>
    public abstract FocusNode? FindFirstFocusInDirection(FocusNode currentNode, TraversalDirection direction);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.invalidateScopeData</c>.</summary>
    public virtual void InvalidateScopeData(FocusScopeNode node)
    {
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.changedScope</c>.</summary>
    public virtual void ChangedScope(FocusNode? node = null, FocusScopeNode? oldScope = null)
    {
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.next</c>.</summary>
    public bool Next(FocusNode currentNode) => MoveFocus(currentNode, forward: true);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.previous</c>.</summary>
    public bool Previous(FocusNode currentNode) => MoveFocus(currentNode, forward: false);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.inDirection</c>.</summary>
    public abstract bool InDirection(FocusNode currentNode, TraversalDirection direction);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy.sortDescendants</c>.</summary>
    public abstract IEnumerable<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode);

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._canRequestTraversalFocus</c>.</summary>
    private protected static bool CanRequestTraversalFocus(FocusNode node) =>
        node.CanRequestFocus && !node.SkipTraversal;

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._getDescendantsWithoutExpandingScope</c>.</summary>
    private static IEnumerable<FocusNode> GetDescendantsWithoutExpandingScope(FocusNode node)
    {
        var result = new List<FocusNode>();
        foreach (FocusNode child in node.Children)
        {
            result.Add(child);
            if (child is not FocusScopeNode)
            {
                result.AddRange(GetDescendantsWithoutExpandingScope(child));
            }
        }

        return result;
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._findGroups</c>.</summary>
    private static (Dictionary<FocusNode, FocusTraversalGroupInfo> Groups, FocusTraversalGroupInfo? Rootless)
        FindGroups(FocusScopeNode scope, FocusTraversalGroupNode? scopeGroupNode, FocusNode currentNode)
    {
        FocusTraversalPolicy defaultPolicy = scopeGroupNode?.Policy ?? new ReadingOrderTraversalPolicy();
        var groups = new Dictionary<FocusNode, FocusTraversalGroupInfo>(ReferenceEqualityComparer.Instance);
        FocusTraversalGroupInfo? rootless = null;

        FocusTraversalGroupInfo GroupFor(FocusTraversalGroupNode? key)
        {
            if (key == null)
            {
                return rootless ??= new FocusTraversalGroupInfo(null, defaultPolicy, []);
            }

            if (!groups.TryGetValue(key, out FocusTraversalGroupInfo? info))
            {
                info = new FocusTraversalGroupInfo(key, defaultPolicy, []);
                groups[key] = info;
            }

            return info;
        }

        foreach (FocusNode node in GetDescendantsWithoutExpandingScope(scope))
        {
            FocusTraversalGroupNode? groupNode = FocusTraversalGroup.GetGroupNode(node);
            if (ReferenceEquals(node, groupNode))
            {
                FocusTraversalGroupNode? parentGroup = groupNode!.Parent == null
                    ? null
                    : FocusTraversalGroup.GetGroupNode(groupNode.Parent);
                GroupFor(parentGroup).Members.Add(groupNode);
                continue;
            }

            if (ReferenceEquals(node, currentNode) || (node.CanRequestFocus && !node.SkipTraversal))
            {
                GroupFor(groupNode).Members.Add(node);
            }
        }

        return (groups, rootless);
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._sortAllDescendants</c>.</summary>
    private protected List<FocusNode> SortAllDescendants(FocusScopeNode scope, FocusNode currentNode)
    {
        FocusTraversalGroupNode? scopeGroupNode = FocusTraversalGroup.GetGroupNode(scope);
        (Dictionary<FocusNode, FocusTraversalGroupInfo> groups, FocusTraversalGroupInfo? rootless) =
            FindGroups(scope, scopeGroupNode, currentNode);

        IEnumerable<FocusTraversalGroupInfo> allInfos = rootless == null
            ? groups.Values
            : groups.Values.Append(rootless);
        foreach (FocusTraversalGroupInfo info in allInfos.ToList())
        {
            List<FocusNode> sortedMembers = info.Policy.SortDescendants(info.Members, currentNode).ToList();
            info.Members.Clear();
            info.Members.AddRange(sortedMembers);
        }

        FocusTraversalGroupInfo? scopeGroupInfo = scopeGroupNode == null
            ? rootless
            : groups.GetValueOrDefault(scopeGroupNode);

        var sortedDescendants = new List<FocusNode>();

        void VisitGroups(FocusTraversalGroupInfo info)
        {
            foreach (FocusNode node in info.Members)
            {
                if (groups.TryGetValue(node, out FocusTraversalGroupInfo? nested))
                {
                    VisitGroups(nested);
                }
                else
                {
                    sortedDescendants.Add(node);
                }
            }
        }

        if (scopeGroupInfo != null)
        {
            VisitGroups(scopeGroupInfo);
        }

        sortedDescendants.RemoveAll(
            node => !ReferenceEquals(node, currentNode) && !CanRequestTraversalFocus(node));
        return sortedDescendants;
    }

    /// <summary>Dart parity source: <c>FocusTraversalPolicy._moveFocus</c>.</summary>
    private bool MoveFocus(FocusNode currentNode, bool forward)
    {
        FocusScopeNode nearestScope = currentNode.NearestScope ?? FocusManager.Instance.RootScope;
        InvalidateScopeData(nearestScope);
        FocusNode? focusedChild = nearestScope.FocusedChild;
        if (focusedChild == null)
        {
            FocusNode? firstFocus = forward ? FindFirstFocus(currentNode) : FindLastFocus(currentNode);
            if (firstFocus != null)
            {
                return RequestTabTraversalFocus(
                    firstFocus,
                    forward,
                    alignmentPolicy: forward
                        ? ScrollPositionAlignmentPolicy.KeepVisibleAtEnd
                        : ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
            }
        }

        focusedChild ??= nearestScope;
        List<FocusNode> sortedNodes = SortAllDescendants(nearestScope, focusedChild);
        if (sortedNodes.Count == 0)
        {
            return false;
        }

        if (forward && ReferenceEquals(focusedChild, sortedNodes[^1]))
        {
            switch (nearestScope.TraversalEdgeBehavior)
            {
                case TraversalEdgeBehavior.LeaveFlutterView:
                    focusedChild.Unfocus();
                    return false;
                case TraversalEdgeBehavior.ParentScope:
                    FocusScopeNode? parentScope = nearestScope.EnclosingScope;
                    if (parentScope != null && !ReferenceEquals(parentScope, FocusManager.Instance.RootScope))
                    {
                        focusedChild.Unfocus();
                        parentScope.NextFocus();
                        return !ReferenceEquals(focusedChild.EnclosingScope?.FocusedChild, focusedChild);
                    }

                    return RequestTabTraversalFocus(
                        sortedNodes[0], forward, ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
                case TraversalEdgeBehavior.ClosedLoop:
                    return RequestTabTraversalFocus(
                        sortedNodes[0], forward, ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
                case TraversalEdgeBehavior.Stop:
                default:
                    return false;
            }
        }

        if (!forward && ReferenceEquals(focusedChild, sortedNodes[0]))
        {
            switch (nearestScope.TraversalEdgeBehavior)
            {
                case TraversalEdgeBehavior.LeaveFlutterView:
                    focusedChild.Unfocus();
                    return false;
                case TraversalEdgeBehavior.ParentScope:
                    FocusScopeNode? parentScope = nearestScope.EnclosingScope;
                    if (parentScope != null && !ReferenceEquals(parentScope, FocusManager.Instance.RootScope))
                    {
                        focusedChild.Unfocus();
                        parentScope.PreviousFocus();
                        return !ReferenceEquals(focusedChild.EnclosingScope?.FocusedChild, focusedChild);
                    }

                    return RequestTabTraversalFocus(
                        sortedNodes[^1], forward, ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
                case TraversalEdgeBehavior.ClosedLoop:
                    return RequestTabTraversalFocus(
                        sortedNodes[^1], forward, ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
                case TraversalEdgeBehavior.Stop:
                default:
                    return false;
            }
        }

        IEnumerable<FocusNode> maybeFlipped = forward ? sortedNodes : Enumerable.Reverse(sortedNodes);
        FocusNode? previousNode = null;
        foreach (FocusNode node in maybeFlipped)
        {
            if (ReferenceEquals(previousNode, focusedChild))
            {
                return RequestTabTraversalFocus(
                    node,
                    forward,
                    alignmentPolicy: forward
                        ? ScrollPositionAlignmentPolicy.KeepVisibleAtEnd
                        : ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
            }

            previousNode = node;
        }

        return false;
    }

    /// <summary>A stable sort, matching Dart's <c>mergeSort</c>.</summary>
    private protected static List<T> StableSort<T>(IEnumerable<T> source, Comparison<T> compare) =>
        source.OrderBy(static item => item, Comparer<T>.Create(compare)).ToList();
}

/// <summary>Dart parity source: <c>_DirectionalPolicyDataEntry</c>.</summary>
internal readonly record struct DirectionalPolicyDataEntry(TraversalDirection Direction, FocusNode Node);

/// <summary>Dart parity source: <c>_DirectionalPolicyData</c>.</summary>
internal sealed class DirectionalPolicyData
{
    internal DirectionalPolicyData(List<DirectionalPolicyDataEntry> history)
    {
        History = history;
    }

    internal List<DirectionalPolicyDataEntry> History { get; }
}

/// <summary>
/// Dart parity source: <c>DirectionalFocusTraversalPolicyMixin</c>. C# has no mixins, so the mixin
/// becomes the abstract base every built-in policy derives from (see <c>docs/ai/DIVERGENCES.md</c>).
/// </summary>
public abstract class DirectionalFocusTraversalPolicy : FocusTraversalPolicy
{
    private readonly Dictionary<FocusScopeNode, DirectionalPolicyData> _policyData =
        new(ReferenceEqualityComparer.Instance);

    protected DirectionalFocusTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
        : base(requestFocusCallback)
    {
    }

    public override void InvalidateScopeData(FocusScopeNode node)
    {
        base.InvalidateScopeData(node);
        _policyData.Remove(node);
    }

    public override void ChangedScope(FocusNode? node = null, FocusScopeNode? oldScope = null)
    {
        base.ChangedScope(node, oldScope);
        if (oldScope != null && _policyData.TryGetValue(oldScope, out DirectionalPolicyData? data))
        {
            data.History.RemoveAll(entry => ReferenceEquals(entry.Node, node));
        }
    }

    public override FocusNode? FindFirstFocusInDirection(FocusNode currentNode, TraversalDirection direction)
    {
        IEnumerable<FocusNode> nodes =
            (currentNode.NearestScope ?? FocusManager.Instance.RootScope).TraversalDescendants;
        (bool vertical, bool first) = direction switch
        {
            TraversalDirection.Up => (true, false),
            TraversalDirection.Down => (true, true),
            TraversalDirection.Left => (false, false),
            TraversalDirection.Right => (false, true),
            _ => (true, true),
        };

        List<FocusNode> sorted = StableSort(nodes, (a, b) => vertical
            ? first
                ? a.Rect.Top.CompareTo(b.Rect.Top)
                : b.Rect.Bottom.CompareTo(a.Rect.Bottom)
            : first
                ? a.Rect.Left.CompareTo(b.Rect.Left)
                : b.Rect.Right.CompareTo(a.Rect.Right));
        return sorted.Count == 0 ? null : sorted[0];
    }

    /// <summary>Dart parity source: <c>DirectionalFocusTraversalPolicyMixin._findNextFocusInDirection</c>.</summary>
    private FocusNode? FindNextFocusInDirection(
        FocusNode focusedChild,
        IEnumerable<FocusNode> traversalDescendants,
        TraversalDirection direction,
        bool forward = true)
    {
        Rect focusedRect = focusedChild.Rect;
        switch (direction)
        {
            case TraversalDirection.Down:
            case TraversalDirection.Up:
            {
                IEnumerable<FocusNode> eligibleNodes = SortAndFilterVertically(
                    direction, focusedRect, traversalDescendants, forward);
                if (!eligibleNodes.Any())
                {
                    break;
                }

                eligibleNodes = FilterToSameScrollable(focusedChild, eligibleNodes, Axis.Vertical);
                if (direction == TraversalDirection.Up)
                {
                    eligibleNodes = eligibleNodes.Reverse();
                }

                List<FocusNode> inBand = eligibleNodes
                    .Where(node => OverlapsHorizontalExtent(node.Rect, focusedRect))
                    .ToList();
                if (inBand.Count > 0)
                {
                    List<FocusNode> byDistance = SortByDistancePreferVertical(focusedRect.Center, inBand);
                    return forward ? byDistance[0] : byDistance[^1];
                }

                List<FocusNode> closest = SortClosestEdgesByDistancePreferHorizontal(
                    focusedRect.Center, eligibleNodes);
                return closest.Count == 0 ? null : forward ? closest[0] : closest[^1];
            }

            case TraversalDirection.Right:
            case TraversalDirection.Left:
            {
                IEnumerable<FocusNode> eligibleNodes = SortAndFilterHorizontally(
                    direction, focusedRect, traversalDescendants, forward);
                if (!eligibleNodes.Any())
                {
                    break;
                }

                eligibleNodes = FilterToSameScrollable(focusedChild, eligibleNodes, Axis.Horizontal);
                if (direction == TraversalDirection.Left)
                {
                    eligibleNodes = eligibleNodes.Reverse();
                }

                List<FocusNode> inBand = eligibleNodes
                    .Where(node => OverlapsVerticalExtent(node.Rect, focusedRect))
                    .ToList();
                if (inBand.Count > 0)
                {
                    List<FocusNode> byDistance = SortByDistancePreferHorizontal(focusedRect.Center, inBand);
                    return forward ? byDistance[0] : byDistance[^1];
                }

                List<FocusNode> closest = SortClosestEdgesByDistancePreferVertical(
                    focusedRect.Center, eligibleNodes);
                return closest.Count == 0 ? null : forward ? closest[0] : closest[^1];
            }
        }

        return null;
    }

    private static IEnumerable<FocusNode> FilterToSameScrollable(
        FocusNode focusedChild,
        IEnumerable<FocusNode> eligibleNodes,
        Axis axis)
    {
        if (focusedChild.Context is not { } focusedContext)
        {
            return eligibleNodes;
        }

        Scrollable.ScrollableState? focusedScrollable = Scrollable.MaybeOf(focusedContext, axis);
        if (focusedScrollable == null)
        {
            return eligibleNodes;
        }

        List<FocusNode> filtered = eligibleNodes
            .Where(node => node.Context is { } context
                           && ReferenceEquals(Scrollable.MaybeOf(context, axis), focusedScrollable))
            .ToList();
        return filtered.Count > 0 ? filtered : eligibleNodes;
    }

    /// <summary>Dart's <c>!node.rect.intersect(Rect.fromLTRB(f.left, -inf, f.right, inf)).isEmpty</c>.</summary>
    private static bool OverlapsHorizontalExtent(Rect node, Rect focused) =>
        Math.Max(node.Left, focused.Left) < Math.Min(node.Right, focused.Right) && node.Top < node.Bottom;

    /// <summary>Dart's <c>!node.rect.intersect(Rect.fromLTRB(-inf, f.top, inf, f.bottom)).isEmpty</c>.</summary>
    private static bool OverlapsVerticalExtent(Rect node, Rect focused) =>
        Math.Max(node.Top, focused.Top) < Math.Min(node.Bottom, focused.Bottom) && node.Left < node.Right;

    private static int VerticalCompare(Point target, Point a, Point b) =>
        Math.Abs(a.Y - target.Y).CompareTo(Math.Abs(b.Y - target.Y));

    private static int HorizontalCompare(Point target, Point a, Point b) =>
        Math.Abs(a.X - target.X).CompareTo(Math.Abs(b.X - target.X));

    private static List<FocusNode> SortByDistancePreferVertical(Point target, IEnumerable<FocusNode> nodes) =>
        StableSort(nodes, (nodeA, nodeB) =>
        {
            Point a = nodeA.Rect.Center;
            Point b = nodeB.Rect.Center;
            int vertical = VerticalCompare(target, a, b);
            return vertical == 0 ? HorizontalCompare(target, a, b) : vertical;
        });

    private static List<FocusNode> SortByDistancePreferHorizontal(Point target, IEnumerable<FocusNode> nodes) =>
        StableSort(nodes, (nodeA, nodeB) =>
        {
            Point a = nodeA.Rect.Center;
            Point b = nodeB.Rect.Center;
            int horizontal = HorizontalCompare(target, a, b);
            return horizontal == 0 ? VerticalCompare(target, a, b) : horizontal;
        });

    private static int VerticalCompareClosestEdge(Point target, Rect a, Rect b)
    {
        double aCoord = Math.Abs(a.Top - target.Y) < Math.Abs(a.Bottom - target.Y) ? a.Top : a.Bottom;
        double bCoord = Math.Abs(b.Top - target.Y) < Math.Abs(b.Bottom - target.Y) ? b.Top : b.Bottom;
        return Math.Abs(aCoord - target.Y).CompareTo(Math.Abs(bCoord - target.Y));
    }

    private static int HorizontalCompareClosestEdge(Point target, Rect a, Rect b)
    {
        double aCoord = Math.Abs(a.Left - target.X) < Math.Abs(a.Right - target.X) ? a.Left : a.Right;
        double bCoord = Math.Abs(b.Left - target.X) < Math.Abs(b.Right - target.X) ? b.Left : b.Right;
        return Math.Abs(aCoord - target.X).CompareTo(Math.Abs(bCoord - target.X));
    }

    private static List<FocusNode> SortClosestEdgesByDistancePreferHorizontal(
        Point target,
        IEnumerable<FocusNode> nodes) =>
        StableSort(nodes, (nodeA, nodeB) =>
        {
            int horizontal = HorizontalCompareClosestEdge(target, nodeA.Rect, nodeB.Rect);
            return horizontal == 0
                ? VerticalCompare(target, nodeA.Rect.Center, nodeB.Rect.Center)
                : horizontal;
        });

    private static List<FocusNode> SortClosestEdgesByDistancePreferVertical(
        Point target,
        IEnumerable<FocusNode> nodes) =>
        StableSort(nodes, (nodeA, nodeB) =>
        {
            int vertical = VerticalCompareClosestEdge(target, nodeA.Rect, nodeB.Rect);
            return vertical == 0
                ? HorizontalCompare(target, nodeA.Rect.Center, nodeB.Rect.Center)
                : vertical;
        });

    private static List<FocusNode> SortAndFilterHorizontally(
        TraversalDirection direction,
        Rect target,
        IEnumerable<FocusNode> nodes,
        bool forward)
    {
        Func<FocusNode, bool> predicate = direction switch
        {
            TraversalDirection.Left => node => node.Rect != target
                                               && (forward
                                                   ? node.Rect.Center.X <= target.Left
                                                   : node.Rect.Center.X >= target.Left),
            TraversalDirection.Right => node => node.Rect != target
                                                && (forward
                                                    ? node.Rect.Center.X >= target.Right
                                                    : node.Rect.Center.X <= target.Right),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction."),
        };

        return StableSort(
            nodes.Where(predicate),
            static (a, b) => a.Rect.Center.X.CompareTo(b.Rect.Center.X));
    }

    private static List<FocusNode> SortAndFilterVertically(
        TraversalDirection direction,
        Rect target,
        IEnumerable<FocusNode> nodes,
        bool forward)
    {
        Func<FocusNode, bool> predicate = direction switch
        {
            TraversalDirection.Up => node => node.Rect != target
                                             && (forward
                                                 ? node.Rect.Center.Y <= target.Top
                                                 : node.Rect.Center.Y >= target.Top),
            TraversalDirection.Down => node => node.Rect != target
                                               && (forward
                                                   ? node.Rect.Center.Y >= target.Bottom
                                                   : node.Rect.Center.Y <= target.Bottom),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction."),
        };

        return StableSort(
            nodes.Where(predicate),
            static (a, b) => a.Rect.Center.Y.CompareTo(b.Rect.Center.Y));
    }

    /// <summary>Dart parity source: <c>DirectionalFocusTraversalPolicyMixin._popPolicyDataIfNeeded</c>.</summary>
    private bool PopPolicyDataIfNeeded(
        TraversalDirection direction,
        FocusScopeNode nearestScope,
        FocusNode focusedChild)
    {
        _policyData.TryGetValue(nearestScope, out DirectionalPolicyData? policyData);
        if (policyData != null && policyData.History.Count > 0 && policyData.History[0].Direction != direction)
        {
            if (policyData.History[^1].Node.Parent == null)
            {
                InvalidateScopeData(nearestScope);
                return false;
            }

            bool PopOrInvalidate(TraversalDirection popDirection)
            {
                FocusNode lastNode = policyData.History[^1].Node;
                policyData.History.RemoveAt(policyData.History.Count - 1);
                Scrollable.ScrollableState? lastScrollable = lastNode.Context is { } lastContext
                    ? Scrollable.MaybeOf(lastContext)
                    : null;
                Scrollable.ScrollableState? currentScrollable =
                    FocusManager.Instance.PrimaryFocus?.Context is { } currentContext
                        ? Scrollable.MaybeOf(currentContext)
                        : null;
                if (!ReferenceEquals(lastScrollable, currentScrollable))
                {
                    InvalidateScopeData(nearestScope);
                    return false;
                }

                ScrollPositionAlignmentPolicy alignmentPolicy =
                    popDirection is TraversalDirection.Up or TraversalDirection.Left
                        ? ScrollPositionAlignmentPolicy.KeepVisibleAtStart
                        : ScrollPositionAlignmentPolicy.KeepVisibleAtEnd;
                RequestFocusCallback(lastNode, alignmentPolicy);
                return true;
            }

            switch (direction)
            {
                case TraversalDirection.Down:
                case TraversalDirection.Up:
                    switch (policyData.History[0].Direction)
                    {
                        case TraversalDirection.Left:
                        case TraversalDirection.Right:
                            InvalidateScopeData(nearestScope);
                            break;
                        case TraversalDirection.Up:
                        case TraversalDirection.Down:
                            if (PopOrInvalidate(direction))
                            {
                                return true;
                            }

                            break;
                    }

                    break;
                case TraversalDirection.Left:
                case TraversalDirection.Right:
                    switch (policyData.History[0].Direction)
                    {
                        case TraversalDirection.Left:
                        case TraversalDirection.Right:
                            if (PopOrInvalidate(direction))
                            {
                                return true;
                            }

                            break;
                        case TraversalDirection.Up:
                        case TraversalDirection.Down:
                            InvalidateScopeData(nearestScope);
                            break;
                    }

                    break;
            }
        }

        if (policyData != null && policyData.History.Count == 0)
        {
            InvalidateScopeData(nearestScope);
        }

        return false;
    }

    /// <summary>Dart parity source: <c>DirectionalFocusTraversalPolicyMixin._pushPolicyData</c>.</summary>
    private void PushPolicyData(
        TraversalDirection direction,
        FocusScopeNode nearestScope,
        FocusNode focusedChild)
    {
        var newEntry = new DirectionalPolicyDataEntry(direction, focusedChild);
        if (_policyData.TryGetValue(nearestScope, out DirectionalPolicyData? policyData))
        {
            policyData.History.Add(newEntry);
            return;
        }

        _policyData[nearestScope] = new DirectionalPolicyData([newEntry]);
    }

    /// <summary>
    /// Dart parity source: <c>DirectionalFocusTraversalPolicyMixin._requestTraversalFocusInDirection</c>.
    /// </summary>
    private bool RequestTraversalFocusInDirection(
        FocusNode currentNode,
        FocusNode node,
        FocusScopeNode nearestScope,
        TraversalDirection direction)
    {
        if (node is FocusScopeNode scopeNode)
        {
            if (scopeNode.FocusedChild is { } focusedChild)
            {
                return RequestTraversalFocusInDirection(currentNode, focusedChild, scopeNode, direction);
            }

            FocusNode firstNode = FindFirstFocusInDirection(scopeNode, direction) ?? currentNode;
            RequestFocusCallback(firstNode, AlignmentPolicyFor(direction));
            return true;
        }

        bool nodeHadPrimaryFocus = node.HasPrimaryFocus;
        RequestFocusCallback(node, AlignmentPolicyFor(direction));
        return !nodeHadPrimaryFocus;
    }

    private static ScrollPositionAlignmentPolicy AlignmentPolicyFor(TraversalDirection direction) =>
        direction is TraversalDirection.Up or TraversalDirection.Left
            ? ScrollPositionAlignmentPolicy.KeepVisibleAtStart
            : ScrollPositionAlignmentPolicy.KeepVisibleAtEnd;

    /// <summary>Dart parity source: <c>DirectionalFocusTraversalPolicyMixin._onEdgeForDirection</c>.</summary>
    private bool OnEdgeForDirection(
        FocusNode currentNode,
        FocusNode focusedChild,
        TraversalDirection direction,
        FocusScopeNode? scope = null)
    {
        FocusScopeNode nearestScope = scope ?? currentNode.NearestScope ?? FocusManager.Instance.RootScope;
        FocusNode? found;
        switch (nearestScope.DirectionalTraversalEdgeBehavior)
        {
            case TraversalEdgeBehavior.LeaveFlutterView:
                focusedChild.Unfocus();
                return false;
            case TraversalEdgeBehavior.ParentScope:
                FocusScopeNode? parentScope = nearestScope.EnclosingScope;
                if (parentScope != null && !ReferenceEquals(parentScope, FocusManager.Instance.RootScope))
                {
                    InvalidateScopeData(nearestScope);
                    nearestScope = parentScope;
                    InvalidateScopeData(nearestScope);
                    found = FindNextFocusInDirection(focusedChild, nearestScope.TraversalDescendants, direction);
                    if (found == null)
                    {
                        return OnEdgeForDirection(currentNode, focusedChild, direction, nearestScope);
                    }
                }
                else
                {
                    found = FindNextFocusInDirection(
                        focusedChild, nearestScope.TraversalDescendants, direction, forward: false);
                }

                break;
            case TraversalEdgeBehavior.ClosedLoop:
                found = FindNextFocusInDirection(
                    focusedChild, nearestScope.TraversalDescendants, direction, forward: false);
                break;
            case TraversalEdgeBehavior.Stop:
            default:
                return false;
        }

        return found != null && RequestTraversalFocusInDirection(currentNode, found, nearestScope, direction);
    }

    public override bool InDirection(FocusNode currentNode, TraversalDirection direction)
    {
        FocusScopeNode nearestScope = currentNode.NearestScope ?? FocusManager.Instance.RootScope;
        FocusNode? focusedChild = nearestScope.FocusedChild;
        if (focusedChild == null)
        {
            FocusNode firstFocus = FindFirstFocusInDirection(currentNode, direction) ?? currentNode;
            RequestFocusCallback(firstFocus, AlignmentPolicyFor(direction));
            return true;
        }

        if (PopPolicyDataIfNeeded(direction, nearestScope, focusedChild))
        {
            return true;
        }

        FocusNode? found = FindNextFocusInDirection(
            focusedChild, nearestScope.TraversalDescendants, direction);
        if (found != null)
        {
            PushPolicyData(direction, nearestScope, focusedChild);
            return RequestTraversalFocusInDirection(currentNode, found, nearestScope, direction);
        }

        return OnEdgeForDirection(currentNode, focusedChild, direction);
    }
}

/// <summary>Dart parity source: <c>WidgetOrderTraversalPolicy</c>.</summary>
public sealed class WidgetOrderTraversalPolicy : DirectionalFocusTraversalPolicy
{
    public WidgetOrderTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
        : base(requestFocusCallback)
    {
    }

    public override IEnumerable<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode) => descendants;
}

/// <summary>Dart parity source: <c>_ReadingOrderSortData</c>.</summary>
internal sealed class ReadingOrderSortData
{
    private List<Directionality>? _directionalAncestors;

    internal ReadingOrderSortData(FocusNode node)
    {
        Node = node;
        Rect = node.Rect;
        Directionality = node.Context is { } context ? FindDirectionality(context) : null;
    }

    internal FocusNode Node { get; }

    internal Rect Rect { get; }

    internal TextDirection? Directionality { get; }

    private static TextDirection? FindDirectionality(BuildContext context) =>
        context.GetInherited<Directionality>()?.TextDirection;

    internal static TextDirection? CommonDirectionalityOf(List<ReadingOrderSortData> list)
    {
        HashSet<Directionality>? common = null;
        foreach (ReadingOrderSortData member in list)
        {
            var ancestorSet = new HashSet<Directionality>(
                member.DirectionalAncestors,
                ReferenceEqualityComparer.Instance as IEqualityComparer<Directionality>);
            if (common == null)
            {
                common = ancestorSet;
                continue;
            }

            common.IntersectWith(ancestorSet);
        }

        if (common == null || common.Count == 0)
        {
            return list[0].Directionality;
        }

        foreach (Directionality ancestor in list[0].DirectionalAncestors)
        {
            if (common.Contains(ancestor))
            {
                return ancestor.TextDirection;
            }
        }

        return list[0].Directionality;
    }

    internal static void SortWithDirectionality(List<ReadingOrderSortData> list, TextDirection directionality)
    {
        List<ReadingOrderSortData> sorted = FocusTraversalSorting.StableSortPublic(
            list,
            (a, b) => directionality == TextDirection.Ltr
                ? a.Rect.Left.CompareTo(b.Rect.Left)
                : b.Rect.Right.CompareTo(a.Rect.Right));
        list.Clear();
        list.AddRange(sorted);
    }

    /// <summary>Dart parity source: <c>_ReadingOrderSortData.directionalAncestors</c>.</summary>
    internal IReadOnlyList<Directionality> DirectionalAncestors
    {
        get
        {
            if (_directionalAncestors != null)
            {
                return _directionalAncestors;
            }

            var result = new List<Directionality>();
            if (Node.Context is { } context)
            {
                InheritedElement? directionalityElement =
                    context.GetElementForInheritedWidgetOfExactType<Directionality>();
                while (directionalityElement != null)
                {
                    result.Add((Directionality)directionalityElement.Widget);
                    Element? parent = directionalityElement.Parent;
                    directionalityElement = parent == null
                        ? null
                        : new BuildContext(parent).GetElementForInheritedWidgetOfExactType<Directionality>();
                }
            }

            _directionalAncestors = result;
            return _directionalAncestors;
        }
    }
}

/// <summary>Dart parity source: <c>_ReadingOrderDirectionalGroupData</c>.</summary>
internal sealed class ReadingOrderDirectionalGroupData
{
    private Rect? _rect;

    internal ReadingOrderDirectionalGroupData(List<ReadingOrderSortData> members)
    {
        Members = members;
    }

    internal List<ReadingOrderSortData> Members { get; }

    internal TextDirection? Directionality => Members[0].Directionality;

    internal Rect Rect
    {
        get
        {
            if (_rect == null)
            {
                foreach (ReadingOrderSortData member in Members)
                {
                    _rect = _rect == null ? member.Rect : _rect.Value.Union(member.Rect);
                }
            }

            return _rect ?? default;
        }
    }

    internal static void SortWithDirectionality(
        List<ReadingOrderDirectionalGroupData> list,
        TextDirection directionality)
    {
        List<ReadingOrderDirectionalGroupData> sorted = FocusTraversalSorting.StableSortPublic(
            list,
            (a, b) => directionality == TextDirection.Ltr
                ? a.Rect.Left.CompareTo(b.Rect.Left)
                : b.Rect.Right.CompareTo(a.Rect.Right));
        list.Clear();
        list.AddRange(sorted);
    }
}

/// <summary>The stable sort Dart's <c>mergeSort</c> provides, shared by the reading-order helpers.</summary>
internal static class FocusTraversalSorting
{
    internal static List<T> StableSortPublic<T>(IEnumerable<T> source, Comparison<T> compare) =>
        source.OrderBy(static item => item, Comparer<T>.Create(compare)).ToList();
}

/// <summary>Dart parity source: <c>ReadingOrderTraversalPolicy</c>.</summary>
public class ReadingOrderTraversalPolicy : DirectionalFocusTraversalPolicy
{
    public ReadingOrderTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
        : base(requestFocusCallback)
    {
    }

    /// <summary>Dart parity source: <c>ReadingOrderTraversalPolicy.sort</c>.</summary>
    public static IEnumerable<FocusNode> Sort(IEnumerable<FocusNode> nodes)
    {
        List<FocusNode> nodeList = nodes.ToList();
        if (nodeList.Count <= 1)
        {
            return nodeList;
        }

        var unplaced = nodeList.Select(static node => new ReadingOrderSortData(node)).ToList();
        var sortedList = new List<FocusNode>();

        ReadingOrderSortData current = PickNext(unplaced);
        sortedList.Add(current.Node);
        unplaced.Remove(current);

        while (unplaced.Count > 0)
        {
            current = PickNext(unplaced);
            sortedList.Add(current.Node);
            unplaced.Remove(current);
        }

        return sortedList;
    }

    /// <summary>Dart parity source: <c>ReadingOrderTraversalPolicy._collectDirectionalityGroups</c>.</summary>
    private static List<ReadingOrderDirectionalGroupData> CollectDirectionalityGroups(
        IReadOnlyList<ReadingOrderSortData> candidates)
    {
        TextDirection? currentDirection = candidates[0].Directionality;
        var currentGroup = new List<ReadingOrderSortData>();
        var result = new List<ReadingOrderDirectionalGroupData>();
        foreach (ReadingOrderSortData candidate in candidates)
        {
            if (candidate.Directionality == currentDirection)
            {
                currentGroup.Add(candidate);
                continue;
            }

            currentDirection = candidate.Directionality;
            result.Add(new ReadingOrderDirectionalGroupData(currentGroup));
            currentGroup = [candidate];
        }

        if (currentGroup.Count > 0)
        {
            result.Add(new ReadingOrderDirectionalGroupData(currentGroup));
        }

        foreach (ReadingOrderDirectionalGroupData bandGroup in result)
        {
            if (bandGroup.Members.Count == 1)
            {
                continue;
            }

            ReadingOrderSortData.SortWithDirectionality(
                bandGroup.Members, bandGroup.Directionality ?? TextDirection.Ltr);
        }

        return result;
    }

    /// <summary>Dart parity source: <c>ReadingOrderTraversalPolicy._pickNext</c>.</summary>
    private static ReadingOrderSortData PickNext(List<ReadingOrderSortData> candidates)
    {
        List<ReadingOrderSortData> sorted = FocusTraversalSorting.StableSortPublic(
            candidates, static (a, b) => a.Rect.Top.CompareTo(b.Rect.Top));
        candidates.Clear();
        candidates.AddRange(sorted);
        ReadingOrderSortData topmost = candidates[0];

        List<ReadingOrderSortData> inBandOfTop = candidates
            .Where(item => Math.Max(item.Rect.Top, topmost.Rect.Top)
                           < Math.Min(item.Rect.Bottom, topmost.Rect.Bottom)
                           && item.Rect.Left < item.Rect.Right)
            .ToList();
        if (inBandOfTop.Count <= 1)
        {
            return topmost;
        }

        TextDirection nearestCommonDirectionality =
            ReadingOrderSortData.CommonDirectionalityOf(inBandOfTop) ?? TextDirection.Ltr;
        ReadingOrderSortData.SortWithDirectionality(inBandOfTop, nearestCommonDirectionality);

        List<ReadingOrderDirectionalGroupData> bandGroups = CollectDirectionalityGroups(inBandOfTop);
        if (bandGroups.Count == 1)
        {
            return bandGroups[0].Members[0];
        }

        ReadingOrderDirectionalGroupData.SortWithDirectionality(bandGroups, nearestCommonDirectionality);
        return bandGroups[0].Members[0];
    }

    public override IEnumerable<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode) => Sort(descendants);
}

/// <summary>Dart parity source: <c>FocusOrder</c>.</summary>
public abstract class FocusOrder : IComparable<FocusOrder>
{
    public int CompareTo(FocusOrder? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (GetType() != other.GetType())
        {
            throw new InvalidOperationException(
                $"The sorting algorithm must not compare incomparable keys. Comparing {this} with {other}.");
        }

        return DoCompare(other);
    }

    protected abstract int DoCompare(FocusOrder other);
}

/// <summary>Dart parity source: <c>NumericFocusOrder</c>.</summary>
public sealed class NumericFocusOrder : FocusOrder
{
    public NumericFocusOrder(double order)
    {
        Order = order;
    }

    public double Order { get; }

    protected override int DoCompare(FocusOrder other) => Order.CompareTo(((NumericFocusOrder)other).Order);
}

/// <summary>Dart parity source: <c>LexicalFocusOrder</c>.</summary>
public sealed class LexicalFocusOrder : FocusOrder
{
    public LexicalFocusOrder(string order)
    {
        Order = order;
    }

    public string Order { get; }

    protected override int DoCompare(FocusOrder other) =>
        string.CompareOrdinal(Order, ((LexicalFocusOrder)other).Order);
}

/// <summary>Dart parity source: <c>_OrderedFocusInfo</c>.</summary>
internal readonly record struct OrderedFocusInfo(FocusNode Node, FocusOrder Order);

/// <summary>Dart parity source: <c>OrderedTraversalPolicy</c>.</summary>
public sealed class OrderedTraversalPolicy : DirectionalFocusTraversalPolicy
{
    public OrderedTraversalPolicy(
        FocusTraversalPolicy? secondary = null,
        TraversalRequestFocusCallback? requestFocusCallback = null) : base(requestFocusCallback)
    {
        Secondary = secondary;
    }

    public FocusTraversalPolicy? Secondary { get; }

    public override IEnumerable<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode)
    {
        FocusTraversalPolicy secondaryPolicy = Secondary ?? new ReadingOrderTraversalPolicy();
        IEnumerable<FocusNode> sortedDescendants = secondaryPolicy.SortDescendants(descendants, currentNode);
        var unordered = new List<FocusNode>();
        var ordered = new List<OrderedFocusInfo>();
        foreach (FocusNode node in sortedDescendants)
        {
            FocusOrder? order = node.Context is { } context ? FocusTraversalOrder.MaybeOf(context) : null;
            if (order != null)
            {
                ordered.Add(new OrderedFocusInfo(node, order));
            }
            else
            {
                unordered.Add(node);
            }
        }

        List<OrderedFocusInfo> sortedOrdered = FocusTraversalSorting.StableSortPublic(
            ordered, static (a, b) => a.Order.CompareTo(b.Order));
        return sortedOrdered.Select(static info => info.Node).Concat(unordered);
    }
}

/// <summary>Dart parity source: <c>FocusTraversalOrder</c>.</summary>
public sealed class FocusTraversalOrder : InheritedWidget
{
    public FocusTraversalOrder(FocusOrder order, Widget child, Key? key = null) : base(key)
    {
        Order = order;
        Child = child;
    }

    public FocusOrder Order { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;

    public static FocusOrder Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "FocusTraversalOrder.Of() was called with a context that does not contain a "
                   + "FocusTraversalOrder widget.");
    }

    public static FocusOrder? MaybeOf(BuildContext context) =>
        context.GetInherited<FocusTraversalOrder>()?.Order;
}

/// <summary>Dart parity source: <c>_FocusTraversalGroupNode</c>.</summary>
internal sealed class FocusTraversalGroupNode : FocusNode
{
    internal FocusTraversalGroupNode(FocusTraversalPolicy policy, string? debugLabel = null)
        : base(debugLabel: debugLabel)
    {
        Policy = policy;
    }

    internal FocusTraversalPolicy Policy { get; set; }
}

/// <summary>Dart parity source: <c>FocusTraversalGroup</c>.</summary>
public sealed class FocusTraversalGroup : StatefulWidget
{
    /// <summary>
    /// C#-only fallback: Flutter's <c>FocusTraversalGroup.of</c> asserts that an app-level group exists,
    /// while Plumix's unit tests and hosts routinely build trees without one.
    /// </summary>
    private static readonly ReadingOrderTraversalPolicy DefaultPolicyInstance = new();

    public FocusTraversalGroup(
        Widget child,
        FocusTraversalPolicy? policy = null,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true,
        Action<FocusNode>? onFocusNodeCreated = null,
        FocusNode? parentNode = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Policy = policy ?? new ReadingOrderTraversalPolicy();
        DescendantsAreFocusable = descendantsAreFocusable;
        DescendantsAreTraversable = descendantsAreTraversable;
        OnFocusNodeCreated = onFocusNodeCreated;
        ParentNode = parentNode;
    }

    public Widget Child { get; }

    public FocusTraversalPolicy Policy { get; }

    public bool DescendantsAreFocusable { get; }

    public bool DescendantsAreTraversable { get; }

    public Action<FocusNode>? OnFocusNodeCreated { get; }

    public FocusNode? ParentNode { get; }

    /// <summary>Dart parity source: <c>FocusTraversalGroup.maybeOfNode</c>.</summary>
    public static FocusTraversalPolicy? MaybeOfNode(FocusNode node) => GetGroupNode(node)?.Policy;

    /// <summary>Dart parity source: <c>FocusTraversalGroup._getGroupNode</c>.</summary>
    internal static FocusTraversalGroupNode? GetGroupNode(FocusNode node)
    {
        while (node.Parent != null)
        {
            if (node.Context == null)
            {
                return null;
            }

            if (node is FocusTraversalGroupNode groupNode)
            {
                return groupNode;
            }

            node = node.Parent!;
        }

        return null;
    }

    /// <summary>Dart parity source: <c>FocusTraversalGroup.of</c>.</summary>
    public static FocusTraversalPolicy Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "Unable to find a Focus or FocusScope widget in the given context, or the FocusNode "
                   + "from the widget that was found is not associated with a FocusTraversalPolicy.");
    }

    /// <summary>Dart parity source: <c>FocusTraversalGroup.maybeOf</c>.</summary>
    public static FocusTraversalPolicy? MaybeOf(BuildContext context)
    {
        FocusNode? node = Focus.MaybeOf(context, scopeOk: true, createDependency: false);
        return node == null ? null : MaybeOfNode(node);
    }

    /// <summary>The policy that governs traversal from <paramref name="node"/>.</summary>
    internal static FocusTraversalPolicy PolicyForNode(FocusNode node)
    {
        return MaybeOfNode(node)
               ?? (node.Context is { } context ? MaybeOf(context) : null)
               ?? DefaultPolicyInstance;
    }

    public override State CreateState() => new FocusTraversalGroupState();

    private sealed class FocusTraversalGroupState : State
    {
        private FocusTraversalGroupNode? _focusNode;

        private FocusTraversalGroup CurrentWidget => (FocusTraversalGroup)Element.Widget;

        public override void InitState()
        {
            _focusNode = new FocusTraversalGroupNode(CurrentWidget.Policy, debugLabel: "FocusTraversalGroup");
            CurrentWidget.OnFocusNodeCreated?.Invoke(_focusNode);
        }

        public override void Dispose()
        {
            _focusNode?.Dispose();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (FocusTraversalGroup)oldWidget;
            if (!ReferenceEquals(old.Policy, CurrentWidget.Policy))
            {
                _focusNode!.Policy = CurrentWidget.Policy;
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Focus(
                child: CurrentWidget.Child,
                focusNode: _focusNode,
                parentNode: CurrentWidget.ParentNode,
                canRequestFocus: false,
                skipTraversal: true,
                includeSemantics: false,
                descendantsAreFocusable: CurrentWidget.DescendantsAreFocusable,
                descendantsAreTraversable: CurrentWidget.DescendantsAreTraversable);
        }
    }
}

/// <summary>Dart parity source: <c>RequestFocusIntent</c>.</summary>
public sealed class RequestFocusIntent : Intent
{
    public RequestFocusIntent(FocusNode focusNode, TraversalRequestFocusCallback? requestFocusCallback = null)
    {
        FocusNode = focusNode;
        RequestFocusCallback =
            requestFocusCallback ?? FocusTraversalPolicy.DefaultTraversalRequestFocusCallback;
    }

    public FocusNode FocusNode { get; }

    public TraversalRequestFocusCallback RequestFocusCallback { get; }
}

/// <summary>Dart parity source: <c>RequestFocusAction</c>.</summary>
public sealed class RequestFocusAction : FlutterAction<RequestFocusIntent>
{
    public override object? Invoke(RequestFocusIntent intent)
    {
        intent.RequestFocusCallback(intent.FocusNode);
        return null;
    }
}

/// <summary>Dart parity source: <c>NextFocusIntent</c>.</summary>
public sealed class NextFocusIntent : Intent
{
}

/// <summary>Dart parity source: <c>NextFocusAction</c>.</summary>
public sealed class NextFocusAction : FlutterAction<NextFocusIntent>
{
    public override object? Invoke(NextFocusIntent intent)
    {
        return FocusManager.Instance.PrimaryFocus?.NextFocus() ?? false;
    }

    public override KeyEventResult ToKeyEventResult(NextFocusIntent intent, object? invokeResult)
    {
        return invokeResult is true ? KeyEventResult.Handled : KeyEventResult.SkipRemainingHandlers;
    }
}

/// <summary>Dart parity source: <c>PreviousFocusIntent</c>.</summary>
public sealed class PreviousFocusIntent : Intent
{
}

/// <summary>Dart parity source: <c>PreviousFocusAction</c>.</summary>
public sealed class PreviousFocusAction : FlutterAction<PreviousFocusIntent>
{
    public override object? Invoke(PreviousFocusIntent intent)
    {
        return FocusManager.Instance.PrimaryFocus?.PreviousFocus() ?? false;
    }

    public override KeyEventResult ToKeyEventResult(PreviousFocusIntent intent, object? invokeResult)
    {
        return invokeResult is true ? KeyEventResult.Handled : KeyEventResult.SkipRemainingHandlers;
    }
}

/// <summary>Dart parity source: <c>DirectionalFocusIntent</c>.</summary>
public sealed class DirectionalFocusIntent : Intent
{
    public DirectionalFocusIntent(TraversalDirection direction, bool ignoreTextFields = true)
    {
        Direction = direction;
        IgnoreTextFields = ignoreTextFields;
    }

    public TraversalDirection Direction { get; }

    public bool IgnoreTextFields { get; }
}

/// <summary>Dart parity source: <c>DirectionalFocusAction</c>.</summary>
public sealed class DirectionalFocusAction : FlutterAction<DirectionalFocusIntent>
{
    private readonly bool _isForTextField;

    public DirectionalFocusAction()
    {
        _isForTextField = false;
    }

    private DirectionalFocusAction(bool isForTextField)
    {
        _isForTextField = isForTextField;
    }

    /// <summary>Dart parity source: <c>DirectionalFocusAction.forTextField</c>.</summary>
    public static DirectionalFocusAction ForTextField() => new(isForTextField: true);

    public override object? Invoke(DirectionalFocusIntent intent)
    {
        if (!intent.IgnoreTextFields || !_isForTextField)
        {
            FocusManager.Instance.PrimaryFocus?.FocusInDirection(intent.Direction);
        }

        return null;
    }
}

/// <summary>Dart parity source: <c>ExcludeFocusTraversal</c>.</summary>
public sealed class ExcludeFocusTraversal : StatelessWidget
{
    public ExcludeFocusTraversal(
        Widget child,
        bool excluding = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Excluding = excluding;
    }

    public Widget Child { get; }

    public bool Excluding { get; }

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            child: Child,
            canRequestFocus: false,
            skipTraversal: true,
            includeSemantics: false,
            descendantsAreTraversable: !Excluding);
    }
}
