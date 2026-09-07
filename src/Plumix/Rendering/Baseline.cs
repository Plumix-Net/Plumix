using Avalonia;
using Plumix.UI;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity sources:
// flutter/packages/flutter/lib/src/rendering/shifted_box.dart (RenderBaseline)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIgnoreBaseline)

public sealed class RenderBaseline : RenderShiftedBox
{
    private double _baseline;
    private TextBaseline _baselineType;

    public RenderBaseline(
        double baseline,
        TextBaseline baselineType,
        RenderBox? child = null) : base(child)
    {
        _baseline = baseline;
        _baselineType = baselineType;
    }

    public double Baseline
    {
        get => _baseline;
        set
        {
            if (_baseline.Equals(value))
            {
                return;
            }

            _baseline = value;
            MarkNeedsLayout();
        }
    }

    public TextBaseline BaselineType
    {
        get => _baselineType;
        set
        {
            if (_baselineType == value)
            {
                return;
            }

            _baselineType = value;
            MarkNeedsLayout();
        }
    }

    private (Size Size, double Top) ComputeSizes(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, TextBaseline, double?> getBaseline)
    {
        RenderBox? child = Child;
        if (child is null)
        {
            return (constraints.Smallest, 0.0);
        }

        BoxConstraints childConstraints = constraints.Loosen();
        Size childSize = layoutChild(child, childConstraints);
        double childBaseline = getBaseline(child, childConstraints, _baselineType) ?? childSize.Height;
        double top = _baseline - childBaseline;
        return (constraints.Constrain(new Size(childSize.Width, top + childSize.Height)), top);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        ComputeSizes(constraints, ChildLayoutHelper.DryLayoutChild, ChildLayoutHelper.GetDryBaseline).Size;

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        double? result1 = child?.GetDryBaseline(constraints.Loosen(), baseline);
        double? result2 = child?.GetDryBaseline(constraints.Loosen(), _baselineType);
        if (result1 is null || result2 is null)
        {
            return null;
        }

        return _baseline + result1 - result2;
    }

    protected override void PerformLayout()
    {
        (Size size, double top) =
            ComputeSizes(Constraints, ChildLayoutHelper.LayoutChild, ChildLayoutHelper.GetBaseline);
        Size = size;
        if (Child?.parentData is BoxParentData parentData)
        {
            parentData.offset = new Point(0.0, top);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("baseline", Baseline));
        properties.Add(new EnumProperty<TextBaseline>("baselineType", BaselineType));
    }
}

public sealed class RenderIgnoreBaseline : RenderProxyBox
{
    public RenderIgnoreBaseline(RenderBox? child = null)
    {
        Child = child;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => null;

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => null;
}
