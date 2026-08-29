using System.Diagnostics;
using Avalonia;
using Plumix.UI;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart (_RenderTheater)

internal sealed class OverlayTheaterParentData : StackParentData
{
    public bool CanSizeOverlay { get; set; }

    public bool IsOnstage { get; set; } = true;

    public bool IsPortal { get; set; }

    public RenderBox? PortalAnchor { get; set; }

    public long PortalZOrder { get; set; }
}

internal sealed class RenderOverlayTheater : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData> _container;
    private Alignment _alignment;
    private Clip _clipBehavior;
    private bool _alwaysSizeToContent;
    private bool _hasVisualOverflow;
    private bool _layingOutSizeDeterminingChild;

    /// <summary>
    /// Adding or removing a deferred child does not affect the layout of the other children or of the
    /// overlay itself, so <see cref="MarkNeedsLayout"/> is suppressed while one is in flight.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>_RenderTheater._outstandingDeferredChildUpdateCalls</c>. It is a counter rather
    /// than a flag because <c>attach</c>/<c>detach</c> can nest inside a theater tree walk.
    /// </remarks>
    private int _outstandingDeferredChildUpdateCalls;

    public RenderOverlayTheater(
        Alignment alignment,
        Clip clipBehavior,
        bool alwaysSizeToContent)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData>(this);
        _alignment = alignment;
        _clipBehavior = clipBehavior;
        _alwaysSizeToContent = alwaysSizeToContent;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

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

    public bool AlwaysSizeToContent
    {
        get => _alwaysSizeToContent;
        set
        {
            if (_alwaysSizeToContent == value)
            {
                return;
            }

            _alwaysSizeToContent = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not OverlayTheaterParentData)
        {
            child.parentData = new OverlayTheaterParentData();
        }
    }

    /// <remarks>Flutter's <c>_RenderTheater.performLayout</c>.</remarks>
    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        RenderBox? sizeDeterminingChild = null;
        bool finiteBiggest = double.IsFinite(constraints.MaxWidth)
                             && double.IsFinite(constraints.MaxHeight);

        if (!_alwaysSizeToContent && finiteBiggest)
        {
            Size = constraints.Biggest;
        }
        else
        {
            sizeDeterminingChild = FindSizeDeterminingChild();
            _layingOutSizeDeterminingChild = true;
            sizeDeterminingChild.Layout(constraints, parentUsesSize: true);
            _layingOutSizeDeterminingChild = false;
            Size = constraints.Constrain(sizeDeterminingChild.Size);
        }

        bool hadVisualOverflow = _hasVisualOverflow;
        _hasVisualOverflow = false;
        BoxConstraints nonPositionedConstraints = BoxConstraints.Tight(Size);

        // Deferred children (the render subtrees of the `OverlayPortal`s hosted by the entries) are
        // interleaved with the entries here, exactly as they are painted. Laying one out only resizes
        // it - `RenderDeferredLayoutBox.Layout` defers the real work to the pipeline owner so it runs
        // after both the theater and the portal's own layout surrogate are done.
        foreach (RenderBox child in ChildrenInPaintOrder())
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (!ReferenceEquals(child, sizeDeterminingChild))
            {
                LayoutTheaterChild(child, nonPositionedConstraints, Size, _alignment);
            }
            else
            {
                parentData.offset = _alignment.AlongOffset(Size, child.Size);
            }

            _hasVisualOverflow |= ChildOverflows(child, parentData.offset);
        }

        if (hadVisualOverflow != _hasVisualOverflow)
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>_RenderTheaterMixin.layoutChild</c>. Static because the deferred layout
    /// box and the overlay-child layout builder mix the same body in over their own single child.
    /// </remarks>
    internal static void LayoutTheaterChild(
        RenderBox child,
        BoxConstraints nonPositionedChildConstraints,
        Size hostSize,
        Alignment alignment)
    {
        var parentData = (StackParentData)child.parentData!;
        if (!parentData.IsPositioned)
        {
            child.Layout(nonPositionedChildConstraints, parentUsesSize: true);
            parentData.offset = alignment.AlongOffset(hostSize, child.Size);
        }
        else
        {
            Debug.Assert(
                child is not RenderDeferredLayoutBox,
                "all RenderDeferredLayoutBoxes must be non-positioned children.");
            LayoutPositionedChild(child, parentData, hostSize, alignment);
        }
    }

    /// <summary>Whether the theater is currently laying out the child that determines its size.</summary>
    /// <remarks>Flutter's <c>_RenderTheater._layingOutSizeDeterminingChild</c>.</remarks>
    internal bool LayingOutSizeDeterminingChild => _layingOutSizeDeterminingChild;

    /// <remarks>Flutter's <c>_RenderTheater._resolvedAlignment</c>.</remarks>
    internal Alignment ResolvedAlignment => _alignment;

    /// <remarks>Flutter's <c>_RenderTheater.markNeedsLayout</c>.</remarks>
    public override void MarkNeedsLayout()
    {
        if (_outstandingDeferredChildUpdateCalls == 0)
        {
            base.MarkNeedsLayout();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_clipBehavior == Clip.None)
        {
            PaintOnstageChildren(context, offset);
            return;
        }

        context.PushClipRect(
            new Rect(offset, Size),
            clippedContext => PaintOnstageChildren(clippedContext, offset));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        foreach (RenderBox child in ChildrenInHitTestOrder())
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            RenderBox localChild = child;
            bool isHit = result.AddWithPaintOffset(
                parentData.offset,
                position,
                (hitResult, transformed) => localChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _clipBehavior == Clip.None
            ? null
            : new Rect(new Point(), Size);
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return DescribeApproximatePaintClip(child);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        foreach (RenderBox child in ChildrenInPaintOrder())
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (!parentData.IsPortal)
            {
                visitor(child);
            }
        }
    }

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);

    public void RemoveAll() => _container.RemoveAll();

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext context, Point offset)
    {
        _container.DefaultPaint(context, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    /// <remarks>
    /// Flutter's <c>_RenderTheater._addDeferredChild</c> together with
    /// <c>_OverlayEntryLocation._addToChildModel</c>: the theater adopts the box without dirtying its
    /// own layout, and the layout surrogate is dirtied instead so the box is laid out in this frame.
    /// </remarks>
    internal void AddDeferredChild(
        RenderDeferredLayoutBox child,
        RenderBox anchor,
        long zOrder)
    {
        if (!ReferenceEquals(anchor.Parent, this))
        {
            throw new InvalidOperationException("An OverlayPortal anchor must belong to the target Overlay.");
        }

        _outstandingDeferredChildUpdateCalls += 1;
        Insert(child, after: anchor);
        UpdatePortalParentData(child, anchor, zOrder);

        // The overlay still needs repainting when a deferred child is added: `MarkNeedsLayout` usually
        // implies `MarkNeedsPaint`, but here it is suppressed.
        MarkNeedsPaint();
        _outstandingDeferredChildUpdateCalls -= 1;
        Debug.Assert(_outstandingDeferredChildUpdateCalls >= 0);

        child.LayoutSurrogate.MarkNeedsLayout();
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._moveChild</c>.</remarks>
    internal void MoveDeferredChild(
        RenderDeferredLayoutBox child,
        RenderOverlayTheater oldTheater,
        RenderBox anchor,
        long zOrder)
    {
        if (!ReferenceEquals(oldTheater, this))
        {
            oldTheater.RemoveDeferredChild(child);
            AddDeferredChild(child, anchor, zOrder);
            return;
        }

        _outstandingDeferredChildUpdateCalls += 1;
        UpdatePortalParentData(child, anchor, zOrder);
        MarkNeedsPaint();
        _outstandingDeferredChildUpdateCalls -= 1;
        Debug.Assert(_outstandingDeferredChildUpdateCalls >= 0);
    }

    /// <remarks>Flutter's <c>_RenderTheater._removeDeferredChild</c>.</remarks>
    internal void RemoveDeferredChild(RenderDeferredLayoutBox child)
    {
        _outstandingDeferredChildUpdateCalls += 1;
        Remove(child);
        MarkNeedsPaint();
        _outstandingDeferredChildUpdateCalls -= 1;
        Debug.Assert(_outstandingDeferredChildUpdateCalls >= 0);
    }

    private RenderBox FindSizeDeterminingChild()
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (parentData.IsOnstage
                && !parentData.IsPortal
                && parentData.CanSizeOverlay
                && !parentData.IsPositioned)
            {
                return child;
            }
        }

        string reason = _alwaysSizeToContent
            ? "Overlay.AlwaysSizeToContent requires a non-positioned onstage entry with CanSizeOverlay=true."
            : "An unbounded Overlay requires a non-positioned onstage entry with CanSizeOverlay=true.";
        throw new InvalidOperationException(reason);
    }

    private void PaintOnstageChildren(PaintingContext context, Point offset)
    {
        foreach (RenderBox child in ChildrenInPaintOrder())
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            context.PaintChild(child, parentData.offset + offset);
        }
    }

    private IEnumerable<RenderBox> ChildrenInPaintOrder()
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (!parentData.IsOnstage || parentData.IsPortal)
            {
                continue;
            }

            yield return child;
            foreach (RenderBox portal in PortalChildrenForAnchor(child))
            {
                yield return portal;
            }
        }
    }

    private IEnumerable<RenderBox> ChildrenInHitTestOrder()
    {
        return ChildrenInPaintOrder().Reverse();
    }

    private IEnumerable<RenderBox> PortalChildrenForAnchor(RenderBox anchor)
    {
        return EnumerateChildren()
            .Where(child =>
            {
                var parentData = (OverlayTheaterParentData)child.parentData!;
                return parentData.IsPortal
                       && ReferenceEquals(parentData.PortalAnchor, anchor)
                       && IsPortalAnchorOnstage(parentData);
            })
            .OrderBy(child => ((OverlayTheaterParentData)child.parentData!).PortalZOrder);
    }

    private IEnumerable<RenderBox> EnumerateChildren()
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            yield return child;
        }
    }

    private static bool IsPortalAnchorOnstage(OverlayTheaterParentData parentData)
    {
        return parentData.PortalAnchor?.parentData is OverlayTheaterParentData
        {
            IsOnstage: true,
            IsPortal: false,
        };
    }

    private void UpdatePortalParentData(
        RenderBox child,
        RenderBox anchor,
        long zOrder)
    {
        var parentData = (OverlayTheaterParentData)child.parentData!;
        parentData.IsPortal = true;
        parentData.IsOnstage = true;
        parentData.CanSizeOverlay = false;
        parentData.PortalAnchor = anchor;
        parentData.PortalZOrder = zOrder;

        // Flutter's `_OverlayEntryLocation._addToChildModel` repaints, recomputes compositing bits and
        // dirties semantics, but deliberately never dirties the theater's layout.
        MarkNeedsPaint();
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsSemanticsUpdate();
    }

    private static void LayoutPositionedChild(
        RenderBox child,
        StackParentData parentData,
        Size hostSize,
        Alignment alignment)
    {
        double? childWidth = ComputeChildExtent(
            parentData.Left,
            parentData.Right,
            parentData.Width,
            hostSize.Width);
        double? childHeight = ComputeChildExtent(
            parentData.Top,
            parentData.Bottom,
            parentData.Height,
            hostSize.Height);
        var childConstraints = new BoxConstraints(
            MinWidth: childWidth ?? 0.0,
            MaxWidth: childWidth ?? hostSize.Width,
            MinHeight: childHeight ?? 0.0,
            MaxHeight: childHeight ?? hostSize.Height);
        child.Layout(childConstraints, parentUsesSize: true);

        Point alignedOffset = alignment.AlongOffset(hostSize, child.Size);
        double x = parentData.Left
                   ?? (parentData.Right.HasValue
                       ? hostSize.Width - parentData.Right.Value - child.Size.Width
                       : alignedOffset.X);
        double y = parentData.Top
                   ?? (parentData.Bottom.HasValue
                       ? hostSize.Height - parentData.Bottom.Value - child.Size.Height
                       : alignedOffset.Y);
        parentData.offset = new Point(x, y);
    }

    private static double? ComputeChildExtent(
        double? leading,
        double? trailing,
        double? extent,
        double availableExtent)
    {
        if (leading.HasValue && trailing.HasValue)
        {
            return Math.Max(0.0, availableExtent - leading.Value - trailing.Value);
        }

        return extent.HasValue ? Math.Max(0.0, extent.Value) : null;
    }

    private bool ChildOverflows(RenderBox child, Point offset)
    {
        return offset.X < 0.0
               || offset.Y < 0.0
               || offset.X + child.Size.Width > Size.Width
               || offset.Y + child.Size.Height > Size.Height;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}

