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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            var childParentData = (BoxParentData)_child.parentData!;
            visitor(_child);
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
        RenderBox child = _child;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
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

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
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
            || position.X >= Size.Width
            || position.Y >= Size.Height)
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<HitTestBehavior>(
            "behavior",
            Behavior,
            defaultValue: DiagnosticsDefaults.NullValue));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<object>("metaData", MetaData));
    }
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int>("index", Index));
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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (!_excluding)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("excluding", Excluding));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("blocking", Blocking));
    }
}

public class RenderConstrainedBox : RenderProxyBox
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<BoxConstraints>("additionalConstraints", AdditionalConstraints));
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

    // Dart mixes `DebugOverflowIndicatorMixin` in; C# has no mixins, so its state lives here.
    private readonly DebugOverflowIndicator _debugOverflowIndicator = new();

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
                _debugOverflowIndicator.PaintOverflowIndicator(
                    this,
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

    /// <inheritdoc />
    public override string ToStringShort()
    {
        string header = base.ToStringShort();
        if (_isOverflowing)
        {
            header += " OVERFLOWING";
        }

        return header;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>(
            "textDirection",
            TextDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, defaultValue: double.PositiveInfinity));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, defaultValue: double.PositiveInfinity));
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
            MarkNeedsLayoutForSizedByParentChange();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderConstrainedOverflowBox.sizedByParent</c>: with
    /// <see cref="OverflowBoxFit.DeferToChild"/> the size is as small as the child when it does not
    /// overflow, so the box cannot be sized by its parent alone.
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
        RenderBox? child = Child;
        if (child == null)
        {
            return null;
        }

        BoxConstraints childConstraints = GetInnerConstraints(constraints);
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result == null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        Size size = GetDryLayout(constraints);
        return result + _alignment.AlongOffset(size, childSize).Y;
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            if (_fit == OverflowBoxFit.DeferToChild)
            {
                Size = Constraints.Constrain(Child.Size);
            }

            ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
        properties.Add(new DoubleProperty("minWidth", MinWidth, ifNull: "use parent minWidth constraint"));
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, ifNull: "use parent maxWidth constraint"));
        properties.Add(new DoubleProperty("minHeight", MinHeight, ifNull: "use parent minHeight constraint"));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, ifNull: "use parent maxHeight constraint"));
        properties.Add(new EnumProperty<OverflowBoxFit>("fit", Fit));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
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
            MarkNeedsLayoutForSizedByParentChange();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        _offstage ? 0.0 : base.ComputeMinIntrinsicWidth(height);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        _offstage ? 0.0 : base.ComputeMaxIntrinsicWidth(height);

    protected override double ComputeMinIntrinsicHeight(double width) =>
        _offstage ? 0.0 : base.ComputeMinIntrinsicHeight(width);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        _offstage ? 0.0 : base.ComputeMaxIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
        _offstage ? null : base.ComputeDistanceToActualBaseline(baseline);

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderOffstage.sizedByParent</c>: an offstage child takes no room.</remarks>
    protected override bool SizedByParent => _offstage;

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        _offstage ? constraints.Smallest : base.ComputeDryLayout(constraints);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) =>
        _offstage ? null : base.ComputeDryBaseline(constraints, baseline);

    protected override void PerformLayout()
    {
        if (_offstage)
        {
            Child?.Layout(Constraints);
            return;
        }

        base.PerformLayout();
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_offstage && base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child) => !_offstage;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_offstage)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_offstage)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("offstage", Offstage));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        if (Child is null)
        {
            return [];
        }

        return
        [
            Child.ToDiagnosticsNode(
                name: "child",
                style: Offstage ? DiagnosticsTreeStyle.Offstage : DiagnosticsTreeStyle.Sparse),
        ];
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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("ignoring", _ignoring));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            _ignoringSemantics,
            description: _ignoringSemantics is null ? null : $"implicitly {_ignoringSemantics}"));
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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("absorbing", Absorbing));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            IgnoringSemantics,
            description: IgnoringSemantics is null ? null : $"implicitly {IgnoringSemantics}"));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Thickness>("padding", Padding));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
        properties.Add(new DoubleProperty("widthFactor", _widthFactor, ifNull: "expand"));
        properties.Add(new DoubleProperty("heightFactor", _heightFactor, ifNull: "expand"));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("aspectRatio", AspectRatio));
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
        properties.Add(new DoubleProperty("widthFactor", _widthFactor, ifNull: "pass-through"));
        properties.Add(new DoubleProperty("heightFactor", _heightFactor, ifNull: "pass-through"));
    }
}

