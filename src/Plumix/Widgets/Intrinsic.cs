using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

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

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIntrinsicWidth(stepWidth: EffectiveStep(StepWidth), stepHeight: EffectiveStep(StepHeight));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var intrinsicWidth = (RenderIntrinsicWidth)renderObject;
        intrinsicWidth.StepWidth = EffectiveStep(StepWidth);
        intrinsicWidth.StepHeight = EffectiveStep(StepHeight);
    }

    private static double? ValidateWidgetStep(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Intrinsic steps must be non-negative and finite.");
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

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderIntrinsicHeight();
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

        double? width = null;
        if (!Constraints.HasTightWidth)
        {
            double intrinsicWidth = Child.GetMaxIntrinsicWidth(Constraints.MaxHeight);
            if (intrinsicWidth <= 0.0)
            {
                Child.Layout(IntrinsicProbeConstraints.ForWidth(Constraints), parentUsesSize: true);
                intrinsicWidth = Child.Size.Width;
            }
            width = Constraints.ConstrainWidth(ApplyStep(intrinsicWidth, _stepWidth));
        }

        double? height = null;
        if (_stepHeight.HasValue && !Constraints.HasTightHeight)
        {
            double intrinsicHeight = Child.GetMaxIntrinsicHeight(width ?? Constraints.MaxWidth);
            if (intrinsicHeight <= 0.0)
            {
                Child.Layout(IntrinsicProbeConstraints.ForHeight(Constraints), parentUsesSize: true);
                intrinsicHeight = Child.Size.Height;
            }
            height = Constraints.ConstrainHeight(ApplyStep(intrinsicHeight, _stepHeight));
        }

        var finalConstraints = Constraints.Tighten(width: width, height: height);
        Child.Layout(finalConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = default;
    }

    private static double ApplyStep(double value, double? step) =>
        step.HasValue ? Math.Ceiling(value / step.Value) * step.Value : value;

    private static void ValidateRenderStep(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0.0))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Render intrinsic steps must be positive and finite.");
        }
    }
}

public sealed class RenderIntrinsicHeight : RenderProxyBox
{
    public RenderIntrinsicHeight(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        if (Constraints.HasTightHeight)
        {
            Child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = default;
            return;
        }

        double intrinsicHeight = Child.GetMaxIntrinsicHeight(Constraints.MaxWidth);
        if (intrinsicHeight <= 0.0)
        {
            Child.Layout(IntrinsicProbeConstraints.ForHeight(Constraints), parentUsesSize: true);
            intrinsicHeight = Child.Size.Height;
        }
        double height = Constraints.ConstrainHeight(intrinsicHeight);
        Child.Layout(Constraints.Tighten(height: height), parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = default;
    }
}

internal static class IntrinsicProbeConstraints
{
    public static BoxConstraints ForWidth(BoxConstraints constraints)
    {
        double minHeight = constraints.MinHeight;
        double maxHeight = constraints.MaxHeight;
        if (double.IsFinite(maxHeight))
        {
            minHeight = maxHeight;
        }

        return new BoxConstraints(
            MinWidth: 0.0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: minHeight,
            MaxHeight: maxHeight);
    }

    public static BoxConstraints ForHeight(BoxConstraints constraints)
    {
        double minWidth = constraints.MinWidth;
        double maxWidth = constraints.MaxWidth;
        if (double.IsFinite(maxWidth))
        {
            minWidth = maxWidth;
        }

        return new BoxConstraints(
            MinWidth: minWidth,
            MaxWidth: maxWidth,
            MinHeight: 0.0,
            MaxHeight: double.PositiveInfinity);
    }
}
