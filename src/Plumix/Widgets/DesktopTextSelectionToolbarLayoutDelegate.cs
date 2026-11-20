using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/desktop_text_selection_toolbar_layout_delegate.dart

/// <summary>Positions a desktop selection toolbar at its anchor while keeping it inside the viewport.</summary>
public sealed class DesktopTextSelectionToolbarLayoutDelegate : SingleChildLayoutDelegate
{
    public DesktopTextSelectionToolbarLayoutDelegate(Offset anchor)
    {
        Anchor = anchor;
    }

    public Offset Anchor { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => constraints.Loosen();

    public override Offset GetPositionForChild(Size size, Size childSize)
    {
        var overhang = new Offset(
            Anchor.Dx + childSize.Width - size.Width,
            Anchor.Dy + childSize.Height - size.Height);
        return new Offset(
            overhang.Dx > 0.0 ? Anchor.Dx - overhang.Dx : Anchor.Dx,
            overhang.Dy > 0.0 ? Anchor.Dy - overhang.Dy : Anchor.Dy);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not DesktopTextSelectionToolbarLayoutDelegate oldToolbarDelegate
               || oldToolbarDelegate.Anchor != Anchor;
    }
}
