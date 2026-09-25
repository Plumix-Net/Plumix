using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>
/// A base class for render boxes that resemble their children.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderProxyBox</c>. C# has no mixins, so the bodies of
/// <c>RenderObjectWithChildMixin</c> and <c>RenderProxyBoxMixin</c> are folded into this class.
/// </remarks>
public class RenderProxyBox : RenderBox, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    /// <summary>Creates a proxy render box. Proxy render boxes are rarely created directly because
    /// they proxy the render box protocol to <paramref name="child"/>.</summary>
    public RenderProxyBox(RenderBox? child = null)
    {
        Child = child;
    }

    /// <summary>Dart's <c>RenderObjectWithChildMixin.child</c>: drops the old child and adopts the
    /// new one without an equality check; adoption and dropping mark this object dirty.</summary>
    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;

            if (_child != null)
            {
                AdoptChild(_child);
            }
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        // We don't actually use the offset argument in BoxParentData, so let's
        // avoid allocating it at all.
        if (child.parentData is not ParentData)
        {
            child.parentData = new ParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return Child?.GetMinIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Child?.GetMaxIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return Child?.GetMinIntrinsicHeight(width) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return Child?.GetMaxIntrinsicHeight(width) ?? 0.0;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        return Child?.GetDistanceToActualBaseline(baseline) ?? base.ComputeDistanceToActualBaseline(baseline);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return Child?.GetDryBaseline(constraints, baseline) ?? base.ComputeDryBaseline(constraints, baseline);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return Child?.GetDryLayout(constraints) ?? ComputeSizeForNoChild(constraints);
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(Constraints, parentUsesSize: true);
            Size = Child.Size;
        }
        else
        {
            Size = ComputeSizeForNoChild(Constraints);
        }
    }

    /// <summary>
    /// Dart's <c>RenderProxyBoxMixin.computeSizeForNoChild</c>: the size to take when there is no child.
    /// Read by both <see cref="PerformLayout"/> and <see cref="ComputeDryLayout"/>.
    /// </summary>
    public virtual Size ComputeSizeForNoChild(BoxConstraints constraints)
    {
        return constraints.Smallest;
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return Child?.HitTest(result, position) ?? false;
    }

    /// <summary>Dart's <c>RenderProxyBoxMixin.applyPaintTransform</c>: the child is painted at the
    /// proxy's own offset, so no transform is contributed.</summary>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        RenderBox? child = Child;
        if (child == null)
        {
            return;
        }

        context.PaintChild(child, offset);
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>A <see cref="RenderProxyBox"/> subclass that allows you to customize the hit-testing behavior.
/// </summary>
/// <remarks>Flutter's <c>RenderProxyBoxWithHitTestBehavior</c>.</remarks>
public abstract class RenderProxyBoxWithHitTestBehavior : RenderProxyBox
{
    /// <summary>Initializes member variables for subclasses.</summary>
    protected RenderProxyBoxWithHitTestBehavior(
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null) : base(child)
    {
        Behavior = behavior;
    }

    /// <summary>How to behave during hit testing when deciding how the hit test propagates to children
    /// and whether to consider targets behind this one.</summary>
    public HitTestBehavior Behavior { get; set; }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        bool hitTarget = false;
        if (Size.Contains(position))
        {
            hitTarget = HitTestChildren(result, position) || HitTestSelf(position);
            if (hitTarget || Behavior == HitTestBehavior.Translucent)
            {
                result.Add(new BoxHitTestEntry(this, position));
            }
        }

        return hitTarget;
    }

    protected override bool HitTestSelf(Point position) => Behavior == HitTestBehavior.Opaque;

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

/// <summary>Imposes additional constraints on its child.</summary>
/// <remarks>Flutter's <c>RenderConstrainedBox</c>.</remarks>
public class RenderConstrainedBox : RenderProxyBox
{
    private BoxConstraints _additionalConstraints;

    /// <summary>Creates a render box that constrains its child.</summary>
    public RenderConstrainedBox(BoxConstraints additionalConstraints, RenderBox? child = null) : base(child)
    {
        if (Constants.KDebugMode && !additionalConstraints.DebugAssertIsValid())
        {
            throw new AssertionError("'additionalConstraints.debugAssertIsValid()': is not true.");
        }

        _additionalConstraints = additionalConstraints;
    }

