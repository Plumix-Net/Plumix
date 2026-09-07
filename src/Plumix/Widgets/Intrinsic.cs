using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (IntrinsicWidth, IntrinsicHeight)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIntrinsicWidth, RenderIntrinsicHeight)

public sealed class IntrinsicWidth : SingleChildRenderObjectWidget
{
    public IntrinsicWidth(
        Widget? child = null,
        double? stepWidth = null,
        double? stepHeight = null,
        Key? key = null) : base(child, key)
    {
        StepWidth = ValidateWidgetStep(stepWidth, nameof(stepWidth));
        StepHeight = ValidateWidgetStep(stepHeight, nameof(stepHeight));
    }

    public double? StepWidth { get; }
    public double? StepHeight { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIntrinsicWidth(stepWidth: EffectiveStep(StepWidth), stepHeight: EffectiveStep(StepHeight));

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var intrinsicWidth = (RenderIntrinsicWidth)renderObject;
        intrinsicWidth.StepWidth = EffectiveStep(StepWidth);
        intrinsicWidth.StepHeight = EffectiveStep(StepHeight);
    }

    private static double? ValidateWidgetStep(double? value, string parameterName)
    {
        if (Constants.KDebugMode && value is not (null or >= 0.0))
        {
            throw new AssertionError($"{parameterName} must be non-negative.");
        }

        return value;
    }

    private static double? EffectiveStep(double? value) => value == 0.0 ? null : value;
}

public sealed class IntrinsicHeight : SingleChildRenderObjectWidget
{
    public IntrinsicHeight(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderIntrinsicHeight();
}

public sealed class RenderIntrinsicWidth : RenderProxyBox
{
    private double? _stepWidth;
    private double? _stepHeight;

    public RenderIntrinsicWidth(
        double? stepWidth = null,
        double? stepHeight = null,
        RenderBox? child = null)
    {
        ValidateRenderStep(stepWidth, nameof(stepWidth));
        ValidateRenderStep(stepHeight, nameof(stepHeight));
        _stepWidth = stepWidth;
        _stepHeight = stepHeight;
        Child = child;
    }

    public double? StepWidth
    {
        get => _stepWidth;
        set
        {
            ValidateRenderStep(value, nameof(value));
            if (_stepWidth == value) return;
            _stepWidth = value;
            MarkNeedsLayout();
        }
    }

    public double? StepHeight
    {
        get => _stepHeight;
        set
        {
            ValidateRenderStep(value, nameof(value));
            if (_stepHeight == value) return;
            _stepHeight = value;
            MarkNeedsLayout();
        }
    }

    private static void ValidateRenderStep(double? value, string parameterName)
    {
        if (Constants.KDebugMode && value is not (null or > 0.0))
        {
            throw new AssertionError($"{parameterName} must be greater than zero.");
        }
    }

    private static double ApplyStep(double input, double? step)
    {
        Debug.Assert(double.IsFinite(input));
        return step is null ? input : Math.Ceiling(input / step.Value) * step.Value;
    }

    protected override double ComputeMinIntrinsicWidth(double height) => GetMaxIntrinsicWidth(height);

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Child is null ? 0.0 : ApplyStep(Child.GetMaxIntrinsicWidth(height), _stepWidth);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        double probe = double.IsFinite(width) ? width : GetMaxIntrinsicWidth(double.PositiveInfinity);
        return ApplyStep(Child.GetMinIntrinsicHeight(probe), _stepHeight);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        double probe = double.IsFinite(width) ? width : GetMaxIntrinsicWidth(double.PositiveInfinity);
        return ApplyStep(Child.GetMaxIntrinsicHeight(probe), _stepHeight);
    }

    private BoxConstraints ChildConstraints(RenderBox child, BoxConstraints constraints)
    {
        double? width = constraints.HasTightWidth
            ? null
            : ApplyStep(child.GetMaxIntrinsicWidth(constraints.MaxHeight), _stepWidth);
        double? height = _stepHeight is null
            ? null
            : ApplyStep(child.GetMaxIntrinsicHeight(constraints.MaxWidth), _stepHeight);
        return constraints.Tighten(width: width, height: height);
    }

    private Size ComputeSize(BoxConstraints constraints, Func<RenderBox, BoxConstraints, Size> layoutChild)
    {
        RenderBox? child = Child;
        return child is null ? constraints.Smallest : layoutChild(child, ChildConstraints(child, constraints));
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        return child?.GetDryBaseline(ChildConstraints(child, constraints), baseline);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(Constraints, ChildLayoutHelper.LayoutChild);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("stepWidth", StepWidth));
        properties.Add(new DoubleProperty("stepHeight", StepHeight));
    }
}

public sealed class RenderIntrinsicHeight : RenderProxyBox
{
    public RenderIntrinsicHeight(RenderBox? child = null)
    {
        Child = child;
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        double probe = double.IsFinite(height) ? height : Child.GetMaxIntrinsicHeight(double.PositiveInfinity);
        Debug.Assert(double.IsFinite(probe));
        return Child.GetMinIntrinsicWidth(probe);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        double probe = double.IsFinite(height) ? height : Child.GetMaxIntrinsicHeight(double.PositiveInfinity);
        Debug.Assert(double.IsFinite(probe));
        return Child.GetMaxIntrinsicWidth(probe);
    }

    protected override double ComputeMinIntrinsicHeight(double width) => GetMaxIntrinsicHeight(width);

    private static BoxConstraints ChildConstraints(RenderBox child, BoxConstraints constraints) =>
        constraints.HasTightHeight
            ? constraints
            : constraints.Tighten(height: child.GetMaxIntrinsicHeight(constraints.MaxWidth));

    private Size ComputeSize(BoxConstraints constraints, Func<RenderBox, BoxConstraints, Size> layoutChild)
    {
        RenderBox? child = Child;
        return child is null ? constraints.Smallest : layoutChild(child, ChildConstraints(child, constraints));
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        return child?.GetDryBaseline(ChildConstraints(child, constraints), baseline);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(Constraints, ChildLayoutHelper.LayoutChild);
    }
}
