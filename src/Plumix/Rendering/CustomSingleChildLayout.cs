using Avalonia;
using Plumix.Foundation;

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
public sealed class RenderCustomSingleChildLayoutBox : RenderProxyBox
{
    private SingleChildLayoutDelegate _layoutDelegate;

    public RenderCustomSingleChildLayoutBox(
        SingleChildLayoutDelegate layoutDelegate,
        RenderBox? child = null)
    {
        _layoutDelegate = layoutDelegate ?? throw new ArgumentNullException(nameof(layoutDelegate));
        Child = child;
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

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_layoutDelegate.GetSize(Constraints));
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
