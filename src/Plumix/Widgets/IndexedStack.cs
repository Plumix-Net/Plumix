using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/indexed_stack.dart

namespace Plumix.Widgets;

/// <summary>A <see cref="Stack"/> that shows a single child from a list of children.</summary>
public sealed class IndexedStack : StatelessWidget
{
    public IndexedStack(
        IReadOnlyList<Widget>? children = null,
        int? index = 0,
        AlignmentGeometry? alignment = null,
        TextDirection? textDirection = null,
        Clip clipBehavior = Clip.HardEdge,
        StackFit sizing = StackFit.Loose,
        Key? key = null) : base(key)
    {
        Children = children ?? [];
        Index = index;
        Alignment = alignment ?? AlignmentDirectional.TopStart;
        TextDirection = textDirection;
        ClipBehavior = clipBehavior;
        Sizing = sizing;
    }

    /// <summary>The widgets below this widget in the tree.</summary>
    public IReadOnlyList<Widget> Children { get; }

    /// <summary>The index of the child to show.</summary>
    public int? Index { get; }

    /// <summary>How to align the non-positioned and partially-positioned children.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>The text direction with which to resolve <see cref="Alignment"/>.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>How to clip overflowing content. Defaults to <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>How to size the non-positioned children.</summary>
    public StackFit Sizing { get; }

    public override Widget Build(BuildContext context)
    {
        // Each child is wrapped with VisibilityScope (so Visibility.Of reports the child as hidden
        // when it is not the selected index) and with ExcludeFocus (so non-selected children cannot
        // receive focus). Neither introduces a RenderObject between the child and the enclosing
        // RenderIndexedStack, so ParentDataWidgets such as Positioned still apply their
        // StackParentData. Painting, hit-testing and semantics for non-selected children are
        // already handled by RenderIndexedStack.
        List<Widget> wrappedChildren = new(Children.Count);
        for (int i = 0; i < Children.Count; i++)
        {
            bool isSelected = i == Index;
            wrappedChildren.Add(new VisibilityScope(
                isSelected,
                new ExcludeFocus(Children[i], excluding: !isSelected)));
        }

        return new RawIndexedStack(
            wrappedChildren,
            Index,
            Alignment,
            TextDirection,
            ClipBehavior,
            Sizing);
    }
}

/// <summary>The render object widget that backs <see cref="IndexedStack"/>. Dart's private
/// `_RawIndexedStack`.</summary>
internal sealed class RawIndexedStack : Stack
{
    public RawIndexedStack(
        IReadOnlyList<Widget>? children = null,
        int? index = 0,
        AlignmentGeometry? alignment = null,
        TextDirection? textDirection = null,
        Clip clipBehavior = Clip.HardEdge,
        StackFit sizing = StackFit.Loose,
        Key? key = null)
        : base(
            children,
            alignment: alignment,
            fit: sizing,
            clipBehavior: clipBehavior,
            textDirection: textDirection,
            key: key)
    {
        if (Constants.KDebugMode
            && index is { } value
            && !(value == 0 && Children.Count == 0)
            && (value < 0 || value >= Children.Count))
        {
            throw new AssertionError("The index must be null or within the range of children.");
        }

        Index = index;
    }

    public int? Index { get; }

    internal override Element CreateElement() => new IndexedStackElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        TextDirection? textDirection = ResolveTextDirection(context);
        return new RenderIndexedStack(
            alignment: Alignment,
            textDirection: textDirection,
            fit: Fit,
            clipBehavior: ClipBehavior,
            index: Index);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderIndexedStack)renderObject;
        stack.Index = Index;
        stack.Fit = Fit;
        stack.ClipBehavior = ClipBehavior;
        stack.Alignment = Alignment;
        stack.TextDirection = ResolveTextDirection(context);
    }

    private TextDirection? ResolveTextDirection(BuildContext context)
    {
        TextDirection? textDirection = TextDirection ?? Directionality.MaybeOf(context);
        if (Constants.KDebugMode && Alignment.RequiresTextDirection && textDirection is null)
        {
            throw new AssertionError(
                "IndexedStack requires a Directionality ancestor when its alignment is directional.");
        }

        return textDirection;
    }
}

/// <summary>Dart's private `_IndexedStackElement`: visits only the displayed child onstage.</summary>
internal sealed class IndexedStackElement : MultiChildRenderObjectElement
{
    public IndexedStackElement(RawIndexedStack widget) : base(widget)
    {
    }

    internal override void DebugVisitOnstageChildren(Action<Element> visitor)
    {
        int? index = ((RawIndexedStack)Widget).Index;
        if (index.HasValue && Children.Count > 0)
        {
            visitor(Children[index.Value]);
        }
    }
}
