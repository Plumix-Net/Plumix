using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/viewport.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget through which a portion of larger content can be viewed, laying its slivers out in both
/// growth directions from an optional <see cref="Center"/> child.
/// </summary>
public class Viewport : MultiChildRenderObjectWidget
{
    public Viewport(
        ViewportOffset offset,
        IReadOnlyList<Widget> slivers,
        AxisDirection axisDirection = AxisDirection.Down,
        AxisDirection? crossAxisDirection = null,
        double anchor = 0.0,
        Key? center = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(slivers, key)
    {
        ArgumentNullException.ThrowIfNull(offset);
        if (center != null && slivers.Count(child => Equals(child.Key, center)) != 1)
        {
            throw new ArgumentException(
                "The center key must match the key of exactly one of the supplied slivers.",
                nameof(center));
        }

        Offset = offset;
        AxisDirection = axisDirection;
        CrossAxisDirection = crossAxisDirection;
        Anchor = anchor;
        Center = center;
        ScrollCacheExtent = scrollCacheExtent;
        PaintOrder = paintOrder;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The direction in which the scroll offset increases.</summary>
    public AxisDirection AxisDirection { get; }

    /// <summary>
    /// The direction in which child slivers are laid out in the cross axis, or null to resolve it
    /// from the ambient <see cref="Directionality"/>.
    /// </summary>
    public AxisDirection? CrossAxisDirection { get; }

    /// <summary>Where the zero scroll offset sits, as a fraction of the viewport's main axis extent.</summary>
    public double Anchor { get; }

    /// <summary>Which part of the content inside the viewport should be visible.</summary>
    public ViewportOffset Offset { get; }

    /// <summary>The key of the first sliver laid out in the forward growth direction.</summary>
    public Key? Center { get; }

    /// <summary>How much content is laid out beyond the visible area.</summary>
    public ScrollCacheExtent? ScrollCacheExtent { get; }

    /// <summary>Which sliver paints on top when slivers overlap.</summary>
    public SliverPaintOrder PaintOrder { get; }

    public Clip ClipBehavior { get; }

    /// <summary>
    /// The cross-axis direction implied by <paramref name="axisDirection"/> and the ambient text
    /// direction.
    /// </summary>
    public static AxisDirection GetDefaultCrossAxisDirection(BuildContext context, AxisDirection axisDirection)
    {
        return axisDirection switch
        {
            AxisDirection.Up => TextDirectionToAxisDirection(Directionality.Of(context)),
            AxisDirection.Down => TextDirectionToAxisDirection(Directionality.Of(context)),
            AxisDirection.Right => AxisDirection.Down,
            _ => AxisDirection.Down,
        };
    }

    /// <remarks>Flutter's <c>textDirectionToAxisDirection</c>.</remarks>
    internal static AxisDirection TextDirectionToAxisDirection(TextDirection textDirection)
    {
        return textDirection == TextDirection.Rtl ? AxisDirection.Left : AxisDirection.Right;
    }

    internal override Element CreateElement() => new ViewportElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderViewport(
            offset: Offset,
            axisDirection: AxisDirection,
            crossAxisDirection: CrossAxisDirection
                                ?? GetDefaultCrossAxisDirection(context, AxisDirection),
            anchor: Anchor,
            scrollCacheExtent: ScrollCacheExtent,
            paintOrder: PaintOrder,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderViewport)renderObject;
        viewport.AxisDirection = AxisDirection;
        viewport.CrossAxisDirection = CrossAxisDirection
                                      ?? GetDefaultCrossAxisDirection(context, AxisDirection);
        viewport.Anchor = Anchor;
        viewport.Offset = Offset;
        viewport.ScrollCacheExtent = ScrollCacheExtent
                                     ?? ScrollCacheExtent.Pixels(RenderAbstractViewport.DefaultCacheExtent);
        viewport.PaintOrder = PaintOrder;
        viewport.ClipBehavior = ClipBehavior;
    }
}

/// <summary>
/// A widget that is bigger on the inside and shrink wraps its children in the main axis.
/// </summary>
public class ShrinkWrappingViewport : MultiChildRenderObjectWidget
{
    public ShrinkWrappingViewport(
        ViewportOffset offset,
        IReadOnlyList<Widget> slivers,
        AxisDirection axisDirection = AxisDirection.Down,
        AxisDirection? crossAxisDirection = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(slivers, key)
    {
        ArgumentNullException.ThrowIfNull(offset);
        Offset = offset;
        AxisDirection = axisDirection;
        CrossAxisDirection = crossAxisDirection;
        ScrollCacheExtent = scrollCacheExtent;
        PaintOrder = paintOrder;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The direction in which the scroll offset increases.</summary>
    public AxisDirection AxisDirection { get; }

    /// <summary>The direction in which child slivers are laid out in the cross axis.</summary>
    public AxisDirection? CrossAxisDirection { get; }

    /// <summary>Which part of the content inside the viewport should be visible.</summary>
    public ViewportOffset Offset { get; }

    /// <summary>How much content is laid out beyond the visible area.</summary>
    public ScrollCacheExtent? ScrollCacheExtent { get; }

    /// <summary>Which sliver paints on top when slivers overlap.</summary>
    public SliverPaintOrder PaintOrder { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderShrinkWrappingViewport(
            offset: Offset,
            axisDirection: AxisDirection,
            crossAxisDirection: CrossAxisDirection
                                ?? Viewport.GetDefaultCrossAxisDirection(context, AxisDirection),
            scrollCacheExtent: ScrollCacheExtent,
            paintOrder: PaintOrder,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderShrinkWrappingViewport)renderObject;
        viewport.AxisDirection = AxisDirection;
        viewport.CrossAxisDirection = CrossAxisDirection
                                      ?? Viewport.GetDefaultCrossAxisDirection(context, AxisDirection);
        viewport.Offset = Offset;
        viewport.ScrollCacheExtent = ScrollCacheExtent
                                     ?? ScrollCacheExtent.Pixels(RenderAbstractViewport.DefaultCacheExtent);
        viewport.PaintOrder = PaintOrder;
        viewport.ClipBehavior = ClipBehavior;
    }
}

/// <summary>
/// The element for a <see cref="Viewport"/>, which keeps the render viewport's <c>center</c> child
/// in sync with the widget's center key.
/// </summary>
/// <remarks>Flutter's private <c>_ViewportElement</c>.</remarks>
public sealed class ViewportElement : MultiChildRenderObjectElement
{
    private bool _doingMountOrUpdate;
    private int? _centerSlotIndex;

    public ViewportElement(Viewport widget) : base(widget)
    {
    }

    private RenderViewport RenderViewport => (RenderViewport)RequireRenderObject();

    protected override void OnMount()
    {
        _doingMountOrUpdate = true;
        base.OnMount();
        UpdateCenter();
        _doingMountOrUpdate = false;
    }

    internal override void Update(Widget newWidget)
    {
        _doingMountOrUpdate = true;
        base.Update(newWidget);
        UpdateCenter();
        _doingMountOrUpdate = false;
    }

    private void UpdateCenter()
    {
        var viewport = (Viewport)Widget;
        if (viewport.Center != null)
        {
            int elementIndex = 0;
            foreach (Element element in Children)
            {
                if (Equals(element.Widget.Key, viewport.Center))
                {
                    RenderViewport.Center = element.RenderObject as RenderSliver;
                    break;
                }

                elementIndex++;
            }

            _centerSlotIndex = elementIndex;
            return;
        }

        if (Children.Count > 0)
        {
            RenderViewport.Center = Children[0].RenderObject as RenderSliver;
            _centerSlotIndex = 0;
            return;
        }

        RenderViewport.Center = null;
        _centerSlotIndex = null;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        base.InsertRenderObjectChild(child, slot);
        if (!_doingMountOrUpdate && slot is IndexedSlot<Element?> indexedSlot
                                 && indexedSlot.Index == _centerSlotIndex)
        {
            RenderViewport.Center = child as RenderSliver;
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        base.RemoveRenderObjectChild(child, slot);
        if (!_doingMountOrUpdate && ReferenceEquals(RenderViewport.Center, child))
        {
            RenderViewport.Center = null;
        }
    }
}
