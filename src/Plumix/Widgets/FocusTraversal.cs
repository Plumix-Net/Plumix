using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_traversal.dart
public delegate void TraversalRequestFocusCallback(FocusNode node);

public abstract class FocusTraversalPolicy
{
    protected FocusTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
    {
        RequestFocusCallback = requestFocusCallback ?? DefaultTraversalRequestFocusCallback;
    }

    public TraversalRequestFocusCallback RequestFocusCallback { get; }

    public static void DefaultTraversalRequestFocusCallback(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.RequestFocus();
    }

    public virtual IReadOnlyList<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(descendants);
        ArgumentNullException.ThrowIfNull(currentNode);
        return descendants.ToList();
    }

    public bool Next(FocusNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return (currentNode.Manager ?? FocusManager.Instance).FocusNext();
    }

    public bool Previous(FocusNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return (currentNode.Manager ?? FocusManager.Instance).FocusPrevious();
    }
}

public sealed class WidgetOrderTraversalPolicy : FocusTraversalPolicy
{
    public WidgetOrderTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
        : base(requestFocusCallback)
    {
    }
}

public class ReadingOrderTraversalPolicy : FocusTraversalPolicy
{
    public ReadingOrderTraversalPolicy(TraversalRequestFocusCallback? requestFocusCallback = null)
        : base(requestFocusCallback)
    {
    }

    public static IReadOnlyList<FocusNode> Sort(IEnumerable<FocusNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var remaining = nodes
            .Select((node, index) => ReadingOrderSortData.Create(node, index))
            .ToList();
        if (remaining.Count <= 1)
        {
            return remaining.Select(data => data.Node).ToList();
        }

        var result = new List<FocusNode>(remaining.Count);
        while (remaining.Count > 0)
        {
            ReadingOrderSortData next = PickNext(remaining);
            result.Add(next.Node);
            remaining.Remove(next);
        }

        return result;
    }

    public override IReadOnlyList<FocusNode> SortDescendants(
        IEnumerable<FocusNode> descendants,
        FocusNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return Sort(descendants);
    }

    private static ReadingOrderSortData PickNext(IReadOnlyList<ReadingOrderSortData> candidates)
    {
        var sortedByTop = candidates
            .OrderBy(candidate => candidate.Rect.Top)
            .ThenBy(candidate => candidate.SourceIndex)
            .ToList();
        ReadingOrderSortData topmost = sortedByTop[0];
        double bandTop = topmost.Rect.Top;
        double bandBottom = topmost.Rect.Bottom;
        var inBand = sortedByTop
            .Where(candidate => IntersectsHorizontalBand(candidate.Rect, bandTop, bandBottom))
            .ToList();
        if (inBand.Count <= 1)
        {
            return topmost;
        }

        TextDirection commonDirection = ResolveCommonDirection(inBand);
        List<ReadingOrderSortData> geometricallySorted = SortByDirection(
            inBand,
            commonDirection,
            data => data.Rect);
        var runs = new List<DirectionalRun>();
        foreach (ReadingOrderSortData data in geometricallySorted)
        {
            if (runs.Count == 0 || runs[^1].Direction != data.Direction)
            {
                runs.Add(new DirectionalRun(data.Direction));
            }

            runs[^1].Members.Add(data);
        }

        foreach (DirectionalRun run in runs)
        {
            run.Members = SortByDirection(run.Members, run.Direction, data => data.Rect);
            run.Bounds = ComputeBounds(run.Members.Select(data => data.Rect));
        }

        if (runs.Count == 1)
        {
            return runs[0].Members[0];
        }

        List<DirectionalRun> sortedRuns = SortByDirection(runs, commonDirection, run => run.Bounds);
        return sortedRuns[0].Members[0];
    }

    private static bool IntersectsHorizontalBand(Rect rect, double bandTop, double bandBottom)
    {
        return rect.Width > 0.0
               && rect.Height > 0.0
               && rect.Bottom > bandTop
               && rect.Top < bandBottom;
    }

    private static TextDirection ResolveCommonDirection(IReadOnlyList<ReadingOrderSortData> candidates)
    {
        foreach (DirectionalityAncestor ancestor in candidates[0].Ancestors)
        {
            bool shared = candidates
                .Skip(1)
                .All(candidate => candidate.Ancestors.Any(other => ReferenceEquals(other.Element, ancestor.Element)));
            if (shared)
            {
                return ancestor.Direction;
            }
        }

        return candidates[0].Direction;
    }

    private static List<T> SortByDirection<T>(
        IEnumerable<T> values,
        TextDirection direction,
        Func<T, Rect> rectSelector)
    {
        return direction == TextDirection.Ltr
            ? values.OrderBy(value => rectSelector(value).Left).ToList()
            : values.OrderByDescending(value => rectSelector(value).Right).ToList();
    }

    private static Rect ComputeBounds(IEnumerable<Rect> rectangles)
    {
        using IEnumerator<Rect> enumerator = rectangles.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return default;
        }

