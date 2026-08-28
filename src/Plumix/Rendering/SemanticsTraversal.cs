using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// The default accessibility traversal sort: the geometry-driven grouping Flutter applies to a
/// node's children before the sort keys are honored.
/// </summary>
/// <remarks>
/// Flutter's private <c>_childrenInDefaultOrder</c>, <c>_SemanticsSortGroup</c>, <c>_BoxEdge</c> and
/// <c>_TraversalSortNode</c>, plus the sort-key grouping in
/// <c>SemanticsNode._childrenInTraversalOrder</c>.
/// </remarks>
internal static class SemanticsTraversal
{
    /// <summary>Sorts <paramref name="node"/>'s children into traversal order.</summary>
    public static IReadOnlyList<SemanticsNode> Sort(
        SemanticsNode node,
        IReadOnlyList<SemanticsNode> children)
    {
        if (children.Count < 2)
        {
            return children;
        }

        TextDirection? inheritedTextDirection = node.TextDirection;
        SemanticsNode? ancestor = node.Parent;
        while (inheritedTextDirection is null && ancestor is not null)
        {
            inheritedTextDirection = ancestor.TextDirection;
            ancestor = ancestor.Parent;
        }

        // In the absence of a text direction Flutter defaults to paint order.
        List<SemanticsNode> childrenInDefaultOrder = inheritedTextDirection is { } textDirection
            ? ChildrenInDefaultOrder(children, textDirection)
            : [.. children];

        // List sorting is not guaranteed to be stable, so children are first partitioned into groups
        // that have comparable sort keys. Those groups stay in place; only their contents are sorted.
        var everythingSorted = new List<TraversalSortNode>(childrenInDefaultOrder.Count);
        var sortNodes = new List<TraversalSortNode>();
        SemanticsSortKey? lastSortKey = null;
        for (int position = 0; position < childrenInDefaultOrder.Count; position++)
        {
            SemanticsNode child = childrenInDefaultOrder[position];
            SemanticsSortKey? sortKey = child.SortKey;
            lastSortKey = position > 0 ? childrenInDefaultOrder[position - 1].SortKey : null;
            bool isCompatibleWithPreviousSortKey =
                position == 0
                || (sortKey?.GetType() == lastSortKey?.GetType()
                    && (sortKey is null || sortKey.Name == lastSortKey!.Name));
            if (!isCompatibleWithPreviousSortKey && sortNodes.Count > 0)
            {
                // Groups with null sort keys are left alone: sorting them would not be stable.
                if (lastSortKey is not null)
                {
                    sortNodes.Sort(TraversalSortNode.Comparer);
                }

                everythingSorted.AddRange(sortNodes);
                sortNodes.Clear();
            }

            sortNodes.Add(new TraversalSortNode(child, sortKey, position));
        }

        if (lastSortKey is not null)
        {
            sortNodes.Sort(TraversalSortNode.Comparer);
        }

        everythingSorted.AddRange(sortNodes);
        return everythingSorted.ConvertAll(static sortNode => sortNode.Node);
    }

    /// <summary>
    /// Breaks the children into groups that do not overlap vertically, orders those groups top to
    /// bottom, then sorts within each group.
    /// </summary>
    private static List<SemanticsNode> ChildrenInDefaultOrder(
        IReadOnlyList<SemanticsNode> children,
        TextDirection textDirection)
    {
        var edges = new List<BoxEdge>(children.Count * 2);
        foreach (SemanticsNode child in children)
        {
            // A small delta shrinks the child rects, which removes the merely-touching cases.
            Rect childRect = Deflate(child.Rect, 0.1);
            edges.Add(new BoxEdge(
                IsLeadingEdge: true,
                Offset: PointInParentCoordinates(child, childRect.TopLeft).Y,
                Node: child));
            edges.Add(new BoxEdge(
                IsLeadingEdge: false,
                Offset: PointInParentCoordinates(child, childRect.BottomRight).Y,
                Node: child));
        }

        edges.Sort(BoxEdge.Comparer);

        List<SemanticsSortGroup> verticalGroups = GroupByDepth(edges, textDirection);
        verticalGroups.Sort(SemanticsSortGroup.Comparer);

        var result = new List<SemanticsNode>(children.Count);
        foreach (SemanticsSortGroup group in verticalGroups)
        {
            result.AddRange(SortedWithinVerticalGroup(group));
        }

        return result;
    }

