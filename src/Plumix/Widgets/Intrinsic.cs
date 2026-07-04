using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (IntrinsicWidth)

public sealed class IntrinsicWidth : SingleChildRenderObjectWidget
{
    public IntrinsicWidth(
        Widget? child = null,
        double? stepWidth = null,
        double? stepHeight = null,
        Key? key = null) : base(child, key)
    {
        StepWidth = ValidateStep(stepWidth, nameof(stepWidth));
        StepHeight = ValidateStep(stepHeight, nameof(stepHeight));
    }

    public double? StepWidth { get; }
    public double? StepHeight { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIntrinsicWidth(stepWidth: StepWidth, stepHeight: StepHeight);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var intrinsicWidth = (RenderIntrinsicWidth)renderObject;
        intrinsicWidth.StepWidth = StepWidth;
        intrinsicWidth.StepHeight = StepHeight;
    }

    private static double? ValidateStep(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Intrinsic steps must be positive and finite.");
        }

        return value;
    }
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
        _stepWidth = stepWidth;
        _stepHeight = stepHeight;
        Child = child;
    }

    public double? StepWidth
    {
        get => _stepWidth;
        set
        {
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
            if (_stepHeight == value) return;
            _stepHeight = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        if (Constraints.HasTightWidth && (!_stepHeight.HasValue || Constraints.HasTightHeight))
        {
            Child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = default;
            return;
        }

        var probeConstraints = new BoxConstraints(
            MinWidth: Constraints.HasTightWidth ? Constraints.MinWidth : 0,
            MaxWidth: Constraints.HasTightWidth ? Constraints.MaxWidth : double.PositiveInfinity,
            MinHeight: Constraints.MinHeight,
            MaxHeight: Constraints.MaxHeight);
        Child.Layout(probeConstraints, parentUsesSize: true);

        var width = Constraints.ConstrainWidth(ApplyStep(Child.Size.Width, _stepWidth));
        double? height = _stepHeight.HasValue
            ? Constraints.ConstrainHeight(ApplyStep(Child.Size.Height, _stepHeight))
            : null;
        var finalConstraints = Constraints.Tighten(width: width, height: height);
        Child.Layout(finalConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = default;
    }

    private static double ApplyStep(double value, double? step) =>
        step.HasValue ? Math.Ceiling(value / step.Value) * step.Value : value;
}
