using Avalonia;
using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Covers <c>rendering/viewport_offset.dart</c> and <c>rendering/viewport.dart</c>: the offset
/// protocol, the center-anchored two-directional layout, paint/hit-test order, the reveal algorithm
/// and the shrink-wrapping viewport.
/// </summary>
public class ViewportPortTests
{
    [Fact]
    public void ViewportOffsetFixed_KeepsItsValueThroughJumpsAndTakesCorrections()
    {
        ViewportOffset offset = ViewportOffset.Fixed(42.0);

        Assert.Equal(42.0, offset.Pixels);
        Assert.True(offset.HasPixels);
        Assert.True(offset.ApplyViewportDimension(100.0));
        Assert.True(offset.ApplyContentDimensions(0.0, 200.0));
        Assert.Equal(ScrollDirection.Idle, offset.UserScrollDirection);
        Assert.False(offset.AllowImplicitScrolling);

        offset.JumpTo(10.0);
        Assert.Equal(42.0, offset.Pixels);

        offset.CorrectBy(-12.0);
        Assert.Equal(30.0, offset.Pixels);

        Assert.Equal(0.0, ViewportOffset.Zero().Pixels);
    }

    [Fact]
    public void ViewportOffset_CorrectBy_DoesNotNotifyListeners_ButJumpToDoes()
    {
        var offset = new TestViewportOffset();
        int notifications = 0;
        offset.AddListener(() => notifications += 1);

        offset.CorrectBy(25.0);
        Assert.Equal(25.0, offset.Pixels);
        Assert.Equal(0, notifications);

        offset.JumpTo(80.0);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void ViewportOffset_ToString_ReportsTheOffsetWhenItHasPixels()
    {
        Assert.Contains("offset: 12.5", ViewportOffset.Fixed(12.5).ToString());
    }

    [Fact]
    public void FlipScrollDirection_SwapsForwardAndReverse_AndLeavesIdleAlone()
    {
        Assert.Equal(ScrollDirection.Idle, ScrollDirectionUtils.FlipScrollDirection(ScrollDirection.Idle));
        Assert.Equal(
            ScrollDirection.Reverse,
            ScrollDirectionUtils.FlipScrollDirection(ScrollDirection.Forward));
        Assert.Equal(
            ScrollDirection.Forward,
            ScrollDirectionUtils.FlipScrollDirection(ScrollDirection.Reverse));
    }

    [Fact]
    public void RenderViewport_AppliesViewportAndContentDimensions_WithTheAnchorFolded()
    {
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [new FixedSliver(400), new FixedSliver(400)]);
        Layout(viewport, new Size(100, 200));

        Assert.Equal(200, offset.ViewportDimension);
        Assert.Equal(0, offset.MinScrollExtent);
        Assert.Equal(600, offset.MaxScrollExtent);

        // anchor 0.5 puts the zero scroll offset in the middle of the viewport, so only the trailing
        // half is subtracted from the content extent. The minimum stays clamped at zero because no
        // sliver grows in the reverse direction.
        viewport.Anchor = 0.5;
        Layout(viewport, new Size(100, 200));
        Assert.Equal(0, offset.MinScrollExtent);
        Assert.Equal(700, offset.MaxScrollExtent);
    }

    [Fact]
    public void RenderViewport_CenterChild_GrowsPrecedingSliversInTheReverseDirection()
    {
        var before = new FixedSliver(300);
        var center = new FixedSliver(400);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [before, center]);
        viewport.Center = center;
        Layout(viewport, new Size(100, 200));

        Assert.Equal(GrowthDirection.Reverse, before.LastConstraints.GrowthDirection);
        Assert.Equal(GrowthDirection.Forward, center.LastConstraints.GrowthDirection);

        // The reverse sliver occupies negative scroll offsets, so the minimum extent is negative.
        Assert.Equal(-300, offset.MinScrollExtent);
        Assert.Equal(200, offset.MaxScrollExtent);

        // At the zero offset the center child sits at the leading edge and the reverse sliver is
        // entirely scrolled off the leading edge.
        Assert.Equal(new Point(0, 0), viewport.PaintOffsetOf(center));
        Assert.Equal(0.0, before.Geometry.PaintExtent);

