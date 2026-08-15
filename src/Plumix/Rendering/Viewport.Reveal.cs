using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart

namespace Plumix.Rendering;

/// <summary>
/// The offset a viewport must be scrolled to in order to reveal a particular element, together with
/// the rectangle that element would then occupy.
/// </summary>
public sealed class RevealedOffset
{
    public RevealedOffset(double offset, Rect rect)
    {
        Offset = offset;
        Rect = rect;
    }

    /// <summary>The offset the viewport must take to reveal the element.</summary>
    public double Offset { get; }

    /// <summary>
    /// Where the revealed element would sit relative to the top left corner of the visible part of
    /// the viewport, once <see cref="Offset"/> has been applied.
    /// </summary>
    public Rect Rect { get; }

    /// <summary>
    /// Picks the leading or trailing edge offset that actually has to be scrolled to, or null when
    /// the element is already fully visible at <paramref name="currentOffset"/>.
    /// </summary>
    public static RevealedOffset? ClampOffset(
        RevealedOffset leadingEdgeOffset,
        RevealedOffset trailingEdgeOffset,
        double currentOffset)
    {
        bool inverted = leadingEdgeOffset.Offset < trailingEdgeOffset.Offset;
        RevealedOffset smaller = inverted ? leadingEdgeOffset : trailingEdgeOffset;
        RevealedOffset larger = inverted ? trailingEdgeOffset : leadingEdgeOffset;
        if (currentOffset > larger.Offset)
        {
            return larger;
        }

        if (currentOffset < smaller.Offset)
        {
            return smaller;
        }

        return null;
    }

    public override string ToString()
    {
        return $"RevealedOffset(offset: {Offset}, rect: {Rect})";
    }
}

/// <summary>
/// A render object that is bigger on the inside: it shows a portion of its content, selected by a
/// scroll offset, and can report the offset that would reveal a given descendant.
/// </summary>
/// <remarks>
/// Flutter declares this as an <c>abstract interface class</c> that extends <c>RenderObject</c>. C#
/// interfaces cannot require a base class, so the contract is a plain interface implemented by the
/// viewport render objects, and Dart's statics live on the <see cref="RenderAbstractViewport"/>
/// companion class.
/// </remarks>
public interface IRenderAbstractViewport
{
    /// <summary>
    /// Returns the offset that would reveal <paramref name="target"/> (or <paramref name="rect"/>,
    /// expressed in the target's coordinate system) at the given <paramref name="alignment"/>.
    /// </summary>
    /// <param name="alignment">0.0 aligns the leading edge, 1.0 the trailing edge, 0.5 centers.</param>
    /// <param name="axis">
    /// Only meaningful for two-dimensional viewports; one-dimensional viewports replace it with
    /// their own axis rather than rejecting a mismatch.
    /// </param>
    RevealedOffset GetOffsetToReveal(
        RenderObject target,
        double alignment,
        Rect? rect = null,
        Axis? axis = null);

    /// <summary>Which part of the content inside the viewport should be visible.</summary>
    ViewportOffset Offset { get; }
}

/// <summary>Statics from Dart's <c>RenderAbstractViewport</c>.</summary>
public static class RenderAbstractViewport
{
    /// <summary>The default cache extent, in logical pixels, of a viewport.</summary>
    public const double DefaultCacheExtent = 250.0;

    /// <summary>The viewport that most tightly encloses the given render object, or null.</summary>
    public static IRenderAbstractViewport? MaybeOf(RenderObject? renderObject)
    {
        while (renderObject != null)
        {
            if (renderObject is IRenderAbstractViewport viewport)
            {
                return viewport;
            }

            renderObject = renderObject.Parent;
        }

        return null;
    }

    /// <summary>The viewport that most tightly encloses the given render object.</summary>
    public static IRenderAbstractViewport Of(RenderObject? renderObject)
    {
        return MaybeOf(renderObject)
               ?? throw new InvalidOperationException(
                   "RenderAbstractViewport.Of() was called with a render object that was not a "
                   + "descendant of a RenderAbstractViewport. The render object where the viewport "
                   + $"search started was: {renderObject}");
    }

    /// <summary>
    /// Scrolls <paramref name="viewport"/> just far enough to reveal <paramref name="descendant"/>,
    /// and returns the rectangle the revealed element ends up occupying, for the next viewport out.
    /// </summary>
    /// <remarks>Flutter's <c>RenderViewportBase.showInViewport</c>.</remarks>
    public static Rect? ShowInViewport(
        IRenderAbstractViewport viewport,
        ViewportOffset offset,
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (descendant is null)
        {
            return rect;
        }

        RevealedOffset leadingEdgeOffset = viewport.GetOffsetToReveal(descendant, 0.0, rect);
        RevealedOffset trailingEdgeOffset = viewport.GetOffsetToReveal(descendant, 1.0, rect);
        RevealedOffset? targetOffset = RevealedOffset.ClampOffset(
            leadingEdgeOffset,
            trailingEdgeOffset,
            offset.Pixels);
        if (targetOffset is null)
        {
            // Already fully visible: report the untouched rect in the viewport's parent space, so an
            // enclosing viewport keeps working with a rectangle it can interpret.
            var viewportObject = (RenderObject)viewport;
            Rect localRect = rect ?? descendant.PaintBounds;
            if (viewportObject.Parent is null)
            {
                return localRect;
            }

            return RenderObject.TransformRect(
                descendant.GetTransformTo(viewportObject.Parent),
                localRect);
        }

        _ = offset.MoveTo(targetOffset.Offset, duration, curve ?? Curves.Ease);
        return targetOffset.Rect;
    }
}