    /// <summary>Additional constraints to apply to <see cref="RenderProxyBox.Child"/> during layout.</summary>
    public BoxConstraints AdditionalConstraints
    {
        get => _additionalConstraints;
        set
        {
            if (Constants.KDebugMode && !value.DebugAssertIsValid())
            {
                throw new AssertionError("'value.debugAssertIsValid()': is not true.");
            }

            if (_additionalConstraints == value)
            {
                return;
            }

            _additionalConstraints = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (_additionalConstraints.HasBoundedWidth && _additionalConstraints.HasTightWidth)
        {
            return _additionalConstraints.MinWidth;
        }

        double width = base.ComputeMinIntrinsicWidth(height);
        Debug.Assert(double.IsFinite(width));
        if (!_additionalConstraints.HasInfiniteWidth)
        {
            return _additionalConstraints.ConstrainWidth(width);
        }

        return width;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (_additionalConstraints.HasBoundedWidth && _additionalConstraints.HasTightWidth)
        {
            return _additionalConstraints.MinWidth;
        }

        double width = base.ComputeMaxIntrinsicWidth(height);
        Debug.Assert(double.IsFinite(width));
        if (!_additionalConstraints.HasInfiniteWidth)
        {
            return _additionalConstraints.ConstrainWidth(width);
        }

        return width;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (_additionalConstraints.HasBoundedHeight && _additionalConstraints.HasTightHeight)
        {
            return _additionalConstraints.MinHeight;
        }

        double height = base.ComputeMinIntrinsicHeight(width);
        Debug.Assert(double.IsFinite(height));
        if (!_additionalConstraints.HasInfiniteHeight)
        {
            return _additionalConstraints.ConstrainHeight(height);
        }

        return height;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (_additionalConstraints.HasBoundedHeight && _additionalConstraints.HasTightHeight)
        {
            return _additionalConstraints.MinHeight;
        }

        double height = base.ComputeMaxIntrinsicHeight(width);
        Debug.Assert(double.IsFinite(height));
        if (!_additionalConstraints.HasInfiniteHeight)
        {
            return _additionalConstraints.ConstrainHeight(height);
        }

        return height;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return Child?.GetDryBaseline(_additionalConstraints.Enforce(constraints), baseline);
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        if (Child != null)
        {
            Child.Layout(_additionalConstraints.Enforce(constraints), parentUsesSize: true);
            Size = Child.Size;
        }
        else
        {
            Size = _additionalConstraints.Enforce(constraints).Constrain(new Size());
        }
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return Child?.GetDryLayout(_additionalConstraints.Enforce(constraints))
            ?? _additionalConstraints.Enforce(constraints).Constrain(new Size());
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        base.DebugPaintSize(context, offset);
        if (Constants.KDebugMode)
        {
            // Dart's `child!.size.isEmpty`.
            if (Child == null || Child.Size.Width <= 0.0 || Child.Size.Height <= 0.0)
            {
                context.Canvas.DrawRectangle(
                    new SolidColorBrush(new Color(0x90909090)),
                    null,
                    new Rect(offset, Size));
            }
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<BoxConstraints>("additionalConstraints", AdditionalConstraints));
    }
}

/// <summary>Constrains the child's maximum width and height if it is not otherwise constrained.</summary>
/// <remarks>Flutter's <c>RenderLimitedBox</c>.</remarks>
public class RenderLimitedBox : RenderProxyBox
{
    private double _maxWidth;
    private double _maxHeight;

    /// <summary>Creates a render box that imposes a maximum width or maximum height on its child if the
    /// child is otherwise unconstrained.</summary>
    public RenderLimitedBox(
        RenderBox? child = null,
        double maxWidth = double.PositiveInfinity,
        double maxHeight = double.PositiveInfinity) : base(child)
    {
        if (Constants.KDebugMode && !(maxWidth >= 0.0))
        {
            throw new AssertionError("'maxWidth >= 0.0': is not true.");
        }

        if (Constants.KDebugMode && !(maxHeight >= 0.0))
        {
            throw new AssertionError("'maxHeight >= 0.0': is not true.");
        }

        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
    }

    /// <summary>The value to use for maxWidth if the incoming maxWidth constraint is infinite.</summary>
    public double MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (Constants.KDebugMode && !(value >= 0.0))
            {
                throw new AssertionError("'value >= 0.0': is not true.");
            }

            if (_maxWidth == value)
            {
                return;
            }

            _maxWidth = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The value to use for maxHeight if the incoming maxHeight constraint is infinite.</summary>
    public double MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (Constants.KDebugMode && !(value >= 0.0))
            {
                throw new AssertionError("'value >= 0.0': is not true.");
            }

            if (_maxHeight == value)
            {
                return;
            }

