using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/proxy_box.dart (approximate)

namespace Plumix.Rendering;

public abstract class RenderProxyBox : RenderBox, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_child != null)
        {
            var childParentData = (BoxParentData)_child.parentData!;
            visitor(_child, childParentData.offset, Matrix.Identity);
        }
    }

    protected override void PerformLayout()
    {
        if (_child != null)
        {
            _child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(_child.Size);
            ((BoxParentData)_child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Constrain(new Size());
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return _child?.GetMinIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return _child?.GetMaxIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return _child?.GetMinIntrinsicHeight(width) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return _child?.GetMaxIntrinsicHeight(width) ?? 0.0;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return _child?.GetDryLayout(constraints) ?? constraints.Smallest;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return _child?.GetDryBaseline(constraints, baseline);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            var childParentData = (BoxParentData)_child.parentData!;
            ctx.PaintChild(_child, childParentData.offset + offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null)
        {
            return false;
        }

        var childParentData = (BoxParentData)_child.parentData!;
        return _child.HitTest(result, position - childParentData.offset);
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        if (_child is null)
        {
            return null;
        }

        var childParentData = (BoxParentData)_child.parentData!;
        double? childBaseline = _child.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline + childParentData.offset.Y;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart
public abstract class RenderProxyBoxWithHitTestBehavior : RenderProxyBox
{
    private HitTestBehavior _behavior;

    protected RenderProxyBoxWithHitTestBehavior(
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null)
    {
        _behavior = behavior;
        Child = child;
    }

    public HitTestBehavior Behavior
    {
        get => _behavior;
        set => _behavior = value;
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!HasSize
            || position.X < 0
            || position.Y < 0
            || position.X > Size.Width
            || position.Y > Size.Height)
        {
            return false;
        }

        bool hitTarget = HitTestChildren(result, position) || HitTestSelf(position);
        if (hitTarget || Behavior == HitTestBehavior.Translucent)
        {
            result.Add(new BoxHitTestEntry(this, position));
        }

        return hitTarget;
    }

    protected override bool HitTestSelf(Point position)
    {
        return Behavior == HitTestBehavior.Opaque;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderMetaData)
public sealed class RenderMetaData : RenderProxyBoxWithHitTestBehavior
{
    public RenderMetaData(
        object? metaData = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null) : base(behavior, child)
    {
        MetaData = metaData;
    }

    public object? MetaData { get; set; }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIndexedSemantics)
public sealed class RenderIndexedSemantics : RenderProxyBox
{
    private int _index;

    public RenderIndexedSemantics(
        int index,
        RenderBox? child = null)
    {
        _index = index;
        Child = child;
    }

    public int Index
    {
        get => _index;
        set
        {
            if (_index == value)
            {
                return;
            }

            _index = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IndexInParent = _index;
    }
}

internal sealed class RenderVisibility : RenderProxyBox
{
    private bool _visible;
    private bool _maintainSemantics;

    public RenderVisibility(bool visible, bool maintainSemantics, RenderBox? child = null)
    {
        _visible = visible;
        _maintainSemantics = maintainSemantics;
        Child = child;
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            _visible = value;
            MarkNeedsPaint();
        }
    }

    public bool MaintainSemantics
    {
        get => _maintainSemantics;
        set
        {
            if (_maintainSemantics == value)
            {
                return;
            }

            _maintainSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_maintainSemantics || _visible)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_visible)
        {
            base.Paint(ctx, offset);
        }
    }
}

public sealed class RenderExcludeSemantics : RenderProxyBox
{
    private bool _excluding;

    public RenderExcludeSemantics(bool excluding = true, RenderBox? child = null)
    {
        _excluding = excluding;
        Child = child;
    }

    public bool Excluding
    {
        get => _excluding;
        set
        {
            if (_excluding == value)
            {
                return;
            }

            _excluding = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (!_excluding)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }
}

public sealed class RenderBlockSemantics : RenderProxyBox
{
    private bool _blocking;

    public RenderBlockSemantics(bool blocking = true, RenderBox? child = null)
    {
        _blocking = blocking;
        Child = child;
    }

    public bool Blocking
    {
        get => _blocking;
        set
        {
            if (_blocking == value)
            {
                return;
            }

            _blocking = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingSemanticsOfPreviouslyPaintedNodes = _blocking;
    }
}

public sealed class RenderConstrainedBox : RenderProxyBox
{
    private BoxConstraints _additionalConstraints;

    public RenderConstrainedBox(BoxConstraints additionalConstraints, RenderBox? child = null)
    {
        _additionalConstraints = additionalConstraints;
        Child = child;
    }

    public BoxConstraints AdditionalConstraints
    {
        get => _additionalConstraints;
        set
        {
            if (_additionalConstraints.Equals(value))
            {
                return;
            }

            _additionalConstraints = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var enforced = _additionalConstraints.Enforce(Constraints);

        if (Child != null)
        {
            Child.Layout(enforced, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = enforced.Constrain(new Size());
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (_additionalConstraints.HasTightWidth)
        {
            return _additionalConstraints.MinWidth;
        }

        return _additionalConstraints.ConstrainWidth(base.ComputeMinIntrinsicWidth(height));
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (_additionalConstraints.HasTightWidth)
        {
            return _additionalConstraints.MinWidth;
        }

        return _additionalConstraints.ConstrainWidth(base.ComputeMaxIntrinsicWidth(height));
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (_additionalConstraints.HasTightHeight)
        {
            return _additionalConstraints.MinHeight;
        }

        return _additionalConstraints.ConstrainHeight(base.ComputeMinIntrinsicHeight(width));
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (_additionalConstraints.HasTightHeight)
        {
            return _additionalConstraints.MinHeight;
        }

        return _additionalConstraints.ConstrainHeight(base.ComputeMaxIntrinsicHeight(width));
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        BoxConstraints enforced = _additionalConstraints.Enforce(constraints);
        return Child?.GetDryLayout(enforced) ?? enforced.Smallest;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return Child?.GetDryBaseline(_additionalConstraints.Enforce(constraints), baseline);
    }
}

// Retained as a compatibility surface for direct render-object consumers.
// New UnconstrainedBox widgets compose RenderConstraintsTransformBox, matching Flutter.
public sealed class RenderUnconstrainedBox : RenderProxyBox
{
    private Alignment _alignment;
    private Axis? _constrainedAxis;

    public RenderUnconstrainedBox(
        Alignment alignment = default,
        Axis? constrainedAxis = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _constrainedAxis = constrainedAxis;
        Child = child;
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

    public Axis? ConstrainedAxis
    {
        get => _constrainedAxis;
        set
        {
            if (_constrainedAxis == value)
            {
                return;
            }

            _constrainedAxis = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Size = Constraints.Constrain(new Size());
            return;
        }

        var childConstraints = _constrainedAxis switch
        {
            Axis.Horizontal => new BoxConstraints(
                MinWidth: Constraints.MinWidth,
                MaxWidth: Constraints.MaxWidth,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity),
            Axis.Vertical => new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: Constraints.MinHeight,
                MaxHeight: Constraints.MaxHeight),
            null => new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity),
            _ => throw new ArgumentOutOfRangeException()
        };

        Child.Layout(childConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/shifted_box.dart
// (RenderConstraintsTransformBox)
public sealed class RenderConstraintsTransformBox : RenderProxyBox
{
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private BoxConstraintsTransform _constraintsTransform;
    private Clip _clipBehavior;
    private BoxConstraints? _childConstraints;
    private bool _isOverflowing;

    public RenderConstraintsTransformBox(
        AlignmentGeometry alignment,
        TextDirection? textDirection,
        BoxConstraintsTransform constraintsTransform,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None)
    {
        _alignment = alignment;
        _textDirection = textDirection;
        _constraintsTransform = constraintsTransform
            ?? throw new ArgumentNullException(nameof(constraintsTransform));
        _clipBehavior = clipBehavior;
        Child = child;
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
            MarkNeedsLayout();
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
            MarkNeedsLayout();
        }
    }

    public BoxConstraintsTransform ConstraintsTransform
    {
        get => _constraintsTransform;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_constraintsTransform, value))
            {
                return;
            }

            _constraintsTransform = value;
            bool needsLayout = _childConstraints is null
                || !HasBoxConstraints
                || !_childConstraints.Value.Equals(TransformConstraints(CurrentBoxConstraints));
            if (needsLayout)
            {
                MarkNeedsLayout();
            }
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

    public bool IsOverflowing => _isOverflowing;

    public BoxConstraints? ChildConstraints => _childConstraints;

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Size = Constraints.Smallest;
            _childConstraints = null;
            _isOverflowing = false;
            return;
        }

        BoxConstraints childConstraints = TransformConstraints(Constraints);
        _childConstraints = childConstraints;
        Child.Layout(childConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);

        Alignment resolvedAlignment = ResolveAlignment();
        var childParentData = (BoxParentData)Child.parentData!;
        childParentData.offset = resolvedAlignment.AlongOffset(Size, Child.Size);
        _isOverflowing = HasOverflow(
            container: new Rect(default, Size),
            child: new Rect(childParentData.offset, Child.Size));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child == null)
        {
            return;
        }

        if (!_isOverflowing || _clipBehavior == Clip.None)
        {
            base.Paint(context, offset);
#if DEBUG
            if (_isOverflowing && _clipBehavior == Clip.None && Size.Width > 0 && Size.Height > 0)
            {
                var childParentData = (BoxParentData)Child.parentData!;
                DebugOverflowIndicator.Paint(
                    context,
                    offset,
                    new Rect(default, Size),
                    new Rect(childParentData.offset, Child.Size));
            }
#endif
            return;
        }

        context.PushClipRect(
            new Rect(offset, Size),
            clippedContext => base.Paint(clippedContext, offset),
            _clipBehavior);
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _clipBehavior == Clip.None || !_isOverflowing
            ? null
            : new Rect(default, Size);
    }

    private BoxConstraints TransformConstraints(BoxConstraints constraints)
    {
        BoxConstraints transformed = _constraintsTransform(constraints);
        if (!transformed.IsNormalized)
        {
            throw new InvalidOperationException(
                $"ConstraintsTransformBox returned non-normalized constraints: {transformed}.");
        }

        return transformed;
    }

    private Alignment ResolveAlignment()
    {
        if (_alignment.IsDirectional && !_textDirection.HasValue)
        {
            throw new InvalidOperationException(
                "A directional ConstraintsTransformBox alignment requires a TextDirection.");
        }

        return _alignment.Resolve(_textDirection ?? UI.TextDirection.Ltr);
    }

    private static bool HasOverflow(Rect container, Rect child)
    {
        const double tolerance = Constants.PrecisionErrorTolerance;
        return child.Left < container.Left - tolerance
            || child.Top < container.Top - tolerance
            || child.Right > container.Right + tolerance
            || child.Bottom > container.Bottom + tolerance;
    }
}

public sealed class RenderLimitedBox : RenderProxyBox
{
    private double _maxWidth;
    private double _maxHeight;

    public RenderLimitedBox(
        double maxWidth = double.PositiveInfinity,
        double maxHeight = double.PositiveInfinity,
        RenderBox? child = null)
    {
        _maxWidth = ValidateMaxValue(maxWidth, nameof(maxWidth));
        _maxHeight = ValidateMaxValue(maxHeight, nameof(maxHeight));
        Child = child;
    }

    public double MaxWidth
    {
        get => _maxWidth;
        set
        {
            double normalized = ValidateMaxValue(value, nameof(value));
            if (Math.Abs(_maxWidth - normalized) < 0.0001)
            {
                return;
            }

            _maxWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double MaxHeight
    {
        get => _maxHeight;
        set
        {
            double normalized = ValidateMaxValue(value, nameof(value));
            if (Math.Abs(_maxHeight - normalized) < 0.0001)
            {
                return;
            }

            _maxHeight = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var limitedConstraints = new BoxConstraints(
            MinWidth: Constraints.MinWidth,
            MaxWidth: Constraints.HasBoundedWidth ? Constraints.MaxWidth : Constraints.ConstrainWidth(MaxWidth),
            MinHeight: Constraints.MinHeight,
            MaxHeight: Constraints.HasBoundedHeight ? Constraints.MaxHeight : Constraints.ConstrainHeight(MaxHeight));

        if (Child != null)
        {
            Child.Layout(limitedConstraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = limitedConstraints.Constrain(new Size());
        }
    }

    private static double ValidateMaxValue(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Max value must be non-negative.");
        }

        return value;
    }
}

public enum OverflowBoxFit
{
    Max,
    DeferToChild
}

public sealed class RenderConstrainedOverflowBox : RenderProxyBox
{
    private Alignment _alignment;
    private double? _minWidth;
    private double? _maxWidth;
    private double? _minHeight;
    private double? _maxHeight;
    private OverflowBoxFit _fit;

    public RenderConstrainedOverflowBox(
        Alignment alignment = default,
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null,
        OverflowBoxFit fit = OverflowBoxFit.Max,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _minWidth = ValidateConstraint(minWidth, nameof(minWidth));
        _maxWidth = ValidateConstraint(maxWidth, nameof(maxWidth));
        _minHeight = ValidateConstraint(minHeight, nameof(minHeight));
        _maxHeight = ValidateConstraint(maxHeight, nameof(maxHeight));
        ValidateRanges(_minWidth, _maxWidth, nameof(minWidth), nameof(maxWidth));
        ValidateRanges(_minHeight, _maxHeight, nameof(minHeight), nameof(maxHeight));
        _fit = fit;
        Child = child;
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

    public double? MinWidth
    {
        get => _minWidth;
        set
        {
            double? normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(normalized, _maxWidth, nameof(value), nameof(MaxWidth));
            if (_minWidth == normalized)
            {
                return;
            }

            _minWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MaxWidth
    {
        get => _maxWidth;
        set
        {
            double? normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(_minWidth, normalized, nameof(MinWidth), nameof(value));
            if (_maxWidth == normalized)
            {
                return;
            }

            _maxWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MinHeight
    {
        get => _minHeight;
        set
        {
            double? normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(normalized, _maxHeight, nameof(value), nameof(MaxHeight));
            if (_minHeight == normalized)
            {
                return;
            }

            _minHeight = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MaxHeight
    {
        get => _maxHeight;
        set
        {
            double? normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(_minHeight, normalized, nameof(MinHeight), nameof(value));
            if (_maxHeight == normalized)
            {
                return;
            }

            _maxHeight = normalized;
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
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            Size = _fit switch
            {
                OverflowBoxFit.Max => Constraints.Biggest,
                OverflowBoxFit.DeferToChild => Constraints.Constrain(Child.Size),
                _ => throw new ArgumentOutOfRangeException()
            };
            ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
            return;
        }

        Size = _fit switch
        {
            OverflowBoxFit.Max => Constraints.Biggest,
            OverflowBoxFit.DeferToChild => Constraints.Smallest,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: _minWidth ?? constraints.MinWidth,
            MaxWidth: _maxWidth ?? constraints.MaxWidth,
            MinHeight: _minHeight ?? constraints.MinHeight,
            MaxHeight: _maxHeight ?? constraints.MaxHeight);
    }

    private static double? ValidateConstraint(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (double.IsNaN(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Constraint value must be non-negative.");
        }

        return value.Value;
    }

    private static void ValidateRanges(
        double? minValue,
        double? maxValue,
        string minName,
        string maxName)
    {
        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
        {
            throw new ArgumentOutOfRangeException(
                minName,
                $"{minName} cannot be greater than {maxName}.");
        }
    }
}

public sealed class RenderSizedOverflowBox : RenderProxyBox
{
    private Alignment _alignment;
    private Size _requestedSize;

    public RenderSizedOverflowBox(
        Size requestedSize,
        Alignment alignment = default,
        RenderBox? child = null)
    {
        _requestedSize = requestedSize;
        _alignment = alignment;
        Child = child;
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

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_requestedSize);
        if (Child == null)
        {
            return;
        }

        Child.Layout(Constraints, parentUsesSize: true);
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
    }
}

public sealed class RenderOffstage : RenderProxyBox
{
    private bool _offstage;

    public RenderOffstage(bool offstage = true, RenderBox? child = null)
    {
        _offstage = offstage;
        Child = child;
    }

    public bool Offstage
    {
        get => _offstage;
        set
        {
            if (_offstage == value)
            {
                return;
            }

            _offstage = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (_offstage)
        {
            if (Child != null)
            {
                Child.Layout(Constraints, parentUsesSize: true);
                ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
            }

            Size = Constraints.Smallest;
            return;
        }

        base.PerformLayout();
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_offstage && base.HitTest(result, position);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_offstage)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_offstage)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }
}

public sealed class RenderIgnorePointer : RenderProxyBox
{
    private bool _ignoring;
    private bool? _ignoringSemantics;

    public RenderIgnorePointer(
        bool ignoring = true,
        bool? ignoringSemantics = null,
        RenderBox? child = null)
    {
        _ignoring = ignoring;
        _ignoringSemantics = ignoringSemantics;
        Child = child;
    }

    public bool Ignoring
    {
        get => _ignoring;
        set
        {
            if (_ignoring == value)
            {
                return;
            }

            _ignoring = value;
            if (_ignoringSemantics == null)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public bool? IgnoringSemantics
    {
        get => _ignoringSemantics;
        set
        {
            if (_ignoringSemantics == value)
            {
                return;
            }

            _ignoringSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_ignoring && base.HitTest(result, position);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_ignoringSemantics == true)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingUserActions = _ignoring && (_ignoringSemantics ?? true);
    }
}

public sealed class RenderAbsorbPointer : RenderProxyBox
{
    private bool _absorbing;
    private bool? _ignoringSemantics;

    public RenderAbsorbPointer(
        bool absorbing = true,
        bool? ignoringSemantics = null,
        RenderBox? child = null)
    {
        _absorbing = absorbing;
        _ignoringSemantics = ignoringSemantics;
        Child = child;
    }

    public bool Absorbing
    {
        get => _absorbing;
        set
        {
            if (_absorbing == value)
            {
                return;
            }

            _absorbing = value;
            if (_ignoringSemantics == null)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public bool? IgnoringSemantics
    {
        get => _ignoringSemantics;
        set
        {
            if (_ignoringSemantics == value)
            {
                return;
            }

            _ignoringSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!_absorbing)
        {
            return base.HitTest(result, position);
        }

        if (!HasSize
            || position.X < 0
            || position.Y < 0
            || position.X > Size.Width
            || position.Y > Size.Height)
        {
            return false;
        }

        result.Add(new BoxHitTestEntry(this, position));
        return true;
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_ignoringSemantics == true)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingUserActions = _absorbing && (_ignoringSemantics ?? true);
    }
}

public sealed class RenderPadding : RenderProxyBox
{
    private Thickness _padding;

    public RenderPadding(Thickness padding, RenderBox? child = null)
    {
        _padding = padding;
        Child = child;
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding.Equals(value))
            {
                return;
            }

            _padding = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Size = Constraints.Constrain(new Size(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom));
            return;
        }

        var innerConstraints = Constraints.Deflate(Padding);
        Child.Layout(innerConstraints, parentUsesSize: true);

        var childSize = Child.Size;
        Size = Constraints.Constrain(
            new Size(childSize.Width + Padding.Left + Padding.Right, childSize.Height + Padding.Top + Padding.Bottom));

        ((BoxParentData)Child.parentData!).offset = new Point(Padding.Left, Padding.Top);
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return Padding.Left + Padding.Right
               + (Child?.GetMinIntrinsicWidth(Math.Max(0.0, height - Padding.Top - Padding.Bottom)) ?? 0.0);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Padding.Left + Padding.Right
               + (Child?.GetMaxIntrinsicWidth(Math.Max(0.0, height - Padding.Top - Padding.Bottom)) ?? 0.0);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return Padding.Top + Padding.Bottom
               + (Child?.GetMinIntrinsicHeight(Math.Max(0.0, width - Padding.Left - Padding.Right)) ?? 0.0);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return Padding.Top + Padding.Bottom
               + (Child?.GetMaxIntrinsicHeight(Math.Max(0.0, width - Padding.Left - Padding.Right)) ?? 0.0);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        BoxConstraints innerConstraints = constraints.Deflate(Padding);
        Size childSize = Child?.GetDryLayout(innerConstraints) ?? innerConstraints.Smallest;
        return constraints.Constrain(new Size(
            childSize.Width + Padding.Left + Padding.Right,
            childSize.Height + Padding.Top + Padding.Bottom));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        double? childBaseline = Child?.GetDryBaseline(constraints.Deflate(Padding), baseline);
        return childBaseline.HasValue ? childBaseline.Value + Padding.Top : null;
    }
}

public sealed class RenderAlign : RenderProxyBox
{
    private Alignment _alignment;
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderAlign(
        Alignment alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _widthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        _heightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
        Child = child;
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

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            double? normalized = ValidateFactor(value, nameof(value));
            if (_widthFactor == normalized)
            {
                return;
            }

            _widthFactor = normalized;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            double? normalized = ValidateFactor(value, nameof(value));
            if (_heightFactor == normalized)
            {
                return;
            }

            _heightFactor = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        bool shrinkWrapWidth = _widthFactor.HasValue || !Constraints.HasBoundedWidth;
        bool shrinkWrapHeight = _heightFactor.HasValue || !Constraints.HasBoundedHeight;

        if (Child == null)
        {
            double fallbackWidth = shrinkWrapWidth ? 0.0 : double.PositiveInfinity;
            double fallbackHeight = shrinkWrapHeight ? 0.0 : double.PositiveInfinity;
            Size = Constraints.Constrain(new Size(fallbackWidth, fallbackHeight));
            return;
        }

        Child.Layout(BoxConstraints.Loose(Constraints.Biggest), parentUsesSize: true);
        var childSize = Child.Size;
        double widthFactor = _widthFactor ?? 1.0;
        double heightFactor = _heightFactor ?? 1.0;
        double targetWidth = shrinkWrapWidth ? childSize.Width * widthFactor : double.PositiveInfinity;
        double targetHeight = shrinkWrapHeight ? childSize.Height * heightFactor : double.PositiveInfinity;
        Size = Constraints.Constrain(new Size(targetWidth, targetHeight));
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, childSize);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        bool shrinkWrapWidth = _widthFactor.HasValue || !constraints.HasBoundedWidth;
        bool shrinkWrapHeight = _heightFactor.HasValue || !constraints.HasBoundedHeight;
        Size childSize = Child?.GetDryLayout(BoxConstraints.Loose(constraints.Biggest)) ?? new Size();
        double targetWidth = shrinkWrapWidth
            ? childSize.Width * (_widthFactor ?? 1.0)
            : double.PositiveInfinity;
        double targetHeight = shrinkWrapHeight
            ? childSize.Height * (_heightFactor ?? 1.0)
            : double.PositiveInfinity;
        return constraints.Constrain(new Size(targetWidth, targetHeight));
    }

    private static double? ValidateFactor(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Factor must be non-negative.");
        }

        return value.Value;
    }
}

public sealed class RenderAspectRatio : RenderProxyBox
{
    private double _aspectRatio;

    public RenderAspectRatio(double aspectRatio, RenderBox? child = null)
    {
        _aspectRatio = ValidateAspectRatio(aspectRatio, nameof(aspectRatio));
        Child = child;
    }

    public double AspectRatio
    {
        get => _aspectRatio;
        set
        {
            double normalized = ValidateAspectRatio(value, nameof(value));
            if (Math.Abs(_aspectRatio - normalized) < 0.0001)
            {
                return;
            }

            _aspectRatio = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var computedSize = ComputeSizeForConstraints(Constraints);
        Size = computedSize;

        if (Child != null)
        {
            Child.Layout(BoxConstraints.Tight(computedSize));
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
    }

    private Size ComputeSizeForConstraints(BoxConstraints constraints)
    {
        if (constraints.IsTight)
        {
            return constraints.Smallest;
        }

        if (double.IsPositiveInfinity(constraints.MaxWidth) &&
            double.IsPositiveInfinity(constraints.MaxHeight))
        {
            throw new InvalidOperationException(
                "RenderAspectRatio requires at least one bounded axis.");
        }

        double width = constraints.MaxWidth;
        double height = width / _aspectRatio;

        if (double.IsPositiveInfinity(width))
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width > constraints.MaxWidth)
        {
            width = constraints.MaxWidth;
            height = width / _aspectRatio;
        }

        if (height > constraints.MaxHeight)
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width < constraints.MinWidth)
        {
            width = constraints.MinWidth;
            height = width / _aspectRatio;
        }

        if (height < constraints.MinHeight)
        {
            height = constraints.MinHeight;
            width = height * _aspectRatio;
        }

        return constraints.Constrain(new Size(width, height));
    }

    private static double ValidateAspectRatio(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Aspect ratio must be finite and positive.");
        }

        return value;
    }
}

public sealed class RenderFractionallySizedBox : RenderProxyBox
{
    private Alignment _alignment;
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderFractionallySizedBox(
        Alignment alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _widthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        _heightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
        Child = child;
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

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            double? normalized = ValidateFactor(value, nameof(value));
            if (_widthFactor == normalized)
            {
                return;
            }

            _widthFactor = normalized;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            double? normalized = ValidateFactor(value, nameof(value));
            if (_heightFactor == normalized)
            {
                return;
            }

            _heightFactor = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            var innerConstraints = GetInnerConstraints(Constraints);
            Child.Layout(innerConstraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
            return;
        }

        Size = Constraints.Constrain(GetInnerConstraints(Constraints).Constrain(new Size()));
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        double minWidth = constraints.MinWidth;
        double maxWidth = constraints.MaxWidth;

        if (_widthFactor.HasValue && double.IsFinite(maxWidth))
        {
            double width = maxWidth * _widthFactor.Value;
            minWidth = width;
            maxWidth = width;
        }

        double minHeight = constraints.MinHeight;
        double maxHeight = constraints.MaxHeight;

        if (_heightFactor.HasValue && double.IsFinite(maxHeight))
        {
            double height = maxHeight * _heightFactor.Value;
            minHeight = height;
            maxHeight = height;
        }

        return new BoxConstraints(
            MinWidth: minWidth,
            MaxWidth: maxWidth,
            MinHeight: minHeight,
            MaxHeight: maxHeight);
    }

    private static double? ValidateFactor(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (!double.IsFinite(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Factor must be finite and non-negative.");
        }

        return value.Value;
    }
}

public sealed class RenderFittedBox : RenderProxyBox
{
    private BoxFit _fit;
    private Alignment _alignment;
    private Matrix _transform = Matrix.Identity;
    private bool _paintDataDirty = true;

    public RenderFittedBox(
        BoxFit fit = BoxFit.Contain,
        Alignment alignment = default,
        RenderBox? child = null)
    {
        _fit = fit;
        _alignment = alignment;
        Child = child;
    }

    public BoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            _paintDataDirty = true;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
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
            _paintDataDirty = true;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(
                new BoxConstraints(
                    MaxWidth: double.PositiveInfinity,
                    MaxHeight: double.PositiveInfinity),
                parentUsesSize: true);

            Size = _fit switch
            {
                BoxFit.ScaleDown => Constraints.Constrain(
                    Constraints.Loosen().ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size)),
                _ => Constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size)
            };

            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Smallest;
        }

        _paintDataDirty = true;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || Size.Width <= 0 || Size.Height <= 0 || Child.Size.Width <= 0 || Child.Size.Height <= 0)
        {
            return;
        }

        UpdatePaintData();
        ctx.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y), translatedContext =>
        {
            translatedContext.PushTransform(_transform, transformedContext =>
            {
                transformedContext.PaintChild(Child, new Point(0, 0));
            });
        });
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || Size.Width <= 0 || Size.Height <= 0 || Child.Size.Width <= 0 || Child.Size.Height <= 0)
        {
            return false;
        }

        UpdatePaintData();
        if (!_transform.TryInvert(out var inverse))
        {
            return false;
        }

        var transformedPosition = inverse.Transform(position);
        return Child.HitTest(result, transformedPosition);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child == null)
        {
            return;
        }

        UpdatePaintData();
        visitor(Child, new Point(0, 0), _transform);
    }

    private void UpdatePaintData()
    {
        if (!_paintDataDirty)
        {
            return;
        }

        _paintDataDirty = false;
        if (Child == null)
        {
            _transform = Matrix.Identity;
            return;
        }

        var childSize = Child.Size;
        var fittedSizes = BoxFitUtils.ApplyBoxFit(_fit, childSize, Size);
        var sourceSize = fittedSizes.Source;
        var destinationSize = fittedSizes.Destination;

        if (sourceSize.Width <= 0.0 || sourceSize.Height <= 0.0 ||
            destinationSize.Width <= 0.0 || destinationSize.Height <= 0.0)
        {
            _transform = Matrix.Identity;
            return;
        }

        var sourceOffset = _alignment.AlongOffset(childSize, sourceSize);
        var destinationOffset = _alignment.AlongOffset(Size, destinationSize);
        double scaleX = destinationSize.Width / sourceSize.Width;
        double scaleY = destinationSize.Height / sourceSize.Height;

        _transform =
            Matrix.CreateTranslation(destinationOffset.X, destinationOffset.Y)
            * new Matrix(scaleX, 0, 0, scaleY, 0, 0)
            * Matrix.CreateTranslation(-sourceOffset.X, -sourceOffset.Y);
    }
}

public sealed class RenderColoredBox : RenderProxyBox
{
    private Color _color;

    public RenderColoredBox(Color color, RenderBox? child = null)
    {
        _color = color;
        Child = child;
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        ctx.DrawRectangle(new SolidColorBrush(Color), null, new Rect(offset, Size));
        base.Paint(ctx, offset);
    }
}

public sealed class RenderDecoratedBox : RenderProxyBox
{
    private Decoration _decoration;
    private DecorationPosition _position;
    private ImageConfiguration _configuration;
    private BoxPainter? _painter;

    public RenderDecoratedBox(
        Decoration decoration,
        RenderBox? child = null,
        ImageConfiguration? configuration = null,
        DecorationPosition position = DecorationPosition.Background)
    {
        _decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        _position = position;
        _configuration = configuration ?? ImageConfiguration.Empty;
        Child = child;
    }

    public DecorationPosition Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            MarkNeedsPaint();
        }
    }

    public BoxDecoration Decoration
    {
        get => _decoration as BoxDecoration
               ?? throw new InvalidOperationException("The current decoration is not a BoxDecoration.");
        set => DecorationValue = value;
    }

    public Decoration DecorationValue
    {
        get => _decoration;
        set
        {
            Decoration next = value ?? throw new ArgumentNullException(nameof(value));
            if (_decoration == next)
            {
                return;
            }

            DisposePainter();
            _decoration = next;
            MarkNeedsPaint();
        }
    }

    public ImageConfiguration Configuration
    {
        get => _configuration;
        set
        {
            var next = value ?? ImageConfiguration.Empty;
            if (_configuration == next)
            {
                return;
            }

            _configuration = next;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_position == DecorationPosition.Background)
        {
            PaintDecoration(ctx, offset);
        }

        base.Paint(ctx, offset);

        if (_position == DecorationPosition.Foreground)
        {
            PaintDecoration(ctx, offset);
        }
    }

    private void PaintDecoration(PaintingContext ctx, Point offset)
    {
        _painter ??= _decoration.CreateBoxPainter(HandleImageChanged);
        _painter.Paint(ctx, offset, _configuration.CopyWith(size: Size));
    }

    protected override void OnDetach()
    {
        DisposePainter();
        base.OnDetach();
    }

    private void HandleImageChanged()
    {
        MarkNeedsPaint();
    }

    private void DisposePainter()
    {
        _painter?.Dispose();
        _painter = null;
    }
}

public class RenderOpacity : RenderProxyBox
{
    private double _opacity;
    private bool _alwaysIncludeSemantics;

    public RenderOpacity(double opacity = 1.0, RenderBox? child = null)
        : this(opacity, alwaysIncludeSemantics: false, child)
    {
    }

    public RenderOpacity(
        double opacity,
        bool alwaysIncludeSemantics,
        RenderBox? child = null)
    {
        _opacity = Math.Clamp(opacity, 0.0, 1.0);
        _alwaysIncludeSemantics = alwaysIncludeSemantics;
        Child = child;
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_opacity - clamped) < 0.0001)
            {
                return;
            }

            bool semanticsVisibilityChanged = (_opacity == 0.0) != (clamped == 0.0);
            _opacity = clamped;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                if (semanticsVisibilityChanged)
                {
                    MarkNeedsSemanticsUpdate();
                }
            }
        }
    }

    public bool AlwaysIncludeSemantics
    {
        get => _alwaysIncludeSemantics;
        set
        {
            if (_alwaysIncludeSemantics == value)
            {
                return;
            }

            _alwaysIncludeSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null && Opacity < 1.0;

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as OpacityOffsetLayer ?? new OpacityOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityOffsetLayer opacityLayer)
        {
            opacityLayer.Opacity = Opacity;
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Opacity > 0.0 || AlwaysIncludeSemantics)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }
}