        Rect first = enumerator.Current;
        double left = first.Left;
        double top = first.Top;
        double right = first.Right;
        double bottom = first.Bottom;
        while (enumerator.MoveNext())
        {
            Rect rect = enumerator.Current;
            left = Math.Min(left, rect.Left);
            top = Math.Min(top, rect.Top);
            right = Math.Max(right, rect.Right);
            bottom = Math.Max(bottom, rect.Bottom);
        }

        return new Rect(left, top, right - left, bottom - top);
    }

    private sealed class ReadingOrderSortData
    {
        private ReadingOrderSortData(
            FocusNode node,
            Rect rect,
            IReadOnlyList<DirectionalityAncestor> ancestors,
            int sourceIndex)
        {
            Node = node;
            Rect = rect;
            Ancestors = ancestors;
            SourceIndex = sourceIndex;
        }

        public FocusNode Node { get; }

        public Rect Rect { get; }

        public IReadOnlyList<DirectionalityAncestor> Ancestors { get; }

        public int SourceIndex { get; }

        public TextDirection Direction => Ancestors.Count == 0
            ? TextDirection.Ltr
            : Ancestors[0].Direction;

        public static ReadingOrderSortData Create(FocusNode node, int sourceIndex)
        {
            ArgumentNullException.ThrowIfNull(node);
            return new ReadingOrderSortData(
                node,
                node.ResolveTraversalRect() ?? default,
                ResolveDirectionalityAncestors(node),
                sourceIndex);
        }

        private static IReadOnlyList<DirectionalityAncestor> ResolveDirectionalityAncestors(FocusNode node)
        {
            var result = new List<DirectionalityAncestor>();
            for (Element? ancestor = node.AttachmentElement?.Parent;
                 ancestor != null;
                 ancestor = ancestor.Parent)
            {
                if (ancestor.Widget is Directionality directionality)
                {
                    result.Add(new DirectionalityAncestor(ancestor, directionality.TextDirection));
                }
            }

            return result;
        }
    }

    private sealed record DirectionalityAncestor(Element Element, TextDirection Direction);

    private sealed class DirectionalRun
    {
        public DirectionalRun(TextDirection direction)
        {
            Direction = direction;
        }

        public TextDirection Direction { get; }

        public List<ReadingOrderSortData> Members { get; set; } = [];

        public Rect Bounds { get; set; }
    }
}

public sealed class FocusTraversalGroup : StatefulWidget
{
    public FocusTraversalGroup(
        Widget child,
        FocusTraversalPolicy? policy = null,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true,
        Action<FocusNode>? onFocusNodeCreated = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Policy = policy ?? new ReadingOrderTraversalPolicy();
        DescendantsAreFocusable = descendantsAreFocusable;
        DescendantsAreTraversable = descendantsAreTraversable;
        OnFocusNodeCreated = onFocusNodeCreated;
    }

    public Widget Child { get; }

    public FocusTraversalPolicy Policy { get; }

    public bool DescendantsAreFocusable { get; }

    public bool DescendantsAreTraversable { get; }

    public Action<FocusNode>? OnFocusNodeCreated { get; }

    public static FocusTraversalPolicy? MaybeOfNode(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node is FocusTraversalGroupNode groupNode
            ? groupNode.Policy
            : node.TraversalGroup?.Policy;
    }

    public static FocusTraversalPolicy? MaybeOf(BuildContext context)
    {
        return context.GetInherited<FocusTraversalGroupMarker>()?.GroupNode.Policy;
    }

    public static FocusTraversalPolicy Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("No FocusTraversalGroup policy found in context.");
    }

    public override State CreateState()
    {
        return new FocusTraversalGroupState();
    }

    private sealed class FocusTraversalGroupState : State
    {
        private FocusTraversalGroupNode? _groupNode;

        private FocusTraversalGroup Current => (FocusTraversalGroup)StateWidget;

        public override void InitState()
        {
            _groupNode = new FocusTraversalGroupNode(Current.Policy);
            Current.OnFocusNodeCreated?.Invoke(_groupNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _groupNode!.Policy = Current.Policy;
        }

        public override Widget Build(BuildContext context)
        {
            return new Focus(
                focusNode: _groupNode,
                canRequestFocus: false,
                skipTraversal: true,
                includeSemantics: false,
                descendantsAreFocusable: Current.DescendantsAreFocusable,
                descendantsAreTraversable: Current.DescendantsAreTraversable,
                child: new FocusTraversalGroupMarker(
                    groupNode: _groupNode!,
                    child: Current.Child));
        }

        public override void Dispose()
        {
            _groupNode?.Dispose();
            _groupNode = null;
        }
    }
}

internal sealed class FocusTraversalGroupNode : FocusNode
{
    public FocusTraversalGroupNode(FocusTraversalPolicy policy)
    {
        Policy = policy;
    }

    public FocusTraversalPolicy Policy { get; set; }
}

internal sealed class FocusTraversalGroupMarker : InheritedWidget
{
    public FocusTraversalGroupMarker(
        FocusTraversalGroupNode groupNode,
        Widget child) : base()
    {
        GroupNode = groupNode;
        Child = child;
    }

    public FocusTraversalGroupNode GroupNode { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((FocusTraversalGroupMarker)oldWidget).GroupNode, GroupNode);
    }
}
