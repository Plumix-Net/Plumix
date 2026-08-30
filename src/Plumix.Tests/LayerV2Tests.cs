using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class LayerV2Tests
{
    [Fact]
    public void PushClipRect_CreatesClipRectLayer_WhenCompositingIsNeeded()
    {
        var leaf = new TestClipPainterRenderBox(needsCompositing: true);
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
    public void PushClipRect_ClipsOnTheCanvas_WhenCompositingIsNotNeeded()
    {
        var leaf = new TestClipPainterRenderBox(needsCompositing: false);
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        // Dart's non-compositing branch records the clip into the picture instead of a clip layer.
        Layer child = Assert.Single(pipeline.RootLayer.Children);
        var picture = Assert.IsType<PictureLayer>(child);
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void PushClipRect_WithClipNone_PaintsWithoutAnyClip()
    {
        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);

        ClipRectLayer? layer = context.PushClipRect(
            needsCompositing: true,
            new Point(3, 5),
            new Rect(0, 0, 10, 10),
            (clipped, offset) => clipped.Canvas.DrawRectangle(Brushes.Gold, null, new Rect(offset, new Size(4, 4))),
            Clip.None);

        Assert.Null(layer);
        Assert.IsType<PictureLayer>(Assert.Single(rootLayer.Children));
    }

    [Fact]
    public void PushClipRect_ShiftsTheClipByTheOffset()
    {
        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);

        ClipRectLayer? layer = context.PushClipRect(
            needsCompositing: true,
            new Point(3, 5),
            new Rect(0, 0, 10, 10),
            (_, _) => { });

        Assert.NotNull(layer);
        Assert.Equal(new Rect(3, 5, 10, 10), layer!.ClipRect);
        Assert.Equal(Clip.HardEdge, layer.ClipBehavior);
    }

    [Fact]
    public void PushClipRect_ReusesTheLayerItWasGiven()
    {
        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);
        var oldLayer = new ClipRectLayer();

        ClipRectLayer? layer = context.PushClipRect(
            needsCompositing: true,
            new Point(0, 0),
            new Rect(0, 0, 10, 10),
            (_, _) => { },
            oldLayer: oldLayer);

        Assert.Same(oldLayer, layer);
    }

    [Fact]
    public void PushClipRRect_CreatesClipRRectLayer_WhenCompositingIsNeeded()
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
        Assert.Equal(new Rect(5, 7, 44, 26), clipLayer.ClipRRect.Rect);
        Assert.Equal(BorderRadius.Circular(8), clipLayer.ClipRRect.Radii);
        Assert.IsType<PictureLayer>(Assert.Single(clipLayer.Children));
    }

    [Fact]
    public void PushClipPath_CreatesClipPathLayer_WhenCompositingIsNeeded()
    {
        var path = new Plumix.UI.Path();
        path.AddOval(new Rect(0, 0, 50, 30));

        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);
        ClipPathLayer? layer = context.PushClipPath(
            needsCompositing: true,
            new Point(2, 4),
            new Rect(0, 0, 50, 30),
            path,
            (clipped, _) => clipped.Canvas.DrawRectangle(Brushes.Gold, null, new Rect(0, 0, 80, 40)));

        Assert.NotNull(layer);
        Assert.Same(layer, Assert.Single(rootLayer.Children));
        Assert.Equal(new Rect(2, 4, 50, 30), layer!.ClipPath.GetBounds());
        Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
    }

    [Fact]
    public void PushClipGeometry_CreatesGeometryLayer_WhenCompositingIsNeeded()
    {
        var geometry = new RectangleGeometry(new Rect(0, 0, 50, 30));

        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);
        context.PushClipGeometry(
            needsCompositing: true,
            new Point(0, 0),
            new Rect(0, 0, 50, 30),
            geometry,
            (clipped, _) => clipped.Canvas.DrawRectangle(Brushes.Gold, null, new Rect(0, 0, 80, 40)));

        var clipLayer = Assert.IsType<ClipGeometryLayer>(Assert.Single(rootLayer.Children));
        Assert.Same(geometry, clipLayer.Geometry);
        Assert.IsType<PictureLayer>(Assert.Single(clipLayer.Children));
    }

    [Fact]
    public void RenderClipRRect_UpdatesLayerClip_WhenSizeChanges()
    {
        var clip = new RenderClipRRect(
            child: new TestCompositingRenderBox());
        var renderView = new RenderView
        {
            Child = clip
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(80, 40));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var firstLayer = Assert.IsType<ClipRRectLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(0, 0, 80, 40), firstLayer.ClipRRect.Rect);

        pipeline.RequestLayout();
        pipeline.FlushLayout(new Size(200, 40));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var secondLayer = Assert.IsType<ClipRRectLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(0, 0, 200, 40), secondLayer.ClipRRect.Rect);
        Assert.Same(firstLayer, secondLayer);
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
        Assert.Equal(107, opacityLayer.Alpha);

        var transformLayer = Assert.IsType<TransformLayer>(Assert.Single(opacityLayer.Children));
        Assert.Equal(Matrix4.TranslationValues(12, 8, 0.0), transformLayer.Transform);
        Assert.IsType<PictureLayer>(Assert.Single(transformLayer.Children));
    }

    [Fact]
    public void PushOpacity_CarriesTheOffsetOnTheLayer()
    {
        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);
        Point painterOffset = new(-1, -1);

        OpacityLayer layer = context.PushOpacity(
            new Point(7, 9),
            128,
            (_, offset) => painterOffset = offset);

        Assert.Equal(new Point(7, 9), layer.Offset);
        Assert.Equal(128, layer.Alpha);
        Assert.Equal(new Point(0, 0), painterOffset);
    }

    [Fact]
    public void PushTransform_WithoutCompositing_KeepsTheCanvasBalanced()
    {
        var rootLayer = new ContainerLayer();
        var context = new PaintingContext(rootLayer);
        int saveCountInside = 0;

        TransformLayer? layer = context.PushTransform(
            needsCompositing: false,
            new Point(4, 4),
            Matrix4.TranslationValues(2, 3, 0.0),
            (transformed, _) => saveCountInside = transformed.Canvas.GetSaveCount());

        Assert.Null(layer);
        Assert.Equal(2, saveCountInside);
        Assert.Equal(1, context.Canvas.GetSaveCount());
    }

    private sealed class TestClipPainterRenderBox : RenderBox
    {
        private readonly bool _needsCompositing;

        public TestClipPainterRenderBox(bool needsCompositing)
        {
            _needsCompositing = needsCompositing;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(80, 40));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.PushClipRect(
                _needsCompositing,
                offset,
                new Rect(new Point(4, 6), new Size(40, 24)),
                (clipContext, clipOffset) => clipContext.Canvas.DrawRectangle(
                    Brushes.Crimson,
                    null,
                    new Rect(clipOffset, Size)));
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
                offset,
                107,
                (opacityContext, opacityOffset) => opacityContext.PushTransform(
                    true,
                    opacityOffset,
                    Matrix4.TranslationValues(12, 8, 0.0),
                    (transformContext, transformOffset) => transformContext.Canvas.DrawRectangle(
                        Brushes.MediumSeaGreen,
                        null,
                        new Rect(transformOffset, new Size(20, 10)))));
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
                true,
                offset,
                new Rect(new Point(5, 7), new Size(44, 26)),
                RRect.FromRectAndCorners(
                    new Rect(new Point(5, 7), new Size(44, 26)),
                    BorderRadius.Circular(8)),
                (clipContext, clipOffset) => clipContext.Canvas.DrawRectangle(
                    Brushes.CornflowerBlue,
                    null,
                    new Rect(clipOffset, Size)));
        }
    }

    /// <summary>A leaf that always needs compositing, so its ancestors take the layer branch.</summary>
    private sealed class TestCompositingRenderBox : RenderBox
    {
        protected override bool AlwaysNeedsCompositing => true;

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(Constraints.Biggest);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.Canvas.DrawRectangle(Brushes.Transparent, null, new Rect(offset, Size));
        }
    }
}
