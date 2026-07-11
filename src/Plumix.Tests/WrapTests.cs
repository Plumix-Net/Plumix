using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

public sealed class WrapTests
{
    [Fact]
    public void RenderWrap_WrapsRunsAndAlignsChildrenWithinAvailableSpace()
    {
        var first = new FixedSizeRenderBox(new Size(40, 10));
        var second = new FixedSizeRenderBox(new Size(40, 20));
        var third = new FixedSizeRenderBox(new Size(30, 15));
        var wrap = new RenderWrap(
            spacing: 10,
            runSpacing: 5,
            alignment: WrapAlignment.Center,
            runAlignment: WrapAlignment.Center);
        wrap.AddAll([first, second, third]);

        Layout(wrap, new Size(100, 80));

        Assert.Equal(new Size(100, 80), wrap.Size);
        Assert.Equal(new Point(5, 20), ParentData(first).offset);
        Assert.Equal(new Point(55, 20), ParentData(second).offset);
        Assert.Equal(new Point(35, 45), ParentData(third).offset);
    }

    [Fact]
    public void RenderWrap_RtlStartAlignmentReversesVisualChildOrder()
    {
        var first = new FixedSizeRenderBox(new Size(20, 10));
        var second = new FixedSizeRenderBox(new Size(20, 10));
        var wrap = new RenderWrap(
            spacing: 5,
            textDirection: TextDirection.Rtl,
            alignment: WrapAlignment.Start);
        wrap.AddAll([first, second]);

        Layout(wrap, new Size(100, 30));

        Assert.Equal(new Point(80, 0), ParentData(first).offset);
        Assert.Equal(new Point(55, 0), ParentData(second).offset);
    }

    [Fact]
    public void RenderWrap_VerticalDirectionControlsRunOrderAndCrossAlignment()
    {
        var first = new FixedSizeRenderBox(new Size(10, 40));
        var second = new FixedSizeRenderBox(new Size(20, 40));
        var third = new FixedSizeRenderBox(new Size(15, 30));
        var wrap = new RenderWrap(
            direction: Axis.Vertical,
            spacing: 5,
            runSpacing: 10,
            crossAxisAlignment: WrapCrossAlignment.End,
            textDirection: TextDirection.Rtl,
            verticalDirection: Plumix.Painting.VerticalDirection.Up);
        wrap.AddAll([first, second, third]);

        Layout(wrap, new Size(80, 100));

        Assert.Equal(new Point(60, 60), ParentData(first).offset);
        Assert.Equal(new Point(60, 15), ParentData(second).offset);
        Assert.Equal(new Point(35, 70), ParentData(third).offset);
    }

    private static WrapParentData ParentData(RenderBox child)
    {
        return Assert.IsType<WrapParentData>(child.parentData);
    }

    private static void Layout(RenderBox child, Size size)
    {
        var constrained = new RenderConstrainedBox(BoxConstraints.Tight(size), child);
        var root = new RenderView { Child = constrained };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(size);
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _desiredSize;

        public FixedSizeRenderBox(Size desiredSize)
        {
            _desiredSize = desiredSize;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
