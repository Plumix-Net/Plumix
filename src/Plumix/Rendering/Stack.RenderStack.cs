using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using System.Diagnostics;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/stack.dart

namespace Plumix.Rendering;

public enum StackFit
{
    Loose,
    Expand,
    Passthrough
}

public class StackParentData : ContainerBoxParentData<RenderBox>
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Right { get; set; }
    public double? Bottom { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    public RelativeRect Rect
    {
        get => new(Left!.Value, Top!.Value, Right!.Value, Bottom!.Value);
        set
        {
            Top = value.Top;
            Right = value.Right;
            Bottom = value.Bottom;
            Left = value.Left;
        }
    }

    public bool IsPositioned =>
        Left.HasValue
        || Top.HasValue
        || Right.HasValue
        || Bottom.HasValue
        || Width.HasValue
        || Height.HasValue;

    public BoxConstraints PositionedChildConstraints(Size stackSize)
    {
        double? width = Left.HasValue && Right.HasValue
            ? stackSize.Width - Right.Value - Left.Value
            : Width;
        double? height = Top.HasValue && Bottom.HasValue
            ? stackSize.Height - Bottom.Value - Top.Value
            : Height;

        Debug.Assert(!width.HasValue || !double.IsNaN(width.Value));
        Debug.Assert(!height.HasValue || !double.IsNaN(height.Value));
        return BoxConstraints.TightFor(
            width: width.HasValue ? Math.Max(0.0, width.Value) : null,
            height: height.HasValue ? Math.Max(0.0, height.Value) : null);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var values = new List<string>();
        if (Top is not null)
        {
            values.Add($"top={DoubleProperty.FormatDouble(Top)}");
        }

        if (Right is not null)
        {
            values.Add($"right={DoubleProperty.FormatDouble(Right)}");
        }

        if (Bottom is not null)
        {
            values.Add($"bottom={DoubleProperty.FormatDouble(Bottom)}");
        }

        if (Left is not null)
        {
            values.Add($"left={DoubleProperty.FormatDouble(Left)}");
        }

        if (Width is not null)
        {
            values.Add($"width={DoubleProperty.FormatDouble(Width)}");
        }

        if (Height is not null)
        {
            values.Add($"height={DoubleProperty.FormatDouble(Height)}");
        }

        if (values.Count == 0)
        {
            values.Add("not positioned");
        }

        values.Add(base.ToString());
        return string.Join("; ", values);
    }
}

