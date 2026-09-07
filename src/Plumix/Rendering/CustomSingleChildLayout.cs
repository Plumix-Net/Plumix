using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/shifted_box.dart

/// <summary>Controls the size, child constraints, and child position of a custom single-child layout.</summary>
public abstract class SingleChildLayoutDelegate
{
    protected SingleChildLayoutDelegate(IListenable? relayout = null)
    {
        Relayout = relayout;
    }

    internal IListenable? Relayout { get; }

    public virtual Size GetSize(BoxConstraints constraints) => constraints.Biggest;

    public virtual BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => constraints;

    public virtual Point GetPositionForChild(Size size, Size childSize) => default;

    public abstract bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate);
}

/// <summary>Defers its size and child placement to a <see cref="SingleChildLayoutDelegate"/>.</summary>
public sealed class RenderCustomSingleChildLayoutBox : RenderShiftedBox
{
    private SingleChildLayoutDelegate _layoutDelegate;

    public RenderCustomSingleChildLayoutBox(
        SingleChildLayoutDelegate layoutDelegate,
        RenderBox? child = null) : base(child)
    {
        _layoutDelegate = layoutDelegate ?? throw new ArgumentNullException(nameof(layoutDelegate));
    }

    public SingleChildLayoutDelegate LayoutDelegate
    {
        get => _layoutDelegate;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_layoutDelegate, value))
            {
                return;
            }

            SingleChildLayoutDelegate oldDelegate = _layoutDelegate;
            if (value.GetType() != oldDelegate.GetType() || value.ShouldRelayout(oldDelegate))
            {
                MarkNeedsLayout();
            }

            if (Attached)
            {
                oldDelegate.Relayout?.RemoveListener(MarkNeedsLayout);
                value.Relayout?.AddListener(MarkNeedsLayout);
            }

            _layoutDelegate = value;
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _layoutDelegate.Relayout?.AddListener(MarkNeedsLayout);
    }

    protected override void OnDetach()
    {
        _layoutDelegate.Relayout?.RemoveListener(MarkNeedsLayout);
        base.OnDetach();
    }

    private Size GetSize(BoxConstraints constraints) => constraints.Constrain(_layoutDelegate.GetSize(constraints));

    protected override double ComputeMinIntrinsicWidth(double height) => FiniteOrZero(
        GetSize(BoxConstraints.TightForFinite(height: height)).Width);

    protected override double ComputeMaxIntrinsicWidth(double height) => FiniteOrZero(
        GetSize(BoxConstraints.TightForFinite(height: height)).Width);

    protected override double ComputeMinIntrinsicHeight(double width) => FiniteOrZero(
        GetSize(BoxConstraints.TightForFinite(width: width)).Height);

    protected override double ComputeMaxIntrinsicHeight(double width) => FiniteOrZero(
        GetSize(BoxConstraints.TightForFinite(width: width)).Height);

    private static double FiniteOrZero(double value) => double.IsFinite(value) ? value : 0.0;

    protected override Size ComputeDryLayout(BoxConstraints constraints) => GetSize(constraints);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        if (child is null)
        {
            return null;
        }

        BoxConstraints childConstraints = _layoutDelegate.GetConstraintsForChild(constraints);
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result is null)
        {
            return null;
        }

        Size childSize = childConstraints.IsTight
            ? childConstraints.Smallest
            : child.GetDryLayout(childConstraints);
        return result + _layoutDelegate.GetPositionForChild(GetSize(constraints), childSize).Y;
    }

    protected override void PerformLayout()
    {
        Size = GetSize(Constraints);
        if (Child is null)
        {
            return;
        }

        BoxConstraints childConstraints = _layoutDelegate.GetConstraintsForChild(Constraints);
        if (!childConstraints.IsNormalized)
        {
            throw new InvalidOperationException("SingleChildLayoutDelegate returned non-normalized child constraints.");
        }

        Child.Layout(childConstraints, parentUsesSize: !childConstraints.IsTight);
        Size childSize = childConstraints.IsTight ? childConstraints.Smallest : Child.Size;
        ((BoxParentData)Child.parentData!).offset = _layoutDelegate.GetPositionForChild(Size, childSize);
    }
}
