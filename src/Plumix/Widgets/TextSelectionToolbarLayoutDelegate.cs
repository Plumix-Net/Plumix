using Avalonia;
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
        Point anchorAbove,
        Point anchorBelow,
        bool? fitsAbove = null)
    {
        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        FitsAbove = fitsAbove;
    }

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

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

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        bool fitsAbove = FitsAbove ?? AnchorAbove.Y >= childSize.Height;
        Point anchor = fitsAbove ? AnchorAbove : AnchorBelow;
        return new Point(
            CenterOn(anchor.X, childSize.Width, size.Width),
            fitsAbove ? Math.Max(0.0, anchor.Y - childSize.Height) : anchor.Y);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not TextSelectionToolbarLayoutDelegate oldToolbarDelegate
               || oldToolbarDelegate.AnchorAbove != AnchorAbove
               || oldToolbarDelegate.AnchorBelow != AnchorBelow
               || oldToolbarDelegate.FitsAbove != FitsAbove;
    }
}
