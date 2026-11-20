using Avalonia;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_selection_toolbar_anchors.dart

/// <summary>The position information for a text selection toolbar.</summary>
/// <remarks>Typically, a menu will attempt to position itself at <see cref="PrimaryAnchor"/>, and if
/// that's not possible, then it will use <see cref="SecondaryAnchor"/> instead, if it exists.</remarks>
/// <param name="PrimaryAnchor">The location that the toolbar should attempt to position itself at.
/// </param>
/// <param name="SecondaryAnchor">The fallback position that should be used if
/// <paramref name="PrimaryAnchor"/> doesn't work.</param>
public readonly record struct TextSelectionToolbarAnchors(
    Offset PrimaryAnchor,
    Offset? SecondaryAnchor = null)
{
    /// <summary>Calculates <see cref="TextSelectionToolbarAnchors"/> based on the given
    /// <paramref name="renderBox"/> and the current selection.</summary>
    public static TextSelectionToolbarAnchors FromSelection(
        RenderBox renderBox,
        double startGlyphHeight,
        double endGlyphHeight,
        IReadOnlyList<TextSelectionPoint> selectionEndpoints)
    {
        ArgumentNullException.ThrowIfNull(renderBox);
        ArgumentNullException.ThrowIfNull(selectionEndpoints);
        (double left, double top, double right, double bottom)? selectionRect = SelectionLTRB(
            renderBox,
            startGlyphHeight,
            endGlyphHeight,
            selectionEndpoints);
        if (selectionRect is not { } rect || rect == (0.0, 0.0, 0.0, 0.0))
        {
            return new TextSelectionToolbarAnchors(default(Offset));
        }

        Rect editingRegion = GetEditingRegion(renderBox);
        double centerX = rect.left + (rect.right - rect.left) / 2;
        return new TextSelectionToolbarAnchors(
            new Offset(centerX, BoxConstraints.ClampDouble(rect.top, editingRegion.Top, editingRegion.Bottom)),
            new Offset(centerX, BoxConstraints.ClampDouble(rect.bottom, editingRegion.Top, editingRegion.Bottom)));
    }

    /// <summary>Returns the <see cref="Rect"/> covering the given selection in the given
    /// <paramref name="renderBox"/> in global coordinates.</summary>
    /// <remarks>Dart's <c>Rect.fromLTRB</c> keeps a negative width for a right-to-left single-line
    /// selection; Avalonia's <see cref="Rect"/> cannot, so the extent is clamped to zero here, while
    /// <see cref="FromSelection"/> keeps the unclamped edges.</remarks>
    public static Rect GetSelectionRect(
        RenderBox renderBox,
        double startGlyphHeight,
        double endGlyphHeight,
        IReadOnlyList<TextSelectionPoint> selectionEndpoints)
    {
        if (SelectionLTRB(renderBox, startGlyphHeight, endGlyphHeight, selectionEndpoints) is not { } rect)
        {
            return default;
        }

        return Rect.FromLTWH(
            rect.left,
            rect.top,
            Math.Max(0.0, rect.right - rect.left),
            Math.Max(0.0, rect.bottom - rect.top));
    }

    // Dart's `Rect.fromPoints` of the box's global top-left and bottom-right corners.
    private static Rect GetEditingRegion(RenderBox renderBox)
    {
        Offset a = renderBox.LocalToGlobal(default);
        Offset b = renderBox.LocalToGlobal(new Offset(renderBox.Size.Width, renderBox.Size.Height));
        double left = Math.Min(a.Dx, b.Dx);
        double top = Math.Min(a.Dy, b.Dy);
        return Rect.FromLTWH(left, top, Math.Max(a.Dx, b.Dx) - left, Math.Max(a.Dy, b.Dy) - top);
    }

    // The body of Dart's `getSelectionRect`; null stands for its `Rect.zero` result.
    private static (double left, double top, double right, double bottom)? SelectionLTRB(
        RenderBox renderBox,
        double startGlyphHeight,
        double endGlyphHeight,
        IReadOnlyList<TextSelectionPoint> selectionEndpoints)
    {
        Rect editingRegion = GetEditingRegion(renderBox);

        if (double.IsNaN(editingRegion.Left)
            || double.IsNaN(editingRegion.Top)
            || double.IsNaN(editingRegion.Right)
            || double.IsNaN(editingRegion.Bottom))
        {
            return null;
        }

        Offset first = selectionEndpoints[0].Point;
        Offset last = selectionEndpoints[^1].Point;
        bool isMultiline = last.Dy - first.Dy > endGlyphHeight / 2;

        return (
            isMultiline ? editingRegion.Left : editingRegion.Left + first.Dx,
            editingRegion.Top + first.Dy - startGlyphHeight,
            isMultiline ? editingRegion.Right : editingRegion.Left + last.Dx,
            editingRegion.Top + last.Dy);
    }
}
