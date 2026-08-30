using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/shifted_box.dart

namespace Plumix.Rendering;

/// <summary>Abstract class for one-child-layout render boxes that provide control over the child's
/// position.</summary>
public abstract class RenderShiftedBox : RenderBox, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    protected RenderShiftedBox(RenderBox? child)
    {
        Child = child;
    }

    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;

            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        _child?.GetMinIntrinsicWidth(height) ?? 0.0;

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        _child?.GetMaxIntrinsicWidth(height) ?? 0.0;

    protected override double ComputeMinIntrinsicHeight(double width) =>
        _child?.GetMinIntrinsicHeight(width) ?? 0.0;

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        _child?.GetMaxIntrinsicHeight(width) ?? 0.0;

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        RenderBox? child = _child;
        if (child is null)
        {
            return base.ComputeDistanceToActualBaseline(baseline);
        }

        double? result = child.GetDistanceToBaseline(baseline, onlyReal: true);
        var childParentData = (BoxParentData)child.parentData!;
        return result + childParentData.offset.Y;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        // The base class does not apply a transform; subclasses override to add their own offsets.
        return _child?.GetDryBaseline(constraints, baseline);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        RenderBox? child = _child;
        if (child != null)
        {
            var childParentData = (BoxParentData)child.parentData!;
            ctx.PaintChild(child, childParentData.offset + offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = _child;
        if (child == null)
        {
            return false;
        }

        var childParentData = (BoxParentData)child.parentData!;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>Insets its child by the given padding.</summary>
public sealed class RenderPadding : RenderShiftedBox
{
    private EdgeInsetsGeometry _padding;
    private TextDirection? _textDirection;
    private Thickness? _resolvedPaddingCache;

    public RenderPadding(
        EdgeInsetsGeometry padding,
        RenderBox? child = null,
        TextDirection? textDirection = null) : base(child)
    {
        if (Constants.KDebugMode && !padding.IsNonNegative)
        {
            throw new AssertionError("padding must be non-negative.");
        }

        _padding = padding;
        _textDirection = textDirection;
    }

    private Thickness ResolvedPadding => _resolvedPaddingCache ??= _padding.Resolve(_textDirection);

    public EdgeInsetsGeometry Padding
    {
        get => _padding;
        set
        {
            if (Constants.KDebugMode && !value.IsNonNegative)
            {
                throw new AssertionError("padding must be non-negative.");
            }

            if (_padding == value)
            {
                return;
            }

            _padding = value;
            MarkNeedResolution();
        }
    }

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

    private void MarkNeedResolution()
    {
        _resolvedPaddingCache = null;
        MarkNeedsLayout();
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child != null)
        {
            // Relies on double.infinity absorption.
            return Child.GetMinIntrinsicWidth(Math.Max(0.0, height - vertical)) + horizontal;
        }

        return horizontal;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child != null)
        {
            return Child.GetMaxIntrinsicWidth(Math.Max(0.0, height - vertical)) + horizontal;
        }

        return horizontal;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child != null)
        {
            return Child.GetMinIntrinsicHeight(Math.Max(0.0, width - horizontal)) + vertical;
        }

        return vertical;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child != null)
        {
            return Child.GetMaxIntrinsicHeight(Math.Max(0.0, width - horizontal)) + vertical;
        }

        return vertical;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child == null)
        {
            return constraints.Constrain(new Size(horizontal, vertical));
        }

        BoxConstraints innerConstraints = constraints.Deflate(padding);
        Size childSize = Child.GetDryLayout(innerConstraints);
        return constraints.Constrain(new Size(horizontal + childSize.Width, vertical + childSize.Height));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return null;
        }

        Thickness padding = ResolvedPadding;
        double? childBaseline = child.GetDryBaseline(constraints.Deflate(padding), baseline);
        return childBaseline is null ? null : childBaseline + padding.Top;
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        Thickness padding = ResolvedPadding;
        double horizontal = padding.Left + padding.Right;
        double vertical = padding.Top + padding.Bottom;
        if (Child == null)
        {
            Size = constraints.Constrain(new Size(horizontal, vertical));
            return;
        }

        BoxConstraints innerConstraints = constraints.Deflate(padding);
        Child.Layout(innerConstraints, parentUsesSize: true);
        var childParentData = (BoxParentData)Child.parentData!;
        childParentData.offset = new Point(padding.Left, padding.Top);
        Size = constraints.Constrain(
            new Size(horizontal + Child.Size.Width, vertical + Child.Size.Height));
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaintSize(context, offset);
        var outerRect = new Rect(offset, Size);
        RenderingDebug.PaintPadding(
            context,
            outerRect,
            Child is null ? null : outerRect.Deflate(_resolvedPaddingCache!.Value));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry>("padding", Padding));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
    }
}