    /// <summary>
    /// Breaks a vertical group into horizontal groups and sorts each of them with the knot sort.
    /// </summary>
    private static List<SemanticsNode> SortedWithinVerticalGroup(SemanticsSortGroup group)
    {
        var edges = new List<BoxEdge>(group.Nodes.Count * 2);
        foreach (SemanticsNode child in group.Nodes)
        {
            Rect childRect = Deflate(child.Rect, 0.1);
            edges.Add(new BoxEdge(
                IsLeadingEdge: true,
                Offset: PointInParentCoordinates(child, childRect.TopLeft).X,
                Node: child));
            edges.Add(new BoxEdge(
                IsLeadingEdge: false,
                Offset: PointInParentCoordinates(child, childRect.BottomRight).X,
                Node: child));
        }

        edges.Sort(BoxEdge.Comparer);

        List<SemanticsSortGroup> horizontalGroups = GroupByDepth(edges, group.TextDirection);
        horizontalGroups.Sort(SemanticsSortGroup.Comparer);
        if (group.TextDirection == TextDirection.Rtl)
        {
            horizontalGroups.Reverse();
        }

        var result = new List<SemanticsNode>(group.Nodes.Count);
        foreach (SemanticsSortGroup horizontalGroup in horizontalGroups)
        {
            result.AddRange(SortedWithinKnot(horizontalGroup));
        }

        return result;
    }

    /// <summary>
    /// Sorts nodes that intersect both vertically and horizontally by topologically sorting the
    /// "traversed before" relation between every pair of node centers.
    /// </summary>
    private static List<SemanticsNode> SortedWithinKnot(SemanticsSortGroup group)
    {
        List<SemanticsNode> nodes = group.Nodes;
        if (nodes.Count <= 1)
        {
            return nodes;
        }

        var nodeMap = new Dictionary<int, SemanticsNode>(nodes.Count);
        var edges = new Dictionary<int, int>(nodes.Count);
        foreach (SemanticsNode node in nodes)
        {
            nodeMap[node.Id] = node;
            Point center = PointInParentCoordinates(node, Center(node.Rect));
            foreach (SemanticsNode nextNode in nodes)
            {
                if (ReferenceEquals(node, nextNode)
                    || (edges.TryGetValue(nextNode.Id, out int target) && target == node.Id))
                {
                    // Skip self, and the pairs whose reverse relation is already established.
                    continue;
                }

                Point nextCenter = PointInParentCoordinates(nextNode, Center(nextNode.Rect));
                double deltaX = nextCenter.X - center.X;
                double deltaY = nextCenter.Y - center.Y;
                // Coincident centers report a direction of 0.0, exactly like Dart's `Offset.direction`.
                double direction = deltaX == 0 && deltaY == 0 ? 0.0 : Math.Atan2(deltaY, deltaX);
                bool isLtrAndForward = group.TextDirection == TextDirection.Ltr
                                       && -Math.PI / 4 < direction
                                       && direction < 3 * Math.PI / 4;
                bool isRtlAndForward = group.TextDirection == TextDirection.Rtl
                                       && (direction < -3 * Math.PI / 4 || direction > 3 * Math.PI / 4);
                if (isLtrAndForward || isRtlAndForward)
                {
                    edges[node.Id] = nextNode.Id;
                }
            }
        }

        var sortedIds = new List<int>(nodes.Count);
        var visitedIds = new HashSet<int>();
        var startNodes = new List<SemanticsNode>(nodes);
        startNodes.Sort(static (a, b) =>
        {
            Point aTopLeft = PointInParentCoordinates(a, a.Rect.TopLeft);
            Point bTopLeft = PointInParentCoordinates(b, b.Rect.TopLeft);
            int verticalDiff = aTopLeft.Y.CompareTo(bTopLeft.Y);
            return verticalDiff != 0 ? -verticalDiff : -aTopLeft.X.CompareTo(bTopLeft.X);
        });

        void Search(int id)
        {
            if (!visitedIds.Add(id))
            {
                return;
            }

            if (edges.TryGetValue(id, out int next))
            {
                Search(next);
            }

            sortedIds.Add(id);
        }

        foreach (SemanticsNode node in startNodes)
        {
            Search(node.Id);
        }

        sortedIds.Reverse();
        return sortedIds.ConvertAll(id => nodeMap[id]);
    }

