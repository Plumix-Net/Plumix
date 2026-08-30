using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/stack.dart (RenderIndexedStack subset)

public sealed class IndexedStackParentData : ContainerBoxParentData<RenderBox>
{
}

public sealed class RenderIndexedStack : RenderBox, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, IndexedStackParentData> _container;
    private int? _index;
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;
    private StackFit _fit;
    private Clip _clipBehavior;
    private bool _hasVisualOverflow;
    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    public RenderIndexedStack(
        int? index = 0,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, IndexedStackParentData>(this);
        _index = index;
        _alignment = alignment;
        _textDirection = textDirection;
        _fit = fit;
        _clipBehavior = clipBehavior;
    }

    /// <summary>How to size the children. Defaults to <see cref="StackFit.Loose"/>.</summary>
    public StackFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value) return;
            _fit = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>How to clip an overflowing child. Defaults to <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value) return;
            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public int? Index
    {
        get => _index;
        set
        {
            if (_index == value) return;
            _index = value;
            MarkNeedsLayout();
        }
    }

    public AlignmentGeometry Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value) return;
            _alignment = value;
            MarkNeedResolution();
        }
    }

    /// <summary>The text direction with which <see cref="Alignment"/> is resolved.</summary>
    public TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value) return;
            _textDirection = value;
            MarkNeedResolution();
        }
    }

    private Alignment ResolvedAlignment => _resolvedAlignment ??= _alignment.Resolve(_textDirection);

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsLayout();
    }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not IndexedStackParentData)
        {
            child.parentData = new IndexedStackParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        GetIntrinsicDimension(child => child.GetMinIntrinsicWidth(height));

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        GetIntrinsicDimension(child => child.GetMaxIntrinsicWidth(height));

    protected override double ComputeMinIntrinsicHeight(double width) =>
        GetIntrinsicDimension(child => child.GetMinIntrinsicHeight(width));

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        GetIntrinsicDimension(child => child.GetMaxIntrinsicHeight(width));

    private BoxConstraints ChildConstraintsFor(BoxConstraints constraints) => _fit switch
    {
        StackFit.Loose => BoxConstraints.Loose(constraints.Biggest),
        StackFit.Expand => BoxConstraints.Tight(constraints.Biggest),
        StackFit.Passthrough => constraints,
        _ => constraints,
    };

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        BoxConstraints childConstraints = ChildConstraintsFor(constraints);
        double maxWidth = constraints.MinWidth;
        double maxHeight = constraints.MinHeight;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Size childSize = child.GetDryLayout(childConstraints);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        return constraints.Constrain(new Size(maxWidth, maxHeight));
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        RenderBox? child = SelectedChild;
        if (child is null)
        {
            return null;
        }

        var data = (IndexedStackParentData)child.parentData!;
        double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline.HasValue ? childBaseline.Value + data.offset.Y : null;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = SelectedChild;
        if (child is null)
        {
            return null;
        }

        BoxConstraints childConstraints = ChildConstraintsFor(constraints);
        Size stackSize = ComputeDryLayout(constraints);
        Size childSize = child.GetDryLayout(childConstraints);
        double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
        return childBaseline.HasValue
            ? childBaseline.Value + ResolvedAlignment.AlongOffset(stackSize, childSize).Y
            : null;
    }

    protected override void PerformLayout()
    {
        _hasVisualOverflow = false;
        BoxConstraints childConstraints = ChildConstraintsFor(Constraints);
        double maxWidth = 0.0;
        double maxHeight = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
            maxWidth = Math.Max(maxWidth, child.Size.Width);
            maxHeight = Math.Max(maxHeight, child.Size.Height);
        }

        Size = Constraints.Constrain(new Size(maxWidth, maxHeight));
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Point offset = ResolvedAlignment.AlongOffset(Size, child.Size);
            ((IndexedStackParentData)child.parentData!).offset = offset;
            _hasVisualOverflow |= offset.X < 0.0
                || offset.Y < 0.0
                || offset.X + child.Size.Width > Size.Width
                || offset.Y + child.Size.Height > Size.Height;
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (SelectedChild is null) return;
        if (_hasVisualOverflow && _clipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                PaintSelectedChild,
                _clipBehavior,
                _clipRectLayer.Layer);
            return;
        }

        _clipRectLayer.Layer = null;
        PaintSelectedChild(context, offset);
    }

    private void PaintSelectedChild(PaintingContext context, Point offset)
    {
        if (SelectedChild is not { } child) return;
        var data = (IndexedStackParentData)child.parentData!;
        context.PaintChild(child, data.offset + offset);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child) =>
        _hasVisualOverflow && _clipBehavior != Clip.None ? new Rect(new Point(), Size) : null;

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        var child = SelectedChild;
        if (child is null) return false;
        var data = (IndexedStackParentData)child.parentData!;
        return result.AddWithPaintOffset(
            data.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        var child = SelectedChild;
        if (child is null) return;
        visitor(child);
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private RenderBox? SelectedChild
    {
        get
        {
            if (!_index.HasValue || _index.Value < 0 || _index.Value >= ChildCount) return null;
            var current = FirstChild;
            for (int i = 0; i < _index.Value && current is not null; i++) current = ChildAfter(current);
            return current;
        }
    }

    private double GetIntrinsicDimension(Func<RenderBox, double> getter)
    {
        double extent = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            extent = Math.Max(extent, getter(child));
        }

        return extent;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new EnumProperty<StackFit>("fit", Fit));
        properties.Add(new EnumProperty<Clip>("clipBehavior", ClipBehavior, defaultValue: Clip.HardEdge));
        properties.Add(new IntProperty("index", Index));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        int i = 0;
        RenderBox? child = _container.FirstChild;
        while (child is not null)
        {
            children.Add(child.ToDiagnosticsNode(
                name: $"child {i + 1}",
                style: i != Index ? DiagnosticsTreeStyle.Offstage : null));
            child = ((IndexedStackParentData)child.parentData!).nextSibling;
            i += 1;
        }

        return children;
    }
}