/// <summary>Abstract class for one-child-layout render boxes that use a
/// <see cref="AlignmentGeometry"/> to align a single child.</summary>
public abstract class RenderAligningShiftedBox : RenderShiftedBox
{
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;

    protected RenderAligningShiftedBox(
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null,
        RenderBox? child = null) : base(child)
    {
        _alignment = alignment;
        _textDirection = textDirection;
    }

    /// <summary>The alignment resolved against <see cref="TextDirection"/>.</summary>
    protected Alignment ResolvedAlignment => _resolvedAlignment ??= _alignment.Resolve(_textDirection);

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

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsLayout();
    }

    /// <summary>Positions the (already laid out) child according to <see cref="Alignment"/>.</summary>
    protected void AlignChild()
    {
        RenderBox child = Child
            ?? throw new AssertionError("AlignChild requires a child.");
        var childParentData = (BoxParentData)child.parentData!;
        childParentData.offset = ResolvedAlignment.AlongOffset(Size, child.Size);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
    }
}

/// <summary>Positions its child using an <see cref="AlignmentGeometry"/>, optionally sizing itself to
/// a multiple of the child's size.</summary>
public class RenderPositionedBox : RenderAligningShiftedBox
{
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderPositionedBox(
        RenderBox? child = null,
        double? widthFactor = null,
        double? heightFactor = null,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null) : base(alignment, textDirection, child)
    {
        AssertFactor(widthFactor, nameof(widthFactor));
        AssertFactor(heightFactor, nameof(heightFactor));
        _widthFactor = widthFactor;
        _heightFactor = heightFactor;
    }

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            AssertFactor(value, nameof(value));
            if (_widthFactor == value)
            {
                return;
            }

            _widthFactor = value;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            AssertFactor(value, nameof(value));
            if (_heightFactor == value)
            {
                return;
            }

            _heightFactor = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        base.ComputeMinIntrinsicWidth(height) * (_widthFactor ?? 1.0);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        base.ComputeMaxIntrinsicWidth(height) * (_widthFactor ?? 1.0);

    protected override double ComputeMinIntrinsicHeight(double width) =>
        base.ComputeMinIntrinsicHeight(width) * (_heightFactor ?? 1.0);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        base.ComputeMaxIntrinsicHeight(width) * (_heightFactor ?? 1.0);

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        bool shrinkWrapWidth = _widthFactor is not null || double.IsPositiveInfinity(constraints.MaxWidth);
        bool shrinkWrapHeight = _heightFactor is not null || double.IsPositiveInfinity(constraints.MaxHeight);
        if (Child is { } child)
        {
            Size childSize = child.GetDryLayout(constraints.Loosen());
            return constraints.Constrain(new Size(
                shrinkWrapWidth ? childSize.Width * (_widthFactor ?? 1.0) : double.PositiveInfinity,
                shrinkWrapHeight ? childSize.Height * (_heightFactor ?? 1.0) : double.PositiveInfinity));
        }