public sealed class RenderFittedBox : RenderProxyBox
{
    private BoxFit _fit;
    private Alignment _alignment;
    private Matrix4? _transform;
    private bool _hasVisualOverflow;

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
            ClearPaintData();
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
            ClearPaintData();
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

        ClearPaintData();
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child) =>
        Size.Width > 0 && Size.Height > 0 && Child is { } fitted
        && fitted.Size.Width > 0 && fitted.Size.Height > 0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || !PaintsChild(Child))
        {
            return;
        }

        UpdatePaintData();
        ctx.PushTransform(Matrix4.TranslationValues(offset.X, offset.Y, 0.0), translatedContext =>
        {
            translatedContext.PushTransform(_transform!, transformedContext =>
            {
                transformedContext.PaintChild(Child, new Point(0, 0));
            });
        });
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || !PaintsChild(Child))
        {
            return false;
        }

        UpdatePaintData();
        return result.AddWithPaintTransform(
            _transform,
            position,
            (hitResult, hitPosition) => Child.HitTest(hitResult, hitPosition));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child == null)
        {
            return;
        }

        UpdatePaintData();
        visitor(Child);
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (!PaintsChild(child))
        {
            transform.SetZero();
            return;
        }

        UpdatePaintData();
        transform.Multiply(_transform!);
    }

    private void ClearPaintData()
    {
        _hasVisualOverflow = false;
        _transform = null;
    }

    /// <remarks>Flutter's <c>RenderFittedBox._updatePaintData</c>.</remarks>
    private void UpdatePaintData()
    {
        if (_transform != null)
        {
            return;
        }

        if (Child == null)
        {
            _hasVisualOverflow = false;
            _transform = Matrix4.Identity();
            return;
        }

        var childSize = Child.Size;
        var fittedSizes = BoxFitUtils.ApplyBoxFit(_fit, childSize, Size);
        var sourceSize = fittedSizes.Source;
        var destinationSize = fittedSizes.Destination;

        if (sourceSize.Width <= 0.0 || sourceSize.Height <= 0.0 ||
            destinationSize.Width <= 0.0 || destinationSize.Height <= 0.0)
        {
            _hasVisualOverflow = false;
            _transform = Matrix4.Identity();
            return;
        }

        var sourceOffset = _alignment.AlongOffset(childSize, sourceSize);
        var destinationOffset = _alignment.AlongOffset(Size, destinationSize);
        double scaleX = destinationSize.Width / sourceSize.Width;
        double scaleY = destinationSize.Height / sourceSize.Height;
        _hasVisualOverflow = sourceSize.Width < childSize.Width || sourceSize.Height < childSize.Height;

        Matrix4 transform = Matrix4.TranslationValues(destinationOffset.X, destinationOffset.Y, 0.0);
        transform.ScaleByDouble(scaleX, scaleY, 1.0, 1);
        transform.TranslateByDouble(-sourceOffset.X, -sourceOffset.Y, 0, 1);
        _transform = transform;
    }

    /// <summary>Whether the fitted child is larger than the source rectangle it was inscribed into.</summary>
    /// <remarks>Flutter's <c>RenderFittedBox._hasVisualOverflow</c>.</remarks>
    public bool HasVisualOverflow
    {
        get
        {
            UpdatePaintData();
            return _hasVisualOverflow;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<BoxFit>("fit", Fit));
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
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

    /// The decoration as a [BoxDecoration]. A [ShapeDecoration] whose shape has an exact box
    /// equivalent (rounded rectangle, stadium, circle or [BoxBorder]) is projected onto one; any
    /// other shape has no box equivalent and throws.
    public BoxDecoration Decoration
    {
        get => _decoration switch
        {
            BoxDecoration box => box,
            ShapeDecoration shape when TryProjectToBoxDecoration(shape) is { } projected => projected,
            _ => throw new InvalidOperationException("The current decoration is not a BoxDecoration."),
        };
        set => DecorationValue = value;
    }

    private static BoxDecoration? TryProjectToBoxDecoration(ShapeDecoration decoration)
    {
        BorderSide side = ShapeBorderGeometry.SideOrNone(decoration.Shape);
        BoxBorder? border = decoration.Shape as BoxBorder
                            ?? (side == BorderSide.None ? null : Border.FromBorderSide(side));
        BoxShape shape = ShapeBorderGeometry.BoxShapeOf(decoration.Shape);
        BorderRadius? radius = decoration.Shape is BoxBorder
            ? null
            : ShapeBorderGeometry.ResolveRadiusOrNull(decoration.Shape);
        if (radius is null && decoration.Shape is not BoxBorder)
        {
            return null;
        }

        return new BoxDecoration(
            Color: decoration.Color,
            Gradient: decoration.Gradient,
            Border: border,
            BorderRadius: shape == BoxShape.Circle ? null : radius,
            BoxShadows: decoration.Shadows is { Count: > 0 } ? decoration.Shadows : null,
            Image: decoration.Image,
            Shape: shape);
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(_decoration.ToDiagnosticsNode(name: "decoration"));
        properties.Add(new DiagnosticsProperty<ImageConfiguration>("configuration", Configuration));
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

            bool didNeedCompositing = AlwaysNeedsCompositing;
            bool semanticsVisibilityChanged = (_opacity == 0.0) != (clamped == 0.0);
            _opacity = clamped;
            if (didNeedCompositing != AlwaysNeedsCompositing)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsCompositedLayerUpdate();
            if (semanticsVisibilityChanged && !AlwaysIncludeSemantics)
            {
                MarkNeedsSemanticsUpdate();
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

    public override bool IsRepaintBoundary => AlwaysNeedsCompositing;
    protected override bool AlwaysNeedsCompositing => Child != null && Opacity > 0.0;

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

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Opacity > 0.0 || AlwaysIncludeSemantics)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

public sealed class RenderTransform : RenderProxyBox
{
    private Matrix4 _transform;
    private Point? _origin;
    private Alignment? _alignment;
    private FilterQuality? _filterQuality;

    public RenderTransform(
        Matrix4 transform,
        Alignment? alignment,
        RenderBox? child,
        FilterQuality? filterQuality = null,
        Point? origin = null,
        bool transformHitTests = true)
    {
        _transform = Matrix4.Copy(transform);
        _alignment = alignment;
        _filterQuality = filterQuality;
        _origin = origin;
        TransformHitTests = transformHitTests;
        Child = child;
    }

    public RenderTransform(Matrix4 transform, RenderBox? child = null) : this(transform, null, child)
    {
    }

    /// <summary>The origin of the coordinate system in which to apply the transform.</summary>
    public Point? Origin
    {
        get => _origin;
        set
        {
            if (_origin == value) return;
            _origin = value;
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
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

    /// <summary>Whether hit tests are transformed along with the paint.</summary>
    public bool TransformHitTests { get; set; }

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

    /// <remarks>
    /// Flutter's <c>RenderTransform._effectiveTransform</c>: `T(origin) * T(a) * M * T(-a) * T(-origin)`
    /// with `a = alignment.alongSize(size)`. Returns the live transform when neither is set.
    /// </remarks>
    public Matrix4 EffectiveTransform
    {
        get
        {
            if (_origin is null && _alignment is null)
            {
                return _transform;
            }

            Matrix4 result = Matrix4.Identity();
            if (_origin is { } origin)
            {
                result.TranslateByDouble(origin.X, origin.Y, 0, 1);
            }

            Point translation = default;
            if (_alignment is { } alignment)
            {
                translation = alignment.AlongSize(Size);
                result.TranslateByDouble(translation.X, translation.Y, 0, 1);
            }

            result.Multiply(_transform);

            if (_alignment is not null)
            {
                result.TranslateByDouble(-translation.X, -translation.Y, 0, 1);
            }

            if (_origin is { } originValue)
            {
                result.TranslateByDouble(-originValue.X, -originValue.Y, 0, 1);
            }

            return result;
        }
    }

    public Matrix4 Transform
    {
        get => _transform;
        set
        {
            if (_transform == value)
            {
                return;
            }

            _transform = Matrix4.Copy(value);
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>Resets the transform to the identity.</summary>
    public void SetIdentity()
    {
        _transform.SetIdentity();
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the x axis.</summary>
    public void RotateX(double radians)
    {
        _transform.RotateX(radians);
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the y axis.</summary>
    public void RotateY(double radians)
    {
        _transform.RotateY(radians);
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the z axis.</summary>
    public void RotateZ(double radians)
    {
        _transform.RotateZ(radians);
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a translation.</summary>
    public void Translate(double x, double y = 0.0, double z = 0.0)
    {
        _transform.TranslateByDouble(x, y, z, 1);
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a scale.</summary>
    public void Scale(double x, double? y = null, double? z = null)
    {
        _transform.ScaleByDouble(x, y ?? x, z ?? x, 1);
        MarkNeedsCompositedLayerUpdate();
        MarkNeedsSemanticsUpdate();
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    /// <inheritdoc />
    /// <remarks>
    /// Flutter drops the layer and paints nothing when the effective transform is singular or carries
    /// a non-finite entry; Plumix expresses the same rule by skipping the child paint.
    /// </remarks>
    public override bool PaintsChild(RenderObject child)
    {
        double determinant = EffectiveTransform.Determinant();
        return determinant != 0 && double.IsFinite(determinant);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child is null || !PaintsChild(Child))
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child != null)
        {
            visitor(Child);
        }
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        transform.Multiply(EffectiveTransform);
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

    public override bool HitTest(BoxHitTestResult result, Point position) =>
        HitTestChildren(result, position);

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null)
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        return result.AddWithPaintTransform(
            TransformHitTests ? EffectiveTransform : null,
            position - childParentData.offset,
            (hitResult, hitPosition) => Child.HitTest(hitResult, hitPosition));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new TransformProperty("transform matrix", _transform));
        properties.Add(new DiagnosticsProperty<Point?>("origin", Origin));
        properties.Add(new DiagnosticsProperty<Alignment?>("alignment", Alignment));
        properties.Add(new DiagnosticsProperty<bool>("transformHitTests", TransformHitTests));
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
        Vector offset = PaintOffset;
        return result.AddWithPaintOffset(
            TransformHitTests ? new Point(offset.X, offset.Y) : null,
            position - data.offset,
            (hitResult, hitPosition) => Child.HitTest(hitResult, hitPosition));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child is null) return;
        var data = (BoxParentData)Child.parentData!;
        var offset = PaintOffset;
        visitor(Child);
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        transform.TranslateByDouble(Translation.X * Size.Width, Translation.Y * Size.Height, 0, 1);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Vector>("translation", Translation));
        properties.Add(new DiagnosticsProperty<bool>("transformHitTests", TransformHitTests));
    }
}

public sealed class RenderRotatedBox : RenderProxyBox
{
    private const double QuarterTurnRadians = Math.PI / 2.0;

    private int _quarterTurns;
    private Matrix4 _paintTransform = Matrix4.Identity();

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
        _paintTransform = Matrix4.Identity();
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

        _paintTransform = Matrix4.Identity();
        _paintTransform.TranslateByDouble(Size.Width / 2.0, Size.Height / 2.0, 0, 1);
        _paintTransform.RotateZ(QuarterTurnRadians * (QuarterTurns % 4));
        _paintTransform.TranslateByDouble(-Child.Size.Width / 2.0, -Child.Size.Height / 2.0, 0, 1);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null)
        {
            return false;
        }

        return result.AddWithPaintTransform(
            _paintTransform,
            position,
            (hitResult, hitPosition) => Child.HitTest(hitResult, hitPosition));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child != null)
        {
            visitor(Child);
        }
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (Child is not null)
        {
            transform.Multiply(_paintTransform);
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

        ctx.PushTransform(Matrix4.TranslationValues(offset.X, offset.Y, 0.0), translatedContext =>
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
        Action<PointerPanZoomStartEvent>? onPointerPanZoomStart = null,
        Action<PointerPanZoomUpdateEvent>? onPointerPanZoomUpdate = null,
        Action<PointerPanZoomEndEvent>? onPointerPanZoomEnd = null,
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
        OnPointerPanZoomStart = onPointerPanZoomStart;
        OnPointerPanZoomUpdate = onPointerPanZoomUpdate;
        OnPointerPanZoomEnd = onPointerPanZoomEnd;
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

    /// <summary>Called when a trackpad pan/zoom gesture starts over this render object.</summary>
    public Action<PointerPanZoomStartEvent>? OnPointerPanZoomStart { get; set; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress reports new values.</summary>
    public Action<PointerPanZoomUpdateEvent>? OnPointerPanZoomUpdate { get; set; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress ends.</summary>
    public Action<PointerPanZoomEndEvent>? OnPointerPanZoomEnd { get; set; }

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
            case PointerPanZoomStartEvent panZoomStartEvent:
                OnPointerPanZoomStart?.Invoke(panZoomStartEvent);
                break;
            case PointerPanZoomUpdateEvent panZoomUpdateEvent:
                OnPointerPanZoomUpdate?.Invoke(panZoomUpdateEvent);
                break;
            case PointerPanZoomEndEvent panZoomEndEvent:
                OnPointerPanZoomEnd?.Invoke(panZoomEndEvent);
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

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagsSummary<Delegate>(
            "listeners",
            [
                new KeyValuePair<string, Delegate?>("down", OnPointerDown),
                new KeyValuePair<string, Delegate?>("move", OnPointerMove),
                new KeyValuePair<string, Delegate?>("up", OnPointerUp),
                new KeyValuePair<string, Delegate?>("hover", OnPointerHover),
                new KeyValuePair<string, Delegate?>("cancel", OnPointerCancel),
                new KeyValuePair<string, Delegate?>("signal", OnPointerSignal),
            ],
            ifEmpty: "<none>"));
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
    private string? _increasedValue;
    private string? _decreasedValue;
    private SemanticsRole _role;
    private SemanticsInputType _inputType;
    private SemanticsHitTestBehavior _hitTestBehavior;
    private SemanticsFlags _flags;
    private Action? _onTap;
    private Action? _onLongPress;
    private Action? _onDismiss;
    private Action? _onExpand;
    private Action? _onCollapse;
    private Action? _onIncrease;
    private Action? _onDecrease;
    private IReadOnlyDictionary<CustomSemanticsAction, Action>? _customSemanticsActions;
    private Action? _onFocus;
    private bool _liveRegion;
    private bool _container;
    private bool _explicitChildNodes;
    private bool _mergeDescendants;
    private SemanticsSortKey? _sortKey;
    private TextDirection? _textDirection;
    private SemanticsTag? _tagForChildren;
    private AccessibilityFocusBlockType _accessibilityFocusBlockType;
    private object? _traversalParentIdentifier;
    private object? _traversalChildIdentifier;

    public RenderSemanticsAnnotations(
        string? label = null,
        string? hint = null,
        string? onTapHint = null,
        string? tooltip = null,
        string? value = null,
        string? minValue = null,
        string? maxValue = null,
        string? increasedValue = null,
        string? decreasedValue = null,
        SemanticsRole role = SemanticsRole.None,
        SemanticsInputType inputType = SemanticsInputType.None,
        SemanticsHitTestBehavior hitTestBehavior = SemanticsHitTestBehavior.Defer,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        Action? onLongPress = null,
        Action? onDismiss = null,
        Action? onExpand = null,
        Action? onCollapse = null,
        Action? onIncrease = null,
        Action? onDecrease = null,
        IReadOnlyDictionary<CustomSemanticsAction, Action>? customSemanticsActions = null,
        bool liveRegion = false,
        bool container = false,
        bool explicitChildNodes = false,
        SemanticsSortKey? sortKey = null,
        TextDirection? textDirection = null,
        bool mergeDescendants = false,
        SemanticsTag? tagForChildren = null,
        AccessibilityFocusBlockType accessibilityFocusBlockType = AccessibilityFocusBlockType.None,
        object? traversalParentIdentifier = null,
        object? traversalChildIdentifier = null,
        RenderBox? child = null)
    {
        _traversalParentIdentifier = traversalParentIdentifier;
        _traversalChildIdentifier = traversalChildIdentifier;
        _accessibilityFocusBlockType = accessibilityFocusBlockType;
        _label = label;
        _hint = hint;
        _onTapHint = onTapHint;
        _tooltip = tooltip;
        _value = value;
        _minValue = minValue;
        _maxValue = maxValue;
        _increasedValue = increasedValue;
        _decreasedValue = decreasedValue;
        _role = role;
        _inputType = inputType;
        _hitTestBehavior = hitTestBehavior;
        _flags = flags;
        _onTap = onTap;
        _onLongPress = onLongPress;
        _onDismiss = onDismiss;
        _onExpand = onExpand;
        _onCollapse = onCollapse;
        _onIncrease = onIncrease;
        _onDecrease = onDecrease;
        _customSemanticsActions = customSemanticsActions;
        _liveRegion = liveRegion;
        _container = container;
        _explicitChildNodes = explicitChildNodes;
        _sortKey = sortKey;
        _textDirection = textDirection;
        _mergeDescendants = mergeDescendants;
        _tagForChildren = tagForChildren;
        Child = child;
    }

    /// <remarks>Flutter's <c>RenderSemanticsAnnotations.traversalParentIdentifier</c>.</remarks>
    public object? TraversalParentIdentifier
    {
        get => _traversalParentIdentifier;
        set
        {
            if (Equals(_traversalParentIdentifier, value))
            {
                return;
            }

            _traversalParentIdentifier = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>RenderSemanticsAnnotations.traversalChildIdentifier</c>.</remarks>
    public object? TraversalChildIdentifier
    {
        get => _traversalChildIdentifier;
        set
        {
            if (Equals(_traversalChildIdentifier, value))
            {
                return;
            }

            _traversalChildIdentifier = value;
            MarkNeedsSemanticsUpdate();
        }
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

    public string? IncreasedValue
    {
        get => _increasedValue;
        set
        {
            if (_increasedValue == value) return;
            _increasedValue = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public string? DecreasedValue
    {
        get => _decreasedValue;
        set
        {
            if (_decreasedValue == value) return;
            _decreasedValue = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnIncrease
    {
        get => _onIncrease;
        set
        {
            if (_onIncrease == value) return;
            bool hadHandler = _onIncrease is not null;
            _onIncrease = value;
            if (hadHandler != (value is not null)) MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnDecrease
    {
        get => _onDecrease;
        set
        {
            if (_onDecrease == value) return;
            bool hadHandler = _onDecrease is not null;
            _onDecrease = value;
            if (hadHandler != (value is not null)) MarkNeedsSemanticsUpdate();
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

    public SemanticsHitTestBehavior HitTestBehavior
    {
        get => _hitTestBehavior;
        set
        {
            if (_hitTestBehavior == value)
            {
                return;
            }

            _hitTestBehavior = value;
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

    public Action? OnExpand
    {
        get => _onExpand;
        set
        {
            if (ReferenceEquals(_onExpand, value)) return;
            _onExpand = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnCollapse
    {
        get => _onCollapse;
        set
        {
            if (ReferenceEquals(_onCollapse, value)) return;
            _onCollapse = value;
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

    /// <summary>
    /// The reading direction for this subtree's semantics, and the direction the default traversal
    /// sort walks siblings in.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.textDirection</c>.</remarks>
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
            MarkNeedsSemanticsUpdate();
        }
    }

    public SemanticsSortKey? SortKey
    {
        get => _sortKey;
        set
        {
            if (Equals(_sortKey, value))
            {
                return;
            }

            _sortKey = value;
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

    public SemanticsTag? TagForChildren
    {
        get => _tagForChildren;
        set
        {
            if (ReferenceEquals(_tagForChildren, value))
            {
                return;
            }

            _tagForChildren = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>
    /// Whether assistive technologies may move accessibility focus onto this node, its subtree, or
    /// both.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.accessibilityFocusBlockType</c>.</remarks>
    public AccessibilityFocusBlockType AccessibilityFocusBlockType
    {
        get => _accessibilityFocusBlockType;
        set
        {
            if (_accessibilityFocusBlockType == value)
            {
                return;
            }

            _accessibilityFocusBlockType = value;
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
            && string.IsNullOrWhiteSpace(_increasedValue)
            && string.IsNullOrWhiteSpace(_decreasedValue)
            && _role == SemanticsRole.None
            && _inputType == SemanticsInputType.None
            && _hitTestBehavior == SemanticsHitTestBehavior.Defer
            && _flags == SemanticsFlags.None
            && _onTap is null
            && _onLongPress is null
            && _onDismiss is null
            && _onExpand is null
            && _onCollapse is null
            && _onIncrease is null
            && _onDecrease is null
            && _customSemanticsActions is null
            && _onFocus is null
            && !_liveRegion
            && !_container
            && !_explicitChildNodes
            && _sortKey is null
            && _textDirection is null
            && _tagForChildren is null
            && _accessibilityFocusBlockType == AccessibilityFocusBlockType.None
            && _traversalParentIdentifier is null
            && _traversalChildIdentifier is null
            && !_mergeDescendants)
        {
            return;
        }

        if (_tagForChildren is not null)
        {
            configuration.AddTagForChildren(_tagForChildren);
        }

        configuration.IsSemanticBoundary = _container;
        configuration.AccessibilityFocusBlockType = _accessibilityFocusBlockType;
        configuration.Role = _role;
        configuration.InputType = _inputType;
        configuration.HitTestBehavior = _hitTestBehavior;
        configuration.ExplicitChildNodes = _explicitChildNodes;
        configuration.SortKey = _sortKey;
        configuration.TextDirection = _textDirection;
        configuration.TraversalParentIdentifier = _traversalParentIdentifier;
        configuration.TraversalChildIdentifier = _traversalChildIdentifier;
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
        configuration.IncreasedValue = _increasedValue;
        configuration.DecreasedValue = _decreasedValue;


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
        if (_onExpand is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Expand, _onExpand);
        }
        if (_onCollapse is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Collapse, _onCollapse);
        }
        if (_onIncrease is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Increase, _onIncrease);
        }
        if (_onDecrease is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Decrease, _onDecrease);
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
