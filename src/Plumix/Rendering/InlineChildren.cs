using Avalonia;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix.Rendering;

/// Parent data used by [RenderParagraph] to position its inline children.
public sealed class TextParentData : ContainerBoxParentData<RenderBox>
{
    /// The offset at which to paint the child in the parent's coordinate system.
    ///
    /// A `null` value indicates this child is not currently laid out, typically
    /// because it was truncated away by an ellipsis. Such a child is skipped when
    /// painting, hit testing and building semantics.
    public Point? InlineOffset { get; internal set; }

    /// The [PlaceholderSpan] associated with this render child.
    ///
    /// This field is usually set by a [ParentDataWidget], and is typically not
    /// null when `PerformLayout` is called.
    public PlaceholderSpan? Span { get; set; }

    /// Clears the state a detached child must not retain.
    public void DetachInline()
    {
        Span = null;
        InlineOffset = null;
    }

    public override string ToString()
    {
        return $"widget: {Span}, {(InlineOffset is null ? "not laid out" : $"offset: {InlineOffset}")}";
    }
}

/// Useful default behaviors for boxes whose children are inline placeholders
/// inside a paragraph.
///
/// Ported from Flutter's `RenderInlineChildrenContainerDefaults` mixin, which
/// C# expresses as static helpers over the child list.
public static class RenderInlineChildrenContainerDefaults
{
    /// Computes the [PlaceholderDimensions] for the given `child`.
    private static PlaceholderDimensions LayoutChild(
        RenderBox child,
        BoxConstraints childConstraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, TextBaseline, double?> getBaseline)
    {
        PlaceholderSpan? span = (child.parentData as TextParentData)?.Span;
        if (span is null)
        {
            return PlaceholderDimensions.Empty;
        }

        return new PlaceholderDimensions(
            size: layoutChild(child, childConstraints),
            alignment: span.Alignment,
            baseline: span.Baseline,
            baselineOffset: span.Alignment == PlaceholderAlignment.Baseline
                ? getBaseline(child, childConstraints, span.Baseline ?? TextBaseline.Alphabetic)
                : null);
    }

    /// Computes the [PlaceholderDimensions] for every inline child.
    ///
    /// Sizes the given inline children with the maximum width of the paragraph;
    /// their height is left unconstrained, so an inline child may be taller than
    /// the paragraph.
    public static List<PlaceholderDimensions> LayoutInlineChildren(
        IReadOnlyList<RenderBox> children,
        double maxWidth,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, TextBaseline, double?> getChildBaseline)
    {
        if (children.Count == 0)
        {
            return [];
        }

        var constraints = new BoxConstraints(MaxWidth: maxWidth);
        var dimensions = new List<PlaceholderDimensions>(children.Count);
        foreach (RenderBox child in children)
        {
            dimensions.Add(LayoutChild(child, constraints, layoutChild, getChildBaseline));
        }

        return dimensions;
    }

    /// Positions each inline child according to the corresponding laid-out box.
    ///
    /// Children that have no corresponding box, because they were truncated away,
    /// get a null offset.
    public static void PositionInlineChildren(IReadOnlyList<RenderBox> children, IReadOnlyList<Rect> boxes)
    {
        if (boxes.Count > children.Count)
        {
            throw new InvalidOperationException(
                "Invalid number of boxes provided to PositionInlineChildren. The number of boxes "
                + $"({boxes.Count}) exceeds the number of child render objects ({children.Count}).");
        }

        int index = 0;
        for (; index < boxes.Count; index += 1)
        {
            RequireParentData(children[index]).InlineOffset = boxes[index].Position;
        }

        for (; index < children.Count; index += 1)
        {
            RequireParentData(children[index]).InlineOffset = null;
        }
    }

    /// Paints the inline children in layout order, stopping at the first child
    /// that has no offset.
    public static void PaintInlineChildren(
        IReadOnlyList<RenderBox> children,
        PaintingContext context,
        Point offset)
    {
        foreach (RenderBox child in children)
        {
            Point? childOffset = RequireParentData(child).InlineOffset;
            if (childOffset is null)
            {
                return;
            }

            context.PaintChild(child, childOffset.Value + offset);
        }
    }

    /// Hit tests the inline children in layout order, stopping at the first child
    /// that has no offset.
    public static bool HitTestInlineChildren(
        IReadOnlyList<RenderBox> children,
        BoxHitTestResult result,
        Point position)
    {
        foreach (RenderBox child in children)
        {
            Point? childOffset = RequireParentData(child).InlineOffset;
            if (childOffset is null)
            {
                return false;
            }

            if (child.HitTest(result, position - childOffset.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static TextParentData RequireParentData(RenderBox child)
    {
        return child.parentData as TextParentData
               ?? throw new InvalidOperationException("An inline paragraph child requires TextParentData.");
    }
}
