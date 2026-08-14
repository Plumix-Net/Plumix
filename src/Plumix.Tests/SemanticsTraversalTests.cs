using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart
// Mirrors flutter/packages/flutter/test/semantics/traversal_order_test.dart and the geometry
// assertions in flutter/packages/flutter/test/widgets/semantics_clipping_test.dart.

namespace Plumix.Tests;

public sealed class SemanticsTraversalTests
{
    [Fact]
    public void SemanticsNode_RectIsLocalAndTransformMapsIntoTheParentNode()
    {
        var leaf = new LabelBox("Moved", new Size(12, 8));
        var transform = new RenderTransform(Matrix.CreateTranslation(30, 12), leaf);
        SemanticsNode root = Compile(transform, new Size(220, 120));

        SemanticsNode moved = Assert.Single(root.Children);
        Assert.Equal(new Rect(0, 0, 12, 8), moved.Rect);
        Assert.Equal(Matrix.CreateTranslation(30, 12), moved.Transform);
        Assert.Equal(new Rect(30, 12, 12, 8), moved.GlobalRect);
    }

    [Fact]
    public void SemanticsNode_IdentityTransformIsNormalizedToNull()
    {
        SemanticsNode root = Compile(new LabelBox("Still", new Size(12, 8)), new Size(220, 120));

        SemanticsNode still = Assert.Single(root.Children);
        Assert.Null(still.Transform);
        Assert.Equal(new Rect(0, 0, 12, 8), still.GlobalRect);
    }

    [Fact]
    public void TraversalOrder_WithoutATextDirection_KeepsPaintOrder()
    {
        SemanticsNode root = CompileRow(
            new LabelBox("Right", new Size(20, 10)),
            new LabelBox("Left", new Size(20, 10)),
            textDirection: null);

        Assert.Equal(
            ["Right", "Left"],
            root.ChildrenInTraversalOrder.Select(static node => node.Label));
    }

    [Fact]
    public void TraversalOrder_RightToLeftReversesTheHorizontalGroups()
    {
        var left = new PositionedLabelBox("Left", new Rect(0, 0, 40, 20));
        var right = new PositionedLabelBox("Right", new Rect(60, 0, 40, 20));

        SemanticsNode root = CompileDirectionalRow([left, right], TextDirection.Rtl);

        Assert.Equal(
            ["Right", "Left"],
            root.ChildrenInTraversalOrder.Select(static node => node.Label));
    }

    [Fact]
    public void TraversalOrder_SortKeysTakePrecedenceOverGeometry()
    {
        var first = new PositionedLabelBox("First", new Rect(0, 0, 40, 20), new OrdinalSortKey(1.0));
        var second = new PositionedLabelBox("Second", new Rect(60, 0, 40, 20), new OrdinalSortKey(0.0));

        SemanticsNode root = CompileDirectionalRow([first, second], TextDirection.Ltr);

        Assert.Equal(
            ["Second", "First"],
            root.ChildrenInTraversalOrder.Select(static node => node.Label));
    }

    [Fact]
    public void BlockingUserActions_StripsTheActionsFromTheNode()
    {
        int taps = 0;
        var button = new TapBox("Tap", new Size(20, 10), () => taps += 1);
        var ignored = new RenderIgnorePointer(ignoring: true, child: button);
        SemanticsNode root = Compile(ignored, new Size(220, 120));

        SemanticsNode node = Assert.Single(root.Children);
        Assert.Equal("Tap", node.Label);
        Assert.True(node.AreUserActionsBlocked);
        Assert.Equal(SemanticsActions.None, node.Actions);
        Assert.False(node.PerformAction(SemanticsActions.Tap));
        Assert.Equal(0, taps);
    }

    private static SemanticsNode Compile(RenderBox child, Size size)
    {
        var renderView = new RenderView { Child = child };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(size);
        pipeline.FlushSemantics();
        return Assert.IsType<SemanticsNode>(pipeline.SemanticsOwner.RootNode);
    }

    private static SemanticsNode CompileRow(RenderBox first, RenderBox second, TextDirection? textDirection)
    {
        var row = new RenderFlex(children: [first, second], direction: Axis.Horizontal);
        if (textDirection is not { } direction)
        {
            return Compile(row, new Size(220, 120));
        }

        var annotated = new RenderSemanticsAnnotations(
            textDirection: direction,
            explicitChildNodes: true,
            child: row);
        return Assert.Single(Compile(annotated, new Size(220, 120)).Children);
    }

    private static SemanticsNode CompileDirectionalRow(
        List<PositionedLabelBox> children,
        TextDirection textDirection)
    {
        var row = new RenderFlex(children: [.. children], direction: Axis.Horizontal);
        var root = new RenderSemanticsAnnotations(
            textDirection: textDirection,
            explicitChildNodes: true,
            child: row);
        return Assert.Single(Compile(root, new Size(220, 120)).Children);
    }

    /// <summary>A leaf that annotates a label without declaring a boundary of its own.</summary>
    private sealed class LabelBox(string label, Size size) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.Label = label;
        }
    }

    /// <summary>A leaf that reports a label and a tap action.</summary>
    private sealed class TapBox(string label, Size size, Action onTap) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.Label = label;
            configuration.AddActionHandler(SemanticsActions.Tap, onTap);
        }
    }

    /// <summary>A leaf laid out in a row, so the geometry-driven sort has something to order.</summary>
    private sealed class PositionedLabelBox(string label, Rect rect, SemanticsSortKey? sortKey = null) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(rect.Size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.Label = label;
            configuration.SortKey = sortKey;
        }
    }
}