        offset.JumpTo(-120);
        Layout(viewport, new Size(100, 200));
        Assert.Equal(120.0, before.Geometry.PaintExtent);
        Assert.Equal(new Point(0, 120), viewport.PaintOffsetOf(center));
    }

    [Fact]
    public void RenderViewport_ScrollOffsetOf_CountsForwardFromCenterAndBackwardBeforeIt()
    {
        var before = new FixedSliver(300);
        var center = new FixedSliver(400);
        var after = new FixedSliver(400);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [before, center, after]);
        viewport.Center = center;
        Layout(viewport, new Size(100, 200));

        Assert.Equal(0, viewport.ScrollOffsetOf(center, 0));
        Assert.Equal(400, viewport.ScrollOffsetOf(after, 0));
        Assert.Equal(-25, viewport.ScrollOffsetOf(before, 25));
        Assert.Equal(-1, viewport.IndexOfFirstChild);
        Assert.Equal("center child", RenderViewport.LabelForChild(0));
        Assert.Equal("child -1", RenderViewport.LabelForChild(-1));
    }

    [Fact]
    public void RenderViewport_ScrollOffsetCorrection_IsNegatedForTheReverseSequence()
    {
        var correcting = new CorrectOnceSliver(correction: -50, scrollExtent: 300);
        var center = new FixedSliver(400);
        var offset = new TestViewportOffset(-100);
        RenderViewport viewport = BuildViewport(offset, [correcting, center]);
        viewport.Center = center;
        Layout(viewport, new Size(100, 200));

        // A reverse-growth sliver's correction moves the offset the other way.
        Assert.Equal(-50, offset.Pixels);
        Assert.Equal(1, offset.CorrectionCount);
    }

    [Fact]
    public void RenderViewport_PaintOrder_DefaultsToFirstIsTop_AndHitTestsInTheReverseOrder()
    {
        var paints = new List<string>();
        var first = new FixedSliver(100, "first", paints);
        var second = new FixedSliver(100, "second", paints);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [first, second]);

        PipelineOwner pipeline = Layout(viewport, new Size(100, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Equal(["second", "first"], paints);
        Assert.Equal([first, second], viewport.ChildrenInHitTestOrder.ToList());

        paints.Clear();
        viewport.PaintOrder = SliverPaintOrder.LastIsTop;
        pipeline.RequestPaint();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Equal(["first", "second"], paints);
        Assert.Equal([second, first], viewport.ChildrenInHitTestOrder.ToList());
    }

    [Theory]
    [InlineData(null, 250.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(500.0, 500.0)]
    public void RenderViewport_PixelCacheExtent_GrowsTheSemanticsClip(double? cacheExtent, double expected)
    {
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(
            offset,
            [new FixedSliver(400)],
            cacheExtent is { } pixels ? ScrollCacheExtent.Pixels(pixels) : null);
        Layout(viewport, new Size(100, 200));

        Rect? clip = viewport.InvokeDescribeSemanticsClip(null);
        Assert.NotNull(clip);
        Assert.Equal(-expected, clip!.Value.Top);
        Assert.Equal(200 + expected, clip.Value.Bottom);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 100.0)]
    [InlineData(2.5, 500.0)]
    public void RenderViewport_ViewportCacheExtent_ScalesWithTheMainAxisExtent(double value, double expected)
    {
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(
            offset,
            [new FixedSliver(400)],
            ScrollCacheExtent.Viewport(value));
        Layout(viewport, new Size(100, 200));

        Rect? clip = viewport.InvokeDescribeSemanticsClip(null);
        Assert.Equal(-expected, clip!.Value.Top);
    }

    [Fact]
    public void RenderViewport_DescribeApproximatePaintClip_RespectsClipBehavior()
    {
        var sliver = new FixedSliver(400);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [sliver]);
        Layout(viewport, new Size(100, 200));

        Assert.Equal(new Rect(0, 0, 100, 200), viewport.InvokeDescribeApproximatePaintClip(sliver));

        viewport.ClipBehavior = Clip.None;
        Assert.Null(viewport.InvokeDescribeApproximatePaintClip(sliver));
    }

    [Fact]
    public void RenderViewport_Intrinsics_ThrowBecauseTheyWouldInstantiateEveryChild()
    {
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => viewport.GetMinIntrinsicHeight(0));
        Assert.Contains("does not support returning intrinsic dimensions", error.Message);
        Assert.Contains("consider a RenderShrinkWrappingViewport", error.Message);

        var shrinkWrapping = new RenderShrinkWrappingViewport(offset: ViewportOffset.Zero());
        InvalidOperationException shrinkWrapError =
            Assert.Throws<InvalidOperationException>(() => shrinkWrapping.GetMinIntrinsicHeight(0));
        Assert.Contains("giving the viewport loose constraints", shrinkWrapError.Message);
    }

    [Fact]
    public void RenderViewport_RejectsACrossAxisDirectionOnItsOwnAxis()
    {
        Assert.Throws<ArgumentException>(() => new RenderViewport(
            offset: ViewportOffset.Zero(),
            crossAxisDirection: AxisDirection.Up));
    }

    [Fact]
    public void RenderViewport_PassesTheCrossAxisDirectionToItsSlivers()
    {
        var sliver = new FixedSliver(400);
        RenderViewport viewport = BuildViewport(new TestViewportOffset(), [sliver]);
        Layout(viewport, new Size(100, 200));
        Assert.Equal(AxisDirection.Right, sliver.LastConstraints.CrossAxisDirection);

        viewport.CrossAxisDirection = AxisDirection.Left;
        Layout(viewport, new Size(100, 200));
        Assert.Equal(AxisDirection.Left, sliver.LastConstraints.CrossAxisDirection);
    }

    [Fact]
    public void RenderViewport_ChangingTheOffset_MovesTheListenerAndRelaysOut()
    {
        var first = new TestViewportOffset();
        var second = new TestViewportOffset(150);
        RenderViewport viewport = BuildViewport(first, [new FixedSliver(600)]);
        Layout(viewport, new Size(100, 200));

        viewport.Offset = second;
        Layout(viewport, new Size(100, 200));
        Assert.Equal(200, second.ViewportDimension);
        Assert.Equal(400, second.MaxScrollExtent);

        // The old offset is no longer listened to, so it cannot dirty this viewport any more.
        Assert.Equal(200, first.ViewportDimension);
    }

    [Fact]
    public void RenderViewport_GetOffsetToReveal_Down()
    {
        var offset = new TestViewportOffset(300);
        var boxes = new List<SizedRenderBox>();
        var slivers = new List<RenderSliver>();
        for (int index = 0; index < 10; index++)
        {
            var box = new SizedRenderBox(new Size(300, 100));
            boxes.Add(box);
            slivers.Add(new RenderSliverToBoxAdapter(box));
        }

        RenderViewport viewport = BuildViewport(offset, slivers);
        Layout(viewport, new Size(300, 200));

        RevealedOffset leading = viewport.GetOffsetToReveal(boxes[5], 0.0);
        Assert.Equal(500.0, leading.Offset);
        Assert.Equal(new Rect(0, 0, 300, 100), leading.Rect);

        RevealedOffset trailing = viewport.GetOffsetToReveal(boxes[5], 1.0);
        Assert.Equal(400.0, trailing.Offset);
        Assert.Equal(new Rect(0, 100, 300, 100), trailing.Rect);

        RevealedOffset partial = viewport.GetOffsetToReveal(boxes[5], 0.0, new Rect(40, 40, 10, 10));
        Assert.Equal(540.0, partial.Offset);
        Assert.Equal(new Rect(40, 0, 10, 10), partial.Rect);
    }

    [Fact]
    public void RenderViewport_GetOffsetToReveal_PinnedSliverReportsInfinityForTheLeadingEdge()
    {
        var pinned = new PinnedSliver(60);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [pinned, new FixedSliver(600)]);
        Layout(viewport, new Size(100, 200));

        Assert.Equal(double.PositiveInfinity, viewport.GetOffsetToReveal(pinned, 0.0).Offset);
    }

    [Fact]
    public void RenderViewport_GetOffsetToReveal_ReverseGrowthSliverAlignsFromItsTrailingEdge()
    {
        var before = new FixedSliver(300);
        var center = new FixedSliver(400);
        var offset = new TestViewportOffset();
        RenderViewport viewport = BuildViewport(offset, [before, center]);
        viewport.Center = center;
        Layout(viewport, new Size(100, 200));

        // A reverse-growth sliver is just outside the leading edge when the offset equals its own
        // leading scroll offset, so revealing it subtracts the target extent.
        RevealedOffset revealed = viewport.GetOffsetToReveal(before, 0.0);
        Assert.Equal(-300.0 - before.Geometry.PaintExtent, revealed.Offset);
    }

    [Fact]
    public void RenderShrinkWrappingViewport_SizesItselfToTheSumOfMaxPaintExtents()
    {
        var offset = new TestViewportOffset();
        var viewport = new RenderShrinkWrappingViewport(offset: offset);
        viewport.Insert(new FixedSliver(120));
        viewport.Insert(new FixedSliver(150), after: viewport.LastChild);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 600));

        Assert.Equal(270, viewport.Size.Height);
        Assert.Equal(270, offset.ViewportDimension);
        Assert.Equal(0, offset.MinScrollExtent);
        Assert.Equal(0, offset.MaxScrollExtent);
    }

    [Fact]
    public void RenderShrinkWrappingViewport_ClampsItsExtentToTheIncomingConstraints()
    {
        var offset = new TestViewportOffset();
        var viewport = new RenderShrinkWrappingViewport(offset: offset);
        viewport.Insert(new FixedSliver(900));

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(200, viewport.Size.Height);
        Assert.Equal(700, offset.MaxScrollExtent);
    }

    [Fact]
    public void RenderShrinkWrappingViewport_RejectsAnUnboundedCrossAxis()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var viewport = new RenderShrinkWrappingViewport(offset: ViewportOffset.Zero());
        viewport.Insert(new FixedSliver(100));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => viewport.Layout(new BoxConstraints(MaxWidth: double.PositiveInfinity)));
        Assert.Contains("Viewports expand in the cross axis", error.Message);
    }

    [Fact]
    public void RenderShrinkWrappingViewport_LaysOutChildrenByLogicalOffset()
    {
        var first = new FixedSliver(120);
        var second = new FixedSliver(150);
        var offset = new TestViewportOffset(60);
        var viewport = new RenderShrinkWrappingViewport(offset: offset);
        viewport.Insert(first);
        viewport.Insert(second, after: first);

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 600));

        Assert.Equal(new Point(0, 0), viewport.PaintOffsetOf(first));
        Assert.Equal(new Point(0, 60), viewport.PaintOffsetOf(second));
        Assert.IsType<SliverLogicalContainerParentData>(first.parentData);
    }

    private static RenderViewport BuildViewport(
        ViewportOffset offset,
        IReadOnlyList<RenderSliver> slivers,
        ScrollCacheExtent? scrollCacheExtent = null)
    {
        var viewport = new RenderViewport(offset: offset, scrollCacheExtent: scrollCacheExtent);
        RenderSliver? previous = null;
        foreach (RenderSliver sliver in slivers)
        {
            viewport.Insert(sliver, after: previous);
            previous = sliver;
        }

        return viewport;
    }

    private static PipelineOwner Layout(RenderViewport viewport, Size size)
    {
        if (viewport.Parent is RenderView existing)
        {
            existing.Owner!.RequestLayout();
            existing.Owner.FlushLayout(size);
            return existing.Owner;
        }

        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(size);
        return pipeline;
    }

    private sealed class SizedRenderBox(Size size) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private class FixedSliver(double scrollExtent, string? name = null, List<string>? paints = null)
        : RenderSliver
    {
        public SliverConstraints LastConstraints { get; private set; }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            LastConstraints = constraints;
            double paintExtent = CalculatePaintOffset(constraints, from: 0.0, to: scrollExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: paintExtent,
                MaxPaintExtent: scrollExtent,
                CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: scrollExtent),
                HasVisualOverflow: scrollExtent > constraints.RemainingPaintExtent);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
            if (name != null)
            {
                paints?.Add(name);
            }
        }
    }

    /// <summary>A sliver that pins itself to the leading edge, like a pinned header.</summary>
    private sealed class PinnedSliver(double extent) : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            double paintExtent = Math.Min(extent, constraints.RemainingPaintExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: extent,
                PaintExtent: paintExtent,
                LayoutExtent: Math.Max(0.0, extent - constraints.ScrollOffset),
                MaxPaintExtent: extent,
                MaxScrollObstructionExtent: extent,
                PaintOrigin: Math.Min(constraints.ScrollOffset, extent),
                CacheExtent: paintExtent);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// <summary>A sliver that asks for one scroll offset correction and then lays out normally.</summary>
    private sealed class CorrectOnceSliver(double correction, double scrollExtent) : RenderSliver
    {
        private bool _corrected;

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            if (!_corrected)
            {
                _corrected = true;
                Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
                return;
            }

            double paintExtent = CalculatePaintOffset(constraints, from: 0.0, to: scrollExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: paintExtent,
                MaxPaintExtent: scrollExtent);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