            _maxHeight = value;
            MarkNeedsLayout();
        }
    }

    private BoxConstraints LimitConstraints(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: constraints.MinWidth,
            MaxWidth: constraints.HasBoundedWidth ? constraints.MaxWidth : constraints.ConstrainWidth(MaxWidth),
            MinHeight: constraints.MinHeight,
            MaxHeight: constraints.HasBoundedHeight ? constraints.MaxHeight : constraints.ConstrainHeight(MaxHeight));
    }

    private Size ComputeSize(BoxConstraints constraints, Func<RenderBox, BoxConstraints, Size> layoutChild)
    {
        if (Child != null)
        {
            Size childSize = layoutChild(Child, LimitConstraints(constraints));
            return constraints.Constrain(childSize);
        }

        return LimitConstraints(constraints).Constrain(new Size());
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(Constraints, ChildLayoutHelper.LayoutChild);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, defaultValue: double.PositiveInfinity));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, defaultValue: double.PositiveInfinity));
    }
}

/// <summary>Attempts to size the child to a specific aspect ratio.</summary>
/// <remarks>Flutter's <c>RenderAspectRatio</c>.</remarks>
public class RenderAspectRatio : RenderProxyBox
{
    private double _aspectRatio;

    /// <summary>Creates as render object with a specific aspect ratio.</summary>
    public RenderAspectRatio(double aspectRatio, RenderBox? child = null) : base(child)
    {
        if (Constants.KDebugMode && !(aspectRatio > 0.0))
        {
            throw new AssertionError("'aspectRatio > 0.0': is not true.");
        }

        if (Constants.KDebugMode && !double.IsFinite(aspectRatio))
        {
            throw new AssertionError("'aspectRatio.isFinite': is not true.");
        }

        _aspectRatio = aspectRatio;
    }

    /// <summary>The aspect ratio to attempt to use.</summary>
    public double AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (Constants.KDebugMode && !(value > 0.0))
            {
                throw new AssertionError("'value > 0.0': is not true.");
            }

            if (Constants.KDebugMode && !double.IsFinite(value))
            {
                throw new AssertionError("'value.isFinite': is not true.");
            }

            if (_aspectRatio == value)
            {
                return;
            }

            _aspectRatio = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (double.IsFinite(height))
        {
            return height * _aspectRatio;
        }

        return Child?.GetMinIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (double.IsFinite(height))
        {
            return height * _aspectRatio;
        }

        return Child?.GetMaxIntrinsicWidth(height) ?? 0.0;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (double.IsFinite(width))
        {
            return width / _aspectRatio;
        }

        return Child?.GetMinIntrinsicHeight(width) ?? 0.0;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (double.IsFinite(width))
        {
            return width / _aspectRatio;
        }

        return Child?.GetMaxIntrinsicHeight(width) ?? 0.0;
    }

    private Size ApplyAspectRatio(BoxConstraints constraints)
    {
        if (Constants.KDebugMode && !constraints.DebugAssertIsValid())
        {
            throw new AssertionError("'constraints.debugAssertIsValid()': is not true.");
        }

        if (Constants.KDebugMode && !constraints.HasBoundedWidth && !constraints.HasBoundedHeight)
        {
            string runtimeType = GetType().Name;
            throw new FlutterError(
                $"{runtimeType} has unbounded constraints.\n"
                + $"This {runtimeType} was given an aspect ratio of {DartDoubleToString(AspectRatio)} but was given "
                + "both unbounded width and unbounded height constraints. Because both "
                + "constraints were unbounded, this render object doesn't know how much "
                + "size to consume.");
        }

        if (constraints.IsTight)
        {
            return constraints.Smallest;
        }

        double width = constraints.MaxWidth;
        double height;

        // We default to picking the height based on the width, but if the width
        // would be infinite, that's not sensible so we try to infer the height
        // from the width.
        if (double.IsFinite(width))
        {
            height = width / _aspectRatio;
        }
        else
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        // Similar to RenderImage, we iteratively attempt to fit within the given
        // constraints while maintaining the given aspect ratio. The order of
        // applying the constraints is also biased towards inferring the height
        // from the width.

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

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ApplyAspectRatio(constraints);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return base.ComputeDryBaseline(BoxConstraints.Tight(GetDryLayout(constraints)), baseline);
    }

    protected override void PerformLayout()
    {
        Size = GetDryLayout(Constraints);
        Child?.Layout(BoxConstraints.Tight(Size));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("aspectRatio", AspectRatio));
    }

    /// <summary>Dart's <c>double.toString</c> for the unbounded-constraints error text: integral values keep
    /// a <c>.0</c> suffix.</summary>
    private static string DartDoubleToString(double value)
    {
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return double.IsFinite(value) && value == Math.Floor(value) && !text.Contains('E') ? text + ".0" : text;
    }
}