        return constraints.Constrain(new Size(
            shrinkWrapWidth ? 0.0 : double.PositiveInfinity,
            shrinkWrapHeight ? 0.0 : double.PositiveInfinity));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return null;
        }

        BoxConstraints childConstraints = constraints.Loosen();
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result is null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        bool shrinkWrapWidth = _widthFactor is not null || double.IsPositiveInfinity(constraints.MaxWidth);
        bool shrinkWrapHeight = _heightFactor is not null || double.IsPositiveInfinity(constraints.MaxHeight);
        Size size = constraints.Constrain(new Size(
            shrinkWrapWidth ? childSize.Width * (_widthFactor ?? 1.0) : double.PositiveInfinity,
            shrinkWrapHeight ? childSize.Height * (_heightFactor ?? 1.0) : double.PositiveInfinity));
        return result + ResolvedAlignment.AlongOffset(size, childSize).Y;
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        bool shrinkWrapWidth = _widthFactor is not null || double.IsPositiveInfinity(constraints.MaxWidth);
        bool shrinkWrapHeight = _heightFactor is not null || double.IsPositiveInfinity(constraints.MaxHeight);
        if (Child is { } child)
        {
            child.Layout(constraints.Loosen(), parentUsesSize: true);
            Size = constraints.Constrain(new Size(
                shrinkWrapWidth ? child.Size.Width * (_widthFactor ?? 1.0) : double.PositiveInfinity,
                shrinkWrapHeight ? child.Size.Height * (_heightFactor ?? 1.0) : double.PositiveInfinity));
            AlignChild();
        }
        else
        {
            Size = constraints.Constrain(new Size(
                shrinkWrapWidth ? 0.0 : double.PositiveInfinity,
                shrinkWrapHeight ? 0.0 : double.PositiveInfinity));
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaintSize(context, offset);
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (Child is { } child && child.Size.Width != 0.0 && child.Size.Height != 0.0)
        {
            var childParentData = (BoxParentData)child.parentData!;
            var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFFFFFF00)), 1.0);
            var path = new UI.Path();
            bool drew = false;

            if (childParentData.offset.Y > 0.0)
            {
                // Vertical alignment arrows.
                double headSize = Math.Min(childParentData.offset.Y * 0.2, 10.0);
                path.MoveTo(offset.X + (Size.Width / 2.0), offset.Y);
                path.RelativeLineTo(0.0, childParentData.offset.Y - headSize);
                path.RelativeLineTo(headSize, 0.0);
                path.RelativeLineTo(-headSize, headSize);
                path.RelativeLineTo(-headSize, -headSize);
                path.RelativeLineTo(headSize, 0.0);
                path.MoveTo(offset.X + (Size.Width / 2.0), offset.Y + Size.Height);
                path.RelativeLineTo(0.0, -childParentData.offset.Y + headSize);
                path.RelativeLineTo(headSize, 0.0);
                path.RelativeLineTo(-headSize, -headSize);
                path.RelativeLineTo(-headSize, headSize);
                path.RelativeLineTo(headSize, 0.0);
                drew = true;
            }

            if (childParentData.offset.X > 0.0)
            {
                // Horizontal alignment arrows.
                double headSize = Math.Min(childParentData.offset.X * 0.2, 10.0);
                path.MoveTo(offset.X, offset.Y + (Size.Height / 2.0));
                path.RelativeLineTo(childParentData.offset.X - headSize, 0.0);
                path.RelativeLineTo(0.0, headSize);
                path.RelativeLineTo(headSize, -headSize);
                path.RelativeLineTo(-headSize, -headSize);
                path.RelativeLineTo(0.0, headSize);
                path.MoveTo(offset.X + Size.Width, offset.Y + (Size.Height / 2.0));
                path.RelativeLineTo(-childParentData.offset.X + headSize, 0.0);
                path.RelativeLineTo(0.0, headSize);
                path.RelativeLineTo(-headSize, -headSize);
                path.RelativeLineTo(headSize, -headSize);
                path.RelativeLineTo(0.0, headSize);
                drew = true;
            }

            if (drew)
            {
                context.Canvas.DrawPath(path, brush: null, pen: pen);
            }
        }
        else
        {
            context.Canvas.DrawRectangle(
                new SolidColorBrush(Color.FromUInt32(0x90909090)),
                null,
                new Rect(offset, Size));
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("widthFactor", _widthFactor, ifNull: "expand"));
        properties.Add(new DoubleProperty("heightFactor", _heightFactor, ifNull: "expand"));
    }

    private static void AssertFactor(double? value, string parameterName)
    {
        if (Constants.KDebugMode && value is { } factor && !(factor >= 0.0))
        {
            throw new AssertionError($"{parameterName} must be null or non-negative.");
        }
    }
}

/// <summary>How much space a <see cref="RenderConstrainedOverflowBox"/> takes up.</summary>
public enum OverflowBoxFit
{
    /// <summary>The box takes the largest size the incoming constraints allow.</summary>
    Max,

    /// <summary>The box sizes itself to its child when the child does not overflow.</summary>
    DeferToChild,
}

/// <summary>Imposes different constraints on its child than it gets from its parent, possibly
/// allowing the child to overflow the parent.</summary>
public sealed class RenderConstrainedOverflowBox : RenderAligningShiftedBox
{
    private double? _minWidth;
    private double? _maxWidth;
    private double? _minHeight;
    private double? _maxHeight;
    private OverflowBoxFit _fit;