    /// <summary>
    /// Walks the sorted edges and closes a group whenever the nesting depth returns to zero, so no
    /// two groups overlap along the traversal axis.
    /// </summary>
    private static List<SemanticsSortGroup> GroupByDepth(List<BoxEdge> edges, TextDirection textDirection)
    {
        var groups = new List<SemanticsSortGroup>();
        SemanticsSortGroup? group = null;
        int depth = 0;
        foreach (BoxEdge edge in edges)
        {
            if (edge.IsLeadingEdge)
            {
                depth += 1;
                group ??= new SemanticsSortGroup(edge.Offset, textDirection);
                group.Nodes.Add(edge.Node);
            }
            else
            {
                depth -= 1;
            }

            if (depth == 0 && group is not null)
            {
                groups.Add(group);
                group = null;
            }
        }

        return groups;
    }

    /// <summary>Converts <paramref name="point"/> into the node's parent's coordinate system.</summary>
    private static Point PointInParentCoordinates(SemanticsNode node, Point point)
    {
        // The traversal transform, so a grafted child sorts in its traversal parent's coordinates.
        return node.TraversalTransform is { } transform
            ? MatrixUtils.TransformPoint(transform, point)
            : point;
    }

    private static Point Center(Rect rect) => new(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);

    private static Rect Deflate(Rect rect, double delta)
    {
        return new Rect(
            rect.X + delta,
            rect.Y + delta,
            Math.Max(0.0, rect.Width - 2 * delta),
            Math.Max(0.0, rect.Height - 2 * delta));
    }

    /// <summary>One edge of a node's box along the traversal axis.</summary>
    /// <remarks>Flutter's private <c>_BoxEdge</c>.</remarks>
    private readonly record struct BoxEdge(bool IsLeadingEdge, double Offset, SemanticsNode Node)
    {
        public static IComparer<BoxEdge> Comparer { get; } =
            Comparer<BoxEdge>.Create(static (a, b) => a.Offset.CompareTo(b.Offset));
    }

    /// <summary>
    /// A group of nodes that is disjoint, along one axis, from every other group under the same parent.
    /// </summary>
    /// <remarks>Flutter's private <c>_SemanticsSortGroup</c>.</remarks>
    private sealed class SemanticsSortGroup(double startOffset, TextDirection textDirection)
    {
        public double StartOffset { get; } = startOffset;

        public TextDirection TextDirection { get; } = textDirection;

        public List<SemanticsNode> Nodes { get; } = [];

        public static IComparer<SemanticsSortGroup> Comparer { get; } =
            Comparer<SemanticsSortGroup>.Create(static (a, b) => a.StartOffset.CompareTo(b.StartOffset));
    }

    /// <summary>
    /// Orders one node among its siblings: the sort key takes precedence over the default position.
    /// </summary>
    /// <remarks>Flutter's private <c>_TraversalSortNode</c>.</remarks>
    private readonly record struct TraversalSortNode(SemanticsNode Node, SemanticsSortKey? SortKey, int Position)
    {
        public static IComparer<TraversalSortNode> Comparer { get; } =
            Comparer<TraversalSortNode>.Create(static (a, b) =>
                a.SortKey is null || b.SortKey is null
                    ? a.Position - b.Position
                    : a.SortKey.CompareTo(b.SortKey));
    }
}
