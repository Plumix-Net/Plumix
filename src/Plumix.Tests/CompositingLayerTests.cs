using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class CompositingLayerTests
{
    [Fact]
    public void RenderView_UsesPipelineRootLayer_AsItsCompositedLayer()
    {
        var renderView = new RenderView
        {
            Child = new TestLeafRenderBox()
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Same(pipeline.RootLayer, renderView._layer);
    }

    [Fact]
    public void ReplaceRootLayer_RepaintsTreeIntoNewRootLayer()
    {
        var leaf = new TestLeafRenderBox();
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var replacement = new OffsetLayer();
        pipeline.ReplaceRootLayer(replacement);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Same(replacement, pipeline.RootLayer);
        Assert.NotEmpty(replacement.Children);
    }

    [Fact]
    public void ReplaceRootLayer_WithSameLayer_DoesNotRepaintTree()
    {
        var leaf = new TestLeafRenderBox();
        var renderView = new RenderView
        {
            Child = leaf
        };

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);

        var sameRootLayer = pipeline.RootLayer;
        pipeline.ReplaceRootLayer(sameRootLayer);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
    }

    [Fact]
    public void RepaintBoundary_CreatesDedicatedOffsetLayer()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestRepaintBoundaryRenderBox(leaf);
        var root = new RenderView
        {
            Child = boundary
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Single(pipeline.RootLayer.Children);
        var boundaryLayer = Assert.IsType<OffsetLayer>(pipeline.RootLayer.Children[0]);
        Assert.Single(boundaryLayer.Children);
        Assert.IsType<PictureLayer>(boundaryLayer.Children[0]);

        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);
    }

    [Fact]
    public void RepaintBoundary_DoesNotRepaintWhenOnlyParentIsDirty()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestRepaintBoundaryRenderBox(leaf);
        var parent = new TestParentPainterRenderBox(boundary);
        var root = new RenderView
        {
            Child = parent
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, parent.PaintCount);
        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);

        parent.TriggerRepaint();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(2, parent.PaintCount);
        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);
    }

    [Fact]
    public void RepaintBoundary_DirtyBoundary_RepaintsWithoutParentRepaint()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestRepaintBoundaryRenderBox(leaf);
        var parent = new TestParentPainterRenderBox(boundary);
        var root = new RenderView
        {
            Child = parent
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, parent.PaintCount);
        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);

        boundary.TriggerRepaint();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, parent.PaintCount);
        Assert.Equal(2, boundary.PaintCount);
        Assert.Equal(2, leaf.PaintCount);
    }

    [Fact]
    public void RepaintBoundary_LayerPropertyUpdate_DoesNotRepaintChildren()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestLayerUpdatingBoundaryRenderBox(leaf);
        var root = new RenderView
        {
            Child = boundary
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(1, boundary.LayerUpdateCount);

        boundary.TriggerLayerPropertyUpdate();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(2, boundary.LayerUpdateCount);
    }

    [Fact]
    public void RepaintBoundary_LayerPropertyUpdate_IsNotLostWhenBoundaryAlsoRepaints()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestLayerUpdatingBoundaryRenderBox(leaf);
        var root = new RenderView
        {
            Child = boundary
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, boundary.PaintCount);
        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(1, boundary.LayerUpdateCount);

        boundary.TriggerRepaintAndLayerPropertyUpdate();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(2, boundary.PaintCount);
        Assert.Equal(2, leaf.PaintCount);
        Assert.Equal(2, boundary.LayerUpdateCount);
    }

    [Fact]
    public void RenderOpacity_UpdatesLayerOpacity_WithoutRepaintingChild()
    {
        var leaf = new TestLeafRenderBox();
        var opacity = new RenderOpacity(opacity: 0.9, child: leaf);
        var root = new RenderView
        {
            Child = opacity
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        var opacityLayer = Assert.IsType<OpacityOffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(0.9, opacityLayer.Opacity, 3);

        opacity.Opacity = 0.25;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(0.25, opacityLayer.Opacity, 3);
    }

    [Fact]
    public void RenderTransform_UpdatesLayerTransform_WithoutRepaintingChild()
    {
        var leaf = new TestLeafRenderBox();
        var transform = new RenderTransform(Matrix.CreateTranslation(8, 4), leaf);
        var root = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        var transformLayer = Assert.IsType<TransformOffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(Matrix.CreateTranslation(8, 4), transformLayer.Transform);

        transform.Transform = Matrix.CreateTranslation(21, 13);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(Matrix.CreateTranslation(21, 13), transformLayer.Transform);
    }

    [Fact]
    public void RenderClipRect_UpdatesLayerClip_WithoutRepaintingChild()
    {
        var leaf = new TestLeafRenderBox();
        var clipRect = new RenderClipRect(leaf);
        var root = new RenderView
        {
            Child = clipRect
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        var clipLayer = Assert.IsType<ClipRectOffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(0, 0, 32, 32), clipLayer.ClipRect);

        clipRect.ClipRect = new Rect(3, 5, 20, 12);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(new Rect(3, 5, 20, 12), clipLayer.ClipRect);
    }

    [Fact]
    public void RenderColoredBox_NoOpColorUpdate_DoesNotRepaintChild()
    {
        var leaf = new TestLeafRenderBox();
        var coloredBox = new RenderColoredBox(Colors.CadetBlue, leaf);
        var root = new RenderView
        {
            Child = coloredBox
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);

        coloredBox.Color = Colors.CadetBlue;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
    }

    [Fact]
    public void DetachedBoundaryLayer_DirtyChild_RepaintsAfterAncestorRecovery()
    {
        var leaf = new TestLeafRenderBox();
        var boundary = new TestRepaintBoundaryRenderBox(leaf);
        var root = new RenderView
        {
            Child = boundary
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var boundaryLayer = Assert.IsType<OffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        pipeline.RootLayer.Remove(boundaryLayer);
        Assert.Null(boundaryLayer.Parent);

        boundary.TriggerRepaint();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(2, boundary.PaintCount);
        Assert.Equal(2, leaf.PaintCount);
        Assert.Single(pipeline.RootLayer.Children);
        Assert.Same(boundaryLayer, pipeline.RootLayer.Children[0]);
    }

    [Fact]
    public void RepaintBoundary_ToggleToNonBoundary_DropsDedicatedLayer()
    {
        var leaf = new TestLeafRenderBox();
        var toggle = new ToggleBoundaryRenderBox(initialBoundary: true, child: leaf);
        var root = new RenderView
        {
            Child = toggle
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Contains(pipeline.RootLayer.Children, static layer => layer is OffsetLayer);
        Assert.Equal(1, toggle.PaintCount);
        Assert.Equal(1, leaf.PaintCount);

        toggle.IsBoundary = false;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.DoesNotContain(pipeline.RootLayer.Children, static layer => layer is OffsetLayer);
        Assert.Equal(2, toggle.PaintCount);
        Assert.Equal(2, leaf.PaintCount);
        Assert.Null(toggle._layer);
    }

    [Fact]
    public void RepaintBoundary_ToggleToBoundary_CreatesDedicatedLayer()
    {
        var leaf = new TestLeafRenderBox();
        var toggle = new ToggleBoundaryRenderBox(initialBoundary: false, child: leaf);
        var root = new RenderView
        {
            Child = toggle
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.DoesNotContain(pipeline.RootLayer.Children, static layer => layer is OffsetLayer);
        Assert.Equal(1, toggle.PaintCount);
        Assert.Equal(1, leaf.PaintCount);

        toggle.IsBoundary = true;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Contains(pipeline.RootLayer.Children, static layer => layer is OffsetLayer);
        Assert.Equal(2, toggle.PaintCount);
        Assert.Equal(2, leaf.PaintCount);
        Assert.IsType<OffsetLayer>(toggle._layer);
    }

    private sealed class TestLeafRenderBox : RenderBox
    {
        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(32, 32));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            ctx.DrawRectangle(Brushes.CadetBlue, null, new Rect(offset, Size));
        }
    }

    private sealed class TestRepaintBoundaryRenderBox : RenderProxyBox
    {
        public int PaintCount { get; private set; }

        public TestRepaintBoundaryRenderBox(RenderBox child)
        {
            Child = child;
        }

        public override bool IsRepaintBoundary => true;

        public void TriggerRepaint()
        {
            MarkNeedsPaint();
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            base.Paint(ctx, offset);
        }
    }

    private sealed class TestParentPainterRenderBox : RenderProxyBox
    {
        public int PaintCount { get; private set; }

        public TestParentPainterRenderBox(RenderBox child)
        {
            Child = child;
        }

        public void TriggerRepaint()
        {
            MarkNeedsPaint();
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            ctx.DrawRectangle(Brushes.Transparent, null, new Rect(offset, Size));
            base.Paint(ctx, offset);
        }
    }

    private sealed class TestLayerUpdatingBoundaryRenderBox : RenderProxyBox
    {
        public int PaintCount { get; private set; }
        public int LayerUpdateCount { get; private set; }

        public TestLayerUpdatingBoundaryRenderBox(RenderBox child)
        {
            Child = child;
        }

        public override bool IsRepaintBoundary => true;

        public void TriggerLayerPropertyUpdate()
        {
            MarkNeedsCompositedLayerUpdate();
        }

        public void TriggerRepaintAndLayerPropertyUpdate()
        {
            MarkNeedsPaint();
            MarkNeedsCompositedLayerUpdate();
        }

        protected override void UpdateCompositedLayer(OffsetLayer layer)
        {
            LayerUpdateCount += 1;
            layer.Offset = new Point(2, 3);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            base.Paint(ctx, offset);
        }
    }

    private sealed class ToggleBoundaryRenderBox : RenderProxyBox
    {
        private bool _isBoundary;
        public int PaintCount { get; private set; }

        public ToggleBoundaryRenderBox(bool initialBoundary, RenderBox child)
        {
            _isBoundary = initialBoundary;
            Child = child;
        }

        public bool IsBoundary
        {
            get => _isBoundary;
            set
            {
                if (_isBoundary == value)
                {
                    return;
                }

                _isBoundary = value;
                MarkNeedsCompositingBitsUpdate();
                MarkNeedsPaint();
            }
        }

        public override bool IsRepaintBoundary => _isBoundary;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            base.Paint(ctx, offset);
        }
    }
}