/// <summary>Sizes its child to the child's maximum intrinsic width.</summary>
/// <remarks>Flutter's <c>RenderIntrinsicWidth</c>.</remarks>
public class RenderIntrinsicWidth : RenderProxyBox
{
    private double? _stepWidth;
    private double? _stepHeight;

    /// <summary>Creates a render object that sizes itself to its child's intrinsic width.</summary>
    public RenderIntrinsicWidth(
        double? stepWidth = null,
        double? stepHeight = null,
        RenderBox? child = null) : base(child)
    {
        if (Constants.KDebugMode && !(stepWidth == null || stepWidth > 0.0))
        {
            throw new AssertionError("'stepWidth == null || stepWidth > 0.0': is not true.");
        }

        if (Constants.KDebugMode && !(stepHeight == null || stepHeight > 0.0))
        {
            throw new AssertionError("'stepHeight == null || stepHeight > 0.0': is not true.");
        }

        _stepWidth = stepWidth;
        _stepHeight = stepHeight;
    }

    /// <summary>If non-null, force the child's width to be a multiple of this value.</summary>
    public double? StepWidth
    {
        get => _stepWidth;
        set
        {
            if (Constants.KDebugMode && !(value == null || value > 0.0))
            {
                throw new AssertionError("'value == null || value > 0.0': is not true.");
            }

            if (value == _stepWidth)
            {
                return;
            }

            _stepWidth = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>If non-null, force the child's height to be a multiple of this value.</summary>
    public double? StepHeight
    {
        get => _stepHeight;
        set
        {
            if (Constants.KDebugMode && !(value == null || value > 0.0))
            {
                throw new AssertionError("'value == null || value > 0.0': is not true.");
            }

            if (value == _stepHeight)
            {
                return;
            }

            _stepHeight = value;
            MarkNeedsLayout();
        }
    }

    private static double ApplyStep(double input, double? step)
    {
        Debug.Assert(double.IsFinite(input));
        if (step == null)
        {
            return input;
        }

        return Math.Ceiling(input / step.Value) * step.Value;
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return GetMaxIntrinsicWidth(height);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (Child == null)
        {
            return 0.0;
        }

        double width = Child.GetMaxIntrinsicWidth(height);
        return ApplyStep(width, _stepWidth);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (Child == null)
        {
            return 0.0;
        }

        if (!double.IsFinite(width))
        {
            width = GetMaxIntrinsicWidth(double.PositiveInfinity);
        }

        Debug.Assert(double.IsFinite(width));
        double height = Child.GetMinIntrinsicHeight(width);
        return ApplyStep(height, _stepHeight);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (Child == null)
        {
            return 0.0;
        }

        if (!double.IsFinite(width))
        {
            width = GetMaxIntrinsicWidth(double.PositiveInfinity);
        }

        Debug.Assert(double.IsFinite(width));
        double height = Child.GetMaxIntrinsicHeight(width);
        return ApplyStep(height, _stepHeight);
    }

    private BoxConstraints ChildConstraints(RenderBox child, BoxConstraints constraints)
    {
        return constraints.Tighten(
            width: constraints.HasTightWidth
                ? null
                : ApplyStep(child.GetMaxIntrinsicWidth(constraints.MaxHeight), _stepWidth),
            height: StepHeight == null
                ? null
                : ApplyStep(child.GetMaxIntrinsicHeight(constraints.MaxWidth), _stepHeight));
    }

    private Size ComputeSize(Func<RenderBox, BoxConstraints, Size> layoutChild, BoxConstraints constraints)
    {
        RenderBox? child = Child;
        return child == null
            ? constraints.Smallest
            : layoutChild(child, ChildConstraints(child, constraints));
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ComputeSize(ChildLayoutHelper.DryLayoutChild, constraints);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        return child?.GetDryBaseline(ChildConstraints(child, constraints), baseline);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(ChildLayoutHelper.LayoutChild, Constraints);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("stepWidth", StepWidth));
        properties.Add(new DoubleProperty("stepHeight", StepHeight));
    }
}

/// <summary>Sizes its child to the child's intrinsic height.</summary>
/// <remarks>Flutter's <c>RenderIntrinsicHeight</c>.</remarks>
public class RenderIntrinsicHeight : RenderProxyBox
{
    /// <summary>Creates a render object that sizes itself to its child's intrinsic height.</summary>
    public RenderIntrinsicHeight(RenderBox? child = null) : base(child)
    {
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (Child == null)
        {
            return 0.0;
        }

        if (!double.IsFinite(height))
        {
            height = Child.GetMaxIntrinsicHeight(double.PositiveInfinity);
        }

        Debug.Assert(double.IsFinite(height));
        return Child.GetMinIntrinsicWidth(height);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (Child == null)
        {
            return 0.0;
        }

        if (!double.IsFinite(height))
        {
            height = Child.GetMaxIntrinsicHeight(double.PositiveInfinity);
        }

        Debug.Assert(double.IsFinite(height));
        return Child.GetMaxIntrinsicWidth(height);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return GetMaxIntrinsicHeight(width);
    }