    public RenderConstrainedOverflowBox(
        RenderBox? child = null,
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null,
        OverflowBoxFit fit = OverflowBoxFit.Max,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null) : base(alignment, textDirection, child)
    {
        _minWidth = minWidth;
        _maxWidth = maxWidth;
        _minHeight = minHeight;
        _maxHeight = maxHeight;
        _fit = fit;
    }

    public double? MinWidth
    {
        get => _minWidth;
        set
        {
            if (_minWidth == value)
            {
                return;
            }

            _minWidth = value;
            MarkNeedsLayout();
        }
    }

    public double? MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (_maxWidth == value)
            {
                return;
            }

            _maxWidth = value;
            MarkNeedsLayout();
        }
    }

    public double? MinHeight
    {
        get => _minHeight;
        set
        {
            if (_minHeight == value)
            {
                return;
            }

            _minHeight = value;
            MarkNeedsLayout();
        }
    }

    public double? MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (_maxHeight == value)
            {
                return;
            }

            _maxHeight = value;
            MarkNeedsLayout();
        }
    }

    public OverflowBoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            MarkNeedsLayoutForSizedByParentChange();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// With <see cref="OverflowBoxFit.DeferToChild"/> the size is as small as the child when it does
    /// not overflow, so the box cannot be sized by its parent alone.
    /// </remarks>
    protected override bool SizedByParent => _fit switch
    {
        OverflowBoxFit.Max => true,
        OverflowBoxFit.DeferToChild => false,
        _ => throw new ArgumentOutOfRangeException(nameof(Fit)),
    };

    protected override Size ComputeDryLayout(BoxConstraints constraints) => _fit switch
    {
        OverflowBoxFit.Max => constraints.Biggest,
        OverflowBoxFit.DeferToChild => Child?.GetDryLayout(constraints) ?? constraints.Smallest,
        _ => throw new ArgumentOutOfRangeException(nameof(Fit)),
    };

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return null;
        }

        BoxConstraints childConstraints = GetInnerConstraints(constraints);
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result is null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        Size size = GetDryLayout(constraints);
        return result + ResolvedAlignment.AlongOffset(size, childSize).Y;
    }

    protected override void PerformLayout()
    {
        if (Child is { } child)
        {
            child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            if (_fit == OverflowBoxFit.DeferToChild)
            {
                Size = Constraints.Constrain(child.Size);
            }

            AlignChild();
            return;
        }

        if (_fit == OverflowBoxFit.DeferToChild)
        {
            Size = Constraints.Smallest;
        }
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: _minWidth ?? constraints.MinWidth,
            MaxWidth: _maxWidth ?? constraints.MaxWidth,
            MinHeight: _minHeight ?? constraints.MinHeight,
            MaxHeight: _maxHeight ?? constraints.MaxHeight);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("minWidth", MinWidth, ifNull: "use parent minWidth constraint"));
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, ifNull: "use parent maxWidth constraint"));
        properties.Add(new DoubleProperty("minHeight", MinHeight, ifNull: "use parent minHeight constraint"));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, ifNull: "use parent maxHeight constraint"));
        properties.Add(new EnumProperty<OverflowBoxFit>("fit", Fit));
    }
}

/// <summary>A render box that is a specific size but passes its original constraints through to its
/// child, which it allows to overflow.</summary>
public sealed class RenderSizedOverflowBox : RenderAligningShiftedBox
{
    private Size _requestedSize;

    public RenderSizedOverflowBox(
        Size requestedSize,
        RenderBox? child = null,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null) : base(alignment, textDirection, child)
    {
        _requestedSize = requestedSize;
    }

    public Size RequestedSize
    {
        get => _requestedSize;
        set
        {
            if (_requestedSize == value)
            {
                return;
            }

            _requestedSize = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) => _requestedSize.Width;

    protected override double ComputeMaxIntrinsicWidth(double height) => _requestedSize.Width;

    protected override double ComputeMinIntrinsicHeight(double width) => _requestedSize.Height;

    protected override double ComputeMaxIntrinsicHeight(double width) => _requestedSize.Height;

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return base.ComputeDistanceToActualBaseline(baseline);
        }

