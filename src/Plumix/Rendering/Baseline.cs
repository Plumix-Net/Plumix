using Avalonia;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity sources:
// flutter/packages/flutter/lib/src/rendering/shifted_box.dart (RenderBaseline)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderIgnoreBaseline)

public sealed class RenderBaseline : RenderProxyBox
{
    private double _baseline;
    private TextBaseline _baselineType;

    public RenderBaseline(
        double baseline,
        TextBaseline baselineType,
        RenderBox? child = null)
    {
        _baseline = baseline;
        _baselineType = baselineType;
        Child = child;
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

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        BoxConstraints childConstraints = Constraints.Loosen();
        Child.Layout(childConstraints, parentUsesSize: true);
        double childBaseline = Child.GetDistanceToBaseline(_baselineType, onlyReal: true) ?? Child.Size.Height;
        double top = _baseline - childBaseline;
        Size = Constraints.Constrain(new Size(Child.Size.Width, top + Child.Size.Height));
        ((BoxParentData)Child.parentData!).offset = new Point(0, top);
    }
}

public sealed class RenderIgnoreBaseline : RenderProxyBox
{
    public RenderIgnoreBaseline(RenderBox? child = null)
    {
        Child = child;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => null;
}