/// <summary>
/// The overlay child's own render object: a relayout boundary under the theater whose real layout is
/// deferred until both the theater and the <see cref="RenderOverlayPortalSurrogate"/> are laid out.
/// </summary>
/// <remarks>
/// Flutter's <c>_RenderDeferredLayoutBox</c>. It guarantees that it only lays out after the sizes of
/// the render objects from its layout surrogate (a descendant of the theater) up through the theater
/// are known. To that end it is a relayout boundary, adding it to the theater never dirties the
/// theater, and <see cref="Layout"/> is overridden so a tree walk only resizes it and re-enqueues it
/// on the pipeline owner instead of laying its subtree out prematurely. When the pipeline owner
/// reaches it, it behaves like an overlay with a single entry.
/// </remarks>
internal sealed class RenderDeferredLayoutBox : RenderProxyBox
{
    private readonly RenderOverlayPortalSurrogate _layoutSurrogate;
    private bool _doingLayoutFromTreeWalk;

    /// <remarks>
    /// Flutter's <c>_RenderDeferredLayoutBox._needsLayout</c> deliberately shadows
    /// <c>RenderObject._needsLayout</c>; C# has no field shadowing across a private field, so this is
    /// the separately named copy the tree-walk protocol reads.
    /// </remarks>
    private bool _deferredNeedsLayout = true;

