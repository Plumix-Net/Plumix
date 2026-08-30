using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;
using Plumix.UI;

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
    public void RenderOpacity_CompositingTracksVisibilityWithoutAnotherLayout()
    {
        var opacity = new RenderOpacity(opacity: 1.0, child: new TestLeafRenderBox());
        var root = new RenderView
        {
            Child = opacity
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();

        Assert.True(opacity.NeedsCompositing);

        opacity.Opacity = 0.0;
        Assert.True(opacity.NeedsCompositingBitsUpdate);
        pipeline.FlushCompositingBits();

        Assert.False(opacity.NeedsCompositing);

        opacity.Opacity = 0.5;
        Assert.True(opacity.NeedsCompositingBitsUpdate);
        pipeline.FlushCompositingBits();

        Assert.True(opacity.NeedsCompositing);
    }

    [Fact]
    public void InitialAttach_InitializesAlwaysNeedsCompositingWithoutLayoutInvalidation()
    {
        var child = new AlwaysCompositingRenderBox();
        var root = new RenderView
        {
            Child = child
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();

        Assert.True(child.NeedsCompositing);
        Assert.False(child.NeedsCompositingBitsUpdate);

        child.MarkNeedsLayout();
        pipeline.FlushLayout(new Size(300, 200));

        Assert.False(child.NeedsCompositingBitsUpdate);
        Assert.False(root.NeedsCompositingBitsUpdate);
    }

    [Fact]
    public void RenderTransform_UpdatesLayerTransform_WithoutRepaintingChild()
    {
        var leaf = new TestLeafRenderBox();
        var transform = new RenderTransform(
            Matrix4.TranslationValues(8, 4, 0.0),
            alignment: null,
            child: leaf,
            filterQuality: FilterQuality.Low);
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
        Assert.Equal(Matrix4.TranslationValues(8, 4, 0.0), transformLayer.Transform);
        Assert.Equal(FilterQuality.Low, transformLayer.FilterQuality);

        transform.Transform = Matrix4.TranslationValues(21, 13, 0.0);
        transform.FilterQuality = FilterQuality.High;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(Matrix4.TranslationValues(21, 13, 0.0), transformLayer.Transform);
        Assert.Equal(FilterQuality.High, transformLayer.FilterQuality);
    }

    [Fact]
    public void RenderTransform_WithAlignment_RotatesAroundTheAnchorPoint()
    {
        var leaf = new TestLeafRenderBox();
        var transform = new RenderTransform(
            Matrix4.RotationZ(Math.PI / 2.0),
            alignment: Alignment.Center,
            child: leaf);
        var root = new RenderView
        {
            Child = transform
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(300, 200));

        Assert.Equal(new Size(32, 32), transform.Size);
        var center = new Point(16, 16);
        Point mappedCenter = MatrixUtils.TransformPoint(transform.EffectiveTransform, center);
        Assert.Equal(center.X, mappedCenter.X, 6);
        Assert.Equal(center.Y, mappedCenter.Y, 6);

        Point mappedTopLeft = MatrixUtils.TransformPoint(transform.EffectiveTransform, new Point(0, 0));
        Assert.Equal(32.0, mappedTopLeft.X, 6);
        Assert.Equal(0.0, mappedTopLeft.Y, 6);
    }

    [Fact]
    public void RenderClipRect_UpdatesLayerClip_WithoutRepaintingChild()
    {
        var leaf = new TestLeafRenderBox();
        var clipper = new FixedRectClipper(new Rect(0, 0, 32, 32));
        var clipRect = new RenderClipRect(leaf, clipper: clipper);
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

        clipper.Rect = new Rect(3, 5, 20, 12);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, leaf.PaintCount);
        Assert.Equal(new Rect(3, 5, 20, 12), clipLayer.ClipRect);
    }

    [Fact]
    public void RenderColoredBox_NoOpColorUpdate_DoesNotRepaintChild()
    {
        var leaf = new TestLeafRenderBox();
        var coloredBox = new RenderColoredBox(Colors.CadetBlue, child: leaf);
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

        // Flutter's `_skippedPaintingOnLayer` walks up only as far as the first ancestor whose own
        // layer is still attached — the one that detached us — and leaves the decision to repaint to
        // it, so the dirty boundary does not paint itself back in.
        Assert.Equal(1, boundary.PaintCount);
        Assert.Empty(pipeline.RootLayer.Children);

        root.MarkNeedsPaint();
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

    private sealed class AlwaysCompositingRenderBox : RenderBox
    {
        protected override bool AlwaysNeedsCompositing => true;

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(32, 32));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
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
            // Flutter's `updateCompositedLayer` may configure the layer but must leave `offset` alone:
            // `PaintingContext._compositeChild` owns it and asserts the callee did not touch it.
            LayerUpdateCount += 1;
            layer.DebugCreator = LayerUpdateCount;
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
