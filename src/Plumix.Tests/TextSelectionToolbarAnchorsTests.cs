using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_selection_toolbar_anchors.dart —
// Flutter has no test file for it; `EditableTextState.contextMenuAnchors` and
// `SelectableRegionState.contextMenuAnchors` reach it through EditableTextContextMenuDartParityTests.
public sealed class TextSelectionToolbarAnchorsTests
{
    // A box of `size` laid out at `offset` in an attached tree, since `localToGlobal` needs an owner.
    private static RenderBox LaidOutBox(FrameworkDartTester tester, Point offset, Size size)
    {
        var key = new GlobalObjectKey<State>(new object());
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Padding(
                EdgeInsetsGeometry.FromLTRB(offset.X, offset.Y, 0, 0),
                new Align(
                    alignment: Alignment.TopLeft,
                    child: new SizedBox(key: key, width: size.Width, height: size.Height)))));
        return (RenderBox)key.CurrentContext!.FindRenderObject()!;
    }

    [Fact]
    public void FromSelectionCentersOnASingleLineSelectionAndClampsToTheEditingRegion()
    {
        using var tester = new FrameworkDartTester();
        RenderBox box = LaidOutBox(tester, new Point(100, 50), new Size(200, 40));
        TextSelectionPoint[] points =
        [
            new(new Point(20, 14), TextDirection.Ltr),
            new(new Point(80, 14), TextDirection.Ltr),
        ];

        Assert.Equal(new Rect(120, 50, 60, 14), TextSelectionToolbarAnchors.GetSelectionRect(box, 14, 14, points));
        TextSelectionToolbarAnchors anchors = TextSelectionToolbarAnchors.FromSelection(box, 14, 14, points);
        Assert.Equal(new Point(150, 50), anchors.PrimaryAnchor);
        Assert.Equal(new Point(150, 64), anchors.SecondaryAnchor);

        // A glyph taller than the region is clamped to its top edge.
        anchors = TextSelectionToolbarAnchors.FromSelection(box, 30, 30, points);
        Assert.Equal(new Point(150, 50), anchors.PrimaryAnchor);

        // Right-to-left endpoints keep Dart's negative width, so the anchor is still their midpoint.
        TextSelectionPoint[] reversed = [points[1], points[0]];
        anchors = TextSelectionToolbarAnchors.FromSelection(box, 14, 14, reversed);
        Assert.Equal(150, anchors.PrimaryAnchor.X);
    }

    [Fact]
    public void FromSelectionSpansTheEditingRegionForAMultilineSelection()
    {
        using var tester = new FrameworkDartTester();
        RenderBox box = LaidOutBox(tester, new Point(100, 50), new Size(200, 100));
        TextSelectionPoint[] points =
        [
            new(new Point(150, 14), TextDirection.Ltr),
            new(new Point(10, 42), TextDirection.Ltr),
        ];

        Assert.Equal(new Rect(100, 50, 200, 42), TextSelectionToolbarAnchors.GetSelectionRect(box, 14, 14, points));
        TextSelectionToolbarAnchors anchors = TextSelectionToolbarAnchors.FromSelection(box, 14, 14, points);
        Assert.Equal(new Point(200, 50), anchors.PrimaryAnchor);
        Assert.Equal(new Point(200, 92), anchors.SecondaryAnchor);

        // Endpoints less than half an end glyph apart are one line: the rect runs from the first
        // endpoint, and Avalonia's Rect clamps Dart's negative width to zero.
        points[1] = new TextSelectionPoint(new Point(10, 20), TextDirection.Ltr);
        Assert.Equal(new Rect(250, 50, 0, 20), TextSelectionToolbarAnchors.GetSelectionRect(box, 14, 14, points));
        Assert.Equal(180, TextSelectionToolbarAnchors.FromSelection(box, 14, 14, points).PrimaryAnchor.X);
    }
}