public sealed class RenderTransform : RenderProxyBox
{
    private Matrix _transform;
    private Alignment? _alignment;
    private FilterQuality? _filterQuality;

    public RenderTransform(
        Matrix transform,
        Alignment? alignment,
        RenderBox? child,
        FilterQuality? filterQuality = null)
    {
        _transform = transform;
        _alignment = alignment;
        _filterQuality = filterQuality;
        Child = child;
    }

    public RenderTransform(Matrix transform, RenderBox? child = null) : this(transform, null, child)
    {
    }

    public Alignment? Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value) return;
            _alignment = value;
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    public FilterQuality? FilterQuality
    {
        get => _filterQuality;
        set
        {
            if (_filterQuality == value) return;
            _filterQuality = value;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
            }
        }
    }

    public Matrix EffectiveTransform
    {
        get
        {
            if (!Alignment.HasValue) return Transform;
            var anchor = new Point(
                Size.Width * (Alignment.Value.X + 1) / 2.0,
                Size.Height * (Alignment.Value.Y + 1) / 2.0);
            return Matrix.CreateTranslation(anchor.X, anchor.Y)
                   * Transform
                   * Matrix.CreateTranslation(-anchor.X, -anchor.Y);
        }
    }

    public Matrix Transform
    {
        get => _transform;
        set
        {
            if (_transform == value)
            {
                return;
            }

            _transform = value;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child != null)
        {
            var childParentData = (BoxParentData)Child.parentData!;
            visitor(Child, childParentData.offset, EffectiveTransform);
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as TransformOffsetLayer ?? new TransformOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is TransformOffsetLayer transformLayer)
        {
            transformLayer.Transform = EffectiveTransform;
            transformLayer.FilterQuality = FilterQuality;
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null)
        {
            return false;
        }

        if (!EffectiveTransform.TryInvert(out var inverse))
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        var transformedPosition = inverse.Transform(position - childParentData.offset);
        return Child.HitTest(result, transformedPosition);
    }
}

