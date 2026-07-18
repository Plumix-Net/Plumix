using Avalonia;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/desktop_text_selection_toolbar_layout_delegate.dart

/// <summary>Positions a desktop selection toolbar at its anchor while keeping it inside the viewport.</summary>
public sealed class DesktopTextSelectionToolbarLayoutDelegate : SingleChildLayoutDelegate
{
    public DesktopTextSelectionToolbarLayoutDelegate(Point anchor)
    {
        Anchor = anchor;
    }

    public Point Anchor { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => constraints.Loosen();

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        var overhang = new Vector(
            Anchor.X + childSize.Width - size.Width,
            Anchor.Y + childSize.Height - size.Height);
        return new Point(
            overhang.X > 0.0 ? Anchor.X - overhang.X : Anchor.X,
            overhang.Y > 0.0 ? Anchor.Y - overhang.Y : Anchor.Y);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not DesktopTextSelectionToolbarLayoutDelegate oldToolbarDelegate
               || oldToolbarDelegate.Anchor != Anchor;
    }
}