    public RenderDeferredLayoutBox(RenderOverlayPortalSurrogate layoutSurrogate)
    {
        _layoutSurrogate = layoutSurrogate;
    }

    internal RenderOverlayPortalSurrogate LayoutSurrogate => _layoutSurrogate;

    internal RenderOverlayTheater Theater => Parent as RenderOverlayTheater
        ?? throw new InvalidOperationException(
            $"The parent of this {nameof(RenderDeferredLayoutBox)} is not a {nameof(RenderOverlayTheater)}.");

    protected override bool SizedByParent => true;

    /// <remarks>
    /// Flutter's <c>_RenderTheaterMixin.setupParentData</c>: the overlay child may use
    /// <c>Positioned</c>, so its parent data has to carry the stack slots.
    /// </remarks>
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not StackParentData)
        {
            child.parentData = new StackParentData();
        }
    }

    /// <remarks>
    /// Flutter's <c>_RenderDeferredLayoutBox.debugLayoutParent</c>: mutation permissions are checked
    /// against the layout surrogate, not the theater, because that is the node that lays this one out.
    /// </remarks>
    protected override RenderObject? DebugLayoutParent =>
        Constants.KDebugMode ? _layoutSurrogate : null;

    protected override void PerformResize()
    {
        Size = Constraints.Biggest;
    }

    /// <remarks>
    /// Flutter's <c>_RenderDeferredLayoutBox.redepthChildren</c>. The surrogate can be adopted after
    /// this box enters the theater; until then it cannot redepth this child, and its own
    /// <c>RedepthChildren</c> restores the invariant once it is adopted. Dart spells the guard
    /// <c>_layoutSurrogate.attached</c>; what <c>redepthChild</c> actually requires is that the two
    /// share an owner, and Plumix builds whole render subtrees before attaching them, so the guard is
    /// the owner comparison Dart's version stands in for.
    /// </remarks>
    protected override void RedepthChildren()
    {
        if (ReferenceEquals(_layoutSurrogate.Owner, Owner))
        {
            _layoutSurrogate.RedepthDeferredChild(this);
        }

        base.RedepthChildren();
    }

    public override void MarkNeedsLayout()
    {
        _deferredNeedsLayout = true;
        base.MarkNeedsLayout();
    }

    /// <remarks>
    /// Flutter's <c>_RenderDeferredLayoutBox.layout</c>. <c>parentUsesSize</c> is ignored because this
    /// box is sized by its parent.
    /// </remarks>
    public override void Layout(BoxConstraints constraints, bool parentUsesSize = false)
    {
        DoLayoutFrom(Parent!, constraints);
    }

    /// <remarks>Flutter's <c>_RenderDeferredLayoutBox._doLayoutFrom</c>.</remarks>
    internal void DoLayoutFrom(RenderObject treeWalkParent, BoxConstraints constraints)
    {
        bool shouldAddToDirtyList = _deferredNeedsLayout
                                    || !HasBoxConstraints
                                    || !CurrentBoxConstraints.Equals(constraints);
        Debug.Assert(!_doingLayoutFromTreeWalk);
        _doingLayoutFromTreeWalk = true;
        base.Layout(constraints);
        _doingLayoutFromTreeWalk = false;
        _deferredNeedsLayout = false;

        if (!shouldAddToDirtyList)
        {
            return;
        }

        // Rather than laying this subtree out through the tree walk, put it on the pipeline owner's
        // dirty list. That way (1) it is laid out after the two nodes it depends on - the theater and
        // the layout surrogate - because its depth is greater than theirs, and (2) by the time its
        // child lays out, every node from the surrogate up to the theater has finished laying out, so
        // the child can read their sizes and compute the portal's paint transform inside the overlay.
        // Going through a layout callback lets the node merge back into the dirty list in the right
        // order when it is not already dirty, so the subtree is never laid out twice.
        treeWalkParent.InvokeLayoutCallbackOnTreeWalkParent(MarkNeedsLayout);
    }

    protected override void PerformLayout()
    {
        if (_doingLayoutFromTreeWalk)
        {
            _deferredNeedsLayout = false;
            return;
        }

        // Reached either from `PipelineOwner.FlushLayout` or from the layout surrogate's PerformLayout.
        Debug.Assert(Parent is not null);
        RenderBox? child = Child;
        if (child is null)
        {
            _deferredNeedsLayout = false;
            return;
        }

        Debug.Assert(Constraints.IsTight);
        RenderOverlayTheater.LayoutTheaterChild(child, Constraints, Size, Theater.ResolvedAlignment);
        _deferredNeedsLayout = false;
    }
}