// Dart parity sources:
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderFractionalTranslation)
// - flutter/packages/flutter/lib/src/rendering/rotated_box.dart (RenderRotatedBox)
public sealed class RenderFractionalTranslation : RenderProxyBox
{
    private Vector _translation;
    private bool _transformHitTests;

    public RenderFractionalTranslation(
        Vector translation,
        bool transformHitTests = true,
        RenderBox? child = null)
    {
        _translation = translation;
        _transformHitTests = transformHitTests;
        Child = child;
    }

    public Vector Translation
    {
        get => _translation;
        set
        {
            if (_translation == value) return;
            _translation = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool TransformHitTests
    {
        get => _transformHitTests;
        set => _transformHitTests = value;
    }

    private Vector PaintOffset => new(Size.Width * Translation.X, Size.Height * Translation.Y);

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return HitTestChildren(result, position);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child is null) return;
        var data = (BoxParentData)Child.parentData!;
        ctx.PaintChild(Child, data.offset + offset + PaintOffset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null) return false;
        var data = (BoxParentData)Child.parentData!;
        var offset = TransformHitTests ? PaintOffset : default;
        return Child.HitTest(result, position - data.offset - offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child is null) return;
        var data = (BoxParentData)Child.parentData!;
        var offset = PaintOffset;
        visitor(Child, data.offset, Matrix.CreateTranslation(offset.X, offset.Y));
    }
}

