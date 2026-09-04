using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/two_dimensional_viewport.dart

namespace Plumix.Rendering;

/// <summary>
/// The relative position of a child in a <see cref="RenderTwoDimensionalViewport"/>, as a pair of
/// indices along the horizontal and vertical axes.
/// </summary>
/// <remarks>
/// Flutter's <c>ChildVicinity</c>. The indices are relative to the neighbouring children; they are
/// not required to be a strict row/column numbering, so a table with merged cells may skip values.
/// </remarks>
public class ChildVicinity : IComparable<ChildVicinity>
{
    public ChildVicinity(int xIndex, int yIndex)
    {
        Debug.Assert(xIndex >= -1);
        Debug.Assert(yIndex >= -1);
        XIndex = xIndex;
        YIndex = yIndex;
    }

    /// <summary>
    /// An unassigned child position; the child may be moving from one position to another.
    /// </summary>
    public static ChildVicinity Invalid { get; } = new(xIndex: -1, yIndex: -1);

    /// <summary>The index of the child in the horizontal axis.</summary>
    public int XIndex { get; }

    /// <summary>The index of the child in the vertical axis.</summary>
    public int YIndex { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter compares <c>xIndex</c> first and returns the raw index difference rather than a
    /// normalized -1/0/1, so callers that read the magnitude see the same numbers.
    /// </remarks>
    public int CompareTo(ChildVicinity? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (XIndex == other.XIndex)
        {
            return YIndex - other.YIndex;
        }

        return XIndex - other.XIndex;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>operator ==</c> checks only <c>other is ChildVicinity</c>, so a subclass with the
    /// same indices equals its base; the port keeps that.
    /// </remarks>
    public override bool Equals(object? obj) =>
        obj is ChildVicinity other && other.XIndex == XIndex && other.YIndex == YIndex;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(XIndex, YIndex);

    public static bool operator ==(ChildVicinity? left, ChildVicinity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ChildVicinity? left, ChildVicinity? right) => !(left == right);

    public static bool operator <(ChildVicinity left, ChildVicinity right) => left.CompareTo(right) < 0;

    public static bool operator >(ChildVicinity left, ChildVicinity right) => left.CompareTo(right) > 0;

    public static bool operator <=(ChildVicinity left, ChildVicinity right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ChildVicinity left, ChildVicinity right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => $"(xIndex: {XIndex}, yIndex: {YIndex})";
}

/// <summary>
/// Parent data for the children of a <see cref="RenderTwoDimensionalViewport"/>.
/// </summary>
/// <remarks>Flutter's <c>TwoDimensionalViewportParentData</c>.</remarks>
public class TwoDimensionalViewportParentData : ParentData, IKeepAliveParentData
{
    /// <summary>
    /// The position of the child relative to the parent, in the viewport's own (unreversed)
    /// coordinate space. Must be set by <see cref="RenderTwoDimensionalViewport.LayoutChildSequence"/>.
    /// </summary>
    public Point? LayoutOffset { get; set; }

    /// <summary>The position of this child in the two-dimensional grid of children.</summary>
    public ChildVicinity Vicinity { get; set; } = ChildVicinity.Invalid;

    /// <summary>
    /// The visible portion of the child, which is the whole child unless it is clipped by the
    /// viewport's edge.
    /// </summary>
    /// <remarks>Flutter's private <c>_paintExtent</c>, written by <c>updateChildPaintData</c>.</remarks>
    internal Size? PaintExtent { get; set; }

    /// <remarks>Flutter's private <c>_previousSibling</c>.</remarks>
    internal RenderBox? PreviousSibling { get; set; }

    /// <remarks>Flutter's private <c>_nextSibling</c>.</remarks>
    internal RenderBox? NextSibling { get; set; }

    /// <summary>
    /// The distance from the top-left visible corner of the parent to the top-left visible corner of
    /// this child. Equal to <see cref="LayoutOffset"/> when both axis directions are
    /// <see cref="AxisDirection.Down"/> and <see cref="AxisDirection.Right"/>.
    /// </summary>
    public Point? PaintOffset { get; set; }

    /// <summary>Whether the child is currently visible in the viewport.</summary>
    public bool IsVisible
    {
        get
        {
            Debug.Assert(DebugPaintExtentDetermined());
            // Dart spells this as `_paintExtent != Size.zero || height != 0 || width != 0`; the
            // first disjunct already answers the question for every determined extent.
            return PaintExtent != default(Size)
                   || PaintExtent!.Value.Height != 0.0
                   || PaintExtent!.Value.Width != 0.0;
        }
    }

    /// <inheritdoc />
    public bool KeepAlive { get; set; }

    /// <inheritdoc />
    public bool KeptAlive => KeepAlive && !IsVisible;

    private bool DebugPaintExtentDetermined()
    {
        if (PaintExtent is null)
        {
            throw new FlutterError(
            [
                new ErrorSummary("The paint extent of the child has not been determined yet."),
                new ErrorDescription(
                    "The paint extent, and therefore the visibility, of a child of a "
                    + "RenderTwoDimensionalViewport is computed after "
                    + "RenderTwoDimensionalViewport.layoutChildSequence."),
            ]);
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string visibility = PaintExtent is null
            ? "not visible; "
            : $"{(!IsVisible ? "not " : string.Empty)}visible - paintExtent={DartFormat.SizeOf(PaintExtent.Value)}; ";
        return $"vicinity={Vicinity}; "
               + $"layoutOffset={(LayoutOffset is { } layout ? DartFormat.Offset(layout) : "null")}; "
               + $"paintOffset={(PaintOffset is { } paint ? DartFormat.Offset(paint) : "null")}; "
               + visibility
               + (KeepAlive ? "keepAlive; " : string.Empty);
    }
}

/// <summary>
/// Creates, reuses and removes the children of a <see cref="RenderTwoDimensionalViewport"/> lazily.
/// </summary>
/// <remarks>
/// Flutter's <c>TwoDimensionalChildManager</c>, whose members are library-private. C# has no
/// library-private members, so the four methods are public but are only ever called by
/// <see cref="RenderTwoDimensionalViewport"/>.
/// </remarks>
public interface ITwoDimensionalChildManager
{
    /// <summary>Called by the viewport before it lays out its children.</summary>
    void StartLayout();

    /// <summary>Builds the child at <paramref name="vicinity"/>, if the delegate provides one.</summary>
    void BuildChild(ChildVicinity vicinity);

    /// <summary>Carries the existing child at <paramref name="vicinity"/> into this layout pass.</summary>
    void ReuseChild(ChildVicinity vicinity);

    /// <summary>Called by the viewport after it has laid out its children.</summary>
    void EndLayout();
}

/// <summary>
/// A render object that lays out and paints a lazily built, two-dimensional grid of children.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderTwoDimensionalViewport</c>. Subclasses implement
/// <see cref="LayoutChildSequence"/> and must not override <see cref="PerformLayout"/>, which does
/// the bookkeeping on both sides of it.
/// </remarks>
public abstract class RenderTwoDimensionalViewport : RenderBox, IRenderAbstractViewport
{
    private readonly ITwoDimensionalChildManager _childManager;
    private readonly Dictionary<ChildVicinity, RenderBox> _children = [];
    private readonly Dictionary<ChildVicinity, RenderBox> _activeChildrenForLayoutPass = [];
    private readonly Dictionary<ChildVicinity, RenderBox> _keepAliveBucket = [];
    private readonly List<RenderBox> _debugDanglingKeepAlives = [];
    private readonly List<ChildVicinity> _currentChildVicinities = [];
    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();
    private readonly Action _markNeedsLayoutListener;
    private readonly Action _delegateNotificationListener;

    private ViewportOffset _horizontalOffset;
    private AxisDirection _horizontalAxisDirection;
    private ViewportOffset _verticalOffset;
    private AxisDirection _verticalAxisDirection;
    private TwoDimensionalChildDelegate _delegate;
    private Axis _mainAxis;
    private ScrollCacheExtent _scrollCacheExtent;
    private Clip _clipBehavior;
    private bool _hasVisualOverflow;
    private RenderBox? _firstChild;
    private RenderBox? _lastChild;
    private bool _didResize = true;
    private bool _needsDelegateRebuild = true;
    private List<RenderBox>? _debugOrphans;

    protected RenderTwoDimensionalViewport(
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        TwoDimensionalChildDelegate @delegate,
        Axis mainAxis,
        ITwoDimensionalChildManager childManager,
        ScrollCacheExtent? scrollCacheExtent = null,
        Clip clipBehavior = Clip.HardEdge)
    {
        if (verticalAxisDirection != AxisDirection.Down && verticalAxisDirection != AxisDirection.Up)
        {
            throw new AssertionError("TwoDimensionalViewport.verticalAxisDirection is not Axis.vertical.");
        }

        if (horizontalAxisDirection != AxisDirection.Left && horizontalAxisDirection != AxisDirection.Right)
        {
            throw new AssertionError("TwoDimensionalViewport.horizontalAxisDirection is not Axis.horizontal.");
        }

        _childManager = childManager;
        _horizontalOffset = horizontalOffset;
        _horizontalAxisDirection = horizontalAxisDirection;
        _verticalOffset = verticalOffset;
        _verticalAxisDirection = verticalAxisDirection;
        _delegate = @delegate;
        _mainAxis = mainAxis;
        _scrollCacheExtent = scrollCacheExtent
                             ?? ScrollCacheExtent.Pixels(RenderAbstractViewport.DefaultCacheExtent);
        _clipBehavior = clipBehavior;
        _markNeedsLayoutListener = () => MarkNeedsLayout();
        _delegateNotificationListener = HandleDelegateNotification;
    }

    /// <summary>Which part of the content inside the viewport should be visible horizontally.</summary>
    public ViewportOffset HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            if (ReferenceEquals(_horizontalOffset, value))
            {
                return;
            }

            if (Attached)
            {
                _horizontalOffset.RemoveListener(_markNeedsLayoutListener);
            }

            _horizontalOffset = value;
            if (Attached)
            {
                _horizontalOffset.AddListener(_markNeedsLayoutListener);
            }

            MarkNeedsLayout();
        }
    }

    /// <summary>The direction in which <see cref="HorizontalOffset"/> increases.</summary>
    public AxisDirection HorizontalAxisDirection
    {
        get => _horizontalAxisDirection;
        set
        {
            if (_horizontalAxisDirection == value)
            {
                return;
            }

            _horizontalAxisDirection = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Which part of the content inside the viewport should be visible vertically.</summary>
    public ViewportOffset VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            if (ReferenceEquals(_verticalOffset, value))
            {
                return;
            }

            if (Attached)
            {
                _verticalOffset.RemoveListener(_markNeedsLayoutListener);
            }

            _verticalOffset = value;
            if (Attached)
            {
                _verticalOffset.AddListener(_markNeedsLayoutListener);
            }

            MarkNeedsLayout();
        }
    }

    /// <summary>The direction in which <see cref="VerticalOffset"/> increases.</summary>
    public AxisDirection VerticalAxisDirection
    {
        get => _verticalAxisDirection;
        set
        {
            if (_verticalAxisDirection == value)
            {
                return;
            }

            _verticalAxisDirection = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Supplies the children for this viewport.</summary>
    public TwoDimensionalChildDelegate Delegate
    {
        get => _delegate;
        set
        {
            if (ReferenceEquals(_delegate, value))
            {
                return;
            }

            if (Attached)
            {
                _delegate.RemoveListener(_delegateNotificationListener);
            }

            TwoDimensionalChildDelegate oldDelegate = _delegate;
            _delegate = value;
            if (Attached)
            {
                _delegate.AddListener(_delegateNotificationListener);
            }

            if (_delegate.GetType() != oldDelegate.GetType() || _delegate.ShouldRebuild(oldDelegate))
            {
                HandleDelegateNotification();
            }
        }
    }

    /// <summary>
    /// The major of the two axes, which decides the order in which children are painted:
    /// <see cref="Axis.Vertical"/> paints row major, <see cref="Axis.Horizontal"/> column major.
    /// </summary>
    public Axis MainAxis
    {
        get => _mainAxis;
        set
        {
            if (_mainAxis == value)
            {
                return;
            }

            _mainAxis = value;
            // Child order needs to be resorted, which happens in PerformLayout.
            MarkNeedsLayout();
        }
    }

    /// <summary>How much content beyond the visible area is laid out.</summary>
    public ScrollCacheExtent ScrollCacheExtent
    {
        get => _scrollCacheExtent;
        set
        {
            ScrollCacheExtent effectiveValue = value
                                               ?? ScrollCacheExtent.Pixels(
                                                   RenderAbstractViewport.DefaultCacheExtent);
            if (_scrollCacheExtent == effectiveValue)
            {
                return;
            }

            _scrollCacheExtent = effectiveValue;
            MarkNeedsLayout();
        }
    }

    /// <summary>The raw value of <see cref="ScrollCacheExtent"/>, in its own <see cref="CacheExtentStyle"/>.</summary>
    public double CacheExtent
    {
        get => _scrollCacheExtent.Value;
        set => ScrollCacheExtent = _scrollCacheExtent.Style == CacheExtentStyle.Viewport
            ? ScrollCacheExtent.Viewport(value)
            : ScrollCacheExtent.Pixels(value);
    }

    /// <summary>Whether <see cref="CacheExtent"/> counts pixels or viewport fractions.</summary>
    public CacheExtentStyle CacheExtentStyle
    {
        get => _scrollCacheExtent.Style;
        set => ScrollCacheExtent = value == CacheExtentStyle.Viewport
            ? ScrollCacheExtent.Viewport(CacheExtent)
            : ScrollCacheExtent.Pixels(CacheExtent);
    }

    /// <summary>How the viewport clips content that overflows it.</summary>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <inheritdoc />
    public override bool IsRepaintBoundary => true;

    /// <inheritdoc />
    protected override bool SizedByParent => true;

    /// <summary>
    /// The first child in paint order, or null. Meaningless during
    /// <see cref="LayoutChildSequence"/>, which runs before the paint order is rebuilt.
    /// </summary>
    public RenderBox? FirstChild => _firstChild;

    /// <summary>The last child in paint order, or null.</summary>
    public RenderBox? LastChild => _lastChild;

    /// <summary>Whether the viewport changed size since the last layout pass.</summary>
    public bool DidResize => _didResize;

    /// <summary>Whether the delegate must be consulted again for every child of this layout pass.</summary>
    protected bool NeedsDelegateRebuild => _needsDelegateRebuild;

    /// <summary>The size of the viewport, which is always the biggest size its parent allows.</summary>
    public Size ViewportDimension
    {
        get
        {
            Debug.Assert(HasSize);
            return Size;
        }
    }

    /// <remarks>
    /// Dart's <c>RenderAbstractViewport</c> has no <c>offset</c>; the C# interface adds one so
    /// <see cref="RenderAbstractViewport.ShowInViewport"/> can drive a one-dimensional viewport. A
    /// two-dimensional viewport answers with the offset of its <see cref="MainAxis"/>, and never
    /// takes that path itself because it overrides <see cref="ShowOnScreen"/>.
    /// </remarks>
    ViewportOffset IRenderAbstractViewport.Offset =>
        MainAxis == Axis.Vertical ? VerticalOffset : HorizontalOffset;

    /// <summary>The child before <paramref name="child"/> in paint order, or null.</summary>
    public RenderBox? ChildBefore(RenderBox child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return ParentDataOf(child).PreviousSibling;
    }

    /// <summary>The child after <paramref name="child"/> in paint order, or null.</summary>
    public RenderBox? ChildAfter(RenderBox child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return ParentDataOf(child).NextSibling;
    }

    private void HandleDelegateNotification() => MarkNeedsLayout(withDelegateRebuild: true);

    /// <inheritdoc />
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TwoDimensionalViewportParentData)
        {
            child.parentData = new TwoDimensionalViewportParentData();
        }
    }

    /// <summary>The parent data of <paramref name="child"/>, which this viewport owns.</summary>
    public virtual TwoDimensionalViewportParentData ParentDataOf(RenderBox child)
    {
        Debug.Assert(
            _children.ContainsValue(child)
            || _keepAliveBucket.ContainsValue(child)
            || (_debugOrphans?.Contains(child) ?? false));
        return (TwoDimensionalViewportParentData)child.parentData!;
    }

    /// <summary>The active child at <paramref name="vicinity"/>, or null when there is none.</summary>
    protected RenderBox? GetChildFor(ChildVicinity vicinity) =>
        _children.TryGetValue(vicinity, out RenderBox? child) ? child : null;

    /// <inheritdoc />
    protected override void OnAttach()
    {
        base.OnAttach();
        _horizontalOffset.AddListener(_markNeedsLayoutListener);
        _verticalOffset.AddListener(_markNeedsLayoutListener);
        _delegate.AddListener(_delegateNotificationListener);
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _horizontalOffset.RemoveListener(_markNeedsLayoutListener);
        _verticalOffset.RemoveListener(_markNeedsLayoutListener);
        _delegate.RemoveListener(_delegateNotificationListener);
        base.OnDetach();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart re-deepens the active children with <c>child.redepthChildren()</c>, which leaves their
    /// own depth to <c>adoptChild</c>; C# cannot reach a sibling's protected member, so both groups
    /// go through <see cref="RenderObject.RedepthChild"/>, which also sets that depth.
    /// </remarks>
    protected override void RedepthChildren()
    {
        foreach (RenderBox child in _children.Values)
        {
            RedepthChild(child);
        }

        foreach (RenderBox child in _keepAliveBucket.Values)
        {
            RedepthChild(child);
        }
    }

    /// <inheritdoc />
    public override void VisitChildren(Action<RenderObject> visitor)
    {
        RenderBox? child = _firstChild;
        while (child != null)
        {
            visitor(child);
            child = ParentDataOf(child).NextSibling;
        }

        foreach (RenderBox keptAlive in _keepAliveBucket.Values)
        {
            visitor(keptAlive);
        }
    }

    /// <inheritdoc />
    /// <remarks>Kept-alive children are offscreen, so they contribute no semantics.</remarks>
    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        RenderBox? child = _firstChild;
        while (child != null)
        {
            TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
            visitor(child);
            child = childParentData.NextSibling;
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        return _children.Keys
            .Select(vicinity => _children[vicinity].ToDiagnosticsNode(name: vicinity.ToString()))
            .ToList();
    }

    /// <inheritdoc />
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Debug.Assert(RenderingDebug.CheckHasBoundedAxis(Axis.Vertical, constraints));
        Debug.Assert(RenderingDebug.CheckHasBoundedAxis(Axis.Horizontal, constraints));
        return constraints.Biggest;
    }

    /// <inheritdoc />
    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        foreach (RenderBox child in _children.Values)
        {
            TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
            if (!childParentData.IsVisible)
            {
                // Can't hit a child that is not visible.
                continue;
            }

            bool isHit = result.AddWithPaintOffset(
                childParentData.PaintOffset,
                position,
                (hitResult, transformed) => child.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override void PerformResize()
    {
        Size? oldSize = HasSize ? Size : null;
        base.PerformResize();
        // Ignoring the return value since a layout follows either way.
        HorizontalOffset.ApplyViewportDimension(Size.Width);
        VerticalOffset.ApplyViewportDimension(Size.Height);
        if (oldSize != Size)
        {
            // Subclass layout can depend on the viewport size.
            _didResize = true;
        }
    }

    /// <inheritdoc />
    public RevealedOffset GetOffsetToReveal(
        RenderObject target,
        double alignment,
        Rect? rect = null,
        Axis? axis = null)
    {
        axis ??= MainAxis;
        double offset;
        AxisDirection axisDirection;
        if (axis == Axis.Vertical)
        {
            offset = VerticalOffset.Pixels;
            axisDirection = VerticalAxisDirection;
        }
        else
        {
            offset = HorizontalOffset.Pixels;
            axisDirection = HorizontalAxisDirection;
        }

        rect ??= target.PaintBounds;
        RenderObject child = target;
        while (!ReferenceEquals(child.Parent, this))
        {
            child = child.Parent!;
        }

        var box = (RenderBox)child;
        Rect rectLocal = MatrixUtils.TransformRect(target.GetTransformTo(child), rect.Value);
        double leadingScrollOffset = offset;

        // Scroll offset of `rect` within `child`.
        leadingScrollOffset += axisDirection switch
        {
            AxisDirection.Up => box.Size.Height - rectLocal.Bottom,
            AxisDirection.Left => box.Size.Width - rectLocal.Right,
            AxisDirection.Right => rectLocal.Left,
            _ => rectLocal.Top,
        };

        // Scroll offset of `child` within the viewport.
        Point paintOffset = ParentDataOf(box).PaintOffset!.Value;
        leadingScrollOffset += axisDirection switch
        {
            AxisDirection.Up => ViewportDimension.Height - paintOffset.Y - box.Size.Height,
            AxisDirection.Left => ViewportDimension.Width - paintOffset.X - box.Size.Width,
            AxisDirection.Right => paintOffset.X,
            _ => paintOffset.Y,
        };

        Matrix4 transform = target.GetTransformTo(this);
        Rect targetRect = MatrixUtils.TransformRect(transform, rect.Value);
        double mainAxisExtentDifference = axis == Axis.Horizontal
            ? ViewportDimension.Width - rectLocal.Width
            : ViewportDimension.Height - rectLocal.Height;
        double targetOffset = leadingScrollOffset - (mainAxisExtentDifference * alignment);
        double offsetDifference = axis == Axis.Horizontal
            ? HorizontalOffset.Pixels - targetOffset
            : VerticalOffset.Pixels - targetOffset;

        targetRect = axisDirection switch
        {
            AxisDirection.Up => Translate(targetRect, 0.0, -offsetDifference),
            AxisDirection.Down => Translate(targetRect, 0.0, offsetDifference),
            AxisDirection.Left => Translate(targetRect, -offsetDifference, 0.0),
            _ => Translate(targetRect, offsetDifference, 0.0),
        };

        return new RevealedOffset(targetOffset, targetRect);
    }

    private static Rect Translate(Rect rect, double dx, double dy) =>
        new(rect.X + dx, rect.Y + dy, rect.Width, rect.Height);

    /// <inheritdoc />
    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        bool allowHorizontal = HorizontalOffset.AllowImplicitScrolling;
        bool allowVertical = VerticalOffset.AllowImplicitScrolling;
        AxisDirection? axisDirection = null;
        switch (allowHorizontal, allowVertical)
        {
            case (true, true):
                break;
            case (false, true):
                axisDirection = VerticalAxisDirection;
                break;
            case (true, false):
                axisDirection = HorizontalAxisDirection;
                break;
            case (false, false):
                base.ShowOnScreen(descendant, rect, duration, curve);
                return;
        }

        Rect? newRect = ShowInViewport(
            descendant: descendant,
            viewport: this,
            axisDirection: axisDirection,
            rect: rect,
            duration: duration,
            curve: curve);

        base.ShowOnScreen(rect: newRect, duration: duration, curve: curve);
    }

    /// <summary>
    /// Scrolls <paramref name="viewport"/> just far enough to reveal <paramref name="descendant"/>,
    /// on one axis or on both, and returns the rectangle it ends up occupying.
    /// </summary>
    /// <remarks>Flutter's <c>RenderTwoDimensionalViewport.showInViewport</c>.</remarks>
    public static Rect? ShowInViewport(
        RenderTwoDimensionalViewport viewport,
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null,
        AxisDirection? axisDirection = null)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        if (descendant is null)
        {
            return rect;
        }

        Rect? ShowVertical(Rect? target) => ShowInViewportForAxisDirection(
            descendant, viewport, Axis.Vertical, target, duration, curve);

        Rect? ShowHorizontal(Rect? target) => ShowInViewportForAxisDirection(
            descendant, viewport, Axis.Horizontal, target, duration, curve);

        switch (axisDirection)
        {
            case AxisDirection.Left:
            case AxisDirection.Right:
                return ShowHorizontal(rect);
            case AxisDirection.Up:
            case AxisDirection.Down:
                return ShowVertical(rect);
            default:
                // Update rect after revealing in one axis before revealing in the next.
                rect = ShowHorizontal(rect) ?? rect;
                // Only the final rect is returned, after both axes have been revealed.
                rect = ShowVertical(rect);
                if (rect is null)
                {
                    // `descendant` is between the leading and trailing edge and hence already fully
                    // shown on screen.
                    Debug.Assert(viewport.Parent != null);
                    Matrix4 transform = descendant.GetTransformTo(viewport.Parent);
                    return MatrixUtils.TransformRect(transform, descendant.PaintBounds);
                }

                return rect;
        }
    }

    private static Rect? ShowInViewportForAxisDirection(
        RenderObject descendant,
        RenderTwoDimensionalViewport viewport,
        Axis axis,
        Rect? rect,
        TimeSpan duration,
        Curve? curve)
    {
        ViewportOffset offset = axis == Axis.Vertical ? viewport.VerticalOffset : viewport.HorizontalOffset;
        RevealedOffset leadingEdgeOffset = viewport.GetOffsetToReveal(descendant, 0.0, rect, axis);
        RevealedOffset trailingEdgeOffset = viewport.GetOffsetToReveal(descendant, 1.0, rect, axis);
        double currentOffset = offset.Pixels;
        RevealedOffset? targetOffset = RevealedOffset.ClampOffset(
            leadingEdgeOffset,
            trailingEdgeOffset,
            currentOffset);
        if (targetOffset is null)
        {
            // Already visible in this axis.
            return null;
        }

        _ = offset.MoveTo(targetOffset.Offset, duration, curve ?? Curves.Ease);
        return targetOffset.Rect;
    }

    /// <inheritdoc />
    public override void MarkNeedsLayout() => MarkNeedsLayout(withDelegateRebuild: false);

    /// <summary>
    /// Marks the viewport as needing layout, optionally asking every child to be rebuilt from the
    /// delegate rather than reused.
    /// </summary>
    /// <remarks>Flutter's <c>markNeedsLayout({bool withDelegateRebuild = false})</c>.</remarks>
    public void MarkNeedsLayout(bool withDelegateRebuild)
    {
        _needsDelegateRebuild = _needsDelegateRebuild || withDelegateRebuild;
        base.MarkNeedsLayout();
    }

    /// <summary>
    /// Lays out the children the viewport needs, obtaining each one through
    /// <see cref="BuildOrObtainChildFor"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderTwoDimensionalViewport.layoutChildSequence</c>. Implementations must lay
    /// out every child they obtain with <c>parentUsesSize: true</c>, set its
    /// <see cref="TwoDimensionalViewportParentData.LayoutOffset"/>, and call
    /// <see cref="ViewportOffset.ApplyContentDimensions"/> on both offsets before returning.
    /// </remarks>
    protected abstract void LayoutChildSequence();

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        _firstChild = null;
        _lastChild = null;
        _activeChildrenForLayoutPass.Clear();
        _childManager.StartLayout();

        // Subclass lays out children.
        LayoutChildSequence();

        Debug.Assert(DebugCheckContentDimensions());
        _didResize = false;
        _needsDelegateRebuild = false;
        CacheKeepAlives();
        InvokeLayoutCallback<BoxConstraints>(
            _ =>
            {
                _childManager.EndLayout();
                Debug.Assert(_debugOrphans?.Count is null or 0);
                Debug.Assert(_debugDanglingKeepAlives.Count == 0);
                // Ensure we are not keeping anything alive that should not be any longer.
                Debug.Assert(!_keepAliveBucket.Values.Any(child => !ParentDataOf(child).KeepAlive));
                // Organize children in paint order and complete parent data after un-used children
                // are disposed of by the child manager.
                ReifyChildren();
            },
            Constraints);
    }

    private void CacheKeepAlives()
    {
        List<RenderBox> remainingChildren = _children.Values
            .Distinct()
            .Except(_activeChildrenForLayoutPass.Values)
            .ToList();
        foreach (RenderBox child in remainingChildren)
        {
            TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
            if (childParentData.KeepAlive)
            {
                _keepAliveBucket[childParentData.Vicinity] = child;
                // Let the child manager know we intend to keep this.
                _childManager.ReuseChild(childParentData.Vicinity);
            }
        }
    }

    /// <summary>Sorts the current vicinities row major.</summary>
    private void SortByYIndex()
    {
        _currentChildVicinities.Sort((a, b) =>
        {
            int yComparison = a.YIndex.CompareTo(b.YIndex);
            return yComparison != 0 ? yComparison : a.XIndex.CompareTo(b.XIndex);
        });
    }

    /// <summary>Sorts the current vicinities column major.</summary>
    private void SortByXIndex() => _currentChildVicinities.Sort();

    private void ReifyChildren()
    {
        Debug.Assert(_firstChild is null);
        Debug.Assert(_lastChild is null);
        RenderBox? previousChild = null;
        switch (MainAxis)
        {
            case Axis.Vertical:
                // Row major traversal.
                SortByYIndex();
                break;
            default:
                // Column major traversal.
                SortByXIndex();
                break;
        }

        foreach (ChildVicinity vicinity in _currentChildVicinities)
        {
            previousChild = CompleteChildParentData(vicinity, previousChild) ?? previousChild;
        }

        _lastChild = previousChild;
        if (_lastChild != null)
        {
            ParentDataOf(_lastChild).NextSibling = null;
        }

        // Reset for the next layout pass.
        _currentChildVicinities.Clear();
    }

    private RenderBox? CompleteChildParentData(ChildVicinity vicinity, RenderBox? previousChild)
    {
        Debug.Assert(vicinity != ChildVicinity.Invalid);
        // It is possible and valid for a vicinity to be skipped.
        if (_children.TryGetValue(vicinity, out RenderBox? child))
        {
            Debug.Assert(ParentDataOf(child).Vicinity == vicinity);
            UpdateChildPaintData(child);
            if (previousChild is null)
            {
                // FirstChild is only set once.
                Debug.Assert(_firstChild is null);
                _firstChild = child;
            }
            else
            {
                ParentDataOf(previousChild).NextSibling = child;
                ParentDataOf(child).PreviousSibling = previousChild;
            }

            return child;
        }

        return null;
    }

    private bool DebugCheckContentDimensions()
    {
        const string hint =
            "Subclasses should call applyContentDimensions on the verticalOffset and "
            + "horizontalOffset to set the min and max scroll offset. If the contents exceed one or "
            + "both sides of the viewportDimension, ensure the viewportDimension height or width is "
            + "subtracted in that axis for the correct extent.";
        if (VerticalOffset is ScrollPosition { HasContentDimensions: false })
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "The verticalOffset was not given content dimensions during layoutChildSequence."),
                new ErrorHint(hint),
            ]);
        }

        if (HorizontalOffset is ScrollPosition { HasContentDimensions: false })
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "The horizontalOffset was not given content dimensions during layoutChildSequence."),
                new ErrorHint(hint),
            ]);
        }

        return true;
    }

    /// <summary>
    /// Returns the child at <paramref name="vicinity"/>, building it through the child manager when
    /// it is not already live, or null when the delegate has no child there.
    /// </summary>
    public RenderBox? BuildOrObtainChildFor(ChildVicinity vicinity)
    {
        if (vicinity == ChildVicinity.Invalid)
        {
            throw new AssertionError("A child cannot be built for ChildVicinity.invalid.");
        }

        // This should only be called during layout.
        Debug.Assert(DebugDoingThisLayout);
        if (_needsDelegateRebuild
            || (!_children.ContainsKey(vicinity) && !_keepAliveBucket.ContainsKey(vicinity)))
        {
            InvokeLayoutCallback<BoxConstraints>(_ => _childManager.BuildChild(vicinity), Constraints);
        }
        else
        {
            _keepAliveBucket.Remove(vicinity);
            _childManager.ReuseChild(vicinity);
        }

        if (!_children.TryGetValue(vicinity, out RenderBox? child))
        {
            // There is no child for this vicinity; the end of the children in one or both of the
            // x/y indices may have been reached.
            return null;
        }

        _activeChildrenForLayoutPass[vicinity] = child;
        ParentDataOf(child).Vicinity = vicinity;
        _currentChildVicinities.Add(vicinity);
        return child;
    }

    /// <summary>
    /// Computes the paint extent, paint offset and visibility of <paramref name="child"/> from the
    /// layout offset its subclass assigned.
    /// </summary>
    public void UpdateChildPaintData(RenderBox child)
    {
        TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
        if (childParentData.LayoutOffset is null)
        {
            throw new AssertionError(
                $"The child with ChildVicinity(xIndex: {childParentData.Vicinity.XIndex}, "
                + $"yIndex: {childParentData.Vicinity.YIndex}) was not provided a layoutOffset. This "
                + "should be set during layoutChildSequence, representing the position of the child.");
        }

        if (!child.HasSize)
        {
            // Child must have been laid out by now.
            throw new AssertionError(
                $"The child with ChildVicinity(xIndex: {childParentData.Vicinity.XIndex}, "
                + $"yIndex: {childParentData.Vicinity.YIndex}) was not laid out during "
                + "layoutChildSequence, so child.HasSize is false.");
        }

        Point layoutOffset = childParentData.LayoutOffset.Value;
        childParentData.PaintExtent = ComputeChildPaintExtent(layoutOffset, child.Size);
        childParentData.PaintOffset = ComputeAbsolutePaintOffsetFor(child, layoutOffset);
        // Flutter writes `_hasVisualOverflow || layoutOffset != _paintExtent || !isVisible` here, and
        // the middle term compares an Offset against a Size, which Dart's `Offset.operator ==` never
        // accepts. It is therefore always true and latches the flag on the first child of the pass,
        // so a two-dimensional viewport always clips. Reproduced, rather than "fixed", for parity.
        _hasVisualOverflow = true;
    }

    /// <summary>The visible portion of a child laid out at <paramref name="layoutOffset"/>.</summary>
    public Size ComputeChildPaintExtent(Point layoutOffset, Size childSize)
    {
        if (childSize == default(Size) || childSize.Height == 0.0 || childSize.Width == 0.0)
        {
            return default;
        }

        double width;
        if (layoutOffset.X < 0.0)
        {
            if (layoutOffset.X + childSize.Width <= 0.0)
            {
                return default;
            }

            width = layoutOffset.X + childSize.Width;
        }
        else if (layoutOffset.X >= ViewportDimension.Width)
        {
            return default;
        }
        else
        {
            width = layoutOffset.X + childSize.Width > ViewportDimension.Width
                ? ViewportDimension.Width - layoutOffset.X
                : childSize.Width;
        }

        double height;
        if (layoutOffset.Y < 0.0)
        {
            if (layoutOffset.Y + childSize.Height <= 0.0)
            {
                return default;
            }

            height = layoutOffset.Y + childSize.Height;
        }
        else if (layoutOffset.Y >= ViewportDimension.Height)
        {
            return default;
        }
        else
        {
            height = layoutOffset.Y + childSize.Height > ViewportDimension.Height
                ? ViewportDimension.Height - layoutOffset.Y
                : childSize.Height;
        }

        return new Size(width, height);
    }

    /// <summary>
    /// Converts the unreversed <paramref name="layoutOffset"/> into the offset the child is painted
    /// at, applying both axis directions.
    /// </summary>
    protected Point ComputeAbsolutePaintOffsetFor(RenderBox child, Point layoutOffset)
    {
        // This is only usable once we have sizes.
        Debug.Assert(HasSize);
        Debug.Assert(child.HasSize);
        double xOffset = HorizontalAxisDirection switch
        {
            AxisDirection.Right => layoutOffset.X,
            AxisDirection.Left => ViewportDimension.Width - (layoutOffset.X + child.Size.Width),
            _ => throw new InvalidOperationException("This should not happen"),
        };
        double yOffset = VerticalAxisDirection switch
        {
            AxisDirection.Up => ViewportDimension.Height - (layoutOffset.Y + child.Size.Height),
            AxisDirection.Down => layoutOffset.Y,
            _ => throw new InvalidOperationException("This should not happen"),
        };
        return new Point(xOffset, yOffset);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (_children.Count == 0)
        {
            return;
        }

        if (_hasVisualOverflow && ClipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(0, 0, ViewportDimension.Width, ViewportDimension.Height),
                PaintChildren,
                ClipBehavior,
                _clipRectLayer.Layer);
        }
        else
        {
            _clipRectLayer.Layer = null;
            PaintChildren(context, offset);
        }
    }

    private void PaintChildren(PaintingContext context, Point offset)
    {
        RenderBox? child = _firstChild;
        while (child != null)
        {
            TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
            if (childParentData.IsVisible)
            {
                Point paintOffset = childParentData.PaintOffset!.Value;
                context.PaintChild(child, new Point(offset.X + paintOffset.X, offset.Y + paintOffset.Y));
            }

            child = childParentData.NextSibling;
        }
    }

    internal void InsertChild(RenderBox child, ChildVicinity slot)
    {
        Debug.Assert(DebugTrackOrphans(newOrphan: _children.GetValueOrDefault(slot)));
        Debug.Assert(!_keepAliveBucket.ContainsValue(child));
        _children[slot] = child;
        AdoptChild(child);
    }

    internal void MoveChild(RenderBox child, ChildVicinity from, ChildVicinity to)
    {
        TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
        if (!childParentData.KeptAlive)
        {
            if (_children.GetValueOrDefault(from) == child)
            {
                _children.Remove(from);
            }

            Debug.Assert(DebugTrackOrphans(newOrphan: _children.GetValueOrDefault(to), noLongerOrphan: child));
            _children[to] = child;
            return;
        }

        // If the child in the bucket is not the current child, someone has already moved and
        // replaced the current child, and we cannot remove this child.
        if (_keepAliveBucket.GetValueOrDefault(childParentData.Vicinity) == child)
        {
            _keepAliveBucket.Remove(childParentData.Vicinity);
        }

        Debug.Assert(DebugForgetDanglingKeepAlive(child));
        // If there is an existing child in the new slot, that child will be moved to another index.
        // In other cases the existing child should have been removed by RemoveChild, so it is fine
        // to overwrite it.
        Debug.Assert(DebugTrackDanglingKeepAlive(childParentData.Vicinity));
        _keepAliveBucket[childParentData.Vicinity] = child;
    }

    internal void RemoveChild(RenderBox child, ChildVicinity slot)
    {
        TwoDimensionalViewportParentData childParentData = ParentDataOf(child);
        if (!childParentData.KeptAlive)
        {
            if (_children.GetValueOrDefault(slot) == child)
            {
                _children.Remove(slot);
            }

            Debug.Assert(DebugTrackOrphans(noLongerOrphan: child));
            if (_keepAliveBucket.GetValueOrDefault(childParentData.Vicinity) == child)
            {
                _keepAliveBucket.Remove(childParentData.Vicinity);
            }

            Debug.Assert(_keepAliveBucket.GetValueOrDefault(childParentData.Vicinity) != child);
            DropChild(child);
            return;
        }

        Debug.Assert(_keepAliveBucket.GetValueOrDefault(childParentData.Vicinity) == child);
        Debug.Assert(DebugForgetDanglingKeepAlive(child));
        _keepAliveBucket.Remove(childParentData.Vicinity);
        DropChild(child);
    }

    private bool DebugTrackOrphans(RenderBox? newOrphan = null, RenderBox? noLongerOrphan = null)
    {
        _debugOrphans ??= [];
        if (newOrphan != null)
        {
            _debugOrphans.Add(newOrphan);
        }

        if (noLongerOrphan != null)
        {
            _debugOrphans.Remove(noLongerOrphan);
        }

        return true;
    }

    private bool DebugForgetDanglingKeepAlive(RenderBox child)
    {
        _debugDanglingKeepAlives.Remove(child);
        return true;
    }

    private bool DebugTrackDanglingKeepAlive(ChildVicinity vicinity)
    {
        if (_keepAliveBucket.TryGetValue(vicinity, out RenderBox? existing))
        {
            _debugDanglingKeepAlives.Add(existing);
        }

        return true;
    }

    /// <summary>
    /// Throws unless the pipeline is measuring intrinsics: instantiating every child of a viewport
    /// to answer an intrinsic query would defeat the point of the viewport being lazy.
    /// </summary>
    protected bool DebugThrowIfNotCheckingIntrinsics()
    {
        if (!DebugCheckingIntrinsics)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"{GetType().Name} does not support returning intrinsic dimensions."),
                new ErrorDescription(
                    "Calculating the intrinsic dimensions would require instantiating every child of "
                    + "the viewport, which defeats the point of viewports being lazy."),
            ]);
        }

        return true;
    }

    /// <inheritdoc />
    protected override double ComputeMinIntrinsicWidth(double height)
    {
        Debug.Assert(DebugThrowIfNotCheckingIntrinsics());
        return 0.0;
    }

    /// <inheritdoc />
    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        Debug.Assert(DebugThrowIfNotCheckingIntrinsics());
        return 0.0;
    }

    /// <inheritdoc />
    protected override double ComputeMinIntrinsicHeight(double width)
    {
        Debug.Assert(DebugThrowIfNotCheckingIntrinsics());
        return 0.0;
    }

    /// <inheritdoc />
    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        Debug.Assert(DebugThrowIfNotCheckingIntrinsics());
        return 0.0;
    }

    /// <inheritdoc />
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        Point paintOffset = ParentDataOf((RenderBox)child).PaintOffset!.Value;
        transform.TranslateByDouble(paintOffset.X, paintOffset.Y, 0, 1);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }
}
