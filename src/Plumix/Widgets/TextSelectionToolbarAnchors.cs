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
    Point PrimaryAnchor,
    Point? SecondaryAnchor = null)
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
            return new TextSelectionToolbarAnchors(default(Point));
        }

        Rect editingRegion = GetEditingRegion(renderBox);
        double centerX = rect.left + (rect.right - rect.left) / 2;
        return new TextSelectionToolbarAnchors(
            new Point(centerX, BoxConstraints.ClampDouble(rect.top, editingRegion.Top, editingRegion.Bottom)),
            new Point(centerX, BoxConstraints.ClampDouble(rect.bottom, editingRegion.Top, editingRegion.Bottom)));
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

        return new Rect(
            rect.left,
            rect.top,
            Math.Max(0.0, rect.right - rect.left),
            Math.Max(0.0, rect.bottom - rect.top));
    }

    // Dart's `Rect.fromPoints` of the box's global top-left and bottom-right corners.
    private static Rect GetEditingRegion(RenderBox renderBox)
    {
        Point a = renderBox.LocalToGlobal(default);
        Point b = renderBox.LocalToGlobal(new Point(renderBox.Size.Width, renderBox.Size.Height));
        double left = Math.Min(a.X, b.X);
        double top = Math.Min(a.Y, b.Y);
        return new Rect(left, top, Math.Max(a.X, b.X) - left, Math.Max(a.Y, b.Y) - top);
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

        Point first = selectionEndpoints[0].Point;
        Point last = selectionEndpoints[^1].Point;
        bool isMultiline = last.Y - first.Y > endGlyphHeight / 2;

        return (
            isMultiline ? editingRegion.Left : editingRegion.Left + first.X,
            editingRegion.Top + first.Y - startGlyphHeight,
            isMultiline ? editingRegion.Right : editingRegion.Left + last.X,
            editingRegion.Top + last.Y);
    }
}