public sealed class RenderRotatedBox : RenderProxyBox
{
    private const double QuarterTurnRadians = Math.PI / 2.0;

    private int _quarterTurns;
    private Matrix _paintTransform = Matrix.Identity;

    public RenderRotatedBox(int quarterTurns, RenderBox? child = null)
    {
        _quarterTurns = quarterTurns;
        Child = child;
    }

    public int QuarterTurns
    {
        get => _quarterTurns;
        set
        {
            if (_quarterTurns == value)
            {
                return;
            }

            _quarterTurns = value;
            MarkNeedsLayout();
        }
    }

    private bool IsVertical => QuarterTurns % 2 != 0;

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMinIntrinsicHeight(height)
            : Child.GetMinIntrinsicWidth(height);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMaxIntrinsicHeight(height)
            : Child.GetMaxIntrinsicWidth(height);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMinIntrinsicWidth(width)
            : Child.GetMinIntrinsicHeight(width);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMaxIntrinsicWidth(width)
            : Child.GetMaxIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return constraints.Smallest;
        }

        Size childSize = Child.GetDryLayout(IsVertical ? constraints.Flipped : constraints);
        return IsVertical ? childSize.Flipped : childSize;
    }

    protected override void PerformLayout()
    {
        _paintTransform = Matrix.Identity;
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        Child.Layout(IsVertical ? Constraints.Flipped : Constraints, parentUsesSize: true);
        Size = IsVertical
            ? new Size(Child.Size.Height, Child.Size.Width)
            : Child.Size;
        ((BoxParentData)Child.parentData!).offset = default;

        double radians = QuarterTurnRadians * (QuarterTurns % 4);
        _paintTransform = Matrix.CreateTranslation(-Child.Size.Width / 2.0, -Child.Size.Height / 2.0)
                          * CreateRotationMatrix(radians)
                          * Matrix.CreateTranslation(Size.Width / 2.0, Size.Height / 2.0);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null || !_paintTransform.TryInvert(out Matrix inverse))
        {
            return false;
        }

        return Child.HitTest(result, inverse.Transform(position));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child != null)
        {
            visitor(Child, default, _paintTransform);
        }
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        return null;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        ctx.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y), translatedContext =>
        {
            translatedContext.PushTransform(_paintTransform, transformedContext =>
            {
                transformedContext.PaintChild(Child, default);
            });
        });
    }

    private static Matrix CreateRotationMatrix(double radians)
    {
        double sine = Math.Sin(radians);
        if (sine == 1.0)
        {
            return new Matrix(0, 1, -1, 0, 0, 0);
        }

        if (sine == -1.0)
        {
            return new Matrix(0, -1, 1, 0, 0, 0);
        }

        double cosine = Math.Cos(radians);
        if (cosine == -1.0)
        {
            return new Matrix(-1, 0, 0, -1, 0, 0);
        }

        return new Matrix(cosine, sine, -sine, cosine, 0, 0);
    }
}