        double? result = child.GetDistanceToBaseline(baseline, onlyReal: true);
        if (result is null)
        {
            return base.ComputeDistanceToActualBaseline(baseline);
        }

        var childParentData = (BoxParentData)child.parentData!;
        return result + childParentData.offset.Y;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return null;
        }

        double? result = child.GetDryBaseline(constraints, baseline);
        if (result is null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(constraints);
        Size size = GetDryLayout(constraints);
        return result + ResolvedAlignment.AlongOffset(size, childSize).Y;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        constraints.Constrain(_requestedSize);

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_requestedSize);
        if (Child is { } child)
        {
            child.Layout(Constraints, parentUsesSize: true);
            AlignChild();
        }
    }
}

/// <summary>Sizes its child to a fraction of the total available space.</summary>
public sealed class RenderFractionallySizedOverflowBox : RenderAligningShiftedBox
{
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderFractionallySizedOverflowBox(
        RenderBox? child = null,
        double? widthFactor = null,
        double? heightFactor = null,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null) : base(alignment, textDirection, child)
    {
        AssertFactor(widthFactor, nameof(widthFactor));
        AssertFactor(heightFactor, nameof(heightFactor));
        _widthFactor = widthFactor;
        _heightFactor = heightFactor;
    }

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            AssertFactor(value, nameof(value));
            if (_widthFactor == value)
            {
                return;
            }

            _widthFactor = value;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            AssertFactor(value, nameof(value));
            if (_heightFactor == value)
            {
                return;
            }

            _heightFactor = value;
            MarkNeedsLayout();
        }
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        double minWidth = constraints.MinWidth;
        double maxWidth = constraints.MaxWidth;
        if (_widthFactor is { } widthFactor)
        {
            double width = maxWidth * widthFactor;
            minWidth = width;
            maxWidth = width;
        }

        double minHeight = constraints.MinHeight;
        double maxHeight = constraints.MaxHeight;
        if (_heightFactor is { } heightFactor)
        {
            double height = maxHeight * heightFactor;
            minHeight = height;
            maxHeight = height;
        }

        return new BoxConstraints(
            MinWidth: minWidth,
            MaxWidth: maxWidth,
            MinHeight: minHeight,
            MaxHeight: maxHeight);
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        double result = Child is { } child
            // The following line relies on double.infinity absorption.
            ? child.GetMinIntrinsicWidth(height * (_heightFactor ?? 1.0))
            : base.ComputeMinIntrinsicWidth(height);
        return result / (_widthFactor ?? 1.0);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        double result = Child is { } child
            ? child.GetMaxIntrinsicWidth(height * (_heightFactor ?? 1.0))
            : base.ComputeMaxIntrinsicWidth(height);
        return result / (_widthFactor ?? 1.0);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        double result = Child is { } child
            ? child.GetMinIntrinsicHeight(width * (_widthFactor ?? 1.0))
            : base.ComputeMinIntrinsicHeight(width);
        return result / (_heightFactor ?? 1.0);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        double result = Child is { } child
            ? child.GetMaxIntrinsicHeight(width * (_widthFactor ?? 1.0))
            : base.ComputeMaxIntrinsicHeight(width);
        return result / (_heightFactor ?? 1.0);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is { } child)
        {
            Size childSize = child.GetDryLayout(GetInnerConstraints(constraints));
            return constraints.Constrain(childSize);
        }

        return constraints.Constrain(GetInnerConstraints(constraints).Constrain(new Size()));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is not { } child)
        {
            return null;
        }

        BoxConstraints childConstraints = GetInnerConstraints(constraints);
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result is null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        Size size = GetDryLayout(constraints);
        return result + ResolvedAlignment.AlongOffset(size, childSize).Y;
    }

    protected override void PerformLayout()
    {
        if (Child is { } child)
        {
            child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            Size = Constraints.Constrain(child.Size);
            AlignChild();
        }
        else
        {
            Size = Constraints.Constrain(GetInnerConstraints(Constraints).Constrain(new Size()));
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("widthFactor", _widthFactor, ifNull: "pass-through"));
        properties.Add(new DoubleProperty("heightFactor", _heightFactor, ifNull: "pass-through"));
    }

    private static void AssertFactor(double? value, string parameterName)
    {
        if (Constants.KDebugMode && value is { } factor && !(factor >= 0.0))
        {
            throw new AssertionError($"{parameterName} must be null or non-negative.");
        }
    }
}