    private BoxConstraints ChildConstraints(RenderBox child, BoxConstraints constraints)
    {
        return constraints.HasTightHeight
            ? constraints
            : constraints.Tighten(height: child.GetMaxIntrinsicHeight(constraints.MaxWidth));
    }

    private Size ComputeSize(Func<RenderBox, BoxConstraints, Size> layoutChild, BoxConstraints constraints)
    {
        RenderBox? child = Child;
        return child == null
            ? constraints.Smallest
            : layoutChild(child, ChildConstraints(child, constraints));
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ComputeSize(ChildLayoutHelper.DryLayoutChild, constraints);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        return child?.GetDryBaseline(ChildConstraints(child, constraints), baseline);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(ChildLayoutHelper.LayoutChild, Constraints);
    }
}

/// <summary>Excludes the child from baseline computations in the parent.</summary>
/// <remarks>Flutter's <c>RenderIgnoreBaseline</c>.</remarks>
public class RenderIgnoreBaseline : RenderProxyBox
{
    /// <summary>Create a render object that causes the parent to ignore the child for baseline
    /// computations.</summary>
    public RenderIgnoreBaseline(RenderBox? child = null) : base(child)
    {
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => null;

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => null;
}

/// <summary>Makes its child partially transparent.</summary>
/// <remarks>Flutter's <c>RenderOpacity</c>.</remarks>
public class RenderOpacity : RenderProxyBox
{
    private int _alpha;
    private double _opacity;
    private bool _alwaysIncludeSemantics;

    /// <summary>Creates a partially transparent render object.</summary>
    public RenderOpacity(
        double opacity = 1.0,
        bool alwaysIncludeSemantics = false,
        RenderBox? child = null) : base(child)
    {
        if (Constants.KDebugMode && !(opacity >= 0.0 && opacity <= 1.0))
        {
            throw new AssertionError("'opacity >= 0.0 && opacity <= 1.0': is not true.");
        }

        _opacity = opacity;
        _alwaysIncludeSemantics = alwaysIncludeSemantics;
        _alpha = Color.GetAlphaFromOpacity(opacity);
    }

    /// <inheritdoc />
    public override bool AlwaysNeedsCompositing => Child != null && _alpha > 0;

    /// <inheritdoc />
    public override bool IsRepaintBoundary => AlwaysNeedsCompositing;

    /// <summary>The fraction to scale the child's alpha value.</summary>
    public double Opacity
    {
        get => _opacity;
        set
        {
            if (Constants.KDebugMode && !(value >= 0.0 && value <= 1.0))
            {
                throw new AssertionError("'value >= 0.0 && value <= 1.0': is not true.");
            }

            if (_opacity == value)
            {
                return;
            }

            bool didNeedCompositing = AlwaysNeedsCompositing;
            bool wasVisible = _alpha != 0;
            _opacity = value;
            _alpha = Color.GetAlphaFromOpacity(_opacity);
            if (didNeedCompositing != AlwaysNeedsCompositing)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsCompositedLayerUpdate();
            if (wasVisible != (_alpha != 0) && !AlwaysIncludeSemantics)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>Whether child semantics are included regardless of the opacity.</summary>
    public bool AlwaysIncludeSemantics
    {
        get => _alwaysIncludeSemantics;
        set
        {
            if (value == _alwaysIncludeSemantics)
            {
                return;
            }

            _alwaysIncludeSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return _alpha > 0;
    }

    /// <summary>Dart's <c>RenderOpacity.updateCompositedLayer</c> (construction half); the cast fails for a
    /// non-<see cref="OpacityLayer"/> like Dart's covariant parameter.</summary>
    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return (OpacityLayer?)oldLayer ?? new OpacityLayer();
    }

    /// <summary>Dart's <c>RenderOpacity.updateCompositedLayer</c> (configuration half).</summary>
    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        ((OpacityLayer)layer).Alpha = _alpha;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child == null || _alpha == 0)
        {
            return;
        }

        base.Paint(context, offset);
    }

    /// <inheritdoc />
    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child != null && (_alpha != 0 || AlwaysIncludeSemantics))
        {
            visitor(Child);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            value: AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}
