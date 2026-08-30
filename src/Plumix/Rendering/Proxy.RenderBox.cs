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
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaintSize(context, offset);
        if (Child is null || Child.Size.Width <= 0 || Child.Size.Height <= 0)
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

    /// <inheritdoc />
    public override void Reassemble()
    {
        base.Reassemble();
        _debugOverflowIndicator.Reassemble();
    }

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

        _clipRectLayer.Layer = context.PushClipRect(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            base.Paint,
            _clipBehavior,
            _clipRectLayer.Layer);
    }

    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
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

public sealed class RenderFittedBox : RenderProxyBox
{
    private BoxFit _fit;
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;
    private Clip _clipBehavior;
    private Matrix4? _transform;
    private bool? _hasVisualOverflow;

    public RenderFittedBox(
        BoxFit fit = BoxFit.Contain,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None)
    {
        _fit = fit;
        _alignment = alignment;
        _textDirection = textDirection;
        _clipBehavior = clipBehavior;
        Child = child;
    }

    private Alignment Resolve() => _resolvedAlignment ??= _alignment.Resolve(_textDirection);

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsPaint();
    }

    /// <remarks>Flutter's <c>RenderFittedBox._fitAffectsLayout</c>: only `scaleDown` changes the size
    /// the box takes, so every other fit change is paint-only.</remarks>
    private static bool FitAffectsLayout(BoxFit fit) => fit == BoxFit.ScaleDown;

    public BoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            BoxFit lastFit = _fit;
            _fit = value;
            if (FitAffectsLayout(lastFit) || FitAffectsLayout(value))
            {
                MarkNeedsLayout();
            }
            else
            {
                ClearPaintData();
                MarkNeedsPaint();
            }
        }
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
            ClearPaintData();
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
            ClearPaintData();
            MarkNeedResolution();
        }
    }

    /// <summary>How to clip the child when it overflows. Defaults to <see cref="Clip.None"/>.</summary>
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

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is not { } child)
        {
            return constraints.Smallest;
        }

        Size childSize = child.GetDryLayout(BoxConstraints.Unbounded);
        if (_fit == BoxFit.ScaleDown)
        {
            Size unconstrainedSize = constraints.Loosen()
                .ConstrainSizeAndAttemptToPreserveAspectRatio(childSize);
            return constraints.Constrain(unconstrainedSize);
        }

        return constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(childSize);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        // The baseline of the child laid out unconstrained, without the paint-time transform.
        return Child?.GetDryBaseline(BoxConstraints.Unbounded, baseline);
    }

    protected override void PerformLayout()
    {
        if (Child is { } child)
        {
            child.Layout(BoxConstraints.Unbounded, parentUsesSize: true);
            Size = _fit == BoxFit.ScaleDown
                ? Constraints.Constrain(
                    Constraints.Loosen().ConstrainSizeAndAttemptToPreserveAspectRatio(child.Size))
                : Constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(child.Size);
            ClearPaintData();
        }
        else
        {
            Size = Constraints.Smallest;
        }
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child) =>
        Size.Width != 0 && Size.Height != 0
        && child is RenderBox box && box.Size.Width != 0 && box.Size.Height != 0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (Child is not { } child || !PaintsChild(child))
        {
            return;
        }

        UpdatePaintData();
        if (_hasVisualOverflow == true && _clipBehavior != Clip.None)
        {
            Layer = ctx.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                (context, childOffset) => PaintChildWithTransform(context, childOffset),
                clipBehavior: _clipBehavior,
                oldLayer: Layer as ClipRectLayer);
        }
        else
        {
            Layer = PaintChildWithTransform(ctx, offset);
        }
    }

    /// <remarks>Flutter's <c>RenderFittedBox._paintChildWithTransform</c>: a pure translation is
    /// applied as a paint offset instead of pushing a transform layer.</remarks>
    private TransformLayer? PaintChildWithTransform(PaintingContext context, Point offset)
    {
        Point? childOffset = MatrixUtils.GetAsTranslation(_transform!);
        if (childOffset is null)
        {
            return context.PushTransform(
                NeedsCompositing,
                offset,
                _transform!,
                base.Paint,
                Layer as TransformLayer);
        }

        base.Paint(context, offset + childOffset.Value);
        return null;
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is not { } child || !PaintsChild(child))
        {
            return false;
        }

        UpdatePaintData();
        return result.AddWithPaintTransform(
            _transform,
            position,
            (hitResult, hitPosition) => base.HitTestChildren(hitResult, hitPosition));
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
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
        _hasVisualOverflow = null;
        _transform = null;
    }

    /// <remarks>Flutter's <c>RenderFittedBox._updatePaintData</c>.</remarks>
    private void UpdatePaintData()
    {
        if (_transform != null)
        {
            return;
        }

        if (Child is not { } child)
        {
            _hasVisualOverflow = false;
            _transform = Matrix4.Identity();
            return;
        }

        Alignment resolvedAlignment = Resolve();
        Size childSize = child.Size;
        FittedSizes fittedSizes = BoxFitUtils.ApplyBoxFit(_fit, childSize, Size);
        double scaleX = fittedSizes.Destination.Width / fittedSizes.Source.Width;
        double scaleY = fittedSizes.Destination.Height / fittedSizes.Source.Height;
        Rect sourceRect = resolvedAlignment.Inscribe(
            fittedSizes.Source,
            new Rect(new Point(0, 0), childSize));
        Rect destinationRect = resolvedAlignment.Inscribe(
            fittedSizes.Destination,
            new Rect(new Point(0, 0), Size));
        _hasVisualOverflow =
            sourceRect.Width < childSize.Width || sourceRect.Height < childSize.Height;

        Matrix4 transform = Matrix4.TranslationValues(destinationRect.Left, destinationRect.Top, 0.0);
        transform.ScaleByDouble(scaleX, scaleY, 1.0, 1);
        transform.TranslateByDouble(-sourceRect.Left, -sourceRect.Top, 0, 1);
        _transform = transform;
    }

    /// <summary>Whether the fitted child is larger than the source rectangle it was inscribed into.</summary>
    /// <remarks>Flutter's <c>RenderFittedBox._hasVisualOverflow</c>.</remarks>
    public bool HasVisualOverflow
    {
        get
        {
            UpdatePaintData();
            return _hasVisualOverflow ?? false;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<BoxFit>("fit", Fit));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
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
        return oldLayer as OpacityLayer ?? new OpacityLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityLayer opacityLayer)
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
    private AlignmentGeometry? _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;
    private FilterQuality? _filterQuality;

    public RenderTransform(
        Matrix4 transform,
        AlignmentGeometry? alignment,
        RenderBox? child,
        FilterQuality? filterQuality = null,
        Point? origin = null,
        bool transformHitTests = true,
        TextDirection? textDirection = null)
    {
        _transform = Matrix4.Copy(transform);
        _alignment = alignment;
        _textDirection = textDirection;
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
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public AlignmentGeometry? Alignment
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

    private Alignment? ResolvedAlignment =>
        _alignment is { } alignment ? _resolvedAlignment ??= alignment.Resolve(_textDirection) : null;

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
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
                MarkNeedsPaint();
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
            Alignment? resolvedAlignment = ResolvedAlignment;
            if (_origin is null && resolvedAlignment is null)
            {
                return _transform;
            }

            Matrix4 result = Matrix4.Identity();
            if (_origin is { } origin)
            {
                result.TranslateByDouble(origin.X, origin.Y, 0, 1);
            }

            Point translation = default;
            if (resolvedAlignment is { } alignment)
            {
                translation = alignment.AlongSize(Size);
                result.TranslateByDouble(translation.X, translation.Y, 0, 1);
            }

            result.Multiply(_transform);

            if (resolvedAlignment is not null)
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
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>Resets the transform to the identity.</summary>
    public void SetIdentity()
    {
        _transform.SetIdentity();
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the x axis.</summary>
    public void RotateX(double radians)
    {
        _transform.RotateX(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the y axis.</summary>
    public void RotateY(double radians)
    {
        _transform.RotateY(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a rotation about the z axis.</summary>
    public void RotateZ(double radians)
    {
        _transform.RotateZ(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a translation.</summary>
    public void Translate(double x, double y = 0.0, double z = 0.0)
    {
        _transform.TranslateByDouble(x, y, z, 1);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Post-multiplies the transform by a scale.</summary>
    public void Scale(double x, double? y = null, double? z = null)
    {
        _transform.ScaleByDouble(x, y ?? x, z ?? x, 1);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }


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
        ArgumentNullException.ThrowIfNull(ctx);
        if (Child is null)
        {
            return;
        }

        Matrix4 transform = EffectiveTransform;
        if (FilterQuality is null)
        {
            Point? childOffset = MatrixUtils.GetAsTranslation(transform);
            if (childOffset is null)
            {
                double determinant = transform.Determinant();
                if (determinant == 0.0 || !double.IsFinite(determinant))
                {
                    Layer = null;
                    return;
                }

                Layer = ctx.PushTransform(NeedsCompositing, offset, transform, base.Paint, Layer as TransformLayer);
            }
            else
            {
                base.Paint(ctx, offset + childOffset.Value);
                Layer = null;
            }

            return;
        }

        // Dart wraps the transform in an `ImageFilterLayer` built from `ImageFilter.matrix`, which
        // Avalonia's drawing backend has no counterpart for; the transform layer carries the sampling
        // quality instead (docs/ai/DIVERGENCES.md).
        TransformLayer filteredLayer = Layer as TransformLayer ?? new TransformLayer();
        filteredLayer.FilterQuality = FilterQuality;
        Layer = ctx.PushTransform(true, offset, transform, base.Paint, filteredLayer);
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
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
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

        Layer = ctx.PushTransform(
            NeedsCompositing,
            offset,
            _paintTransform,
            (transformedContext, transformedOffset) => transformedContext.PaintChild(Child, transformedOffset),
            Layer as TransformLayer);
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

/// <summary>
/// Clips its child to a rectangle.
/// </summary>
/// <remarks>Flutter's <c>RenderClipRect</c>.</remarks>
public sealed class RenderClipRect : RenderCustomClip<Rect>
{
    public RenderClipRect(
        RenderBox? child = null,
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.HardEdge) : base(child, clipper, clipBehavior)
    {
    }

    /// <inheritdoc />
    protected override Rect DefaultClip => new(new Point(0, 0), Size);

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            Layer = null;
            return;
        }

        Layer = context.PushClipRect(
            NeedsCompositing,
            offset,
            EffectiveClip,
            base.Paint,
            ClipBehavior,
            Layer as ClipRectLayer);
    }

    /// <inheritdoc />
    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null && !EffectiveClip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        Rect clip = EffectiveClip;
        context.Canvas.DrawGeometry(
            null,
            RenderCustomClipDebug.DebugPen,
            new RectangleGeometry(new Rect(clip.Position + offset, clip.Size)));
        RenderCustomClipDebug.PaintScissors(context, offset, clip.Width);
    }
}

/// <summary>
/// Clips its child to a rounded rectangle.
/// </summary>
/// <remarks>Flutter's <c>RenderClipRRect</c>.</remarks>
public sealed class RenderClipRRect : RenderCustomClip<RRect>
{
    private BorderRadiusGeometry _borderRadius;
    private TextDirection? _textDirection;

    public RenderClipRRect(
        RenderBox? child = null,
        BorderRadiusGeometry? borderRadius = null,
        CustomClipper<RRect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        TextDirection? textDirection = null) : base(child, clipper, clipBehavior)
    {
        _borderRadius = borderRadius ?? Rendering.BorderRadius.Zero;
        _textDirection = textDirection;
    }

    /// <summary>The border radius of the rounded corners.</summary>
    public BorderRadiusGeometry BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            MarkNeedsClip();
        }
    }

    /// <summary>The text direction with which to resolve a directional <see cref="BorderRadius"/>.</summary>
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
            MarkNeedsClip();
        }
    }

    /// <inheritdoc />
    protected override RRect DefaultClip => RRect.FromRectAndCorners(
        new Rect(new Point(0, 0), Size),
        _borderRadius.Resolve(_textDirection ?? Plumix.UI.TextDirection.Ltr));

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            Layer = null;
            return;
        }

        RRect clip = EffectiveClip;
        Layer = context.PushClipRRect(
            NeedsCompositing,
            offset,
            clip.Rect,
            clip,
            base.Paint,
            ClipBehavior,
            Layer as ClipRRectLayer);
    }

    /// <inheritdoc />
    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    /// <inheritdoc />
    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return ClipBehavior == Clip.None ? null : EffectiveClip.Rect;
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        RRect clip = EffectiveClip;
        if (!Rendering.Layer.ContainsRoundedRect(clip.Rect, clip.Radii, position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        RRect clip = EffectiveClip;
        var path = new Plumix.UI.Path();
        path.AddRRect(RRect.FromRectAndCorners(
            new Rect(clip.Rect.Position + offset, clip.Rect.Size),
            clip.Radii));
        context.Canvas.DrawPath(path, brush: null, pen: RenderCustomClipDebug.DebugPen);
        RenderCustomClipDebug.PaintScissors(context, offset, clip.Width);
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
        DebugHandleEvent(@event, entry);
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
    private int _propertyBatchDepth;
    private bool _propertyBatchMarkedDirty;

    /// <summary>
    /// Suppresses the per-property semantics invalidation until the returned scope is disposed, then
    /// fires it once if any property changed.
    /// </summary>
    /// <remarks>
    /// Flutter has a single <c>properties</c> setter that assigns a <c>SemanticsProperties</c> value
    /// object and calls <c>markNeedsSemanticsUpdate()</c> once. Plumix exposes one setter per property,
    /// so an update that changes several of them would otherwise invalidate — and immediately
    /// re-collect — the semantics configuration in the middle of the batch, capturing the callbacks
    /// that had not been assigned yet.
    /// </remarks>
    internal PropertyBatch BeginPropertyBatch() => new(this);

    private void MarkNeedsSemanticsUpdateBatched()
    {
        if (_propertyBatchDepth > 0)
        {
            _propertyBatchMarkedDirty = true;
            return;
        }

        MarkNeedsSemanticsUpdate();
    }

    internal readonly struct PropertyBatch : IDisposable
    {
        private readonly RenderSemanticsAnnotations _owner;

        internal PropertyBatch(RenderSemanticsAnnotations owner)
        {
            _owner = owner;
            _owner._propertyBatchDepth += 1;
        }

        public void Dispose()
        {
            _owner._propertyBatchDepth -= 1;
            if (_owner._propertyBatchDepth > 0 || !_owner._propertyBatchMarkedDirty)
            {
                return;
            }

            _owner._propertyBatchMarkedDirty = false;
            _owner.MarkNeedsSemanticsUpdate();
        }
    }

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
    private Action? _onDidGainAccessibilityFocus;
    private Action? _onDidLoseAccessibilityFocus;
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? Hint
    {
        get => _hint;
        set
        {
            if (_hint == value) return;
            _hint = value;
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? IncreasedValue
    {
        get => _increasedValue;
        set
        {
            if (_increasedValue == value) return;
            _increasedValue = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? DecreasedValue
    {
        get => _decreasedValue;
        set
        {
            if (_decreasedValue == value) return;
            _decreasedValue = value;
            MarkNeedsSemanticsUpdateBatched();
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
            if (hadHandler != (value is not null)) MarkNeedsSemanticsUpdateBatched();
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
            if (hadHandler != (value is not null)) MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? MinValue
    {
        get => _minValue;
        set
        {
            if (_minValue == value) return;
            _minValue = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public string? MaxValue
    {
        get => _maxValue;
        set
        {
            if (_maxValue == value) return;
            _maxValue = value;
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public SemanticsRole Role
    {
        get => _role;
        set
        {
            if (_role == value) return;
            _role = value;
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public Action? OnDismiss
    {
        get => _onDismiss;
        set
        {
            if (ReferenceEquals(_onDismiss, value)) return;
            _onDismiss = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public Action? OnExpand
    {
        get => _onExpand;
        set
        {
            if (ReferenceEquals(_onExpand, value)) return;
            _onExpand = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public Action? OnCollapse
    {
        get => _onCollapse;
        set
        {
            if (ReferenceEquals(_onCollapse, value)) return;
            _onCollapse = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public Action? OnFocus
    {
        get => _onFocus;
        set
        {
            if (ReferenceEquals(_onFocus, value)) return;
            _onFocus = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    /// <remarks>Flutter's <c>SemanticsProperties.onDidGainAccessibilityFocus</c>.</remarks>
    public Action? OnDidGainAccessibilityFocus
    {
        get => _onDidGainAccessibilityFocus;
        set
        {
            if (ReferenceEquals(_onDidGainAccessibilityFocus, value)) return;
            _onDidGainAccessibilityFocus = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    /// <remarks>Flutter's <c>SemanticsProperties.onDidLoseAccessibilityFocus</c>.</remarks>
    public Action? OnDidLoseAccessibilityFocus
    {
        get => _onDidLoseAccessibilityFocus;
        set
        {
            if (ReferenceEquals(_onDidLoseAccessibilityFocus, value)) return;
            _onDidLoseAccessibilityFocus = value;
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public Action? OnLongPress
    {
        get => _onLongPress;
        set
        {
            if (ReferenceEquals(_onLongPress, value)) return;
            _onLongPress = value;
            MarkNeedsSemanticsUpdateBatched();
        }
    }

    public bool LiveRegion
    {
        get => _liveRegion;
        set
        {
            if (_liveRegion == value) return;
            _liveRegion = value;
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            MarkNeedsSemanticsUpdateBatched();
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
            && _onDidGainAccessibilityFocus is null
            && _onDidLoseAccessibilityFocus is null
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
        if (_onDidGainAccessibilityFocus is not null)
        {
            configuration.OnDidGainAccessibilityFocus = _onDidGainAccessibilityFocus;
        }
        if (_onDidLoseAccessibilityFocus is not null)
        {
            configuration.OnDidLoseAccessibilityFocus = _onDidLoseAccessibilityFocus;
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
            ctx.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                (clippedContext, clippedOffset) =>
                {
                    PaintSplash(clippedContext, clippedOffset);
                    base.Paint(clippedContext, clippedOffset);
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
        ctx.Canvas.DrawCircle(brush, pen: null, center: offset + resolvedOrigin, radius: radius);
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