/// <summary>
/// The overlay child of an <c>OverlayPortal.WithLayoutBuilder</c>: it rebuilds during its own layout
/// with the anchor geometry the theater and the layout surrogate have just produced.
/// </summary>
/// <remarks>
/// Flutter's <c>_RenderLayoutBuilder</c> in <c>overlay.dart</c> (not the one in
/// <c>layout_builder.dart</c>). It has the same size and paint transform as its parent and its
/// theater, it is a relayout boundary marked dirty every frame - through a transient frame callback
/// that does not schedule a frame of its own - and it runs a layout callback in
/// <see cref="PerformLayout"/>.
/// </remarks>
internal sealed class RenderOverlayPortalLayoutBuilder : RenderProxyBox, IRenderObjectWithLayoutCallback
{
    private Action<OverlayChildLayoutInfo>? _callback;
    private OverlayChildLayoutInfo? _layoutInfo;
    private int? _callbackId;

    internal RenderOverlayTheater Theater => (Parent as RenderDeferredLayoutBox)?.Theater
        ?? throw new InvalidOperationException(
            $"The parent of this {nameof(RenderOverlayPortalLayoutBuilder)} is not a "
            + $"{nameof(RenderDeferredLayoutBox)}.");

    /// <remarks>Flutter's <c>RenderAbstractLayoutBuilderMixin.layoutInfo</c>.</remarks>
    internal OverlayChildLayoutInfo LayoutInfo => _layoutInfo
        ?? throw new InvalidOperationException(
            "OverlayPortal layout information is only available while the layout callback runs.");

