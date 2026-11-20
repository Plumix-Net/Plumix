using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/text_selection_toolbar_layout_delegate.dart

/// <summary>
/// Positions a text-selection toolbar above its primary anchor when it fits and below its fallback anchor otherwise.
/// </summary>
public sealed class TextSelectionToolbarLayoutDelegate : SingleChildLayoutDelegate
{
    public TextSelectionToolbarLayoutDelegate(
        Offset anchorAbove,
        Offset anchorBelow,
        bool? fitsAbove = null)
    {
        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        FitsAbove = fitsAbove;
    }

    public Offset AnchorAbove { get; }

    public Offset AnchorBelow { get; }

    public bool? FitsAbove { get; }

    public static double CenterOn(double position, double width, double max)
    {
        if (position - (width / 2.0) < 0.0)
        {
            return 0.0;
        }

        if (position + (width / 2.0) > max)
        {
            return max - width;
        }

        return position - (width / 2.0);
    }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => constraints.Loosen();

    public override Offset GetPositionForChild(Size size, Size childSize)
    {
        bool fitsAbove = FitsAbove ?? AnchorAbove.Dy >= childSize.Height;
        Offset anchor = fitsAbove ? AnchorAbove : AnchorBelow;
        return new Offset(
            CenterOn(anchor.Dx, childSize.Width, size.Width),
            fitsAbove ? Math.Max(0.0, anchor.Dy - childSize.Height) : anchor.Dy);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not TextSelectionToolbarLayoutDelegate oldToolbarDelegate
               || oldToolbarDelegate.AnchorAbove != AnchorAbove
               || oldToolbarDelegate.AnchorBelow != AnchorBelow
               || oldToolbarDelegate.FitsAbove != FitsAbove;
    }
}