public class RenderStack : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, StackParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, StackParentData> _container;
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;
    private StackFit _fit;
    private Clip _clipBehavior;
    private bool _hasVisualOverflow;
    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    public RenderStack(
        List<RenderBox>? children = null,
        AlignmentGeometry? alignment = null,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge,
        TextDirection? textDirection = null)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, StackParentData>(this);
        _alignment = alignment ?? AlignmentDirectional.TopStart;
        _textDirection = textDirection;
        _fit = fit;
        _clipBehavior = clipBehavior;
        AddAll(children);
    }

    public AlignmentGeometry Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

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
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedResolution();
        }
    }

    protected Alignment ResolvedAlignment => _resolvedAlignment ??= _alignment.Resolve(_textDirection);

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsLayout();
    }

    public StackFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
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

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);
    public void RemoveAll() => _container.RemoveAll();
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not StackParentData)
        {
            child.parentData = new StackParentData();
        }
    }

    protected static double GetIntrinsicDimension(
        RenderStack stack,
        Func<RenderBox, double> mainChildSizeGetter)
    {
        double extent = 0.0;
        for (RenderBox? child = stack.FirstChild; child != null; child = stack.ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            if (!childParentData.IsPositioned)
            {
                extent = Math.Max(extent, mainChildSizeGetter(child));
            }
        }

        return extent;
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        GetIntrinsicDimension(this, child => child.GetMinIntrinsicWidth(height));

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        GetIntrinsicDimension(this, child => child.GetMaxIntrinsicWidth(height));

    protected override double ComputeMinIntrinsicHeight(double width) =>
        GetIntrinsicDimension(this, child => child.GetMinIntrinsicHeight(width));

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        GetIntrinsicDimension(this, child => child.GetMaxIntrinsicHeight(width));

    protected BoxConstraints NonPositionedConstraintsFor(BoxConstraints constraints) => Fit switch
    {
        StackFit.Loose => constraints.Loosen(),
        StackFit.Expand => BoxConstraints.Tight(constraints.Biggest),
        StackFit.Passthrough => constraints,
        _ => throw new ArgumentOutOfRangeException(),
    };

    protected Size ComputeSize(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild)
    {
        if (ChildCount == 0)
        {
            return double.IsFinite(constraints.MaxWidth) && double.IsFinite(constraints.MaxHeight)
                ? constraints.Biggest
                : constraints.Smallest;
        }

        bool hasNonPositionedChildren = false;
        double width = constraints.MinWidth;
        double height = constraints.MinHeight;
        BoxConstraints nonPositionedConstraints = NonPositionedConstraintsFor(constraints);
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            if (childParentData.IsPositioned)
            {
                continue;
            }

            hasNonPositionedChildren = true;
            Size childSize = layoutChild(child, nonPositionedConstraints);
            width = Math.Max(width, childSize.Width);
            height = Math.Max(height, childSize.Height);
        }

        Size size = hasNonPositionedChildren ? new Size(width, height) : constraints.Biggest;
        if (Constants.KDebugMode && (!double.IsFinite(size.Width) || !double.IsFinite(size.Height)))
        {
            throw new AssertionError("A RenderStack requires finite constraints when it has only positioned children.");
        }

        return size;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
        _container.DefaultComputeDistanceToHighestActualBaseline(baseline);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        BoxConstraints nonPositionedConstraints = NonPositionedConstraintsFor(constraints);
        Alignment resolvedAlignment = ResolvedAlignment;
        Size stackSize = ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild);
        double? result = null;
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            double? childBaseline = BaselineForChild(
                child,
                nonPositionedConstraints,
                stackSize,
                resolvedAlignment,
                baseline);
            if (childBaseline.HasValue)
            {
                result = result.HasValue
                    ? Math.Min(result.Value, childBaseline.Value)
                    : childBaseline;
            }
        }

        return result;
    }

    protected static double? BaselineForChild(
        RenderBox child,
        BoxConstraints nonPositionedConstraints,
        Size stackSize,
        Alignment resolvedAlignment,
        TextBaseline baseline)
    {
        var childParentData = (StackParentData)child.parentData!;
        BoxConstraints childConstraints = childParentData.IsPositioned
            ? childParentData.PositionedChildConstraints(stackSize)
            : nonPositionedConstraints;
        double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
        if (!childBaseline.HasValue)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        double y = childParentData.Top
                   ?? (childParentData.Bottom.HasValue
                       ? stackSize.Height - childParentData.Bottom.Value - childSize.Height
                       : resolvedAlignment.AlongOffset(stackSize, childSize).Y);
        return childBaseline.Value + y;
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        bool hadVisualOverflow = _hasVisualOverflow;
        _hasVisualOverflow = false;
        Size = ComputeSize(constraints, ChildLayoutHelper.LayoutChild);
        Alignment resolvedAlignment = ResolvedAlignment;

        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            if (!childParentData.IsPositioned)
            {
                childParentData.offset = resolvedAlignment.AlongOffset(Size, child.Size);
                continue;
            }

            _hasVisualOverflow |= LayoutPositionedChild(child, childParentData, Size, resolvedAlignment);
        }

        if (hadVisualOverflow != _hasVisualOverflow)
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    protected static bool LayoutPositionedChild(
        RenderBox child,
        StackParentData childParentData,
        Size size,
        Alignment alignment)
    {
        BoxConstraints childConstraints = childParentData.PositionedChildConstraints(size);
        child.Layout(childConstraints, parentUsesSize: true);

        Point alignedOffset = alignment.AlongOffset(size, child.Size);
        double x = childParentData.Left
                   ?? (childParentData.Right.HasValue
                       ? size.Width - childParentData.Right.Value - child.Size.Width
                       : alignedOffset.X);
        double y = childParentData.Top
                   ?? (childParentData.Bottom.HasValue
                       ? size.Height - childParentData.Bottom.Value - child.Size.Height
                       : alignedOffset.Y);
        childParentData.offset = new Point(x, y);
        return x < 0.0
               || x + child.Size.Width > size.Width
               || y < 0.0
               || y + child.Size.Height > size.Height;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_hasVisualOverflow && ClipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(), Size),
                PaintStack,
                ClipBehavior,
                _clipRectLayer.Layer);
            return;
        }

        _clipRectLayer.Layer = null;
        PaintStack(context, offset);
    }

    protected virtual void PaintStack(PaintingContext context, Point offset) => DefaultPaint(context, offset);

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        DefaultHitTestChildren(result, position);

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child) =>
        _hasVisualOverflow && ClipBehavior != Clip.None ? new Rect(new Point(), Size) : null;

    protected override Rect? DescribeSemanticsClip(RenderObject? child) => DescribeApproximatePaintClip(child);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new EnumProperty<StackFit>("fit", Fit));
        properties.Add(new EnumProperty<Clip>("clipBehavior", ClipBehavior, defaultValue: Clip.HardEdge));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}