public sealed class RenderClipRect : RenderProxyBox
{
    private Rect _clipRect;
    private bool _hasExplicitClipRect;
    private CustomClipper<Rect>? _clipper;
    private Clip _clipBehavior;

    public RenderClipRect(
        RenderBox? child = null,
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.HardEdge)
    {
        _clipper = clipper;
        _clipBehavior = clipBehavior;
        Child = child;
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
            MarkNeedsCompositingBitsUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    public CustomClipper<Rect>? Clipper
    {
        get => _clipper;
        set
        {
            if (ReferenceEquals(_clipper, value))
            {
                return;
            }

            CustomClipper<Rect>? oldClipper = _clipper;
            _clipper = value;
            if (Attached)
            {
                oldClipper?.RemoveListener(MarkNeedsClip);
                value?.AddListener(MarkNeedsClip);
            }

            MarkNeedsClip();
        }
    }

    public Rect ClipRect
    {
        get => _clipRect;
        set
        {
            if (_hasExplicitClipRect && _clipRect == value)
            {
                return;
            }

            _clipRect = value;
            _hasExplicitClipRect = true;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public void ClearClipRect()
    {
        if (!_hasExplicitClipRect)
        {
            return;
        }

        _hasExplicitClipRect = false;
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    public override bool IsRepaintBoundary => Child != null && _clipBehavior != Clip.None;
    protected override bool AlwaysNeedsCompositing => Child != null && _clipBehavior != Clip.None;

    protected override void OnAttach()
    {
        base.OnAttach();
        _clipper?.AddListener(MarkNeedsClip);
    }

    protected override void OnDetach()
    {
        _clipper?.RemoveListener(MarkNeedsClip);
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        bool hadSize = HasSize;
        var previousSize = hadSize ? Size : default;
        base.PerformLayout();

        if (_hasExplicitClipRect || Child == null)
        {
            return;
        }

        if (!hadSize || previousSize != Size)
        {
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as ClipRectOffsetLayer ?? new ClipRectOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is ClipRectOffsetLayer clipLayer)
        {
            clipLayer.ClipRect = EffectiveClip;
        }
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _clipBehavior == Clip.None
            ? null
            : _hasExplicitClipRect
            ? _clipRect
            : _clipper?.GetApproximateClipRect(Size) ?? new Rect(new Point(0, 0), Size);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        Rect clip = EffectiveClip;
        if (!clip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    private Rect EffectiveClip => _hasExplicitClipRect
        ? _clipRect
        : _clipper?.GetClip(Size) ?? new Rect(new Point(0, 0), Size);

    private void MarkNeedsClip()
    {
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }
}

public sealed class RenderClipRRect : RenderProxyBox
{
    private Rect _clipRect;
    private bool _hasExplicitClipRect;
    private BorderRadius _borderRadius;

    public RenderClipRRect(RenderBox? child = null)
    {
        Child = child;
    }

    public Rect ClipRect
    {
        get => _clipRect;
        set
        {
            if (_hasExplicitClipRect && _clipRect == value)
            {
                return;
            }

            _clipRect = value;
            _hasExplicitClipRect = true;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public BorderRadius BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    protected override void PerformLayout()
    {
        bool hadSize = HasSize;
        var previousSize = hadSize ? Size : default;
        base.PerformLayout();

        if (_hasExplicitClipRect || Child == null)
        {
            return;
        }

        if (!hadSize || previousSize != Size)
        {
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as ClipRRectOffsetLayer ?? new ClipRRectOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is ClipRRectOffsetLayer clipLayer)
        {
            clipLayer.ClipRect = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
            clipLayer.BorderRadius = _borderRadius;
        }
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var clip = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
        if (!Layer.ContainsRoundedRect(clip, _borderRadius, position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }
}

public class RenderPointerListener : RenderProxyBox
{
    private HitTestBehavior _behavior;

    public RenderPointerListener(
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerEnterEvent>? onPointerEnter = null,
        Action<PointerExitEvent>? onPointerExit = null,
        Action<PointerHoverEvent>? onPointerHover = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action<PointerSignalEvent>? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerEnter = onPointerEnter;
        OnPointerExit = onPointerExit;
        OnPointerHover = onPointerHover;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerSignal = onPointerSignal;
        _behavior = behavior;
        Child = child;
    }

    public Action<PointerDownEvent>? OnPointerDown { get; set; }

    public Action<PointerMoveEvent>? OnPointerMove { get; set; }

    public Action<PointerEnterEvent>? OnPointerEnter { get; set; }

    public Action<PointerExitEvent>? OnPointerExit { get; set; }

    public Action<PointerHoverEvent>? OnPointerHover { get; set; }

    public Action<PointerUpEvent>? OnPointerUp { get; set; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; set; }

    public Action<PointerSignalEvent>? OnPointerSignal { get; set; }

    public HitTestBehavior Behavior
    {
        get => _behavior;
        set => _behavior = value;
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (position.X < 0 || position.Y < 0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return false;
        }

        bool hitTarget = HitTestChildren(result, position) || HitTestSelf(position);
        if (hitTarget || Behavior == HitTestBehavior.Translucent || Behavior == HitTestBehavior.Opaque)
        {
            result.Add(new BoxHitTestEntry(this, position));
        }

        return hitTarget || Behavior == HitTestBehavior.Opaque || Behavior == HitTestBehavior.Translucent;
    }

    protected override bool HitTestSelf(Point position)
    {
        return Behavior == HitTestBehavior.Opaque;
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        switch (@event)
        {
            case PointerDownEvent downEvent:
                OnPointerDown?.Invoke(downEvent);
                break;
            case PointerMoveEvent moveEvent:
                OnPointerMove?.Invoke(moveEvent);
                break;
            case PointerEnterEvent enterEvent:
                OnPointerEnter?.Invoke(enterEvent);
                break;
            case PointerExitEvent exitEvent:
                OnPointerExit?.Invoke(exitEvent);
                break;
            case PointerHoverEvent hoverEvent:
                OnPointerHover?.Invoke(hoverEvent);
                break;
            case PointerUpEvent upEvent:
                OnPointerUp?.Invoke(upEvent);
                break;
            case PointerCancelEvent cancelEvent:
                OnPointerCancel?.Invoke(cancelEvent);
                break;
            case PointerSignalEvent signalEvent:
                OnPointerSignal?.Invoke(signalEvent);
                break;
        }
    }
}

public sealed class RenderSemanticsAnnotations : RenderProxyBox
{
    private string? _label;
    private string? _hint;
    private string? _onTapHint;
    private string? _tooltip;
    private string? _value;
    private string? _minValue;
    private string? _maxValue;
    private SemanticsRole _role;
    private SemanticsInputType _inputType;
    private SemanticsFlags _flags;
    private Action? _onTap;
    private Action? _onLongPress;
    private Action? _onDismiss;
    private IReadOnlyDictionary<CustomSemanticsAction, Action>? _customSemanticsActions;
    private Action? _onFocus;
    private bool _liveRegion;
    private bool _container;
    private bool _explicitChildNodes;
    private bool _mergeDescendants;

    public RenderSemanticsAnnotations(
        string? label = null,
        string? hint = null,
        string? onTapHint = null,
        string? tooltip = null,
        string? value = null,
        string? minValue = null,
        string? maxValue = null,
        SemanticsRole role = SemanticsRole.None,
        SemanticsInputType inputType = SemanticsInputType.None,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        Action? onLongPress = null,
        Action? onDismiss = null,
        IReadOnlyDictionary<CustomSemanticsAction, Action>? customSemanticsActions = null,
        bool liveRegion = false,
        bool container = false,
        bool explicitChildNodes = false,
        bool mergeDescendants = false,
        RenderBox? child = null)
    {
        _label = label;
        _hint = hint;
        _onTapHint = onTapHint;
        _tooltip = tooltip;
        _value = value;
        _minValue = minValue;
        _maxValue = maxValue;
        _role = role;
        _inputType = inputType;
        _flags = flags;
        _onTap = onTap;
        _onLongPress = onLongPress;
        _onDismiss = onDismiss;
        _customSemanticsActions = customSemanticsActions;
        _liveRegion = liveRegion;
        _container = container;
        _explicitChildNodes = explicitChildNodes;
        _mergeDescendants = mergeDescendants;
        Child = child;
    }

    public string? Label
    {
        get => _label;
        set
        {
            if (_label == value)
            {
                return;
            }

            _label = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? Hint
    {
        get => _hint;
        set
        {
            if (_hint == value) return;
            _hint = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? OnTapHint
    {
        get => _onTapHint;
        set
        {
            if (_onTapHint == value)
            {
                return;
            }

            _onTapHint = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? Tooltip
    {
        get => _tooltip;
        set
        {
            if (_tooltip == value)
            {
                return;
            }

            _tooltip = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? MinValue
    {
        get => _minValue;
        set
        {
            if (_minValue == value) return;
            _minValue = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? MaxValue
    {
        get => _maxValue;
        set
        {
            if (_maxValue == value) return;
            _maxValue = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public SemanticsFlags Flags
    {
        get => _flags;
        set
        {
            if (_flags == value)
            {
                return;
            }

            _flags = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public SemanticsRole Role
    {
        get => _role;
        set
        {
            if (_role == value) return;
            _role = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public SemanticsInputType InputType
    {
        get => _inputType;
        set
        {
            if (_inputType == value)
            {
                return;
            }

            _inputType = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnTap
    {
        get => _onTap;
        set
        {
            if (ReferenceEquals(_onTap, value))
            {
                return;
            }

            _onTap = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnDismiss
    {
        get => _onDismiss;
        set
        {
            if (ReferenceEquals(_onDismiss, value)) return;
            _onDismiss = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnFocus
    {
        get => _onFocus;
        set
        {
            if (ReferenceEquals(_onFocus, value)) return;
            _onFocus = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public IReadOnlyDictionary<CustomSemanticsAction, Action>? CustomSemanticsActions
    {
        get => _customSemanticsActions;
        set
        {
            if (ReferenceEquals(_customSemanticsActions, value))
            {
                return;
            }

            _customSemanticsActions = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnLongPress
    {
        get => _onLongPress;
        set
        {
            if (ReferenceEquals(_onLongPress, value)) return;
            _onLongPress = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool LiveRegion
    {
        get => _liveRegion;
        set
        {
            if (_liveRegion == value) return;
            _liveRegion = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool Container
    {
        get => _container;
        set
        {
            if (_container == value)
            {
                return;
            }

            _container = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool ExplicitChildNodes
    {
        get => _explicitChildNodes;
        set
        {
            if (_explicitChildNodes == value)
            {
                return;
            }

            _explicitChildNodes = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool MergeDescendants
    {
        get => _mergeDescendants;
        set
        {
            if (_mergeDescendants == value)
            {
                return;
            }

            _mergeDescendants = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(_label)
            && string.IsNullOrWhiteSpace(_hint)
            && string.IsNullOrWhiteSpace(_onTapHint)
            && string.IsNullOrWhiteSpace(_tooltip)
            && string.IsNullOrWhiteSpace(_value)
            && string.IsNullOrWhiteSpace(_minValue)
            && string.IsNullOrWhiteSpace(_maxValue)
            && _role == SemanticsRole.None
            && _inputType == SemanticsInputType.None
            && _flags == SemanticsFlags.None
            && _onTap is null
            && _onLongPress is null
            && _onDismiss is null
            && _customSemanticsActions is null
            && _onFocus is null
            && !_liveRegion
            && !_container
            && !_explicitChildNodes
            && !_mergeDescendants)
        {
            return;
        }

        configuration.IsSemanticBoundary = _container;
        configuration.Role = _role;
        configuration.InputType = _inputType;
        configuration.ExplicitChildNodes = _explicitChildNodes;
        if (_mergeDescendants)
        {
            configuration.IsMergingSemanticsOfDescendants = true;
        }

        if (!string.IsNullOrWhiteSpace(_label))
        {
            configuration.Label = _label;
        }

        configuration.Value = _value;
        configuration.MinValue = _minValue;
        configuration.MaxValue = _maxValue;


        if (!string.IsNullOrWhiteSpace(_hint))
        {
            configuration.Hint = _hint;
        }

        if (!string.IsNullOrWhiteSpace(_onTapHint))
        {
            configuration.OnTapHint = _onTapHint;
        }

        if (!string.IsNullOrWhiteSpace(_tooltip))
        {
            configuration.Tooltip = _tooltip;
        }

        configuration.Flags |= _flags;
        if (_liveRegion)
        {
            configuration.Flags |= SemanticsFlags.IsLiveRegion;
        }
        if (_onTap is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Tap, _onTap);
        }
        if (_onLongPress is not null)
        {
            configuration.AddActionHandler(SemanticsActions.LongPress, _onLongPress);
        }
        if (_onDismiss is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Dismiss, _onDismiss);
        }
        if (_onFocus is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Focus, _onFocus);
        }
        if (_customSemanticsActions is not null)
        {
            foreach (var pair in _customSemanticsActions)
            {
                configuration.AddCustomActionHandler(pair.Key, pair.Value);
            }
        }
    }
}

public sealed class RenderInkSplash : RenderProxyBox
{
    private Color? _splashColor;
    private Point _splashOrigin;
    private double _splashProgress;
    private double? _splashRadius;
    private bool _clipToBounds = true;

    public RenderInkSplash(
        Color? splashColor = null,
        Point splashOrigin = default,
        double splashProgress = 0,
        double? splashRadius = null,
        bool clipToBounds = true,
        RenderBox? child = null)
    {
        _splashColor = splashColor;
        _splashOrigin = splashOrigin;
        _splashProgress = NormalizeProgress(splashProgress);
        _splashRadius = NormalizeRadius(splashRadius);
        _clipToBounds = clipToBounds;
        Child = child;
    }

    public Color? SplashColor
    {
        get => _splashColor;
        set
        {
            if (_splashColor == value)
            {
                return;
            }

            _splashColor = value;
            MarkNeedsPaint();
        }
    }

    public Point SplashOrigin
    {
        get => _splashOrigin;
        set
        {
            if (_splashOrigin == value)
            {
                return;
            }

            _splashOrigin = value;
            MarkNeedsPaint();
        }
    }

    public double SplashProgress
    {
        get => _splashProgress;
        set
        {
            double normalized = NormalizeProgress(value);
            if (Math.Abs(_splashProgress - normalized) < 0.0001)
            {
                return;
            }

            _splashProgress = normalized;
            MarkNeedsPaint();
        }
    }

    public double? SplashRadius
    {
        get => _splashRadius;
        set
        {
            double? normalized = NormalizeRadius(value);
            if (_splashRadius == normalized)
            {
                return;
            }

            _splashRadius = normalized;
            MarkNeedsPaint();
        }
    }

    public bool ClipToBounds
    {
        get => _clipToBounds;
        set
        {
            if (_clipToBounds == value)
            {
                return;
            }

            _clipToBounds = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_clipToBounds)
        {
            var clipRect = new Rect(offset, Size);
            ctx.PushClipRect(clipRect, clippedContext =>
            {
                PaintSplash(clippedContext, offset);
                base.Paint(clippedContext, offset);
            });
            return;
        }

        PaintSplash(ctx, offset);
        base.Paint(ctx, offset);
    }

    private void PaintSplash(PaintingContext ctx, Point offset)
    {
        if (!_splashColor.HasValue || _splashProgress <= 0)
        {
            return;
        }

        var resolvedOrigin = ResolveOrigin(Size, _splashOrigin);
        double localMaxRadius = Math.Sqrt((Size.Width * Size.Width) + (Size.Height * Size.Height));
        double constrainedMaxRadius = _splashRadius.HasValue
            ? Math.Min(localMaxRadius, _splashRadius.Value)
            : localMaxRadius;
        double radius = constrainedMaxRadius * _splashProgress;

        var brush = new SolidColorBrush(_splashColor.Value);
        ctx.DrawCircle(brush, pen: null, center: offset + resolvedOrigin, radius: radius);
    }

    private static double NormalizeProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static double? NormalizeRadius(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        double resolved = value.Value;
        if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
        {
            return null;
        }

        return resolved;
    }

    private static Point ResolveOrigin(Size size, Point origin)
    {
        var center = new Point(size.Width / 2, size.Height / 2);

        double x = double.IsNaN(origin.X) || double.IsInfinity(origin.X)
            ? center.X
            : origin.X;
        double y = double.IsNaN(origin.Y) || double.IsInfinity(origin.Y)
            ? center.Y
            : origin.Y;

        return new Point(x, y);
    }
}
