using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class LayerV2Tests
{
    [Fact]
    public void PushClipRect_CreatesClipRectLayer_WithPictureChild()
    {
        var leaf = new TestClipPainterRenderBox();
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var clipLayer = Assert.IsType<ClipRectLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(4, 6, 40, 24), clipLayer.ClipRect);
        Assert.IsType<PictureLayer>(Assert.Single(clipLayer.Children));
    }

    [Fact]
    public void PushClipRRect_CreatesClipRRectLayer_WithPictureChild()
    {
        var leaf = new TestClipRRectPainterRenderBox();
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var clipLayer = Assert.IsType<ClipRRectLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(5, 7, 44, 26), clipLayer.ClipRect);
        Assert.Equal(BorderRadius.Circular(8), clipLayer.BorderRadius);
        Assert.IsType<PictureLayer>(Assert.Single(clipLayer.Children));
    }

    [Fact]
    public void RenderClipRRect_UpdatesLayerClipRect_WhenSizeChanges()
    {
        var clip = new RenderClipRRect(
            child: new TestExpandRenderBox());
        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(80, 40));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var firstLayer = Assert.IsType<ClipRRectOffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(0, 0, 80, 40), firstLayer.ClipRect);

        pipeline.RequestLayout();
        pipeline.FlushLayout(new Size(200, 40));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var secondLayer = Assert.IsType<ClipRRectOffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(0, 0, 200, 40), secondLayer.ClipRect);
    }

    [Fact]
    public void PushOpacityAndTransform_CreateNestedLayerTree()
    {
        var leaf = new TestOpacityTransformPainterRenderBox();
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var opacityLayer = Assert.IsType<OpacityLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(0.42, opacityLayer.Opacity, 3);

        var transformLayer = Assert.IsType<TransformLayer>(Assert.Single(opacityLayer.Children));
        Assert.Equal(Matrix.CreateTranslation(12, 8), transformLayer.Transform);
        Assert.IsType<PictureLayer>(Assert.Single(transformLayer.Children));
    }

    private sealed class TestClipPainterRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(80, 40));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.PushClipRect(
                new Rect(offset + new Point(4, 6), new Size(40, 24)),
                clipContext => clipContext.DrawRectangle(Brushes.Crimson, null, new Rect(offset, Size)));
        }
    }

    private sealed class TestOpacityTransformPainterRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(100, 50));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.PushOpacity(
                0.42,
                opacityContext => opacityContext.PushTransform(
                    Matrix.CreateTranslation(12, 8),
                    transformContext => transformContext.DrawRectangle(
                        Brushes.MediumSeaGreen,
                        null,
                        new Rect(offset, new Size(20, 10)))));
        }
    }

    private sealed class TestClipRRectPainterRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(90, 46));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.PushClipRRect(
                new Rect(offset + new Point(5, 7), new Size(44, 26)),
                BorderRadius.Circular(8),
                clipContext => clipContext.DrawRectangle(Brushes.CornflowerBlue, null, new Rect(offset, Size)));
        }
    }

    private sealed class TestExpandRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(Constraints.Biggest);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.DrawRectangle(Brushes.Transparent, null, new Rect(offset, Size));
        }
    }
}