    protected override bool SizedByParent => true;

    internal void UpdateCallback(Action<OverlayChildLayoutInfo> callback)
    {
        if (_callback == callback)
        {
            return;
        }

        _callback = callback;
        ScheduleLayoutCallback();
    }

    /// <remarks>Flutter's <c>_LayoutBuilderElement.unmount</c> assigns <c>_callback = null</c> directly,
    /// without scheduling another callback run.</remarks>
    internal void ClearCallback() => _callback = null;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not StackParentData)
        {
            child.parentData = new StackParentData();
        }
    }

    protected override void PerformResize()
    {
        Size = Constraints.Biggest;
    }

    void IRenderObjectWithLayoutCallback.LayoutCallback()
    {
        _layoutInfo = ComputeNewLayoutInfo();
        _callback!(_layoutInfo);
    }

    protected override void PerformLayout()
    {
        RunLayoutCallback();
        if (Child is { } child)
        {
            RenderOverlayTheater.LayoutTheaterChild(child, Constraints, Size, Theater.ResolvedAlignment);
        }

        // Dart asserts `_callbackId == null` here, because a Flutter frame always runs the transient
        // callback before the next layout. Plumix's widget-test harnesses flush layout without running a
        // frame at all, so a still-pending id is reused instead of asserted on.
        _callbackId ??= Scheduler.ScheduleFrameCallback(HandleFrameCallback, rescheduling: true);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_callbackId is { } callbackId)
        {
            Scheduler.CancelFrameCallbackWithId(callbackId);
            _callbackId = null;
        }

        base.Dispose();
    }

    private void HandleFrameCallback(TimeSpan timeStamp)
    {
        _callbackId = null;
        MarkNeedsLayout();
    }

    /// <remarks>Flutter's <c>_RenderLayoutBuilder._computeNewLayoutInfo</c>.</remarks>
    private OverlayChildLayoutInfo ComputeNewLayoutInfo()
    {
        var parent = (RenderDeferredLayoutBox)Parent!;
        RenderOverlayPortalSurrogate layoutSurrogate = parent.LayoutSurrogate;
        RenderOverlayTheater theater = parent.Theater;
        Debug.Assert(layoutSurrogate.HasSize);
        return new OverlayChildLayoutInfo(
            layoutSurrogate.Size,
            layoutSurrogate.GetTransformTo(theater),
            Size);
    }
}
